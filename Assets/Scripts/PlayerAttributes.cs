using UnityEngine;

/// <summary>
/// ScriptableObject containing player combat and movement attributes.
/// Provides designer-friendly interface for tuning player stats.
/// Can be used for progression/upgrade system.
/// </summary>
[CreateAssetMenu(fileName = "Player Attributes", menuName = "Cube Survivor/Player Attributes")]
public class PlayerAttributes : ScriptableObject
{
    [Header("Combat Stats")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackSpeed = 1f; // Attacks per second
    
    [Header("Player Stats")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 2f; // Max time before auto-destroy
    
    // Public properties for read-only access
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float AttackSpeed => attackSpeed;
    public float MoveSpeed => moveSpeed;
    public float MaxHealth => maxHealth;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    
    /// <summary>
    /// Calculate attack cooldown based on attack speed.
    /// </summary>
    public float AttackCooldown => attackSpeed > 0 ? 1f / attackSpeed : 1f;
    
    /// <summary>
    /// Validate attribute values and clamp to reasonable ranges.
    /// </summary>
    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackSpeed = Mathf.Max(0.1f, attackSpeed);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        maxHealth = Mathf.Max(1f, maxHealth);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        
        #if UNITY_EDITOR
        // Update debug info in inspector
        debugAttackCooldown = AttackCooldown;
        #endif
    }
    
    #if UNITY_EDITOR
    [Header("Debug Info (Read Only)")]
    [SerializeField, Tooltip("Calculated from Attack Speed")] 
    private float debugAttackCooldown = 0f;
    #endif
}