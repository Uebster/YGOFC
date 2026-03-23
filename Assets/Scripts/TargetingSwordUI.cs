using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TargetingSwordUI : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image img;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        
        // Garante que a espada começa invisível
        if (img != null) img.enabled = false;
        
        // Ignora cliques do mouse para não atrapalhar você a clicar nas cartas!
        if (img != null) img.raycastTarget = false; 
    }

    void Update()
    {
        CardDisplay attacker = null;

        // 1. Se você já clicou no monstro e ele está selecionado para atacar, a espada fica presa nele
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null && CardEffectManager.Instance.luaDuel.currentAttacker != null)
        {
            attacker = CardEffectManager.Instance.luaDuel.currentAttacker.unityCard;
        }
        // 2. Se não clicou ainda, mas o mouse está sobre um monstro SEU em posição de ATAQUE durante a BATTLE PHASE
        // Só exibe a espada a partir do Turno 2 (Nenhum jogador ataca no primeiro turno).
        else if (GameManager.Instance != null && PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle && GameManager.Instance.turnCount > 1)
        {
            if (CardDisplay.HoveredCard != null && CardDisplay.HoveredCard.isOnField && CardDisplay.HoveredCard.isPlayerCard && 
                CardDisplay.HoveredCard.CurrentCardData != null && CardDisplay.HoveredCard.CurrentCardData.type.Contains("Monster") && 
                CardDisplay.HoveredCard.position == CardDisplay.BattlePosition.Attack)
            {
                attacker = CardDisplay.HoveredCard;
            }
        }

        // Se achamos um monstro nas condições acima, acende a espada e mira ela!
        if (attacker != null)
        {
            if (!img.enabled) img.enabled = true;

            // Prende a espada no centro da carta
            rectTransform.position = attacker.transform.position;

            // Mira a ponta (Top Center) para o Cursor do Mouse
            Vector3 mousePos;
#if ENABLE_INPUT_SYSTEM
            mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            mousePos = Input.mousePosition;
#endif
            Vector3 dir = mousePos - rectTransform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
        else
        {
            // Se tirou o mouse ou saiu da Battle Phase, apaga a espada
            if (img != null && img.enabled) img.enabled = false;
        }
    }
}