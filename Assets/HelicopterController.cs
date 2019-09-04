using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterController : AvatarController
{
    private Transform topRotor, tailRotor, cockpit, helicopter;
    private Rigidbody rbTopRotor, rbTailRotor, rbHelicopter;

    private void Start()
    {
        helicopter = transform;
        topRotor = helicopter.Find("Top_rotor");
        tailRotor = helicopter.Find("Tail_Rotor");
        cockpit = helicopter.Find("Cockpit");
        rbHelicopter = helicopter.GetComponent<Rigidbody>();
        rbTopRotor = topRotor.GetComponent<Rigidbody>();
        rbTailRotor = tailRotor.GetComponent<Rigidbody>();
    }

    // ==================== Pilot Control System ====================

    // This system is defined by functions in the ECU.

    // ==================== Engine Control Unit ====================

    private bool isEngineOn = false;

    private float
        powerCoefTopRotor = 1.0f, powerCoefTailRotor = 1.0f,
        brakeCoefTopRotor = 1.0f, brakeCoefTailRotor = 1.0f;

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
        upAcc = 0;
        rbHelicopter.velocity = 500 * SCALE_UPACC * -Vector3.up;
    }

    public void ClimbUp()
    {
        SpeedUpTopRotor(powerCoefTopRotor);
        SpeedUpTailRotor(powerCoefTailRotor);
    }

    public void FallDown()
    {
        SlowDownTopRotor(brakeCoefTopRotor);
        SlowDownTailRotor(brakeCoefTailRotor);
    }

    private float scaleHoverTurn = 0.05f;
    public void HovorTurnRight()
    {
        SpeedUpTailRotor(scaleHoverTurn);
    }

    public void HovorTurnLeft()
    {
        SpeedUpTailRotor(-scaleHoverTurn);
    }

    private float scaleTiltAngle = 10.0f;
    public void MoveForward()
    {
        TiltTopRotor(helicopter.right, scaleTiltAngle);
    }
    public void MoveBackward()
    {
        TiltTopRotor(helicopter.right, -scaleTiltAngle);
    }

    // ==================== Internal Behavior ====================

    private float upAcc, forwardAcc, torqueAcc;
    private const float SCALE_ALTITUDE_MAX = 0.6f, COEF_ATTITUDE = -10;
    private const float
        SCALE_TORQUE = 20f,
        SCALE_UPACC = 100f,
        SCALE_FORWARD_ACC = 10f;

    private void FixedUpdate()
    {
        var effectiveUpAcc = (
            upAcc * Mathf.Exp(COEF_ATTITUDE * (helicopter.localPosition.y / SCALE_ALTITUDE_MAX)
        ));
        rbHelicopter.AddForce(effectiveUpAcc * helicopter.up, ForceMode.Acceleration);
        rbHelicopter.AddTorque(torqueAcc * helicopter.up, ForceMode.Acceleration);

        // Hack forward acceleration
        rbHelicopter.AddForce(forwardAcc * cockpit.forward, ForceMode.Acceleration);
    }

    private void SpeedUpTopRotor(float amount)
    {
        if (isEngineOn)
        {
            if (upAcc < 0) upAcc = 0;
            upAcc += -Physics.gravity.y * amount * SCALE_UPACC;
            torqueAcc += SCALE_TORQUE * amount;
        }
    }
    private void SlowDownTopRotor(float amount)
    {
        if (isEngineOn)
        {
            if (upAcc > 0) upAcc = 0;
            upAcc += Physics.gravity.y * amount * SCALE_UPACC;
            torqueAcc -= SCALE_TORQUE * amount;
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

}
