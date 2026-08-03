# Project U 개발 일지

## 70일차 : 실제 게임 데이터 기반 Visual Identity 자동 동기화 구현

### 1. 개발 목표

69일차에는 `ContentVisualIdentity`에 콘텐츠 종류와 콘텐츠 ID를 직접 입력하고, 해당 ID를 바탕으로 Visual Profile을 검색하도록 구현하였다.

70일차에는 이 수동 입력 단계를 줄이기 위해 실제 게임 기능이 이미 가지고 있는 데이터에서 콘텐츠 ID를 자동으로 읽어 `ContentVisualIdentity`와 Visual Profile을 동기화하도록 확장하였다.

이번 작업의 핵심 목표는 다음과 같다.

- `ItemData`, `EnemyCombatData`, `BuildRecipeData`에서 콘텐츠 ID 자동 획득
- 실제 데이터에 맞는 `ContentVisualIdentityCategory` 자동 결정
- Identity와 Visual Profile 자동 동기화
- 생성 직후 또는 저장 복원 후 변경된 데이터 재확인
- 실행 중 데이터가 변경될 경우 외형 자동 갱신
- 적·월드 아이템·건축물에 실제 데이터 연결 방식 적용
- 기존 기능 스크립트를 직접 수정하지 않고 공통 연결 계층으로 구현

---

### 2. 최종 연결 구조

```text
실제 게임 데이터
├─ WorldItemPickup.ItemData
├─ EnemyHealth.CombatData
└─ PlacedBuildObject.RecipeData
        ↓
ContentVisualDataSourceBinder
        ↓
ContentVisualIdentity
        ↓
ContentVisualProfileBinder
        ↓
GameDataRegistry
        ↓
ContentVisualProfile
        ↓
ContentVisualRoot
        ↓
VisualInstance
```

적용 예시:

```text
EnemyHealth.CombatData
→ enemy_basic
→ visual_enemy_basic
→ VP_Enemy_Basic
```

```text
WorldItemPickup.ItemData
→ item_wood
→ visual_item_wood
→ VP_Item_Wood
```

```text
PlacedBuildObject.RecipeData
→ structure_wood_floor
→ visual_buildable_wood_floor
→ VP_Buildable_WoodFloor
```

---

### 3. ContentVisualDataSourceBinder 구현

실제 게임 데이터에서 콘텐츠 ID를 읽어 Visual Identity를 자동으로 구성하는 `ContentVisualDataSourceBinder`를 추가하였다.

지원하는 데이터 원본:

| Source Type | 실제 데이터 원본 |
|---|---|
| Auto | 현재 Root에서 유효한 원본 하나를 자동 탐색 |
| Item Pickup | `WorldItemPickup.ItemData` |
| Enemy | `EnemyHealth.CombatData` |
| Build Object | `PlacedBuildObject.RecipeData` 또는 BuildRecipeData Override |

주요 기능:

- 같은 Root의 Visual 관련 컴포넌트 자동 검색
- 실제 게임 데이터 컴포넌트 자동 검색
- 콘텐츠 ID 자동 추출
- Identity 분류 자동 결정
- Identity 자동 설정
- Visual Profile ID 자동 계산
- Profile 변경 필요 여부 확인
- 변경된 경우에만 Visual Profile 재적용
- 실행 상태 Inspector 표시
- 실제 데이터부터 Visual Profile까지 전체 연결 검증

---

### 4. 실행 순서 설정

`ContentVisualDataSourceBinder`는 `ContentVisualProfileBinder`보다 먼저 실행되도록 설정하였다.

```text
ContentVisualDataSourceBinder
Execution Order: -600

ContentVisualProfileBinder
Execution Order: -500
```

실행 흐름:

```text
ContentVisualDataSourceBinder.Awake
→ 실제 데이터에서 콘텐츠 ID 확인
→ ContentVisualIdentity 갱신

ContentVisualProfileBinder.Awake
→ 갱신된 Identity에서 Profile ID 확인
→ 캐시 또는 Registry Profile 적용
```

이를 통해 Prefab에 저장된 실제 데이터가 있다면 외형 적용 전에 올바른 Identity를 준비할 수 있다.

---

### 5. 동기화 시점 구현

#### Awake 동기화

Prefab에 직렬화된 데이터 참조를 읽어 Binder보다 먼저 Identity를 준비한다.

```text
Awake
→ 데이터 원본 확인
→ Identity 갱신
→ Profile Binder 실행 준비
```

#### Start 동기화

오브젝트 생성 후 `Initialize()` 또는 `RestoreFromSave()`에서 변경된 실제 데이터를 다시 확인한다.

```text
Instantiate
→ Awake
→ Initialize 또는 RestoreFromSave
→ Start
→ 최종 데이터 동기화
```

#### 실행 중 변경 감시

실행 중 데이터가 변경되는 재사용 Prefab을 지원하기 위해 일정 간격으로 실제 데이터 변화를 확인한다.

기본 설정:

```text
Monitor Runtime Source Changes: On
Runtime Check Interval: 0.25
Apply Visual When Source Changes: On
```

다음 조건 중 하나가 발생할 때만 외형을 다시 적용한다.

- Identity 분류 변경
- 콘텐츠 ID 변경
- 캐시된 Visual Profile 누락
- 캐시 Profile ID 불일치
- 마지막 Profile 적용 실패

동일한 데이터와 Profile이 유지되는 동안에는 외형을 반복 생성하지 않는다.

---

### 6. 아이템 분류 자동 결정

`ItemData`의 분류 값을 확인하여 일반 아이템과 무기·도구를 구분하도록 구현하였다.

```text
Is Tool: False
Is Weapon: False
→ ContentVisualIdentityCategory.Item
```

```text
Is Tool: True
또는
Is Weapon: True
→ ContentVisualIdentityCategory.Weapon
```

예시:

```text
일반 아이템
item_wood
→ visual_item_wood
```

```text
도구 또는 무기
item_stone_axe
→ visual_weapon_stone_axe
```

---

### 7. Optional Data Override 구현

일부 Prefab은 실행 전에는 실제 게임 데이터 컴포넌트가 없거나 데이터가 초기화되지 않을 수 있다.

이를 처리하기 위해 다음 예외 데이터 참조를 추가하였다.

```text
Item Data Override
Enemy Combat Data Override
Build Recipe Data Override
```

우선순위:

```text
실행 중 실제 데이터
→ Override 데이터
```

예를 들어 건축물은 Prefab Mode에서 `PlacedBuildObject`가 없을 수 있으므로 `BuildRecipeData Override`를 사용하고, 실제 설치 후에는 `PlacedBuildObject.RecipeData`를 우선 사용한다.

---

### 8. Editor 도구 구현

`ContentVisualDataSourceBinderEditor`를 추가하여 Inspector와 상단 메뉴에서 데이터 연결을 관리할 수 있도록 구성하였다.

Inspector 버튼:

```text
Refresh Identity From Data Source
Resolve Profile From Registry And Apply
Validate Data Source Visual Link
```

Unity 상단 메뉴:

```text
Project U
→ Visual
→ Add Data Source Binder To Selected Roots

Project U
→ Visual
→ Resolve And Apply Selected Data Source Visuals
```

Editor 도구는 다음 작업을 수행한다.

```text
실제 데이터 확인
→ Identity 갱신
→ Visual Profile ID 계산
→ GameDataRegistry 검색
→ Profile 캐시
→ Visual 적용
→ Prefab 또는 Scene 변경 저장
```

---

### 9. 기본 적 Prefab 적용

대상 Prefab:

```text
Assets/_ProjectU/Prefabs/Development/Placeholder/
PF_Temp_Enemy_Basic.prefab
```

데이터 원본:

```text
EnemyHealth.CombatData
→ EnemyCombatData_Basic
```

동기화 결과:

```text
Resolved Source: Enemy
Resolved Data Asset Name: EnemyCombatData_Basic
Resolved Content Id: enemy_basic
Resolved Visual Profile Id: visual_enemy_basic
Last Synchronization Succeeded: On
```

최종 연결:

```text
EnemyCombatData_Basic
→ enemy_basic
→ visual_enemy_basic
→ VP_Enemy_Basic
```

기존 기능 유지:

- NavMesh 이동
- 플레이어 추적
- 공격 예고
- 공격 처리
- 피격과 밀림
- 체력 처리
- 적 사망
- Root CapsuleCollider

생성된 `TEMP_Capsule_Visual`에는 MeshFilter와 MeshRenderer만 존재하며 Visual 내부 Collider는 포함되지 않는다.

---

### 10. WoodPickup 적용

대상 Prefab:

```text
Assets/_ProjectU/Prefabs/Items/WoodPickup.prefab
```

데이터 원본:

```text
WorldItemPickup.ItemData
→ ItemData_Wood
```

#### ItemData ID 정리

초기에는 `ItemData_Wood`의 ID가 다음과 같이 설정되어 있었다.

```text
resource_wood
```

현재 Visual Profile 규칙과 일치하도록 다음과 같이 변경하였다.

```text
item_wood
```

변경 후 자동 연결:

```text
ItemData_Wood
→ item_wood
→ visual_item_wood
→ VP_Item_Wood
```

동기화 결과:

```text
Resolved Source: Item Pickup
Resolved Data Asset Name: ItemData_Wood
Resolved Content Id: item_wood
Resolved Visual Profile Id: visual_item_wood
Last Synchronization Succeeded: On
```

기존 기능 유지:

- F 상호작용
- 목재 획득
- 인벤토리 추가
- Rigidbody
- BoxCollider
- WorldObjectIdentity
- 저장 상태 관리

---

### 11. WoodFloorPlaced 적용

대상 Prefab:

```text
Assets/_ProjectU/Prefabs/Building/WoodFloorPlaced.prefab
```

Prefab Mode에서는 다음 Override를 사용한다.

```text
Build Recipe Data Override
→ BuildRecipe_WoodFloor
```

실제 건축 후에는 다음 데이터가 우선 적용된다.

```text
PlacedBuildObject.RecipeData
```

동기화 결과:

```text
Resolved Source: Build Object
Resolved Data Asset Name: BuildRecipe_WoodFloor
Resolved Content Id: structure_wood_floor
Resolved Visual Profile Id: visual_buildable_wood_floor
Last Synchronization Succeeded: On
```

최종 연결:

```text
BuildRecipe_WoodFloor
→ structure_wood_floor
→ visual_buildable_wood_floor
→ VP_Buildable_WoodFloor
```

생성 외형:

```text
Position: 0, 0.05, 0
Scale: 1, 0.1, 1
Collider: 없음
```

기존 기능 유지:

- 건축 Preview
- 건축 배치
- 회전
- 연결 지점
- 재료 소모
- 건축 Collider
- 저장과 불러오기

---

### 12. Registry 갱신

`ItemData_Wood`의 ID가 변경되었으므로 `GameDataRegistry`를 다시 갱신하였다.

변경 내용:

```text
resource_wood 제거
item_wood 등록
```

Registry의 ItemData 배열도 ID 정렬 기준에 맞게 다시 정렬되었다.

Asset GUID 참조는 유지되므로 `ItemData_Wood`를 직접 참조하는 기존 Prefab과 제작 데이터의 연결은 유지된다.

---

### 13. 신규 스크립트

```text
Assets/_ProjectU/Scripts/Visual/
└─ ContentVisualDataSourceBinder.cs

Assets/_ProjectU/Scripts/Visual/Editor/
└─ ContentVisualDataSourceBinderEditor.cs
```

기존 전투·아이템·건축 기능 스크립트는 직접 수정하지 않았다.

```text
WorldItemPickup.cs
EnemyHealth.cs
PlacedBuildObject.cs
BuildPlacementController.cs
```

각 스크립트가 이미 공개하고 있는 실제 데이터 참조를 공통 Binder가 읽는 방식으로 구현하였다.

---

### 14. 수정된 Asset 및 Prefab

```text
Assets/_ProjectU/Data/Items/ItemData_Wood.asset
Assets/_ProjectU/Data/Registry/GameDataRegistry.asset

Assets/_ProjectU/Prefabs/Development/Placeholder/
PF_Temp_Enemy_Basic.prefab

Assets/_ProjectU/Prefabs/Items/
WoodPickup.prefab

Assets/_ProjectU/Prefabs/Building/
WoodFloorPlaced.prefab
```

---

### 15. 최종 연결 상태

#### 기본 적

```text
EnemyCombatData_Basic
→ enemy_basic
→ visual_enemy_basic
→ VP_Enemy_Basic

Last Synchronization Succeeded: On
```

#### WoodPickup

```text
ItemData_Wood
→ item_wood
→ visual_item_wood
→ VP_Item_Wood

Last Synchronization Succeeded: On
```

#### WoodFloorPlaced

```text
BuildRecipe_WoodFloor
→ structure_wood_floor
→ visual_buildable_wood_floor
→ VP_Buildable_WoodFloor

Last Synchronization Succeeded: On
```

세 테스트 대상 모두 실제 데이터에서 콘텐츠 ID를 읽어 Visual Identity와 Profile을 정상적으로 동기화한다.

---

### 16. 테스트 항목

#### Unity Console

```text
Window
→ General
→ Console
```

완료 기준:

```text
빨간색 컴파일 오류 0개
```

#### Prefab 검증

다음 세 Prefab에서 실행한다.

```text
Refresh Identity From Data Source
Resolve Profile From Registry And Apply
Validate Data Source Visual Link
```

대상:

```text
PF_Temp_Enemy_Basic
WoodPickup
WoodFloorPlaced
```

#### Play Mode

다음 Scene에서 실행한다.

```text
Assets/_ProjectU/Scenes/00_Bootstrap.unity
```

확인 항목:

- 기본 적 데이터와 Visual Profile 자동 연결
- 적 추적·공격·피격·사망 정상
- WoodPickup 데이터와 Visual Profile 자동 연결
- F 입력을 통한 목재 획득 정상
- 인벤토리 목재 추가 정상
- WoodFloor 데이터와 Visual Profile 자동 연결
- 건축 배치·회전·연결 정상
- 저장과 불러오기 정상
- Visual 내부 Collider 없음
- `resource_wood` 관련 오류 없음
- Data Source Visual 동기화 오류 없음

---

### 17. 작업 결과

70일차 작업을 통해 Visual 시스템이 Prefab에 수동으로 입력된 문자열에만 의존하지 않고, 실제 게임 데이터의 ID를 기준으로 자동 동작할 수 있게 되었다.

기존 방식:

```text
Prefab의 ContentVisualIdentity에
Category와 Content Id 직접 입력
```

변경된 방식:

```text
실제 게임 데이터 참조
→ 콘텐츠 ID 자동 추출
→ Identity 자동 설정
→ Visual Profile 자동 검색 및 적용
```

이를 통해 다음과 같은 효과를 얻었다.

- 데이터와 외형 ID 불일치 감소
- Prefab 수동 설정 작업 감소
- 생성 및 저장 복원 과정 대응
- 공통 Prefab 재사용 가능성 향상
- 실제 데이터 변경 시 외형 자동 갱신
- 적·아이템·건축물의 공통 Visual 연결 방식 통일

---

### 18. 다음 개발 방향

다음 일차에는 현재 세 테스트 대상에만 적용된 실제 데이터 기반 Visual 연결 구조를 프로젝트 전체 콘텐츠 검사 도구로 확장한다.

우선순위:

1. 전체 ItemData와 Visual Profile 연결 누락 검사
2. 전체 EnemyCombatData와 Visual Profile 연결 누락 검사
3. 전체 BuildRecipeData와 Visual Profile 연결 누락 검사
4. ID는 존재하지만 Profile Asset이 없는 콘텐츠 목록 출력
5. Profile은 존재하지만 실제 콘텐츠 데이터가 없는 고아 Profile 검사
6. Stone Axe를 이용한 Weapon 분류 자동 연결 테스트
7. 다른 건축물과 월드 아이템에 Data Source Binder 순차 적용
