using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class posPIDController : MonoBehaviour
{

    [Header("constraints")]
    public Transform target;
    public float MaxLinearVelocity = 100;
    Rigidbody rb;

    [Header("POSITION")]
    public bool posDebugs;
    public bool x, y, z;

    PID_pos deltaController;
    PID_pos deltaVController;
    public Vector3 force;
    float deltaTime;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        deltaController = gameObject.GetComponents<PID_pos>()[0];
        deltaController.myName = "velocity";
        deltaVController = gameObject.GetComponents<PID_pos>()[1];
        deltaVController.myName = "deltaV";
        rb.maxLinearVelocity = MaxLinearVelocity;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        deltaTime = Time.fixedDeltaTime;
        if (x || y || z)
        {
            Vector3 f = PosPID(target.position, -rb.linearVelocity, posDebugs);
            if (!x) { f.x = 0; }
            if (!y) { f.y = 0; }
            if (!z) { f.z = 0; }
            rb.AddForce(f);
        }
    }

    Vector3 PosPID(Vector3 targetPos, Vector3 velocityIn, bool debugs)
    {
        Vector3 posError = targetPos - transform.position;
        Vector3 linearVel = deltaController.GetVectorOutput(posError, deltaTime);
        Vector3 deltaVCorrection = deltaVController.GetVectorOutput(velocityIn, deltaTime);
        force = (linearVel + deltaVCorrection);

        if (debugs)
        {
            Debug.DrawRay(transform.position - Vector3.up, posError, Color.yellow);
            Debug.DrawLine(transform.position, targetPos, Color.white);
            Debug.DrawRay(transform.position + Vector3.up, force, Color.red);
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);
            Debug.DrawRay(transform.position - (Vector3.down / 2f), target.forward * 5f, Color.white);
        }

        return force;
    }

}
