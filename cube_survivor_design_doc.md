# Cube Survivor - Design Document
## Playsaurus Game Jam Prototype

---

## 1. GAME CONCEPT

**Genre:** Idle Auto-Battler / Survivor-like

**Core Hook:** Navigate a cube-shaped world where each face is a battlefield. Automatically attack enemies while managing positioning and face transitions. Progress through increasingly difficult biomes with exponential scaling.

**Target Prototype Duration:** 1 day development

---

## 2. CORE GAMEPLAY LOOP

1. Player spawns on a cube face (north-facing)
2. Player moves using analog stick to position themselves
3. Character automatically attacks nearby enemies with ranged weapon
4. Enemies spawn, move toward player, and attack on contact
5. Defeated enemies drop currency
6. Player can traverse to adjacent cube faces by reaching edges
7. Level completes when enemy kill count quota is reached
8. Player returns to level selector and spends currency on permanent upgrades
9. Player attempts next biome or retries current biome with better stats

---

## 3. WORLD & NAVIGATION

### Cube Structure
- World consists of a cube with 6 faces
- Each face is a playable battlefield
- Player always appears to be on the "north" face from camera perspective

### Face Transition System
- **Trigger:** Player walks to edge of current face
- **Behavior:** 
  - Cube rotates to bring the adjacent face to north position
  - Player seamlessly continues movement onto new face
  - Camera remains fixed in world space
  - Transition should feel smooth and intuitive

### Enemy Visibility Rules
- Enemies exist on all cube faces simultaneously
- Enemies are **hidden/invisible** when on faces other than the current player face
- Enemies become **visible** when they are on the same face as the player
- Enemies **can transition between faces** following same rules as player
- Hidden enemies still move and navigate, but cannot damage player or be damaged

---

## 4. LEVEL STRUCTURE

### Biome/Cube System
- Each cube represents a biome (Level 1, Level 2, etc.)
- Each biome has unique visual theme and difficulty parameters

### Win Condition
- **Primary Goal:** Reach target enemy kill count
- Kill counter displays on HUD
- When quota reached, level ends and player returns to level selector

### Progression
- **Unlocking:** Beat previous biome to unlock next biome
- **Currency Rewards:** Earn currency for each attempt (win or lose)
- **Meta-Progression:** Spend currency on permanent upgrades between runs
- No permadeath - player keeps upgrades between attempts

### Prototype Scope
- **Target:** 1-2 biomes functional
- Focus on core mechanics over content variety

---

## 5. COMBAT SYSTEM

### Player Combat
- **Auto-Attack:** Character automatically shoots at nearest enemy in range
- **Weapon Type:** Single ranged projectile weapon for prototype
- **No Manual Aiming:** System handles target acquisition
- **Attack Frequency:** Determined by Attack Speed attribute

### Enemy Combat
- **Contact Damage:** Enemies damage player by touching them
- **Movement:** Enemies pathfind toward player
- **AI Behavior:** Simple chase behavior sufficient for prototype

### Damage System
- Track player health
- Track enemy health
- Apply damage on hit (projectiles for player, collision for enemies)
- Death state for both player and enemies

---

## 6. CHARACTER ATTRIBUTES

### Core Stats
Players can upgrade these attributes between runs:

1. **Damage** - How much damage attacks deal
2. **Range** - How far projectiles travel / attack detection radius
3. **Attack Speed** - How frequently attacks occur
4. **Movement Speed** - How fast player moves
5. **Health** - Maximum hit points

### Scaling Philosophy
- Exponential growth in both player power and enemy difficulty
- Later biomes require significant investment in upgrades
- Currency inflation matches power scaling

---

## 7. ECONOMY & PROGRESSION

### Single Currency System
- **Source:** Dropped by defeated enemies
- **Collection:** Automatic pickup or collision-based
- **Usage:** Upgrade attributes in hub/level selector

### Upgrade Hub
- Accessible from level selector screen
- Display current attribute levels
- Show upgrade costs (increasing per level)
- Apply upgrades permanently

### Meta-Progression Goals
- Encourage repeated attempts at difficult biomes
- Reward incremental progress even on failed runs
- Create "one more run" loop

---

## 8. ENEMY SYSTEM

### Spawn System
- Enemies spawn at designated points or randomly on faces
- Spawn rate increases over time or based on kill count
- Consider spawn limits to maintain performance

### Enemy Types
- **Prototype Scope:** 2-3 enemy types maximum
- Different enemies = different parameters (health, speed, damage)
- Can reuse same model with color variations

### Despawn Rules
- Enemies despawn when killed
- Optional: Despawn enemies too far from player to manage performance

---

## 9. TECHNICAL IMPLEMENTATION PRIORITIES

### Phase 1: Foundation (Critical)
1. **World Rotation System**
   - Cube geometry setup
   - Face transition triggers
   - Smooth rotation animation
   - Player position mapping across transitions

2. **Input System**
   - Analog stick / WASD movement input
   - Movement vector calculation
   - Input smoothing

3. **Character Movement**
   - Basic movement controller
   - Collision with cube face boundaries
   - Edge detection for face transitions

### Phase 2: Core Gameplay
4. **Movement with Rotation**
   - Maintain player position during cube rotation
   - Ensure controls remain intuitive during transition
   - Handle coordinate system transformation

5. **Character Attributes System**
   - Create data structure for stats
   - Apply attributes to character behavior
   - Upgrade system backend

6. **Damage System**
   - Health tracking for player and enemies
   - Damage application
   - Death handling

### Phase 3: Combat Loop
7. **Character Auto-Attack**
   - Target acquisition (find nearest enemy)
   - Projectile spawning
   - Attack cooldown based on Attack Speed
   - Range checking

8. **Enemy Behavior**
   - Pathfinding to player
   - Contact damage application
   - Basic AI state machine

### Phase 4: Enemy Management
9. **Spawn System**
   - Enemy spawner locations
   - Spawn timing/waves
   - Enemy pool management

10. **Despawn System**
    - Death animations/effects
    - Remove from scene
    - Drop currency on death

### Phase 5: Polish (If Time Allows)
- UI for health, kill count, currency
- Visual feedback for hits
- Audio cues
- Particle effects

---

## 10. MINIMUM VIABLE PROTOTYPE (MVP)

### Must Have:
- ✓ Playable cube with face transitions working
- ✓ Player movement with analog stick
- ✓ Auto-attacking player character
- ✓ 1-2 enemy types that chase and damage player
- ✓ Kill count tracking and level completion
- ✓ Currency drops and collection
- ✓ Basic upgrade system (at least 2-3 attributes)
- ✓ 1 functional biome start to finish

### Nice to Have:
- Second biome with different difficulty
- More enemy variety
- Visual polish
- Sound effects
- More sophisticated spawning patterns

### Can Skip:
- Multiple weapon types
- Skill trees
- Boss encounters
- Elaborate UI
- Extensive particle effects
- Complex enemy AI

---

## 11. DESIGN RISKS & SOLUTIONS

### Risk: Face Transition Disorientation
**Solution:** 
- Keep transitions smooth and quick
- Maintain player's relative position
- Consider visual guide (compass, indicator)

### Risk: Hidden Enemies Feel Unfair
**Solution:**
- Ensure enemies are visible before attacking
- Add sound cues for nearby hidden enemies (optional)
- Balance spawn rates to prevent overwhelming one face

### Risk: Scope Creep on 1-Day Timeline
**Solution:**
- Ruthlessly prioritize Phase 1-4 implementation list
- Cut features rather than polish
- Accept placeholder art/audio
- Focus on "playable and fun" over "pretty"

### Risk: Exponential Scaling Balance
**Solution:**
- Use simple multipliers for prototype (2x, 4x, 8x)
- Tune after core loop is functional
- Make upgrade costs and enemy stats easily tweakable

---

## 12. SUCCESS METRICS FOR PROTOTYPE

A successful prototype demonstrates:

1. **Core Loop Clarity** - Players understand: move, shoot, collect, upgrade
2. **Face Transition** - Cube rotation feels natural and intuitive
3. **Progression Feel** - Upgrades create noticeable power increase
4. **Challenge Curve** - Each biome feels harder but achievable with upgrades
5. **"One More Run"** - Desire to retry after failure/completion

---

## 13. POST-JAM EXPANSION IDEAS
(Not for prototype, but document for future reference)

- Idle/offline progress mechanics
- Multiple weapon types and attack patterns
- Prestige system
- Boss encounters on specific cube faces
- Synergies between upgrades
- Achievement system
- Daily challenges
- More biome variety (6+ biomes)
- Co-op multiplayer (both players on same cube)

---

## QUICK REFERENCE: Key Mechanics Summary

| Mechanic | Implementation |
|----------|----------------|
| **Movement** | Analog stick, free 2D movement on cube face |
| **Face Transition** | Walk to edge → cube rotates → continue on new face |
| **Combat** | Auto-target nearest enemy, shoot projectile |
| **Enemy Damage** | Contact-based collision damage |
| **Enemy Visibility** | Only visible on player's current face |
| **Win Condition** | Kill X enemies per level |
| **Progression** | Currency → upgrades → retry harder levels |
| **Attributes** | Damage, Range, Attack Speed, Move Speed, Health |
| **Scope** | 1-2 biomes, 2-3 enemy types, 1 weapon type |

---

**Document Version:** 1.0  
**Last Updated:** Game Jam Day 1  
**Status:** Ready for Implementation

