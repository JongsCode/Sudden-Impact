using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviourPun
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;

    [Header("Animator Hashes")]
    private readonly int hashVelocityX = Animator.StringToHash("VelocityX");
    private readonly int hashVelocityZ = Animator.StringToHash("VelocityZ");
    private readonly int hashWeaponType = Animator.StringToHash("WeaponType");

    private readonly int hashAction = Animator.StringToHash("Action");
    private readonly int hashThrow = Animator.StringToHash("Throw");
    private readonly int hashInteract = Animator.StringToHash("Interact");
    private readonly int hashRoll = Animator.StringToHash("Roll");
    private readonly int hashStun = Animator.StringToHash("Stun");
    private readonly int hashDead = Animator.StringToHash("Dead");

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        // 이벤트 구독 (컨트롤러에서 행동이 발생하면 자동으로 트리거 함수 실행)
        playerController.OnAttackEvent += TriggerAction;
        playerController.OnRollEvent += TriggerRoll;
        playerController.OnThrowEvent += TriggerThrow;
        playerController.OnInteractEvent += TriggerInteract;
        playerController.OnStunnedEvent += TriggerStun;

        // 초기화
        animator.SetLayerWeight(1, 1f);
    }

    private void OnDisable()
    {
        // 메모리 누수 방지 및 참조 에러 방지
        playerController.OnAttackEvent -= TriggerAction;
        playerController.OnRollEvent -= TriggerRoll;
        playerController.OnThrowEvent -= TriggerThrow;
        playerController.OnInteractEvent -= TriggerInteract;
        playerController.OnStunnedEvent -= TriggerStun;
    }

    private void LateUpdate()
    {
        // 사망 상태 체크 (Trigger 대신 Bool로 확실하게 눕혀놓기 위함)
        bool isDead = playerController.GetPlayerState == PlayerController.PlayerState.Dead;
        animator.SetBool(hashDead, isDead);

        if (isDead)
        {
            animator.SetLayerWeight(1, 0f);
            return;
        }

        UpdateMovementAnimations();
        UpdateWeaponState();
    }

    private void UpdateMovementAnimations()
    {
        // 마우스를 바라보는 회전축(로컬) 기준으로 물리 속도를 변환
        Vector3 localVelocity = transform.InverseTransformDirection(playerController.CurrentVelocity.normalized);

        // Blend Tree로 전달 (전후좌우 8방향 보간용)
        animator.SetFloat(hashVelocityX, localVelocity.x);
        animator.SetFloat(hashVelocityZ, localVelocity.z);
    }

    private void UpdateWeaponState()
    {
        // 기본값은 0 (칼/맨손)
        int currentWeaponType = 0;

        // 총을 들고 있고, 들고 있는 총 오브젝트가 확실히 존재한다면
        if (playerController.UseGun && playerController.MyEquippedGun != null)
        {
            // 총의 Enum 타입을 int로 형변환
            currentWeaponType = (int)playerController.MyEquippedGun.WeaponType;
        }

        // 애니메이터로 파라미터 전송 (0, 1, 2, 3, 4가 자동으로 들어감)
        animator.SetInteger(hashWeaponType, currentWeaponType);
    }

    private IEnumerator RollCoroutine()
    {
        animator.SetLayerWeight(1, 0f);
        animator.SetTrigger(hashRoll);
        yield return new WaitForSeconds(0.2f);
        animator.SetLayerWeight(1, 1f);
    }

    // --- 애니메이션 트리거 전송 ---
    private void TriggerAction() => animator.SetTrigger(hashAction);
    private void TriggerRoll() => StartCoroutine(RollCoroutine());
    private void TriggerThrow() => animator.SetTrigger(hashThrow);
    private void TriggerInteract() => animator.SetTrigger(hashInteract);
    private void TriggerStun() => animator.SetTrigger(hashStun);
}
