using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ConfManager collects all misc methods regarding the configuration of game stage and UI.
/// </summary>
public class ConfManager : MonoBehaviour
{
    public InformationPanel informationPanel;
    public TextMeshProUGUI timerText;
    public GridController gridController;
    public CodeInterpreter codeInterpreter;
    public PartnerSocket partnerSocket;
    public VoiceController voiceController;
    public GameObject arrowKeyPanel;
    public GameObject sttCtrlPanel, sttCtrlPanelAlwaysOn;
    public GameObject developerPanel;
    public GameObject startScreen, endScreen, nextStageScreen;
    public Transform stageProgressView;
    public Button stageButtonPrefab;
    public CameraFocusControl cameraFocusControl;
    public AvatarController[] defaultAvatars, helicopterAvatars;

    public InputField serverIPField;
    private const string PREFKEY_SERVER_IP = "pref_key_server_ip";
    public Boolean UseOfflineMode { get; set; }  // set single-user mode
    public Boolean UseRewardAsOrigin { get; set; }  // set the avatar's position the last reward

    public ConfigScrollerController configScrollerController;

    private List<Button> stageButtons;
    private List<string> stageKeys;
    private int currentStageIndex;
    private OrderedDictionary stages;
    private List<OrderedDictionary> configSets;
    private ScriptObject scriptSolution;

    private TimeSpan timer;
    private float timeElapsed;

    [HideInInspector] public bool isSlave;

    public const string CTRL_APPLY_CONFIG = "CTRL_APPLY_CONFIG";


    // ========================================
    // System Routines
    // ========================================

    private void Start()
    {
        // Prevent the screen from sleeping
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // Open the welcome screen
        startScreen.SetActive(true);
        endScreen.SetActive(false);
        nextStageScreen.SetActive(false);
        // Add user's random name
        startScreen.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = DataLogger.username;

        // Default layout
        sttCtrlPanel.SetActive(true);
        sttCtrlPanelAlwaysOn.SetActive(false);

        // Recording
        ToggleAlwaysOnRecording(true);

        // Networking
        serverIPField.onValueChanged.AddListener((string value) =>
        {
            SetSocketIPAddr();
        });
        ShowSocketIPAddr();

        // Retrieve the predefined configuration set
        configSets = InitConfigSets();
        configScrollerController.SetData(configSets);
    }

    private void Update()
    {
        if (timer != null && timer.TotalSeconds > 0)
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= 1)
            {
                timer = timer.Subtract(TimeSpan.FromSeconds(1));
                timerText.text = string.Format("{0:c}", timer);
                if (timer.TotalSeconds <= 0.5f)
                {
                    timerText.text = "<mark=#ff0000aa>" + timerText.text + "</mark>";
                }
                timeElapsed = 0;
            }
        }
    }


    // ========================================
    // Custom Maps and Configuration Logistics
    // ========================================

    /// <summary>
    /// Used by the start button in the menu screen. This method serves as a preprocessing stage before the game-starting routine.
    /// </summary>
    /// <param name="configSetID"></param>
    public void LoadConfigSetAndStartGame(int configSetID = 0)
    {
        // Load a configuration set
        if (configSetID < configSets.Count)
        {
            stages = configSets[configSetID];
        }
        else
        {
            Debug.LogError("Invalid config set ID: " + configSetID);
            return;
        }

        stageButtons = new List<Button>();
        stageKeys = new List<string>();
        foreach (DictionaryEntry kv in stages)
        {
            var newButton = Instantiate(stageButtonPrefab, parent: stageProgressView);
            var newButtonText = newButton.GetComponentInChildren<Text>();

            newButton.onClick.AddListener(() => { ApplyConfigurationSync((string)kv.Key); });
            newButton.interactable = false;  // To test you can make it true
            newButtonText.text = (string)kv.Key;
            newButton.gameObject.SetActive(true);

            stageKeys.Add((string)kv.Key);
            stageButtons.Add(newButton);
        }
        stageButtons[0].interactable = true;
        stageButtons[stageButtons.Count - 1].GetComponent<Image>().sprite = null;

        StartGame();
    }

    /// <summary>
    /// Load configuration files according to a pre-defined rule. This is the only place you need to change when adding new game stages (which should be defined in an external file).
    /// </summary>
    /// <param name="configSetID"></param>
    /// <returns></returns>

    private List<OrderedDictionary> InitConfigSets()
    {
        // Import the copied configurations
        var configTutorialAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopterTutorial-ASUFall19", true);
        var configTutorialNonAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopterTutorial-ASUFall19", false);
        var configHeliAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopter-ASUFall19", true);
        var configHeliNonAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopter-ASUFall19", false);

        var configJamesA1M1 = OgStageConfig.ImportConfigJSON("OC-JamesA1M1-08012020.json", false);
        var configJamesA1M2 = OgStageConfig.ImportConfigJSON("OC-JamesA1M2-08012020.json", true);
        var configJamesA1M3 = OgStageConfig.ImportConfigJSON("OC-JamesA1M3-10062020.json", true);

        var stageList = new List<OrderedDictionary>() {
            new OrderedDictionary() {{"2020-JA1-Map1", configJamesA1M1}, {"2020-JA1-Map2", configJamesA1M2}, {"2020-JA1-Map3", configJamesA1M3}},
            new OrderedDictionary() {{"ASUF19-Tutorial", configTutorialAR}, {"Task-AR", configHeliAR}, {"Task-NonAR", configHeliNonAR}},
            new OrderedDictionary() {{"ASUF19-Tutorial", configTutorialNonAR}, {"Task-NonAR", configHeliNonAR}, {"Task-AR", configHeliAR}},
        };

        return stageList;
    }


    // ========================================
    // Loading Configs
    // ========================================

    public void ApplyConfigurationSync(string confName)
    {
        if (stageKeys.Contains(confName))
        {
            var newStageIndex = stageKeys.IndexOf(confName);

            if (currentStageIndex != newStageIndex)
            {
                currentStageIndex = newStageIndex;
                _ApplyConfiguration((OgStageConfig)stages[confName]);

                // Also tell the partner to load the configuration
                partnerSocket.BroadcastAvatarCtrl(
                    new CodeObjectOneCommand(CTRL_APPLY_CONFIG, new string[] { confName })
                );
            }
        }
        else
        {
            Debug.LogError("Configuration was not found: " + confName);
        }
    }

    private void _ApplyConfiguration(OgStageConfig conf)
    {
        DataLogger.Log(this.gameObject, LogTag.SYSTEM, "Loading a configuration, " + conf);

        if (conf.isAREnabled)
            cameraFocusControl.ToggleARCamera(true);
        else
            cameraFocusControl.ToggleARCamera(false);

        timer = TimeSpan.FromMinutes(conf.timerTotalMin);
        timerText.text = string.Format("{0:c}", timer);
        timeElapsed = 0;

        nextStageScreen.SetActive(false);

        informationPanel.ReplaceContent(conf.problem);
        gridController.LoadGridMap(conf.map);
        arrowKeyPanel.SetActive(conf.isArrowKeyEnabled);
        sttCtrlPanel.SetActive(!conf.isArrowKeyEnabled);
        developerPanel.SetActive(conf.isDeveloperPanelEnabled);

        codeInterpreter.ExecMode = conf.execMode;

        // Set avatars
        if (conf.avatarSetName.Equals("Avatar-Helicopter"))
        {
            codeInterpreter.SetAvatarGameObjects(helicopterAvatars[0], helicopterAvatars[1]);
            // A ghost avatar serves as placeholder
            if (partnerSocket.IsMaster)
                helicopterAvatars[1].gameObject.SetActive(false);
            else
                helicopterAvatars[0].gameObject.SetActive(false);
        }
        else
        {
            codeInterpreter.SetAvatarGameObjects(defaultAvatars[0], defaultAvatars[1]);
        }

        // Load scripts
        if (partnerSocket.IsMaster)
        {
            if (conf.masterScript != null)
            {
                codeInterpreter.LoadPredefinedScript(conf.masterScript);
            }
        }
        else
        {
            if (conf.slaveScript != null)
            {
                codeInterpreter.LoadPredefinedScript(conf.slaveScript);
            }
        }

        informationPanel.ShowInfoPanel(true);
        codeInterpreter.ResetAvatars();
    }


    // ========================================
    // Internal Game-control-flow Logistics 
    // ========================================

    private void SetSocketIPAddr()
    {
        PlayerPrefs.SetString(PREFKEY_SERVER_IP, serverIPField.text);
        partnerSocket.SetServerIP(serverIPField.text);
    }
    private void ShowSocketIPAddr()
    {
        string savedIP = PlayerPrefs.GetString(PREFKEY_SERVER_IP, partnerSocket.GetServerIP());
        serverIPField.text = savedIP;
    }

    /// <summary>
    /// The major game-starting routine. This method does the following things:
    /// - Set up the internet connection to the IP entered by the user
    /// - Apply the game configuration (currently associated to individual start buttons)
    /// </summary>
    public void StartGame()
    {
        // The user can choose whether to start with the offline mode or not
        if (!UseOfflineMode)
        {
            SetSocketIPAddr();
            if (!partnerSocket.IsConnected())
                partnerSocket.SetupRemoteServer();
        }

        // Apply the first game configuration
        currentStageIndex = 0;
        _ApplyConfiguration((OgStageConfig)stages[stageKeys[currentStageIndex]]);
        // Disable the start screen and reset the avatar
        startScreen.SetActive(false);
        codeInterpreter.ResetAvatars();
        // informationPanel.ShowInstructionScreen(true);
    }

    public void StopGame()
    {
        endScreen.SetActive(true);
    }

    public void RestartGame(bool decoy = false)
    {
        if (!decoy)
        {
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM_WARNING,
                "[Admin] Trying to restart the scene..."
            );
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            endScreen.SetActive(false);
        }
    }

    public void EnableNextStage()
    {
        stageButtons[currentStageIndex].interactable = false;

        var nextStageIndex = currentStageIndex + 1;
        if (stageButtons != null && nextStageIndex < stageButtons.Count)
        {
            stageButtons[nextStageIndex].interactable = true;
            nextStageScreen.SetActive(true);
        }
        if (nextStageIndex == stageButtons.Count)
        {
            StopGame();
        }
    }

    public void GoToNextStage()
    {
        var tmp = currentStageIndex + 1;
        if (tmp < stageKeys.Count)
        {
            ApplyConfigurationSync(stageKeys[tmp]);
            currentStageIndex = tmp;
        }
    }

    public void GoToPrevStage()
    {
        var tmp = currentStageIndex - 1;
        if (tmp >= 0)
        {
            ApplyConfigurationSync(stageKeys[tmp]);
            currentStageIndex = tmp;
        }
    }

    public void CloseNextStageScreen()
    {
        nextStageScreen.SetActive(false);
    }


    // ========================================
    // Misc
    // ========================================

    public void ToggleAlwaysOnRecording(bool value)
    {
        // When mic is always on, do not send the final clip but every clip
        voiceController.ToggleMicrophone(value);
        // voiceController.sendEveryClip = value;
        // voiceController.sendFinalClip = !value;

        sttCtrlPanelAlwaysOn.SetActive(value);
        sttCtrlPanel.SetActive(!value);
    }

    public void CloseDeveloperPanel()
    {
        developerPanel.SetActive(false);
    }

    private int _developerMagicCount = 0;
    public void OpenDeveloperPanel()
    {
        _developerMagicCount++;
        if (_developerMagicCount >= 5)
        {
            developerPanel.SetActive(true);
            _developerMagicCount = 0;
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM_WARNING,
                "[Admin] The developer panel is open."
            );
        }
    }

    /// <summary>
    /// An abstract data type of game configuration
    /// </summary>
    private class OgStageConfig
    {
        public string name;
        public string problem;
        public string map;
        public string masterScript, slaveScript;
        public bool isArrowKeyEnabled, isDeveloperPanelEnabled;
        public bool isAREnabled;
        public string avatarSetName;
        public CodeInterpreter.ScriptExecMode execMode;
        public int timerTotalMin;

        public OgStageConfig(
            string name, string problem, string map, string masterScript, string slaveScript,
            bool isArrowKeyEnabled, bool isDeveloperPanelEnabled, string avatarSetName, CodeInterpreter.ScriptExecMode execMode)
        {
            this.name = name;
            this.problem = problem;
            this.map = map;
            this.masterScript = masterScript.Equals("null") ? null : masterScript;
            this.slaveScript = slaveScript.Equals("null") ? null : slaveScript;
            this.isArrowKeyEnabled = isArrowKeyEnabled;
            this.isDeveloperPanelEnabled = isDeveloperPanelEnabled;
            this.avatarSetName = avatarSetName;
            this.execMode = execMode;

            this.isAREnabled = false;
        }

        /// <summary>
        /// Load a configuration file from the Unity Resources to the data storage on the device.
        /// </summary>
        /// <param name="filename">the file to load</param>
        /// <returns></returns>
        private static string CopyResourceToDevice(string filename)
        {
            Debug.Log("Loading from resources:" + filename);
            // Load the asset from the resources (the filename should not have any extension)
            var _resFilename = Path.GetFileNameWithoutExtension(filename);
            var resData = (TextAsset)Resources.Load(_resFilename, typeof(TextAsset));
            // Define the path to output
            var dataFolderPath = Application.persistentDataPath;
            var outPath = Path.Combine(dataFolderPath, _resFilename) + ".txt";
            // Dump the asset
            using (var writer = new StreamWriter(outPath))
            {
                writer.Write(resData.text);
            }
            return outPath;
        }

        /// <summary>
        /// Parse and import a configuration file into an object.
        /// </summary>
        /// <param name="filename">A text file in the JSON format</param>
        /// <param name="enableAR">Whether to enable AR</param>
        /// <returns>A config object</returns>
        public static OgStageConfig ImportConfigJSON(string filename, bool enableAR)
        {
            // Load the file into a string
            var configFilePath = CopyResourceToDevice(filename);
            var json = "";
            using (var reader = new StreamReader(configFilePath))
            {
                json = reader.ReadToEnd();
            }
            // If a field is missing, it will be set by the default value of its type
            var jsonObj = JsonUtility.FromJson<OgStageConfigJSON>(json);

            // Convert the config in json into the format accepted by the system

            var name = jsonObj.name;
            var problem = jsonObj.problem;

            var map = jsonObj.map;
            CopyResourceToDevice(map);
            var masterScript = jsonObj.masterScript;
            CopyResourceToDevice(masterScript);
            var slaveScript = jsonObj.slaveScript;
            CopyResourceToDevice(slaveScript);

            var isArrowKeyEnabled = Boolean.Parse(jsonObj.isArrowKeyEnabled);
            var isDeveloperPanelEnabled = Boolean.Parse(jsonObj.isDeveloperPanelEnabled);

            var avatarSetName = (jsonObj.avatarSetName == null) ? "Avatar-Default" : jsonObj.avatarSetName;
            var syncMode = (CodeInterpreter.ScriptExecMode)jsonObj.execMode;

            var timerTotalMin = (jsonObj.timerTotalMin > 0) ? jsonObj.timerTotalMin : 15;

            OgStageConfig rt = null;
            rt = new OgStageConfig(
                name, problem, map,
                masterScript, slaveScript,
                isArrowKeyEnabled, isDeveloperPanelEnabled,
                avatarSetName, syncMode
            );
            rt.isAREnabled = enableAR;
            rt.timerTotalMin = timerTotalMin;

            return rt;
        }

        override public string ToString()
        {
            return name;
        }
    }


    /// <summary>
    /// This class is specifically used as a wrapper to read/write a configuration in the JSON format. 
    /// </summary>
    private class OgStageConfigJSON
    {
        public string name, problem, map, masterScript, slaveScript, isArrowKeyEnabled, isDeveloperPanelEnabled, avatarSetName;
        public int execMode;
        public int timerTotalMin;
    }
}
