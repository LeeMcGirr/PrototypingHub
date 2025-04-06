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
    public float sleepTime = .5f;
    public float tolerance = .3f;
    public float MaxAngularVelocity = 3;
    Rigidbody rb;


    [Header("ROTATION")]
    public bool debugs;
    public bool x, y, z;

    private PID_rot deltaController;
    private PID_rot deltaVController;
    public float scalar;
    public Vector3 torque;

    float deltaTime;
    Quaternion prevTarget;

    public bool awake;
    public bool onTarget;


    void Start()
    {
        awake = true;
        onTarget = false;
        rb = GetComponent<Rigidbody>();
        deltaController = gameObject.GetComponents<PID_rot>()[0];
        deltaController.myName = "velocity";
        deltaVController = gameObject.GetComponents<PID_rot>()[1];
        deltaVController.myName = "deltaV";
        rb.maxAngularVelocity = MaxAngularVelocity;
        StartCoroutine(SleepTimer());

    }

    void FixedUpdate()
    {
        deltaTime = Time.fixedDeltaTime;
        Quaternion tD = prevTarget * Quaternion.Inverse(target.rotation);
        float targetDelta = Mathf.Abs(tD.x) + Mathf.Abs(tD.y) + Mathf.Abs(tD.z);
        if (targetDelta > .1f) { deltaController.ResetController(); deltaVController.ResetController(); }

        //get the delta in quaternion format and sanitize so it's relative to identity rotation ---------------------------------------------------------------------------
        Quaternion deltaRot = target.rotation * Quaternion.Inverse(transform.rotation); //delta rotation
        Quaternion i = Quaternion.identity;
        Quaternion error = new Quaternion(i.x - deltaRot.x, i.y - deltaRot.y, i.z - deltaRot.z, i.w - deltaRot.w);
        float errorMagn = Mathf.Abs(error.x) + Mathf.Abs(error.y) + Mathf.Abs(error.z);
        
        if (errorMagn < tolerance) { onTarget = true; }
        else 
        { 
           onTarget = false;
            if (!awake)
            {
                StartCoroutine(SleepTimer());
                awake = true;
            }
        }

        if (awake)
        {

            //grab the eulers and neutralize based off requested rotations
            Vector3 eulers = error.eulerAngles;
            if (!x) eulers.x = 0;
            if (!y) eulers.y = 0;
            if (!z) eulers.z = 0;

            //recalculate the delta and flip when .w<0 to ensure we always rotate the shortest distance
            deltaRot = Quaternion.Euler(eulers);
            if (deltaRot.w < 0f)
            { deltaRot = new Quaternion(-deltaRot.x, -deltaRot.y, -deltaRot.z, -deltaRot.w); }
            Vector3 axis = new Vector3(deltaRot.x, deltaRot.y, deltaRot.z);
            Debug.DrawRay(transform.position, axis * 3f, Color.yellow);
            Debug.DrawRay(transform.position, -axis * 3f, Color.blue);
            Debug.DrawRay(transform.position, rb.angularVelocity, Color.magenta);


            deltaRot.ToAngleAxis(out float a, out Vector3 eulerRot);

            scalar = Gyro(a, -rb.angularVelocity.magnitude, 'W');
            torque = new Vector3(deltaRot.x * scalar, deltaRot.y * scalar, deltaRot.z * scalar);
            Debug.DrawRay(transform.position, torque * 5f, Color.black);

            rb.AddTorque(torque);
        }
        prevTarget = target.rotation;

    }

    float Gyro(float angleError, float angularIn, char axis)
    {
        angleError = RemapFloat(angleError, 0f, 360f, -1f, 1f);
        float delta = deltaController.GetOutput(angleError, deltaTime, axis); //delta controller K = difference in Quaternion, I = amount of time on one side of quat (needs to be a vector 3?), D = rate of rotation
        float deltaV = deltaVController.GetOutput(angularIn, deltaTime, axis); //deltaV controller K = difference in angularVelocity to 0, I = amount of time on one side of zero, D = rate of acceleration
        float s = (delta + deltaV);
        return s;
    }

    //Vector3 axisPID(Vector3 axis, Vector3 angularIn, bool debugs)
    //{
    //    Vector3 posError = axis - transform.position;
    //    Vector3 linearVel = deltaController.GetVectorOutput(posError, deltaTime);
    //    Vector3 deltaVCorrection = deltaVController.GetVectorOutput(angularIn, deltaTime);
    //    Vector3 a = (linearVel + deltaVCorrection);
    //    if (debugs)
    //    {
    //        Debug.DrawRay(transform.position - Vector3.up, posError, Color.yellow);
    //        Debug.DrawLine(transform.position, axis, Color.white);
    //        Debug.DrawRay(transform.position + Vector3.up, a, Color.red);
    //    }
    //    return a;
    //}

    IEnumerator SleepTimer()
    {
        float sleepy = sleepTime;
        while(sleepy > 0f)
        {
            if(onTarget) { sleepy -= Time.deltaTime; }
            else { sleepy = sleepTime; }
            yield return null;
        }

        awake = false;
        deltaController.ResetController(); 
        deltaVController.ResetController();
        rb.angularVelocity = Vector3.zero;
        Debug.Log("SLEEP");
    }
}
