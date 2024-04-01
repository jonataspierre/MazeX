using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class LobbyController : MonoBehaviourPunCallbacks
{
    //public static LobbyController instance;

    [Header("Pages")]
    [SerializeField] GameObject pageTitle;
    [SerializeField] GameObject pageLobby;
    [SerializeField] GameObject pageRoom;

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

    [SerializeField] int MaxPlayers = 15;

    private void Awake()
    {
        //if(instance == null)
        //{
        //    instance = this;
        //}
        //else
        //{
        //    Destroy(gameObject);
        //}
        //GetCurrentPlayers();
    }    

    #region Title
    public void OnClickConnect()
    {
        if (inputNickName.text.Length >= 1)
        {
            btnTextConnect.text = "Conectando...";

            string _nickName = inputNickName.text;

            PhotonController.instance.ConnectToServer(_nickName);
        }
    }

    public override void OnConnectedToMaster()
    {        
        pageTitle.SetActive(false);
        pageLobby.SetActive(true);
    }
    #endregion

    #region Lobby
    public override void OnJoinedLobby()
    {
        playerNicknameText.text = PhotonNetwork.LocalPlayer.NickName;
    }

    public void OnClickCreateRoom()
    {
        if(inputRoomName.text.Length >= 1)
        {
            string _roomName = inputRoomName.text;

            Debug.Log($"Entrando na room ${_roomName}");
            if (!PhotonNetwork.InLobby)
            {
                Debug.LogWarning($"Falha ao entrar na room {_roomName}, pois o usuário não está em um Looby");
                return;
            }

            RoomOptions roomOptions = new RoomOptions();
            //roomOptions.IsOpen = true;
            //roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = (byte)MaxPlayers;
            //roomOptions.PublishUserId = true;

            PhotonNetwork.JoinOrCreateRoom(_roomName, roomOptions, TypedLobby.Default);
        }
        else
        {
            Debug.Log("Precisa ter mais de 3 caracteres");
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Criou a sala {PhotonNetwork.CurrentRoom.Name}");        
    }
    #endregion

    #region List Rooms
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //if (Time.time >= nextUpdateTime)
        //{
        //    UpdateRoomList(roomList);
        //    nextUpdateTime = Time.time + timeBetweenUpdates;
        //}

        //DestroyRoomItem();

        UpdateRoomList(roomList);
    }

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

    void UpdateRoomList(List<RoomInfo> roomList)
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

    public override void OnJoinedRoom()
    {
        Debug.Log($"Você entrou em uma sala chamada {PhotonNetwork.CurrentRoom.Name}");
        pageLobby.SetActive(false);
        pageRoom.SetActive(true);
        roomName.text = "Sala\n" + PhotonNetwork.CurrentRoom.Name;
        DestroyRoomItem();
        roomItemsList.Clear();
    }

    public override void OnLeftRoom()
    {        
        pageRoom.SetActive(false);
        pageLobby.SetActive(true);
        DestroyRoomItem();
    }
}
