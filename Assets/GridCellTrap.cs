using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellTrap : MonoBehaviour
{
    public GridController gridController;
    private float trapPower = 800f;
    private float trapTorque = 800f;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(string.Format("GRID, {0}, Trap Triggered", other.name));
        var rigidbody = other.GetComponent<Rigidbody>();
        rigidbody.constraints = RigidbodyConstraints.None;
        rigidbody.AddForce(Vector3.up * trapPower, ForceMode.Impulse);
        rigidbody.AddTorque(new Vector3(0f, 1f, 1f) * trapTorque, ForceMode.Impulse);
    }
}
