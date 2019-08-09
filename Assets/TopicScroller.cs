﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// This controller controls the user's keyword list that is generated from
/// the transcripts. The keywords will be presented at the bottom of screen.
/// </summary>
public class TopicScroller : MonoBehaviour, IEnhancedScrollerDelegate
{
    private List<string> _topics;
    private float cellviewSize = 128f;

    public EnhancedScroller mTopicScroller;
    public TopicCellView mTopicCellView;
    public CodeInterpreter mCodeInterpreter;

    void Start()
    {
        _topics = new List<string>();

        mTopicScroller.gameObject.SetActive(true);
        mTopicScroller.Delegate = this;
        mTopicScroller.ReloadData();
    }

    public void ReplaceTopicsAndRefresh(IEnumerable<string> topics)
    {
        _topics.Clear();
        _topics.AddRange(topics);
        mTopicScroller.ReloadData();
    }

    // ======================================================================

    int IEnhancedScrollerDelegate.GetNumberOfCells(EnhancedScroller scroller)
    {
        return _topics.Count;
    }

    float IEnhancedScrollerDelegate.GetCellViewSize(
        EnhancedScroller scroller, int dataIndex)
    {
        return cellviewSize;
    }

    EnhancedScrollerCellView IEnhancedScrollerDelegate.GetCellView(
        EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        TopicCellView cellView = scroller.GetCellView(mTopicCellView) as TopicCellView;
        cellView.SetData(_topics[dataIndex]);

        // Simulation effects
        if (mCodeInterpreter != null)
            cellView.SetOnClickEvent(mCodeInterpreter.GetTopicButtonEvent(_topics[dataIndex]));

        return cellView;
    }
}
