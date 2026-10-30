using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private float _damage = 25f;

    private Player _player;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out IDamageable _damageable))
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = 25f;
            Vector3 hitPoint = other.ClosestPoint(_player.transform.position);
            damageInfo.HitPoint = hitPoint;
            damageInfo.HitNormal = (_player.transform.position - hitPoint).normalized;
            _damageable.TakeDamage(damageInfo);
        }
    }
}
