import json
import os
import csv
import tkinter as tk
from tkinter import filedialog

# Dicionário para forçar cartas específicas em Pools exatos.
# Adicione qualquer carta aqui que você queira blindar com um Tier fixo.
PREDEFINED_POOLS = {
    # Antigas 5.5 rebaixadas para 5.1 (Aparecem a partir do Ato 9)
    "Pot of Greed": "5.1",
    "Raigeki": "5.1",
    "Dark Hole": "5.1",
    "Monster Reborn": "5.1",
    "Change of Heart": "5.1",
    "Harpie's Feather Duster": "5.1",
    "Graceful Charity": "5.1",
    "Delinquent Duo": "5.1",
    "The Forceful Sentry": "5.1",
    "Confiscation": "5.1",
    "Snatch Steal": "5.1",
    "Premature Burial": "5.1",
    "Imperial Order": "5.1",
    "Mirror Force": "5.1",
    "Call of the Haunted": "5.1",
    "Ring of Destruction": "5.1",
    "Jinzo": "5.1",
    "Magical Scientist": "5.1",
    "Cyber Jar": "5.1",
    "Fiber Jar": "5.1",
    "Thousand-Eyes Restrict": "5.1",
    "Barrel Dragon": "5.1",
    
    # As verdadeiras 5.5 absolutas
    "Black Luster Soldier - Envoy of the Beginning": "5.5",
    "Chaos Emperor Dragon - Envoy of the End": "5.5",

    # Monstros Fracos mas Mortais (Exceções Blindadas)
    "Injection Fairy Lily": "4.5",
    "Spirit Reaper": "4.5",
    "Marshmallon": "4.5",
    "Sinister Serpent": "4.5",
    "Tribe-Infecting Virus": "4.5",
    "D.D. Warrior Lady": "4.5",
    "D.D. Assailant": "4.5",
    "Sangan": "4.5",
    "Witch of the Black Forest": "4.5",
    "Morphing Jar": "4.5",
    "Mask of Darkness": "4.1",
    "Exiled Force": "4.1",
    "Relinquished": "4.1",
    "Black Illusion Ritual": "4.1",
    "Legendary Jujitsu Master": "4.1",
    "Axe of Despair": "4.1",
    "United We Stand": "4.5",
    "Mage Power": "4.5",
    
    "Copycat": "3.5",
    "Penguin Soldier": "3.5",
    "Man-Eater Bug": "3.5",
    "Magician of Faith": "3.5",
    "Senju of the Thousand Hands": "3.5",
    "Sonic Bird": "3.5",
    "Wall of Illusion": "3.5",
    "Slate Warrior": "3.5",
    
    "Trap Hole": "3.1",
    "Bottomless Trap Hole": "3.5",
    "Kuriboh": "2.5" # Exemplo: Forçar o Kuriboh a ser Pool 2.5
}

# A Banlist do Jogo (0 = Forbidden, 1 = Limited, 2 = Semi)
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

def estimate_pool(card):
    """
    Tenta adivinhar o Pool (1.1 a 5.5) baseado nos status da carta.
    Isso serve como base para o usuário não ter que preencher tudo do zero.
    """
    name = card.get("name", "")
    if name in PREDEFINED_POOLS:
        return PREDEFINED_POOLS[name]

    # Filtro 2: A Regra da Banlist
    card_id = card.get("id", "")
    if card_id in BANLIST:
        limit = BANLIST[card_id]
        if limit == 0: return "5.5"
        if limit == 1: return "4.5"
        if limit == 2: return "3.5"

    pool_major = 1
    pool_minor = 1
    
    desc = card.get("description", "").lower()
    name = card.get("name", "").lower()
    card_type = card.get("type", "")

    # Filtro 3: O Pente Fino dos Status
    if card_type.startswith("Monster"):
        level = card.get("level", 1)
        atk = card.get("atk", 0)
        defense = card.get("def", 0)
        
        # Sobrevivência (Maior Status domina)
        max_stat = max(atk, defense)
        
        if max_stat < 500: pool_major = 1; pool_minor = 1
        elif max_stat < 800: pool_major = 1; pool_minor = 3
        elif max_stat < 1200: pool_major = 1; pool_minor = 5
        elif max_stat < 1400: pool_major = 2; pool_minor = 1
        elif max_stat < 1600: pool_major = 2; pool_minor = 3
        elif max_stat < 1800: pool_major = 2; pool_minor = 5
        elif max_stat < 1900: pool_major = 3; pool_minor = 1
        elif max_stat < 2100: pool_major = 3; pool_minor = 3
        elif max_stat < 2400: pool_major = 3; pool_minor = 5 
        elif max_stat < 2600: pool_major = 4; pool_minor = 1 
        elif max_stat < 3000: pool_major = 4; pool_minor = 3
        else: pool_major = 5; pool_minor = 1 # Blue-Eyes e acima

        # Regra do Custo/Benefício (A "Taxa de Tributo")
        is_extra = "Fusion" in card_type or "Ritual" in card_type or "Synchro" in card_type or "Xyz" in card_type
        if not is_extra:
            if level <= 4 and atk >= 1900: # Recompensa apenas pro ATK esmagador de nível 4 (Gemini Elf, etc)
                pool_major += 1
            elif level in [5, 6] and max_stat < 2000: # Sacrifício inútil
                pool_major -= 1
            elif level >= 7 and max_stat < 2500: # Dois sacrifícios jogados fora
                pool_major -= 1
        
        # Imposto de Efeito (Transborda matematicamente)
        if "Effect" in card_type or "Flip" in card_type:
            pool_minor += 2
            if pool_minor > 5:
                pool_minor = pool_minor % 5
                if pool_minor == 0: pool_minor = 1
                pool_major += 1

        # Monstros de Fusão/Ritual geralmente são mais raros
        if is_extra:
            pool_major = max(pool_major, 3)

    else:
        # Magias e Armadilhas (Estimativa por palavras-chave)
        pool_major = 2
        pool_minor = 3
        
        # Palavras-chave de cartas poderosas
        strong_keywords = ["destroy all", "draw 2", "change of heart", "monster reborn", "control", "negate", "special summon", "equip"]
        weak_keywords = ["increase", "500 points", "def", "gain"]

        if any(k in desc for k in strong_keywords) or any(k in name for k in strong_keywords):
            pool_major = 3
            pool_minor = 5
        elif any(k in desc for k in weak_keywords):
            pool_major = 1
            pool_minor = 4

    if pool_major > 5: pool_major = 5
    if pool_major < 1: pool_major = 1
    if pool_minor > 5: pool_minor = 5
    if pool_minor < 1: pool_minor = 1
    
    # Previne que o teto absoluto seja alcançado por cartas genéricas burras
    if pool_major == 5 and pool_minor > 1:
        pool_minor = 1 # O máximo para cartas fora da Banlist/Predefined é 5.1

    return f"{pool_major}.{pool_minor}"

def main():
    root = tk.Tk()
    root.withdraw()

    print("-> Selecione o arquivo JSON de cartas...")
    cards_json_path = filedialog.askopenfilename(
        title="Selecione o arquivo JSON de cartas",
        filetypes=[("Arquivos JSON", "*.json"), ("Todos os Arquivos", "*.*")]
    )
    if not cards_json_path:
        print("Seleção cancelada.")
        return

    print("-> Escolha onde salvar o template CSV...")
    output_csv_path = filedialog.asksaveasfilename(
        title="Salvar template CSV como...",
        defaultextension=".csv",
        filetypes=[("Arquivos CSV", "*.csv"), ("Todos os Arquivos", "*.*")]
    )
    if not output_csv_path:
        print("Seleção cancelada.")
        return

    print(f"-> Lendo {os.path.basename(cards_json_path)}...")
    with open(cards_json_path, 'r', encoding='utf-8') as f:
        cards = json.load(f)

    print(f"-> Gerando template para {len(cards)} cartas...")

    with open(output_csv_path, 'w', newline='', encoding='utf-8') as csvfile:
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
    print(f"Arquivo gerado em: {output_csv_path}")
    print("1. Abra este arquivo no Excel ou Google Sheets.")
    print("2. A coluna 'Suggested_Pool' é o chute do script.")
    print("3. Preencha a coluna 'Final_Pool' APENAS se discordar da sugestão.")
    print("4. Salve o arquivo (mantenha o formato CSV).")

if __name__ == "__main__":
    main()
