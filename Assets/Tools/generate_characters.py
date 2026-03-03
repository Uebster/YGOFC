import json
import os
import random

# Configuração de Caminhos
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHARACTERS_OUT = os.path.join(BASE_DIR, "Characters", "characters.json")
CARDS_JSON = os.path.join(BASE_DIR, "Cards", "cards.json")

def load_card_map():
    """Carrega o cards.json e cria um mapa de Nome -> ID"""
    if not os.path.exists(CARDS_JSON):
        print(f"[AVISO] cards.json não encontrado em: {CARDS_JSON}")
        return {}, []
    
    with open(CARDS_JSON, 'r', encoding='utf-8') as f:
        cards = json.load(f)
    
    # Retorna mapa {nome_lower: id} e lista de todos os IDs para preenchimento
    return {c["name"].lower(): c["id"] for c in cards}, [c["id"] for c in cards]

def get_character_roster():
    """Retorna a lista fixa de 100 personagens definidos."""
    return [
        # 01-10: Treinamento Inicial
        {"id": "001_novice", "name": "Novice Duelist", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "002_student", "name": "Student Duelist", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "003_female_student", "name": "Female Duelist", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "004_tristan", "name": "Tristan Taylor", "role": "Treinamento", "difficulty": "Easy", "field": "Forest"},
        {"id": "005_tea", "name": "Tea Gardner", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "006_grandpa", "name": "Grandpa Solomon Muto", "role": "Treinamento", "difficulty": "Medium", "field": "Normal"},
        {"id": "007_intermediate", "name": "Intermediate Duelist", "role": "Treinamento", "difficulty": "Medium", "field": "Normal"},
        {"id": "008_expert", "name": "Expert Duelist", "role": "Treinamento", "difficulty": "Hard", "field": "Normal"},
        {"id": "009_mokuba", "name": "Mokuba Kaiba", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "010_duke", "name": "Duke Devlin", "role": "Treinamento", "difficulty": "Medium", "field": "Normal"},

        # 11-20: Torneio Local
        {"id": "011_joey", "name": "Joey Wheeler", "role": "Torneio Local", "difficulty": "Medium", "field": "Sogen"},
        {"id": "012_mai", "name": "Mai Valentine", "role": "Torneio Local", "difficulty": "Medium", "field": "Mountain"},
        {"id": "013_rex", "name": "Rex Raptor", "role": "Torneio Local", "difficulty": "Medium", "field": "Wasteland"},
        {"id": "014_weevil", "name": "Weevil Underwood", "role": "Torneio Local", "difficulty": "Medium", "field": "Forest"},
        {"id": "015_mako", "name": "Mako Tsunami", "role": "Torneio Local", "difficulty": "Medium", "field": "Umi"},
        {"id": "016_keith", "name": "Bandit Keith", "role": "Torneio Local", "difficulty": "Hard", "field": "Normal"},
        {"id": "017_espa", "name": "Espa Roba", "role": "Torneio Local", "difficulty": "Medium", "field": "Normal"},
        {"id": "018_bakura", "name": "Bakura Ryou", "role": "Torneio Local", "difficulty": "Easy", "field": "Normal"},
        {"id": "019_yami_bakura", "name": "Dark Bakura", "role": "Torneio Local", "difficulty": "Hard", "field": "Yami"},
        {"id": "020_pegasus", "name": "Pegasus J. Crawford", "role": "Torneio Local", "difficulty": "Hard", "field": "Normal"},

        # 21-30: Torneio Principal
        {"id": "021_kaiba", "name": "Seto Kaiba", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "022_yugi", "name": "Yugi Muto", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "023_noah", "name": "Noah Kaiba", "role": "Torneio Principal", "difficulty": "Hard", "field": "Umi"},
        {"id": "024_gozaburo", "name": "Gozaburo Kaiba", "role": "Torneio Principal", "difficulty": "Hard", "field": "Yami"},
        {"id": "025_ishizu", "name": "Ishizu Ishtar", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "026_odion", "name": "Odion", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "027_strings", "name": "Strings", "role": "Torneio Principal", "difficulty": "Medium", "field": "Normal"},
        {"id": "028_arkana", "name": "Arkana", "role": "Torneio Principal", "difficulty": "Medium", "field": "Yami"},
        {"id": "029_rare_hunter", "name": "Rare Hunter", "role": "Torneio Principal", "difficulty": "Medium", "field": "Normal"},
        {"id": "030_rare_hunter_elite", "name": "Rare Hunter Elite", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},

        # 31-40: Guardiões Elementais / Extra
        {"id": "031_desert_mage", "name": "Desert Mage", "role": "Guardião", "difficulty": "Medium", "field": "Wasteland"},
        {"id": "032_forest_mage", "name": "Forest Mage", "role": "Guardião", "difficulty": "Medium", "field": "Forest"},
        {"id": "033_mountain_mage", "name": "Mountain Mage", "role": "Guardião", "difficulty": "Medium", "field": "Mountain"},
        {"id": "034_meadow_mage", "name": "Meadow Mage", "role": "Guardião", "difficulty": "Medium", "field": "Sogen"},
        {"id": "035_ocean_mage", "name": "Ocean Mage", "role": "Guardião", "difficulty": "Medium", "field": "Umi"},
        {"id": "036_labyrinth_mage", "name": "Labyrinth Mage", "role": "Guardião", "difficulty": "Medium", "field": "Normal"},
        {"id": "037_simon", "name": "Simon Muran", "role": "Guardião", "difficulty": "Medium", "field": "Normal"},
        {"id": "038_shadi", "name": "Shadi", "role": "Guardião", "difficulty": "Medium", "field": "Normal"},
        {"id": "039_novice_gbc", "name": "Novice Duelist (GBC)", "role": "Extra", "difficulty": "Easy", "field": "Normal"},
        {"id": "040_female_gbc", "name": "Female Duelist (GBC)", "role": "Extra", "difficulty": "Easy", "field": "Normal"},

        # 41-50: Big Five / Extra
        {"id": "041_gansley", "name": "Gansley", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "042_crump", "name": "Crump", "role": "Big Five", "difficulty": "Medium", "field": "Umi"},
        {"id": "043_johnson", "name": "Johnson", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "044_leclyde", "name": "LeClyde", "role": "Big Five", "difficulty": "Medium", "field": "Wasteland"},
        {"id": "045_crockett", "name": "Crockett", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "046_intermediate_gbc", "name": "Intermediate Duelist (GBC)", "role": "Extra", "difficulty": "Medium", "field": "Normal"},
        {"id": "047_expert_gbc", "name": "Expert Duelist (GBC)", "role": "Extra", "difficulty": "Hard", "field": "Normal"},
        {"id": "048_student_gbc", "name": "Student Duelist (GBC)", "role": "Extra", "difficulty": "Easy", "field": "Normal"},
        {"id": "049_female_gba", "name": "Female Duelist (GBA)", "role": "Extra", "difficulty": "Medium", "field": "Normal"},
        {"id": "050_grandpa_gba", "name": "Grandpa Solomon Muto (GBA)", "role": "Extra", "difficulty": "Hard", "field": "Normal"},

        # 51-60: Vilões / Desafios Especiais
        {"id": "051_heishin", "name": "Heishin", "role": "Vilão", "difficulty": "Hard", "field": "Normal"},
        {"id": "052_arkana_dark", "name": "Arkana (Dark Version)", "role": "Vilão", "difficulty": "Hard", "field": "Yami"},
        {"id": "053_bakura_spirit", "name": "Bakura Ryou (Dark Spirit)", "role": "Vilão", "difficulty": "Hard", "field": "Yami"},
        {"id": "054_tea_adv", "name": "Tea Gardner (Advanced)", "role": "Desafio", "difficulty": "Medium", "field": "Normal"},
        {"id": "055_tristan_adv", "name": "Tristan Taylor (Advanced)", "role": "Desafio", "difficulty": "Medium", "field": "Forest"},
        {"id": "056_joey_adv", "name": "Joey Wheeler (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Sogen"},
        {"id": "057_mai_adv", "name": "Mai Valentine (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Mountain"},
        {"id": "058_rex_adv", "name": "Rex Raptor (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Wasteland"},
        {"id": "059_weevil_adv", "name": "Weevil Underwood (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Forest"},
        {"id": "060_mako_adv", "name": "Mako Tsunami (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Umi"},

        # 61-70: Rivais Avançados / Servos
        {"id": "061_keith_adv", "name": "Bandit Keith (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Normal"},
        {"id": "062_espa_adv", "name": "Espa Roba (Advanced)", "role": "Desafio", "difficulty": "Hard", "field": "Normal"},
        {"id": "063_pegasus_adv", "name": "Pegasus J. Crawford (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "064_kaiba_adv", "name": "Seto Kaiba (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "065_yugi_adv", "name": "Yugi Muto (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "066_ishizu_adv", "name": "Ishizu Ishtar (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "067_odion_rematch", "name": "Odion (Rematch)", "role": "Servo", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "068_strings_rematch", "name": "Strings (Rematch)", "role": "Servo", "difficulty": "Hard", "field": "Normal"},
        {"id": "069_rare_hunter_rematch", "name": "Rare Hunter (Rematch)", "role": "Servo", "difficulty": "Hard", "field": "Normal"},
        {"id": "070_rare_elite_rematch", "name": "Rare Hunter Elite (Rematch)", "role": "Servo", "difficulty": "Very Hard", "field": "Normal"},

        # 71-80: High Mages / Guardiões Avançados
        {"id": "071_anubisius", "name": "High Mage Anubisius", "role": "High Mage", "difficulty": "Very Hard", "field": "Yami"},
        {"id": "072_isis", "name": "High Mage Isis", "role": "High Mage", "difficulty": "Very Hard", "field": "Umi"},
        {"id": "073_secmeton", "name": "High Mage Secmeton", "role": "High Mage", "difficulty": "Very Hard", "field": "Wasteland"},
        {"id": "074_martis", "name": "High Mage Martis", "role": "High Mage", "difficulty": "Very Hard", "field": "Mountain"},
        {"id": "075_kepura", "name": "High Mage Kepura", "role": "High Mage", "difficulty": "Very Hard", "field": "Sogen"},
        {"id": "076_desert_rematch", "name": "Desert Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Wasteland"},
        {"id": "077_forest_rematch", "name": "Forest Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Forest"},
        {"id": "078_mountain_rematch", "name": "Mountain Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Mountain"},
        {"id": "079_meadow_rematch", "name": "Meadow Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Sogen"},
        {"id": "080_ocean_rematch", "name": "Ocean Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Umi"},

        # 81-90: Guardiões / Big Five Rematch
        {"id": "081_labyrinth_rematch", "name": "Labyrinth Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Normal"},
        {"id": "082_gansley_rematch", "name": "Gansley (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Normal"},
        {"id": "083_crump_rematch", "name": "Crump (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Umi"},
        {"id": "084_johnson_rematch", "name": "Johnson (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Normal"},
        {"id": "085_leclyde_rematch", "name": "LeClyde (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Wasteland"},
        {"id": "086_crockett_rematch", "name": "Crockett (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Normal"},
        {"id": "087_simon_rematch", "name": "Simon Muran (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Normal"},
        {"id": "088_shadi_rematch", "name": "Shadi (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Normal"},
        {"id": "089_bakura_final", "name": "Bakura Ryou (Final)", "role": "Final", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "090_dark_bakura_final", "name": "Dark Bakura (Final)", "role": "Final", "difficulty": "Extreme", "field": "Yami"},

        # 91-100: Final Rematches / Marik
        {"id": "091_joey_final", "name": "Joey Wheeler (Final)", "role": "Final", "difficulty": "Extreme", "field": "Sogen"},
        {"id": "092_mai_final", "name": "Mai Valentine (Final)", "role": "Final", "difficulty": "Extreme", "field": "Mountain"},
        {"id": "093_rex_final", "name": "Rex Raptor (Final)", "role": "Final", "difficulty": "Extreme", "field": "Wasteland"},
        {"id": "094_weevil_final", "name": "Weevil Underwood (Final)", "role": "Final", "difficulty": "Extreme", "field": "Forest"},
        {"id": "095_mako_final", "name": "Mako Tsunami (Final)", "role": "Final", "difficulty": "Extreme", "field": "Umi"},
        {"id": "096_keith_final", "name": "Bandit Keith (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "097_espa_final", "name": "Espa Roba (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "098_pegasus_final", "name": "Pegasus J. Crawford (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "099_kaiba_final", "name": "Seto Kaiba (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "100_marik", "name": "Marik Ishtar", "role": "Boss Final", "difficulty": "Extreme", "field": "Yami"}
    ]

def get_default_deck(name, card_map, all_ids):
    # Decks simplificados baseados no nome do personagem
    name_lower = name.lower()
    deck_names = ["Celtic Guardian", "Silver Fang", "Mammoth Graveyard"] # Base genérica
    
    if "yugi" in name_lower or "grandpa" in name_lower or "simon" in name_lower:
        deck_names += ["Dark Magician", "Curse of Dragon", "Giant Soldier of Stone", "Mystical Elf", "Book of Secret Arts", "Summoned Skull"]
    elif "kaiba" in name_lower or "seto" in name_lower or "mokuba" in name_lower or "gozaburo" in name_lower or "noah" in name_lower:
        deck_names += ["Blue-Eyes White Dragon", "Battle Ox", "Ryu-Kishin Powered", "La Jinn the Mystical Genie", "Lord of D."]
    elif "joey" in name_lower:
        deck_names += ["Red-Eyes B. Dragon", "Flame Swordsman", "Time Wizard", "Baby Dragon", "Axe Raider", "Panther Warrior"]
    elif "mai" in name_lower:
        deck_names += ["Harpie Lady", "Harpie's Pet Dragon", "Harpie Lady Sisters", "Cyber Shield", "Elegant Egotist"]
    elif "rex" in name_lower:
        deck_names += ["Two-Headed King Rex", "Crawling Dragon #2", "Uraby", "Megazowler"]
    elif "weevil" in name_lower:
        deck_names += ["Great Moth", "Basic Insect", "Cocoon of Evolution", "Insect Queen", "Petit Moth"]
    elif "mako" in name_lower or "ocean" in name_lower:
        deck_names += ["Kairyu-Shin", "Jellyfish", "Great White", "The Legendary Fisherman", "Umi"]
    elif "keith" in name_lower:
        deck_names += ["Slot Machine", "Launcher Spider", "Blast Sphere", "Metalmorph"]
    elif "pegasus" in name_lower:
        deck_names += ["Toon Summoned Skull", "Toon Mermaid", "Manga Ryu-Ran", "Blue-Eyes Toon Dragon", "Dragon Piper", "Bickuribox", "Toon World"]
    elif "bakura" in name_lower:
        deck_names += ["Dark Necrofear", "Headless Knight", "Earthbound Spirit", "The Earl of Demise", "Change of Heart"]
    elif "marik" in name_lower or "rare hunter" in name_lower or "strings" in name_lower or "odion" in name_lower:
        deck_names += ["Drill Bug", "Gil Garth", "Viser Des", "Lava Golem", "Bowganian"]
    elif "ishizu" in name_lower:
        deck_names += ["Kelbek", "Agido", "Zolga", "Mudora"]
    elif "shadi" in name_lower:
        deck_names += ["Millennium Golem", "Sanga of the Thunder"]
    elif "mage" in name_lower:
        if "mountain" in name_lower: deck_names += ["Twin-Headed Thunder Dragon", "Thunder Dragon"]
        elif "meadow" in name_lower: deck_names += ["Gaia the Fierce Knight", "Curse of Dragon"]
        elif "forest" in name_lower: deck_names += ["Great Moth", "Hercules Beetle"]
        elif "desert" in name_lower: deck_names += ["Labyrinth Wall", "Wall Shadow"]
    elif "novice" in name_lower or "student" in name_lower:
        deck_names += ["Hitotsu-Me Giant", "M-Warrior #1", "M-Warrior #2"]
    elif "expert" in name_lower:
        deck_names += ["Gemini Elf", "Vorse Raider", "Mechanicalchaser"]
    
    # Converter Nomes para IDs
    deck_ids = []
    for c_name in deck_names:
        cid = card_map.get(c_name.lower())
        if cid:
            deck_ids.append(cid)
            
    # Preencher até 40 cartas com cartas aleatórias do banco
    while len(deck_ids) < 40:
        if all_ids:
            deck_ids.append(random.choice(all_ids))
        else:
            deck_ids.append("0001") # Fallback se o banco estiver vazio
            
    return deck_ids[:40]

def generate():
    print("Carregando banco de cartas...")
    card_map, all_ids = load_card_map()
    print(f"Cartas carregadas: {len(card_map)}")

    roster = get_character_roster()
    final_characters = []
    
    for char_data in roster:
        deck = get_default_deck(char_data["name"], card_map, all_ids)
        
        char_entry = {
            "id": char_data["id"],
            "name": char_data["name"],
            "deck_A": deck,
            "deck_B": deck, # Placeholder: mesmo deck por enquanto
            "deck_C": deck, # Placeholder
            "rewards": [], # Placeholder
            "field": char_data["field"],
            "difficulty": char_data["difficulty"],
            "story_role": char_data["role"]
        }
        final_characters.append(char_entry)
    
    os.makedirs(os.path.dirname(CHARACTERS_OUT), exist_ok=True)
    with open(CHARACTERS_OUT, 'w', encoding='utf-8') as f:
        json.dump(final_characters, f, indent=2)
        
    print(f"Sucesso! {len(final_characters)} personagens gerados em {CHARACTERS_OUT}")

if __name__ == "__main__":
    generate()
