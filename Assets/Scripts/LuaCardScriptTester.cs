using System;
using System.Collections.Generic;
using System.Diagnostics;
using MoonSharp.Interpreter;
using UnityEngine;

/// <summary>
/// FASE 26: LuaCardScriptTester
/// Validates and tests all 1,498 card scripts against the constant system
/// </summary>
public class LuaCardScriptTester
{
    private Script luaEngine;
    private LuaScriptLoader scriptLoader;
    private LuaErrorCapture errorCapture;
    private TestReporter reporter;
    private Stopwatch performanceTimer;

    public class TestResult
    {
        public int CardId { get; set; }
        public string CardName { get; set; }
        public bool Loaded { get; set; }
        public bool Executed { get; set; }
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
        public long MemoryUsageKB { get; set; }
        public int ConstantsUsed { get; set; }
        public int EffectsRegistered { get; set; }
        public DateTime TestTime { get; set; }
    }

    public LuaCardScriptTester(Script engine)
    {
        luaEngine = engine ?? throw new ArgumentNullException(nameof(engine));
        scriptLoader = new LuaScriptLoader();
        errorCapture = new LuaErrorCapture();
        reporter = new TestReporter();
        performanceTimer = new Stopwatch();
    }

    /// <summary>
    /// Load and execute a single card script
    /// </summary>
    public TestResult TestSingleCard(int cardId)
    {
        var result = new TestResult
        {
            CardId = cardId,
            TestTime = DateTime.UtcNow
        };

        try
        {
            performanceTimer.Restart();
            long memoryBefore = GC.GetTotalMemory(false);

            // STEP 1: Load script
            string scriptCode = scriptLoader.LoadScriptFromFile(cardId);
            if (string.IsNullOrEmpty(scriptCode))
            {
                result.Loaded = false;
                result.ErrorType = "FileNotFound";
                result.ErrorMessage = $"Script file not found for card {cardId}";
                return result;
            }

            // STEP 2: Validate syntax
            if (!scriptLoader.ValidateLuaSyntax(scriptCode))
            {
                result.Loaded = false;
                result.ErrorType = "SyntaxError";
                result.ErrorMessage = $"Lua syntax error in card {cardId}";
                return result;
            }

            result.Loaded = true;

            // STEP 3: Execute script
            try
            {
                var scriptResult = luaEngine.DoString(scriptCode);
                result.Executed = true;
                result.EffectsRegistered = CountEffectsRegistered();
            }
            catch (Exception ex)
            {
                result.Executed = false;
                errorCapture.CaptureRuntimeError(ex);
                result.ErrorType = errorCapture.GetErrorCategory(ex);
                result.ErrorMessage = ex.Message;
            }

            // STEP 4: Measure performance
            performanceTimer.Stop();
            result.ExecutionTimeMs = performanceTimer.ElapsedMilliseconds;
            
            long memoryAfter = GC.GetTotalMemory(false);
            result.MemoryUsageKB = (memoryAfter - memoryBefore) / 1024;

            // STEP 5: Count constants used
            result.ConstantsUsed = CountConstantsUsed(scriptCode);

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorType = "UnexpectedError";
            result.ErrorMessage = ex.Message;
            errorCapture.LogErrorDetails(ex, $"CardId: {cardId}");
            return result;
        }
    }

    /// <summary>
    /// Test a batch of cards
    /// </summary>
    public List<TestResult> TestBatch(List<int> cardIds, Action<int, int> progressCallback = null)
    {
        var results = new List<TestResult>();
        int total = cardIds.Count;

        for (int i = 0; i < cardIds.Count; i++)
        {
            var result = TestSingleCard(cardIds[i]);
            results.Add(result);
            reporter.LogTestResult(cardIds[i], result);

            progressCallback?.Invoke(i + 1, total);
        }

        return results;
    }

    /// <summary>
    /// Test simple cards (111-500 bytes)
    /// </summary>
    public List<TestResult> TestSimpleCards()
    {
        UnityEngine.Debug.Log("FASE 26: Testing simple cards (111-500 bytes)...");
        var cardIds = scriptLoader.GetCardsBySize(111, 500);
        cardIds = cardIds.GetRange(0, Math.Min(50, cardIds.Count));
        return TestBatch(cardIds, (current, total) => 
            UnityEngine.Debug.Log($"Simple cards: {current}/{total}"));
    }

    /// <summary>
    /// Test standard cards (500-2,000 bytes)
    /// </summary>
    public List<TestResult> TestStandardCards()
    {
        UnityEngine.Debug.Log("FASE 26: Testing standard cards (500-2,000 bytes)...");
        var cardIds = scriptLoader.GetCardsBySize(500, 2000);
        cardIds = cardIds.GetRange(0, Math.Min(50, cardIds.Count));
        return TestBatch(cardIds, (current, total) => 
            UnityEngine.Debug.Log($"Standard cards: {current}/{total}"));
    }

    /// <summary>
    /// Test complex cards (2,000+ bytes)
    /// </summary>
    public List<TestResult> TestComplexCards()
    {
        UnityEngine.Debug.Log("FASE 26: Testing complex cards (2,000+ bytes)...");
        var cardIds = scriptLoader.GetCardsBySize(2000, int.MaxValue);
        cardIds = cardIds.GetRange(0, Math.Min(50, cardIds.Count));
        return TestBatch(cardIds, (current, total) => 
            UnityEngine.Debug.Log($"Complex cards: {current}/{total}"));
    }

    /// <summary>
    /// Test random sample of cards
    /// </summary>
    public List<TestResult> TestRandomSample(int sampleSize = 50)
    {
        UnityEngine.Debug.Log($"FASE 26: Testing random sample ({sampleSize} cards)...");
        var cardIds = scriptLoader.GetRandomCardSample(sampleSize);
        return TestBatch(cardIds, (current, total) => 
            UnityEngine.Debug.Log($"Random sample: {current}/{total}"));
    }

    /// <summary>
    /// Test all 200 sample cards (50 simple, 50 standard, 50 complex, 50 random)
    /// </summary>
    public void RunFullValidation()
    {
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
        UnityEngine.Debug.Log("FASE 26: FULL VALIDATION STARTING");
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");

        var allResults = new List<TestResult>();

        // Test each category
        allResults.AddRange(TestSimpleCards());
        allResults.AddRange(TestStandardCards());
        allResults.AddRange(TestComplexCards());
        allResults.AddRange(TestRandomSample());

        // Generate reports
        reporter.GenerateSummary();
        reporter.ExportToCSV("fase26_test_results.csv");
        reporter.ExportToJSON("fase26_test_results.json");

        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
        UnityEngine.Debug.Log("FASE 26: VALIDATION COMPLETE");
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
    }

    /// <summary>
    /// Test ALL 1,498 card scripts and capture failure details
    /// </summary>
    public TestSummary RunCompleteValidation()
    {
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
        UnityEngine.Debug.Log("FASE 27: COMPLETE VALIDATION - ALL 1,498 CARDS");
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
        UnityEngine.Debug.Log("");

        var allResults = new List<TestResult>();
        var failedResults = new List<TestResult>();
        
        var allCardIds = scriptLoader.GetAllCardIds();
        int total = allCardIds.Count;
        int passed = 0;
        int failed = 0;

        Stopwatch overallTimer = Stopwatch.StartNew();

        for (int i = 0; i < total; i++)
        {
            int cardId = allCardIds[i];
            var result = TestSingleCard(cardId);
            allResults.Add(result);

            if (result.Executed && !string.IsNullOrEmpty(result.ErrorType) && result.ErrorType != "None")
            {
                failed++;
                failedResults.Add(result);
                UnityEngine.Debug.Log($"[FAIL] Card {cardId}: {result.ErrorType} - {result.ErrorMessage}");
            }
            else if (result.Executed)
            {
                passed++;
            }
            else
            {
                failed++;
                failedResults.Add(result);
            }

            // Progress every 100 cards
            if ((i + 1) % 100 == 0)
            {
                UnityEngine.Debug.Log($"Progress: {i + 1}/{total} | Passed: {passed} | Failed: {failed}");
            }
        }

        overallTimer.Stop();

        UnityEngine.Debug.Log("");
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
        UnityEngine.Debug.Log("RESULTS SUMMARY");
        UnityEngine.Debug.Log("═══════════════════════════════════════════════");
        UnityEngine.Debug.Log($"Total Cards Tested: {total}");
        UnityEngine.Debug.Log($"Passed: {passed} ({(passed * 100.0 / total):F1}%)");
        UnityEngine.Debug.Log($"Failed: {failed} ({(failed * 100.0 / total):F1}%)");
        UnityEngine.Debug.Log($"Time Elapsed: {overallTimer.ElapsedMilliseconds}ms");
        UnityEngine.Debug.Log("");

        if (failedResults.Count > 0)
        {
            UnityEngine.Debug.Log("═══════════════════════════════════════════════");
            UnityEngine.Debug.Log($"FAILED CARDS ({failedResults.Count})");
            UnityEngine.Debug.Log("═══════════════════════════════════════════════");
            
            // Group by error type
            var errorGroups = new Dictionary<string, List<TestResult>>();
            foreach (var result in failedResults)
            {
                if (!errorGroups.ContainsKey(result.ErrorType))
                    errorGroups[result.ErrorType] = new List<TestResult>();
                errorGroups[result.ErrorType].Add(result);
            }

            foreach (var group in errorGroups)
            {
                UnityEngine.Debug.Log($"\n[{group.Key}] - {group.Value.Count} cards:");
                foreach (var card in group.Value)
                {
                    UnityEngine.Debug.Log($"  cDM{card.CardId:D4}: {card.ErrorMessage}");
                }
            }
        }

        UnityEngine.Debug.Log("═══════════════════════════════════════════════");

        return new TestSummary
        {
            TotalCards = total,
            PassedCards = passed,
            FailedCards = failed,
            FailedResults = failedResults,
            TotalTimeMs = overallTimer.ElapsedMilliseconds
        };
    }

    public class TestSummary
    {
        public int TotalCards { get; set; }
        public int PassedCards { get; set; }
        public int FailedCards { get; set; }
        public List<TestResult> FailedResults { get; set; }
        public long TotalTimeMs { get; set; }
    }

    /// <summary>
    /// Count how many effect registrations succeeded
    /// </summary>
    private int CountEffectsRegistered()
    {
        try
        {
            var effectCount = luaEngine.Globals.Get("_EFFECT_COUNT_");
            if (effectCount != null)
            {
                return (int)effectCount.Number;
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Count how many constants are referenced in script
    /// </summary>
    private int CountConstantsUsed(string scriptCode)
    {
        int count = 0;
        var constantPatterns = new[] { "EVENT_", "EFFECT_", "LOCATION_", "PHASE_", "DUEL_", "TIMING_", "HINTMSG_", "WIN_REASON_" };
        
        foreach (var pattern in constantPatterns)
        {
            count += (scriptCode.Length - scriptCode.Replace(pattern, "").Length) / pattern.Length;
        }

        return count;
    }

    /// <summary>
    /// Get memory usage of current Lua engine
    /// </summary>
    public long GetMemoryUsage()
    {
        return GC.GetTotalMemory(false) / 1024; // in KB
    }

    /// <summary>
    /// Get test statistics
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return reporter.GetStatistics();
    }
}
