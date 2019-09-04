using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public enum GridCellType { BASE, TRAP, REWARD, REWARD_DUAL, WALL, WALL_2, WALL_3 };
public delegate void GridCellUpdateDelegate(GridCellType cellType, Collider other);

public class GridController : MonoBehaviour
{
    public Transform gridStart, gridEnd;
    public Transform gridCellPrefab, gridCellTargetPrefab, girdCellTrapPrefab, gridCellTargetDuoPrefab, gridCellWallPrefab;
    public AvatarController avatarController, rivalAvatarController;
    public CodeInterpreter codeInterpreter;
    public ConfManager confManager;

    private Vector3[,] cellVectorMap;  // The real-world map of the game.
    private Transform[,] cellObjectsMap;  // The map of grid cells
    private GridCellType[,] cellTypeMap;  // The map of grid cells with types
    private List<GridCellTarget> flagCells;  // A list of flag cells
    private List<GridCellTargetDual> dualFlagCells;  // A list of flag cells
    private int numFlagsCaptured;
    private int targetNumOfFlags;
    private int numInX, numInZ;
    private static string dataFolderPath;

    public int NumFlagsCaptured
    {
        get
        {
            return numFlagsCaptured;
        }

        set
        {
            numFlagsCaptured = value;
        }
    }

    public int TargetNumOfFlags
    {
        get
        {
            return targetNumOfFlags;
        }

        set
        {
            targetNumOfFlags = value;
        }
    }

    // ==================== Unity Lifecycle ====================

    void Start()
    {
        dataFolderPath = Application.persistentDataPath;
    }

    // ==================== Map Utilities ====================

    /// <summary>
    /// Generating a map with only white default cells.
    /// </summary>
    private void GenerateDefaultMap()
    {
        DataLogger.Log(this.gameObject, LogTag.MAP, "Loading a default map.");

        Vector3 startingPoint = gridStart.localPosition;
        Vector3 endPoint = gridEnd.localPosition;
        float distX = Mathf.Abs(startingPoint.x - endPoint.x);
        float distZ = Mathf.Abs(startingPoint.z - endPoint.z);

        // Measure the size of one step and padding
        float stepSize = gridStart.localScale.x;
        float padding = stepSize * 0.10f;
        numInX = Mathf.CeilToInt(distX / (stepSize + padding)) + 1;
        numInZ = Mathf.CeilToInt(distZ / (stepSize + padding)) + 1;

        DataLogger.Log(
            this.gameObject, LogTag.SYSTEM,
            "Grid Step Size: " + stepSize
        );
        DataLogger.Log(
            this.gameObject, LogTag.SYSTEM,
            "Grid: numInX=" + numInX + ", numInZ=" + numInZ
        );

        cellVectorMap = new Vector3[numInX, numInZ];
        cellObjectsMap = new Transform[numInX, numInZ];
        cellTypeMap = new GridCellType[numInX, numInZ];

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
                newCell.name = "CellClone";
                newCell.gameObject.SetActive(true);

                cellVectorMap[x, z] = newPos;
                cellObjectsMap[x, z] = newCell;
                cellTypeMap[x, z] = GridCellType.BASE;
            }
        }

        // Deactivate the starting and end cells
        gridStart.gameObject.SetActive(false);
        gridEnd.gameObject.SetActive(false);

        // When the grid is ready, reset the positions of avatars
        avatarController.ResetPosition();
        rivalAvatarController.ResetPosition();
    }

    private void GenerateMapFromCells(GridCellType[,] cells)
    {
        // Map geo info
        var startingPoint = gridStart.localPosition;
        var endPoint = gridEnd.localPosition;
        // var distX = Mathf.Abs(startingPoint.x - endPoint.x);
        // var distZ = Mathf.Abs(startingPoint.z - endPoint.z);
        var stepSize = gridStart.localScale.x;
        var padding = stepSize * 0.10f;
        numInX = cells.GetLength(0);
        numInZ = cells.GetLength(1);

        // Init map objects
        cellVectorMap = new Vector3[numInX, numInZ];
        cellObjectsMap = new Transform[numInX, numInZ];
        flagCells = new List<GridCellTarget>();
        dualFlagCells = new List<GridCellTargetDual>();
        NumFlagsCaptured = 0;
        TargetNumOfFlags = 0;
        cellTypeMap = cells;

        // Generate grid cells
        for (var x = 0; x < numInX; x++)
        {
            for (var z = 0; z < numInZ; z++)
            {
                var cellType = cellTypeMap[x, z];
                Transform newCell;
                switch (cellType)
                {
                    case GridCellType.REWARD:
                        newCell = (Transform)Instantiate(gridCellTargetPrefab, transform, false);
                        newCell.GetComponent<GridCellTarget>().SetUpdateDelegate(
                            GridCellUpdateCallback
                        );
                        flagCells.Add(newCell.GetComponent<GridCellTarget>());
                        TargetNumOfFlags += 1;
                        break;
                    case GridCellType.REWARD_DUAL:
                        newCell = (Transform)Instantiate(gridCellTargetDuoPrefab, transform, false);
                        newCell.GetComponent<GridCellTargetDual>().SetUpdateDelegate(
                            GridCellUpdateCallback
                        );
                        dualFlagCells.Add(newCell.GetComponent<GridCellTargetDual>());
                        TargetNumOfFlags += 2;
                        break;
                    case GridCellType.TRAP:
                        newCell = (Transform)Instantiate(girdCellTrapPrefab, transform, false);
                        newCell.GetComponent<GridCellTrap>().SetUpdateDelegate(
                            GridCellUpdateCallback
                        );
                        break;
                    case GridCellType.WALL:
                    case GridCellType.WALL_2:
                    case GridCellType.WALL_3:
                        newCell = (Transform)Instantiate(gridCellWallPrefab, transform, false);
                        newCell.GetComponent<GridCellWall>().SetHeight((int)cellType - 3);
                        break;
                    default:
                        newCell = (Transform)Instantiate(gridCellPrefab, transform, false);
                        break;
                }

                // Set the position
                Vector3 newPos = startingPoint;
                newPos.x = newPos.x + x * (stepSize + padding);
                newPos.z = newPos.z - z * (stepSize + padding);
                newCell.transform.localPosition = newPos;
                newCell.name = "CellClone";
                newCell.gameObject.SetActive(true);
                // Save for future references
                cellVectorMap[x, z] = newPos;
                cellObjectsMap[x, z] = newCell;
                cellTypeMap[x, z] = GridCellType.BASE;
            }
        }

        // Deactivate the starting and end cells
        gridStart.gameObject.SetActive(false);
        gridEnd.gameObject.SetActive(false);
        // When the grid is ready, reset the positions of avatars
        avatarController.UpdateStartingCells();
        avatarController.ResetPosition();
        rivalAvatarController.UpdateStartingCells();
        rivalAvatarController.ResetPosition();
    }

    public void LoadGridMap(string mapName)
    {
        isThisStageClear = false;
        RemoveGridMap();

        if (mapName.Equals("Default"))
        {
            GenerateDefaultMap();
        }
        else
        {
            var map = ImportGridAndProblem(mapName);
            GenerateMapFromCells(map);
        }

        // TODO: When a grid map is loaded, should it reset the positions of two avatars?
        //
        // Now the reset funciton is controlled asynchronously. It means a player can
        // only reset his/her own avatar. When a map is loaded, this function does not
        // reset the rival avatar and may result in a weird position of the rival avatar.
    }

    private void RemoveGridMap()
    {
        foreach (Transform o in transform)
            if (o.name.Equals("CellClone"))
                Destroy(o.gameObject);
    }

    public void ResetMap()
    {
        if (flagCells != null)
            foreach (var c in flagCells)
                c.Reset();

        if (dualFlagCells != null)
            foreach (var c in dualFlagCells)
                c.Reset();

        NumFlagsCaptured = 0;
    }

    public bool IsStageClear()
    {
        // TODO: This method should consider whether the avatar is dead or not.
        // TODO: This method should be synchronized between local and remote devices.
        return (numFlagsCaptured == targetNumOfFlags);
    }

    private bool isThisStageClear;
    private void GridCellUpdateCallback(GridCellType cellType, Collider other)
    {
        switch (cellType)
        {
            case GridCellType.TRAP:
                DataLogger.Log(this.gameObject, LogTag.MAP, "A trap is triggered.");

                var ac = other.GetComponent<AvatarController>();
                if (ac != null)
                {
                    ac.IsDead = true;
                    if (!ac.IsRival)
                    {
                        codeInterpreter.StopRunningScript();
                    }
                }
                break;
            case GridCellType.REWARD:
                DataLogger.Log(this.gameObject, LogTag.MAP, "A reward is collected.");
                NumFlagsCaptured += 1;
                break;
            case GridCellType.REWARD_DUAL:
                DataLogger.Log(this.gameObject, LogTag.MAP, "A dual reward is collected.");
                NumFlagsCaptured += 1;
                break;
            case GridCellType.BASE:
                break;
        }

        if (IsStageClear())
        {
            if (!isThisStageClear)
            {
                DataLogger.Log(this.gameObject, LogTag.SYSTEM, "Stage Clear!");
                confManager.EnableNextStage();
                isThisStageClear = true;
            }
        }
    }

    private static void ExportGridAndProblem(GridCellType[,] map, string mapName)
    {
        var cellNumInX = map.GetLength(0);
        var cellNumInZ = map.GetLength(1);
        var mapStr = cellNumInX + "\n" + cellNumInZ + "\n";
        for (int i = 0; i < cellNumInX; i++)
        {
            for (int j = 0; j < cellNumInZ; j++)
            {
                mapStr += ((int)map[i, j]).ToString();
            }
            mapStr += "\n";
        }

        var path = Path.Combine(dataFolderPath, mapName);
        using (var writer = new StreamWriter(path, false))
        {
            writer.WriteLine(mapStr);
            writer.Close();
        }

        DataLogger.Log(LogTag.SYSTEM, string.Format("SYSTEM, Export a map as {0}", path));
    }

    private static GridCellType[,] ImportGridAndProblem(string mapName)
    {
        var path = Path.Combine(dataFolderPath, mapName);

        using (var reader = new StreamReader(path))
        {
            var cellNumInX = int.Parse(reader.ReadLine());
            var cellNumInZ = int.Parse(reader.ReadLine());
            var importedCellMap = new GridCellType[cellNumInX, cellNumInZ];

            for (int i = 0; i < cellNumInX; i++)
            {
                var row = reader.ReadLine();
                var rowCells = row.ToCharArray();
                for (int j = 0; j < cellNumInZ; j++)
                {
                    importedCellMap[i, j] = (GridCellType)Char.GetNumericValue(rowCells[j]);
                }
            }

            DataLogger.Log(LogTag.SYSTEM, string.Format("MAP, A map is loaded: {0}", path));

            return importedCellMap;
        }
    }

    // ==================== Coordinate Transformation ====================

    public Transform GetTheLastCellInGrid()
    {
        return cellObjectsMap[numInX - 1, numInZ - 1];
    }

    public Transform GetTheFirstCellInGrid()
    {
        return cellObjectsMap[0, 0];
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
        if (x < 0 || z < 0 || x >= cellVectorMap.GetLength(0) || z >= cellVectorMap.GetLength(1))
            return null;
        return cellVectorMap[x, z];
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
