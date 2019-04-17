using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// This controller controls the user's keyword list that is generated from
/// the transcripts. The keywords will be presented at the bottom of screen.
/// </summary>
public class ConceptController : MonoBehaviour, IEnhancedScrollerDelegate
{
    private List<ConceptData> _myConcepts;

    public EnhancedScroller myConceptScroller;
    public ConceptCellView conceptCellViewPrefab;

    public bool Enable2DScroller;

    void Start()
    {
        _myConcepts = new List<ConceptData>();
        //_myConcepts.Add(new ConceptData("Loop"));
        //_myConcepts.Add(new ConceptData("Variable"));
        //_myConcepts.Add(new ConceptData("Condition"));
        //_myConcepts.Add(new ConceptData("Flow Control"));
        //_myConcepts.Add(new ConceptData("Array"));
        //_myConcepts.Add(new ConceptData("File I/O"));

        if (Enable2DScroller)
        {
            myConceptScroller.gameObject.SetActive(true);
            myConceptScroller.Delegate = this;
            myConceptScroller.ReloadData();
        }
        else
        {
            myConceptScroller.gameObject.SetActive(false);
        }
    }

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _myConcepts.Count;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 150f;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        ConceptCellView cellView = scroller.GetCellView(conceptCellViewPrefab) as ConceptCellView;
        cellView.SetData(_myConcepts[dataIndex]);
        return cellView;
    }
}
