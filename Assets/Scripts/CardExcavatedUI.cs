using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardExcavatedUI : MonoBehaviour
{
    public static CardExcavatedUI Instance;

    [Tooltip("Arraste o objeto PlayerCard (RawImage) aqui. Ele servirá de âncora e tamanho.")]
    public RawImage playerCardAnchor; 
    [Tooltip("Arraste o objeto OpponentCard (RawImage) aqui. Ele servirá de âncora e tamanho.")]
    public RawImage opponentCardAnchor; 

    private Dictionary<CardData, GameObject> activeGhosts = new Dictionary<CardData, GameObject>();
    private bool isWaitingForClick = false;

    void Awake()
    {
        Instance = this;
        // Esconde as imagens âncoras base, usaremos apenas o Transform delas
        if (playerCardAnchor != null) playerCardAnchor.gameObject.SetActive(false);
        if (opponentCardAnchor != null) opponentCardAnchor.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (isWaitingForClick && Input.GetMouseButtonDown(0))
        {
            isWaitingForClick = false;
        }
    }

    public void ClearCards()
    {
        StopAllCoroutines();
        foreach (var ghost in activeGhosts.Values)
        {
            if (ghost != null) Destroy(ghost);
        }
        activeGhosts.Clear();

        System.Action<Transform> ClearChildren = (parent) => {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith("ExcavatedGhost_") || child.name.StartsWith("ExcavateTrailGhost") || child.name.StartsWith("ExcavationLineTrail"))
                {
                    Destroy(child.gameObject);
                }
            }
        };

        ClearChildren(this.transform);
        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null) {
            Canvas c = GameManager.Instance.duelFieldUI.GetComponentInParent<Canvas>();
            if (c != null) ClearChildren(c.transform);
        }

        isWaitingForClick = false;
    }

    public void ShowExcavatedCards(List<CardData> cards, bool isPlayer, System.Action onComplete)
    {
        ClearCards();
        gameObject.SetActive(true);
        StartCoroutine(ExcavateRoutine(cards, isPlayer, onComplete));
    }

    private IEnumerator ExcavateRoutine(List<CardData> cards, bool isPlayer, System.Action onComplete)
    {
        bool isSidePanel = GameManager.Instance != null && GameManager.Instance.excavationMode == GameManager.ExcavationMode.SidePanel;

        ExcavationSettings settings = null;
        if (DuelFXManager.Instance != null) {
            settings = isSidePanel ? DuelFXManager.Instance.excavationSidePanel : DuelFXManager.Instance.excavationTopOfDeck;
        } else {
            settings = new ExcavationSettings();
        }

        RawImage anchorImage = isPlayer ? playerCardAnchor : opponentCardAnchor;
        RectTransform anchorRT = anchorImage != null ? anchorImage.GetComponent<RectTransform>() : null;
        
        float spacing = settings.sidePanelSpacing;
        if (isSidePanel && anchorRT != null && spacing == 0) spacing = anchorRT.rect.width + 10f; // Fallback
        
        // DIREÇÃO INVERTIDA: Agora as cartas se espalham em direção ao centro do campo!
        Vector3 moveDirection = isPlayer ? Vector3.left : Vector3.right; 

        PileDisplay deckPile = isPlayer ? GameManager.Instance.playerDeckDisplay : GameManager.Instance.opponentDeckDisplay;
        Vector3 deckPos = deckPile != null ? deckPile.transform.position : Vector3.zero;
        if (deckPile != null && deckPile.contentParent != null && deckPile.contentParent.childCount > 0)
        {
            // Garante que o ponto de partida seja a coordenada real da carta no TOPO da pilha
            deckPos = deckPile.contentParent.GetChild(deckPile.contentParent.childCount - 1).position;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardData cData = cards[i];
            
            GameObject ghost = new GameObject("ExcavatedGhost_" + cData.name, typeof(RectTransform), typeof(RawImage), typeof(Outline));
            
            // Tenta colocar no canvas principal para não ser cortado ou sumir na resolução final
            Transform uiParent = this.transform;
            if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null) {
                Canvas c = GameManager.Instance.duelFieldUI.GetComponentInParent<Canvas>();
                if (c != null) uiParent = c.transform;
            }
            ghost.transform.SetParent(uiParent, false);
            ghost.transform.SetAsLastSibling();
            
            RectTransform rt = ghost.GetComponent<RectTransform>();
            
            if (isSidePanel && anchorRT != null)
            {
                rt.sizeDelta = anchorRT.sizeDelta;
                rt.localRotation = anchorRT.localRotation;
            }
            else
            {
                // TopOfDeck mode: Usa tamanho padrão do campo e vira de frente para a tela
                rt.sizeDelta = new Vector2(146, 214); 
                rt.localScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
                rt.localRotation = isPlayer ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
            }
            
            RawImage ri = ghost.GetComponent<RawImage>();
            ri.texture = GameManager.Instance.GetCardBackTexture();
            
            Outline outline = ghost.GetComponent<Outline>();
            outline.effectColor = Color.cyan;
            outline.effectDistance = new Vector2(4, -4);

            activeGhosts[cData] = ghost;

            Vector3 startPos = deckPos;
            Vector3 targetPos = startPos;

            if (isSidePanel && anchorRT != null)
            {
                targetPos = anchorRT.position + (moveDirection * (spacing * i));
            }
            else if (isSidePanel)
            {
                targetPos = startPos + (moveDirection * (spacing * i));
            }
            else
            {
                // TopOfDeck: Usa o Offset X e Y exatos configurados no DuelFXManager
                targetPos = startPos + new Vector3(settings.popOffset.x, settings.popOffset.y + (i * 2f), 0f);
            }

            if (DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations)
            {
                Vector3 baseScale = rt.localScale;
                DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.drawSound); 
                StartCoroutine(FlyAndFlipGhost(ghost, rt, ri, startPos, targetPos, cData, baseScale, settings, isSidePanel));
                yield return new WaitForSeconds(0.15f); // Cadência ligeiramente mais rápida
            }
            else
            {
                rt.position = targetPos;
                StartCoroutine(LoadTextureRoutine(ri, cData));
            }
        }

        yield return new WaitForSeconds(0.4f); // Espera o último voo terminar

        if (GameManager.Instance.autoExcavateFlow) yield return new WaitForSeconds(GameManager.Instance.excavateAutoDelay);
        else
        {
            isWaitingForClick = true;
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Clique na tela para continuar...");
            yield return new WaitWhile(() => isWaitingForClick);
        }

        onComplete?.Invoke();
    }

    private IEnumerator FlyAndFlipGhost(GameObject ghost, RectTransform rt, RawImage ri, Vector3 startPos, Vector3 targetGlobalPos, CardData cData, Vector3 baseScale, ExcavationSettings settings, bool isSidePanel)
    {
        float duration = settings.duration > 0 ? settings.duration : 0.4f; 
        float t = 0;
        rt.position = startPos;
        
        float flipScale = settings.flipScaleMult;
        Vector3 initialScale = baseScale * settings.startScaleMult;
        Vector3 finalScale = baseScale * settings.endScaleMult;

        bool flipped = false;

        GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
        if (settings.useTrail && settings.trailType == AttackTrailType.ContinuousLine) {
            lineObj = new GameObject("ExcavationLineTrail", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(ghost.transform.parent, false);
            lineObj.transform.SetSiblingIndex(ghost.transform.GetSiblingIndex());
            lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.pivot = new Vector2(0, 0.5f); lineRT.position = startPos;
            lineImg = lineObj.GetComponent<Image>(); lineImg.color = settings.trailColor;
        }
        float spawnTrailTimer = 0f;

        while (t < 1f)
        {
            if (ghost == null) break;
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            Vector3 currentPos = Vector3.Lerp(startPos, targetGlobalPos, smooth);
            if (isSidePanel) currentPos.y += Mathf.Sin(smooth * Mathf.PI) * settings.sidePanelArcHeight; // Arco Parabólico
            rt.position = currentPos;

            // Escala Fluida combinada com Rotação 3D Simulada no Eixo X
            float currentMult = t < 0.5f ? Mathf.Lerp(settings.startScaleMult, flipScale, t * 2f) : Mathf.Lerp(flipScale, settings.endScaleMult, (t - 0.5f) * 2f);
            float scaleX = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
            rt.localScale = new Vector3(baseScale.x * currentMult * scaleX, baseScale.y * currentMult, baseScale.z * currentMult);

            if (t >= 0.5f && !flipped) { flipped = true; StartCoroutine(LoadTextureRoutine(ri, cData)); if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.flipSound); }
            
            if (settings.useTrail) {
                if (settings.trailType == AttackTrailType.Shadows || settings.trailType == AttackTrailType.SmoothShadows) {
                    spawnTrailTimer -= Time.deltaTime; 
                    float interval = settings.trailType == AttackTrailType.SmoothShadows ? 0.015f : 0.04f;
                    if (spawnTrailTimer <= 0) { 
                        spawnTrailTimer = interval; 
                        SpawnTrailGhost(rt, ri.texture, settings.trailColor, settings.trailType == AttackTrailType.SmoothShadows ? 0.15f : 0.3f); 
                    }
                } else if (settings.trailType == AttackTrailType.ContinuousLine && lineObj != null) {
                    Vector3 dirToCurrent = currentPos - startPos;
                    float dist = dirToCurrent.magnitude; float canvasScale = lineObj.transform.lossyScale.x;
                    if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, settings.trailWidth);
                    lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                }
            }
            yield return null;
        }
        if (lineObj != null) StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));
        if (ghost != null) { rt.position = targetGlobalPos; rt.localScale = finalScale; }
    }

    private void SpawnTrailGhost(RectTransform sourceRT, Texture tex, Color color, float duration)
    {
        GameObject ghost = new GameObject("ExcavateTrailGhost", typeof(RectTransform), typeof(RawImage));
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

    private IEnumerator LoadTextureRoutine(RawImage ri, CardData cData)
    {
        string url = "file://" + System.IO.Path.Combine(Application.streamingAssetsPath, cData.image_filename);
        try { url = new System.Uri(System.IO.Path.Combine(Application.streamingAssetsPath, cData.image_filename)).AbsoluteUri; } catch { }
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url)) {
            yield return request.SendWebRequest();
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success && ri != null) ri.texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
        }
    }

    public GameObject GetAndConsumeGhost(CardData cData)
    {
        if (activeGhosts.ContainsKey(cData))
        {
            GameObject dumbGhost = activeGhosts[cData];
            activeGhosts.Remove(cData);

            if (dumbGhost == null) return null;

            // FIX: Instancia o fantasma inteligente DENTRO do Canvas (UI Parent) para ele ser visto viajando!
            GameObject smartGhost = Instantiate(GameManager.Instance.cardPrefab, dumbGhost.transform.parent);
            smartGhost.transform.position = dumbGhost.transform.position;
            smartGhost.transform.rotation = dumbGhost.transform.rotation;
            smartGhost.transform.localScale = dumbGhost.transform.localScale;
            CardDisplay display = smartGhost.GetComponent<CardDisplay>();
            display.SetCard(cData, GameManager.Instance.GetCardBackTexture(), true); // A carta já foi revelada, então começa face-up

            Destroy(dumbGhost); // Destrói o fantasma visual antigo

            if (activeGhosts.Count == 0) gameObject.SetActive(false); // Desliga o painel se não sobrar ninguém
            return smartGhost;
        }
        return null;
    }
}