﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public Transform exampleTarget;
    public Transform gridStart, gridEnd;
    public Transform gridCellPrefab;
    public VirtualBtnHandler vbPrefab;

    private Vector3[,] cellCoordinates;

    void Start()
    {
        Vector3 startingPoint = gridStart.localPosition;
        Vector3 endPoint = gridEnd.localPosition;
        float distX = Mathf.Abs(startingPoint.x - endPoint.x);
        float distZ = Mathf.Abs(startingPoint.z - endPoint.z);

        float stepSize = (
            gridStart.GetComponent<Renderer>().bounds.size.x
            / gridStart.parent.localScale.x);
        float padding = stepSize * 0.10f;
        int numInX = Mathf.CeilToInt(distX / (stepSize + padding)) + 1;
        int numInZ = Mathf.CeilToInt(distZ / (stepSize + padding)) + 1;

        Debug.Log("Grid: numInX=" + numInX + ", numInZ=" + numInZ);

        cellCoordinates = new Vector3[numInX, numInZ];

        // Generate grid cells
        for (int x = 0; x < numInX; x++)
        {
            for (int z = 0; z < numInZ; z++)
            {
                Transform newCell = (Transform)Instantiate(
                        original: gridCellPrefab,
                        parent: exampleTarget,
                        instantiateInWorldSpace: false
                );
                Vector3 newPos = startingPoint;
                newPos.x = newPos.x + x * (stepSize + padding);
                newPos.z = newPos.z - z * (stepSize + padding);
                newCell.transform.localPosition = newPos;
                newCell.name = "Cell" + x + z;

                cellCoordinates[x, z] = newPos;

                VirtualBtnHandler cellvb = Instantiate(vbPrefab, exampleTarget, false);
                cellvb.transform.localPosition = newCell.localPosition;
                cellvb.gameObject.name = "vb" + x + z;
            }
        }

        // Deactivate the starting and end cells
        vbPrefab.gameObject.SetActive(false);
        gridStart.gameObject.SetActive(false);
        gridEnd.gameObject.SetActive(false);
    }

    /// <summary>
    /// Transforms the position from the grid coordinate to the real coordinate.
    /// </summary>
    /// <returns>The cell to real.</returns>
    /// <param name="x">The x coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    public Vector3? TransformCellCoorToPos(int x, int z)
    {
        if (x < 0 || z < 0 || x >= cellCoordinates.GetLength(0) || z >= cellCoordinates.GetLength(1))
            return null;
        return cellCoordinates[x, z];
    }

    /// <summary>
    /// Gets the next cell position. Note the cell position is represented by
    /// the grid coordinates. For example, 1, 0, 2 means the cell at index 1, 2.
    /// </summary>
    /// <returns>The next cell position.</returns>
    /// <param name="currentPos">Current position.</param>
    /// <param name="direction">Direction.</param>
    public Vector3 GetNextCellCoor(Vector3 currentPos, Direction direction)
    {
        Vector3 newPos = currentPos;
        switch (direction)
        {
            case Direction.NORTH:
                newPos += DirectionVec.NORTH;
                break;
            case Direction.SOUTH:
                newPos += DirectionVec.SOUTH;
                break;
            case Direction.EAST:
                newPos += DirectionVec.EAST;
                break;
            case Direction.WEST:
                newPos += DirectionVec.WEST;
                break;
        }
        return newPos;
    }

    public Direction GetDirFromString(string direction)
    {
        Direction dir = Direction.UNKNOWN;
        switch (direction)
        {
            case "NORTH":
                dir = Direction.NORTH;
                break;
            case "SOUTH":
                dir = Direction.SOUTH;
                break;
            case "WEST":
                dir = Direction.WEST;
                break;
            case "EAST":
                dir = Direction.EAST;
                break;
        }
        return dir;
    }



    public enum Direction { NORTH = 0, SOUTH = 1, EAST = 2, WEST = 3, UNKNOWN }

    public static class DirectionVec
    {
        public static Vector3 NORTH = new Vector3(0, 0, -1);
        public static Vector3 SOUTH = new Vector3(0, 0, 1);
        public static Vector3 WEST = new Vector3(-1, 0, 0);
        public static Vector3 EAST = new Vector3(1, 0, 0);
    }
}