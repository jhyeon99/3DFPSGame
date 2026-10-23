using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Slider _staminaSlider;

    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMove _playerMove;

    private void LateUpdate()
    {
        _hpSlider.value = _playerHealth.Health / _playerHealth.MaxHealth;
        _staminaSlider.value = _playerMove.Stamina / _playerMove.MaxStamina;
    }
}
