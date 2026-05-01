using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChainManager
{
    public class OperationInfo
    {
        public int category;
        public LuaGroup targetGroup;
        public int count;
        public int player;
        public int param;
    }

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
        public LuaEffect disableReason;
        public int disablePlayer;
        public Dictionary<int, OperationInfo> opInfo = new Dictionary<int, OperationInfo>();
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
        core.luaDuel.ClearOperationInfo();
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
            targetParam = core.luaDuel.targetParam,
            opInfo = new Dictionary<int, OperationInfo>(core.luaDuel._opInfoCache)
        };
        
        // Registra o uso ("Once per turn")
        if (effect.hasCountLimit || effect.countLimitMax > 0)
        {
            if (effect.countLimitCode == 0) effect.currentUsages++;
            else
            {
                string key = $"{tp}_{effect.countLimitCode}";
                if (!core.luaDuel.hardOncePerTurnUsages.ContainsKey(key)) core.luaDuel.hardOncePerTurnUsages[key] = 0;
                core.luaDuel.hardOncePerTurnUsages[key]++;
            }
        }
        
        // Registra o uso ("Once per duel")
        if (effect.isOath)
        {
            string oathKey = $"{tp}_{effect.code}_OATH";
            if (!core.luaDuel.oathUsages.ContainsKey(oathKey)) core.luaDuel.oathUsages[oathKey] = 0;
            core.luaDuel.oathUsages[oathKey]++;
        }
                
        // --- EFFECT_FLAG_CARD_TARGET ---
        // Se o efeito exige alvo e o jogador/IA os escolheu com sucesso, avisa o mundo que essas cartas viraram alvo!
        if (effect.isCardTarget && core.luaDuel.currentTargetGroup != null && core.luaDuel.currentTargetGroup.cards.Count > 0)
        {
            Debug.Log($"<color=cyan>[Targeting]</color> {luaCard.unityData.name} marcou {core.luaDuel.currentTargetGroup.cards.Count} carta(s) como alvo!");
            EventData edTarget = new EventData(core.luaDuel.currentTargetGroup, tp, 0, effect, 0, tp);
            core.eventManager.TriggerLuaEvent(1028, edTarget); // 1028 = EVENT_BECOME_TARGET
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

        EventData edChaining = new EventData(newLink.card, tp, newLink.chainIndex, effect, 0, tp);
        if (!isDummy) core.eventManager.TriggerLuaEvent(1027, edChaining); // 1027 = EVENT_CHAINING

        // JANELA DE RESPOSTA (Speed 2/3 - Pergunta ao Oponente e depois ao Jogador)
        bool someoneResponded = false;
        yield return core.StartCoroutine(ResponseWindowRoutine(1 - tp, newLink, edChaining, (res) => someoneResponded = res));

        if (!someoneResponded)
        {
            yield return core.StartCoroutine(ResponseWindowRoutine(tp, newLink, edChaining, (res) => someoneResponded = res));
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

            // Dispara EVENT_CHAIN_END e a janela TIMING_CHAIN_END
            if (!isDummy)
            {
                core.eventManager.TriggerLuaEvent(1026, null); // 1026 = EVENT_CHAIN_END
                core.StartCoroutine(core.OpenFastEffectWindow("Fim da Corrente", 1026, null, 0x8000)); // 0x8000 = TIMING_CHAIN_END
            }
            
            core.CleanChainExpiredModifiers();
        }

        activeChainTasks--;
        onComplete?.Invoke();
    }

    private IEnumerator ResponseWindowRoutine(int priorityPlayer, ChainLink triggerLink, EventData edChaining, System.Action<bool> onComplete)
    {
        bool isHuman = (priorityPlayer == 0); // 0 = Player, 1 = Opponent(IA)
        int eventCode = 1027; // EVENT_CHAINING
        List<CardDisplay> validResponses = core.GetValidResponses(priorityPlayer, triggerLink, eventCode, edChaining);

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

    public bool NegateChainLink(int chainIndex, bool isActivation = true, LuaEffect reasonEffect = null, int reasonPlayer = 0)
    {
        if (chainIndex == 0 && resolvingLink != null)
        {
            if (resolvingLink.effect != null)
            {
                if (isActivation && (resolvingLink.effect.cannotNegate || resolvingLink.effect.cannotInactivate)) { Debug.Log($"<color=green>[ChainManager]</color> Negação falhou! {resolvingLink.card?.unityData?.name} possui PROTEÇÃO DE ATIVAÇÃO."); return false; }
                if (!isActivation && resolvingLink.effect.cannotDisable) { Debug.Log($"<color=green>[ChainManager]</color> Negação falhou! {resolvingLink.card?.unityData?.name} possui EFFECT_FLAG_CANNOT_DISABLE."); return false; }
            }

            if (isActivation) resolvingLink.isActivationNegated = true;
            else resolvingLink.isNegated = true;

            resolvingLink.disableReason = reasonEffect;
            resolvingLink.disablePlayer = reasonPlayer;
            if (resolvingLink.card != null && resolvingLink.card.unityCard != null) resolvingLink.card.unityCard.AddStatus(0x40000); // STATUS_ACTIVATE_DISABLED
            return true;
        }

        var link = currentChain.Find(l => l.chainIndex == chainIndex);
        if (link != null) { 
            if (link.effect != null)
            {
                if (isActivation && (link.effect.cannotNegate || link.effect.cannotInactivate)) { Debug.Log($"<color=green>[ChainManager]</color> Negação falhou! {link.card?.unityData?.name} possui PROTEÇÃO DE ATIVAÇÃO."); return false; }
                if (!isActivation && link.effect.cannotDisable) { Debug.Log($"<color=green>[ChainManager]</color> Negação falhou! {link.card?.unityData?.name} possui EFFECT_FLAG_CANNOT_DISABLE."); return false; }
            }

            if (isActivation) link.isActivationNegated = true;
            else link.isNegated = true;
            
            link.disableReason = reasonEffect;
            link.disablePlayer = reasonPlayer;
            if (link.card != null && link.card.unityCard != null) link.card.unityCard.AddStatus(0x40000); // STATUS_ACTIVATE_DISABLED
            return true; 
        }
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
            GameManager.Instance.MoveCard(luaCard.unityCard, CardLocation.Graveyard, 0x400); // REASON_RULE
        }
    }

    private void AbortActivation(LuaCard luaCard)
    {
        if (luaCard != null && luaCard.unityCard != null && luaCard.unityCard.isOnField)
        {
            if (luaCard.unityData.type.Contains("Spell") || luaCard.unityData.type.Contains("Trap"))
            {
                Debug.Log($"[ChainManager] Ativação cancelada/abortada. Destruindo carta mágica: {luaCard.unityData.name}");
                GameManager.Instance.MoveCard(luaCard.unityCard, CardLocation.Graveyard, 0x400); // REASON_RULE
            }
        }
    }
}