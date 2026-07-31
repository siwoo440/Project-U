## 53일차 : 하이러키 정리 및 공통 UI 관리 기반 구축

### 개발 목표

기존 Gameplay Scene에 기능과 UI 오브젝트가 한곳에 혼재되어 있어 관리가 어려운 문제를 개선하고, 인벤토리·보관함·작업대 등 여러 팝업 UI를 안정적으로 확장할 수 있는 공통 관리 기반을 구축한다.

이번 작업에서는 보관함 UI를 실제 런타임 프리팹 생성 방식으로 완전히 전환하기보다, 기존 Scene UI를 유지하면서 공통 UI 관리자와 입력 잠금 구조를 먼저 적용하는 것을 목표로 하였다.

### 구현 내용

#### 1. Gameplay Scene 하이러키 정리

Gameplay Scene의 오브젝트를 역할별로 구분할 수 있도록 하이러키 구조를 정리하였다.

게임 관리 시스템, Player, World 오브젝트, Runtime 생성 오브젝트, UI와 Debug 오브젝트를 구분하여 이후 기능을 추가하거나 문제를 확인하기 쉽게 구성하였다.

기존 오브젝트의 부모를 변경할 때 월드 위치와 Inspector 참조가 유지되도록 확인하였다.

#### 2. UI 관리 구조 정리

항상 표시되는 HUD와 필요할 때 표시되는 팝업 UI를 구분할 수 있도록 UI 구조를 정리하였다.

인벤토리와 보관함 UI는 기존 Scene 배치 방식을 유지하되, 이후 프리팹 생성 방식으로 변경할 수 있도록 공통 관리 구조를 준비하였다.

런타임 팝업이 배치될 수 있도록 PopupLayer 사용 구조를 준비하였다.

#### 3. GameplayInputLock 구현

팝업 UI가 열렸을 때 Player와 Camera 입력을 각 UI 스크립트가 개별적으로 차단하던 구조를 공통 입력 잠금 방식으로 변경하였다.

GameplayInputLock을 통해 다음 기능을 한곳에서 비활성화하고 이전 상태로 복구하도록 구성하였다.

- Player 이동
- Camera 회전
- Player 상호작용
- Hotbar 아이템 사용
- 건축 배치 입력
- Hotbar 숫자키 입력

여러 UI 또는 Alt 커서 입력이 동시에 활성화되더라도 하나의 잠금이 해제되었다는 이유로 전체 입력이 잘못 복구되지 않도록 잠금 ID를 구분하였다.

#### 4. GameUIManager 구현

인벤토리와 보관함 팝업의 실행 순서를 공통으로 관리하는 GameUIManager를 추가하였다.

한 번에 하나의 팝업만 활성화되도록 처리하고, 기존 팝업이 열린 상태에서 다른 팝업을 열면 이전 팝업을 먼저 닫도록 구성하였다.

팝업 열기와 닫기에 맞춰 GameplayInputLock을 획득하거나 해제하도록 연결하였다.

#### 5. 인벤토리 팝업 입력 구조 개선

InventoryPopupController가 PlayerMovement와 Camera 같은 컴포넌트를 직접 비활성화하지 않도록 수정하였다.

I 키와 ESC 입력은 InventoryPopupController가 처리하지만, 실제 팝업 실행 순서와 입력 잠금은 GameUIManager와 GameplayInputLock이 담당하도록 역할을 분리하였다.

Alt 키를 통한 커서 표시도 공통 입력 잠금 시스템을 사용하도록 변경하였다.

#### 6. 보관함 팝업 공통 관리 연결

StorageContainerUI가 직접 커서를 변경하는 대신 GameUIManager에 열기와 닫기를 요청하도록 수정하였다.

보관함 UI가 열린 동안 Player 이동, Camera 회전, 상호작용, Hotbar와 건축 입력이 공통으로 차단되도록 연결하였다.

인벤토리와 보관함이 동시에 표시되지 않도록 팝업 전환 처리를 추가하였다.

#### 7. 보관함 프리팹 전환 준비

보관함 UI를 이후 런타임 프리팹 생성 방식으로 변경할 수 있도록 관련 스크립트 구조를 준비하였다.

InventorySlotsUI에 PlayerInventory를 실행 중 전달할 수 있는 초기화 구조를 추가하고, StorageInteractable이 Scene의 StorageContainerUI를 직접 찾는 대신 GameUIManager를 사용하도록 변경할 준비를 진행하였다.

GameplaySaveController도 불러오기 전에 개별 보관함 UI가 아닌 GameUIManager를 통해 현재 팝업을 정리할 수 있도록 변경하였다.

### 현재 미완료 항목

다음 작업은 이번 작업에서 완료하지 않았으며 다음 일차로 이관한다.

- PF_UI_StoragePopup 프리팹 실제 생성
- 기존 Scene StoragePopupController 제거
- GameUIManager의 Storage Popup Prefab 연결
- 최초 보관함 사용 시 프리팹 생성
- 생성된 보관함 팝업 인스턴스 재사용
- 소형 상자와 대형 상자 간 팝업 재사용 테스트
- 저장 및 불러오기와 런타임 보관함 팝업 연동 테스트

### 확인 항목

- GameplayInputLock와 GameUIManager가 Scene에 하나씩 존재하는지 확인
- 일반 인벤토리 I 키 열기와 닫기 확인
- 인벤토리 ESC 닫기 확인
- Alt 커서 표시와 입력 복구 확인
- 보관함 F 상호작용 확인
- 보관함 Close 버튼과 ESC 닫기 확인
- 인벤토리와 보관함 동시 표시 방지 확인
- 작업대 사용 후 기존 인벤토리와 제작 기능 확인
- 팝업 종료 후 Player 이동과 Camera 입력 복구 확인
- Console 오류와 Missing 참조 확인

### 작업 결과

Gameplay Scene의 하이러키 구조를 정리하고, 인벤토리와 보관함 팝업이 각각 입력과 커서를 관리하던 구조를 공통 관리 방식으로 변경하였다.

GameUIManager와 GameplayInputLock을 통해 팝업 실행 순서와 Player 입력 잠금을 중앙에서 관리할 수 있는 기반을 구축하였다.

보관함 UI의 실제 런타임 프리팹 생성과 재사용은 아직 완료하지 않았으며, 다음 개발 일차에서 이어서 구현한다.