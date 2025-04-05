using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using static moreMaths;

public class PID_rot : MonoBehaviour
{
    public string myName;
    [Range(0f, 50f)]
    public float Kp = 1;
    [Range(0f, 100f)]
    public float Ki = 0;
    [Range(0f, 3f)]
    public float Kd = 0.1f;


    public float P, I, D;
    public Quaternion qP, qI, qD;

    public Vector4 rotError;
    Quaternion quatError;

    public void Start()
    {
        quatError = Quaternion.identity;
    }

    public float GetOutput(float currentError, float deltaTime, char axis)
    {
        float error;
        if (axis == 'x' || axis == 'X') { error = rotError.x; }
        else if (axis == 'y' || axis == 'Y') { error = rotError.y; }
        else if (axis == 'z' || axis == 'Z') { error = rotError.z; }
        else { error = rotError.w; }

        P = currentError;
        if (!float.IsNaN(P * deltaTime)) { I += P * deltaTime; }
        D = (P - error) / deltaTime;

        if (axis == 'x' || axis == 'X') { rotError.x = currentError; }
        else if (axis == 'y' || axis == 'Y') { rotError.y = currentError; }
        else if (axis == 'z' || axis == 'Z') { rotError.z = currentError; }
        else { rotError.w = currentError; }

        return P * Kp + I * Ki + D * Kd;
    }

    // ----------------------- BROKEN ----------------------------------- //
    public Vector3 GetQuaternionOutput(Quaternion currentError, float deltaTime)
    {
        qP = currentError;
        qI = QAdd(qI, QMultiply(qP, deltaTime));
        float scalar = 1 / deltaTime;
        Debug.Log(scalar);

        if(quatError != Quaternion.identity)
        { qD = QMultiply((qP * Quaternion.Inverse(quatError)), scalar); }

        else { qD = QMultiply(currentError, scalar); }


        quatError = currentError;
        Quaternion torque = (QMultiply(qP, P)) * (QMultiply(qI, I)) * (QMultiply(qD, D));
        return new Vector3(torque.x, torque.y, torque.z);
    }

}
