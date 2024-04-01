using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerItem : MonoBehaviour
{
    public TextMeshProUGUI playerName;

    public Player player { get; private set; }

    public void SetPlayerInfo(Player _player)
    {
        player = _player;
        playerName.text = player.NickName;
    }
}
