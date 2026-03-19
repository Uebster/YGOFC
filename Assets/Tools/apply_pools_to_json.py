import json
import os
import csv
import tkinter as tk
from tkinter import filedialog

def main():
    root = tk.Tk()
    root.withdraw()

    print("-> Selecione o arquivo CSV de pools...")
    csv_path = filedialog.askopenfilename(
        title="Selecione o arquivo CSV de pools",
        filetypes=[("Arquivos CSV", "*.csv"), ("Todos os Arquivos", "*.*")]
    )
    if not csv_path:
        print("Seleção cancelada.")
        return

    print("-> Selecione o arquivo JSON de cartas para ATUALIZAR...")
    cards_json_path = filedialog.askopenfilename(
        title="Selecione o arquivo JSON de cartas",
        filetypes=[("Arquivos JSON", "*.json"), ("Todos os Arquivos", "*.*")]
    )
    if not cards_json_path:
        print("Seleção cancelada.")
        return

    print("-> Lendo planilha de pools...")
    pool_map = {}
    
    try:
        with open(csv_path, 'r', encoding='utf-8') as f:
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
    print(f"-> Atualizando {os.path.basename(cards_json_path)}...")
    with open(cards_json_path, 'r', encoding='utf-8') as f:
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
    with open(cards_json_path, 'w', encoding='utf-8') as f:
        json.dump(cards, f, indent=2)

    print(f"=== SUCESSO ===")
    print(f"{updated_count} cartas atualizadas com a propriedade 'pool' em {os.path.basename(cards_json_path)}.")

if __name__ == "__main__":
    main()
