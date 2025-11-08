# Multiple Enemy Prefab Setup Guide

## Overview
This guide explains how to create multiple enemy types with different visuals but shared behavior using prefab variants.

## Current Implementation
- **EnemyMovementController.cs**: Shared behavior script for all enemies
- **EnemyManager.cs**: Enhanced to support multiple enemy prefabs with random selection
- **EnemyData.cs**: ScriptableObject for future stat variations (optional)

## Setup Steps

### 1. Create Base Enemy Prefab (if not already done)
1. Create a GameObject in the scene
2. Add required components:
   - `EnemyMovementController` script
   - `HealthController` component  
   - `Animator` component
   - `Rigidbody` component
   - Collider component (for physics/triggers)
3. Add visual elements (mesh, materials, etc.)
4. Drag to Assets folder to create prefab
5. Name it `BaseEnemy` or `Enemy_Type1`

### 2. Create Enemy Prefab Variants
1. **Right-click the base enemy prefab** in Assets folder
2. **Select "Create > Prefab Variant"**
3. **Name the variant** (e.g., `Enemy_Fast`, `Enemy_Heavy`, `Enemy_Ranged`)
4. **Double-click to edit the variant**
5. **Modify visual elements**:
   - Change mesh renderer materials/colors
   - Swap mesh filters for different models
   - Adjust scale for size variations
   - Add/remove visual effects
6. **Adjust stats in inspector**:
   - Modify attack damage, health, speed in EnemyMovementController
   - Configure HealthController max health
7. **Save the variant**

### 3. Configure EnemyManager
1. Select the EnemyManager GameObject in scene
2. In the inspector, find "Enemy Prefabs" section
3. **Set array size** to number of enemy types you want
4. **Assign prefab variants** to each slot in the array
5. **Set fallback enemy prefab** (optional backup)
6. **Enable "Use Random Enemy Types"** for variety

### 4. Animation Setup (For Each Prefab)
Each enemy prefab should have an Animator with these parameters:
- **`attack`** (bool) - triggers attack animation
- **`hit`** (bool) - triggers hit reaction animation  
- **`death`** (trigger) - triggers death animation
- **Walking animation** - automatic based on movement

#### Animation Events Setup:
1. Open the **Attack Animation** in Animation window
2. Add **Animation Event** at the frame where damage should occur
3. Set event function to: **`DealDamageToPlayer`**
4. This connects to the method in EnemyMovementController

### 5. Component Requirements (Each Prefab Must Have)
- ✅ **EnemyMovementController** - Combat and movement behavior
- ✅ **HealthController** - Health management and death handling
- ✅ **Animator** - Animation control with proper parameters
- ✅ **Rigidbody** - Physics movement
- ✅ **Collider** - Trigger/collision detection
- ✅ **MeshRenderer** + **MeshFilter** - Visual representation

## Example Enemy Type Ideas

### Enemy Type 1: Basic Grunt
- **Visual**: Default mesh, gray material
- **Stats**: 50 HP, 20 damage, normal speed
- **Behavior**: Standard chase and attack

### Enemy Type 2: Fast Runner  
- **Visual**: Smaller scale, red material
- **Stats**: 30 HP, 15 damage, fast speed
- **Behavior**: Quick attacks, lower damage

### Enemy Type 3: Heavy Tank
- **Visual**: Larger scale, dark material
- **Stats**: 100 HP, 35 damage, slow speed
- **Behavior**: High damage, slow but durable

### Enemy Type 4: Elite
- **Visual**: Different mesh, special material/effects
- **Stats**: 80 HP, 30 damage, normal speed
- **Behavior**: Higher stats overall

## Testing the System
1. **Start the game** with multiple enemy prefabs assigned
2. **Check spawn variety**: Different enemy types should spawn randomly
3. **Test combat**: All enemies should attack and take damage properly
4. **Test animations**: Attack, hit, and death animations should work
5. **Test balance**: Adjust stats for fair gameplay

## Troubleshooting

### Issue: All enemies look the same
- **Solution**: Check that you assigned different prefab variants to EnemyManager array slots

### Issue: Enemies not attacking
- **Solution**: Verify each prefab has Animator with correct parameters and Animation Events

### Issue: Enemies not taking damage  
- **Solution**: Ensure each prefab has HealthController component

### Issue: Animation not playing
- **Solution**: Check Animator Controller is assigned and parameters match script

## Future Enhancements (Optional)
1. **EnemyData ScriptableObjects**: Create data assets for easier stat management
2. **Weighted Spawning**: Different spawn rates for different enemy types
3. **Face-Specific Enemies**: Certain enemies only spawn on specific cube faces
4. **Special Abilities**: Unique behaviors per enemy type

---

**Current Status**: Ready for multiple enemy prefab creation and testing!