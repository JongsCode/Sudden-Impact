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

    [Header("Parameters")]
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sprintSpeed = 2f;
    [SerializeField] private float rollDistance = 2.0f;
    [SerializeField] private float rollDuration = 2.0f;
    [SerializeField] private float pickUpDistance = 1f;

    [Header("ForDebug")]
    [SerializeField] private float curHp;

    [SerializeField] private TestWeapon closestWeapon;
    [SerializeField] private TestWeapon myEquippedWeapon;

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

    public void Fire(Vector2 _mousePos)
    {
        Debug.Log("[PlayerController] Im Start Sprinting");
                
    }

    #region 던지기 , 줍기

    public void PickUpAndDrop(InputAction.CallbackContext ctx)
    {
        Debug.Log("[PlayerController] Im Droping");

        if (!closestWeapon) return;
        photonView.RPC(nameof(PickUpItem), RpcTarget.All ,closestWeapon.photonView.ViewID);


    }

    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        TestWeapon weapon;
        if(other.TryGetComponent<TestWeapon>(out weapon))
        {
            if (other.CompareTag("EquippedWeapon")) return;
            nearbyItems.Add(weapon);

            if (closestWeapon == null)
            {
                closestWeapon = weapon;
                StartCoroutine(CheckClosestWeapon());
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
                StopCoroutine(CheckClosestWeapon());
                closestWeapon = null;
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
                float curItemDis = Vector3.Distance(transform.position,closestWeapon.transform.position);

                if (newItemDis < curItemDis)
                {
                    closestWeapon = weapon;
                }
            }
        }
    }

    [PunRPC]
    public void PickUpItem(int _viewID)
    {
        if(!photonView.IsMine)
        {
            closestWeapon = PhotonView.Find(_viewID).GetComponent<TestWeapon>();
        }
        myEquippedWeapon = closestWeapon;

        closestWeapon.transform.SetParent(weaponAttachPoint);
        closestWeapon.transform.localPosition = Vector3.zero;
        closestWeapon.transform.localRotation = Quaternion.identity;

    }

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
    }

    #endregion

}
