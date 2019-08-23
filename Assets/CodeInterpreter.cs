using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EnhancedUI.EnhancedScroller;

public class CodeInterpreter : MonoBehaviour, IEnhancedScrollerDelegate
{
    public AvatarController avatar, rivalAvatar;
    public TMPro.TextMeshProUGUI scriptTextMesh;
    public EnhancedScroller scriptScroller;
    public CodeObjectCellView codeObjectCellViewPrefab;

    public PartnerSocket partnerSocket;
    public CodeEditor codeEditor;

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
            case "PROGRAM CONTROL FLOW":
            case "THE WHILE LOOP":
            case "THE FOR LOOP":
            case "THE FOREACH LOOP":
            case "THE DO...WHILE LOOP":
                script = new ScriptObject(new List<CodeObjectOneCommand>() {
                    new CodeObjectOneCommand("MOVE", new string[] {"EAST"}),
                    new CodeObjectLoop(
                        "LOOP",
                        new string[] {"3"},
                        new List<CodeObjectOneCommand>() {
                            new CodeObjectOneCommand("MOVE", new string[] {"SOUTH"}),
                            new CodeObjectOneCommand("MOVE", new string[] {"EAST"}),
                        }
                    ),
                    new CodeObjectLoop(
                        "LOOP",
                        new string[] {"2"},
                        new List<CodeObjectOneCommand>() {
                            new CodeObjectOneCommand("MOVE", new string[] {"WEST"}),
                        }
                    ),
                    new CodeObjectLoop(
                        "LOOP",
                        new string[] {"3"},
                        new List<CodeObjectOneCommand>() {
                            new CodeObjectOneCommand("MOVE", new string[] {"NORTH"}),
                            new CodeObjectOneCommand("MOVE", new string[] {"EAST"}),
                        }
                    ),
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
            UpdateCodeViewer();

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
        var procScript = new List<CodeObjectOneCommand>();
        foreach (CodeObjectOneCommand codeObject in script)
        {
            if (codeObject.GetCommand().Equals("LOOP"))
            {
                _ParseLoop((CodeObjectLoop)codeObject, procScript);
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
    /// Parse a loop command. A loop command will be simply rolled out as a sequence of commands.
    /// </summary>
    /// <param name="codeObject">Code object.</param>
    /// <param name="procScript">Proc script.</param>
    private void _ParseLoop(CodeObjectOneCommand codeObject, List<CodeObjectOneCommand> procScript)
    {
        int repeat = int.Parse(codeObject.GetArgs()[0]);

        var codeToRepeat = new List<CodeObjectOneCommand>();
        for (int i = 1; i < codeObject.GetArgs().Length; i++)
        {
            string s = codeObject.GetArgs()[i];
            // Extract command and args
            string[] subCode = s.Split('(');
            string subCommand = subCode[0];
            string[] subArgs = subCode[1].Replace(")", "").Split(',');
            codeToRepeat.Add(new CodeObjectOneCommand(subCommand, subArgs));
        }

        for (int _ = 0; _ < repeat; _++)
            procScript.AddRange(codeToRepeat);
    }

    /// <summary>
    /// Parse a loop command made by the new loop object.
    /// </summary>
    /// <param name="codeObject"></param>
    /// <param name="procScript"></param>
    private void _ParseLoop(CodeObjectLoop codeObject, List<CodeObjectOneCommand> procScript)
    {
        var repeat = codeObject.GetLoopTimes();
        var codeToRepeat = codeObject.GetNestedCommands();
        for (int _ = 0; _ < repeat; _++)
        {
            procScript.AddRange(codeToRepeat);
        }
    }

    /// <summary>
    /// The coroutine for the script to run in the background.
    /// </summary>
    /// <returns>The script.</returns>
    /// <param name="script">Script.</param>
    private IEnumerator _RunScriptCoroutine(List<CodeObjectOneCommand> script)
    {
        int counter = 0;
        while (counter < script.Count)
        {
            CodeObjectOneCommand nextCodeObject = script[counter++];
            RunCommand(nextCodeObject);

            // Send the current command to the remote clients
            partnerSocket.BroadcastAvatarCtrl(nextCodeObject);

            yield return new WaitForSeconds(CMD_RUNNING_DELAY);
        }
    }

    /// <summary>
    /// Run a single command in the given codeObject. This is the place to define additional commands of avatar if needed.
    /// </summary>
    /// <param name="codeObject">Code object.</param>
    public void RunCommand(CodeObjectOneCommand codeObject, bool forRival = false)
    {
        AvatarController runner = (!forRival) ? avatar : rivalAvatar;

        Debug.Log(string.Format("Running Cmd, {0}, {1}", forRival, codeObject));

        string command = codeObject.GetCommand();
        string[] args = codeObject.GetArgs();

        switch (command)
        {
            case "MOVE":
                runner.Move(args[0]);
                break;
        }
    }

    private void ResetVariableList()
    {
        scriptVariables.Clear();
    }

    public void ResetAvatars()
    {
        avatar.ResetPosition();
        rivalAvatar.ResetPosition();
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
        CodeObjectOneCommand codeObject = loadedScript.GetScript()[dataIndex];
        return codeObject.GetLength() * 45f;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Model
        CodeObjectOneCommand codeObject = loadedScript.GetScript()[dataIndex];

        // View
        CodeObjectCellView cellView = scroller.GetCellView(codeObjectCellViewPrefab) as CodeObjectCellView;
        cellView.SetData(codeObject);

        // Onclick event
        cellView.SetCodeModifyingDelegate(ModifyCodeObject);

        return cellView;
    }

    /// <summary>
    /// Used to modify the codeObject when a CodeView is clicked.
    ///
    /// Note: This is a delegate that will be passed into cellviews.
    /// </summary>
    /// <param name="codeObject"></param>
    private void ModifyCodeObject(CodeObjectOneCommand codeObject)
    {
        codeEditor.DispatchEditor(codeObject);
    }

    public void UpdateCodeViewer()
    {
        // Display the script
        scriptTextMesh.SetText(loadedScript.ToString());
        scriptScroller.ReloadData(scrollPositionFactor: 0.0f);
    }

    // ====================

    public void _TestLoadingScript()
    {
        LoadPredefinedScript("PROGRAM CONTROL FLOW", false);
        // LoadPredefinedScript("SEQUENTIAL", false);
    }
}
