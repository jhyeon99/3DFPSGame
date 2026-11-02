# 조명·포스트 프로세싱·최적화 보고서 (초안)

> 작성 방식: 항목을 위에서부터 하나씩 적용하고, 단계마다 **변경 전 → 변경 → 변경 후 → 분석**을 요약한다.
> 상태 표시: ✅ 완료 / ⬜ 진행 예정. `[직접 작성]`은 본인이 채울 부분이다.

## 0. 개요
- 프로젝트: 3D FPS 학습용 게임 (Unity 6 `6000.3.22f1`, URP, 씬 `GameScene`)
- 목표: 적은 비용으로 분위기(연출)를 높이고 GPU/CPU/RAM 부하를 줄인다.
- AI 활용: Claude Code로 현황 분석, 설정 변경, 측정 스크립트 작성, 스크린샷 촬영을 진행했다.

### 공통 측정 조건 (모든 단계 동일)
- 환경: Unity 에디터 Play 모드, Game 뷰 1920×1080 / Scale 0.43x, `PC` 품질, 에디터 창을 앞에 둔 상태
- 장면: 시작 위치·같은 방향. 좀비 스포너를 끄고 좀비를 제거해 좀비 0마리, 스킨 메쉬 1개로 통일
- 수치: `PerfSampler`(`Assets/02.Scripts/Test/PerfSampler.cs`)로 워밍업 120프레임 후 900프레임 평균 → `perf_*.json`
- 화면: 같은 장면에서 Game 화면, Stats, Profiler(중앙값 프레임) 캡처
- 각 단계는 **직전 단계가 적용된 상태**를 Before로, 해당 변경을 적용한 상태를 After로 비교한다.
- 한계: 에디터 측정이라 FPS 변동이 크다(같은 설정에서 105~239 FPS). 실제 빌드 측정이 아니다.

**측정 프로토콜 (단계 2부터 적용, 이전 단계는 아래 주의 참고)**
1. Profiler 기록을 끄고 이전 프레임 기록을 비운 뒤 Play (Profiler 기록이 쌓이면 Total Used Memory가 수 GB로 부풀고 프레임 시간이 달라졌다)
2. 좀비 스포너를 끄고 좀비를 제거한 상태에서 `PerfSampler` 실행 (Profiler 꺼짐)
3. 측정이 끝나면 Profiler를 4초만 켜서 기록하고, 중앙값 프레임을 선택해 Stats·Profiler 화면을 캡처

> **주의**: 단계 1의 `perf_before/after.json`은 위 프로토콜 이전에 측정해 평균 프레임이 4.7ms대로 나왔고, 같은 상태를 새 프로토콜로 다시 재면 2.4ms대였다. 두 프로토콜의 절대값(FPS, ms, 메모리)은 비교할 수 없다. 단계 1 안에서의 Before/After 비교(Shadow Casters 등 렌더 카운트)는 유효하다.

## 진행 요약

| 단계 | 내용 | 상태 | 핵심 결과 |
|---|---|---|---|
| 기준 | 변경 전 측정 | ✅ | Draw Calls 574, Tris 415k, Shadow Casters 721 |
| 1 | 조명·그림자 튜닝 (Realtime) | ✅ | Shadow Casters -54%, Tris -31%, Draw Calls -26%, FPS 변화 없음 |
| 2 | 포스트 프로세싱 (Color Adjustments 추가, Bloom·SSAO 비용 절감) | ✅ | 색감 개선, 프레임 2.44→2.34ms(-4%, 오차 범위), 메모리 -84MB |
| 3 | Mixed/Baked 조명 (Shadowmask), Light Probe | ✅ | Shadow Casters 342→2, Draw Calls -33%, 프레임 -9%, 메모리 +40MB |
| 4 | CPU 최적화 (좀비 경로 갱신, 이펙트 풀링, 로그·Camera.main) | ✅ | `Zombie.Update()` -70%, `Fire()` -54%, 전체 프레임 변화 없음 |
| 5 | RAM 최적화 (텍스처 6개 2048→1024) | ✅ | 씬 텍스처 -32MB(-28%), 런타임 Texture Memory -31MB, 화질 차이 없음 |

---

## 기준: 변경 전 상태

| 항목 | 상태 |
|---|---|
| 라이트 | Directional 1개(Intensity 2, Soft Shadow, Realtime) + 총구 화염 Point Light 5개(Intensity 4, Range 4.1, 그림자 없음, Realtime) |
| 베이크 | 라이트맵·Light Probe·Reflection Probe 없음 (전부 Realtime) |
| 환경광 | Skybox 방식, `RenderSettings.sun` 미지정, Fog 꺼짐 |
| URP 그림자 | Shadow Distance 50, Cascade 4, Main Shadow 2048, Soft Shadow Quality High, 추가 광원 그림자 맵 2048 |
| 포스트 프로세싱 | Global Volume: Tonemapping(Neutral), Bloom(Threshold 1, Intensity 0.25, High Quality Filtering 켜짐), Vignette(0.2). `PC_Renderer`에 SSAO Renderer Feature 켜짐(Intensity 0.4, Downsample 꺼짐). Color Adjustments 없음 |

---

## 단계 1. 조명·그림자 튜닝 ✅

### 목표
Realtime 조명 구조는 유지하되 그림자 비용을 줄이고, 환경광·안개로 분위기를 개선한다.

### 변경 내용
**씬 (`GameScene.unity`)**

| 대상 | 변경 전 | 변경 후 | 이유 |
|---|---|---|---|
| Directional Light Intensity | 2 | 1.3 | 과노출을 줄여 그림자 대비를 살림 |
| `RenderSettings.sun` | 미지정 | Directional Light | 스카이박스와 태양 연동 |
| Fog | 꺼짐 | Exponential Squared, 밀도 0.008, 청회색 | 원경을 부드럽게 하고 분위기 개선 |
| Ambient | Skybox | Trilight (Sky 0.36/0.42/0.50, Equator 0.22/0.24/0.26, Ground 0.10/0.09/0.08) | 그림자 영역이 너무 검지 않게 |
| 총구 화염 Point Light ×5 | Range 4.1 / Intensity 4 | Range 3 / Intensity 3 | 영향 범위를 줄여 라이트 비용 감소 |

**URP 에셋 (`PC_RPAsset.asset`)**

| 항목 | 변경 전 | 변경 후 |
|---|---|---|
| Shadow Distance | 50 | 30 |
| Shadow Cascade Count | 4 | 2 |
| Soft Shadow Quality | High(3) | Medium(2) |
| Additional Lights Shadow Atlas | 2048 | 1024 |

### 결과 (Before → After)

**`PerfSampler` 900프레임 평균** (`perf_before.json` → `perf_after.json`)

| 항목 | Before | After | 변화 |
|---|---|---|---|
| Draw Calls | 574 | 426 | -26% |
| Batches | 162 | 148 | -9% |
| SetPass Calls | 70 | 63 | -10% |
| Triangles | 415,406 | 286,997 | -31% |
| Vertices | 445,686 | 294,014 | -34% |
| Shadow Casters | 721 | 335 | -54% |
| Total Used Memory | 1794.8 MB | 1771.4 MB | -1.3% |
| 평균 FPS | 212.3 | 209.4 | 차이 없음 |
| 평균 프레임 | 4.71 ms | 4.78 ms | 차이 없음 |
| p95 프레임 | 5.54 ms | 6.15 ms | 약간 증가 |
| GC Alloc/프레임 | 48,647 B | 49,820 B | 차이 없음 |

**Stats·Profiler 화면** (같은 장면, 중앙값 프레임)

| 항목 | Before | After |
|---|---|---|
| Batches | 160 | 143 |
| SetPass Calls | 69 | 62 |
| Tris | 445k | 286k |
| Shadow Casters | 702 | 328 |
| Profiler CPU (선택 프레임) | 3.15 ms | 3.06 ms |

### 증거
수치: `perf_before.json` / `perf_after.json`

**게임 화면**

![Before (단계 1 전)](before_game.png)

![After (단계 1 후)](after_game.png)

**Stats + Profiler**

![Before](before_stats_profiler.png)

![After](after_stats_profiler.png)

### 분석
- **효과가 확인된 것**: Shadow Distance 50→30과 Cascade 4→2로 그림자 맵에 그려지는 물체가 줄어 Shadow Casters가 약 절반으로 줄었고, 그림자 패스의 삼각형·드로우콜이 함께 줄었다.
- **FPS가 변하지 않은 이유(추정)**: 에디터에서 약 200 FPS 이상이라 CPU(메인 스레드)·에디터 오버헤드에 걸려 있고, 프레임 시간이 3~5ms 수준이라 GPU 절감이 측정 오차에 묻혔다. 사양이 낮은 기기나 빌드에서 재측정해야 확인된다.
- **p95 증가(5.54→6.15ms)**: 에디터 측정 변동으로 보이며 성능 저하의 근거로 보기는 어렵다.
- **메모리가 거의 같은 이유**: 텍스처·메쉬 에셋을 바꾸지 않았다. 약 23MB 감소는 그림자 맵 관련 렌더 타깃 축소 때문일 수 있으나 확인하지 않았다.
- **연출**: Trilight 환경광으로 그림자 영역이 덜 어둡고 안개로 원경이 부드러워졌다. 정량 지표는 없고 스크린샷으로만 확인했다.
- **한계**: 위 변경은 한 번에 묶어서 측정했다. 변경별 기여도(예: Cascade만의 효과)는 분리하지 못했다.

### 결론
렌더링 작업량(Shadow Casters, Tris, Draw Calls)은 크게 줄었으나 에디터 환경에서 FPS 개선으로는 이어지지 않았다.

---

## 단계 2. 포스트 프로세싱 ✅

### 목표
Color Adjustments를 추가해 색감을 개선하고(2-A), 비용이 큰 Bloom 고품질 필터와 SSAO 풀 해상도 계산을 줄여 GPU 부담을 낮춘다(2-B). 2-A와 2-B를 따로 측정해 각 변경의 영향을 분리했다.

### 변경 내용
| 하위 단계 | 대상 | 변경 전 | 변경 후 | 이유 |
|---|---|---|---|---|
| 2-A | Color Adjustments (Global Volume) | 없음 | Post Exposure 0, Contrast +12, Saturation +10, Color Filter (1, 0.97, 0.92) | 약간 따뜻하고 선명한 색감 |
| 2-B | Bloom `High Quality Filtering` | 켜짐 | 꺼짐 | 블룸 필터 샘플링 비용 절감 |
| 2-B | SSAO `Downsample` (`PC_Renderer`) | 꺼짐 | 켜짐 | AO를 절반 해상도로 계산해 렌더 타깃·샘플링 비용 절감 |

변경 파일: `Assets/Settings/SampleSceneProfile.asset`, `Assets/Settings/PC_Renderer.asset`

### 결과 (Before → After)
**`PerfSampler` 900프레임 평균** (모두 단계 2 프로토콜)

| 항목 | 단계 1 후 (Before) | 2-A Color Adjustments | 2-B Bloom·SSAO 절감 (최종) | 최종 변화 |
|---|---|---|---|---|
| 평균 프레임 | 2.44 ms | 2.40 ms | 2.34 ms | -4% |
| 평균 FPS | 409.7 | 417.3 | 426.7 | +4% |
| p95 프레임 | 3.10 ms | 3.09 ms | 2.97 ms | -4% |
| Draw Calls | 430 | 435 | 440 | 차이 없음 |
| Batches | 148 | 149 | 149 | 차이 없음 |
| SetPass Calls | 63 | 63 | 63 | 차이 없음 |
| Triangles | 287,129 | 287,620 | 287,570 | 차이 없음 |
| Shadow Casters | 337 | 339 | 342 | 차이 없음 |
| GC Alloc/프레임 | 34,177 B | 28,993 B | 34,979 B | 차이 없음 |
| Total Used Memory | 2238.9 MB | 2231.9 MB | 2155.2 MB | -83.7 MB (-3.7%) |

**Stats·Profiler 화면** (중앙값 프레임, Profiler 켠 상태라 ms는 위 평균보다 큼)

| 항목 | Before | 2-A | 2-B |
|---|---|---|---|
| Profiler CPU (선택 프레임) | 2.87 ms | 3.20 ms | 3.22 ms |

### 증거
수치: `perf_s1_base.json` → `perf_s2a_coloradj.json` → `perf_s2b_trim.json`

**게임 화면**

![Before (단계 1 후 상태)](s2_before_game.png)

![2-A (Color Adjustments 추가)](s2a_game.png)

![2-B (Bloom·SSAO 절감)](s2b_game.png)

**Stats + Profiler**

![Before](s2_before_stats_profiler.png)

![2-A](s2a_stats_profiler.png)

![2-B](s2b_stats_profiler.png)

### 분석
- **2-A (Color Adjustments)**: 비용 변화가 없다(2.44→2.40ms, 오차 범위). 색 보정은 이미 돌고 있는 후처리 패스 안에서 처리되어 추가 패스가 생기지 않기 때문으로 보인다. 화면은 따뜻한 톤과 대비가 조금 강해졌다.
- **2-B (Bloom·SSAO 절감)**: 평균 프레임이 2.40→2.34ms로 줄었으나 0.06ms 차이라 측정 오차와 구분하기 어렵다. 메모리는 2231.9→2155.2MB로 약 77MB 줄었다. SSAO를 절반 해상도로 계산해 렌더 타깃이 작아진 영향으로 추정하지만 원인을 분리 검증하지는 않았다.
- **화질**: 2-B 후 Game 화면(`s2b_game.png`)에서 눈에 띄는 노이즈나 번짐 증가는 보이지 않았다. 비교는 육안 확인이다.
- **Profiler 화면의 CPU 증가(2.87→3.22ms)**: Profiler를 켠 상태의 프레임이라 오버헤드가 포함되고, 선택한 중앙값 프레임이 매번 다르다. 단계 간 성능 비교는 위 `PerfSampler` 평균을 기준으로 한다.
- **한계**: GPU 시간은 에디터에서 `--ms`로 나와 측정하지 못했다. 이 장면에서는 CPU 병목이 크고 GPU 부담이 작아 절감 효과가 FPS에 거의 드러나지 않는다.

### 결론
색감은 비용 없이 개선했고, Bloom·SSAO 절감은 눈에 띄는 화질 저하 없이 메모리를 소폭 줄였다. 프레임 시간 개선은 측정 오차 범위 안이다.

> Unity가 `PC_Renderer.asset`을 저장하면서 자동으로 에셋 버전을 올려(`m_AssetVersion` 2→3) 변경과 무관한 줄이 diff에 함께 잡혔다.

---

## 단계 3. Mixed/Baked 조명, Light Probe ✅

### 목표
정적 오브젝트의 직접·간접광과 그림자를 라이트맵·Shadowmask로 구워 실시간 그림자 비용을 줄이고, 움직이는 오브젝트(플레이어, 좀비)는 Light Probe로 조명한다.

### 사전 확인
- 씬 `MeshRenderer` 403개 중 402개가 이미 Static + Contribute GI였다(`SkinnedMeshRenderer` 1개만 동적). 별도 정적 설정 없이 베이크 대상이 준비되어 있었다.
- 변경 전에는 `LightingSettings` 에셋이 없었고 라이트맵·Light Probe도 없었다.

### 변경 내용
| 대상 | 변경 전 | 변경 후 | 이유 |
|---|---|---|---|
| Lighting Settings | 에셋 없음 | `GameSceneLightingSettings.lighting` 신규: Progressive CPU, Mixed Lighting `Shadowmask`, 해상도 8 texels/unit, 최대 1024, Direct 32 / Indirect 128 / Environment 64 샘플, Bounce 2 | 대형 맵(약 154×147m)에 맞춰 베이크 시간·용량을 작게 |
| Directional Light | Realtime | Mixed | 정적 오브젝트의 그림자·간접광을 베이크하고 동적 오브젝트에는 실시간 그림자 유지 |
| Quality(PC) Shadowmask Mode | Distance Shadowmask | Shadowmask | 정적 오브젝트가 실시간 그림자 맵에 그려지지 않도록 |
| Light Probe Group `LightProbes` | 없음 | 프로브 265개 (NavMesh 위 8m 격자, 높이 +1.2m) | 플레이어·좀비가 라이트맵의 간접광을 받도록 |
| 총구 화염 Point Light ×5 | Realtime | Realtime 유지 | 카메라에 붙어 움직이는 순간 광원이라 베이크 대상이 아님 |

신규/변경 파일: `Assets/01.Scenes/GameScene/` (라이트맵 `Lightmap-0~4_comp_light.exr`, `_comp_shadowmask.png`, `LightingData.asset`, `ReflectionProbe-0.exr`, `GameSceneLightingSettings.lighting`), `ProjectSettings/QualitySettings.asset`, `GameScene.unity`

### 베이크 결과
- 베이크 시간 33.4초, 라이트맵 5장(1024×1024), 총 약 8.4MB, 베이크된 프로브 265개
- `.exr`와 `.png`는 `.gitattributes`에서 Git LFS 대상이다.

### 결과 (Before → After)
**`PerfSampler` 900프레임 평균** (`perf_s2b_trim.json` → `perf_s3_baked.json`)

| 항목 | 단계 2 후 (Before) | 단계 3 후 (After) | 변화 |
|---|---|---|---|
| Shadow Casters | 342 | 2 | -99% |
| Draw Calls | 440 | 294 | -33% |
| Triangles | 287,570 | 244,480 | -15% |
| Vertices | 295,281 | 215,247 | -27% |
| SetPass Calls | 63 | 57 | -10% |
| Batches | 149 | 162 | +9% |
| 평균 프레임 | 2.34 ms | 2.12 ms | -9% |
| 평균 FPS | 426.7 | 470.8 | +10% |
| p95 프레임 | 2.97 ms | 2.67 ms | -10% |
| GC Alloc/프레임 | 34,979 B | 26,361 B | 변화 작음 |
| Total Used Memory | 2155.2 MB | 2194.9 MB | +39.7 MB (+1.8%) |

**Stats·Profiler 화면** (중앙값 프레임): Batches 151, Tris 242.4k, SetPass 56, Shadow Casters 2, Profiler CPU 2.51ms

### 증거
수치: `perf_s2b_trim.json` → `perf_s3_baked.json`

**게임 화면** (Before는 단계 2 후 화면, After는 단계 3 후 화면)

![Before (단계 2 후)](s2b_game.png)

![After (단계 3 후)](s3_game.png)

**Stats + Profiler**

![Before (단계 2 후)](s2b_stats_profiler.png)

![After (단계 3 후)](s3_stats_profiler.png)

### 분석
- **가장 큰 효과는 Shadow Casters 342→2**: Shadowmask 모드에서는 정적 오브젝트의 그림자가 베이크되어 실시간 그림자 맵에는 동적 오브젝트(플레이어)만 그려진다. 그 결과 그림자 패스의 Draw Calls와 Triangles가 함께 줄었다(Draw Calls -146, Tris -43k).
- **프레임 시간 -9%**: 단계 1·2에서는 오차 범위였던 개선이 처음으로 p95까지 함께 낮아졌다(2.97→2.67ms). 다만 이 에디터 환경은 CPU 병목이라 효과는 작게 보인다. GPU가 약한 기기에서 효과가 더 클 것으로 예상하나 이번에는 측정하지 못했다.
- **Batches 증가(+9%)**: 라이트맵을 쓰는 오브젝트는 렌더러마다 라이트맵 인덱스·스케일이 달라 SRP Batcher 외의 배칭이 덜 묶이기 때문으로 추정한다. Draw Calls 총량은 줄었으므로 손해는 작다.
- **메모리 +40MB**: 라이트맵·Shadowmask 텍스처와 `LightingData`가 늘어난 비용이다. 베이크 파일 자체는 8.4MB지만 런타임 메모리에는 더 크게 잡혔다. 메모리를 아껴야 하는 기기에서는 라이트맵 해상도를 낮추는 방안이 있다.
- **화질**: Game 화면에서 눈에 띄는 열화는 보이지 않았다(`s3_game.png`). 정적 그림자는 해상도 8 texels/unit이라 가까이에서는 번질 수 있고, 한 장면만 비교했다는 한계가 있다. 움직이는 좀비·플레이어의 Light Probe 조명 품질은 이번 장면(좀비 0마리)에서 확인하지 못했다.
- **Reflection Probe**: 직접 추가하지 않았다. 베이크 중 씬 기본 반사 프로브(`ReflectionProbe-0.exr`)가 자동 생성되었다.

### 결론
조명 구조를 Mixed + Shadowmask로 바꾸자 Shadow Casters와 Draw Calls가 크게 줄고 프레임 시간도 약 9% 개선되었다. 대신 메모리가 약 40MB 늘고 Git LFS에 라이트맵 파일이 추가된다.

---

## 단계 4. CPU 최적화 (스크립트) ✅

### 목표
코드를 훑어 반복 비용이 큰 부분을 찾아 고친다: 추격 중 매 프레임 경로 재계산, `Instantiate`로 만드는 피격·폭발 이펙트, 매 프레임 `Camera.main` 호출, 상시 실행되는 `Debug.Log`.

### 분석한 후보와 선택
| 후보 | 위치 | 선택 | 이유 |
|---|---|---|---|
| 추격 중 매 프레임 `SetDestination` | `Zombie.Trace()` | ✅ | 호출마다 경로를 다시 계산함 |
| 피격 이펙트 `Instantiate` | `PlayerGunFire.Fire()` | ✅ | 이미 `EffectPool`이 있는데 쓰지 않음(프로젝트 규칙: 새 이펙트는 풀 사용) |
| 폭발 이펙트 `Instantiate` | `Drum.Explode_Coroutine()` | ✅ | 같은 이유 |
| `Camera.main` 매 프레임 호출 | `ZombieHealthBar.LateUpdate()`, `PlayerGunFire.Fire()` | ✅ | 좀비마다 매 프레임 호출 |
| 상시 `Debug.Log` (문자열 보간) | `Zombie`, `Player`, `PlayerGunFire` | ✅ | `GameLogger.Log`는 `ENABLE_LOG`가 없으면 호출 자체가 제거됨 |
| 좀비 피 이펙트 `Instantiate` | `Zombie.TakeDamage()` | ❌ | 이펙트가 좀비의 자식으로 붙는 `GameObject` 프리팹이라 풀 구조(`ParticleSystem` 기반)와 맞지 않아 프리팹 수정이 필요함 |
| `Idle`/`Attack`의 매 프레임 `StopAgent` | `Zombie` | ❌ | 효과가 작고 동작 변경 위험이 있음 |

### 변경 내용
| 대상 | 변경 전 | 변경 후 |
|---|---|---|
| `Zombie.Trace()` | 매 프레임 `SetDestination` | 0.2초(`RepathInterval`)마다 `SetDestination`. `StopAgent()`에서 타이머를 초기화해 추격 재개 시 바로 계산 |
| `Zombie`, `Player`, `PlayerGunFire`의 `Debug.Log` | 항상 실행 | `GameLogger.Log` (조건부 컴파일로 제거) |
| `ZombieHealthBar.LateUpdate()` | `Camera.main.transform` 매번 | `Awake`에서 `_cameraTransform` 캐시 |
| `PlayerGunFire` | `Instantiate(_hitEffectPrefab)`, `Camera.main` 2회 | `EffectPool.Instance.Play(...)`, `_cameraTransform` 캐시 |
| `Drum` | `Instantiate` 폭발 이펙트, 매직 넘버 10 | `EffectPool.Instance.Play(...)`, `ExplosionEffectScale` 상수 |

변경 파일: `Zombie.cs`, `ZombieHealthBar.cs`, `Player.cs`, `PlayerGunFire.cs`, `Drum.cs` (`Assets/02.Scripts/`)

### 측정 방법
- **시나리오**: 좀비 10마리를 같은 시드로 스폰하고 전원이 플레이어를 추격하게 한다(`_detectDistance` 크게). 플레이어는 죽지 않게 체력을 크게 한다.
- **스크립트 비용**: Profiler로 약 600프레임을 기록해 `Zombie.Update()`, `ZombieHealthBar.LateUpdate()` 마커 시간을 합산한다. 좀비 수가 달라서(12마리 vs 11마리) 좀비 1마리당으로 환산했다.
- **사격 테스트**: 0.1초마다 1발씩 8초간 80발을 `Fire()`로 쏜다(같은 장면, 새 Play 세션).
- Before는 `HEAD`의 코드, After는 수정한 코드로 같은 순서로 측정했다.

### 결과 (Before → After)
**스크립트 비용** (Profiler 마커, 좀비 1마리·프레임당)

| 항목 | Before | After | 변화 |
|---|---|---|---|
| `Zombie.Update()` | 0.0028 ms (전체 0.0332 ms/12마리) | 0.00085 ms (전체 0.0094 ms/11마리) | -70% |
| `ZombieHealthBar.LateUpdate()` | 0.00098 ms (전체 0.0118 ms) | 0.00077 ms (전체 0.0085 ms) | -21% |

**사격 테스트** (80발, 0.1초 간격)

| 항목 | Before | After | 변화 |
|---|---|---|---|
| `Fire()` 평균 시간 | 0.410 ms | 0.188 ms | -54% |
| 샷당 관리 메모리 할당 | 716 B | 665 B | 큰 차이 없음 |
| GC 컬렉션(8초) | 1 | 1 | 같음 |
| 최대 프레임 시간 | 68.5 ms | 70.6 ms | 같음 (첫 이펙트 생성 시 튐) |
| 파티클 시스템 개수 (시작→최대→종료) | 5 → 53 → 14 | 5 → 53 → 53 | 풀이 유지 |

**전체 프레임** (`PerfSampler`, 좀비 10마리 추격, 900프레임 평균)

| 항목 | Before | After | 변화 |
|---|---|---|---|
| 평균 프레임 | 3.19 ms | 3.19 ms | 차이 없음 |
| 평균 FPS | 313.5 | 313.6 | 차이 없음 |
| p95 프레임 | 3.89 ms | 3.90 ms | 차이 없음 |
| GC Alloc/프레임 | 38,688 B | 38,065 B | -1.6% |
| Total Used Memory | 2146.1 MB | 2194.6 MB | +48.5 MB (측정 세션 편차 포함) |

### 증거
수치: `perf_s4_before_zombies.json` → `perf_s4_after_zombies.json`. 마커·사격 테스트 값은 Profiler 마커와 `Fire()` 호출 실측 결과이며 위 표에 기록했다.

**Stats + Profiler** (좀비 10마리 추격 장면)

![Before (수정 전 코드)](s4_before_stats_profiler.png)

![After (수정 후 코드)](s4_after_stats_profiler.png)

### 분석
- **`Zombie.Update()` -70%**: 경로 재계산을 매 프레임에서 0.2초마다로 줄인 효과다. 다만 절대값이 좀비 1마리·프레임당 약 0.002ms로 이미 작아서 **전체 프레임 시간(3.19ms)에는 변화가 없었다**. 이 장면에서 CPU 시간은 렌더링·애니메이터가 대부분이고 좀비 스크립트는 1% 미만이다.
- **프레임 시간 개선이 없는 이유**: 스크립트 비용 자체가 작은 데다 이 에디터 환경은 약 300 FPS로 돌고 있어 수십 마이크로초 차이는 드러나지 않는다. 좀비가 수십~수백 마리로 늘어나면 의미가 커질 것으로 예상하나 이번에는 확인하지 않았다.
- **`Fire()` -54%**: `Instantiate` 대신 풀에서 꺼내 재사용해서 빨라졌다. 하지만 샷당 할당량과 GC 횟수는 같았다(둘 다 사격 중 관리 메모리 할당은 `Fire` 안의 다른 코드와 측정에 쓴 리플렉션 호출에서 나오는 것으로 보인다).
- **파티클 개수**: Before는 이펙트가 끝나면 스스로 파괴되어 종료 시 14개로 줄었고, After는 풀이 53개를 계속 보유한다. 대신 첫 생성 이후에는 `Instantiate`/`Destroy`가 없다. 연사 중 메모리는 늘 수 있는 트레이드오프이며 풀 크기 제한(`MaxCount`)은 설정하지 않았다.
- **최대 프레임 시간(≈70ms)**: Before와 After 모두 첫 이펙트·풀 생성 시점에 튄다. 풀에 `Preload`를 설정하면 줄일 수 있으나 이번에는 적용하지 않았다.
- **로그 제거**: `GameLogger.Log`는 `ENABLE_LOG`가 정의되지 않으면 호출이 제거되므로 이번 측정에서는 상태 전이 로그 비용이 사라진다. 로그가 쏟아지는 구간(예: 좀비 여러 마리가 연달아 전이)에서 효과가 있으나 별도로 분리해 측정하지는 않았다.
- **Total Used Memory +48MB**: 서로 다른 Play 세션 간 편차다. 이번 단계 변경이 메모리를 늘렸다고 보기는 어렵다. 풀이 이펙트를 유지하는 영향은 일부 있을 수 있다.
- **검증하지 않은 것**: 드럼 폭발 이펙트를 풀로 바꾼 부분은 폭발을 직접 일으켜 시각적으로 확인하지 못했다(스케일을 `Play` 직후에 적용하는 방식). 직접 드럼통을 쏴서 폭발이 이전과 같은지 확인해 주세요.
- **코드 포맷**: `dotnet format --verify-no-changes`는 내가 수정한 파일뿐 아니라 수정하지 않은 파일(`PlayerMeleeAttack.cs`, `UI_HitEffect.cs` 등)에서도 줄바꿈·공백 오류를 보고한다. 기존 상태이며 이번 변경으로 새로 생긴 것이 아니다. 그대로 커밋하면 pre-commit 훅에서 막힐 수 있다.

### 결론
좀비 AI와 이펙트 생성의 비용을 구조적으로 줄였다(`Zombie.Update()` -70%, `Fire()` -54%). 다만 이 장면에서는 CPU 시간 대부분이 스크립트가 아니라서 전체 프레임 시간 개선은 확인되지 않았고, 좀비 수가 많거나 연사가 잦은 상황에서 이득이 커질 것이다.

---

## 단계 5. RAM 최적화 (에셋) ✅

### 목표
씬이 쓰는 텍스처·메시·오디오 중 메모리 비중이 큰 것을 찾아 화질 영향이 작은 것부터 줄인다.

### 현황 조사 (씬 의존 에셋 기준)
| 종류 | 개수 | 메모리 | 비고 |
|---|---|---|---|
| 텍스처 | 77개 | 113.7 MB | 상위 6개(2048×2048)가 약 43MB. 노멀 맵 2개가 각 10.7MB |
| 메시 | 162개 | 14.9 MB | Read/Write가 켜진 모델 27개(3.2MB) |
| 오디오 | 2개 | 1.3 MB | 비중 작음 |

Profiler의 Texture Memory(328MB)에는 씬 의존 에셋 외에 에디터·UI·기본 텍스처가 포함된다.

### 선택한 후보
| 후보 | 선택 | 이유 |
|---|---|---|
| 2048 노멀 맵 2개(Sword, Bacon) | ✅ 1024 | 각 10.7→2.7MB. 노멀 맵은 해상도를 낮춰도 눈에 덜 띔 |
| Sword 알베도 2048 | ✅ 1024 | 5.4→1.4MB |
| 2048 환경 알베도 3개(Hangar_v3, Hangar_v4, Cargo_container_v1) | ✅ 1024 | 각 5.4→1.4MB |
| 모델 Read/Write 해제 | ❌ | 3.2MB로 작고, 정적 배칭에 필요할 수 있어 위험 |
| 오디오 설정 | ❌ | 1.3MB뿐이라 효과 없음 |
| 라이트맵 Shadowmask 텍스처(1024² ×5) | ❌ | 압축이 되지 않는 형식이라 임포터로 줄일 수 없음 |

### 변경 내용
`TextureImporter.maxTextureSize` 2048 → 1024 (6개 파일, `.meta`만 변경)
- `Assets/05.Models/Sword/..._texture.png`, `..._texture_normal.png`
- `Assets/05.Models/Bacon/normal.png`
- `Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v3/…`, `Hangar_v4/…`
- `Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/…`

### 결과 (Before → After)
**Play 모드 120프레임 후 `ProfilerRecorder`** (좀비 0마리, 증거: `ram_s5_before_after.json`)

| 항목 | Before | After | 변화 |
|---|---|---|---|
| Texture Memory | 328.0 MB | 297.1 MB | -30.9 MB (-9.4%) |
| Total Used Memory | 2275.1 MB | 2238.7 MB | -36.4 MB (-1.6%) |
| Total Reserved Memory | 3266.7 MB | 3249.9 MB | -16.8 MB |
| Mesh Memory | 27.9 MB | 27.9 MB | 변화 없음 |
| GC Used Memory | 789.2 MB | 786.7 MB | 변화 없음 |
| Material Count | 218 | 216 | (변경 없음, 오차) |
| 씬 의존 텍스처 합계(에디터 조회) | 113.7 MB | 81.7 MB | -32.0 MB (-28%) |

**Profiler Memory 모듈 화면** (같은 장면, 중앙값 프레임 선택. 그래프에 표시되는 값이라 위 `ProfilerRecorder` 값과 절대값이 다르지만 감소량은 같다)

| 항목 | Before (2048) | After (1024) | 변화 |
|---|---|---|---|
| Texture Memory | 287.3 MB | 255.3 MB | -32.0 MB (-11%) |
| Mesh Memory | 24.6 MB | 24.6 MB | 변화 없음 |
| Profiler CPU (선택 프레임) | 2.61 ms | 2.43 ms | 오차 범위 |

**화질**: `s5_before_hangar.png` ↔ `s5_after_hangar.png` (Hangar 벽을 약 14m 거리에서 촬영), `s5_before_container.png` ↔ `s5_after_container.png` (컨테이너를 약 7m 거리에서 촬영). 육안으로 눈에 띄는 차이는 없었다.

### 증거
수치: `ram_s5_before_after.json`

**Profiler** (Game 뷰 + CPU/Rendering/Memory 모듈)

![Before (2048)](s5_before_profiler.png)

![After (1024)](s5_after_profiler.png)

**화질 비교: Hangar 벽 (약 14m)**

![Before (2048)](s5_before_hangar.png)

![After (1024)](s5_after_hangar.png)

**화질 비교: 컨테이너 (약 7m)**

![Before (2048)](s5_before_container.png)

![After (1024)](s5_after_container.png)

### 분석
- **씬 텍스처 -28%, 런타임 Texture Memory -9.4%**: 변경 대상이 전체 텍스처 메모리의 일부(씬 의존 113.7MB 중 약 32MB)이고, 나머지는 에디터·UI 텍스처 등이라 런타임 합계에서는 감소율이 작아 보인다.
- **Total Used Memory -36MB**: 텍스처 감소분과 거의 같다. 에디터 세션 편차가 있어 ±수십 MB 오차가 있을 수 있다.
- **화질**: 가까이서 본 건물 벽에서는 차이가 보이지 않았다. 다만 한 장소·한 거리에서만 비교했고, 근접 확대나 다른 각도는 확인하지 않았다. 무기(Sword)는 1인칭에서 가까이 보이므로 근접 모드에서 질감이 거칠어지지 않았는지 직접 확인이 필요하다.
- **Mesh Memory 변화 없음**: 이번에는 메시를 건드리지 않았다. 후보 중 가장 큰 `Roblox Bacon Hair.fbx`(5.5MB), `X Bot@Zombie Reaction Hit.fbx`(4.6MB)는 용도·참조 확인이 필요해서 보류했다.
- **컨테이너 비교**: `Cargo_container_v1`을 약 7m 거리에서 정면으로 찍어 비교했다(`s5_before_container.png` ↔ `s5_after_container.png`). 화면 차이는 거의 없고, 2048 쪽의 "CARGO" 글자 가장자리가 아주 조금 더 또렷한 정도였다.
- **처음 컨테이너 촬영이 검게 나온 이유**: 첫 시도에서 카메라 위치가 컨테이너 적층 내부(좌표가 컨테이너와 겹침)였기 때문이다. 라이트맵(인덱스 1, UV2 있음)에는 문제가 없었고, 단계 3의 Shadowmask 때문에 어두워진 것이 아니었다.
- **실제 빌드 효과**: 에디터에서는 압축 전 원본이 함께 메모리에 있다. 빌드에서는 텍스처가 압축된 상태로 올라가므로 절감 폭이 다를 수 있다.

### 결론
텍스처 6개의 최대 해상도를 줄여 씬 텍스처를 약 32MB(-28%) 줄였고 눈에 띄는 화질 저하는 보이지 않았다. 런타임 전체로는 약 36MB(-1.6%) 감소였다.

---

## 누적 결과 요약

단계 1은 프로토콜 변경 전 측정이라 렌더 카운트만 이어서 비교하고, 프레임·메모리는 단계 1 후(새 프로토콜 재측정값)부터 이어간다.

| 항목 | 기준 | 단계 1 | 단계 1 후 (재측정) | 단계 2 | 단계 3 | 단계 4 | 단계 5 |
|---|---|---|---|---|---|---|---|
| Draw Calls | 574 | 426 | 430 | 440 | 294 | | |
| Batches | 162 | 148 | 148 | 149 | 162 | | |
| SetPass Calls | 70 | 63 | 63 | 63 | 57 | | |
| Triangles | 415,406 | 286,997 | 287,129 | 287,570 | 244,480 | | |
| Shadow Casters | 721 | 335 | 337 | 342 | 2 | | |
| 평균 프레임(ms) | (4.71)* | (4.78)* | 2.44 | 2.34 | 2.12 | | |
| p95 프레임(ms) | (5.54)* | (6.15)* | 3.10 | 2.97 | 2.67 | | |
| Total Used Memory(MB) | (1794.8)* | (1771.4)* | 2238.9 | 2155.2 | 2194.9 | | |

\* 이전 프로토콜 값이라 오른쪽 열과 비교할 수 없다. 기준 상태를 새 프로토콜로 다시 재면 이 칸을 채울 수 있다.

단계 4는 좀비 10마리가 추격하는 별도 장면에서 측정했기 때문에 위 표(좀비 0마리)에 같은 칸으로 이어 쓰지 않았다. 단계 4 결과는 해당 단계 표를 본다.

---

## 과정에서 생긴 문제와 해결
- 씬을 저장할 때 에디터 메모리에 남아 있던 이전 컬링 최적화(되돌린 커밋)가 파일에 다시 섞여 들어갔다. `git diff`로 발견해 씬을 `HEAD`로 복원하고 조명 값만 다시 적용했다.
- 에디터가 백그라운드이면 Play 프레임이 거의 진행되지 않아 측정이 무효가 되었다. 에디터를 앞에 둔 상태에서만 측정했다.
- 좀비 스포너가 켜져 있으면 장면마다 Batches가 달라져(예: 143 vs 223) 비교가 불가능했다. 스포너를 끄고 좀비를 제거해 조건을 통일했다.
- 이전에 측정해 둔 `PostFX_Report/perf_before.json`은 다른 장면에서 측정한 값이라 이번 비교에는 쓰지 않았다.
- 단계 2 측정 중 Profiler 기록이 쌓여 Total Used Memory가 5.5GB로 부풀고(정상 약 2.2GB) 프레임 시간도 달라진 것을 발견했다. 기록을 비우고 Profiler를 끈 채 `PerfSampler`를 돌리는 프로토콜로 바꿔 다시 측정했다. 잘못 측정한 파일(`invalid_step2a_profiler_on.json`)은 참고용으로 남겼다.

## 느낀 점 / AI 활용 `[직접 작성]`
- 직접 설정한 값과 AI가 제안한 값의 차이, AI 도움이 효과적이었던 부분과 한계를 적는다.

## 첨부 파일 (`Docs/Lighting_Report/`)
- `perf_*.json`, `ram_s5_before_after.json`: 단계별 측정 결과 (`invalid_step2a_profiler_on.json`은 Profiler가 켜진 채 잘못 측정한 참고용 파일)
- `*_game.png`, `*_hangar.png`, `*_container.png`: 게임 화면 Before/After
- `*_stats_profiler.png`, `*_profiler.png`: Stats와 Profiler 화면
- 측정 코드: `Assets/02.Scripts/Test/PerfSampler.cs`
