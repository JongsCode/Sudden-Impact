using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private PlayerRegistry playerRegistry;

    public override void OnJoinedRoom()
    {
        int team = PhotonNetwork.LocalPlayer.ActorNumber % 2; // 0 또는 1
        var go = PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);

        go.SetActive(false); // 경기 시작 전 비활성

                             // AllBuffered로 내 viewID랑 팀을 전파
                             // 나중에 들어오는 플레이어도 기존 플레이어 정보를 받을 수 있음
        photonView.RPC(nameof(RegisterToRegistry), RpcTarget.AllBuffered,
                       go.GetComponent<PhotonView>().ViewID, team);
    }

    [PunRPC]
    private void RegisterToRegistry(int _viewID, int _team)
    {
        PhotonView pv = PhotonView.Find(_viewID);
        if (pv == null) return;

        PlayerController player = pv.GetComponent<PlayerController>();
        playerRegistry.RegisterPlayerTeam(player, _team);

        if (player.photonView.IsMine)
        {
            playerRegistry.RegisterLocalPlayer(player); // 로컬 플레이어 주입
            playerRegistry.RegisterMyTeam(_team);

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();

            props["team"] = _team;
            props["viewID"] = _viewID;

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }
}
