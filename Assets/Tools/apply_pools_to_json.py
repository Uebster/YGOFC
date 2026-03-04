import json
import os
import csv

# Configuração de Caminhos
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STREAMING_ASSETS_DIR = os.path.join(BASE_DIR, "Assets", "StreamingAssets")
CARDS_JSON_PATH = os.path.join(STREAMING_ASSETS_DIR, "cards.json")
CSV_PATH = os.path.join(BASE_DIR, "Assets", "Tools", "card_pool_template.csv")

def main():
    if not os.path.exists(CSV_PATH):
        print(f"ERRO: Arquivo CSV não encontrado em {CSV_PATH}")
        print("Rode o script 'generate_pool_template.py' primeiro.")
        return

    print("-> Lendo planilha de pools...")
    pool_map = {}
    
    try:
        with open(CSV_PATH, 'r', encoding='utf-8') as f:
            reader = csv.DictReader(f)
            for row in reader:
                card_id = row["ID"]
                
                # Lógica de prioridade: Final_Pool > Suggested_Pool
                pool = row["Final_Pool"].strip()
                if not pool:
                    pool = row["Suggested_Pool"].strip()
                
                # Validação básica
                try:
                    parts = pool.split('.')
                    if len(parts) == 2:
                        pool_map[card_id] = pool
                    else:
                        print(f"AVISO: Formato de pool inválido para {row['Name']}: {pool}. Usando 1.1")
                        pool_map[card_id] = "1.1"
                except:
                    pool_map[card_id] = "1.1"
                    
    except Exception as e:
        print(f"ERRO ao ler CSV: {e}")
        return

    print(f"-> Carregados {len(pool_map)} definições de pool.")

    # Atualizar o JSON
    print("-> Atualizando cards.json...")
    with open(CARDS_JSON_PATH, 'r', encoding='utf-8') as f:
        cards = json.load(f)

    updated_count = 0
    for card in cards:
        if card["id"] in pool_map:
            card["pool"] = pool_map[card["id"]]
            updated_count += 1
        else:
            # Fallback se a carta não estava na planilha
            card["pool"] = "1.1"
            print(f"AVISO: Carta {card['name']} ({card['id']}) não encontrada no CSV. Definida como 1.1")

    # Salvar de volta
    with open(CARDS_JSON_PATH, 'w', encoding='utf-8') as f:
        json.dump(cards, f, indent=2)

    print(f"=== SUCESSO ===")
    print(f"{updated_count} cartas atualizadas com a propriedade 'pool'.")

if __name__ == "__main__":
    main()
