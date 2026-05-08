using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CardSelectionUI : MonoBehaviour
{
    public static CardSelectionUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Transform contentArea; // Onde as cartas aparecem
    public GameObject cardItemPrefab; // Prefab da carta (pode ser o mesmo do DeckBuilder)
    public Button confirmButton;
    public TextMeshProUGUI confirmButtonText;
    public Button closeButton; // Para o botão CloseDeckCards da hierarquia

    private List<CardData> sourceList;
    private List<CardDisplay> selectedDisplays = new List<CardDisplay>();
    private int minSelection = 1;
    private int maxSelection = 1;
    private System.Action<List<CardData>> onConfirm;
    private HighlightCategory currentCategory = HighlightCategory.GenericTarget;

    // Cache de objetos instanciados para performance
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Dictionary<CardDisplay, GameObject> badges = new Dictionary<CardDisplay, GameObject>();

    // Variáveis para salvar o visual original e poder deixá-lo transparente
    private bool originalColorsSaved = false;
    private Color originalPanelColor;
    private Color originalScrollColor;
    private Image panelImage;
    private Image scrollImage;
    private bool isAnimating = false;
    private bool canCancelSelection = true;
    private Vector3 deckSourcePos;
    private bool isPlayerSource = false;

    void Awake()
    {
        Instance = this;
        // AUTO-CONFIGURAÇÃO: Tenta encontrar referências se não estiverem atribuídas
        if (contentArea == null)
        {
            // Busca profunda pelo Content, caso a hierarquia mude levemente
            var contentTransform = transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "Content");
            if (contentTransform != null) contentArea = contentTransform;
        }

        if (closeButton == null)
        {
            // Busca profunda pelo botão de fechar
            var btnTr = transform.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "CloseDeckCards");
            if (btnTr != null) closeButton = btnTr;
        }

        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        // Garante que comece fechado
        gameObject.SetActive(false);
    }

    public void Show(List<CardData> cards, string title, int min, int max, System.Action<List<CardData>> callback, HighlightCategory category = HighlightCategory.GenericTarget, bool canCancel = true)
    {
        sourceList = cards;
        minSelection = min;
        maxSelection = max;
        onConfirm = callback;
        currentCategory = category;
        canCancelSelection = canCancel;
        selectedDisplays.Clear();

        if (titleText) titleText.text = title;
        
        SaveOriginalColors();
        ApplyViewOnlyMode(max <= 0);

        // Se o prefab não estiver atribuído, tenta pegar do GameManager
        if (cardItemPrefab == null && GameManager.Instance != null)
            cardItemPrefab = GameManager.Instance.cardPrefab;

        gameObject.SetActive(true);
        RefreshUI();
    }

    private void SaveOriginalColors()
    {
        if (originalColorsSaved) return;
        panelImage = GetComponent<Image>();
        if (panelImage != null) originalPanelColor = panelImage.color;
        
        ScrollRect sr = GetComponentInChildren<ScrollRect>(true);
        if (sr != null) scrollImage = sr.GetComponent<Image>();
        if (scrollImage != null) originalScrollColor = scrollImage.color;
        
        originalColorsSaved = true;
    }

    private void ApplyViewOnlyMode(bool isViewOnly)
    {
        if (panelImage != null) panelImage.color = isViewOnly ? new Color(0f, 0f, 0f, 0.85f) : originalPanelColor;
        if (scrollImage != null) scrollImage.color = isViewOnly ? Color.clear : originalScrollColor;
    }

    private void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                CardDisplay cd = obj.GetComponent<CardDisplay>();
                if (cd != null && DuelFXManager.Instance != null)
                    DuelFXManager.Instance.SetSelectionIcon(cd, currentCategory, SelectionState.None);
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        badges.Clear();
    }

    void RefreshUI()
    {
        ClearSpawnedObjects();

        if (sourceList == null || cardItemPrefab == null) return;

        foreach (var card in sourceList)
        {
            GameObject go = Instantiate(cardItemPrefab, contentArea);
            spawnedObjects.Add(go);

            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display == null) display = go.AddComponent<CardDisplay>();
            
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            if (maxSelection <= 0) cg.alpha = 0f; // Começa invisível para a animação de entrada

            // Configura visual
            display.SetCard(card, GameManager.Instance.GetCardBackTexture(), true);
            display.isInteractable = false; // Sem menu de ação (apenas clique para selecionar)

            // Adiciona comportamento de clique
            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            
            // Remove listeners antigos se houver (por segurança)
            btn.onClick.RemoveAllListeners();
            
            // Captura a variável para o closure
            CardData currentCard = card;
            btn.onClick.AddListener(() => ToggleSelection(display));

            // Atualiza estado visual inicial
            UpdateCardVisual(display);
        }

        UpdateConfirmButton();

        if (maxSelection <= 0)
        {
            StartCoroutine(AnimateIntroRoutine());
        }
    }

    void ToggleSelection(CardDisplay display)
    {
        if (maxSelection <= 0) return; // Modo apenas visualização (View-Only)

        if (selectedDisplays.Contains(display))
        {
            selectedDisplays.Remove(display);
        }
        else
        {
            if (selectedDisplays.Count < maxSelection)
            {
                selectedDisplays.Add(display);
            }
            else if (maxSelection == 1)
            {
                // Se for seleção única, troca a seleção atual pela nova
                selectedDisplays.Clear();
                selectedDisplays.Add(display);
                // Precisamos atualizar visualmente todas as cartas para remover o destaque da anterior
                // Para simplificar, chamamos RefreshVisuals em todas
                RefreshAllVisuals();
                UpdateConfirmButton();
                return;
            }
            else if (maxSelection > 1) // NOVO: Rolling Selection para múltiplas!
            {
                selectedDisplays.RemoveAt(0); // Remove a escolha mais antiga
                selectedDisplays.Add(display); // Adiciona a nova na frente
                RefreshAllVisuals();
                UpdateConfirmButton();
                return;
            }
        }
        
        // Se a ordem importa (seleção múltipla), atualizamos todos para garantir que os números (1, 2, 3) fiquem corretos
        // Ex: Se desmarcar o 1, o 2 vira 1.
        if (maxSelection > 1) RefreshAllVisuals();
        else UpdateCardVisual(display);

        UpdateConfirmButton();
    }

    void UpdateCardVisual(CardDisplay display)
    {
        bool isSelected = selectedDisplays.Contains(display);
        
        SelectionState state = isSelected ? SelectionState.Selected : SelectionState.Available;
        if (maxSelection <= 0) state = SelectionState.None;

        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.SetSelectionIcon(display, currentCategory, state);
        else
            display.SetHighlight(currentCategory, isSelected && maxSelection > 0);

        // Lógica de Ordem Visual (Badges)
        if (isSelected && maxSelection > 1)
        {
            int order = selectedDisplays.IndexOf(display) + 1;
            ShowSelectionBadge(display, order);
        }
        else
        {
            HideSelectionBadge(display);
        }
    }

    void ShowSelectionBadge(CardDisplay display, int order)
    {
        if (!badges.ContainsKey(display) || badges[display] == null)
        {
            // Cria o badge se não existir
            GameObject badge = new GameObject("OrderBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(display.transform, false);
            
            // Configura Imagem (Fundo Azul)
            Image img = badge.GetComponent<Image>();
            img.color = new Color(0f, 0.5f, 1f, 0.9f); // Azul semitransparente
            
            RectTransform rect = badge.GetComponent<RectTransform>();
            // Posiciona no canto superior direito
            rect.anchorMin = new Vector2(0.75f, 0.75f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Configura Texto
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badge.transform, false);
            
            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 14; // Tamanho base, o AutoSizing ajusta
            txt.color = Color.white;
            txt.enableAutoSizing = true;
            txt.fontStyle = FontStyles.Bold;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            badges[display] = badge;
        }
        
        GameObject b = badges[display];
        b.SetActive(true);
        b.GetComponentInChildren<TextMeshProUGUI>().text = order.ToString();
        b.transform.SetAsLastSibling(); // Garante que fique por cima da carta
    }

    void HideSelectionBadge(CardDisplay display)
    {
        if (badges.ContainsKey(display) && badges[display] != null)
        {
            badges[display].SetActive(false);
        }
    }

    void RefreshAllVisuals()
    {
        foreach (var go in spawnedObjects)
        {
            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display != null)
            {
                UpdateCardVisual(display);
            }
        }
    }

    void UpdateConfirmButton()
    {
        if (confirmButton)
        {
            if (maxSelection <= 0)
            {
                confirmButton.interactable = true;
                if (confirmButtonText) confirmButtonText.text = "OK";
                return;
            }

            bool isValid = selectedDisplays.Count >= minSelection && selectedDisplays.Count <= maxSelection;
            confirmButton.interactable = isValid;
            
            if (confirmButtonText)
            {
                if (isValid) confirmButtonText.text = "Confirmar";
                else confirmButtonText.text = $"Selecione {minSelection}-{maxSelection}";
            }
        }
    }

    void ConfirmSelection()
    {
        if (isAnimating) return;
        if (maxSelection <= 0)
        {
            StartCoroutine(AnimateOutroRoutine(true));
        }
        else
        {
            FinishClose(true);
        }
    }

    void CancelSelection()
    {
        if (!gameObject.activeInHierarchy)
        {
            FinishClose(false);
            return;
        }

        if (isAnimating) return;
        if (!canCancelSelection)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Você deve concluir esta seleção. Não é possível cancelar.");
            return;
        }
        if (maxSelection <= 0)
        {
            StartCoroutine(AnimateOutroRoutine(false));
        }
        else
        {
            FinishClose(false);
        }
    }

    private void FinishClose(bool isConfirm)
    {
        gameObject.SetActive(false);
        List<CardData> result = isConfirm ? selectedDisplays.Select(d => d.CurrentCardData).ToList() : new List<CardData>();
        ClearSpawnedObjects();
        onConfirm?.Invoke(result);
    }

    private IEnumerator AnimateIntroRoutine()
    {
        isAnimating = true;
        if (confirmButton != null) confirmButton.interactable = false;

        yield return new WaitForEndOfFrame(); // Espera a Unity calcular a largura do GridLayout!

        deckSourcePos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0); // Centro padrão
        isPlayerSource = false;

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null && sourceList != null && sourceList.Count > 0)
        {
            CardData firstCard = sourceList[0];
            bool foundPile = false;

            System.Action<List<CardData>, PileDisplay, bool> CheckPile = (pileList, pileDisplay, isPlayer) => {
                if (!foundPile && pileList != null && pileList.Contains(firstCard) && pileDisplay != null) {
                    deckSourcePos = pileDisplay.transform.position;
                    if (pileDisplay.contentParent != null && pileDisplay.contentParent.childCount > 0) {
                        deckSourcePos = pileDisplay.contentParent.GetChild(pileDisplay.contentParent.childCount - 1).position;
                    }
                    isPlayerSource = isPlayer;
                    foundPile = true;
                }
            };

            CheckPile(GameManager.Instance.GetPlayerMainDeck(), GameManager.Instance.playerDeckDisplay, true);
            CheckPile(GameManager.Instance.GetOpponentMainDeck(), GameManager.Instance.opponentDeckDisplay, false);
            CheckPile(GameManager.Instance.GetPlayerGraveyard(), GameManager.Instance.playerGraveyardDisplay, true);
            CheckPile(GameManager.Instance.GetOpponentGraveyard(), GameManager.Instance.opponentGraveyardDisplay, false);
            CheckPile(GameManager.Instance.GetPlayerExtraDeck(), GameManager.Instance.playerExtraDeckDisplay, true);
            CheckPile(GameManager.Instance.GetOpponentExtraDeck(), GameManager.Instance.opponentExtraDeckDisplay, false);
            CheckPile(GameManager.Instance.GetPlayerRemoved(), GameManager.Instance.playerRemovedDisplay, true);
            CheckPile(GameManager.Instance.GetOpponentRemoved(), GameManager.Instance.opponentRemovedDisplay, false);
        }

        // PLAYER: Da Esquerda para a Direita (0 -> N)
        // OPPONENT: Da Direita para a Esquerda (N -> 0)
        int startIdx = isPlayerSource ? 0 : spawnedObjects.Count - 1;
        int endIdx = isPlayerSource ? spawnedObjects.Count : -1;
        int step = isPlayerSource ? 1 : -1;

        CardFlightSettings settings = DuelFXManager.Instance != null ? DuelFXManager.Instance.flightCardSelectionUI : new CardFlightSettings();

        for (int i = startIdx; i != endIdx; i += step)
        {
            GameObject realCard = spawnedObjects[i];
            Vector3 targetPos = realCard.transform.position;
            
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);
            
            StartCoroutine(FlyGhostCard(deckSourcePos, targetPos, true, realCard.GetComponent<CanvasGroup>(), settings, realCard.GetComponent<CardDisplay>().CurrentCardData));
            yield return new WaitForSeconds(0.15f); // Intervalo de cascata
        }

        yield return new WaitForSeconds(0.4f); // Espera o último fantasma pousar
        isAnimating = false;
        if (confirmButton != null) confirmButton.interactable = true;
    }

    private IEnumerator AnimateOutroRoutine(bool isConfirm)
    {
        isAnimating = true;
        if (confirmButton != null) confirmButton.interactable = false;

        // PLAYER: Da Direita para a Esquerda (N -> 0)
        // OPPONENT: Da Esquerda para a Direita (0 -> N)
        int startIdx = isPlayerSource ? spawnedObjects.Count - 1 : 0;
        int endIdx = isPlayerSource ? -1 : spawnedObjects.Count;
        int step = isPlayerSource ? -1 : 1;

        for (int i = startIdx; i != endIdx; i += step)
        {
            GameObject realCard = spawnedObjects[i];
            Vector3 startPos = realCard.transform.position;
            CanvasGroup cg = realCard.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f; // Esconde a carta da UI

            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);
            
            CardFlightSettings settings = DuelFXManager.Instance != null ? DuelFXManager.Instance.flightCardSelectionUI : new CardFlightSettings();
            StartCoroutine(FlyGhostCard(startPos, deckSourcePos, false, cg, settings, realCard.GetComponent<CardDisplay>().CurrentCardData));
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.4f); // Espera o último fantasma voltar pro Deck
        isAnimating = false;
        FinishClose(isConfirm);
    }

    private IEnumerator LoadTextureForGhost(RawImage ri, CardData cData)
    {
        string url = "file://" + System.IO.Path.Combine(Application.streamingAssetsPath, cData.image_filename);
        try { url = new System.Uri(System.IO.Path.Combine(Application.streamingAssetsPath, cData.image_filename)).AbsoluteUri; } catch { }
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url)) {
            yield return request.SendWebRequest();
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success && ri != null) ri.texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
        }
    }

    private IEnumerator FlyGhostCard(Vector3 start, Vector3 end, bool isIntro, CanvasGroup realCardCG, CardFlightSettings settings, CardData cData)
    {
        if (settings == null) settings = new CardFlightSettings();

        GameObject ghost = new GameObject("GhostCard_ViewOnly", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(transform, false);
        ghost.transform.SetAsLastSibling();

        RectTransform rt = ghost.GetComponent<RectTransform>();
        if (realCardCG != null) {
            RectTransform realRt = realCardCG.GetComponent<RectTransform>();
            rt.sizeDelta = realRt.rect.size;
            rt.pivot = realRt.pivot;
        }
        RawImage ri = ghost.GetComponent<RawImage>();
        Texture2D backTex = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;
        Texture2D frontTex = null;

        CardDisplay realCD = realCardCG != null ? realCardCG.GetComponent<CardDisplay>() : null;
        if (isIntro) ri.texture = backTex;
        else {
            if (realCD != null) frontTex = realCD.GetFrontTexture();
            ri.texture = frontTex != null ? frontTex : backTex; // Se tiver saindo, começa com a frente
        }

        float duration = settings.duration > 0 ? settings.duration : 0.4f;
        float t = 0f;
        bool flipped = false;

        // --- LÓGICA DE VOO BIDIRECIONAL ---
        // Inverte a escala e o offset para o voo de volta, garantindo uma animação perfeitamente simétrica.
        Vector3 actualStart;
        Vector3 actualEnd;
        Vector3 baseStartScale;
        Vector3 baseEndScale;

        if (isIntro) {
            actualStart = start + new Vector3(settings.popOffset.x, settings.popOffset.y, 0);
            actualEnd = end;
            baseStartScale = rt.localScale * settings.startScaleMult;
            baseEndScale = rt.localScale * settings.endScaleMult;
        } else { // Voo de Retorno (Outro)
            actualStart = start;
            actualEnd = end; // O offset de "pouso" no deck não é necessário aqui, a pilha já tem um visual de entrada.
            baseStartScale = rt.localScale * settings.endScaleMult;   // Começa com a escala final
            baseEndScale = rt.localScale * settings.startScaleMult * 0.8f; // Termina com a escala inicial (Reduzido em 20% para suavizar o encaixe visual no deck)
        }

        rt.position = actualStart;

        GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
        if (settings.useTrail && settings.trailType == AttackTrailType.ContinuousLine) {
            lineObj = new GameObject("FlightLineTrail", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(ghost.transform.parent, false);
            lineObj.transform.SetSiblingIndex(ghost.transform.GetSiblingIndex());
            lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.pivot = new Vector2(0, 0.5f); lineRT.position = actualStart;
            lineImg = lineObj.GetComponent<Image>(); lineImg.color = settings.trailColor;
        }

        float spawnTrailTimer = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            // Cria um arco parabólico durante o voo
            Vector3 currentPos = Vector3.Lerp(actualStart, actualEnd, smooth);
            currentPos.y += Mathf.Sin(smooth * Mathf.PI) * 60f; 
            rt.position = currentPos;

            // Animação de Flip no meio do voo (entre 40% e 60% do trajeto)
            if (t >= 0.4f && t <= 0.6f)
            {
                float flipProgress = (t - 0.4f) / 0.2f;
                float scaleX = Mathf.Cos(flipProgress * Mathf.PI);
                
                float scaleMultiplier = 1f + Mathf.Sin(smooth * Mathf.PI) * (settings.flightScale - 1f);
                Vector3 currentBaseScale = Vector3.Lerp(baseStartScale, baseEndScale, smooth) * scaleMultiplier;
                
                rt.localScale = new Vector3(currentBaseScale.x * Mathf.Abs(scaleX), currentBaseScale.y, currentBaseScale.z);

                if (flipProgress >= 0.5f && !flipped)
                {
                    flipped = true;
                    if (isIntro) {
                        if (frontTex == null && realCD != null) frontTex = realCD.GetFrontTexture();
                        if (frontTex != null) ri.texture = frontTex;
                        else StartCoroutine(LoadTextureForGhost(ri, cData));
                    } else {
                        ri.texture = backTex; // Se tiver saindo, vira pro verso
                    }
                }
            }
            else
            {
                float scaleMultiplier = 1f + Mathf.Sin(smooth * Mathf.PI) * (settings.flightScale - 1f);
                rt.localScale = Vector3.Lerp(baseStartScale, baseEndScale, smooth) * scaleMultiplier;
            }

            if (settings.useTrail) {
                if (settings.trailType == AttackTrailType.Shadows || settings.trailType == AttackTrailType.SmoothShadows) {
                    spawnTrailTimer -= Time.deltaTime; 
                    float interval = settings.trailType == AttackTrailType.SmoothShadows ? 0.015f : 0.04f;
                    if (spawnTrailTimer <= 0) { 
                        spawnTrailTimer = interval; 
                        SpawnTrailGhost(rt, ri.texture, settings.trailColor, settings.trailType == AttackTrailType.SmoothShadows ? 0.15f : 0.3f); 
                    }
                } else if (settings.trailType == AttackTrailType.ContinuousLine && lineObj != null) {
                    Vector3 dirToCurrent = currentPos - actualStart;
                    float dist = dirToCurrent.magnitude; float canvasScale = lineObj.transform.lossyScale.x;
                    if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, settings.trailWidth);
                    lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                }
            }

            yield return null;
        }

        if (lineObj != null) {
            StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));
        }

        if (isIntro && realCardCG != null) realCardCG.alpha = 1f; // Revela a carta real encaixada na UI
        Destroy(ghost);
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
        
        StartCoroutine(FadeAndDestroyCardGhost(ghost, ri, duration));
    }

    private IEnumerator FadeAndDestroyCardGhost(GameObject obj, RawImage img, float duration)
    {
        float t = 0; Color startColor = img.color;
        Vector3 startScale = obj.transform.localScale;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.7f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator FadeAndDestroyLine(GameObject obj, Image img, float duration)
    {
        float t = 0; Color startColor = img.color;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }
}
