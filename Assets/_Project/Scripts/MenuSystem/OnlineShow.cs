using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class OnlineShow : MonoBehaviourPun
{
    public TextMeshProUGUI countPlayersText;

    private void OnEnable()
    {
        if(photonView.IsMine && PhotonNetwork.IsMasterClient)
        {
            InvokeRepeating("UpdateOnlinePlayers", 0f, 5f);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator UpdateOnlinePlayers()
    {
        int index = PhotonNetwork.CountOfPlayers;
        countPlayersText.text = $"Online: <color=\"green\"><b>{index}</b></color>";
        yield return new WaitForSeconds(5f);
    }
}
