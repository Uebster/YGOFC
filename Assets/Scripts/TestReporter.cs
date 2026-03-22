using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// FASE 26: TestReporter
/// Generates test reports and exports results
/// </summary>
public class TestReporter
{
    private List<LuaCardScriptTester.TestResult> testResults = new List<LuaCardScriptTester.TestResult>();
    private DateTime sessionStartTime;
    private DateTime sessionEndTime;

    public TestReporter()
    {
        sessionStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Log a test result
    /// </summary>
    public void LogTestResult(int cardId, LuaCardScriptTester.TestResult result)
    {
        testResults.Add(result);
        
        string status = result.Executed ? "✅ PASS" : "❌ FAIL";
        Debug.Log($"Card {cardId}: {status} | {result.ErrorType ?? "OK"} | {result.ExecutionTimeMs}ms");
    }

    /// <summary>
    /// Generate test summary
    /// </summary>
    public void GenerateSummary()
    {
        sessionEndTime = DateTime.UtcNow;

        var summary = new StringBuilder();
        summary.AppendLine("\n═══════════════════════════════════════════════════════");
        summary.AppendLine("FASE 26: TEST SUMMARY REPORT");
        summary.AppendLine("═══════════════════════════════════════════════════════");
        summary.AppendLine();

        // Basic statistics
        int totalTested = testResults.Count;
        int totalPassed = testResults.Count(r => r.Executed);
        int totalFailed = totalTested - totalPassed;
        double passRate = totalTested > 0 ? (totalPassed * 100.0) / totalTested : 0;

        summary.AppendLine($"📊 STATISTICS:");
        summary.AppendLine($"   Total Tested:     {totalTested}");
        summary.AppendLine($"   Passed:           {totalPassed} ✅");
        summary.AppendLine($"   Failed:           {totalFailed} ❌");
        summary.AppendLine($"   Pass Rate:        {passRate:F1}%");
        summary.AppendLine();

        // Performance metrics
        var execTimes = testResults.Where(r => r.ExecutionTimeMs > 0).Select(r => r.ExecutionTimeMs).ToList();
        if (execTimes.Count > 0)
        {
            summary.AppendLine($"⚡ PERFORMANCE:");
            summary.AppendLine($"   Average Time:    {execTimes.Average():F0}ms");
            summary.AppendLine($"   Min Time:        {execTimes.Min()}ms");
            summary.AppendLine($"   Max Time:        {execTimes.Max()}ms");
            summary.AppendLine();
        }

        // Memory analysis
        var memUsages = testResults.Where(r => r.MemoryUsageKB > 0).Select(r => r.MemoryUsageKB).ToList();
        if (memUsages.Count > 0)
        {
            summary.AppendLine($"💾 MEMORY:");
            summary.AppendLine($"   Average Usage:   {memUsages.Average():F0} KB");
            summary.AppendLine($"   Max Usage:       {memUsages.Max()} KB");
            summary.AppendLine($"   Total Sessions:  {memUsages.Sum() / 1024} MB");
            summary.AppendLine();
        }

        // Error breakdown
        var errors = testResults.Where(r => !r.Executed).GroupBy(r => r.ErrorType);
        if (errors.Count() > 0)
        {
            summary.AppendLine($"❌ ERROR BREAKDOWN:");
            foreach (var errorGroup in errors.OrderByDescending(g => g.Count()))
            {
                summary.AppendLine($"   {errorGroup.Key}: {errorGroup.Count()}");
            }
            summary.AppendLine();
        }

        // Time taken
        var duration = sessionEndTime - sessionStartTime;
        summary.AppendLine($"⏱️  SESSION:");
        summary.AppendLine($"   Started:  {sessionStartTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"   Ended:    {sessionEndTime:yyyy-MM-dd HH:mm:ss}");
        summary.AppendLine($"   Duration: {duration.TotalMinutes:F1} minutes");
        summary.AppendLine();

        // Recommendation
        summary.AppendLine($"📋 RECOMMENDATION:");
        if (passRate >= 95)
        {
            summary.AppendLine($"   ✅ READY FOR FASE 27 - Functions Implementation");
        }
        else if (passRate >= 85)
        {
            summary.AppendLine($"   🟡 ACCEPTABLE - Fix top 5 errors first");
        }
        else
        {
            summary.AppendLine($"   ❌ NEEDS DEBUGGING - Resolve main error categories");
        }

        summary.AppendLine();
        summary.AppendLine("═══════════════════════════════════════════════════════");

        Debug.Log(summary.ToString());
    }

    /// <summary>
    /// Export results to CSV
    /// </summary>
    public void ExportToCSV(string filename)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("CardId,CardName,Loaded,Executed,ErrorType,ErrorMessage,ExecutionTimeMs,MemoryUsageKB,ConstantsUsed,EffectsRegistered,TestTime");

        // Data rows
        foreach (var result in testResults)
        {
            var row = new List<string>
            {
                result.CardId.ToString(),
                result.CardName ?? "",
                result.Loaded.ToString(),
                result.Executed.ToString(),
                result.ErrorType ?? "",
                "\"" + (result.ErrorMessage ?? "").Replace("\"", "\"\"") + "\"",
                result.ExecutionTimeMs.ToString(),
                result.MemoryUsageKB.ToString(),
                result.ConstantsUsed.ToString(),
                result.EffectsRegistered.ToString(),
                result.TestTime.ToString("O")
            };

            csv.AppendLine(string.Join(",", row));
        }

        // Write file
        string filePath = Path.Combine("Assets/Scripts/Reports", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, csv.ToString());

        Debug.Log($"✅ CSV exported to: {filePath}");
    }

    /// <summary>
    /// Export results to JSON
    /// </summary>
    public void ExportToJSON(string filename)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine("  \"sessionInfo\": {");
        json.AppendLine($"    \"startTime\": \"{sessionStartTime:O}\",");
        json.AppendLine($"    \"endTime\": \"{sessionEndTime:O}\",");
        json.AppendLine($"    \"durationMinutes\": {(sessionEndTime - sessionStartTime).TotalMinutes}");
        json.AppendLine("  },");
        json.AppendLine("  \"summary\": {");
        
        int totalTested = testResults.Count;
        int totalPassed = testResults.Count(r => r.Executed);
        json.AppendLine($"    \"totalTested\": {totalTested},");
        json.AppendLine($"    \"passed\": {totalPassed},");
        json.AppendLine($"    \"failed\": {totalTested - totalPassed},");
        json.AppendLine($"    \"passRate\": {(totalTested > 0 ? (totalPassed * 100.0) / totalTested : 0)}");
        json.AppendLine("  },");
        json.AppendLine("  \"results\": [");

        // Result rows
        for (int i = 0; i < testResults.Count; i++)
        {
            var result = testResults[i];
            json.AppendLine("    {");
            json.AppendLine($"      \"cardId\": {result.CardId},");
            json.AppendLine($"      \"loaded\": {result.Loaded.ToString().ToLower()},");
            json.AppendLine($"      \"executed\": {result.Executed.ToString().ToLower()},");
            json.AppendLine($"      \"errorType\": \"{result.ErrorType ?? ""}\",");
            json.AppendLine($"      \"executionTimeMs\": {result.ExecutionTimeMs},");
            json.AppendLine($"      \"memoryUsageKB\": {result.MemoryUsageKB}");
            json.Append("    }");
            
            if (i < testResults.Count - 1)
                json.AppendLine(",");
            else
                json.AppendLine();
        }

        json.AppendLine("  ]");
        json.AppendLine("}");

        // Write file
        string filePath = Path.Combine("Assets/Scripts/Reports", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, json.ToString());

        Debug.Log($"✅ JSON exported to: {filePath}");
    }

    /// <summary>
    /// Get statistics dictionary
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        int totalTested = testResults.Count;
        int totalPassed = testResults.Count(r => r.Executed);
        var execTimes = testResults.Where(r => r.ExecutionTimeMs > 0).Select(r => r.ExecutionTimeMs).ToList();

        return new Dictionary<string, object>
        {
            { "totalTested", totalTested },
            { "totalPassed", totalPassed },
            { "totalFailed", totalTested - totalPassed },
            { "passRate", totalTested > 0 ? (totalPassed * 100.0) / totalTested : 0 },
            { "avgExecutionTimeMs", execTimes.Count > 0 ? execTimes.Average() : 0 },
            { "minExecutionTimeMs", execTimes.Count > 0 ? execTimes.Min() : 0 },
            { "maxExecutionTimeMs", execTimes.Count > 0 ? execTimes.Max() : 0 },
            { "sessionDurationMinutes", (sessionEndTime - sessionStartTime).TotalMinutes }
        };
    }

    /// <summary>
    /// Get failed tests
    /// </summary>
    public List<LuaCardScriptTester.TestResult> GetFailedTests()
    {
        return testResults.Where(r => !r.Executed).ToList();
    }

    /// <summary>
    /// Get top slowest tests
    /// </summary>
    public List<LuaCardScriptTester.TestResult> GetSlowestTests(int count = 10)
    {
        return testResults
            .OrderByDescending(r => r.ExecutionTimeMs)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Clear all results
    /// </summary>
    public void ClearResults()
    {
        testResults.Clear();
        sessionStartTime = DateTime.UtcNow;
    }
}
