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

        if (attacker.hasAttackedThisTurn)
        {
            Debug.LogWarning("Este monstro já atacou neste turno.");
            return;
        }

        // Verifica restrições de ataque (Gravity Bind, etc)
        if (!CanAttack(attacker)) return;

        currentAttacker = attacker;
        Debug.Log($"Ataque declarado por {attacker.CurrentCardData.name}. Selecione um alvo.");
        
        // Aqui você pode ativar um modo de seleção visual (brilho nos alvos válidos)
        // Se o oponente não tiver monstros, pode atacar direto (Direct Attack)
        if (HasDirectAttackCondition())
        {
            UIManager.Instance.ShowConfirmation("Atacar diretamente?", () => CheckTrapsAndAttackDirectly(attacker));
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

        // Para Alligator's Sword Dragon e Amphibious Bugroth MK-3:
        if (attacker.CurrentCardData.id == "0037" || attacker.CurrentCardData.id == "0053") // IDs
        {
            if (AreAllEnemyMonstersEarthWaterOrFire()) return true;
        }

        return true;
    }

    public bool CanAttack(CardDisplay attacker)
    {
        if (GameManager.Instance.IsCardActiveOnField("85742772") && attacker.CurrentCardData.level >= 4)
        {
        // Messenger of Peace (44656491)
        if (GameManager.Instance.IsCardActiveOnField("44656491") && attacker.currentAtk >= 1500)
        {
            Debug.Log("Ataque impedido por Messenger of Peace!");
            return false;
        }

        if (cannotAttackFaceDown && currentTarget != null && currentTarget.isFlipped)
        {
            Debug.Log("Ataque impedido por A Feint Plan: Não pode atacar monstros face-down neste turno.");
            return false;
        }

        // Hook para outros efeitos (Swords of Revealing Light, etc)
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.IsAttackRestricted(attacker))
        {
            return false;
        }

        return true;
    }

    }

    public void SelectTarget(CardDisplay target)
    {
        if (currentAttacker == null) return;
        if (target.isPlayerCard == currentAttacker.isPlayerCard)
        {
            Debug.LogWarning("Não pode atacar seu próprio monstro.");
            return;
        }

        UIManager.Instance.ShowConfirmation($"Atacar {target.CurrentCardData.name}?", () => CheckTrapsAndBattle(currentAttacker, target));
        currentTarget = target;
    }

    private bool HasDirectAttackCondition()
    {
        // Verifica se o oponente não tem monstros
        if (forceDirectAttack) return true;
        if (GameManager.Instance.duelFieldUI == null) return false;
        foreach (Transform zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount > 0) return false;
        }
        return true;
    }

    private void CheckTrapsAndAttackDirectly(CardDisplay attacker)
    {
        // Verifica armadilhas antes do dano (passa callback para continuar se não houver trap)
        SpellTrapManager.Instance.CheckForTraps(SpellTrapTrigger.Attack, attacker, null, () => 
        {
            // Hook OnAttackDeclared (para Kuriboh, etc)
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.OnAttackDeclared(attacker, null, () => PerformDirectAttack(attacker));
            }
            else
            {
                PerformDirectAttack(attacker);
            }
        });
    }

    private void PerformDirectAttack(CardDisplay attacker)
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
        ClearBattleState();
    }

    private void CheckTrapsAndBattle(CardDisplay attacker, CardDisplay target)
    {
        // Revela o alvo se estiver virado para baixo (Flip)
        if (target.isFlipped)
        {
            target.RevealCard(true); // true = triggered by attack
        }

        SpellTrapManager.Instance.CheckForTraps(SpellTrapTrigger.Attack, attacker, target, () =>
        {
            // Hook OnAttackDeclared
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.OnAttackDeclared(attacker, target, () => PerformBattle(attacker, target));
            }
            else
            {
                PerformBattle(attacker, target);
            }
        });
    }

    private void PerformBattle(CardDisplay attacker, CardDisplay target)
    {
        // Hook OnDamageCalculation (Skyscraper, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageCalculation(attacker, target);
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
        if (target.position == CardDisplay.BattlePosition.Attack)
        {
            // Ataque vs Ataque
            if (atk > def)
            {
                int damage = atk - def;
                Debug.Log($"Vitória do Atacante! Oponente toma {damage} de dano. Alvo destruído.");
                GameManager.Instance.DamageOpponent(damage);
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
            }
            else if (atk < def)
            {
                int damage = def - atk;
                Debug.Log($"Vitória do Alvo! Atacante toma {damage} de dano. Atacante destruído.");
                GameManager.Instance.DamagePlayer(damage);
                GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                Destroy(attacker.gameObject);
            }
            else // Empate
            {
                Debug.Log("Empate! Ambos destruídos.");
                GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                GameManager.Instance.SendToGraveyard(attacker.CurrentCardData, attacker.isPlayerCard);
                Destroy(target.gameObject);
                Destroy(attacker.gameObject);
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
                Debug.Log($"Defesa Sólida! Atacante toma {damage} de dano.");
                GameManager.Instance.DamagePlayer(damage);
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);
            }
            else
            {
                Debug.Log("Empate (Defesa). Nada acontece.");
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);
            }
        }

        // Hook OnBattleEnd (D.D. Warrior Lady, Mystic Tomato)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnBattleEnd(attacker, target);
        }

        attacker.hasAttackedThisTurn = true;
        ClearBattleState();
    }

    private void ClearBattleState()
    {
        currentAttacker = null;
        currentTarget = null;
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

        string newPos = card.position == CardDisplay.BattlePosition.Attack ? "Defesa" : "Ataque";
        UIManager.Instance.ShowConfirmation($"Mudar para {newPos}?", () => {
            card.ChangePosition();
            card.hasChangedPositionThisTurn = true;
        });
    }
}
