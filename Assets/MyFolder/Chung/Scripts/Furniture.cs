using UnityEngine;
using Photon.Pun;
using UnityEditor.UIElements;

public class Furniture : MonoBehaviourPun, IAttackReceiver
{
    [Header("Stats")]
    [SerializeField] protected float maxHp = 50f;
    protected float curHp;
    protected bool isDestroyed = false;



    protected virtual void Awake()
    {
        curHp = maxHp;
    }

    // IAttackReceiver 구현
    public virtual void OnReceiveImpact(ImpactData _data)
    {
        if (isDestroyed) return;
        // 네트워크 상의 모든 인스턴스에 데미지 동기화
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, _data.damage);
    }

    [PunRPC]
    protected void RPC_ApplyDamage(float _damage)
    {
        curHp -= _damage;
        if (curHp <= 0 && !isDestroyed)
        {
            
            OnBroken();
        }
    }

    protected virtual void OnBroken()
    {
        isDestroyed = true;
        
        Item item = GetComponent<Item>();
        if (item != null)
        {
            Debug.Log("SetGhostFurniture");
            item.SetBrokenState(isDestroyed);
        }
        // 부서진 후 물리 판정 제거
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
    }
}