using UnityEngine;
using TMPro;
using Photon.Pun;
public class RoomButton : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI inputRoomTitle;
    
    private string roomName;

    public void SetData(string _roomName, int _playerCount, int _capacity)
    {
        roomName = _roomName;
        inputRoomTitle.text = string.Format("{0} ( {1} / {2} )", _roomName, _playerCount, _capacity);
    }

    public void OnEnterRoom() // RoomButton 클릭
    {
        PhotonNetwork.JoinRoom(roomName);
    }
}
