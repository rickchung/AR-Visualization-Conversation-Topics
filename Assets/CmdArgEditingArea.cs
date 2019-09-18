using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CmdArgEditingArea : OneCmdEditingArea
{
    public Slider argSlider;
    public GameObject argSelector;
    public Button argSelectorButton;
    public TextMeshProUGUI sliderLabel;
    private List<string> modifiableCmds;
    private List<string> modifiableCmdsWtArgs;

    public void SetModifiableCmdList(List<string> modifiableCmds, List<string> modifiableCmdsWtArgs)
    {
        this.modifiableCmds = modifiableCmds;
        this.modifiableCmdsWtArgs = modifiableCmdsWtArgs;
    }

    override public void AttachCodeObject(
        CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;
        JustAwakenedFromInactive = true;

        var command = codeObject.GetCommand();
        var args = codeObject.GetArgs();

        // Clean available commands in the previous run
        oneCmdDropdown.ClearOptions();


        // ===== The Command Selector =====

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

        //  ===== End of The Command Selector =====


        //  ===== The Argument Selector =====

        // If the attached code has some arguments, put the values in the slider
        //
        // TODO: There are may features haven't implemented...
        //      1) the number of arguments in the to-be-attached CodeObject
        //      2) the types of those arguments
        //      3) Now this code only works for the first argument

        var argOps = AttachedCodeObject.GetArgOps();

        // If options is null, it means the command does not accept any arguments
        if (argOps == null)
        {
            argSlider.interactable = false;
            argSlider.gameObject.SetActive(false);
            argSelector.SetActive(false);
        }
        else
        {
            int arg0 = -1;
            // Determine the type of args
            if (Int32.TryParse(argOps[0], out arg0))
            {
                argSelector.SetActive(false);

                argSlider.value = Int32.Parse(AttachedCodeObject.GetArgs()[0]);

                argSlider.gameObject.SetActive(true);
                argSlider.interactable = true;
            }
            else
            {
                argSlider.gameObject.SetActive(false);
                argSlider.interactable = false;

                foreach (Transform c in argSelector.transform)
                {
                    Destroy(c.gameObject);
                }
                foreach (var op in argOps)
                {
                    var newBtn = Instantiate(argSelectorButton, parent: argSelector.transform);
                    newBtn.GetComponentInChildren<Text>().text = op;
                }

                argSelector.SetActive(true);
            }
        }

        //  ===== End of The Argument Selector =====


        codeToggle.isOn = !AttachedCodeObject.IsDisabled();
    }

    public void OnCmdChange(int value)
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
}
