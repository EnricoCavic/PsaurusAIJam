using UnityEngine;
using System;

/// <summary>
/// Health system component for both player and enemies.
/// Handles damage application, health tracking, and death events.
/// </summary>
public class HealthController : MonoBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    
    [Header("Behavior")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDelay = 0f; // Delay before destroy (for death animation)
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showHealthBar = false; // For development
    
    // Events for other systems to subscribe to
    public static event Action<HealthController, float> OnHealthChanged; // (health controller, new health)
    public static event Action<HealthController> OnDeath; // (health controller)
    
    // Instance events for specific entities
    public event Action<float> OnHealthChangedLocal; // (new health)
    public event Action OnDeathLocal;
    
    // State tracking
    private bool isDead = false;
    
    // Properties
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;
    public bool IsAlive => !isDead;
    
    void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        if (showDebugInfo)
        {
            Debug.Log($"HealthController: {gameObject.name} initialized with {currentHealth}/{maxHealth} health");
        }
    }
    
    /// <summary>
    /// Initialize health with specific max health value.
    /// Useful for enemies with different health values.
    /// </summary>
    public void Initialize(float health)
    {
        maxHealth = health;
        currentHealth = health;
        isDead = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"HealthController: {gameObject.name} initialized with {currentHealth}/{maxHealth} health");
        }
    }
    
    /// <summary>
    /// Apply damage to this entity.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        float actualDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"HealthController: {gameObject.name} took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(this, currentHealth);
        OnHealthChangedLocal?.Invoke(currentHealth);
        
        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heal this entity.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        float actualHealing = currentHealth - oldHealth;
        
        if (actualHealing > 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"HealthController: {gameObject.name} healed {actualHealing}. Health: {currentHealth}/{maxHealth}");
            }
            
            // Trigger events
            OnHealthChanged?.Invoke(this, currentHealth);
            OnHealthChangedLocal?.Invoke(currentHealth);
        }
    }
    
    /// <summary>
    /// Set health to maximum.
    /// </summary>
    public void FullHeal()
    {
        Heal(maxHealth);
    }
    
    /// <summary>
    /// Instantly kill this entity.
    /// </summary>
    public void Kill()
    {
        TakeDamage(currentHealth);
    }
    
    /// <summary>
    /// Handle death logic.
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"HealthController: {gameObject.name} died");
        }
        
        // Trigger events
        OnDeath?.Invoke(this);
        OnDeathLocal?.Invoke();
        
        // Handle destruction
        if (destroyOnDeath)
        {
            if (deathDelay > 0)
            {
                Invoke(nameof(DestroyEntity), deathDelay);
            }
            else
            {
                DestroyEntity();
            }
        }
    }
    
    /// <summary>
    /// Destroy the game object (called after death delay if applicable).
    /// </summary>
    private void DestroyEntity()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Reset health to maximum and clear death state.
    /// Useful for respawning or health upgrades.
    /// </summary>
    public void ResetHealth(float? newMaxHealth = null)
    {
        if (newMaxHealth.HasValue)
        {
            maxHealth = newMaxHealth.Value;
        }
        
        currentHealth = maxHealth;
        isDead = false;
        
        // Cancel any pending destruction
        CancelInvoke(nameof(DestroyEntity));
        
        if (showDebugInfo)
        {
            Debug.Log($"HealthController: {gameObject.name} reset to {currentHealth}/{maxHealth} health");
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(this, currentHealth);
        OnHealthChangedLocal?.Invoke(currentHealth);
    }    /// <summary>
    /// Get health as a formatted string for UI.
    /// </summary>
    public string GetHealthString()
    {
        return $"{currentHealth:F0}/{maxHealth:F0}";
    }
    
    #region Debug Visualization
    
    void OnGUI()
    {
        if (!showHealthBar) return;
        
        Vector3 worldPos = transform.position + Vector3.up * 2f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        
        if (screenPos.z > 0)
        {
            // Convert to GUI coordinates
            screenPos.y = Screen.height - screenPos.y;
            
            // Draw health bar background
            GUI.color = Color.black;
            GUI.Box(new Rect(screenPos.x - 25, screenPos.y - 10, 50, 10), "");
            
            // Draw health bar fill
            GUI.color = Color.Lerp(Color.red, Color.green, HealthPercentage);
            GUI.Box(new Rect(screenPos.x - 24, screenPos.y - 9, 48 * HealthPercentage, 8), "");
            
            // Draw health text
            GUI.color = Color.white;
            GUI.Label(new Rect(screenPos.x - 25, screenPos.y + 5, 50, 20), GetHealthString());
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw health percentage as a sphere color
        Gizmos.color = Color.Lerp(Color.red, Color.green, HealthPercentage);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
    }
    
    #endregion
}