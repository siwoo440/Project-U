# Project U 개발 일지

---

## 29일차 : 침낭 건축 및 수면 시스템 구현

### 개발 목표

28일차에 구현한 낮과 밤 시간 순환 시스템을 생존 플레이와 연결하기 위해 침낭 건축 및 수면 기능을 구현했다. 플레이어가 밤이나 새벽에 설치한 침낭을 사용하면 다음 오전 8시까지 시간이 이동하며, 체력 회복과 함께 허기와 갈증이 소비되도록 구성했다.

### 구현 내용

- 건축 목록에 침낭 제작 데이터 추가
- 침낭 설치 및 철거 기능 구현
- 설치된 침낭의 상호작용 기능 구현
- 낮 시간 수면 차단 기능 구현
- 밤과 새벽 시간 수면 허용 기능 구현
- 수면 시 화면 암전 효과 구현
- 수면 후 오전 8시로 시간 이동 기능 구현
- 저녁 수면 시 다음 날짜로 변경
- 새벽 수면 시 현재 날짜 유지
- 수면 시 체력 30 회복
- 수면 시 허기 15 소비
- 수면 시 갈증 20 소비
- 허기 또는 갈증 부족 시 수면 차단
- 수면 중 상호작용 및 건축 입력 차단
- 침낭 철거 시 제작 재료 일부 반환

### 추가 및 수정 파일

```text
Assets/_ProjectU/Scripts/Environment/DayNightCycle.cs
Assets/_ProjectU/Scripts/Environment/SleepSystem.cs
Assets/_ProjectU/Scripts/Survival/PlayerHunger.cs
Assets/_ProjectU/Scripts/Survival/PlayerThirst.cs
Assets/_ProjectU/Scripts/Survival/SleepingBagInteractable.cs
Assets/_ProjectU/Prefabs/Building/SleepingBagPlaced.prefab
Assets/_ProjectU/Prefabs/Building/SleepingBagPreview.prefab
Assets/_ProjectU/Data/Building/BuildRecipe_SleepingBag.asset
Assets/_ProjectU/Scenes/Gameplay/20_Gameplay.unity
```

### 오류 수정

- `SleepingBagPlaced`의 루트 레이어를 `Default`에서 `Structure`로 변경
- `PlayerInteractor`가 침낭 Collider를 감지하지 못하던 문제 해결
- 침낭 안내 문구가 표시되지 않던 문제 해결
- 침낭을 바라보고 F키를 눌러도 수면이 시작되지 않던 문제 해결
- `BuildRecipe_WoodWall`의 중복된 `Recipe Id`를 `structure_wood_wall`로 복구

### 테스트 결과

- 침낭 설치 시 목재 8개 소비 확인
- 낮에는 `SLEEP AT NIGHT` 문구 표시 확인
- 밤에는 `F - SLEEP UNTIL 08:00` 문구 표시 확인
- 수면 시 암전 후 오전 8시로 이동 확인
- 저녁 수면 시 날짜 증가 확인
- 새벽 수면 시 날짜 유지 확인
- 체력 30 회복 확인
- 허기 15 및 갈증 20 소비 확인
- 허기와 갈증 부족 시 수면 차단 확인
- 수면 중 중복 입력 차단 확인
- 침낭 철거 시 목재 4개 반환 확인
- Console 오류 없음 확인

### 완료 결과

낮과 밤의 시간 변화가 침낭 및 생존 능력치와 연결되었다. 플레이어는 밤을 건너뛰기 위해 체력 회복 효과와 허기·갈증 소비를 고려해야 하며, 설치형 생존 시설을 활용하는 기본 플레이 흐름을 갖추게 되었다.
