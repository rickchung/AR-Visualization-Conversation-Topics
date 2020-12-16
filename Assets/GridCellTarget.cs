using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTarget : MonoBehaviour, IGridCell
{
    private GridCellType cellType = GridCellType.REWARD;
    private Transform flagPole;
    private GridCellUpdateDelegate updateDelegate;

    public bool WasEatenBefore { get; set; }

    void Start()
    {
        flagPole = transform.Find("FlagPole");
        WasEatenBefore = false;
    }

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (flagPole.gameObject.activeSelf)
        {
            DataLogger.Log(
                this.gameObject, LogTag.MAP,
                string.Format("Target Collected by {0}", other.name)
            );
            flagPole.gameObject.SetActive(false);
            if (updateDelegate != null)
            {
                WasEatenBefore = true;
                updateDelegate(this, other);
            }
        }
    }

    public void Reset()
    {
        if (flagPole != null)
            flagPole.gameObject.SetActive(true);
    }

    public GridCellType GetCellType()
    {
        return this.cellType;
    }

    public Vector3 GetCellPosition()
    {
        return this.transform.localPosition;
    }

    public Transform GetCell()
    {
        return this.transform;
    }
}
