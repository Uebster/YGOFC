using UnityEngine;

public partial class CardEffectManager
{
    void InitializeEffects_Part3()
    {
        /*
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
        AddEffect("1120", c => Debug.Log("Magic Cylinder: Nega ataque e causa dano igual ATK."));

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
        AddEffect("1268", Effect_MonsterReborn);

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
        */
    }
}
