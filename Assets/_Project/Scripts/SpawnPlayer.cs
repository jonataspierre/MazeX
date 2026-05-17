using Photon.Pun;
using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    public static SpawnPlayer Instance;
    
    //public GameObject localPlayer;
    public GameObject playerPreFab;

    private void OnEnable()
    {
        Generator2D.OnRoomCreate.AddListener(InstantiatePlayer);
    }

    private void OnDisable()
    {
        Generator2D.OnRoomCreate.RemoveListener(InstantiatePlayer);
    }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
        //localPlayer.transform.SetParent(transform, false);
    }

    public void InstantiatePlayer(Vector3 position)
    {
        GameObject localPlayer = PhotonNetwork.Instantiate("PlayerManager", position, Quaternion.identity, 0);
        localPlayer.name = "Manager_" + PhotonNetwork.LocalPlayer.NickName;
    }
}
