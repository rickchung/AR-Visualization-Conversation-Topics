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
    public GameObject arrowKeyPanel;
    public GameObject sttCtrlPanel;
    public GameObject developerPanel;
    public GameObject startScreen, endScreen;
    public Transform stageProgressView;
    public Button stageButtonPrefab;
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

        // Load predefined maps/scripts to the data folder
        var dataFolderPath = Application.persistentDataPath;
        var filesToCopy = new string[] {
            "OgConfig-Tutorial",
                "OgMap-Tutorial1", "OgScript-Tutorial1-M", "OgScript-Tutorial1-S",
            "OgConfig-Puzzle1",
                "OgMap-Puzzle1", "OgScript-Puzzle1-M", "OgScript-Puzzle1-S",
            "OgConfig-Puzzle2",
                "OgMap-Puzzle2", "OgScript-Puzzle2-M", "OgScript-Puzzle2-S",
            "OgConfig-Puzzle3",
                "OgMap-Puzzle3", "OgScript-Puzzle3-M", "OgScript-Puzzle3-S",
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
        stages.Add("Puzzle1", OgStageConfig.ImportConfigFile("OgConfig-Puzzle1"));
        stages.Add("Puzzle2", OgStageConfig.ImportConfigFile("OgConfig-Puzzle2"));
        stages.Add("Puzzle3", OgStageConfig.ImportConfigFile("OgConfig-Puzzle3"));

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
            Debug.LogWarning("Configuration is not found: " + confName);
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

        informationPanel.ShowInfoPanel(true);
    }

    public void StartGame()
    {
        partnerSocket.SetupRemoteServer();
        ApplyConfiguration("Puzzle2");
        startScreen.SetActive(false);
        codeInterpreter.ResetAvatars();
    }

    public void StopGame()
    {
        endScreen.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EnableNextStage()
    {
        currentStageIndex++;
        if (stageButtons != null && currentStageIndex < stageButtons.Count)
        {
            stageButtons[currentStageIndex].interactable = true;
        }
    }

    public class OgStageConfig
    {
        public string name;
        public string problem;
        public string map;
        public string masterScript, slaveScript;
        public bool isArrowKeyEnabled, isDeveloperPanelEnabled;

        public OgStageConfig(
            string name, string problem, string map, string masterScript, string slaveScript,
            bool isArrowKeyEnabled, bool isDeveloperPanelEnabled)
        {
            this.name = name;
            this.problem = problem;
            this.map = map;
            this.masterScript = masterScript.Equals("null") ? null : masterScript;
            this.slaveScript = slaveScript.Equals("null") ? null : slaveScript;
            this.isArrowKeyEnabled = isArrowKeyEnabled;
            this.isDeveloperPanelEnabled = isDeveloperPanelEnabled;
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

                rt = new OgStageConfig(
                    name, problem, map,
                    masterScript, slaveScript,
                    isArrowKeyEnabled, isDeveloperPanelEnabled
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
