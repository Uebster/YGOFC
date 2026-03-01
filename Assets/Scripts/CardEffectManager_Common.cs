using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // --- MÉTODOS UTILITÁRIOS COMUNS (REAPROVEITADOS) ---

    void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    void Effect_GainLP(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.playerLP += amount;
        else GameManager.Instance.opponentLP += amount;
        // TODO: Atualizar UI de LP
        Debug.Log($"{source.CurrentCardData.name}: Ganhou {amount} LP.");
    }

    void Effect_PayLP(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamagePlayer(amount);
        // Nota: Oponente geralmente não paga custo em scripts automáticos, mas se precisar:
        // else GameManager.Instance.DamageOpponent(amount);
    }

    void Effect_DestroyType(CardDisplay source, string type)
    {
        Debug.Log($"Destruindo todos os monstros tipo {type}...");
        // Implementação real requereria iterar sobre o campo e destruir
        // DestroyAllMonsters(true, true, (m) => m.CurrentCardData.race == type);
    }

    void Effect_SearchDeck(CardDisplay source, string term, string typeFilter = "")
    {
        Debug.Log($"Procurando '{term}' ({typeFilter}) no deck...");
        // GameManager.Instance.OpenCardSelection(...)
    }

    void Effect_SearchDeckTop(CardDisplay source, string type, string subType = "")
    {
        Debug.Log($"Procurando {type}/{subType} para colocar no topo do deck.");
    }

    void Effect_Equip(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "")
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (target) => {
                    if (!target.isOnField || !target.CurrentCardData.type.Contains("Monster")) return false;
                    if (!string.IsNullOrEmpty(requiredRace) && target.CurrentCardData.race != requiredRace) return false;
                    if (!string.IsNullOrEmpty(requiredAttribute) && target.CurrentCardData.attribute != requiredAttribute) return false;
                    return true;
                },
                (target) => 
                {
                    Debug.Log($"{source.CurrentCardData.name} equipada em {target.CurrentCardData.name}");
                    target.ModifyStats(atkBonus, defBonus);
                    // TODO: Vincular visualmente
                }
            );
        }
    }

    void Effect_Field(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "", int levelMod = 0)
    {
        Debug.Log($"Campo ativado: {source.CurrentCardData.name}. Buff: {atkBonus}/{defBonus}");
        // Lógica de aplicar buff em área
    }

    void Effect_FlipDestroy(CardDisplay source, TargetType type)
    {
        Debug.Log($"Efeito FLIP ativado: {source.CurrentCardData.name}");
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (target) => IsValidTarget(target, type),
                (target) => 
                {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                }
            );
        }
    }

    void Effect_FlipReturn(CardDisplay source, TargetType type)
    {
        Debug.Log($"Efeito FLIP (Return) ativado: {source.CurrentCardData.name}");
        // Lógica de bounce
    }

    void Effect_FlipDestroyLevel(CardDisplay source, int level)
    {
        bool isPlayer = source.isPlayerCard;
        Transform[] targetZones = isPlayer ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
        
        foreach(Transform zone in targetZones)
        {
            if(zone.childCount > 0)
            {
                CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                if(target != null && target.CurrentCardData.level == level)
                {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, !isPlayer);
                    Destroy(target.gameObject);
                }
            }
        }
    }

    void Effect_TributeToDraw(CardDisplay source, int tributes, int draws)
    {
        if (SummonManager.Instance.HasEnoughTributes(tributes, source.isPlayerCard))
        {
            Debug.Log($"Tributando {tributes} para comprar {draws}.");
            // TODO: Consumir tributos
            for(int i=0; i<draws; i++) GameManager.Instance.DrawCard(true);
        }
    }

    void Effect_TributeToBurn(CardDisplay source, int tributes, int damage, string race = "")
    {
        Debug.Log($"Tributando {tributes} {race} para causar {damage} dano.");
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_LevelUp(CardDisplay source, string nextLevelId)
    {
        Debug.Log($"Level Up! Invocando {nextLevelId}.");
        // Lógica de buscar no deck/mão e invocar
    }

    void Effect_TurnSet(CardDisplay source)
    {
        if (source.position == CardDisplay.BattlePosition.Attack)
            source.ChangePosition();
        source.ShowBack();
    }

    void Effect_BuffStats(CardDisplay source, int atk, int def)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => t.ModifyStats(atk, def)
            );
        }
    }

    void Effect_ChangeControl(CardDisplay source, bool returnAtEndPhase)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.isPlayerCard != source.isPlayerCard,
                (t) => GameManager.Instance.SwitchControl(t)
            );
        }
    }

    void Effect_Revive(CardDisplay source, bool anyGraveyard)
    {
        List<CardData> targets = new List<CardData>();
        targets.AddRange(GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster")));
        if (anyGraveyard)
            targets.AddRange(GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.type.Contains("Monster")));

        GameManager.Instance.OpenCardSelection(targets, "Selecione monstro para reviver", (selected) => {
            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            Debug.Log($"Revivendo {selected.name}");
        });
    }

    void Effect_Union(CardDisplay source, string targetName, int atkBuff, int defBuff)
    {
        Debug.Log($"Union: Tentando equipar em {targetName}...");
        // Lógica de Union simplificada
        Effect_Equip(source, atkBuff, defBuff);
    }

    void Effect_CoinTossDestroy(CardDisplay source, int numCoins, int requiredHeads, TargetType targetType)
    {
        GameManager.Instance.TossCoin(numCoins, (heads) => {
            if (heads >= requiredHeads)
            {
                Debug.Log($"{source.CurrentCardData.name}: {heads} caras! Sucesso.");
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => IsValidTarget(t, targetType) && t.isPlayerCard != source.isPlayerCard,
                        (t) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                            GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                            Destroy(t.gameObject);
                        }
                    );
                }
            }
            else
            {
                Debug.Log($"{source.CurrentCardData.name}: {heads} caras. Falhou.");
            }
        });
    }
}
