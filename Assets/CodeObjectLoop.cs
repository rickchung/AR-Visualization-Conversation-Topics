using System;
using System.Collections.Generic;

/// <summary>
/// This is a specific CodeObject designed for loop commands.
/// </summary>
public class CodeObjectLoop : CodeObjectOneCommand
{
    private List<CodeObjectOneCommand> nestedCommands;
    private int loopTimes;

    public CodeObjectLoop(string command, string[] args, List<CodeObjectOneCommand> nestedCommands) : base(command, args)
    {
        this.nestedCommands = nestedCommands;
        loopTimes = Int32.Parse(args[0]);
    }

    /// <summary>
    /// Get the nested commands in this loop excluding disabled commands.
    /// </summary>
    /// <returns></returns>
    public List<CodeObjectOneCommand> GetNestedCommands(bool ignoreDisabled = false)
    {
        if (!ignoreDisabled)
        {
            return nestedCommands;
        }

        var rtList = new List<CodeObjectOneCommand>();
        foreach (var c in nestedCommands)
        {
            if (!c.IsDisabled())
            {
                rtList.Add(c);
            }
        }
        return rtList;
    }

    public int GetLoopTimes()
    {
        return loopTimes;
    }

    public void SetLoopTimes(int value)
    {
        loopTimes = value;
        string[] args = GetArgs();
        args[0] = loopTimes.ToString();
        SetArgs(args);
    }

    public int GetNumOfNestedCmd()
    {
        return nestedCommands.Count;
    }

    override public int GetLength()
    {
        return GetNumOfNestedCmd() + 1;
    }

    override public string ConvertCodetoString()
    {
        return "LOOP " + GetArgString() + ";";
    }

    override public string GetArgString(bool richtext = false)
    {
        string rt = "";
        string numRepeat = loopTimes.ToString();

        rt += "REPEAT {\n";
        foreach (CodeObjectOneCommand c in nestedCommands)
        {
            rt += "    " + c.GetCommand(richtext) + " " + c.GetArgString(richtext) + "\n";
        }
        rt += "} " + numRepeat + " Times";

        if (richtext && IsDisabled())
        {
            rt = string.Format("<alpha=#44><s>{0}</s><alpha=#FF>", rt);
        }

        return rt;
    }


    override public string ToString()
    {
        return ConvertCodetoString();
    }

}
