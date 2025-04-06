using System.Collections;
using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static moreMaths;

public class gyroPIDController : MonoBehaviour
{
    [Header("constraints")]
    public Transform target;
    public float MaxAngularVelocity = 20;
    Rigidbody rb;


    [Header("ROTATION")]
    public bool debugs;
    public bool x, y, z;

    private PID_rot deltaController;
    private PID_rot deltaVController;
    public float scalar;
    public Vector3 torque;

    float deltaTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        deltaController = gameObject.GetComponents<PID_rot>()[0];
        deltaController.myName = "velocity";
        deltaVController = gameObject.GetComponents<PID_rot>()[1];
        deltaVController.myName = "deltaV";
        rb.maxAngularVelocity = MaxAngularVelocity;

    }

    void FixedUpdate()
    {
        deltaTime = Time.fixedDeltaTime;

        //get the delta in quaternion format and sanitize so it's relative to identity rotation ---------------------------------------------------------------------------
        Quaternion deltaRot = target.rotation * Quaternion.Inverse(transform.rotation);
        Quaternion i = Quaternion.identity;
        Quaternion error = new Quaternion(i.x - deltaRot.x, i.y - deltaRot.y, i.z - deltaRot.z, i.w - deltaRot.w);

        //grab the eulers and neutralize based off requested rotations
        Vector3 eulers = error.eulerAngles;
        if(!x) eulers.x = 0;
        if(!y) eulers.y = 0;
        if(!z) eulers.z = 0;

        //recalculate the delta and flip when .w<0 to ensure we always rotate the shortest distance
        deltaRot = Quaternion.Euler(eulers);
        if (deltaRot.w < 0f)
            { deltaRot = new Quaternion(-deltaRot.x, -deltaRot.y, -deltaRot.z, -deltaRot.w); }
        Debug.DrawRay(transform.position, new Vector3(deltaRot.x, deltaRot.y, deltaRot.z)*3f, Color.yellow);
        Debug.DrawRay(transform.position, rb.angularVelocity, Color.magenta);

        Quaternion conj = Conj(transform.rotation);
        conj *= target.rotation;
        float angle = 360 - ((Mathf.Acos(conj.w)*Mathf.Rad2Deg));

        deltaRot.ToAngleAxis(out float a, out Vector3 eulerRot);
        Debug.Log("angle from conj: " + angle + "angle from angleAxis: " + a);
        //Debug.Log("angle between quaternions: " + angle);
        //above is our final value for scalar error on the quaternion --------------------------------------------------------


        //---------------------------PROBLEM: angularVelocity.magnitude returns positive always ------------------------------------
        //is there some way to flip the sign? do we need to flip the sign?
        //consider feeding it the raw angularVelocity not the magnitude then synthesizing it into a single float to approximate flipping signs?
        //Debug.Log(Gyro(angle, rb.angularVelocity, 'W'));
        scalar = Gyro(angle, rb.angularVelocity.magnitude, 'W');
        Vector3 rotationAxis = Gyro(angle, rb.angularVelocity, 'W');
        torque = new Vector3(deltaRot.x * scalar, deltaRot.y * scalar, deltaRot.z * scalar);
        //torque = new Vector3(deltaRot.x * s.x, deltaRot.y * s.y, deltaRot.z * s.z);
        //Debug.Log("torque: " +  torque);

        rb.AddTorque(torque);

    }


    //----------------------- BIG PROBLEM - I needs to be calculated separately for x/y/z I think ------------------//

    Vector3 Gyro(float angleError, Vector3 angularIn, char axis)
    {
        angleError = RemapFloat(angleError, 0f, 360f, -1f, 1f);
        float delta = deltaController.GetOutput(angleError, angularIn, deltaTime, axis);
        Vector3 deltaV = deltaVController.GetVectorOutput(angularIn, deltaTime);
        Vector3 s = new Vector3(deltaV.x + delta, deltaV.y + delta, deltaV.z + delta);
        return s;
    }

    float Gyro(float angleError, float angularIn, char axis)
    {
        angleError = RemapFloat(angleError, 0f, 360f, -1f, 1f);
        float delta = deltaController.GetOutput(angleError, rb.angularVelocity, deltaTime, axis); //delta controller K = difference in Quaternion, I = amount of time on one side of quat (needs to be a vector 3?), D = rate of rotation
        float deltaV = deltaVController.GetOutput(angularIn, rb.angularVelocity, deltaTime, axis); //deltaV controller K = difference in angularVelocity to 0, I = amount of time on one side of zero, D = rate of acceleration
        Vector3 angularDelta = deltaVController.GetVectorOutput(rb.angularVelocity, deltaTime);
        angularDelta *= Mathf.Rad2Deg;
        Debug.Log(angularDelta);
        float s = (delta + deltaV);
        return s;
    }

    Vector3 axisPID(Vector3 axis, Vector3 angularIn, bool debugs)
    {
        Vector3 posError = axis - transform.position;
        Vector3 linearVel = deltaController.GetVectorOutput(posError, deltaTime);
        Vector3 deltaVCorrection = deltaVController.GetVectorOutput(angularIn, deltaTime);
        Vector3 a = (linearVel + deltaVCorrection);
        if (debugs)
        {
            Debug.DrawRay(transform.position - Vector3.up, posError, Color.yellow);
            Debug.DrawLine(transform.position, axis, Color.white);
            Debug.DrawRay(transform.position + Vector3.up, a, Color.red);
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);
            Debug.DrawRay(transform.position - (Vector3.down / 2f), target.forward * 5f, Color.white);
        }

        return a;
    }


    //angle between quaternions
    //>> x = [0.968, 0.008, -0.008, 0.252]; x = x / norm(x); % ECI->BODY1
    //>> y = [0.382, 0.605, 0.413, 0.563]; y = y / norm(y); % ECI->BODY2
    //>> z = quatmultiply(quatconj(x), y) % BODY1->BODY2
    //z = 0.5132    0.6911    0.2549    0.4405
    //>> a = 2 * acosd(z(4)) % min angle rotation from BODY1 to BODY2
    //a =  127.7227

    //simple torque hack
    //      var quat0:Quaternion;
    //      var quat1:Quaternion;
    //      var quat10:Quaternion;
    //      quat0=transform.rotation;
    //      quat1=target.transform.rotation;
    //      quat10=quat1* Quaternion.Inverse(quat0);

    //BROKEN
    //Vector3 Gyro(Quaternion targetRot, Vector3 angularVelocityIn)
    //{
    //    Quaternion i = Quaternion.identity;
    //    Quaternion error = new Quaternion(i.x - targetRot.x, i.y - targetRot.y, i.z - targetRot.z, i.w - targetRot.w);
    //    Quaternion angularV = new Quaternion(angularVelocityIn.x, angularVelocityIn.y, angularVelocityIn.z, 0f);
    //    //Quaternion delta = angularV * targetRot;

    //    Vector3 linearRot = deltaController.GetQuaternionOutput(error, deltaTime);
    //    Vector3 angularDeltaV = deltaVController.GetQuaternionOutput(angularV, deltaTime);
    //    torque = (linearRot + angularDeltaV);

    //    return torque;
    //}



}
