# Project U 개발 일지

---
## 63일차 : 회피 이동 및 전투 무적 시스템 구현

- 개발일: 2026-08-02
- 개발 단계: 플레이어 전투 조작
- 개발 상태: 완료

---
## 개발 목표

기존 `CharacterController` 기반 이동 시스템에 방향 회피를 추가하고, 회피 중에는 적의 전투 피해를 무시할 수 있도록 전투 무적 판정을 구현한다.

회피 기능은 기존 이동·스태미나·근접 공격·활 장전·건축·상호작용 시스템과 충돌하지 않도록 통합하며, 일반 생존 피해와 전투 피해를 구분한다.

---
## 주요 구현 내용

### 1. 방향 회피 이동

- `Left Ctrl` 입력으로 회피를 실행한다.
- WASD 입력 방향을 기준으로 회피 방향을 결정한다.
- 대각선 입력도 다른 방향과 동일한 거리로 이동한다.
- 방향 입력 없이 회피하면 카메라 반대 방향으로 후퇴한다.
- 회피 이동은 기존 `CharacterController.Move()`를 사용한다.
- 벽과 충돌하면 벽을 통과하지 않고 회피가 종료된다.
- 경사면에서는 현재 지면 방향을 따라 회피한다.
- 공중과 낙하 상태에서는 회피를 사용할 수 없다.

### 2. 회피 스태미나와 재사용 대기시간

- 회피할 때 스태미나를 소비한다.
- 스태미나가 부족하면 회피가 실행되지 않는다.
- 회피 후 재사용 대기시간을 적용한다.
- 체온 상태에 따른 기존 행동 스태미나 소비 배율을 그대로 사용한다.

### 3. 회피 중 공격 취소

회피를 시작하면 다음 상태를 취소한다.

- 진행 중인 근접 공격
- 저장된 다음 근접 콤보 입력
- 활 장전
- 활 예상 궤적
- 활시위 당김 상태
- 장전 화살 표시
- 활 조준 카메라 확대

회피 중에는 다음 입력을 차단한다.

- 근접 공격
- 활 장전과 화살 발사
- 채집 공격
- F 상호작용
- 상호작용 안내 표시

### 4. 회피 전투 무적

- 회피 시작 시 일정 시간 동안 전투 무적을 적용한다.
- 회피 무적 중 적의 전투 피해를 차단한다.
- 회피가 끝나거나 중단되면 회피 무적을 종료한다.
- UI, 건축 모드, 사망 또는 컴포넌트 비활성화 상황에서도 무적 상태를 정리한다.

### 5. 피격 후 무적

- 전투 피해를 받은 뒤 짧은 피격 무적 시간을 적용한다.
- 동일한 공격 판정이 여러 프레임 겹쳐도 체력이 연속으로 감소하지 않는다.
- 피격 무적이 끝난 뒤에는 다시 전투 피해를 받을 수 있다.

### 6. 생존 피해와 전투 피해 분리

`PlayerHealth`의 피해 처리를 다음 두 방식으로 구분했다.

#### 일반 피해

```text
TakeDamage(float damageAmount)
```

적용 대상:

- 굶주림
- 갈증
- 추위
- 낙하
- 환경 위험
- 지속 피해

일반 피해는 회피 무적으로 차단하지 않는다.

#### 전투 피해

```text
TakeCombatDamage(float damageAmount)
```

적용 대상:

- 적 근접 공격
- 적 투사체
- 전투 함정
- 전투 충돌 피해

전투 피해는 회피 무적과 피격 후 무적의 영향을 받는다.

### 7. 플레이어 전투 피해 수신기

`PlayerCombatDamageReceiver`를 생성하여 플레이어가 기존 `ICombatDamageReceiver` 규칙으로 전투 피해를 받을 수 있도록 구성했다.

- 여러 Player Collider를 하나의 피해 대상으로 처리한다.
- 동일 공격자와 동일 공격 고유 번호의 중복 피해를 차단한다.
- 무적으로 차단한 피해 횟수를 기록한다.
- 실제 적용된 전투 피해 횟수를 기록한다.
- 개발용 테스트 전투 피해 기능을 제공한다.

### 8. 사망과 부활 상태 정리

- 사망 상태에서는 회피를 사용할 수 없다.
- 사망하면 남아 있는 회피 무적과 피격 무적을 해제한다.
- 부활 시 회피 진행 상태와 재사용 대기시간을 초기화한다.
- 부활 후 전투 무적 상태를 초기화한다.

---
## 기본 설정값

### 회피 설정

| 항목 | 값 |
|---|---:|
| 회피 키 | Left Ctrl |
| 회피 거리 | 4.5m |
| 회피 시간 | 0.35초 |
| 회피 재사용 대기시간 | 0.8초 |
| 스태미나 비용 | 18 |
| 회피 무적 시간 | 0.28초 |
| 무입력 회피 | 후방 회피 |
| 공중 회피 | 사용하지 않음 |
| 벽 충돌 시 종료 | 사용 |

### 전투 무적 설정

| 항목 | 값 |
|---|---:|
| 피격 후 무적 시간 | 0.35초 |
| 동일 공격 단계 중복 차단 | 사용 |
| 생존 피해 무적 적용 | 사용하지 않음 |

---
## 입력 설정

기존 Input Actions의 Player 또는 Gameplay Action Map에 다음 액션을 추가했다.

```text
Action Name: Dodge
Action Type: Button
Control Type: Button
Binding: <Keyboard>/leftCtrl
```

---
## 생성한 스크립트

```text
Assets/_ProjectU/Scripts/Combat/PlayerCombatDamageReceiver.cs
```

---
## 수정한 스크립트

```text
Assets/_ProjectU/Scripts/Player/PlayerMovement.cs
Assets/_ProjectU/Scripts/Survival/PlayerHealth.cs
Assets/_ProjectU/Scripts/Interaction/PlayerInteractor.cs
```

---
## Inspector 연결

### PlayerMovement

```text
Camera Transform              Main Camera
Build Placement Controller    기존 건축 관리자
Weapon Attack Controller      PlayerWeaponAttackController
Bow Charge Controller         PlayerBowChargeController
Dodge Action Reference        Dodge
```

### PlayerCombatDamageReceiver

```text
Player Health                     PlayerHealth
Damage Root                       Player
Reject Duplicate Attack Sequence 사용
Enable Debug Damage Key           사용하지 않음
```

### PlayerInteractor

```text
Player Movement               PlayerMovement
Weapon Attack Controller      PlayerWeaponAttackController
Bow Charge Controller         PlayerBowChargeController
Build Placement Controller    기존 건축 관리자
```

---
## 테스트 결과

- 전후좌우와 대각선 방향으로 회피할 수 있다.
- 방향 입력 없이 회피하면 카메라 반대 방향으로 이동한다.
- 모든 방향에서 회피 거리가 동일하게 유지된다.
- 회피 시 스태미나가 정상적으로 소비된다.
- 스태미나가 부족하면 회피가 실행되지 않는다.
- 회피 재사용 대기시간이 적용된다.
- 공중에서는 회피가 실행되지 않는다.
- 벽을 향해 회피해도 벽을 통과하지 않는다.
- 경사면에서 지면을 따라 회피한다.
- 회피 시작 시 근접 공격과 콤보가 취소된다.
- 회피 시작 시 활 장전과 조준 상태가 취소된다.
- 회피 중 공격·채집·상호작용 입력이 차단된다.
- 회피 무적 중 전투 피해가 차단된다.
- 첫 피격 이후 짧은 시간 동안 추가 전투 피해가 차단된다.
- 동일 공격 단계에서 여러 Collider로 인한 중복 피해가 차단된다.
- 굶주림·추위·낙하 피해는 회피 중에도 적용된다.
- 사망 상태에서는 회피할 수 없다.
- 부활 후 회피와 전투 무적 상태가 정상적으로 초기화된다.

---
## 완료 기준

플레이어가 지상에서 `Left Ctrl`을 누르면 현재 WASD 입력 방향으로 회피하며, 방향 입력이 없으면 후방으로 회피한다.

회피에는 스태미나 비용과 재사용 대기시간이 적용되고, 이동 중 벽을 통과하지 않는다.

회피 시작 시 진행 중인 공격과 활 장전이 취소되며, 회피 중에는 전투 피해를 일정 시간 무시한다.

일반 피격 뒤에는 짧은 피격 무적이 적용되고, 굶주림·추위·낙하와 같은 생존 피해는 전투 무적과 별도로 처리된다.

---
## 다음 개발 방향

64일차에는 적 기본 능력치와 공통 전투 상태 구조를 구현한다.

- 적 체력과 사망 상태
- 적 이동 속도와 공격 능력치
- 플레이어 탐지 거리
- 공격 거리와 공격 대기시간
- 적 전투 피해 전달
- 피격과 사망 이벤트
- 이후 적 AI에서 공통으로 사용할 데이터 구조
