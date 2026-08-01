# Project U 개발 일지

## 55일차 : 일반 인벤토리 UI 지연 생성 및 런타임 참조 연결

### 개발 목표

Gameplay Scene에 항상 배치되어 있던 일반 인벤토리 UI를 프리팹 기반의 런타임 생성 방식으로 변경한다.

게임 시작 시에는 일반 인벤토리 팝업을 생성하지 않고, 플레이어가 처음 I 키를 누르거나 제작 시설과 상호작용했을 때만 생성한다. 생성된 팝업은 닫을 때 제거하지 않고 비활성화하여 이후 다시 사용할 때 기존 인스턴스를 재사용하도록 구성한다.

또한 프리팹 에셋이 Scene의 Player 관련 컴포넌트를 직접 참조할 수 없는 문제를 해결하기 위해, `GameUIManager`가 인벤토리 팝업 생성 직후 필요한 시스템 참조를 전달하도록 런타임 초기화 구조를 적용한다.

---

### 구현 내용

#### 1. 일반 인벤토리 UI 프리팹 제작

기존 Gameplay Scene에 직접 배치되어 있던 일반 인벤토리 UI를 다음 프리팹으로 분리하였다.

```text
Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_InventoryPopup.prefab
```

기본 구조는 다음과 같다.

```text
PF_UI_InventoryPopup
├─ InventoryPopupController
└─ InventoryPopup
   ├─ InventoryHotbarArea
   ├─ InventoryStorageArea
   ├─ ItemDetailPanel
   ├─ EquipmentPanel
   └─ CraftingArea
```

`PF_UI_InventoryPopup`은 런타임에 생성되어 유지되는 프리팹 루트이며, 실제 화면 요소는 자식 `InventoryPopup`에 배치하였다.

---

#### 2. 일반 인벤토리 UI 지연 생성

`GameUIManager`가 다음 항목을 관리하도록 확장하였다.

```text
GameplayInputLock
PlayerInventory
InventoryItemDropper
PlayerEquipment
CraftingManager
PopupLayer
PF_UI_InventoryPopup
PF_UI_StoragePopup
```

게임 시작 직후에는 `PopupLayer` 아래에 일반 인벤토리와 보관함 팝업이 존재하지 않는다.

플레이어가 처음 I 키를 누르면 다음 순서로 일반 인벤토리 팝업을 생성한다.

```text
I 키 입력
→ GameUIManager.ToggleInventory()
→ PF_UI_InventoryPopup 최초 생성
→ Player 관련 시스템 런타임 연결
→ InventoryPopup 활성화
```

---

#### 3. 생성된 인벤토리 팝업 재사용

일반 인벤토리를 닫을 때 프리팹 인스턴스를 제거하지 않고 실제 화면 패널만 비활성화하도록 구성하였다.

```text
PF_UI_InventoryPopup
→ 런타임에 계속 유지

InventoryPopup
→ 열 때 활성화
→ 닫을 때 비활성화
```

인벤토리를 다시 열면 새로운 프리팹을 생성하지 않고 기존 인스턴스를 재사용한다.

정상적인 런타임 구조는 다음과 같다.

```text
Canvas
└─ PopupLayer
   ├─ PF_UI_InventoryPopup
   └─ PF_UI_StoragePopup
```

각 팝업 프리팹은 최대 한 개의 인스턴스만 유지한다.

---

#### 4. 공통 팝업 입력 중앙 관리

기존에는 `InventoryPopupController`가 I 키와 Alt 키를 직접 처리하였다.

55일차에는 입력 처리를 `GameUIManager`로 이동하여 일반 인벤토리 팝업이 아직 생성되지 않은 상태에서도 입력을 받을 수 있도록 수정하였다.

관리하는 입력은 다음과 같다.

```text
I
→ 일반 인벤토리 열기와 닫기

ESC
→ 현재 열린 팝업 닫기

Left Alt / Right Alt
→ 마우스 커서 사용을 위한 입력 잠금
```

일반 인벤토리와 보관함은 동시에 표시되지 않으며, 새로운 팝업을 열면 기존 팝업을 먼저 숨긴다.

---

#### 5. GameplayInputLock 연동

인벤토리와 보관함이 열려 있는 동안 공통 입력 잠금 시스템을 사용한다.

차단 대상은 다음과 같다.

```text
플레이어 이동
카메라 회전
상호작용
Hotbar 아이템 사용
건축 모드 입력
Hotbar 숫자키 입력
```

팝업을 닫으면 보유 중인 다른 잠금 상태를 확인한 후 플레이어 입력과 커서 상태를 복구한다.

Alt 키와 팝업 잠금은 서로 다른 잠금 ID를 사용하여 동시에 적용되더라도 충돌하지 않도록 구성하였다.

---

#### 6. InventorySlotsUI 런타임 연결

일반 인벤토리 프리팹 내부의 슬롯 UI는 Project 프리팹이므로 Scene의 `PlayerInventory`를 직접 참조할 수 없다.

다음 두 영역의 `Player Inventory`는 프리팹에서 `None`으로 유지한다.

```text
InventoryHotbarArea
InventoryStorageArea
```

팝업 생성 직후 `GameUIManager`가 `PlayerInventory`를 전달하고, 각 `InventorySlotsUI`가 슬롯을 생성하도록 구성하였다.

팝업 내부에서는 다음 슬롯을 표시한다.

```text
Hotbar 8칸
기본 인벤토리 슬롯
가방 장착으로 확장된 추가 슬롯
```

---

#### 7. 화면 하단 고정 Hotbar 복구

프리팹 전환 과정에서 Scene에 항상 존재하는 `HotbarPanel`의 `Player Inventory` 참조가 해제되어, 화면 하단 Hotbar 슬롯이 생성되지 않는 문제가 발생하였다.

일반 인벤토리 팝업 내부 슬롯과 달리 화면 하단 고정 Hotbar는 Scene UI이므로 다음처럼 직접 연결하였다.

```text
HotbarPanel
└─ InventorySlotsUI
   ├─ Player Inventory: Player의 PlayerInventory
   ├─ Visible Slot Count: 8
   └─ Start Slot Index: 0
```

이를 통해 게임 시작 직후 Hotbar 8칸과 아이템 아이콘, 수량, 선택 표시가 다시 생성되도록 수정하였다.

---

#### 8. 아이템 상세 UI 런타임 초기화

`InventoryDetailUI`에 런타임 초기화 기능을 추가하였다.

팝업 생성 시 다음 참조를 전달한다.

```text
PlayerInventory
InventoryItemDropper
PlayerEquipment
```

이를 통해 다음 기능을 유지한다.

```text
선택 아이템 이름 표시
아이템 분류 표시
아이템 설명 표시
아이템 수량 표시
아이템 한 개 제거
아이템 한 개 버리기
장비 아이템 장착
```

프리팹 내부의 Text, Image, Button 참조는 프리팹에서 유지하고, Player 관련 런타임 참조만 실행 중 연결한다.

---

#### 9. 장비 슬롯 UI 런타임 초기화

다음 장비 슬롯의 `EquipmentSlotUI`에 `PlayerEquipment`를 런타임으로 전달하도록 수정하였다.

```text
HeadSlot
BodySlot
LegsSlot
FeetSlot
BackpackSlot
```

각 장비 슬롯은 다음 기능을 유지한다.

```text
장착 중인 아이템 아이콘 표시
장착 중인 아이템 이름 표시
빈 슬롯 EMPTY 표시
UNEQUIP 버튼 활성화와 비활성화
장비 해제 후 인벤토리 복귀
```

프리팹 에셋의 `Player Equipment`가 `None`인 것은 정상이며, I 키 최초 입력 후 생성된 런타임 인스턴스에서 실제 `PlayerEquipment`가 연결된다.

---

#### 10. 장비 능력치 UI 런타임 초기화

`EquipmentStatsUI`도 프리팹에서 Scene의 `PlayerEquipment`를 직접 참조하지 않도록 변경하였다.

팝업 생성 시 `GameUIManager`가 `PlayerEquipment`를 전달하고, 다음 종합 능력치를 갱신한다.

```text
방어력
최대 체력 보너스
이동 속도 보너스
허기 소모 감소
갈증 소모 감소
추위 저항
인벤토리 추가 슬롯
```

프리팹에서는 다음 상태를 유지한다.

```text
Player Equipment: None
Stats Text: 프리팹 내부 TextMeshPro 연결
```

---

#### 11. 제작 UI 런타임 초기화

`CraftingRecipeButton`이 `PlayerInventory`와 `CraftingManager`를 런타임에 전달받도록 수정하였다.

프리팹 내부에서는 다음 외부 참조를 비워 둔다.

```text
Player Inventory: None
Crafting Manager: None
```

다음 프리팹 내부 참조와 데이터는 유지한다.

```text
Recipe Data
Recipe Name Text
Ingredient Text
Status Text
Craft Button
```

제작 버튼은 다음 상태를 표시한다.

```text
LOCKED
WRONG FACILITY
NEED MATERIALS
INVENTORY FULL
READY
CRAFTED
```

---

#### 12. 제작 시설과 동적 인벤토리 연결

기존 `CraftingFacilityInteractable`은 Scene에 존재하던 `InventoryPopupController`를 직접 찾았다.

일반 인벤토리가 런타임 프리팹으로 변경되면서 Scene에 해당 컨트롤러가 존재하지 않게 되었으므로, 제작 시설이 `GameUIManager`를 통해 인벤토리 팝업을 열도록 수정하였다.

작업대 사용 흐름은 다음과 같다.

```text
작업대 F 상호작용
→ CraftingManager의 활성 시설을 Workbench로 변경
→ GameUIManager.OpenInventory()
→ 일반 인벤토리 팝업 생성 또는 재사용
```

인벤토리를 닫거나 다른 팝업으로 전환하면 제작 시설 세션을 종료하고 기본 `Hand` 상태로 복구한다.

---

#### 13. B 키 건축 모드 입력 수정

일반 인벤토리 프리팹 전환 후 `BuildPlacementController`가 삭제된 Scene의 `InventoryPopupController`를 계속 참조하면서 B 키 입력 기능이 비활성화되는 문제가 발생하였다.

기존 참조를 다음처럼 변경하였다.

```text
기존
InventoryPopupController

수정
GameUIManager
```

건축 모드 진입과 종료 조건은 `GameUIManager.HasOpenPopup`을 기준으로 검사한다.

```text
팝업이 닫혀 있음
→ B 키로 건축 모드 진입 가능

팝업이 열려 있음
→ B 키 건축 모드 진입 차단

건축 모드 중 팝업 열림
→ 건축 모드 자동 종료
```

---

#### 14. 인벤토리 프리팹 초기화 실패 진단 보완

초기 버전에서는 아이템 상세, 장비, 제작 UI 중 하나라도 초기화에 실패하면 전체 인벤토리 팝업 생성이 중단되었다.

이를 다음처럼 구분하였다.

```text
핵심 InventorySlotsUI 초기화 실패
→ 인벤토리 팝업 초기화 중단

아이템 상세 UI 초기화 실패
→ 해당 기능 비활성화
→ 인벤토리 팝업은 계속 표시

장비 UI 초기화 실패
→ 해당 기능 비활성화
→ 인벤토리 팝업은 계속 표시

제작 UI 초기화 실패
→ 해당 기능 비활성화
→ 인벤토리 팝업은 계속 표시
```

실패한 오브젝트 이름과 컴포넌트 종류를 Console에 출력하여 Inspector 참조 문제를 찾기 쉽게 수정하였다.

---

#### 15. 자식 UI Awake 실행 순서 수정

I 키를 처음 눌렀을 때 다음 UI가 모두 런타임 초기화 실패를 출력하는 문제가 발생하였다.

```text
ItemDetailPanel
HeadSlot
BodySlot
LegsSlot
FeetSlot
BackpackSlot
EquipmentPanel
Recipe_Axe
Recipe_Pickaxe
```

원인은 `InventoryPopupController.Awake()`에서 자식 `InventoryPopup`을 너무 일찍 비활성화한 것이었다.

기존 실행 순서는 다음과 같았다.

```text
PF_UI_InventoryPopup 생성
→ 부모 InventoryPopupController.Awake()
→ InventoryPopup 즉시 비활성화
→ 일부 자식 UI Awake 실행 전 중단
→ 자식 Initialize 호출
→ internalReferencesValid가 false
→ 런타임 초기화 실패
```

이를 다음 순서로 수정하였다.

```text
PF_UI_InventoryPopup 생성
→ 부모와 자식 UI Awake 완료
→ Player 관련 런타임 참조 전달
→ 아이템 상세·장비·제작 UI Initialize 완료
→ 마지막에 InventoryPopup 비활성화
→ 실제 열기 요청에서 다시 활성화
```

핵심 수정 내용은 다음과 같다.

```text
InventoryPopupController.Awake()
→ popupPanel.SetActive(false) 제거

InventoryPopupController.Initialize()
→ 모든 자식 UI 초기화 후 popupPanel.SetActive(false) 실행
```

이를 통해 런타임 생성 인스턴스의 다음 필드가 정상적으로 채워지도록 수정하였다.

```text
InventoryDetailUI.Player Inventory
InventoryDetailUI.Item Dropper
InventoryDetailUI.Player Equipment

EquipmentSlotUI.Player Equipment

EquipmentStatsUI.Player Equipment

CraftingRecipeButton.Player Inventory
CraftingRecipeButton.Crafting Manager
```

---

### 주요 수정 파일

```text
Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_InventoryPopup.prefab
Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity

Assets/_ProjectU/Scripts/Building/BuildPlacementController.cs
Assets/_ProjectU/Scripts/Crafting/CraftingFacilityInteractable.cs
Assets/_ProjectU/Scripts/Crafting/CraftingRecipeButton.cs
Assets/_ProjectU/Scripts/Equipment/EquipmentSlotUI.cs
Assets/_ProjectU/Scripts/Equipment/EquipmentStatsUI.cs
Assets/_ProjectU/Scripts/Inventory/InventoryDetailUI.cs
Assets/_ProjectU/Scripts/Inventory/InventoryPopupController.cs
Assets/_ProjectU/Scripts/UI/Core/GameUIManager.cs
```

---

### 최종 Inspector 구성

#### GameUIManager

```text
Gameplay Input Lock
→ Scene의 GameplayInputLock

Player Inventory
→ Player의 PlayerInventory

Inventory Item Dropper
→ Player의 InventoryItemDropper

Player Equipment
→ Player의 PlayerEquipment

Crafting Manager
→ Player의 CraftingManager

Popup Layer
→ Canvas/PopupLayer

Inventory Popup Prefab
→ PF_UI_InventoryPopup

Storage Popup Prefab
→ PF_UI_StoragePopup
```

#### 화면 하단 HotbarPanel

```text
Player Inventory
→ Player의 PlayerInventory

Visible Slot Count
→ 8

Start Slot Index
→ 0
```

#### PF_UI_InventoryPopup 내부 런타임 필드

```text
InventorySlotsUI.Player Inventory
→ None

InventoryDetailUI.Player Inventory
→ None

InventoryDetailUI.Item Dropper
→ None

InventoryDetailUI.Player Equipment
→ None

EquipmentSlotUI.Player Equipment
→ None

EquipmentStatsUI.Player Equipment
→ None

CraftingRecipeButton.Player Inventory
→ None

CraftingRecipeButton.Crafting Manager
→ None
```

위 필드들은 게임 실행 중 `GameUIManager`가 자동으로 연결한다.

---

### 테스트 항목

#### 게임 시작 상태

- `PopupLayer`에 인벤토리 팝업이 아직 생성되지 않는지 확인
- 화면 하단 Hotbar 8칸이 정상적으로 표시되는지 확인
- 숫자키 1~8 선택 표시가 정상인지 확인
- Console에 참조 누락 오류가 없는지 확인

#### 일반 인벤토리 최초 생성

- I 키 최초 입력 시 `PF_UI_InventoryPopup`이 생성되는지 확인
- `InventoryPopup`이 화면에 표시되는지 확인
- 플레이어 이동과 카메라 입력이 잠기는지 확인
- 커서가 표시되는지 확인

#### 인벤토리 종료와 재사용

- I 키 재입력으로 닫히는지 확인
- ESC 키로 닫히는지 확인
- 닫힌 뒤 Player 입력이 복구되는지 확인
- 다시 열었을 때 기존 프리팹 인스턴스를 재사용하는지 확인
- `PopupLayer` 아래 인벤토리 팝업이 한 개만 존재하는지 확인

#### 아이템 상세 기능

- 슬롯 선택 시 아이템 이름이 표시되는지 확인
- 아이템 분류와 설명이 표시되는지 확인
- 수량이 정상적으로 표시되는지 확인
- REMOVE ONE 기능이 작동하는지 확인
- DROP ONE 기능이 작동하는지 확인
- 장비 아이템의 EQUIP 버튼이 활성화되는지 확인

#### 장비 기능

- 머리 장비 장착과 해제가 가능한지 확인
- 몸 장비 장착과 해제가 가능한지 확인
- 다리 장비 장착과 해제가 가능한지 확인
- 신발 장비 장착과 해제가 가능한지 확인
- 가방 장착과 해제가 가능한지 확인
- 장비 능력치가 즉시 갱신되는지 확인
- 가방 장착 시 인벤토리 슬롯이 확장되는지 확인

#### 제작 기능

- 맨손 제작법 상태가 정상적으로 표시되는지 확인
- 작업대가 필요한 제작법이 `WRONG FACILITY`로 표시되는지 확인
- 재료 부족 상태가 정상적으로 표시되는지 확인
- 조건 충족 시 제작 버튼이 활성화되는지 확인
- 제작 후 재료와 결과 아이템 수량이 갱신되는지 확인

#### 작업대 상호작용

- 작업대를 바라보고 F 입력 시 일반 인벤토리가 열리는지 확인
- 작업대 제작법이 활성화되는지 확인
- 인벤토리를 닫으면 기본 Hand 제작 상태로 복구되는지 확인

#### 건축 모드

- 팝업이 닫힌 상태에서 B 키가 작동하는지 확인
- 인벤토리 열린 상태에서 B 키가 차단되는지 확인
- 건축 모드 중 I 키를 누르면 건축 모드가 종료되는지 확인
- 인벤토리 종료 후 B 키가 다시 작동하는지 확인

#### 보관함 전환

- 보관함을 연 상태에서 I 키를 누르면 보관함이 닫히는지 확인
- 일반 인벤토리가 이어서 열리는지 확인
- 두 팝업이 동시에 표시되지 않는지 확인
- 최종 팝업 종료 후 입력 잠금이 해제되는지 확인

---

### 해결한 문제

```text
일반 인벤토리가 Scene에 항상 존재하던 구조
→ 런타임 프리팹 지연 생성 방식으로 변경

I 키를 눌러도 반응하지 않던 문제
→ GameUIManager 입력 처리와 프리팹 참조 연결

B 키를 눌러도 건축 모드가 실행되지 않던 문제
→ BuildPlacementController의 이전 팝업 참조 제거

화면 하단 Hotbar 슬롯이 표시되지 않던 문제
→ Scene HotbarPanel의 PlayerInventory 참조 복구

아이템 상세 UI 런타임 참조 오류
→ Player 시스템 런타임 전달

장비 슬롯과 장비 능력치 런타임 참조 오류
→ PlayerEquipment 런타임 전달

제작 버튼 런타임 참조 오류
→ PlayerInventory와 CraftingManager 런타임 전달

Player Equipment가 비어 보이던 문제
→ 프리팹과 런타임 인스턴스의 참조 구조 구분

자식 UI Awake 이전 비활성화 문제
→ 모든 자식 UI 초기화 후 팝업 숨김
```

---

### 작업 결과

일반 인벤토리 UI를 Gameplay Scene 상시 배치 방식에서 런타임 프리팹 생성 방식으로 전환하였다.

게임 시작 시에는 일반 인벤토리 팝업을 생성하지 않으며, I 키 최초 입력 또는 제작 시설 상호작용 시 팝업을 생성한다.

생성된 인벤토리 팝업은 제거하지 않고 재사용하며, 보관함 팝업과 동일한 `PopupLayer`, `GameUIManager`, `GameplayInputLock` 구조를 사용한다.

인벤토리 내부의 슬롯, 아이템 상세, 장비, 장비 능력치와 제작 UI는 Scene의 Player 시스템을 직접 참조하지 않고 런타임 초기화 방식으로 연결된다.

화면 하단 Hotbar, I 키 인벤토리 입력, B 키 건축 입력과 제작 시설 상호작용도 새로운 공통 UI 관리 구조에 맞게 수정하였다.

마지막으로 부모 팝업이 자식 UI의 `Awake()` 실행 전에 비활성화되던 초기화 순서 문제를 수정하여, 인벤토리 최초 생성 시 발생하던 아이템 상세·장비·제작 UI의 연쇄 참조 오류를 해결하였다.

---

### 커밋 정보

```text
55일차 : 일반 인벤토리 UI 지연 생성 및 런타임 참조 연결
```
