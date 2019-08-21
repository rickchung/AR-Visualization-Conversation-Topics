using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using TMPro;

public class CodeObjectCellView : EnhancedScrollerCellView
{
    public CodeObject codeObject;
    public TMPro.TextMeshProUGUI commandText;
    public TMPro.TextMeshProUGUI argumentsText;

    public void SetData(CodeObject codeObject)
    {
        this.codeObject = codeObject;
        commandText.text = codeObject.GetCommand();
        argumentsText.text = codeObject.GetArgString();
    }
}
