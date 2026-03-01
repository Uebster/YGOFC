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
        if (card.type.Contains("Spell"))
        {
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
    public bool CheckChainResponse(ChainManager.ChainLink lastLink)
    {
        // Lógica futura: Verificar Trap Cards ou Quick-Play Spells que podem responder
        // Ex: Se o oponente ativou algo, verificar minhas armadilhas
        
        return false;
    }

    // --- LÓGICA DE SELEÇÃO DE ALVO ---

    public void StartTargetSelection(System.Predicate<CardDisplay> filter, System.Action<CardDisplay> callback)
    {
        isSelectingTarget = true;
        targetFilter = filter;
        onTargetSelected = callback;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowConfirmation("Selecione um alvo no campo.", null, CancelTargetSelection);
        
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