using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        foreach (CodeObject c in mScript)
        {
            rt += c + "\n";
        }

        return rt;
    }
}
