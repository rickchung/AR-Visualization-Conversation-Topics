using System.Collections;
using System.Collections.Generic;

public class ScriptObject : IEnumerable
{
    private List<CodeObject> mScript;

    public ScriptObject(List<CodeObject> script)
    {
        mScript = script;
    }

    public List<CodeObject> GetScript()
    {
        return mScript;
    }

    public IEnumerator GetEnumerator()
    {
        foreach (CodeObject c in mScript)
        {
            yield return c;
        }
    }

    override public string ToString()
    {
        string rt = "";

        int lineNumber = 1;
        foreach (CodeObject c in mScript)
        {
            rt += "<color=#24A0FF>L" + lineNumber.ToString() + "</color>: " + c + "\n\n";
            lineNumber += 1;
        }

        return rt;
    }
}
