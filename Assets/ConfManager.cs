using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
    public GameObject startScreen;
    public Button[] stageButtons;
    private int currentStageIndex;

    [HideInInspector] public bool isSlave;

    private Dictionary<string, OgStageConf> stages;

    private void Start()
    {
        // Prevent the screen from sleeping
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Load predefined maps/scripts to the data folder
        var dataFolderPath = Application.persistentDataPath;
        var filesToCopy = new string[] {
            "OgMap-Tutorial1",
            "OgMap-Tutorial2", "OgScript-Tutorial2-M", "OgScript-Tutorial2-S",
            "OgMap-Puzzle1", "OgScript-Puzzle1-M", "OgScript-Puzzle1-S",
            "OgMap-Puzzle2", "OgScript-Puzzle2-M", "OgScript-Puzzle2-S",
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
                "A predefined map/script is saved to at " + path
            );
        }

        // Open the welcome screen
        startScreen.SetActive(true);

        // Init configurations of stages

        stages = new Dictionary<string, OgStageConf>();

        var p1 = @"Tutorial

Welcome! This is the first tutorial stage of Ogmented. In this tutorial, you will learn the interface of Ogmented including the layout of maps and how to navigate the avatar around.

Your avater is a cube marked as blue on a grid. Please try to move your avatar around and collect the flag in the map.";

        stages.Add("Tutorial1", new OgStageConf(
            name: "Tutorial1",
            problem: p1,
            map: "OgMap-Tutorial1.txt",
            masterScript: null,
            slaveScript: null,
            isArrowKeyEnabled: true,
            isDeveloperPanelEnabled: false
        ));

        stages.Add("Puzzle1", new OgStageConf(
            name: "Puzzle1",
            problem: @"Puzzle 1",
            map: "OgMap-Tutorial2.txt",
            masterScript: "OgScript-Tutorial2-M.txt",
            slaveScript: "OgScript-Tutorial2-S.txt",
            isArrowKeyEnabled: false,
            isDeveloperPanelEnabled: false
        ));

        stages.Add("Puzzle2", new OgStageConf(
            name: "Puzzle2",
            problem: @"Puzzle 2",
            map: "OgMap-Puzzle1.txt",
            masterScript: "OgScript-Puzzle1-M.txt",
            slaveScript: "OgScript-Puzzle1-S.txt",
            isArrowKeyEnabled: false,
            isDeveloperPanelEnabled: false
        ));

        stages.Add("Puzzle3", new OgStageConf(
            name: "Puzzle3",
            problem: @"Puzzle 3",
            map: "OgMap-Puzzle2.txt",
            masterScript: "OgScript-Puzzle2-M.txt",
            slaveScript: "OgScript-Puzzle2-S.txt",
            isArrowKeyEnabled: false,
            isDeveloperPanelEnabled: false
        ));


        // Init stage buttons
        if (stageButtons != null)
        {
            foreach (var b in stageButtons)
            {
                b.interactable = false;
            }
            stageButtons[0].interactable = true;
        }
    }

    public void ApplyConfiguration(string confName)
    {
        if (stages.ContainsKey(confName))
        {
            ApplyConfiguration(stages[confName]);
        }
    }

    public void ApplyConfiguration(OgStageConf conf)
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
        ApplyConfiguration("Tutorial1");
        startScreen.SetActive(false);
    }

    public void EnableNextStage()
    {
        currentStageIndex++;
        if (stageButtons != null && currentStageIndex < stageButtons.Length)
        {
            stageButtons[currentStageIndex].interactable = true;
        }
    }

    public class OgStageConf
    {
        public string name;
        public string problem;
        public string map;
        public string masterScript, slaveScript;
        public bool isArrowKeyEnabled, isDeveloperPanelEnabled;

        public OgStageConf(
            string name, string problem, string map, string masterScript, string slaveScript,
            bool isArrowKeyEnabled, bool isDeveloperPanelEnabled
        )
        {
            this.name = name;
            this.problem = problem;
            this.map = map;
            this.masterScript = masterScript;
            this.slaveScript = slaveScript;
            this.isArrowKeyEnabled = isArrowKeyEnabled;
            this.isDeveloperPanelEnabled = isDeveloperPanelEnabled;
        }

        override public string ToString()
        {
            return name;
        }
    }
}
