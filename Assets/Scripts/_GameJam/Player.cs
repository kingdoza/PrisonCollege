using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 방지
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 유지
    }
    public UpgradeDoors LightSwitch;
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.7f; // 달리기 배수


    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;
    public float maxLookAngle = 80f;

    private CharacterController controller;
    private Vector3 velocity;
    public GameObject FloodSystem;
    private float xRotation = 0f;
    private float speedRate = 1;

    public AudioPlayer audioPlayer;

    public float chaosInterval = 5;
    private float chaosRemained;

    public GameObject warningPrefab;
    public Canvas canvas;
    public AudioSource warningSource;

    void Start()
    {
        chaosRemained = chaosInterval;
        FloodSystem.GetComponent<FloodController>().FullEvent.AddListener(OnFloodFull);
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnFloodFull(bool isFull)
    {
        Debug.Log("OnFloodFull : " + isFull);
        speedRate = isFull ? 0.3f : 1;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();

        chaosRemained -= Time.deltaTime;
        if (chaosRemained <= 0)
        {
            ChaosSystem.chaos = Mathf.Max(ChaosSystem.chaos - 1, 0);
            chaosRemained = chaosInterval;
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Shift 입력 감지
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isSprinting
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime * speedRate);

        // 중력 처리
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
