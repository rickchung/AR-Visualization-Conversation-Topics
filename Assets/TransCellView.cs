using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UnityEngine.UI;

public class TransCellView : EnhancedScrollerCellView
{
    public Text conceptNameText;

    public void SetData(string data)
    {
        conceptNameText.text = data;
    }
}
