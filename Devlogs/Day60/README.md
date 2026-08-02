# 프로젝트 U 개발 일지

## 60일차 : 무기 공통 데이터와 기본 공격 구조 구현

### 개발 목표

기존 도구 채집 공격을 전투 시스템으로 확장할 수 있도록 공통 무기 데이터와 기본 공격 실행 구조를 구현한다.

도끼와 곡괭이는 기존 채집 기능을 유지하면서 전투 대상에게 피해를 줄 수 있도록 구성하고, 이후 근접 무기·원거리 무기·몬스터 AI·공통 피해 처리 시스템을 연결할 수 있는 기반을 만든다.

---

## 구현 내용

### 1. 무기 공격 방식 추가

`WeaponAttackType`을 생성하여 아이템의 공격 방식을 구분하였다.

```text
None
Melee
Ranged
```

60일차에는 `Melee` 공격을 구현하였다. `Ranged`는 이후 원거리 무기와 발사체 시스템에서 사용한다.

### 2. Weapon 아이템 분류 추가

기존 `ItemCategory`의 마지막 값에 `Weapon`을 추가하였다.

```text
CraftingMaterial = 0
Tool = 1
Food = 2
Equipment = 3
Drink = 4
Medicine = 5
Weapon = 6
```

기존 아이템의 직렬화 번호를 유지하기 위해 중간 번호를 변경하지 않았다.

도끼와 곡괭이는 채집 기능을 유지해야 하므로 `Tool` 분류를 계속 사용한다.

### 3. ItemData 전투 능력치 확장

`ItemData`에 다음 전투 데이터를 추가하였다.

| 항목 | 설명 |
| --- | --- |
| Weapon Attack Type | 공격 방식 |
| Base Damage | 기본 피해량 |
| Attack Cooldown | 공격 간 최소 대기시간 |
| Attack Range | 근접 공격 거리 |
| Attack Radius | SphereCast 반지름 |
| Stamina Cost | 공격 스태미나 비용 |
| Impact Force | 이후 넉백에 사용할 충격량 |

`Tool`과 `Weapon` 분류만 공격 데이터를 사용할 수 있도록 구성하였다.

### 4. 공통 전투 피해 데이터 구현

`CombatHitData`를 생성하여 다음 정보를 피해 대상에게 전달하도록 구성하였다.

- 공격자
- 사용 아이템
- 공격 방식
- 피해량
- 충격량
- 충돌 지점
- 공격 방향
- 실제 충돌 Collider

이 데이터는 이후 방어력, 넉백, 피격 효과와 상태 효과에 공통으로 사용한다.

### 5. 공통 피해 수신 규칙 구현

`ICombatDamageReceiver`를 생성하였다.

전투 피해를 받을 수 있는 대상은 다음 기능을 제공한다.

- 피해 대상 기준 Transform
- 생존 여부
- `CombatHitData` 피해 수신

훈련 표적과 이후 몬스터 체력 시스템이 같은 구조를 사용하도록 구성하였다.

### 6. 플레이어 공통 무기 공격 관리자 구현

`PlayerWeaponAttackController`를 생성하였다.

좌클릭 공격은 다음 순서로 처리된다.

1. 현재 Hotbar 아이템 확인
2. 아이템 공격 가능 여부 확인
3. 공격 재사용 대기시간 확인
4. 스태미나 소비 가능 여부 확인
5. 스태미나 소비
6. 도구 휘두르기 연출 실행
7. 채집 자원이면 기존 채집 실행
8. 전투 대상이면 근접 공격 판정
9. 가장 가까운 대상에게 피해 전달

### 7. 1인칭·3인칭 공통 공격 방향

공격 방향은 Main Camera의 화면 중앙을 기준으로 계산한다.

실제 공격 판정은 Player 아래의 `WeaponAttackOrigin`에서 시작한다.

따라서 1인칭과 3인칭 모두 같은 조준 방식을 사용하면서 공격이 Camera 위치에서 발생하지 않도록 구성하였다.

### 8. 근접 공격 판정 구현

근접 공격에는 `SphereCastNonAlloc`을 사용하였다.

공격 범위 안에서 다음 조건을 만족하는 가장 가까운 대상 하나를 선택한다.

- `Damageable` 레이어
- `ICombatDamageReceiver` 구현
- 생존 상태
- Player 자신의 계층이 아님

여러 Collider가 있어도 한 번의 공격으로 한 번만 피해를 받도록 구성하였다.

### 9. 맨손 공격 구현

공격 가능한 아이템을 선택하지 않았을 때 사용할 맨손 공격을 추가하였다.

| 항목 | 값 |
| --- | --- |
| Damage | 2 |
| Cooldown | 0.65초 |
| Range | 1.4 |
| Radius | 0.25 |
| Stamina Cost | 0 |
| Impact Force | 1 |

`Allow Unarmed Attack`을 해제하면 맨손 공격을 비활성화할 수 있다.

### 10. 공격 스태미나 소비 기능 추가

기존 `PlayerStamina`에 다음 기능을 추가하였다.

```text
CanConsume
TryConsume
```

공격 전에 필요한 스태미나를 검사하고, 부족하면 공격 연출·피해·추가 소비를 모두 차단한다.

공격 비용에는 현재 체온에 따른 스태미나 소비 배율이 적용된다.

### 11. PlayerInteractor 역할 분리

`PlayerInteractor`는 좌클릭과 F키 입력을 확인하고 실제 공격은 `PlayerWeaponAttackController`에 전달하도록 수정하였다.

수정 후 역할은 다음과 같다.

- 좌클릭 공격 입력 확인
- F키 일반 상호작용 입력 확인
- 전방 `InteractableBase` 탐지
- 공격 입력 전달
- 일반 상호작용 실행

무기 데이터, 쿨타임, 스태미나와 피해 판정은 `PlayerWeaponAttackController`가 담당한다.

### 12. 기존 채집 기능 유지

기존 나무와 돌 채집 흐름을 유지하였다.

```text
Axe + 나무
→ Wood 획득

Pickaxe + 돌
→ Stone 획득
```

잘못된 도구를 사용하면 휘두르기는 실행되지만 자원은 획득하지 못한다.

F키는 계속 월드 아이템 획득과 일반 상호작용에 사용한다.

### 13. 도끼 전투 데이터

| 항목 | 값 |
| --- | --- |
| Item Category | Tool |
| Tool Type | Axe |
| Weapon Attack Type | Melee |
| Base Damage | 12 |
| Attack Cooldown | 0.55 |
| Attack Range | 2 |
| Attack Radius | 0.45 |
| Stamina Cost | 8 |
| Impact Force | 3 |
| Maximum Stack | 1 |

### 14. 곡괭이 전투 데이터

| 항목 | 값 |
| --- | --- |
| Item Category | Tool |
| Tool Type | Pickaxe |
| Weapon Attack Type | Melee |
| Base Damage | 10 |
| Attack Cooldown | 0.65 |
| Attack Range | 1.9 |
| Attack Radius | 0.4 |
| Stamina Cost | 9 |
| Impact Force | 4 |
| Maximum Stack | 1 |

### 15. 전투 훈련 표적 구현

`TrainingDamageTarget`을 생성하여 몬스터 AI 없이도 전투 기반을 확인할 수 있도록 구성하였다.

기본 설정은 다음과 같다.

| 항목 | 값 |
| --- | --- |
| Maximum Health | 50 |
| Reset After Defeat | 활성화 |
| Reset Delay | 2초 |
| Log Damage | 활성화 |

훈련 표적을 통해 피해량, 재사용 대기시간, 스태미나 소비와 체력 복구를 확인하였다.

### 16. Damageable 레이어 추가

전투 피해를 받을 대상만 구분하기 위해 `Damageable` 레이어를 추가하였다.

다음 레이어는 피해 대상에서 제외한다.

- Player
- Terrain
- Ground
- Building
- UI
- Ignore Raycast
- Preview

### 17. 건축 모드와 UI 입력 차단 유지

건축 모드에서는 `PlayerInteractor`가 비활성화되므로 좌클릭은 건축물 배치에만 사용된다.

다음 UI가 열린 상태에서도 공격이 실행되지 않는다.

- 인벤토리
- 보관함
- 전체 지도
- Pause Menu

---

## 생성 및 수정 파일

### 생성

```text
Assets/_ProjectU/Scripts/Combat/WeaponAttackType.cs
Assets/_ProjectU/Scripts/Combat/CombatHitData.cs
Assets/_ProjectU/Scripts/Combat/ICombatDamageReceiver.cs
Assets/_ProjectU/Scripts/Combat/TrainingDamageTarget.cs
Assets/_ProjectU/Scripts/Combat/PlayerWeaponAttackController.cs
```

### 수정

```text
Assets/_ProjectU/Scripts/Items/ItemCategory.cs
Assets/_ProjectU/Scripts/Items/ItemData.cs
Assets/_ProjectU/Scripts/Player/PlayerStamina.cs
Assets/_ProjectU/Scripts/Interaction/PlayerInteractor.cs
Assets/_ProjectU/Data/Items/ItemData_Axe.asset
Assets/_ProjectU/Data/Items/ItemData_Pickaxe.asset
Assets/_ProjectU/Scenes/20_Gameplay.unity
ProjectSettings/TagManager.asset
```

---

## Scene 구성

```text
20_Gameplay

├─ Player
│  ├─ FirstPersonCameraAnchor
│  ├─ WeaponAttackOrigin
│  ├─ PlayerVisual
│  ├─ PlayerInventory
│  ├─ PlayerStamina
│  ├─ PlayerInteractor
│  ├─ PlayerWeaponAttackController
│  ├─ BuildPlacementController
│  └─ BuildModeCameraController
│
├─ Main Camera
│  ├─ Camera
│  ├─ ThirdPersonCameraFollow
│  └─ AudioListener
│
├─ CombatTrainingTarget
│  ├─ MeshRenderer
│  ├─ BoxCollider
│  └─ TrainingDamageTarget
│
└─ === UIManagers ===
   ├─ GameUIManager
   ├─ GameplayInputLock
   ├─ WorldMapController
   └─ PauseMenuController
```

---

## 최종 조작법

| 입력 | 기능 |
| --- | --- |
| 좌클릭 | 선택 아이템 공격 또는 자원 채집 |
| F | 월드 아이템 획득과 일반 상호작용 |
| 숫자 1~8 | Hotbar 슬롯 선택 |
| V | 1인칭·3인칭 전환 |
| B | 건축 모드 진입·종료 |
| I | 인벤토리 |
| M | 전체 지도 |
| ESC | 현재 팝업 종료 또는 Pause Menu |

---

## 확인 항목

- Combat 폴더와 신규 스크립트 생성
- ItemData 전투 능력치 표시
- Weapon 분류 추가 후 기존 아이템 분류 유지
- Player에 `PlayerWeaponAttackController` 추가
- `WeaponAttackOrigin` 연결
- Main Camera를 `View Transform`에 연결
- `Damageable` 레이어 생성
- 훈련 표적에 `TrainingDamageTarget` 추가
- 맨손 공격 피해 확인
- Axe 공격 피해 12 확인
- Pickaxe 공격 피해 10 확인
- 공격 재사용 대기시간 확인
- 공격 시 스태미나 소비 확인
- 스태미나 부족 시 공격 차단
- 한 번의 공격으로 한 대상에게 한 번만 피해 적용
- 1인칭 공격 방향 확인
- 3인칭 공격 방향 확인
- Axe 나무 채집 유지
- Pickaxe 돌 채집 유지
- 잘못된 도구의 자원 획득 차단
- F키 일반 상호작용 유지
- 건축 모드 공격 차단
- 인벤토리·지도·Pause Menu 중 공격 차단
- Console 컴파일 오류와 Missing Reference 없음

---

## 60일차 결과

기존 채집 도구를 전투에서도 사용할 수 있는 공통 무기 공격 기반을 완성하였다.

아이템별로 피해량, 공격 간격, 범위, 스태미나 비용과 충격량을 설정할 수 있으며, 좌클릭 공격은 현재 Hotbar 아이템 데이터를 기준으로 실행된다.

1인칭과 3인칭 모두 화면 중앙을 기준으로 공격 방향을 계산하고 실제 공격은 플레이어 위치의 `WeaponAttackOrigin`에서 시작하도록 구성하였다.

공격 결과는 공통 피해 데이터와 피해 수신 인터페이스로 전달되므로 이후 몬스터 체력, 방어력, 넉백, 피격 효과와 상태 효과 시스템을 같은 구조에 연결할 수 있다.
