using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OpponentAI : MonoBehaviour
{
    public static OpponentAI Instance;

    [Header("Configuração da IA")]
    [Tooltip("Tempo em segundos que a IA espera entre ações para simular pensamento.")]
    public float actionDelay = 1.2f;
    [Tooltip("A pontuação mínima que uma ação precisa ter para ser considerada.")]
    public float actionScoreThreshold = 10f;
    public bool isThinking = false;

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

    public void StartAITurn()
    {
        if (isThinking) return;
        StartCoroutine(AITurnRoutine());
    }

    IEnumerator AITurnRoutine()
    {
        isThinking = true;
        Debug.Log("AI: --- INÍCIO DO TURNO ---");

        yield return new WaitForSeconds(actionDelay);

        PhaseManager.Instance.ChangePhase(GamePhase.Standby);
        yield return new WaitForSeconds(actionDelay);

        // --- MAIN PHASE 1 ---
        PhaseManager.Instance.ChangePhase(GamePhase.Main1);
        yield return StartCoroutine(ExecuteMainPhaseLogic());

        // --- BATTLE PHASE ---
        if (CanEnterBattlePhase() && HasAttackCapableMonsters())
        {
            PhaseManager.Instance.ChangePhase(GamePhase.Battle);
            yield return new WaitForSeconds(actionDelay);
            yield return StartCoroutine(ExecuteBattlePhaseLogic());
        }

        // --- MAIN PHASE 2 ---
        PhaseManager.Instance.ChangePhase(GamePhase.Main2);
        yield return StartCoroutine(ExecuteMainPhaseLogic()); // Reavalia jogadas na Main Phase 2

        // --- END PHASE ---
        PhaseManager.Instance.ChangePhase(GamePhase.End);
        yield return new WaitForSeconds(actionDelay);

        Debug.Log("AI: --- FIM DO TURNO ---");
        GameManager.Instance.SwitchTurn();
        isThinking = false;
    }

    // O "cérebro" principal da IA durante as Main Phases
    IEnumerator ExecuteMainPhaseLogic()
    {
        while (true)
        {
            // 1. Avalia todas as jogadas possíveis
            List<AIAction> possibleActions = EvaluateAllPossibleActions();

            // 2. Se não há jogadas, encerra a fase
            if (possibleActions.Count == 0)
            {
                Debug.Log("AI: Nenhuma ação possível encontrada.");
                break;
            }

            // 3. Ordena as jogadas pela maior pontuação
            possibleActions.Sort((a, b) => b.Score.CompareTo(a.Score));
            AIAction bestAction = possibleActions[0];

            // 4. Se a melhor jogada não for boa o suficiente, guarda recursos
            if (bestAction.Score < actionScoreThreshold)
            {
                Debug.Log($"AI: Melhor ação ({bestAction.Description}) tem pontuação baixa ({bestAction.Score}). Guardando recursos.");
                break;
            }

            // 5. Executa a melhor jogada
            Debug.Log($"AI AÇÃO (Score: {bestAction.Score}): {bestAction.Description}");
            bestAction.Execute();
            
            // Espera a ação ser processada visualmente
            yield return new WaitForSeconds(actionDelay);
        }
    }

    // Reúne todas as avaliações
    private List<AIAction> EvaluateAllPossibleActions()
    {
        List<AIAction> actions = new List<AIAction>();

        actions.AddRange(EvaluateSummonActions());
        actions.AddRange(EvaluateSpellActions());
        actions.AddRange(EvaluateSetActions());
        // Futuramente: actions.AddRange(EvaluatePositionChangeActions());

        return actions;
    }

    #region Avaliação de Ações

    private List<AIAction> EvaluateSummonActions()
    {
        var actions = new List<AIAction>();
        if (SummonManager.Instance.hasPerformedNormalSummon) return actions;

        var hand = GameManager.Instance.opponentHand;
        var monstersInHand = hand.Select(go => go.GetComponent<CardDisplay>()).Where(cd => cd != null && cd.CurrentCardData.type.Contains("Monster")).ToList();
        
        int myMonsterCount = GetMyMonstersOnField().Count;
        int playerStrongestATK = GetPlayerStrongestAtk();

        foreach (var monster in monstersInHand)
        {
            int tributesNeeded = SummonManager.Instance.GetRequiredTributes(monster.CurrentCardData.level);
            if (myMonsterCount < tributesNeeded) continue;

            // Avalia invocar em Ataque
            float attackScore = monster.CurrentCardData.atk - playerStrongestATK;
            if (playerStrongestATK == 0) attackScore += 500; // Bônus por campo aberto
            if (tributesNeeded > 0) attackScore -= GetTributeCost(); // Penalidade por tributar
            
            actions.Add(new AIAction {
                Score = attackScore,
                Description = $"Invocar {monster.CurrentCardData.name} em Ataque.",
                Execute = () => GameManager.Instance.TrySummonMonster(monster.gameObject, monster.CurrentCardData, false)
            });

            // Avalia invocar em Defesa (Set)
            float defenseScore = monster.CurrentCardData.def - playerStrongestATK;
            if (monster.CurrentCardData.def > 2000) defenseScore += 500; // Bônus por ser uma boa parede
            if (tributesNeeded > 0) defenseScore -= GetTributeCost();
            
            actions.Add(new AIAction {
                Score = defenseScore,
                Description = $"Baixar (Set) {monster.CurrentCardData.name} em Defesa.",
                Execute = () => GameManager.Instance.TrySummonMonster(monster.gameObject, monster.CurrentCardData, true)
            });
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
            if (cd == null || !cd.CurrentCardData.type.Contains("Spell")) continue;

            float score = 0;
            string description = "";
            System.Action execution = null;

            // Lógica específica por carta
            switch (cd.CurrentCardData.id)
            {
                case "1268": // Monster Reborn
                    var bestTarget = GetBestGraveyardMonster();
                    if (bestTarget != null)
                    {
                        score = bestTarget.atk + 1000; // Pontuação alta por trazer um monstro forte de graça
                        description = $"Ativar Monster Reborn para reviver {bestTarget.name} ({bestTarget.atk} ATK).";
                        execution = () => CardEffectManager.Instance.ExecuteCardEffect(cd); // O efeito em si pedirá o alvo
                    }
                    break;
                
                case "1480": // Raigeki
                case "0414": // Dark Hole
                    int opponentMonsters = GetPlayerMonsterCount();
                    int myMonsters = GetMyMonstersOnField().Count;
                    score = (opponentMonsters * 1000) - (myMonsters * 1200); // Penaliza mais destruir os próprios
                    if (opponentMonsters > 0)
                    {
                        description = $"Ativar {cd.CurrentCardData.name} para destruir {opponentMonsters} monstro(s).";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;
                
                case "1447": // Pot of Greed
                    score = 2500; // Comprar cartas é quase sempre a melhor jogada
                    description = "Ativar Pot of Greed para comprar 2 cartas.";
                    execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    break;

                case "0287": // Change of Heart
                    var strongestOpponent = GetStrongestPlayerMonster();
                    if (strongestOpponent != null)
                    {
                        score = strongestOpponent.currentAtk + 500;
                        description = $"Ativar Change of Heart para roubar {strongestOpponent.CurrentCardData.name}.";
                        execution = () => CardEffectManager.Instance.ExecuteCardEffect(cd);
                    }
                    break;
                
                default: // Lógica genérica para Equipamentos e Campos
                    if (cd.CurrentCardData.property == "Equip")
                    {
                        // Encontra o melhor alvo para o equipamento
                        var bestTargetToEquip = GetMyMonstersOnField().OrderByDescending(m => m.currentAtk).FirstOrDefault();
                        if (bestTargetToEquip != null)
                        {
                            score = 300; // Base score for an equip
                            description = $"Equipar {cd.CurrentCardData.name} em {bestTargetToEquip.CurrentCardData.name}.";
                            execution = () => CardEffectManager.Instance.ExecuteCardEffect(cd);
                        }
                    }
                    else if (cd.CurrentCardData.property == "Field")
                    {
                        score = 200; // Ativar campo é geralmente bom
                        description = $"Ativar Campo {cd.CurrentCardData.name}.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;
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
        var hand = GameManager.Instance.opponentHand;
        int occupiedZones = 0;
        foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if(z.childCount > 0) occupiedZones++;
        if (occupiedZones >= 5) return actions;

        foreach (var go in hand)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd != null && cd.CurrentCardData.type.Contains("Trap"))
            {
                float score = 100; // Pontuação base para baixar uma armadilha
                if (cd.CurrentCardData.id == "1251") score = 500; // Mirror Force
                if (cd.CurrentCardData.id == "1962") score = 300; // Trap Hole
                
                actions.Add(new AIAction {
                    Score = score,
                    Description = $"Baixar (Set) {cd.CurrentCardData.name}.",
                    Execute = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, true)
                });
            }
        }
        // Pega apenas a melhor armadilha para baixar, para não encher o campo à toa
        return actions.OrderByDescending(a => a.Score).Take(1).ToList();
    }

    #endregion

    #region Lógica de Batalha (Melhorada)

    IEnumerator ExecuteBattlePhaseLogic()
    {
        List<CardDisplay> myMonsters = GetMyMonstersOnField().OrderByDescending(m => m.currentAtk).ToList();

        foreach (var attacker in myMonsters)
        {
            if (attacker == null || attacker.position == CardDisplay.BattlePosition.Defense || attacker.hasAttackedThisTurn) continue;

            // Avalia o melhor alvo para este atacante
            CardDisplay bestTarget = FindBestTarget(attacker);
            bool didAttack = false;

            // Executa a batalha dentro de um bloco try-catch para segurança
            try
            {
                if (bestTarget != null) // Encontrou um alvo vantajoso
                {
                    Debug.Log($"AI: {attacker.CurrentCardData.name} ataca {bestTarget.CurrentCardData.name}!");
                    BattleManager.Instance.currentAttacker = attacker;
                    BattleManager.Instance.PerformBattle(attacker, bestTarget);
                    didAttack = true;
                }
                else if (GetPlayerMonsterCount() == 0) // Campo aberto
                {
                    Debug.Log($"AI: {attacker.CurrentCardData.name} ataca diretamente!");
                    BattleManager.Instance.currentAttacker = attacker;
                    BattleManager.Instance.PerformDirectAttack(attacker);
                    didAttack = true;
                }
                else
                {
                    Debug.Log($"AI: {attacker.CurrentCardData.name} não encontrou um alvo vantajoso. Não vai atacar.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AI Error during battle: {e.Message}\n{e.StackTrace}");
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
        float bestScore = -1000; // Inicia com score negativo

        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var defender = zone.GetChild(0).GetComponent<CardDisplay>();
                if (defender == null) continue;

                float score = 0;
                if (defender.position == CardDisplay.BattlePosition.Attack)
                {
                    if (attacker.currentAtk > defender.currentAtk)
                    {
                        // Prioriza destruir o monstro mais forte que consegue vencer
                        score = 100 + defender.currentAtk; 
                    }
                    else
                    {
                        // Atacar um monstro mais forte é uma péssima ideia
                        score = -1000 - (defender.currentAtk - attacker.currentAtk);
                    }
                }
                else // Defesa
                {
                    if (attacker.currentAtk > defender.currentDef)
                    {
                        // Bom para limpar o campo, mas não causa dano. Prioridade menor.
                        score = 50 + defender.currentAtk; 
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

    #endregion

    #region Helpers de Análise de Campo

    private float GetTributeCost()
    {
        // Lógica simples: Pega os monstros mais fracos como custo
        var myMonsters = GetMyMonstersOnField();
        if (myMonsters.Count == 0) return 9999; // Custo infinito se não tiver nada
        myMonsters.Sort((a, b) => a.currentAtk.CompareTo(b.currentAtk));
        return myMonsters.First().currentAtk;
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
        return monsters.Any(m => m.position == CardDisplay.BattlePosition.Attack && !m.hasAttackedThisTurn);
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

    List<CardDisplay> GetMyMonstersOnField()
    {
        List<CardDisplay> list = new List<CardDisplay>();
        foreach (var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount > 0)
                list.Add(zone.GetChild(0).GetComponent<CardDisplay>());
        }
        return list;
    }

    #endregion
}
