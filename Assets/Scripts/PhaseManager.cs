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
    
    [Header("Controle de Pulo de Fase")]
    // Flags para o turno ATUAL
    public bool skipDrawPhase = false;
    public bool skipStandbyPhase = false;
    public bool skipMain1Phase = false;
    public bool skipBattlePhase = false;

    // Flags para o PRÓXIMO turno (armazenamento)
    public bool playerSkipNextDraw = false;
    public bool opponentSkipNextDraw = false;
    public bool playerSkipNextStandby = false;
    public bool opponentSkipNextStandby = false;
    public bool playerSkipNextMain1 = false;
    public bool opponentSkipNextMain1 = false;
    public bool playerSkipNextBattle = false;
    public bool opponentSkipNextBattle = false;

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

                    if (btn != null) btn.onClick.AddListener(() => TryChangePhase(phaseEnum));
                }
            }
        }
    }

    public void StartTurn()
    {
        if (SummonManager.Instance != null) SummonManager.Instance.ResetTurnStats();
        if (SpellTrapManager.Instance != null) SpellTrapManager.Instance.ResetTurnStats();
        if (BattleManager.Instance != null) BattleManager.Instance.ResetTurnStats();
        
        // Configura os pulos de fase para este turno
        bool isPlayer = true;
        if (GameManager.Instance != null) isPlayer = GameManager.Instance.isPlayerTurn;

        // Reseta flags do turno atual
        skipDrawPhase = false;
        skipStandbyPhase = false;
        skipMain1Phase = false;
        skipBattlePhase = false;

        // Aplica flags pendentes e as consome
        if (isPlayer)
        {
            if (playerSkipNextDraw) { skipDrawPhase = true; playerSkipNextDraw = false; }
            if (playerSkipNextStandby) { skipStandbyPhase = true; playerSkipNextStandby = false; }
            if (playerSkipNextMain1) { skipMain1Phase = true; playerSkipNextMain1 = false; }
            if (playerSkipNextBattle) { skipBattlePhase = true; playerSkipNextBattle = false; }
        }
        else
        {
            if (opponentSkipNextDraw) { skipDrawPhase = true; opponentSkipNextDraw = false; }
            if (opponentSkipNextStandby) { skipStandbyPhase = true; opponentSkipNextStandby = false; }
            if (opponentSkipNextMain1) { skipMain1Phase = true; opponentSkipNextMain1 = false; }
            if (opponentSkipNextBattle) { skipBattlePhase = true; opponentSkipNextBattle = false; }
        }

        if (skipDrawPhase) Debug.Log("PhaseManager: Draw Phase será pulada.");
        if (skipStandbyPhase) Debug.Log("PhaseManager: Standby Phase será pulada.");
        if (skipMain1Phase) Debug.Log("PhaseManager: Main Phase 1 será pulada.");
        if (skipBattlePhase) Debug.Log("PhaseManager: Battle Phase será pulada.");

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
                    // Verifica se deve pular o Draw (ex: Time Seal, Offerings to the Doomed)
                    if (skipDrawPhase || (SpellTrapManager.Instance != null && SpellTrapManager.Instance.skipDrawPhase))
                    {
                        if (SpellTrapManager.Instance != null) SpellTrapManager.Instance.ConsumeSkipDraw();
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
                // Notifica o CardEffectManager sobre o início da Standby Phase
                if (CardEffectManager.Instance != null)
                {
                    CardEffectManager.Instance.OnPhaseStart(GamePhase.Standby);
                }
                break;
            case GamePhase.Main1:
                if (skipMain1Phase)
                {
                    Debug.Log("Main Phase 1 pulada.");
                    ChangePhase(GamePhase.Battle);
                    return;
                }
                // Habilita interações
                break;
            case GamePhase.Battle:
                if (skipBattlePhase)
                {
                    Debug.Log("Battle Phase pulada.");
                    ChangePhase(GamePhase.End); // Se pular Battle, geralmente vai para End (ou Main 2 se permitido, mas End é mais seguro)
                    return;
                }
                // Habilita batalha
                break;
            case GamePhase.Main2:
                // Habilita interações novamente (se possível)
                break;
            case GamePhase.End:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnEndPhaseStart();
                    // Se for turno do jogador, troca o turno automaticamente após processar a End Phase
                    if (GameManager.Instance.isPlayerTurn)
                    {
                        StartCoroutine(AutoSwitchTurn());
                    }
                }
                break;
        }
    }

    // Método para registrar um pulo de fase para o PRÓXIMO turno de um jogador
    public void RegisterSkipNextPhase(bool targetPlayerIsHuman, GamePhase phase)
    {
        if (targetPlayerIsHuman)
        {
            if (phase == GamePhase.Draw) playerSkipNextDraw = true;
            else if (phase == GamePhase.Standby) playerSkipNextStandby = true;
            else if (phase == GamePhase.Main1) playerSkipNextMain1 = true;
            else if (phase == GamePhase.Battle) playerSkipNextBattle = true;
        }
        else
        {
            if (phase == GamePhase.Draw) opponentSkipNextDraw = true;
            else if (phase == GamePhase.Standby) opponentSkipNextStandby = true;
            else if (phase == GamePhase.Main1) opponentSkipNextMain1 = true;
            else if (phase == GamePhase.Battle) opponentSkipNextBattle = true;
        }
        Debug.Log($"PhaseManager: Agendado pulo de {phase} para o próximo turno de {(targetPlayerIsHuman ? "Player" : "Oponente")}.");
    }

    IEnumerator HandleStandbyPhase()
    {
        yield return new WaitForSeconds(standbyPhaseDuration);
        ChangePhase(GamePhase.Main1);
    }

    IEnumerator AutoSwitchTurn()
    {
        yield return new WaitForSeconds(1.5f); // Pequeno delay para feedback visual
        if (GameManager.Instance != null) GameManager.Instance.SwitchTurn();
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

    public void UpdateHoverColors(bool isPlayerTurn)
    {
        if (GameManager.Instance == null) return;

        Color hoverColor = isPlayerTurn ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor;

        foreach (var button in phaseButtons.Values)
        {
            MillenniumButton mb = button.GetComponent<MillenniumButton>();
            if (mb != null)
            {
                mb.textHoverColor = hoverColor;
            }
        }
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

                // O efeito de brilho agora usa a cor de hover do turno atual
                Outline outline = image.GetComponent<Outline>();
                if (outline == null) outline = image.gameObject.AddComponent<Outline>();

                outline.enabled = isCurrent;
                outline.effectColor = GameManager.Instance.isPlayerTurn ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor;
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
                    // No modo normal, só pode avançar. A lógica de qual fase pode ir para qual está em TryChangePhase.
                    button.interactable = (int)phase > (int)currentPhase;
                }
            }
        }
    }
}