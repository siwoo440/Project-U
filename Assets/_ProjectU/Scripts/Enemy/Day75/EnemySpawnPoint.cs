using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class EnemySpawnPoint : MonoBehaviour
{
    [Header("Enemy")]
    [Tooltip("이 Spawn Point에서 생성할 적 Prefab입니다.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("생성된 적을 정리할 부모입니다. 비우면 Scene 루트에 생성합니다.")]
    [SerializeField] private Transform runtimeEnemyParent;

    [Header("Initial Spawn")]
    [Tooltip("Scene 시작 시 적을 자동 생성합니다.")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("첫 생성까지 기다릴 시간입니다.")]
    [SerializeField, Min(0f)] private float initialSpawnDelay;

    [Header("Respawn")]
    [Tooltip("적 사망 후 다시 생성할지 설정합니다.")]
    [SerializeField] private bool respawnEnabled = true;

    [Tooltip("적 사망 시점부터 다음 적이 생성되기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float respawnDelay = 10f;

    [Tooltip("사망한 적의 몸을 Scene에 남겨둘 시간입니다.")]
    [SerializeField, Min(0f)] private float corpseVisibleDuration = 2f;

    [Header("Position")]
    [Tooltip("Spawn Point 중심에서 무작위로 생성할 반경입니다.")]
    [SerializeField, Min(0f)] private float spawnRadius = 0.5f;

    [Tooltip("후보 위치에서 가장 가까운 NavMesh를 찾을 반경입니다.")]
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 3f;

    [Tooltip("무작위 NavMesh 위치 검색을 시도할 횟수입니다.")]
    [SerializeField, Range(1, 20)] private int navMeshSearchAttempts = 8;

    [Header("Debug")]
    [Tooltip("생성과 재생성 결과를 Console에 출력합니다.")]
    [SerializeField] private bool logSpawnResults = true;

    [Header("Runtime")]
    [SerializeField] private GameObject currentEnemy;
    [SerializeField] private EnemyHealth currentEnemyHealth;
    [SerializeField] private bool waitingForRespawn;

    private Coroutine spawnRoutine;

    public GameObject CurrentEnemy => currentEnemy;
    public bool HasLivingEnemy =>
        currentEnemyHealth != null
        && !currentEnemyHealth.IsDead;

    private void Start()
    {
        if (!spawnOnStart)
        {
            return;
        }

        ScheduleInitialSpawn();
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        UnsubscribeCurrentEnemy();
        waitingForRespawn = false;
    }

    [ContextMenu("Spawn Enemy Now")]
    public bool SpawnEnemyNow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enemy Spawn은 Play Mode에서 실행해야 합니다.", this);
            return false;
        }

        return SpawnEnemy();
    }

    private void ScheduleInitialSpawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(InitialSpawnRoutine());
    }

    private IEnumerator InitialSpawnRoutine()
    {
        if (initialSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(initialSpawnDelay);
        }

        spawnRoutine = null;
        SpawnEnemy();
    }

    private bool SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"{gameObject.name}의 Enemy Prefab이 비어 있습니다.", this);
            return false;
        }

        if (currentEnemy != null)
        {
            return false;
        }

        if (!TryFindSpawnPosition(out Vector3 spawnPosition))
        {
            Debug.LogError(
                $"{gameObject.name} 주변에서 적을 생성할 NavMesh 위치를 찾지 못했습니다.",
                this);
            return false;
        }

        currentEnemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            transform.rotation,
            runtimeEnemyParent);

        currentEnemyHealth = currentEnemy.GetComponent<EnemyHealth>();

        if (currentEnemyHealth == null)
        {
            Debug.LogError(
                $"{enemyPrefab.name} Prefab에 EnemyHealth가 없습니다.",
                currentEnemy);

            Destroy(currentEnemy);
            currentEnemy = null;
            return false;
        }

        currentEnemyHealth.Died += HandleEnemyDied;
        waitingForRespawn = false;

        if (logSpawnResults)
        {
            Debug.Log(
                $"{gameObject.name} 적 생성 / {currentEnemy.name} / 위치 {spawnPosition}",
                this);
        }

        return true;
    }

    private void HandleEnemyDied(CombatHitData killingHitData)
    {
        if (currentEnemyHealth != null)
        {
            currentEnemyHealth.Died -= HandleEnemyDied;
        }

        if (waitingForRespawn)
        {
            return;
        }

        waitingForRespawn = true;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(RespawnRoutine(currentEnemy));
    }

    private IEnumerator RespawnRoutine(GameObject deadEnemy)
    {
        float corpseDuration = respawnEnabled
            ? Mathf.Min(corpseVisibleDuration, respawnDelay)
            : corpseVisibleDuration;

        if (corpseDuration > 0f)
        {
            yield return new WaitForSeconds(corpseDuration);
        }

        if (deadEnemy != null)
        {
            Destroy(deadEnemy);
        }

        currentEnemy = null;
        currentEnemyHealth = null;

        if (!respawnEnabled)
        {
            waitingForRespawn = false;
            spawnRoutine = null;
            yield break;
        }

        float remainingDelay = Mathf.Max(
            0f,
            respawnDelay - corpseDuration);

        if (remainingDelay > 0f)
        {
            yield return new WaitForSeconds(remainingDelay);
        }

        waitingForRespawn = false;
        spawnRoutine = null;
        SpawnEnemy();
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < navMeshSearchAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidatePosition = transform.position
                + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(
                candidatePosition,
                out NavMeshHit navMeshHit,
                navMeshSampleRadius,
                NavMesh.AllAreas))
            {
                spawnPosition = navMeshHit.position;
                return true;
            }
        }

        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit centerHit,
            navMeshSampleRadius,
            NavMesh.AllAreas))
        {
            spawnPosition = centerHit.position;
            return true;
        }

        spawnPosition = transform.position;
        return false;
    }

    private void UnsubscribeCurrentEnemy()
    {
        if (currentEnemyHealth != null)
        {
            currentEnemyHealth.Died -= HandleEnemyDied;
        }
    }

    private void OnValidate()
    {
        initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        corpseVisibleDuration = Mathf.Max(0f, corpseVisibleDuration);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
        navMeshSearchAttempts = Mathf.Clamp(navMeshSearchAttempts, 1, 20);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward);
    }
}
