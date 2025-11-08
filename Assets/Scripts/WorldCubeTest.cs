using UnityEngine;

/// <summary>
/// Simple test script for the WorldCube direction-based rotation system.
/// Provides keyboard shortcuts to test cube rotations in different directions.
/// </summary>
public class WorldCubeTest : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private bool showInstructions = true;
    
    void Update()
    {
        if (!enableKeyboardShortcuts || WorldCube.Instance == null) return;
        
        // Direction-based rotation controls
        if (Input.GetKeyDown(KeyCode.A))
            WorldCube.Instance.TryRotateInDirection(RotationDirection.Left);
        
        if (Input.GetKeyDown(KeyCode.D))
            WorldCube.Instance.TryRotateInDirection(RotationDirection.Right);
            
        if (Input.GetKeyDown(KeyCode.W))
            WorldCube.Instance.TryRotateInDirection(RotationDirection.Up);
            
        if (Input.GetKeyDown(KeyCode.S))
            WorldCube.Instance.TryRotateInDirection(RotationDirection.Down);
    }
    
    void OnGUI()
    {
        if (!showInstructions) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("WorldCube Direction Test Controls:");
        GUILayout.Label("WASD: Move player");
        GUILayout.Label("Q: Rotate Left");
        GUILayout.Label("E: Rotate Right");
        GUILayout.Label("R: Rotate Up");
        GUILayout.Label("F: Rotate Down");
        GUILayout.Label("");
        
        if (WorldCube.Instance != null)
        {
            GUILayout.Label($"Current Face: {WorldCube.Instance.CurrentFace}");
            GUILayout.Label($"Is Rotating: {WorldCube.Instance.IsRotating}");
        }
        else
        {
            GUILayout.Label("WorldCube not found!");
        }
        
        GUILayout.Label("");
        GUILayout.Label("Walk into detectors to trigger rotations");
        
        GUILayout.EndArea();
    }
}