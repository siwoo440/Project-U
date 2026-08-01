# 프로젝트 U 개발 일지

## 59일차 : 1·3인칭 전환 및 자유 건축 카메라 구현

### 개발 목표

기존 플레이어 중심 건축 방식을 개선하여 건축 모드에서 플레이어를 제자리에 고정하고, 자유롭게 이동할 수 있는 전용 건축 카메라를 제공한다.

일반 플레이에서는 1인칭과 3인칭 시점을 자연스럽게 전환할 수 있도록 구성하고, 건축 모드 종료 시 진입 전에 사용하던 시점으로 복귀하도록 한다.

---

## 구현 내용

### 1. 자유 건축 카메라 구현

`BuildModeCameraController`를 새로 추가하였다.

건축 모드에 진입하면 현재 플레이어 카메라 제어를 일시적으로 중단하고, 같은 카메라를 자유 건축 카메라로 사용하도록 구성하였다.

건축 모드 카메라 조작은 다음과 같다.

- 우클릭 홀드 + 마우스 이동: 카메라 회전
- 마우스 휠: 카메라 전진 및 후진
- 휠 버튼 홀드 + 마우스 이동: 카메라 평행 이동
- B 또는 ESC: 건축 모드 종료

건축 모드 중에는 플레이어의 위치와 회전을 진입 시점 값으로 유지한다.

카메라는 플레이어를 기준으로 지정된 최대 거리와 높이 범위 안에서만 이동할 수 있도록 제한하였다.

카메라 이동 경로에는 `SphereCast` 충돌 검사를 적용하여 지형과 구조물을 통과하지 않도록 구성하였다.

---

### 2. 건축 중 플레이어 입력 정지

건축 모드에 진입하면 다음 Gameplay 컴포넌트를 일시적으로 비활성화하도록 구성하였다.

- `PlayerMovement`
- `PlayerInteractor`
- `HotbarItemUse`
- `HotbarInput`

건축 모드를 종료하면 각 컴포넌트가 건축 진입 전에 가지고 있던 활성 상태로 복구된다.

건축 시스템 자체에 필요한 다음 컴포넌트는 비활성화 대상에서 제외하였다.

- `BuildPlacementController`
- `BuildModeCameraController`
- `ThirdPersonCameraFollow`

---

### 3. 마우스 포인터 기반 Preview 배치

기존 화면 중앙 Ray 방식에서 실제 마우스 포인터 위치를 사용하는 방식으로 변경하였다.

건축 모드에서 마우스 포인터 아래에 있는 지면이나 구조물을 검사하여 Preview를 표시한다.

적용된 기능은 다음과 같다.

- 포인터 아래 지면에 Preview 표시
- 건축 구역 내부 여부 검사
- 플레이어 기준 최대 건축 거리 검사
- Terrain 경사와 높이 차이 검사
- 장애물과 기존 건축물 충돌 검사
- 건축 연결점 탐색 및 Snap
- 재료 보유 여부 검사
- 설치 가능 상태에 따른 Preview 재질 변경

카메라를 우클릭 또는 휠 버튼으로 조작하는 동안에는 Preview 갱신과 좌클릭 설치를 일시적으로 차단하였다.

---

### 4. 기존 건축 기능 유지

자유 건축 카메라를 적용하면서 기존 건축 기능을 유지하였다.

- Q / E: Preview 회전
- Z / X: 이전 및 다음 건축물 선택
- R: 설치 모드와 철거 모드 전환
- 좌클릭: 설치 또는 철거
- 건축 재료 소비
- 철거 시 설정된 비율만큼 재료 환급
- 바닥, 벽, 자유 배치, 가구 배치 지원
- `BuildConnectionPoint` 기반 구조 연결
- 보관함 등 철거 제한 기능 유지

---

### 5. 1인칭·3인칭 통합 카메라 구현

기존 `ThirdPersonCameraFollow`를 1인칭과 3인칭을 함께 관리하는 통합 카메라로 확장하였다.

클래스 이름은 기존 참조와 설정 메뉴 연결을 유지하기 위해 변경하지 않았다.

기본 시점 전환 키는 다음과 같다.

- V: 1인칭과 3인칭 시점 전환

시점 전환 시 다음 값이 자연스럽게 보간된다.

- 카메라 위치
- Field Of View
- Near Clipping Plane

플레이어 아래에 `FirstPersonCameraAnchor`를 생성하고, 해당 위치를 1인칭 카메라 기준점으로 사용하도록 구성하였다.

---

### 6. 1인칭 플레이어 외형 처리

1인칭 전환 시 플레이어 몸 내부가 카메라에 보이지 않도록 `PlayerVisual` 아래의 Renderer를 숨기도록 구성하였다.

3인칭으로 복귀하거나 자유 건축 카메라를 사용하는 동안에는 기존 Renderer 활성 상태를 복구한다.

플레이어 루트 오브젝트 자체를 비활성화하지 않고 Renderer만 제어하므로 다음 기능은 유지된다.

- 플레이어 이동
- 충돌 처리
- 체력
- 인벤토리
- 상호작용
- 장비와 기타 Gameplay 컴포넌트

---

### 7. 건축 모드와 1·3인칭 상태 연동

건축 모드 진입 전 사용 중이던 시점을 유지하도록 구성하였다.

#### 3인칭에서 건축 진입

1. 3인칭 상태에서 B 입력
2. 자유 건축 카메라 활성화
3. 건축 종료
4. 기존 3인칭 시점으로 복귀

#### 1인칭에서 건축 진입

1. 1인칭 상태에서 B 입력
2. 플레이어 외형 Renderer 복구
3. 자유 건축 카메라 활성화
4. 건축 종료
5. 기존 1인칭 시점으로 복귀
6. 플레이어 외형 Renderer 다시 숨김

---

### 8. ESC 입력 충돌 방지

`PauseMenuController`를 수정하여 건축 모드에서 ESC를 누른 같은 프레임에 Pause Menu가 함께 열리지 않도록 처리하였다.

입력 흐름은 다음과 같다.

1. 건축 모드에서 ESC 입력
2. 건축 모드만 종료
3. 다음 ESC 입력
4. Pause Menu 표시

전체 지도나 다른 팝업이 열려 있던 프레임에도 기존 UI가 우선 닫히도록 처리하였다.

---

### 9. Scene 및 Inspector 설정 보완

`20_Gameplay` Scene에서 다음 참조와 설정을 보완하였다.

- `PauseMenuController`에 `BuildPlacementController` 연결
- `BuildPlacementController`에 `BuildModeCameraController` 연결
- `BuildPlacementController`에 `WorldMapController` 연결
- `ThirdPersonCameraFollow`에 `FirstPersonCameraAnchor` 연결
- `ThirdPersonCameraFollow`에 `PlayerVisual` 연결
- `BuildModeCameraController`의 Gameplay 비활성화 목록 연결
- 건축 카메라 충돌 레이어에서 Player와 UI 계열 제외
- 건축 가능 거리와 자유 카메라 이동 거리 통일
- 시작 시점을 3인칭으로 설정
- 1인칭 상하 시야 범위 확장
- 건축 HUD 크기와 글자 크기 보정
- 정보 표시용 UI의 Raycast Target 해제

---

## 주요 설정값

### ThirdPersonCameraFollow

| 항목 | 값 |
| --- | --- |
| Start View Mode | Third Person |
| View Toggle Key | V |
| View Transition Smooth Time | 0.22 |
| First Person Field Of View | 70 |
| Third Person Field Of View | 60 |
| First Person Near Clip Plane | 0.03 |
| Third Person Near Clip Plane | 0.1 |
| Initial Pitch | 20 |
| Minimum Pitch | -80 |
| Maximum Pitch | 85 |
| First Person Anchor Height | 1.65 |

### BuildModeCameraController

| 항목 | 값 |
| --- | --- |
| Look Sensitivity | 0.15 |
| Pan Sensitivity | 0.015 |
| Wheel Move Step | 2 |
| Maximum Distance From Player | 25 |
| Minimum Height From Player | 1 |
| Maximum Height From Player | 30 |
| Collision Radius | 0.3 |
| Collision Padding | 0.2 |
| Confine Cursor | 활성화 |

### BuildPlacementController

| 항목 | 값 |
| --- | --- |
| Maximum Build Distance | 25 |
| Maximum Placement Ray Distance | 200 |
| Terrain Probe Height | 5 |
| Terrain Probe Distance | 12 |
| Collision Padding | 0.02 |
| Connection Snap Distance | 0.8 |

---

## 생성 및 수정 파일

### 생성

```text
Assets/_ProjectU/Scripts/Building/BuildModeCameraController.cs
```

### 수정

```text
Assets/_ProjectU/Scripts/Building/BuildPlacementController.cs
Assets/_ProjectU/Scripts/Camera/ThirdPersonCameraFollow.cs
Assets/_ProjectU/Scripts/UI/Pause/PauseMenuController.cs
Assets/_ProjectU/Scenes/20_Gameplay.unity
```

---

## 최종 조작법

| 입력 | 기능 |
| --- | --- |
| V | 1인칭·3인칭 전환 |
| B | 건축 모드 진입·종료 |
| ESC | 건축 모드 종료 또는 Pause Menu |
| 우클릭 드래그 | 건축 카메라 회전 |
| 마우스 휠 | 건축 카메라 전진·후진 |
| 휠 버튼 드래그 | 건축 카메라 평행 이동 |
| 좌클릭 | 건축물 설치 또는 철거 |
| Q / E | Preview 회전 |
| Z / X | 건축물 선택 |
| R | 설치·철거 모드 전환 |

---

## 확인 항목

- 게임 시작 시 3인칭 시점 표시
- V 입력으로 1인칭과 3인칭 자연스러운 전환
- 1인칭에서 플레이어 몸 내부가 보이지 않음
- B 입력으로 자유 건축 카메라 활성화
- 건축 중 플레이어 이동과 상호작용 차단
- 우클릭, 휠, 휠 버튼으로 건축 카메라 조작
- 마우스 포인터 아래 Preview 표시
- 설치 가능 여부에 따른 Preview 재질 변경
- 기존 건축 연결점과 재료 소비 기능 유지
- 철거 대상 포인터 선택과 재료 환급 유지
- 건축 종료 후 진입 전 1·3인칭 상태 복구
- ESC 한 번으로 건축 모드만 종료
- 다음 ESC에서 Pause Menu 표시
- 전체 지도와 인벤토리 사용 시 건축 모드 정상 정리
- Console 컴파일 오류와 Missing Reference 없음

---

## 59일차 결과

플레이어가 일반 플레이에서는 1인칭과 3인칭을 자유롭게 전환할 수 있게 되었다.

건축 모드에서는 플레이어를 고정하고 자유 건축 카메라를 사용하여 넓은 범위를 편하게 확인할 수 있게 되었다.

화면 중앙이 아닌 실제 마우스 포인터 위치에 Preview가 표시되며, 기존 설치·연결·재료·철거 시스템과 함께 작동하도록 통합하였다.
