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
    // AÇÕES DE MUDANÇAS DE PHASES
    // ==============================================================================

    // Novo método chamado pelo botão "Free Duel"
    public void StartDuel()
    {
        // Limpa o estado anterior (destrói cartas visuais, limpa listas)
        CleanupDuelState();

        // FIX: Garante que a referência da UI do campo esteja atualizada antes de qualquer coisa
        if (duelFieldUI == null) duelFieldUI = FindFirstObjectByType<DuelFieldUI>();
        if (duelFieldUI == null) Debug.LogError("GameManager: DuelFieldUI não encontrado na cena! O placar não funcionará.");

        // Garante que os gerenciadores essenciais existam na cena
        EnsureCoreManagers();

        // NOVA LÓGICA: Desabilita cliques na mão até o jogo estar pronto
        if (playerHandLayoutGroup != null && playerHandCanvasGroup == null)
            playerHandCanvasGroup = playerHandLayoutGroup.GetComponent<CanvasGroup>();
        if (playerHandLayoutGroup != null && playerHandCanvasGroup == null)
            playerHandCanvasGroup = playerHandLayoutGroup.gameObject.AddComponent<CanvasGroup>();
        
        if (playerHandCanvasGroup != null)
        {
            playerHandCanvasGroup.interactable = false;
        }

        List<CardData> pDeck = InitializePlayerDeck();
        (List<CardData> oMain, List<CardData> oExtra) = InitializeOpponentDeck();
        DeckManager.Instance.SetupDecks(pDeck, playerExtraDeck, oMain, oExtra);

        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.PreloadScriptsForDecks(pDeck, playerExtraDeck, oMain, oExtra);
        }

        // Inicializa LP
        playerLP = 8000;
        opponentLP = 8000;
        turnCount = 0;
        isDuelOver = false;

        // Define quem começa
        if (forcePlayerGoingFirst) isPlayerTurn = true;
        else if (devMode) isPlayerTurn = true; // Padrão Dev
        else isPlayerTurn = (Random.value > 0.5f); // Aleatório real

        UpdateLPUI();

        // Reseta flag de draw antes de começar o turno
        if (DeckManager.Instance != null) DeckManager.Instance.ResetTurnStats();

        // PhaseManager.Instance.StartTurn() agora é chamado dentro de DuelStartSequence

        // Inicia o rastreamento de pontuação para o Rank
        if (DuelScoreManager.Instance != null)
        {
            DuelScoreManager.Instance.StartDuelTracking();
        }

        // Aplica o tema visual (se estivermos em um duelo de campanha ou tivermos um índice válido)
        if (campaignDatabase != null && DuelThemeManager.Instance != null)
        {
            // Se currentDuelIndex for -1 (Free Duel), tenta usar o tema do Ato 1 ou um padrão
            int levelToUse = (currentDuelIndex > 0) ? currentDuelIndex : 1;

            // DEV MODE: Sobrescreve o tema se um Ato de teste for definido
            if (devMode && testActThemeIndex > 0)
            {
                levelToUse = (testActThemeIndex - 1) * 10 + 1; // Ato 1 = Nível 1, Ato 2 = Nível 11...
            }

            DuelTheme theme = campaignDatabase.GetThemeForLevel(levelToUse);
            // Fallback Robusto: Só aceita o tema da campanha se ele tiver pelo menos o tabuleiro preenchido!
            if (theme != null && theme.boardBackground != null)
                DuelThemeManager.Instance.ApplyTheme(theme);
            else if (DuelThemeManager.Instance.defaultTheme != null)
                DuelThemeManager.Instance.ApplyTheme(DuelThemeManager.Instance.defaultTheme);
        }
        else if (DuelThemeManager.Instance != null && DuelThemeManager.Instance.defaultTheme != null)
        {
            DuelThemeManager.Instance.ApplyTheme(DuelThemeManager.Instance.defaultTheme);
            }

                StartCoroutine(DuelStartSequence());    
        }
    private IEnumerator DuelStartSequence()
    {
        yield return new WaitForSeconds(0.5f); // Delay inicial para a cena abrir e a UI estabilizar

        // 1. ANÚNCIO DE INÍCIO E PAUSA
        AnnounceText(phaseAnnouncements.textStartDuel);
        yield return new WaitForSeconds(1.5f);

        // Embaralha os decks com animação visual ANTES de comprar as cartas
        if (!disableDeckShuffle && DeckManager.Instance != null)
        {
            DeckManager.Instance.ShuffleDeck(true);
            DeckManager.Instance.ShuffleDeck(false);
            
            // Agora ele respeita rigorosamente o tempo total do SpriteSheet + Delays que você configurou no Inspector
            float waitTime = 0.8f;
            if (DuelFXManager.Instance != null)
            {
                waitTime = DuelFXManager.Instance.GetShuffleTotalDuration();
            }
            yield return new WaitForSeconds(waitTime);
        }

        // COMPRA AS CARTAS INICIAIS AQUI (Completando o Setup ANTES do turno começar)
        yield return StartCoroutine(DrawInitialHandRoutine(5));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(DrawInitialOpponentHandRoutine(5));
        yield return new WaitForSeconds(0.5f); 

        // 2. ANÚNCIO DO TURNO
        AnnounceText(isPlayerTurn ? phaseAnnouncements.textPlayerTurn : phaseAnnouncements.textOpponentTurn);
        // Espera o texto do turno sumir perfeitamente para emendar na Draw Phase
        yield return new WaitForSeconds((phaseAnnouncements.displayDuration + phaseAnnouncements.fadeDuration) * phaseAnnouncements.masterDelayMultiplier);

        if (PhaseManager.Instance != null) PhaseManager.Instance.StartTurn();
        else Debug.LogError("PhaseManager não encontrado mesmo após tentativa de criação!");

        // START AI TURN SE A IA COMEÇAR:
        if (!isPlayerTurn && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            OpponentAI.Instance.StartAITurn(!isSimulating);
        }
    }

    // Sobrecarga para iniciar duelo contra personagem específico (Campanha)
    public void StartDuel(CharacterData opponent, int duelIndex = -1)
    {
        currentOpponent = opponent;
        currentDuelIndex = duelIndex;
        StartDuel(); // Chama o método principal
    }

    // Chamado para trocar o turno (ex: botão End Phase ou automático)
    public void SwitchTurn()
    {
        isPlayerTurn = !isPlayerTurn;

        if (isPlayerTurn) {
            lpPaidLastTurnPlayer = lpPaidThisTurnPlayer;
            lpPaidThisTurnPlayer = 0;
        } else {
            lpPaidLastTurnOpponent = lpPaidThisTurnOpponent;
            lpPaidThisTurnOpponent = 0;
        }

        RefreshAttackIndicators();

        // Atualiza as cores de hover dos botões de fase para o turno atual
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.UpdateHoverColors(isPlayerTurn);
        }
        
        StartCoroutine(TurnTransitionRoutine());
    }

    private IEnumerator TurnTransitionRoutine()
    {
        AnnounceText(isPlayerTurn ? phaseAnnouncements.textPlayerTurn : phaseAnnouncements.textOpponentTurn);
        
        // Espera o texto do turno sumir perfeitamente para emendar na Draw Phase
        yield return new WaitForSeconds((phaseAnnouncements.displayDuration + phaseAnnouncements.fadeDuration) * phaseAnnouncements.masterDelayMultiplier);

        if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isBusy);

        if (PhaseManager.Instance != null) PhaseManager.Instance.StartTurn();

        // Se for turno do oponente, inicia a IA
        if (!isPlayerTurn && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            OpponentAI.Instance.StartAITurn(!isSimulating);
        }
    }

    // Chamado pelo PhaseManager quando entra na Draw Phase
    public void OnDrawPhaseStart()
    {
        normalSummonsThisTurnPlayer = 0;
        normalSummonsThisTurnOpponent = 0;

        // Reseta os ataques dos monstros em campo
        if (duelFieldUI != null)
        {
            if (duelFieldUI.playerMonsterZones != null)
            {
                foreach (var zone in duelFieldUI.playerMonsterZones)
                {
                    if (zone != null && zone.childCount > 0)
                    {
                        var card = zone.GetComponentInChildren<CardDisplay>();
                        if (card != null) {
                            card.hasAttackedThisTurn = false;
                            card.hasChangedPositionThisTurn = false;
                        }
                    }
                }
            }
            if (duelFieldUI.opponentMonsterZones != null)
            {
                foreach (var zone in duelFieldUI.opponentMonsterZones)
                {
                    if (zone != null && zone.childCount > 0)
                    {
                        var card = zone.GetComponentInChildren<CardDisplay>();
                        if (card != null) {
                            card.hasAttackedThisTurn = false;
                            card.hasChangedPositionThisTurn = false;
                        }
                    }
                }
            }
        }

        turnCount++; // Incrementa o turno
        if (DeckManager.Instance != null) DeckManager.Instance.ResetTurnStats();
        
        StartCoroutine(DrawPhaseRoutine());
    }

    private IEnumerator DrawPhaseRoutine()
    {
        // 1. Anuncia a fase PRIMEIRO
        AnnounceText(phaseAnnouncements.textDrawPhase);
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnPhaseStart(GamePhase.Draw);
        
        // 2. Espera o tempo configurado de leitura do texto para sacar a carta com calma
        yield return new WaitForSeconds(phaseAnnouncements.displayDuration * phaseAnnouncements.masterDelayMultiplier);

        // Validação da Regra do Primeiro Turno
        bool skipDraw = (turnCount == 1 && applyModernFirstTurnDrawRule);

        if (isPlayerTurn)
        {
            if (!canPlayerDrawFromDeck && !skipDraw)
            {
                for (int i = 0; i < 1; i++)
                    DrawCard();
            }
            else if (skipDraw) 
            {
                Debug.Log("[GameManager] Regra Moderna: Turno 1. Nenhuma carta comprada.");
                StartCoroutine(DelayedPhaseChange(GamePhase.Standby, phaseAnnouncements.drawToStandbyDelay * phaseAnnouncements.masterDelayMultiplier));
            }
        }
        else
        {
            if (!skipDraw) DrawOpponentCard();
            else 
            {
                Debug.Log("[GameManager] Regra Moderna: Turno 1 (Oponente). Nenhuma carta comprada.");
                StartCoroutine(DelayedPhaseChange(GamePhase.Standby, phaseAnnouncements.drawToStandbyDelay * phaseAnnouncements.masterDelayMultiplier));
            }
        }
    }

    // Chamado pelo PhaseManager quando entra na Standby Phase
    public void OnStandbyPhaseStart()
    {
        // Verifica custos de manutenção (Imperial Order, Mirror Wall, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.CheckMaintenanceCosts();
        }
        AnnounceText(phaseAnnouncements.textStandbyPhase);
    }

    // Chamado pelo PhaseManager quando entra na End Phase
    public void OnEndPhaseStart()
    {
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnPhaseStart(GamePhase.End);
        }
        AnnounceText(phaseAnnouncements.textEndPhase);

        // Inicia verificação de Limite de Mão
        StartCoroutine(HandleHandLimitSequence());
    }

    public void OnMainPhase1Start()
    {
        AnnounceText(phaseAnnouncements.textMainPhase1);
        RefreshAttackIndicators();
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnPhaseStart(GamePhase.Main1);
    }

    public void OnBattlePhaseStart()
    {
        AnnounceText(phaseAnnouncements.textBattlePhase);
        RefreshAttackIndicators();
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnPhaseStart(GamePhase.Battle);
    }

    public void OnMainPhase2Start()
    {
        AnnounceText(phaseAnnouncements.textMainPhase2);
        RefreshAttackIndicators();
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnPhaseStart(GamePhase.Main2);
    }

    private IEnumerator HandleHandLimitSequence()
    {
        // Verifica se a regra está ativa
        if (!enableHandLimit)
        {
            // Se for turno do jogador, troca o turno automaticamente (já que não haverá descarte)
            if (isPlayerTurn && !isSimulating)
            {
                if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isBusy);
                yield return new WaitForSeconds(0.5f);
                SwitchTurn();
            }
            yield break;
        }

        // Verifica Infinite Cards (0942)
        int currentLimit = handLimit;
        if (IsCardActiveOnField("Infinite Cards") || IsCardActiveOnField("0942")) currentLimit = 99;

        List<GameObject> hand = isPlayerTurn ? playerHand : opponentHand;
        
        if (hand.Count > currentLimit)
        {
            int toDiscard = hand.Count - currentLimit;
            Debug.Log($"[GameManager] Limite de mão excedido ({hand.Count}/{currentLimit}). Descartando {toDiscard} cartas.");

            if (isPlayerTurn)
            {
                if (isSimulating)
                {
                    for (int i = 0; i < toDiscard; i++) DiscardCard(playerHand[0].GetComponent<CardDisplay>());
                }
                else
                {
                    yield return StartCoroutine(PlayerDiscardHandLimit(toDiscard));
                }
            }
            else
            {
                // IA descarta
                if (OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeSelf && !isSimulating)
                {
                    OpponentAI.Instance.PerformHandLimitDiscard(toDiscard);
                    yield return new WaitForSeconds(1.0f); // Tempo para visualização
                }
                else if (isSimulating)
                {
                    for (int i = 0; i < toDiscard; i++) DiscardCard(opponentHand[0].GetComponent<CardDisplay>());
                }
            }
        }

        // Se for turno do jogador, troca o turno automaticamente após processar a End Phase e Limite de Mão
        // (A IA troca o turno no final da rotina dela, então não precisamos chamar aqui para ela)
        if (isPlayerTurn && !isSimulating)
        {
            if (CardEffectManager.Instance != null) yield return new WaitWhile(() => CardEffectManager.Instance.isBusy);
            yield return new WaitForSeconds(0.5f);
            SwitchTurn();
        }
    }

    private IEnumerator PlayerDiscardHandLimit(int count)
    {
        bool done = false;
        List<CardData> handData = GetPlayerHandData();
        
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Limite de mão excedido. Descarte {count} cartas.");
        yield return new WaitForSeconds(1.5f);

        OpenCardMultiSelection(handData, $"Descarte {count} cartas (Limite de Mão)", count, count, (selected) => {
            foreach(var c in selected)
            {
                GameObject go = playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c);
                if (go != null) DiscardCard(go.GetComponent<CardDisplay>());
            }
            done = true;
        });

        while (!done) yield return null;
    }

    private IEnumerator DelayedPhaseChange(GamePhase phase, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CardEffectManager.Instance != null) {
            yield return new WaitWhile(() => CardEffectManager.Instance.isBusy);
        }
        if (PhaseManager.Instance != null) PhaseManager.Instance.ChangePhase(phase);
    }

    // --- MENU DE SELEÇÃO DE FASE (Botão Direito) ---
    public void OpenPhaseSelectionMenu()
    {
        if (UIManager.Instance != null && UIManager.Instance.phaseSelectionMenu != null)
        {
            UIManager.Instance.phaseSelectionMenu.Show();
        }
        else
        {
            Debug.LogError("O novo Painel de Fases (PhaseSelectionMenu) não foi atribuído no UIManager!");
        }
    }
    // Método para finalizar o duelo e calcular o Rank
    // Chame isso quando o HP de alguém chegar a 0 ou Deck acabar
    public void EndDuel(bool playerWon, bool isDeckOut = false)
    {
        if (isDuelOver) return; // Previne chamadas múltiplas
        isDuelOver = true;

        StartCoroutine(EndDuelRoutine(playerWon, isDeckOut));
    }

    private IEnumerator EndDuelRoutine(bool playerWon, bool isDeckOut)
    {
        // 1. Mostra a mensagem de WIN/LOSE (Pula no modo simulador rápido para não atrasar)
        if (!isSimulating && UIManager.Instance != null && UIManager.Instance.endDuelMessagePanel != null)
        {
            Transform winImg = UIManager.Instance.endDuelMessagePanel.transform.Find("ImageWin");
            Transform loseImg = UIManager.Instance.endDuelMessagePanel.transform.Find("ImageLose");
            
            if (winImg != null) winImg.gameObject.SetActive(playerWon);
            if (loseImg != null) loseImg.gameObject.SetActive(!playerWon);

            CanvasGroup cg = UIManager.Instance.endDuelMessagePanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = UIManager.Instance.endDuelMessagePanel.AddComponent<CanvasGroup>();
            
            cg.alpha = 0f;
            UIManager.Instance.endDuelMessagePanel.SetActive(true);

            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
                yield return null;
            }
            cg.alpha = 1f;
        }

        // Pausa dramática apenas se não estiver simulando
        if (!isSimulating) yield return new WaitForSeconds(1.5f);

        GameObject blackOverlay = null;
        Image blackImg = null;

        // 2. Transição suave para tela preta antes de esconder a mensagem e ir pros rewards
        if (!isSimulating && UIManager.Instance != null && UIManager.Instance.endDuelMessagePanel != null)
        {
            blackOverlay = new GameObject("BlackOverlay_EndDuel", typeof(RectTransform), typeof(Canvas), typeof(Image));
            Canvas canvas = blackOverlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // Fica por cima de tudo
            
            blackImg = blackOverlay.GetComponent<Image>();
            blackImg.color = new Color(0, 0, 0, 0);

            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                blackImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, elapsed / fadeTime));
                yield return null;
            }

            UIManager.Instance.endDuelMessagePanel.SetActive(false);
        }

        // 3. Calcula pontuação e recompensas
        if (DuelScoreManager.Instance != null)
        {
            DuelScoreManager.Instance.StopDuelTracking(playerWon, isDeckOut, playerLP);

            int score;
            DuelRank rank = DuelScoreManager.Instance.CalculateFinalRank(out score);
            Debug.Log($"DUELO FINALIZADO! Venceu: {playerWon} | Rank: {rank} | Pontos: {score}");
            Debug.Log(DuelScoreManager.Instance.GetScoreReport());

            CardData rewardCard = null;
            bool isNewCard = false;

            if (playerWon)
            {
                if (currentOpponent != null)
                {
                    // Registra vitória na Biblioteca
                    if (SaveLoadSystem.Instance != null)
                    {
                        SaveLoadSystem.Instance.RegisterDuelistWin(currentOpponent.id);
                    }

                    // --- SISTEMA DE DROP RATE PERCENTUAL ---
                    rewardCard = CalculateDrop(rank, currentOpponent);
                }

                // FALLBACK DE TESTE: Se não tiver oponente ou o drop dele falhar, dá uma carta aleatória do banco inteiro!
                if (rewardCard == null && cardDatabase != null && cardDatabase.cardDatabase.Count > 0)
                {
                    rewardCard = cardDatabase.cardDatabase[UnityEngine.Random.Range(0, cardDatabase.cardDatabase.Count)];
                }

                Debug.Log($"Recompensa Drop Rate: {rewardCard?.name ?? "Nenhuma"}");

                // LÓGICA DE DROP: Adiciona ao Baú do Jogador
                if (rewardCard != null)
                {
                    // Verifica se é nova (não existe no baú) ANTES de adicionar
                    isNewCard = !playerTrunk.Contains(rewardCard.id);

                    playerTrunk.Add(rewardCard.id);
                    
                    // Salva o progresso imediatamente para garantir o drop
                    if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.SaveGame(currentSaveID);
                }
            }
            
            if (!isSimulating && UIManager.Instance != null)
            {
                // Se venceu, passa o Rank real. Se perdeu, passa "LOSE" para a UI de Recompensas saber.
                if (playerWon) 
                {
                    UIManager.Instance.ShowRewardScreen(rank.ToString(), rewardCard, isNewCard);
                }
                else 
                {
                    UIManager.Instance.ShowRewardScreen("LOSE", null, false);
                }
            }
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.Btn_BackToMenu();
        }

        // 4. Remove a tela preta suavemente revelando a tela de Rewards
        if (blackOverlay != null && blackImg != null)
        {
            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                blackImg.color = new Color(0, 0, 0, Mathf.Lerp(1f, 0f, elapsed / fadeTime));
                yield return null;
            }
            Destroy(blackOverlay);
        }
    }

    private CardData CalculateDrop(DuelRank rank, CharacterData opp)
    {
        // Percentuais definidos na documentação do Game Design (ScoringAndDropRate.md)
        float pSPlus = 0, pS = 0, pB = 0, pC = 0, pD = 0;

        switch (rank)
        {
            case DuelRank.SPlus: pSPlus = 15f; pS = 34f; pB = 25.5f; pC = 17f; pD = 8.5f; break;
            case DuelRank.S:
            case DuelRank.APlus:
            case DuelRank.A:     pSPlus = 0f; pS = 40f; pB = 30f; pC = 20f; pD = 10f; break;
            case DuelRank.BPlus:
            case DuelRank.B:     pSPlus = 0f; pS = 10f; pB = 40f; pC = 30f; pD = 20f; break;
            case DuelRank.CPlus:
            case DuelRank.C:     pSPlus = 0f; pS = 3f; pB = 17f; pC = 40f; pD = 40f; break;
            case DuelRank.DPlus:
            case DuelRank.D:     pSPlus = 0f; pS = 1f; pB = 4f; pC = 25f; pD = 70f; break;
            case DuelRank.F:     return null;
        }

        List<string> poolSPlus = new List<string>();
        List<string> poolS = new List<string>();
        List<string> poolB = new List<string>();
        List<string> poolC = new List<string>();
        List<string> poolD = new List<string>();

        // Os drops S+ (A Única) e S (Super Raros) vêm diretamente do "unique_drops" gerado pelo Python
        if (opp.unique_drops != null && opp.unique_drops.Count > 0)
        {
            poolSPlus.Add(opp.unique_drops[0]); // Primeira carta é a Assinatura S+
            for (int i = 1; i < opp.unique_drops.Count; i++)
            {
                poolS.Add(opp.unique_drops[i]);
            }
        }

        // Coleta todas as cartas de todos os decks do personagem para fatiar nas tiers inferiores
        HashSet<string> allDeckCards = new HashSet<string>();
        if (opp.deck_A != null) foreach (var id in opp.deck_A) allDeckCards.Add(id);
        if (opp.deck_B != null) foreach (var id in opp.deck_B) allDeckCards.Add(id);
        if (opp.deck_C != null) foreach (var id in opp.deck_C) allDeckCards.Add(id);
        if (opp.extra_deck_A != null) foreach (var id in opp.extra_deck_A) allDeckCards.Add(id);
        if (opp.extra_deck_B != null) foreach (var id in opp.extra_deck_B) allDeckCards.Add(id);
        if (opp.extra_deck_C != null) foreach (var id in opp.extra_deck_C) allDeckCards.Add(id);

        // Remove as cartas que já foram separadas como S/S+
        foreach (var id in poolSPlus) allDeckCards.Remove(id);
        foreach (var id in poolS) allDeckCards.Remove(id);

        // Prepara para ordenar pelo poder do Pool (Ex: 5.5 -> 1.1)
        List<CardData> fillerCards = new List<CardData>();
        foreach (var id in allDeckCards)
        {
            CardData c = cardDatabase.GetCardById(id);
            if (c != null) fillerCards.Add(c);
        }

        fillerCards.Sort((x, y) => {
            float px = 1.1f; float py = 1.1f;
            float.TryParse(x.pool, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out px);
            float.TryParse(y.pool, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out py);
            int comp = py.CompareTo(px);
            if (comp == 0) return y.atk.CompareTo(x.atk); // Desempata pelo ATK
            return comp;
        });

        // Fatiamento das cartas restantes (25% B, 30% C, 45% D)
        int totalFiller = fillerCards.Count;
        int bCount = Mathf.CeilToInt(totalFiller * 0.25f);
        int cCount = Mathf.CeilToInt(totalFiller * 0.30f);

        for (int i = 0; i < totalFiller; i++)
        {
            if (i < bCount) poolB.Add(fillerCards[i].id);
            else if (i < bCount + cCount) poolC.Add(fillerCards[i].id);
            else poolD.Add(fillerCards[i].id);
        }

        // Fallbacks de segurança (evita lista vazia)
        if (poolSPlus.Count == 0) pSPlus = 0f;
        if (poolS.Count == 0) { poolS.AddRange(poolB); pS = pS / 2f; } // Reduz a chance de S se não houver drops únicos reais
        if (poolB.Count == 0) poolB.AddRange(poolC);
        if (poolC.Count == 0) poolC.AddRange(poolD);
        if (poolD.Count == 0) poolD.AddRange(poolC);

        float totalWeight = pSPlus + pS + pB + pC + pD;
        if (totalWeight <= 0) return null;

        // Roleta de Porcentagens
        float roll = Random.Range(0f, totalWeight);
        List<string> selectedPool = null;

        if (roll < pSPlus && poolSPlus.Count > 0) selectedPool = poolSPlus;
        else
        {
            roll -= pSPlus;
            if (roll < pS) selectedPool = poolS;
            else
            {
                roll -= pS;
                if (roll < pB) selectedPool = poolB;
                else
                {
                    roll -= pB;
                    if (roll < pC) selectedPool = poolC;
                    else selectedPool = poolD;
                }
            }
        }

        if (selectedPool == null || selectedPool.Count == 0) selectedPool = poolD;

        if (selectedPool != null && selectedPool.Count > 0)
        {
            string finalId = selectedPool[Random.Range(0, selectedPool.Count)];
            return cardDatabase.GetCardById(finalId);
        }

        return null;
    }
    // --- CONDIÇÕES DE VITÓRIA ESPECIAIS ---

    public void CheckExodiaWin()
    {
        // Remove cartas nulas da mão caso alguma tenha sido destruída sem ser removida da lista
        playerHand.RemoveAll(go => go == null);
        opponentHand.RemoveAll(go => go == null);

        // IDs das 5 partes do Exodia (Baseado no seu JSON)
        string[] exodiaParts = { "DM0595", "DM1490", "DM1030", "DM1491", "DM1031" };
        HashSet<string> handIds = new HashSet<string>();

        foreach (GameObject cardGO in playerHand)
        {
            CardDisplay display = cardGO.GetComponent<CardDisplay>();
            if (display != null) handIds.Add(display.CurrentCardData.id);
        }

        if (exodiaParts.All(id => handIds.Contains(id)))
        {
            Debug.Log("EXODIA OBLITERATE! VITÓRIA AUTOMÁTICA!");
            // TODO: Tocar animação especial do Exodia aqui
            // TROFÉU: Exodia
            if (TrophyManager.Instance != null) TrophyManager.Instance.Unlock(80);
            
            // Tenta forçar a busca caso o painel tenha começado desativado no Inspector
            if (ExodiaWinUI.Instance == null)
            {
                ExodiaWinUI.Instance = Resources.FindObjectsOfTypeAll<ExodiaWinUI>().FirstOrDefault();
            }

            if (ExodiaWinUI.Instance != null)
            {
                ExodiaWinUI.Instance.ShowWinSequence(true, () => EndDuel(true));
            }
            else
            {
                EndDuel(true);
            }
        }
    }

    // --- CONTROLE DE FASE (UI) ---

    public void TryChangePhase(GamePhase newPhase)
    {
        if (PhaseManager.Instance != null) PhaseManager.Instance.TryChangePhase(newPhase);
    }

    // --- SISTEMA DE INFORMATIVOS E ANÚNCIOS DE FASE ---

    private GameObject currentAnnouncementObj;
    private Coroutine currentAnnouncementRoutine;

    public void AnnounceText(string text)
    {
        if (!phaseAnnouncements.enableAnnouncements || isSimulating) return;
        
        if (currentAnnouncementRoutine != null)
        {
            StopCoroutine(currentAnnouncementRoutine);
            currentAnnouncementRoutine = null;
        }
        if (currentAnnouncementObj != null)
        {
            Destroy(currentAnnouncementObj);
            currentAnnouncementObj = null;
        }

        currentAnnouncementRoutine = StartCoroutine(AnnouncementRoutine(text));
    }

    private IEnumerator AnnouncementRoutine(string text)
    {
        Transform uiParent = GetUIParent();
        if (duelFieldUI != null) 
        {
            Transform fieldArea = duelFieldUI.transform.Find("FieldArea");
            Transform fieldImg = duelFieldUI.transform.Find("FieldImg");
            if (fieldArea != null) uiParent = fieldArea;
            else if (fieldImg != null) uiParent = fieldImg;
            else uiParent = duelFieldUI.transform;
        }
        if (uiParent == null) yield break;

        GameObject announceObj = new GameObject("PhaseAnnouncement", typeof(RectTransform), typeof(CanvasGroup));
        currentAnnouncementObj = announceObj;
        announceObj.transform.SetParent(uiParent, false);
        announceObj.transform.SetAsLastSibling();

        RectTransform rt = announceObj.GetComponent<RectTransform>();
        rt.anchoredPosition = phaseAnnouncements.offset + new Vector2(0, phaseAnnouncements.slideDistance);
        rt.sizeDelta = new Vector2(1200, 200);

        CanvasGroup cg = announceObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(announceObj.transform, false);
        
        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txtRt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = phaseAnnouncements.fontSize;
        
        Color txtColor = phaseAnnouncements.neutralTextColor;
        if (text != phaseAnnouncements.textStartDuel)
            txtColor = isPlayerTurn ? phaseAnnouncements.playerTextColor : phaseAnnouncements.opponentTextColor;
        
        tmp.color = txtColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;

        if (phaseAnnouncements.useOutline)
        {
            GameObject shadowObj = Instantiate(textObj, announceObj.transform);
            shadowObj.name = "Shadow";
            shadowObj.transform.SetAsFirstSibling();
            TextMeshProUGUI shadowTmp = shadowObj.GetComponent<TextMeshProUGUI>();
            shadowTmp.color = phaseAnnouncements.outlineColor;
            shadowTmp.rectTransform.anchoredPosition = phaseAnnouncements.outlineThickness;
        }

        float fadeDur = phaseAnnouncements.fadeDuration * phaseAnnouncements.masterDelayMultiplier;
        float holdDur = phaseAnnouncements.displayDuration * phaseAnnouncements.masterDelayMultiplier;
        
        float t = 0;
        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = phaseAnnouncements.offset;

        // Animação de Entrada (Slide In e Fade In)
        if (fadeDur > 0)
        {
            while (t < fadeDur)
            {
                t += Time.deltaTime;
                float p = t / fadeDur;
                cg.alpha = p;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, p));
                yield return null;
            }
        }
        else
        {
            cg.alpha = 1f;
            rt.anchoredPosition = targetPos;
        }

        // Segura o texto na tela
        if (holdDur > 0) yield return new WaitForSeconds(holdDur);

        // Animação de Saída (Slide Out e Fade Out)
        t = 0;
        startPos = rt.anchoredPosition;
        targetPos = phaseAnnouncements.offset - new Vector2(0, phaseAnnouncements.slideDistance);

        if (fadeDur > 0)
        {
            while (t < fadeDur)
            {
                t += Time.deltaTime;
                float p = t / fadeDur;
                cg.alpha = 1f - p;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, p));
                yield return null;
            }
        }
        else
        {
            cg.alpha = 0f;
            rt.anchoredPosition = targetPos;
        }

        Destroy(announceObj);
    }
}