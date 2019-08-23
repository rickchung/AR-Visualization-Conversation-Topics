using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    protected string cellTag;

    void Start()
    {
        SetCellTag("BaseCell");
    }

    public void SetCellTag(string tag)
    {
        cellTag = tag;
    }

    private void OnTriggerEnter(Collider other)
    {

    }

    private void OnTriggerExit(Collider other)
    {

    }
}
