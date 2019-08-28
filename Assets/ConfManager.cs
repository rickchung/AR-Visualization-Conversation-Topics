using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public bool isSlave;

    private Dictionary<string, OgStageConf> stages;

    private void Start()
    {
        startScreen.SetActive(true);

        stages = new Dictionary<string, OgStageConf>();
        stages.Add("Tutorial1", new OgStageConf(
            name: "Tutorial1",
            problem:
@"Tutorial-1

Welcome! This is the first tutorial stage of Ogmented. In this tutorial, you will learn the interface of Ogmented including the layout of maps and how to navigate the avatar around.

Your avater is a cube marked as blue on a grid. Please try to move your avatar around and collect the flag in the map.",
            map: "Default1.OgMap.txt",
            masterScript: null,
            slaveScript: null,
            isArrowKeyEnabled: true,
            isDeveloperPanelEnabled: false
        ));

        stages.Add("Tutorial2", new OgStageConf(
            name: "Tutorial2",
            problem:
@"Tutorial-2

In the second tutorial, you will learn how to control the avatar by running a script.

Just like controlling by arrow keys, your avatar can follow a script of commands which tell it how to move around.

A script is shown on the right hand side of screen. Please try to pree run and see what your avatar will do.

Once you are familiar with the script, try to tap a command in the script and modify its content. Your goal is to correct the script and make your avatar collect all flags on the screen.",
            map: "Default2.OgMap.txt",
            masterScript: "Default1.OgScript.txt",
            slaveScript: null,
            isArrowKeyEnabled: false,
            isDeveloperPanelEnabled: false
        ));
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
