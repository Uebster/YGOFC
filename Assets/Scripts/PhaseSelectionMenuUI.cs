using UnityEngine;
using UnityEngine.UI;

public class PhaseSelectionMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button drawButton;
    public Button standbyButton;
    public Button main1Button;
    public Button battleButton;
    public Button main2Button;
    public Button endButton;

    void Update()
    {
        if (gameObject.activeSelf)
        {
            bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
#endif
            if (escPressed)
            {
                gameObject.SetActive(false);
            }
        }
    }

    void Awake()
    {
        // Adiciona listeners
        drawButton?.onClick.AddListener(() => OnPhaseSelected(GamePhase.Draw));
        standbyButton?.onClick.AddListener(() => OnPhaseSelected(GamePhase.Standby));
        main1Button?.onClick.AddListener(() => OnPhaseSelected(GamePhase.Main1));
        battleButton?.onClick.AddListener(() => OnPhaseSelected(GamePhase.Battle));
        main2Button?.onClick.AddListener(() => OnPhaseSelected(GamePhase.Main2));
        endButton?.onClick.AddListener(() => OnPhaseSelected(GamePhase.End));
        
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (GameManager.Instance == null || PhaseManager.Instance == null) return;

        gameObject.SetActive(true);
        
        // Esconde todos os botões inicialmente
        drawButton?.gameObject.SetActive(false);
        standbyButton?.gameObject.SetActive(false);
        main1Button?.gameObject.SetActive(false);
        battleButton?.gameObject.SetActive(false);
        main2Button?.gameObject.SetActive(false);
        endButton?.gameObject.SetActive(false);

        GamePhase current = PhaseManager.Instance.currentPhase;
        bool devMode = GameManager.Instance.devMode;

        if (devMode)
        {
            drawButton?.gameObject.SetActive(true);
            standbyButton?.gameObject.SetActive(true);
            main1Button?.gameObject.SetActive(true);
            battleButton?.gameObject.SetActive(true);
            main2Button?.gameObject.SetActive(true);
            endButton?.gameObject.SetActive(true);
        }
        else
        {
            // Lógica de fluxo normal do jogo
            if (current == GamePhase.Draw) standbyButton?.gameObject.SetActive(true);
            else if (current == GamePhase.Standby) main1Button?.gameObject.SetActive(true);
            else if (current == GamePhase.Main1)
            {
                battleButton?.gameObject.SetActive(true);
                endButton?.gameObject.SetActive(true);
            }
            else if (current == GamePhase.Battle)
            {
                main2Button?.gameObject.SetActive(true);
                endButton?.gameObject.SetActive(true);
            }
            else if (current == GamePhase.Main2)
            {
                endButton?.gameObject.SetActive(true);
            }
        }
    }

    private void OnPhaseSelected(GamePhase phase)
    {
        PhaseManager.Instance?.TryChangePhase(phase);
        gameObject.SetActive(false);
    }
}
