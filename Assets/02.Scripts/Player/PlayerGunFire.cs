using System.Collections;
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
    [SerializeField] private float _reloadingTime = 3f;
    
    private float _fireTimer = 0f;
    private float _currentBullet = 0f;
    private Coroutine _reloadCoroutine;

    private void Awake()
    {
        _currentBullet = _maxBullet;
    }

    private void Start()
    {
        // 피격 이펙트 풀을 미리 만들어 둔다 (EffectPool 인스펙터에서 미리 만들기 개수 설정 가능)
        EffectPool.Instance.Register(_hitEffectPrefab);
    }
    
    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;
        
        // 1. 마우스 왼쪽 버튼 입력 받기
        bool isMouseVisible = Cursor.visible;
        if (isMouseVisible == false && Input.GetButton("Fire1") && _fireTimer <= 0f && _currentBullet > 0)
        {
            _currentBullet--;
            _fireTimer = _fireInterval;
            Shoot();
            
            // 이 방식은 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소 
            //StopAllCoroutines();
            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소");
                _reloadCoroutine = null;
            }
        }

        // 2. R키 누르면 총알 재장전
        if (Input.GetButtonDown("Reload") && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작");
        
        yield return new WaitForSeconds(_reloadingTime);
        
        _currentBullet = _maxBullet;
        
        Debug.Log("재장전 종료");
        _reloadCoroutine = null;
    }

    private void Shoot()
    {   
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
            
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;
            
            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
            
            /*
            hitInfo.collider.GetComponent<Zombie>()?.TakeDamage(_damage);
            hitInfo.collider.GetComponent<Drum>()?.TakeDamage(_damage);
            hitInfo.collider.GetComponent<Box>()?.TakeDamage(_damage);
            */
        }
    }
}
