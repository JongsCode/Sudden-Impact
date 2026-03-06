using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviourPun, IAttackReceiver
{
    public delegate void StunDelegate(bool _isInStun);

    private StunDelegate stunCallback;

    public StunDelegate StunCallback { set { stunCallback = value; } }

    public enum PlayerState
    {
        NotReady, Idle, Sprint, Rolling, Stunned, Dead
    }

    [SerializeField] private Rigidbody myRigidbody;
    [SerializeField] private PhotonTransformView myTransformView;
    [SerializeField] private Transform weaponAttachPoint;
    [SerializeField] private Weapon myKnife;
    [SerializeField] private PlayerRegistry registry;
    [SerializeField] private GameObject dummyFlagMesh;

    [Header("Parameters")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 1.4f;
    [SerializeField] private float rollDistance = 2.0f;
    [SerializeField] private float rollDuration = 0.2f;
    [SerializeField] private float pickUpDistance = 1f;
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float rollCooldown = 4f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRadius = 2.0f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Animator Bridge Data")]
    // 애니메이터가 읽어갈 퍼블릭 게터
    public Vector3 CurrentVelocity => myRigidbody.linearVelocity;
    public bool UseGun => useGun;

    // 애니메이터에게 행동을 알리는 이벤트
    public event Action OnAttackEvent;
    public event Action OnRollEvent;
    public event Action OnThrowEvent;
    public event Action OnInteractEvent;
    public event Action OnStunnedEvent;

    [Header("ForDebug")]
    [SerializeField] private float curHp;
    [SerializeField] private Weapon closestGun;
    [SerializeField] private Weapon myEquippedGun;
    [SerializeField] private bool useGun;
    [SerializeField] private bool hasEnemyFlag;
    [SerializeField] private int myTeam;
    [SerializeField] private PlayerState playerState;

    private Coroutine curCheakClosestWeaponCoroutine;
    private List<Weapon> nearbyItems = new List<Weapon>();
    private Vector3 curMoveInput;
    private Vector3 lastMoveDir;
    private bool isSprinting;
    private float lastRollTime;

    public int MyTeam { get { return myTeam; } }
    public bool HasEnemyFlag { get { return hasEnemyFlag; } }
    public PlayerState GetPlayerState { get { return playerState; } }
    public Weapon MyEquippedGun { get { return myEquippedGun; } }

    private void Awake()
    {
        myKnife.SetOwner(photonView.Owner.ActorNumber, registry.MyTeam);
        if (photonView.IsMine)
        {
            myRigidbody.isKinematic = false;
        }
    }

    private void OnEnable()
    {
        curHp = maxHp;
        SetPlayerState(PlayerState.Idle);
        GameEvents.WeaponChanged(myKnife.WeaponType.ToString(), false);
    }

    private void OnDisable()
    {
        // 죽거나 꺼질 때, 만약 내가 기절 상태였다면 입력을 강제로 복구하고 나감
        if (photonView.IsMine)
        {
            stunCallback?.Invoke(true);
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        // 구르기 중에는 구르기 코루틴이 처리.
        if (playerState == PlayerState.Rolling) return;

        // 상태 이상일 때는 강제로 속도를 0으로 만들고 기절
        if (playerState == PlayerState.Stunned || playerState == PlayerState.Dead || playerState == PlayerState.NotReady)
        {
            myRigidbody.linearVelocity = Vector3.zero;
            myRigidbody.angularVelocity = Vector3.zero;
            return;
        }

        // MovePosition을 버리고 linearVelocity(물리 속도)를 직접 제어합니다.
        if (curMoveInput.sqrMagnitude > 0.01f)
        {
            float currentSpeed = isSprinting ? (moveSpeed * sprintSpeed) : moveSpeed;

            // 입력 방향으로 목표 속도를 계산합니다.
            Vector3 targetVelocity = curMoveInput.normalized * currentSpeed;

            // 물리 엔진의 속도(Velocity)에 직접 값을 넣습니다. 
            // 엔진이 스스로 충돌을 계산하므로 아무리 얇은 벽이라도 절대 뚫지 못합니다.
            myRigidbody.linearVelocity = targetVelocity;
        }
        else
        {
            // 입력이 없을 때는 즉시 속도를 0으로 만들어 빙판길 미끄러짐(하키볼)을 막습니다.
            myRigidbody.linearVelocity = Vector3.zero;
        }

        // 회전력은 매 프레임 죽여서 오뚝이처럼 넘어지는 것을 막습니다.
        myRigidbody.angularVelocity = Vector3.zero;
    }

    public void Init(int _myTeam)
    {
        myTeam = _myTeam;
    }

    // 라운드 시작시 초기화 목적으로 호출
    public void Respawn(Vector3 spawnPos)
    {
        curHp = maxHp;
        SetPlayerState(PlayerState.Idle);
        transform.position = spawnPos;
        gameObject.SetActive(true);
        dummyFlagMesh.SetActive(false);
        GameEvents.HpChanged(curHp);
    }

    // 라운드 종료 시 즉각적인 초기화 및 조작 차단
    public void OnRoundEndReset()
    {
        // 1. 상태를 NotReady로 바꿔서 모든 조작(이동, 사격)을 막음
        SetPlayerState(PlayerState.NotReady);

        // 2. 깃발 상태 초기화 및 시각적 가짜 깃발 끄기
        hasEnemyFlag = false;
        dummyFlagMesh.SetActive(false);

        // 3. 무기 정리 (선택 사항: 들고 있던 무기를 내려놓거나, 초기 상태로 되돌림)
        DropWeapon();

        // 4. 구르기나 기절 코루틴이 돌고 있다면 정지
        StopAllCoroutines();
    }

    private void SetPlayerState(PlayerState _state)
    {
        playerState = _state;
    }

    #region 깃발 로직

    public void GetFlag()
    {
        hasEnemyFlag = true;
        dummyFlagMesh.SetActive(true);
    }

    #endregion

    #region 조작 로직

    #region 이동 및 방향 전환
    public void MovePlayer(Vector3 _moveAxis)
    {
        lastMoveDir = _moveAxis.normalized;
        curMoveInput = _moveAxis;
        //Vector3 moveVector = transform.position + ((_moveAxis.normalized * moveSpeed) * Time.deltaTime);
        //myRigidbody.MovePosition(moveVector);
    }

    public void RotatePlayer(Vector3 _aimPos)
    {
        if (playerState == PlayerState.Rolling
            || playerState == PlayerState.Dead
            || playerState == PlayerState.NotReady) return;

        Vector3 lookPos = _aimPos - transform.position;
        lookPos.y = 0;

        float distance = lookPos.magnitude;

        if (distance > 0.001f)
        {
            // 1. 에임을 향하는 기본 회전값
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);

            if (myEquippedGun != null)
            {
                Transform muzzleTransform = myEquippedGun.AttackPoint;

                if (muzzleTransform != null)
                {
                    // [핵심 수정] 총구의 '전체 대각선 거리'가 아니라, 
                    // 캐릭터(transform) 기준으로 순수하게 '우측으로 몇 미터 떨어져 있는지(로컬 X좌표)'만 가져옵니다!
                    float rightOffset = transform.InverseTransformPoint(muzzleTransform.position).x;

                    // 마우스가 우측 오프셋보다 멀리 있을 때만 역산 적용 (아크사인 에러/NaN 방지)
                    if (distance > Mathf.Abs(rightOffset))
                    {
                        // 3. 순수 우측 오프셋(rightOffset)만을 사용해 정확한 비틀림 각도 계산
                        float correctionAngle = Mathf.Asin(rightOffset / distance) * Mathf.Rad2Deg;

                        // 4. 회전 적용 (오른쪽(양수)에 있으면 음수 각도로 왼쪽으로 틂)
                        targetRotation *= Quaternion.Euler(0f, -correctionAngle, 0f);
                    }
                }
            }

            // 5. 최종 물리 회전
            myRigidbody.MoveRotation(targetRotation);
        }
    }
    #endregion

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

        if (playerState == PlayerState.Rolling
            || playerState == PlayerState.Stunned
            || playerState == PlayerState.Dead)
            yield break;

        if (Time.time < lastRollTime + rollCooldown)
        {
            Debug.Log("[PlayerController] Rolling is Cooling Down");
            yield break;
        }

        Vector3 rollDirection;
        if (Vector3.SqrMagnitude(lastMoveDir) > 0.2f)
        {
            rollDirection = lastMoveDir;
        }
        else
        {
            rollDirection = transform.forward;
        }

        myRigidbody.MoveRotation(Quaternion.LookRotation(rollDirection));

        float startSpeed = (rollDistance / rollDuration) * 2f;

        float elapsed = 0f;

        OnRollEvent?.Invoke();

        while (elapsed < rollDuration)
        {
            // 물리 프레임 시간 누적
            elapsed += Time.fixedDeltaTime;
            float t = elapsed / rollDuration;

            // 속도를 선형적으로 줄임(EaseOut)
            float currentSpeed = Mathf.Lerp(startSpeed, 0f, t);

            // 오직 벨로시티(Velocity)만으로 구르기
            myRigidbody.linearVelocity = rollDirection * currentSpeed;

            yield return new WaitForFixedUpdate();
        }

        // 구르기가 끝난 후 잔여 속도를 소멸시켜 미끄러짐 방지
        myRigidbody.linearVelocity = Vector3.zero;

        lastRollTime = Time.time;
    }

    // 상태 설정용
    private IEnumerator RollingStateCoroutine()
    {
        playerState = PlayerState.Rolling;
        yield return new WaitForSeconds(rollDuration);
        playerState = PlayerState.Idle;
    }

    #endregion

    #region 무기 전환
    public void TrySwapWeapon(InputAction.CallbackContext ctx)
    {
        if (myEquippedGun != null)
        {
            useGun = useGun ? false : true;

            photonView.RPC(nameof(SwapWeapon), RpcTarget.All, useGun);
        }
        else
        {
            useGun = false;
            photonView.RPC(nameof(SwapWeapon), RpcTarget.All, useGun);
        }
    }

    [PunRPC]
    private void SwapWeapon(bool _useGun)
    {
        
        if (myEquippedGun != null)
        {
            myEquippedGun.gameObject.SetActive(_useGun);
            
        }
        myKnife.gameObject.SetActive(!_useGun);

        string curWeaponName = useGun ? myEquippedGun.WeaponType.ToString() : myKnife.WeaponType.ToString();
        GameEvents.WeaponChanged(curWeaponName, _useGun);
    }
    #endregion

    #region 달리기
    public void SprintStart(InputAction.CallbackContext ctx)
    {
        // 기절, 사망 등 조작 불능 상태면 달리기 무시
        if (playerState == PlayerState.NotReady || playerState == PlayerState.Stunned || playerState == PlayerState.Dead) return;

        isSprinting = true;
        // 필요하다면 여기서 playerState = PlayerState.Sprint; 로 변경해도 좋습니다.
        Debug.Log("[PlayerController] Im Start Sprinting");
    }

    public void SprintEnd(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
        Debug.Log("[PlayerController] Im end Sprinting");
    }
    #endregion

    public void TryAttack(Vector3 _aimPos, bool _isHeld = false)
    {

        if (useGun)
        {
            myEquippedGun.Attack(_isHeld);
            Debug.Log("[PlayerController] Im Start Fire");
        }

        else
        {
            myKnife.Attack(_isHeld);
            Debug.Log("[PlayerController] Im Start MeleeAtack");
        }

    }

    #region 던지기 , 줍기

    public void PickUpAndDrop(InputAction.CallbackContext ctx)
    {
        Debug.Log("[PlayerController] Im Equiet or Throwing");

        if (closestGun == null && myEquippedGun != null)
        {
            photonView.RPC(nameof(TryThrow), RpcTarget.All, myEquippedGun.photonView.ViewID);
            Debug.Log("[PlayerController] Try Throw");
            return;
        }
        else if (closestGun != null)
        {
            photonView.RPC(nameof(PickUpItem), RpcTarget.All, closestGun.photonView.ViewID);
        }


    }

    #region 줍기
    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        Weapon weapon;
        if (other.TryGetComponent<Weapon>(out weapon))
        {
            if (weapon == myEquippedGun) return;
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
        if (!photonView.IsMine)
        {
            PhotonView targetView = PhotonView.Find(_viewID);

            if (targetView == null) return;
            closestGun = targetView.GetComponent<Weapon>();
        }

        DropWeapon();

        if (nearbyItems.Contains(myEquippedGun))
        {
            nearbyItems.Remove(myEquippedGun);
        }

        myEquippedGun = closestGun;
        nearbyItems.Remove(closestGun);
        closestGun = null;

        myEquippedGun.gameObject.layer = 11;
        myEquippedGun.SetOwner(PhotonNetwork.LocalPlayer.ActorNumber, myTeam);

        if (photonView.IsMine)
        {
            myEquippedGun.photonView.RequestOwnership();
        }
        Item item = myEquippedGun.GetComponent<Item>();
        item.PickItem();
        Debug.Log("PickItem");

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

    [PunRPC]
    private void TryThrow(int _viewID)
    {
        // 내 손에 총이 없거나, 던지라는 총의 ID가 내 손의 총과 다르면 무시
        if (myEquippedGun == null || myEquippedGun.photonView.ViewID != _viewID) return;

        // 무기가 Gun일 때만 던지기(ThrowWeapon) 실행
        if (myEquippedGun is Gun myGun)
        {
            myGun.ThrowWeapon();
            OnThrowEvent?.Invoke();
        }
        else
        {
            // 만약 칼(MeleeKnife)도 나중에 던지는 기능이 생긴다면
            Debug.Log("이 무기는 던질 수 없는 무기입니다 (Gun이 아님).");
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(myEquippedGun.gameObject);
        }

        myEquippedGun = null;
        useGun = false;
        SwapWeapon(false);
    }

    #endregion

    #endregion

    public void TryInteract(InputAction.CallbackContext ctx)
    {
        // 1. 내 주변 2m 안의 가구/오브젝트 검색
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactableLayer);

        foreach (var hit in hits)
        {
            // 2. 인터페이스 추출 시도
            if (hit.TryGetComponent<IInteractable>(out var target))
            {
                // 3. 된다면 실행
                target.Interact(this);

                break; // 한 번에 하나만 상호작용
            }
        }
    }

    #endregion

    #region OnImpact
    public void OnReceiveImpact(ImpactData _data)
    {
        Debug.Log($"[PlayerController] On Impact actor: {_data.attackerActorNumber}, ATKteam : {_data.attackerTeam}, myTeam{myTeam} , Data : {_data.type}");
        // 상태 검사
        if (
            curHp <= 0
            || playerState == PlayerState.NotReady
            || playerState == PlayerState.Dead
            || playerState == PlayerState.Rolling
            || _data.attackerTeam == myTeam
            ) { return; }

        //Debug.Log($"[PlayerController] Receive State is Ok");

        if (_data.type == DamageType.Throw)
        {
            StunPlayer();
        }

        //내가 쏜 총알이 아니면 데미지 RPC호출
        if (_data.attackerActorNumber != photonView.Owner.ActorNumber)
        {
            photonView.RPC(nameof(TakeDamage), RpcTarget.All, _data.damage);
            Debug.Log($"[PlayerController] Received");
        }

    }

    [PunRPC]
    public void TakeDamage(float _damage)
    {
        curHp -= _damage;
        GameEvents.HpChanged(curHp);
        Debug.Log($"[PlayerController] <color=red> Hit </color> {photonView.Owner.ActorNumber}'s Hp Is : {curHp}");

        if (curHp <= 0)
        {
            DiePlayer();
        }
    }

    private void DiePlayer()
    {
        DropWeapon();
        playerState = PlayerState.Dead;
        // 게임 메니저의 이벤트 버스 호출 필요
        // 인풋 메니저에게 콜백 필요
        DebugGameManager.Instance?.OnPlayerDied(this);

        if (hasEnemyFlag)
        {
            hasEnemyFlag = false;
            dummyFlagMesh.SetActive(false);
        }

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

        if (photonView.IsMine)
        {
            playerState = PlayerState.Stunned;
            stunCallback?.Invoke(false);
            yield return new WaitForSeconds(stunDuration);
            playerState = PlayerState.Idle;
            stunCallback?.Invoke(true);
        }
        else
        {
            playerState = PlayerState.Stunned;
            yield return new WaitForSeconds(stunDuration);
            playerState = PlayerState.Idle;
        }
    }

    #endregion

}
