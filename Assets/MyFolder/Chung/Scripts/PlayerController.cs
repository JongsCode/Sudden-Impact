using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Photon.Pun;
using System.Collections.Generic;

public class PlayerController : MonoBehaviourPun, IAttackReceiver
{
    public delegate void StunDelegate(bool _isInStun);

    private StunDelegate stunCallback;

    public StunDelegate StunCallback { set  { stunCallback = value; } }

    [SerializeField] private Rigidbody myRigidbody;
    [SerializeField] private PhotonTransformView myTransformView;
    [SerializeField] private Transform weaponAttachPoint;
    [SerializeField] private Weapon myKnife;
    [SerializeField] private PlayerRegistry registry;

    [Header("Parameters")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 2f;
    [SerializeField] private float rollDistance = 2.0f;
    [SerializeField] private float rollDuration = 0.2f;
    [SerializeField] private float pickUpDistance = 1f;
    [SerializeField] private float stunDuration = 1.5f;

    [Header("ForDebug")]
    [SerializeField] private float curHp;
    [SerializeField] private Weapon closestGun;
    [SerializeField] private Weapon myEquippedGun;
    [SerializeField] private bool useGun;
    [SerializeField] private int myTeam;

    private Coroutine curCheakClosestWeaponCoroutine;
    private List<Weapon> nearbyItems = new List<Weapon>();

    public enum PlayerState
    {
        NotReady, Idel, Sprint, Rolling, Stunned, Dead
    }
    private PlayerState playerState;
    public PlayerState GetPlayerState { get { return playerState; } }

    private void Awake()
    {
        myKnife.SetOwner(photonView.Owner.ActorNumber, registry.MyTeam);
    }

    private void OnEnable()
    {
        curHp = maxHp;
        SetPlayerState(PlayerState.Idel);
    }

    private void OnDisable()
    {
        // 죽거나 꺼질 때, 만약 내가 기절 상태였다면 입력을 강제로 복구하고 나감
        if (photonView.IsMine)
        {
            stunCallback?.Invoke(true);
        }
    }

    public void Init(int _myTeam)
    {
        myTeam = _myTeam;
    }

    public void Respawn(Vector3 spawnPos)
    {
        curHp = maxHp;
        SetPlayerState(PlayerState.Idel);
        transform.position = spawnPos;
        gameObject.SetActive(true);
    }

    private void SetPlayerState(PlayerState _state)
    {
        playerState = _state;
    }

    #region 조작 로직
    public void MovePlayer(Vector3 _moveAxis)
    {


        Vector3 moveVector = transform.position + ((_moveAxis * moveSpeed) * Time.deltaTime);
        myRigidbody.MovePosition(moveVector);
    }

    public void RotatePlayer(Vector3 _aimPos)
    {

        // 1. 에임 위치를 받아 상대위치 계산
        Vector3 lookPos = _aimPos - transform.position;

       lookPos.y = 0; // 2. x , z 만 가지고 계산하면 y축의 회전만 사용

        // 3. 방향이 0이 아닐 때만 회전 처리 (제자리에서 에러 방지)
        if (lookPos.sqrMagnitude > 0.001f)
        {
            // 4. 방향을 Quaternion으로 변환
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);

            // 5. 리지드바디를 통해 물리적으로 회전 
            myRigidbody.MoveRotation(targetRotation);
        }
    }


    #region 구르기 로직
    public void TryRoll(InputAction.CallbackContext ctx)
    {
        // 이동 코루틴
        StartCoroutine(RollCoroutine());

        // 무적 RPC
        photonView.RPC(nameof(StartRollCRP), RpcTarget.All);
    }

    [PunRPC]
    public void StartRollCRP()
    {
        StartCoroutine(RollingStateCoroutine());
    }

    private IEnumerator RollCoroutine()
    {
        Debug.Log($"코루틴 시작 | IsMine: {photonView.IsMine} | forward: {transform.forward} | startPos: {transform.position}");

        Vector3 rollDirection = transform.forward;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + rollDirection * rollDistance;
        float elapsed = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rollDuration;

            // EaseOut 느낌으로 초반 빠르고 후반 느리게
            myRigidbody.MovePosition(Vector3.Lerp(startPos, targetPos, t * t * (3f - 2f * t)));


            //Debug.Log($"[PlayerController] while Is Working / Progress: { t }, Position Value : { Vector3.Lerp(startPos, targetPos, t * t * (3f - 2f * t)) }");
            yield return null;
        }

        //myRigidbody.MovePosition(targetPos);
    }

    private IEnumerator RollingStateCoroutine()
    {
        playerState = PlayerState.Rolling;
        yield return new WaitForSeconds(rollDuration);
        playerState = PlayerState.Idel;
    }

    #endregion

    public void TrySwapWeapon(InputAction.CallbackContext ctx)
    {
        if (myEquippedGun != null)
        {
            useGun = useGun ? false : true;

            photonView.RPC(nameof(SwapWeapon), RpcTarget.All, useGun);
        }
    }

    [PunRPC]
    private void SwapWeapon(bool _useGun)
    {
        myEquippedGun.gameObject.SetActive(_useGun);
        myKnife.gameObject.SetActive(!_useGun);
    }


    public void SprintStart(InputAction.CallbackContext ctx)
    {
        Debug.Log("[PlayerController] Im Start Sprinting");
        moveSpeed *= sprintSpeed;
    }

    public void SprintEnd(InputAction.CallbackContext ctx)
    {
        moveSpeed /= sprintSpeed;
        Debug.Log("[PlayerController] Im end Sprinting");
    }

    public void TryAttack(Vector3 _aimPos)
    {

        if (useGun)
        {
        Debug.Log("[PlayerController] Im Start Fire");
        }

        else
        {
            myKnife.Attack(_aimPos);
            Debug.Log("[PlayerController] Im Start MeleeAtack"); 
        }
                
    }

    #region 던지기 , 줍기

    public void PickUpAndDrop(InputAction.CallbackContext ctx)
    {
        Debug.Log("[PlayerController] Im Equiet or Throwing");

        if (!closestGun)
        {
            Debug.Log("[PlayerController] Try Throw");
            return;
        }
        photonView.RPC(nameof(PickUpItem), RpcTarget.All ,closestGun.photonView.ViewID);


    }

    #region 줍기
    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        Weapon weapon;
        if(other.TryGetComponent<Weapon>(out weapon))
        {
            if (other.CompareTag("EquippedWeapon")) return;
            nearbyItems.Add(weapon);

            if (closestGun == null)
            {
                closestGun = weapon;
                curCheakClosestWeaponCoroutine = StartCoroutine(CheckClosestWeapon());
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine) return;

        Weapon weapon;
        if (other.TryGetComponent<Weapon>(out weapon))
        {
            if (!nearbyItems.Remove(weapon)) return;

            if (nearbyItems.Count == 0)
            {
                StopCoroutine(curCheakClosestWeaponCoroutine);
                closestGun = null;
            }
        }
    }

    private IEnumerator CheckClosestWeapon()
    {
        while (nearbyItems.Count > 0)   // 트리거에 들어와 있는 아이템이 있으면
        {
            yield return new WaitForSeconds(5f / 60f); // 5프레임 주기 
            float minSqrDistance = float.MaxValue; 
            Weapon tempClosest = null;

            for (int i = nearbyItems.Count - 1; i >= 0; i--) // 역순 순회로 안정성 확보 
            {
                if (nearbyItems[i] == null) { nearbyItems.RemoveAt(i); continue; }
            

            float sqrDist = (transform.position - nearbyItems[i].transform.position).sqrMagnitude; 
            if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    tempClosest = nearbyItems[i]; 
                }
            }
            closestGun = tempClosest;
        }
        closestGun = null;
        curCheakClosestWeaponCoroutine = null;
    }

    //private IEnumerator CheckClosestWeapon()
    //{
    //    while (nearbyItems.Count > 0)
    //    {
    //        yield return new WaitForSeconds(5f / 60f); // 5프레임마다 

    //        foreach (var weapon in nearbyItems)
    //        {
    //            float newItemDis = Vector3.Distance(transform.position,weapon.transform.position);
    //            float curItemDis = Vector3.Distance(transform.position,closestGun.transform.position);

    //            if (newItemDis < curItemDis)
    //            {
    //                closestGun = weapon;
    //            }
    //        }
    //    }
    //}

    [PunRPC]
    public void PickUpItem(int _viewID)
    {
        if(!photonView.IsMine)
        {
            closestGun = PhotonView.Find(_viewID).GetComponent<Weapon>();
        }

        DropWeapon();

        if (nearbyItems.Contains(myEquippedGun))
        {
            nearbyItems.Remove(myEquippedGun);
        }

        myEquippedGun = closestGun;
        closestGun = null;

        if(photonView.IsMine)
        {
            myEquippedGun.photonView.RequestOwnership();
        }

        myEquippedGun.transform.SetParent(weaponAttachPoint);
        myEquippedGun.transform.localPosition = Vector3.zero;
        myEquippedGun.transform.localRotation = Quaternion.identity;

        useGun = true;
        SwapWeapon(useGun);

    }

    private void DropWeapon()
    {
        if (myEquippedGun != null)
        {
            myEquippedGun.gameObject.SetActive(true);
            myEquippedGun.transform.SetParent(null);
            myEquippedGun = null;
        }
    }
    #endregion

    #region 던지기

    private void TryThrow()
    {

    }

    #endregion

    #endregion

    #endregion

    #region OnImpact
    public void OnReceiveImpact(ImpactData _data)
    {
        // 상태 검사
        if (
            curHp <= 0 
            || playerState == PlayerState.NotReady 
            || playerState == PlayerState.Dead 
            || playerState == PlayerState.Rolling
            || _data.attackerTeam == myTeam 
            ) { return; }

            //Debug.Log($"[PlayerController] Receive State is Ok");

        if(_data.type == DamageType.Throw)
        {
            StunPlayer();
        }

        //내가 쏜 총알이 아니면 데미지 RPC호출
        if(_data.attackerActorNumber != photonView.Owner.ActorNumber)
        {
            photonView.RPC(nameof(TakeDamage), RpcTarget.All, _data.damage);
            Debug.Log($"[PlayerController] Received");
        }

    }

    [PunRPC]
    public void TakeDamage(float _damage)
    {
        curHp -= _damage;
        Debug.Log($"[PlayerController] <color=red> Hit </color> {photonView.Owner.ActorNumber}'s Hp Is : {curHp}");

        if (curHp <= 0)
        {
            DiePlayer();
        }
    }

    private void DiePlayer()
    {
        DropWeapon();
        // 게임 메니저의 이벤트 버스 호출 필요
        // 인풋 메니저에게 콜백 필요
        DebugGameManager.Instance?.OnPlayerDied(this);


        this.gameObject.SetActive(false);
    }

    private void StunPlayer()
    {
        photonView.RPC(nameof(StunRPC), photonView.Owner);

    }

    [PunRPC]
    private void StunRPC()
    {
        if (!gameObject.activeSelf) return;
        StartCoroutine(StunCoroutine());
    }

    private IEnumerator StunCoroutine()
    {
        Debug.Log($"[PlayerController] {photonView.Owner.ActorNumber} is Stuned");
        
        if(photonView.IsMine)
        {
            playerState = PlayerState.Stunned;
            stunCallback?.Invoke(false);
            yield return new WaitForSeconds(stunDuration);
            playerState = PlayerState.Idel;
            stunCallback?.Invoke(true);
        }
        else
        {
            playerState = PlayerState.Stunned;
            yield return new WaitForSeconds(stunDuration);
            playerState = PlayerState.Idel;
        }
    }

    #endregion

}
