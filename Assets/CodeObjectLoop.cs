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

    override public int GetLength()
    {
        return nestedCommands.Count + 1;
    }


    override public string ConvertCodetoString()
    {
        return "LOOP " + GetArgString();
    }

    override public string GetArgString()
    {
        string rt = "";
        string numRepeat = GetArgs()[0];

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
