using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance;

    [Header("Configuração de Fases")]
    public float standbyPhaseDuration = 0.2f;
    public TextMeshProUGUI phaseText;

    public GamePhase currentPhase = GamePhase.Draw;

    [Header("Referências da UI de Fases")]
    public Transform phaseIndicatorContainer; // Arraste o objeto "PhaseIndicator" aqui

    [Header("Visuais dos Botões")]
    public Color phaseActiveColor = new Color(1f, 0.8f, 0f, 1f);
    public Color phaseInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
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
                    if (img != null) 
                    { 
                        phaseImages[phaseEnum] = img; 
                        // Garante que o botão de fase tenha o script MillenniumButton para o hover funcionar
                        if (btn.GetComponent<MillenniumButton>() == null) btn.gameObject.AddComponent<MillenniumButton>();
                    }

                    // FIX: Desativa a transição padrão do botão para evitar que ele fique cinza escuro quando não interativo
                    // Isso permite que nossa lógica de cores (phaseInactiveColor) prevaleça.
                    if (btn != null) btn.transition = Selectable.Transition.None;

                    if (btn != null) btn.onClick.AddListener(() => TryChangePhase(phaseEnum));
                }
            }
        }

        // FIX: Inicializa o visual dos botões imediatamente para esconder os inativos (evita botões brancos no início)
        UpdatePhaseButtonsUI();
        // Define a cor de hover inicial (Player começa, então verde)
        UpdateHoverColors(true);
    }

    public void StartTurn()
    {
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
                if (GameManager.Instance != null)
                {
                    if (CardEffectManager.Instance != null)
                    {
                        CardEffectManager.Instance.OnPreDrawPhase(GameManager.Instance.isPlayerTurn, () => {
                            GameManager.Instance.OnDrawPhaseStart();
                        });
                    }
                    else
                    {
                        GameManager.Instance.OnDrawPhaseStart();
                    }
                }
                break;
            case GamePhase.Standby:
                if (CardEffectManager.Instance != null)
                {
                    CardEffectManager.Instance.OnPhaseStart(GamePhase.Standby);
                }
                StartCoroutine(HandleStandbyPhase());
                break;
            case GamePhase.Main1:
                break;
            case GamePhase.Battle:
                break;
            case GamePhase.Main2:
                break;
            case GamePhase.End:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnEndPhaseStart();
                }
                break;
        }
    }

    IEnumerator HandleStandbyPhase()
    {
        yield return new WaitForSeconds(standbyPhaseDuration);
        TryChangePhase(GamePhase.Main1);
    }

    public void TryChangePhase(GamePhase newPhase)
    {
        // TODO LUA: Substituir a chamada direta pela emissão de um Request para o motor Lua validar se a fase pode ser alterada.
        ChangePhase(newPhase);
    }

    public void UpdateHoverColors(bool isPlayerTurn)
    {
        if (GameManager.Instance == null) return;

        // Usa as cores específicas de fase se definidas, senão usa as genéricas, respeitando o switch
        Color hoverColor = isPlayerTurn ? GameManager.Instance.phaseHoverColorPlayer : GameManager.Instance.phaseHoverColorOpponent;
        bool enableHover = GameManager.Instance.enablePhaseHoverEffect;

        foreach (var button in phaseButtons.Values)
        {
            MillenniumButton mb = button.GetComponent<MillenniumButton>();
            if (mb != null)
            {
                // Se o efeito estiver ativado, aplica a cor. Se não, mantém a cor normal (sem efeito visual de hover)
                // Nota: MillenniumButton usa bgHoverColor para o fundo.
                mb.useColorTint = enableHover; 
                if (enableHover) mb.bgHoverColor = hoverColor;
                // O texto pode manter o hover padrão ou seguir a cor, aqui deixamos opcional
            }
        }
    }

    private void UpdatePhaseButtonsUI()
    {
        bool devMode = GameManager.Instance != null && GameManager.Instance.devMode;
        
        // Define a cor ativa baseada no turno (semitransparente)
        Color activeColor = Color.white;
        if (GameManager.Instance != null)
        {
            activeColor = GameManager.Instance.isPlayerTurn ? GameManager.Instance.phaseHoverColorPlayer : GameManager.Instance.phaseHoverColorOpponent;
        }

        for (int i = 0; i < 6; i++)
        {
            GamePhase phase = (GamePhase)i;
            
            // Atualiza o visual (cor e neon)
            if (phaseImages.ContainsKey(phase))
            {
                Image image = phaseImages[phase];
                bool isCurrent = (phase == currentPhase);
                
                // FIX: Botões inativos ficam totalmente transparentes. Ativo usa a cor do tema.
                image.color = isCurrent ? activeColor : Color.clear;
                
                // FIX CRÍTICO: Atualiza o MillenniumButton para que ele saiba qual é a "cor normal" deste estado.
                // Sem isso, o MillenniumButton reseta para Branco (Sólido) ao passar o mouse ou habilitar o objeto.
                MillenniumButton mb = image.GetComponent<MillenniumButton>();
                if (mb == null && phaseButtons.ContainsKey(phase)) 
                    mb = phaseButtons[phase].GetComponent<MillenniumButton>();

                if (mb != null)
                {
                    mb.bgNormalColor = isCurrent ? activeColor : Color.clear;
                    
                    // FIX: Força a atualização visual imediata se o mouse não estiver sobre o botão
                    // Isso resolve o problema de fases anteriores ficarem "acesas"
                    if (image != null)
                    {
                        image.color = mb.bgNormalColor;
                    }

                    // FIX: Atualiza também a imagem do próprio botão (filho), pois o MillenniumButton
                    // pode estar segurando a cor antiga.
                    Image btnImage = mb.GetComponent<Image>();
                    if (btnImage != null && btnImage != image)
                    {
                        btnImage.color = mb.bgNormalColor;
                    }
                }

                // O efeito de brilho agora usa a cor de hover do turno atual
                Outline outline = image.GetComponent<Outline>();
                if (outline == null) outline = image.gameObject.AddComponent<Outline>();

                outline.enabled = isCurrent;
                outline.effectColor = activeColor;
                outline.effectDistance = new Vector2(3, -3);
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
                    // No modo normal, só pode avançar E se for turno do jogador
                    bool isMyTurn = GameManager.Instance != null && GameManager.Instance.isPlayerTurn;
                    button.interactable = isMyTurn && ((int)phase > (int)currentPhase);
                }
            }
        }
    }
}
