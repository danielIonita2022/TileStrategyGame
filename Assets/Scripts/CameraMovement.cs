using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private Camera cam;

    [SerializeField]
    private float zoomStep, minCamSize, maxCamSize;

    [SerializeField]
    private SpriteRenderer mapRenderer;

    [SerializeField]
    private float mapWidth = 1000f;

    [SerializeField]
    private float mapHeight = 1000f;

    private float mapMaxX, mapMinX, mapMinY, mapMaxY;

    private Vector3 dragOrigin;

    [SerializeField]
    private float dragThreshold = 10f;

    private Vector2 touchStartPosition;

    private bool isDragging = false;

    public bool IsDragging => isDragging;

    private void Update()
    {
        if (Application.isMobilePlatform)
        {
            HandleTouchInput();
        }
        else
        {
            PanCamera();
            HandleZoom();
        }
    }

    private void Awake()
    {
        mapMinX = -mapWidth / 2f;
        mapMaxX = mapWidth / 2f;
        mapMinY = -mapHeight / 2f;
        mapMaxY = mapHeight / 2f;
    }

    private void PanCamera()
    {
        //save position of mouse in world space when drag start
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        //calcualte distance between drag origin and new position if its still held
        if (Input.GetMouseButton(1))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            //print("origin " + dragOrigin + " newPosition " + cam.ScreenToWorldPoint(Input.mousePosition) + " =difference" + difference);


            //move the camera by that distance
            cam.transform.position += difference;

            //Not working clamping
            /*Vector3 targetPosition = cam.transform.position + difference;
            cam.transform.position = ClampCamera(targetPosition);*/
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1) // Single touch for panning
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    isDragging = false; // Reset dragging flag
                    break;

                case TouchPhase.Moved:
                    // Check if the touch has moved beyond the threshold
                    if (!isDragging && Vector2.Distance(touch.position, touchStartPosition) > dragThreshold)
                    {
                        isDragging = true; // Start dragging
                        dragOrigin = cam.ScreenToWorldPoint(touch.position);
                    }

                    if (isDragging)
                    {
                        Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(touch.position);
                        cam.transform.position += difference;
                    }
                    break;

                case TouchPhase.Ended:
                    isDragging = false;
                    break;
            }
        }
        else if (Input.touchCount == 2) // Two-finger touch for pinch-to-zoom
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Calculate the pinch distance
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            // Reverse the zoom direction
            ZoomPhone(-difference * 0.01f); // Scale the zoom speed
        }
    }

    private void HandleTap(Vector2 screenPosition)
    {
        // Your logic for handling a tap on the screen
        Debug.Log("Tap detected at: " + screenPosition);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            ZoomIn();
        }
        else if (scroll < 0f)
        {
            ZoomOut();
        }

        // Zoom in when "+" or "=" key is pressed
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
        {
            ZoomIn();
        }

        // Zoom out when "-" key is pressed
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            ZoomOut();
        }
    }


    public void ZoomIn()
    {
        float newSize = cam.orthographicSize - zoomStep;
        cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
    }

    public void ZoomOut()
    {
        float newSize = cam.orthographicSize + zoomStep;
        cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
    }

    private void ZoomPhone(float increment)
    {
        float newSize = cam.orthographicSize + increment;
        cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
    }

    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        // Adjust boundaries to clamp correctly
        float minX = mapMinX + camWidth;
        float maxX = mapMaxX - camWidth;
        float minY = mapMinY + camHeight;
        float maxY = mapMaxY - camHeight;

        // Prevent snapping by ensuring calculated bounds make sense
        if (maxX < minX) minX = maxX = (mapMinX + mapMaxX) / 2f;
        if (maxY < minY) minY = maxY = (mapMinY + mapMaxY) / 2f;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPosition.z);
    }

    // Start is called before the first frame update
    void Start()
    {

    }
}
