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
    }

    public List<ChainLink> currentChain = new List<ChainLink>();
    public bool isChainResolving = false;

    private CardEffectManager core;

    public ChainManager(CardEffectManager coreManager)
    {
        this.core = coreManager;
    }

    public IEnumerator BuildAndResolveChainRoutine(LuaCard luaCard, LuaEffect effect, object triggerArgs, int tp, System.Action onComplete)
    {
        // NOVO: Se a corrente atual já está em resolução, os novos gatilhos (Ex: Destruição em Batalha, RaiseSingleEvent) 
        // devem aguardar para formarem uma NOVA corrente limpa logo em seguida (SEGOC Simplificado).
        if (isChainResolving)
        {
            Debug.Log($"[Chain] Gatilho pendente para {luaCard.unityData?.name}. Aguardando a corrente atual finalizar...");
            yield return new WaitWhile(() => isChainResolving);
        }

        // FASE DE ATIVAÇÃO (Custo e Alvo) [chk=1]
        core.luaDuel.currentActivatingEffect = effect;
        if (effect.costFunc != null) {
            yield return core.StartCoroutine(core.RunLuaCoroutine(effect.costFunc, effect, tp, triggerArgs, 1));
            if (!core.lastCoroutineSuccess) { onComplete?.Invoke(); yield break; }
        }
        if (effect.targetFunc != null) {
            Debug.Log($"[Surgical Log] Chamando targetFunc (chk=1) para {luaCard.unityData.name}");
            yield return core.StartCoroutine(core.RunLuaCoroutine(effect.targetFunc, effect, tp, triggerArgs, 1));
            Debug.Log($"[Surgical Log] Sucesso da targetFunc: {core.lastCoroutineSuccess}");
            if (!core.lastCoroutineSuccess) { onComplete?.Invoke(); yield break; }
        }
        core.luaDuel.currentActivatingEffect = null;

        // ADICIONA À CORRENTE LIFO
        ChainLink newLink = new ChainLink { 
            chainIndex = currentChain.Count + 1, 
            effect = effect, 
            card = luaCard, 
            player = tp, 
            triggerArgs = triggerArgs 
        };
        currentChain.Add(newLink);
        Debug.Log($"[Chain] Link {newLink.chainIndex}: {luaCard.unityData.name} adicionado à pilha.");

        // Feedback Visual de Corrente
        if (DuelFXManager.Instance != null && luaCard.unityCard != null)
            DuelFXManager.Instance.PlayChainLinkEffect(luaCard.unityCard, newLink.chainIndex);

        // JANELA DE RESPOSTA (Speed 2/3 - Pergunta ao Oponente)
        yield return core.StartCoroutine(ResponseWindowRoutine(1 - tp, newLink));

        // RESOLUÇÃO LIFO (De trás pra frente) - Apenas o Link 1 comanda o desempilhamento!
        if (newLink.chainIndex == 1)
        {
            isChainResolving = true;
            Debug.Log($"[Chain] Iniciando Resolução da Corrente com {currentChain.Count} elos...");

            for (int i = currentChain.Count - 1; i >= 0; i--)
            {
                ChainLink link = currentChain[i];
                if (link.isActivationNegated)
                {
                    Debug.Log($"[Chain] Link {link.chainIndex} ({link.card.unityData.name}): Ativação Negada.");
                    continue;
                }
                if (link.isNegated)
                {
                    Debug.Log($"[Chain] Link {link.chainIndex} ({link.card.unityData.name}): Efeito Negado.");
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
            Debug.Log($"[Chain] Corrente resolvida e limpa com sucesso!");
        }

        onComplete?.Invoke();
    }

    private IEnumerator ResponseWindowRoutine(int priorityPlayer, ChainLink triggerLink)
    {
        bool isHuman = (priorityPlayer == 0); // 0 = Player, 1 = Opponent(IA)
        List<CardDisplay> validResponses = core.GetValidResponses(priorityPlayer, triggerLink);

        if (validResponses.Count == 0) yield break; // Ninguém pode responder, a corrente avança

        bool decisionMade = false;
        CardDisplay chosenCard = null;

        if (isHuman && UIManager.Instance != null && (!GameManager.Instance.isSimulating))
        {
            UIManager.Instance.ShowResponseWindow(validResponses, (selectedCard) => {
                chosenCard = selectedCard;
                decisionMade = true;
            }, () => {
                decisionMade = true; // Passou
            });
            while (!decisionMade) yield return null;
        }
        else if (!isHuman && OpponentAI.Instance != null && (!GameManager.Instance.isSimulating))
        {
            decisionMade = true;
        }
        else decisionMade = true;

        if (chosenCard != null)
        {
            if (chosenCard.isFlipped == false && (chosenCard.CurrentCardData.type.Contains("Spell") || chosenCard.CurrentCardData.type.Contains("Trap")))
                chosenCard.ShowFront(); 
                
            bool childChainEnded = false;
            core.ActivateCard(chosenCard, triggerLink.card, () => { childChainEnded = true; });
            while (!childChainEnded) yield return null;
        }
    }

    public bool NegateChainLink(int chainIndex)
    {
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
            GameManager.Instance.SendToGraveyard(luaCard.unityData, luaCard.unityCard.isPlayerCard, CardLocation.Field, SendReason.Rule);
            GameObject.Destroy(luaCard.unityCard.gameObject);
        }
    }
}