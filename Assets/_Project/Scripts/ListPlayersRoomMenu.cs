using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListPlayersRoomMenu : MonoBehaviourPunCallbacks
{
    [Space(10)]
    [Header("Room PlayerList")]
    public PlayerItem playerItemPrefab;
    [SerializeField] List<PlayerItem> playerItemsList = new List<PlayerItem>();
    public Transform playerListContent;

    public override void OnEnable()
    {
        base.OnEnable();        
        GetCurrentPlayers();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        for(int  i = 0; i < playerItemsList.Count; i++)
        {
            Destroy(playerItemsList[i].gameObject);
        }

        playerItemsList.Clear();
    }

    #region List Players
    private void GetCurrentPlayers()
    {
        foreach (KeyValuePair<int, Player> playerInfo in PhotonNetwork.CurrentRoom.Players)
        {
            AddPlayerList(playerInfo.Value);
        }
    }

    private void AddPlayerList(Player _player)
    {
        int index = playerItemsList.FindIndex(x => x.player == _player);
        if (index != -1)
        {
            playerItemsList[index].SetPlayerInfo(_player);
        }
        
        PlayerItem _newPlayer = Instantiate(playerItemPrefab, playerListContent);
        
        if (_newPlayer != null)
        {
            _newPlayer.SetPlayerInfo(_player);
            playerItemsList.Add(_newPlayer);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.UserId} entrou na sala");

        AddPlayerList(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.UserId} saiu da sala");

        int index = playerItemsList.FindIndex(x => x.player == otherPlayer);
        if (index != -1)
        {
            Destroy(playerItemsList[index].gameObject);
            playerItemsList.RemoveAt(index);
        }
    }
    #endregion
}
