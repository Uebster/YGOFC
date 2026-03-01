using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // --- MÉTODOS UTILITÁRIOS COMUNS (REAPROVEITADOS) ---

    // --- SISTEMA DE EVENTOS E FASES (TURNOBSERVER) ---

    public void OnPhaseStart(GamePhase phase)
    {
        Debug.Log($"CardEffectManager: Processando efeitos da fase {phase}...");

        if (phase == GamePhase.Standby)
        {
            // 1. Custos de Manutenção (Maintenance Costs)
            CheckMaintenanceCosts();

            // 2. Efeitos de Fase (Phase Triggers)
            
            // Dancing Fairy (0393): Ganha 1000 LP se em Defesa
            CheckActiveCards("0393", (card) => {
                if (card.position == CardDisplay.BattlePosition.Defense && card.isPlayerCard)
                {
                    Effect_GainLP(card, 1000);
                }
            });

            // Darklord Marie (0453): Ganha 200 LP se no GY
            List<CardData> myGY = GameManager.Instance.GetPlayerGraveyard();
            foreach(var cardData in myGY)
            {
                if (cardData.id == "0453")
                {
                    Debug.Log("Darklord Marie (GY): Ganha 200 LP.");
                    GameManager.Instance.playerLP += 200;
                    // TODO: Atualizar UI
                }
            }

            // Solar Flare Dragon (1686): Dano na End Phase (mas vamos por aqui como exemplo de estrutura)
            // (Na verdade é End Phase, movido para lá se fosse o caso)
        }
        else if (phase == GamePhase.End)
        {
            // Solar Flare Dragon (1686): 500 dano
            CheckActiveCards("1686", (card) => {
                if (card.isPlayerCard) Effect_DirectDamage(card, 500);
            });

            // Limpa buffs temporários de todas as cartas no campo
            // (Isso deveria ser feito em todos os monstros, não só nos ativos)
            if (GameManager.Instance.duelFieldUI != null)
            {
                CleanAllExpiredModifiers();
            }
        }
    }

    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer)
    {
        // Coffin Seller (0314): Dano quando monstro do oponente vai pro GY
        if (!isOwnerPlayer && card.type.Contains("Monster"))
        {
            CheckActiveCards("0314", (source) => {
                if (source.isPlayerCard)
                {
                    Debug.Log("Coffin Seller: Oponente enviou monstro ao GY. 300 Dano.");
                    Effect_DirectDamage(source, 300);
                }
            });
        }
    }

    public void OnSpecialSummon(CardDisplay summonedCard)
    {
        // Card of Safe Return (0266): Compra 1 quando monstro é invocado do GY
        // (Precisaríamos saber se veio do GY, por enquanto assumimos que sim para teste ou adicionamos flag)
        CheckActiveCards("0266", (source) => {
            if (source.isPlayerCard && summonedCard.isPlayerCard)
            {
                Debug.Log("Card of Safe Return: Compra 1 carta.");
                GameManager.Instance.DrawCard();
            }
        });
    }

    public void OnDamageTaken(bool isPlayer, int amount)
    {
        // Numinous Healer (1360), Attack and Receive (0117) - Geralmente são Traps ativáveis, não automáticas.
        // Mas efeitos contínuos como "Des Wombat" (0477) preveniriam isso antes.
    }

    public void OnCardLeavesField(CardDisplay card)
    {
        // Remove quaisquer modificadores que esta carta tenha aplicado em outras
        // Ex: Se um Equip Spell for destruído, o monstro perde o buff
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

            foreach (var zone in allZones)
            {
                if (zone.childCount == 0) continue;
                CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                if (target != null) target.RemoveModifiersFromSource(card);
            }
        }
    }

    // --- BATTLE HOOKS ---

    public bool IsAttackRestricted(CardDisplay attacker)
    {
        // Swords of Revealing Light (72302403)
        // Verifica se o oponente do atacante tem Swords ativo
        if (GameManager.Instance.duelFieldUI != null)
        {
            bool attackerIsPlayer = attacker.isPlayerCard;
            Transform[] enemySpellZones = attackerIsPlayer ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;
            
            foreach (var zone in enemySpellZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.id == "72302403")
                    {
                        Debug.Log("Ataque impedido por Swords of Revealing Light!");
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue)
    {
        // Aqui poderíamos verificar Kuriboh na mão, etc.
        // Por enquanto, apenas continua o fluxo.
        onContinue?.Invoke();
    }

    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target)
    {
        // Skyscraper (63035430)
        // Se E-Hero atacando monstro mais forte, +1000 ATK.
        if (GameManager.Instance.IsCardActiveOnField("63035430"))
        {
            if (attacker.CurrentCardData.name.Contains("Elemental HERO") && target != null)
            {
                int targetPower = (target.position == CardDisplay.BattlePosition.Attack) ? target.currentAtk : target.currentDef;
                if (targetPower > attacker.currentAtk)
                {
                    Debug.Log("Skyscraper: +1000 ATK para E-HERO.");
                    attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 1000, null));
                }
            }
        }

        // Injection Fairy Lily (79575620) - Lógica de pagar LP seria aqui
    }

    public void OnBattleEnd(CardDisplay attacker, CardDisplay target)
    {
        // D.D. Warrior Lady (7572887) - Lógica de banir seria aqui
        // Mystic Tomato (83011278) - Lógica de busca seria aqui
    }

    public void OnLifePointsGained(bool isPlayer, int amount)
    {
        // Fire Princess (64752646)
        // Se você ganhar LP, causa 500 de dano ao oponente.
        CheckActiveCards("64752646", (card) => {
            if (card.isPlayerCard == isPlayer)
            {
                Debug.Log("Fire Princess: Dano por cura.");
                Effect_DirectDamage(card, 500);
            }
        });
    }

    // Helper para iterar cartas ativas no campo
    private void CheckActiveCards(string cardId, System.Action<CardDisplay> action)
    {
        if (GameManager.Instance.duelFieldUI == null) return;
        
        // Verifica todas as zonas do jogador (e oponente se necessário)
        // Simplificado para zonas do jogador por enquanto
        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
        allZones.Add(GameManager.Instance.duelFieldUI.playerFieldSpell);

        foreach (var zone in allZones)
        {
            if (zone.childCount == 0) continue;
            CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
            if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.id == cardId)
            {
                action(cd);
            }
        }
    }

    private void CheckMaintenanceCosts()
    {
        // Imperial Order (0932)
        CheckActiveCards("0932", (card) => {
            if (card.isPlayerCard)
            {
                if (GameManager.Instance.playerLP > 700)
                {
                    Debug.Log("Imperial Order: Manutenção de 700 LP paga.");
                    GameManager.Instance.DamagePlayer(700);
                }
                else
                {
                    Debug.Log("Imperial Order: Destruída por falta de LP.");
                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                    Destroy(card.gameObject);
                }
            }
        });

        // Mirror Wall (1252)
        CheckActiveCards("1252", (card) => {
            if (card.isPlayerCard)
            {
                if (GameManager.Instance.playerLP > 2000)
                {
                    Debug.Log("Mirror Wall: Manutenção de 2000 LP paga.");
                    GameManager.Instance.DamagePlayer(2000);
                }
                else
                {
                    Debug.Log("Mirror Wall: Destruída por falta de LP.");
                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                    Destroy(card.gameObject);
                }
            }
        });
    }

    private void CleanAllExpiredModifiers()
    {
        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount == 0) continue;
            CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
            if (cd != null)
            {
                cd.CleanExpiredModifiers();
            }
        }
    }

    // --- FIM DO SISTEMA DE EVENTOS ---

    void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        }
    void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    void Effect_GainLP(CardDisplay source, int amount)
    {
        GameManager.Instance.GainLifePoints(source.isPlayerCard, amount);
    }

    void Effect_PayLP(CardDisplay source, int amount)
    {
        GameManager.Instance.PayLifePoints(source.isPlayerCard, amount);
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
                    // Usa o novo sistema de modificadores
                    if (atkBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, atkBonus, source));
                    if (defBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, defBonus, source));
                    // TODO: Vincular visualmente
                }
            );
        }
    }

    void Effect_Field(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "", int levelMod = 0)
    {
        // Lógica simplificada: Aplica em todos os monstros atuais (em um sistema real, seria um efeito contínuo que checa ao entrar/sair)
        // Por enquanto, vamos aplicar como "Continuous" em todos os monstros válidos já no campo
        if (GameManager.Instance.duelFieldUI == null) return;

        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount == 0) continue;
            CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
            if (target == null) continue;

            bool matchRace = string.IsNullOrEmpty(requiredRace) || target.CurrentCardData.race == requiredRace;
            bool matchAttr = string.IsNullOrEmpty(requiredAttribute) || target.CurrentCardData.attribute == requiredAttribute;

            if (matchRace && matchAttr)
            {
                if (atkBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, atkBonus, source));
                if (defBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Field, StatModifier.Operation.Add, defBonus, source));
            }
        }
        Debug.Log($"Campo ativado: {source.CurrentCardData.name}. Buff aplicado.");
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
        if (source.position == CardDisplay.BattlePosition.Attack) source.ChangePosition();
        source.ShowBack();
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
                (t) => {
                    if (atk != 0) t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, atk, source));
                    if (def != 0) t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, def, source));
                }
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
