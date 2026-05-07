// Assets/Scripts/CardComparisonUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CardComparisonUI : MonoBehaviour
{
    public static CardComparisonUI Instance;

    [Header("Geral")]
    public TextMeshProUGUI comparisonText;
    public TextMeshProUGUI vsText; // Opcional: Crie um texto "VS" no meio da tela e arraste aqui!

    [Header("Jogador")]
    public RawImage playerCardImage; // Troque o componente Image por RawImage na Unity!
    public Outline playerOutline;
    public TextMeshProUGUI playerStatsText;
    public GameObject playerWinLoseStamp; // O WinorLoseImgPlayer
    public TextMeshProUGUI playerWinLoseText;

    [Header("Oponente")]
    public RawImage opponentCardImage; // Troque o componente Image por RawImage na Unity!
    public Outline opponentOutline;
    public TextMeshProUGUI opponentStatsText;
    public GameObject opponentWinLoseStamp; // O WinorLoseImgOpponent
    public TextMeshProUGUI opponentWinLoseText;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    public void ShowVersus(CardDisplay pCard, CardDisplay oCard, string title, string pStatStr, string oStatStr, int pVal, int oVal, System.Action onComplete, bool higherWins = true)
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        comparisonText.text = title;
        if (vsText != null) vsText.gameObject.SetActive(false); // Esconde o VS no início

        // Aplica as cores do GameManager nos Outlines
        if (GameManager.Instance != null && playerOutline != null && opponentOutline != null)
        {
            playerOutline.effectColor = GameManager.Instance.playerHoverColor;
            opponentOutline.effectColor = GameManager.Instance.opponentHoverColor;
        }

        // Esconde os elementos antes da animação
        playerCardImage.color = Color.clear;
        opponentCardImage.color = Color.clear;
        
        // Previne a "sombra fantasma" limpando a textura do duelo anterior
        playerCardImage.texture = null;
        opponentCardImage.texture = null;
        
        playerStatsText.text = "";
        opponentStatsText.text = "";
        playerWinLoseStamp.SetActive(false);
        opponentWinLoseStamp.SetActive(false);
        
        // Desliga o Outline enquanto voa
        if (playerOutline != null) playerOutline.enabled = false;
        if (opponentOutline != null) opponentOutline.enabled = false;

        StartCoroutine(CinematicRoutine(pCard, oCard, pStatStr, oStatStr, pVal, oVal, onComplete, higherWins));
    }

    private IEnumerator CinematicRoutine(CardDisplay pCard, CardDisplay oCard, string pStatStr, string oStatStr, int pVal, int oVal, System.Action onComplete, bool higherWins)
    {
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);

        // 1. ANIMAÇÃO DE VOO (Cartas físicas para o Painel)
        if (pCard != null) {
            LayoutElement pLe = pCard.GetComponent<LayoutElement>();
            if (pLe == null) pLe = pCard.gameObject.AddComponent<LayoutElement>();
            pLe.ignoreLayout = true;
            pCard.isGhostImage = true; // Previne o hover e interações indesejadas
            pCard.transform.SetParent(playerCardImage.transform, true);
            pCard.transform.SetAsFirstSibling(); // Garante que fica atrás dos textos (WIN/LOSE)
        }
        if (oCard != null) {
            LayoutElement oLe = oCard.GetComponent<LayoutElement>();
            if (oLe == null) oLe = oCard.gameObject.AddComponent<LayoutElement>();
            oLe.ignoreLayout = true;
            oCard.isGhostImage = true; // Previne o hover e interações indesejadas
            oCard.transform.SetParent(opponentCardImage.transform, true);
            oCard.transform.SetAsFirstSibling(); // Garante que fica atrás dos textos (WIN/LOSE)
        }

        Vector3 pStart = pCard != null ? pCard.transform.position : Vector3.zero;
        Vector3 oStart = oCard != null ? oCard.transform.position : Vector3.zero;
        Vector3 pTarget = playerCardImage.transform.position;
        Vector3 oTarget = opponentCardImage.transform.position;

        CardFlightSettings settings = DuelFXManager.Instance != null ? DuelFXManager.Instance.flightCardComparisonUI : new CardFlightSettings();
        float duration = settings.duration > 0 ? settings.duration : 0.6f;
        float flyT = 0;
        Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale * 1.5f : Vector3.one * 1.5f;
        float spawnTrailTimer = 0f;

        while (flyT < 1f)
        {
            flyT += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, flyT);
            
            float scaleMultiplier = 1f + Mathf.Sin(smooth * Mathf.PI) * (settings.flightScale - 1f);
            
            if (pCard != null) {
                Vector3 currentPos = Vector3.Lerp(pStart, pTarget, smooth);
                currentPos.y += Mathf.Sin(smooth * Mathf.PI) * 100f; // Arco Parabólico
                pCard.transform.position = currentPos;
                pCard.transform.localScale = Vector3.Lerp(GameManager.Instance.handCardScale, targetScale, smooth) * scaleMultiplier;
                pCard.transform.rotation = Quaternion.Lerp(Quaternion.Euler(0,0,0), Quaternion.identity, smooth);
            }
            if (oCard != null) {
                Vector3 currentPos = Vector3.Lerp(oStart, oTarget, smooth);
                currentPos.y += Mathf.Sin(smooth * Mathf.PI) * 100f; // Arco Parabólico
                oCard.transform.position = currentPos;
                oCard.transform.localScale = Vector3.Lerp(GameManager.Instance.handCardScale, targetScale, smooth) * scaleMultiplier;
                oCard.transform.rotation = Quaternion.Lerp(Quaternion.Euler(0,0,180f), Quaternion.identity, smooth);
            }
            
            if (settings.useTrail) {
                spawnTrailTimer -= Time.deltaTime;
                if (spawnTrailTimer <= 0) {
                    spawnTrailTimer = 0.04f;
                    if (pCard != null) SpawnTrailGhost(pCard.GetComponent<RectTransform>(), pCard.GetFrontTexture() ?? pCard.cardImage.texture, settings.trailColor, 0.3f);
                    if (oCard != null) SpawnTrailGhost(oCard.GetComponent<RectTransform>(), oCard.GetFrontTexture() ?? oCard.cardImage.texture, settings.trailColor, 0.3f);
                }
            }
            yield return null;
        }

        // Revela as cartas no painel (Flip 3D)
        bool pFlipped = false; bool oFlipped = false;
        if (pCard != null && pCard.isFlipped) pCard.ShowFront(true, () => pFlipped = true); else pFlipped = true;
        if (oCard != null && oCard.isFlipped) oCard.ShowFront(true, () => oFlipped = true); else oFlipped = true;
        
        yield return new WaitUntil(() => pFlipped && oFlipped);

        // Acende o Outline (Hover) apenas após as cartas pousarem e virarem
        if (playerOutline != null) playerOutline.enabled = true;
        if (opponentOutline != null) opponentOutline.enabled = true;

        // 2. TEXTO VS E STATUS (Tensão)
        if (vsText != null)
        {
            vsText.gameObject.SetActive(true);
            StartCoroutine(SlamTextRoutine(vsText.rectTransform));
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.attackTravelSound);
        }

        yield return new WaitForSeconds(0.5f);

        playerStatsText.text = pStatStr;
        opponentStatsText.text = oStatStr;
        StartCoroutine(SlamTextRoutine(playerStatsText.rectTransform));
        StartCoroutine(SlamTextRoutine(opponentStatsText.rectTransform));
        
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.defenseSound);

        yield return new WaitForSeconds(1.0f); // Pausa dramática lendo os números

        // 3. JULGAMENTO (Win, Lose, Draw)
        bool pWin = higherWins ? (pVal > oVal) : (pVal < oVal);
        bool oWin = higherWins ? (oVal > pVal) : (oVal < pVal);
        bool isDraw = pVal == oVal;

        ApplyStamp(playerWinLoseStamp, playerWinLoseText, isDraw ? "DRAW" : (pWin ? "WIN" : "LOSE"), isDraw ? Color.yellow : (pWin ? Color.green : Color.red));
        ApplyStamp(opponentWinLoseStamp, opponentWinLoseText, isDraw ? "DRAW" : (oWin ? "WIN" : "LOSE"), isDraw ? Color.yellow : (oWin ? Color.green : Color.red));

        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.attackImpactSound);

        yield return new WaitForSeconds(0.5f);

        // 4. EXPLOSÃO NO PERDEDOR E LIMPEZA
        // Desliga os outlines para a explosão ficar limpa
        if (playerOutline != null) playerOutline.enabled = false;
        if (opponentOutline != null) opponentOutline.enabled = false;

        // Esconde os carimbos de WIN/LOSE exatamente no instante em que a carta explode/evapora
        if (playerWinLoseStamp != null) playerWinLoseStamp.SetActive(false);
        if (opponentWinLoseStamp != null) opponentWinLoseStamp.SetActive(false);

        if (DuelFXManager.Instance != null && DuelFXManager.Instance.explosionVFX != null)
        {
            if (!pWin && pCard != null) 
            {
                DuelFXManager.Instance.PlayDestruction(pCard);
                pCard.SetVisibility(false); // O Fantasma gerado vai explodir, a carta real fica invisível
            }
            if (!oWin && oCard != null)
            {
                DuelFXManager.Instance.PlayDestruction(oCard);
                oCard.SetVisibility(false); // O Fantasma gerado vai explodir, a carta real fica invisível
            }
        }

        yield return new WaitForSeconds(0.6f); 

        // 5. FADE OUT E FIM
        float fadeT = 0;
        while (fadeT < 1f)
        {
            fadeT += Time.deltaTime / 0.3f; // Fade Out acelerado
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            yield return null;
        }

        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private void SpawnTrailGhost(RectTransform sourceRT, Texture tex, Color color, float duration)
    {
        GameObject ghost = new GameObject("CardTrailGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(sourceRT.parent, false);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        rt.pivot = sourceRT.pivot;
        
        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = tex; ri.color = color;
        
        Color startColor = ri.color;
        Vector3 startScale = ghost.transform.localScale;
        StartCoroutine(FadeAndDestroy(ghost, ri, startColor, startScale, duration));
    }

    private IEnumerator FadeAndDestroy(GameObject obj, RawImage img, Color startColor, Vector3 startScale, float duration)
    {
        float t = 0;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.7f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator SlamTextRoutine(RectTransform rt)
    {
        float t = 0;
        Vector3 endScale = Vector3.one;
        Vector3 startScale = Vector3.one * 3f;

        while (t < 1f)
        {
            t += Time.deltaTime / 0.2f;
            rt.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        rt.localScale = endScale;
    }

    private void ApplyStamp(GameObject stampObj, TextMeshProUGUI textObj, string text, Color color)
    {
        stampObj.SetActive(true);
        textObj.text = text;
        textObj.color = color;
        stampObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        StartCoroutine(SlamTextRoutine(stampObj.GetComponent<RectTransform>()));
    }
}
