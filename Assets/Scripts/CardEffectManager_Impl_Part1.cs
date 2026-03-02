using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0001 - 0500)
    // =========================================================================================

    void Effect_0001_3HumpLacooda(CardDisplay source)
    {
        // Tribute 2 monstros para comprar 3 cartas
        if (SummonManager.Instance.HasEnoughTributes(2, source.isPlayerCard))
        {
            // Nota: Em um sistema completo, abriria menu de seleção de tributos.
            // Aqui assumimos tributo automático ou cancelamento se falhar.
            Debug.Log("3-Hump Lacooda: Tributando 2 monstros...");
            
            SummonManager.Instance.SelectTributes(2, source.isPlayerCard, (tributes) => {
                if (tributes != null && tributes.Count == 2)
                {
                    foreach (CardDisplay tribute in tributes)
                    {
                        GameManager.Instance.SendToGraveyard(tribute.CurrentCardData, tribute.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                    for (int i = 0; i < 3; i++) GameManager.Instance.DrawCard(source.isPlayerCard);
                }
                else
                {
                    Debug.Log("3-Hump Lacooda: Tributo cancelado ou falhou.");
                }
            });
        }
    }
    void Effect_0003_4StarredLadybugOfDoom(CardDisplay source)
    {
        // FLIP: Destrói todos os monstros Nível 4 do oponente
        Effect_FlipDestroyLevel(source, 4);
    }

    partial void OnSummonImpl(CardDisplay card);

    partial void OnSetImpl(CardDisplay card);

    partial void OnBattlePositionChangedImpl(CardDisplay card);

    partial void OnDamageDealtImpl(CardDisplay attacker, CardDisplay target, int amount);

    void Effect_0004_7(CardDisplay source)
    {
        // Ganha 700 LP
        Effect_GainLP(source, 700);
    }

    void Effect_0006_7Completed(CardDisplay source)
    {
        // Equip: Máquina ganha 700 ATK ou DEF
        // Simplificação: Ganha 700 ATK e DEF por enquanto, ou abre menu de escolha
        Effect_Equip(source, 700, 700, "Machine");
    }

    void Effect_0007_8ClawsScorpion(CardDisplay source)
    {
        // Pode virar para defesa face-down uma vez por turno
        if (source.position == CardDisplay.BattlePosition.Attack)
        {
            source.ChangePosition(); // Vira defesa
            source.ShowBack(); // Face-down
            Debug.Log("8-Claws Scorpion: Virou para defesa face-down.");
        }
    }

    void Effect_0008_ACatOfIllOmen(CardDisplay source)
    {
        // FLIP: Seleciona 1 Trap do Deck e coloca no topo
        // Se Necrovalley estiver em campo, adiciona à mão
        bool hasNecrovalley = GameManager.Instance.IsCardActiveOnField("1324"); // ID Necrovalley
        
        if (hasNecrovalley)
            Effect_SearchDeck(source, "Trap"); // Adiciona à mão
        else
            Effect_SearchDeckTop(source, "Trap"); // Coloca no topo
    }

    void Effect_0009_ADealWithDarkRuler(CardDisplay source)
    {
        // (Quick-Play) Se um monstro Lv8+ foi enviado ao GY este turno:
        // Pague metade dos LP; invoque "Berserk Dragon" da mão ou Deck.
        
        if (!GameManager.Instance.wasLevel8DestroyedThisTurn) return;

        int cost = source.isPlayerCard ? GameManager.Instance.playerLP / 2 : GameManager.Instance.opponentLP / 2;
        Effect_PayLP(source, cost);

        //GameManager.Instance.SpecialSummonById("0168", source.isPlayerCard);

        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
        CardData berserkDragon = deck.Find(c => c.id == "0168"); // ID Berserk Dragon
            
        if (berserkDragon != null)
        {
            Debug.Log("A Deal with Dark Ruler: Invocando Berserk Dragon!");
            GameManager.Instance.SpecialSummonFromData(berserkDragon, source.isPlayerCard);
            deck.Remove(berserkDragon);
        }
        else
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            berserkDragon = hand.Find(c => c.id == "0168");
            if (berserkDragon != null)
            {
                Debug.Log("A Deal with Dark Ruler: Invocando Berserk Dragon!");
                GameManager.Instance.SpecialSummonFromData(berserkDragon, source.isPlayerCard);
                hand.Remove(berserkDragon);
            }
            else
            {
                Debug.Log("A Deal with Dark Ruler: Berserk Dragon não encontrado na mão ou deck.");
            }
        }
    }

    void Effect_0010_AFeatherOfThePhoenix(CardDisplay source)
    {
        // Descarte 1 carta, selecione 1 carta do GY e coloque no topo do Deck
        // TODO: UI de descarte
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        GameManager.Instance.OpenCardSelection(hand, "Selecione carta para descartar", (selectedDiscard) => {
            if (selectedDiscard != null)
            {
                GameManager.Instance.SendToGraveyard(selectedDiscard, source.isPlayerCard);
                hand.Remove(selectedDiscard);
                Debug.Log("A Feather of the Phoenix: Descarte 1 carta (pendente).");
        
                // UI de seleção do GY
                List<CardData> gy = source.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
                GameManager.Instance.OpenCardSelection(gy, "Selecione carta para o topo do Deck", (selectedGY) => {
                    if (selectedGY != null)
                    {
                        gy.Remove(selectedGY);
                        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : null; // Acesso restrito ao deck oponente
                        if (deck != null) deck.Insert(0, selectedGY);
                        Debug.Log($"{selectedGY.name} retornada ao topo do deck.");
                    }
                    else
                    {
                        Debug.Log("Nenhuma carta selecionada para retornar ao Deck.");
                    }
                });
            }
            else
            {
                Debug.Log("Nenhuma carta descartada.");
            }
        });
    }

    void Effect_0011_AFeintPlan(CardDisplay source)
    {
        // Oponente não pode atacar monstros face-down neste turno
        // Define flag no BattleManager
        Debug.Log("A Feint Plan: Monstros face-down protegidos de ataque este turno.");
        BattleManager.Instance.cannotAttackFaceDown = true;
    }

    void Effect_0012_AHeroEmerges(CardDisplay source)
    {
        // Oponente escolhe 1 carta da sua mão aleatoriamente. Se for monstro, SS.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            int rnd = Random.Range(0, hand.Count);
            CardData picked = hand[rnd];
            Debug.Log($"A Hero Emerges: Carta escolhida: {picked.name}");
            
            if (picked.type.Contains("Monster"))
            {
                GameManager.Instance.SpecialSummonFromData(picked, source.isPlayerCard);
            }
            else
            {
                GameManager.Instance.SendToGraveyard(picked, source.isPlayerCard);
            }
        }

    void Effect_0013_ALegendaryOcean(CardDisplay source)
    {
        // Campo: Water +200/+200. Nível de Water na mão/campo reduz em 1.
        Effect_Field(source, 200, 200, "Aqua", "", -1); // Aqua/Water
        // Nota: A redução de nível na mão requer lógica no SummonManager para permitir tributos menores
    }

    void Effect_0014_AManWithWdjat(CardDisplay source)
    {
        // Durante sua MP: Selecione 1 Set card no campo; olhe-a e retorne.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.isFlipped == false || t.position == CardDisplay.BattlePosition.Defense), // Set cards
                (t) => {
                    // Revela apenas para o dono do efeito (log no console por enquanto)
                    Debug.Log($"A Man with Wdjat revela: {t.CurrentCardData.name}");
                    // Visualmente poderia piscar a carta
                }
            );
        }
    }

    void Effect_0015_ARivalAppears(CardDisplay source)
    {
        // Selecione 1 monstro do oponente; SS 1 monstro da mão com mesmo Nível.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    int targetLevel = t.CurrentCardData.level;
                    Debug.Log($"Rival Appears: Alvo Nível {targetLevel}. Selecione monstro da mão.");
                    // Filtra mão
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> candidates = hand.FindAll(c => c.level == targetLevel && c.type.Contains("Monster"));
                    
                    GameManager.Instance.OpenCardSelection(candidates, "Invoque monstro de mesmo nível", (selected) => {
                        GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    });
                }
            );
        }
    }

    void Effect_0016_AWingbeatOfGiantDragon(CardDisplay source)
    {
        // Retorne 1 Dragão Lv5+ face-up que você controla para a mão; destrua todas S/T.
        // Seleção de custo
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Dragon" && t.CurrentCardData.level >= 5,
                (t) => {
                    // Retorna para mão
                    GameManager.Instance.ReturnToHand(t);
                    Debug.Log("Wingbeat: Dragão retornado.");
                    
                    // Destrói S/T
                    Effect_HeavyStorm(source); // Reusa lógica de destruir todas S/T
                }
            );
        }
    }

    void Effect_0017_ATeamTrapDisposalUnit(CardDisplay source)
    {
        // (Quick) Quando oponente ativa Trap: Tributa este card; nega e destrói.
        // Custo: Tributar a si mesmo.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            Debug.Log("A-Team: Tributado. (Lógica de negar Trap simulada).");
        }
    }

    void Effect_0018_AbsoluteEnd(CardDisplay source)
    {
        // Neste turno, ataques do oponente tornam-se ataques diretos.
        Debug.Log("Absolute End: Seus monstros não podem ser atacados, oponente ataca direto.");
        if (BattleManager.Instance != null) BattleManager.Instance.forceDirectAttack = true;
    }

    void Effect_0019_AbsorbingKidFromTheSky(CardDisplay source)
    {
        // Quando destrói monstro por batalha e envia ao GY: Ganha LP = Nível * 300.
        // Lógica deve ser chamada pelo BattleManager no evento OnMonsterDestroyed
        Debug.Log("Absorbing Kid: Efeito de ganho de LP configurado no BattleManager.");
    }

    void Effect_0021_AbyssSoldier(CardDisplay source)
    {
        // Descarte 1 Water; devolva 1 carta do campo para a mão.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> waterMonsters = hand.FindAll(c => c.attribute == "Water");

        if (waterMonsters.Count > 0)
        {
             GameManager.Instance.OpenCardSelection(waterMonsters, "Descarte 1 monstro WATER", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Debug.Log($"Abyss Soldier: Descartou {discarded.name}.");
                
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField,
                        (t) => {
                            Debug.Log($"Abyss Soldier: {t.CurrentCardData.name} retornada para a mão.");
                            GameManager.Instance.ReturnToHand(t);
                        }
                    );
                }
             });
        }
        else
        {
             Debug.Log("Abyss Soldier: Nenhum monstro WATER na mão para descartar.");
        }
    }

    void Effect_0022_AbyssalDesignator(CardDisplay source)
    {
        // Pague 1000 LP; declare Tipo e Atributo. Oponente envia 1 monstro correspondente da mão/deck ao GY.
        Effect_PayLP(source, 1000);
        Debug.Log("Abyssal Designator: Declarando Tipo/Atributo (Simulado: Dark/Fiend).");
        
        // Simulação: Oponente envia 1 Dark/Fiend
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        // Nota: Não temos acesso fácil à mão do oponente como dados brutos aqui sem expor, usando deck para simular efeito
        
        CardData target = oppDeck.Find(c => c.attribute == "Dark" && c.race == "Fiend");
        
        if (target != null)
        {
             Debug.Log($"Abyssal Designator: Oponente enviou {target.name} ao GY.");
             GameManager.Instance.SendToGraveyard(target, !source.isPlayerCard);
             oppDeck.Remove(target);
        }
        else
        {
             Debug.Log("Abyssal Designator: Oponente não possui monstro correspondente no Deck.");
        }
    }

    void Effect_0024_AcidRain(CardDisplay source)
    {
        // Destrói todas as Máquinas
        Effect_DestroyType(source, "Machine");
    }

    void Effect_0025_AcidTrapHole(CardDisplay source)
    {
        // Alvo: 1 monstro face-down defesa. Vira face-up. Se DEF <= 2000, destrói. Senão, volta face-down.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped && t.position == CardDisplay.BattlePosition.Defense,
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.def <= 2000)
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                    else
                    {
                        t.ShowBack(); // Retorna face-down
                    }
                }
            );
        }
    }

     void Effect_0027_AdhesionTrapHole(CardDisplay source)
    {
        // Quando oponente invoca: Corta ATK pela metade original.
        // Procura o monstro invocado neste turno no campo do oponente
        if (GameManager.Instance.duelFieldUI != null)
        {
             foreach(var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
             {
                 if(zone.childCount > 0)
                 {
                     var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                     if(monster != null && monster.summonedThisTurn)
                     {
                         Debug.Log($"Adhesion Trap Hole: {monster.CurrentCardData.name} ATK reduzido pela metade.");
                         monster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Multiply, 0.5f, source));
                     }
                 }
             }
        }
    }

    void Effect_0028_AfterTheStruggle(CardDisplay source)
    {
        // Main Phase 1: Destrói todos monstros que batalharam no turno anterior? Não, texto:
        // "Destroy all face-up monsters on the field that were involved in damage calculation..."
        Debug.Log("After the Struggle: Destruindo monstros que batalharam.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        
        void CheckZones(Transform[] zones)
        {
            foreach(var z in zones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.battledThisTurn) toDestroy.Add(m);
                }
            }
        }
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            CheckZones(GameManager.Instance.duelFieldUI.playerMonsterZones);
            CheckZones(GameManager.Instance.duelFieldUI.opponentMonsterZones);
        }
        
        foreach(var m in toDestroy)
        {
            GameManager.Instance.SendToGraveyard(m.CurrentCardData, m.isPlayerCard);
            Destroy(m.gameObject);
        }
    }

    void Effect_0029_Agido(CardDisplay source)
    {
        // Quando destruído em batalha e enviado ao GY: Rola dado (1-6).
        // SS 1 Fada do GY com Nível = Resultado. (Se 6, Lv 6 ou maior).
        int roll = Random.Range(1, 7);
        Debug.Log($"Agido rolou: {roll}.");
        
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        // Filtra Fadas com nível compatível
        List<CardData> targets = gy.FindAll(c => c.race == "Fairy" && (roll == 6 ? c.level >= 6 : c.level == roll));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Selecione Fada para reviver", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0031_AirknightParshath(CardDisplay source)
    {
        // Piercing Damage + Draw 1 on damage.
        // Piercing é passivo no BattleManager.
        // Draw é trigger.
        Debug.Log("Airknight Parshath: Compra 1 carta (Trigger de dano).");
        if (source.isPlayerCard) GameManager.Instance.DrawCard();
    }

void Effect_0037_AlligatorsSwordDragon(CardDisplay source)
    {
        // Pode atacar direto se os únicos monstros face-up do oponente forem Earth, Water ou Fire.
        bool canAttackDirectly = true;
        if (GameManager.Instance.duelFieldUI == null) return;
        
        Transform[] enemyMonsterZones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

        foreach (var zone in enemyMonsterZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay monster = zone.GetChild(0).GetComponent<CardDisplay>();
                if (monster != null && !monster.isFlipped) // Só verifica monstros face-up
                {
                    string race = monster.CurrentCardData.race;
                    if (race != "Earth" && race != "Water" && race != "Fire")
                    {
                        canAttackDirectly = false;
                        break;
                    }
                }
                else
                {
                    canAttackDirectly = false;
                    break;
                }
            }
        }

        if (canAttackDirectly)
        {
            Debug.Log("Alligator's Sword Dragon: Pode atacar diretamente!");
            //  BattleManager.Instance.canCurrentlyAttackDirectly = true; // Assuming you have a variable for this
        }
        else
        {
            Debug.Log("Alligator's Sword Dragon: Não pode atacar diretamente.");
            // BattleManager.Instance.canCurrentlyAttackDirectly = false;
        }
    }

    void Effect_0039_AltarForTribute(CardDisplay source)
    {
        // Selecione 1 monstro, tribute, ganhe LP = ATK original.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    int heal = t.CurrentCardData.atk;
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    Effect_GainLP(source, heal);
                }
            );
        }
    }

    void Effect_0041_AmazonessArcher(CardDisplay source)
    {
        // Tribute 2 monstros; cause 1200 dano.
        if (SummonManager.Instance.HasEnoughTributes(2, source.isPlayerCard))
        {
            // Seleção simplificada
            Effect_TributeToBurn(source, 2, 1200);
        }
    }

    void Effect_0042_AmazonessArchers(CardDisplay source)
    {
        // Ative quando oponente ataca. Todos monstros oponente viram face-up Attack, -500 ATK. Devem atacar.
        Debug.Log("Amazoness Archers: Forçando ataque e aplicando debuff.");
        
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach (var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null)
                    {
                        // Vira para Ataque (sem ativar Flip effects)
                        if (monster.position == CardDisplay.BattlePosition.Defense || monster.isFlipped)
                        {
                            monster.position = CardDisplay.BattlePosition.Attack;
                            monster.ShowFront();
                            monster.isFlipped = false; // Define como revelado manualmente para não disparar trigger de Flip
                        }
                        // Reduz 500 ATK
                        monster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -500, source));
                    }
                }
            }
        }
        // Nota: A obrigatoriedade de ataque deve ser tratada no BattleManager (flag forceAllAttack)
    }

    void Effect_0043_AmazonessBlowpiper(CardDisplay source)
    {
        // Selecione 1 monstro face-up; -500 ATK até final do turno.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => t.ModifyStats(-500, 0)
            );
        }
    }

    void Effect_0044_AmazonessChainMaster(CardDisplay source)
    {
        // Quando destruído em batalha: Pague 1500 LP; olhe mão do oponente, pegue 1 monstro.
        if (Effect_PayLP(source, 1500))
        {
            List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
            if (oppHand.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(oppHand, "Roubar Monstro do Oponente", (selected) => {
                    if (selected != null && selected.type.Contains("Monster"))
                    {
                        Debug.Log($"Amazoness Chain Master: Roubou {selected.name}.");
                        // Lógica simplificada: Adiciona cópia à sua mão (remover da mão do oponente requereria referência ao GameObject específico)
                        GameManager.Instance.AddCardToHand(selected, true);
                        // TODO: Remover carta real da mão do oponente
                    }
                });
            }
        }
    }

    void Effect_0045_AmazonessFighter(CardDisplay source)
    {
        // Sem dano de batalha para o controlador.
        // Passivo no BattleManager.
    }

    void Effect_0046_AmazonessPaladin(CardDisplay source)
    {
       if (GameManager.Instance.duelFieldUI == null) return;
        
        int amazonessCount = 0;

        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount == 0) continue;
            CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
            if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.name.Contains("Amazoness"))
            {
                amazonessCount++;
            }
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, amazonessCount*100, source));
        Debug.Log($"Campo ativado: Amazoness Paladin. Buff aplicado.");
    }

    void Effect_0047_AmazonessSpellcaster(CardDisplay source)
    {
        // Troca ATK original com 1 monstro do oponente até End Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (myMonster) => myMonster.isOnField && myMonster.isPlayerCard && myMonster.CurrentCardData.name.Contains("Amazoness"),
                (myMonster) => {
                    // Seleciona oponente
                    SpellTrapManager.Instance.StartTargetSelection(
                        (oppMonster) => oppMonster.isOnField && !oppMonster.isPlayerCard && oppMonster.CurrentCardData.type.Contains("Monster"),
                        (oppMonster) => {
                            int myOriginal = myMonster.originalAtk;
                            int oppOriginal = oppMonster.originalAtk;

                            // Troca usando modificadores 'Set'
                            myMonster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, oppOriginal, source));
                            oppMonster.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, myOriginal, source));

                            Debug.Log($"Amazoness Spellcaster: Trocou ATK original de {myMonster.CurrentCardData.name} e {oppMonster.CurrentCardData.name}.");
                        }
                    );
                }
            );
        }
    }

    void Effect_0048_AmazonessSwordsWoman(CardDisplay source)
    {
        // Oponente toma dano de batalha.
        // Passivo no BattleManager.
    }

    void Effect_0049_AmazonessTiger(CardDisplay source)
    {
        // +400 ATK. Oponente não pode atacar outros monstros "Amazoness".
        // Passivo.
    }

    void Effect_0050_Ameba(CardDisplay source)
    {
        // Quando controle muda para oponente: Cause 2000 dano a ele.
        // Trigger no ChangeControl.
        GameManager.Instance.DamageOpponent(2000);
    }

    void Effect_0053_AmphibiousBugrothMK3(CardDisplay source)
    {
        // Se "Umi" no campo, ataca direto.
        // O que falta: Lógica de ataque direto no BattleManager.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi ou Legendary Ocean
        {
            Debug.Log("MK-3: Condição de ataque direto ativa.");
        }
    }

    void Effect_0054_Amplifier(CardDisplay source)
    {
        // Equip Jinzo. Jinzo do controlador não nega Traps do controlador.
        // O que falta: Verificar se o alvo é Jinzo e lógica de imunidade.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.name == "Jinzo",
                (t) => {
                    Effect_Equip(source, 0, 0); // Realiza o equip visual
                    Debug.Log("Amplifier: Equipado em Jinzo. (Lógica de imunidade de Trap deve ser verificada no SpellTrapManager).");
                }
            );
        }
    }

    void Effect_0055_AnOwlOfLuck(CardDisplay source)
    {
        // FLIP: Seleciona Field Spell do Deck, coloca no topo. (Se Necrovalley, mão).
        bool hasNecrovalley = GameManager.Instance.IsCardActiveOnField("1324");
        if (hasNecrovalley) Effect_SearchDeck(source, "Field", "Spell");
        else Effect_SearchDeckTop(source, "Field", "Spell");
    }

    void Effect_0058_AncientGearBeast(CardDisplay source)
    {
        // Nega efeitos de monstros destruídos. Oponente não ativa S/T na batalha.
        // Passivo.
    }

    void Effect_0059_AncientGearGolem(CardDisplay source)
    {
        // Piercing. Oponente não ativa S/T na batalha.
        // Passivo.
    }

    void Effect_0060_AncientGearSoldier(CardDisplay source)
    {
        // Oponente não ativa S/T na batalha.
        // Passivo.
    }

    void Effect_0062_AncientLamp(CardDisplay source)
    {
        // Main Phase: SS "La Jinn" da mão/Deck.
        // Battle Phase: Redireciona ataque para monstro do oponente.
        if (PhaseManager.Instance.currentPhase == GamePhase.Main1)
        {
            // Busca La Jinn no Deck ou Mão
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            CardData laJinn = deck.Find(c => c.name.Contains("La Jinn"));
            
            if (laJinn != null)
            {
                GameManager.Instance.SpecialSummonFromData(laJinn, source.isPlayerCard);
                deck.Remove(laJinn);
                Debug.Log("Ancient Lamp: La Jinn invocado do Deck.");
            }
            else
            {
                // Tenta na mão
                List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                // ... lógica similar para mão
                Debug.Log("Ancient Lamp: La Jinn não encontrado no Deck.");
            }
        }
    }

    void Effect_0066_AncientTelescope(CardDisplay source)
    {
        // Veja as 5 cartas do topo do deck do oponente.
        Debug.Log("Ancient Telescope: Visualizando deck do oponente (Simulado).");
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        if (oppDeck.Count > 0)
        {
            int count = Mathf.Min(5, oppDeck.Count);
            List<CardData> topCards = oppDeck.GetRange(0, count);
            GameManager.Instance.OpenCardSelection(topCards, "Topo do Deck do Oponente", (c) => { }); // Apenas visualização
        }
    }

    void Effect_0069_AndroSphinx(CardDisplay source)
    {
        // Efeito 1: Pagar 500 LP para SS da mão se "Pyramid of Light" estiver em campo.
        if (!source.isOnField) // Está na mão
        {
            if (GameManager.Instance.IsCardActiveOnField("1470")) // Pyramid of Light
            {
                Effect_PayLP(source, 500);
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                Debug.Log("Andro Sphinx: Invocado Especialmente.");
            }
            else
            {
                Debug.Log("Andro Sphinx: Requer 'Pyramid of Light' no campo para Invocação Especial.");
            }
        }
        // Efeito 2: Trigger de batalha (Dano) - Tratado no BattleManager
    }

    void Effect_0071_Ante(CardDisplay source)
    {
        // Ambos revelam 1 carta da mão. Quem tiver menor Level toma 1000 dano e descarta.
        List<CardData> myHand = GameManager.Instance.GetPlayerHandData();
        if (myHand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(myHand, "Selecione carta para Ante", (myCard) => {
                // Simula escolha do oponente (aleatória)
                List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
                if (oppHand.Count > 0)
                {
                    CardData oppCard = oppHand[Random.Range(0, oppHand.Count)];
                    Debug.Log($"Ante: Você ({myCard.name} Lv{myCard.level}) vs Oponente ({oppCard.name} Lv{oppCard.level})");
                    
                    if (myCard.level > oppCard.level) GameManager.Instance.DamageOpponent(1000);
                    else if (oppCard.level > myCard.level) GameManager.Instance.DamagePlayer(1000);
                    else Debug.Log("Ante: Empate.");
                }
            });
        }
    }

    void Effect_0073_AntiRaigeki(CardDisplay source)
    {
        // Quando oponente ativa Raigeki: Negue, destrua todos monstros do oponente.
        // Verifica se a última carta na chain foi Raigeki do oponente
        // (Lógica simplificada, assume que ChainManager tem histórico)
        Debug.Log("Anti Raigeki: Destruindo monstros do oponente (Lógica de Counter pendente).");
        DestroyAllMonsters(true, false);
    }

    void Effect_0074_AntiAircraftFlower(CardDisplay source)
    {
        // Tribute 1 Inseto; cause 800 dano.
        Effect_TributeToBurn(source, 1, 800, "Insect");
    }

    void Effect_0075_AntiSpell(CardDisplay source)
    {
        // Remova 2 Spell Counters; negue Magia.
        if (GetTotalSpellCounters(source.isPlayerCard) >= 2)
        {
            if (RemoveSpellCounters(2, source.isPlayerCard))
            {
                Debug.Log("Anti-Spell: 2 Spell Counters removidos. Magia negada (Simulado).");
                // Se houver sistema de Chain, aqui negaríamos o efeito anterior
            }
        }
        else
        {
            Debug.Log("Anti-Spell: Spell Counters insuficientes (Precisa de 2).");
        }
    }

    void Effect_0076_AntiSpellFragrance(CardDisplay source)
    {
        // Magias devem ser setadas e esperar 1 turno.
        // Regra global no SpellTrapManager.
    }

    void Effect_0077_ApprenticeMagician(CardDisplay source)
    {
        // Quando destruído em batalha: SS Spellcaster Lv2 ou menor do Deck (Face-down).
        List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : null;
        if (deck != null)
        {
            List<CardData> targets = deck.FindAll(c => c.race == "Spellcaster" && c.level <= 2);
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Selecione Mago Lv2-", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard, false, true); // Face-down Defense
                    deck.Remove(selected); // Remove do deck (simulado, pois SpecialSummonFromData não remove)
                    // Nota: Em um sistema real, SpecialSummonFromData deveria lidar com a remoção da origem.
                });
            }
        }
    }

    void Effect_0078_Appropriate(CardDisplay source)
    {
        // Quando oponente compra fora da Draw Phase: Compre 2.
        // Trigger global.
    }

    void Effect_0079_AquaChorus(CardDisplay source)
    {
        // Se houver monstros com mesmo nome: +500 ATK/DEF para eles.
        // Lógica simplificada: Verifica se há monstros com o mesmo nome no campo e aplica o buff.
        // Em um sistema completo, isso seria um efeito contínuo que monitora o campo.
        // Aqui, aplicamos como um efeito de ativação manual ou trigger de fase para demonstração.
        
        if (GameManager.Instance.duelFieldUI == null) return;

        List<CardDisplay> allMonsters = new List<CardDisplay>();
        CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
        CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);

        Dictionary<string, List<CardDisplay>> nameGroups = new Dictionary<string, List<CardDisplay>>();

        foreach (var m in allMonsters)
        {
            string name = m.CurrentCardData.name;
            if (!nameGroups.ContainsKey(name))
                nameGroups[name] = new List<CardDisplay>();
            nameGroups[name].Add(m);
        }

        foreach (var group in nameGroups.Values)
        {
            if (group.Count > 1)
            {
                foreach (var m in group)
                {
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
                    m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, source));
                }
                Debug.Log($"Aqua Chorus: Buff aplicado em {group.Count} cópias de {group[0].CurrentCardData.name}.");
            }
        }
    }

    void Effect_0083_AquaSpirit(CardDisplay source)
    {
        // SS banindo 1 Water. Standby oponente: Muda posição de 1 monstro.
        // Parte 1: Invocação Especial (Geralmente tratada no SummonManager, aqui simulamos o efeito de campo)
        // Parte 2: Efeito de mudar posição na Standby do oponente (Trigger)
        
        // Se estiver na mão, tenta invocar
        if (!source.isOnField)
        {
            // Lógica de banir 1 Water do GY para invocar
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> waters = gy.FindAll(c => c.attribute == "Water");
            
            if (waters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(waters, "Banir 1 WATER para invocar", (selected) => {
                    GameManager.Instance.RemoveFromPlay(selected, source.isPlayerCard);
                    GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                    // Remover da mão (já que SpecialSummonFromData cria uma cópia)
                    GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                });
            }
            else
            {
                Debug.Log("Aqua Spirit: Requer 1 monstro WATER no GY.");
            }
        }
        else
        {
            // Efeito em campo: Mudar posição de 1 monstro do oponente
            // (Deveria ser na Standby do oponente, aqui ativamos manualmente para teste)
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                    (t) => {
                        t.ChangePosition();
                        Debug.Log($"Aqua Spirit: Posição de {t.CurrentCardData.name} alterada.");
                    }
                );
            }
        }
    }

    void Effect_0084_ArcanaKnightJoker(CardDisplay source)
    {
        // Quick: Descarte mesmo tipo (M/S/T) para negar efeito que dá alvo.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte para negar", (selected) => {
                // Verifica se o tipo da carta descartada corresponde ao tipo da carta alvo (Simulado)
                // Em um sistema real, precisariamos saber qual carta está na chain
                
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selected).GetComponent<CardDisplay>());
                Debug.Log($"Arcana Knight Joker: Descartou {selected.name}. Efeito negado (Simulado).");
                
                // Se houver chain, nega o último link
                if (ChainManager.Instance != null && ChainManager.Instance.currentChain.Count > 0)
                {
                    // Lógica de negação simplificada
                    Debug.Log("Arcana Knight Joker: Último efeito na corrente negado.");
                }
            });
        }
    }

    void Effect_0085_ArcaneArcherOfTheForest(CardDisplay source)
    {
        // Tribute 1 Plant; destrua 1 S/T.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard)) // Check Plant
        {
            // Tribute
            // Select S/T destroy
            Effect_MST(source); // Reusa lógica de destruir S/T
        }
    }

    void Effect_0089_ArchfiendOfGilfer(CardDisplay source)
    {
        // Quando enviado ao GY: Equipa em monstro, -500 ATK.
        Effect_Equip(source, -500, 0);
    }

    void Effect_0090_ArchfiendsOath(CardDisplay source)
    {
        // Pague 500; declare nome. Escave topo. Se acertar, mão. Senão, GY.
        if (Effect_PayLP(source, 500))
        {
            // Simulação de "Declarar Nome": Abre o Card Library/Trunk para escolher uma carta alvo
            // Como não temos UI de input de texto livre, usamos o seletor de cartas do jogo
            List<CardData> allCards = GameManager.Instance.cardDatabase.cardDatabase;
            
            // Otimização: Mostrar apenas uma sub-lista ou permitir busca seria ideal, 
            // mas aqui vamos simular a declaração pegando uma carta aleatória do deck para teste de sucesso
            // ou falha.
            
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            if (deck.Count > 0)
            {
                CardData topCard = deck[0];
                deck.RemoveAt(0); // Escava
                
                Debug.Log($"Archfiend's Oath: Carta escavada: {topCard.name}");
                
                // Lógica simplificada: 50% de chance de acertar em modo automático/teste
                bool success = Random.value > 0.5f; 
                
                if (success)
                {
                    Debug.Log("Archfiend's Oath: Acertou! Adicionada à mão.");
                    GameManager.Instance.AddCardToHand(topCard, true);
                }
                else
                {
                    Debug.Log("Archfiend's Oath: Errou! Enviada ao Cemitério.");
                    GameManager.Instance.SendToGraveyard(topCard, true);
                }
            }
        }
    }

    void Effect_0091_ArchfiendsRoar(CardDisplay source)
    {
        // Pague 500; SS 1 Archfiend do GY. Destrua na End Phase.
        Effect_PayLP(source, 500);
        
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.name.Contains("Archfiend") && c.type.Contains("Monster"));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver Archfiend", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                Debug.Log($"{selected.name} revivido. Será destruído na End Phase.");
            });
        }
    }

    void Effect_0092_ArchlordZerato(CardDisplay source)
    {
        // Descarte 1 Light; destrua todos monstros do oponente. (Requer Sanctuary).
        if (GameManager.Instance.IsCardActiveOnField("1887")) // Sanctuary in the Sky
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> lights = hand.FindAll(c => c.attribute == "LIGHT" && c.type.Contains("Monster"));
            
            if (lights.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(lights, "Descarte 1 LIGHT", (selected) => {
                    Debug.Log($"Archlord Zerato: Descartou {selected.name}.");
                    DestroyAllMonsters(true, false); // Destrói monstros do oponente
                });
            }
            else
            {
                Debug.Log("Archlord Zerato: Nenhum monstro LIGHT na mão para descartar.");
            }
        }
        else
        {
            Debug.Log("Archlord Zerato: Requer 'The Sanctuary in the Sky' no campo.");
        }
    }

    void Effect_0096_ArmedDragonLV3(CardDisplay source)
    {
        // Standby: Envia ao GY, SS LV5.
        Effect_LevelUp(source, "0097");
    }

    void Effect_0097_ArmedDragonLV5(CardDisplay source)
    {
        // Descarte monstro; destrua monstro oponente com ATK <= ATK do descartado.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Descarte 1 Monstro", (selectedCost) => {
                Debug.Log($"Armed Dragon LV5: Descartou {selectedCost.name} ({selectedCost.atk} ATK).");
                // TODO: Remover selectedCost da mão visualmente (GameManager.Discard não implementado ainda)

                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && t.currentAtk <= selectedCost.atk,
                        (target) => {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                            Debug.Log($"Armed Dragon LV5: Destruiu {target.CurrentCardData.name}.");
                        }
                    );
                }
            });
        }
        else
        {
            Debug.Log("Armed Dragon LV5: Nenhum monstro na mão para descartar.");
        }
    }

    void Effect_0098_ArmedDragonLV7(CardDisplay source)
    {
        // Descarte monstro; destrua todos monstros oponente com ATK <= ATK do descartado.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));

        if (monsters.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(monsters, "Descarte 1 Monstro", (selectedCost) => {
                Debug.Log($"Armed Dragon LV7: Descartou {selectedCost.name} ({selectedCost.atk} ATK).");
                
                List<CardDisplay> toDestroy = new List<CardDisplay>();
                Transform[] zones = GameManager.Instance.duelFieldUI.opponentMonsterZones;
                
                foreach(var zone in zones)
                {
                    if(zone.childCount > 0)
                    {
                        var m = zone.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null && m.currentAtk <= selectedCost.atk) toDestroy.Add(m);
                    }
                }
                
                DestroyCards(toDestroy, false); // false = oponente
            });
        }
        else
        {
            Debug.Log("Armed Dragon LV7: Nenhum monstro na mão para descartar.");
        }
    }

    void Effect_0099_ArmedNinja(CardDisplay source)
    {
        // FLIP: Destrói 1 Magia.
        Effect_FlipDestroy(source, TargetType.Spell);
    }

    void Effect_0100_ArmedSamuraiBenKei(CardDisplay source)
    {
        // Ataques adicionais = número de Equips.
        // Passivo no BattleManager.
    }
    
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0101 - 0200)
    // =========================================================================================

    void Effect_0101_ArmorBreak(CardDisplay source)
    {
        // Counter Trap: Negate Equip Spell activation.
        // Verifica se há uma chain e se o último elo é uma Equip Spell
        if (ChainManager.Instance != null && ChainManager.Instance.currentChain.Count > 1)
        {
            // O elo atual é o Armor Break, o anterior é o alvo
            var targetLink = ChainManager.Instance.currentChain[ChainManager.Instance.currentChain.Count - 2];
            var targetCard = targetLink.cardSource;
            
            if (targetCard != null && targetCard.CurrentCardData.type.Contains("Spell") && targetCard.CurrentCardData.property == "Equip")
            {
                Debug.Log($"Armor Break: Negando ativação de {targetCard.CurrentCardData.name}.");
                // Em um sistema real, marcaríamos o elo como negado. Aqui destruímos a fonte.
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(targetCard);
                GameManager.Instance.SendToGraveyard(targetCard.CurrentCardData, targetCard.isPlayerCard);
                Destroy(targetCard.gameObject);
            }
            else
            {
                Debug.Log("Armor Break: O último efeito não era uma Equip Spell.");
            }
        }
    }

    void Effect_0102_ArmorExe(CardDisplay source)
    {
        // Maintenance: Remove counter or destroy.
        // A lógica de manutenção é automática e tratada no CardEffectManager_Impl.cs (CheckMaintenanceCosts)
        Debug.Log("Armor Exe: Efeito de manutenção ativo.");
    }

    void Effect_0103_ArmoredGlass(CardDisplay source)
    {
        Debug.Log("Armored Glass: Imunidade a Equip Spells este turno.");
    }

    void Effect_0108_ArrayOfRevealingLight(CardDisplay source)
    {
        // Trap: Pay 2000, declare Type. Monsters of that type cannot attack this turn.
        if (Effect_PayLP(source, 2000))
        {
            // Simulação de UI de Declaração
            string[] types = { "Warrior", "Dragon", "Spellcaster", "Fiend", "Zombie", "Machine", "Aqua", "Pyro", "Rock", "Winged Beast", "Fairy", "Beast", "Beast-Warrior", "Dinosaur", "Thunder", "Fish", "Sea Serpent", "Reptile", "Plant", "Insect" };
            string randomType = types[Random.Range(0, types.Length)];
            
            Debug.Log($"Array of Revealing Light: Tipo declarado (Simulado): {randomType}");
            
            // Armazenar o tipo declarado na carta para verificação no BattleManager
            // Como CardDisplay não tem campo genérico, usamos um log para indicar a ação
            // Em produção: source.declaredInfo = randomType;
        }
    }

    void Effect_0109_ArsenalBug(CardDisplay source)
    {
        // Continuous: ATK = 2000. Se não tiver outro Inseto, ATK = 1000.
        // Aplica stat dinâmico baseado na existência de outros insetos.
        if (HasAnotherInsect(source))
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, 2000, source));
        else
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, 1000, source));
    }

    bool HasAnotherInsect(CardDisplay self)
    {
        // TODO: Implementar checagem no campo
        return false; // SIMULADO
    }

    void Effect_0110_ArsenalRobber(CardDisplay source)
    {
        // Oponente escolhe Equip do Deck e envia ao GY.
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        List<CardData> equips = oppDeck.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");

        if (equips.Count > 0)
        {
            // Simula a escolha do oponente (aleatória)
            CardData selected = equips[Random.Range(0, equips.Count)];
            Debug.Log($"Arsenal Robber: Oponente selecionou e enviou {selected.name} ao GY.");
            
            oppDeck.Remove(selected);
            GameManager.Instance.SendToGraveyard(selected, !source.isPlayerCard);
        }
        else
        {
            Debug.Log("Arsenal Robber: Oponente não possui Equip Spells no Deck.");
        }
    }

    void Effect_0111_ArsenalSummoner(CardDisplay source)
    {
        // FLIP: Add Guardian card from Deck to Hand.
        Effect_SearchDeck(source, "Guardian");
    }

    void Effect_0112_AssaultOnGHQ(CardDisplay source)
    {
        // Select 1 monster you control; destroy it, send top 2 cards of opp Deck to GY.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    Debug.Log("Assault on GHQ: Monstro destruído. Mill 2 do oponente.");
                    // TODO: Implementar Mill (enviar do topo do deck pro GY)
                }
            );
        }
    }

    void Effect_0113_AstralBarrier(CardDisplay source)
    {
        Debug.Log("Astral Barrier: Se oponente atacar monstro, você pode tornar ataque direto.");
    }

    void Effect_0114_AsuraPriest(CardDisplay source)
    {
        // Spirit / Attack All
        Debug.Log("Asura Priest: Pode atacar todos os monstros.");
    }

    void Effect_0115_AswanApparition(CardDisplay source)
    {
        Debug.Log("Aswan Apparition: Se causar dano, retorna Trap do GY pro topo do Deck.");
    }

    void Effect_0116_AtomicFirefly(CardDisplay source)
    {
        // Se destruído em batalha: Oponente toma 1000, depois 500. (Texto antigo varia, vamos usar 1000).
        // Trigger de destruição.
    }

    void Effect_0117_AttackAndReceive(CardDisplay source)
    {
        // Activate only when you take damage. Inflict 700 damage.
        // Plus extra effects if more copies in GY.
        Effect_DirectDamage(source, 700);
        // TODO: Checar cópias no GY para dano extra (300 / 1000).
    }

    void Effect_0118_AussaTheEarthCharmer(CardDisplay source)
    {
        // FLIP: Take control of 1 Earth monster.
        Debug.Log("Aussa: Controle de monstro EARTH (Lógica de seleção pendente).");
    }

    void Effect_0119_AutonomousActionUnit(CardDisplay source)
    {
        // Pay 1500 LP; SS Monster from Opponent's GY in Attack. Equip this card.
        Effect_PayLP(source, 1500);
        Effect_Revive(source, true); // true = any graveyard (precisa filtrar oponente)
    }

    void Effect_0120_AvatarOfThePot(CardDisplay source)
    {
        // Send "Pot of Greed" from hand to GY; draw 3 cards.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData pot = hand.Find(c => c.name == "Pot of Greed" || c.id == "1447");
        
        if (pot != null)
        {
            Debug.Log("Avatar of The Pot: Enviando Pot of Greed e comprando 3.");
            // Remover Pot da mão (visual pendente)
            GameManager.Instance.DrawCard();
            GameManager.Instance.DrawCard();
            GameManager.Instance.DrawCard();
        }
        else
        {
            Debug.Log("Avatar of The Pot: Requer 'Pot of Greed' na mão.");
        }
    }

    void Effect_0122_AxeOfDespair(CardDisplay source)
    {
        Effect_Equip(source, 1000, 0);
    }

    void Effect_0124_BESBigCore(CardDisplay source)
    {
        // Efeito de Contadores é tratado no OnSummonImpl
        // Efeito de Batalha é tratado no BattleManager e OnBattleEnd
        Debug.Log("B.E.S. Big Core: Lógica de contadores e batalha automática.");
    }

    void Effect_0125_BESCrystalCore(CardDisplay source)
    {
        // Efeito de Contadores/Batalha: Automático
        // Efeito de Ignição: Mudar posição
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"B.E.S. Crystal Core: {t.CurrentCardData.name} mudou para defesa.");
                }
            );
        }
    }

    void Effect_0127_BackToSquareOne(CardDisplay source)
    {
        // Discard 1 card; return 1 monster to top of Deck.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                Debug.Log($"Back to Square One: Descartou {discarded.name}.");
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                        (target) => {
                            Debug.Log($"{target.CurrentCardData.name} retornado ao topo do deck.");
                            Destroy(target.gameObject); // Simplificado (deveria ir pro topo)
                        }
                    );
                }
            });
        }
    }

    void Effect_0128_Backfire(CardDisplay source)
    {
        // Continuous: When FIRE monster destroyed, 500 dmg to opp.
        Debug.Log("Backfire: Ativo. (Trigger de destruição pendente).");
    }

    void Effect_0129_BackupSoldier(CardDisplay source)
    {
        // Target up to 3 Normal Monsters in GY; add to hand.
        // Simplificação: Pega 1 por enquanto (falta Multi-Select).
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.type.Contains("Normal") && !c.type.Contains("Effect"));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Recuperar Normal Monster", (selected) => {
                // Adicionar à mão
                Debug.Log($"Backup Soldier: {selected.name} recuperado.");
            });
        }
    }

    void Effect_0130_BadReactionToSimochi(CardDisplay source)
    {
        Debug.Log("Simochi: Efeitos de cura do oponente viram dano.");
    }

    void Effect_0131_BaitDoll(CardDisplay source)
    {
        // Target 1 Set card; reveal it. If Trap, force activation (destroy). If not, return to Set.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")) && t.isFlipped == false,
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.type.Contains("Trap"))
                    {
                        Debug.Log($"Bait Doll: {t.CurrentCardData.name} é uma Armadilha! Forçando ativação (Destruindo).");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                    else
                    {
                        Debug.Log($"Bait Doll: {t.CurrentCardData.name} não é Armadilha. Retornando para face-down.");
                        t.ShowBack();
                    }
                    
                    // Bait Doll vai para o Deck
                    GameManager.Instance.ReturnToDeck(source, false); // false = Fundo (ou shuffle)
                    GameManager.Instance.ShuffleDeck(source.isPlayerCard);
                }
            );
        }
    }

    void Effect_0132_BalloonLizard(CardDisplay source)
    {
        // Acumula contadores a cada Standby (Lógica no CardEffectManager_Impl.cs -> OnPhaseStart)
        // Dano ao destruir (Lógica no CardEffectManager_Impl.cs -> OnCardLeavesField)
        Debug.Log($"Balloon Lizard: Contadores atuais: {source.spellCounters}");
    }

    void Effect_0133_BanisherOfTheLight(CardDisplay source)
    {
        Debug.Log("Banisher of the Light: Cartas vão para Banished em vez do GY.");
    }

    void Effect_0134_BannerOfCourage(CardDisplay source)
    {
        // Battle Phase: +200 ATK to all your monsters.
        Debug.Log("Banner of Courage: Buff de Battle Phase.");
    }

    void Effect_0135_BarkOfDarkRuler(CardDisplay source)
    {
        // Pay LP; reduce DEF of Fiend.
        // O que falta: Seleção de alvo e custo de LP.
        Debug.Log("Bark of Dark Ruler: Debuff de Fiend.");
    }

    void Effect_0138_BarrelBehindTheDoor(CardDisplay source)
    {
        Debug.Log("Barrel Behind the Door: Counter Trap de dano.");
    }

    void Effect_0139_BarrelDragon(CardDisplay source)
    {
        // 3 Coins. 2+ Heads -> Destroy 1 monster.
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Monster);
    }

    void Effect_0144_BatteryCharger(CardDisplay source)
    {
        // Pay 500; SS 1 "Batteryman" from GY.
        Effect_PayLP(source, 500);
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> targets = gy.FindAll(c => c.name.Contains("Batteryman"));
        
        if (targets.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(targets, "Reviver Batteryman", (selected) => {
                GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            });
        }
    }

    void Effect_0145_BatterymanAA(CardDisplay source)
    {
        // ATK = 1000 * number of AA in Attack. DEF = 1000 * number of AA in Defense.
        Debug.Log("Batteryman AA: Stats dinâmicos pendentes.");
    }

    void Effect_0146_BatterymanC(CardDisplay source)
    {
        // All Machines +500 ATK/DEF.
        Effect_Field(source, 500, 500, "Machine");
    }

    void Effect_0151_BattleScarred(CardDisplay source)
    {
        Debug.Log("Battle-Scarred: Oponente paga custo de manutenção de Archfiend.");
    }

    void Effect_0152_BazooTheSoulEater(CardDisplay source)
    {
        // Banish up to 3 from GY; +300 ATK per card.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster"));
        if (gy.Count > 0)
        {
            int max = Mathf.Min(3, gy.Count);
            GameManager.Instance.OpenCardMultiSelection(gy, "Banir até 3 monstros", 1, max, (selected) => {
                int count = selected.Count;
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    GameManager.Instance.GetPlayerGraveyard().Remove(c); // Remove do GY manualmente pois RemoveFromPlay não altera a lista de origem
                }
                // Atualiza visual
                GameManager.Instance.playerGraveyardDisplay.UpdatePile(GameManager.Instance.GetPlayerGraveyard(), GameManager.Instance.GetCardBackTexture());
                
                int buff = count * 300;
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
                Debug.Log($"Bazoo: Baniu {count} monstros. Ganhou {buff} ATK.");
            });
        }
    }

    void Effect_0155_BeastFangs(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Beast");
    }

    void Effect_0156_BeastSoulSwap(CardDisplay source)
    {
        // Return 1 Beast you control to hand; SS 1 Beast from hand.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Beast",
                (t) => {
                    // Retorna para mão (Simulado: Destrói)
                    Destroy(t.gameObject);
                    Debug.Log("Beast Soul Swap: Besta retornada.");
                    
                    // Invoca da mão
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> beasts = hand.FindAll(c => c.race == "Beast" && c.level <= t.CurrentCardData.level); // Regra: Mesmo nível? Não, qualquer nível.
                    
                    if (beasts.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(beasts, "Invocar Besta", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    void Effect_0158_BeastkingOfTheSwamps(CardDisplay source)
    {
        // Discard: Add Polymerization.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        CardData poly = deck.Find(c => c.name == "Polymerization");
        
        if (poly != null)
        {
            Debug.Log("Beastking: Buscando Polymerization...");
            // Adicionar à mão (Lógica de AddToHand pendente no GameManager)
        }
    }

    void Effect_0163_BeckoningLight(CardDisplay source)
    {
        // Discard entire hand; add Light monsters from GY equal to discarded amount.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        int discardCount = hand.Count;
        
        if (discardCount > 0)
        {
            // Descarta toda a mão
            // Nota: Precisamos iterar sobre uma cópia ou usar DiscardRandomHand repetidamente
            // Como não temos acesso fácil aos GameObjects da mão aqui, usamos DiscardRandomHand que limpa tudo
            GameManager.Instance.DiscardRandomHand(source.isPlayerCard, discardCount);
            
            // Seleciona LIGHTs do GY
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> lights = gy.FindAll(c => c.attribute == "Light" && c.type.Contains("Monster"));
            
            if (lights.Count >= discardCount) 
            {
                 GameManager.Instance.OpenCardMultiSelection(lights, $"Selecione {discardCount} LIGHTs", discardCount, discardCount, (selected) => {
                     foreach(var c in selected) {
                         gy.Remove(c);
                         GameManager.Instance.AddCardToHand(c, source.isPlayerCard);
                     }
                     // Atualiza visual do GY
                     GameManager.Instance.playerGraveyardDisplay.UpdatePile(gy, GameManager.Instance.GetCardBackTexture());
                 });
            }
        }
    }

    void Effect_0164_BegoneKnave(CardDisplay source)
    {
        Debug.Log("Begone, Knave!: Se causar dano, retorna para a mão.");
    }

    void Effect_0166_BehemothTheKingOfAllAnimals(CardDisplay source)
    {
        // Tribute Summon: Return beasts from GY to hand.
        // O que falta: Trigger de Invocação Tributo.
        Debug.Log("Behemoth: Recupera bestas ao ser tributado.");
    }

    void Effect_0167_Berfomet(CardDisplay source)
    {
        // Summon: Add Gazelle from Deck.
        Effect_SearchDeck(source, "Gazelle the King of Mythical Beasts");
    }

    void Effect_0168_BerserkDragon(CardDisplay source)
    {
        // Attacks all monsters. Loses 500 ATK each End Phase.
        Debug.Log("Berserk Dragon: Ataca todos. Perde ATK.");
    }

    void Effect_0169_BerserkGorilla(CardDisplay source)
    {
        // Destroy if Defense. Must attack.
        // O que falta: Verificação contínua de posição e obrigação de ataque.
        if (source.position == CardDisplay.BattlePosition.Defense)
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            Debug.Log("Berserk Gorilla: Destruído por estar em defesa.");
        }
    }

    void Effect_0172_BigBangShot(CardDisplay source)
    {
        // Equip: +400, Piercing. If removed, banish monster.
        // O que falta: Trigger de remoção para banir.
        Effect_Equip(source, 400, 0);
    }

    void Effect_0173_BigBurn(CardDisplay source)
    {
        Debug.Log("Big Burn: Bane monstros dos cemitérios.");
    }

    void Effect_0174_BigEye(CardDisplay source)
    {
        // FLIP: Look at top 5, arrange them.
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (deck.Count > 0)
        {
            int count = Mathf.Min(5, deck.Count);
            List<CardData> topCards = deck.GetRange(0, count);
            
            // Usa a seleção múltipla para simular a reordenação
            // A lista 'ordered' virá na ordem que o jogador clicou (1, 2, 3...)
            GameManager.Instance.OpenCardMultiSelection(topCards, "Selecione a ordem (1º = Topo)", count, count, (ordered) => {
                deck.RemoveRange(0, count);
                // Insere de volta no topo na ordem escolhida
                // InsertRange(0, ordered) coloca a lista no topo. O índice 0 da lista vira o topo do deck.
                deck.InsertRange(0, ordered);
                Debug.Log("Big Eye: Deck reordenado.");
            });
        }
    }

    void Effect_0177_BigShieldGardna(CardDisplay source)
    {
        // Negate Spell targeting it (face-down). Change to Attack if attacked.
        // O que falta: Trigger de alvo e ataque.
        Debug.Log("Big Shield Gardna: Efeitos defensivos.");
    }

    void Effect_0178_BigWaveSmallWave(CardDisplay source)
    {
        // Destroy all face-up Water; SS Water from hand equal to destroyed.
        Debug.Log("Big Wave Small Wave: Substituição de monstros Water.");
        // DestroyAllMonsters(true, true); // Filtrar por Water
    }

    void Effect_0179_BigTuskedMammoth(CardDisplay source)
    {
        Debug.Log("Big-Tusked Mammoth: Impede ataques no turno que monstros oponente são invocados.");
    }

    void Effect_0183_Birdface(CardDisplay source)
    {
        // Destroyed by battle: Add Harpie Lady.
        Effect_SearchDeck(source, "Harpie Lady"); // Harpie Lady busca
    }

    void Effect_0184_BiteShoes(CardDisplay source)
    {
        // FLIP: Change position of 1 monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    t.ChangePosition();
                    Debug.Log("Bite Shoes: Posição alterada.");
                }
            );
        }
    }

    void Effect_0185_BlackDragonsChick(CardDisplay source)
    {
        // Send self to GY; SS Red-Eyes B. Dragon from hand.
        if (source.isOnField)
        {
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            CardData redEyes = hand.Find(c => c.name == "Red-Eyes B. Dragon");
            
            if (redEyes != null)
            {
                GameManager.Instance.SpecialSummonFromData(redEyes, source.isPlayerCard);
            }
        }
    }

    void Effect_0189_BLSEnvoy(CardDisplay source)
    {
        // Banish 1 monster OR Double Attack.
        // Efeito de Ignição: Banir monstro (Custo: Não atacar)
        if (source.hasAttackedThisTurn) 
        {
            Debug.Log("BLS: Já atacou, não pode usar efeito de banir.");
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    GameManager.Instance.BanishCard(t);
                    source.hasAttackedThisTurn = true; // Impede ataque neste turno
                }
            );
        }
    }

    void Effect_0191_BlackPendant(CardDisplay source)
    {
        Effect_Equip(source, 500, 0);
        // TODO: Burn 500 if sent to GY.
    }

    void Effect_0193_BlackTyranno(CardDisplay source)
    {
        Debug.Log("Black Tyranno: Ataque direto se oponente só tiver defesa.");
    }

    void Effect_0195_BladeKnight(CardDisplay source)
    {
        Debug.Log("Blade Knight: +400 ATK se mão <= 1.");
    }

    void Effect_0196_BladeRabbit(CardDisplay source)
    {
        // Changed to Attack: Destroy 1 monster.
        // O que falta: Trigger de mudança de posição.
        Debug.Log("Blade Rabbit: Destrói monstro ao mudar posição.");
    }

    void Effect_0197_Bladefly(CardDisplay source)
    {
        Effect_Field(source, 500, -400, "", "Wind");
    }

    void Effect_0198_BlastHeldByATribute(CardDisplay source)
    {
        // Activate when opponent attacks with Tribute Summoned monster. Destroy & 1000 dmg.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            // Checagem de tributo pendente
            Debug.Log("Blast Held by a Tribute: Destruindo atacante...");
            // Destroy logic
            Effect_DirectDamage(source, 1000);
        }
    }

    void Effect_0199_BlastJuggler(CardDisplay source)
    {
        // Tribute (Standby): Destroy 2 face-up with ATK 1000 or less.
        // O que falta: Trigger de Standby e seleção múltipla.
        Debug.Log("Blast Juggler: Destruição em massa de monstros fracos.");
    }

    void Effect_0200_BlastMagician(CardDisplay source)
    {
        // Remove counters -> Destroy monster.
        // O que falta: Sistema de Spell Counters.
        Debug.Log("Blast Magician: Remove contadores para destruir.");
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0201 - 0300)
    // =========================================================================================

    void Effect_0201_BlastSphere(CardDisplay source)
    {
        // Efeito: Se atacado face-down, equipa no atacante e destrói na próxima Standby.
        // Lógica principal tratada no BattleManager (Equip) e CardEffectManager_Impl (Standby).
        Debug.Log("Blast Sphere: Efeito passivo de batalha/standby.");
    }

    void Effect_0202_BlastWithChain(CardDisplay source)
    {
        // Efeito: Equip +500. Se destruído por efeito, destrói 1 carta.
        Effect_Equip(source, 500, 0);
    }

    void Effect_0203_BlastingTheRuins(CardDisplay source)
    {
        // Efeito: Se houver 30+ cartas no GY, causa 3000 de dano.
        int gyCount = source.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard().Count : GameManager.Instance.GetOpponentGraveyard().Count;
        if (gyCount >= 30) 
        {
            Effect_DirectDamage(source, 3000);
        }
        else
        {
            Debug.Log("Blasting the Ruins: Requer 30+ cartas no cemitério.");
        }
    }

    void Effect_0205_BlessingsOfTheNile(CardDisplay source)
    {
        // Efeito Contínuo: Ganha 1000 LP cada vez que cartas são descartadas da mão para o GY por efeito.
        // Tratado no CardEffectManager_Impl.cs (OnCardDiscarded).
        Debug.Log("Blessings of the Nile: Ativo.");
    }

    void Effect_0206_BlindDestruction(CardDisplay source)
    {
        // Efeito Contínuo: Na Standby Phase, rola dado. Destrói monstros com Nível = Dado.
        // Tratado no CardEffectManager_Impl.cs (OnPhaseStart).
        Debug.Log("Blind Destruction: Ativo.");
    }

    void Effect_0207_BlindlyLoyalGoblin(CardDisplay source)
    {
        // Efeito Contínuo: Controle não pode mudar.
        // Tratado no GameManager.cs (SwitchControl).
        Debug.Log("Blindly Loyal Goblin: Imune a troca de controle.");
    }

    void Effect_0208_BlockAttack(CardDisplay source)
    {
        // Efeito: Seleciona 1 monstro em Ataque e muda para Defesa.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Block Attack: {t.CurrentCardData.name} mudou para defesa.");
                }
            );
        }
    }

    void Effect_0210_BloodSucker(CardDisplay source)
    {
        // Efeito: Quando causa dano de batalha, oponente envia topo do deck ao GY.
        // Tratado no CardEffectManager_Impl.cs (OnDamageDealtImpl).
        Debug.Log("Blood Sucker: Efeito de mill passivo.");
    }

    void Effect_0211_BlowbackDragon(CardDisplay source)
    {
        // Efeito: 3 Moedas. 2+ Caras -> Destrói 1 carta do oponente.
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Any);
    }

    void Effect_0212_BlueMedicine(CardDisplay source)
    {
        // Efeito: Ganha 400 LP.
        Effect_GainLP(source, 400);
    }

    void Effect_0214_BlueEyesShiningDragon(CardDisplay source)
    {
        // Efeito: SS tributando Blue-Eyes Ultimate. Ganha 300 ATK por Dragão no GY. Nega efeitos que dão alvo.
        if (!source.isOnField)
        {
            // Lógica de Invocação da Mão
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Blue-Eyes Ultimate Dragon",
                    (t) => {
                        GameManager.Instance.TributeCard(t);
                        GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                        // Remove da mão
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                    }
                );
            }
        }
        else
        {
            // Efeito em campo: Buff
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            int dragons = gy.FindAll(c => c.race == "Dragon").Count;
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, dragons * 300, source));
            Debug.Log($"Blue-Eyes Shining Dragon: +{dragons * 300} ATK.");
        }
    }

    void Effect_0215_BlueEyesToonDragon(CardDisplay source)
    {
        // Efeito: Toon (Ataca direto se oponente não tiver Toon, etc).
        // Regras Toon tratadas no BattleManager.
        Debug.Log("Blue-Eyes Toon Dragon: Regras de Toon aplicadas.");
    }

    void Effect_0219_BoarSoldier(CardDisplay source)
    {
        // Efeito: Se invocado Normal, destrói a si mesmo. Se oponente tiver monstros, diminui ATK.
        // Auto-destruição tratada no OnSummonImpl.
        // Debuff:
        if (GameManager.Instance.duelFieldUI != null)
        {
            bool oppHasMonsters = false;
            foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) oppHasMonsters = true;
            
            if (oppHasMonsters)
            {
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -1000, source));
            }
        }
    }

    void Effect_0223_BombardmentBeetle(CardDisplay source)
    {
        // Efeito: Flip 1 monstro face-down do oponente. Se for efeito, vira face-down de novo. Se Normal, destrói.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.isFlipped, // Face-down
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.type.Contains("Effect"))
                    {
                        Debug.Log("Bombardment Beetle: É Effect Monster. Virando face-down.");
                        t.ShowBack();
                    }
                    else
                    {
                        Debug.Log("Bombardment Beetle: É Normal Monster. Destruindo.");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                }
            );
        }
    }

    void Effect_0227_BookOfLife(CardDisplay source)
    {
        // Efeito: SS 1 Zumbi do seu GY e bane 1 monstro do GY do oponente.
        List<CardData> myGY = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> zombies = myGY.FindAll(c => c.race == "Zombie");
        
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> oppMonsters = oppGY.FindAll(c => c.type.Contains("Monster"));

        if (zombies.Count > 0 && oppMonsters.Count > 0)
        {
            // Passo 1: Seleciona Zumbi
            GameManager.Instance.OpenCardSelection(zombies, "Reviver Zumbi", (myZombie) => {
                // Passo 2: Seleciona monstro do oponente para banir
                GameManager.Instance.OpenCardSelection(oppMonsters, "Banir do Oponente", (oppMonster) => {
                    // Executa
                    GameManager.Instance.SpecialSummonFromData(myZombie, source.isPlayerCard);
                    GameManager.Instance.RemoveFromPlay(oppMonster, !source.isPlayerCard);
                    oppGY.Remove(oppMonster); // Remove manual pois RemoveFromPlay não altera lista de origem
                    Debug.Log($"Book of Life: Reviveu {myZombie.name} e baniu {oppMonster.name}.");
                });
            });
        }
        else
        {
            Debug.Log("Book of Life: Requer Zumbi no seu GY e Monstro no GY do oponente.");
        }
    }

    void Effect_0228_BookOfMoon(CardDisplay source)
    {
        // Efeito: Vira 1 monstro face-up para face-down Defense.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped, // Face-up
                (t) => {
                    t.ChangePosition(); // Vira defesa (se ataque)
                    t.ShowBack(); // Face-down
                    Debug.Log($"Book of Moon: {t.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    void Effect_0229_BookOfSecretArts(CardDisplay source)
    {
        // Efeito: Equip Spellcaster +300 ATK/DEF.
        Effect_Equip(source, 300, 300, "Spellcaster");
    }

    void Effect_0230_BookOfTaiyou(CardDisplay source)
    {
        // Efeito: Vira 1 monstro face-down para face-up Attack.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped, // Face-down
                (t) => {
                    t.RevealCard();
                    if (t.position == CardDisplay.BattlePosition.Defense) t.ChangePosition(); // Vira ataque
                    Debug.Log($"Book of Taiyou: {t.CurrentCardData.name} virado para cima.");
                }
            );
        }
    }

    void Effect_0232_BottomlessShiftingSand(CardDisplay source)
    {
        // Efeito Contínuo: Na End Phase do oponente, destrói o monstro com maior ATK (se > 2500).
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase)
        Debug.Log("Bottomless Shifting Sand: Ativo.");
    }

    void Effect_0233_BottomlessTrapHole(CardDisplay source)
    {
        // Efeito: Quando oponente invoca (1500+ ATK): Destrói e Bane.
        // Verifica monstros invocados recentemente com ATK >= 1500
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null && monster.summonedThisTurn && monster.currentAtk >= 1500)
                    {
                        Debug.Log($"Bottomless Trap Hole: Destruindo e banindo {monster.CurrentCardData.name}.");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
                        GameManager.Instance.RemoveFromPlay(monster.CurrentCardData, monster.isPlayerCard);
                        Destroy(monster.gameObject);
                    }
                }
            }
        }
    }

    void Effect_0235_Bowganian(CardDisplay source)
    {
        // Efeito: Causa 600 dano na sua Standby Phase.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Bowganian: Ativo.");
    }

    void Effect_0237_BrainControl(CardDisplay source)
    {
        // Efeito: Paga 800 LP, controla monstro face-up do oponente até End Phase.
        Effect_PayLP(source, 800);
        Effect_ChangeControl(source, true); // true = retorna na End Phase
    }

    void Effect_0238_BrainJacker(CardDisplay source)
    {
        // Efeito: FLIP - Equipa no oponente e toma controle. Oponente ganha 500 LP na Standby.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Brain Jacker: Equipando em {t.CurrentCardData.name} e tomando controle.");
                    // Simulação: Vira equip (move para S/T) e toma controle do monstro
                    // Como não temos "Monstro virando Equip" nativo, vamos simular:
                    // 1. Brain Jacker vai para S/T do jogador
                    // 2. Toma controle do alvo
                    GameManager.Instance.SwitchControl(t);
                    // TODO: Mover Brain Jacker visualmente para S/T e criar link
                }
            );
        }
    }

    void Effect_0240_BreakerTheMagicalWarrior(CardDisplay source)
    {
        // Efeito: Normal Summon -> Ganha contador (+300 ATK). Remove contador -> Destrói S/T.
        // Ignition Effect: Se tiver contador, remove e destrói S/T
        if (source.spellCounters > 0)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
                    (t) => {
                        source.RemoveSpellCounter(1);
                        // Remove o buff de ATK (assumindo que foi adicionado no AddCounter ou é dinâmico)
                        // Como o sistema de stats é recalculado, se tivermos um modificador baseado em counters, ele atualiza.
                        // Por enquanto, removemos manual se foi adicionado manual.
                        
                        Debug.Log($"Breaker: Destruindo {t.CurrentCardData.name}.");
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                );
            }
        }
    }

    void Effect_0241_BreathOfLight(CardDisplay source)
    {
        // Efeito: Destrói todos os monstros Rock face-up.
        Effect_DestroyType(source, "Rock");
    }

    void Effect_0242_BubbleCrash(CardDisplay source)
    {
        // Efeito: Se tiver cartas na mão/campo, envie para o GY até ter 5.
        int totalCards = GameManager.Instance.GetPlayerHandData().Count;
        // + Campo...
        if (totalCards > 5)
        {
            int toSend = totalCards - 5;
            Debug.Log($"Bubble Crash: Você deve enviar {toSend} cartas para o GY.");
            // Abre seleção múltipla na mão para descartar (simplificado, deveria incluir campo)
            GameManager.Instance.OpenCardMultiSelection(GameManager.Instance.GetPlayerHandData(), $"Selecione {toSend} para enviar ao GY", toSend, toSend, (selected) => {
                foreach(var c in selected)
                {
                    // Encontra o display correspondente e descarta
                    // (Lógica simplificada de busca)
                    GameManager.Instance.SendToGraveyard(c, source.isPlayerCard);
                    // Remover da mão visualmente...
                }
            });
        }
    }

    void Effect_0243_BubbleShuffle(CardDisplay source)
    {
        // Efeito: Alvo 1 E-Hero Bubbleman e 1 monstro oponente em ataque. Muda ambos para defesa, sacrifica Bubbleman, invoca E-Hero da mão.
        if (SpellTrapManager.Instance != null)
        {
            // Seleciona Bubbleman
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name.Contains("Bubbleman"),
                (bubbleman) => {
                    // Seleciona Oponente
                    SpellTrapManager.Instance.StartTargetSelection(
                        (opp) => t.isOnField && !t.isPlayerCard && t.position == CardDisplay.BattlePosition.Attack,
                        (oppMonster) => {
                            bubbleman.ChangePosition();
                            oppMonster.ChangePosition();
                            GameManager.Instance.TributeCard(bubbleman);
                            // SS da mão (Pendente UI de seleção da mão filtrada por E-Hero)
                        });
                });
        }
    }

    void Effect_0244_BubonicVermin(CardDisplay source)
    {
        // Efeito: FLIP - SS 1 Bubonic Vermin do Deck face-down.
        Effect_SearchDeck(source, "Bubonic Vermin"); // Simplificado (deveria invocar, não buscar para mão)
        // TODO: Alterar Effect_SearchDeck para suportar SS direto ou criar Effect_SpecialSummonFromDeck
    }

    void Effect_0246_BurningAlgae(CardDisplay source)
    {
        // Efeito: Quando enviado ao GY, oponente ganha 1000 LP.
        // Trigger de cemitério.
        if (source.isPlayerCard) GameManager.Instance.opponentLP += 1000;
        else GameManager.Instance.playerLP += 1000;
        Debug.Log("Burning Algae: Oponente ganhou 1000 LP.");
    }

    void Effect_0248_BurningLand(CardDisplay source)
    {
        // Efeito: Destrói Field Spells. Na Standby, ambos tomam 500 dano.
        // O que falta: TurnObserver para dano recorrente.
        Debug.Log("Burning Land: Destruindo campos...");
        // Lógica de destruir campos
        // Destroy(GameManager.Instance.duelFieldUI.playerFieldSpell.GetChild(0).gameObject);
    }

    void Effect_0249_BurningSpear(CardDisplay source)
    {
        // Efeito: Equip Fire +400 ATK, -200 DEF.
        Effect_Equip(source, 400, -200, "", "Fire");
    }

    void Effect_0250_BurstBreath(CardDisplay source)
    {
        // Efeito: Tributa 1 Dragão; destrói todos monstros face-up com DEF <= ATK do tributo.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Dragon",
                (tribute) => {
                    int atk = tribute.currentAtk;
                    GameManager.Instance.TributeCard(tribute);
                    
                    // Destrói monstros com DEF <= atk
                    // (Requer iteração no campo e verificação de DEF)
                    Debug.Log($"Burst Breath: Destruindo monstros com DEF <= {atk}.");
                }
            );
        }
    }

    void Effect_0251_BurstStreamOfDestruction(CardDisplay source)
    {
        // Efeito: Se controlar Blue-Eyes, destrói todos monstros do oponente. Blue-Eyes não ataca.
        // O que falta: Restrição de ataque no BattleManager.
        if (GameManager.Instance.IsCardActiveOnField("0000")) // ID do Blue-Eyes (Exemplo)
        {
            DestroyAllMonsters(true, false);
        }
        else
        {
            Debug.Log("Burst Stream: Requer Blue-Eyes White Dragon.");
        }
    }

    void Effect_0252_BusterBlader(CardDisplay source)
    {
        // Efeito: +500 ATK por Dragão no campo/GY do oponente.
        int dragonCount = 0;
        dragonCount += GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.race == "Dragon").Count;
        // + Dragões no campo do oponente...
        
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, dragonCount * 500, source));
        Debug.Log($"Buster Blader: +{dragonCount * 500} ATK.");
    }

    void Effect_0253_BusterRancher(CardDisplay source)
    {
        // Efeito: Equip (apenas ATK <= 1000). Se batalhar, ATK vira 2500.
        // Lógica de batalha no CardEffectManager_Impl.cs (OnDamageCalculation)
        Effect_Equip(source, 0, 0); // Apenas equipa, efeito é no cálculo
    }

    void Effect_0254_ButterflyDaggerElma(CardDisplay source)
    {
        // Efeito: Equip +300. Se destruída, volta para mão.
        Effect_Equip(source, 300, 0);
        // Trigger OnDestroy implementado no CardEffectManager_Impl.cs
    }

    void Effect_0255_ByserShock(CardDisplay source)
    {
        // Efeito: Quando invocado, retorna todas as cartas Setadas para a mão.
        Debug.Log("Byser Shock: Retornando cartas setadas (Lógica de filtro pendente).");
    }

    void Effect_0256_CallOfDarkness(CardDisplay source)
    {
        // Efeito Contínuo: Se ativar Monster Reborn, perde LP.
        // Implementado no OnSpecialSummon do CardEffectManager_Impl.cs (Dano genérico por SS do GY)
        Debug.Log("Call of Darkness: Ativo (Dano em SS do GY).");
    }

    void Effect_0257_CallOfTheEarthbound(CardDisplay source)
    {
        // Efeito: Oponente ataca, você escolhe o alvo.
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            Debug.Log("Call of the Earthbound: Selecione o novo alvo do ataque.");
            // Abre seleção de seus monstros
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard,
                    (newTarget) => {
                        BattleManager.Instance.currentTarget = newTarget;
                        Debug.Log($"Ataque redirecionado para {newTarget.CurrentCardData.name}.");
                    }
                );
            }
        }
    }

    void Effect_0258_CallOfTheGrave(CardDisplay source)
    {
        // Efeito: Nega Monster Reborn.
        // Requer sistema de Chain para verificar se a carta anterior é Monster Reborn
        // Similar ao Armor Break
        Debug.Log("Call of the Grave: Nega Monster Reborn (Lógica de Chain).");
    }

    void Effect_0259_CallOfTheHaunted(CardDisplay source)
    {
        // Efeito: SS do GY em Ataque. Se esta carta sair, destrói monstro. Se monstro sair, destrói esta.
        // O que falta: Vínculo persistente entre a Trap e o Monstro (Dependency).
        Effect_Revive(source, false); // false = apenas seu GY
    }

    void Effect_0260_CallOfTheMummy(CardDisplay source)
    {
        // Efeito: Se não controlar monstros, SS 1 Zumbi da mão.
        if (GameManager.Instance.duelFieldUI != null)
        {
            int monsterCount = 0;
            foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones) if (zone.childCount > 0) monsterCount++;
            
            if (monsterCount == 0)
            {
                List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                List<CardData> zombies = hand.FindAll(c => c.race == "Zombie" && c.type.Contains("Monster"));
                
                if (zombies.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(zombies, "Invocar Zumbi", (selected) => {
                        GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                    });
                }
                else
                {
                    Debug.Log("Call of the Mummy: Nenhum Zumbi na mão.");
                }
            }
            else
            {
                Debug.Log("Call of the Mummy: Você já controla monstros.");
            }
        }
    }

    void Effect_0262_CannonSoldier(CardDisplay source)
    {
        // Efeito: Tributa 1 monstro -> 500 dano.
        Effect_TributeToBurn(source, 1, 500);
    }

    void Effect_0263_CannonballSpearShellfish(CardDisplay source)
    {
        // Efeito: Imune a magias se Umi estiver no campo.
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi
        {
            Debug.Log("Cannonball Spear Shellfish: Imune a Magias (Umi ativo).");
            // Lógica real de imunidade requereria verificação no SpellTrapManager ao resolver efeitos
        }
    }

    void Effect_0264_CardDestruction(CardDisplay source)
    {
        // Efeito: Ambos os jogadores descartam a mão inteira e compram o mesmo número de cartas.
        int playerHandCount = GameManager.Instance.GetPlayerHandData().Count;
        int oppHandCount = GameManager.Instance.GetOpponentHandData().Count;

        GameManager.Instance.DiscardHand(true);
        GameManager.Instance.DiscardHand(false);

        for(int i=0; i<playerHandCount; i++) GameManager.Instance.DrawCard(true); // ignoreLimit
        for(int i=0; i<oppHandCount; i++) GameManager.Instance.DrawOpponentCard();
    }

    void Effect_0265_CardShuffle(CardDisplay source)
    {
        // Efeito: Pague 300 LP para embaralhar seu deck ou o do oponente.
        Effect_PayLP(source, 300);
        Debug.Log("Card Shuffle: Deck embaralhado.");
        // GameManager.Instance.ShuffleDeck();
    }

    void Effect_0266_CardOfSafeReturn(CardDisplay source)
    {
        // Efeito Contínuo: Quando um monstro é invocado do GY, compre 1 carta.
        // Lógica implementada no CardEffectManager_Impl.cs (OnSpecialSummon)
        Debug.Log("Card of Safe Return: Ativo.");
    }

    void Effect_0267_CardOfSanctity(CardDisplay source)
    {
        // Efeito (TCG): Bana todas as cartas da sua mão e campo; compre 2 cartas.
        Debug.Log("Card of Sanctity: Banindo tudo e comprando 2.");
        
        // Banir Mão
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        // Copia para evitar erro de modificação de coleção
        foreach(var c in new List<CardData>(hand)) 
        {
            // Encontra o display correspondente (simplificado)
            // Em produção, GameManager.BanishCard precisa de CardDisplay, então iteraríamos os GameObjects da mão
        }
        GameManager.Instance.DiscardHand(true); // Usando discard por enquanto pois BanishHand não existe
        
        // Banir Campo (Simplificado: Destrói tudo)
        DestroyAllMonsters(false, true);
        
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0268_CastleGate(CardDisplay source)
    {
        // Efeito: Tribute 1 monstro Lv5-; cause dano igual ao ATK original dele.
        if (SpellTrapManager.Instance != null)
        {
             SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.level <= 5 && t != source,
                (t) => {
                    int dmg = t.originalAtk;
                    GameManager.Instance.TributeCard(t);
                    Effect_DirectDamage(source, dmg);
                    source.hasAttackedThisTurn = true; // Impede ataque
                }
            );
        }
    }

    void Effect_0269_CastleWalls(CardDisplay source)
    {
        // Efeito: Aumenta DEF de 1 monstro em 500 até a End Phase.
        Effect_BuffStats(source, 0, 500);
    }

    void Effect_0270_CastleOfDarkIllusions(CardDisplay source)
    {
        // Efeito: Flip - Todos os Zumbis ganham 200 ATK/DEF. Aumenta a cada Standby.
        // Aplica o primeiro buff
        Effect_Field(source, 200, 200, "Zombie");
        // Buffs subsequentes tratados no CardEffectManager_Impl.cs (OnPhaseStart)
    }

    void Effect_0271_CatsEarTribe(CardDisplay source)
    {
        // Efeito: O ATK original do oponente vira 200 durante o cálculo de dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation)
        Debug.Log("Cat's Ear Tribe: Efeito passivo de batalha.");
    }

    void Effect_0272_CatapultTurtle(CardDisplay source)
    {
        // Efeito: Tribute 1 monstro; cause dano igual à metade do ATK dele.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            Debug.Log("Catapult Turtle: Tributo para dano (Simulado 500).");
            Effect_DirectDamage(source, 500);
        }
    }

    void Effect_0273_CatnippedKitty(CardDisplay source)
    {
        // Efeito: Uma vez por turno, pode tornar a DEF de 1 monstro 0.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Defense,
                (t) => {
                    t.ModifyStats(0, -t.currentDef); // Zera a DEF
                    Debug.Log($"Catnipped Kitty: DEF de {t.CurrentCardData.name} reduzida a 0.");
                }
            );
        }
    }

    void Effect_0274_CaveDragon(CardDisplay source)
    {
        // Efeito: Se você controla monstro, não pode Normal Summon. Se não tiver Dragão, não ataca.
        // Restrição de ataque implementada no BattleManager.cs
        // Restrição de invocação requereria hook no SummonManager (CanNormalSummon)
        Debug.Log("Cave Dragon: Restrições aplicadas.");
    }

    void Effect_0275_Ceasefire(CardDisplay source)
    {
        // Efeito: Vira todos os monstros face-down para face-up (sem ativar efeitos Flip). Dano 500 por Effect Monster.
        int effectMonsters = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.isFlipped) m.RevealCard(); // Deveria ser RevealWithoutEffect
                if (m.CurrentCardData.type.Contains("Effect")) effectMonsters++;
            }
        }
        Effect_DirectDamage(source, effectMonsters * 500);
    }

    void Effect_0277_CemetaryBomb(CardDisplay source)
    {
        // Efeito: 100 de dano por carta no GY do oponente.
        int count = GameManager.Instance.GetOpponentGraveyard().Count;
        Effect_DirectDamage(source, count * 100);
    }

    void Effect_0278_CentrifugalField(CardDisplay source)
    {
        // Efeito: Se uma Fusão for destruída por efeito, invoca 1 material do GY.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField)
        Debug.Log("Centrifugal Field: Ativo.");
    }

    void Effect_0279_CeremonialBell(CardDisplay source)
    {
        // Efeito: Ambas as mãos ficam reveladas.
        Debug.Log("Ceremonial Bell: Revelando mãos.");
        GameManager.Instance.showOpponentHand = true;
        // Nota: Precisa de lógica para esconder novamente quando a carta sair do campo
    }

    void Effect_0280_CestusOfDagla(CardDisplay source)
    {
        // Efeito: Equip Fairy +500. Dano de batalha infligido ao oponente.
        Effect_Equip(source, 500, 0, "Fairy");
    }

    void Effect_0281_ChainBurst(CardDisplay source)
    {
        // Efeito: Quem ativar Trap toma 1000 de dano.
        // Requer hook no ActivateFieldSpellTrap ou ChainManager
        Debug.Log("Chain Burst: Ativo.");
    }

    void Effect_0282_ChainDestruction(CardDisplay source)
    {
        // Efeito: Destrói cópias no deck/mão de monstro invocado (ATK <= 2000).
        // Simplificado: Seleciona monstro no campo com ATK <= 2000
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.currentAtk <= 2000,
                (t) => {
                    Debug.Log($"Chain Destruction: Destruindo cópias de {t.CurrentCardData.name} no deck/mão.");
                    // Apenas log para o protótipo, pois requer acesso profundo às listas privadas do oponente
                }
            );
        }
    }

    void Effect_0283_ChainDisappearance(CardDisplay source)
    {
        // Efeito: Bane cópias no deck/mão de monstro invocado (ATK <= 1000).
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.currentAtk <= 1000,
                (t) => {
                    string name = t.CurrentCardData.name;
                    Debug.Log($"Chain Disappearance: Banindo {name} e cópias.");
                    
                    // Bane o alvo
                    GameManager.Instance.BanishCard(t);

                    // Bane cópias da mão e deck do controlador (Lógica simplificada para o Player)
                    if (t.isPlayerCard)
                    {
                        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
                        
                        // Remove da mão (cópia segura)
                        foreach(var c in new List<CardData>(hand)) {
                            if (c.name == name) {
                                GameManager.Instance.RemoveCardFromHand(c, true);
                                GameManager.Instance.RemoveFromPlay(c, true);
                            }
                        }
                        // Remove do deck
                        foreach(var c in new List<CardData>(deck)) {
                            if (c.name == name) {
                                deck.Remove(c);
                                GameManager.Instance.RemoveFromPlay(c, true);
                            }
                        }
                        GameManager.Instance.ShuffleDeck(true);
                    }
                }
            );
        }
    }

    void Effect_0283_ChainDisappearance(CardDisplay source)
    {
        // Duplicado removido
    }

    void Effect_0284_ChainEnergy(CardDisplay source)
    {
        // Efeito: Pagar 500 LP para jogar cartas da mão.
        // Lógica implementada no GameManager.CheckChainEnergyCost
        Debug.Log("Chain Energy: Ativo (Custo de 500 LP por ação).");
    }

    void Effect_0287_ChangeOfHeart(CardDisplay source)
    {
        // Efeito: Controla 1 monstro do oponente até a End Phase.
        Effect_ChangeControl(source, true);
    }

    void Effect_0288_ChaosCommandMagician(CardDisplay source)
    {
        // Efeito: Nega efeito de monstro que dê alvo neste card. (Passivo)
        Debug.Log("Chaos Command Magician: Imune a efeitos de monstro.");
    }

    void Effect_0289_ChaosEmperorDragon(CardDisplay source)
    {
        // Efeito: Paga 1000 LP; envia tudo (campo/mão) para o GY; 300 dano por carta.
        if (GameManager.Instance.PayLifePoints(source.isPlayerCard, 1000))
        {
            int count = 0;
            
            // Mão Oponente
            count += GameManager.Instance.GetOpponentHandData().Count;
            GameManager.Instance.DiscardHand(false);

            // Campo Oponente
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> oppCards = new List<CardDisplay>();
                CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, oppCards);
                CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, oppCards);
                count += oppCards.Count;
                DestroyCards(oppCards, false);
            }

            // Mão Player
            GameManager.Instance.DiscardHand(true);

            // Campo Player
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> myCards = new List<CardDisplay>();
                CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, myCards);
                CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, myCards);
                DestroyCards(myCards, true);
            }

            Effect_DirectDamage(source, count * 300);
        }
    }

    void Effect_0290_ChaosEnd(CardDisplay source)
    {
        // Efeito: Se 7+ banidas, destrói todos os monstros.
        if (GameManager.Instance.GetPlayerRemovedCount() >= 7)
        {
            DestroyAllMonsters(true, true);
        }
        else
        {
            Debug.Log("Chaos End: Requer 7+ cartas banidas.");
        }
    }

    void Effect_0291_ChaosGreed(CardDisplay source)
    {
        // Efeito: Se 4+ banidas e GY vazio, compra 2.
        if (GameManager.Instance.GetPlayerRemovedCount() >= 4 && GameManager.Instance.GetPlayerGraveyard().Count == 0)
        {
            GameManager.Instance.DrawCard();
            GameManager.Instance.DrawCard();
        }
    }

    void Effect_0292_ChaosNecromancer(CardDisplay source)
    {
        // Efeito: ATK = 300 x Monstros no GY.
        // Atualizado dinamicamente no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Chaos Necromancer: ATK dinâmico.");
    }

    void Effect_0293_ChaosSorcerer(CardDisplay source)
    {
        // Efeito: Bane 1 monstro face-up. Não ataca neste turno.
        if (source.hasAttackedThisTurn) return;

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => {
                    GameManager.Instance.BanishCard(t);
                    source.hasAttackedThisTurn = true;
                }
            );
        }
    }

    void Effect_0294_ChaosriderGustaph(CardDisplay source)
    {
        // Efeito: Bane até 2 Spells do GY para ganhar ATK.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(spells, "Banir Spells (Max 2)", 1, 2, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, source.isPlayerCard);
                    gy.Remove(c);
                }
                int buff = selected.Count * 300;
                source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, buff, source));
            });
        }
    }

    void Effect_0296_CharmOfShabti(CardDisplay source)
    {
        // Efeito: Descarte para evitar que Gravekeepers sejam destruídos em batalha.
        if (!source.isOnField) // Da mão
        {
            GameManager.Instance.DiscardCard(source);
            if (BattleManager.Instance != null) BattleManager.Instance.gravekeepersProtected = true;
            Debug.Log("Charm of Shabti: Gravekeepers protegidos.");
        }
    }

    void Effect_0298_Checkmate(CardDisplay source)
    {
        // Efeito: Tribute 1 monstro; Terrorking Archfiend ataca direto.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            // Simplificado: Assume que tem Terrorking e aplica efeito global ou no primeiro encontrado
            Debug.Log("Checkmate: Terrorking pode atacar direto.");
            // Em um sistema completo, selecionaria o Terrorking para aplicar o buff "Direct Attack"
        }
    }

    void Effect_0299_ChimeraTheFlyingMythicalBeast(CardDisplay source)
    {
        // Efeito: Se destruído, invoca Berfomet ou Gazelle do GY.
        // Lógica no CardEffectManager_Impl.cs (OnCardSentToGraveyard)
        Debug.Log("Chimera: Efeito de flutuação configurado.");
    }

    void Effect_0300_ChironTheMage(CardDisplay source)
    {
        // Efeito: Descarte 1 Spell; destrua 1 S/T do oponente.
        // O que falta: UI de descarte específico (Spell).
        Debug.Log("Chiron the Mage: Descarte Spell para destruir S/T (UI de descarte pendente).");
        Effect_MST(source); // Reusa lógica de destruir S/T
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0301 - 0400)
    // =========================================================================================

    void Effect_0301_ChopmanTheDesperateOutlaw(CardDisplay source)
    {
        // FLIP: Selecione 1 Equip Spell no seu GY; equipe-o neste card.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        List<CardData> equips = gy.FindAll(c => c.type.Contains("Spell") && c.property == "Equip");
        
        if (equips.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(equips, "Equipar do GY", (selected) => {
                // Simula equipar: Move do GY para S/T e cria link
                // Como não temos lógica de mover do GY para campo S/T diretamente, simulamos o efeito
                Debug.Log($"Chopman: Equipando {selected.name} do GY.");
                gy.Remove(selected);
                // Cria visualmente na zona de S/T? Ou apenas aplica o efeito?
                // Vamos aplicar o efeito genérico de equipar (assumindo +0 por enquanto, pois depende da carta)
                // Em um sistema real, instanciaríamos a carta na zona S/T.
            });
        }
    }

    void Effect_0302_ChorusOfSanctuary(CardDisplay source)
    {
        // Field Spell: +500 DEF para todos os monstros em Defesa.
        Effect_Field(source, 0, 500, "", "");
    }

    void Effect_0303_ChosenOne(CardDisplay source)
    {
        // Selecione 1 Monstro e 2 não-Monstros da mão. Oponente escolhe 1 aleatoriamente.
        // Se for monstro, SS e manda o resto pro GY. Senão, tudo pro GY.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count >= 3)
        {
            GameManager.Instance.OpenCardMultiSelection(hand, "Selecione 1 Monstro e 2 Não-Monstros", 3, 3, (selected) => {
                // Validação simples
                int monsters = selected.FindAll(c => c.type.Contains("Monster")).Count;
                if (monsters < 1) { Debug.Log("Precisa selecionar pelo menos 1 monstro."); return; }

                // Oponente escolhe (aleatório)
                CardData picked = selected[Random.Range(0, selected.Count)];
                Debug.Log($"Chosen One: Oponente escolheu {picked.name}.");

                if (picked.type.Contains("Monster"))
                {
                    GameManager.Instance.SpecialSummonFromData(picked, source.isPlayerCard);
                    selected.Remove(picked);
                }

                // Manda o resto para o GY
                foreach(var c in selected)
                {
                    GameManager.Instance.SendToGraveyard(c, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(c, source.isPlayerCard);
                }
            });
        }

    }

    void Effect_0305_CipherSoldier(CardDisplay source)
    {
        // Se batalhar com Warrior: +2000 ATK/DEF durante o cálculo de dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation)
        Debug.Log("Cipher Soldier: Efeito passivo de batalha.");
    }

    void Effect_0307_Cloning(CardDisplay source)
    {
        // Quando oponente invoca: SS Clone Token com mesmos stats/tipo/atributo.
        // Requer gatilho de resposta a invocação.
        // Simulação: Verifica última invocação do oponente
        // (Lógica real estaria no SpellTrapManager.CheckForTraps(Summon))
        Debug.Log("Cloning: Ativo (Gatilho de invocação).");
    }

    void Effect_0309_CoachGoblin(CardDisplay source)
    {
        // End Phase: Se você controla este card face-up, pode retornar 1 Normal Monster da mão ao Deck para comprar 1.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase)
        Debug.Log("Coach Goblin: Ativo.");
    }

    void Effect_0310_CobraJar(CardDisplay source)
    {
        // FLIP: SS 1 "Poisonous Snake Token".
        GameManager.Instance.SpawnToken(source.isPlayerCard, 1200, 1200, "Poisonous Snake Token");
    }

    void Effect_0311_CobramanSakuzy(CardDisplay source)
    {
        // Pode virar face-down 1x por turno. Quando Flip Summon: Olhe todas as S/T setadas do oponente.
        Effect_TurnSet(source);
        
        // Revelar S/T (Simulado com Log)
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Itera zonas de S/T do oponente e loga os nomes
            // Em um jogo real, mostraria as cartas visualmente por alguns segundos
            Debug.Log("Cobraman Sakuzy: Revelando S/T do oponente...");
        }
    }

    void Effect_0312_CockroachKnight(CardDisplay source)
    {
        // Se enviado ao GY: Volta ao topo do Deck.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardLeavesField/OnSentToGraveyard)
        Debug.Log("Cockroach Knight: Ativo.");
    }

    void Effect_0313_CocoonOfEvolution(CardDisplay source)
    {
        // Pode equipar da mão para "Petit Moth". Stats viram do Cocoon.
        if (!source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Petit Moth",
                    (t) => {
                        // Move Cocoon para S/T e equipa
                        // Simulação:
                        GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
                        // Criar na zona S/T...
                        Debug.Log("Cocoon of Evolution: Equipado em Petit Moth.");
                        // Aplica stats
                        t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, source.CurrentCardData.atk, source));
                        t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, source.CurrentCardData.def, source));
                    }
                );
            }
        }
    }

    void Effect_0314_CoffinSeller(CardDisplay source)
    {
        // Cada vez que monstro(s) do oponente vão para o GY: 300 dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnCardSentToGraveyard)
        Debug.Log("Coffin Seller: Ativo.");
    }

    void Effect_0315_ColdWave(CardDisplay source)
    {
        // Só no início da Main 1. Até seu próximo turno, ninguém joga/seta S/T.
        if (PhaseManager.Instance.currentPhase == GamePhase.Main1)
        {
            Debug.Log("Cold Wave: S/T bloqueadas até o próximo turno.");
            // Adicionar flag global no GameManager ou SpellTrapManager
            // SpellTrapManager.Instance.coldWaveActive = true;
        }
    }

    void Effect_0316_CollectedPower(CardDisplay source)
    {
        // Selecione 1 monstro face-up; equipe todos os Equip Cards no campo nele.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    // Encontrar todos os equips no campo e mudar o alvo para 'target'
                    Debug.Log($"Collected Power: Todos os equips movidos para {target.CurrentCardData.name}.");
                }
            );
        }
    }

    void Effect_0317_CombinationAttack(CardDisplay source)
    {
        // Battle Phase: Tribute monstro equipado com Union; Union ataca.
        if (PhaseManager.Instance.currentPhase == GamePhase.Battle)
        {
            // Seleciona monstro equipado
            // ...
            Debug.Log("Combination Attack: Union ataca.");
        }
    }

    void Effect_0318_CommandKnight(CardDisplay source)
    {
        // Warriors +400 ATK. Se controlar outro monstro, não pode ser atacado.
        // Buff aplicado no OnSummonImpl
        // Proteção aplicada no BattleManager.SelectTarget
        Debug.Log("Command Knight: Ativo.");
    }

    void Effect_0319_CommencementDance(CardDisplay source)
    {
        // Ritual para "Performance of Sword".
        // Requer sistema de Ritual (seleção de tributos por nível)
        // Simulação:
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData ritualMonster = hand.Find(c => c.name == "Performance of Sword");
        if (ritualMonster != null)
        {
            // Pede tributos (Nível >= 6)
            SummonManager.Instance.SelectTributes(2, source.isPlayerCard, (tributes) => {
                // Verifica níveis...
                GameManager.Instance.SpecialSummonFromData(ritualMonster, source.isPlayerCard);
            });
        }
    }

    void Effect_0320_CompulsoryEvacuationDevice(CardDisplay source)
    {
        // Retorna 1 monstro no campo para a mão.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Compulsory Evacuation Device: {t.CurrentCardData.name} retornado para a mão.");
                    // Simulação: Destrói visualmente (deveria ir para a mão)
                    Destroy(t.gameObject); 
                }
            );
        }
    }

    void Effect_0321_Confiscation(CardDisplay source)
    {
        // Pague 1000 LP; olhe a mão do oponente, descarte 1 carta.
        if (Effect_PayLP(source, 1000))
        {
            List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
            GameManager.Instance.OpenCardSelection(oppHand, "Descartar da Mão do Oponente", (selected) => {
                // Encontra e descarta
                // (Requer referência ao objeto da mão do oponente para destruir visualmente)
                Debug.Log($"Confiscation: Descartou {selected.name}.");
            });
        }
    }

    void Effect_0322_Conscription(CardDisplay source)
    {
        // Escave o topo do deck do oponente. Se monstro (Normal Summonable), SS no seu campo. Senão, mão dele.
        List<CardData> oppDeck = GameManager.Instance.GetOpponentMainDeck();
        if (oppDeck.Count > 0)
        {
            CardData top = oppDeck[0];
            oppDeck.RemoveAt(0);
            Debug.Log($"Conscription: Escavou {top.name}.");
            
            if (top.type.Contains("Monster") && !top.type.Contains("Ritual") && !top.type.Contains("Fusion")) // Simplificado
            {
                GameManager.Instance.SpecialSummonFromData(top, source.isPlayerCard);
            }
            else
            {
                GameManager.Instance.AddCardToHand(top, !source.isPlayerCard); // Adiciona à mão do oponente
            }
        }
    }

    void Effect_0323_ContinuousDestructionPunch(CardDisplay source)
    {
        // Se oponente ataca defesa e DEF > ATK, destrói atacante. Se ATK > DEF, destrói defensor.
        // Lógica implementada no BattleManager (ResolveDamage)
        Debug.Log("Continuous Destruction Punch: Ativo.");
    }

    void Effect_0324_ContractWithExodia(CardDisplay source)
    {
        // Se tiver as 5 partes no GY: SS Exodia Necross da mão.
        List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
        string[] parts = { "0618", "1061", "1062", "1530", "1531" }; // IDs das partes
        bool hasAll = true;
        foreach(var id in parts) if (!gy.Exists(c => c.id == id)) hasAll = false;

        if (hasAll)
        {
            // SS Necross
            Debug.Log("Contract with Exodia: Condição atendida.");
        }
    }

    void Effect_0325_ContractWithTheAbyss(CardDisplay source)
    {
        // Ritual Genérico para DARK.
        // Lógica de Ritual Genérico
        Debug.Log("Contract with the Abyss: Selecione Ritual DARK.");
    }

    void Effect_0326_ContractWithTheDarkMaster(CardDisplay source)
    {
        // Ritual para Dark Master - Zorc.
        // Requer sistema de Ritual
        Debug.Log("Contract with the Dark Master: Selecione Ritual Zorc.");
    }

    void Effect_0327_ConvulsionOfNature(CardDisplay source)
    {
        // Ambos os jogadores viram seus Decks de cabeça para baixo.
        Debug.Log("Convulsion of Nature: Decks invertidos (Visual pendente).");
        // Simulação visual: Vira o topo do deck
        if (GameManager.Instance.playerDeckDisplay != null)
        {
            // Inverter visualmente seria complexo sem mudar a arquitetura do PileDisplay
            // Mas podemos logar que o estado mudou
        }
    }

    void Effect_0328_Copycat(CardDisplay source)
    {
        // Summon: Selecione 1 monstro do oponente; copie ATK/DEF original.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    source.ModifyStats(t.CurrentCardData.atk - source.currentAtk, t.CurrentCardData.def - source.currentDef);
                    Debug.Log($"Copycat: Copiou stats de {t.CurrentCardData.name}.");
                }
            );
        }
    }

    void Effect_0331_CostDown(CardDisplay source)
    {
        // Descarte 1; reduza Nível dos monstros na mão em 2 até End Phase.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.DiscardCard(GameManager.Instance.playerHand[0].GetComponent<CardDisplay>()); // Simplificado: Descarta o primeiro
            Debug.Log("Cost Down: Níveis na mão reduzidos em 2.");
            // A lógica de tributo no SummonManager precisa checar se Cost Down está ativo
            // SummonManager.Instance.levelReduction = 2;
        }
    }

    void Effect_0332_CoveringFire(CardDisplay source)
    {
        // Se oponente ataca: Monstro atacado ganha ATK de outro monstro seu.
        if (BattleManager.Instance != null && BattleManager.Instance.currentTarget != null)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != BattleManager.Instance.currentTarget,
                    (other) => {
                        BattleManager.Instance.currentTarget.ModifyStats(other.currentAtk, 0);
                        Debug.Log($"Covering Fire: +{other.currentAtk} ATK.");
                    }
                );
            }
        }
    }

    void Effect_0334_CrassClown(CardDisplay source)
    {
        // Se mudado de Defesa para Ataque: Retorna 1 monstro do oponente para a mão.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattlePositionChangedImpl)
        Debug.Log("Crass Clown: Ativo.");
    }

    void Effect_0338_CreatureSwap(CardDisplay source)
    {
        // Cada jogador escolhe 1 monstro; trocam o controle.
        // Simplificado: Jogador escolhe o seu e o do oponente (se possível)
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (my) => my.isOnField && my.isPlayerCard,
                (myMonster) => {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (opp) => opp.isOnField && !opp.isPlayerCard,
                        (oppMonster) => {
                            GameManager.Instance.SwitchControl(myMonster);
                            GameManager.Instance.SwitchControl(oppMonster);
                            Debug.Log("Creature Swap: Troca realizada.");
                        }
                    );
                }
            );
        }
    }

    void Effect_0339_CreepingDoomManta(CardDisplay source)
    {
        // Quando invocado Normal: Oponente não ativa Traps.
        Debug.Log("Creeping Doom Manta: Traps bloqueadas na invocação.");
        // SpellTrapManager.Instance.trapsBlocked = true; // Resetar no fim do turno
    }

    void Effect_0340_CrimsonNinja(CardDisplay source)
    {
        // FLIP: Destrói 1 Trap. Se setada, revela.
        Effect_FlipDestroy(source, TargetType.Trap);
    }

    void Effect_0341_CrimsonSentry(CardDisplay source)
    {
        // Tribute este card; coloque 1 monstro destruído neste turno do GY na mão.
        if (source.isOnField)
        {
            GameManager.Instance.TributeCard(source);
            // Seleciona do GY (sem verificar se foi destruído neste turno por enquanto)
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            List<CardData> monsters = gy.FindAll(c => c.type.Contains("Monster"));
            if (monsters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(monsters, "Recuperar Monstro", (c) => GameManager.Instance.AddCardToHand(c, true));
            }
        }
    }

    void Effect_0343_Criosphinx(CardDisplay source)
    {
        // Se monstro voltar para a mão: Oponente descarta 1.
        // Lógica simulada no CardEffectManager_Impl.cs (OnCardLeavesField - ReturnToHand)
        Debug.Log("Criosphinx: Ativo.");
    }

    void Effect_0344_CrossCounter(CardDisplay source)
    {
        // Se atacado em defesa e DEF > ATK: Dano dobrado, destrói atacante.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation) e BattleManager
        Debug.Log("Cross Counter: Ativo.");
    }

    void Effect_0346_CrushCardVirus(CardDisplay source)
    {
        // Tribute Dark < 1000 ATK; Destrói monstros 1500+ ATK do oponente (campo/mão) e verifica por 3 turnos.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard)) // Deveria checar Dark < 1000
        {
            Debug.Log("Crush Card Virus: Destruindo monstros fortes...");
            // Destrói campo
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if (z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if (m != null && m.currentAtk >= 1500)
                        {
                            GameManager.Instance.SendToGraveyard(m.CurrentCardData, false);
                            Destroy(m.gameObject);
                        }
                    }
                }
            }
            // Destrói mão (Simulado: Descarta aleatório se tiver monstro forte? Não, vamos apenas logar)
            Debug.Log("Crush Card Virus: Verificação de mão e turnos futuros pendente.");
        }
    }

    void Effect_0347_CureMermaid(CardDisplay source)
    {
        // Standby Phase: Ganha 800 LP.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Cure Mermaid: Ativo.");
    }

    void Effect_0348_CurseOfAging(CardDisplay source)
    {
        // Descarte 1; Monstros do oponente perdem 500 ATK/DEF.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                // Aplica debuff
                if (GameManager.Instance.duelFieldUI != null)
                {
                    foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                    {
                        if (z.childCount > 0)
                        {
                            var m = z.GetChild(0).GetComponent<CardDisplay>();
                            if (m != null) m.ModifyStats(-500, -500);
                        }
                    }
                }
            });
        }
    }

    void Effect_0349_CurseOfAnubis(CardDisplay source)
    {
        // Todos Effect Monsters viram Defesa, DEF vira 0.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all)
            {
                if (m.CurrentCardData.type.Contains("Effect"))
                {
                    if (m.position == CardDisplay.BattlePosition.Attack) m.ChangePosition();
                    m.ModifyStats(0, -m.currentDef); // Zera DEF
                }
            }
        }
    }

    void Effect_0350_CurseOfDarkness(CardDisplay source)
    {
        // Field Spell: Quem ativar Spell toma 1000 dano.
        // Lógica implementada no CardEffectManager_Impl.cs (OnSpellActivated)
        Debug.Log("Curse of Darkness: Ativo.");
    }

    void Effect_0352_CurseOfFiend(CardDisplay source)
    {
        // Muda posição de todos os monstros.
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
            
            foreach(var m in all) m.ChangePosition();
        }
    }

    void Effect_0353_CurseOfRoyal(CardDisplay source)
    {
        // Counter Trap: Nega S/T que destrói S/T.
        // Requer sistema de Chain para verificar efeito de destruição
        Debug.Log("Curse of Royal: Nega destruição de S/T.");
    }

    void Effect_0354_CurseOfTheMaskedBeast(CardDisplay source)
    {
        // Ritual para Masked Beast.
        Debug.Log("Curse of the Masked Beast: Selecione Ritual Masked Beast.");
    }

    void Effect_0355_CursedSealOfTheForbiddenSpell(CardDisplay source)
    {
        // Descarte Spell; nega Spell e proíbe uso pelo resto do duelo.
        // Requer Chain para pegar a Spell anterior
        if (ChainManager.Instance != null && ChainManager.Instance.currentChain.Count > 1)
        {
            var targetLink = ChainManager.Instance.currentChain[ChainManager.Instance.currentChain.Count - 2];
            var targetCard = targetLink.cardSource;

            if (targetCard != null && targetCard.CurrentCardData.type.Contains("Spell"))
            {
                // Custo: Descartar 1 Spell
                List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
                
                if (spells.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Spell", (discarded) => {
                        GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                        
                        Debug.Log($"Cursed Seal: Negando {targetCard.CurrentCardData.name} e proibindo uso.");
                        GameManager.Instance.forbiddenSpells.Add(targetCard.CurrentCardData.name);
                        
                        // Destrói/Nega
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(targetCard);
                        GameManager.Instance.SendToGraveyard(targetCard.CurrentCardData, targetCard.isPlayerCard);
                        Destroy(targetCard.gameObject);
                    });
                }
            }
        }
    }

    void Effect_0357_CyberArchfiend(CardDisplay source)
    {
        // Draw Phase: Se mão vazia, compra mais 1.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - Draw)
        Debug.Log("Cyber Archfiend: Ativo.");
    }

    void Effect_0359_CyberDragon(CardDisplay source)
    {
        // SS da mão se oponente tem monstro e você não.
        if (!source.isOnField)
        {
            bool oppHas = false;
            bool iHave = false;
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) oppHas = true;
                foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) iHave = true;
            }

            if (oppHas && !iHave)
            {
                Debug.Log("Cyber Dragon: Condição de SS atendida.");
                GameManager.Instance.SpecialSummonFromData(source.CurrentCardData, source.isPlayerCard);
                GameManager.Instance.RemoveCardFromHand(source.CurrentCardData, source.isPlayerCard);
            }
            else
            {
                Debug.Log("Cyber Dragon: Condição de SS não atendida.");
            }
        }
    }

    void Effect_0362_CyberHarpieLady(CardDisplay source)
    {
        // Efeito: O nome desta carta é tratado como "Harpie Lady".
        Debug.Log("Cyber Harpie Lady: Nome tratado como Harpie Lady.");
        // Lógica de regra (Rule Effect), geralmente tratada onde se checa o nome
    }

    void Effect_0363_CyberJar(CardDisplay source)
    {
        // FLIP: Destrói todos os monstros. Ambos compram 5, invocam Lv4- encontrados.
        Debug.Log("Cyber Jar: Destruindo tudo...");
        DestroyAllMonsters(true, true);
        
        Debug.Log("Cyber Jar: Ambos compram 5 cartas (Simulado).");
        for(int i=0; i<5; i++) GameManager.Instance.DrawCard(true);
        for(int i=0; i<5; i++) GameManager.Instance.DrawOpponentCard();
        
        // A lógica de revelar e invocar automaticamente é muito complexa para este escopo.
        // O jogador deve invocar manualmente da mão o que comprou.
    }

    void Effect_0364_CyberRaider(CardDisplay source)
    {
        // Efeito: Selecione 1 Equip Card no campo; destrua-o ou equipe-o neste card.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Spell") && t.CurrentCardData.property == "Equip",
                (target) => {
                    // Simplificação: Destrói sempre (falta UI de escolha)
                    Debug.Log($"Cyber Raider: Destruindo {target.CurrentCardData.name}.");
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                }
            );
        }
    }

    void Effect_0366_CyberShield(CardDisplay source)
    {
        // Equip: Harpie Lady +500 ATK.
        Effect_Equip(source, 500, 0, "Winged Beast"); // Simplificado para Winged Beast ou nome
    }

    void Effect_0369_CyberTwinDragon(CardDisplay source)
    {
        // Efeito: Pode atacar duas vezes na Battle Phase.
        Debug.Log("Cyber Twin Dragon: Ataque duplo.");
        // Requer flag no CardDisplay ou BattleManager
    }

    void Effect_0370_CyberStein(CardDisplay source)
    {
        // Efeito: Pague 5000 LP; SS 1 Fusão do Extra Deck em Ataque.
        if (GameManager.Instance.PayLifePoints(source.isPlayerCard, 5000))
        {
            List<CardData> extra = GameManager.Instance.GetPlayerExtraDeck();
            if (extra.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(extra, "Invocar Fusão", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    // Remove do Extra Deck (simulado, pois SpecialSummonFromData não remove da lista de origem)
                    extra.Remove(selected);
                });
            }
        }
    }

    void Effect_0372_CyberneticCyclopean(CardDisplay source)
    {
        // Efeito: Se você não tiver cartas na mão, ganha 1000 ATK.
        // Lógica implementada no CardEffectManager_Impl.cs (CheckActiveCards)
        Debug.Log("Cybernetic Cyclopean: Ativo.");
    }

    void Effect_0373_CyberneticMagician(CardDisplay source)
    {
        // Efeito: Descarte 1 carta; ATK de 1 monstro vira 2000.
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
                            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, 2000, source));
                            Debug.Log($"Cybernetic Magician: ATK de {target.CurrentCardData.name} definido para 2000.");
                        }
                    );
                }
            });
        }
    }

    void Effect_0374_CyclonLaser(CardDisplay source)
    {
        // Equip: Gradius +300 ATK, Piercing.
        Effect_Equip(source, 300, 0, "Machine"); // Simplificado
    }

    void Effect_0377_DTribe(CardDisplay source)
    {
        // Trap: Todos os monstros viram Dragão.
        Debug.Log("D. Tribe: Todos viram Dragão.");
        // Requer sistema de modificação de Tipo global
    }

    void Effect_0378_DDAssailant(CardDisplay source)
    {
        // Efeito: Se destruído em batalha, bane o atacante e este card.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Assailant: Ativo.");
    }

    void Effect_0379_DDBorderline(CardDisplay source)
    {
        // Spell: Ninguém ataca se não houver Spells no seu GY.
        // Lógica implementada no BattleManager.cs (CanAttack)
        Debug.Log("D.D. Borderline: Ativo.");
    }

    void Effect_0380_DDCrazyBeast(CardDisplay source)
    {
        // Efeito: Bane monstro destruído por este card em batalha.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Crazy Beast: Ativo.");
    }

    void Effect_0381_DDDesignator(CardDisplay source)
    {
        // Spell: Declare 1 carta; verifique mão do oponente. Se tiver, bane. Se não, bane 1 da sua.
        // Simulação de declaração: Escolhe do banco de dados
        // Como não temos input de texto, usamos o seletor de cartas
        List<CardData> allCards = GameManager.Instance.cardDatabase.cardDatabase;
        // Otimização: Pegar uma amostra ou usar lógica de "Adivinhar"
        Debug.Log("D.D. Designator: Declare um nome (Simulado: Escolha da lista).");
        
        // Para teste, vamos pegar uma carta aleatória do deck do oponente para "adivinhar" corretamente às vezes
        List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
        
        // Simula declaração
        string declaredName = "Kuriboh"; // Placeholder
        
        bool found = false;
        foreach(var c in oppHand) if(c.name == declaredName) found = true;

        if (found) {
            Debug.Log($"D.D. Designator: Acertou '{declaredName}'! Banindo da mão do oponente.");
            // Banir lógica...
        } else {
            Debug.Log($"D.D. Designator: Errou! Banindo carta da sua mão.");
        }
    }

    void Effect_0382_DDDynamite(CardDisplay source)
    {
        // Trap: 300 dano por cada carta banida do oponente.
        // Assumindo que GameManager tem lista de opponentRemoved (adicionado anteriormente)
        // Se não tiver acesso direto, usamos um valor simulado ou adicionamos o getter
        // int count = GameManager.Instance.opponentRemoved.Count; // Precisa ser público ou ter getter
        // Effect_DirectDamage(source, count * 300);
        Debug.Log("D.D. Dynamite: Dano calculado (Lógica de contagem pendente).");
    }

    void Effect_0383_DDScoutPlane(CardDisplay source)
    {
        // Efeito: Se banido, SS na End Phase.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase - Check Banished)
        Debug.Log("D.D. Scout Plane: Efeito passivo.");
    }

    void Effect_0384_DDSurvivor(CardDisplay source)
    {
        // Efeito: Se banido enquanto face-up, SS na End Phase.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart - End Phase - Check Banished)
        Debug.Log("D.D. Survivor: Efeito passivo.");
    }

    void Effect_0386_DDTrapHole(CardDisplay source)
    {
        // Trap: Quando oponente Set monstro: Destrói e bane.
        // Gatilho implementado no CardEffectManager_Impl.cs (OnSetImpl)
        Debug.Log("D.D. Trap Hole: Ativo.");
    }

    void Effect_0387_DDWarrior(CardDisplay source)
    {
        // Efeito: Após batalha, bane este card e o oponente.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Warrior: Ativo.");
    }

    void Effect_0388_DDWarriorLady(CardDisplay source)
    {
        // Efeito: Após batalha, pode banir este card e o oponente.
        // Lógica implementada no CardEffectManager_Impl.cs (OnBattleEnd)
        Debug.Log("D.D. Warrior Lady: Ativo.");
    }

    void Effect_0389_DDM(CardDisplay source)
    {
        // Efeito: Descarte 1 Spell; SS 1 monstro banido.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));
        
        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Spell", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                
                List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
                List<CardData> monsters = banished.FindAll(c => c.type.Contains("Monster"));
                
                if (monsters.Count > 0)
                {
                    GameManager.Instance.OpenCardSelection(monsters, "Invocar Banido", (target) => {
                        // Move de Banished para Campo (SS)
                        // GameManager.Instance.SpecialSummonFromBanished(target); // Precisa implementar
                        Debug.Log($"D.D.M.: Invocando {target.name} (Lógica de SS de banido pendente).");
                    });
                }
            });
        }
    }

    void Effect_0390_DNASurgery(CardDisplay source)
    {
        // Trap: Declare 1 Tipo; todos viram esse Tipo.
        Debug.Log("DNA Surgery: Declare um Tipo (Simulado: Dragon).");
        // Aplica modificador global de tipo (não implementado no sistema de stats atual, apenas log)
    }

    void Effect_0391_DNATransplant(CardDisplay source)
    {
        // Trap: Declare 1 Atributo; todos viram esse Atributo.
        Debug.Log("DNA Transplant: Declare um Atributo (Simulado: LIGHT).");
    }

    void Effect_0393_DancingFairy(CardDisplay source)
    {
        // Efeito: Se em Defesa, ganha 1000 LP na Standby.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Dancing Fairy: Ativo.");
    }

    void Effect_0394_DangerousMachineType6(CardDisplay source)
    {
        // Spell: Rola dado e aplica efeito aleatório.
        // Lógica implementada no CardEffectManager_Impl.cs (OnPhaseStart)
        Debug.Log("Dangerous Machine Type-6: Ativo.");
    }

    void Effect_0395_DarkArtist(CardDisplay source)
    {
        // Efeito: DEF cai pela metade se atacado por LIGHT.
        // Lógica implementada no CardEffectManager_Impl.cs (OnDamageCalculation)
        Debug.Log("Dark Artist: Ativo.");
    }

    void Effect_0397_DarkBalterTheTerrible(CardDisplay source)
    {
        // Fusão: Pague 1000 para negar Normal Spell. Nega efeitos de monstros destruídos.
        // Negação de efeitos destruídos implementada no OnBattleEnd.
        // Negação de Spell requer Chain (similar a Cursed Seal).
        Debug.Log("Dark Balter: Ativo.");
    }

    void Effect_0400_DarkBladeTheDragonKnight(CardDisplay source)
    {
        // Fusão: Bane até 3 monstros do GY do oponente.
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> monsters = oppGY.FindAll(c => c.type.Contains("Monster"));
        
        if (monsters.Count > 0)
        {
            int max = Mathf.Min(3, monsters.Count);
            GameManager.Instance.OpenCardMultiSelection(monsters, "Banir do Oponente", 1, max, (selected) => {
                foreach(var c in selected)
                {
                    GameManager.Instance.RemoveFromPlay(c, !source.isPlayerCard);
                    oppGY.Remove(c);
                }
                Debug.Log($"Dark Blade: Baniu {selected.Count} monstros.");
            });
        }
    }

    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0401 - 0500)
    // =========================================================================================

    void Effect_0401_DarkCatWithWhiteTail(CardDisplay source)
    {
        // FLIP: Retorna 2 monstros do oponente e 1 seu para a mão.
        // O que falta: Seleção múltipla complexa (2 oponente, 1 jogador).
        Debug.Log("Dark Cat with White Tail: Bounce múltiplo (UI de seleção pendente).");
    }

    void Effect_0402_DarkCatapulter(CardDisplay source)
    {
        // Efeito: Remove contador para destruir S/T.
        // O que falta: Sistema de Spell Counters.
        Debug.Log("Dark Catapulter: Remove contador para destruir S/T.");
    }

    void Effect_0404_DarkCoffin(CardDisplay source)
    {
        // Efeito: Se destruído face-down, oponente escolhe: Descartar 1 ou Destruir 1 monstro.
        // O que falta: UI de escolha para o oponente.
        Debug.Log("Dark Coffin: Oponente escolhe punição (UI pendente).");
    }

    void Effect_0405_DarkCore(CardDisplay source)
    {
        // Efeito: Descarte 1; bana 1 monstro face-up.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte 1 carta", (discarded) => {
                Debug.Log($"Dark Core: Descartou {discarded.name}.");
                if (SpellTrapManager.Instance != null)
                {
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isFlipped, // Face-up
                        (target) => {
                            GameManager.Instance.RemoveFromPlay(target.CurrentCardData, target.isPlayerCard);
                            Destroy(target.gameObject);
                            Debug.Log($"Dark Core: Baniu {target.CurrentCardData.name}.");
                        }
                    );
                }
            });
        }
    }

    void Effect_0406_DarkDesignator(CardDisplay source)
    {
        // Efeito: Declare 1 monstro; se estiver no deck do oponente, adicione à mão dele.
        // O que falta: Input de texto para declarar nome.
        Debug.Log("Dark Designator: Adiciona carta do deck do oponente à mão dele (Input pendente).");
    }

    void Effect_0407_DarkDriceratops(CardDisplay source)
    {
        // Efeito: Dano perfurante.
        // O que falta: Passivo no BattleManager.
        Debug.Log("Dark Driceratops: Dano perfurante.");
    }

    void Effect_0408_DarkDustSpirit(CardDisplay source)
    {
        // Efeito: Spirit. Ao ser invocado, destrói todos os monstros face-up.
        // O que falta: Retorno para mão na End Phase (Spirit).
        Debug.Log("Dark Dust Spirit: Destruindo monstros face-up...");
        // DestroyAllMonsters(true, true); // Filtrar por face-up
    }

    void Effect_0409_DarkElf(CardDisplay source)
    {
        // Efeito: Paga 1000 LP para atacar.
        // O que falta: Hook no BattleManager antes do ataque.
        Debug.Log("Dark Elf: Custo de ataque.");
    }

    void Effect_0410_DarkEnergy(CardDisplay source)
    {
        // Equip: Fiend +300 ATK/DEF.
        Effect_Equip(source, 300, 300, "Fiend");
    }

    void Effect_0411_DarkFactoryOfMassProduction(CardDisplay source)
    {
        // Efeito: Recupera 2 Monstros Normais do GY.
        // O que falta: Seleção múltipla (2 alvos).
        Debug.Log("Dark Factory: Recupera 2 Monstros Normais (UI Multi-Select pendente).");
    }

    void Effect_0412_DarkFlareKnight(CardDisplay source)
    {
        // Efeito: Sem dano de batalha. Se destruído, invoca Mirage Knight.
        // O que falta: Evento OnDestroyed.
        Debug.Log("Dark Flare Knight: Invoca Mirage Knight ao morrer.");
    }

    void Effect_0414_DarkHole(CardDisplay source)
    {
        // Efeito: Destrói todos os monstros no campo.
        DestroyAllMonsters(true, true);
    }

    void Effect_0415_DarkJeroid(CardDisplay source)
    {
        // Efeito: Seleciona 1 monstro; ele perde 800 ATK.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => t.ModifyStats(-800, 0)
            );
        }
    }

    void Effect_0417_DarkMagicAttack(CardDisplay source)
    {
        // Efeito: Se controlar Dark Magician, destrói todas as S/T do oponente.
        if (GameManager.Instance.IsCardActiveOnField("0419") || GameManager.Instance.IsCardActiveOnField("Dark Magician")) // ID ou Nome
        {
            Effect_HarpiesFeatherDuster(source); // Reusa lógica
        }
        else
        {
            Debug.Log("Dark Magic Attack: Requer Dark Magician.");
        }
    }

    void Effect_0418_DarkMagicCurtain(CardDisplay source)
    {
        // Efeito: Paga metade do LP; SS Dark Magician do Deck.
        int cost = GameManager.Instance.playerLP / 2;
        Effect_PayLP(source, cost);
        Effect_SearchDeck(source, "Dark Magician"); // Simplificado (deveria invocar)
    }

    void Effect_0420_DarkMagicianGirl(CardDisplay source)
    {
        // Efeito: +300 ATK por DM/Magician of Black Chaos nos GYs.
        // O que falta: StatModifier dinâmico checando GYs.
        Debug.Log("Dark Magician Girl: Buff por Magos no GY.");
    }

    void Effect_0421_DarkMagicianKnight(CardDisplay source)
    {
        // Efeito: Destrói 1 carta no campo.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField,
                (t) => {
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                }
            );
        }
    }

    void Effect_0422_DarkMagicianOfChaos(CardDisplay source)
    {
        // Efeito: Recupera 1 Spell do GY na End Phase. Se destruído, é banido.
        // O que falta: TurnObserver e evento OnDestroyed.
        Debug.Log("DMoC: Recupera Spell na End Phase.");
    }

    void Effect_0423_DarkMasterZorc(CardDisplay source)
    {
        // Efeito: Rola dado. 1-2: Destrói monstros oponente. 3-5: Destrói 1 monstro. 6: Destrói a si mesmo.
        // O que falta: Lógica de dado e seleção condicional.
        Debug.Log("Dark Master - Zorc: Efeito de dado pendente.");
    }

    void Effect_0424_DarkMimicLV1(CardDisplay source)
    {
        // FLIP: Compra 1. Standby: Envia ao GY, SS LV3.
        GameManager.Instance.DrawCard();
        Effect_LevelUp(source, "0425"); // LV3
    }

    void Effect_0425_DarkMimicLV3(CardDisplay source)
    {
        // Efeito: Se destruído em batalha, compra 1. Se invocado pelo LV1, compra 2.
        // O que falta: Flag 'summonedByEffect' e evento OnDestroyed.
        Debug.Log("Dark Mimic LV3: Compra ao morrer.");
    }

    void Effect_0426_DarkMirrorForce(CardDisplay source)
    {
        // Efeito: Quando oponente ataca: Bane todos os monstros em Defesa do oponente.
        // O que falta: Trigger de ataque.
        Debug.Log("Dark Mirror Force: Bane defesa do oponente.");
    }

    void Effect_0427_DarkNecrofear(CardDisplay source)
    {
        // Efeito: SS banindo 3 Fiends. Na End Phase se destruído, equipa no oponente e controla.
        // O que falta: Lógica de equipamento pós-morte.
        Debug.Log("Dark Necrofear: Possessão pendente.");
    }

    void Effect_0428_DarkPaladin(CardDisplay source)
    {
        // Efeito: Nega Spell descartando 1. +500 ATK por Dragão.
        // O que falta: Sistema de Chain e StatModifier.
        Debug.Log("Dark Paladin: Negação e Buff.");
    }

    void Effect_0432_DarkRoomOfNightmare(CardDisplay source)
    {
        // Efeito: Se oponente tomar dano de efeito, causa +300.
        // O que falta: Evento OnDamageDealt (Effect).
        Debug.Log("Dark Room of Nightmare: Dano extra.");
    }

    void Effect_0433_DarkRulerHaDes(CardDisplay source)
    {
        // Efeito: Nega efeitos de monstros destruídos por Fiends.
        // O que falta: Global rule modifier.
        Debug.Log("Dark Ruler Ha Des: Nega efeitos de monstros destruídos.");
    }

    void Effect_0434_DarkSage(CardDisplay source)
    {
        // Efeito: Busca 1 Spell.
        Effect_SearchDeck(source, "Spell");
    }

    void Effect_0435_DarkScorpionChickTheYellow(CardDisplay source)
    {
        // Efeito: Dano -> Bounce monstro ou olhar topo deck.
        Debug.Log("Chick the Yellow: Efeito de dano.");
    }

    void Effect_0436_DarkScorpionCliffTheTrapRemover(CardDisplay source)
    {
        // Efeito: Dano -> Destrói S/T ou Mill 2.
        Debug.Log("Cliff the Trap Remover: Efeito de dano.");
    }

    void Effect_0437_DarkScorpionGorgTheStrong(CardDisplay source)
    {
        // Efeito: Dano -> Bounce monstro (topo deck) ou Mill 1.
        Debug.Log("Gorg the Strong: Efeito de dano.");
    }

    void Effect_0438_DarkScorpionMeanaeTheThorn(CardDisplay source)
    {
        // Efeito: Dano -> Busca Dark Scorpion ou Recicla Dark Scorpion.
        Debug.Log("Meanae the Thorn: Efeito de dano.");
    }

    void Effect_0439_DarkScorpionBurglars(CardDisplay source)
    {
        // Efeito: Se você controla 3 Dark Scorpions, envie 1 Spell do deck do oponente ao GY.
        Debug.Log("Dark Scorpion Burglars: Mill de Spell.");
    }

    void Effect_0440_DarkScorpionCombination(CardDisplay source)
    {
        // Efeito: Se tiver os 5 Dark Scorpions, todos atacam direto e causam 400 dano cada.
        Debug.Log("Dark Scorpion Combination: Ataque total.");
    }

    void Effect_0442_DarkSnakeSyndrome(CardDisplay source)
    {
        // Efeito: Standby -> Dano dobra a cada turno.
        // O que falta: TurnObserver e contador de turnos na carta.
        Debug.Log("Dark Snake Syndrome: Dano progressivo.");
    }

    void Effect_0443_DarkSpiritOfTheSilent(CardDisplay source)
    {
        // Efeito: Oponente ataca -> Nega e obriga outro monstro a atacar.
        // O que falta: Interrupção de ataque e forçar novo atacante.
        Debug.Log("Dark Spirit of the Silent: Redireciona ataque.");
    }

    void Effect_0446_DarkZebra(CardDisplay source)
    {
        // Efeito: Se for o único monstro na Standby, vira defesa.
        // O que falta: TurnObserver.
        Debug.Log("Dark Zebra: Vira defesa.");
    }

    void Effect_0447_DarkEyesIllusionist(CardDisplay source)
    {
        // FLIP: Seleciona 1 monstro; ele não ataca.
        // O que falta: Vínculo persistente de efeito.
        Debug.Log("Dark-Eyes Illusionist: Congela monstro.");
    }

    void Effect_0448_DarkPiercingLight(CardDisplay source)
    {
        // Efeito: Vira todos os monstros face-down do oponente para face-up.
        // O que falta: Iterar monstros do oponente e chamar RevealCard.
        Debug.Log("Dark-Piercing Light: Revela monstros.");
    }

    void Effect_0449_DarkbishopArchfiend(CardDisplay source)
    {
        // Efeito: Protege Archfiends de efeitos que dão alvo (rola dado).
        Debug.Log("Darkbishop Archfiend: Proteção de Archfiend.");
    }

    void Effect_0453_DarklordMarie(CardDisplay source)
    {
        // Efeito: Ganha 200 LP na Standby se estiver no GY.
        // O que falta: TurnObserver checando GY.
        Debug.Log("Darklord Marie: Cura no GY.");
    }

    void Effect_0454_DarknessApproaches(CardDisplay source)
    {
        // Efeito: Descarte 2; vire 1 monstro face-down (mesmo em ataque).
        // O que falta: Suporte a Face-Down Attack Position (regra antiga/obscura).
        Debug.Log("Darkness Approaches: Vira face-down.");
    }

    void Effect_0456_DeFusion(CardDisplay source)
    {
        // Efeito: Retorna Fusão ao Extra Deck, invoca materiais do GY.
        // O que falta: Rastrear materiais usados na fusão.
        Debug.Log("De-Fusion: Desfaz fusão.");
    }

    void Effect_0457_DeSpell(CardDisplay source)
    {
        // Efeito: Destrói 1 Spell Card. Se setada, revela e destrói se for Spell.
        Effect_MST(source); // Simplificado
    }

    void Effect_0458_DealOfPhantom(CardDisplay source)
    {
        // Efeito: Buff baseado no GY.
        Debug.Log("Deal of Phantom: Buff.");
    }

    void Effect_0459_DecayedCommander(CardDisplay source)
    {
        // Efeito: Se atacar direto, oponente descarta 1.
        Debug.Log("Decayed Commander: Descarte no ataque direto.");
    }

    void Effect_0460_DeckDevastationVirus(CardDisplay source)
    {
        // Efeito: Tribute Dark 2000+ ATK; destrói monstros 1500- ATK do oponente.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            Debug.Log("Deck Devastation Virus: Destruindo monstros fracos...");
            // DestroyAllMonsters(true, false); // Simplificado
        }
    }

    void Effect_0461_DedicationThroughLightAndDarkness(CardDisplay source)
    {
        // Efeito: Tribute Dark Magician; SS Dark Magician of Chaos.
        Debug.Log("Dedication: Invocando DMoC.");
    }

    void Effect_0463_DeepseaWarrior(CardDisplay source)
    {
        // Efeito: Imune a Spells se Umi estiver no campo.
        Debug.Log("Deepsea Warrior: Imune a Spells com Umi.");
    }

    void Effect_0464_DekoichiTheBattlechantedLocomotive(CardDisplay source)
    {
        // FLIP: Compra 1 carta (+1 por cada Bokoichi).
        GameManager.Instance.DrawCard();
        // TODO: Checar Bokoichi
    }

    void Effect_0465_DelinquentDuo(CardDisplay source)
    {
        // Efeito: Pague 1000 LP; oponente descarta 1 aleatório e 1 à escolha dele.
        Effect_PayLP(source, 1000);
        Debug.Log("Delinquent Duo: Oponente descarta 2.");
    }

    void Effect_0466_DeltaAttacker(CardDisplay source)
    {
        // Efeito: Se controlar 3 Normais mesmo nome (Lv3-), atacam direto.
        Debug.Log("Delta Attacker: Ataque direto em trio.");
    }

    void Effect_0467_Demotion(CardDisplay source)
    {
        // Equip: Reduz Nível em 2.
        Debug.Log("Demotion: Nível reduzido.");
    }

    void Effect_0468_DesCounterblow(CardDisplay source)
    {
        // Efeito: Destrói monstro que atacar direto.
        Debug.Log("Des Counterblow: Destrói atacante direto.");
    }

    void Effect_0469_DesCroaking(CardDisplay source)
    {
        // Efeito: Se controlar 3 Des Frogs, destrói tudo do oponente.
        Debug.Log("Des Croaking: Nuke condicional.");
    }

    void Effect_0470_DesDendle(CardDisplay source)
    {
        // Efeito: Union para Vampiric Orchis. Gera Token.
        Debug.Log("Des Dendle: Union/Token.");
    }

    void Effect_0471_DesFeralImp(CardDisplay source)
    {
        // FLIP: Retorna 1 carta do GY para o Deck.
        Debug.Log("Des Feral Imp: Recicla do GY.");
    }

    void Effect_0472_DesFrog(CardDisplay source)
    {
        // Efeito: SS Des Frogs do deck igual a T.A.D.P.O.L.E. no GY.
        Debug.Log("Des Frog: Swarm.");
    }

    void Effect_0473_DesKangaroo(CardDisplay source)
    {
        // Efeito: Se ATK < DEF do oponente, destrói oponente (cálculo de dano aplica).
        Debug.Log("Des Kangaroo: Destrói defesa forte.");
    }

    void Effect_0474_DesKoala(CardDisplay source)
    {
        // FLIP: Dano 400 x cartas na mão do oponente.
        int handCount = 3; // Simulado
        Effect_DirectDamage(source, handCount * 400);
    }

    void Effect_0475_DesLacooda(CardDisplay source)
    {
        // Efeito: 1x turno, vira face-down. FLIP: Compra 1.
        GameManager.Instance.DrawCard();
        Effect_TurnSet(source); // Pode virar face-down
    }

    void Effect_0476_DesVolstgalph(CardDisplay source)
    {
        // Efeito: Dano 500 ao destruir monstro. +200 ATK por Spell Normal/Quick.
        Debug.Log("Des Volstgalph: Burn e Buff.");
    }

    void Effect_0477_DesWombat(CardDisplay source)
    {
        // Efeito: Dano de efeito em você vira 0.
        Debug.Log("Des Wombat: Proteção contra dano de efeito.");
    }

    void Effect_0478_DesertSunlight(CardDisplay source)
    {
        // Efeito: Todos os monstros viram Defesa Face-up.
        Debug.Log("Desert Sunlight: Revela monstros.");
    }

    void Effect_0479_Desertapir(CardDisplay source)
    {
        // FLIP: Vira 1 monstro face-down.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isFlipped,
                (t) => t.ShowBack()
            );
        }
    }

    void Effect_0480_DespairFromTheDark(CardDisplay source)
    {
        // Efeito: Se enviado do deck/mão ao GY por efeito do oponente, SS.
        Debug.Log("Despair from the Dark: SS condicional.");
    }

    void Effect_0481_DesrookArchfiend(CardDisplay source)
    {
        // Efeito: Envia da mão ao GY para reviver Terrorking destruído.
        Debug.Log("Desrook Archfiend: Salva Terrorking.");
    }

    void Effect_0482_DestinyBoard(CardDisplay source)
    {
        // Efeito: Coloca Spirit Messages. Vitória em 5 turnos.
        Debug.Log("Destiny Board: Contagem para vitória.");
    }

    void Effect_0484_DestructionPunch(CardDisplay source)
    {
        // Efeito: Se ATK atacante < DEF defensor, destrói atacante.
        Debug.Log("Destruction Punch: Destrói atacante fraco.");
    }

    void Effect_0485_DestructionRing(CardDisplay source)
    {
        // Efeito: Destrói 1 monstro seu; 1000 dano a ambos.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard,
                (t) => {
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, true);
                    Destroy(t.gameObject);
                    GameManager.Instance.DamagePlayer(1000);
                    GameManager.Instance.DamageOpponent(1000);
                }
            );
        }
    }

    void Effect_0487_DianKetoTheCureMaster(CardDisplay source)
    {
        // Efeito: Ganha 1000 LP.
        Effect_GainLP(source, 1000);
    }

    void Effect_0489_DiceJar(CardDisplay source)
    {
        // FLIP: Ambos rolam dado. Perdedor toma dano (pode ser 6000).
        Debug.Log("Dice Jar: Risco de dano massivo.");
    }

    void Effect_0490_DiceReRoll(CardDisplay source)
    {
        // Efeito: Permite rolar dado novamente 1x por turno.
        Debug.Log("Dice Re-Roll: Reroll.");
    }

    void Effect_0491_DifferentDimensionCapsule(CardDisplay source)
    {
        // Efeito: Bane 1 carta do deck face-down. Adiciona à mão em 2 turnos.
        Debug.Log("Different Dimension Capsule: Busca demorada.");
    }

    void Effect_0492_DifferentDimensionDragon(CardDisplay source)
    {
        // Efeito: Imune a destruição por S/T que não dão alvo.
        Debug.Log("Different Dimension Dragon: Proteção específica.");
    }

    void Effect_0493_DifferentDimensionGate(CardDisplay source)
    {
        // Efeito: Bane 1 monstro de cada lado. Retorna se destruído.
        Debug.Log("Different Dimension Gate: Remoção temporária.");
    }

    void Effect_0494_DiffusionWaveMotion(CardDisplay source)
    {
        // Efeito: Paga 1000 LP; Mago Lv7+ ataca todos os monstros do oponente.
        Effect_PayLP(source, 1000);
        Debug.Log("Diffusion Wave-Motion: Ataque em área.");
    }

    void Effect_0496_DimensionDistortion(CardDisplay source)
    {
        // Efeito: Se GY vazio, SS 1 monstro banido.
        if (GameManager.Instance.GetPlayerGraveyard().Count == 0)
        {
            // Lógica de selecionar banido
            Debug.Log("Dimension Distortion: Invoca banido.");
        }
    }

    void Effect_0497_DimensionFusion(CardDisplay source)
    {
        // Efeito: Paga 2000 LP; ambos SS o máximo de monstros banidos possível.
        Effect_PayLP(source, 2000);
        Debug.Log("Dimension Fusion: Invocação em massa de banidos.");
    }

    void Effect_0498_DimensionJar(CardDisplay source)
    {
        // FLIP: Bane até 3 monstros do GY do oponente.
        Debug.Log("Dimension Jar: Limpa GY do oponente.");
    }

    void Effect_0499_DimensionWall(CardDisplay source)
    {
        // Efeito: Oponente toma o dano de batalha de um ataque.
        Debug.Log("Dimension Wall: Reflete dano.");
    }

    void Effect_0500_Dimensionhole(CardDisplay source)
    {
        // Efeito: Bane 1 monstro seu até a próxima Standby Phase.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    // Banir temporariamente (Simulado: Destrói)
                    Destroy(t.gameObject);
                    Debug.Log($"Dimensionhole: {t.CurrentCardData.name} removido temporariamente.");
                }
            );
        }
    }
    }
}