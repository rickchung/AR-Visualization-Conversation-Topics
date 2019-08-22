using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OneCmdEditingArea : MonoBehaviour
{
    public CodeEditor editorDispatcher;
    public TextMeshProUGUI oneCmdLabel;
    public TMP_Dropdown oneCmdDropdown;
    public Button oneCmdSubmitBtn;

    private CodeObject attachedCodeObject;

    public void AttachCodeObject(CodeObject codeObject, List<string> argOptions)
    {
        attachedCodeObject = codeObject;

        string command = attachedCodeObject.GetCommand();
        string[] args = attachedCodeObject.GetArgs();

        oneCmdLabel.text = command;
        oneCmdDropdown.ClearOptions();
        oneCmdDropdown.AddOptions(argOptions);
        oneCmdDropdown.value = (int)Enum.Parse(typeof(GridController.Direction), args[0]);
    }

    public void ApplyChangeToCodeObject()
    {
        if (attachedCodeObject != null)
        {
            string newCommand = oneCmdLabel.text;
            int newArg = oneCmdDropdown.value;
            string newArgStr = Enum.ToObject(typeof(GridController.Direction), newArg).ToString();
            attachedCodeObject.SetCommand(newCommand);
            attachedCodeObject.SetArgs(new string[] { newArgStr });
            // Clean up
            attachedCodeObject = null;
            editorDispatcher.DismissEditor();
        }
    }
}