using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PreviewUIController : MonoBehaviour
{
	[SerializeField] private UIDocument uiDocument;
    private Image previewImage;
    private Button rotateLeftButton;
    private Button rotateRightButton;

    private int previewRotationState = 0; // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°
    private float[] rotationAngles = { 0f, 90f, 180f, 270f };

    void OnEnable()
	{
        uiDocument = GetComponent<UIDocument>();
		if (uiDocument == null)
		{
			Debug.LogError("UIDocument component not found on " + gameObject.name);
			return;
		}

		var root = uiDocument.rootVisualElement;
		previewImage = root.Q<Image>("PreviewImage");
        rotateLeftButton = root.Q<Button>("RotateLeftButton");
        rotateRightButton = root.Q<Button>("RotateRightButton");
        if (previewImage == null)
		{
			Debug.LogError("PreviewImage not found in UXML.");
		}
        if (rotateLeftButton != null)
            rotateLeftButton.clicked += RotateLeft;
        else
            Debug.LogError("RotateLeftButton not found in UXML.");

        if (rotateRightButton != null)
            rotateRightButton.clicked += RotateRight;
        else
            Debug.LogError("RotateRightButton not found in UXML.");
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (rotateLeftButton != null)
            rotateLeftButton.clicked -= RotateLeft;

        if (rotateRightButton != null)
            rotateRightButton.clicked -= RotateRight;
    }

    /// <summary>
    /// Updates the preview image with the selected tile's sprite.
    /// </summary>
    public void UpdatePreview(Sprite tileSprite)
    {
        if (previewImage != null)
        {
            previewImage.sprite = tileSprite;
            previewImage.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("Updated preview image.");
        }
        else
        {
            Debug.LogError("PreviewImage is not assigned in PreviewUIController.");
        }
    }

    // Optionally, hide the preview image
    public void HidePreview()
	{
		if (previewImage != null)
		{
			previewImage.style.display = DisplayStyle.None;
		}
	}

	// Optionally, show the preview image
	public void ShowPreview()
	{
		if (previewImage != null)
		{
			previewImage.style.display = DisplayStyle.Flex;
		}
	}

    /// <summary>
    /// Rotates the preview tile to the left (counter-clockwise).
    /// </summary>
    private void RotateLeft()
    {
        previewRotationState = (previewRotationState + 3) % 4; // Equivalent to -1 mod 4
        ApplyRotation();
    }

    /// <summary>
    /// Rotates the preview tile to the right (clockwise).
    /// </summary>
    private void RotateRight()
    {
        previewRotationState = (previewRotationState + 1) % 4;
        ApplyRotation();
    }

    /// <summary>
    /// Applies the current rotation state to the preview image.
    /// </summary>
    private void ApplyRotation()
    {
        if (previewImage != null)
        {
            previewImage.transform.rotation = Quaternion.Euler(0, 0, rotationAngles[previewRotationState]);
            Debug.Log($"Preview rotated to {rotationAngles[previewRotationState]} degrees.");
        }
    }

    /// <summary>
    /// Retrieves the current rotation state of the preview tile.
    /// </summary>
    public int GetPreviewRotationState()
    {
        return previewRotationState;
    }

    public void ResetPreviewRotationState()
    {
        previewRotationState = 0;
    }
}