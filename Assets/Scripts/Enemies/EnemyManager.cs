using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private PieceData enemyPawnData;
    [SerializeField] private PieceData enemyQueenData;
    [SerializeField] private float spawnInterval = 1.5f;

    private List<Vector3> waypoints;
    private int enemiesAlive;

    private void Awake()
    {
        GenerateWaypoints();
    }

    private void OnEnable()
    {
        Enemy.OnAnyEnemyRemoved += OnEnemyRemoved;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyRemoved -= OnEnemyRemoved;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.WaveInProgress)
            StartCoroutine(SpawnWave());
    }

    private void GenerateWaypoints()
    {
        waypoints = new List<Vector3>();

        for (int x = -1; x <= 8; x++)
            waypoints.Add(new Vector3(x, -1, 0));

        for (int y = 0; y <= 8; y++)
            waypoints.Add(new Vector3(8, y, 0));

        for (int x = 7; x >= -1; x--)
            waypoints.Add(new Vector3(x, 8, 0));

        for (int y = 7; y >= 0; y--)
            waypoints.Add(new Vector3(-1, y, 0));
    }

    private IEnumerator SpawnWave()
    {
        int wave = GameManager.Instance.CurrentWave;
        var units = GenerateWaveUnits(wave);
        enemiesAlive = units.Count;

        foreach (var unitData in units)
        {
            SpawnEnemy(unitData);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private List<PieceData> GenerateWaveUnits(int wave)
    {
        var units = new List<PieceData>();

        int pawnCount = 10 + wave;
        int queenCount = Mathf.Max(0, wave - 2);

        for (int i = 0; i < pawnCount; i++)
            units.Add(enemyPawnData);
        for (int i = 0; i < queenCount; i++)
            units.Add(enemyQueenData);

        return units;
    }

    private void SpawnEnemy(PieceData unitData)
    {
        GameObject obj = Instantiate(enemyPrefab, waypoints[0], Quaternion.identity);
        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetData(unitData);
            enemy.SetWaypoints(waypoints);
        }
    }

    private void OnEnemyRemoved()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0)
            GameManager.Instance.EndWave();
    }
}
