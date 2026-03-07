using UnityEngine;
using UnityEngine.UI;

public class TurnClockController : MonoBehaviour
{
    [Header("Componentes")]
    public Image clockFace; // A imagem de fundo (o relógio)
    public Transform clockHand; // O ponteiro que vai girar
    
    [Header("Configuração")]
    [Tooltip("Se verdadeiro, esconde o relógio quando o contador chegar a 0.")]
    public bool hideOnZero = true;

    // Chamado pelo CardDisplay para atualizar o visual
    public void UpdateClock(int currentTurns, int maxTurns)
    {
        if (maxTurns <= 0) return;

        // Garante que o objeto esteja ativo se tiver turnos
        if (!gameObject.activeSelf && currentTurns > 0) gameObject.SetActive(true);

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

        // Opcional: Trocar a cor ou sprite do clockFace dependendo do Ato (lógica futura)
        // if (DuelThemeManager.Instance != null) ...

        if (hideOnZero && currentTurns <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
