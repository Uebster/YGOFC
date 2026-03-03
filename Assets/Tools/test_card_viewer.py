import pygame
import json
import os
import sys

# --- Configurações ---
SCREEN_WIDTH = 800
SCREEN_HEIGHT = 600
BG_COLOR = (30, 30, 30)
TEXT_COLOR = (255, 255, 255)

# Qualidade da imagem baseada na proporção de alta resolução (813x1185)
CARD_ASPECT_RATIO = 813 / 1185
CARD_HEIGHT = 540 # Altura da carta na tela
CARD_WIDTH = int(CARD_HEIGHT * CARD_ASPECT_RATIO) # Largura calculada para manter proporção

# --- Caminhos ---
# Base: Project Root (Subindo 3 níveis de Scripts/Tools)
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CARDS_DIR = os.path.join(BASE_DIR, "Scripts", "Cards")
JSON_PATH = os.path.join(CARDS_DIR, "cards.json")

def load_db():
    print(f"Lendo banco de dados: {JSON_PATH}")
    if not os.path.exists(JSON_PATH):
        print("ERRO: cards.json não encontrado!")
        sys.exit(1)
    with open(JSON_PATH, 'r', encoding='utf-8') as f:
        return json.load(f)

def main():
    pygame.init()
    screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
    pygame.display.set_caption("Yu-Gi-Oh! Forbidden Chaos - Card Viewer")
    clock = pygame.time.Clock()
    font_title = pygame.font.SysFont("Arial", 32, bold=True)
    font_info = pygame.font.SysFont("Arial", 24)

    # Carregar Cartas
    cards = load_db()
    if not cards:
        print("Nenhuma carta no banco de dados.")
        return

    # Carregar Verso da Carta (Background)
    card_back_img = None
    # O nome padrão para o verso da carta é "0000 - Verso_da_Carta.jpg", usado pelo script de download.
    # Se você usou outro nome, renomeie o arquivo ou altere a linha abaixo.
    back_filename = "0000 - Background.jpg"
    back_path = os.path.join(CARDS_DIR, "YuGiOh_OCG_Classic_2147", back_filename)
    
    if os.path.exists(back_path):
        try:
            loaded_back = pygame.image.load(back_path)
            card_back_img = pygame.transform.scale(loaded_back, (CARD_WIDTH, CARD_HEIGHT))
            print(f"Verso da carta '{back_filename}' carregado com sucesso!")
        except Exception as e:
            print(f"ERRO ao carregar verso: {e}")
    else:
        print(f"AVISO: Verso da carta não encontrado em '{back_path}'.")
        print("A função de virar a carta (clique) não funcionará visualmente.")

    current_index = 0
    running = True
    
    # Cache de imagem para não carregar do disco todo frame
    current_image = None
    last_index = -1
    is_flipped = False # Estado da carta (False = Frente, True = Verso)

    while running:
        # 1. Event Handling
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
            elif event.type == pygame.MOUSEBUTTONDOWN:
                if event.button == 1: # Clique esquerdo
                    mouse_x, mouse_y = pygame.mouse.get_pos()
                    # Coordenadas da carta (mesmas usadas no draw)
                    img_x = 50
                    img_y = (SCREEN_HEIGHT - CARD_HEIGHT) // 2
                    card_rect = pygame.Rect(img_x, img_y, CARD_WIDTH, CARD_HEIGHT)
                    
                    if card_rect.collidepoint(mouse_x, mouse_y):
                        is_flipped = not is_flipped # Inverte o estado

            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_RIGHT:
                    current_index = (current_index + 1) % len(cards)
                    is_flipped = False # Reseta para frente ao mudar de carta
                elif event.key == pygame.K_LEFT:
                    current_index = (current_index - 1) % len(cards)
                    is_flipped = False # Reseta para frente ao mudar de carta
                elif event.key == pygame.K_ESCAPE:
                    running = False

        # 2. Update Data
        card = cards[current_index]
        
        # Carregar imagem se mudou de carta
        if current_index != last_index:
            img_path = os.path.join(CARDS_DIR, card["image_filename"])
            # Normalizar separadores de caminho para o SO atual
            img_path = img_path.replace("/", os.sep).replace("\\", os.sep)
            
            if os.path.exists(img_path):
                try:
                    loaded_img = pygame.image.load(img_path)
                    current_image = pygame.transform.scale(loaded_img, (CARD_WIDTH, CARD_HEIGHT))
                except Exception as e:
                    print(f"Erro ao carregar imagem: {e}")
                    current_image = None
            else:
                print(f"Imagem não encontrada: {img_path}")
                current_image = None
            last_index = current_index

        # 3. Draw
        screen.fill(BG_COLOR)

        # Desenhar Imagem
        img_x = 50
        img_y = (SCREEN_HEIGHT - CARD_HEIGHT) // 2
        
        if is_flipped and card_back_img:
            screen.blit(card_back_img, (img_x, img_y))
        elif current_image:
            screen.blit(current_image, (img_x, img_y))
        else:
            # Placeholder se não tiver imagem
            pygame.draw.rect(screen, (100, 100, 100), (img_x, img_y, CARD_WIDTH, CARD_HEIGHT))
            text_missing = font_info.render("Imagem Ausente", True, TEXT_COLOR)
            screen.blit(text_missing, (img_x + 50, img_y + 200))

        # Desenhar Textos (Lado Direito)
        info_x = img_x + CARD_WIDTH + 50
        info_y = img_y
        
        # ID e Nome
        text_id = font_info.render(f"ID: {card['id']}", True, (200, 200, 200))
        screen.blit(text_id, (info_x, info_y))
        
        text_name = font_title.render(card['name'], True, (255, 215, 0)) # Dourado
        screen.blit(text_name, (info_x, info_y + 40))

        # Dados
        y_offset = 100
        
        # Tipo
        type_str = f"[{card.get('type', '???')}]"
        if 'race' in card: type_str += f" / {card['race']}"
        screen.blit(font_info.render(type_str, True, TEXT_COLOR), (info_x, info_y + y_offset))
        y_offset += 40

        # Nível / Atributo
        if 'level' in card:
            screen.blit(font_info.render(f"Level: {card['level']}", True, TEXT_COLOR), (info_x, info_y + y_offset))
            y_offset += 40
        
        # ATK / DEF
        if 'atk' in card:
            atk_def_str = f"ATK: {card['atk']}"
            if 'def' in card: atk_def_str += f" / DEF: {card['def']}"
            screen.blit(font_info.render(atk_def_str, True, (255, 100, 100)), (info_x, info_y + y_offset))
            y_offset += 40

        # Descrição (Quebra de linha simples)
        desc = card.get('description', '')
        if desc:
            words = desc.split(' ')
            line = ""
            desc_y = info_y + y_offset + 20
            font_desc = pygame.font.SysFont("Arial", 18)
            
            # Largura máxima do texto da descrição, calculada dinamicamente
            max_desc_width = SCREEN_WIDTH - info_x - 20
            
            for word in words:
                test_line = line + word + " "
                if font_desc.size(test_line)[0] > max_desc_width:
                    screen.blit(font_desc.render(line, True, (200, 200, 200)), (info_x, desc_y))
                    desc_y += 25
                    line = word + " "
                else:
                    line = test_line
            screen.blit(font_desc.render(line, True, (200, 200, 200)), (info_x, desc_y))

        # Controles
        footer_text = font_info.render(f"Carta {current_index + 1} de {len(cards)} | Setas: Navegar | Clique: Virar | ESC: Sair", True, (150, 150, 150))
        screen.blit(footer_text, (20, SCREEN_HEIGHT - 40))

        pygame.display.flip()
        clock.tick(30)

    pygame.quit()

if __name__ == "__main__":
    main()
