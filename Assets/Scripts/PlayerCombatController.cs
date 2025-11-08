using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// Handles player auto-combat: target acquisition, attack timing, and projectile firing.
/// Works with PlayerAttributes for stats and integrates with face visibility rules.
/// </summary>
public class PlayerCombatController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint; // Where projectiles spawn from
    
    [Header("Targeting")]
    [SerializeField] private LayerMask enemyLayerMask = -1; // Which layers contain enemies
    [SerializeField] private bool showDebugRange = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableCombat = true;
    [SerializeField] private bool showTargetingDebug = false;
    
    // Combat state
    private float lastAttackTime = 0f;
    private Transform currentTarget = null;
    private EnhancedPlayerController playerController;
    
    // Cache for performance
    private Collider[] enemyColliders = new Collider[50]; // Reusable array for overlap detection
    
    void Awake()
    {
        playerController = GetComponent<EnhancedPlayerController>();
        
        // Validate required components
        if (playerAttributes == null)
        {
            Debug.LogError($"PlayerCombatController: No PlayerAttributes assigned to {gameObject.name}");
        }
        
        if (projectilePrefab == null)
        {
            Debug.LogError($"PlayerCombatController: No projectile prefab assigned to {gameObject.name}");
        }
        
        if (firePoint == null)
        {
            // Use player transform as fallback
            firePoint = transform;
            Debug.LogWarning($"PlayerCombatController: No fire point assigned, using player transform");
        }
    }
    
    void Start()
    {
        // Initialize player health from PlayerAttributes
        InitializePlayerHealth();
        
        // Validate that PlayerAttributes is set and log attack stats
        if (playerAttributes != null)
        {
            Debug.Log($"PlayerCombat: Using attack speed {playerAttributes.AttackSpeed} (cooldown: {playerAttributes.AttackCooldown:F2}s)");
        }
    }
    
    /// <summary>
    /// Initialize the player's HealthController with maxHealth from PlayerAttributes.
    /// </summary>
    private void InitializePlayerHealth()
    {
        if (playerAttributes == null) return;
        
        HealthController healthController = GetComponent<HealthController>();
        if (healthController != null)
        {
            healthController.Initialize(playerAttributes.MaxHealth);
            Debug.Log($"PlayerCombat: Initialized player health to {playerAttributes.MaxHealth}");
        }
        else
        {
            Debug.LogWarning($"PlayerCombatController: No HealthController found on {gameObject.name}");
        }
    }
    
    void Update()
    {
        if (!enableCombat || playerAttributes == null) return;
        
        // Don't attack during face transitions
        if (playerController != null && playerController.IsTransitioning()) return;
        
        HandleAutoAttack();
        
        // Debug face detection periodically
        if (showTargetingDebug && Time.time % 2f < 0.1f) // Every 2 seconds for brief moment
        {
            DebugFaceDetection();
        }
    }
    
    /// <summary>
    /// Main auto-attack logic: find target and attack if cooldown allows.
    /// Only attacks when enemies are in range.
    /// </summary>
    private void HandleAutoAttack()
    {
        // Find nearest enemy target first
        Transform target = FindNearestEnemy();
        
        // Clear current target if no enemies in range
        if (target == null)
        {
            if (currentTarget != null)
            {
                if (showTargetingDebug)
                {
                    Debug.Log("PlayerCombat: No enemies in range, stopping attack");
                }
                currentTarget = null;
            }
            return; // Stop attacking when no enemies are in range
        }
        
        // Check attack cooldown only when we have a target
        if (Time.time - lastAttackTime < playerAttributes.AttackCooldown)
        {
            return;
        }
        
        // Fire at target
        FireProjectile(target);
        lastAttackTime = Time.time;
        currentTarget = target;
        
        if (showTargetingDebug)
        {
            Debug.Log($"PlayerCombat: Attacking {target.name} at distance {Vector3.Distance(transform.position, target.position):F2}");
        }
    }
    
    /// <summary>
    /// Find the nearest enemy within attack range on the current face.
    /// Respects face visibility rules from the cube system.
    /// </summary>
    private Transform FindNearestEnemy()
    {
        // Force update face detection for player to ensure accuracy
        BaseMovementController playerMovement = GetComponent<BaseMovementController>();
        if (playerMovement != null && playerMovement.enabled)
        {
            // Update player's face detection manually to ensure it's current
            if (Time.time % 0.5f < Time.fixedDeltaTime) // Every 0.5 seconds
            {
                playerMovement.UpdateFaceOrientation();
            }
        }
        
        // Use OverlapSphere for efficient range-based detection
        int enemyCount = Physics.OverlapSphereNonAlloc(
            transform.position, 
            playerAttributes.AttackRange, 
            enemyColliders, 
            enemyLayerMask
        );
        
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        int visibleEnemyCount = 0;
        
        for (int i = 0; i < enemyCount; i++)
        {
            Transform enemy = enemyColliders[i].transform;
            
            // Skip if enemy is not visible (different face)
            if (!IsEnemyVisible(enemy))
            {
                continue;
            }
            
            visibleEnemyCount++;
            float distance = Vector3.Distance(transform.position, enemy.position);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        if (showTargetingDebug && enemyCount > 0)
        {
            Debug.Log($"PlayerCombat: Found {enemyCount} enemies in range, {visibleEnemyCount} visible on current face");
        }
        
        return nearestEnemy;
    }
    
    /// <summary>
    /// Check if enemy is visible (on the same face as player).
    /// Integrates with WorldCube face detection system.
    /// </summary>
    private bool IsEnemyVisible(Transform enemy)
    {
        // Basic visibility check - enemy should be active in hierarchy
        if (!enemy.gameObject.activeInHierarchy)
        {
            if (showTargetingDebug)
            {
                Debug.Log($"PlayerCombat: Enemy {enemy.name} not active in hierarchy");
            }
            return false;
        }
        
        // Get current player face from player's BaseMovementController for accuracy
        BaseMovementController playerMovement = GetComponent<BaseMovementController>();
        BaseMovementController enemyMovement = enemy.GetComponent<BaseMovementController>();
        
        if (playerMovement == null || enemyMovement == null)
        {
            if (showTargetingDebug)
            {
                Debug.LogWarning($"PlayerCombat: Missing BaseMovementController - Player: {playerMovement != null}, Enemy {enemy.name}: {enemyMovement != null}");
            }
            return true; // Fallback to visible if components missing
        }
        
        CubeFace playerFace = playerMovement.GetCurrentFace();
        CubeFace enemyFace = enemyMovement.GetCurrentFace();
        
        // Additional validation using position-based detection for accuracy
        CubeFace playerPositionFace = DetectFaceFromPosition(transform.position);
        CubeFace enemyPositionFace = DetectFaceFromPosition(enemy.position);
        
        // Use position-based detection if BaseMovementController values seem inconsistent
        if (playerFace != playerPositionFace && showTargetingDebug)
        {
            Debug.LogWarning($"PlayerCombat: Player face mismatch - BaseMovement: {playerFace}, Position: {playerPositionFace}");
            playerFace = playerPositionFace; // Use position-based as fallback
        }
        
        if (enemyFace != enemyPositionFace && showTargetingDebug)
        {
            Debug.LogWarning($"PlayerCombat: Enemy {enemy.name} face mismatch - BaseMovement: {enemyFace}, Position: {enemyPositionFace}");
            enemyFace = enemyPositionFace; // Use position-based as fallback
        }
        
        bool sameface = playerFace == enemyFace;
        
        if (showTargetingDebug)
        {
            Debug.Log($"PlayerCombat: Player face: {playerFace}, Enemy {enemy.name} face: {enemyFace}, Same face: {sameface}");
        }
        
        return sameface;
    }
    
    /// <summary>
    /// Detect cube face from position using the same logic as BaseMovementController.
    /// Used as a fallback for face detection validation.
    /// </summary>
    private CubeFace DetectFaceFromPosition(Vector3 position)
    {
        Vector3 cubeCenter = WorldCube.Instance != null ? WorldCube.Instance.transform.position : Vector3.zero;
        Vector3 directionFromCenter = (position - cubeCenter).normalized;
        
        // Determine which face based on the largest component
        float absX = Mathf.Abs(directionFromCenter.x);
        float absY = Mathf.Abs(directionFromCenter.y);
        float absZ = Mathf.Abs(directionFromCenter.z);
        
        if (absX > absY && absX > absZ)
        {
            // On East or West face
            return directionFromCenter.x > 0 ? CubeFace.East : CubeFace.West;
        }
        else if (absY > absX && absY > absZ)
        {
            // On Top or Bottom face
            return directionFromCenter.y > 0 ? CubeFace.Top : CubeFace.Bottom;
        }
        else if (absZ > absX && absZ > absY)
        {
            // On North or South face
            return directionFromCenter.z > 0 ? CubeFace.North : CubeFace.South;
        }
        
        // Fallback to North if unable to determine
        return CubeFace.North;
    }
    
    /// <summary>
    /// Fire a projectile at the target.
    /// </summary>
    private void FireProjectile(Transform target)
    {
        if (projectilePrefab == null) return;
        
        // Calculate direction to target
        Vector3 direction = (target.position - firePoint.position).normalized;
        
        // Spawn projectile at fire point
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        
        // Initialize projectile with target and stats
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, playerAttributes.ProjectileSpeed, playerAttributes.Damage, playerAttributes.ProjectileLifetime);
        }
        else
        {
            Debug.LogError("PlayerCombat: Projectile prefab missing Projectile component!");
        }
    }
    
    /// <summary>
    /// Get current attack range for external systems.
    /// </summary>
    public float GetAttackRange()
    {
        return playerAttributes != null ? playerAttributes.AttackRange : 0f;
    }
    
    /// <summary>
    /// Check if player can currently attack (not in cooldown).
    /// </summary>
    public bool CanAttack()
    {
        return enableCombat && (Time.time - lastAttackTime >= playerAttributes.AttackCooldown);
    }
    
    #region Debug Visualization
    
    void OnDrawGizmos()
    {
        if (!showDebugRange || playerAttributes == null) return;
        
        // Draw attack range circle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerAttributes.AttackRange);
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
        
        // Draw fire point
        if (firePoint != null && firePoint != transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
    
    /// <summary>
    /// Debug method to show current face detection states.
    /// </summary>
    private void DebugFaceDetection()
    {
        // Get player face from multiple sources for comparison
        CubeFace worldCubeFace = WorldCube.Instance != null ? WorldCube.Instance.CurrentFace : CubeFace.North;
        
        BaseMovementController playerMovement = GetComponent<BaseMovementController>();
        CubeFace playerFace = playerMovement != null ? playerMovement.GetCurrentFace() : CubeFace.North;
        
        CubeFace playerPositionFace = DetectFaceFromPosition(transform.position);
        
        Debug.Log($"PlayerCombat Face Debug - WorldCube: {worldCubeFace}, Player BaseMovement: {playerFace}, Position-based: {playerPositionFace}");
        
        // Also check nearby enemies
        int enemyCount = Physics.OverlapSphereNonAlloc(
            transform.position, 
            playerAttributes.AttackRange, 
            enemyColliders, 
            enemyLayerMask
        );
        
        for (int i = 0; i < enemyCount && i < 3; i++) // Limit to 3 enemies for readability
        {
            Transform enemy = enemyColliders[i].transform;
            BaseMovementController enemyMovement = enemy.GetComponent<BaseMovementController>();
            if (enemyMovement != null)
            {
                CubeFace enemyFace = enemyMovement.GetCurrentFace();
                CubeFace enemyPositionFace = DetectFaceFromPosition(enemy.position);
                Debug.Log($"PlayerCombat: Enemy {enemy.name} - BaseMovement: {enemyFace}, Position-based: {enemyPositionFace}");
            }
        }
    }
    
    #endregion
}