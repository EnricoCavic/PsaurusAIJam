using UnityEngine;

/// <summary>
/// Defines the directions for cube rotation.
/// The cube rotates around its center to show different faces.
/// </summary>
public enum RotationDirection
{
    Left,      // Rotate cube left (counterclockwise around Y-axis)
    Right,     // Rotate cube right (clockwise around Y-axis)  
    Up,        // Rotate cube up (around X-axis to show top)
    Down       // Rotate cube down (around X-axis to show bottom)
}

/// <summary>
/// Defines the six faces of the cube world for reference.
/// Used for tracking current orientation and state.
/// </summary>
public enum CubeFace
{
    North = 0,  // Default starting face (facing camera)
    East = 1,   // Right side
    South = 2,  // Back side
    West = 3,   // Left side  
    Top = 4,     // Top side
    Bottom = 5    // Bottom side
}