using System.Collections;
using UnityEngine;
using Photon.Pun;

// 던진 총 - Bullet 상속
// 날아가면서 메쉬가 빙글빙글 돌고 맞으면 스턴(base.Init~)
public class ThrownGun : Bullet
{
    [SerializeField] private float spinSpeed = 360f;    // 초당 회전 각도
    [SerializeField] private float throwSpeed = 15f;    // 던지기 이동 속도

    [SerializeField] private GameObject gunMesh;

    private Rigidbody Rigidbody;

    //public void Init(int _actorId, int _team, GameObject _targetMesh)
    //{
    //    base.Init(_actorId, _team, 0f);    //던지기는 데미지 없음!!

    //    // 던져진 총만 특수 초기화 : 메쉬 주입
    //    gunMesh = _targetMesh;
    //    gunMesh.transform.SetParent(this.transform);
    //    gunMesh.transform.localPosition = Vector3.zero;
    //    gunMesh.transform.localRotation = Quaternion.identity;

    //    // 회전 애니메이션
    //    StartCoroutine(SpinMesh());
    //}

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }

    public override void Init(int _actorId, int _team, float _damage)
    {
        base.Init(_actorId, _team, _damage);

        StartCoroutine(SpinMesh());
    }

    // 자식 메쉬만 로컬축으로 회전 코루틴
    private IEnumerator SpinMesh()
    {
        while (gunMesh != null)
        {
            gunMesh.transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    // 부모(Bullet) Update
    protected override void Update()
    {
        // 기존의 매 프레임 겟 컴포넌트 방식에서 어웨이크에서 미리 저장하여 겟 컴포턴트 비용 절감
        Rigidbody.MovePosition(transform.position + transform.forward * throwSpeed * Time.deltaTime);
    }

    // 충돌 : 스턴 유발 (DamageType.Throw)
    protected override void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.TryGetComponent<IAttackReceiver>(out var receiver))
        {
            ImpactData data = new ImpactData
            {
                damage = 0f,                        // 던지기는 데미지 없음, 스턴만
                attackerActorNumber = attackerId,
                attackerTeam = teamId,
                type = DamageType.Throw,
                hitPoint = other.ClosestPoint(transform.position),
                hitNormal = transform.forward * -1f
            };

            receiver.OnReceiveImpact(data);
            Debug.Log("[ThrownGun] 맞았음 스턴 걸림");

            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        if (data != null && data.Length >= 3)
        {
            int actorId = (int)data[0];
            int team = (int)data[1];
            float dmg = (float)data[2];
            Init(actorId, team, dmg); // 모든 클라이언트에서 초기화 실행
        }
    }

}