import os

def refactor_lua_api():
    print("=== Refatorador Automático: LuaAPI ===")
    script_dir = os.path.dirname(os.path.abspath(__file__))
    lua_api_path = os.path.join(script_dir, "..", "Scripts", "LuaAPI.cs")
    output_dir = os.path.join(script_dir, "..", "Scripts", "LuaAPI")

    if not os.path.exists(lua_api_path):
        print(f"[ERRO] Arquivo não encontrado: {lua_api_path}")
        return

    with open(lua_api_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    os.makedirs(output_dir, exist_ok=True)
    current_file = None
    buffers = {}
    
    using_header = (
        "using UnityEngine;\n"
        "using MoonSharp.Interpreter;\n"
        "using System.Collections;\n"
        "using System.Collections.Generic;\n"
        "using System;\n\n"
    )

    # Lê linha a linha e direciona para o arquivo correto
    for line in lines:
        if "// 1. CLASSE DUEL" in line:
            current_file = "LuaDuel.cs"
            buffers[current_file] = [using_header, "// ==============================================================================\n", line]
            continue
        elif "// 2. CLASSE CARD" in line:
            current_file = "LuaCard.cs"
            buffers[current_file] = [using_header, "// ==============================================================================\n", line]
            continue
        elif "// 3. CLASSE EFFECT" in line:
            current_file = "LuaEffect.cs"
            buffers[current_file] = [using_header, "// ==============================================================================\n", line]
            continue
        elif "// 4. CLASSE GROUP" in line:
            current_file = "LuaGroup.cs"
            buffers[current_file] = [using_header, "// ==============================================================================\n", line]
            continue
        elif "// 5. CLASSES DE PROCEDIMENTO" in line:
            current_file = "LuaProcsAndStubs.cs"
            buffers[current_file] = [using_header, "// ==============================================================================\n", line]
            continue
        
        if current_file and not line.startswith("using "):
            if current_file in buffers:
                buffers[current_file].append(line)

    for filename, content_lines in buffers.items():
        out_path = os.path.join(output_dir, filename)
        with open(out_path, "w", encoding="utf-8") as f:
            f.writelines(content_lines)
        print(f"Gerado: {filename}")

    # Renomeia o monstro original para backup de segurança
    backup_path = lua_api_path + ".backup"
    if os.path.exists(backup_path): os.remove(backup_path)
    os.rename(lua_api_path, backup_path)
    
    print("\n[SUCESSO] LuaAPI.cs separada em 5 arquivos na pasta Scripts/LuaAPI/!")

if __name__ == '__main__':
    refactor_lua_api()