using UnityEngine;

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
        // Requer sistema de Chain/Response.
        Debug.Log("A-Team: Efeito de negar Trap ativado (Simulação).");
        // Destroy(source.gameObject);
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
        Debug.Log("Amazoness Chain Master: Roubando monstro da mão (Simulado: Pega o primeiro).");
        // Simulação
        if (GameManager.Instance.GetPlayerHandData().Count > 0) // Deveria ser mão do oponente
        {
            // Adiciona à mão do jogador
        }
    }

    void Effect_0045_AmazonessFighter(CardDisplay source)
    {
        // Sem dano de batalha para o controlador.
        // Passivo no BattleManager.
    }

    void Effect_0046_AmazonessPaladin(CardDisplay source)
    {
        // +100 ATK por cada "Amazoness".
        // Passivo/Continuous Update.
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
        if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) // Umi ou Legendary Ocean
        {
            Debug.Log("MK-3: Condição de ataque direto ativa.");
        }
    }

    void Effect_0054_Amplifier(CardDisplay source)
    {
        // Equip Jinzo. Jinzo do controlador não nega Traps do controlador.
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
        // Se destruir monstro em defesa: Causa dano = metade do ATK.
        // Trigger de batalha.
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
        Debug.Log("Anti-Spell: Tentando remover contadores...");
    }

    void Effect_0076_AntiSpellFragrance(CardDisplay source)
    {
        // Magias devem ser setadas e esperar 1 turno.
        // Regra global no SpellTrapManager.
    }

    void Effect_0077_ApprenticeMagician(CardDisplay source)
    {
        // Summon: Põe Spell Counter. Destroy: SS Spellcaster Lv2- do Deck.
        Debug.Log("Apprentice Magician: Efeitos de contador e float.");
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
        Debug.Log("Arcana Knight Joker: Negação seletiva.");
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
        Debug.Log("Archfiend's Oath: Adivinhe a carta (Sempre erra na simulação atual).");
    }

    void Effect_0091_ArchfiendsRoar(CardDisplay source)
    {
        // Pague 500; SS 1 Archfiend do GY. Destrua na End Phase.
        Effect_PayLP(source, 500);
        Effect_Revive(source, false); // Filtro Archfiend pendente
    }

    void Effect_0092_ArchlordZerato(CardDisplay source)
    {
        // Descarte 1 Light; destrua todos monstros do oponente. (Requer Sanctuary).
        if (GameManager.Instance.IsCardActiveOnField("1887")) // Sanctuary in the Sky
        {
            // Discard logic
            DestroyAllMonsters(true, false);
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
        // Standby: SS LV7.
        Debug.Log("Armed Dragon LV5: Efeito de destruição e Level Up.");
    }

    void Effect_0098_ArmedDragonLV7(CardDisplay source)
    {
        // Descarte monstro; destrua todos monstros oponente com ATK <= ATK do descartado.
        Debug.Log("Armed Dragon LV7: Destruição em massa.");
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
    
    void Effect_0101_ArmorBreak(CardDisplay source)
    {
        Debug.Log("Armor Break: Negar ativação de Equip Spell.");
    }

    void Effect_0102_ArmorExe(CardDisplay source)
    {
        Debug.Log("Armor Exe: Remover contador ou destruir.");
    }

    void Effect_0103_ArmoredGlass(CardDisplay source)
    {
        Debug.Log("Armored Glass: Negar efectos de Equipamento.");
    }

    void Effect_0108_ArrayOfRevealingLight(CardDisplay source)
    {
        Debug.Log("Array of Revealing Light: Declarar tipo.");
    }

    void Effect_0109_ArsenalBug(CardDisplay source)
    {
        Debug.Log("Arsenal Bug: ATK/DEF vira 1000 se não houver outro Inseto.");
    }

    void Effect_0110_ArsenalRobber(CardDisplay source)
    {
        Debug.Log("Arsenal Robber: Oponente escolhe uma Equip Spell do deck e envia ao GY.");
    }

    void Effect_0111_ArsenalSummoner(CardDisplay source)
    {
        // Debug.Log("Arsenal Summoner (FLIP: Search Guardian)");
        Effect_SearchDeck(source, "Guardian");
    }

    void Effect_0112_AssaultOnGHQ(CardDisplay source)
    {
        Debug.Log("Assault on GHQ: Destruir monstro para millar oponente.");
    }

    void Effect_0113_AstralBarrier(CardDisplay source)
    {
        Debug.Log("Astral Barrier: Redirecionar para ataque direto.");
    }

    void Effect_0114_AsuraPriest(CardDisplay source)
    {
        Debug.Log("Asura Priest: Ataca todos. Retorna para mão.");
    }

    void Effect_0115_AswanApparition(CardDisplay source)
    {
        Debug.Log("Aswan Apparition: Reciclar Trap do GY.");
    }

    void Effect_0116_AtomicFirefly(CardDisplay source)
    {
        Debug.Log("Atomic Firefly: 1000 dano ao oponente.");
    }

    void Effect_0117_AttackAndReceive(CardDisplay source)
    {
        Effect_DirectDamage(source, 700);
    }

    void Effect_0118_AussaTheEarthCharmer(CardDisplay source)
    {
        Debug.Log("Aussa: Controlar monstro EARTH.");
    }

    void Effect_0119_AutonomousActionUnit(CardDisplay source)
    {
        Effect_PayLP(source, 1500);
        Debug.Log("Autonomous Action Unit: Invocar do GY do oponente.");
    }

    void Effect_0120_AvatarOfThePot(CardDisplay source)
    {
        Debug.Log("Avatar of The Pot: Enviando Pot of Greed da mão para comprar 3.");
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0122_AxeOfDespair(CardDisplay source)
    {
        // Debug.Log("Axe of Despair (Equip +1000)");
        Effect_Equip(source, 1000, 0);
    }

    void Effect_0124_BESBigCore(CardDisplay source)
    {
        Debug.Log("B.E.S. Big Core: Contadores.");
    }

    void Effect_0125_BESCrystalCore(CardDisplay source)
    {
        Debug.Log("B.E.S. Crystal Core: Contadores.");
    }

    void Effect_0127_BackToSquareOne(CardDisplay source)
    {
        Debug.Log("Back to Square One: Descartar para retornar monstro ao topo do deck.");
    }

    void Effect_0128_Backfire(CardDisplay source)
    {
        Effect_DirectDamage(source, 500);
    }

    void Effect_0129_BackupSoldier(CardDisplay source)
    {
        Debug.Log("Backup Soldier: Recuperando monstros normais do GY.");
    }

    void Effect_0130_BadReactionToSimochi(CardDisplay source)
    {
        Debug.Log("Bad Reaction to Simochi ativado. Cura vira dano.");
    }

    void Effect_0131_BaitDoll(CardDisplay source)
    {
        Debug.Log("Bait Doll: Forçando ativação.");
    }

    void Effect_0132_BalloonLizard(CardDisplay source)
    {
        Debug.Log("Balloon Lizard: Contadores e dano.");
    }

    void Effect_0133_BanisherOfTheLight(CardDisplay source)
    {
        Debug.Log("Banisher of the Light: Banir cartas enviadas ao GY.");
    }

    void Effect_0134_BannerOfCourage(CardDisplay source)
    {
        Debug.Log("Banner of Courage: +200 ATK na Battle Phase.");
    }

    void Effect_0135_BarkOfDarkRuler(CardDisplay source)
    {
        Debug.Log("Bark of Dark Ruler: Pagar LP para reduzir stats.");
    }

    void Effect_0138_BarrelBehindTheDoor(CardDisplay source)
    {
        Debug.Log("Barrel Behind the Door: Refletir dano de efeito.");
    }

    void Effect_0139_BarrelDragon(CardDisplay source)
    {
        // Debug.Log("Barrel Dragon (Coin toss destroy)");
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Monster);
    }

    void Effect_0144_BatteryCharger(CardDisplay source)
    {
        Effect_PayLP(source, 500);
        Debug.Log("Battery Charger: SS Batteryman do GY.");
    }

    void Effect_0145_BatterymanAA(CardDisplay source)
    {
        Debug.Log("Batteryman AA: Ganha ATK/DEF.");
    }

    void Effect_0146_BatterymanC(CardDisplay source)
    {
        Debug.Log("Batteryman C: Buff em Machines.");
    }

    void Effect_0151_BattleScarred(CardDisplay source)
    {
        Debug.Log("Battle-Scarred: Oponente paga custo de Archfiend.");
    }

    void Effect_0152_BazooTheSoulEater(CardDisplay source)
    {
        Debug.Log("Bazoo: Banir do GY para ganhar ATK.");
    }

    void Effect_0155_BeastFangs(CardDisplay source)
    {
        // Debug.Log("Beast Fangs (Equip +300/300)");
        Effect_Equip(source, 300, 300, "Beast");
    }

    void Effect_0156_BeastSoulSwap(CardDisplay source)
    {
        Debug.Log("Beast Soul Swap: Troca de Bestas.");
    }

    void Effect_0158_BeastkingOfTheSwamps(CardDisplay source)
    {
        Debug.Log("Beastking: Substituto de fusão ou buscar Poly.");
    }

    void Effect_0163_BeckoningLight(CardDisplay source)
    {
        Debug.Log("Beckoning Light: Troca mão por Light do GY.");
    }

    void Effect_0164_BegoneKnave(CardDisplay source)
    {
        Debug.Log("Begone, Knave! ativado. Monstros que causam dano voltam para a mão.");
    }

    void Effect_0166_BehemothTheKingOfAllAnimals(CardDisplay source)
    {
        Debug.Log("Behemoth: Retornar Bestas do GY.");
    }

    void Effect_0167_Berfomet(CardDisplay source)
    {
        // Debug.Log("Berfomet (Search Gazelle)");
        Effect_SearchDeck(source, "Gazelle the King of Mythical Beasts");
    }

    void Effect_0168_BerserkDragon(CardDisplay source)
    {
        Debug.Log("Berserk Dragon: Ataca todos.");
    }

    void Effect_0169_BerserkGorilla(CardDisplay source)
    {
        Debug.Log("Berserk Gorilla: Destruído se defesa. Deve atacar.");
    }

    void Effect_0172_BigBangShot(CardDisplay source)
    {
        // Debug.Log("Big Bang Shot (Equip +400, Piercing, Banish)");
        Effect_Equip(source, 400, 0);
    }

    void Effect_0173_BigBurn(CardDisplay source)
    {
        Debug.Log("Big Burn: Banir ambos os cemitérios.");
    }

    void Effect_0174_BigEye(CardDisplay source)
    {
        Debug.Log("Big Eye: Reordenar topo do deck.");
    }

    void Effect_0177_BigShieldGardna(CardDisplay source)
    {
        Debug.Log("Big Shield Gardna: Nega magia e muda posição.");
    }

    void Effect_0178_BigWaveSmallWave(CardDisplay source)
    {
        Debug.Log("Big Wave Small Wave: Substituindo monstros de Água.");
    }

    void Effect_0179_BigTuskedMammoth(CardDisplay source)
    {
        Debug.Log("Big-Tusked Mammoth: Impede ataque no turno de invocação.");
    }

    void Effect_0183_Birdface(CardDisplay source)
    {
        // Debug.Log("Birdface (Search Harpie)");
        Effect_SearchDeck(source, "Harpie Lady");
    }

    void Effect_0184_BiteShoes(CardDisplay source)
    {
        Debug.Log("Bite Shoes: Mudar posição de batalha.");
    }

    void Effect_0185_BlackDragonsChick(CardDisplay source)
    {
        Debug.Log("Black Dragon's Chick: Invocando Red-Eyes B. Dragon.");
    }

    void Effect_0189_BLSEnvoy(CardDisplay source)
    {
        Debug.Log("BLS Envoy: Banir ou Ataque Duplo.");
    }

    void Effect_0191_BlackPendant(CardDisplay source)
    {
        // Debug.Log("Black Pendant (Equip +500, Burn 500)");
        Effect_Equip(source, 500, 0);
    }

    void Effect_0193_BlackTyranno(CardDisplay source)
    {
        Debug.Log("Black Tyranno: Ataque direto se tudo defesa.");
    }

    void Effect_0195_BladeKnight(CardDisplay source)
    {
        Debug.Log("Blade Knight: Buff se mão vazia.");
    }

    void Effect_0196_BladeRabbit(CardDisplay source)
    {
        Debug.Log("Blade Rabbit: Destruir monstro ao mudar para defesa.");
    }

    void Effect_0197_Bladefly(CardDisplay source)
    {
        // Debug.Log("Bladefly (Buff Wind)");
        Effect_Field(source, 500, 500, "", "WIND");
    }

    void Effect_0198_BlastHeldByATribute(CardDisplay source)
    {
        Debug.Log("Blast Held by a Tribute: Destruindo atacante e causando 1000 dano.");
    }

    void Effect_0199_BlastJuggler(CardDisplay source)
    {
        Debug.Log("Blast Juggler: Destruir monstros fracos.");
    }

    void Effect_0200_BlastMagician(CardDisplay source)
    {
        Debug.Log("Blast Magician: Removendo contadores para destruir monstro.");
    }

    void Effect_0201_BlastSphere(CardDisplay source)
    {
        Debug.Log("Blast Sphere: Se atacado face-down, equipa no atacante e destrói na próxima Standby.");
    }

    void Effect_0202_BlastWithChain(CardDisplay source)
    {
        // Debug.Log("Blast with Chain (Equip +500, destroy card if destroyed)");
        Effect_Equip(source, 500, 0);
    }

    void Effect_0203_BlastingTheRuins(CardDisplay source)
    {
        int gyCount = source.isPlayerCard ? GameManager.Instance.playerGraveyardDisplay.pileData.Count : GameManager.Instance.opponentGraveyardDisplay.pileData.Count;
        if (gyCount >= 30) GameManager.Instance.DamageOpponent(3000);
    }

    void Effect_0205_BlessingsOfTheNile(CardDisplay source)
    {
        Debug.Log("Blessings of the Nile: Ganha 1000 LP quando cartas são descartadas.");
    }

    void Effect_0206_BlindDestruction(CardDisplay source)
    {
        Debug.Log("Blind Destruction: Rola dado na Standby para destruir monstros.");
    }

    void Effect_0207_BlindlyLoyalGoblin(CardDisplay source)
    {
        Debug.Log("Blindly Loyal Goblin: Controle não pode mudar.");
    }

    void Effect_0208_BlockAttack(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack && !t.isPlayerCard,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Block Attack: {t.CurrentCardData.name} mudou para defesa.");
                }
            );
        }
    }

    void Effect_0210_BloodSucker(CardDisplay source)
    {
        Debug.Log("Blood Sucker: Envia topo do deck do oponente ao GY ao causar dano.");
    }

    void Effect_0211_BlowbackDragon(CardDisplay source)
    {
        // Debug.Log("Blowback Dragon (Coin toss destroy)");
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Any);
    }

    void Effect_0212_BlueMedicine(CardDisplay source)
    {
        // Debug.Log("Blue Medicine (Gain 400 LP)");
        Effect_GainLP(source, 400);
    }

    void Effect_0214_BlueEyesShiningDragon(CardDisplay source)
    {
        Debug.Log("Blue-Eyes Shining Dragon: Nega efeitos que dão alvo.");
    }

    void Effect_0215_BlueEyesToonDragon(CardDisplay source)
    {
        Debug.Log("Toon Dragon: Ataca direto se oponente não tiver Toon.");
    }

    void Effect_0219_BoarSoldier(CardDisplay source)
    {
        Debug.Log("Boar Soldier: Destruído se Normal Summon.");
    }

    void Effect_0223_BombardmentBeetle(CardDisplay source)
    {
        Debug.Log("Bombardment Beetle: Revela face-down do oponente.");
    }

    void Effect_0227_BookOfLife(CardDisplay source)
    {
        Debug.Log("Book of Life: Invocar Zumbi e banir monstro do oponente.");
        Effect_Revive(source, false);
    }

    void Effect_0228_BookOfMoon(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                    t.ShowBack();
                    Debug.Log($"Book of Moon: {t.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    void Effect_0229_BookOfSecretArts(CardDisplay source)
    {
        // Debug.Log("Book of Secret Arts (Equip +300/300 Spellcaster)");
        Effect_Equip(source, 300, 300, "Spellcaster");
    }

    void Effect_0230_BookOfTaiyou(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped,
                (t) => {
                    t.RevealCard();
                    t.ChangePosition();
                    Debug.Log($"Book of Taiyou: {t.CurrentCardData.name} virado para cima.");
                }
            );
        }
    }

    void Effect_0232_BottomlessShiftingSand(CardDisplay source)
    {
        Debug.Log("Bottomless Shifting Sand: Destrói monstro com maior ATK.");
    }

    void Effect_0233_BottomlessTrapHole(CardDisplay source)
    {
        Debug.Log("Bottomless Trap Hole: Destruindo e banindo monstro invocado com 1500+ ATK.");
    }

    void Effect_0235_Bowganian(CardDisplay source)
    {
        Debug.Log("Bowganian: 600 dano na Standby Phase.");
    }

    void Effect_0237_BrainControl(CardDisplay source)
    {
        Debug.Log("Brain Control: Pagar 800 LP para controlar monstro.");
        Effect_ChangeControl(source, true);
        Effect_PayLP(source, 800);
    }

    void Effect_0238_BrainJacker(CardDisplay source)
    {
        Debug.Log("Brain Jacker: Equipa e toma controle.");
    }

    void Effect_0240_BreakerTheMagicalWarrior(CardDisplay source)
    {
        Debug.Log("Breaker: Ganhou contador. Pode remover para destruir S/T.");
    }

    void Effect_0241_BreathOfLight(CardDisplay source)
    {
        // Debug.Log("Breath of Light (Destroy Rock)");
        Effect_DestroyType(source, "Rock");
    }

    void Effect_0242_BubbleCrash(CardDisplay source)
    {
        Debug.Log("Bubble Crash: Envia cartas ao GY até ter 5.");
    }

    void Effect_0243_BubbleShuffle(CardDisplay source)
    {
        Debug.Log("Bubble Shuffle: Mudando posições e invocando.");
    }

    void Effect_0244_BubonicVermin(CardDisplay source)
    {
        // Debug.Log("Bubonic Vermin (Flip SS)");
        Effect_SearchDeck(source, "Bubonic Vermin");
    }

    void Effect_0246_BurningAlgae(CardDisplay source)
    {
        if(source.isPlayerCard) GameManager.Instance.opponentLP += 1000; 
        else GameManager.Instance.playerLP += 1000;
    }

    void Effect_0248_BurningLand(CardDisplay source)
    {
        Debug.Log("Burning Land: Destrói campos e causa dano na Standby.");
    }

    void Effect_0249_BurningSpear(CardDisplay source)
    {
        // Debug.Log("Burning Spear (Equip +400/-200)");
        Effect_Equip(source, 400, -200, "", "Fire");
    }

    void Effect_0250_BurstBreath(CardDisplay source)
    {
        Debug.Log("Burst Breath: Tributa Dragão para destruir monstros.");
    }

    void Effect_0251_BurstStreamOfDestruction(CardDisplay source)
    {
        Debug.Log("Burst Stream: Destruir monstros do oponente (se tiver Blue-Eyes).");
        // DestroyAllMonsters(true, false); // Requer acesso ao método
    }

    void Effect_0252_BusterBlader(CardDisplay source)
    {
        Debug.Log("Buster Blader: Ganha ATK por Dragões.");
    }

    void Effect_0253_BusterRancher(CardDisplay source)
    {
        Debug.Log("Buster Rancher: Buff massivo se ATK base <= 1000.");
    }

    void Effect_0254_ButterflyDaggerElma(CardDisplay source)
    {
        // Debug.Log("Butterfly Dagger - Elma (Equip +300)");
        Effect_Equip(source, 300, 0);
    }

    void Effect_0255_ByserShock(CardDisplay source)
    {
        Debug.Log("Byser Shock: Retorna cartas setadas para a mão.");
    }

    void Effect_0256_CallOfDarkness(CardDisplay source)
    {
        Debug.Log("Call of Darkness: Pune Monster Reborn.");
    }

    void Effect_0257_CallOfTheEarthbound(CardDisplay source)
    {
        Debug.Log("Call of the Earthbound: Redireciona ataque.");
    }

    void Effect_0258_CallOfTheGrave(CardDisplay source)
    {
        Debug.Log("Call of the Grave: Nega Monster Reborn.");
    }

    void Effect_0259_CallOfTheHaunted(CardDisplay source)
    {
        Debug.Log("Call of the Haunted: Invocar do GY em ataque.");
        Effect_Revive(source, true);
    }

    void Effect_0260_CallOfTheMummy(CardDisplay source)
    {
        Debug.Log("Call of the Mummy: Invocando Zumbi da mão.");
    }

    void Effect_0262_CannonSoldier(CardDisplay source)
    {
        // Debug.Log("Cannon Soldier (Tribute burn)");
        Effect_TributeToBurn(source, 1, 500);
    }

    void Effect_0263_CannonballSpearShellfish(CardDisplay source)
    {
        Debug.Log("Cannonball Spear Shellfish: Imune a magias com Umi.");
    }

    void Effect_0264_CardDestruction(CardDisplay source)
    {
        Debug.Log("Card Destruction: Ambos descartam mão e compram a mesma quantidade.");
    }

    void Effect_0265_CardShuffle(CardDisplay source)
    {
        Effect_PayLP(source, 300);
        Debug.Log("Deck embaralhado.");
    }

    void Effect_0266_CardOfSafeReturn(CardDisplay source)
    {
        Debug.Log("Card of Safe Return: Compre 1 quando invocar do GY.");
    }

    void Effect_0267_CardOfSanctity(CardDisplay source)
    {
        Debug.Log("Card of Sanctity: Banindo mão e campo, comprando 2.");
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0268_CastleGate(CardDisplay source)
    {
        Debug.Log("Castle Gate: Tributa para causar dano.");
    }

    void Effect_0269_CastleWalls(CardDisplay source)
    {
        // Debug.Log("Castle Walls (Trap +500 DEF)");
        Effect_BuffStats(source, 0, 500);
    }

    void Effect_0270_CastleOfDarkIllusions(CardDisplay source)
    {
        Debug.Log("Castle of Dark Illusions: Buff em Zumbis.");
    }

    void Effect_0271_CatsEarTribe(CardDisplay source)
    {
        Debug.Log("Cat's Ear Tribe: ATK do oponente vira 200.");
    }

    void Effect_0272_CatapultTurtle(CardDisplay source)
    {
        Debug.Log("Catapult Turtle: Tributando para causar dano.");
    }

    void Effect_0273_CatnippedKitty(CardDisplay source)
    {
        Debug.Log("Catnipped Kitty: Torna DEF do oponente 0.");
    }

    void Effect_0274_CaveDragon(CardDisplay source)
    {
        Debug.Log("Cave Dragon: Restrições de invocação e ataque.");
    }

    void Effect_0275_Ceasefire(CardDisplay source)
    {
        Debug.Log("Ceasefire: Virar todos para cima e causar dano por efeito.");
    }

    void Effect_0277_CemetaryBomb(CardDisplay source)
    {
        int damage = GameManager.Instance.opponentGraveyardDisplay.pileData.Count * 100;
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_0278_CentrifugalField(CardDisplay source)
    {
        Debug.Log("Centrifugal Field: Recupera material de fusão.");
    }

    void Effect_0279_CeremonialBell(CardDisplay source)
    {
        Debug.Log("Ceremonial Bell: Mãos reveladas.");
    }

    void Effect_0280_CestusOfDagla(CardDisplay source)
    {
        // Debug.Log("Cestus of Dagla (Equip +500 Fairy)");
        Effect_Equip(source, 500, 0, "Fairy");
    }

    void Effect_0281_ChainBurst(CardDisplay source)
    {
        Debug.Log("Chain Burst: Dano ao ativar armadilha.");
    }

    void Effect_0282_ChainDestruction(CardDisplay source)
    {
        Debug.Log("Chain Destruction: Destrói cópias no deck/mão.");
    }

    void Effect_0283_ChainDisappearance(CardDisplay source)
    {
        Debug.Log("Chain Disappearance: Bane cópias no deck/mão.");
    }

    void Effect_0284_ChainEnergy(CardDisplay source)
    {
        Debug.Log("Chain Energy: Custo de LP para jogar.");
    }

    void Effect_0287_ChangeOfHeart(CardDisplay source)
    {
        Debug.Log("Change of Heart: Controlar monstro até o fim do turno.");
        Effect_ChangeControl(source, true);
    }

    void Effect_0288_ChaosCommandMagician(CardDisplay source)
    {
        Debug.Log("Chaos Command Magician: Nega efeitos de monstro que dão alvo.");
    }

    void Effect_0289_ChaosEmperorDragon(CardDisplay source)
    {
        Effect_PayLP(source, 1000);
        Debug.Log("Chaos Emperor Dragon: Enviar tudo para o GY e causar dano.");
    }

    void Effect_0290_ChaosEnd(CardDisplay source)
    {
        if (GameManager.Instance.playerRemoved.Count >= 7)
        {
            // DestroyAllMonsters(true, true); // Requer acesso
        }
    }

    void Effect_0291_ChaosGreed(CardDisplay source)
    {
        if(GameManager.Instance.playerRemoved.Count >= 4 && GameManager.Instance.playerGraveyard.Count == 0) 
        { 
            GameManager.Instance.DrawCard(); 
            GameManager.Instance.DrawCard(); 
        }
    }

    void Effect_0292_ChaosNecromancer(CardDisplay source)
    {
        Debug.Log("Chaos Necromancer: ATK baseado no GY.");
    }

    void Effect_0293_ChaosSorcerer(CardDisplay source)
    {
        Debug.Log("Chaos Sorcerer: Banir monstro face-up.");
    }

    void Effect_0294_ChaosriderGustaph(CardDisplay source)
    {
        Debug.Log("Chaosrider Gustaph: Bane magias para ganhar ATK.");
    }

    void Effect_0296_CharmOfShabti(CardDisplay source)
    {
        Debug.Log("Charm of Shabti: Protege Gravekeepers.");
    }

    void Effect_0298_Checkmate(CardDisplay source)
    {
        Debug.Log("Checkmate: Terrorking ataca direto.");
    }

    void Effect_0299_ChimeraTheFlyingMythicalBeast(CardDisplay source)
    {
        Debug.Log("Chimera: Invoca material do GY.");
    }

    void Effect_0300_ChironTheMage(CardDisplay source)
    {
        Debug.Log("Chiron the Mage: Descarte Magia para destruir S/T.");
    }
}
