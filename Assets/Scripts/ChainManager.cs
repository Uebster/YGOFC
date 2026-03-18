using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ChainManager : MonoBehaviour
{
    public static ChainManager Instance;

    public enum TriggerType { CardActivation, Summon, Attack, Flip, Effect }

    [System.Serializable]
    public class ChainLink
    {
        public int linkNumber;
        public CardDisplay cardSource;
        public bool isPlayerEffect;
        public TriggerType trigger;
        public int spellSpeed;
        public bool isNegated; // Novo: Se o efeito foi negado
        public CardDisplay target; // Novo: Alvo específico (se houver)
        // public EffectData effect; // Futuro: Lógica específica do efeito
    }

    public List<ChainLink> currentChain = new List<ChainLink>();
    public bool isChainResolving = false;
    private bool playerPassed;
    private bool opponentPassed;

    void Awake()
    {
        Instance = this;
    }

    // Adiciona uma carta à corrente (Link 1, Link 2, etc.)
    public void AddToChain(CardDisplay card, bool isPlayer, TriggerType triggerType = TriggerType.CardActivation, CardDisplay targetCard = null, System.Action onChainResolved = null)
    {
        if (isChainResolving)
        {
            Debug.LogWarning("Não é possível adicionar à corrente enquanto ela está resolvendo.");
            return;
        }

        ChainLink link = new ChainLink();
        link.linkNumber = currentChain.Count + 1;
        link.cardSource = card;
        link.isPlayerEffect = isPlayer;
        link.trigger = triggerType;
        link.spellSpeed = GetSpellSpeed(card.CurrentCardData);
        link.target = targetCard;
        link.isNegated = false;

        currentChain.Add(link);
        Debug.Log($"Chain Link {link.linkNumber}: {card.CurrentCardData.name} ativado.");

        // Feedback Visual
        if (DuelFXManager.Instance != null)
        {
            // Poderíamos tocar um som de "Chain Link" aqui
        }

        // Inicia o processo de resposta
        if (currentChain.Count == 1)
        {
            StartCoroutine(ResponseRoutine(onChainResolved));
        }
    }

    private IEnumerator ResponseRoutine(System.Action onChainResolved)
    {
        // A prioridade para responder ao último elo é do oponente de quem o ativou.
        bool currentPlayerIsPlayer = !GetLastChainLink().isPlayerEffect;

        while (true)
        {
            playerPassed = false;
            opponentPassed = false;
            int chainCountAtStart = currentChain.Count; // FIX: Rastreia tamanho da chain para detectar mudanças

            // Loop de prioridade para 2 jogadores
            for (int i = 0; i < 2; i++)
            {
                List<CardDisplay> responses = new List<CardDisplay>();
                try
                {
                    responses = SpellTrapManager.Instance.GetValidResponses(GetLastChainLink(), currentPlayerIsPlayer);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ChainManager] Erro ao buscar respostas: {e.Message}");
                    // Em caso de erro, assume sem resposta para não travar
                    responses.Clear();
                }

                if (responses.Count > 0)
                {
                    if (currentPlayerIsPlayer)
                    {
                        Debug.Log($"[ChainManager] Aguardando resposta do Jogador. {responses.Count} opções.");
                        // Espera a resposta do jogador humano
                        try 
                        {
                            UIManager.Instance.ShowResponseWindow(responses, () => { playerPassed = true; });
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[ChainManager] Erro ao mostrar UI: {e.Message}");
                            playerPassed = true;
                        }
                        
                        // FIX: Adicionado timeout para evitar travamento infinito se a UI falhar
                        float timeout = 5f; // Reduzido para 5s para feedback mais rápido em caso de erro
                        float timer = 0f;
                        // Adicionado verificação de responses.Count > 0 para evitar loop se a lista for limpa externamente
                        // FIX: Verifica se a chain cresceu (jogador ativou algo) para sair do loop
                        while (!playerPassed && responses.Count > 0 && timer < timeout && currentChain.Count == chainCountAtStart)
                        {
                            timer += Time.unscaledDeltaTime; // Usa unscaled para não travar se o jogo pausar
                            yield return null;
                        }
                        
                        if (currentChain.Count > chainCountAtStart)
                        {
                             Debug.Log("[ChainManager] Nova carta adicionada à corrente. Interrompendo espera.");
                        }
                        else if (timer >= timeout)
                        {
                            Debug.LogWarning("ChainManager: Timeout aguardando resposta do jogador. Passando automaticamente.");
                            playerPassed = true;
                        }
                    }
                    else
                    {
                        // --- INTELIGÊNCIA ARTIFICIAL: AVALIANDO RESPOSTA ---
                        yield return new WaitForSeconds(1.2f); // Simula o tempo de pensar da IA
                        
                        CardDisplay aiChoice = null;
                        if (OpponentAI.Instance != null)
                        {
                            aiChoice = OpponentAI.Instance.ChooseBestResponse(responses, GetLastChainLink());
                        }

                        if (aiChoice != null)
                        {
                            Debug.Log($"[ChainManager] IA decidiu responder com a armadilha/efeito rápido: {aiChoice.CurrentCardData.name}");
                            if (aiChoice.CurrentCardData.type.Contains("Monster")) {
                                // Se no futuro a IA tiver Quick Effects de Monstros
                                ChainManager.Instance.AddToChain(aiChoice, aiChoice.isPlayerCard, ChainManager.TriggerType.Effect);
                            } else {
                                GameManager.Instance.ActivateFieldSpellTrap(aiChoice.gameObject);
                            }
                        }
                        else
                        {
                            Debug.Log("[ChainManager] IA avaliou a situação e decidiu não gastar recursos (Passou).");
                            opponentPassed = true;
                        }
                    }
                }
                else
                {
                    if (currentPlayerIsPlayer) playerPassed = true;
                    else opponentPassed = true;
                }

                // Se uma nova carta foi adicionada à chain, o loop de prioridade reinicia para o outro jogador
                // FIX: Usa chainCountAtStart em vez de responses.Count para evitar loop infinito
                if (currentChain.Count > chainCountAtStart)
                {
                    Debug.Log($"[ChainManager] Chain cresceu ({chainCountAtStart} -> {currentChain.Count}). Reiniciando prioridade.");
                    currentPlayerIsPlayer = !GetLastChainLink().isPlayerEffect;
                    chainCountAtStart = currentChain.Count; // Atualiza para a nova iteração
                    i = -1; // Reinicia o loop for
                    continue;
                }

                // Passa a prioridade para o outro jogador
                currentPlayerIsPlayer = !currentPlayerIsPlayer;
            }

            // Se ambos os jogadores passaram em sequência, a chain para de construir
            if (playerPassed && opponentPassed)
            {
                break;
            }
        }

        ResolveChain(onChainResolved);
    }

    public int GetSpellSpeed(CardData card)
    {
        if (card == null) return 0;
        if (card.type.Contains("Counter Trap")) return 3;
        if (card.type.Contains("Quick-Play Spell")) return 2;
        if (card.type.Contains("Trap")) return 2;
        // Efeitos rápidos de monstros (Quick Effects) seriam 2
        // Todo o resto (Normal Spells, etc.) é 1
        return 1;
    }

    // Retorna o último elo da corrente (para Counter Traps e efeitos de resposta)
    public ChainLink GetLastChainLink()
    {
        if (currentChain.Count > 0) return currentChain[currentChain.Count - 1];
        return null;
    }

    // Inicia a resolução da corrente
    public void ResolveChain(System.Action onChainResolved = null)
    {
        if (currentChain.Count == 0 || isChainResolving) return;
        StartCoroutine(ResolveChainRoutine(onChainResolved));
    }

    private IEnumerator ResolveChainRoutine(System.Action onChainResolved)
    {
        isChainResolving = true;
        Debug.Log("Resolvendo Corrente...");

        // Resolve de trás para frente (LIFO - Last In, First Out)
        try
        {
            for (int i = currentChain.Count - 1; i >= 0; i--)
            {
                ChainLink link = currentChain[i];
                Debug.Log($"Resolvendo Chain Link {link.linkNumber}: {link.cardSource.CurrentCardData.name}");
                
                if (link.isNegated)
                {
                    Debug.Log($"Chain Link {link.linkNumber} foi NEGADO e não resolverá.");
                }
                else
                {
                    // Feedback Visual da Resolução
                    if (link.cardSource != null)
                    {
                        // link.cardSource.Highlight(); // Exemplo
                    }

                    // --- EXECUÇÃO DO EFEITO ---
                    if (CardEffectManager.Instance != null)
                    {
                        try
                        {
                            CardEffectManager.Instance.ExecuteCardEffect(link.cardSource);
                            
                            // Hook para Counter Traps (Van'Dalgyon)
                            if (link.cardSource.CurrentCardData.type.Contains("Counter Trap"))
                            {
                                CardEffectManager.Instance.OnCounterTrapResolved(link.cardSource);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Erro ao executar efeito de {link.cardSource.CurrentCardData.name}: {e.Message}\n{e.StackTrace}");
                        }
                    }
                }

                yield return new WaitForSeconds(0.5f); // Delay para visualização e impacto

                // --- LIMPEZA PÓS-RESOLUÇÃO ---
                // Regra: Magias/Armadilhas Normais, Rápidas e Rituais vão para o GY após resolver.
                // Contínuas, Campo, Equipamento e Pêndulo ficam no campo.
                if (link.cardSource != null)
                {
                    CardData data = link.cardSource.CurrentCardData;
                    bool staysOnField = IsContinuousType(data);
                    
                    if (!staysOnField)
                    {
                        GameManager.Instance.SendToGraveyard(data, link.isPlayerEffect);
                        Destroy(link.cardSource.gameObject);
                    }
                }
            }
        }
        finally
        {
            currentChain.Clear();
            isChainResolving = false;
            Debug.Log("Corrente Resolvida (Finalizado).");

            // Se o gatilho original não foi negado, executa a ação pós-corrente (ex: o ataque continua)
            if (onChainResolved != null)
            {
                onChainResolved.Invoke();
            }
        }
    }

    // Método para negar um elo específico da corrente
    public void NegateLink(int linkIndex)
    {
        // O índice é baseado em 1 (Link 1, Link 2...)
        // Mas a lista é 0-based.
        if (linkIndex > 0 && linkIndex <= currentChain.Count)
        {
            currentChain[linkIndex - 1].isNegated = true;
        }
    }

    private bool IsContinuousType(CardData card)
    {
        if (card == null) return false;
        
        // Monstros permanecem (efeitos de monstro na chain)
        if (card.type.Contains("Monster")) return true;

        // Verifica propriedade (ícone)
        if (card.property == "Continuous") return true;
        if (card.property == "Field") return true;
        if (card.property == "Equip") return true;
        
        return false;
    }
}
