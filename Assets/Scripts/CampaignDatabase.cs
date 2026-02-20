using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCampaignDatabase", menuName = "YuGiOh/Campaign Database")]
public class CampaignDatabase : ScriptableObject
{
    [System.Serializable]
    public class ActData
    {
        public string actName;
        [TextArea(3, 5)]
        public string description;
        public Sprite backgroundMap; // Imagem de fundo para este ato (opcional)
        public List<string> opponentIDs; // IDs dos 10 oponentes (ex: "simon", "yugi")
    }

    public List<ActData> acts = new List<ActData>();

    // Função auxiliar para pegar o oponente correto baseado no nível global (1 a 100)
    public string GetOpponentIdByGlobalLevel(int globalLevel)
    {
        // Ajusta para índice 0 (nível 1 vira índice 0)
        int index = globalLevel - 1;
        
        // Cada ato tem 10 oponentes (Fixo)
        int actIndex = index / 10;
        int opponentIndex = index % 10;

        if (actIndex < acts.Count)
        {
            if (opponentIndex < acts[actIndex].opponentIDs.Count)
            {
                return acts[actIndex].opponentIDs[opponentIndex];
            }
        }
        
        return "simon"; // Fallback
    }

    public ActData GetActData(int actIndex)
    {
        if (actIndex >= 0 && actIndex < acts.Count)
            return acts[actIndex];
        return null;
    }

    // --- FERRAMENTA DE CONFIGURAÇÃO AUTOMÁTICA ---
    // Clique com o botão direito no componente no Inspector e selecione "Load Optimized Campaign"
    [ContextMenu("Load Optimized Campaign")]
    public void LoadOptimizedCampaign()
    {
        acts.Clear();

        // Act 1: O Início (Treinamento e Escola)
        // Boss: Duke Devlin
        AddAct("Act 1: O Início", 
            "Sua jornada começa na Academia de Duelos. Enfrente estudantes e amigos para provar seu valor.",
            new List<string> { "001_novice", "002_student", "003_female_student", "007_intermediate", "008_expert", "009_mokuba", "004_tristan", "005_tea", "006_grandpa", "010_duke" });

        // Act 2: Reino dos Duelistas (Rivais Clássicos)
        // Boss: Pegasus
        AddAct("Act 2: Reino dos Duelistas", 
            "Você foi convidado para a ilha de Pegasus. Os maiores rivais estão aqui.",
            new List<string> { "013_rex", "014_weevil", "015_mako", "011_joey", "012_mai", "016_keith", "017_espa", "018_bakura", "019_yami_bakura", "020_pegasus" });

        // Act 3: Batalha na Cidade (Kaiba Corp & Rare Hunters)
        // Boss: Seto Kaiba (Trocamos Noah/Gozaburo por fillers GBA para manter o tema 'Cidade')
        AddAct("Act 3: Batalha na Cidade", 
            "O torneio mudou para as ruas de Domino City. Cuidado com os Rare Hunters.",
            new List<string> { "029_rare_hunter", "030_rare_hunter_elite", "027_strings", "028_arkana", "026_odion", "025_ishizu", "049_female_gba", "050_grandpa_gba", "022_yugi", "021_kaiba" });

        // Act 4: Guardiões Místicos (Magos e Antigos)
        // Boss: Shadi (Movemos os fillers GBC para o início como 'Ilusões')
        AddAct("Act 4: Guardiões Místicos", 
            "Para obter o poder antigo, você deve derrotar os guardiões dos elementos.",
            new List<string> { "039_novice_gbc", "040_female_gbc", "031_desert_mage", "032_forest_mage", "033_mountain_mage", "034_meadow_mage", "035_ocean_mage", "036_labyrinth_mage", "037_simon", "038_shadi" });

        // Act 5: Mundo Virtual (Big Five)
        // Boss: Gozaburo Kaiba (Trazidos do Act 3 para cá, onde fazem sentido)
        AddAct("Act 5: Pesadelo Virtual", 
            "Você está preso no ciberespaço. Os Big Five e a família Kaiba digital querem seu corpo.",
            new List<string> { "046_intermediate_gbc", "047_expert_gbc", "048_student_gbc", "041_gansley", "042_crump", "043_johnson", "044_leclyde", "045_crockett", "023_noah", "024_gozaburo" });

        // Act 6: Ascensão das Trevas (Versões Dark/Advanced)
        // Boss: Heishin (Movido do início para o final como o grande vilão deste arco)
        AddAct("Act 6: Ascensão das Trevas", 
            "Heishin despertou e corrompeu os duelistas. Enfrente suas versões sombrias.",
            new List<string> { "054_tea_adv", "055_tristan_adv", "058_rex_adv", "059_weevil_adv", "060_mako_adv", "056_joey_adv", "057_mai_adv", "052_arkana_dark", "053_bakura_spirit", "051_heishin" });

        // Act 7: A Elite (Advanced Rivals)
        // Boss: Kaiba Advanced (Movemos os Rematches para o início como aquecimento)
        AddAct("Act 7: O Desafio da Elite", 
            "Os melhores do mundo retornaram, mais fortes do que nunca.",
            new List<string> { "069_rare_hunter_rematch", "070_rare_elite_rematch", "067_odion_rematch", "068_strings_rematch", "061_keith_adv", "062_espa_adv", "063_pegasus_adv", "066_ishizu_adv", "065_yugi_adv", "064_kaiba_adv" });

        // Act 8: Altos Magos (High Mages)
        // Boss: Anubisius (O mais icônico dos High Mages)
        AddAct("Act 8: O Vale dos Reis", 
            "Viaje ao passado para enfrentar os Sumos Sacerdotes e seus poderes divinos.",
            new List<string> { "076_desert_rematch", "077_forest_rematch", "078_mountain_rematch", "079_meadow_rematch", "080_ocean_rematch", "072_isis", "073_secmeton", "074_martis", "075_kepura", "071_anubisius" });

        // Act 9: O Labirinto Final (Rematches e Bakura)
        // Boss: Dark Bakura Final
        AddAct("Act 9: O Labirinto Final", 
            "O mal se reúne para uma última tentativa de pará-lo.",
            new List<string> { "081_labyrinth_rematch", "082_gansley_rematch", "083_crump_rematch", "084_johnson_rematch", "085_leclyde_rematch", "086_crockett_rematch", "087_simon_rematch", "088_shadi_rematch", "089_bakura_final", "090_dark_bakura_final" });

        // Act 10: A Batalha Suprema (Final Versions)
        // Boss: Marik
        AddAct("Act 10: A Batalha Suprema", 
            "O torneio definitivo para decidir o destino do mundo.",
            new List<string> { "091_joey_final", "092_mai_final", "093_rex_final", "094_weevil_final", "095_mako_final", "096_keith_final", "097_espa_final", "098_pegasus_final", "099_kaiba_final", "100_marik" });

        Debug.Log("Campanha otimizada carregada com sucesso!");
    }

    private void AddAct(string name, string desc, List<string> opponents)
    {
        ActData newAct = new ActData();
        newAct.actName = name;
        newAct.description = desc;
        newAct.opponentIDs = opponents;
        acts.Add(newAct);
    }
}