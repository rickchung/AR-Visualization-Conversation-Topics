using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;

public class LoopEditingArea : MonoBehaviour, EditingArea, IEnhancedScrollerDelegate
{
    public CodeEditor editorDispatcher;
    public TextMeshProUGUI loopTimesLabel;
    public Button loopTimesPlusBtn, loopTimesMinusBtn;
    public Button loopSubmitBtn;
    public OneCmdEditingAreaCellView oneCmdEditingAreaPrefab;
    public EnhancedScroller nestedCmdContainer;

    private CodeObjectLoop attachedCodeObject;
    private int loopTimes;

    private void Start()
    {
        nestedCmdContainer.Delegate = this;
        nestedCmdContainer.cellViewWillRecycle = (EnhancedScrollerCellView cv) =>
        {
            // This will remove the attached CodeObject in CellViews that
            // are going to be recycled.
            var tmp = (OneCmdEditingAreaCellView)cv;
            tmp.RemoveAttachedCo();
        };
    }

    public void AttachCodeObject(CodeObjectOneCommand codeObject, bool showSubmitBtn = true)
    {
        attachedCodeObject = (CodeObjectLoop)codeObject;

        string command = attachedCodeObject.GetCommand();
        string[] args = attachedCodeObject.GetArgs();
        loopTimes = Int32.Parse(args[0]);

        loopTimesLabel.text = "} " + loopTimes + " Times";

        nestedCmdContainer.ClearRecycled();
        nestedCmdContainer.ReloadData();
    }

    public void DismissEditor()
    {
        if (attachedCodeObject != null)
        {
            // Clean up
            nestedCmdContainer.ClearRecycled();
            attachedCodeObject = null;
            editorDispatcher.DismissEditor();
        }
    }

    public void ChangeLoopTimes(int value)
    {
        loopTimes += value;
        if (loopTimes < 0)
            loopTimes = 0;
        loopTimesLabel.text = "} " + loopTimes + " Times";

        // Apply the change to the code object
        string[] args = attachedCodeObject.GetArgs();
        args[0] = loopTimes.ToString();
        attachedCodeObject.SetArgs(args);
        attachedCodeObject.SetLoopTimes(loopTimes);
    }

    // ====================

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return attachedCodeObject.GetNumOfNestedCmd();
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 150f;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        var cellView = scroller.GetCellView(oneCmdEditingAreaPrefab)
            as OneCmdEditingAreaCellView;
        var nestedCode = attachedCodeObject.GetNestedCommands()[dataIndex];
        cellView.AttachCodeObject(nestedCode);
        return cellView;
    }
}
