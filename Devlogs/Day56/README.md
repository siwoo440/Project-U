# Project U 개발 일지

## 56일차 : 공통 UI 런타임 검증 및 Inspector Tooltip 적용

### 개발 목표

53~55일차에 걸쳐 일반 인벤토리와 보관함 UI를 `GameUIManager`, `GameplayInputLock`, `PopupLayer` 중심의 런타임 프리팹 구조로 변경하였다.

56일차에는 변경된 공통 UI 구조가 정상적으로 유지되는지 자동으로 검사하고, 관리자 상태와 실제 팝업 표시 상태가 어긋났을 때 자동으로 복구할 수 있는 검증 시스템을 추가하였다.

또한 Unity Inspector에 노출되는 각 필드의 의미를 바로 확인할 수 있도록 프로젝트 전체 직렬화 필드에 한국어 Tooltip을 일괄 적용하는 Editor 도구를 구현하였다.

---

### 구현 내용

#### 1. Gameplay UI 런타임 검증 시스템 추가

다음 스크립트를 새로 추가하였다.

```text
Assets/_ProjectU/Scripts/Debug/GameplayUIRuntimeValidator.cs
```

`GameplayUIRuntimeValidator`는 Gameplay Scene의 공통 UI 구성과 런타임 상태를 검사한다.

검사 대상은 다음과 같다.

```text
GameUIManager
GameplayInputLock
EventSystem
PlayerInventory
화면 하단 고정 Hotbar
BuildPlacementController
PopupLayer
일반 인벤토리 팝업 프리팹
보관함 팝업 프리팹
```

---

#### 2. 핵심 관리자 중복 검사

Gameplay Scene에 다음 관리자가 정확히 한 개만 존재하는지 검사하도록 구현하였다.

```text
GameUIManager
GameplayInputLock
EventSystem
```

관리자가 없거나 두 개 이상 존재하면 Console에 오류를 출력한다.

예시:

```text
GameUIManager가 Scene에 2개 있습니다. 정확히 1개만 유지해야 합니다.
GameplayInputLock이 Scene에 0개 있습니다. 정확히 1개만 유지해야 합니다.
EventSystem이 Scene에 2개 있습니다. UI 입력을 위해 정확히 1개만 유지해야 합니다.
```

---

#### 3. Scene 필수 참조 검사

다음 Scene 참조가 정상적으로 연결되어 있는지 검사한다.

```text
Game UI Manager
Gameplay Input Lock
Player Inventory
Fixed Hotbar Slots UI
Build Placement Controller
Popup Layer
```

누락된 참조는 자동 검색을 시도하며, 검색에 실패하면 Console에 오류를 출력한다.

Inspector의 Context Menu를 통해 다음 기능도 실행할 수 있다.

```text
Resolve Scene References
```

해당 기능은 Scene에서 다음 컴포넌트를 자동으로 검색한다.

```text
GameUIManager
GameplayInputLock
PlayerInventory
BuildPlacementController
PopupLayer
Hotbar 계층의 InventorySlotsUI
```

---

#### 4. 화면 하단 고정 Hotbar 검사

Scene에 항상 존재하는 화면 하단 Hotbar가 정상적으로 초기화되었는지 검사한다.

검사 항목은 다음과 같다.

```text
InventorySlotsUI 참조 존재
Scene 오브젝트 참조 여부
컴포넌트 활성 상태
GameObject 활성 상태
Play Mode 슬롯 초기화 완료 여부
Hotbar 계층 아래 배치 여부
```

게임 실행 후 `InventorySlotsUI.IsInitialized`가 `false`라면 다음 설정을 확인하도록 오류를 출력한다.

```text
Player Inventory
Slot Container
Slot Template
Visible Slot Count
Start Slot Index
```

---

#### 5. 팝업 프리팹 구성 검사

다음 팝업 프리팹이 연결되어 있는지 검사한다.

```text
PF_UI_InventoryPopup
PF_UI_StoragePopup
```

일반 인벤토리 프리팹은 다음 구성도 추가로 검사한다.

```text
프리팹 루트 활성 상태
InventoryPopup 자식 존재 여부
InventoryPopup 자식 활성 상태
InventorySlotsUI 구성 개수
```

`InventoryPopup` 자식은 프리팹 에셋에서 활성 상태를 유지해야 한다.

런타임 생성 직후 자식 UI의 `Awake()`와 외부 참조 초기화가 완료된 다음 `InventoryPopupController`가 실제 화면을 숨긴다.

---

#### 6. 팝업 중복 생성 검사

`PopupLayer` 아래에 생성된 다음 컴포넌트 수를 검사한다.

```text
InventoryPopupController
StorageContainerUI
```

각 팝업은 최대 한 개만 존재해야 한다.

잘못된 상태:

```text
PopupLayer
├─ PF_UI_InventoryPopup
├─ PF_UI_InventoryPopup
└─ PF_UI_StoragePopup
```

중복이 발견되면 다음 형식으로 경고를 출력한다.

```text
인벤토리 팝업 중복 2개
보관함 팝업 중복 2개
```

---

#### 7. 관리자 상태와 실제 팝업 상태 검사

`GameUIManager.CurrentPopupType`과 실제 팝업 화면 표시 상태가 일치하는지 검사한다.

검사하는 상태는 다음과 같다.

```text
CurrentPopupType: None
CurrentPopupType: Inventory
CurrentPopupType: Storage
```

발견 가능한 불일치는 다음과 같다.

```text
관리자는 Inventory 상태지만 실제 인벤토리가 닫힘
관리자는 Storage 상태지만 실제 보관함이 닫힘
관리자는 None 상태지만 실제 팝업이 열림
인벤토리와 보관함이 동시에 열림
```

---

#### 8. 인벤토리 실제 표시 상태 추가

기존 `InventoryPopupController.IsOpen`은 관리자 요청에 따라 변경되는 논리 상태였다.

Hierarchy에서 `InventoryPopup` 자식을 직접 비활성화하거나 예외 상황으로 실제 화면만 꺼진 경우에는 논리 상태와 화면 상태가 달라질 수 있었다.

이를 해결하기 위해 다음 속성을 추가하였다.

```csharp
public bool IsVisible => popupPanel != null && popupPanel.activeSelf;
```

검증 시스템은 인벤토리 화면 상태를 검사할 때 `IsOpen`이 아니라 `IsVisible`을 사용한다.

이를 통해 다음 상황도 감지할 수 있다.

```text
GameUIManager CurrentPopupType: Inventory
InventoryPopupController IsOpen: true
InventoryPopup 실제 Active: false
```

---

#### 9. 팝업 상태 자동 복구

관리자 상태와 실제 화면 상태가 다르면 `GameUIManager`의 기존 종료 기능을 사용하여 상태를 정리한다.

자동 복구 대상은 다음과 같다.

```text
인벤토리와 보관함 동시 표시
관리자는 Inventory인데 실제 인벤토리 닫힘
관리자는 Storage인데 실제 보관함 닫힘
관리자는 None인데 실제 팝업 표시
```

복구 시 다음 상태를 함께 정리한다.

```text
현재 팝업 종류
인벤토리 화면 상태
보관함 화면 상태
GameplayInputLock
커서 표시 상태
플레이어 입력 상태
```

Inspector Context Menu에서 다음 기능을 수동으로 실행할 수도 있다.

```text
Repair Current Popup State
```

---

#### 10. 입력 잠금 중 건축 관리자 오검출 수정

`GameplayInputLock`은 팝업을 열거나 Alt 키를 누르면 `BuildPlacementController`를 의도적으로 비활성화한다.

초기 검증 코드에서는 `BuildPlacementController.enabled`가 `false`인 모든 상황을 오류로 판단하였다.

이로 인해 다음 정상 상황에서도 경고가 출력될 수 있었다.

```text
일반 인벤토리 열림
보관함 열림
Alt 키 입력
다른 UI 입력 잠금 활성화
```

검사 조건을 다음처럼 수정하였다.

```text
BuildPlacementController가 비활성화됨
+
GameplayInputLock이 잠겨 있지 않음
→ 실제 오류로 판단
```

입력 잠금에 의해 정상적으로 비활성화된 경우에는 오류를 출력하지 않는다.

---

#### 11. 반복 Console 로그 방지

런타임 검증은 지정된 간격마다 실행되지만 같은 문제가 계속 유지되는 동안 동일한 로그를 반복 출력하지 않도록 구성하였다.

동작 방식은 다음과 같다.

```text
현재 오류 목록을 문자열로 조합
→ 이전 오류 상태와 비교
→ 동일하면 출력 생략
→ 오류 종류가 변경되면 새 경고 출력
→ 모든 오류가 사라지면 상태 초기화
```

이를 통해 Console이 같은 경고로 계속 채워지는 문제를 방지하였다.

---

### Inspector Tooltip 시스템

#### 12. Inspector Tooltip 일괄 적용 Editor 도구 추가

다음 Editor 전용 스크립트를 추가하였다.

```text
Assets/_ProjectU/Editor/ProjectUInspectorTooltipApplier.cs
```

이 스크립트는 다음 경로의 런타임 스크립트를 검사한다.

```text
Assets/_ProjectU/Scripts
```

`Editor` 폴더 안의 스크립트는 검사 대상에서 제외한다.

Editor 전용 폴더에 있으므로 게임 빌드에는 포함되지 않는다.

---

#### 13. Tooltip Editor 메뉴 추가

Unity 상단 메뉴에 다음 기능을 추가하였다.

```text
Tools
└─ Project U
   └─ Inspector Tooltips
      ├─ 1. Preview Missing Tooltips
      ├─ 2. Apply Tooltips From Korean Comments
      └─ 3. Validate Tooltip Coverage
```

각 메뉴의 역할은 다음과 같다.

```text
Preview Missing Tooltips
→ 파일을 변경하지 않고 적용 대상과 예상 결과 확인

Apply Tooltips From Korean Comments
→ 누락된 Tooltip을 실제 스크립트에 추가

Validate Tooltip Coverage
→ 모든 Inspector 직렬화 필드의 Tooltip 적용 여부 검사
```

---

#### 14. 한국어 주석 기반 Tooltip 생성

필드 끝에 작성된 한국어 주석을 Tooltip 설명으로 사용한다.

적용 전:

```csharp
[SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자
```

적용 후:

```csharp
[Tooltip("공통 게임 UI 관리자.")]
[SerializeField] private GameUIManager gameUIManager; // 공통 게임 UI 관리자
```

기존 코드의 필드 이름, 형식, 값과 Inspector 직렬화 정보는 변경하지 않는다.

---

#### 15. 기본 Tooltip 자동 생성

필드 끝에 설명 주석이 없는 경우 변수 이름과 형식을 이용하여 기본 Tooltip을 생성한다.

bool 필드:

```csharp
[Tooltip("Validate On Start 기능의 사용 여부를 설정합니다.")]
[SerializeField] private bool validateOnStart;
```

숫자 필드:

```csharp
[Tooltip("Monitor Interval에 사용할 수치를 설정합니다.")]
[SerializeField] private float monitorInterval;
```

오브젝트 참조 필드:

```csharp
[Tooltip("Main Camera에 사용할 Scene 오브젝트, 컴포넌트 또는 에셋을 연결합니다.")]
[SerializeField] private Camera mainCamera;
```

배열과 List 필드:

```csharp
[Tooltip("Build Recipes에 사용할 요소 목록을 설정합니다.")]
[SerializeField] private BuildRecipeData[] buildRecipes;
```

---

#### 16. Tooltip 적용 대상 확장

초기 도구는 `[SerializeField]` 필드만 검사하였다.

이 방식으로는 Inspector에 표시되는 다음 필드들이 누락될 수 있었다.

```text
[SerializeReference] 필드
public 직렬화 필드
중첩 [Serializable] 데이터의 public 필드
배열과 List Element 내부 데이터 필드
```

검사 범위를 다음과 같이 확장하였다.

```text
[SerializeField]
[SerializeReference]
Inspector에 표시되는 public 필드
중첩 [Serializable] 클래스의 public 필드
Header·Range·Min·TextArea 등 다른 속성이 붙은 필드
여러 줄로 선언된 배열과 List 필드
```

---

#### 17. Inspector에 표시되지 않는 필드 제외

다음 항목은 Unity Inspector 직렬화 대상이 아니므로 Tooltip 적용에서 제외한다.

```text
public static 필드
public const 필드
public readonly 필드
public event
delegate
프로퍼티
메서드
생성자
[NonSerialized] 필드
[HideInInspector] 필드
```

이를 통해 Inspector에 표시되지 않는 코드에 불필요한 Tooltip이 추가되는 것을 방지하였다.

---

#### 18. 연속 직렬화 필드 Tooltip 누락 수정

Tooltip 일괄 적용 후 각 필드 묶음의 첫 번째 요소에는 Tooltip이 적용되지만, 바로 아래에 이어지는 필드에는 Tooltip이 적용되지 않는 문제가 발생하였다.

예시:

```csharp
[Tooltip("선택 대상 인벤토리.")]
[SerializeField] private PlayerInventory playerInventory;
[SerializeField] private InventoryPopupController popupController;
```

Inspector에서는 다음 상태가 되었다.

```text
Player Inventory
→ Tooltip 표시

Popup Controller
→ Tooltip 미표시
```

---

#### 19. 연속 필드 누락 원인

기존 분석기는 `[`로 시작하는 다음 줄을 모두 속성 전용 줄로 판단하였다.

```csharp
[SerializeField] private PlayerInventory playerInventory;
```

하지만 위 코드는 속성만 있는 줄이 아니라 속성과 필드 선언이 같은 줄에 존재한다.

도구는 아래 `Popup Controller`를 검사하면서 위쪽 필드 선언까지 현재 필드의 속성 블록으로 포함하였다.

그 결과 첫 번째 필드의 Tooltip을 두 번째 필드의 Tooltip으로 잘못 판단하였다.

```text
Player Inventory의 Tooltip 발견
→ Popup Controller에도 Tooltip이 있다고 오판
→ Popup Controller Tooltip 추가 생략
```

---

#### 20. 속성 전용 줄과 필드 선언 줄 구분

다음 두 형식을 구분하도록 파서를 수정하였다.

속성만 존재하는 줄:

```csharp
[Header("References")]
[Tooltip("설명")]
```

속성과 필드 선언이 같은 줄:

```csharp
[SerializeField] private PlayerInventory playerInventory;
[SerializeField, Min(0f)] private float value;
```

수정된 도구는 한 줄의 선행 속성을 제거한 뒤 남은 코드가 있는지 검사한다.

```text
속성 제거 후 남은 코드 없음
→ 속성 전용 줄

속성 제거 후 private, public 등의 선언이 남음
→ 현재 필드 선언 줄
```

---

#### 21. 각 필드 독립 처리

연속된 직렬화 필드를 각각 독립적으로 처리하도록 수정하였다.

수정 후 `HotbarInput`의 결과는 다음과 같다.

```csharp
[Tooltip("선택 대상 인벤토리.")]
[SerializeField] private PlayerInventory playerInventory; // 선택 대상 인벤토리

[Tooltip("인벤토리 팝업 관리자.")]
[SerializeField] private InventoryPopupController popupController; // 인벤토리 팝업 관리자
```

Inspector에서도 각 항목에 개별 Tooltip이 표시된다.

```text
Player Inventory
→ 선택 대상 인벤토리.

Popup Controller
→ 인벤토리 팝업 관리자.
```

---

#### 22. 기존 Tooltip 중복 생성 방지

이미 Tooltip이 있는 필드는 수정하지 않는다.

```csharp
[Tooltip("플레이어 카메라.")]
[SerializeField] private Camera mainCamera;
```

도구를 여러 번 실행하더라도 다음처럼 중복 생성되지 않는다.

```csharp
[Tooltip("플레이어 카메라.")]
[Tooltip("플레이어 카메라.")]
[SerializeField] private Camera mainCamera;
```

기존 Tooltip은 유지하고 누락된 필드에만 새 Tooltip을 추가한다.

---

#### 23. Tooltip 적용 결과 출력

Editor 도구는 실행 후 다음 정보를 Console에 출력한다.

```text
검사 스크립트 수
전체 Inspector 직렬화 필드 수
SerializeField·SerializeReference 수
public 직렬화 필드 수
기존 Tooltip 수
새로 추가한 Tooltip 수
한국어 주석 기반 Tooltip 수
기본 설명 생성 Tooltip 수
변경 파일 수
변경 파일 경로
```

이를 통해 적용 범위와 누락 가능성을 확인할 수 있다.

---

### 주요 수정 및 생성 파일

```text
Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity

Assets/_ProjectU/Scripts/Debug/GameplayUIRuntimeValidator.cs
Assets/_ProjectU/Scripts/Inventory/InventoryPopupController.cs

Assets/_ProjectU/Editor/ProjectUInspectorTooltipApplier.cs
```

Tooltip 일괄 적용으로 `Assets/_ProjectU/Scripts` 아래의 다수 런타임 스크립트에 `[Tooltip]` 속성이 추가되었다.

---

### 최종 Inspector 구성

#### GameplayUIRuntimeValidator

```text
Game UI Manager
Gameplay Input Lock
Player Inventory
Fixed Hotbar Slots UI
Build Placement Controller
Popup Layer

Inventory Popup Prefab
Storage Popup Prefab

Validate On Start
Monitor Runtime State
Repair Popup State Mismatch
Monitor Interval
Log Success Summary
```

모든 필드 이름에 마우스를 올리면 한국어 설명이 표시된다.

#### HotbarInput

```text
Player Inventory
→ 선택 대상 인벤토리.

Popup Controller
→ 인벤토리 팝업 관리자.
```

각 필드가 연속해서 선언되어 있어도 개별 Tooltip이 표시된다.

---

### 테스트 항목

#### Gameplay UI 시작 검증

- 게임 시작 후 검증 성공 메시지가 출력되는지 확인
- GameUIManager가 정확히 한 개인지 확인
- GameplayInputLock이 정확히 한 개인지 확인
- EventSystem이 정확히 한 개인지 확인
- Hotbar가 정상적으로 초기화되는지 확인
- BuildPlacementController가 정상적으로 활성화되는지 확인
- PopupLayer가 활성화되어 있는지 확인

#### 팝업 동작

- I 키로 일반 인벤토리가 열리는지 확인
- I 또는 ESC로 일반 인벤토리가 닫히는지 확인
- F 키로 보관함이 열리는지 확인
- F 또는 ESC로 보관함이 닫히는지 확인
- 일반 인벤토리와 보관함이 동시에 열리지 않는지 확인
- 팝업 인스턴스가 중복 생성되지 않는지 확인

#### 상태 자동 복구

- 일반 인벤토리를 연 뒤 실제 `InventoryPopup` 자식을 비활성화
- 0.5초 후 상태 불일치가 감지되는지 확인
- CurrentPopupType이 정리되는지 확인
- GameplayInputLock이 해제되는지 확인
- 플레이어 이동과 카메라 입력이 복구되는지 확인

#### 입력 잠금 오검출

- 일반 인벤토리를 연 상태에서 건축 관리자 오류가 발생하지 않는지 확인
- 보관함을 연 상태에서 건축 관리자 오류가 발생하지 않는지 확인
- Alt 키를 누른 상태에서 건축 관리자 오류가 발생하지 않는지 확인
- 모든 입력 잠금 해제 후 B 키가 다시 작동하는지 확인

#### Tooltip 적용

- Tooltip 미리보기 메뉴가 정상 동작하는지 확인
- Tooltip 적용 메뉴가 정상 동작하는지 확인
- Tooltip Coverage 검사에 통과하는지 확인
- `[SerializeField]` 필드에 Tooltip이 표시되는지 확인
- `[SerializeReference]` 필드에 Tooltip이 표시되는지 확인
- public 직렬화 필드에 Tooltip이 표시되는지 확인
- 중첩 Serializable 데이터 내부 필드에 Tooltip이 표시되는지 확인
- 연속된 직렬화 필드 모두에 Tooltip이 표시되는지 확인
- 기존 Tooltip이 중복 생성되지 않는지 확인

---

### 해결한 문제

```text
GameUIManager와 실제 팝업 상태가 어긋나는 문제
→ 논리 상태와 실제 화면 상태를 별도로 검사

InventoryPopup 화면 강제 비활성화를 감지하지 못하는 문제
→ InventoryPopupController.IsVisible 추가

인벤토리와 보관함이 동시에 열릴 수 있는 예외 상태
→ 런타임 감시와 자동 복구 추가

입력 잠금 중 BuildPlacementController 비활성화를 오류로 판단하는 문제
→ GameplayInputLock.IsLocked 조건으로 정상 비활성화 제외

동일한 검증 경고가 반복 출력되는 문제
→ 마지막 오류 상태 비교로 중복 로그 차단

Inspector 필드의 역할을 코드에서 확인해야 하는 문제
→ 프로젝트 전체 한국어 Tooltip 일괄 적용

[SerializeField] 외 public 직렬화 필드가 누락되는 문제
→ public 및 SerializeReference 필드 검사 추가

배열과 List 내부 Serializable 필드가 누락되는 문제
→ 중첩 public 필드 검사 추가

각 필드 묶음의 첫 번째 요소만 Tooltip이 적용되는 문제
→ 속성 전용 줄과 필드 선언 줄 구분

Player Inventory에는 Tooltip이 있지만 Popup Controller에는 없는 문제
→ 연속 직렬화 필드를 각각 독립적으로 처리
```

---

### 작업 결과

56일차에는 53~55일차에서 변경한 공통 UI 구조를 안정적으로 유지하기 위한 런타임 검증 시스템을 구현하였다.

게임 시작 시 핵심 UI 관리자와 Scene 참조를 자동으로 검사하고, 실행 중에는 Hotbar 초기화, 팝업 중복 생성, 관리자 상태와 실제 화면 상태의 불일치를 감시한다.

예외 상태가 발견되면 `GameUIManager`의 기존 종료 기능을 사용하여 팝업 화면과 입력 잠금을 안전하게 정리한다.

추가로 프로젝트 전체 Inspector 직렬화 필드에 한국어 Tooltip을 적용하는 Editor 도구를 구현하였다.

이 도구는 `[SerializeField]`, `[SerializeReference]`, public 직렬화 필드와 중첩 Serializable 데이터를 모두 검사하며, 기존 한국어 주석을 Tooltip 설명으로 활용한다.

마지막으로 속성과 필드 선언이 같은 줄에 존재하는 코드를 속성 전용 줄로 잘못 판단하여 각 묶음의 첫 필드만 Tooltip이 적용되던 문제를 수정하였다.

이를 통해 `Player Inventory`, `Popup Controller`처럼 연속해서 선언된 모든 Inspector 필드에서도 개별적인 한국어 설명을 확인할 수 있게 되었다.

---

### 커밋 정보

```text
56일차 : 공통 UI 런타임 검증 및 Inspector Tooltip 적용
```
