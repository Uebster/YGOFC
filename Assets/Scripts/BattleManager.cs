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
        gravekeepersProtected = false;
        isBattleResolving = false;
    }

    // Prepara o monstro para atacar (Seleção)
    public void PrepareAttack(CardDisplay attacker)
    {
        if (PhaseManager.Instance.currentPhase != GamePhase.Battle) return;
        if (attacker.position == CardDisplay.BattlePosition.Defense) return;

        try
        {
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

        if (attacker.attacksDeclaredThisTurn >= maxAttacks || attacker.hasAttackedThisTurn) // Mantendo hasAttackedThisTurn para compatibilidade
        {
            Debug.LogWarning("Este monstro já atacou neste turno.");
            return;
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

        if (HasDirectAttackCondition())
        {
             // Realiza o ataque direto
             if (ChainManager.Instance != null)
             {
                ChainManager.Instance.AddToChain(currentAttacker, currentAttacker.isPlayerCard, ChainManager.TriggerType.Attack, null, () => PerformDirectAttack(currentAttacker));
             }
        }
        else
        {
            Debug.Log("Não pode atacar diretamente (Oponente tem monstros ou efeito impede).");
        }
    }

    public bool CanAttack(CardDisplay attacker)
    {
        if (CardEffectManager.Instance != null)
        {
            return CardEffectManager.Instance.CanDeclareAttack(attacker);
        }
        return true;
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

        // Exceção: Patrician of Darkness permite selecionar seus próprios monstros como alvo do ataque do oponente
        if (patricianOfDarknessActive && !currentAttacker.isPlayerCard && target.isPlayerCard)
        {
            // Permite
            Debug.Log("Patrician of Darkness: Alvo redirecionado.");
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

        UIManager.Instance.ShowConfirmation($"Atacar {target.CurrentCardData.name}?", () => {
            // FIX: Adicionado fallback. Se ChainManager não existir, ataca direto.
            if (ChainManager.Instance != null)
            {
                ChainManager.Instance.AddToChain(attacker, attacker.isPlayerCard, ChainManager.TriggerType.Attack, targetCard, () => PerformBattle(attacker, targetCard));
            }
            else
            {
                PerformBattle(attacker, targetCard);
            }
        });
        currentTarget = target;
        // Limpa a seleção visual após confirmar o alvo
        if (currentAttacker != null) 
        {
            currentAttacker.SetAttackSelectionVisual(false);
        }
    }

    private bool HasDirectAttackCondition()
    {
        // Verifica se o oponente não tem monstros
        if (forceDirectAttack) return true;
        if (GameManager.Instance.duelFieldUI == null) return false;
        foreach (Transform zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount > 0)
            {
                // Toon Logic: Can attack direct if opponent has no Toons
                if (currentAttacker != null && (currentAttacker.CurrentCardData.race == "Toon" || currentAttacker.CurrentCardData.id == "0215"))
                {
                    CardDisplay defender = zone.GetChild(0).GetComponent<CardDisplay>();
                    // Só considera monstros Toon Face-Up como bloqueadores
                    if (defender != null && !defender.isFlipped && defender.CurrentCardData.race == "Toon") return false; // Has toon, must attack it
                    continue; // Not a toon, ignore for direct attack condition
                }
                return false;
            }
        }

        // 1553 - Rocket Jumper
        if (currentAttacker != null && currentAttacker.CurrentCardData.id == "1553")
        {
            // Pode atacar direto se o oponente só tiver monstros em Defesa
            bool onlyDefense = true;
            bool hasMonsters = false;
            foreach (Transform zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
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

    private void CheckTrapsAndAttackDirectly(CardDisplay attacker)
    {
        // Obsoleto. O fluxo agora é controlado pelo ChainManager.
    }

    public void PerformDirectAttack(CardDisplay attacker)
    {
        // Hook OnDamageCalculation (para Injection Fairy Lily, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageCalculation(attacker, null);
        }

        int damage = attacker.currentAtk; // Usa currentAtk (pode ter mudado no hook acima)
        Debug.Log($"Ataque Direto! Dano: {damage}");
        
        // Aplica dano ao oponente
        // FIX: Verifica quem está atacando para causar dano no alvo correto
        if (attacker.isPlayerCard)
            GameManager.Instance.DamageOpponent(damage);
        else
            GameManager.Instance.DamagePlayer(damage);
        
        if (DuelFXManager.Instance != null) 
            DuelFXManager.Instance.PlayAttack(attacker, null, null); // Null target = direct
        
        attacker.hasAttackedThisTurn = true;
        attacker.attacksDeclaredThisTurn++;
        ClearBattleState();
        if (attacker != null) attacker.SetAttackSelectionVisual(false); // Garante limpeza visual
    }

    private void CheckTrapsAndBattle(CardDisplay attacker, CardDisplay target)
    {
        // Obsoleto. O fluxo agora é controlado pelo ChainManager.
    }

    public void PerformBattle(CardDisplay attacker, CardDisplay target)
    {
        // FIX: Verifica se os monstros ainda existem e estão no campo (podem ter sido destruídos durante a Chain)
        if (attacker == null || !attacker.isOnField || target == null || !target.isOnField)
        {
            Debug.LogWarning("Batalha cancelada: Atacante ou Alvo não estão mais no campo.");
            isBattleResolving = false;
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

        // Hook OnDamageCalculation (Skyscraper, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageCalculation(attacker, target);
        }

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
                ResolveDamage(attacker, target, atkPoints, targetPoints);
                isBattleResolving = false;
            });
        }
        else
        {
            ResolveDamage(attacker, target, atkPoints, targetPoints);
            isBattleResolving = false;
        }
    }

    private void ResolveDamage(CardDisplay attacker, CardDisplay target, int atk, int def)
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
            // Não é destruído, mas o dano ainda é calculado
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

        if (target.position == CardDisplay.BattlePosition.Attack)
        {
            // Ataque vs Ataque
            if (atk > def)
            {
                int damage = atk - def;
                Debug.Log($"Vitória do Atacante! Oponente toma {damage} de dano. Alvo destruído.");
                
                if (!noBattleDamage && !attacker.cannotInflictBattleDamage)
                {
                    if (target.isPlayerCard) GameManager.Instance.DamagePlayer(damage);
                    else GameManager.Instance.DamageOpponent(damage);
                }
                
                if (!targetIsBES)
                {
                    // Charm of Shabti (0296) / Waboku (2047)
                    if ((gravekeepersProtected && target.CurrentCardData.name.Contains("Gravekeeper")) || wabokuActive)
                    {
                        Debug.Log("Monstro protegido (Charm of Shabti ou Waboku).");
                    }
                    else
                    {
                        GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                        Destroy(target.gameObject);
                    }
                }
            }
            else if (atk < def)
            {
                int damage = def - atk;
                Debug.Log($"Vitória do Alvo! Atacante toma {damage} de dano. Atacante destruído.");
                if (!noBattleDamage)
                {
                    if (attacker.isPlayerCard) GameManager.Instance.DamagePlayer(damage);
                    else GameManager.Instance.DamageOpponent(damage);
                }  

                if (!attackerIsRocketWarrior)
                {
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                    Destroy(attacker.gameObject);
                }
            }
            else // Empate
            {
                Debug.Log("Empate! Ambos destruídos.");
                if (!wabokuActive)
                {
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard, CardLocation.Field, SendReason.Battle);
                    Destroy(target.gameObject);
                }
                if (!attackerIsRocketWarrior && !wabokuActive) {
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard, CardLocation.Field, SendReason.Battle);
                    Destroy(attacker.gameObject);
                }
            }
        }
        else // Ataque vs Defesa
        {
            if (atk > def)
            {
                Debug.Log("Vitória do Atacante! Alvo destruído (sem dano).");
                if (!wabokuActive)
                {
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard, CardLocation.Field, SendReason.Battle);
                    Destroy(target.gameObject);
                }
            }
            else if (atk < def)
            {
                int damage = def - atk;
                
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

                if (attacker.isPlayerCard) GameManager.Instance.DamagePlayer(damage);
                else GameManager.Instance.DamageOpponent(damage);

                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);

                // Continuous Destruction Punch (0323)
                // Se defesa > ataque, destrói o atacante.
                if (GameManager.Instance.IsCardActiveOnField("0323"))
                {
                    Debug.Log("Continuous Destruction Punch: Destruindo atacante.");
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard, CardLocation.Field, SendReason.Effect);
                    Destroy(attacker.gameObject);
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
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard, CardLocation.Field, SendReason.Effect);
                    Destroy(attacker.gameObject);
                }
            }
            else
            {
                Debug.Log("Empate (Defesa). Nada acontece.");
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);
            }

            // Piercing Damage (Dark Driceratops 0407, etc)
            // Adicionado Meteorain (globalPiercing)
            if (atk > def && (attacker.CurrentCardData.id == "0407" || attacker.CurrentCardData.id == "0059" || globalPiercing)) 
            {
                int piercing = atk - def;
                Debug.Log($"Dano Perfurante! {piercing} de dano.");
                if (target.isPlayerCard) GameManager.Instance.DamagePlayer(piercing);
                else GameManager.Instance.DamageOpponent(piercing);            
            }

            // Different Dimension Dragon (0492)
            // Não pode ser destruído por batalha com monstro de ATK <= 1900.
            // A lógica de destruição acima (Destroy(target.gameObject)) precisa ser condicional.
            // Como o código acima já executou, isso é um problema de arquitetura do método ResolveDamage.
            // Idealmente, deveríamos calcular "willDestroy" antes de aplicar.
            // Para este protótipo, vamos assumir que DDD tem um efeito que previne a destruição no OnCardLeavesField ou similar,
            // ou injetar a lógica antes do Destroy.
            // (Devido à complexidade de reescrever o ResolveDamage inteiro, deixaremos como nota:
            //  DDD requer verificação de ATK do atacante antes de aplicar Destroy).
        }

        // Hook OnBattleEnd (D.D. Warrior Lady, Mystic Tomato)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnBattleEnd(attacker, target);
        }

        attacker.hasAttackedThisTurn = true;
        attacker.attacksDeclaredThisTurn++;
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

        string newPos = card.position == CardDisplay.BattlePosition.Attack ? "Defesa" : "Ataque";
        UIManager.Instance.ShowConfirmation($"Mudar para {newPos}?", () => {
            card.ChangePosition();
            card.hasChangedPositionThisTurn = true;
        });
    }
}
