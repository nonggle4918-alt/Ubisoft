using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDatabase
{
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    public DatabaseTable<AssetRecord> Assets { get; private set; }
    public DatabaseTable<CharacterRecord> Characters { get; private set; }
    public DatabaseTable<EnemyRecord> Enemies { get; private set; }
    public DatabaseTable<SpawnRecord> Spawns { get; private set; }
    public DatabaseTable<StageRecord> Stages { get; private set; }
    public DatabaseTable<TierDrawRecord> TierDraw { get; private set; }
    public DatabaseTable<TierStatRecord> TierStats { get; private set; }

    private float totalTierWeight;
    private bool tierWeightCalculated;

    public static GameDatabase Load()
    {
        return new GameDatabase
        {
            Assets = LoadTable<AssetRecord>("asset"),
            Characters = LoadTable<CharacterRecord>("character"),
            Enemies = LoadTable<EnemyRecord>("enemy"),
            Spawns = LoadTable<SpawnRecord>("spawn"),
            Stages = LoadTable<StageRecord>("stage"),
            TierDraw = LoadTable<TierDrawRecord>("tierDraw"),
            TierStats = LoadTable<TierStatRecord>("tierStat")
        };
    }

    public CharacterRecord GetCharacter(int id) => Characters.rows.FirstOrDefault(row => row.id == id);
    public EnemyRecord GetEnemy(int id) => Enemies.rows.FirstOrDefault(row => row.id == id);
    public StageRecord GetStage(int stageNumber) => Stages.rows.FirstOrDefault(row => row.stageNumber == stageNumber);
    public SpawnRecord[] GetSpawns(string groupId) => Spawns.rows.Where(row => row.groupId == groupId).ToArray();
    public AssetRecord GetAsset(string resourceId) => Assets.rows.FirstOrDefault(row => row.resourceId == resourceId);

    public int RollTier()
    {
        if (!tierWeightCalculated)
        {
            totalTierWeight = 0;
            foreach (var row in TierDraw.rows)
                totalTierWeight += row.weight;
            tierWeightCalculated = true;
        }

        float roll = UnityEngine.Random.Range(0f, totalTierWeight);
        float cumulative = 0;
        foreach (var row in TierDraw.rows)
        {
            cumulative += row.weight;
            if (roll < cumulative)
                return row.tier;
        }
        return TierDraw.rows.Count > 0 ? TierDraw.rows[0].tier : 1;
    }

    public TierStatRecord GetTierStat(int pieceId, int tier)
    {
        return TierStats.rows.FirstOrDefault(row => row.id == pieceId && row.tier == tier);
    }

    public Sprite GetSprite(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId)) return null;
        if (spriteCache.TryGetValue(resourceId, out Sprite cachedSprite)) return cachedSprite;

        AssetRecord asset = GetAsset(resourceId);
        Sprite sprite = null;
        if (asset != null && !string.IsNullOrEmpty(asset.path))
        {
            string path = asset.path.Replace('\\', '/');
            int resourcesIndex = path.IndexOf("Resources/", System.StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0)
                sprite = Resources.Load<Sprite>(path.Substring(resourcesIndex + "Resources/".Length));
        }

        if (sprite == null)
            sprite = Resources.LoadAll<Sprite>("Sprites").FirstOrDefault(item => item.name == resourceId);

        spriteCache[resourceId] = sprite;
        return sprite;
    }

    private static DatabaseTable<T> LoadTable<T>(string resourceName)
    {
        TextAsset json = Resources.Load<TextAsset>($"Database/{resourceName}");
        if (json == null)
        {
            Debug.LogError($"Database JSON was not found: Resources/Database/{resourceName}.json");
            return new DatabaseTable<T>();
        }

        return JsonUtility.FromJson<DatabaseTable<T>>(json.text) ?? new DatabaseTable<T>();
    }
}
