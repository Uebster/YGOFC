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

        CardEffectManager.Instance.StartCoroutine(DestroyCardsRoutine(toDestroy));
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

    public DynValue Draw(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        bool isPlayer = IsPlayer(player);

        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        CardEffectManager.Instance.StartCoroutine(DrawRoutine(isPlayer, amt));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Draw") });
    }

    private IEnumerator DrawRoutine(bool isPlayer, int amount)
    {
        if (isPlayer && GameManager.Instance != null && GameManager.Instance.canPlayerDrawFromDeck && !GameManager.Instance.isSimulating)
        {
            for (int i = 0; i < amount; i++)
            {
                int drawsLeft = amount - i;
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Efeito ativado: Compre {drawsLeft} carta(s) do seu Deck.");
                
                GameManager.Instance.pendingEffectDraws = 1;
                yield return new WaitWhile(() => GameManager.Instance.pendingEffectDraws > 0);
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                if (isPlayer) GameManager.Instance.DrawCard(true);
                else GameManager.Instance.DrawOpponentCard();
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.4f);
            }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(amount);
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
        SendReason sReason = SendReason.Effect;
        if ((ocgReason & 0x4000) != 0) sReason = SendReason.Discarded;
        else if ((ocgReason & 0x1) != 0) sReason = SendReason.Destroyed;
        else if ((ocgReason & 0x2) != 0) sReason = SendReason.Tribute;

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
            if (c.unityCard != null)
            {
                GameManager.Instance.MoveCard(c.unityCard, CardLocation.Graveyard, sReason);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
            else if (c.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sourceLoc = RemoveDataFromAllPiles(c.unityData, out wasPlayerPile);
                GameManager.Instance.SendToGraveyard(c.unityData, wasPlayerPile, sourceLoc, sReason);
                
                if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                {
                    CardFlightSettings flightSettings = null;
                    if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToGraveyard;
                    else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToGraveyard;
                    else if (sourceLoc == CardLocation.Banished) flightSettings = DuelFXManager.Instance.flightBanishToGraveyard;

                    if (flightSettings != null && flightSettings.enableFlight)
                    {
                        Vector3 startPos = GetPilePosition(sourceLoc, wasPlayerPile);
                        Vector3 endPos = wasPlayerPile ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
                        Quaternion rot = wasPlayerPile ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                        
                        bool isPile = sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.ExtraDeck || sourceLoc == CardLocation.Banished;
                        
                        if (isPile && DuelFXManager.Instance.extractionCinematic.enableExtraction)
                        {
                            bool startFaceUp = sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished;
                            bool animDone = false;
                            DuelFXManager.Instance.PlayExtractionCinematic(c.unityData, GameManager.Instance.GetCardBackTexture(), wasPlayerPile, sourceLoc, startPos, startFaceUp, true, (ghost, pos) => {
                                DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), true, true, pos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, false, () => animDone = true, ghost);
                            });
                            yield return new WaitUntil(() => animDone);
                        }
                        else
                        {
                            DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), true, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, true, null);
                        }
                    }
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
            if (c.unityCard != null)
            {
                GameManager.Instance.BanishCard(c.unityCard);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.4f);
            }
            else if (c.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sourceLoc = GetPileLocation(c.unityData, out wasPlayerPile);
                
                if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                {
                    Vector3 startPos = GetPilePosition(sourceLoc, wasPlayerPile);

                    CardFlightSettings flightSettings = null;
                    if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToBanished;
                    else if (sourceLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToBanished;
                    else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToBanished;

                    bool animDone = false;
                    DuelFXManager.Instance.PlayExtractionCinematic(c.unityData, GameManager.Instance.GetCardBackTexture(), wasPlayerPile, sourceLoc, startPos, sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished, true, (ghost, pos) => {
                        CardEffectManager.Instance.StartCoroutine(ProcessBanishVFXAndFlight(c.unityData, wasPlayerPile, sourceLoc, flightSettings, ghost, pos, () => animDone = true));
                    });
                    
                    yield return new WaitUntil(() => animDone);
                }
                else
                {
                    RemoveDataFromAllPiles(c.unityData, out wasPlayerPile);
                    GameManager.Instance.RemoveFromPlay(c.unityData, wasPlayerPile);
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
        
        if (cd.isInPile)
        {
            cd.transform.SetAsLastSibling(); 
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

        while(t < 1f) { t += Time.deltaTime / dur; cd.transform.localScale = Vector3.Lerp(origScale, peakScale, Mathf.SmoothStep(0, 1, t)); yield return null; }
        t = 0;
        while(t < 1f) { t += Time.deltaTime / dur; cd.transform.localScale = Vector3.Lerp(peakScale, origScale, Mathf.SmoothStep(0, 1, t)); yield return null; }
        cd.transform.localScale = origScale;
        
        if (!hadOutline) GameObject.Destroy(outline);
        else { outline.effectColor = origColor; outline.enabled = false; }
        
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator ProcessBanishVFXAndFlight(CardData cData, bool wasPlayerPile, CardLocation sourceLoc, CardFlightSettings flightSettings, GameObject ghost, Vector3 pos, System.Action onComplete)
    {
        bool useVortex = DuelFXManager.Instance.useBanishPrefab && DuelFXManager.Instance.banishVFX != null;

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

        RemoveDataFromAllPiles(cData, out wasPlayerPile);
        GameManager.Instance.RemoveFromPlay(cData, wasPlayerPile);

        if (flightSettings != null && flightSettings.enableFlight && !useVortex)
        {
            Vector3 endPos = wasPlayerPile ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
            Quaternion rot = wasPlayerPile ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
            
            bool flightDone = false;
            
            DuelFXManager.Instance.PlayCardFlight(cData, GameManager.Instance.GetCardBackTexture(), true, true, pos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, rot, rot, flightSettings, false, () => {
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

    private CardLocation GetPileLocation(CardData data, out bool wasPlayerPile)
    {
        wasPlayerPile = true;
        if (GameManager.Instance == null) return CardLocation.Unknown;
        if (DeckManager.Instance != null) {
            if (DeckManager.Instance.GetPlayerDeck().Contains(data)) { wasPlayerPile = true; return CardLocation.Deck; }
            if (DeckManager.Instance.GetOpponentDeck().Contains(data)) { wasPlayerPile = false; return CardLocation.Deck; }
        }
        if (GameManager.Instance.GetPlayerMainDeck().Contains(data)) { wasPlayerPile = true; return CardLocation.Deck; }
        if (GameManager.Instance.GetOpponentMainDeck().Contains(data)) { wasPlayerPile = false; return CardLocation.Deck; }
        if (GameManager.Instance.GetPlayerExtraDeck().Contains(data)) { wasPlayerPile = true; return CardLocation.ExtraDeck; }
        if (GameManager.Instance.GetOpponentExtraDeck().Contains(data)) { wasPlayerPile = false; return CardLocation.ExtraDeck; }
        if (GameManager.Instance.GetPlayerGraveyard().Contains(data)) { wasPlayerPile = true; return CardLocation.Graveyard; }
        if (GameManager.Instance.GetOpponentGraveyard().Contains(data)) { wasPlayerPile = false; return CardLocation.Graveyard; }
        if (GameManager.Instance.GetPlayerRemoved().Contains(data)) { wasPlayerPile = true; return CardLocation.Banished; }
        if (GameManager.Instance.GetOpponentRemoved().Contains(data)) { wasPlayerPile = false; return CardLocation.Banished; }
        return CardLocation.Unknown;
    }

    public int SendtoDeck(object target, object player, object seq, object reason)
    {
        CardEffectManager.Instance.StartCoroutine(SendtoDeckRoutine(target, player, seq, reason));
        return 1; // Retorna um valor síncrono, mas a animação roda em background
    }

    private IEnumerator SendtoDeckRoutine(object target, object player, object seq, object reason)
    {
        int count = 0;
        bool toTop = (ConvertToInt(seq) == 0);
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
                if (c.unityData != null) cardsToAnimate.Add(c.unityData);

                bool targetDeckIsPlayer = true;
                bool sFaceUp = true;
                CardLocation sLoc = CardLocation.Unknown;
                Vector3? sPos = null;

                if (c.unityCard != null) 
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
                    
                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(c.unityCard);
                    GameObject.Destroy(c.unityCard.gameObject);

                    count++; 
                }                 
                else if (c.unityData != null) { 
                    bool wasPlayerPile; 
                    sLoc = RemoveDataFromAllPiles(c.unityData, out wasPlayerPile); 
                    sPos = GetPilePosition(sLoc, wasPlayerPile);
                    targetDeckIsPlayer = playerProvided ? (pInt == 0) : wasPlayerPile;
                    
                    isOwnerList.Add(targetDeckIsPlayer);
                    startFaceUpList.Add(true);
                    sourceLocList.Add(sLoc);
                    startPosList.Add(sPos);

                    if(targetDeckIsPlayer) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, c.unityData); 
                    else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, c.unityData); 
                    count++; 
                }
            }
            // Debug.Log($"[Lua] Duel.SendtoDeck(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card)
        {
            if (card.unityData != null) cardsToAnimate.Add(card.unityData);
            bool targetDeckIsPlayer = true;
            bool sFaceUp = true;
            CardLocation sLoc = CardLocation.Unknown;
            Vector3? sPos = null;

            if (card.unityCard != null)
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
                
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card.unityCard);
                GameObject.Destroy(card.unityCard.gameObject);

                count = 1;;
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile; 
                sLoc = RemoveDataFromAllPiles(card.unityData, out wasPlayerPile);
                sPos = GetPilePosition(sLoc, wasPlayerPile);
                targetDeckIsPlayer = playerProvided ? (pInt == 0) : wasPlayerPile;
                
                isOwnerList.Add(targetDeckIsPlayer);
                startFaceUpList.Add(true);
                sourceLocList.Add(sLoc);
                startPosList.Add(sPos);

                if(targetDeckIsPlayer) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, card.unityData); 
                else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, card.unityData);
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

        if (!toTop && GameManager.Instance != null) 
        {
            if (playerProvided) GameManager.Instance.ShuffleDeck(pInt == 0);
            else 
            {
                // Se devolveu cartas de donos diferentes, embaralha ambos os decks afetados
                if (isOwnerList.Contains(true)) GameManager.Instance.ShuffleDeck(true);
                if (isOwnerList.Contains(false)) GameManager.Instance.ShuffleDeck(false);
            }
        }
    }

    public void ShuffleDeck(object player)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ShuffleDeck(IsPlayer(player));
    }

    public void ShuffleHand(object player)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ShuffleHand(IsPlayer(player));
    }

    public void Release(object target, object reason)
    {
        if (target is LuaGroup group)
        {
            this.lastCostGroup = group; // Armazena o grupo para exclusão de alvo
            foreach (var c in group.cards)
                if (c.unityCard != null) GameManager.Instance.TributeCard(c.unityCard);
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            LuaGroup tempGroup = new LuaGroup();
            tempGroup.AddCard(card);
            this.lastCostGroup = tempGroup; // Armazena a carta para exclusão de alvo
            GameManager.Instance.TributeCard(card.unityCard);
        }
    }

    public void ReleaseRitualMaterial(object target) { } // O C# assume a destruição física através da UI de Ritual!

    public void RaiseEvent(object triggerCard, object eventCode, object eg, object ep, object ev, object re, object r)
    {
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.TriggerLuaEvent(ConvertToInt(eventCode), triggerCard);
    }

    public void RaiseSingleEvent(object triggerCard, object eventCode, object e, object ep, object ev, object re, object r)
    {
        if (triggerCard is LuaCard lc && CardEffectManager.Instance != null)
        {
            int code = ConvertToInt(eventCode);
            var matchingEffects = lc.registeredEffects.FindAll(eff => eff.code == code);
            
            foreach(var effect in matchingEffects)
            {
                if (CardEffectManager.Instance.CanActivateEffect(lc, effect, lc.GetControler(), null))
                {
                    CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.chainManager.BuildAndResolveChainRoutine(lc, effect, null, lc.GetControler(), null));
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
        if (CardEffectManager.Instance != null) return CardEffectManager.Instance.NegateChainLink(ConvertToInt(chainc));
        return true; 
    }
    public bool NegateEffect(object chainc) 
    { 
        if (CardEffectManager.Instance != null) return CardEffectManager.Instance.NegateChainLink(ConvertToInt(chainc));
        return true; 
    }
    public void ChangeChainOperation(object chainc, object op) { }

    public void SetPossibleOperationInfo(params object[] args) { }
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
            if (c.unityCard != null)
            {
                GameManager.Instance.ReturnToHand(c.unityCard);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
            else if (c.unityData != null)
            {
                bool wasPlayerPile;
                RemoveDataFromAllPiles(c.unityData, out wasPlayerPile);
                GameManager.Instance.AddCardToHand(c.unityData, wasPlayerPile, null, CardLocation.Deck, false, wasPlayerPile);
                count++;
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.3f);
            }
        }

        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    private CardLocation RemoveDataFromAllPiles(CardData data, out bool wasPlayerPile)
    {
        wasPlayerPile = true;
        if (GameManager.Instance == null) return CardLocation.Unknown;
        if (DeckManager.Instance != null) {
            if (DeckManager.Instance.GetPlayerDeck().Contains(data)) { 
                DeckManager.Instance.GetPlayerDeck().Remove(data); 
                DeckManager.Instance.UpdateDeckVisuals(); 
                if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false;
                wasPlayerPile = true; return CardLocation.Deck; 
            }
            if (DeckManager.Instance.GetOpponentDeck().Contains(data)) { 
                DeckManager.Instance.GetOpponentDeck().Remove(data); 
                DeckManager.Instance.UpdateDeckVisuals(); 
                if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false;
                wasPlayerPile = false; return CardLocation.Deck; 
            }
        }
        // Fallbacks de Segurança
        if (GameManager.Instance.GetPlayerMainDeck().Contains(data)) { GameManager.Instance.GetPlayerMainDeck().Remove(data); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false; wasPlayerPile = true; return CardLocation.Deck; }
        if (GameManager.Instance.GetOpponentMainDeck().Contains(data)) { GameManager.Instance.GetOpponentMainDeck().Remove(data); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false; wasPlayerPile = false; return CardLocation.Deck; }
        
        if (GameManager.Instance.GetPlayerExtraDeck().Contains(data)) { GameManager.Instance.GetPlayerExtraDeck().Remove(data); wasPlayerPile = true; return CardLocation.ExtraDeck; }
        if (GameManager.Instance.GetOpponentExtraDeck().Contains(data)) { GameManager.Instance.GetOpponentExtraDeck().Remove(data); wasPlayerPile = false; return CardLocation.ExtraDeck; }
        if (GameManager.Instance.GetPlayerGraveyard().Contains(data)) { GameManager.Instance.GetPlayerGraveyard().Remove(data); wasPlayerPile = true; return CardLocation.Graveyard; }
        if (GameManager.Instance.GetOpponentGraveyard().Contains(data)) { GameManager.Instance.GetOpponentGraveyard().Remove(data); wasPlayerPile = false; return CardLocation.Graveyard; }
        if (GameManager.Instance.GetPlayerRemoved().Contains(data)) { GameManager.Instance.GetPlayerRemoved().Remove(data); wasPlayerPile = true; return CardLocation.Banished; }
        if (GameManager.Instance.GetOpponentRemoved().Contains(data)) { GameManager.Instance.GetOpponentRemoved().Remove(data); wasPlayerPile = false; return CardLocation.Banished; }
        return CardLocation.Unknown;
    }

    public bool SpecialSummon(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        bool isPlayerSummoning = IsPlayer(player);
        int posInt = ConvertToInt(pos);
        bool inDefense = (posInt & 0x8) != 0 || (posInt & 0xA) != 0; // Verifica se tem flag de defesa

        if (target is LuaGroup group && group.cards.Count > 0)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                {
                    bool originalOwner = c.unityCard.ownerPlayer;
                    if (GameManager.Instance.playerHand.Contains(c.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(c.unityCard.gameObject);
                    else if (GameManager.Instance.opponentHand.Contains(c.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(c.unityCard.gameObject);
                    
                    Vector3? sPos = null; CardLocation sLoc = CardLocation.Unknown;
                    if (c.unityCard != null) { sPos = c.unityCard.transform.position; sLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand; }
                    else { sLoc = CardLocation.Graveyard; sPos = isPlayerSummoning ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; }
                    
                    GameManager.Instance.SpecialSummonFromData(c.unityCard.CurrentCardData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc, originalOwner);
                    GameObject.Destroy(c.unityCard.gameObject);
                }
                else if (c.unityData != null)
                {
                    bool wasPlayerPile; RemoveDataFromAllPiles(c.unityData, out wasPlayerPile);
                    GameManager.Instance.SpecialSummonFromData(c.unityData, isPlayerSummoning, -1, true, inDefense, null, CardLocation.Graveyard, wasPlayerPile);
                }
            }
            // Debug.Log($"[Lua] Duel.SpecialSummon(Grupo)");
            return true;
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null)
            {
                bool originalOwner = card.unityCard.ownerPlayer;
                if (GameManager.Instance.playerHand.Contains(card.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(card.unityCard.gameObject);
                else if (GameManager.Instance.opponentHand.Contains(card.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(card.unityCard.gameObject);
                
                Vector3? sPos = null; CardLocation sLoc = CardLocation.Unknown;
                if (card.unityCard != null) { sPos = card.unityCard.transform.position; sLoc = card.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand; }
                else { sLoc = CardLocation.Graveyard; sPos = isPlayerSummoning ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; }
                
                GameManager.Instance.SpecialSummonFromData(card.unityCard.CurrentCardData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc, originalOwner);
                GameObject.Destroy(card.unityCard.gameObject);
                // Debug.Log($"[Lua] Duel.SpecialSummon({card.unityCard.CurrentCardData.name})");
                return true;
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile; RemoveDataFromAllPiles(card.unityData, out wasPlayerPile);
                GameManager.Instance.SpecialSummonFromData(card.unityData, isPlayerSummoning, -1, true, inDefense, null, CardLocation.Graveyard, wasPlayerPile);
                // Debug.Log($"[Lua] Duel.SpecialSummon({card.unityData.name} - Token)");
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
            tokenData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == strCode);
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

        List<CardDisplay> cardsToChange = new List<CardDisplay>();
        if (target is LuaGroup group) { foreach (var c in group.cards) if (c.unityCard != null) cardsToChange.Add(c.unityCard); }
        else if (target is LuaCard card && card.unityCard != null) { cardsToChange.Add(card.unityCard); }

        CardEffectManager.Instance.StartCoroutine(ChangePositionRoutine(cardsToChange, posAU, posAD, posDU, posDD));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ChangePosition") });
    }

    private IEnumerator ChangePositionRoutine(List<CardDisplay> cards, int posAU, int posAD, int posDU, int posDD)
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

            bool isAttackTarget = (targetPos == 1 || targetPos == 2);
            bool isFaceUpTarget = (targetPos == 1 || targetPos == 4);

            if ((isAttackTarget && c.position == CardDisplay.BattlePosition.Defense) || (!isAttackTarget && c.position == CardDisplay.BattlePosition.Attack))
            {
                c.position = isAttackTarget ? CardDisplay.BattlePosition.Attack : CardDisplay.BattlePosition.Defense;
                c.transform.localRotation = Quaternion.Euler(0, 0, isAttackTarget ? (c.isPlayerCard ? 0f : 180f) : (c.isPlayerCard ? 90f : -90f));
            }

            if (isFaceUpTarget && c.isFlipped)
            {
                pendingAnimations++;
                c.RevealCard(false, true, () => { 
                    if (GameManager.Instance != null) { 
                        GameManager.Instance.OnBattlePositionChanged(c); 
                        if (isAttackTarget) GameManager.Instance.OnFlipSummon(c); 
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
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, false, IsPlayer(player), false, c.unityData.level >= 5, null, null, sourcePos, sourceLoc);
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
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, true, IsPlayer(player), true, c.unityData.level >= 5, null, null, sourcePos, sourceLoc);
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
    private IEnumerator DestroyCardsRoutine(List<CardDisplay> cards)
    {
        int count = 0;
        foreach (var c in cards)
        {
            if (c != null && c.isOnField)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(c);
                GameManager.Instance.MoveCard(c, CardLocation.Graveyard, SendReason.Destroyed);
                count++;
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.25f);
            }
        }
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }
}