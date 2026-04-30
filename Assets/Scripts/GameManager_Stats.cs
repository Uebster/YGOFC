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
    // Status de Batalha
    // ==============================================================================

    public bool PayLifePoints(bool isPlayer, int amount)
    {
        int currentLP = isPlayer ? playerLP : opponentLP;
        if (currentLP < amount) return false; // Não pode pagar

        if (isPlayer) {
            playerLP -= amount;
            lpPaidThisTurnPlayer += amount;
        }
        else {
            opponentLP -= amount;
            lpPaidThisTurnOpponent += amount;
        }

        UpdateLPUI();
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} pagou {amount} LP.");

        if (CardEffectManager.Instance != null)
        {
            EventData ed = new EventData(null, isPlayer ? 0 : 1, amount, null, 0, isPlayer ? 0 : 1);
            CardEffectManager.Instance.TriggerLuaEvent(1201, ed); // EVENT_PAY_LPCOST
        }

        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, false, isPlayer);
        }

        return true;
    }

    public void GainLifePoints(bool isPlayer, int amount)
    {
        if (isPlayer) playerLP += amount;
        else opponentLP += amount;

        UpdateLPUI();
        
        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, true, isPlayer);
        }
        
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} ganhou {amount} LP.");

        // Notifica sistema de efeitos (Ex: Fire Princess)
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnLifePointsGained(isPlayer, amount);
    }
    
    // Ferramenta de DEV/QA: Lê a descrição de uma carta e injeta dependências mencionadas entre aspas no Deck
    public void Dev_InjectDependencies(CardData data)
    {
        if (data == null || string.IsNullOrEmpty(data.description)) return;

        var matches = System.Text.RegularExpressions.Regex.Matches(data.description, "\"([^\"]+)\"");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string mentionedName = match.Groups[1].Value;
            CardData dependency = cardDatabase.cardDatabase.Find(c => c.name == mentionedName);
            if (dependency != null && dependency.name != data.name)
            {
                if (dependency.type.Contains("Fusion") || dependency.type.Contains("Synchro") || dependency.type.Contains("Xyz") || dependency.type.Contains("Link"))
                {
                    if (!playerExtraDeck.Contains(dependency)) playerExtraDeck.Add(dependency);
                    Debug.Log($"<color=orange>🔗 [QA] Dependência detectada: Adicionada 1 cópia de '{dependency.name}' ao Extra Deck. (Novo tamanho do Extra Deck: {playerExtraDeck.Count})</color>");
                }
                else
                {
                    GetPlayerMainDeck().Add(dependency);
                    GetPlayerMainDeck().Add(dependency);
                    GetPlayerMainDeck().Add(dependency);
                    Debug.Log($"<color=orange>🔗 [QA] Dependência detectada: Adicionadas 3 cópias de '{dependency.name}' ao Baralho Principal. (Novo tamanho do Deck: {GetPlayerMainDeck().Count})</color>");
                }
            }
            else 
            { 
                // Ignora falsos positivos de Arquétipos comuns
                if (!mentionedName.Contains("Archfiend") && !mentionedName.Contains("Toon") && !mentionedName.Contains("Gravekeeper's") && !mentionedName.Contains("Spirit Message") && !mentionedName.Contains("Amazoness"))
                    Debug.LogWarning($"<color=yellow>⚠️ [QA] Dependência '{mentionedName}' solicitada, mas não encontrada no DB (Pode ser um Arquétipo).</color>"); 
            }
        }
        
        if (DeckManager.Instance != null) DeckManager.Instance.UpdateDeckVisuals();
    }

    public void UpdateCardViewer(CardDisplay hoveredCard, bool isFaceUp)
    {
        if (cardViewerDisplay == null) return;

        if (isFaceUp && hoveredCard != null && hoveredCard.CurrentCardData != null)
        {
            // Otimização: Só recarrega a carta inteira se for uma diferente ou se estava mostrando o verso
            if (cardViewerShowingBack || cardViewerDisplay.CurrentCardData == null || cardViewerDisplay.CurrentCardData.id != hoveredCard.CurrentCardData.id)
            {
                cardViewerDisplay.SetCard(hoveredCard.CurrentCardData, cardBackTexture, true);
                cardViewerShowingBack = false;
            }
            
            // ATUALIZAÇÃO: Injeta os valores dinâmicos (ATK/DEF/LVL) da carta sob o mouse para o viewer
            if (hoveredCard.CurrentCardData.type.Contains("Monster"))
            {
                cardViewerDisplay.currentAtk = hoveredCard.currentAtk;
                cardViewerDisplay.currentDef = hoveredCard.currentDef;
                cardViewerDisplay.currentLevel = hoveredCard.currentLevel;
                cardViewerDisplay.originalLevel = hoveredCard.originalLevel;
                cardViewerDisplay.SendMessage("DisplayCardDetails", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            // Se a carta estiver virada para baixo ou for do oponente, mostra o verso
            cardViewerDisplay.SetCardBackOnly(cardBackTexture);
            cardViewerShowingBack = true;
        }

        // Força a atualização visual do Card Viewer para aplicar as configurações de borda/arredondamento
        // Isso é importante se as configurações mudarem em tempo real ou se o estado da carta mudar
        // O método SetCard já chama ApplyRoundedCorners, mas podemos garantir aqui também se necessário.
    }

    public void ClearCardViewer()
    {
        if (cardViewerDisplay == null) return;
        cardViewerDisplay.SetCardBackOnly(cardBackTexture);
        cardViewerShowingBack = true;
    }

    public void RefreshAllCardsVisuals()
    {
        List<GameObject> allCards = new List<GameObject>();
        allCards.AddRange(playerHand);
        allCards.AddRange(opponentHand);
        if (duelFieldUI != null) {
            if (duelFieldUI.playerMonsterZones != null) foreach (var z in duelFieldUI.playerMonsterZones) if (z != null && z.childCount > 0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            if (duelFieldUI.opponentMonsterZones != null) foreach (var z in duelFieldUI.opponentMonsterZones) if (z != null && z.childCount > 0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            if (duelFieldUI.playerSpellZones != null) foreach (var z in duelFieldUI.playerSpellZones) if (z != null && z.childCount > 0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
            if (duelFieldUI.opponentSpellZones != null) foreach (var z in duelFieldUI.opponentSpellZones) if (z != null && z.childCount > 0) { var cd = z.GetComponentInChildren<CardDisplay>(); if (cd != null) allCards.Add(cd.gameObject); }
        }

        // COLETAR TODAS AS AURAS ATIVAS NO CAMPO (FIELD e EQUIP)
        List<LuaEffect> activeAuras = new List<LuaEffect>();
        if (CardEffectManager.Instance != null)
        {
            foreach (var go in allCards)
            {
                CardDisplay auraSource = go.GetComponent<CardDisplay>();
                if (auraSource != null && auraSource.isOnField && !auraSource.isFlipped)
                {
                    LuaCard sourceLc = CardEffectManager.Instance.EnsureCardScriptLoaded(auraSource) ?? new LuaCard(auraSource);
                    if (sourceLc != null && sourceLc.registeredEffects != null)
                    {
                        foreach (var eff in sourceLc.registeredEffects.Where(e => e.isTypeField || e.isTypeEquip))
                        {
                            activeAuras.Add(eff);
                        }
                    }
                }
            }
        }

        foreach (var go in allCards)
        {
            if (go != null)
            {
                CardDisplay cd = go.GetComponent<CardDisplay>();
                if (cd != null && cd.CurrentCardData != null)
                {
                    // 1. Reseta os valores para a base original
                    cd.ResetStatsToOriginal();
                    
                    if (cd.CurrentCardData.type.Contains("Monster"))
                    {
                        // Puxa o Script do monstro da memória para ler se ele sofreu alguma alteração temporária
                        LuaCard lc = CardEffectManager.Instance != null ? CardEffectManager.Instance.EnsureCardScriptLoaded(cd) : new LuaCard(cd);
                        if (lc == null) lc = new LuaCard(cd);
                        
                        // 1.5 Processa modificadores SINGLE próprios da carta (SET_BASE_ATK, etc)
                        int baseAtk = lc.GetBaseAttack();
                        int baseDef = lc.GetBaseDefense();

                        if (CardEffectManager.Instance != null)
                        {
                            var singleEffects = lc.registeredEffects.FindAll(e => e.isTypeSingle); // EFFECT_TYPE_SINGLE
                            foreach(var eff in singleEffects)
                            {
                                if (eff.singleRange && (eff.range & lc.GetLocation()) == 0) continue; // EFFECT_FLAG_SINGLE_RANGE

                                int val = 0;
                                object valObj = eff.GetValue();
                                if (valObj is double || valObj is long) val = System.Convert.ToInt32(valObj);
                                else if (valObj is MoonSharp.Interpreter.Closure valClosure)
                                {
                                    try {
                                        var res = CardEffectManager.Instance.luaEngine.Call(valClosure, eff, lc);
                                        if (res.Type == MoonSharp.Interpreter.DataType.Number) val = (int)res.Number;
                                    } catch { }
                                }

                                if (eff.code == 1 || eff.code == 100) baseAtk += val; // UPDATE_ATTACK
                                else if (eff.code == 4 || eff.code == 104) baseDef += val; // UPDATE_DEFENSE
                                else if (eff.code == 2) baseAtk = val; // SET_ATTACK
                                else if (eff.code == 5) baseDef = val; // SET_DEFENSE
                            }
                        }

                        cd.currentAtk = baseAtk;
                        cd.currentDef = baseDef;
                        
                        // 2. Modificadores FIELD (Auras Globais LUA e Equipamentos)
                        foreach (var aura in activeAuras)
                        {
                            if (IsTargetOfAura(aura, lc))
                            {
                                int val = EvaluateEffectValue(aura, aura.owner, lc);
                                if (aura.code == 1 || aura.code == 100) cd.currentAtk += val;
                                else if (aura.code == 4 || aura.code == 104) cd.currentDef += val;
                                else if (aura.code == 2 || aura.code == 101) cd.currentAtk = val;
                                else if (aura.code == 5 || aura.code == 105) cd.currentDef = val;
                                else if (aura.code == 130) cd.currentLevel += val;
                                else if (aura.code == 131) cd.currentLevel = val;
                                else if (aura.code == 203) cd.hasPiercing = true; // EFFECT_PIERCE
                            }
                        }

                        // 4. Aplica modificadores SINGLE Finais
                        if (CardEffectManager.Instance != null)
                        {
                            var singleEffects = lc.registeredEffects.FindAll(e => e.isTypeSingle);
                            foreach(var eff in singleEffects)
                            {
                                if (eff.singleRange && (eff.range & lc.GetLocation()) == 0) continue; // EFFECT_FLAG_SINGLE_RANGE

                                if (eff.code == 3 || eff.code == 102) cd.currentAtk = EvaluateEffectValue(eff, lc, lc);
                                else if (eff.code == 6 || eff.code == 106) cd.currentDef = EvaluateEffectValue(eff, lc, lc);
                                else if (eff.code == 203) cd.hasPiercing = true; // EFFECT_PIERCE
                            }
                        }
                        
                        CardLocation loc = cd.isOnField ? CardLocation.Field : CardLocation.Hand;
                        cd.currentLevel = cd.originalLevel + CardEffectManager.Instance.auraManager.GetStatModifier(lc, "LEVEL", loc);
                    }
                    
                    // 4. Força a atualização dos textos na interface da carta
                    cd.SendMessage("DisplayCardDetails", SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    // Função de DEV para alternar visibilidade da mão do oponente
    public void ToggleOpponentHandVisibility()
    {
        if (!devMode) return;

        showOpponentHand = !showOpponentHand; // Alterna o estado

        foreach (GameObject cardGO in opponentHand)
        {
            CardDisplay display = cardGO.GetComponent<CardDisplay>();
            if (display != null)
            {
                if (showOpponentHand) display.ShowFront();
                else display.ShowBack();
            }
        }
    }

    // --- FUTURAS IMPLEMENTAÇÕES DE COMANDOS (COMENTÁRIOS) ---
    /*
    public void EnterBattlePhase() {
        // Lógica: Clicar no botão "Battle Phase" ou Botão Direito em campo vazio durante Main1
        ChangePhase(GamePhase.Battle);
    }

    public void EndTurn() {
        // Lógica: Clicar em "End Phase" ou Botão Direito em campo vazio durante Main2 (ou Battle se pular M2)
        ChangePhase(GamePhase.End);
        // Trocar turno para oponente...
    }

    // Comandos de Jogo:
    // Summon: Arrastar carta de monstro para zona de monstro (Face Up Attack)
    // Set: Botão direito na carta da mão -> "Set" (Face Down Defense)
    // Activate: Clicar em Spell/Trap -> "Activate"
    // Surrender: Botão direito no Deck -> "Surrender"
    */



    // Chamado automaticamente no Editor quando você muda um valor
    void OnValidate()
    {
        // Atualiza visibilidade da mão do oponente em tempo real se alterar o showOpponentHand no Inspector
        if (Application.isPlaying && opponentHand != null)
        {
            foreach (GameObject cardGO in opponentHand)
            {
                if (cardGO != null)
                {
                    CardDisplay display = cardGO.GetComponent<CardDisplay>();
                    if (display != null)
                    {
                        if (showOpponentHand) display.ShowFront();
                        else display.ShowBack();
                    }
                }
            }
        }
    }

    // Alterna entre Tela Cheia e Modo Janela
    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        Debug.Log($"[GameManager] Fullscreen alterado para: {Screen.fullScreen}");
    }

    // --- SISTEMA DE DANO E LP ---

    public void DamagePlayer(int amount)
    {
        if (infiniteLP) return;

        playerLP -= amount;
        if (playerLP < 0) playerLP = 0;
        UpdateLPUI();

        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, false, true);
        }

        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordDamageTaken(amount);
        Debug.Log($"Player tomou {amount} de dano. LP Restante: {playerLP}");

        // TROFÉU: Dano Sofrido
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("damage_taken", amount);

        // Atualiza a música baseada na nova situação de vida
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.UpdateBGM(playerLP, opponentLP);

        if (playerLP <= 0) EndDuel(false);

        // Notifica dano (para cartas como Numinous Healer, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageTaken(true, amount);
        }
    }

    public void DamageOpponent(int amount)
    {
        opponentLP -= amount;
        if (opponentLP < 0) opponentLP = 0;
        UpdateLPUI();

        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, false, false);
        }

        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordDamageDealt(amount);
        Debug.Log($"Oponente tomou {amount} de dano. LP Restante: {opponentLP}");

        // TROFÉU: Dano Causado
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("damage_dealt", amount);

        // Atualiza a música baseada na nova situação de vida
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.UpdateBGM(playerLP, opponentLP);

        if (opponentLP <= 0) EndDuel(true);

        // Notifica dano
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageTaken(false, amount);
        }
    }

    private void UpdateLPUI()
    {
        if (duelFieldUI != null)
        {
            if (duelFieldUI.playerLPText != null) 
                duelFieldUI.playerLPText.text = playerLP.ToString();
            else 
                Debug.LogWarning("GameManager: PlayerLPText não atribuído no DuelFieldUI.");

            if (duelFieldUI.opponentLPText != null) 
                duelFieldUI.opponentLPText.text = opponentLP.ToString();
            else
                Debug.LogWarning("GameManager: OpponentLPText não atribuído no DuelFieldUI.");
        }
        else
        {
            Debug.LogWarning("GameManager: Tentativa de atualizar LP, mas DuelFieldUI é nulo.");
        }
    }

    public Transform GetFreeMonsterZone(bool isPlayer)
    {
        if (duelFieldUI == null) return null;
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        if (zones == null) return null;

        // Se for oponente e a opção estiver marcada, inverte a ordem de busca
        if (!isPlayer && fillOpponentZonesFromRight)
        {
            for (int i = zones.Length - 1; i >= 0; i--)
            {
                if (zones[i].childCount == 0) return zones[i];
            }
        }
        else
        {
            foreach (Transform zone in zones)
            {
            if (zone.childCount == 0 && !duelFieldUI.IsZoneBlocked(zone)) return zone;
            }
        }
        return null;
    }

    public Transform GetFreeSpellZone(bool isPlayer)
    {
        if (duelFieldUI == null) return null;
        Transform[] zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        if (zones == null) return null;

        // Se for oponente e a opção estiver marcada, inverte a ordem de busca
        if (!isPlayer && fillOpponentZonesFromRight)
        {
            for (int i = zones.Length - 1; i >= 0; i--)
            {
                if (zones[i].childCount == 0) return zones[i];
            }
        }
        else
        {
            foreach (Transform zone in zones)
            {
                if (zone.childCount == 0) return zone;
            }
        }
        return null;
    }

    void OnDisable()
    {
        // Garante que a requisição seja cancelada se o objeto for desativado
        if (backTextureRequest != null && !backTextureRequest.isDone)
        {
            backTextureRequest.Dispose();
            backTextureRequest = null;
        }
    }

    void OnDestroy()
    {
        // Garante que a requisição seja limpa se o jogo parar abruptamente
        if (backTextureRequest != null)
        {
            backTextureRequest.Dispose();
            backTextureRequest = null;
        }

    }

    // Ferramenta de DEV para trocar oponente
    [ContextMenu("DEV: Next Opponent")]
    public void Dev_NextOpponent()
    {
        if (!devMode || campaignDatabase == null) return;

        int actIndex = 0;
        int oppIndex = 0;
        bool found = false;

        // Encontra o índice do oponente atual
        if (currentOpponent != null)
        {
            for(int i=0; i<campaignDatabase.acts.Count; i++)
            {
                for(int j=0; j<campaignDatabase.acts[i].opponentIDs.Count; j++)
                {
                    if (campaignDatabase.acts[i].opponentIDs[j] == currentOpponent.id)
                    {
                        actIndex = i;
                        oppIndex = j;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }

        // Avança para o próximo
        oppIndex++;
        if (oppIndex >= campaignDatabase.acts[actIndex].opponentIDs.Count)
        {
            oppIndex = 0;
            actIndex++;
            if (actIndex >= campaignDatabase.acts.Count) actIndex = 0;
        }

        string nextID = campaignDatabase.acts[actIndex].opponentIDs[oppIndex];
        CharacterData nextChar = characterDatabase.GetCharacterById(nextID);
        
        if (nextChar != null)
        {
            Debug.Log($"Dev: Trocando para {nextChar.name} (Ato {actIndex+1})");
            StartDuel(nextChar, (actIndex * 10) + oppIndex + 1);
        }
    }

    // Intérprete Nativo de Valores (Calcula funções LUA em tempo real)
    private int EvaluateEffectValue(LuaEffect eff, LuaCard sourceCard, LuaCard targetCard)
    {
        int val = 0;
        object valObj = eff.GetValue();
        if (valObj is double || valObj is long) val = System.Convert.ToInt32(valObj);
        else if (valObj is MoonSharp.Interpreter.Closure valClosure && CardEffectManager.Instance != null)
        {
            try {
                var res = CardEffectManager.Instance.luaEngine.Call(valClosure, eff, targetCard);
                if (res.Type == MoonSharp.Interpreter.DataType.Number) val = (int)res.Number;
                else if (res.Type == MoonSharp.Interpreter.DataType.Boolean && res.Boolean) val = 1;
            } catch { }
        }
        return val;
    }

    // Filtro OCGCore Absoluto (Sabe quem deve ser afetado pela Aura)
    private bool IsTargetOfAura(LuaEffect aura, LuaCard targetCard)
    {
        if (aura.owner == null || targetCard == null) return false;
        
        // --- EFFECT_FLAG_SET_AVAILABLE ---
        // Se o alvo está virado para baixo no campo, ele é imune a auras, a não ser que a aura tenha a permissão explícita!
        if (targetCard.IsOnField() && targetCard.IsFacedown() && !aura.isSetAvailable) return false;

        if (aura.conditionFunc != null && CardEffectManager.Instance != null && aura.conditionFunc != CardEffectManager.Instance.dummyClosureTrue)
        {
            try {
                var res = CardEffectManager.Instance.luaEngine.Call(aura.conditionFunc, aura);
                if (res.Type == MoonSharp.Interpreter.DataType.Boolean && !res.Boolean) return false;
            } catch { return false; }
        }
        if (aura.type == 4) // Equipamento Físico
        {
            if (CardEffectManager.Instance != null) {
                LuaCard equippedToLua = aura.owner.GetEquipTarget(); // Força a Mágica a memorizar/cachear o alvo no LUA
                CardDisplay equippedTo = equippedToLua != null ? equippedToLua.unityCard : null;
                if (equippedTo != null && targetCard.unityCard != null && equippedTo == targetCard.unityCard) return true;
            } return false;
        }
        if (!aura.ignoreRange)
        {
            int sourcePlayer = aura.owner.GetControler(); int targetPlayer = targetCard.GetControler(); int targetLoc = targetCard.GetLocation();
            int allowedLocs = sourcePlayer == targetPlayer ? aura.targetRangeSelf : aura.targetRangeOpponent;
            if (aura.bothSide) allowedLocs = aura.targetRangeSelf | aura.targetRangeOpponent;
            if ((allowedLocs & targetLoc) == 0) return false;
        }
        
        if (aura.targetFunc != null && CardEffectManager.Instance != null && aura.targetFunc != CardEffectManager.Instance.dummyClosureTrue)
        {
            try { var res = CardEffectManager.Instance.luaEngine.Call(aura.targetFunc, aura, targetCard); if (res.Type == MoonSharp.Interpreter.DataType.Boolean && !res.Boolean) return false; } catch { return false; }
        } return true;
    }
}