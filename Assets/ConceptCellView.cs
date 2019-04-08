using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;
using UnityEngine.UI;

public class ConceptCellView : EnhancedScrollerCellView
{
    public Text conceptNameText;

    public void SetData(ConceptData data)
    {
        conceptNameText.text = data.conceptName;
    }
}
