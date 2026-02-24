using System.Collections;
using UnityEngine;
using Photon.Pun;

// 던진 총 - Bullet 상속
// 날아가면서 메쉬가 빙글빙글 돌고 맞으면 스턴(base.Init~)
public class ThrownGun : Bullet
{
    [SerializeField] private float spinSpeed = 360f;    // 초당 회전 각도
    [SerializeField] private float throwSpeed = 15f;    // 던지기 이동 속도

    private GameObject gunMesh;

    public void Init(int _actorId, int _team, GameObject _targetMesh)
    {
        base.Init(_actorId, _team, 0f);    //던지기는 데미지 없음!!

        // 던져진 총만 특수 초기화 : 메쉬 주입
        gunMesh = _targetMesh;
        gunMesh.transform.SetParent(this.transform);
        gunMesh.transform.localPosition = Vector3.zero;
        gunMesh.transform.localRotation = Quaternion.identity;

        // 회전 애니메이션
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
        GetComponent<Rigidbody>().MovePosition(transform.position + transform.forward * throwSpeed * Time.deltaTime);
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

        PhotonView pv = PhotonView.Find((int)data[2]);
        GameObject MeshGo = pv.transform.GetChild((int)data[3]).gameObject;

        // 여기서 meshIdx를 보고 자신의 자식 오브젝트 중 해당 번호를 활성화
        // 예: transform.GetChild(meshIdx).gameObject.SetActive(true);
        Init((int)data[0], (int)data[1], MeshGo);
    }
}