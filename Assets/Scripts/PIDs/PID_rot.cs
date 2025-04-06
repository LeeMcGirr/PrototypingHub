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
    [Range(0f, 10f)]
    public float Ki = 0;
    [Range(0f, 10f)]
    public float Kd = 0.1f;


    public float P, I, D;
    public Vector3 vP, vI, vD;

    public Vector4 rotError;

    public float GetOutput(float currentError, Vector3 basis, float deltaTime, char axis)
    {
        float error;
        if (axis == 'x' || axis == 'X') { error = rotError.x; }
        else if (axis == 'y' || axis == 'Y') { error = rotError.y; }
        else if (axis == 'z' || axis == 'Z') { error = rotError.z; }
        else { error = rotError.w; }

        P = currentError;
        if (!float.IsNaN(P * deltaTime)) 
        {
            if (basis.x + basis.y + basis.z < 0f) { I -= P * deltaTime; }
            else { I += P * deltaTime; }
        }
        if (!float.IsNaN(P - error)) { D = (P - error) / deltaTime; }
        else { D = 0f; }

        if (axis == 'x' || axis == 'X') { rotError.x = currentError; }
        else if (axis == 'y' || axis == 'Y') { rotError.y = currentError; }
        else if (axis == 'z' || axis == 'Z') { rotError.z = currentError; }
        else { rotError.w = currentError; }

        return P * Kp + I * Ki + D * Kd;
    }

    public Vector3 GetVectorOutput(Vector3 currentError, float deltaTime)
    {
        vP = currentError;
        vI += vP * deltaTime; //starts small, increases the longer we stay on one side of an object
        vD = (vP - (Vector3)rotError) / deltaTime; //approaches zero as the rate of change in acceleration decreases
        rotError = new Vector4 (currentError.x, currentError.y, currentError.z, rotError.w); //housekeeping for next frame

        return vP * Kp + vI * Ki + vD * Kd;
    }

    public void ResetController()
    {
        P = 0f;
        I = 0f;
        D = 0f;
        vP = Vector3.zero;
        vI = Vector3.zero;
        vD = Vector3.zero;
        rotError = Vector4.zero;
    }    

}
