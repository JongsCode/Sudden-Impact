using UnityEngine;
using Photon.Pun;

public abstract class Gun : Weapon
{
    [Header("Gun References")]
    [SerializeField] protected GameObject projectilePrefab;     // 쏘는 총알 프리팹
    [SerializeField] protected GameObject thrownWeaponPrefab; // 던지는 총(무기) 프리팹
    [SerializeField] protected Transform muzzlePoint;         // 총구 위치

    [Header("Parameter")]
    [SerializeField] protected int maxAmmo = 30;
    [SerializeField] protected float fireRate = 0.1f;         // 연사 속도 

    [Tooltip("체크 시 꾹 누르면 연사, 해제 시 클릭마다 단발")]
    public bool isAutomatic = true;                           // 단발/연사 구분용 스위치 (PlayerController에서 읽음)

    protected int currentAmmo;
    protected float lastFireTime;                             // 마지막으로 총을 쏜 시간을 기억하는 변수

    protected virtual void OnEnable()
    {
        currentAmmo = maxAmmo; // 총을 꺼낼 때 장탄수 채우기
        lastFireTime = 0f;     // 무기를 꺼내자마자 바로 쏠 수 있도록 타이머 초기화
    }

    public override void Attack(Vector3 aimPos)
    {
        // 부모가 여기서 모든 쿨다운과 잔탄을 엄격하게 통제합니다.
        if (Time.time < lastFireTime + fireRate) return;

        if (currentAmmo <= 0)
        {
            Debug.Log("장탄수가 부족합니다! (재장전 필요)");
            return;
        }

        // 검사 통과! 시간 갱신 및 탄약 소모
        lastFireTime = Time.time;
        currentAmmo--;

        // 2. 자식아, 검사는 끝났다. 이제 진짜 총알을 쏴라!
        FireProjectile(aimPos);
    }

    // 3. 자식 클래스들이 반드시 구현해야 하는 '순수 발사 로직'
    protected abstract void FireProjectile(Vector3 aimPos);

    // PlayerController에서 호출했던 무기 던지기 기능
    public virtual void ThrowWeapon()
    {
        if (thrownWeaponPrefab == null) return;

        object[] info = new object[]
        {
            ownerActorNumber, ownerTeam, 0f
        };

        // 던져지는 무기도 네트워크상에서 모두에게 보여야 하므로 Photon Instantiate 사용
        GameObject thrownObj = PhotonNetwork.Instantiate(
            thrownWeaponPrefab.name,
            transform.position,
            transform.rotation,
            0,
            info
        );
    }
}