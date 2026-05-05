using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class LuaDuel
{
    // ==============================================================================
    // INTERFACE, HINTS E MENSAGENS (LuaDuel_UI)
    // Modais, Seleções, Minigames e Interrupções de Jogo Assíncronas
    // ==============================================================================

    // --- CORTE 1 ADAPTADO AO SEU LUAHINTS.CS ---
    public void Hint(object msgType, object player, object desc) 
    { 
        int type = ConvertToInt(msgType);
        int descCode = ConvertToInt(desc);

        if (type == 3) // HINT_SELECTMSG
        {
            lastHintMsg = GetHintMessageString(descCode);
        }
        else if (type == 1 || type == 2) // HINT_EVENT ou HINT_MESSAGE
        {
            if (UIManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.isSimulating)
            {
                if (desc is string s) UIManager.Instance.ShowMessage(s);
                else if (descCode > 10000) // Formato LUA para Carta/Efeito (ID * 16 + Index)
                {
                    int cardId = descCode / 16;
                    var cData = GameManager.Instance.cardDatabase?.cardDatabase.Find(c => c.password == cardId.ToString());
                    if (cData != null) UIManager.Instance.ShowMessage($"Effect Activated: {cData.name}");
                }
                else UIManager.Instance.ShowMessage(GetHintMessageString(descCode));
            }
        }
        else if (type == 6) // PHINT_DESC_ADD
        {
            string descStr = desc is string s ? s : GetHintMessageString(descCode);
            if (ConvertToInt(player) == 0) GameManager.Instance.playerHintDesc = descStr;
            else GameManager.Instance.opponentHintDesc = descStr;
        }
        else if (type == 7) // PHINT_DESC_REMOVE
        {
            if (ConvertToInt(player) == 0) GameManager.Instance.playerHintDesc = "";
            else GameManager.Instance.opponentHintDesc = "";
        }
        else if (type >= 200 && type <= 203 && UIManager.Instance != null && !GameManager.Instance.isSimulating) // HINT_SKILL
        {
            string skillName = desc is string s2 ? s2 : GetHintMessageString(descCode);
            UIManager.Instance.ShowMessage($"Skill Ativada:\n{skillName}");
        }
    }

    public DynValue ConfirmDecktop(object player, object count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        LuaGroup topCards = GetDecktopGroup(player, count);
        List<CardData> cardsToShow = topCards.cards.Select(c => c.unityData).Where(d => d != null).ToList();

        string cardNames = string.Join(", ", cardsToShow.Select(c => c.name));
        // Debug.Log($"<color=magenta>[LuaDuel LOG]</color> LUA pediu ConfirmDecktop. Revelando: {cardNames}");

        if (cardsToShow.Count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            GameManager.Instance.OpenCardMultiSelection(cardsToShow, "Top Deck Cards", 0, 0, (selected) => {
                CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            }, HighlightCategory.GenericTarget, true); // Force modal for deck view
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ConfirmDecktop") });
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

    public DynValue SortDecktop(object sort_player, object target_player, object count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        int c = ConvertToInt(count);
        bool isTargetPlayer = IsPlayer(target_player);
        List<CardData> deck = isTargetPlayer ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
        
        List<CardData> topCards = new List<CardData>();
        for (int i = 0; i < c && i < deck.Count; i++) topCards.Add(deck[i]);

        if (topCards.Count > 0 && GameManager.Instance != null && !GameManager.Instance.isSimulating)
        {
            if (ReorderCardsUI.Instance == null)
                ReorderCardsUI.Instance = Resources.FindObjectsOfTypeAll<ReorderCardsUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());

            if (ReorderCardsUI.Instance != null)
            {
                ReorderCardsUI.Instance.Show(topCards, "View/Reorder cards", (reordered) => {
                    for (int i = 0; i < topCards.Count; i++) deck.Remove(topCards[i]);
                    for (int i = reordered.Count - 1; i >= 0; i--) deck.Insert(0, reordered[i]);
                    if (DeckManager.Instance != null) DeckManager.Instance.UpdateDeckVisuals();
                    CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                    CardEffectManager.Instance.isWaitingForLuaYield = false;
                });
                return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SortDecktop") });
            }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
        CardEffectManager.Instance.isWaitingForLuaYield = false;
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SortDecktop") });
    }

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
            string msg = "Do you want to activate this effect?";
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
        bool isTypeDeclare = options.Length > 0 && (opt1 == 70 || opt1 == 71 || opt1 == 72);
       
        if (isTypeDeclare)
        {
            TypeDeclareUI typeUI = TypeDeclareUI.Instance ?? Resources.FindObjectsOfTypeAll<TypeDeclareUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());
            if (typeUI != null)
            {
                List<int> typeOpts = options.Select(o => ConvertToInt(o)).ToList();
                typeUI.Show(typeOpts, (selectedValue) => {
                    // O LUA exige o ÍNDICE do array da escolha, não o número da constante bruta!
                    int index = typeOpts.IndexOf(selectedValue);
                    CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(Mathf.Max(0, index));
                    CardEffectManager.Instance.isWaitingForLuaYield = false;
                });
                return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectOption") });
            }
        }

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
                if (optVal == 704 || optVal == 96) optStrings.Add("Attack (ATK)");
                else if (optVal == 705 || optVal == 97) optStrings.Add("Defense (DEF)");
                else if (optVal == 60) optStrings.Add("Heads"); // SELECT_HEADS
                else if (optVal == 61) optStrings.Add("Tails"); // SELECT_TAILS
                else if (optVal == 70) optStrings.Add("Monster Card"); // DECLTYPE_MONSTER Fallback
                else if (optVal == 71) optStrings.Add("Spell Card"); // DECLTYPE_SPELL Fallback
                else if (optVal == 72) optStrings.Add("Trap Card"); // DECLTYPE_TRAP Fallback
                else if (optVal > 10000)
                {
                    int cardId = optVal / 16;
                    int strIdx = optVal % 16;
                    
                    if (cardId == 24140059) // A Cat of Ill Omen
                    {
                        if (strIdx == 0) optStrings.Add("Place on top of Deck");
                        else if (strIdx == 1) optStrings.Add("Add to hand");
                        else optStrings.Add($"Option {strIdx}");
                    }
                    else
                    {
                        var cData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == cardId.ToString());
                        if (cData != null) optStrings.Add($"Effect {strIdx + 1} ({cData.name})");
                        else optStrings.Add($"Action {strIdx + 1}");
                    }
                }
                else 
                    optStrings.Add($"Option {optVal}");
            }

            MultipleChoiceUI.Instance.Show(optStrings, "Choose an effect:", 1, 1, (selected) => {
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
        // Traduz os códigos de descrição embutidos do YGOPro (ID * 16 + index)
        if (hintCode > 10000)
        {
            int cardId = hintCode / 16;
            if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
            {
                var cData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == cardId.ToString() || c.id == cardId.ToString());
                if (cData != null) return $"Activate effect of {cData.name}?";
            }
            return "Activate card effect?";
        }

        switch (hintCode)
        {
            case 500: return "Select monster(s) to Tribute";
            case 501: return "Select card(s) to discard";
            case 502: return "Select card(s) to destroy";
            case 503: return "Select card(s) to banish";
            case 504: return "Select card(s) to send to GY";
            case 505: return "Select card(s) to return to hand";
            case 506: return "Select card(s) to add to hand";
            case 507: return "Select card(s) to return to Deck";
            case 508: return "Select monster to Normal Summon";
            case 509: return "Select monster to Special Summon";
            case 510: return "Select card to Set";
            case 511: return "Select Fusion Material";
            case 512: return "Select Synchro Material";
            case 513: return "Select Xyz Material";
            case 514: return "Select a face-up card";
            case 515: return "Select a face-down card";
            case 516: return "Select Attack Position monster";
            case 517: return "Select Defense Position monster";
            case 518: return "Select target to equip";
            case 519: return "Select Xyz Material to detach";
            case 520: return "Select monster to change control";
            case 521: return "Select card to replace destruction";
            case 522: return "Select face-up Attack Position monster";
            case 523: return "Select face-up Defense Position monster";
            case 524: return "Select face-down Attack Position monster";
            case 525: return "Select face-down Defense Position monster";
            case 526: return "Select card to reveal";
            case 527: return "Select card to place on field";
            case 528: return "Select monster to change battle position";
            case 529: return "Select a card on your field";
            case 530: return "Select a card on opponent's field";
            case 531: return "Select monster to Tribute";
            case 532: return "Select monster to detach material from";
            case 533: return "Select Link Material";
            case 549: return "Select attack target";
            case 550: return "Select card to apply effect";
            case 551: return "Select target for effect";
            case 552: return "Toss the coin";
            case 553: return "Roll the dice";
            case 554: return "Declare a card type";
            case 555: return "Select an option";
            case 556: return "Select effect to resolve";
            case 560: return "Select a card";
            case 561: return "Select battle position";
            case 562: return "Declare an attribute";
            case 563: return "Declare a monster type (Race)";
            case 564: return "Declare a card name";
            case 565: return "Declare a number";
            case 566: return "Select effect to activate";
            case 567: return "Declare a Level/Rank";
            case 568: return "Select card to resolve";
            case 569: return "Select target zone";
            case 570: return "Select zone to disable";
            case 571: return "Select zone to move to";
            case 572: return "Select card to place counter";
            case 575: return "Select target to negate";
            case 576: return "Choose ATK or DEF";
            case 577: return "Select target to apply to";
            case 578: return "Select material to attach";
            case 579: return "Select card to send to GY";
            default: return $"Select (Action {hintCode})";
        }
    }

    public DynValue AnnounceNumber(object player, params object[] args)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            int defaultNum = (args != null && args.Length > 0) ? ConvertToInt(args[args.Length - 1]) : 1000;
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(defaultNum);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceNumber") });
        }

        Action<int> onNumberSelected = (num) => {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(num);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        };

        if (UIManager.Instance != null && UIManager.Instance.declareNumberUI != null)
        {
            UIManager.Instance.declareNumberUI.Show("Declare a Number", 1, 9999999, onNumberSelected);
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceNumber") });
        }

        onNumberSelected(1000);
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceNumber") });
    }

    public DynValue AnnounceLevel(object player, params object[] args)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            int defaultLvl = (args != null && args.Length > 0) ? ConvertToInt(args[args.Length - 1]) : 4;
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(defaultLvl);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceLevel") });
        }

        Action<int> onLevelSelected = (lvl) => {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(lvl);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        };

        if (UIManager.Instance != null && UIManager.Instance.declareNumberUI != null)
        {
            UIManager.Instance.declareNumberUI.Show("Declare a Level", 1, 12, onLevelSelected);
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceLevel") });
        }

        onLevelSelected(4);
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceLevel") });
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
        
        // Debug.Log($"<color=magenta>[LuaDuel LOG]</color> LUA pediu AnnounceCard. Pausando Engine e abrindo a UI Preditiva...");

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

        if (UIManager.Instance != null && UIManager.Instance.declareCardNameUI != null)
        {
            UIManager.Instance.declareCardNameUI.Show("Declare 1 Card Name", onCardSelected);
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceCard") });
        }

        Debug.LogWarning("[LuaDuel] DeclareCardNameUI não encontrada no UIManager. Retornando ID de teste (Kuriboh).");
        onCardSelected(40640057);
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("AnnounceCard") });
    }

    public DynValue SelectReleaseGroupCost(object player, object filterFunc, object min, object max, object use_hand, object excluded, params object[] extraArgs)
    {
        return InternalSelectMatchingCard(player, filterFunc, player, 0x04 | (ConvertToInt(use_hand) != 0 ? 0x02 : 0), 0, ConvertToInt(min), ConvertToInt(max), excluded, HighlightCategory.Tribute, extraArgs);
    }

    public DynValue SelectDisableField(object player, object count, object locSelf, object locOpp, object filter)
    {
        int pInt = ConvertToInt(player);
        int cInt = ConvertToInt(count);
        int lSelf = ConvertToInt(locSelf);
        int lOpp = ConvertToInt(locOpp);
        
        int selectedMask = 0;
        int selectedCount = 0;

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            bool isPlayer = pInt == 0;
            
            System.Action<Transform[], int, bool> CheckZones = (zones, typeShift, isMyZ) => {
                for (int i = 0; i < zones.Length; i++)
                {
                    if (selectedCount >= cInt) return;
                    if (zones[i].childCount == 0 && !GameManager.Instance.duelFieldUI.IsZoneBlocked(zones[i]))
                    {
                        int bitIndex = i;
                        if (typeShift == 0x08) bitIndex += 8; 
                        if (!isMyZ) bitIndex += 16; 
                        
                        selectedMask |= (1 << bitIndex);
                        selectedCount++;
                    }
                }
            };

            if ((lSelf & 0x04) != 0) CheckZones(isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones, 0x04, true);
            if ((lOpp & 0x04) != 0) CheckZones(!isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones, 0x04, false);
            if ((lSelf & 0x08) != 0) CheckZones(isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones, 0x08, true);
            if ((lOpp & 0x08) != 0) CheckZones(!isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones, 0x08, false);
        }

        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(selectedMask);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectDisableField") });
    }

    public DynValue SelectEffect(object player, object group)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(0);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectEffect") });
    }

    public DynValue ConfirmCards(object player, object targets)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        List<CardData> offFieldCards = new List<CardData>();
        List<CardDisplay> handCardsPlayer = new List<CardDisplay>();
        List<CardDisplay> handCardsOpponent = new List<CardDisplay>();

        if (targets is LuaGroup group)
        {
            // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Alvo é LuaGroup. Cartas: {group.cards.Count}");
            foreach (var c in group.cards)
            {
                if (c.unityCard != null && (!c.unityCard.isOnField || c.unityCard.CurrentLocation == CardLocation.Hand))
                {
                    if (c.unityCard.isPlayerCard) handCardsPlayer.Add(c.unityCard);
                    else handCardsOpponent.Add(c.unityCard);
                }

                if (c.unityCard != null && c.unityCard.isFlipped && c.unityCard.isOnField) c.unityCard.ShowFront();
                else if (c.unityCard == null && c.unityData != null) offFieldCards.Add(c.unityData);
            }
        }
        else if (targets is LuaCard card)
        {
            // Debug.Log($"<color=yellow>[ConfirmCards LOG]</color> Alvo é LuaCard (Single).");
            if (card.unityCard != null && (!card.unityCard.isOnField || card.unityCard.CurrentLocation == CardLocation.Hand))
            {
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
                        "LEVEL CLASH!",
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
            GameManager.Instance.OpenCardMultiSelection(offFieldCards, "Revealed Top Deck Cards", 0, 0, (selected) => {
                if (handCardsPlayer.Count > 0 || handCardsOpponent.Count > 0)
                {
                    CardEffectManager.Instance.StartCoroutine(RevealHandCardsRoutine(handCardsPlayer, handCardsOpponent, 1.5f, ConvertToInt(player)));
                }
                else
                {
                    CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                    CardEffectManager.Instance.isWaitingForLuaYield = false;
                }
            });
        }
        else if (handCardsPlayer.Count > 0 || handCardsOpponent.Count > 0)
        {
            if (GameManager.Instance != null && !GameManager.Instance.isSimulating) {
                CardEffectManager.Instance.StartCoroutine(RevealHandCardsRoutine(handCardsPlayer, handCardsOpponent, 1.5f, ConvertToInt(player)));
            } else {
                CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            }
        }
        else
        {
            CardEffectManager.Instance.StartCoroutine(ConfirmCardsRoutine(1.2f));
        }
        
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("ConfirmCards") });
    }

    private IEnumerator RevealHandCardsRoutine(List<CardDisplay> pHand, List<CardDisplay> oHand, float delay, int viewingPlayer)
    {
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.flipSound);

        // Desliga os layouts temporariamente para evitar que o "esmagamento" da escala acione reorganizações malucas
        UnityEngine.UI.HorizontalLayoutGroup pLayout = GameManager.Instance != null ? GameManager.Instance.playerHandLayoutGroup.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() : null;
        UnityEngine.UI.HorizontalLayoutGroup oLayout = GameManager.Instance != null ? GameManager.Instance.opponentHandLayoutGroup.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() : null;
        if (pLayout != null) pLayout.enabled = false;
        if (oLayout != null) oLayout.enabled = false;

        // Apenas vira a carta travada no mesmo eixo, sem pop-up/hover
        foreach(var c in pHand) { if (c.isFlipped) c.ShowFront(true); }
        foreach(var c in oHand) { if (c.isFlipped) c.ShowFront(true); }

        // Libera o motor LUA imediatamente para que ele possa engatilhar a Seleção, se houver.
        CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
        CardEffectManager.Instance.isWaitingForLuaYield = false;

        yield return new WaitForSeconds(delay);

        // Aguarda a seleção e confirmação do jogador (se houver alguma ativa) antes de fechar a mão
        if (GameManager.Instance != null)
        {
            yield return new WaitWhile(() => GameManager.Instance.isSelectingFromHand || (CardSelectionUI.Instance != null && CardSelectionUI.Instance.gameObject.activeSelf));
        }

        bool playerHandFullyRevealed = pHand.Count > 0 && GameManager.Instance != null && pHand.Count >= GameManager.Instance.playerHand.Count;
        bool oppHandFullyRevealed = oHand.Count > 0 && GameManager.Instance != null && oHand.Count >= GameManager.Instance.opponentHand.Count;

        bool oppWait = false;
        foreach(var c in oHand) { 
            if (GameManager.Instance != null && !GameManager.Instance.showOpponentHand) 
            {
                c.ShowBack(true); // Gira suavemente de volta para faceback sem exceções
                oppWait = true;
            }
        }

        // Aguarda os flips terminarem antes de religar o layout
        if (oppWait)
        {
            float flipWait = DuelFXManager.Instance != null ? DuelFXManager.Instance.flipAnimationDuration : 0.3f;
            float animSpeed = DuelFXManager.Instance != null && DuelFXManager.Instance.animationSpeed > 0 ? DuelFXManager.Instance.animationSpeed : 1f;
            yield return new WaitForSeconds(flipWait / animSpeed);
        }

        if (pLayout != null) pLayout.enabled = true;
        if (oLayout != null) oLayout.enabled = true;

        // Embaralha a mão para impedir contagem de cartas (Hand Tracking)
        if (GameManager.Instance != null)
        {
            if (pHand.Count > 0 && !playerHandFullyRevealed) GameManager.Instance.ShuffleHand(true);
            if (oHand.Count > 0 && !oppHandFullyRevealed) GameManager.Instance.ShuffleHand(false);
        }
    }

    private IEnumerator ConfirmCardsRoutine(float delay)
    {
        CardEffectManager.Instance.yieldReturnValue = DynValue.Nil;
        CardEffectManager.Instance.isWaitingForLuaYield = false;

        yield return new WaitForSeconds(delay);
        
        if (GameManager.Instance != null)
        {
            yield return new WaitWhile(() => GameManager.Instance.isSelectingFromHand || (CardSelectionUI.Instance != null && CardSelectionUI.Instance.gameObject.activeSelf));
        }
    }

    public DynValue SelectTarget(object player, object filterFunc, object player2, object locSelf, object locOpp, object min, object max, object excluded, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        string promptTitle = lastHintMsg;
        lastHintMsg = "Select a target"; // Reseta para o próximo uso

        bool canCancel = !(ConvertToInt(min) > 0 && CardEffectManager.Instance != null && CardEffectManager.Instance.isChainResolving);

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
                }, HighlightCategory.GenericTarget, canCancel);
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
                }, HighlightCategory.GenericTarget, true, canCancel); // Força Modal para garantir seleção de pilhas invisíveis
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
        lastHintMsg = "Select"; // Reseta

        bool canCancel = !(ConvertToInt(min) > 0 && CardEffectManager.Instance != null && CardEffectManager.Instance.isChainResolving);

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
            }, category, forceModal, canCancel);
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
    
    public DynValue DiscardHand(object player, object filter, object min, object max, object reason, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        bool isPlayer = IsPlayer(player);
        int minAmt = ConvertToInt(min);
        int maxAmt = ConvertToInt(max);
        
        bool canCancel = !(minAmt > 0 && CardEffectManager.Instance != null && CardEffectManager.Instance.isChainResolving);
        
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
            GameManager.Instance.OpenCardMultiSelection(validCards, $"Discard between {minAmt} and {maxAmt} card(s)", minAmt, maxAmt, (selectedCards) => {
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
            }, HighlightCategory.GenericTarget, false, canCancel);
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("DiscardHand") });
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

    public DynValue SelectFusionMaterial(object player, object card, object mg, object gc, object chkf)
    {
        return InternalSelectMatchingCard(player, null, player, 0x0E, 0, 1, 99, null, HighlightCategory.Fusion, null);
    }
}