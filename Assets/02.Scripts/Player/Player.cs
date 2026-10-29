using UnityEngine;
using System;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    private float _health;
    
    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private WeaponMode _weaponMode = WeaponMode.Normal;
    public WeaponMode WeaponMode => _weaponMode;

    // Action 자료형: 함수(들)를 저장할 수 있다.
    public event Action OnDamaged;
    public event Action<WeaponMode> OnWeaponChanged;

    private void Awake()
    {
        _health = _maxHealth;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponMode = WeaponMode.Normal;
            OnWeaponChanged?.Invoke(WeaponMode.Normal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _weaponMode = WeaponMode.Sniper;
            OnWeaponChanged?.Invoke(WeaponMode.Sniper);
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;
        
        _health -= damageInfo.Value;

        OnDamaged?.Invoke();
        
        Debug.Log($"플레이어가 맞았다. 데미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다!");
            GameManager.Instance.GameOver();
        }
    }
}
