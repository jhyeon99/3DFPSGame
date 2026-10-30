using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;
    [SerializeField] private Collider _meleeCollider;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = GetComponentInChildren<Animator>();
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");

        // 타격 구간(애니메이션 이벤트)에서만 켠다.
        DeactivateCollider();
    }
    
    public void ActivateCollider() => _meleeCollider.enabled = true;
    public void DeactivateCollider() => _meleeCollider.enabled = false;

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
