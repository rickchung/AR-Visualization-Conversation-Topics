using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTarget : MonoBehaviour
{
    private GridCellType cellType = GridCellType.REWARD;
    private Transform flagPole;
    private GridCellUpdateDelegate updateDelegate;

    void Start()
    {
        flagPole = transform.Find("FlagPole");
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
                updateDelegate(this.cellType, other);
            }
        }
    }

    public void Reset()
    {
        flagPole.gameObject.SetActive(true);
    }
}
