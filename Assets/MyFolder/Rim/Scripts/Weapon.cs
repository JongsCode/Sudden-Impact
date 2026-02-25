using UnityEngine;
using Photon.Pun;

// 모든 무기의 최상위 추상 클래스
// Gun, Knife 등 여기서 상속받음
public abstract class Weapon : MonoBehaviourPun
{
    [SerializeField] protected float damage;
    protected int ownerActorNumber = -1;
    protected int ownerTeam = -1;

    // 게으른 초기화 SetOwner 안 불렸을 때 LocalPlayer에서 가져옴
    protected int OwnerActorNumberLazy =>
        ownerActorNumber != -1 ? ownerActorNumber : PhotonNetwork.LocalPlayer.ActorNumber;

    // 공격실행 (하위 클래스가 반드시 구현)
    public abstract void Attack(Vector3 direction);

    // 플레이어가 무기 집었을 때
    // actorNumber랑 team 받아서 저장해둠 (총알 Init할 때 여기서 꺼내씀)
    public virtual void SetOwner(int actorNumber, int team)
    {
        ownerActorNumber = actorNumber;
        ownerTeam = team;
    }

    // 무기가 드롭될 때
    public virtual void ClearOwner()
    {
        ownerActorNumber = -1;
        ownerTeam = -1;
        Debug.Log("[무기] clearOwner");
    }
}