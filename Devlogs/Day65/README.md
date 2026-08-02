# Project U 개발 일지

---

## 65일차 : 적 공격 예고와 피격 밀림 시스템 구현

- 개발일: 2026-08-02
- 개발 단계: 적 공격 반응 및 전투 가독성 개선
- 개발 상태: 완료

---

## 개발 목표

기존 적 공격은 플레이어가 공격 범위 안에 들어오는 즉시 피해를 적용하는 구조였다.

이번 작업에서는 적 공격을 준비, 판정, 후딜레이, 재사용 대기의 단계로 분리하여 플레이어가 공격을 보고 회피할 수 있도록 개선한다.

또한 기존 `CombatHitData`에 포함되어 있던 충격량과 공격 방향을 실제 전투 반응에 연결하여, 적 공격을 맞은 플레이어와 플레이어 공격을 맞은 적이 각각 공격 방향으로 밀리도록 구현한다.

---

## 주요 구현 내용

### 1. 적 공격 세부 단계 추가

`EnemyAttackPhase`를 생성하여 적 공격의 세부 진행 단계를 관리한다.

```text
Ready
새로운 공격을 시작할 수 있는 준비 상태

Windup
공격 전에 플레이어에게 공격을 예고하는 준비 상태

Recovery
공격 판정 이후 적이 행동할 수 없는 후딜레이 상태

Cooldown
후딜레이 종료 후 다음 공격까지 기다리는 상태
```

기본 공격 흐름:

```text
Ready
→ Windup
→ 공격 판정
→ Recovery
→ Cooldown
→ Ready
```

기존 `EnemyCombatState.Attacking` 안에서 공격의 세부 단계를 별도로 관리하도록 구성했다.

---

### 2. 적 공격 시간 데이터 추가

`EnemyCombatData`에 적 공격의 시간과 판정 범위를 조절할 수 있는 설정을 추가했다.

추가 항목:

| 항목 | 기본값 | 설명 |
|---|---:|---|
| Attack Windup Duration | 0.45초 | 공격 시작부터 실제 피해 판정까지의 준비 시간 |
| Attack Recovery Duration | 0.35초 | 피해 판정 이후 적이 행동하지 못하는 후딜레이 |
| Attack Range Grace Distance | 0.35m | 판정 순간 허용하는 추가 공격 거리 |
| Track Target During Windup | On | 공격 준비 중 플레이어 방향으로 회전할지 여부 |

기본 공격 설정:

| 항목 | 값 |
|---|---:|
| Attack Range | 1.8 |
| Attack Damage | 10 |
| Attack Impact Force | 2 |
| Attack Cooldown | 0.65 |
| Attack Windup Duration | 0.45 |
| Attack Recovery Duration | 0.35 |
| Attack Range Grace Distance | 0.35 |

한 번의 기본 공격 주기:

```text
공격 준비 0.45초
+ 공격 후딜레이 0.35초
+ 재사용 대기 0.65초
= 약 1.45초
```

---

### 3. 적 공격 준비와 실제 판정 분리

기존 구조에서는 적이 공격 거리 안에 들어오면 즉시 `ReceiveDamage()`를 호출했다.

수정된 구조에서는 다음 순서로 처리한다.

```text
플레이어가 공격 범위 진입
→ 공격 고유 번호 생성
→ Windup 시작
→ 준비 시간 진행
→ 판정 순간 플레이어 거리 재확인
→ CombatHitData 생성
→ PlayerCombatDamageReceiver에 피해 전달
→ Recovery 시작
→ Cooldown 시작
```

공격 준비가 시작된 시점에는 플레이어 체력이 감소하지 않는다.

`Attack Windup Duration`이 끝나는 순간에만 실제 피해 판정이 발생한다.

---

### 4. 공격 판정 거리 재검사

공격을 시작할 때뿐만 아니라 실제 판정 순간에도 플레이어와 적의 거리를 다시 계산한다.

판정 허용 거리:

```text
Attack Range
+ Attack Range Grace Distance
```

기본 적의 판정 허용 거리:

```text
1.8m + 0.35m
= 2.15m
```

공격 준비 중 플레이어가 2.15m보다 멀리 이동하면 공격은 빗나간다.

빗나간 공격은 다음 효과를 발생시키지 않는다.

- 플레이어 체력 감소
- 플레이어 피격 밀림
- 피격 후 무적 시작

---

### 5. 공격 준비 중 회피 가능

플레이어는 적의 `Windup`을 확인한 뒤 회피할 수 있다.

회피 중 공격 판정이 들어오면 다음 순서로 처리된다.

```text
적 공격 판정
→ PlayerCombatDamageReceiver 수신
→ PlayerHealth 전투 무적 확인
→ 회피 무적으로 피해 차단
→ 밀림 차단
```

회피 무적으로 차단된 공격은 체력과 위치에 영향을 주지 않는다.

---

### 6. 공격 후딜레이 구현

피해 판정이 끝나면 적은 `Recovery` 상태에 들어간다.

Recovery 동안 제한되는 행동:

- 새로운 공격 시작
- NavMesh 추적 이동
- 플레이어 방향 회전
- 공격 예고 표시
- 다음 공격 판정

후딜레이가 끝나면 `Cooldown`으로 이동한다.

Cooldown이 끝난 뒤에만 다음 공격을 시작할 수 있다.

---

### 7. 공격 중 피격 취소

적이 공격 준비 또는 후딜레이 중 플레이어 공격을 맞으면 현재 공격을 취소한다.

처리 흐름:

```text
Windup 또는 Recovery
→ 플레이어 공격 적중
→ EnemyHealth.Damaged 이벤트
→ 현재 공격 절차 취소
→ 공격 예고 숨김
→ EnemyCombatState.Hit
→ 피격 경직 적용
→ Cooldown
```

공격 준비 중 적을 처치하면 준비 중이던 공격은 실행되지 않는다.

---

### 8. 공격 단계 이벤트 구성

향후 적 애니메이션, 효과음과 VFX를 연결할 수 있도록 다음 이벤트를 추가했다.

```text
AttackPhaseChanged
공격 단계가 변경될 때 호출

AttackWindupStarted
공격 준비가 시작될 때 호출

AttackHitFrameReached
실제 공격 판정 시점에 호출

AttackRecoveryStarted
공격 후딜레이가 시작될 때 호출

AttackSequenceFinished
공격 절차가 정상 완료될 때 호출

AttackCancelled
피격, 사망 또는 대상 상실로 공격이 취소될 때 호출
```

현재는 시간 기반으로 판정을 실행하며, 이후 Animator의 Animation Event와 연결할 수 있는 구조로 구성했다.

---

### 9. 적 공격 예고 범위 표시

`EnemyAttackTelegraph`를 추가하여 공격 준비 중 적의 공격 범위를 원형 선으로 표시한다.

Hierarchy 구조:

```text
Enemy_Basic_Test
├─ AttackOrigin
└─ AttackTelegraph
```

`AttackTelegraph` 구성:

```text
Transform
LineRenderer
EnemyAttackTelegraph
```

공격 예고 동작:

```text
Windup 시작
→ 원형 선 표시

공격 준비 진행
→ 노란색에서 붉은색으로 변경
→ 선 굵기 증가

공격 판정
→ 원형 선 숨김
```

기본 설정:

| 항목 | 값 |
|---|---:|
| Circle Segments | 48 |
| Ground Offset | 0.03 |
| Base Line Width | 0.05 |
| Imminent Width Multiplier | 1.8 |

LineRenderer에는 투명한 URP Unlit Material을 사용한다.

---

### 10. 플레이어 피격 밀림

`PlayerCombatImpactMotor`를 생성하여 적 공격의 충격량을 실제 플레이어 이동에 적용한다.

필수 컴포넌트:

```text
CharacterController
PlayerHealth
PlayerCombatImpactMotor
```

밀림 거리 계산:

```text
최종 밀림 거리
= Impact Force × Distance Per Impact Force
```

기본 설정:

| 항목 | 값 |
|---|---:|
| Distance Per Impact Force | 0.35 |
| Impact Duration | 0.18 |
| Maximum Impact Distance | 1.5 |

기본 적 공격 충격량이 2이므로 예상 밀림 거리는 다음과 같다.

```text
2 × 0.35
= 약 0.7m
```

실제 이동에는 `CharacterController.Move()`를 사용하므로 벽과 지형 충돌이 적용된다.

---

### 11. 플레이어 피해 수신과 밀림 연결

`PlayerCombatDamageReceiver`가 실제 체력 피해 적용 후 `PlayerCombatImpactMotor`를 호출하도록 수정했다.

처리 순서:

```text
CombatHitData 수신
→ 생존 상태 확인
→ 동일 공격 번호 중복 확인
→ 회피 또는 피격 무적 확인
→ 체력 피해 적용
→ Impact Force 확인
→ 공격 방향으로 플레이어 밀림
```

밀림은 피해가 실제 적용된 경우에만 발생한다.

다음 상황에서는 밀림도 차단된다.

- 회피 무적
- 피격 후 무적
- 동일 공격 번호 중복
- 플레이어 사망
- 공격 피해량 0

---

### 12. 적 피격 밀림

`EnemyCombatImpactMotor`를 생성하여 플레이어 공격을 맞은 적도 공격 방향으로 밀리도록 구현했다.

필수 컴포넌트:

```text
EnemyHealth
NavMeshAgent
EnemyCombatImpactMotor
```

기본 설정:

| 항목 | 값 |
|---|---:|
| Distance Per Impact Force | 0.12 |
| Impact Duration | 0.16 |
| Maximum Impact Distance | 1.5 |

적 밀림에는 `NavMeshAgent.Move()`를 사용한다.

처리 흐름:

```text
플레이어 공격 적중
→ EnemyHealth 피해 적용
→ Damaged 이벤트
→ 적 추적 이동 정지
→ 공격 방향으로 적 밀림
→ 피격 경직 종료
→ NavMesh 추적 재개
```

근접 공격과 화살 공격 모두 `CombatHitData.ImpactForce`가 0보다 크면 적 밀림이 적용된다.

---

### 13. NavMesh 추적 시스템 연동

기존 `EnemyNavMeshMovement`는 `EnemyCombatState.Chasing`일 때만 이동한다.

적이 다음 상태에 들어가면 추적 이동이 자동으로 멈춘다.

```text
Attacking
Hit
Dead
```

따라서 공격 준비, 후딜레이와 피격 밀림 중에는 적이 플레이어를 추적하지 않는다.

피격 경직과 밀림이 끝나고 적이 다시 `Chasing` 상태가 되면 NavMesh 경로를 갱신하여 추적을 계속한다.

---

## 생성한 스크립트

```text
Assets/_ProjectU/Scripts/Enemy/EnemyAttackPhase.cs
Assets/_ProjectU/Scripts/Enemy/EnemyAttackTelegraph.cs
Assets/_ProjectU/Scripts/Enemy/EnemyCombatImpactMotor.cs
Assets/_ProjectU/Scripts/Combat/PlayerCombatImpactMotor.cs
```

---

## 수정한 스크립트

```text
Assets/_ProjectU/Scripts/Enemy/EnemyCombatData.cs
Assets/_ProjectU/Scripts/Enemy/EnemyCombatController.cs
Assets/_ProjectU/Scripts/Combat/PlayerCombatDamageReceiver.cs
```

---

## 생성한 Material

```text
Assets/_ProjectU/Materials/Combat/M_EnemyAttackTelegraph.mat
```

Material 설정:

```text
Shader: Universal Render Pipeline/Unlit
Surface Type: Transparent
Render Face: Both
Depth Write: Off
```

---

## 수정한 Scene

```text
Assets/_ProjectU/Scenes/20_Gameplay.unity
```

Scene 변경 내용:

- Player에 `PlayerCombatImpactMotor` 추가
- `PlayerCombatDamageReceiver`에 Impact Motor 연결
- Enemy_Basic_Test에 `EnemyCombatImpactMotor` 추가
- Enemy_Basic_Test 자식으로 AttackTelegraph 추가
- AttackTelegraph에 LineRenderer 추가
- AttackTelegraph에 `EnemyAttackTelegraph` 추가
- 공격 예고 Material 연결
- EnemyCombatData_Basic 공격 시간 설정 변경

---

## 테스트 결과

### 공격 단계

- 적이 공격 범위 안에 들어오면 `Windup`을 시작한다.
- Windup 시작 시 즉시 피해가 발생하지 않는다.
- 준비 시간이 끝나는 시점에만 피해 판정이 발생한다.
- 공격 판정 후 `Recovery`가 적용된다.
- Recovery 종료 후 `Cooldown`이 적용된다.
- Cooldown 종료 후 다시 `Ready`가 된다.
- 공격 절차 중 중복 공격이 시작되지 않는다.

### 공격 예고

- Windup에서 원형 공격 예고가 표시된다.
- 준비 진행률에 따라 노란색에서 붉은색으로 변경된다.
- 판정 직전에 선이 굵어진다.
- 공격 판정 후 예고 선이 사라진다.
- 피격이나 사망으로 공격이 취소되면 즉시 사라진다.

### 회피와 범위 이탈

- 공격 준비 중 회피할 수 있다.
- 회피 무적으로 공격 피해를 차단할 수 있다.
- 회피로 차단한 공격은 플레이어를 밀지 않는다.
- 판정 순간 허용 거리 밖이면 공격이 빗나간다.
- 빗나간 공격은 체력과 위치에 영향을 주지 않는다.

### 플레이어 밀림

- 실제 적 공격 피해를 받으면 플레이어가 공격 방향으로 밀린다.
- CharacterController 충돌이 적용된다.
- 벽 근처에서는 실제 밀림 거리가 줄어든다.
- 회피 무적과 피격 후 무적 중에는 밀림이 적용되지 않는다.
- 플레이어 사망 상태에서는 밀림이 중단된다.

### 적 밀림

- 근접 공격을 맞은 적이 공격 방향으로 밀린다.
- 화살 공격의 충격량도 적 밀림에 적용된다.
- 적 밀림 중 NavMesh 추적이 정지한다.
- 밀림과 피격 경직이 끝나면 추적을 재개한다.
- 사망한 적은 밀림을 중단한다.
- 적이 NavMesh 위에 있을 때 정상적으로 동작한다.

### 공격 취소

- Windup 중 적을 공격하면 현재 공격이 취소된다.
- Recovery 중 피격되어도 현재 공격 절차가 정리된다.
- 피격 후 적은 `Hit` 상태로 변경된다.
- 공격 취소 후 기본 Cooldown이 적용된다.
- 준비 중 적을 처치하면 공격 판정이 발생하지 않는다.

---

## 완료 기준

적이 플레이어를 추적하여 공격 범위 안에 들어오면 즉시 피해를 주지 않고 공격 준비를 시작한다.

공격 준비 중 원형 예고가 표시되며, 준비 시간이 끝나는 순간에만 실제 공격 판정이 발생한다.

플레이어는 예고를 보고 회피하거나 공격 범위 밖으로 이동해 피해를 피할 수 있다.

공격에 실제로 맞으면 플레이어가 공격 방향으로 밀리고, 회피 또는 피격 후 무적으로 막으면 밀림도 발생하지 않는다.

플레이어 공격을 맞은 적은 공격 방향으로 밀리며, 공격 준비 중 피격되면 준비 중이던 공격이 취소된다.

공격 판정 후에는 후딜레이와 재사용 대기시간이 순서대로 적용되며, 모든 단계가 끝난 뒤에만 다음 공격을 시작한다.

---

## 다음 개발 방향

66일차에는 적 전투 상태를 실제 애니메이션과 효과에 연결한다.

주요 예정 항목:

- 적 Animator Controller 구성
- Idle, Move, Attack, Hit, Death 애니메이션 상태 연결
- 공격 준비와 실제 판정 Animation Event 연결
- 공격 효과음 재생
- 피격 효과음과 피격 VFX
- 사망 애니메이션 종료 후 Collider 처리
- 애니메이션이 없는 임시 모델용 기본 반응 유지
