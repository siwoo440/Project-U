# Project U 개발 일지

---

## 64일차 : 적 공통 전투와 NavMesh 추적 이동 구현

- 개발일: 2026-08-02
- 개발 단계: 적 전투 기반 및 추적 이동
- 개발 상태: 완료

---

## 개발 목표

플레이어의 근접 공격, 활 공격, 회피와 피격 무적 시스템에 연결할 수 있는 적 공통 전투 구조를 구현한다.

모든 적이 공유할 체력·방어력·탐지·공격 능력치를 데이터로 분리하고, 플레이어를 감지하면 NavMesh 경로를 따라 추적한 뒤 공격 거리에서 전투 피해를 전달하도록 구성한다.

월드 저장 시스템 초기화를 막고 있던 중복 `World Object ID`도 함께 정리한다.

---

## 주요 구현 내용

### 1. 적 공통 전투 데이터

`EnemyCombatData` ScriptableObject를 생성하여 적 종류별 능력치를 하나의 데이터 Asset으로 관리한다.

관리 항목:

- 적 고유 ID
- 적 표시 이름
- 최대 체력
- 방어력 비율
- 이동 속도
- 회전 속도
- 플레이어 탐지 거리
- 추적 해제 거리
- 공격 거리
- 기본 공격 피해량
- 공격 충격량
- 공격 재사용 대기시간
- 피격 반응 시간
- 사망 후 Collider 처리
- 사망 후 오브젝트 제거 여부
- 사망 오브젝트 제거 대기시간

기본 적 데이터:

| 항목 | 값 |
|---|---:|
| Enemy ID | enemy_basic |
| Display Name | Basic Enemy |
| Maximum Health | 50 |
| Defense Percent | 0 |
| Move Speed | 3 |
| Rotation Speed | 360 |
| Detection Range | 10 |
| Lose Target Range | 14 |
| Attack Range | 1.8 |
| Attack Damage | 10 |
| Attack Impact Force | 2 |
| Attack Cooldown | 1.25 |
| Hit Reaction Duration | 0.2 |

---

### 2. 적 공통 전투 상태

`EnemyCombatState`를 생성하여 적의 현재 행동 상태를 구분한다.

```text
Idle
플레이어를 인식하지 않은 대기 상태

Chasing
플레이어를 인식했지만 공격 거리 밖인 추적 상태

Attacking
플레이어가 공격 거리 안에 있는 공격 상태

Hit
플레이어 공격에 맞아 잠시 행동이 중단된 피격 상태

Dead
체력이 모두 소진된 사망 상태
```

기본 상태 흐름:

```text
Idle
→ 플레이어 탐지
→ Chasing
→ 공격 거리 진입
→ Attacking
→ 피해 수신
→ Hit
→ Chasing 또는 Attacking 복귀
→ 체력 0
→ Dead
```

---

### 3. 적 체력과 피해 수신

`EnemyHealth`가 `ICombatDamageReceiver`를 구현하도록 구성했다.

따라서 기존 플레이어 전투 시스템을 별도로 수정하지 않고 다음 공격을 받을 수 있다.

- 근접 기본 공격
- 근접 연속 공격
- 활 장전 공격
- 장력에 따른 화살 피해
- 이후 추가할 다른 전투 공격

피해 처리 흐름:

```text
플레이어 공격
→ CombatHitData 전달
→ 동일 공격 번호 중복 검사
→ 방어력 적용
→ 적 체력 감소
→ 피격 이벤트
→ 체력 0이면 사망 이벤트
```

방어력 계산:

```text
실제 피해 = 전달 피해 × (1 - 방어력 / 100)
```

동일 공격자가 같은 `Attack Sequence ID`로 여러 Collider를 맞혀도 체력은 한 번만 감소한다.

---

### 4. 적 피격과 사망 처리

적이 피해를 받으면 `Hit` 상태로 변경된다.

`Hit Reaction Duration`이 끝나면 플레이어와의 거리에 따라 다음 상태로 복귀한다.

```text
공격 거리 밖
→ Chasing

공격 거리 안
→ Attacking
```

적 체력이 0이 되면 다음 처리를 실행한다.

- 현재 체력을 0으로 고정
- `Dead` 상태 적용
- 플레이어 추적 중단
- 적 공격 중단
- 전체 Collider 비활성화
- 추가 피해 차단
- 설정에 따라 일정 시간 뒤 오브젝트 제거

Inspector의 `Revive Enemy` 메뉴를 통해 테스트용 부활도 가능하도록 구성했다.

---

### 5. 플레이어 탐지와 공격

`EnemyCombatController`에서 플레이어의 `PlayerCombatDamageReceiver`를 자동 검색한다.

탐지 구조:

```text
플레이어가 Detection Range 안으로 진입
→ 추적 시작

플레이어가 Detection Range 밖으로 이동
→ 이미 추적 중이라면 계속 유지

플레이어가 Lose Target Range 밖으로 이동
→ 추적 해제
→ Idle 복귀
```

플레이어가 공격 거리 안으로 들어오면 적은 `Attacking` 상태가 되고, 재사용 대기시간마다 `CombatHitData`를 생성하여 플레이어에게 전달한다.

적 공격에는 다음 플레이어 전투 규칙이 그대로 적용된다.

- 플레이어 장비 방어력
- 회피 무적
- 피격 후 무적
- 동일 공격 단계 중복 차단
- 플레이어 사망 처리

---

### 6. NavMesh 추적 이동

초기 적 전투 구조는 `Chasing` 상태만 판정하고 실제 위치를 이동시키지 않았다.

이를 해결하기 위해 `EnemyNavMeshMovement`를 추가했다.

주요 기능:

- `NavMeshAgent`를 이용한 플레이어 추적
- `EnemyCombatData.MoveSpeed` 자동 적용
- `EnemyCombatData.RotationSpeed` 자동 적용
- 공격 거리 기준 자동 정지
- 일정 간격으로 플레이어 목적지 갱신
- Chasing 상태에서만 이동
- Attacking, Hit, Dead 상태에서 이동 정지
- NavMesh 밖에 있을 때 가까운 NavMesh 위치 검색
- Inspector 메뉴를 통한 NavMesh 재배치
- 현재 이동 상태와 남은 거리 확인

이동 흐름:

```text
Idle
→ 이동하지 않음

Chasing
→ 플레이어 위치를 NavMesh 목적지로 설정
→ 플레이어 추적 이동

Attacking
→ 이동 정지
→ 플레이어 방향 회전과 공격

Hit
→ 이동 정지

Dead
→ 이동과 경로 처리 종료
```

---

### 7. NavMesh 환경 구성

`20_Gameplay` Scene에 Navigation 관리 오브젝트를 구성했다.

```text
=== Navigation ===
└─ Nav Mesh Surface
```

NavMeshSurface 설정:

| 항목 | 설정 |
|---|---|
| Agent Type | Humanoid |
| Collect Objects | All |
| Use Geometry | Physics Colliders |
| Default Area | Walkable |

Terrain과 바닥 Collider를 기준으로 NavMesh를 Bake하고, 적에게 다음 컴포넌트를 추가했다.

```text
NavMeshAgent
EnemyNavMeshMovement
```

적의 발밑과 플레이어 이동 구역에 파란색 NavMesh가 생성되도록 구성했다.

---

### 8. 적 테스트 오브젝트 구성

테스트 적:

```text
Enemy_Basic_Test
└─ AttackOrigin
```

적 루트 컴포넌트:

```text
Transform
Mesh Filter
Mesh Renderer
Capsule Collider
NavMeshAgent
EnemyHealth
EnemyCombatController
EnemyNavMeshMovement
```

`AttackOrigin`은 적 공격이 시작되는 위치로 사용한다.

---

### 9. 월드 저장 시스템 중복 ID 오류 해결

발생 오류:

```text
월드 저장 시스템 초기화 실패
중복 World Object ID가 있습니다:
00df8735ea7a4383809201c66506be38
```

오류 원인은 다음 세 Scene 오브젝트가 동일한 ID를 사용하고 있었기 때문이다.

```text
BowPickup
ArrowPickup
ApplePickup
```

기존 중복 ID:

```text
00df8735ea7a4383809201c66506be38
```

수정 결과:

```text
BowPickup
→ 기존 ID 유지

ArrowPickup
→ 새로운 고유 ID 발급

ApplePickup
→ 새로운 고유 ID 발급
```

세 오브젝트가 서로 다른 ID를 사용하도록 수정한 뒤 `20_Gameplay` Scene을 저장했다.

---

### 10. WorldObjectIdentity 보강

기존 `WorldObjectIdentity`는 Runtime ID 생성 함수만 제공하고 있어 Scene 오브젝트의 ID를 Inspector에서 편리하게 변경하기 어려웠다.

다음 Context Menu를 추가했다.

```text
Generate New World Object ID
Clear World Object ID
```

새 ID 발급 기능:

- Edit Mode에서만 영구 ID 변경 허용
- GUID 형식의 32자리 ID 생성
- Undo 기록 지원
- Scene 변경 상태 자동 기록
- Prefab 인스턴스 Override 자동 기록
- Project 창의 원본 Prefab에는 고정 ID 발급 차단
- Play Mode 변경 방지

World Object ID는 Project 창의 원본 Prefab이 아니라 Hierarchy의 Scene 인스턴스마다 서로 다르게 유지한다.

---

## 생성한 스크립트

```text
Assets/_ProjectU/Scripts/Enemy/EnemyCombatState.cs
Assets/_ProjectU/Scripts/Enemy/EnemyCombatData.cs
Assets/_ProjectU/Scripts/Enemy/EnemyHealth.cs
Assets/_ProjectU/Scripts/Enemy/EnemyCombatController.cs
Assets/_ProjectU/Scripts/Enemy/EnemyNavMeshMovement.cs
```

---

## 수정한 스크립트

```text
Assets/_ProjectU/Scripts/World/WorldObjectIdentity.cs
```

---

## 생성한 데이터

```text
Assets/_ProjectU/Data/Enemies/EnemyCombatData_Basic.asset
```

---

## 수정한 Scene

```text
Assets/_ProjectU/Scenes/20_Gameplay.unity
```

Scene 변경 내용:

- `Enemy_Basic_Test` 추가
- `AttackOrigin` 추가
- 적 체력과 전투 상태 연결
- NavMeshAgent 추가
- EnemyNavMeshMovement 추가
- NavMeshSurface 구성 및 Bake
- ArrowPickup World Object ID 변경
- ApplePickup World Object ID 변경

---

## 테스트 결과

### 적 데이터

- `EnemyCombatData` Asset을 생성할 수 있다.
- 적 능력치를 Inspector에서 설정할 수 있다.
- 체력, 방어력, 탐지 거리와 공격력이 데이터에서 적용된다.

### 적 체력과 피해

- 플레이어 근접 공격으로 적 체력이 감소한다.
- 활과 화살 공격으로 적 체력이 감소한다.
- 장력에 따른 최종 피해량이 적용된다.
- 방어력 비율에 따라 실제 피해가 감소한다.
- 동일 공격 단계의 중복 피해가 차단된다.
- 피격 시 `Hit` 상태로 변경된다.
- 체력이 0이면 `Dead` 상태로 변경된다.
- 사망 후 Collider가 비활성화된다.
- 사망한 적에게 추가 피해가 적용되지 않는다.
- Inspector 메뉴를 통해 적을 부활시킬 수 있다.

### 적 탐지와 이동

- 탐지 거리 밖에서는 `Idle` 상태를 유지한다.
- 탐지 거리 안에서는 `Chasing` 상태로 변경된다.
- Chasing 상태에서 플레이어를 향해 이동한다.
- NavMesh 경로를 따라 장애물을 우회한다.
- 플레이어가 움직이면 목적지가 다시 계산된다.
- 공격 거리 안에서는 이동을 멈춘다.
- 플레이어가 추적 해제 거리 밖으로 이동하면 `Idle`로 복귀한다.
- 적이 NavMesh 밖에 있으면 가까운 NavMesh 위치로 재배치할 수 있다.

### 적 공격과 플레이어 방어

- 공격 거리 안에서 적이 플레이어를 공격한다.
- 공격 재사용 대기시간이 적용된다.
- 적 공격이 플레이어 체력을 감소시킨다.
- 플레이어 회피 무적으로 적 공격을 차단할 수 있다.
- 피격 후 무적으로 연속 공격이 차단된다.
- 플레이어 사망 후 적 공격이 중단된다.

### 월드 저장

- BowPickup, ArrowPickup, ApplePickup의 ID가 서로 다르다.
- 월드 저장 시스템 중복 ID 초기화 오류가 사라진다.
- GameplaySaveController가 정상적으로 활성화된다.
- 현재 게임 저장을 실행할 수 있다.
- 월드 아이템과 채집 자원 상태를 저장할 수 있다.

---

## 완료 기준

플레이어가 적의 탐지 거리 안으로 들어가면 적이 `Chasing` 상태로 변경되고 NavMesh 경로를 따라 플레이어를 추적한다.

적이 공격 거리 안으로 들어오면 이동을 멈추고 재사용 대기시간마다 플레이어에게 전투 피해를 전달한다.

플레이어의 근접 공격과 화살 공격은 적의 체력을 감소시키며, 피격·사망·중복 피해 차단이 정상적으로 처리된다.

플레이어의 회피 무적과 피격 후 무적은 적 공격에도 동일하게 적용된다.

Scene의 모든 월드 아이템과 채집 자원은 서로 다른 `World Object ID`를 사용하며, 월드 저장 시스템이 오류 없이 초기화된다.

---

## 다음 개발 방향

65일차에는 적 공격의 시각적·물리적 반응과 기본 AI 완성도를 높인다.

주요 예정 항목:

- 적 공격 준비 시간
- 공격 판정 시점 분리
- 공격 후딜레이
- 플레이어 피격 밀림
- 적 피격 밀림 또는 경직
- 적 공격 범위 시각화
- 적 애니메이션 연결용 이벤트
- 추적 중 장애물과 경사 이동 점검
- 여러 적이 동시에 추적할 때의 회피 품질 개선
