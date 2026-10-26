using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Slider _staminaSlider;

    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMove _playerMove;

    private void Start()
    {
        RefreshHealth();
        _playerHealth.OnDamaged += RefreshHealth;
    }
    
    private void LateUpdate()
    {
        // RefreshHealth(); // 폴링
        _staminaSlider.value = _playerMove.Stamina / _playerMove.MaxStamina;
    }

    private void RefreshHealth()
    {
        // 문제 : 체력값이 변하지 않았음에도 초당 60회 이상 캔버스를 불필요하게 다시 그린다. -> 성능 낭비가 일어난다.
        // 오고 하나 정도는 괜찮아 보이지만.. 이런 불필요한 연산이 다른 UI에서 누적될수록 느려진다.
        // 해결: 값이 바뀌었을 때만 캔버스 갱신을 한다.
        
        // 이때 쓰는게 옵저버 패턴
        // 주체의 상태가 바뀔 때마다 구독중인 관찰자에게 자동으로 알려주는 패턴
        // C#에서는 이것을 Action으로 구현할 수 있다.
        _hpSlider.value = _playerHealth.Health / _playerHealth.MaxHealth;
    }
}
