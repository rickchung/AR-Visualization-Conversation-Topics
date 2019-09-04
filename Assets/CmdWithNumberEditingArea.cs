using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CmdWithNumberEditingArea : OneCmdEditingArea
{
    public TextMeshProUGUI sliderLabel;

    override public void AttachCodeObject(
        CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;

        var command = codeObject.GetCommand();
        var args = codeObject.GetArgs();

        oneCmdDropdown.ClearOptions();
        if (!availableCommands.Contains(command))
        {

            oneCmdDropdown.interactable = false;
        }
        else
        {
            oneCmdDropdown.AddOptions(availableCommands);
            oneCmdDropdown.value = availableCommands.IndexOf(command);
        }
    }
    public void OnCmdChange(int value)
    {
        string newCommand = oneCmdDropdown.options[value].text;
        var oldCommand = AttachedCodeObject.GetCommand();

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Code Modified, {0}, {1}", oldCommand + " " + newCommand,
                AttachedCodeObject.GetCommand() + " " + AttachedCodeObject.GetArgString()
            )
        );

        AttachedCodeObject.SetCommand(newCommand);
        AttachedCodeObject.SetArgs(new string[] { "1.0" });

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }
    public void OnArgChange(float value)
    {
        var intValue = (int)value;
        sliderLabel.text = intValue.ToString();

        string command = oneCmdDropdown.options[oneCmdDropdown.value].text;
        string newArgStr = value.ToString();

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Code Modified, {0}, {1}",
                command + " " + newArgStr,
                AttachedCodeObject.GetCommand() + " " + AttachedCodeObject.GetArgString()
            )
        );

        AttachedCodeObject.SetArgs(new string[] { newArgStr });

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

    // ========== TODO: Find a way to list available options of parameters ==========
    private static List<string> availableCommands = new List<string>() {
        "START_ENGINE",
        "STOP_ENGINE",
        "CLIMB_UP",
        "FALL_DOWN",
        "MOVE_FORWARD",
        "MOVE_BACKWARD",
        "TURN_RIGHT",
        "TURN_LEFT",
        "WAIT",
    };

    // ==========
}
