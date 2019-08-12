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
    public Transform problemSolvingWorkspace;
    private Transform cardTemplate;

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

        cardTemplate = problemSolvingWorkspace.Find("CardTemplate");
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

        cellView.onClickedDelegate = TopicCellClicked;

        return cellView;
    }

    private void TopicCellClicked(string value)
    {
        Transform cardClone = Instantiate(cardTemplate);
        cardClone.SetParent(problemSolvingWorkspace);
        cardClone.localPosition = new Vector3(0, 0, -5f);
        cardClone.localRotation = new Quaternion(0, 0, 0, 0);
        TMPro.TextMeshProUGUI text = cardClone.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        text.text = value;
        cardClone.gameObject.SetActive(true);
    }
}
