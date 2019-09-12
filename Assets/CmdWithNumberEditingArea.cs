using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CmdWithNumberEditingArea : OneCmdEditingArea
{
    public Slider argSlider;
    public TextMeshProUGUI sliderLabel;

    override public void AttachCodeObject(
        CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;
        JustAwakenedFromInactive = true;

        var command = codeObject.GetCommand();
        var args = codeObject.GetArgs();

        // Clean available commands in the previous run
        oneCmdDropdown.ClearOptions();

        // If a command is not allowed to be modified, disable the dropdown menu
        if (!modifiableCmds.Contains(command))
        {
            oneCmdDropdown.GetComponentInChildren<TextMeshProUGUI>().text = command;
            oneCmdDropdown.interactable = false;
            codeToggle.interactable = false;
        }
        else
        {
            oneCmdDropdown.AddOptions(modifiableCmds);
            oneCmdDropdown.value = modifiableCmds.IndexOf(command);
            oneCmdDropdown.interactable = true;
            codeToggle.interactable = true;
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

        codeToggle.isOn = !AttachedCodeObject.IsDisabled();
    }

    public void OnCmdChange(int value)
    {
        if (JustAwakenedFromInactive)
        {
            JustAwakenedFromInactive = false;
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
                if (modifiableCmdsWtArgs.Contains(newCommand))
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
    private static List<string> modifiableCmds = new List<string>() {
        HelicopterController.CMD_START_ENG, HelicopterController.CMD_STOP_ENG,
        HelicopterController.CMD_CLIMP_UP, HelicopterController.CMD_FALL_DOWN,
        HelicopterController.CMD_MOVE_FORWARD, HelicopterController.CMD_MOVE_BACKWARD,
        HelicopterController.CMD_SLOWDOWN_TAIL, HelicopterController.CMD_SPEEDUP_TAIL,

        // HelicopterController.CMD_TOP_POWER, HelicopterController.CMD_TAIL_POWER,
        // HelicopterController.CMD_TOP_BRAKE, HelicopterController.CMD_TAIL_BRAKE,

        CodeInterpreter.CMD_WAIT,
    };

    private static List<string> modifiableCmdsWtArgs = new List<string>()
    {
        CodeInterpreter.CMD_WAIT
    };

    // ==========
}
