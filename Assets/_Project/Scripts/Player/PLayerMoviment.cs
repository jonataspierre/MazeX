using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLayerMoviment : MonoBehaviourPun
{
    public float moveSpeed = 50f;
    public float rotationSpeed = 50f;

    private CharacterController characterController;
    [SerializeField] Transform cameraTransform;

    PlayerManager_new playerManager;

    PhotonView PV;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        PV = GetComponent<PhotonView>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager_new>();
    }

    void Start()
    {
        if (PV.IsMine)
        {
            //EquipItem(0);
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(characterController);            
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput);
        if (moveDirection != Vector3.zero)
        {
            // Mover na direção em relação à câmera local
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection.y = 0f;
            moveDirection.Normalize(); // Normalizar o vetor para que o jogador não se mova mais rápido na diagonal

            // Rotação do jogador para a direção de movimento
            Quaternion newRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
        }

        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Girar a câmera com o analógico direito apenas
        float rotateHorizontal = Input.GetAxisRaw("Look X");
        float rotateVertical = Input.GetAxisRaw("Look Y");

        if (Mathf.Abs(rotateHorizontal) > 0.1f || Mathf.Abs(rotateVertical) > 0.1f)
        {
            Vector3 rotation = new Vector3(0f, rotateHorizontal, 0f) * rotationSpeed * Time.deltaTime;
            transform.Rotate(rotation); // Rotaciona apenas o jogador no eixo Y (horizontal)

            Vector3 cameraRotation = new Vector3(-rotateVertical, 0f, 0f) * rotationSpeed * Time.deltaTime;
            cameraTransform.Rotate(cameraRotation); // Rotaciona a câmera no eixo X (vertical)
        }

        // Atualizar a posição horizontal da câmera para seguir o jogador
        Vector3 cameraPosition = transform.position;
        cameraPosition.y = cameraTransform.position.y; // Mantém a altura atual da câmera
        cameraTransform.position = cameraPosition;

        //// Girar a câmera com o analógico direito
        //float rotateHorizontal = Input.GetAxisRaw("Look X");
        //float rotateVertical = Input.GetAxisRaw("Look Y");

        //if (Mathf.Abs(rotateHorizontal) > 0.1f || Mathf.Abs(rotateVertical) > 0.1f)
        //{
        //    Vector3 rotation = new Vector3(rotateVertical, rotateHorizontal, 0f) * rotationSpeed * Time.deltaTime;
        //    cameraTransform.Rotate(rotation);
        //}

        //// Atualizar a posição horizontal da câmera para seguir o jogador
        //Vector3 cameraPosition = transform.position;
        //cameraPosition.y = cameraTransform.position.y; // Mantém a altura atual da câmera
        //cameraTransform.position = cameraPosition;

        //float horizontalInput = Input.GetAxis("Horizontal");
        //float verticalInput = Input.GetAxis("Vertical");

        //Vector3 moveDirection = cameraTransform.TransformDirection(new Vector3(horizontalInput, 0f, verticalInput));
        //moveDirection.y = 0f;

        //characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        //if (moveDirection != Vector3.zero)
        //{
        //    Quaternion newRotation = Quaternion.LookRotation(moveDirection);
        //    transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
        //}

        //// Girar a câmera com o analógico direito
        //float rotateHorizontal = Input.GetAxisRaw("Look X");
        //float rotateVertical = Input.GetAxisRaw("Look Y");

        //if (Mathf.Abs(rotateHorizontal) > 0.1f || Mathf.Abs(rotateVertical) > 0.1f)
        //{
        //    Vector3 rotation = new Vector3(rotateVertical, rotateHorizontal, 0f) * rotationSpeed * Time.deltaTime;
        //    cameraTransform.Rotate(rotation);
        //}

        //// Atualizar a posição horizontal da câmera para seguir o jogador
        //Vector3 cameraPosition = transform.position;
        //cameraPosition.y = cameraTransform.position.y; // Mantém a altura atual da câmera
        //cameraTransform.position = cameraPosition;

        //////Sincronizar a posição e rotação pela rede
        ////if (photonView.IsMine || !PhotonNetwork.IsConnected)
        ////{
        ////    // Enviar posição e rotação para outros clientes
        ////    photonView.RPC("SyncPositionAndRotation", RpcTarget.OthersBuffered, transform.position, transform.rotation);
        ////}
    }

    //[PunRPC]
    //void SyncPositionAndRotation(Vector3 position, Quaternion rotation)
    //{
    //    // Atualizar posição e rotação do jogador nos outros clientes
    //    transform.position = position;
    //    transform.rotation = rotation;
    //}

    //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    //{
    //    if (stream.IsWriting)
    //    {
    //        // Enviar posição e rotação para outros clientes
    //        stream.SendNext(transform.position);
    //        stream.SendNext(transform.rotation);
    //    }
    //    else
    //    {
    //        // Receber posição e rotação dos outros jogadores
    //        transform.position = (Vector3)stream.ReceiveNext();
    //        transform.rotation = (Quaternion)stream.ReceiveNext();
    //    }
    //}
}
