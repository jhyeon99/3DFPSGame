using UnityEngine;

// F키를 누르면 검을 휘두른다. 베기 애니메이션(Slash1~3)을 콤보로 이어서 재생한다.
// 검은 손 본의 자식이라 애니메이션을 그대로 따라간다.
public class PlayerSwordSwing : MonoBehaviour
{
    private const int SlashCount = 3;
    private const string SlashTag = "Slash";
    private static readonly int SlashTrigger = Animator.StringToHash("Slash");
    private static readonly int SlashIndex = Animator.StringToHash("SlashIndex");

    [Header("입력")]
    [SerializeField] private KeyCode _swingKey = KeyCode.F;

    [Header("콤보")]
    [SerializeField, Range(0f, 1f)] private float _comboInputStartRatio = 0.5f; // 이 진행도부터 다음 베기 입력을 받는다.

    private Animator _animator;
    private int _comboIndex;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (Cursor.visible) return;
        if (!Input.GetKeyDown(_swingKey)) return;

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        bool isSlashing = state.IsTag(SlashTag);

        // 베는 중에는 어느 정도 진행된 뒤에만 다음 콤보를 받는다.
        if (isSlashing && state.normalizedTime < _comboInputStartRatio) return;

        _comboIndex = isSlashing ? _comboIndex % SlashCount + 1 : 1;
        _animator.SetInteger(SlashIndex, _comboIndex);
        _animator.SetTrigger(SlashTrigger);
    }
}
