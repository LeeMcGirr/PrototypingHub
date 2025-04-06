using UnityEngine;

public class RotationPIDController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private bool useTargetTransform = false;
    [SerializeField] private Vector3 targetEulerAngles;

    [Header("PID Parameters")]
    [SerializeField] private float proportionalGain = 2.0f;
    [SerializeField] private float integralGain = 0.0f;
    [SerializeField] private float derivativeGain = 0.5f;
    [SerializeField] private float maxIntegralAccumulation = 10.0f;
    [SerializeField] private float maxRotationSpeed = 360.0f; // degrees per second

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Vector3 previousError = Vector3.zero;
    private Vector3 integralAccumulation = Vector3.zero;
    private Quaternion targetRotation;

    private void Start()
    {
        if (!useTargetTransform)
        {
            targetRotation = Quaternion.Euler(targetEulerAngles);
        }
    }

    private void Update()
    {
        if (useTargetTransform && targetTransform != null)
        {
            targetRotation = targetTransform.rotation;
        }
        else if (!useTargetTransform)
        {
            targetRotation = Quaternion.Euler(targetEulerAngles);
        }

        ApplyPIDRotation();
    }

    public void SetTargetRotation(Quaternion newTarget)
    {
        useTargetTransform = false;
        targetRotation = newTarget;
        targetEulerAngles = newTarget.eulerAngles;
    }

    public void SetTargetRotation(Vector3 eulerAngles)
    {
        useTargetTransform = false;
        targetEulerAngles = eulerAngles;
        targetRotation = Quaternion.Euler(eulerAngles);
    }

    public void SetTargetTransform(Transform target)
    {
        targetTransform = target;
        useTargetTransform = true;
    }

    private void ApplyPIDRotation()
    {
        // Calculate the error quaternion (difference between current and target rotation)
        Quaternion errorQuaternion = targetRotation * Quaternion.Inverse(transform.rotation);
        
        // Convert to axis-angle representation
        errorQuaternion.ToAngleAxis(out float angle, out Vector3 axis);
        
        // Normalize angle to [-180, 180] range
        if (angle > 180f)
        {
            angle -= 360f;
        }
        
        // Calculate error vector (axis * angle in radians)
        Vector3 error = axis * angle * Mathf.Deg2Rad;
        
        // Calculate integral term with anti-windup
        integralAccumulation += error * Time.deltaTime;
        integralAccumulation = Vector3ClampMagnitude(integralAccumulation, maxIntegralAccumulation);
        
        // Calculate derivative term
        Vector3 derivative = (error - previousError) / Time.deltaTime;
        previousError = error;
        
        // Calculate PID output
        Vector3 output = proportionalGain * error + 
                         integralGain * integralAccumulation + 
                         derivativeGain * derivative;
        
        // Clamp rotation speed
        output = Vector3ClampMagnitude(output, maxRotationSpeed * Mathf.Deg2Rad);
        
        // Convert back to degrees for rotation
        Vector3 rotationAmount = output * Mathf.Rad2Deg * Time.deltaTime;
        
        // Apply rotation
        transform.Rotate(rotationAmount, Space.World);
        
        // Debug info
        if (showDebugInfo)
        {
            Debug.Log($"Error: {error}, Output: {output}, Rotation: {rotationAmount}");
        }
    }

    private Vector3 Vector3ClampMagnitude(Vector3 vector, float maxMagnitude)
    {
        if (vector.sqrMagnitude > maxMagnitude * maxMagnitude)
        {
            return vector.normalized * maxMagnitude;
        }
        return vector;
    }

    // Helper method to reset the controller (useful when changing targets dramatically)
    public void ResetController()
    {
        previousError = Vector3.zero;
        integralAccumulation = Vector3.zero;
    }
}
