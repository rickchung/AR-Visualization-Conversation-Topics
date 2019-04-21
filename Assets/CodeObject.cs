public class CodeObject
{
    public string commmand;
    public string[] args;

    public CodeObject(string commmand, string[] args)
    {
        this.commmand = commmand;
        this.args = args;
    }

    override public string ToString()
    {
        string rt = "";

        switch (commmand)
        {
            case "LOOP":
                string numRepeat = args[0];
                rt += "REPEAT {\n";
                for (int i = 1; i < args.Length; i++)
                    rt += "    " + args[i] + "\n";
                rt += "} " + numRepeat + " Times";
                break;
            default:
                rt = commmand + "(" + string.Join(",", args) + ")";
                break;
        }
        return rt;
    }
}