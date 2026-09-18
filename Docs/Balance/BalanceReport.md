# Project U 밸런스 보고서

`Tools > Project U > Balance Report` 또는 `Build Content > 13. Balance Pass`가 만든 표입니다. 데이터를 바꾸면 다시 실행하세요.

- 하루 600초 (게임 1시간 = 25초), 한 계절 28일
- 결과 : 오류 0개

## 1. 활동별 수입

| 활동 | 수입 | 기준 |
| --- | --- | --- |
| 농사 | 한 칸 하루 5.4코인 | 작물마다 평균의 70~150% |
| 낚시 | 1분 94.7코인 | 비교 기준 |
| 적 사냥 | 1분 6.5코인 | 낚시의 50% 이하 |
| 채집 (STONE) | 한 곳 1분 23.1코인 | 낚시 이하 |
| 채집 (WOOD) | 한 곳 1분 23.1코인 | 낚시 이하 |

## 2. 농사 (씨앗 1개)

| 작물 | 계절 | 자라는 날 | 씨앗 | 평균 수확 | 한 개 값 | 한 번 이익 | 한 칸 하루 | 한 계절 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| POTATO | 봄 | 3일 | 6 | 2 | 10 | 15.8 | 5.3 | 142.2 |
| PUMPKIN | 가을 | 7일 | 14 | 1.5 | 35 | 42.7 | 6.1 | 170.8 |
| STRAWBERRY | 봄 | 5일 | 8 | 3.5 | 9 | 25.9 | 5.2 | 129.5 |
| TOMATO | 여름 | 6일 | 8 | 3 | 11 | 27.4 | 4.6 | 109.6 |
| WINTER RADISH | 겨울 | 4일 | 9 | 2 | 15 | 23.7 | 5.9 | 165.9 |

## 3. 낚시 (맑은 날 연못)

| 계절 | 나오는 종류 | 한 마리 평균 값 | 한 마리 시간 | 1분 수입 |
| --- | --- | --- | --- | --- |
| 봄 | 2 | 19.1 | 11.6초 | 98.6 |
| 여름 | 2 | 17.4 | 11.5초 | 90.3 |
| 가을 | 3 | 20.6 | 11.7초 | 105.6 |
| 겨울 | 1 | 16 | 11.4초 | 84.2 |

## 4. 적 (도끼 기준)

| 생성 지점 | 적 | 전리품 표 | 쓰러뜨리는 타수 | 걸리는 시간 | 플레이어가 버티는 타수 | 전리품 기대 값 | 다시 나옴 | 1분 수입 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Spawn_MeleeGrunt_01 | Enemy_MeleeGrunt | EnemyLootTable_MeleeGrunt | 6 | 3.3초 | 9 | 7.6 | 180초 (게임 7.2시간) | 2.4 |
| Spawn_MeleeGrunt_02 | Enemy_MeleeGrunt | EnemyLootTable_MeleeGrunt | 6 | 3.3초 | 9 | 7.6 | 180초 (게임 7.2시간) | 2.4 |
| Spawn_RangedSpitter_01 | Enemy_RangedSpitter | EnemyLootTable_RangedSpitter | 4 | 2.2초 | 13 | 6.9 | 240초 (게임 9.6시간) | 1.7 |

## 5. 가축 · 요리

| 가축 | 하루 먹이 값 | 하루 생산물 값 | 하루 이익 | 데려오기 | 회수 |
| --- | --- | --- | --- | --- | --- |
| CHICKEN | 3 | 13.5 | 10.5 | 12 | 1.1일 |
| COW | 6 | 20 | 14 | 24 | 1.7일 |

| 요리 | 재료 판매가 | 요리 판매가 | 늘어난 값 |
| --- | --- | --- | --- |
| BAKED APPLE | 4 | 10 | 6 |
| BAKED POTATO | 10 | 16 | 6 |
| MUSHROOM SKEWER | 12 | 22 | 10 |
| GRILLED CRUCIAN | 15 | 26 | 11 |
| GRILLED TROUT | 34 | 52 | 18 |
| PUMPKIN SOUP | 38 | 54 | 16 |
| TOMATO STEW | 35 | 52 | 17 |
| GOLDEN CARP FEAST | 122 | 180 | 58 |
| FRIED EGGS | 20 | 34 | 14 |
| VEGGIE OMELETTE | 37 | 56 | 19 |
| WARM MILK | 16 | 28 | 12 |

## 6. 의뢰

| 의뢰 | NPC | 계절 | 필요 물건 판매가 | 보상 값 | 배율 |
| --- | --- | --- | --- | --- | --- |
| quest_dravia_ore | char_dravia | 모든 계절 | 35 | 70 | 2.00 |
| quest_dravia_stone | char_dravia | 모든 계절 | 20 | 30 | 1.50 |
| quest_dravia_wood | char_dravia | 모든 계절 | 20 | 30 | 1.50 |
| quest_dravia_special (특별) | char_dravia | 모든 계절 | 122 | 290 | 2.38 |
| quest_lichel_catfish | char_lichel | 여름 | 35 | 50 | 1.43 |
| quest_lichel_skewer | char_lichel | 모든 계절 | 44 | 60 | 1.36 |
| quest_lichel_wood | char_lichel | 모든 계절 | 15 | 25 | 1.67 |
| quest_lichel_special (특별) | char_lichel | 여름 | 166 | 570 | 3.43 |
| quest_lunette_mushroom | char_lunette | 모든 계절 | 24 | 40 | 1.67 |
| quest_lunette_berry | char_lunette | 모든 계절 | 12 | 25 | 2.08 |
| quest_lunette_apple | char_lunette | 모든 계절 | 16 | 25 | 1.56 |
| quest_lunette_special (특별) | char_lunette | 모든 계절 | 54 | 144 | 2.67 |
| quest_milky_milk | char_milky | 모든 계절 | 48 | 60 | 1.25 |
| quest_milky_strawberry | char_milky | 봄 | 54 | 60 | 1.11 |
| quest_milky_pumpkin | char_milky | 가을 | 70 | 90 | 1.29 |
| quest_milky_special (특별) | char_milky | 모든 계절 | 128 | 270 | 2.11 |
| quest_mio_crucian | char_mio | 봄 | 45 | 60 | 1.33 |
| quest_mio_smelt | char_mio | 겨울 | 48 | 65 | 1.35 |
| quest_mio_trout | char_mio | 봄 | 68 | 90 | 1.32 |
| quest_mio_special (특별) | char_mio | 모든 계절 | 96 | 170 | 1.77 |
| quest_mireille_apple | char_mireille | 모든 계절 | 20 | 30 | 1.50 |
| quest_mireille_egg | char_mireille | 모든 계절 | 30 | 40 | 1.33 |
| quest_mireille_fiber | char_mireille | 모든 계절 | 10 | 25 | 2.50 |
| quest_mireille_special (특별) | char_mireille | 모든 계절 | 66 | 110 | 1.67 |
| quest_verona_potato | char_verona | 봄 | 50 | 65 | 1.30 |
| quest_verona_tomato | char_verona | 여름 | 55 | 70 | 1.27 |
| quest_verona_pumpkin | char_verona | 가을 | 70 | 90 | 1.29 |
| quest_verona_radish | char_verona | 겨울 | 60 | 75 | 1.25 |
| quest_verona_special (특별) | char_verona | 모든 계절 | 64 | 174 | 2.72 |

## 7. 호감도 (매일 말 걸기 기준, 알파 NPC 7명)

대화 +1 · 매우 좋아함 +8 · 좋아함 +4 · 선물 하루 1번 · 한 주 2번 · 단계 20/40/60

| 방법 | 호기심 | 신뢰 | 애정 |
| --- | --- | --- | --- |
| 대화만 (이벤트 포함) | 20일 | 35일 | 47일 |
| 대화 + 좋아하는 선물 | 8일 | 15일 | 22일 |
| 대화 + 매우 좋아하는 선물 | 4일 | 9일 | 15일 |

## 8. 생존 · 큰 물건

- 하루 허기 19 · 갈증 24 줄어듦 (감자 1.3개, 물병 0.7개 = 6코인)

| 물건 | 파는 곳 | 가격 | 낚시로 | 밭 10칸으로 |
| --- | --- | --- | --- | --- |
| BOW | 대장간 | 150 | 1.6분 | 2.8일 |
| IRON AXE | 대장간 | 240 | 2.5분 | 4.4일 |
| GOLDEN CARP FEAST | 선셋 카페 | 320 | 3.4분 | 5.9일 |
| SMALL BACKPACK | 떠돌이 상인 | 450 | 4.8분 | 8.3일 |

## 9. 검사 결과

- 참고 : 의뢰 quest_milky_strawberry : 보상이 판매가의 1.11배로 조금 낮습니다. (상점에서 사서 전달하는 값보다 높일 수 없는 경우)
