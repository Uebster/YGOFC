using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

public class RitualManager : MonoBehaviour
{
    public static RitualManager Instance;

    public LuaGroup lastRitualTributes = null;

    void Awake()
    {
        Instance = this;
    }

    public LuaGroup GetRitualMaterial(int playerIndex, object nocheck)
    {
        LuaGroup monsters = new LuaGroup();
        if (CardEffectManager.Instance == null || CardEffectManager.Instance.luaDuel == null) return monsters;

        // Coleta todos os monstros da Mão e do Campo
        LuaGroup candidates = CardEffectManager.Instance.luaDuel.GetMatchingGroup(null, playerIndex, 0x02 | 0x04, 0, null);
        
        foreach (var c in candidates.cards) if (c.IsMonster()) monsters.AddCard(c);
        
        // Futuro: Verificar cartas no GY que possuem EFFECT_EXTRA_RITUAL_MATERIAL e adicionar à lista
        
        return monsters;
    }

    public IEnumerator ReleaseRitualMaterialRoutine(LuaGroup group)
    {
        lastRitualTributes = group;
        int ocgReason = 0x40 | 0x80 | 0x100000; // REASON_EFFECT | REASON_MATERIAL | REASON_RITUAL
        int count = 0;

        foreach (var c in group.cards)
        {
            c.currentReason = ocgReason;
            if (c.unityCard != null) {
                if (c.unityCard.isOnField) GameManager.Instance.TributeCard(c.unityCard, ocgReason);
                else {
                    GameManager.Instance.RemoveCardFromHand(c.unityData, c.unityCard.ownerPlayer);
                    GameManager.Instance.SendToGraveyard(c.unityData, c.unityCard.ownerPlayer, CardLocation.Hand, ocgReason);
                    if (c.unityCard.gameObject != null) Destroy(c.unityCard.gameObject);
                }
                count++;
            } else if (c.unityData != null) {
                bool wasPlayer = c.ownerPlayerIndex == 0;
                CardLocation loc = CardLocation.Unknown;
                if (GameManager.Instance.GetPlayerMainDeck().Contains(c.unityData) || GameManager.Instance.GetOpponentMainDeck().Contains(c.unityData)) loc = CardLocation.Deck;
                
                if (wasPlayer) { GameManager.Instance.GetPlayerMainDeck().Remove(c.unityData); }
                else { GameManager.Instance.GetOpponentMainDeck().Remove(c.unityData); }

                GameManager.Instance.SendToGraveyard(c.unityData, wasPlayer, loc, ocgReason);
                count++;
            }
        }

        if (count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            if (DuelFXManager.Instance != null && DuelFXManager.Instance.spellSound != null) 
                DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound); 
            
            yield return new WaitForSeconds(0.6f);
        }

        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    // Valida se os materiais de tributo são suficientes para o Monstro de Ritual.
    public bool ValidateRitual(CardData ritualSpell, CardData ritualMonster, List<CardData> selectedTributes)
    {
        if (ritualSpell == null || ritualMonster == null || !ritualMonster.type.Contains("Ritual") || selectedTributes == null)
        {
            return false;
        }

        // 1. Verifica se a Magia de Ritual corresponde ao Monstro de Ritual
        // Simplificação: A descrição da magia deve conter o nome do monstro.
        if (!ritualSpell.description.Contains(ritualMonster.name))
        {
            // Poderíamos ter uma exceção para rituais genéricos como "Contract with the Abyss"
            if (ritualSpell.id != "0325") // ID de Contract with the Abyss
            {
                 Debug.LogWarning($"A magia '{ritualSpell.name}' não é para '{ritualMonster.name}'.");
                 return false;
            }
        }

        // 2. Calcula o total de Níveis dos tributos
        int totalTributeLevels = 0;
        foreach (var tribute in selectedTributes)
        {
            totalTributeLevels += tribute.level;
        }

        // 3. Verifica se o Nível é suficiente
        if (totalTributeLevels < ritualMonster.level)
        {
            Debug.Log($"Nível de tributo insuficiente. Necessário: {ritualMonster.level}, Oferecido: {totalTributeLevels}");
            return false;
        }

        return true; // Todos os critérios foram atendidos
    }

    public List<CardData> GetPossibleRitualMonsters(CardData ritualSpell, List<CardData> hand)
    {
        List<CardData> possible = new List<CardData>();
        if (ritualSpell == null || hand == null) return possible;

        foreach (var card in hand)
        {
            if (card != null && card.type.Contains("Ritual") && card.type.Contains("Monster"))
            {
                if (!string.IsNullOrEmpty(ritualSpell.description) && ritualSpell.description.Contains(card.name) || ritualSpell.id == "0325")
                    possible.Add(card);
            }
        }
        return possible;
    }
}
