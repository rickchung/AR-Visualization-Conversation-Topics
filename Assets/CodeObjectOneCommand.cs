public class CodeObjectOneCommand
{
    private string command;
    private string[] args;
    private bool disabled;

    private bool isBeingEdited;
    private bool isRunning;

    public bool IsRunning
    {
        get
        {
            return isRunning;
        }

        set
        {
            isRunning = value;
        }
    }

    public bool IsBeingEdited
    {
        get
        {
            return isBeingEdited;
        }

        set
        {
            isBeingEdited = value;
        }
    }

    public CodeObjectOneCommand(string command, string[] args)
    {
        this.command = command;
        this.args = args;
        this.disabled = false;
    }

    // ====================

    /// <summary>
    /// Get the command of this code object. The richtext flag specifies whether to return the string command reflecting the state of this code object. For example, when the code is disabled, the string will show in a faded font.
    /// </summary>
    /// <param name="richtext"></param>
    /// <returns></returns>
    public string GetCommand(bool richtext = false)
    {
        var rt = command;

        if (!richtext)
            return rt;

        if (!disabled)
            return rt;
        else
            return string.Format("<alpha=#44><s>{0}</s><alpha=#FF>", rt);
    }

    public string[] GetArgs()
    {
        return args;
    }

    /// <summary>
    /// Return the arg of this code as a string. The richtext flag specifies whether to return the string command reflecting the state of this code object. For example, when the code is disabled, the string will show in a faded font.
    virtual public string GetArgString(bool richtext = false)
    {
        var rt = "(" + string.Join(",", args) + ")";

        if (richtext && IsDisabled())
        {
            rt = string.Format("<alpha=#44><s>{0}</s><alpha=#FF>", rt);
        }

        return rt;
    }


    public bool IsDisabled()
    {
        return this.disabled;
    }

    virtual public int GetLength()
    {
        return GetCommand().Length;
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

    // ==================== Networking

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