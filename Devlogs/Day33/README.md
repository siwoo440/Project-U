# Project U 개발 일지

---

## 33일차 : 플레이어 상태 및 게임 시간 저장 연동

### 개발 목표

32일차에 구현한 JSON 파일 시스템을 실제 게임 진행 데이터와 연결했다. 플레이어의 위치·방향·생존 수치와 게임 날짜·시간을 저장하고, Play Mode를 다시 실행한 뒤에도 저장 시점의 상태를 복원할 수 있도록 구성했다.

### 주요 구현 내용

- 플레이어 위치와 회전 저장 및 복원
- 카메라 좌우 시점과 플레이어 방향 동기화
- 체력·허기·갈증·스태미나 저장 및 복원
- 현재 날짜와 게임 시각 저장 및 복원
- 불러오기 직후 시간 HUD와 낮·밤 조명 갱신
- 순간 이동 전후 `CharacterController` 안전 처리
- 불러오기 이후 낙하 속도와 이동 상태 초기화
- 저장된 Scene과 현재 Scene 일치 여부 검사
- 기본 저장 파일 손상 시 기존 백업 복구 기능 유지
- Play Mode에서 실행할 수 있는 저장·불러오기 검사 메뉴 구성
- `UnityEditor` 전용 검사 스크립트를 `Editor` 폴더로 이동

### 변경된 스크립트

| 파일 | 변경 내용 |
|---|---|
| `Assets/_ProjectU/Scripts/Survival/PlayerHealth.cs` | 저장된 현재 체력 적용 기능 추가 |
| `Assets/_ProjectU/Scripts/Player/PlayerStamina.cs` | 저장된 스태미나와 탈진 상태 적용 기능 추가 |
| `Assets/_ProjectU/Scripts/Environment/DayNightCycle.cs` | 저장된 날짜·시간과 환경 상태 적용 기능 추가 |
| `Assets/_ProjectU/Scripts/Camera/ThirdPersonCameraFollow.cs` | 저장된 플레이어 방향에 맞춘 카메라 Y축 회전 적용 |

### 추가된 스크립트

| 파일 | 역할 |
|---|---|
| `Assets/_ProjectU/Scripts/Save/GameplaySaveController.cs` | 실제 플레이 상태 수집, JSON 저장, 불러온 상태 적용 관리 |

### Editor 스크립트 정리

빌드에 포함되면 안 되는 `UnityEditor` 기반 검사 스크립트를 다음 폴더로 이동했다.

```text
Assets/_ProjectU/Scripts/Editor/SaveSchemaValidator.cs
Assets/_ProjectU/Scripts/Editor/SaveFileSystemValidator.cs
```

이를 통해 저장 검사 메뉴는 Unity Editor에서 유지하면서 실제 게임 빌드에서는 제외되도록 정리했다.

### 저장 대상

| 구분 | 저장 데이터 |
|---|---|
| Scene | 현재 Scene 이름 |
| 플레이어 Transform | 위치, 회전 |
| 생존 상태 | 체력, 허기, 갈증, 스태미나 |
| 게임 시간 | 현재 날짜, 현재 시각 |

현재 저장 슬롯은 `slot_01`을 사용한다.

### 저장 처리 흐름

1. Play Mode와 필수 컴포넌트 참조 확인
2. 현재 Scene 이름 수집
3. 플레이어 위치와 회전 수집
4. 체력·허기·갈증·스태미나 수집
5. 현재 날짜와 시각 수집
6. `SaveGameData`에 수집한 값 기록
7. `SaveFileService`를 통해 `slot_01.json` 저장
8. 기존 정상 파일이 있으면 백업 파일 생성

### 불러오기 처리 흐름

1. 기본 저장 파일 또는 정상 백업 파일 읽기
2. 저장된 Scene과 현재 Scene 비교
3. 플레이어 위치·회전 데이터 검사
4. `CharacterController`를 잠시 비활성화
5. 플레이어 위치와 회전 적용
6. 카메라 좌우 방향 적용
7. `CharacterController` 기존 상태 복구
8. 이동과 낙하 상태 초기화
9. 체력·허기·갈증·스태미나 적용
10. 날짜·시각과 낮·밤 환경 즉시 갱신

### Scene 구성

`20_Gameplay` Scene에 저장 관리 오브젝트를 추가했다.

```text
=== SaveSystem ===
└── GameplaySaveController
```

`GameplaySaveController`에는 다음 참조를 연결했다.

| Inspector 필드 | 연결 대상 |
|---|---|
| Slot Id | `slot_01` |
| Player Transform | `Player` |
| Third Person Camera Follow | `Main Camera`의 `ThirdPersonCameraFollow` |
| Day Night Cycle | `DayNightCycle` 컴포넌트가 있는 오브젝트 |

### 검증 결과

- `Save Data Schema` 검사 성공
- `Save File System` 검사 성공
- 플레이어 위치와 방향 저장·복원 성공
- 카메라 좌우 방향 복원 성공
- 체력·허기·갈증·스태미나 복원 성공
- 날짜와 게임 시각 복원 성공
- 시간 HUD와 조명 상태 즉시 갱신 성공
- Play Mode 종료 후 다시 실행한 상태에서 불러오기 성공
- 두 번째 저장 시 `slot_01.backup.json` 생성 확인
- 저장 완료 후 `.tmp` 파일 미잔류 확인
- 불러오기 이후 이동·달리기·점프·낙하 기능 정상
- 기존 인벤토리·채집·건축·조리·수면·부활 기능 정상
- Console 컴파일 오류 및 실행 오류 없음

### 이번 일차 제외 범위

- 자동 저장과 시작 시 자동 불러오기
- 저장·불러오기 UI
- 여러 저장 슬롯 선택과 삭제
- 인벤토리와 핫바 저장
- 장비 저장
- 월드 아이템과 채집 자원 상태 저장
- 설치 건축물 복원
- 모닥불과 침낭 상태 복원
- 다른 Scene으로 이동한 뒤 자동 불러오기
- 카메라 상하 각도와 확대 거리 저장

### 완료 기준

- 플레이어 Transform 저장 및 복원 완료
- 생존 수치 저장 및 복원 완료
- 날짜와 게임 시각 저장 및 복원 완료
- 카메라 방향과 플레이어 방향 동기화 완료
- 불러오기 직후 이동·낙하 상태 정상화
- 게임 재실행 이후 저장 데이터 유지
- 기본 파일과 백업 파일 복구 기능 유지
- 기존 게임 기능 회귀 테스트 완료
- Console 오류 없음

---

## 다음 개발 방향

34일차에는 인벤토리·핫바·장비 데이터를 아이템 ID 기준으로 저장하고 복원한다. 장비가 최대 체력과 같은 능력치를 변경할 수 있으므로 장비를 먼저 복원한 뒤 저장된 현재 체력을 적용하는 순서로 구성한다.
