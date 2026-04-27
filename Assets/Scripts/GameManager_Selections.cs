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
    // SELECTIONS
    // ==============================================================================

    private bool handSelectionCanCancel = true;
    private int handSelectionMinRequired = 0;

    public void CancelAttackTargeting()
    {
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null && CardEffectManager.Instance.luaDuel.currentAttacker != null)
        {
            var attacker = CardEffectManager.Instance.luaDuel.currentAttacker.unityCard;
            if (attacker != null) attacker.SetAttackSelectionVisual(false);
            CardEffectManager.Instance.luaDuel.currentAttacker = null;
            if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
            RefreshAttackIndicators();
        }
    }
    // --- SISTEMA DE MOEDAS ---
    public void TossCoin(int numberOfCoins, System.Action<int> onResult)
    {
        if (isSimulating)
        {
            int headsCount = 0;
            for (int i = 0; i < numberOfCoins; i++)
                if (alwaysCoinHead || Random.value > 0.5f) headsCount++;
            onResult?.Invoke(headsCount);
            return;
        }

        StartCoroutine(CoinTossRoutine(numberOfCoins, onResult));
    }

    private IEnumerator CoinTossRoutine(int numberOfCoins, System.Action<int> onResult)
    {
        int headsCount = 0;
        for (int i = 0; i < numberOfCoins; i++)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Jogando moeda {i + 1}...");
            yield return new WaitForSeconds(0.8f); // Tempo para "animação"

            bool isHeads = Random.value > 0.5f;
            
            // Debug: Força Cara se a opção estiver ativa
            if (alwaysCoinHead) isHeads = true;

            if (isHeads) headsCount++;
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage(isHeads ? "CARA!" : "COROA!");
            yield return new WaitForSeconds(0.5f);
        }

        // --- SISTEMA 3: SECOND COIN TOSS ---
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.HasActiveSecondCoinToss(out bool isPlayerToss))
        {
            if (isPlayerToss && UIManager.Instance != null)
            {
                bool isWaiting = true;
                bool doReroll = false;
                UIManager.Instance.ShowConfirmation($"Você tirou {headsCount} cara(s). Usar Second Coin Toss para tentar de novo?",
                    () => { doReroll = true; isWaiting = false; },
                    () => { isWaiting = false; }
                );
                while(isWaiting) yield return null;

                if (doReroll)
                {
                    CardEffectManager.Instance.ConsumeSecondCoinToss(true);
                    StartCoroutine(CoinTossRoutine(numberOfCoins, onResult));
                    yield break; // Cancela este fluxo e inicia o novo
                }
            }
            else if (!isPlayerToss && headsCount == 0) // IA simples: Rerola se tirou 0 caras
            {
                CardEffectManager.Instance.ConsumeSecondCoinToss(false);
                StartCoroutine(CoinTossRoutine(numberOfCoins, onResult));
                yield break;
            }
        }
        
        onResult?.Invoke(headsCount);
    }

    // --- SISTEMA DE DADOS ---
    public void RollDice(int count, bool requireChoice, Action<List<int>> callback)
    {
        if (isSimulating)
        {
            List<int> results = new List<int>();
            for (int i = 0; i < count; i++) results.Add(alwaysDiceSix ? 6 : UnityEngine.Random.Range(1, 7));
            callback?.Invoke(results);
            return;
        }

        Action<List<int>> interceptCallback = (results) => {
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.HasActiveDiceReRoll(out bool isPlayerToss))
            {
                if (isPlayerToss && UIManager.Instance != null)
                {
                    string resStr = string.Join(", ", results);
                    UIManager.Instance.ShowConfirmation($"Você tirou {resStr} nos dados. Usar Dice Re-Roll para tentar de novo?",
                        () => {
                            CardEffectManager.Instance.ConsumeDiceReRoll(true);
                            RollDice(count, requireChoice, callback);
                        },
                        () => {
                            callback?.Invoke(results);
                        }
                    );
                }
                else if (!isPlayerToss) // IA simples: Rerola se a soma for muito baixa
                {
                    int sum = results.Sum();
                    if (sum <= count * 3) {
                        CardEffectManager.Instance.ConsumeDiceReRoll(false);
                        RollDice(count, requireChoice, callback);
                    } else {
                        callback?.Invoke(results);
                    }
                }
            }
            else
            {
                callback?.Invoke(results);
            }
        };

        if (diceRollUI != null)
        {
            diceRollUI.ShowRoll(count, requireChoice, diceRollManualSpin, interceptCallback, alwaysDiceSix);
        }
        else
        {
            List<int> results = new List<int>();
            for (int i = 0; i < count; i++) results.Add(alwaysDiceSix ? 6 : UnityEngine.Random.Range(1, 7));
            interceptCallback(results);
        }
    }
    public void OpenCardSelection(List<CardData> sourceList, string title, System.Action<CardData> onSelected)
    {
        OpenCardMultiSelection(sourceList, title, 1, 1, (selectedList) =>
        {
            if (selectedList != null && selectedList.Count > 0)
                onSelected?.Invoke(selectedList[0]);
        });
    }

    // Método para Seleção Múltipla
    public void OpenCardMultiSelection(List<CardData> sourceList, string title, int min, int max, System.Action<List<CardData>> onSelected, HighlightCategory category = HighlightCategory.GenericTarget, bool forceModal = false, bool canCancel = true)
    {
        if (isSimulating)
        {
                if (max <= 0) { onSelected?.Invoke(new List<CardData>()); return; }
            int count = Mathf.Max(min, 1);
            onSelected?.Invoke(sourceList.Take(count).ToList());
            return;
        }
        // Verifica se TODAS as cartas da seleção estão fisicamente presentes na Mesa ou na Mão
        bool isHandOrFieldSubset = sourceList.Count > 0 && sourceList.All(c => {
            if (playerHand.Exists(go => go.GetComponent<CardDisplay>().CurrentCardData == c)) return true;
            if (opponentHand.Exists(go => go.GetComponent<CardDisplay>().CurrentCardData == c)) return true;
            if (FindCardOnField(c.id, true) != null) return true;
            if (FindCardOnField(c.id, false) != null) return true;
            return false;
        });

        bool isFusionOrRitual = title.Contains("Fusão") || title.Contains("Ritual") || title.Contains("Tributo");

        if (useDirectHandSelection && !forceModal && isHandOrFieldSubset && min == max && !isFusionOrRitual)
        {
            StartDirectSelection(sourceList, min, max, null, title, onSelected, category, canCancel);
            return;
        }

        if (sourceList.Count > 0)
        {
            if (CardSelectionUI.Instance != null)
            {
                CardSelectionUI.Instance.Show(sourceList, title, min, max, onSelected, category, canCancel);
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowCardSelection(sourceList, title, min, max, onSelected);
            }
            else
            {
                Debug.LogError("GameManager: UIManager não encontrado para seleção de cartas.");
            }
        }
        else
        {
            Debug.Log("Nenhuma carta válida para selecionar.");
        }
    }

    // --- LÓGICA DE SELEÇÃO DIRETA DA MÃO ---

    private void StartDirectSelection(List<CardData> candidates, int min, int max, System.Func<List<CardData>, bool> validator, string title, System.Action<List<CardData>> callback, HighlightCategory category = HighlightCategory.GenericTarget, bool canCancel = true)
    {
        isSelectingFromHand = true;
        handSelectionCandidates = candidates;
        handSelectionCountRequired = max;
        handSelectionMinRequired = min;
        handSelectionCanCancel = canCancel;
        customSelectionValidator = validator;
        handSelectionCallback = callback;
        currentHandSelectionObjects = new List<GameObject>();
        currentSelectionHighlightCategory = category;

        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(title);

        // Destaca as cartas válidas na mão E no campo
        List<GameObject> allCards = new List<GameObject>(playerHand);
        if (duelFieldUI != null) {
            foreach(var z in duelFieldUI.playerMonsterZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            foreach(var z in duelFieldUI.playerSpellZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            foreach(var z in duelFieldUI.opponentMonsterZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            foreach(var z in duelFieldUI.opponentSpellZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
        }

        foreach (var go in allCards)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd != null && handSelectionCandidates.Any(c => c == cd.CurrentCardData || c.id == cd.CurrentCardData.id))
            {
                if (DuelFXManager.Instance != null)
                    DuelFXManager.Instance.SetSelectionIcon(cd, currentSelectionHighlightCategory, SelectionState.Available);
                else
                    cd.SetHighlight(currentSelectionHighlightCategory, true);
            }
        }
        Debug.Log($"[GameManager] Iniciando seleção tátil: {title}");
    }

    public void OpenDirectCardDisplaySelection(List<CardDisplay> candidates, string title, int min, int max, System.Action<List<CardDisplay>> callback, HighlightCategory category = HighlightCategory.GenericTarget)
    {
        isSelectingFromHand = true;
        handSelectionDisplayCandidates = candidates;
        handSelectionCandidates = null; // Garante que a seleção por CardData não interfira
        handSelectionCountRequired = max;
        handSelectionMinRequired = min;
        handSelectionCanCancel = true;
        displaySelectionCallback = callback;
        handSelectionCallback = null;
        currentHandSelectionObjects = new List<GameObject>();
        currentSelectionHighlightCategory = category;

        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(title);

        foreach (var cd in handSelectionDisplayCandidates)
        {
            if (cd != null)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetSelectionIcon(cd, currentSelectionHighlightCategory, SelectionState.Available);
                else cd.SetHighlight(currentSelectionHighlightCategory, true);
            }
        }
        Debug.Log($"[GameManager] Iniciando seleção tátil EXATA por display: {title}");
    }

    public void HandleHandCardClick(CardDisplay card)
    {
        if (!isSelectingFromHand) return;
        
        Debug.Log($"[Selection] Clique detectado na carta: {card.CurrentCardData?.name}");

        if (handSelectionDisplayCandidates != null) {
            if (!handSelectionDisplayCandidates.Contains(card)) {
                Debug.Log($"[Selection] Bloqueado: {card.CurrentCardData?.name} não é um alvo selecionável neste momento.");
                return;
            }
        } else {
            if (handSelectionCandidates == null || !handSelectionCandidates.Any(c => c == card.CurrentCardData || c.id == card.CurrentCardData.id)) {
                Debug.Log($"[Selection] Bloqueado: Os dados de {card.CurrentCardData?.name} não estão na lista de permitidos.");
                return;
            }
        }

        if (currentHandSelectionObjects.Contains(card.gameObject))
        {
            // Deselecionar
            Debug.Log($"[Selection] A carta {card.CurrentCardData?.name} foi DESMARCADA.");
            currentHandSelectionObjects.Remove(card.gameObject);
        }
        else
        {
            // Selecionar
            Debug.Log($"[Selection] Selecionando: {card.CurrentCardData?.name}. Progresso (Selecionadas/Máximo): {currentHandSelectionObjects.Count + 1} / {handSelectionCountRequired}");

            // --- SMART SELECTION ALGORITHM ---
            List<CardData> currentDataBeforeClick = currentHandSelectionObjects.Select(go => go.GetComponent<CardDisplay>().CurrentCardData).ToList();
            bool wasValid = customSelectionValidator != null && customSelectionValidator(currentDataBeforeClick);

            currentHandSelectionObjects.Add(card.gameObject);
                
            if (customSelectionValidator != null)
            {
                List<CardData> testSelection = currentHandSelectionObjects.Select(go => go.GetComponent<CardDisplay>().CurrentCardData).ToList();
                    
                // Se a adição tornou a seleção inválida (ex: excesso de tributo OCG)
                if (!customSelectionValidator(testSelection))
                {
                    // Tenta achar um subset válido que INCLUA a nova carta
                    List<GameObject> bestSubset = null;
                    int bestScore = int.MinValue;
                    int n = currentHandSelectionObjects.Count;
                    int subsetCount = 1 << (n - 1);
                    
                    for (int i = 0; i < subsetCount; i++)
                    {
                        List<GameObject> candidate = new List<GameObject>();
                        int score = 0;
                        for (int j = 0; j < n - 1; j++)
                        {
                            if ((i & (1 << j)) != 0)
                            {
                                candidate.Add(currentHandSelectionObjects[j]);
                                score += j; // Prefere manter cartas mais recentes
                            }
                        }
                        candidate.Add(card.gameObject); // Sempre inclui a nova carta
                        
                        List<CardData> candidateData = candidate.Select(go => go.GetComponent<CardDisplay>().CurrentCardData).ToList();
                        if (customSelectionValidator(candidateData))
                        {
                            int candidateLevelSum = candidateData.Sum(c => c.level);
                            // Penaliza a quantidade de cartas, penaliza sobras de nível (busca exatidão), prefere recentes
                            int currentScore = -candidate.Count * 10000 - candidateLevelSum * 100 + score;
                            if (currentScore > bestScore)
                            {
                                bestScore = currentScore;
                                bestSubset = candidate;
                            }
                        }
                    }

                    if (bestSubset != null)
                    {
                        currentHandSelectionObjects = bestSubset;
                        Debug.Log($"[Selection] Smart Select: Subset válido encontrado! Ajustando a seleção de forma inteligente.");
                    }
                    else
                    {
                        if (wasValid)
                        {
                            // A seleção anterior já era válida. O clique em uma nova carta que não forma subset válido 
                            // indica que o jogador quer "recomeçar" a seleção a partir desta nova carta.
                            currentHandSelectionObjects.Clear();
                            currentHandSelectionObjects.Add(card.gameObject);
                            Debug.Log($"[Selection] Smart Select: Seleção anterior era válida. Recomeçando com {card.CurrentCardData.name}.");
                        }
                        else
                        {
                            // Apenas acumula (se ultrapassar o limite, remove o mais antigo)
                            if (currentHandSelectionObjects.Count > handSelectionCountRequired)
                            {
                                currentHandSelectionObjects.RemoveAt(0);
                                Debug.Log($"[Selection] Rolling Select: Removida a carta mais antiga para respeitar o limite.");
                            }
                        }
                    }
                }
            }
            else
            {
                // Lógica de limite padrão (Rolling Selection Tradicional) sem validador customizado
                if (currentHandSelectionObjects.Count > handSelectionCountRequired)
                {
                    currentHandSelectionObjects.RemoveAt(0);
                    Debug.Log($"[Selection] Rolling Select: Removida a carta mais antiga para respeitar o limite de {handSelectionCountRequired}.");
                }
            }
        }

        // Atualiza o visual de TODAS as cartas baseado no estado final da lista
        RefreshHandSelectionVisuals();

        List<CardData> currentDataSelection = currentHandSelectionObjects.Select(go => go.GetComponent<CardDisplay>().CurrentCardData).ToList();

        // Verifica se completou a seleção
        bool isValid = false;
        if (customSelectionValidator != null) {
            isValid = customSelectionValidator(currentDataSelection);
            Debug.Log($"[Selection] Avaliador Customizado do Ritual/Fusão aprovou esta combinação? {isValid}");
        } else {
            isValid = currentHandSelectionObjects.Count >= handSelectionMinRequired && currentHandSelectionObjects.Count <= handSelectionCountRequired;
            if (handSelectionCountRequired == 1 && currentHandSelectionObjects.Count == 1) isValid = true;
            Debug.Log($"[Selection] Quantidade padrão aprovada? {isValid}");
        }

        if (isValid)
        {
            if (confirmHandSelection)
            {
                string cardName = currentDataSelection[0].name;
                var cd = currentHandSelectionObjects[0].GetComponent<CardDisplay>();
                if (cd != null && cd.isFlipped && !cd.isPlayerCard) cardName = "monstro virado para baixo";

                string msg = customSelectionValidator != null ? "Confirmar os materiais selecionados?" : (handSelectionCountRequired == 1 ? $"Selecionar {cardName}?" : "Confirmar seleção?");
                UIManager.Instance.ShowConfirmation(msg, () => FinishHandSelection(false), () => {
                    // O jogador recusou a seleção atual. Como a janela é apenas de confirmação,
                    // cancelar aqui significa abortar completamente o efeito/ritual.
                    GameManager.Instance.justCanceledSomething = true;
                    FinishHandSelection(true);
                });
            }
            else
            {
                FinishHandSelection(false);
            }
        }
        else
        {
            // Tenta ocultar a janela de confirmação caso ela esteja aberta (útil ao desmarcar cartas e invalidar a soma)
            if (UIManager.Instance != null)
            {
                var hideMethod = UIManager.Instance.GetType().GetMethod("HideConfirmation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase) 
                              ?? UIManager.Instance.GetType().GetMethod("CloseConfirmation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (hideMethod != null) 
                {
                    hideMethod.Invoke(UIManager.Instance, null);
                }
                else 
                {
                    var panelField = UIManager.Instance.GetType().GetField("panelConfirmation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (panelField != null)
                    {
                        object panelVal = panelField.GetValue(UIManager.Instance);
                        if (panelVal is GameObject go) go.SetActive(false);
                        else if (panelVal is Component comp) comp.gameObject.SetActive(false);
                    }
                    else
                    {
                        Transform t = UIManager.Instance.transform.Find("Panel_Confirmation") ?? UIManager.Instance.transform.Find("PanelConfirmation");
                        if (t != null) t.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void RefreshHandSelectionVisuals()
    {
        List<CardDisplay> allCandidates = new List<CardDisplay>();
        if (handSelectionDisplayCandidates != null)
        {
            allCandidates.AddRange(handSelectionDisplayCandidates);
        }
        else
        {
            List<GameObject> allCards = new List<GameObject>(playerHand);
            if (duelFieldUI != null) {
                foreach(var z in duelFieldUI.playerMonsterZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
                foreach(var z in duelFieldUI.playerSpellZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
                foreach(var z in duelFieldUI.opponentMonsterZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
                foreach(var z in duelFieldUI.opponentSpellZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            }

            foreach (var go in allCards)
            {
                var cd = go.GetComponent<CardDisplay>();
                if (cd != null && handSelectionCandidates != null && handSelectionCandidates.Any(c => c == cd.CurrentCardData || c.id == cd.CurrentCardData.id))
                    allCandidates.Add(cd);
            }
        }

        foreach (var cd in allCandidates)
        {
            if (currentHandSelectionObjects.Contains(cd.gameObject))
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetSelectionIcon(cd, currentSelectionHighlightCategory, SelectionState.Selected);
                else { cd.SetHighlight(currentSelectionHighlightCategory, false); cd.SetAttackSelectionVisual(true); }
            }
            else
            {
                cd.SetAttackSelectionVisual(false);
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetSelectionIcon(cd, currentSelectionHighlightCategory, SelectionState.Available);
                else cd.SetHighlight(currentSelectionHighlightCategory, true);
            }
        }
    }

    public void FinishHandSelection(bool isCancel)
    {
        if (isCancel && !handSelectionCanCancel)
        {
            if (UIManager.Instance != null && !isSimulating) UIManager.Instance.ShowMessage("Esta seleção é obrigatória e não pode ser cancelada.");
            return;
        }

        if (handSelectionDisplayCandidates != null)
        {
            foreach (var cd in handSelectionDisplayCandidates)
            {
                if (cd != null)
                {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetSelectionIcon(cd, currentSelectionHighlightCategory, SelectionState.None);
                    cd.SetHighlight(currentSelectionHighlightCategory, false);
                    cd.SetAttackSelectionVisual(false);
                }
            }
        }
        else
        {
            List<GameObject> allCards = new List<GameObject>(playerHand);
            if (duelFieldUI != null) {
                foreach(var z in duelFieldUI.playerMonsterZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
                foreach(var z in duelFieldUI.playerSpellZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
                foreach(var z in duelFieldUI.opponentMonsterZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
                foreach(var z in duelFieldUI.opponentSpellZones) if(z.childCount>0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            }

            foreach (var go in allCards)
            {
                var cd = go.GetComponent<CardDisplay>();
                if (cd != null)
                {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetSelectionIcon(cd, currentSelectionHighlightCategory, SelectionState.None);
                    cd.SetHighlight(currentSelectionHighlightCategory, false);
                    cd.SetAttackSelectionVisual(false);
                }
            }
        }

        isSelectingFromHand = false;
        
        if (displaySelectionCallback != null) {
            List<CardDisplay> finalData = currentHandSelectionObjects.Select(go => go.GetComponent<CardDisplay>()).ToList();
            var tempCallback = displaySelectionCallback;
            displaySelectionCallback = null; handSelectionDisplayCandidates = null;
            tempCallback.Invoke(isCancel ? new List<CardDisplay>() : finalData);
        } else if (handSelectionCallback != null) {
            List<CardData> finalData = currentHandSelectionObjects.Select(go => go.GetComponent<CardDisplay>().CurrentCardData).ToList();
            var tempCallback = handSelectionCallback;
            handSelectionCallback = null; handSelectionCandidates = null;
            tempCallback.Invoke(isCancel ? new List<CardData>() : finalData);
        }
    }

    // --- LÓGICA DE SELEÇÃO DE RESPOSTA DIRETA ---
    public void StartResponseSelection(List<CardDisplay> candidates, System.Action<CardDisplay> onSelect, System.Action onCancel)
    {
        isSelectingResponse = true;
        responseCandidates = candidates;
        responseCallback = onSelect;
        responseCancelCallback = onCancel;

        // Removido o brilho contínuo ("mira") das candidatas. 
        // O jogador será guiado puramente pelo Hover Preditivo (balão) ao passar o mouse.
    }

    public void HandleResponseSelection(CardDisplay card)
    {
        if (!isSelectingResponse) return;
        if (!responseCandidates.Contains(card)) return;

        FinishResponseSelection(card);
    }

    public void CancelResponseSelection() { if (!isSelectingResponse) return; FinishResponseSelection(null); }

    public bool IsResponseCandidate(CardDisplay card)
    {
        return isSelectingResponse && responseCandidates != null && responseCandidates.Contains(card);
    }

    private void FinishResponseSelection(CardDisplay selectedCard)
    {
        isSelectingResponse = false;

        if (selectedCard != null) responseCallback?.Invoke(selectedCard);
        else responseCancelCallback?.Invoke();
    }
    // --- INDICADOR DE ATAQUE DISPONÍVEL ---

    /// <summary>
    /// Deve ser chamado pelo CardDisplay no método OnPointerEnter/OnPointerExit.
    /// Mostra a espadinha (indicador) apenas durante o Hover se o modo for HoverOnly.
    /// </summary>
    public void HandleAttackIndicatorHover(CardDisplay card, bool isHovering)
    {
        if (!card.isOnField || !card.isPlayerCard || card.isFlipped) return; // Só exibe indicadores em cartas de face para cima do jogador
        if (!card.CurrentCardData.type.Contains("Monster")) return;
        
        // --- NOVO: Trava de Bloqueio Visual ---
        // Se estivermos selecionando alvo para um efeito, em uma corrente, ou selecionando algo na mão,
        // silencia os indicadores táticos para manter a tela focada apenas no targeting.
        bool isBusy = (CardEffectManager.Instance != null && (CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isChainResolving)) || isSelectingFromHand;
        
        if (isBusy)
        {
            if (DuelFXManager.Instance != null) 
            {
                DuelFXManager.Instance.SetCanAttackIndicator(card, false);
                DuelFXManager.Instance.SetCannotAttackIndicator(card, false);
                DuelFXManager.Instance.SetCannotChangePosIndicator(card, false);
            }
            return;
        }

        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        bool isBattlePhase = currentPhase == GamePhase.Battle;
        bool isFirstTurn = turnCount == 1;

        if (attackIndicatorMode == AttackIndicatorMode.HoverOnly)
        {
            if (isBattlePhase && !isFirstTurn)
            {
                bool isAttacker = CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentAttacker != null && CardEffectManager.Instance.luaDuel.currentAttacker.unityCard == card;
                bool canAttack = isPlayerTurn && card.position == CardDisplay.BattlePosition.Attack && !card.hasAttackedThisTurn && !isAttacker;
                
                // Verifica restrição global de ataque (ex: Array of Revealing Light)
                if (canAttack && CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null)
                {
                    LuaCard cachedCard = CardEffectManager.Instance.EnsureCardScriptLoaded(card) ?? new LuaCard(card);
                    if (CardEffectManager.Instance.auraManager.IsUnderRestriction(cachedCard, null, "CANNOT_ATTACK", CardLocation.Field))
                        canAttack = false;
                }
                
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(card, isHovering && canAttack);

                bool cannotAttack = !canAttack;
                if (card.position != CardDisplay.BattlePosition.Attack) cannotAttack = false;
                
                if (isAttacker) cannotAttack = false;
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCannotAttackIndicator(card, isHovering && cannotAttack);
            }
            else
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(card, false);
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCannotAttackIndicator(card, false);
            }
        }

        bool isMainPhase = currentPhase == GamePhase.Main1 || currentPhase == GamePhase.Main2;
        bool cannotChangePos = card.hasChangedPositionThisTurn || card.summonedTurnCount == turnCount || card.hasAttackedThisTurn;
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCannotChangePosIndicator(card, isHovering && cannotChangePos && isMainPhase);
    }

    /// <summary>
    /// Atualiza permanentemente a espadinha sobre todos os monstros que podem atacar
    /// se o modo AlwaysInBattlePhase estiver selecionado.
    /// OBS: Deve ser chamado ao final da rotina de ataque no Lua (`Core.Attack`) para apagar a espada do monstro que acabou de atacar!
    /// </summary>
    public void RefreshAttackIndicators()
    {
        if (duelFieldUI == null) return;
        
        bool isBusy = (CardEffectManager.Instance != null && (CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isChainResolving)) || isSelectingFromHand;

        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        bool isFirstTurn = turnCount == 1;
        bool showIndicators = !isBusy && attackIndicatorMode == AttackIndicatorMode.AlwaysInBattlePhase && currentPhase == GamePhase.Battle && isPlayerTurn && !isFirstTurn;

        // Debug.Log($"[RefreshAttackIndicators] Iniciando Varredura. Fase: {currentPhase} | Deve Mostrar: {showIndicators}");

        Transform[] zones = duelFieldUI.playerMonsterZones;
        for (int i = 0; i < zones.Length; i++)
        {
            Transform zone = zones[i];
            if (zone.childCount > 0)
            {
                // Double Check: Busca a carta em qualquer posição da zona (ignora Fantasmas que tomaram a Posição 0)
                CardDisplay card = zone.GetComponentInChildren<CardDisplay>();
                if (card == null)
                {
                    // Debug.Log($"[RefreshAttackIndicators] Zona {i + 1} possui filhos, mas nenhum CardDisplay (Encontrado Fantasma: {zone.GetChild(0).name}).");
                    continue;
                }
                
                if (!card.isFlipped && card.CurrentCardData.type.Contains("Monster"))
                {
                    if (showIndicators)
                    {
                        bool isBattlePhase = currentPhase == GamePhase.Battle;
                        if (isBattlePhase)
                        {
                        bool isAttacker = CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentAttacker != null && CardEffectManager.Instance.luaDuel.currentAttacker.unityCard == card;
                        bool canAttack = card.position == CardDisplay.BattlePosition.Attack && !card.hasAttackedThisTurn && !isAttacker;

                        // Verifica restrição global de ataque
                        if (canAttack && CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null)
                        {
                            LuaCard cachedCard = CardEffectManager.Instance.EnsureCardScriptLoaded(card) ?? new LuaCard(card);
                            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(cachedCard, null, "CANNOT_ATTACK", CardLocation.Field))
                                canAttack = false;
                        }
                                                        
                            // Debug.Log($"[RefreshAttackIndicators] {card.CurrentCardData.name} (Zona {i+1}) -> Em Ataque? {card.position == CardDisplay.BattlePosition.Attack} | Já atacou? {card.hasAttackedThisTurn} | Recebeu Espadinha? {canAttack}");
                            
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(card, canAttack);

                            bool cannotAttack = !canAttack;
                            if (card.position != CardDisplay.BattlePosition.Attack) cannotAttack = false;
                            
                        if (isAttacker) cannotAttack = false;
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCannotAttackIndicator(card, cannotAttack);
                        }
                        else
                        {
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCanAttackIndicator(card, false);
                            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetCannotAttackIndicator(card, false);
                        }
                    }
                    else
                    {
                        if (DuelFXManager.Instance != null)
                        {
                            DuelFXManager.Instance.SetCanAttackIndicator(card, false);
                            DuelFXManager.Instance.SetCannotAttackIndicator(card, false);
                        }
                    }
                }
            }
        }
    }
}
