using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UnityEngine.UI;

public class TopicCellView : EnhancedScrollerCellView
{
    public Text mTopicName;
    public Button mTopicButton;

    public void SetData(string data)
    {
        mTopicName.text = data;
    }
}
