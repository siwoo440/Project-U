# Project U 개발 일지

## 76일차 : 몬스터 체력바·피해 숫자·처치 연출 및 전투 효과음 구현

### 1. 개발 목표

76일차에는 74일차에 제작한 Melee Grunt와 Ranged Spitter, 75일차에 연결한 스폰·전리품 흐름 위에 전투 피드백을 추가하였다.

기존 전투 시스템은 피해·사망 결과를 Console 로그로만 확인할 수 있었기 때문에, 플레이어가 화면과 소리만으로 전투 상황을 이해할 수 있도록 하는 것이 이번 일차의 목표이다.

기획서 76일차 완료 기준:

```text
대표 몬스터의 체력·피해량·명중·공격 예고·처치 효과와
간단한 음향이 전투 상황에 맞춰 표시된다.
```

이번 일차의 핵심 목표는 다음과 같다.

- 몬스터 머리 위 체력바 표시
- 실제 적용 피해량 숫자 표시
- 명중 시 피격 번쩍임과 흔들림
- 처치 시 파편 효과와 사라짐 연출
- 근접·원거리 공통 공격 예고 표시
- 근접 몬스터 발밑 공격 범위 표시
- 피격·처치·공격 예고 효과음
- 기존 `EnemyHealth`, `EnemyCombatController` 수정 없이 이벤트로 연결

---

## 2. 설계 방향

기존 적 시스템에는 이미 전투 피드백에 필요한 이벤트가 준비되어 있었다.

```text
EnemyHealth
├─ Damaged(CombatHitData, float)   → 공격 정보와 실제 피해량
├─ Died(CombatHitData)             → 사망 원인 공격 정보
└─ Revived()                       → 부활 완료

EnemyCombatController
├─ CurrentState                    → Idle / Chasing / Attacking / Hit / Dead
├─ CurrentAttackPhase              → Ready / Windup / Recovery / Cooldown
└─ AttackPhaseNormalized           → 공격 준비 진행 비율

EnemyRangedAttackController
├─ CurrentPhase                    → 원거리 공격 단계
└─ PhaseNormalized                 → 원거리 공격 준비 진행 비율
```

따라서 기존 전투 코드는 수정하지 않고, 이 이벤트와 상태를 구독하는 피드백 전용 컴포넌트를 추가하였다.

또한 아직 전용 이펙트·UI·오디오 에셋이 없기 때문에 모든 표시 요소는 **Prefab 없이 코드로 생성**하고, 이후 실제 에셋이 준비되면 Inspector에서 교체할 수 있도록 선택 슬롯을 두었다.

---

## 3. 신규 스크립트 구성

```text
Assets/_ProjectU/Scripts/Enemy/Day76/
├─ EnemyCombatFeedback.cs      → 적 전투 피드백 통합 관리
├─ EnemyWorldHealthBar.cs      → 머리 위 World Space 체력바
├─ CombatDamagePopup.cs        → 떠오르는 피해 숫자
├─ CombatFeedbackDebris.cs     → 피격 불꽃·처치 파편
└─ CombatFeedbackAudio.cs      → 전투 효과음 재생·기본 효과음 생성
```

적 Prefab에는 `EnemyCombatFeedback` 하나만 추가하고, 나머지는 실행 중에 자동 생성된다.

---

## 4. EnemyCombatFeedback

적 Root에 추가하는 통합 피드백 컴포넌트이다.

```text
[RequireComponent(typeof(EnemyHealth))]
```

참조는 비워두면 같은 오브젝트에서 자동으로 찾는다.

```text
EnemyHealth
EnemyCombatController
EnemyRangedAttackController (원거리 적만)
ContentVisualRoot
```

`ContentVisualRoot`의 표준 기준점을 사용하여 위치를 결정한다.

| 기준점 | 사용처 |
| --- | --- |
| `UIAnchor` | 체력바, 공격 예고 `!` 위치 |
| `EffectOrigin` | 처치 파편 위치, 잘못된 피격 지점 보정 |
| `Visual` | 피격 번쩍임, 흔들림, 처치 축소 대상 |

기준점이 없는 적도 동작하도록 Root 기준 높이를 대체값으로 사용한다.

---

## 5. 체력바

`EnemyWorldHealthBar`는 World Space Canvas로 체력바를 생성한다.

```text
WorldHealthBar (Canvas / World Space)
├─ Background
├─ Inner
│  ├─ Trail   → 줄어든 체력을 천천히 따라가는 잔상
│  └─ Fill    → 현재 체력
└─ Name       → EnemyCombatData Display Name
```

동작 규칙:

- 피격 시 표시 후 4초 유지
- 추적·공격·피격 상태에서는 계속 표시
- 공격 준비 시작 시 표시
- 카메라와 30m 이상 떨어지면 숨김
- 항상 카메라를 향하도록 회전
- 체력 비율에 따라 초록 → 빨강 색상 변화
- 피해 후 0.35초 뒤 잔상 감소
- 사망 시 0으로 줄어든 뒤 0.6초 후 숨김
- 부활 시 즉시 최대 체력으로 초기화

---

## 6. 피해 숫자

`CombatDamagePopup`은 `Damaged` 이벤트의 **실제 적용 피해량**을 표시한다.

방어력이 적용된 뒤의 값을 사용하므로 `EnemyCombatData`의 Defense Percent가 화면 숫자에 그대로 반영된다.

```text
일반 피해   → 연한 흰색
처치 타격   → 빨간색, 1.4배 크기
```

연출:

```text
피격 지점 생성
→ 1.5배 크기로 튀어나옴
→ 위로 떠오르며 약간 옆으로 흩어짐
→ 후반 40% 동안 투명해짐
→ 0.8초 후 풀로 반환
```

피격 지점이 비어 있거나 적과 5m 이상 떨어진 값이면 `EffectOrigin` 위치로 보정한다.

생성 비용을 줄이기 위해 최대 24개까지 재사용 풀을 사용하고, Scene 전환으로 파괴된 항목은 건너뛰도록 처리하였다.

---

## 7. 명중 피드백

### 피격 번쩍임

`Visual` 아래 Renderer의 색상을 `MaterialPropertyBlock`으로 흰색으로 바꾼 뒤 0.12초 동안 원래 색으로 되돌린다.

`ContentVisualRoot`가 임시 Primitive 색상을 PropertyBlock으로 적용하고 있기 때문에, 기존 PropertyBlock 색상이 있으면 그 값을 저장했다가 복구하도록 처리하였다.

제외 대상:

```text
LineRenderer
TrailRenderer
ParticleSystemRenderer
TextMeshPro
비활성 Renderer
```

### 피격 흔들림

`Visual` Transform을 0.15초 동안 최대 12% 커졌다가 돌아오게 하였다.

기능 Root가 아닌 Visual만 변경하므로 Collider, NavMesh, 상호작용에는 영향을 주지 않는다.

### 피격 불꽃

공격 반대 방향으로 작은 노란 파편 4개를 튀긴다.

`Hit Effect Prefab`을 연결하면 기본 파편 대신 해당 Prefab을 사용한다.

---

## 8. 처치 연출

`Died` 이벤트에서 다음을 실행한다.

```text
처치 효과음
→ 공격 예고 숨김
→ 체력바 0 표시
→ 붉은 파편 12개 폭발
→ 0.2초 대기
→ 0.6초 동안 Visual 축소 및 0.4m 가라앉음
```

75일차 `EnemySpawnPoint`의 시체 유지 시간 2초 안에 연출이 끝나도록 구성하였다.

`Revived` 이벤트가 오면 Visual의 크기와 위치를 원래 값으로 복구한다.

`Death Effect Prefab`을 연결하면 기본 파편 대신 해당 Prefab을 사용한다.

### 기본 파편

`CombatFeedbackDebris`는 Rigidbody·Collider 없이 직접 이동하는 작은 Cube이다.

- 기본 Cube에서 Mesh와 Render Pipeline 기본 Material만 가져와 재사용
- 중력 적용
- 무작위 회전
- 수명 동안 점점 작아짐
- 플레이어·적·투사체와 충돌하지 않음

---

## 9. 공격 예고

### 공통 `!` 표시

근접·원거리 적 모두 공격 준비(Windup) 단계에서 머리 위에 `!`를 표시한다.

```text
준비 시작 → 노란색
판정 직전 → 빨간색
```

준비 진행률에 따라 크기가 커지고 빠르게 맥동한다.

원거리 적은 74일차에 `EnemyCombatController`가 아닌 `EnemyRangedAttackController`의 공격 단계를 사용하도록 분리되어 기존 Telegraph가 동작하지 않았으므로, 두 컨트롤러의 공격 단계를 모두 확인하도록 구성하였다.

### 근접 발밑 범위 원

65일차에 제작한 `EnemyAttackTelegraph`는 현재 어떤 Prefab·Scene에도 연결되어 있지 않았다.

`Ground Telegraph Material`이 연결된 근접 적은 실행 시 다음 오브젝트를 자동 생성한다.

```text
AttackTelegraph_Runtime
├─ LineRenderer
└─ EnemyAttackTelegraph
```

CapsuleCollider 바닥 높이를 계산하여 원을 발밑에 배치한다.

URP Unlit Material은 LineRenderer 정점 색상을 사용하지 않으므로, Telegraph가 계산한 색상을 Material 인스턴스의 `_BaseColor`로 매 프레임 복사하였다.

원거리 적은 Attack Range가 7.5m로 커서 발밑 원이 오히려 혼란스럽기 때문에 원을 생성하지 않는다.

---

## 10. 전투 효과음

프로젝트에 전투용 오디오 파일이 아직 없어서 `CombatFeedbackAudio`에서 기본 효과음을 코드로 생성하였다.

| 효과음 | 길이 | 구성 |
| --- | --- | --- |
| 피격 | 0.14초 | 짧은 노이즈 + 낮아지는 저음 타격 |
| 처치 | 0.5초 | 420Hz → 60Hz로 떨어지는 음 + 노이즈 |
| 공격 예고 | 0.2초 | 두 단계로 올라가는 경고음 |

재생 규칙:

- 피격음은 처치 타격이 아닐 때만 재생
- 공격 예고음은 Windup 시작 순간 1회 재생
- 매 재생마다 피치를 ±6% 무작위 변경하여 반복 피로 감소
- 3D 70% 공간감, 3m\~40m 선형 감쇠
- `AudioListener.volume` 기반 마스터 볼륨 설정 자동 반영

`Hit Clip`, `Kill Clip`, `Warning Clip`에 오디오 파일을 연결하면 기본음 대신 사용한다.

---

## 11. Prefab 적용

### Enemy_MeleeGrunt

```text
Enemy_MeleeGrunt
└─ EnemyCombatFeedback
   └─ Ground Telegraph Material: M_BowTrajectory
```

### Enemy_RangedSpitter

```text
Enemy_RangedSpitter
└─ EnemyCombatFeedback
   └─ Ground Telegraph Material: None
```

75일차 `EnemySpawnPoint`가 두 Prefab을 생성하므로 Scene 수정 없이 스폰되는 모든 몬스터에 적용된다.

---

## 12. 주요 Inspector 설정

```text
Health Bar
├─ Show Health Bar: On
├─ Fallback Bar Height: 2.2
├─ Health Bar Offset: (0, 0.35, 0)
├─ Health Bar Visible Duration: 4
├─ Show Health Bar In Combat: On
└─ Health Bar Max Distance: 30

Damage Number
├─ Show Damage Numbers: On
└─ Damage Number Size: 4

Hit Flash
├─ Hit Flash Duration: 0.12
├─ Hit Punch Scale: 0.12
├─ Hit Punch Duration: 0.15
└─ Hit Spark Count: 4

Death
├─ Death Debris Count: 12
├─ Death Shrink Delay: 0.2
├─ Death Shrink Duration: 0.6
└─ Death Sink Distance: 0.4

Audio
├─ Play Sounds: On
└─ Sound Volume: 0.7
```

---

## 13. 전체 피드백 흐름

```text
플레이어 공격 적중
↓
EnemyHealth.ReceiveDamage
↓
Damaged 이벤트
↓
EnemyCombatFeedback
├─ 체력바 갱신 및 표시
├─ 피해 숫자 생성
├─ 피격 번쩍임
├─ Visual 흔들림
├─ 피격 불꽃
└─ 피격음
↓
체력 0
↓
Died 이벤트
↓
EnemyCombatFeedback
├─ 처치 숫자 (빨간색)
├─ 처치음
├─ 처치 파편
└─ Visual 축소
↓
EnemyLootDropper 전리품 생성 (75일차)
↓
EnemySpawnPoint 재생성 대기 (75일차)
```

---

## 14. 검증

### 컴파일

프로젝트 버전 `6000.3.9f1` Batch Mode로 프로젝트를 열어 확인하였다.

- 스크립트 컴파일 오류 없음
- `Enemy_MeleeGrunt.prefab` Import 정상
- `Enemy_RangedSpitter.prefab` Import 정상
- `ProjectVersion.txt` 변경 없음

### 기존 코드 영향

다음 기존 스크립트는 수정하지 않았다.

```text
EnemyHealth
EnemyCombatController
EnemyAttackTelegraph
EnemyRangedAttackController
EnemySpawnPoint
EnemyLootDropper
ContentVisualRoot
```

---

## 15. 다음 개발 방향

77일차에는 대표 몬스터 전투 결과를 저장·사망·부활 흐름과 연결한다.

주요 작업 예정:

1. 몬스터 전투로 획득한 보상의 저장 확인
2. 플레이어 체력·상태 저장 확인
3. 플레이어 사망 처리 흐름 점검
4. 침낭 부활 연결
5. 이어하기 후 상태 복원
6. 스폰 포인트 재생성 상태 저장 여부 결정
7. 75일차 완료 기준의 시간 조건 스폰 처리 여부 결정

---

## 76일차 완료 상태

- [x] `EnemyCombatFeedback.cs` 추가
- [x] `EnemyWorldHealthBar.cs` 추가
- [x] `CombatDamagePopup.cs` 추가
- [x] `CombatFeedbackDebris.cs` 추가
- [x] `CombatFeedbackAudio.cs` 추가
- [x] 머리 위 체력바 구현
- [x] 실제 피해량 숫자 표시 구현
- [x] 처치 타격 숫자 강조 구현
- [x] 피격 번쩍임 및 기존 PropertyBlock 색상 복구
- [x] 피격 흔들림 구현
- [x] 피격 불꽃·처치 파편 구현
- [x] 처치 시 Visual 축소 연출 구현
- [x] 근접·원거리 공통 공격 예고 `!` 구현
- [x] 근접 적 발밑 공격 범위 원 자동 생성
- [x] 피격·처치·공격 예고 기본 효과음 구현
- [x] `Enemy_MeleeGrunt` Prefab 적용
- [x] `Enemy_RangedSpitter` Prefab 적용
- [x] 스크립트 컴파일 오류 확인
- [ ] Play Mode 전투 연출 확인
- [ ] 75일차 전리품·재생성 회귀 테스트

---

## Git Commit

```text
76일차 : 몬스터 체력바·피해 숫자·처치 연출 및 전투 효과음 구현
```
