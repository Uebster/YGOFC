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

    void Awake()
    {
        Instance = this;
    }

    public void ResetTurnStats()
    {
        extraDrawsPerTurn = 0;
        // skipDrawPhase geralmente persiste até ser consumido, então não reseta aqui cegamente
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
    public bool CheckForTraps(SpellTrapTrigger trigger, CardDisplay source, CardDisplay target)
    {
        // Lógica simplificada: Verifica se o oponente tem traps setadas
        // Em um jogo real, verificaria cada carta para ver se a condição de ativação é atendida (ex: Mirror Force no ataque)
        
        // Exemplo: Se for ataque, pergunta ao jogador se quer ativar algo (se tiver)
        // UIManager.Instance.ShowConfirmation("Ativar Armadilha?", () => ActivateTrap(...));
        
        // Retorna true se uma interrupção ocorreu (chain iniciada)
        // Retorna false se o jogo deve prosseguir normalmente
        
        return false; 
    }
}