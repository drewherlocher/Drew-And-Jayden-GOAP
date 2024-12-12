using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f;     // Speed of camera movement
    public float zoomSpeed = 5f;     // Speed of zooming
    public float minZoom = 5f;       // Minimum zoom level
    public float maxZoom = 20f;      // Maximum zoom level

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        MoveCamera();
        ZoomCamera();
    }

    private void MoveCamera()
    {
        if (Input.GetMouseButton(1)) // Right mouse button for moving
        {
            float moveX = Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float moveY = Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;

            transform.position -= new Vector3(moveX, moveY, 0);
        }
    }

    private void ZoomCamera()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}