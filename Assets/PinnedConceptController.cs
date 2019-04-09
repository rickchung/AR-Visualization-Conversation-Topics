using System.Collections;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using UnityEngine;

public class PinnedConceptController : MonoBehaviour, IEnhancedScrollerDelegate {

    private List<ConceptData> _pinnedConcepts;

    public EnhancedScroller pinnedConceptScroller;
    public ConceptCellView conceptCellViewPrefab;

    void Start()
    {
        _pinnedConcepts = new List<ConceptData>();

        //_pinnedConcepts.Add(new ConceptData("For Loop"));
        //_pinnedConcepts.Add(new ConceptData("While Loop"));
        //_pinnedConcepts.Add(new ConceptData("Array"));
        //_pinnedConcepts.Add(new ConceptData("Nested Loop"));

        pinnedConceptScroller.Delegate = this;
        pinnedConceptScroller.ReloadData();
    }

    public void AddNewConcept(ConceptData concept)
    {
        _pinnedConcepts.Add(concept);
        pinnedConceptScroller.ReloadData();
    }

    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _pinnedConcepts.Count;
    }

    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return 150f;
    }

    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        ConceptCellView cellView = scroller.GetCellView(conceptCellViewPrefab) as ConceptCellView;
        cellView.SetData(_pinnedConcepts[dataIndex]);
        return cellView;
    }
}