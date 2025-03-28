using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);

    private Transform target;

    void Start()
    {
        if (GridManager.Instance != null)
        {
            GameObject centerPoint = new GameObject("GridCenterTarget");
            centerPoint.transform.position = GridManager.Instance.GetGridCenter();
            target = centerPoint.transform;

            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }
        else
        {
            Debug.LogError("CameraController: GridManager instance not found!");
        }
    }
}
