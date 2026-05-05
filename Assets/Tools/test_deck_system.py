from flask import Flask, render_template_string, request, jsonify, send_file
import json
import os
import tkinter as tk
from tkinter import filedialog

app = Flask(__name__)

global_chars = []
global_cards = {}
global_img_dir = ""
image_map = {}

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>Yu-Gi-Oh! Deck System Viewer</title>
    <style>
        :root {
            --cyan: #00e5ff;
            --purple: #b537f2;
            --gold: #d4af37;
            --green: #39ff14;
            --red: #ff073a;
            --bg-dark: #080808;
            --panel-bg: #111111;
        }
        body { 
            background: var(--bg-dark); color: #fff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex; justify-content: center; align-items: flex-start; min-height: 100vh; margin: 0; padding: 20px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--gold); padding: 30px; 
            border-radius: 20px; width: 1400px; box-shadow: 0 0 40px rgba(212, 175, 55, 0.15);
            display: flex; flex-direction: column; height: 95vh; box-sizing: border-box;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--gold); text-shadow: 0 0 10px var(--gold), 0 0 20px rgba(212, 175, 55, 0.5);
            flex-shrink: 0;
        }
        
        .config-bar { display: flex; flex-direction: column; gap: 10px; margin-bottom: 20px; flex-shrink: 0;}
        .input-group { display: flex; gap: 10px; align-items: stretch; }
        
        input[type="text"] { 
            flex-grow: 1; padding: 10px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 8px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input[type="text"]:focus { outline: none; border-color: var(--cyan); box-shadow: 0 0 10px rgba(0, 229, 255, 0.3); }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 8px; padding: 10px 20px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; color: var(--cyan); border-color: var(--cyan); 
            box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan);
        }
        .action-btn:hover { background: var(--cyan); color: #000 !important; text-shadow: none; box-shadow: inset 0 0 20px var(--cyan); }
        
        .gold-btn { color: var(--gold); border-color: var(--gold); box-shadow: inset 0 0 8px rgba(212,175,55,0.3); text-shadow: 0 0 5px var(--gold); }
        .gold-btn:hover { background: var(--gold); box-shadow: inset 0 0 20px var(--gold); color: #000 !important; }

        .viewer-layout { display: flex; gap: 30px; flex-grow: 1; min-height: 0; }
        .left-col { flex: 0 0 320px; display: flex; flex-direction: column; gap: 15px; }
        
        .avatar-placeholder {
            width: 100%; height: 100px; background: #050505; border: 2px dashed #444; border-radius: 12px;
            display: flex; justify-content: center; align-items: center; color: #666; font-weight: bold; font-style: italic; flex-shrink: 0;
        }

        .char-top-bar {
            display: flex; justify-content: space-between; align-items: center; 
            background: #050505; padding: 15px 20px; border-radius: 12px; border: 1px solid #333; flex-shrink: 0;
            box-shadow: inset 0 0 20px rgba(0,0,0,0.5);
        }
        .char-info { display: flex; flex-direction: column; }
        .char-name { color: var(--gold); margin: 0; font-size: 1.8em; text-shadow: 0 0 10px rgba(212,175,55,0.4); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;}
        .char-details { color: #aaa; font-size: 0.85em; margin-top: 5px; }

        .card-image-container {
            width: 100%; padding-top: 145%; position: relative; border-radius: 12px; border: 2px solid var(--purple);
            box-shadow: 0 0 20px rgba(181, 55, 242, 0.2); overflow: hidden; background: #000; cursor: pointer; flex-shrink: 0;
        }
        .card-image-container img {
            position: absolute; top: 0; left: 0; width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s;
        }
        .alt-hint { position: absolute; bottom: 10px; right: 10px; background: rgba(0,0,0,0.8); color: var(--gold); padding: 4px 8px; border-radius: 5px; font-size: 0.8em; font-weight: bold; pointer-events: none;}
        
        .desc-box {
            background: rgba(0, 229, 255, 0.05); padding: 15px; border-left: 4px solid var(--cyan);
            border-radius: 0 8px 8px 0; font-size: 0.85em; color: #eee; line-height: 1.4; 
            flex-grow: 1; display: flex; flex-direction: column; min-height: 0;
        }
        .desc-title { color: var(--cyan); font-weight: bold; margin-bottom: 5px; font-size: 1.1em; flex-shrink: 0;}
        .desc-content { overflow-y: auto; flex-grow: 1; padding-right: 5px; }

        .right-col { flex-grow: 1; display: flex; flex-direction: column; min-width: 0; gap: 15px; }
        
        .nav-controls {
            display: flex; align-items: center; gap: 10px;
        }
        
        .deck-header {
            display: flex; justify-content: space-between; align-items: center;
            border-bottom: 1px solid #333; padding-bottom: 10px; flex-shrink: 0;
        }
        .deck-header h3 { margin: 0; color: #fff; }
        
        .deck-tabs { display: flex; gap: 15px; justify-content: center; flex: 1; margin: 0 30px; }
        .tab-btn { padding: 8px 15px; font-size: 0.9em; opacity: 0.5; }
        .tab-btn.active { opacity: 1; box-shadow: inset 0 0 20px rgba(212,175,55,0.4); border-color: var(--gold); color: var(--gold);}

        .deck-scroll-area {
            flex-grow: 1; overflow-y: auto; background: #050505; border: 1px solid #222; border-radius: 8px; padding: 15px;
        }

        .deck-grid {
            display: grid; grid-template-columns: repeat(10, 1fr); gap: 5px; margin-bottom: 20px;
        }

        .card-slot {
            aspect-ratio: 813 / 1185; background: #111; border: 1px solid #333; border-radius: 4px; overflow: hidden;
            cursor: pointer; position: relative; transition: border-color 0.2s;
        }
        .card-slot img { width: 100%; height: 100%; object-fit: cover; }
        .card-slot:hover { border-color: var(--cyan); box-shadow: 0 0 10px var(--cyan); z-index: 10; }

        ::-webkit-scrollbar { width: 8px; height: 8px;}
        ::-webkit-scrollbar-track { background: #111; border-radius: 4px; }
        ::-webkit-scrollbar-thumb { background: #444; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: var(--purple); }
    </style>
</head>
<body>
    <div class="panel-container">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; flex-shrink: 0;" id="header_container">
            <h1 class="neon-title" id="main_title" style="margin: 0;">Character Deck System Viewer</h1>
            <button id="btn_toggle_config" class="action-btn gold-btn" style="display: none; padding: 6px 12px; font-size: 0.8em;" onclick="toggleConfig()">▼ Mostrar Config</button>
        </div>
        
        <div class="config-bar" id="config_bar">
            <div class="input-group">
                <input type="text" id="json_path" placeholder="1. Caminho do characters.json (Ex: C:/YuGiOh/Assets/StreamingAssets/characters.json)">
                <button class="action-btn" style="flex: 0 0 auto;" onclick="selectPath('file', 'json_path')">Procurar</button>
            </div>
            <div class="input-group">
                <input type="text" id="cards_path" placeholder="2. Caminho do cards.json (Opcional - Para ler nomes e descrições)">
                <button class="action-btn" style="flex: 0 0 auto;" onclick="selectPath('file', 'cards_path')">Procurar</button>
            </div>
            <div class="input-group">
                <input type="text" id="img_path" placeholder="3. Pasta de Imagens Brutas das Cartas (Onde estão os .jpg)">
                <button class="action-btn" style="flex: 0 0 auto;" onclick="selectPath('folder', 'img_path')">Procurar</button>
                <button class="action-btn gold-btn" style="flex: 0 0 200px;" onclick="loadDB()">🚀 CARREGAR SISTEMA</button>
            </div>
        </div>

        <div id="viewer_section" class="viewer-layout" style="display: none;">
            <div class="left-col">
                <div class="avatar-placeholder">[Espaço Futuro para Avatar do Oponente]</div>
                
                <div class="card-image-container" onclick="cycleImage()">
                    <img id="card_image" src="" alt="Imagem da Carta">
                    <div id="alt_hint" class="alt-hint" style="display: none;">CLICK: ALT ART</div>
                </div>
                
                <div class="desc-box">
                    <div class="desc-title" id="sig_name">Carta Assinatura / Hover</div>
                    <div id="sig_desc" class="desc-content">Passe o mouse nas cartas do deck para ler os detalhes.</div>
                </div>
            </div>

            <div class="right-col">
                <div class="char-top-bar">
                    <div class="char-info">
                        <h2 id="c_name" class="char-name">Nome do Personagem</h2>
                        <div id="c_id" class="char-details">ID: - | Dificuldade: - | Role: - | Field: -</div>
                    </div>
                    <div class="nav-controls">
                        <button class="action-btn" style="padding: 8px 15px;" onclick="prevChar()">&lt; Ant</button>
                        <span id="char_counter" style="color: #aaa; font-size: 0.9em; font-weight: bold;">0 / 0</span>
                        <button class="action-btn" style="padding: 8px 15px;" onclick="nextChar()">Próx &gt;</button>
                        <input type="text" id="search_input" placeholder="🔍 Buscar ID ou Nome... (Enter)" onkeypress="handleSearch(event)" style="width: 250px; margin-left: 10px;">
                    </div>
                </div>

                <div class="deck-header">
                    <h3>Estrutura de Decks</h3>
                    <div class="deck-tabs">
                        <button id="btn_deckA" class="action-btn tab-btn active" onclick="selectDeckVersion('A')">Deck A (Fácil)</button>
                        <button id="btn_deckB" class="action-btn tab-btn" onclick="selectDeckVersion('B')">Deck B (Médio)</button>
                        <button id="btn_deckC" class="action-btn tab-btn" onclick="selectDeckVersion('C')">Deck C (Difícil)</button>
                    </div>
                    <div style="display: flex; gap: 10px;">
                        <button id="btn_edit" class="action-btn purple-btn" style="padding: 6px 12px; font-size: 0.8em;" onclick="toggleEdit()">✏️ EDITAR DECK</button>
                        <button class="action-btn red-btn" style="padding: 6px 12px; font-size: 0.8em;" onclick="saveToServer()">💾 SALVAR JSON</button>
                    </div>
                </div>

                <div class="deck-scroll-area">
                    <textarea id="deck_editor" style="display: none; height: 95%; font-size: 1.1em;"></textarea>
                    <div id="deck_visuals">
                        <h4 style="margin-top:0; color:#ccc;">Main Deck (<span id="main_count">0</span>)</h4>
                        <div class="deck-grid" id="main_deck_grid"></div>
                        
                        <h4 style="color:#ccc;">Extra Deck (<span id="extra_count">0</span>)</h4>
                        <div class="deck-grid" id="extra_deck_grid"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let currentIndex = 0;
        let totalChars = 0;
        let currentChar = null;
        let currentDeckVersion = 'A';
        
        let currentImages = [];
        let currentImageIndex = 0;
        let hoverTimeout;

        function scheduleHover(cardId) {
            clearTimeout(hoverTimeout);
            hoverTimeout = setTimeout(() => hoverCard(cardId), 250); // Atraso tático de 250ms
        }

        function clearHover() {
            clearTimeout(hoverTimeout);
        }

        function selectPath(type, inputId) {
            const route = type === 'file' ? '/select_file' : '/select_folder';
            fetch(route).then(r => r.json()).then(d => {
                if(d.path) document.getElementById(inputId).value = d.path;
            });
        }

        function loadDB() {
            const data = {
                char_path: document.getElementById('json_path').value,
                cards_path: document.getElementById('cards_path').value,
                img_path: document.getElementById('img_path').value
            };
            fetch('/load', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) })
            .then(r => r.json()).then(d => {
                if (d.error) alert(d.error);
                else {
                    totalChars = d.count;
                    isConfigVisible = false;
                    document.getElementById('config_bar').style.display = 'none';
                    document.getElementById('btn_toggle_config').style.display = 'block';
                    document.getElementById('btn_toggle_config').innerText = '▼ Mostrar Config';
                    
                    document.getElementById('viewer_section').style.display = 'flex';
                    loadChar(0);
                }
            });
        }

        function loadChar(index) {
            if (totalChars === 0) return;
            if (index < 0) index = totalChars - 1;
            if (index >= totalChars) index = 0;
            currentIndex = index;
            
            document.getElementById('char_counter').innerText = `${currentIndex + 1} / ${totalChars}`;
            
            fetch(`/api/char/${index}`).then(r => r.json()).then(char => {
                currentChar = char;
                document.getElementById('c_name').innerText = char.name || 'Unknown';
                document.getElementById('c_id').innerText = `ID: ${char.id} | Dificuldade: ${char.difficulty} | Role: ${char.story_role} | Field: ${char.field}`;
                
                if(char.signature_card) hoverCard(char.signature_card);
                renderDeck();
            });
        }

        function selectDeckVersion(ver) {
            currentDeckVersion = ver;
            document.getElementById('btn_deckA').classList.remove('active');
            document.getElementById('btn_deckB').classList.remove('active');
            document.getElementById('btn_deckC').classList.remove('active');
            document.getElementById(`btn_deck${ver}`).classList.add('active');
            renderDeck();
        }

        function renderDeck() {
            if(!currentChar) return;
            const mainKey = `deck_${currentDeckVersion}`;
            const extraKey = `extra_deck_${currentDeckVersion}`;
            
            const mainDeck = currentChar[mainKey] || [];
            const extraDeck = currentChar[extraKey] || [];
            
            document.getElementById('main_count').innerText = mainDeck.length;
            document.getElementById('extra_count').innerText = extraDeck.length;
            
            const mainGrid = document.getElementById('main_deck_grid');
            mainGrid.innerHTML = '';
            mainDeck.forEach(cardId => {
                mainGrid.innerHTML += `<div class="card-slot" onmouseenter="scheduleHover('${cardId}')" onmouseleave="clearHover()"><img src="/api/image_raw?id=${cardId}" loading="lazy"></div>`;
            });
            
            const extraGrid = document.getElementById('extra_deck_grid');
            extraGrid.innerHTML = '';
            extraDeck.forEach(cardId => {
                extraGrid.innerHTML += `<div class="card-slot" onmouseenter="scheduleHover('${cardId}')" onmouseleave="clearHover()"><img src="/api/image_raw?id=${cardId}" loading="lazy"></div>`;
            });
        }

        function hoverCard(cardId) {
            if (!cardId) return;
            fetch(`/api/images/${cardId}`).then(r => r.json()).then(data => {
                currentImages = data.images;
                currentImageIndex = 0;
                updateImageDisplay();
            });
            fetch(`/api/card_info/${cardId}`).then(r => r.json()).then(card => {
                if(!card.error) {
                    document.getElementById('sig_name').innerText = card.name;
                    document.getElementById('sig_desc').innerText = card.description;
                } else {
                    document.getElementById('sig_name').innerText = cardId;
                    document.getElementById('sig_desc').innerText = "[Nenhuma informação encontrada no cards.json]";
                }
            });
        }
        
        function updateImageDisplay() {
            const imgEl = document.getElementById('card_image');
            const hintEl = document.getElementById('alt_hint');
            if (currentImages.length > 0) {
                imgEl.src = `/api/image_raw?path=${encodeURIComponent(currentImages[currentImageIndex])}`;
                hintEl.style.display = currentImages.length > 1 ? 'block' : 'none';
                if (currentImages.length > 1) hintEl.innerText = `ALT ART (${currentImageIndex + 1}/${currentImages.length}) CLICK`;
            } else {
                imgEl.src = "";
                hintEl.style.display = 'none';
            }
        }
        
        function cycleImage() {
            if (currentImages.length > 1) {
                currentImageIndex = (currentImageIndex + 1) % currentImages.length;
                updateImageDisplay();
            }
        }

        function nextChar() { loadChar(currentIndex + 1); }
        function prevChar() { loadChar(currentIndex - 1); }

        let isEditing = false;
        function toggleEdit() {
            if(!currentChar) return;
            const editor = document.getElementById('deck_editor');
            const visuals = document.getElementById('deck_visuals');
            const btn = document.getElementById('btn_edit');
            
            if(!isEditing) {
                const editableData = {
                    deck_A: currentChar.deck_A || [], extra_deck_A: currentChar.extra_deck_A || [],
                    deck_B: currentChar.deck_B || [], extra_deck_B: currentChar.extra_deck_B || [],
                    deck_C: currentChar.deck_C || [], extra_deck_C: currentChar.extra_deck_C || []
                };
                editor.value = JSON.stringify(editableData, null, 2);
                visuals.style.display = 'none';
                editor.style.display = 'block';
                btn.innerText = "✔️ APLICAR ALTERAÇÃO";
                btn.classList.replace('purple-btn', 'green-btn');
            } else {
                try {
                    const parsed = JSON.parse(editor.value);
                    currentChar.deck_A = parsed.deck_A || []; currentChar.extra_deck_A = parsed.extra_deck_A || [];
                    currentChar.deck_B = parsed.deck_B || []; currentChar.extra_deck_B = parsed.extra_deck_B || [];
                    currentChar.deck_C = parsed.deck_C || []; currentChar.extra_deck_C = parsed.extra_deck_C || [];
                    
                    fetch('/update_char', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ index: currentIndex, data: currentChar }) });
                    
                    visuals.style.display = 'block';
                    editor.style.display = 'none';
                    btn.innerText = "✏️ EDITAR DECK";
                    btn.classList.replace('green-btn', 'purple-btn');
                    renderDeck();
                } catch (e) {
                    alert("Erro no formato JSON! Corrija a sintaxe e tente aplicar novamente.\\nDetalhe: " + e.message);
                    return;
                }
            }
            isEditing = !isEditing;
        }

        function saveToServer() {
            const charPath = document.getElementById('json_path').value;
            fetch('/save_db', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ path: charPath }) })
            .then(r => r.json()).then(d => { if(d.ok) alert(d.message); else alert("Erro ao salvar: " + d.error); });
        }
        
        function handleSearch(e) {
            if (e.key === 'Enter') {
                const q = document.getElementById('search_input').value;
                if (!q) return;
                fetch(`/api/search?q=${encodeURIComponent(q)}`).then(r => r.json()).then(d => {
                    if(d.index !== undefined) loadChar(d.index);
                    else alert("Personagem não encontrado.");
                });
            }
        }

        let isConfigVisible = true;
        function toggleConfig() {
            const configBar = document.getElementById('config_bar');
            const btnToggle = document.getElementById('btn_toggle_config');
            isConfigVisible = !isConfigVisible;
            if(isConfigVisible) {
                configBar.style.display = 'flex';
                btnToggle.innerText = '▲ Ocultar Config';
            } else {
                configBar.style.display = 'none';
                btnToggle.innerText = '▼ Mostrar Config';
            }
        }
    </script>
</body>
</html>
"""

@app.route('/')
def home():
    return render_template_string(HTML_UI)

@app.route('/select_file')
def select_file():
    root = tk.Tk()
    root.attributes("-topmost", True)
    root.withdraw()
    file_path = filedialog.askopenfilename(title="Selecione o arquivo JSON", filetypes=[("JSON Files", "*.json")])
    root.destroy()
    return jsonify({"path": file_path})

@app.route('/select_folder')
def select_folder():
    root = tk.Tk()
    root.attributes("-topmost", True)
    root.withdraw()
    folder = filedialog.askdirectory(title="Selecione a pasta")
    root.destroy()
    return jsonify({"path": folder})

@app.route('/load', methods=['POST'])
def load():
    global global_chars, global_cards, global_img_dir, image_map
    data = request.json
    char_path = data.get('char_path', '')
    cards_path = data.get('cards_path', '')
    global_img_dir = data.get('img_path', '')
    
    if not os.path.exists(char_path):
        return jsonify({"error": "Arquivo characters.json não encontrado!"})
        
    try:
        with open(char_path, 'r', encoding='utf-8') as f:
            global_chars = json.load(f)
            
        if os.path.exists(cards_path):
            with open(cards_path, 'r', encoding='utf-8') as f:
                cards_list = json.load(f)
                global_cards = {c.get('id', ''): c for c in cards_list}
        else:
            global_cards = {}
            
        # Varredura inteligente de imagens
        image_map = {}
        if os.path.exists(global_img_dir):
            for root, _, files in os.walk(global_img_dir):
                for f in files:
                    if f.endswith('.jpg') or f.endswith('.png'):
                        parts = f.split(' - ')
                        if len(parts) >= 1:
                            cid_part = parts[0]
                            cid = cid_part.split('_')[0] # Limpa sufixos de Alt Art
                            if cid not in image_map:
                                image_map[cid] = []
                            image_map[cid].append(os.path.join(root, f))
            
            # Ordena para a Original vir primeiro
            for cid in image_map:
                image_map[cid].sort(key=lambda x: 1 if "_Alt" in x else 0)
                
        return jsonify({"count": len(global_chars)})
    except Exception as e:
        return jsonify({"error": str(e)})

@app.route('/api/char/<int:index>')
def get_char(index):
    if 0 <= index < len(global_chars):
        return jsonify(global_chars[index])
    return jsonify({"error": "Index out of bounds"}), 404

@app.route('/api/search')
def search_char():
    query = request.args.get('q', '').lower()
    for i, c in enumerate(global_chars):
        if query in str(c.get('id', '')).lower() or query in c.get('name', '').lower():
            return jsonify({"index": i})
    return jsonify({"error": "Not found"}), 404

@app.route('/api/card_info/<card_id>')
def get_card_info(card_id):
    card = global_cards.get(card_id)
    if card: return jsonify(card)
    return jsonify({"error": "Not found"}), 404

@app.route('/api/images/<card_id>')
def get_images(card_id):
    if card_id in image_map and len(image_map[card_id]) > 0:
        return jsonify({"images": image_map[card_id]})
    return jsonify({"images": []})

@app.route('/api/image_raw')
def image_raw():
    path = request.args.get('path')
    card_id = request.args.get('id')
    
    if path and os.path.exists(path):
        return send_file(path)
    elif card_id and card_id in image_map and len(image_map[card_id]) > 0:
        return send_file(image_map[card_id][0])
        
    return "Not found", 404

@app.route('/update_char', methods=['POST'])
def update_char():
    data = request.json
    idx = data.get('index')
    if idx is not None and 0 <= idx < len(global_chars):
        global_chars[idx] = data['data']
        return jsonify({"ok": True})
    return jsonify({"ok": False})

@app.route('/save_db', methods=['POST'])
def save_db():
    path = request.json.get('path')
    if not path or not os.path.exists(path):
        return jsonify({"ok": False, "error": "Caminho do arquivo characters.json é inválido ou arquivo não foi carregado."})
    try:
        import shutil, datetime
        bak_path = f"{path}.bak_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}"
        shutil.copy2(path, bak_path)
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(global_chars, f, indent=2)
        return jsonify({"ok": True, "message": f"Sucesso!\nArquivo salvo em:\n{path}\n\nBackup de segurança criado:\n{os.path.basename(bak_path)}"})
    except Exception as e:
        return jsonify({"ok": False, "error": str(e)})

if __name__ == "__main__":
    # Usa a porta 5002 para não colidir com o Extractor ou Card Viewer
    app.run(debug=True, port=5002)
