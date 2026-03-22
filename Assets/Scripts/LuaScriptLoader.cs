using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// FASE 26: LuaScriptLoader
/// Handles loading and managing Lua card scripts
/// </summary>
public class LuaScriptLoader
{
    private const string SCRIPTS_DIRECTORY = "Assets/Scripts/LuaScripts";
    private const string SCRIPT_EXTENSION = ".lua";
    private Dictionary<int, string> scriptCache = new Dictionary<int, string>();
    private List<(int cardId, long size)> scriptSizeCache;

    public LuaScriptLoader()
    {
        InitializeSizeCache();
    }

    /// <summary>
    /// Load a single script from file by card ID
    /// </summary>
    public string LoadScriptFromFile(int cardId)
    {
        // Check cache first
        if (scriptCache.ContainsKey(cardId))
        {
            return scriptCache[cardId];
        }

        try
        {
            string filePath = GetScriptPath(cardId);
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Script file not found: {filePath}");
                return null;
            }

            string scriptContent = File.ReadAllText(filePath);
            scriptCache[cardId] = scriptContent;
            return scriptContent;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading script for card {cardId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load all scripts from directory
    /// </summary>
    public Dictionary<int, string> LoadAllScripts()
    {
        var allScripts = new Dictionary<int, string>();
        var dir = new DirectoryInfo(SCRIPTS_DIRECTORY);

        if (!dir.Exists)
        {
            Debug.LogError($"Scripts directory not found: {SCRIPTS_DIRECTORY}");
            return allScripts;
        }

        var files = dir.GetFiles($"*{SCRIPT_EXTENSION}");
        
        foreach (var file in files)
        {
            if (TryExtractCardId(file.Name, out int cardId))
            {
                allScripts[cardId] = File.ReadAllText(file.FullName);
            }
        }

        return allScripts;
    }

    /// <summary>
    /// Get the file path for a card script
    /// </summary>
    public string GetScriptPath(int cardId)
    {
        // Try multiple naming conventions
        string[] patterns = new[]
        {
            $"{SCRIPTS_DIRECTORY}/cDM{cardId}{SCRIPT_EXTENSION}",
            $"{SCRIPTS_DIRECTORY}/c{cardId}{SCRIPT_EXTENSION}",
            $"{SCRIPTS_DIRECTORY}/card_{cardId}{SCRIPT_EXTENSION}",
            $"{SCRIPTS_DIRECTORY}/{cardId}{SCRIPT_EXTENSION}"
        };

        foreach (var pattern in patterns)
        {
            if (File.Exists(pattern))
                return pattern;
        }

        // Return the most likely path (will fail if not found)
        return $"{SCRIPTS_DIRECTORY}/cDM{cardId}{SCRIPT_EXTENSION}";
    }

    /// <summary>
    /// Validate Lua syntax without executing
    /// </summary>
    public bool ValidateLuaSyntax(string luaCode)
    {
        if (string.IsNullOrEmpty(luaCode))
            return false;

        try
        {
            // Try to parse without executing
            var lexer = new Regex(@"(\bend\b|\bfunction\b|\blocal\b|\breturn\b)", RegexOptions.Multiline);
            var tokens = lexer.Matches(luaCode);
            
            // Basic syntax check - ensure balanced Lua structures
            int functionCount = Regex.Matches(luaCode, @"\bfunction\b").Count;
            int endCount = Regex.Matches(luaCode, @"\bend\b").Count;
            
            if (functionCount != endCount)
            {
                Debug.LogWarning("Unbalanced function/end in Lua code");
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get cards by file size range
    /// </summary>
    public List<int> GetCardsBySize(long minBytes, long maxBytes)
    {
        if (scriptSizeCache == null)
        {
            InitializeSizeCache();
        }

        return scriptSizeCache
            .Where(x => x.size >= minBytes && x.size <= maxBytes)
            .Select(x => x.cardId)
            .ToList();
    }

    /// <summary>
    /// Get a random sample of cards
    /// </summary>
    public List<int> GetRandomCardSample(int sampleSize = 50)
    {
        if (scriptSizeCache == null)
        {
            InitializeSizeCache();
        }

        var random = new System.Random();
        return scriptSizeCache
            .OrderBy(x => random.Next())
            .Take(sampleSize)
            .Select(x => x.cardId)
            .ToList();
    }

    /// <summary>
    /// Get all available card IDs
    /// </summary>
    public List<int> GetAllCardIds()
    {
        if (scriptSizeCache == null)
        {
            InitializeSizeCache();
        }

        return scriptSizeCache
            .Select(x => x.cardId)
            .ToList();
    }

    /// <summary>
    /// Get total count of available scripts
    /// </summary>
    public int GetScriptCount()
    {
        var dir = new DirectoryInfo(SCRIPTS_DIRECTORY);
        if (!dir.Exists) return 0;
        return dir.GetFiles($"*{SCRIPT_EXTENSION}").Length;
    }

    /// <summary>
    /// Clear the script cache to free memory
    /// </summary>
    public void ClearCache()
    {
        scriptCache.Clear();
        GC.Collect();
    }

    // ==================== PRIVATE METHODS ====================

    /// <summary>
    /// Initialize cache with script file sizes
    /// </summary>
    private void InitializeSizeCache()
    {
        scriptSizeCache = new List<(int, long)>();
        var dir = new DirectoryInfo(SCRIPTS_DIRECTORY);

        if (!dir.Exists)
        {
            Debug.LogError($"Scripts directory not found: {SCRIPTS_DIRECTORY}");
            return;
        }

        var files = dir.GetFiles($"*{SCRIPT_EXTENSION}");
        
        foreach (var file in files)
        {
            if (TryExtractCardId(file.Name, out int cardId))
            {
                scriptSizeCache.Add((cardId, file.Length));
            }
        }

        // Sort by card ID
        scriptSizeCache.Sort((a, b) => a.cardId.CompareTo(b.cardId));
    }

    /// <summary>
    /// Try to extract card ID from filename
    /// </summary>
    private bool TryExtractCardId(string filename, out int cardId)
    {
        cardId = 0;

        // Remove extension
        string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

        // Try cDM123 format
        var match = Regex.Match(nameWithoutExt, @"cDM(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
        {
            cardId = id;
            return true;
        }

        // Try c123 format
        match = Regex.Match(nameWithoutExt, @"c(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out id))
        {
            cardId = id;
            return true;
        }

        // Try just number format
        if (int.TryParse(nameWithoutExt, out id))
        {
            cardId = id;
            return true;
        }

        return false;
    }
}
