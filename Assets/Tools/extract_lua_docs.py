import os
import re
import tkinter as tk
from tkinter import filedialog, messagebox

def extract_functions_from_lua(filepath):
    functions = []
    # Regex para capturar: function NomeDaClasse.NomeDaFuncao(param1, param2)
    pattern = re.compile(r'(?:local\s+)?function\s+([a-zA-Z0-9_.:]+)\s*\((.*?)\)')
    
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        for line in f:
            match = pattern.search(line)
            if match:
                func_name = match.group(1)
                params = match.group(2)
                functions.append((func_name, params))
    return functions

def generate_markdown(input_folder, format_type):
    output_lines = ["# 📖 Documentação Automática: OCGCore Lua Library\n\n"]
    
    for filename in os.listdir(input_folder):
        if not filename.endswith(".lua"):
            continue
            
        filepath = os.path.join(input_folder, filename)
        functions = extract_functions_from_lua(filepath)
        
        if not functions:
            continue
            
        output_lines.append(f"## 📄 Arquivo: `{filename}`\n")
        
        for idx, (func_name, params) in enumerate(functions, 1):
            param_list = [p.strip() for p in params.split(',')] if params.strip() else ["(Sem parâmetros)"]
            param_str = ", ".join(param_list)
            
            if format_type == "checkbox":
                output_lines.append(f"- [ ] **`{func_name}`** (`{param_str}`)\n")
            elif format_type == "numbered":
                output_lines.append(f"{idx}. **`{func_name}`** (`{param_str}`)\n")
            else:
                output_lines.append(f"🔹 **`{func_name}`**\n   * Parâmetros: `{param_str}`\n")
                
        output_lines.append("\n---\n")

    return "".join(output_lines)

def main():
    root = tk.Tk()
    root.withdraw() # Esconde a janela principal

    messagebox.showinfo("LUA Extractor", "Selecione a pasta onde estão os seus arquivos .lua (Ex: SupportLua)")
    input_folder = filedialog.askdirectory(title="Selecione a pasta de origem (.lua)")
    
    if not input_folder:
        return

    # Pergunta qual formato o usuário deseja
    format_choice = messagebox.askquestion("Formato de Saída", "Deseja gerar a lista com Checkboxes (- [ ])?\n\n(Escolher 'Não' gerará uma lista Numerada).")
    fmt = "checkbox" if format_choice == 'yes' else "numbered"

    output_file = os.path.join(input_folder, f"LUA_Documentation_{fmt}.md")
    
    docs = generate_markdown(input_folder, fmt)
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(docs)
        
    messagebox.showinfo("Sucesso!", f"Documentação gerada com sucesso!\nSalva em:\n{output_file}")

if __name__ == "__main__":
    main()
