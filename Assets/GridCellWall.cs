using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCellWall : MonoBehaviour
{
    private GridCellType cellType = GridCellType.WALL;
    private GridCellUpdateDelegate updateDelegate;

    public GameObject cubeLevel;

    public void SetUpdateDelegate(GridCellUpdateDelegate d)
    {
        updateDelegate = d;
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
}
