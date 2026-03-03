using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SpellCounterManager : MonoBehaviour
{
    public static SpellCounterManager Instance;

    [Header("Visuals")]
    public GameObject counterPrefab; // Prefab com um Sprite/Texto para o contador
    public Vector3 counterOffset = new Vector3(0.3f, 0.3f, -0.1f);

    // Dicionário para rastrear contadores por carta
    private Dictionary<CardDisplay, int> counters = new Dictionary<CardDisplay, int>();
    private Dictionary<CardDisplay, GameObject> visualCounters = new Dictionary<CardDisplay, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public void AddCounter(CardDisplay card, int amount = 1)
    {
        if (card == null) return;

        if (!counters.ContainsKey(card))
        {
            counters[card] = 0;
        }

        counters[card] += amount;
        UpdateVisuals(card);
        Debug.Log($"SpellCounterManager: {amount} contador(es) adicionado(s) a {card.CurrentCardData.name}. Total: {counters[card]}");
    }

    public bool RemoveCounter(CardDisplay card, int amount = 1)
    {
        if (card == null || !counters.ContainsKey(card)) return false;

        if (counters[card] >= amount)
        {
            counters[card] -= amount;
            if (counters[card] <= 0)
            {
                counters.Remove(card);
                RemoveVisuals(card);
            }
            else
            {
                UpdateVisuals(card);
            }
            Debug.Log($"SpellCounterManager: {amount} contador(es) removido(s) de {card.CurrentCardData.name}.");
            return true;
        }
        return false;
    }

    public int GetCount(CardDisplay card)
    {
        if (card == null || !counters.ContainsKey(card)) return 0;
        return counters[card];
    }

    public bool RemoveCountersFromField(int amount, bool isPlayer)
    {
        // Remove 'amount' contadores de qualquer lugar do campo do jogador
        int total = GetTotalCounters(isPlayer);
        if (total < amount) return false;

        int remaining = amount;
        List<CardDisplay> keys = new List<CardDisplay>(counters.Keys);
        
        foreach (var card in keys)
        {
            if (card.isPlayerCard == isPlayer)
            {
                int available = counters[card];
                int toRemove = Mathf.Min(available, remaining);
                
                RemoveCounter(card, toRemove);
                remaining -= toRemove;
                
                if (remaining <= 0) break;
            }
        }
        return true;
    }

    public int GetTotalCounters(bool isPlayer)
    {
        int total = 0;
        foreach (var kvp in counters)
        {
            if (kvp.Key.isPlayerCard == isPlayer)
            {
                total += kvp.Value;
            }
        }
        return total;
    }

    private void UpdateVisuals(CardDisplay card)
    {
        if (!visualCounters.ContainsKey(card))
        {
            // Cria visual se não existir
            if (counterPrefab != null)
            {
                GameObject go = Instantiate(counterPrefab, card.transform);
                go.transform.localPosition = counterOffset;
                go.transform.localRotation = Quaternion.identity;
                visualCounters[card] = go;
            }
        }

        // Atualiza texto se houver
        if (visualCounters.ContainsKey(card))
        {
            GameObject go = visualCounters[card];
            TextMeshPro text = go.GetComponentInChildren<TextMeshPro>();
            if (text != null)
            {
                text.text = counters[card].ToString();
            }
        }
    }

    private void RemoveVisuals(CardDisplay card)
    {
        if (visualCounters.ContainsKey(card))
        {
            if (visualCounters[card] != null) Destroy(visualCounters[card]);
            visualCounters.Remove(card);
        }
    }
    
    // Limpeza quando carta sai do campo
    public void OnCardLeavesField(CardDisplay card)
    {
        if (counters.ContainsKey(card))
        {
            counters.Remove(card);
            RemoveVisuals(card);
        }
    }
}
