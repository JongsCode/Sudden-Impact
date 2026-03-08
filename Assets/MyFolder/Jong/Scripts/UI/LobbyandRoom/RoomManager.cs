using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby and Room Panel")]
    [SerializeField]
    private GameObject panelLobby;
    [SerializeField]
    private GameObject panelRoom;

    [Header("팀 UI 연결")]
    [SerializeField]
    private Transform team1Parent;
    [SerializeField]
    private Transform team2Parent;

    [Header("PlayerIcon 및 버튼")]
    [SerializeField]
    private GameObject playerIconPrefab;
    [SerializeField]
    private TextMeshProUGUI textReadyButton;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    public override void OnJoinedRoom()
    {
        int team1Count = 0;
        int team2Count = 0;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("Team"))
            {
                int team = (int)player.CustomProperties["Team"];
                if (team == 1)
                {
                    team1Count++;
                }
                else if (team == 2)
                {
                    team2Count++;
                }
            }
        }
        int myTeam = 1;
        if (team1Count > team2Count)
        {
            myTeam = 2;
        }

        Hashtable props = new Hashtable()
        {
            { "Team", myTeam },
            { "IsReady", false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        if (PhotonNetwork.IsMasterClient)
        {
            textReadyButton.text = "Wait";
            SetReadyState(true);
        }
        else
        {
            textReadyButton.text = "Ready";
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomUI();
        CheckAllReady();

    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomUI();
        CheckAllReady();
    }

    public void OnClickTeam1Button() => SetTeam(1);
    public void OnClickTeam2Button() => SetTeam(2);
    private void SetTeam(int _teamNumber)
    {
        Hashtable props = new Hashtable() { { "Team", _teamNumber } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void SetReadyButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (textReadyButton.text == "AllReady")
            {
                Debug.Log("모든 플레이어 준비 완료! 게임 씬으로 이동합니다!");
                PhotonNetwork.LoadLevel("FOVTest");
            }
        }
        else
        {
            if (textReadyButton.text == "Ready")
            {
                textReadyButton.text = "Wait Other Players";
                SetReadyState(true);
            }
            else
            {
                textReadyButton.text = "Ready";
                SetReadyState(false);
            }
        }
    }
    private void SetReadyState(bool _isReady)
    {
        Hashtable props = new Hashtable() { { "IsReady", _isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Team") || changedProps.ContainsKey("IsReady"))
        {
            UpdateRoomUI();
            CheckAllReady();
        }
    }

    private void UpdateRoomUI()
    {
        foreach (Transform child in team1Parent) Destroy(child.gameObject);
        foreach (Transform child in team2Parent) Destroy(child.gameObject);

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerTeam = 1;
            bool isReady = false;

            if (player.CustomProperties.ContainsKey("Team"))
            {
                playerTeam = (int)player.CustomProperties["Team"];
            }
            if (player.CustomProperties.ContainsKey("IsReady"))
            {
                isReady = (bool)player.CustomProperties["IsReady"];
            }

            Transform parent = (playerTeam == 1) ? team1Parent : team2Parent;

            GameObject playerIconGo = Instantiate(playerIconPrefab, parent);
            PlayerIcon playerIcon = playerIconGo.GetComponent<PlayerIcon>();
            playerIcon.SetUserInfo(player.NickName, player.ActorNumber, playerTeam);
            playerIcon.SetReady(isReady);
        }
    }

    private void CheckAllReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int readyCount = 0;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsMasterClient) continue;

            if (player.CustomProperties.ContainsKey("IsReady"))
            {
                if ((bool)player.CustomProperties["IsReady"] == true)
                {
                    readyCount++;
                }
            }
        }

        if (readyCount == (PhotonNetwork.CurrentRoom.PlayerCount - 1) && PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            textReadyButton.text = "AllReady";
        }
        else
        {
            textReadyButton.text = "Wait Other Players";
        }
    }
    public void SetLeaveButton()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

    }

    public override void OnLeftRoom()
    {
        panelLobby.SetActive(true);
        panelRoom.SetActive(false);
    }

}
