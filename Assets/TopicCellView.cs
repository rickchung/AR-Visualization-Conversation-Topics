using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public delegate void TopicCellviewClickedDelegate(string value);

public class TopicCellView : EnhancedScrollerCellView
{
    public TMPro.TextMeshProUGUI mTopicName;
    public Button mTopicButton;

    public TopicCellviewClickedDelegate onClickedDelegate;

    public void SetData(string data)
    {
        mTopicName.text = data;
    }

    public void TopicCellButton_OnClick(string value)
    {
        if (onClickedDelegate != null)
        {
            onClickedDelegate(mTopicName.text);
        }
    }
}
