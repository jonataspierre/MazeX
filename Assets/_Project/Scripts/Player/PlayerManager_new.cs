using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PlayerManager_new : MonoBehaviour
{
    PhotonView PV;

    GameObject controller;

    int kills;
    int deaths;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (PV.IsMine)
        {
            CreateController();
        }
    }

    void CreateController()
    {
        //Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        controller = PhotonNetwork.Instantiate("Player_1", new Vector3(Random.Range(5f, 11f), 1.5f, Random.Range(5f, 11f)), Quaternion.identity, 0, new object[] { PV.ViewID });
        controller.name = "Player " + PhotonNetwork.LocalPlayer.NickName;
        //controller.transform.SetParent(transform);
    }

    //public static PlayerManager_new Find(Player player)
    //{
    //    return FindObjectsOfType<PlayerManager_new>().SingleOrDefault(x => x.PV.Owner == player);
    //}

    //public static CharacterController Find(Player player)
    //{
    //    return FindObjectsOfType<CharacterController>().SingleOrDefault(x => x.PV.Owner == player);
    //}

    //public static PlayerInputHandler_new Find(Player player)
    //{
    //    return FindObjectsOfType<PlayerInputHandler_new>().SingleOrDefault(x => x.PV.Owner == player);
    //}
}
