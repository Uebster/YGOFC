using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // --- MÉTODOS UTILITÁRIOS COMUNS (REAPROVEITADOS) ---

    public void CheckMaintenanceCosts()
    {
        // Exemplo: Imperial Order (0932)
        if (GameManager.Instance.IsCardActiveOnField("0932"))
        {
            Debug.Log("Imperial Order: Manutenção de 700 LP.");
            if (GameManager.Instance.playerLP > 700)
            {
                GameManager.Instance.DamagePlayer(700);
            }
            else
            {
                Debug.Log("Imperial Order destruída por falta de LP.");
                // TODO: Destruir a carta
            }
        }
    }

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

    // --- EFEITOS COMUNS (REFERENCIADOS POR MÚLTIPLAS CARTAS) ---

    void Effect_MST(CardDisplay source)
    {
        Debug.Log("MST: Destruir 1 S/T.");
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                (t) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }

    void Effect_HeavyStorm(CardDisplay source)
    {
        Debug.Log("Heavy Storm: Destruir todas as S/T.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, toDestroy);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, toDestroy);
            CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell, GameManager.Instance.duelFieldUI.opponentFieldSpell }, toDestroy);
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_HarpiesFeatherDuster(CardDisplay source)
    {
        Debug.Log("Harpie's Feather Duster: Destruir S/T do oponente.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;
            CollectCards(zones, toDestroy);
            Transform fieldZone = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentFieldSpell : GameManager.Instance.duelFieldUI.playerFieldSpell;
            CollectCards(new Transform[] { fieldZone }, toDestroy);
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_Raigeki(CardDisplay source)
    {
        DestroyAllMonsters(true, false);
    }

    void Effect_MirrorForce(CardDisplay source)
    {
        Debug.Log("Mirror Force: Destruir monstros em ataque do oponente.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            foreach (var zone in zones)
            {
                if (zone.childCount > 0)
                {
                    var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null && monster.position == CardDisplay.BattlePosition.Attack)
                        toDestroy.Add(monster);
                }
            }
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_MagicCylinder(CardDisplay source)
    {
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            int damage = BattleManager.Instance.currentAttacker.CurrentCardData.atk;
            Effect_DirectDamage(source, damage);
        }
    }

    void Effect_EnemyController(CardDisplay source)
    {
        Debug.Log("Enemy Controller: Escolha 1 efeito (Mudar Posição ou Controlar).");
        Effect_ChangeControl(source, true);
    }

    void Effect_MonsterReborn(CardDisplay source)
    {
        Effect_Revive(source, true);
    }

    void Effect_MagePower(CardDisplay source)
    {
        Effect_Equip(source, 500, 500); // Simplificado
    }

    void Effect_MukaMuka(CardDisplay source)
    {
        Debug.Log("Muka Muka: Ganha ATK por cartas na mão.");
    }

    void Effect_MysticBox(CardDisplay source)
    {
        Debug.Log("Mystic Box: Destruir e trocar controle.");
    }

    void Effect_Scapegoat(CardDisplay source)
    {
        for(int i=0; i<4; i++) GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Sheep Token");
    }

    void Effect_RingOfDestruction(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => {
                    int damage = t.CurrentCardData.atk;
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                    GameManager.Instance.DamagePlayer(damage);
                    GameManager.Instance.DamageOpponent(damage);
                }
            );
        }
    }

    void Effect_SecretBarrel(CardDisplay source)
    {
        Effect_DirectDamage(source, 1000); // Simplificado
    }
}
