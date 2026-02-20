using UnityEngine;
using Photon.Pun;

/// <summary>
/// 모든 무기의 최상위 추상 클래스
/// Gun, Knife 등이 이 클래스를 상속받음
/// </summary>
public abstract class Weapon : MonoBehaviourPun
{
    // ──────────────────────────────────────────────
    // 공통 데이터
    // ──────────────────────────────────────────────

    [Header("Base Stats")] //기본스탯
    [SerializeField] protected float damage;

    // 소지자 정보 (네트워크 동기화 필요 시 RPC로 처리?)
    protected int ownerActorNumber = -1;
    protected int ownerTeam = -1;

    // 소지 여부 확인용
    public bool IsHeld => ownerActorNumber != -1;

    // ──────────────────────────────────────────────
    // 추상 메소드 (하위 클래스가 반드시 구현)
    // ──────────────────────────────────────────────

    /// <summary>
    /// 공격 실행.
    /// Gun: 총알 발사 / Knife: 근접 판정
    /// </summary>
    /// 공격 방향 (마우스 방향 벡터)
    public abstract void Attack(Vector3 direction);

   
    


    
    // ──────────────────────────────────────────────
    // 공통 메소드
    // ──────────────────────────────────────────────

    /// <summary>
    /// 플레이어가 무기를 집었을 때 호출.
    /// 소지자 정보를 등록한다.
    /// </summary>
    public virtual void SetOwner(int actorNumber, int team)
    {
        ownerActorNumber = actorNumber;
        ownerTeam = team;
        Debug.Log($"[Weapon] SetOwner - Actor:{actorNumber} / Team:{team}");
    }

    /// <summary>
    /// 무기가 드롭될 때 호출
    /// 소지자 정보를 초기화
    /// </summary>
    public virtual void ClearOwner()
    {
        ownerActorNumber = -1;
        ownerTeam = -1;
        Debug.Log("[Weapon] ClearOwner - 소지자 정보 초기화");
    }

    // Getter
    public float Damage => damage;
    public int OwnerActorNumber => ownerActorNumber;
    public int OwnerTeam => ownerTeam;
}
