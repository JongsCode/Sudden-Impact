using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [SerializeField] protected float speed = 100f;
    protected float damage;
    protected int attackActorNum;
    protected int team;

    protected Rigidbody rb;

    [SerializeField]
    protected DamageType damageType;
    [SerializeField]
    private LayerMask obstacleLayer;

    protected virtual void Awake()
    {
        
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Update()
    {
        rb.MovePosition(transform.position + transform.forward * speed * Time.deltaTime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.TryGetComponent<IAttackReceiver>(out var receiver))
        {

            ImpactData data = new ImpactData
            {
                damage = damage,
                attackerActorNumber = attackActorNum,
                attackerTeam = team,
                type = damageType,
                hitPoint = other.ClosestPoint(transform.position),
                hitNormal = transform.forward * -1f
            };

            receiver.OnReceiveImpact(data);
            Debug.Log($"[Projectile] Projectile Hit");
            PhotonNetwork.Destroy(gameObject);
        }
        else if(other.gameObject.layer == obstacleLayer)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;

        // 데이터가 제대로 들어왔는지 방어 코드
        if (data != null && data.Length >= 3)
        {
            // object로 넘어오므로 원래 타입으로 캐스팅(Unboxing)
            attackActorNum = (int)data[0];
            team = (int)data[1];
            damage = (float)data[2];

            Debug.Log($"[Projectile] 스폰 동기화 완료! 공격자: {attackActorNum}, 팀: {team}, 데미지: {damage}");
        }
    }

}