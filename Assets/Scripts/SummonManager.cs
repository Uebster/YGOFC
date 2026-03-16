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

    // Armazena a zona do último monstro tributado automaticamente (para a IA/Oponente usar a mesma zona)
    public Transform lastAutoTributeZone;

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
        lastAutoTributeZone = null;
        // Resetar contadores de special summon se necessário (geralmente não há limite por turno para special, mas podemos querer rastrear)
    }

    // Verifica se pode realizar Normal Summon/Set
    public bool CanNormalSummon()
    {
        if (GameManager.Instance != null && (GameManager.Instance.devMode || GameManager.Instance.infiniteNormalSummons)) return true;
        return !hasPerformedNormalSummon;
    }

    // Calcula tributos necessários baseado no nível
    public int GetRequiredTributes(int level)
    {
        if (GameManager.Instance != null && GameManager.Instance.disableTributeRequirements) return 0;

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

    // Executa todo o fluxo de invocação e gerencia exceções antes de finalizar no GameManager
    public void ExecuteSummonFlow(GameObject cardGO, CardData card, bool isSet, bool isSpecial, bool isPlayer, bool ignoreLimit = false)
    {
        if (isSpecial)
        {
            // Lógica de contagem de Special Summon
            if (isPlayer)
            {
                if (card.type.Contains("Fusion")) fusionSummonCount++;
                else if (card.type.Contains("Ritual")) ritualSummonCount++;
            }
            GameManager.Instance.FinalizeSummon(cardGO, card, isSet, isPlayer, isSet, false, null);
            return;
        }

        // INTERCEPTAÇÃO: Condições Alternativas de Invocação e Custos Dinâmicos (Sistema 4)
        if (isPlayer && !ignoreLimit && UIManager.Instance != null && !isSet && !GameManager.Instance.isSimulating)
        {
            CardDisplay display = cardGO.GetComponent<CardDisplay>();
            
            // 0706 - Fusilier Dragon (Normal sem tributo, stats cortados)
            if (card.id == "0706") 
            {
                UIManager.Instance.ShowConfirmation("Fusilier Dragon: Invocar sem Tributo (ATK/DEF reduzidos pela metade)?", 
                () => {
                    display.tributeCount = 0;
                    hasPerformedNormalSummon = true;
                    GameManager.Instance.FinalizeSummon(cardGO, card, false, isPlayer, false, false, null);
                    display.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Multiply, 0.5f, display));
                    display.AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Permanent, StatModifier.Operation.Multiply, 0.5f, display));
                }, 
                () => { ProcessStandardSummon(cardGO, card, isSet, isPlayer, ignoreLimit); });
                return; // Espera a UI
            }
            // 0768 - Gilford the Lightning / 1257 - Moisture Creature (Tributos massivos)
            else if ((card.id == "0768" || card.id == "1257") && GameManager.Instance.GetMonsterCount(isPlayer) >= 3) 
            {
                UIManager.Instance.ShowConfirmation($"Usar 3 Tributos para ativar o efeito especial de {card.name}?", 
                () => {
                    SelectTributes(3, true, (tributes) => {
                        foreach(var t in tributes) GameManager.Instance.TributeCard(t);
                        display.isTributeSummoned = true;
                        display.tributeCount = 3; 
                        hasPerformedNormalSummon = true;
                        GameManager.Instance.FinalizeSummon(cardGO, card, isSet, isPlayer, isSet, true, null);
                    });
                }, 
                () => { ProcessStandardSummon(cardGO, card, isSet, isPlayer, ignoreLimit); });
                return; // Espera a UI
            }
            // 0166 - Behemoth the King of All Animals (Tributo único = 2000 ATK)
            else if (card.id == "0166" && GameManager.Instance.GetMonsterCount(isPlayer) >= 1) 
            {
                UIManager.Instance.ShowConfirmation("Invocar com apenas 1 Tributo (ATK original torna-se 2000)?",
                () => {
                    SelectTributes(1, true, (tributes) => {
                        foreach(var t in tributes) GameManager.Instance.TributeCard(t);
                        display.isTributeSummoned = true;
                        display.tributeCount = 1;
                        hasPerformedNormalSummon = true;
                        GameManager.Instance.FinalizeSummon(cardGO, card, isSet, isPlayer, isSet, true, null);
                        display.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Permanent, StatModifier.Operation.Set, 2000, display));
                    });
                },
                () => { ProcessStandardSummon(cardGO, card, isSet, isPlayer, ignoreLimit); });
                return; // Espera a UI
            }
        }

        // Se não foi interceptado, segue o fluxo normal
        ProcessStandardSummon(cardGO, card, isSet, isPlayer, ignoreLimit);
    }

    private void ProcessStandardSummon(GameObject cardGO, CardData card, bool isSet, bool isPlayer, bool ignoreLimit)
    {
        if (narrowPassActive)
        {
            if (narrowPassSummonCount >= 2)
            {
                Debug.LogWarning("Narrow Pass: Limite de 2 invocações adicionais atingido.");
                return;
            }
        }
        else if (!CanNormalSummon() && !ignoreLimit)
        {
            Debug.LogWarning("Já realizou Normal Summon neste turno.");
            return;
        }

        int tributesNeeded = GetRequiredTributes(card.level);
        if (!HasEnoughTributes(tributesNeeded, isPlayer, card))
        {
            if (isPlayer) Debug.LogWarning($"Tributos insuficientes! Precisa de {tributesNeeded}.");
            return;
        }

        if (tributesNeeded > 0)
        {
            if (enableAutoTribute || !isPlayer || GameManager.Instance.isSimulating)
            {
                ProcessAutoTribute(tributesNeeded, isPlayer);
                
                hasPerformedNormalSummon = true;
                if (isPlayer && narrowPassActive) narrowPassSummonCount++;

                CardDisplay cd = cardGO.GetComponent<CardDisplay>();
                if (cd != null) cd.tributeCount = tributesNeeded;

                Transform specificZone = null;
                if (lastAutoTributeZone != null && GameManager.Instance.placeTributeSummonInTributeZone) specificZone = lastAutoTributeZone;

                GameManager.Instance.FinalizeSummon(cardGO, card, isSet, isPlayer, isSet, true, specificZone);
            }
            else
            {
                StartManualTributeSelection(cardGO, card, isSet, isPlayer, tributesNeeded);
            }
        }
        else
        {
            hasPerformedNormalSummon = true;
            if (isPlayer && narrowPassActive) narrowPassSummonCount++;

            GameManager.Instance.FinalizeSummon(cardGO, card, isSet, isPlayer, isSet, false, null);
        }
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
        // Captura a zona do primeiro tributo para ocupar o lugar
        Transform targetZone = null;
        if (GameManager.Instance != null && GameManager.Instance.placeTributeSummonInTributeZone)
        {
            if (currentSelectedTributes.Count > 0 && currentSelectedTributes[0] != null)
            {
                targetZone = currentSelectedTributes[0].transform.parent;
            }
        }

        foreach (var monster in currentSelectedTributes)
        {
            GameManager.Instance.TributeCard(monster);
        }

        isSelectingTributes = false;
        
        // Marca que a invocação normal foi realizada (pois o PerformSummon retornou false para esperar esta seleção)
        hasPerformedNormalSummon = true;
        if (narrowPassActive) narrowPassSummonCount++;
        
        CardDisplay cd = pendingCardGO.GetComponent<CardDisplay>();
        if (cd != null) cd.tributeCount = currentTributesRequired;

        if (GameManager.Instance != null)
            GameManager.Instance.FinalizeSummon(pendingCardGO, pendingCardData, pendingIsSet, true, pendingIsSet, true, targetZone);
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
        lastAutoTributeZone = null; // Reseta antes de processar

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

            // Captura a zona do primeiro tributo
            if (tributed == 0)
            {
                lastAutoTributeZone = monster.transform.parent;
            }

            GameManager.Instance.TributeCard(monster);
            tributed++;
        }
    }

    public void SelectTributes(int count, bool isPlayer, System.Action<List<CardDisplay>> onComplete, List<CardDisplay> current = null)
    {
        if (current == null) current = new List<CardDisplay>();

        if (current.Count == count)
        {
            onComplete?.Invoke(current);
            return;
        }

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard == isPlayer && t.CurrentCardData.type.Contains("Monster") && !current.Contains(t),
                (selected) =>
                {
                    current.Add(selected);
                    SelectTributes(count, isPlayer, onComplete, current);
                }
            );
        }
    }
}