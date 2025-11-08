# WorldCube Setup Guide - Direction-Based Rotation

## Overview
This guide shows how to set up the simplified WorldCube system that uses direction-based rotation instead of face-specific targeting. The cube rotates in response to directional input (left, right, up, down) rather than trying to reach specific faces.

## Scene Setup Steps

### 1. Create the Cube Structure
1. Create an empty GameObject named "WorldCube" 
2. Add the `WorldCube` component to it
3. Create a child GameObject named "CubeRoot"
4. Add your cube mesh/model as a child of "CubeRoot"

### 2. Create Face Turn Detectors (Static - Don't Parent to Cube)
Create 4 empty GameObjects as children of the scene (NOT the cube):
- `FaceTurnDetector_North` - Position north of the cube (triggers Up rotation)
- `FaceTurnDetector_East` - Position east of the cube (triggers Right rotation)  
- `FaceTurnDetector_South` - Position south of the cube (triggers Down rotation)
- `FaceTurnDetector_West` - Position west of the cube (triggers Left rotation)

Each detector should have:
- A trigger collider (Box or Sphere)
- `SimpleFaceTrigger` component (added automatically by WorldCube)

### 3. Create Rotation Points (Parented to CubeRoot)
Create 4 empty GameObjects as children of "CubeRoot":
- `RotationPoint_North` - Center of north face
- `RotationPoint_East` - Center of east face
- `RotationPoint_South` - Center of south face
- `RotationPoint_West` - Center of west face

**Important**: Position these at the center of each face of your cube. They will be used as reference points for player positioning during rotations.

### 4. Create Player
1. Create a Capsule primitive named "Player"
2. Tag it as "Player"
3. Add `SimplePlayerController` component
4. Position on the cube surface

### 5. Configure WorldCube Component
In the WorldCube component inspector:
- Assign CubeRoot to the "Cube Root" field
- Assign each face turn detector to the corresponding field
- Assign each rotation point to the corresponding field
- Set rotation duration (default 1.0 seconds is good)

### 6. Add Test Component (Optional)
Add the `WorldCubeTest` component to any GameObject to get keyboard shortcuts for testing.

## How It Works (Updated)

### Direction-Based Rotation
- **Left/Right**: Rotates cube around Y-axis (horizontal rotation)
- **Up/Down**: Rotates cube around X-axis (vertical rotation)
- Each rotation is exactly 90 degrees in the specified direction
- No predetermined "target faces" - just rotates in the direction requested

### Face Detection Mapping
- **North detector** → triggers **Up rotation** (tilt cube up)
- **East detector** → triggers **Right rotation** (rotate cube right)
- **South detector** → triggers **Down rotation** (tilt cube down)  
- **West detector** → triggers **Left rotation** (rotate cube left)

### Player Position Mapping (Improved)
- Rotation points move with the cube (parented to CubeRoot)
- Player position is stored relative to the **current face's rotation point** before rotation
- After rotation, position is recalculated using the **same rotation point's new world position**
- This maintains the player's relative position on the cube surface during transitions

## Key Changes from Previous Version
- ✅ **Direction-based rotation**: Rotate Left/Right/Up/Down instead of targeting specific faces
- ✅ **Simplified logic**: No complex face transition mappings
- ✅ **Intuitive controls**: Direction matches expected cube movement
- ✅ **Cleaner code**: Removed complex face-to-face transition calculations
- ✅ **Better UX**: User chooses direction rather than destination

## Testing
1. Use WASD to move the player around the cube
2. Walk into detectors to trigger directional rotations:
   - North detector → cube tilts up
   - East detector → cube rotates right
   - South detector → cube tilts down
   - West detector → cube rotates left
3. Use keyboard shortcuts (if WorldCubeTest is added):
   - Q: Rotate Left
   - E: Rotate Right  
   - R: Rotate Up
   - F: Rotate Down
4. Verify player maintains relative position during rotations
5. Check that cube rotates smoothly 90 degrees in each direction

## Rotation Points Usage
The rotation points serve as anchors for player positioning:
1. **Before rotation**: Player position is converted to local space relative to current rotation point
2. **During rotation**: Rotation points move with the cube
3. **After rotation**: Player position is converted back to world space using the same rotation point's new position

This ensures the player "sticks" to the cube face during rotations.

## Troubleshooting
- Make sure player has "Player" tag
- Ensure face detectors have trigger colliders
- Verify rotation points are positioned at the center of each cube face
- Check that CubeRoot is assigned in WorldCube component
- Make sure DOTween is properly imported
- Verify rotation points are properly parented to CubeRoot (they should move with the cube)