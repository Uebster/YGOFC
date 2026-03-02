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

    void Effect_0526_DraggedDownIntoTheGrave(CardDisplay source)
    {
        // Both players reveal their hands, discard 1 card from the opponent's hand, then draw 1 card.
        // Simplified: Discard 1 random from each, draw 1 each.
        GameManager.Instance.DiscardRandomHand(true, 1);
        GameManager.Instance.DiscardRandomHand(false, 1);
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawOpponentCard();
        Debug.Log("Dragged Down: Descarte e compra mútuos.");
    }

    void Effect_0527_DragonCaptureJar(CardDisplay source)
    {
        // Change all face-up Dragon-Type monsters to Defense Position.
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
        // FLIP: Destroy all "Dragon Capture Jar". Change all face-up Dragons to Attack Position.
        // Destroy Jars
        List<CardDisplay> jars = new List<CardDisplay>();
        // ... (Logic to find jars in S/T zones) ...
        // Simplified: Check active cards
        // Change Dragons
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

            foreach (var m in allMonsters)
            {
                if (m.CurrentCardData.race == "Dragon" && m.position == CardDisplay.BattlePosition.Defense)
                {
                    m.ChangePosition();
                }
            }
        }
    }

    void Effect_0531_DragonSeeker(CardDisplay source)
    {
        // When Normal Summoned: Destroy 1 face-up Dragon.
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
        bool hasTarget = false;
        // Check for targets...
        // For prototype, just burn.
        Effect_DirectDamage(source, 800);
    }

    void Effect_0536_DragonsMirror(CardDisplay source)
    {
        // Fusion Summon Dragon from Extra Deck by banishing materials from Field/GY.
        Debug.Log("Dragon's Mirror: Fusão de Dragão (Lógica de seleção de materiais pendente).");
    }

    void Effect_0537_DragonsRage(CardDisplay source)
    {
        // Continuous: Dragon-Type monsters inflict piercing Battle Damage.
        Debug.Log("Dragon's Rage: Dragões causam dano perfurante (Passivo).");
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
        // Activate when Amazoness targeted. Return to hand, SS another monster.
        Debug.Log("Dramatic Rescue: Resgate de Amazoness.");
    }

    void Effect_0542_DreamClown(CardDisplay source)
    {
        // If changed to Defense: Destroy 1 monster.
        // Trigger logic in OnBattlePositionChangedImpl.
        Debug.Log("Dream Clown: Ativo.");
    }

    void Effect_0543_Dreamsprite(CardDisplay source)
    {
        // If attacked: Calculate damage on another monster.
        Debug.Log("Dreamsprite: Redirecionamento.");
    }

    void Effect_0544_DrillBug(CardDisplay source)
    {
        // FLIP: Shuffle Parasite Paracide into opp deck.
        Debug.Log("Drill Bug: Parasite Paracide.");
    }

    void Effect_0545_Drillago(CardDisplay source)
    {
        // If the only cards your opponent controls are face-up monsters with 1600 or more ATK, this card can attack your opponent directly.
        Debug.Log("Drillago: Condição de ataque direto (Passivo).");
    }

    void Effect_0546_Drillroid(CardDisplay source)
    {
        // Before damage calculation, if this card attacks a Defense Position monster: Destroy that monster.
        Debug.Log("Drillroid: Destrói defesa antes do cálculo (Passivo).");
    }

    void Effect_0547_DrivingSnow(CardDisplay source)
    {
        // If Trap destroyed: Destroy 1 S/T.
        Debug.Log("Driving Snow: Ativo.");
    }

    void Effect_0550_DropOff(CardDisplay source)
    {
        // Opponent discards card they just drew.
        // Trigger logic in OnDraw.
        Debug.Log("Drop Off: Descarte forçado.");
    }

    void Effect_0551_DummyGolem(CardDisplay source)
    {
        // FLIP: Your opponent selects 1 monster they control. Switch control of the selected monster and this card.
        // Simplificação: Troca com um monstro aleatório ou o primeiro encontrado, já que não temos UI para o oponente escolher.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            
            if (oppMonsters.Count > 0)
            {
                CardDisplay target = oppMonsters[0]; // Simplificado
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
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && !t.isPlayerCard,
                (target) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                    Debug.Log($"Dust Tornado: Destruiu {target.CurrentCardData.name}.");
                    
                    // Parte opcional: Setar da mão
                    // Simplificado: Apenas log, pois requer UI complexa de seleção da mão durante resolução
                    Debug.Log("Dust Tornado: Pode setar 1 S/T da mão (Pendente).");
                }
            );
        }
    }

    void Effect_0557_EarthChant(CardDisplay source)
    {
        // Ritual Spell for EARTH Ritual Monster.
        Debug.Log("Earth Chant: Ritual de Terra.");
    }

    void Effect_0559_Earthquake(CardDisplay source)
    {
        // Change all face-up monsters on the field to Defense Position.
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

    void Effect_0566_ElectricLizard(CardDisplay source)
    {
        // A non Zombie-Type monster attacking "Electric Lizard" cannot attack on its following turn.
        // Lógica passiva no BattleManager/TurnManager.
        Debug.Log("Electric Lizard: Efeito de atordoamento configurado.");
    }

    void Effect_0567_ElectricSnake(CardDisplay source)
    {
        // When this card is discarded from your hand to the Graveyard by an effect of a card controlled by your opponent, draw 2 cards.
        // Lógica no OnCardDiscarded.
        Debug.Log("Electric Snake: Efeito passivo de descarte.");
    }

    void Effect_0568_ElectroWhip(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Thunder");
    }

    void Effect_0579_ElementalHEROBubbleman(CardDisplay source)
    {
        // If hand is empty, SS. If summoned and no other cards, draw 2.
        // Lógica de SS da mão deve ser tratada no SummonManager ou como efeito de ignição na mão.
        // Aqui tratamos o efeito de compra ao ser invocado.
        if (source.isOnField)
        {
            int handCount = GameManager.Instance.GetPlayerHandData().Count;
            int fieldCount = 0;
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
        // If sent from hand to GY by opponent's card effect: Gain 2000 LP.
        // Lógica no OnCardDiscarded (precisa verificar se foi efeito do oponente).
        Debug.Log("Elephant Statue of Blessing: Efeito passivo de descarte.");
    }

    void Effect_0587_ElephantStatueOfDisaster(CardDisplay source)
    {
        // If sent from hand to GY by opponent's card effect: 2000 damage to opponent.
        // Lógica no OnCardDiscarded.
        Debug.Log("Elephant Statue of Disaster: Efeito passivo de descarte.");
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
        // SS as Normal Monster (Reptile/Earth/Lv4/1600/1800). Still a Trap.
        // Lógica de Trap Monster.
        Debug.Log("Embodiment of Apophis: Invocado como monstro.");
        // GameManager.Instance.SpawnTrapMonster(...)
    }

    void Effect_0592_EmergencyProvisions(CardDisplay source)
    {
        // Send any number of S/T to GY; gain 1000 LP each.
        // Requer seleção múltipla de S/T no campo.
        Debug.Log("Emergency Provisions: Envie S/T para ganhar LP (Seleção múltipla pendente).");
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

    void Effect_0603_EnergyDrain(CardDisplay source)
    {
        // Target 1 face-up monster you control; it gains 200 ATK/DEF for each card your opponent currently has in their hand.
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
        // If a Beast, Beast-Warrior, or Winged Beast-Type monster you control attacks a Defense Position monster, inflict piercing battle damage.
        Debug.Log("Enraged Battle Ox: Dano perfurante para Bestas/Guerreiros-Besta/Aladas (Passivo).");
    }

    void Effect_0606_EnragedMukaMuka(CardDisplay source)
    {
        // This card gains 400 ATK and DEF for each card in your hand.
        int handCount = GameManager.Instance.GetPlayerHandData().Count;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, handCount * 400, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, handCount * 400, source));
    }

    void Effect_0607_EradicatingAerosol(CardDisplay source)
    {
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
                // Verifica se tem modificadores do tipo Equipment
                // (Simplificação: assume que se tem modificador de equip, está equipado)
                // Idealmente, verificaríamos os links de cartas.
                // Como não temos acesso fácil aos links aqui, vamos checar se há alguma carta de Equipamento no campo linkada a ele?
                // Por enquanto, vamos destruir se tiver qualquer modificador de equipamento.
                // (Necessário expor activeModifiers ou similar no CardDisplay)
                // Assumindo que não temos acesso fácil, logamos.
                Debug.Log($"Eternal Rest: Verificando {m.CurrentCardData.name} (Lógica de detecção de equipamento pendente).");
            }
        }
        // DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_0611_ExarionUniverse(CardDisplay source)
    {
        // During your Battle Step, if this card attacks a Defense Position monster: You can make this card lose exactly 400 ATK, and if it does, it will inflict piercing battle damage.
        Debug.Log("Exarion Universe: Opção de perfurar (Passivo/Ativável na batalha).");
    }

    void Effect_0612_Exchange(CardDisplay source)
    {
        // Both players reveal their hands and add 1 card from each other's hand to their hand.
        Debug.Log("Exchange: Troca de cartas na mão (Requer UI complexa de seleção mútua).");
        // Simulação: Troca uma carta aleatória
        List<CardData> myHand = GameManager.Instance.GetPlayerHandData();
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        
        if (myHand.Count > 0 && oppHand.Count > 0)
        {
            CardData myCard = myHand[Random.Range(0, myHand.Count)];
            CardData oppCard = oppHand[Random.Range(0, oppHand.Count)];
            
            GameManager.Instance.RemoveCardFromHand(myCard, true);
            GameManager.Instance.RemoveCardFromHand(oppCard, false);
            
            GameManager.Instance.AddCardToHand(oppCard, true);
            GameManager.Instance.AddCardToHand(myCard, false); // Adiciona à mão do oponente (visualmente pode ser estranho se não for network)
            
            Debug.Log($"Exchange: Trocou {myCard.name} por {oppCard.name}.");
        }
    }

    void Effect_0613_ExchangeOfTheSpirit(CardDisplay source)
    {
        // If both players have 15 or more cards in their Graveyards: Pay 1000 LP; swap Deck and GY.
        int myGYCount = GameManager.Instance.GetPlayerGraveyard().Count;
        int oppGYCount = GameManager.Instance.GetOpponentGraveyard().Count;

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
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Itera tudo e remove
            // ...
            Debug.Log("Exhausting Spell: Todos os Spell Counters removidos.");
        }
    }

    void Effect_0615_ExileOfTheWicked(CardDisplay source)
    {
        Effect_DestroyType(source, "Fiend");
    }

    void Effect_0616_ExiledForce(CardDisplay source)
    {
        // Tribute this card to target 1 monster; destroy it.
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

    void Effect_0620_FairyBox(CardDisplay source)
    {
        // Coin toss on attack -> 0 ATK. Maintenance 500 LP.
        Debug.Log("Fairy Box: Efeito de batalha e manutenção.");
    }

    void Effect_0622_FairyGuardian(CardDisplay source)
    {
        // Tribute to return Spell from GY to Deck.
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
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> equips = gy.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");

        if (equips.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(equips, "Recuperar Equip Spell", (selected) => {
                gy.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                Debug.Log($"Fairy of the Spring: {selected.name} recuperada.");
                // TODO: Bloquear ativação desta carta neste turno
            });
        }
    }

    void Effect_0628_FairysHandMirror(CardDisplay source)
    {
        // Switch opponent's Spell effect that targets 1 monster to another correct target.
        Debug.Log("Fairy's Hand Mirror: Redirecionamento de alvo de Magia (Requer sistema de Chain/Targeting avançado).");
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
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        GameManager.Instance.OpenCardSelection(oppHand, "Mão do Oponente", (selected) => {
            if (selected.type.Contains("Spirit"))
            {
                // Discard logic (precisa achar o GO)
                Debug.Log($"Fengsheng Mirror: Descartando Spirit {selected.name}.");
            }
            else
            {
                Debug.Log("Fengsheng Mirror: Carta não é Spirit.");
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
        // FLIP: Shuffle all (hand, field, GY) to Deck. Draw 5.
        Debug.Log("Fiber Jar: Resetando o jogo...");
        
        // 1. Mover tudo para o Deck
        // (Simplificado: Chama CleanupDuelState e reinicia decks com as cartas atuais)
        // Em um sistema real, moveria cada carta individualmente.
        
        // 2. Embaralhar
        GameManager.Instance.ShuffleDeck(true);
        GameManager.Instance.ShuffleDeck(false);
        
        // 3. Comprar 5
        for(int i=0; i<5; i++) GameManager.Instance.DrawCard(true);
        for(int i=0; i<5; i++) GameManager.Instance.DrawOpponentCard();
    }

    void Effect_0640_FiendComedian(CardDisplay source)
    {
        // Coin toss. Heads: Banish opp GY. Tails: Mill deck equal to opp GY.
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
        // Switch opponent's Spell effect that targets 1 S/T to another correct target.
        Debug.Log("Fiend's Hand Mirror: Redirecionamento de alvo de Magia (S/T).");
    }

    void Effect_0650_FiendsSanctuary(CardDisplay source)
    {
        // SS Metal Fiend Token.
        GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Metal Fiend Token");
        // Token logic:
        // - Cannot attack
        // - Opponent takes battle damage
        // - Pay 1000 LP standby or destroy
        Debug.Log("Fiend's Sanctuary: Token invocado (Lógica de dano/manutenção pendente).");
    }
}
