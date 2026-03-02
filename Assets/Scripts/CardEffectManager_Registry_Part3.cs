using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part3()
    {
        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1001 - 1100)
        // =========================================================================================

        // 1001 - Kangaroo Champ (Change to Defense)
        AddEffect("1001", Effect_1001_KangarooChamp);

        // 1004 - Karakuri Spider (Destroy Dark)
        AddEffect("1004", Effect_1004_KarakuriSpider);

        // 1005 - Karate Man (Double ATK)
        AddEffect("1005", Effect_1005_KarateMan);

        // 1008 - Kazejin (Zero ATK)
        AddEffect("1008", Effect_1008_Kazejin);

        // 1009 - Kelbek (Bounce attacker)
        AddEffect("1009", Effect_1009_Kelbek);

        // 1010 - Keldo (Shuffle GY into Deck)
        AddEffect("1010", Effect_1010_Keldo);

        // 1014 - King Dragun (SS Dragon)
        AddEffect("1014", Effect_1014_KingDragun);

        // 1016 - King Tiger Wanghu (Destroy weak summon)
        AddEffect("1016", Effect_1016_KingTigerWanghu);

        // 1018 - King of the Skull Servants (Stats / Revive)
        AddEffect("1018", Effect_1018_KingOfTheSkullServants);

        // 1019 - King of the Swamp (Search Poly)
        AddEffect("1019", Effect_1019_KingOfTheSwamp);

        // 1020 - King's Knight (SS Jack's Knight)
        AddEffect("1020", Effect_1020_KingsKnight);

        // 1021 - Kiryu (Union)
        AddEffect("1021", Effect_1021_Kiryu);

        // 1022 - Kiseitai (Equip on attack)
        AddEffect("1022", Effect_1022_Kiseitai);

        // 1023 - Kishido Spirit (Battle protection)
        AddEffect("1023", Effect_1023_KishidoSpirit);

        // 1024 - Knight's Title (SS Dark Magician Knight)
        AddEffect("1024", Effect_1024_KnightsTitle);

        // 1025 - Koitsu (Union)
        AddEffect("1025", Effect_1025_Koitsu);

        // 1028 - Kotodama (Destroy duplicates)
        AddEffect("1028", Effect_1028_Kotodama);

        // 1031 - Kozaky's Self-Destruct Button (Damage on destroy)
        AddEffect("1031", Effect_1031_KozakysSelfDestructButton);

        // 1033 - Kryuel (Coin destroy)
        AddEffect("1033", Effect_1033_Kryuel);

        // 1035 - Kunai with Chain (Mode change / Buff)
        AddEffect("1035", Effect_1035_KunaiWithChain);

        // 1037 - Kuriboh (Negate damage)
        AddEffect("1037", Effect_1037_Kuriboh); // Já estava registrado, mas vamos garantir que a implementação esteja correta

        // 1040 - Kycoo the Ghost Destroyer (Banish GY)
        AddEffect("1040", Effect_1040_KycooTheGhostDestroyer);

        // 1046 - Labyrinth of Nightmare (Change positions)
        AddEffect("1046", Effect_1046_LabyrinthOfNightmare);

        // 1047 - Lady Assailant of Flames (Flip Banish/Burn)
        AddEffect("1047", Effect_1047_LadyAssailantOfFlames);

        // 1048 - Lady Ninja Yae (Bounce S/T)
        AddEffect("1048", Effect_1048_LadyNinjaYae);

        // 1049 - Lady Panther (Recycle)
        AddEffect("1049", Effect_1049_LadyPanther);

        // 1051 - Larvae Moth (SS Condition)
        AddEffect("1051", Effect_1051_LarvaeMoth);

        // 1053 - Laser Cannon Armor (Equip Insect +300)
        AddEffect("1053", Effect_1053_LaserCannonArmor);

        // 1054 - Last Day of Witch (Destroy Spellcasters)
        AddEffect("1054", Effect_1054_LastDayOfWitch);

        // 1055 - Last Turn (Win Condition)
        AddEffect("1055", Effect_1055_LastTurn);

        // 1056 - Last Will (SS from Deck)
        AddEffect("1056", Effect_1056_LastWill);

        // 1059 - Lava Battleguard (Buff)
        AddEffect("1059", Effect_1059_LavaBattleguard);

        // 1060 - Lava Golem (Tribute 2 Opp, Burn)
        AddEffect("1060", Effect_1060_LavaGolem);

        // 1063 - Legacy Hunter (Hand Shuffle)
        AddEffect("1063", Effect_1063_LegacyHunter);

        // 1064 - Legacy of Yata-Garasu (Draw)
        AddEffect("1064", Effect_1064_LegacyOfYataGarasu);

        // 1065 - Legendary Black Belt (Burn DEF)
        AddEffect("1065", Effect_1065_LegendaryBlackBelt);

        // 1066 - Legendary Fiend (Gain ATK)
        AddEffect("1066", Effect_1066_LegendaryFiend);

        // 1067 - Legendary Flame Lord (Ritual)
        AddEffect("1067", Effect_1067_LegendaryFlameLord);

        // 1068 - Legendary Jujitsu Master (Spin)
        AddEffect("1068", Effect_1068_LegendaryJujitsuMaster);

        // 1069 - Legendary Sword (Equip Warrior +300)
        AddEffect("1069", Effect_1069_LegendarySword);

        // 1070 - Leghul (Direct Attack)
        AddEffect("1070", Effect_1070_Leghul);

        // 1071 - Lekunga (SS Token)
        AddEffect("1071", Effect_1071_Lekunga);

        // 1075 - Lesser Fiend (Banish destroyed)
        AddEffect("1075", Effect_1075_LesserFiend);

        // 1076 - Level Conversion Lab (Change Level)
        AddEffect("1076", Effect_1076_LevelConversionLab);

        // 1077 - Level Limit - Area B (Defense Position)
        AddEffect("1077", Effect_1077_LevelLimitAreaB);

        // 1078 - Level Up! (SS LV monster)
        AddEffect("1078", Effect_1078_LevelUp);

        // 1079 - Levia-Dragon - Daedalus (Nuke)
        AddEffect("1079", Effect_1079_LeviaDragonDaedalus);

        // 1080 - Life Absorbing Machine (Heal)
        AddEffect("1080", Effect_1080_LifeAbsorbingMachine);

        // 1081 - Light of Intervention (No Set)
        AddEffect("1081", Effect_1081_LightOfIntervention);

        // 1082 - Light of Judgment (Hand/Field destruction)
        AddEffect("1082", Effect_1082_LightOfJudgment);

        // 1083 - Lighten the Load (Reload high level)
        AddEffect("1083", Effect_1083_LightenTheLoad);

        // 1084 - Lightforce Sword (Banish hand)
        AddEffect("1084", Effect_1084_LightforceSword);

        // 1085 - Lightning Blade (Equip Warrior +800)
        AddEffect("1085", Effect_1085_LightningBlade);

        // 1087 - Lightning Vortex (Destroy Face-up)
        AddEffect("1087", Effect_1087_LightningVortex);

        // 1088 - Limiter Removal (Double Machine ATK)
        AddEffect("1088", Effect_1088_LimiterRemoval);

        // 1091 - Little Chimera (Field Fire +500, Water -400)
        AddEffect("1091", Effect_1091_LittleChimera);

        // 1093 - Little-Winguard (Change Pos)
        AddEffect("1093", Effect_1093_LittleWinguard);

        // 1096 - Lone Wolf (Immunity)
        AddEffect("1096", Effect_1096_LoneWolf);

        // 1097 - Lord Poison (Revive Plant)
        AddEffect("1097", Effect_1097_LordPoison);

        // 1098 - Lord of D. (Protect Dragons)
        AddEffect("1098", Effect_1098_LordOfD);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1101 - 1200)
        // =========================================================================================

        // 1101 - Lost Guardian (DEF = banished Rock * 700)
        AddEffect("1101", Effect_1101_LostGuardian);

        // 1103 - Luminous Soldier (Buff vs Dark)
        AddEffect("1103", Effect_1103_LuminousSoldier);

        // 1104 - Luminous Spark (Field Light +500/-400)
        AddEffect("1104", Effect_1104_LuminousSpark);

        // 1112 - Machine Conversion Factory (Equip Machine +300/300)
        AddEffect("1112", Effect_1112_MachineConversionFactory);

        // 1113 - Machine Duplication (SS duplicates)
        AddEffect("1113", Effect_1113_MachineDuplication);

        // 1114 - Machine King (Buff per Machine)
        AddEffect("1114", Effect_1114_MachineKing);

        // 1117 - Mad Sword Beast (Piercing)
        AddEffect("1117", Effect_1117_MadSwordBeast);

        // 1119 - Mage Power (Equip Buff per S/T)
        AddEffect("1119", Effect_MagePower);

        // 1120 - Magic Cylinder (Negate & Burn)
        AddEffect("1120", Effect_MagicCylinder);

        // 1121 - Magic Drain (Counter Spell)
        AddEffect("1121", Effect_1121_MagicDrain);

        // 1122 - Magic Formula (Equip DM +700, Heal)
        AddEffect("1122", Effect_1122_MagicFormula);

        // 1123 - Magic Jammer (Discard 1, Negate Spell)
        AddEffect("1123", Effect_1123_MagicJammer);

        // 1124 - Magic Reflector (Counter on Spell)
        AddEffect("1124", Effect_1124_MagicReflector);

        // 1125 - Magical Arm Shield (Redirect Attack)
        AddEffect("1125", Effect_1125_MagicalArmShield);

        // 1126 - Magical Dimension (Tribute SS Destroy)
        AddEffect("1126", Effect_1126_MagicalDimension);

        // 1127 - Magical Explosion (Burn per Spell in GY)
        AddEffect("1127", Effect_1127_MagicalExplosion);

        // 1129 - Magical Hats (Hide monster)
        AddEffect("1129", Effect_1129_MagicalHats);

        // 1130 - Magical Labyrinth (Equip Wall, SS Shadow)
        AddEffect("1130", Effect_1130_MagicalLabyrinth);

        // 1131 - Magical Marionette (Counters, Destroy)
        AddEffect("1131", Effect_1131_MagicalMarionette);

        // 1132 - Magical Merchant (Excavate)
        AddEffect("1132", Effect_1132_MagicalMerchant);

        // 1133 - Magical Plant Mandragola (Counters)
        AddEffect("1133", Effect_1133_MagicalPlantMandragola);

        // 1134 - Magical Scientist (Pay 1000 SS Fusion)
        AddEffect("1134", Effect_1134_MagicalScientist);

        // 1135 - Magical Stone Excavation (Discard 2, Add Spell)
        AddEffect("1135", Effect_1135_MagicalStoneExcavation);

        // 1136 - Magical Thorn (Burn on discard)
        AddEffect("1136", Effect_1136_MagicalThorn);

        // 1137 - Magician of Black Chaos (Ritual)
        AddEffect("1137", Effect_1137_MagicianOfBlackChaos);

        // 1138 - Magician of Faith (Flip Add Spell)
        AddEffect("1138", Effect_1138_MagicianOfFaith);

        // 1139 - Magician's Valkyria (Protect Spellcasters)
        AddEffect("1139", Effect_1139_MagiciansValkyria);

        // 1140 - Maha Vailo (Buff per Equip)
        AddEffect("1140", Effect_1140_MahaVailo);

        // 1141 - Maharaghi (Spirit, Peek)
        AddEffect("1141", Effect_1141_Maharaghi);

        // 1142 - Maiden of the Aqua (Umi field)
        AddEffect("1142", Effect_1142_MaidenOfTheAqua);

        // 1144 - Maji-Gire Panda (Buff per Beast destroyed)
        AddEffect("1144", Effect_1144_MajiGirePanda);

        // 1145 - Major Riot (Return -> SS)
        AddEffect("1145", Effect_1145_MajorRiot);

        // 1146 - Maju Garzett (ATK = Tributes)
        AddEffect("1146", Effect_1146_MajuGarzett);

        // 1147 - Makiu, the Magical Mist (Destroy DEF < ATK)
        AddEffect("1147", Effect_1147_MakiuTheMagicalMist);

        // 1148 - Makyura the Destructor (Trap from hand)
        AddEffect("1148", Effect_1148_MakyuraTheDestructor);

        // 1149 - Malevolent Catastrophe (Destroy S/T on attack)
        AddEffect("1149", Effect_1149_MalevolentCatastrophe);

        // 1150 - Malevolent Nuzzler (Equip +700)
        AddEffect("1150", Effect_1150_MalevolentNuzzler);

        // 1151 - Malice Dispersion (Discard 1, Destroy Continuous Traps)
        AddEffect("1151", Effect_1151_MaliceDispersion);

        // 1152 - Malice Doll of Demise (Revive)
        AddEffect("1152", Effect_1152_MaliceDollOfDemise);

        // 1155 - Man-Eater Bug (Flip Destroy)
        AddEffect("1155", Effect_1155_ManEaterBug);

        // 1159 - Man-Thro' Tro' (Tribute Normal -> 800 dmg)
        AddEffect("1159", Effect_1159_ManThroTro);

        // 1160 - Manga Ryu-Ran (Toon)
        AddEffect("1160", Effect_1160_MangaRyuRan);

        // 1161 - Manju of the Ten Thousand Hands (Search Ritual)
        AddEffect("1161", Effect_1161_ManjuOfTheTenThousandHands);

        // 1162 - Manticore of Darkness (Revive loop)
        AddEffect("1162", Effect_1162_ManticoreOfDarkness);

        // 1163 - Marauding Captain (SS from hand, Lock attack)
        AddEffect("1163", Effect_1163_MaraudingCaptain);

        // 1165 - Marshmallon (Indestructible, Burn)
        AddEffect("1165", Effect_1165_Marshmallon);

        // 1166 - Marshmallon Glasses (Redirect attack)
        AddEffect("1166", Effect_1166_MarshmallonGlasses);

        // 1167 - Maryokutai (Negate Spell)
        AddEffect("1167", Effect_1167_Maryokutai);

        // 1169 - Mask of Brutality (Equip +1000/-1000)
        AddEffect("1169", Effect_1169_MaskOfBrutality);

        // 1170 - Mask of Darkness (Flip Add Trap)
        AddEffect("1170", Effect_1170_MaskOfDarkness);

        // 1171 - Mask of Dispel (Burn 500)
        AddEffect("1171", Effect_1171_MaskOfDispel);

        // 1172 - Mask of Restrict (No Tributes)
        AddEffect("1172", Effect_1172_MaskOfRestrict);

        // 1173 - Mask of Weakness (Debuff -700)
        AddEffect("1173", Effect_1173_MaskOfWeakness);

        // 1174 - Mask of the Accursed (Lock Attack, Burn)
        AddEffect("1174", Effect_1174_MaskOfTheAccursed);

        // 1175 - Masked Beast Des Gardius (Snatch Steal on death)
        AddEffect("1175", Effect_1175_MaskedBeastDesGardius);

        // 1177 - Masked Dragon (Float into Dragon)
        AddEffect("1177", Effect_1177_MaskedDragon);

        // 1178 - Masked Sorcerer (Draw on damage)
        AddEffect("1178", Effect_1178_MaskedSorcerer);

        // 1179 - Mass Driver (Tribute -> 400 dmg)
        AddEffect("1179", Effect_1179_MassDriver);

        // 1182 - Master Monk (Double Attack)
        AddEffect("1182", Effect_1182_MasterMonk);

        // 1184 - Mataza the Zapper (Double Attack, No Control Switch)
        AddEffect("1184", Effect_1184_MatazaTheZapper);

        // 1186 - Maximum Six (Roll die -> Gain ATK)
        AddEffect("1186", Effect_1186_MaximumSix);

        // 1187 - Mazera DeVille (Hand Destruction)
        AddEffect("1187", Effect_1187_MazeraDeVille);

        // 1190 - Mecha-Dog Marron (Burn on destroy)
        AddEffect("1190", Effect_1190_MechaDogMarron);

        // 1192 - Mechanical Hound (No Spells)
        AddEffect("1192", Effect_1192_MechanicalHound);

        // 1196 - Medusa Worm (Flip Destroy, Cycle)
        AddEffect("1196", Effect_1196_MedusaWorm);

        // 1197 - Mefist the Infernal General (Piercing, Discard)
        AddEffect("1197", Effect_1197_MefistTheInfernalGeneral);

        // 1199 - Mega Ton Magical Cannon (Remove 10 counters -> Nuke)
        AddEffect("1199", Effect_1199_MegaTonMagicalCannon);

        // 1200 - Megamorph (Double/Halve ATK)
        AddEffect("1200", Effect_1200_Megamorph);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1201 - 1300)
        // =========================================================================================

        // 1201 - Megarock Dragon (SS Banish Rocks)
        AddEffect("1201", Effect_1201_MegarockDragon);

        // 1204 - Meltiel, Sage of the Sky (Gain LP / Destroy)
        AddEffect("1204", Effect_1204_MeltielSageOfTheSky);

        // 1205 - Memory Crusher (Mill Extra Deck)
        AddEffect("1205", Effect_1205_MemoryCrusher);

        // 1207 - Mermaid Knight (Double Attack with Umi)
        AddEffect("1207", Effect_1207_MermaidKnight);

        // 1208 - Mesmeric Control (Lock Battle Position)
        AddEffect("1208", Effect_1208_MesmericControl);

        // 1209 - Messenger of Peace (Lock Attack >= 1500)
        AddEffect("1209", Effect_1209_MessengerOfPeace);

        // 1211 - Metal Detector (Negate Continuous Trap)
        AddEffect("1211", Effect_1211_MetalDetector);

        // 1215 - Metal Reflect Slime (Trap Monster)
        AddEffect("1215", Effect_1215_MetalReflectSlime);

        // 1216 - Metallizing Parasite - Lunatite (Union Protect)
        AddEffect("1216", Effect_1216_MetallizingParasiteLunatite);

        // 1217 - Metalmorph (Trap Equip)
        AddEffect("1217", Effect_1217_Metalmorph);

        // 1218 - Metalsilver Armor (Redirect Target)
        AddEffect("1218", Effect_1218_MetalsilverArmor);

        // 1219 - Metalzoa (SS from Deck)
        AddEffect("1219", Effect_1219_Metalzoa);

        // 1220 - Metamorphosis (Tribute -> Fusion)
        AddEffect("1220", Effect_1220_Metamorphosis);

        // 1223 - Meteor of Destruction (Burn 1000)
        AddEffect("1223", Effect_1223_MeteorOfDestruction);

        // 1224 - Meteorain (Piercing for all)
        AddEffect("1224", Effect_1224_Meteorain);

        // 1225 - Michizure (Destroy on destroy)
        AddEffect("1225", Effect_1225_Michizure);

        // 1226 - Micro Ray (DEF 0)
        AddEffect("1226", Effect_1226_MicroRay);

        // 1227 - Mid Shield Gardna (Flip Negate Spell)
        AddEffect("1227", Effect_1227_MidShieldGardna);

        // 1232 - Millennium Scorpion (Gain ATK on kill)
        AddEffect("1232", Effect_1232_MillenniumScorpion);

        // 1234 - Milus Radiant (Field Earth +500, Wind -400)
        AddEffect("1234", Effect_1234_MilusRadiant);

        // 1235 - Minar (Burn on discard)
        AddEffect("1235", Effect_1235_Minar);

        // 1236 - Mind Control (Take Control)
        AddEffect("1236", Effect_1236_MindControl);

        // 1237 - Mind Crush (Hand Destruction)
        AddEffect("1237", Effect_1237_MindCrush);

        // 1238 - Mind Haxorz (Peek Hand/Set)
        AddEffect("1238", Effect_1238_MindHaxorz);

        // 1239 - Mind Wipe (Hand Refresh)
        AddEffect("1239", Effect_1239_MindWipe);

        // 1240 - Mind on Air (Reveal Hand)
        AddEffect("1240", Effect_1240_MindOnAir);

        // 1241 - Mine Golem (Burn on destroy)
        AddEffect("1241", Effect_1241_MineGolem);

        // 1242 - Minefield Eruption (Burn per Golem)
        AddEffect("1242", Effect_1242_MinefieldEruption);

        // 1244 - Minor Goblin Official (Burn Standby)
        AddEffect("1244", Effect_1244_MinorGoblinOfficial);

        // 1245 - Miracle Dig (Recycle Banished)
        AddEffect("1245", Effect_1245_MiracleDig);

        // 1246 - Miracle Fusion (Fusion Banish GY)
        AddEffect("1246", Effect_1246_MiracleFusion);

        // 1247 - Miracle Restoring (Revive DM/BB)
        AddEffect("1247", Effect_1247_MiracleRestoring);

        // 1248 - Mirage Dragon (No Traps Battle)
        AddEffect("1248", Effect_1248_MirageDragon);

        // 1249 - Mirage Knight (Battle Logic)
        AddEffect("1249", Effect_1249_MirageKnight);

        // 1250 - Mirage of Nightmare (Draw/Discard)
        AddEffect("1250", Effect_1250_MirageOfNightmare);

        // 1251 - Mirror Force (Destroy Attack Pos)
        AddEffect("1251", Effect_1251_MirrorForce);

        // 1252 - Mirror Wall (Halve ATK)
        AddEffect("1252", Effect_1252_MirrorWall);

        // 1254 - Mispolymerization (Return Fusion)
        AddEffect("1254", Effect_1254_Mispolymerization);

        // 1255 - Moai Interceptor Cannons (Flip Face-down)
        AddEffect("1255", Effect_1255_MoaiInterceptorCannons);

        // 1256 - Mobius the Frost Monarch (Destroy 2 S/T)
        AddEffect("1256", Effect_1256_MobiusTheFrostMonarch);

        // 1257 - Moisture Creature (Destroy S/T)
        AddEffect("1257", Effect_1257_MoistureCreature);

        // 1259 - Mokey Mokey Smackdown (Buff)
        AddEffect("1259", Effect_1259_MokeyMokeySmackdown);

        // 1261 - Molten Destruction (Field Fire +500/-400)
        AddEffect("1261", Effect_1261_MoltenDestruction);

        // 1262 - Molten Zombie (Draw on SS)
        AddEffect("1262", Effect_1262_MoltenZombie);

        // 1264 - Monk Fighter (No Battle Damage)
        AddEffect("1264", Effect_1264_MonkFighter);

        // 1266 - Monster Eye (Recycle Poly)
        AddEffect("1266", Effect_1266_MonsterEye);

        // 1267 - Monster Gate (Excavate SS)
        AddEffect("1267", Effect_1267_MonsterGate);

        // 1268 - Monster Reborn (Revive)
        AddEffect("1268", Effect_1268_MonsterReborn);

        // 1269 - Monster Recovery (Shuffle Hand/Field)
        AddEffect("1269", Effect_1269_MonsterRecovery);

        // 1274 - Mooyan Curry (Gain 200 LP)
        AddEffect("1274", Effect_1274_MooyanCurry);

        // 1275 - Morale Boost (Gain/Lose LP on Equip)
        AddEffect("1275", Effect_1275_MoraleBoost);

        // 1276 - Morinphen (Normal)
        AddEffect("1276", Effect_1276_Morinphen);

        // 1277 - Morphing Jar (Hand Reset)
        AddEffect("1277", Effect_1277_MorphingJar);

        // 1278 - Morphing Jar #2 (Deck Reset)
        AddEffect("1278", Effect_1278_MorphingJar2);

        // 1279 - Mother Grizzly (Search Water)
        AddEffect("1279", Effect_1279_MotherGrizzly);

        // 1280 - Mountain (Field Dragon/WingedBeast/Thunder +200)
        AddEffect("1280", Effect_1280_Mountain);

        // 1281 - Mountain Warrior (Normal)
        AddEffect("1281", Effect_1281_MountainWarrior);

        // 1282 - Mr. Volcano (Normal)
        AddEffect("1282", Effect_1282_MrVolcano);

        // 1283 - Mucus Yolk (Direct Attack / Gain ATK)
        AddEffect("1283", Effect_1283_MucusYolk);

        // 1284 - Mudora (Buff per Fairy)
        AddEffect("1284", Effect_1284_Mudora);

        // 1285 - Muka Muka (Buff per Hand)
        AddEffect("1285", Effect_1285_MukaMuka);

        // 1286 - Muko (Anti-Draw)
        AddEffect("1286", Effect_1286_Muko);

        // 1287 - Multiplication of Ants (Tokens)
        AddEffect("1287", Effect_1287_MultiplicationOfAnts);

        // 1288 - Multiply (Kuriboh Tokens)
        AddEffect("1288", Effect_1288_Multiply);

        // 1291 - Mushroom Man #2 (Burn Control)
        AddEffect("1291", Effect_1291_MushroomMan2);

        // 1292 - Musician King (Fusion)
        AddEffect("1292", Effect_1292_MusicianKing);

        // 1293 - Mustering of the Dark Scorpions (Swarm)
        AddEffect("1293", Effect_1293_MusteringOfTheDarkScorpions);

        // 1294 - My Body as a Shield (Negate Destroy)
        AddEffect("1294", Effect_1294_MyBodyAsAShield);

        // 1295 - Mysterious Guard (Flip Bounce)
        AddEffect("1295", Effect_1295_MysteriousGuard);

        // 1296 - Mysterious Puppeteer (Gain LP on Summon)
        AddEffect("1296", Effect_1296_MysteriousPuppeteer);

        // 1298 - Mystic Box (Destroy & Swap)
        AddEffect("1298", Effect_1298_MysticBox);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1301 - 1400)
        // =========================================================================================

        // 1301 - Mystic Lamp (Direct Attack)
        AddEffect("1301", c => Debug.Log("Mystic Lamp: Ataque direto."));

        // 1302 - Mystic Plasma Zone (Field Dark +500/-400)
        AddEffect("1302", c => Effect_Field(c, 500, -400, "", "Dark"));

        // 1303 - Mystic Probe (Negate Continuous Spell)
        AddEffect("1303", c => Debug.Log("Mystic Probe: Nega Spell Contínua."));

        // 1304 - Mystic Swordsman LV2 (Destroy Face-down)
        AddEffect("1304", c => Debug.Log("Mystic Swordsman LV2: Destrói face-down sem flip."));

        // 1305 - Mystic Swordsman LV4 (Destroy Face-down)
        AddEffect("1305", c => Debug.Log("Mystic Swordsman LV4: Destrói face-down sem flip."));

        // 1306 - Mystic Swordsman LV6 (Destroy Face-down / Top Deck)
        AddEffect("1306", c => Debug.Log("Mystic Swordsman LV6: Destrói face-down e põe no topo."));

        // 1307 - Mystic Tomato (Search Dark)
        AddEffect("1307", c => Effect_SearchDeck(c, "Dark"));

        // 1308 - Mystical Beast of Serket (Eat Monster)
        AddEffect("1308", c => Debug.Log("Serket: Destrói monstro batalhado e ganha 500 ATK."));

        // 1318 - Mystical Space Typhoon (Destroy S/T)
        AddEffect("1318", Effect_MST);

        // 1319 - Mystik Wok (Tribute -> Heal)
        AddEffect("1319", c => Debug.Log("Mystik Wok: Tributa monstro para ganhar LP (ATK ou DEF)."));

        // 1324 - Necrovalley (Field GK +500, GY Lock)
        AddEffect("1324", c => { Effect_Field(c, 500, 500, "Gravekeeper's"); Debug.Log("Necrovalley: Bloqueia efeitos no GY."); });

        // 1325 - Needle Ball (Flip Burn)
        AddEffect("1325", c => { Effect_PayLP(c, 2000); Effect_DirectDamage(c, 1000); });

        // 1326 - Needle Burrower (Burn on destroy)
        AddEffect("1326", c => Debug.Log("Needle Burrower: Dano igual Nível x 500."));

        // 1327 - Needle Ceiling (Destroy all if 4+)
        AddEffect("1327", c => Debug.Log("Needle Ceiling: Destrói todos monstros se houver 4 ou mais."));

        // 1329 - Needle Worm (Mill 5)
        AddEffect("1329", c => Debug.Log("Needle Worm: Oponente descarta 5 do topo do deck."));

        // 1330 - Negate Attack (Negate & End Battle)
        AddEffect("1330", c => Debug.Log("Negate Attack: Nega ataque e encerra Battle Phase."));

        // 1331 - Neko Mane King (End Turn)
        AddEffect("1331", c => Debug.Log("Neko Mane King: Encerra o turno do oponente."));

        // 1338 - Newdoria (Destroy on destroy)
        AddEffect("1338", c => Debug.Log("Newdoria: Destrói 1 monstro ao ser destruído."));

        // 1339 - Night Assailant (Flip Destroy / Recycle)
        AddEffect("1339", c => Debug.Log("Night Assailant: Flip destrói monstro. Recupera Flip do GY."));

        // 1341 - Nightmare Horse (Direct Attack)
        AddEffect("1341", c => Debug.Log("Nightmare Horse: Ataque direto."));

        // 1342 - Nightmare Penguin (Buff Water / Bounce)
        AddEffect("1342", c => Debug.Log("Nightmare Penguin: Buff Water. Flip retorna carta."));

        // 1344 - Nightmare Wheel (Lock & Burn)
        AddEffect("1344", c => Debug.Log("Nightmare Wheel: Prende monstro e causa 500 dano."));

        // 1345 - Nightmare's Steelcage (Stall)
        AddEffect("1345", c => Debug.Log("Nightmare's Steelcage: Ninguém ataca por 2 turnos."));

        // 1346 - Nimble Momonga (Heal & SS)
        AddEffect("1346", c => { Effect_GainLP(c, 1000); Debug.Log("Nimble Momonga: SS cópias do deck."); });

        // 1353 - Nobleman of Crossout (Banish Face-down)
        AddEffect("1353", c => Debug.Log("Nobleman of Crossout: Destrói e bane face-down."));

        // 1354 - Nobleman of Extermination (Banish Face-down S/T)
        AddEffect("1354", c => Debug.Log("Nobleman of Extermination: Destrói e bane S/T face-down."));

        // 1355 - Nobleman-Eater Bug (Destroy 2)
        AddEffect("1355", c => Debug.Log("Nobleman-Eater Bug: Destrói 2 monstros."));

        // 1364 - Obnoxious Celtic Guard (Battle Protection)
        AddEffect("1364", c => Debug.Log("Obnoxious Celtic Guard: Não morre por monstros 1900+ ATK."));

        // 1368 - Offerings to the Doomed (Destroy & Skip Draw)
        AddEffect("1368", c => Debug.Log("Offerings to the Doomed: Destrói face-up, pula Draw Phase."));

        // 1371 - Ojama Delta Hurricane!! (Nuke)
        AddEffect("1371", c => Debug.Log("Ojama Delta Hurricane!!: Destrói tudo do oponente se tiver Ojamas."));

        // 1374 - Ojama Trio (Tokens)
        AddEffect("1374", c => Debug.Log("Ojama Trio: Invoca 3 Tokens no campo do oponente."));

        // 1376 - Old Vindictive Magician (Flip Destroy)
        AddEffect("1376", c => Effect_FlipDestroy(c, TargetType.Monster));

        // 1382 - Ookazi (Burn 800)
        AddEffect("1382", c => Effect_DirectDamage(c, 800));

        // 1384 - Opti-Camouflage Armor (Direct Attack Lv1)
        AddEffect("1384", c => Debug.Log("Opti-Camouflage Armor: Nível 1 ataca direto."));

        // 1387 - Ordeal of a Traveler (Hand Game)
        AddEffect("1387", c => Debug.Log("Ordeal of a Traveler: Oponente adivinha carta da mão."));

        // 1397 - Painful Choice (Search 5)
        AddEffect("1397", c => Debug.Log("Painful Choice: Seleciona 5, oponente escolhe 1 para sua mão."));

        // 1398 - Paladin of White Dragon (Ritual / SS Blue-Eyes)
        AddEffect("1398", c => Debug.Log("Paladin of White Dragon: Ritual. Tributa para invocar Blue-Eyes."));

        // 1400 - Pandemonium (Field Archfiend)
        AddEffect("1400", c => Debug.Log("Pandemonium: Busca Archfiend quando um é destruído."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1401 - 1500)
        // =========================================================================================

        // 1401 - Pandemonium Watchbear (Protect Pandemonium)
        AddEffect("1401", c => Debug.Log("Pandemonium Watchbear: Protege Pandemonium."));

        // 1402 - Panther Warrior (Tribute to attack)
        AddEffect("1402", c => Debug.Log("Panther Warrior: Tributa 1 para atacar."));

        // 1403 - Paralyzing Potion (Equip non-Machine no attack)
        AddEffect("1403", c => Debug.Log("Paralyzing Potion: Impede ataque de não-Máquina."));

        // 1404 - Parasite Paracide (Flip Shuffle into Opp Deck)
        AddEffect("1404", c => Debug.Log("Parasite Paracide: Embaralha no deck do oponente."));

        // 1406 - Patrician of Darkness (Choose attack targets)
        AddEffect("1406", c => Debug.Log("Patrician of Darkness: Escolhe alvos de ataque."));

        // 1407 - Patroid (Look face-down)
        AddEffect("1407", c => Debug.Log("Patroid: Olha carta face-down."));

        // 1408 - Patrol Robo (Look face-down standby)
        AddEffect("1408", c => Debug.Log("Patrol Robo: Olha carta face-down na Standby."));

        // 1410 - Penalty Game! (Lock Draw or S/T)
        AddEffect("1410", c => Debug.Log("Penalty Game!: Bloqueia Draw ou S/T."));

        // 1412 - Penguin Knight (Shuffle GY to Deck)
        AddEffect("1412", c => Debug.Log("Penguin Knight: Recicla GY se millado."));

        // 1413 - Penguin Soldier (Flip Bounce 2)
        AddEffect("1413", c => Debug.Log("Penguin Soldier: Retorna 2 monstros para a mão."));

        // 1414 - Penumbral Soldier Lady (Buff vs Light)
        AddEffect("1414", c => Debug.Log("Penumbral Soldier Lady: +1000 ATK contra LIGHT."));

        // 1416 - Perfect Machine King (Buff per Machine)
        AddEffect("1416", c => Debug.Log("Perfect Machine King: +500 ATK por Máquina."));

        // 1417 - Perfectly Ultimate Great Moth (SS Condition)
        AddEffect("1417", c => Debug.Log("Perfectly Ultimate Great Moth: SS difícil."));

        // 1418 - Performance of Sword (Ritual)
        AddEffect("1418", c => Debug.Log("Performance of Sword: Ritual."));

        // 1419 - Peten the Dark Clown (Banish to SS)
        AddEffect("1419", c => Debug.Log("Peten: Bane do GY para invocar outro."));

        // 1426 - Pharaoh's Treasure (Shuffle, Draw -> Add from GY)
        AddEffect("1426", c => Debug.Log("Pharaoh's Treasure: Efeito complexo de deck."));

        // 1428 - Phoenix Wing Wind Blast (Discard -> Spin)
        AddEffect("1428", c => Debug.Log("Phoenix Wing Wind Blast: Descarta 1, topo do deck."));

        // 1429 - Physical Double (Token Copy)
        AddEffect("1429", c => Debug.Log("Physical Double: Cria Token com stats do oponente."));

        // 1430 - Pikeru's Circle of Enchantment (No Effect Damage)
        AddEffect("1430", c => Debug.Log("Pikeru's Circle: Sem dano de efeito."));

        // 1431 - Pikeru's Second Sight (Reveal Draws)
        AddEffect("1431", c => Debug.Log("Pikeru's Second Sight: Ver compras do oponente."));

        // 1432 - Pinch Hopper (SS Insect from Hand)
        AddEffect("1432", c => Debug.Log("Pinch Hopper: Invoca Inseto da mão ao ir pro GY."));

        // 1433 - Pineapple Blast (Destroy if outnumbered)
        AddEffect("1433", c => Debug.Log("Pineapple Blast: Equilibra número de monstros."));

        // 1434 - Piranha Army (Double Direct Damage)
        AddEffect("1434", c => Debug.Log("Piranha Army: Dano direto dobrado."));

        // 1435 - Pitch-Black Power Stone (Spell Counters)
        AddEffect("1435", c => Debug.Log("Pitch-Black Power Stone: Gera contadores."));

        // 1436 - Pitch-Black Warwolf (No Traps Battle)
        AddEffect("1436", c => Debug.Log("Pitch-Black Warwolf: Sem Traps na Battle Phase."));

        // 1437 - Pitch-Dark Dragon (Union)
        AddEffect("1437", c => Effect_Union(c, "Dark Blade", 400, 400));
        AddEffect("1437", c => Debug.Log("Pitch-Dark Dragon: Union para Dark Blade."));

        // 1438 - Pixie Knight (Opponent Recycle Spell)
        AddEffect("1438", c => Debug.Log("Pixie Knight: Oponente recupera Magia."));

        // 1439 - Poison Draw Frog (Draw on GY)
        AddEffect("1439", c => { GameManager.Instance.DrawCard(); Debug.Log("Poison Draw Frog: Compra 1."); });

        // 1440 - Poison Fangs (Burn on Beast Damage)
        AddEffect("1440", c => Debug.Log("Poison Fangs: Dano extra por Bestas."));

        // 1441 - Poison Mummy (Flip Burn)
        AddEffect("1441", c => Effect_DirectDamage(c, 500));

        // 1442 - Poison of the Old Man (Heal/Burn)
        AddEffect("1442", c => Debug.Log("Poison of the Old Man: Cura 1200 ou Dano 800."));

        // 1443 - Pole Position (Immunity Loop)
        AddEffect("1443", c => Debug.Log("Pole Position: Maior ATK imune a magias."));

        // 1444 - Polymerization (Fusion)
        AddEffect("1444", c => Debug.Log("Polymerization: Realiza Fusão."));

        // 1445 - Possessed Dark Soul (Steal Lv3-)
        AddEffect("1445", c => Debug.Log("Possessed Dark Soul: Rouba monstros Lv3 ou menor."));

        // 1446 - Pot of Generosity (Return 2 to Deck)
        AddEffect("1446", c => Debug.Log("Pot of Generosity: Retorna 2 da mão ao deck."));

        // 1447 - Pot of Greed (Draw 2)
        AddEffect("1447", c => { 
            if (c.isPlayerCard) { GameManager.Instance.DrawCard(true); GameManager.Instance.DrawCard(true); }
            else { GameManager.Instance.DrawOpponentCard(); GameManager.Instance.DrawOpponentCard(); }
            Debug.Log("Pot of Greed: Comprou 2 cartas.");
        });

        // 1449 - Power Bond (Fusion Machine Double ATK)
        AddEffect("1449", c => Debug.Log("Power Bond: Fusão Machine com dobro de ATK e dano."));

        // 1450 - Power of Kaishin (Equip Aqua +300)
        AddEffect("1450", c => Effect_Equip(c, 300, 300, "Aqua"));

        // 1452 - Precious Cards from Beyond (Draw 2 on 2-Tribute)
        AddEffect("1452", c => Debug.Log("Precious Cards: Compra 2 ao tributar 2."));

        // 1453 - Premature Burial (Pay 800 Revive)
        AddEffect("1453", c => { Effect_PayLP(c, 800); Debug.Log("Premature Burial: Revive monstro."); });

        // 1454 - Prepare to Strike Back (Coin Toss Position)
        AddEffect("1454", c => Debug.Log("Prepare to Strike Back: Moeda para mudar posição."));

        // 1456 - Prickle Fairy (Anti-Insect / Position Change)
        AddEffect("1456", c => Debug.Log("Prickle Fairy: Impede Insetos. Vira defesa."));

        // 1457 - Primal Seed (Recycle Banished)
        AddEffect("1457", c => Debug.Log("Primal Seed: Recupera banidas se tiver BLS/CED."));

        // 1458 - Princess of Tsurugi (Flip Burn per S/T)
        AddEffect("1458", c => Debug.Log("Princess of Tsurugi: Dano por S/T do oponente."));

        // 1460 - Prohibition (Declare card)
        AddEffect("1460", c => Debug.Log("Prohibition: Proíbe carta."));

        // 1461 - Protective Soul Ailin (Union)
        AddEffect("1461", c => Debug.Log("Protective Soul Ailin: Union para Lei Lei."));

        // 1462 - Protector of the Sanctuary (No extra draws)
        AddEffect("1462", c => Debug.Log("Protector of the Sanctuary: Impede compras extras."));

        // 1468 - Pyramid Energy (Buff ATK or DEF)
        AddEffect("1468", c => Debug.Log("Pyramid Energy: +200 ATK ou +500 DEF."));

        // 1469 - Pyramid Turtle (Float Zombie)
        AddEffect("1469", c => Effect_SearchDeck(c, "Zombie"));

        // 1470 - Pyramid of Light (Banish Sphinxes)
        AddEffect("1470", c => Debug.Log("Pyramid of Light: Mantém Sphinxes."));

        // 1471 - Pyro Clock of Destiny (Turn Count)
        AddEffect("1471", c => Debug.Log("Pyro Clock: Avança contagem de turnos."));

        // 1476 - Question (Guess GY)
        AddEffect("1476", c => Debug.Log("Question: Adivinhar monstro no fundo do GY."));

        // 1478 - Rafflesia Seduction (Flip Snatch Steal)
        AddEffect("1478", c => Debug.Log("Rafflesia Seduction: Rouba monstro por 1 turno."));

        // 1479 - Raging Flame Sprite (Direct Attack, Gain ATK)
        AddEffect("1479", c => Debug.Log("Raging Flame Sprite: Ataque direto, +1000 ATK."));

        // 1480 - Raigeki (Destroy All Opp Monsters)
        AddEffect("1480", Effect_Raigeki);

        // 1481 - Raigeki Break (Discard 1 Destroy 1)
        AddEffect("1481", c => Debug.Log("Raigeki Break: Descarta 1, destrói 1."));

        // 1482 - Raimei (Burn 300)
        AddEffect("1482", c => Effect_DirectDamage(c, 300));

        // 1483 - Rain of Mercy (Heal 1000 Both)
        AddEffect("1483", c => { GameManager.Instance.playerLP += 1000; GameManager.Instance.opponentLP += 1000; Debug.Log("Rain of Mercy: Ambos curam 1000."); });

        // 1484 - Rainbow Flower (Direct Attack)
        AddEffect("1484", c => Debug.Log("Rainbow Flower: Ataque direto."));

        // 1486 - Raise Body Heat (Equip Dinosaur +300)
        AddEffect("1486", c => Effect_Equip(c, 300, 300, "Dinosaur"));

        // 1489 - Rare Metalmorph (Buff Machine)
        AddEffect("1489", c => Debug.Log("Rare Metalmorph: +500 ATK para Máquina."));

        // 1490 - Raregold Armor (Aggro)
        AddEffect("1490", c => Debug.Log("Raregold Armor: Redireciona ataques."));

        // 1492 - Ray of Hope (Recycle Light)
        AddEffect("1492", c => Debug.Log("Ray of Hope: Recicla 2 LIGHT."));

        // 1493 - Re-Fusion (Pay 800 Revive Fusion)
        AddEffect("1493", c => { Effect_PayLP(c, 800); Debug.Log("Re-Fusion: Revive Fusão."); });

        // 1494 - Ready for Intercepting (Face-down)
        AddEffect("1494", c => Debug.Log("Ready for Intercepting: Vira monstro face-down."));

        // 1495 - Really Eternal Rest (Destroy Equipped)
        AddEffect("1495", c => Debug.Log("Really Eternal Rest: Destrói monstros equipados."));

        // 1496 - Reaper of the Cards (Flip Destroy Trap)
        AddEffect("1496", c => Effect_FlipDestroy(c, TargetType.Trap));

        // 1497 - Reaper on the Nightmare (Direct Attack / Discard)
        AddEffect("1497", c => Debug.Log("Reaper on the Nightmare: Ataque direto e descarte."));

        // 1498 - Reasoning (Excavate SS)
        AddEffect("1498", c => Debug.Log("Reasoning: Escava e invoca se nível não for adivinhado."));

        // 1499 - Reckless Greed (Draw 2 Skip 2)
        AddEffect("1499", c => Debug.Log("Reckless Greed: Compra 2, pula 2 Draw Phases."));

        // 1500 - Recycle (Pay 300 Recycle)
        AddEffect("1500", c => { Effect_PayLP(c, 300); Debug.Log("Recycle: Retorna carta do GY ao deck."); });
    }
}
