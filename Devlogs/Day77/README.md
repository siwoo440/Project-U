# Project U 개발 일지

## 77일차 : 몬스터 스폰 상태 저장 및 사망·부활 흐름 연결

### 1. 개발 목표

77일차에는 74\~76일차에 만든 몬스터 전투 콘텐츠를 저장·사망·부활·이어하기 흐름과 연결하였다.

기획서 77일차 완료 기준:

```text
대표 몬스터 전투에서 획득한 보상과 플레이어 상태가 저장되고
사망·침낭 부활·이어하기 흐름이 정상 동작한다.
```

기존 프로젝트에는 저장·불러오기, 침낭 부활 지점, 사망 화면, 부활 처리가 이미 구현되어 있었다.

하지만 몬스터가 추가되면서 다음 문제가 생겼다.

| 문제 | 결과 |
| --- | --- |
| 몬스터 스폰 상태가 저장되지 않음 | 처치 → 저장 → 불러오기로 몬스터와 전리품을 무한히 얻을 수 있음 |
| 사망 상태 저장 차단 없음 | 체력 0으로 저장되면 이어하기 직후 사망 상태가 됨 |
| 플레이어 사망 이벤트 없음 | 다른 시스템이 사망·부활 시점에 반응하기 어려움 |
| 부활 직후 보호 없음 | 부활하자마자 주변 공격이나 투사체에 다시 맞을 수 있음 |

이번 일차의 핵심 목표는 다음과 같다.

- 플레이어 사망·부활 이벤트 추가
- 부활 직후 전투 보호 시간 적용
- 사망 상태 저장 차단
- 체력 0 저장 파일 불러오기 보정
- 적 Spawn Point 처치·재생성 상태 저장
- 불러오기 시 처치 상태와 남은 재생성 시간 복원
- 기존 저장 파일 호환 유지
- Spawn Point 저장 ID 발급 도구 추가

---

## 2. 기획서 사망·부활 규칙 확인

기획서의 사망 규칙은 다음과 같다.

```text
체력 0
→ 게임 종료가 아닌 부활 처리
→ 마지막 침낭 또는 기본 스폰 장소에서 부활
→ 아이템과 골드는 잃지 않음
→ 일정 시간 경과
→ 체력 일부 회복 상태
```

기존 `PlayerRespawnSystem.TryRespawn()`은 이미 다음을 처리하고 있었다.

- 등록 침낭 우선, 없으면 기본 부활 지점
- 부활 시각(기본 8시)으로 시간 이동
- 체력 50, 허기 50, 갈증 50 상태로 부활
- 젖음·체온 초기화
- 인벤토리 유지

따라서 이번 일차에는 부활 규칙 자체는 유지하고, 몬스터 전투와 저장 시스템에서 부족한 부분만 보완하였다.

또한 `EnemyCombatController.IsTargetValid()`가 이미 `targetReceiver.IsAlive`를 검사하고 있어 플레이어가 사망하면 적이 대상을 해제하는 것을 확인하였다.

---

## 3. PlayerHealth 이벤트 추가

`PlayerHealth`에 사망과 부활 이벤트를 추가하였다.

```text
PlayerHealth
├─ Damaged(float)              기존
├─ Healed(float)               기존
├─ CombatDamageBlocked(float)  기존
├─ Died()                      추가
└─ Revived()                   추가
```

### Died

피해 처리 중 체력이 0이 되어 사망 상태가 된 순간 한 번 전달한다.

```text
ApplyDamage
→ 체력 감소
→ Damaged 이벤트
→ 사망 상태 확인
→ Died 이벤트
```

### Revived

`Revive()`가 성공하면 회복 이벤트 뒤에 전달한다.

기존 `PlayerDeathUI`는 매 프레임 사망 상태를 검사하는 방식을 그대로 유지하고, 이후 추가되는 기능은 이벤트로 연결할 수 있도록 하였다.

---

## 4. 부활 직후 전투 보호

`PlayerHealth`에 외부에서 전투 무적을 요청하는 함수를 추가하였다.

```text
BeginCombatInvulnerability(float duration)
```

기존 피격 무적 종료 시각을 연장하는 방식이므로 기존 전투 무적 판정과 Inspector 표시를 그대로 사용한다.

`PlayerRespawnSystem`에는 다음 설정을 추가하였다.

```text
Respawn Protection Duration: 2
```

부활 흐름:

```text
사망 화면에서 부활 선택
→ 부활 위치 이동
→ 허기·갈증·젖음·체온 적용
→ 부활 시각 적용
→ PlayerHealth.Revive
→ 2초 전투 보호 시작
```

부활 직후 날아오던 투사체나 주변 몬스터의 공격 판정이 곧바로 적용되는 문제를 줄였다.

---

## 5. 사망 상태 저장 차단

`GameplaySaveController.SaveCurrentGame()`에서 플레이어 사망 여부를 먼저 확인한다.

```text
SaveCurrentGame
→ 저장 기능 사용 가능 확인
→ 플레이어 사망 상태 확인
   → 사망: 경고 출력 후 저장 중단
   → 생존: 기존 저장 진행
```

일시정지 메뉴 저장, 수면 자동 저장, 디버그 저장이 모두 이 함수를 사용하므로 한 곳에서 차단된다.

---

## 6. 체력 0 저장 파일 보정

이전 버전에서 체력 0 상태로 저장된 파일이 있을 수 있으므로 불러오기에서도 보정한다.

```text
저장 체력 > 0  → 그대로 적용
저장 체력 <= 0 → PlayerRespawnSystem.RespawnHealth 적용 + 경고 출력
```

`PlayerRespawnSystem`에 부활 체력을 읽을 수 있는 속성을 추가하였다.

```text
RespawnHealth
```

---

## 7. 적 스폰 상태 저장 설계

몬스터 개체 자체(위치, 체력, 추적 상태)를 저장하지 않고 **Spawn Point의 상태만 저장**하도록 설계하였다.

채집 자원의 `GatherableResourceSaveData`가 남은 재생성 시간을 저장하는 방식과 같은 구조이다.

선택 이유:

- 전투 중간 상태를 복원하는 것보다 단순하고 안정적
- 저장·불러오기 반복으로 전리품을 무한 획득하는 문제만 정확히 해결
- 살아 있는 몬스터는 불러오기 후 새로 생성되어도 게임 진행에 문제가 없음

---

## 8. 저장 데이터 구조

`SaveDataSchema`에 다음 데이터를 추가하였다.

```text
SaveGameData
├─ hasEnemySpawnData
└─ enemySpawns : EnemySpawnSaveData
   └─ spawnPoints : List<EnemySpawnPointSaveData>
      ├─ spawnPointId
      ├─ isDefeated
      └─ respawnRemainingSeconds
```

| 필드 | 의미 |
| --- | --- |
| `spawnPointId` | Scene 안에서 Spawn Point를 구분하는 고유 ID |
| `isDefeated` | 적이 처치되어 비어 있는 상태 |
| `respawnRemainingSeconds` | 재생성까지 남은 시간, 재생성하지 않는 지점은 -1 |

기존 저장 데이터와 같은 방식으로 `hasEnemySpawnData` 플래그를 사용하였다.

77일차 이전 저장 파일은 이 값이 `false`이므로 기존처럼 모든 몬스터가 생성된 상태로 불러온다. 따라서 `SaveVersionPolicy` 버전은 변경하지 않았다.

---

## 9. 저장 데이터 검증

`SaveDataValidator`에 적 스폰 데이터 검사를 추가하였다.

검사 항목:

- `hasEnemySpawnData`가 false면 이전 파일로 허용
- 저장 목록 존재 여부
- Spawn Point ID 빈 값
- Spawn Point ID 중복
- 남은 시간 NaN·Infinity
- 남은 시간 -1 미만

---

## 10. EnemySpawnPoint 수정

75일차 `EnemySpawnPoint`에 저장 기능을 추가하였다.

### 추가 설정

```text
Save
└─ Spawn Point Id
```

### 추가 실행 상태

```text
isPermanentlyCleared   재생성하지 않는 지점의 처치 완료 상태
respawnAtTime          재생성 예정 시각
```

### 추가 속성

```text
SpawnPointId
HasValidSpawnPointId
IsDefeated
RespawnRemainingSeconds
```

적이 사망하면 재생성 예정 시각을 기록하고, 저장 시 남은 시간을 계산한다.

재생성을 사용하지 않는 지점은 처치 후 영구 처치 상태로 기록한다.

### ApplySavedState

불러온 상태를 적용하는 함수를 추가하였다.

```text
ApplySavedState(isDefeated, respawnRemainingSeconds)
```

처리 흐름:

```text
진행 중인 생성·재생성 루틴 중단
↓
처치되지 않은 상태
→ 남아 있는 시체 정리
→ 적이 없으면 즉시 생성

처치된 상태
→ 현재 적 제거
→ 재생성 없음 또는 -1: 영구 처치 상태
→ 남은 시간만큼 기다린 뒤 재생성
```

남은 시간은 `0 ~ Respawn Delay` 범위로 제한한다.

### 불러오기 시점 대응

이어하기에서는 `SceneFlowManager`가 Gameplay Scene 로드 후 한 프레임 뒤에 불러오기를 실행한다.

그 시점에는 Spawn Point의 `Start()`가 이미 실행되어 적이 생성되어 있거나 첫 생성 대기 중일 수 있다.

따라서 `ApplySavedState`는 이미 생성된 적과 대기 중인 루틴을 모두 정리한 뒤 저장 상태를 적용하도록 구성하였다.

---

## 11. EnemySpawnSaveBridge

저장 연결을 위해 다음 스크립트를 추가하였다.

```text
Assets/_ProjectU/Scripts/Enemy/Day77/EnemySpawnSaveBridge.cs
```

기존 Save Bridge와 달리 Scene 컴포넌트가 아닌 정적 클래스로 구성하였다.

실행 시 Scene의 활성 `EnemySpawnPoint`를 자동으로 검색하므로 `GameplaySaveController`에 별도 참조를 연결하지 않아도 된다.

### TryCapture

```text
전체 Spawn Point 검색
→ ID 없는 지점은 경고 후 제외
→ ID 중복 시 저장 실패
→ 처치 상태와 남은 시간 수집
→ hasEnemySpawnData = true
```

### TryRestore

```text
hasEnemySpawnData 없음 → 기본 상태 유지
↓
저장 목록을 ID 기준으로 정리
↓
전체 Spawn Point 순회
→ 저장 항목 있음: 저장 상태 적용
→ 저장 항목 없음: 살아 있는 기본 상태 적용
```

저장 이후 새로 배치한 Spawn Point도 정상적으로 몬스터를 생성한다.

---

## 12. GameplaySaveController 연결

### 저장 순서

```text
플레이어·시간·인벤토리·제작법
→ 월드 상태
→ 설치 건축물
→ 보관함
→ 부활 지점
→ 적 스폰 지점   추가
→ 파일 저장
```

### 불러오기 순서

```text
제작법
→ 인벤토리·장비
→ 월드 상태
→ 설치 건축물
→ 보관함
→ 부활 지점
→ 적 스폰 지점   추가
→ 플레이어·시간·날씨 적용
```

---

## 13. Spawn Point ID 발급

### Scene ID 지정

`20_Gameplay` Scene의 Spawn Point 3개에 ID를 지정하였다.

| Spawn Point | ID |
| --- | --- |
| Spawn_MeleeGrunt_01 | `enemy_spawn_meleegrunt_01` |
| Spawn_MeleeGrunt_02 | `enemy_spawn_meleegrunt_02` |
| Spawn_RangedSpitter_01 | `enemy_spawn_rangedspitter_01` |

### 발급 도구

이후 Spawn Point를 추가할 때 사용할 Editor 메뉴를 추가하였다.

```text
Assets/_ProjectU/Scripts/Enemy/Day77/Editor/EnemySpawnPointIdTool.cs
```

```text
Tools
→ Project U
→ Assign Enemy Spawn Point IDs
```

기능:

- 현재 Scene의 전체 Spawn Point 검색
- ID가 비어 있거나 중복된 지점에 새 ID 발급
- Undo 기록
- Prefab 인스턴스 Override 기록
- Scene 변경 상태 표시

실행 결과:

```text
Enemy Spawn Point ID 검사 완료 / 전체 3개 / 새 ID 0개
```

Scene에 지정한 3개 ID가 모두 정상이며 중복이 없는 것을 확인하였다.

---

## 14. 전체 흐름

### 처치 후 저장·이어하기

```text
몬스터 처치
→ EnemySpawnPoint 재생성 대기 (예: 10초)
→ 4초 후 저장
→ 남은 시간 6초 기록
→ 메인 메뉴
→ 이어하기
→ Spawn Point 기본 생성 후 저장 상태 적용
→ 생성된 적 제거
→ 6초 후 재생성
```

### 전투 중 사망·부활

```text
플레이어 체력 0
→ PlayerHealth.Died
→ 적 대상 해제
→ 사망 화면 표시
→ 저장 시도 차단
→ 부활 선택
→ 침낭 또는 기본 부활 지점
→ 아이템 유지, 시간 경과, 체력 일부
→ PlayerHealth.Revived
→ 2초 전투 보호
```

---

## 15. 검증

### 컴파일

- Unity 6000.3.9f1 Roslyn 컴파일러로 `Assembly-CSharp` 컴파일 오류 없음
- 신규 Editor 도구를 포함한 `Assembly-CSharp-Editor` 컴파일 오류 없음
- 열린 Unity Editor에서 스크립트 Reload 후 Console 오류 없음
- Spawn Point ID 발급 도구 실행 결과 정상

### 기존 코드 영향

다음 기존 기능의 동작 방식은 변경하지 않았다.

```text
PlayerDeathUI 사망 화면 표시 방식
PlayerRespawnSystem 부활 위치·수치 규칙
EnemyCombatController 대상 해제 규칙
WorldSaveBridge 월드 아이템·채집 자원 저장
기존 저장 파일 버전
```

---

## 16. 신규 및 수정 파일

```text
Assets/_ProjectU/
├─ Scripts/
│  ├─ Enemy/
│  │  ├─ Day75/
│  │  │  └─ EnemySpawnPoint.cs                  수정
│  │  └─ Day77/
│  │     ├─ EnemySpawnSaveBridge.cs             추가
│  │     └─ Editor/
│  │        └─ EnemySpawnPointIdTool.cs         추가
│  ├─ Save/
│  │  ├─ GameplaySaveController.cs              수정
│  │  ├─ SaveDataSchema.cs                      수정
│  │  └─ SaveDataValidator.cs                   수정
│  └─ Survival/
│     ├─ PlayerHealth.cs                        수정
│     └─ PlayerRespawnSystem.cs                 수정
└─ Scenes/
   └─ 20_Gameplay.unity                         수정
```

---

## 17. 다음 개발 방향

78일차에는 대표 전투 콘텐츠 수직 슬라이스를 점검한다.

주요 작업 예정:

1. 탐색부터 거점 복귀까지 연속 플레이 루트 구성
2. 근접·원거리 몬스터 동시 전투 점검
3. 회피·근접 연속 공격·활 공격 연계 점검
4. 전리품 획득과 인벤토리·보관함 연결 점검
5. 거점 복귀 후 제작·저장 흐름 점검
6. 사망·부활·이어하기 반복 점검
7. 전투 수치와 스폰 간격 1차 조정
8. 발견된 불편 요소와 오류 정리

---

## 77일차 완료 상태

- [x] `PlayerHealth.Died` 이벤트 추가
- [x] `PlayerHealth.Revived` 이벤트 추가
- [x] `BeginCombatInvulnerability` 추가
- [x] 부활 직후 2초 전투 보호 적용
- [x] `RespawnHealth` 속성 추가
- [x] 사망 상태 저장 차단
- [x] 체력 0 저장 파일 불러오기 보정
- [x] `EnemySpawnSaveData` 저장 구조 추가
- [x] 적 스폰 저장 데이터 검증 추가
- [x] `EnemySpawnPoint` 저장 ID 추가
- [x] 처치 상태와 남은 재생성 시간 계산
- [x] 불러온 스폰 상태 적용
- [x] `EnemySpawnSaveBridge.cs` 추가
- [x] 저장·불러오기 순서에 적 스폰 상태 연결
- [x] 이전 저장 파일 호환 유지
- [x] Scene Spawn Point 3개 ID 지정
- [x] `EnemySpawnPointIdTool.cs` 추가
- [x] Spawn Point ID 검사 도구 실행
- [x] 스크립트 컴파일 오류 확인
- [ ] 처치 → 저장 → 이어하기 재생성 대기 Play 확인
- [ ] 전리품 저장·복원 Play 확인
- [ ] 사망 → 침낭·기본 부활 Play 확인
- [ ] 사망 상태 저장 차단 Play 확인

---

## Git Commit

```text
77일차 : 몬스터 스폰 상태 저장 및 사망·부활 흐름 연결
```
