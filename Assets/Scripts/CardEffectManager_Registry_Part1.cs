using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part1()
    {
        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0001 - 0500)
        // =========================================================================================

        // 0001 - 3-Hump Lacooda (Tribute 2 to Draw 3)
        AddEffect("0001", Effect_0001_3HumpLacooda);

        // 0003 - 4-Starred Ladybug of Doom (FLIP: Destroy opponent Level 4 monsters)
        AddEffect("0003", Effect_0003_4StarredLadybugOfDoom);

        // 0004 - 7 (Spell: Gain 700 LP)
        AddEffect("0004", Effect_0004_7);

        // 0006 - 7 Completed (Equip: +700 ATK or DEF to Machine)
        AddEffect("0006", Effect_0006_7Completed);

        // 0007 - 8-Claws Scorpion (Set self)
        AddEffect("0007", Effect_0007_8ClawsScorpion);

        // 0008 - A Cat of Ill Omen (FLIP: Search Trap)
        AddEffect("0008", Effect_0008_ACatOfIllOmen);

        // 0009 - A Deal with Dark Ruler (Special Summon Berserk Dragon)
        AddEffect("0009", Effect_0009_ADealWithDarkRuler);

        // 0010 - A Feather of the Phoenix (Discard 1, Target GY, Top Deck)
        AddEffect("0010", Effect_0010_AFeatherOfThePhoenix);

        // 0011 - A Feint Plan (Trap: No attacks on face-down)
        AddEffect("0011", Effect_0011_AFeintPlan);

        // 0012 - A Hero Emerges (Trap: SS from hand when attacked)
        AddEffect("0012", Effect_0012_AHeroEmerges);

        // 0013 - A Legendary Ocean (Field: Water +200/200, Level -1)
        AddEffect("0013", Effect_0013_ALegendaryOcean);

        // 0014 - A Man with Wdjat (Reveal Set Card)
        AddEffect("0014", Effect_0014_AManWithWdjat);

        // 0015 - A Rival Appears! (Special Summon same level)
        AddEffect("0015", Effect_0015_ARivalAppears);

        // 0016 - A Wingbeat of Giant Dragon (Return Dragon, Destroy S/T)
        AddEffect("0016", Effect_0016_AWingbeatOfGiantDragon);

        // 0017 - A-Team: Trap Disposal Unit (Negate Trap)
        AddEffect("0017", Effect_0017_ATeamTrapDisposalUnit);

        // 0018 - Absolute End (Trap: Attacks become direct)
        AddEffect("0018", Effect_0018_AbsoluteEnd);

        // 0019 - Absorbing Kid from the Sky (Heal on destroy)
        AddEffect("0019", Effect_0019_AbsorbingKidFromTheSky);

        // 0021 - Abyss Soldier (Discard Water, Bounce)
        AddEffect("0021", Effect_0021_AbyssSoldier);

        // 0022 - Abyssal Designator (Pay 1000, Declare Type/Attr)
        AddEffect("0022", Effect_0022_AbyssalDesignator);

        // 0024 - Acid Rain (Destroy Machines)
        AddEffect("0024", Effect_0024_AcidRain);

        // 0025 - Acid Trap Hole (Target Set, Flip, Destroy if DEF < 2000)
        AddEffect("0025", Effect_0025_AcidTrapHole);

        // 0027 - Adhesion Trap Hole (Trap: Halve ATK)
        AddEffect("0027", Effect_0027_AdhesionTrapHole);

        // 0028 - After the Struggle (Spell: Destroy battle participants)
        AddEffect("0028", Effect_0028_AfterTheStruggle);

        // 0029 - Agido (Dice Roll SS)
        AddEffect("0029", Effect_0029_Agido);

        // 0031 - Airknight Parshath (Piercing + Draw)
        AddEffect("0031", Effect_0031_AirknightParshath);

        // 0037 - Alligator's Sword Dragon (Direct Attack condition)
        AddEffect("0037", Effect_0037_AlligatorsSwordDragon);

        // 0039 - Altar for Tribute (Tribute to Heal)
        AddEffect("0039", Effect_0039_AltarForTribute);

        // 0041 - Amazoness Archer (Tribute 2, Burn 1200)
        AddEffect("0041", Effect_0041_AmazonessArcher);

        // 0042 - Amazoness Archers (Trap: -500 ATK, must attack)
        AddEffect("0042", Effect_0042_AmazonessArchers);

        // 0043 - Amazoness Blowpiper (Effect: -500 ATK)
        AddEffect("0043", Effect_0043_AmazonessBlowpiper);

        // 0044 - Amazoness Chain Master (Effect: Steal monster from hand)
        AddEffect("0044", Effect_0044_AmazonessChainMaster);

        // 0045 - Amazoness Fighter (Effect: No battle damage)
        AddEffect("0045", Effect_0045_AmazonessFighter);

        // 0046 - Amazoness Paladin (Effect: +100 ATK per Amazoness)
        AddEffect("0046", Effect_0046_AmazonessPaladin);

        // 0047 - Amazoness Spellcaster (Swap ATK)
        AddEffect("0047", Effect_0047_AmazonessSpellcaster);

        // 0048 - Amazoness Swords Woman (Effect: Reflect damage)
        AddEffect("0048", Effect_0048_AmazonessSwordsWoman);

        // 0049 - Amazoness Tiger (Effect: +400 ATK, attack target)
        AddEffect("0049", Effect_0049_AmazonessTiger);

        // 0050 - Ameba (Burn on control switch)
        AddEffect("0050", Effect_0050_Ameba);

        // 0053 - Amphibious Bugroth MK-3 (Direct Attack with Umi)
        AddEffect("0053", Effect_0053_AmphibiousBugrothMK3);

        // 0054 - Amplifier (Equip to Jinzo)
        AddEffect("0054", Effect_0054_Amplifier);

        // 0055 - An Owl of Luck (FLIP: Field Spell to top)
        AddEffect("0055", Effect_0055_AnOwlOfLuck);

        // 0058 - Ancient Gear Beast (No S/T in battle, Negate effects)
        AddEffect("0058", Effect_0058_AncientGearBeast);

        // 0059 - Ancient Gear Golem (Piercing, No S/T in battle)
        AddEffect("0059", Effect_0059_AncientGearGolem);

        // 0060 - Ancient Gear Soldier (No S/T in battle)
        AddEffect("0060", Effect_0060_AncientGearSoldier);

        // 0062 - Ancient Lamp (SS La Jinn)
        AddEffect("0062", Effect_0062_AncientLamp);

        // 0066 - Ancient Telescope (See top 5)
        AddEffect("0066", Effect_0066_AncientTelescope);

        // 0069 - Andro Sphinx (SS cost, Burn)
        AddEffect("0069", Effect_0069_AndroSphinx);

        // 0071 - Ante (Hand reveal game)
        AddEffect("0071", Effect_0071_Ante);

        // 0073 - Anti Raigeki (Trap: Negate Raigeki)
        AddEffect("0073", Effect_0073_AntiRaigeki);

        // 0074 - Anti-Aircraft Flower (Tribute Insect -> 800 dmg)
        AddEffect("0074", Effect_0074_AntiAircraftFlower);

        // 0075 - Anti-Spell (Trap: Remove counters to negate spell)
        AddEffect("0075", Effect_0075_AntiSpell);

        // 0076 - Anti-Spell Fragrance (Trap: Must set spells)
        AddEffect("0076", Effect_0076_AntiSpellFragrance);

        // 0077 - Apprentice Magician (Spell Counter, SS on destroy)
        AddEffect("0077", Effect_0077_ApprenticeMagician);

        // 0078 - Appropriate (Trap: Draw when opp draws)
        AddEffect("0078", Effect_0078_Appropriate);

        // 0079 - Aqua Chorus (Trap: +500 ATK/DEF to same name)
        AddEffect("0079", Effect_0079_AquaChorus);

        // 0083 - Aqua Spirit (Position Change)
        AddEffect("0083", Effect_0083_AquaSpirit);

        // 0084 - Arcana Knight Joker (Negate targeting)
        AddEffect("0084", Effect_0084_ArcanaKnightJoker);

        // 0085 - Arcane Archer of the Forest (Tribute Plant -> Destroy S/T)
        AddEffect("0085", Effect_0085_ArcaneArcherOfTheForest);

        // 0089 - Archfiend of Gilfer (Equip on GY sent)
        AddEffect("0089", Effect_0089_ArchfiendOfGilfer);

        // 0090 - Archfiend's Oath (Pay 500, declare, excavate)
        AddEffect("0090", Effect_0090_ArchfiendsOath);

        // 0091 - Archfiend's Roar (Pay 500, SS Archfiend)
        AddEffect("0091", Effect_0091_ArchfiendsRoar);

        // 0092 - Archlord Zerato (Discard LIGHT -> Destroy Monsters)
        AddEffect("0092", Effect_0092_ArchlordZerato);

        // 0096 - Armed Dragon LV3 (Standby Phase Level Up)
        AddEffect("0096", Effect_0096_ArmedDragonLV3);

        // 0097 - Armed Dragon LV5 (Discard -> Destroy Monster)
        AddEffect("0097", Effect_0097_ArmedDragonLV5);

        // 0098 - Armed Dragon LV7 (Discard -> Destroy All Monsters)
        AddEffect("0098", Effect_0098_ArmedDragonLV7);

        // 0099 - Armed Ninja (FLIP: Destroy Spell)
        AddEffect("0099", Effect_0099_ArmedNinja);

        // 0100 - Armed Samurai - Ben Kei (Multi attacks)
        AddEffect("0100", Effect_0100_ArmedSamuraiBenKei);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0101 - 0200)
        // =========================================================================================

        // 0101 - Armor Break (Trap: Negate Equip)
        AddEffect("0101", Effect_0101_ArmorBreak);

        // 0102 - Armor Exe (Maintenance cost)
        AddEffect("0102", Effect_0102_ArmorExe);

        // 0103 - Armored Glass (Trap: Negate Equips)
        AddEffect("0103", Effect_0103_ArmoredGlass);

        // 0108 - Array of Revealing Light (Declare Type)
        AddEffect("0108", Effect_0108_ArrayOfRevealingLight);

        // 0109 - Arsenal Bug (Stats modification)
        AddEffect("0109", Effect_0109_ArsenalBug);

        // 0110 - Arsenal Robber (Opponent sends Equip)
        AddEffect("0110", Effect_0110_ArsenalRobber);

        // 0111 - Arsenal Summoner (FLIP: Search Guardian)
        AddEffect("0111", Effect_0111_ArsenalSummoner);

        // 0112 - Assault on GHQ (Destroy own -> Mill opp)
        AddEffect("0112", Effect_0112_AssaultOnGHQ);

        // 0113 - Astral Barrier (Direct Attack redirection)
        AddEffect("0113", Effect_0113_AstralBarrier);

        // 0114 - Asura Priest (Attack all)
        AddEffect("0114", Effect_0114_AsuraPriest);

        // 0115 - Aswan Apparition (Damage -> Trap recycling)
        AddEffect("0115", Effect_0115_AswanApparition);

        // 0116 - Atomic Firefly (Destroyed -> Damage)
        AddEffect("0116", Effect_0116_AtomicFirefly);

        // 0117 - Attack and Receive (Damage trigger)
        AddEffect("0117", Effect_0117_AttackAndReceive);

        // 0118 - Aussa the Earth Charmer (FLIP: Control Earth)
        AddEffect("0118", Effect_0118_AussaTheEarthCharmer);

        // 0119 - Autonomous Action Unit (Pay 1500 -> SS opp GY)
        AddEffect("0119", Effect_0119_AutonomousActionUnit);

        // 0120 - Avatar of The Pot (Send Pot -> Draw 3)
        AddEffect("0120", Effect_0120_AvatarOfThePot);

        // 0122 - Axe of Despair (Equip +1000)
        AddEffect("0122", Effect_0122_AxeOfDespair);

        // 0124 - B.E.S. Big Core (Counters)
        AddEffect("0124", Effect_0124_BESBigCore);

        // 0125 - B.E.S. Crystal Core (Counters)
        AddEffect("0125", Effect_0125_BESCrystalCore);

        // 0127 - Back to Square One (Discard -> Bounce)
        AddEffect("0127", Effect_0127_BackToSquareOne);

        // 0128 - Backfire (Fire destroyed -> Damage)
        AddEffect("0128", Effect_0128_Backfire);

        // 0129 - Backup Soldier (Recycle Normals)
        AddEffect("0129", Effect_0129_BackupSoldier);

        // 0130 - Bad Reaction to Simochi (Heal -> Damage)
        AddEffect("0130", Effect_0130_BadReactionToSimochi);

        // 0131 - Bait Doll (Force activation)
        AddEffect("0131", Effect_0131_BaitDoll);

        // 0132 - Balloon Lizard (Counters -> Damage)
        AddEffect("0132", Effect_0132_BalloonLizard);

        // 0133 - Banisher of the Light (Macro Cosmos effect)
        AddEffect("0133", Effect_0133_BanisherOfTheLight);

        // 0134 - Banner of Courage (Battle Phase Buff)
        AddEffect("0134", Effect_0134_BannerOfCourage);

        // 0135 - Bark of Dark Ruler (Pay LP -> Debuff)
        AddEffect("0135", Effect_0135_BarkOfDarkRuler);

        // 0138 - Barrel Behind the Door (Reflect damage)
        AddEffect("0138", Effect_0138_BarrelBehindTheDoor);

        // 0139 - Barrel Dragon (Coin toss destroy)
        AddEffect("0139", Effect_0139_BarrelDragon);

        // 0144 - Battery Charger (Pay 500 -> SS Batteryman)
        AddEffect("0144", Effect_0144_BatteryCharger);

        // 0145 - Batteryman AA (Stats)
        AddEffect("0145", Effect_0145_BatterymanAA);

        // 0146 - Batteryman C (Buff Machines)
        AddEffect("0146", Effect_0146_BatterymanC);

        // 0151 - Battle-Scarred (Archfiend cost)
        AddEffect("0151", Effect_0151_BattleScarred);

        // 0152 - Bazoo the Soul-Eater (Banish -> Buff)
        AddEffect("0152", Effect_0152_BazooTheSoulEater);

        // 0155 - Beast Fangs (Equip +300/300)
        AddEffect("0155", Effect_0155_BeastFangs);

        // 0156 - Beast Soul Swap (Swap Beast)
        AddEffect("0156", Effect_0156_BeastSoulSwap);

        // 0158 - Beastking of the Swamps (Fusion Sub / Search Poly)
        AddEffect("0158", Effect_0158_BeastkingOfTheSwamps);

        // 0163 - Beckoning Light (Discard hand -> Retrieve Light)
        AddEffect("0163", Effect_0163_BeckoningLight);

        // 0164 - Begone, Knave! (Damage -> Bounce)
        AddEffect("0164", Effect_0164_BegoneKnave);

        // 0166 - Behemoth the King of All Animals (Tribute effect)
        AddEffect("0166", Effect_0166_BehemothTheKingOfAllAnimals);

        // 0167 - Berfomet (Search Gazelle)
        AddEffect("0167", Effect_0167_Berfomet);

        // 0168 - Berserk Dragon (Multi attack)
        AddEffect("0168", Effect_0168_BerserkDragon);

        // 0169 - Berserk Gorilla (Must attack)
        AddEffect("0169", Effect_0169_BerserkGorilla);

        // 0172 - Big Bang Shot (Equip +400, Piercing, Banish)
        AddEffect("0172", Effect_0172_BigBangShot);

        // 0173 - Big Burn (Banish GYs)
        AddEffect("0173", Effect_0173_BigBurn);

        // 0174 - Big Eye (FLIP: Reorder deck)
        AddEffect("0174", Effect_0174_BigEye);

        // 0177 - Big Shield Gardna (Negate target, change pos)
        AddEffect("0177", Effect_0177_BigShieldGardna);

        // 0178 - Big Wave Small Wave (Swap Water monsters)
        AddEffect("0178", Effect_0178_BigWaveSmallWave);

        // 0179 - Big-Tusked Mammoth (Prevent attack)
        AddEffect("0179", Effect_0179_BigTuskedMammoth);

        // 0183 - Birdface (Search Harpie)
        AddEffect("0183", Effect_0183_Birdface);

        // 0184 - Bite Shoes (FLIP: Change pos)
        AddEffect("0184", Effect_0184_BiteShoes);

        // 0185 - Black Dragon's Chick (SS Red-Eyes)
        AddEffect("0185", Effect_0185_BlackDragonsChick);

        // 0186 - Black Illusion Ritual (Ritual Spell)
        AddEffect("0186", Effect_0186_BlackIllusionRitual);

        // 0187 - Black Luster Ritual (Ritual Spell)
        AddEffect("0187", Effect_0187_BlackLusterRitual);

        // 0188 - Black Luster Soldier (Ritual Monster)
        AddEffect("0188", Effect_0188_BlackLusterSoldier);

        // 0189 - BLS - Envoy (Banish / Double Attack)
        AddEffect("0189", Effect_0189_BLSEnvoy);

        // 0190 - Black Magic Ritual (Ritual Spell)
        AddEffect("0190", Effect_0190_BlackMagicRitual);

        // 0191 - Black Pendant (Equip +500, Burn 500)
        AddEffect("0191", Effect_0191_BlackPendant);

        // 0193 - Black Tyranno (Direct Attack)
        AddEffect("0193", Effect_0193_BlackTyranno);

        // 0195 - Blade Knight (Hand size buff)
        AddEffect("0195", Effect_0195_BladeKnight);

        // 0196 - Blade Rabbit (Pos change -> Destroy)
        AddEffect("0196", Effect_0196_BladeRabbit);

        // 0197 - Bladefly (Buff Wind)
        AddEffect("0197", Effect_0197_Bladefly);

        // 0198 - Blast Held by a Tribute (Destroy attacking tribute)
        AddEffect("0198", Effect_0198_BlastHeldByATribute);

        // 0199 - Blast Juggler (Tribute -> Destroy weak)
        AddEffect("0199", Effect_0199_BlastJuggler);

        // 0200 - Blast Magician (Counters -> Destroy)
        AddEffect("0200", Effect_0200_BlastMagician);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0201 - 0300)
        // =========================================================================================

        // 0201 - Blast Sphere (Equip to attacker, destroy & burn)
        AddEffect("0201", Effect_0201_BlastSphere);

        // 0202 - Blast with Chain (Equip +500, destroy card if destroyed)
        AddEffect("0202", Effect_0202_BlastWithChain);

        // 0203 - Blasting the Ruins (30+ GY -> 3000 dmg)
        AddEffect("0203", Effect_0203_BlastingTheRuins);

        // 0205 - Blessings of the Nile (Gain LP on discard)
        AddEffect("0205", Effect_0205_BlessingsOfTheNile);

        // 0206 - Blind Destruction (Dice roll destroy)
        AddEffect("0206", Effect_0206_BlindDestruction);

        // 0207 - Blindly Loyal Goblin (Control switch immunity)
        AddEffect("0207", Effect_0207_BlindlyLoyalGoblin);

        // 0208 - Block Attack (Change to Defense)
        AddEffect("0208", Effect_0208_BlockAttack);

        // 0210 - Blood Sucker (Mill on damage)
        AddEffect("0210", Effect_0210_BloodSucker);

        // 0211 - Blowback Dragon (Coin toss destroy)
        AddEffect("0211", Effect_0211_BlowbackDragon);

        // 0212 - Blue Medicine (Gain 400 LP)
        AddEffect("0212", Effect_0212_BlueMedicine);

        // 0214 - Blue-Eyes Shining Dragon (SS condition, negate target)
        AddEffect("0214", Effect_0214_BlueEyesShiningDragon);

        // 0215 - Blue-Eyes Toon Dragon (Toon)
        AddEffect("0215", Effect_0215_BlueEyesToonDragon);

        // 0219 - Boar Soldier (Destroy if Normal Summoned)
        AddEffect("0219", Effect_0219_BoarSoldier);

        // 0223 - Bombardment Beetle (Flip check)
        AddEffect("0223", Effect_0223_BombardmentBeetle);

        // 0227 - Book of Life (SS Zombie, Banish opp monster)
        AddEffect("0227", Effect_0227_BookOfLife);

        // 0228 - Book of Moon (Face-down Defense)
        AddEffect("0228", Effect_0228_BookOfMoon);

        // 0229 - Book of Secret Arts (Equip +300/300 Spellcaster)
        AddEffect("0229", Effect_0229_BookOfSecretArts);

        // 0230 - Book of Taiyou (Face-up Attack)
        AddEffect("0230", Effect_0230_BookOfTaiyou);

        // 0232 - Bottomless Shifting Sand (Destroy highest ATK)
        AddEffect("0232", Effect_0232_BottomlessShiftingSand);

        // 0233 - Bottomless Trap Hole (Destroy & Banish >= 1500)
        AddEffect("0233", Effect_0233_BottomlessTrapHole);

        // 0235 - Bowganian (Burn 600)
        AddEffect("0235", Effect_0235_Bowganian);

        // 0237 - Brain Control (Take control)
        AddEffect("0237", Effect_0237_BrainControl);

        // 0238 - Brain Jacker (Flip take control)
        AddEffect("0238", Effect_0238_BrainJacker);

        // 0240 - Breaker the Magical Warrior (Counter destroy S/T)
        AddEffect("0240", Effect_0240_BreakerTheMagicalWarrior);

        // 0241 - Breath of Light (Destroy Rock)
        AddEffect("0241", Effect_0241_BreathOfLight);

        // 0242 - Bubble Crash (Hand/Field limit)
        AddEffect("0242", Effect_0242_BubbleCrash);

        // 0243 - Bubble Shuffle (Change pos, SS HERO)
        AddEffect("0243", Effect_0243_BubbleShuffle);

        // 0244 - Bubonic Vermin (Flip SS)
        AddEffect("0244", Effect_0244_BubonicVermin);

        // 0246 - Burning Algae (Opp gain LP)
        AddEffect("0246", Effect_0246_BurningAlgae);

        // 0248 - Burning Land (Destroy Field, Burn)
        AddEffect("0248", Effect_0248_BurningLand);

        // 0249 - Burning Spear (Equip +400/-200)
        AddEffect("0249", Effect_0249_BurningSpear);

        // 0250 - Burst Breath (Tribute Dragon, destroy <= ATK)
        AddEffect("0250", Effect_0250_BurstBreath);

        // 0251 - Burst Stream of Destruction (Destroy all opp monsters if BEWD)
        AddEffect("0251", Effect_0251_BurstStreamOfDestruction);

        // 0252 - Buster Blader (Passive buff)
        AddEffect("0252", Effect_0252_BusterBlader);

        // 0253 - Buster Rancher (Equip buff small monster)
        AddEffect("0253", Effect_0253_BusterRancher);

        // 0254 - Butterfly Dagger - Elma (Equip +300)
        AddEffect("0254", Effect_0254_ButterflyDaggerElma);

        // 0255 - Byser Shock (Return Set cards)
        AddEffect("0255", Effect_0255_ByserShock);

        // 0256 - Call of Darkness (Anti-Monster Reborn)
        AddEffect("0256", Effect_0256_CallOfDarkness);

        // 0257 - Call of the Earthbound (Redirect attack)
        AddEffect("0257", Effect_0257_CallOfTheEarthbound);

        // 0258 - Call of the Grave (Negate Monster Reborn)
        AddEffect("0258", Effect_0258_CallOfTheGrave);

        // 0259 - Call of the Haunted (SS from GY)
        AddEffect("0259", Effect_0259_CallOfTheHaunted);

        // 0260 - Call of the Mummy (SS Zombie)
        AddEffect("0260", Effect_0260_CallOfTheMummy);

        // 0262 - Cannon Soldier (Tribute burn)
        AddEffect("0262", Effect_0262_CannonSoldier);

        // 0263 - Cannonball Spear Shellfish (Immunity)
        AddEffect("0263", Effect_0263_CannonballSpearShellfish);

        // 0264 - Card Destruction (Hand refresh)
        AddEffect("0264", Effect_0264_CardDestruction);

        // 0265 - Card Shuffle (Pay 300 shuffle)
        AddEffect("0265", Effect_0265_CardShuffle);

        // 0266 - Card of Safe Return (Draw on SS)
        AddEffect("0266", Effect_0266_CardOfSafeReturn);

        // 0267 - Card of Sanctity (Draw until 2)
        AddEffect("0267", Effect_0267_CardOfSanctity);

        // 0268 - Castle Gate (Tribute burn)
        AddEffect("0268", Effect_0268_CastleGate);

        // 0269 - Castle Walls (Trap +500 DEF)
        AddEffect("0269", Effect_0269_CastleWalls);

        // 0270 - Castle of Dark Illusions (Flip buff Zombies)
        AddEffect("0270", Effect_0270_CastleOfDarkIllusions);

        // 0271 - Cat's Ear Tribe (Set opp ATK to 200)
        AddEffect("0271", Effect_0271_CatsEarTribe);

        // 0272 - Catapult Turtle (Tribute burn half ATK)
        AddEffect("0272", Effect_0272_CatapultTurtle);

        // 0273 - Catnipped Kitty (Zero DEF)
        AddEffect("0273", Effect_0273_CatnippedKitty);

        // 0274 - Cave Dragon (Restrictions)
        AddEffect("0274", Effect_0274_CaveDragon);

        // 0275 - Ceasefire (Flip all, burn)
        AddEffect("0275", Effect_0275_Ceasefire);

        // 0277 - Cemetary Bomb (Burn per GY card)
        AddEffect("0277", Effect_0277_CemetaryBomb);

        // 0278 - Centrifugal Field (Fusion recovery)
        AddEffect("0278", Effect_0278_CentrifugalField);

        // 0279 - Ceremonial Bell (Reveal hands)
        AddEffect("0279", Effect_0279_CeremonialBell);

        // 0280 - Cestus of Dagla (Equip +500 Fairy)
        AddEffect("0280", Effect_0280_CestusOfDagla);

        // 0281 - Chain Burst (Burn on Trap)
        AddEffect("0281", Effect_0281_ChainBurst);

        // 0282 - Chain Destruction (Destroy copies)
        AddEffect("0282", Effect_0282_ChainDestruction);

        // 0283 - Chain Disappearance (Banish copies)
        AddEffect("0283", Effect_0283_ChainDisappearance);

        // 0284 - Chain Energy (Cost to play)
        AddEffect("0284", Effect_0284_ChainEnergy);

        // 0285 - Chakra (Ritual Monster)
        AddEffect("0285", Effect_0285_Chakra);

        // 0287 - Change of Heart (Take control)
        AddEffect("0287", Effect_0287_ChangeOfHeart);

        // 0288 - Chaos Command Magician (Negate target)
        AddEffect("0288", Effect_0288_ChaosCommandMagician);

        // 0289 - Chaos Emperor Dragon (Nuke)
        AddEffect("0289", Effect_0289_ChaosEmperorDragon);

        // 0290 - Chaos End (Nuke monsters)
        AddEffect("0290", Effect_0290_ChaosEnd);

        // 0291 - Chaos Greed (Draw 2)
        AddEffect("0291", Effect_0291_ChaosGreed);

        // 0292 - Chaos Necromancer (ATK = GY * 300)
        AddEffect("0292", Effect_0292_ChaosNecromancer);

        // 0293 - Chaos Sorcerer (Banish)
        AddEffect("0293", Effect_0293_ChaosSorcerer);

        // 0294 - Chaosrider Gustaph (Banish spells for ATK)
        AddEffect("0294", Effect_0294_ChaosriderGustaph);

        // 0296 - Charm of Shabti (Protect Gravekeepers)
        AddEffect("0296", Effect_0296_CharmOfShabti);

        // 0298 - Checkmate (Direct attack)
        AddEffect("0298", Effect_0298_Checkmate);

        // 0299 - Chimera the Flying Mythical Beast (SS on destroy)
        AddEffect("0299", Effect_0299_ChimeraTheFlyingMythicalBeast);

        // 0300 - Chiron the Mage (Destroy S/T)
        AddEffect("0300", Effect_0300_ChironTheMage);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0301 - 0400)
        // =========================================================================================

        // 0301 - Chopman the Desperate Outlaw (Flip: Equip Spell from GY)
        AddEffect("0301", Effect_0301_ChopmanTheDesperateOutlaw);

        // 0302 - Chorus of Sanctuary (Continuous Spell: +500 DEF to Defense Position)
        AddEffect("0302", Effect_0302_ChorusOfSanctuary);

        // 0303 - Chosen One (Spell: Hand selection game)
        AddEffect("0303", Effect_0303_ChosenOne);

        // 0305 - Cipher Soldier (Effect: +2000 ATK/DEF vs Warrior)
        AddEffect("0305", Effect_0305_CipherSoldier);

        // 0307 - Cloning (Trap: SS Clone Token)
        AddEffect("0307", Effect_0307_Cloning);

        // 0309 - Coach Goblin (Effect: Return Normal Monster to deck to draw 1)
        AddEffect("0309", Effect_0309_CoachGoblin);

        // 0310 - Cobra Jar (Flip: SS Token)
        AddEffect("0310", Effect_0310_CobraJar);

        // 0311 - Cobraman Sakuzy (Effect: Flip face-down once per turn. When flipped face-up, look at Set S/T)
        AddEffect("0311", Effect_0311_CobramanSakuzy);

        // 0312 - Cockroach Knight (Effect: When sent to GY, return to top of Deck)
        AddEffect("0312", Effect_0312_CockroachKnight);

        // 0313 - Cocoon of Evolution (Effect: Equip to Petit Moth)
        AddEffect("0313", Effect_0313_CocoonOfEvolution);

        // 0314 - Coffin Seller (Trap: Damage when monster sent to opp GY)
        AddEffect("0314", Effect_0314_CoffinSeller);

        // 0315 - Cold Wave (Spell: No S/T until next turn)
        AddEffect("0315", Effect_0315_ColdWave);

        // 0316 - Collected Power (Trap: Equip all Equips to target)
        AddEffect("0316", Effect_0316_CollectedPower);

        // 0317 - Combination Attack (Spell: Union monster attack again)
        AddEffect("0317", Effect_0317_CombinationAttack);

        // 0318 - Command Knight (Effect: Warrior +400 ATK, cannot be attacked if other monster)
        AddEffect("0318", Effect_0318_CommandKnight);

        // 0319 - Commencement Dance (Ritual Spell)
        AddEffect("0319", Effect_0319_CommencementDance);

        // 0320 - Compulsory Evacuation Device (Trap: Return monster to hand)
        AddEffect("0320", Effect_0320_CompulsoryEvacuationDevice);

        // 0321 - Confiscation (Spell: Pay 1000, discard opp hand)
        AddEffect("0321", Effect_0321_Confiscation);

        // 0322 - Conscription (Trap: Excavate top deck, SS if monster)
        AddEffect("0322", Effect_0322_Conscription);

        // 0323 - Continuous Destruction Punch (Spell: Destroy attacker if DEF > ATK)
        AddEffect("0323", Effect_0323_ContinuousDestructionPunch);

        // 0324 - Contract with Exodia (Spell: SS Exodia Necross)
        AddEffect("0324", Effect_0324_ContractWithExodia);

        // 0325 - Contract with the Abyss (Ritual Spell)
        AddEffect("0325", Effect_0325_ContractWithTheAbyss);

        // 0326 - Contract with the Dark Master (Ritual Spell)
        AddEffect("0326", Effect_0326_ContractWithTheDarkMaster);

        // 0327 - Convulsion of Nature (Spell: Turn decks upside down)
        AddEffect("0327", Effect_0327_ConvulsionOfNature);

        // 0328 - Copycat (Effect: Copy ATK/DEF)
        AddEffect("0328", Effect_0328_Copycat);

        // 0331 - Cost Down (Spell: Discard 1, Level -2)
        AddEffect("0331", Effect_0331_CostDown);

        // 0332 - Covering Fire (Trap: Gain ATK of other monster)
        AddEffect("0332", Effect_0332_CoveringFire);

        // 0333 - Crab Turtle (Ritual Monster)
        AddEffect("0333", Effect_0333_CrabTurtle);

        // 0334 - Crass Clown (Effect: Return monster when changed to Attack)
        AddEffect("0334", Effect_0334_CrassClown);

        // 0338 - Creature Swap (Spell: Swap monsters)
        AddEffect("0338", Effect_0338_CreatureSwap);

        // 0339 - Creeping Doom Manta (Effect: No Traps on Summon)
        AddEffect("0339", Effect_0339_CreepingDoomManta);

        // 0340 - Crimson Ninja (Flip: Destroy Trap)
        AddEffect("0340", Effect_0340_CrimsonNinja);

        // 0341 - Crimson Sentry (Effect: Tribute to return destroyed monster)
        AddEffect("0341", Effect_0341_CrimsonSentry);

        // 0343 - Criosphinx (Effect: Discard when monster returned to hand)
        AddEffect("0343", Effect_0343_Criosphinx);

        // 0344 - Cross Counter (Trap: Double damage on defense, destroy attacker)
        AddEffect("0344", Effect_0344_CrossCounter);

        // 0346 - Crush Card Virus (Trap: Destroy high ATK monsters)
        AddEffect("0346", Effect_0346_CrushCardVirus);

        // 0347 - Cure Mermaid (Effect: Gain LP)
        AddEffect("0347", Effect_0347_CureMermaid);

        // 0348 - Curse of Aging (Trap: Discard 1, -500 ATK/DEF)
        AddEffect("0348", Effect_0348_CurseOfAging);

        // 0349 - Curse of Anubis (Trap: Effect monsters to Defense, DEF 0)
        AddEffect("0349", Effect_0349_CurseOfAnubis);

        // 0350 - Curse of Darkness (Trap: Damage on Spell activation)
        AddEffect("0350", Effect_0350_CurseOfDarkness);

        // 0352 - Curse of Fiend (Spell: Change positions)
        AddEffect("0352", Effect_0352_CurseOfFiend);

        // 0353 - Curse of Royal (Trap: Negate S/T destruction)
        AddEffect("0353", Effect_0353_CurseOfRoyal);

        // 0354 - Curse of the Masked Beast (Ritual Spell)
        AddEffect("0354", Effect_0354_CurseOfTheMaskedBeast);

        // 0355 - Cursed Seal of the Forbidden Spell (Trap: Negate Spell)
        AddEffect("0355", Effect_0355_CursedSealOfTheForbiddenSpell);

        // 0357 - Cyber Archfiend (Effect: Draw if hand empty)
        AddEffect("0357", Effect_0357_CyberArchfiend);

        // 0359 - Cyber Dragon (Effect: SS if opp controls monster)
        AddEffect("0359", Effect_0359_CyberDragon);

        // 0362 - Cyber Harpie Lady (Effect: Name treated as Harpie Lady)
        AddEffect("0362", Effect_0362_CyberHarpieLady);

        // 0363 - Cyber Jar (Flip: Destroy all, draw 5, SS)
        AddEffect("0363", Effect_0363_CyberJar);

        // 0364 - Cyber Raider (Effect: Destroy/Equip Equip Card)
        AddEffect("0364", Effect_0364_CyberRaider);

        // 0366 - Cyber Shield (Spell: Equip Harpie +500)
        AddEffect("0366", Effect_0366_CyberShield);

        // 0369 - Cyber Twin Dragon (Fusion Monster)
        AddEffect("0369", Effect_0369_CyberTwinDragon);

        // 0370 - Cyber-Stein (Effect: Pay 5000 SS Fusion)
        AddEffect("0370", Effect_0370_CyberStein);

        // 0372 - Cybernetic Cyclopean (Effect: +1000 ATK if hand empty)
        AddEffect("0372", Effect_0372_CyberneticCyclopean);

        // 0373 - Cybernetic Magician (Effect: Discard 1, ATK 2000)
        AddEffect("0373", Effect_0373_CyberneticMagician);

        // 0374 - Cyclon Laser (Spell: Equip Gradius +300, Piercing)
        AddEffect("0374", Effect_0374_CyclonLaser);

        // 0377 - D. Tribe (Trap: Treat as Dragon)
        AddEffect("0377", Effect_0377_DTribe);

        // 0378 - D.D. Assailant (Effect: Banish on destroy)
        AddEffect("0378", Effect_0378_DDAssailant);

        // 0379 - D.D. Borderline (Spell: No battle if no spells in GY)
        AddEffect("0379", Effect_0379_DDBorderline);

        // 0380 - D.D. Crazy Beast (Effect: Banish destroyed monster)
        AddEffect("0380", Effect_0380_DDCrazyBeast);

        // 0381 - D.D. Designator (Spell: Declare card, remove from hand)
        AddEffect("0381", Effect_0381_DDDesignator);

        // 0382 - D.D. Dynamite (Trap: Damage per banished)
        AddEffect("0382", Effect_0382_DDDynamite);

        // 0383 - D.D. Scout Plane (Effect: SS if banished)
        AddEffect("0383", Effect_0383_DDScoutPlane);

        // 0384 - D.D. Survivor (Effect: SS if banished)
        AddEffect("0384", Effect_0384_DDSurvivor);

        // 0386 - D.D. Trap Hole (Trap: Destroy/Banish Set monster)
        AddEffect("0386", Effect_0386_DDTrapHole);

        // 0387 - D.D. Warrior (Effect: Banish both on battle)
        AddEffect("0387", Effect_0387_DDWarrior);

        // 0388 - D.D. Warrior Lady (Effect: Banish both on battle)
        AddEffect("0388", Effect_0388_DDWarriorLady);

        // 0389 - D.D.M. - Different Dimension Master (Effect: Discard Spell, SS banished)
        AddEffect("0389", Effect_0389_DDM);

        // 0390 - DNA Surgery (Trap: Change Type)
        AddEffect("0390", Effect_0390_DNASurgery);

        // 0391 - DNA Transplant (Trap: Change Attribute)
        AddEffect("0391", Effect_0391_DNATransplant);

        // 0393 - Dancing Fairy (Effect: Gain 1000 LP in Defense)
        AddEffect("0393", Effect_0393_DancingFairy);

        // 0394 - Dangerous Machine Type-6 (Spell: Dice effect)
        AddEffect("0394", Effect_0394_DangerousMachineType6);

        // 0395 - Dark Artist (Effect: Halve DEF vs Light)
        AddEffect("0395", Effect_0395_DarkArtist);

        // 0397 - Dark Balter the Terrible (Fusion Monster)
        AddEffect("0397", Effect_0397_DarkBalterTheTerrible);

        // 0400 - Dark Blade the Dragon Knight (Fusion Monster)
        AddEffect("0400", Effect_0400_DarkBladeTheDragonKnight);

        // 0401 - Dark Cat with White Tail (FLIP: Bounce)
        AddEffect("0401", Effect_0401_DarkCatWithWhiteTail);

        // 0402 - Dark Catapulter (Counters -> Destroy S/T)
        AddEffect("0402", Effect_0402_DarkCatapulter);

        // 0404 - Dark Coffin (Destroyed -> Discard/Destroy)
        AddEffect("0404", Effect_0404_DarkCoffin);

        // 0405 - Dark Core (Discard 1 -> Banish Face-up)
        AddEffect("0405", Effect_0405_DarkCore);

        // 0406 - Dark Designator (Add from Deck)
        AddEffect("0406", Effect_0406_DarkDesignator);

        // 0407 - Dark Driceratops (Piercing)
        AddEffect("0407", Effect_0407_DarkDriceratops);

        // 0408 - Dark Dust Spirit (Spirit / Nuke face-up)
        AddEffect("0408", Effect_0408_DarkDustSpirit);

        // 0409 - Dark Elf (Attack Cost)
        AddEffect("0409", Effect_0409_DarkElf);

        // 0410 - Dark Energy (Equip Fiend +300)
        AddEffect("0410", Effect_0410_DarkEnergy);

        // 0411 - Dark Factory of Mass Production (Recycle 2 Normal)
        AddEffect("0411", Effect_0411_DarkFactoryOfMassProduction);

        // 0412 - Dark Flare Knight (No Battle Damage / SS Mirage Knight)
        AddEffect("0412", Effect_0412_DarkFlareKnight);
        
        // 0414 - Dark Hole (Destroy all monsters)
        AddEffect("0414", Effect_0414_DarkHole);

        // 0415 - Dark Jeroid (Debuff -800)
        AddEffect("0415", Effect_0415_DarkJeroid);

        // 0417 - Dark Magic Attack (Destroy S/T if DM)
        AddEffect("0417", Effect_0417_DarkMagicAttack);

        // 0418 - Dark Magic Curtain (Pay half LP -> SS DM)
        AddEffect("0418", Effect_0418_DarkMagicCurtain);

        // 0420 - Dark Magician Girl (Buff per DM/MoBC in GY)
        AddEffect("0420", Effect_0420_DarkMagicianGirl);

        // 0421 - Dark Magician Knight (Destroy 1 card)
        AddEffect("0421", Effect_0421_DarkMagicianKnight);

        // 0422 - Dark Magician of Chaos (Recycle Spell / Banish)
        AddEffect("0422", Effect_0422_DarkMagicianOfChaos);

        // 0423 - Dark Master - Zorc (Dice destroy)
        AddEffect("0423", Effect_0423_DarkMasterZorc);

        // 0424 - Dark Mimic LV1 (Flip Draw / Level Up)
        AddEffect("0424", Effect_0424_DarkMimicLV1);

        // 0425 - Dark Mimic LV3 (Draw on destroy)
        AddEffect("0425", Effect_0425_DarkMimicLV3);

        // 0426 - Dark Mirror Force (Banish Defense)
        AddEffect("0426", Effect_0426_DarkMirrorForce);

        // 0427 - Dark Necrofear (SS Condition / Snatch Steal)
        AddEffect("0427", Effect_0427_DarkNecrofear);

        // 0428 - Dark Paladin (Negate Spell / Buff)
        AddEffect("0428", Effect_0428_DarkPaladin);

        // 0432 - Dark Room of Nightmare (Burn bonus)
        AddEffect("0432", Effect_0432_DarkRoomOfNightmare);

        // 0433 - Dark Ruler Ha Des (Negate effects of destroyed)
        AddEffect("0433", Effect_0433_DarkRulerHaDes);

        // 0434 - Dark Sage (Search Spell)
        AddEffect("0434", Effect_0434_DarkSage);

        // 0435 - Dark Scorpion - Chick the Yellow (Bounce/TopDeck)
        AddEffect("0435", Effect_0435_DarkScorpionChickTheYellow);

        // 0436 - Dark Scorpion - Cliff the Trap Remover (Destroy S/T / Mill)
        AddEffect("0436", Effect_0436_DarkScorpionCliffTheTrapRemover);

        // 0437 - Dark Scorpion - Gorg the Strong (Bounce/Mill)
        AddEffect("0437", Effect_0437_DarkScorpionGorgTheStrong);

        // 0438 - Dark Scorpion - Meanae the Thorn (Search/Recycle)
        AddEffect("0438", Effect_0438_DarkScorpionMeanaeTheThorn);

        // 0439 - Dark Scorpion Burglars (Mill Spell)
        AddEffect("0439", Effect_0439_DarkScorpionBurglars);

        // 0440 - Dark Scorpion Combination (Direct Attack)
        AddEffect("0440", Effect_0440_DarkScorpionCombination);

        // 0442 - Dark Snake Syndrome (Progressive Burn)
        AddEffect("0442", Effect_0442_DarkSnakeSyndrome);

        // 0443 - Dark Spirit of the Silent (Redirect Attack)
        AddEffect("0443", Effect_0443_DarkSpiritOfTheSilent);

        // 0446 - Dark Zebra (Change to Defense)
        AddEffect("0446", Effect_0446_DarkZebra);

        // 0447 - Dark-Eyes Illusionist (Flip: Freeze)
        AddEffect("0447", Effect_0447_DarkEyesIllusionist);

        // 0448 - Dark-Piercing Light (Flip all face-up)
        AddEffect("0448", Effect_0448_DarkPiercingLight);

        // 0449 - Darkbishop Archfiend (Protect Archfiends)
        AddEffect("0449", Effect_0449_DarkbishopArchfiend);

        // 0453 - Darklord Marie (Gain LP in GY)
        AddEffect("0453", Effect_0453_DarklordMarie);

        // 0454 - Darkness Approaches (Face-down)
        AddEffect("0454", Effect_0454_DarknessApproaches);

        // 0456 - De-Fusion (Return to Extra)
        AddEffect("0456", Effect_0456_DeFusion);

        // 0457 - De-Spell (Destroy Spell)
        AddEffect("0457", Effect_0457_DeSpell);

        // 0458 - Deal of Phantom (Buff)
        AddEffect("0458", Effect_0458_DealOfPhantom);

        // 0459 - Decayed Commander (Hand Destruction)
        AddEffect("0459", Effect_0459_DecayedCommander);

        // 0460 - Deck Devastation Virus (Destroy low ATK)
        AddEffect("0460", Effect_0460_DeckDevastationVirus);

        // 0461 - Dedication through Light and Darkness (SS DMoC)
        AddEffect("0461", Effect_0461_DedicationThroughLightAndDarkness);

        // 0463 - Deepsea Warrior (Immune to Spells)
        AddEffect("0463", Effect_0463_DeepseaWarrior);

        // 0464 - Dekoichi the Battlechanted Locomotive (Draw 1+)
        AddEffect("0464", Effect_0464_DekoichiTheBattlechantedLocomotive);

        // 0465 - Delinquent Duo (Discard 2)
        AddEffect("0465", Effect_0465_DelinquentDuo);

        // 0466 - Delta Attacker (Direct Attack)
        AddEffect("0466", Effect_0466_DeltaAttacker);

        // 0467 - Demotion (Level -2)
        AddEffect("0467", Effect_0467_Demotion);

        // 0468 - Des Counterblow (Destroy direct attacker)
        AddEffect("0468", Effect_0468_DesCounterblow);

        // 0469 - Des Croaking (Nuke)
        AddEffect("0469", Effect_0469_DesCroaking);

        // 0470 - Des Dendle (Equip/Token)
        AddEffect("0470", Effect_0470_DesDendle);

        // 0471 - Des Feral Imp (Recycle)
        AddEffect("0471", Effect_0471_DesFeralImp);

        // 0472 - Des Frog (Swarm)
        AddEffect("0472", Effect_0472_DesFrog);

        // 0473 - Des Kangaroo (Defensive Destroy)
        AddEffect("0473", Effect_0473_DesKangaroo);

        // 0474 - Des Koala (Burn)
        AddEffect("0474", Effect_0474_DesKoala);

        // 0475 - Des Lacooda (Draw)
        AddEffect("0475", Effect_0475_DesLacooda);

        // 0476 - Des Volstgalph (Burn/Buff)
        AddEffect("0476", Effect_0476_DesVolstgalph);

        // 0477 - Des Wombat (No Effect Damage)
        AddEffect("0477", Effect_0477_DesWombat);

        // 0478 - Desert Sunlight (Position Change)
        AddEffect("0478", Effect_0478_DesertSunlight);

        // 0479 - Desertapir (Flip Face-down)
        AddEffect("0479", Effect_0479_Desertapir);

        // 0480 - Despair from the Dark (SS if milled)
        AddEffect("0480", Effect_0480_DespairFromTheDark);

        // 0481 - Desrook Archfiend (Revive Terrorking)
        AddEffect("0481", Effect_0481_DesrookArchfiend);

        // 0482 - Destiny Board (Win Condition)
        AddEffect("0482", Effect_0482_DestinyBoard);

        // 0484 - Destruction Punch (Destroy Attacker)
        AddEffect("0484", Effect_0484_DestructionPunch);

        // 0485 - Destruction Ring (Burn)
        AddEffect("0485", Effect_0485_DestructionRing);

        // 0487 - Dian Keto the Cure Master (Heal 1000)
        AddEffect("0487", Effect_0487_DianKetoTheCureMaster);

        // 0489 - Dice Jar (Dice Burn)
        AddEffect("0489", Effect_0489_DiceJar);

        // 0490 - Dice Re-Roll (Reroll)
        AddEffect("0490", Effect_0490_DiceReRoll);

        // 0491 - Different Dimension Capsule (Search Delayed)
        AddEffect("0491", Effect_0491_DifferentDimensionCapsule);

        // 0492 - Different Dimension Dragon (Protection)
        AddEffect("0492", Effect_0492_DifferentDimensionDragon);

        // 0493 - Different Dimension Gate (Banish 2)
        AddEffect("0493", Effect_0493_DifferentDimensionGate);

        // 0494 - Diffusion Wave-Motion (Attack All)
        AddEffect("0494", Effect_0494_DiffusionWaveMotion);

        // 0496 - Dimension Distortion (Revive Banished)
        AddEffect("0496", Effect_0496_DimensionDistortion);

        // 0497 - Dimension Fusion (Mass Revive)
        AddEffect("0497", Effect_0497_DimensionFusion);

        // 0498 - Dimension Jar (Banish GY)
        AddEffect("0498", Effect_0498_DimensionJar);

        // 0499 - Dimension Wall (Reflect Damage)
        AddEffect("0499", Effect_0499_DimensionWall);

        // 0500 - Dimensionhole (Blink)
        AddEffect("0500", Effect_0500_Dimensionhole);
    }
}
