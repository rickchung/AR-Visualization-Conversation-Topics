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
    // TODO: Remove deprecated variables
    [HideInInspector] public GameObject ViewContainer;
    [HideInInspector] public GameObject codingPanel;

    public Button runButton;

    private ScriptObject loadedScript;
    private ScriptObject solutionScript;
    private System.Random rand = new System.Random(100);
    private ScriptExecMode execMode;
    private bool _cmdSwitchLock, _cmdSwitchRunOnceFlag, _stepSwitchLock, _coroutineLock;
    private float semTimeElapsed;
    private bool isScriptRunning, isRemoteFinished;

    private static string dataFolderPath;

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
    public bool StepSwitchLock
    {
        get
        {
            return _stepSwitchLock;
        }
        set
        {
            _stepSwitchLock = value;
        }
    }

    public bool CmdSwitchLock
    {
        get
        {
            return _cmdSwitchLock;
        }
        set
        {
            _cmdSwitchLock = value;
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

    /// <summary>
    /// Set avatars in the scene. This method is invoked when a new configuration (stage) is opened.
    /// </summary>
    /// <param name="player1"></param>
    /// <param name="rival"></param>
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

        // Init code editor
        codeEditor.LoadModifiableCommands(avatar);
    }

    [Obsolete("This method is not used since topic view is disabled in this version.")]
    public UnityAction GetTopicButtonEvent(string topic)
    {
        UnityAction action = () =>
        {
            LoadPredefinedScript(topic, broadcast: true);
        };
        return action;
    }

    [Obsolete("This method is not used since topic view is disabled in this version.")]
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

    [Obsolete("This method is not used since topic view is disabled in this version.")]
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
    ///
    /// Note: This method should always be called "after" the avatars you want to use in a scene are loaded because some properties of a script are associated with the avatars in use, which will be evaluated when reading/evaluating the code from a file.
    /// </summary>
    /// <param name="scriptName">Script name.</param>
    public void LoadPredefinedScript(string scriptName, bool broadcast = false)
    {
        ScriptObject script = ImportScriptObject(scriptName);
        if ((loadedScript = script) != null)
        {
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM, "A predefined script is imported " + scriptName);

            // Scan the script by the avatar
            foreach (var v in loadedScript.GetScript())
            {
                if (avatar.IsLockCommand(v.GetCommand()))
                    v.IsLockCommand = true;
            }

            solutionScript = loadedScript.DeepCopy();

            RandomizeLoadedScript();
        }

        // Display the script
        UpdateCodeViewer();
        // Enable the viewer
        SetActiveTopicView(true, broadcast: false);
        // Enable the viewer on the remote device
        if (broadcast) partnerSocket.BroadcastTopicCtrl(scriptName);
    }

    /// <summary>
    /// Replace the commands in the existing code objects by random commands
    /// </summary>
    public void RandomizeLoadedScript()
    {
        // Only use modifiable commands
        var availableCmds = avatar.GetModifiableCmds();

        if (loadedScript != null)
        {
            foreach (var v in loadedScript.GetScript())
            {
                // If a code object is not locked and one of available commands
                if (v.IsLockCommand == false && availableCmds.Contains(v.GetCommand()))
                {
                    // Get a random command
                    var randCmd = availableCmds[rand.Next(availableCmds.Count)];
                    // Replace the old command by the new one
                    v.SetCommand(randCmd);
                    // If the command has arguments, set them by default values
                    var argOptions = GetArgOptions(randCmd);
                    v.SetArgOps(argOptions);
                    v.ResetArgs();
                }
            }

            DumpCurrentScript("InitScript");

            UpdateCodeViewer();
        }
    }

    public void LoadSolutionScript()
    {
        loadedScript = solutionScript.DeepCopy();
        UpdateCodeViewer();
    }

    public void RunLoadedScriptSync()
    {
        RunLoadedScript();
        partnerSocket.BroadcastAvatarCtrl(
            new CodeObjectOneCommand(CTRL_SEM_RUNSCRIPT, new string[] { })
        );
    }

    /// <summary>
    /// /// Run the loaded script.
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
                CTRL_MAX_WAIT_TIME = 3;
                if (partnerSocket.IsMaster)
                    StepSwitchLock = false;
                else
                    StepSwitchLock = true;
                break;

            case ScriptExecMode.SYNC_CMD_SWITCHING:
                // if (partnerSocket.IsMaster)
                //     StepSwitchLock = true;
                // else
                //     StepSwitchLock = false;
                CmdSwitchLock = false;
                _cmdSwitchRunOnceFlag = false;
                CTRL_MAX_WAIT_TIME = 100;
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
        DumpCurrentScript("PreExec-");

        var script = scriptObject.GetScript();
        IsRemoteFinished = false;
        IsScriptRunning = true;

        int counter = 0;
        // While there are remaining commands and the flag "IsRunning" still true.
        // The flag may be set to false by other routines at some time point.
        while (counter < script.Count && IsScriptRunning)
        {
            var nextIndex = counter;
            counter++;
            CodeObjectOneCommand nextCodeObject = script[nextIndex];

            // Show the code highlight
            nextCodeObject.IsRunning = true;
            UpdateCodeViewer(scrollToTop: false, scrollToIndex: nextIndex);

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
                            CmdSwitchLock = avatar.IsLockCommand(c.GetCommand());
                            do
                                yield return _CoroutineCtrlPreCmd(c);
                            while (_coroutineLock);
                            RunCommand(c);
                            _CoroutineCtrlPostCmd(c);
                        }
                    }
                }
                // If the command is "Wait"
                else if (nextCodeObject.GetCommand().Equals(CMD_WAIT))
                {
                    CmdSwitchLock = avatar.IsLockCommand(nextCodeObject.GetCommand());
                    for (int i = 0; i < 2 * int.Parse(nextCodeObject.GetArgs()[0]); i++)
                    {
                        do
                            yield return _CoroutineCtrlPreCmd(nextCodeObject);
                        while (_coroutineLock);
                        RunCommand(nextCodeObject);
                        _CoroutineCtrlPostCmd(nextCodeObject);
                    }
                }
                // For all the other kinds of commands
                else
                {
                    CmdSwitchLock = avatar.IsLockCommand(nextCodeObject.GetCommand());
                    do
                        yield return _CoroutineCtrlPreCmd(nextCodeObject);
                    while (_coroutineLock);
                    RunCommand(nextCodeObject);
                    _CoroutineCtrlPostCmd(nextCodeObject);
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
    // This process "locks" the execution until a certain condition is met.
    private WaitForSeconds _CoroutineCtrlPreCmd(CodeObjectOneCommand c)
    {
        WaitForSeconds ctrlSignal = null;
        switch (execMode)
        {
            case ScriptExecMode.ASYNC:
                ctrlSignal = new WaitForSeconds(CMD_RUNNING_DELAY);
                break;

            case ScriptExecMode.SYNC_CMD_SWITCHING:
                if (CmdSwitchLock)
                {
                    if (_cmdSwitchRunOnceFlag)
                    {
                        _cmdSwitchRunOnceFlag = false;
                        SendUnlockSignal();
                    }

                    _coroutineLock = true;
                    ctrlSignal = null;

                    semTimeElapsed += Time.deltaTime;
                    if (semTimeElapsed >= CTRL_MAX_WAIT_TIME)
                    {
                        DataLogger.Log(this.gameObject, LogTag.SYSTEM_WARNING, "Lock Time Out. Proceeed");
                        _coroutineLock = false;
                        semTimeElapsed = 0;
                        ctrlSignal = new WaitForSeconds(0);
                    }
                }
                else
                {
                    _coroutineLock = false;
                    ctrlSignal = new WaitForSeconds(CMD_RUNNING_DELAY);
                }
                break;
            case ScriptExecMode.SYNC_STEP_SWITCHING:
                // Set the lock, wait until the remote responses
                if (StepSwitchLock)
                {
                    // Debug.Log("Locked!");
                    _coroutineLock = true;
                    ctrlSignal = null;

                    semTimeElapsed += Time.deltaTime;
                    if (semTimeElapsed >= CTRL_MAX_WAIT_TIME)
                    {
                        DataLogger.Log(this.gameObject, LogTag.SYSTEM_WARNING,
                            "Lock Time Out. Proceeed"
                        );
                        StepSwitchLock = false;
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
        }
        return ctrlSignal;
    }

    // A post-processing procedure used after running a command
    // This process reset the "lock".
    private void _CoroutineCtrlPostCmd(CodeObjectOneCommand c)
    {
        switch (execMode)
        {
            case ScriptExecMode.ASYNC:
                // Do nothing
                break;

            case ScriptExecMode.SYNC_CMD_SWITCHING:
                _cmdSwitchRunOnceFlag = true;
                break;
            case ScriptExecMode.SYNC_STEP_SWITCHING:
                // If the remote hasn't finished,
                if (IsRemoteFinished == false)
                {
                    // Lock self and send unlock message
                    StepSwitchLock = true;
                    SendUnlockSignal();
                }
                // If the remote has finished,
                else
                {
                    // Do not send unlock messages and unlock self directly
                    StepSwitchLock = false;
                }
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
        string command = codeObject.GetCommand();
        string[] args = codeObject.GetArgs();

        if (!avatar.IsStaticCommand(command))
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
                runner.ParseCommand(command, args);
            }
            else
            {
                DataLogger.Log(
                    this.gameObject, LogTag.SCRIPT,
                    string.Format("Avatar is dead., {0}, {1}", fromRemote, codeObject)
                );
            }
        }
        else
        {
            avatar.ParseCommand(command, args);
            rivalAvatar.ParseCommand(command, args);
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

    private void SendUnlockSignal()
    {
        partnerSocket.BroadcastAvatarCtrl(
            new CodeObjectOneCommand(CTRL_SEM_UNLOCK, new string[] { })
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

        var cmd = codeObject.GetCommand();
        if (cmd.Equals("LOOP"))
        {
            size = (codeObject.GetArgs().Length + 2) * 45f;
        }
        else if (cmd.Equals(CMD_WAIT))
        {
            size = 90f;
        }

        return size;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Model
        CodeObjectOneCommand codeObject = loadedScript.GetScript()[dataIndex];

        // View
        CodeObjectCellView cellView = scroller.GetCellView(codeObjectCellViewPrefab) as CodeObjectCellView;
        cellView.SetData(codeObject, dataIndex, partnerSocket.IsMaster);

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

    public void UpdateCodeViewer(bool scrollToTop = true, int scrollToIndex = -1)
    {
        // Display the script
        if (loadedScript != null)
            scriptTextMesh.SetText(loadedScript.ToString());

        var scrollPos = scriptScroller.NormalizedScrollPosition;
        if (scrollToTop)
        {
            scrollPos = 0.0f;
        }
        scriptScroller.ReloadData(scrollPositionFactor: scrollPos);

        if (scrollToIndex > 0)
        {
            var scrollPosTop = scriptScroller.ScrollPosition;
            var scrollVisibleSize = scriptScroller.ScrollRectSize;
            var scrollWholeSize = scriptScroller.ScrollSize;
            var curCellPos = scriptScroller.GetScrollPositionForDataIndex(
                scrollToIndex, EnhancedScroller.CellViewPositionEnum.Before
            );

            // If it is not within a visiable range
            if (!(curCellPos > scrollPos && curCellPos < (scrollPos + scrollVisibleSize)))
            {
                if (curCellPos + scrollVisibleSize > scriptScroller.ScrollSize)
                {
                    scriptScroller.ScrollPosition = scrollWholeSize;
                }
                else
                {
                    scriptScroller.JumpToDataIndex(scrollToIndex);
                }
            }
        }
    }


    // ========== File IO for Interpreter ==========

    private void ExportScriptObject(ScriptObject script, string scriptName)
    {
        var scriptString = script.ToString(richtext: false);
        var path = Path.Combine(dataFolderPath, scriptName);
        using (var writer = new StreamWriter(path, false))
        {
            writer.WriteLine(scriptString);
            writer.Close();
        }
    }

    private ScriptObject ImportScriptObject(string scriptName)
    {
        var path = Path.Combine(dataFolderPath, scriptName);
        ScriptObject rt = null;
        try
        {
            using (var reader = new StreamReader(path))
            {
                rt = ParseTextCodeFromReader(reader);
            }
        }
        catch (FileNotFoundException)
        {
            DataLogger.Log(LogTag.SCRIPT_ERROR, "File to import is not found: " + scriptName);
        }
        return rt;
    }

    private ScriptObject ParseTextCodeFromReader(StreamReader reader)
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

    private Regex regexLoopStart = new Regex(@"LOOP REPEAT {");
    private Regex regexLoopEnd = new Regex(@"} (?<times>\d) Times;");
    private List<Regex> regexCmdNoParams = null;
    private List<Regex> regexCmdWithParams = null;
    private Dictionary<string, string[]> cmdArgDict;

    /// <summary>
    /// Initialize the regex list and argument dictionary by the attached avatar controller.
    /// This method should be called before parsing a text script.
    /// </summary>
    private void _InitRegexAndArgDict()
    {
        // TODO: There may be a better way to init these variables. This is just a lazy solution.
        // TODO: Note I use "HelicopterController" here simply because I'm lazy.

        // Regex of commands without parameters
        regexCmdNoParams = new List<Regex>();
        regexCmdNoParams.AddRange(HelicopterController.GetNoParamCmdRegex());
        // Regex of commands with parameters
        regexCmdWithParams = new List<Regex>();
        regexCmdWithParams.AddRange(HelicopterController.GetOneParamCmdRegex());
        regexCmdWithParams.Add(new Regex(@"(?<cmd>MOVE) \((?<param>\w+)\);"));
        regexCmdWithParams.Add(new Regex(string.Format(@"(?<cmd>{0}) \((?<param>\d+)\);", CMD_WAIT)));

        // Used to look up options of arguments
        cmdArgDict = new Dictionary<string, string[]>();
        cmdArgDict.Add(CMD_WAIT, new string[] { "1", "10" });
        var avatarCmdArgDict = avatar.GetAvailableCmdArgs();
        if (avatarCmdArgDict != null)
        {
            foreach (KeyValuePair<string, string[]> kv in avatarCmdArgDict)
            {
                cmdArgDict.Add(kv.Key, kv.Value);
            }
        }
    }

    private CodeObjectOneCommand _MatchRegexSingleCommand(string line)
    {
        // If the regex list hasn't been set, import it here from the AvatarContoller
        if (regexCmdNoParams == null || regexCmdWithParams == null)
        {
            _InitRegexAndArgDict();
        }

        // No param
        foreach (var r in regexCmdNoParams)
        {
            var matched = r.Matches(line);
            if (matched != null && matched.Count > 0)
            {
                var cmd = matched[0].Groups["cmd"].Value;
                return new CodeObjectOneCommand(cmd, new string[] { });
            }
        }
        // One or more parameters
        foreach (var r in regexCmdWithParams)
        {
            var matched = r.Matches(line);
            if (matched != null && matched.Count > 0)
            {
                var cmd = matched[0].Groups["cmd"].Value;
                var arg = matched[0].Groups["param"].Value;
                var argOps = GetArgOptions(cmd);
                return new CodeObjectOneCommand(cmd, new string[] { arg }, argOps);
            }
        }
        return null;
    }
    private CodeObjectOneCommand _ProcessOneLine(string oneLine, StreamReader reader)
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

    public string[] GetArgOptions(string cmd)
    {
        if (cmdArgDict.ContainsKey(cmd))
            return cmdArgDict[cmd];
        return null;
    }

    public void DumpCurrentScript(string fnamePrefix = "")
    {
        DataLogger.DumpWholeScript(loadedScript, fnamePrefix);

    }

    // ========= Predefined Control Signals ==========

    public enum ScriptExecMode
    {
        ASYNC,
        SYNC_STEP_SWITCHING,
        SYNC_CMD_SWITCHING
    };

    private const float CMD_RUNNING_DELAY = 0.5f;
    public const string CTRL_SIGNAL_RESET = "RESET_POS";
    public const string CTRL_CLOSE_TOPIC_VIEW = "CLOSE_TOPIC_VIEW";
    public const string CTRL_SEM_LOCK = "SEM_LOCK";
    public const string CTRL_SEM_UNLOCK = "SEM_UNLOCK";
    public const string CTRL_SEM_FINISH = "SEM_FINISH";
    public const string CTRL_SEM_RUNSCRIPT = "SEM_RUNSCRIPT";
    private int CTRL_MAX_WAIT_TIME = 3;

    public const string CMD_WAIT = "Continue_Sec";
    public const string CMD_LOOP = "LOOP";
}
