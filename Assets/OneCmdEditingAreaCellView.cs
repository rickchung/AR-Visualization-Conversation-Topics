using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;


public class OneCmdEditingAreaCellView : EnhancedScrollerCellView
{
    public TextMeshProUGUI oneCmdLabel;
    public TMP_Dropdown oneCmdDropdown;
    private CodeObjectOneCommand attachedCodeObject;
    private List<string> availableMoveDirections = OneCmdEditingArea.availableMoveDirections;

    public CodeViewUpdateDelegate codeViewUpdateDelegate;

    public void AttachCodeObject(CodeObjectOneCommand codeObject)
    {
        string command = codeObject.GetCommand();
        string[] args = codeObject.GetArgs();
        oneCmdLabel.text = command;
        oneCmdDropdown.ClearOptions();
        oneCmdDropdown.AddOptions(availableMoveDirections);
        oneCmdDropdown.value = (int)Enum.Parse(
            typeof(GridController.Direction), args[0]
        );
        attachedCodeObject = codeObject;
    }

    public void RemoveAttachedCo()
    {
        attachedCodeObject = null;
    }

    public void OnArgChange(int value)
    {
        if (attachedCodeObject != null)
        {
            int newArg = value;
            string newArgStr = Enum.ToObject(typeof(GridController.Direction), newArg).ToString();

            Debug.Log(
                string.Format("Code Modified, {0}, {1}", newArgStr, attachedCodeObject.GetArgString())
            );

            attachedCodeObject.SetArgs(new string[] { newArgStr });

            if (codeViewUpdateDelegate != null)
                codeViewUpdateDelegate();
        }
    }
}