using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviourPun
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip rollClip;
    [SerializeField] private AudioClip stunClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip weaponSwapClip;
    [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.6f;

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

    private float lastFootstepTime = 0f;
    private bool _wasDead; // death sound once-flag

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
        GameEvents.OnWeaponChanged += HandleWeaponChanged;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지 및 참조 에러 방지
        playerController.OnAttackEvent -= TriggerAction;
        playerController.OnRollEvent -= TriggerRoll;
        playerController.OnThrowEvent -= TriggerThrow;
        playerController.OnInteractEvent -= TriggerInteract;
        playerController.OnStunnedEvent -= TriggerStun;
        GameEvents.OnWeaponChanged -= HandleWeaponChanged;
    }

    private void LateUpdate()
    {
        // 사망 상태 체크 (Trigger 대신 Bool로 확실하게 눕혀놓기 위함)
        bool isDead = playerController.GetPlayerState == PlayerController.PlayerState.Dead;
        animator.SetBool(hashDead, isDead);

        if (isDead && !_wasDead) PlayClip(deathClip);
        _wasDead = isDead;

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
    private void TriggerRoll()
    {
        StartCoroutine(RollCoroutine());
        PlayClip(rollClip);
    }
    private void TriggerThrow() => animator.SetTrigger(hashThrow);
    private void TriggerInteract() => animator.SetTrigger(hashInteract);

    // 에니메이션 이벤트 -- 에니메이션 클립에서 호출
    public void PlayFootstep()
    {
        // 방금 소리가 났다면(0.2초 이내) 무시 (탭댄스 방어)
        if (Time.time - lastFootstepTime < 0.2f) return;

        if (footstepClips == null || footstepClips.Length == 0) return;
        PlayClip(footstepClips[Random.Range(0, footstepClips.Length)], footstepVolume);

        lastFootstepTime = Time.time;

        Debug.Log($"[PlayerAnimator] Play Foot Step Sound");
    }

    private void HandleWeaponChanged(string _name, bool _isGun)
    {
        if (!photonView.IsMine) return;
        PlayClip(weaponSwapClip);
    }

    private void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
    private void TriggerStun()
    {
        animator.SetTrigger(hashStun);
        PlayClip(stunClip);
    }
}
