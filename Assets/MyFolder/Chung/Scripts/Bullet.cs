using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPun
{
    [SerializeField] protected float speed = 100f;
    protected float damage;
    protected int attackerId;
    protected int teamId;

    protected Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Gun이 스폰 직후 데미지/공격자 정보
    // ThrownGun이 base.Init()으로 호출 가능
    public virtual void Init(int _actorId, int _team, float _damage)
    {
        attackerId = _actorId;
        teamId = _team;
        damage = _damage;
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
                attackerActorNumber = attackerId,
                attackerTeam = teamId,
                type = DamageType.Bullet,
                hitPoint = other.ClosestPoint(transform.position),
                hitNormal = transform.forward * -1f
            };

            receiver.OnReceiveImpact(data);
            Debug.Log("총알 맞춤");
            PhotonNetwork.Destroy(gameObject);

        }
    }

}