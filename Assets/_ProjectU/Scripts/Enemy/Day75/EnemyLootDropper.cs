using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyLootDropper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("사망 이벤트를 제공할 EnemyHealth입니다.")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Tooltip("이 적이 사용할 드롭 테이블입니다.")]
    [SerializeField] private EnemyLootTable lootTable;

    [Tooltip("아이템이 생성될 기준 위치입니다. 비우면 현재 적 위치를 사용합니다.")]
    [SerializeField] private Transform dropOrigin;

    [Tooltip("생성된 월드 아이템을 정리할 부모입니다. 비우면 Scene에서 자동 검색합니다.")]
    [SerializeField] private WorldItemDropContainer dropContainer;

    [Header("Spawn")]
    [Tooltip("사망 위치 주변으로 아이템을 살짝 퍼뜨릴 수평 반경입니다.")]
    [SerializeField, Min(0f)] private float dropRadius = 0.3f;

    [Tooltip("아이템이 지면에 박히지 않도록 생성 위치에 더할 높이입니다.")]
    [SerializeField, Min(0f)] private float dropHeight = 0.2f;

    [Header("Physics")]
    [Tooltip("드롭 직후 옆으로 살짝 퍼지는 힘입니다.")]
    [SerializeField, Min(0f)] private float horizontalImpulse = 0.35f;

    [Tooltip("드롭 직후 위로 가볍게 들리는 힘입니다.")]
    [SerializeField, Min(0f)] private float upwardImpulse = 0.45f;

    [Tooltip("드롭 직후 적용할 약한 회전 힘입니다.")]
    [SerializeField, Min(0f)] private float torqueImpulse = 0.12f;

    [Header("Debug")]
    [Tooltip("드롭 결과를 Console에 출력합니다.")]
    [SerializeField] private bool logDropResults = true;

    [Header("Runtime")]
    [Tooltip("현재 생명 주기에서 이미 드롭을 처리했는지 표시합니다.")]
    [SerializeField] private bool hasDroppedLoot;

    private void Reset()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        dropOrigin = transform;
    }

    private void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (dropOrigin == null)
        {
            dropOrigin = transform;
        }

        if (dropContainer == null)
        {
            dropContainer = FindFirstObjectByType<WorldItemDropContainer>();
        }

        if (enemyHealth == null)
        {
            Debug.LogError("EnemyLootDropper에 EnemyHealth가 필요합니다.", this);
            enabled = false;
            return;
        }

        if (lootTable == null)
        {
            Debug.LogError("EnemyLootDropper에 EnemyLootTable을 연결해야 합니다.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died += HandleEnemyDied;
        enemyHealth.Revived += HandleEnemyRevived;
    }

    private void OnDisable()
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleEnemyDied;
        enemyHealth.Revived -= HandleEnemyRevived;
    }

    private void HandleEnemyDied(CombatHitData killingHitData)
    {
        if (hasDroppedLoot || lootTable == null)
        {
            return;
        }

        hasDroppedLoot = true;
        DropLoot();
    }

    private void HandleEnemyRevived()
    {
        hasDroppedLoot = false;
    }

    [ContextMenu("Drop Loot For Test")]
    public void DropLoot()
    {
        if (lootTable == null)
        {
            return;
        }

        if (!lootTable.TryValidate(out string errorMessage))
        {
            Debug.LogError($"Enemy Loot Table 오류: {errorMessage}", this);
            return;
        }

        Transform parentTransform = dropContainer == null
            ? null
            : dropContainer.transform;

        int createdPickupCount = 0;
        IReadOnlyList<EnemyLootTable.LootEntry> entries = lootTable.Entries;

        for (int index = 0; index < entries.Count; index++)
        {
            EnemyLootTable.LootEntry entry = entries[index];

            if (entry == null || !entry.RollDrop())
            {
                continue;
            }

            int quantity = entry.RollQuantity();
            Vector3 spawnPosition = CalculateSpawnPosition();

            Quaternion spawnRotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f);

            WorldItemPickup pickup = Instantiate(
                entry.PickupPrefab,
                spawnPosition,
                spawnRotation,
                parentTransform);

            pickup.Initialize(entry.ItemData, quantity);
            ApplyDropPhysics(pickup);
            createdPickupCount++;

            if (logDropResults)
            {
                Debug.Log(
                    $"{gameObject.name} 드롭 / {entry.ItemData.DisplayName} x{quantity}",
                    pickup);
            }
        }

        if (logDropResults)
        {
            Debug.Log(
                $"{gameObject.name} 전리품 드롭 완료 / 생성 Pickup {createdPickupCount}개",
                this);
        }
    }

    private Vector3 CalculateSpawnPosition()
    {
        Vector3 originPosition = dropOrigin == null
            ? transform.position
            : dropOrigin.position;

        Vector2 randomCircle = Random.insideUnitCircle * dropRadius;

        return originPosition
            + new Vector3(randomCircle.x, dropHeight, randomCircle.y);
    }

    private void ApplyDropPhysics(WorldItemPickup pickup)
    {
        if (pickup == null)
        {
            return;
        }

        Rigidbody itemRigidbody = pickup.GetComponent<Rigidbody>();

        if (itemRigidbody == null)
        {
            return;
        }

        itemRigidbody.linearVelocity = Vector3.zero;
        itemRigidbody.angularVelocity = Vector3.zero;

        Vector2 randomCircle = Random.insideUnitCircle;

        if (randomCircle.sqrMagnitude < 0.0001f)
        {
            randomCircle = Vector2.right;
        }

        randomCircle.Normalize();

        float horizontalScale = Random.Range(0.65f, 1f);
        float upwardScale = Random.Range(0.9f, 1.05f);

        Vector3 impulse = new Vector3(
            randomCircle.x * horizontalImpulse * horizontalScale,
            upwardImpulse * upwardScale,
            randomCircle.y * horizontalImpulse * horizontalScale);

        itemRigidbody.AddForce(impulse, ForceMode.Impulse);

        if (torqueImpulse > 0f)
        {
            Vector3 torqueDirection = new Vector3(
                Random.Range(-0.15f, 0.15f),
                Random.Range(-1f, 1f),
                Random.Range(-0.15f, 0.15f));

            if (torqueDirection.sqrMagnitude < 0.0001f)
            {
                torqueDirection = Vector3.up;
            }

            torqueDirection.Normalize();

            itemRigidbody.AddTorque(
                torqueDirection * torqueImpulse,
                ForceMode.Impulse);
        }
    }

    private void OnValidate()
    {
        dropRadius = Mathf.Max(0f, dropRadius);
        dropHeight = Mathf.Max(0f, dropHeight);
        horizontalImpulse = Mathf.Max(0f, horizontalImpulse);
        upwardImpulse = Mathf.Max(0f, upwardImpulse);
        torqueImpulse = Mathf.Max(0f, torqueImpulse);

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (dropOrigin == null)
        {
            dropOrigin = transform;
        }
    }
}
