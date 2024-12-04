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

    private void Update()
    {
        PanCamera();
        HandleZoom();
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

        //cam.transform.position = ClampCamera(cam.transform.position);
    }

    public void ZoomOut()
    {
        float newSize = cam.orthographicSize + zoomStep;
        cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);

        //cam.transform.position = ClampCamera(cam.transform.position);
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
