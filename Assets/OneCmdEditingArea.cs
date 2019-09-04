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

    public CodeObjectOneCommand AttachedCodeObject
    {
        get
        {
            return attachedCodeObject;
        }

        set
        {
            attachedCodeObject = value;
        }
    }

    virtual public void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;

        string command = AttachedCodeObject.GetCommand();
        string[] args = AttachedCodeObject.GetArgs();

        oneCmdLabel.text = command;
        oneCmdDropdown.ClearOptions();
        oneCmdDropdown.AddOptions(availableMoveDirections);
        oneCmdDropdown.value = (int)Enum.Parse(
            typeof(GridController.Direction), args[0]
        );

        codeToggle.isOn = !AttachedCodeObject.IsDisabled();

        oneCmdSubmitBtn.gameObject.SetActive(showSubmitBtn);
    }

    /// <summary>
    /// A callback used by dropdown UI (set in the Unity inspector).
    /// </summary>
    /// <param name="value"></param>
    virtual public void OnArgChange(int value)
    {
        string newCommand = oneCmdLabel.text;
        int newArg = value;
        string newArgStr = Enum.ToObject(
            typeof(GridController.Direction), newArg
        ).ToString();

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Code Modified, {0}, {1}",
                newCommand + " " + newArgStr,
                AttachedCodeObject.GetCommand() + " " + AttachedCodeObject.GetArgString()
            )
        );

        AttachedCodeObject.SetCommand(newCommand);
        AttachedCodeObject.SetArgs(new string[] { newArgStr });

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
        AttachedCodeObject.SetDisabled(!value);

        Debug.Log(string.Format(
           "Code Disabled, {0}, {1}",
           AttachedCodeObject.ToString(), value
        ));

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

    public void DismissEditor()
    {
        if (AttachedCodeObject != null)
        {
            // Clean up
            AttachedCodeObject = null;
            editorDispatcher.DismissEditor();
        }
    }

    // ========== TODO: Find a way to list available options of parameters ==========
    public static List<string> availableMoveDirections = new List<string>() {
        GridController.Direction.NORTH.ToString(),
        GridController.Direction.SOUTH.ToString(),
        GridController.Direction.EAST.ToString(),
        GridController.Direction.WEST.ToString()
    };

    // ==========
}