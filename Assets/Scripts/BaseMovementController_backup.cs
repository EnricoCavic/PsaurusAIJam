using UnityEngine;

/// <summary>
/// Base movement controller with shared functionality for both player and enemy movement.
/// Handles automatic face detection, face-specific gravity toward cube center, and proper orientation.
/// </summary>
public abstract class BaseMovementController_Backup : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float gravityForce = 9.81f;
    
    [Header("Cube Settings")]
    [SerializeField] protected Vector3 cubeCenter = Vector3.zero;
    [SerializeField] protected float cubeSize = 10f;
    [SerializeField] protected bool useCustomGravity = true;
    [SerializeField] protected bool useAutoFaceDetection = true;
    
    [Header("Face Assignment (Manual Override)")]
    [SerializeField] protected CubeFace manualFace = CubeFace.North;
    [SerializeField] protected bool useManualFace = false;
    
    [Header("Debug")]
    [SerializeField] protected bool showDebugInfo = false;
    
    // Components
    protected Rigidbody rigidBody;
    
    // Face-specific data
    protected CubeFace currentFace;
    protected Vector3 faceUpDirection;
    protected Vector3 faceGravityDirection;
    protected Vector3 faceCenterPosition;
    
    // Transition state (for cube rotations)
    protected bool isTransitioning = false;
    
    #region Unity Lifecycle
    
    protected virtual void Start()
    {
        InitializeComponents();
        
        // Try to find the WorldCube for automatic cube center detection
        if (WorldCube.Instance != null)
        {
            cubeCenter = WorldCube.Instance.transform.position;
        }
        
        UpdateFaceOrientation();
    }
    
    protected virtual void FixedUpdate()
    {
        // Only update face detection when not transitioning to avoid conflicts
        if (useAutoFaceDetection && !useManualFace && !IsTransitioning())
        {
            DetectCurrentFace();
        }

        // Only apply custom gravity and alignment when not transitioning
        if (useCustomGravity && !IsTransitioning())
        {
            ApplyFaceGravity();
            AlignToFace();
        }
        
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }
    
    #endregion
    
    #region Initialization
    
    protected virtual void InitializeComponents()
    {
        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody == null)
        {
            rigidBody = gameObject.AddComponent<Rigidbody>();
        }
        
        rigidBody.freezeRotation = true;
        rigidBody.useGravity = false; // We handle gravity manually
        rigidBody.drag = 1f;
    }
    
    #endregion
    
    #region Transition State Management
    
    /// <summary>
    /// Check if this controller is currently in a transition state (like cube rotation).
    /// Override in derived classes for specific transition detection.
    /// </summary>
    protected virtual bool IsTransitioning()
    {
        // Check if WorldCube is rotating
        if (WorldCube.Instance != null && WorldCube.Instance.IsRotating)
        {
            return true;
        }
        
        // Check local transition state
        return isTransitioning;
    }
    
    /// <summary>
    /// Set the transition state manually.
    /// </summary>
    public virtual void SetTransitionState(bool transitioning)
    {
        isTransitioning = transitioning;
    }
    
    #endregion
    
    #region Face Detection and Orientation
    
    /// <summary>
    /// Automatically detect which cube face this entity is closest to based on position.
    /// </summary>
    protected virtual void DetectCurrentFace()
    {
        Vector3 directionFromCenter = (transform.position - cubeCenter).normalized;
        
        // Determine which face this entity is on based on the largest component
        float absX = Mathf.Abs(directionFromCenter.x);
        float absY = Mathf.Abs(directionFromCenter.y);
        float absZ = Mathf.Abs(directionFromCenter.z);
        
        CubeFace newFace = currentFace;
        
        if (absX > absY && absX > absZ)
        {
            // On East or West face
            newFace = directionFromCenter.x > 0 ? CubeFace.East : CubeFace.West;
        }
        else if (absY > absX && absY > absZ)
        {
            // On Top or Bottom face
            newFace = directionFromCenter.y > 0 ? CubeFace.Top : CubeFace.Bottom;
        }
        else if (absZ > absX && absZ > absY)
        {
            // On North or South face
            newFace = directionFromCenter.z > 0 ? CubeFace.North : CubeFace.South;
        }
        
        // Update face if it changed
        if (newFace != currentFace)
        {
            SetCurrentFace(newFace);
        }
    }

    /// <summary>
    /// Update the face orientation based on current face.
    /// Gravity points perpendicular to face surface (toward the face), up direction is face-relative.
    /// </summary>
    public virtual void UpdateFaceOrientation()
    {
        // Override with manual face if specified
        if (useManualFace)
        {
            currentFace = manualFace;
        }
        
        // Calculate face center position and face-specific gravity/orientation
        Vector3 faceOffset = Vector3.zero;
        float halfSize = cubeSize * 0.5f;
        
        switch (currentFace)
        {
            case CubeFace.North: // +Z face (front)
                faceOffset = Vector3.forward * halfSize;
                faceUpDirection = Vector3.up;
                faceGravityDirection = Vector3.back; // Pull toward the face surface
                break;
            case CubeFace.South: // -Z face (back)
                faceOffset = Vector3.back * halfSize;
                faceUpDirection = Vector3.up;
                faceGravityDirection = Vector3.forward; // Pull toward the face surface
                break;
            case CubeFace.East: // +X face (right)
                faceOffset = Vector3.right * halfSize;
                faceUpDirection = Vector3.up;
                faceGravityDirection = Vector3.left; // Pull toward the face surface
                break;
            case CubeFace.West: // -X face (left)
                faceOffset = Vector3.left * halfSize;
                faceUpDirection = Vector3.up;
                faceGravityDirection = Vector3.right; // Pull toward the face surface
                break;
            case CubeFace.Top: // +Y face (ceiling)
                faceOffset = Vector3.up * halfSize;
                faceUpDirection = Vector3.back; // Up on top face points toward camera (back)
                faceGravityDirection = Vector3.down; // Pull toward the face surface
                break;
            case CubeFace.Bottom: // -Y face (floor)
                faceOffset = Vector3.down * halfSize;
                faceUpDirection = Vector3.forward; // Up on bottom face points away from camera
                faceGravityDirection = Vector3.up; // Pull toward the face surface
                break;
        }
        
        faceCenterPosition = cubeCenter + faceOffset;
    }
    
    /// <summary>
    /// Align the entity's rotation to match the face orientation.
    /// Entity's "down" points in the face gravity direction (toward face surface).
    /// </summary>
    protected virtual void AlignToFace()
    {
        // Use face-specific gravity direction as "down"
        Vector3 downDirection = faceGravityDirection;
        
        // Up direction is opposite to gravity
        Vector3 upDirection = -downDirection;
        
        // Calculate forward direction (perpendicular to up, facing along the face plane)
        Vector3 forwardDirection;
        
        switch (currentFace)
        {
            case CubeFace.North:
            case CubeFace.South:
                forwardDirection = Vector3.ProjectOnPlane(transform.forward, upDirection).normalized;
                if (forwardDirection == Vector3.zero) forwardDirection = Vector3.right;
                break;
            case CubeFace.East:
            case CubeFace.West:  
                forwardDirection = Vector3.ProjectOnPlane(transform.forward, upDirection).normalized;
                if (forwardDirection == Vector3.zero) forwardDirection = Vector3.forward;
                break;
            case CubeFace.Top:
                forwardDirection = Vector3.ProjectOnPlane(Vector3.forward, upDirection).normalized;
                if (forwardDirection == Vector3.zero) forwardDirection = Vector3.right;
                break;
            case CubeFace.Bottom:
                forwardDirection = Vector3.ProjectOnPlane(Vector3.back, upDirection).normalized;
                if (forwardDirection == Vector3.zero) forwardDirection = Vector3.right;
                break;
            default:
                forwardDirection = Vector3.forward;
                break;
        }
        
        // Create rotation from up and forward directions
        if (forwardDirection != Vector3.zero && upDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, upDirection);
            
            // Smoothly rotate toward target orientation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    #endregion
    
    #region Face-Specific Movement
    
    /// <summary>
    /// Apply gravity in the face-specific direction.
    /// </summary>
    protected virtual void ApplyFaceGravity()
    {
        if (rigidBody == null) return;
        
        Vector3 gravityForceVector = faceGravityDirection * gravityForce;
        rigidBody.AddForce(gravityForceVector, ForceMode.Acceleration);
    }
    
    /// <summary>
    /// Get movement vector projected onto the current face plane.
    /// </summary>
    protected Vector3 GetFaceMovement(Vector3 worldDirection)
    {
        // Project the movement onto the face plane (remove the face normal component)
        Vector3 projectedMovement = Vector3.ProjectOnPlane(worldDirection, faceUpDirection);
        return projectedMovement.normalized;
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Set which cube face this controller is currently on.
    /// </summary>
    public virtual void SetCurrentFace(CubeFace face)
    {
        currentFace = face;
        UpdateFaceOrientation();
    }
    
    /// <summary>
    /// Get the current cube face.
    /// </summary>
    public CubeFace GetCurrentFace()
    {
        return currentFace;
    }
    
    /// <summary>
    /// Set movement speed.
    /// </summary>
    public virtual void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    #endregion
    
    #region Debug
    
    protected virtual void DrawDebugInfo()
    {
        // Draw face up direction
        Debug.DrawLine(transform.position, transform.position + faceUpDirection * 1.5f, Color.green);
        
        // Draw gravity direction
        Debug.DrawLine(transform.position, transform.position + faceGravityDirection * 1.5f, Color.red);
        
        // Draw velocity
        Debug.DrawLine(transform.position, transform.position + rigidBody.velocity, Color.blue);
    }
    
    #endregion
}