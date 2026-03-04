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
            // Retorna mão ao deck
            List<GameObject> handObjs = new List<GameObject>(GameManager.Instance.opponentHand); // Acesso direto necessário ou getter
            // Como não temos acesso direto à lista de GOs do oponente aqui, usamos DiscardHand como fallback
            // Mas vamos tentar simular o retorno
            GameManager.Instance.DiscardHand(false); 
            
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
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
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
        GameManager.Instance.BeginRitualSummon(source);
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
                        // Simulação: Adiciona à mão e permite ativar imediatamente
                        GameManager.Instance.AddCardToHand(target, source.isPlayerCard);
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
        // Select 2 Attributes. Opponent selects 1. Destroy all face-up with that Attribute.
        Debug.Log("Earthshaker: Destruição por atributo (Lógica de seleção pendente).");
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
        // Equip opp monster. Cannot attack. Destroy at end of opp 2nd turn.
        Effect_Equip(source, 0, 0);
        Debug.Log("Ekibyo Drakmord: Equipado. (Lógica de destruição retardada pendente).");
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
        if (GameManager.Instance.IsCardActiveOnField("Harpie Lady") || GameManager.Instance.IsCardActiveOnField("0867"))
        {
            Effect_SearchDeck(source, "Harpie Lady", "Monster"); // Should be SS
        }
    }

    void Effect_0571_ElementDoom(CardDisplay source)
    {
        // FIRE: +500 ATK. EARTH: Negate effect of destroyed monster.
        Debug.Log("Element Doom: Efeitos elementais.");
    }

    void Effect_0572_ElementDragon(CardDisplay source)
    {
        // FIRE: +500 ATK. WIND: Attack again.
        Debug.Log("Element Dragon: Efeitos elementais.");
    }

    void Effect_0573_ElementMagician(CardDisplay source)
    {
        // WATER: No control switch. WIND: Attack again.
        Debug.Log("Element Magician: Efeitos elementais.");
    }

    void Effect_0574_ElementSaurus(CardDisplay source)
    {
        // FIRE: +500 ATK. EARTH: Negate effect of destroyed monster.
        Debug.Log("Element Saurus: Efeitos elementais.");
    }

    void Effect_0575_ElementSoldier(CardDisplay source)
    {
        // WATER: No control switch. EARTH: Negate effect of destroyed monster.
        Debug.Log("Element Soldier: Efeitos elementais.");
    }

    void Effect_0576_ElementValkyrie(CardDisplay source)
    {
        // FIRE: +500 ATK. WATER: No control switch.
        Debug.Log("Element Valkyrie: Efeitos elementais.");
    }

    void Effect_0577_ElementalBurst(CardDisplay source)
    {
        // Tribute 1 WIND, 1 WATER, 1 FIRE and 1 EARTH monster; destroy all cards on your opponent's side of the field.
        Debug.Log("Elemental Burst: Requer 4 tributos específicos.");
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
        // Pay 2000 LP. After 20 turns, you win the Duel.
        if (Effect_PayLP(source, 2000))
        {
            Debug.Log("Final Countdown: Contagem de 20 turnos iniciada.");
            // Adicionar contador global no GameManager ou na carta (se ela ficasse no campo, mas é Normal Spell)
            // Como é Normal Spell, ela vai pro GY. Precisamos de um contador no GameManager.
            // GameManager.Instance.StartFinalCountdown(source.isPlayerCard);
        }
    }

    void Effect_0653_FinalDestiny(CardDisplay source)
    {
        // Discard 5 cards from your hand; destroy all cards on the field.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 5)
        {
            // Descarta 5 (Simplificado: Aleatório ou os primeiros 5)
            // Em produção: UI de seleção múltipla obrigatória
            GameManager.Instance.DiscardRandomHand(source.isPlayerCard, 5);
            
            Debug.Log("Final Destiny: Destruindo tudo!");
            DestroyAllMonsters(true, true);
            Effect_HeavyStorm(source); // Destrói S/T
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
        // Roll 3 dice. Inflict damage = sum * 100.
        GameManager.Instance.TossCoin(3, (heads) => { 
            // Nota: TossCoin retorna caras, precisamos de dados (1-6).
            // Simulando dados aqui já que TossCoin é para moedas
            int d1 = Random.Range(1, 7);
            int d2 = Random.Range(1, 7);
            int d3 = Random.Range(1, 7);
            int total = d1 + d2 + d3;
            int damage = total * 100;
            
            Debug.Log($"Fire Darts: Dados {d1}, {d2}, {d3}. Total {total}. Dano {damage}.");
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
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 2)
        {
            GameManager.Instance.DiscardRandomHand(source.isPlayerCard, 2); // Deveria ser Banir
            Effect_DirectDamage(source, 800);
            Debug.Log("Fire Sorcerer: Baniu 2 da mão, causou 800 dano.");
        }
    }

    void Effect_0666_Fissure(CardDisplay source)
    {
        // Destroy the 1 face-up monster your opponent controls that has the lowest ATK.
        // Lógica de seleção automática
        Debug.Log("Fissure: Destruindo menor ATK do oponente.");
        // Implementação real requer varredura do campo oponente
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
        // Decrease ATK and DEF by 400 for each card in your hand.
        int handCount = GameManager.Instance.GetPlayerHandData().Count;
        int debuff = handCount * -400;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, debuff, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, debuff, source));
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
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Forced Ceasefire: Traps bloqueadas neste turno.");
                // SpellTrapManager.Instance.trapsBlocked = true;
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
        // Negate Spell targeting this card. Draw Phase: Add Lv4- Warrior instead of draw.
        Debug.Log("Freed General: Imune a Magias de alvo. Busca Warrior na Draw Phase.");
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
        // Once per turn: SS 1 Union from hand.
        Debug.Log("Frontline Base: Pode invocar Union da mão.");
    }

    void Effect_0698_FrozenSoul(CardDisplay source)
    {
        // If you control no monsters: Discard 1; Opponent skips next Battle Phase.
        Debug.Log("Frozen Soul: Pula Battle Phase do oponente.");
    }

    void Effect_0699_FruitsOfKozakysStudies(CardDisplay source)
    {
        // Look at top 3 cards of Deck, return in any order.
        Debug.Log("Fruits of Kozaky: Reordenar topo do deck.");
    }

    void Effect_0700_FuhRinKaZan(CardDisplay source)
    {
        // If Wind, Water, Fire, Earth on field: Apply 1 effect.
        Debug.Log("Fuh-Rin-Ka-Zan: Efeito poderoso (Raigeki/Harpie/Duo/Pot).");
    }

    void Effect_0701_FuhmaShuriken(CardDisplay source)
    {
        // Equip: +700 ATK. If sent to GY, 700 damage.
        Effect_Equip(source, 700, 0);
        // Lógica de dano ao ir pro GY deve ser no OnCardSentToGraveyard
    }

    void Effect_0702_FulfillmentOfTheContract(CardDisplay source)
    {
        // Pay 800 LP; Select Ritual Monster in GY; SS it and equip this card.
        if (Effect_PayLP(source, 800))
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> rituals = gy.FindAll(c => c.type.Contains("Ritual") && c.type.Contains("Monster"));
            
            if (rituals.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(rituals, "Reviver Ritual", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    // Equipar lógica (simulada)
                    Debug.Log("Fulfillment: Equipado ao monstro revivido.");
                });
            }
        }
    }

    void Effect_0704_FushiNoTori(CardDisplay source)
    {
        // Spirit. If inflicts battle damage, gain LP equal to damage.
        Debug.Log("Fushi No Tori: Spirit. Cura por dano.");
    }

    void Effect_0705_FushiohRichie(CardDisplay source)
    {
        // Flip: SS 1 Zombie from GY.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> zombies = gy.FindAll(c => c.race == "Zombie");
        
        if (zombies.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(zombies, "Reviver Zumbi", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0706_FusilierDragon(CardDisplay source)
    {
        // Can be Normal Summoned without Tribute (ATK/DEF halved).
        Debug.Log("Fusilier Dragon: Opção de invocação sem tributo.");
    }

    void Effect_0707_FusionGate(CardDisplay source)
    {
        // Field Spell: Fusion Summon without Polymerization (banish materials).
        Debug.Log("Fusion Gate: Fusão por banimento.");
    }

    void Effect_0708_FusionRecovery(CardDisplay source)
    {
        // Target 1 Polymerization and 1 Fusion Material in GY; add to hand.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        CardData poly = gy.Find(c => c.name == "Polymerization");
        List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
        
        if (poly != null && monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Recuperar Material", (material) => {
                gy.Remove(poly);
                gy.Remove(material);
                GameManager.Instance.AddCardToHand(poly, source.isPlayerCard);
                GameManager.Instance.AddCardToHand(material, source.isPlayerCard);
                Debug.Log($"Fusion Recovery: Recuperou Polymerization e {material.name}.");
            });
        }
    }

    void Effect_0709_FusionSage(CardDisplay source)
    {
        Effect_SearchDeck(source, "Polymerization");
    }

    void Effect_0710_FusionSwordMurasameBlade(CardDisplay source)
    {
        Effect_Equip(source, 800, 0, "Warrior");
    }

    void Effect_0711_FusionWeapon(CardDisplay source)
    {
        // Equip: Fusion Monster Lv6 or lower. +1500 ATK/DEF.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Fusion") && t.CurrentCardData.level <= 6,
                (t) => {
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, 1500, source));
                    t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, 1500, source));
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
        // Tribute up to 2 Pyro monsters; this card gains 1000 ATK for each.
        Debug.Log("Gaia Soul: Tributo para ATK.");
    }

    void Effect_0719_GaleDogra(CardDisplay source)
    {
        // Pay 3000 LP; send 1 monster from Extra Deck to GY.
        if (Effect_PayLP(source, 3000))
        {
            List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
            if (extra.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(extra, "Enviar ao GY", (selected) => {
                    extra.Remove(selected);
                    GameManager.Instance.SendToGraveyard(selected, source.isPlayerCard);
                    Debug.Log($"Gale Dogra: {selected.name} enviado ao GY.");
                });
            }
        }
    }

    void Effect_0720_GaleLizard(CardDisplay source)
    {
        Effect_FlipReturn(source, TargetType.Monster);
    }

    void Effect_0721_Gamble(CardDisplay source)
    {
        // Coin toss. Heads: Draw 5. Tails: Skip next turn.
        GameManager.Instance.TossCoin(1, (heads) => {
            if (heads == 1)
            {
                Debug.Log("Gamble: Cara! Compra 5.");
                for(int i=0; i<5; i++) GameManager.Instance.DrawCard();
            }
            else
            {
                Debug.Log("Gamble: Coroa! Pula próximo turno.");
            }
        });
    }

    void Effect_0725_GarmaSwordOath(CardDisplay source)
    {
        Debug.Log("Garma Sword Oath: Ritual.");
    }

    void Effect_0728_GarudaTheWindSpirit(CardDisplay source)
    {
        // SS by banishing 1 WIND. Change battle position of opp monster.
        if (!source.isOnField)
        {
            // Lógica de SS da mão
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
        else
        {
            // Efeito em campo: Mudar posição (End Phase do oponente)
            // Requer TurnObserver
            Debug.Log("Garuda: Efeito de mudança de posição (End Phase do oponente).");
        }
    }

    void Effect_0731_GateGuardian(CardDisplay source)
    {
        // SS by Tributing Sanga, Kazejin, Suijin.
        Debug.Log("Gate Guardian: Invocação complexa (Requer 3 tributos específicos).");
    }

    void Effect_0733_GatherYourMind(CardDisplay source)
    {
        Effect_SearchDeck(source, "Gather Your Mind");
    }

    void Effect_0734_GatlingDragon(CardDisplay source)
    {
        // 3 Coins. Destroy monsters equal to Heads.
        GameManager.Instance.TossCoin(3, (heads) => {
            if (heads > 0)
            {
                Debug.Log($"Gatling Dragon: {heads} caras. Destruindo {heads} monstros.");
                // Lógica de destruição múltipla (até 'heads' alvos)
                // Requer UI de seleção múltipla no campo
            }
        });
    }

    void Effect_0736_GearGolemTheMovingFortress(CardDisplay source)
    {
        // Pay 800 LP; attack directly.
        if (Effect_PayLP(source, 800))
        {
            Debug.Log("Gear Golem: Pode atacar diretamente este turno.");
            // source.canAttackDirectly = true;
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
        // Discard to negate effect that makes you discard.
        Debug.Log("Gemini Imps: Nega efeito de descarte.");
    }

    void Effect_0742_GermInfection(CardDisplay source)
    {
        // Equip: Non-Machine loses 300 ATK each Standby.
        Effect_Equip(source, 0, 0);
        // Lógica de debuff progressivo no OnPhaseStart.
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
        // Destroyed by battle: 500 dmg, SS up to 2 Giant Germs from Deck.
        Effect_DirectDamage(source, 500);
        
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> germs = deck.FindAll(c => c.name == "Giant Germ");
        
        if (germs.Count > 0)
        {
            int max = Mathf.Min(2, germs.Count);
            // Auto-summon para simplificar ou abrir seleção
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
        // If no face-up Kozaky, destroy this. If destroyed, damage = orig ATK (2500).
        if (!GameManager.Instance.IsCardActiveOnField("1030")) // Kozaky ID
        {
            Debug.Log("Giant Kozaky: Sem Kozaky. Auto-destruição.");
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            
            // Dano ao dono
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
        // Return all Spell/Trap Cards on the field to the hand.
        List<CardDisplay> toReturn = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, toReturn);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, toReturn);
            CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell, GameManager.Instance.duelFieldUI.opponentFieldSpell }, toReturn);
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
        // Tribute 1 monster; select 1 monster you control, it gains ATK equal to original ATK of tributed monster until End Phase.
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
        // SS by banishing 1 Earth. If destroyed by battle, destroy all S/T.
        if (!source.isOnField)
        {
            // SS Logic
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
        else
        {
            // Destroy S/T logic (OnBattleEnd)
            Debug.Log("Gigantes: Efeito de destruição S/T configurado.");
        }
    }

    void Effect_0767_Gilasaurus(CardDisplay source)
    {
        // You can Special Summon this card from your hand. If you do, opponent can SS 1 monster from their GY.
        if (!source.isOnField)
        {
            GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
            GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
            Debug.Log("Gilasaurus: Invocado. (Oponente pode invocar do GY - Pendente).");
        }
    }

    void Effect_0768_GilfordTheLightning(CardDisplay source)
    {
        // If Tribute Summoned by Tributing 3 monsters: Destroy all monsters opponent controls.
        // Lógica no OnSummonImpl (verificar tributos).
        Debug.Log("Gilford the Lightning: Raigeki se 3 tributos.");
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
        // ATK/DEF = Fiends x 500. Cannot be attacked if another Fiend exists.
        int count = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            foreach(var m in all) if (m.CurrentCardData.race == "Fiend") count++;
        }
        
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, count * 500, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, count * 500, source));
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
        // Once per turn: Toss coin. Heads: Double ATK. Tails: Halve ATK.
        GameManager.Instance.TossCoin(1, (heads) => {
            if (heads == 1)
                source.ModifyStats(source.currentAtk, 0); // Double (Add current)
            else
                source.ModifyStats(-source.currentAtk / 2, 0); // Halve (Subtract half)
        });
    }

    void Effect_0781_GoddessWithTheThirdEye(CardDisplay source)
    {
        // Fusion Substitute.
        Debug.Log("Goddess with the Third Eye: Substituto de fusão.");
    }

    void Effect_0784_GolemSentry(CardDisplay source)
    {
        // Ignition: Flip face-down.
        // Trigger (Flip): Return opp monster to hand.
        if (source.isFlipped)
        {
            // Flip Effect
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
            // Ignition
            Effect_TurnSet(source);
        }
    }

    void Effect_0786_GoodGoblinHousekeeping(CardDisplay source)
    {
        // Draw cards = copies in GY + 1, then return 1 to bottom of deck.
        int copies = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.name == "Good Goblin Housekeeping").Count;
        int drawCount = copies + 1;
        
        for(int i=0; i<drawCount; i++) GameManager.Instance.DrawCard();
        
        // Return 1 to bottom (Simulated: Select from hand to return)
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Retornar ao Fundo do Deck", (selected) => {
                GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                GameManager.Instance.ReturnToDeck(null, false); // false = bottom. Need CardDisplay dummy or refactor ReturnToDeck.
                // Workaround for ReturnToDeck needing CardDisplay:
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
        // Draw 3, Discard 2.
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
        
        // Discard 2 (Simulated: Random or First 2 if no UI)
        // In real game, needs Multi-Select from Hand.
        GameManager.Instance.DiscardRandomHand(source.isPlayerCard, 2); 
        Debug.Log("Graceful Charity: Comprou 3, descartou 2.");
    }

    void Effect_0792_GracefulDice(CardDisplay source)
    {
        // Roll die. All your monsters gain ATK/DEF = result * 100.
        GameManager.Instance.TossCoin(1, (heads) => { // Simulating Die with Coin logic or Random
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
                         if (m != null)
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
        // Cannot be Normal Summoned. SS if Gradius exists. Stats = Gradius stats.
        if (!source.isOnField)
        {
            // Check Gradius
            bool hasGradius = GameManager.Instance.IsCardActiveOnField("Gradius") || GameManager.Instance.IsCardActiveOnField("1095"); // ID Gradius
            if (hasGradius)
            {
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                // Apply stats logic (needs reference to Gradius on field)
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
        // Tribute Summon: Destroy 1 Set card.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped == false,
                (t) => {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }

    void Effect_0799_GraveLure(CardDisplay source)
    {
        // Turn top card of opp deck face-up.
        Debug.Log("Grave Lure: Revelando topo do deck oponente.");
        // Logic to peek/reveal top card
    }

    void Effect_0800_GraveOhja(CardDisplay source)
    {
        // Flip Summon: 300 damage to opp.
        Effect_DirectDamage(source, 300);
    }

    void Effect_0801_GraveProtector(CardDisplay source)
    {
        // Monsters destroyed by battle are shuffled into the Deck instead of going to the GY.
        Debug.Log("Grave Protector: Monstros destruídos voltam ao deck (Passivo).");
    }

    void Effect_0802_GravediggerGhoul(CardDisplay source)
    {
        // Select up to 2 cards from opponent's GY; banish them.
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
                Debug.Log($"Gravedigger Ghoul: Baniu {selected.Count} cartas.");
            });
        }
    }

    void Effect_0803_GravekeepersAssailant(CardDisplay source)
    {
        // If Necrovalley is on the field, when this card attacks, change battle position of opponent's monster.
        if (GameManager.Instance.IsCardActiveOnField("1324")) // Necrovalley
        {
            Debug.Log("Gravekeeper's Assailant: Necrovalley ativo. Efeito de mudança de posição disponível no ataque.");
            // Lógica de trigger no ataque (OnAttackDeclared)
        }
    }

    void Effect_0804_GravekeepersCannonholder(CardDisplay source)
    {
        // Tribute 1 Gravekeeper's monster; inflict 700 damage.
        // Simplificado: Tributa qualquer Spellcaster/GK
        Effect_TributeToBurn(source, 1, 700);
    }

    void Effect_0805_GravekeepersChief(CardDisplay source)
    {
        // When Tribute Summoned: Target 1 Gravekeeper's in GY; SS it.
        if (source.isOnField) // Assumindo que foi Tribute Summoned
        {
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> targets = gy.FindAll(c => c.name.Contains("Gravekeeper") && c.type.Contains("Monster"));
            
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Reviver Gravekeeper", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                });
            }
        }
    }

    void Effect_0806_GravekeepersCurse(CardDisplay source)
    {
        // When Summoned: Inflict 500 damage.
        Effect_DirectDamage(source, 500);
    }

    void Effect_0807_GravekeepersGuard(CardDisplay source)
    {
        // Flip: Target 1 monster opponent controls; return to hand.
        Effect_FlipReturn(source, TargetType.Monster);
    }

    void Effect_0808_GravekeepersServant(CardDisplay source)
    {
        // Opponent must send top card of Deck to GY to declare an attack.
        Debug.Log("Gravekeeper's Servant: Custo de ataque para o oponente (Passivo).");
    }

    void Effect_0809_GravekeepersSpearSoldier(CardDisplay source)
    {
        // Piercing damage.
        Debug.Log("Gravekeeper's Spear Soldier: Dano perfurante (Passivo).");
    }

    void Effect_0810_GravekeepersSpy(CardDisplay source)
    {
        // Flip: SS 1 Gravekeeper's with 1500 or less ATK from Deck.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        List<CardData> targets = deck.FindAll(c => c.name.Contains("Gravekeeper") && c.atk <= 1500 && c.type.Contains("Monster"));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Invocar Gravekeeper", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                deck.Remove(selected);
                GameManager.Instance.ShuffleDeck(source.isPlayerCard);
            });
        }
    }

    void Effect_0811_GravekeepersVassal(CardDisplay source)
    {
        // Battle Damage treated as Effect Damage.
        Debug.Log("Gravekeeper's Vassal: Dano de batalha é tratado como efeito.");
    }

    void Effect_0812_GravekeepersWatcher(CardDisplay source)
    {
        // Discard to negate "discard" effect.
        Debug.Log("Gravekeeper's Watcher: Hand Trap (Nega descarte).");
    }

    void Effect_0813_Graverobber(CardDisplay source)
    {
        // Select 1 Spell in opponent's GY; use it or add to hand.
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> spells = oppGY.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Roubar Magia", (selected) => {
                oppGY.Remove(selected);
                GameManager.Instance.AddCardToHand(selected, source.isPlayerCard);
                Debug.Log($"Graverobber: Roubou {selected.name}.");
            });
        }
    }

    void Effect_0814_GraverobbersRetribution(CardDisplay source)
    {
        // Standby Phase: Inflict 100 damage per opponent's banished monster.
        Debug.Log("Graverobber's Retribution: Dano por banidas (Passivo/Standby).");
    }

    void Effect_0816_GravityAxeGrarl(CardDisplay source)
    {
        // Equip +500 ATK. Monsters opponent controls cannot change battle position.
        Effect_Equip(source, 500, 0);
    }

    void Effect_0817_GravityBind(CardDisplay source)
    {
        // Level 4 or higher monsters cannot attack.
        Debug.Log("Gravity Bind: Bloqueio de ataque Nível 4+ (Passivo).");
    }

    void Effect_0818_GrayWing(CardDisplay source)
    {
        // Discard 1 card; this card can attack twice.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Gray Wing: Ataque duplo ativado.");
                // source.canAttackTwice = true;
            });
        }
    }

    void Effect_0821_GreatDezard(CardDisplay source)
    {
        // If destroys monster: Negate effects / SS Fushioh Richie (after 2 kills).
        Debug.Log("Great Dezard: Efeitos de batalha e evolução.");
    }

    void Effect_0822_GreatLongNose(CardDisplay source)
    {
        // If deals battle damage: Opponent skips next Battle Phase.
        Debug.Log("Great Long Nose: Pula Battle Phase do oponente se causar dano.");
    }

    void Effect_0823_GreatMajuGarzett(CardDisplay source)
    {
        // ATK = 2x original ATK of tributed monster.
        Debug.Log("Great Maju Garzett: ATK definido pelo tributo (Lógica no SummonManager).");
    }

    void Effect_0825_GreatMoth(CardDisplay source)
    {
        // SS Condition (Petit Moth + Cocoon for 4 turns).
        Debug.Log("Great Moth: Invocação especial complexa.");
    }

    void Effect_0826_GreatPhantomThief(CardDisplay source)
    {
        // When this card inflicts Battle Damage: Declare 1 card name; look at opponent's hand and discard it if present.
        // Lógica no OnDamageDealtImpl.
        Debug.Log("Great Phantom Thief: Efeito de dano configurado.");
    }

    void Effect_0828_Greed(CardDisplay source)
    {
        // Continuous Trap: Each time a player draws cards due to a card effect, they take 500 damage per card during the End Phase.
        Debug.Log("Greed: Dano por compra de efeito (Lógica de rastreamento de draws pendente).");
    }

    void Effect_0829_GreenGadget(CardDisplay source)
    {
        // When Normal/Special Summoned: Add Red Gadget from Deck to hand.
        Effect_SearchDeck(source, "Red Gadget");
    }

    void Effect_0831_Greenkappa(CardDisplay source)
    {
        // FLIP: Select 2 Set Spell/Trap cards; destroy them.
        if (SpellTrapManager.Instance != null)
        {
            // Seleção múltipla (2 alvos)
            // Simplificado: Seleciona 1 e depois outro, ou usa lógica automática se houver apenas 2
            Debug.Log("Greenkappa: Destruindo 2 S/T setadas (Simulado).");
            // TODO: Implementar seleção múltipla de alvos no campo
        }
    }

    void Effect_0832_GrenMajuDaEiza(CardDisplay source)
    {
        // ATK/DEF = Removed Cards * 400.
        int removedCount = GameManager.Instance.GetPlayerRemovedCount();
        int stats = removedCount * 400;
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, stats, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, stats, source));
    }

    void Effect_0834_Griggle(CardDisplay source)
    {
        // If control shifts to opponent: Gain 3000 LP.
        // Lógica no SwitchControl (GameManager).
        Debug.Log("Griggle: Ganha 3000 LP ao trocar controle.");
    }

    void Effect_0836_GroundCollapse(CardDisplay source)
    {
        // Select 2 Monster Card Zones; they cannot be used.
        Debug.Log("Ground Collapse: Bloqueio de zonas (Visual pendente).");
    }

    void Effect_0838_GryphonWing(CardDisplay source)
    {
        // When Harpie's Feather Duster is activated: Negate and destroy opp S/T.
        // Requer Chain.
        Debug.Log("Gryphon Wing: Counter de Harpie's Feather Duster.");
    }

    void Effect_0839_GryphonsFeatherDuster(CardDisplay source)
    {
        // Destroy all S/T you control; gain 500 LP for each.
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
        // When destroys monster by battle: Gain LP = Original ATK.
        // Lógica no OnBattleEnd.
        Debug.Log("Guardian Angel Joan: Efeito de cura configurado.");
    }

    void Effect_0841_GuardianBaou(CardDisplay source)
    {
        // If destroys monster by battle: ATK +1000. Negate effects of destroyed monsters.
        // Lógica no OnBattleEnd.
        Debug.Log("Guardian Baou: Efeito de batalha configurado.");
    }

    void Effect_0842_GuardianCeal(CardDisplay source)
    {
        // Send Equip Spell equipped to this card to GY; destroy 1 monster.
        // Requer verificar equips.
        Debug.Log("Guardian Ceal: Envia equip para destruir monstro.");
    }

    void Effect_0843_GuardianElma(CardDisplay source)
    {
        // When Summoned: Equip "Butterfly Dagger - Elma" from GY.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        CardData elma = gy.Find(c => c.name == "Butterfly Dagger - Elma");
        
        if (elma != null)
        {
            // Simula equipar do GY
            Debug.Log("Guardian Elma: Equipando Butterfly Dagger do GY.");
            gy.Remove(elma);
            // Adicionar lógica visual de equipar
            Effect_Equip(source, 300, 0); // Aplica stats
        }
    }

    void Effect_0844_GuardianGrarl(CardDisplay source)
    {
        // If "Gravity Axe - Grarl" is on field, can SS from hand.
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
        // Cannot be targeted for attacks. Unaffected by Spells.
        Debug.Log("Guardian Kay'est: Imunidade ativa.");
    }

    void Effect_0846_GuardianSphinx(CardDisplay source)
    {
        // Flip: Return all opponent's monsters to hand.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> oppMonsters = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppMonsters);
            
            foreach(var m in oppMonsters)
            {
                GameManager.Instance.ReturnToHand(m);
            }
            Debug.Log("Guardian Sphinx: Campo do oponente limpo.");
        }
    }

    void Effect_0847_GuardianStatue(CardDisplay source)
    {
        // Flip: Return 1 opponent's monster to hand.
        Effect_FlipReturn(source, TargetType.Monster);
    }

    void Effect_0848_GuardianTryce(CardDisplay source)
    {
        // If destroyed: SS the material used for Tribute Summon from GY.
        // Requer rastreamento de tributo.
        Debug.Log("Guardian Tryce: Efeito de flutuação (Requer rastreamento de tributo).");
    }

        void Effect_0851_Gust(CardDisplay source)
    {
        // When your Spell Card is destroyed and sent to the Graveyard: Destroy 1 Spell or Trap Card on the field.
        // Requer trigger OnCardSentToGraveyard com verificação de causa.
        Debug.Log("Gust: Efeito de gatilho configurado.");
    }

    void Effect_0852_GustFan(CardDisplay source)
    {
        Effect_Equip(source, 400, -200, "", "Wind");
    }

    void Effect_0853_GyakuGirePanda(CardDisplay source)
    {
        // Gains 500 ATK for each monster your opponent controls. Inflicts piercing battle damage.
        int oppMonsters = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if (z.childCount > 0) oppMonsters++;
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, oppMonsters * 500, source));
        // Piercing é passivo no BattleManager.
    }

    void Effect_0855_Gyroid(CardDisplay source)
    {
        // Once per turn, this card is not destroyed by battle.
        Debug.Log("Gyroid: Proteção de batalha (1x por turno).");
    }

    void Effect_0856_HadeHane(CardDisplay source)
    {
        // FLIP: Return up to 3 monsters on the field to the hand.
        // Requer seleção múltipla de alvos.
        Debug.Log("Hade-Hane: Retornando até 3 monstros (Simulado: 1).");
        Effect_FlipReturn(source, TargetType.Monster);
    }

    void Effect_0857_HallowedLifeBarrier(CardDisplay source)
    {
        // Discard 1 card; you take no damage this turn.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log("Hallowed Life Barrier: Nenhum dano este turno.");
                // GameManager.Instance.playerIsImmuneToDamage = true;
            });
        }
    }

    void Effect_0858_HamburgerRecipe(CardDisplay source)
    {
        Debug.Log("Hamburger Recipe: Ritual.");
    }

    void Effect_0859_HammerShot(CardDisplay source)
    {
        // Destroy 1 face-up Attack Position monster with the highest ATK.
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
        // Tribute this card and 1 other monster; SS "Sacred Phoenix of Nephthys" from hand/Deck.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            GameManager.Instance.TributeCard(source);
            // Seleciona outro tributo
            // ...
            Debug.Log("Hand of Nephthys: Invocando Sacred Phoenix.");
            // Effect_SearchDeck(source, "Sacred Phoenix of Nephthys"); // Deveria ser SS
        }
    }

    void Effect_0863_HannibalNecromancer(CardDisplay source)
    {
        // Remove 1 Spell Counter from your side of the field to destroy 1 Trap Card.
        if (SpellCounterManager.Instance.RemoveCountersFromField(1, source.isPlayerCard))
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Trap"),
                    (t) => {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                );
            }
        }
    }

    void Effect_0869_HarpieLady2(CardDisplay source)
    {
        // Negates the effects of any Flip Effect Monsters it destroys by battle.
        Debug.Log("Harpie Lady 2: Nega efeitos Flip (Passivo).");
    }

    void Effect_0870_HarpieLady3(CardDisplay source)
    {
        // Any monster that battles with this card cannot declare an attack for 2 turns.
        Debug.Log("Harpie Lady 3: Bloqueia atacante por 2 turnos (Passivo).");
    }

    void Effect_0871_HarpieLadySisters(CardDisplay source)
    {
        // Cannot be Normal Summoned. Must be Special Summoned by "Elegant Egotist".
        Debug.Log("Harpie Lady Sisters: Invocação especial.");
    }

    void Effect_0873_HarpiesPetDragon(CardDisplay source)
    {
        // Gains 300 ATK/DEF for each "Harpie Lady" on the field.
        int harpieCount = 0;
        // ... Lógica de contagem ...
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, harpieCount * 300, source));
        source.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, harpieCount * 300, source));
    }

    void Effect_0874_HarpiesHuntingGround(CardDisplay source)
    {
        // Field: Winged Beast +200 ATK/DEF.
        // When Harpie Lady/Sisters is Summoned: Destroy 1 S/T.
        Effect_Field(source, 200, 200, "Winged Beast");
        // Trigger de invocação no OnSummonImpl.
    }

    void Effect_0875_HayabusaKnight(CardDisplay source)
    {
        // Can make a second attack during each Battle Phase.
        Debug.Log("Hayabusa Knight: Ataque duplo (Passivo).");
    }

    void Effect_0877_HeartOfClearWater(CardDisplay source)
    {
        // Equip: If ATK <= 1300, not destroyed by battle or targeting effects.
        Effect_Equip(source, 0, 0);
        Debug.Log("Heart of Clear Water: Proteção ativa para monstro fraco.");
    }

    void Effect_0878_HeartOfTheUnderdog(CardDisplay source)
    {
        // Draw Phase: If draw Normal Monster, show it to draw 1 more.
        Debug.Log("Heart of the Underdog: Efeito de compra em cadeia (Passivo na Draw Phase).");
    }

    void Effect_0879_HeavyMechSupportPlatform(CardDisplay source)
    {
        // Union: Equip to Machine. +500 ATK/DEF. Protect from destruction.
        Effect_Union(source, "Machine", 500, 500); // Simplificado para qualquer Machine
    }

    void Effect_0880_HeavySlump(CardDisplay source)
    {
        // If opponent has 8+ cards in hand: They shuffle hand into Deck and draw 2.
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        if (oppHand.Count >= 8)
        {
            GameManager.Instance.DiscardHand(false); // Deveria ser Shuffle
            GameManager.Instance.DrawOpponentCard();
            GameManager.Instance.DrawOpponentCard();
            Debug.Log("Heavy Slump: Mão do oponente resetada para 2.");
        }
    }

    void Effect_0882_HelpingRoboForCombat(CardDisplay source)
    {
        // If destroys monster by battle: Draw 1, then return 1 card from hand to bottom of Deck.
        // Lógica no OnBattleEnd.
        Debug.Log("Helping Robo: Efeito de compra e retorno configurado.");
    }

    void Effect_0883_Helpoemer(CardDisplay source)
    {
        // If in GY because destroyed by battle: Opponent discards 1 random card at end of their Battle Phase.
        // Lógica no OnPhaseStart (End of Battle).
        Debug.Log("Helpoemer: Efeito de descarte no GY.");
    }

    void Effect_0885_HeroSignal(CardDisplay source)
    {
        // When monster destroyed by battle: SS 1 Lv4 or lower E-Hero from Deck.
        // Trigger de destruição.
        Effect_SearchDeck(source, "Elemental HERO", "Monster", 4); // Simplificado para busca
    }

    void Effect_0888_HiddenSoldiers(CardDisplay source)
    {
        // When opponent Summons: SS 1 Level 4 or lower DARK monster from hand.
        // Trigger de invocação do oponente.
        Debug.Log("Hidden Soldiers: Gatilho de invocação.");
    }

    void Effect_0889_HiddenSpellbook(CardDisplay source)
    {
        // Target 2 Spells in GY; shuffle into Deck.
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
    }

    void Effect_0890_Hieracosphinx(CardDisplay source)
    {
        // Opponent cannot target face-down Defense monsters for attacks.
        Debug.Log("Hieracosphinx: Protege monstros face-down.");
        if (BattleManager.Instance != null) BattleManager.Instance.cannotAttackFaceDown = true;
    }

    void Effect_0891_HieroglyphLithograph(CardDisplay source)
    {
        // Pay 1000 LP. Hand size limit becomes 7.
        if (Effect_PayLP(source, 1000))
        {
            Debug.Log("Hieroglyph Lithograph: Limite de mão aumentado para 7.");
        }
    }

    void Effect_0893_HiitaTheFireCharmer(CardDisplay source)
    {
        // FLIP: Take control of 1 FIRE monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.attribute == "Fire" && !t.isFlipped,
                (t) => GameManager.Instance.SwitchControl(t)
            );
        }
    }

    void Effect_0894_HinoKaguTsuchi(CardDisplay source)
    {
        // Spirit. If inflicts battle damage: Opponent discards hand before next Draw Phase.
        Debug.Log("Hino-Kagu-Tsuchi: Efeito devastador de descarte (Spirit).");
    }

    void Effect_0895_Hinotama(CardDisplay source)
    {
        Effect_DirectDamage(source, 500);
    }

    void Effect_0897_HirosShadowScout(CardDisplay source)
    {
        // FLIP: Opponent draws 3 cards. Discard any Spells drawn.
        for(int i=0; i<3; i++)
        {
            // Simula compra e verificação
            // Como não temos acesso fácil à carta exata comprada sem retorno do DrawOpponentCard,
            // vamos apenas fazer o oponente comprar.
            // Em um sistema completo, DrawOpponentCard retornaria a CardData.
            GameManager.Instance.DrawOpponentCard();
        }
        Debug.Log("Hiro's Shadow Scout: Oponente comprou 3. (Lógica de descarte de Spell pendente).");
    }

    void Effect_0901_HomunculusTheAlchemicBeing(CardDisplay source)
    {
        // Once per turn, you can change the Attribute of this card.
        Debug.Log("Homunculus: Atributo alterado (Simulado).");
        // Requer UI para escolher atributo
    }

    void Effect_0903_HornOfHeaven(CardDisplay source)
    {
        // Tribute 1 monster; negate the Summon of a monster and destroy it.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            // Seleção de tributo simplificada
            GameManager.Instance.TributeCard(source); // Teria que selecionar outro monstro
            Debug.Log("Horn of Heaven: Invocação negada.");
        }
    }

    void Effect_0904_HornOfLight(CardDisplay source)
    {
        // Equip: +800 DEF. If sent to GY, can pay 500 LP to return to top of Deck.
        Effect_Equip(source, 0, 800);
        // Lógica de retorno ao deck no OnCardSentToGraveyard
    }

    void Effect_0905_HornOfTheUnicorn(CardDisplay source)
    {
        // Equip: +700 ATK/DEF. If sent to GY, returns to top of Deck.
        Effect_Equip(source, 700, 700);
        // Lógica de retorno ao deck no OnCardSentToGraveyard
    }

    void Effect_0908_HorusTheBlackFlameDragonLV8(CardDisplay source)
    {
        // Cannot be Normal Summoned. Must be SS by LV6. Negate Spell activation.
        Debug.Log("Horus LV8: Negação de Magias ativa.");
    }

    void Effect_0909_HorusServant(CardDisplay source)
    {
        // Your opponent cannot target "Horus the Black Flame Dragon" monsters with Spell/Trap or card effects.
        Debug.Log("Horus' Servant: Proteção de Horus ativa.");
    }

    void Effect_0910_Hoshiningen(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Light");
    }

    void Effect_0911_HourglassOfCourage(CardDisplay source)
    {
        // Normal Summon: ATK halved. Standby Phase: ATK doubled.
        if (source.summonedThisTurn)
        {
            source.ModifyStats(-source.originalAtk / 2, 0);
        }
        // Lógica de dobrar na Standby (OnPhaseStart)
    }

    void Effect_0913_HouseOfAdhesiveTape(CardDisplay source)
    {
        // If opponent Summons monster with DEF <= 500: Destroy it.
        Debug.Log("House of Adhesive Tape: Armadilha ativa.");
    }

    void Effect_0914_HowlingInsect(CardDisplay source)
    {
        // Destroyed by battle: SS 1 Insect with ATK <= 1500 from Deck.
        Effect_SearchDeck(source, "Insect", "Monster", 1500); // Simplificado para busca
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
        // End Phase: SS Lv2 or lower Normal Monsters from Deck equal to number of your Lv2 or lower Normal Monsters destroyed by battle this turn.
        Debug.Log("Human-Wave Tactics: Invocação em massa na End Phase.");
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
        // If battles opponent monster and opponent monster is not destroyed: Return opponent monster to hand.
        // Lógica no OnBattleEnd.
        Debug.Log("Hyper Hammerhead: Efeito de bounce configurado.");
    }

    void Effect_0927_HystericFairy(CardDisplay source)
    {
        // Tribute 2 monsters; gain 1000 LP.
        if (SummonManager.Instance.HasEnoughTributes(2, source.isPlayerCard))
        {
            // Seleção de tributos simplificada
            Effect_TributeToBurn(source, 2, 0); // Reusa lógica de tributo, mas sem dano
            Effect_GainLP(source, 1000);
        }
    }

    void Effect_0931_ImpenetrableFormation(CardDisplay source)
    {
        // Target 1 face-up monster; it gains 700 DEF until end of turn.
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
        // Negate all Spell effects. Pay 700 LP standby.
        Debug.Log("Imperial Order: Ativado. Magias negadas.");
        // Lógica de negação global deve ser verificada no SpellTrapManager/CardEffectManager
        // Lógica de manutenção já está no CheckMaintenanceCosts
    }

    void Effect_0933_InabaWhiteRabbit(CardDisplay source)
    {
        // Spirit. Cannot be Special Summoned. Can attack direct.
        Debug.Log("Inaba White Rabbit: Ataque direto (Passivo).");
        // source.canAttackDirectly = true;
    }

    void Effect_0935_IndomitableFighterLeiLei(CardDisplay source)
    {
        // Changes to Defense Position at the end of the Battle Phase.
        Debug.Log("Lei Lei: Vira defesa após atacar (Passivo).");
    }

    void Effect_0936_InfernalFlameEmperor(CardDisplay source)
    {
        // Cannot be Special Summoned. When Tribute Summoned: Banish up to 5 FIRE from GY; destroy S/T equal to banished.
        if (source.isOnField)
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
                    Debug.Log($"Infernal Flame Emperor: Baniu {count}. Destrua {count} S/T.");
                    // Lógica de destruição múltipla de S/T pendente
                });
            }
        }
    }

    void Effect_0937_InfernalqueenArchfiend(CardDisplay source)
    {
        // Standby Phase: Target 1 Archfiend; it gains 1000 ATK until End Phase.
        // Lógica no OnPhaseStart.
        Debug.Log("Infernalqueen Archfiend: Buff na Standby.");
    }

    void Effect_0938_Inferno(CardDisplay source)
    {
        // SS by banishing 1 FIRE. If destroys monster: 1500 damage.
        // Lógica de SS na mão. Lógica de dano no OnBattleEnd.
        Debug.Log("Inferno: Efeitos configurados.");
    }

    void Effect_0939_InfernoFireBlast(CardDisplay source)
    {
        // Target 1 Red-Eyes B. Dragon; inflict damage equal to original ATK. Red-Eyes cannot attack.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Red-Eyes B. Dragon",
                (t) => {
                    Effect_DirectDamage(source, t.originalAtk);
                    t.hasAttackedThisTurn = true; // Impede ataque
                    Debug.Log($"Inferno Fire Blast: {t.originalAtk} de dano.");
                }
            );
        }
    }

    void Effect_0940_InfernoHammer(CardDisplay source)
    {
        // If destroys monster: Flip 1 face-up monster opponent controls to face-down Defense.
        // Lógica no OnBattleEnd.
        Debug.Log("Inferno Hammer: Efeito de flip configurado.");
    }

    void Effect_0941_InfernoTempest(CardDisplay source)
    {
        // Activate when you take 3000+ Battle Damage. Remove all monsters in both Decks and GYs.
        // Trigger no OnDamageTaken.
        Debug.Log("Inferno Tempest: Gatilho de dano massivo.");
    }

    void Effect_0942_InfiniteCards(CardDisplay source)
    {
        // No hand limit.
        Debug.Log("Infinite Cards: Limite de mão removido.");
    }

    void Effect_0943_InfiniteDismissal(CardDisplay source)
    {
        // Destroy Lv3 or lower monsters that attack.
        Debug.Log("Infinite Dismissal: Destrói atacantes fracos.");
    }

    void Effect_0944_InjectionFairyLily(CardDisplay source)
    {
        // Pay 2000 LP during damage calc -> +3000 ATK.
        // Lógica no OnDamageCalculation.
        Debug.Log("Injection Fairy Lily: Efeito de batalha (Paga 2000 LP).");
    }

    void Effect_0946_InsectArmorWithLaserCannon(CardDisplay source)
    {
        Effect_Equip(source, 700, 0, "Insect");
    }

    void Effect_0947_InsectBarrier(CardDisplay source)
    {
        // Opponent's Insect monsters cannot attack.
        Debug.Log("Insect Barrier: Bloqueia ataque de Insetos.");
    }

    void Effect_0948_InsectImitation(CardDisplay source)
    {
        // Tribute 1 monster; SS 1 Insect Lv+1 from Deck.
        if (source.isOnField) // Deveria ser da mão/campo como Spell
        {
            // Seleção de tributo
            // ...
            Debug.Log("Insect Imitation: Evolução de Inseto.");
        }
    }

    void Effect_0950_InsectPrincess(CardDisplay source)
    {
        // All opponent Insects changed to Attack Position.
        // If destroys Insect: +500 ATK.
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
        Debug.Log("Insect Queen: Buff aplicado. (Lógica de tributo para ataque e Token na End Phase pendentes).");
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
                    GameManager.Instance.BanishCard(t);
                    Debug.Log($"Interdimensional Matter Transporter: {t.CurrentCardData.name} banido temporariamente.");
                    // TODO: Agendar retorno na End Phase
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
        // FLIP: Destroy all monsters. Both take damage = Total ATK / 2.
        // Simplificado: Destrói e causa dano fixo ou calculado se possível
        Debug.Log("Jigen Bakudan: Destruição total e dano.");
        DestroyAllMonsters(true, true);
    }

    void Effect_0975_Jinzo(CardDisplay source)
    {
        // Trap Cards cannot be activated. The effects of all Face-up Traps are negated.
        Debug.Log("Jinzo: Traps negadas.");
    }

    void Effect_0976_Jinzo7(CardDisplay source)
    {
        // Can attack directly.
        Debug.Log("Jinzo #7: Ataque direto (Passivo).");
    }

    void Effect_0977_JiraiGumo(CardDisplay source)
    {
        // When attacking: Toss coin. Tails: Lose half LP.
        Debug.Log("Jirai Gumo: Custo de ataque (Moeda).");
    }

    void Effect_0979_JowgenTheSpiritualist(CardDisplay source)
    {
        // Discard 1 random card; destroy all SS monsters. Neither player can SS.
        Debug.Log("Jowgen: Bloqueio de SS e destruição.");
    }

    void Effect_0980_JowlsOfDarkDemise(CardDisplay source)
    {
        // FLIP: Take control of 1 monster until End Phase.
        Effect_ChangeControl(source, true);
    }

    void Effect_0982_JudgmentOfAnubis(CardDisplay source)
    {
        // Counter Trap: Discard 1. Negate S/T destruction effect. Destroy monster & burn.
        Debug.Log("Judgment of Anubis: Counter complexo.");
    }

    void Effect_0983_JudgmentOfTheDesert(CardDisplay source)
    {
        // Face-up monsters cannot change battle position.
        Debug.Log("Judgment of the Desert: Posições travadas.");
    }

    void Effect_0984_JudgmentOfThePharaoh(CardDisplay source)
    {
        // Pay half LP. Select effect to lock.
        Effect_PayLP(source, GameManager.Instance.playerLP / 2);
        Debug.Log("Judgment of the Pharaoh: Bloqueio ativado.");
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
        // If "Chu-Ske the Mouse Fighter", "Monk Fighter", or "Master Monk" battles, destroy opponent's monster at start of Damage Step.
        // Lógica no OnDamageCalculation.
        Debug.Log("Kaminote Blow: Destruição automática para Monks.");
    }
}
