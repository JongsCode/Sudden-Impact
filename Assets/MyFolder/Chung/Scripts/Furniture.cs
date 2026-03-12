using UnityEngine;
using Photon.Pun;

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
        Debug.Log($" {gameObject.name}가 맞았습니다! 들어온 데미지: {_data.damage}");

        if (isDestroyed) return;

        if (photonView == null)
        {
            Debug.LogError("가구에 PhotonView 컴포넌트가 없습니다!");
            return;
        }

        // 네트워크 상의 모든 인스턴스에 데미지 동기화
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, _data.damage);
    }

    [PunRPC]
    protected void RPC_ApplyDamage(float _damage)
    {
        curHp -= _damage;
        Debug.Log($"RPC 통신 성공! 남은 체력: {curHp}");
        if (curHp <= 0 && !isDestroyed)
        {
            Debug.Log($"{gameObject.name} 체력 0 도달! 부서짐 실행!");
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
        Obstacle obstacle = GetComponent<Obstacle>();
        if(obstacle != null)
        {
            obstacle.Broken();
        }
        // 부서진 후 물리 판정 제거
        //if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            //if (!col.isTrigger)
            //{
            //    col.enabled = false;
            //}
            col.enabled = false;
        }
    }
}