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

    [HideInInspector] public CodeViewUpdateDelegate codeViewUpdateDelegate;

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
        int newLoopTimes = loopTimes + value;
        if (newLoopTimes < 0)
            newLoopTimes = 0;


        DataLogger.Log(
            this.gameObject, LogTag.CODING,
            string.Format("Code Modified, loopTimes {0}, loopTimes {1}", newLoopTimes, loopTimes)
        );

        loopTimes = newLoopTimes;
        loopTimesLabel.text = "} " + loopTimes + " Times";

        // Apply the change to the code object
        attachedCodeObject.SetLoopTimes(loopTimes);

        if (codeViewUpdateDelegate != null)
            codeViewUpdateDelegate();
    }

    // ====================
    // EnhancedScroller interfaces for the nested command view

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
        cellView.codeViewUpdateDelegate = codeViewUpdateDelegate;
        return cellView;
    }
}
