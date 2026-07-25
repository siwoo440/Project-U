# 2일차 개발일지 — 기본 Scene 흐름 및 메뉴 전환 구성

## 작업 목표

게임 실행부터 메인 메뉴, 임시 게임 플레이 화면, 메인 메뉴 복귀까지 이어지는 기본 Scene 흐름을 구성한다.

## 완료 작업

- `00_Bootstrap` Scene 생성
- `10_MainMenu` Scene 생성
- `20_Gameplay` Scene 생성
- 실행 시 `00_Bootstrap`에서 `10_MainMenu`로 자동 이동하는 초기 진입 흐름 구성
- `SceneFlowManager`를 통한 비동기 Scene 전환 구조 구성
- 중복 Scene 전환 요청 방지 처리
- `START` 버튼을 통한 `20_Gameplay` 진입 구성
- `BACK TO MENU` 버튼을 통한 메인 메뉴 복귀 구성
- 메인 메뉴 Canvas, 배경, 제목, 시작 버튼 구성
- 게임 플레이 임시 필드용 Ground와 GameplayMarker 구성
- Build Profiles Scene List에 필수 Scene 등록
- `00_Bootstrap → 10_MainMenu → 20_Gameplay → 10_MainMenu` 왕복 흐름 확인
- Console 빨간색 Error 없이 Play Mode 실행 확인

## 생성 Scene

| 순서 | Scene | 역할 |
| ---: | --- | --- |
| 0 | `00_Bootstrap` | 게임 시작 시 전역 Scene 관리자 초기화 및 메인 메뉴 이동 |
| 1 | `10_MainMenu` | 게임 제목과 시작 버튼 표시 |
| 2 | `20_Gameplay` | 이후 실제 게임 플레이 기능을 추가할 임시 필드 |

## 생성 스크립트

| 파일 | 역할 |
| --- | --- |
| `Scripts/Core/SceneFlowManager.cs` | Scene 전환 요청 및 중복 전환 방지 |
| `Scripts/Core/BootstrapLoader.cs` | Bootstrap 실행 후 메인 메뉴 이동 |
| `Scripts/UI/SceneNavigationUI.cs` | 시작·돌아가기 버튼의 Scene 이동 처리 |

## 확인 결과

| 항목 | 결과 |
| --- | --- |
| 게임 시작 Scene | `00_Bootstrap` |
| 메인 메뉴 자동 진입 | 정상 |
| START 버튼 이동 | 정상 |
| BACK TO MENU 버튼 이동 | 정상 |
| Scene 왕복 | 정상 |
| Console Error | 0개 |

## 다음 작업

3일차에는 게임 전역 상태를 관리할 `GameManager`의 기본 구조를 만들고, 메인 메뉴와 게임 플레이에서 공통으로 사용할 게임 상태 초기화 규칙을 구성한다.
