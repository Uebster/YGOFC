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
            // Lógica simplificada de tributo (pega os 2 primeiros disponíveis)
            // TODO: Implementar seleção manual via SummonManager.SelectTributes
            Effect_TributeToDraw(source, 2, 3);
        }
    }

    void Effect_0003_4StarredLadybugOfDoom(CardDisplay source)
    {
        // FLIP: Destrói todos os monstros Nível 4 do oponente
        Effect_FlipDestroyLevel(source, 4);
    }

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
        
        // Verificação de condição (simulada)
        // if (!GameManager.Instance.wasLevel8DestroyedThisTurn) return;

        int cost = source.isPlayerCard ? GameManager.Instance.playerLP / 2 : GameManager.Instance.opponentLP / 2;
        Effect_PayLP(source, cost);
        
        // Busca e invoca Berserk Dragon (ID 0168)
        // GameManager.Instance.SpecialSummonById("0168", source.isPlayerCard);
        Debug.Log("A Deal with Dark Ruler: Invocando Berserk Dragon!");
    }

    void Effect_0010_AFeatherOfThePhoenix(CardDisplay source)
    {
        // Descarte 1 carta, selecione 1 carta do GY e coloque no topo do Deck
        // TODO: UI de descarte
        Debug.Log("A Feather of the Phoenix: Descarte 1 carta (pendente).");
        
        // UI de seleção do GY
        List<CardData> gy = source.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
        GameManager.Instance.OpenCardSelection(gy, "Selecione carta para o topo do Deck", (selected) => {
            gy.Remove(selected);
            List<CardData> deck = source.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : null; // Acesso restrito ao deck oponente
            if (deck != null) deck.Insert(0, selected);
            Debug.Log($"{selected.name} retornada ao topo do deck.");
        });
    }

    void Effect_0011_AFeintPlan(CardDisplay source)
    {
        // Oponente não pode atacar monstros face-down neste turno
        // Define flag no BattleManager
        Debug.Log("A Feint Plan: Monstros face-down protegidos de ataque este turno.");
        // BattleManager.Instance.cannotAttackFaceDown = true;
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
                    // GameManager.Instance.ReturnToHand(t);
                    Destroy(t.gameObject); // Simplificado
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
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard);
            Destroy(source.gameObject);
            Debug.Log("A-Team: Tributado para negar Trap.");
        }
    }

    void Effect_0018_AbsoluteEnd(CardDisplay source)
    {
        // Neste turno, ataques do oponente tornam-se ataques diretos.
        Debug.Log("Absolute End: Seus monstros não podem ser atacados, oponente ataca direto.");
        // BattleManager.Instance.forceDirectAttack = true;
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
        Debug.Log("Abyss Soldier: Descarte Water (pendente).");
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField,
                (t) => {
                    Debug.Log($"Abyss Soldier: {t.CurrentCardData.name} retornada para a mão.");
                    Destroy(t.gameObject); // Simplificado (deveria ir para mão)
                }
            );
        }
    }

    void Effect_0022_AbyssalDesignator(CardDisplay source)
    {
        // Pague 1000 LP; declare Tipo e Atributo. Oponente envia 1 monstro correspondente da mão/deck ao GY.
        Effect_PayLP(source, 1000);
        Debug.Log("Abyssal Designator: Declare Tipo/Atributo (UI Pendente). Oponente envia ao GY.");
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
        // Requer Trigger no SummonManager.
        Debug.Log("Adhesion Trap Hole: ATK do monstro invocado reduzido pela metade.");
    }

    void Effect_0028_AfterTheStruggle(CardDisplay source)
    {
        // Main Phase 1: Destrói todos monstros que batalharam no turno anterior? Não, texto:
        // "Destroy all face-up monsters on the field that were involved in damage calculation..."
        // Requer histórico de batalha no BattleManager.
        Debug.Log("After the Struggle: Destruindo monstros que batalharam (Lógica de histórico pendente).");
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
        Debug.Log("Alligator's Sword Dragon: Verificando condição de ataque direto...");
        // Lógica no BattleManager
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
        Debug.Log("Amazoness Archers: Debuff global e ataque forçado.");
        // Iterar monstros oponente, mudar posição, reduzir ATK.
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
        Effect_PayLP(source, 1500);
        Debug.Log("Amazoness Chain Master: Custo pago. (Lógica de roubar carta da mão do oponente requer UI específica).");
        // Em um sistema completo, abriria a mão do oponente.
        // Por enquanto, apenas o custo é aplicado.
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
                (t) => t.isOnField && !t.isPlayerCard,
                (t) => {
                    int myAtk = source.CurrentCardData.atk;
                    int oppAtk = t.CurrentCardData.atk;
                    // Troca lógica (precisa de suporte a modificadores temporários)
                    Debug.Log($"Amazoness Spellcaster: Trocando {myAtk} com {oppAtk}.");
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
        Effect_Equip(source, 0, 0, "Machine"); // Simplificado
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
            Debug.Log("Ancient Lamp: Buscando La Jinn...");
            // Busca e invoca
        }
    }

    void Effect_0066_AncientTelescope(CardDisplay source)
    {
        // Veja as 5 cartas do topo do deck do oponente.
        Debug.Log("Ancient Telescope: Visualizando deck do oponente (Simulado).");
        // TODO: UI Popup com as cartas
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
        Debug.Log("Ante: Comparando níveis das cartas na mão...");
        // Lógica de comparação e dano
    }

    void Effect_0073_AntiRaigeki(CardDisplay source)
    {
        // Quando oponente ativa Raigeki: Negue, destrua todos monstros do oponente.
        Debug.Log("Anti Raigeki: Counter ativado!");
        DestroyAllMonsters(true, false); // Destrói oponente
    }

    void Effect_0074_AntiAircraftFlower(CardDisplay source)
    {
        // Tribute 1 Inseto; cause 800 dano.
        Effect_TributeToBurn(source, 1, 800, "Insect");
    }

    void Effect_0075_AntiSpell(CardDisplay source)
    {
        // Remova 2 Spell Counters; negue Magia.
        Debug.Log("Anti-Spell: Sistema de Spell Counters ainda não implementado.");
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
        // Passivo.
    }

    void Effect_0083_AquaSpirit(CardDisplay source)
    {
        // SS banindo 1 Water. Standby oponente: Muda posição de 1 monstro.
        Debug.Log("Aqua Spirit: Efeito de congelar posição.");
    }

    void Effect_0084_ArcanaKnightJoker(CardDisplay source)
    {
        // Quick: Descarte mesmo tipo (M/S/T) para negar efeito que dá alvo.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        if (hand.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(hand, "Descarte para negar", (selected) => {
                // Lógica de descarte visual pendente
                Debug.Log($"Arcana Knight Joker: Descartou {selected.name} para negar efeito.");
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
        Effect_PayLP(source, 500);
        
        List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
        if (deck.Count > 0)
        {
            CardData topCard = deck[0];
            deck.RemoveAt(0); // Remove do topo
            
            Debug.Log($"Archfiend's Oath: Carta escavada: {topCard.name}");
            // Como não temos input de texto, vamos simular que errou e vai pro GY (comum em scripts automáticos)
            // ou dar a carta se estiver em modo Dev.
            
            GameManager.Instance.SendToGraveyard(topCard, true);
            Debug.Log("Archfiend's Oath: Carta enviada ao Cemitério (Adivinhação simulada).");
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
        Debug.Log("Armor Break: Lógica de Counter Trap pendente.");
    }

    void Effect_0102_ArmorExe(CardDisplay source)
    {
        // Maintenance: Remove counter or destroy.
        Debug.Log("Armor Exe: Sistema de Contadores pendente.");
    }

    void Effect_0103_ArmoredGlass(CardDisplay source)
    {
        Debug.Log("Armored Glass: Imunidade a Equip Spells este turno.");
    }

    void Effect_0108_ArrayOfRevealingLight(CardDisplay source)
    {
        Debug.Log("Array of Revealing Light: Declarar Tipo (UI Pendente).");
    }

    void Effect_0109_ArsenalBug(CardDisplay source)
    {
        // Continuous: ATK = 2000. Se não tiver outro Inseto, ATK = 1000.
        Debug.Log("Arsenal Bug: Checagem contínua de Insetos pendente.");
    }

    void Effect_0110_ArsenalRobber(CardDisplay source)
    {
        // Oponente escolhe Equip do Deck e envia ao GY.
        Debug.Log("Arsenal Robber: Oponente seleciona Equip do Deck (Simulado: Envia o primeiro).");
        // Simulação futura: Buscar no deck do oponente e enviar.
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
        Debug.Log("B.E.S. Big Core: Sistema de Contadores (3) e destruição em batalha pendente.");
    }

    void Effect_0125_BESCrystalCore(CardDisplay source)
    {
        Debug.Log("B.E.S. Crystal Core: Sistema de Contadores e mudança de posição pendente.");
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
        // Target 1 Set card; reveal it. If Trap, force activation. If not, return to Set.
        Debug.Log("Bait Doll: Seleção de carta setada pendente.");
    }

    void Effect_0132_BalloonLizard(CardDisplay source)
    {
        Debug.Log("Balloon Lizard: Acumula contadores a cada Standby.");
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
        Debug.Log("Bazoo: Banir cartas do GY para buff (UI Multi-Select pendente).");
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
        Debug.Log("Beckoning Light: Troca de mão por Luz (Lógica complexa de contagem).");
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
        Debug.Log("Big Eye: Reordenar deck (UI complexa pendente).");
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
        Effect_SearchDeck(source, "Harpie Lady");
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
        // O que falta: UI de escolha de efeito.
        Debug.Log("BLS Envoy: Escolha efeito (Banir ou Ataque Duplo).");
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
        // O que falta: Sistema de Trigger de Batalha (OnAttack) que permita equipar monstros como se fossem Spells, e um TurnObserver para contar a Standby Phase.
        Debug.Log("Blast Sphere: Lógica de equipar ao atacante pendente.");
    }

    void Effect_0202_BlastWithChain(CardDisplay source)
    {
        // Efeito: Equip +500. Se destruído por efeito, destrói 1 carta.
        // O que falta: Sistema de Trigger de Destruição (OnDestroyed) para cartas S/T.
        Effect_Equip(source, 500, 0);
    }

    void Effect_0203_BlastingTheRuins(CardDisplay source)
    {
        // Efeito: Se houver 30+ cartas no GY, causa 3000 de dano.
        int gyCount = source.isPlayerCard ? GameManager.Instance.playerGraveyardDisplay.pileData.Count : GameManager.Instance.opponentGraveyardDisplay.pileData.Count;
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
        // O que falta: Sistema de Eventos Globais (GlobalEventManager) que dispare 'OnCardDiscarded' para cartas contínuas ouvirem.
        Debug.Log("Blessings of the Nile: Ativo. (Aguardando sistema de eventos de descarte).");
    }

    void Effect_0206_BlindDestruction(CardDisplay source)
    {
        // Efeito Contínuo: Na Standby Phase, rola dado. Destrói monstros com Nível = Dado.
        // O que falta: TurnObserver para disparar efeitos automáticos na Standby Phase.
        Debug.Log("Blind Destruction: Ativo. (Aguardando sistema de fases automático).");
    }

    void Effect_0207_BlindlyLoyalGoblin(CardDisplay source)
    {
        // Efeito Contínuo: Controle não pode mudar.
        // O que falta: Flag 'cannotChangeControl' no CardDisplay ou StatModifier.
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
        // O que falta: Hook no BattleManager 'OnDamageDealt' para monstros específicos.
        Debug.Log("Blood Sucker: Efeito de mill configurado no BattleManager (pendente).");
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
        // O que falta: Sistema de Invocação Especial complexo (tributo específico) e sistema de Negação de Chain.
        Debug.Log("Blue-Eyes Shining Dragon: Requer tributo específico e lógica de negação.");
    }

    void Effect_0215_BlueEyesToonDragon(CardDisplay source)
    {
        // Efeito: Toon (Ataca direto se oponente não tiver Toon, etc).
        // O que falta: Classe base ou Tag 'Toon' que o BattleManager reconheça para aplicar regras de Toon globalmente.
        Debug.Log("Blue-Eyes Toon Dragon: Regras de Toon aplicadas.");
    }

    void Effect_0219_BoarSoldier(CardDisplay source)
    {
        // Efeito: Se invocado Normal, destrói a si mesmo. Se oponente tiver monstros, diminui ATK.
        // O que falta: Detecção do tipo de invocação no momento que ela ocorre (OnSummon).
        Debug.Log("Boar Soldier: Auto-destruição se Normal Summon.");
    }

    void Effect_0223_BombardmentBeetle(CardDisplay source)
    {
        // Efeito: Flip 1 monstro face-down do oponente. Se for efeito, vira face-down de novo.
        // O que falta: Lógica condicional baseada no tipo da carta revelada (Effect vs Normal).
        Debug.Log("Bombardment Beetle: Revelar face-down (Lógica condicional pendente).");
    }

    void Effect_0227_BookOfLife(CardDisplay source)
    {
        // Efeito: SS 1 Zumbi do seu GY e bane 1 monstro do GY do oponente.
        // O que falta: Seleção dupla (1 alvo no seu GY, 1 alvo no GY do oponente) em sequência.
        Debug.Log("Book of Life: Reviver Zumbi e Banir oponente.");
        Effect_Revive(source, false); // Parte 1: Revive
        // Parte 2: Banir (Pendente UI de seleção de GY oponente)
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
        // O que falta: TurnObserver para End Phase e lógica de comparação de todos os monstros no campo.
        Debug.Log("Bottomless Shifting Sand: Ativo. (Aguardando sistema de fases).");
    }

    void Effect_0233_BottomlessTrapHole(CardDisplay source)
    {
        // Efeito: Quando oponente invoca (1500+ ATK): Destrói e Bane.
        // O que falta: ChainManager detectar invocação e oferecer ativação.
        Debug.Log("Bottomless Trap Hole: Gatilho de invocação pendente.");
    }

    void Effect_0235_Bowganian(CardDisplay source)
    {
        // Efeito: Causa 600 dano na sua Standby Phase.
        // O que falta: TurnObserver.
        Debug.Log("Bowganian: Dano na Standby (Automático pendente).");
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
        // O que falta: Lógica de monstro virar Equipamento e sistema de manutenção de LP.
        Debug.Log("Brain Jacker: Controle via Equipamento (Lógica complexa pendente).");
    }

    void Effect_0240_BreakerTheMagicalWarrior(CardDisplay source)
    {
        // Efeito: Normal Summon -> Ganha contador (+300 ATK). Remove contador -> Destrói S/T.
        // O que falta: Sistema de Spell Counters e botões de "Ativar Efeito de Monstro" no campo (Ignition Effect).
        Debug.Log("Breaker: Sistema de Contadores e Efeito de Ignição pendentes.");
    }

    void Effect_0241_BreathOfLight(CardDisplay source)
    {
        // Efeito: Destrói todos os monstros Rock face-up.
        Effect_DestroyType(source, "Rock");
    }

    void Effect_0242_BubbleCrash(CardDisplay source)
    {
        // Efeito: Se tiver cartas na mão/campo, envie para o GY até ter 5.
        // O que falta: UI para o jogador selecionar o que enviar para o GY (Self-Mill seletivo).
        Debug.Log("Bubble Crash: Seleção de cartas para enviar ao GY pendente.");
    }

    void Effect_0243_BubbleShuffle(CardDisplay source)
    {
        // Efeito: Alvo 1 E-Hero Bubbleman e 1 monstro oponente em ataque. Muda ambos para defesa, sacrifica Bubbleman, invoca E-Hero da mão.
        // O que falta: Seleção de múltiplos alvos (1 seu, 1 do oponente) e sequência complexa de ações.
        Debug.Log("Bubble Shuffle: Lógica de múltiplos alvos e sequência pendente.");
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
        // O que falta: Ler status do monstro tributado para filtrar destruição.
        Debug.Log("Burst Breath: Lógica de tributo e filtro de destruição pendente.");
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
        // O que falta: StatModifier dinâmico que conte cartas no GY.
        Debug.Log("Buster Blader: Buff dinâmico pendente.");
    }

    void Effect_0253_BusterRancher(CardDisplay source)
    {
        // Efeito: Equip (apenas ATK <= 1000). Se batalhar, ATK vira 2500.
        // O que falta: Hook no BattleManager para alterar stats durante cálculo de dano.
        Debug.Log("Buster Rancher: Lógica de cálculo de dano pendente.");
    }

    void Effect_0254_ButterflyDaggerElma(CardDisplay source)
    {
        // Efeito: Equip +300. Se destruída, volta para mão.
        Effect_Equip(source, 300, 0);
        // TODO: Trigger OnDestroy para retornar à mão.
    }

    void Effect_0255_ByserShock(CardDisplay source)
    {
        // Efeito: Quando invocado, retorna todas as cartas Setadas para a mão.
        Debug.Log("Byser Shock: Retornando cartas setadas (Lógica de filtro pendente).");
    }

    void Effect_0256_CallOfDarkness(CardDisplay source)
    {
        // Efeito Contínuo: Se ativar Monster Reborn, perde LP.
        // O que falta: ChainManager verificar ativação específica.
        Debug.Log("Call of Darkness: Ativo.");
    }

    void Effect_0257_CallOfTheEarthbound(CardDisplay source)
    {
        // Efeito: Oponente ataca, você escolhe o alvo.
        // O que falta: Interrupção no BattleManager para abrir seleção de alvo.
        Debug.Log("Call of the Earthbound: Redirecionamento de ataque pendente.");
    }

    void Effect_0258_CallOfTheGrave(CardDisplay source)
    {
        // Efeito: Nega Monster Reborn.
        Debug.Log("Call of the Grave: Counter Trap específico.");
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
        // O que falta: Verificar contagem de monstros no campo.
        Debug.Log("Call of the Mummy: SS Zumbi da mão (Verificação pendente).");
    }

    void Effect_0262_CannonSoldier(CardDisplay source)
    {
        // Efeito: Tributa 1 monstro -> 500 dano.
        Effect_TributeToBurn(source, 1, 500);
    }

    void Effect_0263_CannonballSpearShellfish(CardDisplay source)
    {
        // Efeito: Imune a magias se Umi estiver no campo.
        // O que falta: Sistema de Imunidade no CardDisplay/StatModifier.
        Debug.Log("Cannonball Spear Shellfish: Imunidade condicional.");
    }

    void Effect_0264_CardDestruction(CardDisplay source)
    {
        // Efeito: Ambos os jogadores descartam a mão inteira e compram o mesmo número de cartas.
        // O que falta: Lógica de descarte em massa no GameManager.
        Debug.Log("Card Destruction: Descarte de mão e compra (Lógica de descarte em massa pendente).");
        // Simulação:
        // int playerHandCount = GameManager.Instance.playerHand.Count;
        // DiscardAll(true);
        // for(int i=0; i<playerHandCount; i++) GameManager.Instance.DrawCard();
        // Repetir para oponente.
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
        // O que falta: Sistema de Eventos (OnSpecialSummon) para detectar a origem da invocação.
        Debug.Log("Card of Safe Return: Ativo. (Aguardando sistema de eventos de invocação).");
    }

    void Effect_0267_CardOfSanctity(CardDisplay source)
    {
        // Efeito (TCG): Bana todas as cartas da sua mão e campo; compre 2 cartas.
        Debug.Log("Card of Sanctity: Banindo tudo e comprando 2.");
        // Lógica de banir tudo pendente.
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0268_CastleGate(CardDisplay source)
    {
        // Efeito: Tribute 1 monstro; cause dano igual ao ATK original dele. Este monstro não ataca.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            // Deveria selecionar qual tributar para calcular o dano exato
            Debug.Log("Castle Gate: Tributo realizado para dano (Valor fixo 1000 na simulação).");
            Effect_DirectDamage(source, 1000); 
            // source.canAttack = false; // Pendente
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
        // O que falta: Buff global persistente e TurnObserver.
        Debug.Log("Castle of Dark Illusions: Buff em Zumbis (Lógica contínua pendente).");
    }

    void Effect_0271_CatsEarTribe(CardDisplay source)
    {
        // Efeito: O ATK original do oponente vira 200 durante o cálculo de dano.
        // O que falta: Hook no BattleManager (OnDamageCalculation).
        Debug.Log("Cat's Ear Tribe: Efeito de batalha pendente.");
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
        // O que falta: Restrições de invocação e ataque no GameManager/BattleManager.
        Debug.Log("Cave Dragon: Restrições de invocação/ataque.");
    }

    void Effect_0275_Ceasefire(CardDisplay source)
    {
        // Efeito: Vira todos os monstros face-down para face-up (sem ativar efeitos Flip). Dano 500 por Effect Monster.
        // O que falta: Método RevealCardWithoutEffect() e contagem de Effect Monsters.
        Debug.Log("Ceasefire: Revelando monstros e causando dano (Lógica de não ativar Flip pendente).");
        // Simulação de dano
        Effect_DirectDamage(source, 1000);
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
        // O que falta: Evento OnCardDestroyedByEffect.
        Debug.Log("Centrifugal Field: Ativo. (Aguardando sistema de eventos de destruição).");
    }

    void Effect_0279_CeremonialBell(CardDisplay source)
    {
        // Efeito: Ambas as mãos ficam reveladas.
        // O que falta: Flag global showHand no GameManager.
        Debug.Log("Ceremonial Bell: Mãos reveladas (Visual pendente).");
        // GameManager.Instance.showOpponentHand = true;
    }

    void Effect_0280_CestusOfDagla(CardDisplay source)
    {
        // Efeito: Equip Fairy +500. Dano de batalha infligido ao oponente.
        Effect_Equip(source, 500, 0, "Fairy");
    }

    void Effect_0281_ChainBurst(CardDisplay source)
    {
        // Efeito: Quem ativar Trap toma 1000 de dano.
        // O que falta: Evento OnTrapActivated.
        Debug.Log("Chain Burst: Ativo. (Aguardando sistema de eventos de ativação).");
    }

    void Effect_0282_ChainDestruction(CardDisplay source)
    {
        // Efeito: Destrói cópias no deck/mão de monstro invocado (ATK <= 2000).
        Debug.Log("Chain Destruction: Destruição de cópias pendente.");
    }

    void Effect_0283_ChainDisappearance(CardDisplay source)
    {
        // Efeito: Bane cópias no deck/mão de monstro invocado (ATK <= 1000).
        Debug.Log("Chain Disappearance: Banimento de cópias pendente.");
    }

    void Effect_0284_ChainEnergy(CardDisplay source)
    {
        // Efeito: Pagar 500 LP para jogar cartas da mão.
        // O que falta: Hook no GameManager antes de qualquer ação de jogar carta.
        Debug.Log("Chain Energy: Custo de LP para jogar (Lógica de restrição pendente).");
    }

    void Effect_0287_ChangeOfHeart(CardDisplay source)
    {
        // Efeito: Controla 1 monstro do oponente até a End Phase.
        Effect_ChangeControl(source, true);
    }

    void Effect_0288_ChaosCommandMagician(CardDisplay source)
    {
        // Efeito: Nega efeito de monstro que dê alvo neste card.
        // O que falta: Sistema de Targeting que verifique a validade do alvo.
        Debug.Log("Chaos Command Magician: Imune a efeitos de monstro que dão alvo.");
    }

    void Effect_0289_ChaosEmperorDragon(CardDisplay source)
    {
        // Efeito: Paga 1000 LP; envia tudo (campo/mão) para o GY; 300 dano por carta.
        Effect_PayLP(source, 1000);
        Debug.Log("Chaos Emperor Dragon: Enviando tudo para o GY (Lógica de contagem de dano pendente).");
        // DestroyAllMonsters(true, true); // Simplificado
        // Effect_DirectDamage(source, 3000); // Dano estimado
    }

    void Effect_0290_ChaosEnd(CardDisplay source)
    {
        // Efeito: Se 7+ banidas, destrói todos os monstros.
        // O que falta: Contagem de cartas banidas.
        Debug.Log("Chaos End: Destruição em massa (Verificação de banidas pendente).");
    }

    void Effect_0291_ChaosGreed(CardDisplay source)
    {
        // Efeito: Se 4+ banidas e GY vazio, compra 2.
        Debug.Log("Chaos Greed: Compra 2 (Verificação de condições pendente).");
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0292_ChaosNecromancer(CardDisplay source)
    {
        // Efeito: ATK = 300 x Monstros no GY.
        // O que falta: StatModifier dinâmico.
        Debug.Log("Chaos Necromancer: ATK dinâmico pendente.");
    }

    void Effect_0293_ChaosSorcerer(CardDisplay source)
    {
        // Efeito: Bane 1 monstro face-up. Não ataca neste turno.
        // O que falta: Seleção de alvo para banir e restrição de ataque.
        Debug.Log("Chaos Sorcerer: Banir monstro (Lógica de banimento pendente).");
    }

    void Effect_0294_ChaosriderGustaph(CardDisplay source)
    {
        // Efeito: Bane até 2 Spells do GY para ganhar ATK.
        Debug.Log("Chaosrider Gustaph: Banir Spells para ATK (UI de seleção de GY pendente).");
    }

    void Effect_0296_CharmOfShabti(CardDisplay source)
    {
        // Efeito: Descarte para evitar que Gravekeepers sejam destruídos em batalha.
        Debug.Log("Charm of Shabti: Proteção de batalha (Hook no BattleManager pendente).");
    }

    void Effect_0298_Checkmate(CardDisplay source)
    {
        // Efeito: Tribute 1 monstro; Terrorking Archfiend ataca direto.
        Debug.Log("Checkmate: Ataque direto para Terrorking (Lógica de alvo pendente).");
    }

    void Effect_0299_ChimeraTheFlyingMythicalBeast(CardDisplay source)
    {
        // Efeito: Se destruído, invoca Berfomet ou Gazelle do GY.
        // O que falta: Evento OnDestroyed.
        Debug.Log("Chimera: Efeito de flutuação pendente.");
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
        // O que falta: Filtro de Equip Spell no GY e lógica de equipar do GY.
        Debug.Log("Chopman: Equipar do GY (Lógica de seleção de GY pendente).");
    }

    void Effect_0302_ChorusOfSanctuary(CardDisplay source)
    {
        // Field Spell: +500 DEF para todos os monstros em Defesa.
        // O que falta: StatModifier condicional (apenas em Defesa).
        Effect_Field(source, 0, 500, "", "");
    }

    void Effect_0303_ChosenOne(CardDisplay source)
    {
        // Selecione 1 Monstro e 2 não-Monstros da mão. Oponente escolhe 1 aleatoriamente.
        // Se for monstro, SS e manda o resto pro GY. Senão, tudo pro GY.
        // O que falta: UI de seleção de 3 cartas específicas da mão.
        Debug.Log("Chosen One: Minigame de seleção da mão (UI pendente).");
    }

    void Effect_0305_CipherSoldier(CardDisplay source)
    {
        // Se batalhar com Warrior: +2000 ATK/DEF durante o cálculo de dano.
        // O que falta: Hook no BattleManager (OnDamageCalculation).
        Debug.Log("Cipher Soldier: Buff contra Warrior (Cálculo de dano pendente).");
    }

    void Effect_0307_Cloning(CardDisplay source)
    {
        // Quando oponente invoca: SS Clone Token com mesmos stats/tipo/atributo.
        // O que falta: ChainManager detectar invocação do oponente.
        Debug.Log("Cloning: Token Clone (Gatilho de invocação pendente).");
    }

    void Effect_0309_CoachGoblin(CardDisplay source)
    {
        // End Phase: Se você controla este card face-up, pode retornar 1 Normal Monster da mão ao Deck para comprar 1.
        // O que falta: TurnObserver e UI de seleção da mão na End Phase.
        Debug.Log("Coach Goblin: Troca de mão na End Phase (Automático pendente).");
    }

    void Effect_0310_CobraJar(CardDisplay source)
    {
        // FLIP: SS 1 "Poisonous Snake Token".
        GameManager.Instance.SpawnToken(source.isPlayerCard, 1200, 1200, "Poisonous Snake Token");
    }

    void Effect_0311_CobramanSakuzy(CardDisplay source)
    {
        // Pode virar face-down 1x por turno. Quando Flip Summon: Olhe todas as S/T setadas do oponente.
        // O que falta: Lógica de revelar S/T sem ativar.
        Effect_TurnSet(source);
        Debug.Log("Cobraman Sakuzy: Revelar S/T (Visual pendente).");
    }

    void Effect_0312_CockroachKnight(CardDisplay source)
    {
        // Se enviado ao GY: Volta ao topo do Deck.
        // O que falta: Evento OnSentToGraveyard.
        Debug.Log("Cockroach Knight: Retorna ao topo do deck (Automático pendente).");
    }

    void Effect_0313_CocoonOfEvolution(CardDisplay source)
    {
        // Pode equipar da mão para "Petit Moth". Stats viram do Cocoon.
        // O que falta: Ativar efeito de monstro da mão como Equip Spell.
        Debug.Log("Cocoon of Evolution: Equipar da mão (Lógica de Union/Equip da mão pendente).");
    }

    void Effect_0314_CoffinSeller(CardDisplay source)
    {
        // Cada vez que monstro(s) do oponente vão para o GY: 300 dano.
        // O que falta: Evento global OnCardSentToGraveyard.
        Debug.Log("Coffin Seller: Dano passivo (Automático pendente).");
    }

    void Effect_0315_ColdWave(CardDisplay source)
    {
        // Só no início da Main 1. Até seu próximo turno, ninguém joga/seta S/T.
        // O que falta: Restrição global no GameManager/SpellTrapManager.
        Debug.Log("Cold Wave: Bloqueio de S/T (Regra global pendente).");
    }

    void Effect_0316_CollectedPower(CardDisplay source)
    {
        // Selecione 1 monstro face-up; equipe todos os Equip Cards no campo nele.
        // O que falta: Lógica de re-equipar cartas existentes.
        Debug.Log("Collected Power: Roubar equipamentos (Lógica de re-equip pendente).");
    }

    void Effect_0317_CombinationAttack(CardDisplay source)
    {
        // Battle Phase: Tribute monstro equipado com Union; Union ataca.
        // O que falta: Verificar estado de Union e fase de batalha.
        Debug.Log("Combination Attack: Ataque extra de Union (Lógica de batalha pendente).");
    }

    void Effect_0318_CommandKnight(CardDisplay source)
    {
        // Warriors +400 ATK. Se controlar outro monstro, não pode ser atacado.
        // O que falta: StatModifier passivo e restrição de alvo no BattleManager.
        Debug.Log("Command Knight: Buff e proteção (Passivo).");
    }

    void Effect_0319_CommencementDance(CardDisplay source)
    {
        // Ritual para "Performance of Sword".
        Debug.Log("Commencement Dance: Ritual (Sistema de Ritual pendente).");
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
        Effect_PayLP(source, 1000);
        Debug.Log("Confiscation: Olhar mão e descartar (UI de mão do oponente pendente).");
    }

    void Effect_0322_Conscription(CardDisplay source)
    {
        // Escave o topo do deck do oponente. Se monstro (Normal Summonable), SS no seu campo. Senão, mão dele.
        Debug.Log("Conscription: Roubar do topo do deck (Lógica de escavação pendente).");
    }

    void Effect_0323_ContinuousDestructionPunch(CardDisplay source)
    {
        // Se oponente ataca defesa e DEF > ATK, destrói atacante. Se ATK > DEF, destrói defensor.
        // O que falta: Hook no BattleManager (AfterDamageCalculation).
        Debug.Log("Continuous Destruction Punch: Regra de batalha modificada (Passivo).");
    }

    void Effect_0324_ContractWithExodia(CardDisplay source)
    {
        // Se tiver as 5 partes no GY: SS Exodia Necross da mão.
        // O que falta: Verificação complexa de GY.
        Debug.Log("Contract with Exodia: Invocar Necross (Verificação de GY pendente).");
    }

    void Effect_0325_ContractWithTheAbyss(CardDisplay source)
    {
        // Ritual Genérico para DARK.
        Debug.Log("Contract with the Abyss: Ritual DARK (Sistema de Ritual pendente).");
    }

    void Effect_0326_ContractWithTheDarkMaster(CardDisplay source)
    {
        // Ritual para Dark Master - Zorc.
        Debug.Log("Contract with the Dark Master: Ritual Zorc (Sistema de Ritual pendente).");
    }

    void Effect_0327_ConvulsionOfNature(CardDisplay source)
    {
        // Ambos os jogadores viram seus Decks de cabeça para baixo.
        // O que falta: Renderização do Deck com a face para cima.
        Debug.Log("Convulsion of Nature: Decks invertidos (Visual pendente).");
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
        // O que falta: Modificador de Nível na mão (afeta Tributos).
        Debug.Log("Cost Down: Níveis reduzidos (Lógica de tributo pendente).");
    }

    void Effect_0332_CoveringFire(CardDisplay source)
    {
        // Se oponente ataca: Monstro atacado ganha ATK de outro monstro seu.
        // O que falta: Trigger de ataque e seleção de segundo monstro.
        Debug.Log("Covering Fire: Buff de batalha (Gatilho pendente).");
    }

    void Effect_0334_CrassClown(CardDisplay source)
    {
        // Se mudado de Defesa para Ataque: Retorna 1 monstro do oponente para a mão.
        // O que falta: Evento OnPositionChanged.
        Debug.Log("Crass Clown: Bounce ao mudar posição (Gatilho pendente).");
    }

    void Effect_0338_CreatureSwap(CardDisplay source)
    {
        // Cada jogador escolhe 1 monstro; trocam o controle.
        // O que falta: Seleção simultânea ou sequencial forçada.
        Debug.Log("Creature Swap: Troca de monstros (UI de seleção dupla pendente).");
    }

    void Effect_0339_CreepingDoomManta(CardDisplay source)
    {
        // Quando invocado Normal: Oponente não ativa Traps.
        // O que falta: Bloqueio temporário no ChainManager.
        Debug.Log("Creeping Doom Manta: Traps bloqueadas na invocação.");
    }

    void Effect_0340_CrimsonNinja(CardDisplay source)
    {
        // FLIP: Destrói 1 Trap. Se setada, revela.
        Effect_FlipDestroy(source, TargetType.Trap);
    }

    void Effect_0341_CrimsonSentry(CardDisplay source)
    {
        // Tribute este card; coloque 1 monstro destruído neste turno do GY na mão.
        // O que falta: Histórico de destruição do turno.
        Debug.Log("Crimson Sentry: Recuperar monstro (Histórico pendente).");
    }

    void Effect_0343_Criosphinx(CardDisplay source)
    {
        // Se monstro voltar para a mão: Oponente descarta 1.
        // O que falta: Evento OnCardReturnedToHand.
        Debug.Log("Criosphinx: Descarte por bounce (Gatilho pendente).");
    }

    void Effect_0344_CrossCounter(CardDisplay source)
    {
        // Se atacado em defesa e DEF > ATK: Dano dobrado, destrói atacante.
        // O que falta: Hook no BattleManager.
        Debug.Log("Cross Counter: Contra-ataque (Lógica de batalha pendente).");
    }

    void Effect_0346_CrushCardVirus(CardDisplay source)
    {
        // Tribute Dark < 1000 ATK; Destrói monstros 1500+ ATK do oponente (campo/mão) e verifica por 3 turnos.
        // O que falta: Verificação de mão e turnos futuros.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard)) // Deveria checar Dark < 1000
        {
            Debug.Log("Crush Card Virus: Destruindo monstros fortes...");
            // DestroyAllMonsters(true, false); // Simplificado: Destrói campo
        }
    }

    void Effect_0347_CureMermaid(CardDisplay source)
    {
        // Standby Phase: Ganha 800 LP.
        // O que falta: TurnObserver.
        Debug.Log("Cure Mermaid: Cura na Standby (Automático pendente).");
    }

    void Effect_0348_CurseOfAging(CardDisplay source)
    {
        // Descarte 1; Monstros do oponente perdem 500 ATK/DEF.
        // O que falta: UI de descarte.
        Debug.Log("Curse of Aging: Debuff global (UI de descarte pendente).");
    }

    void Effect_0349_CurseOfAnubis(CardDisplay source)
    {
        // Todos Effect Monsters viram Defesa, DEF vira 0.
        Debug.Log("Curse of Anubis: Defesa e DEF 0 (Iteração de monstros pendente).");
    }

    void Effect_0350_CurseOfDarkness(CardDisplay source)
    {
        // Field Spell: Quem ativar Spell toma 1000 dano.
        // O que falta: Evento OnSpellActivated.
        Debug.Log("Curse of Darkness: Dano por magia (Automático pendente).");
    }

    void Effect_0352_CurseOfFiend(CardDisplay source)
    {
        // Muda posição de todos os monstros.
        Debug.Log("Curse of Fiend: Inversão de posições (Iteração pendente).");
    }

    void Effect_0353_CurseOfRoyal(CardDisplay source)
    {
        // Counter Trap: Nega S/T que destrói S/T.
        Debug.Log("Curse of Royal: Counter específico (Lógica de chain pendente).");
    }

    void Effect_0354_CurseOfTheMaskedBeast(CardDisplay source)
    {
        // Ritual para Masked Beast.
        Debug.Log("Curse of the Masked Beast: Ritual (Sistema pendente).");
    }

    void Effect_0355_CursedSealOfTheForbiddenSpell(CardDisplay source)
    {
        // Descarte Spell; nega Spell e proíbe uso pelo resto do duelo.
        // O que falta: Lista de proibições globais no GameManager.
        Debug.Log("Cursed Seal: Negação permanente (Sistema de proibição pendente).");
    }

    void Effect_0357_CyberArchfiend(CardDisplay source)
    {
        // Draw Phase: Se mão vazia, compra mais 1.
        // O que falta: Hook na Draw Phase.
        Debug.Log("Cyber Archfiend: Compra extra (Automático pendente).");
    }

    void Effect_0359_CyberDragon(CardDisplay source)
    {
        // SS da mão se oponente tem monstro e você não.
        // O que falta: Invocação Especial da mão (regra de SummonManager).
        Debug.Log("Cyber Dragon: Condição de SS (Verificação no SummonManager pendente).");
    }

    void Effect_0362_CyberHarpieLady(CardDisplay source)
    {
        // Efeito: O nome desta carta é tratado como "Harpie Lady".
        // O que falta: Sistema de Alias de nomes no CardData ou verificação dinâmica.
        Debug.Log("Cyber Harpie Lady: Nome tratado como Harpie Lady.");
    }

    void Effect_0363_CyberJar(CardDisplay source)
    {
        // FLIP: Destrói todos os monstros. Ambos compram 5, invocam Lv4- encontrados.
        // O que falta: Lógica complexa de escavar e invocar múltiplos monstros para ambos os jogadores.
        Debug.Log("Cyber Jar: Destruindo tudo e comprando 5 (Invocação pendente).");
        // DestroyAllMonsters(true, true); // Requer acesso ao método
        // Draw 5 for both
    }

    void Effect_0364_CyberRaider(CardDisplay source)
    {
        // Efeito: Selecione 1 Equip Card no campo; destrua-o ou equipe-o neste card.
        // O que falta: Escolha de modo (Destruir ou Equipar) e lógica de roubar equipamento.
        Debug.Log("Cyber Raider: Roubar/Destruir Equipamento.");
    }

    void Effect_0366_CyberShield(CardDisplay source)
    {
        // Equip: Harpie Lady +500 ATK.
        Effect_Equip(source, 500, 0, "Winged Beast"); // Simplificado para Winged Beast ou nome
    }

    void Effect_0369_CyberTwinDragon(CardDisplay source)
    {
        // Efeito: Pode atacar duas vezes na Battle Phase.
        // O que falta: StatModifier de ataques múltiplos ou flag no BattleManager.
        Debug.Log("Cyber Twin Dragon: Ataque duplo.");
    }

    void Effect_0370_CyberStein(CardDisplay source)
    {
        // Efeito: Pague 5000 LP; SS 1 Fusão do Extra Deck em Ataque.
        Effect_PayLP(source, 5000);
        // Abrir Extra Deck e invocar
        Debug.Log("Cyber-Stein: Invocar Fusão (UI de Extra Deck pendente).");
        // GameManager.Instance.ViewExtraDeck(source.isPlayerCard); // Precisa permitir seleção para invocar
    }

    void Effect_0372_CyberneticCyclopean(CardDisplay source)
    {
        // Efeito: Se você não tiver cartas na mão, ganha 1000 ATK.
        // O que falta: Verificação contínua da mão e atualização de stats.
        Debug.Log("Cybernetic Cyclopean: +1000 ATK se mão vazia.");
    }

    void Effect_0373_CyberneticMagician(CardDisplay source)
    {
        // Efeito: Descarte 1 carta; ATK de 1 monstro vira 2000.
        // O que falta: UI de descarte e seleção de alvo para modificar ATK base.
        Debug.Log("Cybernetic Magician: Alterar ATK para 2000.");
    }

    void Effect_0374_CyclonLaser(CardDisplay source)
    {
        // Equip: Gradius +300 ATK, Piercing.
        Effect_Equip(source, 300, 0, "Machine"); // Simplificado
    }

    void Effect_0377_DTribe(CardDisplay source)
    {
        // Trap: Todos os monstros viram Dragão.
        // O que falta: Modificador de Tipo global.
        Debug.Log("D. Tribe: Todos viram Dragão.");
    }

    void Effect_0378_DDAssailant(CardDisplay source)
    {
        // Efeito: Se destruído em batalha, bane o atacante e este card.
        // O que falta: Trigger de destruição em batalha no BattleManager.
        Debug.Log("D.D. Assailant: Banir atacante.");
    }

    void Effect_0379_DDBorderline(CardDisplay source)
    {
        // Spell: Ninguém ataca se não houver Spells no seu GY.
        // O que falta: Restrição de ataque global condicional no BattleManager.
        Debug.Log("D.D. Borderline: Bloqueio de ataque.");
    }

    void Effect_0380_DDCrazyBeast(CardDisplay source)
    {
        // Efeito: Bane monstro destruído por este card em batalha.
        // O que falta: Trigger de destruição por batalha.
        Debug.Log("D.D. Crazy Beast: Banir destruído.");
    }

    void Effect_0381_DDDesignator(CardDisplay source)
    {
        // Spell: Declare 1 carta; verifique mão do oponente. Se tiver, bane. Se não, bane 1 da sua.
        // O que falta: Input de texto para declarar nome e verificação da mão.
        Debug.Log("D.D. Designator: Adivinhar carta da mão.");
    }

    void Effect_0382_DDDynamite(CardDisplay source)
    {
        // Trap: 300 dano por cada carta banida do oponente.
        // O que falta: Contagem de banidas do oponente.
        Debug.Log("D.D. Dynamite: Dano por banidas.");
    }

    void Effect_0383_DDScoutPlane(CardDisplay source)
    {
        // Efeito: Se banido, SS na End Phase.
        // O que falta: Evento OnBanished e TurnObserver.
        Debug.Log("D.D. Scout Plane: Retorna se banido.");
    }

    void Effect_0384_DDSurvivor(CardDisplay source)
    {
        // Efeito: Se banido enquanto face-up, SS na End Phase.
        Debug.Log("D.D. Survivor: Retorna se banido.");
    }

    void Effect_0386_DDTrapHole(CardDisplay source)
    {
        // Trap: Quando oponente Set monstro: Destrói e bane.
        // O que falta: Trigger de Set no SummonManager.
        Debug.Log("D.D. Trap Hole: Destruir e banir Set.");
    }

    void Effect_0387_DDWarrior(CardDisplay source)
    {
        // Efeito: Após batalha, bane este card e o oponente.
        Debug.Log("D.D. Warrior: Banir ambos.");
    }

    void Effect_0388_DDWarriorLady(CardDisplay source)
    {
        // Efeito: Após batalha, pode banir este card e o oponente.
        Debug.Log("D.D. Warrior Lady: Banir ambos (Opcional).");
    }

    void Effect_0389_DDM(CardDisplay source)
    {
        // Efeito: Descarte 1 Spell; SS 1 monstro banido.
        // O que falta: UI de descarte específico e seleção de banidos.
        Debug.Log("D.D.M.: Invocar banido.");
    }

    void Effect_0390_DNASurgery(CardDisplay source)
    {
        // Trap: Declare 1 Tipo; todos viram esse Tipo.
        // O que falta: Input de declaração e modificador global.
        Debug.Log("DNA Surgery: Mudar tipo de todos.");
    }

    void Effect_0391_DNATransplant(CardDisplay source)
    {
        // Trap: Declare 1 Atributo; todos viram esse Atributo.
        Debug.Log("DNA Transplant: Mudar atributo de todos.");
    }

    void Effect_0393_DancingFairy(CardDisplay source)
    {
        // Efeito: Se em Defesa, ganha 1000 LP na Standby.
        // O que falta: TurnObserver.
        Debug.Log("Dancing Fairy: Ganha LP na Standby.");
    }

    void Effect_0394_DangerousMachineType6(CardDisplay source)
    {
        // Spell: Rola dado e aplica efeito aleatório.
        Debug.Log("Dangerous Machine Type-6: Efeito de dado.");
    }

    void Effect_0395_DarkArtist(CardDisplay source)
    {
        // Efeito: DEF cai pela metade se atacado por LIGHT.
        // O que falta: Cálculo de dano condicional.
        Debug.Log("Dark Artist: DEF reduzida contra LIGHT.");
    }

    void Effect_0397_DarkBalterTheTerrible(CardDisplay source)
    {
        // Fusão: Pague 1000 para negar Normal Spell. Nega efeitos de monstros destruídos.
        // O que falta: Sistema de Chain para negar Spell.
        Debug.Log("Dark Balter: Negar Spell / Negar efeitos.");
    }

    void Effect_0400_DarkBladeTheDragonKnight(CardDisplay source)
    {
        // Fusão: Bane até 3 monstros do GY do oponente.
        // O que falta: Seleção múltipla no GY do oponente.
        Debug.Log("Dark Blade Dragon Knight: Banir do GY.");
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