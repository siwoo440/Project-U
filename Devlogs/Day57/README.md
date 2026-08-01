# Project U 개발 일지

## 57일차 : 좌측 슬라이드 일시정지 메뉴와 설정·저장·불러오기 기능 구현

### 개발 목표

57일차에는 Gameplay Scene에서 ESC 키를 눌러 사용할 수 있는 일시정지 메뉴를 구현하였다.

일시정지 메뉴는 게임 시작 시 Scene에 고정 배치하지 않고, 최초 ESC 입력 시 `PopupLayer` 아래에 프리팹으로 생성한다. 생성된 인스턴스는 메뉴를 닫아도 삭제하지 않고 비활성화하여 이후 다시 열 때 재사용하도록 구성하였다.

기존 중앙 배치 방식 대신 화면 왼쪽에 붙은 Drawer 형태로 구성하고, 메뉴가 화면 왼쪽 바깥에서 안쪽으로 부드럽게 나타나는 슬라이드 연출을 추가하였다.

또한 일시정지 메뉴에 설정, 저장, 불러오기, 메인 메뉴 이동과 게임 종료 기능을 추가하고, 기존 HUD보다 항상 위에 표시되도록 별도 Canvas 정렬 구조를 적용하였다.

마지막으로 설정 페이지의 초기화 순서에 따라 일시정지 프리팹 생성이 실패하던 문제를 수정하였다.

---

### 구현 내용

#### 1. ESC 일시정지 메뉴 구현

다음 스크립트를 추가하였다.

```text
Assets/_ProjectU/Scripts/UI/Pause/PauseMenuController.cs
Assets/_ProjectU/Scripts/UI/Pause/PauseMenuView.cs
```

`PauseMenuController`는 ESC 입력과 전체 일시정지 상태를 관리한다.

주요 동작은 다음과 같다.

```text
ESC 입력 감지
일시정지 메뉴 최초 생성
생성된 메뉴 인스턴스 재사용
GameplayInputLock 획득과 해제
Time.timeScale 정지와 복구
인벤토리와 보관함 팝업 충돌 방지
메인 메뉴 Scene 이동
게임 종료
```

---

#### 2. 런타임 프리팹 생성과 재사용

일시정지 메뉴는 다음 프리팹으로 구성하였다.

```text
Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_PauseMenu.prefab
```

게임 시작 직후에는 `PopupLayer` 아래에 일시정지 메뉴가 존재하지 않는다.

최초 ESC 입력 시 다음 구조로 생성된다.

```text
Canvas
└─ PopupLayer
   └─ PF_UI_PauseMenu
```

메뉴를 닫아도 인스턴스를 제거하지 않고 화면만 비활성화한다.

따라서 ESC를 여러 번 눌러도 다음처럼 중복 인스턴스가 생성되지 않는다.

```text
정상

PopupLayer
└─ PF_UI_PauseMenu
```

```text
비정상

PopupLayer
├─ PF_UI_PauseMenu
├─ PF_UI_PauseMenu
└─ PF_UI_PauseMenu
```

---

#### 3. GameplayInputLock 연동

일시정지 메뉴가 열리면 `GameplayInputLock`에 다음 잠금 ID를 등록한다.

```text
PauseMenu
```

일시정지 중에는 다음 Gameplay 기능이 정지한다.

```text
플레이어 이동
카메라 회전
상호작용
Hotbar 입력
건축 입력
기타 Gameplay 입력 컴포넌트
```

메뉴를 닫으면 `PauseMenu` 잠금을 해제하고 Gameplay 입력을 복구한다.

`PauseMenuController` 자체는 `GameplayInputLock`의 `Behaviours To Disable` 목록에 포함하지 않는다.

---

#### 4. 게임 시간 정지

일시정지 메뉴를 열 때 다음 값을 적용한다.

```csharp
Time.timeScale = 0f;
```

메뉴를 닫을 때는 일시정지 이전의 시간 배율을 복구한다.

메인 메뉴로 이동하거나 게임을 종료할 때는 다음 값으로 정리한다.

```csharp
Time.timeScale = 1f;
```

이를 통해 일시정지 상태가 다른 Scene까지 이어지는 문제를 방지하였다.

---

#### 5. 기존 팝업과 ESC 입력 충돌 방지

일반 인벤토리 또는 보관함이 열린 상태에서는 ESC 입력 한 번으로 일시정지 메뉴까지 동시에 열리지 않도록 구성하였다.

입력 흐름은 다음과 같다.

```text
I
→ 일반 인벤토리 열림

ESC
→ 일반 인벤토리만 닫힘

ESC
→ 일시정지 메뉴 열림
```

보관함도 동일하게 처리된다.

```text
F
→ 보관함 열림

ESC
→ 보관함만 닫힘

ESC
→ 일시정지 메뉴 열림
```

일시정지 상태에서 I 키로 인벤토리가 열리더라도 `PauseMenuController`가 즉시 닫아 일시정지 메뉴와 일반 팝업이 동시에 표시되지 않도록 구성하였다.

---

### 좌측 슬라이드 메뉴

#### 6. 좌측 Drawer 구조 적용

기존 중앙 메뉴를 화면 왼쪽에 붙은 세로형 Drawer 메뉴로 변경하였다.

최종 프리팹 구조는 다음과 같다.

```text
PF_UI_PauseMenu
├─ Canvas
├─ GraphicRaycaster
├─ CanvasGroup
├─ Image
├─ PauseMenuView
└─ DrawerPanel
   ├─ MainPage
   └─ SettingsPage
```

`PF_UI_PauseMenu` 루트는 전체 화면의 어두운 배경과 입력 차단을 담당한다.

`DrawerPanel`만 실제로 좌우 이동한다.

---

#### 7. DrawerPanel RectTransform 구성

`DrawerPanel`은 화면 왼쪽을 기준으로 배치하였다.

```text
Anchor Min X: 0
Anchor Max X: 0
Pivot X: 0
Position X: 0
```

기본 너비는 다음 값으로 구성하였다.

```text
Width: 420
```

열린 상태에서는 화면 왼쪽 가장자리에 붙는다.

닫힌 상태에서는 다음 거리만큼 왼쪽으로 이동한다.

```text
DrawerPanel 너비 + Hidden Padding
```

---

#### 8. 슬라이드 애니메이션

`PauseMenuView`에 실제 시간 기반 슬라이드 애니메이션을 추가하였다.

```text
Transition Duration: 0.28초
Hidden Padding: 30
```

메뉴를 열 때:

```text
검은 배경 Fade In
DrawerPanel 왼쪽 화면 밖에서 Slide In
버튼 입력 활성화
RESUME 버튼 기본 선택
```

메뉴를 닫을 때:

```text
버튼 입력 차단
DrawerPanel 왼쪽 화면 밖으로 Slide Out
검은 배경 Fade Out
애니메이션 완료 후 루트 비활성화
Gameplay 입력과 시간 복구
```

---

#### 9. Time.timeScale과 무관한 애니메이션

게임이 일시정지되면 `Time.timeScale`이 0이 되므로 일반 `Time.deltaTime`으로는 UI 애니메이션이 움직이지 않는다.

이를 해결하기 위해 다음 값을 사용하였다.

```csharp
Time.unscaledDeltaTime
```

따라서 Gameplay 시간이 정지한 상태에서도 일시정지 메뉴는 정상적으로 열리고 닫힌다.

---

### UI 정렬 구조

#### 10. 기존 HUD보다 위에 표시

일시정지 메뉴가 체력, 허기, 갈증, 날씨, 시간과 Hotbar UI 아래에 표시되는 문제가 발생하였다.

이를 해결하기 위해 `PF_UI_PauseMenu` 루트에 별도 Canvas를 추가하였다.

설정은 다음과 같다.

```text
Override Sorting: 체크
Sorting Layer: Default
Order in Layer: 5000
```

`GraphicRaycaster`도 함께 추가하여 Pause Menu 버튼 입력을 처리하고, 아래쪽 HUD 입력을 차단하도록 구성하였다.

---

#### 11. 전체 화면 배경

`PF_UI_PauseMenu` 루트는 전체 Stretch로 설정하였다.

```text
Anchor Min: 0, 0
Anchor Max: 1, 1

Left: 0
Right: 0
Top: 0
Bottom: 0
```

루트 Image는 반투명한 검은색으로 설정하여 Gameplay와 기존 HUD를 어둡게 가린다.

```text
R: 0
G: 0
B: 0
A: 약 0.8
```

`Raycast Target`은 활성화하여 일시정지 화면 뒤의 UI를 클릭할 수 없게 구성하였다.

---

### 일시정지 메뉴 확장

#### 12. 메인 페이지 구성

일시정지 메뉴의 메인 페이지를 다음 버튼으로 구성하였다.

```text
RESUME
SETTINGS
SAVE GAME
LOAD GAME
MAIN MENU
QUIT GAME
```

Hierarchy 구조:

```text
MainPage
├─ TitleText
├─ ResumeButton
├─ SettingsButton
├─ SaveButton
├─ LoadButton
├─ MainMenuButton
├─ QuitButton
└─ ActionStatusText
```

버튼은 `VerticalLayoutGroup`과 `LayoutElement`를 이용해 세로로 정렬하였다.

---

#### 13. 버튼 이벤트 자동 연결

모든 버튼의 Inspector `On Click()`은 비워 두고, `PauseMenuView`가 런타임에 이벤트를 연결하도록 구성하였다.

```text
ResumeButton
→ ClosePauseMenu

SettingsButton
→ SettingsPage 표시

SaveButton
→ SaveCurrentGameFromPause

LoadButton
→ LoadCurrentGameFromPause

MainMenuButton
→ ReturnToMainMenu

QuitButton
→ QuitGame
```

이를 통해 프리팹의 수동 이벤트 연결 오류와 중복 호출을 방지하였다.

---

### 설정 화면

#### 14. 설정 페이지 추가

다음 스크립트를 새로 추가하였다.

```text
Assets/_ProjectU/Scripts/UI/Pause/PauseSettingsPanel.cs
Assets/_ProjectU/Scripts/Settings/GameSettingsService.cs
```

설정 페이지는 다음 항목으로 구성하였다.

```text
MASTER VOLUME
MOUSE SENSITIVITY
FULLSCREEN
APPLY
BACK
```

Hierarchy 구조:

```text
SettingsPage
├─ SettingsTitleText
├─ MasterVolumeLabel
├─ MasterVolumeSlider
├─ MasterVolumeValueText
├─ MouseSensitivityLabel
├─ MouseSensitivitySlider
├─ MouseSensitivityValueText
├─ FullscreenToggle
├─ ApplyButton
├─ BackButton
└─ SettingsStatusText
```

---

#### 15. 마스터 볼륨 설정

마스터 볼륨은 다음 범위로 구성하였다.

```text
최소값: 0
최대값: 1
기본값: 1
```

화면에는 백분율로 표시한다.

```text
0 → 0%
0.5 → 50%
1 → 100%
```

설정 적용 시 다음 값으로 전체 Unity 오디오 볼륨을 변경한다.

```csharp
AudioListener.volume
```

---

#### 16. 마우스 감도 설정

마우스 감도는 다음 범위로 구성하였다.

```text
최소값: 0.02
최대값: 0.50
기본값: 0.10
```

`ThirdPersonCameraFollow`에 다음 외부 적용 기능을 추가하였다.

```csharp
public float MouseSensitivity { get; }

public void SetMouseSensitivity(float targetSensitivity)
```

설정을 적용한 뒤 Gameplay로 돌아오면 카메라 회전 속도에 즉시 반영된다.

---

#### 17. 전체 화면 설정

`FullscreenToggle`을 통해 전체 화면 여부를 설정할 수 있도록 구성하였다.

```text
체크
→ 전체 화면

체크 해제
→ 창 모드
```

설정 적용 시 다음 값을 변경한다.

```csharp
Screen.fullScreen
```

---

#### 18. 설정값 저장

설정값은 `PlayerPrefs`에 저장한다.

저장 키는 다음과 같다.

```text
ProjectU.MasterVolume
ProjectU.MouseSensitivity
ProjectU.Fullscreen
```

`APPLY` 버튼을 누르면 다음 순서로 처리한다.

```text
현재 UI 값 읽기
값 범위 제한
PlayerPrefs 저장
PlayerPrefs.Save 실행
마스터 볼륨 즉시 적용
마우스 감도 즉시 적용
전체 화면 상태 즉시 적용
```

게임을 다시 실행하면 저장된 설정값을 불러와 적용한다.

---

#### 19. 설정 페이지 이동

`SETTINGS` 버튼을 누르면 다음처럼 페이지를 전환한다.

```text
MainPage 비활성화
SettingsPage 활성화
```

`BACK` 버튼을 누르면 다음처럼 복귀한다.

```text
SettingsPage 비활성화
MainPage 활성화
```

설정 화면에서 ESC를 누르면 Pause Menu를 닫지 않고 MainPage로 먼저 돌아가도록 구성하였다.

입력 흐름:

```text
Pause MainPage
→ SETTINGS
→ SettingsPage
→ ESC
→ Pause MainPage
→ ESC
→ Gameplay 복귀
```

---

### 저장과 불러오기

#### 20. 기존 GameplaySaveController 재사용

프로젝트에 이미 구현된 `GameplaySaveController`를 그대로 사용하였다.

`SAVE GAME` 버튼:

```csharp
GameplaySaveController.SaveCurrentGame();
```

`LOAD GAME` 버튼:

```csharp
GameplaySaveController.LoadCurrentGame();
```

별도의 중복 저장 시스템은 만들지 않았다.

---

#### 21. 저장 대상

기존 Gameplay 저장 시스템을 통해 다음 내용을 저장한다.

```text
플레이어 위치와 회전
체력
허기와 갈증
스태미나
젖음과 체온
날짜와 시간
날씨
인벤토리
장비
월드 상태
건축물
보관함
부활 지점
```

저장 성공과 실패의 상세 결과는 기존 `GameplaySaveController`가 Console에 출력한다.

---

#### 22. 저장과 불러오기 안내

메인 페이지의 `ActionStatusText`를 통해 저장과 불러오기 명령 실행 상태를 표시한다.

저장 버튼:

```text
SAVING...

SAVE COMMAND FINISHED
CHECK CONSOLE FOR RESULT
```

불러오기 버튼:

```text
LOADING...

LOAD COMMAND FINISHED
CHECK CONSOLE FOR RESULT
```

실제 성공 여부는 Console의 기존 저장 시스템 로그를 통해 확인한다.

---

### 초기화 오류 수정

#### 23. ESC 입력 후 메뉴가 표시되지 않는 문제

설정, 저장과 불러오기 기능을 추가한 뒤 ESC를 눌러도 일시정지 메뉴가 표시되지 않는 문제가 발생하였다.

Scene의 `PauseMenuController`와 다음 참조는 모두 정상적으로 연결되어 있었다.

```text
GameplayInputLock
GameUIManager
GameplaySaveController
ThirdPersonCameraFollow
PopupLayer
PF_UI_PauseMenu
```

Pause Menu 프리팹 내부의 `PauseMenuView`와 `PauseSettingsPanel` 참조도 모두 연결되어 있었다.

---

#### 24. 문제 원인

문제는 부모와 자식 컴포넌트의 `Awake()` 실행 순서에 있었다.

기존 구조에서는 `PauseMenuView.Awake()`가 먼저 실행될 경우 다음 순서가 발생할 수 있었다.

```text
PauseMenuView.Awake 실행
→ ShowMainPage 실행
→ PauseSettingsPanel.Hide 실행
→ SettingsPage 비활성화
→ PauseSettingsPanel.Awake가 실행되지 않음
→ internalReferencesValid가 false 상태 유지
→ PauseSettingsPanel.Initialize 실패
→ PauseMenuView.Initialize 실패
→ 생성된 PF_UI_PauseMenu 즉시 삭제
```

이로 인해 ESC 입력은 감지되었지만 생성된 Pause Menu가 즉시 삭제되어 화면에 표시되지 않았다.

---

#### 25. 내부 초기화 보장 기능 추가

다음 두 스크립트를 수정하였다.

```text
Assets/_ProjectU/Scripts/UI/Pause/PauseMenuView.cs
Assets/_ProjectU/Scripts/UI/Pause/PauseSettingsPanel.cs
```

두 컴포넌트에 다음 내부 초기화 보장 기능을 추가하였다.

```csharp
private bool EnsureInternalInitialization()
```

이제 `Awake()`가 아직 실행되지 않았더라도 `Initialize()`, `Show()` 또는 `Hide()`가 내부 참조 검사와 이벤트 등록을 직접 수행할 수 있다.

---

#### 26. 수정된 초기화 순서

수정 후 Pause Menu 생성 과정은 다음과 같다.

```text
PF_UI_PauseMenu 생성
→ PauseMenuView 내부 초기화 보장
→ PauseSettingsPanel 내부 초기화 보장
→ Slider 범위 설정
→ UI 이벤트 등록
→ 저장된 설정값 반영
→ SettingsPage 숨김
→ MainPage 표시
→ Pause Menu 슬라이드 표시
```

Unity의 부모와 자식 `Awake()` 실행 순서에 의존하지 않게 되었다.

---

#### 27. 설정 페이지 비활성화 시점 변경

기존에는 `PauseSettingsPanel.Awake()`에서 SettingsPage를 즉시 비활성화하였다.

수정 후에는 다음 초기화가 모두 끝난 다음 SettingsPage를 숨긴다.

```text
내부 참조 검사
Slider 범위 설정
이벤트 등록
PauseMenuView 연결
ThirdPersonCameraFollow 연결
저장된 설정값 반영
SettingsPage 비활성화
```

이를 통해 비활성화된 자식 오브젝트의 `Awake()`가 실행되지 않아 초기화가 실패하는 문제를 방지하였다.

---

### 주요 생성 및 수정 파일

```text
Assets/_ProjectU/Scenes/20_Gameplay.unity

Assets/_ProjectU/Prefabs/UI/Popups/PF_UI_PauseMenu.prefab

Assets/_ProjectU/Scripts/UI/Pause/PauseMenuController.cs
Assets/_ProjectU/Scripts/UI/Pause/PauseMenuView.cs
Assets/_ProjectU/Scripts/UI/Pause/PauseSettingsPanel.cs

Assets/_ProjectU/Scripts/Settings/GameSettingsService.cs

Assets/_ProjectU/Scripts/Camera/ThirdPersonCameraFollow.cs
```

---

### 최종 Pause Menu 구조

```text
PF_UI_PauseMenu
├─ Canvas
│  ├─ Override Sorting: 체크
│  └─ Order in Layer: 5000
├─ GraphicRaycaster
├─ CanvasGroup
├─ Image
├─ PauseMenuView
└─ DrawerPanel
   ├─ MainPage
   │  ├─ TitleText
   │  ├─ ResumeButton
   │  ├─ SettingsButton
   │  ├─ SaveButton
   │  ├─ LoadButton
   │  ├─ MainMenuButton
   │  ├─ QuitButton
   │  └─ ActionStatusText
   │
   └─ SettingsPage
      ├─ PauseSettingsPanel
      ├─ SettingsTitleText
      ├─ MasterVolumeLabel
      ├─ MasterVolumeSlider
      ├─ MasterVolumeValueText
      ├─ MouseSensitivityLabel
      ├─ MouseSensitivitySlider
      ├─ MouseSensitivityValueText
      ├─ FullscreenToggle
      ├─ ApplyButton
      ├─ BackButton
      └─ SettingsStatusText
```

---

### 테스트 항목

#### 일시정지 기본 동작

- 아무 팝업도 없는 상태에서 ESC로 Pause Menu가 열리는지 확인
- ESC로 Pause Menu가 닫히는지 확인
- RESUME 버튼으로 Gameplay에 복귀하는지 확인
- 메뉴를 여러 번 열어도 인스턴스가 한 개만 존재하는지 확인
- 메뉴가 열린 동안 플레이어 이동이 정지하는지 확인
- 메뉴가 열린 동안 카메라 회전이 정지하는지 확인
- 메뉴가 열린 동안 Hotbar와 건축 입력이 정지하는지 확인
- 메뉴를 닫으면 모든 Gameplay 입력이 복구되는지 확인

#### 좌측 슬라이드

- DrawerPanel이 화면 왼쪽 바깥에서 나타나는지 확인
- 열린 상태에서 화면 왼쪽 가장자리에 붙는지 확인
- 닫을 때 왼쪽 화면 밖으로 들어가는지 확인
- `Time.timeScale = 0` 상태에서도 애니메이션이 작동하는지 확인
- 닫기 애니메이션 완료 후 Gameplay 시간이 복구되는지 확인

#### UI 정렬

- Pause Menu가 체력 UI보다 위에 표시되는지 확인
- Pause Menu가 허기와 갈증 UI보다 위에 표시되는지 확인
- Pause Menu가 날씨와 시간 UI보다 위에 표시되는지 확인
- Pause Menu가 Hotbar보다 위에 표시되는지 확인
- Pause Menu 뒤쪽 HUD를 클릭할 수 없는지 확인

#### 팝업 충돌

- 인벤토리가 열린 상태에서 첫 ESC는 인벤토리만 닫는지 확인
- 두 번째 ESC에서 Pause Menu가 열리는지 확인
- 보관함이 열린 상태에서도 동일하게 동작하는지 확인
- Pause Menu가 열린 상태에서 I 키로 인벤토리가 표시되지 않는지 확인
- Pause Menu가 열린 상태에서 B 키로 건축 모드에 진입하지 않는지 확인

#### 설정 화면

- SETTINGS 버튼으로 SettingsPage가 표시되는지 확인
- BACK 버튼으로 MainPage가 표시되는지 확인
- SettingsPage에서 ESC를 누르면 MainPage로 복귀하는지 확인
- 마스터 볼륨 값이 백분율로 표시되는지 확인
- 마우스 감도 값이 소수점 두 자리로 표시되는지 확인
- Fullscreen Toggle이 정상적으로 작동하는지 확인
- APPLY 버튼을 누르면 설정이 즉시 적용되는지 확인
- 게임 재실행 후 설정값이 유지되는지 확인

#### 저장과 불러오기

- SAVE GAME 버튼으로 기존 저장 기능이 실행되는지 확인
- Console에 저장 성공 또는 실패 결과가 출력되는지 확인
- LOAD GAME 버튼으로 기존 불러오기 기능이 실행되는지 확인
- 플레이어 위치와 상태가 복구되는지 확인
- 인벤토리와 장비가 복구되는지 확인
- 건축물과 보관함이 복구되는지 확인
- 불러오기 후 Pause Menu가 계속 조작 가능한지 확인

#### 초기화 오류 회귀 테스트

- ESC 입력 시 `PF_UI_PauseMenu`가 즉시 삭제되지 않는지 확인
- `PauseMenuView 초기화에 실패했습니다.` 오류가 없는지 확인
- `PauseSettingsPanel 초기화에 실패했습니다.` 오류가 없는지 확인
- SettingsPage를 프리팹에서 활성화한 상태로 저장했는지 확인
- 부모와 자식 Awake 순서와 관계없이 Pause Menu가 열리는지 확인

---

### 해결한 문제

```text
Gameplay에서 ESC를 눌러도 일시정지 기능이 없는 문제
→ PauseMenuController와 PauseMenuView 구현

일시정지 메뉴가 중앙에 고정되어 있는 문제
→ 화면 왼쪽 Drawer 구조와 슬라이드 애니메이션 적용

Time.timeScale이 0일 때 UI 애니메이션이 멈추는 문제
→ Time.unscaledDeltaTime 사용

일시정지 메뉴가 기존 HUD 아래에 표시되는 문제
→ 별도 Canvas와 Override Sorting 5000 적용

일시정지 메뉴 뒤의 HUD를 클릭할 수 있는 문제
→ GraphicRaycaster와 전체 화면 Raycast 차단 적용

인벤토리와 Pause Menu가 같은 ESC 입력으로 동시에 전환되는 문제
→ 프레임 시작 팝업 상태 기록과 LateUpdate 처리

설정 메뉴가 없는 문제
→ 마스터 볼륨, 마우스 감도와 전체 화면 설정 추가

설정값이 게임 재실행 후 초기화되는 문제
→ PlayerPrefs 기반 GameSettingsService 추가

Pause Menu에서 직접 저장하고 불러올 수 없는 문제
→ 기존 GameplaySaveController 기능 연결

ESC 입력 후 Pause Menu가 표시되지 않는 문제
→ PauseMenuView와 PauseSettingsPanel 초기화 순서 의존 제거

SettingsPage 비활성화로 자식 Awake가 실행되지 않는 문제
→ EnsureInternalInitialization 기반 자체 초기화 보장
```

---

### 작업 결과

57일차에는 Gameplay의 공통 UI 구조에 ESC 일시정지 메뉴를 추가하였다.

일시정지 메뉴는 `PopupLayer` 아래에 지연 생성되고 재사용되며, `GameplayInputLock`과 `Time.timeScale`을 이용해 Gameplay 입력과 시간을 안전하게 정지한다.

기존 중앙형 메뉴를 화면 왼쪽 Drawer 방식으로 변경하고, 검은 배경 Fade와 좌측 Slide 애니메이션을 추가하였다. 애니메이션은 `Time.unscaledDeltaTime`을 사용하므로 게임 시간이 정지한 상태에서도 정상적으로 동작한다.

일시정지 메뉴 프리팹에 별도 Canvas와 높은 Sorting Order를 적용하여 기존 체력, 생존 수치, 날씨, 시간과 Hotbar UI보다 항상 위에 표시되도록 수정하였다.

메뉴에는 RESUME, SETTINGS, SAVE GAME, LOAD GAME, MAIN MENU와 QUIT GAME 버튼을 추가하였다.

설정 화면에서는 마스터 볼륨, 마우스 감도와 전체 화면 상태를 변경할 수 있으며, 설정값은 `PlayerPrefs`에 저장되어 게임을 다시 실행해도 유지된다.

저장과 불러오기는 프로젝트에 이미 구현된 `GameplaySaveController`를 재사용하여 중복 저장 시스템을 만들지 않고 Pause Menu에서 기존 기능을 호출하도록 구성하였다.

마지막으로 부모 `PauseMenuView`가 자식 `PauseSettingsPanel`보다 먼저 실행되어 SettingsPage를 비활성화하면 설정 패널 초기화가 실패하고 Pause Menu 인스턴스가 즉시 삭제되던 문제를 수정하였다.

각 컴포넌트가 `EnsureInternalInitialization()`을 통해 자신의 내부 상태를 직접 초기화하도록 변경하여 Unity의 부모와 자식 `Awake()` 실행 순서에 의존하지 않게 되었다.

---

### 커밋 정보

```text
57일차 : 좌측 일시정지 메뉴와 설정·저장·불러오기 기능 구현
```
