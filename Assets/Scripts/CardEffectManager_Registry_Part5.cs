using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part5()
    {
        /*
        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 2001 - 2100)
        // =========================================================================================

        // 2001 - Type Zero Magic Crusher (Discard Spell -> Burn)
        AddEffect("2001", c => { Debug.Log("Type Zero Magic Crusher: Descarte Magia para 500 dano."); Effect_DirectDamage(c, 500); });

        // 2002 - Tyranno Infinity (ATK = Banished Dinos * 1000)
        AddEffect("2002", c => Debug.Log("Tyranno Infinity: ATK = Dinos banidos * 1000."));

        // 2003 - Tyrant Dragon (Double Attack, Negate Trap)
        AddEffect("2003", c => Debug.Log("Tyrant Dragon: Ataque duplo, nega Trap alvo."));

        // 2004 - UFO Turtle (Search Fire)
        AddEffect("2004", c => Effect_SearchDeck(c, "Fire"));

        // 2005 - UFOroid (Search Machine)
        AddEffect("2005", c => Effect_SearchDeck(c, "Machine"));

        // 2006 - UFOroid Fighter (Fusion Stats)
        AddEffect("2006", c => Debug.Log("UFOroid Fighter: Stats = soma dos materiais."));

        // 2007 - Ultimate Baseball Kid (Buff Fire, Burn)
        AddEffect("2007", c => Debug.Log("Ultimate Baseball Kid: Buff por Fire. Envia Fire para 500 dano."));

        // 2008 - Ultimate Insect LV1 (Level Up)
        AddEffect("2008", c => Effect_LevelUp(c, "2009"));

        // 2009 - Ultimate Insect LV3 (Debuff, Level Up)
        AddEffect("2009", c => Effect_LevelUp(c, "2010"));

        // 2010 - Ultimate Insect LV5 (Debuff, Level Up)
        AddEffect("2010", c => Effect_LevelUp(c, "2011"));

        // 2011 - Ultimate Insect LV7 (Debuff All)
        AddEffect("2011", c => Debug.Log("Ultimate Insect LV7: Oponente perde 700 ATK/DEF."));

        // 2012 - Ultimate Obedient Fiend (Attack Restriction)
        AddEffect("2012", c => Debug.Log("Ultimate Obedient Fiend: Restrição de ataque. Nega efeitos."));

        // 2013 - Ultimate Offering (Pay 500 Extra Summon)
        AddEffect("2013", c => { Effect_PayLP(c, 500); Debug.Log("Ultimate Offering: Invocação Normal extra."); });

        // 2014 - Ultra Evolution Pill (Tribute Reptile -> SS Dino)
        AddEffect("2014", c => Debug.Log("Ultra Evolution Pill: Tributa Réptil, invoca Dinossauro."));

        // 2015 - Umi (Field Aqua/Fish/Sea Serpent/Thunder +200)
        AddEffect("2015", c => { Effect_Field(c, 200, 200, "Aqua"); Effect_Field(c, 200, 200, "Fish"); Effect_Field(c, 200, 200, "Sea Serpent"); Effect_Field(c, 200, 200, "Thunder"); });

        // 2016 - Umiiruka (Field Water +500/-400)
        AddEffect("2016", c => Effect_Field(c, 500, -400, "", "Water"));

        // 2017 - Union Attack (Combine ATK)
        AddEffect("2017", c => Debug.Log("Union Attack: Soma ATK para um monstro."));

        // 2018 - Union Rider (Steal Union)
        AddEffect("2018", c => Debug.Log("Union Rider: Rouba Union do oponente."));

        // 2020 - United We Stand (Equip +800 per monster)
        AddEffect("2020", c => Debug.Log("United We Stand: +800 ATK/DEF por monstro."));

        // 2021 - Unity (Combine DEF)
        AddEffect("2021", c => Debug.Log("Unity: DEF = soma das DEFs."));

        // 2023 - Unshaven Angler (2 Tributes Water)
        AddEffect("2023", c => Debug.Log("Unshaven Angler: 2 tributos para Water."));

        // 2024 - Upstart Goblin (Draw 1, Opp +1000 LP)
        AddEffect("2024", c => { GameManager.Instance.DrawCard(); if(c.isPlayerCard) GameManager.Instance.opponentLP += 1000; else GameManager.Instance.playerLP += 1000; Debug.Log("Upstart Goblin: Compra 1, oponente ganha 1000 LP."); });

        // 2027 - Valkyrion the Magna Warrior (SS Magnet Warriors)
        AddEffect("2027", c => Debug.Log("Valkyrion: SS tributando magnet warriors."));

        // 2028 - Vampire Baby (SS destroyed)
        AddEffect("2028", c => Debug.Log("Vampire Baby: Invoca monstro destruído por batalha."));

        // 2029 - Vampire Genesis (SS Banish Lord)
        AddEffect("2029", c => Debug.Log("Vampire Genesis: SS banindo Lord. Revive Zumbi."));

        // 2030 - Vampire Lady (Damage -> Declare Type)
        AddEffect("2030", c => Debug.Log("Vampire Lady: Dano -> Oponente envia carta do tipo declarado do deck."));

        // 2031 - Vampire Lord (Damage -> Declare Type, Revive)
        AddEffect("2031", c => Debug.Log("Vampire Lord: Dano -> Mill tipo. Revive se destruído por efeito."));

        // 2032 - Vampire's Curse (Pay 500 Revive +500)
        AddEffect("2032", c => Debug.Log("Vampire's Curse: Paga 500 para reviver com +500 ATK."));

        // 2033 - Vampiric Orchis (SS Des Dendle)
        AddEffect("2033", c => Debug.Log("Vampiric Orchis: Invoca Des Dendle."));

        // 2034 - Van'Dalgyon the Dark Dragon Lord (SS Counter Trap)
        AddEffect("2034", c => Debug.Log("Van'Dalgyon: SS após Counter Trap."));

        // 2035 - Vengeful Bog Spirit (Summoning Sickness)
        AddEffect("2035", c => Debug.Log("Vengeful Bog Spirit: Monstros não atacam no turno que são invocados."));

        // 2037 - Versago the Destroyer (Fusion Sub)
        AddEffect("2037", c => Debug.Log("Versago: Substituto de fusão."));

        // 2038 - Victory Dragon (Match Winner)
        AddEffect("2038", c => Debug.Log("Victory Dragon: Ataque direto vence a partida."));

        // 2039 - Vile Germs (Equip Plant +300/300)
        AddEffect("2039", c => Effect_Equip(c, 300, 300, "Plant"));

        // 2040 - Vilepawn Archfiend (Protect Archfiend)
        AddEffect("2040", c => Debug.Log("Vilepawn Archfiend: Protege outros Archfiends."));

        // 2042 - Violet Crystal (Equip Zombie +300/300)
        AddEffect("2042", c => Effect_Equip(c, 300, 300, "Zombie"));

        // 2043 - Virus Cannon (Tribute -> Mill Spells)
        AddEffect("2043", c => Debug.Log("Virus Cannon: Envia magias do oponente ao GY."));

        // 2044 - Viser Des (Destroy after 3 turns)
        AddEffect("2044", c => Debug.Log("Viser Des: Destrói alvo após 3 turnos."));

        // 2047 - Waboku (No Damage/Destruction)
        AddEffect("2047", c => Debug.Log("Waboku: Sem dano de batalha, monstros não morrem."));

        // 2048 - Wall Shadow (SS via Labyrinth)
        AddEffect("2048", c => Debug.Log("Wall Shadow: SS via Magical Labyrinth."));

        // 2049 - Wall of Illusion (Bounce Attacker)
        AddEffect("2049", c => Debug.Log("Wall of Illusion: Retorna atacante para a mão."));

        // 2050 - Wall of Revealing Light (Pay LP -> Lock Attack)
        AddEffect("2050", c => { Effect_PayLP(c, 1000); Debug.Log("Wall of Revealing Light: Bloqueia ataques."); });

        // 2051 - Wandering Mummy (Flip Shuffle)
        AddEffect("2051", c => Debug.Log("Wandering Mummy: Vira face-down e embaralha posições."));

        // 2052 - War-Lion Ritual (Ritual)
        AddEffect("2052", c => Debug.Log("War-Lion Ritual: Ritual."));

        // 2054 - Warrior Elimination (Destroy Warriors)
        AddEffect("2054", c => Effect_DestroyType(c, "Warrior"));

        // 2057 - Wasteland (Field Dino/Zombie/Rock +200)
        AddEffect("2057", c => { Effect_Field(c, 200, 200, "Dinosaur"); Effect_Field(c, 200, 200, "Zombie"); Effect_Field(c, 200, 200, "Rock"); });

        // 2058 - Watapon (SS if added)
        AddEffect("2058", c => Debug.Log("Watapon: SS se adicionado à mão por efeito."));

        // 2065 - Wave-Motion Cannon (Accumulate Burn)
        AddEffect("2065", c => Debug.Log("Wave-Motion Cannon: Dano acumulativo."));

        // 2066 - Weapon Change (Swap ATK/DEF)
        AddEffect("2066", c => Debug.Log("Weapon Change: Troca ATK/DEF de Warrior/Machine."));

        // 2068 - Weather Report (Destroy Swords -> Extra BP)
        AddEffect("2068", c => Debug.Log("Weather Report: Destrói Swords, ganha Battle Phase extra."));

        // 2071 - Whirlwind Prodigy (2 Tributes Wind)
        AddEffect("2071", c => Debug.Log("Whirlwind Prodigy: 2 tributos para Wind."));

        // 2073 - White Dragon Ritual (Ritual)
        AddEffect("2073", c => Debug.Log("White Dragon Ritual: Ritual Paladin."));

        // 2074 - White Hole (Anti-Dark Hole)
        AddEffect("2074", c => Debug.Log("White Hole: Protege contra Dark Hole."));

        // 2075 - White Magical Hat (Discard on Damage)
        AddEffect("2075", c => Debug.Log("White Magical Hat: Descarte ao causar dano."));

        // 2076 - White Magician Pikeru (Heal)
        AddEffect("2076", c => Debug.Log("Pikeru: Cura na Standby."));

        // 2077 - White Ninja (Flip Destroy Defense)
        AddEffect("2077", c => Debug.Log("White Ninja: Destrói monstro em defesa."));

        // 2079 - Wicked-Breaking Flamberge - Baou (Equip +500, Negate)
        AddEffect("2079", c => Debug.Log("Baou: +500 ATK, nega efeitos de monstros destruídos."));

        // 2080 - Widespread Ruin (Destroy Highest ATK)
        AddEffect("2080", c => Debug.Log("Widespread Ruin: Destrói atacante com maior ATK."));

        // 2081 - Wild Nature's Release (ATK+=DEF, Destroy)
        AddEffect("2081", c => Debug.Log("Wild Nature's Release: ATK += DEF, destrói na End Phase."));

        // 2083 - Windstorm of Etaqua (Change Positions)
        AddEffect("2083", c => Debug.Log("Windstorm of Etaqua: Muda posições de batalha."));

        // 2090 - Winged Kuriboh (No Battle Damage)
        AddEffect("2090", c => Debug.Log("Winged Kuriboh: Sem dano de batalha no turno."));

        // 2091 - Winged Kuriboh LV10 (Nuke & Burn)
        AddEffect("2091", c => Debug.Log("Winged Kuriboh LV10: Destrói ataque, causa dano."));

        // 2092 - Winged Minion (Tribute Buff Fiend)
        AddEffect("2092", c => Debug.Log("Winged Minion: Tributa para dar +700 ATK/DEF a Fiend."));

        // 2093 - Winged Sage Falcos (Spin on Battle)
        AddEffect("2093", c => Debug.Log("Winged Sage Falcos: Coloca monstro destruído no topo do deck."));

        // 2096 - Witch Doctor of Chaos (Flip Banish GY)
        AddEffect("2096", c => Debug.Log("Witch Doctor of Chaos: Bane monstro do GY."));

        // 2097 - Witch of the Black Forest (Search DEF <= 1500)
        AddEffect("2097", c => Debug.Log("Witch of the Black Forest: Busca monstro com DEF <= 1500."));

        // 2098 - Witch's Apprentice (Field Dark +500, Light -400)
        AddEffect("2098", c => Effect_Field(c, 500, -400, "", "Dark"));

        // 2100 - Wodan the Resident of the Forest (Buff per Plant)
        AddEffect("2100", c => Debug.Log("Wodan: +100 ATK por Planta."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 2101 - 2147)
        // =========================================================================================

        // 2106 - Woodland Sprite (Send Equip -> Burn)
        AddEffect("2106", c => Debug.Log("Woodland Sprite: Envia Equip para causar 500 dano."));

        // 2107 - World Suppression (Negate Field Spell)
        AddEffect("2107", c => Debug.Log("World Suppression: Nega Field Spell."));

        // 2111 - Wroughtweiler (Recycle HERO/Poly)
        AddEffect("2111", c => Debug.Log("Wroughtweiler: Recupera HERO e Poly."));

        // 2112 - Wynn the Wind Charmer (Flip Control Wind)
        AddEffect("2112", c => Debug.Log("Wynn: Controlar monstro WIND."));

        // 2114 - XY-Dragon Cannon (Fusion Destroy S/T)
        AddEffect("2114", c => Debug.Log("XY-Dragon Cannon: Destrói S/T face-up."));

        // 2115 - XYZ-Dragon Cannon (Fusion Destroy Card)
        AddEffect("2115", c => Debug.Log("XYZ-Dragon Cannon: Destrói carta."));

        // 2116 - XZ-Tank Cannon (Fusion Destroy Face-down S/T)
        AddEffect("2116", c => Debug.Log("XZ-Tank Cannon: Destrói S/T face-down."));

        // 2117 - Xing Zhen Hu (Lock 2 Set S/T)
        AddEffect("2117", c => Debug.Log("Xing Zhen Hu: Trava 2 S/T setadas."));

        // 2118 - Y-Dragon Head (Union)
        AddEffect("2118", c => Debug.Log("Y-Dragon Head: Union para X-Head Cannon."));

        // 2119 - YZ-Tank Dragon (Fusion Destroy Face-down Monster)
        AddEffect("2119", c => Debug.Log("YZ-Tank Dragon: Destrói monstro face-down."));

        // 2120 - Yado Karu (Bottom Deck)
        AddEffect("2120", c => Debug.Log("Yado Karu: Coloca cartas da mão no fundo do deck."));

        // 2123 - Yamata Dragon (Draw until 5)
        AddEffect("2123", c => Debug.Log("Yamata Dragon: Compra até ter 5 cartas ao causar dano."));

        // 2125 - Yami (Field Fiend/Spellcaster +200, Fairy -200)
        AddEffect("2125", c => { Effect_Field(c, 200, 200, "Fiend"); Effect_Field(c, 200, 200, "Spellcaster"); Effect_Field(c, -200, -200, "Fairy"); });

        // 2128 - Yata-Garasu (Skip Draw)
        AddEffect("2128", c => Debug.Log("Yata-Garasu: Oponente pula Draw Phase."));

        // 2129 - Yellow Gadget (Search Green Gadget)
        AddEffect("2129", c => Effect_SearchDeck(c, "Green Gadget"));

        // 2130 - Yellow Luster Shield (Continuous Spell +300 DEF)
        AddEffect("2130", c => Effect_BuffStats(c, 0, 300));

        // 2131 - Yomi Ship (Destroy Killer)
        AddEffect("2131", c => Debug.Log("Yomi Ship: Destrói quem o destruiu."));

        // 2133 - Yu-Jo Friendship (Handshake LP Halve)
        AddEffect("2133", c => Debug.Log("Yu-Jo Friendship: Aperto de mão, iguala LP."));

        // 2134 - Z-Metal Tank (Union)
        AddEffect("2134", c => Debug.Log("Z-Metal Tank: Union para X ou Y."));

        // 2135 - Zaborg the Thunder Monarch (Destroy Monster)
        AddEffect("2135", c => Debug.Log("Zaborg: Destrói monstro ao ser tributado."));

        // 2138 - Zera Ritual (Ritual)
        AddEffect("2138", c => Debug.Log("Zera Ritual: Ritual."));

        // 2140 - Zero Gravity (Change Positions)
        AddEffect("2140", c => Debug.Log("Zero Gravity: Inverte posições de todos os monstros."));

        // 2142 - Zolga (Gain 2000 LP on Tribute)
        AddEffect("2142", c => Debug.Log("Zolga: Ganha 2000 LP se usado como tributo."));

        // 2143 - Zoma the Spirit (Trap Monster Burn)
        AddEffect("2143", c => Debug.Log("Zoma: Trap Monster, causa dano ao morrer."));

        // 2144 - Zombie Tiger (Union)
        AddEffect("2144", c => Debug.Log("Zombie Tiger: Union para Decayed Commander."));

        // 2146 - Zombyra the Dark (Attack Restriction, Weaken)
        AddEffect("2146", c => Debug.Log("Zombyra: Não ataca direto. Perde 200 ATK ao destruir."));

        // 2147 - Zone Eater (Destroy after 5 turns)
        AddEffect("2147", c => Debug.Log("Zone Eater: Destrói atacado após 5 turnos."));
        */
    }
}
