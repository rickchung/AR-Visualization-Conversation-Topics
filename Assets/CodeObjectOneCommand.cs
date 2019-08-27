public class CodeObjectOneCommand
{
    private string command;
    private string[] args;
    private bool disabled;

    public CodeObjectOneCommand(string command, string[] args)
    {
        this.command = command;
        this.args = args;
        this.disabled = false;
    }

    // ====================

    public string GetCommand()
    {
        return command;
    }

    public string[] GetArgs()
    {
        return args;
    }

    public void SetCommand(string command)
    {
        this.command = command;
    }

    public void SetArgs(string[] args)
    {
        this.args = args;
    }

    public void SetDisabled(bool disabled)
    {
        this.disabled = disabled;
    }

    public bool IsDisabled()
    {
        return this.disabled;
    }

    virtual public int GetLength()
    {
        return GetArgs().Length;
    }

    // ====================

    override public string ToString()
    {
        return ConvertCodetoString();
    }

    virtual public string ConvertCodetoString()
    {
        string argstr = GetArgString();
        return command + " " + argstr + ";";
    }

    virtual public string GetArgString()
    {
        return "(" + string.Join(",", args) + ")";
    }

    // ====================

    public string ToNetMessage()
    {
        return command + "|" + string.Join(",", args);
    }

    public static CodeObjectOneCommand FromNetMessage(string netMsg)
    {
        string[] split1 = netMsg.Split('|');

        string command = split1[0];

        string[] args = null;
        if (split1.Length > 1)
            args = split1[1].Split(',');

        return new CodeObjectOneCommand(command, args);
    }
}