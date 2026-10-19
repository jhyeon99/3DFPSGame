using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void Awake()
    {
        if (_target == null)
        {
            Debug.LogWarning("[CameraFollow] No target found");
        }
    }
    
    private void LateUpdate()
    {
        transform.position = _target.position;
    }
}
