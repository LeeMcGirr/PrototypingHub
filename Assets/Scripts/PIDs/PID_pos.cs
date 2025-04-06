using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

public class PID_pos : MonoBehaviour
{
    public string myName;
    [Range(0f, 50f)]
    public float Kp = 1;
    [Range(0f, 100f)]
    public float Ki = 0;
    [Range(0f, 3f)]
    public float Kd = 0.1f;

    public float P, I, D;
    public Vector3 vP, vI, vD;
    public Vector4 posError;


    public float GetOutput(float currentError, float deltaTime, char axis)
    {
        float error;
        if (axis == 'x' || axis == 'X') { error = posError.x; }
        else if (axis == 'y' || axis == 'Y') { error = posError.y; }
        else if (axis == 'z' || axis == 'Z') { error = posError.z; }
        else { error = posError.w; }

        P = currentError;
        I += P * deltaTime;
        D = (P - error) / deltaTime;

        if (axis == 'x' || axis == 'X') { posError.x = currentError; }
        else if (axis == 'y' || axis == 'Y') { posError.y = currentError; }
        else if (axis == 'z' || axis == 'Z') { posError.z = currentError; }
        else { posError.w = currentError; }

        return P * Kp + I * Ki + D * Kd;
    }

    public Vector3 GetVectorOutput(Vector3 currentError, float deltaTime)
    {
        vP = currentError;
        vI += vP * deltaTime; //starts small, increases the longer we stay on one side of an object
        vD = (vP - (Vector3)posError) / deltaTime; //approaches zero as the rate of change in acceleration decreases
        posError = (Vector4)currentError; //housekeeping for next frame

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
        posError = Vector4.zero;
    }
}
