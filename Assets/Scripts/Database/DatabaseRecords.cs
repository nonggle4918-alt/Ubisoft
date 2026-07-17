using System;
using System.Collections.Generic;

[Serializable]
public class DatabaseTable<T>
{
    public List<T> rows = new List<T>();
}

[Serializable]
public class AssetRecord
{
    public string resourceId;
    public string type;
    public string path;
}

[Serializable]
public class CharacterRecord
{
    public int id;
    public string name;
    public string type;
    public int attackDamage;
    public float attackRange;
    public float attackCooldown;
    public int cost;
    public string imageResourceId;
    public string effectId;
    public string soundId;
}

[Serializable]
public class EnemyRecord
{
    public int id;
    public string name;
    public string type;
    public int hp;
    public float speed;
    public int dropGold;
    public string imageResourceId;
    public string effectId;
    public string soundId;
}

[Serializable]
public class SpawnRecord
{
    public int spawnId;
    public string groupId;
    public int enemyId;
    public int enemyCount;
}

[Serializable]
public class StageRecord
{
    public int id;
    public int nextId;
    public int stageNumber;
    public string spawnGroupId;
}

[Serializable]
public class TierDrawRecord
{
    public int tier;
    public float weight;
}

[Serializable]
public class TierStatRecord
{
    public int id;
    public int tier;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public int cost;
    public int sell;
}

[Serializable]
public class PieceUpgradeRecord
{
    public int level;
    public int cost;
    public float bishopAtk;
    public float bishopCool;
    public float knightAtk;
    public float knightCool;
    public float rookAtk;
    public float rookCool;
}
