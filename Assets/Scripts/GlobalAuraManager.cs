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
                // Se o LUA não definiu um filtro (alvo específico), afeta todas as cartas!
                if (filterFunc == null) return true; 
                try {
                    // YGOPro Target functions geralmente esperam (e, c)
                    var result = filterFunc.Call(sourceEffect, card);
                    
                    // Fallback de segurança para filtros simples que esperam apenas (c)
                    if (result.Type != MoonSharp.Interpreter.DataType.Boolean)
                        result = filterFunc.Call(card);
                        
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
                    // Verifica se a carta é imune a este efeito específico
                    if (IsImmuneTo(card, aura.sourceEffect))
                    {
                        // Debug.Log($"[AuraManager] A carta {card.unityData?.name} está imune ao bloqueio de {aura.sourceCard?.unityData?.name}!");
                        continue;
                    }

                    Debug.Log($"[AuraManager] Aplicando Aura em {card.unityData.name} ({card.unityData.attribute}/{card.unityData.race}): {statType} {aura.value}");
                    total += aura.value;
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Verifica se uma carta está sob uma restrição específica (ex: "CANNOT_ATTACK").
    /// </summary>
    public bool IsUnderRestriction(LuaCard card, LuaEffect effectToActivate, string restrictionType, CardLocation location)
    {
        foreach (var aura in activeAuras)
        {
            if (aura.modifierType == restrictionType && (aura.affectedLocations & location) != 0)
            {
                bool applies = false;
                if (restrictionType == "CANNOT_ACTIVATE")
                {
                    if (aura.sourceEffect.GetValue() is MoonSharp.Interpreter.Closure valClosure)
                    {
                        try {
                            var res = valClosure.Call(aura.sourceEffect, effectToActivate, card.GetControler());
                            applies = res.Type == MoonSharp.Interpreter.DataType.Boolean && res.Boolean;
                        } catch { applies = false; }
                    }
                }
                else applies = aura.filter(card);

                if (applies)
                {
                    // VERIFICA IMUNIDADE ANTES DE BLOQUEAR!
                    if (IsImmuneTo(card, aura.sourceEffect))
                    {
                        // Debug.Log($"[AuraManager] A carta {card.unityData?.name} está imune ao bloqueio de {aura.sourceCard?.unityData?.name}!");
                        continue;
                    }
                    
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Verifica se uma carta é imune a um efeito restritivo (Ex: Amplifier protegendo Armadilhas do Jinzo).
    /// </summary>
    public bool IsImmuneTo(LuaCard card, LuaEffect threateningEffect)
    {
        if (threateningEffect == null) return false;
        
        foreach (var aura in activeAuras)
        {
            if (aura.modifierType == "IMMUNE")
            {
                if (aura.filter(card))
                {
                    // A imunidade avalia de quem vem a ameaça através da propriedade 'Value' do efeito IMMUNE (e, re)
                    object valObj = aura.sourceEffect.GetValue();
                    if (valObj is MoonSharp.Interpreter.Closure valClosure)
                    {
                        try
                        {
                            var res = valClosure.Call(aura.sourceEffect, threateningEffect);
                            if (res.Type == MoonSharp.Interpreter.DataType.Boolean && res.Boolean)
                            {
                                return true;
                            }
                        }
                        catch { }
                    }
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
        // Debug.Log("[AuraManager] Todas as auras globais foram limpas.");
    }
}