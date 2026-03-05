using Photon.Pun;
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
        PopUpManager.Instance.Show(PopUpType.SignUp);
    }

    public void Success(LoginResult _loginResult)
    {
        // 로그인에 성공했으니, 서버에 피로한 정보를 가져오기 위해 자동 씬 전환은 false로 설정
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = Application.version;

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
            PhotonNetwork.LoadLevel("Login");
            Debug.LogError("닉네임 설정 실패 -> 로그인 씬으로 이동");
        }
        else
        {
            PhotonNetwork.NickName = nickname;
            Debug.LogError("Photon Nickname 설정 완료");
            PhotonNetwork.LoadLevel("Lobby");
        }
    }
    public void Fail(PlayFabError _error)
    {
        Debug.LogError("로그인 실패 : " + _error.GenerateErrorReport());
    }

    
    public void FailInfo(PlayFabError _error)
    {
        Debug.LogError("정보 가져오기 실패 : " + _error.GenerateErrorReport());
    }
}
