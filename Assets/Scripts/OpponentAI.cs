using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OpponentAI : MonoBehaviour
{
    public static OpponentAI Instance;

    public enum AIPersonality { Balanced, Aggressive, Defensive, Combo }

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

    // Classe interna para representar uma jogada e sua pontuação
    private class AIAction
    {
        public System.Action Execute { get; set; }
        public float Score { get; set; }
        public string Description { get; set; }
    }

    void Awake()
    {
        Instance = this;
    }

    public void StartAITurn(bool switchTurn = true)
    {
        if (isThinking || !gameObject.activeInHierarchy) return;
        StartCoroutine(AITurnRoutine(switchTurn));
    }

    public IEnumerator AITurnRoutine(bool switchTurn = true)
    {
        isThinking = true;
        Debug.Log("AI: --- INÍCIO DO TURNO ---");

        // Sincroniza a IA com o avanço automático de fases visual (Draw -> Standby -> Main1)
        float timeout = 0f;
        while (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != GamePhase.Main1 && timeout < 3f)
        {
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
            
            if (!useSimulationFastMode)
                yield return new WaitForSeconds(actionDelay);
                
            yield return StartCoroutine(ExecuteBattlePhaseLogic());
        }

        // --- MAIN PHASE 2 ---
        PhaseManager.Instance.ChangePhase(GamePhase.Main2);
        yield return StartCoroutine(ExecuteMainPhaseLogic()); 

        EvaluateBoardState(); 

        // --- END PHASE ---
        PhaseManager.Instance.ChangePhase(GamePhase.End);
        
        if (!useSimulationFastMode)
            yield return new WaitForSeconds(actionDelay);

        Debug.Log("AI: --- FIM DO TURNO ---");
        
        if (switchTurn)
        {
            GameManager.Instance.SwitchTurn();
        }
        isThinking = false;
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
            
            // Espera apenas se não estiver em simulação rápida
            if (!useSimulationFastMode)
                yield return new WaitForSeconds(actionDelay);
        }
    }

    // Modo otimizado para simulação - joga cartas inteligentemente sem delays
    private List<AIAction> EvaluateDumbActions()
    {
        var actions = new List<AIAction>();
        var hand = GameManager.Instance.opponentHand;
        
        // Separa cartas por tipo (EXCLUINDO cartas já usadas neste turno)
        var monsters = hand.Where(go => {
            var cd = go.GetComponent<CardDisplay>();
            return cd != null && cd.CurrentCardData != null && cd.CurrentCardData.type.Contains("Monster") && !usedCardsThisTurn.Contains(cd.GetInstanceID());
        }).ToList();
        
        var spells = hand.Where(go => {
            var cd = go.GetComponent<CardDisplay>();
            return cd != null && cd.CurrentCardData != null && cd.CurrentCardData.type.Contains("Spell") && !cd.CurrentCardData.type.Contains("Trap") && !usedCardsThisTurn.Contains(cd.GetInstanceID());
        }).ToList();
        
        var traps = hand.Where(go => {
            var cd = go.GetComponent<CardDisplay>();
            return cd != null && cd.CurrentCardData != null && cd.CurrentCardData.type.Contains("Trap") && !usedCardsThisTurn.Contains(cd.GetInstanceID());
        }).ToList();

        // === MONSTROS: Invoca normal summon (máximo 1 por turno) ===
        if (GameManager.Instance.normalSummonsThisTurnOpponent == 0)
        {
            var bestMonster = monsters.FirstOrDefault(); // Pega primeiro disponível
            if (bestMonster != null)
            {
                var cd = bestMonster.GetComponent<CardDisplay>();
                if (cd != null)
                {
                    bool isDef = Random.value > 0.6f; // 40% chance de defesa
                    var cardGO = bestMonster;
                    var cardData = cd.CurrentCardData;
                    int cardInstanceID = cd.GetInstanceID();
                    
                    actions.Add(new AIAction {
                        Score = cd.CurrentCardData.atk + Random.value * 100f,
                        Description = $"Invocar {cd.CurrentCardData.name} em {(isDef ? "Defesa" : "Ataque")}",
                        Execute = () => {
                            bool success = GameManager.Instance.TrySummonMonster(cardGO, cardData, isDef, false);
                            if (success)
                                usedCardsThisTurn.Add(cardInstanceID); // Marca como usada APENAS se sucesso
                        }
                    });
                }
            }
        }

        // === SPELLS: Ativa cartas de magia (máximo 1 por turno simples na simulação) ===
        if (spells.Count > 0)
        {
            var bestSpell = spells.FirstOrDefault();
            if (bestSpell != null)
            {
                var cd = bestSpell.GetComponent<CardDisplay>();
                if (cd != null)
                {
                    var cardGO = bestSpell;
                    var cardData = cd.CurrentCardData;
                    int cardInstanceID = cd.GetInstanceID();
                    bool isField = cd.CurrentCardData.property == "Field";
                    
                    // Spells contínuos e Fields geralmente não consomem a ação (apenas set)
                    bool isContinuous = cd.CurrentCardData.type.Contains("Continuous");
                    
                    actions.Add(new AIAction {
                        Score = (isContinuous ? 100f : 50f) + (isField ? 50f : 0f),
                        Description = $"Ativar {cd.CurrentCardData.name}",
                        Execute = () => {
                            bool success = GameManager.Instance.PlaySpellTrap(cardGO, cardData, false); // false = ativa, não set
                            if (success)
                                usedCardsThisTurn.Add(cardInstanceID);
                        }
                    });
                }
            }
        }

        // === TRAPS: Baixa (será ativada por trigger depois) ===
        if (traps.Count > 0)
        {
            var bestTrap = traps.FirstOrDefault();
            if (bestTrap != null)
            {
                var cd = bestTrap.GetComponent<CardDisplay>();
                if (cd != null)
                {
                    var cardGO = bestTrap;
                    var cardData = cd.CurrentCardData;
                    int cardInstanceID = cd.GetInstanceID();
                    
                    actions.Add(new AIAction {
                        Score = 30f,
                        Description = $"Baixar {cd.CurrentCardData.name}",
                        Execute = () => {
                            bool success = GameManager.Instance.PlaySpellTrap(cardGO, cardData, true); // true = set
                            if (success)
                                usedCardsThisTurn.Add(cardInstanceID);
                        }
                    });
                }
            }
        }

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
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
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
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) boardValue += Mathf.Max(cd.currentAtk, cd.currentDef);
            }
        }
        foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (z.childCount > 0)
            {
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) boardValue -= Mathf.Max(cd.currentAtk, cd.currentDef);
            }
        }
        Debug.Log($"[AI] Board Value: {boardValue}");

        // Ajusta a personalidade dinamicamente dependendo da campanha
        if (GameManager.Instance.currentOpponent != null)
        {
            string diff = GameManager.Instance.currentOpponent.difficulty != null ? GameManager.Instance.currentOpponent.difficulty.ToLower() : "";
            if (diff.Contains("aggressive")) currentPersonality = AIPersonality.Aggressive;
            else if (diff.Contains("defensive")) currentPersonality = AIPersonality.Defensive;
        }
    }

    // Reúne todas as avaliações
    private List<AIAction> EvaluateAllPossibleActions()
    {
        List<AIAction> actions = new List<AIAction>();

        actions.AddRange(EvaluateSummonActions());
        actions.AddRange(EvaluateSpellActions());
        actions.AddRange(EvaluateSetActions());
        actions.AddRange(EvaluateFieldMonsterActions());
        // Futuramente: actions.AddRange(EvaluatePositionChangeActions());

        return actions;
    }

    #region Avaliação de Ações

    private List<AIAction> EvaluateSummonActions()
    {
        var actions = new List<AIAction>();
        if (GameManager.Instance.normalSummonsThisTurnOpponent > 0) return actions;

        var hand = GameManager.Instance.opponentHand;
        var monstersInHand = hand.Select(go => go.GetComponent<CardDisplay>()).Where(cd => cd != null && cd.CurrentCardData != null && cd.CurrentCardData.type.Contains("Monster")).ToList();
        
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
            if (playerStrongestATK == 0) attackScore += 500; // Bônus por campo aberto
            if (tributesNeeded > 0) attackScore -= tributeCost; // Subtrai o custo inteligente
            
            // Na MP2, invocar em ataque é menos útil a menos que seja uma parede muito forte
            if (PhaseManager.Instance.currentPhase == GamePhase.Main2 && monster.CurrentCardData.atk < playerStrongestATK) 
                attackScore -= 1000;
            
            actions.Add(new AIAction {
                Score = attackScore,
                Description = $"Invocar {monster.CurrentCardData.name} em Ataque.",
                Execute = () => GameManager.Instance.TrySummonMonster(monster.gameObject, monster.CurrentCardData, false)
            });

            // Avalia invocar em Defesa (Set)
            float defenseScore = baseScore + monster.CurrentCardData.def - playerStrongestATK;
            if (monster.CurrentCardData.def > 2000) defenseScore += 500; // Bônus por ser uma boa parede
            if (tributesNeeded > 0) defenseScore -= tributeCost;

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
            
            actions.Add(new AIAction {
                Score = defenseScore,
                Description = $"Baixar (Set) {monster.CurrentCardData.name} em Defesa.",
                Execute = () => GameManager.Instance.TrySummonMonster(monster.gameObject, monster.CurrentCardData, true)
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
                    score += opponentCards * 600;
                    if (opponentCards == 0) score -= 1000; // Não queimar recursos à toa
                }
                if ((cat & 0x200) != 0) // CATEGORY_SPECIAL_SUMMON
                {
                    score += 1000; // Reviver/Summon é sempre bom
                }
                if ((cat & 0x10000) != 0) // CATEGORY_DRAW
                {
                    if (GameManager.Instance.GetOpponentMainDeck().Count <= 3) score -= 5000; // Previne suicídio por Deck Out
                    else score += 1500;
                }
                if ((cat & 0x8) != 0) // CATEGORY_TOHAND
                {
                    score += 500;
                }
                if ((cat & 0x80000) != 0) // CATEGORY_DAMAGE
                {
                    score += 600;
                }
                if ((cat & 0x100000) != 0) // CATEGORY_RECOVER
                {
                    score += 300;
                }

                description = $"Ativar Magia {cd.CurrentCardData.name} [Categorias LUA avaliadas]";
                execution = () => CardEffectManager.Instance.ExecuteCardEffect(cd);

                // Fallback para tipos persistentes não cobertos bem por categorias simples
                if (cd.CurrentCardData.property == "Equip")
                {
                    if (cd.CurrentCardData.property == "Equip")
                    {
                        // Regra de Ouro: Tall vs Wide
                        var myMonsters = GetMyMonstersOnField().Where(m => !m.isFlipped).OrderByDescending(m => m.currentAtk).ToList();
                        CardDisplay bestTargetToEquip = null;
                        
                        int playerMaxAtk = GetPlayerStrongestAtk();
                        int myMaxAtk = myMonsters.Count > 0 ? myMonsters[0].currentAtk : 0;
                        
                        if (playerMaxAtk > myMaxAtk && myMonsters.Count > 0) {
                            bestTargetToEquip = myMonsters[0]; // Tall: Concentra no mais forte para tentar passar o boss inimigo
                        } else if (myMonsters.Count > 1) {
                            bestTargetToEquip = myMonsters[myMonsters.Count - 1]; // Wide: Espalha força no mais fraco para dividir riscos
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
                    score += 300;
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
                actions.Add(new AIAction { Score = score, Description = description, Execute = execution });
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
        
        int maxSafeSets = (currentPersonality == AIPersonality.Defensive) ? 3 : 2;
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
                        if ((cat & 0x1) != 0) score += 500; // Destrói
                        if ((cat & 0x10000000) != 0) score += 600; // Nega (CATEGORY_NEGATE)
                        if ((cat & 0x200) != 0) score += 400; // Special Summon
                    }
                    
                    actions.Add(new AIAction {
                        Score = score,
                        Description = $"Baixar (Set) Armadilha {cd.CurrentCardData.name}.",
                        Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true)
                    });
                }
                else if (cd.CurrentCardData.type.Contains("Spell"))
                {
                    // Baixa Spells Rápidas (Quick-Play) na MP2 para usar como defesa no turno do inimigo
                    if (cd.CurrentCardData.property == "Quick-Play" && PhaseManager.Instance.currentPhase == GamePhase.Main2)
                    {
                        actions.Add(new AIAction {
                            Score = 400,
                            Description = $"Baixar (Set) Quick-Play Spell {cd.CurrentCardData.name}.",
                            Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true)
                        });
                    }
                    // Blefe (Bluff) se estivermos perdendo terreno e quisermos assustar o jogador
                    else if (fearScore < 1 && PhaseManager.Instance.currentPhase == GamePhase.Main2 && boardValue < -1000)
                    {
                        actions.Add(new AIAction {
                            Score = 150, // Melhor setar como Blefe do que tomar OTK sem dar medo
                            Description = $"Baixar (Set) Magia de Blefe {cd.CurrentCardData.name}.",
                            Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true)
                        });
                    }
                }
            }
        }
        // Pega apenas a melhor armadilha para baixar, para não encher o campo à toa
        return actions.OrderByDescending(a => a.Score).Take(1).ToList();
    }

    #endregion

    #region Lógica Matemática Avançada (Heurísticas)

    #endregion

    #region Lógica de Batalha (Melhorada)

    IEnumerator ExecuteBattlePhaseLogic()
    {
        List<CardDisplay> myMonsters = GetMyMonstersOnField().OrderByDescending(m => m.currentAtk).ToList();

        foreach (var attacker in myMonsters)
        {
            if (attacker == null || attacker.position == CardDisplay.BattlePosition.Defense || usedCardsThisTurn.Contains(attacker.GetInstanceID())) continue;

            // Avalia o melhor alvo para este atacante
            CardDisplay bestTarget = FindBestTarget(attacker);
            bool didAttack = false;

            if (bestTarget != null) // Encontrou um alvo vantajoso
            {
                Debug.Log($"AI: {attacker.CurrentCardData.name} ataca {bestTarget.CurrentCardData.name}!");
                if (CardEffectManager.Instance != null) {
                    CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(attacker);
                    CardEffectManager.Instance.luaDuel.currentAttackTarget = new LuaCard(bestTarget);
                    var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
                    yield return StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, 
                        CardEffectManager.Instance.luaDuel.currentAttacker, 
                        CardEffectManager.Instance.luaDuel.currentAttackTarget));
                    CardEffectManager.Instance.luaDuel.currentAttacker = null;
                }
                didAttack = true;
            }
            else if (GetPlayerMonsterCount() == 0) // Campo aberto
            {
                Debug.Log($"AI: {attacker.CurrentCardData.name} ataca diretamente!");
                if (CardEffectManager.Instance != null) {
                    CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(attacker);
                    CardEffectManager.Instance.luaDuel.currentAttackTarget = null;
                    var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
                    yield return StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, 
                        CardEffectManager.Instance.luaDuel.currentAttacker, 
                        null));
                    CardEffectManager.Instance.luaDuel.currentAttacker = null;
                }
                didAttack = true;
            }
            else
            {
                Debug.Log($"AI: {attacker.CurrentCardData.name} não encontrou um alvo vantajoso. Não vai atacar.");
            }

            if (didAttack)
            {
                yield return new WaitForSeconds(actionDelay + 1.0f);
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

        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var defender = zone.GetChild(0).GetComponent<CardDisplay>();
                if (defender == null || defender.CurrentCardData == null) continue;

                float score = 0;

                // --- AVALIAÇÃO DE ALVOS DE ALTO VALOR (Floodgates) ---
                if (!defender.isFlipped)
                {
                    if (defender.CurrentCardData.type.Contains("Effect")) score += 300; // Prioriza matar efeito
                }

                // --- SCOUTING (Lidar com Face-Down) ---
                if (defender.isFlipped) // Monstro Setado (Face-down)
                {
                    if (isBossAttacking) {
                        // REGRA DE OURO (Scouting): Não bater com o Boss se houver iscas.
                        // PORÉM, forçamos o ataque se: 1) É o único monstro da IA; 2) Vantagem de campo clara; 3) Oponente sem Traps (Fear = 0); 4) Personalidade Agressiva
                        if (myMonsters.Count == 1 || boardValue >= 1500 || fearScore == 0 || currentPersonality == AIPersonality.Aggressive) {
                            score += 200; // O risco é aceitável, força o ataque para testar o terreno
                        } else {
                            score -= 5000; // Recua e espera um lacaio para testar (Evita Man-Eater Bug)
                        }
                    } else {
                        score += 500; // Lacaios ganham bônus massivo para atacar face-downs e revelar armadilhas
                    }
                }
                else if (defender.position == CardDisplay.BattlePosition.Attack)
                {
                    if (attacker.currentAtk > defender.currentAtk)
                    {
                        // Prioriza destruir o monstro mais forte que consegue vencer
                        score += 100 + defender.currentAtk; 
                    }
                    else
                    {
                        // O SUICIDE CRASH (Bater pra Morrer Taticamente)
                        string[] floaters = { "1587", "2097", "1307", "1639", "0680", "1279", "2004", "0753", "1419", "0749" }; // Sangan, Witch, Tomate, etc
                        bool isFloater = floaters.Contains(attacker.CurrentCardData.id);
                        int damageToTake = defender.currentAtk - attacker.currentAtk;
                        
                        // Só se suicida se não for tomar muito dano e a vida permitir
                        if (isFloater && (GameManager.Instance.opponentLP - damageToTake > panicThreshold) && damageToTake <= 1500)
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

    public CardDisplay ChooseBestResponse(List<CardDisplay> validResponses, CardEffectManager.ChainLink triggerLink)
    {
        if (validResponses == null || validResponses.Count == 0) return null;

        var scoredResponses = validResponses.Select(card => {
            float score = 100; 

            // INTEGRAÇÃO LUA: Verifica se pode ativar a Armadilha/Quick-Play em resposta (Pagar custo, Alvo válido)
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(card);
            LuaEffect quickEffect = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0080); // ACTIVATE / TRIGGER_O
            
            if (quickEffect != null && !CardEffectManager.Instance.CanActivateEffect(lc, quickEffect, 1, triggerLink?.card))
            {
                return new { Card = card, Score = -9999f }; // Cortado na raiz.
            }

            if (triggerLink != null && triggerLink.card != null)
            {
                int cat = quickEffect != null ? quickEffect.category : 0;
                int eventCode = triggerLink.effect != null ? triggerLink.effect.code : 0;
                CardDisplay triggerCard = triggerLink.card.unityCard;

                if (eventCode == 1102) // EVENT_ATTACK_ANNOUNCE
                {
                    var attacker = CardEffectManager.Instance.luaDuel.currentAttacker?.unityCard;
                    var target = CardEffectManager.Instance.luaDuel.currentAttackTarget?.unityCard;
                    
                    int attackerAtk = attacker != null ? attacker.currentAtk : 0;
                    int targetPower = 0;
                    if (target != null) {
                        targetPower = target.position == CardDisplay.BattlePosition.Attack ? target.currentAtk : target.currentDef;
                    }

                    // Se a IA for o alvo e for ganhar a batalha, ela NÃO gasta armadilha (guarda recurso)
                    if (target != null && targetPower > attackerAtk && !target.isFlipped) {
                        score -= 5000;
                    }
                    else if ((cat & 0x1) != 0 || (cat & 0x8) != 0) // DESTROY or TOHAND (Mirror Force, Sakuretsu, Dimensional Prison)
                    {
                        if (attackerAtk >= 1500 || target == null) {
                            score += attackerAtk; // Usa remoção em ataques fortes ou ataques diretos ao HP
                        }
                    }
                    else {
                        score -= 500; // Guarda para ameaças piores
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
                                    var m = zone.GetChild(0).GetComponent<CardDisplay>();
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
                        if (summonedAtk >= 1500) score += summonedAtk;
                        else if (isLifeOrDeath) score += 1000; 
                        else if (preventingTribute) score += 500; 
                        else if (protectingTributeFodder) score += 800;
                        else score -= 5000;
                    }
                }
                else // Any other chain link (CardActivation)
                {
                    if ((cat & 0x10000000) != 0) // CATEGORY_NEGATE (Solemn Judgment, Magic Jammer, Dark Bribe)
                    {
                        score += 1500;
                    }
                }
            }
            
            return new { Card = card, Score = score };
        }).OrderByDescending(x => x.Score).ToList();

        var best = scoredResponses.FirstOrDefault();
        if (best != null && best.Score > 0)
        {
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
            
            if (!isMine && isField && c.GetAttack() > 0) score += 10000 + c.GetAttack(); // Matar o monstro mais forte do player
            else if (!isMine && c.IsSpellTrap()) score += 8000; // Destruir S/T do player
            else if (isMine && isGrave) score += 5000 + c.GetAttack(); // Reviver o próprio monstro mais forte do GY
            else if (isMine && isField) 
            {
                bool isEquipOrBuff = false;
                if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentActivatingEffect != null)
                {
                    var eff = CardEffectManager.Instance.luaDuel.currentActivatingEffect;
                    if (eff.owner != null && eff.owner.unityData != null && eff.owner.unityData.property == "Equip") isEquipOrBuff = true;
                    // CATEGORY_ATKCHANGE (0x800) ou CATEGORY_EQUIP (0x40000)
                    if ((eff.category & 0x800) != 0 || (eff.category & 0x40000) != 0) isEquipOrBuff = true;
                }
                
                if (isEquipOrBuff) score += c.GetAttack(); // Queremos buffar o monstro mais forte
                else score -= c.GetAttack(); // Se for custo/tributo, escolhe o lacaio mais fraco
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
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
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
        return monsters.Any(m => m.position == CardDisplay.BattlePosition.Attack && !usedCardsThisTurn.Contains(m.GetInstanceID()));
    }

    int GetPlayerStrongestAtk()
    {
        int maxAtk = 0;
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
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

    List<CardDisplay> GetMyMonstersOnField()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        foreach (var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
        return list;
    }

    #endregion
}
