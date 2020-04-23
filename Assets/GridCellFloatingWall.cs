using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellFloatingWall : MonoBehaviour
{
    private GridCellType cellType = GridCellType.WALL;
    private GridCellUpdateDelegate updateDelegate;

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        var heli = other.GetComponent<HelicopterController>();
        if (heli != null)
        {
            if (!heli.IsDead)
            {
                DataLogger.Log(
                    this.gameObject, LogTag.MAP,
                    string.Format("Trap triggered by {0}", other.name)
                );

                heli.StopEngine();

                if (updateDelegate != null)
                    updateDelegate(this.cellType, other);
            }
        }
    }
}
