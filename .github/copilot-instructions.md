# AI Coding Instructions: Cube Survivor Game Jam Project

## Copilot task instructions
You are building a Unity game jam prototype with a lot of different systems and steps. These are going to be separated into different tasks and we are going to keep track of these tasks in a living .md document for each of them. Each task file will be named according to the system or feature being implemented. After making any progress on a task, update the corresponding .md document to reflect the current state of that system or feature. This is made to ensure the copilot always has the latest context on what has been done and what still needs to be done.

## Best practices
- Use clear and descriptive names for all variables, functions, and classes.
- Write modular, reusable code with a focus on single responsibility.
- Include comments and documentation for complex logic and important decisions.
- Follow Unity best practices for scene organization, asset management, and component usage.

## Project Overview
This is a Unity 2022.3.62f2 game jam prototype for "Cube Survivor" - an idle auto-battler where players navigate a cube-shaped world with 6 faces as battlefields. Core mechanics include face transitions, auto-combat, and meta-progression.

## Architecture & Core Systems

### Cube World Navigation System
- **Primary Challenge**: World consists of a 6-faced cube where each face is a battlefield
- **Face Transition Logic**: Player walks to edge → cube rotates → player continues on new face
- **Camera Strategy**: Camera stays fixed in world space while cube rotates to bring new face to "north" position
- **Key Implementation**: Use DOTween (already included) for smooth cube rotation animations
- **Coordinate Mapping**: Player position must be maintained during cube rotations

### Enemy Visibility Rules (Critical Design Pattern)
- Enemies exist on ALL faces simultaneously but are only visible on player's current face
- Hidden enemies still move/navigate but cannot damage player or be damaged
- This prevents overwhelming gameplay while maintaining world persistence
- Implementation: Toggle enemy renderer/collider components based on current face

### Auto-Combat System
- Player automatically targets nearest visible enemy
- Projectile-based ranged attacks with attributes: Damage, Range, Attack Speed
- No manual aiming - system handles target acquisition
- Enemies use contact damage (collision-based)

## Development Workflow

### Unity Project Structure
- **Scripts**: `Assets/Scripts/` (currently minimal - only `CubeRotation.cs` stub)
- **Scenes**: `Assets/Scenes/SampleScene.unity`
- **External Dependencies**: DOTween for animations (in `Assets/Plugins/Demigiant/`)
- **Build Target**: Standard Unity build process

### Critical Implementation Phases (Per Design Doc)
1. **Phase 1**: World rotation system with face transitions
2. **Phase 2**: Character movement with cube rotation handling
3. **Phase 3**: Auto-attack and enemy AI
4. **Phase 4**: Spawn/despawn systems with currency drops

### Game Jam Constraints
- **1-day development timeline** - prioritize MVP over polish
- Target: 1-2 biomes, 2-3 enemy types, basic upgrade system
- Cut scope aggressively: no multiple weapons, complex AI, or elaborate UI

## Code Patterns & Conventions

### Component Architecture
- **Manager Classes**: Use singleton pattern for all manager classes (GameManager, EnemyManager, etc.)
- **Script Communication**: Use C# Actions for event-driven communication between scripts
- **Data Management**: Use ScriptableObjects for all data that fits - player attributes, enemy stats, upgrade definitions, biome configurations
- **Core Systems**: Leverage Unity's built-in systems: Physics, Colliders, Renderers

### Performance Considerations (Early Prototype)
- Target platform is mobile but performance optimization is not critical for prototype
- Focus on clean, readable code over optimization
- Enemy pool management for spawn/despawn (when implementing)
- Hide/show enemies rather than destroy/instantiate for face transitions
- Use object pooling for projectiles (if performance becomes an issue)

### Data Architecture
- **Player Attributes**: ScriptableObject with Damage, Range, Attack Speed, Movement Speed, Health
- **Enemy Data**: ScriptableObjects for different enemy types and their stats
- **Biome Configuration**: ScriptableObjects for level parameters and difficulty scaling
- **Single Currency System**: Dropped by enemies, managed through events
- **Exponential Scaling**: Data-driven through ScriptableObjects for easy balancing

## Integration Points

### DOTween Usage
- Already integrated for smooth cube rotation animations
- Use `transform.DORotate()` for face transitions
- Consider easing curves for natural feel

### Unity Physics
- Collision detection for enemy damage (contact-based)
- Projectile physics for player attacks
- Trigger zones for face transition detection

### Scene Management
- Level selector hub for upgrade purchases
- Biome scenes for gameplay
- Persistent player data between scenes using ScriptableObjects

### Event-Driven Architecture
- Use C# Actions for manager-to-manager communication
- Example patterns: `GameManager.OnPlayerDeath`, `EnemyManager.OnEnemyKilled`, `CubeRotation.OnFaceTransition`
- Keep singleton managers decoupled through event subscriptions

### Mobile Platform Considerations
- Target platform is mobile but early prototype prioritizes functionality over optimization
- Use Unity's Input System for touch/accelerometer input when needed
- UI should be designed for touch interaction (larger buttons, gesture-friendly) using ScriptableObjects

### Event-Driven Architecture
- Use C# Actions for manager-to-manager communication
- Example patterns: `GameManager.OnPlayerDeath`, `EnemyManager.OnEnemyKilled`, `CubeRotation.OnFaceTransition`
- Keep singleton managers decoupled through event subscriptions

### Mobile Platform Considerations
- Target platform is mobile but early prototype prioritizes functionality over optimization
- Use Unity's Input System for touch/accelerometer input when needed
- UI should be designed for touch interaction (larger buttons, gesture-friendly)

## Testing & Validation

### Core Mechanics Validation
- Face transitions feel smooth and intuitive
- Player maintains spatial awareness during cube rotation
- Auto-attack targeting works reliably
- Currency collection and upgrade system functional

### Performance Targets
- Maintain 60 FPS with enemies on all 6 faces
- Smooth cube rotation without frame drops
- Responsive player movement during transitions

## Key Files to Reference
- `cube_survivor_design_doc.md` - Complete design specification
- `Assets/Scripts/CubeRotation.cs` - Primary system implementation (currently empty)
- `Assets/Resources/DOTweenSettings.asset` - Animation configuration
- `ProjectSettings/` - Unity project configuration

## Development Priorities
Focus on getting the cube rotation and face transition system working first - this is the core differentiator. Everything else (combat, enemies, upgrades) follows standard Unity patterns but the world navigation system is unique and complex.