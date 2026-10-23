using System;
using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
    }
    
    private void LateUpdate()
    {
        // 0 ~ 100% -> 0 ~ 1
        _slider.value = _zombie.Health / _zombie.MaxHealth;
        
        // 빌보드: 카메라가 나를 쳐다보게 한다.
        _slider.transform.forward = Camera.main.transform.forward;
    }
}
