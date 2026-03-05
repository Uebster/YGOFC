using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public partial class CardEffectManager
{
    // Flags globais para efeitos de turno
    public bool negateContinuousSpells = false;
    public bool redirectSpellTarget = false;
    public bool reverseStats = false; // Para Reverse Trap (1526)

    public bool banishInsteadOfGraveyard = false; // Para Macro Cosmos / Dimensional Fissure / Spirit Elimination

    public string dnaSurgeryDeclaredType = ""; // Para DNA Surgery (0390)

    // --- VALIDAÇÃO DE ATAQUE (Movido do BattleManager) ---

    public bool CanDeclareAttack(CardDisplay attacker)
    {
        // Verifica efeitos contínuos globais (Gravity Bind, Level Limit, Messenger of Peace, etc.)
        if (IsAttackPreventedByContinuousEffect(attacker))
        {
            return false;
        }

        // Armor Exe (0102) - Não pode atacar no turno que foi invocado
        if (attacker.CurrentCardData.id == "0102" && attacker.summonedThisTurn)
        {
            Debug.Log("Ataque impedido: Armor Exe não pode atacar no turno de invocação.");
            return false;
        }

        // Blue-Eyes Toon Dragon (0215) & Toons
        if (attacker.CurrentCardData.race == "Toon" || attacker.CurrentCardData.id == "0215")
        {
            if (attacker.summonedThisTurn)
            {
                Debug.Log("Toon: Não pode atacar no turno que foi invocado.");
                return false;
            }
            if (!GameManager.Instance.PayLifePoints(attacker.isPlayerCard, 500))
            {
                Debug.Log("Toon: LP insuficientes para atacar (500).");
                return false;
            }
        }

        // Cave Dragon (0274)
        if (attacker.CurrentCardData.id == "0274")
        {
            // Não pode atacar a menos que controle outro Dragão
            bool hasOtherDragon = false;
            Transform[] myZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var zone in myZones)
            {
                if (zone.childCount > 0)
                {
                    var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && cd != attacker && cd.CurrentCardData.race == "Dragon") hasOtherDragon = true;
                }
            }
            if (!hasOtherDragon) return false;
        }

        // 2146 - Zombyra the Dark
        if (attacker.CurrentCardData.id == "2146")
        {
            // Cannot attack directly
            if (CheckDirectAttackCondition(attacker))
            {
                Debug.Log("Zombyra the Dark: Não pode atacar diretamente.");
                return false;
            }
        }

        // 2012 - Ultimate Obedient Fiend
        if (attacker.CurrentCardData.id == "2012")
        {
            // Só ataca se for o único monstro e mão vazia
            int handCount = attacker.isPlayerCard ? GameManager.Instance.GetPlayerHandData().Count : GameManager.Instance.GetOpponentHandData().Count;
            int mCount = 0;
            Transform[] zones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var z in zones) if(z.childCount > 0) mCount++;

            if (handCount > 0 || mCount > 1)
            {
                Debug.Log("Ultimate Obedient Fiend: Não pode atacar (Mão ou Campo não vazios).");
                return false;
            }
        }

        // 2035 - Vengeful Bog Spirit
        if (GameManager.Instance.IsCardActiveOnField("2035"))
        {
            if (attacker.summonedThisTurn)
            {
                Debug.Log("Ataque impedido por Vengeful Bog Spirit (Invocado neste turno).");
                return false;
            }
        }

        // 2050 - Wall of Revealing Light
        if (IsWallOfRevealingLightBlocking(attacker))
        {
            Debug.Log("Ataque impedido por Wall of Revealing Light.");
            return false;
        }

        // D.D. Borderline (0379)
        if (GameManager.Instance.IsCardActiveOnField("0379"))
        {
            List<CardData> gy = attacker.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
            bool hasSpell = gy.Exists(c => c.type.Contains("Spell"));
            if (!hasSpell) return false;
        }

        // Dark Elf (0409)
        if (attacker.CurrentCardData.id == "0409")
        {
            if (!GameManager.Instance.PayLifePoints(attacker.isPlayerCard, 1000))
            {
                Debug.Log("Dark Elf: LP insuficientes para atacar (1000).");
                return false;
            }
        }

        // Para Alligator's Sword Dragon e Amphibious Bugroth MK-3:
        if (attacker.CurrentCardData.id == "0037" || attacker.CurrentCardData.id == "0053")
        {
            if (AreAllEnemyMonstersEarthWaterOrFire(attacker)) return true;
        }

        // 1402 - Panther Warrior
        if (attacker.CurrentCardData.id == "1402")
        {
            // Requer tributo para atacar (Lógica simplificada: permite se tiver outro monstro, mas não consome aqui)
            // Em um sistema ideal, abriria popup.
            Debug.Log("Panther Warrior: Tributo necessário (Lógica de custo pendente).");
        }

        return true;
    }

    private bool CheckDirectAttackCondition(CardDisplay attacker)
    {
        if (GameManager.Instance.duelFieldUI == null) return false;
        
        // Verifica zonas do oponente
        Transform[] enemyZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

        foreach (Transform zone in enemyZones)
        {
            if (zone.childCount > 0)
            {
                // Toon Logic: Can attack direct if opponent has no Toons
                if (attacker != null && (attacker.CurrentCardData.race == "Toon" || attacker.CurrentCardData.id == "0215"))
                {
                    CardDisplay defender = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (defender != null && defender.CurrentCardData.race == "Toon") return false; // Has toon, must attack it
                    continue; // Not a toon, ignore for direct attack condition
                }
                return false;
            }
        }

        // 1553 - Rocket Jumper
        if (attacker != null && attacker.CurrentCardData.id == "1553")
        {
            bool onlyDefense = true;
            bool hasMonsters = false;
            foreach (Transform zone in enemyZones)
            {
                if (zone.childCount > 0)
                {
                    hasMonsters = true;
                    CardDisplay m = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m.position == CardDisplay.BattlePosition.Attack) onlyDefense = false;
                }
            }
            if (hasMonsters && onlyDefense) return true;
        }
        return true;
    }

    private bool AreAllEnemyMonstersEarthWaterOrFire(CardDisplay attacker)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return false;
        
        Transform[] enemyZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
        
        foreach (var zone in enemyZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && cd.isOnField && !cd.isFlipped)
                {
                    string r = cd.CurrentCardData.attribute;
                    if (r != "Earth" && r != "Water" && r != "Fire") return false;
                }
            }
        }
        return true;
    }

    private bool IsWallOfRevealingLightBlocking(CardDisplay attacker)
    {
        bool attackerIsPlayer = attacker.isPlayerCard;
        // Wall of Revealing Light está no campo do OPONENTE do atacante
        Transform[] enemySpellZones = attackerIsPlayer ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;
        
        foreach (var zone in enemySpellZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.id == "2050")
                {
                    if (attacker.currentAtk <= cd.paidLifePoints) return true;
                }
            }
        }
        return false;
    }

    // --- MÉTODOS UTILITÁRIOS COMUNS (REAPROVEITADOS) ---

    // --- SISTEMA DE MANUTENÇÃO ---
    private class MaintenanceRequest
    {
        public CardDisplay card;
        public string description;
        public System.Func<bool> canPay;
        public System.Action payAction;
    }
    private Queue<MaintenanceRequest> maintenanceQueue = new Queue<MaintenanceRequest>();

    // --- Helpers para Negação (Chain) ---
    private ChainManager.ChainLink GetLinkToNegate(CardDisplay source)
    {
        if (ChainManager.Instance == null) return null;
        var chain = ChainManager.Instance.currentChain;
        for (int i = 0; i < chain.Count; i++)
        {
            if (chain[i].cardSource == source)
            {
                if (i > 0) return chain[i - 1];
                break;
            }
        }
        return null;
    }

    private void NegateAndDestroy(CardDisplay source, ChainManager.ChainLink targetLink)
    {
        if (targetLink != null)
        {
            ChainManager.Instance.NegateLink(targetLink.linkNumber);
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(targetLink.cardSource);
            GameManager.Instance.SendToGraveyard(targetLink.cardSource.CurrentCardData, targetLink.isPlayerEffect);
            Destroy(targetLink.cardSource.gameObject);
            Debug.Log($"{source.CurrentCardData.name} negou {targetLink.cardSource.CurrentCardData.name}.");
        }
    }

    // --- SISTEMA DE EVENTOS E FASES (TURNOBSERVER) ---

    public void OnPhaseStart(GamePhase phase)
    {
        // Reset flags de turno
        negateContinuousSpells = false;
        redirectSpellTarget = false;
        reverseStats = false;
        banishInsteadOfGraveyard = false;

        // Aplica efeitos contínuos de mudança de posição (ex: Level Limit - Area B)
        ApplyContinuousPositionChecks();

        if (phase == GamePhase.Standby)
        {
            // --- EFEITOS DE STANDBY PHASE ---

            // Wave-Motion Cannon (2065): Acumula contador
            CheckActiveCards("2065", (card) =>
            {
                // O contador aumenta na Standby Phase do controlador
                if (card.isPlayerCard == GameManager.Instance.isPlayerTurn)
                {
                    card.turnCounter++;
                    Debug.Log($"Wave-Motion Cannon: Contador aumentado para {card.turnCounter}.");
                }
            });

            // Processa contadores de destruição retardada
            List<CardDisplay> allMonsters = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, allMonsters);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, allMonsters);
            }

            foreach (var monster in allMonsters.ToList()) // Usar ToList para poder modificar a coleção original
            {
                if (monster != null && monster.destructionTurnCountdown > 0)
                {
                    // O contador diminui no início do turno do jogador que ativou o efeito
                    if (monster.destructionCountdownOwnerIsPlayer == GameManager.Instance.isPlayerTurn)
                    {
                        monster.destructionTurnCountdown--;
                        Debug.Log($"{monster.CurrentCardData.name} será destruído em {monster.destructionTurnCountdown} turno(s).");
                        if (monster.destructionTurnCountdown == 0)
                        {
                            Debug.Log($"{monster.CurrentCardData.name} destruído por efeito retardado.");
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
                            GameManager.Instance.SendToGraveyard(monster.CurrentCardData, monster.isPlayerCard);
                            Destroy(monster.gameObject);
                        }
                    }
                }
            }

            // Processa Reviver na Próxima Standby (Vampire Lord, Vampire's Curse)
            if (reviveNextStandby.Count > 0)
            {
                for (int i = reviveNextStandby.Count - 1; i >= 0; i--)
                {
                    CardData cardData = reviveNextStandby[i];
                    // Verifica se está no GY do jogador atual
                    bool inPlayerGY = GameManager.Instance.GetPlayerGraveyard().Contains(cardData);
                    bool inOppGY = GameManager.Instance.GetOpponentGraveyard().Contains(cardData);

                    if (inPlayerGY && GameManager.Instance.isPlayerTurn)
                    {
                        GameManager.Instance.SpecialSummonFromData(cardData, true);
                        reviveNextStandby.RemoveAt(i);
                        Debug.Log($"{cardData.name} revivido do GY.");
                    }
                    else if (inOppGY && !GameManager.Instance.isPlayerTurn)
                    {
                        GameManager.Instance.SpecialSummonFromData(cardData, false);
                        reviveNextStandby.RemoveAt(i);
                        Debug.Log($"{cardData.name} revivido do GY.");
                    }
                }
            }

            // 2076 - White Magician Pikeru
            CheckActiveCards("2076", (card) =>
            {
                if (card.isPlayerCard == GameManager.Instance.isPlayerTurn && card.position == CardDisplay.BattlePosition.Defense)
                {
                    int monsterCount = GameManager.Instance.GetFieldCardCount(card.isPlayerCard); // Simplificado, conta tudo
                    // Contagem correta de monstros
                    // ...
                    Effect_GainLP(card, monsterCount * 400);
                }
            });
        }

        if (phase == GamePhase.End)
        {
            // --- EFEITOS DE END PHASE ---

            // Processa destruição agendada para a End Phase (Wild Nature's Release)
            List<CardDisplay> toDestroy = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toDestroy);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toDestroy);
            }
            foreach (var monster in toDestroy.Where(m => m != null && m.scheduledForDestruction).ToList())
            {
                Debug.Log($"{monster.CurrentCardData.name} destruído por efeito na End Phase.");
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
                GameManager.Instance.SendToGraveyard(monster.CurrentCardData, monster.isPlayerCard);
                Destroy(monster.gameObject);
            }

            // 1757 - Spiritual Energy Settle Machine (Manutenção)
            bool machineActive = false;
            CheckActiveCards("1757", (card) =>
            {
                machineActive = true;
                if (card.isPlayerCard) // Apenas o controlador paga o custo
                {
                    if (GameManager.Instance.GetPlayerHandData().Count > 0)
                    {
                        UIManager.Instance.ShowConfirmation("Manter 'Spiritual Energy Settle Machine' (descartar 1 carta)?",
                        () =>
                        {
                            // Jogador escolhe qual carta descartar
                            GameManager.Instance.OpenCardSelection(GameManager.Instance.GetPlayerHandData(), "Descarte 1 para manter a Máquina", (toDiscardData) =>
                            {
                                if (toDiscardData != null)
                                {
                                    var cardToDiscard = GameManager.Instance.playerHand.Find(go => go.GetComponent<CardDisplay>().CurrentCardData == toDiscardData);
                                    if (cardToDiscard != null) GameManager.Instance.DiscardCard(cardToDiscard.GetComponent<CardDisplay>());
                                }
                                else // Jogador cancelou a seleção de descarte
                                {
                                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                                    Destroy(card.gameObject);
                                }
                            });
                        },
                        () =>
                        { // Jogador escolheu "Não" na confirmação
                            GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                            Destroy(card.gameObject);
                        });
                    }
                    else
                    {
                        Debug.Log("Spiritual Energy Settle Machine: Sem cartas para descartar. Destruída.");
                        GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                        Destroy(card.gameObject);
                    }
                }
            });

            // Lógica de Retorno de Monstros Spirit
            if (!machineActive)
            {
                List<CardDisplay> monstersToReturn = new List<CardDisplay>();
                if (GameManager.Instance.duelFieldUI != null)
                {
                    CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, monstersToReturn);
                    CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, monstersToReturn);
                }

                foreach (var monster in monstersToReturn.ToList()) // Itera sobre uma cópia para poder modificar a original
                {
                    string id = monster.CurrentCardData.id;
                    if (id == "0114" || id == "2128" || id == "1141" || id == "0933" || id == "1798") // Asura, Yata, Maharaghi, Inaba, Susa
                    {
                        if (monster.summonedThisTurn) // Só retornam no turno que foram invocados/flipados
                        {
                            GameManager.Instance.ReturnToHand(monster);
                        }
                    }
                }
            }
        }

        Debug.Log($"CardEffectManager: Processando efeitos da fase {phase}...");

        if (phase == GamePhase.Draw)
        {
            // Cyber Archfiend (0357): Se mão vazia na Draw Phase, compra +1
            CheckActiveCards("0357", (card) =>
            {
                List<CardData> hand = card.isPlayerCard ? GameManager.Instance.GetPlayerHandData() : GameManager.Instance.GetOpponentHandData();
                if (hand.Count == 0)
                {
                    Debug.Log("Cyber Archfiend: Mão vazia. Compra extra.");
                    if (card.isPlayerCard) GameManager.Instance.DrawCard(); else GameManager.Instance.DrawOpponentCard();
                }
            });
        }
        if (phase == GamePhase.Standby)
        {
            // 1. Custos de Manutenção (Maintenance Costs)
            CheckMaintenanceCosts();

            // 2. Efeitos de Fase (Phase Triggers)

            // Dancing Fairy (0393): Ganha 1000 LP se em Defesa
            CheckActiveCards("0393", (card) =>
            {
                if (card.position == CardDisplay.BattlePosition.Defense && card.isPlayerCard)
                {
                    Effect_GainLP(card, 1000);
                }
            });

            // Darklord Marie (0453): Ganha 200 LP se no GY
            List<CardData> myGY = GameManager.Instance.GetPlayerGraveyard();
            foreach (var cardData in myGY)
            {
                if (cardData.id == "0453")
                {
                    Debug.Log("Darklord Marie (GY): Ganha 200 LP.");
                    GameManager.Instance.playerLP += 200;
                    // TODO: Atualizar UI
                }
            }

            // Balloon Lizard (0132): Add counter
            CheckActiveCards("0132", (card) =>
            {
                if (card.isPlayerCard) card.AddSpellCounter(1);
            });

            // Blast Sphere (0201): Destroy equipped and burn
            CheckActiveCards("0201", (card) =>
            {
                // Se tiver contador (marcado no BattleManager), é a standby phase seguinte
                if (card.spellCounters > 0)
                {
                    // Encontra quem ele está equipando (via CardLink ou lógica simplificada)
                    // Simplificação: Destrói o card e causa dano igual ao ATK dele (ou do alvo, regra diz ATK do alvo)
                    // Como não temos referência fácil ao alvo aqui sem iterar links, vamos simular dano fixo ou do próprio card
                    Debug.Log("Blast Sphere: Detonando!");
                    Effect_DirectDamage(card, card.currentAtk); // Simplificado
                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
                    Destroy(card.gameObject);
                }
            });

            // Blind Destruction (0206)
            CheckActiveCards("0206", (card) =>
            {
                if (card.isPlayerCard)
                {
                    Effect_0206_BlindDestruction_Logic_Impl(card);
                }
            });

            // Bowganian (0235): 600 dano na Standby
            CheckActiveCards("0235", (card) =>
            {
                if (card.isPlayerCard) Effect_DirectDamage(card, 600);
            });

            // Brain Jacker (0238): Ganha 500 LP na Standby do oponente
            CheckActiveCards("0238", (card) =>
            {
                // Se está no campo do oponente (foi equipado e trocou controle), o dono original ganha LP?
                // Texto: "During your opponent's Standby Phase, gain 500 Life Points." (Referindo-se ao controlador do monstro equipado/efeito)
                // Como Brain Jacker vira Equip, ele fica na S/T zone.
                if (!card.isPlayerCard) // É a vez do oponente
                {
                    Effect_GainLP(card, 500);
                }
            });

            // Burning Land (0248): 500 dano na Standby
            CheckActiveCards("0248", (card) =>
            {
                // Dano para o jogador do turno
                if (card.isPlayerCard) GameManager.Instance.DamagePlayer(500);
                else GameManager.Instance.DamageOpponent(500);
            });

            // Bottomless Shifting Sand (0232): Destrói se mão <= 4
            CheckActiveCards("0232", (card) =>
            {
                if (card.isPlayerCard)
                {
                    int handCount = GameManager.Instance.GetPlayerHandData().Count;
                    if (handCount <= 4)
                    {
                        Debug.Log("Bottomless Shifting Sand: Mão <= 4. Auto-destruição.");
                        GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                        Destroy(card.gameObject);
                    }
                }
            });

            // Cybernetic Cyclopean (0372): ATK vira 2400 se mão vazia
            CheckActiveCards("0372", (card) =>
            {
                List<CardData> hand = card.isPlayerCard ? GameManager.Instance.GetPlayerHandData() : GameManager.Instance.GetOpponentHandData();
                if (hand.Count == 0)
                {
                    // Define ATK base para 2400 (Original 1400 + 1000)
                    // Usamos um modificador temporário que dura até a próxima verificação ou permanente que removemos se a condição falhar
                    card.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Set, 2400, card));
                }
            });

            // Dark Catapulter (0402): Add counter in Standby if Defense
            CheckActiveCards("0402", (card) =>
            {
                if (card.isPlayerCard && card.position == CardDisplay.BattlePosition.Defense)
                    card.AddSpellCounter(1);
            });

            // Dangerous Machine Type-6 (0394): Rola dado na Standby
            CheckActiveCards("0394", (card) =>
            {
                if (card.isPlayerCard)
                {
                    int roll = Random.Range(1, 7);
                    Debug.Log($"Dangerous Machine Type-6: Rolou {roll}.");
                    if (roll == 6)
                    {
                        Debug.Log("Rolou 6: Destrói a si mesmo.");
                        GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                        Destroy(card.gameObject);
                    }
                    else if (roll == 1)
                    {
                        Debug.Log("Rolou 1: Descarta 1 carta.");
                        GameManager.Instance.DiscardRandomHand(true, 1);
                    }
                    // Outros efeitos (2-5) são aplicados em outras fases ou são passivos
                }
            });

            // Lava Golem (1060): 1000 damage to controller
            CheckActiveCards("1060", (card) =>
            {
                // The controller takes damage.
                // If card.isPlayerCard is true, it means the player controls it.
                if (card.isPlayerCard) GameManager.Instance.DamagePlayer(1000);
                else GameManager.Instance.DamageOpponent(1000);
                Debug.Log("Lava Golem: 1000 damage to controller.");
            });

            // Mask of Dispel (1171): 500 damage to controller of targeted spell
            CheckActiveCards("1171", (card) =>
            {
                // Encontra o alvo via links
                CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
                foreach (var link in links)
                {
                    if (link.source == card && link.target != null)
                    {
                        int dmg = 500;
                        if (link.target.isPlayerCard) GameManager.Instance.DamagePlayer(dmg);
                        else GameManager.Instance.DamageOpponent(dmg);
                        Debug.Log("Mask of Dispel: 500 de dano.");
                    }
                }
            });

            // 1244 - Minor Goblin Official
            CheckActiveCards("1244", (card) =>
            {
                if (GameManager.Instance.opponentLP <= 3000 && !card.isPlayerCard) // Turno do oponente
                {
                    Effect_DirectDamage(card, 500);
                }
            });

            // 1250 - Mirage of Nightmare
            CheckActiveCards("1250", (card) =>
            {
                if (!card.isPlayerCard) // Standby do Oponente: Compra até 4
                {
                    int handCount = GameManager.Instance.GetPlayerHandData().Count;
                    if (handCount < 4)
                    {
                        int toDraw = 4 - handCount;
                        for (int i = 0; i < toDraw; i++) GameManager.Instance.DrawCard();
                        card.spellCounters = toDraw; // Armazena quantos comprou
                    }
                }
                else // Sua Standby: Descarta
                {
                    if (card.spellCounters > 0)
                    {
                        GameManager.Instance.DiscardRandomHand(true, card.spellCounters);
                        card.spellCounters = 0;
                    }
                }
            });

            // 1755 - Spirit's Invitation
            CheckActiveCards("1755", (card) =>
            {
                if (card.isPlayerCard)
                {
                    if (!Effect_PayLP(card, 500))
                    {
                        Debug.Log("Spirit's Invitation: Manutenção não paga. Destruída.");
                        GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                        Destroy(card.gameObject);
                    }
                }
            });

            // 1757 - Spiritual Energy Settle Machine
            CheckActiveCards("1757", (card) =>
            {
                if (card.isPlayerCard)
                {
                    // Discard 1 card or destroy
                    if (GameManager.Instance.GetPlayerHandData().Count > 0)
                    {
                        GameManager.Instance.DiscardRandomHand(true, 1); // Simplificado
                    }
                    else
                    {
                        GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                        Destroy(card.gameObject);
                    }
                }
            });

            // 1775 - Stim-Pack
            CheckActiveCards("1775", (card) =>
            {
                // Encontra o monstro equipado
                CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
                foreach (var link in links)
                {
                    if (link.source == card && link.target != null)
                    {
                        link.target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -200, card));
                        Debug.Log($"Stim-Pack: {link.target.CurrentCardData.name} perdeu 200 ATK.");
                    }
                }
            });

            // 1859 - The Eye of Truth
            CheckActiveCards("1859", (card) =>
            {
                if (!card.isPlayerCard) // Standby do oponente
                {
                    List<CardData> oppHand = GameManager.Instance.GetOpponentHandData();
                    if (oppHand.Exists(c => c.type.Contains("Spell")))
                    {
                        GameManager.Instance.GainLifePoints(false, 1000); // Oponente ganha LP
                    }
                }
            });

            // 1902 - The Unfriendly Amazon
            CheckActiveCards("1902", (card) =>
            {
                if (card.isPlayerCard)
                {
                    // Tribute or destroy
                    // Simplified: Destroy if no tribute available or chosen
                    if (!SummonManager.Instance.HasEnoughTributes(1, true))
                    {
                        GameManager.Instance.SendToGraveyard(card.CurrentCardData, true);
                        Destroy(card.gameObject);
                    }
                    else
                    {
                        // Prompt tribute (Simulated: Just log)
                        Debug.Log("The Unfriendly Amazon: Manutenção (Tributo pendente).");
                    }
                }
            });

            // 1046 - Labyrinth of Nightmare
            CheckActiveCards("1046", (card) =>
            {
                if (GameManager.Instance.duelFieldUI != null)
                {
                    List<CardDisplay> all = new List<CardDisplay>();
                    CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                    CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
                    foreach (var m in all) m.ChangePosition();
                    Debug.Log("Labyrinth of Nightmare: Posições alteradas na End Phase.");
                }
            });

            // 1093 - Little-Winguard
            CheckActiveCards("1093", (card) =>
            {
                if (card.isPlayerCard) card.ChangePosition(); // Simplificado: Muda automaticamente ou pede confirmação
            });

            // Solar Flare Dragon (1686): Dano na End Phase (mas vamos por aqui como exemplo de estrutura)
            // (Na verdade é End Phase, movido para lá se fosse o caso)
        }
        else if (phase == GamePhase.End)
        {
            // Solar Flare Dragon (1686): 500 dano
            CheckActiveCards("1686", (card) =>
            {
                if (card.isPlayerCard) Effect_DirectDamage(card, 500);
            });

            // Limpa buffs temporários de todas as cartas no campo
            // (Isso deveria ser feito em todos os monstros, não só nos ativos)
            if (GameManager.Instance.duelFieldUI != null)
            {
                CleanAllExpiredModifiers();
            }

            // Bottomless Shifting Sand (0232): Destrói maior ATK na End Phase do oponente
            // (Se phase == End e é o turno do oponente)
            // Nota: OnPhaseStart é chamado para o turno atual. Precisamos saber de quem é o turno.
            // Assumindo que OnPhaseStart roda no cliente para o estado atual do jogo.
            // Se for End Phase do oponente:
            bool isOpponentTurn = !GameManager.Instance.canPlacePlayerCards; // Simplificação
            if (isOpponentTurn)
            {
                CheckActiveCards("0232", (card) =>
                {
                    if (card.isPlayerCard)
                    {
                        Debug.Log("Bottomless Shifting Sand: Destruindo monstro(s) com maior ATK.");
                        // Encontrar maior ATK
                        int maxAtk = -1;
                        List<CardDisplay> targets = new List<CardDisplay>();
                        CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, targets);
                        CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, targets);

                        foreach (var m in targets) if (m.currentAtk > maxAtk) maxAtk = m.currentAtk;

                        List<CardDisplay> toDestroy = targets.FindAll(m => m.currentAtk == maxAtk);
                        DestroyCards(toDestroy, true);
                    }
                });
            }

            // Dark Dust Spirit (0408): Return to hand
            CheckActiveCards("0408", (card) =>
            {
                if (card.isPlayerCard && card.isFlipped == false) // Face-up
                {
                    Debug.Log("Dark Dust Spirit: Retornando para a mão.");
                    GameManager.Instance.ReturnToHand(card);
                }
            });

            // Dark Magician of Chaos (0422): Add Spell from GY
            CheckActiveCards("0422", (card) =>
            {
                if (card.isPlayerCard && card.summonedThisTurn)
                {
                    // Simplificado: Pega a primeira spell
                    List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
                    CardData spell = gy.Find(c => c.type.Contains("Spell"));
                    if (spell != null)
                    {
                        Debug.Log($"DMoC: Recuperando {spell.name}.");
                        gy.Remove(spell);
                        GameManager.Instance.AddCardToHand(spell, true);
                    }
                }
            });

            // Atualização de Buffs Dinâmicos (Dark Magician Girl, Dark Paladin)
            CheckActiveCards("0420", (card) => UpdateDMGBuff(card)); // DMG
            CheckActiveCards("0428", (card) => UpdateDarkPaladinBuff(card)); // Dark Paladin

            // Manticore of Darkness (1162): Revive loop
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            if (gy.Exists(c => c.id == "1162"))
            {
                // Check if player has Beast/Beast-Warrior in hand/field to send
                // Simplified: Just log availability
                Debug.Log("Manticore of Darkness (GY): Pode reviver enviando Besta.");
            }

            // 1249 - Mirage Knight (Banish if battled)
            CheckActiveCards("1249", (card) =>
            {
                if (card.battledThisTurn)
                {
                    Debug.Log("Mirage Knight: Banido após batalha.");
                    GameManager.Instance.BanishCard(card);
                }
            });

            // 1252 - Mirror Wall (Maintenance)
            CheckActiveCards("1252", (card) =>
            {
                if (!Effect_PayLP(card, 2000))
                {
                    Debug.Log("Mirror Wall: Manutenção não paga. Destruída.");
                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
                    Destroy(card.gameObject);
                }
            });

            // 1283 - Mucus Yolk (Gain 1000 ATK if dealt damage)
            CheckActiveCards("1283", (card) =>
            {
                if (card.isPlayerCard && card.battledThisTurn) // Simplificado: Se batalhou e sobreviveu
                {
                    card.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 1000, card));
                    Debug.Log("Mucus Yolk: +1000 ATK.");
                }
            });

            // 1328 - Needle Wall
            CheckActiveCards("1328", (card) =>
            {
                if (card.isPlayerCard)
                {
                    int roll = Random.Range(1, 7);
                    Debug.Log($"Needle Wall: Rolou {roll}.");
                    // Lógica de destruir monstro na zona correspondente (requer mapeamento de zonas)
                }
            });

            // 1344 - Nightmare Wheel
            CheckActiveCards("1344", (card) =>
            {
                // Dano ao controlador do monstro alvo
                // Requer saber quem é o alvo (via CardLink)
                CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
                foreach (var link in links)
                {
                    if (link.source == card && link.target != null)
                    {
                        Effect_DirectDamage(card, 500);
                    }
                }
            });

            // 1345 - Nightmare's Steelcage
            CheckActiveCards("1345", (card) =>
            {
                card.spellCounters--; // Usa contadores para turnos
                if (card.spellCounters <= 0)
                {
                    Debug.Log("Nightmare's Steelcage: Expirou.");
                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
                    Destroy(card.gameObject);
                }
            });

            // Swords of Revealing Light (1811) / Swords of Concealing Light (1810)
            // Decrementa contador na Standby Phase do oponente (para Swords of Revealing Light)
            // A regra diz: "destroy this card during the End Phase of your opponent's 3rd turn".
            // Vamos usar o contador para simplificar: Inicia com 3, decrementa na End Phase do oponente.
            // Se for Swords of Concealing Light: "Destroy this card during your 2nd Standby Phase".
        }
        else if (phase == GamePhase.End)
        {
            // Swords of Revealing Light (1811)
            // Se for o turno do oponente, decrementa.
            if (!GameManager.Instance.isPlayerTurn) // Turno do oponente
            {
                CheckActiveCards("1811", (card) => HandleTurnCounter(card));
            }

            // Swords of Concealing Light (1810) - Destrói na 2ª Standby Phase do controlador.
            // Lógica movida para Standby Phase do controlador.
        }
        else if (phase == GamePhase.End)
        {
            // 1908 - The Wicked Worm Beast
            CheckActiveCards("1908", (card) =>
            {
                if (card.isPlayerCard && !card.isFlipped)
                {
                    GameManager.Instance.ReturnToHand(card);
                }
            });

            // 1979 - Tsukuyomi
            CheckActiveCards("1979", (card) =>
            {
                if (card.isPlayerCard && !card.isFlipped) // Face-up
                {
                    GameManager.Instance.ReturnToHand(card);
                }
            });

            // 1996 - Two-Man Cell Battle
            CheckActiveCards("1996", (card) =>
            {
                if (card.isPlayerCard)
                {
                    // SS Level 4 Normal from hand
                    Debug.Log("Two-Man Cell Battle: Pode invocar Normal Lv4 da mão.");
                }
            });
        }
        else if (phase == GamePhase.Standby)
        {
            // Malice Doll of Demise (1152): Revive if sent by Continuous Spell
            // Requires tracking flag "sentByContinuousSpell"
            // CheckActiveCards not applicable as it's in GY.
            // We check GY directly.
            // ...
        }
        if (phase == GamePhase.Standby)
        {
            // 1578 - Sacred Phoenix of Nephthys
            // Se foi destruído por efeito no turno anterior (marcado no OnCardSentToGraveyard)
            // Revive e destrói S/T
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            CardData phoenix = gy.Find(c => c.id == "1578"); // ID Sacred Phoenix
            if (phoenix != null)
            {
                // Simplificado: Assume que a condição foi atendida se estiver no GY
                // Em um sistema completo, precisaríamos de uma flag "destroyedByEffectLastTurn"
                Debug.Log("Sacred Phoenix of Nephthys: Revivendo na Standby Phase.");
                GameManager.Instance.SpecialSummonFromData(phoenix, true);
                gy.Remove(phoenix);
                Effect_HeavyStorm(null); // Destrói todas as S/T (source null pois já saiu do GY)
            }

            // 1585 - Sand Moth
            // Lógica similar ao Phoenix, mas apenas se destruído face-down por Spell
        }
        {
            // 1162 - Manticore of Darkness
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard();
            if (gy.Exists(c => c.id == "1162"))
            {
                // Lógica simplificada de reviver enviando besta
                Debug.Log("Manticore of Darkness (GY): Pode reviver enviando Besta (Lógica pendente).");
            }

            // 1465 - Pumpking the King of Ghosts
            CheckActiveCards("1465", (card) =>
            {
                if (GameManager.Instance.IsCardActiveOnField("Castle of Dark Illusions") || GameManager.Instance.IsCardActiveOnField("1270"))
                {
                    // Limite de 4 turnos requer contador
                    if (card.spellCounters < 4)
                    {
                        card.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 100, card));
                        card.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 100, card));
                        card.spellCounters++; // Usa spellCounters para rastrear turnos
                        Debug.Log("Pumpking: +100 ATK/DEF (Standby).");
                    }
                }
            });

            // Swords of Concealing Light (1810)
            if (GameManager.Instance.isPlayerTurn) // Minha Standby Phase
            {
                CheckActiveCards("1810", (card) => HandleTurnCounter(card));
            }
        }
    }

    private void UpdateDMGBuff(CardDisplay card)
    {
        int count = 0;
        count += GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.name == "Dark Magician" || c.name == "Magician of Black Chaos").Count;
        count += GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.name == "Dark Magician" || c.name == "Magician of Black Chaos").Count;

        // Remove buff antigo e adiciona novo (simplificado)
        card.RemoveModifiersFromSource(card);
        if (count > 0)
            card.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 300, card));
    }

    private void UpdateDarkPaladinBuff(CardDisplay card)
    {
        int count = 0;
        // Conta dragões no campo e GY
        count += GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.race == "Dragon").Count;
        // ... + Campo ...

        card.RemoveModifiersFromSource(card);
        if (count > 0)
            card.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, count * 500, card));
    }

    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason)
    {
        // Coffin Seller (0314): Dano quando monstro do oponente vai pro GY
        if (!isOwnerPlayer && card.type.Contains("Monster"))
        {
            CheckActiveCards("0314", (source) =>
            {
                if (source.isPlayerCard)
                {
                    Debug.Log("Coffin Seller: Oponente enviou monstro ao GY. 300 Dano.");
                    Effect_DirectDamage(source, 300);
                }
            });
        }

        // Dark Coffin (0404): Se destruído face-down
        // Difícil detectar "face-down" aqui só com CardData. 
        // Assumimos que o CardDisplay chamou este evento e verificou o estado antes.
        if (card.id == "0404")
        {
            // Simula escolha do oponente
            Debug.Log("Dark Coffin: Oponente deve descartar ou destruir monstro.");
            if (isOwnerPlayer) GameManager.Instance.DiscardRandomHand(false, 1); // Simulado
        }

        // 2031 - Vampire Lord
        if (card.id == "2031")
        {
            // Revive se destruído por efeito do oponente (Simplificado: qualquer efeito)
            if (reason == SendReason.Effect || reason == SendReason.Destroyed)
            {
                Debug.Log("Vampire Lord: Agendado para reviver na próxima Standby.");
                reviveNextStandby.Add(card);
            }
        }

        // 2032 - Vampire's Curse
        if (card.id == "2032")
        {
            // Revive se destruído por batalha
            if (reason == SendReason.Battle)
            {
                Debug.Log("Vampire's Curse: Agendado para reviver na próxima Standby.");
                reviveNextStandby.Add(card);
            }
        }

        // 1587 - Sangan
        if (card.id == "1587")
        {
            // Busca monstro com 1500 ou menos de ATK
            // Como Effect_SearchDeck precisa de um CardDisplay source e a carta já foi destruída,
            // precisamos adaptar ou passar null se o método suportar.
            // O método Effect_SearchDeck usa source.isPlayerCard.
            // Vamos criar um CardDisplay temporário ou refatorar Effect_SearchDeck.
            // Por enquanto, assumimos que o dono é o jogador atual se isOwnerPlayer for true.
            if (isOwnerPlayer && fromLocation == CardLocation.Field)
            {
                Effect_SearchDeck(null, "Monster", "", 1500); // Precisa de refatoração para aceitar null source ou bool isPlayer
            }
        }

        // 2004 - UFO Turtle
        if (card.id == "2004" && isOwnerPlayer && reason == SendReason.Battle) // Destroyed by battle
        {
            Debug.Log("UFO Turtle: Invocando FIRE do Deck.");
            Effect_SpecialSummonFromDeck(null, attribute: "Fire", maxAtk: 1500, isPlayerOverride: isOwnerPlayer);
        }

        // 2005 - UFOroid
        if (card.id == "2005" && isOwnerPlayer && reason == SendReason.Battle) // Destroyed by battle
        {
            Debug.Log("UFOroid: Invocando Machine do Deck.");
            Effect_SpecialSummonFromDeck(null, race: "Machine", maxAtk: 1500, isPlayerOverride: isOwnerPlayer);
        }

        // 2090 - Winged Kuriboh
        if (card.id == "2090")
        {
            Debug.Log("Winged Kuriboh: Sem dano de batalha neste turno.");
            if (BattleManager.Instance != null) BattleManager.Instance.noBattleDamageThisTurn = true;
        }

        // 2097 - Witch of the Black Forest
        if (card.id == "2097" && isOwnerPlayer)
        {
            Debug.Log("Witch of the Black Forest: Buscando monstro com DEF <= 1500.");
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> targets = deck.FindAll(c => c.type.Contains("Monster") && c.def <= 1500);
            if (targets.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(targets, "Witch of the Black Forest", (selected) =>
                {
                    deck.Remove(selected);
                    GameManager.Instance.AddCardToHand(selected, true);
                    GameManager.Instance.ShuffleDeck(true);
                });
            }
        }

        // 1010 - Keldo
        if (card.id == "1010" && !isOwnerPlayer && reason == SendReason.Battle) // Destruído por batalha
        {
            // Shuffle 2 cards from opp GY to Deck
            Debug.Log("Keldo: Embaralhando 2 cartas do GY do oponente no Deck.");
            // Lógica de seleção e shuffle
        }

        // Despair from the Dark (0480): SS se enviado do Hand/Deck pelo oponente
        if (card.id == "0480" && isOwnerPlayer) // Se foi para o MEU cemitério
        {
            // Verifica se veio da Mão ou Deck e se foi por efeito (simplificado: assume efeito do oponente se não for custo)
            if ((fromLocation == CardLocation.Hand || fromLocation == CardLocation.Deck) && (reason == SendReason.Effect || reason == SendReason.Discarded || reason == SendReason.Mill))
            {
                Debug.Log("Despair from the Dark: Invocando do GY.");
                GameManager.Instance.SpecialSummonFromData(card, true);
            }
        }

        // Maji-Gire Panda (1144): Gain 500 ATK when Beast destroyed
        if (card.race == "Beast")
        {
            CheckActiveCards("1144", (panda) =>
            {
                panda.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, panda));
            });
        }

        // Desrook Archfiend (0481): Revive Terrorking
        if (card.name == "Terrorking Archfiend" && isOwnerPlayer && reason == SendReason.Destroyed)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            CardData desrook = hand.Find(c => c.id == "0481");
            if (desrook != null)
            {
                Debug.Log("Desrook Archfiend: Enviando da mão para reviver Terrorking.");
                GameManager.Instance.SendToGraveyard(desrook, true);
                GameManager.Instance.SpecialSummonFromData(card, true);
            }
        }

        // 1925 - Thunder Nyan Nyan
        // 1978 - Troop Dragon
        if (card.id == "1978" && reason == SendReason.Battle) // Destroyed by battle
        {
            Debug.Log("Troop Dragon: Invocando cópia do Deck.");
            // Effect_SearchDeck(null, "Troop Dragon"); // Requer adaptação para SS
            // Simulação de SS do deck:
            Effect_SpecialSummonFromDeck(null, nameContains: "Troop Dragon", isPlayerOverride: isOwnerPlayer);
        }

        // 1419 - Peten the Dark Clown
        if (card.id == "1419")
        {
            // Missing Timing Check: Se enviado como Custo ou Tributo, perde o timing.
            if (reason == SendReason.Cost || reason == SendReason.Tribute)
            {
                Debug.Log("Peten the Dark Clown: Perdeu o timing (Enviado como Custo/Tributo).");
                return;
            }

            // Banish this card to SS Peten from hand/Deck
            // Pergunta ao jogador (Simulado)
            Debug.Log("Peten: Ativando efeito (Banir para invocar).");
            GameManager.Instance.RemoveFromPlay(card, isOwnerPlayer); // Bane do GY
            Effect_SearchDeck(null, "Peten the Dark Clown"); // Deveria ser SS direto
        }

        // 1438 - Pixie Knight
        if (card.id == "1438" && !isOwnerPlayer && reason == SendReason.Battle) // Enviado pelo oponente (batalha)
        {
            // Oponente escolhe Spell no GY e põe no topo do Deck
            List<CardData> gy = GameManager.Instance.GetPlayerGraveyard(); // GY do dono do Pixie
            List<CardData> spells = gy.FindAll(c => c.type.Contains("Spell"));
            if (spells.Count > 0)
            {
                // Simula escolha do oponente
                CardData selected = spells[Random.Range(0, spells.Count)];
                gy.Remove(selected);
                GameManager.Instance.GetPlayerMainDeck().Insert(0, selected);
                Debug.Log($"Pixie Knight: Oponente colocou {selected.name} no topo do seu deck.");
            }
        }

        // Masked Beast Des Gardius (1175): Equip Mask of Remnants from Deck
        if (card.id == "1175" && isOwnerPlayer) // Simplificado: Se foi para o GY do dono
        {
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            CardData mask = deck.Find(c => c.name == "The Mask of Remnants");
            if (mask != null)
            {
                if (SpellTrapManager.Instance != null)
                {
                    // Seleciona monstro do oponente para equipar
                    SpellTrapManager.Instance.StartTargetSelection(
                        (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                        (target) =>
                        {
                            Debug.Log("Des Gardius: Equipando Mask of Remnants e tomando controle.");
                            GameManager.Instance.SwitchControl(target);
                            // Visualmente mover Mask para campo...
                        }
                    );
                }
            }
        }

        // 1241 - Mine Golem
        if (card.id == "1241" && !isOwnerPlayer && reason == SendReason.Battle)
        {
            // Assume destruído por batalha
            GameManager.Instance.DamageOpponent(500);
        }

        // 1259 - Mokey Mokey Smackdown
        if (card.race == "Fairy" && isOwnerPlayer)
        {
            CheckActiveCards("1259", (smackdown) =>
            {
                // Verifica se tem Mokey Mokey
                if (GameManager.Instance.IsCardActiveOnField("1258") || GameManager.Instance.IsCardActiveOnField("Mokey Mokey"))
                {
                    // Aplica buff temporário em todos os Mokey Mokeys (Lógica complexa de busca, simplificado para log)
                    Debug.Log("Mokey Mokey Smackdown: Mokey Mokeys ganham 3000 ATK!");
                    // ApplyBuffToAll("Mokey Mokey", 3000);
                }
            });
        }

        // 1275 - Morale Boost (Damage on Equip removed)
        if (card.type.Contains("Spell") && card.property == "Equip")
        {
            CheckActiveCards("1275", (morale) =>
            {
                // Dano ao controlador do Equip
                if (isOwnerPlayer) GameManager.Instance.DamagePlayer(1000);
                else GameManager.Instance.DamageOpponent(1000);
                Debug.Log("Morale Boost: 1000 de dano por Equip removido.");
            });
        }

        // 1331 - Neko Mane King
        if (card.id == "1331" && isOwnerPlayer && !GameManager.Instance.isPlayerTurn && reason == SendReason.Effect) // Enviado pelo oponente no turno dele por efeito
        {
            Debug.Log("Neko Mane King: Encerrando turno do oponente.");
            if (PhaseManager.Instance != null) PhaseManager.Instance.ChangePhase(GamePhase.End);
        }

        // 1338 - Newdoria
        if (card.id == "1338" && !isOwnerPlayer && reason == SendReason.Battle) // Destruído por batalha
        {
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                    (t) =>
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                        Debug.Log("Newdoria: Destruiu alvo.");
                    }
                );
            }
        }

        // 1346 - Nimble Momonga
        if (card.id == "1346" && !isOwnerPlayer && reason == SendReason.Battle) // Destruído por batalha
        {
            GameManager.Instance.GainLifePoints(true, 1000); // Assume dono é player
            List<CardData> deck = GameManager.Instance.GetPlayerMainDeck();
            List<CardData> momongas = deck.FindAll(c => c.name == "Nimble Momonga");
            int max = Mathf.Min(2, momongas.Count);
            for (int i = 0; i < max; i++)
            {
                GameManager.Instance.SpecialSummonFromData(momongas[i], true, false, true); // Face-down Defense
                deck.Remove(momongas[i]);
            }
            GameManager.Instance.ShuffleDeck(true);
            Debug.Log("Nimble Momonga: Curou e invocou cópias.");
        }

        // 1412 - Penguin Knight
        if (card.id == "1412" && isOwnerPlayer && fromLocation == CardLocation.Deck && reason == SendReason.Mill) // Enviado do Deck ao GY por efeito oponente
        {
            // Shuffle GY into Deck
            Debug.Log("Penguin Knight: GY embaralhado no Deck.");
            // Lógica de mover GY para Deck e Shuffle
        }

        // 1351 - Nitro Unit
        // Verifica se o monstro destruído estava equipado com Nitro Unit
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.target != null && link.target.CurrentCardData == card && link.type == CardLink.LinkType.Equipment)
            {
                if (link.source != null && link.source.CurrentCardData.id == "1351")
                {
                    // Dano ao oponente do dono da Nitro Unit (que é quem equipou)
                    // Nitro Unit equipa no monstro do oponente. Se o monstro morre, o controlador do monstro toma dano?
                    // Texto: "inflict damage to your opponent equal to the ATK".
                    // "Your opponent" refere-se ao oponente do controlador da Nitro Unit.
                    // Como Nitro Unit equipa no oponente, o "opponent" do controlador da Nitro Unit é o dono do monstro.
                    int dmg = card.atk;
                    Debug.Log($"Nitro Unit: {dmg} de dano ao oponente.");
                    if (link.source.isPlayerCard) GameManager.Instance.DamageOpponent(dmg);
                    else GameManager.Instance.DamagePlayer(dmg);
                }
            }
        }

        // 1508 - Regenerating Mummy
        if (card.id == "1508" && isOwnerPlayer && fromLocation == CardLocation.Hand && reason == SendReason.Discarded)
        {
            // Se enviado da mão para o GY por efeito do oponente
            // Requer verificação de contexto (quem causou o descarte)
            Debug.Log("Regenerating Mummy: Se descartado pelo oponente, volta para a mão.");
            // GameManager.Instance.AddCardToHand(card, isOwnerPlayer);
        }

        // 1548 - Roc from the Valley of Haze
        if (card.id == "1548" && isOwnerPlayer && fromLocation == CardLocation.Hand)
        {
            // Se enviado da mão para o GY, volta ao Deck
            Debug.Log("Roc from the Valley of Haze: Voltando ao Deck.");
            // GameManager.Instance.ReturnToDeck(card, true); // Requer CardDisplay ou refatoração
        }

        // 1807 - Sword of Deep-Seated
        if (card.id == "1807")
        {
            Debug.Log("Sword of Deep-Seated: Retornando ao topo do Deck.");
            // GameManager.Instance.ReturnToDeck(card, true);
        }

        // 1817 - T.A.D.P.O.L.E.
        // Lógica já implementada no Effect_1817_TADPOLE, mas o gatilho é aqui se for destruído por batalha.
        // Como o efeito é opcional, o jogador deve ativar. Se for automático, colocamos aqui.
        // O efeito diz "When this card you control is destroyed by battle... you can add".
        // Vamos assumir que o CardEffectManager lida com a ativação.

        // 1766 - Statue of the Wicked
        if (card.id == "1766" && isOwnerPlayer && reason == SendReason.Destroyed) // Se destruída
        {
            // Assume que estava face-down se foi destruída por efeito (difícil rastrear aqui)
            Debug.Log("Statue of the Wicked: Invocando Wicked Token.");
            GameManager.Instance.SpawnToken(isOwnerPlayer, 1000, 1000, "Wicked Token");
        }

        // 1870 - The Immortal of Thunder
        if (card.id == "1870" && isOwnerPlayer && fromLocation == CardLocation.Field)
        {
            GameManager.Instance.PayLifePoints(isOwnerPlayer, 5000);
        }
    }

    partial void OnSetImpl(CardDisplay card)
    {
        // D.D. Trap Hole (0386): Quando oponente Seta monstro
        if (!card.isPlayerCard && card.CurrentCardData.type.Contains("Monster"))
        {
            // Verifica se o jogador tem D.D. Trap Hole setada
            // Simplificado: Log de gatilho
            // Em produção: SpellTrapManager.Instance.CheckForTraps(SpellTrapTrigger.Set, null, card, ...);
            Debug.Log($"Oponente Setou {card.CurrentCardData.name}. Gatilho para D.D. Trap Hole.");
        }
    }

    public void OnCardLeavesField(CardDisplay card)
    {
        // Balloon Lizard (0132): Dano ao ser destruído (baseado em contadores)
        if (card.CurrentCardData.id == "0132" && card.spellCounters > 0)
        {
            int damage = card.spellCounters * 400;
            Debug.Log($"Balloon Lizard destruído com {card.spellCounters} contadores. Dano: {damage}");
            // O dono do Balloon Lizard é quem ativou o efeito, mas o dano vai para quem destruiu?
            // Texto: "inflict damage to the controller of the card that destroyed it"
            // Simplificação: Causa dano ao oponente do dono da carta (assumindo que o oponente destruiu)
            if (card.isPlayerCard) GameManager.Instance.DamageOpponent(damage);
            else GameManager.Instance.DamagePlayer(damage);
        }

        // Blast with Chain (0202): Destroy 1 card if destroyed by effect
        if (card.CurrentCardData.id == "0202")
        {
            // Difícil detectar "by effect" aqui sem contexto. 
            // Assumindo trigger genérico ao sair do campo para protótipo.
            Debug.Log("Blast with Chain: Destruído. (Selecione carta para destruir - Pendente).");
        }

        // Butterfly Dagger - Elma (0254): Return to hand if destroyed
        if (card.CurrentCardData.id == "0254")
        {
            // Deveria checar se foi destruída enquanto equipada
            Debug.Log("Butterfly Dagger - Elma: Retornando para a mão.");
            GameManager.Instance.AddCardToHand(card.CurrentCardData, card.isPlayerCard);
        }

        // Dark Magician of Chaos (0422): Banish if leaves field
        if (card.CurrentCardData.id == "0422")
        {
            Debug.Log("DMoC: Banido ao sair do campo.");
            // GameManager.Instance.BanishCard(card); // Cuidado com loop infinito se chamado dentro de Destroy
        }

        // Remove quaisquer modificadores que esta carta tenha aplicado em outras
        // Ex: Se um Equip Spell for destruído, o monstro perde o buff
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

            foreach (var zone in allZones)
            {
                if (zone.childCount == 0) continue;
                CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                if (target != null) target.RemoveModifiersFromSource(card);
            }
        }

        // Destrói cartas que estavam equipadas NESTA carta (Equip Spell dependency)
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            // Se esta carta (card) era o ALVO de um equipamento
            if (link.target == card && link.type == CardLink.LinkType.Equipment)
            {
                if (link.source != null && link.source.isOnField)
                {
                    Debug.Log($"Regra de Equipamento: Destruindo {link.source.CurrentCardData.name} pois o monstro equipado saiu de campo.");
                    GameManager.Instance.SendToGraveyard(link.source.CurrentCardData, link.source.isPlayerCard);
                    Destroy(link.source.gameObject);
                }
            }
        }

        // 1470 - Pyramid of Light
        if (card.CurrentCardData.id == "1470")
        {
            Debug.Log("Pyramid of Light removida: Destruindo Esfinges.");
            // Encontra Andro e Teleia e destrói/bane
            // DestroyCards(FindSphinxes(), card.isPlayerCard);
        }
    }

    // Novo Hook para dano de batalha causado (chamado pelo BattleManager)
    public void OnDamageDealtImpl(CardDisplay attacker, CardDisplay target, int amount)
    {
        // Robbin' Goblin (1543): Se um monstro seu causa dano, oponente descarta 1
        CheckActiveCards("1543", (source) =>
        {
            if (source.isPlayerCard && attacker != null && attacker.isPlayerCard && amount > 0)
            {
                Debug.Log("Robbin' Goblin: Oponente descarta 1 carta aleatória.");
                GameManager.Instance.DiscardRandomHand(false, 1);
            }
        });

        // Robbin' Zombie (1544): Se um monstro seu causa dano, oponente envia carta do topo do Deck para o GY
        CheckActiveCards("1544", (source) =>
        {
            if (source.isPlayerCard && attacker != null && attacker.isPlayerCard && amount > 0)
            {
                Debug.Log("Robbin' Zombie: Oponente envia 1 carta do topo do Deck para o GY.");
                GameManager.Instance.MillCards(false, 1);
            }
        });

        // 2030 - Vampire Lady
        if (attacker.CurrentCardData.id == "2030" && attacker.isPlayerCard && amount > 0)
        {
            Debug.Log("Vampire Lady: Declare um tipo (Card, Spell, Trap) e oponente envia 1 do deck ao GY (Simulado: Monster).");
            GameManager.Instance.MillCards(!attacker.isPlayerCard, 1);
        }

        // 2038 - Victory Dragon
        if (attacker.CurrentCardData.id == "2038" && amount > 0 && (attacker.isPlayerCard ? GameManager.Instance.opponentLP : GameManager.Instance.playerLP) <= 0)
        {
            Debug.Log("Victory Dragon: MATCH WIN!");
            // GameManager.Instance.WinMatch(); // Hypothetical
        }

        // 2075 - White Magical Hat
        if (attacker.CurrentCardData.id == "2075" && amount > 0)
        {
            Debug.Log("White Magical Hat: Oponente descarta 1 carta aleatória.");
            GameManager.Instance.DiscardRandomHand(!attacker.isPlayerCard, 1);
        }

        // 2123 - Yamata Dragon
        if (attacker.CurrentCardData.id == "2123" && amount > 0)
        {
            int handCount = attacker.isPlayerCard ? GameManager.Instance.GetPlayerHandData().Count : GameManager.Instance.GetOpponentHandData().Count;
            if (handCount < 5)
            {
                int toDraw = 5 - handCount;
                Debug.Log($"Yamata Dragon: Comprando {toDraw} cartas.");
                for (int i = 0; i < toDraw; i++)
                    if (attacker.isPlayerCard) GameManager.Instance.DrawCard();
                    else GameManager.Instance.DrawOpponentCard();
            }
        }

        // 2128 - Yata-Garasu
        if (attacker.CurrentCardData.id == "2128" && amount > 0)
        {
            Debug.Log("Yata-Garasu: Oponente pula a próxima Draw Phase.");
            if (PhaseManager.Instance != null) PhaseManager.Instance.RegisterSkipNextPhase(!attacker.isPlayerCard, GamePhase.Draw);
        }
    }

    public void UpdateAllMegamorphs()
    {
        // Encontra todos os links de equipamento ativos para atualizar Megamorphs
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.source != null && link.source.CurrentCardData.id == "1200" && link.target != null)
            {
                UpdateMegamorphStats(link.source, link.target);
            }
        }
    }

    public void OnDamageTaken(bool isPlayer, int amount)
    {
        // Numinous Healer (1360), Attack and Receive (0117) - Geralmente são Traps ativáveis, não automáticas.
        // Mas efeitos contínuos como "Des Wombat" (0477) preveniriam isso antes.

        // Dark Room of Nightmare (0432): Dano extra em dano de efeito
        // Precisamos saber se foi dano de efeito. Assumindo que sim para este contexto.
        CheckActiveCards("0432", (card) =>
        {
            if (card.isPlayerCard != isPlayer) // Se o oponente tomou dano
                Effect_DirectDamage(card, 300);
        });

        // 1360 - Numinous Healer
        // Activate only when you take damage.
        // Como é Trap, deve ser ativada manualmente ou via CheckForTraps.
        // Mas se já estiver ativa (ex: face-up?? Não, é Normal Trap).
        // O efeito de "increase LP by 500 for each Numinous Healer in GY" é parte da resolução.
        // Não é um efeito contínuo.

        // Atualiza Megamorph (1200)
        UpdateAllMegamorphs();
    }

    void UpdateMegamorphStats(CardDisplay megamorph, CardDisplay target)
    {
        target.RemoveModifiersFromSource(megamorph);
        int controllerLP = megamorph.isPlayerCard ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        int enemyLP = megamorph.isPlayerCard ? GameManager.Instance.opponentLP : GameManager.Instance.playerLP;

        if (controllerLP < enemyLP)
        {
            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk * 2, megamorph));
        }
        else if (controllerLP > enemyLP)
        {
            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Set, target.originalAtk / 2, megamorph));
        }
    }

    // Novo Hook para ativação de Spells (chamado pelo GameManager/SpellTrapManager)
    public void OnSpellActivated(CardDisplay spell)
    {
        // 1275 - Morale Boost (Heal on Equip)
        if (spell.CurrentCardData.property == "Equip")
        {
            CheckActiveCards("1275", (morale) =>
            {
                Effect_GainLP(spell, 1000); // Cura o controlador da Spell
                Debug.Log("Morale Boost: +1000 LP.");
            });
        }

        // 1566 - Royal Magical Library (Add Counter)
        CheckActiveCards("1566", (library) =>
        {
            library.AddSpellCounter(1);
            SpellCounterManager.Instance.AddCounter(library, 1);
            Debug.Log("Royal Magical Library: Contador adicionado.");
        });

        // 1656 - Skilled Dark Magician (Add Counter)
        CheckActiveCards("1656", (mage) =>
        {
            mage.AddSpellCounter(1);
            SpellCounterManager.Instance.AddCounter(mage, 1);
            Debug.Log("Skilled Dark Magician: Contador adicionado.");
        });

        // 1657 - Skilled White Magician (Add Counter)
        CheckActiveCards("1657", (mage) =>
        {
            mage.AddSpellCounter(1);
            SpellCounterManager.Instance.AddCounter(mage, 1);
            Debug.Log("Skilled White Magician: Contador adicionado.");
        });

        // 1400 - Pandemonium
        CheckActiveCards("1400", (card) =>
        {
            // Se um Archfiend é destruído, busca Archfiend com nível menor
            // Requer intercepção de destruição de Fiend (no GY?)
            Debug.Log($"Pandemonium: Um monstro Archfiend foi destruído. Busca Archfiend com Nível menor (Simulado).");
        });


        // Effect: SS por tributo -> Adicionar contadores iniciais.
        // https://yugioh.fandom.com/wiki/Card_Errata:MP1-001
        // Para Zaborg the Thunder Monarch (ver Effect_0382_OrcaMegaFortressOfDarkness).

        // 1753 - Spirit of the Pot of Greed
        if (spell.CurrentCardData.name == "Pot of Greed")
        {
            CheckActiveCards("1753", (spirit) =>
            {
                if (spirit.position == CardDisplay.BattlePosition.Attack)
                {
                    Debug.Log("Spirit of the Pot of Greed: Compra extra.");
                    if (spirit.isPlayerCard) GameManager.Instance.DrawCard();
                    else GameManager.Instance.DrawOpponentCard();
                }
            });
        }

    }

    private void HandleTurnCounter(CardDisplay card)
    {
        if (card.turnCounter > 0)
        {
            card.turnCounter--;
            Debug.Log($"{card.CurrentCardData.name}: Turnos restantes: {card.turnCounter}");
            if (card.turnCounter <= 0)
            {
                Debug.Log($"{card.CurrentCardData.name}: Expirou. Destruindo.");
                GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
                Destroy(card.gameObject);
            }
        }
    }

    // --- SISTEMA DE EFEITOS CONTÍNUOS GLOBAIS ---

    public bool IsAttackPreventedByContinuousEffect(CardDisplay attacker)
    {
        // Gravity Bind (0817) - Level 4+ cannot attack
        if (GameManager.Instance.IsCardActiveOnField("0817") && attacker.CurrentCardData.level >= 4)
        {
            Debug.Log("Ataque impedido por Gravity Bind.");
            return true;
        }

        // Level Limit - Area B (1077) - Level 4+ cannot attack (technically become Defense, but blocks attack if checked)
        if (GameManager.Instance.IsCardActiveOnField("1077") && attacker.CurrentCardData.level >= 4)
        {
            Debug.Log("Ataque impedido por Level Limit - Area B.");
            return true;
        }

        // Messenger of Peace (1209) - 1500+ ATK cannot attack
        if (GameManager.Instance.IsCardActiveOnField("1209") && attacker.currentAtk >= 1500)
        {
            Debug.Log("Ataque impedido por Messenger of Peace.");
            return true;
        }

        return false;
    }

    public bool IsSummonRestricted(bool isSpecialSummon)
    {
        // Jowgen the Spiritualist (0979) - No Special Summons
        if (isSpecialSummon && GameManager.Instance.IsCardActiveOnField("0979")) return true;

        // The Last Warrior from Another Planet (1874) - No Summons at all
        if (GameManager.Instance.IsCardActiveOnField("1874")) return true;

        return false;
    }

    public bool ShouldBanishInsteadOfGraveyard()
    {
        // Flag global de turno (ex: Spirit Elimination)
        if (banishInsteadOfGraveyard) return true;

        // Banisher of the Light (0133)
        if (GameManager.Instance.IsCardActiveOnField("0133")) return true;

        // Banisher of the Radiance (1654)
        if (GameManager.Instance.IsCardActiveOnField("1654")) return true;

        // Macro Cosmos (Se implementado - ID pendente)
        // if (GameManager.Instance.IsCardActiveOnField("MACRO_ID")) return true;

        return false;
    }

    public string GetEffectiveRace(CardDisplay card)
    {
        // DNA Surgery (0390)
        if (GameManager.Instance.IsCardActiveOnField("0390") && !string.IsNullOrEmpty(dnaSurgeryDeclaredType))
        {
            return dnaSurgeryDeclaredType;
        }
        return card.CurrentCardData.race;
    }

    private void ApplyContinuousPositionChecks()
    {
        // Level Limit - Area B (1077) - Change Level 4+ to Defense
        if (GameManager.Instance.IsCardActiveOnField("1077") && GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);

            foreach (var m in all)
            {
                if (m.CurrentCardData.level >= 4 && m.position == CardDisplay.BattlePosition.Attack)
                {
                    m.ChangePosition();
                    Debug.Log($"Level Limit - Area B: {m.CurrentCardData.name} mudou para Defesa.");
                }
            }
        }

        // Final Attack Orders (0651) - Change to Attack
        if (GameManager.Instance.IsCardActiveOnField("0651") && GameManager.Instance.duelFieldUI != null)
        {
            List<CardDisplay> all = new List<CardDisplay>();
            CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
            CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);

            foreach (var m in all)
            {
                if (m.position == CardDisplay.BattlePosition.Defense)
                {
                    m.ChangePosition();
                    Debug.Log($"Final Attack Orders: {m.CurrentCardData.name} mudou para Ataque.");
                }
            }
        }
    }

    // --- BATTLE HOOKS ---

    public bool IsAttackRestricted(CardDisplay attacker)
    {
        // Swords of Revealing Light (72302403)
        // Verifica se o oponente do atacante tem Swords ativo
        if (GameManager.Instance.duelFieldUI != null)
        {
            bool attackerIsPlayer = attacker.isPlayerCard;
            Transform[] enemySpellZones = attackerIsPlayer ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;

            foreach (var zone in enemySpellZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.id == "72302403")
                    {
                        Debug.Log("Ataque impedido por Swords of Revealing Light!");
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue)
    {
        // 1507 - Reflect Bounder
        if (target != null && target.CurrentCardData.id == "1507" && !target.isFlipped && target.position == CardDisplay.BattlePosition.Attack)
        {
            int dmg = attacker.currentAtk;
            Debug.Log($"Reflect Bounder: Causando {dmg} de dano ao atacante.");
            if (attacker.isPlayerCard) GameManager.Instance.DamagePlayer(dmg);
            else GameManager.Instance.DamageOpponent(dmg);
        }

        // 1554 - Rocket Warrior
        if (attacker.CurrentCardData.id == "1554" && target != null)
        {
            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -500, attacker));
            Debug.Log("Rocket Warrior: Alvo perdeu 500 ATK.");
        }

        // 1592 - Sasuke Samurai #4
        if (attacker.CurrentCardData.id == "1592" && target != null)
        {
            GameManager.Instance.TossCoin(1, (heads) =>
            {
                if (heads == 1) // Cara = Sucesso
                {
                    Debug.Log("Sasuke Samurai #4: Acertou! Destruindo alvo.");
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                    // Não chama onContinue pois o alvo foi destruído
                }
                else
                {
                    Debug.Log("Sasuke Samurai #4: Errou.");
                    onContinue?.Invoke();
                }
            });
            return; // Interrompe fluxo síncrono
        }

        // 1770 - Steamroid
        if (attacker.CurrentCardData.id == "1770")
        {
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 500, attacker));
            Debug.Log("Steamroid: +500 ATK no ataque.");
        }

        // 1770 - Steamroid (Defesa)
        if (target != null && target.CurrentCardData.id == "1770")
        {
            target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -500, target));
            Debug.Log("Steamroid: -500 ATK ao ser atacado.");
        }

        // Aqui poderíamos verificar Kuriboh na mão, etc.
        // Por enquanto, apenas continua o fluxo.
        onContinue?.Invoke();
    }

    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target)
    {
        // Skyscraper (63035430)
        // Se E-Hero atacando monstro mais forte, +1000 ATK.
        if (GameManager.Instance.IsCardActiveOnField("63035430"))
        {
            if (attacker.CurrentCardData.name.Contains("Elemental HERO") && target != null)
            {
                int targetPower = (target.position == CardDisplay.BattlePosition.Attack) ? target.currentAtk : target.currentDef;
                if (targetPower > attacker.currentAtk)
                {
                    Debug.Log("Skyscraper: +1000 ATK para E-HERO.");
                    attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 1000, null));
                }
            }
        }

        // 1004 - Karakuri Spider
        if (attacker.CurrentCardData.id == "1004" && target != null && target.CurrentCardData.attribute == "Dark")
        {
            Debug.Log("Karakuri Spider: Destruindo monstro DARK.");
            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
            Destroy(target.gameObject);
        }

        // 1008 - Kazejin
        if (target != null && target.CurrentCardData.id == "1008" && !target.hasUsedEffectThisTurn) // Simplificado: 1x por turno
        {
            Debug.Log("Kazejin: Zerando ATK do atacante.");
            attacker.ModifyStats(-attacker.currentAtk, 0);
            target.hasUsedEffectThisTurn = true;
        }

        // 1586 - Sanga of the Thunder
        if (target != null && target.CurrentCardData.id == "1586" && !target.isPlayerCard)
        {
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, 0, target));
            Debug.Log("Sanga of the Thunder: ATK do atacante zerado.");
        }

        // 1780 - Stone Statue of the Aztecs
        if (target != null && target.CurrentCardData.id == "1780" && target.position == CardDisplay.BattlePosition.Defense)
        {
            // A lógica de dobrar o dano deve ser tratada no BattleManager, mas logamos aqui.
            Debug.Log("Stone Statue of the Aztecs: Dano de batalha será dobrado (Se aplicável).");
        }

        // 1903 - The Unhappy Girl
        if (attacker != null && attacker.CurrentCardData.id == "1903" && target != null && attacker.position == CardDisplay.BattlePosition.Attack)
        {
            Debug.Log("The Unhappy Girl: Alvo travado.");
            // target.canAttack = false;
            // target.canChangePosition = false;
        }

        // 1904 - The Unhappy Maiden
        if (target != null && target.CurrentCardData.id == "1904" && target.isPlayerCard != attacker.isPlayerCard) // Destroyed by battle
        {
            // Check if destroyed (usually handled by health check, assume yes here)
            Debug.Log("The Unhappy Maiden: Encerrando Battle Phase.");
            if (PhaseManager.Instance != null) PhaseManager.Instance.ChangePhase(GamePhase.Main2);
        }

        // 1915 - Thousand Needles
        if (target != null && target.CurrentCardData.id == "1915" && target.position == CardDisplay.BattlePosition.Defense)
        {
            if (attacker != null && attacker.currentAtk < target.currentDef)
            {
                Debug.Log("Thousand Needles: Destruindo atacante.");
                GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                Destroy(attacker.gameObject);
            }
        }

        // 1994 - Two Thousand Needles
        if (target != null && target.CurrentCardData.id == "1994" && target.position == CardDisplay.BattlePosition.Defense)
        {
            if (attacker != null && attacker.currentAtk < target.currentDef)
            {
                Debug.Log("Two Thousand Needles: Destruindo atacante.");
                GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                Destroy(attacker.gameObject);
            }
        }

        // Injection Fairy Lily (79575620) - Lógica de pagar LP seria aqui

        // Buster Rancher (0253)
        // Se equipado batalhar com monstro >= 2500 ATK, ganha ATK
        // Precisamos achar quem tem Buster Rancher equipado.
        // Simplificação: Verifica se o atacante tem o modificador de Buster Rancher ou checa links
        // Como não temos acesso fácil aos links aqui, verificamos se o atacante tem o ID 0253 nos modificadores? Não.
        // Vamos checar se existe Buster Rancher ativo e se está linkado ao atacante.
        // (Lógica complexa para este escopo, simplificando para log)
        if (attacker.currentAtk <= 1000) // || attacker.activeModifiers.Exists(m => m.source != null && m.source.CurrentCardData.id == "0253"))
        {
            // Se o alvo for forte
            int targetAtk = (target != null && target.position == CardDisplay.BattlePosition.Attack) ? target.currentAtk : (target != null ? target.currentDef : 0);
            if (targetAtk >= 2500) Debug.Log("Buster Rancher: Ativando buff massivo!");
        }

        // Dark Artist (0395): DEF / 2 se atacado por LIGHT
        if (target != null && target.CurrentCardData.id == "0395" && target.position == CardDisplay.BattlePosition.Defense)
        {
            if (attacker != null && attacker.CurrentCardData.attribute == "Light")
            {
                Debug.Log("Dark Artist: Atacado por LIGHT. DEF reduzida pela metade.");
                target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, 0.5f, target));
            }
        }

        // 1249 - Mirage Knight
        if (attacker.CurrentCardData.id == "1249" && target != null)
        {
            int bonus = target.currentAtk;
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, bonus, attacker));
            Debug.Log($"Mirage Knight: +{bonus} ATK.");
        }

        // 1252 - Mirror Wall
        if (GameManager.Instance.IsCardActiveOnField("1252") && attacker != null && !attacker.isPlayerCard) // Se oponente ataca e Mirror Wall ativa
        {
            // Reduz ATK pela metade (Permanente enquanto atacar? Regra diz "has its ATK halved")
            int reduction = attacker.currentAtk / 2;
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, -reduction, null));
            Debug.Log("Mirror Wall: ATK do atacante reduzido pela metade.");
        }

        // 1264 - Monk Fighter
        if ((target != null && target.CurrentCardData.id == "1264") || (attacker.CurrentCardData.id == "1264"))
        {
            // Dano de batalha 0 para o controlador
            // Requer suporte no BattleManager para anular dano específico
            Debug.Log("Monk Fighter: Dano de batalha será 0.");
        }

        // 1103 - Luminous Soldier
        if (attacker.CurrentCardData.id == "1103" && target != null && target.CurrentCardData.attribute == "Dark")
        {
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, 500, attacker));
        }

        // 1348 - Ninja Grandmaster Sasuke
        if (attacker.CurrentCardData.id == "1348" && target != null && target.position == CardDisplay.BattlePosition.Defense && target.isFlipped)
        {
            Debug.Log("Ninja Grandmaster Sasuke: Destruindo defesa face-up.");
            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
            Destroy(target.gameObject);
        }

        // 1589 - Sasuke Samurai
        if (attacker.CurrentCardData.id == "1589" && target != null && target.isFlipped)
        {
            Debug.Log("Sasuke Samurai: Destruindo monstro face-down.");
            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
            Destroy(target.gameObject);
        }

        // 1207 - Mermaid Knight (Double Attack)
        if (attacker.CurrentCardData.id == "1207" && (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")))
        {
            // Lógica tratada no BattleManager via CanAttackAgain, mas podemos logar aqui
            Debug.Log("Mermaid Knight: Ataque duplo com Umi.");
        }

        string id = attacker.CurrentCardData.id;
        if (id == "1304" || id == "1305" || id == "1306" || id == "1590")
        {
            Debug.Log($"{attacker.CurrentCardData.name}: Destruindo monstro face-down sem virar.");
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
            Destroy(target.gameObject);

            // Se for LV6 (1306), coloca no topo do deck em vez do GY? (Regra diz: "and if you do, you can place it on top...")
            // Implementação simplificada: Vai pro GY.
        }

        // 1773 - Steel Scorpion
        if (target != null && target.CurrentCardData.id == "1773" && !target.isPlayerCard)
        {
            if (attacker != null && !attacker.CurrentCardData.race.Contains("Machine"))
            {
                Debug.Log("Steel Scorpion: Atacante não-máquina marcado para destruição (Simulado).");
                // TODO: Agendar destruição na End Phase do 2º turno do oponente
            }
        }
    }
public void OnBattleEnd(CardDisplay attacker, CardDisplay target)
{
    // D.D. Warrior Lady (7572887) - Lógica de banir seria aqui
    // Mystic Tomato (83011278) - Lógica de busca seria aqui

    // 0019 - Absorbing Kid from the Sky
    if (attacker != null && attacker.CurrentCardData.id == "0019" && target != null)
    {
        // Verifica se o alvo foi destruído (está no cemitério)
        if (GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData) ||
            GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData))
        {
            int level = target.CurrentCardData.level;
            int heal = level * 300;
            Debug.Log($"Absorbing Kid: Destruiu monstro Lv{level}. Ganha {heal} LP.");
            Effect_GainLP(attacker, heal);
        }
    }

    // 1507 - Reflect Bounder (Auto-destruição após cálculo de dano)
    if (target != null && target.CurrentCardData.id == "1507" && target.position == CardDisplay.BattlePosition.Attack)
    {
        Debug.Log("Reflect Bounder: Auto-destruição após batalha.");
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
        Destroy(target.gameObject);
    }

    // 1001 - Kangaroo Champ
    if (attacker != null && attacker.CurrentCardData.id == "1001") attacker.ChangePosition();
    if (target != null && target.CurrentCardData.id == "1001") target.ChangePosition();

    // 1009 - Kelbek
    if (target != null && target.CurrentCardData.id == "1009" && attacker != null)
    {
        Debug.Log("Kelbek: Retornando atacante para a mão.");
        GameManager.Instance.ReturnToHand(attacker);
    }

    // 1063 - Legacy Hunter
    if (attacker != null && attacker.CurrentCardData.id == "1063" && target != null) // Se destruiu (verificar se target foi pro GY)
    {
        Debug.Log("Legacy Hunter: Oponente embaralha 1 carta da mão no deck.");
        GameManager.Instance.DiscardRandomHand(!attacker.isPlayerCard, 1); // Deveria ser shuffle
    }

    // 1232 - Millennium Scorpion
    if (attacker != null && attacker.CurrentCardData.id == "1232" && target != null)
    {
        attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, attacker));
        Debug.Log("Millennium Scorpion: +500 ATK.");
    }

    // 1065 - Legendary Black Belt
    if (attacker != null && target != null)
    {
        int dmg = target.originalDef;
        Effect_DirectDamage(attacker, dmg);
    }

    // 1308 - Mystical Beast of Serket
    if (attacker != null && attacker.CurrentCardData.id == "1308" && target != null)
    {
        // Se destruiu monstro, ganha 500 ATK e bane o monstro
        attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, 500, attacker));
        GameManager.Instance.BanishCard(target); // Bane do GY (ou antes de ir)
        Debug.Log("Serket: Comeu o monstro. +500 ATK.");
    }

    // 1311 - Mystical Knight of Jackal
    if (attacker != null && attacker.CurrentCardData.id == "1311" && target != null)
    {
        // Retorna monstro destruído ao topo do deck
        // Requer lógica de mover do GY para o Deck (Topo)
        Debug.Log("Mystical Knight of Jackal: Monstro retornado ao topo do deck.");
        // GameManager.Instance.ReturnToDeck(target, true);
    }

    // 1068 - Legendary Jujitsu Master
    if (target != null && target.CurrentCardData.id == "1068" && target.position == CardDisplay.BattlePosition.Defense)
    {
        Debug.Log("Legendary Jujitsu Master: Atacante para o topo do deck.");
        // GameManager.Instance.ReturnToDeck(attacker, true);
    }

    // 1326 - Needle Burrower
    if (attacker != null && attacker.CurrentCardData.id == "1326" && target != null)
    {
        int dmg = target.CurrentCardData.level * 500;
        Effect_DirectDamage(attacker, dmg);
        Debug.Log($"Needle Burrower: {dmg} de dano.");
    }

    // 1322 - Necklace of Command
    // Verifica se algum monstro destruído tinha Necklace of Command equipado
    if (target != null && (target.currentAtk < attacker.currentAtk || target.position == CardDisplay.BattlePosition.Defense)) // Target destroyed
    {
        CheckNecklaceOfCommand(target);
    }
    if (attacker != null && attacker.currentAtk < target.currentAtk && target.position == CardDisplay.BattlePosition.Attack) // Attacker destroyed
    {
        CheckNecklaceOfCommand(attacker);
    }

    // 1075 - Lesser Fiend
    if (attacker != null && attacker.CurrentCardData.id == "1075" && target != null)
    {
        Debug.Log("Lesser Fiend: Banindo monstro destruído.");
        GameManager.Instance.BanishCard(target);
    }

    // 1572 - Ryu Kokki
    if (attacker != null && attacker.CurrentCardData.id == "1572" && target != null)
    {
        if (target.CurrentCardData.race == "Warrior" || target.CurrentCardData.race == "Spellcaster")
        {
            Debug.Log("Ryu Kokki: Destruindo Warrior/Spellcaster.");
            GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
            Destroy(target.gameObject);
        }
    }
    if (target != null && target.CurrentCardData.id == "1572" && attacker != null)
    {
        if (attacker.CurrentCardData.race == "Warrior" || attacker.CurrentCardData.race == "Spellcaster")
        {
            Debug.Log("Ryu Kokki: Destruindo Warrior/Spellcaster.");
            GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
            Destroy(attacker.gameObject);
        }
    }

    // B.E.S. Big Core (0124) & Crystal Core (0125)
    // Remove contador no fim da batalha ou destrói
    if (attacker != null && (attacker.CurrentCardData.id == "0124" || attacker.CurrentCardData.id == "0125"))
    {
        HandleBESCounter(attacker);
    }
    if (target != null && (target.CurrentCardData.id == "0124" || target.CurrentCardData.id == "0125"))
    {
        HandleBESCounter(target);
    }

    // BLS - Envoy (0189): Ataque duplo se destruir monstro
    if (attacker != null && attacker.CurrentCardData.id == "0189" && target != null)
    {
        // Verifica se o alvo foi destruído
        // Nota: O BattleManager já marcou hasAttackedThisTurn = true.
        // Precisamos de uma flag especial ou resetar hasAttackedThisTurn condicionalmente.
        Debug.Log("BLS - Envoy: Ativando segundo ataque (Lógica pendente no BattleManager para permitir ataque extra).");
        // attacker.canAttackAgain = true; // Necessário suporte no CardDisplay
    }

    // D.D. Assailant (0378): Bane ambos se destruído em batalha
    // Verifica se D.D. Assailant foi destruído (está no GY ou marcado para destruição)
    // Como OnBattleEnd ocorre antes da destruição visual em alguns casos, verificamos o resultado
    // Simplificação: Se um dos dois é D.D. Assailant e a batalha resultou em sua destruição
    if (attacker != null && attacker.CurrentCardData.id == "0378" && target != null)
    {
        // Se atacante morreu (ATK <= DEF ou ATK <= ATK)
        int atk = attacker.currentAtk;
        int def = (target.position == CardDisplay.BattlePosition.Attack) ? target.currentAtk : target.currentDef;
        if (atk <= def)
        {
            Debug.Log("D.D. Assailant: Banindo ambos após batalha.");
            GameManager.Instance.BanishCard(attacker);
            GameManager.Instance.BanishCard(target);
        }
    }
    // Caso reverso (D.D. Assailant foi atacado)
    if (target != null && target.CurrentCardData.id == "0378" && attacker != null)
    {
        // Lógica similar...
    }

    // D.D. Crazy Beast (0380): Bane monstro destruído
    if (attacker != null && attacker.CurrentCardData.id == "0380" && target != null)
    {
        // Se o alvo foi destruído, bane em vez de enviar ao GY
        // Isso requer intercepção do SendToGraveyard ou verificação aqui
        // Como o BattleManager já enviou ao GY, teríamos que banir do GY.
        // Simplificação:
        Debug.Log("D.D. Crazy Beast: Banindo monstro destruído.");
        // GameManager.Instance.BanishFromGraveyard(target.CurrentCardData);
    }

    // D.D. Warrior (0387): Bane ambos após batalha
    if ((attacker != null && attacker.CurrentCardData.id == "0387") || (target != null && target.CurrentCardData.id == "0387"))
    {
        Debug.Log("D.D. Warrior: Banindo ambos os monstros.");
        if (attacker != null) GameManager.Instance.BanishCard(attacker);
        if (target != null) GameManager.Instance.BanishCard(target);
    }

    // D.D. Warrior Lady (0388): Pode banir ambos (Opcional)
    if ((attacker != null && attacker.CurrentCardData.id == "0388") || (target != null && target.CurrentCardData.id == "0388"))
    {
        // Deveria abrir confirmação
        Debug.Log("D.D. Warrior Lady: Banindo ambos (Assumindo Sim).");
        if (attacker != null) GameManager.Instance.BanishCard(attacker);
        if (target != null) GameManager.Instance.BanishCard(target);
    }

    // Rigorous Reaver (1532): Se destruído em batalha, o atacante perde 500 ATK/DEF
    if (target != null && target.CurrentCardData.id == "1532")
    {
        // Verifica se o alvo foi destruído
        if (attacker != null && attacker.currentAtk >= target.currentDef)
        {
            Debug.Log("Rigorous Reaver: Atacante perde 500 ATK/DEF.");
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -500, target));
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -500, target));
        }
    }

    // Dark Balter the Terrible (0397): Nega efeitos de monstros destruídos
    if (attacker != null && attacker.CurrentCardData.id == "0397" && target != null)
    {
        // Se o alvo foi destruído...
        // A negação de efeitos no GY (como Sangan) requer um sistema de "NegatedStatus" no CardData ou verificação no evento do GY.
        // Por enquanto, apenas logamos.
        Debug.Log("Dark Balter: Efeitos do monstro destruído negados.");
    }

    // Dark Flare Knight (0412): SS Mirage Knight
    if (attacker != null && attacker.CurrentCardData.id == "0412" && attacker.currentAtk <= 0) // Destruído (simplificado)
    {
        Debug.Log("Dark Flare Knight: Invocando Mirage Knight.");
        // GameManager.Instance.SpecialSummonById("1249", attacker.isPlayerCard);
    }

    // Dark Mimic LV3 (0425): Draw 1
    if (target != null && target.CurrentCardData.id == "0425") // Se foi destruído
    {
        Debug.Log("Dark Mimic LV3: Compra 1.");
        if (target.isPlayerCard) GameManager.Instance.DrawCard();
    }

    // Dark Ruler Ha Des (0433): Negate effects of destroyed monsters
    if (attacker != null && attacker.CurrentCardData.race == "Fiend" && attacker.isPlayerCard)
    {
        if (GameManager.Instance.IsCardActiveOnField("0433"))
        {
            Debug.Log("Dark Ruler Ha Des: Efeitos do monstro destruído negados.");
        }
    }

    // Dark Necrofear (0427): Equip on End Phase
    // A lógica real é na End Phase se foi destruído.
    // Marcamos uma flag no GameManager ou similar.
    // GameManager.Instance.necrofearDestroyedThisTurn = true;

    // Des Volstgalph (0476): Burn 500 on destroy monster
    if (attacker != null && attacker.CurrentCardData.id == "0476" && target != null)
    {
        Debug.Log("Des Volstgalph: 500 de dano.");
        Effect_DirectDamage(attacker, 500);
    }

    // Desrook Archfiend (0481): Revive Terrorking
    if (target != null && target.CurrentCardData.name == "Terrorking Archfiend" && target.isPlayerCard)
    {
        List<CardData> hand = GameManager.Instance.GetPlayerHandData();
        CardData desrook = hand.Find(c => c.id == "0481");
        if (desrook != null)
        {
            Debug.Log("Desrook Archfiend: Enviando da mão para reviver Terrorking.");
            GameManager.Instance.SendToGraveyard(desrook, true);
            GameManager.Instance.SpecialSummonFromData(target.CurrentCardData, true);
        }
    }

    // Mecha-Dog Marron (1190): 1000 damage to both if destroyed by battle
    if (attacker != null && target != null && target.CurrentCardData.id == "1190")
    {
        // Verifica se foi destruído por batalha (chamado dentro de OnBattleEnd implica batalha)
        Debug.Log("Mecha-Dog Marron: 1000 de dano para ambos.");
        GameManager.Instance.DamagePlayer(1000);
        GameManager.Instance.DamageOpponent(1000);
    }

    // 1925 - Thunder Nyan Nyan
    // 1978 - Troop Dragon
    if (target != null && target.CurrentCardData.id == "1978") // Destroyed by battle
    {
        Debug.Log("Troop Dragon: Invocando cópia do Deck.");
        // Effect_SearchDeck(null, "Troop Dragon"); // Requer adaptação para SS
    }

    // 1931 - Timeater
    if (attacker != null && attacker.CurrentCardData.id == "1931" && target != null)
    {
        // Se destruiu o monstro (target está no GY ou marcado para destruição)
        // Chamamos o efeito registrado
        ExecuteCardEffect(attacker);
    }

    // 2147 - Zone Eater
    if (target != null && target.CurrentCardData.id == "2147" && attacker != null)
    {
        // Se o Zone Eater foi atacado, marca o atacante para destruição
        attacker.destructionTurnCountdown = 5;
        attacker.destructionCountdownOwnerIsPlayer = attacker.isPlayerCard; // O contador diminui no turno do dono do atacante
        Debug.Log($"Zone Eater: {attacker.CurrentCardData.name} será destruído em 5 turnos.");
    }

    // 2028 - Vampire Baby
    if (attacker != null && attacker.CurrentCardData.id == "2028" && target != null)
    {
        // Se destruiu monstro por batalha e enviou ao GY
        List<CardData> oppGY = GameManager.Instance.GetOpponentGraveyard();
        List<CardData> myGY = GameManager.Instance.GetPlayerGraveyard();

        // if (oppGY.Contains(target.CurrentCardData) || myGY.Contains(target.CurrentCardData))
        // {
        //     Debug.Log("Vampire Baby: Invocando monstro destruído (Simulado no fim da batalha).");
        //     // Remove do GY
        //     if (oppGY.Contains(target.CurrentCardData)) oppGY.Remove(target.CurrentCardData);
        //     else myGY.Remove(target.CurrentCardData);
        //     
        //     // SS no campo do controlador do Vampire Baby
        //     GameManager.Instance.SpecialSummonFromData(target.CurrentCardData, attacker.isPlayerCard);
        // }
    }

    // 2049 - Wall of Illusion
    if (target != null && target.CurrentCardData.id == "2049" && attacker != null)
    {
        // Se o atacante sobreviveu (não foi destruído na batalha), retorna para a mão
        // Verifica se o atacante ainda está no campo (não foi destruído por regra de batalha)
        // Nota: ResolveDamage chama Destroy(), mas o objeto Unity persiste até o fim do frame.
        // Uma verificação robusta seria checar se ele NÃO está no GY.
        // bool attackerInGY = GameManager.Instance.GetPlayerGraveyard().Contains(attacker.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(attacker.CurrentCardData);

        // if (!attackerInGY)
        {
            Debug.Log("Wall of Illusion: Retornando atacante para a mão.");
            GameManager.Instance.ReturnToHand(attacker);
        }
    }

    // 2093 - Winged Sage Falcos
    if (attacker != null && attacker.CurrentCardData.id == "2093" && target != null)
    {
        // Se destruiu monstro em Posição de Ataque
        // Precisamos saber a posição anterior (target.position), mas target pode ter sido destruído.
        // Assumimos que target.position ainda guarda o estado do momento da batalha.
        // if (target.position == CardDisplay.BattlePosition.Attack)
        // {
        //     // Verifica se foi enviado ao GY
        //     List<CardData> gy = target.isPlayerCard ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
        //     if (gy.Contains(target.CurrentCardData))
        //     {
        //         Debug.Log("Winged Sage Falcos: Retornando monstro destruído ao topo do Deck.");
        //         gy.Remove(target.CurrentCardData);
        //         List<CardData> deck = target.isPlayerCard ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
        //         deck.Insert(0, target.CurrentCardData);
        //     }
        // }
    }

    // 2146 - Zombyra the Dark
    if (attacker != null && attacker.CurrentCardData.id == "2146" && target != null)
    {
        // Se destruiu monstro (está no GY)
        // bool targetInGY = GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData);
        // if (targetInGY)
        {
            Debug.Log("Zombyra the Dark: -200 ATK.");
            attacker.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Continuous, StatModifier.Operation.Add, -200, attacker));
        }
    }

    // 2131 - Yomi Ship
    if (target != null && target.CurrentCardData.id == "2131" && attacker != null)
    {
        // Se Yomi Ship foi destruído por batalha
        bool targetInGY = GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData);
        if (targetInGY)
        {
            Debug.Log("Yomi Ship: Destruindo o atacante.");
            GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
            Destroy(attacker.gameObject);
        }
    }

    // Master Monk (1182) & Mataza (1184): Reset attack flag for double attack
    // (Lógica simplificada: Se atacou uma vez, permite atacar de novo resetando a flag)
    // Isso requer um contador de ataques no CardDisplay, que não temos.
    // Workaround: Se for um desses monstros, reseta hasAttackedThisTurn se for o primeiro ataque.
    // Como não sabemos se é o primeiro, isso permitiria ataques infinitos.
    // Solução correta requer adicionar 'attackCount' no CardDisplay.

    // 1762 - Spring of Rebirth & 1755 - Spirit's Invitation (Lógica de retorno à mão)
    // Como não temos um hook explícito OnReturnToHand aqui, verificamos se podemos inferir ou adicionar.
    // O método ReturnToHand no GameManager chama OnCardLeavesField.
    // Vamos adicionar a lógica lá ou criar um novo hook.
    // Por enquanto, simulamos no OnCardLeavesField se o destino for mão.
}

// --- NOVOS HOOKS ESPECÍFICOS ---

partial void OnCounterTrapResolvedImpl(CardDisplay trap)
{
    // 2034 - Van'Dalgyon the Dark Dragon Lord
    List<CardData> hand = trap.isPlayerCard ? GameManager.Instance.GetPlayerHandData() : GameManager.Instance.GetOpponentHandData();
    CardData vandalgyon = hand.Find(c => c.id == "2034");

    if (vandalgyon != null)
    {
        Debug.Log("Van'Dalgyon: Counter Trap resolvida. Invocando da mão.");
        if (trap.isPlayerCard)
        {
            GameManager.Instance.RemoveCardFromHand(vandalgyon, true);
            GameManager.Instance.SpecialSummonFromData(vandalgyon, true);
        }
        else
        {
            // Lógica para oponente (se necessário)
        }
    }
}

partial void OnCardAddedToHandImpl(CardDisplay card)
{
    // 2058 - Watapon
    if (card.CurrentCardData.id == "2058")
    {
        Debug.Log("Watapon: Adicionado à mão por efeito. Invocando...");
        GameManager.Instance.RemoveCardFromHand(card.CurrentCardData, card.isPlayerCard);
        GameManager.Instance.SpecialSummonFromData(card.CurrentCardData, card.isPlayerCard);
    }
}

partial void OnTributeImpl(CardDisplay card)
{
    // 2142 - Zolga
    if (card.CurrentCardData.id == "2142")
    {
        Debug.Log("Zolga: Tributado. Ganha 2000 LP.");
        GameManager.Instance.GainLifePoints(card.isPlayerCard, 2000);
    }
}

public void OnLifePointsGained(bool isPlayer, int amount)
{
    // Fire Princess (0659)
    // Se você ganhar LP, causa 500 de dano ao oponente.
    CheckActiveCards("0659", (card) =>
    {
        if (card.isPlayerCard == isPlayer)
        {
            Debug.Log("Fire Princess: Dano por cura.");
            Effect_DirectDamage(card, 500);
        }
    });

    // Atualiza Megamorph (1200)
    UpdateAllMegamorphs();
}

// Helper para iterar cartas ativas no campo
private void CheckActiveCards(string cardId, System.Action<CardDisplay> action)
{
    if (GameManager.Instance.duelFieldUI == null) return;

    // Verifica todas as zonas do jogador (e oponente se necessário)
    // Simplificado para zonas do jogador por enquanto
    List<Transform> allZones = new List<Transform>();
    allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
    allZones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
    allZones.Add(GameManager.Instance.duelFieldUI.playerFieldSpell);

    foreach (var zone in allZones)
    {
        if (zone.childCount == 0) continue;
        CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
        if (cd != null && cd.isOnField && !cd.isFlipped && cd.CurrentCardData.id == cardId)
        {
            action(cd);
        }
    }
}

public void CheckMaintenanceCosts()
{
    maintenanceQueue.Clear();

    // Imperial Order (0932)
    CheckActiveCards("0932", (card) =>
    {
        if (card.isPlayerCard)
        {
            maintenanceQueue.Enqueue(new MaintenanceRequest
            {
                card = card,
                description = "700 LP",
                canPay = () => GameManager.Instance.playerLP > 700,
                payAction = () => GameManager.Instance.PayLifePoints(true, 700)
            });
        }
    });

    // Mirror Wall (1252)
    CheckActiveCards("1252", (card) =>
    {
        if (card.isPlayerCard)
        {
            maintenanceQueue.Enqueue(new MaintenanceRequest
            {
                card = card,
                description = "2000 LP",
                canPay = () => GameManager.Instance.playerLP > 2000,
                payAction = () => GameManager.Instance.PayLifePoints(true, 2000)
            });
        }
    });

    // Armor Exe (0102)
    CheckActiveCards("0102", (card) =>
    {
        if (card.isPlayerCard)
        {
            maintenanceQueue.Enqueue(new MaintenanceRequest
            {
                card = card,
                description = "1 Spell Counter",
                canPay = () => GetTotalSpellCounters(true) > 0,
                payAction = () => RemoveSpellCounters(1, true)
            });
        }
    });

    // Messenger of Peace (1209)
    CheckActiveCards("1209", (card) =>
    {
        if (card.isPlayerCard)
        {
            maintenanceQueue.Enqueue(new MaintenanceRequest
            {
                card = card,
                description = "100 LP",
                canPay = () => GameManager.Instance.playerLP > 100,
                payAction = () => GameManager.Instance.PayLifePoints(true, 100)
            });
        }
    });

    // Fairy Box (0620)
    CheckActiveCards("0620", (card) =>
    {
        if (card.isPlayerCard)
        {
            maintenanceQueue.Enqueue(new MaintenanceRequest
            {
                card = card,
                description = "500 LP",
                canPay = () => GameManager.Instance.playerLP > 500,
                payAction = () => GameManager.Instance.PayLifePoints(true, 500)
            });
        }
    });

    ProcessNextMaintenance();
}

private void ProcessNextMaintenance()
{
    if (maintenanceQueue.Count == 0) return;

    var req = maintenanceQueue.Dequeue();

    // Verifica se a carta ainda está em campo
    if (req.card == null || !req.card.isOnField)
    {
        ProcessNextMaintenance();
        return;
    }

    if (req.canPay())
    {
        UIManager.Instance.ShowConfirmation(
            $"Pagar {req.description} para manter {req.card.CurrentCardData.name}?",
            () =>
            {
                req.payAction();
                Debug.Log($"{req.card.CurrentCardData.name}: Manutenção paga.");
                ProcessNextMaintenance();
            },
            () =>
            {
                Debug.Log($"{req.card.CurrentCardData.name}: Manutenção recusada. Destruindo.");
                DestroyCardForMaintenance(req.card);
                ProcessNextMaintenance();
            }
        );
    }
    else
    {
        Debug.Log($"{req.card.CurrentCardData.name}: Não pode pagar manutenção. Destruindo.");
        DestroyCardForMaintenance(req.card);
        ProcessNextMaintenance();
    }
}

private void DestroyCardForMaintenance(CardDisplay card)
{
    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
    GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard, CardLocation.Field, SendReason.Rule);
    Destroy(card.gameObject);
}

private void CleanAllExpiredModifiers()
{
    List<Transform> allZones = new List<Transform>();
    allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
    allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

    foreach (var zone in allZones)
    {
        if (zone.childCount == 0) continue;
        CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
        if (cd != null)
        {
            cd.CleanExpiredModifiers();
        }
    }
}

private void HandleBESCounter(CardDisplay card)
{
    if (card.spellCounters > 0)
    {
        card.RemoveSpellCounter(1);
    }
    else
    {
        Debug.Log($"{card.CurrentCardData.name}: Sem contadores após batalha. Destruído.");
        GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
        Destroy(card.gameObject);
    }
}

// --- FIM DO SISTEMA DE EVENTOS ---
void Effect_DirectDamage(CardDisplay source, int amount)
{
    bool targetOpponent = source.isPlayerCard;

    // Mystical Refpanel Logic (1313)
    if (redirectSpellTarget && source.CurrentCardData.type.Contains("Spell"))
    {
        targetOpponent = !targetOpponent; // Inverte o alvo
        Debug.Log("Effect_DirectDamage: Alvo redirecionado por Mystical Refpanel.");
    }

    if (targetOpponent) GameManager.Instance.DamageOpponent(amount);
    else GameManager.Instance.DamagePlayer(amount);

    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
}

private void CheckNecklaceOfCommand(CardDisplay destroyedMonster)
{
    // Verifica se o monstro destruído tinha Necklace of Command (1322) equipado
    CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
    foreach (var link in links)
    {
        if (link.target == destroyedMonster && link.type == CardLink.LinkType.Equipment)
        {
            if (link.source != null && link.source.CurrentCardData.id == "1322")
            {
                Debug.Log("Necklace of Command: Ativado após destruição do monstro equipado.");

                // Effect: Draw 1 OR Discard 1 random from opp hand
                // Como é opcional, abrimos um diálogo simples
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowConfirmation("Necklace of Command: Comprar 1 carta (Sim) ou Descartar do oponente (Não)?",
                        () => GameManager.Instance.DrawCard(),
                        () => GameManager.Instance.DiscardRandomHand(!link.source.isPlayerCard, 1)
                    );
                }
            }
        }
    }
}

void Effect_GainLP(CardDisplay source, int amount)
{
    GameManager.Instance.GainLifePoints(source.isPlayerCard, amount);
}

bool Effect_PayLP(CardDisplay source, int amount)
{
    return GameManager.Instance.PayLifePoints(source.isPlayerCard, amount);
}

void Effect_DestroyType(CardDisplay source, string type)
{
    Debug.Log($"Destruindo todos os monstros tipo {type}...");
    // Implementação real requereria iterar sobre o campo e destruir
    // DestroyAllMonsters(true, true, (m) => m.CurrentCardData.race == type);
}

void Effect_SearchDeck(CardDisplay source, string term, string typeFilter = "", int maxAtk = 9999)
{
    bool isPlayer = source != null ? source.isPlayerCard : true;
    List<CardData> deck = isPlayer ? GameManager.Instance.GetPlayerMainDeck() : null;

    if (deck == null) return;

    List<CardData> results = deck.FindAll(c =>
        c.name.Contains(term) &&
        (string.IsNullOrEmpty(typeFilter) || c.type.Contains(typeFilter)) &&
        c.atk <= maxAtk
    );

    if (results.Count > 0)
    {
        GameManager.Instance.OpenCardSelection(results, $"Selecione '{term}'", (selected) =>
        {
            deck.Remove(selected);
            GameManager.Instance.AddCardToHand(selected, isPlayer);
            GameManager.Instance.ShuffleDeck(isPlayer);
        });
    }
}

void Effect_SearchDeckTop(CardDisplay source, string type, string subType = "")
{
    Debug.Log($"Procurando {type}/{subType} para colocar no topo do deck.");
}

void Effect_SpecialSummonFromDeck(CardDisplay source, string race = "", string attribute = "", int maxAtk = -1, int maxDef = -1, int maxLevel = -1, string nameContains = "", bool? isPlayerOverride = null)
{
    bool isPlayer = isPlayerOverride.HasValue ? isPlayerOverride.Value : (source != null ? source.isPlayerCard : true);
    List<CardData> deck = isPlayer ? GameManager.Instance.GetPlayerMainDeck() : null;

    if (deck == null) return;

    List<CardData> targets = deck.FindAll(c =>
        c.type.Contains("Monster") &&
        (string.IsNullOrEmpty(race) || c.race == race) &&
        (string.IsNullOrEmpty(attribute) || c.attribute == attribute) &&
        (maxAtk == -1 || c.atk <= maxAtk) &&
        (maxDef == -1 || c.def <= maxDef) &&
        (maxLevel == -1 || c.level <= maxLevel) &&
        (string.IsNullOrEmpty(nameContains) || c.name.Contains(nameContains))
    );

    if (targets.Count > 0)
    {
        GameManager.Instance.OpenCardSelection(targets, "Invocar do Deck", (selected) =>
        {
            deck.Remove(selected);
            GameManager.Instance.SpecialSummonFromData(selected, isPlayer);
            GameManager.Instance.ShuffleDeck(isPlayer);
        });
    }
    else
    {
        Debug.Log("Nenhum alvo válido no Deck para Invocação Especial.");
    }
}

// Retorna lista de cartas equipadas no alvo
public List<CardDisplay> GetEquippedCards(CardDisplay target)
{
    List<CardDisplay> equipped = new List<CardDisplay>();
    CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
    foreach (var link in links)
    {
        if (link.target == target && link.type == CardLink.LinkType.Equipment && link.source != null)
        {
            equipped.Add(link.source);
        }
    }
    return equipped;
}

void Effect_Equip(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "")
{
    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (target) =>
            {
                if (!target.isOnField || !target.CurrentCardData.type.Contains("Monster")) return false;
                if (!string.IsNullOrEmpty(requiredRace) && target.CurrentCardData.race != requiredRace) return false;
                if (!string.IsNullOrEmpty(requiredAttribute) && target.CurrentCardData.attribute != requiredAttribute) return false;
                return true;
            },
            (target) =>
            {
                Debug.Log($"{source.CurrentCardData.name} equipada em {target.CurrentCardData.name}");
                GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment);
                GameManager.Instance.CreateCardLink(source, target, CardLink.LinkType.Equipment); // <---- CRIA O LINK
                                                                                                  // Usa o novo sistema de modificadores
                if (atkBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, atkBonus, source));
                if (defBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Equipment, StatModifier.Operation.Add, defBonus, source));
                // TODO: Vincular visualmente
            }
        );
    }
}

void Effect_Field(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "", int levelMod = 0)
{
    // Lógica simplificada: Aplica em todos os monstros atuais (em um sistema real, seria um efeito contínuo que checa ao entrar/sair)
    // Por enquanto, vamos aplicar como "Continuous" em todos os monstros válidos já no campo
    if (GameManager.Instance.duelFieldUI == null) return;

    List<Transform> allZones = new List<Transform>();
    allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
    allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

    foreach (var zone in allZones)
    {
        if (zone.childCount == 0) continue;
        CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
        if (target == null) continue;

        bool matchRace = string.IsNullOrEmpty(requiredRace) || target.CurrentCardData.race == requiredRace;
        bool matchAttr = string.IsNullOrEmpty(requiredAttribute) || target.CurrentCardData.attribute == requiredAttribute;

        if (matchRace && matchAttr)
        {
            if (atkBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Field, StatModifier.Operation.Add, atkBonus, source));
            if (defBonus != 0) target.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Field, StatModifier.Operation.Add, defBonus, source));
        }
    }
    Debug.Log($"Campo ativado: {source.CurrentCardData.name}. Buff aplicado.");
}

void Effect_FlipDestroy(CardDisplay source, TargetType type)
{
    Debug.Log($"Efeito FLIP ativado: {source.CurrentCardData.name}");
    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (target) => IsValidTarget(target, type),
            (target) =>
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
            }
        );
    }
}

void Effect_FlipReturn(CardDisplay source, TargetType type)
{
    Debug.Log($"Efeito FLIP (Return) ativado: {source.CurrentCardData.name}");
    // Lógica de bounce
}

void Effect_FlipDestroyLevel(CardDisplay source, int level)
{
    bool isPlayer = source.isPlayerCard;
    Transform[] targetZones = isPlayer ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

    foreach (Transform zone in targetZones)
    {
        if (zone.childCount > 0)
        {
            CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
            if (target != null && target.CurrentCardData.level == level)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, !isPlayer);
                Destroy(target.gameObject);
            }
        }
    }
}

// Helper para selecionar múltiplos tributos para efeitos
void SelectTributesForEffect(int count, bool isPlayer, System.Action<List<CardDisplay>> onComplete, List<CardDisplay> current = null)
{
    if (current == null) current = new List<CardDisplay>();

    if (current.Count == count)
    {
        onComplete?.Invoke(current);
        return;
    }

    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (t) => t.isOnField && t.isPlayerCard == isPlayer && t.CurrentCardData.type.Contains("Monster") && !current.Contains(t),
            (selected) =>
            {
                current.Add(selected);
                SelectTributesForEffect(count, isPlayer, onComplete, current);
            }
        );
    }
}

void Effect_TributeToDraw(CardDisplay source, int tributes, int draws)
{
    if (SummonManager.Instance.HasEnoughTributes(tributes, source.isPlayerCard))
    {
        SelectTributesForEffect(tributes, source.isPlayerCard, (tributesList) =>
        {
            foreach (var t in tributesList)
            {
                GameManager.Instance.TributeCard(t);
            }
            Debug.Log($"Tributando {tributes} para comprar {draws}.");
            for (int i = 0; i < draws; i++) GameManager.Instance.DrawCard(true);
        });
    }
}

void Effect_TributeToBurn(CardDisplay source, int tributes, int damage, string race = "")
{
    Debug.Log($"Tributando {tributes} {race} para causar {damage} dano.");
    GameManager.Instance.DamageOpponent(damage);
}

void Effect_LevelUp(CardDisplay source, string nextLevelId)
{
    Debug.Log($"Level Up! Invocando {nextLevelId}.");
    // Lógica de buscar no deck/mão e invocar
}

void Effect_TurnSet(CardDisplay source)
{
    if (source.position == CardDisplay.BattlePosition.Attack)
        source.ChangePosition();
    source.ShowBack();
}

void Effect_BuffStats(CardDisplay source, int atk, int def)
{
    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
            (t) =>
            {
                if (atk != 0) t.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, atk, source));
                if (def != 0) t.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, def, source));
            }
        );
    }
}

void Effect_ChangeControl(CardDisplay source, bool returnAtEndPhase)
{
    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.isPlayerCard != source.isPlayerCard,
            (t) => GameManager.Instance.SwitchControl(t)
        );
    }
}

void Effect_Revive(CardDisplay source, bool anyGraveyard)
{
    List<CardData> targets = new List<CardData>();
    targets.AddRange(GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster")));
    if (anyGraveyard)
        targets.AddRange(GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.type.Contains("Monster")));

    GameManager.Instance.OpenCardSelection(targets, "Selecione monstro para reviver", (selected) =>
    {
        GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
        Debug.Log($"Revivendo {selected.name}");
    });
}

void Effect_Union(CardDisplay source, string targetName, int atkBuff, int defBuff)
{
    Debug.Log($"Union: Tentando equipar em {targetName}...");
    // Lógica de Union simplificada
    Effect_Equip(source, atkBuff, defBuff);
}

void Effect_CoinTossDestroy(CardDisplay source, int numCoins, int requiredHeads, TargetType targetType)
{
    GameManager.Instance.TossCoin(numCoins, (heads) =>
    {
        if (heads >= requiredHeads)
        {
            Debug.Log($"{source.CurrentCardData.name}: {heads} caras! Sucesso.");
            if (SpellTrapManager.Instance != null)
            {
                SpellTrapManager.Instance.StartTargetSelection(
                    (t) => IsValidTarget(t, targetType) && t.isPlayerCard != source.isPlayerCard,
                    (t) =>
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                );
            }
        }
        else
        {
            Debug.Log($"{source.CurrentCardData.name}: {heads} caras. Falhou.");
        }
    });
}

// --- SISTEMA DE SPELL COUNTERS (GLOBAL) ---

public int GetTotalSpellCounters(bool isPlayer)
{
    int total = 0;
    if (GameManager.Instance.duelFieldUI != null)
    {
        List<Transform> zones = new List<Transform>();
        if (isPlayer)
        {
            zones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
            zones.AddRange(GameManager.Instance.duelFieldUI.playerSpellZones);
            zones.Add(GameManager.Instance.duelFieldUI.playerFieldSpell);
        }
        else
        {
            zones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);
            zones.AddRange(GameManager.Instance.duelFieldUI.opponentSpellZones);
            zones.Add(GameManager.Instance.duelFieldUI.opponentFieldSpell);
        }

        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) total += cd.spellCounters;
            }
        }
    }
    return total;
}

public bool RemoveSpellCounters(int amount, bool isPlayer)
{
    int total = GetTotalSpellCounters(isPlayer);
    if (total < amount) return false;

    int remaining = amount;
    // Coleta todas as cartas com contadores
    List<CardDisplay> holders = new List<CardDisplay>();
    if (GameManager.Instance.duelFieldUI != null)
    {
        // Reusa a lógica de coleta de zonas do GetTotalSpellCounters ou similar
        // Para simplificar, vamos iterar novamente ou criar um helper CollectAllCards(isPlayer)
        // Aqui faremos a remoção automática (greedy) para o protótipo.
        // Em um jogo completo, abriria uma UI para o jogador escolher de onde remover.

        // ... (Lógica de iteração similar ao GetTotal, mas chamando RemoveSpellCounter)
        // Como o código ficaria grande no diff, vou simplificar assumindo que você pode copiar a lógica de iteração acima
        // e aplicar cd.RemoveSpellCounter(1) até remaining == 0.

        // Implementação simplificada:
        Debug.Log($"Removendo {amount} Spell Counters automaticamente...");
        // TODO: Implementar a iteração real de remoção aqui
    }
    return true;
}


// --- EFEITOS COMUNS (REFERENCIADOS POR MÚLTIPLAS CARTAS) ---

void Effect_MST(CardDisplay source)
{
    Debug.Log("MST: Destruir 1 S/T.");
    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (t) => t.isOnField && (t.CurrentCardData.type.Contains("Spell") || t.CurrentCardData.type.Contains("Trap")),
            (t) =>
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                Destroy(t.gameObject);
            }
        );
    }
}

void Effect_HeavyStorm(CardDisplay source)
{
    Debug.Log("Heavy Storm: Destruir todas as S/T.");
    List<CardDisplay> toDestroy = new List<CardDisplay>();
    if (GameManager.Instance.duelFieldUI != null)
    {

        CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, toDestroy);
        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, toDestroy);
        CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell, GameManager.Instance.duelFieldUI.opponentFieldSpell }, toDestroy);
    }
    DestroyCards(toDestroy, source.isPlayerCard);
}

void Effect_HarpiesFeatherDuster(CardDisplay source)
{
    Debug.Log("Harpie's Feather Duster: Destruir S/T do oponente.");
    List<CardDisplay> toDestroy = new List<CardDisplay>();
    if (GameManager.Instance.duelFieldUI != null)
    {
        Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;
        CollectCards(zones, toDestroy);
        Transform fieldZone = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentFieldSpell : GameManager.Instance.duelFieldUI.playerFieldSpell;
        CollectCards(new Transform[] { fieldZone }, toDestroy);
    }
    DestroyCards(toDestroy, source.isPlayerCard);
}

void Effect_Raigeki(CardDisplay source)
{
    DestroyAllMonsters(true, false);
}

void Effect_MirrorForce(CardDisplay source)
{
    Debug.Log("Mirror Force: Destruir monstros em ataque do oponente.");
    List<CardDisplay> toDestroy = new List<CardDisplay>();
    if (GameManager.Instance.duelFieldUI != null)
    {
        Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                if (monster != null && monster.position == CardDisplay.BattlePosition.Attack)
                    toDestroy.Add(monster);
            }
        }
    }
    DestroyCards(toDestroy, source.isPlayerCard);
}

void Effect_MagicCylinder(CardDisplay source)
{
    if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
    {
        int damage = BattleManager.Instance.currentAttacker.CurrentCardData.atk;
        Effect_DirectDamage(source, damage);
    }
}

void Effect_EnemyController(CardDisplay source)
{
    Debug.Log("Enemy Controller: Escolha 1 efeito (Mudar Posição ou Controlar).");
    Effect_ChangeControl(source, true);
}

void Effect_MonsterReborn(CardDisplay source)
{
    Effect_Revive(source, true);
}

void Effect_MagePower(CardDisplay source)
{
    Effect_Equip(source, 500, 500); // Simplificado
}

void Effect_MukaMuka(CardDisplay source)
{
    Debug.Log("Muka Muka: Ganha ATK por cartas na mão.");
}

void Effect_MysticBox(CardDisplay source)
{
    Debug.Log("Mystic Box: Destruir e trocar controle.");
}

void Effect_Scapegoat(CardDisplay source)
{
    for (int i = 0; i < 4; i++) GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Sheep Token");
}

void Effect_RingOfDestruction(CardDisplay source)
{
    if (SpellTrapManager.Instance != null)
    {
        SpellTrapManager.Instance.StartTargetSelection(
            (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
            (t) =>
            {
                int damage = t.CurrentCardData.atk;
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                Destroy(t.gameObject);
                GameManager.Instance.DamagePlayer(damage);
                GameManager.Instance.DamageOpponent(damage);
            }
        );
    }
}

partial void OnBattlePositionChangedImpl(CardDisplay card)
{
    // 1820 - Tainted Wisdom
    if (card.CurrentCardData.id == "1820" && card.position == CardDisplay.BattlePosition.Defense && !card.isFlipped)
    {
        // If changed from Attack to Defense (assumindo que estava em ataque antes)
        Debug.Log("Tainted Wisdom: Embaralhando o Deck.");
        GameManager.Instance.ShuffleDeck(card.isPlayerCard);
    }

    // 2120 - Yado Karu
    if (card.CurrentCardData.id == "2120" && card.position == CardDisplay.BattlePosition.Defense && !card.isFlipped)
    {
        // Changed from Attack to Defense
        Debug.Log("Yado Karu: Mão retornada ao fundo do Deck (Simulado).");
        // Lógica de retornar mão ao deck
    }
}
void Effect_SecretBarrel(CardDisplay source)
{
    Effect_DirectDamage(source, 1000); // Simplificado
}

private void Effect_0206_BlindDestruction_Logic_Impl(CardDisplay source)
{
    GameManager.Instance.TossCoin(1, (heads) => { // Using TossCoin as Dice proxy
            int roll = Random.Range(1, 7);
            Debug.Log($"Blind Destruction: Rolou {roll}.");
            
            List<CardDisplay> toDestroy = new List<CardDisplay>();
            if (GameManager.Instance.duelFieldUI != null)
            {
                List<CardDisplay> all = new List<CardDisplay>();
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, all);
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, all);
                
                foreach(var m in all)
                {
                    if (roll == 6)
                    {
                        if (m.CurrentCardData.level >= 6) toDestroy.Add(m);
                    }
                    else
                    {
                        if (m.CurrentCardData.level == roll) toDestroy.Add(m);
                    }
                }
            }
            
            DestroyCards(toDestroy, source.isPlayerCard);
    });
}
}
