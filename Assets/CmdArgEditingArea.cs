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

    override public void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        AttachedCodeObject = codeObject;
        JustAwakenedFromInactive = true;
        var command = codeObject.GetCommand();
        var args = codeObject.GetArgs();

        // ===== The Command Selector =====
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
        //  ===== End of The Command Selector =====

        //  ===== The Argument Selector =====
        // If the attached code has some arguments, put the values in the slider
        //
        // TODO: There are may features haven't implemented...
        //      1) the number of arguments in the to-be-attached CodeObject
        //      2) the types of those arguments
        //      3) Now this code only works for the first argument

        var argOps = AttachedCodeObject.GetArgOps();
        if (argOps == null)  // If T, The command does not accept any arguments
        {
            // Disable all the selectors
            argSlider.interactable = false;
            argSlider.gameObject.SetActive(false);
            argSelector.SetActive(false);
        }
        else
        {
            int arg0 = -1;
            if (Int32.TryParse(argOps[0], out arg0))  // If the argument is numeric
            {
                // Hide the arg selector
                argSelector.SetActive(false);
                // Insert the value of arguments of the attached command
                argSlider.value = Int32.Parse(AttachedCodeObject.GetArgs()[0]);
                // Enable the slider
                argSlider.gameObject.SetActive(true);
                argSlider.interactable = true;
                // Enable the submission button
                oneCmdSubmitBtn.gameObject.SetActive(true);
            }
            else  // If the type of args is not numeric
            {
                // Hide the slider
                argSlider.gameObject.SetActive(false);
                argSlider.interactable = false;
                // Remove any previous buttons
                foreach (Transform c in argSelector.transform)
                    Destroy(c.gameObject);
                // Instantiate new buttions for options
                foreach (var op in argOps)
                {
                    var newBtn = Instantiate(argSelectorButton, parent: argSelector.transform);
                    newBtn.GetComponentInChildren<Text>().text = op;
                    // When the button is clicked, replace the argument of attached code
                    // by its literal text value
                    newBtn.onClick.AddListener(() => { OnArgButtonClick(op); });
                }
                // Enable the selector
                argSelector.SetActive(true);
                // Disable the submission button
                oneCmdSubmitBtn.gameObject.SetActive(false);
            }
        }
        //  ===== End of The Argument Selector =====

        codeToggle.isOn = !AttachedCodeObject.IsDisabled();
    }

    /// <summary>
    /// Update the code viewer by the attached delegate
    /// </summary>
    private void UpdateCodeViewer()
    {
        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

    /// <summary>
    /// A callback method only used by the dropdown command selector. It is set in the Unity editor.
    /// </summary>
    /// <param name="value"></param>
    public void OnCmdChange(int value)
    {
        // Get the new command from the dropdown list
        string newCommand = oneCmdDropdown.options[value].text;
        // Old command is from the attached object
        var oldCommand = AttachedCodeObject.GetCommand();

        if (!newCommand.Equals(oldCommand))
        {
            // Replace the old command by the new one
            AttachedCodeObject.SetCommand(newCommand);
            var argOps = editorDispatcher.GetArgOptions(newCommand);
            AttachedCodeObject.SetArgOps(argOps);
            AttachedCodeObject.ResetArgs();

            // Re-dispatch the editor
            editorDispatcher.DispatchEditor(AttachedCodeObject);

            DataLogger.Log(
                this.gameObject, LogTag.CODING,
                string.Format(
                    "A command was modified, fr {0} to {1}",
                    oldCommand, newCommand
                )
            );
        }
    }

    /// <summary>
    /// A callback method only used by the argument buttons in the arg selector to set the argument of attached code by the passed value.
    /// </summary>
    /// <param name="value"></param>
    private void OnArgButtonClick(string value)
    {
        var command = AttachedCodeObject.GetCommand();
        var oldArg = AttachedCodeObject.GetArgString();
        var newArg = value;

        // Replace old arguments and update the code viewer
        AttachedCodeObject.SetArgs(new string[] { value });
        UpdateCodeViewer();

        DismissEditor();

        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Arg was modified for {0} fr {1} to {2}",
                command, oldArg, newArg
            )
        );
    }

    /// <summary>
    /// This callback is used only by the arg slider and set in the Unity editor.
    /// </summary>
    /// <param name="value"></param>
    public void OnArgSliderValueChange(float value)
    {
        // Update the slider label
        var intValue = (int)value;
        sliderLabel.text = intValue.ToString();

        // The new argument is coming from the slider
        string newArg = value.ToString();
        string oldArg = AttachedCodeObject.GetArgString();
        string command = AttachedCodeObject.GetCommand();

        // Replace old arguments
        AttachedCodeObject.SetArgs(new string[] { newArg });
        // Update the code viewer
        UpdateCodeViewer();

        // Log this change
        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format(
                "Arg was modified for {0}, fr {1} to {2}",
                command, oldArg, newArg
            )
        );
    }
}
