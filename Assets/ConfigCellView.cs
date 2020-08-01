using UnityEngine.UI;
using UnityEngine.Events;
using EnhancedUI.EnhancedScroller;

public class ConfigCellView : EnhancedScrollerCellView
{
    public Text textUI;
    public Button button;

    public void SetData(string text, UnityAction action)
    {
        textUI.text = text;
        button.onClick.AddListener(action);
    }

}
