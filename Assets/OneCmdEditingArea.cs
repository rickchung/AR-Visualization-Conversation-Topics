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

    private CodeObjectOneCommand attachedCodeObject;

    public static List<string> availableMoveDirections = new List<string>() {
        GridController.Direction.NORTH.ToString(),
        GridController.Direction.SOUTH.ToString(),
        GridController.Direction.EAST.ToString(),
        GridController.Direction.WEST.ToString()
    };

    public void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        attachedCodeObject = codeObject;

        string command = attachedCodeObject.GetCommand();
        string[] args = attachedCodeObject.GetArgs();

        oneCmdLabel.text = command;
        oneCmdDropdown.ClearOptions();
        oneCmdDropdown.AddOptions(availableMoveDirections);
        oneCmdDropdown.value = (int)Enum.Parse(typeof(GridController.Direction), args[0]);

        oneCmdSubmitBtn.gameObject.SetActive(showSubmitBtn);
    }

    public void OnArgChange(int value)
    {
        string newCommand = oneCmdLabel.text;
        int newArg = value;
        string newArgStr = Enum.ToObject(typeof(GridController.Direction), newArg).ToString();

        attachedCodeObject.SetCommand(newCommand);
        attachedCodeObject.SetArgs(new string[] { newArgStr });
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