using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UnityEngine.UI;
using UnityEngine.Events;

public class TopicCellView : EnhancedScrollerCellView
{
    public Text mTopicName;
    public Button mTopicButton;

    public void SetData(string data)
    {
        mTopicName.text = data;
    }

    public void SetOnClickEvent(UnityAction clickedEvent)
    {
        mTopicButton.onClick.AddListener(clickedEvent);
    }

    public void DisableButton()
    {
        mTopicButton.interactable = false;
    }
}
