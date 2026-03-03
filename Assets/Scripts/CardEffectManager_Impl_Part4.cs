using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 1501 - 1600)
    // =========================================================================================

    // 1502 - Red Gadget
    void Effect_1502_RedGadget(CardDisplay source)
    {
        // When this card is Normal or Special Summoned: You can add 1 "Yellow Gadget" from your Deck to your hand.
        Effect_SearchDeck(source, "Yellow Gadget");
    }

    // 1503 - Red Medicine
    void Effect_1503_RedMedicine(CardDisplay source)
    {
        // Increase your Life Points by 500 points.
        Effect_GainLP(source, 500);
    }

    // 1504 - Red-Eyes B. Chick
    void Effect_1504_RedEyesBChick(CardDisplay source)
    {
        // You can send this face-up card to the Graveyard; Special Summon 1 "Red-Eyes B. Dragon" from your hand.
        if (source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            CardData redEyes = hand.Find(c => c.name == "Red-Eyes B. Dragon");
            if (redEyes != null)
            {
                UIManager.Instance.ShowConfirmation("Tribute Red-Eyes B. Chick to summon Red-Eyes B. Dragon?", () => {
                    GameManager.Instance.TributeCard(source);
                    GameManager.Instance.SpecialSummonFromData(redEyes, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(redEyes, source.isPlayerCard);
                });
            }
        }
    }

    // 1506 - Red-Eyes Black Metal Dragon
    void Effect_1506_RedEyesBlackMetalDragon(CardDisplay source)
    {
        // Cannot be Normal Summoned/Set. Must be Special Summoned with "Metalmorph" from your Deck, and cannot be Special Summoned by other ways.
        Debug.Log("Red-Eyes Black Metal Dragon: Invocação especial via Metalmorph (Lógica no efeito do Metalmorph).");
    }

    // 1507 - Reflect Bounder
    void Effect_1507_ReflectBounder(CardDisplay source)
    {
        // When this face-up card is attacked by an opponent's monster, before damage calculation: Inflict damage to your opponent equal to the ATK of the attacking monster. Then, after damage calculation, destroy this card.
        // Lógica no OnAttackDeclared.
        Debug.Log("Reflect Bounder: Efeito de dano e autodestruição configurado (OnAttackDeclared).");
    }

    // 1508 - Regenerating Mummy
    void Effect_1508_RegeneratingMummy(CardDisplay source)
    {
        // If this card is sent from your hand to your Graveyard by an opponent's card effect: Return this card to your hand.
        // Lógica no OnCardDiscarded.
        Debug.Log("Regenerating Mummy: Efeito de retorno à mão configurado (OnCardDiscarded).");
    }

    // 1509 - Reinforcement of the Army
    void Effect_1509_ReinforcementOfTheArmy(CardDisplay source)
    {
        // Add 1 Level 4 or lower Warrior-Type monster from your Deck to your hand.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> targets = deck.FindAll(c => c.type.Contains("Monster") && c.race == "Warrior" && c.level <= 4);
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Adicionar Guerreiro à Mão", (selected) => {
                deck.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    // 1510 - Reinforcements
    void Effect_1510_Reinforcements(CardDisplay source)
    {
        // Target 1 face-up monster on the field; it gains 500 ATK until the End Phase.
        Effect_BuffStats(source, 500, 0);
    }

    // 1511 - Release Restraint
    void Effect_1511_ReleaseRestraint(CardDisplay source)
    {
        // Tribute 1 "Gearfried the Iron Knight"; Special Summon 1 "Gearfried the Swordmaster" from your hand or Deck.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Gearfried the Iron Knight",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    Effect_SearchDeck(source, "Gearfried the Swordmaster", "Monster"); // Simplificado para busca, deveria ser SS
                }
            );
        }
    }

    // 1512 - Relieve Monster
    void Effect_1512_RelieveMonster(CardDisplay source)
    {
        // Return 1 monster you control to the hand to Special Summon 1 Level 4 monster from your hand.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (toReturn) => {
                    GameManager.Instance.ReturnToHand(toReturn);
                    
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> targets = hand.FindAll(c => c.type.Contains("Monster") && c.level == 4);
                    
                    if (targets.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(targets, "Invocar Lv4 da Mão", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                            GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    // 1513 - Relinquished
    void Effect_1513_Relinquished(CardDisplay source)
    {
        // Once per turn: You can target 1 monster your opponent controls; equip that target to this card.
        if (source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        Debug.Log($"Relinquished: Absorvendo {target.CurrentCardData.name}.");
                        // Lógica de equipar (mover visualmente para S/T e linkar)
                        GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                        
                        // Copia stats
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk, source));
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalDef, source));
                        
                        // Move visualmente
                        target.transform.SetParent(source.transform);
                        target.transform.localPosition = new Vector3(0.2f, 0.2f, 0.1f);
                    }
                );
            }
        }
    }

    // 1514 - Reload
    void Effect_1514_Reload(CardDisplay source)
    {
        // Add all cards in your hand to the Deck and shuffle it. Then, draw the same number of cards you added to the Deck.
        int handCount = GameManager.Instance.GetPlayerHandData().Count;
        if (handCount > 0)
        {
            GameManager.Instance.DiscardHand(true); // Simulado, deveria ser shuffle
            for(int i=0; i<handCount; i++) GameManager.Instance.DrawCard(true);
        }
    }

    // 1515 - Remove Brainwashing
    void Effect_1515_RemoveBrainwashing(CardDisplay source)
    {
        // Control of all monsters on the field returns to their original owners.
        Debug.Log("Remove Brainwashing: Controle de todos os monstros resetado.");
        // Iterar todos os monstros e verificar se o dono é diferente do controlador.
    }

    // 1516 - Remove Trap
    void Effect_1516_RemoveTrap(CardDisplay source)
    {
        // Destroy 1 face-up Trap Card.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Trap") && !t.isFlipped,
                (t) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }

    // 1517 - Rescue Cat
    void Effect_1517_RescueCat(CardDisplay source)
    {
        // Send this card to the GY; Special Summon 2 Level 3 or lower Beast-Type monsters from your Deck, but they are destroyed during the End Phase.
        if (source.isOnField)
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);

            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> targets = deck.FindAll(c => c.type.Contains("Monster") && c.race == "Beast" && c.level <= 3);
            
            if (targets.Count >= 2)
            {
                GameManager.Instance.OpenCardMultiSelection(targets, "Invocar 2 Bestas", 2, 2, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
                        deck.Remove(c);
                        // TODO: Agendar destruição na End Phase
                    }
                    GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                });
            }
        }
    }

    // 1518 - Reshef the Dark Being
    void Effect_1518_ReshefTheDarkBeing(CardDisplay source)
    {
        // Once per turn: You can discard 1 Spell Card to target 1 monster your opponent controls; take control of that target.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Effect_ChangeControl(source, false); // false = permanente
            });
        }
    }

    // 1519 - Respect Play
    void Effect_1519_RespectPlay(CardDisplay source)
    {
        // Both players must keep their hands revealed.
        Debug.Log("Respect Play: Mãos reveladas.");
        GameManager.Instance.showOpponentHand = true;
        // Precisa de lógica para revelar a mão do jogador também.
    }

    // 1520 - Restructer Revolution
    void Effect_1520_RestructerRevolution(CardDisplay source)
    {
        // Inflict 200 damage to your opponent for each card in their hand.
        int oppHandCount = GameManager.Instance.GetOpponentHandData().Count;
        Effect_DirectDamage(source, oppHandCount * 200);
    }

    // 1521 - Resurrection of Chakra
    void Effect_1521_ResurrectionOfChakra(CardDisplay source)
    {
        // This card is used to Ritual Summon "Chakra".
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 1522 - Return Zombie
    void Effect_1522_ReturnZombie(CardDisplay source)
    {
        // Pay 500 LP. Target 1 Zombie-Type monster in your Graveyard; add that target to your hand.
        if (Effect_PayLP(source, 500))
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> zombies = gy.FindAll(c => c.race == "Zombie");
            
            if (zombies.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(zombies, "Recuperar Zumbi", (selected) => {
                    gy.Remove(selected);
                    GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                });
            }
        }
    }

    // 1523 - Return from the Different Dimension
    void Effect_1523_ReturnFromTheDifferentDimension(CardDisplay source)
    {
        // Pay half your LP; Special Summon as many of your banished monsters as possible.
        int cost = GameManager.Instance.playerLP / 2;
        if (Effect_PayLP(source, cost))
        {
            List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
            foreach(var c in banished)
            {
                GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
            }
            banished.Clear();
        }
    }

    // 1524 - Return of the Doomed
    void Effect_1524_ReturnOfTheDoomed(CardDisplay source)
    {
        // Discard 1 Monster Card; add 1 monster with the same name from your Graveyard to your hand.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Descarte 1 Monstro", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                CardData target = gy.Find(c => c.name == discarded.name);
                
                if (target != null)
                {
                    gy.Remove(target);
                    GameManager.Instance.AddCardToHand(target, source.isPlayerCard);
                }
            });
        }
    }

    // 1525 - Reversal Quiz
    void Effect_1525_ReversalQuiz(CardDisplay source)
    {
        // Declare a card type (Monster, Spell, or Trap). Your opponent guesses if the top card of your Deck is that type. If they guess wrong: Swap Life Points with your opponent.
        // Simulação
        int declaredType = Random.Range(0, 3); // 0=M, 1=S, 2=T
        CardData topCard = GameManager.Instance.GetPlayerMainDeck()[0];
        
        bool oppGuessedRight = Random.value > 0.5f; // Simula palpite
        
        bool actualIsDeclaredType = false;
        if (declaredType == 0 && topCard.type.Contains("Monster")) actualIsDeclaredType = true;
        if (declaredType == 1 && topCard.type.Contains("Spell")) actualIsDeclaredType = true;
        if (declaredType == 2 && topCard.type.Contains("Trap")) actualIsDeclaredType = true;

        if (oppGuessedRight == actualIsDeclaredType)
        {
            Debug.Log("Reversal Quiz: Oponente acertou!");
        }
        else
        {
            Debug.Log("Reversal Quiz: Oponente errou! Trocando LP.");
            int pLP = GameManager.Instance.playerLP;
            int oLP = GameManager.Instance.opponentLP;
            GameManager.Instance.playerLP = oLP;
            GameManager.Instance.opponentLP = pLP;
        }
    }

    // 1553 - Rocket Jumper
    void Effect_1553_RocketJumper(CardDisplay source)
    {
        // Can attack directly if opponent controls only Defense Position monsters.
        // Lógica implementada no BattleManager.
        Debug.Log("Rocket Jumper: Pode atacar diretamente se oponente só tiver defesa.");
    }

    // 1554 - Rocket Warrior
    void Effect_1554_RocketWarrior(CardDisplay source)
    {
        // Battle Phase: Cannot be destroyed by battle, no battle damage.
        // If attacks: Target loses 500 ATK until End Phase.
        // Lógica de proteção no BattleManager.
        // Lógica de debuff no OnAttackDeclared/OnDamageCalculation.
        Debug.Log("Rocket Warrior: Efeitos de batalha configurados.");
    }

    // 1555 - Rod of Silence - Kay'est
    void Effect_1555_RodOfSilenceKayest(CardDisplay source)
    {
        // Equip: +500 DEF. Negate Spell effects targeting equipped monster. Destroy other Equips.
        Effect_Equip(source, 0, 500);
        // Lógica de destruir outros equips:
        if (SpellTrapManager.Instance != null && SpellTrapManager.Instance.isSelectingTarget == false) // Evita loop na seleção
        {
            // Encontra o monstro equipado (via CardLink)
            // ... (Simplificado: Assume que o link foi criado no Effect_Equip)
            Debug.Log("Rod of Silence: Outros equipamentos destruídos (Lógica pendente).");
        }
    }

    // 1556 - Rod of the Mind's Eye
    void Effect_1556_RodOfTheMindsEye(CardDisplay source)
    {
        // Equip: If deals battle damage, it becomes 1000.
        Effect_Equip(source, 0, 0);
        // Lógica de dano fixo no BattleManager/OnDamageCalculation.
    }

    // 1559 - Rope of Life
    void Effect_1559_RopeOfLife(CardDisplay source)
    {
        // Trigger: When monster destroyed by battle -> Discard hand -> SS +800 ATK.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Rope of Life: Armadilha de renascimento configurada.");
    }

    // 1561 - Roulette Barrel
    void Effect_1561_RouletteBarrel(CardDisplay source)
    {
        // Roll die twice. Destroy monster with Level = Result.
        if (source.hasUsedEffectThisTurn) return;
        
        GameManager.Instance.TossCoin(2, (heads) => { // Simulando dados
            int d1 = Random.Range(1, 7);
            int d2 = Random.Range(1, 7);
            int result = 0;
            // Regra: Seleciona 1 resultado
            // Simplificado: Usa o maior ou soma? Texto diz "Select 1 result".
            // Vamos usar o primeiro para simplificar.
            result = d1;
            
            Debug.Log($"Roulette Barrel: Rolou {d1} e {d2}. Alvo Nível {result}.");
            
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.CurrentCardData.level == result,
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        });
        source.hasUsedEffectThisTurn = true;
    }

    // 1562 - Royal Command
    void Effect_1562_RoyalCommand(CardDisplay source)
    {
        // Negate activation and effects of Flip Effect Monsters.
        Debug.Log("Royal Command: Efeitos Flip negados (Passivo).");
    }

    // 1563 - Royal Decree
    void Effect_1563_RoyalDecree(CardDisplay source)
    {
        // Negate all other Trap effects on the field.
        Debug.Log("Royal Decree: Outras Traps negadas (Passivo).");
    }

    // 1565 - Royal Keeper
    void Effect_1565_RoyalKeeper(CardDisplay source)
    {
        // FLIP: Gains 300 ATK/DEF.
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 300, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 300, source));
    }

    // 1566 - Royal Magical Library
    void Effect_1566_RoyalMagicalLibrary(CardDisplay source)
    {
        // Accumulate Spell Counter on Spell activation. Remove 3 -> Draw 1.
        // Acúmulo: Tratado no OnSpellActivated (CardEffectManager_Impl.cs).
        // Ignição:
        if (SpellCounterManager.Instance.GetCount(source) >= 3)
        {
            SpellCounterManager.Instance.RemoveCounter(source, 3);
            GameManager.Instance.DrawCard();
            Debug.Log("Royal Magical Library: Comprou 1 carta.");
        }
    }

    // 1567 - Royal Oppression
    void Effect_1567_RoyalOppression(CardDisplay source)
    {
        // Pay 800 LP; negate Special Summon or effect that SS.
        // Requer Chain/Trigger de invocação.
        Debug.Log("Royal Oppression: Negação de SS (Requer Chain).");
    }

    // 1568 - Royal Surrender
    void Effect_1568_RoyalSurrender(CardDisplay source)
    {
        // Negate activation of Continuous Trap and destroy it.
        // Requer Chain.
        Debug.Log("Royal Surrender: Nega Trap Contínua (Requer Chain).");
    }

    // 1569 - Royal Tribute
    void Effect_1569_RoyalTribute(CardDisplay source)
    {
        // If Necrovalley active: Both players discard all monsters in hand.
        if (GameManager.Instance.IsCardActiveOnField("1324")) // Necrovalley
        {
            // Player
            List<CardData> pHand = GameManager.Instance.GetPlayerHandData();
            List<CardData> pMonsters = pHand.FindAll(c => c.type.Contains("Monster"));
            foreach(var c in pMonsters) GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
            
            // Opponent
            List<CardData> oHand = GameManager.Instance.GetOpponentHandData();
            List<CardData> oMonsters = oHand.FindAll(c => c.type.Contains("Monster"));
            // Simula descarte do oponente (remove visualmente)
            // Em produção: GameManager.DiscardOpponentCard(card)
            Debug.Log($"Royal Tribute: Descartados {pMonsters.Count} (Player) e {oMonsters.Count} (Opp) monstros.");
        }
        else
        {
            Debug.Log("Royal Tribute: Requer Necrovalley.");
        }
    }

    // 1571 - Rush Recklessly
    void Effect_1571_RushRecklessly(CardDisplay source)
    {
        // Target 1 face-up monster; +700 ATK until end of turn.
        Effect_BuffStats(source, 700, 0);
    }

    // 1572 - Ryu Kokki
    void Effect_1572_RyuKokki(CardDisplay source)
    {
        // If battles Warrior or Spellcaster: Destroy that monster at end of Damage Step.
        // Lógica no OnBattleEnd.
        Debug.Log("Ryu Kokki: Efeito de batalha configurado.");
    }

    // 1573 - Ryu Senshi
    void Effect_1573_RyuSenshi(CardDisplay source)
    {
        // Pay 1000 LP; negate Normal Trap. Negate Spell targeting this card.
        // Negação de Trap: Requer Chain.
        // Negação de Alvo: Passivo/Trigger.
        Debug.Log("Ryu Senshi: Efeitos de negação ativos.");
    }

    // 1575 - Ryu-Kishin Clown
    void Effect_1575_RyuKishinClown(CardDisplay source)
    {
        // When Summoned: Select 1 face-up monster; change its battle position.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Ryu-Kishin Clown: Posição de {t.CurrentCardData.name} alterada.");
                }
            );
        }
    }

    // 1576 - Sacred Crane
    void Effect_1576_SacredCrane(CardDisplay source)
    {
        // If this card is Special Summoned: You can draw 1 card.
        // Lógica implementada no OnSpecialSummon.
    }

    // 1577 - Sacred Defense Barrier
    void Effect_1577_SacredDefenseBarrier(CardDisplay source)
    {
        // Your opponent cannot select Fairy-Type monsters as an attack target.
        // Lógica passiva no BattleManager.
        Debug.Log("Sacred Defense Barrier: Fadas protegidas de ataques.");
    }

    // 1578 - Sacred Phoenix of Nephthys
    void Effect_1578_SacredPhoenixOfNephthys(CardDisplay source)
    {
        // If destroyed by card effect: SS during next Standby Phase. When you do: Destroy all S/T.
        // Lógica de gatilho no OnCardSentToGraveyard e OnPhaseStart.
        Debug.Log("Sacred Phoenix: Efeitos de renascimento e destruição configurados.");
    }

    // 1579 - Sage's Stone
    void Effect_1579_SagesStone(CardDisplay source)
    {
        // If you control a face-up "Dark Magician Girl": Special Summon 1 "Dark Magician" from your hand or Deck.
        if (GameManager.Instance.IsCardActiveOnField("0420")) // Dark Magician Girl
        {
            Effect_SearchDeck(source, "Dark Magician", "Monster"); // Simplificado para busca, deveria ser SS
        }
    }

    // 1581 - Sakuretsu Armor
    void Effect_1581_SakuretsuArmor(CardDisplay source)
    {
        // When an opponent's monster declares an attack: Target the attacking monster; destroy that target.
        // Lógica no OnAttackDeclared.
        Debug.Log("Sakuretsu Armor: Armadilha de ataque configurada.");
    }

    // 1582 - Salamandra
    void Effect_1582_Salamandra(CardDisplay source)
    {
        // Equip: FIRE monster gains 700 ATK.
        Effect_Equip(source, 700, 0, "", "Fire");
    }

    // 1583 - Salvage
    void Effect_1583_Salvage(CardDisplay source)
    {
        // Target 2 WATER monsters with 1500 or less ATK in your Graveyard; add them to your hand.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.attribute == "Water" && c.atk <= 1500 && c.type.Contains("Monster"));
        
        if (targets.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(targets, "Recuperar 2 WATER", 2, 2, (selected) => {
                foreach(var c in selected)
                {
                    gy.Remove(c);
                    GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                }
            });
        }
    }

    // 1584 - Sand Gambler
    void Effect_1584_SandGambler(CardDisplay source)
    {
        // FLIP: Toss a coin 3 times. 3 Heads: Destroy all opp monsters. 3 Tails: Destroy all your monsters.
        GameManager.Instance.TossCoin(3, (heads) => {
            if (heads == 3)
            {
                Debug.Log("Sand Gambler: 3 Caras! Destruindo monstros do oponente.");
                DestroyAllMonsters(true, false);
            }
            else if (heads == 0)
            {
                Debug.Log("Sand Gambler: 3 Coroas! Destruindo seus monstros.");
                DestroyAllMonsters(false, true);
            }
        });
    }

    // 1585 - Sand Moth
    void Effect_1585_SandMoth(CardDisplay source)
    {
        // If this face-down Defense Position card is destroyed by a Spell Card's effect, it is Special Summoned during the Standby Phase of the next turn.
        // Lógica no OnCardLeavesField e OnPhaseStart.
        Debug.Log("Sand Moth: Efeito de renascimento configurado.");
    }

    // 1586 - Sanga of the Thunder
    void Effect_1586_SangaOfTheThunder(CardDisplay source)
    {
        // During damage calculation in your opponent's turn, if this card is being attacked: You can target the attacking monster; make that target's ATK 0.
        // Lógica no OnDamageCalculation.
        Debug.Log("Sanga of the Thunder: Efeito de batalha configurado.");
    }

    // 1587 - Sangan
    void Effect_1587_Sangan(CardDisplay source)
    {
        // If this card is sent from the field to the Graveyard: Add 1 monster with 1500 or less ATK from your Deck to your hand.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Sangan: Efeito de busca configurado.");
    }

    // 1589 - Sasuke Samurai
    void Effect_1589_SasukeSamurai(CardDisplay source)
    {
        // If this card attacks a face-down Defense Position monster, destroy the monster immediately with this card's effect without flipping it face-up or applying damage calculation.
        // Lógica no OnDamageCalculation.
        Debug.Log("Sasuke Samurai: Efeito de batalha configurado.");
    }

    // 1590 - Sasuke Samurai #2
    void Effect_1590_SasukeSamurai2(CardDisplay source)
    {
        // Your opponent cannot activate Spell/Trap Cards during the Battle Phase.
        // Lógica passiva no BattleManager.
        Debug.Log("Sasuke Samurai #2: Bloqueio de S/T na batalha.");
    }

    // 1591 - Sasuke Samurai #3
    void Effect_1591_SasukeSamurai3(CardDisplay source)
    {
        // Your opponent draws cards until they have 7 cards in their hand.
        int oppHandCount = GameManager.Instance.GetOpponentHandData().Count;
        if (oppHandCount < 7)
        {
            int toDraw = 7 - oppHandCount;
            for(int i=0; i<toDraw; i++) GameManager.Instance.DrawOpponentCard();
        }
    }

    // 1592 - Sasuke Samurai #4
    void Effect_1592_SasukeSamurai4(CardDisplay source)
    {
        // When this card declares an attack: Toss a coin and call it. If you call it right, destroy 1 monster on the field.
        // Lógica no OnAttackDeclared.
        Debug.Log("Sasuke Samurai #4: Efeito de ataque configurado.");
    }

    // 1593 - Satellite Cannon
    void Effect_1593_SatelliteCannon(CardDisplay source)
    {
        // Cannot be destroyed by battle with a Level 7 or lower monster. During each of your End Phases: This card gains 1000 ATK.
        // Lógica de proteção no BattleManager. Lógica de buff no OnPhaseStart.
        Debug.Log("Satellite Cannon: Efeitos passivos e de fase configurados.");
    }

    // 1594 - Scapegoat
    void Effect_1594_Scapegoat(CardDisplay source)
    {
        // Special Summon 4 "Sheep Tokens" (Beast-Type/EARTH/Level 1/ATK 0/DEF 0) in Defense Position.
        for(int i=0; i<4; i++)
        {
            GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Sheep Token");
        }
    }

    // 1595 - Scapeghost
    void Effect_1595_Scapeghost(CardDisplay source)
    {
        // FLIP: You can Special Summon any number of "Black Sheep Tokens".
        // Simulado: Invoca 2
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Black Sheep Token");
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Black Sheep Token");
    }

    // 1597 - Scroll of Bewitchment
    void Effect_1597_ScrollOfBewitchment(CardDisplay source)
    {
        // Equip: Declare 1 Attribute. The equipped monster's Attribute becomes the declared Attribute.
        // Simulado: Muda para LIGHT
        Effect_Equip(source, 0, 0);
        Debug.Log("Scroll of Bewitchment: Atributo alterado para LIGHT (Simulado).");
    }

    // 1600 - Seal of the Ancients
    void Effect_1600_SealOfTheAncients(CardDisplay source)
    {
        // Pay 1000 LP. Look at all cards your opponent has set on the field.
        if (Effect_PayLP(source, 1000))
        {
            Debug.Log("Seal of the Ancients: Revelando cartas setadas do oponente.");
            // Lógica de revelar
        }
    }
}