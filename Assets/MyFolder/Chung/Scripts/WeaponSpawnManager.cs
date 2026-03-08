using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// [New System] 라운드 기반 무기 스폰 매니저 (씬에 1개, MasterClient 통제).
///
/// 핵심 컨셉:
///   - 실제 Gun 오브젝트를 필드에 생성하지 않습니다.
///   - 라운드 시작 → spawnNodes 중 N개 랜덤 활성화.
///   - 플레이어 드롭 → dropNodePool에서 자유 노드 꺼내 위치·데이터 주입.
///   - 라운드 종료 → 모든 스폰 노드 + 드롭 노드 일괄 비활성화.
///
/// 씬 설정:
///   spawnNodes  : 맵에 미리 배치한 고정 픽업 포인트 (각 노드에 EWeaponType + defaultAmmo 설정).
///   dropNodePool: 드롭 전용 풀 (씬에 비활성 상태로 배치, 최소 플레이어 수 * 2 권장).
/// </summary>
public class WeaponSpawnManager : MonoBehaviourPunCallbacks
{
    public static WeaponSpawnManager Instance { get; private set; }

    [Header("Spawn Nodes (Fixed Map Positions)")]
    [Tooltip("씬에 미리 배치된 고정 픽업 포인트들 (각 노드에 EWeaponType + defaultAmmo Inspector 설정 필요)")]
    [SerializeField] private WeaponPickupNode[] spawnNodes;

    [Tooltip("라운드당 활성화할 스폰 노드 수")]
    [SerializeField] private int activeNodesPerRound = 5;

    [Header("Drop Node Pool (Dynamic Drops)")]
    [Tooltip("플레이어가 무기를 버릴 때 재사용할 노드 풀 (씬에 비활성 상태로 미리 배치)")]
    [SerializeField] private WeaponPickupNode[] dropNodePool;

    // 런타임 추적
    private readonly List<WeaponPickupNode> _activeSpawnNodes = new List<WeaponPickupNode>();
    private readonly List<WeaponPickupNode> _activeDropNodes = new List<WeaponPickupNode>();

    // 싱글톤 초기화

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

    // 라운드 이벤트

    private void OnRoundStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        _activeSpawnNodes.Clear();

        // 스폰 노드를 랜덤 셔플 후 앞에서 N개만 활성화
        ShuffleArray(spawnNodes);
        int count = Mathf.Min(activeNodesPerRound, spawnNodes.Length);

        for (int i = 0; i < spawnNodes.Length; i++)
        {
            if (i < count)
            {
                // Inspector에서 설정한 defaultWeaponType + defaultAmmo로 활성화
                spawnNodes[i].ActivateAsSpawnNode();
                _activeSpawnNodes.Add(spawnNodes[i]);
            }
            else
            {
                spawnNodes[i].Deactivate();
            }
        }
    }

    private void OnRoundEnd()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 모든 스폰 노드 비활성화
        foreach (var node in _activeSpawnNodes)
            if (node != null) node.Deactivate();
        _activeSpawnNodes.Clear();

        // 모든 드롭 노드 비활성화
        foreach (var node in _activeDropNodes)
            if (node != null) node.Deactivate();
        _activeDropNodes.Clear();
    }

    // 드롭 노드 생성
    // PlayerController.DropWeapon() → MasterClient RPC → 이 메서드 실행

    /// <summary>
    /// MasterClient 전용: 드롭 풀에서 자유 노드를 꺼내 지정 위치/타입/잔탄수로 배치합니다.
    /// </summary>
    [PunRPC]
    public void RPC_CreateDropNode(Vector3 position, int typeInt, int ammo)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        WeaponPickupNode node = GetFreeDropNode();
        if (node == null)
        {
            Debug.LogWarning("[WeaponSpawnManager] Drop node pool exhausted!");
            return;
        }

        node.SetupAndActivate(position, (Weapon.EWeaponType)typeInt, ammo);
        _activeDropNodes.Add(node);
    }

    // 픽업 완료 후 풀 반환
    // WeaponPickupNode.RPC_RequestEquip()에서 호출

    /// <summary>
    /// 노드가 픽업되면 activeDropNodes 추적 목록에서 제거합니다.
    /// 노드 자체의 비활성화는 RPC_Setup으로 이미 처리됩니다.
    /// </summary>
    public void ReturnNodeToDropPool(WeaponPickupNode node)
    {
        _activeDropNodes.Remove(node);
    }

    // ─── 유틸리티 ────────────────────────────────────────────────────

    /// <summary>
    /// 드롭 풀에서 현재 비활성 상태인 자유 노드를 반환합니다.
    /// </summary>
    private WeaponPickupNode GetFreeDropNode()
    {
        foreach (var node in dropNodePool)
            if (node != null && !node.IsAvailable) return node;
        return null;
    }

    private static void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
