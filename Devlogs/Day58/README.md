# Project U 개발 일지

## 58일차 : 미니맵 클릭 전체 지도 및 휠 줌 구현

### 개발 목표

58일차에는 Gameplay 화면에서 항상 확인할 수 있는 미니맵과 전체 화면 지도 기능을 구현하였다.

미니맵은 플레이어 주변 지형을 위쪽 시점으로 실시간 표시하며, `N` 키를 눌러 작은 미니맵과 확장 미니맵 상태를 전환할 수 있도록 구성하였다.

전체 지도는 `M` 키로 열고 닫을 수 있으며, 기존 `Alt` 키의 마우스 커서 활성화 기능을 활용하여 커서가 표시된 상태에서 미니맵을 클릭해도 동일한 전체 지도 패널이 열리도록 구현하였다.

전체 지도에서는 마우스 휠을 통해 줌인과 줌아웃을 수행할 수 있도록 지도 카메라의 Orthographic Size를 실시간으로 조절하였다.

또한 인벤토리, 보관함, 일시정지 메뉴와 전체 지도가 동시에 열리지 않도록 입력 충돌 방지 구조를 추가하였다.

---

### 최종 입력 구조

```text
N
→ 작은 미니맵과 확장 미니맵 전환

Alt
→ 기존 기능대로 마우스 커서 활성화

Alt로 커서가 활성화된 상태에서 미니맵 클릭
→ M으로 열리는 것과 같은 전체 지도 열기

M
→ 전체 지도 열기 또는 닫기

ESC
→ 전체 지도가 열려 있으면 지도만 먼저 닫기

마우스 휠 위
→ 전체 지도 줌인

마우스 휠 아래
→ 전체 지도 줌아웃
```

기존에 계획했던 `Alt + N` 전체 지도 입력은 제거하였다.

`Alt`는 커서 활성화 기능으로만 사용하고, 전체 지도는 미니맵을 직접 클릭하여 열도록 변경하였다.

---

## 구현 내용

### 1. 지도 전용 Camera 구현

Gameplay Scene에 다음 지도 시스템 오브젝트를 추가하였다.

```text
=== MapSystem ===
└─ MapCamera
```

`MapCamera`에는 다음 컴포넌트를 구성하였다.

```text
Camera
MinimapCameraController
```

지도 카메라는 플레이어 위쪽에 위치하고 아래 방향을 바라보며, Perspective가 아닌 Orthographic 방식으로 월드를 렌더링한다.

```text
Camera Height
→ 플레이어 위쪽 지도 카메라 높이

Camera Euler Angles
→ 기본값 X 90, Y 0, Z 0

Orthographic
→ 활성화
```

지도 카메라에 추가 AudioListener가 존재하지 않도록 제거하였다.

---

### 2. 런타임 RenderTexture 생성

별도의 RenderTexture 에셋을 수동 생성하지 않고 `MinimapCameraController`가 실행 중 다음 RenderTexture를 생성하도록 구현하였다.

```text
RT_Runtime_Minimap
```

기본 설정은 다음과 같다.

```text
Texture Size: 1024
Depth Buffer Bits: 16
Filter Mode: Bilinear
Wrap Mode: Clamp
Mip Map: 사용 안 함
```

같은 RenderTexture를 다음 두 UI가 함께 사용한다.

```text
Gameplay HUD 미니맵
전체 화면 지도 패널
```

이를 통해 지도 카메라를 중복 생성하지 않고 하나의 지도 영상을 재사용한다.

---

### 3. 플레이어 중심 지도 카메라 추적

지도 카메라는 매 `LateUpdate()`에서 플레이어의 현재 위치를 추적한다.

```text
MapCamera X
→ Player X

MapCamera Y
→ Player Y + Camera Height

MapCamera Z
→ Player Z
```

플레이어가 저장 데이터를 불러와 다른 위치로 이동해도 지도 카메라가 다음 프레임에 새로운 위치를 자동으로 따라가도록 구성하였다.

지도 카메라 자체의 위치와 줌 정보는 저장 데이터에 포함하지 않는다.

---

### 4. 지도 Camera 표시 범위 구분

지도 카메라 표시 범위를 다음 세 상태로 구분하였다.

```text
Compact
→ 기본 작은 미니맵

Expanded
→ N 키로 확장한 미니맵

FullMap
→ M 또는 미니맵 클릭으로 연 전체 지도
```

기본 권장 Orthographic Size는 다음과 같다.

```text
Compact Orthographic Size: 35
Expanded Orthographic Size: 60
Full Map Default Orthographic Size: 140
```

Orthographic Size가 작을수록 가까이 확대되고, 클수록 더 넓은 범위가 표시된다.

---

## 미니맵 HUD

### 5. 미니맵 HUD 구성

기존 Gameplay Canvas에 다음 구조를 추가하였다.

```text
Canvas
└─ MinimapHUD
   └─ MinimapPanel
      ├─ MapRawImage
      └─ PlayerDirectionIcon
```

`MinimapHUD`에는 다음 스크립트를 연결하였다.

```text
MinimapHUDView
```

`MinimapPanel`은 화면 오른쪽 위에 배치하였다.

```text
Anchor
→ Top Right

Pivot
→ 1, 1

Compact Size
→ 260 × 260

Compact Anchored Position
→ -30, -30
```

---

### 6. N 키 미니맵 크기 전환

`N` 키를 누르면 다음 두 상태가 전환된다.

```text
Compact
260 × 260
Orthographic Size 35

Expanded
520 × 520
Orthographic Size 60
```

UI의 크기만 확대하는 것이 아니라 지도 카메라의 Orthographic Size도 함께 변경하여 확장 미니맵에서 더 넓은 주변을 확인할 수 있도록 구성하였다.

다시 `N` 키를 누르면 기존 작은 미니맵 상태로 돌아온다.

---

### 7. Alt+N 지도 입력 제거

기존 계획에서는 `Alt + N`으로 전체 지도를 열도록 구성했지만, 현재 프로젝트에서 `Alt`는 마우스 커서 활성화에 사용되고 있다.

입력 역할이 겹치는 문제를 방지하기 위해 `Alt + N` 전체 지도 기능을 제거하였다.

최종 동작은 다음과 같다.

```text
N
→ 미니맵 크기 전환

Alt + N
→ 지도 관련 동작 없음

Alt
→ 마우스 커서 활성화
```

Alt를 누른 상태에서 N을 눌러도 전체 지도가 열리거나 미니맵 크기가 변경되지 않도록 처리하였다.

---

### 8. 플레이어 방향 아이콘

미니맵 중앙에 플레이어가 바라보는 방향을 표시하는 화살표를 추가하였다.

```text
PlayerDirectionIcon
→ 미니맵 중앙 고정

플레이어 회전
→ 방향 아이콘 Z 회전 갱신
```

북쪽이 고정된 지도에서 플레이어의 Y 회전값을 UI Z 회전값으로 변환하여 표시한다.

화살표 Sprite 또는 Text 방향이 반대로 보일 경우 다음 값을 통해 보정할 수 있다.

```text
Direction Rotation Offset
→ 0 또는 180
```

---

## 미니맵 클릭 전체 지도

### 9. MinimapPanel Button 추가

미니맵 자체를 클릭할 수 있도록 `MinimapPanel`에 `Button` 컴포넌트를 추가하였다.

```text
MinimapPanel
├─ Image
└─ Button
```

설정은 다음과 같다.

```text
Interactable
→ 활성화

Target Graphic
→ MinimapPanel Image

On Click
→ 비워 둠
```

`MinimapHUDView`가 런타임에 Button 이벤트를 자동 등록한다.

키보드나 게임패드 UI 선택으로 우발적으로 지도가 열리지 않도록 Button Navigation은 `None`으로 변경한다.

---

### 10. 미니맵 클릭 Raycast 구성

클릭 입력은 부모 `MinimapPanel`이 담당한다.

```text
MinimapPanel Image
→ Raycast Target 활성화

MapRawImage
→ Raycast Target 비활성화

PlayerDirectionIcon
→ Raycast Target 비활성화
```

자식 지도 이미지와 방향 아이콘이 클릭을 가로채지 않고 부모 Button으로 전달되도록 구성하였다.

---

### 11. Alt 커서 활성 상태 검사

미니맵 클릭으로 전체 지도를 열 때 다음 조건을 검사한다.

```text
Cursor.visible == true

그리고

Cursor.lockState != CursorLockMode.Locked
```

따라서 일반 Gameplay 상태에서는 미니맵 클릭이 실행되지 않는다.

```text
일반 Gameplay
→ 커서 잠김
→ 미니맵 클릭 차단

Alt 입력
→ 커서 표시와 잠금 해제
→ 미니맵 클릭 가능
```

커서가 활성화되지 않은 상태에서 Button 선택 이벤트가 발생하더라도 전체 지도 열기 요청을 무시한다.

---

### 12. M과 동일한 전체 지도 공유

미니맵 클릭은 별도의 지도 화면을 만들지 않는다.

다음 두 입력은 같은 `WorldMapController.OpenFullMap()` 기능을 사용한다.

```text
M 키
Alt 커서 상태에서 미니맵 클릭
```

두 방식 모두 다음 프리팹 하나를 생성하고 재사용한다.

```text
PF_UI_WorldMapPanel
```

따라서 다음과 같은 중복 지도는 생성되지 않는다.

```text
비정상

PopupLayer
├─ PF_UI_WorldMapPanel
└─ PF_UI_WorldMapPanel
```

정상 구조:

```text
PopupLayer
└─ PF_UI_WorldMapPanel
```

---

## 전체 지도 패널

### 13. 전체 지도 프리팹 구성

다음 프리팹을 생성하였다.

```text
Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_WorldMapPanel.prefab
```

최종 구조는 다음과 같다.

```text
PF_UI_WorldMapPanel
├─ Canvas
├─ GraphicRaycaster
├─ WorldMapPanelView
└─ WorldMapPanel
   ├─ WorldMapRawImage
   ├─ WorldMapPlayerDirectionIcon
   ├─ WorldMapTitleText
   ├─ CoordinateText
   ├─ MapHintText
   └─ CloseButton
```

전체 지도는 최초 M 입력 또는 미니맵 클릭 시 `PopupLayer` 아래에 생성된다.

이후에는 삭제하지 않고 비활성화하여 재사용한다.

---

### 14. 전체 지도 Canvas 정렬

전체 지도가 기존 Gameplay HUD보다 위에 표시되도록 프리팹 루트에 별도 Canvas를 적용하였다.

```text
Override Sorting
→ 활성화

Sorting Order
→ 4500
```

현재 일시정지 메뉴는 `5000`을 사용하므로 다음 순서를 유지한다.

```text
Gameplay HUD
→ 일반 Canvas

전체 지도
→ Sorting Order 4500

일시정지 메뉴
→ Sorting Order 5000
```

실제 동작에서는 전체 지도와 일시정지 메뉴가 동시에 열리지 않지만 정렬 순서도 분리하였다.

---

### 15. 전체 지도 입력 잠금

전체 지도가 열리면 `GameplayInputLock`에 다음 잠금 ID를 등록한다.

```text
WorldMap
```

전체 지도 중에는 다음 기능을 정지한다.

```text
플레이어 이동
카메라 회전
상호작용
Hotbar 입력
건축 입력
기타 Gameplay 입력
```

전체 지도를 닫으면 `WorldMap` 잠금을 해제한다.

`WorldMapController` 자체는 `GameplayInputLock`의 `Behaviours To Disable` 목록에 넣지 않는다.

지도 관리자가 비활성화되면 M, ESC와 휠 입력을 처리할 수 없기 때문이다.

---

### 16. 전체 지도 게임 시간 정지

`Pause Game Time` 설정이 활성화되어 있으면 전체 지도를 열 때 다음 값을 적용한다.

```csharp
Time.timeScale = 0f;
```

전체 지도를 닫으면 열기 이전 시간 배율을 복구한다.

UI 입력과 마우스 휠은 Input System의 현재 프레임 값을 직접 사용하므로 `Time.timeScale`이 0이어도 정상적으로 작동한다.

---

### 17. 미니맵 임시 숨김과 복구

전체 지도가 열리면 오른쪽 위 미니맵을 임시로 숨긴다.

```text
전체 지도 열기
→ MinimapHUD 숨김
→ Camera ViewMode FullMap 적용

전체 지도 닫기
→ 기존 N 크기 상태 복구
→ Compact 또는 Expanded Camera 범위 복구
→ MinimapHUD 다시 표시
```

전체 지도를 열기 전 미니맵이 확장 상태였다면 지도를 닫은 뒤 확장 상태로 돌아온다.

---

### 18. 전체 지도 좌표와 방향 표시

전체 지도에는 플레이어의 현재 방향과 위치 좌표를 표시한다.

```text
플레이어 방향
→ 화면 중앙 방향 화살표

플레이어 좌표
→ X 좌표와 Z 좌표 표시
```

좌표 예시:

```text
X 125.4   Z -32.8
```

좌표 Text는 선택 참조로 구성하여 연결하지 않아도 전체 지도 기능 자체는 작동한다.

---

## 전체 지도 휠 줌

### 19. Orthographic Size 기반 줌

전체 지도에서 마우스 휠을 사용하면 지도 카메라의 Orthographic Size를 변경한다.

```text
휠 위
→ Orthographic Size 감소
→ 줌인

휠 아래
→ Orthographic Size 증가
→ 줌아웃
```

Perspective 카메라의 위치를 이동하는 방식이 아니라 정사영 카메라의 표시 범위를 조절하는 방식이다.

---

### 20. 전체 지도 줌 설정

`MinimapCameraController`에 다음 설정을 추가하였다.

```text
Full Map Default Orthographic Size
Full Map Minimum Orthographic Size
Full Map Maximum Orthographic Size
Full Map Zoom Step
```

기본 권장값:

```text
Default: 140
Minimum: 45
Maximum: 220
Zoom Step: 12
```

각 항목의 의미:

```text
Default
→ 전체 지도를 처음 열 때 기본 범위

Minimum
→ 가장 가까이 줌인할 수 있는 제한

Maximum
→ 가장 멀리 줌아웃할 수 있는 제한

Zoom Step
→ 휠 입력 한 번에 변하는 값
```

---

### 21. 줌 범위 제한

전체 지도 줌값은 다음 범위를 넘지 않도록 제한한다.

```csharp
Mathf.Clamp(
    currentFullMapOrthographicSize,
    fullMapMinimumOrthographicSize,
    fullMapMaximumOrthographicSize);
```

지나치게 확대되어 지도가 보이지 않거나, 지나치게 축소되어 렌더링 범위를 벗어나는 문제를 방지한다.

---

### 22. 전체 지도 줌 상태 유지

전체 지도를 닫았다가 다시 열어도 마지막으로 사용한 줌값을 유지한다.

예시:

```text
기본 전체 지도
→ Orthographic Size 140

휠 줌인
→ 80

전체 지도 닫기

다시 전체 지도 열기
→ 80 상태 유지
```

기본값으로 되돌릴 수 있도록 다음 기능도 준비하였다.

```csharp
WorldMapController.ResetFullMapZoom();
```

현재 UI에는 별도의 RESET 버튼을 추가하지 않았다.

---

### 23. 전체 지도 입력 안내 수정

기존 `Alt + N` 안내를 제거하고 다음 내용으로 변경하였다.

```text
M / ESC : CLOSE
MOUSE WHEEL : ZOOM
```

한글 UI를 적용할 경우 다음처럼 표시할 수 있다.

```text
M / ESC : 닫기
마우스 휠 : 확대·축소
```

미니맵 근처에도 다음 안내를 선택적으로 표시할 수 있다.

```text
ALT로 커서 활성화 후 클릭 : 전체 지도
```

---

## UI 입력 충돌 방지

### 24. 인벤토리와 전체 지도 충돌 방지

인벤토리 또는 보관함이 열린 상태에서 전체 지도를 열면 기존 팝업을 먼저 닫는다.

```text
I
→ 인벤토리 열기

M 또는 미니맵 클릭
→ 인벤토리 닫기
→ 전체 지도 열기
```

보관함도 동일하게 처리한다.

전체 지도 사용 중 인벤토리나 보관함이 다시 열리면 `LateUpdate()`에서 닫아 동시에 표시되지 않도록 구성하였다.

---

### 25. 일시정지 메뉴와 전체 지도 충돌 방지

일시정지 메뉴가 열린 상태에서는 다음 지도 입력을 차단한다.

```text
N
M
미니맵 클릭
```

전체 지도가 열린 상태에서 ESC를 누르면 지도만 먼저 닫는다.

```text
M
→ 전체 지도 열기

ESC
→ 전체 지도 닫기

ESC
→ 일시정지 메뉴 열기
```

`PauseMenuController`는 ESC가 입력된 프레임 시작 시 전체 지도가 열려 있었는지 확인하고, 같은 ESC 입력으로 일시정지 메뉴가 열리는 것을 방지한다.

---

### 26. Alt 입력 잠금과 WorldMap 잠금 분리

Alt로 커서를 활성화한 상태에서 미니맵을 클릭하면 전체 지도 잠금이 추가된다.

```text
Alt 입력 잠금
+
WorldMap 입력 잠금
```

전체 지도가 열린 뒤 Alt 키를 놓더라도 `WorldMap` 잠금이 유지되므로 커서와 Gameplay 입력 상태가 즉시 복구되지 않는다.

M, ESC 또는 CLOSE 버튼으로 전체 지도를 닫으면 `WorldMap` 잠금이 해제된다.

각 잠금 ID를 별도로 관리하여 입력 상태가 꼬이지 않도록 구성하였다.

---

## 주요 생성 및 수정 파일

```text
Assets/_ProjectU/Scenes/20_Gameplay.unity

Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_WorldMapPanel.prefab

Assets/_ProjectU/Scripts/UI/Map/MinimapCameraController.cs
Assets/_ProjectU/Scripts/UI/Map/MinimapHUDView.cs
Assets/_ProjectU/Scripts/UI/Map/WorldMapPanelView.cs
Assets/_ProjectU/Scripts/UI/Map/WorldMapController.cs

Assets/_ProjectU/Scripts/UI/Pause/PauseMenuController.cs
```

---

## 최종 Scene 구조

```text
20_Gameplay

├─ === MapSystem ===
│  └─ MapCamera
│     ├─ Camera
│     └─ MinimapCameraController
│
├─ === UIManagers ===
│  ├─ GameUIManager
│  ├─ GameplayInputLock
│  ├─ GameplayUIRuntimeValidator
│  ├─ PauseMenuController
│  └─ WorldMapController
│
└─ Canvas
   ├─ MinimapHUD
   │  └─ MinimapPanel
   │     ├─ Image
   │     ├─ Button
   │     ├─ MapRawImage
   │     └─ PlayerDirectionIcon
   │
   └─ PopupLayer
      ├─ 런타임 PF_UI_InventoryPopup
      ├─ 런타임 PF_UI_StoragePopup
      ├─ 런타임 PF_UI_PauseMenu
      └─ 런타임 PF_UI_WorldMapPanel
```

---

## 테스트 항목

### 지도 Camera

- MapCamera가 Player 위쪽을 따라가는지 확인
- MapCamera에 AudioListener가 없는지 확인
- Camera가 Orthographic 상태인지 확인
- Target Texture에 `RT_Runtime_Minimap`이 연결되는지 확인
- Map Layer Mask에 지형이 포함되는지 확인
- 미니맵 영상이 검은색으로만 표시되지 않는지 확인

### 미니맵 기본 동작

- 게임 시작 시 작은 미니맵이 표시되는지 확인
- 플레이어 이동 시 지도 중심이 따라오는지 확인
- 플레이어 회전 시 방향 화살표가 회전하는지 확인
- N으로 확장 미니맵이 표시되는지 확인
- 다시 N을 눌러 작은 미니맵으로 돌아오는지 확인
- Alt+N으로 전체 지도가 열리지 않는지 확인

### 미니맵 클릭

- MinimapPanel에 Button이 연결되어 있는지 확인
- MinimapPanel Image의 Raycast Target이 활성화되어 있는지 확인
- MapRawImage의 Raycast Target이 비활성화되어 있는지 확인
- PlayerDirectionIcon의 Raycast Target이 비활성화되어 있는지 확인
- Alt를 누르지 않은 상태에서 미니맵 클릭이 실행되지 않는지 확인
- Alt로 커서를 활성화한 뒤 미니맵을 클릭할 수 있는지 확인
- 미니맵 클릭 시 M과 같은 전체 지도 패널이 열리는지 확인
- 지도 프리팹이 중복 생성되지 않는지 확인

### 전체 지도

- M으로 전체 지도가 열리는지 확인
- M으로 전체 지도가 닫히는지 확인
- 미니맵 클릭으로 같은 지도가 열리는지 확인
- CLOSE 버튼으로 지도가 닫히는지 확인
- ESC로 지도만 먼저 닫히는지 확인
- 지도 뒤 Gameplay 입력이 차단되는지 확인
- 전체 지도 사용 중 Time.timeScale이 0인지 확인
- 지도 종료 후 시간 배율이 복구되는지 확인
- 지도 종료 후 기존 미니맵 크기가 복구되는지 확인

### 휠 줌

- 전체 지도에서 휠 위로 줌인되는지 확인
- 전체 지도에서 휠 아래로 줌아웃되는지 확인
- Minimum Orthographic Size보다 작아지지 않는지 확인
- Maximum Orthographic Size보다 커지지 않는지 확인
- Gameplay 상태에서는 기존 카메라 휠 줌이 유지되는지 확인
- 전체 지도를 닫았다가 다시 열어도 마지막 줌값이 유지되는지 확인

### UI 충돌

- 인벤토리가 열린 상태에서 M으로 전체 지도를 열 수 있는지 확인
- 전체 지도 전환 시 인벤토리가 닫히는지 확인
- 보관함 상태에서도 동일하게 동작하는지 확인
- Pause Menu가 열린 상태에서 M과 N 입력이 차단되는지 확인
- 전체 지도 ESC 종료 후 Pause Menu가 동시에 열리지 않는지 확인
- 두 번째 ESC에서 Pause Menu가 열리는지 확인

---

## 해결한 문제

```text
Gameplay에 미니맵이 없는 문제
→ 위쪽 Orthographic 지도 Camera와 HUD RawImage 구현

미니맵에서 주변 범위를 조절할 수 없는 문제
→ N Compact / Expanded 전환 추가

전체 지도를 확인할 수 없는 문제
→ M 전체 지도 패널 구현

Alt+N이 기존 Alt 커서 기능과 역할이 겹치는 문제
→ Alt+N 지도 입력 제거

커서를 활성화한 상태에서도 미니맵을 직접 사용할 수 없는 문제
→ MinimapPanel Button과 클릭 이벤트 추가

일반 Gameplay에서 우발적으로 미니맵 클릭 이벤트가 발생하는 문제
→ Cursor.visible과 CursorLockMode 검사

M과 미니맵 클릭이 서로 다른 지도 화면을 만들 수 있는 문제
→ 동일한 WorldMapController와 프리팹 인스턴스 공유

전체 지도에서 표시 범위를 조절할 수 없는 문제
→ 마우스 휠 Orthographic Size 줌 추가

지도가 지나치게 확대 또는 축소될 수 있는 문제
→ Minimum과 Maximum Orthographic Size 제한

전체 지도와 인벤토리·보관함이 동시에 표시되는 문제
→ 기존 팝업 자동 종료와 LateUpdate 충돌 검사

전체 지도 ESC 종료와 Pause Menu가 동시에 실행되는 문제
→ PauseMenuController의 프레임 시작 지도 상태 검사

전체 지도 종료 후 미니맵 크기가 초기화되는 문제
→ 기존 Compact / Expanded 상태와 Camera 범위 복구
```

---

## 작업 결과

58일차에는 플레이어 주변 지형을 실시간으로 표시하는 미니맵과 전체 화면 지도 시스템을 구현하였다.

지도 전용 Orthographic Camera가 플레이어 위를 따라가며 런타임 RenderTexture를 생성하고, 해당 영상을 Gameplay HUD의 미니맵과 전체 지도 패널에서 공동으로 사용하도록 구성하였다.

기본 미니맵은 화면 오른쪽 위에 표시되며 `N` 키로 작은 상태와 확장 상태를 전환할 수 있다. 미니맵 UI 크기와 함께 지도 카메라 표시 범위도 변경되어 확장 상태에서 더 넓은 주변을 확인할 수 있다.

전체 지도는 `M` 키로 열고 닫을 수 있으며, 기존 `Alt` 키로 마우스 커서를 활성화한 뒤 미니맵을 직접 클릭해도 같은 전체 지도 패널이 열린다.

기존 계획의 `Alt + N` 입력은 Alt 커서 기능과 역할이 겹치므로 제거하였다. Alt는 커서 활성화에만 사용하고, 미니맵은 실제 UI 클릭 대상으로 변경하였다.

전체 지도에서는 마우스 휠을 통해 줌인과 줌아웃을 수행할 수 있다. Orthographic Size를 변경하는 방식으로 구현하였으며, 최소·최대 범위를 지정해 과도한 확대와 축소를 방지하였다.

전체 지도 프리팹은 최초 사용 시 `PopupLayer` 아래에 생성하고 이후 재사용한다. 전체 지도 사용 중에는 `GameplayInputLock`과 `Time.timeScale`을 통해 Gameplay 입력과 시간을 정지하며, 지도를 닫으면 기존 입력 상태와 미니맵 크기를 복구한다.

마지막으로 인벤토리, 보관함, 일시정지 메뉴와 전체 지도가 동시에 표시되지 않도록 충돌 방지 처리를 적용하고, 전체 지도에서 ESC를 눌렀을 때 지도만 닫힌 뒤 다음 ESC에서 일시정지 메뉴가 열리도록 입력 우선순위를 정리하였다.

---

## 커밋 정보

```text
58일차 : 미니맵 클릭 전체 지도 및 휠 줌 구현
```
