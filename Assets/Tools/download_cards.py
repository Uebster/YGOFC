from flask import Flask, render_template_string, request, jsonify
import requests
import os
import threading
import time

app = Flask(__name__)

# Controle de progresso global
progresso = {"atual": 0, "total": 0, "status": "Pronto", "card": "", "log": []}

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>Yu-Gi-Oh! Deck Extractor Pro</title>
    <style>
        body { 
            background: #0d0d0d; color: #d4af37; font-family: 'Segoe UI', sans-serif;
            display: flex; flex-direction: column; align-items: center; padding: 40px;
        }
        .panel { 
            background: #1a1a1a; border: 3px solid #d4af37; padding: 25px; 
            border-radius: 10px; width: 500px; box-shadow: 0 0 30px rgba(212, 175, 55, 0.2);
        }
        h1 { text-align: center; text-transform: uppercase; letter-spacing: 3px; margin-bottom: 20px; }
        label { display: block; margin-top: 15px; font-size: 0.9em; color: #aaa; }
        input, select { 
            width: 100%; padding: 10px; margin-top: 5px; background: #222; 
            border: 1px solid #444; color: #fff; border-radius: 4px; box-sizing: border-box;
        }
        .btn-group { display: flex; gap: 10px; margin-top: 25px; }
        button { 
            flex: 1; padding: 15px; background: #d4af37; 
            color: #000; border: none; font-weight: bold; cursor: pointer; text-transform: uppercase;
        }
        button.secondary { background: #3498db; color: #fff; }
        button:hover { opacity: 0.8; }
        .progress-container { background: #000; height: 20px; border-radius: 10px; margin-top: 25px; border: 1px solid #d4af37; overflow: hidden; }
        .progress-bar { width: 0%; height: 100%; background: #d4af37; transition: 0.2s; }
        #log { 
            margin-top: 20px; height: 150px; background: #000; border: 1px solid #333; 
            padding: 10px; font-size: 0.75em; overflow-y: scroll; color: #0f0; font-family: monospace;
        }
    </style>
</head>
<body>
    <div class="panel">
        <h1>Duel Database</h1>

        <label>Nome da Pasta</label>
        <input type="text" id="folder" placeholder="Ex: Downloads_YGO">

        <label>Data Inicial</label>
        <input type="date" id="start" value="1999-01-01">

        <label>Data Final</label>
        <input type="date" id="end" value="2008-12-31">

        <label>Região</label>
        <select id="region">
            <option value="ocg">OCG (Japão)</option>
            <option value="tcg">TCG (Ocidente)</option>
        </select>

        <div class="btn-group">
            <button class="secondary" onclick="startTask('txt')">Gerar Lista TXT</button>
            <button onclick="startTask('img')">Baixar Imagens</button>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <p id="info" style="text-align:center; font-size:0.8em; margin-top:10px;">Aguardando...</p>
        <div id="log"></div>
    </div>

    <script>
        let interval;
        function startTask(type) {
            const data = {
                type: type,
                folder: document.getElementById('folder').value || "Downloads_YGO",
                start: document.getElementById('start').value,
                end: document.getElementById('end').value,
                region: document.getElementById('region').value
            };
            fetch('/start', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) });
            if(interval) clearInterval(interval);
            interval = setInterval(update, 500);
        }

        function update() {
            fetch('/status').then(r => r.json()).then(d => {
                const perc = (d.atual / d.total) * 100 || 0;
                document.getElementById('bar').style.width = perc + '%';
                document.getElementById('info').innerText = d.card ? `Processando: ${d.card}` : d.status;
                if (d.log.length > 0) {
                    const logDiv = document.getElementById('log');
                    logDiv.innerHTML = d.log.map(line => `<div>${line}</div>`).join('');
                    logDiv.scrollTop = logDiv.scrollHeight;
                }
                if(d.status === "Finalizado!") clearInterval(interval);
            });
        }
    </script>
</body>
</html>
"""

def task_executor(tipo, folder, start, end, region):
    global progresso
    progresso = {"atual": 0, "total": 0, "status": "Iniciando...", "card": "", "log": []}
    
    if not os.path.exists(folder): os.makedirs(folder)
    headers = {'User-Agent': 'Mozilla/5.0'}

    url = f"https://db.ygoprodeck.com/api/v7/cardinfo.php?startdate={start}&enddate={end}&dateregion={region}"

    try:
        resp = requests.get(url, headers=headers).json()
        cards = resp.get('data', [])
        cards.sort(key=lambda x: x['name'])
        total = len(cards)
        progresso["total"] = total

        if tipo == 'txt':
            progresso["status"] = "Gerando TXT..."
            path_txt = os.path.join(folder, "lista_cartas.txt")
            with open(path_txt, "w", encoding="utf-8") as f:
                f.write("Nº\tNOME\tTIPO\tRAÇA\tLV\tATK\tDEF\tID\tDESC\n")
                for i, c in enumerate(cards, 1):
                    num = f"{i:04d}"
                    name = c.get('name', 'Unknown')
                    tipo_api = c.get('type', '')
                    cid = c.get('id', '')
                    # Limpa a descrição para uma linha só
                    desc = c.get('desc', '').replace('\n', ' ').replace('\r', ' ')

                    if "Monster" in tipo_api:
                        # Identifica subtipo (Normal ou Efeito)
                        sub_tipo = "Normal" if "Normal" in tipo_api else "Effect"
                        raca = c.get('race', '-')
                        lv = c.get('level', '-')
                        atk = c.get('atk', '-')
                        dfe = c.get('def', '-')
                        # Formato: 0001 | Nome | Monster (Normal/Effect) | Raça | LV | ATK | DEF | ID | Desc
                        linha = f"{num}\t{name}\tMonster ({sub_tipo})\t{raca}\t{lv}\t{atk}\t{dfe}\t{cid}\t{desc}\n"
                    
                    elif "Spell" in tipo_api:
                        # Formato original para Magic: 0001 | Nome | Magic | ID | Desc
                        linha = f"{num}\t{name}\tMagic\t{cid}\t{desc}\n"
                    
                    elif "Trap" in tipo_api:
                        # Formato original para Trap: 0001 | Nome | Trap | ID | Desc
                        linha = f"{num}\t{name}\tTrap\t{cid}\t{desc}\n"
                    
                    else:
                        linha = f"{num}\t{name}\t{tipo_api}\t{cid}\t{desc}\n"
                    
                    f.write(linha)
                    progresso["atual"] = i
                    progresso["card"] = name
            
            progresso["log"].append(f"✓ Lista TXT gerada!")

        else: # Tipo Imagem
            progresso["status"] = "Baixando Imagens..."
            for i, c in enumerate(cards, 1):
                name_raw = c['name']
                clean_name = "".join([char for char in name_raw if char not in r'<>:\"/\\|?*'])
                img_url = c['card_images'][0]['image_url']
                progresso["atual"] = i
                progresso["card"] = name_raw

                path = os.path.join(folder, f"{i:04d} - {clean_name}.jpg")
                if not os.path.exists(path):
                    img_data = requests.get(img_url, headers=headers).content
                    with open(path, 'wb') as f: f.write(img_data)
                
                progresso["log"].append(f"✓ {i:04d}")
                if len(progresso["log"]) > 15: progresso["log"].pop(0)
                time.sleep(0.01)

        progresso["status"] = "Finalizado!"
    except Exception as e:
        progresso["status"] = f"Erro: {str(e)}"

@app.route('/')
def home(): return render_template_string(HTML_UI)

@app.route('/start', methods=['POST'])
def start():
    d = request.json
    threading.Thread(target=task_executor, args=(d['type'], d['folder'], d['start'], d['end'], d['region'])).start()
    return jsonify({"ok": True})

@app.route('/status')
def status(): return jsonify(progresso)

if __name__ == '__main__':
    app.run(debug=True)