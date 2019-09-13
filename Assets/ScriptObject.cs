using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ScriptObject collects CodeObjectOneCommand objects in a list, which acts like a programming script.
/// </summary>
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
            if (!c.IsDisabled())
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

    public ScriptObject DeepCopy()
    {
        ScriptObject other = (ScriptObject)this.MemberwiseClone();
        other.mScript = new List<CodeObjectOneCommand>();
        foreach (var v in this.mScript)
        {
            other.mScript.Add(v.DeepCopy());
        }
        return other;
    }
}
