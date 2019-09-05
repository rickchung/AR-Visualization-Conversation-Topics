using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CmdWithNumberEditingArea : OneCmdEditingArea
{
    public Slider argSlider;
    public TextMeshProUGUI sliderLabel;
    private bool justAwakenedFromInactive = false;

    override public void AttachCodeObject(
        CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;
        justAwakenedFromInactive = true;

        var command = codeObject.GetCommand();
        var args = codeObject.GetArgs();

        // Clean available commands in the previous run
        oneCmdDropdown.ClearOptions();

        // If a command is not allowed to be modified, disable the dropdown menu
        if (!availableCommands.Contains(command))
        {
            oneCmdDropdown.GetComponentInChildren<TextMeshProUGUI>().text = command;
            oneCmdDropdown.interactable = false;
        }
        else
        {
            oneCmdDropdown.AddOptions(availableCommands);
            oneCmdDropdown.value = availableCommands.IndexOf(command);
            oneCmdDropdown.interactable = true;
        }

        // If the attached code has some arguments, put the values in the slider
        if (args.Length > 0)
        {
            argSlider.value = float.Parse(args[0]);
            argSlider.gameObject.SetActive(true);
            argSlider.interactable = true;
        }
        else
        {
            argSlider.interactable = false;
            argSlider.gameObject.SetActive(false);

        }
    }

    public void OnCmdChange(int value)
    {
        if (justAwakenedFromInactive)
        {
            justAwakenedFromInactive = false;
        }
        else
        {
            // Get the new command from the dropdown list
            string newCommand = oneCmdDropdown.options[value].text;
            // Old command is from the attached object
            var oldCommand = AttachedCodeObject.GetCommand();

            if (!newCommand.Equals(oldCommand))
            {
                DataLogger.Log(
                    this.gameObject, LogTag.CODING,
                    string.Format(
                        "Code Modified, {0}, {1}", oldCommand + " " + newCommand,
                        AttachedCodeObject.GetCommand() + " " + AttachedCodeObject.GetArgString()
                    )
                );

                // Replace the old command by the new one
                AttachedCodeObject.SetCommand(newCommand);

                // Check whether the new command requires arguments
                if (cmdWithArgs.Contains(newCommand))
                {
                    AttachedCodeObject.SetArgs(new string[] { "1" });
                    argSlider.value = 1.0f;
                    argSlider.gameObject.SetActive(true);
                    argSlider.interactable = true;
                }
                else
                {
                    AttachedCodeObject.SetArgs(new string[] { });
                    argSlider.gameObject.SetActive(false);
                    argSlider.interactable = false;
                }

                if (codeViewUpdateDelegate != null)
                    codeViewUpdateDelegate();
            }
        }
    }
    public void OnArgChange(float value)
    {
        // Update the slider label
        var intValue = (int)value;
        sliderLabel.text = intValue.ToString();
        // The new argument is from the slider
        string newArgStr = value.ToString();
        string command = AttachedCodeObject.GetCommand();

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Code Modified, {0}, {1}",
                command + " " + newArgStr,
                AttachedCodeObject.GetCommand() + " " + AttachedCodeObject.GetArgString()
            )
        );

        // Replace old arguments
        AttachedCodeObject.SetArgs(new string[] { newArgStr });

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

    // ========== TODO: Find a way to list available options of parameters ==========
    private static List<string> availableCommands = new List<string>() {
        "START_ENGINE", "STOP_ENGINE",
        "CLIMB_UP", "FALL_DOWN",
        "MOVE_FORWARD","MOVE_BACKWARD",
        "TURN_RIGHT", "TURN_LEFT",
        "WAIT",
    };

    private static List<string> cmdWithArgs = new List<string>()
    {
        "WAIT"
    };

    // ==========
}
