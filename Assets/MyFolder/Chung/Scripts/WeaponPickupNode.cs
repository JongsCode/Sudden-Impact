using Photon.Pun;
using UnityEngine;

/// <summary>
/// [New System] 무기 픽업 노드 - 데이터 보관함.
///
/// 역할:
///   - 실제 Gun 오브젝트가 아닌, (EWeaponType + ammo) 데이터만 보유하는 가벼운 표지판입니다.
///   - 라운드 시작 시 WeaponSpawnManager가 SetupAndActivate()로 데이터를 주입합니다.
///   - 플레이어가 버린 총도 이 노드 형태로 필드에 남겨집니다 (CreateDropNode 경로).
///   - 플레이어가 상호작용하면 자신의 EWeaponType + ammo를 MasterClient에 전달하고 숨겨집니다.
///
/// 씬 설정:
///   1. PhotonView 컴포넌트 필수 (씬 배치 오브젝트).
///   2. Collider(isTrigger=false) 추가 후 레이어를 interactableLayer로 설정.
///   3. visualRoot: 활성/비활성 시 켜고 끌 자식 오브젝트 (메시, 이펙트 등).
///   4. defaultAmmo: 라운드 시작 시 이 노드가 제공할 기본 탄약 수 (Inspector에서 설정).
/// </summary>
public class WeaponPickupNode : MonoBehaviourPun, IInteractable
{
    [Header("Spawn Node Configuration (Inspector)")]
    [Tooltip("이 노드가 라운드 시작 시 제공할 무기 종류 (Inspector에서 설정, 드롭 노드는 런타임 덮어씀)")]
    [SerializeField] private Weapon.EWeaponType defaultWeaponType = Weapon.EWeaponType.Pistol;

    [Tooltip("라운드 시작 시 제공할 기본 탄약 수 (드롭 노드는 실제 잔탄수로 덮어씀)")]
    [SerializeField] private int defaultAmmo = 30;

    [Header("Visual")]
    [Tooltip("활성 시 켜고 비활성 시 끄는 비주얼 루트 오브젝트")]
    [SerializeField] private GameObject visualRoot;

    // ─── 런타임 데이터 (RPC_Setup으로 동기화) ────────────────────────
    private Weapon.EWeaponType _weaponType;
    private int _currentAmmo;
    private bool _isAvailable;

    // WeaponSpawnManager가 풀에서 자유 노드를 판별할 때 사용
    public bool IsAvailable => _isAvailable;

    // ─── WeaponSpawnManager에서 호출 ─────────────────────────────────

    /// <summary>
    /// 스폰 노드 활성화 (라운드 시작): Inspector의 defaultWeaponType + defaultAmmo 사용.
    /// </summary>
    public void ActivateAsSpawnNode()
    {
        SetupAndActivate(transform.position, defaultWeaponType, defaultAmmo);
    }

    /// <summary>
    /// 드롭 노드 활성화 (플레이어 무기 버리기): 위치 + 타입 + 잔탄수를 외부에서 주입.
    /// </summary>
    public void SetupAndActivate(Vector3 position, Weapon.EWeaponType type, int ammo)
    {
        // 모든 클라이언트에 위치·데이터·표시 상태를 동시에 동기화
        photonView.RPC(nameof(RPC_Setup), RpcTarget.All,
            position, (int)type, ammo, true);
    }

    /// <summary>
    /// 노드 비활성화 (픽업 완료 or 라운드 종료).
    /// </summary>
    public void Deactivate()
    {
        photonView.RPC(nameof(RPC_Setup), RpcTarget.All,
            transform.position, (int)_weaponType, 0, false);
    }

    [PunRPC]
    private void RPC_Setup(Vector3 position, int typeInt, int ammo, bool visible)
    {
        transform.position = position;
        _weaponType = (Weapon.EWeaponType)typeInt;
        _currentAmmo = ammo;
        _isAvailable = visible;

        if (visualRoot != null)
            visualRoot.SetActive(visible);
    }

    // ─── IInteractable 구현 ──────────────────────────────────────────

    /// <summary>
    /// PlayerController.TryInteract()에서 로컬 플레이어 입력 시 호출됩니다.
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (!_isAvailable) return;

        // 1. 모든 클라이언트에서 즉시 비활성화 (낙관적 업데이트)
        photonView.RPC(nameof(RPC_Setup), RpcTarget.All,
            transform.position, (int)_weaponType, 0, false);

        // 2. MasterClient에게 무기 장착 요청
        //    (type + ammo를 파라미터로 직접 전달 → MasterClient가 노드 상태를 조회할 필요 없음)
        photonView.RPC(nameof(RPC_RequestEquip), RpcTarget.MasterClient,
            player.photonView.ViewID, (int)_weaponType, _currentAmmo);
    }

    /// <summary>
    /// MasterClient 전용: 해당 플레이어에게 RPC_ForceEquipWeapon을 방송합니다.
    /// </summary>
    [PunRPC]
    private void RPC_RequestEquip(int playerViewID, int typeInt, int ammo)
    {
        PhotonView pv = PhotonView.Find(playerViewID);
        if (pv == null) return;

        // All 클라이언트의 PlayerController에서 무기를 활성화
        //pv.RPC(nameof(PlayerController.RPC_ForceEquipWeapon),
        //    RpcTarget.All, typeInt, ammo);

        // 이 노드를 드롭 풀로 반환
        WeaponSpawnManager.Instance?.ReturnNodeToDropPool(this);
    }
}
