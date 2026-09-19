# Project U 데이터 점검

`Tools > Project U > Data Audit` 또는 `Build Content > 14. Data Audit Fixes`가 만든 표입니다. 데이터를 바꾸면 다시 실행하세요.

- 점검 : 데이터 152개 · Prefab 112개 · Scene 오브젝트 1641
- 결과 : 오류 0개 · 경고 2개

## 1. 문제 목록

| 구분 | 영역 | 대상 | 내용 |
| --- | --- | --- | --- |
| 경고 | 연결 | PF_Temp_Enemy_Basic | 적 전리품 표가 없어 아무것도 떨어뜨리지 않습니다. |
| 경고 | 외형 설정 | visual_weapon_axe | 이 외형 설정을 쓰는 Prefab이 없습니다. |

## 2. ID

| 종류 | 개수 | 접두사 | 문제 |
| --- | --- | --- | --- |
| 아이템 | 51 | 분류별 | 0 |
| 제작법 | 8 | recipe_ | 0 |
| 요리법 | 11 | cook_ | 0 |
| 건축물 | 20 | structure_ | 0 |
| 적 | 3 | enemy_ | 0 |
| 작물 | 5 | crop_ | 0 |
| 물고기 | 5 | fish_ | 0 |
| 가축 | 2 | animal_ | 0 |
| NPC | 35 | char_ | 0 |
| NPC 상점 | 3 | shop_ | 0 |
| 보관함 종류 | 3 | storage_ | 0 |
| 외형 설정 | 6 | visual_ | 0 |

## 3. 외형 설정 (Visual Profile)

| Profile ID | Asset | 분류 | 외형 Prefab | 쓰는 곳 |
| --- | --- | --- | --- | --- |
| visual_buildable_wood_floor | VP_Buildable_WoodFloor | Buildable | LP_build_wood_floor | WoodFloorPlaced |
| visual_enemy_basic | VP_Enemy_Basic | Enemy | LP_enemy_slime | PF_Temp_Enemy_Basic |
| visual_enemy_melee_grunt | VP_Enemy_MeleeGrunt | Enemy | LP_enemy_grunt | Enemy_MeleeGrunt |
| visual_enemy_ranged_spitter | VP_Enemy_RangedSpitter | Enemy | LP_enemy_spitter | Enemy_RangedSpitter |
| visual_item_wood | VP_Item_Wood | Item | LP_item_wood_bundle | Scene/=== Map ===, WoodPickup |
| visual_weapon_axe | VP_Weapon_Axe | Weapon | LP_tool_stone_axe | 없음 |

## 4. 아이템 외형 연결

| 아이템 ID | 이름 | 분류 | 아이콘 | 바닥 Prefab | 손 외형 | 저장 목록 | 등록 목록 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| drink_milk | MILK | Drink | O | O | 목록 | O | O |
| drink_water_bottle | WATER BOTTLE | Drink | O | O | 목록 | O | O |
| equipment_cloth_cap | CLOTH CAP | Equipment | O | O | 목록 | O | O |
| equipment_cloth_shirt | CLOTH SHIRT | Equipment | O | O | 목록 | O | O |
| equipment_small_backpack | SMALL BACKPACK | Equipment | O | O | 목록 | O | O |
| equipment_work_hat | WORK HAT | Equipment | O | O | 목록 | O | O |
| food_apple | APPLE | Food | O | O | 목록 | O | O |
| food_baked_apple | BAKED APPLE | Food | O | O | 목록 | O | O |
| food_baked_potato | BAKED POTATO | Food | O | O | 목록 | O | O |
| food_berry | BERRY | Food | O | O | 목록 | O | O |
| food_egg | EGG | Food | O | O | 목록 | O | O |
| food_fried_egg | FRIED EGGS | Food | O | O | 목록 | O | O |
| food_golden_feast | GOLDEN CARP FEAST | Food | O | O | 목록 | O | O |
| food_grilled_fish | GRILLED FISH | Food | O | O | 목록 | O | O |
| food_mushroom_skewer | MUSHROOM SKEWER | Food | O | O | 목록 | O | O |
| food_potato | POTATO | Food | O | O | 목록 | O | O |
| food_pumpkin | PUMPKIN | Food | O | O | 목록 | O | O |
| food_pumpkin_soup | PUMPKIN SOUP | Food | O | O | 목록 | O | O |
| food_strawberry | STRAWBERRY | Food | O | O | 목록 | O | O |
| food_tomato | TOMATO | Food | O | O | 목록 | O | O |
| food_tomato_stew | TOMATO STEW | Food | O | O | 목록 | O | O |
| food_veggie_omelette | VEGGIE OMELETTE | Food | O | O | 목록 | O | O |
| food_warm_milk | WARM MILK | Food | O | O | 목록 | O | O |
| food_winter_radish | WINTER RADISH | Food | O | O | 목록 | O | O |
| item_animal_feed | ANIMAL FEED | CraftingMaterial | O | O | 목록 | O | O |
| item_arrow | ARROW | CraftingMaterial | O | O | 목록 | O | O |
| item_herbal_tea | HERBAL TEA | Drink | O | O | 목록 | O | O |
| item_iron_axe | IRON AXE | Tool | O | O | 목록 | O | O |
| item_iron_ore | IRON ORE | CraftingMaterial | O | O | 목록 | O | O |
| item_plant_fiber | PLANT FIBER | CraftingMaterial | O | O | 목록 | O | O |
| item_wild_mushroom | WILD MUSHROOM | Food | O | O | 목록 | O | O |
| item_wood | WOOD | CraftingMaterial | O | O | 목록 | O | O |
| medicine_bandage | BANDAGE | Medicine | O | O | 목록 | O | O |
| resource_fish_catfish | CATFISH | CraftingMaterial | O | O | 목록 | O | O |
| resource_fish_crucian | CRUCIAN CARP | CraftingMaterial | O | O | 목록 | O | O |
| resource_fish_golden_carp | GOLDEN CARP | CraftingMaterial | O | O | 목록 | O | O |
| resource_fish_smelt | SMELT | CraftingMaterial | O | O | 목록 | O | O |
| resource_fish_trout | TROUT | CraftingMaterial | O | O | 목록 | O | O |
| resource_stone | STONE | CraftingMaterial | O | O | 목록 | O | O |
| resource_worm_bait | WORM BAIT | CraftingMaterial | O | O | 목록 | O | O |
| seed_potato | POTATO SEEDS | Seed | O | O | 목록 | O | O |
| seed_pumpkin | PUMPKIN SEEDS | Seed | O | O | 목록 | O | O |
| seed_strawberry | STRAWBERRY SEEDS | Seed | O | O | 목록 | O | O |
| seed_tomato | TOMATO SEEDS | Seed | O | O | 목록 | O | O |
| seed_winter_radish | WINTER RADISH SEEDS | Seed | O | O | 목록 | O | O |
| tool_axe | AXE | Tool | O | O | 도구 전용 | O | O |
| tool_fishing_rod_basic | FISHING ROD | Tool | O | O | 도구 전용 | O | O |
| tool_hoe | HOE | Tool | O | O | 도구 전용 | O | O |
| tool_pickaxe | PICKAXE | Tool | O | O | 도구 전용 | O | O |
| tool_watering_can | WATERING CAN | Tool | O | O | 도구 전용 | O | O |
| weapon_bow | BOW | Weapon | O | O | 활 전용 | O | O |
