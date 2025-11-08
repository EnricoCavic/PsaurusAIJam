using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Enhanced player controller using Unity's new Input System.
/// Handles movement with support for keyboard, gamepad, and touch input.
/// Works with WorldCube's rotation point system for smooth cube transitions.
/// Extends BaseMovementController for shared movement logic.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnhancedPlayerController_Backup : BaseMovementController_Backup
{
    [Header("Player Movement")]
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private bool useRotationPointOrbiting = true;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool useCameraRelativeMovement = true;

    [Header("Input")]
    [SerializeField] private bool enableInput = true;
    [SerializeField] private float inputDeadzone = 0.1f;

    // Input System
    private PlayerInputAction playerInputActions;
    private InputAction moveAction;
    private Vector2 currentInputVector;
    private Vector2 smoothedInputVector;

    // Movement State
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    // Note: isTransitioning is inherited from BaseMovementController

    // Cube Rotation State
    private Vector3 positionBeforeRotation;
    private Transform currentRotationPoint;
    private Transform orbitingRotationPoint;
    private Vector3 orbitStartPosition;

    #region Unity Lifecycle

    void Awake()
    {
        // Initialize Input Actions - this will work once you generate the C# class
        playerInputActions = new PlayerInputAction();

        // Cache the move action for better performance
        moveAction = playerInputActions.Main.Move;
    }

    protected override void Start()
    {
        base.Start(); // Call base initialization
        InitializePlayerComponents();
        SubscribeToEvents();

        Debug.Log("EnhancedPlayerController: Initialized with new Input System");
    }

    void OnEnable()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Enable();
        }
    }

    void OnDisable()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Disable();
        }
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (playerInputActions != null)
        {
            playerInputActions.Dispose();
        }
    }

    void Update()
    {
        if (enableInput && !isTransitioning)
        {
            HandleInput();
            UpdateMovement();
        }

        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // Call base gravity application
        
        if (!isTransitioning)
        {
            ApplyMovement();
        }
    }

    #endregion

    #region Initialization

    private void InitializePlayerComponents()
    {
        // Find camera if not assigned
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
                Debug.Log("EnhancedPlayerController: Using main camera for movement reference");
            }
            else
            {
                Debug.LogWarning("EnhancedPlayerController: No camera found! Movement will use world coordinates.");
            }
        }

        // Get initial rotation point
        // Note: Rotation point system was simplified in main version
        // if (WorldCube.Instance != null)
        // {
        //     currentRotationPoint = WorldCube.Instance.GetCurrentRotationPoint();
        // }
    }

    private void SubscribeToEvents()
    {
        WorldCube.OnRotationStarted += OnCubeRotationStarted;
        WorldCube.OnRotationCompleted += OnCubeRotationCompleted;
        WorldCube.OnFaceChanged += OnFaceChanged;
    }

    private void UnsubscribeFromEvents()
    {
        WorldCube.OnRotationStarted -= OnCubeRotationStarted;
        WorldCube.OnRotationCompleted -= OnCubeRotationCompleted;
        WorldCube.OnFaceChanged -= OnFaceChanged;
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (moveAction != null)
        {
            currentInputVector = moveAction.ReadValue<Vector2>();
        }
        // Apply deadzone
        if (currentInputVector.magnitude < inputDeadzone)
        {
            currentInputVector = Vector2.zero;
        }
        // Smooth input for better feel
        SmoothInput();
    }

    private void SmoothInput()
    {
        // Smooth input vector for better control feel
        float smoothSpeed = currentInputVector.magnitude > smoothedInputVector.magnitude ? acceleration : deceleration;
        smoothedInputVector = Vector2.MoveTowards(smoothedInputVector, currentInputVector, smoothSpeed * Time.deltaTime);
    }

    #endregion

    #region Movement

    private void UpdateMovement()
    {
        // Convert input to camera-relative movement direction
        Vector3 moveDirection = GetCameraRelativeMovement(smoothedInputVector.x, smoothedInputVector.y);

        // Calculate target velocity
        targetVelocity = moveDirection * moveSpeed;
    }

    /// <summary>
    /// Convert input to camera-relative movement direction.
    /// </summary>
    private Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
    {
        if (!useCameraRelativeMovement || cameraTransform == null)
        {
            // Fallback to world-relative movement
            return new Vector3(horizontal, 0, vertical).normalized;
        }

        // Get camera's forward and right vectors, but keep them on the horizontal plane
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Project to horizontal plane (remove Y component)
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // Normalize the projected vectors
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate movement direction relative to camera
        Vector3 moveDirection = (cameraRight * horizontal) + (cameraForward * vertical);

        return moveDirection.normalized;
    }
    private void ApplyMovement()
    {
        // Smooth velocity changes for better feel
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity,
            (targetVelocity.magnitude > currentVelocity.magnitude ? acceleration : deceleration) * Time.fixedDeltaTime);

        // Apply to rigidbody (preserve Y velocity)
        rigidBody.velocity = new Vector3(currentVelocity.x, rigidBody.velocity.y, currentVelocity.z);
    }

    /// <summary>
    /// Apply custom gravity that always pulls player toward the current face.
    /// This keeps the player grounded on the cube surface after rotations.
    /// </summary>
    private void ApplyCustomGravity()
    {
        if (!useCustomGravity) return;

        // Get the gravity direction based on the current cube face
        Vector3 gravityDirection = GetCurrentGravityDirection();

        // Apply gravity force
        Vector3 gravityForceVector = faceGravityDirection * gravityForce;
        rigidBody.AddForce(gravityForceVector, ForceMode.Acceleration);
    }

    /// <summary>
    /// Get the gravity direction for the current cube face.
    /// This always points "down" relative to the current face.
    /// </summary>
    private Vector3 GetCurrentGravityDirection()
    {
        // Since the camera stays fixed and cube rotates to show different faces,
        // gravity should always point down in world space (-Y)
        // The cube rotation brings the current face to the "north" position
        return Vector3.down;
    }

    #endregion

    #region Cube Rotation Integration

    private void OnCubeRotationStarted(RotationDirection direction)
    {
        isTransitioning = true;

        // Stop movement immediately
        currentVelocity = Vector3.zero;
        targetVelocity = Vector3.zero;
        rigidBody.velocity = Vector3.zero;

        if (useRotationPointOrbiting && WorldCube.Instance != null)
        {
            // Find nearest rotation point to orbit around
            // Note: GetNearestRotationPoint method was removed in simplified version
            // orbitingRotationPoint = WorldCube.Instance.GetNearestRotationPoint(transform.position);
            orbitingRotationPoint = null; // Simplified for backup compatibility

            if (orbitingRotationPoint != null)
            {
                orbitStartPosition = transform.position;
                StartCoroutine(OrbitAroundRotationPoint(direction));

                Debug.Log($"EnhancedPlayerController: Started orbiting around {orbitingRotationPoint.name}");
            }
        }
        else
        {
            positionBeforeRotation = transform.position;
        }
    }

    private IEnumerator OrbitAroundRotationPoint(RotationDirection direction)
    {
        if (orbitingRotationPoint == null) yield break;

        float rotationDuration = WorldCube.Instance != null ? WorldCube.Instance.RotationDuration : 1f;
        float elapsedTime = 0f;

        // Calculate orbit parameters
        float orbitAngle = GetOrbitAngle(direction);
        Vector3 originalRelativePosition = orbitStartPosition - orbitingRotationPoint.position;
        Vector3 horizontalOffset = new Vector3(originalRelativePosition.x, 0, originalRelativePosition.z);
        float horizontalRadius = horizontalOffset.magnitude;
        float originalYOffset = originalRelativePosition.y;
        float initialAngle = Mathf.Atan2(horizontalOffset.z, horizontalOffset.x) * Mathf.Rad2Deg;

        while (elapsedTime < rotationDuration)
        {
            float progress = elapsedTime / rotationDuration;
            float currentAngle = initialAngle + (orbitAngle * progress);

            // Calculate new position
            Vector3 horizontalPosition = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad) * horizontalRadius,
                originalYOffset,
                Mathf.Sin(currentAngle * Mathf.Deg2Rad) * horizontalRadius
            );

            if (orbitingRotationPoint != null)
            {
                transform.position = orbitingRotationPoint.position + horizontalPosition;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private float GetOrbitAngle(RotationDirection direction)
    {
        switch (direction)
        {
            case RotationDirection.Left: return -90f;
            case RotationDirection.Right: return 90f;
            case RotationDirection.Up: return -90f;
            case RotationDirection.Down: return 90f;
            default: return 0f;
        }
    }

    private void OnCubeRotationCompleted()
    {
        isTransitioning = false;
        orbitingRotationPoint = null;

        // Reset input smoothing
        smoothedInputVector = Vector2.zero;
        currentVelocity = Vector3.zero;

        Debug.Log("EnhancedPlayerController: Cube rotation completed, input re-enabled");
    }

    private void OnFaceChanged(CubeFace newFace)
    {
        // Note: GetCurrentRotationPoint method was removed in simplified version
        // if (WorldCube.Instance != null)
        // {
        //     currentRotationPoint = WorldCube.Instance.GetCurrentRotationPoint();
        //     Debug.Log($"EnhancedPlayerController: Updated rotation point for face {newFace}");
        // }
        Debug.Log($"EnhancedPlayerController_Backup: Face changed to {newFace}");
    }

    #endregion

    #region Debug

    protected override void DrawDebugInfo()
    {
        base.DrawDebugInfo(); // Call base debug visualization
        
        // Draw current velocity
        Debug.DrawLine(transform.position, transform.position + currentVelocity, Color.blue);
        
        // Draw target velocity
        Debug.DrawLine(transform.position, transform.position + targetVelocity, Color.cyan);
        
        // Draw input vector (camera-relative)
        Vector3 inputDir = GetCameraRelativeMovement(currentInputVector.x, currentInputVector.y) * 2f;
        Debug.DrawLine(transform.position + Vector3.up * 0.5f, 
                      transform.position + Vector3.up * 0.5f + inputDir, Color.yellow);
        
        // Draw camera forward and right directions (for debugging camera-relative movement)
        if (cameraTransform != null && useCameraRelativeMovement)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();
            
            Debug.DrawLine(transform.position + Vector3.up * 1.5f, 
                          transform.position + Vector3.up * 1.5f + camForward * 1f, Color.cyan);
            Debug.DrawLine(transform.position + Vector3.up * 1.5f, 
                          transform.position + Vector3.up * 1.5f + camRight * 1f, Color.magenta);
        }
        
        // Draw line to current rotation point
        if (currentRotationPoint != null)
        {
            Debug.DrawLine(transform.position, currentRotationPoint.position, Color.yellow);
        }
        
        // Draw line to orbiting rotation point during transitions
        if (orbitingRotationPoint != null && isTransitioning)
        {
            Debug.DrawLine(transform.position, orbitingRotationPoint.position, Color.red);
        }
    }
    
    #endregion

    #region Transition State Override
    
    /// <summary>
    /// Override to include player-specific transition state tracking.
    /// </summary>
    protected override bool IsTransitioning()
    {
        // Use base implementation (checks WorldCube state) plus local transition state
        return base.IsTransitioning() || isTransitioning;
    }
    
    #endregion

    #region Public Properties and Methods

    // Note: IsTransitioning() method is inherited from BaseMovementController
    public float MoveSpeed => moveSpeed;
    public Vector2 CurrentInput => currentInputVector;
    public Vector3 CurrentVelocity => currentVelocity;

    public override void SetMoveSpeed(float speed) => moveSpeed = speed;
    public void SetInputEnabled(bool enabled) => enableInput = enabled;

    #endregion
}