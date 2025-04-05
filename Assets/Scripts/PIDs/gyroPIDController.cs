using System.Collections;
using System.Collections.Generic;
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

        //get the delta in quaternion format
        Quaternion deltaRot = target.rotation * Quaternion.Inverse(transform.rotation);

        //grab the eulers and neutralize based off requested rotations
        Vector3 eulers = deltaRot.eulerAngles;
        if(!x) eulers.x = 0;
        if(!y) eulers.y = 0;
        if(!z) eulers.z = 0;

        //recalculate the delta and flip when .w<0 to ensure we always rotate the shortest distance
        deltaRot = Quaternion.Euler(eulers);
        if (deltaRot.w < 0f)
            { deltaRot = new Quaternion(-deltaRot.x, -deltaRot.y, -deltaRot.z, -deltaRot.w); }
        //Debug.Log("quaternion result: " + deltaRot);

        Quaternion conj = Conj(transform.rotation);
        conj *= target.rotation;
        float angle = 360 - ((Mathf.Acos(conj.w)*Mathf.Rad2Deg));
        //Debug.Log("angle between quaternions: " + angle);
        
        scalar = Gyro(angle, rb.angularVelocity.magnitude, 'W');
        torque = new Vector3(deltaRot.x * scalar, deltaRot.y * scalar, deltaRot.z * scalar);
        //Debug.Log("torque: " +  torque);    
        rb.AddTorque(torque);


    }


    float Gyro(float angleError, float angularIn, char axis)
    {
        angleError = RemapFloat(angleError, 0f, 360f, -1f, 1f);
        float torqueCorrectionForAngle = deltaController.GetOutput(angleError, deltaTime, axis);
        float torqueCorrectionForAngularVelocity = deltaVController.GetOutput(angularIn, deltaTime, axis);
        float s = (torqueCorrectionForAngle + torqueCorrectionForAngularVelocity);
        return s;
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
