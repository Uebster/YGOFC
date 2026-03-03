    // =========================================================================================
    // LÓGICA PARA AS CARTAS (ID 2001 - 2025)
    // =========================================================================================

    // 2001 - Type Zero Magic Crusher
    void Effect_2001_TypeZeroMagicCrusher(CardDisplay source)
    {
        // Discard 1 Spell Card from your hand to inflict 500 damage to your opponent.
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        List<CardData> spells = hand.FindAll(c => c.type.Contains("Spell"));

        if (spells.Count > 0)
        {
            GameManager.Instance.OpenCardSelection(spells, "Descarte 1 Magia", (discarded) => {
                GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                Effect_DirectDamage(source, 500);
            });
        }
    }

    // 2002 - Tyranno Infinity
    void Effect_2002_TyrannoInfinity(CardDisplay source)
    {
        // Original ATK = Banished Dinosaur-Type monsters x 1000.
        if (source.isOnField)
        {
            int count = 0;
            List<CardData> banished = GameManager.Instance.GetPlayerRemoved();
            count += banished.FindAll(c => c.race == "Dinosaur").Count;
            // Nota: Deveria contar os do oponente também se a regra permitir (texto diz "your banished")
            
            int newAtk = count * 1000;
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Original, StatModifier.Operation.Set, newAtk, source));
            Debug.Log($"Tyranno Infinity: ATK definido para {newAtk} ({count} dinossauros banidos).");
        }
    }

    // 2003 - Tyrant Dragon
    void Effect_2003_TyrantDragon(CardDisplay source)
    {
        // Attack twice if opponent has monster. Negate Trap targeting it.
        // Lógica de ataque duplo no BattleManager (verificar flag canAttackAgain).
        // Lógica de negação de Trap:
        var link = GetLinkToNegate(source);
        if (link != null && link.cardSource.CurrentCardData.type.Contains("Trap") && link.target == source)
        {
            NegateAndDestroy(source, link);
        }
    }

    // 2004 - UFO Turtle
    void Effect_2004_UFOTurtle(CardDisplay source) // Based on user registry
    {
        // Destroyed by battle: SS FIRE <= 1500.
        Effect_SearchDeck(source, "Fire", "Monster", 1500); // Deveria ser SS direto
    }

    // 2005 - UFOroid
    void Effect_2005_UFOroid(CardDisplay source) // Based on user registry
    {
        // Destroyed by battle: SS Machine <= 1500.
        Effect_SearchDeck(source, "Machine", "Monster", 1500); // Deveria ser SS direto
    }

    // 2006 - UFOroid Fighter
    void Effect_2006_UFOroidFighter(CardDisplay source) // Based on user registry
    {
        // Fusion: ATK/DEF = Sum of original ATK of materials.
        // Requer sistema de fusão que passe os materiais.
        Debug.Log("UFOroid Fighter: Stats definidos pelos materiais (Lógica pendente no FusionManager).");
    }

    // 2007 - Ultimate Baseball Kid
    void Effect_2007_UltimateBaseballKid(CardDisplay source) // Based on user registry
    {
        // Gain 1000 ATK for each other FIRE monster.
        // Send 1 other FIRE monster to GY to inflict 500 damage.
        
        // Efeito 1: Buff (Passivo/Contínuo - Atualizado no UpdateStats)
        int fireCount = 0;
        if (GameManager.Instance.duelFieldUI != null)
        {
            foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m != source && m.CurrentCardData.attribute == "Fire") fireCount++;
                }
            }
        }
        source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, fireCount * 1000, source));

        // Efeito 2: Burn (Ignition)
        if (source.isOnField)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.isPlayerCard && t != source && t.CurrentCardData.attribute == "Fire",
                    (tribute) => {
                        GameManager.Instance.SendToGraveyard(tribute.CurrentCardData, tribute.isPlayerCard);
                        Destroy(tribute.gameObject);
                        Effect_DirectDamage(source, 500);
                    }
                );
            }
        }
    }

    // 2008 - Ultimate Insect LV1
    void Effect_2008_UltimateInsectLV1(CardDisplay source) // Based on user registry
    {
        // Unaffected by Spells. Standby Phase: Send to GY -> SS LV3.
        Effect_LevelUp(source, "2009");
    }

    // 2009 - Ultimate Insect LV3
    void Effect_2009_UltimateInsectLV3(CardDisplay source) // Based on user registry
    {
        // Debuff, Standby: SS LV5.
        Effect_LevelUp(source, "2010");
    }

    // 2010 - Ultimate Insect LV5
    void Effect_2010_UltimateInsectLV5(CardDisplay source) // Based on user registry
    {
        // If SS by LV3: Opp monsters lose 500 ATK. Standby: SS LV7.
        if (source.wasSpecialSummoned)
        {
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if(z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null) m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, -500, source));
                    }
                }
            }
        }
        Effect_LevelUp(source, "2011");
    }

    // 2011 - Ultimate Insect LV7
    void Effect_2011_UltimateInsectLV7(CardDisplay source) // Based on user registry
    {
        // If SS by LV5: Opp monsters lose 700 ATK/DEF.
        if (source.wasSpecialSummoned)
        {
             if (GameManager.Instance.duelFieldUI != null)
            {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones)
                {
                    if(z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if(m != null)
                        {
                            m.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, -700, source));
                            m.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Field, StatModifier.Operation.Add, -700, source));
                        }
                    }
                }
            }
        }
    }

    // 2012 - Ultimate Obedient Fiend
    void Effect_2012_UltimateObedientFiend(CardDisplay source) // Based on user registry
    {
        // Can only attack if you have no hand and no other monsters.
        // Lógica no BattleManager.CanAttack.
        Debug.Log("Ultimate Obedient Fiend: Restrição de ataque.");
    }

    // 2013 - Ultimate Offering
    void Effect_2013_UltimateOffering(CardDisplay source) // Based on user registry
    {
        // Pay 500 LP: Normal Summon/Set extra.
        if (Effect_PayLP(source, 500))
        {
            Debug.Log("Ultimate Offering: Selecione um monstro da mão para invocar.");
            // Abre seleção da mão para invocar
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> monsters = hand.FindAll(c => c.type.Contains("Monster"));
            
            if (monsters.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(monsters, "Invocar Extra", (selected) => {
                    // Encontra o GameObject correspondente
                    GameObject cardGO = GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == selected);
                    if (cardGO != null)
                    {
                        // Força invocação (ignora limite de 1 por turno)
                        // Precisamos de um método no GameManager que aceite "ignoreLimit" ou similar
                        GameManager.Instance.TrySummonMonster(cardGO, selected, false); 
                    }
                });
            }
        }
    }

    // 2014 - Ultra Evolution Pill
    void Effect_2014_UltraEvolutionPill(CardDisplay source)
    {
        // Tribute 1 Reptile; SS 1 Dinosaur from hand.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.race == "Reptile",
                (tribute) => {
                    GameManager.Instance.TributeCard(tribute);
                    
                    List<CardData> hand = GameManager.Instance.GetPlayerHandData();
                    List<CardData> dinos = hand.FindAll(c => c.race == "Dinosaur" && c.type.Contains("Monster"));
                    
                    if (dinos.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(dinos, "Invocar Dinossauro", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                            GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                        });
                    }
                }
            );
        }
    }

    // 2015 - Umi
    void Effect_2015_Umi(CardDisplay source)
    {
        // Field Spell: Aqua, Fish, Sea Serpent, Thunder +200 ATK/DEF. Machine, Pyro -200.
        Effect_Field(source, 200, 200, "Aqua");
        Effect_Field(source, 200, 200, "Fish");
        Effect_Field(source, 200, 200, "Sea Serpent");
        Effect_Field(source, 200, 200, "Thunder");
        Effect_Field(source, -200, -200, "Machine");
        Effect_Field(source, -200, -200, "Pyro");
    }

    // 2016 - Umiiruka
    void Effect_2016_Umiiruka(CardDisplay source)
    {
        // Field Spell: WATER +500 ATK, -400 DEF.
        Effect_Field(source, 500, -400, "", "Water");
    }

    // 2017 - Union Attack
    void Effect_2017_UnionAttack(CardDisplay source)
    {
        // Target 1 monster; add ATK of all other Attack pos monsters. Cannot inflict battle damage.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.position == CardDisplay.BattlePosition.Attack,
                (target) => {
                    int totalAtk = 0;
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                        {
                            if(z.childCount > 0)
                            {
                                var m = z.GetChild(0).GetComponent<CardDisplay>();
                                if(m != null && m != target && m.position == CardDisplay.BattlePosition.Attack)
                                    totalAtk += m.currentAtk;
                            }
                        }
                    }
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, totalAtk, source));
                    Debug.Log($"Union Attack: +{totalAtk} ATK.");
                    // TODO: Set flag 'cannot inflict damage' on target
                }
            );
        }
    }

    // 2018 - Union Rider
    void Effect_2018_UnionRider(CardDisplay source) // Based on user registry
    {
        // Take control of Union monster.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.description.Contains("Union"), // Simplificado
                (target) => {
                    GameManager.Instance.SwitchControl(target);
                    // Equip logic...
                }
            );
        }
    }

    // 2020 - United We Stand
    void Effect_2020_UnitedWeStand(CardDisplay source) // Based on user registry
    {
        // Equip: +800 ATK/DEF per face-up monster you control.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (target) => {
                    int count = 0;
                    // Conta monstros do controlador da Spell
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
                        foreach(var z in zones) if(z.childCount > 0 && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) count++;
                    }
                    
                    int buff = count * 800;
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, buff, source));
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, buff, source));
                    GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                }
            );
        }
    }

    // 2021 - Unity
    void Effect_2021_Unity(CardDisplay source) // Based on user registry
    {
        // Select 1 monster; DEF becomes sum of all face-up DEF.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && !t.isFlipped,
                (target) => {
                    int totalDef = 0;
                    if (GameManager.Instance.duelFieldUI != null)
                    {
                        foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones)
                        {
                            if(z.childCount > 0)
                            {
                                var m = z.GetChild(0).GetComponent<CardDisplay>();
                                if(m != null && !m.isFlipped) totalDef += m.originalDef;
                            }
                        }
                    }
                    target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, totalDef, source));
                    Debug.Log($"Unity: DEF definida para {totalDef}.");
                }
            );
        }
    }

    // 2023 - Unshaven Angler
    void Effect_2023_UnshavenAngler(CardDisplay source)
    {
        // Treated as 2 Tributes for WATER monster.
        Debug.Log("Unshaven Angler: Vale por 2 tributos para WATER.");
    }

    // 2024 - Upstart Goblin
    void Effect_2024_UpstartGoblin(CardDisplay source)
    {
        // Draw 1 card. Opponent gains 1000 LP.
        GameManager.Instance.DrawCard();
        GameManager.Instance.GainLifePoints(!source.isPlayerCard, 1000);
    }
    // 2027 - Valkyrion the Magna Warrior
    void Effect_2027_ValkyrionTheMagnaWarrior(CardDisplay source)
    {
        // Ignition: Tribute this to SS Alpha, Beta, Gamma from GY.
        if (source.isOnField)
        {
            UIManager.Instance.ShowConfirmation("Separar Valkyrion?", () => {
                GameManager.Instance.TributeCard(source);
                List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                string[] parts = { "Alpha The Magnet Warrior", "Beta The Magnet Warrior", "Gamma The Magnet Warrior" };
                foreach(var partName in parts)
                {
                    CardData part = gy.Find(c => c.name == partName);
                    if (part != null)
                    {
                        GameManager.Instance.SpecialSummonFromData(part, source.isPlayerCard);
                        gy.Remove(part);
                    }
                }
            });
        }
    }

    // 2028 - Vampire Baby
    void Effect_2028_VampireBaby(CardDisplay source)
    {
        // If destroys monster by battle: SS it to your field at End of Battle Phase.
        Debug.Log("Vampire Baby: Efeito de recrutamento configurado (Lógica no OnBattleEnd).");
    }

    // 2029 - Vampire Genesis
    void Effect_2029_VampireGenesis(CardDisplay source)
    {
        // Once per turn: Discard 1 Zombie; SS 1 Zombie from GY with Level < discarded.
        if (source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> zombies = hand.FindAll(c => c.race == "Zombie" && c.type.Contains("Monster"));
            
            if (zombies.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(zombies, "Descarte 1 Zumbi", (discarded) => {
                    GameManager.Instance.DiscardCard(GameManager.Instance.playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == discarded).GetComponent<CardDisplay>());
                    
                    List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                    List<CardData> targets = gy.FindAll(c => c.race == "Zombie" && c.level < discarded.level);
                    
                    if (targets.Count > 0)
                    {
                        GameManager.Instance.OpenCardSelection(targets, "Reviver Zumbi", (selected) => {
                            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                        });
                    }
                });
            }
        }
    }

    // 2030 - Vampire Lady
    void Effect_2030_VampireLady(CardDisplay source)
    {
        // If inflicts battle damage: Declare type; opp sends 1 from Deck to GY.
        Debug.Log("Vampire Lady: Efeito de mill configurado (Lógica no OnDamageDealtImpl).");
    }

    // 2031 - Vampire Lord
    void Effect_2031_VampireLord(CardDisplay source)
    {
        // If inflicts battle damage: Declare type; opp sends 1 from Deck to GY.
        // If destroyed by opp card effect: SS next Standby.
        Debug.Log("Vampire Lord: Efeitos de mill e renascimento configurados.");
    }

    // 2032 - Vampire's Curse
    void Effect_2032_VampiresCurse(CardDisplay source)
    {
        // If destroyed by battle: Pay 500 LP; SS next Standby. Gains 500 ATK.
        Debug.Log("Vampire's Curse: Efeito de renascimento configurado.");
    }

    // 2033 - Vampiric Orchis
    void Effect_2033_VampiricOrchis(CardDisplay source)
    {
        // When Normal Summoned: SS 1 "Des Dendle" from hand.
        if (source.summonedThisTurn && !source.wasSpecialSummoned)
        {
            Effect_SearchDeck(source, "Des Dendle", "Monster"); // Should be SS from hand
        }
    }

    // 2034 - Van'Dalgyon the Dark Dragon Lord
    void Effect_2034_VanDalgyonTheDarkDragonLord(CardDisplay source)
    {
        // If Counter Trap negates: SS this card.
        Debug.Log("Van'Dalgyon: Invocação por Counter Trap (Requer hook).");
    }

    // 2035 - Vengeful Bog Spirit
    void Effect_2035_VengefulBogSpirit(CardDisplay source)
    {
        // Monsters cannot attack the turn they are Summoned.
        Debug.Log("Vengeful Bog Spirit: Enjoo de invocação ativo (Lógica no BattleManager).");
    }

    // 2037 - Versago the Destroyer
    void Effect_2037_VersagoTheDestroyer(CardDisplay source)
    {
        // Fusion Substitute.
        Debug.Log("Versago: Substituto de fusão (Lógica no FusionManager).");
    }

    // 2038 - Victory Dragon
    void Effect_2038_VictoryDragon(CardDisplay source)
    {
        // Direct attack reduces LP to 0 = Match Win.
        Debug.Log("Victory Dragon: Efeito de vitória de partida (Lógica no OnDamageDealtImpl).");
    }

    // 2039 - Vile Germs
    void Effect_2039_VileGerms(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Plant");
    }

    // 2040 - Vilepawn Archfiend
    void Effect_2040_VilepawnArchfiend(CardDisplay source)
    {
        // Opponent cannot attack other Archfiends.
        Debug.Log("Vilepawn Archfiend: Proteção de Archfiends (Lógica no BattleManager).");
    }

    // 2042 - Violet Crystal
    void Effect_2042_VioletCrystal(CardDisplay source)
    {
        Effect_Equip(source, 300, 300, "Zombie");
    }

    // 2043 - Virus Cannon
    void Effect_2043_VirusCannon(CardDisplay source)
    {
        // Tribute any number of non-Tokens; opp sends equal Spells from Deck to GY.
        if (SummonManager.Instance.HasEnoughTributes(1, source.isPlayerCard))
        {
            GameManager.Instance.TributeCard(source); // Seleção pendente
            GameManager.Instance.MillCards(!source.isPlayerCard, 1); // Deveria ser Spells do Deck
            Debug.Log("Virus Cannon: Magias enviadas ao GY.");
        }
    }

    // 2044 - Viser Des
    void Effect_2044_ViserDes(CardDisplay source)
    {
        // Normal Summon: Target 1 opp monster. Destroy it after 3 turns.
        if (source.summonedThisTurn && !source.wasSpecialSummoned)
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && !t.isPlayerCard,
                    (target) => {
                        Debug.Log($"Viser Des: {target.CurrentCardData.name} marcado para destruição em 3 turnos.");
                        // Lógica de contagem de turnos pendente
                    }
                );
            }
        }
    }


    // 2047 - Waboku
    void Effect_2047_Waboku(CardDisplay source)
    {
        // No battle damage, monsters not destroyed by battle this turn.
        Debug.Log("Waboku: Proteção total de batalha este turno (Flags no BattleManager).");
    }

    // 2048 - Wall Shadow
    void Effect_2048_WallShadow(CardDisplay source)
    {
        // SS via Magical Labyrinth.
        Debug.Log("Wall Shadow: Invocado via Magical Labyrinth (Lógica no efeito do Labirinto).");
    }

    // 2049 - Wall of Illusion
    void Effect_2049_WallOfIllusion(CardDisplay source)
    {
        // If attacked: Return attacker to hand.
        Debug.Log("Wall of Illusion: Efeito de bounce configurado (Lógica no OnBattleEnd).");
    }

    // 2050 - Wall of Revealing Light
    void Effect_2050_WallOfRevealingLight(CardDisplay source)
    {
        // Pay multiple of 1000. Monsters with ATK <= paid cannot attack.
        if (Effect_PayLP(source, 1000)) // Simulação: Paga 1000
        {
            Debug.Log("Wall of Revealing Light: Bloqueio de ataque <= 1000 (Lógica no BattleManager).");
        }
    }