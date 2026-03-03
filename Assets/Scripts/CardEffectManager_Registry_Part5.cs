using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part5()
    {
        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 2001 - 2075)
        // =========================================================================================

        // 2001 - Type Zero Magic Crusher (Discard Spell -> Burn)
        AddEffect("2001", Effect_2001_TypeZeroMagicCrusher);

        // 2002 - Tyranno Infinity (ATK = Banished Dinos * 1000)
        AddEffect("2002", Effect_2002_TyrannoInfinity);

        // 2003 - Tyrant Dragon (Double Attack, Negate Trap)
        AddEffect("2003", Effect_2003_TyrantDragon);

        // 2004 - UFO Turtle (Search Fire)
        AddEffect("2004", Effect_2004_UFOTurtle);

        // 2005 - UFOroid (Search Machine)
        AddEffect("2005", Effect_2005_UFOroid);

        // 2006 - UFOroid Fighter (Fusion Stats)
        AddEffect("2006", Effect_2006_UFOroidFighter);

        // 2007 - Ultimate Baseball Kid (Buff Fire, Burn)
        AddEffect("2007", Effect_2007_UltimateBaseballKid);

        // 2008 - Ultimate Insect LV1 (Level Up)
        AddEffect("2008", Effect_2008_UltimateInsectLV1);

        // 2009 - Ultimate Insect LV3 (Debuff, Level Up)
        AddEffect("2009", Effect_2009_UltimateInsectLV3);

        // 2010 - Ultimate Insect LV5 (Debuff, Level Up)
        AddEffect("2010", Effect_2010_UltimateInsectLV5);

        // 2011 - Ultimate Insect LV7 (Debuff All)
        AddEffect("2011", Effect_2011_UltimateInsectLV7);

        // 2012 - Ultimate Obedient Fiend (Attack Restriction)
        AddEffect("2012", Effect_2012_UltimateObedientFiend);

        // 2013 - Ultimate Offering (Pay 500 Extra Summon)
        AddEffect("2013", Effect_2013_UltimateOffering);

        // 2014 - Ultra Evolution Pill (Tribute Reptile -> SS Dino)
        AddEffect("2014", Effect_2014_UltraEvolutionPill);

        // 2015 - Umi (Field Aqua/Fish/Sea Serpent/Thunder +200)
        AddEffect("2015", Effect_2015_Umi);

        // 2016 - Umiiruka (Field Water +500/-400)
        AddEffect("2016", Effect_2016_Umiiruka);

        // 2017 - Union Attack (Combine ATK)
        AddEffect("2017", Effect_2017_UnionAttack);

        // 2018 - Union Rider (Steal Union)
        AddEffect("2018", Effect_2018_UnionRider);

        // 2020 - United We Stand (Equip +800 per monster)
        AddEffect("2020", Effect_2020_UnitedWeStand);

        // 2021 - Unity (Combine DEF)
        AddEffect("2021", Effect_2021_Unity);

        // 2023 - Unshaven Angler (2 Tributes Water)
        AddEffect("2023", Effect_2023_UnshavenAngler);

        // 2024 - Upstart Goblin (Draw 1, Opp +1000 LP)
        AddEffect("2024", Effect_2024_UpstartGoblin);

        // 2027 - Valkyrion the Magna Warrior (SS Magnet Warriors)
        AddEffect("2027", Effect_2027_ValkyrionTheMagnaWarrior);

        // 2028 - Vampire Baby (SS destroyed)
        AddEffect("2028", Effect_2028_VampireBaby);

        // 2029 - Vampire Genesis (SS Banish Lord)
        AddEffect("2029", Effect_2029_VampireGenesis);

        // 2030 - Vampire Lady (Damage -> Declare Type)
        AddEffect("2030", Effect_2030_VampireLady);

        // 2031 - Vampire Lord (Damage -> Declare Type, Revive)
        AddEffect("2031", Effect_2031_VampireLord);

        // 2032 - Vampire's Curse (Pay 500 Revive +500)
        AddEffect("2032", Effect_2032_VampiresCurse);

        // 2033 - Vampiric Orchis (SS Des Dendle)
        AddEffect("2033", Effect_2033_VampiricOrchis);

        // 2034 - Van'Dalgyon the Dark Dragon Lord (SS Counter Trap)
        AddEffect("2034", Effect_2034_VanDalgyonTheDarkDragonLord);

        // 2035 - Vengeful Bog Spirit (Summoning Sickness)
        AddEffect("2035", Effect_2035_VengefulBogSpirit);

        // 2037 - Versago the Destroyer (Fusion Sub)
        AddEffect("2037", Effect_2037_VersagoTheDestroyer);

        // 2038 - Victory Dragon (Match Winner)
        AddEffect("2038", Effect_2038_VictoryDragon);

        // 2039 - Vile Germs (Equip Plant +300/300)
        AddEffect("2039", Effect_2039_VileGerms);

        // 2040 - Vilepawn Archfiend (Protect Archfiend)
        AddEffect("2040", Effect_2040_VilepawnArchfiend);

        // 2042 - Violet Crystal (Equip Zombie +300/300)
        AddEffect("2042", Effect_2042_VioletCrystal);

        // 2043 - Virus Cannon (Tribute -> Mill Spells)
        AddEffect("2043", Effect_2043_VirusCannon);

        // 2044 - Viser Des (Destroy after 3 turns)
        AddEffect("2044", Effect_2044_ViserDes);

        // 2047 - Waboku (No Damage/Destruction)
        AddEffect("2047", Effect_2047_Waboku);

        // 2048 - Wall Shadow (SS via Labyrinth)
        AddEffect("2048", Effect_2048_WallShadow);

        // 2049 - Wall of Illusion (Bounce Attacker)
        AddEffect("2049", Effect_2049_WallOfIllusion);

        // 2050 - Wall of Revealing Light (Pay LP -> Lock Attack)
        AddEffect("2050", Effect_2050_WallOfRevealingLight);

        // 2051 - Wandering Mummy (Flip Shuffle)
        AddEffect("2051", Effect_2051_WanderingMummy);

        // 2052 - War-Lion Ritual (Ritual)
        AddEffect("2052", Effect_2052_WarLionRitual);

        // 2054 - Warrior Elimination (Destroy Warriors)
        AddEffect("2054", Effect_2054_WarriorElimination);

        // 2055 - Warrior of Tradition (Fusion)
        AddEffect("2055", Effect_2055_WarriorOfTradition);

        // 2057 - Wasteland (Field Dino/Zombie/Rock +200)
        AddEffect("2057", Effect_2057_Wasteland);

        // 2058 - Watapon (SS if added)
        AddEffect("2058", Effect_2058_Watapon);

        // 2065 - Wave-Motion Cannon (Accumulate Burn)
        AddEffect("2065", Effect_2065_WaveMotionCannon);

        // 2066 - Weapon Change (Swap ATK/DEF)
        AddEffect("2066", Effect_2066_WeaponChange);

        // 2068 - Weather Report (Destroy Swords -> Extra BP)
        AddEffect("2068", Effect_2068_WeatherReport);

        // 2071 - Whirlwind Prodigy (2 Tributes Wind)
        AddEffect("2071", Effect_2071_WhirlwindProdigy);

        // 2073 - White Dragon Ritual (Ritual)
        AddEffect("2073", Effect_2073_WhiteDragonRitual);

        // 2074 - White Hole (Anti-Dark Hole)
        AddEffect("2074", c => Debug.Log("White Hole: Protege contra Dark Hole (Requer Chain)."));

        // 2075 - White Magical Hat (Discard on Damage)
        AddEffect("2075", Effect_2075_WhiteMagicalHat);

        // 2076 - White Magician Pikeru (Heal)
        AddEffect("2076", Effect_2076_WhiteMagicianPikeru);

        // 2077 - White Ninja (Flip Destroy Defense)
        AddEffect("2077", Effect_2077_WhiteNinja);

        // 2079 - Wicked-Breaking Flamberge - Baou (Equip +500, Negate)
        AddEffect("2079", Effect_2079_WickedBreakingFlambergeBaou);

        // 2080 - Widespread Ruin (Destroy Highest ATK)
        AddEffect("2080", Effect_2080_WidespreadRuin);

        // 2081 - Wild Nature's Release (ATK+=DEF, Destroy)
        AddEffect("2081", Effect_2081_WildNaturesRelease);

        // 2083 - Windstorm of Etaqua (Change Positions)
        AddEffect("2083", Effect_2083_WindstormOfEtaqua);

        // 2090 - Winged Kuriboh (No Battle Damage)
        AddEffect("2090", Effect_2090_WingedKuriboh);

        // 2091 - Winged Kuriboh LV10 (Nuke & Burn)
        AddEffect("2091", Effect_2091_WingedKuribohLV10);

        // 2092 - Winged Minion (Tribute Buff Fiend)
        AddEffect("2092", Effect_2092_WingedMinion);

        // 2093 - Winged Sage Falcos (Spin on Battle)
        AddEffect("2093", Effect_2093_WingedSageFalcos);

        // 2096 - Witch Doctor of Chaos (Flip Banish GY)
        AddEffect("2096", Effect_2096_WitchDoctorOfChaos);

        // 2097 - Witch of the Black Forest (Search DEF <= 1500)
        AddEffect("2097", Effect_2097_WitchOfTheBlackForest);

        // 2098 - Witch's Apprentice (Field Dark +500, Light -400)
        AddEffect("2098", Effect_2098_WitchsApprentice);

        // 2100 - Wodan the Resident of the Forest (Buff per Plant)
        AddEffect("2100", Effect_2100_WodanTheResidentOfTheForest);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 2101 - 2147)
        // =========================================================================================

        // 2106 - Woodland Sprite (Send Equip -> Burn)
        AddEffect("2106", Effect_2106_WoodlandSprite);

        // 2107 - World Suppression (Negate Field Spell)
        AddEffect("2107", Effect_2107_WorldSuppression);

        // 2111 - Wroughtweiler (Recycle HERO/Poly)
        AddEffect("2111", Effect_2111_Wroughtweiler);

        // 2112 - Wynn the Wind Charmer (Flip Control Wind)
        AddEffect("2112", Effect_2112_WynnTheWindCharmer);

        // 2114 - XY-Dragon Cannon (Fusion Destroy S/T)
        AddEffect("2114", Effect_2114_XYDragonCannon);

        // 2115 - XYZ-Dragon Cannon (Fusion Destroy Card)
        AddEffect("2115", Effect_2115_XYZDragonCannon);

        // 2116 - XZ-Tank Cannon (Fusion Destroy Face-down S/T)
        AddEffect("2116", Effect_2116_XZTankCannon);

        // 2117 - Xing Zhen Hu (Lock 2 Set S/T)
        AddEffect("2117", Effect_2117_XingZhenHu);

        // 2118 - Y-Dragon Head (Union)
        AddEffect("2118", Effect_2118_YDragonHead);

        // 2119 - YZ-Tank Dragon (Fusion Destroy Face-down Monster)
        AddEffect("2119", Effect_2119_YZTankDragon);

        // 2120 - Yado Karu (Bottom Deck)
        AddEffect("2120", Effect_2120_YadoKaru);

        // 2123 - Yamata Dragon (Draw until 5)
        AddEffect("2123", Effect_2123_YamataDragon);

        // 2125 - Yami (Field Fiend/Spellcaster +200, Fairy -200)
        AddEffect("2125", Effect_2125_Yami);

        // 2128 - Yata-Garasu (Skip Draw)
        AddEffect("2128", Effect_2128_YataGarasu);

        // 2129 - Yellow Gadget (Search Green Gadget)
        AddEffect("2129", Effect_2129_YellowGadget);

        // 2130 - Yellow Luster Shield (Continuous Spell +300 DEF)
        AddEffect("2130", Effect_2130_YellowLusterShield);

        // 2131 - Yomi Ship (Destroy Killer)
        AddEffect("2131", Effect_2131_YomiShip);

        // 2133 - Yu-Jo Friendship (Handshake LP Halve)
        AddEffect("2133", Effect_2133_YuJoFriendship);

        // 2134 - Z-Metal Tank (Union)
        AddEffect("2134", Effect_2134_ZMetalTank);

        // 2135 - Zaborg the Thunder Monarch (Destroy Monster)
        AddEffect("2135", Effect_2135_ZaborgTheThunderMonarch);

        // 2138 - Zera Ritual (Ritual)
        AddEffect("2138", Effect_2138_ZeraRitual);

        // 2140 - Zero Gravity (Change Positions)
        AddEffect("2140", Effect_2140_ZeroGravity);

        // 2142 - Zolga (Gain 2000 LP on Tribute)
        AddEffect("2142", Effect_2142_Zolga);

        // 2143 - Zoma the Spirit (Trap Monster Burn)
        AddEffect("2143", Effect_2143_ZomaTheSpirit);

        // 2144 - Zombie Tiger (Union)
        AddEffect("2144", Effect_2144_ZombieTiger);

        // 2146 - Zombyra the Dark (Attack Restriction, Weaken)
        AddEffect("2146", Effect_2146_ZombyraTheDark);

        // 2147 - Zone Eater (Destroy after 5 turns)
        AddEffect("2147", Effect_2147_ZoneEater);
    }
}
