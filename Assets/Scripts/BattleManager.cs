using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Estado da Batalha")]
    public CardDisplay currentAttacker;
    public CardDisplay currentTarget;
    public bool isBattlePhase = false;
    public bool cannotAttackFaceDown = false;
    public bool forceDirectAttack = false;
    public bool gravekeepersProtected = false; // Charm of Shabti
    public bool dimensionWallActive = false; // Dimension Wall
    public bool globalPiercing = false; // Meteorain
    public bool battlePositionsLocked = false; // Mesmeric Control
    public bool patricianOfDarknessActive = false; // Patrician of Darkness (1406)
    public bool wabokuActive = false; // Waboku (2047)
    public bool noBattleDamageThisTurn = false; // Winged Kuriboh (2090)
    public bool bewdCannotAttackThisTurn = false; // Burst Stream of Destruction (0251)
    public bool terrorkingCanAttackDirectly = false; // Checkmate (0298)
    public bool crossCounterActive = false; // Cross Counter (0344)
    public bool forceAllAttack = false; // Amazoness Archers (0042)
    public bool threateningRoarActive = false; // Threatening Roar (1918)
    public CardDisplay forcedAttackTarget; // Staunch Defender (1767) / Taunt (1827)
    private bool isBattleResolving = false; // Proteção contra reentrada

    // Verifica se a ativação de armadilhas está bloqueada (ex: Mirage Dragon)
    public bool IsTrapActivationBlocked(bool isPlayer)
    {
        if (!isBattlePhase) return false;

        // Verifica se o oponente controla Mirage Dragon (1248)
        bool oppHasMirage = false;
        Transform[] oppZones = isPlayer ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
        
        foreach(var z in oppZones)
        {
            if(z.childCount > 0)
            {
                var m = z.GetChild(0).GetComponent<CardDisplay>();
                if(m != null && m.CurrentCardData.id == "1248" && !m.isFlipped) oppHasMirage = true;
            }
        }

        // 1590 - Sasuke Samurai #2
        if (GameManager.Instance.IsCardActiveOnField("1590"))
        {
            return true;
        }

        // Ancient Gear (0058, 0059, 0060)
        if (currentAttacker != null)
        {
            string id = currentAttacker.CurrentCardData.id;
            if (id == "0058" || id == "0059" || id == "0060" || id == "1436") return true; // Ancient Gears & Pitch-Black Warwolf
        }

        return oppHasMirage;
    }

    void Awake()
    {
        Instance = this;
    }

    public void ResetTurnStats()
    {
        forceDirectAttack = false;
        globalPiercing = false;
        battlePositionsLocked = false;
        patricianOfDarknessActive = false;
        wabokuActive = false;
        noBattleDamageThisTurn = false;
        bewdCannotAttackThisTurn = false;
        terrorkingCanAttackDirectly = false;
        crossCounterActive = false;
        forceAllAttack = false;
        threateningRoarActive = false;
        forcedAttackTarget = null;
        gravekeepersProtected = false;
        isBattleResolving = false;
        cannotAttackFaceDown = false; // Reset A Feint Plan
        isBattlePhase = false; // Garante que a fase de batalha seja resetada
    }

    // Prepara o monstro para atacar (Seleção)
    public void PrepareAttack(CardDisplay attacker)
    {
        if (PhaseManager.Instance.currentPhase != GamePhase.Battle) return;
        if (attacker.position == CardDisplay.BattlePosition.Defense) return;

        try
        {
            // Garante que o menu de ação esteja fechado ao iniciar um ataque
            if (DuelActionMenu.Instance != null) DuelActionMenu.Instance.CloseMenu();

            // Verifica se pode atacar
            if (!CanAttack(attacker)) return;

            // Desmarca anterior se houver
            if (currentAttacker != null)
            {
                currentAttacker.SetAttackSelectionVisual(false);
            }

            currentAttacker = attacker;
            currentAttacker.SetAttackSelectionVisual(true);
            
            Debug.Log($"Ataque preparado com {attacker.CurrentCardData.name}. Selecione um alvo ou o campo do oponente.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao preparar ataque: {e.Message}\n{e.StackTrace}");
            // Reseta estado para evitar travamento
            currentAttacker = null;
        }
    }

    // Cancela a seleção de ataque
    public void CancelAttack()
    {
        if (currentAttacker != null)
        {
            currentAttacker.SetAttackSelectionVisual(false);
            currentAttacker = null;
            Debug.Log("Seleção de ataque cancelada.");
        }
    }

    // Inicia a declaração de ataque
    public void DeclareAttack(CardDisplay attacker)
    {
        if (PhaseManager.Instance.currentPhase != GamePhase.Battle)
        {
            Debug.LogWarning("Ataques só podem ser declarados na Battle Phase.");
            return;
        }

        if (attacker.position == CardDisplay.BattlePosition.Defense)
        {
            Debug.LogWarning("Monstros em defesa não podem atacar.");
            return;
        }

        // Lógica de Ataques Múltiplos
        int maxAttacks = attacker.maxAttacksPerTurn;

        // 1207 - Mermaid Knight (Ataque duplo com Umi)
        if (attacker.CurrentCardData.id == "1207" && (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013"))) 
            maxAttacks = 2;
        
        // 2003 - Tyrant Dragon (Ataque duplo se oponente tiver monstro)
        if (attacker.CurrentCardData.id == "2003")
        {
            // A regra diz "se o oponente controlar um monstro após o primeiro ataque".
            // Aqui definimos o potencial como 2, a validação de alvo cuidará do resto.
            maxAttacks = 2;
        }

        // 0114 - Asura Priest (Pode atacar todos os monstros)
        if (attacker.CurrentCardData.id == "0114")
        {
            int oppMonsters = 0;
            Transform[] oppZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            foreach (var z in oppZones) if (z.childCount > 0) oppMonsters++;
            maxAttacks = Mathf.Max(1, oppMonsters);
        }

        // 0168 - Berserk Dragon (Pode atacar todos os monstros)
        if (attacker.CurrentCardData.id == "0168")
        {
            int oppMonsters = 0;
            Transform[] oppZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            foreach (var z in oppZones) if (z.childCount > 0) oppMonsters++;
            maxAttacks = Mathf.Max(1, oppMonsters);
        }

        // 0369 - Cyber Twin Dragon
        if (attacker.CurrentCardData.id == "0369") maxAttacks = 2;

        if (attacker.attacksDeclaredThisTurn >= maxAttacks && attacker.hasAttackedThisTurn) // Corrigido: Permite re-atacar se tiver ataques disponíveis
        {
            Debug.LogWarning("Este monstro já atacou neste turno.");
            return;
        }

        // 1851 - The Dark Door
        if (GameManager.Instance.IsCardActiveOnField("1851"))
        {
            Transform[] myZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var z in myZones) {
                if (z.childCount > 0) {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if (m != null && m != attacker && m.attacksDeclaredThisTurn > 0) {
                        Debug.LogWarning("The Dark Door: Apenas 1 monstro pode atacar por Battle Phase.");
                        return;
                    }
                }
            }
        }

        // Verifica restrições de ataque (Gravity Bind, Level Limit, etc)
        if (!CanAttack(attacker)) return;

        // Redireciona para o novo fluxo de preparação
        PrepareAttack(attacker);
    }

    // Tenta realizar um ataque direto (chamado ao clicar no campo do oponente)
    public void TryDirectAttack()
    {
        if (currentAttacker == null) return;
        // Adiciona verificação para impedir ataque no turno 1 ou por outros efeitos
        if (!CanAttack(currentAttacker)) return;
        
        if (currentAttacker.cannotAttackDirectly) {
            Debug.LogWarning("Este monstro não pode atacar diretamente devido ao seu efeito.");
            return;
        }

        if (HasDirectAttackCondition())
        {
                CardDisplay attacker = currentAttacker;

             System.Action triggerAttackEffect = () => {
                 if (CardEffectManager.Instance != null)
                 {
                     CardEffectManager.Instance.OnAttackDeclared(attacker, null, () => PerformDirectAttack(attacker));
                 }
                 else
                 {
                     PerformDirectAttack(attacker);
                 }
             };
             
            System.Action executeAttack = () => {
                currentTarget = null;
                if (currentAttacker != null) currentAttacker.SetAttackSelectionVisual(false);

                if (ChainManager.Instance != null)
                {
                   ChainManager.Instance.AddToChain(attacker, attacker.isPlayerCard, ChainManager.TriggerType.Attack, null, triggerAttackEffect);
                }
                else
                {
                    triggerAttackEffect();
                }
            };

            if (GameManager.Instance != null && !GameManager.Instance.confirmAttackTarget)
            {
                executeAttack();
            }
            else
            {
                UIManager.Instance.ShowConfirmation($"Atacar diretamente com {attacker.CurrentCardData.name}?", executeAttack, CancelAttack);
            }
        }
        else
        {
             CancelAttack();
        }
    }

    public bool CanAttack(CardDisplay attacker)
    {
        if (threateningRoarActive)
        {
            Debug.LogWarning("Ataque impedido: Threatening Roar está ativo.");
            return false;
        }

        if (attacker.cannotAttackThisTurn)
        {
            Debug.LogWarning("Ataque impedido: Este monstro não pode atacar neste turno devido a um efeito.");
            return false;
        }

        if (CardEffectManager.Instance != null)
        {
            return CardEffectManager.Instance.CanDeclareAttack(attacker);
        }
        return true;
    }

    // Helper público para verificar se pode atacar direto (usado pelo Quick Attack)
    public bool CanAttackDirectly()
    {
        return HasDirectAttackCondition();
    }

    public void SelectTarget(CardDisplay target)
    {
        if (currentAttacker == null) return;
        if (!CanAttack(currentAttacker)) return;

        if (target.isPlayerCard == currentAttacker.isPlayerCard)
        {
            Debug.LogWarning("Não pode atacar seu próprio monstro.");
            return;
        }

        if (cannotAttackFaceDown && target.isFlipped)
        {
            UIManager.Instance.ShowMessage("Não é possível atacar monstros virados para baixo neste turno.");
            return;
        }

        // 0113 - Astral Barrier
        if (GameManager.Instance.IsCardActiveOnField("0113") && target.isPlayerCard != currentAttacker.isPlayerCard)
        {
            // Simplificação para protótipo: Força a conversão do ataque (Na prática seria opcional)
            Debug.Log("Astral Barrier: Ataque em monstro convertido em Ataque Direto.");
            TryDirectAttack();
            CancelAttack();
            return;
        }

        // Forced Attack Target
        if (forcedAttackTarget != null && target != forcedAttackTarget)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Você deve atacar o alvo forçado (Taunt / Staunch Defender)!");
            return;
        }

        // Exceção: Patrician of Darkness permite selecionar seus próprios monstros como alvo do ataque do oponente
        if (patricianOfDarknessActive && !currentAttacker.isPlayerCard && target.isPlayerCard)
        {
            // Permite
            Debug.Log("Patrician of Darkness: Alvo redirecionado.");
        }

        // 1686 - Solar Flare Dragon
        if (target.CurrentCardData.id == "1686")
        {
            int pyroCount = 0;
            Transform[] targetZones = target.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var z in targetZones) {
                if (z.childCount > 0) {
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && !cd.isFlipped && cd.CurrentCardData.race == "Pyro") pyroCount++;
                }
            }
            if (pyroCount > 1) {
                Debug.LogWarning("Solar Flare Dragon não pode ser atacado enquanto outro Pyro estiver no campo.");
                return;
            }
        }

        // Command Knight (0318)
        if (target.CurrentCardData.id == "0318")
        {
            bool hasOtherMonster = false;
            Transform[] targetZones = target.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var zone in targetZones)
            {
                if (zone.childCount > 0)
                {
                    var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && cd != target) hasOtherMonster = true;
                }
            }
            if (hasOtherMonster)
            {
                Debug.Log("Command Knight não pode ser atacado enquanto houver outro monstro.");
                return;
            }
        }

        // 2040 - Vilepawn Archfiend
        if (target.CurrentCardData.name.Contains("Archfiend") && target.CurrentCardData.id != "2040")
        {
            // Se houver um Vilepawn Archfiend no campo do alvo, ele protege outros Archfiends
            Transform[] targetZones = target.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var z in targetZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.CurrentCardData.id == "2040" && !m.isFlipped)
                    {
                        Debug.Log("Vilepawn Archfiend: Redirecionando ataque para si mesmo.");
                        // Em um sistema ideal, forçaria a troca de alvo. Aqui bloqueamos o ataque ao alvo original.
                        // return; // Ou implementar lógica de redirecionamento
                    }
                }
            }
        }

        // 0049 - Amazoness Tiger
        if (target.CurrentCardData.name.Contains("Amazoness") && target.CurrentCardData.id != "0049")
        {
            Transform[] targetZones = target.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach(var z in targetZones)
            {
                if(z.childCount > 0)
                {
                    var m = z.GetChild(0).GetComponent<CardDisplay>();
                    if(m != null && m.CurrentCardData.id == "0049" && !m.isFlipped)
                    {
                        Debug.Log("Amazoness Tiger: Redirecionando ataque para si mesmo (Bloqueio).");
                        return;
                    }
                }
            }
        }

        // 1534 - Ring of Magnetism
        CardDisplay forcedTarget = null;
        Transform[] targetZonesCheck = currentAttacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
        foreach(var z in targetZonesCheck) {
            if (z.childCount > 0) {
                var m = z.GetChild(0).GetComponent<CardDisplay>();
                if (m != null && !m.isFlipped && CardEffectManager.Instance != null && CardEffectManager.Instance.GetEquippedCards(m).Exists(c => c.CurrentCardData.id == "1534")) {
                    forcedTarget = m; break;
                }
            }
        }
        if (forcedTarget != null && target != forcedTarget) {
            UIManager.Instance.ShowMessage("Você deve atacar o monstro equipado com Ring of Magnetism!");
            return;
        }

        // 1577 - Sacred Defense Barrier
        if (GameManager.Instance.IsCardActiveOnField("1577"))
        {
            if (target.CurrentCardData.race == "Fairy")
            {
                // Se houver outro monstro não-Fada, o ataque é válido (mas não neste alvo)
                // Se SÓ houver Fadas, o ataque é inválido.
                // Simplificado: Bloqueia o alvo se for Fada.
                Debug.Log("Sacred Defense Barrier: Não pode atacar Fadas.");
                return;
            }
        }

        // Captura as variáveis localmente para garantir que não mudem durante o callback da UI
        CardDisplay attacker = currentAttacker;
        CardDisplay targetCard = target;

        // FIX: Ocultar nome se estiver face-down para não dar spoiler
        // isFlipped = true significa que está mostrando o verso (Face-Down).
        string targetName = !target.isFlipped ? target.CurrentCardData.name : "Monstro Face-Down";

        System.Action triggerAttackEffect = () => {
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.OnAttackDeclared(attacker, targetCard, () => {
                    PerformBattle(attacker, targetCard);
                });
            }
            else
            {
                PerformBattle(attacker, targetCard);
            }
        };

        System.Action executeAttack = () => {
             currentTarget = targetCard;
             if (currentAttacker != null) currentAttacker.SetAttackSelectionVisual(false);
             
             // FIX: Adicionado fallback. Se ChainManager não existir, ataca direto.
             if (ChainManager.Instance != null)
             {
                 ChainManager.Instance.AddToChain(attacker, attacker.isPlayerCard, ChainManager.TriggerType.Attack, targetCard, triggerAttackEffect);
             }
             else
             {
                 triggerAttackEffect();
             }
        };

        if (GameManager.Instance != null && !GameManager.Instance.confirmAttackTarget)
        {
            executeAttack();
        }
        else
        {
            UIManager.Instance.ShowConfirmation($"Atacar {targetName}?", executeAttack, CancelAttack);
        }
    }

    private bool HasDirectAttackCondition()
    {
        if (forceDirectAttack) return true;
        if (GameManager.Instance.duelFieldUI == null) return false;
        
        Transform[] enemyZones = currentAttacker != null && currentAttacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

        bool hasMonsters = false;
        bool hasToon = false;
        bool onlyDefense = true;

        foreach (Transform zone in enemyZones)
        {
            if (zone.childCount > 0)
            {
                hasMonsters = true;
                CardDisplay defender = zone.GetChild(0).GetComponent<CardDisplay>();
                if (defender != null)
                {
                    if (!defender.isFlipped && defender.CurrentCardData.race == "Toon") hasToon = true;
                    if (defender.position == CardDisplay.BattlePosition.Attack) onlyDefense = false;
                }
            }
        }

        if (!hasMonsters) return true;

        if (currentAttacker != null && (currentAttacker.CurrentCardData.race == "Toon" || currentAttacker.CurrentCardData.id == "0215"))
            if (!hasToon) return true;

        if (terrorkingCanAttackDirectly && currentAttacker != null && currentAttacker.CurrentCardData.name == "Terrorking Archfiend")
            return true;

        if (currentAttacker != null && (currentAttacker.CurrentCardData.id == "1553" || currentAttacker.CurrentCardData.id == "1627"))
            if (onlyDefense) return true;
            
        if (currentAttacker != null && currentAttacker.CurrentCardData.id == "0053")
            if (GameManager.Instance.IsCardActiveOnField("2015") || GameManager.Instance.IsCardActiveOnField("0013")) return true;

        if (currentAttacker != null && currentAttacker.CurrentCardData.id == "0193")
        {
            Transform[] enemySpellZones = currentAttacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;
            bool hasST = false;
            foreach (Transform zone in enemySpellZones) if (zone.childCount > 0) hasST = true;
            if (onlyDefense && !hasST) return true;
        }

        if (currentAttacker != null && currentAttacker.CurrentCardData.id == "0037")
        {
            bool canAttackDirectly = true;
            Transform[] enemyMonsterZones = currentAttacker.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

            foreach (var zone in enemyMonsterZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null && !monster.isFlipped) // Só verifica monstros face-up
                    {
                        string attribute = monster.CurrentCardData.attribute;
                        if (attribute != "Earth" && attribute != "Water" && attribute != "Fire")
                        {
                            canAttackDirectly = false;
                            break;
                        }
                    }
                    else if (monster != null && monster.isFlipped) // Se tiver monstro virado para baixo, não pode atacar direto
                    {
                        canAttackDirectly = false;
                        break;
                    }
                }
            }
            if (canAttackDirectly) return true;
        }

        if (currentAttacker != null && currentAttacker.canAttackDirectly) return true;

        return false;
    }

    private void DealBattleDamage(CardDisplay damageTaker, int damage)
    {
        if (damage <= 0 || damageTaker == null) return;
        bool isPlayer = damageTaker.isPlayerCard;

        // 1738 - Spirit Barrier
        if (GameManager.Instance.IsCardActiveOnField("1738"))
        {
            Transform[] mySpells = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            Transform[] myMonsters = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            bool barrierActive = false;
            foreach(var z in mySpells) if(z.childCount > 0 && z.GetChild(0).GetComponent<CardDisplay>().CurrentCardData.id == "1738" && !z.GetChild(0).GetComponent<CardDisplay>().isFlipped) barrierActive = true;
            bool hasMon = false;
            foreach(var z in myMonsters) if(z.childCount > 0) hasMon = true;
            if (barrierActive && hasMon) {
                Debug.Log("Spirit Barrier: Dano de batalha aos LP é 0.");
                return;
            }
        }

        // 0650 - Metal Fiend Token (Reflexo de Dano)
        if (damageTaker.CurrentCardData.name == "Metal Fiend Token")
        {
            Debug.Log("Metal Fiend Token: Dano refletido para o oponente!");
            isPlayer = !isPlayer;
        }

        // 0811 - Gravekeeper's Vassal
        if (currentAttacker != null && currentAttacker.CurrentCardData.id == "0811")
        {
            if (damageTaker != currentAttacker && CardEffectManager.Instance != null) // Só se o dano for para o oponente
            {
                Debug.Log("Gravekeeper's Vassal: Dano de batalha convertido em efeito.");
                CardEffectManager.Instance.Effect_DirectDamage(currentAttacker, damage);
                return;
            }
        }

        // 0045 - Amazoness Fighter
        if (damageTaker.CurrentCardData.id == "0045")
        {
            Debug.Log("Amazoness Fighter: Dano de batalha zerado para o controlador.");
            return;
        }

        // 1513 - Relinquished (Reflexo de Dano)
        if (damageTaker.CurrentCardData.id == "1513")
        {
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.GetEquippedCards(damageTaker).Count > 0)
            {
                Debug.Log("Relinquished: Oponente toma o mesmo dano de batalha.");
                if (isPlayer) GameManager.Instance.DamageOpponent(damage);
                else GameManager.Instance.DamagePlayer(damage);
            }
        }

        // 0048 - Amazoness Swords Woman
        if (damageTaker.CurrentCardData.id == "0048")
        {
            Debug.Log("Amazoness Swords Woman: Dano refletido para o oponente!");
            isPlayer = !isPlayer;
        }

        // 1556 - Rod of the Mind's Eye
        if (currentAttacker != null)
        {
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.GetEquippedCards(currentAttacker).Exists(c => c.CurrentCardData.id == "1556"))
            {
                Debug.Log("Rod of the Mind's Eye: Dano de batalha fixado em 1000.");
                damage = 1000;
            }
        }

        if (isPlayer) GameManager.Instance.DamagePlayer(damage);
        else GameManager.Instance.DamageOpponent(damage);

        // Hook OnDamageDealt (Importante para Don Zaloog, Dark Scorpions, etc)
        if (CardEffectManager.Instance != null)
        {
            CardDisplay inflicter = (damageTaker == currentAttacker) ? currentTarget : currentAttacker;
            
            // Se foi refletido, quem tomou o ataque inicial é quem causou o dano no oponente
            if (damageTaker.CurrentCardData.id == "0048" || damageTaker.CurrentCardData.name == "Metal Fiend Token") 
                inflicter = damageTaker;

            if (inflicter != null)
                CardEffectManager.Instance.OnDamageDealt(inflicter, damageTaker, damage);
        }
    }

    private void CheckTrapsAndAttackDirectly(CardDisplay attacker)
    {
        // Obsoleto. O fluxo agora é controlado pelo ChainManager.
    }

    public void PerformDirectAttack(CardDisplay attacker)
    {
        if (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != GamePhase.Battle)
        {
            Debug.LogWarning("Ataque direto cancelado: A fase de batalha foi encerrada.");
            ClearBattleState();
            if (attacker != null) attacker.SetAttackSelectionVisual(false);
            return;
        }
        if (attacker == null || !attacker.isOnField || currentAttacker != attacker)
        {
            Debug.LogWarning("Ataque direto cancelado: Atacante inválido ou ataque interrompido.");
            ClearBattleState();
            if (attacker != null) attacker.SetAttackSelectionVisual(false);
            return;
        }

        System.Action continueAttack = () => {
            int damage = attacker.currentAtk; // Usa currentAtk (pode ter mudado no hook acima)
            Debug.Log($"Ataque Direto! Dano: {damage}");
            
            if (wabokuActive || noBattleDamageThisTurn)
            {
                damage = 0;
                Debug.Log("Dano de ataque direto prevenido (Waboku/Winged Kuriboh).");
            }
            
            if (damage > 0)
            {
                if (attacker.CurrentCardData.id == "0811" && CardEffectManager.Instance != null)
                {
                    Debug.Log("Gravekeeper's Vassal: Dano de ataque direto convertido para efeito.");
                    CardEffectManager.Instance.Effect_DirectDamage(attacker, damage);
                }
                else
                {
                    if (attacker.isPlayerCard) GameManager.Instance.DamageOpponent(damage);
                    else GameManager.Instance.DamagePlayer(damage);
                }
                
                if (CardEffectManager.Instance != null)
                    CardEffectManager.Instance.OnDamageDealt(attacker, null, damage);
            }
            
            if (DuelFXManager.Instance != null) 
                DuelFXManager.Instance.PlayAttack(attacker, null, null); // Null target = direct
            
            attacker.attacksDeclaredThisTurn++;
            if (attacker.attacksDeclaredThisTurn >= attacker.maxAttacksPerTurn) attacker.hasAttackedThisTurn = true;
            ClearBattleState();
            if (attacker != null) attacker.SetAttackSelectionVisual(false); // Garante limpeza visual
        };

        // Hook OnDamageCalculation (para Injection Fairy Lily, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageCalculation(attacker, null, continueAttack);
        }
        else
        {
            continueAttack();
        }
    }

    private void CheckTrapsAndBattle(CardDisplay attacker, CardDisplay target)
    {
        // Obsoleto. O fluxo agora é controlado pelo ChainManager.
    }

    public void PerformBattle(CardDisplay attacker, CardDisplay target)
    {
        if (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != GamePhase.Battle)
        {
            Debug.LogWarning("Batalha cancelada: A fase de batalha foi encerrada.");
            isBattleResolving = false;
            ClearBattleState();
            if (attacker != null) attacker.SetAttackSelectionVisual(false);
            return;
        }
        // FIX: Verifica se os monstros ainda existem e estão no campo (podem ter sido destruídos durante a Chain)
        if (attacker == null || !attacker.isOnField || target == null || !target.isOnField || currentAttacker != attacker)
        {
            Debug.LogWarning("Batalha cancelada: Atacante/Alvo inválidos ou ataque interrompido.");
            isBattleResolving = false;
            ClearBattleState();
            if (attacker != null) attacker.SetAttackSelectionVisual(false);
            return;
        }

        if (isBattleResolving) 
        {
            Debug.LogWarning("Batalha já está sendo resolvida. Ignorando chamada duplicada.");
            return;
        }
        isBattleResolving = true;

        // Captura o estado face-down antes de revelar
        bool targetWasFaceDown = target.isFlipped;

        // Revela o alvo se estiver virado para baixo (Flip)
        if (target.isFlipped)
        {
            target.RevealCard(true); // true = triggered by attack
        }

        System.Action continueBattle = () => {
            // Blast Sphere (0201) - Antes do cálculo de dano
            // Verifica se estava face-down no início do ataque
            if (target.CurrentCardData.id == "0201" && targetWasFaceDown)
            {
                Debug.Log("Blast Sphere: Atacado face-down. Equipando ao atacante...");
                // Move Blast Sphere para S/T do dono do Blast Sphere e equipa no atacante
                // Simplificação: Apenas cria o link e destrói o objeto visual do monstro para simular que virou equip
                // Em um sistema completo, moveria para a zona de S/T.
                GameManager.Instance.CreateCardLink(target, attacker, CardLink.LinkType.Equipment);
                target.AddSpellCounter(1); // Marca para destruir na Standby
                isBattleResolving = false;
                return; // Cancela batalha
            }
            
            // 1022 - Kiseitai
            if (target.CurrentCardData.id == "1022" && targetWasFaceDown)
            {
                Debug.Log("Kiseitai: Atacado face-down. Equipando ao atacante...");
                GameManager.Instance.CreateCardLink(target, attacker, CardLink.LinkType.Equipment);
                isBattleResolving = false;
                return; // Cancela batalha
            }

            // 0062 - Ancient Lamp
            if (target.CurrentCardData.id == "0062" && targetWasFaceDown)
            {
                Debug.Log("Ancient Lamp: Redirecionando ataque para monstro do oponente!");
                Transform[] oppZones = attacker.isPlayerCard ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
                List<CardDisplay> validTargets = new List<CardDisplay>();
                foreach (var z in oppZones)
                {
                    if (z.childCount > 0)
                    {
                        var m = z.GetChild(0).GetComponent<CardDisplay>();
                        if (m != null && m != attacker) validTargets.Add(m);
                    }
                }
                if (validTargets.Count > 0)
                {
                    CardDisplay newTarget = validTargets[Random.Range(0, validTargets.Count)]; // Automático
                    Debug.Log($"Ancient Lamp forçou {attacker.CurrentCardData.name} a atacar {newTarget.CurrentCardData.name}!");
                    
                    // Retira seleção de ataque e muda o alvo
                    target = newTarget;
                    targetWasFaceDown = target.isFlipped;
                    if (target.isFlipped) target.RevealCard(true);
                }
            }

            attacker.battledThisTurn = true;
            target.battledThisTurn = true;

            int atkPoints = attacker.currentAtk; // Usa currentAtk
            int targetPoints = (target.position == CardDisplay.BattlePosition.Attack) ? target.currentAtk : target.currentDef;

            Debug.Log($"[BattleManager] INICIO Batalha: {attacker.CurrentCardData.name} ({atkPoints}) vs {target.CurrentCardData.name} ({targetPoints})");

            if (DuelFXManager.Instance != null)
            {
                Debug.Log("[BattleManager] Solicitando animação ao DuelFXManager...");
                DuelFXManager.Instance.PlayAttack(attacker, target, () => {
                    Debug.Log("[BattleManager] Animação concluída. Resolvendo dano...");
                    ResolveDamage(attacker, target, atkPoints, targetPoints, targetWasFaceDown);
                    isBattleResolving = false;
                });
            }
            else
            {
                ResolveDamage(attacker, target, atkPoints, targetPoints, targetWasFaceDown);
                isBattleResolving = false;
            }
        };

        // Hook OnDamageCalculation (Skyscraper, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageCalculation(attacker, target, continueBattle);
        }
        else
        {
            continueBattle();
        }
    }

    private void DestroyMonsterInBattle(CardDisplay monster)
    {
        if (GameManager.Instance.IsCardActiveOnField("0801")) { // Grave Protector
            Debug.Log("Grave Protector: Monstro embaralhado no deck em vez de ir ao GY.");
            GameManager.Instance.ReturnToDeck(monster, false);
            GameManager.Instance.ShuffleDeck(monster.isPlayerCard);
        } else {
            GameManager.Instance.SendToGraveyard(monster.CurrentCardData, monster.isPlayerCard, CardLocation.Field, SendReason.Battle);
            Destroy(monster.gameObject);
        }
    }

    private void ResolveDamage(CardDisplay attacker, CardDisplay target, int atk, int def, bool targetWasFaceDown = false)
    {
        // B.E.S. Immunity (0124, 0125)
        bool targetIsBES = target.CurrentCardData.id == "0124" || target.CurrentCardData.id == "0125";

        // 1554 - Rocket Warrior (Invulnerável na batalha durante seu turno)
        bool attackerIsRocketWarrior = attacker.CurrentCardData.id == "1554" && attacker.isPlayerCard == GameManager.Instance.isPlayerTurn; // Simplificado: Assume turno do atacante
        
        // Se Rocket Warrior ataca, ele não toma dano e não é destruído
        // Mas o alvo pode ser destruído e tomar dano normalmente? Não, Rocket Warrior diz "battle damage to both players becomes 0".
        bool noBattleDamage = attackerIsRocketWarrior;

        // Waboku (2047) / Winged Kuriboh (2090)
        if (wabokuActive || noBattleDamageThisTurn)
        {
            noBattleDamage = true;
        }

        // 1593 - Satellite Cannon
        if (target.CurrentCardData.id == "1593" && attacker.CurrentCardData.level <= 7)
        {
            targetIsBES = true;
            Debug.Log("Satellite Cannon: Protegido contra monstro Lv 7-.");
        }

        // 2017 - Union Attack (Flag no atacante)
        if (attacker.cannotInflictBattleDamage)
        {
            Debug.Log("Union Attack: Dano de batalha zerado.");
            // Lógica: Se o atacante venceria e causaria dano, o dano vira 0.
            // Mas a destruição do monstro ainda ocorre? "cannot inflict battle damage to your opponent".
            // Sim, destruição ocorre, apenas o dano aos LP é prevenido.
            // Vamos aplicar isso nos blocos abaixo.
        }

            // 1165 - Marshmallon
            if (target.CurrentCardData.id == "1165") targetIsBES = true; 
            // 1364 - Obnoxious Celtic Guard
            if (target.CurrentCardData.id == "1364" && atk >= 1900) targetIsBES = true;
            // 1349 - Ninjitsu Art of Decoy
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.GetEquippedCards(target).Exists(c => c.CurrentCardData.id == "1349")) targetIsBES = true;
            // 1096 - Lone Wolf
            if (target.CurrentCardData.id == "1264" || target.CurrentCardData.id == "1182") {
                if (GameManager.Instance.IsCardActiveOnField("1096")) targetIsBES = true;
            }

        if (target.position == CardDisplay.BattlePosition.Attack)
        {
            // Ataque vs Ataque
            if (atk > def)
            {
                attacker.destroyedMonsterThisTurn = true;
                int damage = atk - def;
                Debug.Log($"Vitória do Atacante! Oponente toma {damage} de dano. Alvo destruído.");
                
                if (!noBattleDamage && !attacker.cannotInflictBattleDamage)
                {
                    DealBattleDamage(target, damage);
                }
                else
                {
                    Debug.Log($"[BattleManager] Dano prevenido! NoBattleDamage: {noBattleDamage}, AttackerCannotInflict: {attacker.cannotInflictBattleDamage}");
                }
                
                if (!targetIsBES)
                {
                    // Charm of Shabti (0296) / Waboku (2047)
                    if ((gravekeepersProtected && target.CurrentCardData.name.Contains("Gravekeeper")) || wabokuActive)
                    {
                        Debug.Log("Monstro protegido (Charm of Shabti ou Waboku).");
                    }
                    else if (target.CurrentCardData.id == "0492" && atk <= 1900)
                    {
                        Debug.Log("Different Dimension Dragon: Protegido contra monstro com ATK <= 1900.");
                    }
                    else
                    {
                        DestroyMonsterInBattle(target);
                    }
                }
            }
            else if (atk < def)
            {
                int damage = def - atk;
                Debug.Log($"Vitória do Alvo! Atacante toma {damage} de dano. Atacante destruído.");
                if (!noBattleDamage)
                {
                    DealBattleDamage(attacker, damage);
                }
                else
                {
                    Debug.Log($"[BattleManager] Dano prevenido! NoBattleDamage: {noBattleDamage}");
                }

                if (!attackerIsRocketWarrior)
                {
                    if (attacker.CurrentCardData.id == "0492" && def <= 1900)
                        Debug.Log("Different Dimension Dragon: Protegido contra monstro com DEF <= 1900.");
                    else
                        DestroyMonsterInBattle(attacker);
                }
            }
            else // Empate
            {
                Debug.Log("Empate! Ambos destruídos.");
                bool kishido = GameManager.Instance.IsCardActiveOnField("1023");
                if (!wabokuActive && !kishido)
                {
                    if (target.CurrentCardData.id == "0492" && atk <= 1900) Debug.Log("DDD alvo protegido no empate.");
                    else DestroyMonsterInBattle(target);
                }
                if (!attackerIsRocketWarrior && !wabokuActive && !kishido) {
                    if (attacker.CurrentCardData.id == "0492" && def <= 1900) Debug.Log("DDD atacante protegido no empate.");
                    else DestroyMonsterInBattle(attacker);
                }
            }
        }
        else // Ataque vs Defesa
        {
            if (atk > def)
            {
                attacker.destroyedMonsterThisTurn = true;
                Debug.Log("Vitória do Atacante! Alvo destruído (sem dano).");
                if (!wabokuActive)
                {
                    if (target.CurrentCardData.id == "0492" && atk <= 1900)
                        Debug.Log("Different Dimension Dragon: Protegido contra monstro com ATK <= 1900.");
                    else
                        DestroyMonsterInBattle(target);
                }
            }
            else if (atk < def)
            {
                int damage = def - atk;

                // 1780 - Stone Statue of the Aztecs
                if (target.CurrentCardData.id == "1780") {
                    damage *= 2;
                    Debug.Log("Stone Statue of the Aztecs: Dano de batalha refletido dobrado!");
                }

                // 1068 - Legendary Jujitsu Master
                if (target.CurrentCardData.id == "1068") {
                    Debug.Log("Legendary Jujitsu Master: Atacante retornado ao topo do deck.");
                    GameManager.Instance.ReturnToDeck(attacker, true);
                }

                // 1165 - Marshmallon (Dano por atacar face-down)
                if (target.CurrentCardData.id == "1165" && targetWasFaceDown)
                {
                    Debug.Log("Marshmallon: 1000 de dano por atacar face-down.");
                    if (attacker.isPlayerCard) GameManager.Instance.DamagePlayer(1000);
                    else GameManager.Instance.DamageOpponent(1000);
                }
                
                // Dimension Wall (0499)
                // Se o atacante for o oponente e Dimension Wall estiver ativa, o dano volta para ele?
                // Dimension Wall diz: "Instead of you, your opponent takes the Battle Damage".
                // Se o jogador ativou Dimension Wall quando foi atacado:
                if (dimensionWallActive && !attacker.isPlayerCard)
                {
                    Debug.Log("Dimension Wall: Dano refletido para o atacante!");
                    GameManager.Instance.DamageOpponent(damage); // Oponente toma o dano que seria do jogador
                    // O jogador não toma dano (já que damage foi redirecionado)
                    // Mas aqui o atacante (oponente) já tomaria dano por bater em defesa maior.
                    // Dimension Wall é para quando VOCÊ toma dano.
                }
                
                Debug.Log($"Defesa Sólida! Atacante toma {damage} de dano.");

                // 0344 - Cross Counter
                if (crossCounterActive)
                {
                    damage *= 2;
                    Debug.Log("Cross Counter: Dano de reflexão dobrado! Destruindo atacante.");
                    DestroyMonsterInBattle(attacker);
                    crossCounterActive = false;
                }

                DealBattleDamage(attacker, damage);

                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);

                // Continuous Destruction Punch (0323)
                // Se defesa > ataque, destrói o atacante.
                if (GameManager.Instance.IsCardActiveOnField("0323"))
                {
                    Debug.Log("Continuous Destruction Punch: Destruindo atacante.");
                    DestroyMonsterInBattle(attacker);
                }

                // Cross Counter (0344)
                // Se o defensor for Cross Counter (ou tiver o efeito aplicado por armadilha, mas aqui é o monstro 0344? Não, 0344 é a Trap)
                // A carta 0344 é uma Trap Normal que se ativa quando atacado.
                // Vamos assumir que o efeito foi ativado via SpellTrapManager e aplicou um modificador ou flag no alvo.
                // Des Kangaroo (0473)
                // Se ATK atacante < DEF deste card, destrói atacante.
                if (target.CurrentCardData.id == "0473" && atk < def)
                {
                    Debug.Log("Des Kangaroo: Destruindo atacante (ATK < DEF).");
                    DestroyMonsterInBattle(attacker);
                }
            }
            else
            {
                Debug.Log("Empate (Defesa). Nada acontece.");
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);
            }

            // Piercing Damage (Dark Driceratops 0407, etc)
            // Adicionado Meteorain (globalPiercing)
            bool hasBigBangShot = false;
            bool hasCyclonLaser = false;
            if (CardEffectManager.Instance != null)
            {
                hasBigBangShot = CardEffectManager.Instance.GetEquippedCards(attacker).Exists(c => c.CurrentCardData.id == "0172");
                hasCyclonLaser = CardEffectManager.Instance.GetEquippedCards(attacker).Exists(c => c.CurrentCardData.id == "0374");
            }
            
            if (atk > def && (attacker.hasPiercing || attacker.CurrentCardData.id == "0407" || attacker.CurrentCardData.id == "0059" || attacker.CurrentCardData.id == "0031" || globalPiercing || hasBigBangShot || hasCyclonLaser)) 
            {
                int piercing = atk - def;
                Debug.Log($"Dano Perfurante! {piercing} de dano.");
                DealBattleDamage(target, piercing);
            }
        }

        // Hook OnBattleEnd (D.D. Warrior Lady, Mystic Tomato)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnBattleEnd(attacker, target);
        }

        attacker.attacksDeclaredThisTurn++;
        if (attacker.attacksDeclaredThisTurn >= attacker.maxAttacksPerTurn) attacker.hasAttackedThisTurn = true;
        ClearBattleState();
        if (attacker != null) attacker.SetAttackSelectionVisual(false); // Garante limpeza visual
    }

    private void ClearBattleState()
    {
        currentAttacker = null;
        currentTarget = null;
        dimensionWallActive = false;
        // wabokuActive = false; // Waboku dura o turno todo, resetar no PhaseManager
        // noBattleDamageThisTurn = false; // Resetar no PhaseManager
        // patricianOfDarknessActive = false; // Não reseta pois é contínuo enquanto o monstro estiver em campo
        gravekeepersProtected = false; // Reseta no fim da batalha ou turno? Regra diz "until End Phase".
        // isBattleResolving = false; // Não reseta aqui, pois é controlado no PerformBattle
        // Se for até End Phase, deve ser resetado no PhaseManager ou CardEffectManager.
    }

    public void CancelCurrentAttack()
    {
        Debug.Log("Ataque atual cancelado por efeito de carta.");
        ClearBattleState();
    }

    // Chamado pelo clique direito no monstro
    public void TryChangePosition(CardDisplay card)
    {
        if (GameManager.Instance.devMode)
        {
            card.ChangePosition();
            return;
        }

        if (card.hasChangedPositionThisTurn)
        {
            Debug.LogWarning("Este monstro já mudou de posição neste turno.");
            return;
        }

        if (card.summonedThisTurn)
        {
            Debug.LogWarning("Monstros não podem mudar de posição no turno em que foram invocados.");
            return;
        }

        if (PhaseManager.Instance.currentPhase != GamePhase.Main1 && PhaseManager.Instance.currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning("Mudança de posição só permitida na Main Phase.");
            return;
        }

        if (battlePositionsLocked)
        {
            Debug.LogWarning("Mudança de posição bloqueada por efeito (ex: Mesmeric Control).");
            return;
        }

        // 1623 - Shadow Spell
        if (CardEffectManager.Instance != null)
        {
            var equips = CardEffectManager.Instance.GetEquippedCards(card);
            if (equips.Exists(e => e.CurrentCardData.id == "1623"))
            {
                Debug.LogWarning("Mudança de posição bloqueada por Shadow Spell.");
                return;
            }
        }

        string newPos = card.position == CardDisplay.BattlePosition.Attack ? "Defesa" : "Ataque";
        
        System.Action executeChange = () => {
            card.ChangePosition();
            card.hasChangedPositionThisTurn = true;
        };

        if (GameManager.Instance != null && !GameManager.Instance.confirmBattlePositionChange)
        {
            executeChange();
        }
        else
        {
            UIManager.Instance.ShowConfirmation($"Mudar para {newPos}?", executeChange);
        }
    }
}
