using UnityEngine;
using Photon.Pun;

public class TestBullet : MonoBehaviourPun
{
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private int myTeam = 0;
    [SerializeField] private DamageType myType = DamageType.Bullet;


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IAttackReceiver>(out var receiver))
        {
            ImpactData data = new ImpactData
            {
                damage = bulletDamage,
                attackerActorNumber = PhotonNetwork.MasterClient.ActorNumber,
                attackerTeam = myTeam,
                type = myType,
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
