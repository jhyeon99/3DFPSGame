using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"


// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle,       // 대기: 제자리에서 플레이어를 기다린다.
    Trace,      // 추격: 플레이어를 쫓아간다.
    Comeback,    // 복귀: 처음 자리로 돌아간다.
    Attack,     // 공격: 일정 시간마다 플레이어를 때린다.
    Jump,       // 점프: 네비게이션 링크(낙하/점프 구간)를 포물선으로 넘어간다.
    Hit,       // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death,      // 죽음: 쓰러지고 잠시 뒤 사라진다.
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    private const float ArriveTolerance = 0.2f;     // 복귀 지점 도착으로 인정하는 여유 거리

    [Header("스탯")] 
    [SerializeField] private float _detectDistance  = 8;    // 플레이어를 발견하는 거리
    [SerializeField] private float _combackDistance = 15;   // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackDamaga = 10f;     // 공격 데미지
    [SerializeField] private float _attackRange = 1.5f;     // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f;  // 공격 간격
    [SerializeField] private float _moveSpeed = 5;          // 이동속도
    [SerializeField] private float _maxHealth = 30;
    private float _health;

    [Header("점프")]
    [SerializeField] private float _jumpHeight = 2f;        // 포물선의 최고 높이 (출발/도착 지점 위로 이만큼 솟는다)
    [SerializeField] private float _jumpSpeed = 6f;         // 점프 수평 이동속도

    [Header("이펙트")]
    [SerializeField]
    private GameObject _bloodSprayPrefab;
    
    private float _attackTimer = 0f;
    private Vector3 _startPosition;
    private GameObject _player;
    private NavMeshAgent _agent;
    private Animator _animator;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // 체력이 변할 때 구독자(체력바 등)에게 알린다.
    public event Action OnHealthChanged;

    // 좀비가 죽을 때 구독자(스포너 등)에게 알린다.
    public event Action<Zombie> OnDied;

    private ZombieState _state = ZombieState.Idle;

    private void Awake()
    {
        _health = _maxHealth;
    }

    private void Start()
    {
        _startPosition = transform.position;
     
        // 길을 찾아 걷게 해주는 컴포넌트 NavMeshAgent를 찾아서 참조
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;

        // 오프메시 링크(낙하/점프 구간)는 직선으로 자동 통과하지 않고, 직접 포물선으로 넘는다.
        _agent.autoTraverseOffMeshLink = false;

        _animator = GetComponentInChildren<Animator>();
        
        // 게임을 시작하면 플레이어를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");
    }
    

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;
            
            case ZombieState.Trace:
                Trace();
                break;
            
            case  ZombieState.Comeback:
                Comeback();
                break;
            
            case ZombieState.Attack:
                Attack();
                break;
        }
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }
        
        _health -= damageInfo.Value;
        OnHealthChanged?.Invoke();

        StopAllCoroutines();
        
        if (_health > 0f)
        {
            // Todo. 맞은 부위에 피 이펙트를 실행하고 싶다.
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, gameObject.transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            
            // 잠깐 멈칫 하고 싶다!
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            OnDied?.Invoke(this);
            StartCoroutine(Death_Coroutine());
        }
       
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();
        Debug.Log($"{_state} -> Trace");
        _animator.SetTrigger("Hit");
        
        yield return new WaitForSeconds(0.5f);  // 잠깐 멈칫한다!
 
        // 맞았으니 쫒아온다.
        _state = ZombieState.Trace;
    }
    
    private IEnumerator Death_Coroutine()
    {
        StopAgent();
        _animator.SetTrigger("Death");
        yield return new WaitForSeconds(2.0f); 
        Destroy(gameObject);
    }

    // 대기: 제자리에서 플레이어를 기다린다.
    private void Idle()
    {
        StopAgent();

        // 플레이어 가까워지면 Trace 상태로 전이한다.
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("IdleToMove");
            _state = ZombieState.Trace;
        }
    }

    // 추격: 플레이어를 쫓아간다.
    private void Trace()
    {
        if (TryStartJump()) return;

        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _combackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _agent.SetDestination(_startPosition);
            _state = ZombieState.Comeback;
            return;
        }
        
        // 플레이어가 공격 거리 안이면 공격한다.
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _animator.SetTrigger("MoveToAttackDelay");
            _state = ZombieState.Attack;
            return;
        }

        // SetDestination은 목적지 설정
        // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
        // - 한번 세팅하면 내부에서 계속해서 이동 (O)
        _agent.SetDestination(_player.transform.position);
    }

    // 복귀: 처음 자리로 돌아간다.
    private void Comeback()
    {
        if (TryStartJump()) return;

        // 경로 계산이 끝나기 전에는 remainingDistance가 신뢰할 수 없으므로 기다린다.
        if (_agent.pathPending) return;

        // 처음 자리에 도착하면 Idle
        // (pathStatus는 "경로가 유효한지"만 알려주므로 도착 판정에 쓰면 안 된다.)
        if (_agent.remainingDistance <= _agent.stoppingDistance + ArriveTolerance)
        {
            Debug.Log($"{_state} -> Idle");
            _animator.SetTrigger("MoveToIdle");
            _state = ZombieState.Idle;
        }
    }

    // 공격: 일정 시간마다 플레이어를 때린다.
    private void Attack()
    {
        StopAgent();

        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> Trace");
            _animator.SetTrigger("AttackMove");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            _animator.SetTrigger("StartAttack");

            _attackTimer = 0f;
        }
       
    }

    public void AttackAction()
    {
        DamageInfo damageInfo = new DamageInfo()
        {
            Value = _attackDamaga,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized,
        };

        _player.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
    }

    // 오프메시 링크 위에 도달했으면 점프를 시작한다.
    private bool TryStartJump()
    {
        if (!_agent.isOnOffMeshLink) return false;

        Debug.Log($"{_state} -> Jump");
        StartCoroutine(Jump_Coroutine(_state, _agent.currentOffMeshLinkData.endPos));
        _state = ZombieState.Jump;
        return true;
    }

    // 현재 위치에서 링크 도착 지점까지 포물선으로 이동한다.
    private IEnumerator Jump_Coroutine(ZombieState returnState, Vector3 endPos)
    {
        // 에이전트 높이(baseOffset)만큼 올려서 도착 지점에 발이 닿게 한다.
        endPos.y += _agent.baseOffset;

        Vector3 startPos = transform.position;
        float duration = Mathf.Max(Vector3.Distance(startPos, endPos) / _jumpSpeed, 0.1f);

        Vector3 flat = endPos - startPos;
        flat.y = 0f;
        if (flat != Vector3.zero)
        {
            transform.forward = flat.normalized;
        }

        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            // 출발->도착 직선 위에, 4h·t(1-t) 포물선 높이를 더한다. (t=0.5에서 최고점 h)
            Vector3 position = Vector3.Lerp(startPos, endPos, t);
            position.y += _jumpHeight * 4f * t * (1f - t);
            transform.position = position;
            yield return null;
        }

        transform.position = endPos;
        _agent.CompleteOffMeshLink();

        Debug.Log($"Jump -> {returnState}");
        _state = returnState;
    }

    private void StopAgent()
    {
        _agent.ResetPath();                     // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero;         // 미끄럼 방지를 위해 가속도 제로!
    }
}