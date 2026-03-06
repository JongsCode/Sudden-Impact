using TMPro;
using UnityEngine;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textNickname;
    [SerializeField]
    private TextMeshProUGUI textTeamNumber;
    [SerializeField]
    private GameObject goReady;

    private int userActorNumber;
    public int UserActorNumber
    {
        get { return userActorNumber; }
    }
    private int userTeamNumber;
    public int UserTeamNumber
    {
        get { return userTeamNumber; }
    }

    public void SetUserInfo(string _nickName, int _actorNumber, int _teamNumber)
    {
        textNickname.text = _nickName;
        userActorNumber = _actorNumber;
        userTeamNumber = _teamNumber;
        textTeamNumber.text = _teamNumber.ToString();
    }

    public void SetReady(bool _isReady)
    {
        goReady.SetActive(_isReady);
    }

}
