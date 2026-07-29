# 프로젝트 U 30일차 개발 일지

## 개발 주제

침낭 부활 지점 및 사망 복귀 시스템 구현

## 개발 목표

- 침낭에서 수면하면 해당 침낭을 부활 지점으로 등록
- 사망 후 Scene 재시작 없이 등록된 지점으로 복귀
- 침낭이 없을 때 기본 시작 지점으로 부활
- 부활 시 생존 수치와 시간을 지정된 값으로 복구
- 활성 부활 지점으로 지정된 침낭의 철거 방지

## 구현 내용

### 건축 데이터 수정

- `BuildRecipe_WoodWall`의 `Recipe Id`를 `structure_wood_wall`로 복구
- 침낭과 나무 벽의 건축 데이터 식별자 충돌 방지

### 플레이어 부활 처리

- `PlayerRespawnSystem` 추가
- 기본 부활 지점과 침낭 부활 지점 관리
- 사망 상태에서만 부활할 수 있도록 제한
- `CharacterController`를 일시적으로 비활성화한 뒤 안전하게 위치 이동
- 부활 후 이동 및 낙하 상태 초기화

### 생존 수치 복구

- 부활 체력을 `50`으로 설정
- 부활 허기를 `50`으로 설정
- 부활 갈증을 `50`으로 설정
- 부활 시간을 오전 `08:00`으로 이동

### 침낭 부활 지점 등록

- 침낭에서 실제 수면에 성공했을 때만 부활 지점 등록
- 침낭 프리팹에 플레이어가 복귀할 `RespawnPoint` 추가
- 등록된 침낭에 `RESPAWN POINT ACTIVE` 안내 문구 표시
- 다른 침낭에서 수면하면 새로운 침낭으로 부활 지점 변경

### 활성 침낭 철거 제한

- 활성 부활 지점으로 등록된 침낭의 철거 차단
- 철거 시 `ACTIVE RESPAWN POINT` 안내 문구 표시
- 새로운 침낭을 등록한 뒤 이전 침낭을 철거할 수 있도록 처리

### 사망 UI 개선

- 기존 재시작 버튼을 부활 버튼으로 변경
- 버튼 문구를 `RESPAWN`으로 변경
- Scene을 다시 불러오지 않고 플레이어만 부활 처리
- 사망 전에 활성화되어 있던 이동, 카메라, 상호작용 기능 복구
- 부활 실패 시 사망 화면과 시간 정지 상태 유지

### 낙하 상태 초기화

- 부활 시 수직 속도 초기화
- 낙하 여부와 낙하 거리 초기화
- 이전 낙하 정보로 인해 부활 직후 피해가 다시 발생하는 문제 방지

## 테스트 결과

- 침낭 미등록 상태에서 기본 부활 지점 복귀 확인
- 침낭 수면 후 해당 침낭 발치로 부활 확인
- 부활 후 체력, 허기, 갈증이 각각 `50`으로 복구되는 것 확인
- 부활 후 시간이 오전 `08:00`으로 변경되는 것 확인
- 부활 후 이동, 카메라, 상호작용 정상 작동 확인
- 활성 침낭 철거 차단 확인
- 새로운 침낭 등록 후 이전 침낭 철거 가능 확인
- 낙하 사망 후 부활 시 낙하 상태 초기화 확인
- 기존 인벤토리와 건축물이 유지되는 것 확인
- Console 오류 없음 확인

## 주요 변경 파일

- `Assets/_ProjectU/Data/Building/BuildRecipe_WoodWall.asset`
- `Assets/_ProjectU/Prefabs/Building/SleepingBagPlaced.prefab`
- `Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity`
- `Assets/_ProjectU/Scripts/Player/PlayerMovement.cs`
- `Assets/_ProjectU/Scripts/Survival/PlayerDeathUI.cs`
- `Assets/_ProjectU/Scripts/Survival/PlayerHealth.cs`
- `Assets/_ProjectU/Scripts/Survival/PlayerHunger.cs`
- `Assets/_ProjectU/Scripts/Survival/PlayerRespawnSystem.cs`
- `Assets/_ProjectU/Scripts/Survival/PlayerThirst.cs`
- `Assets/_ProjectU/Scripts/Survival/SleepingBagInteractable.cs`

## 완료 상태

30일차 목표인 침낭 기반 부활 지점 등록과 사망 복귀 시스템 구현을 완료했다. 침낭은 수면 시설과 생존 거점의 역할을 함께 가지며, 플레이어는 사망 후 진행 중인 Scene의 건축물과 인벤토리를 유지한 상태로 복귀할 수 있다.
