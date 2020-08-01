using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Events;
using EnhancedUI.EnhancedScroller;

public class ConfigScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
{

    private List<OrderedDictionary> _configSet;

    public EnhancedScroller myScroller;
    public ConfigCellView configCellViewPrefab;
    
    public ConfManager confManager;

    void Start() {
        myScroller.Delegate = this;
    }

    public void SetData(List<OrderedDictionary> data) {
        _configSet = data;
        myScroller.ReloadData();
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        ConfigCellView cellView = scroller.GetCellView(configCellViewPrefab) as ConfigCellView;

        var text = "";
        foreach (DictionaryEntry kv in _configSet[dataIndex]) {
            text += (string)kv.Key + " ";
        }

        cellView.SetData(dataIndex + ": " + text, () => {confManager.LoadConfigSetAndStartGame(dataIndex);});
        return cellView;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 100f;
    }

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _configSet.Count;
    }
}
