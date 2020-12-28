using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellFloatingWall : MonoBehaviour, IGridCell
{
    private GridCellType cellType = GridCellType.WALL;
    private GridCellUpdateDelegate updateDelegate;
    private Vector3[] origin;

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
                    updateDelegate(this, other);
            }
        }
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

    public void Reset()
    {
        // Reset this object itself
        if (origin == null)
        {
            origin = new Vector3[] { this.transform.localPosition, this.transform.rotation.eulerAngles };
        }
        this.transform.localPosition = origin[0];
        this.transform.localRotation = Quaternion.identity;
        this.transform.Rotate(origin[1], Space.Self);
        this.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        this.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
}
