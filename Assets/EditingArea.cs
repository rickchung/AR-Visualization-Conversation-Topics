using System.Collections.Generic;

public interface EditingArea
{
    void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn);
    void DismissEditor();

}
