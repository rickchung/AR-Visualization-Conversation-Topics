using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OneCmdEditingArea : MonoBehaviour, EditingArea
{
    public CodeEditor editorDispatcher;
    public TextMeshProUGUI oneCmdLabel;
    public TMP_Dropdown oneCmdDropdown;
    public Button oneCmdSubmitBtn;
    public Toggle codeToggle;
    private CodeObjectOneCommand attachedCodeObject;
    public CodeViewUpdateDelegate codeViewUpdateDelegate;


    // ========== Temp ==========
    public static List<string> availableMoveDirections = new List<string>() {
        GridController.Direction.NORTH.ToString(),
        GridController.Direction.SOUTH.ToString(),
        GridController.Direction.EAST.ToString(),
        GridController.Direction.WEST.ToString()
    };
    // ==========


    public void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
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

        oneCmdSubmitBtn.gameObject.SetActive(showSubmitBtn);
    }

    /// <summary>
    /// A callback used by dropdown UI (set in the Unity inspector).
    /// </summary>
    /// <param name="value"></param>
    public void OnArgChange(int value)
    {
        string newCommand = oneCmdLabel.text;
        int newArg = value;
        string newArgStr = Enum.ToObject(
            typeof(GridController.Direction), newArg).ToString();

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Code Modified, {0}, {1}",
                newCommand + " " + newArgStr,
                attachedCodeObject.GetCommand() + " " + attachedCodeObject.GetArgString()
            )
        );

        attachedCodeObject.SetCommand(newCommand);
        attachedCodeObject.SetArgs(new string[] { newArgStr });

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
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
           "Code Disabled, {0}, {1}",
           attachedCodeObject.ToString(), value
        ));

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

    public void DismissEditor()
    {
        if (attachedCodeObject != null)
        {
            // Clean up
            attachedCodeObject = null;
            editorDispatcher.DismissEditor();
        }
    }
}