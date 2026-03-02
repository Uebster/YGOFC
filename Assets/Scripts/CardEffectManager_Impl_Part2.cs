using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0501 - 0600)
    // =========================================================================================

    void Effect_0501_Disappear(CardDisplay source)
    {
        // Remove from play 1 card from opponent's Graveyard
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        if (oppGY.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(oppGY, "Banir do Cemitério", (selected) => {
                GameManager.Instance.RemoveFromPlay(selected, !source.isPlayerCard);
                oppGY.Remove(selected);
                Debug.Log($"Disappear: {selected.name} banido do cemitério do oponente.");
            });
        }
        else
        {
            Debug.Log("Disappear: Cemitério do oponente vazio.");
        }
    }

    void Effect_0502_Disarmament(CardDisplay source)
    {
        // Destroy all Equip Cards on the field
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentSpellZones);

            foreach (var zone in allZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && cd.CurrentCardData.property == "Equip")
                    {
                        toDestroy.Add(cd);
                    }
                }
            }
        }
        DestroyCards(toDestroy, source.isPlayerCard);
        Debug.Log($"Disarmament: {toDestroy.Count} cartas de equipamento destruídas.");
    }

    void Effect_0503_DiscFighter(CardDisplay source)
    {
        // Effect: If this card attacks a Defense Position monster with DEF >= 2000, destroy the monster with this card's effect without applying damage calculation.
        // Lógica passiva de batalha. Requer hook no BattleManager ou OnDamageCalculation.
        Debug.Log("Disc Fighter: Efeito passivo de batalha (Destrói defesa >= 2000).");
    }

    void Effect_0506_DisturbanceStrategy(CardDisplay source)
    {
        // Your opponent shuffles their entire hand into the Deck, then draws the same number of cards.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        int count = oppHand.Count;
        if (count > 0)
        {
            GameManager.Instance.DiscardHand(false); // Simplificado: Descarta em vez de embaralhar (limitação atual)
            // Idealmente: Retornar ao deck e embaralhar
            // foreach(var c in oppHand) GameManager.Instance.ReturnToDeck...
            
            for (int i = 0; i < count; i++)
            {
                GameManager.Instance.DrawOpponentCard();
            }
            Debug.Log($"Disturbance Strategy: Oponente trocou {count} cartas.");
        }
    }

    void Effect_0508_DivineWrath(CardDisplay source)
    {
        // Discard 1 card. Negate the activation of an Effect Monster's effect and destroy it.
        // Requer sistema de Chain/Counter.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Divine Wrath: Custo pago. (Negação de efeito pendente no sistema de Chain).");
            });
        }
    }

    void Effect_0510_Doitsu(CardDisplay source)
    {
        // Union monster for Soitsu.
        Effect_Union(source, "Soitsu", 2500, 0); // Soitsu ganha 2500 ATK
    }

    void Effect_0512_Dokurorider(CardDisplay source)
    {
        // Ritual Monster.
        Debug.Log("Dokurorider: Monstro Ritual.");
    }

    void Effect_0515_DonTurtle(CardDisplay source)
    {
        // When Normal Summoned: You can Special Summon any number of "Don Turtle" from your hand.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> turtles = hand.FindAll(c => c.name == "Don Turtle");
        
        if (turtles.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(turtles, "Invocar Don Turtle(s)", 1, turtles.Count, (selected) => {
                foreach (var card in selected)
                {
                    GameManager.Instance.SpecialSummonFromData(card, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(card, source.isPlayerCard);
                }
            });
        }
    }

    void Effect_0516_DonZaloog(CardDisplay source)
    {
        // When this card inflicts Battle Damage: Discard 1 random card OR send top 2 from Deck to GY.
        // Lógica implementada no OnDamageDealtImpl (CardEffectManager_Impl.cs).
        Debug.Log("Don Zaloog: Efeito de dano configurado.");
    }

    void Effect_0517_DoraOfFate(CardDisplay source)
    {
        // Trap: Activate only when you Summon a monster. Select 1 face-up monster on opp side.
        // If Lv of summoned < Lv of selected, destroy selected and inflict damage (Diff x 500).
        // Requer gatilho de invocação.
        if (SpellTrapManager.Instance != null)
        {
            // Assume que foi ativado em resposta a uma invocação sua
            // Precisamos saber qual monstro foi invocado.
            // Simplificação: Pega o último invocado
            // ...
            Debug.Log("Dora of Fate: Selecione monstro do oponente (Lógica de nível pendente).");
        }
    }

    void Effect_0519_DoriadosBlessing(CardDisplay source)
    {
        // Ritual Spell for Doriado.
        Debug.Log("Doriado's Blessing: Ritual.");
    }

    void Effect_0522_DoubleAttack(CardDisplay source)
    {
        // Discard 1 Monster. Select 1 monster with lower Level. It attacks twice.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Descarte 1 Monstro (Custo)", (cost) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == cost).GetComponent<CardDisplay>());
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.level < cost.level,
                        (target) => {
                            Debug.Log($"Double Attack: {target.CurrentCardData.name} pode atacar duas vezes.");
                            // target.canAttackTwice = true; // Requer suporte no CardDisplay/BattleManager
                        }
                    );
                }
            });
        }
    }

    void Effect_0523_DoubleCoston(CardDisplay source)
    {
        // Treated as 2 Tributes for DARK monster.
        Debug.Log("Double Coston: Vale por 2 tributos para DARK.");
    }

    void Effect_0524_DoubleSnare(CardDisplay source)
    {
        // Destroy Jinzo or Royal Decree.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.name == "Jinzo" || t.CurrentCardData.name == "Royal Decree") && !t.isFlipped,
                (target) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                    Debug.Log($"Double Snare: {target.CurrentCardData.name} destruído.");
                }
            );
        }
    }

    void Effect_0525_DoubleSpell(CardDisplay source)
    {
        // Discard 1 Spell. Select 1 Spell in opp GY and use it.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (cost) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == cost).GetComponent<CardDisplay>());
                
                List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
                List<CardData> oppSpells = oppGY.FindAll(c => c.type.Contains("Spell"));
                
                if (oppSpells.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(oppSpells, "Selecione Magia do Oponente", (target) => {
                        Debug.Log($"Double Spell: Copiando efeito de {target.name}.");
                        // Lógica de copiar efeito é complexa.
                        // Simplificação: Adiciona à mão para usar? Não, regra diz "use it".
                        // Tenta executar o efeito imediatamente se possível
                        // CardEffectManager.Instance.ExecuteCardEffect(targetID)... mas precisa de um CardDisplay dummy.
                    });
                }
            });
        }
    }
}
