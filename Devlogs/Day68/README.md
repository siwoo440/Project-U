# Project U 개발 일지

## 68일차 : 공통 Visual Profile 및 데이터 기반 외형 적용 구현

### 1. 개발 목표

67일차에 구현한 `Root`와 `Visual` 분리 구조를 확장하여, 아이템·적·건축물·무기 등의 외형 설정을 각 오브젝트에서 직접 관리하지 않고 `ContentVisualProfile` 데이터로 공통 관리할 수 있도록 구현하였다.

이번 작업의 주요 목표는 다음과 같다.

- 외형 Prefab, 임시 Primitive, Material, 크기와 기준점 설정을 하나의 데이터 Asset으로 관리
- 여러 오브젝트가 동일한 외형 설정을 공유할 수 있는 구조 구현
- `GameDataRegistry`에서 Visual Profile을 ID로 검색할 수 있도록 확장
- Profile을 기능 Root에 자동 적용하는 Binder 구현
- 실제 모델이 없어도 임시 Primitive로 기능을 테스트할 수 있는 구조 유지
- 적·아이템·건축물에 Profile을 실제 적용하여 기존 기능 유지 여부 확인

---

### 2. 구현 내용

#### 2.1 ContentVisualProfile 구현

`ContentVisualProfile` ScriptableObject를 추가하였다.

Visual Profile은 다음 정보를 저장한다.

- Profile ID
- 표시 이름
- 콘텐츠 분류
- 실제 Visual Prefab
- UI 아이콘
- Material Override
- Animator Controller
- 임시 Primitive 종류
- 임시 Material과 색상
- 외형의 로컬 위치·회전·크기
- InteractionPoint 위치
- EffectOrigin 위치
- UIAnchor 위치
- Root Layer 상속 여부
- Visual 내부 Collider 제거 여부
- 대표 AudioClip
- 대표 VFX Prefab

실제 모델이 준비되지 않은 콘텐츠는 `Visual Prefab`을 비워 두고 Placeholder 설정을 사용해 임시 외형을 생성할 수 있다.

---

#### 2.2 ContentVisualProfileBinder 구현

기능 Root에 Visual Profile을 연결하고 적용하는 `ContentVisualProfileBinder`를 추가하였다.

Binder는 다음 기능을 제공한다.

- 직접 연결한 `ContentVisualProfile` 적용
- `GameDataRegistry`에서 Profile ID로 검색하여 적용
- `Awake` 시 Profile 자동 적용
- Profile 적용 시 Visual 재생성
- Root에 남아 있는 기존 Renderer 비활성화
- Profile ID와 외형 생성 정보 검증
- 마지막 적용 Profile ID와 성공 여부 기록

Profile ID가 잘못되었거나 Visual Prefab과 Placeholder 설정이 모두 없는 경우에는 적용을 중단하도록 보완하였다.

---

#### 2.3 ContentVisualRoot 확장

67일차에 구현한 `ContentVisualRoot`를 Visual Profile 기반 구조로 확장하였다.

추가된 주요 기능은 다음과 같다.

- `ContentVisualProfile` 설정 일괄 적용
- 실제 Prefab 또는 임시 Primitive 생성
- 외형 Material과 색상 적용
- Animator Controller 적용
- Profile 기준으로 InteractionPoint, EffectOrigin, UIAnchor 위치 변경
- VisualInstance 내부 Collider 자동 제거
- Visual Layer를 기능 Root와 동일하게 적용
- 생성 외형의 위치·회전·크기 적용
- 현재 적용 Profile과 생성 외형 참조 기록
- Visual 구조와 생성 결과 검증

임시 Material을 사용하는 기존 외형이 흰색으로 덮이지 않도록 `Use Placeholder Color` 기본값을 비활성화하였다.

검증 과정에서는 다음 상태를 오류로 처리하도록 수정하였다.

- 표준 Visual 구조 누락
- Visual Root가 자기 자신을 참조
- Visual Prefab과 Placeholder 설정 모두 누락
- VisualInstance에 생성된 외형 없음
- VisualInstance 내부에 Collider가 남아 있음
- 하나의 기능 Root에 `ContentVisualRoot`가 중복으로 존재

---

#### 2.4 GameDataRegistry 확장

`GameDataRegistry`의 Registry Version을 2로 올리고 `ContentVisualProfile` 목록과 검색 Dictionary를 추가하였다.

Registry에서 관리하는 데이터는 다음과 같다.

- ItemData
- CraftingRecipeData
- BuildRecipeData
- EnemyCombatData
- ContentVisualProfile

Visual Profile은 `visual_` 접두사를 사용하는 ID 체계로 관리한다.

```text
visual_enemy_basic
visual_item_wood
visual_buildable_wood_floor
visual_weapon_stone_axe
```

Registry는 다음 항목을 검사한다.

- 비어 있거나 형식이 잘못된 ID
- 같은 데이터 종류 내부의 중복 ID
- 서로 다른 데이터 종류 사이의 중복 ID
- Visual Profile 권장 접두사
- Visual Prefab과 Placeholder 설정 누락

---

#### 2.5 GameDataRegistryRuntime 확장

`GameDataRegistryRuntime`에 Visual Profile 검색 기능을 추가하였다.

추가된 기능:

- `TryGetVisualProfile()`
- Registry에 등록된 첫 번째 Visual Profile 검색 테스트
- 실행 중 Profile ID를 이용한 Binder 적용 지원

Registry ID 방식은 `GameDataRegistryRuntime`이 먼저 초기화되어야 하므로 `00_Bootstrap` Scene에서 게임을 시작하는 구조를 유지한다.

---

#### 2.6 Editor 도구 확장

`GameDataRegistryEditor`가 프로젝트의 모든 `ContentVisualProfile` Asset을 자동 수집하도록 수정하였다.

사용 메뉴:

```text
Project U
→ Data
→ Create Or Refresh Game Data Registry
```

또는 `GameDataRegistry.asset` Inspector의 다음 버튼을 사용할 수 있다.

```text
Collect All Project Data
Validate Registry
Collect And Validate
```

`ContentVisualProfileBinderEditor`도 추가하였다.

Binder Inspector에서 다음 기능을 사용할 수 있다.

```text
Apply Assigned Profile
Validate Assigned Profile
```

여러 Root를 선택한 뒤 다음 메뉴를 실행하면 Profile을 일괄 적용할 수 있다.

```text
Project U
→ Visual
→ Apply Profiles To Selected Roots
```

---

### 3. 생성한 Visual Profile

#### 적

```text
VP_Enemy_Basic
Profile ID: visual_enemy_basic
Category: Enemy
Placeholder: Capsule
```

#### 아이템

```text
VP_Item_Wood
Profile ID: visual_item_wood
Category: Item
Placeholder: Cube
```

#### 건축물

```text
VP_Buildable_WoodFloor
Profile ID: visual_buildable_wood_floor
Category: Buildable
Placeholder: Cube
Position: 0, 0.05, 0
Scale: 1, 0.1, 1
```

#### 무기

```text
VP_Weapon_StoneAxe
Profile ID: visual_weapon_stone_axe
Category: Weapon
Placeholder: Cube
```

무기 Profile은 현재 Registry 등록과 데이터 구조 준비까지만 진행하였다. 실제 1인칭·3인칭 무기 Socket 적용은 이후 작업에서 진행한다.

---

### 4. Prefab 적용

#### 4.1 적 Prefab

기존 Scene 전용 적을 재사용 가능한 Prefab으로 변경하였다.

```text
Assets/_ProjectU/Prefabs/Development/Placeholder/PF_Temp_Enemy_Basic.prefab
```

적 Prefab Root에는 다음 기능이 유지된다.

- EnemyHealth
- EnemyCombatController
- EnemyNavMeshMovement
- NavMeshAgent
- EnemyCombatImpactMotor
- CapsuleCollider
- ContentVisualRoot
- ContentVisualProfileBinder

Prefab Asset의 Root Transform은 기본값으로 정리하였다.

```text
Position: 0, 0, 0
Rotation: 0, 0, 0
Scale: 1, 1, 1
```

#### 4.2 WoodPickup Prefab

`WoodPickup.prefab`에 다음을 적용하였다.

- ContentVisualRoot
- ContentVisualProfileBinder
- VP_Item_Wood

Prefab 내부에 다음 표준 구조를 저장하였다.

```text
WoodPickup
├─ Visual
│  └─ VisualInstance
│     └─ TEMP_Cube_Visual
├─ InteractionPoint
├─ EffectOrigin
└─ UIAnchor
```

Scene Override로 남아 있던 중복 `ContentVisualRoot`와 표준 자식 오브젝트를 제거하여 기능 Root에는 `ContentVisualRoot`가 한 개만 존재하도록 수정하였다.

기존 기능은 그대로 유지하였다.

- WorldItemPickup
- Rigidbody
- BoxCollider
- WorldObjectIdentity
- F 상호작용
- 인벤토리 획득
- 저장 데이터 연결

#### 4.3 WoodFloorPlaced Prefab

`WoodFloorPlaced.prefab`에 다음을 적용하였다.

- ContentVisualRoot
- ContentVisualProfileBinder
- VP_Buildable_WoodFloor

기존 `FloorMesh`의 Renderer와 Profile 외형이 겹치지 않도록 기존 Renderer를 비활성화하였다.

기존 `BoxCollider`와 건축 연결 지점은 유지하였다.

```text
WoodFloorPlaced
├─ FloorMesh
├─ Connections
├─ Visual
│  └─ VisualInstance
│     └─ TEMP_Cube_Visual
├─ InteractionPoint
├─ EffectOrigin
└─ UIAnchor
```

생성된 Placeholder에는 Collider가 남지 않으며, 실제 충돌 판정은 기존 건축물 Collider가 담당한다.

---

### 5. 추가 및 수정한 스크립트

#### 신규 생성

```text
Assets/_ProjectU/Scripts/Visual/ContentVisualProfile.cs
Assets/_ProjectU/Scripts/Visual/ContentVisualProfileBinder.cs
Assets/_ProjectU/Scripts/Visual/Editor/ContentVisualProfileBinderEditor.cs
```

#### 수정

```text
Assets/_ProjectU/Scripts/Visual/ContentVisualRoot.cs
Assets/_ProjectU/Scripts/Data/GameDataRegistry.cs
Assets/_ProjectU/Scripts/Data/GameDataRegistryRuntime.cs
Assets/_ProjectU/Scripts/Data/Editor/GameDataRegistryEditor.cs
```

---

### 6. 추가 및 수정한 주요 Asset

```text
Assets/_ProjectU/Data/Registry/GameDataRegistry.asset
Assets/_ProjectU/Data/VisualProfiles/Enemies/VP_Enemy_Basic.asset
Assets/_ProjectU/Data/VisualProfiles/Items/VP_Item_Wood.asset
Assets/_ProjectU/Data/VisualProfiles/Buildables/VP_Buildable_WoodFloor.asset
Assets/_ProjectU/Data/VisualProfiles/Weapons/VP_Weapon_StoneAxe.asset
Assets/_ProjectU/Prefabs/Development/Placeholder/PF_Temp_Enemy_Basic.prefab
Assets/_ProjectU/Prefabs/Items/WoodPickup.prefab
Assets/_ProjectU/Prefabs/Building/WoodFloorPlaced.prefab
Assets/_ProjectU/Scenes/20_Gameplay.unity
```

---

### 7. Registry 검증 결과

```text
Registry Version: 2
ItemData: 15
CraftingRecipeData: 2
BuildRecipeData: 9
EnemyCombatData: 1
ContentVisualProfile: 4
종류별 중복 ID: 0
전체 중복 ID: 0
잘못된 ID: 0
```

---

### 8. 테스트 내용

#### 적

- Profile을 통해 빨간 Capsule 외형 생성
- Root의 기존 Renderer 비활성화
- VisualInstance 내부 Collider 제거
- NavMesh 추적 이동 정상
- 공격 예고와 공격 정상
- 적 피격과 밀림 정상
- 적 사망 정상

#### WoodPickup

- Profile을 통해 임시 Cube 외형 생성
- `ContentVisualRoot` 중복 없음
- Material 색상이 흰색으로 덮이지 않음
- Root BoxCollider와 Rigidbody 유지
- F 상호작용 정상
- 인벤토리 획득 정상
- WorldObjectIdentity 유지
- 저장 후 복원 상태 정상

#### WoodFloor

- 일반 Cube가 아닌 얇은 바닥 크기로 표시
- 기존 FloorMesh Renderer와 Profile 외형이 겹치지 않음
- 기존 BoxCollider 유지
- 건축 연결 지점 유지
- 배치와 회전 정상
- 재료 소모 정상
- 저장과 불러오기 정상

#### Registry

- Visual Profile 네 개 자동 수집
- Visual Profile ID 검색 성공
- 중복 ID 없음
- 잘못된 ID 없음
- 직접 Profile 적용 정상
- Registry ID 방식 적용 정상

---

### 9. 작업 결과

68일차 작업을 통해 Project U의 외형 설정을 기능 오브젝트와 분리된 데이터로 관리할 수 있게 되었다.

이제 실제 모델이 준비되면 기능 Root와 전투·상호작용·저장 로직을 수정하지 않고 Visual Profile의 `Visual Prefab`만 교체하여 외형을 변경할 수 있다.

```text
ContentVisualProfile
→ ContentVisualProfileBinder
→ ContentVisualRoot
→ VisualInstance 생성
```

이 구조는 이후 다음 콘텐츠 확장에 사용할 수 있다.

- 적 종류별 모델 교체
- 아이템 종류별 월드 외형
- 건축물 종류별 모델
- 무기와 도구 외형
- 채집 자원 외형
- Animator Controller 연결
- AudioClip과 VFX 연결

---

### 10. 다음 개발 방향

다음 일차에는 Visual Profile 구조를 기반으로 실제 콘텐츠와 연결되는 외형 참조 규칙을 확장한다.

1. 아이템·적·건축 데이터와 Visual Profile 연결 방식 정리
2. 콘텐츠 종류별 Profile 자동 선택 구조 검토
3. 실제 모델 Prefab 교체 시 기능 Root 유지 검증
4. 무기와 도구의 1인칭·3인칭 Visual 분리 준비
5. Profile 누락과 잘못된 연결을 탐지하는 Editor 검증 확장
