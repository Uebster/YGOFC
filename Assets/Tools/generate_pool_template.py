import json
import os
import csv

# Configuração de Caminhos
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STREAMING_ASSETS_DIR = os.path.join(BASE_DIR, "Assets", "StreamingAssets")
CARDS_JSON_PATH = os.path.join(STREAMING_ASSETS_DIR, "cards.json")
OUTPUT_CSV_PATH = os.path.join(BASE_DIR, "Assets", "Tools", "card_pool_template.csv")

def estimate_pool(card):
    """
    Tenta adivinhar o Pool (1.1 a 5.5) baseado nos status da carta.
    Isso serve como base para o usuário não ter que preencher tudo do zero.
    """
    pool_major = 1
    pool_minor = 1
    
    desc = card.get("description", "").lower()
    name = card.get("name", "").lower()

    if card["type"].startswith("Monster"):
        level = card.get("level", 1)
        atk = card.get("atk", 0)
        defense = card.get("def", 0)
        
        # Monstros Normais/Efeito baseados em ATK
        if atk < 500: pool_major = 1; pool_minor = 1
        elif atk < 800: pool_major = 1; pool_minor = 3
        elif atk < 1200: pool_major = 1; pool_minor = 5
        elif atk < 1400: pool_major = 2; pool_minor = 1
        elif atk < 1600: pool_major = 2; pool_minor = 3
        elif atk < 1800: pool_major = 2; pool_minor = 5
        elif atk < 1900: pool_major = 3; pool_minor = 1
        elif atk < 2100: pool_major = 3; pool_minor = 3
        elif atk < 2400: pool_major = 3; pool_minor = 5 # 1 Tributo forte
        elif atk < 2600: pool_major = 4; pool_minor = 1 # Summoned Skull
        elif atk < 3000: pool_major = 4; pool_minor = 3
        else: pool_major = 5; pool_minor = 1 # Blue-Eyes e acima

        # Penalidade para monstros de tributo com status ruins
        if level >= 5 and atk < 1800:
            pool_major = max(1, pool_major - 1)
        
        # Bônus para monstros de efeito
        if "Effect" in card["type"]:
            pool_minor += 1
            if pool_minor > 5:
                pool_minor = 1
                pool_major += 1

        # Monstros de Fusão/Ritual geralmente são mais raros
        if "Fusion" in card["type"] or "Ritual" in card["type"]:
            pool_major = max(pool_major, 3)

    else:
        # Magias e Armadilhas (Estimativa por palavras-chave)
        pool_major = 2
        pool_minor = 3
        
        # Palavras-chave de cartas poderosas
        strong_keywords = ["destroy all", "draw 2", "change of heart", "monster reborn", "control", "negate"]
        weak_keywords = ["increase", "500 points", "def"]

        if any(k in desc for k in strong_keywords) or any(k in name for k in strong_keywords):
            pool_major = 4
            pool_minor = 5
        elif any(k in desc for k in weak_keywords):
            pool_major = 1
            pool_minor = 4

        # Cartas banidas clássicas (Hardcoded para ajudar)
        broken_cards = ["Pot of Greed", "Raigeki", "Dark Hole", "Monster Reborn", "Change of Heart", "Harpie's Feather Duster"]
        if card["name"] in broken_cards:
            pool_major = 5
            pool_minor = 5

    # Garantir limites
    if pool_major > 5: pool_major = 5
    if pool_major < 1: pool_major = 1
    if pool_minor > 5: pool_minor = 5
    
    return f"{pool_major}.{pool_minor}"

def main():
    if not os.path.exists(CARDS_JSON_PATH):
        print(f"ERRO: Arquivo não encontrado: {CARDS_JSON_PATH}")
        return

    print("-> Lendo cards.json...")
    with open(CARDS_JSON_PATH, 'r', encoding='utf-8') as f:
        cards = json.load(f)

    print(f"-> Gerando template para {len(cards)} cartas...")

    with open(OUTPUT_CSV_PATH, 'w', newline='', encoding='utf-8') as csvfile:
        writer = csv.writer(csvfile)
        # Cabeçalho da planilha
        writer.writerow(["ID", "Name", "Type", "ATK", "DEF", "Description", "Suggested_Pool", "Final_Pool"])

        for card in cards:
            suggested = estimate_pool(card)
            # Prepara descrição curta para caber na planilha
            short_desc = card.get("description", "").replace("\n", " ")[:100]
            
            writer.writerow([
                card["id"],
                card["name"],
                card["type"],
                card.get("atk", ""),
                card.get("def", ""),
                short_desc,
                suggested,
                "" # Coluna vazia para você preencher (se deixar vazio, usa o sugerido)
            ])

    print(f"=== SUCESSO ===")
    print(f"Arquivo gerado em: {OUTPUT_CSV_PATH}")
    print("1. Abra este arquivo no Excel ou Google Sheets.")
    print("2. A coluna 'Suggested_Pool' é o chute do script.")
    print("3. Preencha a coluna 'Final_Pool' APENAS se discordar da sugestão.")
    print("4. Salve o arquivo (mantenha o formato CSV).")

if __name__ == "__main__":
    main()
