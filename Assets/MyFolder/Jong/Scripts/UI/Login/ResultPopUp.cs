using TMPro;
using UnityEngine;
using System.Collections;

public class ResultPopUp : PopUp
{
    [SerializeField]
    private TextMeshProUGUI textResult;

    private float closeTime = 0.3f;
    private Animator animator;


    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        animator.Play("ResultPopUpOpen");
    }
    public override void OnCancel()
    {
        textResult.text = "";
        CloseAnimPanelOff(true);
    }

    public override void OnConfirm()
    {

    }

    public void SetResultText(string _text)
    {
        textResult.text = _text;
    }

    private void CloseAnimPanelOff(bool close)
    {
        if (animator != null)
        {
            animator.Play("ResultPopUpClose");
            StartCoroutine(CloseRoutine(close));
        }
        else
        {
            if (close) PopUpManager.Instance.PanelOff();
            gameObject.SetActive(false);
        }
    }

    private IEnumerator CloseRoutine(bool _close)
    {
        yield return new WaitForSeconds(closeTime);

        if (_close) PopUpManager.Instance.PanelOff();
        gameObject.SetActive(false);
    }
}
