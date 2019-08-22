using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;

public class LoopEditingArea : MonoBehaviour, IEnhancedScrollerDelegate, EditingArea
{
    public CodeEditor editorDispatcher;
    public TextMeshProUGUI loopTimesLabel;
    public Button loopTimesPlusBtn, loopTimesMinusBtn;
    public Button loopSubmitBtn;
    public EnhancedScroller nestedCmdScroller;
    public OneCmdEditingArea oneCmdEditingAreaPrefab;

    private CodeObjectLoop attachedCodeObject;
    private int loopTimes;

    private void Start()
    {
        // nestedCmdScroller.Delegate = this;
    }

    public void AttachCodeObject(CodeObjectOneCommand codeObject, List<string> argOptions)
    {
        attachedCodeObject = (CodeObjectLoop)codeObject;

        string command = attachedCodeObject.GetCommand();
        string[] args = attachedCodeObject.GetArgs();
        loopTimes = Int32.Parse(args[0]);

        loopTimesLabel.text = "} " + loopTimes + " Times";
    }

    public void ApplyChangeToCodeObject()
    {
        if (attachedCodeObject != null)
        {
            string[] args = attachedCodeObject.GetArgs();
            args[0] = loopTimes.ToString();

            attachedCodeObject.SetArgs(args);
            attachedCodeObject.SetLoopTimes(loopTimes);

            // Clean up
            attachedCodeObject = null;
            editorDispatcher.DismissEditor();
        }
    }

    public void ChangeLoopTimes(int value)
    {
        if (loopTimes >= 0)
        {
            loopTimes += value;
        }
        loopTimesLabel.text = "} " + loopTimes + " Times";
    }

    // ====================

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return attachedCodeObject.GetNumOfNestedCmd();
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 100f;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        CodeObjectOneCommand co = attachedCodeObject.GetNestedCommands()[dataIndex];
        return null;
    }
}
