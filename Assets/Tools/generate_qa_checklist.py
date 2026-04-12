import os
import json
import tkinter as tk
from tkinter import filedialog

def generate_qa_checklist():
    print("=== Gerador de Checklist de QA (Scripts LUA) ===")
    
    root = tk.Tk()
    root.withdraw()
    
    # 1. Pedir o cards.json para poder ler os nomes e os tipos
    print("-> 1/3: Selecione o arquivo 'cards.json'...")
    cards_json_path = filedialog.askopenfilename(
        title="Selecione o arquivo cards.json",
        filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
    )
    if not cards_json_path:
        print("Operação cancelada.")
        return
        
    # 2. Pedir a pasta onde os arquivos .lua estão fisicamente
    print("-> 2/3: Selecione a pasta com os scripts LUA ('LuaScripts')...")
    lua_dir = filedialog.askdirectory(
        title="Selecione a pasta LuaScripts"
    )
    if not lua_dir:
        print("Operação cancelada.")
        return
        
    # 3. Pedir onde salvar o arquivo final
    print("-> 3/3: Escolha onde salvar o Checklist Markdown...")
    output_path = filedialog.asksaveasfilename(
        title="Salvar QA_Card_Checklist.md como...",
        defaultextension=".md",
        initialfile="QA_Card_Checklist.md",
        filetypes=[("Markdown Files", "*.md")]
    )
    if not output_path:
        print("Operação cancelada.")
        return

    # Carrega o banco de dados e cria um dicionário indexado pelo ID
    try:
        with open(cards_json_path, 'r', encoding='utf-8') as f:
            cards_db = json.load(f)
    except Exception as e:
        print(f"Erro ao ler cards.json: {e}")
        return
        
    card_dict = {}
    for card in cards_db:
        card_id = str(card.get("id", ""))
        card_dict[card_id] = card
        try:
            card_dict[str(int(card_id))] = card # Fallback para IDs sem zeros a esquerda
        except ValueError:
            pass

    # Guarda os nomes dos arquivos lua existentes na pasta (em minúsculo para garantir a busca)
    existing_lua_files = {f.lower() for f in os.listdir(lua_dir) if f.endswith('.lua')}

    spells, traps, effect_monsters = [], [], []
    scripts_encontrados = 0

    for card in cards_db:
        card_id = str(card.get("id", ""))
        c_type = card.get("type", "")
        c_name = card.get("name", "Unknown Name")
        
        # Filtra apenas cartas com efeito do JSON inteiro
        is_spell = "Spell" in c_type or "Magic" in c_type
        is_trap = "Trap" in c_type
        is_effect_monster = "Monster" in c_type and ("Effect" in c_type or "Fusion" in c_type or "Ritual" in c_type)
        
        if not (is_spell or is_trap or is_effect_monster):
            continue
            
        # Verifica se o script LUA existe fisicamente na pasta (cobre formatos "c001.lua" ou "001.lua")
        expected_name1 = f"c{card_id}.lua".lower()
        expected_name2 = f"{card_id}.lua".lower()
        
        try:
            numeric_id = str(int(card_id))
            expected_name3 = f"c{numeric_id}.lua".lower()
            expected_name4 = f"{numeric_id}.lua".lower()
        except ValueError:
            expected_name3 = ""
            expected_name4 = ""
            
        has_script = (expected_name1 in existing_lua_files or 
                      expected_name2 in existing_lua_files or 
                      (expected_name3 and expected_name3 in existing_lua_files) or 
                      (expected_name4 and expected_name4 in existing_lua_files))
        
        if has_script:
            scripts_encontrados += 1
            status = "✅ `[LUA OK]`"
        else:
            status = "❌ `[FALTA LUA]`"
            
        item = f"- [ ] `{card_id}` - **{c_name}** {status}"
        
        if is_spell: spells.append(item)
        elif is_trap: traps.append(item)
        elif is_effect_monster: effect_monsters.append(item)

    # Ordena alfabeticamente pela string do item
    spells.sort(); traps.sort(); effect_monsters.sort()

    total_cards = len(spells) + len(traps) + len(effect_monsters)

    # Escreve o documento
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# 🧪 Checklist de Homologação de Efeitos LUA (QA)\n")
            f.write("Utilize este documento para rastrear o progresso de testes dos scripts LUA das cartas.\n\n")
            f.write(f"**Total de Cartas com Efeitos no JSON:** {total_cards}\n")
            f.write(f"**Scripts LUA Encontrados na Pasta:** {scripts_encontrados}\n\n")
            
            if spells: f.write(f"## Magias (Spells) ({len(spells)})\n" + "\n".join(spells) + "\n\n")
            if traps: f.write(f"## Armadilhas (Traps) ({len(traps)})\n" + "\n".join(traps) + "\n\n")
            if effect_monsters: f.write(f"## Monstros de Efeito ({len(effect_monsters)})\n" + "\n".join(effect_monsters) + "\n\n")
                
        print(f"\n[SUCESSO] Checklist Brutal gerado com sucesso em:\n{output_path}")
        print(f"Total de Cartas listadas: {total_cards}")
        print(f"Scripts identificados: {scripts_encontrados}")
    except Exception as e:
        print(f"Erro ao salvar arquivo Markdown: {e}")

if __name__ == '__main__':
    generate_qa_checklist()