using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ScoreEntry
{
    public string playerName;
    public int score;
    public string dateIso;
}

[Serializable]
internal class ScoreboardData
{
    public List<ScoreEntry> entries = new();
}

public static class ScoreboardManager
{
    // PlayerPrefs key for serialized top 10 data
    private const string TOP10_KEY = "SCOREBOARD_TOP10_JSON";
    public const int MaxEntries = 10;

    // Load current top 10 entries
    public static List<ScoreEntry> LoadTop10()
    {
        string json = PlayerPrefs.GetString(TOP10_KEY, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new List<ScoreEntry>();

        try
        {
            var data = JsonUtility.FromJson<ScoreboardData>(json);

            return data?.entries?
                .OrderByDescending(e => e.score)
                .Take(MaxEntries)
                .ToList()
                ?? new List<ScoreEntry>();
        }
        catch
        {
            // Return empty list when data is invalid
            return new List<ScoreEntry>();
        }
    }

    // Return true if score can enter the top 10 list
    public static bool WouldQualify(int score)
    {
        var list = LoadTop10();
        if (list.Count < MaxEntries) return true;
        return score > list[list.Count - 1].score;
    }

    // Add a score and keep only the best entries
    public static void AddScore(string playerName, int score)
    {
        var list = LoadTop10();

        list.Add(new ScoreEntry
        {
            playerName = SanitizeName(playerName),
            score = score,
            dateIso = DateTime.UtcNow.ToString("o"),
        });

        list = list
            .OrderByDescending(e => e.score)
            .ThenBy(e => e.playerName)
            .Take(MaxEntries)
            .ToList();

        string json = JsonUtility.ToJson(new ScoreboardData { entries = list });

        PlayerPrefs.SetString(TOP10_KEY, json);
        PlayerPrefs.Save();
    }

    // Return best score or 0 when empty
    public static int GetBestScore()
    {
        var list = LoadTop10();
        return list.Count == 0 ? 0 : list[0].score;
    }

    // Sanitize player name for storage
    private static string SanitizeName(string raw)
    {
        string name = (raw ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name)) name = "AAA";
        if (name.Length > 12) name = name.Substring(0, 12);
        return name;
    }
}
