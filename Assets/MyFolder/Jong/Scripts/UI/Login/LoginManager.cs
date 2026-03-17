using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using TMPro;
public class LoginManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputEmail;
    [SerializeField]
    private TMP_InputField inputPassword;

    // [CH 추가] Awake에서 Photon 연결을 미리 시작
    // 기존에는 LobbyManager.Start()에서 연결했기 때문에,
    // 빌드 2개를 동시에 실행하면 두 인스턴스가 같은 타이밍에 ConnectUsingSettings()를 호출 →
    // Photon DNS/TLS 초기화가 메인 스레드를 블로킹하면서 두 창이 동시에 "응답 없음" 발생.
    // 로그인 화면에서 미리 연결을 시작하면, 사용자가 ID/PW를 입력하는 동안 백그라운드에서
    // 연결이 완료되어 로비 진입 시 ConnectUsingSettings() 재호출이 불필요해짐.
    private void Awake()
    {
        PhotonNetwork.GameVersion = Application.version;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";
        PhotonNetwork.AutomaticallySyncScene = false;
        // NetworkClientState == Disconnected 일 때만 Connect (연결 중/완료 상태에서 중복 호출 방지)
        if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void OnLogin() // 로그인 버튼
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = inputEmail.text,
            Password = inputPassword.text
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, Success, Fail);
    }

    public void DisplaySignUp() // 회원가입 버튼
    {
        PopUpManager.Instance.Show(PopUpType.SignUpPopUp);
    }

    public void Success(LoginResult _loginResult)
    {
        // [CH 수정] 아래 두 줄은 Awake()로 이동 (로그인 전에 미리 설정되어야 하므로)
        // PhotonNetwork.AutomaticallySyncScene = false;
        // PhotonNetwork.GameVersion = Application.version;

        var request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request, SuccessInfo, FailInfo);
        Debug.LogError("로그인 성공 -> 계정 정보 가져오기");
    }
    public void SuccessInfo(GetAccountInfoResult _result)
    {
        string nickname = _result.AccountInfo.TitleInfo.DisplayName;

        if(string.IsNullOrEmpty(nickname))
        {
            // 닉네임이 없는 경우 로그인 씬으로 이동
            UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
            Debug.LogError("닉네임 설정 실패 -> 로그인 씬으로 이동");
        }
        else
        {
            PhotonNetwork.NickName = nickname;
            Debug.LogError("Photon Nickname 설정 완료");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }
    public void Fail(PlayFabError _error)
    {
        Debug.LogError("로그인 실패 : " + _error.GenerateErrorReport());
        string textError = "";
        switch (_error.Error)
        {
            case PlayFabErrorCode.InvalidParams:
                if (_error.ErrorDetails != null)
                {
                    if (_error.ErrorDetails.ContainsKey("Email"))
                        textError = "이메일 형식이<br>올바르지 않습니다.";
                    else if (_error.ErrorDetails.ContainsKey("Password"))
                        textError = "비밀번호는 6자리 이상<br>입력해야 합니다.";
                    else
                        textError = "입력한 정보의 형식을<br>다시 확인해 주세요.";
                }
                else
                {
                    textError = "입력값을 다시<br>확인해 주세요.";
                }
                break;

            case PlayFabErrorCode.InvalidEmailOrPassword:
                textError = "이메일이나 비밀번호가<br>일치하지 않습니다.";
                break;

            case PlayFabErrorCode.AccountNotFound:
                textError = "가입되지 않은 계정입니다.회원가입을 진행해 주세요.";
                break;

            case PlayFabErrorCode.EmailAddressNotAvailable:
                textError = "이미 사용 중인<br>이메일입니다.";
                break;

            default:
                textError = "오류가 발생했습니다: " + _error.ErrorMessage;
                break;
        }
        textError = textError.Replace(". ", ".\n");
        PopUpManager.Instance.Show(PopUpType.ResultPopUp, textError);
    }

    
    public void FailInfo(PlayFabError _error)
    {
        Debug.LogError("정보 가져오기 실패 : " + _error.GenerateErrorReport());
    }
}
