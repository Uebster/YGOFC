import json
import os
import sys

# Adiciona o diretório base ao path para importar o duel_core
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.append(BASE_DIR)

from Engine.duel_core import Player

# Caminhos
CARDS_JSON = os.path.join(BASE_DIR, "Scripts", "Cards", "cards.json")
CHARACTERS_JSON = os.path.join(BASE_DIR, "Scripts", "Characters", "characters.json")

def load_json(path):
    if not os.path.exists(path):
        print(f"ERRO: Arquivo não encontrado: {path}")
        return None
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

def main():
    print("=== Teste de Sistema de Deck ===")
    
    # 1. Carregar Banco de Cartas
    print("-> Carregando cartas...")
    cards_data = load_json(CARDS_JSON)
    if not cards_data: return
    
    # Criar dicionário de busca rápida (ID -> Dados)
    card_db = {c["id"]: c for c in cards_data}
    print(f"   {len(card_db)} cartas carregadas.")

    # 2. Carregar Personagens
    print("-> Carregando personagens...")
    chars_data = load_json(CHARACTERS_JSON)
    if not chars_data: return
    print(f"   {len(chars_data)} personagens carregados.")

    # 3. Selecionar um oponente (Ex: Yugi)
    # Procura por alguém com 'yugi' no nome ou pega o primeiro
    opponent_data = next((c for c in chars_data if "yugi" in c["name"].lower()), chars_data[0])
    print(f"\n-> Personagem Selecionado: {opponent_data['name']} (ID: {opponent_data['id']})")

    # 4. Inicializar Jogador (Simulação)
    # O Player recebe a lista de IDs do deck e o banco de dados para converter
    player = Player(opponent_data['name'], opponent_data['deck_A'], card_db)
    
    print(f"-> Deck montado com {player.deck.count()} cartas.")
    
    # 5. Simular Duelo
    print("\n--- Iniciando Simulação ---")
    print("1. Embaralhando Deck...")
    player.deck.shuffle()
    
    print("2. Comprando Mão Inicial (5 cartas)...")
    for i in range(5):
        card = player.draw_card()
        if card:
            print(f"   Saque {i+1}: {card.name} (ATK: {card.atk}/DEF: {card.defense})")
            
    print(f"\n-> Cartas na mão: {len(player.hand)}")
    print(f"-> Cartas restantes no deck: {player.deck.count()}")

if __name__ == "__main__":
    main()
