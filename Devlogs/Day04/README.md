# 4일차 개발일지 - 플레이어 이동 및 3인칭 카메라 구현

## 개발 목표

게임 플레이 Scene에서 플레이어가 직접 이동할 수 있도록 기본 조작을 만들고, 마우스 시점 회전이 가능한 3인칭 카메라를 구성했다.

## 완료 내용

- Build Profiles의 Scene List에 남아 있던 삭제된 `SampleScene` 항목을 제거했다.
- `00_Bootstrap`, `10_MainMenu`, `20_Gameplay` Scene의 실행 순서를 유지했다.
- `PlayerMovement`를 생성해 `CharacterController` 기반 이동을 구현했다.
- `WASD`와 방향키를 이용한 걷기 이동을 구현했다.
- 왼쪽 Shift 입력 중 걷기보다 빠르게 이동하는 달리기를 구현했다.
- Space 입력으로 점프하고 중력에 따라 Ground로 내려오는 처리를 구현했다.
- 대각선 이동 속도가 직선 이동보다 빨라지지 않도록 이동 방향 크기를 제한했다.
- `Player` 루트와 `PlayerVisual` 임시 Capsule 외형을 구성했다.
- 부모 `CharacterController`와 중복되지 않도록 `PlayerVisual`의 Capsule Collider를 제거했다.
- `ThirdPersonCameraFollow`를 생성해 플레이어를 따라보는 3인칭 카메라를 구현했다.
- 마우스 이동으로 카메라의 좌우·상하 시점 회전을 구현했다.
- 카메라의 상하 회전 범위를 제한하고 Escape로 마우스 커서 잠금 상태를 전환하도록 구성했다.
- 플레이어 이동을 카메라의 수평 시선 방향 기준으로 변경했다.
- 플레이어와 카메라 회전에 X·Z축 기울기가 생기지 않도록 수평 회전만 적용했다.
- 메인 메뉴에서 게임 플레이 Scene으로 진입한 뒤 이동·점프·카메라 조작과 메뉴 복귀를 확인했다.

## 조작 방법

| 입력 | 기능 |
| --- | --- |
| `WASD` 또는 방향키 | 카메라 시선 방향 기준 이동 |
| 왼쪽 Shift | 달리기 |
| Space | 점프 |
| 마우스 이동 | 카메라 시점 회전 |
| Escape | 마우스 커서 잠금·해제 |

## 생성 및 수정 파일

```text
Assets/_ProjectU/Scripts/Player/PlayerMovement.cs
Assets/_ProjectU/Scripts/Camera/ThirdPersonCameraFollow.cs
Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity
ProjectSettings/EditorBuildSettings.asset
Devlogs/Day04/README.md
```

## 확인 결과

- `00_Bootstrap`에서 시작해 메인 메뉴로 정상 이동했다.
- START 버튼으로 `20_Gameplay`에 정상 진입했다.
- 걷기·달리기·점프가 정상 동작했다.
- 공중에서 추가 점프가 발생하지 않았다.
- 카메라가 마우스 이동에 따라 회전했고 상하 각도 제한이 적용됐다.
- 좌우 이동 중 카메라와 플레이어가 옆으로 기울지 않았다.
- BACK TO MENU 버튼으로 메인 메뉴에 정상 복귀했다.
- Console의 빨간색 Error가 없는 상태를 확인했다.

## 다음 작업

5일차에는 플레이어가 주변 오브젝트를 탐지하고 `F` 키로 상호작용할 수 있는 기본 상호작용 시스템을 구현한다.
