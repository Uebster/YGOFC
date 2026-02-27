using UnityEngine;
using System.Collections.Generic;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance;

    [Header("Limites de Turno")]
    public bool hasPerformedNormalSummon = false;

    [Header("Contadores de Special Summon (Player)")]
    public int specialSummonFromHandAtk = 0;
    public int specialSummonFromHandDef = 0;
    public int specialSummonFromDeckAtk = 0;
    public int specialSummonFromDeckDef = 0;
    public int specialSummonFromGYAtk = 0;
    public int specialSummonFromGYDef = 0;
    public int fusionSummonCount = 0;
    public int ritualSummonCount = 0;

    [Header("Contadores de Special Summon (Oponente)")]
    public int opponentSpecialSummonFromHand = 0;
    public int opponentSpecialSummonFromDeck = 0;
    public int opponentSpecialSummonFromGY = 0;
    public int opponentSpecialSummonFromField = 0; // Ex: Roubar monstro

    void Awake()
    {
        Instance = this;
    }

    public void ResetTurnStats()
    {
        hasPerformedNormalSummon = false;
        // Resetar contadores de special summon se necessário (geralmente não há limite por turno para special, mas podemos querer rastrear)
    }

    // Verifica se pode realizar Normal Summon/Set
    public bool CanNormalSummon()
    {
        if (GameManager.Instance != null && GameManager.Instance.devMode) return true;
        return !hasPerformedNormalSummon;
    }

    // Calcula tributos necessários baseado no nível
    public int GetRequiredTributes(int level)
    {
        if (level <= 4) return 0;
        if (level <= 6) return 1;
        return 2;
    }

    // Verifica se tem monstros suficientes para tributar
    public bool HasEnoughTributes(int requiredTributes, bool isPlayer)
    {
        if (requiredTributes <= 0) return true;
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return false;

        int count = 0;
        Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;

        foreach (Transform zone in zones)
        {
            if (zone.childCount > 0) count++;
        }

        return count >= requiredTributes;
    }

    // Realiza a invocação (lógica de regras)
    public bool PerformSummon(CardData card, bool isSet, bool isSpecial, bool isPlayer)
    {
        // Se for Normal Summon, verifica limite e tributos
        if (!isSpecial)
        {
            if (!CanNormalSummon())
            {
                Debug.LogWarning("Já realizou Normal Summon neste turno.");
                return false;
            }

            int tributesNeeded = GetRequiredTributes(card.level);
            if (!HasEnoughTributes(tributesNeeded, isPlayer))
            {
                Debug.LogWarning($"Tributos insuficientes! Precisa de {tributesNeeded}.");
                return false;
            }

            // Se precisar de tributo, a lógica de seleção de tributos deve ser chamada aqui (UI).
            // Por enquanto, vamos assumir que o jogador "paga" automaticamente ou que a UI de seleção virá depois.
            // TODO: Implementar UI de seleção de tributos.
            
            if (isPlayer) hasPerformedNormalSummon = true;
        }
        else
        {
            // Lógica de contagem de Special Summon
            if (isPlayer)
            {
                if (card.type.Contains("Fusion")) fusionSummonCount++;
                else if (card.type.Contains("Ritual")) ritualSummonCount++;
                // Outros tipos...
            }
        }

        return true;
    }
}