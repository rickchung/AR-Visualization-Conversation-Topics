﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTrap : MonoBehaviour, IGridCell
{
    private GridCellType cellType = GridCellType.TRAP;
    private static float trapPower = 800f;
    private static float trapTorque = 800f;
    private GridCellUpdateDelegate updateDelegate;

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        DataLogger.Log(
            this.gameObject, LogTag.MAP,
            string.Format("Trap triggered by {0}", other.name)
        );

        TriggerTrapEffect(other);

        if (updateDelegate != null)
        {
            updateDelegate(this, other);
        }
    }

    public static void TriggerTrapEffect(Collider other)
    {
        var rigidbody = other.GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.None;
        rigidbody.AddForce(Vector3.up * trapPower, ForceMode.Impulse);
        rigidbody.AddTorque(new Vector3(0f, 1f, 1f) * trapTorque, ForceMode.Impulse);
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

    public void Reset() {
        // Do nothing
    }

}
