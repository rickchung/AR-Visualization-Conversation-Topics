using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CodeInterpreter : MonoBehaviour
{
    public AvatarController mAvartar;
    public TMPro.TextMeshPro mScriptTextMesh;
    public GameObject mExampleContainer;
    public GameObject mControlPanel;
    public PartnerSocket mPartnerSocket;

    private const float CMD_RUNNING_DELAY = 0.5f;
    public const string CTRL_CLOSE = "close";

    private ScriptObject loadedScript;

    void Start()
    {
        CloseTopicViewAndBroadcast();
    }


    public UnityAction GetTopicButtonEvent(string topic)
    {
        UnityAction action = () =>
        {
            LoadPredefinedScript(topic, broadcast: true);
        };
        return action;
    }

    public void SetActiveTopicView(bool activated, bool broadcast = false)
    {
        if (mExampleContainer) mExampleContainer.SetActive(activated);
        if (mControlPanel) mControlPanel.SetActive(activated);
        if (broadcast) mPartnerSocket.BroadcastTopicCtrl(CTRL_CLOSE);
    }

    public void CloseTopicViewAndBroadcast()
    {
        SetActiveTopicView(false, true);
    }


    // ======================================================================

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
            mScriptTextMesh.SetText(loadedScript.ToString());
            SetActiveTopicView(true, broadcast: false);
            if (broadcast) mPartnerSocket.BroadcastTopicCtrl(scriptName);
        }
    }

    public void _TestLoadingScript()
    {
        LoadPredefinedScript("PROGRAM CONTROL FLOW", true);
    }

    // ======================================================================

    /// <summary>
    /// Run the loaded script.
    /// </summary>
    public void RunLoadedScript()
    {
        if (loadedScript != null)
            _RunScript(loadedScript);
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
            mPartnerSocket.BroadcastAvatarCtrl(nextCodeObject);
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
                mAvartar.Move(args[0]);
                break;
        }
    }
}
