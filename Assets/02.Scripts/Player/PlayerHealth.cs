using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    private float _health;
    
    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private void Awake()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        // Todo. 플레이어 피격
    }
}
