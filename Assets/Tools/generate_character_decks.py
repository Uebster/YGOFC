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
    dependency_map = {} # { "Card Name": ["Dependent ID 1", "Dependent ID 2"] }
    name_to_id = {c["name"].lower(): c["id"] for c in cards}
    
    for card in cards:
        desc = card.get("description", "").lower()
        # Procura nomes de outras cartas na descrição
        for other_name, other_id in name_to_id.items():
            if other_name in desc and other_id != card["id"]:
                if other_id not in dependency_map:
                    dependency_map[other_id] = []
                dependency_map[other_id].append(card["id"])
                
                # Se a carta atual é fusão, adiciona dependência reversa (Materiais -> Fusão)
                if "Fusion" in card["type"]:
                    if card["id"] not in dependency_map:
                        dependency_map[card["id"]] = []
                    dependency_map[card["id"]].append(other_id)

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

def generate_deck(act, difficulty_modifier, cards_by_pool, all_cards_map, dependency_map):
    deck = []
    extra_deck = []
    
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

    # Preenche o Main Deck (40 cartas)
    while len(deck) < 40:
        card_id = random.choice(candidates)
        card_data = all_cards_map.get(card_id)
        
        if not card_data: continue
        
        # Verifica limite de cópias (max 3)
        if deck.count(card_id) >= 3: continue
        
        # Separa Fusão/Ritual
        if "Fusion" in card_data["type"]:
            if len(extra_deck) < 15 and extra_deck.count(card_id) < 3:
                extra_deck.append(card_id)
                # Tenta adicionar materiais de fusão se possível
                # (Lógica simplificada: adicionar Polymerization se tiver fusão)
                poly_id = "1444" # ID padrão da Polymerization
                if poly_id not in deck and len(deck) < 40:
                    deck.append(poly_id)
            continue
            
        deck.append(card_id)
        
        # Verifica dependências (Combos)
        # Se adicionou "Blue-Eyes", tenta adicionar "Blue-Eyes Ultimate" ou suporte
        if card_id in dependency_map:
            related_ids = dependency_map[card_id]
            for related_id in related_ids:
                if len(deck) >= 40: break
                
                related_data = all_cards_map.get(related_id)
                if not related_data: continue
                
                # Se for fusão, vai pro extra
                if "Fusion" in related_data["type"]:
                    if len(extra_deck) < 15: extra_deck.append(related_id)
                # Se for main deck e estiver dentro do pool (ou um pouco acima)
                elif float(related_data.get("pool", "1.1")) <= max_pool + 1.0:
                    if deck.count(related_id) < 3:
                        deck.append(related_id)

    # Ordena para ficar bonito (Monstros -> Spells -> Traps)
    def sort_key(cid):
        c = all_cards_map.get(cid)
        if not c: return 999
        if "Monster" in c["type"]: return 1
        if "Spell" in c["type"]: return 2
        if "Trap" in c["type"]: return 3
        return 4
        
    deck.sort(key=sort_key)
    extra_deck.sort(key=sort_key)
    
    # Combina Main e Extra para o JSON (o jogo separa no loading)
    return deck + extra_deck

# --- MAIN ---

def main():
    print("-> Carregando banco de dados...")
    cards = load_json(CARDS_JSON_PATH)
    characters = load_json(CHARACTERS_JSON_PATH)
    
    if not cards or not characters: return

    # Mapeamento rápido
    all_cards_map = {c["id"]: c for c in cards}
    
    # Agrupa cartas por Pool
    cards_by_pool = {}
    for c in cards:
        try:
            pool_val = float(c.get("pool", "1.1"))
        except:
            pool_val = 1.1
            
        if pool_val not in cards_by_pool:
            cards_by_pool[pool_val] = []
        cards_by_pool[pool_val].append(c["id"])

    # Mapa de dependências
    print("-> Analisando sinergias...")
    dependency_map = build_dependency_map(cards)

    print(f"-> Gerando decks para {len(characters)} personagens...")
    
    # Processa cada personagem
    for char in characters:
        act = get_act_from_id(char["id"])
        
        # Gera 3 variações
        char["deck_A"] = generate_deck(act, "A", cards_by_pool, all_cards_map, dependency_map)
        char["deck_B"] = generate_deck(act, "B", cards_by_pool, all_cards_map, dependency_map)
        char["deck_C"] = generate_deck(act, "C", cards_by_pool, all_cards_map, dependency_map)
        
        print(f"   [{char['id']}] Ato {act} - Decks Gerados ({len(char['deck_A'])} cartas)")

    # --- ESCRITA CUSTOMIZADA DO JSON ---
    # O json.dump padrão não permite formatar arrays em linhas de 10.
    # Vamos construir a string JSON manualmente para respeitar a formatação pedida.
    
    print("-> Salvando characters.json formatado...")
    
    with open(CHARACTERS_JSON_PATH, 'w', encoding='utf-8') as f:
        f.write("[\n")
        
        for i, char in enumerate(characters):
            f.write("  {\n")
            f.write(f'    "id": "{char["id"]}",\n')
            f.write(f'    "name": "{char["name"]}",\n')
            
            # Escreve os decks com formatação especial
            f.write(f'    "deck_A": {format_deck_list_custom(char["deck_A"])},\n')
            f.write(f'    "deck_B": {format_deck_list_custom(char["deck_B"])},\n')
            f.write(f'    "deck_C": {format_deck_list_custom(char["deck_C"])},\n')
            
            # Outros campos (mantendo simples para o exemplo)
            rewards_str = json.dumps(char.get("rewards", []))
            f.write(f'    "rewards": {rewards_str},\n')
            f.write(f'    "field": "{char.get("field", "Normal")}",\n')
            f.write(f'    "difficulty": "{char.get("difficulty", "Easy")}",\n')
            f.write(f'    "story_role": "{char.get("story_role", "Duelist")}"\n')
            
            if i < len(characters) - 1:
                f.write("  },\n")
            else:
                f.write("  }\n") # Último item sem vírgula
                
        f.write("]")

    print("=== SUCESSO ===")
    print("Decks gerados e formatados com 10 cartas por linha.")

if __name__ == "__main__":
    main()
