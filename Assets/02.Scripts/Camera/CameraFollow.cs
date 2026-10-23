using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void Awake()
    {
        if (_target == null)
        {
            GameLogger.Info("[CameraFollow] No target found");
        }
    }
    
    private void LateUpdate()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        
        transform.position = _target.position;
    }
}
