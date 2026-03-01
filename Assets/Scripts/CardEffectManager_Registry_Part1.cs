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
        AddEffect("0101", c => Debug.Log("Armor Break: Negar ativação de Equip Spell."));

        // 0102 - Armor Exe (Maintenance cost)
        AddEffect("0102", c => Debug.Log("Armor Exe: Remover contador ou destruir."));

        // 0103 - Armored Glass (Trap: Negate Equips)
        AddEffect("0103", c => Debug.Log("Armored Glass: Negar efeitos de Equipamento."));

        // 0108 - Array of Revealing Light (Declare Type)
        AddEffect("0108", c => Debug.Log("Array of Revealing Light: Declarar tipo."));

        // 0109 - Arsenal Bug (Stats modification)
        AddEffect("0109", c => Debug.Log("Arsenal Bug: ATK/DEF vira 1000 se não houver outro Inseto."));

        // 0110 - Arsenal Robber (Opponent sends Equip)
        AddEffect("0110", Effect_ArsenalRobber);

        // 0111 - Arsenal Summoner (FLIP: Search Guardian)
        AddEffect("0111", c => Effect_SearchDeck(c, "Guardian"));

        // 0112 - Assault on GHQ (Destroy own -> Mill opp)
        AddEffect("0112", Effect_AssaultOnGHQ);

        // 0113 - Astral Barrier (Direct Attack redirection)
        AddEffect("0113", c => Debug.Log("Astral Barrier: Redirecionar para ataque direto."));

        // 0114 - Asura Priest (Attack all)
        AddEffect("0114", c => Debug.Log("Asura Priest: Ataca todos. Retorna para mão."));

        // 0115 - Aswan Apparition (Damage -> Trap recycling)
        AddEffect("0115", c => Debug.Log("Aswan Apparition: Reciclar Trap do GY."));

        // 0116 - Atomic Firefly (Destroyed -> Damage)
        AddEffect("0116", c => Debug.Log("Atomic Firefly: 1000 dano ao oponente."));

        // 0117 - Attack and Receive (Damage trigger)
        AddEffect("0117", Effect_AttackAndReceive);

        // 0118 - Aussa the Earth Charmer (FLIP: Control Earth)
        AddEffect("0118", c => Debug.Log("Aussa: Controlar monstro EARTH."));

        // 0119 - Autonomous Action Unit (Pay 1500 -> SS opp GY)
        AddEffect("0119", Effect_AutonomousActionUnit);

        // 0120 - Avatar of The Pot (Send Pot -> Draw 3)
        AddEffect("0120", Effect_AvatarOfThePot);

        // 0122 - Axe of Despair (Equip +1000)
        AddEffect("0122", c => Effect_Equip(c, 1000, 0));

        // 0124 - B.E.S. Big Core (Counters)
        AddEffect("0124", c => Debug.Log("B.E.S. Big Core: Contadores."));

        // 0125 - B.E.S. Crystal Core (Counters)
        AddEffect("0125", c => Debug.Log("B.E.S. Crystal Core: Contadores."));

        // 0127 - Back to Square One (Discard -> Bounce)
        AddEffect("0127", Effect_BackToSquareOne);

        // 0128 - Backfire (Fire destroyed -> Damage)
        AddEffect("0128", Effect_Backfire);

        // 0129 - Backup Soldier (Recycle Normals)
        AddEffect("0129", Effect_BackupSoldier);

        // 0130 - Bad Reaction to Simochi (Heal -> Damage)
        AddEffect("0130", c => Debug.Log("Simochi: Cura vira dano."));

        // 0131 - Bait Doll (Force activation)
        AddEffect("0131", c => Debug.Log("Bait Doll: Forçar ativação de set."));

        // 0132 - Balloon Lizard (Counters -> Damage)
        AddEffect("0132", c => Debug.Log("Balloon Lizard: Contadores e dano."));

        // 0133 - Banisher of the Light (Macro Cosmos effect)
        AddEffect("0133", c => Debug.Log("Banisher of the Light: Banir cartas enviadas ao GY."));

        // 0134 - Banner of Courage (Battle Phase Buff)
        AddEffect("0134", c => Debug.Log("Banner of Courage: +200 ATK na Battle Phase."));

        // 0135 - Bark of Dark Ruler (Pay LP -> Debuff)
        AddEffect("0135", c => Debug.Log("Bark of Dark Ruler: Pagar LP para reduzir stats."));

        // 0138 - Barrel Behind the Door (Reflect damage)
        AddEffect("0138", c => Debug.Log("Barrel Behind the Door: Refletir dano de efeito."));

        // 0139 - Barrel Dragon (Coin toss destroy)
        AddEffect("0139", Effect_BarrelDragon);

        // 0144 - Battery Charger (Pay 500 -> SS Batteryman)
        AddEffect("0144", Effect_BatteryCharger);

        // 0145 - Batteryman AA (Stats)
        AddEffect("0145", c => Debug.Log("Batteryman AA: Ganha ATK/DEF."));

        // 0146 - Batteryman C (Buff Machines)
        AddEffect("0146", c => Debug.Log("Batteryman C: Buff em Machines."));

        // 0151 - Battle-Scarred (Archfiend cost)
        AddEffect("0151", c => Debug.Log("Battle-Scarred: Oponente paga custo de Archfiend."));

        // 0152 - Bazoo the Soul-Eater (Banish -> Buff)
        AddEffect("0152", Effect_Bazoo);

        // 0155 - Beast Fangs (Equip +300/300)
        AddEffect("0155", c => Effect_Equip(c, 300, 300, "Beast"));

        // 0156 - Beast Soul Swap (Swap Beast)
        AddEffect("0156", Effect_BeastSoulSwap);

        // 0158 - Beastking of the Swamps (Fusion Sub / Search Poly)
        AddEffect("0158", c => Debug.Log("Beastking: Substituto de fusão ou buscar Poly."));

        // 0163 - Beckoning Light (Discard hand -> Retrieve Light)
        AddEffect("0163", Effect_BeckoningLight);

        // 0164 - Begone, Knave! (Damage -> Bounce)
        AddEffect("0164", c => Debug.Log("Begone, Knave!: Retornar monstro que causou dano."));

        // 0166 - Behemoth the King of All Animals (Tribute effect)
        AddEffect("0166", c => Debug.Log("Behemoth: Retornar Bestas do GY."));

        // 0167 - Berfomet (Search Gazelle)
        AddEffect("0167", c => Effect_SearchDeck(c, "Gazelle the King of Mythical Beasts"));

        // 0168 - Berserk Dragon (Multi attack)
        AddEffect("0168", c => Debug.Log("Berserk Dragon: Ataca todos."));

        // 0169 - Berserk Gorilla (Must attack)
        AddEffect("0169", c => Debug.Log("Berserk Gorilla: Destruído se defesa. Deve atacar."));

        // 0172 - Big Bang Shot (Equip +400, Piercing, Banish)
        AddEffect("0172", c => Effect_Equip(c, 400, 0));

        // 0173 - Big Burn (Banish GYs)
        AddEffect("0173", c => Debug.Log("Big Burn: Banir ambos os cemitérios."));

        // 0174 - Big Eye (FLIP: Reorder deck)
        AddEffect("0174", c => Debug.Log("Big Eye: Reordenar topo do deck."));

        // 0177 - Big Shield Gardna (Negate target, change pos)
        AddEffect("0177", c => Debug.Log("Big Shield Gardna: Nega magia e muda posição."));

        // 0178 - Big Wave Small Wave (Swap Water monsters)
        AddEffect("0178", Effect_BigWaveSmallWave);

        // 0179 - Big-Tusked Mammoth (Prevent attack)
        AddEffect("0179", c => Debug.Log("Big-Tusked Mammoth: Impede ataque no turno de invocação."));

        // 0183 - Birdface (Search Harpie)
        AddEffect("0183", c => Effect_SearchDeck(c, "Harpie Lady"));

        // 0184 - Bite Shoes (FLIP: Change pos)
        AddEffect("0184", c => Debug.Log("Bite Shoes: Mudar posição de batalha."));

        // 0185 - Black Dragon's Chick (SS Red-Eyes)
        AddEffect("0185", Effect_BlackDragonsChick);

        // 0189 - BLS - Envoy (Banish / Double Attack)
        AddEffect("0189", c => Debug.Log("BLS Envoy: Banir ou Ataque Duplo."));

        // 0191 - Black Pendant (Equip +500, Burn 500)
        AddEffect("0191", c => Effect_Equip(c, 500, 0));

        // 0193 - Black Tyranno (Direct Attack)
        AddEffect("0193", c => Debug.Log("Black Tyranno: Ataque direto se tudo defesa."));

        // 0195 - Blade Knight (Hand size buff)
        AddEffect("0195", c => Debug.Log("Blade Knight: Buff se mão vazia."));

        // 0196 - Blade Rabbit (Pos change -> Destroy)
        AddEffect("0196", c => Debug.Log("Blade Rabbit: Destruir monstro ao mudar para defesa."));

        // 0197 - Bladefly (Buff Wind)
        AddEffect("0197", c => Effect_Field(c, 500, 500, "", "WIND"));

        // 0198 - Blast Held by a Tribute (Destroy attacking tribute)
        AddEffect("0198", Effect_BlastHeldByATribute);

        // 0199 - Blast Juggler (Tribute -> Destroy weak)
        AddEffect("0199", c => Debug.Log("Blast Juggler: Destruir monstros fracos."));

        // 0200 - Blast Magician (Counters -> Destroy)
        AddEffect("0200", Effect_BlastMagician);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0201 - 0300)
        // =========================================================================================

        // 0201 - Blast Sphere (Equip to attacker, destroy & burn)
        AddEffect("0201", c => Debug.Log("Blast Sphere: Se atacado face-down, equipa no atacante e destrói na próxima Standby."));

        // 0202 - Blast with Chain (Equip +500, destroy card if destroyed)
        AddEffect("0202", c => Effect_Equip(c, 500, 0));

        // 0203 - Blasting the Ruins (30+ GY -> 3000 dmg)
        AddEffect("0203", c => {
            int gyCount = c.isPlayerCard ? GameManager.Instance.playerGraveyardDisplay.pileData.Count : GameManager.Instance.opponentGraveyardDisplay.pileData.Count;
            if (gyCount >= 30) GameManager.Instance.DamageOpponent(3000);
        });

        // 0205 - Blessings of the Nile (Gain LP on discard)
        AddEffect("0205", c => Debug.Log("Blessings of the Nile: Ganha 1000 LP quando cartas são descartadas."));

        // 0206 - Blind Destruction (Dice roll destroy)
        AddEffect("0206", c => Debug.Log("Blind Destruction: Rola dado na Standby para destruir monstros."));

        // 0207 - Blindly Loyal Goblin (Control switch immunity)
        AddEffect("0207", c => Debug.Log("Blindly Loyal Goblin: Controle não pode mudar."));

        // 0208 - Block Attack (Change to Defense)
        AddEffect("0208", Effect_BlockAttack);

        // 0210 - Blood Sucker (Mill on damage)
        AddEffect("0210", c => Debug.Log("Blood Sucker: Envia topo do deck do oponente ao GY ao causar dano."));

        // 0211 - Blowback Dragon (Coin toss destroy)
        AddEffect("0211", Effect_BlowbackDragon);

        // 0212 - Blue Medicine (Gain 400 LP)
        AddEffect("0212", c => Effect_GainLP(c, 400));

        // 0214 - Blue-Eyes Shining Dragon (SS condition, negate target)
        AddEffect("0214", c => Debug.Log("Blue-Eyes Shining Dragon: Nega efeitos que dão alvo."));

        // 0215 - Blue-Eyes Toon Dragon (Toon)
        AddEffect("0215", c => Debug.Log("Toon Dragon: Ataca direto se oponente não tiver Toon."));

        // 0219 - Boar Soldier (Destroy if Normal Summoned)
        AddEffect("0219", c => Debug.Log("Boar Soldier: Destruído se Normal Summon."));

        // 0223 - Bombardment Beetle (Flip check)
        AddEffect("0223", c => Debug.Log("Bombardment Beetle: Revela face-down do oponente."));

        // 0227 - Book of Life (SS Zombie, Banish opp monster)
        AddEffect("0227", Effect_BookOfLife);

        // 0228 - Book of Moon (Face-down Defense)
        AddEffect("0228", Effect_BookOfMoon);

        // 0229 - Book of Secret Arts (Equip +300/300 Spellcaster)
        AddEffect("0229", c => Effect_Equip(c, 300, 300, "Spellcaster"));

        // 0230 - Book of Taiyou (Face-up Attack)
        AddEffect("0230", Effect_BookOfTaiyou);

        // 0232 - Bottomless Shifting Sand (Destroy highest ATK)
        AddEffect("0232", c => Debug.Log("Bottomless Shifting Sand: Destrói monstro com maior ATK."));

        // 0233 - Bottomless Trap Hole (Destroy & Banish >= 1500)
        AddEffect("0233", Effect_BottomlessTrapHole);

        // 0235 - Bowganian (Burn 600)
        AddEffect("0235", c => Debug.Log("Bowganian: 600 dano na Standby Phase."));

        // 0237 - Brain Control (Take control)
        AddEffect("0237", Effect_BrainControl);

        // 0238 - Brain Jacker (Flip take control)
        AddEffect("0238", c => Debug.Log("Brain Jacker: Equipa e toma controle."));

        // 0240 - Breaker the Magical Warrior (Counter destroy S/T)
        AddEffect("0240", Effect_BreakerTheMagicalWarrior);

        // 0241 - Breath of Light (Destroy Rock)
        AddEffect("0241", c => Effect_DestroyType(c, "Rock"));

        // 0242 - Bubble Crash (Hand/Field limit)
        AddEffect("0242", c => Debug.Log("Bubble Crash: Envia cartas ao GY até ter 5."));

        // 0243 - Bubble Shuffle (Change pos, SS HERO)
        AddEffect("0243", Effect_BubbleShuffle);

        // 0244 - Bubonic Vermin (Flip SS)
        AddEffect("0244", c => Effect_SearchDeck(c, "Bubonic Vermin"));

        // 0246 - Burning Algae (Opp gain LP)
        AddEffect("0246", c => { if(c.isPlayerCard) GameManager.Instance.opponentLP += 1000; else GameManager.Instance.playerLP += 1000; });

        // 0248 - Burning Land (Destroy Field, Burn)
        AddEffect("0248", c => Debug.Log("Burning Land: Destrói campos e causa dano na Standby."));

        // 0249 - Burning Spear (Equip +400/-200)
        AddEffect("0249", c => Effect_Equip(c, 400, -200, "", "Fire"));

        // 0250 - Burst Breath (Tribute Dragon, destroy <= ATK)
        AddEffect("0250", c => Debug.Log("Burst Breath: Tributa Dragão para destruir monstros."));

        // 0251 - Burst Stream of Destruction (Destroy all opp monsters if BEWD)
        AddEffect("0251", Effect_BurstStream);

        // 0252 - Buster Blader (Passive buff)
        AddEffect("0252", c => Debug.Log("Buster Blader: Ganha ATK por Dragões."));

        // 0253 - Buster Rancher (Equip buff small monster)
        AddEffect("0253", c => Debug.Log("Buster Rancher: Buff massivo se ATK base <= 1000."));

        // 0254 - Butterfly Dagger - Elma (Equip +300)
        AddEffect("0254", c => Effect_Equip(c, 300, 0));

        // 0255 - Byser Shock (Return Set cards)
        AddEffect("0255", c => Debug.Log("Byser Shock: Retorna cartas setadas para a mão."));

        // 0256 - Call of Darkness (Anti-Monster Reborn)
        AddEffect("0256", c => Debug.Log("Call of Darkness: Pune Monster Reborn."));

        // 0257 - Call of the Earthbound (Redirect attack)
        AddEffect("0257", c => Debug.Log("Call of the Earthbound: Redireciona ataque."));

        // 0258 - Call of the Grave (Negate Monster Reborn)
        AddEffect("0258", c => Debug.Log("Call of the Grave: Nega Monster Reborn."));

        // 0259 - Call of the Haunted (SS from GY)
        AddEffect("0259", Effect_CallOfTheHaunted);

        // 0260 - Call of the Mummy (SS Zombie)
        AddEffect("0260", Effect_CallOfTheMummy);

        // 0262 - Cannon Soldier (Tribute burn)
        AddEffect("0262", c => Effect_TributeToBurn(c, 1, 500));

        // 0263 - Cannonball Spear Shellfish (Immunity)
        AddEffect("0263", c => Debug.Log("Cannonball Spear Shellfish: Imune a magias com Umi."));

        // 0264 - Card Destruction (Hand refresh)
        AddEffect("0264", Effect_CardDestruction);

        // 0265 - Card Shuffle (Pay 300 shuffle)
        AddEffect("0265", c => { Effect_PayLP(c, 300); Debug.Log("Deck embaralhado."); });

        // 0266 - Card of Safe Return (Draw on SS)
        AddEffect("0266", c => Debug.Log("Card of Safe Return: Compre 1 quando invocar do GY."));

        // 0267 - Card of Sanctity (Draw until 2)
        AddEffect("0267", Effect_CardOfSanctity);

        // 0268 - Castle Gate (Tribute burn)
        AddEffect("0268", c => Debug.Log("Castle Gate: Tributa para causar dano."));

        // 0269 - Castle Walls (Trap +500 DEF)
        AddEffect("0269", c => Effect_BuffStats(c, 0, 500));

        // 0270 - Castle of Dark Illusions (Flip buff Zombies)
        AddEffect("0270", c => Debug.Log("Castle of Dark Illusions: Buff em Zumbis."));

        // 0271 - Cat's Ear Tribe (Set opp ATK to 200)
        AddEffect("0271", c => Debug.Log("Cat's Ear Tribe: ATK do oponente vira 200."));

        // 0272 - Catapult Turtle (Tribute burn half ATK)
        AddEffect("0272", Effect_CatapultTurtle);

        // 0273 - Catnipped Kitty (Zero DEF)
        AddEffect("0273", c => Debug.Log("Catnipped Kitty: Torna DEF do oponente 0."));

        // 0274 - Cave Dragon (Restrictions)
        AddEffect("0274", c => Debug.Log("Cave Dragon: Restrições de invocação e ataque."));

        // 0275 - Ceasefire (Flip all, burn)
        AddEffect("0275", Effect_Ceasefire);

        // 0277 - Cemetary Bomb (Burn per GY card)
        AddEffect("0277", Effect_CemetaryBomb);

        // 0278 - Centrifugal Field (Fusion recovery)
        AddEffect("0278", c => Debug.Log("Centrifugal Field: Recupera material de fusão."));

        // 0279 - Ceremonial Bell (Reveal hands)
        AddEffect("0279", c => Debug.Log("Ceremonial Bell: Mãos reveladas."));

        // 0280 - Cestus of Dagla (Equip +500 Fairy)
        AddEffect("0280", c => Effect_Equip(c, 500, 0, "Fairy"));

        // 0281 - Chain Burst (Burn on Trap)
        AddEffect("0281", c => Debug.Log("Chain Burst: Dano ao ativar armadilha."));

        // 0282 - Chain Destruction (Destroy copies)
        AddEffect("0282", c => Debug.Log("Chain Destruction: Destrói cópias no deck/mão."));

        // 0283 - Chain Disappearance (Banish copies)
        AddEffect("0283", c => Debug.Log("Chain Disappearance: Bane cópias no deck/mão."));

        // 0284 - Chain Energy (Cost to play)
        AddEffect("0284", c => Debug.Log("Chain Energy: Custo de LP para jogar."));

        // 0287 - Change of Heart (Take control)
        AddEffect("0287", Effect_ChangeOfHeart);

        // 0288 - Chaos Command Magician (Negate target)
        AddEffect("0288", c => Debug.Log("Chaos Command Magician: Nega efeitos de monstro que dão alvo."));

        // 0289 - Chaos Emperor Dragon (Nuke)
        AddEffect("0289", Effect_ChaosEmperorDragon);

        // 0290 - Chaos End (Nuke monsters)
        AddEffect("0290", Effect_ChaosEnd);

        // 0291 - Chaos Greed (Draw 2)
        AddEffect("0291", c => { if(GameManager.Instance.playerRemoved.Count >= 4 && GameManager.Instance.playerGraveyard.Count == 0) { GameManager.Instance.DrawCard(); GameManager.Instance.DrawCard(); } });

        // 0292 - Chaos Necromancer (ATK = GY * 300)
        AddEffect("0292", c => Debug.Log("Chaos Necromancer: ATK baseado no GY."));

        // 0293 - Chaos Sorcerer (Banish)
        AddEffect("0293", Effect_ChaosSorcerer);

        // 0294 - Chaosrider Gustaph (Banish spells for ATK)
        AddEffect("0294", c => Debug.Log("Chaosrider Gustaph: Bane magias para ganhar ATK."));

        // 0296 - Charm of Shabti (Protect Gravekeepers)
        AddEffect("0296", c => Debug.Log("Charm of Shabti: Protege Gravekeepers."));

        // 0298 - Checkmate (Direct attack)
        AddEffect("0298", c => Debug.Log("Checkmate: Terrorking ataca direto."));

        // 0299 - Chimera the Flying Mythical Beast (SS on destroy)
        AddEffect("0299", c => Debug.Log("Chimera: Invoca material do GY."));

        // 0300 - Chiron the Mage (Destroy S/T)
        AddEffect("0300", Effect_Chiron);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0301 - 0400)
        // =========================================================================================

        // 0301 - Chopman the Desperate Outlaw (Flip: Equip Spell from GY)
        AddEffect("0301", c => Debug.Log("Chopman: Equipar Spell do GY."));

        // 0302 - Chorus of Sanctuary (Continuous Spell: +500 DEF to Defense Position)
        AddEffect("0302", c => Debug.Log("Chorus of Sanctuary: +500 DEF para monstros em defesa."));

        // 0303 - Chosen One (Spell: Hand selection game)
        AddEffect("0303", c => Debug.Log("Chosen One: Selecionar cartas da mão."));

        // 0305 - Cipher Soldier (Effect: +2000 ATK/DEF vs Warrior)
        AddEffect("0305", c => Debug.Log("Cipher Soldier: +2000 ATK/DEF contra Warrior."));

        // 0307 - Cloning (Trap: SS Clone Token)
        AddEffect("0307", c => Debug.Log("Cloning: Invocar Clone Token."));

        // 0309 - Coach Goblin (Effect: Return Normal Monster to deck to draw 1)
        AddEffect("0309", c => Debug.Log("Coach Goblin: Retornar Normal Monster para comprar 1."));

        // 0310 - Cobra Jar (Flip: SS Token)
        AddEffect("0310", c => Debug.Log("Cobra Jar: Invocar Token."));

        // 0311 - Cobraman Sakuzy (Effect: Flip face-down once per turn. When flipped face-up, look at Set S/T)
        AddEffect("0311", c => Debug.Log("Cobraman Sakuzy: Olhar S/T setadas."));

        // 0312 - Cockroach Knight (Effect: When sent to GY, return to top of Deck)
        AddEffect("0312", c => Debug.Log("Cockroach Knight: Retorna ao topo do deck."));

        // 0313 - Cocoon of Evolution (Effect: Equip to Petit Moth)
        AddEffect("0313", c => Effect_Equip(c, 0, 2000, "Insect")); // Simplificação

        // 0314 - Coffin Seller (Trap: Damage when monster sent to opp GY)
        AddEffect("0314", c => Debug.Log("Coffin Seller: 300 dano por monstro enviado ao GY."));

        // 0315 - Cold Wave (Spell: No S/T until next turn)
        AddEffect("0315", c => Debug.Log("Cold Wave: Bloqueia S/T."));

        // 0316 - Collected Power (Trap: Equip all Equips to target)
        AddEffect("0316", c => Debug.Log("Collected Power: Roubar equipamentos."));

        // 0317 - Combination Attack (Spell: Union monster attack again)
        AddEffect("0317", c => Debug.Log("Combination Attack: Ataque extra com Union."));

        // 0318 - Command Knight (Effect: Warrior +400 ATK, cannot be attacked if other monster)
        AddEffect("0318", c => Debug.Log("Command Knight: +400 ATK para Warriors."));

        // 0319 - Commencement Dance (Ritual Spell)
        AddEffect("0319", c => Debug.Log("Commencement Dance: Ritual."));

        // 0320 - Compulsory Evacuation Device (Trap: Return monster to hand)
        AddEffect("0320", Effect_CompulsoryEvacuationDevice);

        // 0321 - Confiscation (Spell: Pay 1000, discard opp hand)
        AddEffect("0321", c => { Effect_PayLP(c, 1000); Debug.Log("Confiscation: Descartar da mão do oponente."); });

        // 0322 - Conscription (Trap: Excavate top deck, SS if monster)
        AddEffect("0322", c => Debug.Log("Conscription: Roubar monstro do topo do deck."));

        // 0323 - Continuous Destruction Punch (Spell: Destroy attacker if DEF > ATK)
        AddEffect("0323", c => Debug.Log("Continuous Destruction Punch: Destruir atacante."));

        // 0324 - Contract with Exodia (Spell: SS Exodia Necross)
        AddEffect("0324", c => Debug.Log("Contract with Exodia: Invocar Exodia Necross."));

        // 0325 - Contract with the Abyss (Ritual Spell)
        AddEffect("0325", c => Debug.Log("Contract with the Abyss: Ritual DARK."));

        // 0326 - Contract with the Dark Master (Ritual Spell)
        AddEffect("0326", c => Debug.Log("Contract with the Dark Master: Ritual Zorc."));

        // 0327 - Convulsion of Nature (Spell: Turn decks upside down)
        AddEffect("0327", c => Debug.Log("Convulsion of Nature: Inverter decks."));

        // 0328 - Copycat (Effect: Copy ATK/DEF)
        AddEffect("0328", c => Debug.Log("Copycat: Copiar ATK/DEF."));

        // 0331 - Cost Down (Spell: Discard 1, Level -2)
        AddEffect("0331", c => Debug.Log("Cost Down: Reduzir níveis."));

        // 0332 - Covering Fire (Trap: Gain ATK of other monster)
        AddEffect("0332", c => Debug.Log("Covering Fire: Buff de ATK."));

        // 0333 - Crab Turtle (Ritual Monster)
        // 0334 - Crass Clown (Effect: Return monster when changed to Attack)
        AddEffect("0334", c => Debug.Log("Crass Clown: Retornar monstro."));

        // 0338 - Creature Swap (Spell: Swap monsters)
        AddEffect("0338", c => Debug.Log("Creature Swap: Trocar monstros."));

        // 0339 - Creeping Doom Manta (Effect: No Traps on Summon)
        AddEffect("0339", c => Debug.Log("Creeping Doom Manta: Sem traps na invocação."));

        // 0340 - Crimson Ninja (Flip: Destroy Trap)
        AddEffect("0340", c => Effect_FlipDestroy(c, TargetType.Trap));

        // 0341 - Crimson Sentry (Effect: Tribute to return destroyed monster)
        AddEffect("0341", c => Debug.Log("Crimson Sentry: Recuperar monstro."));

        // 0343 - Criosphinx (Effect: Discard when monster returned to hand)
        AddEffect("0343", c => Debug.Log("Criosphinx: Descarte ao retornar para mão."));

        // 0344 - Cross Counter (Trap: Double damage on defense, destroy attacker)
        AddEffect("0344", c => Debug.Log("Cross Counter: Dano dobrado e destruir."));

        // 0346 - Crush Card Virus (Trap: Destroy high ATK monsters)
        AddEffect("0346", c => Debug.Log("Crush Card Virus: Destruir monstros fortes."));
        AddEffect("0346", Effect_CrushCardVirus);

        // 0347 - Cure Mermaid (Effect: Gain LP)
        AddEffect("0347", c => Debug.Log("Cure Mermaid: Ganhar LP na Standby."));

        // 0348 - Curse of Aging (Trap: Discard 1, -500 ATK/DEF)
        AddEffect("0348", c => Debug.Log("Curse of Aging: Debuff global."));

        // 0349 - Curse of Anubis (Trap: Effect monsters to Defense, DEF 0)
        AddEffect("0349", c => Debug.Log("Curse of Anubis: Defesa e DEF 0."));

        // 0350 - Curse of Darkness (Trap: Damage on Spell activation)
        AddEffect("0350", c => Debug.Log("Curse of Darkness: Dano por magia."));

        // 0352 - Curse of Fiend (Spell: Change positions)
        AddEffect("0352", c => Debug.Log("Curse of Fiend: Mudar posições."));

        // 0353 - Curse of Royal (Trap: Negate S/T destruction)
        AddEffect("0353", c => Debug.Log("Curse of Royal: Negar destruição de S/T."));

        // 0354 - Curse of the Masked Beast (Ritual Spell)
        AddEffect("0354", c => Debug.Log("Curse of the Masked Beast: Ritual."));

        // 0355 - Cursed Seal of the Forbidden Spell (Trap: Negate Spell)
        AddEffect("0355", c => Debug.Log("Cursed Seal: Negar e banir magia."));

        // 0357 - Cyber Archfiend (Effect: Draw if hand empty)
        AddEffect("0357", c => Debug.Log("Cyber Archfiend: Comprar na Draw Phase."));

        // 0359 - Cyber Dragon (Effect: SS if opp controls monster)
        AddEffect("0359", c => Debug.Log("Cyber Dragon: Invocação Especial."));

        // 0362 - Cyber Harpie Lady (Effect: Name treated as Harpie Lady)
        AddEffect("0362", c => Debug.Log("Cyber Harpie Lady: Nome tratado como Harpie Lady."));

        // 0363 - Cyber Jar (Flip: Destroy all, draw 5, SS)
        AddEffect("0363", c => Debug.Log("Cyber Jar: Resetar campo."));

        // 0364 - Cyber Raider (Effect: Destroy/Equip Equip Card)
        AddEffect("0364", c => Debug.Log("Cyber Raider: Roubar equipamento."));

        // 0366 - Cyber Shield (Spell: Equip Harpie +500)
        AddEffect("0366", c => Effect_Equip(c, 500, 0, "Winged Beast")); // Simplificado

        // 0369 - Cyber Twin Dragon (Fusion Monster)
        AddEffect("0369", c => Debug.Log("Cyber Twin Dragon: Ataque duplo."));

        // 0370 - Cyber-Stein (Effect: Pay 5000 SS Fusion)
        AddEffect("0370", c => { Effect_PayLP(c, 5000); Debug.Log("Cyber-Stein: Invocar Fusão."); });

        // 0372 - Cybernetic Cyclopean (Effect: +1000 ATK if hand empty)
        AddEffect("0372", c => Debug.Log("Cybernetic Cyclopean: Buff se mão vazia."));

        // 0373 - Cybernetic Magician (Effect: Discard 1, ATK 2000)
        AddEffect("0373", c => Debug.Log("Cybernetic Magician: Alterar ATK."));

        // 0374 - Cyclon Laser (Spell: Equip Gradius +300, Piercing)
        AddEffect("0374", c => Effect_Equip(c, 300, 0, "Machine"));

        // 0377 - D. Tribe (Trap: Treat as Dragon)
        AddEffect("0377", c => Debug.Log("D. Tribe: Todos viram Dragão."));

        // 0378 - D.D. Assailant (Effect: Banish on destroy)
        AddEffect("0378", c => Debug.Log("D.D. Assailant: Banir atacante."));

        // 0379 - D.D. Borderline (Spell: No battle if no spells in GY)
        AddEffect("0379", c => Debug.Log("D.D. Borderline: Impedir batalha."));

        // 0380 - D.D. Crazy Beast (Effect: Banish destroyed monster)
        AddEffect("0380", c => Debug.Log("D.D. Crazy Beast: Banir monstro destruído."));

        // 0381 - D.D. Designator (Spell: Declare card, remove from hand)
        AddEffect("0381", c => Debug.Log("D.D. Designator: Banir da mão."));

        // 0382 - D.D. Dynamite (Trap: Damage per banished)
        AddEffect("0382", c => Debug.Log("D.D. Dynamite: Dano por banidas."));

        // 0383 - D.D. Scout Plane (Effect: SS if banished)
        AddEffect("0383", c => Debug.Log("D.D. Scout Plane: Retornar se banido."));

        // 0384 - D.D. Survivor (Effect: SS if banished)
        AddEffect("0384", c => Debug.Log("D.D. Survivor: Retornar se banido."));

        // 0386 - D.D. Trap Hole (Trap: Destroy/Banish Set monster)
        AddEffect("0386", c => Debug.Log("D.D. Trap Hole: Destruir e banir."));

        // 0387 - D.D. Warrior (Effect: Banish both on battle)
        AddEffect("0387", c => Debug.Log("D.D. Warrior: Banir ambos."));

        // 0388 - D.D. Warrior Lady (Effect: Banish both on battle)
        AddEffect("0388", c => Debug.Log("D.D. Warrior Lady: Banir ambos (opcional)."));

        // 0389 - D.D.M. - Different Dimension Master (Effect: Discard Spell, SS banished)
        AddEffect("0389", c => Debug.Log("D.D.M.: Invocar banido."));

        // 0390 - DNA Surgery (Trap: Change Type)
        AddEffect("0390", c => Debug.Log("DNA Surgery: Mudar tipo."));

        // 0391 - DNA Transplant (Trap: Change Attribute)
        AddEffect("0391", c => Debug.Log("DNA Transplant: Mudar atributo."));

        // 0393 - Dancing Fairy (Effect: Gain 1000 LP in Defense)
        AddEffect("0393", c => Debug.Log("Dancing Fairy: Ganhar LP."));

        // 0394 - Dangerous Machine Type-6 (Spell: Dice effect)
        AddEffect("0394", c => Debug.Log("Dangerous Machine Type-6: Efeito de dado."));

        // 0395 - Dark Artist (Effect: Halve DEF vs Light)
        AddEffect("0395", c => Debug.Log("Dark Artist: Reduzir DEF."));

        // 0397 - Dark Balter the Terrible (Fusion Monster)
        AddEffect("0397", c => Debug.Log("Dark Balter: Negar magia/efeito."));

        // 0400 - Dark Blade the Dragon Knight (Fusion Monster)
        AddEffect("0400", c => Debug.Log("Dark Blade Dragon Knight: Banir do GY."));

        // 0401 - Dark Cat with White Tail (FLIP: Bounce)
        AddEffect("0401", c => Debug.Log("Dark Cat with White Tail: Retorna monstros para a mão."));

        // 0402 - Dark Catapulter (Counters -> Destroy S/T)
        AddEffect("0402", c => Debug.Log("Dark Catapulter: Remove contadores para destruir S/T."));

        // 0404 - Dark Coffin (Destroyed -> Discard/Destroy)
        AddEffect("0404", c => Debug.Log("Dark Coffin: Oponente descarta ou destrói monstro."));

        // 0405 - Dark Core (Discard 1 -> Banish Face-up)
        AddEffect("0405", c => Debug.Log("Dark Core: Descarta 1 para banir monstro face-up."));

        // 0406 - Dark Designator (Add from Deck)
        AddEffect("0406", c => Debug.Log("Dark Designator: Adiciona carta do deck do oponente à mão dele."));

        // 0407 - Dark Driceratops (Piercing)
        AddEffect("0407", c => Debug.Log("Dark Driceratops: Dano perfurante."));

        // 0408 - Dark Dust Spirit (Spirit / Nuke face-up)
        AddEffect("0408", c => Debug.Log("Dark Dust Spirit: Destrói face-up na invocação. Retorna para mão."));

        // 0409 - Dark Elf (Attack Cost)
        AddEffect("0409", c => Debug.Log("Dark Elf: Paga 1000 LP para atacar."));

        // 0410 - Dark Energy (Equip Fiend +300)
        AddEffect("0410", c => Effect_Equip(c, 300, 300, "Fiend"));

        // 0411 - Dark Factory of Mass Production (Recycle 2 Normal)
        AddEffect("0411", c => Debug.Log("Dark Factory: Recupera 2 Monstros Normais."));

        // 0412 - Dark Flare Knight (No Battle Damage / SS Mirage Knight)
        AddEffect("0412", c => Debug.Log("Dark Flare Knight: Sem dano de batalha. Invoca Mirage Knight."));
        
        // 0414 - Dark Hole (Destroy all monsters)
        AddEffect("0414", Effect_DarkHole);

        // 0415 - Dark Jeroid (Debuff -800)
        AddEffect("0415", c => Debug.Log("Dark Jeroid: Reduz ATK de um monstro em 800."));

        // 0417 - Dark Magic Attack (Destroy S/T if DM)
        AddEffect("0417", c => Debug.Log("Dark Magic Attack: Destrói S/T do oponente se controlar Dark Magician."));

        // 0418 - Dark Magic Curtain (Pay half LP -> SS DM)
        AddEffect("0418", c => Debug.Log("Dark Magic Curtain: Paga metade LP para invocar Dark Magician."));

        // 0420 - Dark Magician Girl (Buff per DM/MoBC in GY)
        AddEffect("0420", c => Debug.Log("Dark Magician Girl: Ganha ATK por Dark Magician no GY."));

        // 0421 - Dark Magician Knight (Destroy 1 card)
        AddEffect("0421", c => Debug.Log("Dark Magician Knight: Destrói 1 carta."));

        // 0422 - Dark Magician of Chaos (Recycle Spell / Banish)
        AddEffect("0422", c => Debug.Log("DMoC: Recupera Magia. Bane monstros destruídos."));

        // 0423 - Dark Master - Zorc (Dice destroy)
        AddEffect("0423", c => Debug.Log("Dark Master - Zorc: Rola dado para destruir monstros."));

        // 0424 - Dark Mimic LV1 (Flip Draw / Level Up)
        AddEffect("0424", c => Debug.Log("Dark Mimic LV1: Flip compra 1. Level Up na Standby."));

        // 0425 - Dark Mimic LV3 (Draw on destroy)
        AddEffect("0425", c => Debug.Log("Dark Mimic LV3: Compra 1 (ou 2) ao ser destruído."));

        // 0426 - Dark Mirror Force (Banish Defense)
        AddEffect("0426", c => Debug.Log("Dark Mirror Force: Bane monstros em defesa."));

        // 0427 - Dark Necrofear (SS Condition / Snatch Steal)
        AddEffect("0427", c => Debug.Log("Dark Necrofear: Controla monstro do oponente ao ser destruído."));

        // 0428 - Dark Paladin (Negate Spell / Buff)
        AddEffect("0428", c => Debug.Log("Dark Paladin: Nega magia. Ganha ATK por Dragões."));

        // 0432 - Dark Room of Nightmare (Burn bonus)
        AddEffect("0432", c => Debug.Log("Dark Room of Nightmare: Causa 300 dano extra."));

        // 0433 - Dark Ruler Ha Des (Negate effects of destroyed)
        AddEffect("0433", c => Debug.Log("Dark Ruler Ha Des: Nega efeitos de monstros destruídos por Fiends."));

        // 0434 - Dark Sage (Search Spell)
        AddEffect("0434", c => Debug.Log("Dark Sage: Busca Magia no deck."));

        // 0435 - Dark Scorpion - Chick the Yellow (Bounce/TopDeck)
        AddEffect("0435", c => Debug.Log("Chick the Yellow: Efeito ao causar dano."));

        // 0436 - Dark Scorpion - Cliff the Trap Remover (Destroy S/T / Mill)
        AddEffect("0436", c => Debug.Log("Cliff the Trap Remover: Efeito ao causar dano."));

        // 0437 - Dark Scorpion - Gorg the Strong (Bounce/Mill)
        AddEffect("0437", c => Debug.Log("Gorg the Strong: Efeito ao causar dano."));

        // 0438 - Dark Scorpion - Meanae the Thorn (Search/Recycle)
        AddEffect("0438", c => Debug.Log("Meanae the Thorn: Efeito ao causar dano."));

        // 0439 - Dark Scorpion Burglars (Mill Spell)
        AddEffect("0439", c => Debug.Log("Dark Scorpion Burglars: Oponente envia Magia do deck ao GY."));

        // 0440 - Dark Scorpion Combination (Direct Attack)
        AddEffect("0440", c => Debug.Log("Dark Scorpion Combination: Ataque direto com todos."));

        // 0442 - Dark Snake Syndrome (Progressive Burn)
        AddEffect("0442", c => Debug.Log("Dark Snake Syndrome: Dano progressivo na Standby."));

        // 0443 - Dark Spirit of the Silent (Redirect Attack)
        AddEffect("0443", c => Debug.Log("Dark Spirit of the Silent: Nega ataque e obriga outro monstro a atacar."));

        // 0446 - Dark Zebra (Change to Defense)
        AddEffect("0446", c => Debug.Log("Dark Zebra: Muda para defesa se for o único."));

        // 0447 - Dark-Eyes Illusionist (Flip: Freeze)
        AddEffect("0447", c => Debug.Log("Dark-Eyes Illusionist: Impede ataque do alvo."));

        // 0448 - Dark-Piercing Light (Flip all face-up)
        AddEffect("0448", c => Debug.Log("Dark-Piercing Light: Vira todos os monstros face-down para face-up."));

        // 0449 - Darkbishop Archfiend (Protect Archfiends)
        AddEffect("0449", c => Debug.Log("Darkbishop Archfiend: Protege Archfiends de alvo."));

        // 0453 - Darklord Marie (Gain LP in GY)
        AddEffect("0453", c => Debug.Log("Darklord Marie: Ganha 200 LP na Standby se estiver no GY."));

        // 0454 - Darkness Approaches (Face-down)
        AddEffect("0454", c => Debug.Log("Darkness Approaches: Vira monstro face-down (mesmo em ataque)."));

        // 0456 - De-Fusion (Return to Extra)
        AddEffect("0456", c => Debug.Log("De-Fusion: Retorna fusão e invoca materiais."));

        // 0457 - De-Spell (Destroy Spell)
        AddEffect("0457", c => Debug.Log("De-Spell: Destrói carta de magia."));

        // 0458 - Deal of Phantom (Buff)
        AddEffect("0458", c => Debug.Log("Deal of Phantom: Buff baseado no GY."));

        // 0459 - Decayed Commander (Hand Destruction)
        AddEffect("0459", c => Debug.Log("Decayed Commander: Descarte ao atacar direto."));

        // 0460 - Deck Devastation Virus (Destroy low ATK)
        AddEffect("0460", c => Debug.Log("Deck Devastation Virus: Destrói monstros fracos na mão/deck."));

        // 0461 - Dedication through Light and Darkness (SS DMoC)
        AddEffect("0461", c => Debug.Log("Dedication: Tributa DM para invocar DMoC."));

        // 0463 - Deepsea Warrior (Immune to Spells)
        AddEffect("0463", c => Debug.Log("Deepsea Warrior: Imune a magias com Umi."));

        // 0464 - Dekoichi the Battlechanted Locomotive (Draw 1+)
        AddEffect("0464", c => Debug.Log("Dekoichi: Flip compra 1 (mais por Bokoichi)."));

        // 0465 - Delinquent Duo (Discard 2)
        AddEffect("0465", c => Debug.Log("Delinquent Duo: Paga 1000, oponente descarta 2."));

        // 0466 - Delta Attacker (Direct Attack)
        AddEffect("0466", c => Debug.Log("Delta Attacker: Ataque direto com 3 normais iguais."));

        // 0467 - Demotion (Level -2)
        AddEffect("0467", c => Debug.Log("Demotion: Reduz nível em 2."));

        // 0468 - Des Counterblow (Destroy direct attacker)
        AddEffect("0468", c => Debug.Log("Des Counterblow: Destrói quem ataca direto."));

        // 0469 - Des Croaking (Nuke)
        AddEffect("0469", c => Debug.Log("Des Croaking: Destrói tudo se tiver 3 Des Frogs."));

        // 0470 - Des Dendle (Equip/Token)
        AddEffect("0470", c => Debug.Log("Des Dendle: Union para Vampiric Orchis."));

        // 0471 - Des Feral Imp (Recycle)
        AddEffect("0471", c => Debug.Log("Des Feral Imp: Retorna carta do GY para o Deck."));

        // 0472 - Des Frog (Swarm)
        AddEffect("0472", c => Debug.Log("Des Frog: Invoca cópias baseado em T.A.D.P.O.L.E."));

        // 0473 - Des Kangaroo (Defensive Destroy)
        AddEffect("0473", c => Debug.Log("Des Kangaroo: Destrói atacante se ATK < DEF."));

        // 0474 - Des Koala (Burn)
        AddEffect("0474", c => Debug.Log("Des Koala: Dano por cartas na mão do oponente."));

        // 0475 - Des Lacooda (Draw)
        AddEffect("0475", c => Debug.Log("Des Lacooda: Flip compra 1."));

        // 0476 - Des Volstgalph (Burn/Buff)
        AddEffect("0476", c => Debug.Log("Des Volstgalph: Dano ao destruir monstro. Buff por magia."));

        // 0477 - Des Wombat (No Effect Damage)
        AddEffect("0477", c => Debug.Log("Des Wombat: Protege contra dano de efeito."));

        // 0478 - Desert Sunlight (Position Change)
        AddEffect("0478", c => Debug.Log("Desert Sunlight: Coloca monstros em defesa face-up."));

        // 0479 - Desertapir (Flip Face-down)
        AddEffect("0479", c => Debug.Log("Desertapir: Vira monstro face-down."));

        // 0480 - Despair from the Dark (SS if milled)
        AddEffect("0480", c => Debug.Log("Despair from the Dark: Invoca se enviado do deck/mão ao GY."));

        // 0481 - Desrook Archfiend (Revive Terrorking)
        AddEffect("0481", c => Debug.Log("Desrook Archfiend: Envia da mão para reviver Terrorking."));

        // 0482 - Destiny Board (Win Condition)
        AddEffect("0482", c => Debug.Log("Destiny Board: Condição de vitória em 5 turnos."));

        // 0484 - Destruction Punch (Destroy Attacker)
        AddEffect("0484", c => Debug.Log("Destruction Punch: Destrói atacante se ATK < DEF."));

        // 0485 - Destruction Ring (Burn)
        AddEffect("0485", c => Debug.Log("Destruction Ring: Destrói monstro e causa dano a ambos."));

        // 0487 - Dian Keto the Cure Master (Heal 1000)
        AddEffect("0487", c => Effect_GainLP(c, 1000));

        // 0489 - Dice Jar (Dice Burn)
        AddEffect("0489", c => Debug.Log("Dice Jar: Rola dados, perdedor toma dano massivo."));

        // 0490 - Dice Re-Roll (Reroll)
        AddEffect("0490", c => Debug.Log("Dice Re-Roll: Permite rolar dado novamente."));

        // 0491 - Different Dimension Capsule (Search Delayed)
        AddEffect("0491", c => Debug.Log("Different Dimension Capsule: Busca carta, adiciona em 2 turnos."));

        // 0492 - Different Dimension Dragon (Protection)
        AddEffect("0492", c => Debug.Log("Different Dimension Dragon: Imune a destruição por S/T que não dão alvo."));

        // 0493 - Different Dimension Gate (Banish 2)
        AddEffect("0493", c => Debug.Log("Different Dimension Gate: Bane 1 monstro de cada lado."));

        // 0494 - Diffusion Wave-Motion (Attack All)
        AddEffect("0494", c => Debug.Log("Diffusion Wave-Motion: Mago Nível 7+ ataca todos."));

        // 0496 - Dimension Distortion (Revive Banished)
        AddEffect("0496", c => Debug.Log("Dimension Distortion: Invoca monstro banido se GY vazio."));

        // 0497 - Dimension Fusion (Mass Revive)
        AddEffect("0497", c => Debug.Log("Dimension Fusion: Paga 2000, ambos invocam banidos."));

        // 0498 - Dimension Jar (Banish GY)
        AddEffect("0498", c => Debug.Log("Dimension Jar: Bane cartas do GY do oponente."));

        // 0499 - Dimension Wall (Reflect Damage)
        AddEffect("0499", c => Debug.Log("Dimension Wall: Oponente toma dano de batalha."));

        // 0500 - Dimensionhole (Blink)
        AddEffect("0500", c => Debug.Log("Dimensionhole: Remove monstro até a próxima Standby Phase."));
    }
}
