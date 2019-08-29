using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    public Transform avatar;
    public GridController gridController;
    public PartnerSocket partnerSocket;
    public bool isRival;
    private bool isDead;
    private Transform startingCellInGrid;
    private Vector3? startingCellInVec = null;
    private Vector3 cellPos;
    private const float AVATAR_Y_POS = 0.05f;

    public bool IsDead
    {
        get
        {
            return isDead;
        }

        set
        {
            isDead = value;
        }
    }

    /// <summary>
    /// Move the avatar one step in the given direction.
    /// </summary>
    /// <param name="dir"></param>
    public void Move(string dir)
    {
        MoveInDir(gridController.GetDirFromString(dir, mirror: false));
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
        // These two variables are updated here to ensure this procedure is taken
        // after the grid map is generated
        if (startingCellInGrid == null || startingCellInVec == null)
        {
            Transform sc = null;
            Vector3? vc = null;

            // If you are a master device,
            // - Player1's avatar is at (0, 0, 0)
            // - Rival's avatar is at (last, 0, last)
            // If you are a slave device, the positions are exchanged.
            if (partnerSocket.IsMaster)
            {
                if (!isRival)
                {
                    sc = gridController.GetTheFirstCellInGrid();
                    vc = new Vector3(0, 0, 0);
                }
                else
                {
                    sc = gridController.GetTheLastCellInGrid();
                    var tmp = gridController.GetSizeOfCoor();
                    vc = new Vector3(tmp.x - 1, 0, tmp.z - 1);
                }
            }
            else
            {
                if (!isRival)
                {
                    sc = gridController.GetTheLastCellInGrid();
                    var tmp = gridController.GetSizeOfCoor();
                    vc = new Vector3(tmp.x - 1, 0, tmp.z - 1);
                }
                else
                {
                    sc = gridController.GetTheFirstCellInGrid();
                    vc = new Vector3(0, 0, 0);
                }
            }

            startingCellInGrid = sc;
            startingCellInVec = vc;
        }

        // Reset the position
        Vector3 avatarPos = startingCellInGrid.localPosition;
        avatarPos.y = AVATAR_Y_POS;
        avatar.localPosition = avatarPos;
        avatar.localRotation = new Quaternion();
        cellPos = (Vector3)startingCellInVec;
        // Reset the physics
        var rigidbody = avatar.GetComponent<Rigidbody>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.constraints = (
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation)
        ;
    }

    // ====================

    public void _TestAnything(int dir)
    {
        MoveInDir((GridController.Direction)dir);
    }
}
