using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class HelicopterController : AvatarController
{
    private Transform topRotor, tailRotor, cockpit, helicopter;
    private Rigidbody rbTopRotor, rbTailRotor, rbHelicopter;
    private Quaternion topOrgRot, tailOrgRot;
    private Vector3 topOrgPos, tailOrgPos;

    private void Start()
    {
        helicopter = transform;
        topRotor = helicopter.Find("Top_rotor");
        tailRotor = helicopter.Find("Tail_Rotor");
        cockpit = helicopter.Find("Cockpit");
        rbHelicopter = helicopter.GetComponent<Rigidbody>();
        rbTopRotor = topRotor.GetComponent<Rigidbody>();
        rbTailRotor = tailRotor.GetComponent<Rigidbody>();

        topOrgRot = topRotor.localRotation;
        tailOrgRot = tailRotor.localRotation;
        topOrgPos = topRotor.localPosition;
        tailOrgPos = tailRotor.localPosition;

        poutTopRotor = poutTailRotor = boutTopRotor = boutTailRotor = 1;

        gameObject.SetActive(false);
    }

    override public bool ParseCommand(string command, string[] args)
    {
        bool isSuccessful = false;

        if (!IsDead)
        {
            float value;
            switch (command)
            {
                // Engine(start); Engine(stop);
                case CMD_ENGINE:
                    switch (args[0])
                    {
                        case "start":
                            StartEngine();
                            break;
                        case "stop":
                            StopEngine();
                            break;
                    }
                    break;
                // Climb(up, 10); Climb(down, 10);
                case CMD_CLIMB:
                    switch (args[0])
                    {
                        case "up":
                            SetPowerOutputTopRotor(4);
                            ClimbUp();
                            break;
                        case "down":
                            SetBrakeOutputTopRotor(2);
                            FallDown();
                            break;
                    }
                    break;
                // Move(forward); Move(backward);
                case CMD_MOVE:
                    switch (args[0])
                    {
                        case "forward":
                            MoveForward();
                            break;
                        case "backward":
                            MoveBackward();
                            break;
                    }
                    break;
                // Turn(left); Turn(right);
                case CMD_TURN:
                    switch (args[0])
                    {
                        case "left":
                            HoveringTurnLeft();
                            break;
                        case "right":
                            HoveringTurnRight();
                            break;
                    }
                    break;

                case CMD_START_ENG:
                    StartEngine();
                    break;
                case CMD_STOP_ENG:
                    StopEngine();
                    break;
                case CMD_CLIMP_UP:
                    ClimbUp();
                    break;
                case CMD_FALL_DOWN:
                    FallDown();
                    break;
                case CMD_MOVE_FORWARD:
                    MoveForward();
                    break;
                case CMD_MOVE_BACKWARD:
                    MoveBackward();
                    break;
                case CMD_SLOWDOWN_TAIL:
                    HoveringTurnRight();
                    break;
                case CMD_SPEEDUP_TAIL:
                    HoveringTurnLeft();
                    break;

                case CMD_TOP_POWER:
                    value = float.Parse(args[0]);
                    SetPowerOutputTopRotor(value);
                    break;
                case CMD_TAIL_POWER:
                    value = float.Parse(args[0]);
                    SetPowerOutputTailRotor(value);
                    break;
                case CMD_TOP_BRAKE:
                    value = float.Parse(args[0]);
                    SetBrakeOutputTopRotor(value);
                    break;
                case CMD_TAIL_BRAKE:
                    value = float.Parse(args[0]);
                    SetBrakeOutputTailRotor(value);
                    break;
            }
        }

        return isSuccessful;
    }

    private Dictionary<string, string[]> availableCmdArgs = new Dictionary<string, string[]>
    {
        {CMD_CLIMB, new string[] {"up", "down"}},
        {CMD_ENGINE, new string[] {"start", "stop"}},
        {CMD_TURN, new string[] {"left", "right"}},
        {CMD_MOVE, new string[] {"forward", "backword"}}
    };
    override public string[] GetAvailableArgsForCmd(string command)
    {
        if (availableCmdArgs.ContainsKey(command))
            return availableCmdArgs[command];
        return null;
    }
    override public Dictionary<string, string[]> GetAvailableCmdArgs()
    {
        return availableCmdArgs;
    }

    override public bool IsLockCommand(string command)
    {
        switch (command)
        {
            case CMD_WAIT_P1:
            case CMD_WAIT_P2:
            case CMD_WAIT_PX:
                return true;
        }
        return false;
    }

    override public bool IsStaticCommand(string command)
    {
        // switch (command)
        // {
        //     case CMD_TOP_POWER:
        //     case CMD_TAIL_POWER:
        //     case CMD_TOP_BRAKE:
        //     case CMD_TAIL_BRAKE:
        //         return true;
        // }
        // return false;
        return true;
    }

    override public void ResetPosition()
    {
        base.ResetPosition();

        helicopter.localRotation = Quaternion.Euler(0, 180, 0);
        var pos = helicopter.localPosition;
        pos.y = 0.015f;
        helicopter.localPosition = pos;

        topRotor.localRotation = topOrgRot;
        tailRotor.localRotation = tailOrgRot;
        topRotor.localPosition = topOrgPos;
        tailRotor.localPosition = tailOrgPos;

        poutTopRotor = poutTailRotor = boutTopRotor = boutTailRotor = 1;

        // Reset the physics
        StopEngine();
        forwardAcc = upAcc = torqueAcc = curAltitudeLv = 0;
        rbHelicopter.velocity = Vector3.zero;
        rbHelicopter.angularVelocity = Vector3.zero;
        rbHelicopter.constraints = RigidbodyConstraints.None;
    }

    // ==================== Engine Control Unit ====================

    public void SetPowerOutputTopRotor(float value)
    {
        poutTopRotor = value;
    }

    public void SetPowerOutputTailRotor(float value)
    {
        poutTailRotor = value;
    }

    public void SetBrakeOutputTopRotor(float value)
    {
        boutTopRotor = value;
    }

    public void SetBrakeOutputTailRotor(float value)
    {
        boutTailRotor = value;
    }

    // ==================== Pilot Control System ====================

    private bool isEngineOn = false;
    private float poutTopRotor = 1.0f;
    private float poutTailRotor = 1.0f;
    private float boutTopRotor = 1.0f;
    private float boutTailRotor = 1.0f;
    private float scaleHoverTurn = 0.33f;
    private float scaleTiltAngle = 10.0f;

    private float upAcc, effectiveUpAcc, forwardAcc, torqueAcc;
    private const float SCALE_ALTITUDE_MAX = 0.2f, COEF_ATTITUDE = -1;
    private int curAltitudeLv = 0;
    private const float SCALE_TORQUE = 20f;
    private float SCALE_UPACC = 25f;
    private float SCALE_DOWNACC = 10f;
    private const float SCALE_FORWARD_ACC = 30f;

    public void StartEngine()
    {
        isEngineOn = true;
        rbTopRotor.AddTorque(10f * helicopter.up, ForceMode.Impulse);
        rbTailRotor.AddTorque(10f * -helicopter.right, ForceMode.Impulse);
    }
    public void StopEngine()
    {
        isEngineOn = false;
        rbTopRotor.angularVelocity = Vector3.zero;
        rbTailRotor.angularVelocity = Vector3.zero;
        upAcc = forwardAcc = 0;
        rbHelicopter.velocity = 10 * SCALE_UPACC * -Vector3.up;
    }

    public void ClimbUp()
    {
        curAltitudeLv += 1;
        SpeedUpTopRotor(poutTopRotor);
        // SpeedUpTailRotor(poutTailRotor);
    }
    public void FallDown()
    {
        curAltitudeLv -= 1;
        SlowDownTopRotor(boutTopRotor);
        // SlowDownTailRotor(boutTailRotor);
    }

    public void HoveringTurnRight()
    {
        SpeedUpTailRotor(-scaleHoverTurn);
    }
    public void HoveringTurnLeft()
    {
        SpeedUpTailRotor(scaleHoverTurn);
    }

    public void MoveForward()
    {
        if (forwardAcc <= 0)
            TiltTopRotor(helicopter.right, scaleTiltAngle);
    }
    public void MoveBackward()
    {
        if (forwardAcc >= 0)
            TiltTopRotor(helicopter.right, -scaleTiltAngle);
    }

    // ==================== Internal Behavior ====================

    private void FixedUpdate()
    {
        if (upAcc > 0)
        {
            effectiveUpAcc = (
                upAcc * Mathf.Exp(
                    COEF_ATTITUDE * ((helicopter.localPosition.y / SCALE_ALTITUDE_MAX) * curAltitudeLv)
                )
            );
            rbHelicopter.AddForce(effectiveUpAcc * helicopter.up, ForceMode.Acceleration);
        }
        else
        {
            rbHelicopter.AddForce(upAcc * helicopter.up, ForceMode.Acceleration);
        }

        rbHelicopter.AddTorque(torqueAcc * helicopter.up, ForceMode.Acceleration);

        // Hack forward acceleration
        rbHelicopter.AddForce(forwardAcc * cockpit.forward, ForceMode.Acceleration);

        // // TODO: Ensure the tail rotor has the right rotation
        // var trEular = tailRotor.localEulerAngles;
        // trEular.y = 0; trEular.z = 0;
        // tailRotor.localEulerAngles = trEular;
    }

    private void SpeedUpTopRotor(float amount)
    {
        if (isEngineOn)
        {
            if (upAcc < 0) upAcc = 0;
            upAcc += -Physics.gravity.y * amount * SCALE_UPACC;
            // torqueAcc += SCALE_TORQUE * amount;
        }
    }
    private void SlowDownTopRotor(float amount)
    {
        if (isEngineOn)
        {
            if (upAcc > 0) upAcc = 0;
            upAcc += Physics.gravity.y * amount * SCALE_DOWNACC;
            // torqueAcc -= SCALE_TORQUE * amount;
        }
    }

    /// <summary>
    /// To simplify physics, speeding up the tail rotor only decreases torque of ther helicopter.
    /// </summary>
    /// <param name="amount"></param>
    private void SpeedUpTailRotor(float amount)
    {
        if (isEngineOn)
        {
            torqueAcc -= SCALE_TORQUE * amount;
        }
    }
    /// <summary>
    /// To simplify physics, slowing down the tail rotor only increases torque of the helicopter.
    /// </summary>
    /// <param name="amount"></param>
    private void SlowDownTailRotor(float amount)
    {
        if (isEngineOn)
        {
            torqueAcc += SCALE_TORQUE * amount;
        }
    }

    private void TiltTopRotor(Vector3 axis, float angle)
    {
        if (isEngineOn)
        {
            var vectorAngle = axis * angle;

            // helicopter.Rotate(angles, Space.Self);
            var rot = helicopter.localRotation;
            helicopter.localRotation = Quaternion.Euler(vectorAngle) * rot;

            // Correct rotation of the top rotor
            rbTopRotor.angularVelocity = Vector3.zero;
            rbTopRotor.AddTorque(10f * helicopter.up, ForceMode.Impulse);

            // Supply extra forward acceleration
            if (angle > 0)
            {
                forwardAcc += SCALE_FORWARD_ACC * -Physics.gravity.y;
            }
            else if (angle < 0)
            {
                forwardAcc -= SCALE_FORWARD_ACC * -Physics.gravity.y;
            }

            // Stablize the value
            if (forwardAcc < 10e-4 && forwardAcc > -10e-4)
                forwardAcc = 0;
        }
    }


    // ==================== Available Method Names ====================
    // Whether a code is execuable is determined by the method "ParseCommand" above.

    // V2 Commands
    public const string CMD_MOVE = "Move";
    public const string CMD_CLIMB = "Climb";
    public const string CMD_ENGINE = "Engine";
    public const string CMD_TURN = "Turn";
    public const string CMD_WAIT_PX = "...";

    // V1 Commands
    public const string CMD_START_ENG = "Start_Engine";
    public const string CMD_STOP_ENG = "Stop_Engine";
    public const string CMD_CLIMP_UP = "Climb_Up";
    public const string CMD_FALL_DOWN = "Fall_Down";
    public const string CMD_MOVE_FORWARD = "Move_Forward";
    public const string CMD_MOVE_BACKWARD = "Move_Backward";
    public const string CMD_SLOWDOWN_TAIL = "SlowDown_Tail";
    public const string CMD_SPEEDUP_TAIL = "SpeedUp_Tail";

    public const string CMD_TOP_POWER = "Set_TopSpeed";
    public const string CMD_TAIL_POWER = "Set_TailSpeed";
    public const string CMD_TOP_BRAKE = "Set_TopBrake";
    public const string CMD_TAIL_BRAKE = "Set_TailBrake";

    // Used to control the synchronization
    public const string CMD_WAIT_P1 = "Wait_For_P1";
    public const string CMD_WAIT_P2 = "Wait_For_P2";

    // Regex for parsing code in text files
    private const string REGEX_NOARG_CMD = @"(?<cmd>{0}) \(\);";
    private const string REGEX_ONEARG_CMD = @"(?<cmd>{0}) \((?<param>[\w\d\.]+)\);";
    public static List<Regex> GetNoParamCmdRegex()
    {
        string[] cmds = {
            CMD_START_ENG, CMD_STOP_ENG,
            CMD_CLIMP_UP, CMD_FALL_DOWN,
            CMD_MOVE_FORWARD, CMD_MOVE_BACKWARD,
            CMD_SLOWDOWN_TAIL, CMD_SPEEDUP_TAIL,
            CMD_WAIT_P1, CMD_WAIT_P2,
        };

        var rt = new List<Regex>();
        foreach (var i in cmds)
            rt.Add(new Regex(string.Format(REGEX_NOARG_CMD, i)));

        rt.Add(new Regex(@"(?<cmd>\.\.\.)"));

        return rt;
    }
    public static List<Regex> GetOneParamCmdRegex()
    {
        string[] cmds = {
            CMD_TOP_POWER, CMD_TAIL_POWER,
            CMD_TOP_BRAKE, CMD_TAIL_BRAKE,
            CMD_ENGINE, CMD_MOVE, CMD_CLIMB, CMD_TURN,
        };

        var rt = new List<Regex>();
        foreach (var i in cmds)
            rt.Add(new Regex(string.Format(REGEX_ONEARG_CMD, i)));

        return rt;
    }

    /// <summary>
    /// Return the list of modifiable commands of this avatar. A modifiable command is a command that can be modified in the code editor. When the command is added into the editor, the dropdown list will be activated and let the user to change the command of a code block.
    /// </summary>
    /// <returns></returns>
    override public List<string> GetModifiableCmds()
    {
        List<string> modifiableCmds = new List<string>() {
            CMD_ENGINE, CMD_MOVE, CMD_TURN, CMD_CLIMB,

            // CMD_START_ENG, CMD_STOP_ENG,
            // CMD_CLIMP_UP, CMD_FALL_DOWN,
            // CMD_MOVE_FORWARD, CMD_MOVE_BACKWARD,
            // CMD_SLOWDOWN_TAIL, CMD_SPEEDUP_TAIL,
            // CMD_TOP_POWER, CMD_TAIL_POWER,
            // CMD_TOP_BRAKE, CMD_TAIL_BRAKE,

            CodeInterpreter.CMD_WAIT,
        };
        return modifiableCmds;
    }

    /// <summary>
    /// Return the list of commands with arguments. In a code editor, a command with arguments is initialized differently from the one without arguments. This list is therefore used to check whether a command has arguments or not.
    ///
    /// TODO: Rename this method.
    /// </summary>
    /// <returns></returns>
    override public List<string> GetModifiableCmdsWithArgs()
    {
        List<string> modifiableCmdsWtArgs = new List<string>()
        {
            CMD_TOP_POWER, CMD_TAIL_POWER,
            CMD_TOP_BRAKE, CMD_TAIL_BRAKE,

            CodeInterpreter.CMD_WAIT,
        };
        return modifiableCmdsWtArgs;
    }
}
