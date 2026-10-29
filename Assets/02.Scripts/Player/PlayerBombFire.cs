using System;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던지고 싶다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _defaultThrowPower;
    [SerializeField] private float _maxThrowPower;
    [SerializeField] private float _chargeThrowPowerPerSec;

    [SerializeField] private int _startBomb;
    private float _throwPower;
    private int _remainBomb;

    private Player _player;

    public int RemainBomb => _remainBomb;

    public event Action<int> OnBombChanged;

    private void Awake()
    {
        _remainBomb = _startBomb;
    }

    private void Start()
    {
        _player = GetComponent<Player>();
        
        // 폭탄 풀을 미리 만들어 둔다 (ProjectilePool 인스펙터에서 미리 만들기 개수 설정 가능)
        ProjectilePool.Instance.Register(_bombPrefab);
    }
    
   private void Update()
   {
       if (GameManager.Instance.State != GameState.Playing) return;
       if (_player.WeaponMode != WeaponMode.Normal) return;
        
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            _throwPower = _defaultThrowPower;
        }
        else if (Input.GetButton("Fire2"))
        {
            _throwPower = Mathf.Min(_maxThrowPower, _throwPower + _chargeThrowPowerPerSec * Time.deltaTime);
        }
        else if (Input.GetButtonUp("Fire2"))
        {
            Fire();
        }
        
    }

    private void Fire()
    {
        if (_remainBomb <= 0) return;

        // 2. 발사 위치에서 풀에 있는 수류탄 꺼내기 (풀이 가득 차면 null)
        Bomb bomb = ProjectilePool.Instance.Get(_bombPrefab, _firePoint.position, Quaternion.identity);
        if (bomb == null) return;

        _remainBomb--;
        OnBombChanged?.Invoke(_remainBomb);

        // 3. 꺼낸 수류탄을 시선 방향으로 던지기
        bomb.Rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);
    }
}
