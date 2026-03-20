import json
import os
import random
import re
import tkinter as tk
from tkinter import filedialog

# --- CONFIGURAÇÃO DE BALANCEAMENTO ---
# Define quais pools estão disponíveis para cada Ato (1-10)
ACT_POOL_RANGES = {
    1: (1.1, 1.5), # Início: Cartas muito fracas
    2: (1.3, 2.1), # Monstros de 1200-1400 ATK
    3: (2.1, 2.5), # Monstros de 1500-1700 ATK
    4: (2.4, 3.2), # Introdução de efeitos melhores
    5: (3.1, 3.5), # Monstros 1800+ e Tributos úteis
    6: (3.3, 4.1), # Staples clássicas começam a aparecer
    7: (3.5, 4.3), # Decks competitivos antigos
    8: (4.1, 4.5), # Cartas poderosas
    9: (4.3, 5.2), # Quase meta
    10: (4.5, 5.5) # God Tier / Banlist
}

# --- CONFIGURAÇÃO DE CARTAS ASSINATURA & FORÇADAS ---
# O primeiro ID da lista se torna a Carta S+ (Boss), as demais também são forçadas no deck.
FORCED_CARDS = {
    "kaiba": ["0217", "0217", "0217", "1183"], # Blue-Eyes White Dragon x3, Master of Oz
    "yugi": ["0419"], # Dark Magician
    "joey": ["1504"], # Red-Eyes B. Dragon
    "pegasus": ["1513", "1950"], # Relinquished, Toon World
    "mai": ["0867", "0867", "0867", "0871"], # Harpie Lady x3, Sisters
    "weevil": ["0825", "0313", "1422"], # Great Moth combo
    "rex": ["1995"], # Two-Headed King Rex
    "mako": ["1876"], # The Legendary Fisherman
    "bandit": ["0139"], # Barrel Dragon
    "keith": ["0139"],
    "marik": ["1060"], # Lava Golem
    "strings": ["1527", "0965"], # Revival Jam, Jam Breeding
    "bakura": ["0427"], # Dark Necrofear
    "ishizu": ["0613"], # Exchange of the Spirit
    "odion": ["0590"], # Embodiment of Apophis
    "noah": ["1636"], # Shinato
    "gozaburo": ["0617"], # Exodia Necross
    "rare_hunter": ["0618"], # Exodia the Forbidden One
    "arkana": ["0420"], # Dark Magician Girl
    "shadi": ["1233"], # Millennium Shield
    "heishin": ["2139"], # Zera the Mant
    "isis": ["1612"] # Senju
}

# --- THEMES & ARCHETYPES (PROFILES INTELIGENTES) ---
THEMES = {
    "mako": {"attribute": "Water", "race": "Aqua", "core": "Umi"},
    "ocean": {"attribute": "Water", "race": "Aqua", "core": "Umi"},
    "weevil": {"attribute": "Earth", "race": "Insect", "core": "Insect"},
    "forest": {"attribute": "Earth", "race": "Insect", "core": "Insect"},
    "rex": {"attribute": "Earth", "race": "Dinosaur", "core": "Dinosaur"},
    "mountain": {"attribute": "Wind", "race": "Dragon", "core": "Harpie"},
    "mai": {"attribute": "Wind", "race": "Winged Beast", "core": "Harpie"},
    "joey": {"attribute": "Earth", "race": "Warrior", "core": "Warrior"},
    "meadow": {"attribute": "Earth", "race": "Warrior", "core": "Warrior"},
    "yugi": {"attribute": "Dark", "race": "Spellcaster", "core": "Spellcaster"},
    "arkana": {"attribute": "Dark", "race": "Spellcaster", "core": "Spellcaster"},
    "bakura": {"attribute": "Dark", "race": "Fiend", "core": "DestinyBoard"},
    "marik": {"attribute": "Dark", "race": "Fiend", "core": "Burn"},
    "keith": {"attribute": "Dark", "race": "Machine", "core": "Machine"},
    "machine": {"attribute": "Dark", "race": "Machine", "core": "Machine"},
    "ishizu": {"attribute": "Earth", "race": "Fairy", "core": "Fairy"},
    "rare_hunter": {"attribute": "Dark", "race": "Spellcaster", "core": "Exodia"},
    "desert": {"attribute": "Earth", "race": "Zombie", "core": "Zombie"},
    "labyrinth": {"attribute": "Dark", "race": "Fiend", "core": "Fiend"},
    "pegasus": {"attribute": "Dark", "race": "Spellcaster", "core": "Toon"}
}

# --- PACOTES (CORES) PRÉ-DEFINIDOS ---
# Repetir IDs garante que o deck builder priorize essas cópias (dentro do limite)
CORES = {
    "Exodia": ["0618", "1061", "1062", "1530", "1531", "0324", "0617"],
    "Umi": ["2015", "2015", "2015", "0013", "0013", "1953", "1953", "0053", "0053", "1876", "1365", "0682"], 
    "Burn": ["1606", "1606", "0985", "0985", "0235", "1344", "0442", "1967", "0654"], 
    "Insect": ["0947", "0947", "0948", "1404", "1404", "0951", "1329", "0680", "0680"], 
    "Dinosaur": ["2014", "2014", "2002", "1995", "1995", "0605", "1117", "0407"], 
    "Zombie": ["0227", "0227", "2031", "1508", "1508", "0259", "1544", "1307", "0480"], 
    "Warrior": ["1509", "1509", "1163", "1163", "0688", "0318", "0616", "0048", "1684", "1684"], 
    "DestinyBoard": ["0482", "0482", "1741", "1742", "1743", "1744", "1811", "1811"], 
    "Toon": ["1950", "1950", "1950", "1944", "1948", "1942", "1949", "1949", "1947", "0215"], 
    "Spellcaster": ["1126", "1126", "1138", "0240", "1656", "1656", "0422", "1790", "2125"], 
    "Machine": ["1088", "1088", "0879", "0975", "1113", "0359", "1507"], 
    "Harpie": ["0867", "0867", "0867", "0570", "0570", "0874", "0874", "0871", "0873", "0366", "0366"], 
    "Gravekeeper": ["1324", "1324", "1324", "0810", "0810", "0805", "0809", "0803", "0812"], 
    "Fiend": ["0427", "0750", "1658", "0091", "1197", "0433", "2125", "2125"], 
    "Fairy": ["1887", "1887", "1887", "0092", "1840", "1317", "1639", "1639", "1284"]
}

STAPLES_TIER_1 = ["1962", "0666", "2047", "0457", "1781", "1483"] 
STAPLES_TIER_2 = ["1679", "0555", "1582", "1120", "0228", "0602"] 
STAPLES_TIER_3 = ["1480", "1251", "1447", "0881", "1318", "0287"] 

# --- MOTOR DE DEPENDÊNCIAS EXATAS ---
EXACT_DEPENDENCIES = {
    "0618": ["1061", "1062", "1530", "1531"], 
    "0617": ["0618", "1061", "1062", "1530", "1531", "0324"], 
    "1513": ["0186"], 
    "0188": ["0187"], 
    "1137": ["0190"], 
    "0215": ["1950"], 
    "1942": ["1950"], 
    "0871": ["0570"], 
    "1417": ["1422", "0313"], 
    "0825": ["1422", "0313"], 
    "1175": ["1879"], 
    "1880": ["0354"], 
    "0214": ["0216", "0217", "0217", "0217", "1444"], 
    "0421": ["0419", "1024"], 
}

# --- BANLIST (ID -> Limit) ---
# 0 = Forbidden, 1 = Limited, 2 = Semi-Limited
BANLIST = {
    "0289": 0, "1588": 0, "2097": 0, "2128": 0, "0414": 0, "0465": 0, "0791": 0,
    "0872": 0, "1268": 0, "1480": 0, "2020": 0, "0932": 0, "1251": 0, "0639": 0,
    "0363": 0, "1134": 0, "0370": 0, "1252": 0, "0485": 0, "0287": 0, "0321": 0, "1863": 0,
    
    "0189": 1, "0188": 1, "0293": 1, "0240": 1, "0975": 1, "1973": 1, "1651": 1,
    "0616": 1, "0378": 1, "0388": 1, "0058": 1, "0097": 1, "0098": 1, "0166": 1,
    "0288": 1, "0944": 1, "1507": 1, "1989": 1, "2031": 1, "1277": 1, "1457": 1,
    "0422": 1, "1513": 1, "1790": 1, "1517": 1, "1447": 1, "0881": 1, "1683": 1,
    "1453": 1, "0259": 1, "1533": 1, "1955": 1, "1120": 1, "0275": 1, "1499": 1,
    "1811": 1, "0228": 1, "1318": 1, "0757": 1, "1563": 1, "1119": 1, "1397": 1,
    "1462": 1, "1138": 1, "1170": 1, "1236": 1, "0237": 1, "1088": 1, "1200": 1,
    "1929": 1, "2050": 1, "1610": 1, "0264": 1, "0497": 1, "1523": 1,
    "0618": 1, "1061": 1, "1062": 1, "1530": 1, "1531": 1,
    
    "0338": 2, "1055": 2, "1162": 2, "1163": 2, "1278": 2, "1353": 2, "1509": 2,
    "2024": 2, "0359": 2, "0011": 2, "0602": 2, "1209": 2, "1077": 2, "0817": 2,
    "1245": 2, "0786": 2, "1329": 2, "0077": 2, "0460": 2, "1604": 2, "1498": 2,
}

ALLOW_FORBIDDEN = True 

# --- FUNÇÕES AUXILIARES ---

def load_json(path):
    if not os.path.exists(path): return []
    with open(path, 'r', encoding='utf-8') as f: return json.load(f)

def get_act_from_id(char_id):
    match = re.match(r"(\d+)_", char_id)
    if match:
        num = int(match.group(1))
        return max(1, min(10, (num - 1) // 10 + 1))
    return 1

def build_dependency_map(cards):
    dependency_map = {}
    name_to_id = {c["name"].lower(): c["id"] for c in cards}
    
    for card in cards:
        desc = card.get("description", "").lower()
        for other_name, other_id in name_to_id.items():
            if other_id == card["id"]: continue
            if other_name in desc:
                if card["id"] not in dependency_map: dependency_map[card["id"]] = []
                if other_id not in dependency_map[card["id"]]: dependency_map[card["id"]].append(other_id)
                    
        if "Fusion" in card["type"]:
            poly_id = name_to_id.get("polymerization")
            if poly_id and card["id"] not in dependency_map: dependency_map[card["id"]] = []
            if poly_id and poly_id not in dependency_map[card["id"]]: dependency_map[card["id"]].append(poly_id)

    return dependency_map

def get_pool_candidates(min_p, max_p, cards_by_pool, all_cards_map):
    candidates = []
    for p, ids in cards_by_pool.items():
        if min_p <= p <= max_p:
            for cid in ids:
                if cid in all_cards_map and "Token" not in all_cards_map[cid]["type"]:
                    candidates.append(cid)
    return candidates

def format_deck_list_custom(deck_list):
    if not deck_list: return "[]"
    lines = []
    chunk_size = 10
    for i in range(0, len(deck_list), chunk_size):
        chunk = deck_list[i:i + chunk_size]
        quoted_chunk = [f'"{x}"' for x in chunk]
        lines.append(f"      {', '.join(quoted_chunk)}")
    return "[\n" + ",\n".join(lines) + "\n    ]"

# --- O DECK BUILDER ---

def generate_deck(char_id, act, difficulty_modifier, cards_by_pool, all_cards_map, dependency_map, forced_cards=None, unique_pool=None):
    main_deck = []
    extra_deck = []
    
    theme = None
    for k, v in THEMES.items():
        if k in char_id.lower():
            theme = v
            break

    # Helper inteligente para blindar o cap de 40 cartas e injetar dependências
    def add_card(cid, is_forced=False):
        if not cid or cid not in all_cards_map: return False
        c_data = all_cards_map[cid]
        
        limit = BANLIST.get(cid, 3)
        if ALLOW_FORBIDDEN and limit == 0: limit = 1
        
        if not is_forced and (main_deck.count(cid) + extra_deck.count(cid)) >= limit: 
            return False
            
        if "Fusion" in c_data["type"] or "Synchro" in c_data["type"] or "Xyz" in c_data["type"]:
            if len(extra_deck) < 15: 
                extra_deck.append(cid)
                if "1444" not in main_deck and len(main_deck) < 40:
                    main_deck.append("1444")
                return True
            return False
        else:
            if len(main_deck) < 40 or is_forced:
                main_deck.append(cid)
                if cid in EXACT_DEPENDENCIES:
                    for dep_id in EXACT_DEPENDENCIES[cid]:
                        if main_deck.count(dep_id) == 0 and (len(main_deck) < 40 or is_forced):
                            add_card(dep_id, True)
                return True
            return False

    if forced_cards:
        for cid in forced_cards:
            add_card(cid, True)
            if difficulty_modifier in ["B", "C"]: add_card(cid, True)
    if unique_pool:
        for cid in unique_pool:
            add_card(cid, True)

    if theme and theme.get("core") in CORES:
        for cid in CORES[theme["core"]]:
            add_card(cid)
            if difficulty_modifier in ["B", "C"]: add_card(cid) 
            
    staples = STAPLES_TIER_1
    if act >= 4: staples = STAPLES_TIER_2
    if act >= 8: staples = STAPLES_TIER_3
    for cid in random.sample(staples, min(len(staples), 5)): add_card(cid)
        
    min_p, max_p = ACT_POOL_RANGES.get(act, (1.1, 1.5))
    if difficulty_modifier == "B": min_p += 0.2; max_p += 0.2
    if difficulty_modifier == "C": min_p += 0.4; max_p += 0.5
    
    def fetch_cards(mn, mx):
        ml, mh, sp, tr = [], [], [], []
        for pool_val, ids in cards_by_pool.items():
            if mn <= pool_val <= mx:
                for cid in ids:
                    if cid not in all_cards_map: continue
                    c = all_cards_map[cid]
                    if "Monster" in c["type"]:
                        if c.get("level", 1) <= 4: ml.append(cid)
                        else: mh.append(cid)
                    elif "Spell" in c["type"]:
                        # Previne Field Spells intrusas
                        if c.get("property") == "Field":
                            continue
                        sp.append(cid)
                    elif "Trap" in c["type"]: tr.append(cid)
        return ml, mh, sp, tr

    monsters_low, monsters_high, spells, traps = fetch_cards(min_p, max_p)
    search_min = min_p
    while len(monsters_low) < 15 and search_min > 1.0:
        search_min -= 0.5
        ml, _, _, _ = fetch_cards(search_min, max_p)
        monsters_low = list(set(monsters_low + ml))
            
    def weighted_choice(pool, count):
        if not pool: return []
        weights = []
        for cid in pool:
            w = 1.0
            if theme:
                c = all_cards_map[cid]
                if "Monster" in c["type"]:
                    if c.get("race") == theme["race"]: w += 5.0
                    if c.get("attribute") == theme["attribute"]: w += 3.0
                elif "Spell" in c["type"] and c.get("property") == "Equip":
                    desc = c.get("description", "").lower()
                    if theme["race"].lower() in desc:
                        w += 4.0
                    else:
                        w = 0.1
            weights.append(w)
        return random.choices(pool, weights=weights, k=count)

    monsters_in_deck = sum(1 for cid in main_deck if "Monster" in all_cards_map[cid]["type"])
    spells_in_deck = sum(1 for cid in main_deck if "Spell" in all_cards_map[cid]["type"])
    traps_in_deck = sum(1 for cid in main_deck if "Trap" in all_cards_map[cid]["type"])
    
    need_low = max(0, 16 - monsters_in_deck)
    need_high = max(0, 4)
    need_spell = max(0, 10 - spells_in_deck)
    need_trap = max(0, 10 - traps_in_deck)
    
    fillers = []
    fillers.extend(weighted_choice(monsters_low, need_low))
    fillers.extend(weighted_choice(monsters_high, need_high))
    fillers.extend(weighted_choice(spells, need_spell))
    fillers.extend(weighted_choice(traps, need_trap))
    
    if difficulty_modifier in ["B", "C"]: fillers.extend(STAPLES_TIER_3 + STAPLES_TIER_2)
    else: fillers.extend(STAPLES_TIER_1)
        
    for cid in fillers:
        if len(main_deck) >= 40: break
        add_card(cid)

    fallback_attempts = 0
    safe_pool = monsters_low if len(monsters_low) > 0 else STAPLES_TIER_1
    while len(main_deck) < 40 and fallback_attempts < 1000:
        add_card(random.choice(safe_pool))
        fallback_attempts += 1

    def sort_key(cid):
        c = all_cards_map.get(cid)
        if not c: return 999
        if "Monster" in c["type"]: return 1
        if "Spell" in c["type"]: return 2
        if "Trap" in c["type"]: return 3
        return 4
        
    main_deck.sort(key=sort_key)
    extra_deck.sort(key=sort_key)
    return main_deck, extra_deck

def main():
    root = tk.Tk()
    root.withdraw()

    print("-> Selecione o arquivo cards.json...")
    cards_json_path = filedialog.askopenfilename(
        title="Selecione o arquivo cards.json",
        filetypes=[("Arquivos JSON", "*.json"), ("Todos os Arquivos", "*.*")]
    )
    if not cards_json_path:
        print("Seleção cancelada.")
        return

    print("-> Selecione o arquivo characters.json para ATUALIZAR os decks...")
    characters_json_path = filedialog.askopenfilename(
        title="Selecione o arquivo characters.json",
        filetypes=[("Arquivos JSON", "*.json"), ("Todos os Arquivos", "*.*")]
    )
    if not characters_json_path:
        print("Seleção cancelada.")
        return

    print(f"-> Carregando {os.path.basename(cards_json_path)} e {os.path.basename(characters_json_path)}...")
    cards_data = load_json(cards_json_path)
    characters_data = load_json(characters_json_path)
    
    if not cards_data or not characters_data: return
    all_cards_map = {c["id"]: c for c in cards_data}
    
    cards_by_pool = {}
    for c in cards_data:
        try: pool_val = float(c.get("pool", "1.1"))
        except: pool_val = 1.1
        if pool_val not in cards_by_pool: cards_by_pool[pool_val] = []
        cards_by_pool[pool_val].append(c["id"])

    dependency_map = build_dependency_map(cards_data)
    
    print("-> Distribuindo cartas exclusivas...")
    used_unique_ids = set()
    char_unique_map = {}
    char_signature_map = {}

    for char in characters_data:
        char_id = char["id"]
        act = get_act_from_id(char_id)
        char_unique_map[char_id] = []
        
        # 1. Atribui a Carta Assinatura (S+) a partir da primeira FORCED_CARD
        char_id_lower = char_id.lower()
        sig_id = None
        for key, sigs in FORCED_CARDS.items():
            if key in char_id_lower:
                sig_id = sigs[0]
                for s in sigs:
                    if s not in used_unique_ids and s in all_cards_map:
                        char_unique_map[char_id].append(s)
                        used_unique_ids.add(s)
                break
        
        min_p, max_p = ACT_POOL_RANGES.get(act, (1.1, 1.5))
        
        if not sig_id:
            candidates = get_pool_candidates(min_p, max_p + 0.5, cards_by_pool, all_cards_map)
            candidates = [c for c in candidates if c not in used_unique_ids]
            if candidates:
                candidates.sort(key=lambda x: float(all_cards_map[x].get("pool", "1.1")), reverse=True)
                sig_id = candidates[0]
                char_unique_map[char_id].append(sig_id)
                used_unique_ids.add(sig_id)
                
        char_signature_map[char_id] = sig_id
        
        candidates = get_pool_candidates(min_p, max_p + 0.5, cards_by_pool, all_cards_map)
        candidates = [c for c in candidates if c not in used_unique_ids]
        
        needed = 7 - len(char_unique_map[char_id])
        if needed > 0 and candidates:
            picked = random.sample(candidates, min(len(candidates), needed))
            char_unique_map[char_id].extend(picked)
            used_unique_ids.update(picked)

    print(f"-> Gerando decks para {len(characters_data)} personagens...")
    for char in characters_data:
        act = get_act_from_id(char["id"])
        
        forced = []
        for key, sigs in FORCED_CARDS.items():
            if key in char["id"].lower():
                forced.extend(sigs)

        uniques = char_unique_map.get(char["id"], [])

        main_A, extra_A = generate_deck(char["id"], act, "A", cards_by_pool, all_cards_map, dependency_map, forced, uniques)
        main_B, extra_B = generate_deck(char["id"], act, "B", cards_by_pool, all_cards_map, dependency_map, forced, uniques)
        main_C, extra_C = generate_deck(char["id"], act, "C", cards_by_pool, all_cards_map, dependency_map, forced, uniques)

        char["deck_A"] = main_A; char["extra_deck_A"] = extra_A
        char["deck_B"] = main_B; char["extra_deck_B"] = extra_B
        char["deck_C"] = main_C; char["extra_deck_C"] = extra_C
        
        print(f"   [{char['id']}] Ato {act} - (Main A: {len(char['deck_A'])}, Extra A: {len(char['extra_deck_A'])})")

    print(f"-> Salvando {os.path.basename(characters_json_path)} formatado...")
    with open(characters_json_path, 'w', encoding='utf-8') as f:
        f.write("[\n")
        for i, char in enumerate(characters_data):
            f.write("  {\n")
            f.write(f'    "id": "{char["id"]}",\n')
            f.write(f'    "name": "{char["name"]}",\n')
            
            sig_str = char_signature_map.get(char["id"], "")
            uniques_str = json.dumps(char_unique_map.get(char["id"], []))
            
            f.write(f'    "signature_card": "{sig_str}",\n')
            f.write(f'    "unique_drops": {uniques_str},\n')

            f.write(f'    "deck_A": {format_deck_list_custom(char["deck_A"])},\n')
            f.write(f'    "extra_deck_A": {format_deck_list_custom(char["extra_deck_A"])},\n')
            f.write(f'    "deck_B": {format_deck_list_custom(char["deck_B"])},\n')
            f.write(f'    "extra_deck_B": {format_deck_list_custom(char["extra_deck_B"])},\n')
            f.write(f'    "deck_C": {format_deck_list_custom(char["deck_C"])},\n')
            f.write(f'    "extra_deck_C": {format_deck_list_custom(char["extra_deck_C"])},\n')
            
            rewards_str = json.dumps(char.get("rewards", []))
            f.write(f'    "rewards": {rewards_str},\n')
            f.write(f'    "field": "{char.get("field", "Normal")}",\n')
            f.write(f'    "difficulty": "{char.get("difficulty", "Easy")}",\n')
            f.write(f'    "story_role": "{char.get("story_role", "Duelist")}"\n')
            
            if i < len(characters_data) - 1: f.write("  },\n")
            else: f.write("  }\n")
        f.write("]")

    print("=== SUCESSO ===")
    print("Decks, Cores e Dependências gerados com perfeição.")

if __name__ == "__main__":
    main()
