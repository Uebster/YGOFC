using System.Collections.Generic;
using MoonSharp.Interpreter;

public static class Hints
{
    // ============================================================================
    // INJEÇÃO NA ENGINE LUA
    // ============================================================================
    public static void Register(Script luaEngine)
    {
        // ============================================================================
        // 10. INTERFACE, HINTS E MENSAGENS (UI, Hints & Selects)
        // ============================================================================

        // --- Constantes para exibição de dicas (HINT_*) ---
        luaEngine.Globals["HINT_EVENT"] = 1;               // Exibir um evento (ex: início da fase)
        luaEngine.Globals["HINT_MESSAGE"] = 2;             // Exibir uma mensagem genérica
        luaEngine.Globals["HINT_SELECTMSG"] = 3;           // Exibir uma mensagem de seleção (ver HINTMSG_*)
        luaEngine.Globals["HINT_OPSELECTED"] = 4;          // Indicar que o oponente selecionou uma opção
        luaEngine.Globals["HINT_EFFECT"] = 5;              // Exibir descrição de um efeito
        luaEngine.Globals["HINT_RACE"] = 6;                // Exibir uma raça (depreciado)
        luaEngine.Globals["HINT_ATTRIB"] = 7;              // Exibir um atributo (depreciado)
        luaEngine.Globals["HINT_CODE"] = 8;                // Exibir o código de uma carta (ID)
        luaEngine.Globals["HINT_NUMBER"] = 9;              // Exibir um número
        luaEngine.Globals["HINT_CARD"] = 10;               // Exibir uma carta específica
        luaEngine.Globals["HINT_ZONE"] = 11;               // Exibir uma zona do campo (bitmask)
        luaEngine.Globals["HINT_SKILL"] = 200;             // Exibir string de habilidade (Skill)
        luaEngine.Globals["HINT_SKILL_COVER"] = 201;       // Exibir capa de habilidade
        luaEngine.Globals["HINT_SKILL_FLIP"] = 202;        // Exibir virada de habilidade
        luaEngine.Globals["HINT_SKILL_REMOVE"] = 203;      // Exibir remoção de habilidade

        // --- Dicas associadas a cartas (CHINT_*) ---
        luaEngine.Globals["CHINT_TURN"] = 1;               // Exibir número do turno
        luaEngine.Globals["CHINT_CARD"] = 2;               // Exibir uma carta (pode ser mostrada ao oponente)
        luaEngine.Globals["CHINT_RACE"] = 3;               // Exibir uma raça (tipo de monstro)
        luaEngine.Globals["CHINT_ATTRIBUTE"] = 4;          // Exibir um atributo
        luaEngine.Globals["CHINT_NUMBER"] = 5;             // Exibir um número (genérico)
        luaEngine.Globals["CHINT_DESC_ADD"] = 6;           // Adicionar uma linha à descrição da carta
        luaEngine.Globals["CHINT_DESC_REMOVE"] = 7;        // Remover uma linha da descrição da carta

        // --- Dicas associadas a jogadores (PHINT_*) ---
        luaEngine.Globals["PHINT_DESC_ADD"] = 6;           // Adicionar texto à descrição do jogador
        luaEngine.Globals["PHINT_DESC_REMOVE"] = 7;        // Remover texto da descrição do jogador

        // --- Mensagens de seleção (HINTMSG_*) usadas com HINT_SELECTMSG ---
        luaEngine.Globals["HINTMSG_RELEASE"] = 500;        // "Select monster(s) to Tribute"
        luaEngine.Globals["HINTMSG_DISCARD"] = 501;        // "Select card(s) to discard"
        luaEngine.Globals["HINTMSG_DESTROY"] = 502;        // "Select card(s) to destroy"
        luaEngine.Globals["HINTMSG_REMOVE"] = 503;         // "Select card(s) to banish"
        luaEngine.Globals["HINTMSG_TOGRAVE"] = 504;        // "Select card(s) to send to GY"
        luaEngine.Globals["HINTMSG_RTOHAND"] = 505;        // "Select card(s) to return to hand"
        luaEngine.Globals["HINTMSG_ATOHAND"] = 506;        // "Select card(s) to add to hand"
        luaEngine.Globals["HINTMSG_TODECK"] = 507;         // "Select card(s) to return to Deck"
        luaEngine.Globals["HINTMSG_SUMMON"] = 508;         // "Select monster to Normal Summon"
        luaEngine.Globals["HINTMSG_SPSUMMON"] = 509;       // "Select monster to Special Summon"
        luaEngine.Globals["HINTMSG_SET"] = 510;            // "Select card to Set"
        luaEngine.Globals["HINTMSG_FMATERIAL"] = 511;      // "Select Fusion Material"
        luaEngine.Globals["HINTMSG_SMATERIAL"] = 512;      // "Select Synchro Material"
        luaEngine.Globals["HINTMSG_XMATERIAL"] = 513;      // "Select Xyz Material"
        luaEngine.Globals["HINTMSG_FACEUP"] = 514;         // "Select a face-up card"
        luaEngine.Globals["HINTMSG_FACEDOWN"] = 515;       // "Select a face-down card"
        luaEngine.Globals["HINTMSG_ATTACK"] = 516;         // "Select Attack Position monster"
        luaEngine.Globals["HINTMSG_DEFENSE"] = 517;        // "Select Defense Position monster"
        luaEngine.Globals["HINTMSG_EQUIP"] = 518;          // "Select target to equip"
        luaEngine.Globals["HINTMSG_REMOVEXYZ"] = 519;      // "Select Xyz Material to detach"
        luaEngine.Globals["HINTMSG_CONTROL"] = 520;        // "Select monster to change control"
        luaEngine.Globals["HINTMSG_DESREPLACE"] = 521;     // "Select card to replace destruction"
        luaEngine.Globals["HINTMSG_FACEUPATTACK"] = 522;   // "Select face-up Attack Position monster"
        luaEngine.Globals["HINTMSG_FACEUPDEFENSE"] = 523;  // "Select face-up Defense Position monster"
        luaEngine.Globals["HINTMSG_FACEDOWNATTACK"] = 524; // "Select face-down Attack Position monster"
        luaEngine.Globals["HINTMSG_FACEDOWNDEFENSE"] = 525;// "Select face-down Defense Position monster"
        luaEngine.Globals["HINTMSG_CONFIRM"] = 526;        // "Select card to reveal"
        luaEngine.Globals["HINTMSG_TOFIELD"] = 527;        // "Select card to place on field"
        luaEngine.Globals["HINTMSG_POSCHANGE"] = 528;      // "Select monster to change battle position"
        luaEngine.Globals["HINTMSG_SELF"] = 529;           // "Select a card on your field"
        luaEngine.Globals["HINTMSG_OPPO"] = 530;           // "Select a card on opponent's field"
        luaEngine.Globals["HINTMSG_TRIBUTE"] = 531;        // "Select monster to Tribute"
        luaEngine.Globals["HINTMSG_DEATTACHFROM"] = 532;   // "Select monster to detach material from"
        luaEngine.Globals["HINTMSG_LMATERIAL"] = 533;      // "Select Link Material"
        luaEngine.Globals["HINTMSG_ATTACKTARGET"] = 549;   // "Select attack target"
        luaEngine.Globals["HINTMSG_EFFECT"] = 550;         // "Select card to apply effect"
        luaEngine.Globals["HINTMSG_TARGET"] = 551;         // "Select target for effect"
        luaEngine.Globals["HINTMSG_COIN"] = 552;           // "Toss the coin"
        luaEngine.Globals["HINTMSG_DICE"] = 553;           // "Roll the dice"
        luaEngine.Globals["HINTMSG_CARDTYPE"] = 554;       // "Declare a card type"
        luaEngine.Globals["HINTMSG_OPTION"] = 555;         // "Select an option"
        luaEngine.Globals["HINTMSG_RESOLVEEFFECT"] = 556;  // "Select effect to resolve"
        luaEngine.Globals["HINTMSG_SELECT"] = 560;         // "Select a card"
        luaEngine.Globals["HINTMSG_POSITION"] = 561;       // "Select battle position"
        luaEngine.Globals["HINTMSG_ATTRIBUTE"] = 562;      // "Declare an attribute"
        luaEngine.Globals["HINTMSG_RACE"] = 563;           // "Declare a monster type (Race)"
        luaEngine.Globals["HINTMSG_CODE"] = 564;           // "Declare a card name"
        luaEngine.Globals["HINTMSG_NUMBER"] = 565;         // "Declare a number"
        luaEngine.Globals["HINTMSG_EFFACTIVATE"] = 566;    // "Select effect to activate"
        luaEngine.Globals["HINTMSG_LVRANK"] = 567;         // "Declare a Level/Rank"
        luaEngine.Globals["HINTMSG_RESOLVECARD"] = 568;    // "Select card to resolve"
        luaEngine.Globals["HINTMSG_ZONE"] = 569;           // "Select target zone"
        luaEngine.Globals["HINTMSG_DISABLEZONE"] = 570;    // "Select zone to disable"
        luaEngine.Globals["HINTMSG_TOZONE"] = 571;         // "Select zone to move to"
        luaEngine.Globals["HINTMSG_COUNTER"] = 572;        // "Select card to place counter"
        luaEngine.Globals["HINTMSG_NEGATE"] = 575;         // "Select target to negate"
        luaEngine.Globals["HINTMSG_ATKDEF"] = 576;         // "Choose ATK or DEF"
        luaEngine.Globals["HINTMSG_APPLYTO"] = 577;        // "Select target to apply to"
        luaEngine.Globals["HINTMSG_ATTACH"] = 578;         // "Select material to attach"
        luaEngine.Globals["HINTMSG_RTOGRAVE"] = 579;       // "Select card to send to GY"

        // --- Opções de seleção para moeda (SELECT_*) ---
        luaEngine.Globals["SELECT_HEADS"] = 60;             // Escolher "Cara"
        luaEngine.Globals["SELECT_TAILS"] = 61;             // Escolher "Coroa"

        // --- Declaração de tipos de carta (DECLTYPE_*) ---
        luaEngine.Globals["DECLTYPE_MONSTER"] = 70;         // Declarar "Monstro"
        luaEngine.Globals["DECLTYPE_SPELL"] = 71;           // Declarar "Magia"
        luaEngine.Globals["DECLTYPE_TRAP"] = 72;            // Declarar "Armadilha"

        // --- Anúncio de tipo de carta (ANNOUNCE_*) ---
        luaEngine.Globals["ANNOUNCE_CARD"] = 0x7;           // Anunciar carta (genérico)
        luaEngine.Globals["ANNOUNCE_CARD_FILTER"] = 0x8;    // Anunciar carta com filtro
    }

    // ==============================================================================
    // DICIONÁRIO DE TRADUÇÃO (Mensagens Literais em Inglês)
    // Usado pela UI do jogo (C#) para converter os IDs acima em texto visível
    // ==============================================================================
    private static readonly Dictionary<int, string> HintMessages = new Dictionary<int, string>()
    {
        { 500, "Select monster(s) to Tribute" },
        { 501, "Select card(s) to discard" },
        { 502, "Select card(s) to destroy" },
        { 503, "Select card(s) to banish" },
        { 504, "Select card(s) to send to GY" },
        { 505, "Select card(s) to return to hand" },
        { 506, "Select card(s) to add to hand" },
        { 507, "Select card(s) to return to Deck" },
        { 508, "Select monster to Normal Summon" },
        { 509, "Select monster to Special Summon" },
        { 510, "Select card to Set" },
        { 511, "Select Fusion Material" },
        { 512, "Select Synchro Material" },
        { 513, "Select Xyz Material" },
        { 514, "Select a face-up card" },
        { 515, "Select a face-down card" },
        { 516, "Select Attack Position monster" },
        { 517, "Select Defense Position monster" },
        { 518, "Select target to equip" },
        { 519, "Select Xyz Material to detach" },
        { 520, "Select monster to change control" },
        { 521, "Select card to replace destruction" },
        { 522, "Select face-up Attack Position monster" },
        { 523, "Select face-up Defense Position monster" },
        { 524, "Select face-down Attack Position monster" },
        { 525, "Select face-down Defense Position monster" },
        { 526, "Select card to reveal" },
        { 527, "Select card to place on field" },
        { 528, "Select monster to change battle position" },
        { 529, "Select a card on your field" },
        { 530, "Select a card on opponent's field" },
        { 531, "Select monster to Tribute" },
        { 532, "Select monster to detach material from" },
        { 533, "Select Link Material" },
        { 549, "Select attack target" },
        { 550, "Select card to apply effect" },
        { 551, "Select target for effect" },
        { 552, "Toss the coin" },
        { 553, "Roll the dice" },
        { 554, "Declare a card type" },
        { 555, "Select an option" },
        { 556, "Select effect to resolve" },
        { 560, "Select a card" },
        { 561, "Select battle position" },
        { 562, "Declare an attribute" },
        { 563, "Declare a monster type (Race)" },
        { 564, "Declare a card name" },
        { 565, "Declare a number" },
        { 566, "Select effect to activate" },
        { 567, "Declare a Level/Rank" },
        { 568, "Select card to resolve" },
        { 569, "Select target zone" },
        { 570, "Select zone to disable" },
        { 571, "Select zone to move to" },
        { 572, "Select card to place counter" },
        { 575, "Select target to negate" },
        { 576, "Choose ATK or DEF" },
        { 577, "Select target to apply to" },
        { 578, "Select material to attach" },
        { 579, "Select card to send to GY" }
    };

    /// <summary>
    /// Obtém a mensagem de dica (Hint) em inglês para a UI baseada no código numérico fornecido pelo LUA.
    /// </summary>
    public static string GetMessage(int hintCode)
    {
        if (HintMessages.TryGetValue(hintCode, out string translatedMsg))
            return translatedMsg;
        
        return "Select a card"; // Fallback de segurança se o código LUA for desconhecido
    }
}