using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OpponentAI : MonoBehaviour
{
    public static OpponentAI Instance;

    public enum AIPersonality { Balanced, Aggressive, Defensive, Combo, Fearful, Gambler, Controller, Chaotic }
    public enum AIDynamicState { Aggressive, Cautious, Critical } // Estados A, B, C

    public struct PersonalityWeights {
        public float wGain;
        public float wLoss;
        public float wLife;
        public float wCA; // Card Advantage
    }

    [System.Serializable]
    public class AIMemoryRecord
    {
        public string actionId;
        public float weight;
    }

    [System.Serializable]
    public class AIMemoryData
    {
        public List<AIMemoryRecord> records = new List<AIMemoryRecord>();
    }

    [Header("Configuração da IA")]
    public AIPersonality currentPersonality = AIPersonality.Balanced;
    [Tooltip("Tempo em segundos que a IA espera entre ações para simular pensamento.")]
    public float actionDelay = 1.2f;
    [Tooltip("A pontuação mínima que uma ação precisa ter para ser considerada.")]
    public float actionScoreThreshold = 10f;
    public bool isThinking = false;

    // Propriedade para detectar modo simulação e acelerar
    private bool useSimulationFastMode => GameManager.Instance != null && GameManager.Instance.isSimulating;
    
    // Sistema de prevenção de loop infinito na simulação
    private HashSet<int> usedCardsThisTurn = new HashSet<int>();
    private int simulationActionCount = 0;
    private const int MAX_ACTIONS_PER_PHASE = 20; // Limite seguro de ações por fase

    [Header("Estado Calculado (Runtime)")]
    public int fearScore = 0;           // Quantidade de S/T setadas pelo jogador
    public float boardValue = 0f;       // Quem está ganhando a mesa? (+ = IA, - = Jogador)
    public int panicThreshold = 1000;   // Limite de LP para parar de pagar custos
    public AIDynamicState currentState = AIDynamicState.Cautious; // Estado dinâmico de risco
    
    [Header("Pesos Calculados")]
    public PersonalityWeights currentWeights;

    [Header("Memória de Curto Prazo")]
    public Dictionary<int, string> knownCards = new Dictionary<int, string>();

    [Header("Machine Learning (Memória Heurística)")]
    public AIMemoryData aiMemory = new AIMemoryData();
    private Dictionary<string, float> activeMemoryWeights = new Dictionary<string, float>();
    public List<string> actionsTakenThisDuel = new List<string>();
    private string currentMemoryProfile = "Generic";

    // Sementes para a personalidade Chaotic
    private float chaoticGain = 1f;
    private float chaoticLoss = 1f;
    private float chaoticLife = 1f;
    private float chaoticCA = 1f;

    // Classe interna para representar uma jogada e sua pontuação
    private class AIAction
    {
        public System.Action Execute { get; set; }
        public float Score { get; set; }
        public string Description { get; set; }
        public int CardInstanceID { get; set; }
        public string MemoryID { get; set; }
    }

    void Awake()
    {
        Instance = this;
    }

    // --- MACHINE LEARNING E MEMÓRIA ---
    public void InitializeMemoryForDuel(string profileId)
    {
        currentMemoryProfile = string.IsNullOrEmpty(profileId) ? "Generic" : profileId;
        actionsTakenThisDuel.Clear();
        knownCards.Clear();
        activeMemoryWeights.Clear();

        string path = System.IO.Path.Combine(Application.persistentDataPath, $"AI_Memory_{currentMemoryProfile}.json");
        if (System.IO.File.Exists(path))
        {
            try {
                string json = System.IO.File.ReadAllText(path);
                aiMemory = JsonUtility.FromJson<AIMemoryData>(json);
                foreach (var record in aiMemory.records) activeMemoryWeights[record.actionId] = record.weight;
                Debug.Log($"<color=magenta>[AI Memory]</color> Cérebro carregado para '{currentMemoryProfile}'. ({activeMemoryWeights.Count} sinapses).");
            } catch { Debug.LogWarning($"[AI Memory] Falha ao ler memória de '{currentMemoryProfile}'."); }
        }
        else
        {
            aiMemory = new AIMemoryData();
            Debug.Log($"<color=magenta>[AI Memory]</color> Criando novo cérebro em branco para '{currentMemoryProfile}'.");
        }
    }

    public void SaveMemory()
    {
        aiMemory.records.Clear();
        foreach (var kvp in activeMemoryWeights) aiMemory.records.Add(new AIMemoryRecord { actionId = kvp.Key, weight = kvp.Value });
        string json = JsonUtility.ToJson(aiMemory, true);
        string path = System.IO.Path.Combine(Application.persistentDataPath, $"AI_Memory_{currentMemoryProfile}.json");
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"<color=magenta>[AI Memory]</color> Cérebro salvo com sucesso! ({path})");
    }

    public float GetMemoryWeight(string actionId)
    {
        if (activeMemoryWeights.TryGetValue(actionId, out float weight)) return weight;
        return 1.0f; // Valor neutro base
    }

    public void RecordAction(string actionId)
    {
        if (!actionsTakenThisDuel.Contains(actionId))
        {
            actionsTakenThisDuel.Add(actionId);
            Debug.Log($"<color=magenta>[AI Memory]</color> Ação anotada na caderneta de curto prazo: {actionId}");
        }
    }

    public void ProcessEndOfDuelMemory(bool aiWon)
    {
        if (actionsTakenThisDuel.Count == 0) return;
        Debug.Log($"<color=magenta>[AI Memory]</color> Processando o Juízo Final de '{currentMemoryProfile}'. A IA venceu? {aiWon}");

        foreach (string action in actionsTakenThisDuel)
        {
            float currentWeight = GetMemoryWeight(action);
            // Se ganhou, aumenta em 5% o peso da jogada. Se perdeu, diminui em 10% (pune os erros severamente).
            if (aiWon) currentWeight += 0.05f;
            else currentWeight -= 0.1f;
            
            // Clampa os pesos para a IA não enlouquecer e ficar bitolada (Entre 0.1x e 3.0x de preferência)
            currentWeight = Mathf.Clamp(currentWeight, 0.1f, 3.0f);
            activeMemoryWeights[action] = currentWeight;
        }
        actionsTakenThisDuel.Clear();
        SaveMemory();
    }

    public void StartAITurn(bool switchTurn = true)
    {
        if (isThinking || !gameObject.activeInHierarchy) return;
        StartCoroutine(AITurnRoutine(switchTurn));
    }

    public IEnumerator AITurnRoutine(bool switchTurn = true)
    {
        isThinking = true;
        try        
        {
            Debug.Log("AI: --- INÍCIO DO TURNO ---");

            // Fixa a semente caótica para este turno (evita troca de personalidade a cada frame)
            chaoticGain = Random.Range(0.5f, 1.5f);
            chaoticLoss = Random.Range(0.5f, 1.5f);
            chaoticLife = Random.Range(0.5f, 1.5f);
            chaoticCA = Random.Range(0.5f, 1.5f);

            // Sincroniza a IA com o avanço automático de fases visual (Draw -> Standby -> Main1)
            float timeout = 0f;
            while (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != GamePhase.Main1 && timeout < 10f)
            {
                if (CardEffectManager.Instance != null && (CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isFastEffectWindowOpen || CardEffectManager.Instance.isWaitingForLuaYield))
                    timeout = 0f; // Reseta timeout se tiver cadeia rolando
                timeout += Time.deltaTime;
                yield return null;
            }

            if (!useSimulationFastMode)
                yield return new WaitForSeconds(actionDelay);

            EvaluateBoardState();

            // --- MAIN PHASE 1 ---
            yield return StartCoroutine(ExecuteMainPhaseLogic());

            // --- BATTLE PHASE ---
            if (CanEnterBattlePhase() && HasAttackCapableMonsters())
            {
                PhaseManager.Instance.ChangePhase(GamePhase.Battle);
                if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isFastEffectWindowOpen);
                
                if (!useSimulationFastMode)
                    yield return new WaitForSeconds(actionDelay);
                    
                yield return StartCoroutine(ExecuteBattlePhaseLogic());
            }

            // --- MAIN PHASE 2 ---
            PhaseManager.Instance.ChangePhase(GamePhase.Main2);
            if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isFastEffectWindowOpen);

            yield return StartCoroutine(ExecuteMainPhaseLogic()); 

            yield return StartCoroutine(ExecuteMainPhaseLogic()); 

            // --- END PHASE ---
            PhaseManager.Instance.ChangePhase(GamePhase.End);
            if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isFastEffectWindowOpen);
            
            if (!useSimulationFastMode)
                yield return new WaitForSeconds(actionDelay);

            Debug.Log("AI: --- FIM DO TURNO ---");
            
            if (switchTurn)
            {
                GameManager.Instance.SwitchTurn();
            }
        }
        finally
        {
            isThinking = false;
        }
    }

    // O "cérebro" principal da IA durante as Main Phases
    IEnumerator ExecuteMainPhaseLogic()
    {
        // Reavalia o campo sempre que entra na Main Phase
        EvaluateBoardState();
        
        // Reset de rastreamento para nova fase
        if (useSimulationFastMode)
        {
            usedCardsThisTurn.Clear();
            simulationActionCount = 0;
        }

        while (true)
        {
            // Proteção contra loop infinito na simulação
            if (useSimulationFastMode && simulationActionCount >= MAX_ACTIONS_PER_PHASE)
            {
                Debug.LogWarning($"AI: Limite de ações por fase atingido ({MAX_ACTIONS_PER_PHASE}). Finalizando fase.");
                break;
            }

            // Em simulação usa modo "burro", senão modo estratégico
            List<AIAction> possibleActions = useSimulationFastMode 
                ? EvaluateDumbActions()  
                : EvaluateAllPossibleActions();

            // 2. Se não há jogadas, encerra a fase
            if (possibleActions.Count == 0)
            {
                if (useSimulationFastMode) Debug.Log($"[SIM] AI: Nenhuma ação nova encontrada. Finalizando fase ({simulationActionCount} ações executadas).");
                else Debug.Log("AI: Nenhuma ação possível encontrada.");
                break;
            }

            // 3. Ordena as jogadas pela maior pontuação
            possibleActions.Sort((a, b) => b.Score.CompareTo(a.Score));
            AIAction bestAction = possibleActions[0];

            // Em simulação pula o threshold
            if (!useSimulationFastMode && bestAction.Score < actionScoreThreshold)
            {
                Debug.Log($"AI: Melhor ação ({bestAction.Description}) tem pontuação baixa ({bestAction.Score}). Guardando recursos.");
                break;
            }

            // 5. Executa a melhor jogada
            if (useSimulationFastMode)
            {
                simulationActionCount++;
                Debug.Log($"[SIM {simulationActionCount}] AI AÇÃO (Score: {bestAction.Score}): {bestAction.Description}");
            }
            else
                Debug.Log($"AI AÇÃO (Score: {bestAction.Score}): {bestAction.Description}");
                
            bestAction.Execute();
            
            if (!string.IsNullOrEmpty(bestAction.MemoryID) && !useSimulationFastMode)
                RecordAction(bestAction.MemoryID); // Anota na Caderneta que tomou essa decisão
            
            if (useSimulationFastMode && bestAction.CardInstanceID != 0)
                usedCardsThisTurn.Add(bestAction.CardInstanceID);
            
            // Pausa base para a engine gráfica iniciar a animação da jogada
            yield return new WaitForSeconds(0.6f);
            
            // Garante que a IA espere correntes resolverem antes de pensar na próxima ação!
            if (CardEffectManager.Instance != null)
            {
                yield return new WaitWhile(() => CardEffectManager.Instance.isBusy || GameManager.Instance.pendingVisualTasks > 0);
            }

            // Espera apenas se não estiver em simulação rápida
            if (!useSimulationFastMode)
                yield return new WaitForSeconds(actionDelay);
        }
    }

    // Modo otimizado para simulação - joga cartas inteligentemente sem delays
    private List<AIAction> EvaluateDumbActions()
    {
        var actions = new List<AIAction>();

        actions.AddRange(EvaluateSummonActions().Where(a => a.Score > 0).Take(1));
        actions.AddRange(EvaluateSpellActions().Where(a => a.Score > 50).Take(2));
        actions.AddRange(EvaluateSetActions().Where(a => a.Score > 0).Take(2));
        return actions;
    }

    // Calcula o peso do campo e o perigo iminente
    private void EvaluateBoardState()
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;

        // 1. Calcular Fear Score (Quantas cartas o jogador tem setadas atrás?)
        fearScore = 0;
        foreach (var z in GameManager.Instance.duelFieldUI.playerSpellZones)
        {
            if (z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null && cd.isFlipped) fearScore++;
            }
        }
        Debug.Log($"[AI] Fear Score atual: {fearScore}");

        // 2. Calcular Board Value (Soma de forças)
        boardValue = 0;
        foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) boardValue += Mathf.Max(cd.currentAtk, cd.currentDef);
            }
        }
        foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) boardValue -= Mathf.Max(cd.currentAtk, cd.currentDef);
                    // MEMÓRIA FOTOGRÁFICA: Anota o ID de todas as cartas inimigas que a IA "viu" viradas para cima!
                    if (!cd.isFlipped && cd.CurrentCardData != null) knownCards[cd.GetInstanceID()] = cd.CurrentCardData.id;
            }
        }
        Debug.Log($"[AI] Board Value: {boardValue}");

        // Ajusta a personalidade dinamicamente dependendo da campanha
        if (GameManager.Instance.currentOpponent != null)
        {
            string diff = GameManager.Instance.currentOpponent.difficulty != null ? GameManager.Instance.currentOpponent.difficulty.ToLower() : "";
            if (diff.Contains("aggressive")) currentPersonality = AIPersonality.Aggressive;
            else if (diff.Contains("defensive")) currentPersonality = AIPersonality.Defensive;
            else if (diff.Contains("combo") || diff.Contains("tactician")) currentPersonality = AIPersonality.Combo;
            else if (diff.Contains("fearful") || diff.Contains("stall")) currentPersonality = AIPersonality.Fearful;
            else if (diff.Contains("gambler")) currentPersonality = AIPersonality.Gambler;
            else if (diff.Contains("control")) currentPersonality = AIPersonality.Controller;
            else if (diff.Contains("chaotic") || diff.Contains("random")) currentPersonality = AIPersonality.Chaotic;
        }

        // 3. Determinar Estado Dinâmico (A, B ou C)
        float lpPercentage = (float)GameManager.Instance.opponentLP / 8000f;
        if (lpPercentage >= 0.5f || boardValue >= 1500f) currentState = AIDynamicState.Aggressive; // Estado A
        else if (lpPercentage >= 0.25f) currentState = AIDynamicState.Cautious; // Estado B
        else currentState = AIDynamicState.Critical; // Estado C

        currentWeights = GetCurrentWeights();
        Debug.Log($"[AI] Estado Atual: {currentState} | Pers: {currentPersonality} | Pesos: G:{currentWeights.wGain:F1} L:{currentWeights.wLoss:F1} V:{currentWeights.wLife:F1} CA:{currentWeights.wCA:F1}");
    }

    private PersonalityWeights GetCurrentWeights()
    {
        PersonalityWeights w = new PersonalityWeights();
        
        switch(currentState)
        {
            case AIDynamicState.Aggressive: w = new PersonalityWeights { wGain = 1.0f, wLoss = 0.3f, wLife = 0.1f, wCA = 0.8f }; break;
            case AIDynamicState.Cautious:   w = new PersonalityWeights { wGain = 0.6f, wLoss = 0.8f, wLife = 0.5f, wCA = 1.2f }; break;
            case AIDynamicState.Critical:   w = new PersonalityWeights { wGain = 0.2f, wLoss = 1.5f, wLife = 3.0f, wCA = 0.5f }; break;
        }

        switch(currentPersonality)
        {
            case AIPersonality.Aggressive: w.wGain *= 1.5f; w.wLoss *= 0.6f; w.wLife *= 0.5f; w.wCA *= 0.7f; break;
            case AIPersonality.Defensive:  w.wGain *= 0.6f; w.wLoss *= 1.5f; w.wLife *= 1.8f; w.wCA *= 1.0f; break;
            case AIPersonality.Balanced:   w.wGain *= 0.9f; w.wLoss *= 1.2f; w.wLife *= 1.2f; w.wCA *= 1.1f; break;
            case AIPersonality.Fearful:    w.wGain *= 0.4f; w.wLoss *= 2.0f; w.wLife *= 2.5f; w.wCA *= 0.9f; break;
            case AIPersonality.Combo:      w.wGain *= 1.2f; w.wLoss *= 1.0f; w.wLife *= 0.9f; w.wCA *= 1.4f; break;
            case AIPersonality.Gambler:    w.wGain *= 1.8f; w.wLoss *= 0.8f; w.wLife *= 0.4f; w.wCA *= 0.6f; break;
            case AIPersonality.Controller: w.wGain *= 0.7f; w.wLoss *= 1.4f; w.wLife *= 1.5f; w.wCA *= 1.5f; break;
            case AIPersonality.Chaotic:    
                w.wGain *= chaoticGain; w.wLoss *= chaoticLoss; 
                w.wLife *= chaoticLife; w.wCA *= chaoticCA; break;
        }

        // FOG OF WAR: Oponentes com muitas cartas setadas aumentam o peso da perda
        w.wLoss *= (1.0f + (fearScore * 0.25f));
        return w;
    }

    // Reúne todas as avaliações
    private List<AIAction> EvaluateAllPossibleActions()
    {
        List<AIAction> actions = new List<AIAction>();

        actions.AddRange(EvaluateSummonActions());
        actions.AddRange(EvaluateSpellActions());
        actions.AddRange(EvaluateSetActions());
        actions.AddRange(EvaluateFieldMonsterActions());
        actions.AddRange(EvaluateChangePositionActions());

        return actions;
    }

    private List<AIAction> EvaluateChangePositionActions()
    {
        var actions = new List<AIAction>();
        foreach (var monster in GetMyMonstersOnField())
        {
            if (monster.attacksThisTurn > 0 || monster.hasChangedPositionThisTurn || monster.summonedTurnCount == GameManager.Instance.turnCount) continue;
            
            bool shouldChange = false;
            string desc = "";
            float score = 0;

            if (currentState == AIDynamicState.Critical && monster.position == CardDisplay.BattlePosition.Attack)
            {
                shouldChange = true;
                desc = $"Virar {monster.CurrentCardData.name} para Defesa (Estado Crítico)";
                score = 200 * currentWeights.wLife;
            }
            else if (currentPersonality == AIPersonality.Defensive && monster.position == CardDisplay.BattlePosition.Attack)
            {
                shouldChange = true;
                desc = $"Virar {monster.CurrentCardData.name} para Defesa (Defensivo)";
                score = 150 * currentWeights.wLoss;
            }
            else if (currentState == AIDynamicState.Aggressive && monster.position == CardDisplay.BattlePosition.Defense && monster.currentAtk > monster.currentDef)
            {
                shouldChange = true;
                desc = $"Virar {monster.CurrentCardData.name} para Ataque (Agressivo)";
                score = 150 * currentWeights.wGain;
            }

            if (shouldChange)
            {
                actions.Add(new AIAction {
                    Score = score,
                    Description = desc,
                    Execute = () => { monster.hasChangedPositionThisTurn = true; monster.ChangePosition(); },
                    CardInstanceID = monster.GetInstanceID()
                });
            }
        }
        return actions;
    }

    #region Avaliação de Ações

    private List<AIAction> EvaluateSummonActions()
    {
        var actions = new List<AIAction>();
        if (GameManager.Instance.normalSummonsThisTurnOpponent > 0) return actions;

        var hand = GameManager.Instance.opponentHand;
        var monstersInHand = hand.Select(go => go.GetComponent<CardDisplay>()).Where(cd => cd != null && cd.CurrentCardData != null && cd.CurrentCardData.type.Contains("Monster") && !cd.CurrentCardData.type.Contains("Ritual") && !cd.CurrentCardData.type.Contains("Fusion") && !cd.CurrentCardData.type.Contains("Synchro") && !cd.CurrentCardData.type.Contains("Xyz") && !cd.CurrentCardData.type.Contains("Link")).ToList();
        
        int myMonsterCount = GetMyMonstersOnField().Count;
        int playerStrongestATK = GetPlayerStrongestAtk();
        int myStrongestATK = GetStrongestMyMonsterAtk();

        foreach (var monster in monstersInHand)
        {
            int tributesNeeded = 0;
            if (monster.CurrentCardData.level >= 5 && monster.CurrentCardData.level <= 6) tributesNeeded = 1;
            if (monster.CurrentCardData.level >= 7) tributesNeeded = 2;
            if (myMonsterCount < tributesNeeded) continue;

            float tributeCost = CalculateSmartTributeCost(tributesNeeded);
            float baseScore = 0;

            // --- LÓGICA DE TRIBUTO: MP1 vs MP2 ---
            if (tributesNeeded > 0)
            {
                // REGRA DO ESTADO C: Nunca tributar no estado crítico. Preservar monstros para defesa.
                if (currentState == AIDynamicState.Critical && currentPersonality != AIPersonality.Gambler)
                {
                    continue;
                }

                // Regra da Isca (Baiting)
                if (fearScore >= 2 && currentPersonality != AIPersonality.Aggressive)
                {
                    if (PhaseManager.Instance.currentPhase == GamePhase.Main1) continue; // Evita tributar na MP1, usa iscas.
                }
                else if (PhaseManager.Instance.currentPhase == GamePhase.Main1)
                {
                    // Na MP1, avalia se o tributo é matematicamente vantajoso para o combate
                    if (playerStrongestATK == 0) 
                    {
                        // Campo aberto: Se os tributos batem mais que o Boss, não tributa na MP1! (Teto de Dano)
                        float rawAtkTributeCost = GetRawTributeAttackPower(tributesNeeded);
                        if (rawAtkTributeCost > monster.CurrentCardData.atk) continue; 
                    }
                    else if (playerStrongestATK >= myStrongestATK)
                    {
                        // Estamos travados por uma parede. O novo monstro vira o jogo?
                        if (monster.CurrentCardData.atk > playerStrongestATK)
                        {
                            baseScore += 2000; // Breakthrough! Muito vantajoso!
                        }
                    }
                }
            }

            // Avalia invocar em Ataque
            float attackScore = baseScore + monster.CurrentCardData.atk - playerStrongestATK;
            if (playerStrongestATK == 0) attackScore += 500 * currentWeights.wGain; // Bônus por campo aberto
            if (tributesNeeded > 0) attackScore -= (tributeCost * currentWeights.wLoss) + (tributesNeeded * 500 * currentWeights.wCA); // Custo e desvantagem de cartas
            
            // Na MP2, invocar em ataque é menos útil a menos que seja uma parede muito forte
            if (PhaseManager.Instance.currentPhase == GamePhase.Main2 && monster.CurrentCardData.atk < playerStrongestATK) 
                attackScore -= 1000;
            
            // TABELA VERDADE: Invocação em Posição de Ataque
            if (currentState == AIDynamicState.Critical)
            {
                // C: Nunca invoca em ataque se for mais fraco que a ameaça. Foco na Defesa!
                if (monster.CurrentCardData.atk <= playerStrongestATK && currentPersonality != AIPersonality.Gambler) attackScore -= 5000;
                else attackScore += 1000 * currentWeights.wLife; // Se puder virar o jogo e salvar a vida
            }
            else if (currentState == AIDynamicState.Cautious)
            {
                // B: Só invoca em ataque se tiver bom ATK para bater de frente
                if (monster.CurrentCardData.atk < 1800 && monster.CurrentCardData.atk <= playerStrongestATK) attackScore -= 1500;
            }
            
            if (currentPersonality == AIPersonality.Fearful) attackScore -= 1000 * currentWeights.wLoss;

            // MACHINE LEARNING: Consulta se fomos punidos no passado por sermos gulosos com invocações ofensivas
            string memIdAtk = $"SUM_ATK_{monster.CurrentCardData.id}";
            attackScore *= GetMemoryWeight(memIdAtk);

            actions.Add(new AIAction {
                Score = attackScore,
                Description = $"Invocar {monster.CurrentCardData.name} em Ataque.",
                Execute = () => GameManager.Instance.TrySummonMonster(monster.gameObject, monster.CurrentCardData, false),
                CardInstanceID = monster.GetInstanceID(), MemoryID = memIdAtk
                });

            // Avalia invocar em Defesa (Set)
            float defenseScore = baseScore + monster.CurrentCardData.def - playerStrongestATK;
            if (monster.CurrentCardData.def > 2000) defenseScore += 500; // Bônus por ser uma boa parede
            if (tributesNeeded > 0) defenseScore -= (tributeCost * currentWeights.wLoss) + (tributesNeeded * 500 * currentWeights.wCA);

            // TÁTICA DE SOBREVIVÊNCIA: Se nosso campo está aberto, SETAR um monstro é vital para não tomar OTK!
            if (myMonsterCount == 0 && playerStrongestATK > 0)
            {
                defenseScore += playerStrongestATK; // A pontuação de defesa sobe proporcionalmente ao dano que ela vai absorver!
            }

            // Regra de Ocultação: Monstros setados na defesa devem preferencialmente ser baixados na MP2
            if (PhaseManager.Instance.currentPhase == GamePhase.Main2) 
            {
                defenseScore += 1000;
            }
            else 
            {
                // Na MP1, penaliza setar monstros (a menos que a IA esteja desesperada)
                if (myMonsterCount > 0) defenseScore -= 600; 
            }
            
            // TABELA VERDADE: Invocação em Posição de Defesa (Set)
            if (currentState == AIDynamicState.Critical)
            {
                // C: Obrigatório. Ganha um boost imenso para garantir a parede de proteção.
                defenseScore += 3000 * currentWeights.wLife;
            }
            else if (currentState == AIDynamicState.Cautious)
            {
                // B: Preferencial para não arriscar muito.
                defenseScore += 800 * currentWeights.wLoss;
            }
            else if (currentState == AIDynamicState.Aggressive)
            {
                // A: Raro. Em estado agressivo a IA quer bater, só recua se for inútil em ataque.
                if (monster.CurrentCardData.atk >= 1500) defenseScore -= 2000;
            }

            if (currentPersonality == AIPersonality.Fearful) defenseScore += 1500 * currentWeights.wLife;
            
            string memIdDef = $"SUM_DEF_{monster.CurrentCardData.id}";
            defenseScore *= GetMemoryWeight(memIdDef);

            actions.Add(new AIAction {
                Score = defenseScore,
                Description = $"Baixar (Set) {monster.CurrentCardData.name} em Defesa.",
                Execute = () => GameManager.Instance.TrySummonMonster(monster.gameObject, monster.CurrentCardData, true),
                CardInstanceID = monster.GetInstanceID(), MemoryID = memIdDef
            });
        }
        return actions;
    }

    private List<AIAction> EvaluateFieldMonsterActions()
    {
        var actions = new List<AIAction>();
        var myMonsters = GetMyMonstersOnField();

        foreach (var monster in myMonsters)
        {
            if (monster == null || monster.CurrentCardData == null || usedCardsThisTurn.Contains(monster.GetInstanceID()) || monster.isFlipped) continue;

            string id = monster.CurrentCardData.id;

            // INTEGRAÇÃO LUA: Verifica se o monstro tem um Efeito de Ignição e se o custo (chk=0) pode ser pago
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(monster);
            LuaEffect ignitionEffect = lc?.registeredEffects.Find(e => e.type == 0x0040); // 0x0040 = IGNITION

            if (ignitionEffect != null && CardEffectManager.Instance.CanActivateEffect(lc, ignitionEffect, 1, null))
            {
                float score = 500; // Pontuação base por usar um efeito no campo
                
                // Pontuações heurísticas específicas
                if (id == "1513") score = GetStrongestPlayerMonster()?.currentAtk + 1000 ?? 0;
                else if ((ignitionEffect.category & 0x20000) != 0) score = 800 + (fearScore * 100); // 0x20000 = DESTROY

                actions.Add(new AIAction {
                    Score = score,
                    Description = $"Ativar efeito de Ignição de {monster.CurrentCardData.name}.",
                    Execute = () => { 
                        CardEffectManager.Instance.ActivateCard(monster, null, null); 
                        usedCardsThisTurn.Add(monster.GetInstanceID()); 
                    }
                });
            }
        }
        return actions;
    }

    private List<AIAction> EvaluateSpellActions()
    {
        var actions = new List<AIAction>();
        var hand = new List<GameObject>(GameManager.Instance.opponentHand);

        foreach (var go in hand)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd == null || cd.CurrentCardData == null || !cd.CurrentCardData.type.Contains("Spell")) continue;

            // INTEGRAÇÃO LUA: Checagem estrita de Ativação (chk=0)
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
            LuaEffect activationEffect = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040);
            
            // Se a carta Lua exigir descartar 1 carta e a IA estiver sem mão, o CanActivateEffect retorna FALSE e a IA pula.
            if (activationEffect != null && !CardEffectManager.Instance.CanActivateEffect(lc, activationEffect, 1, null)) continue;

            float score = 0;
            string description = "";
            System.Action execution = null;
            
            if (activationEffect != null)
            {
                int cat = activationEffect.category;
                
                // --- AVALIAÇÃO GENÉRICA BASEADA NO MOTOR LUA (DRY-RUN) ---
                if ((cat & 0x1) != 0) // CATEGORY_DESTROY
                {
                    int opponentCards = GetPlayerMonsterCount() + GameManager.Instance.duelFieldUI.playerSpellZones.Count(z => z.childCount > 0);
                    
                    string desc = cd.CurrentCardData.description.ToLower();
                    if (desc.Contains("spell") || desc.Contains("trap"))
                    {
                        int playerST = GameManager.Instance.duelFieldUI.playerSpellZones.Count(z => z.childCount > 0);
                        score += playerST * 600;
                        if (playerST == 0) score -= 5000;
                    }
                    else if (desc.Contains("monster"))
                    {
                        int playerMonsters = GetPlayerMonsterCount();
                        int playerStrongestAtk = GetPlayerStrongestAtk();
                        // REGRA DA DESTRUIÇÃO (Raigeki/Dark Hole) baseada no Estado
                        if (playerMonsters == 0) score -= 5000;
                        else if (currentState == AIDynamicState.Critical) score += 5000 * currentWeights.wLife; // C: Usa imediato para evitar morte                        
                        else if (playerStrongestAtk < 2000 && currentPersonality != AIPersonality.Gambler && currentPersonality != AIPersonality.Aggressive)
                        {
                            // ECONOMIA DE REMOÇÃO: Não gasta mágicas proativas fortes (Raigeki/Fissure) em monstros fracos!
                            score -= 3000 * currentWeights.wCA;
                        }
                        else if (desc.Contains("all monsters") && currentState == AIDynamicState.Aggressive && playerMonsters < 3 && currentPersonality != AIPersonality.Gambler) score -= 2000 * currentWeights.wLoss; // A: Segura pra ter mais valor
                        else score += (playerMonsters * 600) * currentWeights.wGain;
                    }
                    else 
                    {
                        score += opponentCards * 500;
                        if (opponentCards == 0) score -= 5000;
                    }
                }
                if ((cat & 0x200) != 0) // CATEGORY_SPECIAL_SUMMON
                {
                    CardData bestGyMonster = GetBestGraveyardMonster();
                    score += 1000 + (bestGyMonster != null ? (bestGyMonster.atk * currentWeights.wGain) : 0);
                }
                if ((cat & 0x10000) != 0) // CATEGORY_DRAW
                {
                    if (GameManager.Instance.GetOpponentMainDeck().Count <= 3) score -= 5000; // Previne suicídio por Deck Out
                    else score += 1500 * currentWeights.wCA; // Alta prioridade para CA
                }
                if ((cat & 0x8) != 0) // CATEGORY_TOHAND
                {
                    score += 500 * currentWeights.wGain;
                }
                if ((cat & 0x80000) != 0) // CATEGORY_DAMAGE
                {
                    score += 600 * currentWeights.wGain;
                }
                if ((cat & 0x100000) != 0) // CATEGORY_RECOVER
                {
                    score += 300 * currentWeights.wLife;
                }
                if ((cat & 0x1000) != 0) // CATEGORY_CONTROL
                {
                    int playerMonsters = GetPlayerMonsterCount();
                    score += playerMonsters * 1500 * currentWeights.wGain;
                    if (playerMonsters == 0) score -= 5000;
                }
                if ((cat & 0x80) != 0) // CATEGORY_HANDES
                {
                    int playerHand = GameManager.Instance.playerHand.Count;
                    score += playerHand * 1000 * currentWeights.wCA; // Vantagem de cartas pura
                    if (playerHand == 0) score -= 5000;
                }

                description = $"Ativar Magia {cd.CurrentCardData.name} [Categorias LUA avaliadas]";
                execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);

                // Lógica de Equipamento baseada no Estado
                if (score <= 0 && cd.CurrentCardData.property == "Equip")
                {
                    if (currentState == AIDynamicState.Critical && currentPersonality != AIPersonality.Gambler) score -= 5000; // C: Guarda equipamento, não desperdiça recursos agressivos
                    else
                    {
                        // Regra de Ouro: Tall vs Wide
                        var myMonsters = GetMyMonstersOnField().Where(m => !m.isFlipped).OrderByDescending(m => m.currentAtk).ToList();
                        CardDisplay bestTargetToEquip = null;
                        
                        int playerMaxAtk = GetPlayerStrongestAtk();
                        int myMaxAtk = myMonsters.Count > 0 ? myMonsters[0].currentAtk : 0;
                        int estimatedBoost = 500; // Bônus médio de equipamento
                        
                        // Estado Cauteloso: Não equipa se ainda assim o monstro for mais fraco que a ameaça do oponente
                        if (currentState == AIDynamicState.Cautious && (myMaxAtk + estimatedBoost < playerMaxAtk))
                        {
                            score -= 5000;
                        }
                        else if (playerMaxAtk > myMaxAtk && myMonsters.Count > 0) {
                            bestTargetToEquip = myMonsters[0]; // Tall: Concentra no mais forte para tentar passar o boss inimigo
                        } else if (myMonsters.Count > 1) {
                            if (myMaxAtk >= 2500) bestTargetToEquip = myMonsters[myMonsters.Count - 1]; // Wide: Espalha força no mais fraco para dividir riscos
                            else bestTargetToEquip = myMonsters[0];
                        } else if (myMonsters.Count == 1) {
                            bestTargetToEquip = myMonsters[0];
                        }

                        if (bestTargetToEquip != null)
                        {
                            score = 400; // Base score for an equip
                            description = $"Equipar {cd.CurrentCardData.name} em {bestTargetToEquip.CurrentCardData.name} (Estratégia: {(playerMaxAtk > myMaxAtk ? "Tall" : "Wide")}).";
                            execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                        }
                    }
                }
                else if (cd.CurrentCardData.property == "Field" || cd.CurrentCardData.type.Contains("Continuous"))
                {
                    bool isDefensive = (cat & 0x2000) != 0 || (cat & 0x800) != 0 || cd.CurrentCardData.description.ToLower().Contains("attack");
                    
                    // TABELA VERDADE: Mágicas Contínuas e Campo
                    if (currentState == AIDynamicState.Critical && !isDefensive)
                    {
                        score -= 5000 * currentWeights.wLoss; // C: Não gasta carta à toa se não salva vida ou bloqueia ataque
                    }
                    else if (currentState == AIDynamicState.Cautious && PhaseManager.Instance.currentPhase == GamePhase.Main1)
                    {
                        score += 100 * currentWeights.wLoss; // B: Cauteloso. Segura para jogar apenas na MP2 para evitar limpezas
                    }
                    else
                    {
                        score += 300 * currentWeights.wGain; // A: Joga no início do turno para estabelecer pressão
                    }
                    
                    execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                }
            }
            else
            {
                score += 100;
                description = $"Ativar {cd.CurrentCardData.name}";
                execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
            }

            if (execution != null)
            {
                // MACHINE LEARNING: Multiplica a nota pelo sucesso histórico de usar essa Mágica específica
                string memIdSpell = $"ACT_{cd.CurrentCardData.id}";
                score *= GetMemoryWeight(memIdSpell);
                
                actions.Add(new AIAction { Score = score, Description = description, Execute = execution, CardInstanceID = cd.GetInstanceID(), MemoryID = memIdSpell });
            }
        }
        return actions;
    }

    private List<AIAction> EvaluateSetActions()
    {
        var actions = new List<AIAction>();
        
        // Regra de Overextension (Não setar tudo de uma vez para não perder para Heavy Storm)
        var hand = GameManager.Instance.opponentHand;
        int occupiedZones = 0;
        foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if(z.childCount > 0) occupiedZones++;
        
        int maxSafeSets = 2;
        switch(currentPersonality) {
            case AIPersonality.Defensive: maxSafeSets = 4; break;
            case AIPersonality.Fearful: maxSafeSets = 5; break;
            case AIPersonality.Controller: maxSafeSets = 3; break;
            case AIPersonality.Aggressive: maxSafeSets = 1; break; // Agressivo prefere não setar mágicas
        }
        if (currentState == AIDynamicState.Critical) maxSafeSets = 5; // C: Dane-se a prudência, baixe tudo que pode nos salvar!
        if (currentPersonality == AIPersonality.Gambler) maxSafeSets = Random.Range(1, 6);
        
        if (occupiedZones >= maxSafeSets) return actions; // Previne tempestade

        foreach (var go in hand)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd != null && cd.CurrentCardData != null)
            {
                if (cd.CurrentCardData.type.Contains("Trap"))
                {
                    float score = 100; // Pontuação base para baixar uma armadilha
                    if (PhaseManager.Instance.currentPhase == GamePhase.Main2) 
                        score += 800;
                    else {
                        score -= 500; // Desencoraja setar na MP1
                    }

                    LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                    LuaEffect quickEffect = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0080);
                    if (quickEffect != null)
                    {
                        int cat = quickEffect.category;
                        if ((cat & 0x1) != 0) score += 500 * currentWeights.wGain; // Destrói
                        if ((cat & 0x10000000) != 0) score += 600 * (currentPersonality == AIPersonality.Controller ? 2f : 1f); // Nega
                        if ((cat & 0x200) != 0) score += 400 * currentWeights.wCA; // Special Summon
                    }
                    
                    string memIdTrap = $"SET_{cd.CurrentCardData.id}";
                    score *= GetMemoryWeight(memIdTrap);

                    actions.Add(new AIAction {
                        Score = score,
                        Description = $"Baixar (Set) Armadilha {cd.CurrentCardData.name}.",
                        Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true),
                        CardInstanceID = cd.GetInstanceID(), MemoryID = memIdTrap
                        });
                }
                else if (cd.CurrentCardData.type.Contains("Spell"))
                {
                    // Baixa Spells Rápidas (Quick-Play) na MP2 para usar como defesa no turno do inimigo
                    if (cd.CurrentCardData.property == "Quick-Play" && PhaseManager.Instance.currentPhase == GamePhase.Main2)
                    {
                        string memIdSet = $"SET_{cd.CurrentCardData.id}";
                        float setScore = 400 * GetMemoryWeight(memIdSet);

                        actions.Add(new AIAction {
                            Score = setScore,
                            Description = $"Baixar (Set) Quick-Play Spell {cd.CurrentCardData.name}.",
                            Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true),
                            CardInstanceID = cd.GetInstanceID(), MemoryID = memIdSet
                            });
                    }
                    // Blefe (Bluff) se estivermos perdendo terreno e quisermos assustar o jogador
                    else if (fearScore < 1 && PhaseManager.Instance.currentPhase == GamePhase.Main2 && boardValue < -1000)
                    {
                        float bluffScore = 150 * GetMemoryWeight("TACTIC_BLUFF");

                        actions.Add(new AIAction {
                            Score = bluffScore, // Melhor setar como Blefe do que tomar OTK sem dar medo
                            Description = $"Baixar (Set) Magia de Blefe {cd.CurrentCardData.name}.",
                            Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true),
                            CardInstanceID = cd.GetInstanceID(), MemoryID = "TACTIC_BLUFF"
                        });
                    }
                }
            }
        }
        
        int allowedSets = Mathf.Max(0, maxSafeSets - occupiedZones);
        return actions.OrderByDescending(a => a.Score).Take(allowedSets).ToList();
    }

    #endregion

    #region Lógica Matemática Avançada (Heurísticas)

    #endregion

    #region Lógica de Batalha (Melhorada)

    IEnumerator ExecuteBattlePhaseLogic()
    {
        List<CardDisplay> myMonsters = GetMyMonstersOnField();
        
        // Prioriza monstros que são OBRIGADOS a atacar (EFFECT_MUST_ATTACK)
        var mustAttackers = myMonsters.Where(m => {
            var lc = CardEffectManager.Instance.EnsureCardScriptLoaded(m);
            return lc != null && lc.IsHasEffect(191).Type != MoonSharp.Interpreter.DataType.Nil;
        }).OrderByDescending(m => m.currentAtk).ToList();

        var otherAttackers = myMonsters.Except(mustAttackers).OrderByDescending(m => m.currentAtk).ToList();

        // Concatena, garantindo que os obrigatórios venham primeiro
        var attackOrder = mustAttackers.Concat(otherAttackers).ToList();

        foreach (var attacker in attackOrder)        
        {
            LuaCard atkLc = CardEffectManager.Instance != null ? CardEffectManager.Instance.EnsureCardScriptLoaded(attacker) : null;
            bool canDefAttack = atkLc != null && atkLc.IsHasEffect(190).Type != MoonSharp.Interpreter.DataType.Nil;
            
            if (attacker == null || (attacker.position == CardDisplay.BattlePosition.Defense && !canDefAttack) || attacker.attacksThisTurn >= attacker.maxAttacks) continue;
            
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
                LuaCard cachedCard = atkLc ?? new LuaCard(attacker);
                if (CardEffectManager.Instance.auraManager.IsUnderRestriction(cachedCard, null, "CANNOT_ATTACK", CardLocation.Field)) continue;
            }

            // Repete o ataque para monstros com múltiplos ataques
            while(attacker.attacksThisTurn < attacker.maxAttacks)
            {
                // Avalia o melhor alvo para este atacante
                CardDisplay bestTarget = FindBestTarget(attacker);
                bool didAttack = false;

                if (bestTarget != null) // Encontrou um alvo vantajoso
                {
                    // MACHINE LEARNING (Anotação de Traumas): A IA atacou um monstro que ela NÃO SABIA O QUE ERA?
                    if (bestTarget.isFlipped && !knownCards.ContainsKey(bestTarget.GetInstanceID()))
                    {
                        RecordAction("ATTACK_UNKNOWN_FACEDOWN");
                    }

                    Debug.Log($"AI: {attacker.CurrentCardData.name} ataca {bestTarget.CurrentCardData.name}!");
                    attacker.attacksThisTurn++;
                    if (CardEffectManager.Instance != null) {
                        CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(attacker);
                        CardEffectManager.Instance.luaDuel.currentAttackTarget = new LuaCard(bestTarget);
                        var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
                        yield return StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, 
                            CardEffectManager.Instance.luaDuel.currentAttacker, 
                            CardEffectManager.Instance.luaDuel.currentAttackTarget));
                    }
                    didAttack = true;
                }
                else if (GetPlayerMonsterCount() == 0) // Campo aberto
                {
                    if (atkLc != null && atkLc.IsHasEffect(73).Type != MoonSharp.Interpreter.DataType.Nil) break; // EFFECT_CANNOT_DIRECT_ATTACK

                    // REGRA DO RELATÓRIO TÉCNICO: Ataque Direto vs Estado Dinâmico
                    bool isLethal = GameManager.Instance.playerLP <= attacker.currentAtk;
                    if (currentPersonality == AIPersonality.Fearful && fearScore > 0 && !isLethal)
                    {
                        Debug.Log($"AI: {attacker.CurrentCardData.name} cancela ataque direto (Personalidade Medrosa). Risco inaceitável.");
                        break;
                    }
                    if (currentState == AIDynamicState.Critical && !isLethal && fearScore > 0 && currentPersonality != AIPersonality.Gambler)
                    {
                        Debug.Log($"AI: {attacker.CurrentCardData.name} cancela ataque direto (Estado Crítico). Medo de ativação de armadilha fatal.");
                        break;
                    }
                    else if (currentState == AIDynamicState.Cautious && fearScore >= 2 && !isLethal && currentPersonality != AIPersonality.Aggressive)
                    {
                        Debug.Log($"AI: {attacker.CurrentCardData.name} recua do ataque direto (Estado Cauteloso). Alto risco de armadilhas.");
                        break;
                    }

                    Debug.Log($"AI: {attacker.CurrentCardData.name} ataca diretamente!");
                    attacker.attacksThisTurn++;
                    if (CardEffectManager.Instance != null) {
                        CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(attacker);
                        CardEffectManager.Instance.luaDuel.currentAttackTarget = null;
                        var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
                        yield return StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, 
                            CardEffectManager.Instance.luaDuel.currentAttacker, 
                            null));
                    }
                    didAttack = true;
                }
                else
                {
                    Debug.Log($"AI: {attacker.CurrentCardData.name} não encontrou um alvo vantajoso. Não vai atacar.");
                    break; // Sai do loop de múltiplos ataques se não houver alvos bons
                }

                if (didAttack)
                {
                    if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isFastEffectWindowOpen || GameManager.Instance.pendingVisualTasks > 0);
                    if (!useSimulationFastMode) yield return new WaitForSeconds(actionDelay + 1.0f);
                }
                else break; // Se não atacou, não tenta de novo
            }
        }
    }

    CardDisplay FindBestTarget(CardDisplay attacker)
    {
        CardDisplay bestTarget = null;
        float bestScore = -10000; // Inicia com score negativo

        // Determina se o atacante atual é o nosso "Boss Monster"
        List<CardDisplay> myMonsters = GetMyMonstersOnField();
        CardDisplay myBoss = myMonsters.OrderByDescending(m => m.currentAtk).FirstOrDefault();
        bool isBossAttacking = (attacker == myBoss);

        // --- IDENTIFICAÇÃO DE ALVOS OBRIGATÓRIOS (TAUNT) ---
        List<CardDisplay> mandatoryTargets = new List<CardDisplay>();
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones) {
            if (zone.childCount > 0) {
                var defender = zone.GetComponentInChildren<CardDisplay>();
                if (defender != null) {
                    LuaCard defLc = CardEffectManager.Instance.EnsureCardScriptLoaded(defender);
                    if (defLc != null && defLc.IsHasEffect(196).Type != MoonSharp.Interpreter.DataType.Nil) // EFFECT_ONLY_BE_ATTACKED
                        mandatoryTargets.Add(defender);
                }
            }
        }
        bool hasMandatoryTarget = mandatoryTargets.Count > 0;

        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var defender = zone.GetComponentInChildren<CardDisplay>();
                if (defender == null || defender.CurrentCardData == null) continue;
                
                // Se o oponente tem alvos com TAUNT, a IA só pode analisar eles!
                if (hasMandatoryTarget && !mandatoryTargets.Contains(defender)) continue;

                LuaCard defLc = CardEffectManager.Instance.EnsureCardScriptLoaded(defender);
                if (defLc != null && defLc.IsHasEffect(70).Type != MoonSharp.Interpreter.DataType.Nil) continue; // EFFECT_CANNOT_BE_BATTLE_TARGET

                float score = 0;

                // --- AVALIAÇÃO DE ALVOS DE ALTO VALOR (Floodgates) ---
                if (!defender.isFlipped)
                {
                    if (defender.CurrentCardData.type.Contains("Effect")) score += 300; // Prioriza matar efeito
                }

                // --- SCOUTING (Lidar com Face-Down) ---
                if (defender.isFlipped) // Monstro Setado (Face-down)
                {
                    // A IA lembra dessa carta? (Memória Fotográfica)
                    bool isKnown = knownCards.ContainsKey(defender.GetInstanceID());
                    
                    if (isKnown)
                    {
                        CardData knownData = GameManager.Instance.cardDatabase.GetCardById(knownCards[defender.GetInstanceID()]);
                        if (knownData != null)
                        {
                            if (attacker.currentAtk > knownData.def)
                            {
                                if (knownData.description.Contains("FLIP:") || knownData.type.Contains("Effect")) score += 500 * currentWeights.wGain;
                                else score += 50 + knownData.def;
                            }
                            else score -= 5000; // Bater numa parede que a gente já conhece é suicídio burro
                        }
                    }
                    else
                    {
                        // CARTA DESCONHECIDA!
                        if (currentPersonality == AIPersonality.Fearful) {
                            score -= 5000;
                        } else if (myMonsters.Count >= 2) {
                            // TÁTICA DO BOSS VANGUARD: Se temos 2+ monstros, o Boss deve bater primeiro no Face-Down para quebrar a parede e abrir o campo pro Lacaio!
                            if (isBossAttacking) score += 600 * currentWeights.wGain;
                            else score -= 1000; // Lacaios aguardam o Boss agir
                        } else if (currentState == AIDynamicState.Critical && currentPersonality != AIPersonality.Gambler) {
                            score -= 5000 * currentWeights.wLoss; // C: Só temos 1 monstro. Atacar e morrer pro MEB nos deixa vulneráveis a um Ataque Direto letal. Recua.
                        } else if (currentState == AIDynamicState.Aggressive || currentPersonality == AIPersonality.Aggressive) {
                            score += 500 * currentWeights.wGain;
                        } else {
                            // B (Cauteloso):
                            int estimatedDef = 1500;
                            if (attacker.currentAtk > estimatedDef + 500) score += 200;
                            else score -= 1000;
                        }
                        
                        score *= GetMemoryWeight("ATTACK_UNKNOWN_FACEDOWN"); // MACHINE LEARNING: Aplica o Trauma caso já tenha sido explodida muito por atacar Face-Downs
                    }
                }
                else if (defender.position == CardDisplay.BattlePosition.Attack)
                {
                    if (attacker.currentAtk > defender.currentAtk)
                    {
                        // RANK DE AMEAÇA: Decide qual monstro é mais perigoso destruir
                        float threatScore = ComputeThreatScore(defender);
                        
                        // Estado Crítico prioriza remover dano puro da mesa. A/B priorizam engines (efeitos).
                        if (currentState == AIDynamicState.Critical)
                            score += (100 + defender.currentAtk) * currentWeights.wLife; 
                        else
                            score += (100 + (threatScore * 10f)) * currentWeights.wGain; 
                    }
                    else
                    {
                        // O SUICIDE CRASH (Bater pra Morrer Taticamente)
                        string[] floaters = { "1587", "2097", "1307", "1639", "0680", "1279", "2004", "0753", "1419", "0749" }; // Sangan, Witch, Tomate, etc
                        bool isFloater = floaters.Contains(attacker.CurrentCardData.id);
                        int damageToTake = defender.currentAtk - attacker.currentAtk;
                        
                        if (currentState == AIDynamicState.Critical && currentPersonality != AIPersonality.Gambler)
                        {
                            score -= 5000; // C: NUNCA ataca monstros mais fortes. Prioridade é sobreviver.
                        }
                        // Só se suicida em estado Agressivo se a vida permitir
                        else if (isFloater && (GameManager.Instance.opponentLP - damageToTake > panicThreshold) && damageToTake <= 1500 && (currentState == AIDynamicState.Aggressive || currentPersonality == AIPersonality.Combo))
                        {
                            score += 300; // Bônus por sacrificar Sangan pra buscar carta chave
                        }
                        else
                        {
                            score -= 1000 + damageToTake; // Péssima ideia
                        }
                    }
                }
                else // Defesa
                {
                    if (attacker.currentAtk > defender.currentDef)
                    {
                        // Bom para limpar o campo, mas não causa dano. Prioridade menor.
                        score += 50 + defender.currentDef; 
                    }
                    else
                    {
                        // Atacar uma parede é inútil
                        score = -500;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = defender;
                }
            }
        }
        
        // Se a melhor jogada ainda for ruim (ex: atacar um monstro mais forte), não ataca.
        if (bestScore < 0) return null;

        return bestTarget;
    }

    // --- LÓGICA DE LIMITE DE MÃO ---

    public List<CardDisplay> GetSmartDiscardTargets(int count)
    {
        List<GameObject> hand = new List<GameObject>(GameManager.Instance.opponentHand);
        if (hand.Count == 0) return new List<CardDisplay>();

        var scoredCards = hand.Select(go => {
            var cd = go.GetComponent<CardDisplay>();
            float score = 0;
            if (cd != null && cd.CurrentCardData != null)
            {
                if (cd.CurrentCardData.type.Contains("Monster"))
                {
                    if (cd.CurrentCardData.level >= 5)
                    {
                        score += cd.CurrentCardData.atk / 10f; 
                    }
                    else
                    {
                        score += 200 + (cd.CurrentCardData.atk / 10f); // Lacaios são úteis para baixar
                    }
                }
                else 
                {
                    LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                    LuaEffect eff = lc?.registeredEffects.Find(e => e.type == 0x0010);
                    if (eff != null)
                    {
                        if ((eff.category & 0x10000) != 0) score += 1000; // Draw
                        if ((eff.category & 0x200) != 0) score += 1000; // SpSummon
                        if ((eff.category & 0x1) != 0) score += 900; // Destroy
                    }
                    score += 300;
                }
            }
            return new { Display = cd, Score = score };
        }).OrderBy(x => x.Score).ToList(); // Ordena do menor (pior carta) para o maior

        return scoredCards.Take(count).Select(x => x.Display).ToList();
    }

    public void PerformHandLimitDiscard(int count)
    {
        var targets = GetSmartDiscardTargets(count);
        foreach(var t in targets)
        {
            Debug.Log($"AI Hand Limit: Descartando taticamente {t.CurrentCardData.name}");
            GameManager.Instance.DiscardCard(t);
        }
    }

    public CardDisplay GetBestCardToDiscardFromOpponent()
    {
        List<GameObject> hand = new List<GameObject>(GameManager.Instance.playerHand);
        if (hand.Count == 0) return null;

        var scoredCards = hand.Select(go => {
            var cd = go.GetComponent<CardDisplay>();
            float score = 0;
            if (cd != null && cd.CurrentCardData != null)
            {
                if (cd.CurrentCardData.type.Contains("Monster"))
                {
                    if (cd.CurrentCardData.level <= 4) score += cd.CurrentCardData.atk;
                    else score += cd.CurrentCardData.atk * 0.5f;
                }
                else
                {
                    LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                    LuaEffect eff = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0080);
                    if (eff != null)
                    {
                        if ((eff.category & 0x1) != 0) score += 1800;
                        if ((eff.category & 0x10000) != 0) score += 1200;
                        if ((eff.category & 0x200) != 0) score += 1500;
                    }
                    score += 500;
                }
            }
            return new { Display = cd, Score = score };
        }).OrderByDescending(x => x.Score).ToList();

        return scoredCards.FirstOrDefault()?.Display;
    }

    public CardDisplay ChooseBestResponse(List<CardDisplay> validResponses, ChainManager.ChainLink triggerLink)
    {
        if (validResponses == null || validResponses.Count == 0) return null;

        var scoredResponses = validResponses.Select(card => {
            float score = 100; 

            // INTEGRAÇÃO LUA: Verifica se pode ativar a Armadilha/Quick-Play em resposta (Pagar custo, Alvo válido)
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(card);
            LuaEffect quickEffect = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0080); // ACTIVATE / TRIGGER_O
            
            object argsToPass = triggerLink != null ? (triggerLink.isDummy ? triggerLink.triggerArgs : triggerLink.card) : null;

            if (quickEffect != null && !CardEffectManager.Instance.CanActivateEffect(lc, quickEffect, 1, argsToPass))
            {
                return new { Card = card, Score = -9999f }; // Cortado na raiz.
            }

            if (triggerLink != null && triggerLink.card != null)
            {
                int cat = quickEffect != null ? quickEffect.category : 0;
                int eventCode = triggerLink.effect != null ? triggerLink.effect.code : 0;
                
                CardDisplay triggerCard = null;
                if (argsToPass is LuaCard tc) triggerCard = tc.unityCard;
                else if (argsToPass is LuaGroup g && g.cards.Count > 0) triggerCard = g.cards[0].unityCard;

                if (eventCode == 1130) // EVENT_ATTACK_ANNOUNCE
                {
                    var attacker = CardEffectManager.Instance.luaDuel.currentAttacker?.unityCard;
                    var target = CardEffectManager.Instance.luaDuel.currentAttackTarget?.unityCard;
                    
                    int attackerAtk = attacker != null ? attacker.currentAtk : 0;
                    int targetPower = 0;
                    if (target != null) {
                        targetPower = target.position == CardDisplay.BattlePosition.Attack ? target.currentAtk : target.currentDef;
                    }

                    bool isLethal = GameManager.Instance.opponentLP <= attackerAtk;

                    // Se a IA for o alvo e for ganhar a batalha, ela NÃO gasta armadilha (guarda recurso)
                    if (target != null && targetPower > attackerAtk && !target.isFlipped) {
                        score -= 5000;
                    }
                    else if ((cat & 0x1) != 0 || (cat & 0x8) != 0) // DESTROY or TOHAND (Mirror Force, Sakuretsu, Dimensional Prison)
                    {
                        // Fogo Amigo: Se o atacante for da IA, não ativa armadilha contra si
                        if (attacker != null && !attacker.isPlayerCard)
                        {
                            score -= 9000;
                        }
                        else
                        {
                            if (currentState == AIDynamicState.Critical || isLethal || currentPersonality == AIPersonality.Fearful) {
                                score += 5000 * currentWeights.wLife; // C: Usa imediatamente no primeiro ataque (Questão de Vida ou Morte)
                            } else if (currentState == AIDynamicState.Cautious || currentPersonality == AIPersonality.Controller) {
                                if (attackerAtk >= 2000 || target == null) score += attackerAtk * currentWeights.wGain; // B: Guarda para ataque grande
                                else score -= 500;
                            } else {
                                // Agressivo: Só usa se for para salvar um monstro seu ou evitar muito dano
                                if (target != null || attackerAtk >= 2500) score += attackerAtk * currentWeights.wGain;
                                else score -= 1000;
                            }
                        }
                    }
                    else // Outras armadilhas de resposta a ataque (ex: Waboku, Negate Attack, Roar)
                    {
                        if (attacker != null && !attacker.isPlayerCard)
                        {
                            score -= 9000;
                        }
                        else
                        {
                            if (currentState == AIDynamicState.Critical || isLethal || currentPersonality == AIPersonality.Fearful) {
                                score += 4000 * currentWeights.wLife; // C: Usa no primeiro ataque garantido
                            } else if (currentState == AIDynamicState.Cautious || currentPersonality == AIPersonality.Defensive) {
                                if (attackerAtk >= 2000 || target == null) score += 500 * currentWeights.wLife; // B: Guarda para bloquear dano grande
                                else score -= 1000;
                            } else {
                                score -= 1000; // A: Guarda recurso, não liga pra dano menor
                            }
                        }
                    }
                }
                else if (eventCode == 1100 || eventCode == 1101 || eventCode == 1105) // SUMMON_SUCCESS, FLIP_SUMMON, SPSUMMON
                {
                    int summonedAtk = triggerCard != null ? triggerCard.currentAtk : 0;
                    bool isLifeOrDeath = GameManager.Instance.opponentLP <= panicThreshold || GameManager.Instance.opponentLP <= summonedAtk;
                    bool preventingTribute = GetPlayerMonsterCount() > 1; // Jogador já tem outro monstro, pode estar acumulando para tributar
                    
                    // A Arte da Proteção de Tributo (Setup)
                    bool protectingTributeFodder = false;
                    bool hasHighLevelInHand = GameManager.Instance.opponentHand.Any(go => {
                        var cd = go.GetComponent<CardDisplay>();
                        return cd != null && cd.CurrentCardData.type.Contains("Monster") && cd.CurrentCardData.level >= 5;
                    });
                    
                    if (hasHighLevelInHand)
                    {
                        var myMonsters = GetMyMonstersOnField();
                        if (myMonsters.Count > 0)
                        {
                            int maxOtherPlayerAtk = 0;
                            foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones) {
                                if (zone.childCount > 0) {
                                    var m = zone.GetComponentInChildren<CardDisplay>();
                                    // Procura outras ameaças em posição de ataque
                                    if (m != null && m != triggerCard && !m.isFlipped && m.position == CardDisplay.BattlePosition.Attack) {
                                        if (m.currentAtk > maxOtherPlayerAtk) maxOtherPlayerAtk = m.currentAtk;
                                    }
                                }
                            }
                            
                            foreach (var myM in myMonsters) {
                                int myStat = myM.position == CardDisplay.BattlePosition.Attack ? myM.currentAtk : myM.currentDef;
                                // Se o novo monstro mata nosso lacaio, mas os outros monstros antigos não matariam... nós o salvamos!
                                if (summonedAtk > myStat && maxOtherPlayerAtk <= myStat) {
                                    protectingTributeFodder = true;
                                    break;
                                }
                            }
                        }
                    }

                    if ((cat & 0x1) != 0 || (cat & 0x4) != 0) // DESTROY or REMOVE (Trap Hole, Bottomless)
                    {
                        // Fogo Amigo: Se a IA invocou, não ativa armadilha contra si
                        if (triggerCard != null && !triggerCard.isPlayerCard)
                        {
                            score -= 9000;
                        }
                        else
                        {
                            if (currentState == AIDynamicState.Critical || isLifeOrDeath || currentPersonality == AIPersonality.Fearful) {
                                score += 5000 * currentWeights.wLife; // C: Usa imediatamente para evitar que QUALQUER monstro ataque
                            }
                            else if (summonedAtk >= 1500) score += summonedAtk * currentWeights.wGain; // A/B: Espera valer a pena
                            else if (preventingTribute) score += 500; 
                            else if (protectingTributeFodder) score += 800;
                            else score -= 5000;
                        }
                    }
                }
                else // Any other chain link (CardActivation)
                {
                    if ((cat & 0x10000000) != 0) // CATEGORY_NEGATE (Solemn Judgment, Magic Jammer, Dark Bribe)
                    {
                        if (GameManager.Instance.opponentLP <= panicThreshold && currentPersonality != AIPersonality.Gambler)
                        {
                            score -= 5000; // Não gasta LP em negações arriscadas se a vida estiver no limiar de pânico
                        }
                        score += 1500 * (currentPersonality == AIPersonality.Controller ? 2f : 1f);
                    }
                }
            }
            
            return new { Card = card, Score = score };
        }).OrderByDescending(x => x.Score).ToList();

        var best = scoredResponses.FirstOrDefault();
        if (best != null && best.Score > 0)
        {
            RecordAction($"RESPOND_{best.Card.CurrentCardData.id}"); // MACHINE LEARNING: Anota o trauma/vitória dessa resposta
            return best.Card;
        }
        return null;
    }

    #endregion

    #region Lógica LUA de Resolução (O Bypass de Seleção)

    // Usado pela Ponte Lua (LuaAPI.cs) quando a Engine pede para a IA escolher alvos
    public LuaGroup SelectLuaTargets(LuaGroup candidates, int min, int max)
    {
        LuaGroup result = new LuaGroup();
        if (candidates == null || candidates.cards.Count == 0) return result;

        int toSelect = Mathf.Clamp(max, 1, candidates.cards.Count);
        
        // Heurística de Seleção de Alvo (O que a IA gosta de mirar?)
        var sorted = candidates.cards.OrderByDescending(c => {
            float score = 0;
            bool isMine = c.IsControler(1); // 1 = Oponente (IA)
            bool isField = c.IsLocation(0x04) || c.IsLocation(0x08); // Campo
            bool isGrave = c.IsLocation(0x10); // Cemitério
            bool isHand = c.IsLocation(0x02); // Mão
            bool isSpellTrap = c.IsSpellTrap();
            
            LuaEffect currentEff = null;
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
            {
                if (CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
                    currentEff = CardEffectManager.Instance.chainManager.resolvingLink.effect;
                else
                    currentEff = CardEffectManager.Instance.luaDuel.currentActivatingEffect;
            }

            bool isPositiveEffect = false;
            bool isNegativeEffect = false;
            bool isCost = false;

            if (currentEff != null)
            {
                int cat = currentEff.category;
                
                if ((cat & 0x40000) != 0 || (cat & 0x200000) != 0 || (cat & 0x400000) != 0 || (cat & 0x100000) != 0 || currentEff.isTypeEquip || (currentEff.owner != null && currentEff.owner.unityData != null && currentEff.owner.unityData.property == "Equip"))
                    isPositiveEffect = true; 
                    
                if ((cat & 0x1) != 0 || (cat & 0x4) != 0 || (cat & 0x8) != 0 || (cat & 0x10) != 0 || (cat & 0x20) != 0 || (cat & 0x10000000) != 0 || (cat & 0x2000) != 0)
                    isNegativeEffect = true; 
                    
                if ((cat & 0x1000) != 0) // CATEGORY_CONTROL (Snatch Steal, Change of Heart)
                {
                    isPositiveEffect = false;
                    isNegativeEffect = true;
                }
                
                if ((cat & 0x80) != 0) // CATEGORY_HANDES (Confiscation, Delinquent Duo)
                {
                    isNegativeEffect = true;
                }

                if (CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.activeChainTasks > 0 && !CardEffectManager.Instance.isChainResolving)
                    isCost = true; // Chk=0/Chk=1 da ativação (pagando tributos, descartando da própria mão)

                // --- ESTRATÉGIA ESPECÍFICA PARA A CARTA "ANTE" ---
                if (currentEff.owner != null && currentEff.owner.unityData != null && (currentEff.owner.unityData.id == "11324436" || currentEff.owner.unityData.id == "DM0070" || currentEff.owner.unityData.name == "Ante"))
                {
                    if (isMine && isHand)
                    {
                        int lp = GameManager.Instance.opponentLP;
                        int lvl = c.GetLevel();
                        
                        if (lp <= 2000) 
                        {
                            // Desespero: Vida baixa, a IA precisa tentar ganhar a aposta a todo custo!
                            score += lvl * 1000; 
                        }
                        else 
                        {
                            // Estratégico: Se tem um monstro de nível MUITO alto (7 ou 8), aposta ele para esmagar o jogador.
                            if (lvl >= 7) score += 5000 + (lvl * 100);
                            // Caso contrário, joga a carta mais inútil/fraca (nível 0 de Spells/Traps ou nível baixo) para perder de propósito e poupar os monstros bons.
                            else score -= (lvl * 100) + c.GetAttack(); 
                        }
                        return score; // Retorna imediatamente para não misturar com as outras lógicas
                    }
                }
            }

            if (isCost && isMine)
            {
                if (isHand) score -= (c.GetAttack() + c.GetLevel() * 100) * currentWeights.wLoss;
                else if (isField) score -= c.GetAttack() * currentWeights.wLoss;
                return score;
            }

            if (isNegativeEffect)
            {
                if (!isMine)
                {
                    if (isField && !isSpellTrap) 
                    {
                        float targetThreat = c.GetAttack() + c.GetDefense();
                        
                        // ECONOMIA DE DANO E PROTEÇÃO: Se estamos apanhando na Battle Phase e temos que destruir alguém,
                        // nós punimos drasticamente a prioridade de destruir monstros que JÁ ATACARAM neste turno, mirando nos que ainda vão bater!
                        if (c.unityCard != null && c.unityCard.attacksThisTurn > 0 && PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle)
                        {
                            targetThreat *= 0.3f; 
                        }
                        
                        score += (10000 + targetThreat) * currentWeights.wGain;
                    }
                    else if (isField && isSpellTrap) score += 8000 * currentWeights.wGain;
                    else if (isHand) 
                    {
                        if (isSpellTrap) score += 9000 * currentWeights.wCA;
                        else score += (5000 + c.GetAttack()) * currentWeights.wCA;
                    }
                    else if (isGrave) score += (5000 + c.GetAttack()) * currentWeights.wGain;
                }
                else
                {
                    if (isField && !isSpellTrap) score -= c.GetAttack() * currentWeights.wLoss;
                    else if (isHand) score -= (c.GetAttack() + c.GetLevel() * 100) * currentWeights.wLoss;
                }
            }
            else if (isPositiveEffect)
            {
                if (isMine)
                {
                    if (isField && !isSpellTrap) score += (10000 + c.GetAttack()) * currentWeights.wGain;
                    else if (isGrave && !isSpellTrap) score += (5000 + c.GetAttack()) * currentWeights.wGain;
                }
                else
                {
                    score -= c.GetAttack() * currentWeights.wLoss;
                }
            }
            else
            {
                if (!isMine && isField && !isSpellTrap) score += (10000 + c.GetAttack()) * currentWeights.wGain;
                else if (!isMine && isSpellTrap) score += 8000 * currentWeights.wGain;
                else if (isMine && isGrave && !isSpellTrap) score += (5000 + c.GetAttack()) * currentWeights.wGain;
                else if (isMine && isField) score -= c.GetAttack() * currentWeights.wLoss;
                else if (isMine && isHand) score -= c.GetAttack() * currentWeights.wLoss;
                else if (!isMine && isHand) score += c.GetAttack() * currentWeights.wGain; 
            }
            
            return score;
        }).ToList();

        for (int i = 0; i < toSelect && i < sorted.Count; i++)
        {
            result.AddCard(sorted[i]);
        }
        return result;
    }

    #endregion

    #region Helpers de Análise de Campo

    private float CalculateSmartTributeCost(int amount)
    {
        if (amount <= 0) return 0;
        var myMonsters = GetMyMonstersOnField();
        if (myMonsters.Count < amount) return 9999; // Custo infinito se não tem o suficiente
        
        // Ordena pela lógica de "Abdicação" (O que queremos perder primeiro?)
        myMonsters.Sort((a, b) => {
            // 1. Monstros roubados (dono original não é a IA) têm prioridade absoluta de abate
            bool aStolen = a.isPlayerCard; 
            bool bStolen = b.isPlayerCard;
            if (aStolen && !bStolen) return -1;
            if (!aStolen && bStolen) return 1;


            // 3. Menor ATK
            return a.currentAtk.CompareTo(b.currentAtk);
        });

        float cost = 0;
        for(int i=0; i<amount; i++) 
        {
            if (myMonsters[i].isPlayerCard) cost -= 1000; // Desconto imenso por ser roubado
            else cost += myMonsters[i].currentAtk;
        }
        return cost;
    }

    private CardData GetBestGraveyardMonster()
    {
        var playerGY = GameManager.Instance.GetPlayerGraveyard();
        var oppGY = GameManager.Instance.GetOpponentGraveyard();
        var allGY = playerGY.Concat(oppGY).ToList();
        
        return allGY.Where(c => c.type.Contains("Monster")).OrderByDescending(c => c.atk).FirstOrDefault();
    }

    private CardDisplay GetStrongestPlayerMonster()
    {
        CardDisplay strongest = null;
        int maxAtk = -1;
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetComponentInChildren<CardDisplay>();
                if (cd != null && !cd.isFlipped && cd.currentAtk > maxAtk)
                {
                    maxAtk = cd.currentAtk;
                    strongest = cd;
                }
            }
        }
        return strongest;
    }
    
    bool CanEnterBattlePhase()
    {
        return GameManager.Instance.turnCount > 1;
    }

    bool HasAttackCapableMonsters()
    {
        var monsters = GetMyMonstersOnField();
        return monsters.Any(m => {
            LuaCard cachedCard = CardEffectManager.Instance != null ? CardEffectManager.Instance.EnsureCardScriptLoaded(m) : null;
            bool canDefAttack = cachedCard != null && cachedCard.IsHasEffect(190).Type != MoonSharp.Interpreter.DataType.Nil; // EFFECT_DEFENSE_ATTACK
            
            if (m.position != CardDisplay.BattlePosition.Attack && !canDefAttack) return false;
            if (m.attacksThisTurn >= m.maxAttacks) return false;
            
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
                if (CardEffectManager.Instance.auraManager.IsUnderRestriction(cachedCard ?? new LuaCard(m), null, "CANNOT_ATTACK", CardLocation.Field)) return false;
            }
            return true;
        });
    }

    int GetPlayerStrongestAtk()
    {
        int maxAtk = 0;
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetComponentInChildren<CardDisplay>();
                if (cd != null && !cd.isFlipped)
                {
                    int val = (cd.position == CardDisplay.BattlePosition.Attack) ? cd.currentAtk : cd.currentDef;
                    if (val > maxAtk) maxAtk = val;
                }
            }
        }
        return maxAtk;
    }

    int GetPlayerMonsterCount()
    {
        int count = 0;
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
            if (zone.childCount > 0) count++;
        return count;
    }

    private int GetStrongestMyMonsterAtk()
    {
        int maxAtk = 0;
        foreach (var m in GetMyMonstersOnField())
        {
            if (m.position == CardDisplay.BattlePosition.Attack && m.currentAtk > maxAtk)
                maxAtk = m.currentAtk;
        }
        return maxAtk;
    }

    private float GetRawTributeAttackPower(int amount)
    {
        var myMonsters = GetMyMonstersOnField();
        if (myMonsters.Count < amount) return 9999;
        myMonsters.Sort((a, b) => a.currentAtk.CompareTo(b.currentAtk));
        float power = 0;
        for(int i=0; i<amount; i++) 
        {
            if (myMonsters[i].position == CardDisplay.BattlePosition.Attack && !myMonsters[i].isFlipped)
                power += myMonsters[i].currentAtk;
        }
        return power;
    }

    private float ComputeThreatScore(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return 0;
        
        float score = 0;
        float atkPwr = Mathf.Max(card.currentAtk, 0);
        
        // 1. Poder Destrutivo Base (ATK)
        score += (atkPwr / 100f) * 2.5f * currentWeights.wGain;

        // 2. Potencial de Virada / Engine
        if (card.CurrentCardData.type.Contains("Effect"))
        {
            score += 15f * currentWeights.wGain; // Bônus considerável para monstros de efeito
            if (card.CurrentCardData.level >= 4 && card.CurrentCardData.level <= 6) score += 10f * currentWeights.wCA; // Motores do deck
        }
        
        // 3. Subtrai o "peso" do Custo
        int tributes = 0;
        if (card.CurrentCardData.level >= 5 && card.CurrentCardData.level <= 6) tributes = 1;
        if (card.CurrentCardData.level >= 7) tributes = 2;
        score -= (tributes * 5f * currentWeights.wCA);
        
        if (card.CurrentCardData.level >= 7) score -= 15f * currentWeights.wLoss; // Monstro enorme exige recursos e é vulnerável a remoção simples
        
        return score;
    }

    List<CardDisplay> GetMyMonstersOnField()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        foreach (var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetComponentInChildren<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
        return list;
    }

    #endregion
}
