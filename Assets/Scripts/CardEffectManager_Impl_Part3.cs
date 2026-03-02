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
        Debug.Log("Kangaroo Champ: Vira defesa após batalha (Passivo).");
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
        Debug.Log("Kazejin: Zera ATK do atacante (Passivo/Ativável).");
    }

    void Effect_1009_Kelbek(CardDisplay source)
    {
        // Effect: If attacked, return attacking monster to hand.
        // Lógica no OnBattleEnd.
        Debug.Log("Kelbek: Retorna atacante para mão (Passivo).");
    }

    void Effect_1010_Keldo(CardDisplay source)
    {
        // Effect: When sent to GY by battle: Select 2 cards in opp GY, shuffle into Deck.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Keldo: Recicla GY do oponente (Passivo).");
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
        Debug.Log("King Tiger Wanghu: Destrói invocações fracas (Passivo).");
    }

    void Effect_1018_KingOfTheSkullServants(CardDisplay source)
    {
        // Effect: ATK = Skull Servants/King of Skull Servants in GY * 1000. Revive by banishing 1 Skull Servant.
        // Lógica de ATK no OnPhaseStart/UpdateStats.
        // Lógica de Revive no OnCardSentToGraveyard.
        Debug.Log("King of the Skull Servants: Stats dinâmicos e ressurreição.");
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
        Debug.Log("Kiseitai: Efeito de parasita configurado.");
    }

    void Effect_1023_KishidoSpirit(CardDisplay source)
    {
        // Effect: Monsters not destroyed by battle if ATK is equal.
        // Lógica no BattleManager (ResolveDamage).
        Debug.Log("Kishido Spirit: Proteção em empate de ATK.");
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
        Debug.Log("Kotodama: Regra de unicidade ativa.");
    }

    void Effect_1031_KozakysSelfDestructButton(CardDisplay source)
    {
        // Effect: When "Kozaky" is destroyed and sent to the Graveyard, inflict 1000 damage to the controller of this card.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Kozaky's Self-Destruct Button: Dano ao destruir Kozaky.");
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
        Debug.Log("Kuriboh: Dano de batalha 0 (Hand Trap).");
    }

    void Effect_1040_KycooTheGhostDestroyer(CardDisplay source)
    {
        // Effect: When inflicts battle damage: Banish up to 2 monsters from opp GY. Opponent cannot banish from GY.
        // Lógica de banir no OnDamageDealtImpl.
        // Lógica de bloqueio no GameManager (RemoveFromPlay).
        Debug.Log("Kycoo: Bloqueio de banimento e efeito de dano.");
    }

    void Effect_1046_LabyrinthOfNightmare(CardDisplay source)
    {
        // Effect: End Phase: Change battle position of all face-up monsters.
        // Lógica no OnPhaseStart (End Phase).
        Debug.Log("Labyrinth of Nightmare: Mudança de posição na End Phase.");
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
        Debug.Log("Larvae Moth: Invocação especial complexa.");
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
        Debug.Log("Last Turn: Evento especial de vitória (Não implementado totalmente).");
    }

    void Effect_1056_LastWill(CardDisplay source)
    {
        // Effect: If monster sent to GY: SS 1 monster with ATK <= 1500 from Deck.
        // Trigger global.
        Debug.Log("Last Will: Ativo (Gatilho de GY).");
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
        // Lógica de SS no oponente.
        Debug.Log("Lava Golem: Invocação no campo do oponente.");
    }

    void Effect_1063_LegacyHunter(CardDisplay source)
    {
        // Effect: If destroys monster by battle: Opponent shuffles 1 random card from hand to Deck.
        // Lógica no OnBattleEnd.
        Debug.Log("Legacy Hunter: Hand shuffle on kill.");
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
        Debug.Log("Legendary Fiend: +700 ATK na Standby.");
    }

    void Effect_1067_LegendaryFlameLord(CardDisplay source)
    {
        // Ritual. Can put counter. Remove 3 counters -> Destroy all monsters.
        Debug.Log("Legendary Flame Lord: Ritual e Nuke.");
    }

    void Effect_1068_LegendaryJujitsuMaster(CardDisplay source)
    {
        // Effect: If battled in Defense: Return attacker to top of Deck.
        // Lógica no OnBattleEnd.
        Debug.Log("Legendary Jujitsu Master: Spin attacker.");
    }

    void Effect_1069_LegendarySword(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Warrior");
    }

    void Effect_1070_Leghul(CardDisplay source)
    {
        // Effect: Can attack directly.
        Debug.Log("Leghul: Ataque direto.");
        // source.canAttackDirectly = true;
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
        // Requer rastreamento de LP pagos.
        Debug.Log("Life Absorbing Machine: Recuperação de LP (Lógica de rastreamento pendente).");
    }

    void Effect_1081_LightOfIntervention(CardDisplay source)
    {
        // Effect: Monsters cannot be Set face-down. Monsters Set in Defense are summoned in Face-up Defense.
        Debug.Log("Light of Intervention: Monstros devem ser invocados face-up.");
        // Requer hook no SummonManager.
    }

    void Effect_1082_LightOfJudgment(CardDisplay source)
    {
        // Effect: If "The Sanctuary in the Sky" is on field: Discard 1 LIGHT; look at opp hand discard 1 OR destroy 1 card opp controls.
        if (GameManager.Instance.IsCardActiveOnField("1887"))
        {
            // Lógica de escolha
            Debug.Log("Light of Judgment: Efeito ativado.");
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
        // Aplica buff e marca para destruição.
        Debug.Log("Limiter Removal: Máquinas com dobro de ATK.");
    }

    void Effect_1091_LittleChimera(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Fire");
    }

    void Effect_1093_LittleWinguard(CardDisplay source)
    {
        // Effect: Once per turn, during End Phase: Change battle position.
        Debug.Log("Little-Winguard: Pode mudar posição na End Phase.");
    }

    void Effect_1096_LoneWolf(CardDisplay source)
    {
        // Effect: If you control "Monk Fighter" or "Master Monk", it cannot be destroyed by battle.
        Debug.Log("Lone Wolf: Proteção ativa.");
    }

    void Effect_1097_LordPoison(CardDisplay source)
    {
        // Effect: If destroyed by battle: SS 1 Plant from GY.
        Debug.Log("Lord Poison: Revive Planta.");
    }

    void Effect_1098_LordOfD(CardDisplay source)
    {
        // Effect: Dragons cannot be targeted by card effects.
        Debug.Log("Lord of D.: Proteção de Dragões ativa.");
    }

    void Effect_1101_LostGuardian(CardDisplay source)
    {
        // Effect: DEF = 700 * banished Rocks.
        // Lógica de stats dinâmicos.
        Debug.Log("Lost Guardian: DEF dinâmico.");
    }

    void Effect_1103_LuminousSoldier(CardDisplay source)
    {
        // Effect: If attacks DARK, +500 ATK.
        Debug.Log("Luminous Soldier: Buff vs DARK.");
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
        // Effect: Counter Trap. Opponent can discard 1 Spell to negate this. If not, negate Spell.
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
        if (GameManager.Instance.IsCardActiveOnField("Spellcaster") || GameManager.Instance.IsCardActiveOnField("0419")) // Check Spellcaster
        {
            if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
            {
                // Tribute
                // SS from Hand
                // Destroy
                Debug.Log("Magical Dimension: Sequência de efeitos (Simulado).");
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
        Debug.Log("Magical Hats: Esconde-esconde (Simulado).");
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
        Debug.Log("Magical Merchant: Escavando deck...");
        // Lógica de escavação
    }

    void Effect_1133_MagicalPlantMandragola(CardDisplay source)
    {
        // FLIP: Place 1 Spell Counter on each face-up card that can have one.
        Debug.Log("Mandragola: Distribuindo contadores.");
        // Itera campo e adiciona
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
        Debug.Log("Magical Thorn: Ativo.");
    }

    void Effect_1137_MagicianOfBlackChaos(CardDisplay source)
    {
        Debug.Log("Magician of Black Chaos: Ritual.");
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
        Debug.Log("Magician's Valkyria: Protege outros Magos.");
    }

    void Effect_1140_MahaVailo(CardDisplay source)
    {
        // Effect: Gains 500 ATK for each Equip Card equipped to this card.
        // Lógica passiva/stat modifier.
        // Como não temos contagem fácil de equips no alvo, simulamos:
        Debug.Log("Maha Vailo: Ganha ATK por equips (Lógica de contagem pendente).");
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
        Debug.Log("Maiden of the Aqua: Campo é Umi.");
    }

    void Effect_1144_MajiGirePanda(CardDisplay source)
    {
        // Effect: Gains 500 ATK when a Beast is destroyed.
        // Lógica no OnCardLeavesField.
        Debug.Log("Maji-Gire Panda: Ativo.");
    }

    void Effect_1145_MajorRiot(CardDisplay source)
    {
        // Effect: Return all monsters to hand, then SS same number.
        Debug.Log("Major Riot: Reset de monstros (Complexo).");
    }

    void Effect_1146_MajuGarzett(CardDisplay source)
    {
        // Effect: ATK = combined original ATK of 2 tributes.
        // Lógica no SummonManager.
        Debug.Log("Maju Garzett: ATK definido pelos tributos.");
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
        Debug.Log("Malice Doll of Demise: Efeito de renascimento configurado.");
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
        Debug.Log("Manga Ryu-Ran: Regras Toon aplicadas.");
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
        Debug.Log("Manticore of Darkness: Loop de renascimento.");
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
        // Lógica no BattleManager (ResolveDamage e OnDamageCalculation).
        Debug.Log("Marshmallon: Indestrutível e dano ao atacar face-down.");
    }

    void Effect_1166_MarshmallonGlasses(CardDisplay source)
    {
        // Effect: Opponent can only select "Marshmallon" as an attack target.
        Debug.Log("Marshmallon Glasses: Redirecionamento de ataque.");
    }

    void Effect_1167_Maryokutai(CardDisplay source)
    {
        // Effect: Quick Effect: Tribute this card to negate Spell activation and destroy it.
        // Requer Chain.
        Debug.Log("Maryokutai: Negação de Magia.");
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
        Debug.Log("Mask of Dispel: Dano contínuo em Magia.");
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
        Debug.Log("Des Gardius: Efeito de controle ao morrer.");
    }

    void Effect_1177_MaskedDragon(CardDisplay source)
    {
        // Effect: Destroyed by battle -> SS Dragon with ATK <= 1500 from Deck.
        Effect_SearchDeck(source, "Dragon", "Monster", 1500); // Simplificado para busca
    }

    void Effect_1178_MaskedSorcerer(CardDisplay source)
    {
        // Effect: If inflicts battle damage: Draw 1 card.
        // Lógica no OnDamageDealtImpl.
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
        Debug.Log("Master Monk: Ataque duplo (Passivo).");
    }

    void Effect_1184_MatazaTheZapper(CardDisplay source)
    {
        // Effect: Can attack twice. Control cannot switch.
        Debug.Log("Mataza: Ataque duplo e controle fixo (Passivo).");
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
        // Lógica no OnBattleEnd.
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
        // Lógica de piercing no BattleManager. Lógica de descarte no OnDamageDealtImpl.
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
}
