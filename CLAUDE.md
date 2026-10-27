# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`, URP) 기반 3D FPS 학습용 게임. 플레이어가 좀비를 총·폭탄으로 처치하고, 폭발하는 드럼통 등 환경 오브젝트와 상호작용한다. 메인 씬은 `Assets/Workshop/GameScene.unity`.

게임 코드는 `Assets/02.Scripts/` 아래에만 작성한다. 에셋 폴더는 번호 접두사(`01.Scenes`, `02.Scripts`, `03.Prefabs` …)로 정리되어 있다.

## 명령어

Unity 에디터에서 빌드·실행하며, 별도의 CLI 빌드 스크립트나 테스트 코드는 없다.

```bash
# 최초 1회: 로컬 dotnet 도구(Husky.Net) 복원 및 git hook 설치
dotnet tool restore
dotnet husky install

# 포맷/네이밍 검사 (pre-commit hook과 동일)
dotnet format 3DFPSGame.sln --verify-no-changes --severity warn --include Assets/02.Scripts/

# 자동 수정
dotnet format 3DFPSGame.sln --severity warn --include Assets/02.Scripts/
```

`.sln`/`.csproj`는 Unity가 생성하는 파일이므로, 스크립트를 추가한 뒤 Unity 에디터가 프로젝트 파일을 갱신해야 `dotnet format`이 새 파일을 인식한다.

## Git 규칙

- **커밋 메시지**: `.husky/commit-msg`가 `type: 설명` 형식을 강제한다. 허용 type: `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` (예: `feat: 미니맵 줌 버튼`). 설명은 한국어로 쓴다.
- **pre-commit**: 위 `dotnet format --verify-no-changes`가 통과해야 커밋된다.
- **Git LFS** 사용 중 (push/checkout hook이 `git-lfs`를 요구).
- **CI**: `main`에 `Assets/02.Scripts/**/*.cs` 변경이 push되면 `.github/workflows/gemini-commit-review.yml`이 Gemini로 커밋 리뷰를 수행한다. 리뷰 기준은 `Docs/CodeReviewGuide.md` (중요도 Critical/High/Medium/Low, 매직 넘버, 묻지 말고 시켜라, SOLID를 과하지 않게 적용, Unity 생명주기·할당 확인 등). 코드를 작성하거나 리뷰할 때 이 기준을 따른다.

## 코드 스타일 (`.editorconfig`, `Assets/02.Scripts/**.cs`에만 적용)

- 4칸 스페이스, Allman 스타일 중괄호. 한 줄 조기 return은 중괄호 생략 가능, 여러 줄이면 중괄호 필수.
- private 필드 `_camelCase`, private `const`/`static readonly`는 `PascalCase`, 타입은 `PascalCase`(언더스코어 금지 — 단, 기존 `UI_` 접두사 클래스는 예외로 존재), 지역 변수 `camelCase`.
- 인스펙터 노출은 `[SerializeField] private` 필드 + 읽기 전용 프로퍼티(`public float Health => _health;`) 패턴을 사용한다.
- 코루틴 메서드는 `이름_Coroutine` 접미사를 붙인다.
- 주석은 한국어로 작성한다.

## 아키텍처

### 게임 상태 (`Game/GameManager.cs`)
`GameManager.Instance`(싱글톤)가 `GameState`(`Ready → Playing → GameOver`)를 가진다. 플레이어 입력, 좀비 AI, 커서 처리 등 대부분의 `Update()`는 맨 앞에서 `GameManager.Instance.State != GameState.Playing`이면 return한다. 새 게임플레이 컴포넌트도 이 가드를 따른다.

### 데미지 시스템 (`Core/`)
피해를 받는 모든 대상(`Zombie`, `PlayerHealth`, `Drum`, `Box`)은 `IDamageable.TakeDamage(DamageInfo)`를 구현한다. 공격 측은 구체 타입이 아니라 `GetComponent<IDamageable>()?.TakeDamage(...)`로 호출한다. `DamageInfo`는 데미지 값·맞은 위치·법선을 담는 struct이다. 범위 공격(`Drum`)은 `LayerMask.GetMask("Drum", "Zombie")`로 대상을 찾는다.

### 오브젝트 풀 (`Pool/` + `Assets/Modules/Submodules/Pools`)
외부 라이브러리 `Redcode.Pools`(별도 asmdef `Pools`)를 감싼 구조:
- `PrefabPoolTable<T>`: 프리팹별 `IPool<T>`를 관리하고, 복제본→풀 역매핑과 이중 반납 방지를 담당.
- `PrefabPoolBehaviour<T>`: 인스펙터 설정(MaxCount, Preload)을 받는 공통 MonoBehaviour 부모. 미등록 프리팹은 첫 사용 시 자동 등록.
- `ProjectilePool`, `EffectPool`: 자동 생성 싱글톤(`Instance` 접근 시 씬에 없으면 생성). `EffectPool.Play()`는 파티클이 끝나면 자동 반납하며, 프리팹의 Stop Action(Destroy/Disable)을 None으로 바꿔 풀을 보호한다.
- `Weapon/Projectile`: 풀링되는 투사체의 추상 부모(`IPoolObject` 구현). 꺼낼 때 Rigidbody 속도를 초기화하고, 자식은 `OnSpawned()`를 오버라이드하며 `ReturnToPool()`로 반납한다.

일부 코드(총 피격 이펙트, 좀비 피 이펙트, 드럼 폭발)는 아직 `Instantiate/Destroy`를 직접 사용한다. 새 이펙트·투사체는 풀을 사용한다.

### 좀비 AI (`Monster/Zombie.cs`)
enum 기반 FSM(`Idle, Trace, Comeback, Attack, Hit, Death`). `Update()`의 switch가 상태별 메서드를 호출하고, `Hit`/`Death`는 `TakeDamage`에서 `StopAllCoroutines()` 후 코루틴으로 처리한다. `CharacterController.SimpleMove`로 이동하며 플레이어는 `"Player"` 태그로 찾는다.

### 입력과 커서
레거시 `Input` 매니저(`Input.GetButton("Fire1")`, `"Reload"` 등)를 사용한다(Active Input Handling = Both). `CursorManager`가 기본적으로 커서를 잠그고, Left Alt를 누르는 동안 커서를 표시한다. `PlayerGunFire`는 `Cursor.visible`이 true면 발사하지 않으므로 UI 조작 중 사격이 잠긴다.

### 로깅 (`Game/GameLogger.cs`)
`GameLogger.Log`는 `[Conditional("ENABLE_LOG")]`라 해당 심볼이 정의되지 않으면 호출 자체가 제거된다(현재 미정의). `Info`는 `LogWarning`, `LogError`는 항상 출력된다.

### UI
`UI_` 접두사 클래스(`UI_PlayerStats`, `UI_HitEffect`, `UI_Minimap`)와 TextMeshPro를 사용한다. 미니맵은 `MinimapCamera` + RenderTexture + 마스크 방식이다.
