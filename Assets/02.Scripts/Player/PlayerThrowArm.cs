using System.Collections;
using UnityEngine;

// 폭탄을 던질 때 오른팔만 움직인다.
// 우클릭을 누르는 동안 팔을 뒤로 젖히고, 떼면 머리 위로 넘겨 던진 뒤 원래 포즈로 돌아온다.
public class PlayerThrowArm : MonoBehaviour
{
    // 팔이 아래를 향할 때 0도, 뒤 90도, 위 180도, 앞 270도.
    [Header("각도")]
    [SerializeField] private float _windupPitch = 110f;
    [SerializeField] private float _throwPitch = 290f;

    [Header("시간")]
    [SerializeField] private float _windupTime = 0.2f;
    [SerializeField] private float _throwTime = 0.15f;
    [SerializeField] private float _recoverTime = 0.25f;

    private Transform _upperArm;
    private Transform _foreArm;
    private Quaternion _restLocalRotation;
    private Coroutine _motion;
    private bool _isActive;
    private float _pitch;
    private float _weight;

    private void Awake()
    {
        Animator animator = GetComponentInChildren<Animator>();
        _upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _foreArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

        if (_upperArm == null || _foreArm == null)
        {
            Debug.LogError("오른팔 본을 찾지 못했습니다. 모델의 Humanoid 아바타를 확인하세요.");
            enabled = false;
        }
    }

    // 팔을 뒤로 젖히며 던질 준비를 한다.
    public void BeginWindup()
    {
        if (!enabled) return;

        StopMotion();
        Activate();
        _motion = StartCoroutine(Windup_Coroutine());
    }

    // 젖힌 팔을 앞으로 휘둘러 던진다. 준비 동작 없이 호출돼도 자연스럽게 이어진다.
    public void Throw()
    {
        if (!enabled) return;

        StopMotion();
        if (!_isActive)
        {
            Activate();
            _weight = 1f;
        }
        _motion = StartCoroutine(Throw_Coroutine());
    }

    // 던지지 않고 준비 동작만 취소한다(폭탄이 없을 때).
    public void Cancel()
    {
        if (!enabled || !_isActive) return;

        StopMotion();
        _motion = StartCoroutine(Recover_Coroutine());
    }

    // Animator가 매 프레임 포즈를 쓴 뒤에 덮어쓴다. 시작 시점의 포즈를 기준으로 매번 다시 계산해 누적되지 않는다.
    private void LateUpdate()
    {
        if (!_isActive) return;

        _upperArm.localRotation = _restLocalRotation;

        Vector3 currentDirection = (_foreArm.position - _upperArm.position).normalized;
        Quaternion fullRotation = Quaternion.FromToRotation(currentDirection, GetTargetDirection());
        _upperArm.rotation = Quaternion.Slerp(Quaternion.identity, fullRotation, _weight) * _upperArm.rotation;
    }

    // 플레이어 기준 팔 방향. pitch 0도는 아래이고 값이 커질수록 뒤 -> 위 -> 앞으로 돈다.
    private Vector3 GetTargetDirection()
    {
        float pitchRad = _pitch * Mathf.Deg2Rad;
        Vector3 local = new Vector3(0f, -Mathf.Cos(pitchRad), -Mathf.Sin(pitchRad));
        return transform.TransformDirection(local);
    }

    private void Activate()
    {
        _restLocalRotation = _upperArm.localRotation;
        _pitch = _windupPitch;
        _weight = 0f;
        _isActive = true;
    }

    private void StopMotion()
    {
        if (_motion != null)
        {
            StopCoroutine(_motion);
            _motion = null;
        }
    }

    private IEnumerator Windup_Coroutine()
    {
        float startWeight = _weight;
        for (float t = 0f; t < _windupTime; t += Time.deltaTime)
        {
            _weight = Mathf.SmoothStep(startWeight, 1f, t / _windupTime);
            yield return null;
        }
        _weight = 1f;
        _motion = null;
    }

    private IEnumerator Throw_Coroutine()
    {
        _weight = 1f;
        for (float t = 0f; t < _throwTime; t += Time.deltaTime)
        {
            _pitch = Mathf.Lerp(_windupPitch, _throwPitch, Mathf.SmoothStep(0f, 1f, t / _throwTime));
            yield return null;
        }
        _pitch = _throwPitch;

        yield return Recover_Coroutine();
    }

    private IEnumerator Recover_Coroutine()
    {
        float startWeight = _weight;
        for (float t = 0f; t < _recoverTime; t += Time.deltaTime)
        {
            _weight = Mathf.SmoothStep(startWeight, 0f, t / _recoverTime);
            yield return null;
        }

        _weight = 0f;
        yield return null; // 가중치 0인 마지막 프레임까지 적용한 뒤 끝낸다.
        _isActive = false;
        _motion = null;
    }
}
