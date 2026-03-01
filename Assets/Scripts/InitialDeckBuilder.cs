using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InitialDeckBuilder : MonoBehaviour
{
    public static InitialDeckBuilder Instance;

    [Header("Configuração do Deck Inicial")]
    public int deckSize = 40;

    [System.Serializable]
    public class CardPoolDefinition
    {
        public string poolName;
        public int quantity;
        [Header("Filtros")]
        public bool isMonster;
        public bool isSpell;
        public bool isTrap;
        [Tooltip("Apenas para Monstros")]
        public bool allowEffectMonsters = false;
        public int minAtk = 0;
        public int maxAtk = 9999;
        public int maxLevel = 4;
        [Tooltip("IDs específicos para banir (ex: Exodia, Raigeki)")]
        public List<string> forbiddenIds = new List<string>();
        [Tooltip("IDs específicos que DEVEM estar neste pool se possível")]
        public List<string> requiredIds = new List<string>();
    }

    public List<CardPoolDefinition> deckStructure = new List<CardPoolDefinition>();

    void Awake()
    {
        Instance = this;
    }

    // Configuração Padrão (Hardcoded para garantir que funcione se não configurar no Inspector)
    private void SetupDefaultStructure()
    {
        if (deckStructure.Count > 0) return;

        // 1. Monstros Fracos (Fodder) - 15 cartas
        deckStructure.Add(new CardPoolDefinition {
            poolName = "Weak Monsters",
            quantity = 15,
            isMonster = true,
            allowEffectMonsters = false,
            maxLevel = 4,
            maxAtk = 950
        });

        // 2. Monstros Médios - 12 cartas
        deckStructure.Add(new CardPoolDefinition {
            poolName = "Medium Monsters",
            quantity = 12,
            isMonster = true,
            allowEffectMonsters = false,
            maxLevel = 4,
            minAtk = 951,
            maxAtk = 1300
        });

        // 3. Monstros Fortes (Ace) - 3 cartas
        deckStructure.Add(new CardPoolDefinition {
            poolName = "Strong Monsters",
            quantity = 3,
            isMonster = true,
            allowEffectMonsters = true, // Permite efeitos simples
            maxLevel = 4,
            minAtk = 1301,
            maxAtk = 1600
        });

        // 4. Magias (Equips/Fields) - 5 cartas
        deckStructure.Add(new CardPoolDefinition {
            poolName = "Spells",
            quantity = 5,
            isSpell = true,
            // Banir cartas OP de início
            forbiddenIds = new List<string> { "0336", "0337", "0414", "0791", "1447", "1480" } // Dark Hole, Raigeki, Pot of Greed, etc.
        });

        // 5. Armadilhas - 5 cartas
        deckStructure.Add(new CardPoolDefinition {
            poolName = "Traps",
            quantity = 5,
            isTrap = true,
            forbiddenIds = new List<string> { "1251", "1252" } // Mirror Force, Mirror Wall
        });
    }

    public List<CardData> GenerateInitialDeck()
    {
        SetupDefaultStructure();
        
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null)
        {
            Debug.LogError("InitialDeckBuilder: CardDatabase não encontrado!");
            return new List<CardData>();
        }

        List<CardData> allCards = GameManager.Instance.cardDatabase.cardDatabase;
        List<CardData> newDeck = new List<CardData>();
        System.Random rng = new System.Random();

        foreach (var pool in deckStructure)
        {
            // 1. Filtra as cartas candidatas para este pool
            var candidates = allCards.Where(card => IsCardValidForPool(card, pool)).ToList();

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"InitialDeckBuilder: Nenhuma carta encontrada para o pool '{pool.poolName}'");
                continue;
            }

            // 2. Seleciona aleatoriamente a quantidade necessária
            for (int i = 0; i < pool.quantity; i++)
            {
                if (newDeck.Count >= deckSize) break;

                CardData selected = candidates[rng.Next(candidates.Count)];
                newDeck.Add(selected);
            }
        }

        // Embaralha o deck final
        newDeck = newDeck.OrderBy(x => rng.Next()).ToList();

        // Preenche até 40 se faltou algo (fallback com monstros fracos)
        while (newDeck.Count < deckSize)
        {
            var filler = allCards.Where(c => c.type.Contains("Normal") && c.level <= 4 && c.atk < 1000).ToList();
            if (filler.Count > 0)
                newDeck.Add(filler[rng.Next(filler.Count)]);
            else
                break; // Evita loop infinito se DB estiver vazio
        }

        Debug.Log($"Deck Inicial Gerado: {newDeck.Count} cartas.");
        return newDeck;
    }

    private bool IsCardValidForPool(CardData card, CardPoolDefinition pool)
    {
        // Filtros Globais de Exclusão
        if (pool.forbiddenIds.Contains(card.id)) return false;
        if (card.type.Contains("Fusion")) return false;
        if (card.type.Contains("Ritual")) return false;
        if (card.type.Contains("Token")) return false;

        // Filtro de Tipo
        bool typeMatch = false;
        if (pool.isMonster && card.type.Contains("Monster")) typeMatch = true;
        else if (pool.isSpell && card.type.Contains("Spell")) typeMatch = true;
        else if (pool.isTrap && card.type.Contains("Trap")) typeMatch = true;

        if (!typeMatch) return false;

        // Filtros Específicos de Monstro
        if (pool.isMonster)
        {
            if (card.level > pool.maxLevel) return false;
            if (card.atk < pool.minAtk || card.atk > pool.maxAtk) return false;
            
            bool isEffect = card.type.Contains("Effect");
            if (isEffect && !pool.allowEffectMonsters) return false;
        }

        return true;
    }
}
