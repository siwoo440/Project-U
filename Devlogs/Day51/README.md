# 프로젝트 U 51일차 개발 일지

---

## 개발 정보

- 개발 일차: 51일차
- 작업일: 2026년 7월 31일
- Unity 버전: Unity 6000.3.9f1
- 대상 Scene: `Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity`
- 커밋 제목: `51일차 : 구조 건축 부품 및 연결 시스템 구현`
- 커밋 SHA: `9532ca93364fe183e779e912d75c9486bcd7b76c`

---

## 개발 목표

기존 Terrain 중심 건축 시스템을 구조 부품 기반 건축 시스템으로 확장한다.

나무 기초를 새로 추가하고, 기초·바닥·벽이 정해진 연결점을 기준으로 설치되도록 구성한다. 또한 하위 구조물을 지지하고 있는 기초나 바닥을 먼저 철거할 수 없도록 구조 의존 관계를 관리한다.

최종 구조는 다음과 같다.

```text
Terrain
└─ Foundation
   ├─ Floor
   │  └─ Wall
   └─ Wall
```

---

## 주요 구현 내용

### 1. 건축 구조 역할 분리

기존 `BuildPlacementType`은 배치 방식 판단에 계속 사용하고, 건축물의 구조상 역할은 새 `BuildStructureType`으로 분리하였다.

추가한 구조 역할은 다음과 같다.

- `None`
- `Foundation`
- `Floor`
- `Wall`
- `Furniture`
- `Roof`

기존 `BuildPlacementType`의 `Floor`, `Wall`, `Free` 값은 변경하지 않아 기존 건축 데이터와 저장 데이터의 호환성을 유지하였다.

`BuildRecipeData`에는 `structureType` 필드를 추가하여 각 건축 Recipe가 배치 방식과 구조 역할을 함께 가질 수 있도록 수정하였다.

---

### 2. 건축 연결점 시스템 구현

새로운 `BuildConnectionPoint` 컴포넌트를 추가하였다.

연결점은 다음 정보를 관리한다.

- 연결점 고유 ID
- 연결 가능한 구조 역할 목록
- 연결점 소유 건축물
- 현재 연결된 건축물
- 연결점 사용 여부
- 설치 위치와 회전값

연결점이 이미 사용 중이면 같은 위치에 다른 구조물을 중복 설치할 수 없도록 처리하였다.

---

### 3. 나무 기초 건축물 추가

Terrain에 직접 설치할 수 있는 `WOOD FOUNDATION` 건축물을 추가하였다.

추가 데이터:

- Recipe ID: `structure_wood_foundation`
- 구조 역할: `Foundation`
- 배치 방식: `Floor`
- 회전 단위: 90도
- 필요 재료: Wood 4개
- 철거 재료 반환 비율: 50%

추가 프리팹:

- `WoodFoundationPlaced`
- `WoodFoundationPreview`

나무 기초에는 다음 연결점을 구성하였다.

- 상단 바닥 연결점 1개
- 북쪽 벽 연결점 1개
- 남쪽 벽 연결점 1개
- 동쪽 벽 연결점 1개
- 서쪽 벽 연결점 1개

---

### 4. 기존 나무 바닥 확장

기존 `WOOD FLOOR`의 구조 역할을 `Floor`로 설정하였다.

바닥 전체 영역을 검사하도록 충돌 판정 범위를 수정하였다.

변경 전에는 Z축 충돌 검사 범위가 매우 작아 바닥 전체 중첩을 정확하게 검사하기 어려웠다. 이를 1×1 바닥 크기에 맞도록 수정하였다.

나무 바닥 프리팹에는 네 방향의 벽 연결점을 추가하였다.

- `wall_north`
- `wall_south`
- `wall_east`
- `wall_west`

이를 통해 벽이 Terrain 경계가 아닌 바닥 프리팹의 실제 연결점을 기준으로 설치되도록 확장하였다.

---

### 5. 기존 나무 벽 확장

기존 `WOOD WALL`의 구조 역할을 `Wall`로 설정하였다.

벽은 기초 또는 바닥의 벽 연결점을 바라볼 때만 설치할 수 있도록 변경하였다.

연결점의 위치와 회전값을 사용하여 벽이 각 구조물의 가장자리 중앙에 정렬되도록 처리하였다.

---

### 6. 구조 연결 배치 판정 구현

`BuildPlacementController`의 배치 계산을 두 방식으로 분리하였다.

#### Terrain 배치

다음 구조물은 기존 Terrain 기반 배치 방식을 사용한다.

- Foundation
- 구조 역할이 `None`인 자유 배치 건축물
- 기존 모닥불
- 기존 침낭

#### 연결점 배치

다음 구조물은 지지 건축물의 연결점을 사용한다.

- Floor
- Wall

화면 중앙 Raycast로 지지 구조물을 탐지한 후, 바라본 위치에서 가장 가까운 사용 가능한 연결점을 선택하도록 구현하였다.

연결점 탐지 과정에서 다음 조건을 검사한다.

- 최대 건축 거리
- 지지 건축물 존재 여부
- 연결 가능한 구조 역할
- 연결점 점유 여부
- 연결점 탐지 거리
- 설치 공간 충돌
- 필요 재료 보유 여부

설치할 수 없는 경우에는 다음 상태 문구를 표시한다.

- `LOOK AT SUPPORT`
- `INVALID SUPPORT`
- `NO FREE CONNECTION`
- `TOO FAR`
- `SPACE BLOCKED`
- `NEED MATERIALS`
- `CONNECTION FAILED`

---

### 7. 구조물 지지 관계 관리

`PlacedBuildObject`에 구조 관계 데이터를 추가하였다.

관리하는 정보:

- 현재 건축물의 구조 역할
- 현재 건축물을 지지하는 하위 구조물
- 사용 중인 지지 연결점
- 현재 건축물이 지지하는 상위 구조물 목록
- 하위 연결점 목록

구조물 설치 시 다음 관계를 등록한다.

```text
지지 구조물
└─ 연결점
   └─ 새로 설치한 구조물
```

구조물이 철거되면 연결점 점유 상태와 지지 관계도 함께 해제하도록 구성하였다.

---

### 8. 구조물 철거 제한 구현

`PlacedBuildObject`가 `IBuildRemovalGuard`를 구현하도록 수정하였다.

현재 다른 구조물을 지지하고 있는 기초나 바닥은 철거할 수 없다.

철거 순서는 다음과 같이 제한된다.

```text
벽 철거
→ 바닥 철거
→ 기초 철거
```

하위 구조물이 남아 있는 상태에서 지지 구조물을 철거하려고 하면 다음 문구를 표시한다.

```text
REMOVE SUPPORTED STRUCTURES FIRST
```

철거 제한 검사도 단일 컴포넌트만 확인하는 방식에서 하위 오브젝트에 연결된 전체 `IBuildRemovalGuard`를 확인하는 방식으로 확장하였다.

이를 통해 구조 의존 관계와 보관함 같은 기존 기능별 철거 제한을 함께 검사할 수 있도록 구성하였다.

---

### 9. 설치 실패 시 재료 복구 처리

구조물 생성 후 연결점 등록에 실패하면 생성된 구조물을 제거하고 소비한 재료를 인벤토리로 복구하도록 처리하였다.

이 과정으로 연결점 점유 상태가 설치 직전에 변경되거나 잘못된 연결 관계가 발생했을 때 재료만 손실되는 문제를 방지하였다.

---

### 10. Gameplay Scene 연결

`20_Gameplay` Scene의 `BuildPlacementController`에 나무 기초 Recipe를 추가하였다.

건축 Recipe 순서는 다음과 같이 구성하였다.

1. Wood Foundation
2. Wood Floor
3. Wood Wall
4. Campfire
5. Sleeping Bag

연결점 탐지 거리 설정을 추가하고, 나무 기초 데이터를 기존 건축물 저장 등록 목록에도 연결하였다.

---

## 추가 및 수정된 주요 파일

### 새로 추가한 파일

```text
Assets/_ProjectU/Data/Building/BuildRecipe_WoodFoundation.asset

Assets/_ProjectU/Prefabs/Building/WoodFoundationPlaced.prefab
Assets/_ProjectU/Prefabs/Building/WoodFoundationPreview.prefab

Assets/_ProjectU/Scripts/Building/BuildConnectionPoint.cs
Assets/_ProjectU/Scripts/Building/BuildStructureType.cs
```

### 수정한 파일

```text
Assets/_ProjectU/Data/Building/BuildRecipe_WoodFloor.asset
Assets/_ProjectU/Data/Building/BuildRecipe_WoodWall.asset

Assets/_ProjectU/Prefabs/Building/WoodFloorPlaced.prefab

Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity

Assets/_ProjectU/Scripts/Building/BuildPlacementController.cs
Assets/_ProjectU/Scripts/Building/BuildRecipeData.cs
Assets/_ProjectU/Scripts/Building/PlacedBuildObject.cs
```

이번 커밋에서는 `.meta` 파일을 포함하여 총 17개 파일이 변경되었다.

- 새 파일: 10개
- 수정 파일: 7개

---

## 동작 확인

다음 항목을 기준으로 51일차 구현을 확인하였다.

- 나무 기초를 Terrain의 격자 중앙에 설치할 수 있음
- 기초 상단 연결점에 나무 바닥을 설치할 수 있음
- 기초 가장자리에 나무 벽을 설치할 수 있음
- 바닥 가장자리에 나무 벽을 설치할 수 있음
- 연결점이 없는 위치에는 바닥과 벽을 설치할 수 없음
- 이미 사용한 연결점에 구조물을 중복 설치할 수 없음
- 연결된 구조물이 남아 있으면 지지 구조물을 철거할 수 없음
- 벽부터 역순으로 철거하면 바닥과 기초를 정상적으로 철거할 수 있음
- 철거 시 기존 재료 반환 규칙이 유지됨
- 기존 모닥불과 침낭의 Terrain 자유 배치가 유지됨
- 기존 건축 Recipe ID와 `BuildPlacementType` 값이 유지됨

---

## 현재 제한 사항

51일차에서는 실행 중인 구조물 사이의 연결 관계를 관리한다.

다음 정보의 저장과 불러오기는 아직 구현하지 않았다.

- 지지 구조물 ID
- 사용 중인 연결점 ID
- 연결점 점유 상태
- 불러오기 후 기초·바닥·벽 관계 재구성

구조 건축물의 연결 관계 저장과 복원은 54일차 작업에서 구현한다.

---

## 개발 결과

기존의 단순 Terrain 배치 시스템을 기초·바닥·벽이 서로 연결되는 구조 건축 시스템으로 확장하였다.

나무 기초가 구조 건축의 시작점 역할을 하며, 바닥과 벽은 지정된 연결점을 기준으로만 설치된다. 또한 구조물 사이의 지지 관계를 등록하여 하위 구조물이 남아 있는 상태에서 기반 구조물이 먼저 철거되는 문제를 방지하였다.

이번 작업으로 이후 기능성 가구, 지붕, 상위 벽, 구조물 교체 및 저장 시스템을 확장할 수 있는 기본 구조를 마련하였다.

---

## 다음 개발 방향

52일차에는 기능성 가구 연결을 구현한다.

주요 작업 예정:

- 침낭과 보관함을 `Furniture` 구조 역할로 확장
- 기초 또는 바닥 위 가구 연결점 배치
- 가구별 설치 가능 표면 검사
- 기존 상호작용 기능과 구조 건축 시스템 연결
- 가구 철거 제한과 보관 상태 검사 통합
