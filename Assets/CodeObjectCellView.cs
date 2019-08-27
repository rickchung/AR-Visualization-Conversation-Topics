using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using TMPro;

public delegate void CodeModifyingDelegate(CodeObjectOneCommand codeObject);


/// <summary>
/// This class is a CellView class for showing code in the script scroller.
/// </summary>
public class CodeObjectCellView : EnhancedScrollerCellView
{
    public CodeObjectOneCommand codeObject;
    public TMPro.TextMeshProUGUI commandText;
    public TMPro.TextMeshProUGUI argumentsText;
    private CodeModifyingDelegate codeModifyingDelegate;

    public void SetData(CodeObjectOneCommand codeObject)
    {
        this.codeObject = codeObject;
        commandText.text = codeObject.GetCommand(richtext: true);
        argumentsText.text = codeObject.GetArgString(richtext: true);
    }

    public void SetCodeModifyingDelegate(CodeModifyingDelegate cmDelegate)
    {
        codeModifyingDelegate = cmDelegate;
    }

    /// <summary>
    /// A wrapper for passing attributes to the delegate function defined in the controller.
    /// </summary>
    /// <param name="codeObject"></param>
    public void ModifyCodeObject()
    {
        if (codeModifyingDelegate != null)
        {
            codeModifyingDelegate(this.codeObject);
        }
    }
}
