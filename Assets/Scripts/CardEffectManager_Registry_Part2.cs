using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part2()
    {
        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0501 - 0600)
        // =========================================================================================

        // 0501 - Disappear (Remove from play 1 card from opponent's Graveyard)
        AddEffect("0501", Effect_0501_Disappear);

        // 0502 - Disarmament (Destroy all Equip Cards)
        AddEffect("0502", Effect_0502_Disarmament);

        // 0503 - Disc Fighter (Destroy Defense Position monster with DEF >= 2000)
        AddEffect("0503", Effect_0503_DiscFighter);

        // 0506 - Disturbance Strategy (Opponent shuffles hand, draws same number)
        AddEffect("0506", Effect_0506_DisturbanceStrategy);

        // 0508 - Divine Wrath (Discard 1, negate monster effect, destroy)
        AddEffect("0508", Effect_0508_DivineWrath);

        // 0510 - Doitsu (Union)
        AddEffect("0510", Effect_0510_Doitsu);

        // 0512 - Dokurorider (Ritual)
        AddEffect("0512", Effect_0512_Dokurorider);

        // 0515 - Don Turtle (SS Don Turtle)
        AddEffect("0515", Effect_0515_DonTurtle);

        // 0516 - Don Zaloog (Hand destruction / Mill)
        AddEffect("0516", Effect_0516_DonZaloog);

        // 0517 - Dora of Fate (Damage on summon)
        AddEffect("0517", Effect_0517_DoraOfFate);

        // 0519 - Doriado's Blessing (Ritual Spell)
        AddEffect("0519", Effect_0519_DoriadosBlessing);

        // 0522 - Double Attack (Discard monster -> Double attack)
        AddEffect("0522", Effect_0522_DoubleAttack);

        // 0523 - Double Coston (2 Tributes for DARK)
        AddEffect("0523", Effect_0523_DoubleCoston);

        // 0524 - Double Snare (Destroy Jinzo/Royal Decree)
        AddEffect("0524", Effect_0524_DoubleSnare);

        // 0525 - Double Spell (Discard Spell -> Use Opp Spell)
        AddEffect("0525", Effect_0525_DoubleSpell);

        // 0526 - Dragged Down into the Grave (Hand destruction/Draw)
        AddEffect("0526", Effect_0526_DraggedDownIntoTheGrave);

        // 0527 - Dragon Capture Jar (Dragons to Defense)
        AddEffect("0527", Effect_0527_DragonCaptureJar);

        // 0528 - Dragon Manipulator (Flip: Control Dragon)
        AddEffect("0528", Effect_0528_DragonManipulator);

        // 0529 - Dragon Master Knight (Fusion)
        AddEffect("0529", Effect_0529_DragonMasterKnight);

        // 0530 - Dragon Piper (Flip: Destroy Jar, Dragons to Attack)
        AddEffect("0530", Effect_0530_DragonPiper);

        // 0531 - Dragon Seeker (Destroy Dragon)
        AddEffect("0531", Effect_0531_DragonSeeker);

        // 0533 - Dragon Treasure (Equip Dragon +300/300)
        AddEffect("0533", Effect_0533_DragonTreasure);

        // 0535 - Dragon's Gunfire (Damage or Destroy)
        AddEffect("0535", Effect_0535_DragonsGunfire);

        // 0536 - Dragon's Mirror (Fusion Banish)
        AddEffect("0536", Effect_0536_DragonsMirror);

        // 0537 - Dragon's Rage (Piercing for Dragons)
        AddEffect("0537", Effect_0537_DragonsRage);

        // 0539 - Dragonic Attack (Equip Warrior -> Dragon +500)
        AddEffect("0539", Effect_0539_DragonicAttack);

        // 0540 - Draining Shield (Negate attack, Gain LP)
        AddEffect("0540", Effect_0540_DrainingShield);

        // 0541 - Dramatic Rescue (Return Amazoness, SS)
        AddEffect("0541", Effect_0541_DramaticRescue);

        // 0542 - Dream Clown (Destroy on Defense)
        AddEffect("0542", Effect_0542_DreamClown);

        // 0543 - Dreamsprite (Redirect attack)
        AddEffect("0543", Effect_0543_Dreamsprite);

        // 0544 - Drill Bug (Parasite Paracide effect)
        AddEffect("0544", Effect_0544_DrillBug);

        // 0545 - Drillago (Direct attack condition)
        AddEffect("0545", Effect_0545_Drillago);

        // 0546 - Drillroid (Destroy Defense)
        AddEffect("0546", Effect_0546_Drillroid);

        // 0547 - Driving Snow (Destroy S/T)
        AddEffect("0547", Effect_0547_DrivingSnow);

        // 0550 - Drop Off (Discard draw)
        AddEffect("0550", Effect_0550_DropOff);

        // 0551 - Dummy Golem (Flip: Swap control)
        AddEffect("0551", Effect_0551_DummyGolem);

        // 0554 - Dust Barrier (Normal Monster immune to Spells)
        AddEffect("0554", Effect_0554_DustBarrier);

        // 0555 - Dust Tornado (Destroy S/T, Set)
        AddEffect("0555", Effect_0555_DustTornado);

        // 0556 - Eagle Eye (No Traps on Summon)
        AddEffect("0556", Effect_0556_EagleEye);

        // 0557 - Earth Chant (Ritual Spell)
        AddEffect("0557", Effect_0557_EarthChant);

        // 0559 - Earthquake (Change to Defense)
        AddEffect("0559", Effect_0559_Earthquake);

        // 0560 - Earthshaker (Attribute destroy)
        AddEffect("0560", Effect_0560_Earthshaker);

        // 0561 - Eatgaboon (Destroy low ATK summon)
        AddEffect("0561", Effect_0561_Eatgaboon);

        // 0562 - Ebon Magician Curran (Burn)
        AddEffect("0562", Effect_0562_EbonMagicianCurran);

        // 0563 - Ectoplasmer (Tribute to burn)
        AddEffect("0563", Effect_0563_Ectoplasmer);

        // 0564 - Ekibyo Drakmord (Lock attack, destroy)
        AddEffect("0564", Effect_0564_EkibyoDrakmord);

        // 0566 - Electric Lizard (Stun Zombie attacker)
        AddEffect("0566", Effect_0566_ElectricLizard);

        // 0567 - Electric Snake (Draw 2 on discard)
        AddEffect("0567", Effect_0567_ElectricSnake);

        // 0568 - Electro-Whip (Equip Thunder +300/300)
        AddEffect("0568", Effect_0568_ElectroWhip);

        // 0569 - Electromagnetic Bagworm (Flip: Control Machine)
        AddEffect("0569", Effect_0569_ElectromagneticBagworm);

        // 0570 - Elegant Egotist (SS Harpie)
        AddEffect("0570", Effect_0570_ElegantEgotist);

        // 0571 - Element Doom (Attribute effects)
        AddEffect("0571", Effect_0571_ElementDoom);

        // 0572 - Element Dragon (Attribute effects)
        AddEffect("0572", Effect_0572_ElementDragon);

        // 0573 - Element Magician (Attribute effects)
        AddEffect("0573", Effect_0573_ElementMagician);

        // 0574 - Element Saurus (Attribute effects)
        AddEffect("0574", Effect_0574_ElementSaurus);

        // 0575 - Element Soldier (Attribute effects)
        AddEffect("0575", Effect_0575_ElementSoldier);

        // 0576 - Element Valkyrie (Attribute effects)
        AddEffect("0576", Effect_0576_ElementValkyrie);

        // 0577 - Elemental Burst (Tribute 4 -> Nuke)
        AddEffect("0577", Effect_0577_ElementalBurst);

        // 0579 - Elemental HERO Bubbleman (SS, Draw 2)
        AddEffect("0579", Effect_0579_ElementalHEROBubbleman);

        // 0582 - Elemental HERO Flame Wingman (Burn on destroy)
        AddEffect("0582", Effect_0582_ElementalHEROFlameWingman);

        // 0584 - Elemental HERO Thunder Giant (Discard -> Destroy)
        AddEffect("0584", Effect_0584_ElementalHEROThunderGiant);

        // 0585 - Elemental Mistress Doriado (Ritual)
        AddEffect("0585", Effect_0585_ElementalMistressDoriado);

        // 0586 - Elephant Statue of Blessing (Gain LP on discard)
        AddEffect("0586", Effect_0586_ElephantStatueOfBlessing);

        // 0587 - Elephant Statue of Disaster (Damage on discard)
        AddEffect("0587", Effect_0587_ElephantStatueOfDisaster);

        // 0588 - Elf's Light (Equip LIGHT +400/-200)
        AddEffect("0588", Effect_0588_ElfsLight);

        // 0589 - Emblem of Dragon Destroyer (Search Buster Blader)
        AddEffect("0589", Effect_0589_EmblemOfDragonDestroyer);

        // 0590 - Embodiment of Apophis (Trap Monster)
        AddEffect("0590", Effect_0590_EmbodimentOfApophis);

        // 0592 - Emergency Provisions (Send S/T -> Gain LP)
        AddEffect("0592", Effect_0592_EmergencyProvisions);

        // 0593 - Emes the Infinity (Gain ATK)
        AddEffect("0593", Effect_0593_EmesTheInfinity);

        // 0594 - Emissary of the Afterlife (Search Normal)
        AddEffect("0594", Effect_0594_EmissaryOfTheAfterlife);

        // 0595 - Emissary of the Oasis (Protect Normal)
        AddEffect("0595", Effect_0595_EmissaryOfTheOasis);

        // 0599 - Enchanted Javelin (Gain LP equal to ATK)
        AddEffect("0599", Effect_0599_EnchantedJavelin);

        // 0600 - Enchanting Fitting Room (Pay 800, Excavate)
        AddEffect("0600", Effect_0600_EnchantingFittingRoom);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0601 - 0700)
        // =========================================================================================

        // 0602 - Enemy Controller (Change Pos / Take Control)
        AddEffect("0602", Effect_EnemyController);

        // 0603 - Energy Drain (Buff)
        AddEffect("0603", Effect_0603_EnergyDrain);

        // 0604 - Enervating Mist (Hand limit 5)
        AddEffect("0604", Effect_0604_EnervatingMist);

        // 0605 - Enraged Battle Ox (Piercing for Beast/Beast-Warrior/Winged Beast)
        AddEffect("0605", Effect_0605_EnragedBattleOx);

        // 0606 - Enraged Muka Muka (Buff per hand)
        AddEffect("0606", Effect_0606_EnragedMukaMuka);

        // 0607 - Eradicating Aerosol (Destroy Insects)
        AddEffect("0607", Effect_0607_EradicatingAerosol);

        // 0608 - Eria the Water Charmer (Flip: Control Water)
        AddEffect("0608", Effect_0608_EriaTheWaterCharmer);

        // 0609 - Eternal Drought (Destroy Fish)
        AddEffect("0609", Effect_0609_EternalDrought);

        // 0610 - Eternal Rest (Destroy Equipped)
        AddEffect("0610", Effect_0610_EternalRest);

        // 0611 - Exarion Universe (Piercing option)
        AddEffect("0611", Effect_0611_ExarionUniverse);

        // 0612 - Exchange (Swap card in hand)
        AddEffect("0612", Effect_0612_Exchange);

        // 0613 - Exchange of the Spirit (Swap Deck/GY)
        AddEffect("0613", Effect_0613_ExchangeOfTheSpirit);

        // 0614 - Exhausting Spell (Remove Spell Counters)
        AddEffect("0614", Effect_0614_ExhaustingSpell);

        // 0615 - Exile of the Wicked (Destroy Fiends)
        AddEffect("0615", Effect_0615_ExileOfTheWicked);

        // 0616 - Exiled Force (Tribute to destroy)
        AddEffect("0616", Effect_0616_ExiledForce);

        // 0617 - Exodia Necross (SS condition)
        AddEffect("0617", Effect_0617_ExodiaNecross);

        // 0620 - Fairy Box (Coin toss 0 ATK)
        AddEffect("0620", Effect_0620_FairyBox);

        // 0622 - Fairy Guardian (Recycle Spell)
        AddEffect("0622", Effect_0622_FairyGuardian);

        // 0623 - Fairy King Truesdale (Buff Plants)
        AddEffect("0623", Effect_0623_FairyKingTruesdale);

        // 0624 - Fairy Meteor Crush (Equip Piercing)
        AddEffect("0624", Effect_0624_FairyMeteorCrush);

        // 0626 - Fairy of the Spring (Recycle Equip)
        AddEffect("0626", Effect_0626_FairyOfTheSpring);

        // 0628 - Fairy's Hand Mirror (Redirect Spell)
        AddEffect("0628", Effect_0628_FairysHandMirror);

        // 0631 - Fake Trap (Protect Traps)
        AddEffect("0631", Effect_0631_FakeTrap);

        // 0632 - Falling Down (Snatch Steal Archfiend)
        AddEffect("0632", Effect_0632_FallingDown);

        // 0633 - Familiar Knight (SS on destroy)
        AddEffect("0633", Effect_0633_FamiliarKnight);

        // 0634 - Fatal Abacus (Burn on GY)
        AddEffect("0634", Effect_0634_FatalAbacus);

        // 0635 - Fear from the Dark (SS on discard)
        AddEffect("0635", Effect_0635_FearFromTheDark);

        // 0636 - Fengsheng Mirror (Discard Spirit)
        AddEffect("0636", Effect_0636_FengshengMirror);

        // 0637 - Fenrir (Skip Draw)
        AddEffect("0637", Effect_0637_Fenrir);

        // 0639 - Fiber Jar (Reset Duel)
        AddEffect("0639", Effect_0639_FiberJar);

        // 0640 - Fiend Comedian (Banish/Mill)
        AddEffect("0640", Effect_0640_FiendComedian);

        // 0645 - Fiend Skull Dragon (Fusion Negate)
        AddEffect("0645", Effect_0645_FiendSkullDragon);

        // 0648 - Fiend's Hand Mirror (Redirect Spell)
        AddEffect("0648", Effect_0648_FiendsHandMirror);

        // 0650 - Fiend's Sanctuary (SS Token)
        AddEffect("0650", Effect_0650_FiendsSanctuary);

        // 0651 - Final Attack Orders (Force Attack)
        AddEffect("0651", Effect_0651_FinalAttackOrders);

        // 0652 - Final Countdown (Win in 20)
        AddEffect("0652", Effect_0652_FinalCountdown);

        // 0653 - Final Destiny (Discard 5 Nuke)
        AddEffect("0653", Effect_0653_FinalDestiny);

        // 0654 - Final Flame (Burn 600)
        AddEffect("0654", Effect_0654_FinalFlame);

        // 0655 - Final Ritual of the Ancients (Ritual Spell)
        AddEffect("0655", Effect_0655_FinalRitualOfTheAncients);

        // 0656 - Fire Darts (Dice Burn)
        AddEffect("0656", Effect_0656_FireDarts);

        // 0659 - Fire Princess (Burn on Heal)
        AddEffect("0659", Effect_0659_FirePrincess);

        // 0661 - Fire Sorcerer (Banish Hand Burn)
        AddEffect("0661", Effect_0661_FireSorcerer);

        // 0666 - Fissure (Destroy lowest ATK)
        AddEffect("0666", Effect_0666_Fissure);

        // 0667 - Five-Headed Dragon (Battle Protection)
        AddEffect("0667", Effect_0667_FiveHeadedDragon);

        // 0673 - Flame Ruler (2 Tributes Fire)
        AddEffect("0673", Effect_0673_FlameRuler);

        // 0676 - Flash Assailant (Debuff hand)
        AddEffect("0676", Effect_0676_FlashAssailant);

        // 0677 - Flint (Lock)
        AddEffect("0677", Effect_0677_Flint);

        // 0680 - Flying Kamakiri #1 (Search Wind)
        AddEffect("0680", Effect_0680_FlyingKamakiri1);

        // 0683 - Follow Wind (Equip +300)
        AddEffect("0683", Effect_0683_FollowWind);

        // 0684 - Foolish Burial (Mill)
        AddEffect("0684", Effect_0684_FoolishBurial);

        // 0685 - Forced Ceasefire (No Traps)
        AddEffect("0685", Effect_0685_ForcedCeasefire);

        // 0686 - Forced Requisition (Discard)
        AddEffect("0686", Effect_0686_ForcedRequisition);

        // 0687 - Forest (Field Buff)
        AddEffect("0687", Effect_0687_Forest);

        // 0688 - Formation Union (Union)
        AddEffect("0688", Effect_0688_FormationUnion);

        // 0691 - Fox Fire (Revive)
        AddEffect("0691", Effect_0691_FoxFire);

        // 0692 - Freed the Brave Wanderer (Banish Light Destroy)
        AddEffect("0692", Effect_0692_FreedTheBraveWanderer);

        // 0693 - Freed the Matchless General (Search Warrior)
        AddEffect("0693", Effect_0693_FreedTheMatchlessGeneral);

        // 0694 - Freezing Beast (Union)
        AddEffect("0694", Effect_0694_FreezingBeast);

        // 0696 - Frontier Wiseman (Negate Target)
        AddEffect("0696", Effect_0696_FrontierWiseman);

        // 0697 - Frontline Base (SS Union)
        AddEffect("0697", Effect_0697_FrontlineBase);

        // 0698 - Frozen Soul (Skip Battle)
        AddEffect("0698", Effect_0698_FrozenSoul);

        // 0699 - Fruits of Kozaky's Studies (Reorder)
        AddEffect("0699", Effect_0699_FruitsOfKozakysStudies);

        // 0700 - Fuh-Rin-Ka-Zan (4 Elements)
        AddEffect("0700", Effect_0700_FuhRinKaZan);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 0701 - 0800)
        // =========================================================================================

        // 0701 - Fuhma Shuriken (Equip Ninja +700, Burn 700 on GY)
        AddEffect("0701", Effect_0701_FuhmaShuriken);

        // 0702 - Fulfillment of the Contract (Pay 800, Revive Ritual)
        AddEffect("0702", Effect_0702_FulfillmentOfTheContract);

        // 0704 - Fushi No Tori (Spirit, Heal damage)
        AddEffect("0704", Effect_0704_FushiNoTori);

        // 0705 - Fushioh Richie (Flip, Negate, SS Zombie)
        AddEffect("0705", Effect_0705_FushiohRichie);

        // 0706 - Fusilier Dragon (NS no tribute)
        AddEffect("0706", Effect_0706_FusilierDragon);

        // 0707 - Fusion Gate (Field Fusion)
        AddEffect("0707", Effect_0707_FusionGate);

        // 0708 - Fusion Recovery (Add Poly + Material)
        AddEffect("0708", Effect_0708_FusionRecovery);

        // 0709 - Fusion Sage (Search Poly)
        AddEffect("0709", Effect_0709_FusionSage);

        // 0710 - Fusion Sword Murasame Blade (Equip Warrior +800)
        AddEffect("0710", Effect_0710_FusionSwordMurasameBlade);

        // 0711 - Fusion Weapon (Equip Fusion Lv6- +1500)
        AddEffect("0711", Effect_0711_FusionWeapon);

        // 0715 - Gaia Power (Field Earth +500/-400)
        AddEffect("0715", Effect_0715_GaiaPower);

        // 0716 - Gaia Soul (Tribute Pyro buff)
        AddEffect("0716", Effect_0716_GaiaSoul);

        // 0719 - Gale Dogra (Pay 3000 dump Extra)
        AddEffect("0719", Effect_0719_GaleDogra);

        // 0720 - Gale Lizard (Flip Return)
        AddEffect("0720", Effect_0720_GaleLizard);

        // 0721 - Gamble (Coin toss)
        AddEffect("0721", Effect_0721_Gamble);

        // 0725 - Garma Sword Oath (Ritual)
        AddEffect("0725", Effect_0725_GarmaSwordOath);

        // 0728 - Garuda the Wind Spirit (SS Banish Wind)
        AddEffect("0728", Effect_0728_GarudaTheWindSpirit);

        // 0731 - Gate Guardian (SS Tributes)
        AddEffect("0731", Effect_0731_GateGuardian);

        // 0733 - Gather Your Mind (Search self)
        AddEffect("0733", Effect_0733_GatherYourMind);

        // 0734 - Gatling Dragon (Coin destroy)
        AddEffect("0734", Effect_0734_GatlingDragon);

        // 0736 - Gear Golem the Moving Fortress (Pay 800 Direct)
        AddEffect("0736", Effect_0736_GearGolemTheMovingFortress);

        // 0737 - Gearfried the Iron Knight (Destroy Equip)
        AddEffect("0737", Effect_0737_GearfriedTheIronKnight);

        // 0738 - Gearfried the Swordmaster (Destroy on Equip)
        AddEffect("0738", Effect_0738_GearfriedTheSwordmaster);

        // 0740 - Gemini Imps (Negate discard)
        AddEffect("0740", Effect_0740_GeminiImps);

        // 0742 - Germ Infection (Equip Debuff)
        AddEffect("0742", Effect_0742_GermInfection);

        // 0743 - Gernia (SS on destroy)
        AddEffect("0743", Effect_0743_Gernia);

        // 0744 - Getsu Fuhma (Destroy Fiend/Zombie)
        AddEffect("0744", Effect_0744_GetsuFuhma);

        // 0745 - Ghost Knight of Jackal (SS opp monster)
        AddEffect("0745", Effect_0745_GhostKnightOfJackal);

        // 0747 - Giant Axe Mummy (Flip, Destroy weak attacker)
        AddEffect("0747", Effect_0747_GiantAxeMummy);

        // 0749 - Giant Germ (Burn, SS)
        AddEffect("0749", Effect_0749_GiantGerm);

        // 0750 - Giant Kozaky (Destroy if no Kozaky)
        AddEffect("0750", Effect_0750_GiantKozaky);

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
        AddEffect("0932", c => Debug.Log("Imperial Order ativado. Manutenção de 700 LP na Standby Phase."));

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
    }
}
