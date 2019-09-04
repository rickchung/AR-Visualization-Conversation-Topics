using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConfManager : MonoBehaviour
{
    // The format of a configuration file:
    // L1: Problem (Stage) description
    // L2: The map to use, local script, remote script
    // L3: isArrowKeyEnabled, isDeveloperPanelEnabled

    public InformationPanel informationPanel;
    public GridController gridController;
    public CodeInterpreter codeInterpreter;
    public PartnerSocket partnerSocket;
    public VoiceController voiceController;
    public GameObject arrowKeyPanel;
    public GameObject sttCtrlPanel, sttCtrlPanelAlwaysOn;
    public GameObject developerPanel;
    public GameObject startScreen, endScreen;
    public Transform stageProgressView;
    public Button stageButtonPrefab;

    public AvatarController[] defaultAvatars, helicopterAvatars;

    private List<Button> stageButtons;
    private int currentStageIndex;

    [HideInInspector] public bool isSlave;

    private Dictionary<string, OgStageConfig> stages;

    private void Start()
    {
        // Prevent the screen from sleeping
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // Open the welcome screen
        startScreen.SetActive(true);
        endScreen.SetActive(false);

        // Default layout
        sttCtrlPanel.SetActive(true);
        sttCtrlPanelAlwaysOn.SetActive(false);

        // Load predefined maps/scripts to the data folder
        var dataFolderPath = Application.persistentDataPath;
        var filesToCopy = new string[] {
            "OgConfig-Tutorial",
                "OgMap-Tutorial1", "OgScript-Tutorial1-M", "OgScript-Tutorial1-S",
            "OgConfig-Puzzle3",
                "OgMap-Puzzle3", "OgScript-Puzzle3-M", "OgScript-Puzzle3-S",
            "OgConfig-FlyingHelicopter",
                "OgMap-FlyingHelicopter", "OgScript-FlyingHelicopter-M", "OgScript-FlyingHelicopter-S",
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

        // Init configurations of stages
        stages = new Dictionary<string, OgStageConfig>();
        stages.Add("Tutorial", OgStageConfig.ImportConfigFile("OgConfig-Tutorial"));
        stages.Add("S1-Algorithm", OgStageConfig.ImportConfigFile("OgConfig-Puzzle3"));
        stages.Add("S2-FlyingHelicopter", OgStageConfig.ImportConfigFile("OgConfig-FlyingHelicopter"));

        // Init stage buttons
        stageButtons = new List<Button>();
        foreach (var kv in stages)
        {
            var newButton = Instantiate(stageButtonPrefab, parent: stageProgressView);
            var newButtonText = newButton.GetComponentInChildren<Text>();

            newButton.onClick.AddListener(() => { ApplyConfiguration(kv.Key); });
            newButton.interactable = false;
            newButtonText.text = kv.Key;
            newButton.gameObject.SetActive(true);

            stageButtons.Add(newButton);
        }
        stageButtons[0].interactable = true;
        stageButtons[stageButtons.Count - 1].GetComponent<Image>().sprite = null;
    }

    public void ApplyConfiguration(string confName)
    {
        if (stages.ContainsKey(confName))
        {
            ApplyConfiguration(stages[confName]);
        }
        else
        {
            Debug.LogError("Configuration is not found: " + confName);
        }
    }

    public void ApplyConfiguration(OgStageConfig conf)
    {
        DataLogger.Log(this.gameObject, LogTag.SYSTEM, "Loading a configuration, " + conf);
        informationPanel.ReplaceContent(conf.problem);
        gridController.LoadGridMap(conf.map);
        if (partnerSocket.IsMaster)
        {
            if (conf.masterScript != null)
                codeInterpreter.LoadPredefinedScript(conf.masterScript);
        }
        else
        {
            if (conf.slaveScript != null)
                codeInterpreter.LoadPredefinedScript(conf.slaveScript);
        }
        arrowKeyPanel.SetActive(conf.isArrowKeyEnabled);
        sttCtrlPanel.SetActive(!conf.isArrowKeyEnabled);
        developerPanel.SetActive(conf.isDeveloperPanelEnabled);

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

        informationPanel.ShowInfoPanel(true);
    }

    public void StartGame(string startingStage)
    {
        partnerSocket.SetupRemoteServer();
        ApplyConfiguration(startingStage);
        currentStageIndex = 0;
        startScreen.SetActive(false);
        codeInterpreter.ResetAvatars();

        informationPanel.ShowInstructionScreen(true);
    }

    public void StopGame()
    {
        endScreen.SetActive(true);
    }

    public void RestartGame()
    {
        DataLogger.Log(
            this.gameObject, LogTag.SYSTEM_WARNING,
            "[Admin] Trying to restart the scene..."
        );
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EnableNextStage()
    {
        stageButtons[currentStageIndex].interactable = false;
        currentStageIndex++;
        if (stageButtons != null && currentStageIndex < stageButtons.Count)
        {
            stageButtons[currentStageIndex].interactable = true;
        }
        if (currentStageIndex == stageButtons.Count)
        {
            StopGame();
        }
    }

    public void JumpToStage(string name)
    {
        DataLogger.Log(
            this.gameObject, LogTag.SYSTEM_WARNING,
            "[Admin] Trying to jump to the stage: " + name
        );

        bool successful = false;
        switch (name)
        {
            case "Tutorial":
                ApplyConfiguration("Tutorial");
                currentStageIndex = 0;
                successful = true;
                break;
            case "S1-Algorithm":
                ApplyConfiguration("S1-Algorithm");
                currentStageIndex = 1;
                successful = true;
                break;
            case "S2-FlyingHelicopter":
                ApplyConfiguration("S2-FlyingHelicopter");
                currentStageIndex = 2;
                successful = true;
                break;
        }

        if (successful)
        {
            foreach (var b in stageButtons)
                b.interactable = false;
            stageButtons[currentStageIndex].interactable = true;
        }
    }

    public void ToggleAlwaysOnRecording(bool value)
    {
        // When mic is always on, do not send the final clip but every clip
        voiceController.ToggleMicrophone(value);
        voiceController.sendEveryClip = value;
        voiceController.sendFinalClip = !value;

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
        public string avatarSetName;

        public OgStageConfig(
            string name, string problem, string map, string masterScript, string slaveScript,
            bool isArrowKeyEnabled, bool isDeveloperPanelEnabled, string avatarSetName)
        {
            this.name = name;
            this.problem = problem;
            this.map = map;
            this.masterScript = masterScript.Equals("null") ? null : masterScript;
            this.slaveScript = slaveScript.Equals("null") ? null : slaveScript;
            this.isArrowKeyEnabled = isArrowKeyEnabled;
            this.isDeveloperPanelEnabled = isDeveloperPanelEnabled;
            this.avatarSetName = avatarSetName;
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

                rt = new OgStageConfig(
                    name, problem, map,
                    masterScript, slaveScript,
                    isArrowKeyEnabled, isDeveloperPanelEnabled,
                    avatarSetName
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
