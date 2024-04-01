using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class RoomItem : MonoBehaviour
{
    public TextMeshProUGUI roomName;

    public RoomInfo roomInfo { get; private set; }

    public void SetRoomName(RoomInfo _roomInfo)
    {
        roomInfo = _roomInfo;
        roomName.text = roomInfo.Name;
    }

    public void OnClickRoomItem()
    {
        PhotonNetwork.JoinRoom(roomName.text);
    }
}
