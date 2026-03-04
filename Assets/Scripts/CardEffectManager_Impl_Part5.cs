using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public partial class CardEffectManager
{   

    // =========================================================================================
    // LÓGICA PARA AS CARTAS (ID 2001 - 2025)
    // =========================================================================================

    // 2001 - Type Zero Magic Crusher
    void Effect_2001_TypeZeroMagicCrusher(CardDisplay source)
    {
        // Discard 1 Spell Card from your hand to inflict 500 damage to your opponent.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Effect_DirectDamage(source, 500);
            });
        }
    }

    // 2002 - Tyranno Infinity
    void Effect_2002_TyrannoInfinity(CardDisplay source)
    {
        // Original ATK = Banished Dinosaur-Type monsters x 1000.
        if (source.isOnField)
        {
            int count = 0;
            List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
            count += banished.FindAll(c => c.race == "Dinosaur").Count;
            // Nota: Deveria contar os do oponente também se a regra permitir (texto diz "your banished")
            
            int newAtk = count * 1000;
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Original, StatModifier.Operation.Set, newAtk, source));
            Debug.Log($"Tyranno Infinity: ATK definido para {newAtk} ({count} dinossauros banidos).");
        }
    }

    // 2003 - Tyrant Dragon
    void Effect_2003_TyrantDragon(CardDisplay source)
    {
        // Attack twice if opponent has monster. Negate Trap targeting it.
        // Lógica de ataque duplo no BattleManager (verificar flag canAttackAgain).
        // Lógica de negação de Trap:
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Trap") && link.target == source)
        {
            NegateAndDestroy(source, link);
        }
    }

    // 2004 - UFO Turtle
    void Effect_2004_UFOTurtle(CardDisplay source) // Based on user registry
    {
        // Destroyed by battle: SS FIRE <= 1500.
        Effect_SpecialSummonFromDeck(source, attribute: "Fire", maxAtk: 1500);
    }

    // 2005 - UFOroid
    void Effect_2005_UFOroid(CardDisplay source) // Based on user registry
    {
        // Destroyed by battle: SS Machine <= 1500.
        Effect_SpecialSummonFromDeck(source, race: "Machine", maxAtk: 1500);
    }

    // 2006 - UFOroid Fighter
    void Effect_2006_UFOroidFighter(CardDisplay source) // Based on user registry
    {
        // Fusion: ATK/DEF = Sum of original ATK of materials.
        // Requer sistema de fusão que passe os materiais.
        int totalAtk = 0;
        int totalDef = 0;
        foreach (var mat in source.fusionMaterialsUsed)
        {
            totalAtk += mat.atk;
            totalDef += mat.atk; // Regra: "Original ATK and DEF ... become the sum of the original ATK of the Fusion Material Monsters"
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Original, StatModifier.Operation.Set, totalAtk, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Original, StatModifier.Operation.Set, totalDef, source));
        Debug.Log($"UFOroid Fighter: Stats definidos para {totalAtk}/{totalDef}.");
    }

    // 2007 - Ultimate Baseball Kid
    void Effect_2007_UltimateBaseballKid(CardDisplay source) // Based on user registry
    {
        // Gain 1000 ATK for each other FIRE monster.
        // Send 1 other FIRE monster to GY to inflict 500 damage.
        
        // Efeito 1: Buff (Passivo/Contínuo - Atualizado no UpdateStats)
        int fireCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m != source && m.CurrentCardData.attribute == "Fire") fireCount++;
                }
            }
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, fireCount * 1000, source));

        // Efeito 2: Burn (Ignition)
        if (source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != source && t.CurrentCardData.attribute == "Fire",
                    (tribute) => {
                        GameManager.Instance.SendToGraveyard(tribute.CurrentCardData, tribute.isPlayerCard);
                        Destroy(tribute.gameObject);
                        Effect_DirectDamage(source, 500);
                    }
                );
            }
        }
    }

    // 2008 - Ultimate Insect LV1
    void Effect_2008_UltimateInsectLV1(CardDisplay source) // Based on user registry
    {
        // Unaffected by Spells. Standby Phase: Send to GY -> SS LV3.
        Effect_LevelUp(source, "2009");
    }

    // 2009 - Ultimate Insect LV3
    void Effect_2009_UltimateInsectLV3(CardDisplay source) // Based on user registry
    {
        // Debuff, Standby: SS LV5.
        Effect_LevelUp(source, "2010");
    }

    // 2010 - Ultimate Insect LV5
    void Effect_2010_UltimateInsectLV5(CardDisplay source) // Based on user registry
    {
        // If SS by LV3: Opp monsters lose 500 ATK. Standby: SS LV7.
        if (source.wasSpecialSummoned)
        {
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if(z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null) m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, -500, source));
                    }
                }
            }
        }
        Effect_LevelUp(source, "2011");
    }

    // 2011 - Ultimate Insect LV7
    void Effect_2011_UltimateInsectLV7(CardDisplay source) // Based on user registry
    {
        // If SS by LV5: Opp monsters lose 700 ATK/DEF.
        if (source.wasSpecialSummoned)
        {
             if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if(z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null)
                        {
                            m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, -700, source));
                            m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Field, StatModifier.Operation.Add, -700, source));
                        }
                    }
                }
            }
        }
    }

    // 2012 - Ultimate Obedient Fiend
    void Effect_2012_UltimateObedientFiend(CardDisplay source) // Based on user registry
    {
        // Can only attack if you have no hand and no other monsters.
        // Lógica no BattleManager.CanAttack.
        Debug.Log("Ultimate Obedient Fiend: Restrição de ataque.");
    }

    // 2013 - Ultimate Offering
    void Effect_2013_UltimateOffering(CardDisplay source) // Based on user registry
    {
        // Pay 500 LP: Normal Summon/Set extra.
        if (Effect_PayLP(source, 500))
        {
            Debug.Log("Ultimate Offering: Selecione um monstro da mão para invocar.");
            // Abre seleção da mão para invocar
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));
            
            if (monsters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(monsters, "Invocar Extra", (selected) => {
                    // Encontra o GameObject correspondente
                    GameObject cardGO = GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selected);
                    if (cardGO != null)
                    {
                        // Força invocação (ignora limite de 1 por turno)
                        GameManager.Instance.TrySummonMonster(cardGO, selected, false, true); // true = ignoreLimit
                    }
                });
            }
        }
    }

    // 2014 - Ultra Evolution Pill
    void Effect_2014_UltraEvolutionPill(CardDisplay source)
    {
        // Tribute 1 Reptile; SS 1 Dinosaur from hand.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Reptile",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> dinos = hand.FindAll(c => c.race == "Dinosaur" && c.type.Contains("Monster"));
                    
                    if (dinos.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(dinos, "Invocar Dinossauro", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                            GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    // 2015 - Umi
    void Effect_2015_Umi(CardDisplay source)
    {
        // Field Spell: Aqua, Fish, Sea Serpent, Thunder +200 ATK/DEF. Machine, Pyro -200.
        Effect_Field(source, 200, 200, "Aqua");
        Effect_Field(source, 200, 200, "Fish");
        Effect_Field(source, 200, 200, "Sea Serpent");
        Effect_Field(source, 200, 200, "Thunder");
        Effect_Field(source, -200, -200, "Machine");
        Effect_Field(source, -200, -200, "Pyro");
    }

    // 2016 - Umiiruka
    void Effect_2016_Umiiruka(CardDisplay source)
    {
        // Field Spell: WATER +500 ATK, -400 DEF.
        Effect_Field(source, 500, -400, "", "Water");
    }

    // 2017 - Union Attack
    void Effect_2017_UnionAttack(CardDisplay source)
    {
        // Target 1 monster; add ATK of all other Attack pos monsters. Cannot inflict battle damage.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.position == CardDisplay.BattlePosition.Attack,
                (target) => {
                    int totalAtk = 0;
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                        {
                            if(z.childCount > 0)
                            {
                                var m = z.GetChild(0).GetComponent<CardDisplay>();
                                if(m != null && m != target && m.position == CardDisplay.BattlePosition.Attack)
                                    totalAtk += m.currentAtk;
                            }
                        }
                    }
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, totalAtk, source));
                    target.cannotInflictBattleDamage = true;
                    Debug.Log($"Union Attack: +{totalAtk} ATK.");
                }
            );
        }
    }

    // 2018 - Union Rider
    void Effect_2018_UnionRider(CardDisplay source) // Based on user registry
    {
        // Take control of Union monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.description.Contains("Union") && t.position == CardDisplay.BattlePosition.Attack, // Face-up Union
                (target) => {
                    // Equipa o monstro do oponente no Union Rider
                    GameManager.Instance.EquipMonsterToMonster(target, source);
                    
                    // Ganha ATK/DEF do equipado
                    source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, target.originalAtk, source));
                    source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, target.originalDef, source));
                    
                    // Muda o dono para o player (para que vá para o GY do player se destruído? Não, regras de controle. Mas visualmente está na S/T do player)
                    target.isPlayerCard = source.isPlayerCard;
                }
            );
        }
    }

    // 2020 - United We Stand
    void Effect_2020_UnitedWeStand(CardDisplay source) // Based on user registry
    {
        // Equip: +800 ATK/DEF per face-up monster you control.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    int count = 0;
                    // Conta monstros do controlador da Spell
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
                        foreach(var z in zones) if(z.childCount > 0 && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) count++;
                    }
                    
                    int buff = count * 800;
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, buff, source));
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, buff, source));
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                }
            );
        }
    }

    // 2021 - Unity
    void Effect_2021_Unity(CardDisplay source) // Based on user registry
    {
        // Select 1 monster; DEF becomes sum of all face-up DEF.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && !t.isFlipped,
                (target) => {
                    int totalDef = 0;
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                        {
                            if(z.childCount > 0)
                            {
                                var m = z.GetChild(0).GetComponent<CardDisplay>();
                                if(m != null && !m.isFlipped) totalDef += m.originalDef;
                            }
                        }
                    }
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, totalDef, source));
                    Debug.Log($"Unity: DEF definida para {totalDef}.");
                }
            );
        }
    }

    // 2023 - Unshaven Angler
    void Effect_2023_UnshavenAngler(CardDisplay source)
    {
        // Treated as 2 Tributes for WATER monster.
        Debug.Log("Unshaven Angler: Vale por 2 tributos para WATER.");
    }

    // 2024 - Upstart Goblin
    void Effect_2024_UpstartGoblin(CardDisplay source)
    {
        // Draw 1 card. Opponent gains 1000 LP.
        GameManager.Instance.DrawCard();
        GameManager.Instance.GainLifePoints(!source.isPlayerCard, 1000);
    }
    // 2027 - Valkyrion the Magna Warrior
    void Effect_2027_ValkyrionTheMagnaWarrior(CardDisplay source)
    {
        // Ignition: Tribute this to SS Alpha, Beta, Gamma from GY.
        if (source.isOnField)
        {
            UIManager.Instance.ShowConfirmation("Separar Valkyrion?", () => {
                GameManager.Instance.TributeCard(source);
                List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                string[] parts = { "Alpha The Magnet Warrior", "Beta The Magnet Warrior", "Gamma The Magnet Warrior" };
                foreach(var partName in parts)
                {
                    CardData part = gy.Find(c => c.name == partName);
                    if (part != null)
                    {
                        GameManager.Instance.SpecialSummonFromData(part, source.isPlayerCard);
                        gy.Remove(part);
                    }
                }
            });
        }
    }

    // 2028 - Vampire Baby
    void Effect_2028_VampireBaby(CardDisplay source)
    {
        // If destroys monster by battle: SS it to your field at End of Battle Phase.
        Debug.Log("Vampire Baby: Efeito de recrutamento configurado (Lógica no OnBattleEnd).");
    }

    // 2029 - Vampire Genesis
    void Effect_2029_VampireGenesis(CardDisplay source)
    {
        // Once per turn: Discard 1 Zombie; SS 1 Zombie from GY with Level < discarded.
        if (source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> zombies = hand.FindAll(c => c.race == "Zombie" && c.type.Contains("Monster"));
            
            if (zombies.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(zombies, "Descarte 1 Zumbi", (discarded) => {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                    
                    List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                    List<CardData> targets = gy.FindAll(c => c.race == "Zombie" && c.level < discarded.level);
                    
                    if (targets.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(targets, "Reviver Zumbi", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        });
                    }
                });
            }
        }
    }

    // 2030 - Vampire Lady
    void Effect_2030_VampireLady(CardDisplay source)
    {
        // If inflicts battle damage: Declare type; opp sends 1 from Deck to GY.
        Debug.Log("Vampire Lady: Efeito de mill configurado (Lógica no OnDamageDealtImpl).");
    }

    // 2031 - Vampire Lord
    void Effect_2031_VampireLord(CardDisplay source)
    {
        // If inflicts battle damage: Declare type; opp sends 1 from Deck to GY.
        // If destroyed by opp card effect: SS next Standby.
        Debug.Log("Vampire Lord: Mill no dano. Renascimento na Standby (OnCardSentToGraveyard).");
    }

    // 2032 - Vampire's Curse
    void Effect_2032_VampiresCurse(CardDisplay source)
    {
        // If destroyed by battle: Pay 500 LP; SS next Standby. Gains 500 ATK.
        Debug.Log("Vampire's Curse: Renascimento na Standby (OnCardSentToGraveyard).");
    }

    // 2033 - Vampiric Orchis
    void Effect_2033_VampiricOrchis(CardDisplay source)
    {
        // When Normal Summoned: SS 1 "Des Dendle" from hand.
        if (source.summonedThisTurn && !source.wasSpecialSummoned)
        {
            Effect_SearchDeck(source, "Des Dendle", "Monster"); // Should be SS from hand
        }
    }

    // 2034 - Van'Dalgyon the Dark Dragon Lord
    void Effect_2034_VanDalgyonTheDarkDragonLord(CardDisplay source)
    {
        // If Counter Trap negates: SS this card.
        Debug.Log("Van'Dalgyon: Invocação por Counter Trap (OnCounterTrapResolved).");
    }

    // 2035 - Vengeful Bog Spirit
    void Effect_2035_VengefulBogSpirit(CardDisplay source)
    {
        // Monsters cannot attack the turn they are Summoned.
        Debug.Log("Vengeful Bog Spirit: Enjoo de invocação ativo (Lógica no BattleManager).");
    }

    // 2037 - Versago the Destroyer
    void Effect_2037_VersagoTheDestroyer(CardDisplay source)
    {
        // Fusion Substitute.
        Debug.Log("Versago: Substituto de fusão (Lógica no FusionManager).");
    }

    // 2038 - Victory Dragon
    void Effect_2038_VictoryDragon(CardDisplay source)
    {
        // Direct attack reduces LP to 0 = Match Win.
        Debug.Log("Victory Dragon: Efeito de vitória de partida (Lógica no OnDamageDealtImpl).");
    }

    // 2039 - Vile Germs
    void Effect_2039_VileGerms(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Plant");
    }

    // 2040 - Vilepawn Archfiend
    void Effect_2040_VilepawnArchfiend(CardDisplay source)
    {
        // Opponent cannot attack other Archfiends.
        Debug.Log("Vilepawn Archfiend: Proteção de Archfiends (Lógica no BattleManager).");
    }

    // 2042 - Violet Crystal
    void Effect_2042_VioletCrystal(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Zombie");
    }

    // 2043 - Virus Cannon
    void Effect_2043_VirusCannon(CardDisplay source)
    {
        // Tribute any number of non-Tokens; opp sends equal Spells from Deck to GY.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            GameManager.Instance.TributeCard(source); // Seleção pendente
            GameManager.Instance.MillCards(!source.isPlayerCard, 1); // Deveria ser Spells do Deck
            Debug.Log("Virus Cannon: Magias enviadas ao GY.");
        }
    }

    // 2044 - Viser Des
    void Effect_2044_ViserDes(CardDisplay source)
    {
        // Normal Summon: Target 1 opp monster. Destroy it after 3 of your turns.
        if (source.summonedThisTurn && !source.wasSpecialSummoned)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard,
                    (target) => {
                        target.destructionTurnCountdown = 3;
                        target.destructionCountdownOwnerIsPlayer = source.isPlayerCard; // Countdown ticks on your turn
                        Debug.Log($"Viser Des: {target.CurrentCardData.name} marcado para destruição em 3 de seus turnos.");
                    }
                );
            }
        }
    }


    // 2047 - Waboku
    void Effect_2047_Waboku(CardDisplay source)
    {
        // No battle damage, monsters not destroyed by battle this turn.
        Debug.Log("Waboku: Proteção total de batalha este turno (Flags no BattleManager).");
    }

    // 2048 - Wall Shadow
    void Effect_2048_WallShadow(CardDisplay source)
    {
        // SS via Magical Labyrinth.
        Debug.Log("Wall Shadow: Invocado via Magical Labyrinth (Lógica no efeito do Labirinto).");
    }

    // 2049 - Wall of Illusion
    void Effect_2049_WallOfIllusion(CardDisplay source)
    {
        // If attacked: Return attacker to hand.
        Debug.Log("Wall of Illusion: Efeito de bounce configurado (Lógica no OnBattleEnd).");
    }

    // 2050 - Wall of Revealing Light
    void Effect_2050_WallOfRevealingLight(CardDisplay source)
    {
        // Pay multiple of 1000. Monsters with ATK <= paid cannot attack.
        int payment = 1000; // Simulação: Deveria abrir UI para escolher múltiplo
        if (Effect_PayLP(source, payment))
        {
            source.paidLifePoints = payment;
            Debug.Log("Wall of Revealing Light: Bloqueio de ataque <= 1000 (Lógica no BattleManager).");
        }
    }

        // 2051 - Wandering Mummy
    void Effect_2051_WanderingMummy(CardDisplay source)
    {
        // Once per turn: Flip face-down. Shuffle face-down Defense monsters.
        if (source.isOnField && !source.isFlipped)
        {
            Effect_TurnSet(source);
            Debug.Log("Wandering Mummy: Monstros face-down embaralhados (Visualmente).");
        }
    }

    // 2052 - War-Lion Ritual
    void Effect_2052_WarLionRitual(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 2054 - Warrior Elimination
    void Effect_2054_WarriorElimination(CardDisplay source)
    {
        Effect_DestroyType(source, "Warrior");
    }

    // 2055 - Warrior of Tradition
    void Effect_2055_WarriorOfTradition(CardDisplay source)
    {
        // Fusion Monster (Non-Effect).
        Debug.Log("Warrior of Tradition: Fusão sem efeito.");
    }

    // 2057 - Wasteland
    void Effect_2057_Wasteland(CardDisplay source)
    {
        Effect_Field(source, 200, 200, "Dinosaur");
        Effect_Field(source, 200, 200, "Zombie");
        Effect_Field(source, 200, 200, "Rock");
    }

    // 2058 - Watapon
    void Effect_2058_Watapon(CardDisplay source)
    {
        // If added to hand by effect: SS.
        // Lógica no OnCardAddedToHand (se implementado) ou checagem manual.
        Debug.Log("Watapon: Invocação automática ao ser adicionado à mão (OnCardAddedToHand).");
    }

    // 2065 - Wave-Motion Cannon
    void Effect_2065_WaveMotionCannon(CardDisplay source)
    {
        // Ignition: Send to GY -> Damage 1000 * Standby Phases passed.
        // O contador é incrementado no OnPhaseStart.
        if (source.isOnField)
        {
            UIManager.Instance.ShowConfirmation("Ativar efeito do Canhão de Onda-de-Movimento?", () => {
                int damage = source.turnCounter * 1000;
                Debug.Log($"Wave-Motion Cannon: Ativado com {source.turnCounter} contadores. Dano: {damage}");
                GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
                Destroy(source.gameObject);
                Effect_DirectDamage(source, damage);
            });
        }
    }

    // 2066 - Weapon Change
    void Effect_2066_WeaponChange(CardDisplay source)
    {
        // Pay 700. Swap ATK/DEF of Warrior/Machine.
        if (Effect_PayLP(source, 700))
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && (t.CurrentCardData.race == "Warrior" || t.CurrentCardData.race == "Machine"),
                    (target) => {
                        int atk = target.currentAtk;
                        int def = target.currentDef;
                        target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, def, source));
                        target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, atk, source));
                    }
                );
            }
        }
    }

    // 2068 - Weather Report
    void Effect_2068_WeatherReport(CardDisplay source)
    {
        // FLIP: Destroy all opp Swords of Revealing Light.
        Debug.Log("Weather Report: Destruindo Swords of Revealing Light (Lógica de busca pendente).");
    }

    // 2071 - Whirlwind Prodigy
    void Effect_2071_WhirlwindProdigy(CardDisplay source)
    {
        Debug.Log("Whirlwind Prodigy: Vale por 2 tributos para WIND.");
    }

    // 2073 - White Dragon Ritual
    void Effect_2073_WhiteDragonRitual(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 2074 - White Hole
    void Effect_2074_WhiteHole(CardDisplay source)
    {
        // When opp activates Dark Hole: Negate/Protect.
        Debug.Log("White Hole: Protege contra Dark Hole (Requer Chain).");
    }

    // 2075 - White Magical Hat
    void Effect_2075_WhiteMagicalHat(CardDisplay source)
    {
        // Battle Damage -> Opponent discards 1 random.
        Debug.Log("White Magical Hat: Efeito de descarte configurado (OnDamageDealtImpl).");
    }

    // 2076 - White Magician Pikeru
    void Effect_2076_WhiteMagicianPikeru(CardDisplay source)
    {
        // Standby Phase: Gain 400 LP per monster you control.
        // Lógica no OnPhaseStart.
        Debug.Log("White Magician Pikeru: Efeito de cura na Standby Phase configurado.");
    }

    // 2077 - White Ninja
    void Effect_2077_WhiteNinja(CardDisplay source)
    {
        // FLIP: Destroy 1 Defense Position monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Defense,
                (target) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                }
            );
        }
    }

    // 2079 - Wicked-Breaking Flamberge - Baou
    void Effect_2079_WickedBreakingFlambergeBaou(CardDisplay source)
    {
        // Send 1 card from hand to GY to equip. +500 ATK. Negate effects of destroyed monsters.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0 && !source.isOnField)
        {
             GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                 GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                 Effect_Equip(source, 500, 0);
             });
        }
        else if (source.isOnField)
        {
             // Already equipped logic handled by modifiers
        }
    }

    // 2080 - Widespread Ruin
    void Effect_2080_WidespreadRuin(CardDisplay source)
    {
        // Opponent attacks: Destroy Attack Pos monster with highest ATK.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            // Assuming source.isPlayerCard is true (player's trap), target opponent
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            
            foreach(var z in zones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.position == CardDisplay.BattlePosition.Attack) oppMonsters.Add(m);
                }
            }

            if (oppMonsters.Count > 0)
            {
                oppMonsters.Sort((a, b) => b.currentAtk.CompareTo(a.currentAtk)); // Descending
                CardDisplay target = oppMonsters[0];
                
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
                Debug.Log($"Widespread Ruin: Destruiu {target.CurrentCardData.name}.");
            }
        }
    }

    // 2081 - Wild Nature's Release
    void Effect_2081_WildNaturesRelease(CardDisplay source)
    {
        // Target Beast/Beast-Warrior; ATK += DEF. Destroy at End Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.race == "Beast" || t.CurrentCardData.race == "Beast-Warrior"),
                (target) => {
                    int boost = target.currentDef;
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, boost, source));
                    target.scheduledForDestruction = true;
                    Debug.Log($"Wild Nature's Release: +{boost} ATK. Será destruído na End Phase.");
                }
            );
        }
    }

    // 2083 - Windstorm of Etaqua
    void Effect_2083_WindstormOfEtaqua(CardDisplay source)
    {
        // Change battle positions of all face-up monsters opponent controls.
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            foreach(var z in zones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && !m.isFlipped) m.ChangePosition();
                }
            }
        }
    }

    // 2090 - Winged Kuriboh
    void Effect_2090_WingedKuriboh(CardDisplay source)
    {
        // Destroyed -> No battle damage this turn.
        // Logic in OnCardSentToGraveyard.
        Debug.Log("Winged Kuriboh: Proteção de dano configurada.");
    }

    // 2091 - Winged Kuriboh LV10
    void Effect_2091_WingedKuribohLV10(CardDisplay source)
    {
        // Tribute (Quick): Destroy all Attack Pos monsters opp controls, inflict damage = combined original ATK.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            
            List<CardDisplay> toDestroy = new List<CardDisplay>();
            int totalDamage = 0;
            
            if (GameManager.Instance.duelFieldUI != null)
            {
                Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
                foreach(var z in zones)
                {
                    if(z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null && m.position == CardDisplay.BattlePosition.Attack)
                        {
                            toDestroy.Add(m);
                            totalDamage += m.originalAtk;
                        }
                    }
                }
            }
            
            foreach(var m in toDestroy)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(m);
                GameManager.Instance.SendToGraveyard(m.CurrentCardData, m.isPlayerCard);
                Destroy(m.gameObject);
            }
            
            Effect_DirectDamage(source, totalDamage);
        }
    }

    // 2092 - Winged Minion
    void Effect_2092_WingedMinion(CardDisplay source)
    {
        // Tribute: Target Fiend; +700 ATK/DEF.
        if (source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.race == "Fiend" && t != source,
                    (target) => {
                        GameManager.Instance.TributeCard(source);
                        target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 700, source));
                        target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 700, source));
                    }
                );
            }
        }
    }

    // 2093 - Winged Sage Falcos
    void Effect_2093_WingedSageFalcos(CardDisplay source)
    {
        // Battle damage -> Top of Deck. (Actually: If destroys Attack pos monster).
        // Logic in OnBattleEnd.
        Debug.Log("Winged Sage Falcos: Efeito de batalha configurado.");
    }

    // 2096 - Witch Doctor of Chaos
    void Effect_2096_WitchDoctorOfChaos(CardDisplay source)
    {
        // FLIP: Banish 1 monster from GY.
        List<CardData> targets = new List<CardData>();
        targets.AddRange(GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster")));
        targets.AddRange(GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.type.Contains("Monster")));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Banir do GY", (selected) => {
                bool isPlayer = GameManager.Instance.GetPlayerGraveyard().Contains(selected);
                GameManager.Instance.RemoveFromPlay(selected, isPlayer);
                if (isPlayer) GameManager.Instance.GetPlayerGraveyard().Remove(selected);
                else GameManager.Instance.GetOpponentGraveyard().Remove(selected);
            });
        }
    }

    // 2097 - Witch of the Black Forest
    void Effect_2097_WitchOfTheBlackForest(CardDisplay source)
    {
        // Sent to GY: Add monster with DEF <= 1500.
        // Logic in OnCardSentToGraveyard.
        Debug.Log("Witch of the Black Forest: Efeito de busca configurado.");
    }

    // 2098 - Witch's Apprentice
    void Effect_2098_WitchsApprentice(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Dark");
    }

    // 2100 - Wodan the Resident of the Forest
    void Effect_2100_WodanTheResidentOfTheForest(CardDisplay source)
    {
        // +100 ATK per Plant.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Assuming manual iteration
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) { var m = z.GetChild(0).GetComponent<CardDisplay>(); if(m && m.CurrentCardData.race == "Plant") count++; }
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) { var m = z.GetChild(0).GetComponent<CardDisplay>(); if(m && m.CurrentCardData.race == "Plant") count++; }
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 100, source));
    }
        // 2106 - Woodland Sprite
    void Effect_2106_WoodlandSprite(CardDisplay source)
    {
        // Send 1 Equip Card equipped to this card to the GY. Inflict 500 damage.
        Effect_DirectDamage(source, 500);
        List<CardDisplay> equippedCards = GetEquippedCards(source);
        
        if (equippedCards.Count > 0)
        {
            // Converte para CardData para usar o seletor genérico (ou cria seletor de CardDisplay)
            // Como OpenCardSelection usa CardData, vamos mapear de volta.
            // Nota: Isso pode ser ambíguo se houver múltiplas cópias iguais equipadas.
            // Para simplificar, removemos o primeiro encontrado.
            CardDisplay toSend = equippedCards[0];
            GameManager.Instance.SendToGraveyard(toSend.CurrentCardData, toSend.isPlayerCard);
            Destroy(toSend.gameObject);
            Effect_DirectDamage(source, 500);
        }
    }

    // 2107 - World Suppression
    void Effect_2107_WorldSuppression(CardDisplay source)
    {
        // Negate Field Spell activation.
        Debug.Log("World Suppression: Nega Field Spell (Requer Chain).");
    }

    // 2111 - Wroughtweiler
    void Effect_2111_Wroughtweiler(CardDisplay source)
    {
        // Destroyed by battle: Target 1 Elemental HERO and 1 Polymerization in GY; add to hand.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        CardData poly = gy.Find(c => c.name == "Polymerization");
        CardData hero = gy.Find(c => c.name.Contains("Elemental HERO"));

        if (poly != null && hero != null)
        {
            gy.Remove(poly);
            gy.Remove(hero);
            GameManager.Instance.AddCardToHand(poly, source.isPlayerCard);
            GameManager.Instance.AddCardToHand(hero, source.isPlayerCard);
            Debug.Log("Wroughtweiler: Recuperou HERO e Polymerization.");
        }
    }

    // 2112 - Wynn the Wind Charmer
    void Effect_2112_WynnTheWindCharmer(CardDisplay source)
    {
        // FLIP: Take control of 1 WIND monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.attribute == "Wind",
                (target) => GameManager.Instance.SwitchControl(target)
            );
        }
    }

    // 2114 - XY-Dragon Cannon
    void Effect_2114_XYDragonCannon(CardDisplay source)
    {
        // Discard 1 card; destroy 1 face-up S/T.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && !t.isFlipped,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                    );
                }
            });
        }
    }

    // 2115 - XYZ-Dragon Cannon
    void Effect_2115_XYZDragonCannon(CardDisplay source)
    {
        // Discard 1 card; destroy 1 card opp controls.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                    );
                }
            });
        }
    }

    // 2116 - XZ-Tank Cannon
    void Effect_2116_XZTankCannon(CardDisplay source)
    {
        // Discard 1 card; destroy 1 face-down S/T.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && t.isFlipped,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                    );
                }
            });
        }
    }

    // 2117 - Xing Zhen Hu
    void Effect_2117_XingZhenHu(CardDisplay source)
    {
        // Select 2 Set S/T; they cannot be activated.
        Debug.Log("Xing Zhen Hu: Trava 2 S/T setadas (Lógica de seleção múltipla e bloqueio pendente).");
    }

    // 2118 - Y-Dragon Head
    void Effect_2118_YDragonHead(CardDisplay source)
    {
        Effect_Union(source, "X-Head Cannon", 400, 400);
    }

    // 2119 - YZ-Tank Dragon
    void Effect_2119_YZTankDragon(CardDisplay source)
    {
        // Discard 1 card; destroy 1 face-down monster.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && t.isFlipped,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                    );
                }
            });
        }
    }

    // 2120 - Yado Karu
    void Effect_2120_YadoKaru(CardDisplay source)
    {
        // Change Attack to Defense -> Place hand on bottom of Deck.
        // Lógica no OnBattlePositionChangedImpl.
        Debug.Log("Yado Karu: Efeito de manipulação de mão configurado.");
    }

    // 2123 - Yamata Dragon
    void Effect_2123_YamataDragon(CardDisplay source)
    {
        // Spirit. Battle Damage -> Draw until 5.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("Yamata Dragon: Efeito de compra configurado.");
    }

    // 2125 - Yami
    void Effect_2125_Yami(CardDisplay source)
    {
        Effect_Field(source, 200, 200, "Fiend");
        Effect_Field(source, 200, 200, "Spellcaster");
        Effect_Field(source, -200, -200, "Fairy");
    }

    // 2128 - Yata-Garasu
    void Effect_2128_YataGarasu(CardDisplay source)
    {
        // Spirit. Battle Damage -> Skip next Draw Phase.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("Yata-Garasu: Efeito de bloqueio de compra configurado.");
    }

    // 2129 - Yellow Gadget
    void Effect_2129_YellowGadget(CardDisplay source)
    {
        Effect_SearchDeck(source, "Green Gadget");
    }

    // 2130 - Yellow Luster Shield
    void Effect_2130_YellowLusterShield(CardDisplay source)
    {
        Effect_BuffStats(source, 0, 300);
    }

    // 2131 - Yomi Ship
    void Effect_2131_YomiShip(CardDisplay source)
    {
        // Destroyed by battle -> Destroy killer.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Yomi Ship: Efeito de destruição mútua configurado.");
    }

    // 2133 - Yu-Jo Friendship
    void Effect_2133_YuJoFriendship(CardDisplay source)
    {
        // Handshake -> LP becomes average.
        int combined = GameManager.Instance.playerLP + GameManager.Instance.opponentLP;
        int average = combined / 2;
        GameManager.Instance.playerLP = average;
        GameManager.Instance.opponentLP = average;
        Debug.Log("Yu-Jo Friendship: LP igualados.");
    }

    // 2134 - Z-Metal Tank
    void Effect_2134_ZMetalTank(CardDisplay source)
    {
        Effect_Union(source, "X-Head Cannon", 600, 600);
        Effect_Union(source, "Y-Dragon Head", 600, 600);
    }

    // 2135 - Zaborg the Thunder Monarch
    void Effect_2135_ZaborgTheThunderMonarch(CardDisplay source)
    {
        // Tribute Summon: Destroy 1 monster.
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
    }

    // 2138 - Zera Ritual
    void Effect_2138_ZeraRitual(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 2139 - Zera the Mant
    void Effect_2139_ZeraTheMant(CardDisplay source)
    {
        Debug.Log("Zera the Mant: Monstro Ritual.");
    }

    // 2140 - Zero Gravity
    void Effect_2140_ZeroGravity(CardDisplay source)
    {
        // Change positions of all face-up monsters.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) m.ChangePosition();
        }
    }

    // 2142 - Zolga
    void Effect_2142_Zolga(CardDisplay source)
    {
        // Tribute -> Gain 2000 LP.
        // Lógica no OnTribute (se existisse) ou OnCardSentToGraveyard.
        // Simplificado: Se foi tributado (verificar flag isTributeSummoned no monstro invocado? Não, Zolga é o tributo).
        Debug.Log("Zolga: Ganha 2000 LP se usado como tributo (OnTribute).");
    }

    // 2143 - Zoma the Spirit
    void Effect_2143_ZomaTheSpirit(CardDisplay source)
    {
        // SS Trap Monster. Destroyed by battle -> Burn = ATK of killer.
        GameManager.Instance.SpawnToken(source.isPlayerCard, 1800, 500, "Zoma Token");
        // Lógica de dano no OnBattleEnd do Token.
    }

    // 2144 - Zombie Tiger
    void Effect_2144_ZombieTiger(CardDisplay source)
    {
        Effect_Union(source, "Decayed Commander", 500, 500);
    }

    // 2145 - Zombie Warrior
    void Effect_2145_ZombieWarrior(CardDisplay source)
    {
        Debug.Log("Zombie Warrior: Monstro de Fusão.");
    }

    // 2146 - Zombyra the Dark
    void Effect_2146_ZombyraTheDark(CardDisplay source)
    {
        // Cannot attack direct. Destroy monster -> -200 ATK.
        // Lógica de ataque direto no BattleManager.
        // Lógica de debuff no OnBattleEnd.
        Debug.Log("Zombyra: Efeitos de restrição e debuff configurados.");
    }

    // 2147 - Zone Eater
    void Effect_2147_ZoneEater(CardDisplay source)
    {
        // If this face-down card is attacked, destroy attacker after 5 turns.
        // Lógica implementada no OnBattleEnd (CardEffectManager_Impl.cs).
        Debug.Log("Zone Eater: Efeito de destruição retardada configurado.");
    }
}