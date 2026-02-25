using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject FOVPrefab;
    [SerializeField] private PlayerRegistry playerRegistry;
    [SerializeField] private int layerNumber;


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
            player.gameObject.layer = layerNumber;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();

            props["team"] = _team;
            props["viewID"] = _viewID;

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        }
            SetPlayerInfo(player,_team);

        int actualLocalTeam = PhotonNetwork.LocalPlayer.ActorNumber % 2;

        if (_team == actualLocalTeam)
        {
            // FOV는 네트워크 동기화가 필요 없는 순수 로컬 시각 효과이므로 일반 Instantiate 사용
            // 단, 이미 FOV가 달려있는지 중복 생성 방지 체크를 권장합니다.
            if (player.transform.Find(FOVPrefab.name) == null)
            {
                GameObject fov = Instantiate(FOVPrefab, player.transform);
                fov.name = FOVPrefab.name; // 이름 맞춰주기 (중복 체크용)
            }
        }
    }

    private void SetPlayerInfo(PlayerController _player, int _team)
    {
        _player.Init(_team);
    }
}
