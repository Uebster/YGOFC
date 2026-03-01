using UnityEngine;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 0001 - 0500)
    // =========================================================================================

    void Effect_0001_3HumpLacooda(CardDisplay source)
    {
        // Debug.Log("3-Hump Lacooda (Tribute 2 to Draw 3)");
        Effect_TributeToDraw(source, 2, 3);
    }

    void Effect_0003_4StarredLadybugOfDoom(CardDisplay source)
    {
        // Debug.Log("4-Starred Ladybug of Doom (FLIP: Destroy opponent Level 4 monsters)");
        Effect_FlipDestroyLevel(source, 4);
    }

    void Effect_0004_7(CardDisplay source)
    {
        // Debug.Log("7 (Spell: Gain 700 LP)");
        Effect_GainLP(source, 700);
    }

    void Effect_0006_7Completed(CardDisplay source)
    {
        // Debug.Log("7 Completed (Equip: +700 ATK or DEF to Machine)");
        Effect_Equip(source, 700, 700, "Machine");
    }

    void Effect_0007_8ClawsScorpion(CardDisplay source)
    {
        // Debug.Log("8-Claws Scorpion (Set self)");
        Effect_TurnSet(source);
    }

    void Effect_0008_ACatOfIllOmen(CardDisplay source)
    {
        // Debug.Log("A Cat of Ill Omen (FLIP: Search Trap)");
        Effect_SearchDeck(source, "Trap");
    }

    void Effect_0009_ADealWithDarkRuler(CardDisplay source)
    {
        Debug.Log("A Deal with Dark Ruler: Requer condição de nível 8 destruído.");
    }

    void Effect_0010_AFeatherOfThePhoenix(CardDisplay source)
    {
        Debug.Log("Feather of the Phoenix: Selecione carta no cemitério.");
    }

    void Effect_0011_AFeintPlan(CardDisplay source)
    {
        Debug.Log("A Feint Plan ativado: Monstros face-down não podem ser atacados este turno.");
    }

    void Effect_0012_AHeroEmerges(CardDisplay source)
    {
        Debug.Log("A Hero Emerges: Oponente escolhe carta da sua mão para invocar.");
    }

    void Effect_0013_ALegendaryOcean(CardDisplay source)
    {
        // Debug.Log("A Legendary Ocean (Field: Water +200/200, Level -1)");
        Effect_Field(source, 200, 200, "Aqua", "", -1);
    }

    void Effect_0014_AManWithWdjat(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped && !t.isPlayerCard,
                (t) => Debug.Log($"Revelada: {t.CurrentCardData.name}")
            );
        }
    }

    void Effect_0015_ARivalAppears(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster") && !t.isFlipped,
                (t) => Debug.Log($"Rival Appears: Invocando monstro Nível {t.CurrentCardData.level} da mão.")
            );
        }
    }

    void Effect_0016_AWingbeatOfGiantDragon(CardDisplay source)
    {
        Debug.Log("Wingbeat: Retornar Dragão e destruir S/T.");
    }

    void Effect_0017_ATeamTrapDisposalUnit(CardDisplay source)
    {
        Debug.Log("A-Team: Efeito Rápido de negar armadilha.");
    }

    void Effect_0018_AbsoluteEnd(CardDisplay source)
    {
        Debug.Log("Absolute End: Ataques se tornam diretos.");
    }

    void Effect_0019_AbsorbingKidFromTheSky(CardDisplay source)
    {
        Debug.Log("Absorbing Kid: Ganha LP igual ao nível do monstro destruído x 300.");
    }

    void Effect_0021_AbyssSoldier(CardDisplay source)
    {
        Debug.Log("Abyss Soldier: Descarte Water para retornar carta.");
    }

    void Effect_0022_AbyssalDesignator(CardDisplay source)
    {
        // Debug.Log("Abyssal Designator (Pay 1000, Declare Type/Attr)");
        Effect_PayLP(source, 1000);
    }

    void Effect_0024_AcidRain(CardDisplay source)
    {
        // Debug.Log("Acid Rain (Destroy Machines)");
        Effect_DestroyType(source, "Machine");
    }

    void Effect_0025_AcidTrapHole(CardDisplay source)
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

    void Effect_0027_AdhesionTrapHole(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => t.ModifyStats(-t.currentAtk / 2, 0)
            );
        }
    }

    void Effect_0028_AfterTheStruggle(CardDisplay source)
    {
        Debug.Log("After the Struggle: Destrói monstros que batalharam.");
    }

    void Effect_0029_Agido(CardDisplay source)
    {
        int roll = Random.Range(1, 7);
        Debug.Log($"Agido rolou: {roll}. Invocando Fada Nível {roll} do GY.");
    }

    void Effect_0031_AirknightParshath(CardDisplay source)
    {
        Debug.Log("Airknight Parshath: Efeito ativado (Draw 1).");
        if (source.isPlayerCard) GameManager.Instance.DrawCard();
        else GameManager.Instance.DrawOpponentCard();
    }

    void Effect_0037_AlligatorsSwordDragon(CardDisplay source)
    {
        Debug.Log("Alligator's Sword Dragon: Pode atacar direto sob condições.");
    }

    void Effect_0039_AltarForTribute(CardDisplay source)
    {
        Debug.Log("Altar for Tribute: Tributar 1 para ganhar LP.");
    }

    void Effect_0041_AmazonessArcher(CardDisplay source)
    {
        // Debug.Log("Amazoness Archer (Tribute 2, Burn 1200)");
        Effect_TributeToBurn(source, 2, 1200);
    }

    void Effect_0042_AmazonessArchers(CardDisplay source)
    {
        Debug.Log("Amazoness Archers: Oponente perde 500 ATK e deve atacar.");
    }

    void Effect_0043_AmazonessBlowpiper(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.CurrentCardData.type.Contains("Monster"),
                (t) => t.ModifyStats(-500, 0)
            );
        }
    }

    void Effect_0044_AmazonessChainMaster(CardDisplay source)
    {
        Effect_PayLP(source, 1500);
        Debug.Log("Amazoness Chain Master: Olhar mão do oponente e pegar um monstro.");
    }

    void Effect_0045_AmazonessFighter(CardDisplay source)
    {
        Debug.Log("Amazoness Fighter: Você não toma dano de batalha.");
    }

    void Effect_0046_AmazonessPaladin(CardDisplay source)
    {
        Debug.Log("Amazoness Paladin: Ganha 100 ATK por cada Amazoness.");
    }

    void Effect_0047_AmazonessSpellcaster(CardDisplay source)
    {
        Debug.Log("Amazoness Spellcaster: Trocar ATK.");
    }

    void Effect_0048_AmazonessSwordsWoman(CardDisplay source)
    {
        Debug.Log("Amazoness Swords Woman: Oponente toma o dano de batalha.");
    }

    void Effect_0049_AmazonessTiger(CardDisplay source)
    {
        Debug.Log("Amazoness Tiger: Ganha 400 ATK. Oponente só pode atacar este.");
    }

    void Effect_0050_Ameba(CardDisplay source)
    {
        Debug.Log("Ameba: Controle mudou! Causando 2000 de dano ao oponente.");
        GameManager.Instance.DamageOpponent(2000);
    }

    void Effect_0053_AmphibiousBugrothMK3(CardDisplay source)
    {
        Debug.Log("MK-3: Ataca direto se Umi estiver no campo.");
    }

    void Effect_0054_Amplifier(CardDisplay source)
    {
        // Debug.Log("Amplifier (Equip to Jinzo)");
        Effect_Equip(source, 0, 0, "Machine");
    }

    void Effect_0055_AnOwlOfLuck(CardDisplay source)
    {
        // Debug.Log("An Owl of Luck (FLIP: Field Spell to top)");
        Effect_SearchDeckTop(source, "Field", "Spell");
    }

    void Effect_0058_AncientGearBeast(CardDisplay source)
    {
        Debug.Log("Ancient Gear Beast: Nega efeitos e impede S/T na batalha.");
    }

    void Effect_0059_AncientGearGolem(CardDisplay source)
    {
        Debug.Log("Ancient Gear Golem: Dano Perfurante e impede S/T na batalha.");
    }

    void Effect_0060_AncientGearSoldier(CardDisplay source)
    {
        Debug.Log("Ancient Gear Soldier: Impede S/T na batalha.");
    }

    void Effect_0062_AncientLamp(CardDisplay source)
    {
        Debug.Log("Ancient Lamp: Invocando La Jinn.");
    }

    void Effect_0066_AncientTelescope(CardDisplay source)
    {
        Debug.Log("Ancient Telescope: Vendo topo do deck do oponente.");
    }

    void Effect_0069_AndroSphinx(CardDisplay source)
    {
        Debug.Log("Andro Sphinx: Causa dano ao destruir monstros em defesa.");
    }

    void Effect_0071_Ante(CardDisplay source)
    {
        Debug.Log("Ante: Minigame de revelar cartas.");
    }

    void Effect_0073_AntiRaigeki(CardDisplay source)
    {
        Debug.Log("Anti Raigeki: Negando Raigeki e destruindo monstros do oponente.");
        // DestroyAllMonsters(true, false); // Requer acesso ao método em CardEffectManager
    }

    void Effect_0074_AntiAircraftFlower(CardDisplay source)
    {
        // Debug.Log("Anti-Aircraft Flower (Tribute Insect -> 800 dmg)");
        Effect_TributeToBurn(source, 1, 800, "Insect");
    }

    void Effect_0075_AntiSpell(CardDisplay source)
    {
        Debug.Log("Anti-Spell: Remove 2 contadores para negar magia.");
    }

    void Effect_0076_AntiSpellFragrance(CardDisplay source)
    {
        Debug.Log("Anti-Spell Fragrance: Magias devem ser baixadas antes de usar.");
    }

    void Effect_0077_ApprenticeMagician(CardDisplay source)
    {
        Debug.Log("Apprentice Magician: Coloca contador e invoca mago Lv2 ao morrer.");
    }

    void Effect_0078_Appropriate(CardDisplay source)
    {
        Debug.Log("Appropriate: Compre 2 cartas quando o oponente comprar fora da Draw Phase.");
    }

    void Effect_0079_AquaChorus(CardDisplay source)
    {
        Debug.Log("Aqua Chorus: Buff para monstros com mesmo nome.");
    }

    void Effect_0083_AquaSpirit(CardDisplay source)
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

    void Effect_0084_ArcanaKnightJoker(CardDisplay source)
    {
        Debug.Log("Arcana Knight Joker: Descarta para negar efeito que dá alvo.");
    }

    void Effect_0085_ArcaneArcherOfTheForest(CardDisplay source)
    {
        Debug.Log("Arcane Archer: Tributar Planta para destruir S/T.");
    }

    void Effect_0089_ArchfiendOfGilfer(CardDisplay source)
    {
        Effect_Equip(source, -500, 0);
    }

    void Effect_0090_ArchfiendsOath(CardDisplay source)
    {
        GameManager.Instance.DamagePlayer(500);
        Debug.Log("Archfiend's Oath: Declare uma carta.");
    }

    void Effect_0091_ArchfiendsRoar(CardDisplay source)
    {
        GameManager.Instance.DamagePlayer(500);
        Debug.Log("Archfiend's Roar: Selecione Archfiend no GY.");
    }

    void Effect_0092_ArchlordZerato(CardDisplay source)
    {
        Debug.Log("Archlord Zerato: Descarte LIGHT para destruir monstros.");
    }

    void Effect_0096_ArmedDragonLV3(CardDisplay source)
    {
        // Debug.Log("Armed Dragon LV3 (Standby Phase Level Up)");
        Effect_LevelUp(source, "0097");
    }

    void Effect_0097_ArmedDragonLV5(CardDisplay source)
    {
        Debug.Log("Armed Dragon LV5: Descarte monstro para destruir alvo.");
    }

    void Effect_0098_ArmedDragonLV7(CardDisplay source)
    {
        Debug.Log("Armed Dragon LV7: Descarte monstro para destruir todos <= ATK.");
    }

    void Effect_0099_ArmedNinja(CardDisplay source)
    {
        // Debug.Log("Armed Ninja (FLIP: Destroy Spell)");
        Effect_FlipDestroy(source, TargetType.Spell);
    }

    void Effect_0100_ArmedSamuraiBenKei(CardDisplay source)
    {
        Debug.Log("Ben Kei: Ganha 1 ataque extra por equipamento.");
    }

    void Effect_0101_ArmorBreak(CardDisplay source)
    {
        Debug.Log("Armor Break: Negar ativação de Equip Spell.");
    }

    void Effect_0102_ArmorExe(CardDisplay source)
    {
        Debug.Log("Armor Exe: Remover contador ou destruir.");
    }

    void Effect_0103_ArmoredGlass(CardDisplay source)
    {
        Debug.Log("Armored Glass: Negar efectos de Equipamento.");
    }

    void Effect_0108_ArrayOfRevealingLight(CardDisplay source)
    {
        Debug.Log("Array of Revealing Light: Declarar tipo.");
    }

    void Effect_0109_ArsenalBug(CardDisplay source)
    {
        Debug.Log("Arsenal Bug: ATK/DEF vira 1000 se não houver outro Inseto.");
    }

    void Effect_0110_ArsenalRobber(CardDisplay source)
    {
        Debug.Log("Arsenal Robber: Oponente escolhe uma Equip Spell do deck e envia ao GY.");
    }

    void Effect_0111_ArsenalSummoner(CardDisplay source)
    {
        // Debug.Log("Arsenal Summoner (FLIP: Search Guardian)");
        Effect_SearchDeck(source, "Guardian");
    }

    void Effect_0112_AssaultOnGHQ(CardDisplay source)
    {
        Debug.Log("Assault on GHQ: Destruir monstro para millar oponente.");
    }

    void Effect_0113_AstralBarrier(CardDisplay source)
    {
        Debug.Log("Astral Barrier: Redirecionar para ataque direto.");
    }

    void Effect_0114_AsuraPriest(CardDisplay source)
    {
        Debug.Log("Asura Priest: Ataca todos. Retorna para mão.");
    }

    void Effect_0115_AswanApparition(CardDisplay source)
    {
        Debug.Log("Aswan Apparition: Reciclar Trap do GY.");
    }

    void Effect_0116_AtomicFirefly(CardDisplay source)
    {
        Debug.Log("Atomic Firefly: 1000 dano ao oponente.");
    }

    void Effect_0117_AttackAndReceive(CardDisplay source)
    {
        Effect_DirectDamage(source, 700);
    }

    void Effect_0118_AussaTheEarthCharmer(CardDisplay source)
    {
        Debug.Log("Aussa: Controlar monstro EARTH.");
    }

    void Effect_0119_AutonomousActionUnit(CardDisplay source)
    {
        Effect_PayLP(source, 1500);
        Debug.Log("Autonomous Action Unit: Invocar do GY do oponente.");
    }

    void Effect_0120_AvatarOfThePot(CardDisplay source)
    {
        Debug.Log("Avatar of The Pot: Enviando Pot of Greed da mão para comprar 3.");
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0122_AxeOfDespair(CardDisplay source)
    {
        // Debug.Log("Axe of Despair (Equip +1000)");
        Effect_Equip(source, 1000, 0);
    }

    void Effect_0124_BESBigCore(CardDisplay source)
    {
        Debug.Log("B.E.S. Big Core: Contadores.");
    }

    void Effect_0125_BESCrystalCore(CardDisplay source)
    {
        Debug.Log("B.E.S. Crystal Core: Contadores.");
    }

    void Effect_0127_BackToSquareOne(CardDisplay source)
    {
        Debug.Log("Back to Square One: Descartar para retornar monstro ao topo do deck.");
    }

    void Effect_0128_Backfire(CardDisplay source)
    {
        Effect_DirectDamage(source, 500);
    }

    void Effect_0129_BackupSoldier(CardDisplay source)
    {
        Debug.Log("Backup Soldier: Recuperando monstros normais do GY.");
    }

    void Effect_0130_BadReactionToSimochi(CardDisplay source)
    {
        Debug.Log("Bad Reaction to Simochi ativado. Cura vira dano.");
    }

    void Effect_0131_BaitDoll(CardDisplay source)
    {
        Debug.Log("Bait Doll: Forçando ativação.");
    }

    void Effect_0132_BalloonLizard(CardDisplay source)
    {
        Debug.Log("Balloon Lizard: Contadores e dano.");
    }

    void Effect_0133_BanisherOfTheLight(CardDisplay source)
    {
        Debug.Log("Banisher of the Light: Banir cartas enviadas ao GY.");
    }

    void Effect_0134_BannerOfCourage(CardDisplay source)
    {
        Debug.Log("Banner of Courage: +200 ATK na Battle Phase.");
    }

    void Effect_0135_BarkOfDarkRuler(CardDisplay source)
    {
        Debug.Log("Bark of Dark Ruler: Pagar LP para reduzir stats.");
    }

    void Effect_0138_BarrelBehindTheDoor(CardDisplay source)
    {
        Debug.Log("Barrel Behind the Door: Refletir dano de efeito.");
    }

    void Effect_0139_BarrelDragon(CardDisplay source)
    {
        // Debug.Log("Barrel Dragon (Coin toss destroy)");
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Monster);
    }

    void Effect_0144_BatteryCharger(CardDisplay source)
    {
        Effect_PayLP(source, 500);
        Debug.Log("Battery Charger: SS Batteryman do GY.");
    }

    void Effect_0145_BatterymanAA(CardDisplay source)
    {
        Debug.Log("Batteryman AA: Ganha ATK/DEF.");
    }

    void Effect_0146_BatterymanC(CardDisplay source)
    {
        Debug.Log("Batteryman C: Buff em Machines.");
    }

    void Effect_0151_BattleScarred(CardDisplay source)
    {
        Debug.Log("Battle-Scarred: Oponente paga custo de Archfiend.");
    }

    void Effect_0152_BazooTheSoulEater(CardDisplay source)
    {
        Debug.Log("Bazoo: Banir do GY para ganhar ATK.");
    }

    void Effect_0155_BeastFangs(CardDisplay source)
    {
        // Debug.Log("Beast Fangs (Equip +300/300)");
        Effect_Equip(source, 300, 300, "Beast");
    }

    void Effect_0156_BeastSoulSwap(CardDisplay source)
    {
        Debug.Log("Beast Soul Swap: Troca de Bestas.");
    }

    void Effect_0158_BeastkingOfTheSwamps(CardDisplay source)
    {
        Debug.Log("Beastking: Substituto de fusão ou buscar Poly.");
    }

    void Effect_0163_BeckoningLight(CardDisplay source)
    {
        Debug.Log("Beckoning Light: Troca mão por Light do GY.");
    }

    void Effect_0164_BegoneKnave(CardDisplay source)
    {
        Debug.Log("Begone, Knave! ativado. Monstros que causam dano voltam para a mão.");
    }

    void Effect_0166_BehemothTheKingOfAllAnimals(CardDisplay source)
    {
        Debug.Log("Behemoth: Retornar Bestas do GY.");
    }

    void Effect_0167_Berfomet(CardDisplay source)
    {
        // Debug.Log("Berfomet (Search Gazelle)");
        Effect_SearchDeck(source, "Gazelle the King of Mythical Beasts");
    }

    void Effect_0168_BerserkDragon(CardDisplay source)
    {
        Debug.Log("Berserk Dragon: Ataca todos.");
    }

    void Effect_0169_BerserkGorilla(CardDisplay source)
    {
        Debug.Log("Berserk Gorilla: Destruído se defesa. Deve atacar.");
    }

    void Effect_0172_BigBangShot(CardDisplay source)
    {
        // Debug.Log("Big Bang Shot (Equip +400, Piercing, Banish)");
        Effect_Equip(source, 400, 0);
    }

    void Effect_0173_BigBurn(CardDisplay source)
    {
        Debug.Log("Big Burn: Banir ambos os cemitérios.");
    }

    void Effect_0174_BigEye(CardDisplay source)
    {
        Debug.Log("Big Eye: Reordenar topo do deck.");
    }

    void Effect_0177_BigShieldGardna(CardDisplay source)
    {
        Debug.Log("Big Shield Gardna: Nega magia e muda posição.");
    }

    void Effect_0178_BigWaveSmallWave(CardDisplay source)
    {
        Debug.Log("Big Wave Small Wave: Substituindo monstros de Água.");
    }

    void Effect_0179_BigTuskedMammoth(CardDisplay source)
    {
        Debug.Log("Big-Tusked Mammoth: Impede ataque no turno de invocação.");
    }

    void Effect_0183_Birdface(CardDisplay source)
    {
        // Debug.Log("Birdface (Search Harpie)");
        Effect_SearchDeck(source, "Harpie Lady");
    }

    void Effect_0184_BiteShoes(CardDisplay source)
    {
        Debug.Log("Bite Shoes: Mudar posição de batalha.");
    }

    void Effect_0185_BlackDragonsChick(CardDisplay source)
    {
        Debug.Log("Black Dragon's Chick: Invocando Red-Eyes B. Dragon.");
    }

    void Effect_0189_BLSEnvoy(CardDisplay source)
    {
        Debug.Log("BLS Envoy: Banir ou Ataque Duplo.");
    }

    void Effect_0191_BlackPendant(CardDisplay source)
    {
        // Debug.Log("Black Pendant (Equip +500, Burn 500)");
        Effect_Equip(source, 500, 0);
    }

    void Effect_0193_BlackTyranno(CardDisplay source)
    {
        Debug.Log("Black Tyranno: Ataque direto se tudo defesa.");
    }

    void Effect_0195_BladeKnight(CardDisplay source)
    {
        Debug.Log("Blade Knight: Buff se mão vazia.");
    }

    void Effect_0196_BladeRabbit(CardDisplay source)
    {
        Debug.Log("Blade Rabbit: Destruir monstro ao mudar para defesa.");
    }

    void Effect_0197_Bladefly(CardDisplay source)
    {
        // Debug.Log("Bladefly (Buff Wind)");
        Effect_Field(source, 500, 500, "", "WIND");
    }

    void Effect_0198_BlastHeldByATribute(CardDisplay source)
    {
        Debug.Log("Blast Held by a Tribute: Destruindo atacante e causando 1000 dano.");
    }

    void Effect_0199_BlastJuggler(CardDisplay source)
    {
        Debug.Log("Blast Juggler: Destruir monstros fracos.");
    }

    void Effect_0200_BlastMagician(CardDisplay source)
    {
        Debug.Log("Blast Magician: Removendo contadores para destruir monstro.");
    }

    void Effect_0201_BlastSphere(CardDisplay source)
    {
        Debug.Log("Blast Sphere: Se atacado face-down, equipa no atacante e destrói na próxima Standby.");
    }

    void Effect_0202_BlastWithChain(CardDisplay source)
    {
        // Debug.Log("Blast with Chain (Equip +500, destroy card if destroyed)");
        Effect_Equip(source, 500, 0);
    }

    void Effect_0203_BlastingTheRuins(CardDisplay source)
    {
        int gyCount = source.isPlayerCard ? GameManager.Instance.playerGraveyardDisplay.pileData.Count : GameManager.Instance.opponentGraveyardDisplay.pileData.Count;
        if (gyCount >= 30) GameManager.Instance.DamageOpponent(3000);
    }

    void Effect_0205_BlessingsOfTheNile(CardDisplay source)
    {
        Debug.Log("Blessings of the Nile: Ganha 1000 LP quando cartas são descartadas.");
    }

    void Effect_0206_BlindDestruction(CardDisplay source)
    {
        Debug.Log("Blind Destruction: Rola dado na Standby para destruir monstros.");
    }

    void Effect_0207_BlindlyLoyalGoblin(CardDisplay source)
    {
        Debug.Log("Blindly Loyal Goblin: Controle não pode mudar.");
    }

    void Effect_0208_BlockAttack(CardDisplay source)
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

    void Effect_0210_BloodSucker(CardDisplay source)
    {
        Debug.Log("Blood Sucker: Envia topo do deck do oponente ao GY ao causar dano.");
    }

    void Effect_0211_BlowbackDragon(CardDisplay source)
    {
        // Debug.Log("Blowback Dragon (Coin toss destroy)");
        Effect_CoinTossDestroy(source, 3, 2, TargetType.Any);
    }

    void Effect_0212_BlueMedicine(CardDisplay source)
    {
        // Debug.Log("Blue Medicine (Gain 400 LP)");
        Effect_GainLP(source, 400);
    }

    void Effect_0214_BlueEyesShiningDragon(CardDisplay source)
    {
        Debug.Log("Blue-Eyes Shining Dragon: Nega efeitos que dão alvo.");
    }

    void Effect_0215_BlueEyesToonDragon(CardDisplay source)
    {
        Debug.Log("Toon Dragon: Ataca direto se oponente não tiver Toon.");
    }

    void Effect_0219_BoarSoldier(CardDisplay source)
    {
        Debug.Log("Boar Soldier: Destruído se Normal Summon.");
    }

    void Effect_0223_BombardmentBeetle(CardDisplay source)
    {
        Debug.Log("Bombardment Beetle: Revela face-down do oponente.");
    }

    void Effect_0227_BookOfLife(CardDisplay source)
    {
        Debug.Log("Book of Life: Invocar Zumbi e banir monstro do oponente.");
        Effect_Revive(source, false);
    }

    void Effect_0228_BookOfMoon(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.position == CardDisplay.BattlePosition.Attack,
                (t) => {
                    t.ChangePosition();
                    t.ShowBack();
                    Debug.Log($"Book of Moon: {t.CurrentCardData.name} virado para baixo.");
                }
            );
        }
    }

    void Effect_0229_BookOfSecretArts(CardDisplay source)
    {
        // Debug.Log("Book of Secret Arts (Equip +300/300 Spellcaster)");
        Effect_Equip(source, 300, 300, "Spellcaster");
    }

    void Effect_0230_BookOfTaiyou(CardDisplay source)
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isFlipped,
                (t) => {
                    t.RevealCard();
                    t.ChangePosition();
                    Debug.Log($"Book of Taiyou: {t.CurrentCardData.name} virado para cima.");
                }
            );
        }
    }

    void Effect_0232_BottomlessShiftingSand(CardDisplay source)
    {
        Debug.Log("Bottomless Shifting Sand: Destrói monstro com maior ATK.");
    }

    void Effect_0233_BottomlessTrapHole(CardDisplay source)
    {
        Debug.Log("Bottomless Trap Hole: Destruindo e banindo monstro invocado com 1500+ ATK.");
    }

    void Effect_0235_Bowganian(CardDisplay source)
    {
        Debug.Log("Bowganian: 600 dano na Standby Phase.");
    }

    void Effect_0237_BrainControl(CardDisplay source)
    {
        Debug.Log("Brain Control: Pagar 800 LP para controlar monstro.");
        Effect_ChangeControl(source, true);
        Effect_PayLP(source, 800);
    }

    void Effect_0238_BrainJacker(CardDisplay source)
    {
        Debug.Log("Brain Jacker: Equipa e toma controle.");
    }

    void Effect_0240_BreakerTheMagicalWarrior(CardDisplay source)
    {
        Debug.Log("Breaker: Ganhou contador. Pode remover para destruir S/T.");
    }

    void Effect_0241_BreathOfLight(CardDisplay source)
    {
        // Debug.Log("Breath of Light (Destroy Rock)");
        Effect_DestroyType(source, "Rock");
    }

    void Effect_0242_BubbleCrash(CardDisplay source)
    {
        Debug.Log("Bubble Crash: Envia cartas ao GY até ter 5.");
    }

    void Effect_0243_BubbleShuffle(CardDisplay source)
    {
        Debug.Log("Bubble Shuffle: Mudando posições e invocando.");
    }

    void Effect_0244_BubonicVermin(CardDisplay source)
    {
        // Debug.Log("Bubonic Vermin (Flip SS)");
        Effect_SearchDeck(source, "Bubonic Vermin");
    }

    void Effect_0246_BurningAlgae(CardDisplay source)
    {
        if(source.isPlayerCard) GameManager.Instance.opponentLP += 1000; 
        else GameManager.Instance.playerLP += 1000;
    }

    void Effect_0248_BurningLand(CardDisplay source)
    {
        Debug.Log("Burning Land: Destrói campos e causa dano na Standby.");
    }

    void Effect_0249_BurningSpear(CardDisplay source)
    {
        // Debug.Log("Burning Spear (Equip +400/-200)");
        Effect_Equip(source, 400, -200, "", "Fire");
    }

    void Effect_0250_BurstBreath(CardDisplay source)
    {
        Debug.Log("Burst Breath: Tributa Dragão para destruir monstros.");
    }

    void Effect_0251_BurstStreamOfDestruction(CardDisplay source)
    {
        Debug.Log("Burst Stream: Destruir monstros do oponente (se tiver Blue-Eyes).");
        // DestroyAllMonsters(true, false); // Requer acesso ao método
    }

    void Effect_0252_BusterBlader(CardDisplay source)
    {
        Debug.Log("Buster Blader: Ganha ATK por Dragões.");
    }

    void Effect_0253_BusterRancher(CardDisplay source)
    {
        Debug.Log("Buster Rancher: Buff massivo se ATK base <= 1000.");
    }

    void Effect_0254_ButterflyDaggerElma(CardDisplay source)
    {
        // Debug.Log("Butterfly Dagger - Elma (Equip +300)");
        Effect_Equip(source, 300, 0);
    }

    void Effect_0255_ByserShock(CardDisplay source)
    {
        Debug.Log("Byser Shock: Retorna cartas setadas para a mão.");
    }

    void Effect_0256_CallOfDarkness(CardDisplay source)
    {
        Debug.Log("Call of Darkness: Pune Monster Reborn.");
    }

    void Effect_0257_CallOfTheEarthbound(CardDisplay source)
    {
        Debug.Log("Call of the Earthbound: Redireciona ataque.");
    }

    void Effect_0258_CallOfTheGrave(CardDisplay source)
    {
        Debug.Log("Call of the Grave: Nega Monster Reborn.");
    }

    void Effect_0259_CallOfTheHaunted(CardDisplay source)
    {
        Debug.Log("Call of the Haunted: Invocar do GY em ataque.");
        Effect_Revive(source, true);
    }

    void Effect_0260_CallOfTheMummy(CardDisplay source)
    {
        Debug.Log("Call of the Mummy: Invocando Zumbi da mão.");
    }

    void Effect_0262_CannonSoldier(CardDisplay source)
    {
        // Debug.Log("Cannon Soldier (Tribute burn)");
        Effect_TributeToBurn(source, 1, 500);
    }

    void Effect_0263_CannonballSpearShellfish(CardDisplay source)
    {
        Debug.Log("Cannonball Spear Shellfish: Imune a magias com Umi.");
    }

    void Effect_0264_CardDestruction(CardDisplay source)
    {
        Debug.Log("Card Destruction: Ambos descartam mão e compram a mesma quantidade.");
    }

    void Effect_0265_CardShuffle(CardDisplay source)
    {
        Effect_PayLP(source, 300);
        Debug.Log("Deck embaralhado.");
    }

    void Effect_0266_CardOfSafeReturn(CardDisplay source)
    {
        Debug.Log("Card of Safe Return: Compre 1 quando invocar do GY.");
    }

    void Effect_0267_CardOfSanctity(CardDisplay source)
    {
        Debug.Log("Card of Sanctity: Banindo mão e campo, comprando 2.");
        GameManager.Instance.DrawCard();
        GameManager.Instance.DrawCard();
    }

    void Effect_0268_CastleGate(CardDisplay source)
    {
        Debug.Log("Castle Gate: Tributa para causar dano.");
    }

    void Effect_0269_CastleWalls(CardDisplay source)
    {
        // Debug.Log("Castle Walls (Trap +500 DEF)");
        Effect_BuffStats(source, 0, 500);
    }

    void Effect_0270_CastleOfDarkIllusions(CardDisplay source)
    {
        Debug.Log("Castle of Dark Illusions: Buff em Zumbis.");
    }

    void Effect_0271_CatsEarTribe(CardDisplay source)
    {
        Debug.Log("Cat's Ear Tribe: ATK do oponente vira 200.");
    }

    void Effect_0272_CatapultTurtle(CardDisplay source)
    {
        Debug.Log("Catapult Turtle: Tributando para causar dano.");
    }

    void Effect_0273_CatnippedKitty(CardDisplay source)
    {
        Debug.Log("Catnipped Kitty: Torna DEF do oponente 0.");
    }

    void Effect_0274_CaveDragon(CardDisplay source)
    {
        Debug.Log("Cave Dragon: Restrições de invocação e ataque.");
    }

    void Effect_0275_Ceasefire(CardDisplay source)
    {
        Debug.Log("Ceasefire: Virar todos para cima e causar dano por efeito.");
    }

    void Effect_0277_CemetaryBomb(CardDisplay source)
    {
        int damage = GameManager.Instance.opponentGraveyardDisplay.pileData.Count * 100;
        GameManager.Instance.DamageOpponent(damage);
    }

    void Effect_0278_CentrifugalField(CardDisplay source)
    {
        Debug.Log("Centrifugal Field: Recupera material de fusão.");
    }

    void Effect_0279_CeremonialBell(CardDisplay source)
    {
        Debug.Log("Ceremonial Bell: Mãos reveladas.");
    }

    void Effect_0280_CestusOfDagla(CardDisplay source)
    {
        // Debug.Log("Cestus of Dagla (Equip +500 Fairy)");
        Effect_Equip(source, 500, 0, "Fairy");
    }

    void Effect_0281_ChainBurst(CardDisplay source)
    {
        Debug.Log("Chain Burst: Dano ao ativar armadilha.");
    }

    void Effect_0282_ChainDestruction(CardDisplay source)
    {
        Debug.Log("Chain Destruction: Destrói cópias no deck/mão.");
    }

    void Effect_0283_ChainDisappearance(CardDisplay source)
    {
        Debug.Log("Chain Disappearance: Bane cópias no deck/mão.");
    }

    void Effect_0284_ChainEnergy(CardDisplay source)
    {
        Debug.Log("Chain Energy: Custo de LP para jogar.");
    }

    void Effect_0287_ChangeOfHeart(CardDisplay source)
    {
        Debug.Log("Change of Heart: Controlar monstro até o fim do turno.");
        Effect_ChangeControl(source, true);
    }

    void Effect_0288_ChaosCommandMagician(CardDisplay source)
    {
        Debug.Log("Chaos Command Magician: Nega efeitos de monstro que dão alvo.");
    }

    void Effect_0289_ChaosEmperorDragon(CardDisplay source)
    {
        Effect_PayLP(source, 1000);
        Debug.Log("Chaos Emperor Dragon: Enviar tudo para o GY e causar dano.");
    }

    void Effect_0290_ChaosEnd(CardDisplay source)
    {
        if (GameManager.Instance.playerRemoved.Count >= 7)
        {
            // DestroyAllMonsters(true, true); // Requer acesso
        }
    }

    void Effect_0291_ChaosGreed(CardDisplay source)
    {
        if(GameManager.Instance.playerRemoved.Count >= 4 && GameManager.Instance.playerGraveyard.Count == 0) 
        { 
            GameManager.Instance.DrawCard(); 
            GameManager.Instance.DrawCard(); 
        }
    }

    void Effect_0292_ChaosNecromancer(CardDisplay source)
    {
        Debug.Log("Chaos Necromancer: ATK baseado no GY.");
    }

    void Effect_0293_ChaosSorcerer(CardDisplay source)
    {
        Debug.Log("Chaos Sorcerer: Banir monstro face-up.");
    }

    void Effect_0294_ChaosriderGustaph(CardDisplay source)
    {
        Debug.Log("Chaosrider Gustaph: Bane magias para ganhar ATK.");
    }

    void Effect_0296_CharmOfShabti(CardDisplay source)
    {
        Debug.Log("Charm of Shabti: Protege Gravekeepers.");
    }

    void Effect_0298_Checkmate(CardDisplay source)
    {
        Debug.Log("Checkmate: Terrorking ataca direto.");
    }

    void Effect_0299_ChimeraTheFlyingMythicalBeast(CardDisplay source)
    {
        Debug.Log("Chimera: Invoca material do GY.");
    }

    void Effect_0300_ChironTheMage(CardDisplay source)
    {
        Debug.Log("Chiron the Mage: Descarte Magia para destruir S/T.");
    }
}
