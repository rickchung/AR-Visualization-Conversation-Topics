using EnhancedUI.EnhancedScroller;

public class CodeObject
{
    public string command;
    public string[] args;

    public CodeObject(string command, string[] args)
    {
        this.command = command;
        this.args = args;
    }

    // ====================

    override public string ToString()
    {
        string argstr = CmdArgToString(command, args);
        return command + " " + argstr;
    }

    public string GetCommand()
    {
        return command;
    }

    public string[] GetArgs()
    {
        return args;
    }

    public string GetArgString()
    {
        return CmdArgToString(command, args);
    }

    public void SetCommand(string command)
    {
        this.command = command;
    }

    public void SetArgs(string[] args)
    {
        this.args = args;
    }

    private static string CmdArgToString(string command, string[] args)
    {
        string rt = "";
        switch (command)
        {
            case "LOOP":
                string numRepeat = args[0];
                rt += "REPEAT {\n";
                for (int i = 1; i < args.Length; i++)
                    rt += "    " + args[i] + "\n";
                rt += "} " + numRepeat + " Times";
                break;
            default:
                rt = "(" + string.Join(",", args) + ")";
                break;
        }
        return rt;
    }

    // ====================

    public string ToNetMessage()
    {
        return command + "|" + string.Join(",", args);
    }

    public static CodeObject FromNetMessage(string netMsg)
    {
        string[] split1 = netMsg.Split('|');

        string command = split1[0];

        string[] args = null;
        if (split1.Length > 1)
            args = split1[1].Split(',');

        return new CodeObject(command, args);
    }
}