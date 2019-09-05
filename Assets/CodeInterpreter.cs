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
    private bool isScriptRunning;
    private static string dataFolderPath;

    void Start()
    {
        dataFolderPath = Application.persistentDataPath;

        CloseTopicViewAndBroadcast();

        // Init the script scroller
        scriptScroller.Delegate = this;
        scriptScroller.ReloadData(scrollPositionFactor: 0.0f);

        if (avatar != rivalAvatar)
        {
            rivalAvatar.IsRival = true;
        }
    }


    // ========== Misc Methods ==========

    public void SetAvatarGameObjects(AvatarController player1, AvatarController rival)
    {
        // Disable the existing avatars if existing
        if (avatar != null)
            avatar.gameObject.SetActive(false);
        if (rivalAvatar != null)
            rivalAvatar.gameObject.SetActive(false);

        // Replace the avatars
        avatar = player1;
        rivalAvatar = rival;
        if (avatar != rivalAvatar)
        {
            rivalAvatar.IsRival = true;
        }

        // Enable avatars
        avatar.gameObject.SetActive(true);
        rivalAvatar.gameObject.SetActive(true);
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

    public void SwitchCodeView()
    {
        // var tmp1 = scriptTextMesh.transform.parent.gameObject;
        // tmp1.SetActive(!tmp1.activeSelf);
        var tmp2 = scriptScroller.gameObject;
        tmp2.SetActive(!tmp2.activeSelf);
    }


    // ========== Script Interpreter ==========

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
                "A predefined script is imported " + scriptName + ": " + script
            );
        }

        // Display the script
        UpdateCodeViewer();
        // Enable the viewer
        SetActiveTopicView(true, broadcast: false);
        // Enable the viewer on the remote device
        if (broadcast) partnerSocket.BroadcastTopicCtrl(scriptName);
    }

    /// <summary>
    /// Run the loaded script.
    /// </summary>
    public void RunLoadedScript()
    {
        // TODO: Make this function synchronized between local and remote devices.

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
        StartCoroutine("_RunScriptCoroutine", script);
        isScriptRunning = true;
    }

    /// <summary>
    /// Execute a ScripObject
    /// </summary>
    /// <param name="scriptObject"></param>
    /// <returns></returns>
    private IEnumerator _RunScriptCoroutine(ScriptObject scriptObject)
    {
        var script = scriptObject.GetScript();

        int counter = 0;
        while (counter < script.Count)
        {
            CodeObjectOneCommand nextCodeObject = script[counter++];
            nextCodeObject.IsRunning = true;

            if (nextCodeObject.GetCommand().Equals("LOOP"))
            {
                var numRepeats = ((CodeObjectLoop)nextCodeObject).GetLoopTimes();
                var codeToRepeat = ((CodeObjectLoop)nextCodeObject).GetNestedCommands(ignoreDisabled: true);
                for (int _ = 0; _ < numRepeats; _++)
                {
                    foreach (var c in codeToRepeat)
                    {
                        RunCommand(c);
                        yield return new WaitForSeconds(CMD_RUNNING_DELAY);
                    }
                }
            }
            else if (nextCodeObject.GetCommand().Equals("WAIT"))
            {
                for (int i = 0; i < 2 * int.Parse(nextCodeObject.GetArgs()[0]); i++)
                {
                    RunCommand(nextCodeObject);
                    yield return new WaitForSeconds(CMD_RUNNING_DELAY);
                }
            }
            else
            {
                RunCommand(nextCodeObject);
                yield return new WaitForSeconds(CMD_RUNNING_DELAY);
            }

            nextCodeObject.IsRunning = false;
        }
        isScriptRunning = false;

        DataLogger.Log(
            this.gameObject, LogTag.SCRIPT,
            "The execution of a script has finished."
        );
    }

    /// <summary>
    /// Invoke the parser on avatars to run the passed code object. This method will also be invoked through the network.
    /// </summary>
    /// <param name="codeObject">Code object to execute</param>
    /// <param name="fromRemote">Whether the codeObjec is for my rival's avatar</param>
    public void RunCommand(CodeObjectOneCommand codeObject, bool fromRemote = false)
    {
        // Choose the runner according to the source of input commands
        AvatarController runner = (!fromRemote) ? avatar : rivalAvatar;

        if (!runner.IsDead)
        {
            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT,
                string.Format("Running Cmd, fromRemote={0}, {1}", fromRemote, codeObject)
            );
            string command = codeObject.GetCommand();
            string[] args = codeObject.GetArgs();
            runner.ParseCommand(command, args);
        }
        else
        {
            DataLogger.Log(
                this.gameObject, LogTag.SCRIPT,
                string.Format("Avatar is dead., {0}, {1}", fromRemote, codeObject)
            );
        }

        // If this method is invoked locally, it will broadcast the command to remote devices. Note: When a message arrives at the remote device, it will also invoke this method but with the flag fromRemote = true, to prevent a broadcast storm.
        if (fromRemote == false)
        {
            partnerSocket.BroadcastAvatarCtrl(codeObject);
        }
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
    /// Reset the positions of avaters. When the fromRemote flag is true, the method resets the position of the rival avater. This flag is also designed for the remote control from the connected device.
    /// </summary>
    /// <param name="fromRemote"></param>
    public void ResetAvatars(bool fromRemote = false)
    {
        if (!fromRemote)
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
            if (rivalAvatar)
                rivalAvatar.ResetPosition();
        }

        // TODO: Reset avatars' states (this code does not make sense)
        if (avatar)
            avatar.IsDead = false;
        if (rivalAvatar)
            rivalAvatar.IsDead = false;

        // Reset the map
        gridController.ResetMap();
    }


    // ========== Implementation of EnhancedScroller interfaces ==========

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
        var size = 45f + (codeObject.GetLength() / 25) * 30f;

        if (codeObject.GetCommand().Equals("LOOP"))
        {
            size = (codeObject.GetArgs().Length + 2) * 45f;
        }

        return size;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Model
        CodeObjectOneCommand codeObject = loadedScript.GetScript()[dataIndex];

        // View
        CodeObjectCellView cellView = scroller.GetCellView(codeObjectCellViewPrefab) as CodeObjectCellView;
        cellView.SetData(codeObject, dataIndex);
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

    public void UpdateCodeViewer(bool scrollToTop = true)
    {
        // Display the script
        if (loadedScript != null)
            scriptTextMesh.SetText(loadedScript.ToString());

        var scrollPos = scriptScroller.NormalizedScrollPosition;
        if (scrollToTop)
            scrollPos = 0.0f;
        scriptScroller.ReloadData(scrollPositionFactor: scrollPos);
    }


    // ========== File IO for Interpreter ==========

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

    private static
        Regex regexLoopStart = new Regex(@"LOOP REPEAT {"),
        regexLoopEnd = new Regex(@"} (?<times>\d) Times;");
    private static Regex[] regexSingleCmdNoParam = {
        new Regex(@"(?<cmd>START_ENGINE) \(\)"),
        new Regex(@"(?<cmd>STOP_ENGINE) \(\)"),
        new Regex(@"(?<cmd>CLIMB_UP) \(\)"),
        new Regex(@"(?<cmd>FALL_DOWN) \(\)"),
        new Regex(@"(?<cmd>MOVE_FORWARD) \(\);"),
        new Regex(@"(?<cmd>MOVE_BACKWARD) \(\);"),
        new Regex(@"(?<cmd>TURN_RIGHT) \(\)"),
        new Regex(@"(?<cmd>TURN_LEFT) \(\)"),
    };
    private static Regex[] regexSingleCmdOneParam = {
        new Regex(@"(?<cmd>WAIT) \((?<param>\d+)\);"),
        new Regex(@"(?<cmd>MOVE) \((?<param>\w+)\);"),
        new Regex(@"(?<cmd>SET_TOP_PWR_OUTPUT) \((?<param>[\d\.]+)\)"),
        new Regex(@"(?<cmd>SET_TAIL_PWR_OUTPUT) \((?<param>[\d\.]+)\)"),
        new Regex(@"(?<cmd>SET_TOP_BRAKE_OUTPUT) \((?<param>[\d\.]+)\)"),
        new Regex(@"(?<cmd>SET_TAIL_BRAKE_OUTPUT) \((?<param>[\d\.]+)\)"),
    };

    private static CodeObjectOneCommand _MatchRegexSingleCommand(string line)
    {
        // No param
        foreach (var r in regexSingleCmdNoParam)
        {
            var matched = r.Matches(line);
            if (matched != null && matched.Count > 0)
            {
                return new CodeObjectOneCommand(
                    matched[0].Groups["cmd"].Value, new string[] { }
                );
            }
        }

        // One param
        foreach (var r in regexSingleCmdOneParam)
        {
            var matched = r.Matches(line);
            if (matched != null && matched.Count > 0)
            {
                return new CodeObjectOneCommand(
                    matched[0].Groups["cmd"].Value,
                    new string[] { matched[0].Groups["param"].Value }
                );
            }
        }
        return null;
    }
    private static CodeObjectOneCommand _ProcessOneLine(string oneLine, StreamReader reader)
    {
        // Try to match a command
        var singleCmd = _MatchRegexSingleCommand(oneLine);
        if (singleCmd != null) return singleCmd;

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

}
