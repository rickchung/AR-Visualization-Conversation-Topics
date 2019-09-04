using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterController : AvatarController
{
    private Transform topRotor, tailRotor, helicopter;
    private Rigidbody rbTopRotor, rbTailRotor, rbHelicopter;

    private void Start()
    {
        helicopter = transform;
        rbHelicopter = helicopter.GetComponent<Rigidbody>();

        topRotor = helicopter.Find("Top_rotor");
        tailRotor = helicopter.Find("Tail_Rotor");
        rbTopRotor = topRotor.GetComponent<Rigidbody>();
        rbTailRotor = tailRotor.GetComponent<Rigidbody>();

        StartEngine();
        ClimbUp();
    }

    // ==================== Pilot Control System ====================

    // This system is defined by functions in the ECU.

    // ==================== Engine Control Unit ====================

    private float
        climbUpTopAcc = 1.0f, climbUpTailAcc = 1.0f,
        fallDownTopAcc = 1.0f, fallDownTailAcc = 1.0f,
        hovorTurnRightAcc = 1.0f, hovorTurnLeftAcc = 1.0f,
        moveForwardAngle = 1.0f;
    private bool isEngineOn = false;

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
        rbHelicopter.velocity = 5000 * -Vector3.up;
    }

    public void ClimbUp()
    {
        SpeedUpTopRotor(climbUpTopAcc);
        SpeedUpTailRotor(climbUpTailAcc);
    }

    public void FallDown()
    {
        SpeedDownTopRotor(fallDownTopAcc);
        SpeedUpTailRotor(fallDownTailAcc);
    }

    public void HovorTurnRight()
    {
        SpeedUpTailRotor(hovorTurnRightAcc);
    }

    public void HovorTurnLeft()
    {
        SpeedUpTailRotor(hovorTurnLeftAcc);
    }

    public void MoveForward()
    {
        TiltTopRotor(helicopter.forward * moveForwardAngle);
    }

    // ==================== Internal Behavior ====================


    private float upAcc, torqueAcc;
    private const float SCALE_TORQUE = 10f;
    private const float SCALE_UPACC = 4f;
    private const float SCALE_ALTITUDE_MAX = 0.6f;
    private void FixedUpdate()
    {
        var effectiveUpAcc = upAcc * Mathf.Exp(-2 * (helicopter.localPosition.y / SCALE_ALTITUDE_MAX));
        rbHelicopter.AddForce(effectiveUpAcc * helicopter.up, ForceMode.Force);
        rbHelicopter.AddTorque(torqueAcc * helicopter.up, ForceMode.Acceleration);
    }

    private void SpeedUpTopRotor(float amount)
    {
        if (isEngineOn)
        {
            upAcc += -Physics.gravity.y * amount * SCALE_UPACC;
            torqueAcc += SCALE_TORQUE * amount;
        }
    }
    private void SpeedDownTopRotor(float amount)
    {
        if (isEngineOn)
        {
            upAcc += Physics.gravity.y * amount * SCALE_UPACC;
            torqueAcc -= SCALE_TORQUE * amount;
        }
    }

    private void SpeedUpTailRotor(float amount)
    {
        if (isEngineOn)
        {
            torqueAcc -= SCALE_TORQUE * amount;
        }
    }

    private void SpeedDownTailRotor(float amount)
    {
        if (isEngineOn)
        {
            torqueAcc += SCALE_TORQUE * amount;
        }
    }

    private void TiltTopRotor(Vector3 angles)
    {
        if (isEngineOn)
        {
            helicopter.Rotate(angles, Space.Self);
        }
    }

}
