using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necessário para ordenar lista de tributos

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance;

    [Header("Limites de Turno")]
    public bool hasPerformedNormalSummon = false;
    
    [Header("Configuração")]
    public bool enableAutoTribute = false; // Switch para alternar entre tributo automático e manual

    // Narrow Pass (1320)
    public bool narrowPassActive = false;
    public int narrowPassSummonCount = 0;

    // Estado da Seleção Manual de Tributo
    [HideInInspector] public bool isSelectingTributes = false;
    private GameObject pendingCardGO;
    private CardData pendingCardData;
    private bool pendingIsSet;
    private List<CardDisplay> currentSelectedTributes = new List<CardDisplay>();
    private int currentTributesRequired;

    [Header("Contadores de Special Summon (Player)")]
    public int specialSummonFromHandAtk = 0;
    public int specialSummonFromHandDef = 0;
    public int specialSummonFromDeckAtk = 0;
    public int specialSummonFromDeckDef = 0;
    public int specialSummonFromGYAtk = 0;
    public int specialSummonFromGYDef = 0;
    public int fusionSummonCount = 0;
    public int ritualSummonCount = 0;

    [Header("Contadores de Special Summon (Oponente)")]
    public int opponentSpecialSummonFromHand = 0;
    public int opponentSpecialSummonFromDeck = 0;
    public int opponentSpecialSummonFromGY = 0;
    public int opponentSpecialSummonFromField = 0; // Ex: Roubar monstro

    void Awake()
    {
        Instance = this;
    }

    public void ResetTurnStats()
    {
        hasPerformedNormalSummon = false;
        narrowPassSummonCount = 0;
        // Resetar contadores de special summon se necessário (geralmente não há limite por turno para special, mas podemos querer rastrear)
    }

    // Verifica se pode realizar Normal Summon/Set
    public bool CanNormalSummon()
    {
        if (GameManager.Instance != null && GameManager.Instance.devMode) return true;
        return !hasPerformedNormalSummon;
    }

    // Calcula tributos necessários baseado no nível
    public int GetRequiredTributes(int level)
    {
        if (level <= 4) return 0;
        if (level <= 6) return 1;
        return 2;
    }

    // Verifica se tem monstros suficientes para tributar
    public bool HasEnoughTributes(int requiredTributes, bool isPlayer, CardData monsterToSummon = null)
    {
        if (requiredTributes <= 0) return true;
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return false;

        int tributeValue = 0;
        Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;

        foreach (Transform zone in zones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null)
                {
                    int value = 1;
                    if (monsterToSummon != null)
                    {
                        string id = cd.CurrentCardData.id;
                        // Lógica de Tributo Duplo
                        if (id == "2023" && monsterToSummon.attribute == "Water") value = 2; // Unshaven Angler
                        else if (id == "2071" && monsterToSummon.attribute == "Wind") value = 2; // Whirlwind Prodigy
                        else if (id == "1901" && monsterToSummon.attribute == "Earth") value = 2; // Trojan Horse
                        else if (id == "0523" && monsterToSummon.attribute == "Dark") value = 2; // Double Coston
                        else if (id == "0995" && monsterToSummon.attribute == "Light") value = 2; // Kaiser Sea Horse
                        else if (id == "0673" && monsterToSummon.attribute == "Fire") value = 2; // Flame Ruler
                    }
                    tributeValue += value;
                }
            }
        }

        return tributeValue >= requiredTributes;
    }

    // Realiza a invocação (lógica de regras)
    public bool PerformSummon(GameObject cardGO, CardData card, bool isSet, bool isSpecial, bool isPlayer, bool ignoreLimit = false)
    {
        // Se for Normal Summon, verifica limite e tributos
        if (!isSpecial)
        {
            // ... (verificações de Narrow Pass e Normal Summon existentes) ...
            if (narrowPassActive)
            {
                if (narrowPassSummonCount >= 2)
                {
                    Debug.LogWarning("Narrow Pass: Limite de 2 invocações adicionais atingido.");
                    return false;
                }
            }
            else if (!CanNormalSummon() && !ignoreLimit)
            {
                Debug.LogWarning("Já realizou Normal Summon neste turno.");
                return false;
            }

            int tributesNeeded = GetRequiredTributes(card.level);
            if (!HasEnoughTributes(tributesNeeded, isPlayer, card))
            {
                // Para a IA, isso não é um erro, é apenas uma verificação
                if (isPlayer) Debug.LogWarning($"Tributos insuficientes! Precisa de {tributesNeeded}.");
                return false;
            }

            if (tributesNeeded > 0)
            {
                // ALTERAÇÃO AQUI: Se for IA (!isPlayer) OU Simulação, força o auto-tributo
                if (enableAutoTribute || !isPlayer || GameManager.Instance.isSimulating)
                {
                    ProcessAutoTribute(tributesNeeded, isPlayer);
                }
                else
                {
                    StartManualTributeSelection(cardGO, card, isSet, isPlayer, tributesNeeded);
                    return false; // Interrompe o fluxo imediato para esperar a seleção
                }
            }

            // FIX: Marca que a invocação normal foi realizada, seja Player ou IA
            hasPerformedNormalSummon = true;
            if (isPlayer && narrowPassActive) narrowPassSummonCount++;
        }
        else
        {
            // Lógica de contagem de Special Summon
            if (isPlayer)
            {
                if (card.type.Contains("Fusion")) fusionSummonCount++;
                else if (card.type.Contains("Ritual")) ritualSummonCount++;
                // Outros tipos...
            }
        }

        return true;
    }

    // Inicia o modo de seleção manual
    private void StartManualTributeSelection(GameObject cardGO, CardData card, bool isSet, bool isPlayer, int required)
    {
        isSelectingTributes = true;
        pendingCardGO = cardGO;
        pendingCardData = card;
        pendingIsSet = isSet;
        currentTributesRequired = required;
        currentSelectedTributes.Clear();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowConfirmation($"Selecione {required} monstro(s) para tributar.", null, CancelTributeSelection); // Null no confirm pois a confirmação virá após selecionar
        
        Debug.Log($"Iniciando seleção de tributo. Necessário: {required}");
    }

    // Chamado pelo CardDisplay ao clicar em um monstro no campo
    public void SelectTributeCandidate(CardDisplay candidate)
    {
        if (!isSelectingTributes) return;
        if (!candidate.isPlayerCard || !candidate.isOnField) return;

        if (currentSelectedTributes.Contains(candidate))
        {
            // Deselecionar
            currentSelectedTributes.Remove(candidate);
            candidate.SetTributeHighlight(false);
        }
        else
        {
            // Selecionar
            if (currentSelectedTributes.Count < currentTributesRequired)
            {
                currentSelectedTributes.Add(candidate);
                candidate.SetTributeHighlight(true);
            }
        }

        // Verifica se completou a seleção
        if (currentSelectedTributes.Count == currentTributesRequired)
        {
            UIManager.Instance.ShowConfirmation("Tributar os monstros selecionados?", ConfirmManualTribute, CancelTributeSelection);
        }
    }

    private void ConfirmManualTribute()
    {
        foreach (var monster in currentSelectedTributes)
        {
            GameManager.Instance.TributeCard(monster);
        }

        isSelectingTributes = false;
        
        // Marca que a invocação normal foi realizada (pois o PerformSummon retornou false para esperar esta seleção)
        hasPerformedNormalSummon = true;
        if (narrowPassActive) narrowPassSummonCount++;

        if (GameManager.Instance != null)
            GameManager.Instance.FinalizeSummon(pendingCardGO, pendingCardData, pendingIsSet, true, pendingIsSet, true);
    }

    public void CancelTributeSelection()
    {
        isSelectingTributes = false;
        foreach (var monster in currentSelectedTributes)
        {
            if (monster != null) monster.SetTributeHighlight(false);
        }
        currentSelectedTributes.Clear();
        Debug.Log("Seleção de tributo cancelada.");
    }

    // Lógica temporária: Sacrifica automaticamente os monstros com menor ATK
    private void ProcessAutoTribute(int count, bool isPlayer)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;

        Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        List<CardDisplay> availableMonsters = new List<CardDisplay>();

        // Coleta monstros disponíveis
        foreach (Transform zone in zones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) availableMonsters.Add(cd);
            }
        }

        // Ordena por ATK (menor para maior) para sacrificar os mais fracos primeiro
        // Isso é uma lógica básica para validar o sistema. No futuro, abriremos um menu de seleção.
        availableMonsters.Sort((a, b) => a.CurrentCardData.atk.CompareTo(b.CurrentCardData.atk));

        int tributed = 0;
        foreach (CardDisplay monster in availableMonsters)
        {
            if (tributed >= count) break;

            GameManager.Instance.TributeCard(monster);
            tributed++;
        }
        
        // Finaliza como Tribute Summon
        // Nota: O fluxo original chamava FinalizeSummon no GameManager.TrySummonMonster.
        // Precisamos garantir que a flag seja passada corretamente lá também se for automático.
    }

    public void SelectTributes(int count, bool isPlayer, System.Action<List<CardDisplay>> onComplete)
    {
        // Simplificado: Usa seleção automática ou abre UI se implementado
        if (enableAutoTribute)
        {
            // Lógica de auto-tributo (não retorna lista, apenas executa)
            // Para este método que requer callback, precisamos simular a seleção
            // ...
            Debug.LogWarning("SelectTributes: Auto-tributo não suporta callback de lista ainda.");
        }
        else
        {
            // Inicia seleção manual (requer adaptação para retornar lista via callback em vez de finalizar summon)
            // Por enquanto, log de erro
            Debug.LogError("SelectTributes: Seleção manual com callback não implementada.");
        }
    }
}