# Project U 개발 일지

## 69일차 : 콘텐츠 ID 기반 Visual Profile 자동 연결 구현

### 1. 개발 목표

68일차에 구현한 `ContentVisualProfile` 구조를 확장하여, 각 Prefab에 Visual Profile을 직접 지정하거나 Visual Profile ID를 별도로 입력하지 않아도 콘텐츠 ID를 기준으로 알맞은 Visual Profile을 자동으로 찾고 적용할 수 있도록 구현하였다.

이번 작업의 핵심 목표는 다음과 같다.

- 콘텐츠 종류와 콘텐츠 ID를 공통 형식으로 관리
- 콘텐츠 ID에서 Visual Profile ID를 자동 계산
- 계산된 ID를 이용해 `GameDataRegistry`에서 Profile 검색
- 검색된 Profile을 `ContentVisualProfileBinder`에 캐시
- 적·아이템·건축물에 자동 연결 방식 실제 적용
- 잘못되거나 비어 있는 Identity가 자동 적용되지 않도록 보호
- 기존 직접 Profile 참조 방식과의 호환 유지

---

### 2. 최종 연결 구조

```text
콘텐츠 데이터 ID
→ ContentVisualIdentity
→ ContentVisualProfileIdUtility
→ ContentVisualProfileBinder
→ GameDataRegistry
→ ContentVisualProfile
→ ContentVisualRoot
→ VisualInstance
```

예시:

```text
enemy_basic
→ visual_enemy_basic
→ VP_Enemy_Basic

item_wood
→ visual_item_wood
→ VP_Item_Wood

structure_wood_floor
→ visual_buildable_wood_floor
→ VP_Buildable_WoodFloor
```

---

### 3. ContentVisualIdentity 구현

기능 Root가 나타내는 콘텐츠 종류와 콘텐츠 ID를 저장하는 `ContentVisualIdentity`를 추가하였다.

지원하는 콘텐츠 분류:

- Item
- Weapon
- Enemy
- Buildable
- Resource
- Other

주요 기능:

- 콘텐츠 종류 저장
- 원본 콘텐츠 ID 저장
- Visual Profile ID 자동 계산
- 명시적 Visual Profile ID 예외 지정
- 계산 결과 Inspector 미리보기
- Identity 설정 검증
- Editor 또는 생성 시스템에서 사용할 설정 메서드 제공

`Other` 분류처럼 자동 변환 규칙을 적용할 수 없는 콘텐츠는 명시적 Visual Profile ID를 사용할 수 있다.

---

### 4. Visual Profile ID 변환 규칙

`ContentVisualProfileIdUtility`를 추가하여 콘텐츠 ID를 표준 Visual Profile ID로 변환하도록 구현하였다.

| 콘텐츠 종류 | 원본 콘텐츠 ID | Visual Profile ID |
|---|---|---|
| Item | `item_wood` | `visual_item_wood` |
| Weapon | `item_stone_axe` | `visual_weapon_stone_axe` |
| Enemy | `enemy_basic` | `visual_enemy_basic` |
| Buildable | `structure_wood_floor` | `visual_buildable_wood_floor` |
| Resource | `resource_tree` | `visual_resource_tree` |

무기와 자원은 일반 `ItemData` ID를 사용하는 경우도 처리할 수 있도록 구성하였다.

```text
item_stone_axe
→ visual_weapon_stone_axe

item_wood_log
→ visual_resource_wood_log
```

변환 과정에서는 다음 항목을 검사한다.

- 콘텐츠 ID 공통 형식
- 콘텐츠 종류별 필수 접두사
- 고유 이름 부분 존재 여부
- 계산된 Visual Profile ID 형식
- 명시적 Profile ID의 `visual_` 접두사

---

### 5. ContentVisualProfileBinder 확장

기존 Binder에 `ContentVisualIdentity` 기반 검색 방식을 추가하였다.

지원하는 Profile 연결 방식:

```text
1. ContentVisualProfile 직접 참조
2. Visual Profile ID 직접 입력 후 Registry 검색
3. ContentVisualIdentity에서 ID를 계산하여 Registry 검색
```

Identity 방식이 활성화되면 다음 순서로 동작한다.

```text
ContentVisualIdentity 확인
→ Visual Profile ID 계산
→ 캐시된 Profile ID 비교
→ 필요 시 GameDataRegistryRuntime 검색
→ ContentVisualRoot에 Profile 적용
```

추가된 주요 기능:

- `Resolve From Content Identity`
- `ApplyProfileFromContentIdentity()`
- `TryGetRequestedProfileId()`
- `CacheResolvedProfile()`
- `SetContentIdentityMode()`
- Identity 계산 ID와 캐시 Profile ID 일치 검사
- Runtime Registry가 없을 때 캐시 Profile 사용
- 기존 직접 참조 방식 유지

---

### 6. Editor 도구 구현

#### ContentVisualIdentityEditor

Identity Inspector에서 다음 정보를 확인할 수 있다.

```text
Resolved Visual Profile ID
Identity 검증 결과
```

제공 버튼:

```text
Refresh Resolved Profile ID
Validate Content Visual Identity
```

#### ContentVisualProfileBinderEditor

Binder Inspector에 다음 기능을 추가하였다.

```text
Resolve Identity Profile From Registry
Resolve Identity Profile And Apply
Apply Assigned Profile
Validate Assigned Profile
```

상단 메뉴:

```text
Project U
→ Visual
→ Add Content Identity To Selected Roots

Project U
→ Visual
→ Resolve And Apply Identity Profiles To Selected Roots

Project U
→ Visual
→ Apply Profiles To Selected Roots
```

---

### 7. 빈 Identity 자동 적용 방지

선택 Root에 Identity 구조를 일괄 추가할 때, 새로 생성된 Identity의 콘텐츠 ID가 비어 있어도 Binder의 Identity 모드가 즉시 활성화되는 문제가 있었다.

다음 검사를 추가하여 수정하였다.

```text
Identity ID 계산
→ 유효한 경우에만 Identity 모드 활성화
→ 유효하지 않으면 Identity 모드 비활성화
→ Category와 Content Id 설정 필요 경고 출력
```

따라서 Profile이 준비되지 않은 Prefab에 메뉴를 잘못 실행해도 빈 ID로 자동 검색을 시도하지 않는다.

---

### 8. 적 Prefab 적용

대상 Prefab:

```text
Assets/_ProjectU/Prefabs/Development/Placeholder/
PF_Temp_Enemy_Basic.prefab
```

Identity 설정:

```text
Category: Enemy
Content Id: enemy_basic
Resolved Visual Profile Id: visual_enemy_basic
```

Binder 설정:

```text
Resolve From Content Identity: On
Resolve From Registry By Id: Off
Visual Profile: VP_Enemy_Basic
Visual Profile Id: visual_enemy_basic
Applied Profile Id: visual_enemy_basic
Last Apply Succeeded: On
```

자동 연결 결과:

```text
enemy_basic
→ visual_enemy_basic
→ VP_Enemy_Basic
```

기존 전투 기능은 유지하였다.

- EnemyHealth
- EnemyCombatController
- EnemyNavMeshMovement
- NavMeshAgent
- EnemyCombatImpactMotor
- 공격 예고
- 피격 밀림
- 적 사망

---

### 9. WoodPickup 적용

대상 Prefab:

```text
Assets/_ProjectU/Prefabs/Items/WoodPickup.prefab
```

Identity 설정:

```text
Category: Item
Content Id: item_wood
Resolved Visual Profile Id: visual_item_wood
```

Binder 설정:

```text
Resolve From Content Identity: On
Resolve From Registry By Id: Off
Visual Profile: VP_Item_Wood
Visual Profile Id: visual_item_wood
Applied Profile Id: visual_item_wood
Last Apply Succeeded: On
```

자동 연결 결과:

```text
item_wood
→ visual_item_wood
→ VP_Item_Wood
```

기존 기능은 유지하였다.

- F 상호작용
- 아이템 획득
- 인벤토리 추가
- Rigidbody
- BoxCollider
- WorldObjectIdentity
- 저장 상태 관리

---

### 10. WoodFloorPlaced 적용

대상 Prefab:

```text
Assets/_ProjectU/Prefabs/Building/WoodFloorPlaced.prefab
```

Identity 설정:

```text
Category: Buildable
Content Id: structure_wood_floor
Resolved Visual Profile Id: visual_buildable_wood_floor
```

Binder 설정:

```text
Resolve From Content Identity: On
Resolve From Registry By Id: Off
Visual Profile: VP_Buildable_WoodFloor
Visual Profile Id: visual_buildable_wood_floor
Applied Profile Id: visual_buildable_wood_floor
Last Apply Succeeded: On
```

자동 연결 결과:

```text
structure_wood_floor
→ visual_buildable_wood_floor
→ VP_Buildable_WoodFloor
```

생성 외형 설정:

```text
Position: 0, 0.05, 0
Scale: 1, 0.1, 1
Collider: 없음
```

기존 기능은 유지하였다.

- 건축 배치
- 건축 회전
- 건축 연결 지점
- 기존 건축 Collider
- 재료 소모
- 저장 및 불러오기

---

### 11. 잘못 추가된 항목 정리

개발 과정에서 `WoodFoundationPlaced`에 비어 있는 Identity 관련 컴포넌트가 추가되었으나, 해당 건축물의 Visual Profile이 아직 준비되지 않았으므로 기존 상태로 복구하였다.

제거된 항목:

```text
ContentVisualIdentity
ContentVisualProfileBinder
ContentVisualRoot
```

또한 69일차 작업과 관계없이 생성된 임시 TerrainData Asset도 커밋에서 제거하였다.

최종 69일차 변경 범위에는 다음 대상만 포함된다.

```text
PF_Temp_Enemy_Basic.prefab
WoodPickup.prefab
WoodFloorPlaced.prefab

ContentVisualIdentity.cs
ContentVisualProfileIdUtility.cs
ContentVisualProfileBinder.cs

ContentVisualIdentityEditor.cs
ContentVisualProfileBinderEditor.cs
```

---

### 12. 신규 및 수정 스크립트

#### 신규 생성

```text
Assets/_ProjectU/Scripts/Visual/ContentVisualIdentity.cs
Assets/_ProjectU/Scripts/Visual/ContentVisualProfileIdUtility.cs
Assets/_ProjectU/Scripts/Visual/Editor/ContentVisualIdentityEditor.cs
```

#### 수정

```text
Assets/_ProjectU/Scripts/Visual/ContentVisualProfileBinder.cs
Assets/_ProjectU/Scripts/Visual/Editor/ContentVisualProfileBinderEditor.cs
```

---

### 13. 검증 결과

저장소 기준으로 다음 연결 상태를 확인하였다.

```text
PF_Temp_Enemy_Basic
enemy_basic
→ visual_enemy_basic
→ VP_Enemy_Basic

WoodPickup
item_wood
→ visual_item_wood
→ VP_Item_Wood

WoodFloorPlaced
structure_wood_floor
→ visual_buildable_wood_floor
→ VP_Buildable_WoodFloor
```

각 대상은 다음 상태로 저장되었다.

```text
Resolve From Content Identity: On
Resolve From Registry By Id: Off
Visual Profile 캐시 정상
Applied Profile Id 정상
Last Apply Succeeded: On
```

VisualInstance 내부 임시 외형에는 Collider가 포함되지 않는다.

---

### 14. 로컬 테스트 항목

GitHub 저장소에는 Unity 컴파일을 실행하는 자동 CI가 등록되어 있지 않으므로, 최종 실행 상태는 Unity Editor에서 확인한다.

#### Console

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
Validate Content Visual Identity
Validate Assigned Profile
```

대상:

```text
PF_Temp_Enemy_Basic
WoodPickup
WoodFloorPlaced
```

#### Play Mode

다음 Scene에서 시작한다.

```text
Assets/_ProjectU/Scenes/00_Bootstrap.unity
```

확인 항목:

- 적 추적·공격·피격·사망 정상
- WoodPickup 획득과 저장 정상
- WoodFloor 배치·회전·연결·저장 정상
- WoodFoundation 기존 기능 정상
- 빈 Content ID 오류 없음
- Visual Profile 검색 오류 없음
- Console 빨간색 오류 없음

---

### 15. 작업 결과

69일차 작업을 통해 기능 Root가 원본 콘텐츠 ID만 가지고 있어도 알맞은 Visual Profile을 자동으로 찾을 수 있게 되었다.

기존 방식:

```text
Prefab마다 ContentVisualProfile 직접 연결
또는
Visual Profile ID를 별도로 직접 입력
```

변경된 방식:

```text
콘텐츠 종류와 콘텐츠 ID 입력
→ Visual Profile ID 자동 계산
→ Registry 검색
→ Profile 적용
```

이 구조를 통해 콘텐츠 ID와 외형 데이터 사이의 연결 규칙을 통일할 수 있으며, 새로운 아이템·적·건축물을 추가할 때 발생하는 수동 연결 작업과 입력 실수를 줄일 수 있다.

기존 직접 참조 방식도 유지되므로 모든 콘텐츠를 한 번에 전환하지 않고 순차적으로 Identity 방식으로 변경할 수 있다.

---

### 16. 다음 개발 방향

다음 일차에는 현재 구현한 Identity 구조를 실제 콘텐츠 데이터와 더 직접적으로 연결하는 작업을 진행한다.

우선순위:

1. `ItemData`, `EnemyCombatData`, `BuildRecipeData`에서 콘텐츠 ID 자동 전달
2. 생성·스폰 시 Identity 자동 설정
3. Profile 누락 콘텐츠 전체 검사 도구 구현
4. 무기와 도구의 1인칭·3인칭 Visual 분리
5. 신규 건축물과 자원에 Visual Profile 적용
