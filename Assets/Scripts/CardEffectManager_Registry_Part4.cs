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
        AddEffect("1526", Effect_1526_ReverseTrap);

        // 1527 - Revival Jam (Pay 1000 Revive)
        AddEffect("1527", Effect_1527_RevivalJam);

        // 1528 - Revival of Dokurorider (Ritual)
        AddEffect("1528", Effect_1528_RevivalOfDokurorider);

        // 1532 - Rigorous Reaver (Flip Discard)
        AddEffect("1532", Effect_1532_RigorousReaver);

        // 1533 - Ring of Destruction (Destroy & Burn)
        AddEffect("1533", Effect_1533_RingOfDestruction);

        // 1534 - Ring of Magnetism (Equip -500, Taunt)
        AddEffect("1534", Effect_1534_RingOfMagnetism);

        // 1535 - Riryoku (Halve & Add)
        AddEffect("1535", Effect_1535_Riryoku);

        // 1536 - Riryoku Field (Negate Spell)
        AddEffect("1536", Effect_1536_RiryokuField);

        // 1537 - Rising Air Current (Field Wind +500/-400)
        AddEffect("1537", Effect_1537_RisingAirCurrent);

        // 1538 - Rising Energy (Discard -> +1500)
        AddEffect("1538", Effect_1538_RisingEnergy);

        // 1539 - Rite of Spirit (Revive GK)
        AddEffect("1539", Effect_1539_RiteOfSpirit);

        // 1540 - Ritual Weapon (Equip Ritual Lv6- +1500)
        AddEffect("1540", Effect_1540_RitualWeapon);

        // 1541 - Rivalry of Warlords (Type Lock)
        AddEffect("1541", Effect_1541_RivalryOfWarlords);

        // 1543 - Robbin' Goblin (Damage -> Discard)
        AddEffect("1543", Effect_1543_RobbinGoblin);

        // 1544 - Robbin' Zombie (Damage -> Mill)
        AddEffect("1544", Effect_1544_RobbinZombie);

        // 1548 - Roc from the Valley of Haze (Recycle)
        AddEffect("1548", Effect_1548_RocFromTheValleyOfHaze);

        // 1549 - Rock Bombardment (Mill Rock -> 500 dmg)
        AddEffect("1549", Effect_1549_RockBombardment);

        // 1553 - Rocket Jumper (Direct Attack)
        AddEffect("1553", Effect_1553_RocketJumper);

        // 1554 - Rocket Warrior (Battle protection)
        AddEffect("1554", Effect_1554_RocketWarrior);

        // 1555 - Rod of Silence - Kay'est (Equip +500 DEF)
        AddEffect("1555", Effect_1555_RodOfSilenceKayest);

        // 1556 - Rod of the Mind's Eye (Equip 1000 dmg)
        AddEffect("1556", Effect_1556_RodOfTheMindsEye);

        // 1559 - Rope of Life (Discard -> Revive +800)
        AddEffect("1559", Effect_1559_RopeOfLife);

        // 1561 - Roulette Barrel (Dice Destroy)
        AddEffect("1561", Effect_1561_RouletteBarrel);

        // 1562 - Royal Command (Negate Flip)
        AddEffect("1562", Effect_1562_RoyalCommand);

        // 1563 - Royal Decree (Negate Traps)
        AddEffect("1563", Effect_1563_RoyalDecree);

        // 1565 - Royal Keeper (Flip +300)
        AddEffect("1565", Effect_1565_RoyalKeeper);

        // 1566 - Royal Magical Library (Counters -> Draw)
        AddEffect("1566", Effect_1566_RoyalMagicalLibrary);

        // 1567 - Royal Oppression (Pay 800 Negate SS)
        AddEffect("1567", Effect_1567_RoyalOppression);

        // 1568 - Royal Surrender (Negate Continuous Trap)
        AddEffect("1568", Effect_1568_RoyalSurrender);

        // 1569 - Royal Tribute (Necrovalley Discard)
        AddEffect("1569", Effect_1569_RoyalTribute);

        // 1571 - Rush Recklessly (Target +700)
        AddEffect("1571", Effect_1571_RushRecklessly);

        // 1572 - Ryu Kokki (Destroy Warrior/Spellcaster)
        AddEffect("1572", Effect_1572_RyuKokki);

        // 1573 - Ryu Senshi (Pay 1000 Negate Trap)
        AddEffect("1573", Effect_1573_RyuSenshi);

        // 1575 - Ryu-Kishin Clown (Change Pos)
        AddEffect("1575", Effect_1575_RyuKishinClown);

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
        AddEffect("1588", c => Debug.Log("Sangan: Busca monstro 1500- ATK."));

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
        AddEffect("1595", c => Debug.Log("Scapegoat: Invoca 4 Sheep Tokens."));

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
        AddEffect("1683", c => Debug.Log("Snatch Steal: Toma controle. Oponente ganha 1000 LP por turno."));

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
    }
}
