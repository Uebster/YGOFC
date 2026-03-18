using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        bool hasEquip = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentSpellZones);
            foreach (var z in allZones) if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.property == "Equip") hasEquip = true;
        }
        if (!hasEquip)
        {
            UIManager.Instance.ShowMessage("Não há Cartas de Equipamento no campo.");
            return;
        }

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
        Debug.Log("Disc Fighter: Efeito passivo de batalha transferido para OnAttackDeclaredRoutine.");
    }

    void Effect_0506_DisturbanceStrategy(CardDisplay source)
    {
        // Your opponent shuffles their entire hand into the Deck, then draws the same number of cards.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count == 0)
        {
            UIManager.Instance.ShowMessage("A mão do oponente está vazia.");
            return;
        }

        int count = oppHand.Count;
        if (count > 0)
        {
            GameManager.Instance.ReturnHandToDeck(false);
            for (int i = 0; i < count; i++) GameManager.Instance.DrawOpponentCard();
            Debug.Log($"Disturbance Strategy: Oponente trocou {count} cartas de forma verdadeira.");
        }
    }

    void Effect_0508_DivineWrath(CardDisplay source)
    {
        // Discard 1 card. Negate the activation of an Effect Monster's effect and destroy it.
        var linkCheck = GetLinkToNegate(source);
        if (linkCheck == null || !linkCheck.cardSource.CurrentCardData.type.Contains("Monster"))
        {
            UIManager.Instance.ShowMessage("Não há ativação de efeito de monstro para negar.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 carta da mão.");
            return;
        }

        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                var link = GetLinkToNegate(source);
                if (link != null && link.cardSource.CurrentCardData.type.Contains("Monster"))
                {
                    NegateAndDestroy(source, link);
                }
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
        
        if (turtles.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não tem 'Don Turtle' na mão.");
            return;
        }

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
        Debug.Log("Don Zaloog: Efeito de dano totalmente integrado ao OnDamageDealtImpl.");
    }

    void Effect_0517_DoraOfFate(CardDisplay source)
    {
        CardDisplay summoned = CardEffectManager.Instance.lastSummonedMonster;
        if (summoned != null && summoned.isOnField && summoned.isPlayerCard == source.isPlayerCard)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard != source.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                    (target) => {
                        int myLv = summoned.CurrentCardData.level;
                        int oppLv = target.CurrentCardData.level;
                        if (myLv < oppLv)
                        {
                            int diff = oppLv - myLv;
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                            Effect_DirectDamage(source, diff * 500);
                        }
                        else
                        {
                            int diff = myLv - oppLv;
                            if (source.isPlayerCard) GameManager.Instance.DamagePlayer(diff * 500);
                            else GameManager.Instance.DamageOpponent(diff * 500);
                        }
                    }
                );
            }
        }
        else
        {
            UIManager.Instance.ShowMessage("Deve ser ativada logo após você invocar um monstro.");
        }
    }

    void Effect_0519_DoriadosBlessing(CardDisplay source)
    {
        // Ritual Spell for Doriado.
        GameManager.Instance.BeginRitualSummon(source);
    }

    void Effect_0522_DoubleAttack(CardDisplay source)
    {
        // Discard 1 Monster. Select 1 monster with lower Level. It attacks twice.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 monstro da mão.");
            return;
        }

        bool hasValidTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0 && monsters.Exists(c => c.level > z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.level)) hasValidTarget = true;
        }

        if (!hasValidTarget)
        {
            UIManager.Instance.ShowMessage("Você não possui um monstro no campo com nível menor que os monstros da sua mão.");
            return;
        }

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
                            target.maxAttacksPerTurn = 2;
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
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allST = new List<CardDisplay>();
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allST);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allST);
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allST);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allST);

            foreach (var c in allST) if (!c.isFlipped && (c.CurrentCardData.name == "Jinzo" || c.CurrentCardData.name == "Royal Decree")) hasTarget = true;
        }

        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há 'Jinzo' ou 'Royal Decree' face-up no campo.");
            return;
        }

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

        if (spells.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você precisa descartar 1 carta mágica.");
            return;
        }

        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> oppSpells = oppGY.FindAll(c => c.type.Contains("Spell"));

        if (oppSpells.Count == 0)
        {
            UIManager.Instance.ShowMessage("O oponente não tem cartas mágicas no cemitério.");
            return;
        }

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (cost) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == cost).GetComponent<CardDisplay>());
                
                List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
                List<CardData> oppSpells = oppGY.FindAll(c => c.type.Contains("Spell"));
                
                if (oppSpells.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(oppSpells, "Selecione Magia do Oponente", (target) => {
                        GameManager.Instance.GetOpponentGraveyard().Remove(target);
                        Transform freeZone = null;
                        foreach(var z in GameManager.Instance.duelFieldUI.playerSpellZones) {
                            if (z.childCount == 0) { freeZone = z; break; }
                        }
                        if (freeZone != null) {
                            GameObject newSpell = Instantiate(GameManager.Instance.cardPrefab, freeZone);
                            CardDisplay cd = newSpell.GetComponent<CardDisplay>();
                            cd.SetCard(target, GameManager.Instance.GetCardBackTexture(), true);
                            cd.isPlayerCard = source.isPlayerCard;
                            cd.isOnField = true;
                            GameManager.Instance.ActivateFieldSpellTrap(newSpell);
                        } else {
                            UIManager.Instance.ShowMessage("Sem zonas S/T livres. Magia enviada ao seu GY.");
                            GameManager.Instance.SendToGraveyard(target, source.isPlayerCard);
                        }
                    });
                }
            });
        }
    }

    void Effect_0526_DraggedDownIntoTheGrave(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerHandData().Count > 0 && GameManager.Instance.GetOpponentHandData().Count > 0)
        {
            GameManager.Instance.OpenCardSelection(GameManager.Instance.GetOpponentHandData(), "Descartar do Oponente", (oppTarget) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.opponentHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == oppTarget).GetComponent<CardDisplay>(), true);
                
                // IA simplificada ou seleção mútua (A IA descarta 1 aleatória sua)
                GameManager.Instance.DiscardRandomHand(true, 1);
                
                GameManager.Instance.DrawCard();
                GameManager.Instance.DrawOpponentCard();
            });
        }
    }

    void Effect_0527_DragonCaptureJar(CardDisplay source)
    {
        // Change all face-up Dragon-Type monsters to Defense Position.
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
            foreach (var m in allMonsters) if (m.CurrentCardData.race == "Dragon" && m.position == CardDisplay.BattlePosition.Attack) hasTarget = true;
        }
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há Dragões em Posição de Ataque no campo.");
            return;
        }

        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

            foreach (var m in allMonsters)
            {
                if (m.CurrentCardData.race == "Dragon" && m.position == CardDisplay.BattlePosition.Attack)
                {
                    m.ChangePosition();
                }
            }
        }
    }

    void Effect_0528_DragonManipulator(CardDisplay source)
    {
        // FLIP: Take control of 1 face-up Dragon-Type monster your opponent controls.
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                if (z.childCount > 0 && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Dragon") hasTarget = true;
        }
        
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros do tipo Dragão face-up.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.race == "Dragon" && !t.isFlipped,
                (t) => GameManager.Instance.SwitchControl(t)
            );
        }
    }

    void Effect_0529_DragonMasterKnight(CardDisplay source)
    {
        // Must be Fusion Summoned. +500 ATK for each Dragon on the field and in GY.
        int count = 0;
        // Count field
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
            foreach (var m in allMonsters) if (m.CurrentCardData.race == "Dragon") count++;
        }
        // Count GY
        count += GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.race == "Dragon").Count;
        count += GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.race == "Dragon").Count;

        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 500, source));
    }

    void Effect_0530_DragonPiper(CardDisplay source)
    {
        if (source.isFlipped)
        {
            List<CardDisplay> jars = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, jars);
                CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, jars);
            }
            foreach(var jar in jars) {
                if (jar.CurrentCardData.name == "Dragon Capture Jar" || jar.CurrentCardData.id == "0527") {
                    GameManager.Instance.SendToGraveyard(jar.CurrentCardData, jar.isPlayerCard);
                    Destroy(jar.gameObject);
                }
            }
            
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
            }
            foreach (var m in allMonsters)
            {
                if (m.CurrentCardData.race == "Dragon" && m.position == CardDisplay.BattlePosition.Defense) m.ChangePosition();
            }
        }
    }

    void Effect_0531_DragonSeeker(CardDisplay source)
    {
        // When Normal Summoned: Destroy 1 face-up Dragon.
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
            foreach (var m in allMonsters) if (m.CurrentCardData.race == "Dragon" && !m.isFlipped) hasTarget = true;
        }
        if (!hasTarget)
        {
            Debug.Log("Dragon Seeker: Nenhum Dragão face-up para destruir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.race == "Dragon" && !t.isFlipped,
                (t) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }

    void Effect_0533_DragonTreasure(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Dragon");
    }

    void Effect_0535_DragonsGunfire(CardDisplay source)
    {
        // Activate 1: Inflict 800 damage OR Destroy 1 face-up monster with DEF <= 800.
        // Simplified: If target available, destroy. Else burn.
        // Check for targets...
        // For prototype, just burn.
        bool hasDragon = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.race == "Dragon" && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) hasDragon = true;
        }
        if (!hasDragon)
        {
            UIManager.Instance.ShowMessage("Você precisa controlar um monstro do tipo Dragão face-up.");
            return;
        }

        List<string> options = new List<string> { "Causar 800 de Dano", "Destruir monstro com DEF <= 800" };
        if (MultipleChoiceUI.Instance != null) {
            MultipleChoiceUI.Instance.Show(options, "Dragon's Gunfire: Escolha", 1, 1, (selected) => {
                if (selected[0].Contains("Dano")) {
                    Effect_DirectDamage(source, 800);
                } else {
                    if (SpellTrapManager.Instance != null) {
                        SpellTrapManager.Instance.StartTargetSelection(
                            (t) => t.isOnField && !t.isFlipped && t.currentDef <= 800,
                            (target) => {
                                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                                Destroy(target.gameObject);
                            }
                        );
                    }
                }
            });
        }
    }

    void Effect_0536_DragonsMirror(CardDisplay source)
    {
        GameManager.Instance.BeginFusionSummon(source);
    }

    void Effect_0537_DragonsRage(CardDisplay source)
    {
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0)
                {
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && CardEffectManager.Instance.GetEffectiveRace(cd) == "Dragon") cd.hasPiercing = true;
                }
        }
    }

    void Effect_0539_DragonicAttack(CardDisplay source)
    {
        // Equip Warrior. Becomes Dragon, +500 ATK.
        Effect_Equip(source, 500, 0, "Warrior");
        // Race change logic pending.
    }

    void Effect_0540_DrainingShield(CardDisplay source)
    {
        // Negate attack, gain LP = ATK.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            int atk = BattleManager.Instance.currentAttacker.currentAtk;
            Effect_GainLP(source, atk);
            // Negate attack logic (End Battle Step)
            Debug.Log("Draining Shield: Ataque negado.");
        }
    }

    void Effect_0541_DramaticRescue(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name.Contains("Amazoness"),
                (target) => {
                    GameManager.Instance.ReturnToHand(target);
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));
                    if (monsters.Count > 0) {
                        GameManager.Instance.OpenCardSelection(monsters, "Invocar Monstro", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                            GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    void Effect_0542_DreamClown(CardDisplay source)
    {
        // If changed to Defense: Destroy 1 monster.
        // Trigger logic in OnBattlePositionChangedImpl.
        Debug.Log("Dream Clown: Ativo.");
    }

    void Effect_0543_Dreamsprite(CardDisplay source)
    {
        if (BattleManager.Instance != null && BattleManager.Instance.currentTarget == source)
        {
            if (SpellTrapManager.Instance != null) {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard == source.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && t != source,
                    (newTarget) => {
                        BattleManager.Instance.currentTarget = newTarget;
                        Debug.Log($"Dreamsprite: Ataque redirecionado para {newTarget.CurrentCardData.name}.");
                    }
                );
            }
        }
    }

    void Effect_0544_DrillBug(CardDisplay source)
    {
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        CardData parasite = deck.Find(c => c.name == "Parasite Paracide");
        if (parasite != null)
        {
            deck.Remove(parasite);
            GameManager.Instance.GetOpponentMainDeck().Add(parasite);
            GameManager.Instance.ShuffleDeck(false);
        }
    }

    void Effect_0545_Drillago(CardDisplay source)
    {
        bool oppOnlyStrong = true;
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (z.childCount > 0)
                {
                    hasMonster = true;
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m.currentAtk < 1600 || m.isFlipped) oppOnlyStrong = false;
                }
            }
        }
        if (hasMonster && oppOnlyStrong) source.canAttackDirectly = true;
    }

    void Effect_0546_Drillroid(CardDisplay source)
    {
        Debug.Log("Drillroid: Destrói defesa antes do cálculo (Transferido para OnAttackDeclaredRoutine).");
    }

    void Effect_0547_DrivingSnow(CardDisplay source)
    {
        // If Trap destroyed: Destroy 1 S/T.
        Debug.Log("Driving Snow: Ativo.");
    }

    void Effect_0550_DropOff(CardDisplay source)
    {
        Debug.Log("Drop Off: Resolvido no trigger OnCardDrawn.");
    }

    void Effect_0551_DummyGolem(CardDisplay source)
    {
        // FLIP: Your opponent selects 1 monster they control. Switch control of the selected monster and this card.
        bool oppHasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppHasMonster = true;
        }
        if (!oppHasMonster)
        {
            UIManager.Instance.ShowMessage("O oponente não controla monstros para trocar.");
            return;
        }

        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            
            if (oppMonsters.Count > 0)
            {
                // Simula escolha do oponente (aleatória)
                CardDisplay target = oppMonsters[Random.Range(0, oppMonsters.Count)];
                GameManager.Instance.SwitchControl(source);
                GameManager.Instance.SwitchControl(target);
                Debug.Log($"Dummy Golem: Trocou controle com {target.CurrentCardData.name}.");
            }
        }
    }

    void Effect_0554_DustBarrier(CardDisplay source)
    {
        // Face-up Normal Monsters on the field are unaffected by your opponent's Spell Cards.
        Debug.Log("Dust Barrier: Imunidade a Magias para Monstros Normais (Passivo).");
    }

    void Effect_0555_DustTornado(CardDisplay source)
    {
        // Target 1 Spell/Trap your opponent controls; destroy that target, then you can Set 1 Spell/Trap from your hand.
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentSpellZones) if (z.childCount > 0) hasTarget = true;
            if (GameManager.Instance.duelFieldUI.opponentFieldSpell.childCount > 0) hasTarget = true;
        }
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("O oponente não controla Magias/Armadilhas.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && !t.isPlayerCard,
                (target) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                    Debug.Log($"Dust Tornado: Destruiu {target.CurrentCardData.name}.");

                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> st = hand.FindAll(c => c.type.Contains("Spell") || c.type.Contains("Trap"));
                    if (st.Count > 0 && UIManager.Instance != null) {
                        UIManager.Instance.ShowConfirmation("Dust Tornado: Baixar (Set) 1 Magia/Armadilha da mão?", () => {
                            GameManager.Instance.OpenCardSelection(st, "Selecione para Setar", (selectedST) => {
                                GameObject handCard = GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selectedST);
                                if (handCard != null) GameManager.Instance.PlaySpellTrap(handCard, selectedST, true);
                            });
                        }, null);
                    }
                }
            );
        }
    }

    void Effect_0556_EagleEye(CardDisplay source)
    {
        // When Normal Summoned: No Trap Cards can be activated.
        Debug.Log("Eagle Eye: Traps bloqueadas na invocação.");
    }

    void Effect_0557_EarthChant(CardDisplay source)
    {
        // Ritual Spell for EARTH Ritual Monster.
        GameManager.Instance.BeginRitualSummon(source);
    }

    void Effect_0559_Earthquake(CardDisplay source)
    {
        // Change all face-up monsters on the field to Defense Position.
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
            foreach (var m in allMonsters) if (m.position == CardDisplay.BattlePosition.Attack) hasTarget = true;
        }
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros em Posição de Ataque no campo.");
            return;
        }

        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

            foreach (var m in allMonsters)
            {
                if (m.position == CardDisplay.BattlePosition.Attack)
                {
                    m.ChangePosition();
                }
            }
            Debug.Log("Earthquake: Todos os monstros em defesa.");
        }
    }

    void Effect_0560_Earthshaker(CardDisplay source)
    {
        List<string> attributes = new List<string> { "Earth", "Water", "Fire", "Wind", "Light", "Dark" };
        if (MultipleChoiceUI.Instance != null)
        {
            MultipleChoiceUI.Instance.Show(attributes, "Earthshaker: Escolha 2 Atributos", 2, 2, (selected) => {
                if (selected.Count == 2)
                {
                    // Oponente escolhe 1 dos 2 (Simulando IA rápida)
                    string chosenToDestroy = selected[Random.Range(0, 2)];
                    Debug.Log($"Earthshaker: Oponente escolheu o atributo {chosenToDestroy} para destruir.");
                    
                    List<CardDisplay> toDestroy = new List<CardDisplay>();
                    if (GameManager.Instance.duelFieldUI != null) {
                        CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toDestroy);
                        CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toDestroy);
                    }
                    List<CardDisplay> finalTargets = toDestroy.FindAll(m => m.CurrentCardData.attribute == chosenToDestroy && !m.isFlipped);
                    DestroyCards(finalTargets, source.isPlayerCard);
                }
            });
        }
    }

    void Effect_0561_Eatgaboon(CardDisplay source)
    {
        // If monster with ATK <= 1000 is Summoned: Destroy it.
        Debug.Log("Eatgaboon: Armadilha ativa.");
    }

    void Effect_0562_EbonMagicianCurran(CardDisplay source)
    {
        // Standby Phase: 300 damage per opp monster.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) count++;
        }
        Effect_DirectDamage(source, count * 300);
    }

    void Effect_0563_Ectoplasmer(CardDisplay source)
    {
        // End Phase: Tribute 1 face-up monster, inflict damage = half original ATK.
        Debug.Log("Ectoplasmer: Tributo e dano na End Phase.");
    }

    void Effect_0564_EkibyoDrakmord(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    Effect_Equip(source, 0, 0); 
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                    target.cannotAttackThisTurn = true;
                    target.destructionTurnCountdown = 2;
                    target.destructionCountdownOwnerIsPlayer = !source.isPlayerCard;
                    Debug.Log($"Ekibyo Drakmord: Equipado em {target.CurrentCardData.name}. Destruição em 2 turnos.");
                }
            );
        }
    }

    void Effect_0566_ElectricLizard(CardDisplay source)
    {
        // A non Zombie-Type monster attacking "Electric Lizard" cannot attack on its following turn.
        // Lógica passiva no BattleManager/TurnManager.
        Debug.Log("Electric Lizard: Efeito de atordoamento configurado.");
    }

    void Effect_0567_ElectricSnake(CardDisplay source)
    {
        Debug.Log("Electric Snake: Resolvido no trigger OnCardDiscarded.");
    }

    void Effect_0568_ElectroWhip(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Thunder");
    }

    void Effect_0569_ElectromagneticBagworm(CardDisplay source)
    {
        // FLIP: Take control of 1 Machine opp controls until End Phase.
        // Destroyed by battle: Killer -500 ATK/DEF.
        if (source.isFlipped)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.race == "Machine",
                    (target) => GameManager.Instance.SwitchControl(target)
                );
            }
        }
        // Debuff logic in OnBattleEnd
    }

    void Effect_0570_ElegantEgotist(CardDisplay source)
    {
        // If Harpie Lady on field: SS 1 Harpie Lady or Sisters from Hand/Deck.
        if (!GameManager.Instance.IsCardActiveOnField("Harpie Lady") && !GameManager.Instance.IsCardActiveOnField("0867"))
        {
            UIManager.Instance.ShowMessage("Requer 'Harpie Lady' face-up no campo.");
            return;
        }

        if (GameManager.Instance.IsCardActiveOnField("Harpie Lady") || GameManager.Instance.IsCardActiveOnField("0867"))
        {
            Effect_SearchDeck(source, "Harpie Lady", "Monster"); // Should be SS
        }
    }

    void Effect_0571_ElementDoom(CardDisplay source)
    {
        bool hasFire = false;
        if (GameManager.Instance.duelFieldUI != null) {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.attribute == "Fire") hasFire = true;
        }
        source.RemoveModifiersFromSource(source);
        if (hasFire) source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
    }

    void Effect_0572_ElementDragon(CardDisplay source)
    {
        bool hasFire = false;
        bool hasWind = false;
        if (GameManager.Instance.duelFieldUI != null) {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) {
                if (!m.isFlipped && m.CurrentCardData.attribute == "Fire") hasFire = true;
                if (!m.isFlipped && m.CurrentCardData.attribute == "Wind") hasWind = true;
            }
        }
        source.RemoveModifiersFromSource(source);
        if (hasFire) source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
        if (hasWind) source.maxAttacksPerTurn = 2; else source.maxAttacksPerTurn = 1;
    }

    void Effect_0573_ElementMagician(CardDisplay source)
    {
        bool hasWind = false;
        if (GameManager.Instance.duelFieldUI != null) {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.attribute == "Wind") hasWind = true;
        }
        if (hasWind) source.maxAttacksPerTurn = 2; else source.maxAttacksPerTurn = 1;
    }

    void Effect_0574_ElementSaurus(CardDisplay source)
    {
        bool hasFire = false;
        if (GameManager.Instance.duelFieldUI != null) {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.attribute == "Fire") hasFire = true;
        }
        source.RemoveModifiersFromSource(source);
        if (hasFire) source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
    }

    void Effect_0575_ElementSoldier(CardDisplay source)
    {
        // WATER: No control switch. EARTH: Negate effect of destroyed monster.
        Debug.Log("Element Soldier: Efeitos elementais.");
    }

    void Effect_0576_ElementValkyrie(CardDisplay source)
    {
        bool hasFire = false;
        if (GameManager.Instance.duelFieldUI != null) {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.attribute == "Fire") hasFire = true;
        }
        source.RemoveModifiersFromSource(source);
        if (hasFire) source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
    }

    void Effect_0577_ElementalBurst(CardDisplay source)
    {
        CardDisplay wind = null, water = null, fire = null, earth = null;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null)
                    {
                        if (wind == null && CardEffectManager.Instance.HasAttribute(m, "Wind")) wind = m;
                        else if (water == null && CardEffectManager.Instance.HasAttribute(m, "Water")) water = m;
                        else if (fire == null && CardEffectManager.Instance.HasAttribute(m, "Fire")) fire = m;
                        else if (earth == null && CardEffectManager.Instance.HasAttribute(m, "Earth")) earth = m;
                    }
                }
            }
        }
        if (wind == null || water == null || fire == null || earth == null)
        {
            UIManager.Instance.ShowMessage("Você precisa tributar 1 monstro WIND, WATER, FIRE e EARTH.");
            return;
        }
        GameManager.Instance.TributeCard(wind);
        GameManager.Instance.TributeCard(water);
        GameManager.Instance.TributeCard(fire);
        GameManager.Instance.TributeCard(earth);
        DestroyAllMonsters(true, false);
        Effect_HarpiesFeatherDuster(source);
    }

    void Effect_0579_ElementalHEROBubbleman(CardDisplay source)
    {
        // If hand is empty, SS. If summoned and no other cards, draw 2.
        // Lógica de SS da mão deve ser tratada no SummonManager ou como efeito de ignição na mão.
        // Aqui tratamos o efeito de compra ao ser invocado.
        if (source.isOnField)
        {
            int handCount = GameManager.Instance.GetPlayerHandData().Count;
            // Conta cartas no campo (excluindo Bubbleman)
            if (GameManager.Instance.duelFieldUI != null)
            {
                // ... lógica de contagem ...
                // Simplificado: Se mão vazia (apenas Bubbleman estava lá) e campo vazio (apenas Bubbleman agora)
                // Como é difícil verificar o estado exato "no momento da invocação" sem um snapshot,
                // vamos simplificar para: Se mão vazia, compra 2.
                if (handCount == 0)
                {
                    Debug.Log("Bubbleman: Comprando 2 cartas.");
                    GameManager.Instance.DrawCard();
                    GameManager.Instance.DrawCard();
                }
            }
        }
    }

    void Effect_0582_ElementalHEROFlameWingman(CardDisplay source)
    {
        // Destroy monster by battle -> Burn equal to ATK.
        // Lógica implementada no OnDamageDealtImpl ou OnBattleEnd.
        Debug.Log("Flame Wingman: Efeito de dano configurado.");
    }

    void Effect_0584_ElementalHEROThunderGiant(CardDisplay source)
    {
        // Discard 1 card -> Destroy 1 monster with original ATK < Thunder Giant's ATK.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Sua mão está vazia.");
            return;
        }

        bool hasValidTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                if (z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().originalAtk < source.currentAtk && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) hasValidTarget = true;
        }
        if (!hasValidTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros do oponente com ATK original menor que este card.");
            return;
        }

        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.originalAtk < source.currentAtk,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                            Debug.Log($"Thunder Giant: Destruiu {target.CurrentCardData.name}.");
                        }
                    );
                }
            });
        }
    }

    void Effect_0585_ElementalMistressDoriado(CardDisplay source)
    {
        // Ritual. Attribute is treated as Wind, Water, Fire, and Earth.
        Debug.Log("Doriado: Atributos múltiplos.");
    }

    void Effect_0586_ElephantStatueOfBlessing(CardDisplay source)
    {
        Debug.Log("Elephant Statue of Blessing: Resolvido no trigger OnCardDiscarded.");
    }

    void Effect_0587_ElephantStatueOfDisaster(CardDisplay source)
    {
        Debug.Log("Elephant Statue of Disaster: Resolvido no trigger OnCardDiscarded.");
    }

    void Effect_0588_ElfsLight(CardDisplay source)
    {
        // Equip only to a LIGHT monster. It gains 400 ATK and loses 200 DEF.
        Effect_Equip(source, 400, -200, "", "Light");
    }

    void Effect_0589_EmblemOfDragonDestroyer(CardDisplay source)
    {
        // Add 1 "Buster Blader" from your Deck or Graveyard to your hand.
        List<CardData> sources = new List<CardData>();
        sources.AddRange(GameManager.Instance.GetPlayerMainDeck());
        sources.AddRange(GameManager.Instance.GetPlayerGraveyard());
        
        List<CardData> targets = sources.FindAll(c => c.name == "Buster Blader");
        
        if (targets.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui 'Buster Blader' no Deck ou Cemitério.");
            return;
        }

        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Selecionar Buster Blader", (selected) => {
                if (GameManager.Instance.GetPlayerMainDeck().Contains(selected))
                    GameManager.Instance.GetPlayerMainDeck().Remove(selected);
                else if (GameManager.Instance.GetPlayerGraveyard().Contains(selected))
                    GameManager.Instance.GetPlayerGraveyard().Remove(selected);
                
                GameManager.Instance.AddCardToHand(selected, true);
                GameManager.Instance.ShuffleDeck(true);
                Debug.Log("Emblem of Dragon Destroyer: Buster Blader adicionado à mão.");
            });
        }
        else
        {
            Debug.Log("Emblem of Dragon Destroyer: Buster Blader não encontrado no Deck ou GY.");
        }
    }

    void Effect_0590_EmbodimentOfApophis(CardDisplay source)
    {
        if (GameManager.Instance.ConvertTrapToMonster(source, 1600, 1800, 4, "Reptile", "Earth"))
        {
            Debug.Log("Embodiment of Apophis: Invocado como Trap Monster.");
        }
        else
        {
            UIManager.Instance.ShowMessage("Não há zonas de monstros disponíveis.");
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
        }
    }

    void Effect_0592_EmergencyProvisions(CardDisplay source)
    {
        List<CardDisplay> myST = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, myST);
            CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell }, myST);
            myST.Remove(source);
        }
        
        if (myST.Count > 0)
        {
            int count = myST.Count;
            foreach(var st in myST) {
                GameManager.Instance.SendToGraveyard(st.CurrentCardData, st.isPlayerCard);
                Destroy(st.gameObject);
            }
            Effect_GainLP(source, count * 1000);
        }
    }

    void Effect_0593_EmesTheInfinity(CardDisplay source)
    {
        // Increase ATK by 700 each time it destroys a monster by battle.
        // Lógica no OnBattleEnd.
        Debug.Log("Emes the Infinity: Efeito passivo de batalha.");
    }

    void Effect_0594_EmissaryOfTheAfterlife(CardDisplay source)
    {
        // When sent from field to GY: Each player selects Lv3 or lower Normal Monster from Deck to Hand.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Emissary of the Afterlife: Busca mútua.");
    }

    void Effect_0595_EmissaryOfTheOasis(CardDisplay source)
    {
        // Lv3 or lower Normal Monsters cannot be destroyed by battle.
        // Passivo no BattleManager.
        Debug.Log("Emissary of the Oasis: Proteção ativa.");
    }

    void Effect_0599_EnchantedJavelin(CardDisplay source)
    {
        // Gain LP equal to attacking monster's ATK.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            int atk = BattleManager.Instance.currentAttacker.currentAtk;
            Effect_GainLP(source, atk);
            Debug.Log($"Enchanted Javelin: Ganhou {atk} LP.");
        }
    }

    void Effect_0600_EnchantingFittingRoom(CardDisplay source)
    {
        // Pay 800 LP. Pick up 4 cards. SS Lv3 or lower Normal Monsters. Return rest to Deck.
        if (GameManager.Instance.playerLP <= 800)
        {
            UIManager.Instance.ShowMessage("Pontos de Vida insuficientes (800 necessários).");
            return;
        }
        if (GameManager.Instance.GetPlayerMainDeck().Count == 0)
        {
            UIManager.Instance.ShowMessage("Seu Deck está vazio.");
            return;
        }

        if (Effect_PayLP(source, 800))
        {
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            if (deck.Count > 0)
            {
                int count = Mathf.Min(4, deck.Count);
                List<CardData> picked = deck.GetRange(0, count);
                deck.RemoveRange(0, count);

                List<CardData> toSummon = picked.FindAll(c => c.level <= 3 && c.type.Contains("Normal") && c.type.Contains("Monster"));
                
                foreach (var card in toSummon)
                {
                    GameManager.Instance.SpecialSummonFromData(card, source.isPlayerCard);
                    picked.Remove(card);
                }

                // Retorna o resto
                foreach (var card in picked)
                {
                    deck.Add(card);
                }
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                Debug.Log($"Enchanting Fitting Room: {toSummon.Count} monstros invocados.");
            }
        }
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0601 - 0700)
    // =========================================================================================

    void Effect_0603_EnergyDrain(CardDisplay source)
    {
        // Target 1 face-up monster you control; it gains 200 ATK/DEF for each card your opponent currently has in their hand.
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0 && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) hasMonster = true;
        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Você não controla monstros face-up.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    int oppHandCount = GameManager.Instance.GetOpponentHandData().Count;
                    int buff = oppHandCount * 200;
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
                    Debug.Log($"Energy Drain: {target.CurrentCardData.name} ganhou {buff} ATK/DEF.");
                }
            );
        }
    }

    void Effect_0604_EnervatingMist(CardDisplay source)
    {
        // Your opponent's hand size limit becomes 5.
        Debug.Log("Enervating Mist: Limite de mão do oponente reduzido para 5 (Lógica de descarte na End Phase pendente).");
    }

    void Effect_0605_EnragedBattleOx(CardDisplay source)
    {
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                if (z.childCount > 0)
                {
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && (CardEffectManager.Instance.GetEffectiveRace(cd) == "Beast" || CardEffectManager.Instance.GetEffectiveRace(cd) == "Beast-Warrior" || CardEffectManager.Instance.GetEffectiveRace(cd) == "Winged Beast")) cd.hasPiercing = true;
                }
        }
    }

    void Effect_0606_EnragedMukaMuka(CardDisplay source)
    {
        Debug.Log("Enraged Muka Muka: Buff dinâmico movido para OnPhaseStart.");
    }

    void Effect_0607_EradicatingAerosol(CardDisplay source)
    {
        bool hasInsect = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (!m.isFlipped && m.CurrentCardData.race == "Insect") hasInsect = true;
        }
        if (!hasInsect)
        {
            UIManager.Instance.ShowMessage("Não há monstros Inseto face-up no campo.");
            return;
        }
        Effect_DestroyType(source, "Insect");
    }

    void Effect_0608_EriaTheWaterCharmer(CardDisplay source)
    {
        // FLIP: Take control of 1 WATER monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.attribute == "Water" && !t.isFlipped,
                (t) => GameManager.Instance.SwitchControl(t)
            );
        }
    }

    void Effect_0609_EternalDrought(CardDisplay source)
    {
        bool hasFish = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (!m.isFlipped && m.CurrentCardData.race == "Fish") hasFish = true;
        }
        if (!hasFish)
        {
            UIManager.Instance.ShowMessage("Não há monstros Peixe face-up no campo.");
            return;
        }
        Effect_DestroyType(source, "Fish");
    }

    void Effect_0610_EternalRest(CardDisplay source)
    {
        // Destroy all monsters equipped with Equip Cards.
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

            foreach (var m in allMonsters)
            {
                if (CardEffectManager.Instance.GetEquippedCards(m).Count > 0)
                {
                    toDestroy.Add(m);
                }
            }
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_0611_ExarionUniverse(CardDisplay source)
    {
        Debug.Log("Exarion Universe: Lógica de ativação movida para o OnAttackDeclared (Sistema 2).");
    }

    void Effect_0612_Exchange(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerHandData().Count == 0 || GameManager.Instance.GetOpponentHandData().Count == 0)
        {
            UIManager.Instance.ShowMessage("Ambos os jogadores precisam ter cartas na mão.");
            return;
        }

        List<CardData> myHand = GameManager.Instance.GetPlayerHandData();
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        
        GameManager.Instance.OpenCardSelection(oppHand, "Escolha 1 carta do oponente", (oppCard) => {
            CardData myCard = myHand[Random.Range(0, myHand.Count)]; // IA escolhe uma sua aleatória
            GameManager.Instance.RemoveCardFromHand(myCard, true);
            GameManager.Instance.RemoveCardFromHand(oppCard, false);
            GameManager.Instance.AddCardToHand(oppCard, true);
            GameManager.Instance.AddCardToHand(myCard, false);
            Debug.Log($"Exchange: Você pegou {oppCard.name} e a IA pegou {myCard.name}.");
        });
    }

    void Effect_0613_ExchangeOfTheSpirit(CardDisplay source)
    {
        // If both players have 15 or more cards in their Graveyards: Pay 1000 LP; swap Deck and GY.
        int myGYCount = GameManager.Instance.GetPlayerGraveyard().Count;
        int oppGYCount = GameManager.Instance.GetOpponentGraveyard().Count;

        if (myGYCount < 15 || oppGYCount < 15)
        {
            UIManager.Instance.ShowMessage("Ambos os jogadores precisam ter 15 ou mais cartas no Cemitério.");
            return;
        }

        if (myGYCount >= 15 && oppGYCount >= 15)
        {
            if (Effect_PayLP(source, 1000))
            {
                // Swap Player
                List<CardData> myDeck = GameManager.Instance.GetPlayerMainDeck();
                List<CardData> myGY = GameManager.Instance.GetPlayerGraveyard();
                List<CardData> temp = new List<CardData>(myDeck);
                myDeck.Clear();
                myDeck.AddRange(myGY);
                myGY.Clear();
                myGY.AddRange(temp);
                GameManager.Instance.ShuffleDeck(true);

                // Swap Opponent
                List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
                List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
                temp = new List<CardData>(oppDeck);
                oppDeck.Clear();
                oppDeck.AddRange(oppGY);
                oppGY.Clear();
                oppGY.AddRange(temp);
                GameManager.Instance.ShuffleDeck(false);

                Debug.Log("Exchange of the Spirit: Decks e Cemitérios trocados.");
            }
        }
    }

    void Effect_0614_ExhaustingSpell(CardDisplay source)
    {
        // Remove all Spell Counters on the field.
        if (GetTotalSpellCounters(true) == 0 && GetTotalSpellCounters(false) == 0)
        {
            UIManager.Instance.ShowMessage("Não há Spell Counters no campo.");
            return;
        }

        if (GameManager.Instance.duelFieldUI != null)
        {
            // Itera tudo e remove
            // ...
            Debug.Log("Exhausting Spell: Todos os Spell Counters removidos.");
        }
    }

    void Effect_0615_ExileOfTheWicked(CardDisplay source)
    {
        bool hasFiend = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (!m.isFlipped && m.CurrentCardData.race == "Fiend") hasFiend = true;
        }
        if (!hasFiend)
        {
            UIManager.Instance.ShowMessage("Não há monstros Demônio face-up no campo.");
            return;
        }
        Effect_DestroyType(source, "Fiend");
    }

    void Effect_0616_ExiledForce(CardDisplay source)
    {
        // Tribute this card to target 1 monster; destroy it.
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m != source) hasTarget = true;
        }
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há outros monstros no campo para destruir.");
            return;
        }

        if (source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (target) => {
                        GameManager.Instance.TributeCard(source);
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
    }

    void Effect_0617_ExodiaNecross(CardDisplay source)
    {
        // Cannot be Normal Summoned. SS by Contract with Exodia.
        // Cannot be destroyed by battle/spell/trap.
        // Gains 500 ATK each Standby.
        // Destroy if Exodia parts not in GY.
        Debug.Log("Exodia Necross: Efeitos passivos e de manutenção configurados.");
    }

    void Effect_0618_ExodiaTheForbiddenOne(CardDisplay source)
    {
        GameManager.Instance.CheckExodiaWin();
    }

    void Effect_0620_FairyBox(CardDisplay source)
    {
        Debug.Log("Fairy Box: Efeito de moeda movido para o OnAttackDeclared (Sistema 2).");
    }

    void Effect_0622_FairyGuardian(CardDisplay source)
    {
        // Tribute to return Spell from GY to Deck.
        List<CardData> gyCheck = GameManager.Instance.GetPlayerGraveyard();
        if (!gyCheck.Exists(c => c.type.Contains("Spell")))
        {
            UIManager.Instance.ShowMessage("Você não possui Cartas Mágicas no Cemitério.");
            return;
        }

        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));
            if (spells.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(spells, "Retornar Magia ao Deck", (selected) => {
                    gy.Remove(selected);
                    GameManager.Instance.ReturnToDeck(null, true); // Precisa adaptar ReturnToDeck para aceitar CardData ou criar novo método
                    // Workaround:
                    GameManager.Instance.GetPlayerMainDeck().Insert(0, selected);
                    Debug.Log($"Fairy Guardian: {selected.name} retornada ao topo do deck.");
                });
            }
        }
    }

    void Effect_0623_FairyKingTruesdale(CardDisplay source)
    {
        // While in Defense: Plant monsters gain 500 ATK/DEF.
        if (source.position == CardDisplay.BattlePosition.Defense)
        {
            Effect_Field(source, 500, 500, "Plant");
        }
    }

    void Effect_0624_FairyMeteorCrush(CardDisplay source)
    {
        // Equip: Piercing.
        Effect_Equip(source, 0, 0);
        // Lógica de piercing no BattleManager.
    }

    void Effect_0626_FairyOfTheSpring(CardDisplay source)
    {
        // Target 1 Equip Spell in GY; add to hand. Cannot activate this turn.
        List<CardData> gyCheck = GameManager.Instance.GetPlayerGraveyard();
        if (!gyCheck.Exists(c => c.type.Contains("Spell") && c.property == "Equip"))
        {
            UIManager.Instance.ShowMessage("Você não possui Magias de Equipamento no Cemitério.");
            return;
        }

        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> equips = gy.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");

        if (equips.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(equips, "Recuperar Equip Spell", (selected) => {
                gy.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                GameManager.Instance.forbiddenSpells.Add(selected.name);
                Debug.Log($"Fairy of the Spring: {selected.name} recuperada. Não pode ser ativada neste turno.");
            });
        }
    }

    void Effect_0628_FairysHandMirror(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Spell") && link.target != null && link.target.CurrentCardData.type.Contains("Monster"))
        {
            if (SpellTrapManager.Instance != null) {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t != link.target,
                    (newTarget) => {
                        link.target = newTarget;
                        Debug.Log($"Fairy's Hand Mirror: Alvo da magia redirecionado para {newTarget.CurrentCardData.name}.");
                    }
                );
            }
        }
    }

    void Effect_0631_FakeTrap(CardDisplay source)
    {
        // Destroy this card instead of other Traps.
        Debug.Log("Fake Trap: Destruída no lugar de outras armadilhas.");
    }

    void Effect_0632_FallingDown(CardDisplay source)
    {
        // Equip to opponent's monster; take control. Destroy if no Archfiend. Burn 800.
        if (GameManager.Instance.IsCardActiveOnField("Archfiend") || GameManager.Instance.IsCardActiveOnField("0009")) // Verifica Archfiend genérico
        {
            Effect_ChangeControl(source, false);
            // Lógica de manutenção/dano no OnPhaseStart
        }
        else
        {
            Debug.Log("Falling Down: Requer carta Archfiend.");
        }
    }

    void Effect_0633_FamiliarKnight(CardDisplay source)
    {
        // If destroyed by battle: Each player SS 1 Lv4 monster.
        // Lógica no OnBattleEnd.
        Debug.Log("Familiar Knight: Efeito de invocação mútua configurado.");
    }

    void Effect_0634_FatalAbacus(CardDisplay source)
    {
        // Each time a monster is sent to GY, 500 damage to owner.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Fatal Abacus: Efeito passivo de dano.");
    }

    void Effect_0635_FearFromTheDark(CardDisplay source)
    {
        // If sent from hand/deck to GY by opponent: SS this card.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Fear from the Dark: Efeito passivo de invocação.");
    }

    void Effect_0636_FengshengMirror(CardDisplay source)
    {
        // Look at opponent's hand, discard 1 Spirit.
        if (GameManager.Instance.GetOpponentHandData().Count == 0)
        {
            UIManager.Instance.ShowMessage("A mão do oponente está vazia.");
            return;
        }

        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        GameManager.Instance.OpenCardSelection(oppHand, "Mão do Oponente", (selected) => {
            if (selected.type.Contains("Spirit"))
            {
                var go = GameManager.Instance.opponentHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selected);
                if (go != null) GameManager.Instance.DiscardCard(go.GetComponent<CardDisplay>(), true);
                Debug.Log($"Fengsheng Mirror: Descartou {selected.name}.");
            }
        });
    }

    void Effect_0637_Fenrir(CardDisplay source)
    {
        // SS by banishing 2 Water. Destroy monster -> Skip Draw.
        // Lógica de SS no SummonManager/Hand.
        // Lógica de Skip Draw no OnBattleEnd/PhaseManager.
        Debug.Log("Fenrir: Efeitos configurados.");
    }

    void Effect_0639_FiberJar(CardDisplay source)
    {
        List<CardDisplay> allCards = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null) {
            CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, allCards);
            CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, allCards);
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allCards);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allCards);
        }
        foreach(var c in allCards) GameManager.Instance.ReturnToDeck(c, false);
        
        GameManager.Instance.ReturnHandToDeck(true);
        GameManager.Instance.ReturnHandToDeck(false);
        
        List<CardData> pGY = GameManager.Instance.GetPlayerGraveyard();
        GameManager.Instance.GetPlayerMainDeck().AddRange(pGY);
        pGY.Clear();
        
        List<CardData> oGY = GameManager.Instance.GetOpponentGraveyard();
        GameManager.Instance.GetOpponentMainDeck().AddRange(oGY);
        oGY.Clear();

        GameManager.Instance.ShuffleDeck(true);
        GameManager.Instance.ShuffleDeck(false);

        for(int i=0; i<5; i++) GameManager.Instance.DrawCard(true);
        for(int i=0; i<5; i++) GameManager.Instance.DrawOpponentCard();
    }

    void Effect_0640_FiendComedian(CardDisplay source)
    {
        // Coin toss. Heads: Banish opp GY. Tails: Mill deck equal to opp GY.
        if (GameManager.Instance.GetOpponentGraveyard().Count == 0)
        {
            UIManager.Instance.ShowMessage("O Cemitério do oponente está vazio.");
            return;
        }

        GameManager.Instance.TossCoin(1, (heads) => {
            int oppGYCount = GameManager.Instance.GetOpponentGraveyard().Count;
            if (heads == 1)
            {
                Debug.Log("Fiend Comedian (Cara): Banindo cemitério do oponente.");
                // Banish all opp GY
                List<CardData> gy = new List<CardData>(GameManager.Instance.GetOpponentGraveyard());
                foreach(var c in gy)
                {
                    GameManager.Instance.RemoveFromPlay(c, false);
                    GameManager.Instance.GetOpponentGraveyard().Remove(c);
                }
            }
            else
            {
                Debug.Log($"Fiend Comedian (Coroa): Millando {oppGYCount} cartas.");
                GameManager.Instance.MillCards(source.isPlayerCard, oppGYCount);
            }
        });
    }

    void Effect_0645_FiendSkullDragon(CardDisplay source)
    {
        // Fusion. Negate Flip effects. Negate Trap targeting it.
        Debug.Log("Fiend Skull Dragon: Efeitos passivos ativos.");
    }

    void Effect_0648_FiendsHandMirror(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Spell") && link.target != null && (link.target.CurrentCardData.type.Contains("Spell") || link.target.CurrentCardData.type.Contains("Trap")))
        {
            if (SpellTrapManager.Instance != null) {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && t != link.target,
                    (newTarget) => {
                        link.target = newTarget;
                        Debug.Log($"Fiend's Hand Mirror: Alvo da magia redirecionado para {newTarget.CurrentCardData.name}.");
                    }
                );
            }
        }
    }

    void Effect_0650_FiendsSanctuary(CardDisplay source)
    {
        // SS Metal Fiend Token.
        if (GameManager.Instance.GetFreeMonsterZone(source.isPlayerCard) == null)
        {
            UIManager.Instance.ShowMessage("Não há zonas de monstros disponíveis.");
            return;
        }

        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Metal Fiend Token");
        // Token logic:
        // - Cannot attack
        // - Opponent takes battle damage
        // - Pay 1000 LP standby or destroy
        Debug.Log("Fiend's Sanctuary: Token invocado (Lógica de dano/manutenção pendente).");
    }

    void Effect_0651_FinalAttackOrders(CardDisplay source)
    {
        // All face-up monsters on the field are changed to Attack Position and their battle positions cannot be changed.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

            foreach (var m in allMonsters)
            {
                if (m.position == CardDisplay.BattlePosition.Defense)
                {
                    m.ChangePosition();
                }
            }
            Debug.Log("Final Attack Orders: Todos os monstros em ataque.");
            // Nota: A restrição de mudança de posição deve ser checada no BattleManager/GameManager
        }
    }

    void Effect_0652_FinalCountdown(CardDisplay source)
    {
        if (GameManager.Instance.playerLP <= 2000)
        {
            UIManager.Instance.ShowMessage("Pontos de Vida insuficientes (Requer mais de 2000).");
            return;
        }

        if (Effect_PayLP(source, 2000))
        {
            CardEffectManager.Instance.finalCountdownActive = true;
            CardEffectManager.Instance.finalCountdownTurnsLeft = 20;
            CardEffectManager.Instance.finalCountdownPlayer = source.isPlayerCard;
            Debug.Log("Final Countdown: Contagem de 20 turnos iniciada.");
        }
    }

    void Effect_0653_FinalDestiny(CardDisplay source)
    {
        // Discard 5 cards from your hand; destroy all cards on the field.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count < 5)
        {
            UIManager.Instance.ShowMessage("Requer 5 cartas na mão para descartar.");
            return;
        }

        if (hand.Count >= 5)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Descarte 5 cartas", 5, 5, (selected) => {
                foreach (var c in selected) {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c).GetComponent<CardDisplay>());
                }
                Debug.Log("Final Destiny: Destruindo tudo!");
                DestroyAllMonsters(true, true);
                Effect_HeavyStorm(source); // Destrói S/T
            });
        }
        else
        {
            Debug.Log("Final Destiny: Requer 5 cartas na mão.");
        }
    }

    void Effect_0654_FinalFlame(CardDisplay source)
    {
        Effect_DirectDamage(source, 600);
    }

    void Effect_0655_FinalRitualOfTheAncients(CardDisplay source)
    {
        // Ritual Spell for "Reshef the Dark Being".
        Debug.Log("Final Ritual of the Ancients: Ritual para Reshef.");
    }

    void Effect_0656_FireDarts(CardDisplay source)
    {
        GameManager.Instance.RollDice(3, false, (results) => {
            int total = results.Sum();
            int damage = total * 100;
            Debug.Log($"Fire Darts: Rolou {results[0]}, {results[1]}, {results[2]}. Total {total}. Dano {damage}.");
            Effect_DirectDamage(source, damage);
        });
    }

    void Effect_0659_FirePrincess(CardDisplay source)
    {
        // Each time you gain Life Points, inflict 500 damage to your opponent.
        // Lógica implementada no CardEffectManager_Impl.cs (OnLifePointsGained)
        Debug.Log("Fire Princess: Efeito passivo de cura.");
    }

    void Effect_0661_FireSorcerer(CardDisplay source)
    {
        // FLIP: Banish 2 cards from your hand to inflict 800 damage.
        if (GameManager.Instance.GetPlayerHandData().Count < 2)
        {
            UIManager.Instance.ShowMessage("Requer 2 cartas na mão para banir.");
            return;
        }

        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Banir 2 da mão", 2, 2, (selected) => {
                foreach(var c in selected) {
                    var go = GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c);
                    if (go != null) {
                        GameManager.Instance.RemoveCardFromHand(c, source.isPlayerCard);
                        GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                        Destroy(go);
                    }
                }
                Effect_DirectDamage(source, 800);
                Debug.Log("Fire Sorcerer: Baniu 2 da mão, causou 800 dano.");
            });
        }
    }

    void Effect_0666_Fissure(CardDisplay source)
    {
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            
            CardDisplay target = null;
            int minAtk = 9999;
            
            foreach(var m in oppMonsters)
            {
                if (!m.isFlipped)
                {
                    if (m.currentAtk < minAtk) { minAtk = m.currentAtk; target = m; }
                }
            }
            
            if (target != null)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
            }
        }
    }

    void Effect_0667_FiveHeadedDragon(CardDisplay source)
    {
        // Cannot be destroyed by battle with Earth, Water, Fire, Wind, or Dark monsters.
        // Lógica passiva no BattleManager (ResolveDamage)
        Debug.Log("Five-Headed Dragon: Proteção de batalha ativa.");
    }

    void Effect_0673_FlameRuler(CardDisplay source)
    {
        // If you Tribute Summon a FIRE monster, you can treat this 1 monster as 2 Tributes.
        // Lógica no SummonManager (GetRequiredTributes/ProcessAutoTribute)
        Debug.Log("Flame Ruler: Vale por 2 tributos para FIRE.");
    }

    void Effect_0676_FlashAssailant(CardDisplay source)
    {
        Debug.Log("Flash Assailant: Debuff dinâmico movido para OnPhaseStart.");
    }

    void Effect_0677_Flint(CardDisplay source)
    {
        // Equip: Equipped monster loses 300 ATK, cannot change position or attack.
        // If destroyed, equip to another monster.
        Effect_Equip(source, -300, 0);
        // Lógica de travar posição/ataque no BattleManager/GameManager.
        // Lógica de re-equipar no OnCardLeavesField.
    }

    void Effect_0680_FlyingKamakiri1(CardDisplay source)
    {
        // When destroyed by battle: SS 1 WIND monster with ATK <= 1500 from Deck.
        // Diferente do Effect_SearchDeck (que adiciona à mão), este invoca.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> targets = deck.FindAll(c => c.attribute == "Wind" && c.atk <= 1500 && c.type.Contains("Monster"));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Invocar WIND <= 1500", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                deck.Remove(selected);
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    void Effect_0683_FollowWind(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Winged Beast");
    }

    void Effect_0684_FoolishBurial(CardDisplay source)
    {
        // Send 1 monster from Deck to GY.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        
        if (!deck.Exists(c => c.type.Contains("Monster")))
        {
            UIManager.Instance.ShowMessage("Você não possui monstros no Deck.");
            return;
        }

        List<CardData> monsters = deck.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Enviar ao GY", (selected) => {
                deck.Remove(selected);
                GameManager.Instance.SendToGraveyard(selected, source.isPlayerCard);
                Debug.Log($"Foolish Burial: {selected.name} enviado ao GY.");
            });
        }
    }

    void Effect_0685_ForcedCeasefire(CardDisplay source)
    {
        // Discard 1 card. No Traps can be activated this turn.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count == 0)
        {
            UIManager.Instance.ShowMessage("Sua mão está vazia.");
            return;
        }

        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Forced Ceasefire: Traps bloqueadas neste turno.");
                CardEffectManager.Instance.trapsBlockedThisTurn = true;
            });
        }
    }

    void Effect_0686_ForcedRequisition(CardDisplay source)
    {
        // Continuous Trap: When you discard, opponent must discard same number.
        Debug.Log("Forced Requisition: Ativo (Lógica no OnCardDiscarded).");
    }

    void Effect_0687_Forest(CardDisplay source)
    {
        // Field: Insect, Plant, Beast, Beast-Warrior +200 ATK/DEF.
        Effect_Field(source, 200, 200, "Insect");
        Effect_Field(source, 200, 200, "Plant");
        Effect_Field(source, 200, 200, "Beast");
        Effect_Field(source, 200, 200, "Beast-Warrior");
    }

    void Effect_0688_FormationUnion(CardDisplay source)
    {
        // Union monsters can equip/unequip.
        Debug.Log("Formation Union: Ativo (Permite equipar/desequipar Unions).");
    }

    void Effect_0691_FoxFire(CardDisplay source)
    {
        // If destroyed face-up: SS during End Phase.
        Debug.Log("Fox Fire: Renasce na End Phase.");
    }

    void Effect_0692_FreedTheBraveWanderer(CardDisplay source)
    {
        // Banish 2 LIGHT from GY; destroy 1 monster with higher ATK.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> lights = gy.FindAll(c => c.attribute == "Light");
        
        if (lights.Count < 2)
        {
            UIManager.Instance.ShowMessage("Requer 2 monstros LIGHT no Cemitério.");
            return;
        }
        
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach (var m in all) if (m.currentAtk > source.currentAtk && !m.isFlipped) hasTarget = true;
        }
        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros face-up com ATK maior que este card.");
            return;
        }
        
        if (lights.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(lights, "Banir 2 LIGHT", 2, 2, (selected) => {
                foreach(var c in selected) {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    gy.Remove(c);
                }
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.currentAtk > source.currentAtk,
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

    void Effect_0693_FreedTheMatchlessGeneral(CardDisplay source)
    {
        Debug.Log("Freed General: Imune a Magias de alvo. Efeito de Busca movido para OnPreDrawPhase.");
    }

    void Effect_0694_FreezingBeast(CardDisplay source)
    {
        Effect_Union(source, "Burning Beast", 0, 0);
    }

    void Effect_0696_FrontierWiseman(CardDisplay source)
    {
        // Negate Spell targeting Warrior.
        Debug.Log("Frontier Wiseman: Protege Warriors de Magias de alvo.");
    }

    void Effect_0697_FrontlineBase(CardDisplay source)
    {
        List<CardData> hand = source.isPlayerCard ? GameManager.Instance.GetPlayerHandData() : GameManager.Instance.GetOpponentHandData();
        List<CardData> unions = hand.FindAll(c => c.description.Contains("Union") && c.level <= 4);
        
        if (unions.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(unions, "Invocar Union", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0698_FrozenSoul(CardDisplay source)
    {
        int myLp = source.isPlayerCard ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        int oppLp = source.isPlayerCard ? GameManager.Instance.opponentLP : GameManager.Instance.playerLP;
        
        if (myLp <= oppLp - 2000) {
            if (PhaseManager.Instance != null) PhaseManager.Instance.RegisterSkipNextPhase(!source.isPlayerCard, GamePhase.Battle);
        } else {
            UIManager.Instance.ShowMessage("Seus LP devem estar no mínimo 2000 pontos abaixo dos do oponente.");
        }
    }

    void Effect_0699_FruitsOfKozakysStudies(CardDisplay source)
    {
        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
        if (deck.Count < 3)
        {
            UIManager.Instance.ShowMessage("Requer pelo menos 3 cartas no Deck.");
            return;
        }
        
        int count = Mathf.Min(3, deck.Count);
        List<CardData> topCards = deck.GetRange(0, count);
        
        if (source.isPlayerCard && ReorderCardsUI.Instance != null)
        {
            ReorderCardsUI.Instance.Show(topCards, "Fruits of Kozaky: Reordene as cartas", (ordered) => {
                deck.RemoveRange(0, count);
                deck.InsertRange(0, ordered);
                Debug.Log("Fruits of Kozaky's Studies: Deck reordenado.");
            });
        }
        else if (!source.isPlayerCard)
        {
            deck.RemoveRange(0, count);
            topCards.Sort((a,b) => Random.Range(-1, 2)); // Embaralha levemente as 3 do oponente
            deck.InsertRange(0, topCards);
        }
    }

    void Effect_0700_FuhRinKaZan(CardDisplay source)
    {
        // If Wind, Water, Fire, Earth on field: Apply 1 effect.
        bool wind = false, water = false, fire = false, earth = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all)
            {
                if (!m.isFlipped)
                {
                    if (CardEffectManager.Instance.HasAttribute(m, "Wind")) wind = true;
                    if (CardEffectManager.Instance.HasAttribute(m, "Water")) water = true;
                    if (CardEffectManager.Instance.HasAttribute(m, "Fire")) fire = true;
                    if (CardEffectManager.Instance.HasAttribute(m, "Earth")) earth = true;
                }
            }
        }
        
        if (!(wind && water && fire && earth))
        {
            UIManager.Instance.ShowMessage("Requer monstros WIND, WATER, FIRE e EARTH face-up no campo.");
            return;
        }

        List<string> options = new List<string> { 
            "Destruir todos os monstros do oponente", 
            "Destruir todas as Magias/Armadilhas do oponente", 
            "Oponente descarta 2 cartas", 
            "Comprar 2 cartas" 
        };

        if (MultipleChoiceUI.Instance != null)
        {
            MultipleChoiceUI.Instance.Show(options, "Fuh-Rin-Ka-Zan: Escolha 1 Efeito", 1, 1, (selected) => {
                if (selected.Count == 1)
                {
                    string opt = selected[0];
                    if (opt.Contains("monstros")) DestroyAllMonsters(true, false);
                    else if (opt.Contains("Magias")) Effect_HarpiesFeatherDuster(source);
                    else if (opt.Contains("descarta")) GameManager.Instance.DiscardRandomHand(false, 2);
                    else if (opt.Contains("Comprar")) {
                        GameManager.Instance.DrawCard();
                        GameManager.Instance.DrawCard();
                    }
                }
            });
        }
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0701 - 0800)
    // =========================================================================================

    void Effect_0701_FuhmaShuriken(CardDisplay source)
    {
        bool hasNinja = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.name.Contains("Ninja")) hasNinja = true;
        }

        if (!hasNinja)
        {
            UIManager.Instance.ShowMessage("Não há monstros 'Ninja' face-up no campo para equipar.");
            return;
        }
        Effect_Equip(source, 700, 0);
        Debug.Log("Germ Infection: O decaimento de ATK ocorre na Standby Phase (OnPhaseStart).");
    }

    void Effect_0702_FulfillmentOfTheContract(CardDisplay source)
    {
        if (GameManager.Instance.playerLP <= 800)
        {
            UIManager.Instance.ShowMessage("Pontos de Vida insuficientes (800 necessários).");
            return;
        }

        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> rituals = gy.FindAll(c => c.type.Contains("Ritual") && c.type.Contains("Monster"));
        
        if (rituals.Count == 0)
        {
            UIManager.Instance.ShowMessage("Você não possui Monstros Ritual no Cemitério.");
            return;
        }

        if (Effect_PayLP(source, 800))
        {
            GameManager.Instance.OpenCardSelection(rituals, "Reviver Ritual", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                Debug.Log("Fulfillment: Equipado ao monstro revivido.");
            });
        }
    }

    void Effect_0704_FushiNoTori(CardDisplay source)
    {
        // Spirit. If inflicts battle damage, gain LP equal to damage.
        Debug.Log("Fushi No Tori: Spirit. Cura por dano.");
    }

    void Effect_0705_FushiohRichie(CardDisplay source)
    {
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> zombies = gy.FindAll(c => c.race == "Zombie");
        
        if (zombies.Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há monstros Zombie no Cemitério.");
            return;
        }

        GameManager.Instance.OpenCardSelection(zombies, "Reviver Zumbi", (selected) => {
            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
        });
    }

    void Effect_0706_FusilierDragon(CardDisplay source)
    {
        // Can be Normal Summoned without Tribute (ATK/DEF halved).
        Debug.Log("Fusilier Dragon: Opção de invocação sem tributo.");
    }

    void Effect_0707_FusionGate(CardDisplay source)
    {
        GameManager.Instance.BeginFusionSummon(source); 
    }

    void Effect_0708_FusionRecovery(CardDisplay source)
    {
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        CardData poly = gy.Find(c => c.name == "Polymerization");
        List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
        
        if (poly == null || monsters.Count == 0)
        {
            UIManager.Instance.ShowMessage("Requer 'Polymerization' e pelo menos 1 Monstro no Cemitério.");
            return;
        }

        GameManager.Instance.OpenCardSelection(monsters, "Recuperar Material", (material) => {
            gy.Remove(poly);
            gy.Remove(material);
            GameManager.Instance.AddCardToHand(poly, source.isPlayerCard);
            GameManager.Instance.AddCardToHand(material, source.isPlayerCard);
            Debug.Log($"Fusion Recovery: Recuperou Polymerization e {material.name}.");
        });
    }

    void Effect_0709_FusionSage(CardDisplay source)
    {
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (!deck.Exists(c => c.name == "Polymerization"))
        {
            UIManager.Instance.ShowMessage("Você não possui 'Polymerization' no Deck.");
            return;
        }
        Effect_SearchDeck(source, "Polymerization");
    }

    void Effect_0710_FusionSwordMurasameBlade(CardDisplay source)
    {
        bool hasWarrior = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.race == "Warrior") hasWarrior = true;
        }

        if (!hasWarrior)
        {
            UIManager.Instance.ShowMessage("Não há monstros 'Warrior' face-up no campo.");
            return;
        }
        Effect_Equip(source, 800, 0, "Warrior");
    }

    void Effect_0711_FusionWeapon(CardDisplay source)
    {
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.type.Contains("Fusion") && m.CurrentCardData.level <= 6) hasTarget = true;
        }

        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros de Fusão Nível 6 ou menor face-up.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Fusion") && t.CurrentCardData.level <= 6 && !t.isFlipped,
                (t) => {
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, 1500, source));
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, 1500, source));
                    GameManager.Instance.CreateCardLink(source, t, CardLink.LinkType.Equipment);
                    Debug.Log("Fusion Weapon: Equipado.");
                }
            );
        }
    }

    void Effect_0715_GaiaPower(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Earth");
    }

    void Effect_0716_GaiaSoul(CardDisplay source)
    {
        List<CardDisplay> pyros = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null) CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, pyros);
        
        var validTributes = pyros.FindAll(m => m != source && m.CurrentCardData.race == "Pyro");
        if (validTributes.Count > 0 && UIManager.Instance != null) {
            List<CardData> pData = validTributes.Select(m => m.CurrentCardData).ToList();
            int max = Mathf.Min(2, pData.Count);
            GameManager.Instance.OpenCardMultiSelection(pData, "Tributar até 2 Pyros", 1, max, (selected) => {
                foreach(var s in selected) {
                    var m = validTributes.Find(x => x.CurrentCardData == s);
                    if (m != null) GameManager.Instance.TributeCard(m);
                }
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, selected.Count * 1000, source));
            });
        }
    }

    void Effect_0719_GaleDogra(CardDisplay source)
    {
        if (GameManager.Instance.playerLP <= 3000)
        {
            UIManager.Instance.ShowMessage("Pontos de Vida insuficientes (Requer mais de 3000).");
            return;
        }

        List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
        if (extra.Count == 0)
        {
            UIManager.Instance.ShowMessage("Seu Extra Deck está vazio.");
            return;
        }

        if (Effect_PayLP(source, 3000))
        {
            GameManager.Instance.OpenCardSelection(extra, "Enviar ao GY", (selected) => {
                extra.Remove(selected);
                GameManager.Instance.SendToGraveyard(selected, source.isPlayerCard);
                Debug.Log($"Gale Dogra: {selected.name} enviado ao GY.");
            });
        }
    }

    void Effect_0720_GaleLizard(CardDisplay source)
    {
        Effect_FlipReturn(source, TargetType.Monster);
    }

    void Effect_0721_Gamble(CardDisplay source)
    {
        if (GameManager.Instance.GetOpponentHandData().Count < 6 || GameManager.Instance.GetPlayerHandData().Count > 2)
        {
            UIManager.Instance.ShowMessage("O oponente deve ter 6+ cartas e você 2 ou menos.");
            return;
        }

        GameManager.Instance.TossCoin(1, (heads) => {
            if (heads == 1)
            {
                Debug.Log("Gamble: Cara! Compra 5.");
                for(int i=0; i<5; i++) GameManager.Instance.DrawCard();
            }
            else
            {
                Debug.Log("Gamble: Coroa! Pula próximo turno.");
                if (PhaseManager.Instance != null) PhaseManager.Instance.RegisterSkipNextPhase(true, GamePhase.Draw); // Pularia turno inteiro, simplificado
            }
        });
    }

    void Effect_0725_GarmaSwordOath(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    void Effect_0728_GarudaTheWindSpirit(CardDisplay source)
    {
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> winds = gy.FindAll(c => c.attribute == "Wind");
            
            if (winds.Count == 0)
            {
                UIManager.Instance.ShowMessage("Você precisa de 1 monstro WIND no Cemitério.");
                return;
            }

            if (winds.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(winds, "Banir 1 WIND", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    gy.Remove(selected);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
        else
        {
            Debug.Log("Garuda: Efeito de mudança de posição (End Phase do oponente).");
        }
    }

    void Effect_0731_GateGuardian(CardDisplay source)
    {
        if (!source.isOnField)
        {
            bool hasSanga = GameManager.Instance.IsCardActiveOnField("1586") || GameManager.Instance.IsCardActiveOnField("Sanga of the Thunder");
            bool hasKazejin = GameManager.Instance.IsCardActiveOnField("1008") || GameManager.Instance.IsCardActiveOnField("Kazejin");
            bool hasSuijin = GameManager.Instance.IsCardActiveOnField("1788") || GameManager.Instance.IsCardActiveOnField("Suijin");
            
            if (hasSanga && hasKazejin && hasSuijin)
            {
                List<CardDisplay> toTribute = new List<CardDisplay>();
                if (GameManager.Instance.duelFieldUI != null) CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toTribute);
                
                foreach(var m in toTribute.ToList()) {
                    string id = m.CurrentCardData.id;
                    if (id == "1586" || id == "1008" || id == "1788") GameManager.Instance.TributeCard(m);
                }

                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
            }
            else { UIManager.Instance.ShowMessage("Requer Sanga, Kazejin e Suijin no campo."); }
        }
    }

    void Effect_0733_GatherYourMind(CardDisplay source)
    {
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (!deck.Exists(c => c.name == "Gather Your Mind"))
        {
            UIManager.Instance.ShowMessage("Você não possui outra cópia de 'Gather Your Mind' no Deck.");
            return;
        }
        Effect_SearchDeck(source, "Gather Your Mind");
    }

    void Effect_0734_GatlingDragon(CardDisplay source)
    {
        if (source.hasUsedEffectThisTurn)
        {
            UIManager.Instance.ShowMessage("Efeito já ativado neste turno.");
            return;
        }

        GameManager.Instance.TossCoin(3, (heads) => {
            if (heads > 0)
            {
                Debug.Log($"Gatling Dragon: {heads} caras. Destruindo {heads} monstros.");
            }
        });
        source.hasUsedEffectThisTurn = true;
    }

    void Effect_0736_GearGolemTheMovingFortress(CardDisplay source)
    {
        if (Effect_PayLP(source, 800))
        {
            source.canAttackDirectly = true;
        }
    }

    void Effect_0737_GearfriedTheIronKnight(CardDisplay source)
    {
        // If equipped, destroy equip card.
        // Lógica passiva/trigger ao ser equipado.
        Debug.Log("Gearfried: Destrói equipamentos automaticamente.");
    }

    void Effect_0738_GearfriedTheSwordmaster(CardDisplay source)
    {
        // If equipped, destroy 1 monster opp controls.
        // Trigger ao ser equipado.
        Debug.Log("Gearfried Swordmaster: Destrói monstro ao equipar.");
    }

    void Effect_0740_GeminiImps(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null)
        {
            GameManager.Instance.DiscardCard(source);
            NegateAndDestroy(source, link);
            GameManager.Instance.DrawCard();
            Debug.Log("Gemini Imps: Negou descarte e comprou 1 carta.");
        }
    }

    void Effect_0742_GermInfection(CardDisplay source)
    {
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.CurrentCardData.race != "Machine") hasTarget = true;
        }

        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros Não-Máquina face-up no campo para equipar.");
            return;
        }

        Effect_Equip(source, 0, 0);
    }

    void Effect_0743_Gernia(CardDisplay source)
    {
        // If destroyed by effect: SS next Standby.
        Debug.Log("Gernia: Renasce na próxima Standby.");
    }

    void Effect_0744_GetsuFuhma(CardDisplay source)
    {
        // Destroy Fiend/Zombie battled.
        // Lógica no OnBattleEnd.
        Debug.Log("Getsu Fuhma: Destrói Fiend/Zombie após batalha.");
    }

    void Effect_0745_GhostKnightOfJackal(CardDisplay source)
    {
        // SS opp monster destroyed by battle to your field in Def.
        // Lógica no OnBattleEnd.
        Debug.Log("Ghost Knight: Rouba monstro destruído.");
    }

    void Effect_0747_GiantAxeMummy(CardDisplay source)
    {
        // Flip face-down once per turn. If attacked face-down and ATK < DEF, destroy attacker.
        Effect_TurnSet(source);
        // Lógica de destruição no BattleManager (ResolveDamage).
    }

    void Effect_0749_GiantGerm(CardDisplay source)
    {
        Effect_DirectDamage(source, 500);
        
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> germs = deck.FindAll(c => c.name == "Giant Germ");
        
        if (germs.Count > 0)
        {
            int max = Mathf.Min(2, germs.Count);
            for(int i=0; i<max; i++)
            {
                GameManager.Instance.SpecialSummonFromData(germs[i], source.isPlayerCard);
                deck.Remove(germs[i]);
            }
            GameManager.Instance.ShuffleDeck(source.isPlayerCard);
        }
    }

    void Effect_0750_GiantKozaky(CardDisplay source)
    {
        if (!GameManager.Instance.IsCardActiveOnField("1030")) // Kozaky
        {
            Debug.Log("Giant Kozaky: Sem Kozaky. Auto-destruição.");
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            
            if (source.isPlayerCard) GameManager.Instance.DamagePlayer(2500);
            else GameManager.Instance.DamageOpponent(2500);
        }
    }

    void Effect_0752_GiantOrc(CardDisplay source)
    {
        // Changes to Defense Position at the end of the Battle Phase.
        // Lógica no OnPhaseStart (End of Battle) ou OnBattleEnd.
        Debug.Log("Giant Orc: Vira defesa após atacar (Lógica passiva).");
    }

    void Effect_0753_GiantRat(CardDisplay source)
    {
        // When destroyed by battle: SS 1 Earth monster with ATK <= 1500 from Deck.
        Effect_SearchDeck(source, "Earth", "Monster", 1500); // Simplificado para busca, idealmente SS
    }

    void Effect_0757_GiantTrunade(CardDisplay source)
    {
        List<CardDisplay> toReturn = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, toReturn);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, toReturn);
            CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell, GameManager.Instance.duelFieldUI.opponentFieldSpell }, toReturn);
        }

        if (toReturn.Count == 0)
        {
            UIManager.Instance.ShowMessage("Não há Magias/Armadilhas no campo.");
            return;
        }
        
        foreach(var c in toReturn)
        {
            GameManager.Instance.ReturnToHand(c);
        }
        Debug.Log("Giant Trunade: Todas as S/T retornadas para a mão.");
    }

    void Effect_0759_GiftOfTheMysticalElf(CardDisplay source)
    {
        // Gain 300 LP for each monster on the field.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null) {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) count++;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) count++;
        }
        Effect_GainLP(source, count * 300);
    }

    void Effect_0760_GiftOfTheMartyr(CardDisplay source)
    {
        int monsterCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0) monsterCount++;

        if (monsterCount < 2)
        {
            UIManager.Instance.ShowMessage("Você precisa de pelo menos 2 monstros no campo (1 para tributar, 1 para receber).");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (tribute) => {
                    int atk = tribute.originalAtk;
                    GameManager.Instance.TributeCard(tribute);
                    
                    SpellTrapManager.Instance.StartTargetSelection(
                        (target) => target.isOnField && target.isPlayerCard && target.CurrentCardData.type.Contains("Monster"),
                        (target) => {
                            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, atk, source));
                            Debug.Log($"Gift of the Martyr: +{atk} ATK para {target.CurrentCardData.name}.");
                        }
                    );
                }
            );
        }
    }

    void Effect_0763_Gigantes(CardDisplay source)
    {
        if (!source.isOnField)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> earths = gy.FindAll(c => c.attribute == "Earth");
            
            if (earths.Count == 0)
            {
                UIManager.Instance.ShowMessage("Você precisa de 1 monstro EARTH no Cemitério.");
                return;
            }

            if (earths.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(earths, "Banir 1 EARTH", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    gy.Remove(selected);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
        }
        else
        {
            Debug.Log("Gigantes: Efeito de destruição S/T configurado.");
        }
    }

    void Effect_0767_Gilasaurus(CardDisplay source)
    {
        if (!source.isOnField)
        {
            GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
            GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
            Debug.Log("Gilasaurus: Invocado. (Oponente pode invocar do GY - Pendente).");
        }
    }

    void Effect_0768_GilfordTheLightning(CardDisplay source)
    {
        if (source.summonedThisTurn && source.isTributeSummoned && source.tributeCount >= 3)
        {
            Debug.Log("Gilford the Lightning: Destruindo monstros do oponente.");
            DestroyAllMonsters(true, false);
        }
    }

    void Effect_0771_GoblinAttackForce(CardDisplay source)
    {
        // Changes to Defense Position at the end of the Battle Phase.
        Debug.Log("Goblin Attack Force: Vira defesa após atacar.");
    }

    void Effect_0773_GoblinEliteAttackForce(CardDisplay source)
    {
        // Changes to Defense Position at the end of the Battle Phase.
        Debug.Log("Goblin Elite Attack Force: Vira defesa após atacar.");
    }

    void Effect_0774_GoblinFan(CardDisplay source)
    {
        // Destroy Lv2 or lower Flip monster when flipped.
        Debug.Log("Goblin Fan: Ativo (Destrói Flip Lv2-).");
    }

    void Effect_0775_GoblinKing(CardDisplay source)
    {
        Debug.Log("Goblin King: Buff dinâmico movido para OnPhaseStart.");
    }

    void Effect_0776_GoblinThief(CardDisplay source)
    {
        Effect_DirectDamage(source, 500);
        Effect_GainLP(source, 500);
    }

    void Effect_0777_GoblinZombie(CardDisplay source)
    {
        // Effect 1: Battle Damage -> Mill (Handled in OnDamageDealtImpl)
        // Effect 2: Sent to GY -> Search Zombie <= 1200 DEF (Handled in OnCardSentToGraveyard)
        Debug.Log("Goblin Zombie: Efeitos passivos/gatilho configurados.");
    }

    void Effect_0778_GoblinOfGreed(CardDisplay source)
    {
        // Passive: Cannot discard for costs.
        Debug.Log("Goblin of Greed: Impede descarte como custo.");
    }

    void Effect_0779_GoblinsSecretRemedy(CardDisplay source)
    {
        Effect_GainLP(source, 600);
    }

    void Effect_0780_GoddessOfWhim(CardDisplay source)
    {
        if (source.hasUsedEffectThisTurn)
        {
            UIManager.Instance.ShowMessage("Efeito já ativado neste turno.");
            return;
        }

        GameManager.Instance.TossCoin(1, (heads) => {
            if (heads == 1)
                source.ModifyStats(source.currentAtk, 0); 
            else
                source.ModifyStats(-source.currentAtk / 2, 0); 
        });
        source.hasUsedEffectThisTurn = true;
    }

    void Effect_0781_GoddessWithTheThirdEye(CardDisplay source)
    {
        // Fusion Substitute.
        Debug.Log("Goddess with the Third Eye: Substituto de fusão.");
    }

    void Effect_0784_GolemSentry(CardDisplay source)
    {
        if (source.isFlipped)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (t) => GameManager.Instance.ReturnToHand(t)
                );
            }
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

    void Effect_0786_GoodGoblinHousekeeping(CardDisplay source)
    {
        int copies = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.name == "Good Goblin Housekeeping").Count;
        int drawCount = copies + 1;
        
        for(int i=0; i<drawCount; i++) GameManager.Instance.DrawCard(true); // Ignore limit temporarily
        
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Retornar ao Fundo do Deck", (selected) => {
                GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                GameManager.Instance.GetPlayerMainDeck().Add(selected);
                Debug.Log($"Good Goblin Housekeeping: {selected.name} retornado ao fundo.");
            });
        }
    }

    void Effect_0787_GoraTurtle(CardDisplay source)
    {
        // Monsters with ATK >= 1900 cannot attack.
        Debug.Log("Gora Turtle: Bloqueio de ataque >= 1900 (Passivo).");
    }

    void Effect_0788_GoraTurtleOfIllusion(CardDisplay source)
    {
        // Negate Spell/Trap targeting this card.
        Debug.Log("Gora Turtle of Illusion: Imune a alvos S/T.");
    }

    void Effect_0790_GorgonsEye(CardDisplay source)
    {
        // Negate effects of Defense Position monsters.
        Debug.Log("Gorgon's Eye: Nega efeitos em defesa (Passivo).");
    }

    void Effect_0791_GracefulCharity(CardDisplay source)
    {
        if (GameManager.Instance.GetPlayerMainDeck().Count < 3)
        {
            UIManager.Instance.ShowMessage("Você precisa de pelo menos 3 cartas no Deck.");
            return;
        }

        GameManager.Instance.DrawCard(true);
        GameManager.Instance.DrawCard(true);
        GameManager.Instance.DrawCard(true);
        
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Descarte 2 cartas", 2, 2, (selected) => {
                foreach (var c in selected)
                {
                    GameObject go = GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c);
                    if (go != null) GameManager.Instance.DiscardCard(go.GetComponent<CardDisplay>());
                }
            });
        }
    }

    void Effect_0792_GracefulDice(CardDisplay source)
    {
        bool hasMonster = false;
        if (GameManager.Instance.duelFieldUI != null)
            foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if (z.childCount > 0 && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) hasMonster = true;

        if (!hasMonster)
        {
            UIManager.Instance.ShowMessage("Você não controla monstros face-up.");
            return;
        }

        GameManager.Instance.TossCoin(1, (heads) => { 
             int roll = Random.Range(1, 7);
             int buff = roll * 100;
             Debug.Log($"Graceful Dice: Rolou {roll}. Buff {buff}.");
             
             if (GameManager.Instance.duelFieldUI != null)
             {
                 foreach(var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
                 {
                     if (zone.childCount > 0)
                     {
                         var m = zone.GetChild(0).GetComponent<CardDisplay>();
                         if (m != null && !m.isFlipped)
                         {
                             m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
                             m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
                         }
                     }
                 }
             }
        });
    }

    void Effect_0794_GradiusOption(CardDisplay source)
    {
        if (!source.isOnField)
        {
            bool hasGradius = GameManager.Instance.IsCardActiveOnField("Gradius") || GameManager.Instance.IsCardActiveOnField("1095");
            if (hasGradius)
            {
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                Debug.Log("Gradius' Option: Stats dinâmicos integrados ao OnPhaseStart.");
            }
            else
            {
                UIManager.Instance.ShowMessage("Requer 'Gradius' face-up no campo.");
            }
        }
    }

    void Effect_0795_Granadora(CardDisplay source)
    {
        // Summon: Gain 1000 LP. Destroy: Take 2000 damage.
        if (source.isOnField)
        {
            Effect_GainLP(source, 1000);
        }
        // Destroy logic in OnCardLeavesField
    }

    void Effect_0797_GranmargTheRockMonarch(CardDisplay source)
    {
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            bool hasTarget = false;
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> all = new List<CardDisplay>();
                CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, all);
                CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, all);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                foreach(var m in all) if (m.isFlipped) hasTarget = true;
            }

            if (!hasTarget)
            {
                Debug.Log("Granmarg: Não há cartas Setadas para destruir.");
                return;
            }

            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isFlipped,
                    (t) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                );
            }
        }
    }

    void Effect_0799_GraveLure(CardDisplay source)
    {
        if (GameManager.Instance.GetOpponentMainDeck().Count == 0)
        {
            UIManager.Instance.ShowMessage("O Deck do oponente está vazio.");
            return;
        }
        Debug.Log("Grave Lure: Revelando topo do deck oponente.");
    }

    void Effect_0800_GraveOhja(CardDisplay source)
    {
        Effect_DirectDamage(source, 300);
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0801 - 0900)
    // =========================================================================================

    void Effect_0801_GraveProtector(CardDisplay source)
    {
        Debug.Log("Grave Protector: Monstros destruídos voltam ao deck (Requer redirecionamento no BattleManager).");
    }

    void Effect_0802_GravediggerGhoul(CardDisplay source)
    {
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        if (oppGY.Count > 0)
        {
            int max = Mathf.Min(2, oppGY.Count);
            GameManager.Instance.OpenCardMultiSelection(oppGY, "Banir do Oponente", 1, max, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, !source.isPlayerCard);
                    oppGY.Remove(c);
                }
            });
        }
    }

    void Effect_0803_GravekeepersAssailant(CardDisplay source)
    {
        Debug.Log("Gravekeeper's Assailant: Efeito de ataque transferido para OnAttackDeclaredRoutine.");
    }

    void Effect_0804_GravekeepersCannonholder(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name.Contains("Gravekeeper"),
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    Effect_DirectDamage(source, 700);
                }
            );
        }
    }

    void Effect_0805_GravekeepersChief(CardDisplay source)
    {
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> targets = gy.FindAll(c => c.name.Contains("Gravekeeper") && c.type.Contains("Monster"));
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Reviver Gravekeeper", (selected) => {
                    gy.Remove(selected);
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                });
            }
        }
    }

    void Effect_0806_GravekeepersCurse(CardDisplay source)
    {
        if (source.summonedThisTurn)
        {
            Effect_DirectDamage(source, 500);
        }
    }

    void Effect_0807_GravekeepersGuard(CardDisplay source)
    {
        if (source.isFlipped && SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => GameManager.Instance.ReturnToHand(target)
            );
        }
    }

    void Effect_0808_GravekeepersServant(CardDisplay source)
    {
        Debug.Log("Gravekeeper's Servant: Custo de ataque transferido para OnAttackDeclaredRoutine.");
    }

    void Effect_0809_GravekeepersSpearSoldier(CardDisplay source)
    {
        source.hasPiercing = true;
    }

    void Effect_0810_GravekeepersSpy(CardDisplay source)
    {
        if (source.isFlipped)
        {
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> targets = deck.FindAll(c => c.name.Contains("Gravekeeper") && c.atk <= 1500 && c.type.Contains("Monster"));
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Invocar Gravekeeper", (selected) => {
                    deck.Remove(selected);
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard, true, false); 
                    GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                });
            }
        }
    }

    void Effect_0811_GravekeepersVassal(CardDisplay source)
    {
        Debug.Log("Gravekeeper's Vassal: Requer modificação no DealBattleDamage do BattleManager.");
    }

    void Effect_0812_GravekeepersWatcher(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null)
        {
            GameManager.Instance.DiscardCard(source);
            NegateAndDestroy(source, link);
        }
    }

    void Effect_0813_Graverobber(CardDisplay source)
    {
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> spells = oppGY.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Roubar Magia", (selected) => {
                oppGY.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0814_GraverobbersRetribution(CardDisplay source)
    {
        Debug.Log("Graverobber's Retribution: Efeito de dano transferido para OnPhaseStart.");
    }

    void Effect_0816_GravityAxeGrarl(CardDisplay source)
    {
        Effect_Equip(source, 500, 0);
        // A proteção de posição foi sinalizada no log original para o BattleManager.
    }

    void Effect_0817_GravityBind(CardDisplay source)
    {
        Debug.Log("Gravity Bind: Bloqueio de ataque integrado ao IsAttackPreventedByContinuousEffect.");
    }

    void Effect_0818_GrayWing(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                source.maxAttacksPerTurn = 2;
            });
        }
    }

    void Effect_0821_GreatDezard(CardDisplay source)
    {
        if (source.spellCounters >= 2)
        {
            UIManager.Instance.ShowConfirmation("Tributar Great Dezard para invocar Fushioh Richie?", () => {
                GameManager.Instance.TributeCard(source);
                Effect_SearchDeck(source, "Fushioh Richie", "Monster");
            });
        }
    }

    void Effect_0822_GreatLongNose(CardDisplay source)
    {
        Debug.Log("Great Long Nose: Efeito de pular fase transferido para OnDamageDealtImpl.");
    }

    void Effect_0823_GreatMajuGarzett(CardDisplay source)
    {
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, 4000, source));
            Debug.Log("Great Maju Garzett: ATK definido para 4000 (Simulado pela falta de histórico de tributo).");
        }
    }

    void Effect_0825_GreatMoth(CardDisplay source)
    {
        if (!source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Petit Moth",
                    (target) => {
                        GameManager.Instance.TributeCard(target);
                        GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                    }
                );
            }
        }
    }

    void Effect_0826_GreatPhantomThief(CardDisplay source)
    {
        Debug.Log("Great Phantom Thief: Efeito de dano transferido para OnDamageDealtImpl.");
    }

    void Effect_0828_Greed(CardDisplay source)
    {
        Debug.Log("Greed: Dano por compra ativado (Monitorado pela End Phase).");
    }

    void Effect_0829_GreenGadget(CardDisplay source)
    {
        if (source.summonedThisTurn)
            Effect_SearchDeck(source, "Red Gadget");
    }

    void Effect_0831_Greenkappa(CardDisplay source)
    {
        if (source.isFlipped && SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t1) => t1.isOnField && (t1.CurrentCardData.type.Contains("Spell") || t1.CurrentCardData.type.Contains("Trap")) && t1.isFlipped,
                (target1) => {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t2) => t2.isOnField && (t2.CurrentCardData.type.Contains("Spell") || t2.CurrentCardData.type.Contains("Trap")) && t2.isFlipped && t2 != target1,
                        (target2) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target1);
                            GameManager.Instance.SendToGraveyard(target1.CurrentCardData, target1.isPlayerCard);
                            Destroy(target1.gameObject);
                            
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target2);
                            GameManager.Instance.SendToGraveyard(target2.CurrentCardData, target2.isPlayerCard);
                            Destroy(target2.gameObject);
                        }
                    );
                }
            );
        }
    }

    void Effect_0832_GrenMajuDaEiza(CardDisplay source)
    {
        int removedCount = GameManager.Instance.GetPlayerRemovedCount();
        int stats = removedCount * 400;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, stats, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, stats, source));
        // Nota: O buff se atualiza dinamicamente no OnPhaseStart (End Phase).
    }

    void Effect_0834_Griggle(CardDisplay source)
    {
        Effect_GainLP(source, 3000);
        Debug.Log("Griggle: Ganhou 3000 LP.");
    }

    void Effect_0836_GroundCollapse(CardDisplay source)
    {
        if (GameManager.Instance.duelFieldUI == null) return;

        List<Transform> availableZones = new List<Transform>();
        foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) 
            if (z.childCount == 0 && !GameManager.Instance.duelFieldUI.IsZoneBlocked(z)) availableZones.Add(z);
        foreach (var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) 
            if (z.childCount == 0 && !GameManager.Instance.duelFieldUI.IsZoneBlocked(z)) availableZones.Add(z);

        int zonesToBlock = Mathf.Min(2, availableZones.Count);
        if (zonesToBlock > 0)
        {
            List<Transform> blocked = new List<Transform>();
            for (int i = 0; i < zonesToBlock; i++)
            {
                int rnd = Random.Range(0, availableZones.Count);
                Transform chosen = availableZones[rnd];
                GameManager.Instance.duelFieldUI.BlockZone(chosen);
                blocked.Add(chosen);
                availableZones.RemoveAt(rnd);
            }
            CardEffectManager.Instance.blockedZonesByCard[source] = blocked;
            Debug.Log($"Ground Collapse: {zonesToBlock} zonas bloqueadas.");
        }
    }

    void Effect_0838_GryphonWing(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.name == "Harpie's Feather Duster" && !link.isPlayerEffect)
        {
            NegateAndDestroy(source, link);
            Effect_HarpiesFeatherDuster(source); // Vira o efeito contra o oponente!
            Debug.Log("Gryphon Wing: Nega e reflete o efeito!");
        }
    }

    void Effect_0839_GryphonsFeatherDuster(CardDisplay source)
    {
        List<CardDisplay> myST = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, myST);
            CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell }, myST);
        }
        
        int count = myST.Count;
        DestroyCards(myST, true);
        Effect_GainLP(source, count * 500);
    }

    void Effect_0840_GuardianAngelJoan(CardDisplay source)
    {
        Debug.Log("Guardian Angel Joan: Efeito de cura transferido para OnBattleEnd.");
    }

    void Effect_0841_GuardianBaou(CardDisplay source)
    {
        Debug.Log("Guardian Baou: Efeito de buff de batalha transferido para OnBattleEnd.");
    }

    void Effect_0842_GuardianCeal(CardDisplay source)
    {
        if (source.isOnField)
        {
            List<CardDisplay> equips = GetEquippedCards(source);
            if (equips.Count > 0)
            {
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                        (target) => {
                            CardDisplay equipToSend = equips[0];
                            GameManager.Instance.SendToGraveyard(equipToSend.CurrentCardData, equipToSend.isPlayerCard);
                            Destroy(equipToSend.gameObject);
                            
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                        }
                    );
                }
            }
            else
            {
                UIManager.Instance.ShowMessage("Guardian Ceal não possui cartas de equipamento.");
            }
        }
    }

    void Effect_0843_GuardianElma(CardDisplay source)
    {
        if (source.summonedThisTurn)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            CardData elma = gy.Find(c => c.name == "Butterfly Dagger - Elma");
            
            if (elma != null)
            {
                Debug.Log("Guardian Elma: Equipando Butterfly Dagger do GY.");
                gy.Remove(elma);
                Effect_Equip(source, 300, 0); // Aplica stats
            }
        }
    }

    void Effect_0844_GuardianGrarl(CardDisplay source)
    {
        if (!source.isOnField)
        {
            if (GameManager.Instance.IsCardActiveOnField("Gravity Axe - Grarl") || GameManager.Instance.IsCardActiveOnField("0816"))
            {
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
            }
        }
    }

    void Effect_0845_GuardianKayest(CardDisplay source)
    {
        Debug.Log("Guardian Kay'est: Imunidade ativa.");
    }

    void Effect_0846_GuardianSphinx(CardDisplay source)
    {
        if (source.isFlipped && GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            
            foreach(var m in oppMonsters)
            {
                GameManager.Instance.ReturnToHand(m);
            }
            Debug.Log("Guardian Sphinx: Campo do oponente limpo.");
        }
        else if (!source.isFlipped && !source.hasUsedEffectThisTurn)
        {
            Effect_TurnSet(source);
            source.hasUsedEffectThisTurn = true;
        }
    }

    void Effect_0847_GuardianStatue(CardDisplay source)
    {
        if (source.isFlipped)
            Effect_FlipReturn(source, TargetType.Monster);
        else if (!source.hasUsedEffectThisTurn)
        {
            Effect_TurnSet(source);
            source.hasUsedEffectThisTurn = true;
        }
    }

    void Effect_0848_GuardianTryce(CardDisplay source)
    {
        Debug.Log("Guardian Tryce: Efeito de flutuação (Requer rastreamento de tributo).");
    }

    void Effect_0851_Gust(CardDisplay source)
    {
        Debug.Log("Gust: Gatilho transferido para OnCardSentToGraveyard.");
    }

    void Effect_0852_GustFan(CardDisplay source)
    {
        Effect_Equip(source, 400, -200, "", "Wind");
    }

    void Effect_0853_GyakuGirePanda(CardDisplay source)
    {
        source.hasPiercing = true;
        // ATK boost is dynamic, handled in OnPhaseStart
    }

    void Effect_0855_Gyroid(CardDisplay source)
    {
        Debug.Log("Gyroid: Proteção de batalha transferida para OnDamageCalculationRoutine.");
    }

    void Effect_0856_HadeHane(CardDisplay source)
    {
        if (source.isFlipped)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            }

            if (all.Count > 0 && SpellTrapManager.Instance != null)
            {
                int max = Mathf.Min(3, all.Count);
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (t1) => {
                        GameManager.Instance.ReturnToHand(t1);
                        all.Remove(t1);
                        if (all.Count > 0 && max > 1) {
                            SpellTrapManager.Instance.StartTargetSelection(
                                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                                (t2) => {
                                    GameManager.Instance.ReturnToHand(t2);
                                    all.Remove(t2);
                                    if (all.Count > 0 && max > 2) {
                                        SpellTrapManager.Instance.StartTargetSelection(
                                            (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                                            (t3) => { GameManager.Instance.ReturnToHand(t3); }
                                        );
                                    }
                                }
                            );
                        }
                    }
                );
            }
        }
    }

    void Effect_0857_HallowedLifeBarrier(CardDisplay source)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                if (BattleManager.Instance != null) BattleManager.Instance.noBattleDamageThisTurn = true; 
                Debug.Log("Hallowed Life Barrier: Dano prevenido este turno.");
            });
        }
    }

    void Effect_0858_HamburgerRecipe(CardDisplay source)
    {
        GameManager.Instance.BeginRitualSummon(source);
    }

    void Effect_0859_HammerShot(CardDisplay source)
    {
        List<CardDisplay> targets = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, targets);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, targets);
        }

        CardDisplay highestAtkTarget = null;
        int maxAtk = -1;

        foreach(var m in targets)
        {
            if (m.position == CardDisplay.BattlePosition.Attack && !m.isFlipped)
            {
                if (m.currentAtk > maxAtk)
                {
                    maxAtk = m.currentAtk;
                    highestAtkTarget = m;
                }
            }
        }

        if (highestAtkTarget != null)
        {
            Debug.Log($"Hammer Shot: Destruindo {highestAtkTarget.CurrentCardData.name}.");
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(highestAtkTarget);
            GameManager.Instance.SendToGraveyard(highestAtkTarget.CurrentCardData, highestAtkTarget.isPlayerCard);
            Destroy(highestAtkTarget.gameObject);
        }
    }

    void Effect_0860_HandOfNephthys(CardDisplay source)
    {
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != source && t.CurrentCardData.type.Contains("Monster"),
                    (tribute) => {
                        GameManager.Instance.TributeCard(source);
                        GameManager.Instance.TributeCard(tribute);
                        Effect_SpecialSummonFromDeck(source, nameContains: "Sacred Phoenix of Nephthys"); 
                    }
                );
            }
        }
    }

    void Effect_0861_HaneHane(CardDisplay source)
    {
        if (source.isFlipped && SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (target) => GameManager.Instance.ReturnToHand(target)
            );
        }
    }

    void Effect_0863_HannibalNecromancer(CardDisplay source)
    {
        if (SpellCounterManager.Instance.GetCount(source) >= 1)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Trap"),
                    (target) => {
                        SpellCounterManager.Instance.RemoveCounter(source, 1);
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                );
            }
        }
        else
        {
            UIManager.Instance.ShowMessage("Hannibal Necromancer precisa de 1 Spell Counter.");
        }
    }

    void Effect_0869_HarpieLady2(CardDisplay source)
    {
        Debug.Log("Harpie Lady 2: Nega efeitos Flip (Transferido para OnBattleEnd).");
    }

    void Effect_0870_HarpieLady3(CardDisplay source)
    {
        Debug.Log("Harpie Lady 3: Bloqueia atacante por 2 turnos (Transferido para OnBattleEnd).");
    }

    void Effect_0871_HarpieLadySisters(CardDisplay source)
    {
        Debug.Log("Harpie Lady Sisters: Invocação especial por Elegant Egotist.");
    }

    void Effect_0873_HarpiesPetDragon(CardDisplay source)
    {
        Debug.Log("Harpie's Pet Dragon: Buff de ATK dinâmico (Transferido para OnPhaseStart).");
    }

    void Effect_0874_HarpiesHuntingGround(CardDisplay source)
    {
        Effect_Field(source, 200, 200, "Winged Beast");
        Debug.Log("Harpies' Hunting Ground: Trigger de invocação movido para OnSummonImpl.");
    }

    void Effect_0875_HayabusaKnight(CardDisplay source)
    {
        source.maxAttacksPerTurn = 2;
    }

    void Effect_0877_HeartOfClearWater(CardDisplay source)
    {
        bool hasTarget = false;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (!m.isFlipped && m.currentAtk <= 1300) hasTarget = true;
        }

        if (!hasTarget)
        {
            UIManager.Instance.ShowMessage("Não há monstros com ATK 1300 ou menos para equipar.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.currentAtk <= 1300 && !t.isFlipped,
                (target) => {
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                    Debug.Log($"Heart of Clear Water: Equipado em {target.CurrentCardData.name}. Protegido de batalha e alvo.");
                }
            );
        }
    }

    void Effect_0878_HeartOfTheUnderdog(CardDisplay source)
    {
        Debug.Log("Heart of the Underdog: Gatilho de compra transferido para OnCardDrawnImpl.");
    }

    void Effect_0879_HeavyMechSupportPlatform(CardDisplay source)
    {
        Effect_Union(source, "Machine", 500, 500); 
    }

    void Effect_0880_HeavySlump(CardDisplay source)
    {
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count >= 8)
        {
            GameManager.Instance.ReturnHandToDeck(false); 
            GameManager.Instance.DrawOpponentCard();
            GameManager.Instance.DrawOpponentCard();
            Debug.Log("Heavy Slump: Mão do oponente embaralhada e comprou 2 cartas.");
        }
        else
        {
            UIManager.Instance.ShowMessage("O oponente precisa ter 8 ou mais cartas na mão.");
        }
    }

    void Effect_0882_HelpingRoboForCombat(CardDisplay source)
    {
        Debug.Log("Helping Robo for Combat: Efeito transferido para OnBattleEnd.");
    }

    void Effect_0883_Helpoemer(CardDisplay source)
    {
        Debug.Log("Helpoemer: Efeito de descarte no GY transferido para OnPhaseStart.");
    }

    void Effect_0885_HeroSignal(CardDisplay source)
    {
        Debug.Log("Hero Signal: Gatilho de destruição transferido para OnCardSentToGraveyard.");
    }

    void Effect_0888_HiddenSoldiers(CardDisplay source)
    {
        Debug.Log("Hidden Soldiers: Gatilho transferido para OnSummonImpl.");
    }

    void Effect_0889_HiddenSpellbook(CardDisplay source)
    {
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count >= 2)
        {
            GameManager.Instance.OpenCardMultiSelection(spells, "Reciclar 2 Magias", 2, 2, (selected) => {
                foreach(var c in selected)
                {
                    gy.Remove(c);
                    GameManager.Instance.GetPlayerMainDeck().Add(c);
                }
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                Debug.Log("Hidden Spellbook: 2 Magias retornadas ao Deck.");
            });
        }
        else
        {
            UIManager.Instance.ShowMessage("Você precisa de pelo menos 2 Magias no Cemitério.");
        }
    }

    void Effect_0890_Hieracosphinx(CardDisplay source)
    {
        Debug.Log("Hieracosphinx: Protege monstros face-down (Passivo).");
        if (BattleManager.Instance != null) BattleManager.Instance.cannotAttackFaceDown = true;
    }

    void Effect_0891_HieroglyphLithograph(CardDisplay source)
    {
        if (GameManager.Instance.playerLP <= 1000)
        {
            UIManager.Instance.ShowMessage("Pontos de Vida insuficientes (1000 necessários).");
            return;
        }

        if (Effect_PayLP(source, 1000))
        {
            if (GameManager.Instance != null) GameManager.Instance.handLimit = 7;
            Debug.Log("Hieroglyph Lithograph: Limite de mão aumentado para 7.");
        }
    }

    void Effect_0893_HiitaTheFireCharmer(CardDisplay source)
    {
        if (source.isFlipped && SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.attribute == "Fire" && !t.isFlipped,
                (t) => GameManager.Instance.SwitchControl(t)
            );
        }
    }

    void Effect_0894_HinoKaguTsuchi(CardDisplay source)
    {
        Debug.Log("Hino-Kagu-Tsuchi: Efeito de descarte devastador transferido para OnDamageDealtImpl.");
    }

    void Effect_0895_Hinotama(CardDisplay source)
    {
        Effect_DirectDamage(source, 500);
    }

    void Effect_0897_HirosShadowScout(CardDisplay source)
    {
        if (source.isFlipped)
        {
            for(int i=0; i<3; i++) GameManager.Instance.DrawOpponentCard();
            Debug.Log("Hiro's Shadow Scout: Oponente comprou 3 cartas. Descarte pendente para o Oponente em OnCardDrawn.");
        }
        else if (!source.hasUsedEffectThisTurn)
        {
            Effect_TurnSet(source);
            source.hasUsedEffectThisTurn = true;
        }
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0901 - 1000)
    // =========================================================================================

    void Effect_0901_HomunculusTheAlchemicBeing(CardDisplay source)
    {
        if (source.hasUsedEffectThisTurn) return;
        string[] attributes = { "Light", "Dark", "Water", "Fire", "Earth", "Wind" };
        string chosen = attributes[Random.Range(0, attributes.Length)];
        source.temporaryAttribute = chosen;
        source.hasUsedEffectThisTurn = true;
        Debug.Log($"Homunculus: Atributo alterado para {chosen}.");
    }

    void Effect_0903_HornOfHeaven(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.trigger == ChainManager.TriggerType.Summon)
        {
            if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
            {
                if (SpellTrapManager.Instance != null) {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                        (tribute) => {
                            GameManager.Instance.TributeCard(tribute);
                            NegateAndDestroy(source, link);
                        }
                    );
                }
            }
        }
    }

    void Effect_0904_HornOfLight(CardDisplay source)
    {
        Effect_Equip(source, 0, 800);
    }

    void Effect_0905_HornOfTheUnicorn(CardDisplay source)
    {
        Effect_Equip(source, 700, 700);
    }

    void Effect_0908_HorusTheBlackFlameDragonLV8(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Spell"))
        {
            NegateAndDestroy(source, link);
        }
    }

    void Effect_0909_HorusServant(CardDisplay source)
    {
        Debug.Log("Horus' Servant: Proteção de Horus ativa (Passivo).");
    }

    void Effect_0910_Hoshiningen(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Light");
    }

    void Effect_0911_HourglassOfCourage(CardDisplay source)
    {
        if (source.summonedThisTurn && !source.wasSpecialSummoned)
        {
            StatModifier mod = new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Multiply, 0, source);
            mod.multiplier = 0.5f;
            source.AddStatModifier(mod);
        }
    }

    void Effect_0913_HouseOfAdhesiveTape(CardDisplay source)
    {
        Debug.Log("House of Adhesive Tape: Gatilho transferido para OnSummonImpl.");
    }

    void Effect_0914_HowlingInsect(CardDisplay source)
    {
        Effect_SpecialSummonFromDeck(source, "Insect", "", 1500);
    }

    void Effect_0915_HugeRevolution(CardDisplay source)
    {
        // Main Phase: If you control People Running About, Oppressed People, and United Resistance: Destroy all cards in opponent's hand and field.
        bool hasPeople = GameManager.Instance.IsCardActiveOnField("People Running About") || GameManager.Instance.IsCardActiveOnField("1415");
        bool hasOppressed = GameManager.Instance.IsCardActiveOnField("Oppressed People") || GameManager.Instance.IsCardActiveOnField("1386");
        bool hasUnited = GameManager.Instance.IsCardActiveOnField("United Resistance") || GameManager.Instance.IsCardActiveOnField("2022");

        if (hasPeople && hasOppressed && hasUnited)
        {
            Debug.Log("Huge Revolution: Destruição total!");
            DestroyAllMonsters(true, false);
            Effect_HarpiesFeatherDuster(source);
            GameManager.Instance.DiscardHand(false);
        }
        else
        {
            Debug.Log("Huge Revolution: Requer o trio da revolução.");
        }
    }

    void Effect_0916_HumanWaveTactics(CardDisplay source)
    {
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> targets = deck.FindAll(c => c.level <= 2 && c.type.Contains("Normal") && c.type.Contains("Monster"));
        
        if (targets.Count > 0 && source.isPlayerCard == GameManager.Instance.isPlayerTurn)
        {
            GameManager.Instance.OpenCardSelection(targets, "Human-Wave: SS Lv2 Normal", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                deck.Remove(selected);
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    void Effect_0922_Hyena(CardDisplay source)
    {
        // Destroyed by battle: SS any number of "Hyena" from Deck.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> hyenas = deck.FindAll(c => c.name == "Hyena");
        
        if (hyenas.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(hyenas, "Invocar Hyenas", 1, hyenas.Count, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.SpecialSummonFromData(c, source.isPlayerCard);
                    deck.Remove(c);
                }
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    void Effect_0926_HyperHammerhead(CardDisplay source)
    {
        Debug.Log("Hyper Hammerhead: Efeito de bounce transferido para OnBattleEnd.");
    }

    void Effect_0927_HystericFairy(CardDisplay source)
    {
        if (SummonManager.Instance.HasEnoughTributes(2, source.isPlayerCard))
        {
            SelectTributesForEffect(2, source.isPlayerCard, (tributes) => {
                foreach (var t in tributes) GameManager.Instance.TributeCard(t);
                Effect_GainLP(source, 1000);
            });
        }
    }

    void Effect_0931_ImpenetrableFormation(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 700, source));
                    Debug.Log($"Impenetrable Formation: +700 DEF para {t.CurrentCardData.name}.");
                }
            );
        }
    }

    void Effect_0932_ImperialOrder(CardDisplay source)
    {
        Debug.Log("Imperial Order: Ativado. Magias negadas.");
    }

    void Effect_0933_InabaWhiteRabbit(CardDisplay source)
    {
        source.canAttackDirectly = true;
    }

    void Effect_0935_IndomitableFighterLeiLei(CardDisplay source)
    {
        Debug.Log("Lei Lei: Vira defesa após atacar transferido para OnBattleEnd.");
    }

    void Effect_0936_InfernalFlameEmperor(CardDisplay source)
    {
        if (source.summonedThisTurn && source.isTributeSummoned)
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> fires = gy.FindAll(c => c.attribute == "Fire");
            
            if (fires.Count > 0)
            {
                int max = Mathf.Min(5, fires.Count);
                GameManager.Instance.OpenCardMultiSelection(fires, "Banir FIRE", 1, max, (selected) => {
                    foreach(var c in selected)
                    {
                        GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                        gy.Remove(c);
                    }
                    int count = selected.Count;
                    Debug.Log($"Infernal Flame Emperor: Baniu {count}. Destruindo até {count} S/T.");
                    
                    List<CardDisplay> stToDestroy = new List<CardDisplay>();
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, stToDestroy);
                        CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.opponentFieldSpell }, stToDestroy);
                    }
                    DestroyCards(stToDestroy.Take(count).ToList(), source.isPlayerCard);
                });
            }
        }
    }

    void Effect_0937_InfernalqueenArchfiend(CardDisplay source)
    {
        Debug.Log("Infernalqueen Archfiend: Buff na Standby transferido para OnPhaseStart.");
    }

    void Effect_0938_Inferno(CardDisplay source)
    {
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

    void Effect_0939_InfernoFireBlast(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Red-Eyes B. Dragon",
                (t) => {
                    Effect_DirectDamage(source, t.originalAtk);
                    t.cannotAttackThisTurn = true; // Impede ataque usando flag limpa
                    Debug.Log($"Inferno Fire Blast: {t.originalAtk} de dano.");
                }
            );
        }
    }

    void Effect_0940_InfernoHammer(CardDisplay source)
    {
        Debug.Log("Inferno Hammer: Efeito de flip transferido para OnBattleEnd.");
    }

    void Effect_0941_InfernoTempest(CardDisplay source)
    {
        Debug.Log("Inferno Tempest: Gatilho de dano massivo transferido para OnDamageTaken.");
    }

    void Effect_0942_InfiniteCards(CardDisplay source)
    {
        if (GameManager.Instance != null) GameManager.Instance.handLimit = 99; // Effectively infinite
        Debug.Log("Infinite Cards: Limite de mão removido (Efeito Contínuo).");
    }

    void Effect_0943_InfiniteDismissal(CardDisplay source)
    {
        Debug.Log("Infinite Dismissal: Gatilho de ataque transferido para OnAttackDeclaredRoutine.");
    }

    void Effect_0944_InjectionFairyLily(CardDisplay source)
    {
        Debug.Log("Injection Fairy Lily: Efeito de pagar LP movido para OnDamageCalculation (Sistema 2).");
    }

    void Effect_0946_InsectArmorWithLaserCannon(CardDisplay source)
    {
        Effect_Equip(source, 700, 0, "Insect");
    }

    void Effect_0947_InsectBarrier(CardDisplay source)
    {
        Debug.Log("Insect Barrier: Bloqueia ataque de Insetos (Verificado em IsAttackPreventedByContinuousEffect).");
    }

    void Effect_0948_InsectImitation(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (tribute) => {
                    int targetLevel = tribute.CurrentCardData.level + 1;
                    GameManager.Instance.TributeCard(tribute);
                    Effect_SpecialSummonFromDeck(source, "Insect", "", -1, -1, targetLevel);
                }
            );
        }
    }

    void Effect_0950_InsectPrincess(CardDisplay source)
    {
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.CurrentCardData.race == "Insect" && m.position == CardDisplay.BattlePosition.Defense) m.ChangePosition();
                }
            }
        }
    }

    void Effect_0951_InsectQueen(CardDisplay source)
    {
        // Gains 200 ATK for each Insect on the field.
        // Cannot attack unless you tribute 1 monster.
        // End Phase: SS 1 Insect Monster Token if it destroyed a monster.
        int insects = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.CurrentCardData.race == "Insect") insects++;
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, insects * 200, source));
    }

    void Effect_0952_InsectSoldiersOfTheSky(CardDisplay source)
    {
        // ATK increases by 1000 when attacking a WIND monster.
        // Lógica no OnDamageCalculation.
        Debug.Log("Insect Soldiers: Buff vs WIND (Passivo).");
    }

    void Effect_0953_Inspection(CardDisplay source)
    {
        // Standby Phase: Pay 500 LP to look at 1 random card in opponent's hand.
        if (Effect_PayLP(source, 500))
        {
            List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
            if (oppHand.Count > 0)
            {
                CardData randomCard = oppHand[Random.Range(0, oppHand.Count)];
                Debug.Log($"Inspection: Carta revelada: {randomCard.name}");
                // Mostrar visualmente em um popup
            }
        }
    }

    void Effect_0954_InterdimensionalMatterTransporter(CardDisplay source)
    {
        // Target 1 face-up monster you control; banish it until the End Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    CardData targetData = t.CurrentCardData;
                    GameManager.Instance.BanishCard(t);
                    CardEffectManager.Instance.imT_BanishedCards.Add(targetData);
                    Debug.Log($"Interdimensional Matter Transporter: {targetData.name} banido temporariamente.");
                }
            );
        }
    }

    void Effect_0956_InvaderOfDarkness(CardDisplay source)
    {
        // Opponent cannot activate Quick-Play Spell Cards.
        Debug.Log("Invader of Darkness: Quick-Play Spells bloqueadas.");
    }

    void Effect_0957_InvaderOfTheThrone(CardDisplay source)
    {
        // FLIP: Select 1 opponent's monster; switch control of it and this card.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    GameManager.Instance.SwitchControl(source);
                    GameManager.Instance.SwitchControl(target);
                    Debug.Log("Invader of the Throne: Troca realizada.");
                }
            );
        }
    }

    void Effect_0958_InvasionOfFlames(CardDisplay source)
    {
        // When Normal Summoned: Trap Cards cannot be activated.
        Debug.Log("Invasion of Flames: Traps bloqueadas na invocação.");
    }

    void Effect_0959_Invigoration(CardDisplay source)
    {
        Effect_Equip(source, 400, -200, "", "Earth");
    }

    void Effect_0960_InvitationToADarkSleep(CardDisplay source)
    {
        // Summon: Select 1 monster opp controls; it cannot attack.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Invitation to a Dark Sleep: {t.CurrentCardData.name} não pode atacar.");
                    // t.canAttack = false;
                }
            );
        }
    }

    void Effect_0961_IronBlacksmithKotetsu(CardDisplay source)
    {
        // FLIP: Add 1 Equip Spell from Deck to hand.
        Effect_SearchDeck(source, "Equip", "Spell");
    }

    void Effect_0964_JadeInsectWhistle(CardDisplay source)
    {
        // Opponent selects 1 Insect from their Deck and places it on top.
        Debug.Log("Jade Insect Whistle: Oponente deve colocar Inseto no topo (Simulado).");
        // Simulação:
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        CardData insect = oppDeck.Find(c => c.race == "Insect");
        if (insect != null)
        {
            oppDeck.Remove(insect);
            oppDeck.Insert(0, insect);
            Debug.Log($"Jade Insect Whistle: {insect.name} movido para o topo.");
        }
    }

    void Effect_0965_JamBreedingMachine(CardDisplay source)
    {
        // Standby Phase: SS 1 Slime Token. No other Summons allowed.
        // Lógica no OnPhaseStart.
        Debug.Log("Jam Breeding Machine: Ativo.");
    }

    void Effect_0966_JamDefender(CardDisplay source)
    {
        // Redirect attack to "Revival Jam".
        Debug.Log("Jam Defender: Redirecionamento ativo.");
    }

    void Effect_0967_JarRobber(CardDisplay source)
    {
        // Negate Pot of Greed. You draw 1 card.
        Debug.Log("Jar Robber: Nega Pot of Greed (Requer Chain).");
    }

    void Effect_0968_JarOfGreed(CardDisplay source)
    {
        GameManager.Instance.DrawCard();
    }

    void Effect_0973_Jetroid(CardDisplay source)
    {
        // If targeted for attack: Can activate Trap from hand.
        Debug.Log("Jetroid: Permite Trap da mão se atacado.");
    }

    void Effect_0974_JigenBakudan(CardDisplay source)
    {
        if (source.isFlipped)
        {
            List<CardDisplay> myMonsters = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null) CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, myMonsters);
            
            int totalAtk = myMonsters.Sum(m => m.currentAtk);
            DestroyAllMonsters(true, true);
            
            int dmg = totalAtk / 2;
            if (source.isPlayerCard) GameManager.Instance.DamagePlayer(dmg); else GameManager.Instance.DamageOpponent(dmg);
            Effect_DirectDamage(source, dmg);
        }
        else if (!source.hasUsedEffectThisTurn)
        {
            Effect_TurnSet(source);
            source.hasUsedEffectThisTurn = true;
        }
    }

    void Effect_0975_Jinzo(CardDisplay source)
    {
        // Trap Cards cannot be activated. The effects of all Face-up Traps are negated.
        Debug.Log("Jinzo: Traps negadas.");
    }

    void Effect_0976_Jinzo7(CardDisplay source)
    {
        source.canAttackDirectly = true;
    }

    void Effect_0977_JiraiGumo(CardDisplay source)
    {
        Debug.Log("Jirai Gumo: Efeito de moeda transferido para OnAttackDeclaredRoutine.");
    }

    void Effect_0979_JowgenTheSpiritualist(CardDisplay source)
    {
        List<CardData> hand = source.isPlayerCard ? GameManager.Instance.GetPlayerHandData() : GameManager.Instance.GetOpponentHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.DiscardRandomHand(source.isPlayerCard, 1);
            
            Debug.Log("Jowgen: Descartou 1 carta. Destruindo monstros Special Summoned.");
            List<CardDisplay> toDestroy = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toDestroy);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toDestroy);
            }
            foreach(var m in toDestroy) if (m.wasSpecialSummoned) {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(m);
                GameManager.Instance.SendToGraveyard(m.CurrentCardData, m.isPlayerCard);
                Destroy(m.gameObject);
            }
        }
    }

    void Effect_0980_JowlsOfDarkDemise(CardDisplay source)
    {
        if (source.isFlipped)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (target) => GameManager.Instance.SwitchControl(target)
                );
            }
        }
        else if (!source.hasUsedEffectThisTurn)
        {
            Effect_TurnSet(source);
            source.hasUsedEffectThisTurn = true;
        }
    }

    void Effect_0982_JudgmentOfAnubis(CardDisplay source)
    {
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Spell"))
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            if (hand.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                    NegateAndDestroy(source, link);
                    
                    if (SpellTrapManager.Instance != null) {
                        SpellTrapManager.Instance.StartTargetSelection(
                            (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                            (target) => {
                                int atk = target.currentAtk;
                                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                                Destroy(target.gameObject);
                                Effect_DirectDamage(source, atk);
                            }
                        );
                    }
                });
            }
        }
    }

    void Effect_0983_JudgmentOfTheDesert(CardDisplay source)
    {
        if (BattleManager.Instance != null) BattleManager.Instance.battlePositionsLocked = true;
        Debug.Log("Judgment of the Desert: Posições travadas (integrado ao BattleManager).");
    }

    void Effect_0984_JudgmentOfThePharaoh(CardDisplay source)
    {
        if (Effect_PayLP(source, GameManager.Instance.playerLP / 2))
        {
            if (MultipleChoiceUI.Instance != null) {
                List<string> opts = new List<string> { "Bloquear Invocações do Oponente", "Bloquear Magias/Armadilhas" };
                MultipleChoiceUI.Instance.Show(opts, "Judgment of the Pharaoh:", 1, 1, (selected) => {
                    if (selected[0].Contains("Invocações")) {
                        Debug.Log("Judgment of the Pharaoh: Oponente não pode invocar (Simulado).");
                    } else {
                        Debug.Log("Judgment of the Pharaoh: Oponente não pode ativar S/T (Simulado).");
                    }
                });
            }
        }
    }

    void Effect_0985_JustDesserts(CardDisplay source)
    {
        // Inflict 500 damage to opponent for each monster they control.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null) {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) count++;
        }
        Effect_DirectDamage(source, count * 500);
    }

    void Effect_0986_KA2DesScissors(CardDisplay source)
    {
        // When destroys monster by battle and sends to GY: Inflict damage = Level * 500.
        // Lógica no OnBattleEnd.
        Debug.Log("KA-2 Des Scissors: Efeito de dano configurado.");
    }

    void Effect_0990_Kaibaman(CardDisplay source)
    {
        // Tribute this card; SS 1 Blue-Eyes White Dragon from hand.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            CardData blueEyes = hand.Find(c => c.name == "Blue-Eyes White Dragon");
            
            if (blueEyes != null)
            {
                GameManager.Instance.SpecialSummonFromData(blueEyes, source.isPlayerCard);
            }
        }
    }

    void Effect_0992_KaiserColosseum(CardDisplay source)
    {
        // If there is a monster on your side, opponent cannot place monsters if they have >= your number.
        // Lógica de restrição de invocação no SummonManager.
        Debug.Log("Kaiser Colosseum: Restrição de invocação ativa.");
    }

    void Effect_0994_KaiserGlider(CardDisplay source)
    {
        // Not destroyed by battle with same ATK. If destroyed: Return 1 monster to hand.
        // Lógica de retorno no OnCardLeavesField.
        Debug.Log("Kaiser Glider: Efeito de bounce ao ser destruído.");
    }

    void Effect_0995_KaiserSeaHorse(CardDisplay source)
    {
        // 2 Tributes for LIGHT monster.
        // Lógica no SummonManager.
        Debug.Log("Kaiser Sea Horse: Vale por 2 tributos para LIGHT.");
    }

    void Effect_0998_KaminoteBlow(CardDisplay source)
    {
        Debug.Log("Kaminote Blow: Efeito transferido para OnAttackDeclaredRoutine.");
    }
}
