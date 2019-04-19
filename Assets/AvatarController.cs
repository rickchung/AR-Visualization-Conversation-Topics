using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    public Transform avatar;
    public Transform origin;
    public GridController gridController;
    private Vector3 cellPos;

    private const float AVATAR_Y_POS = 0.05f;

    private void Start()
    {
        ResetPosition();
    }

    /// <summary>
    /// Move the avatar to the cellCoor which is represented in the grid coordinate.
    /// </summary>
    /// <param name="cellCoor">Cell coor.</param>
    private void MoveToCell(Vector3 cellCoor)
    {
        var newPos = gridController.TransformCellCoorToPos(
            (int)cellCoor.x, (int)cellCoor.z
        );

        if (newPos != null)
        {
            Vector3 _newPos = (Vector3)newPos;
            _newPos.y = AVATAR_Y_POS;
            avatar.localPosition = _newPos;
            cellPos = cellCoor;
        }
    }

    public void Move(string dir)
    {
        Move(gridController.GetDirFromString(dir));
    }

    public void Move(GridController.Direction dir)
    {
        Vector3 nextCell = gridController.GetNextCellCoor(cellPos, dir);
        MoveToCell(nextCell);
    }

    public void ResetPosition()
    {
        Vector3 avatarPos = origin.localPosition;
        avatarPos.y = 0.05f;
        avatar.localPosition = avatarPos;
        // Assume the avatar is placed at 0, 0 of the grid coordinate.
        cellPos = new Vector3(0, 0, 0);
    }

    public void _TestAnything(int dir)
    {
        Move((GridController.Direction)dir);
    }
}
