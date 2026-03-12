using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using System.Collections;

public class SignUpPopUp : PopUp
{
    
    [SerializeField]
    private TMP_InputField inputEmail;
    [SerializeField]
    private TMP_InputField inputPassword;
    [SerializeField]
    private TMP_InputField inputNickname;
    [SerializeField]
    private TextMeshProUGUI textErrorMessage;

    private float closeTime = 0.4f;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animator != null)
            animator.Play("Open");
    }
    public override void OnConfirm()
    {
        SignUp();
    }

    public override void OnCancel()
    {
        inputEmail.text = "";
        inputPassword.text = "";
        inputNickname.text = "";

        CloseAnimPanelOff(true);
    }

    private void SignUp()
    {
        if(string.IsNullOrEmpty(inputEmail.text) || string.IsNullOrEmpty(inputPassword.text) || string.IsNullOrEmpty(inputNickname.text))
        {
            Debug.Log("빈 칸을 채워주세요");
            PopUpManager.Instance.Show(PopUpType.ResultPopUp, "빈 칸을 채워주세요.");

            return;
        }
        

        // PlayFab 서버로 보낼 정보
        var request = new RegisterPlayFabUserRequest
        {
            Email = inputEmail.text,
            Password = inputPassword.text,
            Username = inputNickname.text,
            DisplayName = inputNickname.text
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, Success, Fail);
    }
 

    public void Success(RegisterPlayFabUserResult _result)
    {
        Debug.Log("회원가입 성공");
        // Photon Nickname을 설정하고 컴퓨터에 저장
        PhotonNetwork.NickName = inputNickname.text;
        PlayerPrefs.SetString("Nickname", PhotonNetwork.NickName);
        PlayerPrefs.Save();

        inputEmail.text = "";
        inputPassword.text = "";
        inputNickname.text = "";

        PopUpManager.Instance.Show(PopUpType.ResultPopUp, "회원가입 성공.");
        CloseAnimPanelOff(false);
    }

    public void Fail(PlayFabError _error)
    {
     
        string textError = "";
        switch (_error.Error)
        {
            case PlayFabErrorCode.InvalidParams:
                if (_error.ErrorDetails != null)
                {
                    if (_error.ErrorDetails.ContainsKey("Email"))
                    {
                        textError = "이메일 형식이<br>올바르지 않습니다.";
                        inputEmail.text = "";
                    }
                    else if (_error.ErrorDetails.ContainsKey("Password"))
                    {
                        textError = "비밀번호는 6자리 이상<br>입력해야 합니다.";
                        inputPassword.text = "";
                    }
                    else if (_error.ErrorDetails.ContainsKey("name"))
                    {
                        textError = "닉네임은 3~20자리 사이로<br>입력해야 합니다.";
                        inputNickname.text = "";
                    }
                    else
                        textError = "입력한 정보의 형식을<br>다시 확인해 주세요.";
                }
                else
                {
                    textError = "입력값을 다시<br>확인해 주세요.";
                }
                break;

            case PlayFabErrorCode.EmailAddressNotAvailable:
                textError = "이미 사용 중인<br>이메일입니다.";
                inputEmail.text = "";
                break;

            case PlayFabErrorCode.UsernameNotAvailable:
            case PlayFabErrorCode.NameNotAvailable:
                textError = "이미 사용 중인<br>닉네임입니다.";
                inputNickname.text = "";
                break;

            default:
                textError = "오류가 발생했습니다: " + _error.ErrorMessage;
                break;
        }
        textError = textError.Replace(". ", ".<br>");
        PopUpManager.Instance.Show(PopUpType.ResultPopUp, textError);
    }

    private void CloseAnimPanelOff(bool close)
    {
        if (animator != null)
        {
            animator.Play("Close");
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
