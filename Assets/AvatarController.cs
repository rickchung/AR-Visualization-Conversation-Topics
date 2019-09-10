﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarController : MonoBehaviour
{
    public Transform avatar;
    public GridController gridController;
    public PartnerSocket partnerSocket;
    public CodeInterpreter codeInterpreter;
    private bool isRival;
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

    public bool IsRival
    {
        get
        {
            return isRival;
        }

        set
        {
            isRival = value;
        }
    }

    /// <summary>
    /// This method accepts a command and args parsed by a CodeInterpreter and decide how to enact. Returns true when a command is accepted.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public virtual bool ParseCommand(string command, string[] args)
    {
        bool isSuccessful = false;

        if (!IsDead)
        {
            switch (command)
            {
                case "MOVE":
                    this.Move(args[0]);
                    isSuccessful = true;
                    break;
                default:
                    DataLogger.Log(
                        this.gameObject, LogTag.SYSTEM_WARNING,
                        "Command is rejected by the avatar, " + command
                    );
                    isSuccessful = false;
                    break;
            }
        }

        return isSuccessful;
    }

    public virtual bool IsLockCommand(string command)
    {
        return false;
    }

    public virtual bool IsStaticCommand(string command)
    {
        return false;
    }

    // ==================== Available Behaviors of an Avatar ====================

    /// <summary>
    /// Move the avatar one step in the given direction.
    /// </summary>
    /// <param name="dir"></param>
    private void Move(string dir)
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
        // If running out of the boundary
        else
        {
            DataLogger.Log(this.gameObject, LogTag.MAP,
                "The avatar exceeded the boundary (isRival = " + IsRival + ")"
            );
            IsDead = true;
            GridCellTrap.TriggerTrapEffect(avatar.GetComponent<Collider>());
            if (!IsRival) codeInterpreter.InterruptRunningScript();
        }
    }

    // ==================== General Behavior of an Avatar ====================

    /// <summary>
    /// Reset the position of the avatar to the origin.
    /// </summary>
    public virtual void ResetPosition()
    {
        // These two variables are updated here to ensure this procedure is taken
        // after the grid map is generated
        if (startingCellInGrid == null || startingCellInVec == null)
        {
            UpdateStartingCells();
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

    public void UpdateStartingCells()
    {
        Transform sc = null;
        Vector3? vc = null;

        // If you are a master device,
        // - Player1's avatar is at (0, 0, 0)
        // - Rival's avatar is at (last, 0, last)
        // If you are a slave device, the positions are exchanged.
        if (partnerSocket.IsMaster)
        {
            if (!IsRival)
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
            if (!IsRival)
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

    // ==================== Testing Functions ====================

    public void _TestAnything(int dir)
    {
        MoveInDir((GridController.Direction)dir);
    }
}
