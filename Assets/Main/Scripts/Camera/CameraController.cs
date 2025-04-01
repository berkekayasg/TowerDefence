using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target & Positioning")]
    [SerializeField] private float initialDistance = 15f; // Initial distance from target
    [SerializeField] private float smoothSpeed = 5f; // Speed for smooth movement/rotation

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 20f;
    [SerializeField] private float minZoomDistance = 5f;
    [SerializeField] private float maxZoomDistance = 50f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Panning")]
    [SerializeField] private float panSpeed = 10f;

    private Transform target; // The point the camera orbits and looks at
    private Vector3 offsetDirection; // Normalized direction from target to camera
    private float currentDistance; // Current distance from target

    // Drag state variables
    private bool isRotating = false;
    private bool isPanning = false;
    private Vector3 lastMousePosition;

    void Start()
    {
        if (GridManager.Instance != null)
        {
            // Create a target object at the grid center
            GameObject centerPoint = new GameObject("GridCenterTarget");
            centerPoint.transform.position = GridManager.Instance.GetGridCenter();
            target = centerPoint.transform;

            // Initial setup
            currentDistance = initialDistance;
            // Use a default offset direction if needed, or calculate from initial position
            offsetDirection = new Vector3(0, 0.7f, -0.7f).normalized; // Example initial angle

            // Set initial position and rotation without smoothing
            Vector3 desiredPosition = target.position + offsetDirection * currentDistance;
            transform.position = desiredPosition;
            transform.LookAt(target.position);
        }
        else
        {
            Debug.LogError("CameraController: GridManager instance not found! Camera control disabled.");
            enabled = false; // Disable script if no grid manager
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        UpdateCameraTransform();
    }

    void HandleInput()
    {
        // --- Zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
        }

        // --- Rotation ---
        if (Input.GetMouseButtonDown(1)) // Right Mouse Button Down
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1)) // Right Mouse Button Up
        {
            isRotating = false;
        }

        // --- Panning ---
        if (Input.GetMouseButtonDown(2)) // Middle Mouse Button Down
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2)) // Middle Mouse Button Up
        {
            isPanning = false;
        }

        // --- Apply Drag ---
        if (isRotating)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            // Rotate the offset direction around the target's up axis
            Quaternion horizontalRotation = Quaternion.AngleAxis(delta.x * rotationSpeed * Time.deltaTime, Vector3.up);
            // Rotate the offset direction around the camera's right axis for vertical rotation (clamped)
            Quaternion verticalRotation = Quaternion.AngleAxis(-delta.y * rotationSpeed * Time.deltaTime, transform.right);

            // Apply rotations to the offset direction
            offsetDirection = horizontalRotation * verticalRotation * offsetDirection;

            // Prevent camera flipping over the top/bottom (optional clamping)
            // You might need to clamp the angle relative to the horizontal plane if issues arise

            lastMousePosition = Input.mousePosition;
        }
        else if (isPanning)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            // Convert screen delta to world movement relative to camera orientation
            Vector3 forward = transform.forward;
            forward.y = 0; // Project onto horizontal plane
            forward.Normalize();
            Vector3 right = transform.right;
            right.y = 0; // Project onto horizontal plane
            right.Normalize();

            Vector3 move = (right * -delta.x + forward * -delta.y) * panSpeed * Time.deltaTime * (currentDistance / initialDistance); // Scale pan speed by zoom
            target.position += move;

            lastMousePosition = Input.mousePosition;
        }
    }

    void UpdateCameraTransform()
    {
        // Calculate desired position based on target, offset direction, and distance
        Vector3 desiredPosition = target.position + offsetDirection * currentDistance;

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Smoothly rotate the camera to look at the target
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
    }
}
