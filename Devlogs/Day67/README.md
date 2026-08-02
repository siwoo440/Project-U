# Project U 개발 일지

## 67일차 : Root와 Visual 분리 및 공통 외형 관리 구조 구현

### 개발 목표

게임 오브젝트의 기능과 외형을 분리하여, 현재는 Unity Primitive와 임시 Material로 기능을 검증하고 이후 실제 모델 에셋이 추가되면 게임 로직을 수정하지 않고 외형만 교체할 수 있는 공통 구조를 구현한다.

적, 월드 아이템, 건축물 등 서로 다른 콘텐츠가 다음 표준 계층 구조를 공통으로 사용할 수 있도록 구성한다.

```text
ObjectRoot
├─ Visual
│  └─ VisualInstance
├─ InteractionPoint
├─ EffectOrigin
└─ UIAnchor
```

---

### 구현 내용

#### 1. 공통 Visual Root 관리자 구현

`ContentVisualRoot` 컴포넌트를 추가하였다.

게임 오브젝트의 Root에는 다음 기능을 유지한다.

- 이동
- 전투
- 체력
- 상호작용
- 저장
- Collider
- Rigidbody
- NavMeshAgent
- 고유 World ID

실제 화면에 표시되는 외형은 `Visual/VisualInstance` 아래에서 관리하도록 분리하였다.

---

#### 2. 표준 자식 구조 자동 생성

`ContentVisualRoot`를 추가한 오브젝트에 다음 자식을 자동으로 생성할 수 있도록 구현하였다.

```text
Visual
└─ VisualInstance

InteractionPoint
EffectOrigin
UIAnchor
```

각 자식의 역할은 다음과 같다.

| 자식 오브젝트 | 역할 |
|---|---|
| `Visual` | 오브젝트 외형 전체 표시 여부 관리 |
| `VisualInstance` | 임시 Primitive 또는 실제 모델 Prefab 배치 |
| `InteractionPoint` | 플레이어 상호작용 기준 위치 |
| `EffectOrigin` | 공격·피격·채집·건축 효과 생성 위치 |
| `UIAnchor` | 체력바·이름·상호작용 UI 표시 위치 |

기존 `AttackOrigin`, `ProjectileOrigin` 등 기능별 기준점은 Visual 아래로 이동시키지 않고 Root 직속 자식으로 유지한다.

---

#### 3. 임시 Primitive 외형 생성

실제 모델 Prefab이 없는 상태에서도 기능을 확인할 수 있도록 Unity Primitive 기반 임시 외형 생성을 구현하였다.

지원하는 Primitive 예시:

- Cube
- Sphere
- Capsule
- Cylinder
- Plane

설정된 외형 Prefab이 없을 때 다음 항목을 기준으로 임시 외형을 생성한다.

- Primitive 종류
- 임시 Material
- 로컬 위치
- 로컬 회전
- 로컬 크기

---

#### 4. 실제 Visual Prefab 교체 기능

`Configured Visual Prefab`에 실제 또는 임시 모델 Prefab을 연결하면 기존 VisualInstance 자식을 제거하고 새로운 외형을 생성하도록 구현하였다.

외형 교체 흐름:

```text
기존 VisualInstance 외형 제거
→ 설정된 Visual Prefab 확인
→ Prefab 생성
→ 위치·회전·크기 적용
→ Root Layer 상속
→ Visual 내부 Collider 제거
```

모델 Prefab이 없는 경우에는 설정된 Primitive를 대신 생성한다.

---

#### 5. Visual Collider 자동 제거

Primitive나 외부 모델 Prefab에 포함된 Collider가 Root Collider와 중복되지 않도록, 생성된 VisualInstance 내부 Collider를 자동으로 제거하도록 구성하였다.

충돌과 피해 판정은 다음처럼 Root에서 담당한다.

```text
ObjectRoot
├─ Collider
├─ Rigidbody
├─ 기능 컴포넌트
└─ Visual
   └─ VisualInstance
      └─ Renderer와 모델
```

이를 통해 모델 교체가 물리 충돌, 아이템 획득, 적 피해 판정과 건축 판정에 영향을 주지 않도록 하였다.

---

#### 6. Root Layer 상속 구현

생성된 Visual Prefab과 모든 하위 오브젝트의 Layer를 Root 오브젝트와 동일하게 적용하도록 구현하였다.

이를 통해 외형을 교체한 이후에도 다음 기능이 기존 Layer 규칙을 유지한다.

- 카메라 표시
- Raycast
- 상호작용 검사
- 공격 대상 검사
- 건축 충돌 검사

---

#### 7. 기존 Root Renderer 제어 기능

기존 테스트 오브젝트의 MeshRenderer가 Root에 남아 있는 경우 새 Visual과 겹쳐 보일 수 있으므로 다음 기능을 추가하였다.

```text
Disable Legacy Root Renderers
Enable Legacy Root Renderers
```

Root의 Renderer만 비활성화하며 다음 기능 컴포넌트는 유지한다.

- Collider
- Rigidbody
- NavMeshAgent
- EnemyHealth
- EnemyCombatController
- 아이템 획득 컴포넌트
- 건축 컴포넌트
- WorldObjectIdentity

---

#### 8. Visual 표시 상태 제어

`Visual` 자식의 활성 상태를 변경하여 외형만 표시하거나 숨길 수 있도록 구현하였다.

Visual이 비활성화되어도 다음 기능은 계속 작동한다.

- 적 이동
- 적 공격
- 피해 판정
- 아이템 상호작용
- 건축물 충돌
- 저장과 불러오기

이를 통해 게임 로직과 외형이 정상적으로 분리되었는지 확인할 수 있다.

---

#### 9. Visual 구조 검증 기능

다음 항목을 검사하는 Visual 구조 검증 기능을 추가하였다.

- `Visual` 존재 여부
- `VisualInstance` 존재 여부
- `InteractionPoint` 존재 여부
- `EffectOrigin` 존재 여부
- `UIAnchor` 존재 여부
- Visual 참조가 Root 자신을 가리키는지 여부
- VisualInstance 내부 Collider 존재 여부

검증 결과는 Console에서 확인할 수 있다.

---

#### 10. Editor 작업 도구 구현

`ContentVisualRootEditor`를 추가하였다.

Unity 상단 메뉴에 다음 기능을 추가하였다.

```text
Project U
└─ Visual
   ├─ Add Standard Visual Root To Selection
   ├─ Rebuild Selected Visuals
   └─ Validate Selected Visual Roots
```

Inspector에는 다음 버튼을 추가하였다.

```text
Ensure Standard Structure
Rebuild Configured Visual
Apply Current Visual Transform
Disable Legacy Root Renderers
Enable Legacy Root Renderers
Validate Visual Structure
```

여러 Root 오브젝트를 동시에 선택하여 표준 Visual 구조를 일괄 적용할 수 있다.

---

### 추가된 스크립트

```text
Assets/_ProjectU/Scripts/Visual/ContentVisualRoot.cs

Assets/_ProjectU/Scripts/Visual/Editor/
ContentVisualRootEditor.cs
```

기존 전투, 아이템, 건축과 저장 관련 스크립트는 수정하지 않았다.

---

### 생성한 임시 Material

```text
Assets/_ProjectU/Materials/Placeholder/
├─ M_Temp_Enemy
├─ M_Temp_Item
└─ M_Temp_Buildable
```

임시 Material은 실제 에셋이 추가될 때까지 콘텐츠 종류를 구분하기 위한 용도로 사용한다.

---

### 적용 구조

#### 적

```text
Enemy_Basic_Test
├─ Visual
│  └─ VisualInstance
│     └─ TEMP_Capsule_Visual
├─ InteractionPoint
├─ EffectOrigin
├─ UIAnchor
├─ AttackOrigin
└─ AttackTelegraph
```

Root에 유지한 주요 컴포넌트:

- CapsuleCollider
- NavMeshAgent
- EnemyHealth
- EnemyCombatController
- EnemyNavMeshMovement
- EnemyCombatImpactMotor
- EnemyAttackTelegraph

#### 월드 아이템

```text
WorldItemRoot
├─ Visual
│  └─ VisualInstance
│     └─ TEMP_Cube_Visual
├─ InteractionPoint
├─ EffectOrigin
└─ UIAnchor
```

Root에 유지한 주요 컴포넌트:

- Collider
- Rigidbody
- 아이템 획득 컴포넌트
- WorldObjectIdentity

#### 건축물

```text
BuildableRoot
├─ Visual
│  └─ VisualInstance
│     └─ TEMP_Cube_Visual
├─ InteractionPoint
├─ EffectOrigin
└─ UIAnchor
```

Root에 유지한 주요 컴포넌트:

- Collider
- 건축물 기능 컴포넌트
- 상호작용 컴포넌트
- WorldObjectIdentity

---

### 테스트 내용

#### 적 테스트

- 임시 Capsule 외형 생성 확인
- 기존 Root Renderer 비활성화 확인
- VisualInstance Collider 제거 확인
- NavMesh 추적 이동 확인
- 적 공격 준비와 공격 판정 확인
- 플레이어 피격과 밀림 확인
- 적 피격과 밀림 확인
- 적 사망 처리 확인
- Visual 비활성 상태에서도 전투 로직 작동 확인

#### 아이템 테스트

- 임시 Cube 외형 생성 확인
- Root Collider와 Rigidbody 유지 확인
- F 상호작용 확인
- 인벤토리 획득 확인
- 월드 아이템 제거 확인
- 저장 후 월드 아이템 복원 확인
- 획득한 아이템이 불러오기 후 재생성되지 않는지 확인

#### 건축물 테스트

- 임시 Cube 외형 생성 확인
- Preview 표시 확인
- 배치 가능·불가 판정 확인
- 회전 확인
- 재료 소모 확인
- 실제 설치 확인
- 철거 확인
- 저장 후 설치 상태 복원 확인

#### Visual 교체 테스트

- 임시 Primitive 생성 확인
- 별도 Visual Prefab 연결 확인
- Visual Prefab 교체 후 기능 유지 확인
- Configured Visual Prefab을 제거했을 때 Primitive로 복귀 확인
- 외형 변경 후 Root Collider와 기능 유지 확인

---

### 완료 결과

적, 월드 아이템과 건축물의 기능과 외형을 분리할 수 있는 공통 구조를 구현하였다.

현재는 Unity Primitive와 임시 Material로 기능을 확인할 수 있으며, 이후 실제 3D 모델 에셋이 추가되면 다음 작업만으로 교체할 수 있다.

```text
실제 모델 Prefab 준비
→ Configured Visual Prefab에 연결
→ Rebuild Configured Visual 실행
→ 위치·회전·크기 보정
→ Visual 구조 검증
```

전투, 상호작용, 저장과 건축 로직은 Root에 유지되므로 실제 모델을 교체해도 기존 게임 기능을 다시 구현할 필요가 없다.

---

### 다음 개발 방향

68일차에는 아이템, 무기, 적과 건축물에 연결할 공통 Visual Profile 데이터를 구현한다.

다음 정보를 ScriptableObject로 관리할 예정이다.

- Visual Prefab
- Inventory Icon
- Material
- Animator Controller
- Audio Profile
- VFX Profile
- 기본 임시 색상
- 외형 위치·회전·크기 보정값

이를 통해 Scene이나 Prefab에서 외형을 직접 설정하는 대신 데이터에 연결된 Visual 정보를 자동으로 적용할 수 있도록 확장한다.
