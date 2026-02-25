using UnityEngine;
using Photon.Pun;

// Gun : Weapon
// 권총, 우지, 샷건, 소총은 여기서 상속받음
public abstract class Gun : Weapon
{
    [SerializeField] protected int maxAmmo;
    protected int curAmmo;

    [SerializeField] protected GameObject bulletPrefab;  // 꼭 Resources 폴더에 있어야
    [SerializeField] protected GameObject thrownPrefab;
    [SerializeField] protected Transform firePoint;
    //[SerializeField] protected GameObject meshObject;    // 던질 때 ThrownGun에 넘겨줄 총 메쉬 // 수정됨 단순 프리팹 소환으로 수정됨
    [SerializeField] protected float fireRate = 1f;      // 초당 발사 횟수
    protected float lastFireTime = -999f;



    protected virtual void Awake()
    {
        curAmmo = maxAmmo;
    }

    // 발사
    // PlayerController에서 마우스 방향 벡터 넘겨줌
    //public override void Attack(Vector3 direction)
    //{
    //    if (!photonView.IsMine) return;
    //    if (IsAmmoEmpty()) return;
    //    if (Time.time - lastFireTime < 1f / fireRate) return;

    //    lastFireTime = Time.time;
    //    FireBullet(direction);
    //    curAmmo--;

    //    Debug.Log($"[총] 남은 탄: {curAmmo}/{maxAmmo}");
    //}

    // 총알 스폰
    // 샷건처럼 동시다나가는 Gun 하위 클래스에서 override
    protected virtual void FireBullet(Vector3 direction)
    {
        transform.LookAt(direction);

        GameObject bulletObj = PhotonNetwork.Instantiate(
            bulletPrefab.name,
            firePoint.position,
            firePoint.rotation
        );

        // Gun이 Bullet에 damage 직접 주입
        if (bulletObj.TryGetComponent<Bullet>(out var bullet))
        {
            bullet.Init(OwnerActorNumberLazy, ownerTeam, damage);
        }
    }

    // 탄퍼짐 있는 총기는 하위 클래스에서 override (우지, 샷건 등)
    //protected virtual Vector3 GetFireDirection(Vector3 direction)
   //
   //   return direction.normalized;
  //}

// 던지기
// 탄남아있어도 던질수있음
public void ThrowWeapon()
    {
        if (!photonView.IsMine) return;

        //transform.LookAt(transform.position + throwDirection);


        // ThrownGun 프리팹 네트워크 생성
        GameObject thrownObj = PhotonNetwork.Instantiate(
            thrownPrefab.name,
            firePoint.position,
            firePoint.rotation,
            0,
            new object[] { OwnerActorNumberLazy, ownerTeam, 0f }
            // 포톤 네트워크 인스턴티에이트는 해당 객체에게 값을 넘겨주는 기능이 있음
            // 추후 불렛에도 적용
        );

         //확장된 Init 호출
         // 기존의 불릿의 Init 사용
        //if (thrownObj.TryGetComponent<ThrownGun>(out var thrownGun))
        //{
        //    thrownGun.Init(OwnerActorNumberLazy, ownerTeam, 0f);
        //}




        ClearOwner();
        Debug.Log("[Gun] 던지기");
    }


    public bool IsAmmoEmpty() => curAmmo <= 0;
    public int CurAmmo => curAmmo;
    public int MaxAmmo => maxAmmo;

    public override void SetOwner(int actorNumber, int team)
    {
        base.SetOwner(actorNumber, team);

        // 주을때 물리 끄는
        if (TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;
    }
}