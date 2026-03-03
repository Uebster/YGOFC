using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part4()
    {
        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1501 - 1600)
        // =========================================================================================

        // 1502 - Red Gadget (Search Yellow Gadget)
        AddEffect("1502", c => Effect_SearchDeck(c, "Yellow Gadget"));

        // 1503 - Red Medicine (Gain 500 LP)
        AddEffect("1503", Effect_1503_RedMedicine);

        // 1504 - Red-Eyes B. Chick
        AddEffect("1504", Effect_1504_RedEyesBChick);

        // 1507 - Reflect Bounder (Damage on attack)
        AddEffect("1507", Effect_1507_ReflectBounder);

        // 1508 - Regenerating Mummy (Return to hand)
        AddEffect("1508", Effect_1508_RegeneratingMummy);

        // 1509 - Reinforcement of the Army (Search Warrior)
        AddEffect("1509", Effect_1509_ReinforcementOfTheArmy);

        // 1510 - Reinforcements (Buff +500)
        AddEffect("1510", Effect_1510_Reinforcements);

        // 1511 - Release Restraint (Tribute Gearfried -> SS Swordmaster)
        AddEffect("1511", Effect_1511_ReleaseRestraint);

        // 1512 - Relieve Monster (Return -> SS Lv4)
        AddEffect("1512", Effect_1512_RelieveMonster);

        // 1513 - Relinquished (Ritual Absorb)
        AddEffect("1513", Effect_1513_Relinquished);

        // 1514 - Reload (Shuffle Hand Draw)
        AddEffect("1514", Effect_1514_Reload);

        // 1515 - Remove Brainwashing (Control Reset)
        AddEffect("1515", Effect_1515_RemoveBrainwashing);

        // 1516 - Remove Trap (Destroy Face-up Trap)
        AddEffect("1516", Effect_1516_RemoveTrap);

        // 1517 - Rescue Cat (Tribute -> SS 2 Beasts)
        AddEffect("1517", Effect_1517_RescueCat);

        // 1518 - Reshef the Dark Being (Discard Spell -> Control)
        AddEffect("1518", Effect_1518_ReshefTheDarkBeing);

        // 1520 - Restructer Revolution (Burn per hand card)
        AddEffect("1520", Effect_1520_RestructerRevolution);

        // 1521 - Resurrection of Chakra (Ritual)
        AddEffect("1521", Effect_1521_ResurrectionOfChakra);

        // 1522 - Return Zombie (Pay 500 Recycle)
        AddEffect("1522", Effect_1522_ReturnZombie);

        // 1523 - Return from the Different Dimension (Pay half LP -> SS Banished)
        AddEffect("1523", Effect_1523_ReturnFromTheDifferentDimension);

        // 1524 - Return of the Doomed (Discard -> Recycle)
        AddEffect("1524", Effect_1524_ReturnOfTheDoomed);

        // 1525 - Reversal Quiz (Swap LP)
        AddEffect("1525", Effect_1525_ReversalQuiz);

        // 1526 - Reverse Trap (Invert mods)
        AddEffect("1526", Effect_1526_ReverseTrap);

        // 1527 - Revival Jam (Pay 1000 Revive)
        AddEffect("1527", c => Debug.Log("Revival Jam: Efeito de renascimento configurado."));

        // 1528 - Revival of Dokurorider (Ritual)
        AddEffect("1528", Effect_1528_RevivalOfDokurorider);

        // 1532 - Rigorous Reaver (Flip Discard)
        AddEffect("1532", c => Debug.Log("Rigorous Reaver: Efeitos de FLIP e batalha configurados."));

        // 1533 - Ring of Destruction (Destroy & Burn)
        AddEffect("1533", Effect_1533_RingOfDestruction);

        // 1534 - Ring of Magnetism (Equip -500, Taunt)
        AddEffect("1534", Effect_1534_RingOfMagnetism);

        // 1535 - Riryoku (Halve & Add)
        AddEffect("1535", Effect_1535_Riryoku);

        // 1536 - Riryoku Field (Negate Spell)
        AddEffect("1536", c => Debug.Log("Riryoku Field: Negação de alvo (Requer Chain)."));

        // 1537 - Rising Air Current (Field Wind +500/-400)
        AddEffect("1537", Effect_1537_RisingAirCurrent);

        // 1538 - Rising Energy (Discard -> +1500)
        AddEffect("1538", Effect_1538_RisingEnergy);

        // 1539 - Rite of Spirit (Revive GK)
        AddEffect("1539", Effect_1539_RiteOfSpirit);

        // 1540 - Ritual Weapon (Equip Ritual Lv6- +1500)
        AddEffect("1540", Effect_1540_RitualWeapon);

        // 1541 - Rivalry of Warlords (Type Lock)
        AddEffect("1541", c => Debug.Log("Rivalry of Warlords: Restrição de Tipo ativa (Lógica no SummonManager)."));

        // 1543 - Robbin' Goblin (Damage -> Discard)
        AddEffect("1543", c => Debug.Log("Robbin' Goblin: Ativo."));

        // 1544 - Robbin' Zombie (Damage -> Mill)
        AddEffect("1544", c => Debug.Log("Robbin' Zombie: Ativo."));

        // 1548 - Roc from the Valley of Haze (Recycle)
        AddEffect("1548", c => Debug.Log("Roc: Efeito de reciclagem configurado."));

        // 1549 - Rock Bombardment (Mill Rock -> 500 dmg)
        AddEffect("1549", Effect_1549_RockBombardment);

        // 1553 - Rocket Jumper (Direct Attack)
        AddEffect("1553", c => Debug.Log("Rocket Jumper: Pode atacar diretamente se oponente só tiver defesa."));

        // 1554 - Rocket Warrior (Battle protection)
        AddEffect("1554", c => Debug.Log("Rocket Warrior: Efeitos de batalha configurados."));

        // 1555 - Rod of Silence - Kay'est (Equip +500 DEF)
        AddEffect("1555", Effect_1555_RodOfSilenceKayest);

        // 1556 - Rod of the Mind's Eye (Equip 1000 dmg)
        AddEffect("1556", c => Effect_Equip(c, 0, 0));

        // 1559 - Rope of Life (Discard -> Revive +800)
        AddEffect("1559", c => Debug.Log("Rope of Life: Armadilha de renascimento configurada."));

        // 1561 - Roulette Barrel (Dice Destroy)
        AddEffect("1561", Effect_1561_RouletteBarrel);

        // 1562 - Royal Command (Negate Flip)
        AddEffect("1562", c => Debug.Log("Royal Command: Efeitos Flip negados (Passivo)."));

        // 1563 - Royal Decree (Negate Traps)
        AddEffect("1563", c => Debug.Log("Royal Decree: Outras Traps negadas (Passivo)."));

        // 1565 - Royal Keeper (Flip +300)
        AddEffect("1565", Effect_1565_RoyalKeeper);

        // 1566 - Royal Magical Library (Counters -> Draw)
        AddEffect("1566", Effect_1566_RoyalMagicalLibrary);

        // 1567 - Royal Oppression (Pay 800 Negate SS)
        AddEffect("1567", c => Debug.Log("Royal Oppression: Negação de SS (Requer Chain)."));

        // 1568 - Royal Surrender (Negate Continuous Trap)
        AddEffect("1568", c => Debug.Log("Royal Surrender: Nega Trap Contínua (Requer Chain)."));

        // 1569 - Royal Tribute (Necrovalley Discard)
        AddEffect("1569", Effect_1569_RoyalTribute);

        // 1571 - Rush Recklessly (Target +700)
        AddEffect("1571", Effect_1571_RushRecklessly);

        // 1572 - Ryu Kokki (Destroy Warrior/Spellcaster)
        AddEffect("1572", c => Debug.Log("Ryu Kokki: Efeito de batalha configurado."));

        // 1573 - Ryu Senshi (Pay 1000 Negate Trap)
        AddEffect("1573", c => Debug.Log("Ryu Senshi: Efeitos de negação ativos."));

        // 1575 - Ryu-Kishin Clown (Change Pos)
        AddEffect("1575", Effect_1575_RyuKishinClown);

        // 1576 - Sacred Crane (Draw on SS)
        AddEffect("1576", c => Debug.Log("Sacred Crane: Efeito de compra no OnSpecialSummon."));

        // 1578 - Sacred Phoenix of Nephthys (Revive/Nuke S/T)
        AddEffect("1578", c => Debug.Log("Sacred Phoenix: Renasce e destrói S/T."));

        // 1579 - Sage's Stone (SS DM)
        AddEffect("1579", Effect_1579_SagesStone);

        // 1581 - Sakuretsu Armor (Destroy Attacker)
        AddEffect("1581", c => Debug.Log("Sakuretsu Armor: Destrói monstro atacante."));

        // 1582 - Salamandra (Equip Fire +700)
        AddEffect("1582", Effect_1582_Salamandra);

        // 1583 - Salvage (Add 2 Water)
        AddEffect("1583", Effect_1583_Salvage);

        // 1584 - Sand Gambler (Coin Destroy)
        AddEffect("1584", Effect_1584_SandGambler);

        // 1586 - Sanga of the Thunder (Zero ATK)
        AddEffect("1586", c => Debug.Log("Sanga: Zera ATK do atacante."));

        // 1587 - Sangan (Search 1500-)
        AddEffect("1587", c => Debug.Log("Sangan: Busca monstro 1500- ATK."));

        // 1589 - Sasuke Samurai (Destroy Face-down)
        AddEffect("1589", c => Debug.Log("Sasuke Samurai: Destrói face-down antes do cálculo."));

        // 1590 - Sasuke Samurai #2 (Pay 800 No S/T)
        AddEffect("1590", c => Debug.Log("Sasuke Samurai #2: Bloqueio de S/T na batalha."));

        // 1591 - Sasuke Samurai #3 (Hand Fill)
        AddEffect("1591", Effect_1591_SasukeSamurai3);

        // 1592 - Sasuke Samurai #4 (Coin Destroy)
        AddEffect("1592", c => Debug.Log("Sasuke Samurai #4: Moeda para destruir monstro."));

        // 1593 - Satellite Cannon (Gain ATK)
        AddEffect("1593", c => Debug.Log("Satellite Cannon: Ganha 1000 ATK por turno."));

        // 1594 - Scapegoat (SS 4 Tokens)
        AddEffect("1594", Effect_1594_Scapegoat);

        // 1595 - Scapeghost
        AddEffect("1595", Effect_1595_Scapeghost);

        // 1597 - Scroll of Bewitchment (Equip Change Attribute)
        AddEffect("1597", Effect_1597_ScrollOfBewitchment);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1601 - 1700)
        // =========================================================================================

        // 1601 - Seal of the Ancients (Pay 1000, Reveal Face-down)
        AddEffect("1601", Effect_1601_SealOfTheAncients);

        // 1603 - Sebek's Blessing (Quick-Play: Gain LP = Damage)
        AddEffect("1603", Effect_1603_SebeksBlessing);

        // 1604 - Second Coin Toss (Redo coin toss)
        AddEffect("1604", Effect_1604_SecondCoinToss);

        // 1605 - Second Goblin (Union)
        AddEffect("1605", Effect_1605_SecondGoblin);

        // 1606 - Secret Barrel (Burn per card)
        AddEffect("1606", Effect_1606_SecretBarrel);

        // 1607 - Secret Pass to the Treasures (Direct attack < 1000)
        AddEffect("1607", Effect_1607_SecretPassToTheTreasures);

        // 1610 - Self-Destruct Button (Draw game condition)
        AddEffect("1610", Effect_1610_SelfDestructButton);

        // 1612 - Senju of the Thousand Hands (Search Ritual Monster)
        AddEffect("1612", Effect_1612_SenjuOfTheThousandHands);

        // 1613 - Senri Eye (Pay 100, peek deck)
        AddEffect("1613", Effect_1613_SenriEye);

        // 1615 - Serial Spell (Discard hand, copy spell)
        AddEffect("1615", Effect_1615_SerialSpell);

        // 1618 - Serpentine Princess (Deck shuffle -> SS)
        AddEffect("1618", Effect_1618_SerpentinePrincess);

        // 1619 - Servant of Catabolism (Direct attack)
        AddEffect("1619", Effect_1619_ServantOfCatabolism);

        // 1620 - Seven Tools of the Bandit (Pay 1000 negate Trap)
        AddEffect("1620", Effect_1620_SevenToolsOfTheBandit);

        // 1621 - Shadow Ghoul (Gain ATK per GY)
        AddEffect("1621", Effect_1621_ShadowGhoul);

        // 1623 - Shadow Spell (Continuous Trap: -700 ATK, no attack/change pos)
        AddEffect("1623", Effect_1623_ShadowSpell);

        // 1624 - Shadow Tamer (Flip: Control Fiend)
        AddEffect("1624", Effect_1624_ShadowTamer);

        // 1625 - Shadow of Eyes (Trigger: Set -> Attack)
        AddEffect("1625", Effect_1625_ShadowOfEyes);

        // 1626 - Shadowknight Archfiend (Maintenance, Dice negate)
        AddEffect("1626", Effect_1626_ShadowknightArchfiend);

        // 1627 - Shadowslayer (Direct attack if all def)
        AddEffect("1627", Effect_1627_Shadowslayer);

        // 1629 - Share the Pain (Tribute 1, Opp tribute 1)
        AddEffect("1629", Effect_1629_ShareThePain);

        // 1630 - Shield & Sword (Swap ATK/DEF)
        AddEffect("1630", Effect_1630_ShieldAndSword);

        // 1631 - Shield Crush (Destroy Def)
        AddEffect("1631", Effect_1631_ShieldCrush);

        // 1632 - Shien's Spy (Give control)
        AddEffect("1632", Effect_1632_ShiensSpy);

        // 1633 - Shift (Redirect target)
        AddEffect("1633", Effect_1633_Shift);

        // 1634 - Shifting Shadows (Shuffle def monsters)
        AddEffect("1634", Effect_1634_ShiftingShadows);

        // 1635 - Shinato's Ark (Ritual Spell)
        AddEffect("1635", Effect_1635_ShinatosArk);

        // 1636 - Shinato, King of a Higher Plane (Burn on destroy)
        AddEffect("1636", Effect_1636_ShinatoKingOfAHigherPlane);

        // 1637 - Shine Palace (Equip Light +700)
        AddEffect("1637", Effect_1637_ShinePalace);

        // 1639 - Shining Angel (Floater Light)
        AddEffect("1639", Effect_1639_ShiningAngel);

        // 1641 - Shooting Star Bow - Ceal (Equip -1000, Direct Attack)
        AddEffect("1641", Effect_1641_ShootingStarBowCeal);

        // 1643 - Shrink (Halve ATK)
        AddEffect("1643", Effect_1643_Shrink);

        // 1644 - Silent Doom (Revive Normal Def)
        AddEffect("1644", Effect_1644_SilentDoom);

        // 1645 - Silent Swordsman LV3 (Negate Spell target, LV up)
        AddEffect("1645", Effect_1645_SilentSwordsmanLV3);

        // 1646 - Silent Swordsman LV5 (Immune Spell, LV up)
        AddEffect("1646", Effect_1646_SilentSwordsmanLV5);

        // 1647 - Silent Swordsman LV7 (Negate all Spells)
        AddEffect("1647", Effect_1647_SilentSwordsmanLV7);

        // 1648 - Silpheed (SS Banish Wind, Hand discard)
        AddEffect("1648", Effect_1648_Silpheed);

        // 1649 - Silver Bow and Arrow (Equip Fairy +300)
        AddEffect("1649", Effect_1649_SilverBowAndArrow);

        // 1651 - Sinister Serpent (Return to hand)
        AddEffect("1651", Effect_1651_SinisterSerpent);

        // 1652 - Sixth Sense (Dice draw/mill)
        AddEffect("1652", Effect_1652_SixthSense);

        // 1653 - Skelengel (Flip Draw)
        AddEffect("1653", Effect_1653_Skelengel);

        // 1655 - Skill Drain (Negate face-up effects)
        AddEffect("1655", Effect_1655_SkillDrain);

        // 1656 - Skilled Dark Magician (Counters -> SS DM)
        AddEffect("1656", Effect_1656_SkilledDarkMagician);

        // 1657 - Skilled White Magician (Counters -> SS BB)
        AddEffect("1657", Effect_1657_SkilledWhiteMagician);

        // 1658 - Skull Archfiend of Lightning (Maintenance, Dice negate)
        AddEffect("1658", Effect_1658_SkullArchfiendOfLightning);

        // 1659 - Skull Dice (Dice debuff)
        AddEffect("1659", Effect_1659_SkullDice);

        // 1661 - Skull Guardian (Ritual)
        AddEffect("1661", Effect_1661_SkullGuardian);

        // 1662 - Skull Invitation (Burn on GY)
        AddEffect("1662", Effect_1662_SkullInvitation);

        // 1664 - Skull Knight #2 (Tribute -> SS copy)
        AddEffect("1664", Effect_1664_SkullKnight2);

        // 1665 - Skull Lair (Banish GY -> Destroy)
        AddEffect("1665", Effect_1665_SkullLair);

        // 1670 - Skull-Mark Ladybug (Heal on GY)
        AddEffect("1670", Effect_1670_SkullMarkLadybug);

        // 1674 - Skyscraper (Field: HERO +1000 on attack)
        AddEffect("1674", Effect_1674_Skyscraper);

        // 1675 - Slate Warrior (Flip buff, debuff killer)
        AddEffect("1675", Effect_1675_SlateWarrior);

        // 1679 - Smashing Ground (Destroy highest DEF)
        AddEffect("1679", Effect_1679_SmashingGround);

        // 1680 - Smoke Grenade of the Thief (Equip: Look hand discard)
        AddEffect("1680", Effect_1680_SmokeGrenadeOfTheThief);

        // 1681 - Snake Fang (Debuff DEF)
        AddEffect("1681", Effect_1681_SnakeFang);

        // 1683 - Snatch Steal (Equip Control, Opp heal)
        AddEffect("1683", Effect_1683_SnatchSteal);

        // 1684 - Sogen (Field Warrior/Beast-Warrior +200)
        AddEffect("1684", Effect_1684_Sogen);

        // 1686 - Solar Flare Dragon (Burn, Protect)
        AddEffect("1686", Effect_1686_SolarFlareDragon);

        // 1687 - Solar Ray (Burn per Light)
        AddEffect("1687", Effect_1687_SolarRay);

        // 1688 - Solemn Judgment (Counter: Pay half negate)
        AddEffect("1688", Effect_1688_SolemnJudgment);

        // 1689 - Solemn Wishes (Heal on draw)
        AddEffect("1689", Effect_1689_SolemnWishes);

        // 1691 - Solomon's Lawbook (Skip Standby)
        AddEffect("1691", Effect_1691_SolomonsLawbook);

        // 1692 - Sonic Bird (Search Ritual Spell)
        AddEffect("1692", Effect_1692_SonicBird);

        // 1694 - Sonic Jammer (Flip: No Spells)
        AddEffect("1694", Effect_1694_SonicJammer);

        // 1696 - Sorcerer of Dark Magic (Negate Traps)
        AddEffect("1696", Effect_1696_SorcererOfDarkMagic);

        // 1698 - Soul Absorption (Heal on banish)
        AddEffect("1698", Effect_1698_SoulAbsorption);

        // 1699 - Soul Demolition (Pay 500 Banish GY)
        AddEffect("1699", Effect_1699_SoulDemolition);

        // 1700 - Soul Exchange (Tribute Opp monster)
        AddEffect("1700", Effect_1700_SoulExchange);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1701 - 1800)
        // =========================================================================================

        // 1702 - Soul Release (Banish 5 GY)
        AddEffect("1702", Effect_1702_SoulRelease);

        // 1703 - Soul Resurrection (Revive Normal Defense)
        AddEffect("1703", Effect_1703_SoulResurrection);

        // 1704 - Soul Reversal (Recycle Flip)
        AddEffect("1704", Effect_1704_SoulReversal);

        // 1705 - Soul Rope (Pay 1000 SS Lv4)
        AddEffect("1705", Effect_1705_SoulRope);

        // 1706 - Soul Taker (Destroy & Heal Opp)
        AddEffect("1706", Effect_1706_SoulTaker);

        // 1708 - Soul of Purity and Light (SS Condition / Debuff)
        AddEffect("1708", Effect_1708_SoulOfPurityAndLight);

        // 1709 - Soul of the Pure (Gain 800)
        AddEffect("1709", Effect_1709_SoulOfThePure);

        // 1710 - Soul-Absorbing Bone Tower (Mill on Zombie SS)
        AddEffect("1710", Effect_1710_SoulAbsorbingBoneTower);

        // 1714 - Spark Blaster (Pos Change)
        AddEffect("1714", Effect_1714_SparkBlaster);

        // 1715 - Sparks (Burn 200)
        AddEffect("1715", Effect_1715_Sparks);

        // 1716 - Spatial Collapse (Limit 5)
        AddEffect("1716", Effect_1716_SpatialCollapse);

        // 1717 - Spear Cretin (Flip Mutual Revive)
        AddEffect("1717", Effect_1717_SpearCretin);

        // 1718 - Spear Dragon (Piercing / Defense)
        AddEffect("1718", Effect_1718_SpearDragon);

        // 1719 - Special Hurricane (Destroy SS)
        AddEffect("1719", Effect_1719_SpecialHurricane);

        // 1720 - Spell Absorption (Gain LP on Spell)
        AddEffect("1720", Effect_1720_SpellAbsorption);

        // 1721 - Spell Canceller (Negate Spells)
        AddEffect("1721", Effect_1721_SpellCanceller);

        // 1722 - Spell Economics (No LP Cost)
        AddEffect("1722", Effect_1722_SpellEconomics);

        // 1723 - Spell Purification (Destroy Continuous Spells)
        AddEffect("1723", Effect_1723_SpellPurification);

        // 1724 - Spell Reproduction (Recycle Spell)
        AddEffect("1724", Effect_1724_SpellReproduction);

        // 1725 - Spell Shattering Arrow (Destroy Face-up Spells)
        AddEffect("1725", Effect_1725_SpellShatteringArrow);

        // 1726 - Spell Shield Type-8 (Negate Spell)
        AddEffect("1726", Effect_1726_SpellShieldType8);

        // 1727 - Spell Vanishing (Negate & Banish)
        AddEffect("1727", Effect_1727_SpellVanishing);

        // 1728 - Spell of Pain (Redirect Damage)
        AddEffect("1728", Effect_1728_SpellOfPain);

        // 1729 - Spell-Stopping Statute (Negate Continuous Spell)
        AddEffect("1729", Effect_1729_SpellStoppingStatute);

        // 1730 - Spellbinding Circle (Lock)
        AddEffect("1730", Effect_1730_SpellbindingCircle);

        // 1731 - Spellbook Organization (Reorder)
        AddEffect("1731", Effect_1731_SpellbookOrganization);

        // 1733 - Sphinx Teleia (SS Condition / Burn)
        AddEffect("1733", Effect_1733_SphinxTeleia);

        // 1737 - Spiral Spear Strike (Piercing Gaia)
        AddEffect("1737", Effect_1737_SpiralSpearStrike);

        // 1738 - Spirit Barrier (No Battle Damage)
        AddEffect("1738", Effect_1738_SpiritBarrier);

        // 1739 - Spirit Caller (Flip SS Normal)
        AddEffect("1739", Effect_1739_SpiritCaller);

        // 1740 - Spirit Elimination (Banish Field)
        AddEffect("1740", Effect_1740_SpiritElimination);

        // 1745 - Spirit Reaper (Indestructible / Discard)
        AddEffect("1745", Effect_1745_SpiritReaper);

        // 1746 - Spirit Ryu (Discard Dragon Buff)
        AddEffect("1746", Effect_1746_SpiritRyu);

        // 1747 - Spirit of Flames (SS Banish Fire)
        AddEffect("1747", Effect_1747_SpiritOfFlames);

        // 1749 - Spirit of the Breeze (Gain LP)
        AddEffect("1749", Effect_1749_SpiritOfTheBreeze);

        // 1752 - Spirit of the Pharaoh (SS Zombies)
        AddEffect("1752", Effect_1752_SpiritOfThePharaoh);

        // 1753 - Spirit of the Pot of Greed (Draw Extra)
        AddEffect("1753", Effect_1753_SpiritOfThePotOfGreed);

        // 1755 - Spirit's Invitation (Bounce)
        AddEffect("1755", Effect_1755_SpiritsInvitation);

        // 1756 - Spiritual Earth Art - Kurogane (Swap Earth)
        AddEffect("1756", Effect_1756_SpiritualEarthArtKurogane);

        // 1757 - Spiritual Energy Settle Machine (Keep Spirits)
        AddEffect("1757", Effect_1757_SpiritualEnergySettleMachine);

        // 1758 - Spiritual Fire Art - Kurenai (Burn)
        AddEffect("1758", Effect_1758_SpiritualFireArtKurenai);

        // 1759 - Spiritual Water Art - Aoi (Hand Destruction)
        AddEffect("1759", Effect_1759_SpiritualWaterArtAoi);

        // 1760 - Spiritual Wind Art - Miyabi (Spin)
        AddEffect("1760", Effect_1760_SpiritualWindArtMiyabi);

        // 1761 - Spiritualism (Bounce S/T)
        AddEffect("1761", Effect_1761_Spiritualism);

        // 1762 - Spring of Rebirth (Gain LP on Bounce)
        AddEffect("1762", Effect_1762_SpringOfRebirth);

        // 1764 - Stamping Destruction (Destroy S/T Burn)
        AddEffect("1764", Effect_1764_StampingDestruction);

        // 1765 - Star Boy (Field Water +500 Fire -400)
        AddEffect("1765", Effect_1765_StarBoy);

        // 1766 - Statue of the Wicked (Token)
        AddEffect("1766", Effect_1766_StatueOfTheWicked);

        // 1767 - Staunch Defender (Forced Attack)
        AddEffect("1767", Effect_1767_StaunchDefender);

        // 1768 - Stealth Bird (Burn / Flip Down)
        AddEffect("1768", Effect_1768_StealthBird);

        // 1770 - Steamroid (Battle Stats)
        AddEffect("1770", Effect_1770_Steamroid);

        // 1773 - Steel Scorpion (Destroy non-Machine)
        AddEffect("1773", Effect_1773_SteelScorpion);

        // 1774 - Steel Shell (Equip Water +400/-200)
        AddEffect("1774", Effect_1774_SteelShell);

        // 1775 - Stim-Pack (Equip +700 / Decay)
        AddEffect("1775", Effect_1775_StimPack);

        // 1780 - Stone Statue of the Aztecs (Double Battle Damage)
        AddEffect("1780", Effect_1780_StoneStatueOfTheAztecs);

        // 1781 - Stop Defense (Change to Attack)
        AddEffect("1781", Effect_1781_StopDefense);

        // 1782 - Stray Lambs (Tokens)
        AddEffect("1782", Effect_1782_StrayLambs);

        // 1783 - Strike Ninja (Dodge)
        AddEffect("1783", Effect_1783_StrikeNinja);

        // 1784 - Stronghold the Moving Fortress (Trap Monster)
        AddEffect("1784", Effect_1784_StrongholdTheMovingFortress);

        // 1786 - Stumbling (Defense on Summon)
        AddEffect("1786", Effect_1786_Stumbling);

        // 1788 - Suijin (Zero ATK)
        AddEffect("1788", Effect_1788_Suijin);

        // 1790 - Summoner Monk (Discard Spell SS)
        AddEffect("1790", Effect_1790_SummonerMonk);

        // 1791 - Summoner of Illusions (Flip SS Fusion)
        AddEffect("1791", Effect_1791_SummonerOfIllusions);

        // 1792 - Super Rejuvenation (Draw Dragons)
        AddEffect("1792", Effect_1792_SuperRejuvenation);

        // 1793 - Super Robolady
        AddEffect("1793", Effect_1793_SuperRobolady);

        // 1794 - Super Roboyarou
        AddEffect("1794", Effect_1794_SuperRoboyarou);

        // 1795 - Super War-Lion (Ritual)
        AddEffect("1795", Effect_1795_SuperWarLion);

        // 1796 - Supply (Recycle Fusion Material)
        AddEffect("1796", Effect_1796_Supply);

        // 1798 - Susa Soldier (Halve Damage)
        AddEffect("1798", Effect_1798_SusaSoldier);

        // 1799 - Swamp Battleguard (Buff)
        AddEffect("1799", Effect_1799_SwampBattleguard);

        // 1800 - Swarm of Locusts (Destroy S/T / Flip Down)
        AddEffect("1800", Effect_1800_SwarmOfLocusts);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1801 - 1850)
        // =========================================================================================

        // 1801 - Swarm of Scarabs (Flip Destroy Monster)
        AddEffect("1801", Effect_1801_SwarmOfScarabs);

        // 1802 - Swift Gaia the Fierce Knight (NS No Tribute)
        AddEffect("1802", Effect_1802_SwiftGaiaTheFierceKnight);

        // 1804 - Sword Hunter (Equip destroyed)
        AddEffect("1804", Effect_1804_SwordHunter);

        // 1806 - Sword of Dark Destruction (Equip Dark +400/-200)
        AddEffect("1806", Effect_1806_SwordOfDarkDestruction);

        // 1807 - Sword of Deep-Seated (Equip +500/500, Recycle)
        AddEffect("1807", Effect_1807_SwordOfDeepSeated);

        // 1808 - Sword of Dragon's Soul (Equip Warrior +700, Destroy Dragon)
        AddEffect("1808", Effect_1808_SwordOfDragonsSoul);

        // 1809 - Sword of the Soul-Eater (Equip Normal Lv3-, Buff)
        AddEffect("1809", Effect_1809_SwordOfTheSoulEater);

        // 1810 - Swords of Concealing Light (Face-down Defense)
        AddEffect("1810", Effect_1810_SwordsOfConcealingLight);

        // 1811 - Swords of Revealing Light (Face-up, No Attack)
        AddEffect("1811", Effect_1811_SwordsOfRevealingLight);

        // 1812 - Swordsman from a Distant Land (Destroy monster after battle)
        AddEffect("1812", Effect_1812_SwordsmanFromADistantLand);

        // 1816 - System Down (Pay 1000 Banish Machines)
        AddEffect("1816", Effect_1816_SystemDown);

        // 1817 - T.A.D.P.O.L.E. (Search copies)
        AddEffect("1817", Effect_1817_TADPOLE);

        // 1818 - Tactical Espionage Expert (No Traps on Summon)
        AddEffect("1818", Effect_1818_TacticalEspionageExpert);

        // 1819 - Tailor of the Fickle (Switch Equip)
        AddEffect("1819", Effect_1819_TailorOfTheFickle);

        // 1820 - Tainted Wisdom (Shuffle Deck)
        AddEffect("1820", Effect_1820_TaintedWisdom);

        // 1823 - Talisman of Spell Sealing (Lock Spells)
        AddEffect("1823", Effect_1823_TalismanOfSpellSealing);

        // 1824 - Talisman of Trap Sealing (Lock Traps)
        AddEffect("1824", Effect_1824_TalismanOfTrapSealing);

        // 1827 - Taunt (Force Attack Target)
        AddEffect("1827", Effect_1827_Taunt);

        // 1829 - Temple of the Kings (Trap turn set, SS Serket)
        AddEffect("1829", Effect_1829_TempleOfTheKings);

        // 1833 - Terraforming (Search Field Spell)
        AddEffect("1833", Effect_1833_Terraforming);

        // 1834 - Terrorking Archfiend (Negate, Maintenance)
        AddEffect("1834", Effect_1834_TerrorkingArchfiend);

        // 1836 - Teva (No Attack Next Turn)
        AddEffect("1836", Effect_1836_Teva);

        // 1839 - The A. Forces (Buff Warriors)
        AddEffect("1839", Effect_1839_TheAForces);

        // 1840 - The Agent of Creation - Venus (Pay 500 SS Shine Ball)
        AddEffect("1840", Effect_1840_TheAgentOfCreationVenus);

        // 1841 - The Agent of Force - Mars (Immune Spell, ATK=LP Diff)
        AddEffect("1841", Effect_1841_TheAgentOfForceMars);

        // 1842 - The Agent of Judgment - Saturn (Tribute Burn)
        AddEffect("1842", Effect_1842_TheAgentOfJudgmentSaturn);

        // 1843 - The Agent of Wisdom - Mercury (Draw if hand empty)
        AddEffect("1843", Effect_1843_TheAgentOfWisdomMercury);

        // 1846 - The Big March of Animals (Buff Beasts)
        AddEffect("1846", Effect_1846_TheBigMarchOfAnimals);

        // 1847 - The Bistro Butcher (Draw 2 for Opp)
        AddEffect("1847", Effect_1847_TheBistroButcher);

        // 1848 - The Cheerful Coffin (Discard)
        AddEffect("1848", Effect_1848_TheCheerfulCoffin);

        // 1849 - The Creator Incarnate (Tribute SS Creator)
        AddEffect("1849", Effect_1849_TheCreatorIncarnate);

        // 1850 - The Dark - Hex-Sealed Fusion (Fusion Sub)
        AddEffect("1850", Effect_1850_TheDarkHexSealedFusion);

        // 1851 - The Dark Door (One Attack)
        AddEffect("1851", c => Debug.Log("The Dark Door: Apenas 1 ataque por turno."));

        // 1853 - The Dragon's Bead (Discard Negate Trap)
        AddEffect("1853", Effect_1853_TheDragonsBead);

        // 1856 - The Earth - Hex-Sealed Fusion (Fusion Sub)
        AddEffect("1856", Effect_1856_TheEarthHexSealedFusion);

        // 1857 - The Emperor's Holiday (Negate Equips)
        AddEffect("1857", Effect_1857_TheEmperorsHoliday);

        // 1858 - The End of Anubis (Negate GY)
        AddEffect("1858", Effect_1858_TheEndOfAnubis);

        // 1859 - The Eye of Truth (Reveal Hand, Heal)
        AddEffect("1859", Effect_1859_TheEyeOfTruth);

        // 1860 - The Fiend Megacyber (SS Condition)
        AddEffect("1860", Effect_1860_TheFiendMegacyber);

        // 1861 - The First Sarcophagus (SS Spirit of Pharaoh)
        AddEffect("1861", Effect_1861_TheFirstSarcophagus);

        // 1862 - The Flute of Summoning Dragon (SS 2 Dragons)
        AddEffect("1862", Effect_1862_TheFluteOfSummoningDragon);

        // 1863 - The Forceful Sentry (Look Hand, Return to Deck)
        AddEffect("1863", Effect_1863_TheForcefulSentry);

        // 1864 - The Forgiving Maiden (Tribute Recycle)
        AddEffect("1864", Effect_1864_TheForgivingMaiden);

        // 1866 - The Graveyard in the Fourth Dimension (Recycle LV)
        AddEffect("1866", Effect_1866_TheGraveyardInTheFourthDimension);

        // 1868 - The Hunter with 7 Weapons (Declare Type +1000)
        AddEffect("1868", Effect_1868_TheHunterWith7Weapons);

        // 1870 - The Immortal of Thunder (Flip +3000 LP, Lose 5000)
        AddEffect("1870", Effect_1870_TheImmortalOfThunder);

        // 1871 - The Inexperienced Spy (Look Hand)
        AddEffect("1871", Effect_1871_TheInexperiencedSpy);

        // 1873 - The Kick Man (Equip on SS)
        AddEffect("1873", Effect_1873_TheKickMan);

        // 1874 - The Last Warrior from Another Planet (Lock Summons)
        AddEffect("1874", Effect_1874_TheLastWarriorFromAnotherPlanet);

        // 1875 - The Law of the Normal (Reset, Destroy)
        AddEffect("1875", Effect_1875_TheLawOfTheNormal);

        // 1876 - The Legendary Fisherman (Immune Umi)
        AddEffect("1876", Effect_1876_TheLegendaryFisherman);

        // 1877 - The Light - Hex-Sealed Fusion (Fusion Sub)
        AddEffect("1877", Effect_1877_TheLightHexSealedFusion);

        // 1878 - The Little Swordsman of Aile (Tribute +700)
        AddEffect("1878", Effect_1878_TheLittleSwordsmanOfAile);

        // 1879 - The Mask of Remnants (Shuffle/Equip)
        AddEffect("1879", Effect_1879_TheMaskOfRemnants);

        // 1880 - The Masked Beast (Ritual)
        AddEffect("1880", Effect_1880_TheMaskedBeast);

        // 1883 - The Puppet Magic of Dark Ruler (Banish -> SS Fiend)
        AddEffect("1883", Effect_1883_ThePuppetMagicOfDarkRuler);

        // 1884 - The Regulation of Tribe (Type Lock)
        AddEffect("1884", Effect_1884_TheRegulationOfTribe);

        // 1885 - The Reliable Guardian (Quick-Play +700 DEF)
        AddEffect("1885", Effect_1885_TheReliableGuardian);

        // 1886 - The Rock Spirit (SS Banish Earth)
        AddEffect("1886", Effect_1886_TheRockSpirit);

        // 1887 - The Sanctuary in the Sky (No Fairy Damage)
        AddEffect("1887", Effect_1887_TheSanctuaryInTheSky);

        // 1889 - The Secret of the Bandit (Discard on Damage)
        AddEffect("1889", Effect_1889_TheSecretOfTheBandit);

        // 1890 - The Selection (Pay 1000 Negate Summon)
        AddEffect("1890", Effect_1890_TheSelection);

        // 1892 - The Shallow Grave (SS Face-down)
        AddEffect("1892", Effect_1892_TheShallowGrave);

        // 1894 - The Spell Absorbing Life (Flip Face-up, Heal)
        AddEffect("1894", Effect_1894_TheSpellAbsorbingLife);

        // 1896 - The Stern Mystic (Flip Reveal)
        AddEffect("1896", Effect_1896_TheSternMystic);

        // 1898 - The Thing in the Crater (SS Pyro)
        AddEffect("1898", Effect_1898_TheThingInTheCrater);

        // 1900 - The Tricky (SS Discard)
        AddEffect("1900", Effect_1900_TheTricky);

        // =========================================================================================
        // LÓGICA PARA AS CARTAS (ID 1901 - 2000)
        // =========================================================================================

        // 1901 - The Trojan Horse (2 Tributes Earth)
        AddEffect("1901", Effect_1901_TheTrojanHorse);

        // 1902 - The Unfriendly Amazon (Maintenance)
        AddEffect("1902", Effect_1902_TheUnfriendlyAmazon);

        // 1903 - The Unhappy Girl (Lock Attack/Pos)
        AddEffect("1903", Effect_1903_TheUnhappyGirl);

        // 1904 - The Unhappy Maiden (End Battle Phase)
        AddEffect("1904", Effect_1904_TheUnhappyMaiden);

        // 1906 - The Warrior Returning Alive (Recycle Warrior)
        AddEffect("1906", Effect_1906_TheWarriorReturningAlive);

        // 1907 - The Wicked Dreadroot (Halve Stats)
        AddEffect("1907", Effect_1907_TheWickedDreadroot);

        // 1908 - The Wicked Worm Beast (Return to Hand)
        AddEffect("1908", Effect_1908_TheWickedWormBeast);

        // 1909 - Theban Nightmare (Buff if empty)
        AddEffect("1909", Effect_1909_ThebanNightmare);

        // 1910 - Theinen the Great Sphinx (SS +3000)
        AddEffect("1910", Effect_1910_TheinenTheGreatSphinx);

        // 1911 - Thestalos the Firestorm Monarch (Discard Burn)
        AddEffect("1911", Effect_1911_ThestalosTheFirestormMonarch);

        // 1913 - Thousand Energy (Buff Lv2 Normal)
        AddEffect("1913", Effect_1913_ThousandEnergy);

        // 1914 - Thousand Knives (Destroy if DM)
        AddEffect("1914", Effect_1914_ThousandKnives);

        // 1915 - Thousand Needles (Destroy Attacker)
        AddEffect("1915", Effect_1915_ThousandNeedles);

        // 1917 - Thousand-Eyes Restrict (Absorb, Lock)
        AddEffect("1917", Effect_1917_ThousandEyesRestrict);

        // 1918 - Threatening Roar (No Attack)
        AddEffect("1918", Effect_1918_ThreateningRoar);

        // 1921 - Throwstone Unit (Tribute Warrior -> Destroy)
        AddEffect("1921", Effect_1921_ThrowstoneUnit);

        // 1922 - Thunder Crash (Destroy Own -> Burn)
        AddEffect("1922", Effect_1922_ThunderCrash);

        // 1923 - Thunder Dragon (Discard -> Add 2)
        AddEffect("1923", Effect_1923_ThunderDragon);

        // 1925 - Thunder Nyan Nyan (Destroy if non-Light)
        AddEffect("1925", Effect_1925_ThunderNyanNyan);

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
    }
}
