using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EnhancedUI.EnhancedScroller;

public class CodeInterpreter : MonoBehaviour, IEnhancedScrollerDelegate
{
    public AvatarController avatar;
    public TMPro.TextMeshProUGUI scriptTextMesh;
    public EnhancedScroller scriptScroller;
    public CodeObjectCellView codeObjectCellViewPrefab;

    public PartnerSocket partnerSocket;
    public GameObject ViewContainer;
    public GameObject codingPanel;

    private const float CMD_RUNNING_DELAY = 0.5f;
    public const string CTRL_CLOSE = "close";

    private ScriptObject loadedScript;
    private Dictionary<string, float> scriptVariables;

    void Start()
    {
        scriptVariables = new Dictionary<string, float>();
        CloseTopicViewAndBroadcast();

        scriptScroller.Delegate = this;
        scriptScroller.ReloadData(scrollPositionFactor: 0.0f);
    }

    public UnityAction GetTopicButtonEvent(string topic)
    {
        UnityAction action = () =>
        {
            LoadPredefinedScript(topic, broadcast: true);
        };
        return action;
    }

    /// <summary>
    /// Enable or disable the avatar visualization
    /// </summary>
    /// <param name="activated"></param>
    /// <param name="broadcast"></param>
    public void SetActiveTopicView(bool activated, bool broadcast = false)
    {
        if (ViewContainer) ViewContainer.SetActive(activated);
        if (codingPanel) codingPanel.SetActive(activated);
        if (broadcast) partnerSocket.BroadcastTopicCtrl(CTRL_CLOSE);
    }

    public void CloseTopicViewAndBroadcast()
    {
        SetActiveTopicView(false, true);
    }

    // ====================
    // Interfaces of script operations

    /// <summary>
    /// Load the predefined script template specified by <paramref name="scriptName"/>.
    /// </summary>
    /// <param name="scriptName">Script name.</param>
    public void LoadPredefinedScript(string scriptName, bool broadcast = false)
    {
        ScriptObject script = null;
        switch (scriptName)
        {
            case "SEQUENTIAL":
            case "SEQUENTIAL LOGICS":
            case "SEQUENCES":
                script = new ScriptObject(new List<CodeObject>() {
                    new CodeObject("MOVE", new string[] {"SOUTH"}),
                    new CodeObject("MOVE", new string[] {"SOUTH"}),
                    new CodeObject("MOVE", new string[] {"EAST"}),
                    new CodeObject("MOVE", new string[] {"EAST"}),
                });
                break;
            case "PROGRAM CONTROL FLOW":
            case "THE WHILE LOOP":
            case "THE FOR LOOP":
            case "THE FOREACH LOOP":
            case "THE DO...WHILE LOOP":
                script = new ScriptObject(new List<CodeObject>() {
                    new CodeObject("LOOP", new string[] {
                        "3", "MOVE(SOUTH)", "MOVE(EAST)"
                    }),
                    new CodeObject("LOOP", new string[] {
                        "2", "MOVE(WEST)"
                    }),
                    new CodeObject("LOOP", new string[] {
                        "2", "MOVE(NORTH)", "MOVE(EAST)"
                    }),
                });
                break;
            default:
                Debug.LogError("The script " + scriptName + " does not exist.");
                break;
        }

        loadedScript = script;
        if (loadedScript != null)
        {
            // Display the script
            scriptTextMesh.SetText(loadedScript.ToString());
            scriptScroller.ReloadData(scrollPositionFactor: 0.0f);

            // Enable the viewer
            SetActiveTopicView(true, broadcast: false);
            // Enable the viewer on the remote device
            if (broadcast) partnerSocket.BroadcastTopicCtrl(scriptName);
        }
    }

    // ====================
    // Internal code compiler

    /// <summary>
    /// Run the loaded script.
    /// </summary>
    public void RunLoadedScript()
    {
        if (loadedScript != null)
        {
            avatar.ResetPosition();
            _RunScript(loadedScript);
        }
    }

    /// <summary>
    /// Run the given ScriptObject. This method also preprocesses the script
    /// by transforming loops into code sequences.
    /// </summary>
    /// <param name="script">Script.</param>
    private void _RunScript(ScriptObject script)
    {
        // Preprocess the script
        var procScript = new List<CodeObject>();
        foreach (CodeObject codeObject in script)
        {
            if (codeObject.command.Equals("LOOP"))
            {
                _ParseLoop(codeObject, procScript);
            }
            else
            {
                procScript.Add(codeObject);
            }
        }

        // Run the script
        ResetVariableList();
        StartCoroutine(_RunScriptCoroutine(procScript));
    }

    /// <summary>
    /// Parse a loop command. A loop command will be simply rolled out as a
    /// sequence of commands.
    /// </summary>
    /// <param name="codeObject">Code object.</param>
    /// <param name="procScript">Proc script.</param>
    private void _ParseLoop(CodeObject codeObject, List<CodeObject> procScript)
    {
        int repeat = int.Parse(codeObject.args[0]);
        var codeToRepeat = new List<CodeObject>();
        for (int i = 1; i < codeObject.args.Length; i++)
        {
            string s = codeObject.args[i];
            // Extract command and args
            string[] subCode = s.Split('(');
            string subCommand = subCode[0];
            string[] subArgs = subCode[1].Replace(")", "").Split(',');
            codeToRepeat.Add(new CodeObject(subCommand, subArgs));
        }

        for (int _ = 0; _ < repeat; _++)
            procScript.AddRange(codeToRepeat);
    }

    /// <summary>
    /// The coroutine for the script to run in the background.
    /// </summary>
    /// <returns>The script.</returns>
    /// <param name="script">Script.</param>
    private IEnumerator _RunScriptCoroutine(List<CodeObject> script)
    {
        int counter = 0;
        while (counter < script.Count)
        {
            CodeObject nextCodeObject = script[counter++];
            RunCommand(nextCodeObject);
            partnerSocket.BroadcastAvatarCtrl(nextCodeObject);
            yield return new WaitForSeconds(CMD_RUNNING_DELAY);
        }
    }

    /// <summary>
    /// Run a single command in the given codeObject.
    /// </summary>
    /// <param name="codeObject">Code object.</param>
    public void RunCommand(CodeObject codeObject)
    {
        Debug.Log("Running: " + codeObject);

        string command = codeObject.command;
        string[] args = codeObject.args;

        switch (command)
        {
            case "MOVE":
                avatar.Move(args[0]);
                break;
        }
    }

    private void ResetVariableList()
    {
        scriptVariables.Clear();
    }

    // ====================
    // Implementation of EnhancedScroller interfaces

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        if (loadedScript != null)
            return loadedScript.GetScript().Count;
        else
            return 0;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        // Model
        CodeObject codeObject = loadedScript.GetScript()[dataIndex];
        return codeObject.GetArgs().Length * 50f;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Model
        CodeObject codeObject = loadedScript.GetScript()[dataIndex];

        // View
        CodeObjectCellView cellView = scroller.GetCellView(codeObjectCellViewPrefab) as CodeObjectCellView;
        cellView.SetData(codeObject);

        return cellView;
    }

    // ====================

    public void _TestLoadingScript()
    {
        LoadPredefinedScript("PROGRAM CONTROL FLOW", false);
    }
}
