# Project U 개발 일지

## 73일차 : 구조 부품·가구·시설 건축 콘텐츠 확장

### 1. 개발 목표

73일차에는 새로운 건축 시스템을 추가하지 않고, 기존에 구현된 `BuildRecipeData`, `BuildPlacementController`, 구조 연결, 철거, 저장 시스템을 그대로 활용하여 실제로 사용할 수 있는 건축 콘텐츠를 확장하였다.

이번 작업의 핵심 목표는 다음과 같다.

- 돌 재질 구조 부품 추가
- 나무 가구 콘텐츠 추가
- 기존 모닥불 기능을 재사용한 신규 시설 추가
- 신규 건축물의 Preview 및 충돌 검사 구성
- 건축 Recipe 등록
- 건축 선택 목록 및 저장 목록 등록
- 설치·회전·연결·철거 테스트
- 저장 및 Continue 복원 테스트
- 기존 건축 콘텐츠 회귀 테스트

---

## 2. 추가한 건축 콘텐츠

73일차에는 총 6개의 신규 건축 콘텐츠를 추가하였다.

| 분류 | 건축물 | Recipe ID | 필요 재료 |
| --- | --- | --- | --- |
| 구조물 | Stone Foundation | `structure_stone_foundation` | Stone 6 |
| 구조물 | Stone Floor | `structure_stone_floor` | Stone 4 |
| 구조물 | Stone Wall | `structure_stone_wall` | Stone 5 |
| 가구 | Wood Table | `structure_wood_table` | Wood 4 |
| 가구 | Wood Chair | `structure_wood_chair` | Wood 2 |
| 시설 | Stone Campfire | `structure_stone_campfire` | Stone 8 + Wood 3 |

기존 9개의 건축 Recipe에 신규 6개를 추가하여 총 15개의 건축 콘텐츠를 사용할 수 있도록 확장하였다.

---

## 3. Day73 건축 데이터 폴더 구성

신규 건축 Recipe는 다음 위치에 정리하였다.

```text
Assets/_ProjectU/Data/Building/Day73/
├─ BuildRecipe_StoneFoundation.asset
├─ BuildRecipe_StoneFloor.asset
├─ BuildRecipe_StoneWall.asset
├─ BuildRecipe_WoodTable.asset
├─ BuildRecipe_WoodChair.asset
└─ BuildRecipe_StoneCampfire.asset
```

신규 Prefab은 다음 위치에 정리하였다.

```text
Assets/_ProjectU/Prefabs/Building/Day73/
├─ StoneFoundationPlaced.prefab
├─ StoneFoundationPreview.prefab
├─ StoneFloorPlaced.prefab
├─ StoneFloorPreview.prefab
├─ StoneWallPlaced.prefab
├─ StoneWallPreview.prefab
├─ WoodTablePlaced.prefab
├─ WoodTablePreview.prefab
├─ WoodChairPlaced.prefab
├─ WoodChairPreview.prefab
├─ StoneCampfirePlaced.prefab
└─ StoneCampfirePreview.prefab
```

임시 건축 Material은 다음 폴더에 구성하였다.

```text
Assets/_ProjectU/Art/Materials/Building/Day73/
├─ M_Build_Stone_Day73.mat
└─ M_Build_WoodFurniture_Day73.mat
```

현재는 콘텐츠 구분과 기능 테스트를 위한 임시 외형을 사용하며, 실제 모델과 텍스처는 후반 외형 정리 단계에서 교체할 예정이다.

---

## 4. Stone Foundation 콘텐츠 제작

기존 `WoodFoundationPlaced` 및 Preview를 기반으로 돌 기초 콘텐츠를 제작하였다.

### BuildRecipe 설정

```text
Recipe Id: structure_stone_foundation
Display Name: STONE FOUNDATION
Structure Type: Foundation
Placement Type: Floor
Rotation Step: 90
```

충돌 검사:

```text
Placement Check Center:
0 / 0.1 / 0

Placement Check Half Extents:
0.48 / 0.09 / 0.48
```

Terrain 조건:

```text
Maximum Slope Angle: 12
Maximum Height Difference: 0.1
```

재료:

```text
Stone 6
```

철거 반환율:

```text
0.5
```

기존 Foundation의 구조 연결점을 유지하여 벽 등의 구조 부품을 연결할 수 있도록 하였다.

---

## 5. Stone Floor 콘텐츠 제작

기존 `WoodFloorPlaced` 구조를 기반으로 돌 바닥을 제작하였다.

### BuildRecipe 설정

```text
Recipe Id: structure_stone_floor
Display Name: STONE FLOOR
Structure Type: Floor
Placement Type: Floor
Rotation Step: 45
```

충돌 검사:

```text
Placement Check Center:
0 / 0.05 / 0

Placement Check Half Extents:
0.48 / 0.04 / 0.48
```

재료:

```text
Stone 4
```

Stone Floor는 이번 일차에서 단순한 임시 돌 외형을 사용하도록 구성하였다.

기존 Wood Floor에서 사용하던 자동 Visual 연결 구조를 신규 Stone Floor에 그대로 적용하지 않고, Day73용 임시 Mesh와 Material을 직접 사용하여 아직 존재하지 않는 Stone 전용 Visual Profile 참조 문제를 방지하였다.

---

## 6. Stone Wall 콘텐츠 제작

기존 `WoodWallPlaced`와 Preview를 복제하여 돌 벽 콘텐츠를 제작하였다.

### BuildRecipe 설정

```text
Recipe Id: structure_stone_wall
Display Name: STONE WALL
Structure Type: Wall
Placement Type: Wall
Rotation Step: 45
```

충돌 검사:

```text
Placement Check Center:
0 / 0.75 / 0

Placement Check Half Extents:
0.47 / 0.74 / 0.035
```

Terrain 조건:

```text
Maximum Slope Angle: 20
Maximum Height Difference: 0.15
```

재료:

```text
Stone 5
```

기존 Wall 연결 구조를 유지하여 Foundation 및 Floor의 벽 연결점에 Snap할 수 있도록 구성하였다.

---

## 7. Wood Table 콘텐츠 제작

기존 Workbench Prefab을 템플릿으로 사용하여 단순 가구인 Wood Table을 제작하였다.

테이블은 제작 시설이 아니므로 기존 Workbench에 존재하는 제작 시설 상호작용 기능을 제거하였다.

### 외형 구성

```text
WoodTablePlaced
├─ TableTop
├─ Leg_FL
├─ Leg_FR
├─ Leg_BL
└─ Leg_BR
```

Primitive Cube를 조합하여 임시 테이블 외형을 제작하였다.

### BuildRecipe 설정

```text
Recipe Id: structure_wood_table
Display Name: WOOD TABLE
Structure Type: Furniture
Allow Ground Placement: Off
Placement Type: Free
Rotation Step: 90
```

재료:

```text
Wood 4
```

Terrain 직접 설치는 허용하지 않고 기존 구조 바닥 위에 배치하는 가구로 구성하였다.

---

## 8. Wood Chair 콘텐츠 제작

Wood Table을 기반으로 별도의 의자 Prefab을 제작하였다.

### 외형 구성

```text
WoodChairPlaced
├─ Seat
├─ Back
├─ Leg_FL
├─ Leg_FR
├─ Leg_BL
└─ Leg_BR
```

### BuildRecipe 설정

```text
Recipe Id: structure_wood_chair
Display Name: WOOD CHAIR
Structure Type: Furniture
Allow Ground Placement: Off
Placement Type: Free
Rotation Step: 90
```

재료:

```text
Wood 2
```

Table과 마찬가지로 바닥 구조물 위 자유 배치 가구로 구성하였다.

---

## 9. Stone Campfire 콘텐츠 제작

기존 `CampfirePlaced`와 Preview를 복제하여 Stone Campfire를 제작하였다.

기존 모닥불의 다음 기능은 그대로 유지하였다.

- `CampfireCookingStation`
- 상호작용 기능
- 불빛
- 불꽃 Visual
- 조리 진행 상태
- 저장 및 복원용 모닥불 상태

외형 중 돌 부분만 신규 Stone Material로 변경하였다.

### BuildRecipe 설정

```text
Recipe Id: structure_stone_campfire
Display Name: STONE CAMPFIRE
Structure Type: Furniture
Allow Ground Placement: On
Placement Type: Free
Rotation Step: 45
```

충돌 검사:

```text
Placement Check Center:
0 / 0.3 / 0

Placement Check Half Extents:
0.4 / 0.29 / 0.4
```

재료:

```text
Stone 8
Wood 3
```

기존 Campfire와 마찬가지로 Terrain에 직접 설치할 수 있도록 구성하였다.

---

## 10. BuildPlacementController 등록

신규 6개 Recipe를 `20_Gameplay` Scene의 `BuildPlacementController`에 등록하였다.

기존:

```text
Build Recipes: 9개
```

변경:

```text
Build Recipes: 15개
```

추가 항목:

```text
BuildRecipe_StoneFoundation
BuildRecipe_StoneFloor
BuildRecipe_StoneWall
BuildRecipe_WoodTable
BuildRecipe_WoodChair
BuildRecipe_StoneCampfire
```

건축 모드에서 Z/X 입력을 이용하여 기존 건축물과 신규 건축물을 순환 선택할 수 있도록 하였다.

---

## 11. PlacedStructureSaveBridge 등록

신규 건축물이 저장 및 Continue 과정에서 복원될 수 있도록 `PlacedStructureSaveBridge`의 건축 Recipe 목록에도 동일한 6개 데이터를 추가하였다.

```text
Build Recipes:
9개 → 15개
```

이를 통해 신규 건축물이 저장될 때 각 `Recipe ID`를 정상적으로 인식하고 해당 Placed Prefab을 이용하여 복원할 수 있도록 구성하였다.

---

## 12. GameDataRegistry 갱신

신규 `BuildRecipeData`를 추가한 뒤 GameDataRegistry를 다시 수집하였다.

실행 메뉴:

```text
Project U
→ Data
→ Create Or Refresh Game Data Registry
```

검증:

```text
Project U
→ Data
→ Validate Default Game Data Registry
```

신규 6개의 건축 Recipe가 Registry에 등록되고 중복 또는 잘못된 콘텐츠 ID가 없는지 확인하였다.

---

## 13. 건축 조작 테스트

기존 건축 시스템의 입력을 사용하여 신규 콘텐츠를 테스트하였다.

```text
B
→ 건축 모드 진입

Z / X
→ 이전 / 다음 건축 Recipe

Q / E
→ 건축물 회전

좌클릭
→ 설치

R
→ 설치 / 철거 모드 전환

B / ESC
→ 건축 모드 종료
```

신규 콘텐츠를 위해 새로운 입력 로직은 추가하지 않았다.

---

## 14. Stone 구조물 연결 테스트

Stone Foundation, Stone Floor, Stone Wall을 이용하여 구조물을 설치하였다.

테스트 흐름:

```text
Stone Foundation 설치
→ Stone Floor 연결
→ Stone Wall 연결
```

확인 결과:

- Foundation Terrain 배치 정상
- Floor 구조 연결 정상
- Wall 연결점 Snap 정상
- Wall 회전 방향 정상
- 연결 불가능 위치 설치 차단
- 필요 Stone 재료 정상 차감

---

## 15. Wood 가구 자유 배치 테스트

Stone 또는 Wood Floor 위에 Wood Table과 Wood Chair를 배치하였다.

확인 결과:

- 바닥 위 자유 배치 정상
- Q/E 회전 정상
- Terrain 직접 배치 제한 정상
- Table과 Chair의 충돌 검사 정상
- 다른 건축물과 겹치는 위치 설치 차단
- Wood 재료 정상 차감

---

## 16. Stone Campfire 기능 테스트

Stone Campfire를 Terrain 위에 설치한 뒤 기존 Campfire 기능이 유지되는지 확인하였다.

확인 항목:

- Terrain 자유 배치 정상
- Stone 8개 차감
- Wood 3개 차감
- 상호작용 안내 표시
- F 입력 상호작용 정상
- CampfireCookingStation 기능 정상
- 불빛 및 불꽃 표시 정상
- 기존 조리 기능 정상

---

## 17. 철거 및 재료 반환 테스트

건축 모드에서 R 입력으로 철거 모드를 활성화하여 신규 건축물을 철거하였다.

모든 신규 Recipe의 기본 철거 반환율은 다음과 같이 설정하였다.

```text
Demolition Refund Ratio: 0.5
```

예:

```text
Stone Foundation
설치 비용: Stone 6
철거 반환: Stone 3
```

확인 결과:

- 신규 건축물 철거 정상
- 철거 대상 표시 정상
- 재료 반환 정상
- 삭제된 건축물이 저장 대상에서 제거됨

---

## 18. 저장 및 Continue 복원 테스트

다음과 같이 여러 신규 건축물을 설치한 상태로 저장하였다.

```text
Stone Foundation
Stone Floor
Stone Wall
Wood Table
Wood Chair
Stone Campfire
```

저장 후 메인 메뉴에서 Continue로 다시 진입하였다.

확인 결과:

- 신규 구조물 위치 유지
- 신규 구조물 회전 유지
- Wood Table 위치·회전 유지
- Wood Chair 위치·회전 유지
- Stone Campfire 위치 유지
- 신규 건축물 중복 생성 없음
- 신규 건축물 누락 없음
- Stone Campfire 상호작용 정상
- Campfire 조리 기능 정상

---

## 19. 기존 건축 콘텐츠 회귀 테스트

신규 건축 콘텐츠 추가 이후 기존 건축 기능도 다시 점검하였다.

확인한 기존 콘텐츠:

```text
WOOD FOUNDATION
WOOD FLOOR
WOOD WALL
WORKBENCH
CAMPFIRE
SLEEPING BAG
SMALL CHEST
LARGE CHEST
STANDING LAMP
```

확인 결과:

- 기존 구조 연결 정상
- 기존 건축 Preview 정상
- Workbench 상호작용 정상
- Campfire 조리 정상
- Storage 기능 정상
- Sleeping Bag 기능 정상
- Standing Lamp 정상
- 기존 건축물 철거 정상
- 저장 및 복원 정상

---

## 20. 스크립트 변경 사항

73일차에는 새로운 `.cs` 파일을 생성하거나 기존 C# 스크립트를 수정하지 않았다.

기존에 구현된 다음 시스템을 그대로 사용하였다.

```text
BuildRecipeData
BuildPlacementController
BuildConnectionPoint
PlacedBuildObject
PlacedStructureSaveBridge
CampfireCookingStation
GameDataRegistry
```

이번 작업은 새로운 기반 시스템 개발보다 현재 시스템을 이용한 실제 콘텐츠 확장에 집중하였다.

---

## 21. 73일차 결과

73일차 작업 전:

```text
건축 콘텐츠 9종
```

73일차 작업 후:

```text
기존 건축 콘텐츠 9종
+
Stone 구조물 3종
+
Wood 가구 2종
+
Stone Campfire 1종
=
총 15종
```

새로운 시스템을 추가하지 않고 기존 데이터, Prefab, 건축 배치, 저장 시스템을 그대로 활용하여 게임에서 실제로 확인할 수 있는 건축 콘텐츠를 확장하였다.

이번 작업으로 앞으로도 다음 방식으로 건축 콘텐츠를 추가할 수 있음을 확인하였다.

```text
기존 Prefab 복제
→ 외형 변경
→ BuildRecipeData 생성
→ BuildPlacementController 등록
→ PlacedStructureSaveBridge 등록
→ GameDataRegistry 갱신
→ 설치·철거·저장 테스트
```

---

## 22. 다음 개발 방향

74일차에는 기존 적 전투 시스템을 활용하여 실제 전투 콘텐츠를 확장한다.

주요 방향:

1. 대표 근접 몬스터 데이터 및 Prefab 제작
2. 대표 원거리 몬스터 데이터 및 Prefab 제작
3. 몬스터별 체력·이동·공격 수치 분리
4. 근접형 추적 및 공격 테스트
5. 원거리형 거리 유지 및 공격 테스트
6. 기존 EnemyHealth와 NavMesh 구조 재사용
7. 몬스터별 임시 외형 구분
8. 저장 및 기존 전투 시스템 회귀 검사

예상 흐름:

```text
플레이어 탐색
→ 몬스터 발견
→ 적 추적
→ 공격 예고
→ 근접 또는 원거리 공격
→ 플레이어 반격
→ 적 처치
```

---

## 73일차 완료 상태

- [x] Stone Foundation 콘텐츠 추가
- [x] Stone Floor 콘텐츠 추가
- [x] Stone Wall 콘텐츠 추가
- [x] Wood Table 콘텐츠 추가
- [x] Wood Chair 콘텐츠 추가
- [x] Stone Campfire 콘텐츠 추가
- [x] 신규 Placed Prefab 제작
- [x] 신규 Preview Prefab 제작
- [x] BuildRecipeData 6개 생성
- [x] BuildPlacementController 등록
- [x] PlacedStructureSaveBridge 등록
- [x] GameDataRegistry 갱신
- [x] 구조물 Snap 테스트
- [x] 가구 자유 배치 테스트
- [x] Stone Campfire 기능 테스트
- [x] 철거 및 재료 반환 테스트
- [x] 저장 및 Continue 복원 테스트
- [x] 기존 건축 콘텐츠 회귀 테스트
- [x] Console 오류 확인
- [x] C# 스크립트 추가·수정 없이 콘텐츠 확장 완료

---

## Git Commit

```text
73일차 : 구조 부품·가구·시설 건축 콘텐츠 확장
```
