# Project U 개발 일지

## 66일차 : 공통 콘텐츠 ID 및 GameDataRegistry 구현

### 개발 목표

프로젝트에 흩어져 있는 아이템, 제작법, 건축법, 적 데이터를 하나의 공통 Registry에서 관리하도록 구성한다.

각 콘텐츠가 이미 사용 중인 고유 ID를 유지하면서, 앞으로 저장 복원·프리팹 생성·에셋 교체 시스템에서 문자열 ID만으로 필요한 데이터를 검색할 수 있는 기반을 마련한다.

---

### 구현 내용

#### 1. 공통 데이터 Registry 구현

`GameDataRegistry` ScriptableObject를 추가하였다.

다음 데이터를 하나의 Registry Asset에서 관리한다.

- `ItemData`
- `CraftingRecipeData`
- `BuildRecipeData`
- `EnemyCombatData`

각 데이터는 다음 고유 ID를 기준으로 등록된다.

| 데이터 종류 | 고유 ID |
|---|---|
| 아이템 | `ItemData.ItemId` |
| 제작법 | `CraftingRecipeData.RecipeId` |
| 건축법 | `BuildRecipeData.RecipeId` |
| 적 | `EnemyCombatData.EnemyId` |

#### 2. ID 기반 데이터 검색 구현

Registry 내부에 데이터 종류별 Dictionary를 구성하였다.

다음 검색 기능을 사용할 수 있다.

- 아이템 ID로 `ItemData` 검색
- 제작법 ID로 `CraftingRecipeData` 검색
- 건축법 ID로 `BuildRecipeData` 검색
- 적 ID로 `EnemyCombatData` 검색
- 전체 Registry에 특정 ID가 존재하는지 확인

검색 실패 시 예외를 발생시키지 않고 `false` 또는 `null`을 반환하도록 구성하였다.

#### 3. 콘텐츠 ID 검증 구현

공통 콘텐츠 ID는 다음 규칙을 사용한다.

- 소문자로 시작
- 소문자와 숫자 사용
- 단어 사이는 밑줄 사용
- 최소 한 번 이상의 밑줄 포함
- 앞뒤 공백 제거

정상 ID 예시:

```text
item_wood
item_stone_axe
recipe_stone_axe
structure_wood_wall
enemy_basic
```

잘못된 ID 예시:

```text
Wood
ITEM_WOOD
item wood
item-wood
item
```

#### 4. 중복 ID 검사 구현

다음 중복 상황을 구분하여 검사한다.

- 같은 데이터 종류 안의 중복 ID
- 서로 다른 데이터 종류 사이의 동일 ID
- 비어 있거나 규칙에 맞지 않는 ID

검증 결과는 Registry Inspector의 실행 상태 값과 Console 로그로 확인할 수 있다.

#### 5. Registry Runtime 관리자 구현

`GameDataRegistryRuntime`을 추가하였다.

주요 기능:

- 다른 게임 시스템보다 먼저 Registry 초기화
- 현재 Registry Asset의 Dictionary 구성
- Play Mode 시작 시 전체 데이터 검증
- Scene 전환 후 Registry 관리자 유지
- 중복 Runtime 관리자 자동 제거
- ID 검색 테스트 기능 제공

`00_Bootstrap` Scene에 배치하고 `DontDestroyOnLoad`를 통해 MainMenu와 Gameplay Scene에서도 유지하도록 구성하였다.

#### 6. Registry Editor 도구 구현

`GameDataRegistryEditor`를 추가하였다.

Unity 상단 메뉴에 다음 기능을 추가하였다.

```text
Project U
└─ Data
   ├─ Create Or Refresh Game Data Registry
   └─ Validate Default Game Data Registry
```

자동 수집 대상:

- 프로젝트 전체 `ItemData`
- 프로젝트 전체 `CraftingRecipeData`
- 프로젝트 전체 `BuildRecipeData`
- 프로젝트 전체 `EnemyCombatData`

수집된 데이터는 ID와 Asset 이름 순서로 정렬된다.

#### 7. Registry Asset 자동 생성 구현

다음 경로에 Registry Asset을 자동 생성하도록 구성하였다.

```text
Assets/_ProjectU/Data/Registry/GameDataRegistry.asset
```

폴더가 존재하지 않으면 Editor 도구가 필요한 폴더를 자동으로 생성한다.

#### 8. 추가 검증 기능 구현

Registry 기본 검증과 함께 다음 항목을 검사한다.

- 데이터 종류별 권장 ID 접두사
- 제작법 결과 아이템 연결 여부
- 제작 결과 아이템의 Registry 등록 여부
- Registry 배열의 빈 참조
- 중복 ID
- 잘못된 ID

권장 접두사:

| 데이터 종류 | 권장 접두사 |
|---|---|
| 아이템 | `item_` |
| 제작법 | `recipe_` |
| 건축법 | `structure_` |
| 적 | `enemy_` |

---

### 추가된 스크립트

```text
Assets/_ProjectU/Scripts/Data/GameDataRegistry.cs
Assets/_ProjectU/Scripts/Data/GameDataRegistryRuntime.cs
Assets/_ProjectU/Scripts/Data/Editor/GameDataRegistryEditor.cs
```

기존 데이터 스크립트는 수정하지 않았다.

```text
Assets/_ProjectU/Scripts/Items/ItemData.cs
Assets/_ProjectU/Scripts/Crafting/CraftingRecipeData.cs
Assets/_ProjectU/Scripts/Building/BuildRecipeData.cs
Assets/_ProjectU/Scripts/Enemy/EnemyCombatData.cs
```

---

### Scene 설정

`00_Bootstrap` Scene에 다음 오브젝트를 추가하였다.

```text
GameDataRegistryRuntime
└─ GameDataRegistryRuntime Component
```

Inspector 설정:

| 항목 | 값 |
|---|---|
| Registry | `GameDataRegistry.asset` |
| Persist Between Scenes | On |
| Validate On Awake | On |

---

### 테스트 내용

- Registry Asset 자동 생성 확인
- 프로젝트 전체 데이터 자동 수집 확인
- 아이템 ID 검색 확인
- 제작법 ID 검색 확인
- 건축법 ID 검색 확인
- 적 ID 검색 확인
- 중복 ID 오류 출력 확인
- 잘못된 ID 오류 출력 확인
- 제작 결과 아이템 누락 검사 확인
- Bootstrap에서 Registry Runtime 초기화 확인
- MainMenu Scene 전환 후 Runtime 유지 확인
- Gameplay Scene 전환 후 Runtime 유지 확인
- 기존 인벤토리 기능 정상 작동 확인
- 기존 제작 기능 정상 작동 확인
- 기존 건축 기능 정상 작동 확인
- 기존 적 전투 기능 정상 작동 확인

---

### 완료 결과

프로젝트의 주요 ScriptableObject 데이터를 하나의 Registry에서 관리할 수 있게 되었다.

앞으로 아이템, 적, 건축물과 제작 결과를 생성할 때 ScriptableObject를 직접 연결하는 방식뿐 아니라 고유 ID를 통해 데이터를 검색할 수 있다.

이 구조는 이후 다음 시스템의 기반으로 사용한다.

- 저장 데이터의 ID 기반 복원
- 임시 프리팹과 실제 에셋 교체
- 아이템 생성 Factory
- 적 생성 Factory
- 건축물 생성 Factory
- 데이터 누락 자동 검사
- 프로젝트 전체 콘텐츠 관리

---

### 다음 개발 방향

67일차에는 게임 로직과 외형을 분리하기 위한 공통 `Root`·`Visual` 구조를 구현한다.

모델이 없는 상태에서는 Primitive를 임시 외형으로 사용하고, 실제 모델이 추가되면 `Visual` 자식만 교체할 수 있도록 구성할 예정이다.
