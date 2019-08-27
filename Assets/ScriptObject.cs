using System.Collections;
using System.Collections.Generic;

public class ScriptObject : IEnumerable
{
    private List<CodeObjectOneCommand> mScript;

    public ScriptObject(List<CodeObjectOneCommand> script)
    {
        mScript = script;
    }

    public List<CodeObjectOneCommand> GetScript()
    {
        return mScript;
    }

    public IEnumerator GetEnumerator()
    {
        foreach (CodeObjectOneCommand c in mScript)
        {
            yield return c;
        }
    }

    override public string ToString()
    {
        return ToString(richtext: true);
    }

    public string ToString(bool richtext)
    {
        string rt = "";

        if (richtext)
        {
            int lineNumber = 1;
            foreach (CodeObjectOneCommand c in mScript)
            {
                string codeString = c.ConvertCodetoString();
                rt += "<color=#24A0FF>L" + lineNumber.ToString() + "</color>: " + codeString + "\n";
                lineNumber += 1;
            }
        }
        else
        {
            int lineNumber = 1;
            foreach (CodeObjectOneCommand c in mScript)
            {
                string codeString = c.ConvertCodetoString();
                rt += codeString + "\n";
                lineNumber += 1;
            }
        }
        return rt;

    }
}
