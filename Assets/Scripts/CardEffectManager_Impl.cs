using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // --- IMPLEMENTAÇÃO DOS EFEITOS ---

    // 0031 - Airknight Parshath
    void Effect_AirknightParshath(CardDisplay source)
    {
        Debug.Log("Airknight Parshath: Efeito ativado (Draw 1).");
        if (source.isPlayerCard)
            GameManager.Instance.DrawCard();
        else
            GameManager.Instance.DrawOpponentCard();
    }

    // 0050 - Ameba
    void Effect_Ameba(CardDisplay source)
    {
        Debug.Log("Ameba: Controle mudou! Causando 2000 de dano ao oponente.");
        GameManager.Instance.DamageOpponent(2000);
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

    public void CheckMaintenanceCosts()
    {
        if (GameManager.Instance.IsCardActiveOnField("0932")) // Imperial Order
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

    void Effect_CrushCardVirus(CardDisplay source)
    {
        if (SummonManager.Instance != null)
        {
            Debug.Log("Crush Card Virus: Tributando e destruindo monstros fortes do oponente.");
            if (SpellTrapManager.Instance != null)
            {
                DestroyAllMonsters(true, false); 
            }
        }
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

    void Effect_GainLP(CardDisplay source, int amount)
    {
        Debug.Log($"Ganhou {amount} LP.");
        // TODO: Implementar lógica real de ganho de LP no GameManager
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
                }
            );
        }
    }

    void Effect_TurnSet(CardDisplay source)
    {
        if (source.position == CardDisplay.BattlePosition.Attack)
            source.ChangePosition();
        source.ShowBack();
    }

    void Effect_SearchDeck(CardDisplay source, string type)
    {
        Debug.Log($"Procurando {type} no deck...");
    }

    void Effect_SearchDeckTop(CardDisplay source, string type, string subType = "")
    {
        Debug.Log($"Procurando {type}/{subType} para colocar no topo do deck.");
    }

    void Effect_FeatherOfThePhoenix(CardDisplay source)
    {
        Debug.Log("Feather of the Phoenix: Selecione carta no cemitério.");
    }

    void Effect_Field(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "", int levelMod = 0)
    {
        Debug.Log($"Campo ativado: {source.CurrentCardData.name}. Buff: {atkBonus}/{defBonus}");
    }

    void Effect_RevealSetCard(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped && !t.isPlayerCard,
                (t) => Debug.Log($"Revelada: {t.CurrentCardData.name}")
            );
        }
    }

    void Effect_ARivalAppears(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => Debug.Log($"Rival Appears: Invocando monstro Nível {t.CurrentCardData.level} da mão.")
            );
        }
    }

    void Effect_WingbeatOfGiantDragon(CardDisplay source)
    {
        Debug.Log("Wingbeat: Retornar Dragão e destruir S/T.");
    }

    void Effect_AbyssSoldier(CardDisplay source)
    {
        Debug.Log("Abyss Soldier: Descarte Water para retornar carta.");
    }

    void Effect_PayLP(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamagePlayer(amount);
    }

    void Effect_DestroyType(CardDisplay source, string type)
    {
        Debug.Log($"Destruindo todos os monstros tipo {type}...");
    }

    void Effect_AcidTrapHole(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped && t.position == CardDisplay.BattlePosition.Defense,
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.def <= 2000)
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                }
            );
        }
    }

    void Effect_Agido(CardDisplay source)
    {
        int roll = Random.Range(1, 7);
        Debug.Log($"Agido rolou: {roll}. Invocando Fada Nível {roll} do GY.");
    }

    void Effect_AltarForTribute(CardDisplay source)
    {
        Debug.Log("Altar for Tribute: Tributar 1 para ganhar LP.");
    }

    void Effect_TributeToBurn(CardDisplay source, int tributes, int damage, string race = "")
    {
        Debug.Log($"Tributando {tributes} {race} para causar {damage} dano.");
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_AmazonessSpellcaster(CardDisplay source)
    {
        Debug.Log("Amazoness Spellcaster: Trocar ATK.");
    }

    void Effect_AncientLamp(CardDisplay source)
    {
        Debug.Log("Ancient Lamp: Invocando La Jinn.");
    }

    void Effect_AncientTelescope(CardDisplay source)
    {
        Debug.Log("Ancient Telescope: Vendo topo do deck do oponente.");
    }

    void Effect_Ante(CardDisplay source)
    {
        Debug.Log("Ante: Minigame de revelar cartas.");
    }

    void Effect_AquaSpirit(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Aqua Spirit: Posição de {t.CurrentCardData.name} alterada.");
                }
            );
        }
    }

    void Effect_ArcaneArcher(CardDisplay source)
    {
        Debug.Log("Arcane Archer: Tributar Planta para destruir S/T.");
    }

    void Effect_ArchfiendsOath(CardDisplay source)
    {
        GameManager.Instance.DamagePlayer(500);
        Debug.Log("Archfiend's Oath: Declare uma carta.");
    }

    void Effect_ArchfiendsRoar(CardDisplay source)
    {
        GameManager.Instance.DamagePlayer(500);
        Debug.Log("Archfiend's Roar: Selecione Archfiend no GY.");
    }

    void Effect_ArchlordZerato(CardDisplay source)
    {
        Debug.Log("Archlord Zerato: Descarte LIGHT para destruir monstros.");
    }

    void Effect_LevelUp(CardDisplay source, string nextLevelId)
    {
        Debug.Log($"Level Up! Invocando {nextLevelId}.");
    }

    void Effect_ArmedDragonLV5(CardDisplay source)
    {
        Debug.Log("Armed Dragon LV5: Descarte monstro para destruir alvo.");
    }

    void Effect_ArmedDragonLV7(CardDisplay source)
    {
        Debug.Log("Armed Dragon LV7: Descarte monstro para destruir todos <= ATK.");
    }

    void Effect_DarkHole(CardDisplay source)
    {
        DestroyAllMonsters(true, true);
    }

    void Effect_Raigeki(CardDisplay source)
    {
        DestroyAllMonsters(true, false);
    }

    void Effect_PotOfGreed(CardDisplay source)
    {
        if (source.isPlayerCard) { GameManager.Instance.DrawCard(true); GameManager.Instance.DrawCard(true); }
        else { GameManager.Instance.DrawOpponentCard(); GameManager.Instance.DrawOpponentCard(); }
    }

    void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    void Effect_MST(CardDisplay source)
    {
        Debug.Log("MST ativado. Seleção de alvo pendente.");
    }

    void Effect_MonsterReborn(CardDisplay source)
    {
        Debug.Log("Monster Reborn ativado. Seleção de cemitério pendente.");
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
    }

    void Effect_BlockAttack(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack && !t.isPlayerCard,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Block Attack: {t.CurrentCardData.name} mudou para defesa.");
                }
            );
        }
    }

    void Effect_BlowbackDragon(CardDisplay source)
    {
        int heads = 0;
        for(int i=0; i<3; i++) if(Random.value > 0.5f) heads++;
        
        if (heads >= 2)
        {
            Debug.Log($"Blowback Dragon: {heads} caras! Destruindo alvo.");
        }
        else
        {
            Debug.Log($"Blowback Dragon: {heads} caras. Falhou.");
        }
    }

    void Effect_BookOfLife(CardDisplay source)
    {
        Debug.Log("Book of Life: Invocar Zumbi e banir monstro do oponente.");
    }

    void Effect_BookOfMoon(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                    t.ShowBack();
                    Debug.Log($"Book of Moon: {t.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    void Effect_BookOfTaiyou(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped,
                (t) => {
                    t.RevealCard();
                    t.ChangePosition();
                    Debug.Log($"Book of Taiyou: {t.CurrentCardData.name} virado para cima.");
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

    void Effect_BrainControl(CardDisplay source)
    {
        Debug.Log("Brain Control: Pagar 800 LP para controlar monstro.");
        Effect_ChangeControl(source, true);
        Effect_PayLP(source, 800);
    }

    void Effect_BurstStream(CardDisplay source)
    {
        Debug.Log("Burst Stream: Destruir monstros do oponente (se tiver Blue-Eyes).");
        DestroyAllMonsters(true, false);
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
                    {
                        toDestroy.Add(monster);
                    }
                }
            }
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_RingOfDestruction(CardDisplay source)
    {
        Debug.Log("Ring of Destruction ativado.");
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
                    Debug.Log($"Ring of Destruction: {damage} de dano para ambos.");
                }
            );
        }
    }

    void Effect_MagicCylinder(CardDisplay source)
    {
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            CardDisplay attacker = BattleManager.Instance.currentAttacker;
            int damage = attacker.CurrentCardData.atk;
            
            Debug.Log($"Magic Cylinder: Negando ataque de {attacker.CurrentCardData.name} e causando {damage} de dano.");
            
            if (source.isPlayerCard) GameManager.Instance.DamageOpponent(damage);
            else GameManager.Instance.DamagePlayer(damage);
        }
    }

    void Effect_Megamorph(CardDisplay source)
    {
        Effect_Equip(source, 0, 0);
        Debug.Log("Megamorph ativado: Se LP < Oponente -> Dobra ATK. Se LP > Oponente -> Metade ATK.");
    }

    void Effect_MagePower(CardDisplay source)
    {
        Effect_Equip(source, 0, 0);
        Debug.Log("Mage Power: Ganha 500 ATK/DEF para cada Spell/Trap que você controla.");
    }

    void Effect_MukaMuka(CardDisplay source)
    {
        Debug.Log("Muka Muka: Ganha 300 ATK/DEF para cada carta na sua mão.");
    }

    void Effect_Scapegoat(CardDisplay source)
    {
        for(int i=0; i<4; i++)
            GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Sheep Token");
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

    void Effect_MysticBox(CardDisplay source)
    {
        Debug.Log("Mystic Box: Selecione 1 monstro do oponente para destruir e 1 seu para dar o controle.");
    }

    void Effect_CallOfTheHaunted(CardDisplay source)
    {
        Debug.Log("Call of the Haunted: Invocar do GY em ataque.");
    }

    void Effect_CardDestruction(CardDisplay source)
    {
        Debug.Log("Card Destruction: Ambos descartam mão e compram a mesma quantidade.");
    }

    void Effect_Ceasefire(CardDisplay source)
    {
        Debug.Log("Ceasefire: Virar todos para cima e causar dano por efeito.");
    }

    void Effect_CemetaryBomb(CardDisplay source)
    {
        int damage = GameManager.Instance.opponentGraveyardDisplay.pileData.Count * 100;
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_ChangeOfHeart(CardDisplay source)
    {
        Debug.Log("Change of Heart: Controlar monstro até o fim do turno.");
    }

    void Effect_ChaosEmperorDragon(CardDisplay source)
    {
        Effect_PayLP(source, 1000);
        Debug.Log("Chaos Emperor Dragon: Enviar tudo para o GY e causar dano.");
    }

    void Effect_ChaosEnd(CardDisplay source)
    {
        if (GameManager.Instance.playerRemoved.Count >= 7)
        {
            DestroyAllMonsters(true, true);
        }
    }

    void Effect_ChaosSorcerer(CardDisplay source)
    {
        Debug.Log("Chaos Sorcerer: Banir monstro face-up.");
    }

    void Effect_AssaultOnGHQ(CardDisplay source)
    {
        Debug.Log("Assault on GHQ: Destruir monstro para millar oponente.");
    }

    void Effect_AutonomousActionUnit(CardDisplay source)
    {
        Effect_PayLP(source, 1500);
        Debug.Log("Autonomous Action Unit: Invocar do GY do oponente.");
    }

    void Effect_BackToSquareOne(CardDisplay source)
    {
        Debug.Log("Back to Square One: Descartar para retornar monstro ao topo do deck.");
    }

    void Effect_BarrelDragon(CardDisplay source)
    {
        int heads = 0;
        for(int i=0; i<3; i++) if(Random.value > 0.5f) heads++;
        if (heads >= 2) Debug.Log("Barrel Dragon: Sucesso! Destruir alvo.");
    }

    void Effect_Bazoo(CardDisplay source)
    {
        Debug.Log("Bazoo: Banir do GY para ganhar ATK.");
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

    void Effect_CompulsoryEvacuationDevice(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Compulsory Evacuation Device: {t.CurrentCardData.name} retornado para a mão.");
                    Destroy(t.gameObject); 
                }
            );
        }
    }

    void Effect_EnemyController(CardDisplay source)
    {
        Debug.Log("Enemy Controller: Escolha 1 efeito (Mudar Posição ou Controlar).");
    }
}
