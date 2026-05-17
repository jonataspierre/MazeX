using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController_new : MonoBehaviourPun
{
    [Header("References")]
    [Tooltip("Reference to the main camera used for the player")]
    public Camera PlayerCamera;
    public Transform cameraTransform;

    [Tooltip("Audio source for footsteps, jump, etc...")]
    public AudioSource AudioSource;

    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    public float GravityDownForce = 20f;
    public CharacterController characterController;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    public float RotationSpeed = 200f;

    [Header("Input Handler")]
    public PlayerInputHandler_new inputHandler;

    // Private variables
    private Vector3 characterVelocity;
    private bool isGrounded;
    private Vector3 groundNormal;
    private float cameraVerticalAngle = 0f;

    void Start()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            Destroy(PlayerCamera.gameObject);
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
            return;

        HandleCharacterMovement();
        HandleCameraRotation();
    }

    void HandleCharacterMovement()
    {
        if (!inputHandler.CanProcessInput())
            return;

        // Get movement input
        Vector3 moveInput = inputHandler.GetMoveInput();

        // Move the player
        Vector3 moveDirection = cameraTransform.TransformDirection(moveInput);
        moveDirection.y = 0f;
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Rotate the player towards the movement direction
        if (moveDirection != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
        }

        // Handle jumping
        if (isGrounded && inputHandler.GetJumpInputDown())
        {
            //characterVelocity.y = Mathf.Sqrt(2f * GravityDownForce * JumpHeight);
        }

        // Apply gravity
        characterVelocity.y -= GravityDownForce * Time.deltaTime;

        // Check if the player is grounded
        isGrounded = characterController.isGrounded;

        // Move the character controller
        characterController.Move(characterVelocity * Time.deltaTime);
    }

    void HandleCameraRotation()
    {
        if (!inputHandler.CanProcessInput())
            return;

        // Horizontal character rotation
        transform.Rotate(Vector3.up, inputHandler.GetLookInputsHorizontal() * RotationSpeed * Time.deltaTime);

        // Vertical camera rotation
        cameraVerticalAngle += inputHandler.GetLookInputsVertical() * RotationSpeed * Time.deltaTime;
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -89f, 89f);
        PlayerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
    }
}
