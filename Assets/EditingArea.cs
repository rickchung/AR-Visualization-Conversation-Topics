using System.Collections.Generic;

public interface EditingArea
{
    void AttachCodeObject(CodeObjectOneCommand codeObject, List<string> argOptions);
    void ApplyChangeToCodeObject();

}
