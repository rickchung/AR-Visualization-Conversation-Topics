using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellWall : MonoBehaviour, IGridCell
{
    private GridCellType cellType = GridCellType.WALL;
    private GridCellUpdateDelegate updateDelegate;
    private Vector3[] origin;
    public GameObject cubeLevel;


    void Start()
    {
        origin = new Vector3[] { this.transform.localPosition, this.transform.rotation.eulerAngles };
    }

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

    public void SetHeight(int numCube)
    {
        for (int i = 0; i < numCube; i++)
        {
            var cloneCube = Instantiate(
                cubeLevel,
                parent: this.transform,
                instantiateInWorldSpace: false
            ) as GameObject;

            var cloneCubePos = cloneCube.transform.localPosition;
            cloneCubePos.y += 15f * i;
            cloneCube.transform.localPosition = cloneCubePos;
            cloneCube.SetActive(true);
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
