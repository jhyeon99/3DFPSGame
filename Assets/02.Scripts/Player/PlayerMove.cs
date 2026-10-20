using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private float _yVelocity = 0f; // 위아래 속도, 매 프레임마다 중력 가속도가 붙는다.
    
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _dashSpeed = 12f;
    [SerializeField] private float _jumpPower = 10f;
    
    [Header("Stamina")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _dashStaminaPerSec = 10f;
    [SerializeField] private float _jumpStaminaPerOnce = 20f;
    [SerializeField] private float _staminaRecoveryPerSec = 20f;
    [SerializeField] private Image _staminaBar;
    
    [SerializeField] private float _currentStamina;
    
    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        
        _currentStamina =  _maxStamina;
    }
    
    private void Update()
    {
        _currentStamina = Mathf.Min(_currentStamina + _staminaRecoveryPerSec * Time.deltaTime, _maxStamina);
        
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
        }
        else
        {
            // 땅에 서있으면 중력 초기화
            _yVelocity = 0f;
        }

        // 4. 만약에 점프키를 누르면 && 땅이라면 ... 점프를 한다.
        if (Input.GetButtonDown("Jump") && _currentStamina >= _jumpStaminaPerOnce)
        {
            _yVelocity = _jumpPower;
            _currentStamina -= _jumpStaminaPerOnce;
        }

        
        // 5. 속도를 구한다.
        bool isDashing = Input.GetKey(KeyCode.LeftShift) && _currentStamina >= _dashStaminaPerSec * Time.deltaTime;
        if (isDashing)
        {
            _currentStamina -= _dashStaminaPerSec * Time.deltaTime;
        }
            
        float speed = isDashing ? _dashSpeed : _moveSpeed;
        Vector3 velocity = direction * speed;
        velocity.y = _yVelocity;
        
        // 6. 속도(방향과 속력)에 따라 이동한다.
        _characterController.Move(velocity * Time.deltaTime);
        
        _staminaBar.fillAmount = _currentStamina / _maxStamina;
    }
}
