using System;
using System.Collections.Generic;

public class CodeObjectLoop : CodeObjectOneCommand
{
    private List<CodeObjectOneCommand> nestedCommands;
    private int loopTimes;

    public CodeObjectLoop(string command, string[] args, List<CodeObjectOneCommand> nestedCommands) : base(command, args)
    {
        this.nestedCommands = nestedCommands;
        loopTimes = Int32.Parse(args[0]);
    }

    public List<CodeObjectOneCommand> GetNestedCommands()
    {
        return nestedCommands;
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
        return "LOOP " + GetArgString();
    }

    override public string GetArgString()
    {
        string rt = "";
        string numRepeat = loopTimes.ToString();

        rt += "REPEAT {\n";
        foreach (CodeObjectOneCommand c in nestedCommands)
        {
            rt += "    " + c.ToString() + "\n";
        }
        rt += "} " + numRepeat + " Times";

        return rt;
    }


    override public string ToString()
    {
        return ConvertCodetoString();
    }

}
