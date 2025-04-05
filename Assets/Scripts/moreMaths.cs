using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class moreMaths : MonoBehaviour
{
    //this is a container for math functions

    // -------------------------------------------- for shaping distribution of a normalized range ------------------------------------ //
    // ------------ speed square remaps a relatively large range to 0 to 1 then uses sqrt to shift the distribution -------------- //
    // ---------------- distribution should move from even (0 to 1) to weighted more towards 0 - like a sqrt graph curve -------- //

    #region SpeedSquare
    public static float SpeedSquare(float currentVal, 
                                    float absMaxVal, 
                                    int sqrtIterations,
                                    float floor, 
                                    float ceiling) //for quadratic weighting towards x = 1
    {
        //get current relative value to absolute max, then normalize it
        //avoid 0 and 1 to avoid problems with lossy data
        float multiplier = currentVal / absMaxVal;
        multiplier = Mathf.Clamp(multiplier, .01f, .99f);
        //get the square root iteratively (n) times
        for(int i = 0; i < sqrtIterations; i++) { multiplier = Mathf.Sqrt(multiplier); }
        //remap into a new target range if requested, gives more flexibility
        multiplier = RemapFloat(multiplier, .01f, .99f, floor, ceiling); 
        return multiplier;
        //distribution goes from (0, .25, .5, .75. 1) to (0, .5, .7, .866, 1) on 1 sqrt, then more at higher iterations
    }
    public static float SpeedSquare(float currentVal, float absMaxVal, int sqrtIterations)
    {
        float multiplier = currentVal / absMaxVal;
        multiplier = Mathf.Clamp(multiplier, .01f, .99f);
        for(int i = 0; i < sqrtIterations; i++) { multiplier = Mathf.Sqrt(multiplier); }
        return multiplier;
    }
    public static float SpeedSquare(float currentVal, float absMaxVal)
    {
        float multiplier = currentVal / absMaxVal;
        multiplier = Mathf.Clamp(multiplier, .01f, .99f);
        return Mathf.Sqrt(multiplier);
    }
    #endregion

    // ------------------ speed exponent does the same thing but with an exp function ---------------------------------------------//
    // ---------------- distribution should move from even (0 to 1) to weighted more towards 1 - like an exp graph curve -------//

    #region SpeedExponent 
    public static float SpeedExponent(float currentVal, 
                                    float absMaxVal, 
                                    int expIterations,
                                    float floor, 
                                    float ceiling) //for exponential weighting towards x = 0
    {
        //get  current relative val to abs max, normalize it
        //avoid 0 and 1 to avoid lossy info or going infinite
        float multiplier = currentVal / absMaxVal;
        multiplier = Mathf.Clamp(multiplier, .01f, .99f);
        //exponent iteratively
        for (int i = 0; i < expIterations; i++) { multiplier *= multiplier; }
        //remap into a new target range if requested, gives more flexibility
        multiplier = RemapFloat(multiplier, .01f, .99f, floor, ceiling);
        return multiplier;
        //distribution goes from (0, .25, .5, .75. 1) to (0, .0625, .25, .5625, 1) on 1 exp, then more at higher iterations
    }

    public static float SpeedExponent(float currentVal, float absMaxVal, int expIterations)
    {
        float multiplier = currentVal / absMaxVal;
        multiplier = Mathf.Clamp(multiplier, .01f, .99f);
        for (int i = 0; i < expIterations; i++) { multiplier *= multiplier; }
        return multiplier;
    }
    public static float SpeedExponent(float currentVal, float absMaxVal)
    {
        float multiplier = currentVal / absMaxVal;
        multiplier = Mathf.Clamp(multiplier, .01f, .99f);
        return multiplier *= multiplier;
    }
    #endregion

    //simple remapFloat operation
    public static float RemapFloat(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    //----------------------- LERP with an animation curve as a shaping function - floats and vectors -----------------//
    #region animCurveLerps
    public static float LerpCurved(float a, float b, AnimationCurve curve, float time)
    {
        //lerp using an animation curve as the t value
        float t = curve.Evaluate(time);
        return Mathf.Lerp(a, b, t);
    }

    public static Vector3 VLerpCurved(Vector3 a, Vector3 b, AnimationCurve curve, float time)
    {
        //lerp using an animation curve as the t value
        float t = curve.Evaluate(time);
        t = Mathf.Clamp(t, 0f, Mathf.Infinity);
        return Vector3.Lerp(a, b, t);
    }
    #endregion


    // -------------------------- ADDITIONAL QUATERNION FUNCTIONS -------------------------------------//
    #region moreQuaternions
    public static Quaternion QMultiply(Quaternion q, float s)
    {
        return new Quaternion(q.x * s, q.y * s, q.z * s, q.w * s);
    }

    public static Quaternion QAdd(Quaternion l, Quaternion r)
    {
        return new Quaternion(l.x + r.x, l.y + r.y, l.z + r.z, l.w + r.w);
    }

    public static Quaternion Conj(Quaternion q)
    {
        return new Quaternion(q.x, q.y, q.z, -q.w);
    }
    #endregion




}
