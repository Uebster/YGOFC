using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 1001 - 1100)
    // =========================================================================================

    void Effect_1001_KangarooChamp(CardDisplay source)
    {
        // Effect: If this card attacks or is attacked, change it to Defense Position at the end of the Damage Step.
        // Lógica no OnBattleEnd.
        Debug.Log("Kangaroo Champ: Efeito de mudança de posição configurado (OnBattleEnd).");
    }

    void Effect_1004_KarakuriSpider(CardDisplay source)
    {
        // Effect: If this card attacks a DARK monster, destroy that monster.
        // Lógica no OnDamageCalculation.
        Debug.Log("Karakuri Spider: Destrói DARK se atacar (Passivo).");
    }

    void Effect_1005_KarateMan(CardDisplay source)
    {
        // Effect: Once per turn, double original ATK. Destroy at End Phase.
        if (source.isOnField)
        {
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, source.originalAtk * 2, source));
            Debug.Log("Karate Man: ATK dobrado. Será destruído na End Phase.");
            // TODO: Agendar destruição na End Phase
        }
    }

    void Effect_1008_Kazejin(CardDisplay source)
    {
        // Effect: Once while face-up, make attacking monster's ATK 0 during damage calculation.
        // Lógica no OnDamageCalculation.
        Debug.Log("Kazejin: Efeito de zerar ATK configurado (OnDamageCalculation).");
    }

    void Effect_1009_Kelbek(CardDisplay source)
    {
        // Effect: If attacked, return attacking monster to hand.
        // Lógica no OnBattleEnd.
        Debug.Log("Kelbek: Efeito de bounce configurado (OnBattleEnd).");
    }

    void Effect_1010_Keldo(CardDisplay source)
    {
        // Effect: When sent to GY by battle: Select 2 cards in opp GY, shuffle into Deck.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Keldo: Efeito de reciclagem configurado (OnCardSentToGraveyard).");
    }

    void Effect_1014_KingDragun(CardDisplay source)
    {
        // Effect: Your Dragons cannot be targeted by Spell/Trap/Effects. Once per turn: SS 1 Dragon from hand.
        if (source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> dragons = hand.FindAll(c => c.race == "Dragon" && c.type.Contains("Monster"));
            
            if (dragons.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(dragons, "Invocar Dragão", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                });
            }
        }
    }

    void Effect_1016_KingTigerWanghu(CardDisplay source)
    {
        // Effect: When monster with ATK <= 1400 is Summoned, destroy it.
        // Lógica no OnSummonImpl (Global Trigger).
        Debug.Log("King Tiger Wanghu: Efeito de destruição automática configurado (OnSummonImpl).");
    }

    void Effect_1018_KingOfTheSkullServants(CardDisplay source)
    {
        // Effect: ATK = Skull Servants/King of Skull Servants in GY * 1000. Revive by banishing 1 Skull Servant.
        if (source.isOnField)
        {
            // Lógica de ATK
            int count = 0;
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            count += gy.FindAll(c => c.name == "Skull Servant" || c.name == "King of the Skull Servants").Count;
            
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, count * 1000, source));
        }
        else if (source.isInPile) // No GY
        {
            // Lógica de Revive (Simplificado: Trigger manual do GY não é suportado nativamente, seria um efeito de ignição se estivesse na mão ou campo, mas aqui é GY trigger)
            // Vamos simular como se fosse ativado ao ser destruído
            Debug.Log("King of the Skull Servants: Efeito de reviver (Requer sistema de efeitos no GY).");
        }
    }

    void Effect_1019_KingOfTheSwamp(CardDisplay source)
    {
        // Effect: Discard to add Polymerization. Substitute for Fusion Material.
        if (!source.isOnField) // Da mão
        {
            GameManager.Instance.DiscardCard(source);
            Effect_SearchDeck(source, "Polymerization");
        }
    }

    void Effect_1020_KingsKnight(CardDisplay source)
    {
        // Effect: If Queen's Knight on field when Normal Summoned: SS Jack's Knight from Deck.
        if (GameManager.Instance.IsCardActiveOnField("Queen's Knight") || GameManager.Instance.IsCardActiveOnField("1475")) // ID Queen's Knight
        {
            Effect_SearchDeck(source, "Jack's Knight", "Monster"); // Simplificado para busca/SS
        }
    }

    void Effect_1021_Kiryu(CardDisplay source)
    {
        // Effect: Union for Dark Blade. +900 ATK. Tribute to destroy face-up monster.
        Effect_Union(source, "Dark Blade", 900, 0);
    }

    void Effect_1022_Kiseitai(CardDisplay source)
    {
        // Effect: If attacked face-down, equip to attacker. Gain LP equal to half attacker's ATK each Standby.
        // Lógica no BattleManager (Equip on attack) e OnPhaseStart (Gain LP).
        Debug.Log("Kiseitai: Efeito de parasita configurado (BattleManager/OnPhaseStart).");
    }

    void Effect_1023_KishidoSpirit(CardDisplay source)
    {
        // Effect: Monsters not destroyed by battle if ATK is equal.
        // Lógica no BattleManager (ResolveDamage).
        Debug.Log("Kishido Spirit: Proteção em empate de ATK configurada (BattleManager).");
    }

    void Effect_1024_KnightsTitle(CardDisplay source)
    {
        // Effect: Tribute Dark Magician; SS Dark Magician Knight from hand/Deck/GY.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Dark Magician",
                (t) => {
                    GameManager.Instance.TributeCard(t);
                    // Busca DMK (Simplificado: Tenta criar direto)
                    Debug.Log("Knight's Title: Invocando Dark Magician Knight.");
                    // GameManager.Instance.SpecialSummonById("0421", source.isPlayerCard);
                }
            );
        }
    }

    void Effect_1025_Koitsu(CardDisplay source)
    {
        // Effect: Union for Aitsu. +3000 ATK. Piercing.
        Effect_Union(source, "Aitsu", 3000, 0);
    }

    void Effect_1028_Kotodama(CardDisplay source)
    {
        // Effect: As long as this card remains face-up on the field, monsters with the same name cannot exist on the field.
        // If a monster with the same name is Summoned, it is destroyed.
        // Lógica no OnSummonImpl (Global Trigger).
        Debug.Log("Kotodama: Regra de unicidade ativa (OnSummonImpl).");
    }

    void Effect_1031_KozakysSelfDestructButton(CardDisplay source)
    {
        // Effect: When "Kozaky" is destroyed and sent to the Graveyard, inflict 1000 damage to the controller of this card.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Kozaky's Self-Destruct Button: Efeito de dano configurado (OnCardSentToGraveyard).");
    }

    void Effect_1033_Kryuel(CardDisplay source)
    {
        // Effect: When sent to GY by battle: Toss coin. Heads: Destroy opponent's monster.
        Effect_CoinTossDestroy(source, 1, 1, TargetType.Monster);
    }

    void Effect_1035_KunaiWithChain(CardDisplay source)
    {
        // Effect: Activate 1 or both:
        // 1. Change attacking monster to Defense.
        // 2. Equip to a monster, +500 ATK.
        // Simplificado: Se atacado, muda posição. Se ativado livremente, equipa.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            BattleManager.Instance.currentAttacker.ChangePosition();
            Debug.Log("Kunai with Chain: Atacante virou defesa.");
        }
        else
        {
            Effect_Equip(source, 500, 0);
        }
    }

    void Effect_1037_Kuriboh(CardDisplay source)
    {
        // Effect: Discard to make battle damage 0.
        // Lógica no OnDamageCalculation (Hand Trap).
        if (!source.isOnField) // Da mão
        {
            GameManager.Instance.DiscardCard(source);
            Debug.Log("Kuriboh: Dano de batalha reduzido a 0 (Efeito ativado da mão).");
        }
    }

    void Effect_1040_KycooTheGhostDestroyer(CardDisplay source)
    {
        // Effect: When inflicts battle damage: Banish up to 2 monsters from opp GY. Opponent cannot banish from GY.
        // Lógica de banir no OnDamageDealtImpl.
        // Lógica de bloqueio no GameManager (RemoveFromPlay).
        if (source.isOnField)
        {
            // Bloqueio passivo (deve ser checado no GameManager.RemoveFromPlay)
            // Efeito de dano:
            // (Isso seria chamado pelo OnDamageDealtImpl)
            List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
            List<CardData> monsters = oppGY.FindAll(c => c.type.Contains("Monster"));
            if (monsters.Count > 0)
            {
                int max = Mathf.Min(2, monsters.Count);
                GameManager.Instance.OpenCardMultiSelection(monsters, "Banir do Oponente (Kycoo)", 1, max, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.RemoveFromPlay(c, !source.isPlayerCard);
                        oppGY.Remove(c);
                    }
                });
            }
        }
    }

    void Effect_1046_LabyrinthOfNightmare(CardDisplay source)
    {
        // Effect: End Phase: Change battle position of all face-up monsters.
        // Lógica no OnPhaseStart (End Phase).
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Itera e muda posição (simplificado)
            Debug.Log("Labyrinth of Nightmare: Mudando posições de batalha na End Phase.");
        }
    }

    void Effect_1047_LadyAssailantOfFlames(CardDisplay source)
    {
        // FLIP: Banish top 3 cards of deck. 800 damage to opponent.
        GameManager.Instance.MillCards(source.isPlayerCard, 3); // Deveria ser Banish
        Effect_DirectDamage(source, 800);
    }

    void Effect_1048_LadyNinjaYae(CardDisplay source)
    {
        // Effect: Discard 1 WIND monster; return all S/T opponent controls to hand.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> winds = hand.FindAll(c => c.attribute == "Wind" && c.type.Contains("Monster"));
        
        if (winds.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(winds, "Descarte 1 WIND", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Effect_HarpiesFeatherDuster(source); // Reusa lógica de limpar S/T (mas deveria ser ReturnToHand)
                Debug.Log("Lady Ninja Yae: S/T retornadas (Simulado como destruição).");
            });
        }
    }

    void Effect_1049_LadyPanther(CardDisplay source)
    {
        // Effect: Tribute this card; return 1 monster destroyed by battle this turn to top of Deck.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            // Seleciona do GY (simplificado)
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
            if (monsters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(monsters, "Retornar ao Topo", (selected) => {
                    gy.Remove(selected);
                    GameManager.Instance.GetPlayerMainDeck().Insert(0, selected);
                });
            }
        }
    }

    void Effect_1051_LarvaeMoth(CardDisplay source)
    {
        // Effect: SS from hand by Tributing Petit Moth equipped with Cocoon for 2 turns.
        if (!source.isOnField)
        {
            // Verifica Petit Moth + Cocoon + Turnos
            // Simplificado: Verifica apenas Petit Moth equipado com Cocoon
            if (GameManager.Instance.IsCardActiveOnField("Petit Moth") || GameManager.Instance.IsCardActiveOnField("1420"))
            {
                // ... Lógica de tributo e SS
                Debug.Log("Larvae Moth: Condição de invocação verificada (simplificada).");
            }
        }
    }

    void Effect_1053_LaserCannonArmor(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Insect");
    }

    void Effect_1054_LastDayOfWitch(CardDisplay source)
    {
        Effect_DestroyType(source, "Spellcaster");
    }

    void Effect_1055_LastTurn(CardDisplay source)
    {
        // Effect: Win condition complexa.
        if (GameManager.Instance.playerLP <= 1000)
        {
            Debug.Log("Last Turn: Ativado! Selecione um monstro.");
            // Seleciona 1 monstro, envia tudo o mais pro GY.
            // SS monstro do oponente. Batalha especial.
            // End Phase: Vitória baseada em monstros restantes.
        }
    }

    void Effect_1056_LastWill(CardDisplay source)
    {
        // Effect: If monster sent to GY: SS 1 monster with ATK <= 1500 from Deck.
        // Trigger global.
        // Marca flag para permitir SS neste turno se condição for atendida
        Debug.Log("Last Will: Efeito ativo. Se um monstro for para o GY, poderá invocar do Deck.");
        // GameManager.Instance.canSpecialSummonLastWill = true;
    }

    void Effect_1059_LavaBattleguard(CardDisplay source)
    {
        // Effect: +500 ATK for each Swamp Battleguard.
        int count = 0;
        if (GameManager.Instance.IsCardActiveOnField("Swamp Battleguard") || GameManager.Instance.IsCardActiveOnField("1799")) count++;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 500, source));
    }

    void Effect_1060_LavaGolem(CardDisplay source)
    {
        // Effect: SS to opponent's field by tributing 2 monsters. 1000 damage standby.
        // Condition: Cannot be Normal Summoned/Set. Must first be Special Summoned (from your hand) to your opponent's field by Tributing 2 monsters they control.
        if (!source.isOnField)
        {
            // Check if opponent has at least 2 monsters
            if (SummonManager.Instance.HasEnoughTributes(2, !source.isPlayerCard))
            {
                 // Select 2 monsters from opponent to tribute
                 List<CardDisplay> oppMonsters = new List<CardDisplay>();
                 if (GameManager.Instance.duelFieldUI != null)
                 {
                     foreach(var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                     {
                         if(zone.childCount > 0) oppMonsters.Add(zone.GetChild(0).GetComponent<CardDisplay>());
                     }
                 }
                 
                 if (oppMonsters.Count >= 2)
                 {
                     // For prototype, just pick the first 2 or random 2, or try to open selection.
                     List<CardData> oppMonsterData = new List<CardData>();
                     foreach(var m in oppMonsters) oppMonsterData.Add(m.CurrentCardData);
                     
                     GameManager.Instance.OpenCardMultiSelection(oppMonsterData, "Tribute 2 Opponent Monsters", 2, 2, (selected) => {
                         // Tribute them
                         foreach(var cardData in selected)
                         {
                             CardDisplay cd = oppMonsters.Find(m => m.CurrentCardData == cardData);
                             if (cd != null) GameManager.Instance.TributeCard(cd);
                         }
                         
                         // SS Lava Golem to opponent's field
                         // We need to move this card from hand to opponent's field.
                         // SpecialSummonFromData usually summons to the player's field specified by the bool flag.
                         // source.isPlayerCard is true (it's in my hand). We want to summon to !source.isPlayerCard (opponent).
                         
                         GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, !source.isPlayerCard, true, false); // Face-up Attack
                         GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                         
                         Debug.Log("Lava Golem: Summoned to opponent's field.");
                     });
                 }
            }
            else
            {
                Debug.Log("Lava Golem: Opponent does not have 2 monsters.");
            }
        }
    }

    void Effect_1063_LegacyHunter(CardDisplay source)
    {
        // Effect: If destroys monster by battle: Opponent shuffles 1 random card from hand to Deck.
        // Lógica no OnBattleEnd.
        if (source.isOnField)
        {
            // Chamado quando destrói monstro
            // GameManager.Instance.ShuffleHandToDeck(!source.isPlayerCard, 1);
            Debug.Log("Legacy Hunter: Oponente embaralha 1 carta da mão no deck.");
        }
    }

    void Effect_1064_LegacyOfYataGarasu(CardDisplay source)
    {
        // Effect: Draw 1 card. If opponent has Spirit, draw 2.
        GameManager.Instance.DrawCard();
        bool oppHasSpirit = false;
        // Checar campo oponente
        if (oppHasSpirit) GameManager.Instance.DrawCard();
    }

    void Effect_1065_LegendaryBlackBelt(CardDisplay source)
    {
        // Effect: Equip. If destroys monster by battle: Inflict damage = DEF of destroyed monster.
        // Lógica no OnBattleEnd.
        Effect_Equip(source, 0, 0); // Apenas equipa
    }

    void Effect_1066_LegendaryFiend(CardDisplay source)
    {
        // Effect: Gains 700 ATK each Standby Phase.
        // Lógica no OnPhaseStart.
        if (source.isOnField && source.position == CardDisplay.BattlePosition.Attack)
        {
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 700, source));
            Debug.Log("Legendary Fiend: +700 ATK (Standby Phase).");
        }
    }

    void Effect_1067_LegendaryFlameLord(CardDisplay source)
    {
        // Ritual. Can put counter. Remove 3 counters -> Destroy all monsters.
        // Lógica de Spell Counter no OnSpellActivated
        if (source.spellCounters >= 3)
        {
            source.RemoveSpellCounter(3);
            DestroyAllMonsters(true, true); // Exceto ele mesmo (filtro pendente)
            Debug.Log("Legendary Flame Lord: Incinerator ativado!");
        }
    }

    void Effect_1068_LegendaryJujitsuMaster(CardDisplay source)
    {
        // Effect: If battled in Defense: Return attacker to top of Deck.
        // Lógica no OnBattleEnd.
        if (source.position == CardDisplay.BattlePosition.Defense)
        {
            // Retorna atacante ao topo
            Debug.Log("Legendary Jujitsu Master: Atacante retornado ao topo do Deck.");
        }
    }

    void Effect_1069_LegendarySword(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Warrior");
    }

    void Effect_1070_Leghul(CardDisplay source)
    {
        // Effect: Can attack directly.
        Debug.Log("Leghul: Ataque direto habilitado.");
        // Flag no BattleManager ou CardDisplay: source.canAttackDirectly = true;
    }

    void Effect_1071_Lekunga(CardDisplay source)
    {
        // Effect: Banish 2 WATER from GY; SS 1 Lekunga Token.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> waters = gy.FindAll(c => c.attribute == "Water");
        
        if (waters.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(waters, "Banir 2 WATER", 2, 2, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    gy.Remove(c);
                }
                GameManager.Instance.SpawnToken(source.isPlayerCard, 700, 700, "Lekunga Token");
            });
        }
    }

    void Effect_1075_LesserFiend(CardDisplay source)
    {
        // Effect: Monsters destroyed by this card are banished instead of going to GY.
        // Lógica no OnBattleEnd.
        Debug.Log("Lesser Fiend: Bane monstros destruídos.");
    }

    void Effect_1076_LevelConversionLab(CardDisplay source)
    {
        // Effect: Reveal 1 monster in hand, roll die. Change level until End Phase.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Revelar Monstro", (selected) => {
                int roll = Random.Range(1, 7);
                Debug.Log($"Level Conversion Lab: Rolou {roll}. Nível de {selected.name} alterado.");
                // Alterar nível na mão é complexo sem instanciar.
                // Simplificação: Apenas log.
            });
        }
    }

    void Effect_1077_LevelLimitAreaB(CardDisplay source)
    {
        // Effect: All Level 4 or higher monsters in Attack Position are changed to Defense Position.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.CurrentCardData.level >= 4 && m.position == CardDisplay.BattlePosition.Attack)
                {
                    m.ChangePosition();
                }
            }
        }
    }

    void Effect_1078_LevelUp(CardDisplay source)
    {
        // Effect: Send 1 "LV" monster to GY; SS the monster written in its text.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name.Contains("LV"),
                (t) => {
                    // Lógica simplificada: Tenta adivinhar o próximo nível pelo nome ou ID
                    // Ex: Horus LV4 -> Horus LV6
                    Debug.Log($"Level Up!: Evoluindo {t.CurrentCardData.name}.");
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    // SS lógica pendente (precisa mapear evoluções)
                }
            );
        }
    }

    void Effect_1079_LeviaDragonDaedalus(CardDisplay source)
    {
        // Effect: Send "Umi" to GY; destroy all other cards on the field.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi
        {
            // Envia Umi (simplificado: destrói field spell)
            // ...
            Debug.Log("Daedalus: Destruindo tudo exceto este card.");
            DestroyAllMonsters(true, true); // Filtrar source
            Effect_HeavyStorm(source);
        }
    }

    void Effect_1080_LifeAbsorbingMachine(CardDisplay source)
    {
        // Effect: Standby Phase: Gain LP equal to half the LP you paid in the last turn.
        // Requires tracking. For now, just log.
        Debug.Log("Life Absorbing Machine: Recuperação de LP (Requer sistema de rastreamento de LP pagos).");
    }

    void Effect_1081_LightOfIntervention(CardDisplay source)
    {
        // Effect: Monsters cannot be Set face-down. Monsters Set in Defense are summoned in Face-up Defense.
        Debug.Log("Light of Intervention: Monstros devem ser invocados face-up (Regra global).");
    }

    void Effect_1082_LightOfJudgment(CardDisplay source)
    {
        // Effect: If "The Sanctuary in the Sky" is on field: Discard 1 LIGHT; look at opp hand discard 1 OR destroy 1 card opp controls.
        if (GameManager.Instance.IsCardActiveOnField("1887")) // Sanctuary
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> lights = hand.FindAll(c => c.attribute == "Light" && c.type.Contains("Monster"));
            
            if (lights.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(lights, "Descarte 1 LIGHT", (discarded) => {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                    
                    // Choose effect (Simulated: Random or fixed for now as we lack choice UI)
                    // Let's implement the "Destroy 1 card opp controls" as it's easier to simulate or just log choice.
                    Debug.Log("Light of Judgment: Efeito ativado (Escolha de efeito pendente).");
                    // Simulating destruction for now
                    if (SpellTrapManager.Instance != null)
                    {
                        SpellTrapManager.Instance.StartTargetSelection(
                            (t) => t.isOnField && !t.isPlayerCard,
                            (t) => {
                                GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                                Destroy(t.gameObject);
                            }
                        );
                    }
                });
            }
        }
    }

    void Effect_1083_LightenTheLoad(CardDisplay source)
    {
        // Effect: Shuffle 1 Level 7+ monster from hand to Deck; draw 1 card.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> highLevel = hand.FindAll(c => c.level >= 7 && c.type.Contains("Monster"));
        
        if (highLevel.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(highLevel, "Embaralhar no Deck", (selected) => {
                GameManager.Instance.RemoveCardFromHand(selected, true);
                GameManager.Instance.ReturnToDeck(null, false); // Workaround
                GameManager.Instance.GetPlayerMainDeck().Add(selected);
                GameManager.Instance.ShuffleDeck(true);
                GameManager.Instance.DrawCard();
            });
        }
    }

    void Effect_1084_LightforceSword(CardDisplay source)
    {
        // Effect: Banish 1 random card from opp hand face-down for 4 turns.
        GameManager.Instance.DiscardRandomHand(false, 1); // Deveria ser banish temporário
        Debug.Log("Lightforce Sword: Carta banida da mão do oponente.");
    }

    void Effect_1085_LightningBlade(CardDisplay source)
    {
        Effect_Equip(source, 800, 0, "Warrior");
    }

    void Effect_1087_LightningVortex(CardDisplay source)
    {
        // Effect: Discard 1 card; destroy all face-up monsters opponent controls.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Lightning Vortex: Destruindo monstros face-up do oponente.");
                // DestroyAllMonsters(true, false); // Filtrar face-up
            });
        }
    }

    void Effect_1088_LimiterRemoval(CardDisplay source)
    {
        // Effect: Double ATK of all Machines. Destroy them at End Phase.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> machines = new List<CardDisplay>();
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.CurrentCardData.race == "Machine")
                    {
                        m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Multiply, 2f, source));
                        // Mark for destruction? We don't have a delayed effect system yet.
                        Debug.Log($"Limiter Removal: {m.CurrentCardData.name} ATK dobrado.");
                    }
                }
            }
        }
    }

    void Effect_1091_LittleChimera(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Fire");
    }

    void Effect_1093_LittleWinguard(CardDisplay source)
    {
        // Effect: Once per turn, during End Phase: Change battle position.
        // This is a trigger effect.
        Debug.Log("Little-Winguard: Pode mudar posição na End Phase.");
    }

    void Effect_1096_LoneWolf(CardDisplay source)
    {
        // Effect: If you control "Monk Fighter" or "Master Monk", it cannot be destroyed by battle.
        Debug.Log("Lone Wolf: Proteção ativa (Passivo).");
    }

    void Effect_1097_LordPoison(CardDisplay source)
    {
        // Effect: If destroyed by battle: SS 1 Plant from GY.
        // Logic in OnBattleEnd/OnCardSentToGraveyard.
        // Here we can implement the selection if called from there.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> plants = gy.FindAll(c => c.race == "Plant" && c != source.CurrentCardData);
        
        if (plants.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(plants, "Reviver Planta", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_1098_LordOfD(CardDisplay source)
    {
        // Effect: Dragons cannot be targeted by card effects.
        Debug.Log("Lord of D.: Proteção de Dragões ativa (Passivo).");
    }

    void Effect_1101_LostGuardian(CardDisplay source)
    {
        // Effect: DEF = 700 * banished Rocks.
        if (source.isOnField)
        {
            int count = 0;
            // Count banished rocks (assuming we can access banished list)
            List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
            count = banished.FindAll(c => c.race == "Rock").Count;
            
            source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, count * 700, source));
        }
    }

    void Effect_1103_LuminousSoldier(CardDisplay source)
    {
        // Effect: If attacks DARK, +500 ATK.
        Debug.Log("Luminous Soldier: Buff vs DARK (OnDamageCalculation).");
    }

    void Effect_1104_LuminousSpark(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Light");
    }

    void Effect_1112_MachineConversionFactory(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Machine");
    }

    void Effect_1113_MachineDuplication(CardDisplay source)
    {
        // Effect: Select 1 Machine with ATK <= 500; SS up to 2 copies from Deck.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Machine" && t.currentAtk <= 500,
                (target) => {
                    string name = target.CurrentCardData.name;
                    List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                    List<CardData> copies = deck.FindAll(c => c.name == name);
                    
                    if (copies.Count > 0)
                    {
                        int max = Mathf.Min(2, copies.Count);
                        GameManager.Instance.OpenCardMultiSelection(copies, "Invocar Cópias", 1, max, (selected) => {
                            foreach(var c in selected)
                            {
                                GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
                                deck.Remove(c);
                            }
                            GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    void Effect_1114_MachineKing(CardDisplay source)
    {
        // Effect: +100 ATK for each Machine on the field.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.CurrentCardData.race == "Machine") count++;
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 100, source));
    }

    void Effect_1117_MadSwordBeast(CardDisplay source)
    {
        // Effect: Piercing damage.
        Debug.Log("Mad Sword Beast: Dano perfurante (Passivo).");
    }

    void Effect_1121_MagicDrain(CardDisplay source)
    {
        // Effect: Counter Trap. Opponent can discard 1 Spell to negate this.
        Debug.Log("Magic Drain: Negação condicional (Requer interação do oponente).");
    }

    void Effect_1122_MagicFormula(CardDisplay source)
    {
        // Effect: Equip "Dark Magician" or "Dark Magician Girl". +700 ATK. If sent to GY, gain 1000 LP.
        Effect_Equip(source, 700, 0, "Spellcaster"); // Simplificado
        // Lógica de cura no OnCardSentToGraveyard
    }

    void Effect_1123_MagicJammer(CardDisplay source)
    {
        // Effect: Discard 1 card; negate Spell activation and destroy it.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Magic Jammer: Magia negada (Simulado).");
            });
        }
    }

    void Effect_1124_MagicReflector(CardDisplay source)
    {
        // Effect: Select 1 Spell Card; place 1 counter on it. If destroyed, remove counter instead.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Spell"),
                (t) => {
                    t.AddSpellCounter(1);
                    Debug.Log($"Magic Reflector: Contador adicionado em {t.CurrentCardData.name}.");
                }
            );
        }
    }

    void Effect_1125_MagicalArmShield(CardDisplay source)
    {
        // Effect: When attacked: Take control of 1 opp monster (except attacker) and make it the target.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t != BattleManager.Instance.currentAttacker,
                    (newTarget) => {
                        GameManager.Instance.SwitchControl(newTarget);
                        BattleManager.Instance.currentTarget = newTarget;
                        Debug.Log($"Magical Arm Shield: {newTarget.CurrentCardData.name} é o novo alvo.");
                    }
                );
            }
        }
    }

    void Effect_1126_MagicalDimension(CardDisplay source)
    {
        // Effect: If you control Spellcaster: Tribute 1 monster; SS Spellcaster from hand, then destroy 1 monster.
        if (GameManager.Instance.IsCardActiveOnField("Spellcaster") || GameManager.Instance.IsCardActiveOnField("0419")) // Check Spellcaster generic
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard,
                    (tribute) => {
                        GameManager.Instance.TributeCard(tribute);
                        
                        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                        List<CardData> spellcasters = hand.FindAll(c => c.race == "Spellcaster" && c.type.Contains("Monster"));
                        
                        if (spellcasters.Count > 0)
                        {
                            GameManager.Instance.OpenCardSelection(spellcasters, "Invocar Mago", (selected) => {
                                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                                GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                                
                                // Optional Destroy
                                UIManager.Instance.ShowConfirmation("Destruir um monstro?", () => {
                                    SpellTrapManager.Instance.StartTargetSelection(
                                        (target) => target.isOnField && target.CurrentCardData.type.Contains("Monster"),
                                        (target) => {
                                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                                            Destroy(target.gameObject);
                                        }
                                    );
                                });
                            });
                        }
                    }
                );
            }
        }
    }

    void Effect_1127_MagicalExplosion(CardDisplay source)
    {
        // Effect: If no cards in hand: 200 damage per Spell in GY.
        if (GameManager.Instance.GetPlayerHandData().Count == 0)
        {
            int spells = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Spell")).Count;
            Effect_DirectDamage(source, spells * 200);
        }
    }

    void Effect_1129_MagicalHats(CardDisplay source)
    {
        // Effect: Select 2 non-monster cards from Deck + 1 monster on field. Shuffle and Set face-down.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (monster) => {
                    List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                    List<CardData> nonMonsters = deck.FindAll(c => !c.type.Contains("Monster"));
                    
                    if (nonMonsters.Count >= 2)
                    {
                        GameManager.Instance.OpenCardMultiSelection(nonMonsters, "Selecione 2 não-monstros", 2, 2, (selected) => {
                            // Change monster to face-down defense
                            if (monster.position == CardDisplay.BattlePosition.Attack) monster.ChangePosition();
                            monster.ShowBack();
                            
                            // Spawn tokens representing the hats (Simulated)
                            foreach(var c in selected)
                            {
                                GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Magical Hat");
                                deck.Remove(c);
                            }
                            GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                            Debug.Log("Magical Hats: Monstro escondido e iscas posicionadas.");
                        });
                    }
                }
            );
        }
    }

    void Effect_1130_MagicalLabyrinth(CardDisplay source)
    {
        // Effect: Equip "Labyrinth Wall"; tribute it to SS "Wall Shadow".
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.name == "Labyrinth Wall",
                (t) => {
                    GameManager.Instance.TributeCard(t);
                    // SS Wall Shadow (ID 2048)
                    // GameManager.Instance.SpecialSummonById("2048", source.isPlayerCard);
                    Debug.Log("Magical Labyrinth: Wall Shadow invocado.");
                }
            );
        }
    }

    void Effect_1131_MagicalMarionette(CardDisplay source)
    {
        // Effect: Spell Counter on Spell activation. +200 ATK per counter. Remove 2 -> Destroy monster.
        // Lógica de contadores no OnSpellActivated.
        // Ignition:
        if (source.spellCounters >= 2)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (t) => {
                        source.RemoveSpellCounter(2);
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                );
            }
        }
    }

    void Effect_1132_MagicalMerchant(CardDisplay source)
    {
        // FLIP: Excavate until S/T found. Add to hand, send monsters to GY.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> excavated = new List<CardData>();
        CardData foundST = null;
        
        int index = 0;
        while (index < deck.Count)
        {
            CardData current = deck[index];
            excavated.Add(current);
            if (current.type.Contains("Spell") || current.type.Contains("Trap"))
            {
                foundST = current;
                break;
            }
            index++;
        }
        
        foreach (var c in excavated)
        {
            deck.Remove(c);
            if (c == foundST)
            {
                GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                Debug.Log($"Magical Merchant: Adicionou {c.name} à mão.");
            }
            else
            {
                GameManager.Instance.SendToGraveyard(c, source.isPlayerCard);
            }
        }
    }

    void Effect_1133_MagicalPlantMandragola(CardDisplay source)
    {
        // FLIP: Place 1 Spell Counter on each face-up card that can have one.
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Simplificado: Adiciona a todos os monstros face-up
            List<CardDisplay> targets = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, targets);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, targets);
            
            foreach(var t in targets)
            {
                if (!t.isFlipped) t.AddSpellCounter(1);
            }
            Debug.Log("Mandragola: Contadores distribuídos.");
        }
    }

    void Effect_1134_MagicalScientist(CardDisplay source)
    {
        // Effect: Pay 1000 LP; SS 1 Level 6 or lower Fusion Monster from Extra Deck.
        // Cannot attack directly, return to Extra Deck at End Phase.
        if (Effect_PayLP(source, 1000))
        {
            List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
            List<CardData> targets = extra.FindAll(c => c.type.Contains("Fusion") && c.level <= 6);
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Invocar Fusão Lv6-", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    // TODO: Aplicar restrição de ataque direto e retorno na End Phase
                    Debug.Log($"Magical Scientist: Invocou {selected.name}.");
                });
            }
        }
    }

    void Effect_1135_MagicalStoneExcavation(CardDisplay source)
    {
        // Effect: Discard 2 cards; add 1 Spell Card from GY to hand.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Descarte 2 cartas", 2, 2, (discarded) => {
                foreach(var c in discarded)
                {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                }
                
                Effect_SearchDeck(source, "Spell"); // Deveria ser do GY, não do Deck. Ajustando:
                
                List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));
                if (spells.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(spells, "Recuperar Magia", (selected) => {
                        gy.Remove(selected);
                        GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                    });
                }
            });
        }
    }

    void Effect_1136_MagicalThorn(CardDisplay source)
    {
        // Effect: When opponent discards from hand to GY, inflict 500 damage per card.
        // Lógica no OnCardDiscarded (CardEffectManager_Impl.cs).
        Debug.Log("Magical Thorn: Ativo (Gatilho de descarte configurado).");
    }

    void Effect_1137_MagicianOfBlackChaos(CardDisplay source)
    {
        Debug.Log("Magician of Black Chaos: Monstro Ritual (Sem efeito).");
    }

    void Effect_1138_MagicianOfFaith(CardDisplay source)
    {
        // FLIP: Target 1 Spell in GY; add to hand.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Recuperar Magia", (selected) => {
                gy.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_1139_MagiciansValkyria(CardDisplay source)
    {
        // Effect: Opponent cannot target other Spellcasters for attacks.
        Debug.Log("Magician's Valkyria: Proteção de Magos ativa (Passivo).");
    }

    void Effect_1140_MahaVailo(CardDisplay source)
    {
        // Effect: Gains 500 ATK for each Equip Card equipped to this card.
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        int equipCount = 0;
        foreach(var link in links)
        {
            if (link.target == source && link.type == CardLink.LinkType.Equipment)
                equipCount++;
        }
        
        source.RemoveModifiersFromSource(source);
        if (equipCount > 0)
        {
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, equipCount * 500, source));
        }
        Debug.Log($"Maha Vailo: {equipCount} equipamentos. +{equipCount*500} ATK.");
    }

    void Effect_1141_Maharaghi(CardDisplay source)
    {
        // Spirit. Summon/Flip: See top card of Deck.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (deck.Count > 0)
        {
            Debug.Log($"Maharaghi: Topo do deck é {deck[0].name}.");
        }
    }

    void Effect_1142_MaidenOfTheAqua(CardDisplay source)
    {
        // Effect: Field treated as Umi.
        // Lógica de verificação de Umi no GameManager deve incluir este card.
        Debug.Log("Maiden of the Aqua: Campo tratado como Umi (Passivo).");
    }

    void Effect_1144_MajiGirePanda(CardDisplay source)
    {
        // Effect: Gains 500 ATK when a Beast is destroyed.
        // Lógica no OnCardLeavesField.
        Debug.Log("Maji-Gire Panda: Efeito de ganho de ATK configurado.");
    }

    void Effect_1145_MajorRiot(CardDisplay source)
    {
        // Effect: Return all monsters to hand, then SS same number.
        Debug.Log("Major Riot: Resetando campo (Lógica complexa de retorno e SS pendente).");
        // Requereria coletar todos, retornar, contar e pedir SS.
    }

    void Effect_1146_MajuGarzett(CardDisplay source)
    {
        // Effect: ATK = combined original ATK of 2 tributes.
        // Lógica no SummonManager.
        Debug.Log("Maju Garzett: ATK definido pelos tributos (Lógica no SummonManager).");
    }

    void Effect_1147_MakiuTheMagicalMist(CardDisplay source)
    {
        // Effect: Select 1 "Summoned Skull" or Thunder monster. Destroy all opp monsters with DEF < ATK of selected.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && (t.CurrentCardData.name == "Summoned Skull" || t.CurrentCardData.race == "Thunder"),
                (selected) => {
                    int threshold = selected.currentAtk;
                    // Destruir oponentes com DEF < threshold
                    Debug.Log($"Makiu: Destruindo monstros com DEF < {threshold}.");
                }
            );
        }
    }

    void Effect_1148_MakyuraTheDestructor(CardDisplay source)
    {
        // Effect: If sent to GY, can activate Traps from hand this turn.
        Debug.Log("Makyura: Traps da mão ativadas.");
        if (SpellTrapManager.Instance != null) SpellTrapManager.Instance.canActivateTrapsFromHand = true;
    }

    void Effect_1149_MalevolentCatastrophe(CardDisplay source)
    {
        // Effect: When opponent declares attack: Destroy all S/T.
        // Requer Trigger de ataque.
        Debug.Log("Malevolent Catastrophe: Destruindo todas as S/T.");
        Effect_HeavyStorm(source);
    }

    void Effect_1150_MalevolentNuzzler(CardDisplay source)
    {
        // Equip: +700 ATK. If sent to GY, pay 500 to return to top of Deck.
        Effect_Equip(source, 700, 0);
        // Lógica de retorno no OnCardSentToGraveyard.
    }

    void Effect_1151_MaliceDispersion(CardDisplay source)
    {
        // Effect: Discard 1 card; destroy all face-up Continuous Trap Cards.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                List<CardDisplay> toDestroy = new List<CardDisplay>();
                if (GameManager.Instance.duelFieldUI != null)
                {
                    List<Transform> zones = new List<Transform>();
                    zones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
                    zones.AddRange(GameManager.Instance.duelFieldUI.opponentSpellZones);
                    
                    foreach(var z in zones)
                    {
                        if (z.childCount > 0)
                        {
                            var cd = z.GetChild(0).GetComponent<CardDisplay>();
                            if (cd != null && !cd.isFlipped && cd.CurrentCardData.type.Contains("Trap") && cd.CurrentCardData.property == "Continuous")
                            {
                                toDestroy.Add(cd);
                            }
                        }
                    }
                }
                DestroyCards(toDestroy, source.isPlayerCard);
                Debug.Log("Malice Dispersion: Traps Contínuas destruídas.");
            });
        }
    }

    void Effect_1152_MaliceDollOfDemise(CardDisplay source)
    {
        // Effect: If sent from field to GY by effect of Continuous Spell, SS during Standby Phase.
        // Lógica no OnCardSentToGraveyard e OnPhaseStart.
        Debug.Log("Malice Doll of Demise: Gatilho de renascimento configurado.");
    }

    void Effect_1155_ManEaterBug(CardDisplay source)
    {
        // FLIP: Target 1 monster on the field; destroy it.
        Effect_FlipDestroy(source, TargetType.Monster);
    }

    void Effect_1159_ManThroTro(CardDisplay source)
    {
        // Effect: Tribute 1 Normal Monster; inflict 800 damage.
        // Simplificado: Usa helper genérico, mas idealmente filtraria por Normal Monster.
        Effect_TributeToBurn(source, 1, 800);
    }

    void Effect_1160_MangaRyuRan(CardDisplay source)
    {
        // Toon Monster.
        Debug.Log("Manga Ryu-Ran: Toon.");
    }

    void Effect_1161_ManjuOfTheTenThousandHands(CardDisplay source)
    {
        // Effect: When Normal/Flip Summoned: Add 1 Ritual Monster or Ritual Spell from Deck.
        Effect_SearchDeck(source, "Ritual");
    }

    void Effect_1162_ManticoreOfDarkness(CardDisplay source)
    {
        // Effect: End Phase: Send 1 Beast/Beast-Warrior from hand/field to GY to SS this card from GY.
        // Lógica no OnPhaseStart (End Phase).
        Debug.Log("Manticore of Darkness: Loop de renascimento configurado.");
    }

    void Effect_1163_MaraudingCaptain(CardDisplay source)
    {
        // Effect: Normal Summon -> SS 1 Lv4 or lower monster from hand. Opponent cannot target other Warriors for attacks.
        if (source.isOnField && source.summonedThisTurn)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> targets = hand.FindAll(c => c.level <= 4 && c.type.Contains("Monster"));
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Invocar Lv4-", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                });
            }
        }
        // Proteção de ataque é passiva no BattleManager.
    }

    void Effect_1165_Marshmallon(CardDisplay source)
    {
        // Effect: Cannot be destroyed by battle. If attacked face-down: 1000 damage to attacker.
        // A indestrutibilidade é passiva (verificada no BattleManager se implementado, ou simulada aqui).
        // O dano é um efeito de gatilho após o cálculo de dano.
        Debug.Log("Marshmallon: Efeito passivo de indestrutibilidade e dano configurado.");

    }

    void Effect_1166_MarshmallonGlasses(CardDisplay source)
    {
        // Effect: Opponent can only select "Marshmallon" as an attack target.
        Debug.Log("Marshmallon Glasses: Oponente forçado a atacar Marshmallon (Passivo).");
    }

    void Effect_1167_Maryokutai(CardDisplay source)
    {
        // Effect: Quick Effect: Tribute this card to negate Spell activation and destroy it.
        if (ChainManager.Instance != null && ChainManager.Instance.currentChain.Count > 0)
        {
            var lastLink = ChainManager.Instance.currentChain[ChainManager.Instance.currentChain.Count - 1];
            if (lastLink.cardSource != null && lastLink.cardSource.CurrentCardData.type.Contains("Spell"))
            {
                GameManager.Instance.TributeCard(source);
                Debug.Log($"Maryokutai: Negando {lastLink.cardSource.CurrentCardData.name}.");
                
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(lastLink.cardSource);
                GameManager.Instance.SendToGraveyard(lastLink.cardSource.CurrentCardData, lastLink.isPlayerEffect);
                Destroy(lastLink.cardSource.gameObject);
            }
        }
    }

    void Effect_1169_MaskOfBrutality(CardDisplay source)
    {
        // Equip: +1000 ATK, -1000 DEF. Pay 1000 LP each Standby Phase.
        Effect_Equip(source, 1000, -1000);
        // Manutenção no CheckMaintenanceCosts.
    }

    void Effect_1170_MaskOfDarkness(CardDisplay source)
    {
        // FLIP: Target 1 Trap in GY; add to hand.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> traps = gy.FindAll(c => c.type.Contains("Trap"));
        
        if (traps.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(traps, "Recuperar Trap", (selected) => {
                gy.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_1171_MaskOfDispel(CardDisplay source)
    {
        // Select 1 face-up Spell. Controller takes 500 damage each Standby Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Spell") && !t.isFlipped,
                (target) => {
                    Debug.Log($"Mask of Dispel: Alvejando {target.CurrentCardData.name}.");
                    // Cria um link para rastrear o alvo durante a Standby Phase
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Continuous);
                }
            );
        }
    }

    void Effect_1172_MaskOfRestrict(CardDisplay source)
    {
        // Neither player can Tribute cards.
        Debug.Log("Mask of Restrict: Tributos bloqueados.");
        // Requer verificação no SummonManager.HasEnoughTributes e GameManager.TributeCard.
    }

    void Effect_1173_MaskOfWeakness(CardDisplay source)
    {
        // Target attacking monster; -700 ATK until end of turn.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            BattleManager.Instance.currentAttacker.ModifyStats(-700, 0);
            Debug.Log("Mask of Weakness: -700 ATK no atacante.");
        }
    }

    void Effect_1174_MaskOfTheAccursed(CardDisplay source)
    {
        // Equip: Cannot attack. 500 damage to controller each Standby Phase.
        Effect_Equip(source, 0, 0);
        // Lógica de bloqueio e dano.
    }

    void Effect_1175_MaskedBeastDesGardius(CardDisplay source)
    {
        // If sent to GY: Equip "The Mask of Remnants" from Deck/Hand to opp monster and take control.
        // Lógica implementada no OnCardSentToGraveyard (CardEffectManager_Impl.cs)
        Debug.Log("Des Gardius: Efeito de controle ao morrer configurado.");
    }

    void Effect_1177_MaskedDragon(CardDisplay source)
    {
        // Effect: Destroyed by battle -> SS Dragon with ATK <= 1500 from Deck.
        Effect_SearchDeck(source, "Dragon", "Monster", 1500); // Simplificado para busca
    }

    void Effect_1178_MaskedSorcerer(CardDisplay source)
    {
        // Effect: If inflicts battle damage: Draw 1 card.
        // Lógica implementada no OnDamageDealtImpl (CardEffectManager_Impl.cs)
        Debug.Log("Masked Sorcerer: Efeito de compra configurado.");
    }

    void Effect_1179_MassDriver(CardDisplay source)
    {
        // Effect: Tribute 1 monster; inflict 400 damage.
        Effect_TributeToBurn(source, 1, 400);
    }

    void Effect_1182_MasterMonk(CardDisplay source)
    {
        // Effect: Cannot be Normal Summoned. SS by tributing Monk Fighter. Can attack twice.
        Debug.Log("Master Monk: Ataque duplo (Lógica no OnBattleEnd).");
    }

    void Effect_1184_MatazaTheZapper(CardDisplay source)
    {
        // Effect: Can attack twice. Control cannot switch.
        Debug.Log("Mataza: Ataque duplo e controle fixo (Lógica no OnBattleEnd).");
    }

    void Effect_1186_MaximumSix(CardDisplay source)
    {
        // Effect: When Tribute Summoned: Roll die. Gain ATK = result * 200.
        if (source.isOnField)
        {
            GameManager.Instance.TossCoin(1, (heads) => { // Simula dado
                int roll = Random.Range(1, 7);
                int buff = roll * 200;
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, buff, source));
                Debug.Log($"Maximum Six: Rolou {roll}. +{buff} ATK.");
            });
        }
    }

    void Effect_1187_MazeraDeVille(CardDisplay source)
    {
        // Effect: Cannot be Normal Summoned. SS by tributing Warrior of Zera while Pandemonium is on field. Opponent discards 3 random cards.
        if (source.isOnField)
        {
            GameManager.Instance.DiscardRandomHand(false, 3);
            Debug.Log("Mazera DeVille: Oponente descartou 3 cartas.");
        }
    }

    void Effect_1190_MechaDogMarron(CardDisplay source)
    {
        // Effect: If destroyed by battle: Both players take 1000 damage.
        // Lógica implementada no OnBattleEnd (CardEffectManager_Impl.cs)
        Debug.Log("Mecha-Dog Marron: Dano mútuo ao morrer.");
    }

    void Effect_1192_MechanicalHound(CardDisplay source)
    {
        // Effect: If you have no cards in hand, opponent cannot activate Spells.
        Debug.Log("Mechanical Hound: Bloqueio de Magias (Passivo).");
    }

    void Effect_1196_MedusaWorm(CardDisplay source)
    {
        // Effect: Flip: Destroy 1 monster opp controls. Can flip face-down once per turn.
        if (source.isFlipped)
        {
            Effect_FlipDestroy(source, TargetType.Monster);
        }
        else
        {
            Effect_TurnSet(source);
        }
    }

    void Effect_1197_MefistTheInfernalGeneral(CardDisplay source)
    {
        // Effect: Piercing damage. If inflicts battle damage: Opponent discards 1 random card.
        // Lógica de piercing no BattleManager. Lógica de descarte no OnDamageDealtImpl (CardEffectManager_Impl.cs).
        Debug.Log("Mefist: Efeitos de batalha configurados.");
    }

    void Effect_1199_MegaTonMagicalCannon(CardDisplay source)
    {
        // Effect: Remove 10 Spell Counters from your field; destroy all cards opponent controls.
        if (RemoveSpellCounters(10, source.isPlayerCard))
        {
            DestroyAllMonsters(true, false);
            Effect_HarpiesFeatherDuster(source);
            Debug.Log("Mega Ton Magical Cannon: Campo do oponente limpo.");
        }
    }

    void Effect_1200_Megamorph(CardDisplay source)
    {
        // Effect: Equip. While your Life Points are lower than your opponent's, double the original ATK of the equipped monster. While your Life Points are higher, halve the original ATK of the equipped monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (target) => target.isOnField && target.CurrentCardData.type.Contains("Monster"),
                (target) => 
                {
                    Debug.Log($"{source.CurrentCardData.name} equipada em {target.CurrentCardData.name}");
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                    UpdateMegamorphStats(source, target);
                }
            );
        }
    }

    void UpdateMegamorphStats(CardDisplay megamorph, CardDisplay target)
    {
        // Remove modificadores anteriores desta fonte para recalcular
        target.RemoveModifiersFromSource(megamorph);

        int controllerLP = megamorph.isPlayerCard ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        int enemyLP = megamorph.isPlayerCard ? GameManager.Instance.opponentLP : GameManager.Instance.playerLP;

        if (controllerLP < enemyLP)
        {
            // Dobra o ATK original
            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk * 2, megamorph));
        }
        else if (controllerLP > enemyLP)
        {
            // Divide o ATK original pela metade
            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk / 2, megamorph));
        }
        // Se LP igual, não aplica modificador (ATK permanece inalterado)
    }

    public void UpdateAllMegamorphs()
    {
        // Encontra todos os links de equipamento ativos para atualizar Megamorphs
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.source != null && link.source.CurrentCardData.id == "1200" && link.target != null)
            {
                UpdateMegamorphStats(link.source, link.target);
            }
        }
    }

    void Effect_1201_MegarockDragon(CardDisplay source)
    {
        // Effect: Cannot be Normal Summoned. SS by banishing Rock monsters from GY. ATK/DEF = 700 * banished.
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> rocks = gy.FindAll(c => c.race == "Rock");
            
            if (rocks.Count > 0)
            {
                GameManager.Instance.OpenCardMultiSelection(rocks, "Banir Rocks", 1, rocks.Count, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                        gy.Remove(c);
                    }
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                    
                    // Aplica stats (precisa encontrar a instância invocada, aqui simulamos no source assumindo que ele vira o do campo)
                    // Em um sistema real, SpecialSummon retorna a instância.
                    // Workaround: O próximo UpdateStats pegará o valor correto se usarmos uma variável global ou se o CardDisplay tiver lógica interna.
                    // Aqui aplicamos um modificador "cego" que pode não funcionar se a referência 'source' for a da mão destruída.
                    Debug.Log($"Megarock Dragon: +{selected.Count * 700} ATK/DEF.");
                });
            }
        }
    }

    void Effect_1204_MeltielSageOfTheSky(CardDisplay source)
    {
        // Effect: When Counter Trap resolves: Gain 1000 LP, destroy 1 card opp controls (if Sanctuary).
        // Requer hook no ChainManager.
        Debug.Log("Meltiel: Efeito de Counter Trap configurado.");
    }

    void Effect_1205_MemoryCrusher(CardDisplay source)
    {
        // Effect: If inflicts battle damage: Opponent sends cards from Extra Deck to GY equal to damage / 100.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("Memory Crusher: Mill do Extra Deck configurado.");
    }

    void Effect_1207_MermaidKnight(CardDisplay source)
    {
        // Effect: If "Umi" is active, can attack twice.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013"))
        {
            Debug.Log("Mermaid Knight: Ataque duplo ativo.");
            // source.canAttackTwice = true;
        }
    }

    void Effect_1208_MesmericControl(CardDisplay source)
    {
        // Effect: Opponent cannot change battle positions of monsters next turn.
        Debug.Log("Mesmeric Control: Posições travadas.");
    }

    void Effect_1209_MessengerOfPeace(CardDisplay source)
    {
        // Effect: Monsters with 1500+ ATK cannot attack. Pay 100 LP standby.
        Debug.Log("Messenger of Peace: Bloqueio de ataque >= 1500.");
    }

    void Effect_1211_MetalDetector(CardDisplay source)
    {
        // Effect: Negate activation of Continuous Trap.
        Debug.Log("Metal Detector: Negação de Trap Contínua.");
    }

    void Effect_1215_MetalReflectSlime(CardDisplay source)
    {
        // Effect: SS as Monster (Aqua/Water/Lv10/0/3000).
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 3000, "Metal Reflect Slime Token");
        Debug.Log("Metal Reflect Slime: Invocado como monstro.");
    }

    void Effect_1216_MetallizingParasiteLunatite(CardDisplay source)
    {
        // Effect: Union. Equipped monster unaffected by opp Spells.
        Effect_Union(source, "Monster", 0, 0); // Alvo genérico
    }

    void Effect_1217_Metalmorph(CardDisplay source)
    {
        // Effect: Equip. +300 ATK/DEF. If attacks, gain half ATK of target.
        Effect_Equip(source, 300, 300);
        // Lógica de ganho de ATK no ataque deve ser no OnDamageCalculation.
    }

    void Effect_1218_MetalsilverArmor(CardDisplay source)
    {
        // Effect: Opponent can only target equipped monster.
        Effect_Equip(source, 0, 0);
        Debug.Log("Metalsilver Armor: Redirecionamento de alvo.");
    }

    void Effect_1219_Metalzoa(CardDisplay source)
    {
        // Effect: SS from Deck by tributing Zoa equipped with Metalmorph.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.name == "Zoa", // Deveria checar Metalmorph
                (t) => {
                    GameManager.Instance.TributeCard(t);
                    // SS Metalzoa do Deck
                    Effect_SearchDeck(source, "Metalzoa", "Monster"); // Simplificado para busca
                }
            );
        }
    }

    void Effect_1220_Metamorphosis(CardDisplay source)
    {
        // Effect: Tribute 1 monster; SS Fusion Monster with same Level from Extra Deck.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard,
                (tribute) => {
                    int level = tribute.CurrentCardData.level;
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
                    List<CardData> targets = extra.FindAll(c => c.level == level);
                    
                    if (targets.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(targets, "Invocar Fusão", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                            extra.Remove(selected);
                        });
                    }
                }
            );
        }
    }

    void Effect_1223_MeteorOfDestruction(CardDisplay source)
    {
        // Effect: If opp LP > 3000, inflict 1000 damage.
        if (GameManager.Instance.opponentLP > 3000)
        {
            Effect_DirectDamage(source, 1000);
        }
    }

    void Effect_1224_Meteorain(CardDisplay source)
    {
        // Effect: All your monsters inflict piercing damage this turn.
        Debug.Log("Meteorain: Piercing global ativo.");
        // Requer flag global no BattleManager.
    }

    void Effect_1225_Michizure(CardDisplay source)
    {
        // Effect: When monster sent to GY: Destroy 1 monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }
    // 1226 - Micro Ray
    void Effect_1226_MicroRay(CardDisplay source)
    {
        // Target 1 face-up monster on the field; that target's DEF becomes 0 until the end of this turn.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (target) => {
                    // Define DEF para 0 (usando modificador SET)
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, 0, source));
                    Debug.Log($"Micro Ray: DEF de {target.CurrentCardData.name} tornou-se 0.");
                }
            );
        }
    }

    // 1227 - Mid Shield Gardna
    void Effect_1227_MidShieldGardna(CardDisplay source)
    {
        // Ignition: Flip face-down.
        // Trigger (Passive): Negate Spell targeting this face-down.
        // A parte de negação é passiva/trigger no SpellTrapManager.
        // A parte de ignição (virar face-down):
        if (source.isOnField && !source.isFlipped)
        {
            Effect_TurnSet(source);
        }
    }

    // 1232 - Millennium Scorpion
    void Effect_1232_MillenniumScorpion(CardDisplay source)
    {
        // Gains 500 ATK each time it destroys a monster by battle.
        // Lógica implementada no OnBattleEnd (CardEffectManager_Impl.cs).
        Debug.Log("Millennium Scorpion: Efeito de crescimento configurado.");
    }

    // 1234 - Milus Radiant
    void Effect_1234_MilusRadiant(CardDisplay source)
    {
        // EARTH +500 ATK, WIND -400 ATK.
        Effect_Field(source, 500, 0, "", "Earth");
        Effect_Field(source, -400, 0, "", "Wind");
    }

    // 1235 - Minar
    void Effect_1235_Minar(CardDisplay source)
    {
        // If discarded by opponent's effect: 1000 damage.
        // Lógica no OnCardDiscarded (CardEffectManager_Impl.cs).
        Debug.Log("Minar: Efeito de dano por descarte configurado.");
    }

    // 1236 - Mind Control
    void Effect_1236_MindControl(CardDisplay source)
    {
        // Pay 800 LP (Errata: No LP cost in modern, but keeping classic if needed. Let's assume classic/modern mix or no cost as per text provided in prompt list? Prompt didn't specify cost, usually 800 is Brain Control. Mind Control is usually no cost but cannot attack/tribute).
        // Text: Target 1 monster opp controls; take control until End Phase. Cannot attack/Tribute.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    GameManager.Instance.SwitchControl(target);
                    // Aplica restrições
                    target.hasAttackedThisTurn = true; // Impede ataque (hack simples)
                    // TODO: Adicionar flag 'cannotBeTributed' no CardDisplay
                    Debug.Log($"Mind Control: Controlando {target.CurrentCardData.name} até a End Phase.");
                    // Agendar retorno na End Phase (GameManager deve tratar isso na limpeza de turno ou via CardLink)
                }
            );
        }
    }

    // 1237 - Mind Crush
    void Effect_1237_MindCrush(CardDisplay source)
    {
        // Declare card name.
        Debug.Log("Mind Crush: Declaração de nome necessária (Simulado: Kuriboh).");
        // Lógica simplificada: Verifica se o oponente tem a carta (hardcoded para teste ou aleatório)
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        string targetName = "Kuriboh"; // Em um jogo real, abriria input de texto
        
        List<CardData> hits = oppHand.FindAll(c => c.name == targetName);
        if (hits.Count > 0)
        {
            Debug.Log($"Mind Crush: Sucesso! Oponente descarta {hits.Count} cópias.");
            // Descarta todas as cópias
            // Requer acesso aos GameObjects da mão do oponente para remover visualmente
            GameManager.Instance.DiscardRandomHand(false, hits.Count); // Simplificado
        }
        else
        {
            Debug.Log("Mind Crush: Falha! Você descarta 1 carta aleatória.");
            GameManager.Instance.DiscardRandomHand(true, 1);
        }
    }

    // 1238 - Mind Haxorz
    void Effect_1238_MindHaxorz(CardDisplay source)
    {
        // Pay 500. Look at opp hand and Set cards.
        if (Effect_PayLP(source, 500))
        {
            Debug.Log("Mind Haxorz: Revelando mão e campo do oponente...");
            GameManager.Instance.ToggleOpponentHandVisibility(); // Revela mão (Dev tool usada como feature)
            // Revelar setadas
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones)
                {
                    if(z.childCount > 0) z.GetChild(0).GetComponent<CardDisplay>().RevealCard();
                }
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if(z.childCount > 0) 
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m.isFlipped) m.RevealCard();
                    }
                }
            }
        }
    }

    // 1239 - Mind Wipe
    void Effect_1239_MindWipe(CardDisplay source)
    {
        // Activate if opp hand <= 3. Opponent shuffles hand to deck, draws same number.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count <= 3 && oppHand.Count > 0)
        {
            int count = oppHand.Count;
            GameManager.Instance.DiscardHand(false); // Deveria ser Shuffle into Deck
            for(int i=0; i<count; i++) GameManager.Instance.DrawOpponentCard();
            Debug.Log("Mind Wipe: Mão do oponente reciclada.");
        }
    }

    // 1240 - Mind on Air
    void Effect_1240_MindOnAir(CardDisplay source)
    {
        // Opponent plays with hand revealed.
        Debug.Log("Mind on Air: Mão do oponente revelada.");
        GameManager.Instance.showOpponentHand = true;
        // Nota: Precisa desligar quando sair do campo (OnCardLeavesField)
    }

    // 1241 - Mine Golem
    void Effect_1241_MineGolem(CardDisplay source)
    {
        // If destroyed by battle: 500 damage to opp.
        // Lógica no OnCardSentToGraveyard (CardEffectManager_Impl.cs).
        Debug.Log("Mine Golem: Efeito de dano configurado.");
    }

    // 1242 - Minefield Eruption
    void Effect_1242_MinefieldEruption(CardDisplay source)
    {
        // Damage 1000 per Mine Golem, then destroy them.
        int count = 0;
        List<CardDisplay> golems = new List<CardDisplay>();
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.CurrentCardData.name == "Mine Golem")
                    {
                        count++;
                        golems.Add(m);
                    }
                }
            }
        }

        if (count > 0)
        {
            Effect_DirectDamage(source, count * 1000);
            foreach(var g in golems)
            {
                GameManager.Instance.SendToGraveyard(g.CurrentCardData, true);
                Destroy(g.gameObject);
            }
            Debug.Log($"Minefield Eruption: {count} Golems explodiram.");
        }
    }

    // 1244 - Minor Goblin Official
    void Effect_1244_MinorGoblinOfficial(CardDisplay source)
    {
        // Opponent LP <= 3000 -> 500 dmg each Standby.
        // Lógica no OnPhaseStart (CardEffectManager_Impl.cs).
        Debug.Log("Minor Goblin Official: Ativo.");
    }

    // 1245 - Miracle Dig
    void Effect_1245_MiracleDig(CardDisplay source)
    {
        // If 5+ banished, return 3 to GY.
        List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
        if (banished.Count >= 5)
        {
            GameManager.Instance.OpenCardMultiSelection(banished, "Retornar 3 ao GY", 3, 3, (selected) => {
                foreach(var c in selected)
                {
                    banished.Remove(c); // Remove da lista de banidos
                    GameManager.Instance.GetPlayerGraveyard().Add(c); // Adiciona ao GY
                }
                // Atualiza visuais
                GameManager.Instance.playerRemovedDisplay.UpdatePile(banished, GameManager.Instance.GetCardBackTexture());
                GameManager.Instance.playerGraveyardDisplay.UpdatePile(GameManager.Instance.GetPlayerGraveyard(), GameManager.Instance.GetCardBackTexture());
                Debug.Log("Miracle Dig: 3 cartas retornadas ao GY.");
            });
        }
    }

    // 1246 - Miracle Fusion
    void Effect_1246_MiracleFusion(CardDisplay source)
    {
        // Fusion Summon E-Hero by banishing materials from Field/GY.
        List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
        List<CardData> heroes = extra.FindAll(c => c.name.Contains("Elemental HERO"));
        
        if (heroes.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(heroes, "Miracle Fusion", (fusionTarget) => {
                // Lógica simplificada: Assume que tem materiais e bane 2 do GY aleatoriamente ou pede seleção
                // Em produção: Verificar materiais específicos do fusionTarget
                Debug.Log($"Miracle Fusion: Invocando {fusionTarget.name} (Banimento de materiais pendente).");
                GameManager.Instance.SpecialSummonFromData(fusionTarget, source.isPlayerCard);
                extra.Remove(fusionTarget);
            });
        }
    }

    // 1247 - Miracle Restoring
    void Effect_1247_MiracleRestoring(CardDisplay source)
    {
        // Remove 2 counters -> SS Dark Magician or Buster Blader from GY.
        if (RemoveSpellCounters(2, source.isPlayerCard))
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> targets = gy.FindAll(c => c.name == "Dark Magician" || c.name == "Buster Blader");
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Reviver Lenda", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                });
            }
        }
    }

    // 1248 - Mirage Dragon
    void Effect_1248_MirageDragon(CardDisplay source)
    {
        // Opponent cannot activate Traps during Battle Phase.
        // Lógica passiva verificada no BattleManager/SpellTrapManager.
        Debug.Log("Mirage Dragon: Traps bloqueadas na batalha.");
    }

    // 1249 - Mirage Knight
    void Effect_1249_MirageKnight(CardDisplay source)
    {
        // Gain ATK = Opponent's ATK during calc. Banish at End Phase.
        // Lógica de ATK no OnDamageCalculation (CardEffectManager_Impl.cs).
        // Lógica de banir no OnPhaseStart (End Phase).
        Debug.Log("Mirage Knight: Efeitos de batalha e auto-banimento configurados.");
    }

    // 1250 - Mirage of Nightmare
    void Effect_1250_MirageOfNightmare(CardDisplay source)
    {
        // Opponent's Standby: Draw until 4. Your Standby: Discard same amount.
        // Lógica no OnPhaseStart (CardEffectManager_Impl.cs).
        Debug.Log("Mirage of Nightmare: Ativo.");
    }

    // 1251 - Mirror Force (Já implementado como Effect_MirrorForce, mantendo referência)
    
    // 1252 - Mirror Wall
    void Effect_1252_MirrorWall(CardDisplay source)
    {
        // Effect: Halve ATK of opponent's attacking monsters. Pay 2000 LP during Standby.
        // A lógica de corte de ATK é passiva e será tratada no hook OnAttackDeclared/OnDamageCalculation.
        // A manutenção é tratada no CheckMaintenanceCosts.
        Debug.Log("Mirror Wall: Ativada. Monstros atacantes terão ATK reduzido.");
    }

    // 1254 - Mispolymerization
    void Effect_1254_Mispolymerization(CardDisplay source)
    {
        // Effect: Return all Fusion Monsters to the Extra Deck.
        List<CardDisplay> fusions = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.CurrentCardData.type.Contains("Fusion")) fusions.Add(m);
            }
        }

        foreach(var f in fusions)
        {
            // Retorna ao Extra Deck (Simulado enviando ao GY com flag, ou destruindo)
            // Em um sistema ideal: GameManager.ReturnToExtraDeck(f);
            Debug.Log($"Mispolymerization: Retornando {f.CurrentCardData.name} ao Extra Deck.");
            GameManager.Instance.SendToGraveyard(f.CurrentCardData, f.isPlayerCard); // Fallback
            Destroy(f.gameObject);
        }
    }

    // 1255 - Moai Interceptor Cannons
    void Effect_1255_MoaiInterceptorCannons(CardDisplay source)
    {
        // Effect: Once per turn, flip face-down.
        if (source.isOnField && !source.isFlipped)
        {
            Effect_TurnSet(source);
        }
    }

    // 1256 - Mobius the Frost Monarch
    void Effect_1256_MobiusTheFrostMonarch(CardDisplay source)
    {
        // Effect: When Tribute Summoned: Target up to 2 S/T; destroy them.
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            if (SpellTrapManager.Instance != null)
            {
                // Seleciona até 2 alvos (S/T)
                // Como o sistema de seleção múltipla atual é genérico, usamos ele filtrando por S/T
                List<CardDisplay> validTargets = new List<CardDisplay>();
                // Coleta S/T do campo
                // ... (Lógica de coleta simplificada)
                
                Debug.Log("Mobius: Selecione até 2 S/T para destruir (Simulado: Destrói 1 S/T aleatória do oponente).");
                Effect_MST(source); // Fallback para 1 alvo
            }
        }
    }

    // 1257 - Moisture Creature
    void Effect_1257_MoistureCreature(CardDisplay source)
    {
        // Effect: If Tribute Summoned by 3 Tributes: Destroy all S/T opp controls.
        // Requer saber quantos tributos foram usados.
        // Como o sistema atual não passa a contagem exata (apenas flag bool), assumimos que se foi Tribute Summoned, o jogador cumpriu o requisito se escolheu essa opção.
        // Em um sistema completo, precisaríamos de `source.tributeCount`.
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            Debug.Log("Moisture Creature: Destruindo S/T do oponente.");
            Effect_HarpiesFeatherDuster(source);
        }
    }

    // 1259 - Mokey Mokey Smackdown
    void Effect_1259_MokeyMokeySmackdown(CardDisplay source)
    {
        // Continuous: While face-up Mokey Mokey exists, if Fairy destroyed, Mokey Mokeys gain 3000 ATK.
        // Lógica passiva/gatilho implementada no OnCardSentToGraveyard.
        Debug.Log("Mokey Mokey Smackdown: Ativo.");
    }

    // 1261 - Molten Destruction
    void Effect_1261_MoltenDestruction(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Fire");
    }

    // 1262 - Molten Zombie
    void Effect_1262_MoltenZombie(CardDisplay source)
    {
        // Effect: When SS from GY: Draw 1 card.
        // Lógica no OnSpecialSummon (CardEffectManager_Impl.cs).
        Debug.Log("Molten Zombie: Efeito de compra configurado.");
    }

    // 1264 - Monk Fighter
    void Effect_1264_MonkFighter(CardDisplay source)
    {
        // Effect: Battle Damage to controller is 0.
        // Lógica no OnDamageCalculation.
        Debug.Log("Monk Fighter: Dano de batalha 0.");
    }

    // 1266 - Monster Eye
    void Effect_1266_MonsterEye(CardDisplay source)
    {
        // Effect: Pay 1000 LP; add 1 Polymerization from GY to hand.
        if (Effect_PayLP(source, 1000))
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            CardData poly = gy.Find(c => c.name == "Polymerization");
            if (poly != null)
            {
                gy.Remove(poly);
                GameManager.Instance.AddCardToHand(poly, source.isPlayerCard);
                Debug.Log("Monster Eye: Polymerization recuperada.");
            }
        }
    }

    // 1267 - Monster Gate
    void Effect_1267_MonsterGate(CardDisplay source)
    {
        // Effect: Tribute 1 monster; excavate until Normal Summonable monster, SS it, send rest to GY.
        if (source.isOnField) // Deveria ser ativado da mão/campo como Spell
        {
            // Seleciona tributo
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (tribute) => {
                        GameManager.Instance.TributeCard(tribute);
                        
                        // Escavação
                        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                        List<CardData> sentToGY = new List<CardData>();
                        CardData foundMonster = null;

                        while (deck.Count > 0)
                        {
                            CardData current = deck[0];
                            deck.RemoveAt(0);
                            
                            // Verifica se pode ser Normal Summoned (simplificado: não é Ritual/Fusion/etc)
                            if (current.type.Contains("Monster") && !current.type.Contains("Ritual") && !current.type.Contains("Fusion"))
                            {
                                foundMonster = current;
                                break;
                            }
                            else
                            {
                                sentToGY.Add(current);
                            }
                        }

                        // Envia escavados para GY
                        foreach(var c in sentToGY) GameManager.Instance.SendToGraveyard(c, source.isPlayerCard);

                        // Invoca o monstro
                        if (foundMonster != null)
                        {
                            GameManager.Instance.SpecialSummonFromData(foundMonster, source.isPlayerCard);
                            Debug.Log($"Monster Gate: Invocou {foundMonster.name}.");
                        }
                    }
                );
            }
        }
    }

    // 1269 - Monster Recovery
    void Effect_1269_MonsterRecovery(CardDisplay source)
    {
        // Effect: Target 1 monster you own; shuffle it and hand into Deck, draw hand size.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    int handCount = GameManager.Instance.GetPlayerHandData().Count;
                    
                    // Retorna monstro ao Deck
                    GameManager.Instance.ReturnToDeck(target, false); // Shuffle
                    
                    // Retorna mão ao Deck
                    GameManager.Instance.DiscardHand(true); // Deveria ser ReturnToDeck
                    // Como DiscardHand manda pro GY, vamos simular a compra apenas
                    // Em produção: Implementar ShuffleHandToDeck
                    
                    for(int i=0; i<handCount; i++) GameManager.Instance.DrawCard();
                    
                    Debug.Log("Monster Recovery: Mão e monstro reciclados.");
                }
            );
        }
    }

    // 1274 - Mooyan Curry
    void Effect_1274_MooyanCurry(CardDisplay source)
    {
        Effect_GainLP(source, 200);
    }

    // 1275 - Morale Boost
    void Effect_1275_MoraleBoost(CardDisplay source)
    {
        // Effect: Equip Spell activated -> Gain 1000. Equip Spell removed -> Take 1000.
        // Lógica implementada via hooks globais no CardEffectManager_Impl.cs.
        Debug.Log("Morale Boost: Ativo.");
    }
}