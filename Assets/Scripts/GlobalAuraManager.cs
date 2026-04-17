using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gerencia efeitos contínuos globais (Auras) que afetam todas as cartas no campo ou na mão.
/// Ex: Magias de Campo (Umi), Skill Drain, Gravity Bind.
/// </summary>
public class GlobalAuraManager : MonoBehaviour
{
    public static GlobalAuraManager Instance;

    public class Aura
    {
        public LuaCard sourceCard;
        public LuaEffect sourceEffect;
        public System.Func<LuaCard, bool> filter; // Função para saber se a carta é afetada
        public string modifierType; // "ATK", "DEF", "LEVEL", "CANNOT_ATTACK", etc.
        public int value;
        public CardLocation affectedLocations;
    }

    private List<Aura> activeAuras = new List<Aura>();

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Chamado pelo LUA para registrar um novo efeito de aura global.
    /// </summary>
    public void RegisterAura(LuaCard sourceCard, LuaEffect sourceEffect, MoonSharp.Interpreter.Closure filterFunc, string modType, int modValue, CardLocation locations)
    {
        // Previne duplicatas da mesma fonte
        RemoveAura(sourceCard, sourceEffect);

        Aura newAura = new Aura
        {
            sourceCard = sourceCard,
            sourceEffect = sourceEffect,
            filter = (card) => {
                try {
                    var result = filterFunc.Call(card);
                    return result.Type == MoonSharp.Interpreter.DataType.Boolean && result.Boolean;
                } catch { return false; }
            },
            modifierType = modType,
            value = modValue,
            affectedLocations = locations
        };
        activeAuras.Add(newAura);
        Debug.Log($"[AuraManager] Aura registrada: {modType} ({modValue}) de {sourceCard.unityData.name}.");
        GameManager.Instance?.RefreshAllCardsVisuals();
    }

    /// <summary>
    /// Chamado pelo LUA quando a carta que gera a aura é destruída ou negada.
    /// </summary>
    public void RemoveAura(LuaCard sourceCard, LuaEffect sourceEffect)
    {
        int removedCount = activeAuras.RemoveAll(a => a.sourceCard == sourceCard && a.sourceEffect == sourceEffect);
        if (removedCount > 0)
        {
            Debug.Log($"[AuraManager] Aura de {sourceCard.unityData.name} removida.");
            GameManager.Instance?.RefreshAllCardsVisuals();
        }
    }

    /// <summary>
    /// Calcula o modificador total para um stat específico de uma carta.
    /// </summary>
    public int GetStatModifier(LuaCard card, string statType, CardLocation location)
    {
        int total = 0;
        foreach (var aura in activeAuras)
        {
            if (aura.modifierType == statType && (aura.affectedLocations & location) != 0)
            {
                if (aura.filter(card))
                {
                    total += aura.value;
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Verifica se uma carta está sob uma restrição específica (ex: "CANNOT_ATTACK").
    /// </summary>
    public bool IsUnderRestriction(LuaCard card, string restrictionType, CardLocation location)
    {
        foreach (var aura in activeAuras)
        {
            if (aura.modifierType == restrictionType && (aura.affectedLocations & location) != 0)
            {
                if (aura.filter(card))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Limpa todas as auras ativas. Chamado no final do duelo.
    /// </summary>
    public void ClearAllAuras()
    {
        activeAuras.Clear();
        Debug.Log("[AuraManager] Todas as auras globais foram limpas.");
    }
}