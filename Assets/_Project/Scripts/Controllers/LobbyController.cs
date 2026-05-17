using Photon.Pun;
using MenuSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;
using MenuSystem.Pages;
using Debug_Log;
using Unity.VisualScripting;

public class LobbyController : MonoBehaviourPunCallbacks
{
    public static LobbyController instance;

    //[Header("Pages")]
    //[SerializeField] GameObject pageTitle;
    //[SerializeField] GameObject pageLobby;
    //[SerializeField] GameObject pageRoom;

    [Space(10)]
    [Header("Title Setup")]
    public TMP_InputField inputNickName;
    public Button btnConnect;
    public TextMeshProUGUI btnTextConnect;

    [Space(10)]
    [Header("Lobby Setup")]
    public TMP_InputField inputRoomName;
    public Button btnCreateRoom;
    public TextMeshProUGUI playerNicknameText;    

    [Space(10)]
    [Header("Lobby RoomList")]
    public RoomItem roomItemPrefab;
    [SerializeField] List<RoomItem> roomItemsList = new List<RoomItem>();
    public Transform roomListContent;
    float timeBetweenUpdates = 5f;
    float nextUpdateTime;

    [Space(10)]
    [Header("Room Setup")]
    public TextMeshProUGUI roomName;
    public Button btnPlay;    

    private DebugLog DebugLog;

    public override void OnEnable()
    {
        if (instance == null) return;
        
        PhotonController.OnJoinedLobbyEvent.AddListener(JoinedLobby);
        PhotonController.OnJoinedRoomEvent.AddListener(JoinedRoom);        
    }

    public override void OnDisable()
    {
        if (instance == null) return;
        
        PhotonController.OnJoinedLobbyEvent.RemoveListener(JoinedLobby);
        PhotonController.OnJoinedRoomEvent.RemoveListener(JoinedRoom);        
    }

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;            
        }

        instance = this;

        DebugLog = DebugLog.instance;

        PageController.instance.OpenPage("title");

        if(PhotonNetwork.CountOfPlayers > 20)
        {
            btnConnect.interactable = false;
            DebugLog.SetDebugLog("Servidor lotado, tente mais tarde");
        }
    }    

    #region OnClicks
    public void OnClickConnect()
    {
        if (inputNickName.text.Length >= 1)
        {
            btnTextConnect.text = "Conectando...";

            string _nickName = inputNickName.text;

            PhotonController.instance.ConnectToServer(_nickName);
        }
    }

    public void OnClickCreateRoom()
    {
        if (!string.IsNullOrEmpty(inputRoomName.text))
        {
            string _roomName = inputRoomName.text;

            PhotonController.instance.JoinOrCreateRoom(_roomName);
        }
        else
        {
            DebugLog.SetDebugLog("Precisa de no mínimo 1 caractere");
        }
    }
    #endregion

    #region On Events Call    
    public void JoinedLobby()
    {
        playerNicknameText.text = PhotonNetwork.LocalPlayer.NickName;

        instance.btnPlay.interactable = false;        

        PageController.instance.OpenPage("lobby");        
    }

    public void JoinedRoom()
    {
        PageController.instance.OpenPage("room");
        roomName.text = PhotonNetwork.CurrentRoom.Name;
        DestroyRoomItem();
        roomItemsList.Clear();
    }    
    #endregion

    #region List Rooms
    //public override void OnRoomListUpdate(List<RoomInfo> roomList)
    //{
    //    UpdateRoomList(roomList);
    //}

    public void DestroyRoomItem()
    {
        foreach (Transform child in roomListContent)
        {
            if(child != null)
            {
                Destroy(child.gameObject);
            }            
        }        
    }

    public void UpdateRoomList(List<RoomInfo> roomList)
    {
        int countRooms = PhotonNetwork.CountOfRooms;
        
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                int index = roomItemsList.FindIndex(x => x.roomInfo.Name == room.Name);
                if (index != -1)
                {
                    Destroy(roomItemsList[index].gameObject);
                    roomItemsList.RemoveAt(index);
                }
            }
            else
            {
                int index = roomItemsList.FindIndex(x => x.roomInfo.Name == room.Name);
                if (index == -1)
                {
                    RoomItem newRoom = Instantiate(roomItemPrefab, roomListContent);
                    if (newRoom != null)
                    {
                        newRoom.SetRoomName(room);
                        roomItemsList.Add(newRoom);
                    }
                }                    
            }
        }
    }
    #endregion

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnClickLeaveRoom()
    {
        Debug.Log($"Você saiu da sala chamada {PhotonNetwork.CurrentRoom.Name}");
        LeaveRoom();
    }

    public void OnClickPlay()
    {
        btnPlay.interactable = false;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("MazeX");
        }
    }

    

    public override void OnLeftRoom()
    {
        PageController.instance.OpenPage("lobby");
        DestroyRoomItem();
    }
}
