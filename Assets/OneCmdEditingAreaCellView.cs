using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// This is a CellView version of the OneCmdEditingArea class. This class is specifically designed for nested contnet in a loop code. Note this class does not do anything with the OneCmdEditingArea class, even though these two classes have pretty much the same set of methods and attributes.
/// </summary>
public class OneCmdEditingAreaCellView : EnhancedScrollerCellView
{
    public TextMeshProUGUI oneCmdLabel;
    public TMP_Dropdown oneCmdDropdown;
    public Toggle codeToggle;

    private CodeObjectOneCommand attachedCodeObject;
    private List<string> availableMoveDirections = OneCmdEditingArea.availableMoveDirections;

    public CodeViewUpdateDelegate codeViewUpdateDelegate;

    public void AttachCodeObject(CodeObjectOneCommand codeObject)
    {
        attachedCodeObject = codeObject;

        string command = attachedCodeObject.GetCommand();
        string[] args = attachedCodeObject.GetArgs();
        oneCmdLabel.text = command;
        oneCmdDropdown.ClearOptions();
        oneCmdDropdown.AddOptions(availableMoveDirections);
        oneCmdDropdown.value = (int)Enum.Parse(
            typeof(GridController.Direction), args[0]
        );
        codeToggle.isOn = !attachedCodeObject.IsDisabled();
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

    /// <summary>
    /// A callback used by a toggle UI (set in the Unity inspector).
    /// </summary>
    /// <param name="value"></param>
    public void OnToggleChange(bool value)
    {
        string newCommand = oneCmdLabel.text;
        attachedCodeObject.SetDisabled(!value);

        Debug.Log(string.Format(
           "Code Disabled in Loop, {0}, {1}",
           attachedCodeObject.ToString(), value
        ));

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

}