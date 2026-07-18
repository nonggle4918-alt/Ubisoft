using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private PieceData enemyPawnData;
    [SerializeField] private PieceData enemyQueenData;
    [SerializeField] private float spawnInterval = 1f;

    private List<Vector3> waypoints;
    private int enemiesAlive;
    private readonly Dictionary<int, PieceData> databaseEnemyData = new Dictionary<int, PieceData>();

    public int RemainingEnemies => Mathf.Max(0, enemiesAlive);

    // Boss stages must not advance on the stage timer alone — see TimerManager.
    public bool StageHasBoss { get; private set; }

    public bool IsBossAlive()
    {
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy != null && !enemy.IsDead && enemy.IsBoss)
                return true;
        }
        return false;
    }

    private void Start()
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

        GridManager grid = FindFirstObjectByType<GridManager>();
        if (grid == null)
        {
            GenerateLegacyWaypoints();
            return;
        }

        var cell00 = grid.GetCell(0, 0);
        if (cell00 == null) { GenerateLegacyWaypoints(); return; }
        var cellLast = grid.GetCell(grid.Width - 1, grid.Height - 1);
        if (cellLast == null) { GenerateLegacyWaypoints(); return; }
        Vector3 tile00 = cell00.transform.position;
        Vector3 tileLast = cellLast.transform.position;
        float left = tile00.x - 1f;
        float bottom = tile00.y - 1f;
        float right = tileLast.x + 1f;
        float top = tileLast.y + 1f;

        for (float x = left; x <= right; x++)
            waypoints.Add(new Vector3(x, bottom, 0));

        for (float y = bottom + 1f; y <= top; y++)
            waypoints.Add(new Vector3(right, y, 0));

        for (float x = right - 1f; x >= left; x--)
            waypoints.Add(new Vector3(x, top, 0));

        for (float y = top - 1f; y >= bottom + 1f; y--)
            waypoints.Add(new Vector3(left, y, 0));
    }

    private void GenerateLegacyWaypoints()
    {
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
        // The boss is listed first in its stage's spawn group, so it enters ahead of the escorts.
        StageHasBoss = units.Exists(unit => unit != null && unit.isBoss);
        float interval = GetSpawnInterval(units.Count);

        foreach (var unitData in units)
        {
            SpawnEnemy(unitData);
            yield return new WaitForSeconds(interval);
        }
    }

    private float GetSpawnInterval(int unitCount)
    {
        if (unitCount <= 1) return 0f;

        TimerManager timerManager = FindFirstObjectByType<TimerManager>();
        if (timerManager == null || timerManager.StageDuration <= 0f)
            return spawnInterval;

        return timerManager.StageDuration / unitCount;
    }

    private List<PieceData> GenerateWaveUnits(int wave)
    {
        GameDatabase database = GameManager.Instance?.Database;
        StageRecord stage = database?.GetStage(wave);
        if (stage != null)
        {
            var databaseUnits = new List<PieceData>();
            foreach (SpawnRecord spawn in database.GetSpawns(stage.spawnGroupId))
            {
                PieceData enemyData = GetDatabaseEnemyData(spawn.enemyId);
                if (enemyData == null)
                {
                    Debug.LogWarning($"Enemy ID {spawn.enemyId} is missing from enemy.json and was skipped.");
                    continue;
                }

                for (int i = 0; i < spawn.enemyCount; i++)
                    databaseUnits.Add(enemyData);
            }

            return databaseUnits;
        }

        return GenerateLegacyWaveUnits(wave);
    }

    private List<PieceData> GenerateLegacyWaveUnits(int wave)
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

    private PieceData GetDatabaseEnemyData(int enemyId)
    {
        if (databaseEnemyData.TryGetValue(enemyId, out PieceData cachedData))
            return cachedData;

        GameDatabase database = GameManager.Instance?.Database;
        EnemyRecord record = database?.GetEnemy(enemyId);
        if (record == null) return null;

        var data = ScriptableObject.CreateInstance<PieceData>();
        data.hideFlags = HideFlags.DontSave;
        data.pieceName = record.name;
        data.team = Team.Enemy;
        data.maxHP = record.hp;
        data.movementSpeed = record.speed;
        data.goldReward = record.dropGold;
        data.isBoss = record.IsBoss;
        data.sprite = database.GetSprite(record.imageResourceId);

        databaseEnemyData[enemyId] = data;
        return data;
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
        SFXManager.Instance?.PlayEnemySpawned();
    }

    private void OnEnemyRemoved()
    {
        enemiesAlive--;
    }
}
