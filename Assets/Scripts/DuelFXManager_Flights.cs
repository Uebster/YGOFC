using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DuelFXManager
{
    // ==============================================================================
    // CARD FLIGHTS
    // ==============================================================================
    
    public void PlayEquipEffect(CardDisplay source, CardDisplay target)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayEquipEffect");
        if (!enableAnimations || source == null || target == null) return; // FIX: Removida a exigência desnecessária do boardCenter
        
        if (useEquipPrefab && equipImpactVFX != null)
        {
            SpawnVFXPublic(equipImpactVFX, target.transform.position);
        }

        if (useEquipGhost)
        {
            StartCoroutine(EquipGhostRoutine(source, target));
        }
    }

    private IEnumerator EquipGhostRoutine(CardDisplay source, CardDisplay target)
    {
        // Cria o fantasma da carta mágica
        GameObject ghost = new GameObject("EquipGhost", typeof(RectTransform), typeof(RawImage));
        
        // FIX: Clona a hierarquia exata primeiro para herdar as âncoras e a escala (ex: 0.8) da carta original!
        Transform originalParent = source.transform.parent;
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform sourceRT = source.GetComponent<RectTransform>();
        rt.anchorMin = sourceRT.anchorMin; rt.anchorMax = sourceRT.anchorMax; rt.pivot = sourceRT.pivot;
        rt.sizeDelta = sourceRT.sizeDelta; rt.anchoredPosition = sourceRT.anchoredPosition;
        rt.localRotation = sourceRT.localRotation; rt.localScale = sourceRT.localScale;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = source.cardImage.texture; 
        ri.color = new Color(1f, 1f, 1f, 0.8f); // Fantasma semitransparente

        // Movemos para o Canvas raiz usando worldPositionStays=true para voar por cima de tudo preservando as proporções
        Transform uiParent = GetUIParent();
        if (uiParent != null) { 
            ghost.transform.SetParent(uiParent, true); 
            ghost.transform.SetAsLastSibling(); 
        }

        PlaySound(spellSound);
        float duration = equipGhostFlightDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0; 
        Vector3 startPos = rt.position; 
        Vector3 startScale = rt.localScale;
        
        // Calcula o scale final absoluto ajustado para a mesma perspectiva do pai da UI
        Vector3 endScale = target.transform.lossyScale;
        if (uiParent != null) {
            endScale = new Vector3(
                target.transform.lossyScale.x / uiParent.lossyScale.x,
                target.transform.lossyScale.y / uiParent.lossyScale.y,
                target.transform.lossyScale.z / uiParent.lossyScale.z
            );
        }
        
        float startZ = rt.eulerAngles.z;
        float targetZ = target.transform.eulerAngles.z;
        
        while (t < 1f) {
            if (target == null || target.gameObject == null || rt == null) break;
            t += Time.deltaTime / duration; 
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // FIX: Viagem em linha reta! Em Top-Down, a sensação de parábola (altura/subida) vem SÓ da Escala.
            rt.position = Vector3.Lerp(startPos, target.transform.position, smoothT);
            
            // Desliza suavemente para a rotação alvo sem giros extras (0 ou 180 graus dependendo do lado do campo)
            float currentZ = Mathf.LerpAngle(startZ, targetZ, smoothT);
            rt.rotation = Quaternion.Euler(0, 0, currentZ); 
            
            // Escala: Incha no meio do caminho (simulando "Subir" em direção à câmera) e desce para o tamanho do alvo
            float scaleMult = 1f + Mathf.Sin(smoothT * Mathf.PI) * (equipGhostFlightScale - 1f);
            rt.localScale = Vector3.Lerp(startScale, endScale, smoothT) * scaleMult; 
            
            yield return null;
        }

        if (ghost != null) Destroy(ghost);

        // Impacto de encaixe preto (Outline Squeeze)
        if (target != null)
        {
            StartCoroutine(EquipOutlineSqueezeRoutine(target));
        }
    }

    private IEnumerator EquipOutlineSqueezeRoutine(CardDisplay target)
    {
        // Cria a "Sombra de Encaixe" (Squeeze) atrás da carta
        GameObject squeezeObj = new GameObject("EquipSqueeze", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        
        Transform parentToUse = target.transform.parent;
        squeezeObj.transform.SetParent(parentToUse, false);
        // Fica logo atrás do alvo
        squeezeObj.transform.SetSiblingIndex(target.transform.GetSiblingIndex()); 
        
        RectTransform rt = squeezeObj.GetComponent<RectTransform>();
        RectTransform targetRT = target.GetComponent<RectTransform>();
        
        rt.position = targetRT.position;
        rt.sizeDelta = targetRT.sizeDelta;
        rt.rotation = targetRT.rotation;
        rt.pivot = targetRT.pivot;

        RawImage ri = squeezeObj.GetComponent<RawImage>();
        ri.texture = target.cardImage.texture;
        ri.color = equipSqueezeColor; // Sombra customizada

        Outline outline = squeezeObj.GetComponent<Outline>();
        outline.effectColor = equipSqueezeColor;
        outline.effectDistance = new Vector2(10f, -10f);
        
        float duration = 0.2f / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        
        Vector3 startScale = target.transform.localScale * equipSqueezeScale;
        Vector3 endScale = target.transform.localScale;
        
        while (t < 1f)
        {
            if (target == null) break;
            t += Time.deltaTime / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            rt.position = targetRT.position; 
            rt.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            
            // Esmaece no último quarto da animação para um encaixe suave
            if (smoothT > 0.7f)
            {
                float fadeT = (smoothT - 0.7f) / 0.3f;
                
                Color currentImgColor = equipSqueezeColor;
                currentImgColor.a = (1f - fadeT) * equipSqueezeColor.a;
                ri.color = currentImgColor;
                
                Color outlineColor = equipSqueezeColor;
                outlineColor.a = (1f - fadeT) * equipSqueezeColor.a;
                outline.effectColor = outlineColor;
            }
            
            yield return null;
        }

        if (squeezeObj != null) Destroy(squeezeObj);
    }

    public void PlayShuffleEffect(Transform pileTransform)
    {
        if (!enableAnimations || pileTransform == null) return;
        // Debug.Log($"[VFX] > [CHAMADA] PlayShuffleEffect");
        
        if (useShuffleRoutine || useCustomHinduShuffle || useSimple2DShuffle || useShufflePrefab)
        {
            StartCoroutine(ShuffleRoutine(pileTransform));
        }
        else
        {
            PlaySound(shuffleSound);
        }
    }

    // Usado pelo GameManager para saber EXATAMENTE quando liberar as compras
    public float GetShuffleTotalDuration()
    {
        float timePerLoop = shuffleDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float extraFadeOutTime = 0f;

        if (useShufflePrefab && shuffleVFX != null)
        {
            ParticleSystem ps = shuffleVFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                if (ps.main.duration > timePerLoop) timePerLoop = ps.main.duration;
                extraFadeOutTime = ps.main.startLifetime.constantMax;
            }
        }
        return (timePerLoop * Mathf.Max(1, shuffleLoopCount)) + extraFadeOutTime + shuffleDeckAppearDelay + shufflePostDelay;
    }

    private IEnumerator ShuffleRoutine(Transform pileTransform)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null && useShuffleRoutine) yield break;

        Vector3 startPos = pileTransform.position;
        Vector3 centerPos = startPos;
        if (shuffleAtCenter)
        {
            if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
            {
                bool isPlayerDeck = (pileTransform == GameManager.Instance.duelFieldUI.playerDeck);
                if (isPlayerDeck && playerBoardCenter != null) centerPos = playerBoardCenter.position;
                else if (!isPlayerDeck && opponentBoardCenter != null) centerPos = opponentBoardCenter.position;
                else if (boardCenter != null) centerPos = boardCenter.position;
            }
            else if (boardCenter != null) centerPos = boardCenter.position;
        }
        RectTransform pileRT = pileTransform.GetComponent<RectTransform>();

        // Ocultar o deck real temporariamente para evitar z-fighting e tremeliques com o VFX
        CanvasGroup pileCG = pileTransform.GetComponent<CanvasGroup>();
        bool addedCG = false;
        if (pileCG == null) { pileCG = pileTransform.gameObject.AddComponent<CanvasGroup>(); addedCG = true; }
        float originalAlpha = pileCG.alpha;
        pileCG.alpha = 0f;

        float routineDuration = shuffleDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        int loops = Mathf.Max(1, shuffleLoopCount);
        float totalTime = routineDuration * loops;
        float extraFadeOutTime = 0f;

            if (useShufflePrefab && shuffleVFX != null)
            {
                ParticleSystem ps = shuffleVFX.GetComponent<ParticleSystem>();
                if (ps != null) extraFadeOutTime = ps.main.startLifetime.constantMax;
            }

        bool runRoutine = useShuffleRoutine || useCustomHinduShuffle || useSimple2DShuffle;

        bool isOpponentDeck = (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null && pileTransform == GameManager.Instance.duelFieldUI.opponentDeck);
        Quaternion basePileRot = pileTransform.rotation;
        if (isOpponentDeck && shuffleOpponent180) basePileRot *= Quaternion.Euler(0, 0, 180f);

        if (runRoutine && uiParent != null)
            {
                int deckSize = useCustomHinduShuffle ? 40 : (useSimple2DShuffle ? 5 : 6); 
                List<GameObject> fakeCards = new List<GameObject>();

                for (int i = 0; i < deckSize; i++)
                {
                    GameObject card = new GameObject($"FakeCard_{i}", typeof(RectTransform), typeof(RawImage));
                    if (pileRT != null && pileRT.parent != null)
                    {
                        card.transform.SetParent(pileRT.parent, false);
                        RectTransform rt = card.GetComponent<RectTransform>();
                        rt.anchorMin = pileRT.anchorMin; rt.anchorMax = pileRT.anchorMax; rt.pivot = pileRT.pivot;
                        rt.sizeDelta = pileRT.sizeDelta; rt.anchoredPosition = pileRT.anchoredPosition; rt.rotation = basePileRot; rt.localScale = pileRT.localScale;
                    }
                    card.transform.SetParent(uiParent, true); 
                    card.transform.SetAsLastSibling();

                    RawImage ri = card.GetComponent<RawImage>();
                    ri.texture = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;
                    fakeCards.Add(card);
                }

                if (useCustomHinduShuffle)
                {
                    Vector3 basePos = centerPos;
                float moveCenterTime = shuffleAtCenter ? 0.2f : 0f; 
                Quaternion stackRot = basePileRot * Quaternion.Euler(0, 0, customShuffleTiltAngle);
                    
                    System.Func<Vector3, int, Vector3> GetStackPos = (center, index) => {
                        Vector3 offset = stackRot * new Vector3(index * -0.4f, index * 0.8f, 0); 
                        return center + offset;
                    };

                if (shuffleAtCenter)
                {
                    float moveT = 0;
                    while (moveT < 1f) {
                        moveT += Time.deltaTime / moveCenterTime;
                        float smooth = Mathf.SmoothStep(0, 1, moveT);
                        for (int i = 0; i < deckSize; i++) {
                            fakeCards[i].transform.position = Vector3.Lerp(startPos, GetStackPos(basePos, i), smooth);
                            fakeCards[i].transform.rotation = Quaternion.Lerp(basePileRot, stackRot, smooth);
                            float scaleMult = 1f + (smooth * (shuffleScaleMultiplier - 1f));
                            fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                        }
                        yield return null;
                    }
                }
                else
                {
                    basePos = startPos;
                    for (int i = 0; i < deckSize; i++) {
                        fakeCards[i].transform.position = GetStackPos(basePos, i);
                        fakeCards[i].transform.rotation = stackRot; // Já está correto
                    }
                }

                int movesPerLoop = 5;
                int totalMoves = movesPerLoop * loops;
                float shufflePhaseTime = Mathf.Max(0.1f, totalTime - (moveCenterTime * 2f));
                float moveDuration = shufflePhaseTime / totalMoves;
                float stepDur = moveDuration / 3f;
                Vector3 rightOffset = basePileRot * new Vector3(110f, 0, 0);

                for (int m = 0; m < totalMoves; m++)
                {
                    if (m % movesPerLoop == 0)
                    {
                        if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);
                    }

                    int packetSize = 10;
                    int extractIdx = 10 + (m % 2) * 5; 
                    List<GameObject> packet = fakeCards.GetRange(extractIdx, packetSize);
                    fakeCards.RemoveRange(extractIdx, packetSize);

                        float t = 0;
                        while (t < 1f) {
                            t += Time.deltaTime / stepDur;
                            float smooth = Mathf.SmoothStep(0, 1, t);
                            for (int i = 0; i < packet.Count; i++) packet[i].transform.position = Vector3.Lerp(GetStackPos(basePos, extractIdx + i), GetStackPos(basePos, extractIdx + i) + rightOffset, smooth);
                            yield return null;
                        }

                        for (int i = 0; i < packet.Count; i++) packet[i].transform.SetAsLastSibling();
                        
                        t = 0;
                        while (t < 1f) {
                            t += Time.deltaTime / stepDur;
                            float smooth = Mathf.SmoothStep(0, 1, t);
                            for (int i = 0; i < packet.Count; i++) packet[i].transform.position = Vector3.Lerp(GetStackPos(basePos, extractIdx + i) + rightOffset, GetStackPos(basePos, fakeCards.Count + i) + rightOffset, smooth);
                            for (int i = extractIdx; i < fakeCards.Count; i++) fakeCards[i].transform.position = Vector3.Lerp(GetStackPos(basePos, i + packetSize), GetStackPos(basePos, i), smooth);
                            yield return null;
                        }

                        fakeCards.AddRange(packet);
                        t = 0;
                        while (t < 1f) {
                            t += Time.deltaTime / stepDur;
                            float smooth = Mathf.SmoothStep(0, 1, t);
                            for (int i = 0; i < packet.Count; i++) packet[i].transform.position = Vector3.Lerp(GetStackPos(basePos, fakeCards.Count - packetSize + i) + rightOffset, GetStackPos(basePos, fakeCards.Count - packetSize + i), smooth);
                            yield return null;
                        }
                    }
                if (shuffleAtCenter)
                {
                    float moveT = 0;
                    while (moveT < 1f) {
                        moveT += Time.deltaTime / moveCenterTime;
                        float smooth = Mathf.SmoothStep(0, 1, moveT);
                        for (int i = 0; i < deckSize; i++) {
                            fakeCards[i].transform.position = Vector3.Lerp(GetStackPos(basePos, i), startPos, smooth);
                            fakeCards[i].transform.rotation = Quaternion.Lerp(stackRot, basePileRot, smooth);
                            float scaleMult = 1f + ((1f - smooth) * (shuffleScaleMultiplier - 1f));
                            fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                        }
                        yield return null;
                    }
                }
            }
            else if (useSimple2DShuffle)
            {
                Vector3 basePos = centerPos;
                float moveCenterTime = shuffleAtCenter ? 0.2f : 0f;
                
                    if (shuffleAtCenter)
                    {
                        float moveT = 0;
                        while (moveT < 1f) {
                            moveT += Time.deltaTime / moveCenterTime;
                            foreach(var c in fakeCards) {
                                c.transform.position = Vector3.Lerp(startPos, basePos, Mathf.SmoothStep(0, 1, moveT));
                                float scaleMult = 1f + (moveT * (shuffleScaleMultiplier - 1f));
                                c.transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                            }
                            yield return null;
                        }
                }
                else
                {
                    basePos = startPos;
                }

                int movesPerLoop = 6;
                int totalMoves = movesPerLoop * loops;
                float shufflePhaseTime = Mathf.Max(0.1f, totalTime - (moveCenterTime * 2f));
                float moveDuration = shufflePhaseTime / totalMoves;
                float halfMove = moveDuration / 2f;
                
                for (int m = 0; m < totalMoves; m++)
                {
                    if (m % movesPerLoop == 0)
                    {
                        if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);
                    }

                    GameObject movingCard = fakeCards[fakeCards.Count - 1];
                    fakeCards.RemoveAt(fakeCards.Count - 1);
                    
                    float yOffset = (m % 2 == 0) ? shuffle2DOffset.y : -shuffle2DOffset.y;
                    float xOffset = shuffle2DOffset.x;
                    Vector3 peakPos = basePos + (basePileRot * new Vector3(xOffset, yOffset, 0f));
                    
                    PlaySound(shuffleSound);
                    
                    float t = 0;
                    while(t < 1f) {
                        t += Time.deltaTime / halfMove;
                        movingCard.transform.position = Vector3.Lerp(basePos, peakPos, Mathf.SmoothStep(0, 1, t));
                        yield return null;
                    }
                    
                    movingCard.transform.SetSiblingIndex(fakeCards[0].transform.GetSiblingIndex());
                    fakeCards.Insert(0, movingCard);
                    
                    t = 0;
                    while(t < 1f) {
                        t += Time.deltaTime / halfMove;
                        movingCard.transform.position = Vector3.Lerp(peakPos, basePos, Mathf.SmoothStep(0, 1, t));
                        yield return null;
                    }
                }

                if (shuffleAtCenter)
                {
                    float moveT = 0;
                    while (moveT < 1f) {
                        moveT += Time.deltaTime / moveCenterTime;
                        foreach(var c in fakeCards) {
                            c.transform.position = Vector3.Lerp(basePos, startPos, Mathf.SmoothStep(0, 1, moveT));
                            float scaleMult = 1f + ((1f - moveT) * (shuffleScaleMultiplier - 1f));
                            c.transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                        }
                        yield return null;
                    }
                }
            }
            else
            {
                float timePerLoop = totalTime / loops;
                float halfDuration = timePerLoop / 2f;

                for (int loopIndex = 0; loopIndex < loops; loopIndex++)
                {
                    PlaySound(shuffleSound); 
                    if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);

                    float t = 0;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / halfDuration;
                        float smooth = Mathf.SmoothStep(0, 1, t);
                        Vector3 currentBasePos = Vector3.Lerp(startPos, centerPos, smooth);

                        for (int i = 0; i < deckSize; i++)
                        {
                            if (fakeCards[i] == null) continue;
                            float xTargetOffset = (i % 2 == 0) ? -60f : 60f;
                            float yTargetOffset = Mathf.Sin(smooth * Mathf.PI) * 40f; 
                            float rotationZ = (i % 2 == 0) ? Mathf.Lerp(0, 20f, smooth) : Mathf.Lerp(0, -20f, smooth);
                            
                            fakeCards[i].transform.position = currentBasePos + new Vector3(xTargetOffset * smooth, yTargetOffset * smooth, 0);
                            fakeCards[i].transform.rotation = basePileRot * Quaternion.Euler(0, 0, rotationZ);

                            if (shuffleAtCenter)
                            {
                                float scaleMult = 1f + (smooth * (shuffleScaleMultiplier - 1f)); 
                                fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                            }
                        }
                        yield return null;
                    }
                    for (int i = 0; i < deckSize; i++)
                    {
                        if (fakeCards[i] == null) continue;
                        int siblingTarget = fakeCards[i].transform.parent.childCount - (i % 2 == 0 ? i : deckSize - i);
                        fakeCards[i].transform.SetSiblingIndex(siblingTarget);
                    }

                    t = 0;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / halfDuration;
                        float smooth = Mathf.SmoothStep(0, 1, t);
                        Vector3 currentBasePos = Vector3.Lerp(centerPos, startPos, smooth);

                        for (int i = 0; i < deckSize; i++)
                        {
                            if (fakeCards[i] == null) continue;
                            float xStartOffset = (i % 2 == 0) ? -60f : 60f;
                            float currentXOffset = Mathf.Lerp(xStartOffset, 0, smooth);
                            float rotationZ = (i % 2 == 0) ? Mathf.Lerp(20f, 0f, smooth) : Mathf.Lerp(-20f, 0f, smooth);
                            
                            fakeCards[i].transform.position = currentBasePos + new Vector3(currentXOffset, 0, 0);
                            fakeCards[i].transform.rotation = basePileRot * Quaternion.Euler(0, 0, rotationZ);

                            if (shuffleAtCenter)
                            {
                                float scaleMult = 1f + ((1f - smooth) * (shuffleScaleMultiplier - 1f)); 
                                fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                            }
                        }
                        yield return null;
                    }
                }
            }

            foreach (var c in fakeCards) Destroy(c);
        }
        else
        {
            float timePerLoop = totalTime / loops;
            for (int loopIndex = 0; loopIndex < loops; loopIndex++)
            {
                PlaySound(shuffleSound);
                if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);
                yield return new WaitForSeconds(timePerLoop);
            }
        }
        if (extraFadeOutTime > 0) yield return new WaitForSeconds(extraFadeOutTime);
        if (shuffleDeckAppearDelay > 0) yield return new WaitForSeconds(shuffleDeckAppearDelay);

        // Restaura a visibilidade do Deck real
        if (pileCG != null)
        {
            pileCG.alpha = originalAlpha;
            if (addedCG) Destroy(pileCG);
        }
    }
    
    public void PlayReturnToDeckAnimation(CardData data, bool isPlayer, CardLocation sourceLoc, Vector3? startPos, System.Action onComplete)
    {
        if (!enableAnimations) { onComplete?.Invoke(); return; }
        
        Vector3 sPos = startPos ?? (isPlayer ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position);
        Vector3 ePos = isPlayer ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
        
        CardFlightSettings settings = null;
        if (sourceLoc == CardLocation.Field) settings = flightFieldToDeck;
        else if (sourceLoc == CardLocation.Graveyard) settings = flightGraveyardToDeck;
        else if (sourceLoc == CardLocation.Banished) settings = flightBanishToDeck;
        else settings = flightGraveyardToDeck;
        bool pop = sourceLoc != CardLocation.Field && sourceLoc != CardLocation.Hand;
        
        PlayCardFlight(data, GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, settings, pop, onComplete);
    }

    public void PlayCardFlight(CardData data, Texture2D backTex, bool startFaceUp, bool endFaceUp, Vector3 startPos, Vector3 endPos, Vector3 startScale, Vector3 endScale, Quaternion startRot, Quaternion endRot, CardFlightSettings settings, bool popFromPile, System.Action onComplete)
    {
        if (!enableAnimations || settings == null || !settings.enableFlight) 

        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(CardFlightRoutine(data, backTex, startFaceUp, endFaceUp, startPos, endPos, startScale, endScale, startRot, endRot, settings, popFromPile, onComplete));
    }

    private IEnumerator CardFlightRoutine(CardData data, Texture2D backTex, bool startFaceUp, bool endFaceUp, Vector3 startPos, Vector3 endPos, Vector3 startScale, Vector3 endScale, Quaternion startRot, Quaternion endRot, CardFlightSettings settings, bool popFromPile, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        GameObject ghost = Instantiate(GameManager.Instance.cardPrefab, uiParent);
        CardDisplay ghostDisplay = ghost.GetComponent<CardDisplay>();
        ghostDisplay.SetCard(data, backTex, startFaceUp);
        ghostDisplay.isInteractable = false;
        
        LayoutElement le = ghost.GetComponent<LayoutElement>();
        if (le != null) Destroy(le);

        ghost.transform.SetAsLastSibling();
        PlaySound(spellSound);

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1f;
        Vector3 initialPos = startPos;
        
        if (popFromPile && settings.popOffset != Vector2.zero)
        {
            ghost.transform.position = startPos;
            ghost.transform.localScale = startScale * settings.startScaleMult;
            ghost.transform.rotation = startRot;

            float popDuration = 0.2f / animSpeed;
            float tPop = 0;
            
            // Aplica o Offset baseado no lado do jogador (esq/dir dependendo da pilha)
            float offsetX = startPos.x < Screen.width / 2f ? settings.popOffset.x : -settings.popOffset.x;
            Vector3 popTarget = startPos + new Vector3(offsetX, settings.popOffset.y, 0); 
            
            while (tPop < 1f)
            {
                if (ghost == null) { onComplete?.Invoke(); yield break; }
                tPop += Time.deltaTime / popDuration;
                float p = Mathf.SmoothStep(0, 1, tPop);
                Vector3 pos = Vector3.Lerp(startPos, popTarget, p);
                // Adiciona um pequeno arco
                pos.y += Mathf.Sin(p * Mathf.PI) * 15f;
                ghost.transform.position = pos;
                yield return null;
            }
            initialPos = popTarget;
        }

        float duration = settings.duration / animSpeed;
        float elapsed = 0f;
        
        GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
        if (settings.useTrail && settings.trailType == AttackTrailType.ContinuousLine) {
            lineObj = new GameObject("FlightLineTrail", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(ghost.transform.parent, true);
            lineObj.transform.SetSiblingIndex(ghost.transform.GetSiblingIndex());
            lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.pivot = new Vector2(0, 0.5f); lineRT.position = initialPos;
            lineImg = lineObj.GetComponent<Image>(); lineImg.color = settings.trailColor;
        }
        
        float spawnTrailTimer = 0;
        Vector3 finalStartScale = startScale * settings.startScaleMult;
        Vector3 finalEndScale = endScale * settings.endScaleMult;

        bool needsFlip = settings.flipDuringFlight && (startFaceUp != endFaceUp);
        bool hasFlipped = false;

        while (elapsed < duration)
        {
            if (ghost == null) 
            {
                if (lineObj != null) Destroy(lineObj);
                onComplete?.Invoke();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            ghost.transform.position = Vector3.Lerp(initialPos, endPos, t);
            ghost.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * (settings.flightScale - 1f);
            ghost.transform.localScale = Vector3.Lerp(finalStartScale, finalEndScale, t) * scaleMultiplier;

            // Lógica de Flip no meio do voo
            if (needsFlip && !hasFlipped)
            {
                float flipStartT = 0.4f;
                float flipEndT = 0.6f;
                if (t >= flipStartT && t <= flipEndT)
                {
                    float flipProgress = (t - flipStartT) / (flipEndT - flipStartT);
                    float scaleY = Mathf.Cos(flipProgress * Mathf.PI); // Vai de 1 a -1
                    
                    ghost.transform.localScale = new Vector3(ghost.transform.localScale.x, ghost.transform.localScale.y * Mathf.Abs(scaleY), ghost.transform.localScale.z);

                    if (flipProgress >= 0.5f) {
                        hasFlipped = true;
                        ghostDisplay.ForceTexture(endFaceUp ? ghostDisplay.GetFrontTexture() : backTex);
                    }
                }
            }

            if (settings.useTrail) {
                if (settings.trailType == AttackTrailType.Shadows || settings.trailType == AttackTrailType.SmoothShadows) {
                    spawnTrailTimer -= Time.deltaTime; 
                    float interval = settings.trailType == AttackTrailType.SmoothShadows ? 0.015f : 0.04f;
                    if (spawnTrailTimer <= 0) { 
                        spawnTrailTimer = interval; 
                        SpawnCardTrailGhost(ghost.GetComponent<RectTransform>(), ghostDisplay.cardImage.texture, settings.trailColor, settings.trailType == AttackTrailType.SmoothShadows ? 0.15f : 0.3f); 
                    }
                } else if (settings.trailType == AttackTrailType.ContinuousLine && lineObj != null) {
                    Vector3 currentPos = ghost.transform.position; Vector3 dirToCurrent = currentPos - initialPos;
                    float dist = dirToCurrent.magnitude; float canvasScale = lineObj.transform.lossyScale.x;
                    if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, settings.trailWidth);
                    lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                }
            }
            yield return null;
        }

        if (lineObj != null) StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));

        if (ghost != null)
        {
            ghost.transform.position = endPos;
            ghost.transform.rotation = endRot;
            ghost.transform.localScale = finalEndScale;

            if (settings.useImpact) 
            {
                if (settings.impactType == ControlSwapImpactType.Squeeze) yield return StartCoroutine(FlightImpactSqueezeRoutine(ghost, settings));
                else { StartCoroutine(PulseGhostRoutine(ghostDisplay, settings.impactScale, 0.2f, settings.impactColor, settings.impactOutlineWidth)); yield return new WaitForSeconds(0.2f); }
            }
            Destroy(ghost);
        }
        onComplete?.Invoke();
    }

    private IEnumerator FlightImpactSqueezeRoutine(GameObject card, CardFlightSettings settings)
    {
        GameObject squeezeObj = new GameObject("FlightSqueeze", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        Transform parentToUse = card.transform.parent;
        squeezeObj.transform.SetParent(parentToUse, false);
        squeezeObj.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 

        RectTransform rt = squeezeObj.GetComponent<RectTransform>();
        RectTransform targetRT = card.GetComponent<RectTransform>();
        rt.position = targetRT.position; rt.sizeDelta = targetRT.sizeDelta;
        rt.rotation = targetRT.rotation; rt.pivot = targetRT.pivot;

        RawImage ri = squeezeObj.GetComponent<RawImage>();
        ri.texture = card.GetComponent<RawImage>().texture; ri.color = settings.impactColor; 
        
        Outline outline = squeezeObj.GetComponent<Outline>();
        outline.effectColor = settings.impactColor; outline.effectDistance = new Vector2(settings.impactOutlineWidth, -settings.impactOutlineWidth);

        float halfDuration = 0.2f / (animationSpeed > 0 ? animationSpeed : 1f); float t = 0;
        Vector3 startScale = card.transform.localScale * settings.impactScale; Vector3 endScale = card.transform.localScale;

        while (t < 1f) {
            if (card == null) break; t += Time.deltaTime / halfDuration; float smooth = Mathf.SmoothStep(0, 1, t);
            rt.position = targetRT.position; rt.localScale = Vector3.Lerp(startScale, endScale, smooth);
            if (smooth > 0.7f) {
                float fadeT = (smooth - 0.7f) / 0.3f;
                Color currentImgColor = settings.impactColor; currentImgColor.a = (1f - fadeT) * settings.impactColor.a; ri.color = currentImgColor;
                Color outlineColor = settings.impactColor; outlineColor.a = (1f - fadeT) * settings.impactColor.a; outline.effectColor = outlineColor;
            } yield return null;
        }
        if (squeezeObj != null) Destroy(squeezeObj);
    }
}
