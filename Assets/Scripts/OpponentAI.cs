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

        EvaluateBoardState();

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

        EvaluateBoardState(); // Avalia uma última vez

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
        // Reavalia o campo sempre que entra na Main Phase
        EvaluateBoardState();

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
            string diff = GameManager.Instance.currentOpponent.difficulty.ToLower();
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
        if (SummonManager.Instance.hasPerformedNormalSummon) return actions;

        var hand = GameManager.Instance.opponentHand;
        var monstersInHand = hand.Select(go => go.GetComponent<CardDisplay>()).Where(cd => cd != null && cd.CurrentCardData.type.Contains("Monster")).ToList();
        
        int myMonsterCount = GetMyMonstersOnField().Count;
        int playerStrongestATK = GetPlayerStrongestAtk();
        int myStrongestATK = GetStrongestMyMonsterAtk();

        foreach (var monster in monstersInHand)
        {
            int tributesNeeded = SummonManager.Instance.GetRequiredTributes(monster.CurrentCardData.level);
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

            // Regra de Ocultação: Monstros setados na defesa devem preferencialmente ser baixados na MP2
            if (PhaseManager.Instance.currentPhase == GamePhase.Main2) 
            {
                defenseScore += 1000;
            }
            else 
            {
                // Na MP1, penaliza setar monstros (a menos que a IA esteja desesperada)
                defenseScore -= 600; 
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
            if (monster.hasUsedEffectThisTurn || monster.isFlipped) continue;

            string id = monster.CurrentCardData.id;

            if (id == "1513") // Relinquished
            {
                // Só absorve se ainda não tem equipamento
                if (CardEffectManager.Instance.GetEquippedCards(monster).Count == 0)
                {
                    var bestTarget = GetStrongestPlayerMonster();
                    if (bestTarget != null && !bestTarget.isFlipped)
                    {
                        actions.Add(new AIAction {
                            Score = bestTarget.currentAtk + 1000, // Alto valor por roubar o boss inimigo
                            Description = $"Ativar Relinquished para absorver {bestTarget.CurrentCardData.name}.",
                            Execute = () => CardEffectManager.Instance.ExecuteCardEffect(monster)
                        });
                    }
                }
            }
            else if (id == "0240") // Breaker the Magical Warrior
            {
                if (monster.spellCounters > 0 && fearScore > 0)
                {
                    actions.Add(new AIAction {
                        Score = 800 + (fearScore * 100), // Quanto mais S/T o jogador tem, mais ele quer quebrar
                        Description = "Ativar Breaker the Magical Warrior para destruir uma S/T.",
                        Execute = () => CardEffectManager.Instance.ExecuteCardEffect(monster)
                    });
                }
            }
            // Novos monstros de efeito (Exiled Force, Tribe-Infecting Virus) podem ser adicionados aqui.
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
                    int myMonstersCount = GetMyMonstersOnField().Count;
                    // Só usa Board Wipe se o jogador tiver vantagem clara de campo
                    score = (opponentMonsters * 1000) - (myMonstersCount * 1200); 
                    if (opponentMonsters > 0 && score > 0) // Evita trocas ruins (1 por 1)
                    {
                        description = $"Ativar {cd.CurrentCardData.name} para destruir {opponentMonsters} monstro(s).";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;
                
                case "1447": // Pot of Greed
                case "0791": // Graceful Charity
                    // Consciência de Deck Out! Não se matar puxando cartas.
                    if (GameManager.Instance.GetOpponentMainDeck().Count <= 3) {
                        score = -5000;
                        description = $"Ignorar {cd.CurrentCardData.name} (Risco de Deck Out).";
                        execution = null;
                    }
                    else {
                        score = 2500; 
                        description = $"Ativar {cd.CurrentCardData.name} para comprar cartas.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;

                case "0465": // Delinquent Duo
                case "0321": // Confiscation
                case "1863": // The Forceful Sentry
                    if (GameManager.Instance.GetPlayerHandData().Count > 0 && GameManager.Instance.opponentLP - 1000 > panicThreshold)
                    {
                        score = 2000;
                        description = $"Ativar {cd.CurrentCardData.name} para destruir a mão do jogador.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;

                case "0287": // Change of Heart
                case "1683": // Snatch Steal
                    var strongestOpponent = GetStrongestPlayerMonster();
                    if (strongestOpponent != null)
                    {
                        score = strongestOpponent.currentAtk + 500;
                        description = $"Ativar {cd.CurrentCardData.name} para roubar {strongestOpponent.CurrentCardData.name}.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;

                case "1453": // Premature Burial
                    var bestPrematureTarget = GetBestGraveyardMonster();
                    if (bestPrematureTarget != null && GameManager.Instance.opponentLP - 800 > panicThreshold)
                    {
                        score = bestPrematureTarget.atk + 800; // Bom valor de ressurreição
                        description = $"Ativar Premature Burial em {bestPrematureTarget.name}.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;

                case "0881": // Heavy Storm
                case "0872": // Harpie's Feather Duster
                    int oppSTCount = GameManager.Instance.duelFieldUI.playerSpellZones.Count(z => z.childCount > 0);
                    int mySTCount = GameManager.Instance.duelFieldUI.opponentSpellZones.Count(z => z.childCount > 0);
                    
                    if (cd.CurrentCardData.id == "0872") mySTCount = 0; // Duster não destrói os próprios

                    if (oppSTCount > mySTCount)
                    {
                        score = (oppSTCount * 800) - (mySTCount * 1000);
                        description = $"Ativar {cd.CurrentCardData.name} para varrer Magias/Armadilhas.";
                        execution = () => CardEffectManager.Instance.ExecuteCardEffect(cd);
                    }
                    break;

                case "1811": // Swords of Revealing Light
                    // Só usa as espadas se estiver perdendo a corrida de dano (Stall)
                    if (boardValue < -1000 || GameManager.Instance.opponentLP <= panicThreshold)
                    {
                        score = 800;
                        description = "Ativar Swords of Revealing Light para defesa.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;

                case "0757": // Giant Trunade
                    int oppCards = GameManager.Instance.duelFieldUI.playerSpellZones.Count(z => z.childCount > 0);
                    bool hasExpiringSwords = GameManager.Instance.duelFieldUI.opponentSpellZones.Cast<Transform>()
                        .Any(z => z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.id == "1811" && z.GetChild(0).GetComponent<CardDisplay>().turnCounter <= 1);
                    
                    if (hasExpiringSwords)
                    {
                        score = 3000; // Prioridade máxima: Salvar as próprias espadas para reciclar!
                        description = "Ativar Giant Trunade para reciclar Swords of Revealing Light!";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    else if (oppCards >= 2 && PhaseManager.Instance.currentPhase == GamePhase.Main1)
                    {
                        score = oppCards * 600;
                        description = "Ativar Giant Trunade para abrir caminho para o ataque.";
                        execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                    }
                    break;

                case "1318": // Mystical Space Typhoon
                    // Na MP1, só ativa se o Fear Score for altíssimo. Senão, guarda para Setar e usar na End Phase do jogador.
                    if (fearScore >= 2 && PhaseManager.Instance.currentPhase == GamePhase.Main1)
                    {
                        score = 900;
                        description = "Ativar MST para limpar o campo antes de atacar.";
                        execution = () => CardEffectManager.Instance.ExecuteCardEffect(cd); // Executará a lógica de seleção de alvo
                    }
                    break;
                
                default: // Lógica genérica para Equipamentos e Campos
                    if (cd.CurrentCardData.property == "Equip")
                    {
                        // Regra de Ouro: Tall vs Wide
                        var myMonsters = GetMyMonstersOnField().OrderByDescending(m => m.currentAtk).ToList();
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
                    else if (cd.CurrentCardData.property == "Field")
                    {
                        int netBalance = CalculateFieldSpellNetBalance(cd.CurrentCardData.id);
                        if (netBalance > 0)
                        {
                            score = 200 + (netBalance * 1.5f); // Quanto maior a vantagem de status, melhor
                            description = $"Ativar Campo {cd.CurrentCardData.name} (Balanço Líquido: +{netBalance} Status).";
                            execution = () => GameManager.Instance.PlaySpellTrap(go, cd.CurrentCardData, false);
                        }
                        else
                        {
                            // Tiro no pé (Ex: Ativar Umi quando o oponente tem mais monstros WATER)
                            // Debug.Log($"[AI] Ignorando {cd.CurrentCardData.name}, balanço negativo de {netBalance}.");
                        }
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
        
        // Regra de Overextension (Não setar tudo de uma vez para não perder para Heavy Storm)
        var hand = GameManager.Instance.opponentHand;
        int occupiedZones = 0;
        foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if(z.childCount > 0) occupiedZones++;
        
        int maxSafeSets = (currentPersonality == AIPersonality.Defensive) ? 3 : 2;
        if (occupiedZones >= maxSafeSets) return actions; // Previne tempestade

        foreach (var go in hand)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd != null && cd.CurrentCardData.type.Contains("Trap"))
            {
                float score = 100; // Pontuação base para baixar uma armadilha
                
                // Ocultação de Intenção: Armadilhas SÓ devem ser baixadas na MP2
                if (PhaseManager.Instance.currentPhase == GamePhase.Main2) 
                {
                    score += 800;
                }
                else {
                    score -= 500; // Desencoraja fortemente setar na MP1 para não tomar remoção de graça antes da hora
                }

                if (cd.CurrentCardData.id == "1251") score = 500; // Mirror Force
                if (cd.CurrentCardData.id == "1962") score = 300; // Trap Hole
                if (cd.CurrentCardData.id == "0259") score = 600; // Call of the Haunted (Prioridade alta para armar)
                
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

    #region Lógica Matemática Avançada (Heurísticas)

    // Calcula a diferença real de ATK/DEF se um Field Spell específico for ativado
    private int CalculateFieldSpellNetBalance(string fieldId)
    {
        int aiGain = 0;
        int playerGain = 0;

        // Define os buffs básicos dependendo do ID
        string buffRace = "", buffAttr = "", debuffRace = "", debuffAttr = "";
        int buffValue = 0, debuffValue = 0;

        switch(fieldId) {
            case "2015": case "0013": // Umi
                buffAttr = "Water"; buffValue = 200; debuffRace = "Machine"; debuffValue = -200; break;
            case "2125": // Yami
                buffRace = "Fiend"; buffValue = 200; debuffRace = "Fairy"; debuffValue = -200; break;
            case "1684": // Sogen
                buffRace = "Warrior"; buffValue = 200; break;
            case "0687": // Forest
                buffRace = "Insect"; buffValue = 200; break; // (Tem Plant e Beast tbm, simplificado)
            case "1104": // Luminous Spark
                buffAttr = "Light"; buffValue = 500; break;
            case "0715": // Gaia Power
                buffAttr = "Earth"; buffValue = 500; break;
            case "1261": // Molten Destruction
                buffAttr = "Fire"; buffValue = 500; break;
            case "1537": // Rising Air Current
                buffAttr = "Wind"; buffValue = 500; break;
        }

        void EvaluateMonsters(Transform[] zones, ref int scoreTracker) {
            foreach(var z in zones) {
                if(z.childCount > 0) {
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if(cd != null && !cd.isFlipped) {
                        string r = CardEffectManager.Instance.GetEffectiveRace(cd);
                        string a = CardEffectManager.Instance.GetEffectiveAttribute(cd);
                        if (r == buffRace || a == buffAttr) scoreTracker += buffValue;
                        if (r == debuffRace || a == debuffAttr) scoreTracker += debuffValue;
                        // Field Spells como Luminous Spark também tiram DEF, mas o que mais importa para IA bater é o ATK
                    }
                }
            }
        }

        EvaluateMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, ref aiGain);
        EvaluateMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, ref playerGain);

        return aiGain - playerGain; // Se for positivo, a IA se beneficia mais que o jogador
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
                if (defender == null) continue;

                float score = 0;

                // --- AVALIAÇÃO DE ALVOS DE ALTO VALOR (Floodgates) ---
                if (!defender.isFlipped)
                {
                    string id = defender.CurrentCardData.id;
                    if (id == "0975" || id == "Jinzo") score += 2000; // Jinzo
                    if (id == "1721" || id == "Spell Canceller") score += 2000;
                }

                // --- SCOUTING (Lidar com Face-Down) ---
                if (defender.isFlipped) // Monstro Setado (Face-down)
                {
                    if (isBossAttacking) {
                        score -= 5000; // NUNCA ataque um face-down com seu Boss Monster (Evita Man-Eater Bug)
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
            if (cd != null)
            {
                // Cartas que a IA quer MANTER têm Score ALTO. Cartas para DESCARTAR têm Score BAIXO.
                if (cd.CurrentCardData.id == "1268") score += 1000; // Monster Reborn
                if (cd.CurrentCardData.id == "1447") score += 1000; // Pot of Greed
                if (cd.CurrentCardData.id == "1480") score += 900;  // Raigeki

                bool hasRevive = hand.Exists(h => h.GetComponent<CardDisplay>()?.CurrentCardData.id == "1268");
                
                if (cd.CurrentCardData.type.Contains("Monster"))
                {
                    if (cd.CurrentCardData.level >= 5)
                    {
                        if (hasRevive) score -= 500; // Quer muito descartar para reviver depois
                        else score += cd.CurrentCardData.atk / 10f; // Mantém se for forte
                    }
                    else
                    {
                        score += 200 + (cd.CurrentCardData.atk / 10f); // Lacaios são úteis para baixar
                    }
                }
                else 
                {
                    score += 300; // Spells/Traps genéricas são boas
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
            if (cd != null)
            {
                if (cd.CurrentCardData.id == "1480" || cd.CurrentCardData.id == "0414") score += 2000;
                if (cd.CurrentCardData.id == "1268" || cd.CurrentCardData.id == "1453") score += 1500;
                if (cd.CurrentCardData.id == "1447" || cd.CurrentCardData.id == "0791" || cd.CurrentCardData.id == "0465") score += 1200;
                if (cd.CurrentCardData.id == "1251" || cd.CurrentCardData.id == "1955" || cd.CurrentCardData.id == "1533") score += 1800;
                
                if (cd.CurrentCardData.type.Contains("Monster"))
                {
                    if (cd.CurrentCardData.level <= 4) score += cd.CurrentCardData.atk;
                    else score += cd.CurrentCardData.atk * 0.5f;
                }
            }
            return new { Display = cd, Score = score };
        }).OrderByDescending(x => x.Score).ToList();

        return scoredCards.FirstOrDefault()?.Display;
    }

    public CardDisplay ChooseBestResponse(List<CardDisplay> validResponses, ChainManager.ChainLink trigger)
    {
        if (validResponses == null || validResponses.Count == 0) return null;

        var scoredResponses = validResponses.Select(card => {
            float score = 100; 
            string id = card.CurrentCardData.id;

            if (trigger != null && trigger.cardSource != null)
            {
                if (trigger.trigger == ChainManager.TriggerType.Attack)
                {
                    var attacker = BattleManager.Instance != null ? BattleManager.Instance.currentAttacker : null;
                    var target = BattleManager.Instance != null ? BattleManager.Instance.currentTarget : null;
                    
                    int attackerAtk = attacker != null ? attacker.currentAtk : 0;
                    int targetPower = 0;
                    if (target != null) {
                        targetPower = target.position == CardDisplay.BattlePosition.Attack ? target.currentAtk : target.currentDef;
                    }

                    // Se a IA for o alvo e for ganhar a batalha, ela NÃO gasta armadilha (guarda recurso)
                    if (target != null && targetPower > attackerAtk && !target.isFlipped) {
                        score -= 5000;
                    }
                    else if (attackerAtk >= 1500 || target == null) {
                        score += attackerAtk; // Usa remoção em ataques fortes ou ataques diretos ao HP
                    }
                    else {
                        score -= 500; // Guarda para ameaças piores
                    }
                }
                else if (trigger.trigger == ChainManager.TriggerType.Summon)
                {
                    int summonedAtk = trigger.cardSource.currentAtk;
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
                                    if (m != null && m != trigger.cardSource && !m.isFlipped && m.position == CardDisplay.BattlePosition.Attack) {
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

                    if (summonedAtk >= 1500) 
                    {
                        score += summonedAtk;
                    }
                    else if (isLifeOrDeath) {
                        score += 1000; // Vida ou morte: ativa o Trap Hole para sobreviver
                    }
                    else if (preventingTribute) {
                        score += 500; // Prevenção tática: destrói a isca fraca para evitar que vire um Boss Monster
                    }
                    else if (protectingTributeFodder) {
                        score += 800; // Proteção tática: salva o tributo para invocar um Boss na próxima rodada
                    }
                    else score -= 5000; // Se não for perigoso e não for tributo, ignora iscas
                }
                else if (trigger.trigger == ChainManager.TriggerType.CardActivation)
                {
                    string threatId = trigger.cardSource.CurrentCardData.id;
                    // Mágicas de Destruição Massiva merecem Counter Traps
                    if (threatId == "1480" || threatId == "0414" || threatId == "0881" || threatId == "0872") score += 2000;
                    else score += 500;
                }
            }

            // Verifica se a IA pode pagar os custos das armadilhas
            if (id == "1688" && GameManager.Instance.opponentLP <= 2000) score -= 5000; // Solemn Judgment
            if (id == "1123" && GameManager.Instance.GetOpponentHandData().Count == 0) score -= 5000; // Magic Jammer precisa de 1 descarte
            if (id == "1620" && GameManager.Instance.opponentLP <= 1000) score -= 5000; // Seven Tools of Bandit
            
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

    #region Helpers de Análise de Campo

    private float CalculateSmartTributeCost(int amount)
    {
        if (amount <= 0) return 0;
        var myMonsters = GetMyMonstersOnField();
        if (myMonsters.Count < amount) return 9999; // Custo infinito se não tem o suficiente
        
        // Ordena pela lógica de "Abdicação" (O que queremos perder primeiro?)
        myMonsters.Sort((a, b) => {
            // 1. Monstros roubados (dono original não é a IA) têm prioridade absoluta de abate
            bool aStolen = a.originalOwnerIsPlayer != a.isPlayerCard;
            bool bStolen = b.originalOwnerIsPlayer != b.isPlayerCard;
            if (aStolen && !bStolen) return -1;
            if (!aStolen && bStolen) return 1;

            // 2. Monstros travados por armadilhas (ex: Nightmare Wheel ou Mask of Accursed)
            bool aTrapped = a.cannotAttackThisTurn || a.cannotAttackDirectly; 
            bool bTrapped = b.cannotAttackThisTurn || b.cannotAttackDirectly;
            if (aTrapped && !bTrapped) return -1;
            if (!aTrapped && bTrapped) return 1;

            // 3. Menor ATK
            return a.currentAtk.CompareTo(b.currentAtk);
        });

        float cost = 0;
        for(int i=0; i<amount; i++) 
        {
            if (myMonsters[i].originalOwnerIsPlayer != myMonsters[i].isPlayerCard) cost -= 1000; // Desconto imenso por ser roubado
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
                list.Add(zone.GetChild(0).GetComponent<CardDisplay>());
        }
        return list;
    }

    #endregion
}
