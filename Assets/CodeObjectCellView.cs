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

    public void SetData(CodeObjectOneCommand codeObject, int dataIndex, bool isMaster)
    {
        this.codeObject = codeObject;

        // Separate cmd and args
        // commandText.text = codeObject.GetCommand(richtext: true);
        // argumentsText.text = codeObject.GetArgString(richtext: true);
        // Combine cmd and args
        commandText.text = (
            "L" + (dataIndex + 1) + "<pos=12%>" +
            codeObject.GetCommand(richtext: true) + " " +
            codeObject.GetArgString(richtext: true)
        );

        var prefix = "";

        if (codeObject.IsLockCommand)
        {
            prefix = isMaster ?
                string.Format("<mark={0}>", PartnerSocket.MASTER_SLAVE_COLORS[1]) :
                string.Format("<mark={0}>", PartnerSocket.MASTER_SLAVE_COLORS[0]);
        }
        if (codeObject.IsRunning)
        {
            prefix = "<mark=#ffffff88>";
        }
        if (codeObject.IsBeingEdited)
        {
            prefix = "<mark=#ffff00aa>";
        }

        commandText.text = prefix + commandText.text;
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
