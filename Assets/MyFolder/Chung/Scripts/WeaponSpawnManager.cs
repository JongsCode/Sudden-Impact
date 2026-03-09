using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// [New System] 라운드 기반 무기 스폰 매니저 (씬에 1개, MasterClient 통제).
///
/// 핵심 컨셉:
///   - 실제 Gun 오브젝트를 필드에 생성하지 않음.
///   - 라운드 시작 → spawnNodes 중 N개 랜덤 활성화 (랜덤 EWeaponType 배정).
///   - 플레이어 드롭 → dropNodePool에서 자유 노드 꺼내 위치·데이터 주입.
///   - 라운드 종료 → 모든 스폰 노드 + 드롭 노드 일괄 비활성화.
///
/// 씬 설정:
///   spawnNodes  : 맵에 미리 배치한 고정 픽업 포인트 (위치 절대 변경 금지).
///   dropNodePool: 드롭 전용 풀 (씬에 비활성 상태로 배치, 최소 플레이어 수 * 2 권장).
/// </summary>
public class WeaponSpawnManager : MonoBehaviourPunCallbacks
{
    public static WeaponSpawnManager Instance { get; private set; }

    [Header("Spawn Nodes (Fixed Map Positions)")]
    [Tooltip("씬에 미리 배치된 고정 픽업 포인트들 — 위치는 절대 변경하지 않음")]
    [SerializeField] private WeaponPickupNode[] spawnNodes;

    [Tooltip("라운드당 활성화할 스폰 노드 수")]
    [SerializeField] private int activeNodesPerRound = 5;

    [Header("Drop Node Pool (Dynamic Drops)")]
    [Tooltip("플레이어가 무기를 버릴 때 재사용할 노드 풀 (씬에 비활성 상태로 미리 배치, 위치 변경 가능)")]
    [SerializeField] private WeaponPickupNode[] dropNodePool;

    // 라운드 시작 시 랜덤 배정할 무기 타입 (칼 제외)
    private static readonly Weapon.EWeaponType[] SpawnableTypes =
    {
        Weapon.EWeaponType.Pistol,
        Weapon.EWeaponType.Uzi,
        Weapon.EWeaponType.Shotgun,
        Weapon.EWeaponType.Rifle
    };

    // ─── 런타임 추적 ─────────────────────────────────────────────────
    private readonly List<WeaponPickupNode> _activeSpawnNodes = new List<WeaponPickupNode>();
    private readonly List<WeaponPickupNode> _activeDropNodes = new List<WeaponPickupNode>();

    // Gun.MaxAmmo 조회용 카탈로그 (PlayerController.Init()에서 MasterClient가 1회 등록)
    private readonly Dictionary<Weapon.EWeaponType, int> _gunMaxAmmos =
        new Dictionary<Weapon.EWeaponType, int>();

    // ─── 싱글톤 초기화 ───────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnRoundStart += OnRoundStart;
        GameEvents.OnRoundEnd += OnRoundEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnRoundStart -= OnRoundStart;
        GameEvents.OnRoundEnd -= OnRoundEnd;
    }

    // ─── 라운드 이벤트 ───────────────────────────────────────────────

    private void OnRoundStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _activeSpawnNodes.Clear();

        // 인덱스 배열만 셔플 — spawnNodes 배열 순서와 노드의 물리적 위치는 그대로 유지
        int[] indices = CreateShuffledIndices(spawnNodes.Length);
        int count = Mathf.Min(activeNodesPerRound, spawnNodes.Length);

        for (int i = 0; i < spawnNodes.Length; i++)
        {
            WeaponPickupNode node = spawnNodes[indices[i]];
            if (node == null) continue;

            if (i < count)
            {
                // Inspector의 defaultWeaponType 무시, 매 라운드 랜덤 타입 배정
                Weapon.EWeaponType randomType =
                    SpawnableTypes[Random.Range(0, SpawnableTypes.Length)];
                node.ActivateAsSpawnNode(randomType, GetDefaultAmmo(randomType));
                _activeSpawnNodes.Add(node);
            }
            else
            {
                node.Deactivate();
            }
        }
    }

    private void OnRoundEnd()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var node in _activeSpawnNodes)
            if (node != null) node.Deactivate();
        _activeSpawnNodes.Clear();

        foreach (var node in _activeDropNodes)
            if (node != null) node.Deactivate();
        _activeDropNodes.Clear();
    }

    // ─── Gun 카탈로그 등록 ───────────────────────────────────────────

    /// <summary>
    /// MasterClient 전용: PlayerController.Init()에서 allGuns를 건네주면
    /// 각 Gun의 MaxAmmo를 캐시. OnRoundStart보다 먼저 호출되어야 함.
    /// </summary>
    public void RegisterGunCatalog(Gun[] guns)
    {
        if (guns == null) return;
        foreach (var gun in guns)
            if (gun != null) _gunMaxAmmos[gun.WeaponType] = gun.MaxAmmo;

        Debug.Log("[WeaponSpawnManager] Gun catalog registered: " +
                  string.Join(", ", System.Linq.Enumerable.Select(
                      _gunMaxAmmos, kv => $"{kv.Key}={kv.Value}")));
    }

    /// <summary>캐시된 MaxAmmo 반환. 등록 전이면 fallback 30.</summary>
    private int GetDefaultAmmo(Weapon.EWeaponType type)
    {
        return _gunMaxAmmos.TryGetValue(type, out int ammo) ? ammo : 30;
    }

    // ─── 드롭 노드 생성 ──────────────────────────────────────────────
    // PlayerController.DropWeapon() → MasterClient RPC → 이 메서드 실행

    /// <summary>
    /// MasterClient 전용: 드롭 풀에서 자유 노드를 꺼내 지정 위치/타입/잔탄수로 배치.
    /// </summary>
    [PunRPC]
    public void RPC_CreateDropNode(Vector3 position, int typeInt, int ammo)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        WeaponPickupNode node = GetFreeDropNode();
        if (node == null)
        {
            Debug.LogWarning("[WeaponSpawnManager] Drop node pool exhausted! " +
                             "Increase dropNodePool size in Inspector.");
            return;
        }

        node.SetupAndActivate(position, (Weapon.EWeaponType)typeInt, ammo);
        _activeDropNodes.Add(node);
    }

    // ─── 픽업 완료 후 풀 반환 ────────────────────────────────────────
    // WeaponPickupNode.RPC_RequestPickup()에서 호출

    /// <summary>
    /// 노드가 픽업되면 activeDropNodes 추적 목록에서 제거.
    /// (스폰 노드는 여기에 없으므로 no-op)
    /// </summary>
    public void ReturnNodeToDropPool(WeaponPickupNode node)
    {
        _activeDropNodes.Remove(node);
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────

    /// <summary>드롭 풀에서 현재 비활성 상태인 자유 노드를 반환.</summary>
    private WeaponPickupNode GetFreeDropNode()
    {
        foreach (var node in dropNodePool)
            if (node != null && !node.IsAvailable) return node;
        return null;
    }

    /// <summary>셔플된 인덱스 배열 생성 — 원본 배열은 건드리지 않음.</summary>
    private static int[] CreateShuffledIndices(int length)
    {
        int[] indices = new int[length];
        for (int i = 0; i < length; i++) indices[i] = i;

        for (int i = length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        return indices;
    }
}
