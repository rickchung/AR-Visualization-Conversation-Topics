﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    public Transform avatar;
    public GridController gridController;
    public bool isRival;
    private Transform startingCellInGrid;
    private Vector3 startingCellInVec;
    private Vector3 cellPos;
    private const float AVATAR_Y_POS = 0.05f;

    /// <summary>
    /// Move the avatar one step in the given direction.
    /// </summary>
    /// <param name="dir"></param>
    public void Move(string dir)
    {
        MoveInDir(gridController.GetDirFromString(dir));
    }

    /// <summary>
    /// Move the avatar one step in the given direction.
    /// </summary>
    /// <param name="dir"></param>
    private void MoveInDir(GridController.Direction dir)
    {
        Vector3 nextCell = gridController.GetNextCellCoor(cellPos, dir);
        MoveToCell(nextCell);
    }

    /// <summary>
    /// Move the avatar to the cellCoor which is represented in the grid coordinate.
    /// </summary>
    /// <param name="cellCoor">Cell coor.</param>
    private void MoveToCell(Vector3 cellCoor)
    {
        var newPos = gridController.TransformCellCoorToPos((int)cellCoor.x, (int)cellCoor.z);
        if (newPos != null)
        {
            Vector3 _newPos = (Vector3)newPos;
            _newPos.y = AVATAR_Y_POS;
            avatar.localPosition = _newPos;
            cellPos = cellCoor;
        }
    }

    /// <summary>
    /// Reset the position of the avatar to the origin.
    /// </summary>
    public void ResetPosition()
    {
        if (startingCellInGrid == null || startingCellInVec == null)
        {
            if (!isRival)
            {
                startingCellInGrid = gridController.GetTheFirstCellInGrid();
                startingCellInVec = new Vector3(0, 0, 0);
            }
            else
            {
                startingCellInGrid = gridController.GetTheLastCellInGrid();
                var tmp = gridController.GetSizeOfCoor();
                startingCellInVec = new Vector3(tmp.x - 1, 0, tmp.z - 1);
            }
        }

        Vector3 avatarPos = startingCellInGrid.localPosition;
        avatarPos.y = 0.05f;
        avatar.localPosition = avatarPos;
        cellPos = startingCellInVec;
    }

    // ====================

    public void _TestAnything(int dir)
    {
        MoveInDir((GridController.Direction)dir);
    }
}
