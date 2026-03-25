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

    [Header("Minigame Panels")]
    public Sprite coinTossPanelBg;
    public Sprite diceRollPanelBg;
    public Sprite turnClockPanelBg;

    [Header("Global Search UI")]
    public Sprite globalSearchPanel;
    public Sprite handleGlobalSearch;
    public Sprite closeGlobalSearchBtn;

    [Header("Card Selection UI")]
    public Sprite cardSelectionPanel;
    public Sprite handleCardSelection;
    public Sprite closeCardSelectionBtn;

    [Header("Minigames (Sorte/Tempo)")]
    public Sprite coinHeadsSprite;
    public Sprite coinTailsSprite;
    [Tooltip("As 6 faces do dado customizado para este tema (Ordem: 1, 2, 3, 4, 5, 6).")]
    public Sprite[] diceFaceSprites;
    [Tooltip("Opcional: Sprite de fundo do relógio de turnos.")]
    public Sprite clockBaseSprite;
    [Tooltip("Opcional: Sprite do ponteiro do relógio de turnos.")]
    public Sprite clockHandSprite;
    [Tooltip("Pivô (centro de rotação) do ponteiro (0 a 1). Padrão: 0.5, 0.5. (Ex: X: 0.5, Y: 0 para girar pela base, ou Y: -0.1 para girar fora da imagem)")]
    public Vector2 clockHandPivot = new Vector2(0.5f, 0.5f);
    [Tooltip("Posição (X, Y) do centro do relógio onde a base do ponteiro deve ser fixada (Apenas para o Relógio Gigante).")]
    public Vector2 clockHandCenter = Vector2.zero;
    [Tooltip("Tamanho customizado da base do relógio. Deixe (0,0) para manter o padrão.")]
    public Vector2 clockBaseSize = Vector2.zero;
    [Tooltip("Tamanho customizado do ponteiro. Deixe (0,0) para manter o padrão.")]
    public Vector2 clockHandSize = Vector2.zero;
    [Tooltip("Preserva a proporção (Aspect Ratio) das imagens do relógio.")]
    public bool preserveClockAspect = true;
    [Tooltip("Se marcado, o relógio terá o efeito de cor (fatia) diminuindo. Se desmarcado, apenas o ponteiro gira livremente.")]
    public bool useClockFillEffect = false;

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
