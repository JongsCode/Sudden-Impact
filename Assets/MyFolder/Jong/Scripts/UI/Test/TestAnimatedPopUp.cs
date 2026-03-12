using System.Collections;
using UnityEngine;

public class TestAnimatedPopUp : MonoBehaviour
{
    [Header("닫기 애니메이션 재생 시간")]
    public float closeDelayTime = 0.5f; // 애니메이션 길이에 맞춰 조절하세요!

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animator != null)
        {
            animator.Play("Open");
        }
    }

    public void OnClickCloseButton()
    {
        if (animator != null)
        {
            animator.Play("Close");

            StartCoroutine(CloseRoutine());
        }
        else
        {
            gameObject.SetActive(false);
            PopUpManager.Instance.PanelOff();
        }
    }

    private IEnumerator CloseRoutine()
    {
        yield return new WaitForSeconds(closeDelayTime);

        gameObject.SetActive(false);

        if (PopUpManager.Instance != null)
        {
            PopUpManager.Instance.PanelOff();
        }
    }
}