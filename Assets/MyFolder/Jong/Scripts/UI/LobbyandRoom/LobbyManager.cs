using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

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

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            PhotonNetwork.JoinLobby();
        }
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
