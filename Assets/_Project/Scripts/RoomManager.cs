using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager instance;

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == "MazeX_new") // We're in the game scene
        {
            //var player = PhotonNetwork.Instantiate(Path.Combine("Demo", "PlayerManager"), Vector3.zero, Quaternion.identity);
            var player = PhotonNetwork.Instantiate("PlayerManager", Vector3.zero, Quaternion.identity);
            player.name = PhotonNetwork.LocalPlayer.NickName;
        }
    }
}
