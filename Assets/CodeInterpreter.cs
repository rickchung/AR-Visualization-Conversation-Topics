using System;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
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
    public GridController gridController;

    [HideInInspector] public GameObject ViewContainer;
    [HideInInspector] public GameObject codingPanel;

    private const float CMD_RUNNING_DELAY = 0.5f;
    public const string CTRL_CLOSE = "close";

    private ScriptObject loadedScript;
    private Dictionary<string, float> scriptVariables;
    private bool isScriptRunning;
    private static string dataFolderPath;


    void Start()
    {
        scriptVariables = new Dictionary<string, float>();
        CloseTopicViewAndBroadcast();

        // Init the script scroller
        scriptScroller.Delegate = this;
        scriptScroller.ReloadData(scrollPositionFactor: 0.0f);

        // Load predefined scripts to the data folder
        dataFolderPath = Application.persistentDataPath;
        string[] predefinedMaps = { "Default1.OgScript", "Default2.OgScript" };
        foreach (var s in predefinedMaps)
        {
            var path = Path.Combine(dataFolderPath, s) + ".txt";
            var txt = (TextAsset)Resources.Load(s, typeof(TextAsset));
            using (var writer = new StreamWriter(path))
            {
                writer.Write(txt.text);
            }
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM,
                "A predefined script is saved as " + path
            );
        }
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

    // ==================== Interfaces of script operations ====================

    /// <summary>
    /// Load the predefined script template specified by <paramref name="scriptName"/>.
    /// </summary>
    /// <param name="scriptName">Script name.</param>
    public void LoadPredefinedScript(string scriptName, bool broadcast = false)
    {
        ScriptObject script = ImportScriptObject(scriptName);
        if ((loadedScript = script) != null)
        {
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM,
                "A predefined script is imported " + scriptName
            );
        }

        // Display the script
        UpdateCodeViewer();
        // Enable the viewer
        SetActiveTopicView(true, broadcast: false);
        // Enable the viewer on the remote device
        if (broadcast) partnerSocket.BroadcastTopicCtrl(scriptName);
    }

    // ==================== File IO for Interpreter ====================

    private static void ExportScriptObject(ScriptObject script, string scriptName)
    {
        var scriptString = script.ToString(richtext: false);
        var path = Path.Combine(dataFolderPath, scriptName);
        using (var writer = new StreamWriter(path, false))
        {
            writer.WriteLine(scriptString);
            writer.Close();
        }
    }

    private static ScriptObject ImportScriptObject(string scriptName)
    {
        var path = Path.Combine(dataFolderPath, scriptName);
        ScriptObject rt = null;
        try
        {
            using (var reader = new StreamReader(path))
            {
                rt = ProcessCode(reader);
            }
        }
        catch (FileNotFoundException)
        {
            DataLogger.Log(LogTag.SCRIPT_ERROR, "File to import is not found: " + scriptName);
        }
        return rt;
    }

    private static ScriptObject ProcessCode(StreamReader reader)
    {
        var codeList = new List<CodeObjectOneCommand>();
        while (reader.Peek() > 0)
        {
            var line = reader.ReadLine();
            var code = _ProcessOneLine(line, reader);
            if (code != null)
                codeList.Add(code);
        }
        return new ScriptObject(codeList); ;
    }

    private static Regex regexMove = new Regex(@"MOVE \((?<param>\w+)\);");
    private static Regex regexLoopStart = new Regex(@"LOOP REPEAT {");
    private static Regex regexLoopEnd = new Regex(@"} (?<times>\d) Times;");
    private static CodeObjectOneCommand _ProcessOneLine(string oneLine, StreamReader reader)
    {
        // Try to match a move command
        var matchMove = regexMove.Matches(oneLine);
        if (matchMove.Count > 0)
        {
            return new CodeObjectOneCommand(
                "MOVE", new string[] { matchMove[0].Groups["param"].Value }
            );
        }
        // Try to match a loop command
        var matchLoopStart = regexLoopStart.Matches(oneLine);
        if (matchLoopStart.Count > 0)
        {
            var loopNestedCode = new List<CodeObjectOneCommand>();
            while (reader.Peek() > 0)
            {
                var aNestedLine = reader.ReadLine();
                var nestedCode = _ProcessOneLine(aNestedLine, reader);
                if (nestedCode != null)
                {
                    loopNestedCode.Add(nestedCode);
                }

                var matchLoopEnd = regexLoopEnd.Matches(aNestedLine);
                if (matchLoopEnd.Count > 0)
                {
                    return new CodeObjectLoop(
                        "LOOP",
                        new string[] { matchLoopEnd[0].Groups["times"].Value },
                        loopNestedCode
                    ); ;
                }
            }
        }

        return null;
    }

    // ==================== Code Interpreter ====================

    /// <summary>
    /// Run the loaded script.
    /// </summary>
    public void RunLoadedScript()
    {
        if (loadedScript != null)
        {
            if (isScriptRunning == false)
            {
                DataLogger.Log(
                    this.gameObject, LogTag.SCRIPT,
                    "Start running the loaded script"
                );

                ResetAvatars();
                _RunScript(loadedScript);
            }
            else
            {
                DataLogger.Log(
                    this.gameObject, LogTag.SCRIPT_WARNING,
                    "A script is running but trying to re-run it"
                );
            }
        }
        else
        {
            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT_ERROR,
                "No loaded script found."
            );
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
        StartCoroutine("_RunScriptCoroutine", procScript);
        isScriptRunning = true;

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
        var codeToRepeat = codeObject.GetNestedCommands(ignoreDisabled: true);
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
            RunCodeObject(nextCodeObject);
            yield return new WaitForSeconds(CMD_RUNNING_DELAY);
        }
        isScriptRunning = false;

        DataLogger.Log(
            this.gameObject, LogTag.SCRIPT,
            "The execution of a script has finished."
        );
    }

    private void RunCodeObject(CodeObjectOneCommand co)
    {
        RunCommand(co);
        // Send the current command to the remote clients
        partnerSocket.BroadcastAvatarCtrl(co);
    }

    /// <summary>
    /// Run a single command in the given codeObject. This is the place to define additional commands of avatar if needed.
    /// </summary>
    /// <param name="codeObject">Code object.</param>
    public void RunCommand(CodeObjectOneCommand codeObject, bool forRival = false)
    {
        AvatarController runner = (!forRival) ? avatar : rivalAvatar;

        if (!runner.IsDead)
        {
            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT,
                string.Format("Running Cmd, {0}, {1}", forRival, codeObject)
            );

            string command = codeObject.GetCommand();
            string[] args = codeObject.GetArgs();

            switch (command)
            {
                case "MOVE":
                    runner.Move(args[0]);
                    break;
            }
        }
        else
        {
            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT,
                string.Format("Avatar is dead., {0}, {1}", forRival, codeObject)
            );
        }
    }

    private void ResetVariableList()
    {
        scriptVariables.Clear();
    }

    public void StopRunningScript()
    {
        if (isScriptRunning)
        {
            StopCoroutine("_RunScriptCoroutine");
            isScriptRunning = false;

            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT,
                "Script is interrupted."
            );
        }
    }

    /// <summary>
    /// Reset the positions of avaters. When the forRival flag is true, the method resets the position of the rival avater. This flag is reserved for the remote control from the connected device.
    /// </summary>
    /// <param name="forRival"></param>
    public void ResetAvatars(bool forRival = false)
    {
        if (!forRival)
        {
            StopRunningScript();
            avatar.ResetPosition();
            // Broadcast your reset message
            CodeObjectOneCommand resetCmd = new CodeObjectOneCommand("RESET_POS", new string[] { });
            partnerSocket.BroadcastAvatarCtrl(resetCmd);

            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT,
                "The avater is reset."
            );
        }
        else
        {
            rivalAvatar.ResetPosition();
        }

        // Reset avatars' states
        avatar.IsDead = false;
        rivalAvatar.IsDead = false;
        // Reset the map
        gridController.ResetMap();
    }

    public void SwitchCodeView()
    {
        // var tmp1 = scriptTextMesh.transform.parent.gameObject;
        // tmp1.SetActive(!tmp1.activeSelf);
        var tmp2 = scriptScroller.gameObject;
        tmp2.SetActive(!tmp2.activeSelf);
    }

    // ==================== Implementation of EnhancedScroller interfaces ====================

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
        if (loadedScript != null)
            scriptTextMesh.SetText(loadedScript.ToString());
        scriptScroller.ReloadData(scrollPositionFactor: 0.0f);
    }

    // ==================== Testing Functions

    public void _TestLoadingScript(string scriptName)
    {
        LoadPredefinedScript(scriptName, broadcast: false);
    }

    public void _TestAnything(int dir)
    {
        switch ((GridController.Direction)dir)
        {
            case GridController.Direction.NORTH:
                RunCodeObject(new CodeObjectOneCommand("MOVE", new string[] { "NORTH" }));
                break;
            case GridController.Direction.SOUTH:
                RunCodeObject(new CodeObjectOneCommand("MOVE", new string[] { "SOUTH" }));
                break;
            case GridController.Direction.EAST:
                RunCodeObject(new CodeObjectOneCommand("MOVE", new string[] { "EAST" }));
                break;
            case GridController.Direction.WEST:
                RunCodeObject(new CodeObjectOneCommand("MOVE", new string[] { "WEST" }));
                break;

        }
    }
}
