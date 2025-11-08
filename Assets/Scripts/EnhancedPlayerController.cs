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
public class EnhancedPlayerController : BaseMovementController
{
    [Header("Player Movement")]
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private bool useRotationPointOrbiting = true;
    
    [Header("Player Attributes")]
    [SerializeField] private PlayerAttributes playerAttributes;
    
    [Header("Animation")]
    [SerializeField] private bool showAnimationDebug = false;

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
    
    // Animation State
    private Animator playerAnimator;
    private bool isRunning = false;
    private bool isDead = false;
    private bool isAttacking = false;
    private int attackLayerIndex = -1;

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
        
        // Unsubscribe from health events
        HealthController playerHealth = GetComponent<HealthController>();
        if (playerHealth != null)
        {
            playerHealth.OnDeathLocal -= () => TriggerDeathAnimation();
        }

        if (playerInputActions != null)
        {
            playerInputActions.Dispose();
        }
    }

    void Update()
    {
        // Don't update if dead
        if (isDead) return;
        
        if (enableInput && !isTransitioning)
        {
            HandleInput();
            UpdateMovement();
            CheckForCubeRotationTrigger(); // New position-based rotation check
        }
        
        UpdateAnimations();

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
        // Initialize animator component (in child object)
        playerAnimator = GetComponentInChildren<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogWarning("EnhancedPlayerController: No Animator component found in children!");
        }
        else
        {
            Debug.Log("EnhancedPlayerController: Found Animator component in child");
            
            // Find the attack layer index
            for (int i = 0; i < playerAnimator.layerCount; i++)
            {
                if (playerAnimator.GetLayerName(i).ToLower().Contains("attack"))
                {
                    attackLayerIndex = i;
                    Debug.Log($"EnhancedPlayerController: Found attack layer at index {attackLayerIndex}");
                    break;
                }
            }
            
            if (attackLayerIndex == -1)
            {
                Debug.LogWarning("EnhancedPlayerController: No 'attack' animation layer found!");
            }
        }
        
        // Initialize health controller integration
        HealthController playerHealth = GetComponent<HealthController>();
        if (playerHealth != null)
        {
            playerHealth.OnDeathLocal += () => TriggerDeathAnimation();
            Debug.Log("EnhancedPlayerController: Connected to HealthController death event");
        }
        else
        {
            Debug.LogWarning("EnhancedPlayerController: No HealthController found - death animation won't auto-trigger");
        }
        
        // Initialize move speed from PlayerAttributes
        if (playerAttributes != null)
        {
            moveSpeed = playerAttributes.MoveSpeed;
            Debug.Log($"EnhancedPlayerController: Set move speed to {moveSpeed} from PlayerAttributes");
        }
        else
        {
            Debug.LogWarning("EnhancedPlayerController: No PlayerAttributes assigned, using default move speed");
        }
        
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
        if (WorldCube.Instance != null)
        {
            currentRotationPoint = WorldCube.Instance.GetCurrentRotationPoint();
            Debug.Log("EnhancedPlayerController: Got initial rotation point");
        }
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

    /// <summary>
    /// Check if player should trigger cube rotation based on position and input direction.
    /// Uses the new position-based system instead of trigger colliders.
    /// </summary>
    private void CheckForCubeRotationTrigger()
    {
        if (WorldCube.Instance == null) return;
        Vector3 cameraRelativeInput = GetCameraRelativeMovement(currentInputVector.x, currentInputVector.y);
        Vector2 inputDirection = new(cameraRelativeInput.x, cameraRelativeInput.z);
        // Use current input (not smoothed) for more responsive rotation triggers
        RotationDirection? rotationDirection = WorldCube.Instance.CheckForRotationTrigger(transform.position, inputDirection);

        if (rotationDirection.HasValue)
        {
            Debug.Log($"EnhancedPlayerController: Triggering {rotationDirection.Value} rotation based on position + input");
            WorldCube.Instance.TryRotateInDirection(rotationDirection.Value);
        }
    }

    #endregion

    #region Animation Handling

    /// <summary>
    /// Update animation parameters based on current player state.
    /// </summary>
    private void UpdateAnimations()
    {
        if (playerAnimator == null || isDead) return;
        
        // Update running parameter based on movement
        bool shouldBeRunning = currentVelocity.magnitude > 0.1f && !isTransitioning;
        
        if (shouldBeRunning != isRunning)
        {
            isRunning = shouldBeRunning;
            playerAnimator.SetBool("running", isRunning);
            
            if (showAnimationDebug)
            {
                Debug.Log($"Player: Set running to {isRunning} (velocity: {currentVelocity.magnitude:F2})");
            }
        }
        
        // Update player rotation to face movement direction
        if (shouldBeRunning && currentVelocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = currentVelocity.normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
        
        // Update attack layer weight
        UpdateAttackLayerWeight();
    }
    
    /// <summary>
    /// Update the attack animation layer weight based on attack state.
    /// </summary>
    private void UpdateAttackLayerWeight()
    {
        if (playerAnimator == null || attackLayerIndex == -1) return;
        
        float targetWeight = isAttacking ? 1f : 0f;
        float currentWeight = playerAnimator.GetLayerWeight(attackLayerIndex);
        
        if (Mathf.Abs(currentWeight - targetWeight) > 0.01f)
        {
            // Smoothly transition layer weight
            float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * 5f);
            playerAnimator.SetLayerWeight(attackLayerIndex, newWeight);
            
            if (showAnimationDebug)
            {
                Debug.Log($"Player: Attack layer weight: {newWeight:F2} (target: {targetWeight})");
            }
        }
    }
    
    /// <summary>
    /// Trigger player death animation.
    /// Called when player health reaches zero.
    /// </summary>
    public void TriggerDeathAnimation()
    {
        if (isDead || playerAnimator == null) return;
        
        isDead = true;
        isRunning = false;
        isAttacking = false; // Stop attacking when dying
        
        // Stop all movement
        currentVelocity = Vector3.zero;
        targetVelocity = Vector3.zero;
        if (rigidBody != null)
        {
            rigidBody.velocity = Vector3.zero;
        }
        
        // Trigger death animation and reset all states
        playerAnimator.SetTrigger("death");
        playerAnimator.SetBool("running", false);
        
        // Reset attack layer weight immediately
        if (attackLayerIndex != -1)
        {
            playerAnimator.SetLayerWeight(attackLayerIndex, 0f);
        }
        
        if (showAnimationDebug)
        {
            Debug.Log("Player: Death animation triggered, attack layer reset");
        }
        
        // Start death sequence
        StartCoroutine(HandleDeathSequence());
    }
    
    /// <summary>
    /// Handle death sequence - wait for animation then notify game systems.
    /// </summary>
    private System.Collections.IEnumerator HandleDeathSequence()
    {
        // Wait for death animation to complete
        yield return new WaitForSeconds(2f);
        
        if (showAnimationDebug)
        {
            Debug.Log("Player: Death animation sequence complete");
        }
        
        // Notify other systems that player has died
        // TODO: Trigger game over or respawn logic
        OnPlayerDeath();
    }
    
    /// <summary>
    /// Called when player death sequence is complete.
    /// Override this method or subscribe to events for game-specific logic.
    /// </summary>
    protected virtual void OnPlayerDeath()
    {
        Debug.Log("Player: Death sequence complete - implement game over logic here");
        
        // Example: Disable the player controller
        enableInput = false;
        
        // TODO: Integrate with GameManager for respawn/game over
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
            orbitingRotationPoint = WorldCube.Instance.GetNearestRotationPoint(transform.position);

            if (orbitingRotationPoint != null)
            {
                orbitStartPosition = transform.position;
                StartCoroutine(OrbitAroundRotationPoint(direction));

                Debug.Log($"EnhancedPlayerController: Started orbiting around {orbitingRotationPoint.name}");
            }
            else
            {
                Debug.LogWarning("EnhancedPlayerController: No rotation point found for orbiting!");
                positionBeforeRotation = transform.position;
            }
        }
        else
        {
            positionBeforeRotation = transform.position;
            Debug.Log("EnhancedPlayerController: Rotation point orbiting disabled, storing position");
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
        if (WorldCube.Instance != null)
        {
            currentRotationPoint = WorldCube.Instance.GetCurrentRotationPoint();
            Debug.Log($"EnhancedPlayerController: Updated rotation point for face {newFace}");
        }
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
    public override bool IsTransitioning()
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
    public bool IsRunning => isRunning;
    public bool IsDead => isDead;
    public bool IsAttacking => isAttacking;

    public override void SetMoveSpeed(float speed) => moveSpeed = speed;
    public void SetInputEnabled(bool enabled) => enableInput = enabled;
    
    /// <summary>
    /// Update move speed from PlayerAttributes.
    /// Call this when attributes change during gameplay.
    /// </summary>
    public void UpdateMoveSpeedFromAttributes()
    {
        if (playerAttributes != null)
        {
            SetMoveSpeed(playerAttributes.MoveSpeed);
            Debug.Log($"EnhancedPlayerController: Updated move speed to {moveSpeed}");
        }
    }

    /// <summary>
    /// Get reference to PlayerAttributes for other systems.
    /// </summary>
    public PlayerAttributes GetPlayerAttributes()
    {
        return playerAttributes;
    }
    
    /// <summary>
    /// Manually trigger death animation (for testing or special cases).
    /// </summary>
    public void ManualTriggerDeath()
    {
        TriggerDeathAnimation();
    }
    
    /// <summary>
    /// Reset player state (for respawn systems).
    /// </summary>
    public void ResetPlayerState()
    {
        isDead = false;
        isRunning = false;
        isAttacking = false;
        enableInput = true;
        currentVelocity = Vector3.zero;
        targetVelocity = Vector3.zero;
        
        if (rigidBody != null)
        {
            rigidBody.velocity = Vector3.zero;
        }
        
        if (playerAnimator != null)
        {
            // Reset animator to default state
            playerAnimator.SetBool("running", false);
            // Reset attack layer weight
            if (attackLayerIndex != -1)
            {
                playerAnimator.SetLayerWeight(attackLayerIndex, 0f);
            }
            // Note: death is a trigger, so it auto-resets
        }
        
        Debug.Log("EnhancedPlayerController: Player state reset for respawn");
    }
    
    /// <summary>
    /// Start player attack animation (sets attack layer weight to max).
    /// Call this when player begins attacking.
    /// </summary>
    public void StartAttack()
    {
        if (isDead) return;
        
        isAttacking = true;
        
        if (showAnimationDebug)
        {
            Debug.Log("Player: Started attacking - attack layer weight will increase");
        }
    }
    
    /// <summary>
    /// Stop player attack animation (sets attack layer weight to 0).
    /// Call this when player stops attacking.
    /// </summary>
    public void StopAttack()
    {
        isAttacking = false;
        
        if (showAnimationDebug)
        {
            Debug.Log("Player: Stopped attacking - attack layer weight will decrease");
        }
    }

    #endregion
}