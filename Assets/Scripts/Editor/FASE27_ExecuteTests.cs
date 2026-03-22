using UnityEditor;
using UnityEditor.SceneHierarchy;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections.Generic;

/// <summary>
/// FASE 27: Test Execution Menu
/// Runs complete validation on all 1,498 card scripts
/// </summary>
public class FASE27_ExecuteTests
{
    [MenuItem("Tools/FASE 27/Execute Complete Validation")]
    public static void ExecuteCompleteValidation()
    {
        UnityEngine.Debug.Log("[FASE 27] Initializing Lua engine...");
        
        try
        {
            // Initialize Lua
            Script luaEngine = new Script(CoreModules.Preset_SoftSandbox);
            
            // Register Lua types
            UserData.RegisterType<LuaDuel>();
            UserData.RegisterType<LuaCard>();
            UserData.RegisterType<LuaEffect>();
            UserData.RegisterType<LuaGroup>();
            
            // Register globals
            luaEngine.Globals["Duel"] = new LuaDuel();
            luaEngine.Globals["Effect"] = typeof(LuaEffect);
            luaEngine.Globals["Card"] = typeof(LuaCard);
            luaEngine.Globals["Group"] = typeof(LuaGroup);

            UnityEngine.Debug.Log("[FASE 27] Creating tester instance...");
            
            // Create tester
            var tester = new LuaCardScriptTester(luaEngine);

            UnityEngine.Debug.Log("[FASE 27] Starting validation...");
            
            // Run complete validation
            var summary = tester.RunCompleteValidation();

            // Save results
            SaveResults(summary);
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[FASE 27] Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [MenuItem("Tools/FASE 27/Show Last Results")]
    public static void ShowLastResults()
    {
        string resultsPath = "Assets/FASE27_RESULTS.txt";
        if (System.IO.File.Exists(resultsPath))
        {
            string content = System.IO.File.ReadAllText(resultsPath);
            UnityEngine.Debug.Log(content);
        }
        else
        {
            UnityEngine.Debug.LogWarning("No results file found. Run 'Execute Complete Validation' first.");
        }
    }

    private static void SaveResults(LuaCardScriptTester.TestSummary summary)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine("FASE 27 - COMPLETE VALIDATION RESULTS");
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Total Cards: {summary.TotalCards}");
        sb.AppendLine($"Passed: {summary.PassedCards} ({(summary.PassedCards * 100.0 / summary.TotalCards):F1}%)");
        sb.AppendLine($"Failed: {summary.FailedCards} ({(summary.FailedCards * 100.0 / summary.TotalCards):F1}%)");
        sb.AppendLine($"Time: {summary.TotalTimeMs}ms");
        sb.AppendLine();

        if (summary.FailedResults.Count > 0)
        {
            sb.AppendLine("═══════════════════════════════════════════════");
            sb.AppendLine($"FAILED CARDS ({summary.FailedResults.Count})");
            sb.AppendLine("═══════════════════════════════════════════════");
            
            var errorGroups = new System.Collections.Generic.Dictionary<string, List<LuaCardScriptTester.TestResult>>();
            
            foreach (var result in summary.FailedResults)
            {
                if (!errorGroups.ContainsKey(result.ErrorType))
                    errorGroups[result.ErrorType] = new List<LuaCardScriptTester.TestResult>();
                errorGroups[result.ErrorType].Add(result);
            }

            foreach (var group in errorGroups)
            {
                sb.AppendLine($"\n[{group.Key}] ({group.Value.Count}):");
                foreach (var result in group.Value)
                {
                    sb.AppendLine($"  cDM{result.CardId:D4}: {result.ErrorMessage}");
                }
            }
        }

        string resultsPath = "Assets/FASE27_RESULTS.txt";
        System.IO.File.WriteAllText(resultsPath, sb.ToString());
        UnityEngine.Debug.Log($"[FASE 27] Results saved to: {resultsPath}");
    }
}
