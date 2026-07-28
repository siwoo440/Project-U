# Project U 24일차 개발 일지

## 개발 주제

기본 제작 시스템 구현

## 개발 목표

- 수집한 나무와 돌을 제작 재료로 활용
- 제작법을 ScriptableObject 데이터로 관리
- 재료 보유량과 인벤토리 공간에 따른 제작 가능 여부 판정
- 제작 성공 시 재료 차감과 결과 아이템 지급
- 인벤토리 팝업에서 제작 상태 실시간 표시

## 구현 내용

### 1. 제작 데이터 구조

- `CraftingIngredient`에 필요 아이템과 수량 저장
- `CraftingRecipeData`에 제작법 ID, 표시 이름, 결과 아이템, 결과 수량, 필요 재료 목록 저장
- 제작법을 코드 수정 없이 Inspector에서 추가·변경할 수 있도록 구성

### 2. 인벤토리 수량 관리

- 전체 슬롯에 나뉜 동일 아이템의 총수량 조회 기능 추가
- 지정 아이템의 필요 수량 보유 여부 확인 기능 추가
- 제작 결과 아이템을 넣을 공간 확인 기능 추가
- 여러 슬롯에서 지정 수량만큼 재료를 제거하는 기능 추가
- 가방 장착으로 확장된 슬롯도 제작 재료 계산에 포함

### 3. 제작 처리

- 모든 재료의 보유 수량 확인
- 제작 결과를 보관할 인벤토리 공간 확인
- 제작 성공 시 결과 아이템 지급 및 재료 차감
- 처리 도중 수량이 달라질 경우 결과와 재료를 복구하도록 구성
- 재료 부족이나 공간 부족 상태에서는 제작 실행 차단

### 4. 제작 UI

- 인벤토리 팝업에 도끼와 곡괭이 제작 항목 추가
- 제작 결과 이름과 수량 표시
- 재료별 현재 보유량과 필요량 표시
- 제작 상태를 `NEED MATERIALS`, `INVENTORY FULL`, `READY`, `CRAFTED`로 표시
- 인벤토리 변경 시 제작 가능 상태와 버튼 활성 상태 자동 갱신

### 5. 제작법 데이터

| 제작 결과 | 필요 재료 |
|---|---|
| 도끼 1개 | 나무 3개, 돌 2개 |
| 곡괭이 1개 | 나무 2개, 돌 4개 |

## 생성 및 수정 파일

### 생성

- `Assets/_ProjectU/Scripts/Crafting/CraftingIngredient.cs`
- `Assets/_ProjectU/Scripts/Crafting/CraftingRecipeData.cs`
- `Assets/_ProjectU/Scripts/Crafting/CraftingManager.cs`
- `Assets/_ProjectU/Scripts/Crafting/CraftingRecipeButton.cs`
- `Assets/_ProjectU/Data/Crafting/CraftingRecipe_Axe.asset`
- `Assets/_ProjectU/Data/Crafting/CraftingRecipe_Pickaxe.asset`

### 수정

- `Assets/_ProjectU/Scripts/Inventory/PlayerInventory.cs`
- `20_Gameplay` Scene의 플레이어 및 인벤토리 UI 구성

## 테스트 결과

- 재료 부족 시 제작 버튼 비활성화 확인
- 재료 충족 시 제작 버튼 활성화 확인
- 도끼와 곡괭이 제작 후 결과 아이템 지급 확인
- 제작 비용에 맞는 나무와 돌 차감 확인
- 인벤토리 공간 부족 시 제작 차단 확인
- 가방 추가 슬롯의 재료 수량 계산 및 차감 확인
- 제작한 도구의 핫바 이동과 기존 채집 기능 연동 확인
- 제작 후 인벤토리 스크롤 유지 확인
- Console 오류 없이 정상 작동 확인

## 완료 결과

기존 채집 시스템에서 획득한 재료를 실제 도구 제작에 사용하는 기본 제작 흐름을 완성했다. 제작법과 재료를 데이터로 분리하여 이후 건축 재료, 장비, 소비 아이템 등의 제작법을 같은 구조로 확장할 수 있다.
