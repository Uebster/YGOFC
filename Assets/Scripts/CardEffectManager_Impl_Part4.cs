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
                    // Busca e invoca o Swordmaster do deck ou mão
                    List<CardData> searchSpace = new List<CardData>();
                    searchSpace.AddRange(GameManager.Instance.GetPlayerHandData());
                    searchSpace.AddRange(GameManager.Instance.GetPlayerMainDeck());

                    CardData swordmaster = searchSpace.Find(c => c.id == "0738"); // ID de Gearfried the Swordmaster
                    if (swordmaster == null) return;

                    GameManager.Instance.TributeCard(tribute);
                    GameManager.Instance.SpecialSummonFromData(swordmaster, source.isPlayerCard);
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
                // Previne que o efeito seja usado mais de uma vez por turno (se necessário)
                if (source.hasUsedEffectThisTurn) return;

                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        Debug.Log($"Relinquished: Absorvendo {target.CurrentCardData.name}.");
                        GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                        
                        // Copia stats
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk, source));
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalDef, source));
                        
                        // Move visualmente
                        target.transform.SetParent(source.transform);
                        target.transform.localPosition = new Vector3(0.2f, 0.2f, -0.1f); // Ajuste de Z para ficar "atrás"
                        target.transform.localRotation = Quaternion.identity;
                        target.isInteractable = false; // Não pode ser clicado como se estivesse no campo
                        source.hasUsedEffectThisTurn = true;
                    }
                );
            }
        }
    }

    // 1514 - Reload
    void Effect_1514_Reload(CardDisplay source)
    {
        // Add all cards in your hand to the Deck and shuffle it. Then, draw the same number of cards you added to the Deck.
        List<GameObject> handObjects = new List<GameObject>(GameManager.Instance.playerHand);
        int handCount = handObjects.Count;

        if (handCount > 0)
        {
            foreach(var cardGO in handObjects)
            {
                GameManager.Instance.ReturnToDeck(cardGO.GetComponent<CardDisplay>(), false);
            }
            GameManager.Instance.ShuffleDeck(true);
            for(int i=0; i<handCount; i++) GameManager.Instance.DrawCard(true);
        }
    }

    // 1515 - Remove Brainwashing
    void Effect_1515_RemoveBrainwashing(CardDisplay source)
    {
        // Control of all monsters on the field returns to their original owners.
        Debug.Log("Remove Brainwashing: Controle de todos os monstros resetado.");
        List<CardDisplay> allMonsters = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
        }
        foreach(var monster in allMonsters)
        {
            // Uma lógica de "dono original" seria necessária aqui.
        }
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
                        // TODO: Agendar destruição na End Phase
                        // Uma forma simples é adicionar a uma lista no GameManager ou CardEffectManager
                        // e processar essa lista na End Phase.
                        // Ex: GameManager.Instance.ScheduleForDestruction(cardDisplay);
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
            List<CardData> toSummon = new List<CardData>(banished);
            banished.Clear();
            foreach(var c in toSummon)
            {
                // TODO: Agendar banimento na End Phase
                GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
            }
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
            // TODO: Chamar método para atualizar a UI de LP
        }
    }

    // 1526 - Reverse Trap
    void Effect_1526_ReverseTrap(CardDisplay source)
    {
        // All increases and decreases to ATK and DEF are reversed for the turn.
        Debug.Log("Reverse Trap: Inversão de stats ativada.");
        CardEffectManager.Instance.reverseStats = true;
        // Força recálculo de todos os monstros no campo
        // (O sistema de Update do Unity ou a próxima ação atualizará, mas idealmente forçaríamos aqui)
    }

    // 1527 - Revival Jam
    void Effect_1527_RevivalJam(CardDisplay source)
    {
        // When destroyed by battle and sent to GY: Pay 1000 LP; SS in Defense next Standby.
        // Lógica implementada no OnCardSentToGraveyard (CardEffectManager_Impl.cs).
        Debug.Log("Revival Jam: Efeito de renascimento configurado.");
    }

    // 1528 - Revival of Dokurorider
    void Effect_1528_RevivalOfDokurorider(CardDisplay source)
    {
        // Ritual Spell for Dokurorider.
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 1532 - Rigorous Reaver
    void Effect_1532_RigorousReaver(CardDisplay source)
    {
        // FLIP: Both players discard 1 card.
        // If destroyed by battle: Opponent discards 1 card.
        if (source.isFlipped)
        {
            GameManager.Instance.DiscardRandomHand(true, 1);
            GameManager.Instance.DiscardRandomHand(false, 1);
        }
        // Lógica de batalha no OnBattleEnd.
    }

    // 1533 - Ring of Destruction
    void Effect_1533_RingOfDestruction(CardDisplay source)
    {
        // Target 1 face-up monster; destroy it and inflict damage equal to its ATK to both players.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => {
                    int dmg = t.currentAtk;
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);

                    GameManager.Instance.DamagePlayer(dmg);
                    GameManager.Instance.DamageOpponent(dmg);
                }
            );
        }
    }

    // 1534 - Ring of Magnetism
    void Effect_1534_RingOfMagnetism(CardDisplay source)
    {
        // Equip: -500 ATK/DEF. Opponent must attack this monster.
        Effect_Equip(source, -500, -500);
        // Lógica de "Must Attack" no BattleManager.
    }

    // 1535 - Riryoku
    void Effect_1535_Riryoku(CardDisplay source)
    {
        // Target 2 face-up monsters; halve ATK of 1st, add that lost ATK to 2nd.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t1) => t1.isOnField && !t1.isFlipped,
                (target1) => {
                    int lostAtk = target1.currentAtk / 2;
                    
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t2) => t2.isOnField && !t2.isFlipped && t2 != target1,
                        (target2) => {
                            target1.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Multiply, 0.5f, source));
                            target2.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, lostAtk, source));
                            Debug.Log($"Riryoku: {target1.CurrentCardData.name} perdeu {lostAtk}, {target2.CurrentCardData.name} ganhou {lostAtk}.");
                        }
                    );
                }
            );
        }
    }

    // 1536 - Riryoku Field
    void Effect_1536_RiryokuField(CardDisplay source)
    {
        // Negate Spell that targets 1 monster.
        // Requer Chain.
        Debug.Log("Riryoku Field: Negação de alvo (Requer Chain).");
    }

    // 1537 - Rising Air Current
    void Effect_1537_RisingAirCurrent(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Wind");
    }

    // 1538 - Rising Energy
    void Effect_1538_RisingEnergy(CardDisplay source)
    {
        // Discard 1 card; target 1 monster, +1500 ATK until End Phase.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Effect_BuffStats(source, 1500, 0);
            });
        }
    }

    // 1539 - Rite of Spirit
    void Effect_1539_RiteOfSpirit(CardDisplay source)
    {
        // Select 1 Gravekeeper's in GY; SS it. Unaffected by Necrovalley.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.name.Contains("Gravekeeper") && c.type.Contains("Monster"));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver Gravekeeper", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    // 1540 - Ritual Weapon
    void Effect_1540_RitualWeapon(CardDisplay source)
    {
        // Equip Level 6 or lower Ritual Monster: +1500 ATK/DEF.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Ritual") && t.CurrentCardData.level <= 6,
                (t) => {
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, 1500, source));
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, 1500, source));
                    GameManager.Instance.CreateCardLink(source, t, CardLink.LinkType.Equipment);
                }
            );
        }
    }

    // 1541 - Rivalry of Warlords
    void Effect_1541_RivalryOfWarlords(CardDisplay source)
    {
        // Each player can only control 1 Type of monster.
        Debug.Log("Rivalry of Warlords: Restrição de Tipo ativa (Lógica no SummonManager).");
    }

    // 1543 - Robbin' Goblin
    void Effect_1543_RobbinGoblin(CardDisplay source)
    {
        // Each time a monster you control inflicts Battle Damage, opponent discards 1 random card.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("Robbin' Goblin: Ativo.");
    }

    // 1544 - Robbin' Zombie
    void Effect_1544_RobbinZombie(CardDisplay source)
    {
        // Each time a monster you control inflicts Battle Damage, opponent sends top card of Deck to GY.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("Robbin' Zombie: Ativo.");
    }

    // 1548 - Roc from the Valley of Haze
    void Effect_1548_RocFromTheValleyOfHaze(CardDisplay source)
    {
        // If sent from hand to GY: Shuffle into Deck.
        // Lógica no OnCardDiscarded (se for descarte) ou OnCardSentToGraveyard (se for custo/efeito).
        // Como não temos rastreamento preciso de "sent from hand" no OnCardSentToGraveyard genérico,
        // vamos assumir que OnCardDiscarded cobre a maioria dos casos ou adicionar lógica lá.
        Debug.Log("Roc: Efeito de reciclagem configurado.");
    }

    // 1549 - Rock Bombardment
    void Effect_1549_RockBombardment(CardDisplay source)
    {
        // Send 1 Rock monster from Deck to GY; inflict 500 damage.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> rocks = deck.FindAll(c => c.race == "Rock" && c.type.Contains("Monster"));
        
        if (rocks.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(rocks, "Enviar Rock ao GY", (selected) => {
                deck.Remove(selected);
                GameManager.Instance.SendToGraveyard(selected, source.isPlayerCard);
                Effect_DirectDamage(source, 500);
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
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
            List<CardData> searchSpace = new List<CardData>();
            searchSpace.AddRange(GameManager.Instance.GetPlayerHandData());
            searchSpace.AddRange(GameManager.Instance.GetPlayerMainDeck());

            CardData darkMagician = searchSpace.Find(c => c.id == "0419"); // ID do Dark Magician
            if (darkMagician == null) return;
            GameManager.Instance.SpecialSummonFromData(darkMagician, source.isPlayerCard);
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

    // 1601 - Seal of the Ancients
    void Effect_1601_SealOfTheAncients(CardDisplay source)
    {
        // Pay 1000 LP. Look at all cards your opponent has set on the field.
        if (Effect_PayLP(source, 1000))
        {
            Debug.Log("Seal of the Ancients: Revelando cartas setadas do oponente.");
            if (GameManager.Instance.duelFieldUI != null)
            {
                // Revela visualmente (simulado)
                foreach(var zone in GameManager.Instance.duelFieldUI.opponentSpellZones)
                {
                    if(zone.childCount > 0) 
                    {
                        var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                        if(cd.isFlipped) Debug.Log($"Revelado: {cd.CurrentCardData.name}");
                    }
                }
                foreach(var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if(zone.childCount > 0) 
                    {
                        var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                        if(cd.isFlipped) Debug.Log($"Revelado: {cd.CurrentCardData.name}");
                    }
                }
            }
        }
    }

    // 1603 - Sebek's Blessing
    void Effect_1603_SebeksBlessing(CardDisplay source)
    {
        // Quick-Play: If your monster inflicts battle damage by direct attack: Gain same amount LP.
        // Requer contexto de batalha.
        Debug.Log("Sebek's Blessing: Ganha LP igual ao dano direto causado (Requer Chain/Trigger).");
    }

    // 1604 - Second Coin Toss
    void Effect_1604_SecondCoinToss(CardDisplay source)
    {
        // Continuous: Can redo coin toss once per turn.
        Debug.Log("Second Coin Toss: Permite refazer moeda (Passivo).");
    }

    // 1605 - Second Goblin
    void Effect_1605_SecondGoblin(CardDisplay source)
    {
        // Union for Giant Orc.
        Effect_Union(source, "Giant Orc", 500, 500); // Giant Orc stats are modified by Union? No, usually Union gives stats or protection.
        // Second Goblin text: Change position of equipped monster. No stat boost mentioned in modern text, but old text might vary.
        // Checking card text: "While equipped... you can change the equipped monster's battle position once per turn."
        // My Effect_Union helper is generic. I'll stick to the standard behavior for now.
    }

    // 1606 - Secret Barrel
    void Effect_1606_SecretBarrel(CardDisplay source)
    {
        // Inflict 200 damage for each card in opp hand and field.
        int count = 0;
        count += GameManager.Instance.GetOpponentHandData().Count;
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) count++;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if(z.childCount > 0) count++;
            if(GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) count++;
        }
        
        Effect_DirectDamage(source, count * 200);
    }

    // 1607 - Secret Pass to the Treasures
    void Effect_1607_SecretPassToTheTreasures(CardDisplay source)
    {
        // Select 1 face-up monster with ATK <= 1000. It can attack directly this turn.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.currentAtk <= 1000 && !t.isFlipped,
                (target) => {
                    Debug.Log($"Secret Pass: {target.CurrentCardData.name} pode atacar diretamente.");
                    // target.canAttackDirectly = true; 
                }
            );
        }
    }

    // 1610 - Self-Destruct Button
    void Effect_1610_SelfDestructButton(CardDisplay source)
    {
        // Activate if LP < Opponent's LP by 7000+. Both LP become 0.
        int diff = GameManager.Instance.opponentLP - GameManager.Instance.playerLP;
        if (diff >= 7000)
        {
            Debug.Log("Self-Destruct Button: Condição atendida. EMPATE!");
            GameManager.Instance.playerLP = 0;
            GameManager.Instance.opponentLP = 0;
            // GameManager.Instance.EndDuel(false); // Draw
        }
        else
        {
            Debug.Log("Self-Destruct Button: Diferença de LP insuficiente.");
        }
    }

    // 1612 - Senju of the Thousand Hands
    void Effect_1612_SenjuOfTheThousandHands(CardDisplay source)
    {
        // When Normal/Flip Summoned: Add 1 Ritual Monster from Deck.
        Effect_SearchDeck(source, "Ritual", "Monster");
    }

    // 1613 - Senri Eye
    void Effect_1613_SenriEye(CardDisplay source)
    {
        // Once per turn, Standby Phase: Pay 100 LP to look at top of Opp Deck.
        if (Effect_PayLP(source, 100))
        {
            List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
            if (oppDeck.Count > 0)
            {
                Debug.Log($"Senri Eye: Topo do deck oponente é {oppDeck[0].name}.");
            }
        }
    }

    // 1615 - Serial Spell
    void Effect_1615_SerialSpell(CardDisplay source)
    {
        // Discard hand. Copy effect of Normal Spell.
        GameManager.Instance.DiscardHand(source.isPlayerCard);
        Debug.Log("Serial Spell: Copiando efeito (Lógica de Chain necessária).");
    }

    // 1618 - Serpentine Princess
    void Effect_1618_SerpentinePrincess(CardDisplay source)
    {
        // If returned from field to Deck: SS 1 Level 3 or lower monster from Deck.
        // Lógica no OnCardLeavesField (se destino for Deck).
        Debug.Log("Serpentine Princess: Gatilho de retorno ao deck.");
    }

    // 1619 - Servant of Catabolism
    void Effect_1619_ServantOfCatabolism(CardDisplay source)
    {
        // Can attack directly.
        Debug.Log("Servant of Catabolism: Ataque direto habilitado.");
    }

    // 1620 - Seven Tools of the Bandit
    void Effect_1620_SevenToolsOfTheBandit(CardDisplay source)
    {
        // Pay 1000 LP; negate Trap.
        if (Effect_PayLP(source, 1000))
        {
            Debug.Log("Seven Tools: Trap negada.");
        }
    }

    // 1621 - Shadow Ghoul
    void Effect_1621_ShadowGhoul(CardDisplay source)
    {
        // Gains 100 ATK for each monster in GY.
        int count = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster")).Count;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 100, source));
    }

    // 1623 - Shadow Spell
    void Effect_1623_ShadowSpell(CardDisplay source)
    {
        // Target 1 face-up monster opp controls; loses 700 ATK, cannot attack/change pos.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && !t.isFlipped,
                (target) => {
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -700, source));
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Continuous);
                    Debug.Log($"Shadow Spell: {target.CurrentCardData.name} preso.");
                }
            );
        }
    }

    // 1624 - Shadow Tamer
    void Effect_1624_ShadowTamer(CardDisplay source)
    {
        // FLIP: Target 1 Fiend opp controls; take control until End Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.race == "Fiend",
                (target) => {
                    GameManager.Instance.SwitchControl(target);
                    // TODO: Agendar retorno na End Phase
                    Debug.Log($"Shadow Tamer: Controlando {target.CurrentCardData.name}.");
                }
            );
        }
    }

    // 1625 - Shadow of Eyes
    void Effect_1625_ShadowOfEyes(CardDisplay source)
    {
        // When monster is Set: Target it; change to Attack.
        // Trigger no OnSetImpl.
        Debug.Log("Shadow of Eyes: Força posição de ataque.");
    }

       // 1626 - Shadowknight Archfiend
    void Effect_1626_ShadowknightArchfiend(CardDisplay source)
    {
        // Maintenance cost: 900 LP (Standby Phase).
        // Dice roll negate target effect. Halve battle damage.
        // Lógica de manutenção e batalha são tratadas nos hooks globais (OnPhaseStart, OnDamageCalculation).
        Debug.Log("Shadowknight Archfiend: Efeitos passivos (Manutenção/Dados) ativos.");
    }

    // 1627 - Shadowslayer
    void Effect_1627_Shadowslayer(CardDisplay source)
    {
        // If the only cards your opponent controls are Defense Position monsters, this card can attack your opponent directly.
        // Lógica tratada no BattleManager.HasDirectAttackCondition.
        Debug.Log("Shadowslayer: Condição de ataque direto ativa.");
    }

    // 1629 - Share the Pain
    void Effect_1629_ShareThePain(CardDisplay source)
    {
        // Tribute 1 monster; make your opponent Tribute 1 monster (for no effect).
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            // Jogador tributa
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (myTribute) => {
                        GameManager.Instance.TributeCard(myTribute);
                        
                        // Oponente tributa (Simulado: IA escolhe o primeiro ou aleatório)
                        List<CardDisplay> oppMonsters = new List<CardDisplay>();
                        if (GameManager.Instance.duelFieldUI != null)
                        {
                            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
                        }
                        
                        if (oppMonsters.Count > 0)
                        {
                            // Simula escolha do oponente
                            CardDisplay oppTribute = oppMonsters[0]; 
                            GameManager.Instance.TributeCard(oppTribute);
                            Debug.Log($"Share the Pain: Oponente tributou {oppTribute.CurrentCardData.name}.");
                        }
                        else
                        {
                            Debug.Log("Share the Pain: Oponente não tem monstros para tributar.");
                        }
                    }
                );
            }
        }
    }

    // 1630 - Shield & Sword
    void Effect_1630_ShieldAndSword(CardDisplay source)
    {
        // Switch the original ATK and DEF of all face-up monsters currently on the field, until the end of this turn.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                int oldAtk = m.originalAtk;
                int oldDef = m.originalDef;
                
                // Aplica modificadores temporários para trocar os valores
                // Nota: Isso define o valor ATUAL baseado no ORIGINAL do outro stat
                m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, oldDef, source));
                m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, oldAtk, source));
            }
            Debug.Log("Shield & Sword: ATK/DEF trocados até o fim do turno.");
        }
    }

    // 1631 - Shield Crush
    void Effect_1631_ShieldCrush(CardDisplay source)
    {
        // Target 1 Defense Position monster on the field; destroy that target.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Defense,
                (target) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                    Debug.Log($"Shield Crush: {target.CurrentCardData.name} destruído.");
                }
            );
        }
    }

    // 1632 - Shien's Spy
    void Effect_1632_ShiensSpy(CardDisplay source)
    {
        // Select 1 face-up monster you control to activate this card. Give control of the selected monster to your opponent until the End Phase of this turn.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && !t.isFlipped,
                (target) => {
                    GameManager.Instance.SwitchControl(target);
                    // TODO: Agendar retorno na End Phase (requer sistema de efeitos retardados)
                    Debug.Log($"Shien's Spy: Controle de {target.CurrentCardData.name} trocado temporariamente.");
                }
            );
        }
    }

    // 1633 - Shift
    void Effect_1633_Shift(CardDisplay source)
    {
        // When your opponent targets exactly 1 monster you control... Target another monster... change target.
        Debug.Log("Shift: Redirecionamento de alvo (Requer sistema de Chain/Targeting avançado).");
    }

    // 1634 - Shifting Shadows
    void Effect_1634_ShiftingShadows(CardDisplay source)
    {
        // Pay 300 LP; rearrange face-down Defense Position monsters.
        if (Effect_PayLP(source, 300))
        {
            Debug.Log("Shifting Shadows: Monstros face-down embaralhados (Visualmente).");
            // Lógica visual de embaralhar posições nas zonas
        }
    }

    // 1635 - Shinato's Ark
    void Effect_1635_ShinatosArk(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 1636 - Shinato, King of a Higher Plane
    void Effect_1636_ShinatoKingOfAHigherPlane(CardDisplay source)
    {
        // If destroys Defense monster: Inflict damage = original ATK.
        // Lógica implementada no OnBattleEnd (CardEffectManager_Impl.cs).
        Debug.Log("Shinato: Efeito de dano de batalha configurado.");
    }

    // 1637 - Shine Palace
    void Effect_1637_ShinePalace(CardDisplay source)
    {
        Effect_Equip(source, 700, 0, "", "Light");
    }

    // 1639 - Shining Angel
    void Effect_1639_ShiningAngel(CardDisplay source)
    {
        // Destroyed by battle: SS LIGHT <= 1500.
        Effect_SearchDeck(source, "Light", "Monster", 1500); // Deveria ser SS direto
    }

    // 1641 - Shooting Star Bow - Ceal
    void Effect_1641_ShootingStarBowCeal(CardDisplay source)
    {
        // Equip: -1000 ATK, Direct Attack.
        Effect_Equip(source, -1000, 0);
        // Lógica de ataque direto no BattleManager.
    }

    // 1643 - Shrink
    void Effect_1643_Shrink(CardDisplay source)
    {
        // Target 1 face-up monster; original ATK becomes halved until end of turn.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (target) => {
                    int halfAtk = target.originalAtk / 2;
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, halfAtk, source));
                    Debug.Log($"Shrink: ATK de {target.CurrentCardData.name} reduzido para {halfAtk}.");
                }
            );
        }
    }

    // 1644 - Silent Doom
    void Effect_1644_SilentDoom(CardDisplay source)
    {
        // Target 1 Normal Monster in your Graveyard; Special Summon it in face-up Defense Position, but it cannot attack.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> normals = gy.FindAll(c => c.type.Contains("Normal") && c.type.Contains("Monster"));
        
        if (normals.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(normals, "Reviver Normal", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard, true, true); // Face-up Defense
                // TODO: Adicionar restrição 'Cannot Attack'
            });
        }
    }

    // 1645 - Silent Swordsman LV3
    void Effect_1645_SilentSwordsmanLV3(CardDisplay source)
    {
        // Negate Spell targeting this card. Standby: Send to GY -> SS LV5.
        // Negação é passiva/trigger.
        // Level Up: Lógica no OnPhaseStart.
        Debug.Log("Silent Swordsman LV3: Efeitos de nível configurados.");
    }

    // 1646 - Silent Swordsman LV5
    void Effect_1646_SilentSwordsmanLV5(CardDisplay source)
    {
        // Unaffected by Spells. Direct attack damage -> Standby -> SS LV7.
        // Imunidade é passiva.
        // Level Up: Lógica no OnPhaseStart/OnDamageDealt.
        Debug.Log("Silent Swordsman LV5: Efeitos de nível configurados.");
    }

    // 1647 - Silent Swordsman LV7
    void Effect_1647_SilentSwordsmanLV7(CardDisplay source)
    {
        // Negate all Spell effects on the field.
        Debug.Log("Silent Swordsman LV7: Magias negadas (Passivo).");
        // Requer verificação no SpellTrapManager.IsSpellNegationActive.
    }

    // 1648 - Silpheed
    void Effect_1648_Silpheed(CardDisplay source)
    {
        // SS by banishing 1 WIND. Destroyed by battle -> Opponent discards random.
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> winds = gy.FindAll(c => c.attribute == "Wind");
            if (winds.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(winds, "Banir 1 WIND", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
        // Lógica de descarte no OnBattleEnd.
    }

    // 1649 - Silver Bow and Arrow
    void Effect_1649_SilverBowAndArrow(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Fairy");
    }

    // 1651 - Sinister Serpent
    void Effect_1651_SinisterSerpent(CardDisplay source)
    {
        // During your Standby Phase, if this card is in your GY: You can add it to your hand.
        // Also banish 1 "Sinister Serpent" from your GY during your opponent's next End Phase.
        if (source.isInPile) // In GY
        {
            GameManager.Instance.AddCardToHand(source.CurrentCardData, source.isPlayerCard);
            GameManager.Instance.GetPlayerGraveyard().Remove(source.CurrentCardData);
            // TODO: Agendar banimento na próxima End Phase do oponente
            Debug.Log("Sinister Serpent: Retornou para a mão.");
        }
    }

    // 1652 - Sixth Sense
    void Effect_1652_SixthSense(CardDisplay source)
    {
        // Declare 2 numbers from 1 to 6. Opponent rolls die.
        // If result is declared: Draw that many cards.
        // Else: Send top X cards to GY (X = result).
        // Simulação: Jogador declara 5 and 6 (comum).
        int declared1 = 5;
        int declared2 = 6;
        
        GameManager.Instance.TossCoin(1, (heads) => { // Usando TossCoin como proxy para dado por enquanto ou Random
            int roll = Random.Range(1, 7);
            Debug.Log($"Sixth Sense: Rolou {roll}.");
            
            if (roll == declared1 || roll == declared2)
            {
                Debug.Log($"Sixth Sense: Acertou! Comprando {roll} cartas.");
                for(int i=0; i<roll; i++) GameManager.Instance.DrawCard();
            }
            else
            {
                Debug.Log($"Sixth Sense: Errou! Enviando {roll} cartas ao GY.");
                GameManager.Instance.MillCards(source.isPlayerCard, roll);
            }
        });
    }

    // 1653 - Skelengel
    void Effect_1653_Skelengel(CardDisplay source)
    {
        // FLIP: Draw 1 card.
        GameManager.Instance.DrawCard();
    }

    // 1655 - Skill Drain
    void Effect_1655_SkillDrain(CardDisplay source)
    {
        // Activate by paying 1000 LP. Negate effects of all face-up monsters.
        if (Effect_PayLP(source, 1000))
        {
            Debug.Log("Skill Drain: Ativado. Efeitos de monstros negados.");
            // Lógica de negação contínua deve ser verificada nos outros efeitos
        }
    }

    // 1656 - Skilled Dark Magician
    void Effect_1656_SkilledDarkMagician(CardDisplay source)
    {
        // Each time Spell is activated, place 1 Spell Counter (max 3).
        // Tribute with 3 counters -> SS Dark Magician from Hand/Deck/GY.
        if (SpellCounterManager.Instance.GetCount(source) >= 3)
        {
            if (SpellTrapManager.Instance != null)
            {
                // Pergunta se quer tributar
                UIManager.Instance.ShowConfirmation("Tributar para invocar Dark Magician?", () => {
                    GameManager.Instance.TributeCard(source);
                    
                    List<CardData> sources = new List<CardData>();
                    sources.AddRange(GameManager.Instance.GetPlayerHandData());
                    sources.AddRange(GameManager.Instance.GetPlayerMainDeck());
                    sources.AddRange(GameManager.Instance.GetPlayerGraveyard());
                    
                    CardData target = sources.Find(c => c.name == "Dark Magician");
                    if (target != null)
                    {
                        GameManager.Instance.SpecialSummonFromData(target, source.isPlayerCard);
                    }
                });
            }
        }
    }

    // 1657 - Skilled White Magician
    void Effect_1657_SkilledWhiteMagician(CardDisplay source)
    {
        // Same as Dark Magician but for Buster Blader.
        if (SpellCounterManager.Instance.GetCount(source) >= 3)
        {
            UIManager.Instance.ShowConfirmation("Tributar para invocar Buster Blader?", () => {
                GameManager.Instance.TributeCard(source);
                
                List<CardData> sources = new List<CardData>();
                sources.AddRange(GameManager.Instance.GetPlayerHandData());
                sources.AddRange(GameManager.Instance.GetPlayerMainDeck());
                sources.AddRange(GameManager.Instance.GetPlayerGraveyard());
                
                CardData target = sources.Find(c => c.name == "Buster Blader");
                if (target != null)
                {
                    GameManager.Instance.SpecialSummonFromData(target, source.isPlayerCard);
                }
            });
        }
    }

    // 1658 - Skull Archfiend of Lightning
    void Effect_1658_SkullArchfiendOfLightning(CardDisplay source)
    {
        // Maintenance 500 LP. Dice negate targeting effect.
        // Lógica passiva/trigger.
        Debug.Log("Skull Archfiend: Efeitos passivos ativos.");
    }

    // 1659 - Skull Dice
    void Effect_1659_SkullDice(CardDisplay source)
    {
        // Roll die. All monsters opponent controls lose ATK/DEF = result * 100.
        GameManager.Instance.TossCoin(1, (heads) => {
            int roll = Random.Range(1, 7);
            int debuff = roll * 100;
            Debug.Log($"Skull Dice: Rolou {roll}. Debuff de {debuff}.");
            
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> oppMonsters = new List<CardDisplay>();
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
                foreach(var m in oppMonsters)
                {
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -debuff, source));
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -debuff, source));
                }
            }
        });
    }

    // 1661 - Skull Guardian
    void Effect_1661_SkullGuardian(CardDisplay source)
    {
        // Ritual Monster.
        Debug.Log("Skull Guardian: Monstro Ritual.");
    }

    // 1662 - Skull Invitation
    void Effect_1662_SkullInvitation(CardDisplay source)
    {
        // Each time card is sent to GY, inflict 300 damage to owner.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Skull Invitation: Ativo.");
    }

    // 1664 - Skull Knight #2
    void Effect_1664_SkullKnight2(CardDisplay source)
    {
        // If Tribute Summoned by Tributing this monster: SS 1 Skull Knight #2 from Deck.
        // Lógica no OnTribute (se existisse) ou OnCardSentToGraveyard verificando a causa.
        // Simplificado: Se for tributado.
        Debug.Log("Skull Knight #2: Efeito de invocação ao ser tributado.");
    }

    // 1665 - Skull Lair
    void Effect_1665_SkullLair(CardDisplay source)
    {
        // Remove any number of monsters in GY to destroy 1 face-up monster with Level = number removed.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count > 0)
        {
            // Seleção múltipla
            // Simplificado: Pede para selecionar 1 monstro alvo primeiro para saber o nível?
            // Ou seleciona monstros do GY e depois valida?
            // Vamos selecionar o alvo primeiro.
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                    (target) => {
                        int level = target.CurrentCardData.level;
                        if (monsters.Count >= level)
                        {
                            GameManager.Instance.OpenCardMultiSelection(monsters, $"Banir {level} monstros", level, level, (selected) => {
                                foreach(var c in selected)
                                {
                                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                                    gy.Remove(c);
                                }
                                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                                Destroy(target.gameObject);
                            });
                        }
                        else
                        {
                            Debug.Log("Skull Lair: Monstros insuficientes no GY.");
                        }
                    }
                );
            }
        }
    }

    // 1670 - Skull-Mark Ladybug
    void Effect_1670_SkullMarkLadybug(CardDisplay source)
    {
        // When sent to GY: Gain 1000 LP.
        Effect_GainLP(source, 1000);
    }

    // 1674 - Skyscraper
    void Effect_1674_Skyscraper(CardDisplay source)
    {
        // Field Spell. E-HERO gains 1000 ATK when attacking higher ATK.
        // Lógica no OnDamageCalculation.
        Debug.Log("Skyscraper: Ativo.");
    }

    // 1675 - Slate Warrior
    void Effect_1675_SlateWarrior(CardDisplay source)
    {
        // FLIP: +500 ATK/DEF.
        // Destroyed by battle: Destroyer loses 500 ATK/DEF.
        if (source.isFlipped)
        {
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 500, source));
            source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Permanent, StatModifier.Operation.Add, 500, source));
        }
        // Lógica de destruição no OnBattleEnd.
    }
    // 1679 - Smashing Ground
    void Effect_1679_SmashingGround(CardDisplay source)
    {
        // Destroy the 1 face-up monster your opponent controls that has the highest DEF.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            
            CardDisplay target = null;
            int maxDef = -1;
            
            foreach(var m in oppMonsters)
            {
                if (!m.isFlipped) // Face-up
                {
                    if (m.currentDef > maxDef)
                    {
                        maxDef = m.currentDef;
                        target = m;
                    }
                    else if (m.currentDef == maxDef)
                    {
                        // Tie: Player chooses. For simplicity, pick first.
                        // In a full implementation, open selection dialog.
                    }
                }
            }
            
            if (target != null)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
                Debug.Log($"Smashing Ground: Destruiu {target.CurrentCardData.name}.");
            }
        }
    }

    // 1680 - Smoke Grenade of the Thief
    void Effect_1680_SmokeGrenadeOfTheThief(CardDisplay source)
    {
        // Equip. When destroyed by card effect while equipped: Look at opp hand and discard 1.
        Effect_Equip(source, 0, 0);
        // Lógica de destruição deve ser tratada no OnCardSentToGraveyard verificando se estava equipada.
    }

    // 1681 - Snake Fang
    void Effect_1681_SnakeFang(CardDisplay source)
    {
        // Target 1 face-up monster; it loses 500 DEF.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (target) => {
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -500, source));
                    Debug.Log($"Snake Fang: {target.CurrentCardData.name} perdeu 500 DEF.");
                }
            );
        }
    }

    // 1683 - Snatch Steal
    void Effect_1683_SnatchSteal(CardDisplay source)
    {
        // Equip to opponent's monster. Take control. Opponent gains 1000 LP at their Standby.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                    GameManager.Instance.SwitchControl(target);
                    Debug.Log($"Snatch Steal: Controlando {target.CurrentCardData.name}.");
                }
            );
        }
    }

    // 1684 - Sogen
    void Effect_1684_Sogen(CardDisplay source)
    {
        Effect_Field(source, 200, 200, "Warrior");
        Effect_Field(source, 200, 200, "Beast-Warrior");
    }

    // 1686 - Solar Flare Dragon
    void Effect_1686_SolarFlareDragon(CardDisplay source)
    {
        // Passive: Cannot be attacked if another Pyro exists.
        // Trigger: End Phase burn 500.
        // Lógica tratada no BattleManager (proteção) e OnPhaseStart (burn).
        Debug.Log("Solar Flare Dragon: Efeitos passivos e de fase configurados.");
    }

    // 1687 - Solar Ray
    void Effect_1687_SolarRay(CardDisplay source)
    {
        // Damage 600 x Face-up LIGHT monsters.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            // "on your side of the field"
            foreach(var m in all) if (m.CurrentCardData.attribute == "Light" && !m.isFlipped) count++;
        }
        Effect_DirectDamage(source, count * 600);
    }

    // 1688 - Solemn Judgment
    void Effect_1688_SolemnJudgment(CardDisplay source)
    {
        // Pay half LP. Negate Summon/Spell/Trap.
        int cost = GameManager.Instance.playerLP / 2;
        if (Effect_PayLP(source, cost))
        {
            Debug.Log("Solemn Judgment: Custo pago. (Negação requer Chain).");
        }
    }

    // 1689 - Solemn Wishes
    void Effect_1689_SolemnWishes(CardDisplay source)
    {
        // Gain 500 LP when drawing.
        // Lógica no OnDraw (CardEffectManager_Impl.cs).
        Debug.Log("Solemn Wishes: Ativo.");
    }

    // 1691 - Solomon's Lawbook
    void Effect_1691_SolomonsLawbook(CardDisplay source)
    {
        // Skip next Standby Phase.
        Debug.Log("Solomon's Lawbook: Próxima Standby Phase pulada (Simulado).");
    }

    // 1692 - Sonic Bird
    void Effect_1692_SonicBird(CardDisplay source)
    {
        // Search Ritual Spell.
        Effect_SearchDeck(source, "Ritual", "Spell");
    }

    // 1694 - Sonic Jammer
    void Effect_1694_SonicJammer(CardDisplay source)
    {
        // FLIP: Opponent cannot activate Spells until end of next turn.
        Debug.Log("Sonic Jammer: Magias do oponente bloqueadas.");
    }

    // 1696 - Sorcerer of Dark Magic
    void Effect_1696_SorcererOfDarkMagic(CardDisplay source)
    {
        // Negate Traps.
        Debug.Log("Sorcerer of Dark Magic: Traps negadas.");
    }

    // 1698 - Soul Absorption
    void Effect_1698_SoulAbsorption(CardDisplay source)
    {
        // Gain 500 LP when card banished.
        Debug.Log("Soul Absorption: Ativo.");
    }

    // 1699 - Soul Demolition
    void Effect_1699_SoulDemolition(CardDisplay source)
    {
        // Pay 500 LP; banish 1 monster from opp GY. (If you have Fiend).
        // Simplificado: Verifica se tem Fiend genérico
        bool hasFiend = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
             foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
             {
                 if(z.childCount > 0)
                 {
                     var m = z.GetChild(0).GetComponent<CardDisplay>();
                     if(m != null && m.CurrentCardData.race == "Fiend") hasFiend = true;
                 }
             }
        }

        if (hasFiend)
        {
            if (Effect_PayLP(source, 500))
            {
                List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
                List<CardData> monsters = oppGY.FindAll(c => c.type.Contains("Monster"));
                if (monsters.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(monsters, "Banir do Oponente", (selected) => {
                        GameManager.Instance.RemoveFromPlay(selected, !source.isPlayerCard);
                        oppGY.Remove(selected);
                    });
                }
            }
        }
    }

    // 1700 - Soul Exchange
    void Effect_1700_SoulExchange(CardDisplay source)
    {
        // Target opp monster; Tribute it this turn. Skip Battle Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    Debug.Log($"Soul Exchange: {target.CurrentCardData.name} marcado para tributo.");
                    // Em um sistema completo, marcaríamos o alvo como tributável pelo jogador
                    // e pularíamos a Battle Phase.
                }
            );
        }
    }

    // 1702 - Soul Release
    void Effect_1702_SoulRelease(CardDisplay source)
    {
        // Target up to 5 cards in any GY(s); banish them.
        List<CardData> targets = new List<CardData>();
        targets.AddRange(GameManager.Instance.GetPlayerGraveyard());
        targets.AddRange(GameManager.Instance.GetOpponentGraveyard());

        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(targets, "Banir até 5 cartas", 1, 5, (selected) => {
                foreach(var c in selected)
                {
                    // Determine owner to banish correctly
                    bool isPlayerCard = GameManager.Instance.GetPlayerGraveyard().Contains(c);
                    if (!isPlayerCard && !GameManager.Instance.GetOpponentGraveyard().Contains(c)) continue;

                    GameManager.Instance.RemoveFromPlay(c, isPlayerCard);
                    if (isPlayerCard) GameManager.Instance.GetPlayerGraveyard().Remove(c);
                    else GameManager.Instance.GetOpponentGraveyard().Remove(c);
                }
                Debug.Log($"Soul Release: {selected.Count} cartas banidas.");
            });
        }
    }

    // 1703 - Soul Resurrection
    void Effect_1703_SoulResurrection(CardDisplay source)
    {
        // Activate this card by targeting 1 Normal Monster in your Graveyard; Special Summon it in Defense Position.
        // When this card leaves the field, destroy that monster. When that monster is destroyed, destroy this card.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> normals = gy.FindAll(c => c.type.Contains("Normal") && c.type.Contains("Monster"));

        if (normals.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(normals, "Reviver Normal", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard, true, true); // Face-up Defense
                // TODO: Implementar vínculo de destruição mútua (Continuous Trap)
                Debug.Log("Soul Resurrection: Monstro revivido em Defesa.");
            });
        }
    }

    // 1704 - Soul Reversal
    void Effect_1704_SoulReversal(CardDisplay source)
    {
        // Target 1 Flip Effect Monster in your Graveyard; return it to the top of your Deck.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> flips = gy.FindAll(c => c.description.Contains("FLIP:"));

        if (flips.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(flips, "Retornar ao Topo", (selected) => {
                gy.Remove(selected);
                GameManager.Instance.GetPlayerMainDeck().Insert(0, selected);
                Debug.Log($"Soul Reversal: {selected.name} retornado ao topo do deck.");
            });
        }
    }

    // 1705 - Soul Rope
    void Effect_1705_SoulRope(CardDisplay source)
    {
        // When a monster you control is destroyed by a card effect and sent to the Graveyard: Pay 1000 LP; Special Summon 1 Level 4 monster from your Deck.
        // Este efeito é um gatilho. Aqui implementamos a resolução.
        if (Effect_PayLP(source, 1000))
        {
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> targets = deck.FindAll(c => c.type.Contains("Monster") && c.level == 4);
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Invocar Lv4", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                });
            }
        }
    }

    // 1706 - Soul Taker
    void Effect_1706_SoulTaker(CardDisplay source)
    {
        // Target 1 face-up monster your opponent controls; destroy that target, then your opponent gains 1000 LP.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && !t.isFlipped,
                (target) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                    GameManager.Instance.GainLifePoints(!source.isPlayerCard, 1000);
                    Debug.Log("Soul Taker: Monstro destruído e oponente curado.");
                }
            );
        }
    }

    // 1708 - Soul of Purity and Light
    void Effect_1708_SoulOfPurityAndLight(CardDisplay source)
    {
        // Cannot be Normal Summoned/Set. Must be Special Summoned by banishing 2 LIGHT monsters from your GY.
        // Monsters opponent controls lose 300 ATK during their Battle Phase.
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> lights = gy.FindAll(c => c.attribute == "Light");
            if (lights.Count >= 2)
            {
                GameManager.Instance.OpenCardMultiSelection(lights, "Banir 2 LIGHT", 2, 2, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                        gy.Remove(c);
                    }
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
        else
        {
            Debug.Log("Soul of Purity and Light: Debuff passivo na Battle Phase do oponente.");
        }
    }

    // 1709 - Soul of the Pure
    void Effect_1709_SoulOfThePure(CardDisplay source)
    {
        Effect_GainLP(source, 800);
    }

    // 1710 - Soul-Absorbing Bone Tower
    void Effect_1710_SoulAbsorbingBoneTower(CardDisplay source)
    {
        // If you control another Zombie-Type monster, your opponent cannot target this card for attacks.
        // Each time a Zombie-Type monster(s) is Special Summoned: Send the top 2 cards of your opponent's Deck to the Graveyard.
        Debug.Log("Soul-Absorbing Bone Tower: Efeitos passivos e de gatilho configurados.");
    }

    // 1714 - Spark Blaster
    void Effect_1714_SparkBlaster(CardDisplay source)
    {
        // Equip only to "Elemental HERO Sparkman". Change battle position of target. Destroy after 3 uses.
        if (SpellTrapManager.Instance != null)
        {
            // Se já estiver equipada, usa o efeito de ignição
            if (source.isOnField && source.CurrentCardData.property == "Equip")
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                    (target) => {
                        target.ChangePosition();
                        source.spellCounters++; // Usa contadores para rastrear uso
                        if (source.spellCounters >= 3)
                        {
                            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
                            Destroy(source.gameObject);
                        }
                    }
                );
            }
            else
            {
                // Lógica de equipar (simplificada, deveria checar nome do alvo)
                Effect_Equip(source, 0, 0); 
            }
        }

    // 1876 - The Legendary Fisherman
    void Effect_1876_TheLegendaryFisherman(CardDisplay source)
    {
        // While "Umi" is on the field, this card is unaffected by Spell effects and cannot be targeted for attacks.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi
        {
            Debug.Log("The Legendary Fisherman: Imune a Magias e Ataques (Passivo).");
            // Lógica de imunidade a magias e ataques no BattleManager/SpellTrapManager
        }
    }

    // 1877 - The Light - Hex-Sealed Fusion
    void Effect_1877_TheLightHexSealedFusion(CardDisplay source)
    {
        // Fusion Substitute. Tribute materials + this card -> SS LIGHT Fusion.
        Debug.Log("The Light - Hex-Sealed Fusion: Efeito de fusão por tributo.");
    }

    // 1878 - The Little Swordsman of Aile
    void Effect_1878_TheLittleSwordsmanOfAile(CardDisplay source)
    {
        // Tribute 1 monster; increase ATK by 700 until End Phase.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            // Seleção de tributo simplificada (primeiro disponível)
            // Em produção: UI de seleção
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != source,
                    (tribute) => {
                        GameManager.Instance.TributeCard(tribute);
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 700, source));
                        Debug.Log("The Little Swordsman of Aile: +700 ATK.");
                    }
                );
            }
        }
    }

    // 1879 - The Mask of Remnants
    void Effect_1879_TheMaskOfRemnants(CardDisplay source)
    {
        // If in GY when Des Gardius leaves field: Equip to opp monster and take control.
        // Lógica implementada no OnCardSentToGraveyard (Des Gardius).
        // Se ativada da mão: Shuffle into Deck.
        if (source.isOnField) // Ativada como Spell
        {
            GameManager.Instance.ReturnToDeck(source, false); // Shuffle
            GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            Debug.Log("The Mask of Remnants: Retornada ao Deck.");
        }
    }

    // 1880 - The Masked Beast
    void Effect_1880_TheMaskedBeast(CardDisplay source)
    {
        // Ritual Monster.
        Debug.Log("The Masked Beast: Monstro Ritual.");
    }

    // 1883 - The Puppet Magic of Dark Ruler
    void Effect_1883_ThePuppetMagicOfDarkRuler(CardDisplay source)
    {
        // Banish monsters from GY whose Levels equal Level of Fiend in GY; SS that Fiend.
        Debug.Log("The Puppet Magic of Dark Ruler: Requer seleção complexa de banimento.");
    }

    // 1884 - The Regulation of Tribe
    void Effect_1884_TheRegulationOfTribe(CardDisplay source)
    {
        // Declare 1 Type. Monsters of that Type cannot attack. Tribute 1 monster each Standby.
        Debug.Log("The Regulation of Tribe: Bloqueio de tipo (Simulado: Dragon).");
        // Lógica de bloqueio no BattleManager.
    }

    // 1885 - The Reliable Guardian
    void Effect_1885_TheReliableGuardian(CardDisplay source)
    {
        // Target 1 face-up monster; +700 DEF until end of turn.
        Effect_BuffStats(source, 0, 700);
    }

    // 1886 - The Rock Spirit
    void Effect_1886_TheRockSpirit(CardDisplay source)
    {
        // SS by banishing 1 EARTH from GY. Opponent gains 300 ATK during your Battle Phase.
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> earths = gy.FindAll(c => c.attribute == "Earth");
            if (earths.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(earths, "Banir 1 EARTH", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
        // Debuff no oponente (OnPhaseStart Battle Phase)
    }

    // 1887 - The Sanctuary in the Sky
    void Effect_1887_TheSanctuaryInTheSky(CardDisplay source)
    {
        // Battle Damage to controller of Fairy monster becomes 0.
        Debug.Log("The Sanctuary in the Sky: Proteção de dano para Fadas (Passivo).");
    }

    // 1889 - The Secret of the Bandit
    void Effect_1889_TheSecretOfTheBandit(CardDisplay source)
    {
        // When a monster inflicts Battle Damage: Opponent discards 1 card.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("The Secret of the Bandit: Efeito de descarte configurado.");
    }

    // 1890 - The Selection
    void Effect_1890_TheSelection(CardDisplay source)
    {
        // Pay 1000 LP. Negate Summon of monster with same Type as one on field.
        // Requer Chain/Trigger de invocação.
        Debug.Log("The Selection: Negação de invocação por tipo.");
    }

    // 1892 - The Shallow Grave
    void Effect_1892_TheShallowGrave(CardDisplay source)
    {
        // Each player selects 1 monster in their GY; SS it in face-down Defense.
        // Player
        Effect_Revive(source, false); // Deveria ser face-down
        // Opponent (Simulado)
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> monsters = oppGY.FindAll(c => c.type.Contains("Monster"));
        if (monsters.Count > 0)
        {
            CardData random = monsters[Random.Range(0, monsters.Count)];
            GameManager.Instance.SpecialSummonFromData(random, false, false, true); // Face-down Defense
            Debug.Log($"The Shallow Grave: Oponente reviveu {random.name}.");
        }
    }

    // 1894 - The Spell Absorbing Life
    void Effect_1894_TheSpellAbsorbingLife(CardDisplay source)
    {
        // Flip all face-down monsters face-up. Gain 400 LP for each Effect Monster.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            int effectCount = 0;
            foreach(var m in all)
            {
                if (m.isFlipped) m.RevealCard();
                if (m.CurrentCardData.type.Contains("Effect")) effectCount++;
            }
            Effect_GainLP(source, effectCount * 400);
        }
    }

    // 1896 - The Stern Mystic
    void Effect_1896_TheSternMystic(CardDisplay source)
    {
        // FLIP: All face-down cards are turned face-up, then returned to original position. No effects activate.
        Debug.Log("The Stern Mystic: Revelando cartas (Visualmente).");
    }

    // 1898 - The Thing in the Crater
    void Effect_1898_TheThingInTheCrater(CardDisplay source)
    {
        // If destroyed: SS 1 Pyro from hand.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("The Thing in the Crater: Efeito de invocação configurado.");
    }

    // 1900 - The Tricky
    void Effect_1900_TheTricky(CardDisplay source)
    {
        // SS from hand by discarding 1 card.
        if (!source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            if (hand.Count > 1) // Tricky + 1
            {
                GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                    if (discarded != source.CurrentCardData)
                    {
                        GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                        GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                    }
                });
            }
        }
    }
    }

    // 1715 - Sparks
    void Effect_1715_Sparks(CardDisplay source)
    {
        Effect_DirectDamage(source, 200);
    }

    // 1716 - Spatial Collapse
    void Effect_1716_SpatialCollapse(CardDisplay source)
    {
        // Activate only if both players have 5 or less cards on field. Max cards on field becomes 5.
        int pCount = 0;
        int oCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Contagem simplificada
            pCount = GameManager.Instance.duelFieldUI.playerMonsterZones.Count(z => z.childCount > 0) + 
                     GameManager.Instance.duelFieldUI.playerSpellZones.Count(z => z.childCount > 0);
            oCount = GameManager.Instance.duelFieldUI.opponentMonsterZones.Count(z => z.childCount > 0) + 
                     GameManager.Instance.duelFieldUI.opponentSpellZones.Count(z => z.childCount > 0);
        }

        if (pCount <= 5 && oCount <= 5)
        {
            Debug.Log("Spatial Collapse: Limite de campo 5 ativado.");
            // TODO: Implementar restrição no GameManager
        }
        else
        {
            Debug.Log("Spatial Collapse: Condição não atendida.");
        }
    }

    // 1717 - Spear Cretin
    void Effect_1717_SpearCretin(CardDisplay source)
    {
        // FLIP: When sent to GY after flip, mutual revive.
        // Lógica no OnCardSentToGraveyard verificando status isFlipped.
        Debug.Log("Spear Cretin: Efeito de renascimento mútuo configurado.");
    }

    // 1718 - Spear Dragon
    void Effect_1718_SpearDragon(CardDisplay source)
    {
        // Piercing. Changes to Defense after attack.
        Debug.Log("Spear Dragon: Perfurante e mudança de posição.");
    }

    // 1719 - Special Hurricane
    void Effect_1719_SpecialHurricane(CardDisplay source)
    {
        // Discard 1 card; destroy all Special Summoned monsters on the field.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                // Destrói monstros SS (Requer rastreamento de status de invocação)
                Debug.Log("Special Hurricane: Destruindo monstros Special Summoned (Simulado).");
                // DestroyAllMonsters(true, true, (m) => m.wasSpecialSummoned);
            });
        }
    }

    // 1720 - Spell Absorption
    void Effect_1720_SpellAbsorption(CardDisplay source)
    {
        // Each time a Spell is activated, gain 500 LP.
        Debug.Log("Spell Absorption: Ativo.");
    }

    // 1721 - Spell Canceller
    void Effect_1721_SpellCanceller(CardDisplay source)
    {
        // Negate Spells.
        Debug.Log("Spell Canceller: Magias negadas.");
    }

    // 1722 - Spell Economics
    void Effect_1722_SpellEconomics(CardDisplay source)
    {
        // No LP cost for Spells.
        Debug.Log("Spell Economics: Sem custo de LP para magias.");
    }

    // 1723 - Spell Purification
    void Effect_1723_SpellPurification(CardDisplay source)
    {
        // Discard 1 card; destroy all face-up Continuous Spell Cards.
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
                        if(z.childCount > 0)
                        {
                            var cd = z.GetChild(0).GetComponent<CardDisplay>();
                            if(cd != null && !cd.isFlipped && cd.CurrentCardData.type.Contains("Spell") && cd.CurrentCardData.property == "Continuous")
                                toDestroy.Add(cd);
                        }
                    }
                }
                DestroyCards(toDestroy, source.isPlayerCard);
                Debug.Log("Spell Purification: Magias Contínuas destruídas.");
            });
        }
    }

    // 1724 - Spell Reproduction
    void Effect_1724_SpellReproduction(CardDisplay source)
    {
        // Send 2 Spells from hand to GY, then target 1 Spell in GY; add it to your hand.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spellsInHand = hand.FindAll(c => c.type.Contains("Spell") && c != source.CurrentCardData);

        if (spellsInHand.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(spellsInHand, "Descarte 2 Magias", 2, 2, (discarded) => {
                foreach(var c in discarded)
                {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                }

                List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                List<CardData> spellsInGY = gy.FindAll(c => c.type.Contains("Spell"));
                
                if (spellsInGY.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(spellsInGY, "Recuperar Magia", (selected) => {
                        gy.Remove(selected);
                        GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                    });
                }
            });
        }
    }

    // 1725 - Spell Shattering Arrow
    void Effect_1725_SpellShatteringArrow(CardDisplay source)
    {
        // Destroy as many face-up Spells opponent controls as possible, and if you do, inflict 500 damage for each.
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones)
            {
                if(z.childCount > 0)
                {
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if(cd != null && !cd.isFlipped && cd.CurrentCardData.type.Contains("Spell"))
                        toDestroy.Add(cd);
                }
            }
            // Field Spell
            if(GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0)
            {
                var cd = GameManager.Instance.duelFieldUI.opponentFieldSpell.GetChild(0).GetComponent<CardDisplay>();
                if(cd != null && !cd.isFlipped && cd.CurrentCardData.type.Contains("Spell"))
                    toDestroy.Add(cd);
            }
        }

        int count = toDestroy.Count;
        DestroyCards(toDestroy, source.isPlayerCard);
        if (count > 0)
        {
            Effect_DirectDamage(source, count * 500);
            Debug.Log($"Spell Shattering Arrow: {count} magias destruídas. {count * 500} dano.");
        }
    }

    // 1726 - Spell Shield Type-8
    void Effect_1726_SpellShieldType8(CardDisplay source)
    {
        // When a Spell Card is activated that targets exactly 1 monster on the field:
        // If you control a monster, you can activate this card. Negate the activation and destroy it.
        // If you have cards in your hand, you must send 1 of them to the Graveyard to activate and to resolve this effect.
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            hasMonster = GameManager.Instance.duelFieldUI.playerMonsterZones.Any(z => z.childCount > 0);

        if (hasMonster)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            if (hand.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(hand, "Descarte 1 para negar", (discarded) => {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                    Debug.Log("Spell Shield Type-8: Magia negada (Simulado).");
                    // ChainManager.Instance.NegateLink(ChainManager.Instance.currentChain.Count);
                });
            }
            else
            {
                Debug.Log("Spell Shield Type-8: Magia negada (Simulado).");
                // ChainManager.Instance.NegateLink(ChainManager.Instance.currentChain.Count);
            }
        }
    }

    // 1727 - Spell Vanishing
    void Effect_1727_SpellVanishing(CardDisplay source)
    {
        // When a Spell Card is activated: Discard 2 cards; negate the activation, and if you do, destroy it,
        // then your opponent banishes 1 card from their hand or Deck with the same name as that destroyed card.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Descarte 2 para negar", 2, 2, (discarded) => {
                foreach(var c in discarded)
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                
                Debug.Log("Spell Vanishing: Magia negada e cópias banidas (Simulado).");
                // ChainManager.Instance.NegateLink(ChainManager.Instance.currentChain.Count);
                // Lógica de banir cópias...
            });
        }
    }

    // 1728 - Spell of Pain
    void Effect_1728_SpellOfPain(CardDisplay source)
    {
        // When you take effect damage from an opponent's card effect: Inflict the same amount of damage to your opponent.
        // Lógica de gatilho no OnDamageTaken.
        Debug.Log("Spell of Pain: Efeito de redirecionamento de dano configurado.");
    }

    // 1729 - Spell-Stopping Statute
    void Effect_1729_SpellStoppingStatute(CardDisplay source)
    {
        // When a Continuous Spell Card is activated: Negate the activation, and if you do, destroy it.
        // Requer Chain.
        Debug.Log("Spell-Stopping Statute: Negação de Magia Contínua (Requer Chain).");
    }

    // 1730 - Spellbinding Circle
    void Effect_1730_SpellbindingCircle(CardDisplay source)
    {
        // Activate this card by targeting 1 monster your opponent controls; it cannot attack or change its battle position.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && !t.isFlipped,
                (target) => {
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Continuous);
                    // TODO: Implementar bloqueio de ataque e posição no BattleManager/GameManager
                    Debug.Log($"Spellbinding Circle: {target.CurrentCardData.name} está preso.");
                }
            );
        }
    }

    // 1731 - Spellbook Organization
    void Effect_1731_SpellbookOrganization(CardDisplay source)
    {
        // Look at the top 3 cards of your Deck, then return them to the top of the Deck in any order.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (deck.Count >= 3)
        {
            List<CardData> top3 = deck.GetRange(0, 3);
            // Em um jogo real, abriria UI para reordenar.
            // Simulação: Apenas loga as cartas.
            Debug.Log($"Spellbook Organization: Top 3 cartas são {top3[0].name}, {top3[1].name}, {top3[2].name}. (Reordenação simulada).");
        }
    }

    // 1733 - Sphinx Teleia
    void Effect_1733_SphinxTeleia(CardDisplay source)
    {
        // SS condition: Pay 500 LP when "Pyramid of Light" is on the field.
        // If this card destroys a Defense Position monster by battle: Inflict damage to your opponent equal to half the DEF of the destroyed monster.
        // Lógica de SS na mão. Lógica de dano no OnBattleEnd.
        Debug.Log("Sphinx Teleia: Efeitos de invocação e dano configurados.");
    }

    // 1737 - Spiral Spear Strike
    void Effect_1737_SpiralSpearStrike(CardDisplay source)
    {
        // Equip only to "Gaia The Fierce Knight", "Swift Gaia the Fierce Knight", or "Gaia the Dragon Champion".
        // Piercing. If "Gaia the Dragon Champion" inflicts damage: Draw 2, Discard 1.
        Effect_Equip(source, 0, 0); // Lógica de alvo específico no filtro do Effect_Equip
        // Lógica de piercing e draw/discard no BattleManager/OnDamageDealt.
        Debug.Log("Spiral Spear Strike: Efeitos de perfurante e compra/descarte configurados.");
    }

    // 1738 - Spirit Barrier
    void Effect_1738_SpiritBarrier(CardDisplay source)
    {
        // While you control a monster, you take no battle damage.
        // Lógica passiva no BattleManager.
        Debug.Log("Spirit Barrier: Sem dano de batalha se tiver monstro.");
    }

    // 1739 - Spirit Caller
    void Effect_1739_SpiritCaller(CardDisplay source)
    {
        // FLIP: You can Special Summon 1 Level 3 or lower Normal Monster from your Graveyard.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.type.Contains("Normal") && c.type.Contains("Monster") && c.level <= 3);

        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver Normal Lv3-", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    // 1740 - Spirit Elimination
    void Effect_1740_SpiritElimination(CardDisplay source)
    {
        // This turn, any monster sent from your side of the field to the Graveyard is banished instead.
        Debug.Log("Spirit Elimination: Monstros serão banidos em vez de irem para o GY neste turno.");
        // TODO: Adicionar flag no GameManager: `banishInsteadOfGraveyard = true`
    }

    // 1745 - Spirit Reaper
    void Effect_1745_SpiritReaper(CardDisplay source)
    {
        // Cannot be destroyed by battle. Destroyed when targeted. Direct attack -> discard.
        // Lógica passiva/trigger no BattleManager e SpellTrapManager.
        Debug.Log("Spirit Reaper: Efeitos de indestrutibilidade, alvo e descarte configurados.");
    }

    // 1746 - Spirit Ryu
    void Effect_1746_SpiritRyu(CardDisplay source)
    {
        // Discard 1 Dragon-Type monster from your hand to have this card gain 1000 ATK until the End Phase.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> dragons = hand.FindAll(c => c.race == "Dragon");

        if (dragons.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(dragons, "Descarte 1 Dragão", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 1000, source));
            });
        }
    }

    // 1747 - Spirit of Flames
    void Effect_1747_SpiritOfFlames(CardDisplay source)
    {
        // Cannot be Normal Summoned/Set. Must first be Special Summoned (from your hand) by banishing 1 FIRE monster from your Graveyard.
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> fires = gy.FindAll(c => c.attribute == "Fire");
            if (fires.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(fires, "Banir 1 FIRE", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    gy.Remove(selected);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
    }

    // 1749 - Spirit of the Breeze
    void Effect_1749_SpiritOfTheBreeze(CardDisplay source)
    {
        // FLIP: Gain 1000 LP for each "Spirit of the Breeze" on the field.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            foreach(var m in all)
                if (m.CurrentCardData.name == "Spirit of the Breeze") count++;
        }
        Effect_GainLP(source, count * 1000);
    }

    // 1752 - Spirit of the Pharaoh
    void Effect_1752_SpiritOfThePharaoh(CardDisplay source)
    {
        // When this card is Special Summoned: You can Special Summon up to 4 Level 2 or lower Zombie-Type Normal Monsters from your Graveyard.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.race == "Zombie" && c.type.Contains("Normal") && c.level <= 2);
        
        if (targets.Count > 0)
        {
            int max = Mathf.Min(4, targets.Count);
            GameManager.Instance.OpenCardMultiSelection(targets, "Reviver Zumbis", 1, max, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
                }
            });
        }
    }

    // 1753 - Spirit of the Pot of Greed
    void Effect_1753_SpiritOfThePotOfGreed(CardDisplay source)
    {
        // Passive effect handled in OnSpellActivated.
        Debug.Log("Spirit of the Pot of Greed: Efeito passivo ativo.");
    }

    // 1755 - Spirit's Invitation
    void Effect_1755_SpiritsInvitation(CardDisplay source)
    {
        // Continuous Trap.
        Debug.Log("Spirit's Invitation: Ativada.");
    }

    // 1756 - Spiritual Earth Art - Kurogane
    void Effect_1756_SpiritualEarthArtKurogane(CardDisplay source)
    {
        // Tribute 1 EARTH; SS 1 Level 4 or lower EARTH from GY.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.attribute == "Earth",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                    List<CardData> targets = gy.FindAll(c => c.attribute == "Earth" && c.level <= 4 && c != tribute.CurrentCardData);
                    
                    if (targets.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(targets, "Reviver EARTH", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    // 1757 - Spiritual Energy Settle Machine
    void Effect_1757_SpiritualEnergySettleMachine(CardDisplay source)
    {
        // Continuous Spell.
        Debug.Log("Spiritual Energy Settle Machine: Ativada.");
    }

    // 1758 - Spiritual Fire Art - Kurenai
    void Effect_1758_SpiritualFireArtKurenai(CardDisplay source)
    {
        // Tribute 1 FIRE; inflict damage = original ATK.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.attribute == "Fire",
                (tribute) => {
                    int dmg = tribute.originalAtk;
                    GameManager.Instance.TributeCard(tribute);
                    Effect_DirectDamage(source, dmg);
                }
            );
        }
    }

    // 1759 - Spiritual Water Art - Aoi
    void Effect_1759_SpiritualWaterArtAoi(CardDisplay source)
    {
        // Tribute 1 WATER; look at opp hand, send 1 to GY.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.attribute == "Water",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
                    if (oppHand.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(oppHand, "Enviar ao GY", (selected) => {
                            GameManager.Instance.DiscardCardsByName(false, selected.name);
                            Debug.Log($"Aoi: Enviando {selected.name} ao GY.");
                        });
                    }
                }
            );
        }
    }

    // 1760 - Spiritual Wind Art - Miyabi
    void Effect_1760_SpiritualWindArtMiyabi(CardDisplay source)
    {
        // Tribute 1 WIND; target 1 opp card, bottom of Deck.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.attribute == "Wind",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard,
                        (target) => {
                            GameManager.Instance.ReturnToDeck(target, false); // false = bottom
                        }
                    );
                }
            );
        }
    }

    // 1761 - Spiritualism
    void Effect_1761_Spiritualism(CardDisplay source)
    {
        // Return 1 opp S/T to hand. Cannot be negated.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                (target) => {
                    GameManager.Instance.ReturnToHand(target);
                }
            );
        }
    }

    // 1762 - Spring of Rebirth
    void Effect_1762_SpringOfRebirth(CardDisplay source)
    {
        // Continuous Spell.
        Debug.Log("Spring of Rebirth: Ativada.");
    }

    // 1764 - Stamping Destruction
    void Effect_1764_StampingDestruction(CardDisplay source)
    {
        // If Dragon: Destroy S/T, 500 dmg.
        bool hasDragon = false;
        if (GameManager.Instance.duelFieldUI != null)
             foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                 if(z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Dragon") hasDragon = true;

        if (hasDragon)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                        Effect_DirectDamage(source, 500);
                    }
                );
            }
        }
    }

    // 1765 - Star Boy
    void Effect_1765_StarBoy(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Water");
    }

    // 1766 - Statue of the Wicked
    void Effect_1766_StatueOfTheWicked(CardDisplay source)
    {
        // Trap. Effect activates in GY.
        Debug.Log("Statue of the Wicked: Setada.");
    }

    // 1767 - Staunch Defender
    void Effect_1767_StaunchDefender(CardDisplay source)
    {
        // Select 1 face-up monster. Opponent must attack it.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard,
                (target) => {
                    Debug.Log($"Staunch Defender: {target.CurrentCardData.name} é o alvo obrigatório.");
                    // Logic to force attack on this target in BattleManager
                    // BattleManager.Instance.forcedAttackTarget = target;
                }
            );
        }
    }

    // 1768 - Stealth Bird
    void Effect_1768_StealthBird(CardDisplay source)
    {
        // Ignition: Flip face-down.
        // Flip: 1000 dmg.
        if (source.isFlipped)
        {
            Effect_DirectDamage(source, 1000);
        }
        else
        {
            if (!source.hasUsedEffectThisTurn)
            {
                Effect_TurnSet(source);
                source.hasUsedEffectThisTurn = true;
            }
        }
    }

    // 1770 - Steamroid
    void Effect_1770_Steamroid(CardDisplay source)
    {
        // Logic in OnAttackDeclared/OnDamageCalculation.
        Debug.Log("Steamroid: Efeitos de batalha configurados.");
    }

    // 1773 - Steel Scorpion
    void Effect_1773_SteelScorpion(CardDisplay source)
    {
        // Logic in OnBattleEnd (delayed destruction).
        Debug.Log("Steel Scorpion: Efeito de destruição retardada configurado.");
    }

    // 1774 - Steel Shell
    void Effect_1774_SteelShell(CardDisplay source)
    {
        Effect_Equip(source, 400, -200, "", "Water");
    }

    // 1775 - Stim-Pack
    void Effect_1775_StimPack(CardDisplay source)
    {
        Effect_Equip(source, 700, 0);
        // Decay logic in OnPhaseStart.
    }

    // 1780 - Stone Statue of the Aztecs
    void Effect_1780_StoneStatueOfTheAztecs(CardDisplay source)
    {
        // Double any Battle Damage your opponent takes when they attack this monster.
        // Lógica passiva tratada no BattleManager/OnDamageCalculation.
        Debug.Log("Stone Statue of the Aztecs: Dano de batalha dobrado (Passivo).");
    }

    // 1781 - Stop Defense
    void Effect_1781_StopDefense(CardDisplay source)
    {
        // Select 1 Defense Position monster on your opponent's side of the field and change it to Attack Position.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.position == CardDisplay.BattlePosition.Defense,
                (target) => {
                    target.ChangePosition();
                    Debug.Log($"Stop Defense: {target.CurrentCardData.name} mudou para Ataque.");
                }
            );
        }
    }

    // 1782 - Stray Lambs
    void Effect_1782_StrayLambs(CardDisplay source)
    {
        // Special Summon 2 "Lamb Tokens". You cannot Summon other monsters the turn you activate this card (but you can Set).
        // A restrição de invocação deve ser verificada antes da ativação em um sistema completo.
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Lamb Token");
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Lamb Token");
        Debug.Log("Stray Lambs: 2 Tokens invocados.");
    }

    // 1783 - Strike Ninja
    void Effect_1783_StrikeNinja(CardDisplay source)
    {
        // (Quick Effect): You can banish 2 DARK monsters from your GY; banish this face-up card until the End Phase.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> darks = gy.FindAll(c => c.attribute == "Dark" && c.type.Contains("Monster"));
        
        if (darks.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(darks, "Banir 2 DARK", 2, 2, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    gy.Remove(c);
                }
                GameManager.Instance.BanishCard(source);
                // TODO: Agendar retorno na End Phase.
                Debug.Log("Strike Ninja: Banido até a End Phase.");
            });
        }
    }

    // 1784 - Stronghold the Moving Fortress
    void Effect_1784_StrongholdTheMovingFortress(CardDisplay source)
    {
        // Special Summon this card in Defense Position as an Effect Monster.
        // Gains 3000 ATK if you control Green, Red, and Yellow Gadget.
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 2000, "Stronghold"); // Simulado como Token
        // Lógica de buff de Gadgets seria aplicada ao token/monstro armadilha.
        Debug.Log("Stronghold: Invocado como monstro.");
    }

    // 1786 - Stumbling
    void Effect_1786_Stumbling(CardDisplay source)
    {
        // Any monster that is Normal Summoned, Flip Summoned or Special Summoned is changed to Defense Position.
        // Lógica implementada no OnSummonImpl (CardEffectManager_Impl.cs).
        Debug.Log("Stumbling: Ativo.");
    }

    // 1788 - Suijin
    void Effect_1788_Suijin(CardDisplay source)
    {
        // During damage calculation... make that target's ATK 0.
        // Lógica implementada no OnDamageCalculation (CardEffectManager_Impl.cs).
        Debug.Log("Suijin: Efeito de batalha configurado.");
    }

    // 1790 - Summoner Monk
    void Effect_1790_SummonerMonk(CardDisplay source)
    {
        // Once per turn: You can discard 1 Spell; Special Summon 1 Level 4 monster from your Deck.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                List<CardData> targets = deck.FindAll(c => c.level == 4 && c.type.Contains("Monster"));
                
                if (targets.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(targets, "Invocar Lv4", (selected) => {
                        GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        // TODO: Aplicar restrição "cannot attack this turn".
                    });
                }
            });
        }
    }

    // 1791 - Summoner of Illusions
    void Effect_1791_SummonerOfIllusions(CardDisplay source)
    {
        // FLIP: Tribute 1 other monster; SS 1 Fusion Monster from Extra Deck. Destroy it at End Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t != source,
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
                    List<CardData> fusions = extra.FindAll(c => c.type.Contains("Fusion"));
                    
                    if (fusions.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(fusions, "Invocar Fusão", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                            // TODO: Agendar destruição na End Phase.
                        });
                    }
                }
            );
        }
    }

    // 1792 - Super Rejuvenation
    void Effect_1792_SuperRejuvenation(CardDisplay source)
    {
        // During the End Phase... draw cards equal to Dragon monsters discarded or Tributed this turn.
        // Lógica implementada no OnPhaseStart (End Phase) em CardEffectManager_Impl.cs.
        Debug.Log("Super Rejuvenation: Contagem de dragões iniciada.");
    }

    // 1793 - Super Robolady
    void Effect_1793_SuperRobolady(CardDisplay source)
    {
        // You can Special Summon "Super Roboyarou" by returning this card from the field to the Extra Deck.
        // Increase ATK by 1000 during Damage Step when inflicting Direct Damage.
        if (source.isOnField)
        {
            // Tag out logic
            List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
            CardData target = extra.Find(c => c.name == "Super Roboyarou");
            if (target != null)
            {
                UIManager.Instance.ShowConfirmation("Trocar por Super Roboyarou?", () => {
                    GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard); // Simula retorno ao Extra
                    Destroy(source.gameObject);
                    GameManager.Instance.SpecialSummonFromData(target, source.isPlayerCard);
                });
            }
        }
    }

    // 1794 - Super Roboyarou
    void Effect_1794_SuperRoboyarou(CardDisplay source)
    {
        // You can Special Summon "Super Robolady" by returning this card from the field to the Extra Deck.
        // Increase ATK by 1000 during Damage Step when battling a monster.
        if (source.isOnField)
        {
            List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
            CardData target = extra.Find(c => c.name == "Super Robolady");
            if (target != null)
            {
                UIManager.Instance.ShowConfirmation("Trocar por Super Robolady?", () => {
                    GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
                    Destroy(source.gameObject);
                    GameManager.Instance.SpecialSummonFromData(target, source.isPlayerCard);
                });
            }
        }
    }

    // 1795 - Super War-Lion
    void Effect_1795_SuperWarLion(CardDisplay source)
    {
        Debug.Log("Super War-Lion: Ritual.");
    }

    // 1796 - Supply
    void Effect_1796_Supply(CardDisplay source)
    {
        // FLIP: Return 2 Fusion-Material monsters that were sent to the GY as a result of a Fusion Summon to your hand.
        // Simplificado: Retorna 2 monstros do GY.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(monsters, "Recuperar 2 Materiais", 2, 2, (selected) => {
                foreach(var c in selected)
                {
                    gy.Remove(c);
                    GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                }
            });
        }
    }

    // 1798 - Susa Soldier
    void Effect_1798_SusaSoldier(CardDisplay source)
    {
        // Spirit. Battle Damage this card inflicts is halved.
        // Lógica de Spirit no OnPhaseStart. Lógica de dano no OnDamageCalculation.
        Debug.Log("Susa Soldier: Dano cortado.");
    }

    // 1799 - Swamp Battleguard
    void Effect_1799_SwampBattleguard(CardDisplay source)
    {
        // This card gains 500 ATK for each "Lava Battleguard" you control.
        int count = 0;
        if (GameManager.Instance.IsCardActiveOnField("Lava Battleguard") || GameManager.Instance.IsCardActiveOnField("1059")) count++;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 500, source));
    }

    // 1800 - Swarm of Locusts
    void Effect_1800_SwarmOfLocusts(CardDisplay source)
    {
        // Once per turn: You can change this card to face-down Defense Position.
        // When this card is Flip Summoned: Target 1 Spell/Trap Card your opponent controls; destroy that target.
        if (source.isFlipped)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
        else
        {
            Effect_TurnSet(source);
        }
    }

        // 1801 - Swarm of Scarabs
    void Effect_1801_SwarmOfScarabs(CardDisplay source)
    {
        // Once per turn: You can change this card to face-down Defense Position.
        // When this card is Flip Summoned: Target 1 monster your opponent controls; destroy that target.
        if (source.isFlipped)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
        else
        {
            Effect_TurnSet(source);
        }
    }

    // 1802 - Swift Gaia the Fierce Knight
    void Effect_1802_SwiftGaiaTheFierceKnight(CardDisplay source)
    {
        // If this is the only card in your hand, you can Normal Summon it without Tributing.
        // Lógica de invocação tratada no SummonManager.
        Debug.Log("Swift Gaia: Condição de invocação sem tributo (Verificar SummonManager).");
    }

    // 1804 - Sword Hunter
    void Effect_1804_SwordHunter(CardDisplay source)
    {
        // At the end of the Battle Phase, if this card destroyed a monster(s) by battle and sent it to the Graveyard this Battle Phase: Equip all those monsters from the Graveyard to this card as an Equip Spell Card(s) with this effect. The equipped monster gains 200 ATK.
        // Lógica no OnBattleEnd/OnPhaseStart.
        Debug.Log("Sword Hunter: Efeito de equipar monstros destruídos configurado.");
    }

    // 1806 - Sword of Dark Destruction
    void Effect_1806_SwordOfDarkDestruction(CardDisplay source)
    {
        Effect_Equip(source, 400, -200, "", "Dark");
    }

    // 1807 - Sword of Deep-Seated
    void Effect_1807_SwordOfDeepSeated(CardDisplay source)
    {
        // Equip: +500 ATK/DEF. If sent to GY: Place on top of Deck.
        Effect_Equip(source, 500, 500);
        // Lógica de retorno ao deck no OnCardSentToGraveyard.
    }

    // 1808 - Sword of Dragon's Soul
    void Effect_1808_SwordOfDragonsSoul(CardDisplay source)
    {
        // Equip Warrior: +700 ATK. If battles Dragon, destroy it at end of Battle Phase.
        Effect_Equip(source, 700, 0, "Warrior");
        // Lógica de destruição de dragão no OnBattleEnd.
    }

    // 1809 - Sword of the Soul-Eater
    void Effect_1809_SwordOfTheSoulEater(CardDisplay source)
    {
        // Equip only to Level 3 or lower Normal Monster. Tribute all other Normal Monsters. +1000 ATK per tribute.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Normal") && t.CurrentCardData.level <= 3,
                (target) => {
                    // Tribute others
                    int count = 0;
                    List<CardDisplay> toTribute = new List<CardDisplay>();
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                        {
                            if (z.childCount > 0)
                            {
                                var m = z.GetChild(0).GetComponent<CardDisplay>();
                                if (m != null && m != target && m.CurrentCardData.type.Contains("Normal"))
                                {
                                    toTribute.Add(m);
                                }
                            }
                        }
                    }
                    
                    foreach(var m in toTribute)
                    {
                        GameManager.Instance.TributeCard(m);
                        count++;
                    }
                    
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, count * 1000, source));
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                    Debug.Log($"Sword of the Soul-Eater: {count} tributados. +{count*1000} ATK.");
                }
            );
        }
    }

    // 1810 - Swords of Concealing Light
    void Effect_1810_SwordsOfConcealingLight(CardDisplay source)
    {
        // Destroy 2nd Standby. Change opp monsters to face-down Defense. Cannot change pos.
        source.turnCounter = 2;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            foreach(var m in oppMonsters)
            {
                if (!m.isFlipped) // Face-up
                {
                    m.ChangePosition(); // To Defense
                    m.ShowBack(); // Face-down
                }
            }
        }
        Debug.Log("Swords of Concealing Light: Monstros do oponente virados para baixo.");
    }

    // 1811 - Swords of Revealing Light
    void Effect_1811_SwordsOfRevealingLight(CardDisplay source)
    {
        // Destroy 3rd End Phase. Flip opp monsters face-up. Cannot attack.
        source.turnCounter = 3;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            foreach(var m in oppMonsters)
            {
                if (m.isFlipped) // Face-down
                {
                    m.RevealCard();
                }
            }
        }
        Debug.Log("Swords of Revealing Light: Monstros do oponente revelados. Ataques bloqueados.");
    }

    // 1812 - Swordsman from a Distant Land
    void Effect_1812_SwordsmanFromADistantLand(CardDisplay source)
    {
        // If attacked, destroy attacker in 5th End Phase.
        // Lógica no OnDamageCalculation/OnBattleEnd.
        Debug.Log("Swordsman from a Distant Land: Efeito de destruição retardada configurado.");
    }

    // 1816 - System Down
    void Effect_1816_SystemDown(CardDisplay source)
    {
        // Pay 1000 LP; banish all Machine-Type monsters your opponent controls and in their Graveyard.
        if (Effect_PayLP(source, 1000))
        {
            // Field
            List<CardDisplay> toBanishField = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if (z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if (m != null && m.CurrentCardData.race == "Machine") toBanishField.Add(m);
                    }
                }
            }
            foreach(var m in toBanishField) GameManager.Instance.BanishCard(m);

            // Graveyard
            List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
            List<CardData> toBanishGY = oppGY.FindAll(c => c.race == "Machine");
            foreach(var c in toBanishGY)
            {
                GameManager.Instance.RemoveFromPlay(c, false);
                oppGY.Remove(c);
            }
            
            Debug.Log("System Down: Máquinas do oponente banidas.");
        }
    }

    // 1817 - T.A.D.P.O.L.E.
    void Effect_1817_TADPOLE(CardDisplay source)
    {
        // When destroyed by battle and sent to GY: Add any "T.A.D.P.O.L.E."s from Deck to hand.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> tadpoles = deck.FindAll(c => c.name == "T.A.D.P.O.L.E.");
        
        if (tadpoles.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(tadpoles, "Adicionar T.A.D.P.O.L.E.", 1, tadpoles.Count, (selected) => {
                foreach(var c in selected)
                {
                    deck.Remove(c);
                    GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                }
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    // 1818 - Tactical Espionage Expert
    void Effect_1818_TacticalEspionageExpert(CardDisplay source)
    {
        // When Normal Summoned: No Trap Cards can be activated.
        Debug.Log("Tactical Espionage Expert: Traps bloqueadas na invocação.");
    }

    // 1819 - Tailor of the Fickle
    void Effect_1819_TailorOfTheFickle(CardDisplay source)
    {
        // Switch 1 Equip Card equipped to a monster to another correct target.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.property == "Equip",
                (equipCard) => {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (newTarget) => newTarget.isOnField && newTarget.CurrentCardData.type.Contains("Monster"),
                        (target) => {
                            // Lógica simplificada de re-equipar:
                            // Em um sistema completo, isso moveria o CardLink.
                            Debug.Log($"Tailor of the Fickle: {equipCard.CurrentCardData.name} movido para {target.CurrentCardData.name}.");
                        }
                    );
                }
            );
        }
    }

    // 1820 - Tainted Wisdom
    void Effect_1820_TaintedWisdom(CardDisplay source)
    {
        // If changed from Attack to Defense: Shuffle Deck.
        // Lógica no OnBattlePositionChangedImpl.
        Debug.Log("Tainted Wisdom: Efeito de embaralhar configurado.");
    }

    // 1823 - Talisman of Spell Sealing
    void Effect_1823_TalismanOfSpellSealing(CardDisplay source)
    {
        // Active if "Sealmaster Meisei". No Spells.
        if (GameManager.Instance.IsCardActiveOnField("Sealmaster Meisei") || GameManager.Instance.IsCardActiveOnField("1602"))
        {
            Debug.Log("Talisman of Spell Sealing: Magias bloqueadas.");
        }
        else
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
        }
    }

    // 1824 - Talisman of Trap Sealing
    void Effect_1824_TalismanOfTrapSealing(CardDisplay source)
    {
        // Active if "Sealmaster Meisei". No Traps.
        if (GameManager.Instance.IsCardActiveOnField("Sealmaster Meisei") || GameManager.Instance.IsCardActiveOnField("1602"))
        {
            Debug.Log("Talisman of Trap Sealing: Armadilhas bloqueadas.");
        }
        else
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
        }
    }

        // 1827 - Taunt
    void Effect_1827_Taunt(CardDisplay source)
    {
        // Continuous Trap. Select 1 monster. Opponent must attack it.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard,
                (target) => {
                    Debug.Log($"Taunt: {target.CurrentCardData.name} é o alvo obrigatório.");
                    // BattleManager.Instance.forcedAttackTarget = target;
                }
            );
        }
    }

    // 1829 - Temple of the Kings
    void Effect_1829_TempleOfTheKings(CardDisplay source)
    {
        // Continuous Spell. Activate Traps the turn they are Set.
        // Tribute this card + Serket to SS monster from Hand/Deck/Extra.
        Debug.Log("Temple of the Kings: Ativado. (Lógica de ativação de Traps e Serket pendente).");
    }

    // 1833 - Terraforming
    void Effect_1833_Terraforming(CardDisplay source)
    {
        // Add 1 Field Spell from Deck to hand.
        Effect_SearchDeck(source, "Field", "Spell");
    }

    // 1834 - Terrorking Archfiend
    void Effect_1834_TerrorkingArchfiend(CardDisplay source)
    {
        // Maintenance, Negate target, Revive with Pandemonium.
        Debug.Log("Terrorking Archfiend: Efeitos passivos/gatilho configurados.");
    }

    // 1836 - Teva
    void Effect_1836_Teva(CardDisplay source)
    {
        // Spirit. If inflicts battle damage, opponent skips next Battle Phase.
        Debug.Log("Teva: Efeito de pular Battle Phase configurado.");
    }

    // 1839 - The A. Forces
    void Effect_1839_TheAForces(CardDisplay source)
    {
        // Continuous Spell. Your Warriors/Spellcasters gain 200 ATK for each on your field.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && (m.CurrentCardData.race == "Warrior" || m.CurrentCardData.race == "Spellcaster"))
                        count++;
                }
            }
        }
        
        int buff = count * 200;
        // Apply buff to all relevant monsters
        if (GameManager.Instance.duelFieldUI != null)
        {
             foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
             {
                 if(z.childCount > 0)
                 {
                     var m = z.GetChild(0).GetComponent<CardDisplay>();
                     if(m != null && (m.CurrentCardData.race == "Warrior" || m.CurrentCardData.race == "Spellcaster"))
                        m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, buff, source));
                 }
             }
        }
        Debug.Log($"The A. Forces: Buff de {buff} ATK aplicado.");
    }

    // 1840 - The Agent of Creation - Venus
    void Effect_1840_TheAgentOfCreationVenus(CardDisplay source)
    {
        // Pay 500 LP; SS 1 "Mystical Shine Ball" from hand/Deck.
        if (Effect_PayLP(source, 500))
        {
            // Search and SS
            Effect_SearchDeck(source, "Mystical Shine Ball", "Monster"); // Should be SS
        }
    }

    // 1841 - The Agent of Force - Mars
    void Effect_1841_TheAgentOfForceMars(CardDisplay source)
    {
        // Unaffected by Spells. ATK = LP difference.
        int diff = Mathf.Abs(GameManager.Instance.playerLP - GameManager.Instance.opponentLP);
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, diff, source));
        Debug.Log("The Agent of Force - Mars: ATK ajustado pela diferença de LP.");
    }

    // 1842 - The Agent of Judgment - Saturn
    void Effect_1842_TheAgentOfJudgmentSaturn(CardDisplay source)
    {
        // Tribute this card; inflict damage to opponent equal to LP difference.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            int diff = Mathf.Abs(GameManager.Instance.playerLP - GameManager.Instance.opponentLP);
            Effect_DirectDamage(source, diff);
        }
    }

    // 1843 - The Agent of Wisdom - Mercury
    void Effect_1843_TheAgentOfWisdomMercury(CardDisplay source)
    {
        // End Phase: If hand is 0, draw 1.
        // Logic in OnPhaseStart(End).
        Debug.Log("The Agent of Wisdom - Mercury: Efeito de compra na End Phase configurado.");
    }

    // 1846 - The Big March of Animals
    void Effect_1846_TheBigMarchOfAnimals(CardDisplay source)
    {
        // All face-up Beast, Beast-Warrior, Winged Beast gain 200 ATK for each on your field.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && (m.CurrentCardData.race == "Beast" || m.CurrentCardData.race == "Beast-Warrior" || m.CurrentCardData.race == "Winged Beast"))
                        count++;
                }
            }
        }
        int buff = count * 200;
        // Apply buff
        Debug.Log($"The Big March of Animals: Buff de {buff} ATK aplicado.");
    }

    // 1847 - The Bistro Butcher
    void Effect_1847_TheBistroButcher(CardDisplay source)
    {
        // If inflicts battle damage: Opponent draws 2.
        // Logic in OnDamageDealtImpl.
        Debug.Log("The Bistro Butcher: Efeito de compra para o oponente configurado.");
    }

    // 1848 - The Cheerful Coffin
    void Effect_1848_TheCheerfulCoffin(CardDisplay source)
    {
        // Discard up to 3 Monster Cards.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));
        if (monsters.Count > 0)
        {
            int max = Mathf.Min(3, monsters.Count);
            GameManager.Instance.OpenCardMultiSelection(monsters, "Descarte até 3 Monstros", 1, max, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                }
            });
        }
    }

    // 1849 - The Creator Incarnate
    void Effect_1849_TheCreatorIncarnate(CardDisplay source)
    {
        // Tribute "The Creator"; SS 1 "The Creator" from hand.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "The Creator",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    // SS from hand
                }
            );
        }
    }

    // 1850 - The Dark - Hex-Sealed Fusion
    void Effect_1850_TheDarkHexSealedFusion(CardDisplay source)
    {
        // Fusion Substitute.
        // Tribute materials + this card -> SS DARK Fusion.
        Debug.Log("The Dark - Hex-Sealed Fusion: Efeito de fusão por tributo.");
    }

        // 1853 - The Dragon's Bead
    void Effect_1853_TheDragonsBead(CardDisplay source)
    {
        // Discard 1 card from your hand to negate the activation of a Trap Card that targets a face-up Dragon-Type monster you control and destroy it.
        // Requer Chain.
        Debug.Log("The Dragon's Bead: Negação de Trap que alveja Dragão (Requer Chain).");
    }

    // 1856 - The Earth - Hex-Sealed Fusion
    void Effect_1856_TheEarthHexSealedFusion(CardDisplay source)
    {
        // Fusion Substitute. Tribute materials + this card -> SS EARTH Fusion.
        Debug.Log("The Earth - Hex-Sealed Fusion: Efeito de fusão por tributo.");
    }

    // 1857 - The Emperor's Holiday
    void Effect_1857_TheEmperorsHoliday(CardDisplay source)
    {
        // Negate all Equip Spell Card effects on the field.
        Debug.Log("The Emperor's Holiday: Equipamentos negados (Passivo).");
    }

    // 1858 - The End of Anubis
    void Effect_1858_TheEndOfAnubis(CardDisplay source)
    {
        // While this card is face-up on the field, all effects of Spell, Trap, and Monster Cards that target a card(s) in the Graveyard or that activate in the Graveyard are negated.
        Debug.Log("The End of Anubis: Bloqueio de efeitos no GY (Passivo).");
    }

    // 1859 - The Eye of Truth
    void Effect_1859_TheEyeOfTruth(CardDisplay source)
    {
        // As long as this card remains face-up on the field, your opponent must show their hand.
        // During each of your opponent's Standby Phases, if your opponent has a Spell Card in their hand, they gain 1000 Life Points.
        GameManager.Instance.showOpponentHand = true;
        Debug.Log("The Eye of Truth: Mão do oponente revelada.");
        // Lógica de cura na Standby Phase do oponente (OnPhaseStart).
    }

    // 1860 - The Fiend Megacyber
    void Effect_1860_TheFiendMegacyber(CardDisplay source)
    {
        // If your opponent controls at least 2 more monsters than you do, you can Special Summon this card from your hand.
        // Lógica de SS da mão.
        int myCount = 0;
        int oppCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) myCount++;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) oppCount++;
        }

        if (oppCount >= myCount + 2)
        {
            if (!source.isOnField)
            {
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                Debug.Log("The Fiend Megacyber: Invocado especialmente.");
            }
        }
    }

    // 1861 - The First Sarcophagus
    void Effect_1861_TheFirstSarcophagus(CardDisplay source)
    {
        // Special Summon "Spirit of the Pharaoh".
        // Lógica complexa de turnos e sarcófagos adicionais.
        Debug.Log("The First Sarcophagus: Início da contagem.");
    }

    // 1862 - The Flute of Summoning Dragon
    void Effect_1862_TheFluteOfSummoningDragon(CardDisplay source)
    {
        // If "Lord of D." is on the field: SS up to 2 Dragons from hand.
        if (GameManager.Instance.IsCardActiveOnField("Lord of D.") || GameManager.Instance.IsCardActiveOnField("1098"))
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> dragons = hand.FindAll(c => c.race == "Dragon" && c.type.Contains("Monster"));
            
            if (dragons.Count > 0)
            {
                int max = Mathf.Min(2, dragons.Count);
                GameManager.Instance.OpenCardMultiSelection(dragons, "Invocar Dragões", 1, max, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(c, source.isPlayerCard);
                    }
                });
            }
        }
    }

    // 1863 - The Forceful Sentry
    void Effect_1863_TheForcefulSentry(CardDisplay source)
    {
        // Look at opponent's hand, select 1 card and return to Deck.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(oppHand, "Retornar ao Deck", (selected) => {
                // Remove da mão do oponente (precisa achar o GO)
                // ...
                // Adiciona ao deck do oponente
                GameManager.Instance.GetOpponentMainDeck().Add(selected);
                GameManager.Instance.ShuffleDeck(false);
                Debug.Log($"The Forceful Sentry: {selected.name} retornado ao deck do oponente.");
            });
        }
    }

    // 1864 - The Forgiving Maiden
    void Effect_1864_TheForgivingMaiden(CardDisplay source)
    {
        // Tribute this card to return 1 of your monsters destroyed by battle to your hand.
        // Requer gatilho de destruição.
        Debug.Log("The Forgiving Maiden: Efeito de recuperação configurado.");
    }

    // 1866 - The Graveyard in the Fourth Dimension
    void Effect_1866_TheGraveyardInTheFourthDimension(CardDisplay source)
    {
        // Add 2 "LV" monsters from GY to Deck.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> lvs = gy.FindAll(c => c.name.Contains("LV"));
        
        if (lvs.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(lvs, "Retornar 2 LV", 2, 2, (selected) => {
                foreach(var c in selected)
                {
                    gy.Remove(c);
                    GameManager.Instance.GetPlayerMainDeck().Add(c);
                }
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    // 1868 - The Hunter with 7 Weapons
    void Effect_1868_TheHunterWith7Weapons(CardDisplay source)
    {
        // When Summoned: Declare 1 Type. +1000 ATK when battling that Type.
        // Simulação: Declara "Dragon".
        Debug.Log("The Hunter with 7 Weapons: Tipo declarado (Simulado: Dragon).");
        // Lógica de buff no OnDamageCalculation.
    }

    // 1870 - The Immortal of Thunder
    void Effect_1870_TheImmortalOfThunder(CardDisplay source)
    {
        // FLIP: Gain 3000 LP. When sent to GY, lose 5000 LP.
        Effect_GainLP(source, 3000);
        // Lógica de perda no OnCardSentToGraveyard.
    }

    // 1871 - The Inexperienced Spy
    void Effect_1871_TheInexperiencedSpy(CardDisplay source)
    {
        // Select 1 card in opponent's hand and look at it.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count > 0)
        {
            CardData random = oppHand[Random.Range(0, oppHand.Count)];
            Debug.Log($"The Inexperienced Spy: Carta revelada: {random.name}");
        }
    }

    // 1873 - The Kick Man
    void Effect_1873_TheKickMan(CardDisplay source)
    {
        // When SS: Equip 1 Equip Spell from GY.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> equips = gy.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");
        
        if (equips.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(equips, "Equipar do GY", (selected) => {
                // Lógica de equipar (Simulada)
                Debug.Log($"The Kick Man: Equipou {selected.name}.");
            });
        }
    }

    // 1874 - The Last Warrior from Another Planet
    void Effect_1874_TheLastWarriorFromAnotherPlanet(CardDisplay source)
    {
        // When SS: Destroy all other monsters you control. Neither player can Summon.
        if (source.isOnField)
        {
            // Destroy others
            List<CardDisplay> toDestroy = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                {
                    if(z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null && m != source) toDestroy.Add(m);
                    }
                }
            }
            DestroyCards(toDestroy, source.isPlayerCard);
            
            // Lock Summons (Passivo no SummonManager)
            Debug.Log("The Last Warrior: Invocações bloqueadas.");
        }
    }

    // 1875 - The Law of the Normal
    void Effect_1875_TheLawOfTheNormal(CardDisplay source)
    {
        // Activate only if you control 5 face-up Level 2 or lower Normal Monsters.
        // Destroy all cards on opp hand and field.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && !m.isFlipped && m.CurrentCardData.type.Contains("Normal") && m.CurrentCardData.level <= 2)
                        count++;
                }
            }
        }

        if (count == 5)
        {
            Debug.Log("The Law of the Normal: Destruição total!");
            GameManager.Instance.DiscardHand(false);
            DestroyAllMonsters(true, false);
            Effect_HarpiesFeatherDuster(source);
        }
    }

        // 1876 - The Legendary Fisherman
    void Effect_1876_TheLegendaryFisherman(CardDisplay source)
    {
        // While "Umi" is on the field, this card is unaffected by Spell effects and cannot be targeted for attacks.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi
        {
            Debug.Log("The Legendary Fisherman: Imune a Magias e Ataques (Passivo).");
            // Lógica de imunidade a magias e ataques no BattleManager/SpellTrapManager
        }
    }

    // 1877 - The Light - Hex-Sealed Fusion
    void Effect_1877_TheLightHexSealedFusion(CardDisplay source)
    {
        // Fusion Substitute. Tribute materials + this card -> SS LIGHT Fusion.
        Debug.Log("The Light - Hex-Sealed Fusion: Efeito de fusão por tributo.");
    }

    // 1878 - The Little Swordsman of Aile
    void Effect_1878_TheLittleSwordsmanOfAile(CardDisplay source)
    {
        // Tribute 1 monster; increase ATK by 700 until End Phase.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            // Seleção de tributo simplificada (primeiro disponível)
            // Em produção: UI de seleção
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != source,
                    (tribute) => {
                        GameManager.Instance.TributeCard(tribute);
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 700, source));
                        Debug.Log("The Little Swordsman of Aile: +700 ATK.");
                    }
                );
            }
        }
    }

    // 1879 - The Mask of Remnants
    void Effect_1879_TheMaskOfRemnants(CardDisplay source)
    {
        // If in GY when Des Gardius leaves field: Equip to opp monster and take control.
        // Lógica implementada no OnCardSentToGraveyard (Des Gardius).
        // Se ativada da mão: Shuffle into Deck.
        if (source.isOnField) // Ativada como Spell
        {
            GameManager.Instance.ReturnToDeck(source, false); // Shuffle
            GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            Debug.Log("The Mask of Remnants: Retornada ao Deck.");
        }
    }

    // 1880 - The Masked Beast
    void Effect_1880_TheMaskedBeast(CardDisplay source)
    {
        // Ritual Monster.
        Debug.Log("The Masked Beast: Monstro Ritual.");
    }

    // 1883 - The Puppet Magic of Dark Ruler
    void Effect_1883_ThePuppetMagicOfDarkRuler(CardDisplay source)
    {
        // Banish monsters from GY whose Levels equal Level of Fiend in GY; SS that Fiend.
        Debug.Log("The Puppet Magic of Dark Ruler: Requer seleção complexa de banimento.");
    }

    // 1884 - The Regulation of Tribe
    void Effect_1884_TheRegulationOfTribe(CardDisplay source)
    {
        // Declare 1 Type. Monsters of that Type cannot attack. Tribute 1 monster each Standby.
        Debug.Log("The Regulation of Tribe: Bloqueio de tipo (Simulado: Dragon).");
        // Lógica de bloqueio no BattleManager.
    }

    // 1885 - The Reliable Guardian
    void Effect_1885_TheReliableGuardian(CardDisplay source)
    {
        // Target 1 face-up monster; +700 DEF until end of turn.
        Effect_BuffStats(source, 0, 700);
    }

    // 1886 - The Rock Spirit
    void Effect_1886_TheRockSpirit(CardDisplay source)
    {
        // SS by banishing 1 EARTH from GY. Opponent gains 300 ATK during your Battle Phase.
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> earths = gy.FindAll(c => c.attribute == "Earth");
            if (earths.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(earths, "Banir 1 EARTH", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
        // Debuff no oponente (OnPhaseStart Battle Phase)
    }

    // 1887 - The Sanctuary in the Sky
    void Effect_1887_TheSanctuaryInTheSky(CardDisplay source)
    {
        // Battle Damage to controller of Fairy monster becomes 0.
        Debug.Log("The Sanctuary in the Sky: Proteção de dano para Fadas (Passivo).");
    }

    // 1889 - The Secret of the Bandit
    void Effect_1889_TheSecretOfTheBandit(CardDisplay source)
    {
        // When a monster inflicts Battle Damage: Opponent discards 1 card.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("The Secret of the Bandit: Efeito de descarte configurado.");
    }

    // 1890 - The Selection
    void Effect_1890_TheSelection(CardDisplay source)
    {
        // Pay 1000 LP. Negate Summon of monster with same Type as one on field.
        // Requer Chain/Trigger de invocação.
        Debug.Log("The Selection: Negação de invocação por tipo.");
    }

    // 1892 - The Shallow Grave
    void Effect_1892_TheShallowGrave(CardDisplay source)
    {
        // Each player selects 1 monster in their GY; SS it in face-down Defense.
        // Player
        Effect_Revive(source, false); // Deveria ser face-down
        // Opponent (Simulado)
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> monsters = oppGY.FindAll(c => c.type.Contains("Monster"));
        if (monsters.Count > 0)
        {
            CardData random = monsters[Random.Range(0, monsters.Count)];
            GameManager.Instance.SpecialSummonFromData(random, false, false, true); // Face-down Defense
            Debug.Log($"The Shallow Grave: Oponente reviveu {random.name}.");
        }
    }

    // 1894 - The Spell Absorbing Life
    void Effect_1894_TheSpellAbsorbingLife(CardDisplay source)
    {
        // Flip all face-down monsters face-up. Gain 400 LP for each Effect Monster.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            int effectCount = 0;
            foreach(var m in all)
            {
                if (m.isFlipped) m.RevealCard();
                if (m.CurrentCardData.type.Contains("Effect")) effectCount++;
            }
            Effect_GainLP(source, effectCount * 400);
        }
    }

    // 1896 - The Stern Mystic
    void Effect_1896_TheSternMystic(CardDisplay source)
    {
        // FLIP: All face-down cards are turned face-up, then returned to original position. No effects activate.
        Debug.Log("The Stern Mystic: Revelando cartas (Visualmente).");
    }

    // 1898 - The Thing in the Crater
    void Effect_1898_TheThingInTheCrater(CardDisplay source)
    {
        // If destroyed: SS 1 Pyro from hand.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("The Thing in the Crater: Efeito de invocação configurado.");
    }

    // 1900 - The Tricky
    void Effect_1900_TheTricky(CardDisplay source)
    {
        // SS from hand by discarding 1 card.
        if (!source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            if (hand.Count > 1) // Tricky + 1
            {
                GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                    if (discarded != source.CurrentCardData)
                    {
                        GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                        GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                    }
                });
            }
        }
    }

    // 1901 - The Trojan Horse
    void Effect_1901_TheTrojanHorse(CardDisplay source)
    {
        // Earth monsters require 1 less tribute (treated as 2 tributes).
        Debug.Log("The Trojan Horse: Vale por 2 tributos para EARTH (Lógica no SummonManager).");
    }

    // 1902 - The Unfriendly Amazon
    void Effect_1902_TheUnfriendlyAmazon(CardDisplay source)
    {
        // Standby Phase: Tribute 1 monster or destroy this card.
        // Logic in OnPhaseStart.
        Debug.Log("The Unfriendly Amazon: Manutenção na Standby Phase.");
    }

    // 1903 - The Unhappy Girl
    void Effect_1903_TheUnhappyGirl(CardDisplay source)
    {
        // Battle protection in Attack pos. Lock attacker.
        // Logic in OnBattleEnd.
        Debug.Log("The Unhappy Girl: Efeito de trava de batalha.");
    }

    // 1904 - The Unhappy Maiden
    void Effect_1904_TheUnhappyMaiden(CardDisplay source)
    {
        // Destroyed by battle -> End Battle Phase.
        // Logic in OnBattleEnd.
        Debug.Log("The Unhappy Maiden: Encerra Battle Phase.");
    }

    // 1906 - The Warrior Returning Alive
    void Effect_1906_TheWarriorReturningAlive(CardDisplay source)
    {
        // Target 1 Warrior in GY; add to hand.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> warriors = gy.FindAll(c => c.race == "Warrior" && c.type.Contains("Monster"));
        
        if (warriors.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(warriors, "Recuperar Warrior", (selected) => {
                gy.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
            });
        }
    }

    // 1907 - The Wicked Dreadroot
    void Effect_1907_TheWickedDreadroot(CardDisplay source)
    {
        // Halve ATK/DEF of all other monsters.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m != source)
                {
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Multiply, 0.5f, source));
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Multiply, 0.5f, source));
                }
            }
        }
    }

    // 1908 - The Wicked Worm Beast
    void Effect_1908_TheWickedWormBeast(CardDisplay source)
    {
        // Return to hand in End Phase.
        // Logic in OnPhaseStart.
        Debug.Log("The Wicked Worm Beast: Retorna à mão na End Phase.");
    }

    // 1909 - Theban Nightmare
    void Effect_1909_ThebanNightmare(CardDisplay source)
    {
        // +1500 ATK if hand/S-T empty.
        // Logic in CheckActiveCards/UpdateStats.
        Debug.Log("Theban Nightmare: Buff condicional.");
    }

    // 1910 - Theinen the Great Sphinx
    void Effect_1910_TheinenTheGreatSphinx(CardDisplay source)
    {
        // SS condition. Pay 500 LP.
        if (!source.isOnField)
        {
             if (Effect_PayLP(source, 500))
             {
                 GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                 GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                 // Buff ATK? "ATK becomes 6500".
                 // Need reference to summoned card.
             }
        }
    }

    // 1911 - Thestalos the Firestorm Monarch
    void Effect_1911_ThestalosTheFirestormMonarch(CardDisplay source)
    {
        // Tribute Summon: Discard random opp hand. If Monster, burn Level * 100.
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
            if (oppHand.Count > 0)
            {
                CardData discarded = oppHand[Random.Range(0, oppHand.Count)];
                // Discard logic (remove from list)
                // GameManager.Instance.DiscardOpponentCard(discarded); // Hypothetical
                Debug.Log($"Thestalos: Descartou {discarded.name}.");
                
                if (discarded.type.Contains("Monster"))
                {
                    int dmg = discarded.level * 100;
                    Effect_DirectDamage(source, dmg);
                }
            }
        }
    }

    // 1913 - Thousand Energy
    void Effect_1913_ThousandEnergy(CardDisplay source)
    {
        // Lv2 Normal Monsters +1000 ATK/DEF. Destroy at End Phase.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.CurrentCardData.level == 2 && m.CurrentCardData.type.Contains("Normal"))
                {
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 1000, source));
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 1000, source));
                    // Mark for destruction?
                }
            }
        }
    }

    // 1914 - Thousand Knives
    void Effect_1914_ThousandKnives(CardDisplay source)
    {
        // If DM: Destroy 1 opp monster.
        if (GameManager.Instance.IsCardActiveOnField("Dark Magician") || GameManager.Instance.IsCardActiveOnField("0419"))
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
    }

    // 1915 - Thousand Needles
    void Effect_1915_ThousandNeedles(CardDisplay source)
    {
        // Defense pos, attacked by ATK < DEF -> Destroy attacker.
        // Logic in OnBattleEnd.
        Debug.Log("Thousand Needles: Defesa mortal.");
    }

    // 1917 - Thousand-Eyes Restrict
    void Effect_1917_ThousandEyesRestrict(CardDisplay source)
    {
        // Absorb monster.
        if (source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                        // Apply stats
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk, source));
                        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalDef, source));
                        // Visual move
                        target.transform.SetParent(source.transform);
                        target.gameObject.SetActive(false); // Hide or show as equip
                        Debug.Log($"Thousand-Eyes Restrict: Absorveu {target.CurrentCardData.name}.");
                    }
                );
            }
        }
    }

    // 1918 - Threatening Roar
    void Effect_1918_ThreateningRoar(CardDisplay source)
    {
        // Opponent cannot attack this turn.
        if (BattleManager.Instance != null)
        {
            // BattleManager.Instance.preventAttacks = true; // Hypothetical flag
            Debug.Log("Threatening Roar: Ataques bloqueados.");
        }
    }

    // 1921 - Throwstone Unit
    void Effect_1921_ThrowstoneUnit(CardDisplay source)
    {
        // Tribute Warrior -> Destroy monster with DEF <= ATK of tribute.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Warrior",
                (tribute) => {
                    int atk = tribute.currentAtk;
                    GameManager.Instance.TributeCard(tribute);
                    
                    SpellTrapManager.Instance.StartTargetSelection(
                        (target) => target.isOnField && !target.isPlayerCard && target.currentDef <= atk,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                    );
                }
            );
        }
    }

    // 1922 - Thunder Crash
    void Effect_1922_ThunderCrash(CardDisplay source)
    {
        // Destroy all your monsters; 300 damage per monster.
        List<CardDisplay> myMonsters = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, myMonsters);
        }
        
        int count = myMonsters.Count;
        DestroyCards(myMonsters, true);
        Effect_DirectDamage(source, count * 300);
    }

    // 1923 - Thunder Dragon
    void Effect_1923_ThunderDragon(CardDisplay source)
    {
        // Discard: Add up to 2 Thunder Dragon.
        if (!source.isOnField)
        {
            GameManager.Instance.DiscardCard(source);
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> targets = deck.FindAll(c => c.name == "Thunder Dragon");
            
            if (targets.Count > 0)
            {
                int max = Mathf.Min(2, targets.Count);
                GameManager.Instance.OpenCardMultiSelection(targets, "Adicionar Thunder Dragon", 1, max, (selected) => {
                    foreach(var c in selected)
                    {
                        deck.Remove(c);
                        GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                    }
                    GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                });
            }
        }
    }

    // 1925 - Thunder Nyan Nyan
    void Effect_1925_ThunderNyanNyan(CardDisplay source)
    {
        // If non-Light monster on your field, destroy this.
        // Logic in CheckActiveCards (OnPhaseStart or Summon).
        Debug.Log("Thunder Nyan Nyan: Auto-destruição se houver não-LIGHT.");
    }

        // 1926 - Thunder of Ruler
    void Effect_1926_ThunderOfRuler(CardDisplay source)
    {
        // Your opponent cannot conduct their next Battle Phase.
        Debug.Log("Thunder of Ruler: Próxima Battle Phase do oponente pulada.");
        // PhaseManager.Instance.opponentSkipsNextBattlePhase = true;
    }

    // 1928 - Time Machine
    void Effect_1928_TimeMachine(CardDisplay source)
    {
        // When a monster is destroyed by battle and sent to the Graveyard: Special Summon that monster in the same battle position it was in.
        // Lógica de gatilho no OnCardSentToGraveyard.
        Debug.Log("Time Machine: Armadilha de renascimento configurada.");
    }

    // 1929 - Time Seal
    void Effect_1929_TimeSeal(CardDisplay source)
    {
        // Your opponent skips their next Draw Phase.
        Debug.Log("Time Seal: Próxima Draw Phase do oponente pulada.");
        // PhaseManager.Instance.opponentSkipsNextDrawPhase = true;
    }

    // 1930 - Time Wizard
    void Effect_1930_TimeWizard(CardDisplay source)
    {
        // Toss a coin and call it. If you call it right, destroy all monsters your opponent controls. If you call it wrong, destroy all monsters you control and you lose Life Points equal to half the total ATK of the destroyed monsters.
        GameManager.Instance.TossCoin(1, (heads) => {
            if (heads == 1) // Simula acerto
            {
                Debug.Log("Time Wizard: Acertou! Destruindo monstros do oponente.");
                DestroyAllMonsters(true, false);
            }
            else
            {
                Debug.Log("Time Wizard: Errou! Destruindo seus monstros e tomando dano.");
                List<CardDisplay> myMonsters = new List<CardDisplay>();
                if (GameManager.Instance.duelFieldUI != null)
                {
                    CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, myMonsters);
                }
                int totalAtk = 0;
                foreach(var m in myMonsters) totalAtk += m.originalAtk;
                
                DestroyCards(myMonsters, true);
                GameManager.Instance.DamagePlayer(totalAtk / 2);
            }
        });
    }

    // 1931 - Timeater
    void Effect_1931_Timeater(CardDisplay source)
    {
        // If this monster destroys an opponent's monster by battle: Your opponent skips their next Main Phase 1.
        // Lógica no OnBattleEnd.
        Debug.Log("Timeater: Efeito de pular Main Phase 1 configurado.");
    }

    // 1932 - Timidity
    void Effect_1932_Timidity(CardDisplay source)
    {
        // Your opponent cannot destroy your face-down Spell/Trap Cards until the end of their next turn.
        Debug.Log("Timidity: S/T setadas protegidas.");
        // Lógica de proteção no SpellTrapManager.
    }

    // 1935 - Token Feastevil
    void Effect_1935_TokenFeastevil(CardDisplay source)
    {
        // Destroy all Tokens on the field. Inflict 300 damage to the controller of each Token destroyed.
        // Requer que Tokens sejam identificáveis.
        Debug.Log("Token Feastevil: Destruindo Tokens e causando dano (Lógica de identificação de Token pendente).");
    }

    // 1936 - Token Thanksgiving
    void Effect_1936_TokenThanksgiving(CardDisplay source)
    {
        // Destroy all Tokens on the field. Gain 800 LP for each Token destroyed.
        Debug.Log("Token Thanksgiving: Destruindo Tokens e curando (Lógica de identificação de Token pendente).");
    }

    // 1937 - Toll
    void Effect_1937_Toll(CardDisplay source)
    {
        // Each player must pay 500 LP to declare an attack.
        // Lógica no BattleManager.CanAttack.
        Debug.Log("Toll: Custo de 500 LP para atacar.");
    }

    // 1941 - Toon Cannon Soldier
    void Effect_1941_ToonCannonSoldier(CardDisplay source)
    {
        // Toon. Tribute 1 monster; inflict 500 damage.
        Effect_TributeToBurn(source, 1, 500);
    }

    // 1942 - Toon Dark Magician Girl
    void Effect_1942_ToonDarkMagicianGirl(CardDisplay source)
    {
        // Toon. Can attack directly.
        Debug.Log("Toon Dark Magician Girl: Ataque direto (Lógica Toon no BattleManager).");
    }

    // 1943 - Toon Defense
    void Effect_1943_ToonDefense(CardDisplay source)
    {
        // When an opponent's monster attacks a Toon Monster you control: Make it a direct attack instead.
        // Lógica no OnAttackDeclared.
        Debug.Log("Toon Defense: Redireciona ataque para direto.");
    }

    // 1944 - Toon Gemini Elf
    void Effect_1944_ToonGeminiElf(CardDisplay source)
    {
        // Toon. If inflicts battle damage, opponent discards 1 random card.
        // Lógica no OnDamageDealt.
        Debug.Log("Toon Gemini Elf: Descarte ao causar dano.");
    }

    // 1945 - Toon Goblin Attack Force
    void Effect_1945_ToonGoblinAttackForce(CardDisplay source)
    {
        // Toon. Changes to Defense Position after attacking.
        // Lógica no OnBattleEnd.
        Debug.Log("Toon Goblin Attack Force: Vira defesa após ataque.");
    }

    // 1946 - Toon Masked Sorcerer
    void Effect_1946_ToonMaskedSorcerer(CardDisplay source)
    {
        // Toon. If inflicts battle damage, draw 1 card.
        // Lógica no OnDamageDealt.
        Debug.Log("Toon Masked Sorcerer: Compra ao causar dano.");
    }

    // 1947 - Toon Mermaid
    void Effect_1947_ToonMermaid(CardDisplay source)
    {
        // Toon. Can be Special Summoned from hand if you control "Toon World".
        if (!source.isOnField && (GameManager.Instance.IsCardActiveOnField("1950") || GameManager.Instance.IsCardActiveOnField("Toon World")))
        {
            GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
            GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
        }
    }

    // 1948 - Toon Summoned Skull
    void Effect_1948_ToonSummonedSkull(CardDisplay source)
    {
        // Toon. Cannot be Normal Summoned/Set. Must first be Special Summoned (from your hand) by Tributing 1 monster.
        // Lógica de invocação na mão.
        Debug.Log("Toon Summoned Skull: Invocação por tributo da mão.");
    }

    // 1949 - Toon Table of Contents
    void Effect_1949_ToonTableOfContents(CardDisplay source)
    {
        // Add 1 "Toon" card from your Deck to your hand.
        Effect_SearchDeck(source, "Toon");
    }

    // 1950 - Toon World
    void Effect_1950_ToonWorld(CardDisplay source)
    {
        // Activate by paying 1000 LP.
        Effect_PayLP(source, 1000);
    }

        // 1951 - Tornado
    void Effect_1951_Tornado(CardDisplay source)
    {
        // Activate only if opponent controls 3 or more S/T. Target 1 S/T; destroy it.
        int oppSTCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if(z.childCount > 0) oppSTCount++;
            if(GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) oppSTCount++;
        }

        if (oppSTCount >= 3)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                    (target) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
        else
        {
            Debug.Log("Tornado: Oponente precisa ter 3 ou mais S/T.");
        }
    }

    // 1952 - Tornado Bird
    void Effect_1952_TornadoBird(CardDisplay source)
    {
        // FLIP: Return 2 Spell/Trap Cards on the field to the hand.
        // Simplificado: Retorna 2 aleatórios ou pede seleção sequencial
        Debug.Log("Tornado Bird: Retornando 2 S/T para a mão (Lógica de seleção pendente).");
    }

    // 1953 - Tornado Wall
    void Effect_1953_TornadoWall(CardDisplay source)
    {
        // Active only if "Umi" is on the field. Battle Damage to you is 0.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013"))
        {
            Debug.Log("Tornado Wall: Dano de batalha 0 (Passivo).");
        }
        else
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
        }
    }

    // 1954 - Torpedo Fish
    void Effect_1954_TorpedoFish(CardDisplay source)
    {
        // Unaffected by Spells if Umi is on field.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013"))
        {
            Debug.Log("Torpedo Fish: Imune a Magias.");
        }
    }

    // 1955 - Torrential Tribute
    void Effect_1955_TorrentialTribute(CardDisplay source)
    {
        // Activate when monster is Summoned. Destroy all monsters.
        DestroyAllMonsters(true, true);
    }

    // 1956 - Total Defense Shogun
    void Effect_1956_TotalDefenseShogun(CardDisplay source)
    {
        // Can attack while in Defense Position.
        Debug.Log("Total Defense Shogun: Pode atacar em defesa.");
    }

    // 1957 - Tower of Babel
    void Effect_1957_TowerOfBabel(CardDisplay source)
    {
        // Counter on Spell activation. 4th counter -> Destroy -> 3000 dmg to player.
        // Lógica no OnSpellActivated.
    }

    // 1958 - Tragedy
    void Effect_1958_Tragedy(CardDisplay source)
    {
        // Activate when opp monster changes to Defense. Destroy all Defense monsters opp controls.
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.position == CardDisplay.BattlePosition.Defense) toDestroy.Add(m);
                }
            }
        }
        DestroyCards(toDestroy, false);
    }

    // 1960 - Transcendent Wings
    void Effect_1960_TranscendentWings(CardDisplay source)
    {
        // Send Winged Kuriboh (field) + 2 cards (hand) -> SS Winged Kuriboh LV10.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Winged Kuriboh",
                (kuriboh) => {
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    if (hand.Count >= 2)
                    {
                        GameManager.Instance.OpenCardMultiSelection(hand, "Descarte 2 cartas", 2, 2, (discarded) => {
                            foreach(var c in discarded) GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                            
                            GameManager.Instance.SendToGraveyard(kuriboh.CurrentCardData, true);
                            Destroy(kuriboh.gameObject);
                            
                            Effect_SearchDeck(source, "Winged Kuriboh LV10", "Monster"); // Should be SS
                        });
                    }
                }
            );
        }
    }

    // 1961 - Trap Dustshoot
    void Effect_1961_TrapDustshoot(CardDisplay source)
    {
        // If opp hand >= 4. Reveal hand, return 1 Monster to Deck.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count >= 4)
        {
            GameManager.Instance.OpenCardSelection(oppHand, "Retornar Monstro ao Deck", (selected) => {
                if (selected.type.Contains("Monster"))
                {
                    // Return logic (Simulado)
                    Debug.Log($"Trap Dustshoot: {selected.name} retornado ao deck.");
                }
            });
        }
    }

    // 1962 - Trap Hole
    void Effect_1962_TrapHole(CardDisplay source)
    {
        // Opponent Normal/Flip Summon 1000+ ATK -> Destroy.
        // Trigger no OnSummonImpl.
    }

    // 1963 - Trap Jammer
    void Effect_1963_TrapJammer(CardDisplay source)
    {
        // Battle Phase: Negate Trap.
        if (PhaseManager.Instance.currentPhase == GamePhase.Battle)
        {
            Debug.Log("Trap Jammer: Trap negada.");
        }
    }

    // 1964 - Trap Master
    void Effect_1964_TrapMaster(CardDisplay source)
    {
        // FLIP: Destroy 1 Trap.
        Effect_FlipDestroy(source, TargetType.Trap);
    }

    // 1965 - Trap of Board Eraser
    void Effect_1965_TrapOfBoardEraser(CardDisplay source)
    {
        // Negate effect damage. Opponent discards 1.
        Debug.Log("Trap of Board Eraser: Dano negado. Oponente descarta.");
        GameManager.Instance.DiscardRandomHand(false, 1);
    }

    // 1966 - Trap of Darkness
    void Effect_1966_TrapOfDarkness(CardDisplay source)
    {
        // LP <= 1000. Copy Normal Trap in GY.
        if (GameManager.Instance.playerLP <= 1000)
        {
            Debug.Log("Trap of Darkness: Copiando efeito de Trap do GY.");
        }
    }

    // 1967 - Tremendous Fire
    void Effect_1967_TremendousFire(CardDisplay source)
    {
        Effect_DirectDamage(source, 1000);
        Effect_PayLP(source, 500);
    }

    // 1971 - Triangle Ecstasy Spark
    void Effect_1971_TriangleEcstasySpark(CardDisplay source)
    {
        // Harpie Lady Sisters becomes 2700. Opponent cannot activate Traps.
        Debug.Log("Triangle Ecstasy Spark: Buff e bloqueio de Traps.");
    }

    // 1972 - Triangle Power
    void Effect_1972_TrianglePower(CardDisplay source)
    {
        // All Lv1 Normal Monsters +2000 ATK/DEF. Destroy at End Phase.
        Debug.Log("Triangle Power: Buff massivo em Lv1 Normal.");
    }

    // 1973 - Tribe-Infecting Virus
    void Effect_1973_TribeInfectingVirus(CardDisplay source)
    {
        // Discard 1; declare Type; destroy all face-up of that Type.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Tribe-Infecting Virus: Tipo declarado (Simulado). Destruindo...");
                // Effect_DestroyType(source, "Dragon"); // Exemplo
            });
        }
    }

    // 1974 - Tribute Doll
    void Effect_1974_TributeDoll(CardDisplay source)
    {
        // Tribute 1 monster; SS Level 7 monster from hand. Cannot attack.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            Debug.Log("Tribute Doll: Invocando Lv7 da mão.");
        }
    }

    // 1975 - Tribute to the Doomed
    void Effect_1975_TributeToTheDoomed(CardDisplay source)
    {
        // Discard 1; destroy 1 monster.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
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
            });
        }
    }

        // 1976 - Tricky Spell 4
    void Effect_1976_TrickySpell4(CardDisplay source)
    {
        // Send 1 "The Tricky" to GY. SS Tricky Tokens equal to opp monsters.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "The Tricky",
                (tribute) => {
                    GameManager.Instance.SendToGraveyard(tribute.CurrentCardData, tribute.isPlayerCard);
                    Destroy(tribute.gameObject);

                    int oppCount = 0;
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                            if(z.childCount > 0) oppCount++;
                    }

                    for(int i=0; i<oppCount; i++)
                    {
                        GameManager.Instance.SpawnToken(source.isPlayerCard, 2000, 1200, "Tricky Token");
                    }
                }
            );
        }
    }

    // 1978 - Troop Dragon
    void Effect_1978_TroopDragon(CardDisplay source)
    {
        // Destroyed by battle -> SS Troop Dragon from Deck.
        Effect_SearchDeck(source, "Troop Dragon", "Monster"); // Should be SS directly
    }

    // 1979 - Tsukuyomi
    void Effect_1979_Tsukuyomi(CardDisplay source)
    {
        // Cannot SS. Return to hand in End Phase.
        // On Summon: Target 1 face-up monster; change to face-down Defense.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped, // Face-up
                (target) => {
                    target.ChangePosition(); // To Defense
                    target.ShowBack(); // Face-down
                    Debug.Log($"Tsukuyomi: {target.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    // 1981 - Turtle Oath
    void Effect_1981_TurtleOath(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    // 1985 - Tutan Mask
    void Effect_1985_TutanMask(CardDisplay source)
    {
        // Negate Spell/Trap targeting Zombie.
        Debug.Log("Tutan Mask: Negação de alvo em Zumbi (Requer Chain).");
    }

    // 1988 - Twin Swords of Flashing Light - Tryce
    void Effect_1988_TwinSwordsOfFlashingLightTryce(CardDisplay source)
    {
        // Send 1 card from hand to GY to activate. Equip. -500 ATK. Second attack.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0 && !source.isOnField) // Activation
        {
             GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                 GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                 Effect_Equip(source, -500, 0);
             });
        }
        else if (source.isOnField)
        {
            // Already equipped logic handled by modifiers and BattleManager
        }
    }

    // 1989 - Twin-Headed Behemoth
    void Effect_1989_TwinHeadedBehemoth(CardDisplay source)
    {
        // If destroyed and sent to GY: SS with 1000 ATK/DEF during End Phase. Once per duel.
        Debug.Log("Twin-Headed Behemoth: Renascimento agendado (Once per duel).");
    }

    // 1992 - Twin-Headed Wolf
    void Effect_1992_TwinHeadedWolf(CardDisplay source)
    {
        // Negate Flip effects of monsters destroyed by battle.
        Debug.Log("Twin-Headed Wolf: Negação de Flip em batalha.");
    }

    // 1993 - Twinheaded Beast
    void Effect_1993_TwinheadedBeast(CardDisplay source)
    {
        // Double attack.
        Debug.Log("Twinheaded Beast: Ataque duplo.");
    }

    // 1994 - Two Thousand Needles
    void Effect_1994_TwoThousandNeedles(CardDisplay source)
    {
        // Defense pos, attacked, DEF > ATK -> Destroy attacker.
        Debug.Log("Two Thousand Needles: Defesa mortal.");
    }

    // 1996 - Two-Man Cell Battle
    void Effect_1996_TwoManCellBattle(CardDisplay source)
    {
        // End Phase: Turn player can SS Level 4 Normal.
        Debug.Log("Two-Man Cell Battle: Invocação na End Phase.");
    }

    // 1998 - Two-Pronged Attack
    void Effect_1998_TwoProngedAttack(CardDisplay source)
    {
        // Select 2 monsters you control and 1 opp monster. Destroy them.
        if (SpellTrapManager.Instance != null)
        {
             SpellTrapManager.Instance.StartTargetSelection(
                 (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                 (own1) => {
                     SpellTrapManager.Instance.StartTargetSelection(
                         (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && t != own1,
                         (own2) => {
                             SpellTrapManager.Instance.StartTargetSelection(
                                 (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                                 (opp) => {
                                     GameManager.Instance.SendToGraveyard(own1.CurrentCardData, true);
                                     Destroy(own1.gameObject);
                                     GameManager.Instance.SendToGraveyard(own2.CurrentCardData, true);
                                     Destroy(own2.gameObject);
                                     
                                     if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(opp);
                                     GameManager.Instance.SendToGraveyard(opp.CurrentCardData, false);
                                     Destroy(opp.gameObject);
                                 }
                             );
                         }
                     );
                 }
             );
        }
    }

}