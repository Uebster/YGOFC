using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DuelFXManager
{
    // ==============================================================================
    // SUMMONS
    // ==============================================================================


    // --- INVOCAÇÃO / TRIBUTO ---

    public void PlaySummonImpact(CardDisplay card, SummonVFXType type = SummonVFXType.Normal)
    {
        if (!enableAnimations || card == null) return;
        StartCoroutine(SummonImpactRoutine(card, type));
    }

    private IEnumerator SummonImpactRoutine(CardDisplay card, SummonVFXType type)
    {
        SummonVFXPackage package = GetSummonPackage(type);
        bool hasMarker = package != null && package.fieldMarker.useMarker;
        SummonImpactSettings settings = GetImpactSettings(type);
        
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();

        if (hasMarker)
        {
            cg.alpha = 0f;
            Transform zone = card.transform.parent != null ? card.transform.parent : card.transform;
            SpawnFieldMarker(zone, type);
            
            float waitTime = settings != null ? settings.delayBeforeImpact : 0.3f;
            yield return new WaitForSeconds(waitTime / (animationSpeed > 0 ? animationSpeed : 1f));
            cg.alpha = 1f;
        }

        PlaySound(summonSound); 
        
        if (settings != null && settings.useImpact)
        {
            GameObject prefabToUse = settings.prefab != null ? settings.prefab : summonVFX; 

            if (settings.usePrefab && prefabToUse != null)
                SpawnVFXPublic(prefabToUse, card.transform.position);

            if (settings.useNativePulse)
                StartCoroutine(PulseGhostRoutine(card, settings.pulseScale, settings.pulseDuration, settings.pulseColor, settings.pulseOutlineWidth));
        }
    }

    // Wrapper de compatibilidade retroativa
    public void PlaySummonEffect(CardDisplay card) => PlaySummonImpact(card, SummonVFXType.Normal);

    public void PlayTokenSummonEffect(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayTokenSummonEffect");
        if (!enableAnimations || card == null) return;
        StartCoroutine(TokenSummonRoutine(card));
    }

    private IEnumerator TokenSummonRoutine(CardDisplay card)
    {
        PlaySound(tokenSummonSound != null ? tokenSummonSound : summonSound);

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();

        Vector3 originalPos = card.transform.position;
        Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;

        // Sempre esconde e encolhe inicialmente, não importa se é nativo ou prefab
        cg.alpha = 0f;
        card.transform.localScale = targetScale * tokenStartScaleMult;

        // Instancia a sua fumaça (Dust_2D)
        if (useTokenSummonPrefab)
        {
            GameObject prefab = tokenSummonVFX != null ? tokenSummonVFX : summonVFX;
            if (prefab != null) SpawnVFXPublic(prefab, card.transform.position);
        }

        // Animação de Surgimento (Controlada pelo Inspector)
        float delay = tokenAppearDelay / (animationSpeed > 0 ? animationSpeed : 1f);
        if (delay > 0) yield return new WaitForSeconds(delay);

        float duration = tokenAppearDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        
        // Controle de cor para evitar o "Flash Branco" e materializar suavemente das sombras
        RawImage ri = card.cardImage;
        Color targetColor = Color.white;
        if (ri != null) { targetColor = ri.color; ri.color = tokenSummonFadeColor; }

        while (t < 1f)
        {
            if (card == null) yield break;
            t += Time.deltaTime / duration;
            float p = Mathf.SmoothStep(0, 1, t);
            
            // Apenas treme se o Nativo estiver ativado
            if (useTokenSummonRoutine)
            {
                float shake = Mathf.Lerp(6f, 0f, p); 
                card.transform.position = originalPos + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0);
            }
            
            cg.alpha = p; 
            card.transform.localScale = Vector3.Lerp(targetScale * tokenStartScaleMult, targetScale * tokenEndScaleMult, p);
            if (ri != null) ri.color = Color.Lerp(tokenSummonFadeColor, targetColor, p);
            
            yield return null;
        }
        if (card != null) { 
            card.transform.position = originalPos; 
            card.transform.localScale = targetScale * tokenEndScaleMult;
            cg.alpha = 1f; 
            if (ri != null) ri.color = targetColor; 
        }
    }

    public void PlayTributeSummonEffect(CardDisplay card)
    {
        PlaySummonImpact(card, SummonVFXType.Tribute);
    }

    public void PlayFusionEffect(CardDisplay card)
    {
        PlaySummonImpact(card, SummonVFXType.Fusion);
    }

    public void PlayTributeEffect(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayTributeEffect");
        if (!enableAnimations || card == null) return;
        PlaySound(tributeSound);
        SetSelectionIcon(card, SummonVFXType.Tribute, SelectionState.Selected);
        if (tributeVFX != null) { GameObject vfx = SpawnVFXPublic(tributeVFX, card.transform.position); if (vfx != null) vfx.transform.SetParent(card.transform); }
    }

    // Wrappers de Compatibilidade Retroativa para as Cinemáticas
    public void PlaySummonCinematic(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        SummonVFXType type = isTribute ? SummonVFXType.Tribute : (isSpecial ? SummonVFXType.Special : SummonVFXType.Normal);
        PlaySummonCinematic(card, type, onComplete);
    }

    public void PlaySummonCinematic(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(SummonCinematicRoutine(card, type, onComplete));
    }

    public void SpawnFieldMarker(Transform targetZone, SummonVFXType type)
    {
        SummonVFXPackage package = GetSummonPackage(type);
        if (package == null || !package.fieldMarker.useMarker) return;
        FieldMarkerSettings settings = package.fieldMarker;

        if (settings.usePrefab && settings.prefab != null)
        {
            GameObject vfx = SpawnVFXPublic(settings.prefab, targetZone.position, true, settings.color);
            if (vfx != null) 
            {
                // Retorna ao falso para não herdar distorções de tela/canvas
                vfx.transform.SetParent(targetZone, false);
                
                // Restaura o tamanho e as âncoras exatas do Prefab original que a Unity distorceu
                RectTransform rt = vfx.GetComponent<RectTransform>();
                RectTransform prefabRt = settings.prefab.GetComponent<RectTransform>();
                if (rt != null && prefabRt != null)
                {
                    rt.anchorMin = prefabRt.anchorMin; rt.anchorMax = prefabRt.anchorMax;
                    rt.pivot = prefabRt.pivot; rt.sizeDelta = prefabRt.sizeDelta;
                    rt.anchoredPosition = Vector2.zero;
                }
                else { vfx.transform.localPosition = Vector3.zero; }
                
                vfx.transform.SetAsFirstSibling();
                // Injeta a escala original do Prefab para a matemática não enlouquecer
                StartCoroutine(PrefabFieldMarkerRoutine(vfx, settings.prefab.transform.localScale, settings));
            }
        }
        if (settings.useNative) StartCoroutine(NativeFieldMarkerRoutine(targetZone, settings));
    }

    private IEnumerator PrefabFieldMarkerRoutine(GameObject vfx, Vector3 baseScale, FieldMarkerSettings settings)
    {
        float t = 0; 
        float duration = settings.duration / (animationSpeed > 0 ? animationSpeed : 1f);

        while (t < 1f) {
            if (vfx == null) break;
            t += Time.deltaTime / duration; 
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            float currentMultiplier = Mathf.Lerp(settings.startSize, settings.endSize, smooth);
            vfx.transform.localScale = baseScale * currentMultiplier;
            yield return null;
        } 
        if (vfx != null) Destroy(vfx); // Retorna a destruição manual para objetos 2D!
    }

    private IEnumerator NativeFieldMarkerRoutine(Transform targetZone, FieldMarkerSettings settings)
    {
        GameObject marker = new GameObject("NativeFieldMarker", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(targetZone, false);
        marker.transform.SetAsFirstSibling();
        
        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200, 200); 
        
        Image img = marker.GetComponent<Image>();
        img.color = new Color(settings.color.r, settings.color.g, settings.color.b, 0f);
        Outline outline = marker.AddComponent<Outline>();
        outline.effectColor = settings.color; outline.effectDistance = new Vector2(5f, -5f);
        
        float t = 0; float duration = settings.duration / (animationSpeed > 0 ? animationSpeed : 1f);
        while (t < 1f) {
            if (marker == null) break;
            t += Time.deltaTime / duration; float smooth = Mathf.SmoothStep(0, 1, t);
            rt.localScale = Vector3.Lerp(Vector3.one * settings.startSize, Vector3.one * settings.endSize, smooth);
            Color c = settings.color; c.a = Mathf.Lerp(settings.color.a, 0f, smooth); outline.effectColor = c;
            yield return null;
        } 
        if (marker != null) Destroy(marker);
    }

    private IEnumerator SummonCinematicRoutine(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        SummonVFXPackage package = GetSummonPackage(type);
        bool useDark = package == null || package.cinematic.useDarkOverlay;
        bool useWhiteFlash = package != null && package.cinematic.useWhiteFlash;

        float fadeDur = package != null ? package.cinematic.darkOverlayFadeDuration : 0.3f;
        float flashDur = package != null ? package.cinematic.flashDuration : 0.4f;
        float appearDur = package != null ? package.cinematic.giantCardAppearDuration : 0.3f;
        float holdDur = package != null ? package.cinematic.giantCardHoldDuration : 0.6f;
        float delayMarker = package != null ? package.cinematic.delayBeforeMarker : 0f;
        float delayDrop = package != null ? package.cinematic.delayBeforeCardDrop : 0.3f;
        float cardScale = package != null ? package.cinematic.giantCardScale : 1.5f;
        float symScale = package != null ? package.cinematic.symbolScale : 400f;
        bool preserveAspect = package == null || package.cinematic.preserveBackgroundAspect;

        GameObject darkOverlay = null; Image darkImg = null;
        GameObject symbolObj = null;
        GameObject cinCard = null;
        
        System.Action Cleanup = () => {
            if (darkOverlay) Destroy(darkOverlay);
            if (symbolObj) Destroy(symbolObj);
            if (cinCard) Destroy(cinCard);
        };

        if (useDark)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(uiParent, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
            rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;

        while (t < fadeDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / fadeDur)); 
            yield return null; 
        }

        if (package != null && package.cinematic.backgroundSymbol != null) { 
            symbolObj = new GameObject("CinematicSymbol", typeof(RectTransform), typeof(Image)); 
            symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(symScale, symScale);
            Image imgSym = symbolObj.GetComponent<Image>(); 
            imgSym.sprite = package.cinematic.backgroundSymbol; 
            imgSym.preserveAspect = preserveAspect;
            imgSym.color = new Color(1, 1, 1, 0.6f); 
            if (package.cinematic.backgroundMaterial != null) imgSym.material = package.cinematic.backgroundMaterial;
        }

        cinCard = new GameObject("CinematicCard", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false);
        cinCard.transform.SetAsLastSibling();
        
        RectTransform rt = cinCard.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 290f);
        
        RawImage ri = cinCard.GetComponent<RawImage>();
        
        bool showBack = !card.isPlayerCard;
        if (showBack && GameManager.Instance != null) ri.texture = GameManager.Instance.GetCardBackTexture();
        else ri.texture = card.cardImage.texture;

        PlaySound(summonSound);
        t = 0;

        while (t < appearDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / appearDur);
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * cardScale, p);
            if (symbolObj != null && package.cinematic.spinBackgroundSymbol)
                symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed);
            yield return null; 
        }

        if (useWhiteFlash) StartCoroutine(NativeWhiteFlashRoutine(uiParent, animSpeed, flashDur));

        float holdTimer = 0;
        while (holdTimer < holdDur) {
            if (card == null) { Cleanup(); yield break; }
            holdTimer += Time.deltaTime * animSpeed;
            if (symbolObj != null && package.cinematic.spinBackgroundSymbol)
                symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed);
            yield return null;
        }

        float markerTimer = 0;
        while (markerTimer < delayMarker) {
            if (card == null) { Cleanup(); yield break; }
            markerTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        Transform targetZone = card.transform.parent != null ? card.transform.parent : card.transform;
        SpawnFieldMarker(targetZone, type);

        float dropTimer = 0;
        while (dropTimer < delayDrop) {
            if (card == null) { Cleanup(); yield break; }
            dropTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        PlaySound(attackTravelSound);

        Vector3 startPos = rt.position; 
        Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p);
            rt.localScale = Vector3.Lerp(Vector3.one * cardScale, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p));
            if (symbolObj != null) {
                Image imgSym = symbolObj.GetComponent<Image>();
                imgSym.color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p));
            }
            yield return null; 
        }

        Cleanup();
        onComplete?.Invoke();
    }

    public void PlayFusionCinematic(CardDisplay card, List<CardData> materials, CardData polyCard, System.Action onComplete)
    {
        PlayFusionCinematic(card, materials, SummonVFXType.Fusion, onComplete);
    }

    public void PlayFusionCinematic(CardDisplay card, List<CardData> materials, SummonVFXType type, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(FusionCinematicRoutine(card, materials, type, onComplete));
    }

    private IEnumerator FusionCinematicRoutine(CardDisplay card, List<CardData> materials, SummonVFXType type, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        SummonVFXPackage package = GetSummonPackage(type);
        bool useDark = package == null || package.cinematic.useDarkOverlay;
        bool useWhiteFlash = package != null && package.cinematic.useWhiteFlash;

        float fadeDur = package != null ? package.cinematic.darkOverlayFadeDuration : 0.3f;
        float orbitDur = package != null ? package.cinematic.orbitDuration : 1.5f;
        float orbitSpeed = package != null ? package.cinematic.cardsOrbitSpeed : 1080f;
        float flashDur = package != null ? package.cinematic.flashDuration : 0.4f;
        float appearDur = package != null ? package.cinematic.giantCardAppearDuration : 0.3f;
        float holdDur = package != null ? package.cinematic.giantCardHoldDuration : 0.6f;
        float delayMarker = package != null ? package.cinematic.delayBeforeMarker : 0f;
        float delayDrop = package != null ? package.cinematic.delayBeforeCardDrop : 0.3f;
        float cardScale = package != null ? package.cinematic.giantCardScale : 1.5f;
        float symScale = package != null ? package.cinematic.symbolScale : 400f;
        bool preserveAspect = package == null || package.cinematic.preserveBackgroundAspect;

        GameObject darkOverlay = null; Image darkImg = null;

        GameObject symbolObj = null;
        GameObject cinCard = null;
        List<RectTransform> matRects = new List<RectTransform>();
        
        System.Action Cleanup = () => {
            if (darkOverlay) Destroy(darkOverlay);
            if (symbolObj) Destroy(symbolObj);
            if (cinCard) Destroy(cinCard);
            foreach (var m in matRects) { if (m) Destroy(m.gameObject); }
        };
        
        if (useDark)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(uiParent, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
            rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < fadeDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / fadeDur)); 
            yield return null; 
        }

        if (package != null && package.cinematic.backgroundSymbol != null) { 
            symbolObj = new GameObject("FusionSymbol", typeof(RectTransform), typeof(Image)); 
            symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(symScale, symScale);
            Image imgSym = symbolObj.GetComponent<Image>(); 
            imgSym.sprite = package.cinematic.backgroundSymbol; 
            imgSym.preserveAspect = preserveAspect;
            imgSym.color = new Color(1, 1, 1, 0);             
            if (package.cinematic.backgroundMaterial != null) imgSym.material = package.cinematic.backgroundMaterial;
        }

        bool faceUpMats = package == null || package.cinematic.showMaterialsFaceUp;

        if (materials != null && materials.Count > 0 && GameManager.Instance != null)
        {
            foreach (var mat in materials)
            {
                GameObject matObj;
                if (GameManager.Instance.cardPrefab != null)
                {
                    matObj = Instantiate(GameManager.Instance.cardPrefab, uiParent);
                    CardDisplay cd = matObj.GetComponent<CardDisplay>();
                    if (cd != null) { cd.SetCard(mat, GameManager.Instance.GetCardBackTexture(), faceUpMats); cd.isInteractable = false; }
                    LayoutElement le = matObj.GetComponent<LayoutElement>(); if (le != null) Destroy(le);
                }
                else
                {
                    matObj = new GameObject("MatCard", typeof(RectTransform), typeof(RawImage));
                    matObj.transform.SetParent(uiParent, false);
                    matObj.GetComponent<RawImage>().texture = GameManager.Instance.GetCardBackTexture();
                }

                RectTransform rtMat = matObj.GetComponent<RectTransform>(); 
                rtMat.anchorMin = new Vector2(0.5f, 0.5f); rtMat.anchorMax = new Vector2(0.5f, 0.5f);
                rtMat.anchoredPosition = Vector2.zero; rtMat.sizeDelta = new Vector2(100f, 145f);
                rtMat.localScale = Vector3.one;
                matRects.Add(rtMat);
            }
        }

        if (matRects.Count > 0)
        {
            PlaySound(spellSound);
            float orbitTime = 0f;
            while (orbitTime < orbitDur)
            {
                if (card == null) { Cleanup(); yield break; }
                orbitTime += Time.deltaTime * animSpeed; float progress = orbitTime / orbitDur;
                float radius = Mathf.Lerp(300f, 0f, progress * progress); 
                if (symbolObj != null) {
                    symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, progress)); 
                    if (package != null && package.cinematic.spinBackgroundSymbol)
                        symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed); 
                }
                for (int i = 0; i < matRects.Count; i++) {
                    float offsetAngle = (360f / matRects.Count) * i; float currentAngle = (orbitTime * orbitSpeed) + offsetAngle;
                    float x = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius; float y = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
                    matRects[i].anchoredPosition = new Vector2(x, y); matRects[i].rotation = Quaternion.Euler(0, 0, currentAngle);
                }
                yield return null;
            }
        }
        else {
            float waitT = 0;
            while (waitT < 0.5f) {
                if (card == null) { Cleanup(); yield break; }
                waitT += Time.deltaTime * animSpeed;
                yield return null;
            }
        }

        foreach (var m in matRects) if (m) Destroy(m.gameObject);
        matRects.Clear();

        PlaySound(fusionSound);
        Vector3 worldCenter = symbolObj != null ? symbolObj.transform.position : uiParent.position;
        if (package != null && package.cinematic.usePrefab && package.cinematic.flashPrefab != null) SpawnVFXPublic(package.cinematic.flashPrefab, worldCenter);
        if (useWhiteFlash) StartCoroutine(NativeWhiteFlashRoutine(uiParent, animSpeed, flashDur));

        cinCard = new GameObject("CinematicFusion", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false);
        cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); 
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>();
        ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < appearDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / appearDur); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * cardScale, p); 
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); 
            yield return null; 
        }

        float holdTimer = 0;
        while (holdTimer < holdDur) {
            if (card == null) { Cleanup(); yield break; }
            holdTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        float markerTimer = 0;
        while (markerTimer < delayMarker) {
            if (card == null) { Cleanup(); yield break; }
            markerTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        Transform targetZone = card.transform.parent != null ? card.transform.parent : card.transform;
        SpawnFieldMarker(targetZone, type);

        float dropTimer = 0;
        while (dropTimer < delayDrop) {
            if (card == null) { Cleanup(); yield break; }
            dropTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * cardScale, targetScale, p); rt.rotation = Quaternion.Slerp(startRot, targetRot, p); 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; 
        }

        Cleanup();       
        onComplete?.Invoke();
    }

    public void PlayRitualCinematic(CardDisplay card, CardData ritualSpell, System.Action onComplete)
    {
        PlayRitualCinematic(card, SummonVFXType.Ritual, onComplete);
    }

    public void PlayRitualCinematic(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(RitualCinematicRoutine(card, type, onComplete));
    }

    private IEnumerator RitualCinematicRoutine(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        SummonVFXPackage package = GetSummonPackage(type);
        bool useDark = package == null || package.cinematic.useDarkOverlay;
        bool useWhiteFlash = package != null && package.cinematic.useWhiteFlash;

        float fadeDur = package != null ? package.cinematic.darkOverlayFadeDuration : 0.3f;
        float orbitDur = package != null ? package.cinematic.orbitDuration : 1.0f; // Usado para o tempo do símbolo na tela
        float flashDur = package != null ? package.cinematic.flashDuration : 0.4f;
        float appearDur = package != null ? package.cinematic.giantCardAppearDuration : 0.3f;
        float holdDur = package != null ? package.cinematic.giantCardHoldDuration : 0.6f;
        float delayMarker = package != null ? package.cinematic.delayBeforeMarker : 0f;
        float delayDrop = package != null ? package.cinematic.delayBeforeCardDrop : 0.3f;
        float cardScale = package != null ? package.cinematic.giantCardScale : 1.5f;
        float symScale = package != null ? package.cinematic.symbolScale : 400f;
        bool preserveAspect = package == null || package.cinematic.preserveBackgroundAspect;

        GameObject darkOverlay = null; Image darkImg = null;
        GameObject symbolObj = null;
        GameObject cinCard = null;

        System.Action Cleanup = () => {
            if (darkOverlay) Destroy(darkOverlay);
            if (symbolObj) Destroy(symbolObj);
            if (cinCard) Destroy(cinCard);
        };

        if (useDark)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image)); 
            darkOverlay.transform.SetParent(uiParent, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>(); 
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
            rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>(); darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < fadeDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / fadeDur)); 
            yield return null; 
        }

        if (package != null && package.cinematic.backgroundSymbol != null) { symbolObj = new GameObject("RitualSymbol", typeof(RectTransform), typeof(Image)); symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(symScale, symScale);
            Image imgSym = symbolObj.GetComponent<Image>(); 
            imgSym.sprite = package.cinematic.backgroundSymbol; 
            imgSym.preserveAspect = preserveAspect;
            imgSym.color = new Color(1, 1, 1, 0); 
            if (package.cinematic.backgroundMaterial != null) imgSym.material = package.cinematic.backgroundMaterial; 
        }

        PlaySound(spellSound); t = 0;
        while (t < orbitDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (symbolObj != null) {
                symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, t / orbitDur)); 
                if (package != null && package.cinematic.spinBackgroundSymbol)
                    symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed); 
            } 
            yield return null; 
        }

        float waitT = 0;
        while (waitT < 0.3f) {
            if (card == null) { Cleanup(); yield break; }
            waitT += Time.deltaTime * animSpeed;
            yield return null;
        }

        PlaySound(fusionSound); 
        Vector3 worldCenter = symbolObj != null ? symbolObj.transform.position : uiParent.position;
        if (package != null && package.cinematic.usePrefab && package.cinematic.flashPrefab != null) SpawnVFXPublic(package.cinematic.flashPrefab, worldCenter);
        if (useWhiteFlash) StartCoroutine(NativeWhiteFlashRoutine(uiParent, animSpeed, flashDur));

        cinCard = new GameObject("CinematicRitual", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false); cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); 
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>(); ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < appearDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / appearDur); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * cardScale, p);
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); 
            yield return null; 
        }

        float holdTimer = 0;
        while (holdTimer < holdDur) {
            if (card == null) { Cleanup(); yield break; }
            holdTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        float markerTimer = 0;
        while (markerTimer < delayMarker) {
            if (card == null) { Cleanup(); yield break; }
            markerTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        Transform targetZone = card.transform.parent != null ? card.transform.parent : card.transform;
        SpawnFieldMarker(targetZone, type);

        if (symbolObj != null) { Destroy(symbolObj); symbolObj = null; }
        
        float dropTimer = 0;
        while (dropTimer < delayDrop) {
            if (card == null) { Cleanup(); yield break; }
            dropTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f)); rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * cardScale, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; 
        }

        Cleanup();
        onComplete?.Invoke();
    }

    private IEnumerator NativeWhiteFlashRoutine(Transform uiParent, float animSpeed, float baseDuration)
    {
        GameObject flashOverlay = new GameObject("WhiteFlashOverlay", typeof(RectTransform), typeof(Image));
        flashOverlay.transform.SetParent(uiParent, false);
        flashOverlay.transform.SetAsLastSibling(); 
        
        RectTransform rtFlash = flashOverlay.GetComponent<RectTransform>();
        rtFlash.anchorMin = Vector2.zero; rtFlash.anchorMax = Vector2.one; 
        rtFlash.offsetMin = Vector2.zero; rtFlash.offsetMax = Vector2.zero;
        
        Image flashImg = flashOverlay.GetComponent<Image>();
        flashImg.color = new Color(1, 1, 1, 1f); 

        float duration = baseDuration / animSpeed;
        float t = 0;
        while (t < duration) { t += Time.deltaTime; flashImg.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t / duration)); yield return null; }
        
        Destroy(flashOverlay);
    }
}