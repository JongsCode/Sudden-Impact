using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Photon.Pun;
using System.Collections.Generic;

public class PlayerController : MonoBehaviourPun, IAttackReceiver
{
    [SerializeField] private Rigidbody myRigidbody;
    [SerializeField] private PhotonTransformView myTransformView;
    [SerializeField] private Transform weaponAttachPoint;
    [SerializeField] private TestWeapon myKnife;

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
    [SerializeField] private TestWeapon closestGun;
    [SerializeField] private TestWeapon myEquippedGun;
    [SerializeField] private bool useGun;

    private Coroutine curCheakClosestWeaponCoroutine;
    private List<TestWeapon> nearbyItems = new List<TestWeapon>();

    public enum PlayerState
    {
        NotReady, Idel, Sprint, Rolling, Stunned, Dead
    }
    private PlayerState playerState;

    private void OnEnable()
    {
        curHp = maxHp;
        SetPlayerState(PlayerState.Idel);
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
        if (playerState == PlayerState.Rolling) return;
        if (playerState == PlayerState.Stunned) return;
        if (playerState == PlayerState.Dead) return;

        Vector3 moveVector = transform.position + ((_moveAxis * moveSpeed) * Time.deltaTime);
        myRigidbody.MovePosition(moveVector);
    }

    public void RotatePlayer(float _moveAngle)
    {
        Quaternion moveDir = Quaternion.Euler(0f, _moveAngle, 0f);
        myRigidbody.rotation = moveDir;
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

    public void TryAttack(Vector2 _mousePos)
    {

        if (useGun)
        {
        Debug.Log("[PlayerController] Im Start Fire");
        }

        else
        {
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

        TestWeapon weapon;
        if(other.TryGetComponent<TestWeapon>(out weapon))
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

        TestWeapon weapon;
        if (other.TryGetComponent<TestWeapon>(out weapon))
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
        while (nearbyItems.Count > 0)
        {
            yield return new WaitForSeconds(5f / 60f); // 5프레임마다 

            foreach (var weapon in nearbyItems)
            {
                float newItemDis = Vector3.Distance(transform.position,weapon.transform.position);
                float curItemDis = Vector3.Distance(transform.position,closestGun.transform.position);

                if (newItemDis < curItemDis)
                {
                    closestGun = weapon;
                }
            }
        }
    }

    [PunRPC]
    public void PickUpItem(int _viewID)
    {
        if(!photonView.IsMine)
        {
            closestGun = PhotonView.Find(_viewID).GetComponent<TestWeapon>();
        }
        myEquippedGun = closestGun;

        myEquippedGun.transform.SetParent(weaponAttachPoint);
        myEquippedGun.transform.localPosition = Vector3.zero;
        myEquippedGun.transform.localRotation = Quaternion.identity;

        useGun = true;

        SwapWeapon(useGun);

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
        //Debug.Log($"[PlayerController] Receive Trying");

        // 상태 검사
        if (curHp <= 0 || 
            playerState == PlayerState.NotReady || 
            playerState == PlayerState.Dead || 
            playerState == PlayerState.Rolling) { return; }

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
        // 게임 메니저의 이벤트 버스 호출 필요
        // 인풋 메니저에게 콜백 필요
        DebugGameManager.Instance?.OnPlayerDied(this);

        myEquippedGun.transform.SetParent(null);
        myEquippedGun = null;

        this.gameObject.SetActive(false);
    }

    private void StunPlayer()
    {
        photonView.RPC(nameof(StunRPC), photonView.Owner);
    }

    [PunRPC]
    private void StunRPC()
    {
        StartCoroutine(StunCoroutine());
    }

    private IEnumerator StunCoroutine()
    {
        Debug.Log($"[PlayerController] {photonView.Owner.ActorNumber} is Stuned");
        playerState = PlayerState.Stunned;
        yield return new WaitForSeconds(stunDuration);
        playerState = PlayerState.Idel;
    }

    #endregion

}
