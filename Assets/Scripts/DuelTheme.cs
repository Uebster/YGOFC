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
    
    [Header("Viewers (Removed Cards)")]
    public Sprite removedViewerPanel;
    public Sprite handleRemoved;
    public Sprite closeRemovedBtn;

    [Header("Minigames (Sorte/Tempo)")]
    public Sprite coinHeadsSprite;
    public Sprite coinTailsSprite;
    [Tooltip("As 6 faces do dado customizado para este tema (Ordem: 1, 2, 3, 4, 5, 6).")]
    public Sprite[] diceFaceSprites;
    [Tooltip("Opcional: Sprite de fundo do relógio de turnos.")]
    public Sprite clockBaseSprite;
    [Tooltip("Opcional: Sprite do ponteiro do relógio de turnos.")]
    public Sprite clockHandSprite;

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
    
    [Header("Position Selection Modal")]
    public Sprite panelPositionSelection;
    public Sprite btnPositionAttack;
    public Sprite btnPositionDefense;

    [Header("Reward Panel")]
    public Sprite rewardPanelBackground;
    public Sprite rewardRankBackground;
    public Sprite rewardContinueButton;

    [Header("Text Styling")]
    public TMP_FontAsset globalFont; // Fonte global para o duelo
    public Color mainTextColor = Color.white;
    public float fontSizeMultiplier = 1.0f; // Para ajustar tamanhos se a fonte for pequena

    [Header("Outlines & Highlights")]
    public Color playerHoverColor = new Color(0.5f, 1f, 0.5f, 1f); // Verde claro
    public Color opponentHoverColor = Color.yellow;
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
    public GameObject banishVFX;
    public GameObject flipVFX;
    
    [Header("Audio Overrides (Opcional)")]
    public AudioClip walkthroughBGM;  // Música da tela de história/walkthrough
    public AudioClip bgmNormal;       // Música padrão
    public AudioClip bgmTense;        // Música de desvantagem (LP < 50% do oponente)
    public AudioClip bgmWinning;      // Música de vantagem (LP > 200% do oponente)
}
