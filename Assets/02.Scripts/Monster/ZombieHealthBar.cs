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

    private void OnEnable()
    {
        _zombie.OnHealthChanged += RefreshHealth;
    }

    private void OnDisable()
    {
        _zombie.OnHealthChanged -= RefreshHealth;
    }

    private void Start()
    {
        RefreshHealth();
    }

    private void LateUpdate()
    {
        // 빌보드: 카메라가 나를 쳐다보게 한다.
        _slider.transform.forward = Camera.main.transform.forward;
    }

    // 체력이 바뀔 때만 슬라이더를 갱신한다. (0 ~ 100% -> 0 ~ 1)
    private void RefreshHealth()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
