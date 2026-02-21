using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance;

    [Header("Audio UI")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider voiceSlider;
    public Slider systemSlider; // Teclado/UI

    [Header("Video UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;
    public Slider brightnessSlider;
    public Toggle retroFilterToggle;

    [Header("Controls UI")]
    public Transform keybindListContent; // Onde os botões serão instanciados
    public GameObject keybindSlotPrefab; // O prefab do KeybindSlot
    public GameObject pressAnyKeyOverlay; // Painel "Pressione uma tecla..."

    [Header("Scene References")]
    public Image brightnessOverlay; // Imagem preta que cobre a tela (Raycast Target = false)
    public GameObject retroFilterObject; // Objeto com shader/efeito de scanline

    // Dados de Runtime
    private Resolution[] resolutions;
    public Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();
    
    // Valores padrão
    private Dictionary<string, KeyCode> defaultBindings = new Dictionary<string, KeyCode>() {
        { "Confirm", KeyCode.Z },
        { "Cancel", KeyCode.X },
        { "Detail", KeyCode.C },
        { "Menu", KeyCode.Return },
        { "Pause", KeyCode.P },
        { "Up", KeyCode.UpArrow },
        { "Down", KeyCode.DownArrow },
        { "Left", KeyCode.LeftArrow },
        { "Right", KeyCode.RightArrow }
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Garante que as opções persistam entre cenas
        
        // Carrega configurações salvas
        LoadSettings();
    }

    void Start()
    {
        InitVideoUI();
        InitAudioUI();
        InitControlsUI();
    }

    // --- VÍDEO ---

    void InitVideoUI()
    {
        // Resoluções
        resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResIndex = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);
                if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                    currentResIndex = i;
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex);
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        // Fullscreen
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // Quality
        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        // VSync
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }

        // Brightness
        if (brightnessSlider != null)
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
            SetBrightness(savedBrightness); // Aplica imediatamente
        }

        // Retro Filter
        if (retroFilterToggle != null)
        {
            bool retroOn = PlayerPrefs.GetInt("RetroFilter", 0) == 1;
            retroFilterToggle.isOn = retroOn;
            retroFilterToggle.onValueChanged.AddListener(SetRetroFilter);
            SetRetroFilter(retroOn);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("Quality", qualityIndex);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    public void SetBrightness(float value)
    {
        // Valor 1 = Brilho normal (Alpha 0). Valor 0 = Escuro (Alpha ~0.8)
        if (brightnessOverlay != null)
        {
            float alpha = 1.0f - Mathf.Clamp01(value);
            // Não deixe ficar totalmente preto, limite a 0.9
            alpha *= 0.9f; 
            Color c = brightnessOverlay.color;
            c.a = alpha;
            brightnessOverlay.color = c;
        }
        PlayerPrefs.SetFloat("Brightness", value);
    }

    public void SetRetroFilter(bool isEnabled)
    {
        if (retroFilterObject != null) retroFilterObject.SetActive(isEnabled);
        PlayerPrefs.SetInt("RetroFilter", isEnabled ? 1 : 0);
    }

    // --- ÁUDIO ---

    void InitAudioUI()
    {
        if (masterSlider) SetupSlider(masterSlider, "MasterVolume", SetMasterVolume);
        if (musicSlider) SetupSlider(musicSlider, "MusicVolume", SetMusicVolume);
        if (sfxSlider) SetupSlider(sfxSlider, "SFXVolume", SetSFXVolume);
        if (voiceSlider) SetupSlider(voiceSlider, "VoiceVolume", SetVoiceVolume);
        if (systemSlider) SetupSlider(systemSlider, "SystemVolume", SetSystemVolume);
    }

    void SetupSlider(Slider slider, string prefKey, UnityEngine.Events.UnityAction<float> action)
    {
        float val = PlayerPrefs.GetFloat(prefKey, 1.0f);
        slider.value = val;
        slider.onValueChanged.AddListener(action);
        // Aplica o valor inicial (você precisará de um AudioManager para usar esses valores)
    }

    public void SetMasterVolume(float value) { PlayerPrefs.SetFloat("MasterVolume", value); AudioListener.volume = value; }
    public void SetMusicVolume(float value) { PlayerPrefs.SetFloat("MusicVolume", value); }
    public void SetSFXVolume(float value) { PlayerPrefs.SetFloat("SFXVolume", value); }
    public void SetVoiceVolume(float value) { PlayerPrefs.SetFloat("VoiceVolume", value); }
    public void SetSystemVolume(float value) { PlayerPrefs.SetFloat("SystemVolume", value); }

    // --- CONTROLES ---

    void InitControlsUI()
    {
        if (keybindListContent == null || keybindSlotPrefab == null) return;

        // Limpa lista visual
        foreach (Transform child in keybindListContent) Destroy(child.gameObject);

        // Cria slots
        foreach (var kvp in keyBindings)
        {
            GameObject go = Instantiate(keybindSlotPrefab, keybindListContent);
            KeybindSlot slot = go.GetComponent<KeybindSlot>();
            if (slot != null) slot.Setup(kvp.Key, kvp.Value, this);
        }
    }

    public void StartRebindProcess(string actionName, KeybindSlot slotUI)
    {
        StartCoroutine(RebindRoutine(actionName, slotUI));
    }

    IEnumerator RebindRoutine(string actionName, KeybindSlot slotUI)
    {
        if (pressAnyKeyOverlay != null) pressAnyKeyOverlay.SetActive(true);

        // Espera até que uma tecla seja pressionada
        bool keyFound = false;
        KeyCode newKey = KeyCode.None;

        while (!keyFound)
        {
            if (Input.anyKeyDown)
            {
                foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(k))
                    {
                        newKey = k;
                        keyFound = true;
                        break;
                    }
                }
            }
            yield return null;
        }

        // Aplica
        if (newKey != KeyCode.None && newKey != KeyCode.Escape) // Escape cancela
        {
            keyBindings[actionName] = newKey;
            slotUI.UpdateKeyText(newKey);
            SaveKeyBindings();
        }

        if (pressAnyKeyOverlay != null) pressAnyKeyOverlay.SetActive(false);
    }

    // --- SISTEMA DE SAVE/LOAD ---

    void LoadSettings()
    {
        // Carrega teclas
        keyBindings.Clear();
        foreach (var kvp in defaultBindings)
        {
            string savedKey = PlayerPrefs.GetString("Key_" + kvp.Key, kvp.Value.ToString());
            KeyCode code = (KeyCode)System.Enum.Parse(typeof(KeyCode), savedKey);
            keyBindings.Add(kvp.Key, code);
        }
        
        // Aplica configurações iniciais de vídeo que não dependem da UI
        SetBrightness(PlayerPrefs.GetFloat("Brightness", 1.0f));
        SetRetroFilter(PlayerPrefs.GetInt("RetroFilter", 0) == 1);
    }

    void SaveKeyBindings()
    {
        foreach (var kvp in keyBindings)
        {
            PlayerPrefs.SetString("Key_" + kvp.Key, kvp.Value.ToString());
        }
        PlayerPrefs.Save();
    }

    // Método auxiliar para outros scripts pegarem o volume
    public float GetVolume(string type)
    {
        return PlayerPrefs.GetFloat(type + "Volume", 1.0f);
    }
    
    // Método auxiliar para verificar input
    public bool GetActionDown(string action)
    {
        if (keyBindings.ContainsKey(action))
            return Input.GetKeyDown(keyBindings[action]);
        return false;
    }
}
