using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    public Transform avatar, remoteAvatar;
    public GridController gridController;
    private Vector3 cellPos;

    private const float AVATAR_Y_POS = 0.05f;

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

    /// <summary>
    /// Move the avatar one step in the given direction.
    /// </summary>
    /// <param name="dir"></param>
    public void Move(string dir)
    {
        Move(gridController.GetDirFromString(dir));
    }

    /// <summary>
    /// Move the avatar one step in the given direction.
    /// </summary>
    /// <param name="dir"></param>
    public void Move(GridController.Direction dir)
    {
        Vector3 nextCell = gridController.GetNextCellCoor(cellPos, dir);
        MoveToCell(nextCell);
    }

    /// <summary>
    /// Reset the position of the avatar to the origin.
    /// </summary>
    public void ResetPosition()
    {
        Vector3 avatarPos = gridController.GetTheFirstCellInGrid().localPosition;
        avatarPos.y = 0.05f;
        avatar.localPosition = avatarPos;
        // Assume the avatar is placed at 0, 0 on the grid coordinate.
        cellPos = new Vector3(0, 0, 0);

        // For the remote avatar
        var remoteAvatarPos = gridController.GetTheLastCellInGrid().localPosition;
        remoteAvatarPos.y = 0.05f;
        remoteAvatar.localPosition = remoteAvatarPos;
    }

    // ====================

    public void _TestAnything(int dir)
    {
        Move((GridController.Direction)dir);
    }
}
