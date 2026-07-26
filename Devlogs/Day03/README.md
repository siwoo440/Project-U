# 3일차 개발일지 - 게임 상태 관리 및 Scene 구조 정리

## 개발 목표

기본 Scene 전환 구조를 정리하고, 현재 게임 상태를 전역으로 관리할 수 있는 기반을 구성했다.

## 완료 내용

- `10_MainMenu` Scene을 `Assets/_ProjectU/Scenes/Menus` 경로로 이동했다.
- `20_Gameplay` Scene을 `Assets/_ProjectU/Scenes/Gameplay` 경로로 이동했다.
- 기본 `SampleScene`을 `TST_EnvironmentSetup`으로 정리하고 테스트용 Scene으로 분리했다.
- `00_Bootstrap` Scene에서 `Main Camera`와 `Directional Light`를 제거했다.
- Build Profiles의 Scene List를 다음 순서로 정리했다.

| 순서 | Scene |
| ---: | --- |
| 0 | `00_Bootstrap` |
| 1 | `10_MainMenu` |
| 2 | `20_Gameplay` |

- `GameState` 열거형을 생성해 `Bootstrap`, `MainMenu`, `Gameplay` 상태를 정의했다.
- `GameManager`를 생성해 Scene 로드 시 현재 게임 상태가 자동으로 갱신되도록 구현했다.
- `SceneFlowManager`에 Bootstrap Scene 이름 상수를 추가했다.
- `BootstrapLoader`에서 `GameManager` 존재 여부를 확인하도록 보완했다.
- `AppRoot`에 `GameManager`를 연결해 Scene 전환 후에도 전역 상태 관리자가 유지되도록 구성했다.
- `GameStateView`를 생성해 메인 메뉴와 게임 플레이 화면에서 현재 상태를 표시하도록 구현했다.
- `START`와 `BACK TO MENU` 버튼을 이용해 메인 메뉴와 게임 플레이 Scene의 왕복 전환을 확인했다.

## 확인 결과

- `00_Bootstrap` 실행 후 `10_MainMenu`로 정상 이동했다.
- 메인 메뉴에서 `STATE: MAIN MENU`가 표시됐다.
- START 버튼 입력 후 `20_Gameplay`로 정상 이동했다.
- 게임 플레이 화면에서 `STATE: GAMEPLAY`가 표시됐다.
- BACK TO MENU 버튼 입력 후 메인 메뉴로 정상 복귀했다.
- Console의 빨간색 Error가 없는 상태를 확인했다.

## 생성 및 수정 파일

```text
Assets/_ProjectU/Scripts/Core/GameState.cs
Assets/_ProjectU/Scripts/Core/GameManager.cs
Assets/_ProjectU/Scripts/Core/SceneFlowManager.cs
Assets/_ProjectU/Scripts/Core/BootstrapLoader.cs
Assets/_ProjectU/Scripts/UI/GameStateView.cs
Assets/_ProjectU/Scenes/Bootstrap/00_Bootstrap.unity
Assets/_ProjectU/Scenes/Menus/10_MainMenu.unity
Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity
ProjectSettings/EditorBuildSettings.asset
Devlogs/Day03/README.md
```
