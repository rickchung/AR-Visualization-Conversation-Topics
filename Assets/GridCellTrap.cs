using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTrap : MonoBehaviour
{
    private GridCellType cellType = GridCellType.TRAP;
    private float trapPower = 800f;
    private float trapTorque = 800f;
    private GridCellUpdateDelegate updateDelegate;

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(string.Format("GRID, {0}, Trap Triggered", other.name));
        var rigidbody = other.GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.None;
        rigidbody.AddForce(Vector3.up * trapPower, ForceMode.Impulse);
        rigidbody.AddTorque(new Vector3(0f, 1f, 1f) * trapTorque, ForceMode.Impulse);

        if (updateDelegate != null)
        {
            updateDelegate(this.cellType);
        }
    }
}
