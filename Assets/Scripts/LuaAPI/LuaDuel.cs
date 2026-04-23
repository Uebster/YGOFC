using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

// ==============================================================================
// 1. CLASSE DUEL (Ações Globais e Tabuleiro)
// Onde a Mágica Acontece: Intercepta os comandos 'Duel.' do OCGCore.
// Tratativas Críticas & Dependências: 
// - CardEffectManager.cs: Exige suspensão (Yields) e Corrotinas Assíncronas.
// - GameManager.cs: Executa a movimentação física 3D e interfaces UI.
// - LuaEngineCore.cs: Injeta Constantes Nativas (ex: CHAININFO_TARGET_CARDS).
// ==============================================================================
[MoonSharpUserData]
public class LuaDuel
{
    public int targetPlayer;
    public int targetParam;
    public LuaGroup currentTargetGroup;
    public LuaCard currentAttacker;
    public LuaCard currentAttackTarget;
    public LuaCard historicalAttacker; // Safety Net: Lembra quem lutou até o fim do turno
    public LuaCard historicalAttackTarget;
    public LuaEffect currentActivatingEffect;
    public LuaGroup lastCostGroup; // Memória de curto prazo para custos pagos
    public List<CardDisplay> pendingComparisonCards = new List<CardDisplay>(); // Buffer para Confrontos Cinemáticos

    public List<LuaEffect> globalEffects = new List<LuaEffect>();
    public Dictionary<string, int> playerFlags = new Dictionary<string, int>();
    public Dictionary<string, Dictionary<int, int>> cardFlags = new Dictionary<string, Dictionary<int, int>>();
    public List<Closure> endTurnCallbacks = new List<Closure>();
    public string lastHintMsg = "Selecione um alvo";
    public bool nextShuffleDisabled = false;

    private int ConvertToInt(object obj)
    {
        if (obj == null) return 0;
        if (obj is MoonSharp.Interpreter.DynValue dv)
        {
            if (dv.Type == MoonSharp.Interpreter.DataType.Number) return (int)dv.Number;
            if (dv.Type == MoonSharp.Interpreter.DataType.Boolean) return dv.Boolean ? 1 : 0;
            if (dv.Type == MoonSharp.Interpreter.DataType.String && int.TryParse(dv.String, out int res)) return res;
            return 0;
        }
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        if (obj is string s && int.TryParse(s, out int parsed)) return parsed;
        return 0;
    }

    // Utilitário de Conversão: 0 = Human Player, 1 = AI Opponent
    public bool IsPlayer(object playerIndex)
    {
        return ConvertToInt(playerIndex) == 0;
    }

    public void Damage(object player, object amount, object reason)
    {
        if (IsPlayer(player)) GameManager.Instance.DamagePlayer(ConvertToInt(amount));
        else GameManager.Instance.DamageOpponent(ConvertToInt(amount));
    }

    public void Recover(object player, object amount, object reason)
    {
        // Debug.Log($"[Surgical Log] Duel.Recover! Jogador_Raw: {player} | Amount_Raw: {amount}");
        int pInt = ConvertToInt(player);
        int aInt = ConvertToInt(amount);
        // Debug.Log($"[Surgical Log] Convertido para C# -> Jogador: {pInt} | Cura: {aInt}");
        if (pInt == 0) GameManager.Instance.GainLifePoints(true, aInt);
        else GameManager.Instance.GainLifePoints(false, aInt);
    }

    public DynValue Destroy(object target, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        List<CardDisplay> toDestroy = new List<CardDisplay>();

        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) if (c.unityCard != null) toDestroy.Add(c.unityCard);
            // Debug.Log($"[Lua] Duel.Destroy(Grupo com {group.cards.Count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            toDestroy.Add(card.unityCard);
            // Debug.Log($"[Lua] Duel.Destroy({card.unityCard.CurrentCardData.name})");
        }

        CardEffectManager.Instance.StartCoroutine(DestroyCardsRoutine(toDestroy));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Destroy") });
    }

    private Vector3 GetPilePosition(CardLocation loc, bool isPlayer)
    {
        if (GameManager.Instance == null) return Vector3.zero;
        switch (loc)
        {
            case CardLocation.Deck: return isPlayer ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
            case CardLocation.Graveyard: return isPlayer ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
            case CardLocation.ExtraDeck: return isPlayer ? GameManager.Instance.playerExtraDeckDisplay.transform.position : GameManager.Instance.opponentExtraDeckDisplay.transform.position;
            case CardLocation.Banished: return isPlayer ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
            default: return Vector3.zero;
        }
    }

    // --- MOVIMENTAÇÃO DE CARTAS ---

    public DynValue Draw(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        bool isPlayer = IsPlayer(player);

        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        CardEffectManager.Instance.StartCoroutine(DrawRoutine(isPlayer, amt));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Draw") });
    }

    private IEnumerator DrawRoutine(bool isPlayer, int amount)
    {
        if (isPlayer && GameManager.Instance != null && GameManager.Instance.canPlayerDrawFromDeck && !GameManager.Instance.isSimulating)
        {
            for (int i = 0; i < amount; i++)
            {
                int drawsLeft = amount - i;
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Efeito ativado: Compre {drawsLeft} carta(s) do seu Deck.");
                
                GameManager.Instance.pendingEffectDraws = 1;
                yield return new WaitWhile(() => GameManager.Instance.pendingEffectDraws > 0);
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                if (isPlayer) GameManager.Instance.DrawCard(true);
                else GameManager.Instance.DrawOpponentCard();
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.4f);
            }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(amount);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public bool IsPlayerCanDraw(object player, object amount = null)
    {
        int amt = amount == null ? 1 : ConvertToInt(amount);
        int deckCount = IsPlayer(player) ? GameManager.Instance.GetPlayerMainDeck().Count : GameManager.Instance.GetOpponentMainDeck().Count;
        return deckCount >= (amt > 0 ? amt : 1);
    }

    public int SendtoGrave(object target, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                {
                    GameManager.Instance.MoveCard(c.unityCard, CardLocation.Graveyard, SendReason.Effect);
                    count++;
                }
                else if (c.unityData != null)
                {
                    bool wasPlayerPile;
                    CardLocation sourceLoc = RemoveDataFromAllPiles(c.unityData, out wasPlayerPile);
                    GameManager.Instance.SendToGraveyard(c.unityData, c.GetControler() == 0, sourceLoc, SendReason.Effect);
                    
                    if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                    {
                        CardFlightSettings flightSettings = null;
                        if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToGraveyard;
                        else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToGraveyard;
                        else if (sourceLoc == CardLocation.Banished) flightSettings = DuelFXManager.Instance.flightBanishToGraveyard;

                        if (flightSettings != null && flightSettings.enableFlight)
                        {
                            Vector3 startPos = GetPilePosition(sourceLoc, wasPlayerPile);
                            Vector3 endPos = c.GetControler() == 0 ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
                            DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), true, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, flightSettings, true, null);
                        }
                    }
                    count++;
                }
            }
            // Debug.Log($"[Lua] Duel.SendtoGrave(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            if (card.unityCard != null)
            {
                GameManager.Instance.MoveCard(card.unityCard, CardLocation.Graveyard, SendReason.Effect);
                count = 1;
                // Debug.Log($"[Lua] Duel.SendtoGrave({card.unityCard.CurrentCardData.name})");
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sourceLoc = RemoveDataFromAllPiles(card.unityData, out wasPlayerPile);
                GameManager.Instance.SendToGraveyard(card.unityData, card.GetControler() == 0, sourceLoc, SendReason.Effect);
                
                if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                {
                    CardFlightSettings flightSettings = null;
                    if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToGraveyard;
                    else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToGraveyard;
                    else if (sourceLoc == CardLocation.Banished) flightSettings = DuelFXManager.Instance.flightBanishToGraveyard;

                    if (flightSettings != null && flightSettings.enableFlight)
                    {
                        Vector3 startPos = GetPilePosition(sourceLoc, wasPlayerPile);
                        Vector3 endPos = card.GetControler() == 0 ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position;
                        DuelFXManager.Instance.PlayCardFlight(card.unityData, GameManager.Instance.GetCardBackTexture(), true, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, flightSettings, true, null);
                    }
                }
                count = 1;
            }
        }
        return count;
    }

    public int Remove(object target, object pos, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) { 
                if (c.unityCard != null) { GameManager.Instance.BanishCard(c.unityCard); count++; }
                else if (c.unityData != null) { 
                    bool wasPlayerPile;
                    CardLocation sourceLoc = RemoveDataFromAllPiles(c.unityData, out wasPlayerPile); 
                    GameManager.Instance.RemoveFromPlay(c.unityData, c.GetControler() == 0); 
                    
                    if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                    {
                        CardFlightSettings flightSettings = null;
                        if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToBanished;
                        else if (sourceLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToBanished;
                        else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToBanished;

                        if (flightSettings != null && flightSettings.enableFlight)
                        {
                            Vector3 startPos = GetPilePosition(sourceLoc, wasPlayerPile);
                            Vector3 endPos = c.GetControler() == 0 ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
                            DuelFXManager.Instance.PlayCardFlight(c.unityData, GameManager.Instance.GetCardBackTexture(), true, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, flightSettings, true, null);
                        }
                    }
                    count++; 
                }
            }
            // Debug.Log($"[Lua] Duel.Remove(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null)
            {
                GameManager.Instance.BanishCard(card.unityCard);
                count = 1;
                // Debug.Log($"[Lua] Duel.Remove({card.unityCard.CurrentCardData.name})");
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile;
                CardLocation sourceLoc = RemoveDataFromAllPiles(card.unityData, out wasPlayerPile);
                GameManager.Instance.RemoveFromPlay(card.unityData, card.GetControler() == 0);
                
                if (DuelFXManager.Instance != null && !GameManager.Instance.isSimulating && sourceLoc != CardLocation.Unknown)
                {
                    CardFlightSettings flightSettings = null;
                    if (sourceLoc == CardLocation.Deck) flightSettings = DuelFXManager.Instance.flightDeckToBanished;
                    else if (sourceLoc == CardLocation.Graveyard) flightSettings = DuelFXManager.Instance.flightGraveyardToBanished;
                    else if (sourceLoc == CardLocation.ExtraDeck) flightSettings = DuelFXManager.Instance.flightExtraToBanished;

                    if (flightSettings != null && flightSettings.enableFlight)
                    {
                        Vector3 startPos = GetPilePosition(sourceLoc, wasPlayerPile);
                        Vector3 endPos = card.GetControler() == 0 ? GameManager.Instance.playerRemovedDisplay.transform.position : GameManager.Instance.opponentRemovedDisplay.transform.position;
                        DuelFXManager.Instance.PlayCardFlight(card.unityData, GameManager.Instance.GetCardBackTexture(), true, true, startPos, endPos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, flightSettings, true, null);
                    }
                }
                count = 1;
            }
        }
        return count;
    }

    public int SendtoDeck(object target, object player, object seq, object reason)
    {
        CardEffectManager.Instance.StartCoroutine(SendtoDeckRoutine(target, player, seq, reason));
        return 1; // Retorna um valor síncrono, mas a animação roda em background
    }

    private IEnumerator SendtoDeckRoutine(object target, object player, object seq, object reason)
    {
        int count = 0;
        bool toTop = (ConvertToInt(seq) == 0);
        List<CardData> cardsToAnimate = new List<CardData>();
        CardLocation capturedSourceLoc = CardLocation.Unknown;
        Vector3? capturedStartPos = null;

        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityData != null) cardsToAnimate.Add(c.unityData);

                // Lógica de dados original (executa imediatamente)
                if (c.unityCard != null) { GameManager.Instance.ReturnToDeck(c.unityCard, toTop); count++; } 
                else if (c.unityData != null) { 
                    bool wasPlayerPile; 
                    capturedSourceLoc = RemoveDataFromAllPiles(c.unityData, out wasPlayerPile); 
                    capturedStartPos = GetPilePosition(capturedSourceLoc, wasPlayerPile);
                    if(c.GetControler() == 0) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, c.unityData); 
                    else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, c.unityData); count++; 
                }
            }
            // Debug.Log($"[Lua] Duel.SendtoDeck(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null)
            {
                if (card.unityData != null) cardsToAnimate.Add(card.unityData);
                GameManager.Instance.ReturnToDeck(card.unityCard, toTop);
                count = 1;
                // Debug.Log($"[Lua] Duel.SendtoDeck({card.unityCard.CurrentCardData.name})");
            }
            else if (card.unityData != null)
            {
                bool wasPlayerPile; 
                capturedSourceLoc = RemoveDataFromAllPiles(card.unityData, out wasPlayerPile);
                capturedStartPos = GetPilePosition(capturedSourceLoc, wasPlayerPile);
                if (card.unityData != null) cardsToAnimate.Add(card.unityData);
                if(card.GetControler() == 0) GameManager.Instance.GetPlayerMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetPlayerMainDeck().Count, card.unityData); 
                else GameManager.Instance.GetOpponentMainDeck().Insert(toTop ? 0 : GameManager.Instance.GetOpponentMainDeck().Count, card.unityData);
                count = 1;
            }
        }

        // Dispara a animação visual após a lógica de dados
        if (GameManager.Instance != null && GameManager.Instance.enableReturnToDeckAnimation && cardsToAnimate.Count > 0)
        {
            bool animDone = false;
            
            CardLocation sourceLoc = CardLocation.Unknown;
            Vector3? startPos = null;
            if (target is LuaCard tCard && tCard.unityCard != null) { sourceLoc = CardLocation.Field; startPos = tCard.unityCard.transform.position; }
            else { sourceLoc = capturedSourceLoc; startPos = capturedStartPos; }
            
            if (DuelFXManager.Instance != null) {
                DuelFXManager.Instance.PlayReturnToDeckAnimation(cardsToAnimate[0], IsPlayer(player), sourceLoc, startPos, () => animDone = true);
            } else { animDone = true; }
            yield return new WaitUntil(() => animDone);
        }

        if (!toTop && GameManager.Instance != null) GameManager.Instance.ShuffleDeck(IsPlayer(player));

        if (DeckManager.Instance != null) DeckManager.Instance.UpdateDeckVisuals();
    }

    public void ShuffleDeck(object player)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ShuffleDeck(IsPlayer(player));
    }

    public void ShuffleHand(object player)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ShuffleHand(IsPlayer(player));
    }

    public DynValue DiscardHand(object player, object filter, object min, object max, object reason, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        bool isPlayer = IsPlayer(player);
        int minAmt = ConvertToInt(min);
        int maxAmt = ConvertToInt(max);
        
        List<GameObject> hand = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
        List<CardData> validCards = new List<CardData>();
        
        object excluded = extraArgs != null && extraArgs.Length > 0 ? extraArgs[0] : null;

        foreach (var go in hand)
        {
            CardDisplay cd = go.GetComponent<CardDisplay>();
            if (cd != null)
            {
                if (excluded != null)
                {
                    if (excluded is LuaCard excCard && cd == excCard.unityCard) continue;
                    if (excluded is LuaGroup excGroup && excGroup.cards.Exists(c => c.unityCard == cd)) continue;
                }

                if (filter is Closure closure)
                {
                    try {
                        DynValue res = closure.Call(new LuaCard(cd));
                        if (res.Type == DataType.Boolean && res.Boolean) validCards.Add(cd.CurrentCardData);
                    } catch (System.Exception ex) {
                        Debug.LogWarning($"[LuaDuel] Erro ao executar filterFunc em DiscardHand para a carta {cd.CurrentCardData?.name}: {ex.Message}");
                    }
                }
                else
                {
                    validCards.Add(cd.CurrentCardData);
                }
            }
        }

        if (validCards.Count == 0 || (!isPlayer && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy))
        {
            int countToDiscard = Mathf.Min(minAmt, hand.Count);
            LuaGroup autoDiscardedGroup = new LuaGroup();
            for (int i = 0; i < countToDiscard; i++)
            {
                if (hand.Count > 0)
                {
                    var cd = hand[0].GetComponent<CardDisplay>();
                    autoDiscardedGroup.AddCard(new LuaCard(cd));
                    GameManager.Instance.DiscardCard(cd);
                }
            }
            this.lastCostGroup = autoDiscardedGroup;
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(countToDiscard);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("DiscardHand") });
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OpenCardMultiSelection(validCards, $"Descarte entre {minAmt} e {maxAmt} carta(s)", minAmt, maxAmt, (selectedCards) => {
                int discardedCount = 0;
                LuaGroup selectedGroup = new LuaGroup();
                if (selectedCards != null)
                {
                    foreach (var cData in selectedCards)
                    {
                        var cardGo = hand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == cData);
                        if (cardGo != null)
                        {
                            GameManager.Instance.DiscardCard(cardGo.GetComponent<CardDisplay>());
                            selectedGroup.AddCard(new LuaCard(cardGo.GetComponent<CardDisplay>()));
                            discardedCount++;
                        }
                    }
                }
                this.lastCostGroup = selectedGroup;
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(discardedCount);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            }, HighlightCategory.GenericTarget, false);
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("DiscardHand") });
    }

    public void Release(object target, object reason)
    {
        if (target is LuaGroup group)
        {
            this.lastCostGroup = group; // Armazena o grupo para exclusão de alvo
            foreach (var c in group.cards)
                if (c.unityCard != null) GameManager.Instance.TributeCard(c.unityCard);
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            LuaGroup tempGroup = new LuaGroup();
            tempGroup.AddCard(card);
            this.lastCostGroup = tempGroup; // Armazena a carta para exclusão de alvo
            GameManager.Instance.TributeCard(card.unityCard);
        }
    }

    public int GetLocationCount(object player, object location, params object[] extraArgs)
    {
        if (GameManager.Instance == null) return 0;
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        int result = 5;
        
        if (loc == 0x04) // LOCATION_MZONE
            result = GameManager.Instance.GetFreeMonsterZones(isPlayer);
        if (loc == 0x08) // LOCATION_SZONE
            result = GameManager.Instance.GetFreeSpellTrapZones(isPlayer);
            
        // Debug.Log($"[LuaDuel] GetLocationCount consultado: Player {player}, Loc {loc} -> Result: {result}");
        return result;
    }

    public int GetTurnPlayer()
    {
        // Retorna 0 se for o turno do Jogador Humano, 1 se for da IA
        return GameManager.Instance.isPlayerTurn ? 0 : 1;
    }

    public int GetPhase() { return GetCurrentPhase(); }

    public int GetCurrentPhase()
    {
        if (PhaseManager.Instance == null) return 0;
        
        // Tradução direta para a tabela hexadecimal do constant.lua
        switch (PhaseManager.Instance.currentPhase)
        {
            case GamePhase.Draw: return 0x01;       // PHASE_DRAW
            case GamePhase.Standby: return 0x02;    // PHASE_STANDBY
            case GamePhase.Main1: return 0x04;      // PHASE_MAIN1
            case GamePhase.Battle: return 0x80;     // PHASE_BATTLE
            case GamePhase.Main2: return 0x100;     // PHASE_MAIN2
            case GamePhase.End: return 0x200;       // PHASE_END
            default: return 0;
        }
    }

    public bool IsPhase(object phaseObj)
    {
        int phase = 0;
        if (phaseObj is double d) phase = (int)d;
        else if (phaseObj is int i) phase = i;
        else if (phaseObj is long l) phase = (int)l;
        return GetCurrentPhase() == phase;
    }

    public void Hint(object msgType, object player, object desc) { 
        int type = ConvertToInt(msgType);
        if (type == 3) // HINT_SELECTMSG
            lastHintMsg = GetHintMessageString(ConvertToInt(desc));
    }
    public void AddCustomActivityCounter(object counter_id, object activity_type, object filter) { }
    public void EnableGlobalFlag(object flag) { }

    public void SetOperationInfo(object chainc, object category, object target, object count, object player, object param) 
    { 
        int idx = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.currentChain.Count >= idx && idx > 0)
        {
            // Guarda a informação na memória do elo (Usado por Stardust Dragon, My Body as a Shield, etc)
            // CardEffectManager.Instance.currentChain[idx - 1].operationInfo = ...
        }
    }
    
    // Extensões Descobertas pelo Mass Validator
    public bool CheckReleaseGroupCost(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) 
    { 
        int locSelf = 0x04 | (ConvertToInt(use_hand) != 0 ? 0x02 : 0);
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, 0, excluded, extraArgs);
        return group.GetCount() >= ConvertToInt(count); 
    }    
    public bool IsTurnPlayer(object player) { return GetTurnPlayer() == ConvertToInt(player); }
    public int GetFieldGroupCount(object player, object location1, object location2) 
    { 
        return GetMatchingGroup(null, player, location1, location2, null).GetCount(); 
    }
    public bool IsEnvironment(object cardcode) { return false; }
    public bool IsPlayerCanDiscardDeck(object player, object count) { return true; }
    public bool IsPlayerCanRemove(object player) { return true; }
    public int GetCustomActivityCount(object counter_id, object player, object activity_type) { return 0; }
    public bool IsPlayerCanSpecialSummonMonster(object player, object code, params object[] args) { return true; }
    public int GetActivityCount(object player, object activity_type, params object[] args) 
    { 
        int type = ConvertToInt(activity_type);
        if (type == 2 && GameManager.Instance != null) // ACTIVITY_NORMALSUMMON
        {
            return IsPlayer(player) ? GameManager.Instance.normalSummonsThisTurnPlayer : GameManager.Instance.normalSummonsThisTurnOpponent;
        }
        return 0; 
    }
    public int GetCurrentChain() { return CardEffectManager.Instance != null ? CardEffectManager.Instance.currentChain.Count : 0; }
    public int GetTurnCount() { return GameManager.Instance != null ? GameManager.Instance.turnCount : 0; }
    public bool IsAbleToEnterBP() { return true; }
    public bool IsBattlePhase() { return PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle; }
    public bool IsMainPhase() { return PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2); }
    public int GetLP(object player) { return IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP; }
    public bool IsPlayerAffectedByEffect(object player, object effect_code) { return false; }
    public LuaGroup GetFieldGroup(object player, object loc1, object loc2) 
    { 
        return GetMatchingGroup(null, player, loc1, loc2, null); 
    }
    public bool IsCanRemoveCounter(object player, object s, object o, object counterType, object count, object reason) { return true; }
    public bool CheckEvent(object event_code) { return false; }

    public void RegisterFlagEffect(object player, object flag, params object[] extraArgs)
    {
        string key = $"{ConvertToInt(player)}_{ConvertToInt(flag)}";
        if (playerFlags.ContainsKey(key)) playerFlags[key]++;
        else playerFlags[key] = 1;
        // Debug.Log($"[LuaDuel] Flag Effect Registrado: Jogador {player}, Flag {flag}");
    }

    public int GetFlagEffect(object player, object flag)
    {
        string key = $"{ConvertToInt(player)}_{ConvertToInt(flag)}";
        int result = playerFlags.ContainsKey(key) ? playerFlags[key] : 0;
        // Debug.Log($"[LuaDuel] GetFlagEffect consultado: Jogador {player}, Flag {flag} -> Resultado: {result}");
        return result;
    }

    public bool HasFlagEffect(object player, object flag) 
    { 
        string key = $"{ConvertToInt(player)}_{ConvertToInt(flag)}";
        bool result = playerFlags.ContainsKey(key) && playerFlags[key] > 0;
        // Debug.Log($"[LuaDuel] HasFlagEffect consultado: Jogador {player}, Flag {flag} -> Resultado: {result}");
        return result;
    }

    public int GetBattleDamage(object player) { return 0; }
    public bool IsDuelType(object type) { return true; }
    public bool IsPlayerCanSpecialSummon(object player) { return true; }
    public int GetMZoneCount(object player) { return 5; }
    public bool IsCanAddCounter(object player, object counterType, object count, object card) { return true; }
    public LuaGroup GetReleaseGroup(object player, object hand = null) { return new LuaGroup(); }
    public LuaGroup GetTributeGroup(object card) { return new LuaGroup(); }
    public int GetMatchingGroupCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) 
    { 
        return GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs).GetCount(); 
    }
    
    public int GetTargetCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) 
    { 
        return GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs).GetCount(); 
    }    
    
    public void ConfirmDecktop(object player, object count)
    {
        if (UIManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            UIManager.Instance.ShowMessage($"Top Deck revelado: {ConvertToInt(count)} carta(s).");
        }
    }
    
    public LuaGroup GetDecktopGroup(object player, object count)
    {
        int c = ConvertToInt(count);
        bool isPlayer = IsPlayer(player);
        List<CardData> deck = isPlayer ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
        LuaGroup g = new LuaGroup();
        for (int i = 0; i < c && i < deck.Count; i++)
        {
            g.AddCard(new LuaCard(deck[i]) { ownerPlayerIndex = isPlayer ? 0 : 1, previousLocation = CardLocation.Deck });
        }
        return g;
    }

    public bool CheckReleaseGroup(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) 
    { 
        int locSelf = 0x04 | (ConvertToInt(use_hand) != 0 ? 0x02 : 0);
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, 0, excluded, extraArgs);
        return group.GetCount() >= ConvertToInt(count); 
    }
    public bool IsPlayerCanDiscardDeckAsCost(object player, object count) { return true; }
    public DynValue GetOperationInfo(object chainc, object category) { return DynValue.NewTuple(DynValue.NewBoolean(false), UserData.Create(new LuaGroup()), DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); }
    
    public DynValue SelectYesNo(object player, object desc)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(true); // AI default Yes
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectYesNo") });
        }

        if (UIManager.Instance != null)
        {
            string msg = "Você deseja ativar este efeito?";
            if (desc is string s) msg = s;
            else if (desc is double d) msg = GetHintMessageString((int)d);
            else if (desc != null) msg = desc.ToString();
            
            UIManager.Instance.ShowConfirmation(msg, () => {
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(true);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            }, () => {
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(false);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(true);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectYesNo") });
    }

    public DynValue SelectOption(object player, params object[] options)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(0);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectOption") });
        }

        // --- NOVO: Interceptação Inteligente de ATK/DEF ---
        int opt1 = options.Length > 0 ? ConvertToInt(options[0]) : 0;
        int opt2 = options.Length > 1 ? ConvertToInt(options[1]) : 0;
        bool isAtkDefChoice = options.Length == 2 && ((opt1 == 704 && opt2 == 705) || (opt1 == 96 && opt2 == 97));
       
        // Interceptação específica para a carta "7 Completed" (ID 86198326) que usa seus próprios IDs de String para ATK/DEF
        if (!isAtkDefChoice && options.Length == 2 && opt1 > 10000 && opt2 > 10000)
        {
            int cId1 = opt1 / 16;
            int sId1 = opt1 % 16;
            int cId2 = opt2 / 16;
            int sId2 = opt2 % 16;
            if (cId1 == 86198326 && sId1 == 0 && cId2 == 86198326 && sId2 == 1)
                isAtkDefChoice = true;
        }
       
        if (isAtkDefChoice)
        {
            if (AnnounceSelectionUI.Instance == null)
                AnnounceSelectionUI.Instance = Resources.FindObjectsOfTypeAll<AnnounceSelectionUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());
            
            if (AnnounceSelectionUI.Instance != null)
            {
                AnnounceSelectionUI.Instance.ShowAtkDefSelection((selectedIndex) => {
                    CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(selectedIndex);
                    CardEffectManager.Instance.isWaitingForLuaYield = false;
                });
                return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectOption") });
            }
        }

        // Encontra a UI na cena se ela começar desligada
        if (MultipleChoiceUI.Instance == null)
            MultipleChoiceUI.Instance = Resources.FindObjectsOfTypeAll<MultipleChoiceUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());

        if (MultipleChoiceUI.Instance != null)
        {
            List<string> optStrings = new List<string>();
            foreach (var opt in options)
            {
                int optVal = ConvertToInt(opt);
                // Tradução rápida dos IDs de String mais comuns do OCGCore (ATK/DEF)
                if (optVal == 704 || optVal == 96) optStrings.Add("Ataque (ATK)");
                else if (optVal == 705 || optVal == 97) optStrings.Add("Defesa (DEF)");
                else if (optVal > 10000)
                {
                    int cardId = optVal / 16;
                    int strIdx = optVal % 16;
                    
                    if (cardId == 24140059) // A Cat of Ill Omen
                    {
                        if (strIdx == 0) optStrings.Add("Colocar no Topo do Deck");
                        else if (strIdx == 1) optStrings.Add("Adicionar para a Mão");
                        else optStrings.Add($"Opção {strIdx}");
                    }
                    else
                    {
                        var cData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == cardId.ToString());
                        if (cData != null) optStrings.Add($"Efeito {strIdx + 1} ({cData.name})");
                        else optStrings.Add($"Ação {strIdx + 1}");
                    }
                }
                else 
                    optStrings.Add($"Opção {optVal}");
            }

            MultipleChoiceUI.Instance.Show(optStrings, "Escolha um efeito:", 1, 1, (selected) => {
                int selectedIndex = selected != null && selected.Count > 0 ? optStrings.IndexOf(selected[0]) : 0;
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(Mathf.Max(0, selectedIndex));
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(0);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectOption") });
    }

    public DynValue SelectPosition(object player, object card, object pos)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1); // POS_FACEUP_ATTACK
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectPosition") });
        }

        if (UIManager.Instance != null && card is LuaCard lc)
        {
            CardData data = lc.unityData ?? new CardData { name = "Card" };
            UIManager.Instance.ShowPositionSelection(data, (selectedPos) => {
                int res = selectedPos == CardDisplay.BattlePosition.Attack ? 1 : 4; 
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(res);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectPosition") });
    }

    private string GetHintMessageString(int hintCode)
    {
        switch (hintCode)
        {
            case 500: return "Selecione um alvo";
            case 501: return "Selecione carta(s) para descartar";
            case 502: return "Confirme a(s) carta(s)";
            case 503: return "Selecione a carta para ativar";
            case 504: return "Selecione carta(s) para tributar";
            case 505: return "Selecione carta(s) para remover (Banir)";
            case 506: return "Selecione carta(s) para destruir";
            case 509: return "Selecione monstro(s) para Special Summon";
            case 510: return "Selecione carta(s) para retornar à mão";
            case 525: return "Selecione carta(s) para Setar";
            case 545: return "Selecione monstro(s) Face-Up (virado p/ cima)";
            case 546: return "Selecione monstro(s) Face-Down (virado p/ baixo)";
            case 549: return "Selecione um alvo para equipar";
            default: return $"Selecione (Ação {hintCode})";
        }
    }

    public int AnnounceNumber(object player, params object[] args) { 
        if (args != null && args.Length > 0) return ConvertToInt(args[args.Length - 1]);
        return 1000; 
    }
    public int AnnounceLevel(object player, params object[] args) { 
        if (args != null && args.Length > 0) return ConvertToInt(args[args.Length - 1]); // Pega o limite máximo passado (Ex: 5)
        return 4; 
    }
    
    public DynValue AnnounceAttribute(object player, object count, object avail)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1); // Default EARTH
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceAttribute") });
        }

        if (AnnounceSelectionUI.Instance == null)
            AnnounceSelectionUI.Instance = Resources.FindObjectsOfTypeAll<AnnounceSelectionUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());

        if (AnnounceSelectionUI.Instance != null)
        {
            AnnounceSelectionUI.Instance.ShowAttributeSelection((selectedValue) => {
                // Debug.Log($"<color=cyan>[LuaDuel] Atributo Declarado (AnnounceAttribute): {selectedValue}</color>");
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(selectedValue);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else { CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1); CardEffectManager.Instance.isWaitingForLuaYield = false; }
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceAttribute") });
    }

    public DynValue AnnounceRace(object player, object count, object avail)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1); // Default Warrior
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceRace") });
        }

        if (AnnounceSelectionUI.Instance == null)
            AnnounceSelectionUI.Instance = Resources.FindObjectsOfTypeAll<AnnounceSelectionUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());

        if (AnnounceSelectionUI.Instance != null)
        {
            AnnounceSelectionUI.Instance.ShowRaceSelection((selectedValue) => {
                // Debug.Log($"<color=cyan>[LuaDuel] Raça Declarada (AnnounceRace): {selectedValue}</color>");
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(selectedValue);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else { CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1); CardEffectManager.Instance.isWaitingForLuaYield = false; }
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceRace") });
    }

    public DynValue AnnounceCard(object player, params object[] args)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(40640057); // IA chuta o código de "Kuriboh"
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceCard") });
        }

        // Callback de Sucesso para destravar o LUA
        Action<int> onCardSelected = (cardId) => {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(cardId);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        };

        // Tenta invocar a nova UI Rápida de Declaração Dinamicamente
        Type declareUIType = Type.GetType("DeclareCardNameUI") ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == "DeclareCardNameUI");
        if (declareUIType != null)
        {
            System.Reflection.FieldInfo instanceField = declareUIType.GetField("Instance");
            object instance = instanceField != null ? instanceField.GetValue(null) : null;
            if (instance != null)
            {
                var showMethod = declareUIType.GetMethod("Show");
                if (showMethod != null)
                {
                    try {
                        showMethod.Invoke(instance, new object[] { "Declare 1 Nome de Carta", onCardSelected });
                        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceCard") });
                    } catch { }
                }
            }
        }

        // Fallback de Segurança para a Busca Global
        Type searchUIType = Type.GetType("GlobalCardSearchUI") ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == "GlobalCardSearchUI");
        if (searchUIType != null)
        {
            System.Reflection.FieldInfo instanceField = searchUIType.GetField("Instance");
            System.Reflection.PropertyInfo instanceProp = searchUIType.GetProperty("Instance");
            object instance = instanceField != null ? instanceField.GetValue(null) : (instanceProp != null ? instanceProp.GetValue(null, null) : null);
            if (instance != null)
            {
                var showMethod = searchUIType.GetMethod("Show");
                if (showMethod != null)
                {
                    try {
                        showMethod.Invoke(instance, new object[] { onCardSelected });
                        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceCard") });
                    } catch { }
                }
            }
        }

        Debug.LogWarning("[LuaDuel] GlobalCardSearchUI não encontrada ou assinatura incompatível. Retornando ID de teste (Kuriboh).");
        onCardSelected(40640057);
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceCard") });
    }

    public bool IsChainNegatable(object chaincount) { return true; }
    public int GetOperationCount(object chainc) { return 0; }
    public bool IsChainDisablable(object chainc) { return true; }
    public void DiscardDeck(object player, object count, object reason) { }
    public bool CheckTribute(object card, object min, object max, object group = null, object zone = null) { return true; }
    public void SetTargetCard(object target) { 
        if (currentTargetGroup == null) currentTargetGroup = new LuaGroup();
        if (target is LuaGroup g) currentTargetGroup.cards.AddRange(g.cards);
        else if (target is LuaCard c) currentTargetGroup.AddCard(c);
    }
    public void ClearTargetCard() { 
        currentTargetGroup = new LuaGroup(); 
    }
    public bool CheckEvent(object event_code, object chainc = null) { return false; }

    public void RaiseEvent(object triggerCard, object eventCode, object eg, object ep, object ev, object re, object r)
    {
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.TriggerLuaEvent(ConvertToInt(eventCode), triggerCard);
    }

    public void RaiseSingleEvent(object triggerCard, object eventCode, object e, object ep, object ev, object re, object r)
    {
        if (triggerCard is LuaCard lc && CardEffectManager.Instance != null)
        {
            int code = ConvertToInt(eventCode);
            var matchingEffects = lc.registeredEffects.FindAll(eff => eff.code == code);
            
            foreach(var effect in matchingEffects)
            {
                if (CardEffectManager.Instance.CanActivateEffect(lc, effect, lc.GetControler(), null))
                {
                    CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.chainManager.BuildAndResolveChainRoutine(lc, effect, null, lc.GetControler(), null));
                }
            }
        }
    }

    public void BreakEffect()
    {
        // Ponto de quebra de resolução de corrente (usado para Miss Timing em jogos reais).
    }

    // Stubs vitais capturados pelo relatório (Counter Traps e UX)
    public void HintSelection(object group) { }
    public bool NegateActivation(object chainc) 
    { 
        if (CardEffectManager.Instance != null) return CardEffectManager.Instance.NegateChainLink(ConvertToInt(chainc));
        return true; 
    }
    public bool NegateEffect(object chainc) 
    { 
        if (CardEffectManager.Instance != null) return CardEffectManager.Instance.NegateChainLink(ConvertToInt(chainc));
        return true; 
    }
    public void ChangeChainOperation(object chainc, object op) { }

    
    public DynValue SelectReleaseGroupCost(object player, object filterFunc, object min, object max, object use_hand, object excluded, params object[] extraArgs)
    {
        int locSelf = 0x04 | (ConvertToInt(use_hand) != 0 ? 0x02 : 0);
        return InternalSelectMatchingCard(player, filterFunc, player, locSelf, 0, ConvertToInt(min), ConvertToInt(max), excluded, HighlightCategory.Tribute, extraArgs);
    }    
    public int SelectDisableField(params object[] args) { return 0; }
    public LuaEffect SelectEffect(params object[] args) { return new LuaEffect { owner = SafeDummyCard() }; }
    
    public DynValue ConfirmCards(object player, object targets) 
    { 
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        List<CardData> offFieldCards = new List<CardData>();
        List<CardDisplay> handCardsPlayer = new List<CardDisplay>();
        List<CardDisplay> handCardsOpponent = new List<CardDisplay>();

        // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Início da Chamada. Player Invocador: {player}");

        // Revela as cartas temporariamente se estiverem no campo viradas para baixo
        if (targets is LuaGroup group) {
            // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Alvo é LuaGroup com {group.cards.Count} cartas.");
            foreach (var c in group.cards) {
                if (c.unityCard != null && (!c.unityCard.isOnField || c.unityCard.CurrentLocation == CardLocation.Hand)) {
                    // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Capturado da Mão: {c.unityCard.CurrentCardData.name} (isPlayer: {c.unityCard.isPlayerCard})");
                    if (c.unityCard.isPlayerCard) handCardsPlayer.Add(c.unityCard);
                    else handCardsOpponent.Add(c.unityCard);
                }

                if (c.unityCard != null && c.unityCard.isFlipped && c.unityCard.isOnField) c.unityCard.ShowFront();
                else if (c.unityCard == null && c.unityData != null) offFieldCards.Add(c.unityData);
            }
        } else if (targets is LuaCard card) {
            // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Alvo é LuaCard (Single).");
            if (card.unityCard != null && (!card.unityCard.isOnField || card.unityCard.CurrentLocation == CardLocation.Hand)) {
                // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Capturado da Mão: {card.unityCard.CurrentCardData.name} (isPlayer: {card.unityCard.isPlayerCard})");
                if (card.unityCard.isPlayerCard) handCardsPlayer.Add(card.unityCard);
                else handCardsOpponent.Add(card.unityCard);
            }

            if (card.unityCard != null && card.unityCard.isFlipped && card.unityCard.isOnField) card.unityCard.ShowFront();
            else if (card.unityCard == null && card.unityData != null) offFieldCards.Add(card.unityData);
        }

        // INTERCEPTAÇÃO CINEMÁTICA: O "Ante" (E Duelos Similares)
        if (CardComparisonUI.Instance == null)
            CardComparisonUI.Instance = Resources.FindObjectsOfTypeAll<CardComparisonUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());

        // Verifica se é a carta Ante (ou similar que exija confronto)
        bool isComparisonEffect = false;
        string currentEffName = "Nenhum";
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
        {
            var eff = CardEffectManager.Instance.chainManager.resolvingLink.effect;
            if (eff != null && eff.owner != null && eff.owner.unityData != null && 
               (eff.owner.unityData.id == "11324436" || eff.owner.unityData.id == "DM0070" || eff.owner.unityData.name == "Ante"))
            {
                currentEffName = eff.owner.unityData.name;
                isComparisonEffect = true;
            }
        }

        // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Efeito em Resolução: {currentEffName} | É Ante/Aposta? {isComparisonEffect} | UI Existe? {CardComparisonUI.Instance != null}");

        if (isComparisonEffect && CardComparisonUI.Instance != null && !GameManager.Instance.isSimulating)
        {
            if (handCardsPlayer.Count > 0) pendingComparisonCards.AddRange(handCardsPlayer);
            if (handCardsOpponent.Count > 0) pendingComparisonCards.AddRange(handCardsOpponent);

            // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Cartas pendentes no Buffer (Player + Oponente): {pendingComparisonCards.Count}");

            if (pendingComparisonCards.Count >= 2)
            {
                var pCard = pendingComparisonCards.FirstOrDefault(c => c.isPlayerCard);
                var oCard = pendingComparisonCards.FirstOrDefault(c => !c.isPlayerCard);
                
                pendingComparisonCards.Clear(); // Limpa o buffer para o próximo confronto

                if (pCard != null && oCard != null)
                {
                    // Debug.Log($"<color=green>[ConfirmCards LOG]</color> SUCESSO! Abrindo Confronto: {pCard.CurrentCardData.name} vs {oCard.CurrentCardData.name}");

                    int pLvl = pCard.CurrentCardData.type.Contains("Monster") ? pCard.originalLevel : 0;
                    int oLvl = oCard.CurrentCardData.type.Contains("Monster") ? oCard.originalLevel : 0;

                    CardComparisonUI.Instance.ShowVersus(
                        pCard, oCard,
                        "CONFRONTO DE NÍVEIS!",
                        $"LVL {pLvl}", $"LVL {oLvl}",
                        pLvl, oLvl,
                        () => {
                            CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                            CardEffectManager.Instance.isWaitingForLuaYield = false;
                        },
                        true // Para Ante, o maior Nível vence a aposta!
                    );
                    return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ConfirmCards") });
                }
                else
                {
                    // Debug.Log($"<color=red>[ConfirmCards LOG]</color> FALHA: Pelo menos uma das cartas no buffer era nula.");
                }
            }
            else
            {
                // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Buffer incompleto (Apenas 1 lado escolheu). Silenciando Engine C# e esperando a segunda metade do LUA...");
                // Silencia a primeira chamada visualmente e aguarda a segunda metade do LUA
                CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                CardEffectManager.Instance.isWaitingForLuaYield = false;
                return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ConfirmCards") });
            }
        }
        else
        {
            pendingComparisonCards.Clear(); // Limpeza de segurança para outras magias
        }

        // Se houver cartas invisíveis (como o topo do Deck), abre o painel modal como um "Visualizador"
        if (offFieldCards.Count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            GameManager.Instance.OpenCardMultiSelection(offFieldCards, "Cartas Reveladas do Topo", 0, 0, (selected) => {
                CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.StartCoroutine(ConfirmCardsRoutine(1.2f));
        }
        
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ConfirmCards") });
    }

    private IEnumerator ConfirmCardsRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }
    public void SetPossibleOperationInfo(params object[] args) { }
    public int GetDrawCount(params object[] args) { return 1; }
    public void SetTargetPlayer(object p) { targetPlayer = ConvertToInt(p); }

    public void RegisterEffect(LuaEffect e, object player = null)
    {
        if (e == null) return;
        globalEffects.Add(e);
    }

    public void SetTargetParam(object p) { targetParam = ConvertToInt(p); }
    
    public DynValue GetChainInfo(object chainc, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null)
    {
         // Debug.Log($"[Surgical Log] GetChainInfo invocado! arg1: {arg1}, arg2: {arg2}");
         List<DynValue> returns = new List<DynValue>();
         List<object> argsList = new List<object>();
         if (arg1 != null) argsList.Add(arg1);
         if (arg2 != null) argsList.Add(arg2);
         if (arg3 != null) argsList.Add(arg3);
         if (arg4 != null) argsList.Add(arg4);

        int targetPl = targetPlayer;
        int targetPa = targetParam;
        LuaGroup targGr = currentTargetGroup;
        ChainManager.ChainLink targetLink = null;
        
        int chainIndex = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null)
        {
            if (chainIndex == 0 && CardEffectManager.Instance.chainManager.resolvingLink != null)
            {
                targetPl = CardEffectManager.Instance.chainManager.resolvingLink.targetPlayer;
                targetPa = CardEffectManager.Instance.chainManager.resolvingLink.targetParam;
                targGr = CardEffectManager.Instance.chainManager.resolvingLink.targetGroup;
                targetLink = CardEffectManager.Instance.chainManager.resolvingLink;
            }
            else if (chainIndex > 0)
            {
                var specificLink = CardEffectManager.Instance.chainManager.currentChain.Find(l => l.chainIndex == chainIndex);
                if (specificLink != null)
                {
                    targetPl = specificLink.targetPlayer;
                    targetPa = specificLink.targetParam;
                    targGr = specificLink.targetGroup;
                    targetLink = specificLink;
                }
            }
        }

        foreach (object o in argsList)
        {
            int arg = ConvertToInt(o);
            if (arg == 1) // CHAININFO_TARGET_PLAYER
            {
                returns.Add(DynValue.NewNumber(targetPl));
            }
            else if (arg == 2) // CHAININFO_TARGET_PARAM
            {
                returns.Add(DynValue.NewNumber(targetPa));
            }
            else if (arg == 3 || arg == 16 || arg == 8388608) // CHAININFO_TARGET_CARDS (3 injetado pelo nosso Core)
            {
                LuaGroup g = targGr != null ? targGr : new LuaGroup();
                returns.Add(UserData.Create(g));
            }
            else if (arg == 64 || arg == 128 || arg == 32) // TRIGGERING_EFFECT (0x40)
            {
                LuaEffect dummyEff = new LuaEffect { 
                    owner = SafeDummyCard(),
                    conditionFunc = CardEffectManager.Instance.dummyClosureTrue,
                    costFunc = CardEffectManager.Instance.dummyClosureTrue,
                    targetFunc = CardEffectManager.Instance.dummyClosureTrue,
                    operationFunc = CardEffectManager.Instance.dummyClosureTrue
                };
                returns.Add(UserData.Create(dummyEff));
            }
            else if (arg == 1024) // CHAININFO_TRIGGERING_LOCATION
            {
                int loc = targetLink != null && targetLink.card != null ? targetLink.card.GetLocation() : 0;
                returns.Add(DynValue.NewNumber(loc));
            }
            else
            {
                returns.Add(DynValue.Nil);
            }
        }
        if (returns.Count == 0) return DynValue.NewTuple(DynValue.Nil, DynValue.Nil);
        if (returns.Count == 1) return returns[0];
        // Debug.Log($"[Surgical Log] GetChainInfo enviando um Tuple de volta ao LUA com {returns.Count} valores.");
        return DynValue.NewTuple(returns.ToArray());
    }

    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public LuaEffect GetPlayerEffect(object player, object effect_code) { return new LuaEffect { owner = SafeDummyCard() }; }

    public LuaCard GetFirstTarget()
    {
        LuaGroup group = GetTargetCards(null);
        return (group != null && group.GetFirst() != null) ? group.GetFirst() : SafeDummyCard();
    }

    public LuaGroup GetTargetCards(object e)
    {
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.targetGroup ?? new LuaGroup();
        return currentTargetGroup != null ? currentTargetGroup : new LuaGroup();
    }

    public int SendtoHand(object target, object player, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) { 
                if (c.unityCard != null) { GameManager.Instance.ReturnToHand(c.unityCard); count++; }
                else if (c.unityData != null) { bool pDummy; RemoveDataFromAllPiles(c.unityData, out pDummy); GameManager.Instance.AddCardToHand(c.unityData, c.GetControler() == 0); count++; }
            }
            // Debug.Log($"[Lua] Duel.SendtoHand(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null)
            {
                GameManager.Instance.ReturnToHand(card.unityCard);
                count = 1;
                // Debug.Log($"[Lua] Duel.SendtoHand({card.unityCard.CurrentCardData.name})");
            }
            else if (card.unityData != null)
            {
                bool pDummy; RemoveDataFromAllPiles(card.unityData, out pDummy);
                GameManager.Instance.AddCardToHand(card.unityData, card.GetControler() == 0);
                count = 1;
            }
        }
        return count;
    }

    private CardLocation RemoveDataFromAllPiles(CardData data, out bool wasPlayerPile)
    {
        wasPlayerPile = true;
        if (GameManager.Instance == null) return CardLocation.Unknown;
        if (DeckManager.Instance != null) {
            if (DeckManager.Instance.GetPlayerDeck().Contains(data)) { 
                DeckManager.Instance.GetPlayerDeck().Remove(data); 
                DeckManager.Instance.UpdateDeckVisuals(); 
                if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false;
                wasPlayerPile = true; return CardLocation.Deck; 
            }
            if (DeckManager.Instance.GetOpponentDeck().Contains(data)) { 
                DeckManager.Instance.GetOpponentDeck().Remove(data); 
                DeckManager.Instance.UpdateDeckVisuals(); 
                if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false;
                wasPlayerPile = false; return CardLocation.Deck; 
            }
        }
        // Fallbacks de Segurança
        if (GameManager.Instance.GetPlayerMainDeck().Contains(data)) { GameManager.Instance.GetPlayerMainDeck().Remove(data); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(true); else nextShuffleDisabled = false; wasPlayerPile = true; return CardLocation.Deck; }
        if (GameManager.Instance.GetOpponentMainDeck().Contains(data)) { GameManager.Instance.GetOpponentMainDeck().Remove(data); if (!nextShuffleDisabled) GameManager.Instance.ShuffleDeck(false); else nextShuffleDisabled = false; wasPlayerPile = false; return CardLocation.Deck; }
        
        if (GameManager.Instance.GetPlayerExtraDeck().Contains(data)) { GameManager.Instance.GetPlayerExtraDeck().Remove(data); wasPlayerPile = true; return CardLocation.ExtraDeck; }
        if (GameManager.Instance.GetOpponentExtraDeck().Contains(data)) { GameManager.Instance.GetOpponentExtraDeck().Remove(data); wasPlayerPile = false; return CardLocation.ExtraDeck; }
        if (GameManager.Instance.GetPlayerGraveyard().Contains(data)) { GameManager.Instance.GetPlayerGraveyard().Remove(data); wasPlayerPile = true; return CardLocation.Graveyard; }
        if (GameManager.Instance.GetOpponentGraveyard().Contains(data)) { GameManager.Instance.GetOpponentGraveyard().Remove(data); wasPlayerPile = false; return CardLocation.Graveyard; }
        if (GameManager.Instance.GetPlayerRemoved().Contains(data)) { GameManager.Instance.GetPlayerRemoved().Remove(data); wasPlayerPile = true; return CardLocation.Banished; }
        if (GameManager.Instance.GetOpponentRemoved().Contains(data)) { GameManager.Instance.GetOpponentRemoved().Remove(data); wasPlayerPile = false; return CardLocation.Banished; }
        return CardLocation.Unknown;
    }

    public bool SpecialSummon(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        bool isPlayerSummoning = IsPlayer(player);
        int posInt = ConvertToInt(pos);
        bool inDefense = (posInt & 0x8) != 0 || (posInt & 0xA) != 0; // Verifica se tem flag de defesa

        if (target is LuaGroup group && group.cards.Count > 0)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                {
                    if (GameManager.Instance.playerHand.Contains(c.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(c.unityCard.gameObject);
                    else if (GameManager.Instance.opponentHand.Contains(c.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(c.unityCard.gameObject);
                    
                    Vector3? sPos = null; CardLocation sLoc = CardLocation.Unknown;
                    if (c.unityCard != null) { sPos = c.unityCard.transform.position; sLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand; }
                    else { sLoc = CardLocation.Graveyard; sPos = isPlayerSummoning ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; }
                    
                    GameManager.Instance.SpecialSummonFromData(c.unityCard.CurrentCardData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc);
                    GameObject.Destroy(c.unityCard.gameObject);
                }
                else if (c.unityData != null)
                {
                    bool pDummy; RemoveDataFromAllPiles(c.unityData, out pDummy);
                    GameManager.Instance.SpecialSummonFromData(c.unityData, isPlayerSummoning, -1, true, inDefense, null, CardLocation.Graveyard);
                }
            }
            // Debug.Log($"[Lua] Duel.SpecialSummon(Grupo)");
            return true;
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null)
            {
                if (GameManager.Instance.playerHand.Contains(card.unityCard.gameObject)) GameManager.Instance.playerHand.Remove(card.unityCard.gameObject);
                else if (GameManager.Instance.opponentHand.Contains(card.unityCard.gameObject)) GameManager.Instance.opponentHand.Remove(card.unityCard.gameObject);
                
                Vector3? sPos = null; CardLocation sLoc = CardLocation.Unknown;
                if (card.unityCard != null) { sPos = card.unityCard.transform.position; sLoc = card.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand; }
                else { sLoc = CardLocation.Graveyard; sPos = isPlayerSummoning ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position; }
                
                GameManager.Instance.SpecialSummonFromData(card.unityCard.CurrentCardData, isPlayerSummoning, -1, true, inDefense, sPos, sLoc);
                GameObject.Destroy(card.unityCard.gameObject);
                // Debug.Log($"[Lua] Duel.SpecialSummon({card.unityCard.CurrentCardData.name})");
                return true;
            }
            else if (card.unityData != null)
            {
                bool pDummy; RemoveDataFromAllPiles(card.unityData, out pDummy);
                GameManager.Instance.SpecialSummonFromData(card.unityData, isPlayerSummoning, -1, true, inDefense, null, CardLocation.Graveyard);
                // Debug.Log($"[Lua] Duel.SpecialSummon({card.unityData.name} - Token)");
                return true;
            }
        }
        
        return false;
    }
    
    public bool SpecialSummonStep(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        return SpecialSummon(target, sumtype, sumplayer, player, nocheck, nolimit, pos);
    }

    public void SpecialSummonComplete()
    {
        // Finaliza a invocação iniciada pelo SpecialSummonStep (Já resolvemos instantaneamente no passo)
    }

    public LuaCard CreateToken(object player, object code)
    {
        int cardCode = ConvertToInt(code);
        CardData tokenData = null;

        // Tenta achar os dados do token (status, imagem) no banco usando o ID (Password)
        if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
        {
            string strCode = cardCode.ToString();
            tokenData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == strCode);
        }

        // Fallback robusto se a API do site não nos enviou o token
        if (tokenData == null)
        {
            tokenData = new CardData 
            { 
                id = "TOKEN", 
                password = cardCode.ToString(),
                name = "Token", 
                type = "Monster (Token)", 
                atk = 0, def = 0, level = 1, 
                race = "Beast", attribute = "Earth" 
            };
        }

        return new LuaCard(tokenData);
    }

    public bool Equip(object player, object equip_card, object target)
    {
        LuaCard ec = equip_card as LuaCard;
        LuaCard t = target as LuaCard;
        if (ec != null && t != null && ec.unityCard != null && t.unityCard != null)
        {
            if (ec.unityData.type.Contains("Monster"))
                GameManager.Instance.EquipMonsterToMonster(ec.unityCard, t.unityCard);
            else
                GameManager.Instance.CreateCardLink(ec.unityCard, t.unityCard, CardLink.LinkType.Equipment);
            
            // Debug.Log($"[Lua] Duel.Equip({ec.unityData.name} em {t.unityData.name})");
            return true;
        }
        return false;
    }

    public void DisableShuffleCheck(params object[] args)
    {
        nextShuffleDisabled = true;
    }

    public DynValue ChangePosition(object target, object au, object ad = null, object du = null, object dd = null, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        int posAU = ConvertToInt(au); // POS_FACEUP_ATTACK (1)
        int posAD = ad != null && (!(ad is MoonSharp.Interpreter.DynValue d1) || !d1.IsNil()) ? ConvertToInt(ad) : posAU;
        int posDU = du != null && (!(du is MoonSharp.Interpreter.DynValue d2) || !d2.IsNil()) ? ConvertToInt(du) : posAU;
        int posDD = dd != null && (!(dd is MoonSharp.Interpreter.DynValue d3) || !d3.IsNil()) ? ConvertToInt(dd) : posAU;

        List<CardDisplay> cardsToChange = new List<CardDisplay>();
        if (target is LuaGroup group) { foreach (var c in group.cards) if (c.unityCard != null) cardsToChange.Add(c.unityCard); }
        else if (target is LuaCard card && card.unityCard != null) { cardsToChange.Add(card.unityCard); }

        CardEffectManager.Instance.StartCoroutine(ChangePositionRoutine(cardsToChange, posAU, posAD, posDU, posDD));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ChangePosition") });
    }

    private IEnumerator ChangePositionRoutine(List<CardDisplay> cards, int posAU, int posAD, int posDU, int posDD)
    {
        int count = 0;
        int pendingAnimations = 0;

        foreach (CardDisplay c in cards)
        {
            if (c == null || !c.isOnField) continue;
            
            int currentPos = 0;
            if (c.position == CardDisplay.BattlePosition.Attack && !c.isFlipped) currentPos = 1;
            else if (c.position == CardDisplay.BattlePosition.Attack && c.isFlipped) currentPos = 2;
            else if (c.position == CardDisplay.BattlePosition.Defense && !c.isFlipped) currentPos = 4;
            else if (c.position == CardDisplay.BattlePosition.Defense && c.isFlipped) currentPos = 8;

            int targetPos = 0;
            if (currentPos == 1) targetPos = posAU;
            else if (currentPos == 2) targetPos = posAD;
            else if (currentPos == 4) targetPos = posDU;
            else if (currentPos == 8) targetPos = posDD;

            if (targetPos == 0 || targetPos == currentPos) continue;

            bool isAttackTarget = (targetPos == 1 || targetPos == 2);
            bool isFaceUpTarget = (targetPos == 1 || targetPos == 4);

            if ((isAttackTarget && c.position == CardDisplay.BattlePosition.Defense) || (!isAttackTarget && c.position == CardDisplay.BattlePosition.Attack))
            {
                c.position = isAttackTarget ? CardDisplay.BattlePosition.Attack : CardDisplay.BattlePosition.Defense;
                c.transform.localRotation = Quaternion.Euler(0, 0, isAttackTarget ? (c.isPlayerCard ? 0f : 180f) : (c.isPlayerCard ? 90f : -90f));
            }

            if (isFaceUpTarget && c.isFlipped)
            {
                pendingAnimations++;
                c.RevealCard(false, true, () => { 
                    if (GameManager.Instance != null) { 
                        GameManager.Instance.OnBattlePositionChanged(c); 
                        if (isAttackTarget) GameManager.Instance.OnFlipSummon(c); 
                    } 
                    pendingAnimations--;            
                });
            }
            else if (!isFaceUpTarget && !c.isFlipped)
            {
                pendingAnimations++;
                c.ShowBack(true, () => {
                    if (GameManager.Instance != null) GameManager.Instance.OnBattlePositionChanged(c);
                    pendingAnimations--;
                });            
            }
            else { if (GameManager.Instance != null) GameManager.Instance.OnBattlePositionChanged(c); }
            count++;
        }

        // Aguarda todas as animações visuais terminarem
        while (pendingAnimations > 0) yield return null;
        
        // Pausa extra de 0.6s para o jogador absorver a informação (ex: Acid Trap Hole)
        if (count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
            yield return new WaitForSeconds(0.6f);

        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }

    public void Summon(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) 
        {
            if (IsPlayer(player)) GameManager.Instance.normalSummonsThisTurnPlayer++;
            else GameManager.Instance.normalSummonsThisTurnOpponent++;
            
            Vector3 sourcePos = c.unityCard.transform.position;
            CardLocation sourceLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand;
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, false, IsPlayer(player), false, c.unityData.level >= 5, null, null, sourcePos, sourceLoc);
        }
    }

    public void MSet(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) 
        {
            if (IsPlayer(player)) GameManager.Instance.normalSummonsThisTurnPlayer++;
            else GameManager.Instance.normalSummonsThisTurnOpponent++;
            
            Vector3 sourcePos = c.unityCard.transform.position;
            CardLocation sourceLoc = c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand;
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, true, IsPlayer(player), true, c.unityData.level >= 5, null, null, sourcePos, sourceLoc);
        }
    }

    public bool CheckLPCost(object player, object cost)
    {
        int lp = IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        return lp >= ConvertToInt(cost);
    }

    public void PayLPCost(object player, object cost)
    {
        GameManager.Instance.PayLifePoints(IsPlayer(player), ConvertToInt(cost));
    }

    public LuaCard GetAttacker()
    {
        return currentAttacker ?? SafeDummyCard();
    }

    public LuaCard GetAttackTarget()
    {
        return currentAttackTarget ?? SafeDummyCard();
    }

    public LuaCard GetHistoricalAttacker()
    {
        return historicalAttacker ?? SafeDummyCard();
    }

    public LuaCard GetHistoricalAttackTarget()
    {
        return historicalAttackTarget ?? SafeDummyCard();
    }
    
    public void CalculateDamage(object attackerObj, object defenderObj)
    {
        LuaCard attacker = attackerObj as LuaCard;
        LuaCard defender = defenderObj as LuaCard;
        
        // FIX: Preserva a memória do Atacante e Defensor para os eventos pós-dano (como After the Struggle)
        this.currentAttacker = attacker;
        this.currentAttackTarget = defender;
        
        // SAFETY NET: Histórico persistente até o fim do turno
        this.historicalAttacker = attacker;
        this.historicalAttackTarget = defender;

        if (attacker == null || attacker.unityCard == null) return;

        CardDisplay atkCard = attacker.unityCard;
        CardDisplay defCard = defender != null ? defender.unityCard : null;

        int atkPower = atkCard.currentAtk;
        bool atkIsPlayer = atkCard.isPlayerCard;
        
        if (defCard == null)
        {
            Debug.Log($"<color=orange>[DEBUG LUA BATTLE]</color> Calculando dano direto: {atkCard.CurrentCardData.name} (ATK {atkPower})...");
        }
        else
        {
            Debug.Log($"<color=orange>[DEBUG LUA BATTLE]</color> Calculando dano: {atkCard.CurrentCardData.name} (ATK {atkPower}) vs {defCard.CurrentCardData.name}...");
        }

        if (defCard == null)
        {
            // Ataque Direto
            if (atkIsPlayer) GameManager.Instance.DamageOpponent(atkPower);
            else GameManager.Instance.DamagePlayer(atkPower);
        }
        else
        {
            int defPower = defCard.position == CardDisplay.BattlePosition.Attack ? defCard.currentAtk : defCard.currentDef;
            bool defIsPlayer = defCard.isPlayerCard;

            if (defCard.position == CardDisplay.BattlePosition.Attack)
            {
                if (atkPower > defPower)
                {
                    if (defIsPlayer) GameManager.Instance.DamagePlayer(atkPower - defPower);
                    else GameManager.Instance.DamageOpponent(atkPower - defPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else if (atkPower < defPower)
                {
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(atkCard);
                    GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayDestruction(atkCard);
                        DuelFXManager.Instance.PlayDestruction(defCard);
                    }
                    GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, SendReason.Battle);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
            }
            else // Defesa
            {
                if (atkPower > defPower)
                {
                    if (atkCard.hasPiercing)
                    {
                        if (defIsPlayer) GameManager.Instance.DamagePlayer(atkPower - defPower);
                        else GameManager.Instance.DamageOpponent(atkPower - defPower);
                    }
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else if (atkPower < defPower)
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                }
                else
                {
                    // Equal ATK and DEF: reflect VFX but no damage
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                }
            }
        }
    }

    public void SSet(object player, object target, params object[] extraArgs)
    {
        if (target is LuaCard card && card.unityCard != null)
            GameManager.Instance.PlaySpellTrap(card.unityCard.gameObject, card.unityData, true);
        else if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null) GameManager.Instance.PlaySpellTrap(c.unityCard.gameObject, c.unityData, true);
        }
    }

    public bool GetControl(object target, object player, object reset_phase = null, object reset_count = null)
    {
        bool targetPlayerIsHuman = IsPlayer(player);
        
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null && c.unityCard.isPlayerCard != targetPlayerIsHuman)
                    GameManager.Instance.SwitchControl(c.unityCard);
            }
            return true;
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            if (card.unityCard.isPlayerCard != targetPlayerIsHuman)
                GameManager.Instance.SwitchControl(card.unityCard);
            return true;
        }
        
        return false;
    }

    public LuaCard GetFieldCard(object player, object location, object sequence)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return null;
        
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        int seq = ConvertToInt(sequence);

        System.Func<CardDisplay, LuaCard> GetCachedCard = (cd) => {
            if (CardEffectManager.Instance != null) {
                LuaCard cached = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                if (cached != null) return cached;
            }
            return new LuaCard(cd);
        };

        Transform[] zones = null;
        if ((loc & 0x04) != 0) zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        else if ((loc & 0x08) != 0) zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;

        if (zones != null && seq >= 0 && seq < zones.Length && zones[seq].childCount > 0)
        {
            var cd = zones[seq].GetComponentInChildren<CardDisplay>();
            if (cd != null) return GetCachedCard(cd);
        }
        if ((loc & 0x08) != 0 && seq == 5) // Field Zone Convention
        {
            Transform fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
            if (fz.childCount > 0) { var cd = fz.GetComponentInChildren<CardDisplay>(); if (cd != null) return GetCachedCard(cd); }
        }
        return null;
    }

    // --- SISTEMA DE BUSCA E FILTROS DO LUA ---

    private void CollectCandidates(int loc, bool isPlayer, List<LuaCard> candidates, LuaCard excluded = null)
    {
        int pIdx = isPlayer ? 0 : 1;

        System.Func<CardDisplay, LuaCard> GetCachedCard = (cd) => {
            if (CardEffectManager.Instance != null) {
                LuaCard cached = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                if (cached != null) return cached;
            }
            return new LuaCard(cd);
        };

        if ((loc & 0x01) != 0) // DECK
        {
            var deck = isPlayer ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
            foreach(var c in deck) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x02) != 0) // HAND
        {
            var handGOs = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
            foreach(var go in handGOs) { 
                CardDisplay cd = go.GetComponent<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x04) != 0) // MZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach (var z in zones) if (z.childCount > 0) { 
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x08) != 0) // SZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            foreach (var z in zones) if (z.childCount > 0) { 
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
            Transform fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
            if (fz.childCount > 0) { 
                CardDisplay cd = fz.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x10) != 0) // GRAVE
        {
            var gy = isPlayer ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
            foreach(var c in gy) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x20) != 0) // REMOVED
        {
            var rm = isPlayer ? GameManager.Instance.GetPlayerRemoved() : GameManager.Instance.GetOpponentRemoved();
            foreach(var c in rm) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x40) != 0) // EXTRA
        {
            var ex = isPlayer ? GameManager.Instance.GetPlayerExtraDeck() : GameManager.Instance.GetOpponentExtraDeck();
            foreach(var c in ex) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
    }

    public LuaGroup GetMatchingGroup(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs)
    {
        LuaGroup group = new LuaGroup();
        List<LuaCard> candidates = new List<LuaCard>();

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            bool isPlayer = IsPlayer(ConvertToInt(player));
            
            LuaCard excludedCard = excluded as LuaCard; // Simplificado para compatibilidade
            int lSelf = ConvertToInt(locSelf);
            int lOpp = ConvertToInt(locOpp);
            if (lSelf != 0) CollectCandidates(lSelf, isPlayer, candidates, excludedCard);
            if (lOpp != 0) CollectCandidates(lOpp, !isPlayer, candidates, excludedCard);
        }

        // Aplica o filtro Lua em cada carta encontrada na Unity
        foreach (var c in candidates)
        {            
            // Previne que cartas que acabaram de ser usadas como custo (ex: A Feather of the Phoenix) sejam alvo de si mesmas
            if (excluded is LuaGroup exg && exg.cards.Exists(exc => exc.unityData == c.unityData)) continue;
            else if (excluded is LuaCard excCard && excCard.unityData == c.unityData) continue;

            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                
                try {
                    DynValue result = closure.Call(callArgs.ToArray());
                    
                    bool passed = result.Type == DataType.Boolean && result.Boolean;
                    
                    Debug.Log($"<color=cyan>[Filtro LUA]</color> Avaliando alvo: '{c.unityData?.name}' | Aprovado no filtro? {passed}");
                    
                    if (result.Type == DataType.Boolean && result.Boolean) {
                        group.AddCard(c);
                    }
                } catch (System.Exception ex) {
                    Debug.LogWarning($"<color=red>[Filtro LUA]</color> Erro fatal ao avaliar '{c.unityData?.name}': {ex.Message}");
                }
            }
            else
            {
                group.AddCard(c);
            }
        }
        return group;
    }

    public bool IsExistingMatchingCard(object filterFunc, object player, object locSelf, object locOpp, object count, object excluded, params object[] extraArgs)
    {
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs);
        bool res = group.GetCount() >= ConvertToInt(count);
        
        Debug.Log($"<color=orange>[LuaDuel]</color> Validando alvo no campo | Requisito: >= {ConvertToInt(count)} alvos | Encontrou: {group.GetCount()} | Permitido? {res}");
        return res;
    }

    public bool IsExistingTarget(object filterFunc, object player, object locSelf, object locOpp, object count, object excluded, params object[] extraArgs)
    {
        return IsExistingMatchingCard(filterFunc, player, locSelf, locOpp, count, excluded, extraArgs);
    }

    // --- OPERAÇÕES ASSÍNCRONAS DE LUA (YIELD REQ) ---

    public DynValue SelectTarget(object player, object filterFunc, object player2, object locSelf, object locOpp, object min, object max, object excluded, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        string promptTitle = lastHintMsg;
        lastHintMsg = "Selecione um alvo"; // Reseta para o próximo uso

        // Combina o 'excluded' do LUA com o nosso 'lastCostGroup' do C#
        LuaGroup finalExcluded = new LuaGroup();
        if (excluded is LuaGroup exGroup)
        {
            finalExcluded.cards.AddRange(exGroup.cards);
        }
        else if (excluded is LuaCard exCard)
        {
            finalExcluded.cards.Add(exCard);
        }
        if (this.lastCostGroup != null)
        {
            finalExcluded.cards.AddRange(this.lastCostGroup.cards);
        }

        int lSelf = ConvertToInt(locSelf);
        int lOpp = ConvertToInt(locOpp);

        // Fallback para assinaturas incompletas
        if (lSelf == 0 && lOpp == 0)
        {
            lSelf = 0x3C;
            lOpp = 0x3C;
        }

        LuaGroup candidates = GetMatchingGroup(filterFunc, ConvertToInt(player2), lSelf, lOpp, finalExcluded, extraArgs);

        if (candidates.cards.Count == 0)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            this.lastCostGroup = null; // Limpa a memória de custo
            return UserData.Create(new LuaGroup());
        }

        // Otimização: Se há apenas um alvo possível, seleciona automaticamente (a menos que min > 1) OU se alwaysConfirmSingleTarget for true
        if (!GameManager.Instance.alwaysConfirmSingleTarget && candidates.cards.Count == 1 && ConvertToInt(min) <= 1)
        {
            LuaGroup selectedGroup = new LuaGroup();
            selectedGroup.AddCard(candidates.cards[0]);
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
            
            if (this.currentTargetGroup == null) this.currentTargetGroup = selectedGroup;
            else this.currentTargetGroup.cards.AddRange(selectedGroup.cards);
            
            this.lastCostGroup = null; // Limpa a memória de custo
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
        }

        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            LuaGroup aiChoice = OpponentAI.Instance.SelectLuaTargets(candidates, ConvertToInt(min), ConvertToInt(max));
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(aiChoice);
            
            if (this.currentTargetGroup == null) this.currentTargetGroup = aiChoice;
            else this.currentTargetGroup.cards.AddRange(aiChoice.cards);
            
            this.lastCostGroup = null; // Limpa a memória de custo
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
        }

        if (GameManager.Instance != null)
        {
            // Extrai a lista estrita de CardDisplays
            List<CardDisplay> exactTargets = new List<CardDisplay>();
            foreach (var c in candidates.cards)
            {
                if (c.unityCard != null && c.unityCard.isOnField) exactTargets.Add(c.unityCard);
            }

            // Se todas as cartas alvo estão no campo, usa a Seleção Estrita por Display (Evita duplicação de CardData)
            if (exactTargets.Count == candidates.cards.Count && exactTargets.Count > 0)
            {
                GameManager.Instance.OpenDirectCardDisplaySelection(exactTargets, promptTitle, ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                    LuaGroup selectedGroup = new LuaGroup();
                    foreach (var display in selectedList)
                    {
                        LuaCard match = candidates.cards.Find(lc => lc.unityCard == display);
                        if (match != null) selectedGroup.AddCard(match);
                        else selectedGroup.AddCard(new LuaCard(display));
                    }
                    CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                    
                    if (this.currentTargetGroup == null) this.currentTargetGroup = selectedGroup;
                    else this.currentTargetGroup.cards.AddRange(selectedGroup.cards);
                    
                    this.lastCostGroup = null; 
                    CardEffectManager.Instance.isWaitingForLuaYield = false;
                }, HighlightCategory.GenericTarget);
            }
            else
            {
                // Fallback (ex: selecionando do Cemitério abre a Modal)
                List<CardData> selectableData = new List<CardData>();
                foreach (var c in candidates.cards) if (c.unityData != null) selectableData.Add(c.unityData);

                GameManager.Instance.OpenCardMultiSelection(selectableData, promptTitle, ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                    LuaGroup selectedGroup = new LuaGroup();
                    foreach (var data in selectedList)
                    {
                        LuaCard match = candidates.cards.Find(lc => lc.unityData == data && !selectedGroup.cards.Contains(lc));
                        if (match != null) selectedGroup.AddCard(match);
                        else selectedGroup.AddCard(new LuaCard(data));
                    }
                    CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                    
                    if (this.currentTargetGroup == null) this.currentTargetGroup = selectedGroup;
                    else this.currentTargetGroup.cards.AddRange(selectedGroup.cards);
                    
                    this.lastCostGroup = null; 
                    CardEffectManager.Instance.isWaitingForLuaYield = false;
                }, HighlightCategory.GenericTarget, true); // Força Modal para garantir seleção de pilhas invisíveis
            }
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(new LuaGroup());
            this.lastCostGroup = null; // Limpa a memória de custo
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
    }

    public DynValue SelectMatchingCard(object player, object filterFunc, object player2, object locSelf, object locOpp, object min, object max, object excluded, params object[] extraArgs)
    {
        return InternalSelectMatchingCard(player, filterFunc, player2, locSelf, locOpp, min, max, excluded, HighlightCategory.GenericTarget, extraArgs);
    }

    private DynValue InternalSelectMatchingCard(object player, object filterFunc, object player2, object locSelf, object locOpp, object min, object max, object excluded, HighlightCategory category, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        
        string promptTitle = lastHintMsg;
        lastHintMsg = "Selecione"; // Reseta

        LuaGroup finalExcluded = new LuaGroup();
        if (excluded is LuaGroup exGroup)
        {
            finalExcluded.cards.AddRange(exGroup.cards);
        }
        else if (excluded is LuaCard exCard)
        {
            finalExcluded.cards.Add(exCard);
        }
        if (this.lastCostGroup != null)
        {
            finalExcluded.cards.AddRange(this.lastCostGroup.cards);
        }

        LuaGroup candidates = GetMatchingGroup(filterFunc, ConvertToInt(player2), ConvertToInt(locSelf), ConvertToInt(locOpp), finalExcluded, extraArgs);
        if (candidates.cards.Count == 0) {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            this.lastCostGroup = null; // Limpa a memória de custo
            return UserData.Create(new LuaGroup());
        }

        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            LuaGroup aiChoice = OpponentAI.Instance.SelectLuaTargets(candidates, ConvertToInt(min), ConvertToInt(max));
            // Debug.Log($"<color=orange>[LuaDuel] Bypass IA (SelectMatchingCard) -> Opções válidas: {candidates.cards.Count}, IA escolheu: {aiChoice.cards.Count}</color>");
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(aiChoice);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            this.lastCostGroup = null; // Limpa a memória de custo
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectMatchingCard") });
        }

        List<CardData> selectableData = new List<CardData>();
        foreach (var c in candidates.cards) {
            if (c.unityData != null) selectableData.Add(c.unityData);
        }

        if (GameManager.Instance != null && selectableData.Count > 0)
        {
            int combinedLoc = ConvertToInt(locSelf) | ConvertToInt(locOpp);
            bool forceModal = (combinedLoc & (0x01 | 0x10 | 0x20 | 0x40)) != 0;

            GameManager.Instance.OpenCardMultiSelection(selectableData, promptTitle, ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                LuaGroup selectedGroup = new LuaGroup();
                foreach (var data in selectedList)
                {
                    LuaCard match = candidates.cards.Find(lc => lc.unityData == data && !selectedGroup.cards.Contains(lc));
                    if (match != null) selectedGroup.AddCard(match);
                    else selectedGroup.AddCard(new LuaCard(data));
                }
                CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
                this.lastCostGroup = null; // Limpa a memória de custo
            }, category, forceModal);
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(new LuaGroup());
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            this.lastCostGroup = null; // Limpa a memória de custo
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectMatchingCard") });
    }

    public DynValue SelectReleaseGroup(object player, object filterFunc, object min, object max, object excluded, params object[] extraArgs)
    {
        return InternalSelectMatchingCard(player, filterFunc, player, 0x04, 0, ConvertToInt(min), ConvertToInt(max), excluded, HighlightCategory.Tribute, extraArgs);
    }

    public int GetTargetPlayer() { 
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.targetPlayer;
        return targetPlayer; 
    }
    public int GetTargetParam() { 
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.targetParam;
        return targetParam; 
    }

    public DynValue TossCoin(object player, object count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        GameManager.Instance.TossCoin(ConvertToInt(count), (heads) => {
            object[] results = new object[ConvertToInt(count)];
            for (int i = 0; i < ConvertToInt(count); i++) results[i] = (i < heads) ? 1 : 0;
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(Array.ConvertAll(results, x => DynValue.NewNumber((int)x)));
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        });

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossCoin") });
    }

    public DynValue TossDice(object player, object count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            int c = ConvertToInt(count);
            DynValue[] dynResults = new DynValue[c];
            for (int i = 0; i < c; i++) dynResults[i] = DynValue.NewNumber(UnityEngine.Random.Range(1, 7));
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(dynResults);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossDice") });
        }

        GameManager.Instance.RollDice(ConvertToInt(count), false, (results) => {
            int c = ConvertToInt(count);
            DynValue[] dynResults = new DynValue[c];
            for (int i = 0; i < c; i++) dynResults[i] = DynValue.NewNumber(results[i]);
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(dynResults);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        });

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossDice") });
    }

    // Helper em Corrotina para a Destruição Simultânea com VFX
    private IEnumerator DestroyCardsRoutine(List<CardDisplay> cards)
    {
        int count = 0;
        foreach (var c in cards)
        {
            if (c != null && c.isOnField)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(c);
                GameManager.Instance.MoveCard(c, CardLocation.Graveyard, SendReason.Destroyed);
                count++;
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.25f);
            }
        }
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }
}

// ==============================================================================
