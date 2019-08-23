using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public Transform gridStart, gridEnd;
    public Transform gridCellPrefab;
    public AvatarController avatarController, rivalAvatarController;
    private Vector3[,] cellCoordinates;  // The real-world map of the game.
    private Transform[,] cellObjsOnCoordinates;  // The map of grid cells
    private int numInX, numInZ;

    void Start()
    {
        Vector3 startingPoint = gridStart.localPosition;
        Vector3 endPoint = gridEnd.localPosition;
        float distX = Mathf.Abs(startingPoint.x - endPoint.x);
        float distZ = Mathf.Abs(startingPoint.z - endPoint.z);

        // Measure the size of one step and padding
        float stepSize = gridStart.localScale.x;
        float padding = stepSize * 0.10f;
        numInX = Mathf.CeilToInt(distX / (stepSize + padding)) + 1;
        numInZ = Mathf.CeilToInt(distZ / (stepSize + padding)) + 1;

        Debug.Log("Grid Step Size: " + stepSize);
        Debug.Log("Grid: numInX=" + numInX + ", numInZ=" + numInZ);

        cellCoordinates = new Vector3[numInX, numInZ];
        cellObjsOnCoordinates = new Transform[numInX, numInZ];

        // Generate grid cells
        for (int x = 0; x < numInX; x++)
        {
            for (int z = 0; z < numInZ; z++)
            {
                Transform newCell = (Transform)Instantiate(
                    original: gridCellPrefab,
                    parent: transform,
                    instantiateInWorldSpace: false
                );
                Vector3 newPos = startingPoint;
                newPos.x = newPos.x + x * (stepSize + padding);
                newPos.z = newPos.z - z * (stepSize + padding);
                newCell.transform.localPosition = newPos;
                newCell.name = "Cell" + x + z;

                cellCoordinates[x, z] = newPos;
                cellObjsOnCoordinates[x, z] = newCell;
            }
        }

        // Deactivate the starting and end cells
        gridStart.gameObject.SetActive(false);
        gridEnd.gameObject.SetActive(false);

        // When the grid is ready, reset the positions of avatars
        avatarController.ResetPosition();
        rivalAvatarController.ResetPosition();
    }

    // ====================
    // Coordinate-related

    public Transform GetTheLastCellInGrid()
    {
        return cellObjsOnCoordinates[numInX - 1, numInZ - 1];
    }

    public Transform GetTheFirstCellInGrid()
    {
        return cellObjsOnCoordinates[0, 0];
    }

    public Vector3 GetSizeOfCoor()
    {
        return new Vector3(numInX, 0, numInZ);
    }

    public enum Direction { NORTH = 0, SOUTH = 1, EAST = 2, WEST = 3, UNKNOWN }

    public static class DirectionVec
    {
        public static Vector3 NORTH = new Vector3(0, 0, -1);
        public static Vector3 SOUTH = new Vector3(0, 0, 1);
        public static Vector3 WEST = new Vector3(-1, 0, 0);
        public static Vector3 EAST = new Vector3(1, 0, 0);
    }

    /// <summary>
    /// Transforms a grid coordinate into the corresponding real-world coordinate (Vector3). If the result goes out of the boundary, the method returns null.
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

    /// <summary>
    /// Convert a direction in string into a value of the Direction type
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Direction GetDirFromString(string direction, bool mirror = false)
    {
        Direction dir = Direction.UNKNOWN;

        if (!mirror)
        {
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
        }
        else
        {
            switch (direction)
            {
                case "NORTH":
                    dir = Direction.SOUTH;
                    break;
                case "SOUTH":
                    dir = Direction.NORTH;
                    break;
                case "WEST":
                    dir = Direction.EAST;
                    break;
                case "EAST":
                    dir = Direction.WEST;
                    break;
            }
        }
        return dir;
    }


}
