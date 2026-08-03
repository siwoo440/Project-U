# Project U 개발 일지

## 71일차 : 기본 재료·도구·음식 아이템 콘텐츠 확장

### 개발 목표

66~70일차에 구축한 아이템 데이터와 저장 기반을 실제 콘텐츠 제작에 활용하였다.

새로운 관리 시스템이나 스크립트를 추가하지 않고 기존 `ItemData`, `WorldItemPickup`, 인벤토리, 핫바 사용, 채집, 저장 시스템을 재사용하여 실제로 획득하고 사용할 수 있는 아이템을 확장하였다.

이번 작업의 목표는 다음과 같다.

- 제작 재료 2종 추가
- 음식 1종 추가
- 음료 1종 추가
- 도구 1종 추가
- 신규 월드 아이템 Prefab 제작
- 인벤토리 획득·중첩·사용 확인
- 도구 채집 기능 연결
- 신규 아이템 저장·복원 확인
- ItemDatabase와 GameDataRegistry 등록

---

### 추가한 아이템

| 종류 | 아이템 | 콘텐츠 ID | 기능 |
|---|---|---|---|
| 제작 재료 | Plant Fiber | `item_plant_fiber` | 기본 생활용품 제작 재료 |
| 제작 재료 | Iron Ore | `item_iron_ore` | 금속 도구·장비 제작 재료 |
| 음식 | Wild Mushroom | `item_wild_mushroom` | 허기 15 회복 |
| 음료 | Herbal Tea | `item_herbal_tea` | 갈증 25 회복 |
| 도구 | Iron Axe | `item_iron_axe` | 나무 채집과 근접 공격 |

모든 신규 아이템은 현재 프로젝트 표준인 `item_` 접두사를 사용하는 고유 ID로 작성하였다.

---

### ItemData 제작

생성 경로:

```text
Assets/_ProjectU/Data/Items/Day71/
├─ ItemData_PlantFiber.asset
├─ ItemData_IronOre.asset
├─ ItemData_WildMushroom.asset
├─ ItemData_HerbalTea.asset
└─ ItemData_IronAxe.asset
```

#### Plant Fiber

```text
Item Id: item_plant_fiber
Display Name: PLANT FIBER
Description: A flexible fiber used for basic crafting.
Item Category: Crafting Material
Maximum Stack: 50
```

#### Iron Ore

```text
Item Id: item_iron_ore
Display Name: IRON ORE
Description: Raw ore used for advanced tools and equipment.
Item Category: Crafting Material
Maximum Stack: 20
```

#### Wild Mushroom

```text
Item Id: item_wild_mushroom
Display Name: WILD MUSHROOM
Description: A common edible mushroom found in the wild.
Item Category: Food
Hunger Restore Amount: 15
Maximum Stack: 10
```

#### Herbal Tea

```text
Item Id: item_herbal_tea
Display Name: HERBAL TEA
Description: A warm herbal drink that restores thirst.
Item Category: Drink
Thirst Restore Amount: 25
Maximum Stack: 5
```

#### Iron Axe

기존 `ItemData_Axe`를 복제하여 제작하였다.

```text
Item Id: item_iron_axe
Display Name: IRON AXE
Description: A sturdy axe used for gathering wood and close combat.

Item Category: Tool
Tool Type: Axe
Weapon Attack Type: Melee

Base Damage: 18
Attack Cooldown: 0.5
Attack Range: 2.1
Attack Radius: 0.5
Stamina Cost: 9
Impact Force: 4
Maximum Stack: 1
```

기존 Axe의 `MeleeComboData`를 재사용하여 새로운 공격 스크립트 없이 근접 공격과 나무 채집 기능에 연결하였다.

---

### 임시 Material 제작

신규 아이템을 월드에서 구분할 수 있도록 임시 Material을 제작하였다.

```text
Assets/_ProjectU/Art/Materials/Items/Day71/
├─ MAT_Item_PlantFiber.mat
├─ MAT_Item_IronOre.mat
├─ MAT_Item_WildMushroom.mat
├─ MAT_Item_HerbalTea.mat
└─ MAT_Item_IronAxe.mat
```

현재는 Primitive와 색상을 이용한 임시 외형을 사용한다. 실제 모델과 텍스처는 후반 외형 정리 단계에서 교체한다.

---

### Pickup Prefab 제작

기존 `ApplePickup.prefab`을 템플릿으로 복제하여 신규 월드 아이템 Prefab을 제작하였다.

```text
Assets/_ProjectU/Prefabs/Items/Day71/
├─ PlantFiberPickup.prefab
├─ IronOrePickup.prefab
├─ WildMushroomPickup.prefab
├─ HerbalTeaPickup.prefab
└─ IronAxePickup.prefab
```

공통 구성:

```text
Pickup Root
├─ MeshFilter
├─ MeshRenderer
├─ Rigidbody
├─ Collider
├─ WorldItemPickup
└─ WorldObjectIdentity
```

연결한 데이터와 초기 수량:

| Prefab | ItemData | 수량 |
|---|---|---:|
| PlantFiberPickup | ItemData_PlantFiber | 5 |
| IronOrePickup | ItemData_IronOre | 3 |
| WildMushroomPickup | ItemData_WildMushroom | 2 |
| HerbalTeaPickup | ItemData_HerbalTea | 1 |
| IronAxePickup | ItemData_IronAxe | 1 |

---

### Gameplay Scene 배치

다음 Scene의 `WorldItems` 부모 아래에 신규 아이템을 배치하였다.

```text
Assets/_ProjectU/Scenes/20_Gameplay.unity
```

Hierarchy:

```text
WorldItems
├─ PlantFiberPickup
├─ IronOrePickup
├─ WildMushroomPickup
├─ HerbalTeaPickup
└─ IronAxePickup
```

각 Scene 인스턴스의 `WorldObjectIdentity`에서 서로 다른 영구 ID를 발급하였다.

원본 Prefab의 ID는 비워 두고 Scene에 배치된 실제 인스턴스에만 고유 ID를 저장하였다.

---

### 데이터베이스 등록

신규 아이템을 저장 데이터에서 복원할 수 있도록 다음 Asset에 등록하였다.

```text
Assets/_ProjectU/Data/Databases/ItemDatabase.asset
```

추가 항목:

```text
ItemData_PlantFiber
ItemData_IronOre
ItemData_WildMushroom
ItemData_HerbalTea
ItemData_IronAxe
```

기존 항목은 유지하고 배열 뒤에 신규 데이터만 추가하였다.

---

### GameDataRegistry 갱신

Unity Editor 메뉴를 이용해 전체 콘텐츠 데이터를 다시 수집하였다.

```text
Project U
→ Data
→ Create Or Refresh Game Data Registry
```

검증:

```text
Project U
→ Data
→ Validate Default Game Data Registry
```

확인 결과:

```text
신규 ItemData 5개 등록
중복 ID 0개
서로 다른 데이터 종류 간 중복 ID 0개
잘못된 ID 0개
빈 ItemData 참조 0개
```

---

### 스크립트 변경 사항

71일차에는 새로운 `.cs` 파일을 추가하거나 기존 스크립트를 수정하지 않았다.

기존 시스템을 그대로 활용하였다.

```text
ItemData.cs
WorldItemPickup.cs
HotbarItemUse.cs
PlayerInventory.cs
WorldObjectIdentity.cs
InventorySaveBridge.cs
WorldSaveBridge.cs
GameDataRegistry.cs
```

이번 작업을 통해 현재 콘텐츠 구조가 실제 아이템 확장에 재사용 가능한지 확인하였다.

---

### 기능 테스트

#### 월드 획득

```text
Plant Fiber 5개
Iron Ore 3개
Wild Mushroom 2개
Herbal Tea 1개
Iron Axe 1개
```

확인 결과:

- F 상호작용 안내 정상
- 신규 아이템 획득 정상
- 인벤토리 이름과 수량 정상
- 제작 재료 중첩 정상
- Iron Axe 단일 중첩 정상
- 전체 획득 후 월드 오브젝트 비활성화 정상

#### 음식 사용

Wild Mushroom을 핫바에 배치하고 마우스 오른쪽 버튼으로 사용하였다.

```text
허기 회복량: 15
소비 수량: 1
```

- 허기가 최대일 때 사용 차단
- 사용 차단 시 수량 유지
- 허기가 부족할 때 정상 사용
- 허기 회복과 수량 감소 정상

#### 음료 사용

Herbal Tea를 핫바에 배치하고 마우스 오른쪽 버튼으로 사용하였다.

```text
갈증 회복량: 25
소비 수량: 1
```

- 갈증이 최대일 때 사용 차단
- 사용 차단 시 수량 유지
- 갈증이 부족할 때 정상 사용
- 갈증 회복과 수량 감소 정상

#### Iron Axe 사용

- 도끼 도구로 정상 인식
- 기존 근접 공격 동작 재사용
- 공격 스태미나 소비 정상
- 나무 채집 피해 적용
- 목재 획득 정상
- 바위 채집에는 사용되지 않음
- 최대 중첩 1개 유지

---

### 인벤토리와 저장 테스트

확인 항목:

- Plant Fiber 최대 50개 중첩
- Iron Ore 최대 20개 중첩
- Wild Mushroom 최대 10개 중첩
- Herbal Tea 최대 5개 중첩
- Iron Axe 최대 1개 보관
- 인벤토리 슬롯 이동 정상
- 핫바 등록 정상
- 아이템 버리기와 재획득 정상
- 인벤토리가 가득 찼을 때 잔여 월드 수량 유지

저장 후 `Continue`로 다시 불러와 다음 항목을 확인하였다.

- 신규 아이템 종류와 수량 유지
- Iron Axe 핫바 위치 유지
- 사용 후 남은 음식 수량 유지
- 월드에 버린 아이템 위치와 수량 복원
- 이미 획득한 Scene 아이템 재등장 방지
- 신규 ItemData의 ID 기반 복원 정상

---

### 기존 기능 회귀 테스트

- WoodPickup 획득 정상
- Apple과 Berry 사용 정상
- 기존 음료 사용 정상
- Bandage 체력 회복 정상
- 기존 Axe와 Pickaxe 채집 정상
- 장비 착용 정상
- 인벤토리 드래그 정상
- 보관함 이동 정상
- 제작 UI 정상
- 건축 배치 정상
- 수면 정상
- 저장과 이어하기 정상
- Console 오류 없음

---

### 추가 및 수정된 주요 파일

#### 신규 파일

```text
Assets/_ProjectU/Data/Items/Day71/
Assets/_ProjectU/Art/Materials/Items/Day71/
Assets/_ProjectU/Prefabs/Items/Day71/
```

#### 수정 파일

```text
Assets/_ProjectU/Data/Databases/ItemDatabase.asset
Assets/_ProjectU/Data/Registry/GameDataRegistry.asset
Assets/_ProjectU/Scenes/20_Gameplay.unity
```

---

### 완료 결과

71일차 작업을 통해 내부 데이터 관리 기반을 실제 콘텐츠 제작에 활용하였다.

```text
새 ItemData 생성
→ Pickup Prefab 제작
→ Gameplay Scene 배치
→ F 입력으로 획득
→ 인벤토리와 핫바 사용
→ 도구 채집
→ 저장과 불러오기
```

앞으로 새로운 아이템을 만들 때마다 별도의 스크립트를 제작하지 않고 기존 데이터와 Prefab을 복제하여 확장할 수 있게 되었다.

---

### 다음 개발 방향

72일차에는 71일차에 추가한 재료와 도구를 제작 시스템에 연결한다.

주요 작업:

1. Plant Fiber를 사용하는 생활용품 제작법 추가
2. Iron Ore를 사용하는 Iron Axe 제작법 추가
3. 작업대와 제작 UI에 신규 제작법 등록
4. 재료 부족과 제작 성공 처리 확인
5. 결과 아이템 지급과 저장 확인
6. 기존 제작법 회귀 테스트

예상 흐름:

```text
재료 획득
→ 제작 시설 상호작용
→ 제작법 선택
→ 필요 재료 확인
→ 재료 소비
→ 결과 아이템 획득
→ 저장과 불러오기
```
