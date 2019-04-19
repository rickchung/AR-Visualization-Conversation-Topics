

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
        return commmand + "(" + string.Join(",", args) + ")";
    }
}