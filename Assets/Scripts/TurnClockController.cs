using UnityEngine;
using UnityEngine.UI;

public class TurnClockController : MonoBehaviour
{
    [Header("Componentes")]
    public Image clockFace; // A imagem de fundo (o relógio)
    public Transform clockHand; // O ponteiro que vai girar
    public Image clockHandImage; // O sprite do ponteiro para receber o tema
    
    [Header("Configuração")]
    [Tooltip("Se verdadeiro, esconde o relógio quando o contador chegar a 0.")]
    public bool hideOnZero = true;

    // Chamado pelo CardDisplay para atualizar o visual
    public void UpdateClock(int currentTurns, int maxTurns)
    {
        if (maxTurns <= 0) return;

        // Garante que o objeto esteja ativo se tiver turnos
        if (!gameObject.activeSelf && currentTurns > 0) gameObject.SetActive(true);

        // Aplica o tema visual atual ao relógio embutido na carta
        if (DuelThemeManager.Instance != null && DuelThemeManager.Instance.currentTheme != null)
        {
            DuelTheme theme = DuelThemeManager.Instance.currentTheme;
            if (clockFace != null && theme.clockBaseSprite != null) 
                clockFace.sprite = theme.clockBaseSprite;
            if (clockHandImage != null && theme.clockHandSprite != null) 
                clockHandImage.sprite = theme.clockHandSprite;
        }

        // Calcula a rotação
        // Exemplo Swords: Max 3.
        // Turno 3 (Início): 0% progresso -> 0 graus
        // Turno 2: 33% progresso -> 120 graus
        // Turno 1: 66% progresso -> 240 graus
        // Turno 0: 100% progresso -> 360 graus
        
        float progress = 1f - ((float)currentTurns / maxTurns);
        float angle = progress * 360f;

        if (clockHand != null)
        {
            // Rotaciona no eixo Z (negativo para sentido horário)
            clockHand.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        if (hideOnZero && currentTurns <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
