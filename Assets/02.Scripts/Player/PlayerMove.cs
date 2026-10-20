using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private const int MaxJumps = 2;
    private const float MaxStamina = 100f;
    
    private float _yVelocity = 0f; // 위아래 속도, 매 프레임마다 중력 가속도가 붙는다.
    private int _jumpCount = 0; // 점프 횟수
    private float _stamina = 0f;

    [Header("스탯")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _sprintSpeed = 12f;
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private float _staminaRecovery = 15f;
    [SerializeField] private float _sprintCost = 10f;
    [SerializeField] private float _jumpCost = 20f;

    [Header("조작감 개선")]
    [SerializeField] private float _coyoteTime = 0.15f; // 코요테 타임 유예 시간
    [SerializeField] private float _fallMultiplier = 2.0f; // 빠른 낙하 중력 배수
    private float _coyoteTimer = 0f; // 코요테 타임 타이머

    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = MaxStamina;
    }
    
    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        float h = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        float v = Input.GetAxisRaw("Vertical"); // -1, 0, 1
        
        // 2. 키보드 입력에 따라 방향을 구한다.
        Vector3 direction = new Vector3(h, 0, v);
        direction.Normalize(); // 대각선 이동시 속도 유지
        direction = transform.TransformDirection(direction);
        // 로컬 좌표계: 한 대상을 중심
        // 월드 좌표계: 세상을 중심

        // 3. 중력을 누적한다.
        if (_characterController.isGrounded == false)
        {
            _yVelocity += Gravity * Time.deltaTime;
            _coyoteTimer -= Time.deltaTime; // 공중에 있으면 코요테 타이머 차감

            // 빠른 낙하: 떨어질 때는 중력을 더 세게 준다.
            float currentGravity = Gravity;
            if (_yVelocity < 0)
            {
                currentGravity *= _fallMultiplier;
            }

            _yVelocity += currentGravity * Time.deltaTime;
        }
        else
        {
            // 땅에 서있으면 중력 초기화
            _yVelocity = -1f;
            _jumpCount = 0;
            _coyoteTimer = _coyoteTime; // 코요테 타이머 초기화
        }

        // 4. 만약에 점프키를 누르면 && 점프 카운트가 2 이하라면 ... 점프를 한다.
        // 코요테 타임 적용: 첫 점프는 땅에 있거나 코요테 시간 내라면 허용
        bool canFirstJump = _jumpCount == 0 && _coyoteTimer > 0f;
        bool canMultiJump = _jumpCount > 0 && _jumpCount < MaxJumps;

        if (Input.GetButtonDown("Jump") && (canFirstJump || canMultiJump) && _stamina >= _jumpCost)
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            _stamina -= _jumpCost;
            _coyoteTimer = 0f; // 점프 직후 코요테 타임 종료
        }
        
        // 5. 속도를 구한다.
        float requiredStamina = _sprintCost * Time.deltaTime;
        bool isSprint = Input.GetButton("Sprint") && _stamina >= requiredStamina;
        float currentSpeed = _moveSpeed;
        if (isSprint)
        {
            currentSpeed = _sprintSpeed;
            _stamina -= requiredStamina;
        }
        else
        {
            _stamina = Mathf.Min(MaxStamina, _stamina + _staminaRecovery *  Time.deltaTime);
        }
        Vector3 velocity = direction * currentSpeed;
        velocity.y = _yVelocity;
        
        // 6. 속도(방향과 속력)에 따라 이동한다.
        //transform.position += direction * _moveSpeed * Time.deltaTime;
        _characterController.Move(velocity * Time.deltaTime);
    }
}
