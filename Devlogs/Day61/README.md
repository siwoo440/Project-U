# 프로젝트 U 개발 일지

## 61일차 : 근접 연속 공격 및 중복 피해 방지 구현

### 개발 목표

60일차에 구현한 단일 근접 공격 구조를 확장하여 아이템마다 여러 단계의 연속 공격을 설정할 수 있도록 한다.

근접 공격을 준비, 타격, 복귀 단계로 분리하고 실제 타격 유효 시간 동안 공격 판정을 반복하되, 같은 공격 단계에서는 같은 대상에게 피해가 한 번만 적용되도록 구성한다.

도끼와 곡괭이에 각각 3단 연속 공격 데이터를 연결하고 공격 단계별 피해량, 공격 범위, 스태미나 비용, 충격량과 연출 속도를 개별적으로 조정할 수 있도록 한다.

---

## 구현 내용

### 1. 근접 공격 진행 단계 추가

`MeleeAttackPhase`를 새로 생성하여 근접 공격의 진행 상태를 구분하였다.

```text
None
→ 공격 진행 없음

Windup
→ 공격 준비 단계

Active
→ 실제 피해 판정 단계

Recovery
→ 공격 후 복귀 단계
```

공격 판정은 `Active` 단계에서만 실행된다.

준비 단계와 복귀 단계에서는 피해를 줄 수 없도록 구성하였다.

---

### 2. 공격 단계별 데이터 구조 구현

`MeleeAttackStepData`를 새로 생성하였다.

각 연속 공격 단계는 다음 데이터를 개별적으로 가진다.

| 항목 | 설명 |
| --- | --- |
| Step Name | 공격 단계 이름 |
| Windup Duration | 실제 타격 전 준비 시간 |
| Active Duration | 피해 판정 유효 시간 |
| Recovery Duration | 공격 후 복귀 시간 |
| Input Buffer Start Normalized | 다음 공격 입력 저장 시작 비율 |
| Damage Multiplier | 기본 피해량 배율 |
| Range Multiplier | 기본 공격 거리 배율 |
| Radius Multiplier | 기본 공격 반지름 배율 |
| Stamina Cost Multiplier | 기본 스태미나 비용 배율 |
| Impact Force Multiplier | 기본 충격량 배율 |
| Maximum Targets | 한 단계에서 피해를 줄 수 있는 최대 대상 수 |
| Animation Speed Multiplier | 휘두르기 연출 속도 배율 |

이를 통해 같은 무기 안에서도 1단, 2단, 3단 공격의 속도와 위력을 다르게 구성할 수 있다.

---

### 3. 아이템별 연속 공격 데이터 구현

`MeleeComboData`를 새로 생성하였다.

연속 공격 데이터는 다음 정보를 관리한다.

- 연속 공격 이름
- 공격 단계 배열
- 다음 공격 단계를 유지할 시간
- 단계별 준비·타격·복귀 시간
- 단계별 능력치 배율
- 단계별 최대 피해 대상 수
- 단계별 휘두르기 속도

연속 공격 설정은 `ItemData` 안에 직접 넣지 않고 별도의 ScriptableObject로 분리하였다.

따라서 여러 아이템이 서로 다른 연속 공격 데이터를 사용할 수 있다.

---

### 4. ItemData에 MeleeComboData 연결

`ItemData`의 근접 공격 영역에 다음 필드를 추가하였다.

```text
Melee Combo Data
```

근접 공격 아이템은 해당 필드에 연속 공격 데이터를 연결할 수 있다.

연속 공격 데이터가 연결된 경우 실제 공격 속도는 다음 값으로 결정된다.

```text
Windup Duration
+
Active Duration
+
Recovery Duration
```

기존 `Attack Cooldown`은 연속 공격 데이터가 없는 아이템의 단일 공격 시간으로 계속 사용된다.

원거리 공격 아이템에는 근접 연속 공격 데이터가 적용되지 않는다.

---

### 5. 도끼 3단 연속 공격 구성

`MeleeCombo_Axe`를 생성하여 도끼에 3단 공격을 설정하였다.

#### 도끼 1단

| 항목 | 값 |
| --- | --- |
| Step Name | AXE SLASH 1 |
| Windup | 0.12초 |
| Active | 0.08초 |
| Recovery | 0.20초 |
| Damage Multiplier | 1.0 |
| Stamina Multiplier | 1.0 |
| Maximum Targets | 1 |
| Animation Speed | 1.0 |

기본 피해량 12가 그대로 적용된다.

```text
12 × 1.0
= 12 피해
```

#### 도끼 2단

| 항목 | 값 |
| --- | --- |
| Step Name | AXE SLASH 2 |
| Windup | 0.10초 |
| Active | 0.08초 |
| Recovery | 0.18초 |
| Damage Multiplier | 1.05 |
| Radius Multiplier | 1.05 |
| Impact Multiplier | 1.1 |
| Maximum Targets | 1 |
| Animation Speed | 1.1 |

```text
12 × 1.05
= 12.6 피해
```

#### 도끼 3단

| 항목 | 값 |
| --- | --- |
| Step Name | AXE FINISH |
| Windup | 0.16초 |
| Active | 0.10초 |
| Recovery | 0.30초 |
| Damage Multiplier | 1.3 |
| Range Multiplier | 1.05 |
| Radius Multiplier | 1.1 |
| Stamina Multiplier | 1.2 |
| Impact Multiplier | 1.4 |
| Maximum Targets | 2 |
| Animation Speed | 0.9 |

```text
12 × 1.3
= 15.6 피해
```

도끼 3단 공격은 서로 다른 대상 두 개까지 피해를 줄 수 있도록 구성하였다.

---

### 6. 곡괭이 3단 연속 공격 구성

`MeleeCombo_Pickaxe`를 생성하여 곡괭이에 3단 공격을 설정하였다.

도끼보다 준비 시간과 복귀 시간이 길고 마지막 공격의 피해량과 충격량이 높도록 구성하였다.

#### 곡괭이 1단

```text
10 × 1.0
= 10 피해
```

#### 곡괭이 2단

```text
10 × 1.1
= 11 피해
```

#### 곡괭이 3단

```text
10 × 1.4
= 14 피해
```

곡괭이 3단 연속 공격의 총 기본 피해는 다음과 같다.

```text
10 + 11 + 14
= 35 피해
```

---

### 7. 연속 공격 입력 저장 구현

공격 중 다음 좌클릭 입력을 저장하는 입력 버퍼를 구현하였다.

각 공격 단계는 `Input Buffer Start Normalized` 값을 가진다.

예를 들어 값이 `0.4`라면 공격 전체 진행도의 40% 이후부터 다음 공격 입력을 저장할 수 있다.

```text
1단 공격 시작
→ 입력 저장 구간 진입
→ 좌클릭
→ 2단 공격 예약

2단 공격 중 입력 저장
→ 3단 공격 예약
```

한 공격 단계에서는 다음 공격 입력을 한 번만 저장할 수 있다.

입력 저장 구간 이전의 지나치게 빠른 입력은 무시된다.

---

### 8. 공격 종료 후 연속 단계 유지

공격 도중 다음 입력을 예약하지 않았더라도 공격 종료 후 일정 시간 안에 다시 좌클릭하면 다음 단계에서 이어지도록 구성하였다.

도끼 기본 유지 시간:

```text
Combo Reset Delay
→ 0.9초
```

곡괭이 기본 유지 시간:

```text
Combo Reset Delay
→ 1초
```

정해진 시간이 지나면 연속 공격 단계가 1단으로 초기화된다.

아이템을 변경하면 기존 아이템의 연속 공격 단계는 이어지지 않는다.

---

### 9. 공격 단계별 스태미나 소비

스태미나는 전체 연속 공격 시작 시 한꺼번에 소비하지 않고 각 공격 단계가 실제로 시작될 때 소비하도록 구성하였다.

도끼 기본 스태미나 비용은 8이다.

```text
1단
8 × 1.0
= 8

2단
8 × 1.0
= 8

3단
8 × 1.2
= 9.6
```

도끼 3단 연속 공격의 총 비용은 다음과 같다.

```text
8 + 8 + 9.6
= 25.6
```

다음 단계가 예약되어 있어도 해당 단계 시작 시 스태미나가 부족하면 그 단계는 실행되지 않는다.

스태미나 부족 시 연속 공격 진행도도 초기화된다.

---

### 10. 타격 유효 시간 반복 판정

기존 공격은 좌클릭 순간 한 번만 SphereCast를 실행하였다.

61일차에는 `Active Duration` 동안 매 프레임 공격 범위를 검사하도록 변경하였다.

따라서 공격 시작 시 범위 밖에 있던 대상이 타격 유효 시간 중 범위 안으로 들어오면 피해를 받을 수 있다.

공격 방향은 각 단계의 `Active` 시작 시 결정되고 해당 단계가 끝날 때까지 유지된다.

다음 연속 공격 단계가 시작되면 Camera 방향을 다시 계산한다.

---

### 11. 동일 대상 중복 피해 방지

`Active Duration` 동안 공격 판정을 반복하면 같은 대상이 여러 프레임에 걸쳐 탐지될 수 있다.

이를 방지하기 위해 현재 공격 단계에서 피해를 받은 대상의 `DamageRoot`를 `HashSet<Transform>`에 저장하도록 구성하였다.

```text
1단 Active 구간
→ 같은 대상 피해 1회

2단 Active 구간
→ 같은 대상 피해 1회

3단 Active 구간
→ 같은 대상 피해 1회
```

부모와 자식에 여러 Collider가 있어도 같은 `DamageRoot`를 공유하면 한 번만 피해를 받는다.

새로운 연속 공격 단계가 시작되면 중복 피해 기록을 초기화하여 다음 단계의 피해는 정상적으로 적용한다.

---

### 12. 단계별 다중 대상 공격

각 공격 단계의 `Maximum Targets` 값을 사용하여 한 번의 공격 단계에서 피해를 줄 수 있는 대상 수를 설정하였다.

기본 설정은 다음과 같다.

```text
도끼 1단
→ 1명

도끼 2단
→ 1명

도끼 3단
→ 최대 2명

곡괭이 모든 단계
→ 1명
```

다중 대상 공격에서도 각 대상은 같은 단계에서 한 번만 피해를 받는다.

---

### 13. CombatHitData 확장

`CombatHitData`에 다음 정보를 추가하였다.

| 항목 | 설명 |
| --- | --- |
| Attack Sequence Id | 각 공격 단계의 고유 번호 |
| Combo Step Index | 0부터 시작하는 연속 공격 단계 |
| Combo Step Number | 1부터 시작하는 표시용 단계 번호 |

피해 대상은 어떤 공격 단계에서 피해를 받았는지 확인할 수 있다.

기존 단일 공격 생성 방식도 유지하여 다른 코드의 호환성을 보존하였다.

---

### 14. ToolSwingAnimation 연속 공격 확장

`ToolSwingAnimation`을 공격 단계별 연출을 지원하도록 확장하였다.

기본 3단 회전 방향은 다음과 같다.

```text
1단
X 65 / Y 0 / Z -25

2단
X 65 / Y 0 / Z 25

3단
X 85 / Y 0 / Z -5
```

1단과 2단은 서로 반대 방향으로 휘두르고, 3단은 위에서 아래로 내려치는 형태로 구성하였다.

공격 단계의 `Animation Speed Multiplier`에 따라 휘두르기 속도도 달라진다.

진행 중인 공격이 취소되면 도구는 즉시 기본 회전값으로 복구된다.

---

### 15. TrainingDamageTarget 확장

훈련 표적에 연속 공격 테스트용 Runtime 정보를 추가하였다.

```text
Received Hit Count
Last Attack Sequence Id
Last Combo Step Number
```

이를 통해 다음 항목을 확인할 수 있다.

- 한 공격 단계에서 피해가 한 번만 적용되는지
- 여러 Collider로 인해 중복 피해가 발생하지 않는지
- 1단·2단·3단 번호가 정상적으로 전달되는지
- 전체 연속 공격의 피해 횟수가 단계 수와 일치하는지

훈련 표적 최대 체력은 연속 공격 테스트를 위해 100으로 확장하였다.

---

### 16. 공격 취소 처리

인벤토리, 전체 지도, Pause Menu 또는 건축 모드로 전환하면 진행 중인 공격을 즉시 취소하도록 구성하였다.

공격 취소 시 다음 상태가 정리된다.

- 실행 중인 공격 코루틴
- 현재 준비·타격·복귀 단계
- 저장된 다음 공격 입력
- 연속 공격 진행도
- 현재 단계 중복 피해 목록
- 도구 휘두르기 연출
- 도구 회전값

UI가 열린 뒤에도 공격 피해가 남아 발생하는 문제를 방지하였다.

---

### 17. 기존 채집 기능 유지

도끼와 곡괭이의 연속 공격 단계에서도 기존 채집 기능이 유지된다.

```text
Axe + 나무
→ 각 공격 단계 Active 구간에서 채집 1회

Pickaxe + 돌
→ 각 공격 단계 Active 구간에서 채집 1회
```

하나의 공격 단계에서 `Active Duration`이 여러 프레임 지속되어도 채집은 한 번만 실행된다.

잘못된 도구 사용 시 자원 획득은 계속 차단된다.

---

### 18. 1인칭·3인칭 공격 유지

1인칭과 3인칭 모두 기존 화면 중앙 조준 방식을 유지한다.

```text
Main Camera 화면 중앙
→ 조준점 계산

Player/WeaponAttackOrigin
→ 실제 SphereCast 시작
```

각 연속 공격 단계가 시작될 때 새로운 Camera 방향을 계산하여 1단과 2단 사이에 시점을 바꾸어도 다음 공격은 새로운 방향으로 실행된다.

---

## 생성 및 수정 파일

### 생성

```text
Assets/_ProjectU/Scripts/Combat/MeleeAttackPhase.cs
Assets/_ProjectU/Scripts/Combat/MeleeAttackStepData.cs
Assets/_ProjectU/Scripts/Combat/MeleeComboData.cs

Assets/_ProjectU/Data/Combat/MeleeCombo_Axe.asset
Assets/_ProjectU/Data/Combat/MeleeCombo_Pickaxe.asset
```

### 수정

```text
Assets/_ProjectU/Scripts/Combat/CombatHitData.cs
Assets/_ProjectU/Scripts/Combat/PlayerWeaponAttackController.cs
Assets/_ProjectU/Scripts/Combat/TrainingDamageTarget.cs
Assets/_ProjectU/Scripts/Items/ItemData.cs
Assets/_ProjectU/Scripts/Interaction/PlayerInteractor.cs
Assets/_ProjectU/Scripts/Tools/ToolSwingAnimation.cs

Assets/_ProjectU/Data/Items/ItemData_Axe.asset
Assets/_ProjectU/Data/Items/ItemData_Pickaxe.asset

Assets/_ProjectU/Scenes/20_Gameplay.unity
```

---

## 최종 공격 흐름

```text
좌클릭
→ 현재 Hotbar 아이템 확인
→ MeleeComboData 확인
→ 시작할 연속 공격 단계 결정
→ 단계별 스태미나 소비
→ Windup 시작
→ ToolSwingAnimation 재생
→ Active 시작
→ 공격 방향 고정
→ SphereCast 반복
→ 동일 대상 중복 피해 차단
→ Recovery 진행
→ 저장된 입력이 있으면 다음 단계
→ 입력이 없으면 Combo 진행도 임시 저장
→ Reset Delay가 지나면 1단으로 초기화
```

---

## 최종 조작법

| 입력 | 기능 |
| --- | --- |
| 좌클릭 | 근접 공격 시작 또는 다음 연속 공격 입력 저장 |
| F | 일반 상호작용 |
| 숫자 1~8 | Hotbar 아이템 선택 |
| V | 1인칭·3인칭 전환 |
| B | 건축 모드 진입·종료 |
| I | 인벤토리 |
| M | 전체 지도 |
| ESC | 현재 기능 종료 또는 Pause Menu |

---

## 확인 항목

- `MeleeAttackPhase` 생성
- `MeleeAttackStepData` 생성
- `MeleeComboData` 생성
- 도끼 3단 연속 공격 데이터 생성
- 곡괭이 3단 연속 공격 데이터 생성
- Axe와 Pickaxe ItemData에 Combo 연결
- 공격 단계가 Windup → Active → Recovery 순서로 진행
- 입력 저장 구간 이후 다음 공격 예약
- 한 단계당 다음 공격 입력 한 번만 저장
- 공격 종료 후 Reset Delay 안에 다음 단계 연결
- Reset Delay 이후 1단으로 초기화
- 아이템 변경 시 기존 Combo 연결 차단
- 단계별 피해량 배율 적용
- 단계별 공격 거리와 반지름 배율 적용
- 단계별 스태미나 비용 적용
- 단계별 충격량 배율 전달
- 단계별 휘두르기 방향 변경
- 단계별 휘두르기 속도 변경
- 같은 단계의 동일 대상 중복 피해 차단
- 여러 Collider를 가진 동일 대상 중복 피해 차단
- 도끼 3단 최대 두 대상 피해 확인
- 공격 중 인벤토리 진입 시 공격 취소
- 공격 중 전체 지도 진입 시 공격 취소
- 공격 중 Pause Menu 진입 시 공격 취소
- 공격 중 건축 모드 진입 시 공격 취소
- 공격 취소 후 도구 회전 기본값 복구
- 나무·돌 채집 기능 유지
- 한 공격 단계에서 채집 한 번만 실행
- F 일반 상호작용 유지
- 1인칭 연속 공격 확인
- 3인칭 연속 공격 확인
- Console 컴파일 오류와 Missing Reference 없음

---

## 61일차 결과

근접 공격을 준비, 타격, 복귀 단계로 분리하고 아이템별 3단 연속 공격을 설정할 수 있는 구조를 완성하였다.

공격 단계마다 피해량, 범위, 스태미나 비용, 충격량, 최대 대상 수와 연출 속도를 개별적으로 조절할 수 있다.

타격 유효 시간 동안 공격 범위를 반복 검사하면서도 같은 대상은 한 공격 단계에서 한 번만 피해를 받도록 중복 피해 방지 구조를 적용하였다.

도끼와 곡괭이는 기존 채집 기능을 유지하면서 서로 다른 속도와 위력을 가진 3단 연속 공격을 사용할 수 있게 되었다.

UI와 건축 모드 전환 시 진행 중인 공격과 연속 공격 상태를 안전하게 정리하도록 구성하여 이후 원거리 무기, 회피, 방어력, 넉백과 몬스터 전투 시스템을 연결할 수 있는 근접 전투 기반을 마련하였다.
