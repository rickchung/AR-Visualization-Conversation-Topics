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

    private CodeObjectOneCommand attachedCodeObject;
    private int loopTimes;

    public void AttachCodeObject(CodeObjectOneCommand codeObject, List<string> argOptions)
    {
        attachedCodeObject = codeObject;
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
        throw new System.NotImplementedException();
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        throw new System.NotImplementedException();
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        throw new System.NotImplementedException();
    }
}
