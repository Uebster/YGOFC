// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;

public enum CardLocation { Hand, Deck, Field, ExtraDeck, Graveyard, Banished, Unknown }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public enum TargetType { Monster, Spell, Trap, Any }

    // Dicionário de zonas físicas bloqueadas (ex: Ojama King)
    public Dictionary<CardDisplay, List<Transform>> blockedZonesByCard = new Dictionary<CardDisplay, List<Transform>>();

    public Script luaEngine;
    public LuaDuel luaDuel;

    // Variáveis de controle de Assincronicidade do Lua
    public DynValue activeLuaCoroutine = null;
    public bool isWaitingForLuaYield = false;
    public DynValue yieldReturnValue = null;

    // Flags de estado global mantidas para compatibilidade com outros Managers
    public bool armoredGlassActive = false;
    public bool reverseStats = false;
    public bool invertDecks = false;
    public bool cannotSummonMonstersThisTurn = false;
    public bool trapsBlockedThisTurn = false;

    public Dictionary<CardDisplay, LuaCard> activeLuaCards = new Dictionary<CardDisplay, LuaCard>();
    
    public class PendingEffect
    {
        public LuaEffect effect;
        public object triggerArgs;
    }
    public Dictionary<CardDisplay, PendingEffect> pendingResolutions = new Dictionary<CardDisplay, PendingEffect>();

    // Closures seguras para preencher funções em branco e evitar "attempt to call a nil value"
    public Closure dummyClosureTrue;

    void Awake()
    {
        Instance = this;
        InitializeLuaEngine();
    }

    private void InitializeLuaEngine()
    {
        // 1. Instancia o interpretador MoonSharp
        luaEngine = new Script();

        // 2. Registra nossas Classes C# para serem expostas ao Lua
        UserData.RegisterType<LuaDuel>();
        UserData.RegisterType<LuaCard>();
        UserData.RegisterType<LuaEffect>();
        UserData.RegisterType<LuaGroup>();

        // 3. Injeta as instâncias globais na memória do Lua
        luaDuel = new LuaDuel();
        luaEngine.Globals["Duel"] = luaDuel;
        luaEngine.Globals["Effect"] = typeof(LuaEffect); // Permite chamar Effect.CreateEffect(c)
        luaEngine.Globals["Card"] = typeof(LuaCard);
        luaEngine.Globals["Group"] = typeof(LuaGroup);

        // 4. Funções Base OCGCore que os scripts chamam o tempo todo
        luaEngine.Globals["GetID"] = (System.Func<DynValue>)(() => DynValue.NewTuple(luaEngine.Globals.Get("self_table"), luaEngine.Globals.Get("self_code")));
        
        // Tabela Auxiliar Básica
        DynValue auxTable = DynValue.NewTable(luaEngine);
        // Aceitar object para prevenir falhas se um userdata vazar como ID
        auxTable.Table.Set("Stringid", DynValue.FromObject(luaEngine, (System.Func<object, object, string>)((code, id) => $"{code}_{id}")));
        
        // O GlobalCheck garante que efeitos passivos no fundo sejam executados!
        auxTable.Table.Set("GlobalCheck", DynValue.FromObject(luaEngine, (System.Action<object, Closure>)((s, func) => { func?.Call(); })));
        
        // Constantes lógicas (Criadas como closures nativas para evitar erros de casting inter-linguagem)
        auxTable.Table.Set("TRUE", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("FALSE", luaEngine.DoString("return function(...) return false end"));

        // Cacheia a closure verdadeira para uso nos Dummies
        dummyClosureTrue = auxTable.Table.Get("TRUE").Function;

        luaEngine.Globals["aux"] = auxTable;

        // Tabelas de Procedimentos Oficiais (EDOPro Lua Updates)
        string[] procTables = { "Spirit", "Toon", "Union", "Gemini", "Pendulum", "Link", "Xyz", "Synchro", "Fusion", "Ritual" };
        foreach (var pName in procTables)
        {
            luaEngine.Globals[pName] = DynValue.NewTable(luaEngine);
        }

        // 5. Injeção de Constantes Vitais (Evita os erros de Bitwise do Lua 5.2)
        InjectVitalConstants();

        Debug.Log("[MoonSharp] Motor LUA Inicializado e pronto para interpretar OCGCore!");
    }

    private void InjectVitalConstants()
    {
        // Para começar a testar feitiços e armadilhas (Dano, Cura, Destruição),
        // injetamos as constantes diretamente no ambiente Lua.
        luaEngine.Globals["LOCATION_MZONE"] = 0x4;
        luaEngine.Globals["LOCATION_SZONE"] = 0x8;
        luaEngine.Globals["LOCATION_GRAVE"] = 0x10;
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8; // Resolvido no C#!
        luaEngine.Globals["LOCATION_HAND"] = 0x2;
        luaEngine.Globals["LOCATION_DECK"] = 0x1;
        luaEngine.Globals["LOCATION_EXTRA"] = 0x40;
        luaEngine.Globals["LOCATION_REMOVED"] = 0x20;
        
        // Tipos de Cartas (TYPE_)
        luaEngine.Globals["TYPE_MONSTER"] = 0x1;
        luaEngine.Globals["TYPE_SPELL"] = 0x2;
        luaEngine.Globals["TYPE_TRAP"] = 0x4;
        luaEngine.Globals["TYPE_NORMAL"] = 0x10;
        luaEngine.Globals["TYPE_EFFECT"] = 0x20;
        luaEngine.Globals["TYPE_FUSION"] = 0x40;
        luaEngine.Globals["TYPE_RITUAL"] = 0x80;
        luaEngine.Globals["TYPE_TRAPMONSTER"] = 0x100;
        luaEngine.Globals["TYPE_SPIRIT"] = 0x200;
        luaEngine.Globals["TYPE_UNION"] = 0x400;
        luaEngine.Globals["TYPE_GEMINI"] = 0x800;
        luaEngine.Globals["TYPE_TUNER"] = 0x1000;
        luaEngine.Globals["TYPE_SYNCHRO"] = 0x2000;
        luaEngine.Globals["TYPE_TOKEN"] = 0x4000;
        luaEngine.Globals["TYPE_QUICKPLAY"] = 0x10000;
        luaEngine.Globals["TYPE_CONTINUOUS"] = 0x20000;
        luaEngine.Globals["TYPE_EQUIP"] = 0x40000;
        luaEngine.Globals["TYPE_FIELD"] = 0x80000;
        luaEngine.Globals["TYPE_COUNTER"] = 0x100000;
        luaEngine.Globals["TYPE_FLIP"] = 0x200000;
        luaEngine.Globals["TYPE_TOON"] = 0x400000;
        luaEngine.Globals["TYPE_XYZ"] = 0x800000;
        luaEngine.Globals["TYPE_PENDULUM"] = 0x1000000;

        luaEngine.Globals["ATTRIBUTE_DARK"] = 0x10;
        luaEngine.Globals["ATTRIBUTE_LIGHT"] = 0x20;
        luaEngine.Globals["ATTRIBUTE_EARTH"] = 0x01;
        luaEngine.Globals["ATTRIBUTE_WATER"] = 0x02;
        luaEngine.Globals["ATTRIBUTE_FIRE"] = 0x04;
        luaEngine.Globals["ATTRIBUTE_WIND"] = 0x08;
        
        luaEngine.Globals["POS_FACEUP"] = 0x1 | 0x4;
        luaEngine.Globals["POS_FACEUP_ATTACK"] = 0x1;
        luaEngine.Globals["POS_FACEDOWN_DEFENSE"] = 0x8;
        luaEngine.Globals["POS_FACEUP_DEFENSE"] = 0x4;
        luaEngine.Globals["POS_DEFENSE"] = 0x4 | 0x8;
        
        luaEngine.Globals["REASON_EFFECT"] = 0x40;
        luaEngine.Globals["EVENT_FREE_CHAIN"] = 1002;
        luaEngine.Globals["CATEGORY_DAMAGE"] = 0x80000;
        luaEngine.Globals["CATEGORY_RECOVER"] = 0x100000;
        luaEngine.Globals["CATEGORY_DESTROY"] = 0x20000;
        luaEngine.Globals["REASON_COST"] = 0x2;
        luaEngine.Globals["REASON_BATTLE"] = 0x10;
        
        // Gatilhos e Eventos (Events OCGCore)
        luaEngine.Globals["EVENT_SUMMON_SUCCESS"] = 11;
        luaEngine.Globals["EVENT_FLIP_SUMMON_SUCCESS"] = 13;
        luaEngine.Globals["EVENT_SPSUMMON_SUCCESS"] = 14;
        luaEngine.Globals["EVENT_FLIP"] = 1014;
        luaEngine.Globals["EVENT_BATTLE_DESTROYED"] = 1010;
        luaEngine.Globals["EVENT_DESTROYED"] = 1011;
        luaEngine.Globals["EVENT_PHASE"] = 4096;
        luaEngine.Globals["EVENT_PHASE_START"] = 4097;
        luaEngine.Globals["EVENT_CHANGE_POS"] = 1013;
        luaEngine.Globals["EVENT_LEAVE_FIELD"] = 1012;

        // Tipos de Efeitos (EFFECT_TYPE)
        luaEngine.Globals["EFFECT_TYPE_SINGLE"] = 0x0001;
        luaEngine.Globals["EFFECT_TYPE_FIELD"] = 0x0002;
        luaEngine.Globals["EFFECT_TYPE_EQUIP"] = 0x0004;
        luaEngine.Globals["EFFECT_TYPE_ACTIONS"] = 0x0008;
        luaEngine.Globals["EFFECT_TYPE_ACTIVATE"] = 0x0010;
        luaEngine.Globals["EFFECT_TYPE_FLIP"] = 0x0020;
        luaEngine.Globals["EFFECT_TYPE_IGNITION"] = 0x0040;
        luaEngine.Globals["EFFECT_TYPE_TRIGGER_O"] = 0x0080;
        luaEngine.Globals["EFFECT_TYPE_QUICK_O"] = 0x0100;
        luaEngine.Globals["EFFECT_TYPE_TRIGGER_F"] = 0x0200;
        luaEngine.Globals["EFFECT_TYPE_QUICK_F"] = 0x0400;
        luaEngine.Globals["EFFECT_TYPE_CONTINUOUS"] = 0x0800;
        
        // Cadeia e Alvos
        luaEngine.Globals["CHAININFO_TARGET_PLAYER"] = 1;
        luaEngine.Globals["CHAININFO_TARGET_PARAM"] = 2;
        
        luaEngine.Globals["HINT_SELECTMSG"] = 1;
        luaEngine.Globals["HINTMSG_DESTROY"] = 506;
        luaEngine.Globals["HINTMSG_SPSUMMON"] = 509;
        luaEngine.Globals["HINTMSG_POSCHANGE"] = 513;
        luaEngine.Globals["HINTMSG_RTOHAND"] = 510;
        luaEngine.Globals["HINTMSG_TARGET"] = 514;
        luaEngine.Globals["HINTMSG_EQUIP"] = 515;

        // 6. O Segredo da Sobrevivência (Metatable Global)
        // Qualquer constante OCGCore (ALL_CAPS) não definida acima retornará 0 em vez de nil.
        // Isso previne que a VM crashe com "attempt to perform arithmetic on a nil value" (Ex: FLAG_A + FLAG_B).
        // Também blinda as tabelas procedurais (Fusion, Ritual, etc) para retornarem efeitos vazios inofensivos caso a função não exista.
        luaEngine.DoString(@"
            local dummyFunc = function() return Effect.CreateEffect(nil) end
            local procMt = { __index = function() return dummyFunc end, __call = function() return dummyFunc end }
            local dummyTable = {}
            setmetatable(dummyTable, procMt)

            local procTables = { aux, Spirit, Toon, Union, Gemini, Pendulum, Link, Xyz, Synchro, Fusion, Ritual }
            for _, pTable in ipairs(procTables) do
                setmetatable(pTable, procMt)
            end

            setmetatable(_G, {
                __index = function(t, k)
                    if type(k) == 'string' and string.match(k, '^[A-Z0-9_]+$') then
                        return 0
                    end
                    -- Se começar com Letra Maiúscula (Ex: GladiatorBeast, Card, Effect) e for desconhecido, devolve Tabela Fantasma
                    if type(k) == 'string' and string.match(k, '^[A-Z][a-zA-Z0-9_]*$') then
                        return dummyTable
                    end
                    return nil
                end
            })

            -- Suporte nativo para o iterador avançado: 'for tc in group do'
            local dummyGroup = Group.CreateGroup()
            local gm = getmetatable(dummyGroup)
            if gm then
                gm.__call = function(grp, state, var)
                    if var == nil then return grp:GetFirst() end
                    return grp:GetNext()
                end
                gm.__iterator = function(grp)
                    return grp:Iter()
                end
            end
        ");
    }

    public LuaCard EnsureCardScriptLoaded(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return null;
        if (activeLuaCards.ContainsKey(card)) return activeLuaCards[card];
        
        string cardId = card.CurrentCardData.id;
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
        
        if (!System.IO.File.Exists(scriptPath)) return null;

        try
        {
            DynValue selfTable = DynValue.NewTable(luaEngine);
            int numericId = 0;
            
            // Usa a senha oficial da Konami (password) como ID interno do LUA
            // Isso garante que funções oficiais como "GetID()" recebam o número real
            if (!string.IsNullOrEmpty(card.CurrentCardData.password))
                int.TryParse(card.CurrentCardData.password, out numericId);
            
            luaEngine.Globals["self_table"] = selfTable;
            luaEngine.Globals["self_code"] = numericId;

            string scriptCode = System.IO.File.ReadAllText(scriptPath);
            scriptCode = SanitizeOCGScript(scriptCode);
            luaEngine.DoString(scriptCode);

            DynValue initialEffect = selfTable.Table.Get("initial_effect");
            if (!initialEffect.IsNil())
            {
                LuaCard luaCard = new LuaCard(card);
                luaEngine.Call(initialEffect, luaCard);
                activeLuaCards[card] = luaCard;
                return luaCard;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API LUA CRASH] Ocorreu uma falha no carregamento do arquivo c{cardId}.lua:\n{ex.Message}");
        }
        
        return null;
    }

    private string SanitizeOCGScript(string script)
    {
        // A engine do EDOPro atualizou para Lua 5.3, que possui operadores bit a bit nativos (&, |, <<, >>, ~).
        // O MoonSharp roda em Lua 5.2, que não os entende nativamente e usa a biblioteca bit32.
        // Esta função sanitiza o script em tempo de execução para evitar crash da Máquina Virtual.

        // 0. Remove comentários para evitar falsos positivos na tradução de operadores (Ex: -- // comment)
        script = Regex.Replace(script, @"--\[\[.*?\]\]", "", RegexOptions.Singleline);
        script = Regex.Replace(script, @"--.*", "");

        // Padrão Avançado: Captura parênteses balanceados infinitos sem gerar Catastrophic Backtracking
        string parens = @"\((?>[^()]+|\((?<DEPTH>)|\)(?<-DEPTH>))*(?(DEPTH)(?!))\)";
        string brackets = @"\[(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\]";
        string curlies = @"\{(?>[^{}]+|\{(?<DEPTH>)|}(?<-DEPTH>))*(?(DEPTH)(?!))\}";
        
        // O padrão captura variáveis, constantes, chamadas de métodos, arrays e agrupamentos
        string termPattern = $@"(?:[\w_]+|{parens}|{brackets}|{curlies})(?:\s*[\.\:]\s*[\w_]+|\s*{parens}|\s*{brackets}|\s*{curlies})*";

        // 1. Substitui o Bitwise NOT unário (~) por bit32.bnot()
        // O (?!=) previne que o Regex altere o operador de desigualdade (~=) nativo do Lua!
        script = Regex.Replace(script, $@"~(?!=)\s*({termPattern})", "bit32.bnot($1)");

        // 2. Divisão Inteira (//) do Lua 5.3 para math.floor()
        string prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*//\s*({termPattern})", "math.floor($1 / $2)");
        }

        // 3. Bitwise Shifts (<< e >>) para garantir compatibilidade
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*<<\s*({termPattern})", "bit32.lshift($1, $2)");
        }
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*>>\s*({termPattern})", "bit32.rshift($1, $2)");
        }

        // 4. Substitui o Bitwise AND (&) por bit32.band()
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*&\s*({termPattern})", "bit32.band($1, $2)");
        }

        // 5. Substitui o Bitwise OR (|) por bit32.bor() (Substitui a adição perigosa anterior)
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*\|\s*({termPattern})", "bit32.bor($1, $2)");
        }

        // 6. Operador de Comprimento (#) do Lua 5.3 para userdata (MoonSharp crash fix)
        // Substitui a chamada de operador por uma verificação segura da função GetCount()
        script = Regex.Replace(script, @"#\s*([a-zA-Z_][a-zA-Z0-9_]*)", "(type($1)=='userdata' and $1:GetCount() or #$1)");

        // 7. Loop Direto sobre Userdata (Iterador para MoonSharp)
        // Converte "for tc in eg do" para "for tc in eg:Iter() do"
        script = Regex.Replace(script, @"for\s+([a-zA-Z0-9_]+)\s+in\s+([a-zA-Z0-9_]+)\s+do", "for $1 in $2:Iter() do");

        return script;
    }

    public void ActivateCard(CardDisplay card, object triggerArgs, System.Action onComplete)
    {
        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) 
        {
            onComplete?.Invoke();
            return;
        }

        // Procura por um efeito ativável instantâneo (Magia, Armadilha ou Ignition)
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);

        if (activationEffect != null)
        {
            int tp = luaCard.GetControler();
            if (!CanActivateEffect(luaCard, activationEffect, tp, triggerArgs))
            {
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} não cumpre os requisitos/custos para ativação no momento.");
                return; // Retorna sem chamar onComplete, abortando a ida para a Corrente
            }

            StartCoroutine(ActivationPhaseRoutine(luaCard, activationEffect, triggerArgs, onComplete));
        }
        else
        {
            onComplete?.Invoke(); // Não tem efeito manual, apenas deixa o jogo seguir
        }
    }

    public bool ExecuteCardEffect(CardDisplay card)
    {
        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) return false;

        // 1. Tenta encontrar e resolver um efeito que já foi ativado e passou pela Corrente
        if (pendingResolutions.TryGetValue(card, out PendingEffect pending))
        {
            pendingResolutions.Remove(card);
            StartCoroutine(ResolutionPhaseRoutine(luaCard, pending.effect, pending.triggerArgs));
            return true;
        }

        // 2. Fallback Original: Procura por um efeito e o ativa instantaneamente (Pula a Corrente)
        // Útil para efeitos engatilhados pela IA ou sistemas que não passam pela interface manual.
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040);

        if (activationEffect != null)
        {
            int tp = luaCard.GetControler();
            
            // 1. Verificação Síncrona (Condition, Cost chk=0, Target chk=0)
            if (!CanActivateEffect(luaCard, activationEffect, tp, null))
            {
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} não cumpre os requisitos/custos para ativação no momento.");
                return false;
            }

            // 2. Pagamento de Custo, Seleção de Alvos e Resolução (chk=1)
            StartCoroutine(ActivateAndResolveRoutine(luaCard, activationEffect, null));
            return true;
        }

        if (!GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} possui scripts carregados, mas não tem efeito ativável imediato.");
        return false;
    }

    private object WrapTriggerArgs(object triggerArgs)
    {
        if (triggerArgs == null) 
        {
            LuaGroup dummyGroup = new LuaGroup();
            dummyGroup.AddCard(new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }));
            return dummyGroup; 
        }
        if (triggerArgs is LuaCard card)
        {
            LuaGroup group = new LuaGroup();
            group.AddCard(card);
            return group;
        }
        else if (triggerArgs is List<CardDisplay> list)
        {
            LuaGroup group = new LuaGroup();
            foreach (var c in list) group.AddCard(new LuaCard(c));
            return group;
        }
        return triggerArgs;
    }

    public bool CanActivateEffect(LuaCard luaCard, LuaEffect effect, int tp, object triggerArgs)
    {
        object eg = WrapTriggerArgs(triggerArgs);
        bool isActionEffect = (effect.type & 0x07F8) != 0;
        object arg2 = isActionEffect ? (object)tp : (object)luaCard;
        try 
        {
            if (effect.conditionFunc != null) {
                DynValue res = luaEngine.Call(effect.conditionFunc, effect, arg2, eg, 0, 0, null, 0, 0);
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            if (effect.costFunc != null) {
                DynValue res = luaEngine.Call(effect.costFunc, effect, arg2, eg, 0, 0, null, 0, 0, 0); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            if (effect.targetFunc != null) {
                DynValue res = luaEngine.Call(effect.targetFunc, effect, arg2, eg, 0, 0, null, 0, 0, 0, null); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API LUA] Erro ao verificar condições de {luaCard.unityData.name}: {ex.Message}");
            return false;
        }
    }

    private IEnumerator ActivationPhaseRoutine(LuaCard luaCard, LuaEffect effect, object triggerArgs, System.Action onComplete)
    {
        int tp = luaCard.GetControler();

        // FASE DE ATIVAÇÃO (Custo e Alvo) [chk=1]
        if (effect.costFunc != null) yield return StartCoroutine(RunLuaCoroutine(effect.costFunc, effect, tp, triggerArgs, 1));
        if (effect.targetFunc != null) yield return StartCoroutine(RunLuaCoroutine(effect.targetFunc, effect, tp, triggerArgs, 1));

        // Guarda na Gaveta de Pendências esperando a autorização do ChainManager
        pendingResolutions[luaCard.unityCard] = new PendingEffect { effect = effect, triggerArgs = triggerArgs };

        onComplete?.Invoke();
    }

    private IEnumerator ResolutionPhaseRoutine(LuaCard luaCard, LuaEffect effect, object triggerArgs)
    {
        int tp = luaCard.GetControler();
        if (effect.operationFunc != null) yield return StartCoroutine(RunLuaCoroutine(effect.operationFunc, effect, tp, triggerArgs, -1));
        Debug.Log($"[API LUA] Efeito final resolvido: {luaCard.unityData.name}");
    }

    private IEnumerator ActivateAndResolveRoutine(LuaCard luaCard, LuaEffect effect, object triggerArgs)
    {
        int tp = luaCard.GetControler();

        // FASE DE ATIVAÇÃO (Custo e Alvo) [chk=1]
        if (effect.costFunc != null) yield return StartCoroutine(RunLuaCoroutine(effect.costFunc, effect, tp, triggerArgs, 1));
        if (effect.targetFunc != null) yield return StartCoroutine(RunLuaCoroutine(effect.targetFunc, effect, tp, triggerArgs, 1));

        // [FUTURO: Ponto onde o ChainManager entra em ação e pergunta ao oponente sobre Counter Traps]
        
        // FASE DE RESOLUÇÃO
        if (effect.operationFunc != null) yield return StartCoroutine(RunLuaCoroutine(effect.operationFunc, effect, tp, triggerArgs, -1));

        Debug.Log($"[API LUA] Efeito resolvido: {luaCard.unityData.name}");
    }

    private IEnumerator RunLuaCoroutine(Closure func, LuaEffect effect, int tp, object triggerArgs, int chk)
    {
        object eg = WrapTriggerArgs(triggerArgs);
        bool isActionEffect = (effect.type & 0x07F8) != 0;
        object arg2 = isActionEffect ? (object)tp : (object)(effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" }));

        activeLuaCoroutine = luaEngine.CreateCoroutine(func);
        DynValue result;
        
        try
        {
            if (chk >= 0) result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, 0, 0, null, 0, 0, chk);
            else result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, 0, 0, null, 0, 0);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API LUA CRASH] O efeito falhou graciosamente sem travar a engine: {e.Message}");
            yield break; // Aborta apenas este efeito, o jogo continua!
        }

        while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
        {
            while (isWaitingForLuaYield) yield return null;
            
            try
            {
                result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[API LUA CRASH] Falha ao continuar o yield: {e.Message}");
                yield break;
            }
        }
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;

        List<CardDisplay> allField = new List<CardDisplay>();
        CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allField);

        foreach(var c in allField)
        {
            LuaCard lc = EnsureCardScriptLoaded(c);
            if (lc == null) continue;

            var matchingEffects = lc.registeredEffects.FindAll(e => e.code == eventCode && (e.type & 0x0001) == 0);
            foreach(var effect in matchingEffects)
            {
                if (CanActivateEffect(lc, effect, lc.GetControler(), triggerArgs))
                {
                    StartCoroutine(ActivateAndResolveRoutine(lc, effect, triggerArgs));
                }
            }
        }
    }

    private void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        if (zones == null) return;
        foreach (var z in zones)
        {
            if (z != null && z.childCount > 0)
            {
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    // --- HOOKS DA ENGINE (O LUA VAI SE INSCREVER NELES DEPOIS) ---
    public void OnSummon(CardDisplay card) { 
        LuaCard lc = EnsureCardScriptLoaded(card);
        if (lc != null) {
            var singleEffects = lc.registeredEffects.FindAll(e => e.code == 11 && (e.type & 0x0001) != 0); // EFFECT_TYPE_SINGLE
            foreach(var e in singleEffects) 
            {
                if (CanActivateEffect(lc, e, lc.GetControler(), null))
                    StartCoroutine(ActivateAndResolveRoutine(lc, e, null));
            }
        }
        TriggerLuaEvent(11, lc); // EVENT_SUMMON_SUCCESS
    }
    public void OnSet(CardDisplay card) { }
    public void OnBattlePositionChanged(CardDisplay card) { }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { }
    public void OnCounterTrapResolved(CardDisplay trap) { }
    public void OnCardAddedToHand(CardDisplay card) { }
    public void OnTribute(CardDisplay card) { }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent) { }
    public void OnCardDrawn(CardData card, bool isPlayer) { }
    public void OnSpecialSummon(CardDisplay card) { }
    public void OnControlSwitched(CardDisplay card) { }
    public void OnPhaseStart(GamePhase phase) { 
        if (phase == GamePhase.End) CleanAllExpiredModifiers(); 
        TriggerLuaEvent(4096, null); // EVENT_PHASE
    }
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) { }
    public void OnDamageTaken(bool isPlayer, int amount) { }
    public void OnLifePointsGained(bool isPlayer, int amount) { }
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) { }
    public void OnSpellActivated(CardDisplay spell) { }
    
    // Métodos (Stubs) mantidos para não quebrar a lógica hardcoded do GameManager
    public void CheckMaintenanceCosts() { }
    public bool HasActiveSecondCoinToss(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeSecondCoinToss(bool isPlayer) { }
    public bool HasActiveDiceReRoll(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeDiceReRoll(bool isPlayer) { }

    public void OnCardLeavesField(CardDisplay card)
    {
        // Limpeza de cache de Lua para não poluir a RAM
        if (activeLuaCards.ContainsKey(card)) activeLuaCards.Remove(card);
        if (pendingResolutions.ContainsKey(card)) pendingResolutions.Remove(card);

        // Limpeza Genérica Obrigatória (Isso continua sendo C# nativo para não bugar a engine)
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);
            foreach (var zone in allZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (target != null) target.RemoveModifiersFromSource(card);
                }
            }
        }

        // Destrói equipamentos físicos que dependem desta carta
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.target == card && link.type == CardLink.LinkType.Equipment && link.source != null && link.source.isOnField)
            {
                GameManager.Instance.SendToGraveyard(link.source.CurrentCardData, link.source.isPlayerCard);
                Destroy(link.source.gameObject);
            }
        }

        // Libera zonas bloqueadas por cartas contínuas
        if (blockedZonesByCard.ContainsKey(card))
        {
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach (var z in blockedZonesByCard[card]) GameManager.Instance.duelFieldUI.UnblockZone(z);
            }
            blockedZonesByCard.Remove(card);
        }
    }

    // --- HOOKS DE BATALHA ---
    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) { 
        if (target != null && target.CurrentCardData != null && (GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData)))
        {
            LuaCard deadCard = EnsureCardScriptLoaded(target);
            if (deadCard != null) {
                var singleEffects = deadCard.registeredEffects.FindAll(e => e.code == 1010 && (e.type & 0x0001) != 0); // EVENT_BATTLE_DESTROYED
                foreach(var e in singleEffects) 
                {
                    if (CanActivateEffect(deadCard, e, deadCard.GetControler(), deadCard))
                        StartCoroutine(ActivateAndResolveRoutine(deadCard, e, deadCard));
                }
            }
            TriggerLuaEvent(1010, deadCard);
        }
    }
    public bool CanDeclareAttack(CardDisplay attacker) { return true; }
    public bool IsAttackPreventedByContinuousEffect(CardDisplay attacker) { return false; }
    public bool IsAttackRestricted(CardDisplay attacker) { return false; }

    // --- REGRAS CONTÍNUAS GLOBAIS ---
    public bool IsSummonRestricted(bool isSpecialSummon) { return false; }
    public bool ShouldBanishInsteadOfGraveyard() { return false; }
    public string GetEffectiveRace(CardDisplay card) { return !string.IsNullOrEmpty(card.temporaryRace) ? card.temporaryRace : (card.isTrapMonster ? card.trapMonsterRace : card.CurrentCardData.race); }
    public string GetEffectiveAttribute(CardDisplay card) { return !string.IsNullOrEmpty(card.temporaryAttribute) ? card.temporaryAttribute : (card.isTrapMonster ? card.trapMonsterAttribute : card.CurrentCardData.attribute); }
    public bool HasAttribute(CardDisplay card, string attribute) { return GetEffectiveAttribute(card).Equals(attribute, System.StringComparison.OrdinalIgnoreCase); }
    public bool CheckChainEnergy(bool isPlayer) { return true; }
    public bool CheckSpatialCollapse(bool isPlayer) { return true; }
    public bool CheckRivalryOfWarlords(bool isPlayer, string newRace) { return true; }
    public bool IsFusionSubstitute(string cardIdOrName) { return false; }

    // --- HELPERS E UTILITÁRIOS (A PONTE LUA VAI CHAMÁ-LOS) ---

    public void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    public void Effect_GainLP(CardDisplay source, int amount)
    {
        GameManager.Instance.GainLifePoints(source.isPlayerCard, amount);
    }

    public bool Effect_PayLP(CardDisplay source, int amount)
    {
        return GameManager.Instance.PayLifePoints(source.isPlayerCard, amount);
    }

    public List<CardDisplay> GetEquippedCards(CardDisplay target)
    {
        List<CardDisplay> equipped = new List<CardDisplay>();
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
            if (link.target == target && link.type == CardLink.LinkType.Equipment && link.source != null)
                equipped.Add(link.source);
        return equipped;
    }

    public void CleanAllExpiredModifiers()
    {
        if (GameManager.Instance.duelFieldUI == null) return;
        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) cd.CleanExpiredModifiers();
            }
        }
    }

    public void RollDice(int amount, System.Action<List<int>> onResult)
    {
        List<int> results = new List<int>();
        for (int i = 0; i < amount; i++) results.Add(Random.Range(1, 7));
        onResult?.Invoke(results);
    }

    public void SetClockCounter(CardDisplay target, int turns)
    {
        if (target != null) target.turnCounter = turns; 
    }

    public void TickClock(CardDisplay card, System.Action onComplete)
    {
        if (card == null || card.turnCounter <= 0) { onComplete?.Invoke(); return; }
        int oldTurns = card.turnCounter;
        card.turnCounter--; 
        if (GameManager.Instance != null && GameManager.Instance.turnClockUI != null && GameManager.Instance.enableTurnClockVisuals)
            GameManager.Instance.turnClockUI.AnimateTick(card.CurrentCardData.name, oldTurns, card.turnCounter, card.maxTurnCounter, onComplete);
        else onComplete?.Invoke(); 
    }

    // =========================================================================
    // FERRAMENTA DE DEV: VALIDADOR DE SCRIPTS EM MASSA
    // =========================================================================
    [ContextMenu("DEV: Validar Todos os Scripts LUA")]
    public void DevValidateAllScripts()
    {
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) 
        {
            Debug.LogError("O jogo precisa estar rodando (Play) para executar a validação.");
            return;
        }
        
        int total = 0, success = 0, failed = 0;
        List<string> errorLogs = new List<string>();
        Debug.Log("<color=cyan>Iniciando Validação em Massa de LUA...</color>");
        
        foreach (var card in GameManager.Instance.cardDatabase.cardDatabase)
        {
            if (card.type.Contains("Normal") && !card.type.Contains("Effect")) continue;
            
            total++;
            GameObject dummyGO = new GameObject("DummyTester");
            dummyGO.AddComponent<UnityEngine.UI.RawImage>(); // Silencia o aviso visual
            CardDisplay dummy = dummyGO.AddComponent<CardDisplay>();
            dummy.gameObject.SetActive(false); // Mantém invisível
            dummy.SetCard(card, null, true);
            
            try
            {
                LuaCard lc = EnsureCardScriptLoaded(dummy);
                if (lc != null) 
                {
                    bool runtimeError = false;
                     // Evita o erro 'Collection was modified' fazendo uma cópia da lista
                    foreach (var eff in new List<LuaEffect>(lc.registeredEffects))
                     {
                        try {
                            object eg = WrapTriggerArgs(null);
                            LuaEffect dummyRe = new LuaEffect { owner = lc };
                            LuaCard dummyChkc = new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 });

                            // Efeitos de Ação/Gatilho (0x07F8 cobre do 0x0008 ao 0x0400: Activate, Flip, Ignition, Quick, Trigger_F, etc)
                            // Se for ação, envia o Jogador (tp). Se for contínuo/campo, envia a Carta (c).
                            bool isActionEffect = (eff.type & 0x07F8) != 0; 

                            if (eff.conditionFunc != null) luaEngine.Call(eff.conditionFunc, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, 0, 0, dummyRe, 0, 0);
                            if (eff.costFunc != null) luaEngine.Call(eff.costFunc, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, 0, 0, dummyRe, 0, 0, 0);
                            // O '0' a mais aqui corrige o chk=0, impedindo ativações falsas explosivas!
                            if (eff.targetFunc != null) luaEngine.Call(eff.targetFunc, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, 0, 0, dummyRe, 0, 0, 0, dummyChkc);
                        } catch (System.Exception ex) {
                            errorLogs.Add($"<color=orange>[RUNTIME] Falha na carta {card.name} ({card.id}): {ex.Message}</color>");
                            runtimeError = true;
                        }
                    }
                    if (runtimeError) failed++;
                    else success++;
                }
            }
            catch (System.Exception ex)
            {
                failed++;
                errorLogs.Add($"<color=red>Falha na carta {card.name} ({card.id}): {ex.Message}</color>");
            }
            DestroyImmediate(dummy.gameObject);
        }
        Debug.Log($"<color=green>Validação Concluída! Total: {total} | Sucesso: {success} | Falhas: {failed}</color>");

        if (errorLogs.Count > 0)
        {
            Debug.LogWarning($"<color=yellow>--- RELATÓRIO DE ERROS ({errorLogs.Count}) ---</color>");
            foreach (var err in errorLogs)
            {
                Debug.LogError(err);
            }
        }
    }
}