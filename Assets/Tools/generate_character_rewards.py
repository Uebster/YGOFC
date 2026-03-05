import json
import os
import random
import re

# --- CONFIGURAÇÃO DE CAMINHOS ---
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STREAMING_ASSETS_DIR = os.path.join(BASE_DIR, "Assets", "StreamingAssets")
CARDS_JSON_PATH = os.path.join(STREAMING_ASSETS_DIR, "cards.json")
CHARACTERS_JSON_PATH = os.path.join(STREAMING_ASSETS_DIR, "characters.json")

# --- CONFIGURAÇÃO DE POOLS (Para preenchimento/filler) ---
ACT_POOL_RANGES = {
    1: (1.1, 1.5), 2: (1.3, 2.1), 3: (2.1, 2.5), 4: (2.4, 3.2),
    5: (3.1, 3.5), 6: (3.3, 4.1), 7: (3.5, 4.3), 8: (4.1, 4.5),
    9: (4.3, 5.2), 10: (4.5, 5.5)
}

def load_json(path):
    if not os.path.exists(path):
        return []
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

def get_act_from_id(char_id):
    match = re.match(r"(\d+)_", char_id)
    if match:
        num = int(match.group(1))
        return max(1, min(10, (num - 1) // 10 + 1))
    return 1

def get_card_power(card_data):
    """Retorna o valor numérico do Pool da carta para ordenação."""
    try:
        return float(card_data.get("pool", "1.1"))
    except:
        return 1.1

def format_list_custom(data_list, indent_level=4):
    """
    Formata uma lista de IDs para ter 10 itens por linha no JSON.
    indent_level: Quantos espaços de indentação a chave da lista tem.
    """
    if not data_list:
        return "[]"
    
    lines = []
    chunk_size = 10
    base_indent = " " * indent_level
    item_indent = " " * (indent_level + 2)
    
    for i in range(0, len(data_list), chunk_size):
        chunk = data_list[i:i + chunk_size]
        quoted_chunk = [f'"{x}"' for x in chunk]
        line_str = ", ".join(quoted_chunk)
        lines.append(f"{item_indent}{line_str}")
    
    content = ",\n".join(lines)
    return f"[\n{content}\n{base_indent}]"

def main():
    print("-> Carregando dados...")
    cards = load_json(CARDS_JSON_PATH)
    characters = load_json(CHARACTERS_JSON_PATH)
    
    if not cards or not characters:
        print("ERRO: Arquivos JSON não encontrados.")
        return

    cards_map = {c["id"]: c for c in cards}
    
    # Organizar cartas por pool para preenchimento (filler)
    cards_by_pool = {}
    for c in cards:
        p = get_card_power(c)
        if p not in cards_by_pool: cards_by_pool[p] = []
        cards_by_pool[p].append(c["id"])

    print(f"-> Gerando recompensas para {len(characters)} personagens...")

    for char in characters:
        char_id = char["id"]
        act = get_act_from_id(char_id)
        
        # Recupera as cartas únicas geradas pelo script de decks
        unique_drops = char.get("unique_drops", [])
        
        # 1. Coletar todas as cartas usadas nos decks do personagem
        used_cards = set()
        for deck_key in ["deck_A", "deck_B", "deck_C"]:
            if deck_key in char:
                used_cards.update(char[deck_key])
        
        reward_pool = list(used_cards)
        
        # 2. Preencher até 120 cartas se necessário (com cartas do Ato atual/anterior)
        target_count = 120
        if len(reward_pool) < target_count:
            needed = target_count - len(reward_pool)
            # Pega range do ato. Ex: Ato 1 (1.1-1.5). Filler deve ser um pouco mais fraco ou igual
            min_p, max_p = ACT_POOL_RANGES.get(act, (1.1, 1.5))
            filler_max = max(1.1, max_p) 
            
            candidates = []
            for p, ids in cards_by_pool.items():
                if 1.0 <= p <= filler_max:
                    candidates.extend(ids)
            
            if candidates:
                # Adiciona aleatórios que ainda não estão na lista
                new_fillers = [c for c in candidates if c not in used_cards]
                
                # Se não tiver únicos suficientes, permite duplicatas do pool geral
                if len(new_fillers) < needed:
                    reward_pool.extend(new_fillers)
                    remaining = needed - len(new_fillers)
                    reward_pool.extend(random.choices(candidates, k=remaining))
                else:
                    reward_pool.extend(random.sample(new_fillers, needed))

        # 3. Identificar S+ (Carta Única)
        s_plus_card = None
        
        # A carta S+ DEVE vir da lista de unique_drops se disponível
        if unique_drops:
            # Pega a mais forte das únicas
            sorted_uniques = sorted(unique_drops, key=lambda x: get_card_power(cards_map.get(x, {})), reverse=True)
            s_plus_card = sorted_uniques[0]
        
        # Fallback se não tiver unique_drops (não deveria acontecer se rodar o deck generator antes)
        if not s_plus_card and reward_pool:
            # Ordena pool por poder
            sorted_by_power = sorted(reward_pool, key=lambda x: get_card_power(cards_map.get(x, {})), reverse=True)
            
            # Prioriza Monstros de Efeito ou Fusão de alto nível
            for cid in sorted_by_power:
                c_data = cards_map.get(cid, {})
                ctype = c_data.get("type", "")
                if "Monster" in ctype and ("Effect" in ctype or "Fusion" in ctype):
                    s_plus_card = cid
                    break

        # Remove S+ do pool geral para garantir exclusividade (opcional, mas recomendado)
        if s_plus_card and s_plus_card in reward_pool:
            # Remove todas as cópias dela do pool comum
            reward_pool = [x for x in reward_pool if x != s_plus_card] # Remove S+ das listas normais

        # 4. Classificar o restante em 4 Tiers (S, B, C, D)
        # Ordena do mais forte para o mais fraco
        reward_pool.sort(key=lambda x: get_card_power(cards_map.get(x, {})), reverse=True)
        
        total = len(reward_pool)
        # Distribuição: 
        # S (High): Top 15%
        # B (Mid): Next 25%
        # C (Low): Next 30%
        # D (Trash): Bottom 30%
        
        idx_s = int(total * 0.15)
        idx_b = int(total * 0.40)
        idx_c = int(total * 0.70)
        
        pool_s = reward_pool[:idx_s]
        pool_b = reward_pool[idx_s:idx_b]
        pool_c = reward_pool[idx_b:idx_c]
        pool_d = reward_pool[idx_c:]
        
        # Fallback para listas vazias
        if not pool_s: pool_s = reward_pool[:1]
        if not pool_d: pool_d = reward_pool[-1:]

        # Garante que as OUTRAS cartas únicas (além da S+) estejam distribuídas nos pools S e B
        # para que o jogador possa obtê-las sem precisar tirar S+ 7 vezes.
        # (A lógica de ordenação acima já deve ter colocado as mais fortes no S/B, 
        # mas como elas são únicas, elas naturalmente cairão lá se tiverem poder alto).

        # 5. Montar objeto de rewards
        char["rewards"] = {
            "s_plus": s_plus_card if s_plus_card else "0001",
            "s": pool_s,
            "b": pool_b,
            "c": pool_c,
            "d": pool_d
        }
        
        s_name = cards_map.get(s_plus_card, {}).get('name', 'Unknown') if s_plus_card else "None"
        print(f"   [{char_id}] Rewards gerados. S+: {s_name} | Total Pool: {len(reward_pool)+1}")

    # --- SALVAMENTO COM FORMATAÇÃO CUSTOMIZADA ---
    print("-> Salvando characters.json formatado (10 cartas/linha)...")
    with open(CHARACTERS_JSON_PATH, 'w', encoding='utf-8') as f:
        f.write("[\n")
        for i, char in enumerate(characters):
            f.write("  {\n")
            f.write(f'    "id": "{char["id"]}",\n')
            f.write(f'    "name": "{char["name"]}",\n')
            
            # Preserva o campo unique_drops
            f.write(f'    "unique_drops": {json.dumps(char.get("unique_drops", []))},\n')

            # Decks (Indentação 4)
            f.write(f'    "deck_A": {format_list_custom(char.get("deck_A", []), 4)},\n')
            f.write(f'    "deck_B": {format_list_custom(char.get("deck_B", []), 4)},\n')
            f.write(f'    "deck_C": {format_list_custom(char.get("deck_C", []), 4)},\n')
            
            # Rewards (Indentação interna 6)
            rewards = char.get("rewards", {})
            f.write('    "rewards": {\n')
            f.write(f'      "s_plus": "{rewards.get("s_plus", "")}",\n')
            f.write(f'      "s": {format_list_custom(rewards.get("s", []), 6)},\n')
            f.write(f'      "b": {format_list_custom(rewards.get("b", []), 6)},\n')
            f.write(f'      "c": {format_list_custom(rewards.get("c", []), 6)},\n')
            f.write(f'      "d": {format_list_custom(rewards.get("d", []), 6)}\n')
            f.write('    },\n')
            
            # Outros campos
            f.write(f'    "field": "{char.get("field", "Normal")}",\n')
            f.write(f'    "difficulty": "{char.get("difficulty", "Easy")}",\n')
            f.write(f'    "story_role": "{char.get("story_role", "Duelist")}"\n')
            
            if i < len(characters) - 1:
                f.write("  },\n")
            else:
                f.write("  }\n")
        f.write("]")
    
    print("=== SUCESSO ===")

if __name__ == "__main__":
    main()
