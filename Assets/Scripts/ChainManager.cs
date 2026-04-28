using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChainManager
{
    public class ChainLink
    {
        public int chainIndex;
        public LuaEffect effect;
        public LuaCard card;
        public int player;
        public object triggerArgs;
        public bool isNegated = false;
        public bool isActivationNegated = false;
        public bool isDummy = false;
        public LuaGroup targetGroup;
        public int targetPlayer;
        public int targetParam;
    }

    public List<ChainLink> currentChain = new List<ChainLink>();
    public bool isChainResolving = false;
    public ChainLink resolvingLink = null;
    public int activeChainTasks = 0;

    private CardEffectManager core;

    public ChainManager(CardEffectManager coreManager)
    {
        this.core = coreManager;
    }

    public IEnumerator BuildAndResolveChainRoutine(LuaCard luaCard, LuaEffect effect, object triggerArgs, int tp, System.Action onComplete, bool isDummy = false)
    {
        activeChainTasks++;
        // NOVO: Se a corrente atual já está em resolução, os novos gatilhos (Ex: Destruição em Batalha, RaiseSingleEvent) 
        // devem aguardar para formarem uma NOVA corrente limpa logo em seguida (SEGOC Simplificado).
        if (isChainResolving)
        {
            Debug.Log($"[Chain] Gatilho pendente para {luaCard.unityData?.name}. Aguardando a corrente atual finalizar...");
            yield return new WaitWhile(() => isChainResolving);
        }

        // FASE DE ATIVAÇÃO (Custo e Alvo) [chk=1]
        core.luaDuel.currentActivatingEffect = effect;
        core.luaDuel.currentTargetGroup = new LuaGroup(); // Limpa os alvos da ativação anterior
        core.luaDuel.targetParam = 0;
        core.luaDuel.targetPlayer = 0;
        if (effect.costFunc != null) {
            yield return core.StartCoroutine(core.RunLuaCoroutine(effect.costFunc, effect, tp, triggerArgs, 1));
            if (!core.lastCoroutineSuccess) { AbortActivation(luaCard); activeChainTasks--; onComplete?.Invoke(); yield break; }
        }
        if (effect.targetFunc != null) {
            Debug.Log($"[Surgical Log] Chamando targetFunc (chk=1) para {luaCard.unityData.name}");
            yield return core.StartCoroutine(core.RunLuaCoroutine(effect.targetFunc, effect, tp, triggerArgs, 1));
            Debug.Log($"[Surgical Log] Sucesso da targetFunc: {core.lastCoroutineSuccess}");
            if (!core.lastCoroutineSuccess) { AbortActivation(luaCard); activeChainTasks--; onComplete?.Invoke(); yield break; }
        }
        core.luaDuel.currentActivatingEffect = null;

        // ADICIONA À CORRENTE LIFO
        ChainLink newLink = new ChainLink { 
            chainIndex = currentChain.Count + 1, 
            effect = effect, 
            card = luaCard, 
            player = tp, 
            triggerArgs = triggerArgs,
            isDummy = isDummy,
            targetGroup = core.luaDuel.currentTargetGroup, // Salva o estado exato da seleção nesta cápsula!
            targetPlayer = core.luaDuel.targetPlayer,
            targetParam = core.luaDuel.targetParam
        };
        
        // Registra o uso ("Once per turn")
        if (effect.countLimitMax > 0)
        {
            if (effect.countLimitCode == 0) effect.currentUsages++;
            else
            {
                string key = $"{tp}_{effect.countLimitCode}";
                if (!core.luaDuel.hardOncePerTurnUsages.ContainsKey(key)) core.luaDuel.hardOncePerTurnUsages[key] = 0;
                core.luaDuel.hardOncePerTurnUsages[key]++;
            }
        }
        
        currentChain.Add(newLink);
        
        int visualLinkNumber = newLink.chainIndex;
        // Subtrai 1 se o Link 1 for um evento Dummy invisível (Gatilho da Engine)
        if (currentChain.Count > 0 && currentChain[0].isDummy)
        {
            visualLinkNumber -= 1;
        }

        // Feedback Visual de Corrente
        if (visualLinkNumber > 0 && !isDummy && DuelFXManager.Instance != null && luaCard.unityData != null)
        {
            Debug.Log($"[Chain] Link {visualLinkNumber}: {luaCard.unityData.name} adicionado à pilha.");
            
            CardDisplay sourceDisplay = luaCard.unityCard;
            if (sourceDisplay == null || !sourceDisplay.gameObject.activeInHierarchy)
            {
                sourceDisplay = core.luaDuel.FindCardDisplayInPiles(luaCard.unityData);
            }

            // SE A CARTA ATIVAR SEU EFEITO LÁ DO FUNDO DO CEMITÉRIO/DECK, DÁ O HIGHLIGHT!
            if (sourceDisplay != null && sourceDisplay.isInPile && !GameManager.Instance.isSimulating)
            {
                yield return core.StartCoroutine(core.luaDuel.AnimateCardActivationInPileRoutine(sourceDisplay));
            }
            else if (sourceDisplay != null && sourceDisplay.isOnField && !GameManager.Instance.isSimulating)
            {
                // Animação de ativação para cartas já no campo que ativam efeitos (Ex: Trap Contínua ativando efeito Trigger)
                if (effect != null && effect.type != 0x0010) // Ignora a ativação inicial (0x0010) pois já foi animada
                {
                    bool isTrap = luaCard.unityData.type.Contains("Trap");
                    DuelFXManager.Instance.PlayCardActivation(sourceDisplay, isTrap, () => {});
                }
            }
            
            if (sourceDisplay != null)
            {
                // Se estamos criando o Link 2, a corrente acabou de se tornar múltipla (Corrente Surpresa!).
                // Mostramos retroativamente o texto do Link 1 para manter o visual limpo caso ninguém responda!
                if (visualLinkNumber == 2)
                {
                    int targetChainIndex = (currentChain.Count > 0 && currentChain[0].isDummy) ? 2 : 1;
                    var link1 = currentChain.Find(l => l.chainIndex == targetChainIndex);
                    if (link1 != null && !link1.isDummy && link1.card != null)
                    {
                        CardDisplay link1Display = link1.card.unityCard;
                        if (link1Display == null || !link1Display.gameObject.activeInHierarchy)
                            link1Display = core.luaDuel.FindCardDisplayInPiles(link1.card.unityData);
                        
                        if (link1Display != null)
                            DuelFXManager.Instance.PlayChainLinkEffect(link1Display, 1);
                    }
                }
                
                if (visualLinkNumber > 1) 
                    DuelFXManager.Instance.PlayChainLinkEffect(sourceDisplay, visualLinkNumber);
            }
        }

        // JANELA DE RESPOSTA (Speed 2/3 - Pergunta ao Oponente e depois ao Jogador)
        bool someoneResponded = false;
        yield return core.StartCoroutine(ResponseWindowRoutine(1 - tp, newLink, (res) => someoneResponded = res));

        if (!someoneResponded)
        {
            yield return core.StartCoroutine(ResponseWindowRoutine(tp, newLink, (res) => someoneResponded = res));
        }

        // RESOLUÇÃO LIFO (De trás pra frente) - Apenas o Link 1 comanda o desempilhamento!
        if (newLink.chainIndex == 1)
        {
            isChainResolving = true;
            Debug.Log($"[Chain] Iniciando Resolução da Corrente com {currentChain.Count} elos...");

            for (int i = currentChain.Count - 1; i >= 0; i--)
            {
                ChainLink link = currentChain[i];
                resolvingLink = link; // Avisa o C# qual cápsula está sendo aberta agora
                if (link.isDummy) continue; // Pula a execução física do dummy (apenas ancora a corrente)

                // Dispara EVENT_CHAIN_SOLVING (1019) para permitir que Jinzo e afins neguem a resolução!
                yield return core.StartCoroutine(core.eventManager.ProcessTriggersRoutine(1019, link.card));

                if (link.isActivationNegated || link.isNegated)
                {
                    Debug.Log($"[Chain] Link {link.chainIndex} ({link.card.unityData.name}): Negado (isActivationNegated={link.isActivationNegated}, isNegated={link.isNegated}).");
                    CleanupSpellTrapAfterResolution(link.card);
                    continue;
                }

                Debug.Log($"[Chain] Resolvendo Link {link.chainIndex}: {link.card.unityData.name}...");
                if (link.effect.operationFunc != null) {
                    Debug.Log($"[Surgical Log] Chamando operationFunc para {link.card.unityData.name}");
                    yield return core.StartCoroutine(core.RunLuaCoroutine(link.effect.operationFunc, link.effect, link.player, link.triggerArgs, -1));
                }

                CleanupSpellTrapAfterResolution(link.card);
            }

            currentChain.Clear();
            isChainResolving = false;
            resolvingLink = null;
            Debug.Log($"[Chain] Corrente resolvida e limpa com sucesso!");
        }

        activeChainTasks--;
        onComplete?.Invoke();
    }

    private IEnumerator ResponseWindowRoutine(int priorityPlayer, ChainLink triggerLink, System.Action<bool> onComplete)
    {
        bool isHuman = (priorityPlayer == 0); // 0 = Player, 1 = Opponent(IA)
        int eventCode = triggerLink != null && triggerLink.effect != null ? triggerLink.effect.code : 0;
        List<CardDisplay> validResponses = core.GetValidResponses(priorityPlayer, triggerLink, eventCode);

        if (validResponses.Count == 0) { onComplete?.Invoke(false); yield break; }

        bool decisionMade = false;
        CardDisplay chosenCard = null;

        if (isHuman && UIManager.Instance != null && (!GameManager.Instance.isSimulating))
        {
            bool autoPass = false;
            // Se o gatilho for uma carta do próprio jogador durante o turno dele, não perguntar se quer acorrentar (evita spam de janelas na Main Phase)
            if (GameManager.Instance.isPlayerTurn && triggerLink != null && triggerLink.player == 0 && !triggerLink.isDummy)
            {
                autoPass = true;
            }

            if (autoPass)
            {
                decisionMade = true;
            }
            else
            {
                UIManager.Instance.ShowResponseWindow(validResponses, (selectedCard) => {
                    chosenCard = selectedCard;
                    decisionMade = true;
                }, () => {
                    decisionMade = true;
                });
            }
            while (!decisionMade) yield return null;
        }
        else if (!isHuman && OpponentAI.Instance != null && (!GameManager.Instance.isSimulating))
        {
            yield return new WaitForSeconds(0.8f); // Delay para a IA "pensar" durante as interrupções, dando tempo das animações prévias assentarem
            chosenCard = OpponentAI.Instance.ChooseBestResponse(validResponses, triggerLink);
            decisionMade = true;
        }
        else decisionMade = true;

        if (chosenCard != null)
        {
            if (chosenCard.isOnField && chosenCard.isFlipped && (chosenCard.CurrentCardData.type.Contains("Spell") || chosenCard.CurrentCardData.type.Contains("Trap")))
            {
                chosenCard.ShowFront(); 
            }
            else if (!chosenCard.isOnField && (chosenCard.CurrentCardData.type.Contains("Spell") || chosenCard.CurrentCardData.type.Contains("Trap")))
            {
                // Move a Magia Rápida ou Armadilha da mão para o campo antes de ativar na corrente
                Transform targetZone = GameManager.Instance.GetFreeSpellZone(chosenCard.isPlayerCard);
                if (targetZone != null)
                {
                    if (chosenCard.isPlayerCard) GameManager.Instance.playerHand.Remove(chosenCard.gameObject);
                    else GameManager.Instance.opponentHand.Remove(chosenCard.gameObject);

                    chosenCard.transform.SetParent(targetZone);
                    chosenCard.transform.localPosition = Vector3.zero;
                    chosenCard.transform.localScale = GameManager.Instance.fieldCardScale;
                    chosenCard.transform.localRotation = Quaternion.Euler(0, 0, chosenCard.isPlayerCard ? 0f : 180f);
                    chosenCard.isOnField = true;
                    chosenCard.isInteractable = false;
                    chosenCard.ShowFront();
                }
            }
                
            bool childChainEnded = false;
            // Se for um evento Dummy (Engine), passamos o argumento original (o monstro invocado). 
            // Se for uma resposta a uma ativação real, passamos a carta que ativou.
            object argsToPass = triggerLink.isDummy ? triggerLink.triggerArgs : triggerLink.card;
            core.ActivateCard(chosenCard, argsToPass, () => { childChainEnded = true; });
            while (!childChainEnded) yield return null;
            
            onComplete?.Invoke(true);
        }
        else
        {
            onComplete?.Invoke(false);
        }
    }

    public bool NegateChainLink(int chainIndex)
    {
        if (chainIndex == 0 && resolvingLink != null)
        {
            resolvingLink.isNegated = true;
            return true;
        }

        var link = currentChain.Find(l => l.chainIndex == chainIndex);
        if (link != null) { link.isActivationNegated = true; return true; }
        return false;
    }

    private void CleanupSpellTrapAfterResolution(LuaCard luaCard)
    {
        if (luaCard == null || luaCard.unityCard == null || !luaCard.unityCard.isOnField) return;

        if (!luaCard.unityData.type.Contains("Spell") && !luaCard.unityData.type.Contains("Trap")) return;

        string prop = (luaCard.unityData.property ?? "").ToLower();
        string type = (luaCard.unityData.type ?? "").ToLower();

        bool isContinuous = type.Contains("continuous") || prop.Contains("continuous");
        bool isEquip = type.Contains("equip") || prop.Contains("equip");
        bool isField = type.Contains("field") || prop.Contains("field");
        bool hasRemainField = luaCard.registeredEffects.Exists(e => e.code == 17); // 17 = EFFECT_REMAIN_FIELD

        if (!isContinuous && !isEquip && !isField && !hasRemainField)
        {
            Debug.Log($"[ChainManager] Limpando Mágica/Armadilha normal após uso: {luaCard.unityData.name}");
            GameManager.Instance.MoveCard(luaCard.unityCard, CardLocation.Graveyard, SendReason.Rule);
        }
    }

    private void AbortActivation(LuaCard luaCard)
    {
        if (luaCard != null && luaCard.unityCard != null && luaCard.unityCard.isOnField)
        {
            if (luaCard.unityData.type.Contains("Spell") || luaCard.unityData.type.Contains("Trap"))
            {
                Debug.Log($"[ChainManager] Ativação cancelada/abortada. Destruindo carta mágica: {luaCard.unityData.name}");
                GameManager.Instance.MoveCard(luaCard.unityCard, CardLocation.Graveyard, SendReason.Rule);
            }
        }
    }
}