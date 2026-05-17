using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class SceneController : MonoBehaviourPunCallbacks
{
    public static SceneController instance;

    const string LobbySceneName = "Lobby";

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadSceneAdditively(LobbySceneName);
    }

    public async void LoadSceneAdditively(string sceneName, bool unloadLobby = false)
    {
        Debug.Log($"Loading Scene: {sceneName}");

        if (unloadLobby)
        {
            ClearLobbyScene();
        }

        var newScene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Aguarda o término do carregamento
        while (!newScene.isDone)
        {
            await Task.Yield();
        }

        newScene.allowSceneActivation = true;
    }    

    public void ClearLobbyScene()
    {
        SceneManager.UnloadSceneAsync(LobbySceneName);
    }
}
