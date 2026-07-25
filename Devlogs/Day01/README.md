# 1일차 개발일지 — 프로젝트 초기 설정 및 폴더 구조 구성

## 작업 목표

Unity 개발 환경을 구성하고, 프로젝트 U에서 사용할 기본 폴더 구조와 파일 명명 규칙을 확정한다.

## 완료 작업

* Unity `6000.3.9f1` 기반 Universal 3D URP 프로젝트 생성
* Universal Render Pipeline 적용 확인
* Windows PC x86_64 플랫폼 기준 설정
* 기준 해상도 `1920×1080` 설정
* Fullscreen Window 화면 모드 설정
* Linear Color Space 설정
* Unity Input System 설치 및 활성화
* 키보드·마우스 기본 입력, 게임패드 보조 입력 방향 확정
* Visible Meta Files 설정
* Force Text 직렬화 설정
* 환경 확인용 Scene Play Mode 실행 확인
* `Assets/_ProjectU` 프로젝트 전용 폴더 생성
* Art, Audio, Data, Prefabs, Scenes, Scripts, Settings, UI, VFX 기본 폴더 구성
* 각 기능별 하위 폴더 구성
* 파일과 폴더 명명 규칙 확정
* 외부 에셋과 자체 제작 파일 분리 규칙 확정
* Unity용 `.gitignore` 파일 작성
* GitHub 저장소 초기 업로드 완료

## 구성 폴더

```text
Assets
└── _ProjectU
    ├── Animations
    ├── Art
    │   ├── Materials
    │   ├── Models
    │   └── Textures
    ├── Audio
    │   ├── Ambience
    │   ├── BGM
    │   └── SFX
    ├── Data
    │   ├── Characters
    │   ├── Items
    │   └── World
    ├── Prefabs
    │   ├── Characters
    │   ├── Environment
    │   ├── Gameplay
    │   └── UI
    ├── Scenes
    │   ├── Bootstrap
    │   ├── Gameplay
    │   ├── Menus
    │   └── Tests
    ├── Scripts
    │   ├── Core
    │   ├── Data
    │   ├── Editor
    │   ├── Input
    │   ├── Interaction
    │   ├── Inventory
    │   ├── Items
    │   ├── Player
    │   ├── Save
    │   ├── UI
    │   └── World
    ├── Settings
    │   ├── Input
    │   └── Rendering
    ├── UI
    │   ├── Fonts
    │   ├── Icons
    │   ├── Sprites
    │   └── Themes
    └── VFX
```

## GitHub 업로드 내용

* GitHub 저장소: `https://github.com/siwoo440/Project-U`
* 기본 브랜치: `main`
* 커밋 제목: `1일차 : 프로젝트 초기 설정 및 폴더 구조 구성`
* Unity 자동 생성 폴더 제외 규칙 적용

  * `Library`
  * `Temp`
  * `Logs`
  * `Obj`
  * `UserSettings`
  * IDE 개인 설정 파일

## 확인 결과

| 항목         | 결과                 |
| ---------- | ------------------ |
| Unity 버전   | `6000.3.9f1`       |
| 렌더 파이프라인   | URP                |
| 대상 플랫폼     | Windows PC x86_64  |
| 입력 시스템     | Unity Input System |
| 기준 해상도     | 1920×1080          |
| 색상 공간      | Linear             |
| Scene 실행   | 정상                 |
| Console 오류 | 빨간색 Error 없음       |
| GitHub 업로드 | 완료                 |

## 다음 작업

2일차에는 게임 시작 흐름에 사용할 `00_Bootstrap`, `10_MainMenu`, `20_Gameplay` Scene을 생성하고, 기본 Scene 전환 구조를 구성한다.

## 수정 예정 항목

* `Assets/Scenes/SampleScene.unity`를 `Assets/_ProjectU/Scenes/Tests/TST_EnvironmentSetup.unity`로 이동 및 이름 변경
* `DevLogs/Day01/README.md` 안의 잘못된 Markdown 이스케이프 문자 제거
* `.gitignore`에 `*.slnx` 추가
* 이미 업로드된 `Project U.slnx` 파일의 Git 추적 해제
