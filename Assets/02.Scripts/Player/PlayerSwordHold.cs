using UnityEngine;

// 근접 무기 모드일 때 1인칭 시야에 검을 쥔 자세를 보여준다.
// 오른손을 카메라 기준의 고정 위치로 IK 이동시키고, 베는 동안에는 애니메이션에 맡긴다.
// Animator가 붙은 오브젝트에 두며, Base Layer의 IK Pass가 켜져 있어야 한다.
[RequireComponent(typeof(Animator))]
public class PlayerSwordHold : MonoBehaviour
{
    private const string MeleeLayerName = "Melee Layer";
    private const string SlashTag = "Slash";

    [Header("쥐는 자세 (카메라 기준)")]
    [SerializeField] private Transform _cameraPosition;                         // 카메라가 따라가는 위치 (Player 자식)
    [SerializeField] private Vector3 _handOffset = new Vector3(0.22f, -0.2f, 0.38f);   // 팔이 닿는 거리 안쪽이어야 한다.
    [SerializeField] private Vector3 _swordEuler = new Vector3(326.7f, 339f, 79.4f);   // 카메라 기준 검 회전 (날이 위·앞·왼쪽, 납작한 면이 카메라 쪽)
    [SerializeField] private Vector3 _elbowHint = new Vector3(0.5f, 0.8f, 0f);  // 플레이어 기준 팔꿈치 방향

    [Header("전환")]
    [SerializeField] private float _blendSpeed = 8f;

    private Player _player;
    private Animator _animator;
    private Transform _camera;
    private Transform _hand;
    private Transform _sword;
    private Renderer _swordRenderer;
    private int _meleeLayerIndex;
    private float _weight;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _animator = GetComponent<Animator>();
        _camera = Camera.main.transform;
        _hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
        _meleeLayerIndex = _animator.GetLayerIndex(MeleeLayerName);

        Sword sword = GetComponentInChildren<Sword>(true);
        _sword = sword.transform;
        _swordRenderer = sword.GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        _player.OnWeaponChanged += UpdateSwordVisible;
        UpdateSwordVisible(_player.WeaponMode);
    }

    private void OnDisable()
    {
        _player.OnWeaponChanged -= UpdateSwordVisible;
    }

    private void Update()
    {
        bool isHolding = _player.WeaponMode == WeaponMode.Melee && !IsSlashing();
        _weight = Mathf.MoveTowards(_weight, isHolding ? 1f : 0f, _blendSpeed * Time.deltaTime);
    }

    // 손 위치: 카메라가 바라보는 방향 기준 오른쪽 아래 앞으로 IK 이동한다.
    private void OnAnimatorIK(int layerIndex)
    {
        if (_weight <= 0f) return;

        Vector3 handPosition = _cameraPosition.position + _camera.rotation * _handOffset;

        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _weight);
        _animator.SetIKPosition(AvatarIKGoal.RightHand, handPosition);

        _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, _weight);
        _animator.SetIKHintPosition(AvatarIKHint.RightElbow, _player.transform.TransformPoint(_elbowHint));
    }

    // 손 회전: IK 회전 목표는 리그의 본 축에 따라 어긋나므로, 애니메이션/IK가 끝난 뒤 본을 직접 돌린다.
    // 검이 원하는 방향을 향하도록 손 회전을 역산한다. (검은 손 본의 자식이라 로컬 회전이 고정이다.)
    private void LateUpdate()
    {
        if (_weight <= 0f) return;

        Quaternion swordRotation = _camera.rotation * Quaternion.Euler(_swordEuler);
        Quaternion handRotation = swordRotation * Quaternion.Inverse(_sword.localRotation);
        _hand.rotation = Quaternion.Slerp(_hand.rotation, handRotation, _weight);
    }

    private bool IsSlashing()
    {
        if (_animator.GetCurrentAnimatorStateInfo(_meleeLayerIndex).IsTag(SlashTag)) return true;

        return _animator.IsInTransition(_meleeLayerIndex)
            && _animator.GetNextAnimatorStateInfo(_meleeLayerIndex).IsTag(SlashTag);
    }

    // 근접 모드가 아닐 때는 검을 숨긴다.
    private void UpdateSwordVisible(WeaponMode weaponMode)
    {
        _swordRenderer.enabled = weaponMode == WeaponMode.Melee;
    }
}
