using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public static class ExcelDatabaseImporter
{
    private const string DatabaseFolder = "Assets/DB";
    private const string OutputFolder = "Assets/Resources/Database";
    [MenuItem("Tools/Database/Import Excel Database")]
    public static void ImportAll()
    {
        if (!Directory.Exists(DatabaseFolder))
        {
            Debug.LogError($"Database folder was not found: {DatabaseFolder}");
            return;
        }

        Directory.CreateDirectory(OutputFolder);

        ImportAssets();
        ImportCharacters();
        ImportEnemies();
        ImportSpawns();
        ImportStages();
        ImportPieceUpgrades();

        AssetDatabase.Refresh();
        Debug.Log("Excel database import completed. Gacha data was skipped.");
    }

    private static void ImportAssets()
    {
        var output = new DatabaseTable<AssetRecord>();
        foreach (ExcelRow row in ReadTable("asset.xlsx", "Res_id"))
        {
            output.rows.Add(new AssetRecord
            {
                resourceId = row.GetString("Res_id"),
                type = row.GetString("type"),
                path = row.GetString("path")
            });
        }
        WriteJson("asset", output);
    }

    private static void ImportCharacters()
    {
        var output = new DatabaseTable<CharacterRecord>();
        foreach (ExcelRow row in ReadTable("character.xlsx", "id"))
        {
            output.rows.Add(new CharacterRecord
            {
                id = row.GetInt("id"),
                name = row.GetString("name"),
                type = row.GetString("type"),
                attackDamage = row.GetInt("Attack_Damage"),
                attackRange = row.GetFloat("Attack_Range"),
                attackCooldown = row.GetFloat("Attack_Cooldown"),
                cost = row.GetInt("Cost"),
                imageResourceId = row.GetString("img_res"),
                effectId = row.GetString("efx"),
                soundId = row.GetString("sf")
            });
        }
        WriteJson("character", output);
    }

    private static void ImportEnemies()
    {
        var output = new DatabaseTable<EnemyRecord>();
        foreach (ExcelRow row in ReadTable("enemy.xlsx", "id"))
        {
            output.rows.Add(new EnemyRecord
            {
                id = row.GetInt("id"),
                name = row.GetString("name"),
                type = row.GetString("type"),
                hp = row.GetInt("Hp"),
                speed = row.GetFloat("speed"),
                dropGold = row.GetInt("drop_gold"),
                imageResourceId = row.GetString("img_res"),
                effectId = row.GetString("efx"),
                soundId = row.GetString("sf")
            });
        }
        WriteJson("enemy", output);
    }

    private static void ImportSpawns()
    {
        var output = new DatabaseTable<SpawnRecord>();
        foreach (ExcelRow row in ReadTable("spawn.xlsx", "spawn_id"))
        {
            output.rows.Add(new SpawnRecord
            {
                spawnId = row.GetInt("spawn_id"),
                groupId = row.GetString("group_id"),
                enemyId = row.GetInt("enmey"),
                enemyCount = row.GetInt("enemy_count")
            });
        }
        WriteJson("spawn", output);
    }

    private static void ImportStages()
    {
        var output = new DatabaseTable<StageRecord>();
        foreach (ExcelRow row in ReadTable("stage.xlsx", "id"))
        {
            output.rows.Add(new StageRecord
            {
                id = row.GetInt("id"),
                nextId = row.GetInt("next_id"),
                stageNumber = row.GetInt("stg_number"),
                spawnGroupId = row.GetString("spawn_id")
            });
        }
        WriteJson("stage", output);
    }

    private static void ImportPieceUpgrades()
    {
        var output = new DatabaseTable<PieceUpgradeRecord>();
        foreach (ExcelRow row in ReadTable("Piece_Upgrade.xlsx", "level"))
        {
            output.rows.Add(new PieceUpgradeRecord
            {
                level = row.GetInt("level"),
                cost = row.GetInt("cost"),
                bishopAtk = row.GetFloat("up_bishop_atk"),
                bishopCool = row.GetFloat("up_bishop_cool"),
                knightAtk = row.GetFloat("up_knight_atk"),
                knightCool = row.GetFloat("up_knight_cool"),
                rookAtk = row.GetFloat("up_rook_atk"),
                rookCool = row.GetFloat("up_rook_cool")
            });
        }
        WriteJson("pieceUpgrade", output);
    }

    private static void WriteJson<T>(string fileName, DatabaseTable<T> table)
    {
        string path = Path.Combine(OutputFolder, fileName + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(table, true));
    }

    private static List<ExcelRow> ReadTable(string fileName, string requiredHeader)
    {
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string csvPath = Path.Combine(DatabaseFolder, baseName + ".csv");
        if (File.Exists(csvPath) && new FileInfo(csvPath).Length > 5)
            return ReadCsvTable(csvPath, requiredHeader);

        string fullPath = Path.Combine(DatabaseFolder, fileName);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Database file was not found.", fullPath);

        using (var fileStream = File.OpenRead(fullPath))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            List<string> sharedStrings = ReadSharedStrings(archive);
            List<Dictionary<int, string>> rows = ReadDataSheet(archive, sharedStrings);
            return BuildRows(rows, requiredHeader);
        }
    }

    private static List<ExcelRow> ReadCsvTable(string path, string requiredHeader)
    {
        var rows = File.ReadAllLines(path)
            .Select(line => ParseCsvLine(line)
                .Select((value, index) => new { index, value })
                .ToDictionary(cell => cell.index, cell => cell.value))
            .ToList();
        return BuildRows(rows, requiredHeader);
    }

    private static List<ExcelRow> BuildRows(List<Dictionary<int, string>> rows, string requiredHeader)
    {
        int headerRowIndex = FindHeaderRow(rows, requiredHeader);
        if (headerRowIndex < 0)
        {
            Debug.LogError($"Could not find the '{requiredHeader}' header column in the database file.");
            return new List<ExcelRow>();
        }

        Dictionary<int, string> headerCells = rows[headerRowIndex];
        var headers = headerCells
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Trim());

        int dataStartIndex = headerRowIndex + 1;
        if (rows.Count > dataStartIndex && IsTypeDefinitionRow(rows[dataStartIndex]))
            dataStartIndex++;

        var result = new List<ExcelRow>();
        for (int rowIndex = dataStartIndex; rowIndex < rows.Count; rowIndex++)
        {
            Dictionary<int, string> cells = rows[rowIndex];
            if (cells.Values.All(string.IsNullOrWhiteSpace))
                continue;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                string value;
                cells.TryGetValue(header.Key, out value);
                values[header.Value] = value ?? string.Empty;
            }
            result.Add(new ExcelRow(values));
        }
        return result;
    }

    private static int FindHeaderRow(List<Dictionary<int, string>> rows, string requiredHeader)
    {
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            if (rows[rowIndex].Values.Any(value => string.Equals(value?.Trim(), requiredHeader, StringComparison.OrdinalIgnoreCase)))
                return rowIndex;
        }
        return -1;
    }

    private static bool IsTypeDefinitionRow(Dictionary<int, string> row)
    {
        string[] typeNames = { "int", "float", "string", "enum", "bool", "double" };
        string[] values = row.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return values.Length > 0 && values.All(value => typeNames.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase));
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString());
        return values;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return new List<string>();

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        using (Stream stream = entry.Open())
        {
            var document = XDocument.Load(stream);
            return document.Descendants(ns + "si")
                .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
                .ToList();
        }
    }

    private static List<Dictionary<int, string>> ReadDataSheet(ZipArchive archive, List<string> sharedStrings)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        List<Dictionary<int, string>> bestRows = null;
        foreach (ZipArchiveEntry entry in archive.Entries.Where(item => item.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)))
        {
            using (Stream stream = entry.Open())
            {
                var document = XDocument.Load(stream);
                var rows = document.Descendants(ns + "row")
                    .Select(row => ReadRow(row, ns, sharedStrings))
                    .ToList();

                if (rows.Any(row => row.Values.Any(value => !string.IsNullOrWhiteSpace(value))) &&
                    (bestRows == null || rows.Count > bestRows.Count))
                    bestRows = rows;
            }
        }
        if (bestRows != null)
            return bestRows;

        throw new InvalidDataException("Could not find a worksheet containing database data.");
    }

    private static Dictionary<int, string> ReadRow(XElement row, XNamespace ns, List<string> sharedStrings)
    {
        var values = new Dictionary<int, string>();
        foreach (XElement cell in row.Elements(ns + "c"))
        {
            string reference = (string)cell.Attribute("r");
            if (string.IsNullOrEmpty(reference)) continue;

            int column = GetColumnIndex(reference);
            string cellType = (string)cell.Attribute("t");
            string value = cell.Element(ns + "v")?.Value ?? string.Empty;

            if (cellType == "s" && int.TryParse(value, out int sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                value = sharedStrings[sharedIndex];
            else if (cellType == "inlineStr")
                value = string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));

            values[column] = value;
        }
        return values;
    }

    private static int GetColumnIndex(string cellReference)
    {
        int column = 0;
        foreach (char character in cellReference)
        {
            if (!char.IsLetter(character)) break;
            column = column * 26 + (char.ToUpperInvariant(character) - 'A' + 1);
        }
        return column - 1;
    }

    private sealed class ExcelRow
    {
        private readonly Dictionary<string, string> values;

        public ExcelRow(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public string GetString(string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }

        public int GetInt(string key)
        {
            int.TryParse(GetString(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
            return value;
        }

        public float GetFloat(string key)
        {
            float.TryParse(GetString(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            return value;
        }
    }
}
