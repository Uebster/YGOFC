// Assets/Scripts/FASE27_QuickTest.cs
// Quick Runtime Test for 40 Critical Cards
using UnityEngine;
using System.Collections.Generic;

public class FASE27_QuickTest : MonoBehaviour
{
    [SerializeField] private bool runTestsOnStart = false;
    private LuaCardScriptTester tester;

    void Start()
    {
        if (!runTestsOnStart) return;
        
        tester = GetComponent<LuaCardScriptTester>();
        if (tester == null) tester = gameObject.AddComponent<LuaCardScriptTester>();

        // Test 40 critical cards
        List<string> criticalCards = new List<string>
        {
            // MissingMethod - IsCanBeSpecialSummoned (9 cards)
            "cDM0009", "cDM0012", "cDM0028", "cDM0061", "cDM0076", "cDM0090", "cDM0095", "cDM0096", "cDM0118",
            
            // MissingMethod - IsEnvironment (4 cards)
            "cDM0008", "cDM0052", "cDM0054", "cDM0091",
            
            // MissingMethod - Fusion.AddProcMix (4 cards)
            "cDM0036", "cDM0051", "cDM0079", "cDM0083",
            
            // MissingMethod - Spirit.AddProcedure (1 card)
            "cDM0113",
            
            // NilIndexing - Sample 22 cards
            "cDM0006", "cDM0010", "cDM0014", "cDM0020", "cDM0024", "cDM0027", "cDM0042", "cDM0057",
            "cDM0082", "cDM0084", "cDM0088", "cDM0098", "cDM0111", "cDM0112", "cDM0114", "cDM0117"
        };

        int passCount = 0;
        int failCount = 0;

        Debug.Log("🧪 [FASE 27] Testing " + criticalCards.Count + " critical cards...\n");

        foreach (string cardId in criticalCards)
        {
            bool success = TestSingleCard(cardId);
            if (success) passCount++;
            else failCount++;
        }

        Debug.Log($"\n✅ TESTE SUMMARY: {passCount} PASS | ❌ {failCount} FAIL");
        Debug.Log($"Success Rate: {(passCount * 100 / criticalCards.Count)}%\n");
    }

    bool TestSingleCard(string cardId)
    {
        try
        {
            // Attempt to load and validate script
            var result = tester.ValidateSingleCard(cardId);
            
            if (result)
            {
                Debug.Log($"✅ {cardId}: Script loaded successfully");
                return true;
            }
            else
            {
                Debug.LogError($"❌ {cardId}: Script validation failed");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ {cardId}: {ex.Message}");
            return false;
        }
    }
}
