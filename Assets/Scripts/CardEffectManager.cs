using UnityEngine;
using System.Collections.Generic;

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;
    
    public enum TargetType { Monster, Spell, Trap, Any }

    // Mapeia ID da carta -> Função de efeito
    private Dictionary<string, System.Action<CardDisplay>> effectDatabase;

    void Awake()
    {
        Instance = this;
        InitializeEffects();
    }

    void InitializeEffects()
    {
        effectDatabase = new Dictionary<string, System.Action<CardDisplay>>();

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0001 - 0100) - SEM PULAR NENHUMA
        // =========================================================================================

        // 0001 - 3-Hump Lacooda (Tribute 2 to Draw 3)
        AddEffect("0001", c => Effect_TributeToDraw(c, 2, 3));

        // 0003 - 4-Starred Ladybug of Doom (FLIP: Destroy opponent Level 4 monsters)
        AddEffect("0003", c => Effect_FlipDestroyLevel(c, 4));

        // 0004 - 7 (Spell: Gain 700 LP)
        AddEffect("0004", c => Effect_GainLP(c, 700));

        // 0006 - 7 Completed (Equip: +700 ATK or DEF to Machine)
        AddEffect("0006", c => Effect_Equip(c, 700, 700, "Machine"));

        // 0007 - 8-Claws Scorpion (Set self)
        AddEffect("0007", Effect_TurnSet);

        // 0008 - A Cat of Ill Omen (FLIP: Search Trap)
        AddEffect("0008", c => Effect_SearchDeck(c, "Trap"));

        // 0009 - A Deal with Dark Ruler (Special Summon Berserk Dragon)
        AddEffect("0009", c => Debug.Log("A Deal with Dark Ruler: Requer condição de nível 8 destruído."));

        // 0010 - A Feather of the Phoenix (Discard 1, Target GY, Top Deck)
        AddEffect("0010", Effect_FeatherOfThePhoenix);

        // 0011 - A Feint Plan (Trap: No attacks on face-down)
        AddEffect("0011", c => Debug.Log("A Feint Plan: Impede ataques a monstros virados para baixo."));

        // 0012 - A Hero Emerges (Trap: SS from hand when attacked)
        AddEffect("0012", c => Debug.Log("A Hero Emerges: Oponente escolhe carta da sua mão para invocar."));

        // 0013 - A Legendary Ocean (Field: Water +200/200, Level -1)
        AddEffect("0013", c => Effect_Field(c, 200, 200, "Aqua", "", -1));

        // 0014 - A Man with Wdjat (Reveal Set Card)
        AddEffect("0014", Effect_RevealSetCard);

        // 0015 - A Rival Appears! (Special Summon same level)
        AddEffect("0015", Effect_ARivalAppears);

        // 0016 - A Wingbeat of Giant Dragon (Return Dragon, Destroy S/T)
        AddEffect("0016", Effect_WingbeatOfGiantDragon);

        // 0017 - A-Team: Trap Disposal Unit (Negate Trap)
        AddEffect("0017", c => Debug.Log("A-Team: Efeito Rápido de negar armadilha."));

        // 0018 - Absolute End (Trap: Attacks become direct)
        AddEffect("0018", c => Debug.Log("Absolute End: Ataques se tornam diretos."));

        // 0019 - Absorbing Kid from the Sky (Heal on destroy)
        AddEffect("0019", c => Debug.Log("Absorbing Kid: Ganha LP igual ao nível do monstro destruído x 300."));

        // 0021 - Abyss Soldier (Discard Water, Bounce)
        AddEffect("0021", Effect_AbyssSoldier);

        // 0022 - Abyssal Designator (Pay 1000, Declare Type/Attr)
        AddEffect("0022", c => Effect_PayLP(c, 1000));

        // 0024 - Acid Rain (Destroy Machines)
        AddEffect("0024", c => Effect_DestroyType(c, "Machine"));

        // 0025 - Acid Trap Hole (Target Set, Flip, Destroy if DEF < 2000)
        AddEffect("0025", Effect_AcidTrapHole);

        // 0027 - Adhesion Trap Hole (Trap: Halve ATK)
        AddEffect("0027", c => Debug.Log("Adhesion Trap Hole: Corta ATK do monstro invocado pela metade."));

        // 0028 - After the Struggle (Spell: Destroy battle participants)
        AddEffect("0028", c => Debug.Log("After the Struggle: Destrói monstros que batalharam."));

        // 0029 - Agido (Dice Roll SS)
        AddEffect("0029", Effect_Agido);

        // 0031 - Airknight Parshath (Piercing + Draw)
        AddEffect("0031", Effect_AirknightParshath);

        // 0037 - Alligator's Sword Dragon (Direct Attack condition)
        AddEffect("0037", c => Debug.Log("Alligator's Sword Dragon: Pode atacar direto sob condições."));

        // 0039 - Altar for Tribute (Tribute to Heal)
        AddEffect("0039", Effect_AltarForTribute);

        // 0041 - Amazoness Archer (Tribute 2, Burn 1200)
        AddEffect("0041", c => Effect_TributeToBurn(c, 2, 1200));

        // 0042 - Amazoness Archers (Trap: -500 ATK, must attack)
        AddEffect("0042", c => Debug.Log("Amazoness Archers: Oponente perde 500 ATK e deve atacar."));

        // 0043 - Amazoness Blowpiper (Effect: -500 ATK)
        AddEffect("0043", c => Debug.Log("Amazoness Blowpiper: Reduz ATK de um monstro em 500."));

        // 0044 - Amazoness Chain Master (Effect: Steal monster from hand)
        AddEffect("0044", c => Debug.Log("Amazoness Chain Master: Paga 1500 LP para pegar monstro da mão do oponente."));

        // 0045 - Amazoness Fighter (Effect: No battle damage)
        AddEffect("0045", c => Debug.Log("Amazoness Fighter: Você não toma dano de batalha."));

        // 0046 - Amazoness Paladin (Effect: +100 ATK per Amazoness)
        AddEffect("0046", c => Debug.Log("Amazoness Paladin: Ganha 100 ATK por cada Amazoness."));

        // 0047 - Amazoness Spellcaster (Swap ATK)
        AddEffect("0047", Effect_AmazonessSpellcaster);

        // 0048 - Amazoness Swords Woman (Effect: Reflect damage)
        AddEffect("0048", c => Debug.Log("Amazoness Swords Woman: Oponente toma o dano de batalha."));

        // 0049 - Amazoness Tiger (Effect: +400 ATK, attack target)
        AddEffect("0049", c => Debug.Log("Amazoness Tiger: Ganha 400 ATK. Oponente só pode atacar este."));

        // 0050 - Ameba (Burn on control switch)
        AddEffect("0050", Effect_Ameba);

        // 0052 - Amphibious Bugroth (Fusion) - Sem efeito
        // 0053 - Amphibious Bugroth MK-3 (Direct Attack with Umi)
        AddEffect("0053", c => Debug.Log("MK-3: Ataca direto se Umi estiver no campo."));

        // 0054 - Amplifier (Equip to Jinzo)
        AddEffect("0054", c => Effect_Equip(c, 0, 0, "Machine")); 

        // 0055 - An Owl of Luck (FLIP: Field Spell to top)
        AddEffect("0055", c => Effect_SearchDeckTop(c, "Field", "Spell"));

        // 0058 - Ancient Gear Beast (No S/T in battle, Negate effects)
        AddEffect("0058", c => Debug.Log("Ancient Gear Beast: Nega efeitos e impede S/T na batalha."));

        // 0059 - Ancient Gear Golem (Piercing, No S/T in battle)
        AddEffect("0059", c => Debug.Log("Ancient Gear Golem: Dano Perfurante e impede S/T na batalha."));

        // 0060 - Ancient Gear Soldier (No S/T in battle)
        AddEffect("0060", c => Debug.Log("Ancient Gear Soldier: Impede S/T na batalha."));

        // 0062 - Ancient Lamp (SS La Jinn)
        AddEffect("0062", Effect_AncientLamp);

        // 0066 - Ancient Telescope (See top 5)
        AddEffect("0066", Effect_AncientTelescope);

        // 0069 - Andro Sphinx (SS cost, Burn)
        AddEffect("0069", c => Debug.Log("Andro Sphinx: Causa dano ao destruir monstros em defesa."));

        // 0071 - Ante (Hand reveal game)
        AddEffect("0071", Effect_Ante);

        // 0073 - Anti Raigeki (Trap: Negate Raigeki)
        AddEffect("0073", c => Debug.Log("Anti Raigeki: Nega Raigeki e destrói monstros do oponente."));

        // 0074 - Anti-Aircraft Flower (Tribute Insect -> 800 dmg)
        AddEffect("0074", c => Effect_TributeToBurn(c, 1, 800, "Insect"));

        // 0075 - Anti-Spell (Trap: Remove counters to negate spell)
        AddEffect("0075", c => Debug.Log("Anti-Spell: Remove 2 contadores para negar magia."));

        // 0076 - Anti-Spell Fragrance (Trap: Must set spells)
        AddEffect("0076", c => Debug.Log("Anti-Spell Fragrance: Magias devem ser baixadas antes de usar."));

        // 0077 - Apprentice Magician (Spell Counter, SS on destroy)
        AddEffect("0077", c => Debug.Log("Apprentice Magician: Coloca contador e invoca mago Lv2 ao morrer."));

        // 0078 - Appropriate (Trap: Draw when opp draws)
        AddEffect("0078", c => Debug.Log("Appropriate: Compre 2 cartas quando o oponente comprar fora da Draw Phase."));

        // 0079 - Aqua Chorus (Trap: +500 ATK/DEF to same name)
        AddEffect("0079", c => Debug.Log("Aqua Chorus: Buff para monstros com mesmo nome."));

        // 0080 - Aqua Dragon (Fusion) - Sem efeito no JSON original, mas se tiver:
        // AddEffect("0080", ...);

        // 0083 - Aqua Spirit (Position Change)
        AddEffect("0083", Effect_AquaSpirit);

        // 0084 - Arcana Knight Joker (Negate targeting)
        AddEffect("0084", c => Debug.Log("Arcana Knight Joker: Descarta para negar efeito que dá alvo."));

        // 0085 - Arcane Archer of the Forest (Tribute Plant -> Destroy S/T)
        AddEffect("0085", Effect_ArcaneArcher);

        // 0089 - Archfiend of Gilfer (Equip on GY sent)
        AddEffect("0089", c => Debug.Log("Archfiend of Gilfer: Se equipa para reduzir ATK em 500."));

        // 0090 - Archfiend's Oath (Pay 500, declare, excavate)
        AddEffect("0090", Effect_ArchfiendsOath);

        // 0091 - Archfiend's Roar (Pay 500, SS Archfiend)
        AddEffect("0091", Effect_ArchfiendsRoar);

        // 0092 - Archlord Zerato (Discard LIGHT -> Destroy Monsters)
        AddEffect("0092", Effect_ArchlordZerato);

        // 0096 - Armed Dragon LV3 (Standby Phase Level Up)
        AddEffect("0096", c => Effect_LevelUp(c, "0097")); // ID do LV5

        // 0097 - Armed Dragon LV5 (Discard -> Destroy Monster)
        AddEffect("0097", Effect_ArmedDragonLV5);

        // 0098 - Armed Dragon LV7 (Discard -> Destroy All Monsters)
        AddEffect("0098", Effect_ArmedDragonLV7);

        // 0099 - Armed Ninja (FLIP: Destroy Spell)
        AddEffect("0099", c => Effect_FlipDestroy(c, TargetType.Spell));

        // 0100 - Armed Samurai - Ben Kei (Multi attacks)
        AddEffect("0100", c => Debug.Log("Ben Kei: Ganha 1 ataque extra por equipamento."));

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
        AddEffect("0110", c => Debug.Log("Arsenal Robber: Oponente envia Equip do Deck."));

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
        AddEffect("0117", c => Debug.Log("Attack and Receive: Dano ao oponente."));

        // 0118 - Aussa the Earth Charmer (FLIP: Control Earth)
        AddEffect("0118", c => Debug.Log("Aussa: Controlar monstro EARTH."));

        // 0119 - Autonomous Action Unit (Pay 1500 -> SS opp GY)
        AddEffect("0119", Effect_AutonomousActionUnit);

        // 0120 - Avatar of The Pot (Send Pot -> Draw 3)
        AddEffect("0120", c => Debug.Log("Avatar of The Pot: Enviar Pot of Greed para comprar 3."));

        // 0122 - Axe of Despair (Equip +1000)
        AddEffect("0122", c => Effect_Equip(c, 1000, 0));

        // 0124 - B.E.S. Big Core (Counters)
        AddEffect("0124", c => Debug.Log("B.E.S. Big Core: Contadores."));

        // 0125 - B.E.S. Crystal Core (Counters)
        AddEffect("0125", c => Debug.Log("B.E.S. Crystal Core: Contadores."));

        // 0127 - Back to Square One (Discard -> Bounce)
        AddEffect("0127", Effect_BackToSquareOne);

        // 0128 - Backfire (Fire destroyed -> Damage)
        AddEffect("0128", c => Debug.Log("Backfire: 500 dano."));

        // 0129 - Backup Soldier (Recycle Normals)
        AddEffect("0129", c => Debug.Log("Backup Soldier: Recuperar monstros normais."));

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
        AddEffect("0144", c => Debug.Log("Battery Charger: SS Batteryman."));

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
        AddEffect("0156", c => Debug.Log("Beast Soul Swap: Trocar Besta."));

        // 0158 - Beastking of the Swamps (Fusion Sub / Search Poly)
        AddEffect("0158", c => Debug.Log("Beastking: Substituto de fusão ou buscar Poly."));

        // 0163 - Beckoning Light (Discard hand -> Retrieve Light)
        AddEffect("0163", c => Debug.Log("Beckoning Light: Trocar mão por Light do GY."));

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
        AddEffect("0178", c => Debug.Log("Big Wave Small Wave: Trocar monstros de água."));

        // 0179 - Big-Tusked Mammoth (Prevent attack)
        AddEffect("0179", c => Debug.Log("Big-Tusked Mammoth: Impede ataque no turno de invocação."));

        // 0183 - Birdface (Search Harpie)
        AddEffect("0183", c => Effect_SearchDeck(c, "Harpie Lady"));

        // 0184 - Bite Shoes (FLIP: Change pos)
        AddEffect("0184", c => Debug.Log("Bite Shoes: Mudar posição de batalha."));

        // 0185 - Black Dragon's Chick (SS Red-Eyes)
        AddEffect("0185", c => Debug.Log("Black Dragon's Chick: Invocar Red-Eyes da mão."));

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
        AddEffect("0198", c => Debug.Log("Blast Held by a Tribute: Destruir atacante tributado e dano."));

        // 0199 - Blast Juggler (Tribute -> Destroy weak)
        AddEffect("0199", c => Debug.Log("Blast Juggler: Destruir monstros fracos."));

        // 0200 - Blast Magician (Counters -> Destroy)
        AddEffect("0200", c => Debug.Log("Blast Magician: Remover contadores para destruir."));

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
        AddEffect("0233", c => Debug.Log("Bottomless Trap Hole: Destrói e bane invocação >= 1500 ATK."));

        // 0235 - Bowganian (Burn 600)
        AddEffect("0235", c => Debug.Log("Bowganian: 600 dano na Standby Phase."));

        // 0237 - Brain Control (Take control)
        AddEffect("0237", Effect_BrainControl);

        // 0238 - Brain Jacker (Flip take control)
        AddEffect("0238", c => Debug.Log("Brain Jacker: Equipa e toma controle."));

        // 0240 - Breaker the Magical Warrior (Counter destroy S/T)
        AddEffect("0240", c => Debug.Log("Breaker: Ganha contador. Remove para destruir S/T."));

        // 0241 - Breath of Light (Destroy Rock)
        AddEffect("0241", c => Effect_DestroyType(c, "Rock"));

        // 0242 - Bubble Crash (Hand/Field limit)
        AddEffect("0242", c => Debug.Log("Bubble Crash: Envia cartas ao GY até ter 5."));

        // 0243 - Bubble Shuffle (Change pos, SS HERO)
        AddEffect("0243", c => Debug.Log("Bubble Shuffle: Muda posição e invoca HERO."));

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
        AddEffect("0260", c => Debug.Log("Call of the Mummy: Invoca Zumbi da mão."));

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
        AddEffect("0267", c => Debug.Log("Card of Sanctity: Banir tudo, comprar até ter 2."));

        // 0268 - Castle Gate (Tribute burn)
        AddEffect("0268", c => Debug.Log("Castle Gate: Tributa para causar dano."));

        // 0269 - Castle Walls (Trap +500 DEF)
        AddEffect("0269", c => Effect_BuffStats(c, 0, 500));

        // 0270 - Castle of Dark Illusions (Flip buff Zombies)
        AddEffect("0270", c => Debug.Log("Castle of Dark Illusions: Buff em Zumbis."));

        // 0271 - Cat's Ear Tribe (Set opp ATK to 200)
        AddEffect("0271", c => Debug.Log("Cat's Ear Tribe: ATK do oponente vira 200."));

        // 0272 - Catapult Turtle (Tribute burn half ATK)
        AddEffect("0272", c => Debug.Log("Catapult Turtle: Tributa para causar metade do ATK."));

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
        AddEffect("0287", c => Effect_ChangeControl(c, true)); // true = devolve no fim do turno (TODO)

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
        AddEffect("0300", c => Debug.Log("Chiron: Descarta magia para destruir S/T."));

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

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0501 - 0600)
        // =========================================================================================

        // 0501 - Disappear (Remove from play 1 card from opponent's Graveyard)
        AddEffect("0501", c => Debug.Log("Disappear: Banir 1 carta do cemitério do oponente."));

        // 0502 - Disarmament (Destroy all Equip Cards)
        AddEffect("0502", c => Debug.Log("Disarmament: Destruir todas as cartas de Equipamento."));

        // 0503 - Disc Fighter (Destroy Defense Position monster with DEF >= 2000)
        AddEffect("0503", c => Debug.Log("Disc Fighter: Destrói monstro em defesa com DEF >= 2000 sem cálculo de dano."));

        // 0506 - Disturbance Strategy (Opponent shuffles hand, draws same number)
        AddEffect("0506", c => Debug.Log("Disturbance Strategy: Oponente embaralha mão e compra o mesmo número."));

        // 0508 - Divine Wrath (Discard 1, negate monster effect, destroy)
        AddEffect("0508", c => Debug.Log("Divine Wrath: Descarte 1 para negar efeito de monstro e destruir."));

        // 0510 - Doitsu (Union)
        AddEffect("0510", c => Debug.Log("Doitsu: Union para Soitsu."));

        // 0512 - Dokurorider (Ritual)
        AddEffect("0512", c => Debug.Log("Dokurorider: Ritual."));

        // 0515 - Don Turtle (SS Don Turtle)
        AddEffect("0515", c => Debug.Log("Don Turtle: Invocar cópias da mão."));

        // 0516 - Don Zaloog (Hand destruction / Mill)
        AddEffect("0516", c => Debug.Log("Don Zaloog: Descarte ou Mill ao causar dano."));

        // 0517 - Dora of Fate (Damage on summon)
        AddEffect("0517", c => Debug.Log("Dora of Fate: Dano ao invocar monstro de nível menor."));

        // 0519 - Doriado's Blessing (Ritual Spell)
        AddEffect("0519", c => Debug.Log("Doriado's Blessing: Ritual."));

        // 0522 - Double Attack (Discard monster -> Double attack)
        AddEffect("0522", c => Debug.Log("Double Attack: Descarte monstro para dar ataque duplo a outro de nível menor."));

        // 0523 - Double Coston (2 Tributes for DARK)
        AddEffect("0523", c => Debug.Log("Double Coston: Vale por 2 tributos para DARK."));

        // 0524 - Double Snare (Destroy Jinzo/Royal Decree)
        AddEffect("0524", c => Debug.Log("Double Snare: Destrói carta que nega Traps."));

        // 0525 - Double Spell (Discard Spell -> Use Opp Spell)
        AddEffect("0525", c => Debug.Log("Double Spell: Copiar magia do cemitério do oponente."));

        // 0526 - Dragged Down into the Grave (Hand destruction/Draw)
        AddEffect("0526", c => Debug.Log("Dragged Down: Ambos descartam e compram 1."));

        // 0527 - Dragon Capture Jar (Dragons to Defense)
        AddEffect("0527", c => Debug.Log("Dragon Capture Jar: Dragões em defesa."));

        // 0528 - Dragon Manipulator (Flip: Control Dragon)
        AddEffect("0528", c => Debug.Log("Dragon Manipulator: Controlar Dragão."));

        // 0529 - Dragon Master Knight (Fusion)
        AddEffect("0529", c => Debug.Log("Dragon Master Knight: +500 ATK por Dragão."));

        // 0530 - Dragon Piper (Flip: Destroy Jar, Dragons to Attack)
        AddEffect("0530", c => Debug.Log("Dragon Piper: Destrói Jarra, Dragões para Ataque."));

        // 0531 - Dragon Seeker (Destroy Dragon)
        AddEffect("0531", c => Debug.Log("Dragon Seeker: Destrói Dragão ao ser invocado."));

        // 0533 - Dragon Treasure (Equip Dragon +300/300)
        AddEffect("0533", c => Effect_Equip(c, 300, 300, "Dragon"));

        // 0535 - Dragon's Gunfire (Damage or Destroy)
        AddEffect("0535", c => Debug.Log("Dragon's Gunfire: 800 dano ou destruir monstro com DEF <= 800."));

        // 0536 - Dragon's Mirror (Fusion Banish)
        AddEffect("0536", c => Debug.Log("Dragon's Mirror: Fusão de Dragão banindo do GY."));

        // 0537 - Dragon's Rage (Piercing for Dragons)
        AddEffect("0537", c => Debug.Log("Dragon's Rage: Dano perfurante para Dragões."));

        // 0539 - Dragonic Attack (Equip Warrior -> Dragon +500)
        AddEffect("0539", c => Debug.Log("Dragonic Attack: Warrior vira Dragon e ganha 500."));

        // 0540 - Draining Shield (Negate attack, Gain LP)
        AddEffect("0540", c => Debug.Log("Draining Shield: Negar ataque e ganhar LP."));

        // 0541 - Dramatic Rescue (Return Amazoness, SS)
        AddEffect("0541", c => Debug.Log("Dramatic Rescue: Salvar Amazoness e invocar outro."));

        // 0542 - Dream Clown (Destroy on Defense)
        AddEffect("0542", c => Debug.Log("Dream Clown: Destruir monstro ao mudar para defesa."));

        // 0543 - Dreamsprite (Redirect attack)
        AddEffect("0543", c => Debug.Log("Dreamsprite: Redirecionar ataque."));

        // 0544 - Drill Bug (Parasite Paracide effect)
        AddEffect("0544", c => Debug.Log("Drill Bug: Colocar Parasite Paracide no deck do oponente."));

        // 0545 - Drillago (Direct attack condition)
        AddEffect("0545", c => Debug.Log("Drillago: Ataque direto se oponente tiver monstros >= 1600 ATK."));

        // 0546 - Drillroid (Destroy Defense)
        AddEffect("0546", c => Debug.Log("Drillroid: Destrói monstro em defesa antes do cálculo."));

        // 0547 - Driving Snow (Destroy S/T)
        AddEffect("0547", c => Debug.Log("Driving Snow: Destruir S/T quando Trap é destruída."));

        // 0550 - Drop Off (Discard draw)
        AddEffect("0550", c => Debug.Log("Drop Off: Oponente descarta carta comprada."));

        // 0551 - Dummy Golem (Flip: Swap control)
        AddEffect("0551", c => Debug.Log("Dummy Golem: Trocar controle com monstro do oponente."));

        // 0554 - Dust Barrier (Normal Monster immune to Spells)
        AddEffect("0554", c => Debug.Log("Dust Barrier: Monstros Normais imunes a Magia por 2 turnos."));

        // 0555 - Dust Tornado (Destroy S/T, Set)
        AddEffect("0555", c => Debug.Log("Dust Tornado: Destruir S/T e setar carta."));

        // 0556 - Eagle Eye (No Traps on Summon)
        AddEffect("0556", c => Debug.Log("Eagle Eye: Sem traps na invocação."));

        // 0557 - Earth Chant (Ritual Spell)
        AddEffect("0557", c => Debug.Log("Earth Chant: Ritual EARTH."));

        // 0559 - Earthquake (Change to Defense)
        AddEffect("0559", c => Debug.Log("Earthquake: Mudar todos face-up para defesa."));

        // 0560 - Earthshaker (Attribute destroy)
        AddEffect("0560", c => Debug.Log("Earthshaker: Destruir monstros de atributos específicos."));

        // 0561 - Eatgaboon (Destroy low ATK summon)
        AddEffect("0561", c => Debug.Log("Eatgaboon: Destruir invocação com ATK <= 500."));

        // 0562 - Ebon Magician Curran (Burn)
        AddEffect("0562", c => Debug.Log("Curran: Dano por monstros do oponente."));

        // 0563 - Ectoplasmer (Tribute to burn)
        AddEffect("0563", c => Debug.Log("Ectoplasmer: Tributar para causar dano."));

        // 0564 - Ekibyo Drakmord (Lock attack, destroy)
        AddEffect("0564", c => Debug.Log("Ekibyo Drakmord: Impede ataque, destrói após 2 turnos."));

        // 0566 - Electric Lizard (Stun Zombie attacker)
        AddEffect("0566", c => Debug.Log("Electric Lizard: Atacante não-Zumbi não ataca no próximo turno."));

        // 0567 - Electric Snake (Draw 2 on discard)
        AddEffect("0567", c => Debug.Log("Electric Snake: Compre 2 se descartado pelo oponente."));

        // 0568 - Electro-Whip (Equip Thunder +300/300)
        AddEffect("0568", c => Effect_Equip(c, 300, 300, "Thunder"));

        // 0569 - Electromagnetic Bagworm (Flip: Control Machine)
        AddEffect("0569", c => Debug.Log("Electromagnetic Bagworm: Controlar Máquina."));

        // 0570 - Elegant Egotist (SS Harpie)
        AddEffect("0570", c => Debug.Log("Elegant Egotist: Invocar Harpie Lady."));

        // 0571 - Element Doom (Attribute effects)
        AddEffect("0571", c => Debug.Log("Element Doom: Efeitos por atributo."));

        // 0572 - Element Dragon (Attribute effects)
        AddEffect("0572", c => Debug.Log("Element Dragon: Efeitos por atributo."));

        // 0573 - Element Magician (Attribute effects)
        AddEffect("0573", c => Debug.Log("Element Magician: Efeitos por atributo."));

        // 0574 - Element Saurus (Attribute effects)
        AddEffect("0574", c => Debug.Log("Element Saurus: Efeitos por atributo."));

        // 0575 - Element Soldier (Attribute effects)
        AddEffect("0575", c => Debug.Log("Element Soldier: Efeitos por atributo."));

        // 0576 - Element Valkyrie (Attribute effects)
        AddEffect("0576", c => Debug.Log("Element Valkyrie: Efeitos por atributo."));

        // 0577 - Elemental Burst (Tribute 4 -> Nuke)
        AddEffect("0577", c => Debug.Log("Elemental Burst: Destruir tudo do oponente."));

        // 0579 - Elemental HERO Bubbleman (SS, Draw 2)
        AddEffect("0579", c => Debug.Log("Bubbleman: SS se mão vazia, compra 2 se campo vazio."));

        // 0582 - Elemental HERO Flame Wingman (Burn on destroy)
        AddEffect("0582", c => Debug.Log("Flame Wingman: Dano igual ATK do monstro destruído."));

        // 0584 - Elemental HERO Thunder Giant (Discard -> Destroy)
        AddEffect("0584", c => Debug.Log("Thunder Giant: Descarte para destruir monstro."));

        // 0585 - Elemental Mistress Doriado (Ritual)
        AddEffect("0585", c => Debug.Log("Doriado: Ritual."));

        // 0586 - Elephant Statue of Blessing (Gain LP on discard)
        AddEffect("0586", c => Debug.Log("Elephant Statue of Blessing: Ganha 2000 LP se descartado pelo oponente."));

        // 0587 - Elephant Statue of Disaster (Damage on discard)
        AddEffect("0587", c => Debug.Log("Elephant Statue of Disaster: 2000 dano se descartado pelo oponente."));

        // 0588 - Elf's Light (Equip LIGHT +400/-200)
        AddEffect("0588", c => Effect_Equip(c, 400, -200, "", "Light"));

        // 0589 - Emblem of Dragon Destroyer (Search Buster Blader)
        AddEffect("0589", c => Effect_SearchDeck(c, "Buster Blader"));

        // 0590 - Embodiment of Apophis (Trap Monster)
        AddEffect("0590", c => Debug.Log("Embodiment of Apophis: Vira monstro."));

        // 0592 - Emergency Provisions (Send S/T -> Gain LP)
        AddEffect("0592", c => Debug.Log("Emergency Provisions: Envia S/T para ganhar 1000 LP cada."));

        // 0593 - Emes the Infinity (Gain ATK)
        AddEffect("0593", c => Debug.Log("Emes the Infinity: Ganha 700 ATK ao destruir monstro."));

        // 0594 - Emissary of the Afterlife (Search Normal)
        AddEffect("0594", c => Debug.Log("Emissary of the Afterlife: Busca Normal Monster Lv3 ou menor."));

        // 0595 - Emissary of the Oasis (Protect Normal)
        AddEffect("0595", c => Debug.Log("Emissary of the Oasis: Protege Normal Monster Lv3 ou menor."));

        // 0599 - Enchanted Javelin (Gain LP equal to ATK)
        AddEffect("0599", c => Debug.Log("Enchanted Javelin: Ganha LP igual ao ATK do atacante."));

        // 0600 - Enchanting Fitting Room (Pay 800, Excavate)
        AddEffect("0600", c => Debug.Log("Enchanting Fitting Room: Paga 800, escava 4, invoca Normais Lv3 ou menor."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0601 - 0700)
        // =========================================================================================

        // 0602 - Enemy Controller (Change Pos / Take Control)
        AddEffect("0602", Effect_EnemyController);

        // 0603 - Energy Drain (Buff)
        AddEffect("0603", c => Debug.Log("Energy Drain: Buff por cartas na mão do oponente."));

        // 0604 - Enervating Mist (Hand limit 5)
        AddEffect("0604", c => Debug.Log("Enervating Mist: Limite de mão 5."));

        // 0607 - Eradicating Aerosol (Destroy Insects)
        AddEffect("0607", c => Effect_DestroyType(c, "Insect"));

        // 0608 - Eria the Water Charmer (Flip: Control Water)
        AddEffect("0608", c => Debug.Log("Eria: Controlar monstro WATER."));

        // 0609 - Eternal Drought (Destroy Fish)
        AddEffect("0609", c => Effect_DestroyType(c, "Fish"));

        // 0610 - Eternal Rest (Destroy Equipped)
        AddEffect("0610", c => Debug.Log("Eternal Rest: Destruir monstros equipados."));

        // 0612 - Exchange (Swap card in hand)
        AddEffect("0612", c => Debug.Log("Exchange: Trocar cartas da mão."));

        // 0613 - Exchange of the Spirit (Swap Deck/GY)
        AddEffect("0613", c => Debug.Log("Exchange of the Spirit: Trocar Deck e GY."));

        // 0614 - Exhausting Spell (Remove Spell Counters)
        AddEffect("0614", c => Debug.Log("Exhausting Spell: Remover contadores."));

        // 0615 - Exile of the Wicked (Destroy Fiends)
        AddEffect("0615", c => Effect_DestroyType(c, "Fiend"));

        // 0616 - Exiled Force (Tribute to destroy)
        AddEffect("0616", c => Debug.Log("Exiled Force: Tributar para destruir."));

        // 0617 - Exodia Necross (SS condition)
        AddEffect("0617", c => Debug.Log("Exodia Necross: Imune e ganha ATK."));

        // 0620 - Fairy Box (Coin toss 0 ATK)
        AddEffect("0620", c => Debug.Log("Fairy Box: Moeda para zerar ATK."));

        // 0622 - Fairy Guardian (Recycle Spell)
        AddEffect("0622", c => Debug.Log("Fairy Guardian: Reciclar Magia."));

        // 0624 - Fairy Meteor Crush (Equip Piercing)
        AddEffect("0624", c => Effect_Equip(c, 0, 0));

        // 0626 - Fairy of the Spring (Recycle Equip)
        AddEffect("0626", c => Debug.Log("Fairy of the Spring: Reciclar Equip Spell."));

        // 0628 - Fairy's Hand Mirror (Redirect Spell)
        AddEffect("0628", c => Debug.Log("Fairy's Hand Mirror: Redirecionar alvo de Magia."));

        // 0631 - Fake Trap (Protect Traps)
        AddEffect("0631", c => Debug.Log("Fake Trap: Proteger armadilhas."));

        // 0632 - Falling Down (Snatch Steal Archfiend)
        AddEffect("0632", c => Debug.Log("Falling Down: Snatch Steal para Archfiends."));

        // 0633 - Familiar Knight (SS on destroy)
        AddEffect("0633", c => Debug.Log("Familiar Knight: SS da mão ao ser destruído."));

        // 0634 - Fatal Abacus (Burn on GY)
        AddEffect("0634", c => Debug.Log("Fatal Abacus: Dano por monstro enviado ao GY."));

        // 0635 - Fear from the Dark (SS on discard)
        AddEffect("0635", c => Debug.Log("Fear from the Dark: SS se descartado."));

        // 0636 - Fengsheng Mirror (Discard Spirit)
        AddEffect("0636", c => Debug.Log("Fengsheng Mirror: Olhar mão e descartar Spirit."));

        // 0637 - Fenrir (Skip Draw)
        AddEffect("0637", c => Debug.Log("Fenrir: Skip Draw Phase."));

        // 0639 - Fiber Jar (Reset Duel)
        AddEffect("0639", c => Debug.Log("Fiber Jar: Reset total do duelo."));

        // 0640 - Fiend Comedian (Banish/Mill)
        AddEffect("0640", c => Debug.Log("Fiend Comedian: Banir GY ou millar."));

        // 0648 - Fiend's Hand Mirror (Redirect Spell)
        AddEffect("0648", c => Debug.Log("Fiend's Hand Mirror: Redirecionar alvo de Magia (S/T)."));

        // 0650 - Fiend's Sanctuary (SS Token)
        AddEffect("0650", c => Debug.Log("Fiend's Sanctuary: SS Token."));

        // 0651 - Final Attack Orders (Force Attack)
        AddEffect("0651", c => Debug.Log("Final Attack Orders: Forçar ataque."));

        // 0652 - Final Countdown (Win in 20)
        AddEffect("0652", c => { Effect_PayLP(c, 2000); Debug.Log("Final Countdown: Contagem iniciada."); });

        // 0653 - Final Destiny (Discard 5 Nuke)
        AddEffect("0653", c => Debug.Log("Final Destiny: Descartar 5 para destruir tudo."));

        // 0654 - Final Flame (Burn 600)
        AddEffect("0654", c => Effect_DirectDamage(c, 600));

        // 0656 - Fire Darts (Dice Burn)
        AddEffect("0656", c => Debug.Log("Fire Darts: Dano de dados."));

        // 0659 - Fire Princess (Burn on Heal)
        AddEffect("0659", c => Debug.Log("Fire Princess: Dano ao curar."));

        // 0661 - Fire Sorcerer (Banish Hand Burn)
        AddEffect("0661", c => Debug.Log("Fire Sorcerer: Banir da mão para dano."));

        // 0662 - Firebird (Gain ATK)
        AddEffect("0662", c => Debug.Log("Firebird: Ganha ATK."));

        // 0666 - Fissure (Destroy lowest ATK)
        AddEffect("0666", c => Debug.Log("Fissure: Destruir menor ATK."));

        // 0673 - Flame Ruler (2 Tributes Fire)
        AddEffect("0673", c => Debug.Log("Flame Ruler: 2 tributos para Fire."));

        // 0676 - Flash Assailant (Debuff hand)
        AddEffect("0676", c => Debug.Log("Flash Assailant: Debuff por cartas na mão."));

        // 0677 - Flint (Lock)
        AddEffect("0677", c => Debug.Log("Flint: Travar monstro."));

        // 0680 - Flying Kamakiri #1 (Search Wind)
        AddEffect("0680", c => Effect_SearchDeck(c, "WIND"));

        // 0683 - Follow Wind (Equip +300)
        AddEffect("0683", c => Effect_Equip(c, 300, 300, "Winged Beast"));

        // 0684 - Foolish Burial (Mill)
        AddEffect("0684", c => Debug.Log("Foolish Burial: Enviar do deck ao GY."));

        // 0685 - Forced Ceasefire (No Traps)
        AddEffect("0685", c => Debug.Log("Forced Ceasefire: Impedir Traps."));

        // 0686 - Forced Requisition (Discard)
        AddEffect("0686", c => Debug.Log("Forced Requisition: Descarte forçado."));

        // 0687 - Forest (Field Buff)
        AddEffect("0687", c => Effect_Field(c, 200, 200, "Insect"));

        // 0688 - Formation Union (Union)
        AddEffect("0688", c => Debug.Log("Formation Union: Equipar/Desequipar Union."));

        // 0691 - Fox Fire (Revive)
        AddEffect("0691", c => Debug.Log("Fox Fire: Reviver."));

        // 0692 - Freed the Brave Wanderer (Banish Light Destroy)
        AddEffect("0692", c => Debug.Log("Freed: Banir 2 Light para destruir."));

        // 0693 - Freed the Matchless General (Search Warrior)
        AddEffect("0693", c => Debug.Log("Freed General: Buscar Warrior."));

        // 0694 - Freezing Beast (Union)
        AddEffect("0694", c => Debug.Log("Freezing Beast: Union."));

        // 0696 - Frontier Wiseman (Negate Target)
        AddEffect("0696", c => Debug.Log("Frontier Wiseman: Negar alvo em Warrior."));

        // 0697 - Frontline Base (SS Union)
        AddEffect("0697", c => Debug.Log("Frontline Base: SS Union."));

        // 0698 - Frozen Soul (Skip Battle)
        AddEffect("0698", c => Debug.Log("Frozen Soul: Pular Battle Phase."));

        // 0699 - Fruits of Kozaky's Studies (Reorder)
        AddEffect("0699", c => Debug.Log("Fruits of Kozaky: Reordenar deck."));

        // 0700 - Fuh-Rin-Ka-Zan (4 Elements)
        AddEffect("0700", c => Debug.Log("Fuh-Rin-Ka-Zan: Efeito poderoso se 4 elementos."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0701 - 0800)
        // =========================================================================================

        // 0701 - Fuhma Shuriken (Equip Ninja +700, Burn 700 on GY)
        AddEffect("0701", c => { Effect_Equip(c, 700, 0); Debug.Log("Fuhma Shuriken: Dano se for pro GY."); });

        // 0702 - Fulfillment of the Contract (Pay 800, Revive Ritual)
        AddEffect("0702", c => { Effect_PayLP(c, 800); Debug.Log("Fulfillment: Reviver Ritual."); });

        // 0704 - Fushi No Tori (Spirit, Heal damage)
        AddEffect("0704", c => Debug.Log("Fushi No Tori: Spirit. Cura igual ao dano."));

        // 0705 - Fushioh Richie (Flip, Negate, SS Zombie)
        AddEffect("0705", c => Debug.Log("Fushioh Richie: Flip SS Zombie."));

        // 0706 - Fusilier Dragon (NS no tribute)
        AddEffect("0706", c => Debug.Log("Fusilier Dragon: NS sem tributo (stats metade)."));

        // 0707 - Fusion Gate (Field Fusion)
        AddEffect("0707", c => Debug.Log("Fusion Gate: Fusão banindo."));

        // 0708 - Fusion Recovery (Add Poly + Material)
        AddEffect("0708", c => Debug.Log("Fusion Recovery: Recuperar Poly e Material."));

        // 0709 - Fusion Sage (Search Poly)
        AddEffect("0709", c => Effect_SearchDeck(c, "Polymerization"));

        // 0710 - Fusion Sword Murasame Blade (Equip Warrior +800)
        AddEffect("0710", c => Effect_Equip(c, 800, 0, "Warrior"));

        // 0711 - Fusion Weapon (Equip Fusion Lv6- +1500)
        AddEffect("0711", c => Debug.Log("Fusion Weapon: +1500 para Fusão Lv6-."));

        // 0715 - Gaia Power (Field Earth +500/-400)
        AddEffect("0715", c => Effect_Field(c, 500, -400, "", "Earth"));

        // 0716 - Gaia Soul (Tribute Pyro buff)
        AddEffect("0716", c => Debug.Log("Gaia Soul: Tributar Pyro para ATK."));

        // 0719 - Gale Dogra (Pay 3000 dump Extra)
        AddEffect("0719", c => { Effect_PayLP(c, 3000); Debug.Log("Gale Dogra: Enviar do Extra para GY."); });

        // 0720 - Gale Lizard (Flip Return)
        AddEffect("0720", c => Effect_FlipReturn(c, TargetType.Monster));

        // 0721 - Gamble (Coin toss)
        AddEffect("0721", c => Debug.Log("Gamble: Moeda."));

        // 0725 - Garma Sword Oath (Ritual)
        AddEffect("0725", c => Debug.Log("Garma Sword Oath: Ritual."));

        // 0728 - Garuda the Wind Spirit (SS Banish Wind)
        AddEffect("0728", c => Debug.Log("Garuda: SS banindo Wind."));

        // 0731 - Gate Guardian (SS Tributes)
        AddEffect("0731", c => Debug.Log("Gate Guardian: SS complexo."));

        // 0733 - Gather Your Mind (Search self)
        AddEffect("0733", c => Effect_SearchDeck(c, "Gather Your Mind"));

        // 0734 - Gatling Dragon (Coin destroy)
        AddEffect("0734", c => Debug.Log("Gatling Dragon: Moedas para destruir."));

        // 0736 - Gear Golem the Moving Fortress (Pay 800 Direct)
        AddEffect("0736", c => { Effect_PayLP(c, 800); Debug.Log("Gear Golem: Ataque direto."); });

        // 0737 - Gearfried the Iron Knight (Destroy Equip)
        AddEffect("0737", c => Debug.Log("Gearfried: Destrói equipamentos."));

        // 0738 - Gearfried the Swordmaster (Destroy on Equip)
        AddEffect("0738", c => Debug.Log("Gearfried Swordmaster: Destrói monstro ao equipar."));

        // 0740 - Gemini Imps (Negate discard)
        AddEffect("0740", c => Debug.Log("Gemini Imps: Nega descarte."));

        // 0742 - Germ Infection (Equip Debuff)
        AddEffect("0742", c => Debug.Log("Germ Infection: -300 ATK por turno."));

        // 0743 - Gernia (SS on destroy)
        AddEffect("0743", c => Debug.Log("Gernia: Renasce na Standby."));

        // 0744 - Getsu Fuhma (Destroy Fiend/Zombie)
        AddEffect("0744", c => Debug.Log("Getsu Fuhma: Destrói Fiend/Zombie."));

        // 0745 - Ghost Knight of Jackal (SS opp monster)
        AddEffect("0745", c => Debug.Log("Ghost Knight: Rouba monstro."));

        // 0747 - Giant Axe Mummy (Flip, Destroy weak attacker)
        AddEffect("0747", c => Debug.Log("Giant Axe Mummy: Destrói atacante fraco."));

        // 0749 - Giant Germ (Burn, SS)
        AddEffect("0749", c => { Effect_DirectDamage(c, 500); Debug.Log("Giant Germ: SS cópias."); });

        // 0750 - Giant Kozaky (Destroy if no Kozaky)
        AddEffect("0750", c => Debug.Log("Giant Kozaky: Dano se destruído."));

        // 0752 - Giant Orc (Defense after attack)
        AddEffect("0752", c => Debug.Log("Giant Orc: Vira defesa."));

        // 0753 - Giant Rat (Recycle Earth)
        AddEffect("0753", c => Debug.Log("Giant Rat: Busca Earth <= 1500."));

        // 0757 - Giant Trunade (Bounce S/T)
        AddEffect("0757", c => Debug.Log("Giant Trunade: Retorna S/T."));

        // 0759 - Gift of The Mystical Elf (Gain LP)
        AddEffect("0759", c => {
            int count = 0;
            if (GameManager.Instance.duelFieldUI != null) {
                foreach(var z in GameManager.Instance.duelFieldUI.playerMonsterZones) if(z.childCount > 0) count++;
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) count++;
            }
            Effect_GainLP(c, count * 300);
        });

        // 0760 - Gift of the Martyr (Buff)
        AddEffect("0760", c => Debug.Log("Gift of the Martyr: Buff ATK."));

        // 0763 - Gigantes (SS Banish Earth, Heavy Storm)
        AddEffect("0763", c => Debug.Log("Gigantes: SS Earth. Destrói S/T."));

        // 0767 - Gilasaurus (SS, Opp SS)
        AddEffect("0767", c => Debug.Log("Gilasaurus: SS da mão."));

        // 0768 - Gilford the Lightning (3 Tribute Raigeki)
        AddEffect("0768", c => Debug.Log("Gilford: Raigeki se 3 tributos."));

        // 0771 - Goblin Attack Force (Defense after attack)
        AddEffect("0771", c => Debug.Log("Goblin Attack Force: Vira defesa."));

        // 0773 - Goblin Elite Attack Force (Defense after attack)
        AddEffect("0773", c => Debug.Log("Goblin Elite: Vira defesa."));

        // 0774 - Goblin Fan (Destroy Flip)
        AddEffect("0774", c => Debug.Log("Goblin Fan: Destrói Flip Lv2-."));

        // 0775 - Goblin King (Stats)
        AddEffect("0775", c => Debug.Log("Goblin King: Stats por Fiends."));

        // 0776 - Goblin Thief (Damage/Heal)
        AddEffect("0776", c => { Effect_DirectDamage(c, 500); Effect_GainLP(c, 500); });

        // 0777 - Goblin Zombie (Mill/Search)
        AddEffect("0777", c => Debug.Log("Goblin Zombie: Mill e Busca."));

        // 0778 - Goblin of Greed (No discard cost)
        AddEffect("0778", c => Debug.Log("Goblin of Greed: Impede custos."));

        // 0779 - Goblin's Secret Remedy (Heal 600)
        AddEffect("0779", c => Effect_GainLP(c, 600));

        // 0780 - Goddess of Whim (Coin ATK)
        AddEffect("0780", c => Debug.Log("Goddess of Whim: Modifica ATK."));

        // 0781 - Goddess with the Third Eye (Fusion Sub)
        AddEffect("0781", c => Debug.Log("Goddess with the Third Eye: Substituto."));

        // 0784 - Golem Sentry (Flip Bounce)
        AddEffect("0784", c => Effect_FlipReturn(c, TargetType.Monster));

        // 0786 - Good Goblin Housekeeping (Draw/Return)
        AddEffect("0786", c => Debug.Log("Good Goblin Housekeeping: Draw e return."));

        // 0787 - Gora Turtle (Lock 1900+)
        AddEffect("0787", c => Debug.Log("Gora Turtle: Bloqueia 1900+."));

        // 0788 - Gora Turtle of Illusion (Negate Target)
        AddEffect("0788", c => Debug.Log("Gora Turtle of Illusion: Nega alvo S/T."));

        // 0790 - Gorgon's Eye (Negate Defense)
        AddEffect("0790", c => Debug.Log("Gorgon's Eye: Nega efeitos defesa."));

        // 0791 - Graceful Charity (Draw 3 Discard 2)
        AddEffect("0791", c => { 
            GameManager.Instance.DrawCard(); 
            GameManager.Instance.DrawCard(); 
            GameManager.Instance.DrawCard(); 
            Debug.Log("Graceful Charity: Descarte 2 cartas."); 
        });

        // 0792 - Graceful Dice (Roll Buff)
        AddEffect("0792", c => Debug.Log("Graceful Dice: Buff por dado."));

        // 0794 - Gradius' Option (Token)
        AddEffect("0794", c => Debug.Log("Gradius' Option: Token."));

        // 0795 - Granadora (Heal/Damage)
        AddEffect("0795", c => { Effect_GainLP(c, 1000); Debug.Log("Granadora: Dano ao morrer."); });

        // 0797 - Granmarg the Rock Monarch (Destroy Set)
        AddEffect("0797", c => Debug.Log("Granmarg: Destrói setada."));

        // 0799 - Grave Lure (Reveal)
        AddEffect("0799", c => Debug.Log("Grave Lure: Revela topo."));

        // 0800 - Grave Ohja (Damage on Flip)
        AddEffect("0800", c => Debug.Log("Grave Ohja: Dano em Flip Summon."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0801 - 0900)
        // =========================================================================================

        // 0801 - Grave Protector (Shuffle destroyed into Deck)
        AddEffect("0801", c => Debug.Log("Grave Protector: Monstros destruídos voltam ao deck."));

        // 0802 - Gravedigger Ghoul (Banish 2 from Opp GY)
        AddEffect("0802", c => Debug.Log("Gravedigger Ghoul: Banir 2 do GY do oponente."));

        // 0803 - Gravekeeper's Assailant (Change battle pos with Necrovalley)
        AddEffect("0803", c => Debug.Log("Gravekeeper's Assailant: Muda posição se Necrovalley."));

        // 0804 - Gravekeeper's Cannonholder (Tribute GK -> 700 dmg)
        AddEffect("0804", c => Effect_TributeToBurn(c, 1, 700, "Spellcaster")); // Simplificado para Spellcaster/GK

        // 0805 - Gravekeeper's Chief (Revive GK)
        AddEffect("0805", c => Debug.Log("Gravekeeper's Chief: Reviver Gravekeeper."));

        // 0806 - Gravekeeper's Curse (Burn 500 on Summon)
        AddEffect("0806", c => Effect_DirectDamage(c, 500));

        // 0807 - Gravekeeper's Guard (Flip Bounce)
        AddEffect("0807", c => Effect_FlipReturn(c, TargetType.Monster));

        // 0808 - Gravekeeper's Servant (Mill to attack)
        AddEffect("0808", c => Debug.Log("Gravekeeper's Servant: Oponente milla para atacar."));

        // 0809 - Gravekeeper's Spear Soldier (Piercing)
        AddEffect("0809", c => Debug.Log("Gravekeeper's Spear Soldier: Dano perfurante."));

        // 0810 - Gravekeeper's Spy (Flip SS GK)
        AddEffect("0810", c => Effect_SearchDeck(c, "Gravekeeper's"));

        // 0811 - Gravekeeper's Vassal (Effect Damage)
        AddEffect("0811", c => Debug.Log("Gravekeeper's Vassal: Dano de batalha vira efeito."));

        // 0812 - Gravekeeper's Watcher (Negate discard effect)
        AddEffect("0812", c => Debug.Log("Gravekeeper's Watcher: Nega efeito de descarte."));

        // 0813 - Graverobber (Use Opp Spell)
        AddEffect("0813", c => Debug.Log("Graverobber: Usar magia do oponente."));

        // 0814 - Graverobber's Retribution (Burn per banished)
        AddEffect("0814", c => Debug.Log("Graverobber's Retribution: Dano por banidas."));

        // 0816 - Gravity Axe - Grarl (Equip +500, Lock pos)
        AddEffect("0816", c => Effect_Equip(c, 500, 0));

        // 0817 - Gravity Bind (Level 4+ no attack)
        AddEffect("0817", c => Debug.Log("Gravity Bind: Nível 4+ não ataca."));

        // 0818 - Gray Wing (Discard -> Double Attack)
        AddEffect("0818", c => Debug.Log("Gray Wing: Ataque duplo."));

        // 0821 - Great Dezard (Negate / SS Fushioh)
        AddEffect("0821", c => Debug.Log("Great Dezard: Nega alvo / Invoca Fushioh."));

        // 0822 - Great Long Nose (Skip Battle Phase)
        AddEffect("0822", c => Debug.Log("Great Long Nose: Pula Battle Phase do oponente."));

        // 0823 - Great Maju Garzett (Double Tribute ATK)
        AddEffect("0823", c => Debug.Log("Great Maju Garzett: ATK = 2x Tributo."));

        // 0825 - Great Moth (SS Condition)
        AddEffect("0825", c => Debug.Log("Great Moth: Invocar via Petit Moth."));

        // 0826 - Great Phantom Thief (Hand destruction)
        AddEffect("0826", c => Debug.Log("Great Phantom Thief: Descarte ao causar dano."));

        // 0828 - Greed (Burn on Draw)
        AddEffect("0828", c => Debug.Log("Greed: Dano ao comprar por efeito."));

        // 0829 - Green Gadget (Search Red Gadget)
        AddEffect("0829", c => Effect_SearchDeck(c, "Red Gadget"));

        // 0831 - Greenkappa (Flip Destroy 2 S/T)
        AddEffect("0831", c => Debug.Log("Greenkappa: Destrói 2 S/T setadas."));

        // 0832 - Gren Maju Da Eiza (Stats = Banished * 400)
        AddEffect("0832", c => Debug.Log("Gren Maju: ATK por banidas."));

        // 0834 - Griggle (Heal on control switch)
        AddEffect("0834", c => Debug.Log("Griggle: Ganha 3000 LP ao trocar controle."));

        // 0836 - Ground Collapse (Block Zones)
        AddEffect("0836", c => Debug.Log("Ground Collapse: Bloqueia zonas."));

        // 0838 - Gryphon Wing (Anti-Harpie Duster)
        AddEffect("0838", c => Debug.Log("Gryphon Wing: Nega Duster e destrói."));

        // 0839 - Gryphon's Feather Duster (Destroy own S/T -> Heal)
        AddEffect("0839", c => Debug.Log("Gryphon's Feather Duster: Destrói próprias S/T e cura."));

        // 0840 - Guardian Angel Joan (Heal on destroy)
        AddEffect("0840", c => Debug.Log("Guardian Angel Joan: Cura igual ATK do destruído."));

        // 0841 - Guardian Baou (Negate effects / Buff)
        AddEffect("0841", c => Debug.Log("Guardian Baou: Nega efeitos e ganha ATK."));

        // 0842 - Guardian Ceal (Send Equip -> Destroy Monster)
        AddEffect("0842", c => Debug.Log("Guardian Ceal: Envia equip para destruir monstro."));

        // 0843 - Guardian Elma (Recycle Equip)
        AddEffect("0843", c => Debug.Log("Guardian Elma: Recupera equip do GY."));

        // 0844 - Guardian Grarl (SS Condition)
        AddEffect("0844", c => Debug.Log("Guardian Grarl: SS se tiver Gravity Axe."));

        // 0845 - Guardian Kay'est (Immune to Spells)
        AddEffect("0845", c => Debug.Log("Guardian Kay'est: Imune a magias."));

        // 0846 - Guardian Sphinx (Flip Bounce All)
        AddEffect("0846", c => Debug.Log("Guardian Sphinx: Retorna todos monstros do oponente."));

        // 0847 - Guardian Statue (Flip Bounce 1)
        AddEffect("0847", c => Effect_FlipReturn(c, TargetType.Monster));

        // 0848 - Guardian Tryce (SS on destroy)
        AddEffect("0848", c => Debug.Log("Guardian Tryce: Invoca material do GY."));

        // 0851 - Gust (Destroy S/T on S destruction)
        AddEffect("0851", c => Debug.Log("Gust: Destrói S/T se magia destruída."));

        // 0852 - Gust Fan (Equip Wind +400/-200)
        AddEffect("0852", c => Effect_Equip(c, 400, -200, "", "Wind"));

        // 0853 - Gyaku-Gire Panda (Buff per opp monster, Piercing)
        AddEffect("0853", c => Debug.Log("Gyaku-Gire Panda: Buff por monstros do oponente."));

        // 0855 - Gyroid (Protect once)
        AddEffect("0855", c => Debug.Log("Gyroid: Protege 1x por turno."));

        // 0856 - Hade-Hane (Flip Bounce 3)
        AddEffect("0856", c => Debug.Log("Hade-Hane: Retorna até 3 monstros."));

        // 0857 - Hallowed Life Barrier (Discard -> No Damage)
        AddEffect("0857", c => Debug.Log("Hallowed Life Barrier: Sem dano neste turno."));

        // 0858 - Hamburger Recipe (Ritual)
        AddEffect("0858", c => Debug.Log("Hamburger Recipe: Ritual."));

        // 0859 - Hammer Shot (Destroy highest ATK)
        AddEffect("0859", c => Debug.Log("Hammer Shot: Destrói maior ATK em ataque."));

        // 0860 - Hand of Nephthys (Tribute 2 -> SS Phoenix)
        AddEffect("0860", c => Debug.Log("Hand of Nephthys: Invoca Sacred Phoenix."));

        // 0861 - Hane-Hane (Flip Bounce 1)
        AddEffect("0861", c => Effect_FlipReturn(c, TargetType.Monster));

        // 0863 - Hannibal Necromancer (Counter -> Destroy Trap)
        AddEffect("0863", c => Debug.Log("Hannibal Necromancer: Remove contador para destruir Trap."));

        // 0868 - Harpie Lady 1 (Buff Wind)
        AddEffect("0868", c => Effect_Field(c, 300, 0, "", "Wind"));

        // 0869 - Harpie Lady 2 (Negate Flip)
        AddEffect("0869", c => Debug.Log("Harpie Lady 2: Nega efeitos de Flip."));

        // 0870 - Harpie Lady 3 (Lock Attack)
        AddEffect("0870", c => Debug.Log("Harpie Lady 3: Bloqueia ataque do oponente por 2 turnos."));

        // 0871 - Harpie Lady Sisters (SS Condition)
        AddEffect("0871", c => Debug.Log("Harpie Lady Sisters: SS via Elegant Egotist."));

        // 0872 - Harpie's Feather Duster (Destroy Opp S/T)
        AddEffect("0872", Effect_HarpiesFeatherDuster);

        // 0873 - Harpie's Pet Dragon (Buff per Harpie)
        AddEffect("0873", c => Debug.Log("Harpie's Pet Dragon: Ganha ATK por Harpies."));

        // 0874 - Harpies' Hunting Ground (Field Buff / Destroy S/T)
        AddEffect("0874", c => { Effect_Field(c, 200, 200, "Winged Beast"); Debug.Log("Hunting Ground: Destrói S/T na invocação de Harpie."); });

        // 0875 - Hayabusa Knight (Double Attack)
        AddEffect("0875", c => Debug.Log("Hayabusa Knight: Ataque duplo."));

        // 0877 - Heart of Clear Water (Equip Protect)
        AddEffect("0877", c => Debug.Log("Heart of Clear Water: Protege monstro fraco."));

        // 0878 - Heart of the Underdog (Draw Normal -> Draw again)
        AddEffect("0878", c => Debug.Log("Heart of the Underdog: Compra extra se comprar Normal Monster."));

        // 0879 - Heavy Mech Support Platform (Union)
        AddEffect("0879", c => Debug.Log("Heavy Mech Support Platform: Union."));

        // 0880 - Heavy Slump (Hand shuffle draw 2)
        AddEffect("0880", c => Debug.Log("Heavy Slump: Oponente compra 2 se tiver 8+."));

        // 0881 - Heavy Storm (Destroy all S/T)
        AddEffect("0881", Effect_HeavyStorm);

        // 0882 - Helping Robo for Combat (Draw/BottomDeck)
        AddEffect("0882", c => Debug.Log("Helping Robo: Compra 1, retorna 1."));

        // 0883 - Helpoemer (Discard on Battle Phase)
        AddEffect("0883", c => Debug.Log("Helpoemer: Oponente descarta na Battle Phase."));

        // 0885 - Hero Signal (SS HERO on destroy)
        AddEffect("0885", c => Debug.Log("Hero Signal: Invoca HERO do deck."));

        // 0888 - Hidden Soldiers (SS Dark on Opp Summon)
        AddEffect("0888", c => Debug.Log("Hidden Soldiers: Invoca DARK da mão."));

        // 0889 - Hidden Spellbook (Recycle 2 Spells)
        AddEffect("0889", c => Debug.Log("Hidden Spellbook: Recicla 2 magias."));

        // 0890 - Hieracosphinx (Protect Face-down)
        AddEffect("0890", c => Debug.Log("Hieracosphinx: Oponente não pode atacar face-down."));

        // 0891 - Hieroglyph Lithograph (Hand limit 7)
        AddEffect("0891", c => { Effect_PayLP(c, 1000); Debug.Log("Hieroglyph Lithograph: Limite de mão 7."); });

        // 0893 - Hiita the Fire Charmer (Flip Control Fire)
        AddEffect("0893", c => Debug.Log("Hiita: Controlar monstro FIRE."));

        // 0894 - Hino-Kagu-Tsuchi (Hand Destruction)
        AddEffect("0894", c => Debug.Log("Hino-Kagu-Tsuchi: Oponente descarta mão na Draw Phase."));

        // 0895 - Hinotama (Burn 500)
        AddEffect("0895", c => Effect_DirectDamage(c, 500));

        // 0897 - Hiro's Shadow Scout (Flip Draw 3 / Discard Spells)
        AddEffect("0897", c => Debug.Log("Hiro's Shadow Scout: Oponente compra 3, descarta magias."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0901 - 1000)
        // =========================================================================================

        // 0901 - Homunculus the Alchemic Being (Change Attribute)
        AddEffect("0901", c => Debug.Log("Homunculus: Mudar atributo."));

        // 0903 - Horn of Heaven (Negate Summon)
        AddEffect("0903", c => Debug.Log("Horn of Heaven: Tributar 1 para negar invocação."));

        // 0904 - Horn of Light (Equip +800 DEF, Recycle)
        AddEffect("0904", c => Effect_Equip(c, 0, 800));

        // 0905 - Horn of the Unicorn (Equip +700/700, Recycle)
        AddEffect("0905", c => Effect_Equip(c, 700, 700));

        // 0906 - Horus the Black Flame Dragon LV4 (Level Up)
        AddEffect("0906", c => Effect_LevelUp(c, "0907")); // LV6

        // 0907 - Horus the Black Flame Dragon LV6 (Unaffected by Spells, Level Up)
        AddEffect("0907", c => Effect_LevelUp(c, "0908")); // LV8

        // 0908 - Horus the Black Flame Dragon LV8 (Negate Spells)
        AddEffect("0908", c => Debug.Log("Horus LV8: Negar ativação de Magia."));

        // 0909 - Horus' Servant (Protect Horus)
        AddEffect("0909", c => Debug.Log("Horus' Servant: Protege Horus de alvo."));

        // 0910 - Hoshiningen (Field Light +500, Dark -400)
        AddEffect("0910", c => Effect_Field(c, 500, -400, "", "Light"));

        // 0911 - Hourglass of Courage (Stats Halved/Doubled)
        AddEffect("0911", c => Debug.Log("Hourglass of Courage: Stats metade/dobro."));

        // 0913 - House of Adhesive Tape (Destroy Summon DEF <= 500)
        AddEffect("0913", c => Debug.Log("House of Adhesive Tape: Destrói invocação com DEF <= 500."));

        // 0914 - Howling Insect (Search Insect <= 1500)
        AddEffect("0914", c => Effect_SearchDeck(c, "Insect"));

        // 0915 - Huge Revolution (Nuke Hand/Field)
        AddEffect("0915", c => Debug.Log("Huge Revolution: Destrói tudo se tiver trio da revolução."));

        // 0916 - Human-Wave Tactics (SS Normal Monsters)
        AddEffect("0916", c => Debug.Log("Human-Wave Tactics: Invoca Normais na End Phase."));

        // 0922 - Hyena (SS Hyena)
        AddEffect("0922", c => Effect_SearchDeck(c, "Hyena"));

        // 0926 - Hyper Hammerhead (Bounce)
        AddEffect("0926", c => Debug.Log("Hyper Hammerhead: Retorna oponente para mão após batalha."));

        // 0927 - Hysteric Fairy (Tribute 2 -> Gain 1000 LP)
        AddEffect("0927", c => { Debug.Log("Hysteric Fairy: Tributa 2 para ganhar 1000 LP."); Effect_GainLP(c, 1000); });

        // 0931 - Impenetrable Formation (Buff DEF)
        AddEffect("0931", c => Debug.Log("Impenetrable Formation: +700 DEF."));

        // 0932 - Imperial Order (Negate Spells)
        AddEffect("0932", c => Debug.Log("Imperial Order: Nega todas as Magias."));

        // 0933 - Inaba White Rabbit (Direct Attack)
        AddEffect("0933", c => Debug.Log("Inaba White Rabbit: Ataque direto, retorna para mão."));

        // 0935 - Indomitable Fighter Lei Lei (Attack -> Defense)
        AddEffect("0935", c => Debug.Log("Lei Lei: Vira defesa após atacar."));

        // 0936 - Infernal Flame Emperor (Banish Fire -> Destroy S/T)
        AddEffect("0936", c => Debug.Log("Infernal Flame Emperor: Bane Fire para destruir S/T."));

        // 0937 - Infernalqueen Archfiend (Buff Archfiend)
        AddEffect("0937", c => Debug.Log("Infernalqueen: +1000 ATK para um Archfiend."));

        // 0938 - Inferno (SS Banish Fire, Burn)
        AddEffect("0938", c => Debug.Log("Inferno: SS banindo Fire. 1500 dano ao destruir monstro."));

        // 0939 - Inferno Fire Blast (Red-Eyes Burn)
        AddEffect("0939", c => Debug.Log("Inferno Fire Blast: Dano igual ATK do Red-Eyes."));

        // 0940 - Inferno Hammer (Flip Face-down)
        AddEffect("0940", c => Debug.Log("Inferno Hammer: Vira monstro do oponente face-down."));

        // 0941 - Inferno Tempest (Banish Decks/GYs)
        AddEffect("0941", c => Debug.Log("Inferno Tempest: Bane monstros dos Decks e GYs."));

        // 0942 - Infinite Cards (No Hand Limit)
        AddEffect("0942", c => Debug.Log("Infinite Cards: Sem limite de mão."));

        // 0943 - Infinite Dismissal (Destroy Lv3-)
        AddEffect("0943", c => Debug.Log("Infinite Dismissal: Destrói Lv3 ou menor na End Phase."));

        // 0944 - Injection Fairy Lily (Pay 2000 -> +3000 ATK)
        AddEffect("0944", c => Debug.Log("Injection Fairy Lily: Paga 2000 LP para ganhar 3000 ATK."));

        // 0946 - Insect Armor with Laser Cannon (Equip Insect +700)
        AddEffect("0946", c => Effect_Equip(c, 700, 0, "Insect"));

        // 0947 - Insect Barrier (Block Insect Attacks)
        AddEffect("0947", c => Debug.Log("Insect Barrier: Insetos do oponente não atacam."));

        // 0948 - Insect Imitation (Tribute -> SS Insect Lv+1)
        AddEffect("0948", c => Debug.Log("Insect Imitation: Evoluir Inseto."));

        // 0950 - Insect Princess (Attack Pos, Buff)
        AddEffect("0950", c => Debug.Log("Insect Princess: Insetos oponente em ataque. Ganha ATK."));

        // 0951 - Insect Queen (Buff, Token)
        AddEffect("0951", c => Debug.Log("Insect Queen: Buff por Insetos. Gera Token."));

        // 0952 - Insect Soldiers of the Sky (Buff vs Wind)
        AddEffect("0952", c => Debug.Log("Insect Soldiers: +1000 ATK contra Wind."));

        // 0953 - Inspection (Look Hand)
        AddEffect("0953", c => { Effect_PayLP(c, 500); Debug.Log("Inspection: Olhar carta da mão."); });

        // 0954 - Interdimensional Matter Transporter (Banish until End Phase)
        AddEffect("0954", c => Debug.Log("Interdimensional Matter Transporter: Banir temporariamente."));

        // 0956 - Invader of Darkness (No Quick-Play)
        AddEffect("0956", c => Debug.Log("Invader of Darkness: Bloqueia Quick-Play Spells."));

        // 0957 - Invader of the Throne (Flip Swap)
        AddEffect("0957", c => Debug.Log("Invader of the Throne: Troca controle."));

        // 0958 - Invasion of Flames (No Traps)
        AddEffect("0958", c => Debug.Log("Invasion of Flames: Sem traps na invocação."));

        // 0959 - Invigoration (Equip Earth +400/-200)
        AddEffect("0959", c => Effect_Equip(c, 400, -200, "", "Earth"));

        // 0960 - Invitation to a Dark Sleep (Lock Attack)
        AddEffect("0960", c => Debug.Log("Invitation to a Dark Sleep: Impede ataque."));

        // 0961 - Iron Blacksmith Kotetsu (Flip Add Equip)
        AddEffect("0961", c => Effect_SearchDeck(c, "Equip"));

        // 0964 - Jade Insect Whistle (Opponent Search Insect)
        AddEffect("0964", c => Debug.Log("Jade Insect Whistle: Oponente busca Inseto para o topo."));

        // 0965 - Jam Breeding Machine (Token)
        AddEffect("0965", c => Debug.Log("Jam Breeding Machine: Gera Slime Token."));

        // 0966 - Jam Defender (Redirect)
        AddEffect("0966", c => Debug.Log("Jam Defender: Redireciona ataque para Revival Jam."));

        // 0967 - Jar Robber (Negate Pot)
        AddEffect("0967", c => Debug.Log("Jar Robber: Nega Pot of Greed e você compra."));

        // 0968 - Jar of Greed (Draw 1)
        AddEffect("0968", c => { GameManager.Instance.DrawCard(); Debug.Log("Jar of Greed: Comprou 1 carta."); });

        // 0973 - Jetroid (Trap from Hand)
        AddEffect("0973", c => Debug.Log("Jetroid: Pode ativar Trap da mão se atacado."));

        // 0974 - Jigen Bakudan (Flip Nuke)
        AddEffect("0974", c => Debug.Log("Jigen Bakudan: Destrói tudo e causa dano."));

        // 0975 - Jinzo (Negate Traps)
        AddEffect("0975", c => Debug.Log("Jinzo: Nega todas as Armadilhas."));

        // 0976 - Jinzo #7 (Direct Attack)
        AddEffect("0976", c => Debug.Log("Jinzo #7: Ataque direto."));

        // 0977 - Jirai Gumo (Coin Toss Attack)
        AddEffect("0977", c => Debug.Log("Jirai Gumo: Moeda ao atacar."));

        // 0979 - Jowgen the Spiritualist (Destroy SS / Prevent SS)
        AddEffect("0979", c => Debug.Log("Jowgen: Destrói SS e impede novas SS."));

        // 0980 - Jowls of Dark Demise (Flip Control)
        AddEffect("0980", c => Debug.Log("Jowls: Toma controle até End Phase."));

        // 0982 - Judgment of Anubis (Counter S/T Destroy)
        AddEffect("0982", c => Debug.Log("Judgment of Anubis: Nega destruição de S/T, destrói monstro e causa dano."));

        // 0983 - Judgment of the Desert (Lock Position)
        AddEffect("0983", c => Debug.Log("Judgment of the Desert: Trava posição de batalha."));

        // 0984 - Judgment of the Pharaoh (Lock/Negate)
        AddEffect("0984", c => Debug.Log("Judgment of the Pharaoh: Bloqueia invocação ou S/T."));

        // 0985 - Just Desserts (Burn 500 per monster)
        AddEffect("0985", c => {
            int count = 0;
            if (GameManager.Instance.duelFieldUI != null) {
                foreach(var z in GameManager.Instance.duelFieldUI.opponentMonsterZones) if(z.childCount > 0) count++;
            }
            Effect_DirectDamage(c, count * 500);
        });

        // 0986 - KA-2 Des Scissors (Burn on destroy)
        AddEffect("0986", c => Debug.Log("KA-2 Des Scissors: Dano igual nível x 500."));

        // 0990 - Kaibaman (Tribute -> SS Blue-Eyes)
        AddEffect("0990", c => Debug.Log("Kaibaman: Invoca Blue-Eyes da mão."));

        // 0992 - Kaiser Colosseum (Limit Monsters)
        AddEffect("0992", c => Debug.Log("Kaiser Colosseum: Limita número de monstros do oponente."));

        // 0994 - Kaiser Glider (Bounce on destroy)
        AddEffect("0994", c => Debug.Log("Kaiser Glider: Retorna monstro para mão."));

        // 0995 - Kaiser Sea Horse (2 Tributes Light)
        AddEffect("0995", c => Debug.Log("Kaiser Sea Horse: 2 tributos para Light."));

        // 0998 - Kaminote Blow (Destroy with Monk)
        AddEffect("0998", c => Debug.Log("Kaminote Blow: Destrói monstro que batalhou com Monk."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1001 - 1100)
        // =========================================================================================

        // 1001 - Kangaroo Champ (Change to Defense)
        AddEffect("1001", c => Debug.Log("Kangaroo Champ: Vira defesa após batalha."));

        // 1004 - Karakuri Spider (Destroy Dark)
        AddEffect("1004", c => Debug.Log("Karakuri Spider: Destrói DARK se atacar."));

        // 1005 - Karate Man (Double ATK)
        AddEffect("1005", c => Debug.Log("Karate Man: Dobra ATK, morre na End Phase."));

        // 1008 - Kazejin (Zero ATK)
        AddEffect("1008", c => Debug.Log("Kazejin: Zera ATK do atacante."));

        // 1009 - Kelbek (Bounce attacker)
        AddEffect("1009", c => Debug.Log("Kelbek: Retorna atacante para mão."));

        // 1010 - Keldo (Shuffle GY into Deck)
        AddEffect("1010", c => Debug.Log("Keldo: Retorna 2 do GY do oponente para o Deck."));

        // 1014 - King Dragun (SS Dragon)
        AddEffect("1014", c => Debug.Log("King Dragun: Invoca Dragão da mão."));

        // 1016 - King Tiger Wanghu (Destroy weak summon)
        AddEffect("1016", c => Debug.Log("King Tiger Wanghu: Destrói invocação <= 1400 ATK."));

        // 1018 - King of the Skull Servants (Stats / Revive)
        AddEffect("1018", c => Debug.Log("King of the Skull Servants: ATK por Skull Servants."));

        // 1019 - King of the Swamp (Search Poly)
        AddEffect("1019", c => Effect_SearchDeck(c, "Polymerization"));

        // 1020 - King's Knight (SS Jack's Knight)
        AddEffect("1020", c => Debug.Log("King's Knight: Invoca Jack's Knight."));

        // 1021 - Kiryu (Union)
        AddEffect("1021", c => Debug.Log("Kiryu: Union para Dark Blade."));

        // 1022 - Kiseitai (Equip on attack)
        AddEffect("1022", c => Debug.Log("Kiseitai: Equipa no atacante e cura."));

        // 1023 - Kishido Spirit (Battle protection)
        AddEffect("1023", c => Debug.Log("Kishido Spirit: Protege se ATK igual."));

        // 1024 - Knight's Title (SS Dark Magician Knight)
        AddEffect("1024", c => Debug.Log("Knight's Title: Invoca Dark Magician Knight."));

        // 1025 - Koitsu (Union)
        AddEffect("1025", c => Debug.Log("Koitsu: Union para Aitsu."));

        // 1028 - Kotodama (Destroy duplicates)
        AddEffect("1028", c => Debug.Log("Kotodama: Destrói monstros com mesmo nome."));

        // 1031 - Kozaky's Self-Destruct Button (Damage on destroy)
        AddEffect("1031", c => Debug.Log("Kozaky's Self-Destruct Button: 1000 dano."));

        // 1033 - Kryuel (Coin destroy)
        AddEffect("1033", c => Debug.Log("Kryuel: Moeda para destruir."));

        // 1035 - Kunai with Chain (Mode change / Buff)
        AddEffect("1035", c => Debug.Log("Kunai with Chain: Vira defesa ou +500 ATK."));

        // 1037 - Kuriboh (Negate damage)
        AddEffect("1037", c => Debug.Log("Kuriboh: Descarta para 0 dano."));

        // 1040 - Kycoo the Ghost Destroyer (Banish GY)
        AddEffect("1040", c => Debug.Log("Kycoo: Bane do GY ao causar dano."));

        // 1046 - Labyrinth of Nightmare (Change positions)
        AddEffect("1046", c => Debug.Log("Labyrinth of Nightmare: Muda posições na End Phase."));

        // 1047 - Lady Assailant of Flames (Flip Banish/Burn)
        AddEffect("1047", c => Debug.Log("Lady Assailant: Bane topo do deck e causa dano."));

        // 1048 - Lady Ninja Yae (Bounce S/T)
        AddEffect("1048", c => Debug.Log("Lady Ninja Yae: Descarta Wind para retornar S/T."));

        // 1049 - Lady Panther (Recycle)
        AddEffect("1049", c => Debug.Log("Lady Panther: Recupera monstro destruído."));

        // 1051 - Larvae Moth (SS Condition)
        AddEffect("1051", c => Debug.Log("Larvae Moth: SS via Petit Moth."));

        // 1053 - Laser Cannon Armor (Equip Insect +300)
        AddEffect("1053", c => Effect_Equip(c, 300, 300, "Insect"));

        // 1054 - Last Day of Witch (Destroy Spellcasters)
        AddEffect("1054", c => Effect_DestroyType(c, "Spellcaster"));

        // 1055 - Last Turn (Win Condition)
        AddEffect("1055", c => Debug.Log("Last Turn: Evento especial de vitória."));

        // 1056 - Last Will (SS from Deck)
        AddEffect("1056", c => Debug.Log("Last Will: Invoca do deck se monstro foi enviado ao GY."));

        // 1059 - Lava Battleguard (Buff)
        AddEffect("1059", c => Debug.Log("Lava Battleguard: Buff por Swamp Battleguard."));

        // 1060 - Lava Golem (Tribute 2 Opp, Burn)
        AddEffect("1060", c => Debug.Log("Lava Golem: Tributa 2 do oponente, causa dano."));

        // 1063 - Legacy Hunter (Hand Shuffle)
        AddEffect("1063", c => Debug.Log("Legacy Hunter: Oponente embaralha carta da mão no deck."));

        // 1064 - Legacy of Yata-Garasu (Draw)
        AddEffect("1064", c => { GameManager.Instance.DrawCard(); Debug.Log("Legacy of Yata-Garasu: Compra 1 (ou 2)."); });

        // 1065 - Legendary Black Belt (Burn DEF)
        AddEffect("1065", c => Debug.Log("Legendary Black Belt: Dano igual DEF."));

        // 1066 - Legendary Fiend (Gain ATK)
        AddEffect("1066", c => Debug.Log("Legendary Fiend: Ganha 700 ATK na Standby."));

        // 1067 - Legendary Flame Lord (Ritual)
        AddEffect("1067", c => Debug.Log("Legendary Flame Lord: Ritual."));

        // 1068 - Legendary Jujitsu Master (Spin)
        AddEffect("1068", c => Debug.Log("Legendary Jujitsu Master: Retorna atacante ao topo do deck."));

        // 1069 - Legendary Sword (Equip Warrior +300)
        AddEffect("1069", c => Effect_Equip(c, 300, 300, "Warrior"));

        // 1070 - Leghul (Direct Attack)
        AddEffect("1070", c => Debug.Log("Leghul: Ataque direto."));

        // 1071 - Lekunga (SS Token)
        AddEffect("1071", c => Debug.Log("Lekunga: Bane Water para invocar Token."));

        // 1075 - Lesser Fiend (Banish destroyed)
        AddEffect("1075", c => Debug.Log("Lesser Fiend: Bane monstros destruídos."));

        // 1076 - Level Conversion Lab (Change Level)
        AddEffect("1076", c => Debug.Log("Level Conversion Lab: Muda nível."));

        // 1077 - Level Limit - Area B (Defense Position)
        AddEffect("1077", c => Debug.Log("Level Limit - Area B: Nível 4+ em defesa."));

        // 1078 - Level Up! (SS LV monster)
        AddEffect("1078", c => Debug.Log("Level Up!: Evolui monstro LV."));

        // 1079 - Levia-Dragon - Daedalus (Nuke)
        AddEffect("1079", c => Debug.Log("Daedalus: Envia Umi para destruir tudo."));

        // 1080 - Life Absorbing Machine (Heal)
        AddEffect("1080", c => Debug.Log("Life Absorbing Machine: Recupera metade do LP pago."));

        // 1081 - Light of Intervention (No Set)
        AddEffect("1081", c => Debug.Log("Light of Intervention: Monstros não podem ser setados."));

        // 1082 - Light of Judgment (Hand/Field destruction)
        AddEffect("1082", c => Debug.Log("Light of Judgment: Descarta ou destrói."));

        // 1083 - Lighten the Load (Reload high level)
        AddEffect("1083", c => Debug.Log("Lighten the Load: Embaralha Nível 7+ para comprar."));

        // 1084 - Lightforce Sword (Banish hand)
        AddEffect("1084", c => Debug.Log("Lightforce Sword: Bane carta da mão temporariamente."));

        // 1085 - Lightning Blade (Equip Warrior +800)
        AddEffect("1085", c => Effect_Equip(c, 800, 0, "Warrior"));

        // 1087 - Lightning Vortex (Destroy Face-up)
        AddEffect("1087", c => Debug.Log("Lightning Vortex: Descarta 1, destrói face-up do oponente."));

        // 1088 - Limiter Removal (Double Machine ATK)
        AddEffect("1088", c => Debug.Log("Limiter Removal: Dobra ATK de Máquinas."));

        // 1091 - Little Chimera (Field Fire +500, Water -400)
        AddEffect("1091", c => Effect_Field(c, 500, -400, "", "Fire"));

        // 1093 - Little-Winguard (Change Pos)
        AddEffect("1093", c => Debug.Log("Little-Winguard: Muda posição na End Phase."));

        // 1096 - Lone Wolf (Immunity)
        AddEffect("1096", c => Debug.Log("Lone Wolf: Imunidade para monstros específicos."));

        // 1097 - Lord Poison (Revive Plant)
        AddEffect("1097", c => Debug.Log("Lord Poison: Revive Planta ao ser destruído."));

        // 1098 - Lord of D. (Protect Dragons)
        AddEffect("1098", c => Debug.Log("Lord of D.: Protege Dragões de alvo."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1101 - 1200)
        // =========================================================================================

        // 1101 - Lost Guardian (DEF = banished Rock * 700)
        AddEffect("1101", c => Debug.Log("Lost Guardian: DEF baseado em Rocks banidos."));

        // 1103 - Luminous Soldier (Buff vs Dark)
        AddEffect("1103", c => Debug.Log("Luminous Soldier: +500 ATK contra DARK."));

        // 1104 - Luminous Spark (Field Light +500/-400)
        AddEffect("1104", c => Effect_Field(c, 500, -400, "", "Light"));

        // 1112 - Machine Conversion Factory (Equip Machine +300/300)
        AddEffect("1112", c => Effect_Equip(c, 300, 300, "Machine"));

        // 1113 - Machine Duplication (SS duplicates)
        AddEffect("1113", c => Debug.Log("Machine Duplication: Invoca cópias do deck."));

        // 1114 - Machine King (Buff per Machine)
        AddEffect("1114", c => Debug.Log("Machine King: +100 ATK por Machine."));

        // 1117 - Mad Sword Beast (Piercing)
        AddEffect("1117", c => Debug.Log("Mad Sword Beast: Dano perfurante."));

        // 1119 - Mage Power (Equip Buff per S/T)
        AddEffect("1119", Effect_MagePower);

        // 1120 - Magic Cylinder (Negate & Burn)
        AddEffect("1120", Effect_MagicCylinder);

        // 1121 - Magic Drain (Counter Spell)
        AddEffect("1121", c => Debug.Log("Magic Drain: Nega magia se oponente não descartar."));

        // 1122 - Magic Formula (Equip DM +700, Heal)
        AddEffect("1122", c => { Effect_Equip(c, 700, 0, "Spellcaster"); Debug.Log("Magic Formula: Cura 1000 se for pro GY."); });

        // 1123 - Magic Jammer (Discard 1, Negate Spell)
        AddEffect("1123", c => Debug.Log("Magic Jammer: Descarta 1 para negar magia."));

        // 1124 - Magic Reflector (Counter on Spell)
        AddEffect("1124", c => Debug.Log("Magic Reflector: Protege magia com contador."));

        // 1125 - Magical Arm Shield (Redirect Attack)
        AddEffect("1125", c => Debug.Log("Magical Arm Shield: Redireciona ataque para monstro do oponente."));

        // 1126 - Magical Dimension (Tribute SS Destroy)
        AddEffect("1126", c => Debug.Log("Magical Dimension: Tributa, Invoca Mago, Destrói."));

        // 1127 - Magical Explosion (Burn per Spell in GY)
        AddEffect("1127", c => Debug.Log("Magical Explosion: Dano por magias no GY."));

        // 1129 - Magical Hats (Hide monster)
        AddEffect("1129", c => Debug.Log("Magical Hats: Esconde monstro e invoca 2 S/T como monstros."));

        // 1130 - Magical Labyrinth (Equip Wall, SS Shadow)
        AddEffect("1130", c => Debug.Log("Magical Labyrinth: Invoca Wall Shadow."));

        // 1131 - Magical Marionette (Counters, Destroy)
        AddEffect("1131", c => Debug.Log("Magical Marionette: Remove contadores para destruir monstros."));

        // 1132 - Magical Merchant (Excavate)
        AddEffect("1132", c => Debug.Log("Magical Merchant: Escava até achar S/T."));

        // 1133 - Magical Plant Mandragola (Counters)
        AddEffect("1133", c => Debug.Log("Mandragola: Coloca contadores em tudo."));

        // 1134 - Magical Scientist (Pay 1000 SS Fusion)
        AddEffect("1134", c => { Effect_PayLP(c, 1000); Debug.Log("Magical Scientist: Invoca Fusão Lv6-."); });

        // 1135 - Magical Stone Excavation (Discard 2, Add Spell)
        AddEffect("1135", c => Debug.Log("Magical Stone Excavation: Recupera magia do GY."));

        // 1136 - Magical Thorn (Burn on discard)
        AddEffect("1136", c => Debug.Log("Magical Thorn: Dano quando oponente descarta."));

        // 1137 - Magician of Black Chaos (Ritual)
        AddEffect("1137", c => Debug.Log("Magician of Black Chaos: Ritual."));

        // 1138 - Magician of Faith (Flip Add Spell)
        AddEffect("1138", c => Debug.Log("Magician of Faith: Recupera magia do GY."));

        // 1139 - Magician's Valkyria (Protect Spellcasters)
        AddEffect("1139", c => Debug.Log("Magician's Valkyria: Oponente não pode atacar outros Magos."));

        // 1140 - Maha Vailo (Buff per Equip)
        AddEffect("1140", c => Debug.Log("Maha Vailo: Ganha ATK por equipamentos."));

        // 1141 - Maharaghi (Spirit, Peek)
        AddEffect("1141", c => Debug.Log("Maharaghi: Olha topo do deck."));

        // 1142 - Maiden of the Aqua (Umi field)
        AddEffect("1142", c => Debug.Log("Maiden of the Aqua: Trata campo como Umi."));

        // 1144 - Maji-Gire Panda (Buff per Beast destroyed)
        AddEffect("1144", c => Debug.Log("Maji-Gire Panda: Ganha ATK quando Besta é destruída."));

        // 1145 - Major Riot (Return -> SS)
        AddEffect("1145", c => Debug.Log("Major Riot: Reset de monstros."));

        // 1146 - Maju Garzett (ATK = Tributes)
        AddEffect("1146", c => Debug.Log("Maju Garzett: ATK igual soma dos tributos."));

        // 1147 - Makiu, the Magical Mist (Destroy DEF < ATK)
        AddEffect("1147", c => Debug.Log("Makiu: Destrói monstros com DEF baixa."));

        // 1148 - Makyura the Destructor (Trap from hand)
        AddEffect("1148", c => Debug.Log("Makyura: Ativar Traps da mão."));

        // 1149 - Malevolent Catastrophe (Destroy S/T on attack)
        AddEffect("1149", c => Debug.Log("Malevolent Catastrophe: Destrói todas as S/T."));

        // 1150 - Malevolent Nuzzler (Equip +700)
        AddEffect("1150", c => Effect_Equip(c, 700, 0));

        // 1151 - Malice Dispersion (Discard 1, Destroy Continuous Traps)
        AddEffect("1151", c => Debug.Log("Malice Dispersion: Destrói Traps Contínuas."));

        // 1152 - Malice Doll of Demise (Revive)
        AddEffect("1152", c => Debug.Log("Malice Doll: Renasce se enviado por Continuous Spell."));

        // 1155 - Man-Eater Bug (Flip Destroy)
        AddEffect("1155", c => Effect_FlipDestroy(c, TargetType.Monster));

        // 1159 - Man-Thro' Tro' (Tribute Normal -> 800 dmg)
        AddEffect("1159", c => Effect_TributeToBurn(c, 1, 800));

        // 1160 - Manga Ryu-Ran (Toon)
        AddEffect("1160", c => Debug.Log("Manga Ryu-Ran: Toon."));

        // 1161 - Manju of the Ten Thousand Hands (Search Ritual)
        AddEffect("1161", c => Effect_SearchDeck(c, "Ritual"));

        // 1162 - Manticore of Darkness (Revive loop)
        AddEffect("1162", c => Debug.Log("Manticore: Revive enviando besta."));

        // 1163 - Marauding Captain (SS from hand, Lock attack)
        AddEffect("1163", c => Debug.Log("Marauding Captain: Invoca da mão, protege Warriors."));

        // 1165 - Marshmallon (Indestructible, Burn)
        AddEffect("1165", c => Debug.Log("Marshmallon: Indestrutível em batalha, 1000 dano."));

        // 1166 - Marshmallon Glasses (Redirect attack)
        AddEffect("1166", c => Debug.Log("Marshmallon Glasses: Ataques devem ser no Marshmallon."));

        // 1167 - Maryokutai (Negate Spell)
        AddEffect("1167", c => Debug.Log("Maryokutai: Nega magia."));

        // 1169 - Mask of Brutality (Equip +1000/-1000)
        AddEffect("1169", c => Effect_Equip(c, 1000, -1000));

        // 1170 - Mask of Darkness (Flip Add Trap)
        AddEffect("1170", c => Debug.Log("Mask of Darkness: Recupera Trap do GY."));

        // 1171 - Mask of Dispel (Burn 500)
        AddEffect("1171", c => Debug.Log("Mask of Dispel: Dano por turno."));

        // 1172 - Mask of Restrict (No Tributes)
        AddEffect("1172", c => Debug.Log("Mask of Restrict: Ninguém pode tributar."));

        // 1173 - Mask of Weakness (Debuff -700)
        AddEffect("1173", c => Debug.Log("Mask of Weakness: -700 ATK."));

        // 1174 - Mask of the Accursed (Lock Attack, Burn)
        AddEffect("1174", c => Debug.Log("Mask of the Accursed: Trava ataque e causa dano."));

        // 1175 - Masked Beast Des Gardius (Snatch Steal on death)
        AddEffect("1175", c => Debug.Log("Des Gardius: Equipa Mask of Remnants ao morrer."));

        // 1177 - Masked Dragon (Float into Dragon)
        AddEffect("1177", c => Debug.Log("Masked Dragon: Invoca Dragão <= 1500 do deck."));

        // 1178 - Masked Sorcerer (Draw on damage)
        AddEffect("1178", c => Debug.Log("Masked Sorcerer: Compra 1 ao causar dano."));

        // 1179 - Mass Driver (Tribute -> 400 dmg)
        AddEffect("1179", c => Effect_TributeToBurn(c, 1, 400));

        // 1182 - Master Monk (Double Attack)
        AddEffect("1182", c => Debug.Log("Master Monk: Ataque duplo."));

        // 1184 - Mataza the Zapper (Double Attack, No Control Switch)
        AddEffect("1184", c => Debug.Log("Mataza: Ataque duplo, controle fixo."));

        // 1186 - Maximum Six (Roll die -> Gain ATK)
        AddEffect("1186", c => Debug.Log("Maximum Six: Ganha ATK no dado."));

        // 1187 - Mazera DeVille (Hand Destruction)
        AddEffect("1187", c => Debug.Log("Mazera DeVille: Oponente descarta 3."));

        // 1190 - Mecha-Dog Marron (Burn on destroy)
        AddEffect("1190", c => Debug.Log("Mecha-Dog Marron: 1000 dano."));

        // 1192 - Mechanical Hound (No Spells)
        AddEffect("1192", c => Debug.Log("Mechanical Hound: Oponente não ativa magias se você não tiver mão."));

        // 1196 - Medusa Worm (Flip Destroy, Cycle)
        AddEffect("1196", c => Debug.Log("Medusa Worm: Destrói monstro, vira face-down."));

        // 1197 - Mefist the Infernal General (Piercing, Discard)
        AddEffect("1197", c => Debug.Log("Mefist: Perfurante e descarte."));

        // 1199 - Mega Ton Magical Cannon (Remove 10 counters -> Nuke)
        AddEffect("1199", c => Debug.Log("Mega Ton Magical Cannon: Remove 10 contadores, destrói tudo do oponente."));

        // 1200 - Megamorph (Double/Halve ATK)
        AddEffect("1200", c => Debug.Log("Megamorph: Dobra ou divide ATK baseado no LP."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1201 - 1300)
        // =========================================================================================

        // 1201 - Megarock Dragon (SS Banish Rocks)
        AddEffect("1201", c => Debug.Log("Megarock Dragon: SS banindo Rocks. Stats = qtd * 700."));

        // 1207 - Mermaid Knight (Double Attack with Umi)
        AddEffect("1207", c => Debug.Log("Mermaid Knight: Ataque duplo com Umi."));

        // 1208 - Mesmeric Control (Lock Battle Position)
        AddEffect("1208", c => Debug.Log("Mesmeric Control: Oponente não muda posição no próximo turno."));

        // 1209 - Messenger of Peace (Lock Attack >= 1500)
        AddEffect("1209", c => Debug.Log("Messenger of Peace: Monstros >= 1500 ATK não atacam."));

        // 1211 - Metal Detector (Negate Continuous Trap)
        AddEffect("1211", c => Debug.Log("Metal Detector: Nega Trap Contínua."));

        // 1215 - Metal Reflect Slime (Trap Monster)
        AddEffect("1215", c => Debug.Log("Metal Reflect Slime: Vira monstro com 3000 DEF."));

        // 1216 - Metallizing Parasite - Lunatite (Union Protect)
        AddEffect("1216", c => Debug.Log("Lunatite: Union. Protege de magias."));

        // 1217 - Metalmorph (Trap Equip)
        AddEffect("1217", c => Debug.Log("Metalmorph: +300 ATK/DEF. Ganha metade do ATK do alvo ao atacar."));

        // 1218 - Metalsilver Armor (Redirect Target)
        AddEffect("1218", c => Debug.Log("Metalsilver Armor: Redireciona alvos para o equipado."));

        // 1219 - Metalzoa (SS from Deck)
        AddEffect("1219", c => Debug.Log("Metalzoa: Invoca do deck tributando Zoa com Metalmorph."));

        // 1220 - Metamorphosis (Tribute -> Fusion)
        AddEffect("1220", c => Debug.Log("Metamorphosis: Tributa 1 para invocar Fusão de mesmo nível."));

        // 1223 - Meteor of Destruction (Burn 1000)
        AddEffect("1223", c => { if(GameManager.Instance.opponentLP > 3000) Effect_DirectDamage(c, 1000); });

        // 1224 - Meteorain (Piercing for all)
        AddEffect("1224", c => Debug.Log("Meteorain: Dano perfurante para todos seus monstros."));

        // 1225 - Michizure (Destroy on destroy)
        AddEffect("1225", c => Debug.Log("Michizure: Destrói monstro quando o seu morre."));

        // 1226 - Micro Ray (DEF 0)
        AddEffect("1226", c => Debug.Log("Micro Ray: DEF do alvo vira 0."));

        // 1227 - Mid Shield Gardna (Flip Negate Spell)
        AddEffect("1227", c => Debug.Log("Mid Shield Gardna: Nega magia que dá alvo."));

        // 1232 - Millennium Scorpion (Gain ATK on kill)
        AddEffect("1232", c => Debug.Log("Millennium Scorpion: +500 ATK por destruição."));

        // 1234 - Milus Radiant (Field Earth +500, Wind -400)
        AddEffect("1234", c => Effect_Field(c, 500, -400, "", "Earth"));

        // 1235 - Minar (Burn on discard)
        AddEffect("1235", c => Debug.Log("Minar: 1000 dano se descartado pelo oponente."));

        // 1236 - Mind Control (Take Control)
        AddEffect("1236", Effect_BrainControl); // Reusa lógica similar

        // 1237 - Mind Crush (Hand Destruction)
        AddEffect("1237", c => Debug.Log("Mind Crush: Declara carta, verifica mão."));

        // 1238 - Mind Haxorz (Peek Hand/Set)
        AddEffect("1238", c => { Effect_PayLP(c, 500); Debug.Log("Mind Haxorz: Ver mão e setadas."); });

        // 1239 - Mind Wipe (Hand Refresh)
        AddEffect("1239", c => Debug.Log("Mind Wipe: Embaralha mão e compra (se <= 3)."));

        // 1240 - Mind on Air (Reveal Hand)
        AddEffect("1240", c => Debug.Log("Mind on Air: Mão do oponente revelada."));

        // 1241 - Mine Golem (Burn on destroy)
        AddEffect("1241", c => Debug.Log("Mine Golem: 500 dano ao morrer."));

        // 1242 - Minefield Eruption (Burn per Golem)
        AddEffect("1242", c => Debug.Log("Minefield Eruption: Dano por Mine Golem."));

        // 1244 - Minor Goblin Official (Burn Standby)
        AddEffect("1244", c => Debug.Log("Minor Goblin Official: 500 dano na Standby se LP <= 3000."));

        // 1245 - Miracle Dig (Recycle Banished)
        AddEffect("1245", c => Debug.Log("Miracle Dig: Retorna 3 banidas ao GY."));

        // 1246 - Miracle Fusion (Fusion Banish GY)
        AddEffect("1246", c => Debug.Log("Miracle Fusion: Fusão HERO banindo do GY."));

        // 1247 - Miracle Restoring (Revive DM/BB)
        AddEffect("1247", c => Debug.Log("Miracle Restoring: Remove contadores para reviver DM ou BB."));

        // 1248 - Mirage Dragon (No Traps Battle)
        AddEffect("1248", c => Debug.Log("Mirage Dragon: Sem Traps na Battle Phase."));

        // 1249 - Mirage Knight (Battle Logic)
        AddEffect("1249", c => Debug.Log("Mirage Knight: Ganha ATK do oponente."));

        // 1250 - Mirage of Nightmare (Draw/Discard)
        AddEffect("1250", c => Debug.Log("Mirage of Nightmare: Compra até 4, descarta na Standby."));

        // 1251 - Mirror Force (Destroy Attack Pos)
        AddEffect("1251", Effect_MirrorForce);

        // 1252 - Mirror Wall (Halve ATK)
        AddEffect("1252", c => Debug.Log("Mirror Wall: Corta ATK pela metade. Custo de manutenção."));

        // 1254 - Mispolymerization (Return Fusion)
        AddEffect("1254", c => Debug.Log("Mispolymerization: Retorna Fusão ao Extra."));

        // 1255 - Moai Interceptor Cannons (Flip Face-down)
        AddEffect("1255", c => Debug.Log("Moai Interceptor Cannons: Vira face-down 1x por turno."));

        // 1256 - Mobius the Frost Monarch (Destroy 2 S/T)
        AddEffect("1256", c => Debug.Log("Mobius: Destrói até 2 S/T ao tributar."));

        // 1257 - Moisture Creature (Destroy S/T)
        AddEffect("1257", c => Debug.Log("Moisture Creature: Destrói S/T se 3 tributos."));

        // 1259 - Mokey Mokey Smackdown (Buff)
        AddEffect("1259", c => Debug.Log("Mokey Mokey Smackdown: ATK 3000 se Fada destruída."));

        // 1261 - Molten Destruction (Field Fire +500/-400)
        AddEffect("1261", c => Effect_Field(c, 500, -400, "", "Fire"));

        // 1262 - Molten Zombie (Draw on SS)
        AddEffect("1262", c => Debug.Log("Molten Zombie: Compra 1 se invocado do GY."));

        // 1264 - Monk Fighter (No Battle Damage)
        AddEffect("1264", c => Debug.Log("Monk Fighter: Sem dano de batalha."));

        // 1266 - Monster Eye (Recycle Poly)
        AddEffect("1266", c => { Effect_PayLP(c, 1000); Debug.Log("Monster Eye: Recupera Polymerization."); });

        // 1267 - Monster Gate (Excavate SS)
        AddEffect("1267", c => Debug.Log("Monster Gate: Tributa 1, escava e invoca."));

        // 1268 - Monster Reborn (Revive) - Já existente, mas reforçando
        AddEffect("1268", c => Effect_Revive(c, true)); // true = qualquer GY

        // 1269 - Monster Recovery (Shuffle Hand/Field)
        AddEffect("1269", c => Debug.Log("Monster Recovery: Embaralha monstro e mão, compra nova mão."));

        // 1274 - Mooyan Curry (Gain 200 LP)
        AddEffect("1274", c => Effect_GainLP(c, 200));

        // 1275 - Morale Boost (Gain/Lose LP on Equip)
        AddEffect("1275", c => Debug.Log("Morale Boost: Ganha/Perde LP com Equips."));

        // 1277 - Morphing Jar (Hand Reset)
        AddEffect("1277", c => Debug.Log("Morphing Jar: Ambos descartam e compram 5."));

        // 1278 - Morphing Jar #2 (Deck Reset)
        AddEffect("1278", c => Debug.Log("Morphing Jar #2: Embaralha monstros e invoca novos."));

        // 1279 - Mother Grizzly (Search Water)
        AddEffect("1279", c => Effect_SearchDeck(c, "Water"));

        // 1280 - Mountain (Field Dragon/WingedBeast/Thunder +200)
        AddEffect("1280", c => { Effect_Field(c, 200, 200, "Dragon"); Effect_Field(c, 200, 200, "Winged Beast"); Effect_Field(c, 200, 200, "Thunder"); });

        // 1283 - Mucus Yolk (Direct Attack / Gain ATK)
        AddEffect("1283", c => Debug.Log("Mucus Yolk: Ataque direto, ganha 1000 ATK."));

        // 1284 - Mudora (Buff per Fairy)
        AddEffect("1284", c => Debug.Log("Mudora: Ganha ATK por Fadas no GY."));

        // 1285 - Muka Muka (Buff per Hand)
        AddEffect("1285", Effect_MukaMuka);

        // 1286 - Muko (Anti-Draw)
        AddEffect("1286", c => Debug.Log("Muko: Descarta cartas compradas."));

        // 1287 - Multiplication of Ants (Tokens)
        AddEffect("1287", c => Debug.Log("Multiplication of Ants: Tributa Inseto, cria 2 Tokens."));

        // 1288 - Multiply (Kuriboh Tokens)
        AddEffect("1288", c => Debug.Log("Multiply: Tributa Kuriboh, enche campo de Tokens."));

        // 1291 - Mushroom Man #2 (Burn Control)
        AddEffect("1291", c => Debug.Log("Mushroom Man #2: Dano na Standby, paga para passar controle."));

        // 1293 - Mustering of the Dark Scorpions (Swarm)
        AddEffect("1293", c => Debug.Log("Mustering: Invoca Dark Scorpions se tiver Don Zaloog."));

        // 1294 - My Body as a Shield (Negate Destroy)
        AddEffect("1294", c => { Effect_PayLP(c, 1500); Debug.Log("My Body as a Shield: Nega destruição."); });

        // 1295 - Mysterious Guard (Flip Bounce)
        AddEffect("1295", c => Debug.Log("Mysterious Guard: Retorna monstro ao topo do deck."));

        // 1296 - Mysterious Puppeteer (Gain LP on Summon)
        AddEffect("1296", c => Debug.Log("Mysterious Puppeteer: Ganha 500 LP por invocação."));

        // 1298 - Mystic Box (Destroy & Swap)
        AddEffect("1298", Effect_MysticBox);

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

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1501 - 1600)
        // =========================================================================================

        // 1502 - Red Gadget (Search Yellow Gadget)
        AddEffect("1502", c => Effect_SearchDeck(c, "Yellow Gadget"));

        // 1503 - Red Medicine (Gain 500 LP)
        AddEffect("1503", c => Effect_GainLP(c, 500));

        // 1507 - Reflect Bounder (Damage on attack)
        AddEffect("1507", c => Debug.Log("Reflect Bounder: Dano igual ATK do atacante."));

        // 1508 - Regenerating Mummy (Return to hand)
        AddEffect("1508", c => Debug.Log("Regenerating Mummy: Retorna para a mão se descartado."));

        // 1509 - Reinforcement of the Army (Search Warrior)
        AddEffect("1509", c => Effect_SearchDeck(c, "Warrior"));

        // 1510 - Reinforcements (Buff +500)
        AddEffect("1510", c => Effect_BuffStats(c, 500, 0));

        // 1511 - Release Restraint (Tribute Gearfried -> SS Swordmaster)
        AddEffect("1511", c => Debug.Log("Release Restraint: Invoca Gearfried Swordmaster."));

        // 1512 - Relieve Monster (Return -> SS Lv4)
        AddEffect("1512", c => Debug.Log("Relieve Monster: Retorna monstro, invoca Lv4 da mão."));

        // 1513 - Relinquished (Ritual Absorb)
        AddEffect("1513", c => Debug.Log("Relinquished: Absorve monstro do oponente."));

        // 1514 - Reload (Shuffle Hand Draw)
        AddEffect("1514", c => Debug.Log("Reload: Embaralha mão e compra o mesmo número."));

        // 1515 - Remove Brainwashing (Control Reset)
        AddEffect("1515", c => Debug.Log("Remove Brainwashing: Controle retorna aos donos."));

        // 1516 - Remove Trap (Destroy Face-up Trap)
        AddEffect("1516", c => Effect_DestroyType(c, "Trap")); // Simplificado

        // 1517 - Rescue Cat (Tribute -> SS 2 Beasts)
        AddEffect("1517", c => Debug.Log("Rescue Cat: Invoca 2 Bestas Lv3 ou menor."));

        // 1518 - Reshef the Dark Being (Discard Spell -> Control)
        AddEffect("1518", c => Debug.Log("Reshef: Descarta Magia para controlar monstro."));

        // 1520 - Restructer Revolution (Burn per hand card)
        AddEffect("1520", c => Debug.Log("Restructer Revolution: 200 dano por carta na mão do oponente."));

        // 1521 - Resurrection of Chakra (Ritual)
        AddEffect("1521", c => Debug.Log("Resurrection of Chakra: Ritual."));

        // 1522 - Return Zombie (Pay 500 Recycle)
        AddEffect("1522", c => { Effect_PayLP(c, 500); Debug.Log("Return Zombie: Retorna do GY para mão."); });

        // 1523 - Return from the Different Dimension (Pay half LP -> SS Banished)
        AddEffect("1523", c => Debug.Log("Return from DD: Invoca monstros banidos."));

        // 1524 - Return of the Doomed (Discard -> Recycle)
        AddEffect("1524", c => Debug.Log("Return of the Doomed: Recupera monstro destruído."));

        // 1525 - Reversal Quiz (Swap LP)
        AddEffect("1525", c => Debug.Log("Reversal Quiz: Troca LP com oponente."));

        // 1526 - Reverse Trap (Invert mods)
        AddEffect("1526", c => Debug.Log("Reverse Trap: Inverte aumentos/diminuições de ATK/DEF."));

        // 1527 - Revival Jam (Pay 1000 Revive)
        AddEffect("1527", c => Debug.Log("Revival Jam: Paga 1000 para reviver na Standby."));

        // 1528 - Revival of Dokurorider (Ritual)
        AddEffect("1528", c => Debug.Log("Revival of Dokurorider: Ritual."));

        // 1532 - Rigorous Reaver (Flip Discard)
        AddEffect("1532", c => Debug.Log("Rigorous Reaver: Ambos descartam."));

        // 1533 - Ring of Destruction (Destroy & Burn)
        AddEffect("1533", Effect_RingOfDestruction);

        // 1534 - Ring of Magnetism (Equip -500, Taunt)
        AddEffect("1534", c => Effect_Equip(c, -500, -500));

        // 1535 - Riryoku (Halve & Add)
        AddEffect("1535", c => Debug.Log("Riryoku: Rouba metade do ATK de um para outro."));

        // 1536 - Riryoku Field (Negate Spell)
        AddEffect("1536", c => Debug.Log("Riryoku Field: Nega magia que dá alvo."));

        // 1537 - Rising Air Current (Field Wind +500/-400)
        AddEffect("1537", c => Effect_Field(c, 500, -400, "", "Wind"));

        // 1538 - Rising Energy (Discard -> +1500)
        AddEffect("1538", c => Debug.Log("Rising Energy: Descarta 1 para +1500 ATK."));

        // 1539 - Rite of Spirit (Revive GK)
        AddEffect("1539", c => Debug.Log("Rite of Spirit: Revive Gravekeeper."));

        // 1540 - Ritual Weapon (Equip Ritual Lv6- +1500)
        AddEffect("1540", c => Effect_Equip(c, 1500, 1500)); // Simplificado

        // 1541 - Rivalry of Warlords (Type Lock)
        AddEffect("1541", c => Debug.Log("Rivalry of Warlords: Apenas 1 Tipo permitido."));

        // 1543 - Robbin' Goblin (Damage -> Discard)
        AddEffect("1543", c => Debug.Log("Robbin' Goblin: Descarte ao causar dano."));

        // 1544 - Robbin' Zombie (Damage -> Mill)
        AddEffect("1544", c => Debug.Log("Robbin' Zombie: Mill ao causar dano."));

        // 1548 - Roc from the Valley of Haze (Recycle)
        AddEffect("1548", c => Debug.Log("Roc: Volta ao deck se enviado da mão ao GY."));

        // 1549 - Rock Bombardment (Mill Rock -> 500 dmg)
        AddEffect("1549", c => { Effect_DirectDamage(c, 500); Debug.Log("Rock Bombardment: Envia Rock do deck ao GY."); });

        // 1553 - Rocket Jumper (Direct Attack)
        AddEffect("1553", c => Debug.Log("Rocket Jumper: Ataque direto se oponente só tem defesa."));

        // 1554 - Rocket Warrior (Battle protection)
        AddEffect("1554", c => Debug.Log("Rocket Warrior: Invulnerável na batalha, reduz ATK alvo."));

        // 1555 - Rod of Silence - Kay'est (Equip +500 DEF)
        AddEffect("1555", c => Effect_Equip(c, 0, 500));

        // 1556 - Rod of the Mind's Eye (Equip 1000 dmg)
        AddEffect("1556", c => Debug.Log("Rod of the Mind's Eye: Dano de batalha vira 1000."));

        // 1559 - Rope of Life (Discard -> Revive +800)
        AddEffect("1559", c => Debug.Log("Rope of Life: Descarta mão para reviver com +800 ATK."));

        // 1561 - Roulette Barrel (Dice Destroy)
        AddEffect("1561", c => Debug.Log("Roulette Barrel: Dado para destruir."));

        // 1562 - Royal Command (Negate Flip)
        AddEffect("1562", c => Debug.Log("Royal Command: Nega efeitos Flip."));

        // 1563 - Royal Decree (Negate Traps)
        AddEffect("1563", c => Debug.Log("Royal Decree: Nega todas as outras Traps."));

        // 1565 - Royal Keeper (Flip +300)
        AddEffect("1565", c => Debug.Log("Royal Keeper: Ganha 300 ATK/DEF ao virar."));

        // 1566 - Royal Magical Library (Counters -> Draw)
        AddEffect("1566", c => Debug.Log("Royal Magical Library: Remove 3 contadores para comprar."));

        // 1567 - Royal Oppression (Pay 800 Negate SS)
        AddEffect("1567", c => Debug.Log("Royal Oppression: Paga 800 para negar SS."));

        // 1568 - Royal Surrender (Negate Continuous Trap)
        AddEffect("1568", c => Debug.Log("Royal Surrender: Nega Trap Contínua."));

        // 1569 - Royal Tribute (Necrovalley Discard)
        AddEffect("1569", c => Debug.Log("Royal Tribute: Ambos descartam monstros (requer Necrovalley)."));

        // 1571 - Rush Recklessly (Target +700)
        AddEffect("1571", c => Effect_BuffStats(c, 700, 0));

        // 1572 - Ryu Kokki (Destroy Warrior/Spellcaster)
        AddEffect("1572", c => Debug.Log("Ryu Kokki: Destrói Warrior/Spellcaster após batalha."));

        // 1573 - Ryu Senshi (Pay 1000 Negate Trap)
        AddEffect("1573", c => Debug.Log("Ryu Senshi: Paga 1000 para negar Trap."));

        // 1575 - Ryu-Kishin Clown (Change Pos)
        AddEffect("1575", c => Debug.Log("Ryu-Kishin Clown: Muda posição de monstro ao ser invocado."));

        // 1578 - Sacred Crane (Draw on SS)
        AddEffect("1578", c => { GameManager.Instance.DrawCard(); Debug.Log("Sacred Crane: Compra 1."); });

        // 1579 - Sacred Phoenix of Nephthys (Revive/Nuke S/T)
        AddEffect("1579", c => Debug.Log("Sacred Phoenix: Renasce e destrói S/T."));

        // 1580 - Sage's Stone (SS DM)
        AddEffect("1580", c => Debug.Log("Sage's Stone: Invoca Dark Magician se tiver DMG."));

        // 1582 - Sakuretsu Armor (Destroy Attacker)
        AddEffect("1582", c => Debug.Log("Sakuretsu Armor: Destrói monstro atacante."));

        // 1583 - Salamandra (Equip Fire +700)
        AddEffect("1583", c => Effect_Equip(c, 700, 0, "Fire"));

        // 1584 - Salvage (Add 2 Water)
        AddEffect("1584", c => Debug.Log("Salvage: Recupera 2 Water 1500- ATK."));

        // 1585 - Sand Gambler (Coin Destroy)
        AddEffect("1585", c => Debug.Log("Sand Gambler: Moedas para destruir monstros."));

        // 1587 - Sanga of the Thunder (Zero ATK)
        AddEffect("1587", c => Debug.Log("Sanga: Zera ATK do atacante."));

        // 1588 - Sangan (Search 1500-)
        AddEffect("1588", c => Effect_SearchDeck(c, "Monster", "", 1500));

        // 1590 - Sasuke Samurai (Destroy Face-down)
        AddEffect("1590", c => Debug.Log("Sasuke Samurai: Destrói face-down antes do cálculo."));

        // 1591 - Sasuke Samurai #2 (Pay 800 No S/T)
        AddEffect("1591", c => { Effect_PayLP(c, 800); Debug.Log("Sasuke Samurai #2: Impede S/T."); });

        // 1592 - Sasuke Samurai #3 (Hand Fill)
        AddEffect("1592", c => Debug.Log("Sasuke Samurai #3: Oponente compra até ter 7 cartas."));

        // 1593 - Sasuke Samurai #4 (Coin Destroy)
        AddEffect("1593", c => Debug.Log("Sasuke Samurai #4: Moeda para destruir monstro."));

        // 1594 - Satellite Cannon (Gain ATK)
        AddEffect("1594", c => Debug.Log("Satellite Cannon: Ganha 1000 ATK por turno."));

        // 1595 - Scapegoat (SS 4 Tokens)
        AddEffect("1595", Effect_Scapegoat);

        // 1597 - Scroll of Bewitchment (Equip Change Attribute)
        AddEffect("1597", c => Debug.Log("Scroll of Bewitchment: Muda atributo."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1601 - 1700)
        // =========================================================================================

        // 1601 - Seal of the Ancients (Pay 1000, Reveal Face-down)
        AddEffect("1601", c => { Effect_PayLP(c, 1000); Debug.Log("Seal of the Ancients: Revela todas as cartas face-down do oponente."); });

        // 1603 - Sebek's Blessing (Quick-Play: Gain LP = Damage)
        AddEffect("1603", c => Debug.Log("Sebek's Blessing: Ganha LP igual ao dano direto causado."));

        // 1604 - Second Coin Toss (Redo coin toss)
        AddEffect("1604", c => Debug.Log("Second Coin Toss: Permite refazer um lançamento de moeda."));

        // 1605 - Second Goblin (Union)
        AddEffect("1605", c => Debug.Log("Second Goblin: Union para Giant Orc."));

        // 1606 - Secret Barrel (Burn per card)
        AddEffect("1606", Effect_SecretBarrel);

        // 1607 - Secret Pass to the Treasures (Direct attack < 1000)
        AddEffect("1607", c => Debug.Log("Secret Pass: Monstro com ATK <= 1000 pode atacar direto."));

        // 1610 - Self-Destruct Button (Draw game condition)
        AddEffect("1610", c => Debug.Log("Self-Destruct Button: Se diferença de LP > 7000, LP de ambos vira 0."));

        // 1612 - Senju of the Thousand Hands (Search Ritual Monster)
        AddEffect("1612", c => Effect_SearchDeck(c, "Ritual", "Monster"));

        // 1613 - Senri Eye (Pay 100, peek deck)
        AddEffect("1613", c => { Effect_PayLP(c, 100); Debug.Log("Senri Eye: Olha carta do topo do deck do oponente."); });

        // 1615 - Serial Spell (Discard hand, copy spell)
        AddEffect("1615", c => Debug.Log("Serial Spell: Descarta mão para copiar efeito de Normal Spell."));

        // 1618 - Serpentine Princess (Deck shuffle -> SS)
        AddEffect("1618", c => Debug.Log("Serpentine Princess: Se embaralhada no deck, invoca monstro Lv3 ou menor."));

        // 1619 - Servant of Catabolism (Direct attack)
        AddEffect("1619", c => Debug.Log("Servant of Catabolism: Pode atacar diretamente."));

        // 1620 - Seven Tools of the Bandit (Pay 1000 negate Trap)
        AddEffect("1620", c => { Effect_PayLP(c, 1000); Debug.Log("Seven Tools: Nega ativação de Armadilha e destrói."); });

        // 1621 - Shadow Ghoul (Gain ATK per GY)
        AddEffect("1621", c => Debug.Log("Shadow Ghoul: Ganha 100 ATK por monstro no seu GY."));

        // 1623 - Shadow Spell (Continuous Trap: -700 ATK, no attack/change pos)
        AddEffect("1623", c => Debug.Log("Shadow Spell: Alvo perde 700 ATK e não pode atacar/mudar posição."));

        // 1624 - Shadow Tamer (Flip: Control Fiend)
        AddEffect("1624", c => Debug.Log("Shadow Tamer: Toma controle de 1 Fiend do oponente."));

        // 1625 - Shadow of Eyes (Trigger: Set -> Attack)
        AddEffect("1625", c => Debug.Log("Shadow of Eyes: Força monstro Setado para Posição de Ataque."));

        // 1626 - Shadowknight Archfiend (Maintenance, Dice negate)
        AddEffect("1626", c => Debug.Log("Shadowknight Archfiend: Custo de manutenção e chance de negar alvo."));

        // 1627 - Shadowslayer (Direct attack if all def)
        AddEffect("1627", c => Debug.Log("Shadowslayer: Ataca direto se oponente só tiver monstros em defesa."));

        // 1629 - Share the Pain (Tribute 1, Opp tribute 1)
        AddEffect("1629", c => Debug.Log("Share the Pain: Você tributa 1, oponente tributa 1."));

        // 1630 - Shield & Sword (Swap ATK/DEF)
        AddEffect("1630", c => Debug.Log("Shield & Sword: Troca ATK e DEF de todos os monstros no campo."));

        // 1631 - Shield Crush (Destroy Def)
        AddEffect("1631", c => Debug.Log("Shield Crush: Destrói 1 monstro em Posição de Defesa."));

        // 1632 - Shien's Spy (Give control)
        AddEffect("1632", c => Debug.Log("Shien's Spy: Dá o controle de um monstro seu para o oponente."));

        // 1633 - Shift (Redirect target)
        AddEffect("1633", c => Debug.Log("Shift: Redireciona alvo de magia/ataque para outro monstro seu."));

        // 1634 - Shifting Shadows (Shuffle def monsters)
        AddEffect("1634", c => { Effect_PayLP(c, 300); Debug.Log("Shifting Shadows: Embaralha posições dos monstros em defesa."); });

        // 1635 - Shinato's Ark (Ritual Spell)
        AddEffect("1635", c => Debug.Log("Shinato's Ark: Ritual para Shinato."));

        // 1636 - Shinato, King of a Higher Plane (Burn on destroy)
        AddEffect("1636", c => Debug.Log("Shinato: Se destruir defesa, causa dano igual ao ATK original."));

        // 1637 - Shine Palace (Equip Light +700)
        AddEffect("1637", c => Effect_Equip(c, 700, 0, "", "Light"));

        // 1639 - Shining Angel (Floater Light)
        AddEffect("1639", c => Effect_SearchDeck(c, "Light"));

        // 1641 - Shooting Star Bow - Ceal (Equip -1000, Direct Attack)
        AddEffect("1641", c => { Effect_Equip(c, -1000, 0); Debug.Log("Ceal: Equipado pode atacar diretamente."); });

        // 1643 - Shrink (Halve ATK)
        AddEffect("1643", c => Debug.Log("Shrink: ATK original do alvo cai pela metade até o fim do turno."));

        // 1644 - Silent Doom (Revive Normal Def)
        AddEffect("1644", c => Debug.Log("Silent Doom: Invoca Normal Monster do GY em defesa."));

        // 1645 - Silent Swordsman LV3 (Negate Spell target, LV up)
        AddEffect("1645", c => Effect_LevelUp(c, "1646")); // LV5

        // 1646 - Silent Swordsman LV5 (Immune Spell, LV up)
        AddEffect("1646", c => Effect_LevelUp(c, "1647")); // LV7

        // 1647 - Silent Swordsman LV7 (Negate all Spells)
        AddEffect("1647", c => Debug.Log("Silent Swordsman LV7: Nega todas as magias no campo."));

        // 1648 - Silpheed (SS Banish Wind, Hand discard)
        AddEffect("1648", c => Debug.Log("Silpheed: SS banindo Wind. Oponente descarta se destruído."));

        // 1649 - Silver Bow and Arrow (Equip Fairy +300)
        AddEffect("1649", c => Effect_Equip(c, 300, 300, "Fairy"));

        // 1651 - Sinister Serpent (Return to hand)
        AddEffect("1651", c => Debug.Log("Sinister Serpent: Retorna do GY para a mão na Standby Phase."));

        // 1652 - Sixth Sense (Dice draw/mill)
        AddEffect("1652", c => Debug.Log("Sixth Sense: Declara 2 números, rola dado. Acertou=Draw, Errou=Mill."));

        // 1653 - Skelengel (Flip Draw)
        AddEffect("1653", c => { GameManager.Instance.DrawCard(); Debug.Log("Skelengel: Compra 1 carta."); });

        // 1655 - Skill Drain (Negate face-up effects)
        AddEffect("1655", c => { Effect_PayLP(c, 1000); Debug.Log("Skill Drain: Nega efeitos de monstros face-up."); });

        // 1656 - Skilled Dark Magician (Counters -> SS DM)
        AddEffect("1656", c => Debug.Log("Skilled Dark Magician: 3 contadores -> Invoca Dark Magician."));

        // 1657 - Skilled White Magician (Counters -> SS BB)
        AddEffect("1657", c => Debug.Log("Skilled White Magician: 3 contadores -> Invoca Buster Blader."));

        // 1658 - Skull Archfiend of Lightning (Maintenance, Dice negate)
        AddEffect("1658", c => Debug.Log("Skull Archfiend: Manutenção e chance de negar alvo."));

        // 1659 - Skull Dice (Dice debuff)
        AddEffect("1659", c => Debug.Log("Skull Dice: Rola dado para reduzir ATK/DEF do oponente."));

        // 1661 - Skull Guardian (Ritual)
        AddEffect("1661", c => Debug.Log("Skull Guardian: Ritual."));

        // 1662 - Skull Invitation (Burn on GY)
        AddEffect("1662", c => Debug.Log("Skull Invitation: 300 dano por cada carta enviada ao GY."));

        // 1664 - Skull Knight #2 (Tribute -> SS copy)
        AddEffect("1664", c => Debug.Log("Skull Knight #2: Invoca cópia se tributado para Fiend."));

        // 1665 - Skull Lair (Banish GY -> Destroy)
        AddEffect("1665", c => Debug.Log("Skull Lair: Bane monstros do GY para destruir monstro face-up."));

        // 1670 - Skull-Mark Ladybug (Heal on GY)
        AddEffect("1670", c => { Effect_GainLP(c, 1000); Debug.Log("Skull-Mark Ladybug: Ganha 1000 LP."); });

        // 1674 - Skyscraper (Field: HERO +1000 on attack)
        AddEffect("1674", c => Debug.Log("Skyscraper: E-HERO ganha 1000 ATK ao atacar monstro mais forte."));

        // 1675 - Slate Warrior (Flip buff, debuff killer)
        AddEffect("1675", c => Debug.Log("Slate Warrior: Flip +500. Quem destruir perde 500."));

        // 1679 - Smashing Ground (Destroy highest DEF)
        AddEffect("1679", c => Debug.Log("Smashing Ground: Destrói monstro do oponente com maior DEF."));

        // 1680 - Smoke Grenade of the Thief (Equip: Look hand discard)
        AddEffect("1680", c => Debug.Log("Smoke Grenade: Olha mão do oponente e descarta 1."));

        // 1681 - Snake Fang (Debuff DEF)
        AddEffect("1681", c => Debug.Log("Snake Fang: Reduz DEF em 500."));

        // 1683 - Snatch Steal (Equip Control, Opp heal)
        AddEffect("1683", c => { Effect_Equip(c, 0, 0); Effect_ChangeControl(c, false); });

        // 1684 - Sogen (Field Warrior/Beast-Warrior +200)
        AddEffect("1684", c => { Effect_Field(c, 200, 200, "Warrior"); Effect_Field(c, 200, 200, "Beast-Warrior"); });

        // 1686 - Solar Flare Dragon (Burn, Protect)
        AddEffect("1686", c => Debug.Log("Solar Flare Dragon: 500 dano na End Phase. Não pode ser atacado se tiver outro Pyro."));

        // 1687 - Solar Ray (Burn per Light)
        AddEffect("1687", c => Debug.Log("Solar Ray: 600 dano por cada monstro LIGHT face-up."));

        // 1688 - Solemn Judgment (Counter: Pay half negate)
        AddEffect("1688", c => Debug.Log("Solemn Judgment: Paga metade do LP para negar Invocação/Magia/Armadilha."));

        // 1689 - Solemn Wishes (Heal on draw)
        AddEffect("1689", c => Debug.Log("Solemn Wishes: Ganha 500 LP cada vez que compra carta."));

        // 1691 - Solomon's Lawbook (Skip Standby)
        AddEffect("1691", c => Debug.Log("Solomon's Lawbook: Pula a próxima Standby Phase."));

        // 1692 - Sonic Bird (Search Ritual Spell)
        AddEffect("1692", c => Effect_SearchDeck(c, "Ritual", "Spell"));

        // 1694 - Sonic Jammer (Flip: No Spells)
        AddEffect("1694", c => Debug.Log("Sonic Jammer: Oponente não pode ativar Magias no próximo turno."));

        // 1696 - Sorcerer of Dark Magic (Negate Traps)
        AddEffect("1696", c => Debug.Log("Sorcerer of Dark Magic: Nega ativação de Armadilhas."));

        // 1698 - Soul Absorption (Heal on banish)
        AddEffect("1698", c => Debug.Log("Soul Absorption: Ganha 500 LP por cada carta banida."));

        // 1699 - Soul Demolition (Pay 500 Banish GY)
        AddEffect("1699", c => Debug.Log("Soul Demolition: Paga 500 para banir monstro do GY do oponente."));

        // 1700 - Soul Exchange (Tribute Opp monster)
        AddEffect("1700", c => Debug.Log("Soul Exchange: Seleciona monstro do oponente para tributar no lugar do seu."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1701 - 1800)
        // =========================================================================================

        // 1702 - Soul Release (Banish 5 GY)
        AddEffect("1702", c => Debug.Log("Soul Release: Banir até 5 do GY."));

        // 1703 - Soul Resurrection (Revive Normal Defense)
        AddEffect("1703", c => Debug.Log("Soul Resurrection: Reviver Normal em Defesa."));

        // 1704 - Soul Reversal (Recycle Flip)
        AddEffect("1704", c => Debug.Log("Soul Reversal: Retornar Flip do GY ao Deck."));

        // 1705 - Soul Rope (Pay 1000 SS Lv4)
        AddEffect("1705", c => Debug.Log("Soul Rope: Paga 1000, SS Lv4 do Deck."));

        // 1706 - Soul Taker (Destroy & Heal Opp)
        AddEffect("1706", c => Debug.Log("Soul Taker: Destrói monstro, oponente ganha 1000 LP."));

        // 1708 - Soul of Purity and Light (SS Condition / Debuff)
        AddEffect("1708", c => Debug.Log("Soul of Purity and Light: SS banindo 2 LIGHT. Debuff oponente."));

        // 1709 - Soul of the Pure (Gain 800)
        AddEffect("1709", c => Effect_GainLP(c, 800));

        // 1710 - Soul-Absorbing Bone Tower (Mill on Zombie SS)
        AddEffect("1710", c => Debug.Log("Bone Tower: Mill 2 quando Zumbi é invocado."));

        // 1714 - Spark Blaster (Pos Change)
        AddEffect("1714", c => Debug.Log("Spark Blaster: Equip Sparkman. Muda posição."));

        // 1715 - Sparks (Burn 200)
        AddEffect("1715", c => Effect_DirectDamage(c, 200));

        // 1716 - Spatial Collapse (Limit 5)
        AddEffect("1716", c => Debug.Log("Spatial Collapse: Limite de 5 cartas."));

        // 1717 - Spear Cretin (Flip Mutual Revive)
        AddEffect("1717", c => Debug.Log("Spear Cretin: Flip, ambos invocam do GY."));

        // 1718 - Spear Dragon (Piercing / Defense)
        AddEffect("1718", c => Debug.Log("Spear Dragon: Perfurante, vira defesa."));

        // 1719 - Special Hurricane (Destroy SS)
        AddEffect("1719", c => Debug.Log("Special Hurricane: Destrói monstros SS."));

        // 1720 - Spell Absorption (Gain LP on Spell)
        AddEffect("1720", c => Debug.Log("Spell Absorption: Ganha 500 LP por magia."));

        // 1721 - Spell Canceller (Negate Spells)
        AddEffect("1721", c => Debug.Log("Spell Canceller: Nega magias."));

        // 1722 - Spell Economics (No LP Cost)
        AddEffect("1722", c => Debug.Log("Spell Economics: Sem custo de LP para magias."));

        // 1723 - Spell Purification (Destroy Continuous Spells)
        AddEffect("1723", c => Debug.Log("Spell Purification: Destrói Continuous Spells."));

        // 1724 - Spell Reproduction (Recycle Spell)
        AddEffect("1724", c => Debug.Log("Spell Reproduction: Recupera magia."));

        // 1725 - Spell Shattering Arrow (Destroy Face-up Spells)
        AddEffect("1725", c => Debug.Log("Spell Shattering Arrow: Destrói face-up Spells e dano."));

        // 1726 - Spell Shield Type-8 (Negate Spell)
        AddEffect("1726", c => Debug.Log("Spell Shield Type-8: Nega magia."));

        // 1727 - Spell Vanishing (Negate & Banish)
        AddEffect("1727", c => Debug.Log("Spell Vanishing: Nega e bane cópias."));

        // 1728 - Spell of Pain (Redirect Damage)
        AddEffect("1728", c => Debug.Log("Spell of Pain: Redireciona dano de efeito."));

        // 1729 - Spell-Stopping Statute (Negate Continuous Spell)
        AddEffect("1729", c => Debug.Log("Spell-Stopping Statute: Nega Continuous Spell."));

        // 1730 - Spellbinding Circle (Lock)
        AddEffect("1730", c => Debug.Log("Spellbinding Circle: Prende monstro."));

        // 1731 - Spellbook Organization (Reorder)
        AddEffect("1731", c => Debug.Log("Spellbook Organization: Reordena topo."));

        // 1733 - Sphinx Teleia (SS Condition / Burn)
        AddEffect("1733", c => Debug.Log("Sphinx Teleia: SS especial, dano em defesa."));

        // 1737 - Spiral Spear Strike (Piercing Gaia)
        AddEffect("1737", c => Debug.Log("Spiral Spear Strike: Perfurante para Gaia."));

        // 1738 - Spirit Barrier (No Battle Damage)
        AddEffect("1738", c => Debug.Log("Spirit Barrier: Sem dano de batalha se tiver monstro."));

        // 1739 - Spirit Caller (Flip SS Normal)
        AddEffect("1739", c => Debug.Log("Spirit Caller: Flip SS Normal Lv3-."));

        // 1740 - Spirit Elimination (Banish Field)
        AddEffect("1740", c => Debug.Log("Spirit Elimination: Banir do campo em vez do GY."));

        // 1745 - Spirit Reaper (Indestructible / Discard)
        AddEffect("1745", c => Debug.Log("Spirit Reaper: Indestrutível batalha, descarte."));

        // 1746 - Spirit Ryu (Discard Dragon Buff)
        AddEffect("1746", c => Debug.Log("Spirit Ryu: Descarte Dragão para ATK."));

        // 1747 - Spirit of Flames (SS Banish Fire)
        AddEffect("1747", c => Debug.Log("Spirit of Flames: SS banindo FIRE."));

        // 1749 - Spirit of the Breeze (Gain LP)
        AddEffect("1749", c => Debug.Log("Spirit of the Breeze: Ganha LP."));

        // 1752 - Spirit of the Pharaoh (SS Zombies)
        AddEffect("1752", c => Debug.Log("Spirit of the Pharaoh: SS Zumbis."));

        // 1753 - Spirit of the Pot of Greed (Draw Extra)
        AddEffect("1753", c => Debug.Log("Spirit of the Pot: Compra extra."));

        // 1755 - Spirit's Invitation (Bounce)
        AddEffect("1755", c => Debug.Log("Spirit's Invitation: Bounce."));

        // 1756 - Spiritual Earth Art - Kurogane (Swap Earth)
        AddEffect("1756", c => Debug.Log("Kurogane: Troca Earth."));

        // 1757 - Spiritual Energy Settle Machine (Keep Spirits)
        AddEffect("1757", c => Debug.Log("Spiritual Energy: Mantém Spirits."));

        // 1758 - Spiritual Fire Art - Kurenai (Burn)
        AddEffect("1758", c => Debug.Log("Kurenai: Tributa Fire para dano."));

        // 1759 - Spiritual Water Art - Aoi (Hand Destruction)
        AddEffect("1759", c => Debug.Log("Aoi: Hand destruction."));

        // 1760 - Spiritual Wind Art - Miyabi (Spin)
        AddEffect("1760", c => Debug.Log("Miyabi: Spin."));

        // 1761 - Spiritualism (Bounce S/T)
        AddEffect("1761", c => Debug.Log("Spiritualism: Bounce S/T."));

        // 1762 - Spring of Rebirth (Gain LP on Bounce)
        AddEffect("1762", c => Debug.Log("Spring of Rebirth: Ganha LP por bounce."));

        // 1764 - Stamping Destruction (Destroy S/T Burn)
        AddEffect("1764", c => Debug.Log("Stamping Destruction: Destrói S/T e dano."));

        // 1765 - Star Boy (Field Water +500 Fire -400)
        AddEffect("1765", c => Effect_Field(c, 500, -400, "", "Water"));

        // 1766 - Statue of the Wicked (Token)
        AddEffect("1766", c => Debug.Log("Statue of the Wicked: Token."));

        // 1767 - Staunch Defender (Forced Attack)
        AddEffect("1767", c => Debug.Log("Staunch Defender: Redireciona ataques."));

        // 1768 - Stealth Bird (Burn / Flip Down)
        AddEffect("1768", c => Debug.Log("Stealth Bird: Dano 1000, flip down."));

        // 1770 - Steamroid (Battle Stats)
        AddEffect("1770", c => Debug.Log("Steamroid: Modifica ATK na batalha."));

        // 1773 - Steel Scorpion (Destroy non-Machine)
        AddEffect("1773", c => Debug.Log("Steel Scorpion: Destrói não-máquina."));

        // 1774 - Steel Shell (Equip Water +400/-200)
        AddEffect("1774", c => Effect_Equip(c, 400, -200, "", "Water"));

        // 1775 - Stim-Pack (Equip +700 / Decay)
        AddEffect("1775", c => Debug.Log("Stim-Pack: +700 ATK, perde 200."));

        // 1780 - Stone Statue of the Aztecs (Double Battle Damage)
        AddEffect("1780", c => Debug.Log("Aztecs: Dano de batalha dobrado."));

        // 1781 - Stop Defense (Change to Attack)
        AddEffect("1781", c => Debug.Log("Stop Defense: Muda para ataque."));

        // 1782 - Stray Lambs (Tokens)
        AddEffect("1782", c => Debug.Log("Stray Lambs: 2 Tokens."));

        // 1783 - Strike Ninja (Dodge)
        AddEffect("1783", c => Debug.Log("Strike Ninja: Dodge banindo 2 Dark."));

        // 1784 - Stronghold the Moving Fortress (Trap Monster)
        AddEffect("1784", c => Debug.Log("Stronghold: Trap Monster."));

        // 1786 - Stumbling (Defense on Summon)
        AddEffect("1786", c => Debug.Log("Stumbling: Invocados viram defesa."));

        // 1788 - Suijin (Zero ATK)
        AddEffect("1788", c => Debug.Log("Suijin: Zera ATK."));

        // 1790 - Summoner Monk (Discard Spell SS)
        AddEffect("1790", c => Debug.Log("Summoner Monk: Descarta Spell invoca Lv4."));

        // 1791 - Summoner of Illusions (Flip SS Fusion)
        AddEffect("1791", c => Debug.Log("Summoner of Illusions: Flip tributa invoca Fusão."));

        // 1792 - Super Rejuvenation (Draw Dragons)
        AddEffect("1792", c => Debug.Log("Super Rejuvenation: Compra por Dragões."));

        // 1795 - Super War-Lion (Ritual)
        AddEffect("1795", c => Debug.Log("Super War-Lion: Ritual."));

        // 1796 - Supply (Recycle Fusion Material)
        AddEffect("1796", c => Debug.Log("Supply: Recupera materiais."));

        // 1798 - Susa Soldier (Halve Damage)
        AddEffect("1798", c => Debug.Log("Susa Soldier: Dano cortado."));

        // 1799 - Swamp Battleguard (Buff)
        AddEffect("1799", c => Debug.Log("Swamp Battleguard: Buff por Lava Battleguard."));

        // 1800 - Swarm of Locusts (Destroy S/T / Flip Down)
        AddEffect("1800", c => Debug.Log("Swarm of Locusts: Destrói S/T, flip down."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1801 - 1900)
        // =========================================================================================

        // 1801 - Swarm of Scarabs (Flip Destroy Monster)
        AddEffect("1801", c => Effect_FlipDestroy(c, TargetType.Monster));

        // 1802 - Swift Gaia the Fierce Knight (NS No Tribute)
        AddEffect("1802", c => Debug.Log("Swift Gaia: NS sem tributo se for a única carta."));

        // 1804 - Sword Hunter (Equip destroyed)
        AddEffect("1804", c => Debug.Log("Sword Hunter: Equipa monstros destruídos."));

        // 1806 - Sword of Dark Destruction (Equip Dark +400/-200)
        AddEffect("1806", c => Effect_Equip(c, 400, -200, "", "Dark"));

        // 1807 - Sword of Deep-Seated (Equip +500/500, Recycle)
        AddEffect("1807", c => Effect_Equip(c, 500, 500));

        // 1808 - Sword of Dragon's Soul (Equip Warrior +700, Destroy Dragon)
        AddEffect("1808", c => Effect_Equip(c, 700, 0, "Warrior"));

        // 1809 - Sword of the Soul-Eater (Equip Normal Lv3-, Buff)
        AddEffect("1809", c => Debug.Log("Sword of the Soul-Eater: Tributa Normais para ganhar ATK."));

        // 1810 - Swords of Concealing Light (Face-down Defense)
        AddEffect("1810", c => Debug.Log("Swords of Concealing Light: Oponente face-down por 2 turnos."));

        // 1811 - Swords of Revealing Light (Face-up, No Attack)
        AddEffect("1811", c => Debug.Log("Swords of Revealing Light: Revela e impede ataque por 3 turnos."));

        // 1812 - Swordsman from a Distant Land (Destroy monster after battle)
        AddEffect("1812", c => Debug.Log("Swordsman from a Distant Land: Destrói monstro em 5 turnos."));

        // 1816 - System Down (Pay 1000 Banish Machines)
        AddEffect("1816", c => { Effect_PayLP(c, 1000); Debug.Log("System Down: Bane Machines do oponente."); });

        // 1817 - T.A.D.P.O.L.E. (Search copies)
        AddEffect("1817", c => Effect_SearchDeck(c, "T.A.D.P.O.L.E."));

        // 1818 - Tactical Espionage Expert (No Traps on Summon)
        AddEffect("1818", c => Debug.Log("Tactical Espionage Expert: Sem traps na invocação."));

        // 1819 - Tailor of the Fickle (Switch Equip)
        AddEffect("1819", c => Debug.Log("Tailor of the Fickle: Troca alvo de equipamento."));

        // 1820 - Tainted Wisdom (Shuffle Deck)
        AddEffect("1820", c => Debug.Log("Tainted Wisdom: Embaralha deck ao mudar para defesa."));

        // 1823 - Talisman of Spell Sealing (Lock Spells)
        AddEffect("1823", c => Debug.Log("Talisman of Spell Sealing: Bloqueia Magias (requer Sealmaster)."));

        // 1824 - Talisman of Trap Sealing (Lock Traps)
        AddEffect("1824", c => Debug.Log("Talisman of Trap Sealing: Bloqueia Armadilhas (requer Sealmaster)."));

        // 1827 - Taunt (Force Attack Target)
        AddEffect("1827", c => Debug.Log("Taunt: Força ataque em monstro específico."));

        // 1829 - Temple of the Kings (Trap turn set, SS Serket)
        AddEffect("1829", c => Debug.Log("Temple of the Kings: Ativa Trap no turno que baixa."));

        // 1833 - Terraforming (Search Field Spell)
        AddEffect("1833", c => Effect_SearchDeck(c, "Field", "Spell"));

        // 1834 - Terrorking Archfiend (Negate, Maintenance)
        AddEffect("1834", c => Debug.Log("Terrorking Archfiend: Nega alvo, custo de manutenção."));

        // 1836 - Teva (No Attack Next Turn)
        AddEffect("1836", c => Debug.Log("Teva: Oponente não ataca no próximo turno."));

        // 1839 - The A. Forces (Buff Warriors)
        AddEffect("1839", c => Debug.Log("The A. Forces: +200 ATK por Warrior/Spellcaster."));

        // 1840 - The Agent of Creation - Venus (Pay 500 SS Shine Ball)
        AddEffect("1840", c => { Effect_PayLP(c, 500); Debug.Log("Venus: Invoca Shine Ball."); });

        // 1841 - The Agent of Force - Mars (Immune Spell, ATK=LP Diff)
        AddEffect("1841", c => Debug.Log("Mars: Imune a Magia, ATK = Diferença de LP."));

        // 1842 - The Agent of Judgment - Saturn (Tribute Burn)
        AddEffect("1842", c => Debug.Log("Saturn: Tributa para causar dano igual diferença de LP."));

        // 1843 - The Agent of Wisdom - Mercury (Draw if hand empty)
        AddEffect("1843", c => Debug.Log("Mercury: Compra 1 se mão vazia na End Phase."));

        // 1846 - The Big March of Animals (Buff Beasts)
        AddEffect("1846", c => Debug.Log("The Big March of Animals: +200 ATK por Besta."));

        // 1847 - The Bistro Butcher (Draw 2 for Opp)
        AddEffect("1847", c => Debug.Log("The Bistro Butcher: Oponente compra 2 ao tomar dano."));

        // 1848 - The Cheerful Coffin (Discard)
        AddEffect("1848", c => Debug.Log("The Cheerful Coffin: Descarta até 3 monstros."));

        // 1849 - The Creator Incarnate (Tribute SS Creator)
        AddEffect("1849", c => Debug.Log("The Creator Incarnate: Invoca The Creator da mão."));

        // 1850 - The Dark - Hex-Sealed Fusion (Fusion Sub)
        AddEffect("1850", c => Debug.Log("The Dark - Hex-Sealed Fusion: Substituto de fusão."));

        // 1851 - The Dark Door (One Attack)
        AddEffect("1851", c => Debug.Log("The Dark Door: Apenas 1 ataque por turno."));

        // 1853 - The Dragon's Bead (Discard Negate Trap)
        AddEffect("1853", c => Debug.Log("The Dragon's Bead: Descarta para negar Trap que alvo Dragão."));

        // 1856 - The Earth - Hex-Sealed Fusion (Fusion Sub)
        AddEffect("1856", c => Debug.Log("The Earth - Hex-Sealed Fusion: Substituto de fusão."));

        // 1857 - The Emperor's Holiday (Negate Equips)
        AddEffect("1857", c => Debug.Log("The Emperor's Holiday: Nega cartas de Equipamento."));

        // 1858 - The End of Anubis (Negate GY)
        AddEffect("1858", c => Debug.Log("The End of Anubis: Nega efeitos no GY."));

        // 1859 - The Eye of Truth (Reveal Hand, Heal)
        AddEffect("1859", c => Debug.Log("The Eye of Truth: Revela mão do oponente, cura se tiver Magia."));

        // 1860 - The Fiend Megacyber (SS Condition)
        AddEffect("1860", c => Debug.Log("The Fiend Megacyber: SS se oponente tiver mais monstros."));

        // 1861 - The First Sarcophagus (SS Spirit of Pharaoh)
        AddEffect("1861", c => Debug.Log("The First Sarcophagus: Inicia contagem para Spirit of Pharaoh."));

        // 1862 - The Flute of Summoning Dragon (SS 2 Dragons)
        AddEffect("1862", c => Debug.Log("The Flute of Summoning Dragon: Invoca até 2 Dragões (requer Lord of D.)."));

        // 1863 - The Forceful Sentry (Look Hand, Return to Deck)
        AddEffect("1863", c => Debug.Log("The Forceful Sentry: Retorna carta da mão do oponente ao deck."));

        // 1864 - The Forgiving Maiden (Tribute Recycle)
        AddEffect("1864", c => Debug.Log("The Forgiving Maiden: Recupera monstro destruído."));

        // 1866 - The Graveyard in the Fourth Dimension (Recycle LV)
        AddEffect("1866", c => Debug.Log("The Graveyard in the Fourth Dimension: Recicla 2 monstros LV."));

        // 1868 - The Hunter with 7 Weapons (Declare Type +1000)
        AddEffect("1868", c => Debug.Log("The Hunter with 7 Weapons: +1000 ATK contra tipo declarado."));

        // 1870 - The Immortal of Thunder (Flip +3000 LP, Lose 5000)
        AddEffect("1870", c => { Effect_GainLP(c, 3000); Debug.Log("The Immortal of Thunder: Ganha 3000 LP (perde 5000 depois)."); });

        // 1871 - The Inexperienced Spy (Look Hand)
        AddEffect("1871", c => Debug.Log("The Inexperienced Spy: Olha 1 carta da mão do oponente."));

        // 1873 - The Kick Man (Equip on SS)
        AddEffect("1873", c => Debug.Log("The Kick Man: Equipa magia do GY ao ser invocado."));

        // 1874 - The Last Warrior from Another Planet (Lock Summons)
        AddEffect("1874", c => Debug.Log("The Last Warrior: Impede invocações."));

        // 1875 - The Law of the Normal (Reset, Destroy)
        AddEffect("1875", c => Debug.Log("The Law of the Normal: Destrói tudo, descarta mãos."));

        // 1876 - The Legendary Fisherman (Immune Umi)
        AddEffect("1876", c => Debug.Log("The Legendary Fisherman: Imune a magia e ataque com Umi."));

        // 1877 - The Light - Hex-Sealed Fusion (Fusion Sub)
        AddEffect("1877", c => Debug.Log("The Light - Hex-Sealed Fusion: Substituto de fusão."));

        // 1878 - The Little Swordsman of Aile (Tribute +700)
        AddEffect("1878", c => Debug.Log("The Little Swordsman of Aile: Tributa para ganhar ATK."));

        // 1879 - The Mask of Remnants (Shuffle/Equip)
        AddEffect("1879", c => Debug.Log("The Mask of Remnants: Equipa e controla (via Des Gardius)."));

        // 1880 - The Masked Beast (Ritual)
        AddEffect("1880", c => Debug.Log("The Masked Beast: Ritual."));

        // 1883 - The Puppet Magic of Dark Ruler (Banish -> SS Fiend)
        AddEffect("1883", c => Debug.Log("The Puppet Magic: Bane monstros para invocar Fiend do GY."));

        // 1884 - The Regulation of Tribe (Type Lock)
        AddEffect("1884", c => Debug.Log("The Regulation of Tribe: Tipo declarado não ataca."));

        // 1885 - The Reliable Guardian (Quick-Play +700 DEF)
        AddEffect("1885", c => Effect_BuffStats(c, 0, 700));

        // 1886 - The Rock Spirit (SS Banish Earth)
        AddEffect("1886", c => Debug.Log("The Rock Spirit: SS banindo Earth."));

        // 1887 - The Sanctuary in the Sky (No Fairy Damage)
        AddEffect("1887", c => Debug.Log("The Sanctuary in the Sky: Sem dano de batalha com Fadas."));

        // 1889 - The Secret of the Bandit (Discard on Damage)
        AddEffect("1889", c => Debug.Log("The Secret of the Bandit: Descarte ao causar dano."));

        // 1890 - The Selection (Pay 1000 Negate Summon)
        AddEffect("1890", c => { Effect_PayLP(c, 1000); Debug.Log("The Selection: Nega invocação de mesmo tipo."); });

        // 1892 - The Shallow Grave (SS Face-down)
        AddEffect("1892", c => Debug.Log("The Shallow Grave: Ambos invocam monstro face-down do GY."));

        // 1894 - The Spell Absorbing Life (Flip Face-up, Heal)
        AddEffect("1894", c => Debug.Log("The Spell Absorbing Life: Vira face-up e cura."));

        // 1896 - The Stern Mystic (Flip Reveal)
        AddEffect("1896", c => Debug.Log("The Stern Mystic: Revela cartas face-down."));

        // 1898 - The Thing in the Crater (SS Pyro)
        AddEffect("1898", c => Debug.Log("The Thing in the Crater: Invoca Pyro da mão ao ser destruído."));

        // 1900 - The Tricky (SS Discard)
        AddEffect("1900", c => Debug.Log("The Tricky: SS descartando 1 carta."));

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1901 - 2000)
        // =========================================================================================

        // 1901 - The Trojan Horse (2 Tributes Earth)
        AddEffect("1901", c => Debug.Log("The Trojan Horse: Vale por 2 tributos para Earth."));

        // 1902 - The Unfriendly Amazon (Maintenance)
        AddEffect("1902", c => Debug.Log("The Unfriendly Amazon: Tributa monstro na Standby ou destrói."));

        // 1903 - The Unhappy Girl (Lock Attack/Pos)
        AddEffect("1903", c => Debug.Log("The Unhappy Girl: Trava monstro que batalhou."));

        // 1904 - The Unhappy Maiden (End Battle Phase)
        AddEffect("1904", c => Debug.Log("The Unhappy Maiden: Encerra Battle Phase ao ser destruída."));

        // 1906 - The Warrior Returning Alive (Recycle Warrior)
        AddEffect("1906", c => Debug.Log("The Warrior Returning Alive: Recupera Warrior do GY."));

        // 1907 - The Wicked Dreadroot (Halve Stats)
        AddEffect("1907", c => Debug.Log("The Wicked Dreadroot: Corta ATK/DEF de todos pela metade."));

        // 1908 - The Wicked Worm Beast (Return to Hand)
        AddEffect("1908", c => Debug.Log("The Wicked Worm Beast: Retorna para mão na End Phase."));

        // 1909 - Theban Nightmare (Buff if empty)
        AddEffect("1909", c => Debug.Log("Theban Nightmare: +1500 ATK se mão/S-T vazios."));

        // 1910 - Theinen the Great Sphinx (SS +3000)
        AddEffect("1910", c => Debug.Log("Theinen: SS especial com 6500 ATK."));

        // 1911 - Thestalos the Firestorm Monarch (Discard Burn)
        AddEffect("1911", c => Debug.Log("Thestalos: Descarta carta da mão do oponente e causa dano."));

        // 1913 - Thousand Energy (Buff Lv2 Normal)
        AddEffect("1913", c => Debug.Log("Thousand Energy: +1000 ATK para Normais Lv2."));

        // 1914 - Thousand Knives (Destroy if DM)
        AddEffect("1914", c => Debug.Log("Thousand Knives: Destrói monstro se controlar Dark Magician."));

        // 1915 - Thousand Needles (Destroy Attacker)
        AddEffect("1915", c => Debug.Log("Thousand Needles: Destrói atacante se DEF > ATK."));

        // 1917 - Thousand-Eyes Restrict (Absorb, Lock)
        AddEffect("1917", c => Debug.Log("Thousand-Eyes Restrict: Absorve monstro, impede ataques."));

        // 1918 - Threatening Roar (No Attack)
        AddEffect("1918", c => Debug.Log("Threatening Roar: Oponente não pode atacar neste turno."));

        // 1921 - Throwstone Unit (Tribute Warrior -> Destroy)
        AddEffect("1921", c => Debug.Log("Throwstone Unit: Tributa Warrior para destruir monstro com DEF <= ATK."));

        // 1922 - Thunder Crash (Destroy Own -> Burn)
        AddEffect("1922", c => Debug.Log("Thunder Crash: Destrói seus monstros, 300 dano por cada."));

        // 1923 - Thunder Dragon (Discard -> Add 2)
        AddEffect("1923", c => Debug.Log("Thunder Dragon: Descarta para buscar até 2 cópias."));

        // 1925 - Thunder Nyan Nyan (Destroy if non-Light)
        AddEffect("1925", c => Debug.Log("Thunder Nyan Nyan: Destruída se houver monstro não-LIGHT."));

        // 1926 - Thunder of Ruler (Skip Battle Phase)
        AddEffect("1926", c => Debug.Log("Thunder of Ruler: Pula Battle Phase do oponente."));

        // 1928 - Time Machine (Revive Destroyed)
        AddEffect("1928", c => Debug.Log("Time Machine: Revive monstro destruído na mesma posição."));

        // 1929 - Time Seal (Skip Draw)
        AddEffect("1929", c => Debug.Log("Time Seal: Pula Draw Phase do oponente."));

        // 1930 - Time Wizard (Coin Destroy)
        AddEffect("1930", c => Debug.Log("Time Wizard: Moeda para destruir monstros ou tomar dano."));

        // 1931 - Timeater (Skip Main 1)
        AddEffect("1931", c => Debug.Log("Timeater: Oponente pula Main Phase 1."));

        // 1932 - Timidity (Protect Set S/T)
        AddEffect("1932", c => Debug.Log("Timidity: Impede destruição de S/T setadas."));

        // 1935 - Token Feastevil (Destroy Tokens Burn)
        AddEffect("1935", c => Debug.Log("Token Feastevil: Destrói Tokens e causa dano."));

        // 1936 - Token Thanksgiving (Destroy Tokens Heal)
        AddEffect("1936", c => Debug.Log("Token Thanksgiving: Destrói Tokens e cura."));

        // 1937 - Toll (Pay to Attack)
        AddEffect("1937", c => Debug.Log("Toll: Paga 500 LP para atacar."));

        // 1941 - Toon Cannon Soldier (Toon, Tribute Burn)
        AddEffect("1941", c => Effect_TributeToBurn(c, 1, 500));

        // 1942 - Toon Dark Magician Girl (Toon, Direct)
        AddEffect("1942", c => Debug.Log("Toon DMG: Ataque direto, SS especial."));

        // 1943 - Toon Defense (Redirect to Direct)
        AddEffect("1943", c => Debug.Log("Toon Defense: Redireciona ataque para direto."));

        // 1944 - Toon Gemini Elf (Toon, Discard)
        AddEffect("1944", c => Debug.Log("Toon Gemini Elf: Descarte ao causar dano."));

        // 1945 - Toon Goblin Attack Force (Toon, Defense)
        AddEffect("1945", c => Debug.Log("Toon Goblin: Vira defesa após ataque."));

        // 1946 - Toon Masked Sorcerer (Toon, Draw)
        AddEffect("1946", c => Debug.Log("Toon Masked Sorcerer: Compra 1 ao causar dano."));

        // 1947 - Toon Mermaid (Toon, SS)
        AddEffect("1947", c => Debug.Log("Toon Mermaid: SS se Toon World."));

        // 1948 - Toon Summoned Skull (Toon, SS)
        AddEffect("1948", c => Debug.Log("Toon Summoned Skull: SS tributando."));

        // 1949 - Toon Table of Contents (Search Toon)
        AddEffect("1949", c => Effect_SearchDeck(c, "Toon"));

        // 1950 - Toon World (Pay 1000)
        AddEffect("1950", c => { Effect_PayLP(c, 1000); Debug.Log("Toon World ativado."); });

        // 1952 - Tornado Bird (Flip Bounce 2 S/T)
        AddEffect("1952", c => Debug.Log("Tornado Bird: Retorna 2 S/T para a mão."));

        // 1953 - Tornado Wall (No Damage with Umi)
        AddEffect("1953", c => Debug.Log("Tornado Wall: Sem dano de batalha se Umi estiver em campo."));

        // 1954 - Torpedo Fish (Immune with Umi)
        AddEffect("1954", c => Debug.Log("Torpedo Fish: Imune a magias com Umi."));

        // 1955 - Torrential Tribute (Destroy All on Summon)
        AddEffect("1955", c => Debug.Log("Torrential Tribute: Destrói todos os monstros na invocação."));

        // 1956 - Total Defense Shogun (Attack in Defense)
        AddEffect("1956", c => Debug.Log("Total Defense Shogun: Pode atacar em posição de defesa."));

        // 1957 - Tower of Babel (Counters -> Burn)
        AddEffect("1957", c => Debug.Log("Tower of Babel: 4º contador causa 3000 de dano."));

        // 1958 - Tragedy (Destroy Defense on Pos Change)
        AddEffect("1958", c => Debug.Log("Tragedy: Destrói monstros em defesa ao mudar posição."));

        // 1960 - Transcendent Wings (SS Winged Kuriboh LV10)
        AddEffect("1960", c => Debug.Log("Transcendent Wings: Evolui Winged Kuriboh."));

        // 1961 - Trap Dustshoot (Hand Shuffle)
        AddEffect("1961", c => Debug.Log("Trap Dustshoot: Retorna carta da mão do oponente ao deck."));

        // 1962 - Trap Hole (Destroy Summon 1000+)
        AddEffect("1962", c => Debug.Log("Trap Hole: Destrói monstro invocado com 1000+ ATK."));

        // 1963 - Trap Jammer (Negate Battle Trap)
        AddEffect("1963", c => Debug.Log("Trap Jammer: Nega Trap na Battle Phase."));

        // 1964 - Trap Master (Flip Destroy Trap)
        AddEffect("1964", c => Effect_FlipDestroy(c, TargetType.Trap));

        // 1965 - Trap of Board Eraser (Negate Burn, Discard)
        AddEffect("1965", c => Debug.Log("Trap of Board Eraser: Nega dano de efeito, oponente descarta."));

        // 1966 - Trap of Darkness (Copy Trap)
        AddEffect("1966", c => Debug.Log("Trap of Darkness: Copia efeito de Trap Normal do GY."));

        // 1967 - Tremendous Fire (Burn 1000/500)
        AddEffect("1967", c => { Effect_DirectDamage(c, 1000); Effect_PayLP(c, 500); });

        // 1971 - Triangle Ecstasy Spark (Buff Harpie Sisters)
        AddEffect("1971", c => Debug.Log("Triangle Ecstasy Spark: Harpie Sisters 2700 ATK, sem Traps."));

        // 1972 - Triangle Power (Buff Lv1 Normal)
        AddEffect("1972", c => Debug.Log("Triangle Power: +2000 ATK para Normais Lv1."));

        // 1973 - Tribe-Infecting Virus (Destroy Type)
        AddEffect("1973", c => Debug.Log("Tribe-Infecting Virus: Descarta para destruir Tipo declarado."));

        // 1974 - Tribute Doll (SS Lv7)
        AddEffect("1974", c => Debug.Log("Tribute Doll: Tributa para invocar Lv7 da mão."));

        // 1975 - Tribute to the Doomed (Discard Destroy)
        AddEffect("1975", c => Debug.Log("Tribute to the Doomed: Descarta para destruir monstro."));

        // 1976 - Tricky Spell 4 (Tokens)
        AddEffect("1976", c => Debug.Log("Tricky Spell 4: Tributa Tricky para invocar Tokens."));

        // 1978 - Troop Dragon (Float)
        AddEffect("1978", c => Effect_SearchDeck(c, "Troop Dragon"));

        // 1979 - Tsukuyomi (Flip Face-down, Return)
        AddEffect("1979", c => Debug.Log("Tsukuyomi: Vira monstro face-down. Retorna para mão."));

        // 1981 - Turtle Oath (Ritual Spell)
        AddEffect("1981", c => Debug.Log("Turtle Oath: Ritual."));

        // 1985 - Tutan Mask (Negate Target Zombie)
        AddEffect("1985", c => Debug.Log("Tutan Mask: Nega S/T que alvo Zumbi."));

        // 1988 - Twin Swords of Flashing Light - Tryce (Equip -500, 2 Attacks)
        AddEffect("1988", c => { Effect_Equip(c, -500, 0); Debug.Log("Tryce: Ataque duplo."); });

        // 1989 - Twin-Headed Behemoth (Revive 1000)
        AddEffect("1989", c => Debug.Log("Twin-Headed Behemoth: Renasce com 1000 ATK/DEF."));

        // 1992 - Twin-Headed Wolf (Negate Flip)
        AddEffect("1992", c => Debug.Log("Twin-Headed Wolf: Nega efeitos Flip em batalha."));

        // 1993 - Twinheaded Beast (Double Attack)
        AddEffect("1993", c => Debug.Log("Twinheaded Beast: Ataque duplo."));

        // 1994 - Two Thousand Needles (Destroy Attacker)
        AddEffect("1994", c => Debug.Log("Two Thousand Needles: Destrói atacante se ATK < DEF."));

        // 1996 - Two-Man Cell Battle (SS Normal End Phase)
        AddEffect("1996", c => Debug.Log("Two-Man Cell Battle: Invoca Normal Lv4 na End Phase."));

        // 1998 - Two-Pronged Attack (Destroy 2 Own, 1 Opp)
        AddEffect("1998", c => Debug.Log("Two-Pronged Attack: Destrói 2 seus e 1 do oponente."));

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

    }

    void AddEffect(string id, System.Action<CardDisplay> effect)
    {
        if (!effectDatabase.ContainsKey(id))
        {
            effectDatabase.Add(id, effect);
        }
        else
        {
            Debug.LogWarning($"Tentativa de registrar efeito duplicado para ID: {id}");
        }
    }

    // Método principal chamado pelo GameManager/ChainManager
    public bool ExecuteCardEffect(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return false;

        string id = card.CurrentCardData.id;

        if (effectDatabase.ContainsKey(id))
        {
            Debug.Log($"Executando efeito da carta: {card.CurrentCardData.name} (ID: {id})");
            effectDatabase[id].Invoke(card);
            return true;
        }
        else
        {
            // Debug.LogWarning($"Efeito não implementado para: {card.CurrentCardData.name} (ID: {id})");
            return false;
        }
    }

    // --- IMPLEMENTAÇÃO DOS EFEITOS ---

    // 0031 - Airknight Parshath
    void Effect_AirknightParshath(CardDisplay source)
    {
        // Efeito 1: Dano Perfurante (Passivo, tratado no BattleManager)
        // Efeito 2: Draw 1 quando causa dano (Gatilho)
        Debug.Log("Airknight Parshath: Efeito ativado (Draw 1).");
        if (source.isPlayerCard)
            GameManager.Instance.DrawCard();
        else
            GameManager.Instance.DrawOpponentCard();
    }

    // 0050 - Ameba
    void Effect_Ameba(CardDisplay source)
    {
        // Este efeito é um Trigger que acontece quando o controle muda.
        // Se chamado manualmente, simula o dano ao oponente do dono atual.
        Debug.Log("Ameba: Controle mudou! Causando 2000 de dano ao oponente.");
        GameManager.Instance.DamageOpponent(2000);
    }

    void Effect_TributeToDraw(CardDisplay source, int tributes, int draws)
    {
        if (SummonManager.Instance.HasEnoughTributes(tributes, source.isPlayerCard))
        {
            Debug.Log($"Tributando {tributes} para comprar {draws}.");
            // TODO: Consumir tributos
            for(int i=0; i<draws; i++) GameManager.Instance.DrawCard(true);
        }
    }

    void Effect_FlipDestroyLevel(CardDisplay source, int level)
    {
        bool isPlayer = source.isPlayerCard;
        Transform[] targetZones = isPlayer ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
        
        foreach(Transform zone in targetZones)
        {
            if(zone.childCount > 0)
            {
                CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                if(target != null && target.CurrentCardData.level == level)
                {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, !isPlayer);
                    Destroy(target.gameObject);
                }
            }
        }
    }

    void Effect_GainLP(CardDisplay source, int amount)
    {
        // Implementar cura no GameManager
        Debug.Log($"Ganhou {amount} LP.");
    }

    void Effect_Equip(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "")
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (target) => {
                    if (!target.isOnField || !target.CurrentCardData.type.Contains("Monster")) return false;
                    if (!string.IsNullOrEmpty(requiredRace) && target.CurrentCardData.race != requiredRace) return false;
                    if (!string.IsNullOrEmpty(requiredAttribute) && target.CurrentCardData.attribute != requiredAttribute) return false;
                    return true;
                },
                (target) => 
                {
                    Debug.Log($"{source.CurrentCardData.name} equipada em {target.CurrentCardData.name}");
                    target.ModifyStats(atkBonus, defBonus);
                }
            );
        }
    }

    void Effect_TurnSet(CardDisplay source)
    {
        if (source.position == CardDisplay.BattlePosition.Attack)
            source.ChangePosition();
        source.ShowBack();
    }

    void Effect_SearchDeck(CardDisplay source, string type)
    {
        Debug.Log($"Procurando {type} no deck...");
    }

    void Effect_SearchDeckTop(CardDisplay source, string type, string subType = "")
    {
        Debug.Log($"Procurando {type}/{subType} para colocar no topo do deck.");
    }

    void Effect_FeatherOfThePhoenix(CardDisplay source)
    {
        Debug.Log("Feather of the Phoenix: Selecione carta no cemitério.");
    }

    void Effect_Field(CardDisplay source, int atkBonus, int defBonus, string requiredRace = "", string requiredAttribute = "", int levelMod = 0)
    {
        Debug.Log($"Campo ativado: {source.CurrentCardData.name}. Buff: {atkBonus}/{defBonus}");
        // Lógica de aplicar buff em área
    }

    void Effect_RevealSetCard(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped && !t.isPlayerCard,
                (t) => Debug.Log($"Revelada: {t.CurrentCardData.name}")
            );
        }
    }

    void Effect_ARivalAppears(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => Debug.Log($"Rival Appears: Invocando monstro Nível {t.CurrentCardData.level} da mão.")
            );
        }
    }

    void Effect_WingbeatOfGiantDragon(CardDisplay source)
    {
        Debug.Log("Wingbeat: Retornar Dragão e destruir S/T.");
    }

    void Effect_AbyssSoldier(CardDisplay source)
    {
        Debug.Log("Abyss Soldier: Descarte Water para retornar carta.");
    }

    void Effect_PayLP(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamagePlayer(amount);
    }

    void Effect_DestroyType(CardDisplay source, string type)
    {
        Debug.Log($"Destruindo todos os monstros tipo {type}...");
    }

    void Effect_AcidTrapHole(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped && t.position == CardDisplay.BattlePosition.Defense,
                (t) => {
                    t.RevealCard();
                    if (t.CurrentCardData.def <= 2000)
                    {
                        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                        GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                        Destroy(t.gameObject);
                    }
                }
            );
        }
    }

    void Effect_Agido(CardDisplay source)
    {
        int roll = Random.Range(1, 7);
        Debug.Log($"Agido rolou: {roll}. Invocando Fada Nível {roll} do GY.");
    }

    void Effect_AltarForTribute(CardDisplay source)
    {
        Debug.Log("Altar for Tribute: Tributar 1 para ganhar LP.");
    }

    void Effect_TributeToBurn(CardDisplay source, int tributes, int damage, string race = "")
    {
        Debug.Log($"Tributando {tributes} {race} para causar {damage} dano.");
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_AmazonessSpellcaster(CardDisplay source)
    {
        Debug.Log("Amazoness Spellcaster: Trocar ATK.");
    }

    void Effect_AncientLamp(CardDisplay source)
    {
        Debug.Log("Ancient Lamp: Invocando La Jinn.");
    }

    void Effect_AncientTelescope(CardDisplay source)
    {
        Debug.Log("Ancient Telescope: Vendo topo do deck do oponente.");
    }

    void Effect_Ante(CardDisplay source)
    {
        Debug.Log("Ante: Minigame de revelar cartas.");
    }

    void Effect_AquaSpirit(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Aqua Spirit: Posição de {t.CurrentCardData.name} alterada.");
                }
            );
        }
    }

    void Effect_ArcaneArcher(CardDisplay source)
    {
        Debug.Log("Arcane Archer: Tributar Planta para destruir S/T.");
    }

    void Effect_ArchfiendsOath(CardDisplay source)
    {
        GameManager.Instance.DamagePlayer(500);
        Debug.Log("Archfiend's Oath: Declare uma carta.");
    }

    void Effect_ArchfiendsRoar(CardDisplay source)
    {
        GameManager.Instance.DamagePlayer(500);
        Debug.Log("Archfiend's Roar: Selecione Archfiend no GY.");
    }

    void Effect_ArchlordZerato(CardDisplay source)
    {
        Debug.Log("Archlord Zerato: Descarte LIGHT para destruir monstros.");
    }

    void Effect_LevelUp(CardDisplay source, string nextLevelId)
    {
        Debug.Log($"Level Up! Invocando {nextLevelId}.");
    }

    void Effect_ArmedDragonLV5(CardDisplay source)
    {
        Debug.Log("Armed Dragon LV5: Descarte monstro para destruir alvo.");
    }

    void Effect_ArmedDragonLV7(CardDisplay source)
    {
        Debug.Log("Armed Dragon LV7: Descarte monstro para destruir todos <= ATK.");
    }

    // --- STAPLES ---

    void Effect_DarkHole(CardDisplay source)
    {
        DestroyAllMonsters(true, true);
    }

    void Effect_Raigeki(CardDisplay source)
    {
        DestroyAllMonsters(true, false);
    }

    void DestroyAllMonsters(bool targetOpponent, bool targetPlayer)
    {
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            if (targetPlayer) CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toDestroy);
            if (targetOpponent) CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toDestroy);
        }
        foreach (var monster in toDestroy)
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
            GameManager.Instance.SendToGraveyard(monster.CurrentCardData, monster.isPlayerCard);
            Destroy(monster.gameObject);
        }
    }

    void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        foreach (var zone in zones)
        {
            if (zone != null && zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    void DestroyCards(List<CardDisplay> cards, bool isPlayerSource)
    {
        foreach (var card in cards)
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
            GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
            Destroy(card.gameObject);
        }
    }

    void CollectMonsters(Transform[] zones, List<CardDisplay> list)
    {
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    void Effect_PotOfGreed(CardDisplay source)
    {
        if (source.isPlayerCard) { GameManager.Instance.DrawCard(true); GameManager.Instance.DrawCard(true); }
        else { GameManager.Instance.DrawOpponentCard(); GameManager.Instance.DrawOpponentCard(); }
    }

    void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    void Effect_MST(CardDisplay source)
    {
        Debug.Log("MST ativado. Seleção de alvo pendente.");
    }

    void Effect_FlipDestroy(CardDisplay source, TargetType type)
    {
        Debug.Log($"Efeito FLIP ativado: {source.CurrentCardData.name}");
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (target) => IsValidTarget(target, type),
                (target) => 
                {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(target);
                    GameManager.Instance.SendToGraveyard(target.CurrentCardData, target.isPlayerCard);
                    Destroy(target.gameObject);
                }
            );
        }
    }

    void Effect_FlipReturn(CardDisplay source, TargetType type)
    {
        Debug.Log($"Efeito FLIP (Return) ativado: {source.CurrentCardData.name}");
    }

    bool IsValidTarget(CardDisplay target, TargetType type)
    {
        if (!target.isOnField) return false;
        switch (type)
        {
            case TargetType.Monster: return target.CurrentCardData.type.Contains("Monster");
            case TargetType.Spell: return target.CurrentCardData.type.Contains("Spell");
            case TargetType.Trap: return target.CurrentCardData.type.Contains("Trap");
            case TargetType.Any: return true;
            default: return false;
        }
    }

    void Effect_BlockAttack(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack && !t.isPlayerCard,
                (t) => {
                    t.ChangePosition();
                    Debug.Log($"Block Attack: {t.CurrentCardData.name} mudou para defesa.");
                }
            );
        }
    }

    void Effect_BlowbackDragon(CardDisplay source)
    {
        // 3 moedas, se 2 caras, destrói alvo
        int heads = 0;
        for(int i=0; i<3; i++) if(Random.value > 0.5f) heads++;
        
        if (heads >= 2)
        {
            Debug.Log($"Blowback Dragon: {heads} caras! Destruindo alvo.");
            // Lógica de seleção de alvo e destruição
        }
        else
        {
            Debug.Log($"Blowback Dragon: {heads} caras. Falhou.");
        }
    }

    void Effect_BookOfLife(CardDisplay source)
    {
        Debug.Log("Book of Life: Invocar Zumbi e banir monstro do oponente.");
    }

    void Effect_BookOfMoon(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack, // Simplificação
                (t) => {
                    t.ChangePosition(); // Vira defesa
                    t.ShowBack(); // Face-down
                    Debug.Log($"Book of Moon: {t.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    void Effect_BookOfTaiyou(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped,
                (t) => {
                    t.RevealCard();
                    t.ChangePosition(); // Vira ataque
                    Debug.Log($"Book of Taiyou: {t.CurrentCardData.name} virado para cima.");
                }
            );
        }
    }

    void Effect_ChangeControl(CardDisplay source, bool returnAtEndPhase)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && t.isPlayerCard != source.isPlayerCard,
                (t) => GameManager.Instance.SwitchControl(t)
            );
        }
    }
    
    void Effect_BurstStream(CardDisplay source)
    {
        // Verifica se controla Blue-Eyes
        // Se sim, destrói todos os monstros do oponente
        Debug.Log("Burst Stream: Destruir monstros do oponente (se tiver Blue-Eyes).");
        DestroyAllMonsters(true, false);
    }

    void Effect_HarpiesFeatherDuster(CardDisplay source)
    {
        Debug.Log("Harpie's Feather Duster: Destruir S/T do oponente.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentSpellZones : GameManager.Instance.duelFieldUI.playerSpellZones;
            CollectCards(zones, toDestroy);
            Transform fieldZone = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentFieldSpell : GameManager.Instance.duelFieldUI.playerFieldSpell;
            CollectCards(new Transform[] { fieldZone }, toDestroy);
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_HeavyStorm(CardDisplay source)
    {
        Debug.Log("Heavy Storm: Destruir todas as S/T.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, toDestroy);
            CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, toDestroy);
            CollectCards(new Transform[] { GameManager.Instance.duelFieldUI.playerFieldSpell, GameManager.Instance.duelFieldUI.opponentFieldSpell }, toDestroy);
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_MirrorForce(CardDisplay source)
    {
        Debug.Log("Mirror Force: Destruir monstros em ataque do oponente.");
        List<CardDisplay> toDestroy = new List<CardDisplay>();
        if (GameManager.Instance.duelFieldUI != null)
        {
            // Se o jogador ativou, destrói os do oponente
            Transform[] zones = source.isPlayerCard ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;
            
            foreach (var zone in zones)
            {
                if (zone.childCount > 0)
                {
                    var monster = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (monster != null && monster.position == CardDisplay.BattlePosition.Attack)
                    {
                        toDestroy.Add(monster);
                    }
                }
            }
        }
        DestroyCards(toDestroy, source.isPlayerCard);
    }

    void Effect_RingOfDestruction(CardDisplay source)
    {
        Debug.Log("Ring of Destruction ativado.");
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped, // Face-up monsters
                (t) => {
                    int damage = t.CurrentCardData.atk;
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(t);
                    GameManager.Instance.SendToGraveyard(t.CurrentCardData, t.isPlayerCard);
                    Destroy(t.gameObject);
                    
                    // Damage both players
                    GameManager.Instance.DamagePlayer(damage);
                    GameManager.Instance.DamageOpponent(damage);
                    Debug.Log($"Ring of Destruction: {damage} de dano para ambos.");
                }
            );
        }
    }

    void Effect_MagicCylinder(CardDisplay source)
    {
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            CardDisplay attacker = BattleManager.Instance.currentAttacker;
            int damage = attacker.CurrentCardData.atk;
            
            Debug.Log($"Magic Cylinder: Negando ataque de {attacker.CurrentCardData.name} e causando {damage} de dano.");
            
            // Nega o ataque (simulado aqui causando o dano)
            if (source.isPlayerCard) GameManager.Instance.DamageOpponent(damage);
            else GameManager.Instance.DamagePlayer(damage);
            // TODO: Cancelar ataque no BattleManager
        }
    }

    void Effect_Megamorph(CardDisplay source)
    {
        Effect_Equip(source, 0, 0); // Placeholder visual
        Debug.Log("Megamorph ativado: Se LP < Oponente -> Dobra ATK. Se LP > Oponente -> Metade ATK.");
    }

    void Effect_MagePower(CardDisplay source)
    {
        Effect_Equip(source, 0, 0); // Placeholder visual
        Debug.Log("Mage Power: Ganha 500 ATK/DEF para cada Spell/Trap que você controla.");
    }

    void Effect_MukaMuka(CardDisplay source)
    {
        Debug.Log("Muka Muka: Ganha 300 ATK/DEF para cada carta na sua mão.");
        // Em um sistema real, isso se inscreveria em eventos de mudança de mão
    }

    void Effect_Scapegoat(CardDisplay source)
    {
        // Invoca 4 Tokens
        for(int i=0; i<4; i++)
            GameManager.Instance.SpawnToken(source.isPlayerCard, 0, 0, "Sheep Token");
    }

    void Effect_Revive(CardDisplay source, bool anyGraveyard)
    {
        List<CardData> targets = new List<CardData>();
        targets.AddRange(GameManager.Instance.GetPlayerGraveyard().FindAll(c => c.type.Contains("Monster")));
        if (anyGraveyard)
            targets.AddRange(GameManager.Instance.GetOpponentGraveyard().FindAll(c => c.type.Contains("Monster")));

        GameManager.Instance.OpenCardSelection(targets, "Selecione monstro para reviver", (selected) => {
            GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
            Debug.Log($"Revivendo {selected.name}");
        });
    }

    void Effect_MysticBox(CardDisplay source)
    {
        Debug.Log("Mystic Box: Selecione 1 monstro do oponente para destruir e 1 seu para dar o controle.");
        // Lógica de seleção dupla e troca de controle
    }

    void Effect_CallOfTheHaunted(CardDisplay source)
    {
        Debug.Log("Call of the Haunted: Invocar do GY em ataque.");
    }

    void Effect_CardDestruction(CardDisplay source)
    {
        Debug.Log("Card Destruction: Ambos descartam mão e compram a mesma quantidade.");
    }

    void Effect_Ceasefire(CardDisplay source)
    {
        Debug.Log("Ceasefire: Virar todos para cima e causar dano por efeito.");
    }

    void Effect_CemetaryBomb(CardDisplay source)
    {
        int damage = GameManager.Instance.opponentGraveyardDisplay.pileData.Count * 100;
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_ChangeOfHeart(CardDisplay source)
    {
        Debug.Log("Change of Heart: Controlar monstro até o fim do turno.");
    }

    void Effect_ChaosEmperorDragon(CardDisplay source)
    {
        Effect_PayLP(source, 1000);
        Debug.Log("Chaos Emperor Dragon: Enviar tudo para o GY e causar dano.");
    }

    void Effect_ChaosEnd(CardDisplay source)
    {
        if (GameManager.Instance.playerRemoved.Count >= 7)
        {
            DestroyAllMonsters(true, true);
        }
    }

    void Effect_ChaosSorcerer(CardDisplay source)
    {
        Debug.Log("Chaos Sorcerer: Banir monstro face-up.");
    }

    void Effect_AssaultOnGHQ(CardDisplay source)
    {
        Debug.Log("Assault on GHQ: Destruir monstro para millar oponente.");
    }

    void Effect_AutonomousActionUnit(CardDisplay source)
    {
        Effect_PayLP(source, 1500);
        Debug.Log("Autonomous Action Unit: Invocar do GY do oponente.");
    }

    void Effect_BackToSquareOne(CardDisplay source)
    {
        Debug.Log("Back to Square One: Descartar para retornar monstro ao topo do deck.");
    }

    void Effect_BarrelDragon(CardDisplay source)
    {
        // 3 moedas, 2 caras = destruir
        int heads = 0;
        for(int i=0; i<3; i++) if(Random.value > 0.5f) heads++;
        if (heads >= 2) Debug.Log("Barrel Dragon: Sucesso! Destruir alvo.");
    }

    void Effect_Bazoo(CardDisplay source)
    {
        Debug.Log("Bazoo: Banir do GY para ganhar ATK.");
    }

    void Effect_BuffStats(CardDisplay source, int atk, int def)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => t.ModifyStats(atk, def)
            );
        }
    }

    void Effect_CompulsoryEvacuationDevice(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => {
                    Debug.Log($"Compulsory Evacuation Device: {t.CurrentCardData.name} retornado para a mão.");
                    // Lógica de retornar para a mão (simplificada: destruir visualmente)
                    // Em um sistema completo, moveria para a lista da mão e instanciaria o prefab lá.
                    Destroy(t.gameObject); 
                }
            );
        }
    }

    void Effect_EnemyController(CardDisplay source)
    {
        Debug.Log("Enemy Controller: Escolha 1 efeito (Mudar Posição ou Controlar).");
    }
}
