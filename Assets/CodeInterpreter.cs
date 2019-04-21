using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeInterpreter : MonoBehaviour
{
    public AvatarController mAvartar;
    public TMPro.TextMeshPro mScriptTextMesh;
    public GameObject mExampleContainer;
    public GameObject mControlPanel;

    private const float CMD_RUNNING_DELAY = 0.5f;
    private ScriptObject loadedScript;
    private List<string> mAvailableScripts = new List<string>
    {
        "SEQUENTIAL"
    };

    public void _TestScript()
    {
        LoadPredefinedScript("SEQUENTIAL");
        RunLoadedScript();
    }

    public void ActivateExampleView(bool activated)
    {
        mExampleContainer.SetActive(activated);
        mControlPanel.SetActive(activated);
    }

    public bool IsTopicSampleAvailable(string topic)
    {
        if (mAvailableScripts.Contains(topic))
            return true;
        return false;
    }

    /// <summary>
    /// Load the predefined script template specified by <paramref name="scriptName"/>.
    /// </summary>
    /// <param name="scriptName">Script name.</param>
    public void LoadPredefinedScript(string scriptName)
    {
        ScriptObject script = null;

        switch (scriptName)
        {
            case "SEQUENTIAL":
                script = new ScriptObject(new List<CodeObject>() {
                    new CodeObject("MOVE", new string[] {"SOUTH"}),
                    new CodeObject("MOVE", new string[] {"SOUTH"}),
                    new CodeObject("MOVE", new string[] {"EAST"}),
                    new CodeObject("MOVE", new string[] {"EAST"}),
                    new CodeObject("LOOP", new string[] {
                        "2", "MOVE(WEST)", "MOVE(NORTH)"
                    })
                });
                break;

        }

        loadedScript = script;

        if (loadedScript != null)
        {
            mScriptTextMesh.SetText(loadedScript.ToString());
            ActivateExampleView(true);
        }
    }

    /// <summary>
    /// Run the loaded script.
    /// </summary>
    public void RunLoadedScript()
    {
        if (loadedScript != null)
            RunScript(loadedScript);
    }

    /// <summary>
    /// Run the given ScriptObject. This method also preprocesses the script
    /// by transforming loops into code sequences.
    /// </summary>
    /// <param name="script">Script.</param>
    private void RunScript(ScriptObject script)
    {
        // Preprocess the script
        var procScript = new List<CodeObject>();
        foreach (CodeObject codeObject in script)
        {
            if (codeObject.commmand.Equals("LOOP"))
            {
                _ParseLoop(codeObject, procScript);
            }
            else
            {
                procScript.Add(codeObject);
            }
        }

        // Run the script
        StartCoroutine(_RunScript(procScript));
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
    private IEnumerator _RunScript(List<CodeObject> script)
    {
        int counter = 0;
        while (counter < script.Count)
        {
            _RunCommand(script[counter++]);
            yield return new WaitForSeconds(CMD_RUNNING_DELAY);
        }
    }

    /// <summary>
    /// Run a single command in the given codeObject.
    /// </summary>
    /// <param name="codeObject">Code object.</param>
    private void _RunCommand(CodeObject codeObject)
    {
        Debug.Log("Running: " + codeObject);

        string command = codeObject.commmand;
        string[] args = codeObject.args;

        switch (command)
        {
            case "MOVE":
                mAvartar.Move(args[0]);
                break;
        }
    }


}
