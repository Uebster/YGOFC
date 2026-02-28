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

        currentChain.Add(link);
        Debug.Log($"Chain Link {link.linkNumber}: {card.currentCardData.name} ativado.");

        // Feedback Visual
        if (DuelFXManager.Instance != null)
        {
            // Poderíamos tocar um som de "Chain Link" aqui
        }

        // TODO: Aqui verificaríamos se o oponente quer responder (Trigger/Fast Effect)
        // Se SpellTrapManager encontrar uma resposta automática, ele adicionaria aqui.
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
            Debug.Log($"Resolvendo Chain Link {link.linkNumber}: {link.cardSource.currentCardData.name}");
            
            // Feedback Visual da Resolução
            if (link.cardSource != null)
            {
                // link.cardSource.Highlight(); // Exemplo
            }

            // --- EXECUÇÃO DO EFEITO ---
            // Aqui chamaria o EffectManager.Execute(link.effect)
            // Por enquanto, assumimos que o efeito "acontece" visualmente
            
            yield return new WaitForSeconds(0.5f); // Delay para visualização e impacto

            // --- LIMPEZA PÓS-RESOLUÇÃO ---
            // Regra: Magias/Armadilhas Normais, Rápidas e Rituais vão para o GY após resolver.
            // Contínuas, Campo, Equipamento e Pêndulo ficam no campo.
            if (link.cardSource != null)
            {
                CardData data = link.cardSource.currentCardData;
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
