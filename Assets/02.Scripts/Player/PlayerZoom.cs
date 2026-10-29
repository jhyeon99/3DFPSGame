using UnityEngine;

public class PlayerZoom : MonoBehaviour
{
    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        bool isZooming = _player.WeaponMode == WeaponMode.Sniper && Input.GetMouseButton(1);
        if (isZooming)
        {
            Camera.main.fieldOfView = 30f;
        }
        else
        {
            Camera.main.fieldOfView = 60f;
        }
    }
}
