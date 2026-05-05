using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.IO;

public class AutoTestRunner : MonoBehaviour
{
    [Header("Configurações do Teste")]
    [Tooltip("Ative para rodar o teste automaticamente ao dar Play nesta cena.")]
    public bool runOnStart = false;

    private bool isRunning = false;
    private List<string> errorLogs = new List<string>();
    private string reportPath;

    void Start()
    {
        if (runOnStart)
        {
            RunMassValidation();
        }
    }

    [ContextMenu("🚀 INICIAR VALIDAÇÃO EM MASSA DE DRY-RUN (LUA)")]
    public void RunMassValidation()
    {
        if (isRunning) return;
        StartCoroutine(ValidationRoutine());
    }

    // O TRUQUE DE MESTRE: Ouve os erros que a engine C# cospe no Console e joga no nosso TXT!
    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (condition.Contains("[API LUA CRASH]"))
        {
            errorLogs.Add(condition);
        }
    }

    private IEnumerator ValidationRoutine()
    {
        isRunning = true;
        
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null || CardEffectManager.Instance == null) 
        {
            Debug.LogError("<color=red>[AutoTestRunner]</color> O jogo precisa estar rodando (Play) com o GameManager e o CardEffectManager ativos na cena.");
            isRunning = false;
            yield break;
        }

        Debug.Log("<color=cyan>[AutoTestRunner]</color> Iniciando Validação em Massa de LUA (Baseado no Banco de Dados)...");

        reportPath = Path.Combine(Application.dataPath, "AutoTestRunner_Report.txt");
        errorLogs.Clear();
        errorLogs.Add("=== RELATÓRIO DO TESTE EM MASSA (LUA) ===");
        errorLogs.Add($"Data: {System.DateTime.Now}");
        errorLogs.Add("-----------------------------------\n");

        // Inscreve o nosso espião de logs
        Application.logMessageReceived += OnLogMessage;

        int total = 0, success = 0, failed = 0;

        // Usa a engine nativa do jogo, que já carregou o constant.lua e utility.lua perfeitamente!
        Script luaEngine = CardEffectManager.Instance.luaEngine;

        foreach (var card in GameManager.Instance.cardDatabase.cardDatabase)
        {
            // Pula Monstros Normais (sem efeito) para focar no que importa
            if (card.type.Contains("Normal") && !card.type.Contains("Effect")) continue;
            
            total++;
            
            // Exatamente o Dummy blindado que a rotina antiga usava
            GameObject dummyGO = new GameObject("DummyTester_" + card.id);
            dummyGO.AddComponent<UnityEngine.UI.RawImage>(); 
            CardDisplay dummy = dummyGO.AddComponent<CardDisplay>();
            dummy.gameObject.SetActive(false); 
            dummy.SetCard(card, null, true);

            int errorsBefore = errorLogs.Count;
            bool runtimeError = false;

            try
            {
                // Retornando à glória da ferramenta antiga!
                LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(dummy);
                if (lc != null) 
                {
                    foreach (var eff in new List<LuaEffect>(lc.registeredEffects))
                    {
                        try {
                            LuaEffect dummyRe = new LuaEffect { owner = lc };
                            LuaCard dummyChkc = new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 });
                            LuaGroup dummyEg = new LuaGroup(); // Previne o Crash de 'eg is nil' nas Armadilhas!
                            object eg = dummyEg;

                            // Se for efeito ativável manualmente, manda o Player (tp), senão manda a Carta (c)
                            bool isManualActivate = (eff.type == 0x0010 || eff.type == 0x0040 || eff.type == 0x0080 || eff.type == 0x0100);
                            object arg2 = isManualActivate ? (object)lc.GetControler() : (object)lc;

                            System.Action<Closure, int> TestFunc = (func, chkArg) => {
                                if (func == null) return;
                                try {
                                    if (chkArg == -1) luaEngine.Call(func, eff, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                                    else if (chkArg == 0) luaEngine.Call(func, eff, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                                    else if (chkArg == 1) luaEngine.Call(func, eff, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
                                } catch (System.Exception firstEx) {
                                    try {
                                        // Fallback: Tenta inverter o arg2 (tp vs lc) caso a assinatura da carta fuja do padrão
                                        object fallbackArg2 = isManualActivate ? (object)lc : (object)lc.GetControler();
                                        if (chkArg == -1) luaEngine.Call(func, eff, fallbackArg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                                        else if (chkArg == 0) luaEngine.Call(func, eff, fallbackArg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                                        else if (chkArg == 1) luaEngine.Call(func, eff, fallbackArg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
                                    } catch {
                                        throw firstEx; // Se o fallback também falhar, joga a exceção original!
                                    }
                                }
                            };

                            TestFunc(eff.conditionFunc, -1);
                            TestFunc(eff.costFunc, 0);
                            TestFunc(eff.targetFunc, 1);
                        } catch (System.Exception ex) {
                            string err = $"[RUNTIME] Falha na carta {card.name} ({card.id}): {ex.Message}";
                            errorLogs.Add(err);
                            runtimeError = true;
                        }
                    }
                }
                else
                {
                    runtimeError = true; // Se lc for nulo, a carta falhou na compilação do Loader!
                }
            }
            catch (System.Exception ex)
            {
                string err = $"[CRÍTICO/SINTAXE] Falha na carta {card.name} ({card.id}): {ex.Message}";
                errorLogs.Add(err);
                runtimeError = true;
            }

            if (CardEffectManager.Instance != null && CardEffectManager.Instance.activeLuaCards.ContainsKey(dummy))
                CardEffectManager.Instance.activeLuaCards.Remove(dummy);
                
            DestroyImmediate(dummyGO.gameObject);
            
            if (errorLogs.Count > errorsBefore || runtimeError) failed++;
            else success++;
        }

        // Desinscreve o espião de logs
        Application.logMessageReceived -= OnLogMessage;

        string summary = $"\n=== [CONCLUÍDO] ===\nTotal Escaneado: {total}\nSucessos: {success}\nFalhas: {failed}";
        errorLogs.Add(summary);
        
        File.WriteAllLines(reportPath, errorLogs);
        Debug.Log($"<color=green>{summary}</color>\n<color=cyan>Relatório completo salvo em: {reportPath}</color>");
        
        isRunning = false;
        
        yield return null; // Apenas para cumprir a interface do IEnumerator sem pausas internas
    }
}