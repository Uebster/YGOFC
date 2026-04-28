using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public partial class GameManager
{
    // ==============================================================================
    // AÇÕES DE TABULEIRO E MOVIMENTAÇÃO FÍSICA
    // ==============================================================================

    public void ReturnToHand(CardDisplay card)
    {
        if (card == null) return;

        CardData data = card.CurrentCardData;
        bool isController = card.isPlayerCard;
        bool isOwner = card.ownerPlayer;
        Vector3 startPos = card.transform.position;
        CardLocation prevLoc = card.CurrentLocation;
        bool isFieldSpellZone = card.isOnField && (card.transform.parent == duelFieldUI.playerFieldSpell || card.transform.parent == duelFieldUI.opponentFieldSpell);

        // Remove modificadores
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Remove o FieldStatUI fantasma que ficaria preso na zona antiga
        Transform oldZone = card.transform.parent;
        if (oldZone != null)
        {
            FieldStatUI statUI = oldZone.GetComponentInChildren<FieldStatUI>();
            if (statUI != null) Destroy(statUI.gameObject);
        }

        // REAPROVEITAMENTO FÍSICO: Não destruímos o GameObject. Isso mantém as referências LUA vivas!
        card.isOnField = false;
        card.isInteractable = true;
        card.isPlayerCard = isOwner; // RESET TO OWNER! A carta voltou pra casa.
        card.hoverYOffset = isOwner ? playerHandHoverYOffset : opponentHandHoverYOffset;
        
        Transform handTransform = isOwner ? playerHandLayoutGroup : opponentHandLayoutGroup;
        card.transform.SetParent(handTransform);
        
        if (isOwner) { if (!playerHand.Contains(card.gameObject)) playerHand.Add(card.gameObject); }
        else { if (!opponentHand.Contains(card.gameObject)) opponentHand.Add(card.gameObject); }

        card.transform.localScale = handCardScale;
        
        // Animação assíncrona reaproveitando a corrotina de voo
        if (enableDrawAnimation && !isSimulating && gameObject.activeInHierarchy)
        {
            CardFlightSettings settings = null;
            if (DuelFXManager.Instance != null)
            {
                if (prevLoc == CardLocation.Field) settings = isFieldSpellZone ? DuelFXManager.Instance.flightFieldSpellZoneToHand : DuelFXManager.Instance.flightFieldToHand;
                else if (prevLoc == CardLocation.Graveyard) settings = DuelFXManager.Instance.flightGraveyardToHand;
                else if (prevLoc == CardLocation.Banished) settings = DuelFXManager.Instance.flightBanishToHand;
                else settings = DuelFXManager.Instance.flightDeckToHand;
            }
            StartCoroutine(AnimateCardToHand(card.gameObject, isOwner, startPos, prevLoc, settings, false));
        }
        else
        {
            if (isOwner || showOpponentHand) card.ShowFront();
            else { card.transform.localRotation = Quaternion.Euler(0, 0, 180f); card.ShowBack(); }
        }

        Debug.Log($"{data.name} retornada para a mão.");

        // 0343 - Criosphinx
        if (data.type.Contains("Monster") && IsCardActiveOnField("0343"))
        {
            Debug.Log($"Criosphinx: Monstro retornou à mão, o dono ({(isOwner ? "Player" : "Oponente")}) descarta 1 carta.");
            DiscardRandomHand(isOwner, 1);
        }
    }

    public void ReturnToDeck(CardDisplay card, bool toTop)
    {
        if (DeckManager.Instance != null) DeckManager.Instance.ReturnToDeck(card, toTop);
    }

    public void TributeCard(CardDisplay card)
    {
        if (card == null) return;

        // Efeito Visual
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayTributeEffect(card);

        // Remove modificadores
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Envia para o GY (Lógica de dados)
        SendToGraveyard(card.CurrentCardData, card.ownerPlayer);

        card.transform.SetParent(null);
        // Destrói o objeto visual
        Destroy(card.gameObject);
    }

    public void RemoveCardFromHand(CardData card, bool isPlayer)
    {
        if (card == null) return;
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        GameObject cardToRemove = hand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData == card);
        if (cardToRemove != null)
        {
            hand.Remove(cardToRemove);
            Destroy(cardToRemove);
        }
    }

    public void DiscardRandomHand(bool isPlayer, int amount, bool causedByOpponent = false)

    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        if (hand.Count == 0) return;

        int count = Mathf.Min(amount, hand.Count);

        for (int i = 0; i < count; i++)
        {
            if (hand.Count == 0) break;
            int rnd = Random.Range(0, hand.Count);
            GameObject cardGO = hand[rnd];
            CardDisplay cd = cardGO.GetComponent<CardDisplay>();
            DiscardCard(cd, causedByOpponent);
        }
    }

    public void ClearFieldZonesOnly()
    {
        if (duelFieldUI == null) return;

        List<CardDisplay> cardsToClear = new List<CardDisplay>();

        System.Action<Transform[]> CollectCardsFromZones = (zones) => {
            if (zones == null) return;
            foreach (var z in zones) {
                if (z == null) continue;
                foreach (Transform child in z) {
                    CardDisplay cd = child.GetComponent<CardDisplay>();
                    if (cd != null) cardsToClear.Add(cd);
                }
            }
        };

        CollectCardsFromZones(duelFieldUI.playerMonsterZones);
        CollectCardsFromZones(duelFieldUI.opponentMonsterZones);
        CollectCardsFromZones(duelFieldUI.playerSpellZones);
        CollectCardsFromZones(duelFieldUI.opponentSpellZones);
        
        if (duelFieldUI.playerFieldSpell != null && duelFieldUI.playerFieldSpell.childCount > 0) {
            CardDisplay cd = duelFieldUI.playerFieldSpell.GetComponentInChildren<CardDisplay>();
            if (cd != null) cardsToClear.Add(cd);
        }
        if (duelFieldUI.opponentFieldSpell != null && duelFieldUI.opponentFieldSpell.childCount > 0) {
            CardDisplay cd = duelFieldUI.opponentFieldSpell.GetComponentInChildren<CardDisplay>();
            if (cd != null) cardsToClear.Add(cd);
        }

        foreach (CardDisplay card in cardsToClear)
        {
            if (card != null && card.gameObject != null)
            {
                // Simula destruição: envia para o GY e destrói o objeto
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
                SendToGraveyard(card.CurrentCardData, card.ownerPlayer, CardLocation.Field, SendReason.Destroyed);
                
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);
                
                card.transform.SetParent(null);
                Destroy(card.gameObject);
            }
        }
    }

    public void SetSpellTrapFromData(CardData cardData, bool isPlayer, int zoneIndex, bool faceUp = false)
    {
        if (cardData == null || (!cardData.type.Contains("Spell") && !cardData.type.Contains("Trap")))
        {
            Debug.LogError("[GameManager] SetSpellTrapFromData: Card is not a Spell or Trap.");
            return;
        }
        
        if (duelFieldUI == null) return;
        Transform[] spellZones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        if (zoneIndex < 0 || zoneIndex >= spellZones.Length) return;

        Transform zone = spellZones[zoneIndex];
        if (zone.childCount > 0)
        {
            Debug.LogWarning($"[GameManager] SetSpellTrapFromData: Zona {zoneIndex} já está ocupada.");
            return;
        }

        GameObject cardGO = Instantiate(cardPrefab, zone);
        CardDisplay cardDisplay = cardGO.GetComponent<CardDisplay>();

        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = fieldCardScale;
        cardDisplay.isInteractable = true; // Set to true so it can be flipped up
        cardDisplay.isPlayerCard = isPlayer;
        cardDisplay.isOnField = true;
        
        cardDisplay.SetCard(cardData, cardBackTexture, faceUp);
        cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 0f : 180f);
    }


    // --- AÇÕES DE JOGO PADRONIZADAS (GAME ACTIONS) ---

    public void BanishCard(CardDisplay card)
    {
        if (card == null) return;
        
        bool isController = card.isPlayerCard;
        bool isOwner = card.ownerPlayer;
        bool wasOnField = card.isOnField;
        CardLocation prevLoc = card.CurrentLocation;
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        // Remove da lista da mão do controlador atual se estiver lá
        if (isController) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        // Adiciona à lista de removidas
        RemoveFromPlay(card.CurrentCardData, isOwner);

        // Remove modificadores que esta carta gerou em outras
        if (wasOnField && CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        if (DuelFXManager.Instance != null && !isSimulating) 
        {
            bool isFieldSpellZone = wasOnField && (card.transform.parent == duelFieldUI.playerFieldSpell || card.transform.parent == duelFieldUI.opponentFieldSpell);
                    Quaternion startRotDeck = card.transform.rotation;
            CardFlightSettings flightSettings = null;
            if (prevLoc == CardLocation.Hand) flightSettings = DuelFXManager.Instance.flightHandToBanished;
            else if (wasOnField) flightSettings = isFieldSpellZone ? DuelFXManager.Instance.flightFieldSpellZoneToBanished : DuelFXManager.Instance.flightFieldToBanished;
            else if (prevLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToBanished;
            else if (prevLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToBanished;
            else flightSettings = DuelFXManager.Instance.flightGraveyardToBanished; // Fallback

            if (flightSettings != null && flightSettings.enableFlight)
            {
                Vector3 endPos = isOwner ? playerRemovedDisplay.transform.position : opponentRemovedDisplay.transform.position;
                Quaternion endRot = isOwner ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                DuelFXManager.Instance.PlayCardFlight(card.CurrentCardData, cardBackTexture, !card.isFlipped, true, startPos, endPos, 
                    wasOnField ? fieldCardScale : handCardScale, fieldCardScale, 
                    startRot, endRot, flightSettings, false, () => {
                        if (DuelFXManager.Instance != null && DuelFXManager.Instance.useBanishPrefab && DuelFXManager.Instance.banishVFX != null) DuelFXManager.Instance.SpawnVFXPublic(DuelFXManager.Instance.banishVFX, endPos);
                    });
            }
            else if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayBanishEffect(card);
        }
        else if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayBanishEffect(card);

        card.transform.SetParent(null);
        // Destrói o objeto visual (Banish não vai pro GY, então não chama SendToGraveyard)
        Destroy(card.gameObject);
    }
    
    private IEnumerator AnimateCardToHand(GameObject realCard, bool isPlayer, Vector3 startPos, CardLocation sourceLoc, CardFlightSettings settings, bool isDraw = false)
    {
        CardDisplay realCardDisplay = realCard.GetComponent<CardDisplay>();
        CanvasGroup realCardCG = realCard.GetComponent<CanvasGroup>();
        if (realCardCG == null) realCardCG = realCard.AddComponent<CanvasGroup>();
        realCardCG.alpha = 0; // Esconde a carta real

        // Espera o LayoutGroup fazer sua mágica
        yield return new WaitForEndOfFrame();

        Vector3 endPos = realCard.transform.position;
        
        System.Action onComplete = () => {
            if (realCard != null) {
                if (isPlayer) realCardDisplay.ShowFront(false);
                else {
                    if (showOpponentHand) realCardDisplay.ShowFront(false);
                    else realCardDisplay.ShowBack(false);
                }
                realCardCG.alpha = 1;
            }
        };

        if (settings != null && DuelFXManager.Instance != null && settings.enableFlight)
        {
            Vector3 sScale = (sourceLoc == CardLocation.Hand) ? handCardScale : fieldCardScale;

            bool isPile = sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.ExtraDeck || sourceLoc == CardLocation.Banished;
            bool startFaceUp = sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished || sourceLoc == CardLocation.Field;
            bool endFaceUp = isPlayer || showOpponentHand;
            Quaternion startRot = Quaternion.Euler(0, 0, isPlayer ? 0 : 180f);
            Quaternion endRot = Quaternion.Euler(0, 0, isPlayer ? 0 : 180f);

            bool ghostEndFaceUp = endFaceUp;
            if (isPile && DuelFXManager.Instance != null) {
                var pileSettings = DuelFXManager.Instance.extractionCinematic.deck;
                if (sourceLoc == CardLocation.Graveyard) pileSettings = DuelFXManager.Instance.extractionCinematic.graveyard;
                else if (sourceLoc == CardLocation.ExtraDeck) pileSettings = DuelFXManager.Instance.extractionCinematic.extraDeck;
                else if (sourceLoc == CardLocation.Banished) pileSettings = DuelFXManager.Instance.extractionCinematic.banished;
                if (!pileSettings.flipDuringExtraction) ghostEndFaceUp = startFaceUp;
            }

            if (isPile && !isDraw && DuelFXManager.Instance.extractionCinematic.enableExtraction)
            {
                bool animDone = false;
                DuelFXManager.Instance.PlayExtractionCinematic(realCardDisplay.CurrentCardData, cardBackTexture, isPlayer, sourceLoc, startPos, startFaceUp, endFaceUp, (ghost, pos) => {
                    DuelFXManager.Instance.PlayCardFlight(realCardDisplay.CurrentCardData, cardBackTexture, ghostEndFaceUp, endFaceUp, pos, endPos, sScale, handCardScale, startRot, endRot, settings, false, () => { animDone = true; }, ghost);
                });
                yield return new WaitUntil(() => animDone);
                onComplete();
            }
            else
            {
                DuelFXManager.Instance.PlayCardFlight(realCardDisplay.CurrentCardData, cardBackTexture, startFaceUp, endFaceUp, startPos, endPos, sScale, handCardScale, startRot, endRot, settings, isPile, onComplete);
            }
        }
        else
        {
            GameObject ghost = Instantiate(cardPrefab, GetUIParent());
            CardDisplay ghostDisplay = ghost.GetComponent<CardDisplay>();
            if (ghostDisplay != null && ghostDisplay.cardImage != null)
            {
                ghostDisplay.cardImage.texture = cardBackTexture;
            }
            ghost.transform.position = startPos;

            float duration = 0.6f / (DuelFXManager.Instance != null && DuelFXManager.Instance.animationSpeed > 0 ? DuelFXManager.Instance.animationSpeed : 1f);
            float elapsed = 0f;
            bool textureSwapped = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                ghost.transform.position = Vector3.Lerp(startPos, endPos, t);

                float yRot = Mathf.Lerp(180f, 0f, t);
                ghost.transform.rotation = Quaternion.Euler(0, yRot, 0);

                if (t >= 0.5f && !textureSwapped)
                {
                    textureSwapped = true;
                    if (isPlayer || showOpponentHand)
                    {
                        Texture2D frontTex = realCardDisplay.GetFrontTexture();
                        if (frontTex != null) ghostDisplay.ForceTexture(frontTex);
                    }
                }
                yield return null;
            }

            Destroy(ghost);
            onComplete();
        }
    }
    // Método para enviar carta para o cemitério (Exemplo de uso)
    public void SendToGraveyard(CardData card, bool isPlayer, CardLocation fromLocation = CardLocation.Unknown, SendReason reason = SendReason.Unknown)
    {
        // Tokens evaporam ao sair do campo, não entram no GY.
        if (card == null || card.id == "TOKEN") return;

        if (isPlayer)
        {
            playerGraveyard.Add(card);
        }
        else
        {
            opponentGraveyard.Add(card);
        }

        if (DuelScoreManager.Instance != null)
        {
            // Registra pontuação se for carta do jogador indo pro cemitério
            if (isPlayer)
            {
                DuelScoreManager.Instance.RecordCardSentToGY();
            }
            // Registra pontuação se for monstro do inimigo indo pro cemitério (destruído)
            else if (card.type.Contains("Monster"))
            {
                DuelScoreManager.Instance.RecordEnemyMonsterDestroyed();
            }
        }

        // Nota: A remoção de modificadores é feita no método Destroy/OnCardLeavesField do CardDisplay, não aqui.

        // Notifica o sistema de efeitos (Gatilhos Globais)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnCardSentToGraveyard(card, isPlayer, fromLocation, reason);
        }

        UpdatePileVisuals();
    }

    // === MÉTODO UNIFICADO DE MOVIMENTAÇÃO DE CARTAS ===
    /// <summary>
    /// Move uma carta entre zonas de forma centralizada e consistente.
    /// Detecta automaticamente a zona de origem e aplica a lógica apropriada.
    /// </summary>
    public void MoveCard(CardDisplay card, CardLocation destination, SendReason reason = SendReason.Unknown)
    {
        if (card == null) return;
        
        bool isController = card.isPlayerCard;
        bool isOwner = card.ownerPlayer;
        CardData data = card.CurrentCardData;
        string logPrefix = $"[MoveCard] {data.name} ({(isOwner ? "Player" : "Opponent")})";
        
        // FASE 13: Track previous location before moving
        card.previousPreviousLocation = card.previousLocation;
        card.previousLocation = card.CurrentLocation;
        card.previousOwner = (isController ? 0 : 1);
        // Debug.Log($"{logPrefix} | Tracked history: from {card.previousLocation} (owner: {card.previousOwner})");
        
        // Declaração unificada de variáveis para evitar erros de escopo
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        bool wasOnField = card.isOnField;
        CardLocation prevLoc = card.previousLocation;
        bool isFieldSpellZone = wasOnField && (card.transform.parent == duelFieldUI.playerFieldSpell || card.transform.parent == duelFieldUI.opponentFieldSpell);
        
        // Remove modificadores antes de sair do campo
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Aplica a movimentação baseada no destino
        switch (destination)
        {
            case CardLocation.Graveyard:
                // Debug.Log($"{logPrefix} -> Graveyard");
                
                if (isController) playerHand.Remove(card.gameObject);
                else opponentHand.Remove(card.gameObject);
                SendToGraveyard(data, isOwner, card.isOnField ? CardLocation.Field : CardLocation.Hand, reason);
                card.transform.SetParent(null);
                Destroy(card.gameObject);

                if (DuelFXManager.Instance != null && !isSimulating)
                {
                    CardFlightSettings flightSettings = wasOnField ? (isFieldSpellZone ? DuelFXManager.Instance.flightFieldSpellZoneToGraveyard : DuelFXManager.Instance.flightFieldToGraveyard) : DuelFXManager.Instance.flightHandToGraveyard;
                    if (flightSettings != null && flightSettings.enableFlight && reason != SendReason.Destroyed && reason != SendReason.Battle && reason != SendReason.Tribute)
                    {
                        Vector3 endPos = isOwner ? playerGraveyardDisplay.transform.position : opponentGraveyardDisplay.transform.position;
                        Quaternion endRot = isOwner ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                        DuelFXManager.Instance.PlayCardFlight(data, cardBackTexture, !card.isFlipped, true, startPos, endPos, 
                            wasOnField ? fieldCardScale : handCardScale, fieldCardScale, 
                            startRot, endRot, flightSettings, false, null);
                    }
                }
                break;

            case CardLocation.Hand:
                // Debug.Log($"{logPrefix} -> Hand");
                
                ReturnToHand(card);
                break;

            case CardLocation.Deck:
                // Debug.Log($"{logPrefix} -> Deck (Top)");

                // Delega para DeckManager
                if (DeckManager.Instance != null) 
                    DeckManager.Instance.ReturnToDeck(card, true);

                if (DuelFXManager.Instance != null && !isSimulating)
                {
                    CardFlightSettings flightSettings = null;
                    if (prevLoc == CardLocation.Hand) flightSettings = DuelFXManager.Instance.flightHandToDeck;
                    else if (wasOnField) flightSettings = isFieldSpellZone ? DuelFXManager.Instance.flightFieldSpellZoneToDeck : DuelFXManager.Instance.flightFieldToDeck;
                    else if (prevLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToDeck;
                    else if (prevLoc == CardLocation.Banished) flightSettings = DuelFXManager.Instance.flightBanishToDeck;
                    else flightSettings = DuelFXManager.Instance.flightGraveyardToDeck; // Fallback

                    if (flightSettings != null && flightSettings.enableFlight && reason != SendReason.Destroyed)
                    {
                        Vector3 endPos = isOwner ? playerDeckDisplay.transform.position : opponentDeckDisplay.transform.position;
                        Quaternion endRot = isOwner ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                        DuelFXManager.Instance.PlayCardFlight(data, cardBackTexture, !card.isFlipped, true, startPos, endPos, 
                            wasOnField ? fieldCardScale : handCardScale, fieldCardScale, 
                            startRot, endRot, flightSettings, false, null);
                    }
                }
                break;

            case CardLocation.Banished:
                // Debug.Log($"{logPrefix} -> Banished");
                
                if (DuelFXManager.Instance != null && !isSimulating)
                {
                    CardFlightSettings flightSettings = null;
                    if (prevLoc == CardLocation.Hand) flightSettings = DuelFXManager.Instance.flightHandToBanished;
                    else if (wasOnField) flightSettings = isFieldSpellZone ? DuelFXManager.Instance.flightFieldSpellZoneToBanished : DuelFXManager.Instance.flightFieldToBanished;
                    else if (prevLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToBanished;
                    else if (prevLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToBanished;
                    else flightSettings = DuelFXManager.Instance.flightGraveyardToBanished; // Fallback

                    if (flightSettings != null && flightSettings.enableFlight && reason != SendReason.Destroyed && reason != SendReason.Battle && reason != SendReason.Tribute)
                    {
                        Vector3 endPos = isOwner ? playerRemovedDisplay.transform.position : opponentRemovedDisplay.transform.position;
                        Quaternion endRot = isOwner ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                        DuelFXManager.Instance.PlayCardFlight(data, cardBackTexture, !card.isFlipped, true, startPos, endPos, 
                            wasOnField ? fieldCardScale : handCardScale, fieldCardScale, 
                            startRot, endRot, flightSettings, false, () => {
                                if (DuelFXManager.Instance != null && DuelFXManager.Instance.useBanishPrefab && DuelFXManager.Instance.banishVFX != null) DuelFXManager.Instance.SpawnVFXPublic(DuelFXManager.Instance.banishVFX, endPos);
                            });
                    }
                    else if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayBanishEffect(card);
                }
                
                if (isController) playerHand.Remove(card.gameObject);
                else opponentHand.Remove(card.gameObject);
                RemoveFromPlay(data, isOwner);
                card.transform.SetParent(null);
                Destroy(card.gameObject);
                break;

            case CardLocation.ExtraDeck:
                // Debug.Log($"{logPrefix} -> Extra Deck");
                
                if (isOwner) playerExtraDeck.Add(data);
                else opponentExtraDeck.Add(data);
                card.transform.SetParent(null);
                Destroy(card.gameObject);

                if (DuelFXManager.Instance != null && !isSimulating)
                {
                    CardFlightSettings flightSettings = wasOnField ? DuelFXManager.Instance.flightFieldToExtraDeck : DuelFXManager.Instance.flightGraveyardToExtraDeck;
                    if (flightSettings != null && flightSettings.enableFlight)
                    {
                        Vector3 endPos = isOwner ? playerExtraDeckDisplay.transform.position : opponentExtraDeckDisplay.transform.position;
                        Quaternion endRot = isOwner ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
                        DuelFXManager.Instance.PlayCardFlight(data, cardBackTexture, !card.isFlipped, true, startPos, endPos, 
                            wasOnField ? fieldCardScale : handCardScale, fieldCardScale, 
                            startRot, endRot, flightSettings, false, null);
                    }
                }
                break;

            default:
                // Debug.LogWarning($"{logPrefix} -> Destino desconhecido: {destination}");
                break;
        }
        
        RefreshAllCardsVisuals();
    }
    // Novo método para remover de jogo (Banir)
    public void RemoveFromPlay(CardData card, bool isPlayer)
    {
        // Tokens evaporam, não vão para a pilha de banidos
        if (card == null || card.id == "TOKEN") return;

        if (isPlayer)
        {
            playerRemoved.Add(card);
        }
        else
        {
            opponentRemoved.Add(card);
        }

        // TODO: Adicionar lógica de pontuação se necessário

        UpdatePileVisuals();
    }

    // Helper para contar cartas no campo de um jogador
    public int GetFieldCardCount(bool isPlayer)
    {
        int count = 0;
        if (duelFieldUI != null)
        {
            Transform[] monsterZones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
            Transform[] spellZones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
            Transform fieldZone = isPlayer ? duelFieldUI.playerFieldSpell : duelFieldUI.opponentFieldSpell;

            foreach (var z in monsterZones) if (z.childCount > 0) count++;
            foreach (var z in spellZones) if (z.childCount > 0) count++;
            if (fieldZone.childCount > 0) count++;
        }
        return count;
    }

    public int GetMonsterCount(bool isPlayer)
    {
        int count = 0;
        if (duelFieldUI != null)
        {
            Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
            foreach (var z in zones) if (z.childCount > 0) count++;
        }
        return count;
    }


    // --- LÓGICA DE SPELL / TRAP ---

    public bool PlaySpellTrap(GameObject cardGO, CardData cardData, bool isSet, Vector3? sourcePos = null, CardLocation sourceLoc = CardLocation.Hand)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;
        string cardName = cardData?.name ?? "Unknown";

        // CAPTURA A POSIÇÃO ANTES DE QUALQUER MOVIMENTO
        if (!sourcePos.HasValue && cardGO != null)
        {
            sourcePos = cardGO.transform.position;
            sourceLoc = (display != null && display.isOnField) ? CardLocation.Field : CardLocation.Hand;
        }

        // 0.5 Validação de Armadilha
        if (!devMode && isPlayer && cardData.type.Contains("Trap") && !isSet)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Traps devem ser setadas antes, não ativadas direto.");
            return false;
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: canPlacePlayerCards desativado.");
            return false;
        }
        // Bloqueia se for uma ação para o oponente, durante o turno do jogador, e o modo dev de controle do oponente estiver desligado.
        // FIX: Se estiver simulando (!isSimulating), ignora essa trava.
        if (!isPlayer && isPlayerTurn && !canPlaceOpponentCards && !isSimulating)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: canPlaceOpponentCards desativado durante turno do jogador.");
            return false;
        }

        // 1. Validação de Fase
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        if (!devMode && isPlayer && currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Spells só permitidos em Main Phase 1/2. Fase atual: {currentPhase}");
            return false;
        }

        // Validação de Condição de Ativação LUA antes de mover a carta para o campo
        if (!isSet && CardEffectManager.Instance != null)
        {
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(display);
            if (lc != null)
            {
                LuaEffect activationEffect = lc.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);
                if (activationEffect != null)
                {
                    int tp = isPlayer ? 0 : 1;
                    if (!CardEffectManager.Instance.CanActivateEffect(lc, activationEffect, tp, null))
                    {
                        Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Falhou na validação Lua de ativação.");
                        if (!isSimulating && UIManager.Instance != null) UIManager.Instance.ShowMessage("Não é possível ativar esta carta agora.");
                        return false; // Cancela a jogada antes de mover a carta!
                    }
                }
            }
        }

        Transform targetZone = null;

        string propStr = cardData.property != null ? cardData.property.ToLower() : "";
        string typeStr = cardData.type != null ? cardData.type.ToLower() : "";

        // 2. Verifica se é Field Spell
        if (propStr.Contains("field") || typeStr.Contains("field"))
        {
            if (duelFieldUI != null)
                targetZone = isPlayer ? duelFieldUI.playerFieldSpell : duelFieldUI.opponentFieldSpell;

            // Regra Clássica: Destrói qualquer Field Spell ativo no campo antes de colocar o novo
            if (duelFieldUI != null)
            {
                if (duelFieldUI.playerFieldSpell.childCount > 0)
                {
                    var oldField = duelFieldUI.playerFieldSpell.GetComponentInChildren<CardDisplay>();
                    if (oldField != null) {
                        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(oldField);
                        SendToGraveyard(oldField.CurrentCardData, true, CardLocation.Field, SendReason.Rule);
                        oldField.transform.SetParent(null);
                        Destroy(oldField.gameObject);
                    }
                }
                if (duelFieldUI.opponentFieldSpell.childCount > 0)
                {
                    var oldField = duelFieldUI.opponentFieldSpell.GetComponentInChildren<CardDisplay>();
                    if (oldField != null) {
                        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(oldField);
                        SendToGraveyard(oldField.CurrentCardData, false, CardLocation.Field, SendReason.Rule);
                        oldField.transform.SetParent(null);
                        Destroy(oldField.gameObject);
                    }
                }
            }
        }
        else
        {
            // Encontrar Zona de Spell Livre
            targetZone = GetFreeSpellZone(isPlayer);
        }

        if (targetZone == null)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Sem zonas de magia/armadilha livres!");
            return false;
        }

        // 3. Mover Carta
        if (isPlayer) playerHand.Remove(cardGO);
        else opponentHand.Remove(cardGO);

        cardGO.transform.SetParent(targetZone);
        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = fieldCardScale;
        
        if (display != null)
        {
            if (isSet) { cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 0f : 180f); display.ShowBack(); }
            else { cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 0f : 180f); display.ShowFront(); }
        }

        Vector3 endPos = cardGO.transform.position;
        Quaternion endRot = cardGO.transform.rotation;

        // 4. Configuração Visual
        if (display != null)
        {
            display.isInteractable = false;
            display.isOnField = true;
            display.summonedTurnCount = turnCount; // Registra o turno de Set/Ativação

            System.Action onActivationCompleteCallback = () => {
                if (display != null) display.SetVisibility(true);
                if (isSet)
                {
                    if (DuelFXManager.Instance != null)
                    {
                        DuelFXManager.Instance.PlayFlipEffect(display); 
                        DuelFXManager.Instance.PlayPlacementAura(display); 
                    }            
                }
                else
                {
                    bool isTrap = cardData.type.Contains("Trap");
                    if (DuelScoreManager.Instance != null)
                    {
                        if (isTrap) DuelScoreManager.Instance.RecordTrapActivation();
                        else DuelScoreManager.Instance.RecordSpellActivation();
                    }
                    
                    if (TrophyManager.Instance != null)
                    {
                        if (isTrap) TrophyManager.Instance.TrackStat("trap_activated", 1);
                        else TrophyManager.Instance.TrackStat("spell_activated", 1);
                    }

                    System.Action onActivationComplete = () => {
                        if (cardData.property == "Ritual") BeginRitualSummon(display);
                        else if (cardData.name == "Polymerization" || cardData.name.Contains("Fusion")) BeginFusionSummon(display);
                        else if (CardEffectManager.Instance != null) CardEffectManager.Instance.ActivateCard(display, null, null);
                        
                        RefreshAllCardsVisuals();
                    };

                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayCardActivation(display, isTrap, onActivationComplete);
                    else onActivationComplete();
                }
                
                if (isSet) RefreshAllCardsVisuals();
            };
            
            if (sourcePos.HasValue && DuelFXManager.Instance != null && !isSimulating)
            {
                bool isFieldSpellZone = targetZone == duelFieldUI.playerFieldSpell || targetZone == duelFieldUI.opponentFieldSpell;
                CardFlightSettings flightSettings = null;
                if (sourceLoc == CardLocation.Hand) 
                {
                    if (isFieldSpellZone) flightSettings = DuelFXManager.Instance.flightHandToFieldSpellZone;
                    else flightSettings = DuelFXManager.Instance.flightHandToSpellZone; // Mágicas/Armadilhas na mão ativadas ou setadas
                }
                else if (sourceLoc == CardLocation.Banished) flightSettings = DuelFXManager.Instance.flightBanishToField;
                else if (sourceLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToField;
                else flightSettings = DuelFXManager.Instance.flightGraveyardToField; // Fallback
               
                if (flightSettings != null && flightSettings.enableFlight)
                {
                    display.SetVisibility(false);
                GameManager.Instance.pendingVisualTasks++;
                System.Action flightCallback = () => { onActivationCompleteCallback(); GameManager.Instance.pendingVisualTasks--; };
                    bool popFromPile = sourceLoc != CardLocation.Hand && sourceLoc != CardLocation.Field;
                    Quaternion startRot = (sourceLoc == CardLocation.Hand) ? Quaternion.Euler(0, 0, isPlayer ? 0 : 180) : Quaternion.identity;
                    Vector3 sScale = (sourceLoc == CardLocation.Hand) ? handCardScale : fieldCardScale;

                    bool sFaceUp = !isSet;
                    if (sourceLoc == CardLocation.Hand) sFaceUp = isPlayer || showOpponentHand;
                    else if (sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished) sFaceUp = true;
                    else if (sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.ExtraDeck) sFaceUp = false;

                    DuelFXManager.Instance.PlayCardFlight(cardData, cardBackTexture, sFaceUp, !isSet, sourcePos.Value, endPos, 
                        sScale, fieldCardScale, 
                    startRot, endRot, flightSettings, popFromPile, flightCallback);
                }
                else onActivationCompleteCallback();
            }
            else onActivationCompleteCallback();
        }
        
        Debug.Log($"[PlaySpellTrap SUCCESS] {cardName} foi {(isSet ? "setada" : "ativada")} com sucesso. {(isPlayer ? "Jogador" : "Oponente")}");
        return true;
    }
    public void ActivateFieldSpellTrap(GameObject cardGO)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display == null || !display.isOnField) return;

        CardData cardData = display.CurrentCardData;
        bool isPlayer = display.isPlayerCard;

        // Validação LUA antes de revelar a carta
        if (CardEffectManager.Instance != null)
        {
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(display);
            if (lc != null)
            {
                LuaEffect activationEffect = lc.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);
                if (activationEffect != null)
                {
                    int tp = isPlayer ? 0 : 1;
                    if (!CardEffectManager.Instance.CanActivateEffect(lc, activationEffect, tp, null))
                    {
                        if (!isSimulating) {
                            Debug.LogWarning($"[GameManager] {cardData.name} não cumpre os requisitos para ser ativada.");
                            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Não é possível ativar esta carta agora (condições não atendidas ou recém-baixada).");
                        }
                        return;
                    }
                }
            }
        }

        // Vira a carta para cima
        display.ShowFront();

        // Efeito de Ativação
        bool isTrap = cardData.type.Contains("Trap");
        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayCardActivation(display, isTrap);

        if (DuelScoreManager.Instance != null)
        {
            if (isTrap) DuelScoreManager.Instance.RecordTrapActivation();
            else DuelScoreManager.Instance.RecordSpellActivation();
        }
        
        // TROFÉU: Ativação (Field Spell)
        if (TrophyManager.Instance != null)
        {
            if (isTrap) TrophyManager.Instance.TrackStat("trap_activated", 1);
            else TrophyManager.Instance.TrackStat("spell_activated", 1);
        }

        System.Action onActivationComplete = () => {
            // Integração com Interfaces Customizadas e LUA
            if (cardData.property == "Ritual")
            {
                BeginRitualSummon(display);
            }
            else if (cardData.name == "Polymerization" || cardData.name.Contains("Fusion"))
            {
                BeginFusionSummon(display);
            }
            else if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.ActivateCard(display, null, null);
            }
            
            RefreshAllCardsVisuals();
        };

        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayCardActivation(display, isTrap, onActivationComplete);
        else
            onActivationComplete();
    }
    // Verifica se uma carta específica está ativa no campo (Face-up)
    // Usado para: Jinzo, Gravity Bind, Umi, etc.
    public bool IsCardActiveOnField(string cardId)
    {
        if (duelFieldUI == null) return false;
        
        List<string> targetIds = new List<string> { cardId };
        if (cardId == "2015" || cardId == "0013" || cardId == "Umi" || cardId == "1142") {
            targetIds.Clear();
            targetIds.Add("2015"); // Umi
            targetIds.Add("0013"); // A Legendary Ocean
            targetIds.Add("1142"); // Maiden of the Aqua
        }

        bool CheckZone(Transform[] zones)
        {
            foreach (var z in zones)
            {
                if (z.childCount > 0)
                {
                    var c = z.GetComponentInChildren<CardDisplay>();
                    if (c != null && c.isOnField && !c.isFlipped && targetIds.Contains(c.CurrentCardData.id)) return true;
                }
            }
            return false;
        }

        if (CheckZone(duelFieldUI.playerSpellZones)) return true;
        if (CheckZone(duelFieldUI.opponentSpellZones)) return true;
        if (CheckZone(duelFieldUI.playerMonsterZones)) return true; // Jinzo é monstro
        if (CheckZone(duelFieldUI.opponentMonsterZones)) return true;

        // Checa Field Spells
        if (duelFieldUI.playerFieldSpell.childCount > 0)
        {
            var c = duelFieldUI.playerFieldSpell.GetComponentInChildren<CardDisplay>();
            if (c != null && !c.isFlipped && targetIds.Contains(c.CurrentCardData.id)) return true;
        }
        if (duelFieldUI.opponentFieldSpell.childCount > 0)
        {
            var c = duelFieldUI.opponentFieldSpell.GetComponentInChildren<CardDisplay>();
            if (c != null && !c.isFlipped && targetIds.Contains(c.CurrentCardData.id)) return true;
        }

        return false;
    }

    // Troca de Controle (Change of Heart, Snatch Steal)
    public void SwitchControl(CardDisplay card)
    {
        // 0207 - Blindly Loyal Goblin
        if (card.CurrentCardData.id == "0207")
        {
            Debug.Log("Blindly Loyal Goblin: Imune a troca de controle.");
            return;
        }

        bool newOwnerIsPlayer = !card.isPlayerCard;
        Transform newZone = GetFreeMonsterZone(newOwnerIsPlayer);

        if (newZone == null)
        {
            Debug.LogWarning("Sem espaço para trocar o controle do monstro.");
            return;
        }

        // Move o FieldStatUI (se existir) para a nova zona para não deixar um fantasma na mesa antiga
        Transform oldZone = card.transform.parent;
        if (oldZone != null)
        {
            FieldStatUI statUI = oldZone.GetComponentInChildren<FieldStatUI>();
            if (statUI != null)
            {
                statUI.transform.SetParent(newZone, false);
            }
        }

        // Remove da lista da mão se por acaso estiver lá (segurança)
        if (card.isPlayerCard) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        // Ajusta rotação visual baseada na posição de batalha e novo dono
        float targetZRot = 0;
        if (card.position == CardDisplay.BattlePosition.Defense) targetZRot = newOwnerIsPlayer ? 90f : -90f;
        else targetZRot = newOwnerIsPlayer ? 0f : 180f;

        System.Action finalizeControlSwap = () => {
            card.isPlayerCard = newOwnerIsPlayer;
            
            // Reajusta a posição Y para não sobrepor os números com o lado invertido
            if (monsterStatDisplayMode != StatDisplayMode.None && fieldStatDisplayPrefab != null)
            {
                float adjustment = (monsterStatDisplayMode == StatDisplayMode.AboveCard) ? -cardYAdjustmentForStats : cardYAdjustmentForStats;
                if (statDisplayMatchCardRotation && !newOwnerIsPlayer) adjustment = -adjustment; 
                card.transform.localPosition = new Vector3(0, adjustment, 0);
            }

            // Roda efeitos que reagem a troca de controle
            if (CardEffectManager.Instance != null && (card.CurrentCardData.id == "0834" || card.CurrentCardData.id == "0050"))
                CardEffectManager.Instance.ExecuteCardEffect(card);
                
            RefreshAllCardsVisuals();
            RefreshAttackIndicators();
            
            // Garante que o PhaseManager libere os botões de Battle Phase 
            if (PhaseManager.Instance != null) PhaseManager.Instance.UpdateHoverColors(isPlayerTurn);
        };

        if (DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations && !isSimulating)
        {
            DuelFXManager.Instance.PlayControlSwap(card, newZone, targetZRot, newOwnerIsPlayer, finalizeControlSwap);
        }
        else
        {
            card.transform.SetParent(newZone);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.Euler(0, 0, targetZRot);

            finalizeControlSwap();
        }
        Debug.Log($"Controle de {card.CurrentCardData.name} alterado.");

    }

    // Helper para encontrar um CardDisplay no campo pelo ID
    public CardDisplay FindCardOnField(string cardId, bool isPlayer)
    {
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var display = zone.GetComponentInChildren<CardDisplay>();
                if (display != null && display.CurrentCardData.id == cardId)
                {
                    return display;
                }
            }
        }

        zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var display = zone.GetComponentInChildren<CardDisplay>();
                if (display != null && display.CurrentCardData.id == cardId)
                {
                    return display;
                }
            }
        }
        return null;
    }
    public void OnBattlePositionChanged(CardDisplay card)
    {
        // Notifica o sistema de efeitos
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnBattlePositionChanged(card);
        }
        RefreshAttackIndicators();
    }

    // --- FERRAMENTAS DE DESENVOLVEDOR (SUPER MENU) ---

    // Ferramenta de DEV para forçar a carta da mão direto pro campo ignorando regras
    public void Dev_ForceCardToField(CardDisplay card)
    {
        if (card == null || card.isOnField) return;
        
        if (card.CurrentCardData.type.Contains("Monster"))
        {
            // Invoca em Ataque, Face-Up, ignorando tributos e limite de Normal Summon
            FinalizeSummon(card.gameObject, card.CurrentCardData, false, card.isPlayerCard);
        }
        else
        {
            // Mágicas e Armadilhas: Força a "Setar" no campo para ignorar as validações da LUA
            PlaySpellTrap(card.gameObject, card.CurrentCardData, true);
        }
    }
    // --- SISTEMA DE FUSÃO ---

    // Helper para equipar um monstro em outro (Union, Relinquished)
    public void EquipMonsterToMonster(CardDisplay equipCard, CardDisplay targetMonster)
    {
        // 1. Move equipCard para zona de S/T
        Transform targetZone = GetFreeSpellZone(equipCard.isPlayerCard);
        if (targetZone == null)
        {
            Debug.LogWarning("Sem zona de S/T para equipar o monstro.");
            SendToGraveyard(equipCard.CurrentCardData, equipCard.isPlayerCard, CardLocation.Field, SendReason.Rule);
            Destroy(equipCard.gameObject);
            return;
        }

        // Remove o FieldStatUI fantasma da zona antiga (Monstros perdem os status ao virarem magias)
        Transform oldZoneEquip = equipCard.transform.parent;
        if (oldZoneEquip != null)
        {
            FieldStatUI statUI = oldZoneEquip.GetComponentInChildren<FieldStatUI>();
            if (statUI != null) Destroy(statUI.gameObject);
        }

        // Remove da zona de monstro/mão se necessário (SetParent cuida da hierarquia, mas listas precisam de update)
        if (equipCard.isPlayerCard && playerHand.Contains(equipCard.gameObject)) playerHand.Remove(equipCard.gameObject);

        equipCard.transform.SetParent(targetZone);
        equipCard.transform.localPosition = Vector3.zero;
        equipCard.transform.localScale = fieldCardScale;
        equipCard.transform.localRotation = Quaternion.Euler(0, 0, 0); // Face-up
        equipCard.isOnField = true;
        equipCard.isInteractable = false;

        CreateCardLink(equipCard, targetMonster, CardLink.LinkType.Equipment);
        Debug.Log($"{equipCard.CurrentCardData.name} equipado em {targetMonster.CurrentCardData.name}.");
    }

    public void CreateCardLink(CardDisplay source, CardDisplay target, CardLink.LinkType type)
    {
        GameObject linkObj = new GameObject($"Link_{source.name}_{target.name}");
        CardLink link = linkObj.AddComponent<CardLink>();
        link.Initialize(source, target, type);

        if (type == CardLink.LinkType.Equipment)
        {
            // Toca a animação do Fantasma indo em direção ao alvo
            if (DuelFXManager.Instance != null)
                DuelFXManager.Instance.PlayEquipEffect(source, target);
                
            if (CardEffectManager.Instance != null)
                CardEffectManager.Instance.OnCardEquipped(source, target);
        }
    }

    public void BeginFusionSummon(CardDisplay sourceCard)
    {
        bool isPlayer = sourceCard.isPlayerCard;
        var extraDeck = isPlayer ? playerExtraDeck : opponentExtraDeck;
        var fusions = extraDeck.Where(c => c.type.Contains("Fusion")).ToList();

        if (fusions.Count == 0)
        {
            Debug.Log("Nenhum Monstro de Fusão no Extra Deck.");
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
            return;
        }

        List<CardData> possibleMaterials = new List<CardData>();
        possibleMaterials.AddRange(GetPlayerHandData().Where(c => c.type.Contains("Monster")));
        
        Transform[] playerZones = duelFieldUI != null ? duelFieldUI.playerMonsterZones : new Transform[0];
        foreach (var zone in playerZones)
        {
            if (zone != null && zone.childCount > 0)
            {
                var cd = zone.GetComponentInChildren<CardDisplay>();
                if (cd != null) possibleMaterials.Add(cd.CurrentCardData);
            }
        }

        List<CardData> possibleFusions = fusions.Where(f => FusionManager.Instance.CanBeFusionSummoned(f, possibleMaterials)).ToList();

        if (possibleFusions.Count == 0)
        {
            Debug.Log("Materiais insuficientes para qualquer Fusão.");
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule); Destroy(sourceCard.gameObject);
            return;
        }

        if (!isPlayer || isSimulating)
        {
            // A IA de Fusão pura será tratada pela LUA. Por ora, abortamos para não travar a UI de humano.
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
            return;
        }

        // Se a opção estiver ativa, desvia o fluxo para os painéis customizados!
        if (useCustomFusionUI)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowFusionUI(sourceCard);
            return;
        }

        // Passo 1: Selecionar a Fusão PRIMEIRO
        if (possibleFusions.Count == 1 && !alwaysConfirmSingleTarget)
        {
            SelectMaterialsForFusion(sourceCard, possibleFusions[0], possibleMaterials);
        }
        else
        {
            OpenCardMultiSelection(possibleFusions, "Selecione a Fusão do Extra Deck", 1, 1, (fList) => {
                if (fList != null && fList.Count > 0)
                {
                    SelectMaterialsForFusion(sourceCard, fList[0], possibleMaterials);
                }
                else
                {
                    SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                    Destroy(sourceCard.gameObject);
                }
            }, HighlightCategory.Fusion);
        }
    }

    private void SelectMaterialsForFusion(CardDisplay sourceCard, CardData targetFusion, List<CardData> possibleMaterials)
    {
        bool isPlayer = sourceCard.isPlayerCard;
        
        // Filtra os materiais para mostrar APENAS os válidos para a fusão selecionada (incluindo substitutos universais)
        List<CardData> validMaterials = new List<CardData>();
        if (FusionManager.Instance != null)
        {
            List<string> requiredNames = FusionManager.Instance.GetMaterialsForFusion(targetFusion);
            foreach (var mat in possibleMaterials)
            {
                if (requiredNames.Contains(mat.name) || FusionManager.Instance.IsSubstitute(mat))
                {
                    validMaterials.Add(mat);
                }
            }
        }
        else
        {
            validMaterials = possibleMaterials;
        }

        System.Func<List<CardData>, bool> fusionValidator = (selectedMaterials) => {
            return FusionManager.Instance.ValidateFusion(targetFusion, selectedMaterials);
        };

        StartDirectSelection(validMaterials, 2, 5, fusionValidator, $"Selecione os Materiais para {targetFusion.name}", (selectedMaterials) => {
            if (selectedMaterials == null || selectedMaterials.Count < 2 || !fusionValidator(selectedMaterials)) {
                if (UIManager.Instance != null && !isSimulating) UIManager.Instance.ShowMessage("Materiais cancelados ou inválidos!");
                SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                Destroy(sourceCard.gameObject);
                return;
            }

            FusionManager.Instance.PerformFusionSummon(sourceCard, targetFusion, selectedMaterials, false);
        }, HighlightCategory.Fusion);
    }
}
