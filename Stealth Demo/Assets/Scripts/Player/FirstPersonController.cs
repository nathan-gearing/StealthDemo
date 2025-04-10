using System.Collections;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public enum PlayerState { Idle, Walking, Sprinting, Crouching}
    
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public Transform cameraTransform;
    public CharacterController controller;
    public static FirstPersonController Instance { get; private set; }

    public float crouchHeight = 0.5f;
    public float standingHeight = 1f;
    public float crouchSpeed = 2f;
    public float standSpeed = 4f;
    public float jumpForce = 1.2f;
    public float sprintSpeed = 7f;

    private PlayerState currentState = PlayerState.Idle;
    public bool isCrouching = false;
    public bool isSprinting = false;
    
    private float verticalVelocity;
    private float xRotation = 0f;
    private Vector3 originalCameraPosition;
    private float bobTimer = 0f;
    private float baseCameraHeight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
     void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        originalCameraPosition = cameraTransform.localPosition;
        baseCameraHeight = originalCameraPosition.y;
    }

    // Update is called once per frame
    void Update()
    {

        isSprinting = Input.GetKey(KeyCode.LeftShift);
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrouch();
        }

        if (isSprinting && isCrouching)
        {
            ToggleCrouch();
        }

        UpdatePlayerState();

        HandleMouseLook();
        HandleMovement();
        HandleHeadBobbing();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float currentSpeed = currentState switch
        {
            PlayerState.Crouching => crouchSpeed,
            PlayerState.Sprinting => sprintSpeed,
            PlayerState.Walking => standSpeed,
            _ => standSpeed
        };
        
        Vector3 move = cameraTransform.right * moveX + transform.forward * moveZ;
        move.y = 0;
        move = move.normalized;

        if (controller.isGrounded)
        {
            verticalVelocity = -1f;
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpForce * -gravity);
            }
        } else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void UpdatePlayerState()
    {
        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;

        if (isCrouching)
        {
            currentState = PlayerState.Crouching;
        }
        else if (isSprinting && isMoving)
        {
            currentState = PlayerState.Sprinting;
        }
        else if (isMoving)
        {
            currentState = PlayerState.Walking;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        bobTimer = 0f;
        /*if (currentState != PlayerState.Crouching)
        {
            currentState = PlayerState.Crouching;
        }
        else
        {
            currentState = PlayerState.Walking;
        }*/

        float targetCameraHeight = isCrouching ? crouchHeight : standingHeight;
        StartCoroutine(CrouchCameraTransition(targetCameraHeight));
    }

    IEnumerator CrouchCameraTransition(float targetHeight)
    {
        float startHeight = cameraTransform.localPosition.y;
        float elapsedTime = 0f;
        float duration = 0.2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newHeight = Mathf.Lerp(startHeight, targetHeight, elapsedTime / duration);
            cameraTransform.localPosition = new Vector3(0, newHeight, 0);
            yield return null;
        }

        cameraTransform.localPosition = new Vector3(0, targetHeight, 0);
        baseCameraHeight = targetHeight;
    }

    void HandleHeadBobbing()
    {

        float bobSpeed = currentState switch
        {
            PlayerState.Sprinting => 1.5f,
            PlayerState.Walking => 1.0f,
            PlayerState.Crouching => 0.5f,
            _ => 0f
        };
        
        if (bobSpeed > 0)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobAmount = Mathf.Sin(bobTimer) * 0.05f;
            cameraTransform.localPosition =  new Vector3(0, baseCameraHeight + bobAmount, 0);
        }
        else
        {
            cameraTransform.localPosition = new Vector3(0, baseCameraHeight, 0);
        }
    }

    
}
