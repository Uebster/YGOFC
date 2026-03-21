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
FORCED_CARDS_NAMES = {
    "kaiba": ["Blue-Eyes White Dragon", "Blue-Eyes White Dragon", "Blue-Eyes White Dragon", "Master of Oz"],
    "yugi": ["Dark Magician"],
    "joey": ["Red-Eyes B. Dragon"],
    "pegasus": ["Relinquished", "Toon World"],
    "mai": ["Harpie Lady", "Harpie Lady", "Harpie Lady", "Harpie Lady Sisters"],
    "weevil": ["Great Moth", "Cocoon of Evolution", "Petit Moth"],
    "rex": ["Two-Headed King Rex"],
    "mako": ["The Legendary Fisherman"],
    "bandit": ["Barrel Dragon"],
    "keith": ["Barrel Dragon"],
    "marik": ["Lava Golem"],
    "strings": ["Revival Jam", "Jam Breeding Machine"],
    "bakura": ["Dark Necrofear"],
    "ishizu": ["Exchange of the Spirit"],
    "odion": ["Embodiment of Apophis"],
    "noah": ["Shinato, King of a Higher Plane"],
    "gozaburo": ["Exodia Necross"],
    "rare_hunter": ["Exodia the Forbidden One"],
    "arkana": ["Dark Magician Girl"],
    "shadi": ["Millennium Shield"],
    "heishin": ["Zera the Mant"],
    "isis": ["Senju of the Thousand Hands"]
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
CORES_NAMES = {
    "Exodia": ["Exodia the Forbidden One", "Left Arm of the Forbidden One", "Left Leg of the Forbidden One", "Right Arm of the Forbidden One", "Right Leg of the Forbidden One", "Contract with Exodia", "Exodia Necross"],
    "Umi": ["Umi", "Umi", "Umi", "A Legendary Ocean", "A Legendary Ocean", "Tornado Wall", "Tornado Wall", "Amphibian Bugroth MK-3", "Amphibian Bugroth MK-3", "The Legendary Fisherman", "Ocean Dragon Lord - Neo-Daedalus", "Levia-Dragon - Daedalus"], 
    "Burn": ["Solar Flare Dragon", "Solar Flare Dragon", "Just Desserts", "Just Desserts", "Bowganian", "Ojama Trio", "Des Koala", "Tremendous Fire", "Meteor of Destruction"], 
    "Insect": ["Insect Barrier", "Insect Barrier", "Insect Princess", "Pinch Hopper", "Pinch Hopper", "Insect Imitation", "Multiplication of Ants", "Flying Kamakiri #1", "Flying Kamakiri #1"], 
    "Dinosaur": ["Uraby", "Uraby", "Tyrant Dragon", "Two-Headed King Rex", "Two-Headed King Rex", "Enraged Battle Ox", "Mad Sword Beast", "Dark Driceratops"], 
    "Zombie": ["Blood Sucker", "Blood Sucker", "Vampire Lord", "Pyramid Turtle", "Pyramid Turtle", "Call of the Mummy", "Robbin' Zombie", "Mystic Tomato", "Despair from the Dark"], 
    "Warrior": ["Marauding Captain", "Marauding Captain", "Goblin Attack Force", "Goblin Attack Force", "Exiled Force", "Command Knight", "D.D. Warrior Lady", "Amazoness Swords Woman", "Reinforcement of the Army", "Reinforcement of the Army"], 
    "DestinyBoard": ["Destiny Board", "Destiny Board", "Spirit Message \"I\"", "Spirit Message \"N\"", "Spirit Message \"A\"", "Spirit Message \"L\"", "The Dark Door", "The Dark Door"], 
    "Toon": ["Toon World", "Toon World", "Toon World", "Toon Mermaid", "Toon Summoned Skull", "Toon Dark Magician Girl", "Toon Goblin Attack Force", "Toon Goblin Attack Force", "Toon Masked Sorcerer", "Blue-Eyes Toon Dragon"], 
    "Spellcaster": ["Magical Scientist", "Magical Scientist", "Magician of Faith", "Breaker the Magical Warrior", "Skilled Dark Magician", "Skilled Dark Magician", "Dark Magician Girl", "Pitch-Black Power Stone", "Apprentice Magician"], 
    "Machine": ["Limiter Removal", "Limiter Removal", "Heavy Mech Support Platform", "Jinzo", "Machine Duplication", "Cyber Dragon", "Reflect Bounder"], 
    "Harpie": ["Harpie Lady", "Harpie Lady", "Harpie Lady", "Elegant Egotist", "Elegant Egotist", "Harpies' Hunting Ground", "Harpies' Hunting Ground", "Harpie Lady Sisters", "Harpie's Brother", "Cyber Shield", "Cyber Shield"], 
    "Gravekeeper": ["Necrovalley", "Necrovalley", "Necrovalley", "Gravekeeper's Spy", "Gravekeeper's Spy", "Gravekeeper's Chief", "Gravekeeper's Spear Soldier", "Gravekeeper's Assailant", "Gravekeeper's Vassal"], 
    "Fiend": ["Dark Necrofear", "Giant Orc", "Skill Drain", "Archfiend Soldier", "Nightmare Wheel", "Dark Ruler Ha Des", "Wall of Illusion", "Wall of Illusion"], 
    "Fairy": ["The Sanctuary in the Sky", "The Sanctuary in the Sky", "The Sanctuary in the Sky", "Archlord Zerato", "The Agent of Judgment - Saturn", "Mudora", "Shining Angel", "Shining Angel", "Zolga"]
}

STAPLES_TIER_1_NAMES = ["Pot of Greed", "Graceful Charity", "Delinquent Duo", "Mystical Space Typhoon", "Snatch Steal", "Premature Burial"] 
STAPLES_TIER_2_NAMES = ["Heavy Storm", "Nobleman of Crossout", "Book of Moon", "Call of the Haunted", "Mirror Force", "Torrential Tribute"] 
STAPLES_TIER_3_NAMES = ["Dust Tornado", "Sakuretsu Armor", "Bottomless Trap Hole", "Smashing Ground", "Waboku", "Magic Cylinder"] 

EXACT_DEPENDENCIES_NAMES = {
    "Exodia the Forbidden One": ["Left Arm of the Forbidden One", "Left Leg of the Forbidden One", "Right Arm of the Forbidden One", "Right Leg of the Forbidden One"], 
    "Exodia Necross": ["Exodia the Forbidden One", "Left Arm of the Forbidden One", "Left Leg of the Forbidden One", "Right Arm of the Forbidden One", "Right Leg of the Forbidden One", "Contract with Exodia"], 
    "Relinquished": ["Black Illusion Ritual"], 
    "Black Luster Soldier": ["Black Luster Ritual"], 
    "Magician of Black Chaos": ["Black Magic Ritual"], 
    "Blue-Eyes Toon Dragon": ["Toon World"], 
    "Toon Dark Magician Girl": ["Toon World"], 
    "Harpie Lady Sisters": ["Elegant Egotist"], 
    "Perfectly Ultimate Great Moth": ["Petit Moth", "Cocoon of Evolution"], 
    "Great Moth": ["Petit Moth", "Cocoon of Evolution"], 
    "The Masked Beast": ["Curse of the Masked Beast"], 
    "Blue-Eyes Shining Dragon": ["Blue-Eyes Ultimate Dragon", "Blue-Eyes White Dragon", "Polymerization"], 
    "Dark Sage": ["Dark Magician", "Time Wizard"]
}

ALLOW_FORBIDDEN = True

# Variáveis globais geradas dinamicamente
FORCED_CARDS = {}
CORES = {}
STAPLES_TIER_1 = []
STAPLES_TIER_2 = []
STAPLES_TIER_3 = []
EXACT_DEPENDENCIES = {}
NAME_TO_ID = {}

# --- FUNÇÕES AUXILIARES ---

def generate_rewards(unique_drops, all_decks_cards, all_cards_map):
    pool_s_plus = unique_drops[0] if len(unique_drops) > 0 else ""
    pool_s = unique_drops[1:7] if len(unique_drops) > 1 else []
    
    used_cards = set(all_decks_cards)
    if pool_s_plus in used_cards: used_cards.remove(pool_s_plus)
    for s in pool_s:
        if s in used_cards: used_cards.remove(s)
        
    def sort_key(cid):
        c = all_cards_map.get(cid, {})
        try: p = float(c.get("pool", "1.1"))
        except: p = 1.1
        atk = c.get("atk", 0) if "Monster" in c.get("type", "") else 0
        return (p, atk)
        
    sorted_cards = sorted(list(used_cards), key=sort_key, reverse=True)
    b_count = int(len(sorted_cards) * 0.25)
    c_count = int(len(sorted_cards) * 0.30)
    
    return {
        "s_plus": pool_s_plus, "s": pool_s,
        "b": sorted_cards[:b_count],
        "c": sorted_cards[b_count:b_count+c_count],
        "d": sorted_cards[b_count+c_count:]
    }

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
    for pool_val, ids in cards_by_pool.items():
        if min_p <= pool_val <= max_p:
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

def format_list_custom(data_list, indent_level=4):
    if not data_list: return "[]"
    lines = []
    chunk_size = 10
    base_indent = " " * indent_level
    item_indent = " " * (indent_level + 2)
    for i in range(0, len(data_list), chunk_size):
        chunk = data_list[i:i + chunk_size]
        quoted_chunk = [f'"{x}"' for x in chunk]
        lines.append(f"{item_indent}{', '.join(quoted_chunk)}")
    content = ",\n".join(lines)
    return f"[\n{content}\n{base_indent}]"

# --- O DECK BUILDER ---

def generate_deck(char_id, act, difficulty_modifier, cards_by_pool, all_cards_map, dependency_map, forced_cards=None, unique_pool=None):
    main_deck = []
    extra_deck = []
    
    theme = None
    for k, v in THEMES.items():
        if k in char_id.lower():
            theme = v
            break

    # Conta apenas cartas válidas (Não-Banidas) para garantir que o deck sempre tenha 40 jogáveis no mínimo!
    def get_valid_main_count():
        count = 0
        for cid in main_deck:
            c = all_cards_map.get(cid, {})
            if c.get("goat_banlist", "Unlimited") != "Banned":
                count += 1
        return count

    # Helper inteligente para blindar o cap de cartas e injetar dependências
    def add_card(cid, is_forced=False):
        if not cid or cid not in all_cards_map: return False
        c_data = all_cards_map[cid]
        
        limit = 3
        goat_status = c_data.get("goat_banlist", "Unlimited")
        is_forbidden = (goat_status == "Banned")
        if is_forbidden: limit = 0
        elif goat_status == "Limited" or goat_status == "1": limit = 1
        elif goat_status == "Semi-Limited": limit = 2
        
        if ALLOW_FORBIDDEN and is_forbidden: limit = 1
        
        if not is_forced and (main_deck.count(cid) + extra_deck.count(cid)) >= limit: 
            return False
            
        if "Fusion" in c_data["type"] or "Synchro" in c_data["type"] or "Xyz" in c_data["type"]:
            if len(extra_deck) < 15: 
                extra_deck.append(cid)
                poly_id = NAME_TO_ID.get("polymerization")
                if poly_id and poly_id not in main_deck and get_valid_main_count() < 40:
                    main_deck.append(poly_id)
                return True
            return False
        else:
            if get_valid_main_count() < 40 or is_forced:
                main_deck.append(cid)
                if cid in EXACT_DEPENDENCIES:
                    for dep_id in EXACT_DEPENDENCIES[cid]:
                        if main_deck.count(dep_id) == 0 and (get_valid_main_count() < 40 or is_forced):
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
        ml, mh, sp, tr, ex = [], [], [], [], []
        for pool_val, ids in cards_by_pool.items():
            if mn <= pool_val <= mx:
                for cid in ids:
                    if cid not in all_cards_map: continue
                    c = all_cards_map[cid]
                    t = c.get("type", "")
                    if "Fusion" in t or "Synchro" in t or "Xyz" in t:
                        ex.append(cid)
                    elif "Monster" in t:
                        if c.get("level", 1) <= 4: ml.append(cid)
                        else: mh.append(cid)
                    elif "Spell" in t:
                        if c.get("property") != "Field": sp.append(cid)
                    elif "Trap" in t: tr.append(cid)
        return ml, mh, sp, tr, ex

    monsters_low, monsters_high, spells, traps, extras = fetch_cards(min_p, max_p)
    search_min = min_p
    while len(monsters_low) < 15 and search_min > 1.0:
        search_min -= 0.5
        ml, _, _, _, _ = fetch_cards(search_min, max_p)
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
    need_extra = max(0, 4 - len(extra_deck))
    
    fillers = []
    fillers.extend(weighted_choice(monsters_low, need_low))
    fillers.extend(weighted_choice(monsters_high, need_high))
    fillers.extend(weighted_choice(spells, need_spell))
    fillers.extend(weighted_choice(traps, need_trap))
    fillers.extend(weighted_choice(extras, need_extra))
    
    if difficulty_modifier in ["B", "C"]: fillers.extend(STAPLES_TIER_3 + STAPLES_TIER_2)
    else: fillers.extend(STAPLES_TIER_1)
        
    for cid in fillers:
        if get_valid_main_count() >= 40: break
        add_card(cid)

    fallback_attempts = 0
    safe_pool = monsters_low if len(monsters_low) > 0 else STAPLES_TIER_1
    while get_valid_main_count() < 40 and fallback_attempts < 1000:
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

    # Tradução Dinâmica de Nomes para IDs (À Prova de Mudanças!)
    global NAME_TO_ID, FORCED_CARDS, CORES, STAPLES_TIER_1, STAPLES_TIER_2, STAPLES_TIER_3, EXACT_DEPENDENCIES
    NAME_TO_ID = {c["name"].lower(): c["id"] for c in cards_data}
    
    def names_to_ids(names_list):
        return [NAME_TO_ID[n.lower()] for n in names_list if n.lower() in NAME_TO_ID]

    FORCED_CARDS = {k: names_to_ids(v) for k, v in FORCED_CARDS_NAMES.items()}
    CORES = {k: names_to_ids(v) for k, v in CORES_NAMES.items()}
    STAPLES_TIER_1 = names_to_ids(STAPLES_TIER_1_NAMES)
    STAPLES_TIER_2 = names_to_ids(STAPLES_TIER_2_NAMES)
    STAPLES_TIER_3 = names_to_ids(STAPLES_TIER_3_NAMES)
    EXACT_DEPENDENCIES = {}
    for k, v in EXACT_DEPENDENCIES_NAMES.items():
        if k.lower() in NAME_TO_ID:
            EXACT_DEPENDENCIES[NAME_TO_ID[k.lower()]] = names_to_ids(v)

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
            if key in char_id_lower and sigs:
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
        
        all_decks_cards = main_A + extra_A + main_B + extra_B + main_C + extra_C
        char["rewards"] = generate_rewards(uniques, all_decks_cards, all_cards_map)
        
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
            
            rewards = char.get("rewards", {})
            f.write('    "rewards": {\n')
            f.write(f'      "s_plus": "{rewards.get("s_plus", "")}",\n')
            f.write(f'      "s": {format_list_custom(rewards.get("s", []), 6)},\n')
            f.write(f'      "b": {format_list_custom(rewards.get("b", []), 6)},\n')
            f.write(f'      "c": {format_list_custom(rewards.get("c", []), 6)},\n')
            f.write(f'      "d": {format_list_custom(rewards.get("d", []), 6)}\n')
            f.write('    },\n')
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
