import json
import os

# Configuração de Caminhos
# Base: Project Root
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Entrada: Scripts/Fields/Arenas.txt
INPUT_FILE = os.path.join(BASE_DIR, "Scripts", "Fields", "Arenas.txt")

# Saída: Fields/fields.json
OUTPUT_DIR = os.path.join(BASE_DIR, "Fields")
OUTPUT_FILE = os.path.join(OUTPUT_DIR, "fields.json")

def generate_fields():
    print(f"Lendo arenas de: {INPUT_FILE}")
    
    if not os.path.exists(INPUT_FILE):
        print("ERRO: Arquivo Arenas.txt não encontrado.")
        return

    fields_list = []

    with open(INPUT_FILE, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    for line in lines:
        line = line.strip()
        if not line: continue

        # Formato esperado: "01 - Nome do Oponente - Nome da Arena"
        parts = line.split(' - ')
        
        if len(parts) >= 3:
            try:
                raw_id = parts[0].strip()
                # opponent_name = parts[1].strip() # Não usado no JSON final da arena, mas útil se quiser descrição
                arena_name = parts[2].strip()

                fields_list.append({
                    "id": f"{int(raw_id):03d}", # Formata para 001, 002, etc.
                    "name": arena_name
                })
            except ValueError:
                print(f"Aviso: Linha com formato inválido ignorada: {line}")

    os.makedirs(OUTPUT_DIR, exist_ok=True)
    with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
        json.dump(fields_list, f, indent=2, ensure_ascii=False)

    print(f"Sucesso! {len(fields_list)} arenas geradas em: {OUTPUT_FILE}")

if __name__ == "__main__":
    generate_fields()