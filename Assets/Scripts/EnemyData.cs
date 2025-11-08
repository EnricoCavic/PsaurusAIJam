using UnityEngine;

/// <summary>
/// ScriptableObject containing enemy-specific stats and configuration.
/// Allows different enemy types to have different health, damage, and behavior values.
/// </summary>
[CreateAssetMenu(fileName = "Enemy Data", menuName = "Cube Survivor/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Visual Identity")]
    [SerializeField] private string enemyName = "Basic Enemy";
    [SerializeField] private string description = "Standard enemy type";
    
    [Header("Combat Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float stopDistance = 0.5f;
    
    [Header("Behavior")]
    [SerializeField] private bool isAggressive = true;
    [SerializeField] private float spawnWeight = 1f; // For random spawning
    
    // Public properties for read-only access
    public string EnemyName => enemyName;
    public string Description => description;
    public float MaxHealth => maxHealth;
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public float MoveSpeed => moveSpeed;
    public float DetectionRange => detectionRange;
    public float StopDistance => stopDistance;
    public bool IsAggressive => isAggressive;
    public float SpawnWeight => spawnWeight;
    
    /// <summary>
    /// Validate attribute values and clamp to reasonable ranges.
    /// </summary>
    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        detectionRange = Mathf.Max(1f, detectionRange);
        stopDistance = Mathf.Max(0.1f, stopDistance);
        spawnWeight = Mathf.Max(0.1f, spawnWeight);
    }
}