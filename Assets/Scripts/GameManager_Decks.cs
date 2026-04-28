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
    // Manipulações de Decks
    // ==============================================================================

    public void AddCardToHand(CardData cardData, bool isPlayer, Vector3? customStartPos = null, CardLocation sourceLoc = CardLocation.Deck, bool isFromFieldSpellZone = false, bool? originalOwner = null, bool isDraw = false)
    {
        // Tokens não podem existir na mão. Evaporam.
        if (cardData == null || cardData.id == "TOKEN") return;

        Transform handTransform = isPlayer ? playerHandLayoutGroup : opponentHandLayoutGroup;
        if (cardPrefab == null || handTransform == null) return;

        GameObject newCardGO = Instantiate(cardPrefab, handTransform);
        CardDisplay newCardDisplay = newCardGO.GetComponent<CardDisplay>();
        if (newCardDisplay == null) newCardDisplay = newCardGO.AddComponent<CardDisplay>();

        // FIX: Garante que as cartas compradas animadas também respeitem a rotação natural da mão (180 pro oponente)
        newCardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 0f : 180f);
        newCardGO.transform.localScale = handCardScale;
        newCardDisplay.hoverYOffset = isPlayer ? playerHandHoverYOffset : opponentHandHoverYOffset;
        newCardDisplay.isInteractable = true;
        newCardDisplay.isPlayerCard = isPlayer;
        newCardDisplay.ownerPlayer = originalOwner ?? isPlayer;

        // Adiciona à lista lógica IMEDIATAMENTE para o LayoutGroup calcular a posição
        if (isPlayer) playerHand.Add(newCardGO);
        else opponentHand.Add(newCardGO);

        // Lógica de animação de saque
        if (enableDrawAnimation && !isSimulating && gameObject.activeInHierarchy)
        {
            newCardDisplay.SetCard(cardData, cardBackTexture, false);
            
            Vector3 startPos = customStartPos ?? (isPlayer ? playerDeckDisplay.transform.position : opponentDeckDisplay.transform.position);
            CardFlightSettings settings = null;
            
            if (DuelFXManager.Instance != null)
            {
                if (sourceLoc == CardLocation.Field) settings = isFromFieldSpellZone ? DuelFXManager.Instance.flightFieldSpellZoneToHand : DuelFXManager.Instance.flightFieldToHand;
                else if (sourceLoc == CardLocation.Deck) settings = DuelFXManager.Instance.flightDeckToHand;
                else if (sourceLoc == CardLocation.Graveyard) { settings = DuelFXManager.Instance.flightGraveyardToHand; }
                else if (sourceLoc == CardLocation.Banished) { settings = DuelFXManager.Instance.flightBanishToHand; }
                else { settings = DuelFXManager.Instance.flightDeckToHand; } // Fallback
            }

            StartCoroutine(AnimateCardToHand(newCardGO, isPlayer, startPos, sourceLoc, settings, isDraw));
        }
        else
        {
            // Lógica antiga: Apenas mostra
            if (isPlayer) newCardDisplay.SetCard(cardData, cardBackTexture, true);
            else
            {
                newCardGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
                newCardDisplay.SetCard(cardData, cardBackTexture, showOpponentHand);
            }
        }

        // Notifica adição à mão (Watapon) - Acontece aqui, pois a carta já está logicamente na mão
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardAddedToHand(newCardDisplay);
        
        RefreshAllCardsVisuals();
    }

    public void DiscardCard(CardDisplay card, bool causedByOpponent = false)
    {
        if (card == null) return;

        bool isController = card.isPlayerCard;
        bool isOwner = card.ownerPlayer;

        if (isController) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);
        
        Vector3 startPos = card.transform.position;

        SendToGraveyard(card.CurrentCardData, isOwner, CardLocation.Hand, SendReason.Discarded);

        // Remove modificadores (caso raro de efeito na mão, mas seguro)
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardDiscarded(card, causedByOpponent);

        if (DuelFXManager.Instance != null && DuelFXManager.Instance.flightHandToGraveyard.enableFlight && !isSimulating)
        {
            Vector3 endPos = isOwner ? playerGraveyardDisplay.transform.position : opponentGraveyardDisplay.transform.position;
            Quaternion startRot = Quaternion.Euler(0, 0, isOwner ? 0 : 180f);
            Quaternion endRot = isOwner ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
            DuelFXManager.Instance.PlayCardFlight(card.CurrentCardData, cardBackTexture, true, true, startPos, endPos, handCardScale, fieldCardScale, startRot, endRot, DuelFXManager.Instance.flightHandToGraveyard, false, null);
        }

        Destroy(card.gameObject);
    }

    public void CleanupDuelState()
    {
        // Destrói objetos visuais das mãos
        foreach (GameObject card in playerHand) if (card != null) Destroy(card);
        playerHand.Clear();

        foreach (GameObject card in opponentHand) if (card != null) Destroy(card);
        opponentHand.Clear();

        // FASE DE LIMPEZA DO CAMPO (Visual e Físico para Simulações)
        if (duelFieldUI != null)
        {
            System.Action<Transform[]> ClearZones = (zones) => {
                if (zones == null) return;
                foreach (var z in zones) {
                    foreach (Transform child in z) Destroy(child.gameObject);
                }
            };
            ClearZones(duelFieldUI.playerMonsterZones);
            ClearZones(duelFieldUI.opponentMonsterZones);
            ClearZones(duelFieldUI.playerSpellZones);
            ClearZones(duelFieldUI.opponentSpellZones);
            if (duelFieldUI.playerFieldSpell != null) foreach (Transform child in duelFieldUI.playerFieldSpell) Destroy(child.gameObject);
            if (duelFieldUI.opponentFieldSpell != null) foreach (Transform child in duelFieldUI.opponentFieldSpell) Destroy(child.gameObject);
        }

        // Destrói vínculos remanescentes (Equipamentos)
        CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links) Destroy(link.gameObject);

        // Desbloqueia as zonas presas por efeitos (ex: Ojama)
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.blockedZonesByCard.Clear();

        // Limpa listas de dados
        playerGraveyard.Clear();
        opponentGraveyard.Clear();
        playerExtraDeck.Clear();
        playerSideDeck.Clear();
        opponentExtraDeck.Clear();
        playerRemoved.Clear();
        opponentRemoved.Clear();
        forbiddenSpells.Clear();

        // Atualiza visuais das pilhas e limpa o viewer
        UpdatePileVisuals();
        ClearCardViewer();
    }

    List<CardData> InitializePlayerDeck()
    {
        // DEV MODE: Sobrescreve o deck do jogador com o de um personagem
        if (devMode && !string.IsNullOrEmpty(testPlayerID) && characterDatabase != null)
        {
            CharacterData charData = characterDatabase.GetCharacterById(testPlayerID);
            if (charData != null)
            {
                Debug.Log($"[DevMode] Jogando como: {charData.name} ({charData.id})");
                List<CardData> devDeck = new List<CardData>();
                
                // --- SELEÇÃO DE DECK ALEATÓRIA (A, B, C) para o Jogador ---
                List<string> selectedMain = charData.deck_A;
                List<string> selectedExtra = charData.extra_deck_A;
                string variant = "A";

                int rng = Random.Range(0, 3);
                if (rng == 1 && charData.deck_B != null && charData.deck_B.Count > 0) { selectedMain = charData.deck_B; selectedExtra = charData.extra_deck_B; variant = "B"; }
                else if (rng == 2 && charData.deck_C != null && charData.deck_C.Count > 0) { selectedMain = charData.deck_C; selectedExtra = charData.extra_deck_C; variant = "C"; }

                Debug.Log($"[DevMode] Usando Deck Variante do Jogador: {variant}");

                if (selectedMain != null) foreach (string id in selectedMain) { CardData c = cardDatabase.GetCardById(id); if (c != null) devDeck.Add(c); }
                
                playerExtraDeck.Clear();
                if (selectedExtra != null) foreach (string id in selectedExtra) { CardData c = cardDatabase.GetCardById(id); if (c != null) playerExtraDeck.Add(c); }

                playerMainDeck = devDeck;
                
                // FIX: Adiciona as cartas do dev deck ao Trunk para que o Deck Builder não fique vazio e não crashe
                playerTrunk.Clear();
                foreach (var c in devDeck) playerTrunk.Add(c.id);
                foreach (var c in playerExtraDeck) playerTrunk.Add(c.id);
                
                // Atualiza nome do jogador na UI
                playerName = charData.name;
                if (duelFieldUI != null && duelFieldUI.playerNameText != null)
                    duelFieldUI.playerNameText.text = playerName;

                UpdatePileVisuals();
                return playerMainDeck;
            }
            else
            {
                Debug.LogWarning($"[DevMode] Personagem {testPlayerID} não encontrado. Usando deck do save.");
            }
        }

        // Se já temos um deck carregado (do Save ou anterior), usamos ele
        if (playerMainDeck.Count > 0)
        {
            UpdatePileVisuals();
            return playerMainDeck;
        }

        List<CardData> newDeck = new List<CardData>();
        // Verifica se já existe um deck salvo no SaveLoadSystem
        // Se não, gera um novo
        if (InitialDeckBuilder.Instance != null)
        {
            Debug.Log("Gerando Deck Inicial Único...");
            List<CardData> generatedDeck = InitialDeckBuilder.Instance.GenerateInitialDeck();

            playerExtraDeck.Clear(); // Deck inicial não tem fusões prontas

            foreach (var card in generatedDeck)
            {
                newDeck.Add(card);
                // Adiciona ao Trunk também para que o jogador "possua" a carta
                if (!playerTrunk.Contains(card.id))
                {
                    playerTrunk.Add(card.id);
                }

                // FIX: Cartas do deck inicial já começam "usadas" (sem tag NEW na biblioteca)
                if (SaveLoadSystem.Instance != null)
                {
                    SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
                }
            }
        }
        else
        {
            // Fallback antigo (carrega tudo ou aleatório simples)
            Debug.LogWarning("InitialDeckBuilder não encontrado. Usando fallback.");
        }

        // Salva o jogo imediatamente para persistir o deck gerado
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame(currentSaveID);
        }

        playerMainDeck = newDeck;
        UpdatePileVisuals();
        return newDeck;
    }

    (List<CardData>, List<CardData>) InitializeOpponentDeck()
    {
        List<CardData> newDeck = new List<CardData>();

        // DEV MODE: Se um ID de teste estiver definido, limpa o oponente atual para forçar o carregamento correto
        if (devMode && !string.IsNullOrEmpty(testOpponentID))
        {
            currentOpponent = null;
        }

        // Validação de segurança: Se o objeto oponente existe mas está vazio/inválido (sem ID), reseta
        if (currentOpponent != null && string.IsNullOrEmpty(currentOpponent.id))
        {
            // A engine limpa o oponente fantasma silenciosamente sem poluir o console do desenvolvedor.
            currentOpponent = null;
        }

        // Se o oponente ainda for nulo aqui (não foi setado no StartDuel), tenta o ID de teste novamente como última tentativa
        if (currentOpponent == null && !string.IsNullOrEmpty(testOpponentID) && characterDatabase != null)
        {
            currentOpponent = characterDatabase.GetCharacterById(testOpponentID);
        }

        // NOVO: Se ainda for nulo (Free Duel sem ID definido), escolhe um aleatório da campanha
        if (currentOpponent == null && campaignDatabase != null && campaignDatabase.acts.Count > 0)
        {
            var allIds = campaignDatabase.acts.SelectMany(a => a.opponentIDs).ToList();
            if (allIds.Count > 0)
            {
                string randomId = allIds[Random.Range(0, allIds.Count)];
                currentOpponent = characterDatabase.GetCharacterById(randomId);
                Debug.Log($"[GameManager] Oponente aleatório selecionado: {currentOpponent?.name}");
            }
        }
        else if (currentOpponent == null && campaignDatabase == null)
        {
            Debug.LogWarning("[GameManager] CampaignDatabase não atribuído! Não foi possível selecionar um oponente aleatório.");
        }

        // Se temos um oponente definido (Campanha), carregamos o deck dele
        if (currentOpponent != null)
        {
            // Garante referência da UI antes de usar e atualiza o nome
            if (duelFieldUI == null) duelFieldUI = FindFirstObjectByType<DuelFieldUI>();
            if (duelFieldUI != null && duelFieldUI.opponentNameText != null)
                duelFieldUI.opponentNameText.text = currentOpponent.name;

            // --- SELEÇÃO DE DECK ALEATÓRIA (A, B, C) ---
            List<string> selectedMain = currentOpponent.deck_A;
            List<string> selectedExtra = currentOpponent.extra_deck_A;
            string variant = "A";

            int rng = Random.Range(0, 3);
            if (testOpponentDeckVariant == 1) rng = 0;
            else if (testOpponentDeckVariant == 2) rng = 1;
            else if (testOpponentDeckVariant == 3) rng = 2;

            if (rng == 1 && currentOpponent.deck_B != null && currentOpponent.deck_B.Count > 0)
            {
                selectedMain = currentOpponent.deck_B;
                selectedExtra = currentOpponent.extra_deck_B;
                variant = "B";
            }
            else if (rng == 2 && currentOpponent.deck_C != null && currentOpponent.deck_C.Count > 0)
            {
                selectedMain = currentOpponent.deck_C;
                selectedExtra = currentOpponent.extra_deck_C;
                variant = "C";
            }

            Debug.Log($"[GameManager] Inicializando oponente: {currentOpponent.name} | Tentando Deck Variante: {variant}");

            // Carrega Main Deck
            if (selectedMain != null && selectedMain.Count > 0) 
            {
                foreach (string id in selectedMain) { 
                    CardData c = cardDatabase.GetCardById(id); 
                    if (c != null) {
                        // Pente-Fino: Se a carta for proibida e o jogo não permitir trapaça da IA, ela é removida e a de respaldo assume seu lugar!
                        if (!allowForbiddenCards && !disableBanlist && c.goat_banlist == "Banned") continue;
                        newDeck.Add(c); 
                    } else Debug.LogWarning($"[GameManager] Carta ID '{id}' não encontrada no DB para o oponente {currentOpponent.name}."); 
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] A lista do Deck {variant} do oponente {currentOpponent.name} está vazia ou nula.");
            }

            // --- FALLBACK DE VARIANTE ---
            // Se a variante escolhida (B ou C) resultou em deck vazio, tenta o Deck A
            if (newDeck.Count == 0 && variant != "A")
            {
                Debug.LogWarning($"[GameManager] Deck {variant} do oponente resultou em 0 cartas válidas. Tentando Deck A como fallback.");
                selectedMain = currentOpponent.deck_A;
                selectedExtra = currentOpponent.extra_deck_A;
                
                if (selectedMain != null)
                {
                    foreach (string id in selectedMain)
                    {
                        CardData c = cardDatabase.GetCardById(id);
                        if (c != null) newDeck.Add(c);
                    }
                }
            }

            // Carrega Extra Deck
            opponentExtraDeck.Clear();
            if (selectedExtra != null) foreach (string id in selectedExtra) { 
                CardData c = cardDatabase.GetCardById(id); 
                if (c != null) {
                    if (!allowForbiddenCards && !disableBanlist && c.goat_banlist == "Banned") continue;
                    opponentExtraDeck.Add(c); 
                } 
            }
        }
        else
        {
            // Fallback: Se não tiver oponente (Free Duel genérico), carrega tudo ou aleatório
            if (duelFieldUI != null && duelFieldUI.opponentNameText != null)
                duelFieldUI.opponentNameText.text = "Training Opponent";

            Debug.Log("Nenhum oponente específico definido. Carregando banco de dados inteiro (Modo Teste).");
            foreach (var card in cardDatabase.cardDatabase)
            {
                if (card.type.Contains("Fusion"))
                {
                    // Fallback should also respect extra_deck
                }
                else
                {
                    newDeck.Add(card);
                }
            }
        }

        // SAFEGUARD CRÍTICO: Se o deck ainda estiver vazio (falha no load ou IDs errados), gera um aleatório
        if (newDeck.Count == 0)
        {
            Debug.LogError("[GameManager] CRÍTICO: Deck do oponente vazio após todas as tentativas! Gerando deck de emergência aleatório.");
            List<CardData> allCards = new List<CardData>(cardDatabase.cardDatabase);
            var validCards = allCards.Where(c => !c.type.Contains("Fusion") && !c.type.Contains("Synchro") && !c.type.Contains("Xyz") && !c.type.Contains("Token")).ToList();

            for (int i = 0; i < 40; i++)
            {
                if (validCards.Count == 0) break;
                CardData randomCard = validCards[Random.Range(0, validCards.Count)];
                newDeck.Add(randomCard);
            }
        }

                // Safeguard para garantir que apenas fusões estejam no Extra Deck do oponente
        var oppNonFusionsInExtra = opponentExtraDeck.Where(c => !c.type.Contains("Fusion")).ToList();
        if (oppNonFusionsInExtra.Any())
        {
            // Debug.LogWarning($"Safeguard (Oponente): {oppNonFusionsInExtra.Count} carta(s) não-fusão encontradas no Extra Deck. Movendo para o Deck Principal.");
            newDeck.AddRange(oppNonFusionsInExtra);
            opponentExtraDeck.RemoveAll(c => !c.type.Contains("Fusion"));
        }
        var oppFusionsInMain = newDeck.Where(c => c.type.Contains("Fusion")).ToList();
        if (oppFusionsInMain.Any())
        {
            // Debug.LogWarning($"Safeguard (Oponente): {oppFusionsInMain.Count} carta(s) de fusão encontradas no Deck Principal. Movendo para o Extra Deck.");
            opponentExtraDeck.AddRange(oppFusionsInMain);
            newDeck.RemoveAll(c => c.type.Contains("Fusion"));
        }
        
        // Shuffle logic moved to DeckManager, but we shuffle the list here before passing
        // Actually DeckManager.SetupDecks doesn't shuffle, so we can shuffle here or call ShuffleDeck later.
        // Let's shuffle the list here.
        for (int i = 0; i < newDeck.Count; i++) { CardData temp = newDeck[i]; int r = Random.Range(i, newDeck.Count); newDeck[i] = newDeck[r]; newDeck[r] = temp; }
        
        opponentMainDeck = newDeck;
        Debug.Log($"[GameManager] Deck do oponente finalizado com {newDeck.Count} cartas (Extra: {opponentExtraDeck.Count}).");
        UpdatePileVisuals();
        return (opponentMainDeck, opponentExtraDeck);
    }

        public IEnumerator DrawInitialHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard(true); // Ignora o limite para a mão inicial
            yield return new WaitForSeconds(playerDrawSpeed); // Delay configurável
        }
    }
    public IEnumerator DrawInitialOpponentHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
           DrawOpponentCard();            
           yield return new WaitForSeconds(opponentDrawSpeed); // Delay configurável
        }
    }    

public void ShuffleDeck(bool isPlayer)
{
    if (DeckManager.Instance != null) DeckManager.Instance.ShuffleDeck(isPlayer);
}
    public void ShuffleOpponentDeck()
    {
        if (DeckManager.Instance != null) DeckManager.Instance.ShuffleDeck(false);
    }

    public void ShuffleHand(bool isPlayer)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        if (hand == null || hand.Count <= 1) return;

        // 1. Salva as posições físicas originais antes de bagunçar a lista
        List<Vector3> originalPositions = new List<Vector3>();
        for (int i = 0; i < hand.Count; i++) originalPositions.Add(hand[i].transform.position);

        // 2. Embaralha a lista lógica (Matemática: Fisher-Yates)
        for (int i = 0; i < hand.Count; i++)
        {
            GameObject temp = hand[i];
            int randomIndex = Random.Range(i, hand.Count);
            hand[i] = hand[randomIndex];
            hand[randomIndex] = temp;
        }

        // 3. Executa a Animação Visual ou Atualização Direta
        bool useAnim = false;
        if (DuelFXManager.Instance != null)
        {
            useAnim = isPlayer ? DuelFXManager.Instance.useHandShuffleAnimation : DuelFXManager.Instance.useOpponentHandShuffleAnimation;
        }

        if (useAnim && !isSimulating)
        {
            StartCoroutine(HandShuffleRoutine(isPlayer, hand, originalPositions));
        }
        else
        {
            // Se não tiver animação, apenas ajusta os índices na hora
            for (int i = 0; i < hand.Count; i++) hand[i].transform.SetSiblingIndex(i);
            
            if (!isPlayer && !showOpponentHand)
            {
                foreach (var go in hand) {
                    var cd = go.GetComponent<CardDisplay>();
                    if (cd != null) cd.ShowBack(false); // sem animação
                }
            }
        }
    }

    private IEnumerator HandShuffleRoutine(bool isPlayer, List<GameObject> hand, List<Vector3> targetPositions)
    {
        Transform layoutTransform = isPlayer ? playerHandLayoutGroup : opponentHandLayoutGroup;
        HorizontalLayoutGroup layoutGroup = layoutTransform.GetComponent<HorizontalLayoutGroup>();
        
        // Desliga o LayoutGroup para que as cartas fiquem livres para voar
        if (layoutGroup != null) layoutGroup.enabled = false;

        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.shuffleSound);

        int cycles = 1;
        float cycleDuration = 0.3f;
        if (DuelFXManager.Instance != null)
        {
            cycles = Random.Range(Mathf.Max(1, DuelFXManager.Instance.handShuffleMinCycles), Mathf.Max(1, DuelFXManager.Instance.handShuffleMaxCycles) + 1);
            cycleDuration = DuelFXManager.Instance.handShuffleDuration;
        }

        for (int cycle = 0; cycle < cycles; cycle++)
        {
            List<Vector3> startPositions = new List<Vector3>();
            foreach (var card in hand) startPositions.Add(card.transform.position);

            List<Vector3> currentTargetPositions = new List<Vector3>(targetPositions);
            if (cycle < cycles - 1)
            {
                // Embaralha as posições alvo temporárias para criar cruzamentos intermediários caóticos
                for (int i = 0; i < currentTargetPositions.Count; i++)
                {
                    Vector3 temp = currentTargetPositions[i];
                    int randomIndex = Random.Range(i, currentTargetPositions.Count);
                    currentTargetPositions[i] = currentTargetPositions[randomIndex];
                    currentTargetPositions[randomIndex] = temp;
                }
            }

            float elapsed = 0f;

            if (DuelFXManager.Instance != null && DuelFXManager.Instance.handShuffleType == HandShuffleType.CollapseAndFan)
            {
                Vector3 centerPos = layoutTransform.position;
                float halfDuration = cycleDuration / 2f;
                
                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, elapsed / halfDuration);
                    for (int i = 0; i < hand.Count; i++) { if (hand[i] != null) hand[i].transform.position = Vector3.Lerp(startPositions[i], centerPos, t); }
                    yield return null;
                }
                
                if (cycle > 0 && DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.shuffleSound);
                
                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, elapsed / halfDuration);
                    for (int i = 0; i < hand.Count; i++) { if (hand[i] != null) hand[i].transform.position = Vector3.Lerp(centerPos, currentTargetPositions[i], t); }
                    yield return null;
                }
            }
            else
            {
                if (cycle > 0 && DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.shuffleSound);
                
                while (elapsed < cycleDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, elapsed / cycleDuration);
                    for (int i = 0; i < hand.Count; i++)
                    {
                        if (hand[i] == null) continue;
                        Vector3 start = startPositions[i]; Vector3 end = currentTargetPositions[i];
                        Vector3 currentPos = Vector3.Lerp(start, end, t);
                        
                        // A Parábola Cross-Swap
                        float dirX = (end.x > start.x) ? 1f : ((end.x < start.x) ? -1f : 0f);
                        float yOffset = Mathf.Sin(t * Mathf.PI) * 30f * dirX; 
                        if (!isPlayer) yOffset = -yOffset; // Inverte o salto Y para a mão do oponente (que está de ponta-cabeça no teto)
                        currentPos.y += yOffset; 
                        hand[i].transform.position = currentPos;
                    }
                    yield return null;
                }
            }
        }
        // Restaura o controle absoluto para o LayoutGroup
        for (int i = 0; i < hand.Count; i++) { if (hand[i] != null) hand[i].transform.SetSiblingIndex(i); }
        if (layoutGroup != null) { layoutGroup.enabled = true; LayoutRebuilder.ForceRebuildLayoutImmediate(layoutTransform as RectTransform); }
    }

    public void MillCards(bool isPlayer, int amount)
    {
        if (DeckManager.Instance != null) DeckManager.Instance.MillCards(isPlayer, amount);
    }
    public void DrawCard(bool ignoreLimit = false)
    {
        if (DeckManager.Instance != null && DeckManager.Instance.GetPlayerDeck() != null && DeckManager.Instance.GetPlayerDeck().Count > 0)
        {
            DeckManager.Instance.DrawCard(true, ignoreLimit);
        }
        // Retiramos o avanço de fase redundante daqui, pois o DeckManager já cuida disso.
    }

    public void DrawOpponentCard()
    {
        if (DeckManager.Instance != null && DeckManager.Instance.GetOpponentDeck() != null && DeckManager.Instance.GetOpponentDeck().Count > 0)
        {
            DeckManager.Instance.DrawCard(false);
        }
    }

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard(true); // Ignora o limite para a mão inicial
        }
    }

    // === CONSULTAS DE ESPAÇO LIVRE ===
    /// <summary>
    /// Retorna quantidade de zonas de monstro livres para invocação
    /// </summary>
    public int GetFreeMonsterZones(bool isPlayer)
    {
        if (duelFieldUI == null) return 0;
        
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        int freeCount = 0;
        
        foreach (var zone in zones)
        {
            if (zone != null && zone.childCount == 0)
                freeCount++;
        }
        
        return freeCount;
    }

    /// <summary>
    /// Retorna quantidade de zonas de S/T livres para setar
    /// </summary>
    public int GetFreeSpellTrapZones(bool isPlayer)
    {
        if (duelFieldUI == null) return 0;
        
        Transform[] zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        int freeCount = 0;
        
        foreach (var zone in zones)
        {
            if (zone != null && zone.childCount == 0)
                freeCount++;
        }
        
        return freeCount;
    }

    public void ViewGraveyard(bool isPlayer)
    {
        CancelAttackTargeting();

        if (UIManager.Instance == null) return;

        List<CardData> graveyard = isPlayer ? playerGraveyard : opponentGraveyard;
        // Opcional: não mostrar cemitério vazio
        if (graveyard.Count == 0) return;

        UIManager.Instance.ShowGraveyard(graveyard, cardBackTexture);
    }

    public void ViewExtraDeck(bool isPlayer)
    {
        CancelAttackTargeting();

        if (UIManager.Instance == null) return;

        // Nota: Normalmente só se pode ver o próprio Extra Deck, a menos que um efeito permita
        bool qaActive = fullTestMode || !string.IsNullOrEmpty(QAAutoSpawner.currentTestCardId);
        if (!isPlayer && !devMode && !qaActive) return;

        List<CardData> extraDeck = isPlayer ? playerExtraDeck : opponentExtraDeck;
        UIManager.Instance.ShowExtraDeck(extraDeck, cardBackTexture);
    }

    public void ViewRemovedCards(bool isPlayer)
    {
        CancelAttackTargeting();

        if (UIManager.Instance == null) return;

        List<CardData> removed = isPlayer ? playerRemoved : opponentRemoved;
        // Reusa o visualizador de cemitério ou um específico se criado
        UIManager.Instance.ShowRemovedCards(removed, cardBackTexture);
    }

    public void DrawInitialOpponentHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawOpponentCard();
        }
    }

    // Atualiza todos os visuais de pilhas (Decks e Cemitérios)
    void UpdatePileVisuals()
    {
        if (DeckManager.Instance != null) DeckManager.Instance.UpdateDeckVisuals();
        if (playerGraveyardDisplay != null) playerGraveyardDisplay.UpdatePile(playerGraveyard, cardBackTexture);
        if (opponentGraveyardDisplay != null) opponentGraveyardDisplay.UpdatePile(opponentGraveyard, cardBackTexture);
        if (playerExtraDeckDisplay != null) playerExtraDeckDisplay.UpdatePile(playerExtraDeck, cardBackTexture);
        if (opponentExtraDeckDisplay != null) opponentExtraDeckDisplay.UpdatePile(opponentExtraDeck, cardBackTexture);
        if (playerRemovedDisplay != null) playerRemovedDisplay.UpdatePile(playerRemoved, cardBackTexture);
        if (opponentRemovedDisplay != null) opponentRemovedDisplay.UpdatePile(opponentRemoved, cardBackTexture);
    }

    IEnumerator LoadCardBackTexture()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, "DMCardImages/0000 - Background.jpg");
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        // Removemos o 'using' para evitar o leak do alocador temporário
        backTextureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return backTextureRequest.SendWebRequest();

        if (backTextureRequest.result == UnityWebRequest.Result.Success)
        {
            cardBackTexture = DownloadHandlerTexture.GetContent(backTextureRequest);
            cardBackTexture.filterMode = FilterMode.Trilinear;
            UpdatePileVisuals(); // Atualiza o visual dos decks caso o duelo já tenha começado
        }
        backTextureRequest.Dispose();
        backTextureRequest = null;
    }

    IEnumerator LoadCardTexture(string imagePath)
    {
        // Este método não é mais usado diretamente pelo GameManager para exibir cartas individuais.
        // A responsabilidade de carregar a textura da frente é do CardDisplay.
        yield break;
    }

    public void ShowNextCard()
    {
        // Lógica de navegação de cartas removida do GameManager,
        // pois agora ele gerencia o deck e mão, não um visualizador de índice.
        // Se quiser um visualizador de deck, ele precisaria de sua própria lógica.
        Debug.LogWarning("Função ShowNextCard não implementada no GameManager. Use DrawCard para comprar cartas.");
    }

    public void ShowPreviousCard()
    {
        Debug.LogWarning("Função ShowPreviousCard não implementada no GameManager. Use DrawCard para comprar cartas.");
    }
    
    // --- GERENCIAMENTO DE DECK (ACESSO PÚBLICO) ---

    public List<CardData> GetPlayerSideDeck() { return playerSideDeck; }

    public bool PlayerHasCard(string cardId)
    {
        // Verifica se o jogador tem a carta no baú (ou se está usando todas as cartas)
        if (devMode) return true;
        return playerTrunk.Contains(cardId);
    }

    // --- SISTEMAS DE JOGO AVANÇADOS ---

    // Ferramenta de Dev: Visualizar Deck
    public void ViewDeck(bool isPlayer)
    {
        CancelAttackTargeting();

        if (UIManager.Instance == null) return;

        bool qaActive = fullTestMode || !string.IsNullOrEmpty(QAAutoSpawner.currentTestCardId);
        if (!isPlayer && !devMode && !qaActive) return;

        List<CardData> deck = isPlayer ? GetPlayerMainDeck() : GetOpponentMainDeck();
        
        Debug.Log($"<color=cyan>[Deck Viewer]</color> Abrindo Deck do {(isPlayer ? "Jogador" : "Oponente")}. Total de Cartas na lista lógica: {deck?.Count ?? 0}");
        
        UIManager.Instance.ShowDeck(deck, cardBackTexture);
    }

    // Helper para o EffectTestManager e outros sistemas
    public Texture2D GetCardBackTexture()
    {
        return cardBackTexture;
    }

    // --- MÉTODOS ADICIONADOS PARA CORRIGIR ERROS DE COMPILAÇÃO ---

    public List<CardData> GetPlayerMainDeck()
    {
        // Se estamos num duelo (DeckManager tem cartas), retorna o deck do duelo
        if (DeckManager.Instance != null && DeckManager.Instance.GetPlayerDeck() != null && DeckManager.Instance.GetPlayerDeck().Count > 0)
        {
            return DeckManager.Instance.GetPlayerDeck();
        }
        // Senão, retorna o deck persistente (para DeckBuilder, SaveSystem, etc)
        return playerMainDeck;
    }

    public List<CardData> GetOpponentMainDeck()
    {
        if (DeckManager.Instance != null && DeckManager.Instance.GetOpponentDeck() != null && DeckManager.Instance.GetOpponentDeck().Count > 0)
        {
            return DeckManager.Instance.GetOpponentDeck();
        }
        return opponentMainDeck;
    }
    public List<CardData> GetPlayerExtraDeck() { return playerExtraDeck; }
    public List<CardData> GetOpponentExtraDeck() { return opponentExtraDeck; }
    public List<CardData> GetPlayerGraveyard() { return playerGraveyard; }
    public List<CardData> GetOpponentGraveyard() { return opponentGraveyard; }
    public List<CardData> GetPlayerRemoved() { return playerRemoved; }
    public List<CardData> GetOpponentRemoved() { return opponentRemoved; }
    public int GetPlayerRemovedCount() { return playerRemoved.Count; }

    public List<CardData> GetPlayerHandData()
    {
        List<CardData> data = new List<CardData>();
        foreach (var go in playerHand)
        {
            if (go != null)
            {
                var display = go.GetComponent<CardDisplay>();
                if (display != null && display.CurrentCardData != null)
                    data.Add(display.CurrentCardData);
            }
        }
        return data;
    }

    public List<CardData> GetOpponentHandData()
    {
        List<CardData> data = new List<CardData>();
        foreach (var go in opponentHand)
        {
            if (go != null)
            {
                var display = go.GetComponent<CardDisplay>();
                if (display != null && display.CurrentCardData != null)
                    data.Add(display.CurrentCardData);
            }
        }
        return data;
    }

    public void SetPlayerDeck(List<CardData> main, List<CardData> side, List<CardData> extra)
    {
        playerMainDeck = new List<CardData>(main);
        playerSideDeck = new List<CardData>(side);
        playerExtraDeck = new List<CardData>(extra);
    }
}