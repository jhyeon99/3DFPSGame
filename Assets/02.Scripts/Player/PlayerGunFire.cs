using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem _hitEffectPrefab;
    
    [Header("스탯")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireInterval = 0.1f;
    [SerializeField] private float _maxBullet = 50f;
    [SerializeField] private float _reloadingTime = 0.5f;
    private float _fireTimer = 0f;
    private float _currentBullet = 0f;
    private bool _isReloading = false;
    private float _reloadingTimer = 0f;

    private void Awake()
    {
        _currentBullet = _maxBullet;
    }
    
    private void Update()
    {
        _reloadingTimer -= Time.deltaTime;
        if (_reloadingTimer <= 0f)
        {
            _isReloading = false;
        }

        if (_isReloading) return;
        
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;
        
        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f)
        {
            _fireTimer = _fireInterval;
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _currentBullet = _maxBullet;
            _reloadingTimer = _reloadingTime;
            _isReloading = true;
        }
    }

    private void Shoot()
    {
        if (_currentBullet <= 0) return;
        _currentBullet--;
        
        // Ray: 레이저(시작 위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딪히면 그 물체를 
        
        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();
        
        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알에 맞았다, 데미지: {_damage}");
            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();
        }
    }
}
