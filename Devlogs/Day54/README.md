# Project U 개발 일지

## 54일차 : 보관함 UI 지연 생성 및 프리팹 재사용 구현

### 개발 목표

기존 Gameplay Scene에 항상 배치되어 있던 보관함 UI를 프리팹 기반의 런타임 생성 방식으로 변경한다.

게임 시작 시에는 보관함 UI를 생성하지 않고, 플레이어가 처음 보관함과 상호작용할 때만 팝업을 생성한다. 이후에는 생성된 팝업을 제거하지 않고 재사용하여 소형 상자와 대형 상자가 하나의 UI를 공유하도록 구성한다.

또한 보관함을 열고 닫을 때 플레이어 입력과 커서 상태가 안정적으로 전환되도록 하고, 작은 상자가 바닥 Collider에 가려 상호작용되지 않던 문제를 해결한다.

---

### 구현 내용

#### 1. 보관함 UI 프리팹 제작

기존 Scene에 배치되어 있던 보관함 UI를 다음 프리팹으로 분리하였다.

```text
Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_StoragePopup.prefab
```

프리팹은 다음 구조로 구성하였다.

```text
PF_UI_StoragePopup
└─ StoragePopup
   ├─ Background
   ├─ Header
   │  ├─ TitleText
   │  └─ CloseButton
   └─ Content
      ├─ PlayerInventoryArea
      └─ StorageArea
```

`PF_UI_StoragePopup`은 런타임 인스턴스를 유지하기 위한 루트이며, 실제 화면 요소는 자식 `StoragePopup`에 배치하였다.

#### 2. 보관함 UI 지연 생성

`GameUIManager`가 보관함 팝업 프리팹과 `PopupLayer`를 관리하도록 연결하였다.

게임 시작 직후에는 `PopupLayer`에 보관함 UI가 존재하지 않는다.

플레이어가 처음 보관함을 사용할 때 다음 순서로 팝업을 생성한다.

```text
보관함 F 상호작용
→ GameUIManager.OpenStorage()
→ PF_UI_StoragePopup 최초 생성
→ PlayerInventory 런타임 연결
→ 선택한 StorageContainer 연결
→ 보관함 화면 표시
```

#### 3. 생성된 팝업 인스턴스 재사용

보관함 UI를 닫을 때 프리팹 인스턴스를 제거하지 않고 화면만 비활성화하도록 구성하였다.

```text
PF_UI_StoragePopup
→ 런타임에 계속 유지

StoragePopup
→ 열 때 활성화
→ 닫을 때 비활성화
```

다른 상자를 다시 열면 새로운 UI를 생성하지 않고 기존 팝업 인스턴스에 새로운 `StorageContainer`를 연결한다.

#### 4. PlayerInventory 런타임 연결

프리팹은 Scene의 `PlayerInventory`를 직접 참조할 수 없으므로 `InventorySlotsUI`에 런타임 초기화 기능을 추가하였다.

보관함 팝업 생성 직후 `GameUIManager`가 플레이어 인벤토리를 전달하며, 보관함 화면에서 다음 영역을 표시한다.

- Hotbar 8칸
- 일반 인벤토리 슬롯
- 아이템 아이콘
- 아이템 수량
- 드래그 이동 상태

#### 5. 보관함과 일반 인벤토리 팝업 충돌 방지

`GameUIManager`가 현재 열린 팝업 종류를 관리하도록 구성하였다.

보관함이 열린 상태에서 일반 인벤토리를 열면 기존 보관함을 먼저 닫고 일반 인벤토리를 표시한다.

반대로 일반 인벤토리가 열린 상태에서 보관함을 사용하면 일반 인벤토리를 닫고 보관함 팝업을 표시한다.

```text
동시에 표시 가능한 주요 팝업 수
→ 1개
```

#### 6. 공통 입력 잠금 연결

보관함이 열린 동안 `GameplayInputLock`을 통해 플레이어 조작을 제한한다.

차단 대상은 다음과 같다.

- 플레이어 이동
- 카메라 회전
- 상호작용
- Hotbar 아이템 사용
- 건축 배치 입력
- Hotbar 숫자키 입력

팝업을 닫으면 이전 입력 상태와 커서 상태를 복원한다.

#### 7. 작은 상자 상호작용 탐지 수정

기존 `PlayerInteractor`는 `SphereCast`로 가장 먼저 충돌한 Collider 하나만 검사하였다.

작은 상자가 바닥 위에 설치된 경우 바닥 Collider가 먼저 감지되면서 상자의 `StorageInteractable`을 찾지 못하는 문제가 발생하였다.

이를 `SphereCastNonAlloc` 기반의 다중 탐지 방식으로 변경하였다.

```text
전방 Collider 전체 탐지
→ 상호작용 컴포넌트가 없는 바닥과 건축물 제외
→ 실제 InteractableBase가 있는 가장 가까운 대상 선택
```

이제 작은 상자가 바닥 Collider 뒤에 있어도 정상적으로 상호작용할 수 있다.

#### 8. 작은 상자 Collider 보완

작은 상자의 상호작용 판정이 지나치게 좁지 않도록 `BoxCollider` 범위를 조정하였다.

```text
Size
X: 1
Y: 0.9
Z: 0.8

Center
X: 0
Y: 0.4
Z: 0
```

#### 9. 보관함 UI 크기 수정

`StoragePopup`이 전체 Stretch 상태에서 추가 크기를 가지면서 화면보다 크게 표시되던 문제를 수정하였다.

실제 팝업을 중앙 고정 방식으로 변경하였다.

```text
Anchor Min: 0.5, 0.5
Anchor Max: 0.5, 0.5
Position: 0, 0
Width: 1580
Height: 800
Pivot: 0.5, 0.5
```

루트 `PF_UI_StoragePopup`은 전체 화면 Stretch 상태를 유지한다.

#### 10. 보관함 종료 기능 보완

보관함은 다음 방법으로 닫을 수 있도록 구성하였다.

- F 키 재입력
- ESC 키
- 닫기 버튼
- 현재 보관함 비활성화
- 현재 보관함 제거
- 플레이어와 보관함 사이 거리 초과

거리 자동 종료 기본값은 다음과 같다.

```text
Maximum Open Distance: 3
```

`GameUIManager`의 내부 팝업 상태가 실제 화면 상태와 일치하지 않더라도 보관함 화면을 강제로 숨길 수 있도록 종료 처리를 보완하였다.

#### 11. Panel Root 연결 문제 수정

보관함을 닫아도 제목과 슬롯 UI가 남고 배경 일부만 사라지던 문제가 발생하였다.

원인은 `StorageContainerUI`의 `Panel Root`가 팝업 전체가 아닌 `Background` 하나만 참조하고 있었기 때문이다.

최종적으로 팝업 전체 루트를 연결하였다.

```text
Panel Root
→ PF_UI_StoragePopup
```

이에 따라 보관함을 닫으면 다음 요소가 모두 함께 사라진다.

- 상자 이름 제목
- 플레이어 인벤토리
- 보관함 슬롯
- 배경
- 스크롤바
- 닫기 버튼

#### 12. 저장 불러오기 전 팝업 정리

`GameplaySaveController`가 저장 데이터를 불러오기 전에 `GameUIManager`를 통해 현재 열린 팝업을 닫도록 연결하였다.

이를 통해 보관함을 연 상태에서 불러오기를 실행해도 다음 상태를 정리할 수 있다.

- 보관함 화면
- 현재 보관함 이벤트 연결
- 커서 표시 상태
- 플레이어 입력 잠금

---

### 주요 수정 파일

```text
Assets/_ProjectU/Prefabs/Building/SmallChestPlaced.prefab
Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_StoragePopup.prefab
Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity
Assets/_ProjectU/Scripts/Interaction/PlayerInteractor.cs
Assets/_ProjectU/Scripts/Storage/StorageContainerUI.cs
Assets/_ProjectU/Scripts/UI/Core/GameUIManager.cs
```

---

### 테스트 항목

#### 보관함 생성

- 게임 시작 직후 `PopupLayer`가 비어 있는지 확인
- 작은 상자를 처음 열 때 보관함 팝업이 생성되는지 확인
- 팝업 이름에 불필요한 `(Clone)`이 남지 않는지 확인
- 다시 열었을 때 팝업이 추가 생성되지 않는지 확인

#### 작은 상자

- 바닥 위에 설치한 작은 상자를 정상 탐지하는지 확인
- `F - OPEN SMALL CHEST` 안내 문구가 표시되는지 확인
- F 키로 정상적으로 열리는지 확인
- 12개 보관함 슬롯이 표시되는지 확인

#### 보관함 종료

- F 키 재입력으로 닫히는지 확인
- ESC 키로 닫히는지 확인
- 닫기 버튼으로 닫히는지 확인
- 닫을 때 제목과 슬롯을 포함한 전체 화면이 사라지는지 확인
- 닫힌 뒤 플레이어 이동과 카메라 입력이 복구되는지 확인

#### 인벤토리 이동

- 플레이어 인벤토리에서 보관함으로 아이템을 이동할 수 있는지 확인
- 보관함에서 플레이어 인벤토리로 아이템을 이동할 수 있는지 확인
- 같은 아이템이 정상적으로 합쳐지는지 확인
- 아이템 수량이 복제되거나 사라지지 않는지 확인

#### 팝업 전환

- 보관함이 열린 상태에서 일반 인벤토리를 열었을 때 보관함이 닫히는지 확인
- 일반 인벤토리와 보관함이 동시에 표시되지 않는지 확인
- 최종 팝업 종료 후 입력 잠금이 정상 해제되는지 확인

#### 저장과 불러오기

- 상자에 아이템을 넣고 저장할 수 있는지 확인
- 보관함이 열린 상태에서 불러오기를 실행할 수 있는지 확인
- 불러오기 전에 팝업이 닫히는지 확인
- 불러오기 후 상자 내용이 정상 복원되는지 확인

---

### 점검 결과

다음 항목은 저장소 기준으로 정상 반영된 것을 확인하였다.

- 작은 상자 상호작용 탐지 개선
- 작은 상자 Collider 확대
- 보관함 팝업 프리팹 생성
- `PopupLayer`와 `GameUIManager` 연결
- 보관함 팝업 최초 생성
- 생성된 팝업 인스턴스 재사용
- 팝업 중앙 고정 크기 적용
- F·ESC·닫기 버튼 종료
- 거리 초과 자동 종료
- 보관함 전체를 대상으로 한 `Panel Root` 연결
- 보관함 종료 시 전체 UI 비활성화

---

### 추가 점검 사항

#### LargeChestPlaced.prefab

```text
Storage Container
→ 같은 루트의 StorageContainer

Game UI Manager
→ None

Layer
→ 루트와 ChestMesh 모두 상호작용 레이어로 통일
```

#### GameplaySaveController

```text
Game UI Manager
→ Scene의 GameUIManager 명시적 연결 권장
```

코드에서 자동 검색을 수행하지만 저장 시스템은 핵심 기능이므로 Inspector에서 직접 연결하는 편이 안전하다.

#### CloseButtonText

현재 닫기 버튼 텍스트가 `Button`이라면 화면 크기에 따라 줄바꿈될 수 있다.

```text
권장 Text: X
권장 Font Size: 28
권장 Alignment: Center / Middle
```

---

### 작업 결과

보관함 UI를 Scene 상시 배치 방식에서 런타임 프리팹 생성 방식으로 전환하였다.

게임 시작 시 보관함 UI를 생성하지 않고 최초 사용 시에만 생성하며, 이후에는 같은 인스턴스를 재사용하도록 구성하였다.

작은 상자가 바닥 Collider에 가려 상호작용되지 않던 문제를 해결하고, 보관함 UI의 크기와 종료 범위를 수정하였다.

보관함을 닫을 때 일부 배경만 사라지고 나머지 요소가 남던 문제도 `Panel Root` 연결 수정으로 해결하였다.

현재 보관함은 소형 상자와 대형 상자가 공통 UI를 사용할 수 있는 기반을 갖추었으며, 일반 인벤토리와의 팝업 충돌 및 입력 잠금도 공통 관리 구조로 처리된다.

---

### 커밋 정보

```text
54일차 : 보관함 UI 지연 생성 및 프리팹 재사용 구현
```
