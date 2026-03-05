using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpPopUp : PopUp
{
    
    [SerializeField]
    private TMP_InputField inputEmail;
    [SerializeField]
    private TMP_InputField inputPassword;
    [SerializeField]
    private TMP_InputField inputNickname;
    public override void OnConfirm()
    {
        SignUp();
    }

    public override void OnCancel()
    {
        inputEmail.text = "";
        inputPassword.text = "";
        inputNickname.text = "";

        PopUpManager.Instance.PanelOff();
        gameObject.SetActive(false);
    }

    private void SignUp()
    {
        if(string.IsNullOrEmpty(inputEmail.text) || string.IsNullOrEmpty(inputPassword.text) || string.IsNullOrEmpty(inputNickname.text))
        {
            Debug.Log("빈 칸을 채워주세요");
            return;
        }
        // Photon Nickname을 설정하고 컴퓨터에 저장
        PhotonNetwork.NickName = inputNickname.text;
        PlayerPrefs.SetString("Nickname", PhotonNetwork.NickName);
        PlayerPrefs.Save();

        // PlayFab 서버로 보낼 정보
        var request = new RegisterPlayFabUserRequest
        {
            Email = inputEmail.text,
            Password = inputPassword.text,
            Username = inputNickname.text,
            DisplayName = inputNickname.text
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, Success, Fail);

        inputEmail.text = "";
        inputPassword.text = "";
        inputNickname.text = "";

        PopUpManager.Instance.PanelOff();
        gameObject.SetActive(false);
    }

    public void Success(RegisterPlayFabUserResult _result)
    {
        Debug.Log("회원가입 성공");
    }

    public void Fail(PlayFabError _error)
    {
        Debug.Log("회원가입 실패" + _error.GenerateErrorReport());
    }
   
}
