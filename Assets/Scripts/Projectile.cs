using UnityEngine;

/// <summary>
/// Simple projectile that moves in a straight line and damages enemies on contact.
/// Auto-destroys on hit, max range, or lifetime expiry.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Projectile parameters (set by PlayerCombatController)
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    
    // Runtime state
    private float spawnTime;
    private Vector3 startPosition;
    private Rigidbody rb;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configure rigidbody for projectile movement
        rb.useGravity = false; // Projectiles ignore gravity
        rb.freezeRotation = true; // Don't tumble
        
        // Ensure collider is trigger for damage detection
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    
    void Start()
    {
        spawnTime = Time.time;
        startPosition = transform.position;
    }
    
    void FixedUpdate()
    {
        // Move projectile forward
        if (rb != null && speed > 0)
        {
            rb.velocity = direction * speed;
        }
        
        // Check lifetime expiry
        if (Time.time - spawnTime >= lifetime)
        {
            DestroyProjectile("Lifetime expired");
            return;
        }
        
        // Check max range (alternative to lifetime)
        float travelDistance = Vector3.Distance(startPosition, transform.position);
        float maxRange = speed * lifetime; // Calculate max range from speed and lifetime
        
        if (travelDistance >= maxRange)
        {
            DestroyProjectile("Max range reached");
        }
    }
    
    /// <summary>
    /// Initialize the projectile with movement and damage parameters.
    /// Called by PlayerCombatController when spawning.
    /// </summary>
    public void Initialize(Vector3 moveDirection, float projectileSpeed, float projectileDamage, float projectileLifetime)
    {
        direction = moveDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = projectileLifetime;
        
        // Rotate to face movement direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Projectile: Initialized with speed={speed}, damage={damage}, lifetime={lifetime}");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            ApplyDamage(other);
            DestroyProjectile($"Hit enemy: {other.name}");
        }
        // Check if we hit world geometry (optional - for obstacle collision)
        else if (other.CompareTag("WorldGeometry"))
        {
            DestroyProjectile($"Hit obstacle: {other.name}");
        }
    }
    
    /// <summary>
    /// Apply damage to the target enemy.
    /// </summary>
    private void ApplyDamage(Collider enemy)
    {
        // Try to find HealthController on the enemy
        HealthController enemyHealth = enemy.GetComponent<HealthController>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            
            if (showDebugInfo)
            {
                Debug.Log($"Projectile: Applied {damage} damage to {enemy.name}");
            }
        }
        else
        {
            // Fallback: Look for health on parent or any other component
            enemyHealth = enemy.GetComponentInParent<HealthController>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"Projectile: {enemy.name} has no HealthController component");
            }
        }
    }
    
    /// <summary>
    /// Destroy the projectile with optional debug message.
    /// </summary>
    private void DestroyProjectile(string reason = "")
    {
        if (showDebugInfo && !string.IsNullOrEmpty(reason))
        {
            Debug.Log($"Projectile: Destroyed - {reason}");
        }
        
        // TODO: Consider object pooling instead of destroy for performance
        Destroy(gameObject);
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw velocity vector
        if (rb != null && rb.velocity != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);
        }
        
        // Draw damage radius (if collider is sphere)
        SphereCollider sphereCol = GetComponent<SphereCollider>();
        if (sphereCol != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, sphereCol.radius);
        }
    }
}