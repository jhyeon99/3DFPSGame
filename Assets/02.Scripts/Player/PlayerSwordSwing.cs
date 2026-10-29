using System.Collections;
using UnityEngine;

// F키를 누르면 오른팔을 위에서 아래로 휘두른다. 검은 손 본의 자식이라 팔을 그대로 따라간다.
public class PlayerSwordSwing : MonoBehaviour
{
    [Header("본")]
    [SerializeField] private Transform _upperArm; // RightArm
    [SerializeField] private Transform _foreArm;  // RightForeArm

    [Header("입력")]
    [SerializeField] private KeyCode _swingKey = KeyCode.F;

    [Header("스윙 시간")]
    [SerializeField] private float _windupTime = 0.15f;
    [SerializeField] private float _slashTime = 0.15f;
    [SerializeField] private float _recoverTime = 0.25f;

    // 팔이 아래를 향할 때를 0도로 보고, 음수일수록 앞쪽 위로 올라간다.
    [Header("스윙 각도")]
    [SerializeField] private float _windupPitch = -160f;
    [SerializeField] private float _slashPitch = -40f;
    [SerializeField] private float _windupYaw = 20f;
    [SerializeField] private float _slashYaw = -20f;

    private bool _isSwinging;
    private float _weight;
    private float _pitch;
    private float _yaw;
    private Quaternion _restLocalRotation;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (Cursor.visible) return;
        if (_isSwinging) return;

        if (Input.GetKeyDown(_swingKey))
        {
            StartCoroutine(Swing_Coroutine());
        }
    }

    // Animator가 매 프레임 포즈를 쓴 뒤에 덮어쓴다. 시작 시점의 포즈를 기준으로 매번 다시 계산해 누적되지 않는다.
    private void LateUpdate()
    {
        if (!_isSwinging) return;

        _upperArm.localRotation = _restLocalRotation;

        Vector3 currentDirection = (_foreArm.position - _upperArm.position).normalized;
        Quaternion fullRotation = Quaternion.FromToRotation(currentDirection, GetTargetDirection());
        _upperArm.rotation = Quaternion.Slerp(Quaternion.identity, fullRotation, _weight) * _upperArm.rotation;
    }

    // 플레이어 기준 팔 방향. pitch 0도는 아래, -90도는 정면, -180도는 위.
    private Vector3 GetTargetDirection()
    {
        float pitchRad = _pitch * Mathf.Deg2Rad;
        Vector3 local = new Vector3(0f, -Mathf.Cos(pitchRad), -Mathf.Sin(pitchRad));
        local = Quaternion.AngleAxis(_yaw, Vector3.up) * local;
        return transform.TransformDirection(local);
    }

    private IEnumerator Swing_Coroutine()
    {
        _isSwinging = true;
        _restLocalRotation = _upperArm.localRotation;
        _pitch = _windupPitch;
        _yaw = _windupYaw;

        // 1. 팔을 들어 올린다(자세 가중치 0 -> 1).
        for (float t = 0f; t < _windupTime; t += Time.deltaTime)
        {
            _weight = Mathf.SmoothStep(0f, 1f, t / _windupTime);
            yield return null;
        }
        _weight = 1f;

        // 2. 내려친다.
        for (float t = 0f; t < _slashTime; t += Time.deltaTime)
        {
            float ratio = Mathf.SmoothStep(0f, 1f, t / _slashTime);
            _pitch = Mathf.Lerp(_windupPitch, _slashPitch, ratio);
            _yaw = Mathf.Lerp(_windupYaw, _slashYaw, ratio);
            yield return null;
        }
        _pitch = _slashPitch;
        _yaw = _slashYaw;

        // 3. 원래 포즈로 돌아온다(가중치 1 -> 0).
        for (float t = 0f; t < _recoverTime; t += Time.deltaTime)
        {
            _weight = Mathf.SmoothStep(1f, 0f, t / _recoverTime);
            yield return null;
        }

        _weight = 0f;
        yield return null; // 가중치 0인 마지막 프레임까지 적용한 뒤 끝낸다.
        _isSwinging = false;
    }
}
