using UnityEngine;
using System.Collections.Generic;

public enum SpellTrapTrigger { Attack, Summon, PhaseChange, Activation }

public class SpellTrapManager : MonoBehaviour
{
    public static SpellTrapManager Instance;

    [Header("Regras Excepcionais")]
    public bool canActivateTrapsFromHand = false; // Ex: Makyura the Destructor
    public bool canActivateSpellsInOpponentTurn = false; // Quick-Play Spells (geralmente sim, mas depende da fase/chain)

    [Header("Controle de Draw Extra")]
    public int extraDrawsPerTurn = 0; // Ex: Shard of Greed, etc.
    public bool skipDrawPhase = false; // Ex: Offerings to the Doomed

    [Header("Controle de Shuffle")]
    public bool needsShuffle = false;

    // --- SISTEMA DE SELEÇÃO DE ALVO (TARGETING) ---
    [HideInInspector] public bool isSelectingTarget = false;
    private System.Action<CardDisplay> onTargetSelected;
    private System.Predicate<CardDisplay> targetFilter;

    void Awake()
    {
        Instance = this;
    }

    public void ResetTurnStats()
    {
        extraDrawsPerTurn = 0;
        // skipDrawPhase geralmente persiste até ser consumido, então não reseta aqui cegamente
        CancelTargetSelection();
    }

    // Verifica se uma carta específica pode ser ativada (considerando exceções)
    public bool CanActivateCard(CardData card, bool isOwnerTurn)
    {
        // Jinzo (77585513): Traps não podem ser ativadas
        if (card.type.Contains("Trap") && GameManager.Instance.IsCardActiveOnField("77585513"))
        {
            Debug.Log("Ativação de Trap bloqueada por Jinzo!");
            return false;
        }

        // Royal Decree (1563): Nega todas as outras Traps
        if (card.type.Contains("Trap") && IsTrapNegationActive())
        {
            Debug.Log("Ativação de Trap bloqueada por Royal Decree!");
            return false;
        }

        // Imperial Order (0932) / Spell Canceller (1721) / Silent Swordsman LV7 (1647)
        if (card.type.Contains("Spell") && IsSpellNegationActive())
        {
            Debug.Log("Ativação de Magia bloqueada por efeito contínuo!");
            return false;
        }

        // Bloqueio Global de Traps (Forced Ceasefire / Invasion of Flames)
        if (card.type.Contains("Trap") && CardEffectManager.Instance != null && CardEffectManager.Instance.trapsBlockedThisTurn)
        {
            Debug.Log("Ativação de Trap bloqueada neste turno por efeito de carta!");
            return false;
        }

        if (card.type.Contains("Spell"))
        {
            // Verifica se há alvo válido para Equip Spell
            if (card.property == "Equip")
            {
                if (!HasValidTargetForEquip(card)) return false;
            }
            
            // 0956 - Invader of Darkness
            if (card.property == "Quick-Play" && GameManager.Instance != null && !isOwnerTurn)
            {
                bool oppHasInvader = false;
                Transform[] oppZones = isOwnerTurn ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
                foreach (var z in oppZones) {
                    if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.id == "0956" && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped)
                        oppHasInvader = true;
                }
                if (oppHasInvader) {
                    Debug.Log("Invader of Darkness: Oponente não pode ativar Quick-Play Spells.");
                    return false;
                }
            }

            // Regras básicas de Spell
            if (isOwnerTurn) return true;
            if (card.property == "Quick-Play") return true; // Quick-Play pode no turno do oponente se setada (regra geral)
            return canActivateSpellsInOpponentTurn;
        }
        else if (card.type.Contains("Trap"))
        {
            // Regras básicas de Trap
            if (canActivateTrapsFromHand) return true;
            // Traps precisam estar setadas por 1 turno (verificação feita no CardDisplay/GameManager geralmente)
            return !isOwnerTurn; // Normalmente ativa no turno do oponente ou em resposta
        }
        return false;
    }

    public bool IsSpellNegationActive()
    {
        if (GameManager.Instance == null) return false;
        if (GameManager.Instance.IsCardActiveOnField("0932")) return true; // Imperial Order
        if (GameManager.Instance.IsCardActiveOnField("1721")) return true; // Spell Canceller
        if (GameManager.Instance.IsCardActiveOnField("1647")) return true; // Silent Swordsman LV7
        return false;
    }

    public bool IsTrapNegationActive()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.IsCardActiveOnField("1563"); // Royal Decree
    }

    private bool HasValidTargetForEquip(CardData card)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return false;

        List<CardDisplay> allMonsters = new List<CardDisplay>();
        
        void Collect(Transform[] zones) {
            foreach(var z in zones) {
                if(z.childCount > 0) {
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if(cd != null && cd.isOnField) allMonsters.Add(cd);
                }
            }
        }
        
        Collect(GameManager.Instance.duelFieldUI.playerMonsterZones);
        Collect(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        if (allMonsters.Count == 0) return false;

        string desc = card.description.ToLower();
        string[] types = { "warrior", "dragon", "spellcaster", "fiend", "zombie", "machine", "aqua", "pyro", "rock", "winged beast", "fairy", "beast", "beast-warrior", "dinosaur", "thunder", "fish", "sea serpent", "reptile", "plant", "insect" };
        
        foreach (string type in types)
        {
            if (desc.Contains("equip only to a " + type) || desc.Contains("equip only to an " + type))
            {
                bool found = false;
                foreach(var m in allMonsters)
                {
                    if (!m.isFlipped && m.CurrentCardData.race.ToLower() == type) { found = true; break; }
                }
                if (!found) return false;
            }
        }
        
        // Se não encontrou restrição de tipo específica, verifica se há qualquer monstro face-up
        bool anyFaceUp = false;
        foreach(var m in allMonsters) if (!m.isFlipped) anyFaceUp = true;
        
        return anyFaceUp;
    }

    public void RegisterExtraDraw(int amount)
    {
        extraDrawsPerTurn += amount;
    }

    public void RegisterSkipDraw()
    {
        skipDrawPhase = true;
    }

    public void ConsumeSkipDraw()
    {
        skipDrawPhase = false;
    }

    public void RequestShuffle()
    {
        needsShuffle = true;
        // Aqui poderia chamar uma animação de shuffle no DeckDisplay
        Debug.Log("SpellTrapManager: Deck Shuffle solicitado.");
        needsShuffle = false; // Consumido
    }

    // Verifica se há armadilhas que podem ser ativadas em resposta a um evento
    public void CheckForTraps(SpellTrapTrigger trigger, CardDisplay source, CardDisplay target, System.Action onContinue)
    {
        // Lógica simplificada: Procura por qualquer armadilha setada no campo do oponente (ou jogador, dependendo da lógica)
        // Para este exemplo, vamos verificar se há alguma Trap setada no campo de quem está sendo atacado.
        
        CardDisplay trapToActivate = null;

        // Exemplo simples: Verifica zonas de spell do oponente (assumindo que o player está atacando)
        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            // Se for ataque do player, checa traps do oponente
            bool checkOpponent = source.isPlayerCard; 
            Transform[] zones = checkOpponent ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;

            foreach (Transform zone in zones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    // Verifica se é Trap e se está virada para baixo (Set)
                    if (cd != null && cd.CurrentCardData.type.Contains("Trap") && cd.isFlipped)
                    {
                        trapToActivate = cd;
                        break; // Encontrou uma (simplificação: pega a primeira)
                    }
                }
            }
        }
        
        if (trapToActivate != null)
        {
            UIManager.Instance.ShowConfirmation($"Ativar {trapToActivate.CurrentCardData.name}?", () => {
                // Sim: Ativa a carta
                GameManager.Instance.PlaySpellTrap(trapToActivate.gameObject, trapToActivate.CurrentCardData, false);
                // Nota: Não chamamos onContinue() aqui porque a ativação interrompe o ataque (ou inicia uma Chain)
            }, onContinue); // Não: Continua o ataque
        }
        else
        {
            onContinue?.Invoke();
        }
    }

    // Verifica se há cartas que podem ser encadeadas (Chained)
    public List<CardDisplay> GetValidResponses(ChainManager.ChainLink triggerLink, bool forPlayer)
    {
        List<CardDisplay> validResponses = new List<CardDisplay>();
        if (triggerLink == null) return validResponses;

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = forPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;

            foreach (Transform zone in zones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd == null || !cd.isFlipped) continue; // Só pode ativar cartas setadas (ou da mão se permitido)

                    int cardSpeed = ChainManager.Instance.GetSpellSpeed(cd.CurrentCardData);
                    if (cardSpeed < triggerLink.spellSpeed) continue; // Spell Speed baixo demais
                    if (triggerLink.spellSpeed == 3 && cardSpeed != 3) continue; // Apenas Counter Traps respondem a Counter Traps

                    // Verifica condições de ativação
                    switch (triggerLink.trigger)
                    {
                        case ChainManager.TriggerType.CardActivation:
                            // Jar Robber (0967) vs Pot of Greed (1447)
                            if (cd.CurrentCardData.id == "0967" && triggerLink.cardSource.CurrentCardData.id == "1447") {
                                validResponses.Add(cd);
                            }
                            // Gemini Imps (0740) & Gravekeeper's Watcher (0812) vs Discard Effects
                            string triggerId = triggerLink.cardSource.CurrentCardData.id;
                            if ((cd.CurrentCardData.id == "0740" || cd.CurrentCardData.id == "0812") && 
                                (triggerId == "0465" || triggerId == "0321" || triggerId == "0791" || triggerId == "0264" || triggerId == "0894")) {
                                validResponses.Add(cd);
                            }

                            // Magic Jammer (1123) vs Spell
                            if (cd.CurrentCardData.id == "1123" && triggerLink.cardSource.CurrentCardData.type.Contains("Spell"))
                            {
                                validResponses.Add(cd);
                            }
                            // Seven Tools of the Bandit (1620) vs Trap
                            if (cd.CurrentCardData.id == "1620" && triggerLink.cardSource.CurrentCardData.type.Contains("Trap"))
                            {
                                validResponses.Add(cd);
                            }
                            break;

                        case ChainManager.TriggerType.Summon:
                            // Trap Hole (1962) vs Normal/Flip Summon 1000+ ATK
                            if (cd.CurrentCardData.id == "1962" && triggerLink.cardSource.currentAtk >= 1000)
                            {
                                // Assumimos que o trigger de Summon é para Normal/Flip
                                validResponses.Add(cd);
                            }
                            break;

                        case ChainManager.TriggerType.Attack:
                            // Sakuretsu Armor (1581) vs Attack
                            if (cd.CurrentCardData.id == "1581")
                            {
                                validResponses.Add(cd);
                            }
                            break;
                    }
                }
            }
        }
        return validResponses;
    }

    // --- LÓGICA DE SELEÇÃO DE ALVO ---

    public void StartTargetSelection(System.Predicate<CardDisplay> filter, System.Action<CardDisplay> callback)
    {
        isSelectingTarget = true;
        targetFilter = filter;
        onTargetSelected = callback;

        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            // Tenta encontrar um alvo válido automaticamente
            List<CardDisplay> validTargets = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                // Coleta todos os candidatos possíveis
                List<Transform> allZones = new List<Transform>();
                allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
                allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);
                allZones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
                allZones.AddRange(GameManager.Instance.duelFieldUI.opponentSpellZones);
                
                foreach(var z in allZones)
                {
                    if (z.childCount > 0)
                    {
                        var cd = z.GetChild(0).GetComponent<CardDisplay>();
                        if (cd != null && filter(cd)) validTargets.Add(cd);
                    }
                }
            }

            if (validTargets.Count > 0)
            {
                // Escolhe um aleatório
                SelectTarget(validTargets[Random.Range(0, validTargets.Count)]);
            }
            else
            {
                CancelTargetSelection();
            }
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.confirmationModal != null)
        {
            UIManager.Instance.ShowConfirmation("Selecione um alvo no campo.", null, CancelTargetSelection);
        }
        else
        {
            Debug.LogError("SpellTrapManager: Não foi possível mostrar a UI de seleção de alvo. Cancelando.");
            CancelTargetSelection();
        }
        
        Debug.Log("SpellTrapManager: Iniciando seleção de alvo...");
    }

    public void SelectTarget(CardDisplay card)
    {
        if (!isSelectingTarget) return;

        if (targetFilter != null && !targetFilter(card))
        {
            Debug.Log("Alvo inválido para esta carta.");
            return;
        }

        // Alvo válido
        isSelectingTarget = false;
        onTargetSelected?.Invoke(card);
    }

    public void CancelTargetSelection()
    {
        isSelectingTarget = false;
        onTargetSelected = null;
        targetFilter = null;
    }
}