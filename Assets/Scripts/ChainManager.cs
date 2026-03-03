using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChainManager : MonoBehaviour
{
    public static ChainManager Instance;

    [System.Serializable]
    public class ChainLink
    {
        public int linkNumber;
        public CardDisplay cardSource;
        public bool isPlayerEffect;
        public bool isNegated; // Novo: Se o efeito foi negado
        public CardDisplay target; // Novo: Alvo específico (se houver)
        // public EffectData effect; // Futuro: Lógica específica do efeito
    }

    public List<ChainLink> currentChain = new List<ChainLink>();
    public bool isChainResolving = false;

    void Awake()
    {
        Instance = this;
    }

    // Adiciona uma carta à corrente (Link 1, Link 2, etc.)
    public void AddToChain(CardDisplay card, bool isPlayer)
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
        link.isNegated = false;
        link.target = null; // Pode ser setado depois se o efeito tiver alvo

        currentChain.Add(link);
        Debug.Log($"Chain Link {link.linkNumber}: {card.CurrentCardData.name} ativado.");

        // Feedback Visual
        if (DuelFXManager.Instance != null)
        {
            // Poderíamos tocar um som de "Chain Link" aqui
        }

        // Verifica se o oponente quer responder (Janela de Resposta)
        CheckForResponse(!isPlayer);
    }

    // Verifica se o jogador alvo tem alguma carta que pode ser encadeada
    private void CheckForResponse(bool checkPlayer)
    {
        if (SpellTrapManager.Instance != null)
        {
            // Verifica se há alguma carta que pode responder ao último elo
            // Por enquanto, simplificado para verificar qualquer Trap/Quick-Play
            // Em um sistema completo, verificaria Spell Speed e condições de ativação
            
            ChainLink lastLink = GetLastChainLink();
            
            // Simula a verificação de resposta
            // Se for o oponente (IA), pode ter uma lógica simples de chance ou prioridade
            // Se for o jogador, abriria um popup "Deseja ativar uma carta?"
            
            // Para este protótipo, vamos apenas resolver a chain se não houver resposta imediata
            // ou se a resposta for automática (ex: Counter Trap obrigatória)
            
            // Exemplo de resposta automática (Counter Trap)
            if (SpellTrapManager.Instance.CheckChainResponse(lastLink))
            {
                // Se encontrou resposta, ela foi adicionada à chain dentro do CheckChainResponse
                // e o fluxo continua recursivamente
                return;
            }
        }

        // Se ninguém respondeu, resolve a chain
        ResolveChain();
    }

    // Retorna o último elo da corrente (para Counter Traps e efeitos de resposta)
    public ChainLink GetLastChainLink()
    {
        if (currentChain.Count > 0) return currentChain[currentChain.Count - 1];
        return null;
    }

    // Inicia a resolução da corrente
    public void ResolveChain()
    {
        if (currentChain.Count == 0 || isChainResolving) return;
        StartCoroutine(ResolveChainRoutine());
    }

    private IEnumerator ResolveChainRoutine()
    {
        isChainResolving = true;
        Debug.Log("Resolvendo Corrente...");

        // Resolve de trás para frente (LIFO - Last In, First Out)
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
                    CardEffectManager.Instance.ExecuteCardEffect(link.cardSource);
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

        currentChain.Clear();
        isChainResolving = false;
        Debug.Log("Corrente Resolvida.");
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
