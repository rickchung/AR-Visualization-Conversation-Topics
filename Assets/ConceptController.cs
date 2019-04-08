﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI.EnhancedScroller;

public class ConceptController : MonoBehaviour, IEnhancedScrollerDelegate
{
    private List<ConceptData> _myConcepts;

    public EnhancedScroller myConceptScroller;
    public ConceptCellView conceptCellViewPrefab;

    void Start()
    {
        _myConcepts = new List<ConceptData>();

        _myConcepts.Add(new ConceptData("Loop"));
        _myConcepts.Add(new ConceptData("Variable"));
        _myConcepts.Add(new ConceptData("Condition"));
        _myConcepts.Add(new ConceptData("Flow Control"));
        _myConcepts.Add(new ConceptData("Array"));
        _myConcepts.Add(new ConceptData("File I/O"));

        myConceptScroller.Delegate = this;
        myConceptScroller.ReloadData();
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
