using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPun
{
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private int myTeam = 0;


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IAttackReceiver>(out var receiver))
        {
            ImpactData data = new ImpactData
            {
                damage = bulletDamage,
                attackerActorNumber = photonView.Owner.ActorNumber,
                attackerTeam = myTeam,
                type = DamageType.Bullet,
                hitPoint = other.ClosestPoint(transform.position),
                hitNormal = transform.forward * -1f
            };
            if (photonView.IsMine)
            {
                receiver.OnReceiveImpact(data);
                Debug.Log("[Bullet] player mathod called");
            }
        }
    }
}
