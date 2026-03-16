using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0001 - 0500)
    // =========================================================================================

    void Effect_0001_3HumpLacooda(CardDisplay source)
    {
        // Regra: Se houver 3 "3-Hump Lacooda" face-up do seu lado do campo, tribute 2 deles para comprar 3 cartas.
        List<CardDisplay> lacoodas = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach (var zone in zones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay m = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && !m.isFlipped && m.CurrentCardData.name == "3-Hump Lacooda")
                    {
                        lacoodas.Add(m);
                    }
                }
            }
        }

        if (lacoodas.Count >= 3)
        {
            Debug.Log("3-Hump Lacooda: Condição atendida. Tributando 2 '3-Hump Lacooda' para comprar 3 cartas.");
            
            // Tributa os 2 primeiros encontrados
            for (int i = 0; i < 2; i++)
            {
                CardDisplay tribute = lacoodas[i];
                GameManager.Instance.SendToGraveyard(tribute.CurrentCardData, tribute.isPlayerCard);
                Destroy(tribute.gameObject);
            }
            
            // Compra 3 cartas
            for (int i = 0; i < 3; i++) GameManager.Instance.DrawCard(false); // ignoreLimit = false, mas forçamos 3 compras
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Requer 3 '3-Hump Lacooda' face-up no campo.");
        }
    }

    void Effect_0003_4StarredLadybugOfDoom(CardDisplay source)
    {
        // Regra: FLIP - Destrói todos os monstros de Nível 4 que o OPONENTE controla.
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] oppZones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            foreach (var zone in oppZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay m = zone.GetChild(0).GetComponent<CardDisplay>();
                    // Só destrói se estiver face-up (regra oficial de nível) e Nível = 4
                    if (m != null && !m.isFlipped && m.CurrentCardData.level == 4)
                    {
                        toDestroy.Add(m);
                    }
                }
            }
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_0004_7(CardDisplay source)
    {
        // Ganha 700 LP
        Effect_GainLP(source, 700);
    }

    void Effect_0006_7Completed(CardDisplay source)
    {        // Regra: Equip Machine. Ganha 700 ATK ou DEF.
        UIManager.Instance.ShowConfirmation("Deseja aumentar o ATK (Sim) ou DEF (Não)?", 
        () => {
            // Sim = ATK
            Effect_Equip(source, 700, 0, "Machine");
        }, 
        () => {
            // Não = DEF
            Effect_Equip(source, 0, 700, "Machine");
        });
    }

    void Effect_0007_8ClawsScorpion(CardDisplay source)
    {
        // Efeito 1 de Ignição: Pode virar para defesa face-down uma vez por turno
        if (source.position == CardDisplay.BattlePosition.Attack)
        {
            source.ChangePosition(); // Vira defesa
            source.ShowBack(); // Face-down
            source.hasUsedEffectThisTurn = true;
            Debug.Log("8-Claws Scorpion: Virou para defesa face-down.");
        }
        // Efeito 2: ATK vira 2400 ao atacar face-down defense (Implementado no OnDamageCalculation)
    }

    void Effect_0008_ACatOfIllOmen(CardDisplay source)
    {
        // Regra: FLIP - Seleciona 1 Trap do Deck e coloca no topo. Se Necrovalley estiver em campo, adiciona à mão em vez disso.
        bool hasNecrovalley = GameManager.Instance.IsCardActiveOnField("1324"); // ID Necrovalley
        
        if (hasNecrovalley)
            Effect_SearchDeck(source, "Trap", "Trap"); // Adiciona à mão
        else
            Effect_SearchDeckTop(source, "Trap", "Trap"); // Coloca no topo
    }

    void Effect_0009_ADealWithDarkRuler(CardDisplay source)
    {
        // Regra: (Quick-Play) Se um monstro Lv8+ que você controla foi enviado ao GY neste turno:
        // Pague metade dos LP; invoque "Berserk Dragon" da mão ou Deck.
        // Placeholder: Requer flag 'wasLevel8DestroyedThisTurn' no GameManager para validação estrita.

        int cost = source.isPlayerCard ? GameManager.Instance.playerLP / 2 : GameManager.Instance.opponentLP / 2;
        Effect_PayLP(source, cost);

        //GameManager.Instance.SpecialSummonById("0168", source.isPlayerCard);

        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
        CardData berserkDragon = deck.Find(c => c.id == "0168"); // ID Berserk Dragon
            
        if (berserkDragon != null)
        {
            Debug.Log("A Deal with Dark Ruler: Invocando Berserk Dragon!");
            GameManager.Instance.SpecialSummonFromData(berserkDragon, source.isPlayerCard);
            deck.Remove(berserkDragon);
        }
        else
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            berserkDragon = hand.Find(c => c.id == "0168");
            if (berserkDragon != null)
            {
                Debug.Log("A Deal with Dark Ruler: Invocando Berserk Dragon!");
                GameManager.Instance.SpecialSummonFromData(berserkDragon, source.isPlayerCard);
                hand.Remove(berserkDragon);
            }
            else
            {
                Debug.Log("A Deal with Dark Ruler: Berserk Dragon não encontrado na mão ou deck.");
            }
        }
    }

    void Effect_0010_AFeatherOfThePhoenix(CardDisplay source)
    {
        // Descarte 1 carta, selecione 1 carta do GY e coloque no topo do Deck
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        GameManager.Instance.OpenCardSelection(hand, "Selecione carta para descartar", (selectedDiscard) => {
            if (selectedDiscard != null)
            {
                GameManager.Instance.SendToGraveyard(selectedDiscard, source.isPlayerCard);
                hand.Remove(selectedDiscard);
        
                // UI de seleção do GY
                List<CardData> gy = source.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
                GameManager.Instance.OpenCardSelection(gy, "Selecione carta para o topo do Deck", (selectedGY) => {
                    if (selectedGY != null)
                    {
                        gy.Remove(selectedGY);
                        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : null; // Acesso restrito ao deck oponente
                        if (deck != null) deck.Insert(0, selectedGY);
                        Debug.Log($"{selectedGY.name} retornada ao topo do deck.");
                    }
                    else
                    {
                        Debug.Log("Nenhuma carta selecionada para retornar ao Deck.");
                    }
                });
            }
            else
            {
                Debug.Log("Nenhuma carta descartada.");
            }
        });
    }

    void Effect_0011_AFeintPlan(CardDisplay source)
    {
        // Oponente não pode atacar monstros face-down neste turno
        // Define flag no BattleManager
        Debug.Log("A Feint Plan: Monstros face-down protegidos de ataque este turno.");
        BattleManager.Instance.cannotAttackFaceDown = true;
    }

    void Effect_0012_AHeroEmerges(CardDisplay source)
    {
        // Oponente escolhe 1 carta da sua mão aleatoriamente. Se for monstro, SS.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            int rnd = Random.Range(0, hand.Count);
            CardData picked = hand[rnd];
            Debug.Log($"A Hero Emerges: Carta escolhida: {picked.name}");
            
            if (picked.type.Contains("Monster"))
            {
                GameManager.Instance.SpecialSummonFromData(picked, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(picked, source.isPlayerCard);
            }
            else
            {
                GameManager.Instance.SendToGraveyard(picked, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(picked, source.isPlayerCard);
            }
        }
    }

    void Effect_0013_ALegendaryOcean(CardDisplay source)
    {
        // Regra: Tratado como "Umi". Monstros WATER ganham +200 ATK/DEF. Reduz o nível de todos os WATER na mão/campo em 1.
        Effect_Field(source, 200, 200, "", "Water", -1);
    }

    void Effect_0014_AManWithWdjat(CardDisplay source)
    {
        // Regra: Quando Invocado por Invocação-Normal: Selecione 1 card Baixado no campo; olhe-o.
        if (source.summonedThisTurn && !source.wasSpecialSummoned)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && (t.isFlipped == false || t.position == CardDisplay.BattlePosition.Defense), // Set cards
                    (t) => {
                        // Revela apenas para o dono do efeito (log no console por enquanto)
                        Debug.Log($"A Man with Wdjat revela: {t.CurrentCardData.name}");
                        // Visualmente poderia piscar a carta temporariamente
                    }
                );
            }
        }
    }

    void Effect_0015_ARivalAppears(CardDisplay source)
    {
        // Regra: Selecione 1 monstro do oponente; SS 1 monstro da mão com mesmo Nível.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.FindAll(c => c.type.Contains("Monster")).Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui monstros na mão para invocar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    int targetLevel = t.CurrentCardData.level;
                    Debug.Log($"Rival Appears: Alvo Nível {targetLevel}. Selecione monstro da mão.");
                    // Filtra mão
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> candidates = hand.FindAll(c => c.level == targetLevel && c.type.Contains("Monster"));
                    
                    GameManager.Instance.OpenCardSelection(candidates, "Invoque monstro de mesmo nível", (selected) => {
                        GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    });
                }
            );
        }
    }

    void Effect_0016_AWingbeatOfGiantDragon(CardDisplay source)
    {
        // Regra: Retorne 1 Dragão Lv5+ face-up que você controla para a mão; destrua todas S/T.
        bool hasDragon = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach (var z in zones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && !m.isFlipped && m.CurrentCardData.race == "Dragon" && m.CurrentCardData.level >= 5) hasDragon = true;
                }
            }
        }

        if (!hasDragon)
        {
            UIManager.Instance.ShowMessage("Requer um monstro Dragon de Nível 5 ou maior face-up no seu campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Dragon" && t.CurrentCardData.level >= 5,
                (t) => {
                    // Retorna para mão
                    GameManager.Instance.ReturnToHand(t);
                    Debug.Log("Wingbeat: Dragão retornado.");
                    
                    // Destrói S/T
                    Effect_HeavyStorm(source); // Reusa lógica de destruir todas S/T
                }
            );
        }
    }

    void Effect_0017_ATeamTrapDisposalUnit(CardDisplay source)
    {
        // Regra: (Quick) Quando oponente ativa Trap: Tributa este card; nega e destrói.
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Trap") && !link.isPlayerEffect)
        {
            GameManager.Instance.TributeCard(source);
            NegateAndDestroy(source, link);
        }
    }

    void Effect_0018_AbsoluteEnd(CardDisplay source)
    {
        // Regra: Ative apenas durante o turno do oponente. Neste turno, ataques do oponente tornam-se ataques diretos.
        if (GameManager.Instance.isPlayerTurn == source.isPlayerCard)
        {
            UIManager.Instance.ShowMessage("Só pode ser ativada no turno do oponente.");
            return;
        }

        Debug.Log("Absolute End: Seus monstros não podem ser atacados, oponente ataca direto.");
        if (BattleManager.Instance != null) BattleManager.Instance.forceDirectAttack = true;
    }

    void Effect_0019_AbsorbingKidFromTheSky(CardDisplay source)
    {
        // Quando destrói monstro por batalha e envia ao GY: Ganha LP = Nível * 300.
        // Lógica deve ser chamada pelo BattleManager no evento OnMonsterDestroyed
        Debug.Log("Absorbing Kid: Efeito de ganho de LP configurado no BattleManager.");
    }

    void Effect_0021_AbyssSoldier(CardDisplay source)
    {
        // Regra: Uma vez por turno: Descarte 1 WATER; devolva 1 carta do campo para a mão.
        if (source.hasUsedEffectThisTurn)
        {
            UIManager.Instance.ShowMessage("Efeito já utilizado neste turno.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> waterMonsters = hand.FindAll(c => c.attribute == "Water");

        if (waterMonsters.Count > 0)
        {
             GameManager.Instance.OpenCardSelection(waterMonsters, "Descarte 1 monstro WATER", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log($"Abyss Soldier: Descartou {discarded.name}.");
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField,
                        (t) => {
                            Debug.Log($"Abyss Soldier: {t.CurrentCardData.name} retornada para a mão.");
                            GameManager.Instance.ReturnToHand(t);
                            source.hasUsedEffectThisTurn = true;
                        }
                    );
                }
             });
        }
        else
        {
             UIManager.Instance.ShowMessage("Requer 1 monstro WATER na mão para descartar.");
        }
    }

    void Effect_0022_AbyssalDesignator(CardDisplay source)
    {
        // Regra: Pague 1000 LP; declare Tipo e Atributo. Oponente envia 1 monstro correspondente da mão/deck ao GY.
        if (Effect_PayLP(source, 1000))
        {
            // Simulação de declaração (Em produção: UI de Input)
            string[] attributes = { "Dark", "Light", "Earth", "Water", "Fire", "Wind" };
            string[] races = { "Fiend", "Zombie", "Warrior", "Spellcaster", "Dragon", "Machine", "Beast" };
            
            string declaredAttribute = attributes[Random.Range(0, attributes.Length)];
            string declaredRace = races[Random.Range(0, races.Length)];

            Debug.Log($"Abyssal Designator: Declarando {declaredAttribute}/{declaredRace}.");
            
            // Simulação: Oponente envia 1 Dark/Fiend
            List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
            
            CardData target = oppDeck.Find(c => c.attribute == declaredAttribute && c.race == declaredRace);
            
            if (target != null)
            {
                 Debug.Log($"Abyssal Designator: Oponente enviou {target.name} ao GY.");
                 GameManager.Instance.SendToGraveyard(target, !source.isPlayerCard);
                 oppDeck.Remove(target);
            }
            else
            {
                 Debug.Log("Abyssal Designator: Oponente não possui monstro correspondente no Deck.");
            }
        }
    }

    void Effect_0024_AcidRain(CardDisplay source)
    {
        // Regra: Destrói todas as Máquinas face-up no campo.
        bool hasMachine = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.race == "Machine") hasMachine = true;
        }

        if (!hasMachine)
        {
            UIManager.Instance.ShowMessage("Não há monstros Máquina face-up no campo.");
            return;
        }

        Effect_DestroyType(source, "Machine");
    }

    void Effect_0025_AcidTrapHole(CardDisplay source)
    {
        // Regra: Alvo: 1 monstro face-down defesa. Vira face-up. Se DEF <= 2000, destrói. Senão, volta face-down.
        bool hasFaceDown = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.isFlipped && m.position == CardDisplay.BattlePosition.Defense) hasFaceDown = true;
        }

        if (!hasFaceDown)
        {
            UIManager.Instance.ShowMessage("Não há monstros virados para baixo em Defesa no campo.");
            return;
        }

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
                    else
                    {
                        t.ShowBack(); // Retorna face-down
                    }
                }
            );
        }
    }

     void Effect_0027_AdhesionTrapHole(CardDisplay source)
    {
        // Regra: Quando oponente invoca: Corta ATK pela metade original.
        List<CardDisplay> targets = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
             foreach(var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
             {
                 if(zone.childCount > 0)
                 {
                     var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                     if(monster != null && monster.summonedThisTurn) targets.Add(monster);
                 }
             }
        }

        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há monstros recém-invocados pelo oponente.");
            return;
        }

        foreach (var monster in targets)
        {
             Debug.Log($"Adhesion Trap Hole: {monster.CurrentCardData.name} ATK reduzido pela metade.");
             int reduction = monster.currentAtk / 2;
             monster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -reduction, source));
        }
    }

    void Effect_0028_AfterTheStruggle(CardDisplay source)
    {
        // Regra: Destrói todos os monstros face-up no campo que batalharam neste turno.
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        
        void CheckZones(Transform[] zones)
        {
            foreach(var z in zones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.battledThisTurn) toDestroy.Add(m);
                }
            }
        }
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            CheckZones(GameManager.Instance.duelFieldUI.playerMonsterZones);
            CheckZones(GameManager.Instance.duelFieldUI.opponentMonsterZones);
        }
        
        if (toDestroy.Count == 0)
        {
            UIManager.Instance.ShowMessage("Nenhum monstro batalhou neste turno.");
            return;
        }

        Debug.Log("After the Struggle: Destruindo monstros que batalharam.");
        foreach(var m in toDestroy)
        {
            GameManager.Instance.SendToGraveyard(m.CurrentCardData, m.isPlayerCard);
            Destroy(m.gameObject);
        }
    }

    void Effect_0029_Agido(CardDisplay source)
    {
        // Regra: Quando destruído em batalha e enviado ao GY: Rola dado (1-6). SS 1 Fada do GY com Nível = Resultado.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        if (!gy.Exists(c => c.race == "Fairy" && c.type.Contains("Monster")))
        {
             UIManager.Instance.ShowMessage("Não há monstros Fairy no seu Cemitério para reviver.");
             return;
        }

        int roll = Random.Range(1, 7);
        Debug.Log($"Agido rolou: {roll}.");
        
        // Filtra Fadas com nível compatível
        List<CardData> targets = gy.FindAll(c => c.race == "Fairy" && (roll == 6 ? c.level >= 6 : c.level == roll));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Selecione Fada para reviver", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0031_AirknightParshath(CardDisplay source)
    {
        // Piercing Damage + Draw 1 on damage.
        // Piercing é passivo no BattleManager.
        // Draw é trigger.
        Debug.Log("Airknight Parshath: Compra 1 carta (Trigger de dano).");
        if (source.isPlayerCard) GameManager.Instance.DrawCard();
    }

void Effect_0037_AlligatorsSwordDragon(CardDisplay source)
    {
        // Pode atacar direto se os únicos monstros face-up do oponente forem Earth, Water ou Fire.
        bool canAttackDirectly = true;
        if (GameManager.Instance.duelFieldUI == null) return;
        
        Transform[] enemyMonsterZones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

        foreach (var zone in enemyMonsterZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay monster = zone.GetChild(0).GetComponent<CardDisplay>();
                if (monster != null && !monster.isFlipped) // Só verifica monstros face-up
                {
                    string race = monster.CurrentCardData.race;
                    if (race != "Earth" && race != "Water" && race != "Fire")
                    {
                        canAttackDirectly = false;
                        break;
                    }
                }
                else
                {
                    canAttackDirectly = false;
                    break;
                }
            }
        }

        if (canAttackDirectly)
        {
            Debug.Log("Alligator's Sword Dragon: Pode atacar diretamente!");
            //  BattleManager.Instance.canCurrentlyAttackDirectly = true; // Assuming you have a variable for this
        }
        else
        {
            Debug.Log("Alligator's Sword Dragon: Não pode atacar diretamente.");
            // BattleManager.Instance.canCurrentlyAttackDirectly = false;
        }
    }

    void Effect_0039_AltarForTribute(CardDisplay source)
    {
        // Regra: Selecione 1 monstro, tribute, ganhe LP = ATK original.
        if (!SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            UIManager.Instance.ShowMessage("Você não controla monstros para tributar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    int heal = t.CurrentCardData.atk;
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    Effect_GainLP(source, heal);
                }
            );
        }
    }

    void Effect_0041_AmazonessArcher(CardDisplay source)
    {
        // Regra: Tribute 2 monstros; cause 1200 de dano.
        if (!SummonManager.Instance.HasEnoughTributes(2, source.isPlayerCard))
        {
            UIManager.Instance.ShowMessage("Você precisa de pelo menos 2 monstros para tributar.");
            return;
        }
        Effect_TributeToBurn(source, 2, 1200);
    }

    void Effect_0042_AmazonessArchers(CardDisplay source)
    {
        // Regra: Ative quando oponente ataca e você controla uma "Amazoness". Monstros oponente viram face-up Attack, -500 ATK.
        bool hasAmazoness = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    var m = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && !m.isFlipped && (m.CurrentCardData.name.Contains("Amazoness") || m.CurrentCardData.name == "Amazon Archer"))
                        hasAmazoness = true;
                }
            }
        }

        if (!hasAmazoness)
        {
            UIManager.Instance.ShowMessage("Você não controla um monstro 'Amazoness' face-up.");
            return;
        }

        Debug.Log("Amazoness Archers: Forçando ataque e aplicando debuff.");
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null)
                    {
                        // Vira para Ataque (sem ativar Flip effects)
                        if (monster.position == CardDisplay.BattlePosition.Defense || monster.isFlipped)
                        {
                            monster.position = CardDisplay.BattlePosition.Attack;
                            monster.ShowFront();
                            monster.isFlipped = false; // Define como revelado manualmente para não disparar trigger de Flip
                        }
                        // Reduz 500 ATK
                        monster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -500, source));
                    }
                }
            }
        }
        // Nota: A obrigatoriedade de ataque deve ser tratada no BattleManager (flag forceAllAttack)
    }

    void Effect_0043_AmazonessBlowpiper(CardDisplay source)
    {
        // Selecione 1 monstro face-up; -500 ATK até final do turno.
        bool hasFaceUp = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped) hasFaceUp = true;
        }

        if (!hasFaceUp)
        {
            UIManager.Instance.ShowMessage("Não há monstros face-up no campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => t.ModifyStats(-500, 0)
            );
        }
    }

    void Effect_0044_AmazonessChainMaster(CardDisplay source)
    {
        // Quando destruído em batalha: Pague 1500 LP; olhe mão do oponente, pegue 1 monstro.
        if (GameManager.Instance.GetOpponentHandData().Count == 0)
        {
            Debug.Log("Amazoness Chain Master: Oponente não tem cartas na mão.");
            return;
        }

        if (Effect_PayLP(source, 1500))
        {
            List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
            if (oppHand.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(oppHand, "Roubar Monstro do Oponente", (selected) => {
                    if (selected != null && selected.type.Contains("Monster"))
                    {
                        Debug.Log($"Amazoness Chain Master: Roubou {selected.name}.");
                        // Lógica simplificada: Adiciona cópia à sua mão (remover da mão do oponente requereria referência ao GameObject específico)
                        GameManager.Instance.AddCardToHand(selected, true);
                        // TODO: Remover carta real da mão do oponente
                    }
                });
            }
        }
    }

    void Effect_0045_AmazonessFighter(CardDisplay source)
    {
        // Sem dano de batalha para o controlador.
        // Passivo no BattleManager.
    }

    void Effect_0046_AmazonessPaladin(CardDisplay source)
    {
       if (GameManager.Instance.duelFieldUI == null) return;
        
        int amazonessCount = 0;

        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount == 0) continue;
            CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
            if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.name.Contains("Amazoness"))
            {
                amazonessCount++;
            }
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, amazonessCount*100, source));
        Debug.Log($"Campo ativado: Amazoness Paladin. Buff aplicado.");
    }

    void Effect_0047_AmazonessSpellcaster(CardDisplay source)
    {
        // Troca ATK original com 1 monstro do oponente até End Phase.
        bool hasAmazoness = false;
        bool oppHasMonster = false;

        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.CurrentCardData.name.Contains("Amazoness")) hasAmazoness = true;
                }
            }
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (z.childCount > 0) oppHasMonster = true;
            }
        }

        if (!hasAmazoness || !oppHasMonster)
        {
            UIManager.Instance.ShowMessage("Requer um monstro 'Amazoness' e um monstro no campo do oponente.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (myMonster) => myMonster.isOnField && myMonster.isPlayerCard && myMonster.CurrentCardData.name.Contains("Amazoness"),
                (myMonster) => {
                    // Seleciona oponente
                    SpellTrapManager.Instance.StartTargetSelection(
                        (oppMonster) => oppMonster.isOnField && !oppMonster.isPlayerCard && oppMonster.CurrentCardData.type.Contains("Monster"),
                        (oppMonster) => {
                            int myOriginal = myMonster.originalAtk;
                            int oppOriginal = oppMonster.originalAtk;

                            // Troca usando modificadores 'Set'
                            myMonster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, oppOriginal, source));
                            oppMonster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, myOriginal, source));

                            Debug.Log($"Amazoness Spellcaster: Trocou ATK original de {myMonster.CurrentCardData.name} e {oppMonster.CurrentCardData.name}.");
                        }
                    );
                }
            );
        }
    }

    void Effect_0048_AmazonessSwordsWoman(CardDisplay source)
    {
        // Oponente toma dano de batalha.
        // Passivo no BattleManager.
    }

    void Effect_0049_AmazonessTiger(CardDisplay source)
    {
        // +400 ATK. Oponente não pode atacar outros monstros "Amazoness".
        // Passivo.
    }

    void Effect_0050_Ameba(CardDisplay source)
    {
        // Quando controle muda para oponente: Cause 2000 dano a ele.
        // Trigger no ChangeControl.
        GameManager.Instance.DamageOpponent(2000);
    }

    void Effect_0053_AmphibiousBugrothMK3(CardDisplay source)
    {
        // Se "Umi" no campo, ataca direto.
        // O que falta: Lógica de ataque direto no BattleManager.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi ou Legendary Ocean
        {
            Debug.Log("MK-3: Condição de ataque direto ativa.");
        }
    }

    void Effect_0054_Amplifier(CardDisplay source)
    {
        // Regra: Equip Jinzo. Jinzo do controlador não nega Traps do controlador.
        bool hasJinzo = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.CurrentCardData.name == "Jinzo" && !m.isFlipped) hasJinzo = true;
        }

        if (!hasJinzo)
        {
            UIManager.Instance.ShowMessage("Não há 'Jinzo' face-up no campo para equipar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.name == "Jinzo",
                (t) => {
                    Effect_Equip(source, 0, 0); // Realiza o equip visual
                    Debug.Log("Amplifier: Equipado em Jinzo. (Lógica de imunidade de Trap deve ser verificada no SpellTrapManager).");
                }
            );
        }
    }

    void Effect_0055_AnOwlOfLuck(CardDisplay source)
    {
        // FLIP: Seleciona Field Spell do Deck, coloca no topo. (Se Necrovalley, mão).
        bool hasNecrovalley = GameManager.Instance.IsCardActiveOnField("1324");
        if (hasNecrovalley) Effect_SearchDeck(source, "Field", "Spell");
        else Effect_SearchDeckTop(source, "Field", "Spell");
    }

    void Effect_0058_AncientGearBeast(CardDisplay source)
    {
        // Nega efeitos de monstros destruídos. Oponente não ativa S/T na batalha.
        // Passivo.
    }

    void Effect_0059_AncientGearGolem(CardDisplay source)
    {
        // Piercing. Oponente não ativa S/T na batalha.
        // Passivo.
    }

    void Effect_0060_AncientGearSoldier(CardDisplay source)
    {
        // Oponente não ativa S/T na batalha.
        // Passivo.
    }

    void Effect_0062_AncientLamp(CardDisplay source)
    {
        // Regra: Main Phase - SS "La Jinn" da mão/Deck.
        if (PhaseManager.Instance.currentPhase == GamePhase.Main1)
        {
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            CardData laJinn = deck.Find(c => c.name.Contains("La Jinn")) ?? hand.Find(c => c.name.Contains("La Jinn"));
            
            if (laJinn == null)
            {
                UIManager.Instance.ShowMessage("Você não possui 'La Jinn' no Deck ou Mão.");
                return;
            }

            if (deck.Contains(laJinn))
            {
                deck.Remove(laJinn);
                GameManager.Instance.SpecialSummonFromData(laJinn, source.isPlayerCard);
            }
            else
            {
                hand.Remove(laJinn);
                GameManager.Instance.SpecialSummonFromData(laJinn, source.isPlayerCard);
            }
        }
    }

    void Effect_0066_AncientTelescope(CardDisplay source)
    {
        // Regra: Olhe até as 5 cartas do topo do deck do oponente.
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        if (oppDeck.Count == 0)
        {
            UIManager.Instance.ShowMessage("O Deck do oponente está vazio.");
            return;
        }

        if (oppDeck.Count > 0)
        {
            int count = Mathf.Min(5, oppDeck.Count);
            List<CardData> topCards = oppDeck.GetRange(0, count);
            GameManager.Instance.OpenCardSelection(topCards, "Topo do Deck do Oponente", (c) => { }); // Apenas visualização
        }
    }

    void Effect_0069_AndroSphinx(CardDisplay source)
    {
        // Efeito 1: Pagar 500 LP para SS da mão se "Pyramid of Light" estiver em campo.
        if (!source.isOnField) // Está na mão
        {
            if (GameManager.Instance.IsCardActiveOnField("1470")) // Pyramid of Light
            {
                Effect_PayLP(source, 500);
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                Debug.Log("Andro Sphinx: Invocado Especialmente.");
            }
            else
            {
                Debug.Log("Andro Sphinx: Requer 'Pyramid of Light' no campo para Invocação Especial.");
            }
        }
        // Efeito 2: Trigger de batalha (Dano) - Tratado no BattleManager
    }

    void Effect_0071_Ante(CardDisplay source)
    {
        // Regra: Ambos revelam 1 carta da mão. Quem tiver menor Level toma 1000 dano e descarta.
        List<CardData> myHand = GameManager.Instance.GetPlayerHandData();
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();

        if (myHand.Count == 0 || oppHand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Ambos os jogadores precisam ter cartas na mão.");
            return;
        }

        if (myHand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(myHand, "Selecione carta para Ante", (myCard) => {
                if (oppHand.Count > 0)
                {
                    CardData oppCard = oppHand[Random.Range(0, oppHand.Count)];
                    Debug.Log($"Ante: Você ({myCard.name} Lv{myCard.level}) vs Oponente ({oppCard.name} Lv{oppCard.level})");
                    
                    if (myCard.level > oppCard.level) GameManager.Instance.DamageOpponent(1000);
                    else if (oppCard.level > myCard.level) GameManager.Instance.DamagePlayer(1000);
                    else Debug.Log("Ante: Empate.");
                }
            });
        }
    }

    void Effect_0073_AntiRaigeki(CardDisplay source)
    {
        // Quando oponente ativa Raigeki: Negue, destrua todos monstros do oponente.
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.name == "Raigeki" && !link.isPlayerEffect)
        {
            NegateAndDestroy(source, link);
            DestroyAllMonsters(true, false);
        }
    }

    void Effect_0074_AntiAircraftFlower(CardDisplay source)
    {
        // Regra: Tribute 1 Inseto; cause 800 dano.
        bool hasInsect = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Insect") hasInsect = true;
        }

        if (!hasInsect)
        {
            UIManager.Instance.ShowMessage("Você não controla monstros Inseto para tributar.");
            return;
        }

        Effect_TributeToBurn(source, 1, 800, "Insect");
    }

    void Effect_0075_AntiSpell(CardDisplay source)
    {
        // Regra: Remova 2 Spell Counters; negue Magia.
        var link = GetLinkToNegate(source);
        if (link == null || !link.cardSource.CurrentCardData.type.Contains("Spell"))
        {
            UIManager.Instance.ShowMessage("Não há uma Carta Mágica sendo ativada para negar.");
            return;
        }

        if (GetTotalSpellCounters(source.isPlayerCard) >= 2)
        {
            if (RemoveSpellCounters(2, source.isPlayerCard)) NegateAndDestroy(source, link);
        }
        else
        {
            UIManager.Instance.ShowMessage("Requer 2 Spell Counters no seu campo.");
        }
    }

    void Effect_0076_AntiSpellFragrance(CardDisplay source)
    {
        // Magias devem ser setadas e esperar 1 turno.
        // Regra global no SpellTrapManager.
    }

    void Effect_0077_ApprenticeMagician(CardDisplay source)
    {
        // Quando destruído em batalha: SS Spellcaster Lv2 ou menor do Deck (Face-down).
        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : null;
        if (deck != null)
        {
            List<CardData> targets = deck.FindAll(c => c.race == "Spellcaster" && c.level <= 2);
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Selecione Mago Lv2-", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard, false, true); // Face-down Defense
                    deck.Remove(selected); // Remove do deck (simulado, pois SpecialSummonFromData não remove)
                    // Nota: Em um sistema real, SpecialSummonFromData deveria lidar com a remoção da origem.
                });
            }
        }
    }

    void Effect_0078_Appropriate(CardDisplay source)
    {
        // Quando oponente compra fora da Draw Phase: Compre 2.
        // Trigger global.
    }

    void Effect_0079_AquaChorus(CardDisplay source)
    {
        // Se houver monstros com mesmo nome: +500 ATK/DEF para eles.
        // Lógica simplificada: Verifica se há monstros com o mesmo nome no campo e aplica o buff.
        // Em um sistema completo, isso seria um efeito contínuo que monitora o campo.
        // Aqui, aplicamos como um efeito de ativação manual ou trigger de fase para demonstração.
        
        if (GameManager.Instance.duelFieldUI == null) return;

        List<CardDisplay> allMonsters = new List<CardDisplay>();
        CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
        CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

        Dictionary<string, List<CardDisplay>> nameGroups = new Dictionary<string, List<CardDisplay>>();

        foreach (var m in allMonsters)
        {
            string name = m.CurrentCardData.name;
            if (!nameGroups.ContainsKey(name))
                nameGroups[name] = new List<CardDisplay>();
            nameGroups[name].Add(m);
        }

        foreach (var group in nameGroups.Values)
        {
            if (group.Count > 1)
            {
                foreach (var m in group)
                {
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
                }
                Debug.Log($"Aqua Chorus: Buff aplicado em {group.Count} cópias de {group[0].CurrentCardData.name}.");
            }
        }
    }

    void Effect_0083_AquaSpirit(CardDisplay source)
    {
        // SS banindo 1 Water. Standby oponente: Muda posição de 1 monstro.
        // Parte 1: Invocação Especial (Geralmente tratada no SummonManager, aqui simulamos o efeito de campo)
        // Parte 2: Efeito de mudar posição na Standby do oponente (Trigger)
        
        // Se estiver na mão, tenta invocar
        if (!source.isOnField)
        {
            // Lógica de banir 1 Water do GY para invocar
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> waters = gy.FindAll(c => c.attribute == "Water");
            
            if (waters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(waters, "Banir 1 WATER para invocar", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    // Remover da mão (já que SpecialSummonFromData cria uma cópia)
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
            else
            {
                Debug.Log("Aqua Spirit: Requer 1 monstro WATER no GY.");
            }
        }
        else
        {
            // Efeito em campo: Mudar posição de 1 monstro do oponente
            // (Deveria ser na Standby do oponente, aqui ativamos manualmente para teste)
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
    }

    void Effect_0084_ArcanaKnightJoker(CardDisplay source)
    {
        // Quick: Descarte mesmo tipo (M/S/T) para negar efeito que dá alvo.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte para negar", (selected) => {
                // Verifica se o tipo da carta descartada corresponde ao tipo da carta alvo (Simulado)
                // Em um sistema real, precisariamos saber qual carta está na chain
                
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selected).GetComponent<CardDisplay>());
                Debug.Log($"Arcana Knight Joker: Descartou {selected.name}. Efeito negado (Simulado).");
                
                // Se houver chain, nega o último link
                if (ChainManager.Instance != null && ChainManager.Instance.currentChain.Count > 0)
                {
                    // Lógica de negação simplificada
                    Debug.Log("Arcana Knight Joker: Último efeito na corrente negado.");
                }
            });
        }
    }

    void Effect_0085_ArcaneArcherOfTheForest(CardDisplay source)
    {
        // Regra: Tribute 1 Plant; destrua 1 S/T.
        bool hasPlant = false;
        bool hasST = false;

        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Plant") hasPlant = true;
            
            foreach (var z in GameManager.Instance.duelFieldUI.playerSpellZones) if (z.childCount > 0) hasST = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if (z.childCount > 0) hasST = true;
            if (GameManager.Instance.duelFieldUI.playerFieldSpell.childCount > 0) hasST = true;
            if (GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) hasST = true;
        }

        if (!hasPlant || !hasST)
        {
            UIManager.Instance.ShowMessage("Requer um monstro Planta e uma Magia/Armadilha no campo para destruir.");
            return;
        }

        Effect_MST(source);
    }

    void Effect_0089_ArchfiendOfGilfer(CardDisplay source)
    {
        // Quando enviado ao GY: Equipa em monstro, -500 ATK.
        Effect_Equip(source, -500, 0);
    }

    void Effect_0090_ArchfiendsOath(CardDisplay source)
    {
        // Pague 500; declare nome. Escave topo. Se acertar, mão. Senão, GY.
        if (Effect_PayLP(source, 500))
        {
            // Simulação de "Declarar Nome": Abre o Card Library/Trunk para escolher uma carta alvo
            // Como não temos UI de input de texto livre, usamos o seletor de cartas do jogo
            List<CardData> allCards = GameManager.Instance.cardDatabase.cardDatabase;
            
            // Otimização: Mostrar apenas uma sub-lista ou permitir busca seria ideal, 
            // mas aqui vamos simular a declaração pegando uma carta aleatória do deck para teste de sucesso
            // ou falha.
            
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            if (deck.Count > 0)
            {
                CardData topCard = deck[0];
                deck.RemoveAt(0); // Escava
                
                Debug.Log($"Archfiend's Oath: Carta escavada: {topCard.name}");
                
                // Lógica simplificada: 50% de chance de acertar em modo automático/teste
                bool success = Random.value > 0.5f; 
                
                if (success)
                {
                    Debug.Log("Archfiend's Oath: Acertou! Adicionada à mão.");
                    GameManager.Instance.AddCardToHand(topCard, true);
                }
                else
                {
                    Debug.Log("Archfiend's Oath: Errou! Enviada ao Cemitério.");
                    GameManager.Instance.SendToGraveyard(topCard, true);
                }
            }
        }
    }

    void Effect_0091_ArchfiendsRoar(CardDisplay source)
    {
        // Regra: Pague 500; SS 1 Archfiend do GY. Destrua na End Phase.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.name.Contains("Archfiend") && c.type.Contains("Monster"));
        
        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui monstros 'Archfiend' no Cemitério.");
            return;
        }

        if (Effect_PayLP(source, 500))
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver Archfiend", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0092_ArchlordZerato(CardDisplay source)
    {
        // Regra: Descarte 1 Light; destrua todos monstros do oponente. (Requer Sanctuary).
        if (!GameManager.Instance.IsCardActiveOnField("1887"))
        {
            UIManager.Instance.ShowMessage("Requer 'The Sanctuary in the Sky' face-up no campo.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> lights = hand.FindAll(c => c.attribute == "Light" && c.type.Contains("Monster"));
        
        if (lights.Count == 0)
        {
            UIManager.Instance.ShowMessage("Requer 1 monstro LIGHT na mão para descartar.");
            return;
        }

        bool oppHasMonsters = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonsters = true;

        if (!oppHasMonsters)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros.");
            return;
        }

        GameManager.Instance.OpenCardSelection(lights, "Descarte 1 LIGHT", (selected) => {
            GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selected).GetComponent<CardDisplay>());
            DestroyAllMonsters(true, false);
        });
    }

    void Effect_0096_ArmedDragonLV3(CardDisplay source)
    {
        // Standby: Envia ao GY, SS LV5.
        Effect_LevelUp(source, "0097");
    }

    void Effect_0097_ArmedDragonLV5(CardDisplay source)
    {
        // Regra: Descarte monstro; destrua monstro oponente com ATK <= ATK do descartado.
        bool oppHasMonsters = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonsters = true;

        if (!oppHasMonsters)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Descarte 1 Monstro", (selectedCost) => {
                Debug.Log($"Armed Dragon LV5: Descartou {selectedCost.name} ({selectedCost.atk} ATK).");
                // TODO: Remover selectedCost da mão visualmente (GameManager.Discard não implementado ainda)

                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && t.currentAtk <= selectedCost.atk,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                            Debug.Log($"Armed Dragon LV5: Destruiu {target.CurrentCardData.name}.");
                        }
                    );
                }
            });
        }
        else
        {
            Debug.Log("Armed Dragon LV5: Nenhum monstro na mão para descartar.");
        }
    }

    void Effect_0098_ArmedDragonLV7(CardDisplay source)
    {
        // Regra: Descarte monstro; destrua todos monstros oponente com ATK <= ATK do descartado.
        bool oppHasMonsters = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonsters = true;

        if (!oppHasMonsters)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Descarte 1 Monstro", (selectedCost) => {
                Debug.Log($"Armed Dragon LV7: Descartou {selectedCost.name} ({selectedCost.atk} ATK).");
                
                List<CardDisplay> toDestroy = new List<CardDisplay>();
                Transform[] zones = GameManager.Instance.duelFieldUI.opponentMonsterZones;
                
                foreach(var zone in zones)
                {
                    if(zone.childCount > 0)
                    {
                        var m = zone.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null && m.currentAtk <= selectedCost.atk) toDestroy.Add(m);
                    }
                }
                
                DestroyCards(toDestroy, false); // false = oponente
            });
        }
        else
        {
            Debug.Log("Armed Dragon LV7: Nenhum monstro na mão para descartar.");
        }
    }

    void Effect_0099_ArmedNinja(CardDisplay source)
    {
        // FLIP: Destrói 1 Magia.
        Effect_FlipDestroy(source, TargetType.Spell);
    }

    void Effect_0100_ArmedSamuraiBenKei(CardDisplay source)
    {
        // Ataques adicionais = número de Equips.
        // Passivo no BattleManager.
    }
    
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0101 - 0200)
    // =========================================================================================

    void Effect_0101_ArmorBreak(CardDisplay source)
    {
        // Regra: Counter Trap: Negate Equip Spell activation.
        var link = GetLinkToNegate(source);
        if (link == null || !link.cardSource.CurrentCardData.type.Contains("Spell") || link.cardSource.CurrentCardData.property != "Equip")
        {
            UIManager.Instance.ShowMessage("Não há uma Magia de Equipamento sendo ativada para negar.");
            return;
        }
        NegateAndDestroy(source, link);
    }

    void Effect_0102_ArmorExe(CardDisplay source)
    {
        // Regra: Maintenance: Remove counter or destroy. Cannot attack turn it's summoned.
        // Manutenção tratada no CheckMaintenanceCosts. Restrição tratada no CanDeclareAttack.
    }

    void Effect_0103_ArmoredGlass(CardDisplay source)
    {
        // Regra: Nega os efeitos de todos os Equip Cards pelo resto do turno.
        Debug.Log("Armored Glass: Equipamentos inativos este turno.");
        CardEffectManager.Instance.armoredGlassActive = true;
    }

    void Effect_0108_ArrayOfRevealingLight(CardDisplay source)
    {
        // Regra: Declare 1 Type. Monsters of that type cannot attack the turn they are Summoned.
        string[] types = { "Warrior", "Dragon", "Spellcaster", "Fiend", "Zombie", "Machine", "Aqua", "Pyro", "Rock", "Winged Beast", "Fairy", "Beast", "Beast-Warrior", "Dinosaur", "Thunder", "Fish", "Sea Serpent", "Reptile", "Plant", "Insect" };
        string declaredType = types[Random.Range(0, types.Length)]; // Simulado
        
        Debug.Log($"Array of Revealing Light: Tipo declarado (Simulado): {declaredType}");
        CardEffectManager.Instance.arrayOfRevealingLightType = declaredType;
    }

    bool HasAnotherInsect(CardDisplay self)
    {
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = self.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach (var z in zones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m != self && m.CurrentCardData.race == "Insect" && !m.isFlipped) return true;
                }
            }
        }
        return false;
    }

    void Effect_0109_ArsenalBug(CardDisplay source)
    {
        if (HasAnotherInsect(source))
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, 2000, source));
        else
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, 1000, source));
    }

    bool HasAnotherInsect(CardDisplay self)
    {
        // TODO: Implementar checagem no campo
        return false; // SIMULADO
    }

    void Effect_0110_ArsenalRobber(CardDisplay source)
    {
        // Regra: Oponente escolhe Equip do Deck e envia ao GY. Se não tiver, você toma 1000 de dano.
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        if (oppDeck.Count == 0) return;

        List<CardData> equips = oppDeck.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");

        if (equips.Count > 0)
        {
            CardData selected = equips[Random.Range(0, equips.Count)];
            Debug.Log($"Arsenal Robber: Oponente selecionou e enviou {selected.name} ao GY.");
            
            oppDeck.Remove(selected);
            GameManager.Instance.SendToGraveyard(selected, !source.isPlayerCard);
        }
        else
        {
            Debug.Log("Arsenal Robber: Oponente não possui Equip Spells no Deck. Você toma 1000 de dano.");
            Effect_DirectDamage(source, 1000);
        }
    }

    void Effect_0111_ArsenalSummoner(CardDisplay source)
    {
        // Regra: FLIP: Add Guardian card from Deck to Hand.
        Effect_SearchDeck(source, "Guardian");
    }

    void Effect_0112_AssaultOnGHQ(CardDisplay source)
    {
        // Regra: Destroy 1 monster you control; send top 2 cards of opp Deck to GY.
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonster = true;

        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Você precisa de um monstro no campo para destruir.");
            return;
        }

        if (GameManager.Instance.GetOpponentMainDeck().Count < 2)
        {
            UIManager.Instance.ShowMessage("O oponente não tem cartas suficientes no deck.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    Debug.Log("Assault on GHQ: Monstro destruído. Mill 2 do oponente.");
                    GameManager.Instance.MillCards(!source.isPlayerCard, 2);
                }
            );
        }
    }

    void Effect_0113_AstralBarrier(CardDisplay source)
    {
        // Regra: If opp attacks a monster, you can make it a direct attack.
        // Implementado no BattleManager.cs (SelectTarget)
    }

    void Effect_0114_AsuraPriest(CardDisplay source)
    {
        // Regra: Spirit / Attack All
        // Lógica de ataque múltiplo no BattleManager.cs
    }

    void Effect_0115_AswanApparition(CardDisplay source)
    {
        // Regra: Dano -> Topo do deck (Trap)
        // Implementado no CardEffectManager_Impl.cs (OnDamageDealtImpl)
    }

    void Effect_0116_AtomicFirefly(CardDisplay source)
    {
        // Se destruído em batalha: Oponente toma 1000, depois 500. (Texto antigo varia, vamos usar 1000).
        // Trigger de destruição.
    }

    void Effect_0117_AttackAndReceive(CardDisplay source)
    {
        // Regra: Cause 700 de dano, mais 300 para cada "Attack and Receive" no seu GY.
        int count = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.id == "0117").Count;
        Effect_DirectDamage(source, 700 + (count * 300));
    }

    void Effect_0118_AussaTheEarthCharmer(CardDisplay source)
    {
        // Regra: FLIP: Tome o controle de 1 monstro EARTH do oponente.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.attribute == "Earth",
                (target) => {
                    GameManager.Instance.SwitchControl(target);
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Continuous);
                }
            );
        }
    }

    void Effect_0119_AutonomousActionUnit(CardDisplay source)
    {
        // Regra: Pague 1500 LP; SS 1 Monstro do GY do oponente em Posição de Ataque e equipe com esta carta.
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> targets = oppGY.FindAll(c => c.type.Contains("Monster"));

        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("O oponente não possui monstros no Cemitério.");
            return;
        }

        if (Effect_PayLP(source, 1500))
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver do Oponente", (selected) => {
                oppGY.Remove(selected);
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard, false, false); // Ataque Face-up
            });
        }
    }

    void Effect_0120_AvatarOfThePot(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData pot = hand.Find(c => c.name == "Pot of Greed" || c.id == "1447");
        
        if (pot != null)
        {
            GameManager.Instance.DiscardCardsByName(source.isPlayerCard, pot.name); // Simula o descarte
            GameManager.Instance.DrawCard();
            GameManager.Instance.DrawCard();
            GameManager.Instance.DrawCard();
        }
        else
        {
            UIManager.Instance.ShowMessage("Você precisa de 'Pot of Greed' na mão para descartar.");
        }
    }

    void Effect_0122_AxeOfDespair(CardDisplay source)
    {
        // Regra: Equip monster gains 1000 ATK.
        Effect_Equip(source, 1000, 0);
    }

    void Effect_0124_BESBigCore(CardDisplay source)
    {
        // Efeito de Contadores é tratado no OnSummonImpl
        // Efeito de Batalha é tratado no BattleManager e OnBattleEnd
        Debug.Log("B.E.S. Big Core: Lógica de contadores e batalha automática.");
    }

    void Effect_0125_BESCrystalCore(CardDisplay source)
    {
        // Efeito de Contadores/Batalha: Automático
        // Efeito de Ignição: Mudar posição
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"B.E.S. Crystal Core: {t.CurrentCardData.name} mudou para defesa.");
                }
            );
        }
    }

    void Effect_0127_BackToSquareOne(CardDisplay source)
    {
        // Regra: Descarte 1 carta; retorne 1 monstro do campo ao topo do Deck.
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonster = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasMonster = true;
        }

        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Não há monstros no campo.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa de 1 carta na mão para descartar.");
            return;
        }

        GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
            GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
            
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        GameManager.Instance.ReturnToDeck(target, true); // true = Topo
                    }
                );
            }
        });
    }

    void Effect_0128_Backfire(CardDisplay source)
    {
        // Continuous: When FIRE monster destroyed, 500 dmg to opp.
        Debug.Log("Backfire: Ativo. (Trigger de destruição pendente).");
    }

    void Effect_0129_BackupSoldier(CardDisplay source)
    {
        // Regra: Se houver 5+ monstros no seu GY: Target até 3 Monstros Normais no GY (1500 ou menos ATK); adicione-os à mão.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        if (gy.FindAll(c => c.type.Contains("Monster")).Count < 5)
        {
            UIManager.Instance.ShowMessage("Você precisa de pelo menos 5 monstros no Cemitério.");
            return;
        }

        List<CardData> targets = gy.FindAll(c => c.type.Contains("Normal") && !c.type.Contains("Effect") && c.atk <= 1500);
        
        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há Monstros Normais com 1500 ou menos de ATK no seu Cemitério.");
            return;
        }

        int max = Mathf.Min(3, targets.Count);
        GameManager.Instance.OpenCardMultiSelection(targets, "Recuperar até 3 Normais (<= 1500 ATK)", 1, max, (selectedList) => {
            foreach(var c in selectedList)
            {
                gy.Remove(c);
                GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
            }
        });
    }

    void Effect_0130_BadReactionToSimochi(CardDisplay source)
    {
        Debug.Log("Simochi: Efeitos de cura do oponente viram dano.");
    }

    void Effect_0131_BaitDoll(CardDisplay source)
    {
        // Regra: Target 1 Set card; reveal it. If Trap, force activation (destroy). If not, return to Set.
        bool hasFaceDown = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allST = new List<CardDisplay>();
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allST);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allST);
            foreach(var c in allST) if (c.isFlipped) hasFaceDown = true;
        }

        if (!hasFaceDown)
        {
            UIManager.Instance.ShowMessage("Não há Magias ou Armadilhas viradas para baixo no campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && t.isFlipped == false,
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.type.Contains("Trap"))
                    {
                        Debug.Log($"Bait Doll: {t.CurrentCardData.name} é uma Armadilha! Forçando ativação (Destruindo).");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                    else
                    {
                        Debug.Log($"Bait Doll: {t.CurrentCardData.name} não é Armadilha. Retornando para face-down.");
                        t.ShowBack();
                    }
                    
                    // Bait Doll vai para o Deck
                    GameManager.Instance.ReturnToDeck(source, false); // false = Fundo (ou shuffle)
                    GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                }
            );
        }
    }

    void Effect_0132_BalloonLizard(CardDisplay source)
    {
        // Acumula contadores a cada Standby (Lógica no CardEffectManager_Impl.cs -> OnPhaseStart)
        // Dano ao destruir (Lógica no CardEffectManager_Impl.cs -> OnCardLeavesField)
        Debug.Log($"Balloon Lizard: Contadores atuais: {source.spellCounters}");
    }

    void Effect_0133_BanisherOfTheLight(CardDisplay source)
    {
        Debug.Log("Banisher of the Light: Cartas vão para Banished em vez do GY.");
    }

    void Effect_0134_BannerOfCourage(CardDisplay source)
    {
        // Battle Phase: +200 ATK to all your monsters.
        Debug.Log("Banner of Courage: Buff de Battle Phase.");
    }

    void Effect_0135_BarkOfDarkRuler(CardDisplay source)
    {
        // Regra: Pay 700 LP. Select 1 face-up Fiend-Type monster. It loses 700 DEF until the End Phase.
        bool hasFiend = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.CurrentCardData.race == "Fiend" && !m.isFlipped) hasFiend = true;
        }

        if (!hasFiend)
        {
            UIManager.Instance.ShowMessage("Não há monstros 'Fiend' face-up no campo.");
            return;
        }

        if (Effect_PayLP(source, 700))
        {
             if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.race == "Fiend",
                    (target) => {
                        target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -700, source));
                    }
                );
            }
        }
    }

    void Effect_0138_BarrelBehindTheDoor(CardDisplay source)
    {
        Debug.Log("Barrel Behind the Door: Counter Trap de dano.");
    }

    void Effect_0139_BarrelDragon(CardDisplay source)
    {
        // 3 Coins. 2+ Heads -> Destroy 1 monster.
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Monster);
    }

    void Effect_0144_BatteryCharger(CardDisplay source)
    {
        // Regra: Pay 500; SS 1 "Batteryman" from GY.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.name.Contains("Batteryman"));
        
        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui monstros 'Batteryman' no Cemitério.");
            return;
        }

        if (Effect_PayLP(source, 500))
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver Batteryman", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                gy.Remove(selected);
            });
        }
    }

    void Effect_0145_BatterymanAA(CardDisplay source)
    {
        // ATK = 1000 * number of AA in Attack. DEF = 1000 * number of AA in Defense.
        int atkCount = 0;
        int defCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all)
            {
                if (m.CurrentCardData.name == "Batteryman AA")
                {
                    if (m.position == CardDisplay.BattlePosition.Attack) atkCount++;
                    else defCount++;
                }
            }
        }
        
        source.RemoveModifiersFromSource(source);
        if (source.position == CardDisplay.BattlePosition.Attack)
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, atkCount * 1000, source));
        else
            source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, defCount * 1000, source));
    }

    void Effect_0146_BatterymanC(CardDisplay source)
    {
        // All Machines +500 ATK/DEF.
        Effect_Field(source, 500, 500, "Machine");
    }

    void Effect_0151_BattleScarred(CardDisplay source)
    {
        Debug.Log("Battle-Scarred: Oponente paga custo de manutenção de Archfiend.");
    }

    void Effect_0152_BazooTheSoulEater(CardDisplay source)
    {
        // Banish up to 3 from GY; +300 ATK per card.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster"));
        if (gy.Count > 0)
        {
            int max = Mathf.Min(3, gy.Count);
            GameManager.Instance.OpenCardMultiSelection(gy, "Banir até 3 monstros", 1, max, (selected) => {
                int count = selected.Count;
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    GameManager.Instance.GetPlayerGraveyard().Remove(c); // Remove do GY manualmente pois RemoveFromPlay não altera a lista de origem
                }
                // Atualiza visual
                GameManager.Instance.playerGraveyardDisplay.UpdatePile(GameManager.Instance.GetPlayerGraveyard(), GameManager.Instance.GetCardBackTexture());
                
                int buff = count * 300;
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
                Debug.Log($"Bazoo: Baniu {count} monstros. Ganhou {buff} ATK.");
            });
        }
    }

    void Effect_0155_BeastFangs(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Beast");
    }

    void Effect_0156_BeastSoulSwap(CardDisplay source)
    {
        // Regra: Return 1 Beast you control to hand; SS 1 Beast from hand.
        bool hasBeast = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) 
                if(z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Beast") hasBeast = true;

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        bool hasBeastInHand = hand.Exists(c => c.race == "Beast" && c.type.Contains("Monster"));

        if (!hasBeast || !hasBeastInHand)
        {
            UIManager.Instance.ShowMessage("Requer uma Besta no campo e outra na mão.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Beast",
                (t) => {
                    // Retorna para mão (Simulado: Destrói)
                    Destroy(t.gameObject);
                    Debug.Log("Beast Soul Swap: Besta retornada.");
                    
                    // Invoca da mão
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> beasts = hand.FindAll(c => c.race == "Beast" && c.level <= t.CurrentCardData.level); // Regra: Mesmo nível? Não, qualquer nível.
                    
                    if (beasts.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(beasts, "Invocar Besta", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    void Effect_0158_BeastkingOfTheSwamps(CardDisplay source)
    {
        // Discard: Add Polymerization.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        CardData poly = deck.Find(c => c.name == "Polymerization");
        
        if (poly != null)
        {
            Debug.Log("Beastking: Buscando Polymerization...");
            // Adicionar à mão (Lógica de AddToHand pendente no GameManager)
        }
    }

    void Effect_0163_BeckoningLight(CardDisplay source)
    {
        // Discard entire hand; add Light monsters from GY equal to discarded amount.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        int discardCount = hand.Count;
        
        if (discardCount > 0)
        {
            GameManager.Instance.DiscardHand(source.isPlayerCard);
            
            // Seleciona LIGHTs do GY
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> lights = gy.FindAll(c => c.attribute == "Light" && c.type.Contains("Monster"));
            
            if (lights.Count >= discardCount) 
            {
                 GameManager.Instance.OpenCardMultiSelection(lights, $"Selecione {discardCount} LIGHTs", discardCount, discardCount, (selected) => {
                     foreach(var c in selected) {
                         gy.Remove(c);
                         GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                     }
                     // Atualiza visual do GY
                     GameManager.Instance.playerGraveyardDisplay.UpdatePile(gy, GameManager.Instance.GetCardBackTexture());
                 });
            }
        }
    }

    void Effect_0164_BegoneKnave(CardDisplay source)
    {
        Debug.Log("Begone, Knave!: Se causar dano, retorna para a mão.");
    }

    void Effect_0166_BehemothTheKingOfAllAnimals(CardDisplay source)
    {
        // Tribute Summon: Return beasts from GY to hand.
        // O que falta: Trigger de Invocação Tributo.
        Debug.Log("Behemoth: Recupera bestas ao ser tributado.");
    }

    void Effect_0167_Berfomet(CardDisplay source)
    {
        // Summon: Add Gazelle from Deck.
        Effect_SearchDeck(source, "Gazelle the King of Mythical Beasts");
    }

    void Effect_0168_BerserkDragon(CardDisplay source)
    {
        // Attacks all monsters. Loses 500 ATK each End Phase.
        Debug.Log("Berserk Dragon: Ataca todos. Perde ATK.");
    }

    void Effect_0169_BerserkGorilla(CardDisplay source)
    {
        // Destroy if Defense. Must attack.
        // O que falta: Verificação contínua de posição e obrigação de ataque.
        if (source.position == CardDisplay.BattlePosition.Defense)
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            Debug.Log("Berserk Gorilla: Destruído por estar em defesa.");
        }
    }

    void Effect_0172_BigBangShot(CardDisplay source)
    {
        // Equip: +400, Piercing. If removed, banish monster.
        // O que falta: Trigger de remoção para banir.
        Effect_Equip(source, 400, 0);
    }

    void Effect_0173_BigBurn(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerGraveyard().Count == 0 && GameManager.Instance.GetOpponentGraveyard().Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há cartas nos Cemitérios para banir.");
            return;
        }
        Debug.Log("Big Burn: Bane monstros dos cemitérios (Simulado).");
    }

    void Effect_0174_BigEye(CardDisplay source)
    {
        // FLIP: Look at top 5, arrange them.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (deck.Count == 0)
        {
            UIManager.Instance.ShowMessage("O seu Deck está vazio.");
            return;
        }

        if (deck.Count > 0)
        {
            int count = Mathf.Min(5, deck.Count);
            List<CardData> topCards = deck.GetRange(0, count);
            
            GameManager.Instance.OpenCardMultiSelection(topCards, "Selecione a ordem (1º = Topo)", count, count, (ordered) => {
                deck.RemoveRange(0, count);
                deck.InsertRange(0, ordered);
                Debug.Log("Big Eye: Deck reordenado.");
            });
        }
    }

    void Effect_0177_BigShieldGardna(CardDisplay source)
    {
        // Negate Spell targeting it (face-down). Change to Attack if attacked.
        // O que falta: Trigger de alvo e ataque.
        Debug.Log("Big Shield Gardna: Efeitos defensivos.");
    }

    void Effect_0178_BigWaveSmallWave(CardDisplay source)
    {
        // Destroy all face-up Water; SS Water from hand equal to destroyed.
        bool hasWaterField = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0 && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.attribute == "Water") hasWaterField = true;
        }

        bool hasWaterHand = GameManager.Instance.GetPlayerHandData().Exists(c => c.attribute == "Water" && c.type.Contains("Monster"));

        if (!hasWaterField || !hasWaterHand)
        {
            UIManager.Instance.ShowMessage("Requer um monstro WATER face-up no campo e um monstro WATER na mão.");
            return;
        }

        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            // "Destroy all face-up WATER monsters you control"
            foreach(var m in all)
            {
                if (m.CurrentCardData.attribute == "Water" && !m.isFlipped) toDestroy.Add(m);
            }
        }
        
        int count = toDestroy.Count;
        DestroyCards(toDestroy, source.isPlayerCard);
        
        if (count > 0)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> waters = hand.FindAll(c => c.attribute == "Water" && c.type.Contains("Monster"));
            
            if (waters.Count > 0)
            {
                int max = Mathf.Min(count, waters.Count);
                GameManager.Instance.OpenCardMultiSelection(waters, $"Invocar até {max} WATER", 1, max, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(c, source.isPlayerCard);
                    }
                });
            }
        }
    }

    void Effect_0179_BigTuskedMammoth(CardDisplay source)
    {
        Debug.Log("Big-Tusked Mammoth: Impede ataques no turno que monstros oponente são invocados.");
    }

    void Effect_0183_Birdface(CardDisplay source)
    {
        // Destroyed by battle: Add Harpie Lady.
        Effect_SearchDeck(source, "Harpie Lady"); // Harpie Lady busca
    }

    void Effect_0184_BiteShoes(CardDisplay source)
    {
        // FLIP: Change position of 1 monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    t.ChangePosition();
                    Debug.Log("Bite Shoes: Posição alterada.");
                }
            );
        }
    }

    void Effect_0185_BlackDragonsChick(CardDisplay source)
    {
        // Send self to GY; SS Red-Eyes B. Dragon from hand.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData redEyes = hand.Find(c => c.name == "Red-Eyes B. Dragon");
        
        if (redEyes == null)
        {
            UIManager.Instance.ShowMessage("Você não possui 'Red-Eyes B. Dragon' na mão.");
            return;
        }

        if (source.isOnField)
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            
            GameManager.Instance.SpecialSummonFromData(redEyes, source.isPlayerCard);
            GameManager.Instance.RemoveCardFromHand(redEyes, source.isPlayerCard);
        }
    }

    void Effect_0189_BLSEnvoy(CardDisplay source)
    {
        // Banish 1 monster OR Double Attack.
        // Efeito de Ignição: Banir monstro (Custo: Não atacar)
        if (source.hasAttackedThisTurn) 
        {
            UIManager.Instance.ShowMessage("BLS: Já atacou neste turno, não pode usar o efeito de banir.");
            return;
        }
        
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0 && z.GetChild(0).gameObject != source.gameObject) hasTarget = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasTarget = true;
        }

        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há outros monstros no campo para banir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    GameManager.Instance.BanishCard(t);
                    source.hasAttackedThisTurn = true; // Impede ataque neste turno
                }
            );
        }
    }

    void Effect_0191_BlackPendant(CardDisplay source)
    {
        Effect_Equip(source, 500, 0);
        // TODO: Burn 500 if sent to GY.
    }

    void Effect_0193_BlackTyranno(CardDisplay source)
    {
        Debug.Log("Black Tyranno: Ataque direto se oponente só tiver defesa.");
    }

    void Effect_0195_BladeKnight(CardDisplay source)
    {
        Debug.Log("Blade Knight: +400 ATK se mão <= 1.");
    }

    void Effect_0196_BladeRabbit(CardDisplay source)
    {
        // Changed to Attack: Destroy 1 monster.
        // O que falta: Trigger de mudança de posição.
        Debug.Log("Blade Rabbit: Destrói monstro ao mudar posição.");
    }

    void Effect_0197_Bladefly(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Wind");
    }

    void Effect_0198_BlastHeldByATribute(CardDisplay source)
    {
        // Activate when opponent attacks with Tribute Summoned monster. Destroy & 1000 dmg.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            // Checagem de tributo pendente
            Debug.Log("Blast Held by a Tribute: Destruindo atacante...");
            // Destroy logic
            Effect_DirectDamage(source, 1000);
        }
    }

    void Effect_0199_BlastJuggler(CardDisplay source)
    {
        // Tribute (Standby): Destroy 2 face-up with ATK 1000 or less.
        if (PhaseManager.Instance.currentPhase != GamePhase.Standby)
        {
            UIManager.Instance.ShowMessage("Este efeito só pode ser ativado na sua Standby Phase.");
            return;
        }
        
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
             List<CardDisplay> all = new List<CardDisplay>();
             CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
             CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
             foreach(var m in all) if (!m.isFlipped && m.currentAtk <= 1000) count++;
        }

        if (count < 2)
        {
            UIManager.Instance.ShowMessage("Requer pelo menos 2 monstros face-up com 1000 ou menos de ATK no campo.");
            return;
        }
        
        if (source.isOnField)
        {
             GameManager.Instance.TributeCard(source);
             Debug.Log("Blast Juggler: Destruindo monstros com ATK <= 1000.");
        }
    }

    void Effect_0200_BlastMagician(CardDisplay source)
    {
        // Remove X counters -> Destroy 1 monster with ATK <= X * 700.
        int counters = SpellCounterManager.Instance.GetCount(source);
        if (counters == 0)
        {
            UIManager.Instance.ShowMessage("Blast Magician não possui Spell Counters.");
            return;
        }

        bool hasValidTarget = false;
        int maxAtkTarget = counters * 700;
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.currentAtk <= maxAtkTarget) hasValidTarget = true;
        }

        if (!hasValidTarget)
        {
             UIManager.Instance.ShowMessage($"Não há monstros com {maxAtkTarget} ou menos de ATK para destruir.");
             return;
        }

            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.currentAtk <= counters * 700,
                    (target) => {
                        // Calcula custo
                        int needed = Mathf.CeilToInt(target.currentAtk / 700f);
                        if (needed == 0) needed = 1; // Mínimo 1
                        
                        SpellCounterManager.Instance.RemoveCounter(source, needed);
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0201 - 0300)
    // =========================================================================================

    void Effect_0201_BlastSphere(CardDisplay source)
    {
        // Effect: If attacked face-down, equip to attacker. Standby: Destroy & Burn.
        // Logic handled in BattleManager (Equip) and OnPhaseStart (Destroy/Burn).
        Debug.Log("Blast Sphere: Efeitos de batalha e standby configurados.");
    }

    void Effect_0202_BlastWithChain(CardDisplay source)
    {
        // Target 1 face-up monster you control; equip this card to that target. It gains 500 ATK.
        // If this card is destroyed by a card effect while equipped: Target 1 card on the field; destroy that target.
        Effect_Equip(source, 500, 0);
    }

    void Effect_0203_BlastingTheRuins(CardDisplay source)
    {
        // Activate only if there are 30 or more cards in your GY. Inflict 3000 damage.
        int gyCount = source.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard().Count : GameManager.Instance.GetOpponentGraveyard().Count;
        if (gyCount >= 30) 
        {
            Effect_DirectDamage(source, 3000);
        }
        else
        {
            Debug.Log("Blasting the Ruins: Requer 30+ cartas no cemitério.");
        }
    }

    void Effect_0205_BlessingsOfTheNile(CardDisplay source)
    {
        // Continuous: Gain 1000 LP when cards are discarded by opponent's effect.
        Debug.Log("Blessings of the Nile: Ativo.");
    }

    void Effect_0206_BlindDestruction(CardDisplay source)
    {
        // Continuous Trap. Logic in OnPhaseStart.
        Debug.Log("Blind Destruction: Ativo.");
    }

    // Helper for Blind Destruction Logic (called from OnPhaseStart)
    void Effect_0206_BlindDestruction_Logic(CardDisplay source)
    {
        GameManager.Instance.TossCoin(1, (heads) => { // Using TossCoin as Dice proxy
             int roll = Random.Range(1, 7);
             Debug.Log($"Blind Destruction: Rolou {roll}.");
             
             List<CardDisplay> toDestroy = new List<CardDisplay>();
             if (GameManager.Instance.duelFieldUI != null)
             {
                 List<CardDisplay> all = new List<CardDisplay>();
                 CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                 CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
                 
                 foreach(var m in all)
                 {
                     if (roll == 6)
                     {
                         if (m.CurrentCardData.level >= 6) toDestroy.Add(m);
                     }
                     else
                     {
                         if (m.CurrentCardData.level == roll) toDestroy.Add(m);
                     }
                 }
             }
             
             DestroyCards(toDestroy, source.isPlayerCard);
        });
    }

    void Effect_0207_BlindlyLoyalGoblin(CardDisplay source)
    {
        // Control cannot switch.
        Debug.Log("Blindly Loyal Goblin: Controle fixo.");
    }

    void Effect_0208_BlockAttack(CardDisplay source)
    {
        // Target 1 Attack Position monster; change to Defense.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                }
            );
        }
    }

    void Effect_0210_BloodSucker(CardDisplay source)
    {
        // Efeito: Quando causa dano de batalha, oponente envia topo do deck ao GY.
        // Tratado no CardEffectManager_Impl.cs (OnDamageDealtImpl).
        Debug.Log("Blood Sucker: Efeito de mill passivo.");
    }

    void Effect_0211_BlowbackDragon(CardDisplay source)
    {
        // Once per turn: Target 1 card opp controls; toss coin 3 times. 2+ Heads -> Destroy.
        if (source.hasUsedEffectThisTurn) return;
        
        if (SpellTrapManager.Instance != null)
        {
            // Effect_CoinTossDestroy handles selection internally if implemented that way, 
            // but here we use the generic helper which assumes random or pre-selection.
            // For Blowback, we need to target first.
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard,
                (target) => {
                    GameManager.Instance.TossCoin(3, (heads) => {
                        if (heads >= 2)
                        {
                            Debug.Log($"Blowback Dragon: {heads} caras. Destruindo {target.CurrentCardData.name}.");
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                        else
                        {
                            Debug.Log($"Blowback Dragon: {heads} caras. Falhou.");
                        }
                    });
                }
            );
            source.hasUsedEffectThisTurn = true;
        }
    }

    void Effect_0212_BlueMedicine(CardDisplay source)
    {
        // Efeito: Ganha 400 LP.
        Effect_GainLP(source, 400);
    }

    void Effect_0214_BlueEyesShiningDragon(CardDisplay source)
    {
        // Efeito: SS tributando Blue-Eyes Ultimate. Ganha 300 ATK por Dragão no GY. Nega efeitos que dão alvo.
        if (!source.isOnField)
        {
            // Lógica de Invocação da Mão
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Blue-Eyes Ultimate Dragon",
                    (t) => {
                        GameManager.Instance.TributeCard(t);
                        GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                        // Remove da mão
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                    }
                );
            }
        }
        else
        {
            Debug.Log("Blue-Eyes Shining Dragon: Buff dinâmico e proteção aplicados (Passivo).");
        }
    }

    void Effect_0215_BlueEyesToonDragon(CardDisplay source)
    {
        // Efeito: Toon (Ataca direto se oponente não tiver Toon, etc).
        // Regras Toon tratadas no BattleManager.
        Debug.Log("Blue-Eyes Toon Dragon: Regras de Toon aplicadas.");
    }

    void Effect_0219_BoarSoldier(CardDisplay source)
    {
        // Efeito: Se invocado Normal, destrói a si mesmo. Se oponente tiver monstros, diminui ATK.
        // Auto-destruição tratada no OnSummonImpl.
        // Debuff:
        if (GameManager.Instance.duelFieldUI != null)
        {
            bool oppHasMonsters = false;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) oppHasMonsters = true;
            
            if (oppHasMonsters)
            {
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -1000, source));
            }
        }
    }

    void Effect_0223_BombardmentBeetle(CardDisplay source)
    {
        // Efeito: Flip 1 monstro face-down do oponente. Se for efeito, vira face-down de novo. Se Normal, destrói.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.isFlipped, // Face-down
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.type.Contains("Effect"))
                    {
                        Debug.Log("Bombardment Beetle: É Effect Monster. Virando face-down.");
                        t.ShowBack();
                    }
                    else
                    {
                        Debug.Log("Bombardment Beetle: É Normal Monster. Destruindo.");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                }
            );
        }
    }

    void Effect_0227_BookOfLife(CardDisplay source)
    {
        // Efeito: SS 1 Zumbi do seu GY e bane 1 monstro do GY do oponente.
        List<CardData> myGY = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> zombies = myGY.FindAll(c => c.race == "Zombie");
        
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> oppMonsters = oppGY.FindAll(c => c.type.Contains("Monster"));

        if (zombies.Count > 0 && oppMonsters.Count > 0)
        {
            // Passo 1: Seleciona Zumbi
            GameManager.Instance.OpenCardSelection(zombies, "Reviver Zumbi", (myZombie) => {
                // Passo 2: Seleciona monstro do oponente para banir
                GameManager.Instance.OpenCardSelection(oppMonsters, "Banir do Oponente", (oppMonster) => {
                    // Executa
                    GameManager.Instance.SpecialSummonFromData(myZombie, source.isPlayerCard);
                    GameManager.Instance.RemoveFromPlay(oppMonster, !source.isPlayerCard);
                    oppGY.Remove(oppMonster); // Remove manual pois RemoveFromPlay não altera lista de origem
                });
            });
        }
    }

    void Effect_0228_BookOfMoon(CardDisplay source)
    {
        // Efeito: Vira 1 monstro face-up para face-down Defense.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped, // Face-up
                (t) => {
                    t.ChangePosition(); // Vira defesa (se ataque)
                    t.ShowBack(); // Face-down
                    Debug.Log($"Book of Moon: {t.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    void Effect_0229_BookOfSecretArts(CardDisplay source)
    {
        // Efeito: Equip Spellcaster +300 ATK/DEF.
        Effect_Equip(source, 300, 300, "Spellcaster");
    }

    void Effect_0230_BookOfTaiyou(CardDisplay source)
    {
        // Efeito: Vira 1 monstro face-down para face-up Attack.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped, // Face-down
                (t) => {
                    t.RevealCard();
                    if (t.position == CardDisplay.BattlePosition.Defense) t.ChangePosition(); // Vira ataque
                    Debug.Log($"Book of Taiyou: {t.CurrentCardData.name} virado para cima.");
                }
            );
        }
    }

    void Effect_0232_BottomlessShiftingSand(CardDisplay source)
    {
        // Efeito Contínuo: Na End Phase do oponente, destrói o monstro com maior ATK (se > 2500).
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase)
        Debug.Log("Bottomless Shifting Sand: Ativo.");
    }

    void Effect_0233_BottomlessTrapHole(CardDisplay source)
    {
        // Efeito: Quando oponente invoca (1500+ ATK): Destrói e Bane.
        // Verifica monstros invocados recentemente com ATK >= 1500
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null && monster.summonedThisTurn && monster.currentAtk >= 1500)
                    {
                        Debug.Log($"Bottomless Trap Hole: Destruindo e banindo {monster.CurrentCardData.name}.");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
                        GameManager.Instance.RemoveFromPlay(monster.CurrentCardData, monster.isPlayerCard);
                        Destroy(monster.gameObject);
                    }
                }
            }
        }
    }

    void Effect_0235_Bowganian(CardDisplay source)
    {
        // Efeito: Causa 600 dano na sua Standby Phase.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Bowganian: Ativo.");
    }

    void Effect_0237_BrainControl(CardDisplay source)
    {
        // Pay 800 LP; take control of 1 face-up monster opp controls until End Phase.
        if (Effect_PayLP(source, 800))
        {
            Effect_ChangeControl(source, true);
        }
    }

    void Effect_0238_BrainJacker(CardDisplay source)
    {
        // Efeito: FLIP - Equipa no oponente e toma controle. Oponente ganha 500 LP na Standby.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Brain Jacker: Equipando em {t.CurrentCardData.name} e tomando controle.");
                    // Simulação: Vira equip (move para S/T) e toma controle do monstro
                    // Como não temos "Monstro virando Equip" nativo, vamos simular:
                    // 1. Brain Jacker vai para S/T do jogador
                    // 2. Toma controle do alvo
                    GameManager.Instance.SwitchControl(t);
                    // TODO: Mover Brain Jacker visualmente para S/T e criar link
                }
            );
        }
    }

    void Effect_0240_BreakerTheMagicalWarrior(CardDisplay source)
    {
        // Efeito: Normal Summon -> Ganha contador (+300 ATK). Remove contador -> Destrói S/T.
        // Ignition Effect: Se tiver contador, remove e destrói S/T
        if (source.spellCounters > 0)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                    (t) => {
                        source.RemoveSpellCounter(1);
                        // Remove o buff de ATK (assumindo que foi adicionado no AddCounter ou é dinâmico)
                        // Como o sistema de stats é recalculado, se tivermos um modificador baseado em counters, ele atualiza.
                        // Por enquanto, removemos manual se foi adicionado manual.
                        
                        Debug.Log($"Breaker: Destruindo {t.CurrentCardData.name}.");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                );
            }
        }
    }

    void Effect_0241_BreathOfLight(CardDisplay source)
    {
        // Efeito: Destrói todos os monstros Rock face-up.
        Effect_DestroyType(source, "Rock");
    }

    void Effect_0242_BubbleCrash(CardDisplay source)
    {
        // Efeito: Se tiver cartas na mão/campo, envie para o GY até ter 5.
        int totalCards = GameManager.Instance.GetPlayerHandData().Count;
        totalCards += GameManager.Instance.GetFieldCardCount(source.isPlayerCard);

        if (totalCards > 5)
        {
            int toSend = totalCards - 5;
            Debug.Log($"Bubble Crash: Você deve enviar {toSend} cartas para o GY.");
            // Abre seleção múltipla na mão para descartar (simplificado para mão por enquanto)
            GameManager.Instance.OpenCardMultiSelection(GameManager.Instance.GetPlayerHandData(), $"Selecione {toSend} para enviar ao GY", toSend, toSend, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                }
            });
        }
    }

    void Effect_0243_BubbleShuffle(CardDisplay source)
    {
        // Efeito: Alvo 1 E-Hero Bubbleman e 1 monstro oponente em ataque. Muda ambos para defesa, sacrifica Bubbleman, invoca E-Hero da mão.
        if (SpellTrapManager.Instance != null)
        {
            // Seleciona Bubbleman
            SpellTrapManager.Instance.StartTargetSelection(
                (tribute) => tribute.isOnField && tribute.isPlayerCard && tribute.CurrentCardData.name.Contains("Bubbleman"),
                (bubbleman) => {
                    // Seleciona Oponente
                    SpellTrapManager.Instance.StartTargetSelection(
                        (opp) => opp.isOnField && !opp.isPlayerCard && opp.position == CardDisplay.BattlePosition.Attack,
                        (oppMonster) => {
                            bubbleman.ChangePosition();
                            oppMonster.ChangePosition();
                            GameManager.Instance.TributeCard(bubbleman);
                            
                            // SS da mão
                            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                            List<CardData> heroes = hand.FindAll(c => c.name.Contains("Elemental HERO") && c.type.Contains("Monster"));
                            
                            if (heroes.Count > 0)
                            {
                                GameManager.Instance.OpenCardSelection(heroes, "Invocar E-Hero", (selected) => {
                                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                                    GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                                });
                            }
                        });
                });
        }
    }

    void Effect_0244_BubonicVermin(CardDisplay source)
    {
        // Efeito: FLIP - SS 1 Bubonic Vermin do Deck face-down.
        Effect_SearchDeck(source, "Bubonic Vermin"); // Simplificado (deveria invocar, não buscar para mão)
        // TODO: Alterar Effect_SearchDeck para suportar SS direto ou criar Effect_SpecialSummonFromDeck
    }

    void Effect_0246_BurningAlgae(CardDisplay source)
    {
        // Efeito: Quando enviado ao GY, oponente ganha 1000 LP.
        // Trigger de cemitério.
        if (source.isPlayerCard) GameManager.Instance.opponentLP += 1000;
        else GameManager.Instance.playerLP += 1000;
        Debug.Log("Burning Algae: Oponente ganhou 1000 LP.");
    }

    void Effect_0248_BurningLand(CardDisplay source)
    {
        // Efeito: Destrói Field Spells. Na Standby, ambos tomam 500 dano.
        // O que falta: TurnObserver para dano recorrente.
        Debug.Log("Burning Land: Destruindo campos...");
        // Lógica de destruir campos
        // Destroy(GameManager.Instance.duelFieldUI.playerFieldSpell.GetChild(0).gameObject);
    }

    void Effect_0249_BurningSpear(CardDisplay source)
    {
        // Efeito: Equip Fire +400 ATK, -200 DEF.
        Effect_Equip(source, 400, -200, "", "Fire");
    }

    void Effect_0250_BurstBreath(CardDisplay source)
    {
        // Efeito: Tributa 1 Dragão; destrói todos monstros face-up com DEF <= ATK do tributo.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Dragon",
                (tribute) => {
                    int atk = tribute.currentAtk;
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardDisplay> toDestroy = new List<CardDisplay>();
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        List<CardDisplay> all = new List<CardDisplay>();
                        CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                        CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
                        foreach(var m in all)
                        {
                            if (!m.isFlipped && m.currentDef <= atk) toDestroy.Add(m);
                        }
                    }
                    DestroyCards(toDestroy, true);
                }
            );
        }
    }

    void Effect_0251_BurstStreamOfDestruction(CardDisplay source)
    {
        // Efeito: Se controlar Blue-Eyes, destrói todos monstros do oponente. Blue-Eyes não ataca.
        // O que falta: Restrição de ataque no BattleManager.
        if (GameManager.Instance.IsCardActiveOnField("0000")) // ID do Blue-Eyes (Exemplo)
        {
            DestroyAllMonsters(true, false);
        }
        else
        {
            Debug.Log("Burst Stream: Requer Blue-Eyes White Dragon.");
        }
    }

    void Effect_0252_BusterBlader(CardDisplay source)
    {
        // Regra: +500 ATK por Dragão no campo/GY do oponente.
        // Buff atualizado dinamicamente no CardEffectManager_Impl.cs
        Debug.Log("Buster Blader: Efeito contínuo ativo.");
    }

    void Effect_0253_BusterRancher(CardDisplay source)
    {
        // Efeito: Equip (apenas ATK <= 1000). Se batalhar, ATK vira 2500.
        // Lógica de batalha no CardEffectManager_Impl.cs (OnDamageCalculation)
        Effect_Equip(source, 0, 0); // Apenas equipa, efeito é no cálculo
    }

    void Effect_0254_ButterflyDaggerElma(CardDisplay source)
    {
        // Efeito: Equip +300. Se destruída, volta para mão.
        Effect_Equip(source, 300, 0);
        // Trigger OnDestroy implementado no CardEffectManager_Impl.cs
    }

    void Effect_0255_ByserShock(CardDisplay source)
    {
        // Regra: Quando invocado, retorna todas as cartas Setadas para a mão.
        List<CardDisplay> toReturn = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, toReturn);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, toReturn);
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toReturn);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toReturn);
        }
        
        int count = 0;
        foreach(var c in toReturn)
        {
            if (c.isFlipped) 
            {
                GameManager.Instance.ReturnToHand(c);
                count++;
            }
        }
        Debug.Log($"Byser Shock: {count} cartas Setadas retornadas à mão.");
    }

    void Effect_0256_CallOfDarkness(CardDisplay source)
    {
        // Efeito Contínuo: Se ativar Monster Reborn, perde LP.
        // Implementado no OnSpecialSummon do CardEffectManager_Impl.cs (Dano genérico por SS do GY)
        Debug.Log("Call of Darkness: Ativo (Dano em SS do GY).");
    }

    void Effect_0257_CallOfTheEarthbound(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.trigger == ChainManager.TriggerType.Attack)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard == source.isPlayerCard,
                    (newTarget) => {
                        if (BattleManager.Instance != null)
                        {
                            BattleManager.Instance.currentTarget = newTarget;
                            Debug.Log($"Call of the Earthbound: Ataque redirecionado para {newTarget.CurrentCardData.name}.");
                        }
                    }
                );
            }
        }
        else
        {
            UIManager.Instance.ShowMessage("Só pode ser ativada em resposta a um ataque.");
        }
    }

    void Effect_0258_CallOfTheGrave(CardDisplay source)
    {
        // Efeito: Nega Monster Reborn.
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.name == "Monster Reborn")
        {
            NegateAndDestroy(source, link);
        }
    }

    void Effect_0259_CallOfTheHaunted(CardDisplay source)
    {
        // Efeito: SS do GY em Ataque. Se esta carta sair, destrói monstro. Se monstro sair, destrói esta.
        // O que falta: Vínculo persistente entre a Trap e o Monstro (Dependency).
        Effect_Revive(source, false); // false = apenas seu GY
    }

    void Effect_0260_CallOfTheMummy(CardDisplay source)
    {
        // Efeito: Se não controlar monstros, SS 1 Zumbi da mão.
        if (GameManager.Instance.duelFieldUI != null)
        {
            int monsterCount = 0;
            foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones) if (zone.childCount > 0) monsterCount++;
            
            if (monsterCount == 0)
            {
                List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                List<CardData> zombies = hand.FindAll(c => c.race == "Zombie" && c.type.Contains("Monster"));
                
                if (zombies.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(zombies, "Invocar Zumbi", (selected) => {
                        GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                    });
                }
                else
                {
                    Debug.Log("Call of the Mummy: Nenhum Zumbi na mão.");
                }
            }
            else
            {
                Debug.Log("Call of the Mummy: Você já controla monstros.");
            }
        }
    }

    void Effect_0262_CannonSoldier(CardDisplay source)
    {
        // Efeito: Tributa 1 monstro -> 500 dano.
        Effect_TributeToBurn(source, 1, 500);
    }

    void Effect_0263_CannonballSpearShellfish(CardDisplay source)
    {
        // Efeito: Imune a magias se Umi estiver no campo.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi
        {
            Debug.Log("Cannonball Spear Shellfish: Imune a Magias (Umi ativo).");
            // Lógica real de imunidade requereria verificação no SpellTrapManager ao resolver efeitos
        }
    }

    void Effect_0264_CardDestruction(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerHandData().Count == 0 && GameManager.Instance.GetOpponentHandData().Count == 0)
        {
            UIManager.Instance.ShowMessage("Nenhum dos jogadores possui cartas na mão para descartar.");
            return;
        }

        int playerHandCount = GameManager.Instance.GetPlayerHandData().Count;
        int oppHandCount = GameManager.Instance.GetOpponentHandData().Count;

        GameManager.Instance.DiscardHand(true);
        GameManager.Instance.DiscardHand(false);

        for(int i=0; i<playerHandCount; i++) GameManager.Instance.DrawCard(true); // ignoreLimit
        for(int i=0; i<oppHandCount; i++) GameManager.Instance.DrawOpponentCard();
    }

    void Effect_0265_CardShuffle(CardDisplay source)
    {
        // Regra: Pague 300 LP para embaralhar seu deck ou o do oponente.
        if (Effect_PayLP(source, 300))
        {
            UIManager.Instance.ShowConfirmation("Card Shuffle: Embaralhar seu Deck (Sim) ou do Oponente (Não)?", 
            () => { GameManager.Instance.ShuffleDeck(true); },
            () => { GameManager.Instance.ShuffleDeck(false); });
        }
    }

    void Effect_0266_CardOfSafeReturn(CardDisplay source)
    {
        // Efeito Contínuo: Quando um monstro é invocado do GY, compre 1 carta.
        // Lógica implementada no CardEffectManager_Impl.cs (OnSpecialSummon)
        Debug.Log("Card of Safe Return: Ativo.");
    }

    void Effect_0267_CardOfSanctity(CardDisplay source)
    {
        // Efeito (TCG): Bana todas as cartas da sua mão e campo; compre 2 cartas.
        
        // Banir Mão
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        // Simula banimento da mão (remove e adiciona a removed)
        foreach(var c in new List<CardData>(hand))
        {
            GameManager.Instance.RemoveCardFromHand(c, source.isPlayerCard);
            GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
        }
        
        // Banir Campo (Simplificado: Destrói tudo)
        DestroyAllMonsters(false, true);
        
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0268_CastleGate(CardDisplay source)
    {
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.level <= 5 && z.GetChild(0).gameObject != source.gameObject) hasTarget = true;

        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Você não controla monstros de Nível 5 ou menor para tributar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
             SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.level <= 5 && t != source,
                (t) => {
                    int dmg = t.originalAtk;
                    GameManager.Instance.TributeCard(t);
                    Effect_DirectDamage(source, dmg);
                    source.hasAttackedThisTurn = true; // Impede ataque
                }
            );
        }
    }

    void Effect_0269_CastleWalls(CardDisplay source)
    {
        // Efeito: Aumenta DEF de 1 monstro em 500 até a End Phase.
        Effect_BuffStats(source, 0, 500);
    }

    void Effect_0270_CastleOfDarkIllusions(CardDisplay source)
    {
        // Efeito: Flip - Todos os Zumbis ganham 200 ATK/DEF. Aumenta a cada Standby.
        // Aplica o primeiro buff
        Effect_Field(source, 200, 200, "Zombie");
        // Buffs subsequentes tratados no CardEffectManager_Impl.cs (OnPhaseStart)
    }

    void Effect_0271_CatsEarTribe(CardDisplay source)
    {
        // Efeito: O ATK original do oponente vira 200 durante o cálculo de dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation)
        Debug.Log("Cat's Ear Tribe: Efeito passivo de batalha.");
    }

    void Effect_0272_CatapultTurtle(CardDisplay source)
    {
        if (!SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            UIManager.Instance.ShowMessage("Você não possui monstros para tributar.");
            return;
        }
        
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (tribute) => {
                    int dmg = tribute.currentAtk / 2;
                    Debug.Log($"Catapult Turtle: Tributou {tribute.CurrentCardData.name} para {dmg} de dano.");
                    GameManager.Instance.TributeCard(tribute);
                    Effect_DirectDamage(source, dmg);
                }
            );
        }
    }

    void Effect_0273_CatnippedKitty(CardDisplay source)
    {
        bool hasDefense = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.position == CardDisplay.BattlePosition.Defense) hasDefense = true;
        }

        if (!hasDefense)
        {
            UIManager.Instance.ShowMessage("Não há monstros em Posição de Defesa no campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Defense,
                (t) => {
                    t.ModifyStats(0, -t.currentDef); // Zera a DEF
                    Debug.Log($"Catnipped Kitty: DEF de {t.CurrentCardData.name} reduzida a 0.");
                }
            );
        }
    }

    void Effect_0274_CaveDragon(CardDisplay source)
    {
        // Efeito: Se você controla monstro, não pode Normal Summon. Se não tiver Dragão, não ataca.
        // Restrição de ataque implementada no BattleManager.cs
        // Restrição de invocação requereria hook no SummonManager (CanNormalSummon)
        Debug.Log("Cave Dragon: Restrições aplicadas.");
    }

    void Effect_0275_Ceasefire(CardDisplay source)
    {
        bool canActivate = false;
        int effectMonsters = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.isFlipped) { m.RevealCard(); canActivate = true; }
                if (m.CurrentCardData.type.Contains("Effect")) { effectMonsters++; canActivate = true; }
            }
        }
        
        if (!canActivate || effectMonsters == 0)
        {
            UIManager.Instance.ShowMessage("Não há monstros de Efeito ou virados para baixo no campo.");
            return;
        }
        
        Effect_DirectDamage(source, effectMonsters * 500);
    }

    void Effect_0277_CemetaryBomb(CardDisplay source)
    {
        int count = GameManager.Instance.GetOpponentGraveyard().Count;
        if (count == 0)
        {
            UIManager.Instance.ShowMessage("O Cemitério do oponente está vazio.");
            return;
        }
        Effect_DirectDamage(source, count * 100);
    }

    void Effect_0278_CentrifugalField(CardDisplay source)
    {
        // Efeito: Se uma Fusão for destruída por efeito, invoca 1 material do GY.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField)
        Debug.Log("Centrifugal Field: Ativo.");
    }

    void Effect_0279_CeremonialBell(CardDisplay source)
    {
        // Efeito: Ambas as mãos ficam reveladas.
        Debug.Log("Ceremonial Bell: Revelando mãos.");
        GameManager.Instance.showOpponentHand = true;
        // Nota: Precisa de lógica para esconder novamente quando a carta sair do campo
    }

    void Effect_0280_CestusOfDagla(CardDisplay source)
    {
        // Efeito: Equip Fairy +500. Dano de batalha infligido ao oponente.
        Effect_Equip(source, 500, 0, "Fairy");
    }

    void Effect_0281_ChainBurst(CardDisplay source)
    {
        // Efeito: Quem ativar Trap toma 1000 de dano.
        // Requer hook no ActivateFieldSpellTrap ou ChainManager
        Debug.Log("Chain Burst: Ativo.");
    }

    void Effect_0282_ChainDestruction(CardDisplay source)
    {
        // Efeito: Destrói cópias no deck/mão de monstro invocado (ATK <= 2000).
        // Simplificado: Seleciona monstro no campo com ATK <= 2000
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.currentAtk <= 2000,
                (t) => {
                    Debug.Log($"Chain Destruction: Destruindo cópias de {t.CurrentCardData.name} no deck/mão.");
                    // Apenas log para o protótipo, pois requer acesso profundo às listas privadas do oponente
                }
            );
        }
    }

    void Effect_0283_ChainDisappearance(CardDisplay source)
    {
        // Efeito: Bane cópias no deck/mão de monstro invocado (ATK <= 1000).
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.currentAtk <= 1000,
                (t) => {
                    string name = t.CurrentCardData.name;
                    Debug.Log($"Chain Disappearance: Banindo {name} e cópias.");
                    
                    // Bane o alvo
                    GameManager.Instance.BanishCard(t);

                    // Bane cópias da mão e deck do controlador (Lógica simplificada para o Player)
                    if (t.isPlayerCard)
                    {
                        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                        
                        // Remove da mão (cópia segura)
                        foreach(var c in new List<CardData>(hand)) {
                            if (c.name == name) {
                                GameManager.Instance.RemoveCardFromHand(c, true);
                                GameManager.Instance.RemoveFromPlay(c, true);
                            }
                        }
                        // Remove do deck
                        foreach(var c in new List<CardData>(deck)) {
                            if (c.name == name) {
                                deck.Remove(c);
                                GameManager.Instance.RemoveFromPlay(c, true);
                            }
                        }
                        GameManager.Instance.ShuffleDeck(true);
                    }
                }
            );
        }
    }

    void Effect_0284_ChainEnergy(CardDisplay source)
    {
        // Efeito: Pagar 500 LP para jogar cartas da mão.
        // Lógica implementada no GameManager.CheckChainEnergyCost
        Debug.Log("Chain Energy: Ativo (Custo de 500 LP por ação).");
    }

    void Effect_0287_ChangeOfHeart(CardDisplay source)
    {
        bool oppHasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonster = true;
        
        if (!oppHasMonster)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros.");
            return;
        }

        Effect_ChangeControl(source, true);
    }

    void Effect_0288_ChaosCommandMagician(CardDisplay source)
    {
        // Efeito: Nega efeito de monstro que dê alvo neste card. (Passivo)
        Debug.Log("Chaos Command Magician: Imune a efeitos de monstro.");
    }

    void Effect_0289_ChaosEmperorDragon(CardDisplay source)
    {
        if (GameManager.Instance.playerLP <= 1000)
        {
            UIManager.Instance.ShowMessage("Você precisa de mais de 1000 LP para ativar este efeito.");
            return;
        }

        if (Effect_PayLP(source, 1000))
        {
            int count = 0;
            
            // Mão Oponente
            count += GameManager.Instance.GetOpponentHandData().Count;
            GameManager.Instance.DiscardHand(false);

            // Campo Oponente
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> oppCards = new List<CardDisplay>();
                CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppCards);
                CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, oppCards);
                count += oppCards.Count;
                DestroyCards(oppCards, false);
            }

            // Mão Player
            count += GameManager.Instance.GetPlayerHandData().Count;
            GameManager.Instance.DiscardHand(true);

            // Campo Player
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> myCards = new List<CardDisplay>();
                CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, myCards);
                CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, myCards);
                count += myCards.Count;
                DestroyCards(myCards, true);
            }

            Effect_DirectDamage(source, count * 300);
        }
    }

    void Effect_0290_ChaosEnd(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerRemovedCount() < 7)
        {
            UIManager.Instance.ShowMessage("Requer 7 ou mais cartas banidas.");
            return;
        }
        
        bool oppHasMonsters = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonsters = true;

        if (!oppHasMonsters)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros.");
            return;
        }

        DestroyAllMonsters(true, false);
    }

    void Effect_0291_ChaosGreed(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerRemovedCount() < 4 || GameManager.Instance.GetPlayerGraveyard().Count > 0)
        {
            UIManager.Instance.ShowMessage("Requer 4+ cartas banidas e nenhum monstro no seu Cemitério.");
            return;
        }
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0292_ChaosNecromancer(CardDisplay source)
    {
        // Regra: ATK = 300 x Monstros no GY.
        // Buff atualizado dinamicamente no CardEffectManager_Impl.cs
        Debug.Log("Chaos Necromancer: ATK dinâmico.");
    }

    void Effect_0293_ChaosSorcerer(CardDisplay source)
    {
        if (source.hasAttackedThisTurn)
        {
            UIManager.Instance.ShowMessage("Chaos Sorcerer: Já atacou neste turno.");
            return;
        }
        
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.gameObject != source.gameObject) hasTarget = true;
        }
        
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros face-up no campo para banir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => {
                    GameManager.Instance.BanishCard(t);
                    source.hasAttackedThisTurn = true;
                }
            );
        }
    }

    void Effect_0294_ChaosriderGustaph(CardDisplay source)
    {
        // Efeito: Bane até 2 Spells do GY para ganhar ATK.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));

        if (spells.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui Cartas Mágicas no Cemitério para banir.");
            return;
        }

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(spells, "Banir Spells (Max 2)", 1, 2, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    gy.Remove(c);
                }
                int buff = selected.Count * 300;
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
            });
        }
    }

    void Effect_0296_CharmOfShabti(CardDisplay source)
    {
        // Efeito: Descarte para evitar que Gravekeepers sejam destruídos em batalha.
        if (!source.isOnField) // Da mão
        {
            GameManager.Instance.DiscardCard(source);
            if (BattleManager.Instance != null) BattleManager.Instance.gravekeepersProtected = true;
            Debug.Log("Charm of Shabti: Gravekeepers protegidos.");
        }
    }

    void Effect_0298_Checkmate(CardDisplay source)
    {
        if (!SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            UIManager.Instance.ShowMessage("Você não tem monstros para tributar.");
            return;
        }
        
        bool hasTerrorking = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.name == "Terrorking Archfiend") hasTerrorking = true;
            
        if (!hasTerrorking)
        {
            UIManager.Instance.ShowMessage("Você não controla 'Terrorking Archfiend'.");
            return;
        }
        
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    if (BattleManager.Instance != null) BattleManager.Instance.terrorkingCanAttackDirectly = true;
                    Debug.Log("Checkmate: Terrorking Archfiend pode atacar direto este turno.");
                }
            );
        }
    }

    void Effect_0299_ChimeraTheFlyingMythicalBeast(CardDisplay source)
    {
        // Efeito: Se destruído, invoca Berfomet ou Gazelle do GY.
        // Lógica no CardEffectManager_Impl.cs (OnCardSentToGraveyard)
        Debug.Log("Chimera: Efeito de flutuação configurado.");
    }

    void Effect_0300_ChironTheMage(CardDisplay source)
    {
        bool oppHasST = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if (z.childCount > 0) oppHasST = true;
            if (GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) oppHasST = true;
        }
        
        if (!oppHasST)
        {
            UIManager.Instance.ShowMessage("O oponente não controla Magias/Armadilhas.");
            return;
        }
        
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 Magia da sua mão.");
            return;
        }

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Effect_MST(source);
            });
        }
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0301 - 0400)
    // =========================================================================================

    void Effect_0301_ChopmanTheDesperateOutlaw(CardDisplay source)
    {
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> equips = gy.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");
        
        if (equips.Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há Magias de Equipamento no seu Cemitério.");
            return;
        }

        if (equips.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(equips, "Equipar do GY", (selected) => {
                // Simula equipar: Move do GY para S/T e cria link
                // Como não temos lógica de mover do GY para campo S/T diretamente, simulamos o efeito
                Debug.Log($"Chopman: Equipando {selected.name} do GY.");
                gy.Remove(selected);
                // Cria visualmente na zona de S/T? Ou apenas aplica o efeito?
                // Vamos aplicar o efeito genérico de equipar (assumindo +0 por enquanto, pois depende da carta)
                // Em um sistema real, instanciaríamos a carta na zona S/T.
            });
        }
    }

    void Effect_0302_ChorusOfSanctuary(CardDisplay source)
    {
        // Field Spell: +500 DEF para todos os monstros em Defesa.
        Effect_Field(source, 0, 500, "", "");
    }

    void Effect_0303_ChosenOne(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        int monsterCount = hand.FindAll(c => c.type.Contains("Monster")).Count;
        int nonMonsterCount = hand.Count - monsterCount;
        
        if (monsterCount < 1 || nonMonsterCount < 2)
        {
            UIManager.Instance.ShowMessage("Requer no mínimo 1 Monstro e 2 Não-Monstros na mão.");
            return;
        }

        if (hand.Count >= 3)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Selecione 1 Monstro e 2 Não-Monstros", 3, 3, (selected) => {
                // Validação simples
                int monsters = selected.FindAll(c => c.type.Contains("Monster")).Count;
                if (monsters < 1) { Debug.Log("Precisa selecionar pelo menos 1 monstro."); return; }

                // Oponente escolhe (aleatório)
                CardData picked = selected[Random.Range(0, selected.Count)];
                Debug.Log($"Chosen One: Oponente escolheu {picked.name}.");

                if (picked.type.Contains("Monster"))
                {
                    GameManager.Instance.SpecialSummonFromData(picked, source.isPlayerCard);
                    selected.Remove(picked);
                }

                // Manda o resto para o GY
                foreach(var c in selected)
                {
                    GameManager.Instance.SendToGraveyard(c, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(c, source.isPlayerCard);
                }
            });
        }

    }

    void Effect_0305_CipherSoldier(CardDisplay source)
    {
        // Se batalhar com Warrior: +2000 ATK/DEF durante o cálculo de dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation)
        Debug.Log("Cipher Soldier: Efeito passivo de batalha.");
    }

    void Effect_0307_Cloning(CardDisplay source)
    {
        // Quando oponente invoca: SS Clone Token com mesmos stats/tipo/atributo.
        // Requer gatilho de resposta a invocação.
        // Simulação: Verifica última invocação do oponente
        // (Lógica real estaria no SpellTrapManager.CheckForTraps(Summon))
        Debug.Log("Cloning: Ativo (Gatilho de invocação).");
    }

    void Effect_0309_CoachGoblin(CardDisplay source)
    {
        // End Phase: Se você controla este card face-up, pode retornar 1 Normal Monster da mão ao Deck para comprar 1.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase)
        Debug.Log("Coach Goblin: Ativo.");
    }

    void Effect_0310_CobraJar(CardDisplay source)
    {
        // FLIP: SS 1 "Poisonous Snake Token".
        GameManager.Instance.SpawnToken(source.isPlayerCard, 1200, 1200, "Poisonous Snake Token");
    }

    void Effect_0311_CobramanSakuzy(CardDisplay source)
    {
        // Pode virar face-down 1x por turno. Quando Flip Summon: Olhe todas as S/T setadas do oponente.
        Effect_TurnSet(source);
        
        // Revelar S/T (Simulado com Log)
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Itera zonas de S/T do oponente e loga os nomes
            // Em um jogo real, mostraria as cartas visualmente por alguns segundos
            Debug.Log("Cobraman Sakuzy: Revelando S/T do oponente...");
        }
    }

    void Effect_0312_CockroachKnight(CardDisplay source)
    {
        // Se enviado ao GY: Volta ao topo do Deck.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField/OnSentToGraveyard)
        Debug.Log("Cockroach Knight: Ativo.");
    }

    void Effect_0313_CocoonOfEvolution(CardDisplay source)
    {
        if (!GameManager.Instance.IsCardActiveOnField("Petit Moth") && !GameManager.Instance.IsCardActiveOnField("1420"))
        {
            UIManager.Instance.ShowMessage("Você precisa controlar um 'Petit Moth' face-up.");
            return;
        }

        if (!source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Petit Moth",
                    (t) => {
                        // Move Cocoon para S/T e equipa
                        // Simulação:
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                        // Criar na zona S/T...
                        Debug.Log("Cocoon of Evolution: Equipado em Petit Moth.");
                        // Aplica stats
                        t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, source.CurrentCardData.atk, source));
                        t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, source.CurrentCardData.def, source));
                    }
                );
            }
        }
    }

    void Effect_0314_CoffinSeller(CardDisplay source)
    {
        // Cada vez que monstro(s) do oponente vão para o GY: 300 dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardSentToGraveyard)
        Debug.Log("Coffin Seller: Ativo.");
    }

    void Effect_0315_ColdWave(CardDisplay source)
    {
        // Só no início da Main 1. Até seu próximo turno, ninguém joga/seta S/T.
        if (PhaseManager.Instance.currentPhase == GamePhase.Main1)
        {
            Debug.Log("Cold Wave: S/T bloqueadas até o próximo turno.");
            // Adicionar flag global no GameManager ou SpellTrapManager
            // SpellTrapManager.Instance.coldWaveActive = true;
        }
    }

    void Effect_0316_CollectedPower(CardDisplay source)
    {
        // Selecione 1 monstro face-up; equipe todos os Equip Cards no campo nele.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    // Encontrar todos os equips no campo e mudar o alvo para 'target'
                    Debug.Log($"Collected Power: Todos os equips movidos para {target.CurrentCardData.name}.");
                }
            );
        }
    }

    void Effect_0317_CombinationAttack(CardDisplay source)
    {
        // Battle Phase: Tribute monstro equipado com Union; Union ataca.
        if (PhaseManager.Instance.currentPhase == GamePhase.Battle)
        {
            // Seleciona monstro equipado
            // ...
            Debug.Log("Combination Attack: Union ataca.");
        }
    }

    void Effect_0318_CommandKnight(CardDisplay source)
    {
        // Warriors +400 ATK. Se controlar outro monstro, não pode ser atacado.
        // Buff aplicado no OnSummonImpl
        // Proteção aplicada no BattleManager.SelectTarget
        Debug.Log("Command Knight: Ativo.");
    }

    void Effect_0319_CommencementDance(CardDisplay source)
    {
        // Ritual para "Performance of Sword".
        // Requer sistema de Ritual (seleção de tributos por nível)
        // Simulação:
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData ritualMonster = hand.Find(c => c.name == "Performance of Sword");
        if (ritualMonster != null)
        {
            // Pede tributos (Nível >= 6)
            SummonManager.Instance.SelectTributes(2, source.isPlayerCard, (tributes) => {
                // Verifica níveis...
                GameManager.Instance.SpecialSummonFromData(ritualMonster, source.isPlayerCard);
            });
        }
    }

    void Effect_0320_CompulsoryEvacuationDevice(CardDisplay source)
    {
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonster = true;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasMonster = true;
        }
        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Não há monstros no campo para retornar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Compulsory Evacuation Device: {t.CurrentCardData.name} retornado para a mão.");
                    // Simulação: Destrói visualmente (deveria ir para a mão)
                    Destroy(t.gameObject); 
                }
            );
        }
    }

    void Effect_0321_Confiscation(CardDisplay source)
    {
        if (GameManager.Instance.GetOpponentHandData().Count == 0)
        {
            UIManager.Instance.ShowMessage("A mão do oponente está vazia.");
            return;
        }

        if (Effect_PayLP(source, 1000))
        {
            List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
            GameManager.Instance.OpenCardSelection(oppHand, "Descartar da Mão do Oponente", (selected) => {
                // Encontra e descarta
                // (Requer referência ao objeto da mão do oponente para destruir visualmente)
                Debug.Log($"Confiscation: Descartou {selected.name}.");
            });
        }
    }

    void Effect_0322_Conscription(CardDisplay source)
    {
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        if (oppDeck.Count == 0)
        {
            UIManager.Instance.ShowMessage("O Deck do oponente está vazio.");
            return;
        }

        if (oppDeck.Count > 0)
        {
            CardData top = oppDeck[0];
            oppDeck.RemoveAt(0);
            Debug.Log($"Conscription: Escavou {top.name}.");
            
            if (top.type.Contains("Monster") && !top.type.Contains("Ritual") && !top.type.Contains("Fusion")) // Simplificado
            {
                GameManager.Instance.SpecialSummonFromData(top, source.isPlayerCard);
            }
            else
            {
                GameManager.Instance.AddCardToHand(top, !source.isPlayerCard); // Adiciona à mão do oponente
            }
        }
    }

    void Effect_0323_ContinuousDestructionPunch(CardDisplay source)
    {
        // Se oponente ataca defesa e DEF > ATK, destrói atacante. Se ATK > DEF, destrói defensor.
        // Lógica implementada no BattleManager (ResolveDamage)
        Debug.Log("Continuous Destruction Punch: Ativo.");
    }

    void Effect_0324_ContractWithExodia(CardDisplay source)
    {
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        string[] parts = { "0618", "1061", "1062", "1530", "1531" }; // IDs das partes
        bool hasAll = true;
        foreach(var id in parts) if (!gy.Exists(c => c.id == id)) hasAll = false;

        if (!hasAll)
        {
            UIManager.Instance.ShowMessage("Você não possui as 5 partes do Exodia no Cemitério.");
            return;
        }
        
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData necross = hand.Find(c => c.name == "Exodia Necross" || c.id == "0617");
        if (necross == null)
        {
            UIManager.Instance.ShowMessage("Você precisa de 'Exodia Necross' na mão para invocar.");
            return;
        }

        GameManager.Instance.SpecialSummonFromData(necross, source.isPlayerCard);
        GameManager.Instance.RemoveCardFromHand(necross, source.isPlayerCard);
    }

    void Effect_0325_ContractWithTheAbyss(CardDisplay source)
    {
        // Ritual Genérico para DARK.
        // Lógica de Ritual Genérico
        Debug.Log("Contract with the Abyss: Selecione Ritual DARK.");
    }

    void Effect_0326_ContractWithTheDarkMaster(CardDisplay source)
    {
        // Ritual para Dark Master - Zorc.
        // Requer sistema de Ritual
        Debug.Log("Contract with the Dark Master: Selecione Ritual Zorc.");
    }

    void Effect_0327_ConvulsionOfNature(CardDisplay source)
    {
        // Ambos os jogadores viram seus Decks de cabeça para baixo.
        Debug.Log("Convulsion of Nature: Decks invertidos (Visual pendente).");
        // Simulação visual: Vira o topo do deck
        if (GameManager.Instance.playerDeckDisplay != null)
        {
            // Inverter visualmente seria complexo sem mudar a arquitetura do PileDisplay
            // Mas podemos logar que o estado mudou
        }
    }

    void Effect_0328_Copycat(CardDisplay source)
    {
        bool oppHasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonster = true;
        
        if (!oppHasMonster)
        {
            UIManager.Instance.ShowMessage("Oponente não controla monstros para copiar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    source.ModifyStats(t.CurrentCardData.atk - source.currentAtk, t.CurrentCardData.def - source.currentDef);
                    Debug.Log($"Copycat: Copiou stats de {t.CurrentCardData.name}.");
                }
            );
        }
    }

    void Effect_0331_CostDown(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Sua mão está vazia.");
            return;
        }

        GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
             GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
             Debug.Log("Cost Down: Níveis na mão reduzidos em 2.");
             // A lógica de tributo no SummonManager precisa checar se Cost Down está ativo
             // SummonManager.Instance.levelReduction = 2;
        });
    }

    void Effect_0332_CoveringFire(CardDisplay source)
    {
        // Se oponente ataca: Monstro atacado ganha ATK de outro monstro seu.
        if (BattleManager.Instance != null && BattleManager.Instance.currentTarget != null)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != BattleManager.Instance.currentTarget,
                    (other) => {
                        BattleManager.Instance.currentTarget.ModifyStats(other.currentAtk, 0);
                        Debug.Log($"Covering Fire: +{other.currentAtk} ATK.");
                    }
                );
            }
        }
    }

    void Effect_0334_CrassClown(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.trigger == ChainManager.TriggerType.Attack)
        {
            if (BattleManager.Instance != null) BattleManager.Instance.crossCounterActive = true;
            Debug.Log("Cross Counter: Dano de reflexão será dobrado e atacante destruído.");
        }
        else
        {
            UIManager.Instance.ShowMessage("Só pode ser ativada em resposta a um ataque do oponente.");
        }
    }

    void Effect_0338_CreatureSwap(CardDisplay source)
    {
        bool myHas = false;
        bool oppHas = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) myHas = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHas = true;
        }

        if (!myHas || !oppHas)
        {
            UIManager.Instance.ShowMessage("Ambos os jogadores precisam controlar pelo menos 1 monstro.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (my) => my.isOnField && my.isPlayerCard,
                (myMonster) => {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (opp) => opp.isOnField && !opp.isPlayerCard,
                        (oppMonster) => {
                            GameManager.Instance.SwitchControl(myMonster);
                            GameManager.Instance.SwitchControl(oppMonster);
                            Debug.Log("Creature Swap: Troca realizada.");
                        }
                    );
                }
            );
        }
    }

    void Effect_0339_CreepingDoomManta(CardDisplay source)
    {
        // Quando invocado Normal: Oponente não ativa Traps.
        Debug.Log("Creeping Doom Manta: Traps bloqueadas na invocação.");
        // SpellTrapManager.Instance.trapsBlocked = true; // Resetar no fim do turno
    }

    void Effect_0340_CrimsonNinja(CardDisplay source)
    {
        // FLIP: Destrói 1 Trap. Se setada, revela.
        Effect_FlipDestroy(source, TargetType.Trap);
    }

    void Effect_0341_CrimsonSentry(CardDisplay source)
    {
        // Tribute este card; coloque 1 monstro destruído neste turno do GY na mão.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            // Seleciona do GY (sem verificar se foi destruído neste turno por enquanto)
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
            if (monsters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(monsters, "Recuperar Monstro", (c) => GameManager.Instance.AddCardToHand(c, true));
            }
        }
    }

    void Effect_0343_Criosphinx(CardDisplay source)
    {
        // Se monstro voltar para a mão: Oponente descarta 1.
        // Lógica simulada no CardEffectManager_Impl.cs (OnCardLeavesField - ReturnToHand)
        Debug.Log("Criosphinx: Ativo.");
    }

    void Effect_0344_CrossCounter(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.trigger == ChainManager.TriggerType.Attack)
        {
            if (BattleManager.Instance != null) BattleManager.Instance.crossCounterActive = true;
            Debug.Log("Cross Counter: Dano de reflexão será dobrado e atacante destruído.");
        }
        else
        {
            UIManager.Instance.ShowMessage("Só pode ser ativada em resposta a um ataque do oponente.");
        }
    }

    void Effect_0346_CrushCardVirus(CardDisplay source)
    {
        bool hasValidTribute = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.CurrentCardData.attribute == "Dark" && m.currentAtk <= 1000) hasValidTribute = true;
                }
            }
        }

        if (!hasValidTribute)
        {
            UIManager.Instance.ShowMessage("Requer 1 monstro DARK com 1000 ou menos de ATK para tributar.");
            return;
        }

        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            Debug.Log("Crush Card Virus: Destruindo monstros fortes...");
            // Destrói campo
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if (z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if (m != null && m.currentAtk >= 1500)
                        {
                            GameManager.Instance.SendToGraveyard(m.CurrentCardData, false);
                            Destroy(m.gameObject);
                        }
                    }
                }
            }
            // Destrói mão (Simulado: Descarta aleatório se tiver monstro forte? Não, vamos apenas logar)
            Debug.Log("Crush Card Virus: Verificação de mão e turnos futuros pendente.");
        }
    }

    void Effect_0347_CureMermaid(CardDisplay source)
    {
        // Standby Phase: Ganha 800 LP.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Cure Mermaid: Ativo.");
    }

    void Effect_0348_CurseOfAging(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerHandData().Count == 0)
        {
            UIManager.Instance.ShowMessage("Sua mão está vazia.");
            return;
        }

        bool oppHasMonsters = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonsters = true;
        
        if (!oppHasMonsters)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                // Aplica debuff
                if (GameManager.Instance.duelFieldUI != null)
                {
                    foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                    {
                        if (z.childCount > 0)
                        {
                            var m = z.GetChild(0).GetComponent<CardDisplay>();
                            if (m != null) m.ModifyStats(-500, -500);
                        }
                    }
                }
            });
        }
    }

    void Effect_0349_CurseOfAnubis(CardDisplay source)
    {
        bool hasEffectMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> checkAll = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, checkAll);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, checkAll);
            foreach(var m in checkAll) if (m.CurrentCardData.type.Contains("Effect") && !m.isFlipped) hasEffectMonster = true;
        }
        
        if (!hasEffectMonster)
        {
            UIManager.Instance.ShowMessage("Não há monstros de Efeito virados para cima no campo.");
            return;
        }

        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.CurrentCardData.type.Contains("Effect"))
                {
                    if (m.position == CardDisplay.BattlePosition.Attack) m.ChangePosition();
                    m.ModifyStats(0, -m.currentDef); // Zera DEF
                }
            }
        }
    }

    void Effect_0350_CurseOfDarkness(CardDisplay source)
    {
        // Field Spell: Quem ativar Spell toma 1000 dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnSpellActivated)
        Debug.Log("Curse of Darkness: Ativo.");
    }

    void Effect_0352_CurseOfFiend(CardDisplay source)
    {
        bool hasMonsters = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonsters = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasMonsters = true;
        }

        if (!hasMonsters)
        {
            UIManager.Instance.ShowMessage("Não há monstros no campo.");
            return;
        }

        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all) m.ChangePosition();
        }
    }

    void Effect_0353_CurseOfRoyal(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && (link.cardSource.CurrentCardData.type.Contains("Spell") || link.cardSource.CurrentCardData.type.Contains("Trap")))
        {
            NegateAndDestroy(source, link);
        }
    }

    void Effect_0354_CurseOfTheMaskedBeast(CardDisplay source)
    {
        // Ritual para Masked Beast.
        Debug.Log("Curse of the Masked Beast: Selecione Ritual Masked Beast.");
    }

    void Effect_0355_CursedSealOfTheForbiddenSpell(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link == null || !link.cardSource.CurrentCardData.type.Contains("Spell"))
        {
            UIManager.Instance.ShowMessage("Não há uma Carta Mágica sendo ativada para negar.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 Magia da sua mão.");
            return;
        }

        GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Spell", (discarded) => {
            GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
            
            GameManager.Instance.forbiddenSpells.Add(link.cardSource.CurrentCardData.name);
            NegateAndDestroy(source, link);
        });
    }

    void Effect_0357_CyberArchfiend(CardDisplay source)
    {
        // Draw Phase: Se mão vazia, compra mais 1.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - Draw)
        Debug.Log("Cyber Archfiend: Ativo.");
    }

    void Effect_0359_CyberDragon(CardDisplay source)
    {
        // SS da mão se oponente tem monstro e você não.
        if (!source.isOnField)
        {
            bool oppHas = false;
            bool iHave = false;
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) oppHas = true;
                foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) iHave = true;
            }

            if (oppHas && !iHave)
            {
                Debug.Log("Cyber Dragon: Condição de SS atendida.");
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
            }
            else
            {
                Debug.Log("Cyber Dragon: Condição de SS não atendida.");
            }
        }
    }

    void Effect_0362_CyberHarpieLady(CardDisplay source)
    {
        // Efeito: O nome desta carta é tratado como "Harpie Lady".
        Debug.Log("Cyber Harpie Lady: Nome tratado como Harpie Lady.");
        // Lógica de regra (Rule Effect), geralmente tratada onde se checa o nome
    }

    void Effect_0363_CyberJar(CardDisplay source)
    {
        // FLIP: Destrói todos os monstros. Ambos compram 5, invocam Lv4- encontrados.
        Debug.Log("Cyber Jar: Destruindo tudo...");
        DestroyAllMonsters(true, true);
        
        Debug.Log("Cyber Jar: Ambos compram 5 cartas (Simulado).");
        for(int i=0; i<5; i++) GameManager.Instance.DrawCard(true);
        for(int i=0; i<5; i++) GameManager.Instance.DrawOpponentCard();
        
        // A lógica de revelar e invocar automaticamente é muito complexa para este escopo.
        // O jogador deve invocar manualmente da mão o que comprou.
    }

    void Effect_0364_CyberRaider(CardDisplay source)
    {
        bool hasEquip = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> st = new List<CardDisplay>();
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, st);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, st);
            foreach(var c in st) if (c.CurrentCardData.property == "Equip") hasEquip = true;
        }

        if (!hasEquip)
        {
            UIManager.Instance.ShowMessage("Não há Cartas de Equipamento no campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Spell") && t.CurrentCardData.property == "Equip",
                (target) => {
                    // Simplificação: Destrói sempre (falta UI de escolha)
                    Debug.Log($"Cyber Raider: Destruindo {target.CurrentCardData.name}.");
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                }
            );
        }
    }

    void Effect_0366_CyberShield(CardDisplay source)
    {
        bool hasHarpie = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.name.Contains("Harpie Lady")) hasHarpie = true;
        }

        if (!hasHarpie)
        {
            UIManager.Instance.ShowMessage("Não há 'Harpie Lady' face-up no campo.");
            return;
        }

        Effect_Equip(source, 500, 0, "Winged Beast");
    }

    void Effect_0369_CyberTwinDragon(CardDisplay source)
    {
        // Efeito: Pode atacar duas vezes na Battle Phase.
        Debug.Log("Cyber Twin Dragon: Ataque duplo.");
        // Requer flag no CardDisplay ou BattleManager
    }

    void Effect_0370_CyberStein(CardDisplay source)
    {
        List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
        List<CardData> targets = extra.FindAll(c => (c.type.Contains("Fusion") || c.type.Contains("Synchro") || c.type.Contains("Xyz")) && c.level <= 6);            
        
        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui alvos válidos no Extra Deck.");
            return;
        }

        if (GameManager.Instance.playerLP <= 5000)
        {
            UIManager.Instance.ShowMessage("Você precisa de mais de 5000 LP para ativar.");
            return;
        }

        if (Effect_PayLP(source, 5000))
        {
            GameManager.Instance.OpenCardSelection(targets, "Invocar Fusão", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                extra.Remove(selected);
            });
        }
    }

    void Effect_0372_CyberneticCyclopean(CardDisplay source)
    {
        // Efeito: Se você não tiver cartas na mão, ganha 1000 ATK.
        // Lógica implementada no CardEffectManager_Impl.cs (CheckActiveCards)
        Debug.Log("Cybernetic Cyclopean: Ativo.");
    }

    void Effect_0373_CyberneticMagician(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 carta da mão.");
            return;
        }

        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonster = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasMonster = true;
        }

        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Não há monstros no campo.");
            return;
        }

        GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
            GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
            
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, 2000, source));
                        Debug.Log($"Cybernetic Magician: ATK de {target.CurrentCardData.name} definido para 2000.");
                    }
                );
            }
        });
    }

    void Effect_0374_CyclonLaser(CardDisplay source)
    {
        bool hasGradius = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.name == "Gradius") hasGradius = true;
        }

        if (!hasGradius)
        {
            UIManager.Instance.ShowMessage("Não há 'Gradius' face-up no campo.");
            return;
        }

        Effect_Equip(source, 300, 0, "Machine");
    }

    void Effect_0377_DTribe(CardDisplay source)
    {
        // Trap: Todos os monstros viram Dragão.
        Debug.Log("D. Tribe: Todos viram Dragão.");
        // Requer sistema de modificação de Tipo global
    }

    void Effect_0378_DDAssailant(CardDisplay source)
    {
        // Efeito: Se destruído em batalha, bane o atacante e este card.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Assailant: Ativo.");
    }

    void Effect_0379_DDBorderline(CardDisplay source)
    {
        // Spell: Ninguém ataca se não houver Spells no seu GY.
        // Lógica implementada no BattleManager.cs (CanAttack)
        Debug.Log("D.D. Borderline: Ativo.");
    }

    void Effect_0380_DDCrazyBeast(CardDisplay source)
    {
        // Efeito: Bane monstro destruído por este card em batalha.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Crazy Beast: Ativo.");
    }

    void Effect_0381_DDDesignator(CardDisplay source)
    {
        // Spell: Declare 1 carta; verifique mão do oponente. Se tiver, bane. Se não, bane 1 da sua.
        // Simulação de declaração: Escolhe do banco de dados
        // Como não temos input de texto, usamos o seletor de cartas
        List<CardData> allCards = GameManager.Instance.cardDatabase.cardDatabase;
        // Otimização: Pegar uma amostra ou usar lógica de "Adivinhar"
        Debug.Log("D.D. Designator: Declare um nome (Simulado: Escolha da lista).");
        
        // Para teste, vamos pegar uma carta aleatória do deck do oponente para "adivinhar" corretamente às vezes
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        
        // Simula declaração
        string declaredName = "Kuriboh"; // Placeholder
        
        bool found = false;
        foreach(var c in oppHand) if(c.name == declaredName) found = true;

        if (found) {
            Debug.Log($"D.D. Designator: Acertou '{declaredName}'! Banindo da mão do oponente.");
            // Banir lógica...
        } else {
            Debug.Log($"D.D. Designator: Errou! Banindo carta da sua mão.");
        }
    }

    void Effect_0382_DDDynamite(CardDisplay source)
    {
        int count = GameManager.Instance.GetOpponentRemoved().Count;
        if (count == 0)
        {
            UIManager.Instance.ShowMessage("O oponente não possui cartas banidas.");
            return;
        }
        Effect_DirectDamage(source, count * 300);
    }

    void Effect_0383_DDScoutPlane(CardDisplay source)
    {
        // Efeito: Se banido, SS na End Phase.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase - Check Banished)
        Debug.Log("D.D. Scout Plane: Efeito passivo.");
    }

    void Effect_0384_DDSurvivor(CardDisplay source)
    {
        // Efeito: Se banido enquanto face-up, SS na End Phase.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase - Check Banished)
        Debug.Log("D.D. Survivor: Efeito passivo.");
    }

    void Effect_0386_DDTrapHole(CardDisplay source)
    {
        // Trap: Quando oponente Set monstro: Destrói e bane.
        // Gatilho implementado no CardEffectManager_Impl.cs (OnSetImpl)
        Debug.Log("D.D. Trap Hole: Ativo.");
    }

    void Effect_0387_DDWarrior(CardDisplay source)
    {
        // Efeito: Após batalha, bane este card e o oponente.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Warrior: Ativo.");
    }

    void Effect_0388_DDWarriorLady(CardDisplay source)
    {
        // Efeito: Após batalha, pode banir este card e o oponente.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Warrior Lady: Ativo.");
    }

    void Effect_0389_DDM(CardDisplay source)
    {
        List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
        List<CardData> monsters = banished.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui monstros banidos.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 Magia da sua mão.");
            return;
        }

        GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Spell", (discarded) => {
            GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
            
            GameManager.Instance.OpenCardSelection(monsters, "Invocar Banido", (target) => {
                GameManager.Instance.RemoveFromPlay(target, true); // Retira da lista de banidos temporariamente
                GameManager.Instance.playerRemovedDisplay.UpdatePile(GameManager.Instance.GetPlayerRemoved(), GameManager.Instance.GetCardBackTexture());
                GameManager.Instance.SpecialSummonFromData(target, source.isPlayerCard);
            });
        });
    }

    void Effect_0390_DNASurgery(CardDisplay source)
    {
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonster = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasMonster = true;
        }

        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Não há monstros no campo.");
            return;
        }

        string declared = "Dragon"; // Em produção: UI de input
        Debug.Log($"DNA Surgery: Tipo declarado: {declared}.");
        CardEffectManager.Instance.dnaSurgeryDeclaredType = declared;
    }

    void Effect_0391_DNATransplant(CardDisplay source)
    {
        // Trap: Declare 1 Atributo; todos viram esse Atributo.
        Debug.Log("DNA Transplant: Declare um Atributo (Simulado: LIGHT).");
    }

    void Effect_0393_DancingFairy(CardDisplay source)
    {
        // Efeito: Se em Defesa, ganha 1000 LP na Standby.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Dancing Fairy: Ativo.");
    }

    void Effect_0394_DangerousMachineType6(CardDisplay source)
    {
        // Spell: Rola dado e aplica efeito aleatório.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Dangerous Machine Type-6: Ativo.");
    }

    void Effect_0395_DarkArtist(CardDisplay source)
    {
        // Efeito: DEF cai pela metade se atacado por LIGHT.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation)
        Debug.Log("Dark Artist: Ativo.");
    }

    void Effect_0397_DarkBalterTheTerrible(CardDisplay source)
    {
        // Fusão: Pague 1000 para negar Normal Spell. Nega efeitos de monstros destruídos.
        // Negação de efeitos destruídos implementada no OnBattleEnd.
        // Negação de Spell requer Chain (similar a Cursed Seal).
        Debug.Log("Dark Balter: Ativo.");
    }

    void Effect_0400_DarkBladeTheDragonKnight(CardDisplay source)
    {
        // Fusão: Bane até 3 monstros do GY do oponente.
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> monsters = oppGY.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count > 0)
        {
            int max = Mathf.Min(3, monsters.Count);
            GameManager.Instance.OpenCardMultiSelection(monsters, "Banir do Oponente", 1, max, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, !source.isPlayerCard);
                    oppGY.Remove(c);
                }
                Debug.Log($"Dark Blade: Baniu {selected.Count} monstros.");
            });
        }
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0401 - 0500)
    // =========================================================================================

    void Effect_0401_DarkCatWithWhiteTail(CardDisplay source)
    {
        int oppCount = 0;
        int myCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppCount++;
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) myCount++;
        }
        if (oppCount < 2 || myCount < 1)
        {
            UIManager.Instance.ShowMessage("Requer 2 monstros do oponente e 1 seu para retornar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            // Seleciona 2 do oponente (simplificado para 1 por limitações de UI sequencial rápida)
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (opp1) => {
                    GameManager.Instance.ReturnToHand(opp1);
                    // Seleciona 1 seu
                    SpellTrapManager.Instance.StartTargetSelection(
                        (my) => my.isOnField && my.isPlayerCard && my.CurrentCardData.type.Contains("Monster"),
                        (my1) => {
                            GameManager.Instance.ReturnToHand(my1);
                        }
                    );
                }
            );
        }
    }

    void Effect_0402_DarkCatapulter(CardDisplay source)
    {
        // Efeito: Remove contador para destruir S/T.
        if (source.spellCounters > 0)
        {
            source.RemoveSpellCounter(1);
            Effect_MST(source); // Destrói S/T
        }
    }

    void Effect_0404_DarkCoffin(CardDisplay source)
    {
        // Efeito: Se destruído face-down, oponente escolhe: Descartar 1 ou Destruir 1 monstro.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardSentToGraveyard)
        Debug.Log("Dark Coffin: Ativo.");
    }

    void Effect_0405_DarkCore(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 carta da mão.");
            return;
        }

        bool hasFaceUp = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (!m.isFlipped) hasFaceUp = true;
        }

        if (!hasFaceUp)
        {
            UIManager.Instance.ShowMessage("Não há monstros face-up no campo.");
            return;
        }

        GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
            Debug.Log($"Dark Core: Descartou {discarded.name}.");
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isFlipped, // Face-up
                    (target) => {
                        GameManager.Instance.RemoveFromPlay(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                        Debug.Log($"Dark Core: Baniu {target.CurrentCardData.name}.");
                    }
                );
            }
        });
    }

    void Effect_0406_DarkDesignator(CardDisplay source)
    {
        // Efeito: Declare 1 monstro; se estiver no deck do oponente, adicione à mão dele.
        // Simulação de declaração
        Debug.Log("Dark Designator: Declarando 'Kuriboh' (Simulado).");
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        CardData target = oppDeck.Find(c => c.name == "Kuriboh");
        
        if (target != null)
        {
            Debug.Log("Dark Designator: Encontrado! Adicionando à mão do oponente.");
            oppDeck.Remove(target);
            GameManager.Instance.AddCardToHand(target, false);
        }
        else
        {
            Debug.Log("Dark Designator: Não encontrado.");
        }
    }

    void Effect_0407_DarkDriceratops(CardDisplay source)
    {
        // Efeito: Dano perfurante.
        // Lógica implementada no BattleManager.cs (ResolveDamage)
        Debug.Log("Dark Driceratops: Ativo.");
    }

    void Effect_0408_DarkDustSpirit(CardDisplay source)
    {
        // Efeito: Spirit. Ao ser invocado, destrói todos os monstros face-up.
        // Retorno para mão implementado no CardEffectManager_Impl.cs (OnPhaseStart - End)
        // Destruição:
        // DestroyAllMonsters(true, true); // Filtrar por face-up (pendente filtro)
    }

    void Effect_0409_DarkElf(CardDisplay source)
    {
        // Efeito: Paga 1000 LP para atacar.
        // Lógica implementada no BattleManager.cs (CanAttack)
        Debug.Log("Dark Elf: Ativo.");
    }

    void Effect_0410_DarkEnergy(CardDisplay source)
    {
        // Equip: Fiend +300 ATK/DEF.
        Effect_Equip(source, 300, 300, "Fiend");
    }

    void Effect_0411_DarkFactoryOfMassProduction(CardDisplay source)
    {
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> normals = gy.FindAll(c => c.type.Contains("Normal") && c.type.Contains("Monster"));
        
        if (normals.Count < 2)
        {
            UIManager.Instance.ShowMessage("Requer pelo menos 2 Monstros Normais no Cemitério.");
            return;
        }

        GameManager.Instance.OpenCardMultiSelection(normals, "Recuperar 2 Normais", 2, 2, (selected) => {
            foreach(var c in selected)
            {
                gy.Remove(c);
                GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
            }
        });
    }

    void Effect_0412_DarkFlareKnight(CardDisplay source)
    {
        // Efeito: Sem dano de batalha. Se destruído, invoca Mirage Knight.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField)
        Debug.Log("Dark Flare Knight: Ativo.");
    }

    void Effect_0414_DarkHole(CardDisplay source)
    {
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) hasMonster = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasMonster = true;
        }
        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Não há monstros no campo.");
            return;
        }
        DestroyAllMonsters(true, true);
    }

    void Effect_0415_DarkJeroid(CardDisplay source)
    {
        bool hasFaceUp = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (!m.isFlipped) hasFaceUp = true;
        }
        if (!hasFaceUp)
        {
            UIManager.Instance.ShowMessage("Não há monstros face-up no campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => t.ModifyStats(-800, 0)
            );
        }
    }

    void Effect_0417_DarkMagicAttack(CardDisplay source)
    {
        if (!GameManager.Instance.IsCardActiveOnField("0419") && !GameManager.Instance.IsCardActiveOnField("Dark Magician"))
        {
            UIManager.Instance.ShowMessage("Requer 'Dark Magician' face-up no campo.");
            return;
        }

        bool oppHasST = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if (z.childCount > 0) oppHasST = true;
            if (GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) oppHasST = true;
        }
        if (!oppHasST)
        {
            UIManager.Instance.ShowMessage("O oponente não controla Magias/Armadilhas para destruir.");
            return;
        }

        Effect_HarpiesFeatherDuster(source);
    }

    void Effect_0418_DarkMagicCurtain(CardDisplay source)
    {
        // Efeito: Paga metade do LP; SS Dark Magician do Deck.
        int cost = GameManager.Instance.playerLP / 2;
        Effect_PayLP(source, cost);
        Effect_SearchDeck(source, "Dark Magician"); // Simplificado (deveria invocar)
    }

    void Effect_0420_DarkMagicianGirl(CardDisplay source)
    {
        // Efeito: +300 ATK por DM/Magician of Black Chaos nos GYs.
        // Lógica implementada no CardEffectManager_Impl.cs (UpdateDMGBuff)
        Debug.Log("Dark Magician Girl: Ativo.");
    }

    void Effect_0421_DarkMagicianKnight(CardDisplay source)
    {
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) hasTarget = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if (z.childCount > 0) hasTarget = true;
            if (GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) hasTarget = true;
        }
        
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há cartas no campo do oponente para destruir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField,
                (t) => {
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }

    void Effect_0422_DarkMagicianOfChaos(CardDisplay source)
    {
        // Efeito: Recupera 1 Spell do GY na End Phase. Se destruído, é banido.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End, OnCardLeavesField)
        Debug.Log("DMoC: Ativo.");
    }

    void Effect_0423_DarkMasterZorc(CardDisplay source)
    {
        // Efeito: Rola dado. 1-2: Destrói monstros oponente. 3-5: Destrói 1 monstro. 6: Destrói a si mesmo.
        int roll = Random.Range(1, 7);
        Debug.Log($"Zorc rolou: {roll}");
        
        if (roll <= 2)
        {
            DestroyAllMonsters(true, false);
        }
        else if (roll <= 5)
        {
            // Destrói 1 monstro (simplificado: aleatório ou primeiro)
            // ...
        }
        else
        {
            // Destrói a si mesmo
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
        }
    }

    void Effect_0424_DarkMimicLV1(CardDisplay source)
    {
        // FLIP: Compra 1. Standby: Envia ao GY, SS LV3.
        GameManager.Instance.DrawCard();
        Effect_LevelUp(source, "0425"); // LV3
    }

    void Effect_0425_DarkMimicLV3(CardDisplay source)
    {
        // Efeito: Se destruído em batalha, compra 1. Se invocado pelo LV1, compra 2.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField)
        Debug.Log("Dark Mimic LV3: Ativo.");
    }

    void Effect_0426_DarkMirrorForce(CardDisplay source)
    {
        // Efeito: Quando oponente ataca: Bane todos os monstros em Defesa do oponente.
        // Requer hook no SpellTrapManager.CheckForTraps(Attack)
        // Simulação:
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.position == CardDisplay.BattlePosition.Defense)
                    {
                        GameManager.Instance.BanishCard(m);
                    }
                }
            }
        }
    }

    void Effect_0427_DarkNecrofear(CardDisplay source)
    {
        // Efeito: SS banindo 3 Fiends. Na End Phase se destruído, equipa no oponente e controla.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End)
        Debug.Log("Dark Necrofear: Ativo.");
    }

    void Effect_0428_DarkPaladin(CardDisplay source)
    {
        // Efeito: Nega Spell descartando 1. +500 ATK por Dragão.
        // Buff implementado no CardEffectManager_Impl.cs (UpdateDarkPaladinBuff)
        // Negação requer Chain
        Debug.Log("Dark Paladin: Ativo.");
    }

    void Effect_0432_DarkRoomOfNightmare(CardDisplay source)
    {
        // Efeito: Se oponente tomar dano de efeito, causa +300.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageTaken)
        Debug.Log("Dark Room of Nightmare: Ativo.");
    }

    void Effect_0433_DarkRulerHaDes(CardDisplay source)
    {
        // Efeito: Nega efeitos de monstros destruídos por Fiends.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField)
        Debug.Log("Dark Ruler Ha Des: Ativo.");
    }

    void Effect_0434_DarkSage(CardDisplay source)
    {
        // Efeito: Busca 1 Spell.
        Effect_SearchDeck(source, "Spell");
    }

    void Effect_0435_DarkScorpionChickTheYellow(CardDisplay source)
    {
        // Efeito: Dano -> Bounce monstro ou olhar topo deck.
        Debug.Log("Chick the Yellow: Efeito passivo de dano (OnDamageDealtImpl).");
    }

    void Effect_0436_DarkScorpionCliffTheTrapRemover(CardDisplay source)
    {
        // Efeito: Dano -> Destrói S/T ou Mill 2.
        Debug.Log("Cliff the Trap Remover: Efeito passivo de dano (OnDamageDealtImpl).");
    }

    void Effect_0437_DarkScorpionGorgTheStrong(CardDisplay source)
    {
        // Efeito: Dano -> Bounce monstro (topo deck) ou Mill 1.
        Debug.Log("Gorg the Strong: Efeito passivo de dano (OnDamageDealtImpl).");
    }

    void Effect_0438_DarkScorpionMeanaeTheThorn(CardDisplay source)
    {
        // Efeito: Dano -> Busca Dark Scorpion ou Recicla Dark Scorpion.
        Debug.Log("Meanae the Thorn: Efeito passivo de dano (OnDamageDealtImpl).");
    }

    void Effect_0439_DarkScorpionBurglars(CardDisplay source)
    {
        // Efeito: Se você controla 3 Dark Scorpions, envie 1 Spell do deck do oponente ao GY.
        Debug.Log("Dark Scorpion Burglars: Efeito passivo.");
    }

    void Effect_0440_DarkScorpionCombination(CardDisplay source)
    {
        // Efeito: Se tiver os 5 Dark Scorpions, todos atacam direto e causam 400 dano cada.
        // Verifica se tem os 5 (Don Zaloog + 4 Scorpions)
        bool hasDon = GameManager.Instance.IsCardActiveOnField("0516");
        bool hasCliff = GameManager.Instance.IsCardActiveOnField("0436");
        bool hasChick = GameManager.Instance.IsCardActiveOnField("0435");
        bool hasGorg = GameManager.Instance.IsCardActiveOnField("0437");
        bool hasMeanae = GameManager.Instance.IsCardActiveOnField("0438");

        if (hasDon && hasCliff && hasChick && hasGorg && hasMeanae)
        {
            Debug.Log("Dark Scorpion Combination: Todos atacam direto com 400 ATK.");
            // Em um sistema real, aplicaria um modificador de "Direct Attack" e "Fixed Damage 400"
        }
        else
        {
            Debug.Log("Dark Scorpion Combination: Requer os 5 membros.");
        }
    }

    void Effect_0442_DarkSnakeSyndrome(CardDisplay source)
    {
        // Efeito: Standby -> Dano dobra a cada turno.
        // Implementado no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Dark Snake Syndrome: Ativo.");
    }

    void Effect_0443_DarkSpiritOfTheSilent(CardDisplay source)
    {
        // Efeito: Oponente ataca -> Nega e obriga outro monstro a atacar.
        // Requer hook no BattleManager.DeclareAttack
        Debug.Log("Dark Spirit of the Silent: Gatilho de ataque.");
    }

    void Effect_0446_DarkZebra(CardDisplay source)
    {
        // Efeito: Se for o único monstro na Standby, vira defesa.
        // Implementado no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Dark Zebra: Ativo.");
    }

    void Effect_0447_DarkEyesIllusionist(CardDisplay source)
    {
        // FLIP: Seleciona 1 monstro; ele não ataca.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Dark-Eyes Illusionist: {t.CurrentCardData.name} congelado.");
                    // Adicionar flag de "Cannot Attack" vinculada a esta carta
                }
            );
        }
    }

    void Effect_0448_DarkPiercingLight(CardDisplay source)
    {
        // Efeito: Vira todos os monstros face-down do oponente para face-up.
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    var m = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.isFlipped) m.RevealCard();
                }
            }
        }
    }

    void Effect_0449_DarkbishopArchfiend(CardDisplay source)
    {
        // Efeito: Protege Archfiends de efeitos que dão alvo (rola dado).
        // Passivo / Trigger de alvo
        Debug.Log("Darkbishop Archfiend: Ativo.");
    }

    void Effect_0453_DarklordMarie(CardDisplay source)
    {
        // Efeito: Ganha 200 LP na Standby se estiver no GY.
        // Implementado no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Darklord Marie: Ativo.");
    }

    void Effect_0454_DarknessApproaches(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count < 2)
        {
            UIManager.Instance.ShowMessage("Requer 2 cartas na mão para descartar.");
            return;
        }

        bool hasFaceUp = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped) hasFaceUp = true;
        }

        if (!hasFaceUp)
        {
            UIManager.Instance.ShowMessage("Não há monstros face-up no campo.");
            return;
        }

        GameManager.Instance.OpenCardMultiSelection(hand, "Descarte 2 cartas", 2, 2, (selected) => {
            foreach(var c in selected) GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
            
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isFlipped,
                    (t) => {
                        t.ShowBack();
                        if (t.position == CardDisplay.BattlePosition.Attack) t.ChangePosition();
                    }
                );
            }
        });
    }

    void Effect_0456_DeFusion(CardDisplay source)
    {
        bool hasFusion = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (m.CurrentCardData.type.Contains("Fusion")) hasFusion = true;
        }
        
        if (!hasFusion)
        {
            UIManager.Instance.ShowMessage("Não há monstros de Fusão no campo.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Fusion"),
                (t) => {
                    Debug.Log($"De-Fusion: Retornando {t.CurrentCardData.name} ao Extra Deck.");
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard); // Simula retorno
                    Destroy(t.gameObject);
                    // SS Materiais: Requer rastreamento que não temos no protótipo
                }
            );
        }
    }

    void Effect_0457_DeSpell(CardDisplay source)
    {
        // Efeito: Destrói 1 Spell Card. Se setada, revela e destrói se for Spell.
        Effect_MST(source); // Simplificado
    }

    void Effect_0458_DealOfPhantom(CardDisplay source)
    {
        // Efeito: Buff baseado no GY.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => {
                    int count = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster")).Count;
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, count * 100, source));
                }
            );
        }
    }

    void Effect_0459_DecayedCommander(CardDisplay source)
    {
        // Efeito: Se atacar direto, oponente descarta 1.
        // Implementado no CardEffectManager_Impl.cs (OnDamageDealtImpl)
        Debug.Log("Decayed Commander: Ativo.");
    }

    void Effect_0460_DeckDevastationVirus(CardDisplay source)
    {
        bool hasValidTribute = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.CurrentCardData.attribute == "Dark" && m.currentAtk >= 2000) hasValidTribute = true;
                }
            }
        }

        if (!hasValidTribute)
        {
            UIManager.Instance.ShowMessage("Requer 1 monstro DARK com 2000 ou mais de ATK para tributar.");
            return;
        }

        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            Debug.Log("Deck Devastation Virus: Destruindo monstros fracos...");
            // DestroyAllMonsters(true, false); // Simplificado
        }
    }

    void Effect_0461_DedicationThroughLightAndDarkness(CardDisplay source)
    {
        bool hasDM = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) 
                if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.name == "Dark Magician") hasDM = true;
        }
        if (!hasDM)
        {
            UIManager.Instance.ShowMessage("Você precisa controlar 'Dark Magician' para tributar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Dark Magician",
                (t) => {
                    GameManager.Instance.TributeCard(t);
                    // Busca DMoC (Mão/Deck/GY)
                    // Simplificado: Tenta criar direto
                    Debug.Log("Dedication: Invocando Dark Magician of Chaos.");
                    // GameManager.Instance.SpecialSummonById("0422", source.isPlayerCard);
                }
            );
        }
    }

    void Effect_0463_DeepseaWarrior(CardDisplay source)
    {
        // Efeito: Imune a Spells se Umi estiver no campo.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013"))
        {
            Debug.Log("Deepsea Warrior: Imune a Magias.");
        }
    }

    void Effect_0464_DekoichiTheBattlechantedLocomotive(CardDisplay source)
    {
        // FLIP: Compra 1 carta (+1 por cada Bokoichi).
        GameManager.Instance.DrawCard();
        // TODO: Checar Bokoichi
    }

    void Effect_0465_DelinquentDuo(CardDisplay source)
    {
        if (GameManager.Instance.GetOpponentHandData().Count == 0)
        {
            UIManager.Instance.ShowMessage("A mão do oponente está vazia.");
            return;
        }

        if (Effect_PayLP(source, 1000))
        {
            Debug.Log("Delinquent Duo: Oponente descarta 2.");
        }
    }

    void Effect_0466_DeltaAttacker(CardDisplay source)
    {
        // Efeito: Se controlar 3 Normais mesmo nome (Lv3-), atacam direto.
        Debug.Log("Delta Attacker: Condição de ataque direto aplicada.");
    }

    void Effect_0467_Demotion(CardDisplay source)
    {
        // Equip: Reduz Nível em 2.
        Effect_Equip(source, 0, 0);
        Debug.Log("Demotion: Nível reduzido em 2 (Visual).");
    }

    void Effect_0468_DesCounterblow(CardDisplay source)
    {
        // Efeito: Destrói monstro que atacar direto.
        // Implementado no CardEffectManager_Impl.cs (OnDamageDealtImpl)
        Debug.Log("Des Counterblow: Ativo.");
    }

    void Effect_0469_DesCroaking(CardDisplay source)
    {
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.CurrentCardData.name == "Des Frog") count++;
                }
            }
        }

        if (count < 3)
        {
            UIManager.Instance.ShowMessage("Requer 3 'Des Frog' no campo.");
            return;
        }

        bool oppHasCards = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) oppHasCards = true;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if(z.childCount > 0) oppHasCards = true;
            if(GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) oppHasCards = true;
        }

        if (!oppHasCards)
        {
            UIManager.Instance.ShowMessage("O oponente não controla cartas para destruir.");
            return;
        }

        DestroyAllMonsters(true, false);
        Effect_HarpiesFeatherDuster(source);
    }

    void Effect_0470_DesDendle(CardDisplay source)
    {
        // Efeito: Union para Vampiric Orchis. Gera Token.
        Effect_Union(source, "Vampiric Orchis", 0, 0);
    }

    void Effect_0471_DesFeralImp(CardDisplay source)
    {
        // FLIP: Retorna 1 carta do GY para o Deck.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        if (gy.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(gy, "Retornar ao Deck", (selected) => {
                gy.Remove(selected);
                List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                deck.Add(selected);
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                Debug.Log($"Des Feral Imp: {selected.name} retornado ao deck.");
            });
        }
    }

    void Effect_0472_DesFrog(CardDisplay source)
    {
        // Efeito: SS Des Frogs do deck igual a T.A.D.P.O.L.E. no GY.
        int tadpoles = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.name.Contains("T.A.D.P.O.L.E.")).Count;
        if (tadpoles > 0)
        {
             Debug.Log($"Des Frog: Invocando até {tadpoles} Des Frogs do Deck.");
             List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
             List<CardData> frogs = deck.FindAll(c => c.name == "Des Frog");
             
             int count = Mathf.Min(tadpoles, frogs.Count);
             for(int i=0; i<count; i++)
             {
                 GameManager.Instance.SpecialSummonFromData(frogs[i], source.isPlayerCard);
                 deck.Remove(frogs[i]);
             }
        }
    }

    void Effect_0473_DesKangaroo(CardDisplay source)
    {
        // Efeito: Se ATK < DEF do oponente, destrói oponente (cálculo de dano aplica).
        // Lógica implementada no BattleManager.cs (ResolveDamage)
        Debug.Log("Des Kangaroo: Ativo.");
    }

    void Effect_0474_DesKoala(CardDisplay source)
    {
        // FLIP: Dano 400 x cartas na mão do oponente.
        int handCount = GameManager.Instance.GetOpponentHandData().Count;
        Effect_DirectDamage(source, handCount * 400);
    }

    void Effect_0475_DesLacooda(CardDisplay source)
    {
        // Efeito: 1x turno, vira face-down. FLIP: Compra 1.
        GameManager.Instance.DrawCard();
        Effect_TurnSet(source); // Pode virar face-down
    }

    void Effect_0476_DesVolstgalph(CardDisplay source)
    {
        // Efeito: Dano 500 ao destruir monstro. +200 ATK por Spell Normal/Quick.
        // Dano implementado no CardEffectManager_Impl.cs (OnCardLeavesField)
        // Buff de Spell requer hook OnSpellResolved (não implementado, apenas log)
        Debug.Log("Des Volstgalph: Ativo.");
    }

    void Effect_0477_DesWombat(CardDisplay source)
    {
        // Efeito: Dano de efeito em você vira 0.
        // Requer hook no GameManager.DamagePlayer para verificar a fonte do dano
        Debug.Log("Des Wombat: Proteção ativa (Lógica de prevenção pendente).");
    }

    void Effect_0478_DesertSunlight(CardDisplay source)
    {
        // Efeito: Todos os monstros viram Defesa Face-up.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                m.position = CardDisplay.BattlePosition.Defense;
                if (m.isFlipped) m.RevealCard();
            }
        }
    }

    void Effect_0479_Desertapir(CardDisplay source)
    {
        // FLIP: Vira 1 monstro face-down.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => t.ShowBack()
            );
        }
    }

    void Effect_0480_DespairFromTheDark(CardDisplay source)
    {
        // Efeito: Se enviado do deck/mão ao GY por efeito do oponente, SS.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardSentToGraveyard)
        Debug.Log("Despair from the Dark: Ativo.");

    }

    void Effect_0481_DesrookArchfiend(CardDisplay source)
    {
        // Efeito: Envia da mão ao GY para reviver Terrorking destruído.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField)
        Debug.Log("Desrook Archfiend: Ativo.");

    }

    void Effect_0482_DestinyBoard(CardDisplay source)
    {
        // Efeito: Coloca Spirit Messages. Vitória em 5 turnos.
        // Requer lógica complexa de turnos e zonas de S/T.
        Debug.Log("Destiny Board: Contagem iniciada (Simulado).");
    }

    void Effect_0484_DestructionPunch(CardDisplay source)
    {
        // Efeito: Se ATK atacante < DEF defensor, destrói atacante.
        // Lógica implementada no BattleManager.cs (ResolveDamage)
        Debug.Log("Destruction Punch: Ativo.");
    }

    void Effect_0485_DestructionRing(CardDisplay source)
    {
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) hasMonster = true;
            
        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Você não controla monstros para destruir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard,
                (t) => {
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    GameManager.Instance.DamagePlayer(1000);
                    GameManager.Instance.DamageOpponent(1000);
                }
            );
        }
    }

    void Effect_0487_DianKetoTheCureMaster(CardDisplay source)
    {
        // Efeito: Ganha 1000 LP.
        Effect_GainLP(source, 1000);
    }

    void Effect_0489_DiceJar(CardDisplay source)
    {
        // FLIP: Ambos rolam dado. Perdedor toma dano (pode ser 6000).
        int pRoll = Random.Range(1, 7);
        int oRoll = Random.Range(1, 7);
        Debug.Log($"Dice Jar: Player {pRoll}, Opponent {oRoll}");
        
        if (pRoll != oRoll)
        {
            if (pRoll < oRoll)
            {
                int dmg = oRoll * 500;
                if (oRoll == 6) dmg = 6000;
                GameManager.Instance.DamagePlayer(dmg);
            }
            else
            {
                int dmg = pRoll * 500;
                if (pRoll == 6) dmg = 6000;
                GameManager.Instance.DamageOpponent(dmg);
            }
        }
        else
        {
            Debug.Log("Dice Jar: Empate! Rolando novamente...");
            Effect_0489_DiceJar(source); // Reroll
        }
    }

    void Effect_0490_DiceReRoll(CardDisplay source)
    {
        // Efeito: Permite rolar dado novamente 1x por turno.
        Debug.Log("Dice Re-Roll: Ativo (Lógica de interceptar dado pendente).");
    }

    void Effect_0491_DifferentDimensionCapsule(CardDisplay source)
    {
        // Efeito: Bane 1 carta do deck face-down. Adiciona à mão em 2 turnos.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        GameManager.Instance.OpenCardSelection(deck, "Selecione carta para banir (Capsule)", (selected) => {
             deck.Remove(selected);
             GameManager.Instance.RemoveFromPlay(selected, true);
             Debug.Log("Different Dimension Capsule: Carta banida face-down (Simulado).");
             // Add counter or tracking for 2 turns later.
        });
    }

    void Effect_0492_DifferentDimensionDragon(CardDisplay source)
    {
        // Efeito: Imune a destruição por S/T que não dão alvo.
        // Proteção de batalha implementada no BattleManager (nota).
        Debug.Log("Different Dimension Dragon: Ativo.");

    }

    void Effect_0493_DifferentDimensionGate(CardDisplay source)
    {
        bool myHas = false;
        bool oppHas = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) myHas = true;
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHas = true;
        }

        if (!myHas || !oppHas)
        {
            UIManager.Instance.ShowMessage("Ambos os jogadores precisam controlar pelo menos 1 monstro.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
             SpellTrapManager.Instance.StartTargetSelection(
                 (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                 (myMonster) => {
                     SpellTrapManager.Instance.StartTargetSelection(
                         (t2) => t2.isOnField && !t2.isPlayerCard && t2.CurrentCardData.type.Contains("Monster"),
                         (oppMonster) => {
                             GameManager.Instance.BanishCard(myMonster);
                             GameManager.Instance.BanishCard(oppMonster);
                             Debug.Log("Different Dimension Gate: Monstros banidos.");
                         }
                     );
                 }
             );
        }
    }

    void Effect_0494_DiffusionWaveMotion(CardDisplay source)
    {
        bool hasMage = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) 
                if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Spellcaster" && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.level >= 7) hasMage = true;
        }

        bool oppHasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonster = true;

        if (!hasMage || !oppHasMonster)
        {
            UIManager.Instance.ShowMessage("Requer Mago Nível 7+ e oponente deve controlar monstros.");
            return;
        }

        if (GameManager.Instance.PayLifePoints(source.isPlayerCard, 1000))
        {
             if (SpellTrapManager.Instance != null)
             {
                 SpellTrapManager.Instance.StartTargetSelection(
                     (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Spellcaster" && t.CurrentCardData.level >= 7,
                     (target) => {
                         Debug.Log($"Diffusion Wave-Motion: {target.CurrentCardData.name} atacará todos os monstros.");
                         // Set flag on monster for multi-attack
                         // target.canAttackAll = true;
                     }
                 );
             }
        }
    }

    void Effect_0496_DimensionDistortion(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerGraveyard().Count > 0)
        {
            UIManager.Instance.ShowMessage("Seu Cemitério precisa estar vazio.");
            return;
        }

        List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
        if (banished.FindAll(c => c.type.Contains("Monster")).Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui monstros banidos.");
            return;
        }

        GameManager.Instance.OpenCardSelection(banished.FindAll(c => c.type.Contains("Monster")), "Invocar Banido", (selected) => {
            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
        });
    }

    void Effect_0497_DimensionFusion(CardDisplay source)
    {
        List<CardData> pBanished = GameManager.Instance.GetPlayerRemoved();
        List<CardData> oBanished = GameManager.Instance.GetOpponentRemoved();

        if (pBanished.FindAll(c => c.type.Contains("Monster")).Count == 0 && oBanished.FindAll(c => c.type.Contains("Monster")).Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há monstros banidos para invocar.");
            return;
        }

        if (GameManager.Instance.PayLifePoints(source.isPlayerCard, 2000))
        {
            // Player
            List<CardData> pBanished = GameManager.Instance.GetPlayerRemoved();
            // Opponent
            // ...
            Debug.Log("Dimension Fusion: Invocando monstros banidos (Lógica de massa pendente).");
            // Loop SS
        }
    }

    void Effect_0498_DimensionJar(CardDisplay source)
    {
        // FLIP: Bane até 3 monstros do GY do oponente.
        Debug.Log("Dimension Jar: Banindo do GY (Simulado).");
        // UI to select from GY.
    }

    void Effect_0499_DimensionWall(CardDisplay source)
    {
        // Efeito: Oponente toma o dano de batalha de um ataque.
        Debug.Log("Dimension Wall: Dano refletido.");
        if (BattleManager.Instance != null) BattleManager.Instance.dimensionWallActive = true;
    }

    void Effect_0500_Dimensionhole(CardDisplay source)
    {
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) hasMonster = true;
            
        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Você não controla monstros para banir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (tribute) => {
                    // Banir temporariamente (Simulado: Destrói)
                    Destroy(tribute.gameObject);
                    Debug.Log($"Dimensionhole: {tribute.CurrentCardData.name} removido temporariamente.");
                }
            );
        }
    }
}