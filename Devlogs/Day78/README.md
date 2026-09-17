# Project U 개발 일지

## 78일차 : 전체 코드 최적화 및 저폴리 모델·맵·UI 테마 적용

### 1. 개발 목표

78일차는 원래 계획의 점검 일차 대신, 지금까지 만든 전체 코드를 다시 확인하여 최적화하고
몰입도를 떨어뜨리던 **기본 도형(Cube·Capsule) 외형, 빈 지형, 기본 UI**를 개선하였다.

| 문제 | 결과 |
| --- | --- |
| 매 프레임 문자열·배열 생성, 씬 전체 검색 | 불필요한 GC와 CPU 사용 |
| 모든 오브젝트가 기본 도형 | 무엇인지 구분하기 어렵고 몰입도가 낮음 |
| 지형에 색과 배치물이 없음 | 맵이 비어 보이고 이동 목표가 없음 |
| UI가 기본 흰 패널 | 게임 분위기와 맞지 않고 정보 구분이 어려움 |

서드파티 에셋은 Git에 포함되지 않으므로, 모든 모델·텍스처·스프라이트는 **코드로 생성**하는 방식을 선택하였다.

---

## 2. 코드 최적화

동작은 유지하고 반복 할당과 검색만 줄였다.

| 대상 | 변경 |
| --- | --- |
| 체력·허기·갈증·젖음·체온 게이지 | 값이 바뀔 때만 `SetText` |
| `DayNightCycle`, `WeatherCycle` | 표시 키(분 단위)가 바뀔 때만 시간·날씨 문구 갱신 |
| `WorldMapPanelView` | 좌표가 바뀔 때만 좌표 문구 갱신 |
| `WorldItemPickup` | 활성 픽업 정적 목록(`ActivePickups`) 추가, swap-remove로 관리 |
| `NearbyLootScanner` | `FindObjectsByType` 대신 `ActivePickups` 사용, 묶음 객체 재사용, 표시 내용이 같으면 UI 갱신 생략 |
| `EnemyProjectile` | `SphereCastNonAlloc` + 공유 버퍼 |
| `BuildPlacementController` | `OverlapBoxNonAlloc`, 안내 문구 `StringBuilder` 재사용 및 변경 시에만 조립 |
| `ContentVisualDataSourceBinder` | 이름 비교 대신 참조 비교 |

```text
이전 : Update → 문자열 생성 → SetText (매 프레임)
이후 : Update → 값 비교 → 바뀐 경우만 SetText
```

---

## 3. 발견한 기존 버그 수정

| 버그 | 원인 | 수정 |
| --- | --- | --- |
| 저장·불러오기 비활성화 | `WorldItemPickupRegistry_Day75`의 픽업 Prefab 참조 4개가 비어 있어 검증 실패 | 참조 복구 |
| 월드 오브젝트 ID 누락 | 씬의 WoodPickup에 `WorldObjectId` 없음 | 맵 꾸미기 도구에서 기존 ID 발급 메뉴 자동 실행 |
| 적 외형 참조 오류 | `ContentVisualRoot`가 Profile 적용 후 삭제된 외형을 계속 참조 | 현재 활성 외형을 다시 찾도록 수정 |
| 건축 미리보기 색 일부만 변경 | 첫 번째 재질만 교체 | 모든 재질 슬롯에 초록·빨강 재질 적용, 재질 배열 캐시 |
| 피격 번쩍임 후 색 이상 | 재질 슬롯별 원래 색 미보관 | 슬롯별 색 저장 후 `SetPropertyBlock(block, slot)`으로 복원 |

추가로 맑은 날에도 옅은 안개(`clearFogDensity`)를 적용하여 원경이 자연스럽게 흐려지도록 하였다.

---

## 4. 저폴리 모델 생성 시스템

### 런타임 스크립트 (`Scripts/Art`)

| 스크립트 | 역할 |
| --- | --- |
| `LowPolyMeshBuilder` | 박스·원기둥·원뿔·구·토러스·쐐기 등 도형을 조합하여 Flat Shading 메시 생성, 색상별 Submesh |
| `StylizedPalette` | 나무껍질·잎·돌·불꽃 등 공용 색상표 |
| `StylizedModelLibrary` | 모델 72종 정의 |
| `StylizedVisualReplacement` | 원래 외형 복원용 기록 |
| `StylizedFlameFlicker` | 불꽃 크기·조명 깜빡임 |

### 모델 목록

| 분류 | 모델 |
| --- | --- |
| 자연 | 나무 4종, 바위, 덤불, 열매 덤불, 풀, 꽃, 버섯, 그루터기, 쓰러진 통나무, 갈대, 산, 절벽 |
| 아이템 | 목재, 돌, 철광석, 식물 섬유, 버섯, 사과, 열매, 붕대, 옷감, 셔츠, 모자, 배낭, 물병, 약초차 등 |
| 도구 | 돌도끼, 철도끼, 곡괭이, 활, 화살, 산성 투사체 |
| 건축 | 나무·돌 바닥, 기초, 벽, 작업대, 상자 2종, 침낭, 조명, 탁자, 의자, 모닥불 2종 |
| 캐릭터 | 플레이어, 근접 몬스터, 원거리 몬스터, 슬라임, 허수아비 |
| 소품 | 울타리, 등불 기둥, 표지판, 나무 상자, 통, 천막, 우물, 깃발, 장작더미, 벤치 |

### 에디터 도구 (`Scripts/Art/Editor`)

- `StylizedArtAssetFactory` : 색상별 URP Lit 재질, 메시, `LP_<id>` Prefab 생성
- `StylizedVisualReplacer` : 기존 기본 도형의 Renderer·Collider·참조는 유지하고 메시만 비운 뒤 `LP_Visual` 자식을 크기에 맞춰 추가
- `StylizedArtPrefabApplier` : 아이템·건축·채집·투사체·적 Prefab 53개 적용, 적·목재·바닥 Visual Profile 연결 및 Registry 등록

```text
기존 Prefab
├─ Collider / 스크립트 (유지)
├─ MeshRenderer (메시만 비움)
└─ LP_Visual (새 저폴리 모델)
```

---

## 5. 맵 꾸미기

`StylizedTerrainPainter`, `StylizedSceneDresser`가 현재 씬을 꾸민다.

| 단계 | 내용 |
| --- | --- |
| 정리 | 참조되지 않는 임시 외형 오브젝트 제거 |
| 캐릭터·도구 | 플레이어, 장착 도구, 씬 픽업, 허수아비 외형 적용 |
| 지형 | 풀·마른 풀·흙·바위 Terrain Layer 칠하기, 길·거점 주변 흙, 풀 심기 |
| 환경 | 나무, 바위·통나무, 덤불, 작은 식물, 원경 산 배치 (고정 Seed) |
| 거점 | 건축 영역 주변 울타리, 등불 기둥, 천막·우물 등 소품 |
| 하늘 | Procedural Skybox |
| 저장 ID | 월드 오브젝트·적 Spawn Point ID 발급 메뉴 실행 |
| NavMesh | 배치물을 포함하여 다시 굽기 |

풀 밀도는 지형 크기에 맞춰 Detail 해상도를 조절하여 작은 지형에서 과도하게 빽빽해지지 않도록 하였다.

---

## 6. UI 테마

| 파일 | 역할 |
| --- | --- |
| `ProjectUUIPalette` | 패널·버튼·슬롯·강조·게이지 색상 |
| `UISpriteFactory` | 둥근 패널·버튼·슬롯·알약·게이지·원형·비네트 9-slice 스프라이트, 아이콘 9종 생성 |
| `UIThemeApplier` | UI Prefab과 현재 씬 HUD에 테마 적용 |

HUD 재배치:

- 생존 게이지 패널과 아이콘
- 시간 패널과 해 아이콘
- 미니맵 테두리와 북쪽 표시
- 상호작용 안내 강조 막대
- 근처 아이템 목록, 핫바, 건축 안내
- 사망 화면 비네트와 강조 버튼

글자에는 그림자(Underlay) 재질을 적용하였고, 스크롤 목록 영역은 안쪽이 파인 어두운 영역으로 표시하였다.

---

## 7. 메뉴

```text
Tools > Project U > Art & UI
├─ Run All (Models + Map + UI)
├─ 1. Generate Low-poly Models
├─ 2. Apply Models To Game Prefabs
├─ 3. Dress Current Scene (Map)
├─ 4. Apply UI Theme To UI Prefabs
├─ 5. Apply UI Theme To Current Scene (HUD)
└─ Restore
   ├─ Restore Primitive Visuals In Prefabs
   ├─ Remove Scene Environment Props
   └─ Restore Selected Object Visual
```

모든 도구는 여러 번 실행해도 결과가 중복되지 않도록 작성하였다.

---

## 8. 검증

| 항목 | 결과 |
| --- | --- |
| 스크립트 컴파일 | 런타임·에디터 어셈블리 오류 0 |
| Prefab 적용 | 53개 성공, 0개 실패 |
| UI Prefab | 5개 적용 |
| 맵 꾸미기 | 나무 260, 바위·통나무 89, 덤불 160, 작은 식물 260, 산 16, 울타리 51 |
| 재실행 | 중복 생성 없음 |
| Play Mode | 적 저폴리 외형·Profile 적용, 피격 체력바·피해 숫자, 처치 연출·전리품 드롭 확인 |
| 저장 시스템 | `GameplaySaveController` 준비 완료 상태 확인 |

---

## 9. 정리

78일차에는 반복 할당과 검색을 줄여 코드를 최적화하고,
코드 기반 저폴리 모델·지형·환경·UI 테마를 만들어 게임 화면의 몰입도를 높였다.

또한 점검 과정에서 저장 시스템이 비활성화되어 있던 문제를 찾아 수정하였다.
