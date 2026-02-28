using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "NewDuelTheme", menuName = "YuGiOh/Duel Theme")]
public class DuelTheme : ScriptableObject
{
    [Header("General UI")]
    public Sprite boardBackground;
    public Sprite fieldImage;
    public Sprite phaseIndicatorBg;
    
    [Header("Profiles")]
    public Sprite playerProfileBg;
    public Sprite opponentProfileBg;

    [Header("Card Viewer")]
    public Sprite panelCardViewer;
    public Sprite panelDescription;
    public Sprite handleDescription;

    [Header("Viewers (GY / Extra)")]
    public Sprite graveyardViewerPanel;
    public Sprite handleGraveyard;
    public Sprite closeGraveyardBtn;
    public Sprite extraDeckViewerPanel;
    public Sprite handleExtraDeck;
    public Sprite closeExtraDeckBtn;

    [Header("Action Menu")]
    public Sprite panelActionMenu;
    public Sprite btnSummon;
    public Sprite btnSet;
    public Sprite btnActivate;
    public Sprite btnCancel;

    [Header("Confirmation Modal")]
    public Sprite panelConfirmation;
    public Sprite btnYes;
    public Sprite btnNo;

    [Header("Text Styling")]
    public TMP_FontAsset globalFont; // Fonte global para o duelo
    public Color mainTextColor = Color.white;
    public float fontSizeMultiplier = 1.0f; // Para ajustar tamanhos se a fonte for pequena

    [Header("Outlines & Highlights")]
    public Color hoverOutlineColor = Color.yellow;
    public Color phaseActiveColor = new Color(1f, 0.8f, 0f, 1f);
    public Color phaseInactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [Header("FX Overrides (Opcional)")]
    // Deixe nulo para usar o padrão do DuelFXManager
    public GameObject spellActivateVFX;
    public GameObject trapActivateVFX;
    public GameObject summonVFX;
    public GameObject fusionVFX;
    public GameObject tributeVFX;
    public GameObject attackVFX;
    public GameObject explosionVFX;
    
    [Header("Audio Overrides (Opcional)")]
    public AudioClip bgm; // Música de fundo do tema
}
