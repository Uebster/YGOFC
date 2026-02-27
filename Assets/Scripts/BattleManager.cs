using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Estado da Batalha")]
    public CardDisplay currentAttacker;
    public CardDisplay currentTarget;
    public bool isBattlePhase = false;

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

        currentAttacker = attacker;
        Debug.Log($"Ataque declarado por {attacker.currentCardData.name}. Selecione um alvo.");
        
        // Aqui você pode ativar um modo de seleção visual (brilho nos alvos válidos)
        // Se o oponente não tiver monstros, pode atacar direto (Direct Attack)
        if (HasDirectAttackCondition())
        {
            UIManager.Instance.ShowConfirmation("Atacar diretamente?", () => PerformDirectAttack(attacker));
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

        currentTarget = target;
        UIManager.Instance.ShowConfirmation($"Atacar {target.currentCardData.name}?", () => PerformBattle(currentAttacker, currentTarget));
    }

    private bool HasDirectAttackCondition()
    {
        // Verifica se o oponente não tem monstros
        if (GameManager.Instance.duelFieldUI == null) return false;
        foreach (Transform zone in GameManager.Instance.duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount > 0) return false;
        }
        return true;
    }

    private void PerformDirectAttack(CardDisplay attacker)
    {
        // Verifica armadilhas antes do dano
        if (SpellTrapManager.Instance.CheckForTraps(SpellTrapTrigger.Attack, attacker, null))
        {
            // Se uma armadilha for ativada e negar o ataque, pare aqui.
            // Por simplicidade, vamos assumir que o SpellTrapManager lida com a interrupção ou retorna true se algo aconteceu.
            // return; 
        }

        int damage = attacker.currentCardData.atk;
        Debug.Log($"Ataque Direto! Dano: {damage}");
        
        // Aplica dano ao oponente
        // GameManager.Instance.DamageOpponent(damage);
        
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttack(attacker, null, null); // Null target = direct
        
        attacker.hasAttackedThisTurn = true;
        ClearBattleState();
    }

    private void PerformBattle(CardDisplay attacker, CardDisplay target)
    {
        // Revela o alvo se estiver virado para baixo (Flip)
        if (target.isFlipped)
        {
            target.RevealCard(true); // true = triggered by attack
        }

        // Verifica armadilhas
        if (SpellTrapManager.Instance.CheckForTraps(SpellTrapTrigger.Attack, attacker, target))
        {
            // return; // Se interrompido
        }

        int atkPoints = attacker.currentCardData.atk;
        int targetPoints = (target.position == CardDisplay.BattlePosition.Attack) ? target.currentCardData.atk : target.currentCardData.def;

        Debug.Log($"Batalha: {attacker.currentCardData.name} ({atkPoints}) vs {target.currentCardData.name} ({targetPoints}) [{target.position}]");

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
                // GameManager.Instance.DamageOpponent(damage);
                GameManager.Instance.SendToGraveyard(target.currentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
            }
            else if (atk < def)
            {
                int damage = def - atk;
                Debug.Log($"Vitória do Alvo! Atacante toma {damage} de dano. Atacante destruído.");
                // GameManager.Instance.DamagePlayer(damage);
                GameManager.Instance.SendToGraveyard(attacker.currentCardData, attacker.isPlayerCard);
                Destroy(attacker.gameObject);
            }
            else // Empate
            {
                Debug.Log("Empate! Ambos destruídos.");
                GameManager.Instance.SendToGraveyard(target.currentCardData, target.isPlayerCard);
                GameManager.Instance.SendToGraveyard(attacker.currentCardData, attacker.isPlayerCard);
                Destroy(target.gameObject);
                Destroy(attacker.gameObject);
            }
        }
        else // Ataque vs Defesa
        {
            if (atk > def)
            {
                Debug.Log("Vitória do Atacante! Alvo destruído (sem dano).");
                GameManager.Instance.SendToGraveyard(target.currentCardData, target.isPlayerCard);
                Destroy(target.gameObject);
            }
            else if (atk < def)
            {
                int damage = def - atk;
                Debug.Log($"Defesa Sólida! Atacante toma {damage} de dano.");
                // GameManager.Instance.DamagePlayer(damage);
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);
            }
            else
            {
                Debug.Log("Empate (Defesa). Nada acontece.");
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayAttackFail(attacker);
            }
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

        card.ChangePosition();
        card.hasChangedPositionThisTurn = true;
    }
}
