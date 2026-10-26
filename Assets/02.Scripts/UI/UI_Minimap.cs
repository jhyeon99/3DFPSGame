using UnityEngine;

public class UI_Minimap : MonoBehaviour
{
    private MinimapCamera _minimapCamera;

    private void Awake()
    {
        _minimapCamera = GetComponent<MinimapCamera>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            _minimapCamera.ZoomIn();
        }
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            _minimapCamera.ZoomOut();
        }
    }
}
