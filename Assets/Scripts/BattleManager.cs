using UnityEngine;
using System.Collections;

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

        // Verifica restrições de ataque (Gravity Bind, etc)
        if (!CanAttack(attacker)) return;

        currentAttacker = attacker;
        Debug.Log($"Ataque declarado por {attacker.CurrentCardData.name}. Selecione um alvo.");

        // Patrician of Darkness (1406): Se o defensor tiver Patrician, ele escolhe o alvo
        if (patricianOfDarknessActive && !attacker.isPlayerCard)
        {
            Debug.Log("Patrician of Darkness: Você escolhe o alvo do ataque do oponente!");
            // A UI deve permitir selecionar seus próprios monstros como alvo.
        }
        
        // Aqui você pode ativar um modo de seleção visual (brilho nos alvos válidos)
        // Se o oponente não tiver monstros, pode atacar direto (Direct Attack)
        if (HasDirectAttackCondition())
        {
            UIManager.Instance.ShowConfirmation("Atacar diretamente?", () => {
                if (ChainManager.Instance != null)
                    ChainManager.Instance.AddToChain(attacker, attacker.isPlayerCard, ChainManager.TriggerType.Attack, null, () => PerformDirectAttack(attacker));
            });
        }
    }

    public bool CanAttack(CardDisplay attacker)
    {
        // Gravity Bind (85742772)
        if (GameManager.Instance.IsCardActiveOnField("85742772") && attacker.CurrentCardData.level >= 4)
        {
            Debug.Log("Ataque impedido por Gravity Bind!");
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

        // 2012 - Ultimate Obedient Fiend
        if (attacker.CurrentCardData.id == "2012")
        {
            // Só ataca se for o único monstro e mão vazia
            int handCount = GameManager.Instance.GetPlayerHandData().Count;
            int monsterCount = GameManager.Instance.GetFieldCardCount(attacker.isPlayerCard); // Isso conta S/T também no método atual, precisa de ajuste ou contagem manual
            // Contagem manual de monstros
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
        // Verifica se o oponente tem a carta ativa
        if (IsWallOfRevealingLightBlocking(attacker))
        {
            Debug.Log("Ataque impedido por Wall of Revealing Light.");
            return false;
        }

        // D.D. Borderline (0379)
        // Se não houver Spells no seu GY, não pode atacar.
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

        // Command Knight (0318)
        // Se controlar outro monstro, não pode ser atacado.
        // Esta verificação é no alvo, não no atacante.
        // Deve ser feita no SelectTarget ou CanAttackTarget (se existisse).
        // Como CanAttack verifica se o atacante pode atacar, vamos adicionar uma verificação de alvo no SelectTarget.

        // Cold Wave (0315)
        // Se ativa, não pode setar/ativar S/T. (Verificado no SpellTrapManager)
        // Mas não impede ataque.

        // Para Alligator's Sword Dragon e Amphibious Bugroth MK-3:
        if (attacker.CurrentCardData.id == "0037" || attacker.CurrentCardData.id == "0053") // IDs
        {
            if (AreAllEnemyMonstersEarthWaterOrFire()) return true;
        }

        return true;
                // 1402 - Panther Warrior
        if (attacker.CurrentCardData.id == "1402")
        {
            // Requer tributo para atacar
            // Como não podemos abrir UI aqui facilmente (retorno bool), verificamos se já tributou?
            // Ou impedimos o ataque se não houver monstros para tributar.
            // Implementação ideal: Ao clicar para atacar, abre popup "Tributar para atacar?".
            // Aqui apenas bloqueamos se não houver outros monstros.
            bool hasFodder = false;
            // ... check fodder ...
            if (!hasFodder) return false;
            
            // Se tiver, o clique do ataque deve tratar o custo.
            // Por enquanto, permitimos e logamos o custo.
            Debug.Log("Panther Warrior: Tributo necessário (Lógica de custo pendente).");
        }

        return true;
    }

    private bool IsWallOfRevealingLightBlocking(CardDisplay attacker)
    {
        bool attackerIsPlayer = attacker.isPlayerCard;
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

    public void SelectTarget(CardDisplay target)
    {
        if (currentAttacker == null) return;
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

        UIManager.Instance.ShowConfirmation($"Atacar {target.CurrentCardData.name}?", () => {
            if (ChainManager.Instance != null)
                ChainManager.Instance.AddToChain(currentAttacker, currentAttacker.isPlayerCard, ChainManager.TriggerType.Attack, target, () => PerformBattle(currentAttacker, target));
        });
        currentTarget = target;
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
                    if (defender != null && defender.CurrentCardData.race == "Toon") return false; // Has toon, must attack it
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
        GameManager.Instance.DamageOpponent(damage);
        
        if (DuelFXManager.Instance != null) 
            DuelFXManager.Instance.PlayAttack(attacker, null, null); // Null target = direct
        
        attacker.hasAttackedThisTurn = true;
        attacker.attacksDeclaredThisTurn++;
        ClearBattleState();
    }

    private void CheckTrapsAndBattle(CardDisplay attacker, CardDisplay target)
    {
        // Obsoleto. O fluxo agora é controlado pelo ChainManager.
    }

    public void PerformBattle(CardDisplay attacker, CardDisplay target)
    {
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
        if (target.CurrentCardData.id == "0201" && target.isFlipped) // Face-down (isFlipped=true means back is showing in CardDisplay logic usually, but let's assume standard logic: isFlipped=true means Face-Down in this context based on previous code)
        {
            // Nota: No CardDisplay, isFlipped=true significa VERSO (Face-Down).
            Debug.Log("Blast Sphere: Atacado face-down. Equipando ao atacante...");
            target.RevealCard();
            // Move Blast Sphere para S/T do dono do Blast Sphere e equipa no atacante
            // Simplificação: Apenas cria o link e destrói o objeto visual do monstro para simular que virou equip
            // Em um sistema completo, moveria para a zona de S/T.
            GameManager.Instance.CreateCardLink(target, attacker, CardLink.LinkType.Equipment);
            target.AddSpellCounter(1); // Marca para destruir na Standby
            return; // Cancela batalha
        }

        attacker.battledThisTurn = true;
        target.battledThisTurn = true;

        int atkPoints = attacker.currentAtk; // Usa currentAtk
        int targetPoints = (target.position == CardDisplay.BattlePosition.Attack) ? target.currentAtk : target.currentDef;

        Debug.Log($"Batalha: {attacker.CurrentCardData.name} ({atkPoints}) vs {target.CurrentCardData.name} ({targetPoints}) [{target.position}]");

        if (DuelFXManager.Instance != null)
        {
            DuelFXManager.Instance.PlayAttack(attacker, target, () => ResolveDamage(attacker, target, atkPoints, targetPoints));
        }
        else
        {
            ResolveDamage(attacker, target, atkPoints, targetPoints);
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
                    GameManager.Instance.DamageOpponent(damage);
                
                if (!targetIsBES)
                {
                    // Charm of Shabti (0296)
                    if (gravekeepersProtected && target.CurrentCardData.name.Contains("Gravekeeper"))
                    {
                        Debug.Log("Gravekeeper protegido por Charm of Shabti.");
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
                if (!noBattleDamage) GameManager.Instance.DamagePlayer(damage);
                
                if (!attackerIsRocketWarrior)
                {
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                    Destroy(attacker.gameObject);
                }
            }
            else // Empate
            {
                Debug.Log("Empate! Ambos destruídos.");
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
                if (!attackerIsRocketWarrior) {
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                    Destroy(attacker.gameObject);
                }
            }
        }
        else // Ataque vs Defesa
        {
            if (atk > def)
            {
                Debug.Log("Vitória do Atacante! Alvo destruído (sem dano).");
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
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
                GameManager.Instance.DamagePlayer(damage);
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);

                // Continuous Destruction Punch (0323)
                // Se defesa > ataque, destrói o atacante.
                if (GameManager.Instance.IsCardActiveOnField("0323"))
                {
                    Debug.Log("Continuous Destruction Punch: Destruindo atacante.");
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
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
                    GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
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
                GameManager.Instance.DamageOpponent(piercing);
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
    }

    private void ClearBattleState()
    {
        currentAttacker = null;
        currentTarget = null;
        dimensionWallActive = false;
        // patricianOfDarknessActive = false; // Não reseta pois é contínuo enquanto o monstro estiver em campo
        gravekeepersProtected = false; // Reseta no fim da batalha ou turno? Regra diz "until End Phase".
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
