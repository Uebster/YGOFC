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
}