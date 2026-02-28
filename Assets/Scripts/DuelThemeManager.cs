using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DuelThemeManager : MonoBehaviour
{
    public static DuelThemeManager Instance;

    [Header("--- REFERÊNCIAS DA CENA ---")]
    
    [Header("1-5: Main Board")]
    public Image panelCardViewer;
    public Image panelDescription;
    public Image handleDescription;
    public Image boardBackground;
    public Image fieldImg;

    [Header("6-8: HUD")]
    public Image phaseIndicator;
    public Image playerProfile;
    public Image opponentProfile;

    [Header("9-14: Viewers")]
    public Image graveyardViewerPanel;
    public Image handleGraveyard;
    public Image closeGraveyard;
    public Image extraDeckViewerPanel;
    public Image handleExtraDeck;
    public Image closeExtraDeck;

    [Header("15-19: Action Menu")]
    public Image panelActionMenu;
    public Image btnSummon;
    public Image btnSet;
    public Image btnActivate;
    public Image btnCancel;

    [Header("20-22: Confirmation")]
    public Image panelConfirmation;
    public Image btnYes;
    public Image btnNo;

    [Header("Text Containers")]
    // Arraste o objeto pai (Canvas ou Panel_Duel) para buscar todos os textos automaticamente
    public Transform uiRootForTexts; 

    void Awake()
    {
        Instance = this;
    }

    public void ApplyTheme(DuelTheme theme)
    {
        if (theme == null) return;

        Debug.Log($"Aplicando tema: {theme.name}");

        // 1. Aplica Sprites (Verifica null para não quebrar se o tema não tiver tudo)
        SetSprite(panelCardViewer, theme.panelCardViewer);
        SetSprite(panelDescription, theme.panelDescription);
        SetSprite(handleDescription, theme.handleDescription);
        SetSprite(boardBackground, theme.boardBackground);
        SetSprite(fieldImg, theme.fieldImage);
        
        SetSprite(phaseIndicator, theme.phaseIndicatorBg);
        SetSprite(playerProfile, theme.playerProfileBg);
        SetSprite(opponentProfile, theme.opponentProfileBg);

        SetSprite(graveyardViewerPanel, theme.graveyardViewerPanel);
        SetSprite(handleGraveyard, theme.handleGraveyard);
        SetSprite(closeGraveyard, theme.closeGraveyardBtn);
        
        SetSprite(extraDeckViewerPanel, theme.extraDeckViewerPanel);
        SetSprite(handleExtraDeck, theme.handleExtraDeck);
        SetSprite(closeExtraDeck, theme.closeExtraDeckBtn);

        SetSprite(panelActionMenu, theme.panelActionMenu);
        SetSprite(btnSummon, theme.btnSummon);
        SetSprite(btnSet, theme.btnSet);
        SetSprite(btnActivate, theme.btnActivate);
        SetSprite(btnCancel, theme.btnCancel);

        SetSprite(panelConfirmation, theme.panelConfirmation);
        SetSprite(btnYes, theme.btnYes);
        SetSprite(btnNo, theme.btnNo);

        // 2. Aplica Estilos de Texto
        if (uiRootForTexts != null)
        {
            var allTexts = uiRootForTexts.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                if (theme.globalFont != null) txt.font = theme.globalFont;
                txt.color = theme.mainTextColor;
                // Nota: Mudar o tamanho de todos pode quebrar o layout, use com cuidado
                // txt.fontSize *= theme.fontSizeMultiplier; 
            }
        }

        // 3. Atualiza Cores Globais no GameManager (para CardDisplay usar)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.themeHoverColor = theme.hoverOutlineColor;
        }

        // 4. Atualiza Phase Manager
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.phaseActiveColor = theme.phaseActiveColor;
            PhaseManager.Instance.phaseInactiveColor = theme.phaseInactiveColor;
            PhaseManager.Instance.phaseOutlineColor = theme.hoverOutlineColor; // Reusa a cor de hover ou cria uma nova no Theme
        }

        // 5. Atualiza FX
        if (DuelFXManager.Instance != null)
        {
            DuelFXManager.Instance.UpdateThemeFX(theme);
        }
    }

    void SetSprite(Image img, Sprite sprite)
    {
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
        }
    }
}
