using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeInterpreter : MonoBehaviour
{
    public AvatarController mAvartar;
    public TMPro.TextMeshPro mScriptTextMesh;

    private const float CMD_RUNNING_DELAY = 0.5f;

    public void _TestScript()
    {
        ScriptObject script = new ScriptObject(new List<CodeObject>() {
            new CodeObject("MOVE", new string[] {"SOUTH"}),
            new CodeObject("MOVE", new string[] {"SOUTH"}),
            new CodeObject("MOVE", new string[] {"EAST"}),
            new CodeObject("MOVE", new string[] {"EAST"}),
            new CodeObject("LOOP", new string[] {"2", "MOVE(WEST)", "MOVE(NORTH)"})
        });

        mScriptTextMesh.SetText(script.ToString());

        RunScript(script);
    }

    public void RunScript(ScriptObject script)
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

    private IEnumerator _RunScript(List<CodeObject> script)
    {
        int counter = 0;
        while (counter < script.Count)
        {
            _RunCommand(script[counter++]);
            yield return new WaitForSeconds(CMD_RUNNING_DELAY);
        }
    }

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
