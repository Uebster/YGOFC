import json
import os
import re

# Configuração de Caminhos
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CARDS_DIR = os.path.join(BASE_DIR, "Scripts", "Cards")
CARDS_OUT = os.path.join(CARDS_DIR, "cards.json")

# Configuração de Processamento
BATCH_SIZE = 500  # Processar em blocos de 500 linhas/cartas
IMAGES_SUBDIR = "YuGiOh_OCG_Classic_2147"

# Correções de Nomes (Typos e Padronização)
NAME_FIXES = {
    "Enchanted Javeling": "Enchanted Javelin",
    "Lightning Conger-": "Lightning Conger",
    "CYBER-TECH ALLIGATOR": "Cyber-Tech Alligator",
    "Dark Spirit of the Slient": "Dark Spirit of the Silent",
    "Blue-eyes White Dragon": "Blue-Eyes White Dragon",
    "Blue-eyes Ultimate Dragon": "Blue-Eyes Ultimate Dragon",
    "Red-eyes B. Dragon": "Red-Eyes B. Dragon",
    "Hitotsu-me Giant": "Hitotsu-Me Giant",
    "Ryu-kishin": "Ryu-Kishin",
    "Ryu-kishin Powered": "Ryu-Kishin Powered",
    "Tri-horned Dragon": "Tri-Horned Dragon",
    "Serpent Night Dragon": "Serpent Night Dragon",
    "Dark-piercing Light": "Dark-Piercing Light",
    "Yamata Dragon Scroll": "Dragon Scroll"
}

def clean_name(name):
    name = name.strip()
    return NAME_FIXES.get(name, name)

def sanitize_filename(name):
    """Remove caracteres inválidos para nomes de arquivo (ex: : ? " < > | *)."""
    return re.sub(r'[<>:"/\\|?*]', '', name)

def parse_poc_cards(filepath, game_name):
    """Parser para arquivos no formato Power of Chaos (Yugi/Kaiba/Joey)"""
    print(f"  -> Processando formato Power of Chaos: {game_name}")
    cards = {}
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Regex ajustado para capturar qualquer total de cartas (ex: [123/155] ou [123/350])
    pattern = re.compile(r"--(.+?)--\s\[(\d+)/\d+\]\n(.+?)(?=\n--|\Z)", re.DOTALL)
    matches = pattern.findall(content)

    for name_raw, num, body in matches:
        name = clean_name(name_raw)
        
        card = {
            "id": int(num),
            "name": name,
            "game_of_origin": game_name,
            "description": ""
        }

        lines = body.strip().split('\n')
        for line in lines:
            if ':' in line:
                key, val = line.split(':', 1)
                key = key.strip().lower()
                val = val.strip()
                
                if key == "level": card["level"] = int(val)
                elif key == "attribute": card["attribute"] = val.upper()
                elif key == "type":
                    if val in ["Spell", "Trap", "Equip", "Continuous", "Quick-Play", "Counter", "Field", "Ritual", "Normal"]:
                        if val in ["Spell", "Trap"]:
                            card["type"] = val
                        else:
                            if "type" not in card: card["type"] = "Spell"
                            card["property"] = val
                    else:
                        card["type"] = "Monster"
                        card["race"] = val
                elif key == "attack": card["atk"] = int(val)
                elif key == "defense": card["def"] = int(val)
                elif key == "description": card["description"] = val
            else:
                if "description" in card: card["description"] += " " + line.strip()

        # Fallback para tipo se não detectado
        if "atk" not in card and "type" not in card:
             prop = card.get("property", "")
             if prop in ["Counter", "Continuous", "Normal"] and "Trap" in card.get("description", ""):
                 card["type"] = "Trap"
             else:
                 card["type"] = "Spell"

        cards[name.lower()] = card
    
    return cards

def parse_fm_cards(filepath, game_name):
    """Parser específico para o formato do Forbidden Memories (Tab separated com Starchips/Passwords)"""
    print(f"  -> Processando formato Forbidden Memories: {game_name}")
    cards = {}
    
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    for line in lines:
        parts = line.strip().split('\t')
        if len(parts) < 4: continue
        
        try:
            fm_id = int(parts[0])
            name = clean_name(parts[1])
            raw_type = parts[2]
            
            card = {
                "id": fm_id,
                "name": name,
                "game_of_origin": game_name,
                "description": "No description available."
            }
            
            if raw_type == "Monster":
                card["type"] = "Monster"
                card["race"] = parts[3]
                card["level"] = int(parts[4])
                card["atk"] = int(parts[5])
                card["def"] = int(parts[6])
                if len(parts) > 7: card["password"] = parts[7]
                if len(parts) > 8: card["starchips"] = parts[8]
            else:
                # Mapeamento de tipos do FM
                if raw_type == "Magic": card["type"] = "Spell"; card["property"] = "Normal"
                elif raw_type == "Equip": card["type"] = "Spell"; card["property"] = "Equip"
                elif raw_type == "Ritual": card["type"] = "Spell"; card["property"] = "Ritual"
                elif raw_type == "Trap": card["type"] = "Trap"; card["property"] = "Normal"
                elif raw_type == "Field": card["type"] = "Spell"; card["property"] = "Field"
                
                if len(parts) > 3: card["password"] = parts[3]
                if len(parts) > 4: card["starchips"] = parts[4]

            cards[name.lower()] = card
        except ValueError:
            continue
            
    return cards

def parse_tsv_cards(filepath, game_name):
    """Parser Genérico para arquivos separados por TAB (comum em listas de GBA/Wiki)"""
    # Espera formato: ID [TAB] Name [TAB] Type [TAB] ...
    print(f"  -> Processando formato Genérico (TSV): {game_name}")
    cards = {}
    
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    for line in lines:
        parts = line.strip().split('\t')
        if len(parts) < 2: continue

        try:
            # Tenta usar a lógica do FM se tiver colunas suficientes (ID, Nome, Tipo...)
            if len(parts) >= 3:
                fm_id = parts[0]
                name = clean_name(parts[1])
                raw_type = parts[2]
                
                card = {
                    "id": int(fm_id),
                    "name": name,
                    "game_of_origin": game_name,
                    "description": ""
                }
                
                if raw_type.startswith("Monster") and len(parts) >= 7:
                    card["type"] = raw_type
                    card["race"] = parts[3]
                    card["level"] = int(parts[4])
                    card["atk"] = int(parts[5])
                    card["def"] = int(parts[6])
                    if len(parts) > 7: card["password"] = parts[7]
                    if len(parts) > 8: card["description"] = parts[8] # Captura descrição
                elif raw_type in ["Magic", "Spell", "Trap", "Equip", "Ritual", "Field"]:
                    if raw_type in ["Magic", "Spell"]: card["type"] = "Spell"; card["property"] = "Normal"
                    elif raw_type == "Trap": card["type"] = "Trap"; card["property"] = "Normal"
                    else: card["type"] = "Spell"; card["property"] = raw_type
                    if len(parts) > 3: card["password"] = parts[3]
                
                    # Suporte para formato estendido (com descrição e ID na coluna 7/8)
                    if len(parts) >= 9:
                        card["description"] = parts[8]
                        # Se a coluna 7 for numérica, é o ID/Password, não a coluna 3 (que seria Raça/Propriedade)
                        if len(parts) > 7 and parts[7].isdigit():
                            card["password"] = parts[7]
                    elif len(parts) >= 5:
                        card["description"] = parts[4]

                cards[name.lower()] = card
            else:
                # Fallback se tiver apenas ID e Nome
                cards[clean_name(parts[1]).lower()] = {
                    "id": int(parts[0]), 
                    "name": clean_name(parts[1]), 
                    "game_of_origin": game_name
                }
        except:
            continue
    
    return cards

def generate():
    print("=== Iniciando Geração do Banco de Dados de Cartas ===")
    print(f"Diretório de Cartas: {CARDS_DIR}")
    
    master_db = {} # Dicionário para consolidar cartas (Chave = Nome em minúsculo)
    total_processed = 0
    
    # Verificação de segurança: Cria a pasta se não existir
    if not os.path.exists(CARDS_DIR):
        print(f"[ERRO] A pasta não foi encontrada em: {CARDS_DIR}")
        print("-> Criando a pasta agora... Por favor, mova o arquivo .txt e as imagens para dentro dela e execute novamente.")
        os.makedirs(CARDS_DIR, exist_ok=True)
        return

    # Escanear todos os arquivos .txt na pasta Cards
    files = [f for f in os.listdir(CARDS_DIR) if f.endswith(".txt") and "(Copy)" not in f]
    files.sort() # Ordenar para consistência
    
    if not files:
        print("[AVISO] Nenhum arquivo .txt encontrado na pasta Cards.")
        return

    for filename in files:
        filepath = os.path.join(CARDS_DIR, filename)
        game_name = os.path.splitext(filename)[0] # Usa o nome do arquivo como "Jogo de Origem"
        print(f"\nLendo arquivo: {filename}")
        
        # Detecção simples de formato lendo o início do arquivo
        format_type = "tsv" # Default
        with open(filepath, 'r', encoding='utf-8') as f:
            header = f.read(1000) # Ler mais caracteres para garantir a detecção
            if "Nº\tNOME\tTIPO" in header: # Header do download_cards.py
                format_type = "tsv"
            elif "--" in header and "[" in header and "]" in header:
                format_type = "poc"
            elif "Monster (" in header: # Detecta formato novo com subtipos (Normal/Effect)
                format_type = "tsv"
            elif "\t" in header and ("Monster" in header or "Magic" in header):
                format_type = "fm"
        
        # Selecionar parser baseado na detecção
        if format_type == "poc":
            extracted = parse_poc_cards(filepath, game_name)
        elif format_type == "fm":
            extracted = parse_fm_cards(filepath, game_name)
        else:
            extracted = parse_tsv_cards(filepath, game_name)
            
        # Mesclar com o banco mestre
        count_new = 0
        count_update = 0
        local_batch_count = 0
        
        for name_key, card_data in extracted.items():
            # Lógica de Blocos: Monitoramento
            local_batch_count += 1
            total_processed += 1
            if local_batch_count % BATCH_SIZE == 0:
                print(f"  ... processando bloco: linha {local_batch_count} ...")

            if name_key in master_db:
                # Atualizar dados existentes (ex: adicionar Password do FM numa carta do Joey)
                existing = master_db[name_key]
                
                # Preservar ID original se for de um jogo prioritário, ou atualizar?
                # Vamos manter o ID do jogo mais "recente" ou específico se definirmos assim.
                # Aqui, apenas enriquecemos dados faltantes.
                if "password" in card_data and "password" not in existing:
                    existing["password"] = card_data["password"]
                if "starchips" in card_data and "starchips" not in existing:
                    existing["starchips"] = card_data["starchips"]
                
                # Se a carta atual tem descrição e a antiga não, atualiza
                if card_data.get("description") and not existing.get("description"):
                    existing["description"] = card_data["description"]
                    existing["game_of_origin"] = card_data["game_of_origin"] # Atualiza origem para a mais rica
                
                count_update += 1
            else:
                master_db[name_key] = card_data
                count_new += 1
                
        print(f"  [Concluído {filename}] Novos: {count_new} | Atualizados: {count_update} | Total lido: {local_batch_count}")

    # Converter para lista e ordenar
    final_list = list(master_db.values())
    final_list.sort(key=lambda x: x["name"])
    
    # Adicionar mapeamento de imagem (Ordem Alfabética -> 1.jpg, 2.jpg...)
    print(f"  -> Mapeando imagens (Ordem Alfabética) na pasta: {IMAGES_SUBDIR}...")
    for i, card in enumerate(final_list):
        # IDs de imagem sequenciais baseados na ordem alfabética
        formatted_id = f"{i + 1:04d}"
        card["id"] = formatted_id
        card["image_id"] = i + 1
        safe_name = sanitize_filename(card["name"])
        card["image_filename"] = f"{IMAGES_SUBDIR}/{formatted_id} - {safe_name}.jpg"

    # Salvar
    os.makedirs(os.path.dirname(CARDS_OUT), exist_ok=True)
    with open(CARDS_OUT, 'w', encoding='utf-8') as f:
        json.dump(final_list, f, indent=2)
        
    print(f"\n=== Sucesso! ===")
    print(f"Total de cartas processadas (bruto): {total_processed}")
    print(f"Total de cartas únicas no JSON: {len(final_list)}")
    print(f"Arquivo salvo em: {CARDS_OUT}")

if __name__ == "__main__":
    generate()
