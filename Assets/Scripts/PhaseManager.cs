using UnityEngine;
using TMPro;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance;

    [Header("Configuração")]
    public float standbyPhaseDuration = 2.0f;
    public TextMeshProUGUI phaseText;

    public GamePhase currentPhase = GamePhase.Draw;

    void Awake()
    {
        Instance = this;
    }

    public void StartTurn()
    {
        if (SummonManager.Instance != null) SummonManager.Instance.ResetTurnStats();
        if (SpellTrapManager.Instance != null) SpellTrapManager.Instance.ResetTurnStats();
        ChangePhase(GamePhase.Draw);
    }

    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"--- FASE: {currentPhase} ---");

        if (phaseText != null) phaseText.text = currentPhase.ToString().ToUpper();
        
        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            GameManager.Instance.duelFieldUI.UpdatePhaseHighlight(currentPhase);
        }

        switch (currentPhase)
        {
            case GamePhase.Draw:
                // Lógica de Draw Phase delegada ao GameManager ou executada aqui
                if (GameManager.Instance != null)
                {
                    // Verifica se deve pular o Draw (ex: Offerings to the Doomed)
                    if (SpellTrapManager.Instance != null && SpellTrapManager.Instance.skipDrawPhase)
                    {
                        SpellTrapManager.Instance.ConsumeSkipDraw();
                        Debug.Log("Draw Phase pulada devido a efeito de carta.");
                        ChangePhase(GamePhase.Standby); // Pula direto
                    }
                    else
                    {
                        GameManager.Instance.OnDrawPhaseStart();
                    }
                }
                break;
            case GamePhase.Standby:
                StartCoroutine(HandleStandbyPhase());
                break;
            case GamePhase.Main1:
                // Habilita interações
                break;
            case GamePhase.Battle:
                // Habilita batalha
                break;
            case GamePhase.Main2:
                // Habilita interações novamente (se possível)
                break;
            // ...
        }
    }

    IEnumerator HandleStandbyPhase()
    {
        yield return new WaitForSeconds(standbyPhaseDuration);
        ChangePhase(GamePhase.Main1);
    }

    public void TryChangePhase(GamePhase newPhase)
    {
        bool devMode = GameManager.Instance != null && GameManager.Instance.devMode;
        
        if (!devMode)
        {
            if ((int)newPhase <= (int)currentPhase)
            {
                Debug.LogWarning("Não é possível voltar para uma fase anterior neste turno.");
                return;
            }
        }
        
        // Validação de fluxo (Draw -> Standby -> Main1 -> Battle -> Main2 -> End)
        // Impede pular fases obrigatórias se não for devMode (embora Standby seja automática)
        if (!devMode && newPhase == GamePhase.Battle && currentPhase == GamePhase.Draw)
        {
             // Exemplo: Não pode ir direto pra Battle do Draw
             return;
        }

        ChangePhase(newPhase);
    }
}