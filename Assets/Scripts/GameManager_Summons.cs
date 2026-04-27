using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public partial class GameManager
{
    // ==============================================================================
    // SUMMONS
    // ==============================================================================

    public CardDisplay SpecialSummonFromData(CardData cardData, bool isPlayer, int zoneIndex = -1, bool inAttackPosition = true, bool faceDown = false, Vector3? sourcePos = null, CardLocation sourceLoc = CardLocation.Graveyard, bool? ownerIsPlayer = null)
    {
        if (cardData == null)
        {
            return null;
        }

        if (duelFieldUI == null) return null;
        Transform[] monsterZones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        Transform zone = null;
        
        if (zoneIndex >= 0 && zoneIndex < monsterZones.Length)
        {
            zone = monsterZones[zoneIndex];
            if (zone.childCount > 0)
            {
                Debug.LogWarning($"[GameManager] SpecialSummonFromData: Zona {zoneIndex} já está ocupada.");
                return null;
            }
        }
        else
        {
            zone = GetFreeMonsterZone(isPlayer);
            if (zone == null)
            {
                Debug.LogWarning("[GameManager] SpecialSummonFromData: Nenhuma zona livre encontrada.");
                return null;
            }
        }
        
        GameObject cardGO = Instantiate(cardPrefab, zone);
        CardDisplay cardDisplay = cardGO.GetComponent<CardDisplay>();

        cardGO.transform.localPosition = Vector3.zero; // Explicitamente centraliza dentro da zona
        cardGO.transform.localScale = fieldCardScale;
        cardGO.transform.localRotation = Quaternion.identity; // Reseta a rotação antes de aplicar a específica

        cardDisplay.isInteractable = false; // Cartas no campo não são interativas como as da mão
        cardDisplay.isPlayerCard = isPlayer;
        cardDisplay.ownerPlayer = ownerIsPlayer ?? isPlayer;
        cardDisplay.isOnField = true;
        cardDisplay.position = inAttackPosition ? CardDisplay.BattlePosition.Attack : CardDisplay.BattlePosition.Defense;
        cardDisplay.summonedTurnCount = turnCount;
        cardDisplay.hasChangedPositionThisTurn = false;
        
        cardDisplay.SetCard(cardData, cardBackTexture, !faceDown);
        
        if (!inAttackPosition) // Defesa
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 90f : -90f);
        }
        else
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 0f : 180f);
        }

        System.Action completeSummon = () => {
            if (monsterStatDisplayMode != StatDisplayMode.None && fieldStatDisplayPrefab != null && cardData.type.Contains("Monster"))
            {
                GameObject statGO = Instantiate(fieldStatDisplayPrefab, cardGO.transform.parent);
                FieldStatUI statUI = statGO.GetComponent<FieldStatUI>();
                if (statUI != null) statUI.Setup(cardDisplay); 
                float adjustment = (monsterStatDisplayMode == StatDisplayMode.AboveCard) ? -cardYAdjustmentForStats : cardYAdjustmentForStats;
                if (statDisplayMatchCardRotation && !isPlayer) adjustment = -adjustment; 
                cardGO.transform.localPosition += new Vector3(0, adjustment, 0);
            }

            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.OnSpecialSummon(cardDisplay);
            }
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayPlacementAura(cardDisplay);
        };

        if (sourcePos.HasValue && DuelFXManager.Instance != null && !isSimulating)
        {
            CardFlightSettings settings = null;
            if (sourceLoc == CardLocation.Hand) settings = DuelFXManager.Instance.flightHandToField;
            else if (sourceLoc == CardLocation.Banished) settings = DuelFXManager.Instance.flightBanishToField;
            else if (sourceLoc == CardLocation.Graveyard) settings = DuelFXManager.Instance.flightGraveyardToField;
            else if (sourceLoc == CardLocation.ExtraDeck) settings = DuelFXManager.Instance.flightExtraToField;
            else settings = DuelFXManager.Instance.flightGraveyardToField; // Fallback

            if (settings != null && !isSimulating && settings.enableFlight)
            {
                cardDisplay.SetVisibility(false);
                bool isPile = sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.ExtraDeck || sourceLoc == CardLocation.Banished;
                Quaternion startRot = (sourceLoc == CardLocation.Hand) ? Quaternion.Euler(0, 0, isPlayer ? 0 : 180f) : Quaternion.identity;
                Vector3 sScale = (sourceLoc == CardLocation.Hand) ? handCardScale : fieldCardScale;
                
                bool sFaceUp = !faceDown;
                if (sourceLoc == CardLocation.Hand) sFaceUp = isPlayer || showOpponentHand;
                else if (sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished) sFaceUp = true;
                else if (sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.ExtraDeck) sFaceUp = false;

                if (isPile && DuelFXManager.Instance.extractionCinematic.enableExtraction)
                {
                    bool ownerPile = ownerIsPlayer ?? isPlayer;
                    DuelFXManager.Instance.PlayExtractionCinematic(cardData, cardBackTexture, ownerPile, sourceLoc, sourcePos.Value, sFaceUp, !faceDown, (ghost, pos) => {
                        DuelFXManager.Instance.PlayCardFlight(cardData, cardBackTexture, !faceDown, !faceDown, pos, cardGO.transform.position, sScale, fieldCardScale, startRot, cardGO.transform.rotation, settings, false, () => { cardDisplay.SetVisibility(true); completeSummon(); }, ghost);
                    });
                }
                else
                {
                    bool popFromPile = sourceLoc != CardLocation.Hand && sourceLoc != CardLocation.Field;
                    DuelFXManager.Instance.PlayCardFlight(cardData, cardBackTexture, sFaceUp, !faceDown, sourcePos.Value, cardGO.transform.position, 
                        sScale, fieldCardScale, startRot, cardGO.transform.rotation, settings, popFromPile, () => {
                        cardDisplay.SetVisibility(true);
                        completeSummon();
                    });
                }
            }
            else completeSummon();
        }
        else completeSummon();
        
        return cardDisplay;
    }

    public void OnSummon(CardDisplay card)
    {
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnSummon(card);
        }
    }

    public void OnFlipSummon(CardDisplay card)
    {
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnFlipSummon(card);
        }
    }

    // --- LÓGICA DE INVOCACÃO ---

    // Renomeado para TrySummonMonster para indicar que é o início do processo
    public bool TrySummonMonster(GameObject cardGO, CardData cardData, bool isSet, bool ignoreLimit = false)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;
        string cardName = cardData?.name ?? "Unknown";
        
        int dynamicLevel = CardEffectManager.Instance.auraManager.GetStatModifier(new LuaCard(cardData), "LEVEL", CardLocation.Hand) + cardData.level;

        Debug.Log($"[Summon Check] Tentando invocar: {cardName} | Lvl Base: {cardData.level} | Lvl Dinâmico (Auras): {dynamicLevel}");

        // 0.1 Validação de Limite de Invocação Normal (se não for ignorado por efeito)
        if (!ignoreLimit && !infiniteNormalSummons)
        {
            int currentSummons = isPlayer ? normalSummonsThisTurnPlayer : normalSummonsThisTurnOpponent;
            if (currentSummons > 0)
            {
                Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: Limite de 1 Invocação Normal por turno atingido.");
                if (isPlayer && !isSimulating && UIManager.Instance != null) 
                {
                    UIManager.Instance.ShowMessage("Você já realizou uma Invocação Normal/Set neste turno.");
                }
                return false;
            }
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: canPlacePlayerCards desativado.");
            return false;
        }
        // Bloqueia se for uma ação para o oponente, durante o turno do jogador, e o modo dev de controle do oponente estiver desligado.
        // A IA (que roda no turno do oponente) não será bloqueada por esta verificação.
        // FIX: Se estiver simulando (!isSimulating), ignora essa trava para permitir que o simulador jogue pelos dois lados rapidamente.
        if (!isPlayer && isPlayerTurn && !canPlaceOpponentCards && !isSimulating)
        {
            Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: canPlaceOpponentCards desativado durante turno do jogador.");
            return false;
        }

        // 1. Validação de Fase
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        if (!devMode && isPlayer && currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2 && !isSimulating)
        {
            Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: Invocação só permitida em Main Phase 1/2. Fase atual: {currentPhase}");
            return false;
        }

        int tributes = 0;
        if (dynamicLevel >= 5 && dynamicLevel <= 6) tributes = 1;
        if (dynamicLevel >= 7) tributes = 2;
        if (disableTributeRequirements) tributes = 0;

        if (tributes > 0)
        {
            List<CardData> possibleTributes = new List<CardData>();
            Transform[] playerZones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
            foreach (var zone in playerZones) {
                if (zone != null && zone.childCount > 0) {
                    var cd = zone.GetComponentInChildren<CardDisplay>();
                    // Cartas Face-down (Setadas) são alvos nativamente válidos!
                    if (cd != null && cd.CurrentCardData != null && cd.CurrentCardData.type.Contains("Monster")) possibleTributes.Add(cd.CurrentCardData);
                }
            }

            if (possibleTributes.Count < tributes)
            {
                if (isPlayer && !isSimulating && UIManager.Instance != null) 
                    UIManager.Instance.ShowMessage($"Tributos insuficientes! Necessário: {tributes}.");
                return false;
            }

            if (!isPlayer || isSimulating)
            {
                var chosen = possibleTributes.OrderBy(c => c.atk).Take(tributes).ToList();
                PerformTributeSummon(cardGO, cardData, isSet, isPlayer, dynamicLevel, chosen);
                return true;
            }

            if (useDirectHandSelection)
            {
                StartDirectSelection(possibleTributes, tributes, tributes, null, $"Selecione {tributes} Tributo(s) para {cardData.name}", (selected) => {
                    if (selected != null && selected.Count == tributes) PerformTributeSummon(cardGO, cardData, isSet, isPlayer, dynamicLevel, selected);
                }, HighlightCategory.Tribute);
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowCardSelection(possibleTributes, $"Selecione {tributes} Tributo(s) para {cardData.name}", tributes, tributes, (selected) => {
                        if (selected != null && selected.Count == tributes) PerformTributeSummon(cardGO, cardData, isSet, isPlayer, dynamicLevel, selected);
                    });
                }
            }
            return true;
        }
        else
        {
            if (!ignoreLimit && !infiniteNormalSummons) {
                if (isPlayer) normalSummonsThisTurnPlayer++;
                else normalSummonsThisTurnOpponent++;
            }
            Vector3 sourcePos = cardGO.transform.position;
            FinalizeSummon(cardGO, cardData, isSet, isPlayer, isSet, false, null, null, sourcePos, CardLocation.Hand);
            return true;
        }
    }

    private void PerformTributeSummon(GameObject cardGO, CardData cardData, bool isSet, bool isPlayer, int dynamicLevel, List<CardData> tributes)
    {
        Transform firstTributeZone = null;

        foreach (var tribute in tributes)
        {
            CardDisplay fieldObject = FindCardOnField(tribute.id, isPlayer);
            if (fieldObject != null) 
            {
                // Salva a zona da primeira carta tributada para pousar nela (se a opção estiver ativa)
                if (firstTributeZone == null) firstTributeZone = fieldObject.transform.parent;
                TributeCard(fieldObject);
            }
        }

        if (!infiniteNormalSummons) {
            if (isPlayer) normalSummonsThisTurnPlayer++;
            else normalSummonsThisTurnOpponent++;
        }

        Vector3 sourcePos = cardGO.transform.position;
        Transform targetZone = placeTributeSummonInTributeZone ? firstTributeZone : null;
        
        FinalizeSummon(cardGO, cardData, isSet, isPlayer, isSet, true, targetZone, null, sourcePos, CardLocation.Hand);
    }

    // Novo método para Special Summon que pede a posição
    public void PerformSpecialSummon(GameObject cardGO, CardData cardData)
    {
        if (isSimulating)
        {
            Vector3 sPosSim = cardGO.transform.position;
            CardLocation sLocSim = cardGO.GetComponent<CardDisplay>().isOnField ? CardLocation.Field : CardLocation.Hand;
            FinalizeSummon(cardGO, cardData, false, true, false, false, null, null, sPosSim, sLocSim); // false = Face-Up
            return;
        }

        if (UIManager.Instance != null)
        {
            Vector3 sPos = cardGO.transform.position;
            CardLocation sLoc = cardGO.GetComponent<CardDisplay>().isOnField ? CardLocation.Field : CardLocation.Hand;

            UIManager.Instance.ShowPositionSelection(cardData, (selectedPosition) =>
            {
                bool isDefense = (selectedPosition == CardDisplay.BattlePosition.Defense);
                // Special Summon geralmente é Face-Up, mesmo em defesa
                FinalizeSummon(cardGO, cardData, isDefense, true, false, false, null, null, sPos, sLoc); // false = Face-Up
            });
        }
    }

    // Novo método público para finalizar a invocação (chamado pelo SummonManager após tributo manual)
    // Atualizado para suportar Face-Down explicitamente
    public void FinalizeSummon(GameObject cardGO, CardData cardData, bool isDefensePos, bool isPlayer, bool isFaceDown = false, bool isTributeSummon = false, Transform specificZone = null, List<CardData> specialMaterials = null, Vector3? sourcePos = null, CardLocation sourceLoc = CardLocation.Hand, bool? ownerIsPlayer = null)
    {
        // 2. Encontrar Zona Livre
        Transform targetZone = specificZone;
        if (targetZone == null) targetZone = GetFreeMonsterZone(isPlayer);

        if (targetZone == null)
        {
            Debug.LogWarning("Sem zonas de monstro livres!");
            if (isPlayer && !isSimulating && UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("Não há Zonas de Monstros livres para invocar!");
            }
            return;
        }

        // 3. Mover Carta (Lógica de Dados e Visual)
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (playerHand.Contains(cardGO)) playerHand.Remove(cardGO);
        else if (opponentHand.Contains(cardGO)) opponentHand.Remove(cardGO);

        cardGO.transform.SetParent(targetZone); // Coloca na zona
        cardGO.transform.localPosition = Vector3.zero; // Centraliza
        cardGO.transform.localScale = fieldCardScale; // Reseta escala para a do campo

        // 4. Configuração Visual (Ataque vs Defesa)
        if (display != null)
        {
            display.isInteractable = false; // Desativa o hover de mão (subir)
            display.isPlayerCard = isPlayer;
            display.ownerPlayer = ownerIsPlayer ?? isPlayer;
            display.isOnField = true;
            display.summonedTurnCount = turnCount; // Registra o turno de invocação
            display.hasChangedPositionThisTurn = false;

            // 1081 - Light of Intervention
            if (isFaceDown && IsCardActiveOnField("1081"))
            {
                isFaceDown = false;
                Debug.Log("Light of Intervention: Monstro Setado forçado a Face-up Defense.");
            }

            if (isDefensePos)
            {
                display.position = CardDisplay.BattlePosition.Defense;
                // Modo Defesa (Set): Virado para baixo e Rotacionado 90 graus
                float zRotation = isPlayer ? 90f : -90f;

                if (isFaceDown) display.ShowBack(); else display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            }
            else
            {
                display.position = CardDisplay.BattlePosition.Attack;
                // Modo Ataque: Virado para cima e Reto
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            }
            
            Vector3 endPos = cardGO.transform.position;
            Quaternion endRot = cardGO.transform.rotation;

            System.Action playEffects = () => {
                if (display != null) display.SetVisibility(true);
                
                bool isFusion = cardData.type.Contains("Fusion");
                bool isRitual = cardData.type.Contains("Ritual");
    
                if (monsterStatDisplayMode != StatDisplayMode.None && fieldStatDisplayPrefab != null && cardData.type.Contains("Monster"))
                {
                    GameObject statGO = Instantiate(fieldStatDisplayPrefab, cardGO.transform.parent);
                    FieldStatUI statUI = statGO.GetComponent<FieldStatUI>();
                    if (statUI != null) statUI.Setup(display);
                    float adjustment = (monsterStatDisplayMode == StatDisplayMode.AboveCard) ? -cardYAdjustmentForStats : cardYAdjustmentForStats;
                    if (statDisplayMatchCardRotation && !isPlayer) adjustment = -adjustment; 
                    cardGO.transform.localPosition += new Vector3(0, adjustment, 0);
                }
    
                bool useCinematic = false;
                if (isFusion) useCinematic = enableFusionCinematic && !isSimulating && !isFaceDown;
                else if (isRitual) useCinematic = enableRitualCinematic && !isSimulating && !isFaceDown;
                else useCinematic = enableSummonCinematics && isTributeSummon && !isSimulating && !isFaceDown;
                
                if (useCinematic && DuelFXManager.Instance != null)
                {
                    display.SetVisibility(false);
                GameManager.Instance.pendingVisualTasks++;
                    System.Action onCinematicComplete = () => {
                        display.SetVisibility(true);
                        DuelFXManager.Instance.PlaySummonAura(display);
                        if (CardEffectManager.Instance != null) {
                            if (isFaceDown) CardEffectManager.Instance.OnSet(display);
                            else if (cardData.type.Contains("Fusion") || cardData.type.Contains("Ritual") || cardData.type.Contains("Synchro") || cardData.type.Contains("Xyz") || cardData.type.Contains("Link")) 
                                CardEffectManager.Instance.OnSpecialSummon(display);
                            else CardEffectManager.Instance.OnSummon(display);
                        }
                    GameManager.Instance.pendingVisualTasks--;
                    };
                    if (isFusion) DuelFXManager.Instance.PlayFusionCinematic(display, specialMaterials, null, onCinematicComplete);
                    else if (isRitual) DuelFXManager.Instance.PlayRitualCinematic(display, null, onCinematicComplete);
                    else DuelFXManager.Instance.PlaySummonCinematic(display, true, false, onCinematicComplete);
                }
                else
                {
                    if (DuelFXManager.Instance != null && !isDefensePos)
                    {
                        if (isTributeSummon && enableTributeSummonAnimation) DuelFXManager.Instance.PlayTributeSummonEffect(display);
                        else DuelFXManager.Instance.PlaySummonEffect(display);
                    }
                    else if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayFlipEffect(display);
                
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayPlacementAura(display);
                    if (CardEffectManager.Instance != null) {
                        if (isFaceDown) CardEffectManager.Instance.OnSet(display);
                        else if (cardData.type.Contains("Fusion") || cardData.type.Contains("Ritual") || cardData.type.Contains("Synchro") || cardData.type.Contains("Xyz") || cardData.type.Contains("Link")) 
                            CardEffectManager.Instance.OnSpecialSummon(display);
                        else CardEffectManager.Instance.OnSummon(display);
                    }
                }
            };
            
            if (sourcePos.HasValue && DuelFXManager.Instance != null && !isSimulating)
            {
                CardFlightSettings settings = null;
                if (sourceLoc == CardLocation.Hand) settings = DuelFXManager.Instance.flightHandToField;
                else if (sourceLoc == CardLocation.Banished) settings = DuelFXManager.Instance.flightBanishToField;
                else if (sourceLoc == CardLocation.Graveyard) settings = DuelFXManager.Instance.flightGraveyardToField;
                else if (sourceLoc == CardLocation.ExtraDeck) settings = DuelFXManager.Instance.flightExtraToField;
                else settings = DuelFXManager.Instance.flightGraveyardToField; // Fallback              
                
                if (settings != null && settings.enableFlight)
                {
                    display.SetVisibility(false);
                    GameManager.Instance.pendingVisualTasks++;
                    System.Action flightCallback = () => { playEffects(); GameManager.Instance.pendingVisualTasks--; };
                    bool popFromPile = sourceLoc != CardLocation.Hand && sourceLoc != CardLocation.Field;
                    Quaternion startRot = (sourceLoc == CardLocation.Hand) ? Quaternion.Euler(0, 0, isPlayer ? 0 : 180) : Quaternion.identity;
                    Vector3 sScale = (sourceLoc == CardLocation.Hand) ? handCardScale : fieldCardScale;
                    
                    bool sFaceUp = !isFaceDown;
                    if (sourceLoc == CardLocation.Hand) sFaceUp = isPlayer || showOpponentHand;
                    else if (sourceLoc == CardLocation.Graveyard || sourceLoc == CardLocation.Banished) sFaceUp = true;
                    else if (sourceLoc == CardLocation.Deck || sourceLoc == CardLocation.ExtraDeck) sFaceUp = false;

                    DuelFXManager.Instance.PlayCardFlight(cardData, cardBackTexture, sFaceUp, !isFaceDown, sourcePos.Value, cardGO.transform.position, 
                        sScale, fieldCardScale, 
                        startRot, endRot, settings, popFromPile, flightCallback);
                }
                else playEffects();
            }
            else playEffects();
        }

        // 5. Pontuação
        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordSummon();
        
        // TROFÉU: Tributo
        if (isTributeSummon && TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("tribute_summon", 1);

        RefreshAttackIndicators();
        RefreshAllCardsVisuals();
    }
    // --- SISTEMA DE RITUAL ---

    public void BeginRitualSummon(CardDisplay sourceCard)
    {
        bool isPlayer = sourceCard.isPlayerCard;
        var handData = isPlayer ? GetPlayerHandData() : GetOpponentHandData();
        
        List<CardData> possibleRituals = RitualManager.Instance.GetPossibleRitualMonsters(sourceCard.CurrentCardData, handData);

        if (possibleRituals.Count == 0)
        {
            Debug.Log("Nenhum Monstro de Ritual válido na mão para esta magia.");
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
            return;
        }

        // Bypass para a IA não travar a tela esperando clique
        if (!isPlayer || isSimulating)
        {
            CardData chosenRitual = possibleRituals[0];
            List<CardData> tributes = new List<CardData>();
            int sum = 0;
            foreach (var c in handData) {
                if (c != chosenRitual && c.level > 0) {
                    tributes.Add(c);
                    sum += c.level;
                    if (sum >= chosenRitual.level) break;
                }
            }
            if (sum >= chosenRitual.level) {
                PerformRitualSummon(sourceCard, chosenRitual, tributes, false);
            } else {
                SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                Destroy(sourceCard.gameObject);
            }
            return;
        }

        // Se a opção estiver ativa, desvia o fluxo para os painéis customizados!
        if (useCustomRitualUI)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowRitualUI(sourceCard);
            return;
        }

        List<CardData> possibleTributes = new List<CardData>();
        possibleTributes.AddRange(handData.Where(c => c.type.Contains("Monster")));
        Transform[] playerZones = duelFieldUI != null ? duelFieldUI.playerMonsterZones : new Transform[0];
        foreach (var zone in playerZones) {
            if (zone.childCount > 0) {
                var cd = zone.GetComponentInChildren<CardDisplay>();
                if (cd != null) possibleTributes.Add(cd.CurrentCardData);
            }
        }

        // Passo 1: Selecionar o Monstro de Ritual PRIMEIRO
        if (possibleRituals.Count == 1 && !alwaysConfirmSingleTarget)
        {
            SelectTributesForRitual(sourceCard, possibleRituals[0], possibleTributes);
        }
        else
        {
            if (useDirectHandSelection)
            {
                StartDirectSelection(possibleRituals, 1, 1, null, "Selecione o Monstro de Ritual", (rList) => {
                    if (rList != null && rList.Count > 0)
                    {
                        SelectTributesForRitual(sourceCard, rList[0], possibleTributes);
                    }
                    else
                    {
                        SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                        Destroy(sourceCard.gameObject);
                    }
                }, HighlightCategory.Ritual);
            }
            else
            {
                UIManager.Instance.ShowCardSelection(possibleRituals, "Selecione o Monstro de Ritual", 1, 1, (rList) => {
                    if (rList != null && rList.Count > 0)
                    {
                        SelectTributesForRitual(sourceCard, rList[0], possibleTributes);
                    }
                    else
                    {
                        SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                        Destroy(sourceCard.gameObject);
                    }
                });
            }
        }
    }

    private void SelectTributesForRitual(CardDisplay sourceCard, CardData targetRitual, List<CardData> possibleTributes)
    {
        bool isPlayer = sourceCard.isPlayerCard;
        
        // Remove o próprio monstro de ritual escolhido da lista de possíveis tributos
        List<CardData> validTributes = new List<CardData>(possibleTributes);
        validTributes.Remove(targetRitual);

        System.Func<List<CardData>, bool> tributeValidator = (selectedTributes) => {
            bool baseValid = false;
            if (RitualManager.Instance != null) {
                baseValid = RitualManager.Instance.ValidateRitual(sourceCard.CurrentCardData, targetRitual, selectedTributes);
            } else {
                baseValid = selectedTributes.Sum(c => c.level) >= targetRitual.level;
            }

            if (!baseValid) return false;

            // Regra OCG Rigorosa: Sem sacrifícios redundantes. Se a remoção de QUALQUER carta da seleção
            // ainda mantiver o nível exigido, a seleção inteira é considerada ilegal.
            int totalLevel = 0;
            List<int> levels = new List<int>();
            foreach(var t in selectedTributes) {
                int lvl = t.level; CardDisplay cd = FindCardOnField(t.id, true); if (cd != null) lvl = cd.currentLevel;
                levels.Add(lvl); totalLevel += lvl;
            }
            foreach(int lvl in levels) if (totalLevel - lvl >= targetRitual.level) return false;
            return true;
        };

        StartDirectSelection(validTributes, 1, 5, tributeValidator, $"Selecione os Tributos para {targetRitual.name}", (selectedTributes) => {
            if (selectedTributes == null || selectedTributes.Count == 0 || !tributeValidator(selectedTributes)) {
                if (UIManager.Instance != null && !isSimulating) UIManager.Instance.ShowMessage("Tributos cancelados ou inválidos!");
                SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                Destroy(sourceCard.gameObject);
                return;
            }

            PerformRitualSummon(sourceCard, targetRitual, selectedTributes, false);
        }, HighlightCategory.Ritual);
    }

   public void PerformRitualSummon(CardDisplay sourceCard, CardData ritualMonster, List<CardData> tributes, bool isDefense)
    {
        if (ritualMonster == null || tributes == null || tributes.Count == 0)
        {
            Debug.LogError("PerformRitualSummon: Dados inválidos.");
            return;
        }

        Debug.Log($"Realizando Invocação-Ritual de {ritualMonster.name}...");

        if (disableRitualCost) Debug.Log("[Cheat] Ritual Cost Disabled: Tributos e Magia preservados.");

        // 1. Envia a Magia de Ritual para o cemitério
        if (!disableRitualCost)
        {
            SendToGraveyard(sourceCard.CurrentCardData, sourceCard.isPlayerCard, CardLocation.Field, SendReason.Effect);
            Destroy(sourceCard.gameObject);
        }

        // 2. Envia os tributos para o cemitério (da mão ou do campo)
        foreach (var tribute in tributes)
        {
            if (disableRitualCost) break;

            var handObject = playerHand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData == tribute);
            if (handObject != null)
            {
                DiscardCard(handObject.GetComponent<CardDisplay>());
            }
            else
            {
                var fieldObject = FindCardOnField(tribute.id, true);
                if (fieldObject != null) TributeCard(fieldObject);
            }
        }

        // 3. Invoca o Monstro de Ritual da mão
        RemoveCardFromHand(ritualMonster, sourceCard.isPlayerCard);
       
       System.Action<bool> doSummon = (def) => {
           // Usa FinalizeSummon para ativar a Cinemática de Ritual Oficialmente
           GameObject cardGO = Instantiate(cardPrefab);
           CardDisplay display = cardGO.GetComponent<CardDisplay>();
           display.SetCard(ritualMonster, cardBackTexture, true);
           display.isPlayerCard = sourceCard.isPlayerCard;

           FinalizeSummon(cardGO, ritualMonster, def, sourceCard.isPlayerCard, false, false, null, tributes);
       };

       if (sourceCard.isPlayerCard && UIManager.Instance != null && !isSimulating)
       {
           UIManager.Instance.ShowPositionSelection(ritualMonster, (pos) => {
               doSummon(pos == CardDisplay.BattlePosition.Defense);
           });
       }
       else
       {
           doSummon(isDefense);
       }

        // TROFÉU: Ritual
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("ritual_summon", 1);
    }
    // Sistema de Tokens (Scapegoat, etc)
    public void SpawnToken(bool forPlayer, int atk, int def, string name, int level = 1, string race = "Token", string attribute = "Earth")
    {
        Transform targetZone = GetFreeMonsterZone(forPlayer);
        if (targetZone == null) return; // Campo cheio

        GameObject tokenGO = Instantiate(tokenPrefab != null ? tokenPrefab : cardPrefab, targetZone);
        CardDisplay display = tokenGO.GetComponent<CardDisplay>();
        if (display == null) display = tokenGO.AddComponent<CardDisplay>();

        // Cria dados fictícios para o Token
        CardData tokenData = new CardData();
        tokenData.id = "TOKEN";
        tokenData.name = name;
        tokenData.type = "Monster (Normal)";
        tokenData.race = race;
        tokenData.attribute = attribute;
        tokenData.atk = atk;
        tokenData.def = def;
        tokenData.level = level;
        tokenData.description = "Token Monster";

        display.isPlayerCard = forPlayer;
        display.isOnField = true;
        display.position = CardDisplay.BattlePosition.Defense;
        display.SetCard(tokenData, cardBackTexture, true); // Face-up

        // Configuração Visual
        tokenGO.transform.localPosition = Vector3.zero;
        tokenGO.transform.localScale = fieldCardScale;
        float zRotation = forPlayer ? 90f : -90f; // Defesa
        tokenGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);

        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySummonEffect(display);
    }
    // Método para Seleção Única (Mantido para compatibilidade, redireciona para Multi)


    // Invocação Especial direta por dados (para Monster Reborn, etc)

    private CardDisplay DefaultSpecialSummon(CardData data, bool forPlayer, bool faceUp = true, bool defense = false)
    {
        Transform targetZone = GetFreeMonsterZone(forPlayer);
        if (targetZone == null) return null;

        GameObject cardGO = Instantiate(cardPrefab, targetZone);
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display == null) display = cardGO.AddComponent<CardDisplay>();

        display.isPlayerCard = forPlayer;
        display.isOnField = true;
        display.position = defense ? CardDisplay.BattlePosition.Defense : CardDisplay.BattlePosition.Attack;
        display.SetCard(data, cardBackTexture, faceUp);

        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = fieldCardScale;

        float zRot = 0;
        if (defense) zRot = forPlayer ? 90f : -90f;
        else zRot = forPlayer ? 0f : 180f;

        cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRot);

        bool isFusion = data.type.Contains("Fusion");
        bool isRitual = data.type.Contains("Ritual");
        bool useCinematic = false;
        
        if (isFusion) useCinematic = enableFusionCinematic && !isSimulating;
        else if (isRitual) useCinematic = enableRitualCinematic && !isSimulating;
        else useCinematic = enableSummonCinematics && !isSimulating;

        if (useCinematic && DuelFXManager.Instance != null)
        {
            display.SetVisibility(false);
            
            System.Action onCinematicComplete = () => {
                display.SetVisibility(true);
                DuelFXManager.Instance.PlaySummonAura(display);
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnSpecialSummon(display);
            };

            if (isFusion)
            {
                DuelFXManager.Instance.PlayFusionCinematic(display, null, null, onCinematicComplete);
            }
            else if (isRitual)
            {
                DuelFXManager.Instance.PlayRitualCinematic(display, null, onCinematicComplete);
            }
            else
            {
                DuelFXManager.Instance.PlaySummonCinematic(display, false, true, onCinematicComplete);
            }
        }
        else
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySummonEffect(display);
            if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnSpecialSummon(display);
        }
        
        // TROFÉU: Special Summon
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("special_summon", 1);

        return display;
    }
}