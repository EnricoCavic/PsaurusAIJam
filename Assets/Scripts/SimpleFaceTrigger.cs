using UnityEngine;
using System;

/// <summary>
/// Simplified direction trigger that detects when player wants to rotate the cube.
/// Each trigger represents a direction to rotate rather than a specific face.
/// Only responds to player, not enemies.
/// </summary>
public class SimpleFaceTrigger : MonoBehaviour
{
    [SerializeField] private RotationDirection triggerDirection;
    [SerializeField] private float cooldownTime = 2f; // Prevent retrigger during rotation
    
    // Events
    public static event Action<RotationDirection> OnDirectionTriggerActivated;
    
    // State
    private float lastTriggerTime = -999f;
    
    /// <summary>
    /// Configure this trigger for a specific rotation direction.
    /// </summary>
    public void Configure(RotationDirection direction)
    {
        triggerDirection = direction;
        gameObject.name = $"DirectionTrigger_{direction}";
    }
    
    /// <summary>
    /// Legacy method for backward compatibility - converts face to direction.
    /// </summary>
    public void Configure(CubeFace face)
    {
        // Convert face to direction for backward compatibility
        RotationDirection direction = ConvertFaceToDirection(face);
        Configure(direction);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Only respond to player, not enemies
        if (!other.CompareTag("Player")) return;
        
        // Check if WorldCube is currently rotating
        if (WorldCube.Instance != null && WorldCube.Instance.IsRotating) return;
        
        // Check cooldown to prevent retrigger
        if (Time.time - lastTriggerTime < cooldownTime) return;
        
        // Check if collider belongs to a player controller (extra safety)
        bool isPlayerController = other.GetComponent<EnhancedPlayerController>() != null;
        
        if (!isPlayerController) return;
        
        lastTriggerTime = Time.time;
        
        Debug.Log($"SimpleFaceTrigger: Player triggered {triggerDirection} rotation");
        
        // Notify the world cube to rotate in this direction
        if (WorldCube.Instance != null)
        {
            WorldCube.Instance.TryRotateInDirection(triggerDirection);
        }
        
        // Notify other systems
        OnDirectionTriggerActivated?.Invoke(triggerDirection);
    }
    
    /// <summary>
    /// Converts legacy face enum to rotation direction.
    /// </summary>
    private RotationDirection ConvertFaceToDirection(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.East: return RotationDirection.Right;
            case CubeFace.West: return RotationDirection.Left;
            case CubeFace.Top: return RotationDirection.Up;
            case CubeFace.Bottom: return RotationDirection.Down;
            default: return RotationDirection.Right; // Default fallback
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw the trigger area
        Gizmos.color = Color.cyan;
        
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(transform.position, sphere.radius);
        }
        
        // Draw direction label and arrow
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, triggerDirection.ToString());
        
        // Draw direction arrow
        Vector3 arrowDirection = GetDirectionVector();
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(arrowDirection), 1f, EventType.Repaint);
        #endif
    }
    
    /// <summary>
    /// Gets the world direction vector for visualization.
    /// </summary>
    private Vector3 GetDirectionVector()
    {
        switch (triggerDirection)
        {
            case RotationDirection.Left: return Vector3.left;
            case RotationDirection.Right: return Vector3.right;
            case RotationDirection.Up: return Vector3.up;
            case RotationDirection.Down: return Vector3.down;
            default: return Vector3.forward;
        }
    }
}