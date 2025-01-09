using UnityEngine;

public class WandFocusPivot : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 10f; // Speed of rotation on the Z-axis

    [Header("Scaling Settings")]
    [SerializeField] private float _scaleSpeed = 2f; // Speed of scaling back and forth
    [SerializeField] private float _scaleFactor = 0.1f; // How much the scale changes

    private Vector3 _originalScale; // To store the initial scale of the object
    private Vector3 _targetScale;   // To store the target scale for slerping
    private float _scaleTime;       // A timer to manage scaling interpolation

    private void Start()
    {
        // Save the original scale as the base scale
        _originalScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        // Rotate the object around the Z-axis
        transform.Rotate(0, 0, _rotationSpeed * Time.deltaTime);

        // Scale the object back and forth
        _scaleTime += Time.deltaTime * _scaleSpeed;
        _targetScale = _originalScale + Vector3.one * Mathf.Sin(_scaleTime) * _scaleFactor;

        // Smoothly interpolate the scale
        transform.localScale = Vector3.Slerp(transform.localScale, _targetScale, Time.deltaTime * _scaleSpeed);
    }
}