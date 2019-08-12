using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// This controller controls the user's keyword list that is generated from
/// the transcripts. The keywords will be presented at the bottom of screen.
/// </summary>
public class TopicScroller : MonoBehaviour, IEnhancedScrollerDelegate
{
    private List<string> topicList;
    private List<string> speakerList;
    private float cellviewSize = 148f;

    public EnhancedScroller scroller;
    public TopicCellView cellview;
    public CodeInterpreter codeInterpreter;

    private string localColor, remoteColor;

    void Start()
    {
        topicList = new List<string>();
        speakerList = new List<string>();

        localColor = StatChartController.COLOR_PALETTE[0];
        remoteColor = StatChartController.COLOR_PALETTE[1];

        scroller.gameObject.SetActive(true);
        scroller.Delegate = this;
        scroller.ReloadData(scrollPositionFactor: 0.0f);
    }

    public void AddTopic(string topic, string speaker)
    {
        topicList.Insert(0, topic);
        speakerList.Insert(0, speaker);
        scroller.ReloadData(scrollPositionFactor: 0.0f);
    }

    public void Refresh()
    {
        scroller.ReloadData(scrollPositionFactor: 0.0f);
    }

    // ======================================================================

    int IEnhancedScrollerDelegate.GetNumberOfCells(EnhancedScroller scroller)
    {
        return topicList.Count;
    }

    float IEnhancedScrollerDelegate.GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellviewSize;
    }

    EnhancedScrollerCellView IEnhancedScrollerDelegate.GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        TopicCellView cellView = scroller.GetCellView(cellview) as TopicCellView;

        var speaker = speakerList[dataIndex];
        var color = (speaker == StatChartController.USERNAME_LOCAL ? localColor : remoteColor);
        var cellViewContent = string.Format(
            "<mark={0}aa>{1}:</mark> {2}", color, speaker, topicList[dataIndex]
        );
        cellView.SetData(cellViewContent);

        // Simulation effects (if available)
        if (codeInterpreter != null)
        {
            cellView.SetOnClickEvent(codeInterpreter.GetTopicButtonEvent(topicList[dataIndex]));
        }

        return cellView;
    }
}
