using UnityEngine;

// 애니메이션 이벤트는 Animator가 붙은 오브젝트의 컴포넌트만 호출할 수 있다.
// Animator가 Player의 자식이므로, 이벤트를 받아 부모의 PlayerMeleeAttack으로 전달한다.
public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerMeleeAttack _meleeAttack;

    private void Awake()
    {
        _meleeAttack = GetComponentInParent<PlayerMeleeAttack>();
    }

    // 베기 클립의 타격 시작 프레임에서 호출한다.
    public void ActivateCollider()
    {
        _meleeAttack.ActivateCollider();
    }

    // 베기 클립의 타격 종료 프레임에서 호출한다.
    public void DeactivateCollider()
    {
        _meleeAttack.DeactivateCollider();
    }

    public void FanAttack()
    {
        _meleeAttack.FanAttack();
    }
}
