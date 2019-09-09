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
    private bool isScriptSyncPaused;
    private bool _coroutineLock;
    private bool isRemoteFinished;
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

    /// <summary>
    /// This property is different from "IsScriptRunning" and used to keep synchronization between local and remote script executions. When a script is paused, it means it is waiting for the remote procedure to unlock it.
    /// </summary>
    /// <value></value>
    public bool IsScriptSyncPaused
    {
        get
        {
            return isScriptSyncPaused;
        }
        set
        {
            isScriptSyncPaused = value;
        }
    }

    /// <summary>
    /// This property indicates whether a local execution is done or interrupted.
    /// </summary>
    /// <value></value>
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

    /// <summary>
    /// Used to catche the execution state of the remote device.
    /// </summary>
    /// <value></value>
    public bool IsRemoteFinished
    {
        get
        {
            return isRemoteFinished;
        }

        set
        {
            isRemoteFinished = value;
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

    public const string CMD_WAIT = "Wait";
    public const string CMD_LOOP = "LOOP";


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
            // If there are no scripts running, run the loaded script
            if (IsScriptRunning == false)
            {
                DataLogger.Log(this.gameObject, LogTag.SCRIPT,
                    "Start running the loaded script"
                );

                // Disable the run button
                runButton.interactable = false;
                // Start the execution routine with the loaded script
                _PreExecProcess(loadedScript);
            }
            // If there are some scripts running now
            else
            {
                DataLogger.Log(this.gameObject, LogTag.SCRIPT_WARNING,
                    "A script is running but trying to re-run it"
                );
            }
        }
        // If there is not a loaded script, log error.
        else
        {
            DataLogger.Log(this.gameObject, LogTag.SCRIPT_ERROR,
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
        // Init sync control before firing the coroutine
        switch (execMode)
        {
            // In the step-switching mode, the master runs first and the slave does later.
            case ScriptExecMode.SYNC_STEP_SWITCHING:
                if (partnerSocket.IsMaster)
                    IsScriptSyncPaused = false;
                else
                    IsScriptSyncPaused = true;
                break;
        }

        // Start the script execution coroutine
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
        IsRemoteFinished = false;
        IsScriptRunning = true;

        int counter = 0;
        // While there are remaining commands and the flag "IsRunning" still true.
        // The flag may be set to false by other routines at some time point.
        while (counter < script.Count && IsScriptRunning)
        {
            CodeObjectOneCommand nextCodeObject = script[counter++];

            // Show the code highlight
            nextCodeObject.IsRunning = true;
            UpdateCodeViewer(scrollToTop: false);

            // If the code is not disabled in the editor
            if (nextCodeObject.IsDisabled() == false)
            {
                // If it's a loop
                if (nextCodeObject.GetCommand().Equals(CMD_LOOP))
                {
                    var numRepeats = ((CodeObjectLoop)nextCodeObject).GetLoopTimes();
                    var codeToRepeat = ((CodeObjectLoop)nextCodeObject).GetNestedCommands(ignoreDisabled: true);
                    for (int _ = 0; _ < numRepeats; _++)
                    {
                        foreach (var c in codeToRepeat)
                        {
                            do
                                yield return _CoroutineCtrlPreCmd(c);
                            while (_coroutineLock);
                            RunCommand(c);
                            _CoroutineCtrlPostCmd();

                        }
                    }
                }
                // If the command is "Wait"
                else if (nextCodeObject.GetCommand().Equals(CMD_WAIT))
                {
                    for (int i = 0; i < 2 * int.Parse(nextCodeObject.GetArgs()[0]); i++)
                    {
                        do
                            yield return _CoroutineCtrlPreCmd(nextCodeObject);
                        while (_coroutineLock);
                        RunCommand(nextCodeObject);
                        _CoroutineCtrlPostCmd();

                    }
                }
                // For all the other kinds of commands
                else
                {
                    do
                        yield return _CoroutineCtrlPreCmd(nextCodeObject);
                    while (_coroutineLock);
                    RunCommand(nextCodeObject);
                    _CoroutineCtrlPostCmd();
                }
            }

            nextCodeObject.IsRunning = false;
        }


        IsScriptRunning = false;
        // Tell the remote that I'm done.
        SendImDoneSignal();

        // If the remote has already finished when I'm done, do the final cleanup
        if (IsRemoteFinished)
            PostExecClear();

        DataLogger.Log(this.gameObject, LogTag.SCRIPT,
            "The execution of a script has finished."
        );
    }

    // A preprocessing procedure used befure running a command
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
                if (IsScriptSyncPaused)
                {
                    // Debug.Log("Locked!");
                    _coroutineLock = true;
                    ctrlSignal = null;
                    semTimeElapsed += Time.deltaTime;

                    // If I've waited for an unlocking signal too long
                    if (semTimeElapsed >= CTRL_MAX_WAIT_TIME)
                    {
                        DataLogger.Log(this.gameObject, LogTag.SYSTEM_WARNING,
                            "Semaphore timeout."
                        );
                        IsScriptSyncPaused = false;
                        semTimeElapsed = 0;
                        ctrlSignal = new WaitForSeconds(0);
                    }
                }
                // If I get an unlocking signal, I will be here.
                else
                {
                    // Debug.Log("Unlocked!");
                    _coroutineLock = false;
                    // I may still need to wait several seconds to make the execution as smooth as possible
                    var leftWaitingTime = Mathf.Max(0, CMD_RUNNING_DELAY - semTimeElapsed);
                    ctrlSignal = new WaitForSeconds(leftWaitingTime);
                    semTimeElapsed = 0;
                }
                break;

            case ScriptExecMode.SYNC_CMD_SWITCHING:
                ctrlSignal = null;
                break;
        }
        return ctrlSignal;
    }

    // A post-processing procedure used after running a command
    private void _CoroutineCtrlPostCmd()
    {
        switch (execMode)
        {
            case ScriptExecMode.ASYNC:
                // Do nothing
                break;

            case ScriptExecMode.SYNC_STEP_SWITCHING:
                // If the remote hasn't finished,
                if (IsRemoteFinished == false)
                {
                    // Lock self and send unlock message
                    IsScriptSyncPaused = true;
                    partnerSocket.BroadcastAvatarCtrl(
                        new CodeObjectOneCommand(CTRL_SEM_UNLOCK, new string[] { })
                    );
                }
                // If the remote has finished,
                else
                {
                    // Do not send unlock messages and unlock self directly
                    IsScriptSyncPaused = false;
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
            DataLogger.Log(this.gameObject, LogTag.SCRIPT,
                string.Format(
                    "Running Cmd, fromRemote={0}, {1}", fromRemote, codeObject
                )
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
    /// Tell the remote that the local has finished execution.
    /// </summary>
    public void SendImDoneSignal()
    {
        partnerSocket.BroadcastAvatarCtrl(
            new CodeObjectOneCommand(CodeInterpreter.CTRL_SEM_FINISH, new string[] { })
        );
    }

    /// <summary>
    /// This method is used to interrupt the exec of local scripts and notify the remote. It does not stop the virtual sync execution (in other words, the local will still wait for the remote completion after this method is invoked). This method is especially useful in the cases like the avatar triggers a trap or goes out of boundary where you need to interrupt the local script but not mean to reset everything. See "StopAndClearRunningState()".
    /// </summary>
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
    /// Clear all intermediate states that occur during execution. This method should only be invoked at the very end of sync execution after all devices finish executions.
    /// </summary>
    public void PostExecClear()
    {
        IsScriptRunning = false;
        IsRemoteFinished = false;
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
            // Ask the remote to call reset function.
            partnerSocket.BroadcastAvatarCtrl(new CodeObjectOneCommand(
                CTRL_SIGNAL_RESET, new string[] { })
            );
        }

        // Interrupt the running script and reset all the execution states
        StopAndClearRunningState();
        // Reset the position of my avatar
        if (avatar)
        {
            avatar.IsDead = false;
            avatar.ResetPosition();
        }
        // Reset the position of the remote's avatar
        if (rivalAvatar)
        {
            rivalAvatar.IsDead = false;
            rivalAvatar.ResetPosition();
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

    private static Regex regexLoopStart = new Regex(@"LOOP REPEAT {");
    private static Regex regexLoopEnd = new Regex(@"} (?<times>\d) Times;");
    private static List<Regex> regexSingleCmdNoParam = null;
    private static List<Regex> regexSingleCmdOneParam = null;

    private static CodeObjectOneCommand _MatchRegexSingleCommand(string line)
    {
        if (regexSingleCmdNoParam == null || regexSingleCmdOneParam == null)
        {
            regexSingleCmdNoParam = new List<Regex>();
            regexSingleCmdNoParam.AddRange(HelicopterController.GetNoParamCmdRegex());

            regexSingleCmdOneParam = new List<Regex>();
            regexSingleCmdOneParam.AddRange(HelicopterController.GetOneParamCmdRegex());
            regexSingleCmdOneParam.Add(new Regex(@"(?<cmd>MOVE) \((?<param>\w+)\);"));
            regexSingleCmdOneParam.Add(
                new Regex(string.Format(@"(?<cmd>{0}) \((?<param>\d+)\);", CMD_WAIT))
            );
        }

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
