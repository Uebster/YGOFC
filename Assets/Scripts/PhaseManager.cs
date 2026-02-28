using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance;

    [Header("Configuração de Fases")]
    public float standbyPhaseDuration = 2.0f;
    public TextMeshProUGUI phaseText;

    public GamePhase currentPhase = GamePhase.Draw;

    [Header("Referências da UI de Fases")]
    public Transform phaseIndicatorContainer; // Arraste o objeto "PhaseIndicator" aqui

    [Header("Visuais dos Botões")]
    public Color phaseActiveColor = new Color(1f, 0.8f, 0f, 1f);
    public Color phaseInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Efeito Neon (Outline)")]
    public bool enablePhaseNeonEffect = true;
    public Color phaseOutlineColor = new Color(1f, 0.8f, 0f, 1f); // Cor do brilho
    public Vector2 phaseOutlineDistance = new Vector2(3, -3); // Espessura/Distância do brilho

    private Dictionary<GamePhase, Button> phaseButtons = new Dictionary<GamePhase, Button>();
    private Dictionary<GamePhase, Image> phaseImages = new Dictionary<GamePhase, Image>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (phaseIndicatorContainer != null)
        {
            string[] phaseNames = { "Draw Phase", "Standby Phase", "Main Phase 1", "Battle Phase", "Main Phase 2", "End Phase" };
            for (int i = 0; i < phaseNames.Length; i++)
            {
                Transform pObj = phaseIndicatorContainer.Find(phaseNames[i]);
                if (pObj != null)
                {
                    GamePhase phaseEnum = (GamePhase)i;
                    Button btn = pObj.GetComponentInChildren<Button>();
                    Image img = pObj.GetComponent<Image>();

                    if (btn != null) { phaseButtons[phaseEnum] = btn; }
                    if (img != null) { phaseImages[phaseEnum] = img; }

                    if (btn != null) btn.onClick.AddListener(() => TryChangePhase(phaseEnum));
                }
            }
        }
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

        if (phaseText != null) phaseText.text = currentPhase.ToString().ToUpper().Replace("1", " 1").Replace("2", " 2");

        UpdatePhaseButtonsUI();

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

    private void UpdatePhaseButtonsUI()
    {
        bool devMode = GameManager.Instance != null && GameManager.Instance.devMode;

        for (int i = 0; i < 6; i++)
        {
            GamePhase phase = (GamePhase)i;
            
            // Atualiza o visual (cor e neon)
            if (phaseImages.ContainsKey(phase))
            {
                Image image = phaseImages[phase];
                bool isCurrent = (phase == currentPhase);
                image.color = isCurrent ? phaseActiveColor : phaseInactiveColor;

                Outline outline = image.GetComponent<Outline>();
                if (enablePhaseNeonEffect)
                {
                    if (outline == null) outline = image.gameObject.AddComponent<Outline>();

                    outline.enabled = isCurrent;
                    outline.effectColor = phaseOutlineColor;
                    outline.effectDistance = phaseOutlineDistance;
                }
                else
                {
                    if (outline != null) outline.enabled = false;
                }
            }

            // Atualiza a interatividade do botão
            if (phaseButtons.ContainsKey(phase))
            {
                Button button = phaseButtons[phase];
                
                // No modo dev, todos os botões são clicáveis.
                if (devMode)
                {
                    button.interactable = true;
                }
                else
                {
                    // No modo normal, só pode avançar. A lógica de qual fase pode ir para qual está em TryChangePhase.
                    button.interactable = (int)phase > (int)currentPhase;
                }
            }
        }
    }
}