using System;
using System.Collections;
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

    private List<Button> stageButtons;
    private List<string> stageKeys;
    private int currentStageIndex;
    private Dictionary<string, OgStageConfig> stages;
    private ScriptObject scriptSolution;

    private TimeSpan timer;
    private int timerTotalMin = 15;
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
    /// Load configuration files according to a pre-defined rule. This is the only place you need to change when adding new game stages (which should be defined in an external file).
    /// </summary>
    /// <param name="configSetID"></param>
    /// <returns></returns>
    private Dictionary<string, OgStageConfig> LoadConfigSets(int configSetID = 0)
    {
        // Import the copied configurations
        var configTutorialAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopterTutorial-ASUFall19", true);
        var configTutorialNonAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopterTutorial-ASUFall19", false);
        var configHeliAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopter-ASUFall19", true);
        var configHeliNonAR = OgStageConfig.ImportConfigJSON("OgConfig-FlyingHelicopter-ASUFall19", false);

        var stages = new Dictionary<string, OgStageConfig>();
        switch (configSetID)
        {
            case 1:  // Tutorial AR - Heli AR - Heli NonAR
                stages.Add("Tutorial", configTutorialAR);
                stages.Add("Task-AR", configHeliAR);
                stages.Add("Task-NonAR", configHeliNonAR);
                break;
            case 2:  // Tutorial NonAR - Heli NonAR - Heli AR
                stages.Add("Tutorial", configTutorialNonAR);
                stages.Add("Task-NonAR", configHeliNonAR);
                stages.Add("Task-AR", configHeliAR);
                break;
            default:  // Tutorial AR - Heli AR
                stages.Add("Tutorial", configTutorialAR);
                stages.Add("Task-AR", configHeliAR);
                break;
        }

        return stages;
    }

    /// <summary>
    /// Used by the start button in the menu screen. This method serves as a preprocessing stage before the game-starting routine.
    /// </summary>
    /// <param name="configSetID"></param>
    public void LoadConfigSetAndStartGame(int configSetID = 0)
    {
        // Load a configuration set
        stages = LoadConfigSets(configSetID);

        stageButtons = new List<Button>();
        stageKeys = new List<string>();
        foreach (var kv in stages)
        {
            var newButton = Instantiate(stageButtonPrefab, parent: stageProgressView);
            var newButtonText = newButton.GetComponentInChildren<Text>();

            newButton.onClick.AddListener(() => { ApplyConfigurationSync(kv.Key); });
            newButton.interactable = false;  // To test you can make it true
            newButtonText.text = kv.Key;
            newButton.gameObject.SetActive(true);

            stageKeys.Add(kv.Key);
            stageButtons.Add(newButton);
        }
        stageButtons[0].interactable = true;
        stageButtons[stageButtons.Count - 1].GetComponent<Image>().sprite = null;

        StartGame();
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
                _ApplyConfiguration(stages[confName]);

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

        timer = TimeSpan.FromMinutes(timerTotalMin);
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
        // TODO: Add a feature that allows the user to choose whehter to enable networking or not.
        SetSocketIPAddr();
        if (!partnerSocket.IsConnected())
            partnerSocket.SetupRemoteServer();

        // Apply the first game configuration
        currentStageIndex = 0;
        _ApplyConfiguration(stages[stageKeys[currentStageIndex]]);
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
        [Obsolete("This method is for the first version of configuration files")]
        public static OgStageConfig ImportConfigFile(string filename, bool enableAR)
        {
            var configFilePath = CopyResourceToDevice(filename);

            OgStageConfig rt = null;
            using (var reader = new StreamReader(configFilePath))
            {
                // L1: name
                var name = reader.ReadLine();
                // L2: problem desc
                string problem = "";
                string nextProblemLine = reader.ReadLine();
                while (nextProblemLine != null && (!nextProblemLine.Trim().Equals("#")))
                {
                    problem += nextProblemLine + "\n";
                    nextProblemLine = reader.ReadLine();
                }

                // L3: map filename (must be placed in Resources)
                var map = reader.ReadLine();
                CopyResourceToDevice(map);
                // L4: master's script filename (must be placed in Resources)
                var masterScript = reader.ReadLine();
                CopyResourceToDevice(masterScript);
                // L5: slave's script filename (must be placed in Resources)
                var slaveScript = reader.ReadLine();
                CopyResourceToDevice(slaveScript);

                // L6: Will the arrow keys be enabled?
                var isArrowKeyEnabled = Boolean.Parse(reader.ReadLine());
                // L7: Will the developer panel be enabled?
                var isDeveloperPanelEnabled = Boolean.Parse(reader.ReadLine());
                // L8: The set name of avaters to use
                var avatarSetName = reader.ReadLine();
                if (avatarSetName == null) avatarSetName = "Avatar-Default";
                // L9: Sync mode
                var syncModeStr = reader.ReadLine();
                CodeInterpreter.ScriptExecMode syncMode = CodeInterpreter.ScriptExecMode.SYNC_STEP_SWITCHING;
                if (syncModeStr != null)
                    syncMode = (CodeInterpreter.ScriptExecMode)int.Parse(syncModeStr);

                rt = new OgStageConfig(
                    name, problem, map,
                    masterScript, slaveScript,
                    isArrowKeyEnabled, isDeveloperPanelEnabled,
                    avatarSetName, syncMode
                );
            }

            rt.isAREnabled = enableAR;

            return rt;
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

            OgStageConfig rt = null;

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

            rt = new OgStageConfig(
                name, problem, map,
                masterScript, slaveScript,
                isArrowKeyEnabled, isDeveloperPanelEnabled,
                avatarSetName, syncMode
            );
            rt.isAREnabled = enableAR; 

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
    }
}
