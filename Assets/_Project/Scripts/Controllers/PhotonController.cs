using System.Collections;
using System.Collections.Generic;
using System;
using Debug_Log;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.Events;
using MenuSystem.Pages;

public class PhotonController : MonoBehaviourPunCallbacks
{
    public static PhotonController instance;

    [Header("Setup")]
    //[SerializeField] string defaultRoomName = "Room";
    [Range(1, 15)]
    [SerializeField] int MaxPlayers = 15;
    //[SerializeField] GameObject _playerPrefab;
    //public string LastLoadedRoom { get; set; }
    //public string TargetRoomToLoad { get; private set; }

    private bool _isLoading;
    
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnLoadingChangedEvent?.Invoke(_isLoading);
        }
    }    

    [field: SerializeField] public bool IsConnected { get ; private set; }
    [field: SerializeField] public bool InLobby { get; private set; }    
    //public GameObject localPlayer;

    //[Header("Testing")]
    //[SerializeField] bool spawnPlayerForTesting;
    //[SerializeField] Transform spawnPointForTesting;

    
    public static UnityEvent<bool> OnLoadingChangedEvent = new UnityEvent<bool>();
    public static UnityEvent<int> OnConnectedChangedEvent = new UnityEvent<int>();
    public static UnityEvent OnJoinedLobbyEvent = new UnityEvent();
    public static UnityEvent OnJoinedRoomEvent = new UnityEvent();

    private DebugLog DebugLog;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DebugLog = DebugLog.instance;

        IsConnected = false;
        InLobby = false;
    }

    private void Start()
    {
        //_nickName = PlayFabController.Instance.PlayerInfo.Nickname;
        //_nickName = $"nickNameTeste_{Guid.NewGuid().ToString()}";

        //if (SceneController.Instance == null)
        //{
        //    Debug.LogWarning("Missing Scene Controller");
        //}

        //if (spawnPlayerForTesting)
        //{
        //    SceneController.Instance.ToggleSceneLoading(false);
        //    SpawnOfflinePlayerForTesting();
        //}
        //else
        //{
        //    ConnectToServer();
        //}

        //ConnectToServer();
    }


    [ContextMenu("Connect To Server")]
    public void ConnectToServer(string _nickName)
    {
        //_nickName = $"nickNameTeste_{Guid.NewGuid().ToString()}";

        DebugLog.SetDebugLog($"Conectando no Photon Master");

        IsLoading = true;

        //ToggleMessageQueue(false);

        PhotonNetwork.AuthValues = new AuthenticationValues(_nickName);
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = _nickName;

        //PhotonNetwork.SendRate = 35;
        //PhotonNetwork.SerializationRate = 12;

        PhotonNetwork.PhotonServerSettings.AppSettings.EnableLobbyStatistics = true;

        PhotonNetwork.ConnectUsingSettings();
    }

    public void JoinOrCreateRoom(string roomName)
    {
        DebugLog.SetDebugLog($"Criando e Entrando na Sala '{roomName}'");
        
        if (!PhotonNetwork.InLobby)
        {
            DebugLog.SetDebugLog($"Falha ao criar/entrar na sala '{roomName}', pois o usuário não está em um Looby");
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = (byte)MaxPlayers;
        roomOptions.PublishUserId = true;

        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    //public void SwitchToRegion(string targetRegion)
    //{
    //    string targetRoomName = $"{defaultRoomName}_{targetRegion}";
    //    TargetRoomToLoad = targetRoomName;

    //    //SceneController.Instance.ToggleSceneLoading(true);
    //    //SceneController.Instance.ClearRegionScenes();

    //    if (LastLoadedRoom != null)
    //    {
    //        LeaveCurrentRoom();
    //    }
    //    else
    //    {
    //        JoinOrCreateRoom(TargetRoomToLoad);
    //    }

    //}

    //[ContextMenu("Leave Current Room")]
    //public void LeaveCurrentRoom()
    //{
    //    if (PhotonNetwork.CurrentRoom != null)
    //    {
    //        PhotonNetwork.LeaveRoom();
    //    }
    //}

    ////public void SpawnPlayer(Vector3 position, EFacingDirection facingDirection)
    ////{
    ////    // Calculate the proper facing direction for the player
    ////    Quaternion rotation = facingDirection == EFacingDirection.Right
    ////        ? _playerPrefab.transform.rotation
    ////        : Quaternion.AngleAxis(180, Vector3.up) * _playerPrefab.transform.rotation;

    ////    // @TODO: use CheckSpawnLocation() to avoid spawning players on top of each others
    ////    localPlayer = PhotonNetwork.Instantiate(_playerPrefab.name, position, rotation, 0);
    ////    PhotonNetwork.NickName = PlayFabController.Instance.PlayerInfo.Nickname;

    ////    Debug.Log("Spawning Player at " + position.ToString());

    ////    if (PlayFabController.Instance.isNewGame)
    ////    {
    ////        PlayFabController.Instance.isNewGame = false;
    ////        DialogueUIController.Instance.LoadingDialogModal(DialogueUIController.Instance.npcInitial, 0, true);
    ////        GameStateController.Instance.SetUniqueState("MQ-1-1-DIALOGUE", 0);
    ////    }
    ////}

    //public void SpawnOfflinePlayerForTesting()
    //{
    //    Vector3 pos = spawnPointForTesting != null ? spawnPointForTesting.position : new Vector3(0,0,0) ;
    //    Instantiate(_playerPrefab, pos, _playerPrefab.transform.rotation);
    //}

    //public void DestroyPlayer()
    //{
    //    Debug.Log("Destroying Player");

    //    if (localPlayer != null)
    //    {
    //        PhotonNetwork.Destroy(localPlayer);
    //    }
    //}

    //public void ToggleMessageQueue(bool value)
    //{
    //    if (PhotonNetwork.LocalPlayer.IsLocal && !PhotonNetwork.IsMasterClient)
    //    {
    //        Debug.Log("MESSAGE QUEUE TURNING:" + value);
    //        PhotonNetwork.IsMessageQueueRunning = value;
    //    }
    //}

    #region Photon Chamadas
    public override void OnConnectedToMaster()
    {
        DebugLog.SetDebugLog("Você conectou ao Photon Master Server!");

        IsConnected = true;

        PhotonNetwork.JoinLobby();

        int countPlayers = PhotonNetwork.CountOfPlayers;

        OnConnectedChangedEvent?.Invoke(countPlayers);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        DebugLog.SetDebugLog("Você foi desconectado do Photon! Motivo: " + cause);

        //if(SceneController.Instance != null)
        //{
        //    SceneController.Instance.ToggleShowLoadingObjectsScene(ELoadingScreens.Failed);
        //}

        IsConnected = false;
    }

    public override void OnJoinedLobby()
    {
        DebugLog.SetDebugLog("Você entrou no Photon Lobby!");
        InLobby = true;

        OnJoinedLobbyEvent?.Invoke();        
    }

    public override void OnLeftLobby()
    {
        DebugLog.SetDebugLog("Você deixou o Lobby!");
        InLobby = false;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        LobbyController.instance.UpdateRoomList(roomList);
    }

    public override void OnCreatedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            DebugLog.SetDebugLog("Você é o dono da sala");
            LobbyController.instance.btnPlay.interactable = true;
        }        
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        DebugLog.SetDebugLog($"Houve um erro ao criar a sala, {message}");
    }

    public override void OnJoinedRoom()
    {
        DebugLog.SetDebugLog($"Você entrou na sala chamada '{PhotonNetwork.CurrentRoom.Name}'");

        IsLoading = false;        

        OnJoinedRoomEvent?.Invoke();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        DebugLog.SetDebugLog($"Houve um erro ao entrar na sala, {message}");
    }

    public override void OnLeftRoom()
    {
        DebugLog.SetDebugLog($"Você saiu da sala");

        IsLoading = true;

        LobbyController.instance.btnPlay.interactable = false;

        // @TODO: destroy player obj for other if he was in a room already
        //DestroyPlayer();
    }

    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{
    //    Debug.Log($"{newPlayer.UserId} entrou na sala");
    //}

    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    Debug.Log($"{otherPlayer.UserId} saiu da sala");
    //}

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        DebugLog.SetDebugLog($"Novo Master é {newMasterClient.UserId}");

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InLobby)
        {
            DebugLog.SetDebugLog("Você é o Dono da sala");
            LobbyController.instance.btnPlay.interactable = true;
        }
    }

    public void LoadMaze()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            //SceneController.instance.ClearLobbyScene();
            PhotonNetwork.LoadLevel("MazeX_new");            
            //SceneController.instance.LoadSceneAdditively("MazeX_new", true);
        }
    }

    


    #endregion

    //#region Helpers
    //private bool CheckSpawnLocation(Vector3 pos, float radius = 1)
    //{
    //    Collider[] colliders = null;
    //    Physics.OverlapSphereNonAlloc(pos, radius, colliders, LayerMask.GetMask("Default"));

    //    foreach (Collider collider in colliders)
    //    {
    //        GameObject go = collider.gameObject;

    //        if (go.transform.CompareTag("Player"))
    //        {
    //            return false;
    //        }
    //    }

    //    return true;
    //}
    //#endregion

}
