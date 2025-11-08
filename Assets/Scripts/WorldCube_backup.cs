using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// Wrapper for the world cube structure. Manages a single cube mesh with reference points
/// for rotation and face detection. Much simpler than separate face GameObjects.
/// </summary>
public class WorldCube_Backup : MonoBehaviour
{
    [Header("Cube Configuration")]
    [SerializeField] GameObject cubeRoot;
    [SerializeField] float rotationDuration = 1f;
    
    [Header("Face Turn Detectors (Static - Don't Rotate)")]
    [SerializeField] GameObject faceTurnDetectorNorth;
    [SerializeField] GameObject faceTurnDetectorEast;
    [SerializeField] GameObject faceTurnDetectorSouth;
    [SerializeField] GameObject faceTurnDetectorWest;

    [Header("Rotation Points (Parented to Cube Root - Rotate with Cube)")]        
    [SerializeField] GameObject rotationPointNorth;
    [SerializeField] GameObject rotationPointEast;
    [SerializeField] GameObject rotationPointSouth;
    [SerializeField] GameObject rotationPointWest;
    
    [Header("Debug")]
    [SerializeField] bool showDebugGizmos = true;
    
    // Current state
    private CubeFace currentFace = CubeFace.North;
    private bool isRotating = false;
    private bool isInitialized = false; // Track if initialization is complete
    private Vector3 currentRotation = Vector3.zero;
    
    // Rotation points original positions (for reset after transitions)
    private Vector3[] originalRotationPointPositions = new Vector3[4];
    private bool rotationPointsInitialized = false;
    
    // Events
    public static event Action<CubeFace> OnFaceChanged;
    public static event Action<RotationDirection> OnRotationStarted; 
    public static event Action OnRotationCompleted;
    
    // Singleton for easy access
    public static WorldCube_Backup Instance { get; private set; }
    
    // Rotation amount per direction (90 degrees)
    private const float ROTATION_ANGLE = 90f;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// Initialize the cube system.
    /// </summary>
    private void Initialize()
    {
        if (cubeRoot == null)
        {
            Debug.LogError("WorldCube: cubeRoot not assigned!");
            return;
        }
        
        Debug.Log("WorldCube: Starting initialization...");
        
        // Ensure cube starts at north face position (no rotation)
        cubeRoot.transform.rotation = Quaternion.identity;
        currentRotation = Vector3.zero;
        currentFace = CubeFace.North; // Start with North face, matching identity rotation
        
        // Initialize DOTween to prevent startup issues
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        
        // Store original rotation point positions for reset after transitions
        StoreOriginalRotationPointPositions();
        
        SetupFaceTurnDetectors();
        
        // Mark initialization as complete
        isInitialized = true;
        
        Debug.Log($"WorldCube: Initialized. Current face: {currentFace}");
    }
    
    /// <summary>
    /// Store the original world positions of rotation points for reset after transitions.
    /// </summary>
    private void StoreOriginalRotationPointPositions()
    {
        if (rotationPointNorth != null) originalRotationPointPositions[0] = rotationPointNorth.transform.position;
        if (rotationPointEast != null) originalRotationPointPositions[1] = rotationPointEast.transform.position;
        if (rotationPointSouth != null) originalRotationPointPositions[2] = rotationPointSouth.transform.position;
        if (rotationPointWest != null) originalRotationPointPositions[3] = rotationPointWest.transform.position;
        
        rotationPointsInitialized = true;
        Debug.Log("WorldCube: Stored original rotation point positions");
    }
    
    /// <summary>
    /// Set up the face turn detectors with direction-based triggers.
    /// </summary>
    private void SetupFaceTurnDetectors()
    {
        SetupDetector(faceTurnDetectorNorth, RotationDirection.Up);    // North detector triggers upward rotation
        SetupDetector(faceTurnDetectorEast, RotationDirection.Left);   // East detector triggers left rotation (to show east face)
        SetupDetector(faceTurnDetectorSouth, RotationDirection.Down);  // South detector triggers downward rotation
        SetupDetector(faceTurnDetectorWest, RotationDirection.Right);  // West detector triggers right rotation (to show west face)
    }
    
    /// <summary>
    /// Set up a single detector with appropriate direction trigger.
    /// </summary>
    private void SetupDetector(GameObject detector, RotationDirection direction)
    {
        if (detector == null) return;
        
        // Add transition trigger component if it doesn't exist
        SimpleFaceTrigger trigger = detector.GetComponent<SimpleFaceTrigger>();
        if (trigger == null)
        {
            trigger = detector.AddComponent<SimpleFaceTrigger>();
        }
        
        trigger.Configure(direction);
        
        // Ensure it has a trigger collider
        Collider col = detector.GetComponent<Collider>();
        if (col == null)
        {
            col = detector.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;
    }
    
    /// <summary>
    /// Rotates the cube in the specified direction.
    /// </summary>
    public bool TryRotateInDirection(RotationDirection direction)
    {
        if (!isInitialized)
        {
            Debug.Log("WorldCube: Not yet initialized, ignoring rotation request");
            return false;
        }
        
        if (isRotating)
        {
            Debug.Log("WorldCube: Already rotating, ignoring request");
            return false;
        }
        
        StartCoroutine(RotateInDirectionCoroutine(direction));
        return true;
    }
    
    /// <summary>
    /// Smooth rotation in the specified direction using DOTween.
    /// </summary>
    private IEnumerator RotateInDirectionCoroutine(RotationDirection direction)
    {
        isRotating = true;
        OnRotationStarted?.Invoke(direction);
        
        // Get the current rotation and calculate target rotation
        Quaternion currentQuat = cubeRoot.transform.rotation;
        Quaternion rotationDelta = GetRotationQuaternion(direction);
        Quaternion targetQuat = rotationDelta * currentQuat; // Apply rotation delta to current rotation
        
        Debug.Log($"WorldCube: Rotating {direction}");
        Debug.Log($"WorldCube: Current rotation: {currentQuat.eulerAngles}, Target: {targetQuat.eulerAngles}");
        
        // Use DOTween for smooth rotation with quaternions
        yield return cubeRoot.transform.DORotateQuaternion(targetQuat, rotationDuration)
            .SetEase(Ease.OutCubic)
            .WaitForCompletion();
        
        // Update state - store the final rotation
        currentRotation = cubeRoot.transform.rotation.eulerAngles;
        UpdateCurrentFace(direction);
        
        // Reset rotation points to original positions after cube rotation completes
        ResetRotationPointsToOriginalPositions();
        
        isRotating = false;
        
        OnRotationCompleted?.Invoke();
        OnFaceChanged?.Invoke(currentFace);
        
        Debug.Log($"WorldCube: Rotation complete. Now showing face: {currentFace}");
    }
    
    /// <summary>
    /// Gets the rotation quaternion for a given direction.
    /// This ensures clean rotations around specific axes without gimbal lock.
    /// </summary>
    private Quaternion GetRotationQuaternion(RotationDirection direction)
    {
        switch (direction)
        {
            case RotationDirection.Left:
                return Quaternion.AngleAxis(-ROTATION_ANGLE, Vector3.forward);  // Rotate left around Z-axis
            case RotationDirection.Right:
                return Quaternion.AngleAxis(ROTATION_ANGLE, Vector3.forward);   // Rotate right around Z-axis
            case RotationDirection.Up:
                return Quaternion.AngleAxis(-ROTATION_ANGLE, Vector3.right);    // Rotate up around X-axis
            case RotationDirection.Down:
                return Quaternion.AngleAxis(ROTATION_ANGLE, Vector3.right);     // Rotate down around X-axis
            default:
                return Quaternion.identity;
        }
    }
    
    /// <summary>
    /// Updates the current face based on the rotation direction.
    /// </summary>
    private void UpdateCurrentFace(RotationDirection direction)
    {
        switch (direction)
        {
            case RotationDirection.Left:
                currentFace = RotateFaceHorizontally(currentFace, -1);
                break;
            case RotationDirection.Right:
                currentFace = RotateFaceHorizontally(currentFace, 1);
                break;
            case RotationDirection.Up:
                currentFace = RotateFaceVertically(currentFace, true);
                break;
            case RotationDirection.Down:
                currentFace = RotateFaceVertically(currentFace, false);
                break;
        }
    }
    
    /// <summary>
    /// Rotates face horizontally (left/right).
    /// </summary>
    private CubeFace RotateFaceHorizontally(CubeFace current, int direction)
    {
        if (current == CubeFace.Top || current == CubeFace.Bottom)
            return current; // Up and Down don't change with horizontal rotation
            
        int[] horizontalCycle = { (int)CubeFace.North, (int)CubeFace.East, (int)CubeFace.South, (int)CubeFace.West };
        int currentIndex = System.Array.IndexOf(horizontalCycle, (int)current);
        
        if (currentIndex >= 0)
        {
            int newIndex = (currentIndex + direction + 4) % 4;
            return (CubeFace)horizontalCycle[newIndex];
        }
        
        return current;
    }
    
    /// <summary>
    /// Rotates face vertically (up/down).
    /// </summary>
    private CubeFace RotateFaceVertically(CubeFace current, bool up)
    {
        if (up)
        {
            switch (current)
            {
                case CubeFace.North: return CubeFace.Top;
                case CubeFace.East: return CubeFace.Top;
                case CubeFace.South: return CubeFace.Top;
                case CubeFace.West: return CubeFace.Top;
                case CubeFace.Top: return CubeFace.South;
                case CubeFace.Bottom: return CubeFace.North;
                default: return current;
            }
        }
        else
        {
            switch (current)
            {
                case CubeFace.North: return CubeFace.Bottom;
                case CubeFace.East: return CubeFace.Bottom;
                case CubeFace.South: return CubeFace.Bottom;
                case CubeFace.West: return CubeFace.Bottom;
                case CubeFace.Top: return CubeFace.North;
                case CubeFace.Bottom: return CubeFace.South;
                default: return current;
            }
        }
    }
    
    /// <summary>
    /// Get the rotation point for the current face (for player positioning).
    /// </summary>
    public Transform GetCurrentRotationPoint()
    {
        return GetRotationPointForFace(currentFace);
    }
    
    /// <summary>
    /// Get rotation point for a specific face.
    /// </summary>
    public Transform GetRotationPointForFace(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.North: return rotationPointNorth?.transform;
            case CubeFace.East: return rotationPointEast?.transform;
            case CubeFace.South: return rotationPointSouth?.transform;
            case CubeFace.West: return rotationPointWest?.transform;
            default: return rotationPointNorth?.transform; // Fallback to north
        }
    }
    
    /// <summary>
    /// Get the nearest rotation point to the player for orbiting during transitions.
    /// </summary>
    public Transform GetNearestRotationPoint(Vector3 playerPosition)
    {
        Transform nearestPoint = null;
        float nearestDistance = float.MaxValue;
        
        Transform[] rotationPoints = {
            rotationPointNorth?.transform,
            rotationPointEast?.transform,
            rotationPointSouth?.transform,
            rotationPointWest?.transform
        };
        
        foreach (Transform point in rotationPoints)
        {
            if (point == null) continue;
            
            float distance = Vector3.Distance(playerPosition, point.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = point;
            }
        }
        
        return nearestPoint;
    }
    
    /// <summary>
    /// Reset rotation points to their original global positions after cube rotation.
    /// This allows them to be used again for the next transition.
    /// </summary>
    public void ResetRotationPointsToOriginalPositions()
    {
        if (!rotationPointsInitialized) return;
        
        if (rotationPointNorth != null) rotationPointNorth.transform.position = originalRotationPointPositions[0];
        if (rotationPointEast != null) rotationPointEast.transform.position = originalRotationPointPositions[1];
        if (rotationPointSouth != null) rotationPointSouth.transform.position = originalRotationPointPositions[2];
        if (rotationPointWest != null) rotationPointWest.transform.position = originalRotationPointPositions[3];
        
        Debug.Log("WorldCube: Reset rotation points to original positions");
    }
    
    // Public properties
    public CubeFace CurrentFace => currentFace;
    public bool IsRotating => isRotating;
    public GameObject CubeRoot => cubeRoot;
    public float RotationDuration => rotationDuration;
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw cube outline
        if (cubeRoot != null)
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = cubeRoot.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        // Draw face detectors
        Gizmos.color = Color.yellow;
        DrawDetectorGizmo(faceTurnDetectorNorth, "N");
        DrawDetectorGizmo(faceTurnDetectorEast, "E");
        DrawDetectorGizmo(faceTurnDetectorSouth, "S");
        DrawDetectorGizmo(faceTurnDetectorWest, "W");
        
        // Draw rotation points
        Gizmos.color = Color.green;
        DrawRotationPointGizmo(rotationPointNorth, "RN");
        DrawRotationPointGizmo(rotationPointEast, "RE");
        DrawRotationPointGizmo(rotationPointSouth, "RS");
        DrawRotationPointGizmo(rotationPointWest, "RW");
    }
    
    private void DrawDetectorGizmo(GameObject detector, string label)
    {
        if (detector != null)
        {
            Gizmos.DrawWireSphere(detector.transform.position, 0.5f);
            UnityEditor.Handles.Label(detector.transform.position, label);
        }
    }
    
    private void DrawRotationPointGizmo(GameObject rotPoint, string label)
    {
        if (rotPoint != null)
        {
            Gizmos.DrawSphere(rotPoint.transform.position, 0.3f);
            UnityEditor.Handles.Label(rotPoint.transform.position, label);
        }
    }
}
