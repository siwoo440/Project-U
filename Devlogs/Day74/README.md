# Project U 개발 일지

## 74일차 : 대표 근접·원거리 몬스터 콘텐츠 제작

### 1. 개발 목표

74일차에는 기존에 구현되어 있던 적 공통 전투 시스템을 기반으로 실제 전투 방식이 서로 다른 대표 몬스터 2종을 제작하였다.

기존의 `EnemyCombatData`, `EnemyHealth`, `EnemyCombatController`, `EnemyNavMeshMovement`, `EnemyAttackTelegraph` 구조를 최대한 재사용하고, 원거리 공격에 필요한 부분만 별도의 신규 스크립트로 추가하였다.

이번 일차의 핵심 목표는 다음과 같다.

- 대표 근접 몬스터 제작
- 대표 원거리 몬스터 제작
- 적별 개별 `EnemyCombatData` 분리
- 원거리 적 투사체 공격 구현
- 기존 NavMesh 추적 시스템 재사용
- 플레이어 전투 피해 시스템과 원거리 공격 연결
- 근접·원거리 적의 전투 방식 차이 확인
- 기존 Basic Enemy 회귀 테스트
- GameDataRegistry 적 데이터 갱신

---

## 2. 신규 몬스터 구성

이번 일차에는 다음 두 종류의 몬스터를 추가하였다.

| 종류 | 이름 | ID | 역할 |
| --- | --- | --- | --- |
| 근접형 | Melee Grunt | `enemy_melee_grunt` | 플레이어에게 빠르게 접근하여 근접 공격 |
| 원거리형 | Ranged Spitter | `enemy_ranged_spitter` | 일정 거리에서 멈춘 뒤 투사체 발사 |

두 적은 동일한 공통 전투 기반을 사용하지만 공격 방식과 전투 거리를 다르게 구성하였다.

---

## 3. 기존 적 전투 시스템 재사용

기존 프로젝트에는 다음 적 시스템이 구현되어 있었다.

```text
EnemyCombatData
EnemyHealth
EnemyCombatController
EnemyNavMeshMovement
EnemyAttackTelegraph
```

이 구조를 그대로 활용하여 다음 기능을 재사용하였다.

- 플레이어 자동 탐지
- 탐지 거리와 추적 해제 거리
- NavMesh 추적 이동
- 공격 거리 도달 시 정지
- 플레이어 방향 회전
- 체력과 방어력
- 피격 상태
- 공격 준비 시간
- 공격 후딜레이
- 공격 재사용 대기시간
- 피격 중 공격 취소
- 사망 상태
- 플레이어 공격자 즉시 추적
- 공격 예고

새로운 몬스터별 차이는 `EnemyCombatData`와 일부 전용 공격 컴포넌트에서 관리하도록 구성하였다.

---

## 4. Melee Grunt 데이터 제작

근접형 대표 몬스터용 데이터를 생성하였다.

```text
EnemyCombatData_MeleeGrunt
```

### Identity

```text
Enemy Id: enemy_melee_grunt
Display Name: MELEE GRUNT
```

### Health

```text
Maximum Health: 60
Defense Percent: 5
```

### Movement

```text
Move Speed: 3.2
Rotation Speed: 420
```

### Detection

```text
Detection Range: 11
Lose Target Range: 15
```

### Attack

```text
Attack Range: 1.7
Attack Damage: 12
Attack Impact Force: 2.5
Attack Cooldown: 1.1
```

### Attack Timing

```text
Attack Windup Duration: 0.45
Attack Recovery Duration: 0.35
Attack Range Grace Distance: 0.35
Track Target During Windup: On
```

### Reaction / Death

```text
Hit Reaction Duration: 0.2

Disable Colliders On Death: On
Destroy After Death: Off
Death Cleanup Delay: 5
```

---

## 5. Melee Grunt Prefab 제작

기존 Basic Enemy 테스트 오브젝트를 기반으로 근접형 Prefab을 제작하였다.

```text
Enemy_MeleeGrunt.prefab
```

주요 구성:

```text
Enemy_MeleeGrunt
├─ EnemyHealth
├─ EnemyCombatController
├─ EnemyNavMeshMovement
├─ NavMeshAgent
├─ CapsuleCollider
├─ AttackOrigin
└─ 임시 Visual
```

`EnemyHealth`의 Combat Data를 다음 데이터로 변경하였다.

```text
EnemyCombatData_MeleeGrunt
```

`EnemyCombatController`는 기존 근접 공격 구조를 그대로 사용하였다.

```text
Auto Find Player: On
Rotate Toward Target: On
Auto Attack When In Range: On
```

따라서 Melee Grunt는 기존 공통 공격 시스템만으로 동작한다.

---

## 6. Melee Grunt 임시 외형

근접형과 원거리형을 쉽게 구분할 수 있도록 임시 Material을 제작하였다.

```text
M_Enemy_MeleeGrunt_Day74
```

근접형은 어두운 빨강 또는 갈색 계열로 설정하여 접근형 적이라는 점을 쉽게 확인할 수 있도록 하였다.

현재 외형은 기능 테스트용 임시 Visual이며 후반 외형 정리 단계에서 실제 모델과 Material로 교체할 예정이다.

---

## 7. Ranged Spitter 데이터 제작

원거리형 대표 몬스터용 데이터를 생성하였다.

```text
EnemyCombatData_RangedSpitter
```

### Identity

```text
Enemy Id: enemy_ranged_spitter
Display Name: RANGED SPITTER
```

### Health

```text
Maximum Health: 40
Defense Percent: 0
```

### Movement

```text
Move Speed: 2.6
Rotation Speed: 360
```

### Detection

```text
Detection Range: 14
Lose Target Range: 18
```

### Attack

```text
Attack Range: 7.5
Attack Damage: 8
Attack Impact Force: 1.2
Attack Cooldown: 1.6
```

### Attack Timing

```text
Attack Windup Duration: 0.65
Attack Recovery Duration: 0.45
Attack Range Grace Distance: 1
Track Target During Windup: On
```

원거리 적은 공격 거리를 크게 설정하여 기존 `EnemyNavMeshMovement`가 플레이어에게 너무 가까이 접근하지 않고 약 7m 전후에서 정지하도록 구성하였다.

---

## 8. Ranged Spitter Prefab 제작

Melee Grunt Prefab을 복제하여 원거리형 적을 제작하였다.

```text
Enemy_RangedSpitter.prefab
```

`EnemyHealth`의 Combat Data는 다음으로 변경하였다.

```text
EnemyCombatData_RangedSpitter
```

원거리형의 기존 `EnemyCombatController`에서는 자동 근접 공격을 비활성화하였다.

```text
Auto Attack When In Range: Off
```

이 설정을 통해 기존 근접 판정과 신규 원거리 공격이 동시에 실행되는 문제를 방지하였다.

---

## 9. 원거리 적 역할 분리

Ranged Spitter에서는 기존 전투 시스템과 신규 공격 시스템의 역할을 다음과 같이 분리하였다.

```text
EnemyCombatController
├─ 플레이어 탐지
├─ 추적 상태
├─ 공격 거리 판정
├─ 플레이어 방향 회전
└─ Attacking 상태 전환

EnemyRangedAttackController
├─ 원거리 공격 준비
├─ 투사체 생성
├─ 공격 후딜레이
└─ 공격 쿨타임
```

기존 공통 시스템을 크게 수정하지 않고 원거리 공격만 독립적인 컴포넌트로 추가하였다.

---

## 10. 신규 EnemyProjectile 스크립트

원거리 적이 실제 투사체를 발사할 수 있도록 다음 스크립트를 추가하였다.

```text
Assets/_ProjectU/Scripts/Enemy/Day74/EnemyProjectile.cs
```

주요 역할:

- 공격자 저장
- 피해량 저장
- 충격량 저장
- 공격 Sequence ID 저장
- 이동 방향 저장
- 투사체 속도 적용
- 최대 생존 시간 관리
- SphereCast 충돌 검사
- 공격자 자신의 Collider 무시
- 플레이어 피격 판정
- `CombatHitData` 생성
- `WeaponAttackType.Ranged` 적용
- 장애물 충돌 시 제거
- 수명 종료 시 자동 삭제

투사체는 Rigidbody 기반이 아니라 매 프레임 SphereCast를 사용하여 빠른 투사체가 Collider를 통과하는 문제를 줄이도록 구성하였다.

---

## 11. 신규 EnemyRangedAttackController 스크립트

원거리 적 공격을 관리하기 위해 다음 스크립트를 추가하였다.

```text
Assets/_ProjectU/Scripts/Enemy/Day74/EnemyRangedAttackController.cs
```

주요 역할:

- `EnemyCombatController` 상태 확인
- 플레이어가 Attack Range 안에 있는지 확인
- 원거리 공격 준비 시간 처리
- 투사체 Prefab 생성
- 플레이어 중심 방향 계산
- 공격 후딜레이 처리
- 공격 쿨타임 처리
- 공격 Sequence ID 증가
- 투사체 피해량을 `EnemyCombatData`에서 전달

공격 데이터는 별도 중복 값으로 관리하지 않고 기존 `EnemyCombatData`의 값을 사용하도록 구성하였다.

---

## 12. 원거리 투사체 Prefab 제작

원거리 공격에서 사용할 투사체 Prefab을 제작하였다.

```text
EnemyProjectile.prefab
```

기본 구성:

```text
EnemyProjectile
├─ Transform
├─ MeshFilter
├─ MeshRenderer
└─ EnemyProjectile
```

기능 테스트용으로 작은 Sphere Mesh를 사용하였다.

기존 SphereCollider는 제거하고 `EnemyProjectile` 내부 SphereCast로 충돌을 처리하도록 구성하였다.

### 임시 Material

```text
M_EnemyProjectile_Day74
```

원거리 적과 투사체를 쉽게 확인할 수 있도록 밝은 색상의 임시 Material을 적용하였다.

---

## 13. EnemyRangedAttackController 설정

Ranged Spitter에 다음 값을 적용하였다.

```text
Projectile Speed: 12
Projectile Lifetime: 5
Projectile Hit Radius: 0.12
Projectile Collision Mask: Everything
Target Height Offset: 0
Auto Fire When In Range: On
```

참조:

```text
Combat Controller:
Enemy_RangedSpitter의 EnemyCombatController

Attack Origin:
AttackOrigin

Projectile Prefab:
EnemyProjectile
```

---

## 14. 원거리 공격 처리 흐름

Ranged Spitter의 전체 공격 흐름은 다음과 같다.

```text
플레이어가 Detection Range 진입
↓
EnemyCombatController가 플레이어 탐지
↓
EnemyNavMeshMovement 추적
↓
약 7.5m 공격 거리 도달
↓
NavMesh 이동 정지
↓
EnemyCombatController 상태 Attacking
↓
EnemyRangedAttackController 공격 준비
↓
0.65초 Windup
↓
EnemyProjectile 생성
↓
플레이어 방향으로 직선 이동
↓
Player Collider 충돌
↓
CombatHitData 생성
↓
WeaponAttackType.Ranged
↓
PlayerCombatDamageReceiver
↓
플레이어 체력 감소 및 밀림
```

---

## 15. 플레이어 전투 피해 시스템 연결

원거리 투사체는 별도의 플레이어 체력 시스템을 만들지 않고 기존 `PlayerCombatDamageReceiver`를 사용하였다.

투사체가 플레이어와 충돌하면 다음 정보를 전달한다.

```text
Attacker
Attack Type
Damage
Impact Force
Hit Point
Hit Direction
Hit Collider
Attack Sequence ID
```

이를 통해 기존의 다음 기능도 그대로 적용된다.

- 플레이어 체력 감소
- 피격 밀림
- 전투 무적
- 회피 무적
- 동일 공격 중복 차단

---

## 16. 원거리 적 이동 방식

Ranged Spitter의 `Attack Range`를 7.5로 설정하였다.

기존 `EnemyNavMeshMovement`는 공격 거리를 기준으로 NavMesh Agent의 정지 거리를 계산하므로 별도의 거리 유지 AI를 만들지 않고도 원거리형 기본 행동을 구성할 수 있었다.

현재 행동:

```text
14m 안
→ 플레이어 탐지

7.5m보다 멀음
→ 플레이어 추적

약 7.5m 도달
→ 이동 정지

공격 준비
→ 투사체 발사
```

현재 단계에서는 플레이어가 적에게 가까이 접근했을 때 뒤로 도망가는 후퇴 AI는 구현하지 않았다.

---

## 17. 근접 몬스터 테스트

Melee Grunt의 기본 행동을 확인하였다.

테스트 흐름:

```text
Idle
→ 플레이어 탐지
→ Chasing
→ 공격 거리 접근
→ Attacking
→ Windup
→ 근접 피해 판정
→ Recovery
→ Cooldown
```

확인 항목:

- 탐지 거리 정상
- NavMesh 추적 정상
- 공격 거리 정지 정상
- 공격 준비 정상
- 플레이어 피해 12 적용
- 피격 밀림 정상
- 공격 쿨타임 정상

---

## 18. 원거리 몬스터 테스트

Ranged Spitter의 행동을 확인하였다.

테스트 흐름:

```text
Idle
→ 플레이어 탐지
→ Chasing
→ 약 7.5m 거리 정지
→ 원거리 Windup
→ 투사체 생성
→ 직선 이동
→ 플레이어 또는 장애물 충돌
```

확인 항목:

- 탐지 거리 정상
- NavMesh 추적 정상
- 원거리 공격 거리 정지 정상
- 투사체 생성 정상
- 투사체 이동 정상
- 플레이어 피해 8 적용
- 원거리 공격 쿨타임 정상

---

## 19. 투사체 회피 테스트

투사체가 생성된 뒤 플레이어가 옆으로 이동하도록 테스트하였다.

투사체는 발사 시점의 방향을 기준으로 직선 이동한다.

따라서 플레이어가 이동하면 다음과 같이 동작한다.

```text
투사체 발사
↓
플레이어 측면 이동
↓
투사체 기존 방향 유지
↓
플레이어를 지나침
```

유도탄처럼 플레이어 방향을 계속 추적하지 않는 것을 확인하였다.

---

## 20. 장애물 충돌 테스트

Ranged Spitter와 플레이어 사이에 벽 또는 구조물을 배치하였다.

```text
Ranged Spitter
→ EnemyProjectile
→ Wall
→ Player
```

정상 결과:

```text
투사체가 Wall에 충돌
→ 투사체 제거
→ Player 피해 없음
```

이를 통해 원거리 공격이 장애물을 무시하고 플레이어에게 직접 피해를 주지 않도록 구성하였다.

---

## 21. 회피 무적 테스트

플레이어가 투사체 피격 순간 기존 회피 기능을 사용하도록 테스트하였다.

정상 흐름:

```text
EnemyProjectile
→ Player Collider 충돌
→ PlayerCombatDamageReceiver
→ 전투 무적 확인
→ 피해 차단
```

기존 회피 및 전투 무적 시스템과 신규 원거리 공격이 정상적으로 연결되었다.

---

## 22. 적 피격 및 공격 취소 테스트

Melee Grunt와 Ranged Spitter를 플레이어 무기로 공격하였다.

확인 항목:

- `EnemyHealth` 체력 감소
- Hit 상태 진입
- 공격 준비 중 피격 시 공격 취소
- 공격한 플레이어를 추적 대상으로 지정
- 사망 시 공격 중단
- 사망 후 이동 중단

---

## 23. 몬스터 사망 테스트

### Melee Grunt

```text
Maximum Health: 60
```

체력을 모두 감소시킨 뒤 다음을 확인하였다.

- Dead 상태 전환
- Collider 비활성화
- 공격 중단
- NavMesh 이동 중단

### Ranged Spitter

```text
Maximum Health: 40
```

공격 준비 중에도 처치 테스트를 진행하였다.

사망 이후에는 새로운 투사체가 생성되지 않도록 확인하였다.

이미 발사된 투사체는 기존 방향으로 계속 이동하도록 유지하였다.

---

## 24. 두 몬스터 동시 전투 테스트

두 몬스터를 동시에 활성화하였다.

정상 전투 흐름:

```text
Melee Grunt
→ 빠르게 플레이어에게 접근
→ 근접 공격

Ranged Spitter
→ 일정 거리에서 정지
→ 원거리 투사체 공격
```

두 적이 단순히 능력치만 다른 것이 아니라 실제 전투 방식이 구분되는 것을 확인하였다.

---

## 25. 원거리 공격 예고 처리

기존 `EnemyAttackTelegraph`는 `EnemyCombatController`의 공격 단계 이벤트를 기준으로 동작한다.

Melee Grunt는 기존 공격 구조를 그대로 사용하므로 기존 Telegraph를 사용할 수 있다.

Ranged Spitter는 별도의 `EnemyRangedAttackController` 공격 단계를 사용하기 때문에 이번 일차에서는 별도의 원거리 공격 예고 UI를 추가하지 않았다.

원거리 공격 예고, 발사 효과, 피격 효과 등은 이후 전투 연출 작업에서 추가할 예정이다.

---

## 26. GameDataRegistry 갱신

신규 적 데이터 2개를 추가한 뒤 GameDataRegistry를 갱신하였다.

```text
Project U
→ Data
→ Create Or Refresh Game Data Registry
```

검증:

```text
Project U
→ Data
→ Validate Default Game Data Registry
```

적 데이터 구성:

```text
enemy_basic
enemy_melee_grunt
enemy_ranged_spitter
```

확인 항목:

- Enemy ID 중복 없음
- 잘못된 ID 없음
- 신규 EnemyCombatData 등록 정상

---

## 27. 신규 스크립트

이번 일차에서 신규로 추가한 C# 파일은 다음 2개이다.

```text
Assets/_ProjectU/Scripts/Enemy/Day74/
├─ EnemyProjectile.cs
└─ EnemyRangedAttackController.cs
```

기존 `EnemyCombatController`, `EnemyNavMeshMovement`, `EnemyHealth` 등은 수정하지 않았다.

기존 시스템을 유지한 상태에서 원거리 기능만 추가함으로써 기존 적 콘텐츠에 미치는 영향을 최소화하였다.

---

## 28. 주요 신규 Asset 구조

```text
Assets/_ProjectU/
├─ Data/
│  └─ Enemies/
│     └─ Day74/
│        ├─ EnemyCombatData_MeleeGrunt.asset
│        └─ EnemyCombatData_RangedSpitter.asset
│
├─ Scripts/
│  └─ Enemy/
│     └─ Day74/
│        ├─ EnemyProjectile.cs
│        └─ EnemyRangedAttackController.cs
│
├─ Prefabs/
│  └─ Enemies/
│     └─ Day74/
│        ├─ Enemy_MeleeGrunt.prefab
│        ├─ Enemy_RangedSpitter.prefab
│        └─ EnemyProjectile.prefab
│
└─ Art/
   └─ Materials/
      └─ Enemies/
         └─ Day74/
            ├─ M_Enemy_MeleeGrunt_Day74.mat
            ├─ M_Enemy_RangedSpitter_Day74.mat
            └─ M_EnemyProjectile_Day74.mat
```

---

## 29. 기존 Basic Enemy 회귀 테스트

신규 몬스터 추가 후 기존 `Enemy_Basic_Test`도 다시 활성화하여 테스트하였다.

확인 항목:

- 플레이어 탐지 정상
- NavMesh 추적 정상
- 근접 공격 정상
- 피격 정상
- 공격 예고 정상
- 사망 정상

신규 원거리 스크립트 추가로 기존 적 시스템에 문제가 발생하지 않는 것을 확인하였다.

---

## 30. 74일차 결과

74일차 이전에는 대표적인 기본 근접 공격형 적만 존재하였다.

```text
Basic Enemy
```

74일차 이후:

```text
공통 Enemy 시스템
        ↓
┌──────────────────────┐
│                      │
Melee Grunt        Ranged Spitter
│                      │
근접 추적              원거리 추적
│                      │
근접 공격              투사체 생성
│                      │
└──────── Player ──────┘
```

형태로 전투 콘텐츠를 확장하였다.

이를 통해 이후 새로운 몬스터를 제작할 때 모든 AI와 전투 시스템을 처음부터 만드는 대신 다음 방식으로 확장할 수 있게 되었다.

```text
EnemyCombatData 복제
→ 수치 조정
→ 기존 Enemy Prefab 복제
→ 공격 방식 선택
→ 임시 Visual 변경
→ Scene 배치
→ 전투 테스트
```

---

## 31. 다음 개발 방향

75일차에는 이번에 제작한 Melee Grunt와 Ranged Spitter를 실제 월드 콘텐츠로 연결한다.

주요 작업 예정:

1. 몬스터 Spawn Point 구조 제작
2. 지역별 생성 가능 몬스터 설정
3. 플레이어 거리 또는 조건에 따른 생성
4. 처치 후 재생성 시간 처리
5. 몬스터 처치 이벤트 연결
6. 몬스터별 Drop Table 작성
7. 처치 시 재료 및 아이템 보상 지급
8. 생성·사망·재생성 반복 테스트
9. 보상 획득과 인벤토리 연결
10. 기존 전투 기능 회귀 테스트

예상 흐름:

```text
지역 진입
→ Spawn Point 활성화
→ 몬스터 생성
→ 전투
→ 몬스터 처치
→ Drop 보상 생성
→ 일정 시간 경과
→ 몬스터 재생성
```

---

## 74일차 완료 상태

- [x] `EnemyCombatData_MeleeGrunt` 생성
- [x] `enemy_melee_grunt` ID 설정
- [x] `Enemy_MeleeGrunt` Prefab 제작
- [x] 근접 추적 동작 확인
- [x] 근접 공격 동작 확인
- [x] 근접 피격 및 사망 확인
- [x] `EnemyCombatData_RangedSpitter` 생성
- [x] `enemy_ranged_spitter` ID 설정
- [x] `Enemy_RangedSpitter` Prefab 제작
- [x] 기존 근접 자동 공격 비활성화
- [x] `EnemyProjectile.cs` 추가
- [x] `EnemyRangedAttackController.cs` 추가
- [x] EnemyProjectile Prefab 제작
- [x] 원거리 적 공격 거리 정지 확인
- [x] 원거리 투사체 발사 확인
- [x] 투사체 Player 피해 확인
- [x] 투사체 장애물 충돌 확인
- [x] 투사체 회피 확인
- [x] 플레이어 전투 무적 연결 확인
- [x] 두 종류의 적 동시 전투 확인
- [x] GameDataRegistry 갱신
- [x] Enemy ID 중복 검사
- [x] 기존 Basic Enemy 회귀 테스트
- [x] Console 오류 확인

---

## Git Commit

```text
74일차 : 대표 근접·원거리 몬스터 콘텐츠 제작
```
