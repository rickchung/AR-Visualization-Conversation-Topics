using System.Collections.Generic;

public delegate void CodeViewUpdateDelegate();

public interface EditingArea
{
    void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn);
    void DismissEditor();

}
