using UnityEditor.Experimental.GraphView;
using UnityEngine;

public enum MeleeHitMode
{
    Collider,
    Fan
}

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _meleeCollider;
    [SerializeField] private MeleeHitMode _meleeHitMode;

    [Header("부채꼴")]
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;
    
    
    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 검 콜라이더는 타격 구간(Collider 모드의 애니메이션 이벤트)에서만 켠다.
        // 히트 모드와 상관없이 평소에는 꺼 둔다. (Fan 모드에서 켜진 채로 남아 있으면 검이 계속 피해를 준다.)
        _meleeCollider.enabled = false;
    }

    public void FanAttack()
    {
        if (_meleeHitMode != MeleeHitMode.Fan) return;

        Vector3 origin = _player.transform.position;
        
        Collider[] colliders = Physics.OverlapSphere(origin, _range, _targetMask);
        foreach (Collider col in colliders)
        {
            Vector3 hitPoint = col.ClosestPoint(origin);
            
            Vector3 direction = hitPoint - origin;
            direction.y = 0f;
            float angle = Vector3.Angle(_player.transform.forward, direction);
            if (angle > _angle / 2f) continue;

            if (Physics.Linecast(origin, hitPoint, LayerMask.NameToLayer("Wall"))) continue;
            
            if (col.TryGetComponent(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.Value = 25f;
                damageInfo.HitPoint = col.ClosestPoint(origin);
                damageInfo.HitNormal = (origin - col.ClosestPoint(origin)).normalized;
                damageable.TakeDamage(damageInfo);
            }
        }
    }
    
    public void ActivateCollider()
    {
        if (_meleeHitMode != MeleeHitMode.Collider) return;
        
        _meleeCollider.enabled = true;   
    }

    public void DeactivateCollider()
    {
        if (_meleeHitMode != MeleeHitMode.Collider) return;
        
        _meleeCollider.enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_player.WeaponMode != WeaponMode.Melee) return;

        if (Input.GetMouseButtonDown(1))
        {
            _animator.SetTrigger("Slash");
        }
    }
}
