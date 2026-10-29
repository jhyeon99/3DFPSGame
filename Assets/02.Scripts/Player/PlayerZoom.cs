using System;
using UnityEngine;

public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private float _normalPov;
    [SerializeField] private float _snipePov;
    
    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        bool isZooming = _player.WeaponMode == WeaponMode.Sniper && Input.GetMouseButton(1);
        _sniperZoom.SetActive(isZooming);
        Camera.main.fieldOfView = isZooming ? _snipePov : _normalPov;
    }
}
