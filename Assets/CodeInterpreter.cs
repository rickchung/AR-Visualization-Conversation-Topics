using System;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    public Button runButton;

    private ScriptObject loadedScript;
    private ScriptExecMode execMode;
    private bool isScriptRunning;
    private bool isScriptPaused;
    public bool isRemoteFinished;
    private static string dataFolderPath;
    public enum ScriptExecMode { ASYNC, SYNC_STEP_SWITCHING, SYNC_CMD_SWITCHING };
    public ScriptExecMode ExecMode
    {
        get
        {
            return execMode;
        }

        set
        {
            execMode = value;
        }
    }
    public bool IsScriptPaused
    {
        get
        {
            return isScriptPaused;
        }
        set
        {
            isScriptPaused = value;
        }
    }

    public bool IsScriptRunning
    {
        get
        {
            return isScriptRunning;
        }
        set
        {
            isScriptRunning = value;
        }
    }


    // ========= Predefined Control Signals ==========
    private const float CMD_RUNNING_DELAY = 0.5f;
    public const string CTRL_SIGNAL_RESET = "RESET_POS";
    public const string CTRL_CLOSE_TOPIC_VIEW = "CLOSE_TOPIC_VIEW";
    public const string CTRL_SEM_LOCK = "SEM_LOCK";
    public const string CTRL_SEM_UNLOCK = "SEM_UNLOCK";
    public const string CTRL_SEM_FINISH = "SEM_FINISH";
    public const string CTRL_SEM_RUNSCRIPT = "SEM_RUNSCRIPT";
    private const int CTRL_MAX_WAIT_TIME = 3;
    private float semTimeElapsed;


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

        runButton.onClick.AddListener(RunLoadedScriptSync);
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
        if (broadcast) partnerSocket.BroadcastTopicCtrl(CTRL_CLOSE_TOPIC_VIEW);
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

    public void RunLoadedScriptSync()
    {
        RunLoadedScript();
        partnerSocket.BroadcastAvatarCtrl(
            new CodeObjectOneCommand(CTRL_SEM_RUNSCRIPT, new string[] { })
        );
    }

    /// <summary>
    /// Run the loaded script.
    /// </summary>
    public void RunLoadedScript()
    {
        if (loadedScript != null)
        {
            if (IsScriptRunning == false)
            {
                DataLogger.Log(
                    this.gameObject, LogTag.SCRIPT,
                    "Start running the loaded script"
                );

                runButton.interactable = false;
                _PreExecProcess(loadedScript);
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
    /// Run the given ScriptObject by starting a coroutine.
    /// </summary>
    /// <param name="script">Script.</param>
    private void _PreExecProcess(ScriptObject script)
    {
        // Init sync control
        switch (execMode)
        {
            // Master runs first. Slave waits first.
            case ScriptExecMode.SYNC_STEP_SWITCHING:
                if (partnerSocket.IsMaster)
                {
                    IsScriptPaused = false;
                }
                else
                {
                    IsScriptPaused = true;
                }
                break;
        }

        StartCoroutine("_RunScriptCoroutine", script);
    }

    /// <summary>
    /// A coroutine for executing ScripObject
    /// </summary>
    /// <param name="scriptObject"></param>
    /// <returns></returns>
    private IEnumerator _RunScriptCoroutine(ScriptObject scriptObject)
    {
        // Dump the script
        DataLogger.DumpWholeScript(scriptObject);

        var script = scriptObject.GetScript();
        isRemoteFinished = false;
        IsScriptRunning = true;

        int counter = 0;
        while (counter < script.Count && IsScriptRunning)
        {
            CodeObjectOneCommand nextCodeObject = script[counter++];
            nextCodeObject.IsRunning = true;
            UpdateCodeViewer(scrollToTop: false);

            if (nextCodeObject.IsDisabled() == false)
            {
                if (nextCodeObject.GetCommand().Equals("LOOP"))
                {
                    var numRepeats = ((CodeObjectLoop)nextCodeObject).GetLoopTimes();
                    var codeToRepeat = ((CodeObjectLoop)nextCodeObject).GetNestedCommands(ignoreDisabled: true);
                    for (int _ = 0; _ < numRepeats; _++)
                    {
                        foreach (var c in codeToRepeat)
                        {
                            do
                            {
                                yield return _CoroutineCtrlPreCmd(c);
                            }
                            while (IsScriptPaused);
                            RunCommand(c);
                        }
                    }
                }
                else if (nextCodeObject.GetCommand().Equals("WAIT"))
                {
                    for (int i = 0; i < 2 * int.Parse(nextCodeObject.GetArgs()[0]); i++)
                    {
                        do
                        {
                            yield return _CoroutineCtrlPreCmd(nextCodeObject);
                        }
                        while (IsScriptPaused);
                        RunCommand(nextCodeObject);
                    }
                }
                else
                {
                    do
                    {
                        yield return _CoroutineCtrlPreCmd(nextCodeObject);
                    }
                    while (IsScriptPaused);
                    RunCommand(nextCodeObject);
                }
            }

            nextCodeObject.IsRunning = false;
            _CoroutineCtrlPostCmd();
        }

        IsScriptRunning = false;
        partnerSocket.BroadcastAvatarCtrl(
            new CodeObjectOneCommand(CodeInterpreter.CTRL_SEM_FINISH, new string[] { })
        );

        // When the remote is also finished,
        if (isRemoteFinished)
            PostExecClear();

        DataLogger.Log(
            this.gameObject, LogTag.SCRIPT, "The execution of a script has finished."
        );
    }

    private WaitForSeconds _CoroutineCtrlPreCmd(CodeObjectOneCommand c)
    {
        WaitForSeconds ctrlSignal = null;
        switch (execMode)
        {
            case ScriptExecMode.ASYNC:
                ctrlSignal = new WaitForSeconds(CMD_RUNNING_DELAY); ;
                break;
            case ScriptExecMode.SYNC_STEP_SWITCHING:
                // Set the lock, wait until the remote responses
                if (IsScriptPaused)
                {
                    // Debug.Log("Local script is paused. Waiting for the remote...");
                    ctrlSignal = null;
                    semTimeElapsed += Time.deltaTime;
                    if (semTimeElapsed >= CTRL_MAX_WAIT_TIME)
                    {
                        DataLogger.Log(
                            this.gameObject, LogTag.SYSTEM_WARNING, "Semaphore timeout."
                        );
                        IsScriptPaused = false;
                        semTimeElapsed = 0;
                        ctrlSignal = new WaitForSeconds(CMD_RUNNING_DELAY);
                    }
                }
                else
                {
                    ctrlSignal = new WaitForSeconds(CMD_RUNNING_DELAY);
                    semTimeElapsed = 0;
                }
                break;
            case ScriptExecMode.SYNC_CMD_SWITCHING:
                ctrlSignal = null;
                break;
        }

        return ctrlSignal;
    }

    private void _CoroutineCtrlPostCmd()
    {
        switch (execMode)
        {
            case ScriptExecMode.ASYNC:
                // Do nothing
                break;
            case ScriptExecMode.SYNC_STEP_SWITCHING:
                IsScriptPaused = true;
                // If the remote hasn't finished, lock self and send unlock message
                if (isRemoteFinished == false)
                {
                    IsScriptPaused = true;
                    partnerSocket.BroadcastAvatarCtrl(
                        new CodeObjectOneCommand(CTRL_SEM_UNLOCK, new string[] { })
                    );
                }
                // If the remote has finished, stop sending unlock messages and unlock self directly
                else
                {
                    IsScriptPaused = false;
                }
                break;
            case ScriptExecMode.SYNC_CMD_SWITCHING:
                // Do nothing
                break;
        }
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

    /// <summary>
    /// Tell the remote that the local has finished execution. Note this is not the same as "clear".
    /// </summary>
    public void SendImDoneSignal()
    {
        partnerSocket.BroadcastAvatarCtrl(
            new CodeObjectOneCommand(CodeInterpreter.CTRL_SEM_FINISH, new string[] { })
        );
    }

    public void InterruptRunningScript()
    {
        StopCoroutine("_RunScriptCoroutine");
        IsScriptRunning = false;
        SendImDoneSignal();
    }

    /// <summary>
    /// Stop a running script and clear all intermediate states.
    /// </summary>
    public void StopAndClearRunningState()
    {
        StopCoroutine("_RunScriptCoroutine");
        foreach (CodeObjectOneCommand c in loadedScript)
        {
            if (c.IsRunning)
            {
                c.IsRunning = false;
                break;
            }
        }
        DataLogger.Log(this.gameObject, LogTag.SCRIPT, "Script is interrupted.");
        PostExecClear();
    }

    /// <summary>
    /// Clear all intermediate states that occur during execution
    /// </summary>
    public void PostExecClear()
    {
        IsScriptRunning = false;
        isRemoteFinished = false;
        runButton.interactable = true;
        UpdateCodeViewer();
    }

    /// <summary>
    /// Reset the positions of avaters. When the fromRemote flag is true, the method resets the position of the rival avater. This flag is also designed for the remote control from the connected device.
    /// </summary>
    /// <param name="fromRemote"></param>
    public void ResetAvatars(bool fromRemote = false)
    {
        if (!fromRemote)
        {
            // Broadcast your reset message
            partnerSocket.BroadcastAvatarCtrl(new CodeObjectOneCommand(
                CTRL_SIGNAL_RESET, new string[] { })
            );
        }

        StopAndClearRunningState();

        if (avatar)
        {
            avatar.IsDead = false;
            avatar.ResetPosition();
        }
        if (rivalAvatar)
        {
            rivalAvatar.ResetPosition();
            rivalAvatar.IsDead = false;
        }

        // Reset the map
        gridController.ResetMap();

        DataLogger.Log(this.gameObject, LogTag.SCRIPT, "The avater is reset.");
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
        new Regex(@"(?<cmd>StartEngine) \(\)"),
        new Regex(@"(?<cmd>StopEngine) \(\)"),
        new Regex(@"(?<cmd>ClimbUp) \(\)"),
        new Regex(@"(?<cmd>FallDown) \(\)"),
        new Regex(@"(?<cmd>MoveForward) \(\);"),
        new Regex(@"(?<cmd>MoveBackward) \(\);"),
        new Regex(@"(?<cmd>SlowDownTail) \(\)"),
        new Regex(@"(?<cmd>SpeedUpTail) \(\)"),
    };
    private static Regex[] regexSingleCmdOneParam = {
        new Regex(@"(?<cmd>Wait) \((?<param>\d+)\);"),
        new Regex(@"(?<cmd>MOVE) \((?<param>\w+)\);"),
        new Regex(@"(?<cmd>SetTopPowerOutput) \((?<param>[\d\.]+)\)"),
        new Regex(@"(?<cmd>SetTailPowerOutput) \((?<param>[\d\.]+)\)"),
        new Regex(@"(?<cmd>SetTopBrakeOutput) \((?<param>[\d\.]+)\)"),
        new Regex(@"(?<cmd>SetTailBrakeOutput) \((?<param>[\d\.]+)\)"),
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
