using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy movement controller with animation-driven attack system.
/// Moves toward player, plays attack animation when in range, handles death and hit feedback.
/// </summary>
public class EnemyMovementController : BaseMovementController
{
    [Header("Enemy Configuration")]
    // TODO: Add EnemyData ScriptableObject support later
    // [SerializeField] private EnemyData enemyData; // ScriptableObject for enemy stats
    // [SerializeField] private bool useEnemyData = true; // Toggle to use data or inspector values
    
    [Header("Enemy Behavior")]
    [SerializeField] private float detectionRange = 50f; // Increased from 10f
    [SerializeField] private float stopDistance = 0.2f;    // Increased from 1f
    [SerializeField] private float moveSpeedMultiplier = 0.1f; // Full speed for testing
    
    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("Animation")]
    [SerializeField] private bool showCombatDebug = false;
    
    // Target tracking
    private Transform playerTransform;
    
    // Combat state
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private bool isDead = false;
    
    // Components
    private Animator enemyAnimator;
    private Transform enemyVisualTransform; // The child object to rotate  
    private HealthController enemyHealth;
    
    #region Unity Lifecycle
    
    protected override void Start()
    {
        // Debug.Log($"Enemy {gameObject.name}: Starting initialization");
        base.Start();
        InitializeEnemyComponents();
        FindPlayer();
        
        // Debug component status
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Components - Animator: {enemyAnimator != null}, Health: {enemyHealth != null}, Player: {playerTransform != null}");
        }
        
        // Automatically parent to cube for rotation handling
        ParentToCube();
    }
    
    /// <summary>
    /// Initialize enemy-specific components and event subscriptions.
    /// </summary>
    private void InitializeEnemyComponents()
    {
        // Get animator component
        enemyAnimator = GetComponentInChildren<Animator>();
        if (enemyAnimator == null)
        {
            Debug.LogWarning($"Enemy {gameObject.name}: No Animator component found!");
        }
        else
        {
            // Cache the visual transform (the child object with the animator)
            enemyVisualTransform = enemyAnimator.transform;
            Debug.Log($"Enemy {gameObject.name}: Cached visual transform: {enemyVisualTransform.name}");
        }
        
        // Get health controller component
        enemyHealth = GetComponent<HealthController>();
        if (enemyHealth == null)
        {
            Debug.LogWarning($"Enemy {gameObject.name}: No HealthController component found!");
        }
        else
        {
            // Subscribe to health events
            enemyHealth.OnDeathLocal += HandleDeath;
            enemyHealth.OnHealthChangedLocal += HandleDamage;
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from health events
        if (enemyHealth != null)
        {
            enemyHealth.OnDeathLocal -= HandleDeath;
            enemyHealth.OnHealthChangedLocal -= HandleDamage;
        }
    }
    
    void Update()
    {
        // Don't do anything if dead
        if (isDead) return;
        
        // Simple movement - move toward player or attack if close enough
        if (playerTransform != null)
        {
            HandleCombatBehavior();
        }
        else
        {
            if (showCombatDebug)
            {
                Debug.LogWarning($"Enemy {gameObject.name}: No player transform found, trying to find player...");
            }
            FindPlayer();
        }
    }
    
    /// <summary>
    /// Handle enemy combat behavior: movement toward player or attack when in range.
    /// </summary>
    private void HandleCombatBehavior()
    {
        if (playerTransform == null || rigidBody == null || isDead) return;
        
        Vector3 directionToPlayer = (playerTransform.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // Always face the player during combat (only rotate visual child)
        if (directionToPlayer != Vector3.zero && enemyVisualTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
            enemyVisualTransform.rotation = Quaternion.Slerp(enemyVisualTransform.rotation, targetRotation, Time.deltaTime * 8f);
        }
        
        // Check if player is within attack range and cooldown is ready
        if (distanceToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            StartAttack();
        }
        // If player is out of attack range, stop attacking and chase
        else if (distanceToPlayer > attackRange)
        {
            if (isAttacking)
            {
                StopAttack();
            }
            HandleMovement(); // Move toward player
        }
        // If we're attacking but still in range, stay still
        else if (isAttacking)
        {
            // Stay still during attack
            rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);
        }
        
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Distance: {distanceToPlayer:F2}, Attack Range: {attackRange}, Attacking: {isAttacking}, Cooldown Ready: {Time.time - lastAttackTime >= attackCooldown}");
        }
    }
    
    /// <summary>
    /// Start attack sequence - trigger attack animation.
    /// </summary>
    private void StartAttack()
    {
        if (isDead || isAttacking) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool("attack", true);
        }
        
        // Start coroutine to auto-stop attack after animation duration
        StartCoroutine(AutoStopAttack());
        
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Starting attack!");
        }
    }
    
    /// <summary>
    /// Stop attack sequence - stop attack animation.
    /// </summary>
    private void StopAttack()
    {
        if (!isAttacking) return;
        
        isAttacking = false;
        
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool("attack", false);
        }
        
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Stopping attack");
        }
    }
    
    /// <summary>
    /// Called by Animation Event during attack animation to deal damage to player.
    /// This method should be called at the appropriate frame in the attack animation.
    /// </summary>
    public void DealDamageToPlayer()
    {
        if (isDead || playerTransform == null) return;
        
        // Check if player is still in range (they might have moved away during animation)
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= attackRange)
        {
            // Find player's HealthController and deal damage
            HealthController playerHealth = playerTransform.GetComponent<HealthController>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                
                if (showCombatDebug)
                {
                    Debug.Log($"Enemy {gameObject.name}: Dealt {attackDamage} damage to player!");
                }
            }
            else
            {
                Debug.LogWarning($"Enemy {gameObject.name}: Player has no HealthController component!");
            }
        }
        else if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Player moved out of range during attack animation");
        }
    }
    
    /// <summary>
    /// Handle enemy death - trigger death animation and cleanup.
    /// </summary>
    private void HandleDeath()
    {
        if (isDead) return;
        
        isDead = true;
        isAttacking = false;
        
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Died, triggering death animation");
        }
        
        // Stop movement
        if (rigidBody != null)
        {
            rigidBody.velocity = Vector3.zero;
        }
        
        // Trigger death animation
        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger("death");
        }
        
        // Start coroutine to wait for death animation and cleanup
        StartCoroutine(DeathSequence());
    }
    
    /// <summary>
    /// Handle damage taken - trigger hit animation.
    /// </summary>
    private void HandleDamage(float newHealth)
    {
        if (isDead) return;
        
        // Trigger hit animation briefly
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool("hit", true);
            // Reset hit bool after a short delay
            StartCoroutine(ResetHitAnimation());
        }
        
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Took damage, health now: {newHealth}");
        }
    }
    
    /// <summary>
    /// Coroutine to handle death sequence - wait for animation then destroy.
    /// </summary>
    private System.Collections.IEnumerator DeathSequence()
    {
        // Wait for death animation to complete (assume 2 seconds max)
        yield return new WaitForSeconds(2f);
        
        // Notify EnemyManager if needed
        if (EnemyManager.Instance != null)
        {
            // Remove from enemy tracking
            // TODO: Add method to EnemyManager to handle enemy removal
        }
        
        if (showCombatDebug)
        {
            Debug.Log($"Enemy {gameObject.name}: Death animation complete, destroying");
        }
        
        // Destroy the enemy
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Coroutine to reset hit animation after brief delay.
    /// </summary>
    private System.Collections.IEnumerator ResetHitAnimation()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (enemyAnimator != null && !isDead)
        {
            enemyAnimator.SetBool("hit", false);
        }
    }
    
    /// <summary>
    /// Coroutine to automatically stop attack after a maximum duration.
    /// This prevents enemies from being stuck in attack state.
    /// </summary>
    private System.Collections.IEnumerator AutoStopAttack()
    {
        // Wait for attack animation duration
        yield return new WaitForSeconds(0.8f);
        
        // Deal damage if still in range (fallback for missing animation events)
        if (isAttacking && !isDead)
        {
            DealDamageToPlayer();
        }
        
        // Wait a bit more for animation to complete
        yield return new WaitForSeconds(0.7f);
        
        // Force stop attack if still attacking
        if (isAttacking && !isDead)
        {
            StopAttack();
            
            if (showCombatDebug)
            {
                Debug.Log($"Enemy {gameObject.name}: Auto-stopped attack after timeout");
            }
        }
    }
    
    private void TestMovement()
    {
        if (rigidBody != null)
        {
            rigidBody.velocity = new Vector3(2f, rigidBody.velocity.y, 0);
            // Debug.Log($"Enemy {gameObject.name}: Test movement - velocity set to: {rigidBody.velocity}");
        }
        else
        {
            Debug.LogError($"Enemy {gameObject.name}: No rigidbody found for test movement");
        }
    }
    
    #endregion
    
    #region Initialization
    
    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning($"Enemy {gameObject.name}: No player found");
        }
    }
    
    #endregion
    
    #region Movement
    
    private void HandleMovement()
    {
        if (playerTransform == null || rigidBody == null) 
        {
            Debug.LogWarning($"Enemy {gameObject.name}: Missing components - Player: {playerTransform != null}, RigidBody: {rigidBody != null}");
            return;
        }
        
        Vector3 directionToPlayer = (playerTransform.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // Debug.Log($"Enemy {gameObject.name}: Distance to player: {distanceToPlayer}, Detection range: {detectionRange}, Stop distance: {stopDistance}");
        
        // Stop if too close or too far
        if (distanceToPlayer <= stopDistance || distanceToPlayer > detectionRange) 
        {
            rigidBody.velocity = new Vector3(0, rigidBody.velocity.y, 0);
            // Debug.Log($"Enemy {gameObject.name}: Stopping - too close ({distanceToPlayer <= stopDistance}) or too far ({distanceToPlayer > detectionRange})");
            return;
        }
        
        // Move directly toward player (world space)
        Vector3 moveDirection = directionToPlayer.normalized;
        float adjustedSpeed = moveSpeed * moveSpeedMultiplier;
        
        // Fallback: ensure we have some speed
        if (adjustedSpeed <= 0.1f)
        {
            adjustedSpeed = 3f; // Default fallback speed
            Debug.LogWarning($"Enemy {gameObject.name}: Using fallback speed {adjustedSpeed}");
        }
        
        // Debug.Log($"Enemy {gameObject.name}: Move speed: {moveSpeed}, Adjusted speed: {adjustedSpeed}, Direction: {moveDirection}");
        
        // Apply horizontal movement only - let gravity handle Y
        Vector3 targetVelocity = new Vector3(
            moveDirection.x * adjustedSpeed,
            rigidBody.velocity.y, // Preserve gravity
            moveDirection.z * adjustedSpeed
        );
        
        rigidBody.velocity = targetVelocity;
        // Debug.Log($"Enemy {gameObject.name}: Set velocity to: {targetVelocity}");
        
        // Face the movement direction (only rotate visual child)
        if (moveDirection != Vector3.zero && enemyVisualTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            enemyVisualTransform.rotation = Quaternion.Slerp(enemyVisualTransform.rotation, targetRotation, Time.deltaTime * 8f);
        }
    }
    
    #endregion
    
    #region Cube Integration
    
    /// <summary>
    /// Parent this enemy to the cube root so it rotates automatically with the cube.
    /// This is much simpler than manual rotation calculations.
    /// </summary>
    public void ParentToCube()
    {
        if (WorldCube.Instance != null && WorldCube.Instance.CubeRoot != null)
        {
            transform.SetParent(WorldCube.Instance.CubeRoot.transform, true);
            // Debug.Log($"Enemy {name}: Parented to cube root for automatic rotation");
        }
        else
        {
            Debug.LogWarning($"Enemy {name}: Cannot parent to cube - WorldCube or CubeRoot not found");
        }
    }
    
    #endregion
}