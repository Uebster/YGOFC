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
        {"id": "001_ren", "name": "Ren", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "002_hiroki", "name": "Hiroki", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "003_nina", "name": "Nina", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "004_katsu", "name": "Katsu", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "005_kenji", "name": "Sensei Kenji", "role": "Treinamento", "difficulty": "Medium", "field": "Normal"},
        {"id": "006_mokuba", "name": "Mokuba Kaiba", "role": "Treinamento", "difficulty": "Easy", "field": "Normal"},
        {"id": "007_tristan", "name": "Tristan Taylor", "role": "Treinamento", "difficulty": "Medium", "field": "Forest"},
        {"id": "008_tea", "name": "Tea Gardner", "role": "Treinamento", "difficulty": "Medium", "field": "Normal"},
        {"id": "009_grandpa", "name": "Grandpa Muto", "role": "Treinamento", "difficulty": "Hard", "field": "Normal"},
        {"id": "010_duke", "name": "Duke Devlin", "role": "Treinamento", "difficulty": "Medium", "field": "Normal"},

        # 11-20: Torneio Local
        {"id": "011_rex", "name": "Rex Raptor", "role": "Torneio Local", "difficulty": "Medium", "field": "Wasteland"},
        {"id": "012_weevil", "name": "Weevil Underwood", "role": "Torneio Local", "difficulty": "Medium", "field": "Forest"},
        {"id": "013_mako", "name": "Mako Tsunami", "role": "Torneio Local", "difficulty": "Medium", "field": "Umi"},
        {"id": "014_joey", "name": "Joey Wheeler", "role": "Torneio Local", "difficulty": "Medium", "field": "Sogen"},
        {"id": "015_mai", "name": "Mai Valentine", "role": "Torneio Local", "difficulty": "Medium", "field": "Mountain"},
        {"id": "016_keith", "name": "Bandit Keith", "role": "Torneio Local", "difficulty": "Hard", "field": "Machine"},
        {"id": "017_espa", "name": "Espa Roba", "role": "Torneio Local", "difficulty": "Medium", "field": "Normal"},
        {"id": "018_bakura", "name": "Bakura Ryou", "role": "Torneio Local", "difficulty": "Medium", "field": "Normal"},
        {"id": "019_yami_bakura", "name": "Yami Bakura", "role": "Torneio Local", "difficulty": "Hard", "field": "Yami"},
        {"id": "020_pegasus", "name": "Maximillion Pegasus", "role": "Torneio Local", "difficulty": "Hard", "field": "Normal"},

        # 21-30: Torneio Principal
        {"id": "021_rare_hunter", "name": "Rare Hunter", "role": "Torneio Principal", "difficulty": "Medium", "field": "Normal"},
        {"id": "022_rare_hunter_elite", "name": "Rare Hunter Elite", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "023_strings", "name": "Strings", "role": "Torneio Principal", "difficulty": "Medium", "field": "Normal"},
        {"id": "024_arkana", "name": "Arkana", "role": "Torneio Principal", "difficulty": "Hard", "field": "Yami"},
        {"id": "025_odion", "name": "Odion", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "026_ishizu", "name": "Ishizu Ishtar", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "027_maki", "name": "Maki", "role": "Torneio Principal", "difficulty": "Medium", "field": "Normal"},
        {"id": "028_takagi", "name": "Sr. Takagi", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},
        {"id": "029_yugi", "name": "Yugi Muto", "role": "Torneio Principal", "difficulty": "Hard", "field": "Yami"},
        {"id": "030_kaiba", "name": "Seto Kaiba", "role": "Torneio Principal", "difficulty": "Hard", "field": "Normal"},

        # 31-40: Guardiões Elementais / Extra
        {"id": "031_niko", "name": "Niko", "role": "Extra", "difficulty": "Easy", "field": "Normal"},
        {"id": "032_amy", "name": "Amy", "role": "Extra", "difficulty": "Easy", "field": "Normal"},
        {"id": "033_desert_mage", "name": "Desert Mage", "role": "Guardião", "difficulty": "Medium", "field": "Wasteland"},
        {"id": "034_forest_mage", "name": "Forest Mage", "role": "Guardião", "difficulty": "Medium", "field": "Forest"},
        {"id": "035_mountain_mage", "name": "Mountain Mage", "role": "Guardião", "difficulty": "Medium", "field": "Mountain"},
        {"id": "036_meadow_mage", "name": "Meadow Mage", "role": "Guardião", "difficulty": "Medium", "field": "Sogen"},
        {"id": "037_ocean_mage", "name": "Ocean Mage", "role": "Guardião", "difficulty": "Medium", "field": "Umi"},
        {"id": "038_labyrinth_mage", "name": "Labyrinth Mage", "role": "Guardião", "difficulty": "Medium", "field": "Normal"},
        {"id": "039_simon", "name": "Simon Muran", "role": "Guardião", "difficulty": "Hard", "field": "Normal"},
        {"id": "040_shadi", "name": "Shadi", "role": "Guardião", "difficulty": "Hard", "field": "Normal"},

        # 41-50: Big Five / Extra
        {"id": "041_sora", "name": "Sora", "role": "Extra", "difficulty": "Medium", "field": "Normal"},
        {"id": "042_ryuu", "name": "Capitão Ryuu", "role": "Extra", "difficulty": "Hard", "field": "Normal"},
        {"id": "043_tony", "name": "Tony", "role": "Extra", "difficulty": "Medium", "field": "Normal"},
        {"id": "044_gansley", "name": "Gansley", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "045_crump", "name": "Crump", "role": "Big Five", "difficulty": "Medium", "field": "Umi"},
        {"id": "046_johnson", "name": "Johnson", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "047_leichter", "name": "Leichter", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "048_nezbitt", "name": "Nezbitt", "role": "Big Five", "difficulty": "Medium", "field": "Normal"},
        {"id": "049_noah", "name": "Noah Kaiba", "role": "Torneio Principal", "difficulty": "Hard", "field": "Umi"},
        {"id": "050_gozaburo", "name": "Gozaburo Kaiba", "role": "Torneio Principal", "difficulty": "Hard", "field": "Yami"},

        # 51-60: Vilões / Desafios Especiais
        {"id": "051_tea_adv", "name": "Tea Gardner (Dark)", "role": "Desafio", "difficulty": "Medium", "field": "Normal"},
        {"id": "052_tristan_adv", "name": "Tristan Taylor (Dark)", "role": "Desafio", "difficulty": "Medium", "field": "Forest"},
        {"id": "053_rex_adv", "name": "Rex Raptor (Dark)", "role": "Desafio", "difficulty": "Hard", "field": "Wasteland"},
        {"id": "054_weevil_adv", "name": "Weevil Underwood (Dark)", "role": "Desafio", "difficulty": "Hard", "field": "Forest"},
        {"id": "055_mako_adv", "name": "Mako Tsunami (Dark)", "role": "Desafio", "difficulty": "Hard", "field": "Umi"},
        {"id": "056_joey_adv", "name": "Joey Wheeler (Dark)", "role": "Desafio", "difficulty": "Hard", "field": "Sogen"},
        {"id": "057_mai_adv", "name": "Mai Valentine (Dark)", "role": "Desafio", "difficulty": "Hard", "field": "Mountain"},
        {"id": "058_arkana_dark", "name": "Arkana (Dark Version)", "role": "Vilão", "difficulty": "Hard", "field": "Yami"},
        {"id": "059_bakura_spirit", "name": "Bakura Ryou (Dark Spirit)", "role": "Vilão", "difficulty": "Hard", "field": "Yami"},
        {"id": "060_heishin", "name": "Heishin", "role": "Vilão", "difficulty": "Hard", "field": "Normal"},

        # 61-70: Rivais Avançados / Servos
        {"id": "061_rare_rematch", "name": "Rare Hunter (Rematch)", "role": "Servo", "difficulty": "Hard", "field": "Normal"},
        {"id": "062_rare_elite_rematch", "name": "Rare Hunter Elite (Rematch)", "role": "Servo", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "063_odion_rematch", "name": "Odion (Rematch)", "role": "Servo", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "064_strings_rematch", "name": "Strings (Rematch)", "role": "Servo", "difficulty": "Hard", "field": "Normal"},
        {"id": "065_keith_adv", "name": "Bandit Keith (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "066_espa_adv", "name": "Espa Roba (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "067_pegasus_adv", "name": "Pegasus (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "068_ishizu_adv", "name": "Ishizu (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "069_yugi_adv", "name": "Yugi Muto (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},
        {"id": "070_kaiba_adv", "name": "Seto Kaiba (Advanced)", "role": "Desafio", "difficulty": "Very Hard", "field": "Normal"},

        # 71-80: High Mages / Guardiões Avançados
        {"id": "071_desert_rematch", "name": "Desert Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Wasteland"},
        {"id": "072_forest_rematch", "name": "Forest Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Forest"},
        {"id": "073_mountain_rematch", "name": "Mountain Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Mountain"},
        {"id": "074_meadow_rematch", "name": "Meadow Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Sogen"},
        {"id": "075_ocean_rematch", "name": "Ocean Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Umi"},
        {"id": "076_isis", "name": "High Mage Isis", "role": "High Mage", "difficulty": "Very Hard", "field": "Umi"},
        {"id": "077_secmeton", "name": "High Mage Secmeton", "role": "High Mage", "difficulty": "Very Hard", "field": "Wasteland"},
        {"id": "078_martis", "name": "High Mage Martis", "role": "High Mage", "difficulty": "Very Hard", "field": "Mountain"},
        {"id": "079_kepura", "name": "High Mage Kepura", "role": "High Mage", "difficulty": "Very Hard", "field": "Sogen"},
        {"id": "080_anubisius", "name": "High Mage Anubisius", "role": "High Mage", "difficulty": "Very Hard", "field": "Yami"},

        # 81-90: Guardiões / Big Five Rematch
        {"id": "081_labyrinth_rematch", "name": "Labyrinth Mage (Rematch)", "role": "Guardião", "difficulty": "Hard", "field": "Normal"},
        {"id": "082_gansley_rematch", "name": "Gansley (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Normal"},
        {"id": "083_crump_rematch", "name": "Crump (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Umi"},
        {"id": "084_johnson_rematch", "name": "Johnson (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Normal"},
        {"id": "085_leichter_rematch", "name": "Leichter (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Wasteland"},
        {"id": "086_nezbitt_rematch", "name": "Nezbitt (Rematch)", "role": "Big Five", "difficulty": "Hard", "field": "Normal"},
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
        {"id": "097_yugi_final", "name": "Yugi Muto (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "098_pegasus_final", "name": "Pegasus J. Crawford (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "099_kaiba_final", "name": "Seto Kaiba (Final)", "role": "Final", "difficulty": "Extreme", "field": "Normal"},
        {"id": "100_marik", "name": "Marik Ishtar", "role": "Boss Final", "difficulty": "Extreme", "field": "Yami"}
    ]

def generate():
    print("Carregando banco de cartas...")
    card_map, all_ids = load_card_map()
    print(f"Cartas carregadas: {len(card_map)}")

    roster = get_character_roster()
    final_characters = []
    
    for char_data in roster:
        char_entry = {
            "id": char_data["id"],
            "name": char_data["name"],
            "deck_A": [], # Serão preenchidos pelo smart builder depois
            "extra_deck_A": [],
            "deck_B": [],
            "extra_deck_B": [],
            "deck_C": [],
            "extra_deck_C": [],
            "unique_drops": [],
            "rewards": {"s_plus": "", "s": [], "b": [], "c": [], "d": []},
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
