using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple enemy manager for the game prototype.
/// Handles spawning enemies on different cube faces and basic enemy management.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject[] enemyPrefabs; // Array of different enemy prefabs
    [SerializeField] private GameObject fallbackEnemyPrefab; // Single prefab fallback
    [SerializeField] private bool useRandomEnemyTypes = true; // Enable random enemy selection
    
    [Header("Spawn Settings")]
    [SerializeField] private int enemiesPerFace = 3;
    [SerializeField] private float spawnRadius = 5f; // Reduced from 8f
    [SerializeField] private float spawnVariation = 0.5f; // Small random offset
    [SerializeField] private bool spawnOnStart = true;
    
    [Header("Face Spawn Points")]
    [SerializeField] private Transform[] northFaceSpawnPoints;
    [SerializeField] private Transform[] southFaceSpawnPoints;
    [SerializeField] private Transform[] eastFaceSpawnPoints;
    [SerializeField] private Transform[] westFaceSpawnPoints;
    [SerializeField] private Transform[] topFaceSpawnPoints;
    [SerializeField] private Transform[] bottomFaceSpawnPoints;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Enemy tracking
    private List<EnemyMovementController> allEnemies = new List<EnemyMovementController>();
    private Dictionary<CubeFace, List<EnemyMovementController>> enemiesByFace = new Dictionary<CubeFace, List<EnemyMovementController>>();
    
    // Singleton pattern
    public static EnemyManager Instance { get; private set; }
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeFaceDictionary();
    }
    
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemiesOnAllFaces();
        }
    }
    
    void Update()
    {
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeFaceDictionary()
    {
        enemiesByFace[CubeFace.North] = new List<EnemyMovementController>();
        enemiesByFace[CubeFace.South] = new List<EnemyMovementController>();
        enemiesByFace[CubeFace.East] = new List<EnemyMovementController>();
        enemiesByFace[CubeFace.West] = new List<EnemyMovementController>();
        enemiesByFace[CubeFace.Top] = new List<EnemyMovementController>();
        enemiesByFace[CubeFace.Bottom] = new List<EnemyMovementController>();
    }
    
    #endregion
    
    #region Enemy Spawning
    
    public void SpawnEnemiesOnAllFaces()
    {
        SpawnEnemiesOnFace(CubeFace.North);
        SpawnEnemiesOnFace(CubeFace.South);
        SpawnEnemiesOnFace(CubeFace.East);
        SpawnEnemiesOnFace(CubeFace.West);
        SpawnEnemiesOnFace(CubeFace.Top);
        SpawnEnemiesOnFace(CubeFace.Bottom);
        
        Debug.Log($"EnemyManager: Spawned {allEnemies.Count} enemies across all faces");
    }
    
    public void SpawnEnemiesOnFace(CubeFace face)
    {
        GameObject prefabToSpawn = GetRandomEnemyPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError("EnemyManager: No enemy prefabs assigned!");
            return;
        }
        
        Transform[] spawnPoints = GetSpawnPointsForFace(face);
        
        for (int i = 0; i < enemiesPerFace; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition(face, spawnPoints);
            SpawnEnemyAtPosition(spawnPosition, face, prefabToSpawn);
        }
    }
    
    private Transform[] GetSpawnPointsForFace(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.North: return northFaceSpawnPoints;
            case CubeFace.South: return southFaceSpawnPoints;
            case CubeFace.East: return eastFaceSpawnPoints;
            case CubeFace.West: return westFaceSpawnPoints;
            case CubeFace.Top: return topFaceSpawnPoints;
            case CubeFace.Bottom: return bottomFaceSpawnPoints;
            default: return new Transform[0];
        }
    }
    
    private Vector3 GetRandomSpawnPosition(CubeFace face, Transform[] spawnPoints)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Use predefined spawn points with small variation
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 basePosition = randomSpawnPoint.position;
            
            // Add small random offset instead of full sphere
            Vector3 smallOffset = new Vector3(
                Random.Range(-spawnVariation, spawnVariation),
                0f, // Don't offset Y to keep on surface
                Random.Range(-spawnVariation, spawnVariation)
            );
            
            return basePosition + smallOffset;
        }
        else
        {
            // Fallback: calculate proper position on cube face
            return GetPositionOnCubeFace(face);
        }
    }
    
    private Vector3 GetPositionOnCubeFace(CubeFace face)
    {
        Vector3 cubeCenter = WorldCube.Instance != null ? WorldCube.Instance.transform.position : Vector3.zero;
        Vector3 facePosition;
        
        // Calculate position on the specified cube face
        switch (face)
        {
            case CubeFace.North:
                facePosition = cubeCenter + Vector3.forward * spawnRadius;
                break;
            case CubeFace.South:
                facePosition = cubeCenter + Vector3.back * spawnRadius;
                break;
            case CubeFace.East:
                facePosition = cubeCenter + Vector3.right * spawnRadius;
                break;
            case CubeFace.West:
                facePosition = cubeCenter + Vector3.left * spawnRadius;
                break;
            case CubeFace.Top:
                facePosition = cubeCenter + Vector3.up * spawnRadius;
                break;
            case CubeFace.Bottom:
                facePosition = cubeCenter + Vector3.down * spawnRadius;
                break;
            default:
                facePosition = cubeCenter + Vector3.forward * spawnRadius;
                break;
        }
        
        // Add small random variation on the face plane
        Vector3 randomOnFace = GetRandomPositionOnFacePlane(face) * spawnVariation;
        
        return facePosition + randomOnFace;
    }
    
    private Vector3 GetRandomPositionOnFacePlane(CubeFace face)
    {
        Vector3 randomOffset;
        
        switch (face)
        {
            case CubeFace.North:
            case CubeFace.South:
                // X-Y plane movement
                randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
                break;
            case CubeFace.East:
            case CubeFace.West:
                // Z-Y plane movement
                randomOffset = new Vector3(0f, Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                break;
            case CubeFace.Top:
            case CubeFace.Bottom:
                // X-Z plane movement
                randomOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                break;
            default:
                randomOffset = Vector3.zero;
                break;
        }
        
        return randomOffset;
    }
    
    /// <summary>
    /// Get a random enemy prefab from the available prefabs.
    /// </summary>
    private GameObject GetRandomEnemyPrefab()
    {
        // If using random enemy types and we have multiple prefabs
        if (useRandomEnemyTypes && enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            // Filter out null prefabs
            GameObject[] validPrefabs = System.Array.FindAll(enemyPrefabs, prefab => prefab != null);
            
            if (validPrefabs.Length > 0)
            {
                return validPrefabs[Random.Range(0, validPrefabs.Length)];
            }
        }
        
        // Fallback to single prefab or first prefab in array
        if (fallbackEnemyPrefab != null)
        {
            return fallbackEnemyPrefab;
        }
        
        if (enemyPrefabs != null && enemyPrefabs.Length > 0 && enemyPrefabs[0] != null)
        {
            return enemyPrefabs[0];
        }
        
        Debug.LogError("EnemyManager: No valid enemy prefabs found!");
        return null;
    }
    
    private void SpawnEnemyAtPosition(Vector3 position, CubeFace face, GameObject prefabToSpawn)
    {
        // Debug logging for spawn position
        Vector3 cubeCenter = WorldCube.Instance != null ? WorldCube.Instance.transform.position : Vector3.zero;
        float distanceFromCube = Vector3.Distance(position, cubeCenter);
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawning {prefabToSpawn.name} on {face} at {position} (distance from cube: {distanceFromCube:F2})");
        }
        
        GameObject enemyObject = Instantiate(prefabToSpawn, position, Quaternion.identity);
        EnemyMovementController enemyController = enemyObject.GetComponent<EnemyMovementController>();
        
        if (enemyController == null)
        {
            enemyController = enemyObject.AddComponent<EnemyMovementController>();
        }
        
        // Set the enemy's face
        // Set enemy to spawn on specific face
        enemyController.SetCurrentFace(face);
        
        // Track the enemy
        allEnemies.Add(enemyController);
        enemiesByFace[face].Add(enemyController);
        
        // Name for debugging
        enemyObject.name = $"Enemy_{face}_{enemiesByFace[face].Count}";
    }
    
    #endregion
    
    #region Enemy Management
    
    public void RemoveEnemy(EnemyMovementController enemy)
    {
        if (enemy == null) return;
        
        allEnemies.Remove(enemy);
        
        // Remove from face-specific list
        CubeFace enemyFace = enemy.GetCurrentFace();
        if (enemiesByFace.ContainsKey(enemyFace))
        {
            enemiesByFace[enemyFace].Remove(enemy);
        }
    }
    
    public List<EnemyMovementController> GetEnemiesOnFace(CubeFace face)
    {
        return enemiesByFace.ContainsKey(face) ? enemiesByFace[face] : new List<EnemyMovementController>();
    }
    
    public List<EnemyMovementController> GetAllEnemies()
    {
        // Return all enemies - simplified from GetVisibleEnemies
        return new List<EnemyMovementController>(allEnemies);
    }
    
    public int GetTotalEnemyCount()
    {
        return allEnemies.Count;
    }
    
    public int GetVisibleEnemyCount()
    {
        // Simplified - all enemies are considered visible now
        return allEnemies.Count;
    }
    
    #endregion
    
    #region Debug
    
    private void UpdateDebugInfo()
    {
        // Debug info is displayed in OnGUI
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 300, 300, 200));
        GUILayout.Label("Enemy Manager Debug:");
        GUILayout.Label($"Total Enemies: {GetTotalEnemyCount()}");
        GUILayout.Label($"Visible Enemies: {GetVisibleEnemyCount()}");
        
        if (WorldCube.Instance != null)
        {
            CubeFace currentFace = WorldCube.Instance.CurrentFace;
            int enemiesOnCurrentFace = GetEnemiesOnFace(currentFace).Count;
            GUILayout.Label($"Enemies on {currentFace}: {enemiesOnCurrentFace}");
        }
        
        GUILayout.Label("");
        
        if (GUILayout.Button("Spawn More Enemies"))
        {
            SpawnEnemiesOnAllFaces();
        }
        
        if (GUILayout.Button("Clear All Enemies"))
        {
            ClearAllEnemies();
        }
        
        GUILayout.EndArea();
    }
    
    private void ClearAllEnemies()
    {
        foreach (EnemyMovementController enemy in allEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        
        allEnemies.Clear();
        InitializeFaceDictionary();
    }
    
    #endregion
}