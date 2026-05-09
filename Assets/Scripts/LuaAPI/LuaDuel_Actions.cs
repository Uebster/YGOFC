using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class LuaDuel
{
    // ==============================================================================
    // ACTIONS, QUERIES & UI (O Baldão Temporário)
    // ==============================================================================

    public DynValue Destroy(object target, object reason)
    {
        int ocgReason = ConvertToInt(reason) | 0x1; // Adiciona REASON_DESTROY
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        List<CardDisplay> toDestroy = new List<CardDisplay>();

        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) if (c.unityCard != null) toDestroy.Add(c.unityCard);
            // Debug.Log($"[Lua] Duel.Destroy(Grupo com {group.cards.Count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            toDestroy.Add(card.unityCard);
            // Debug.Log($"[Lua] Duel.Destroy({card.unityCard.CurrentCardData.name})");
        }

        CardEffectManager.Instance.StartCoroutine(DestroyCardsRoutine(toDestroy, ocgReason));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Destroy") });
    }

    private Vector3 GetPilePosition(CardLocation loc, bool isPlayer)
    {
        if (GameManager.Instance == null) return Vector3.zero;
        switch (loc)
        {
            case CardLocation.Deck: return isPlayer ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
            case CardLocation.Graveyard: return isPlayer ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
            case CardLocation.ExtraDeck: return isPlayer ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position;
            case CardLocation.Banished: return isPlayer ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
            default: return Vector3.zero;
        }
    }

    // --- MOVIMENTAÇÃO DE CARTAS ---

    private void RestorePhysicalBodyIfMissing(LuaCard card)
    {
        if (card != null && card.unityCard == null && card.unityData != null && CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
        {
            card.unityCard = CardEffectManager.Instance.luaDuel.FindCardDisplayInPiles(card.unityData);
        }
    }

    public DynValue Draw(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        int pInt = ConvertToInt(player);

        if (pInt == 2 || pInt > 3) return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Draw") }); // PLAYER_NONE

        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        CardEffectManager.Instance.StartCoroutine(DrawRoutine(pInt, amt));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Draw") });
    }

    private IEnumerator DrawRoutine(int pInt, int amount)
    {
        int totalDrawn = 0;

        if (pInt == 0 || pInt == 3)
        {
            if (GameManager.Instance != null && GameManager.Instance.canPlayerDrawFromDeck && !GameManager.Instance.isSimulating)
            {
                for (int i = 0; i < amount; i++)
                {
                    int drawsLeft = amount - i;
                    if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Efeito ativado: Compre {drawsLeft} carta(s) do seu Deck.");
                    GameManager.Instance.pendingEffectDraws = 1;
                    yield return new WaitWhile(() => GameManager.Instance.pendingEffectDraws > 0);
                    yield return new WaitForSeconds(0.2f);
                    totalDrawn++;
                }
            }
            else
            {
                for (int i = 0; i < amount; i++) { GameManager.Instance.DrawCard(true); totalDrawn++; if (GameManager.Instance == null || !GameManager.Instance.isSimulating) yield return new WaitForSeconds(0.4f); }
            }
        }
        
        if (pInt == 1 || pInt == 3)
        {
            for (int i = 0; i < amount; i++) { GameManager.Instance.DrawOpponentCard(); totalDrawn++; if (GameManager.Instance == null || !GameManager.Instance.isSimulating) yield return new WaitForSeconds(0.4f); }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(pInt == 3 ? totalDrawn / 2 : totalDrawn);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public DynValue SendtoGrave(object target, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        CardEffectManager.Instance.StartCoroutine(SendtoGraveRoutine(target, reason));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SendtoGrave") });
    }

    private IEnumerator SendtoGraveRoutine(object target, object reason)
    {
        int ocgReason = ConvertToInt(reason);

        int count = 0;
        List<LuaCard> cardsToProcess = new List<LuaCard>();

        if (target is LuaGroup group)
        {
            cardsToProcess.AddRange(group.cards);
        }
        else if (target is LuaCard card)
        {
            cardsToProcess.Add(card);
        }

        foreach (var c in cardsToProcess)
        {
            RestorePhysicalBodyIfMissing(c);
            c.currentReason = ocgReason;
            if (c.unityCard != null && !c.unityCard.isInPile)
            {
                GameManager.Instance.MoveCard(c.unityCard, CardLocation.Graveyard, ocgReason);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
            else if (c.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sourceLoc = RemoveDataFromAllPiles(c, out wasPlayerPile);
                GameManager.Instance.SendToGraveyard(c.unityData, wasPlayerPile, sourceLoc, ocgReason);
                
                if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                {
                    GameObject excavatedGhost = CardExcavatedUI.Instance != null ? CardExcavatedUI.Instance.GetAndConsumeGhost(c.unityData) : null;
                    Vector3 startPos = c.unityCard != null ? c.unityCard.transform.position : (excavatedGhost != null ? excavatedGhost.transform.position : GetPilePosition(sourceLoc, wasPlayerPile));

                    CardFlightSettings flightSettings = null;
                    if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToGraveyard;
                    else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToGraveyard;
                    else if (sourceLoc == CardLocation.Banished) flightSettings = DuelFXManager.Instance.flightBanishToGraveyard;

                    if (flightSettings != null && flightSettings.enableFlight)
                    {
                        Vector3 endPos = wasPlayerPile ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
                        Quaternion rot = wasPlayerPile ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                        
                        bool isPile = sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.ExtraDeck || sourceLoc == CardLocation.Banished;
                        
                        if (c.unityCard != null)
                        {
                            bool animDone = false;
                            DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), !c.unityCard.isFlipped, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, false, () => animDone = true, c.unityCard.gameObject);
                            yield return new WaitUntil(() => animDone);
                        }
                        else if (excavatedGhost != null)
                        {
                            bool animDone = false;
                            DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), true, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, false, () => animDone = true, excavatedGhost);
                            yield return new WaitUntil(() => animDone);
                        }
                        else if (isPile && DuelFXManager.Instance.extractionCinematic.enableExtraction)
                        {
                            bool startFaceUp = sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished;
                            
                            var pileSettings = DuelFXManager.Instance.extractionCinematic.deck;
                            if (sourceLoc == CardLocation.Graveyard) pileSettings = DuelFXManager.Instance.extractionCinematic.graveyard;
                            else if (sourceLoc == CardLocation.ExtraDeck) pileSettings = DuelFXManager.Instance.extractionCinematic.extraDeck;
                            else if (sourceLoc == CardLocation.Banished) pileSettings = DuelFXManager.Instance.extractionCinematic.banished;
                            
                            bool currentFaceUp = pileSettings.flipDuringExtraction ? true : startFaceUp;

                            bool animDone = false;
                            DuelFXManager.Instance.PlayExtractionCinematic(c.unityData, GameManager.Instance.GetCardBackTexture(), wasPlayerPile, sourceLoc, startPos, startFaceUp, true, (ghost, pos) => {
                                DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), currentFaceUp, true, pos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, false, () => animDone = true, ghost);
                            });
                            yield return new WaitUntil(() => animDone);
                        }
                        else
                        {
                            bool startFaceUp = sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished;
                            DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), startFaceUp, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, true, null);
                        }
                    }
                    else
                    {
                        if (c.unityCard != null) GameObject.Destroy(c.unityCard.gameObject);
                    }
                }
                else
                {
                    if (c.unityCard != null) GameObject.Destroy(c.unityCard.gameObject);
                }
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public DynValue Remove(object target, object pos, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        CardEffectManager.Instance.StartCoroutine(RemoveRoutine(target, pos, reason));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Remove") });
    }

    private IEnumerator RemoveRoutine(object target, object pos, object reason)
    {
        int ocgReason = ConvertToInt(reason);
        // 1. ESPERA ESTRATÉGICA: Garante que cartas recém-destruídas (ex: Big Bang Shot)
        // terminem de voar e o Cemitério recalcule seu layout antes de tentarmos extrair o alvo!
        if (GameManager.Instance != null && !GameManager.Instance.isSimulating)
            yield return new WaitForSeconds(0.6f);

        // --- HIGHLIGHT DA CARTA FONTE (Seja ela qual for) ---
        yield return CardEffectManager.Instance.StartCoroutine(HighlightEffectSourceRoutine());

        int count = 0;
        List<LuaCard> cardsToProcess = new List<LuaCard>();

        if (target is LuaGroup group)
        {
            cardsToProcess.AddRange(group.cards);
        }
        else if (target is LuaCard card)
        {
            cardsToProcess.Add(card);
        }

        foreach (var c in cardsToProcess)
        {
            RestorePhysicalBodyIfMissing(c);
            c.currentReason = ocgReason;
            if (c.unityCard != null && !c.unityCard.isInPile)
            {
                GameManager.Instance.BanishCard(c.unityCard);
                if (CardEffectManager.Instance != null) 
                    CardEffectManager.Instance.OnCardBanished(c.unityData, c.unityCard.ownerPlayer, c.unityCard.CurrentLocation, ocgReason);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.4f);
            }
            else if (c.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sourceLoc = GetPileLocation(c, out wasPlayerPile);
                
                if (CardEffectManager.Instance != null) 
                    CardEffectManager.Instance.OnCardBanished(c.unityData, wasPlayerPile, sourceLoc, ocgReason);

                if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                {
                    Vector3 startPos = c.unityCard != null ? c.unityCard.transform.position : GetPilePosition(sourceLoc, wasPlayerPile);

                    CardFlightSettings flightSettings = null;
                    if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToBanished;
                    else if (sourceLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToBanished;
                    else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToBanished;

                    bool animDone = false;
                    
                    if (c.unityCard != null)
                    {
                        CardEffectManager.Instance.StartCoroutine(ProcessBanishVFXAndFlight(c, wasPlayerPile, sourceLoc, flightSettings, c.unityCard.gameObject, startPos, () => animDone = true));
                    }
                    else
                    {
                        DuelFXManager.Instance.PlayExtractionCinematic(c.unityData, GameManager.Instance.GetCardBackTexture(), wasPlayerPile, sourceLoc, startPos, sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished, true, (ghost, pos) => {
                            CardEffectManager.Instance.StartCoroutine(ProcessBanishVFXAndFlight(c, wasPlayerPile, sourceLoc, flightSettings, ghost, pos, () => animDone = true));
                        });
                    }
                    
                    yield return new WaitUntil(() => animDone);
                }
                else
                {
                    RemoveDataFromAllPiles(c, out wasPlayerPile);
                    GameManager.Instance.RemoveFromPlay(c.unityData, wasPlayerPile);
                    if (c.unityCard != null) GameObject.Destroy(c.unityCard.gameObject);
                }
                
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.2f);
            }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public IEnumerator HighlightEffectSourceRoutine(LuaEffect specificEffect = null)
    {
        if (GameManager.Instance == null || GameManager.Instance.isSimulating || DuelFXManager.Instance == null || !DuelFXManager.Instance.enableAnimations)
            yield break;

        LuaEffect sourceEff = specificEffect;
        if (sourceEff == null)
        {
            sourceEff = CardEffectManager.Instance.luaDuel.currentContinuousEffect;
            if (sourceEff == null && CardEffectManager.Instance.chainManager.resolvingLink != null)
                sourceEff = CardEffectManager.Instance.chainManager.resolvingLink.effect;
        }

        if (sourceEff != null && sourceEff.owner != null && sourceEff.owner.unityData != null)
        {
            CardDisplay sourceDisplay = null;
            CardData sData = sourceEff.owner.unityData;
            
            sourceDisplay = CardEffectManager.Instance.luaDuel.FindCardDisplayInPiles(sData);
            if (sourceDisplay == null && sourceEff.owner.unityCard != null && sourceEff.owner.unityCard.isOnField) 
                sourceDisplay = sourceEff.owner.unityCard;

            if (sourceDisplay != null && sourceDisplay.isInPile)
            {
                yield return CardEffectManager.Instance.StartCoroutine(AnimateCardActivationInPileRoutine(sourceDisplay));
            }
        }
    }

    public IEnumerator AnimateCardActivationInPileRoutine(CardDisplay cd)
    {
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);
        
        bool isTopCard = true;
        if (cd.transform.parent != null)
        {
            isTopCard = (cd.transform.GetSiblingIndex() == cd.transform.parent.childCount - 1);
        }

        if (cd.isInPile)
        {
            // Se ela estiver no fundo do GY, apenas brilha nas sombras em vez de varar a carta do topo!
            if (isTopCard) cd.transform.SetAsLastSibling(); 
            if (cd.isFlipped) cd.ShowFront(false);
        }

        Vector3 origScale = cd.transform.localScale;
        Vector3 peakScale = origScale * 1.4f;
        float dur = 0.2f;
        float t = 0;
        
        UnityEngine.UI.Outline outline = cd.GetComponent<UnityEngine.UI.Outline>();
        bool hadOutline = outline != null;
        if (!hadOutline) outline = cd.gameObject.AddComponent<UnityEngine.UI.Outline>();
        Color origColor = outline.effectColor;
        outline.effectColor = Color.cyan;
        outline.effectDistance = new Vector2(8, -8);
        outline.enabled = true;

        while(t < 1f) { 
            t += Time.deltaTime / dur; 
            if (isTopCard) cd.transform.localScale = Vector3.Lerp(origScale, peakScale, Mathf.SmoothStep(0, 1, t)); 
            yield return null; 
        }
        t = 0;
        while(t < 1f) { 
            t += Time.deltaTime / dur; 
            if (isTopCard) cd.transform.localScale = Vector3.Lerp(peakScale, origScale, Mathf.SmoothStep(0, 1, t)); 
            yield return null; 
        }
        if (isTopCard) cd.transform.localScale = origScale;
        
        if (!hadOutline) GameObject.Destroy(outline);
        else { outline.effectColor = origColor; outline.enabled = false; }
        
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator ProcessBanishVFXAndFlight(LuaCard lc, bool wasPlayerPile, CardLocation sourceLoc, CardFlightSettings flightSettings, GameObject ghost, Vector3 pos, System.Action onComplete)
    {
        bool useVortex = DuelFXManager.Instance.useBanishPrefab && DuelFXManager.Instance.banishVFX != null;
        CardData cData = lc.unityData;

        if (useVortex)
        {
            DuelFXManager.Instance.SpawnVFXPublic(DuelFXManager.Instance.banishVFX, pos);
            
            if (ghost != null)
            {
                float suckDuration = 0.5f;
                float t = 0;
                Vector3 startScale = ghost.transform.localScale;
                while(t < 1f) {
                    if (ghost == null) break;
                    t += Time.deltaTime / suckDuration;
                    ghost.transform.Rotate(0, 0, 720f * Time.deltaTime);
                    ghost.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                    yield return null;
                }
                GameObject.Destroy(ghost);
                ghost = null; // Apaga o fantasma da memória para que ele não voe!
            }
            else { yield return new WaitForSeconds(0.5f); }
        }

        RemoveDataFromAllPiles(lc, out wasPlayerPile);
        GameManager.Instance.RemoveFromPlay(cData, wasPlayerPile);

        if (flightSettings != null && flightSettings.enableFlight && !useVortex)
        {
            Vector3 endPos = wasPlayerPile ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
            Quaternion rot = wasPlayerPile ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
            
            bool startFaceUpFlight = true;
            if (sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.ExtraDeck) 
            {
                if (DuelFXManager.Instance != null && DuelFXManager.Instance.extractionCinematic.enableExtraction)
                {
                    var pileSettings = sourceLoc == CardLocation.Deck ? DuelFXManager.Instance.extractionCinematic.deck : DuelFXManager.Instance.extractionCinematic.extraDeck;
                    if (!pileSettings.flipDuringExtraction) startFaceUpFlight = false;
                }
                else startFaceUpFlight = false;
            }

            bool flightDone = false;
            
            DuelFXManager.Instance.PlayCardFlight(cData, GameManager.Instance.GetCardBackTexture(), startFaceUpFlight, true, pos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, false, () => {
                flightDone = true;
            }, ghost);
            
            yield return new WaitUntil(() => flightDone);
        }
        else
        {
            if (ghost != null) GameObject.Destroy(ghost);
        }

        onComplete?.Invoke();
    }

    private CardLocation GetPileLocation(LuaCard lc, out bool wasPlayerPile)
    {
        wasPlayerPile = true;
        if (GameManager.Instance == null || lc == null || lc.unityData == null) return CardLocation.Unknown;
        
        CardData data = lc.unityData;
        int ownerP = lc.ownerPlayerIndex;
        if (ownerP == -1) ownerP = lc.GetControler();
        wasPlayerPile = (ownerP == 0);

        int loc = lc.previousLocation != CardLocation.Unknown ? (int)lc.previousLocation : lc.GetLocation();
        
        if ((loc & 0x01) != 0 || loc == (int)CardLocation.Deck) return CardLocation.Deck;
        if ((loc & 0x40) != 0 || loc == (int)CardLocation.ExtraDeck) return CardLocation.ExtraDeck;
        if ((loc & 0x10) != 0 || loc == (int)CardLocation.Graveyard) return CardLocation.Graveyard;
        if ((loc & 0x20) != 0 || loc == (int)CardLocation.Banished) return CardLocation.Banished;

        if (wasPlayerPile) {
            if (GameManager.Instance.GetPlayerGraveyard().Contains(data)) return CardLocation.Graveyard;
            if (DeckManager.Instance.GetPlayerDeck().Contains(data)) { wasPlayerPile = true; return CardLocation.Deck; }
            if (GameManager.Instance.GetPlayerMainDeck().Contains(data)) return CardLocation.Deck;
            if (GameManager.Instance.GetPlayerExtraDeck().Contains(data)) return CardLocation.ExtraDeck;
            if (GameManager.Instance.GetPlayerRemoved().Contains(data)) return CardLocation.Banished;
        } else {
            if (GameManager.Instance.GetOpponentGraveyard().Contains(data)) return CardLocation.Graveyard;
            if (DeckManager.Instance.GetOpponentDeck().Contains(data)) return CardLocation.Deck;
            if (GameManager.Instance.GetOpponentMainDeck().Contains(data)) return CardLocation.Deck;
            if (GameManager.Instance.GetOpponentExtraDeck().Contains(data)) return CardLocation.ExtraDeck;
            if (GameManager.Instance.GetOpponentRemoved().Contains(data)) return CardLocation.Banished;
        }
        return CardLocation.Unknown;
    }

    public DynValue SendtoDeck(object target, object player, object seq, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        CardEffectManager.Instance.StartCoroutine(SendtoDeckRoutine(target, player, seq, reason));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SendtoDeck") });
    }

    private IEnumerator SendtoDeckRoutine(object target, object player, object seq, object reason)
    {
        int count = 0;
        int seqInt = ConvertToInt(seq);
        int ocgReason = ConvertToInt(reason);
        bool toTop = (seqInt == 0); // SEQ_DECKTOP
        bool toBottom = (seqInt == 1); // SEQ_DECKBOTTOM
        bool shuffle = (seqInt == 2) || (!toTop && !toBottom); // SEQ_DECKSHUFFLE
        
        List<CardData> cardsToAnimate = new List<CardData>();
        List<bool> isOwnerList = new List<bool>();
        List<bool> startFaceUpList = new List<bool>();
        List<CardLocation> sourceLocList = new List<CardLocation>();
        List<Vector3?> startPosList = new List<Vector3?>();

        // Verifica se o LUA exigiu o envio para o deck de um player específico (Raro, mas existe). Se nil, usamos o dono original.
        bool playerProvided = player != null && (!(player is MoonSharp.Interpreter.DynValue dv) || !dv.IsNil());
        int pInt = playerProvided ? ConvertToInt(player) : -1;

        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                RestorePhysicalBodyIfMissing(c);
                c.currentReason = ocgReason;
                c.isProcComplete = false; // Perde o certificado ao voltar pro deck
                c.counters.Clear(); // Perde todos os contadores
                if (c.unityData != null) cardsToAnimate.Add(c.unityData);

                bool targetDeckIsPlayer = true;
                bool sFaceUp = true;
                CardLocation sLoc = CardLocation.Unknown;
                Vector3? sPos = null;

                if (c.unityCard != null && !c.unityCard.isInPile) 
                { 
                    targetDeckIsPlayer = playerProvided ? (pInt == 0) : c.unityCard.ownerPlayer;
                    sLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand;
                    sPos = c.unityCard.transform.position;
                    sFaceUp = !c.unityCard.isFlipped;
                    
                    isOwnerList.Add(targetDeckIsPlayer);
                    startFaceUpList.Add(sFaceUp);
                    sourceLocList.Add(sLoc);
                    startPosList.Add(sPos);

                    if(targetDeckIsPlayer) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, c.unityData); 
                    else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, c.unityData); 

                    if (GameManager.Instance.playerHand.Contains(c.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(c.unityCard.gameObject);
                    else if (GameManager.Instance.opponentHand.Contains(c.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(c.unityCard.gameObject);
                    
                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardReturnedToDeck(c.unityData, targetDeckIsPlayer, sLoc, ocgReason);

                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(c.unityCard);
                    GameObject.Destroy(c.unityCard.gameObject);

                    count++; 
                }                 
                else if (c.unityData != null) { 
                    bool wasPlayerPile; 
                    sLoc = RemoveDataFromAllPiles(c, out wasPlayerPile); 
                    sPos = c.unityCard != null ? c.unityCard.transform.position : GetPilePosition(sLoc, wasPlayerPile);
                    targetDeckIsPlayer = playerProvided ? (pInt == 0) : wasPlayerPile;
                    
                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardReturnedToDeck(c.unityData, targetDeckIsPlayer, sLoc, ocgReason);

                    isOwnerList.Add(targetDeckIsPlayer);
                    startFaceUpList.Add(true);
                    sourceLocList.Add(sLoc);
                    startPosList.Add(sPos);

                    if(targetDeckIsPlayer) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, c.unityData); 
                    else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, c.unityData); 
                    
                    if (c.unityCard != null) GameObject.Destroy(c.unityCard.gameObject);
                    count++; 
                }
            }
            // Debug.Log($"[Lua] Duel.SendtoDeck(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card)
        {
            RestorePhysicalBodyIfMissing(card);
            card.currentReason = ocgReason;
            card.isProcComplete = false;
            card.counters.Clear();
            if (card.unityData != null) cardsToAnimate.Add(card.unityData);
            bool targetDeckIsPlayer = true;
            bool sFaceUp = true;
            CardLocation sLoc = CardLocation.Unknown;
            Vector3? sPos = null;

            if (card.unityCard != null && !card.unityCard.isInPile)
            {
                targetDeckIsPlayer = playerProvided ? (pInt == 0) : card.unityCard.ownerPlayer;
                sLoc = card.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand;
                sPos = card.unityCard.transform.position;
                sFaceUp = !card.unityCard.isFlipped;
                
                isOwnerList.Add(targetDeckIsPlayer);
                startFaceUpList.Add(sFaceUp);
                sourceLocList.Add(sLoc);
                startPosList.Add(sPos);

                if(targetDeckIsPlayer) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, card.unityData); 
                else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, card.unityData); 

                if (GameManager.Instance.playerHand.Contains(card.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(card.unityCard.gameObject);
                else if (GameManager.Instance.opponentHand.Contains(card.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(card.unityCard.gameObject);
                
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardReturnedToDeck(card.unityData, targetDeckIsPlayer, sLoc, ocgReason);

                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card.unityCard);
                GameObject.Destroy(card.unityCard.gameObject);

                count = 1;;
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile; 
                sLoc = RemoveDataFromAllPiles(card, out wasPlayerPile);
                sPos = card.unityCard != null ? card.unityCard.transform.position : GetPilePosition(sLoc, wasPlayerPile);
                targetDeckIsPlayer = playerProvided ? (pInt == 0) : wasPlayerPile;
                
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardReturnedToDeck(card.unityData, targetDeckIsPlayer, sLoc, ocgReason);

                isOwnerList.Add(targetDeckIsPlayer);
                startFaceUpList.Add(true);
                sourceLocList.Add(sLoc);
                startPosList.Add(sPos);

                if(targetDeckIsPlayer) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, card.unityData); 
                else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, card.unityData);
                
                if (card.unityCard != null) GameObject.Destroy(card.unityCard.gameObject);
                count = 1;
            }
        }

        // Dispara a animação visual após a lógica de dados
        if (GameManager.Instance != null && GameManager.Instance.enableReturnToDeckAnimation && cardsToAnimate.Count > 0)
        {
            for(int i = 0; i < cardsToAnimate.Count; i++)
            {
                bool animDone = false;
                if (DuelFXManager.Instance != null) {
                    DuelFXManager.Instance.PlayReturnToDeckAnimation(cardsToAnimate[i], isOwnerList[i], sourceLocList[i], startPosList[i], startFaceUpList[i], () => animDone = true);
                } else { animDone = true; }
                yield return new WaitUntil(() => animDone);
            }
        }

        if (shuffle && GameManager.Instance != null) 
        {
            if (playerProvided) GameManager.Instance.ShuffleDeck(pInt == 0);
            else 
            {
                // Se devolveu cartas de donos diferentes, embaralha ambos os decks afetados
                if (isOwnerList.Contains(true)) GameManager.Instance.ShuffleDeck(true);
                if (isOwnerList.Contains(false)) GameManager.Instance.ShuffleDeck(false);
            }
        }
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public void ShuffleDeck(object player)
    {
        int pInt = ConvertToInt(player);
        if (GameManager.Instance != null) {
            if (pInt == 0 || pInt == 3) GameManager.Instance.ShuffleDeck(true);
            if (pInt == 1 || pInt == 3) GameManager.Instance.ShuffleDeck(false);
        }
    }

    public void ShuffleHand(object player)
    {
        int pInt = ConvertToInt(player);
        if (GameManager.Instance != null) {
            if (pInt == 0 || pInt == 3) GameManager.Instance.ShuffleHand(true);
            if (pInt == 1 || pInt == 3) GameManager.Instance.ShuffleHand(false);
        }
    }

    public DynValue Release(object target, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        CardEffectManager.Instance.StartCoroutine(ReleaseRoutine(target, reason));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Release") });
    }

    private IEnumerator ReleaseRoutine(object target, object reason)
    {
        int ocgReason = ConvertToInt(reason) | 0x2; 
        int count = 0;

        if (target is LuaGroup group)
        {
            this.lastCostGroup = group; 
            foreach (var c in group.cards)
            {
                c.currentReason = ocgReason;
                if (c.unityCard != null) {
                    GameManager.Instance.TributeCard(c.unityCard, ocgReason);
                    count++;
                }
            }
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            LuaGroup tempGroup = new LuaGroup();
            tempGroup.AddCard(card);
            this.lastCostGroup = tempGroup; 
            card.currentReason = ocgReason;
            GameManager.Instance.TributeCard(card.unityCard, ocgReason);
            count++;
        }

        // A Poeira Tática: LUA agora espera as partículas de sacrifício sumirem e os monstros caírem no GY!
        if (count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
            yield return new WaitForSeconds(0.6f);

        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public void ReleaseRitualMaterial(object target) { Debug.LogWarning("[LUA STUB] ReleaseRitualMaterial chamado (O C# assume a destruição física através da UI de Ritual!)"); }

    public void RaiseEvent(object eg, object code, object re, object r, object rp, object ep, object ev)
    {
        if (CardEffectManager.Instance != null)
        {
            object actualEg = eg;
            if (eg is DynValue dv) {
                if (dv.Type == DataType.UserData) actualEg = dv.UserData.Object;
                else if (dv.IsNil()) actualEg = null;
            }

            LuaEffect actualRe = re as LuaEffect;
            if (re is DynValue dvRe && dvRe.Type == DataType.UserData) actualRe = dvRe.UserData.Object as LuaEffect;

            EventData ed = new EventData(actualEg, ConvertToInt(ep), ConvertToInt(ev), actualRe, ConvertToInt(r), ConvertToInt(rp));
            CardEffectManager.Instance.TriggerLuaEvent(ConvertToInt(code), ed);
        }
    }

    public void RaiseSingleEvent(object ec, object code, object re, object r, object rp, object ep, object ev)
    {
        object actualEc = ec;
        if (ec is DynValue dv) {
            if (dv.Type == DataType.UserData) actualEc = dv.UserData.Object;
            else if (dv.IsNil()) actualEc = null;
        }

        LuaEffect actualRe = re as LuaEffect;
        if (re is DynValue dvRe && dvRe.Type == DataType.UserData) actualRe = dvRe.UserData.Object as LuaEffect;

        if (actualEc is LuaCard lc && CardEffectManager.Instance != null)
        {
            int eventCode = ConvertToInt(code);
            var matchingEffects = lc.registeredEffects.FindAll(eff => eff.code == eventCode);
            EventData ed = new EventData(lc, ConvertToInt(ep), ConvertToInt(ev), actualRe, ConvertToInt(r), ConvertToInt(rp));
            
            foreach(var effect in matchingEffects)
            {
                if (CardEffectManager.Instance.CanActivateEffect(lc, effect, lc.GetControler(), ed))
                {
                    CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.chainManager.BuildAndResolveChainRoutine(lc, effect, ed, lc.GetControler(), null));
                }
            }
        }
    }

    public void BreakEffect()
    {
        // Ponto de quebra de resolução de corrente (usado para Miss Timing em jogos reais).
    }

    // Stubs vitais capturados pelo relatório (Counter Traps e UX)
    public bool NegateActivation(object chainc) 
    {
        int cIdx = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null)
        {
            var resolvingEff = CardEffectManager.Instance.chainManager.resolvingLink?.effect;
            int rp = CardEffectManager.Instance.chainManager.resolvingLink?.player ?? 0;
            bool success = CardEffectManager.Instance.chainManager.NegateChainLink(cIdx, true, resolvingEff, rp);
            if (success)
            {
                var targetLink = CardEffectManager.Instance.chainManager.currentChain.Find(l => l.chainIndex == cIdx);
                if (targetLink != null) {
                    EventData ed = new EventData(targetLink.card, targetLink.player, cIdx, resolvingEff, 0, rp);
                    CardEffectManager.Instance.TriggerLuaEvent(1024, ed); // EVENT_CHAIN_NEGATED
                }
            }
            return success;
        }
        return true; 
    }
    public bool NegateEffect(object chainc) 
    { 
        int cIdx = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null)
        {
            var resolvingEff = CardEffectManager.Instance.chainManager.resolvingLink?.effect;
            int rp = CardEffectManager.Instance.chainManager.resolvingLink?.player ?? 0;
            bool success = CardEffectManager.Instance.chainManager.NegateChainLink(cIdx, false, resolvingEff, rp);
            // EVENT_CHAIN_DISABLED raramente é ouvido diretamente, mas caso precise no futuro, seria 1025.
            return success;
        }
        return true; 
    }
    public void ChangeChainOperation(object chainc, object op) { Debug.LogWarning("[LUA STUB] ChangeChainOperation chamado"); }

    public void SetPossibleOperationInfo(params object[] args) { Debug.LogWarning("[LUA STUB] SetPossibleOperationInfo chamado"); }
    public void SetTargetPlayer(object p) { targetPlayer = ConvertToInt(p); }

    public void RegisterEffect(LuaEffect e, object player = null)
    {
        if (e == null) return;
        globalEffects.Add(e);
    }

    public void SetTargetParam(object p) { targetParam = ConvertToInt(p); /* Debug.Log($"<color=magenta>[LuaDuel LOG]</color> SetTargetParam: Guardando valor '{targetParam}' na memória do LUA!"); */ }

    public DynValue SendtoHand(object target, object player, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        CardEffectManager.Instance.StartCoroutine(SendtoHandRoutine(target, player, reason));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SendtoHand") });
    }

    private IEnumerator SendtoHandRoutine(object target, object player, object reason)
    {
        int ocgReason = ConvertToInt(reason);
        int count = 0;
        List<LuaCard> cardsToProcess = new List<LuaCard>();

        if (target is LuaGroup group)
        {
            cardsToProcess.AddRange(group.cards);
        }
        else if (target is LuaCard card)
        {
            cardsToProcess.Add(card);
        }

        foreach (var c in cardsToProcess)
        {
            RestorePhysicalBodyIfMissing(c);
            c.currentReason = ocgReason;
            c.isProcComplete = false; // Perde o certificado ao voltar pra mão
            c.counters.Clear(); // Perde todos os contadores
            if (c.unityCard != null && !c.unityCard.isInPile)
            {
                GameManager.Instance.ReturnToHand(c.unityCard);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
            else if (c.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sLoc = RemoveDataFromAllPiles(c, out wasPlayerPile);

                GameObject excavatedGhost = CardExcavatedUI.Instance != null ? CardExcavatedUI.Instance.GetAndConsumeGhost(c.unityData) : null;
                Vector3? customStartPos = excavatedGhost != null ? excavatedGhost.transform.position : null;

                GameManager.Instance.AddCardToHand(c.unityData, wasPlayerPile, customStartPos, sLoc, false, wasPlayerPile);
                
                if (excavatedGhost != null) GameObject.Destroy(excavatedGhost);
                if (c.unityCard != null) GameObject.Destroy(c.unityCard.gameObject);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
        }

        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    private CardLocation RemoveDataFromAllPiles(LuaCard lc, out bool wasPlayerPile)
    {
        wasPlayerPile = true;
        if (GameManager.Instance == null || lc == null || lc.unityData == null) return CardLocation.Unknown;
        
        CardData data = lc.unityData;
        int ownerP = lc.ownerPlayerIndex;
        if (ownerP == -1) ownerP = lc.GetControler();
        wasPlayerPile = (ownerP == 0);

        int loc = lc.previousLocation != CardLocation.Unknown ? (int)lc.previousLocation : lc.GetLocation();
        
        if ((loc & 0x01) != 0 || loc == (int)CardLocation.Deck) {
            if (wasPlayerPile && DeckManager.Instance.GetPlayerDeck().Contains(data)) { DeckManager.Instance.GetPlayerDeck().Remove(data); DeckManager.Instance.UpdateDeckVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (!wasPlayerPile && DeckManager.Instance.GetOpponentDeck().Contains(data)) { DeckManager.Instance.GetOpponentDeck().Remove(data); DeckManager.Instance.UpdateDeckVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (wasPlayerPile && GameManager.Instance.GetPlayerMainDeck().Contains(data)) { GameManager.Instance.GetPlayerMainDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (!wasPlayerPile && GameManager.Instance.GetOpponentMainDeck().Contains(data)) { GameManager.Instance.GetOpponentMainDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false; return CardLocation.Deck; }
        }
        else if ((loc & 0x40) != 0 || loc == (int)CardLocation.ExtraDeck) {
            if (wasPlayerPile && GameManager.Instance.GetPlayerExtraDeck().Contains(data)) { GameManager.Instance.GetPlayerExtraDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.ExtraDeck; }
            if (!wasPlayerPile && GameManager.Instance.GetOpponentExtraDeck().Contains(data)) { GameManager.Instance.GetOpponentExtraDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.ExtraDeck; }
        }
        else if ((loc & 0x10) != 0 || loc == (int)CardLocation.Graveyard) {
            if (wasPlayerPile && GameManager.Instance.GetPlayerGraveyard().Contains(data)) { GameManager.Instance.GetPlayerGraveyard().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Graveyard; }
            if (!wasPlayerPile && GameManager.Instance.GetOpponentGraveyard().Contains(data)) { GameManager.Instance.GetOpponentGraveyard().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Graveyard; }
        }
        else if ((loc & 0x20) != 0 || loc == (int)CardLocation.Banished) {
            if (wasPlayerPile && GameManager.Instance.GetPlayerRemoved().Contains(data)) { GameManager.Instance.GetPlayerRemoved().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Banished; }
            if (!wasPlayerPile && GameManager.Instance.GetOpponentRemoved().Contains(data)) { GameManager.Instance.GetOpponentRemoved().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Banished; }
        }

        if (wasPlayerPile) {
            if (GameManager.Instance.GetPlayerGraveyard().Contains(data)) { GameManager.Instance.GetPlayerGraveyard().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Graveyard; }
            if (DeckManager.Instance.GetPlayerDeck().Contains(data)) { DeckManager.Instance.GetPlayerDeck().Remove(data); DeckManager.Instance.UpdateDeckVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (GameManager.Instance.GetPlayerMainDeck().Contains(data)) { GameManager.Instance.GetPlayerMainDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (GameManager.Instance.GetPlayerExtraDeck().Contains(data)) { GameManager.Instance.GetPlayerExtraDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.ExtraDeck; }
            if (GameManager.Instance.GetPlayerRemoved().Contains(data)) { GameManager.Instance.GetPlayerRemoved().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Banished; }
        } else {
            if (GameManager.Instance.GetOpponentGraveyard().Contains(data)) { GameManager.Instance.GetOpponentGraveyard().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Graveyard; }
            if (DeckManager.Instance.GetOpponentDeck().Contains(data)) { DeckManager.Instance.GetOpponentDeck().Remove(data); DeckManager.Instance.UpdateDeckVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (GameManager.Instance.GetOpponentMainDeck().Contains(data)) { GameManager.Instance.GetOpponentMainDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false; return CardLocation.Deck; }
            if (GameManager.Instance.GetOpponentExtraDeck().Contains(data)) { GameManager.Instance.GetOpponentExtraDeck().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.ExtraDeck; }
            if (GameManager.Instance.GetOpponentRemoved().Contains(data)) { GameManager.Instance.GetOpponentRemoved().Remove(data); GameManager.Instance.UpdatePileVisuals(); return CardLocation.Banished; }
        }
        return CardLocation.Unknown;
    }

    public bool SpecialSummon(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        bool isPlayerSummoning = IsPlayer(player);
        int posInt = ConvertToInt(pos);
        bool inDefense = (posInt & 0x8) != 0 || (posInt & 0xA) != 0; // Verifica se tem flag de defesa

        int sumTypeInt = ConvertToInt(sumtype);
        if (sumTypeInt == 0) sumTypeInt = 0x40000000; // Fallback garantido para SUMMON_TYPE_SPECIAL

        if (target is LuaGroup group && group.cards.Count > 0)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null && !c.unityCard.isInPile)
                {
                    bool originalOwner = c.unityCard.ownerPlayer;
                    if (GameManager.Instance.playerHand.Contains(c.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(c.unityCard.gameObject);
                    else if (GameManager.Instance.opponentHand.Contains(c.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(c.unityCard.gameObject);
                    
                    Vector3? sPos = null; CardLocation sLoc = CardLocation.Unknown;
                    if (c.unityCard != null) { sPos = c.unityCard.transform.position; sLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand; }
                    else { sLoc = CardLocation.Graveyard; sPos = isPlayerSummoning ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; }
                    
                    CardDisplay newDisplay = GameManager.Instance.SpecialSummonFromData(c.unityCard.CurrentCardData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc, originalOwner, sumTypeInt);
                    
                    if (CardEffectManager.Instance.activeLuaCards.ContainsKey(c.unityCard))
                        CardEffectManager.Instance.activeLuaCards.Remove(c.unityCard);
                    GameObject.Destroy(c.unityCard.gameObject);
                    
                    c.unityCard = newDisplay;
                    CardEffectManager.Instance.activeLuaCards[newDisplay] = c;
                }
                else if (c.unityData != null)
                {
                    bool wasPlayerPile; 
                    CardLocation sLoc = RemoveDataFromAllPiles(c, out wasPlayerPile);
                    Vector3 sPos = c.unityCard != null ? c.unityCard.transform.position : GetPilePosition(sLoc, wasPlayerPile);
                    CardDisplay newDisplay = GameManager.Instance.SpecialSummonFromData(c.unityData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc, wasPlayerPile, sumTypeInt);
                    
                    if (c.unityCard != null && c.unityCard != newDisplay) {
                        if (CardEffectManager.Instance.activeLuaCards.ContainsKey(c.unityCard))
                            CardEffectManager.Instance.activeLuaCards.Remove(c.unityCard);
                        GameObject.Destroy(c.unityCard.gameObject);
                    }
                    c.unityCard = newDisplay;
                    CardEffectManager.Instance.activeLuaCards[newDisplay] = c;
                }
            }
            // Debug.Log($"[Lua] Duel.SpecialSummon(Grupo)");
            return true;
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null && !card.unityCard.isInPile)
            {
                bool originalOwner = card.unityCard.ownerPlayer;
                if (GameManager.Instance.playerHand.Contains(card.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(card.unityCard.gameObject);
                else if (GameManager.Instance.opponentHand.Contains(card.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(card.unityCard.gameObject);
                
                Vector3? sPos = null; CardLocation sLoc = CardLocation.Unknown;
                if (card.unityCard != null) { sPos = card.unityCard.transform.position; sLoc = card.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand; }
                else { sLoc = CardLocation.Graveyard; sPos = isPlayerSummoning ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; }
                
                CardDisplay newDisplay = GameManager.Instance.SpecialSummonFromData(card.unityCard.CurrentCardData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc, originalOwner, sumTypeInt);
                
                if (CardEffectManager.Instance.activeLuaCards.ContainsKey(card.unityCard))
                    CardEffectManager.Instance.activeLuaCards.Remove(card.unityCard);
                GameObject.Destroy(card.unityCard.gameObject);
                
                card.unityCard = newDisplay;
                CardEffectManager.Instance.activeLuaCards[newDisplay] = card;
                return true;
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile; 
                CardLocation sLoc = RemoveDataFromAllPiles(card, out wasPlayerPile);
                Vector3 sPos = card.unityCard != null ? card.unityCard.transform.position : GetPilePosition(sLoc, wasPlayerPile);
                CardDisplay newDisplay = GameManager.Instance.SpecialSummonFromData(card.unityData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc, wasPlayerPile, sumTypeInt);
                
                if (card.unityCard != null && card.unityCard != newDisplay) {
                    if (CardEffectManager.Instance.activeLuaCards.ContainsKey(card.unityCard))
                        CardEffectManager.Instance.activeLuaCards.Remove(card.unityCard);
                    GameObject.Destroy(card.unityCard.gameObject);
                }
                card.unityCard = newDisplay;
                CardEffectManager.Instance.activeLuaCards[newDisplay] = card;
                return true;
            }
        }
        
        return false;
    }
    
    public bool SpecialSummonStep(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        return SpecialSummon(target, sumtype, sumplayer, player, nocheck, nolimit, pos);
    }

    public void SpecialSummonComplete()
    {
        // Finaliza a invocação iniciada pelo SpecialSummonStep (Já resolvemos instantaneamente no passo)
    }

    public LuaCard CreateToken(object player, object code)
    {
        int cardCode = ConvertToInt(code);
        CardData tokenData = null;

        // Tenta achar os dados do token (status, imagem) no banco usando o ID (Password)
        if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
        {
            string strCode = cardCode.ToString();
            CardData dbData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == strCode);
            if (dbData != null) tokenData = JsonUtility.FromJson<CardData>(JsonUtility.ToJson(dbData));
        }

        // Fallback robusto se a API do site não nos enviou o token
        if (tokenData == null)
        {
            tokenData = new CardData 
            { 
                id = "TOKEN", 
                password = cardCode.ToString(),
                name = "Token", 
                type = "Monster (Token)", 
                atk = 0, def = 0, level = 1, 
                race = "Beast", attribute = "Earth" 
            };
        }

        return new LuaCard(tokenData);
    }

    public bool Equip(object player, object equip_card, object target)
    {
        LuaCard ec = equip_card as LuaCard;
        LuaCard t = target as LuaCard;
        if (ec != null && t != null && ec.unityCard != null && t.unityCard != null)
        {
            if (ec.unityData.type.Contains("Monster"))
                GameManager.Instance.EquipMonsterToMonster(ec.unityCard, t.unityCard);
            else
                GameManager.Instance.CreateCardLink(ec.unityCard, t.unityCard, CardLink.LinkType.Equipment);
            
            // Debug.Log($"[Lua] Duel.Equip({ec.unityData.name} em {t.unityData.name})");
            return true;
        }
        return false;
    }

    public void DisableShuffleCheck(params object[] args)
    {
        // Debug.Log($"<color=magenta>[LuaDuel LOG]</color> LUA pediu DisableShuffleCheck. O auto-shuffle do C# será bloqueado temporariamente.");
        nextShuffleDisabled = true;
    }

    public DynValue ChangePosition(object target, object au, object ad = null, object du = null, object dd = null, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        int posAU = ConvertToInt(au); // POS_FACEUP_ATTACK (1)
        int posAD = ad != null && (!(ad is MoonSharp.Interpreter.DynValue d1) || !d1.IsNil()) ? ConvertToInt(ad) : posAU;
        int posDU = du != null && (!(du is MoonSharp.Interpreter.DynValue d2) || !d2.IsNil()) ? ConvertToInt(du) : posAU;
        int posDD = dd != null && (!(dd is MoonSharp.Interpreter.DynValue d3) || !d3.IsNil()) ? ConvertToInt(dd) : posAU;

        bool noFlip = false;
        if (extraArgs != null && extraArgs.Length > 0 && ConvertToInt(extraArgs[0]) != 0) noFlip = true;

        List<CardDisplay> cardsToChange = new List<CardDisplay>();
        if (target is LuaGroup group) { foreach (var c in group.cards) if (c.unityCard != null) cardsToChange.Add(c.unityCard); }
        else if (target is LuaCard card && card.unityCard != null) { cardsToChange.Add(card.unityCard); }

        CardEffectManager.Instance.StartCoroutine(ChangePositionRoutine(cardsToChange, posAU, posAD, posDU, posDD, noFlip));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ChangePosition") });
    }

    private IEnumerator ChangePositionRoutine(List<CardDisplay> cards, int posAU, int posAD, int posDU, int posDD, bool noFlip)
    {
        int count = 0;
        int pendingAnimations = 0;

        foreach (CardDisplay c in cards)
        {
            if (c == null || !c.isOnField) continue;
            
            int currentPos = 0;
            if (c.position == CardDisplay.BattlePosition.Attack && !c.isFlipped) currentPos = 1;
            else if (c.position == CardDisplay.BattlePosition.Attack && c.isFlipped) currentPos = 2;
            else if (c.position == CardDisplay.BattlePosition.Defense && !c.isFlipped) currentPos = 4;
            else if (c.position == CardDisplay.BattlePosition.Defense && c.isFlipped) currentPos = 8;

            int targetPos = 0;
            if (currentPos == 1) targetPos = posAU;
            else if (currentPos == 2) targetPos = posAD;
            else if (currentPos == 4) targetPos = posDU;
            else if (currentPos == 8) targetPos = posDD;

            if (targetPos == 0 || targetPos == currentPos) continue;

            bool isAttackTarget = c.position == CardDisplay.BattlePosition.Attack;
            if ((targetPos & 3) != 0 && (targetPos & 12) == 0) isAttackTarget = true;
            else if ((targetPos & 12) != 0 && (targetPos & 3) == 0) isAttackTarget = false;

            bool isFaceUpTarget = !c.isFlipped;
            if ((targetPos & 5) != 0 && (targetPos & 10) == 0) isFaceUpTarget = true;
            else if ((targetPos & 10) != 0 && (targetPos & 5) == 0) isFaceUpTarget = false;

            if ((isAttackTarget && c.position == CardDisplay.BattlePosition.Defense) || (!isAttackTarget && c.position == CardDisplay.BattlePosition.Attack))
            {
                c.position = isAttackTarget ? CardDisplay.BattlePosition.Attack : CardDisplay.BattlePosition.Defense;
                
                if (c.CurrentCardData != null && (c.CurrentCardData.type.Contains("Spell") || c.CurrentCardData.type.Contains("Trap")))
                {
                    c.transform.localRotation = Quaternion.Euler(0, 0, c.isPlayerCard ? 0f : 180f);
                }
                else
                {
                    c.transform.localRotation = Quaternion.Euler(0, 0, isAttackTarget ? (c.isPlayerCard ? 0f : 180f) : (c.isPlayerCard ? 90f : -90f));
                }
                if (GameManager.Instance != null) GameManager.Instance.RefreshAllCardsVisuals();
            }

            if (isFaceUpTarget && c.isFlipped)
            {
                pendingAnimations++;
                c.RevealCard(false, true, () => { 
                    if (GameManager.Instance != null) { 
                        GameManager.Instance.OnBattlePositionChanged(c); 
                        if (isAttackTarget && !noFlip) {
                            c.summonType = 0x20000000; // SUMMON_TYPE_FLIP
                            GameManager.Instance.OnFlipSummon(c); 
                        }
                        else if (!isAttackTarget && !noFlip) CardEffectManager.Instance.eventManager.OnFlip(c);
                    } 
                    pendingAnimations--;            
                });
            }
            else if (!isFaceUpTarget && !c.isFlipped)
            {
                pendingAnimations++;
                c.ShowBack(true, () => {
                    if (GameManager.Instance != null) GameManager.Instance.OnBattlePositionChanged(c);
                    pendingAnimations--;
                });            
            }
            else { if (GameManager.Instance != null) GameManager.Instance.OnBattlePositionChanged(c); }
            count++;
        }

        // Aguarda todas as animações visuais terminarem
        while (pendingAnimations > 0) yield return null;
        
        // Pausa extra de 0.6s para o jogador absorver a informação (ex: Acid Trap Hole)
        if (count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
            yield return new WaitForSeconds(0.6f);

        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public void Summon(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) 
        {
            if (IsPlayer(player)) GameManager.Instance.normalSummonsThisTurnPlayer++;
            else GameManager.Instance.normalSummonsThisTurnOpponent++;
            
            Vector3 sourcePos = c.unityCard.transform.position;
            CardLocation sourceLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand;
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, false, IsPlayer(player), false, c.unityData.level >= 5, null, null, sourcePos, sourceLoc, null, 0x10000000); // SUMMON_TYPE_NORMAL
        }
    }

    public void MSet(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) 
        {
            if (IsPlayer(player)) GameManager.Instance.normalSummonsThisTurnPlayer++;
            else GameManager.Instance.normalSummonsThisTurnOpponent++;
            
            Vector3 sourcePos = c.unityCard.transform.position;
            CardLocation sourceLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand;
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, true, IsPlayer(player), true, c.unityData.level >= 5, null, null, sourcePos, sourceLoc, null, 0x10000000); // SUMMON_TYPE_NORMAL
        }
    }
    public void SSet(object player, object target, params object[] extraArgs)
    {
        if (target is LuaCard card && card.unityCard != null)
            GameManager.Instance.PlaySpellTrap(card.unityCard.gameObject, card.unityData, true);
        else if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null) GameManager.Instance.PlaySpellTrap(c.unityCard.gameObject, c.unityData, true);
        }
    }

    public bool GetControl(object target, object player, object reset_phase = null, object reset_count = null)
    {
        bool targetPlayerIsHuman = IsPlayer(player);
        
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null && c.unityCard.isPlayerCard != targetPlayerIsHuman)
                    GameManager.Instance.SwitchControl(c.unityCard);
            }
            return true;
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            if (card.unityCard.isPlayerCard != targetPlayerIsHuman)
                GameManager.Instance.SwitchControl(card.unityCard);
            return true;
        }
        
        return false;
    }

    // --- OPERAÇÕES ASSÍNCRONAS DE LUA (YIELD REQ) ---

    // Helper em Corrotina para a Destruição Simultânea com VFX
    private IEnumerator DestroyCardsRoutine(List<CardDisplay> cards, int reason)
    {
        int count = 0;
        foreach (var c in cards)
        {
            if (c != null && c.isOnField)
            {
                LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(c);
                if (lc != null) 
                {
                    LuaEffect reasonEffect = null;
                    if (CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
                        reasonEffect = CardEffectManager.Instance.chainManager.resolvingLink.effect;
                        
                    if (!lc.IsDestructable(reasonEffect)) 
                    {
                        Debug.Log($"<color=cyan>[Destroy]</color> {c.CurrentCardData.name} foi protegido contra destruição!");
                        continue; 
                    }
                    lc.currentReason = reason;
                }

                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(c);
                GameManager.Instance.MoveCard(c, CardLocation.Graveyard, reason);
                count++;
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.25f);
            }
        }
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    // --- REGRAS DE VITÓRIA E BATALHA FORÇADA (Para cartas como Last Turn) ---
    public void Win(object player, object reason)
    {
        int p = ConvertToInt(player);
        int r = ConvertToInt(reason);
        
        string reasonStr = "Desconhecido";
        switch (r)
        {
            case 0x10: reasonStr = "Exodia the Forbidden One"; break;
            case 0x11: reasonStr = "Final Countdown"; break;
            case 0x12: reasonStr = "Vennominaga, the Deity of Poisonous Snakes"; break;
            case 0x13: reasonStr = "Holactie the Creator of Light"; break;
            case 0x14: reasonStr = "Exodius the Ultimate Forbidden Lord"; break;
            case 0x15: reasonStr = "Destiny Board"; break;
            case 0x16: reasonStr = "Last Turn"; break;
            case 0x17: reasonStr = "Number 88: Gimmick Puppet of Leo"; break;
            case 0x18: reasonStr = "Number C88: Gimmick Puppet Disaster Leo"; break;
            case 0x19: reasonStr = "Jackpot 7"; break;
            case 0x1a: reasonStr = "Relay Soul"; break;
            case 0x1b: reasonStr = "Ghostrick Angel of Mischief"; break;
            case 0x1c: reasonStr = "Phantasm Spiral Assault"; break;
            case 0x1d: reasonStr = "F.A. Winners"; break;
            case 0x1e: reasonStr = "Flying Elephant"; break;
            case 0x1f: reasonStr = "Exodia, the Legendary Defender"; break;
            case 0x21: reasonStr = "True Exodia"; break;
            case 0x22: reasonStr = "Final Draw"; break;
            case 0x30: reasonStr = "Creator Miracle"; break;
            case 0x52: reasonStr = "Number iC1000: Numerounius Numerounia"; break;
            case 0x53: reasonStr = "Zero Gate of the Void"; break;
            case 0x54: reasonStr = "Deuce"; break;
            case 0x56: reasonStr = "Deck Master (Perda do Deck Master)"; break;
            case 0x57: reasonStr = "Draw of Fate"; break;
            case 0x58: reasonStr = "Musical Sumo Dice Games"; break;
            case 0x59: reasonStr = "Summer Schoolwork"; break;
            default: reasonStr = $"Vitória Customizada LUA ({r})"; break;
        }

        string playerStr = p == 0 ? "Jogador (Você)" : (p == 1 ? "Oponente (IA)" : "Empate (Draw)");
        Debug.Log($"<color=yellow>[Vitória Especial OCGCore]</color> Condição Atingida! Vencedor: {playerStr} | Motivo: {reasonStr}");

        // [FUTURO] O GameManager.EndDuel hoje aceita apenas (bool playerWon). Futuramente, poderemos passar o "reasonStr" para acionar telas UI específicas de Vitória (Ex: Tela do Destiny Board).
        if (p == 0) GameManager.Instance.EndDuel(true);
        else if (p == 1) GameManager.Instance.EndDuel(false);
        else GameManager.Instance.EndDuel(false); // OCGCore manda p=2 para Draw (Empate). O GameManager hoje assume False como derrota. No futuro, crie lógica de Empate!
    }

    public DynValue ForceAttack(object attacker, object target)
    {
        LuaCard atk = attacker as LuaCard;
        LuaCard def = target as LuaCard;
        if (atk != null && def != null && CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = true;
            CardEffectManager.Instance.yieldReturnValue = null;
            CardEffectManager.Instance.StartCoroutine(ForceAttackRoutine(atk, def));
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ForceAttack") });
        }
        return DynValue.Nil;
    }

    private IEnumerator ForceAttackRoutine(LuaCard atk, LuaCard def)
    {
        CardEffectManager.Instance.luaDuel.currentAttacker = atk;
        CardEffectManager.Instance.luaDuel.currentAttackTarget = def;
        var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
        yield return CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, atk, def));
        CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    // --- MINIGAMES DE TABULEIRO (Magical Hats) ---
    public DynValue ShuffleSetCard(object target)
    {
        if (target is LuaGroup group && group.cards.Count > 1)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = true;
            CardEffectManager.Instance.yieldReturnValue = null;
            CardEffectManager.Instance.StartCoroutine(ShuffleSetCardRoutine(group));
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ShuffleSetCard") });
        }
        return DynValue.Nil;
    }

    private IEnumerator ShuffleSetCardRoutine(LuaGroup group)
    {
        List<CardDisplay> cardsToShuffle = new List<CardDisplay>();
        List<Transform> currentZones = new List<Transform>();

        foreach (var lc in group.cards)
        {
            // Garante que só embaralha cartas que estão fisicamente Face-Down no tabuleiro
            if (lc.unityCard != null && lc.unityCard.isOnField && lc.unityCard.isFlipped)
            {
                cardsToShuffle.Add(lc.unityCard);
                currentZones.Add(lc.unityCard.transform.parent);
            }
        }

        if (cardsToShuffle.Count > 1)
        {
            // Algoritmo de Embaralhamento de Zonas (Fisher-Yates)
            for (int i = 0; i < currentZones.Count; i++) {
                Transform temp = currentZones[i]; int randomIndex = UnityEngine.Random.Range(i, currentZones.Count);
                currentZones[i] = currentZones[randomIndex]; currentZones[randomIndex] = temp;
            }
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.shuffleSound);
            // Realiza a troca física de Pais (Zonas)
            for (int i = 0; i < cardsToShuffle.Count; i++) { cardsToShuffle[i].transform.SetParent(currentZones[i], false); cardsToShuffle[i].transform.localPosition = Vector3.zero; }
            if (GameManager.Instance != null && !GameManager.Instance.isSimulating) yield return new WaitForSeconds(0.8f); // Pausa para o suspense
        }

        CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public bool MoveToField(object card, object move_player, object target_player, object dest, object pos, object enable)
    {
        LuaCard lc = card as LuaCard;
        if (lc == null) return false;

        int tPlayer = ConvertToInt(target_player);
        int destLoc = ConvertToInt(dest);
        int posInt = ConvertToInt(pos);
        bool faceUp = (posInt & 0x1) != 0 || (posInt & 0x4) != 0 || (posInt & 0x5) != 0;

        if (lc.unityCard != null)
        {
            if (destLoc == 0x08 || destLoc == 0x400) // LOCATION_SZONE
                GameManager.Instance.PlaySpellTrap(lc.unityCard.gameObject, lc.unityData, !faceUp);
            else if (destLoc == 0x04) // LOCATION_MZONE
                GameManager.Instance.TrySummonMonster(lc.unityCard.gameObject, lc.unityData, !faceUp);
            return true;
        }
        else if (lc.unityData != null)
        {
            bool wasPlayerPile;
            CardLocation sLoc = RemoveDataFromAllPiles(lc, out wasPlayerPile);
            
            if (destLoc == 0x08 || destLoc == 0x400) // LOCATION_SZONE
            {
                Transform zone = GameManager.Instance.GetFreeSpellZone(tPlayer == 0);
                if (zone != null)
                {
                    int zoneIdx = -1;
                    var zones = tPlayer == 0 ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
                    for (int i = 0; i < zones.Length; i++) if (zones[i] == zone) zoneIdx = i;
                    
                    if (zoneIdx != -1) GameManager.Instance.SetSpellTrapFromData(lc.unityData, tPlayer == 0, zoneIdx, faceUp);
                }
            }
            else if (destLoc == 0x04) // LOCATION_MZONE
                GameManager.Instance.SpecialSummonFromData(lc.unityData, tPlayer == 0, -1, faceUp, !faceUp, null, sLoc, wasPlayerPile);
            return true;
        }
        return false;
    }

    public void Overlay(object xyzCard, object materials)
    {
        Debug.LogWarning("[LUA STUB] Duel.Overlay chamado");
        // [FUTURO] Implementar lógica de adicionar Overlay Materials
    }

    // --- FUNÇÕES OBSOLETAS E WORKAROUNDS (YGOPro Legacy) ---
    public void SwapSequence(object c1, object c2)
    {
        LuaCard card1 = c1 as LuaCard;
        LuaCard card2 = c2 as LuaCard;
        if (card1 != null && card2 != null && card1.unityCard != null && card2.unityCard != null)
        {
            Transform parent1 = card1.unityCard.transform.parent;
            Transform parent2 = card2.unityCard.transform.parent;
            
            card1.unityCard.transform.SetParent(parent2, false);
            card2.unityCard.transform.SetParent(parent1, false);
            
            card1.unityCard.transform.localPosition = Vector3.zero;
            card2.unityCard.transform.localPosition = Vector3.zero;
            Debug.Log($"<color=cyan>[Workaround]</color> Posições trocadas entre {card1.unityData?.name} e {card2.unityData?.name}.");
        }
    }

    public void SwapDeckAndGrave(object player)
    {
        int p = ConvertToInt(player);
        Debug.LogWarning($"<color=yellow>[LUA STUB]</color> Duel.SwapDeckAndGrave chamado para o jogador {p} (Exchange of the Spirit)!");
        // [FUTURO] Inverter fisicamente as listas playerMainDeck e playerGraveyard no GameManager.
    }
    
    public void ReturnToField(object card, params object[] args) { Debug.LogWarning("[LUA STUB] Duel.ReturnToField chamado"); }
}