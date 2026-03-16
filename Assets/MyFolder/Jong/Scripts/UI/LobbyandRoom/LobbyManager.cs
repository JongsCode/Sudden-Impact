using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby and Room Panel")]
    [SerializeField]
    private GameObject panelLobby;
    [SerializeField]
    private GameObject panelRoom;

    [Header("방 생성 UI")]
    [SerializeField]
    private TMP_InputField inputRoomName;
    [SerializeField]
    private TMP_InputField inputMaxCapacity;

    [Header("방 목록 UI")]
    [SerializeField]
    private Transform roomListParent;
    [SerializeField]
    private GameObject roomButtonPrefab;

    [Header("Flash Effect")]
    [SerializeField]
    private GameObject flashPivot;
    private RectTransform flashTr;
    [SerializeField]
    private RectTransform imageTarget;
    private bool isTargetFront = false;
   
    [SerializeField]
    private float rotateSpeed;

    private Dictionary<string, GameObject> roomDictionary = new Dictionary<string, GameObject>();

    // [CH 수정] Start() 연결 로직 개선
    // 기존 코드:
    //   if (!PhotonNetwork.IsConnected) { PhotonNetwork.ConnectUsingSettings(); }
    //
    // 문제: LoginManager.Awake()에서 이미 연결을 시작했기 때문에,
    //   1) 이미 연결 완료된 경우 → ConnectUsingSettings() 건너뛰고 OnConnectedToMaster도 안 불림
    //      → JoinLobby()가 영영 호출되지 않아 방 목록이 표시되지 않는 버그 발생
    //   2) 아직 연결 중인 경우 (Disconnected가 아님) → ConnectUsingSettings() 중복 호출 위험
    //
    // 수정: 3가지 상태를 분기 처리
    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            // 로그인 씬에서 이미 연결 완료 → OnConnectedToMaster는 재호출되지 않으므로 직접 JoinLobby
            PhotonNetwork.JoinLobby(new TypedLobby("ASIA", LobbyType.Default));
        }
        else if (PhotonNetwork.NetworkClientState == ClientState.Disconnected)
        {
            // 연결 시도가 전혀 없는 경우에만 Connect (디버그 씬 직접 진입 등 예외 상황 대비)
            PhotonNetwork.GameVersion = Application.version;
            PhotonNetwork.ConnectUsingSettings();
        }
        // else (Disconnected가 아님 = 연결 중): LoginManager에서 연결 중 → OnConnectedToMaster 콜백 대기
        flashTr = flashPivot.GetComponent<RectTransform>();
    }


    private void Update()
    {
        FlashAnimation();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(new TypedLobby("ASIA", LobbyType.Default));
    }

    public void OnCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        int maxCapacity = 8;
        int.TryParse(inputMaxCapacity.text, out maxCapacity);
        roomOptions.MaxPlayers = maxCapacity;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        PhotonNetwork.CreateRoom(inputRoomName.text, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공!, 방 이름 : " + PhotonNetwork.CurrentRoom.Name);
        panelLobby.SetActive(false);
        panelRoom.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> _roomList)
    {
        foreach(RoomInfo room in _roomList)
        {
            if(room.RemovedFromList)
            {
                if(roomDictionary.TryGetValue(room.Name, out GameObject _removeRoom))
                {
                    Destroy(_removeRoom);
                    roomDictionary.Remove(room.Name);
                }
            }
            else
            {
                if(!roomDictionary.ContainsKey(room.Name))
                {
                    GameObject newRoomBtn = Instantiate(roomButtonPrefab, roomListParent);
                    newRoomBtn.GetComponent<RoomButton>().SetData(room.Name, room.PlayerCount, room.MaxPlayers);
                    roomDictionary.Add(room.Name, newRoomBtn);
                }
                else
                {
                    roomDictionary[room.Name].GetComponent<RoomButton>().SetData(room.Name, room.PlayerCount, room.MaxPlayers);
                }
            }
        }
    }

    private void FlashAnimation()
    {
        float wave = Mathf.Sin(Time.time * rotateSpeed);

        float angle = 10f + (10f * wave);

        flashTr.localRotation = Quaternion.Euler(0f, angle, 0f);
        
        if(imageTarget != null)
        {
            if(angle >= 5 &&isTargetFront)
            {
                imageTarget.SetAsFirstSibling();
                isTargetFront = false;
            }
            else if (angle < 5 && !isTargetFront)
            {
                imageTarget.SetAsLastSibling();
                isTargetFront = true;
            }
        }
    }
}
