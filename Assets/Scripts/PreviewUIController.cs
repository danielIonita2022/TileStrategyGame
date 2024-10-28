using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PreviewUIController : MonoBehaviour
{
	private Image previewImage; // Changed from VisualElement to Image
	private UIDocument uiDocument;

	void OnEnable()
	{
		uiDocument = GetComponent<UIDocument>();
		if (uiDocument == null)
		{
			Debug.LogError("UIDocument component not found on " + gameObject.name);
			return;
		}

		var root = uiDocument.rootVisualElement;
		previewImage = root.Q<Image>("PreviewImage"); // Ensure this matches the UXML name
		if (previewImage == null)
		{
			Debug.LogError("PreviewImage not found in UXML.");
		}
	}

	// Method to update the preview image sprite
	public void UpdatePreview(Sprite newSprite)
	{
		if (previewImage != null)
		{
			if (newSprite != null)
			{
				if (newSprite.texture != null)
				{
					previewImage.image = newSprite.texture; // Assign the Texture from the Sprite
					previewImage.style.display = DisplayStyle.Flex; // Ensure the image is visible
																	//currentRotation = 0f; // Reset rotation when a new sprite is assigned
																	//previewImage.style.rotate = new StyleRotate(new Rotate(new Angle(currentRotation, AngleUnit.Degree)));
					Debug.Log("Preview image updated to: " + newSprite.name);
				}
				else
				{
					Debug.LogError("Sprite " + newSprite.name + " has no texture.");
					previewImage.style.display = DisplayStyle.None;
				}
			}
			else
			{
				Debug.LogWarning("Received a null sprite for preview.");
				previewImage.style.display = DisplayStyle.None; // Hide the image if sprite is null
			}
		}
		else
		{
			Debug.LogError("PreviewImage is not assigned.");
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

	//// Method to get the current rotation angle
	//public float GetPreviewRotationAngle()
	//{
	//	if (previewImage != null)
	//	{
	//		Rotate rotation = previewImage.style.rotate.value;
	//		return rotation.value.value; // Returns the angle in degrees
	//	}
	//	return 0f;
	//}
}