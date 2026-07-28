# 프로젝트 U 23일차 개발 일지

## 개발 목표

가방 장착으로 인벤토리 보관함 슬롯이 증가할 때 Content 크기와 세로 스크롤 범위가 즉시 갱신되도록 인벤토리 UI를 안정화했다.

## 구현 내용

### 인벤토리 스크롤 영역 정리

- `InventoryStorageScrollView`의 크기와 세로 스크롤 범위를 조정했다.
- `Viewport`가 Scroll View 영역을 채우도록 Anchor를 수정했다.
- `InventoryStorageContent`가 슬롯 행 수에 맞춰 세로로 확장되도록 설정했다.
- `ScrollRect`에서 가로 스크롤 연결을 제거하고 세로 스크롤만 사용하도록 정리했다.
- 세로 스크롤바를 항상 표시하도록 설정해 동작 상태를 확인할 수 있게 했다.

### 동적 레이아웃 갱신 기능 추가

- `InventorySlotsUI`에 다음 프레임 레이아웃 갱신 처리를 추가했다.
- 가방 장착과 해제로 슬롯이 다시 생성된 뒤 Content 크기를 강제로 재계산하도록 했다.
- Canvas와 `LayoutRebuilder`를 갱신해 `ScrollRect`의 이동 범위가 즉시 반영되도록 했다.
- 슬롯 수가 변경되면 스크롤 위치가 맨 위로 초기화되도록 했다.
- UI 비활성화 시 실행 중인 레이아웃 갱신 코루틴을 안전하게 종료하도록 했다.

### 핫바 ScrollRect 오류 수정

- `HotbarPanel`과 `InventoryHotbarArea`에는 `ScrollRect`가 없는 것이 정상임을 확인했다.
- 모든 `InventorySlotsUI`가 `ScrollRect`를 필수로 찾던 처리를 수정했다.
- 실제 `slotContainer`를 기준으로 상위 `ScrollRect`를 검색하도록 변경했다.
- 일반 핫바 영역은 스크롤 갱신을 오류 없이 건너뛰도록 처리했다.
- `Slot Container`가 연결되지 않은 경우에만 실제 연결 오류를 출력하도록 정리했다.

## 수정 파일

- `Assets/_ProjectU/Scripts/Inventory/InventorySlotsUI.cs`
- `Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity`

## 테스트 결과

- 가방 미착용 시 보관함 24칸 유지
- 가방 장착 시 보관함 32칸으로 증가
- 가방 장착과 해제 후 슬롯 UI 재생성 확인
- 핫바 영역의 `Content 또는 ScrollRect를 찾을 수 없습니다` 오류 제거
- 핫바 선택과 인벤토리 슬롯 드래그 기능 유지
- 추가 슬롯에 아이템이 있을 때 가방 해제 차단 기능 유지

## 추가 확인 항목

- 가방 장착 후 보관함 네 번째 줄까지 마우스 휠로 이동되는지 확인
- 세로 스크롤바 Handle을 직접 드래그할 수 있는지 확인
- `InventoryStorageContent` 높이가 슬롯 행 수에 따라 정상적으로 변경되는지 확인
- 장시간 반복 장착과 해제 시 아이템 복제 또는 소실이 없는지 확인
- Console에 새로운 오류가 발생하지 않는지 확인

## 23일차 완료 내용

가방 장착에 따른 동적 슬롯 생성 이후 인벤토리 레이아웃을 갱신하는 구조를 추가했다. 스크롤 기능이 없는 핫바 영역에서 발생하던 잘못된 `ScrollRect` 오류를 제거하고, 실제 보관함 스크롤 영역만 선택적으로 갱신하도록 수정했다.
