﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ConfManager : MonoBehaviour
{
    // The format of a configuration file:
    // L1: Problem (Stage) description
    // L2: The map to use, local script, remote script
    // L3: isArrowKeyEnabled, isDeveloperPanelEnabled

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

    // ==========

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

    // ==========

    /// <summary>
    /// Load configuration files according to a pre-defined rule. This is the only place you need to change when adding new game stages.
    /// </summary>
    /// <param name="configSetID"></param>
    /// <returns></returns>
    private Dictionary<string, OgStageConfig> LoadConfigSets(int configSetID = 0)
    {
        // Load predefined maps/scripts to the data folder
        var dataFolderPath = Application.persistentDataPath;

        // This is just a list of files to import during the initialization of game.
        // They will be copied from the package to the storage on the device.
        // To add a config into the game, see the variable "stages" in LoadConfigSetAndStartGame below.
        var filesToCopy = new string[] {

            // Currently in use

            "OgConfig-FlyingHelicopter-Tutorial",
                "OgMap-FlyingHelicopter-v0",
                "OgScript-FlyingHelicopter-M-v0",
                "OgScript-FlyingHelicopter-S-v0",

            "OgConfig-FlyingHelicopter",
                "OgMap-FlyingHelicopter-v1",
                "OgScript-FlyingHelicopter-M-v1",
                "OgScript-FlyingHelicopter-S-v1",

            // Old Configurations
            // "OgConfig-Tutorial",
            //     "OgMap-Tutorial1",
            //     "OgScript-Tutorial1-M",
            //     "OgScript-Tutorial1-S",
            // "OgConfig-Puzzle3",
            //     "OgMap-Puzzle3",
            //     "OgScript-Puzzle3-M",
            //     "OgScript-Puzzle3-S",
            // "OgConfig-FlyingHelicopter-Adv",
            //     "OgMap-FlyingHelicopter",
            //     "OgScript-FlyingHelicopter-M",
            //     "OgScript-FlyingHelicopter-S",

        };
        foreach (var s in filesToCopy)
        {
            var path = Path.Combine(dataFolderPath, s) + ".txt";
            var txt = (TextAsset)Resources.Load(s, typeof(TextAsset));
            using (var writer = new StreamWriter(path))
            {
                writer.Write(txt.text);
            }
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM,
                "A predefined map/script is COPIED to " + path
            );
        }

        // User-defined Rules (You should modeify this section when you want to add new stages)

        var configTutorialAR = OgStageConfig.ImportConfigFile("OgConfig-FlyingHelicopter-Tutorial");
        configTutorialAR.isAREnabled = true;
        var configTutorialNonAR = OgStageConfig.ImportConfigFile("OgConfig-FlyingHelicopter-Tutorial");
        configTutorialNonAR.isAREnabled = false;
        var configHeliAR = OgStageConfig.ImportConfigFile("OgConfig-FlyingHelicopter");
        configHeliAR.isAREnabled = true;
        var configHeliNonAR = OgStageConfig.ImportConfigFile("OgConfig-FlyingHelicopter");
        configHeliNonAR.isAREnabled = false;

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
    /// Used by the start button in the menu screen.
    /// </summary>
    /// <param name="configSetID"></param>
    public void LoadConfigSetAndStartGame(int configSetID = 0)
    {
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


    // ==========

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

    // ==========

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

    public void StartGame()
    {
        SetSocketIPAddr();
        if (!partnerSocket.IsConnected())
            partnerSocket.SetupRemoteServer();

        currentStageIndex = 0;
        _ApplyConfiguration(stages[stageKeys[currentStageIndex]]);
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

    // ==========


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

    public class OgStageConfig
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

        public static OgStageConfig ImportConfigFile(string filename)
        {
            var dataDirPath = Application.persistentDataPath;
            var filePath = Path.Combine(dataDirPath, filename) + ".txt";
            OgStageConfig rt = null;

            using (var reader = new StreamReader(filePath))
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
                // L4: master's script filename (must be placed in Resources)
                var masterScript = reader.ReadLine();
                // L5: slave's script filename (must be placed in Resources)
                var slaveScript = reader.ReadLine();
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

            return rt;
        }

        override public string ToString()
        {
            return name;
        }
    }
}
