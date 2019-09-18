﻿using System;
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
    public Button codeToggle;

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

    public bool JustAwakenedFromInactive
    {
        get
        {
            return justAwakenedFromInactive;
        }

        set
        {
            justAwakenedFromInactive = value;
        }
    }

    private CodeObjectOneCommand attachedCodeObject;
    private bool justAwakenedFromInactive = false;

    virtual public void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;
        JustAwakenedFromInactive = true;

        string command = AttachedCodeObject.GetCommand();
        string[] args = AttachedCodeObject.GetArgs();

        oneCmdLabel.text = command;
        oneCmdDropdown.ClearOptions();
        oneCmdDropdown.AddOptions(availableMoveDirections);
        oneCmdDropdown.value = (int)Enum.Parse(
            typeof(GridController.Direction), args[0]
        );

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
        if (!value == AttachedCodeObject.IsDisabled())
            return;

        string newCommand = oneCmdLabel.text;
        AttachedCodeObject.SetDisabled(!value);

        Debug.Log(string.Format(
            "A command {0} is {1}",
            AttachedCodeObject.ToString(), value ? "enabled" : "disabled"
        ));

        UpdateCodeViewer();
        DismissEditor();
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

    /// <summary>
    /// Update the code viewer by the attached delegate
    /// </summary>
    public void UpdateCodeViewer()
    {
        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
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