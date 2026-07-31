# 프로젝트 U 52일차 개발 일지

## 개발 정보

| 항목 | 내용 |
|---|---|
| 프로젝트 | 프로젝트 U |
| 엔진 | Unity 6000.3.9f1 |
| 개발 일차 | 52일차 |
| 개발 주제 | 기능성 가구 건축 연결 및 상호작용 구현 |
| 대상 플랫폼 | Windows PC / Steam |

---

## 1. 개발 목표

51일차에 구현한 기초·바닥·벽 연결 시스템을 확장하여 보관함, 작업대, 조명, 모닥불, 침낭을 기능성 가구로 설치하고 사용할 수 있도록 구현하였다.

```text
Terrain
└─ Foundation
   ├─ Furniture
   └─ Floor
      ├─ Wall
      └─ Furniture
```

---

## 2. 기능성 가구 구조 확장

`BuildStructureType.Furniture`를 실제 건축 배치에 연결하였다.

적용 대상은 다음과 같다.

- 소형 보관함
- 대형 보관함
- 작업대
- 스탠딩 조명
- 모닥불
- 침낭

`BuildRecipeData`에는 기능성 가구의 Terrain 직접 설치 여부를 정하는 `Allow Ground Placement` 설정을 추가하였다.

모닥불과 침낭은 Terrain과 구조물 양쪽에 설치할 수 있고, 보관함·작업대·조명은 구조물 연결점을 통해 설치한다.

---

## 3. 기초와 바닥 연결점 수정

나무 기초의 `floor_top` 연결점이 기존에는 `Floor`만 허용하여 기초 위에 상자를 직접 설치할 수 없었다.

해당 연결점이 다음 구조 역할을 모두 허용하도록 수정하였다.

```text
Floor
Furniture
```

나무 바닥에는 중앙 가구 연결점인 `furniture_center`를 추가하였다.

하나의 연결점에는 하나의 구조물만 설치할 수 있다.

```text
기초 위에 바닥 설치
→ 바닥의 furniture_center에 가구 설치

기초 위에 가구 직접 설치
→ 같은 연결점에 바닥 설치 불가
```

---

## 4. 기능성 가구 배치 처리

`BuildPlacementController`가 `Furniture` 구조 역할을 별도로 판정하도록 확장하였다.

가구 설치 과정은 다음과 같다.

1. 화면 중앙 구조물 탐지
2. 현재 가구를 허용하는 빈 연결점 검색
3. 연결점 위치와 회전에 미리보기 정렬
4. 충돌과 필요 재료 검사
5. 설치 후 연결점 점유
6. 지지 구조물과 가구 관계 등록

Terrain 설치가 허용된 모닥불과 침낭은 구조물을 바라보면 연결 배치를 사용하고, Terrain을 바라보면 기존 자유 배치를 사용한다.

---

## 5. 소형·대형 보관함 구현 및 수정

보관함에 다음 기능을 구성하였다.

- `StorageContainer`
- `StorageInteractable`
- `WorldObjectIdentity`
- `PlacedBuildObject`
- 상자 메시와 Renderer
- 충돌체
- 보관함 종류 데이터

초기 보관함 프리팹에는 Collider만 있고 `MeshFilter`와 `MeshRenderer`가 없어 미리보기와 설치 결과가 보이지 않는 문제가 있었다.

각 보관함에 `ChestMesh`를 추가하고 프리팹 루트 위치를 원점으로 수정하였다.

```text
Position: 0, 0, 0
Rotation: 0, 0, 0
Scale: 1, 1, 1
Layer: Structure
```

대형 보관함 루트가 `Default` 레이어로 저장된 문제도 수정하였다.

대형 보관함의 Recipe ID는 다음 형식으로 통일하였다.

```text
structure_large_chest
```

---

## 6. 보관함 고유 ID와 철거 제한

설치형 보관함 프리팹에 남아 있던 Debug 고정 ID를 제거하였다.

```text
World Object Id: 비움
Debug Structure Id: 비움
```

실제 설치 시 `PlacedBuildObject.Initialize()`가 각 보관함에 새로운 Runtime ID를 발급한다.

이를 통해 여러 보관함이 서로 다른 저장 ID와 내부 아이템 데이터를 사용한다.

`StorageContainer`에는 `IBuildRemovalGuard`를 적용하였다.

아이템이 남아 있는 보관함은 철거할 수 없으며 다음 문구가 표시된다.

```text
EMPTY STORAGE FIRST
```

---

## 7. 보관함 상호작용

`StorageInteractable`을 추가하여 설치한 보관함을 바라보고 F 키로 기존 보관함 UI를 열 수 있도록 구현하였다.

```text
플레이어가 보관함을 바라봄
→ F 입력
→ StorageContainer 확인
→ StorageContainerUI 열기
→ 아이템 이동
```

소형 보관함과 대형 보관함은 서로 독립된 슬롯과 저장 데이터를 사용한다.

---

## 8. 작업대 제작 기능

작업대에 `CraftingFacilityInteractable`을 추가하였다.

```text
F - USE WORKBENCH
→ 현재 제작 시설을 Workbench로 변경
→ 인벤토리·제작 팝업 열기
→ 작업대 전용 제작
→ 팝업 닫기
→ Hand 제작으로 복귀
```

`CraftingManager`에는 제작 시설 변경 기능과 변경 이벤트를 추가하였다.

`CraftingRecipeButton`은 제작 시설이 바뀌면 제작 상태를 즉시 갱신한다.

| 제작법 | 필요 시설 |
|---|---|
| 도끼 | Hand |
| 곡괭이 | Workbench |

---

## 9. 스탠딩 조명 구현

스탠딩 조명에 다음 기능을 구성하였다.

- `PlacedBuildObject`
- `WorldObjectIdentity`
- `ToggleLightInteractable`
- `Point Light`
- 조명 메시와 충돌체

F 키로 점등 상태를 전환한다.

```text
F - TURN OFF LIGHT
F - TURN ON LIGHT
```

Preview 프리팹에 실제 `Point Light`가 포함되어 미리보기가 주변을 밝히는 문제를 수정하였다. Preview에는 메시만 유지하였다.

---

## 10. 모닥불과 침낭 연결

모닥불과 침낭의 구조 역할을 `Furniture`로 변경하였다.

두 건축물은 다음 배치를 지원한다.

```text
Terrain 자유 배치
기초 연결점 배치
바닥 furniture_center 배치
```

기존 기능은 유지된다.

- 모닥불: 조리, 연료, 열기, 결과물 회수, 철거 제한
- 침낭: 수면, 시간 진행, 부활 지점 등록

---

## 11. 지지 관계와 철거 순서

가구가 연결점에 설치되면 지지 관계가 등록된다.

```text
Foundation
└─ Floor
   └─ Small Chest
```

정상 철거 순서는 다음과 같다.

```text
Small Chest
→ Floor
→ Foundation
```

가구가 남은 구조물을 철거하면 다음 문구가 표시된다.

```text
REMOVE SUPPORTED STRUCTURES FIRST
```

---

## 12. 저장 시스템 연결

새 기능성 가구 Recipe를 다음 목록에 등록하였다.

- `BuildPlacementController.BuildRecipes`
- `PlacedStructureSaveBridge.BuildRecipes`

등록 대상은 다음과 같다.

- Wood Foundation
- Wood Floor
- Wood Wall
- Small Chest
- Large Chest
- Workbench
- Standing Lamp
- Campfire
- Sleeping Bag

설치형 보관함은 Runtime Structure ID를 이용해 내부 아이템을 저장한다.

---

## 13. 주요 생성 파일

| 파일 | 내용 |
|---|---|
| `BuildRecipe_SmallChest.asset` | 소형 보관함 건축 데이터 |
| `BuildRecipe_LargeChest.asset` | 대형 보관함 건축 데이터 |
| `BuildRecipe_Workbench.asset` | 작업대 건축 데이터 |
| `BuildRecipe_StandingLamp.asset` | 조명 건축 데이터 |
| `SmallChestPlaced.prefab` | 소형 보관함 |
| `SmallChestPreview.prefab` | 소형 보관함 미리보기 |
| `LargeChestPlaced.prefab` | 대형 보관함 |
| `LargeChestPreview.prefab` | 대형 보관함 미리보기 |
| `WorkbenchPlaced.prefab` | 작업대 |
| `WorkbenchPreview.prefab` | 작업대 미리보기 |
| `StandingLampPlaced.prefab` | 스탠딩 조명 |
| `StandingLampPreview.prefab` | 조명 미리보기 |
| `StorageInteractable.cs` | 보관함 상호작용 |
| `CraftingFacilityInteractable.cs` | 작업대 상호작용 |
| `ToggleLightInteractable.cs` | 조명 점등 전환 |

---

## 14. 주요 수정 파일

| 파일 | 내용 |
|---|---|
| `BuildPlacementController.cs` | Furniture 연결 배치 지원 |
| `BuildRecipeData.cs` | 구조 역할과 Terrain 배치 설정 |
| `StorageContainer.cs` | 설치형 보관함 초기화와 철거 제한 |
| `InventoryPopupController.cs` | 팝업 상태 변경 이벤트 |
| `CraftingManager.cs` | 제작 시설 변경 기능 |
| `CraftingRecipeButton.cs` | 시설 변경 시 UI 갱신 |
| `WoodFoundationPlaced.prefab` | Furniture 연결 허용 |
| `WoodFloorPlaced.prefab` | 가구 중앙 연결점 추가 |
| `BuildRecipe_Campfire.asset` | Furniture 설정 |
| `BuildRecipe_SleepingBag.asset` | Furniture 설정 |
| `CraftingRecipe_Axe.asset` | Hand 제작 설정 |
| `CraftingRecipe_Pickaxe.asset` | Workbench 제작 설정 |
| `20_Gameplay.unity` | Recipe 및 저장 목록 연결 |

---

## 15. 동작 확인

- 기초 위 소형·대형 보관함 설치 가능
- 바닥 위 기능성 가구 설치 가능
- 하나의 연결점에 하나의 구조물만 설치 가능
- 보관함 미리보기와 실제 메시 정상 표시
- 보관함 F 상호작용 정상
- 보관함별 슬롯 독립 동작
- 내용물이 있는 보관함 철거 차단
- 작업대 전용 제작 정상
- 팝업 종료 후 Hand 제작 복귀
- 조명 켜기·끄기 정상
- 모닥불·침낭 Terrain 배치 유지
- 모닥불·침낭 구조물 연결 배치 가능
- 가구가 남아 있는 지지 구조물 철거 차단
- Unity Console 컴파일 오류 없음

---

## 16. 현재 제한 사항

- 하나의 기초 또는 바닥 연결점에는 가구 하나만 설치 가능
- 설치 후 가구 이동과 회전 편집 미지원
- 조명 점등 상태 저장 미지원
- 불러오기 후 지지 구조 ID와 연결점 점유 관계 복원 미지원
- 이전 Debug 보관함 저장 데이터와 새 보관함 구성이 충돌할 수 있음
- 가구 모델은 임시 형태

지지 구조와 연결점 관계 저장은 54일차에 구현할 예정이다.

---

## 17. 개발 결과

52일차 작업을 통해 구조 건축 시스템이 기능성 가구까지 확장되었다.

플레이어는 기초와 바닥 위에 보관함, 작업대, 조명, 모닥불, 침낭을 설치하고 실제 기능을 사용할 수 있다.

채집 자원을 보관하고, 작업대에서 제작하며, 조명과 모닥불을 사용하고, 침낭에서 수면하는 거점 플레이 기반을 완성하였다.

---

## 18. 다음 개발 방향

53일차에는 건축 연결·교체·철거 규칙을 보완한다.

- 연결점 선택 표시
- 빈 연결점과 사용 중인 연결점 구분
- 설치 불가 원인 안내 개선
- 건축물 교체 기능
- 재료 차액 계산
- 철거 반환 처리 통합
- 설치 실패 재료 복구 검증
- 가구 이동 및 재배치 방식 검토
