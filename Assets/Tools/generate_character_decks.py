import json
import os
import random
import re
import math

# --- CONFIGURAÇÃO DE CAMINHOS ---
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STREAMING_ASSETS_DIR = os.path.join(BASE_DIR, "Assets", "StreamingAssets")
CARDS_JSON_PATH = os.path.join(STREAMING_ASSETS_DIR, "cards.json")
CHARACTERS_JSON_PATH = os.path.join(STREAMING_ASSETS_DIR, "characters.json")

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

# --- CONFIGURAÇÃO DE CARTAS ASSINATURA ---
# Mapeia parte do ID do personagem para uma lista de IDs de cartas obrigatórias
SIGNATURE_CARDS = {
    "kaiba": ["0217", "0217", "0217"], # Blue-Eyes White Dragon x3
    "yugi": ["0419"], # Dark Magician
    "joey": ["1504"], # Red-Eyes B. Dragon
    "pegasus": ["1513", "1950"], # Relinquished, Toon World
    "mai": ["0867", "0867", "0867"], # Harpie Lady x3
    "weevil": ["0825", "0313", "1422"], # Great Moth combo
    "rex": ["1995"], # Two-Headed King Rex
    "mako": ["1876"], # The Legendary Fisherman
    "bandit": ["0139"], # Barrel Dragon
    "marik": ["1060"], # Lava Golem
    "bakura": ["0427"], # Dark Necrofear
    "ishizu": ["0613"], # Exchange of the Spirit
    "odion": ["0590"], # Embodiment of Apophis
}

# --- BANLIST (ID -> Limit) ---
# 0 = Forbidden, 1 = Limited, 2 = Semi-Limited
BANLIST = {
    # Forbidden (0)
    "0289": 0, "2128": 0, "2097": 0, "0932": 0, "0872": 0, "0321": 0, "1863": 0,
    "0287": 0, "1055": 0, "0639": 0, "0363": 0, "1134": 0, "0370": 0, "1252": 0,
    "1480": 0, "1268": 0, "0485": 0,

    # Limited (1)
    "0189": 1, "0293": 1, "0240": 1, "1587": 1, "0975": 1, "1973": 1, "1651": 1,
    "0616": 1, "0378": 1, "0388": 1, "1277": 1, "1457": 1, "0422": 1, "1513": 1,
    "1790": 1, "1517": 1, "1447": 1, "0791": 1, "0414": 1, "0881": 1, "1683": 1,
    "1453": 1, "0259": 1, "1251": 1, "1533": 1, "1955": 1, "1120": 1, "0275": 1,
    "1499": 1, "1811": 1, "1353": 1, "0228": 1, "1318": 1, "0757": 1, "1563": 1,
    "2020": 1, "1138": 1, "1170": 1, "1236": 1, "0237": 1, "1088": 1, "1200": 1,
    "1929": 1, "2050": 1, "1610": 1, "0264": 1, "0497": 1, "1523": 1,
    "0618": 1, "1061": 1, "1062": 1, "1530": 1, "1531": 1,

    # Semi-Limited (2)
    "0338": 2, "2024": 2, "1509": 2, "0359": 2, "0011": 2, "0602": 2, "1209": 2,
    "1077": 2, "0817": 2, "1245": 2, "0786": 2, "1329": 2, "1163": 2, "0077": 2,
    "1138": 2, "1162": 2, "0460": 2, "1604": 2, "1498": 2
}

# Configuração Global para permitir proibidas nos decks gerados (ex: Bosses)
ALLOW_FORBIDDEN = False 

# --- FUNÇÕES AUXILIARES ---

def load_json(path):
    if not os.path.exists(path):
        print(f"ERRO: Arquivo não encontrado: {path}")
        return []
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

def get_act_from_id(char_id):
    """Extrai o Ato baseado no ID (ex: '005_tea' -> 1, '015_mako' -> 2)"""
    match = re.match(r"(\d+)_", char_id)
    if match:
        num = int(match.group(1))
        # IDs 001-010 = Ato 1, 011-020 = Ato 2, etc.
        return max(1, min(10, (num - 1) // 10 + 1))
    return 1

def build_dependency_map(cards):
    """Cria um mapa de cartas que citam outras cartas (ex: Polymerization -> Fusões)"""
    dependency_map = {} # { "Card ID": ["Dependent ID 1", "Dependent ID 2"] }
    
    # Primeiro, crie um mapeamento de nome para ID para busca eficiente
    name_to_id = {c["name"].lower(): c["id"] for c in cards}
    
    for card in cards:
        desc = card.get("description", "").lower()
        
        # Procura nomes de outras cartas na descrição
        for other_name, other_id in name_to_id.items():
            # Evita que a carta dependa de si mesma
            if other_id == card["id"]:
                continue
            
            # Verifica se o nome da outra carta está na descrição desta
            if other_name in desc:
                if card["id"] not in dependency_map:
                    dependency_map[card["id"]] = []
                # Adiciona a dependência se ainda não estiver lá
                if other_id not in dependency_map[card["id"]]:
                    dependency_map[card["id"]].append(other_id)
                    
        # Adiciona dependência para Polymerization se a carta for um monstro de fusão
        if "Fusion" in card["type"]:
            poly_id = name_to_id.get("polymerization")
            if poly_id and card["id"] not in dependency_map:
                dependency_map[card["id"]] = []
            if poly_id and poly_id not in dependency_map[card["id"]]:
                dependency_map[card["id"]].append(poly_id)

    return dependency_map

def format_deck_list_custom(deck_list):
    """Formata a lista de IDs para ter 10 itens por linha no JSON final."""
    if not deck_list:
        return "[]"
    
    lines = []
    chunk_size = 10
    for i in range(0, len(deck_list), chunk_size):
        chunk = deck_list[i:i + chunk_size]
        # Formata cada ID com aspas
        quoted_chunk = [f'"{x}"' for x in chunk]
        # Junta com vírgula
        line_str = ", ".join(quoted_chunk)
        lines.append(f"      {line_str}")
    
    # Junta as linhas com quebra de linha e indentação
    content = ",\n".join(lines)
    return f"[\n{content}\n    ]"

def generate_deck(act, difficulty_modifier, cards_by_pool, all_cards_map, dependency_map, forced_cards=None, unique_pool=None):
    main_deck = []
    extra_deck = []
    
    # 1. Adiciona Cartas Assinatura (Forced)
    if forced_cards:
        for cid in forced_cards:
            if cid in all_cards_map:
                c_data = all_cards_map[cid]
                if "Fusion" in c_data["type"] or "Synchro" in c_data["type"] or "Xyz" in c_data["type"]:
                    if len(extra_deck) < 15 and extra_deck.count(cid) < 3: extra_deck.append(cid)
                else:
                    if len(main_deck) < 40 and main_deck.count(cid) < 3: main_deck.append(cid)
    
    # 2. Adiciona Cartas Únicas/Exclusivas (Garante que elas estejam no deck para serem dropadas)
    if unique_pool:
        for cid in unique_pool:
            if cid in all_cards_map:
                c_data = all_cards_map[cid]
                if "Fusion" in c_data["type"] or "Synchro" in c_data["type"] or "Xyz" in c_data["type"]:
                    if len(extra_deck) < 15 and extra_deck.count(cid) < 3:
                        extra_deck.append(cid)
                elif main_deck.count(cid) < 3:
                    if len(main_deck) < 40: main_deck.append(cid)

    # Define o range de pools
    min_pool, max_pool = ACT_POOL_RANGES.get(act, (1.1, 1.5))
    
    # Ajuste por dificuldade (Deck B e C são mais fortes)
    if difficulty_modifier == "B":
        min_pool += 0.2
        max_pool += 0.2
    elif difficulty_modifier == "C":
        min_pool += 0.4
        max_pool += 0.5
        
    # Coleta candidatos válidos
    candidates = []
    for pool_val, ids in cards_by_pool.items():
        if min_pool <= pool_val <= max_pool:
            candidates.extend(ids)
            
    if not candidates:
        print(f"AVISO: Nenhum candidato para Ato {act} (Pool {min_pool}-{max_pool}). Usando Pool 1.1")
        candidates = cards_by_pool.get(1.1, [])
        if not candidates: # Fallback total
            candidates = [c["id"] for c in all_cards_map.values() if "Monster" in c["type"]]

    # Preenche o Main Deck (40 cartas)
    while len(main_deck) < 40:
        if not candidates: break # Evita loop infinito se não houver mais cartas
        
        card_id = random.choice(candidates)
        card_data = all_cards_map.get(card_id)
        
        if not card_data: continue
        
        # Verifica limite de cópias (Banlist + Regra de 3)
        current_count = main_deck.count(card_id) + extra_deck.count(card_id)
        limit = 3
        
        if card_id in BANLIST:
            limit = BANLIST[card_id]
            if ALLOW_FORBIDDEN and limit == 0:
                limit = 1
        
        if current_count >= limit: continue
        
        # Separa Fusão/Synchro/Xyz para o Extra Deck
        if "Fusion" in card_data["type"] or "Synchro" in card_data["type"] or "Xyz" in card_data["type"]:
            if len(extra_deck) < 15 and extra_deck.count(card_id) < 3:
                extra_deck.append(card_id)
                # Se adicionou uma fusão, tenta adicionar Polymerization ao main deck
                if "Fusion" in card_data["type"]:
                    poly_id = "1444" # ID padrão da Polymerization
                    if poly_id not in main_deck and len(main_deck) < 40:
                        main_deck.append(poly_id)
            continue
            
        main_deck.append(card_id)
        
        # Verifica dependências (Combos)
        if card_id in dependency_map:
            related_ids = dependency_map[card_id]
            for related_id in related_ids:
                if len(main_deck) >= 40: break
                
                related_data = all_cards_map.get(related_id)
                if not related_data: continue
                
                # Se for fusão/synchro/xyz, vai pro extra
                if "Fusion" in related_data["type"] or "Synchro" in related_data["type"] or "Xyz" in related_data["type"]:
                    if len(extra_deck) < 15 and extra_deck.count(related_id) < 3:
                        extra_deck.append(related_id)
                # Se for main deck e estiver dentro do pool (ou um pouco acima)
                elif float(related_data.get("pool", "1.1")) <= max_pool + 1.0:
                    if main_deck.count(related_id) < 3:
                        main_deck.append(related_id)

    # Ordena para ficar bonito (Monstros -> Spells -> Traps)
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

# --- MAIN ---

def main():
    print("-> Carregando banco de dados...")
    cards_data = load_json(CARDS_JSON_PATH)
    characters_data = load_json(CHARACTERS_JSON_PATH)
    
    if not cards_data or not characters_data: return

    # Mapeamento rápido
    all_cards_map = {c["id"]: c for c in cards_data}
    
    # Agrupa cartas por Pool
    cards_by_pool = {}
    for c in cards_data:
        try:
            pool_val = float(c.get("pool", "1.1"))
        except:
            pool_val = 1.1
            
        if pool_val not in cards_by_pool:
            cards_by_pool[pool_val] = []
        cards_by_pool[pool_val].append(c["id"])

    # Mapa de dependências
    print("-> Analisando sinergias...")
    dependency_map = build_dependency_map(cards_data)
    
    # --- ALOCAÇÃO DE CARTAS ÚNICAS (EXCLUSIVE DROPS) ---
    print("-> Distribuindo cartas exclusivas...")
    used_unique_ids = set()
    char_unique_map = {} # char_id -> list of card_ids

    for char in characters_data:
        char_id = char["id"]
        act = get_act_from_id(char_id)
        char_unique_map[char_id] = []
        
        # 1. Tenta atribuir Signatures Hardcoded (se ainda não usadas por outro char)
        char_id_lower = char_id.lower()
        for key, sigs in SIGNATURE_CARDS.items():
            if key in char_id_lower:
                for sig_id in sigs:
                    if sig_id not in used_unique_ids and sig_id in all_cards_map:
                        char_unique_map[char_id].append(sig_id)
                        used_unique_ids.add(sig_id)
        
        # 2. Preenche até 7 cartas com cartas aleatórias do Pool do Ato
        target_count = 7
        min_p, max_p = ACT_POOL_RANGES.get(act, (1.1, 1.5))
        
        # Coleta candidatos do pool atual que ainda não são únicos de ninguém
        candidates = []
        for p, ids in cards_by_pool.items():
            if min_p <= p <= max_p + 0.5: # +0.5 para dar um pouco de "tempero" raro
                for cid in ids:
                    if cid not in used_unique_ids:
                        candidates.append(cid)
        
        needed = target_count - len(char_unique_map[char_id])
        if needed > 0 and candidates:
            picked = random.sample(candidates, min(len(candidates), needed))
            char_unique_map[char_id].extend(picked)
            used_unique_ids.update(picked)

    print(f"-> Gerando decks para {len(characters_data)} personagens...")
    
    # Processa cada personagem
    for char in characters_data:
        act = get_act_from_id(char["id"])
        
        # Identifica cartas assinatura pelo ID do personagem
        forced = []
        char_id_lower = char["id"].lower()
        for key, sigs in SIGNATURE_CARDS.items():
            if key in char_id_lower:
                forced.extend(sigs)

        # Pega as cartas únicas atribuídas a este personagem
        uniques = char_unique_map.get(char["id"], [])

        # Gera 3 variações
        main_A, extra_A = generate_deck(act, "A", cards_by_pool, all_cards_map, dependency_map, forced, uniques)
        main_B, extra_B = generate_deck(act, "B", cards_by_pool, all_cards_map, dependency_map, forced, uniques)
        main_C, extra_C = generate_deck(act, "C", cards_by_pool, all_cards_map, dependency_map, forced, uniques)

        char["deck_A"] = main_A
        char["extra_deck_A"] = extra_A # Novo campo para extra deck
        char["deck_B"] = main_B
        char["extra_deck_B"] = extra_B # Novo campo para extra deck
        char["deck_C"] = main_C
        char["extra_deck_C"] = extra_C # Novo campo para extra deck
        
        print(f"   [{char['id']}] Ato {act} - Decks Gerados (Main: {len(char['deck_A'])}, Extra: {len(char['extra_deck_A'])})")

    # --- ESCRITA CUSTOMIZADA DO JSON ---
    print("-> Salvando characters.json formatado...")
    
    with open(CHARACTERS_JSON_PATH, 'w', encoding='utf-8') as f:
        f.write("[\n")
        
        for i, char in enumerate(characters_data):
            f.write("  {\n")
            f.write(f'    "id": "{char["id"]}",\n')
            f.write(f'    "name": "{char["name"]}",\n')
            
            # Salva a lista de drops únicos para o gerador de rewards usar
            uniques_str = json.dumps(char_unique_map.get(char["id"], []))
            f.write(f'    "unique_drops": {uniques_str},\n')

            # Escreve os decks com formatação especial
            f.write(f'    "deck_A": {format_deck_list_custom(char["deck_A"])},\n')
            f.write(f'    "extra_deck_A": {format_deck_list_custom(char["extra_deck_A"])},\n') # Novo
            f.write(f'    "deck_B": {format_deck_list_custom(char["deck_B"])},\n')
            f.write(f'    "extra_deck_B": {format_deck_list_custom(char["extra_deck_B"])},\n') # Novo
            f.write(f'    "deck_C": {format_deck_list_custom(char["deck_C"])},\n')
            f.write(f'    "extra_deck_C": {format_deck_list_custom(char["extra_deck_C"])},\n') # Novo
            
            # Outros campos (mantendo simples para o exemplo)
            rewards_str = json.dumps(char.get("rewards", []))
            f.write(f'    "rewards": {rewards_str},\n')
            f.write(f'    "field": "{char.get("field", "Normal")}",\n')
            f.write(f'    "difficulty": "{char.get("difficulty", "Easy")}",\n')
            f.write(f'    "story_role": "{char.get("story_role", "Duelist")}"\n')
            
            if i < len(characters_data) - 1:
                f.write("  },\n")
            else:
                f.write("  }\n") # Último item sem vírgula
                
        f.write("]")

    print("=== SUCESSO ===")
    print("Decks gerados e formatados com 10 cartas por linha.")

if __name__ == "__main__":
    main()
