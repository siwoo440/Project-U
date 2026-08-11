using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldSaveBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("아이템 ID 데이터베이스입니다.")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Tooltip("전용 Pickup Prefab을 찾지 못했을 때 사용할 기존 공통 월드 아이템 Prefab입니다.")]
    [SerializeField] private WorldItemPickup worldItemPrefab;

    [Tooltip("ItemData별 실제 외형 Pickup Prefab을 제공할 Registry입니다.")]
    [SerializeField] private WorldItemPickupRegistry pickupRegistry;

    [Tooltip("복원 월드 아이템 부모입니다.")]
    [SerializeField] private Transform worldItemParent;

    public bool TryValidateSetup(out string errorMessage)
    {
        if (itemDatabase == null)
        {
            errorMessage = "ItemDatabase 참조가 누락되었습니다.";
            return false;
        }

        if (worldItemPrefab == null)
        {
            errorMessage = "World Item Prefab 참조가 누락되었습니다.";
            return false;
        }

        if (worldItemParent == null)
        {
            errorMessage = "World Item Parent 참조가 누락되었습니다.";
            return false;
        }

        if (!itemDatabase.TryValidate(out errorMessage))
        {
            return false;
        }

        if (pickupRegistry != null
            && !pickupRegistry.TryValidate(out errorMessage))
        {
            return false;
        }

        return TryBuildCurrentObjectMaps(
            out Dictionary<string, WorldItemPickup> worldItems,
            out Dictionary<string, GatherableResource> resources,
            out errorMessage);
    }

    public bool TryCapture(
        SaveGameData saveData,
        out string errorMessage)
    {
        if (saveData == null || saveData.world == null)
        {
            errorMessage = "월드 저장 데이터가 누락되었습니다.";
            return false;
        }

        if (!TryBuildCurrentObjectMaps(
            out Dictionary<string, WorldItemPickup> worldItems,
            out Dictionary<string, GatherableResource> resources,
            out errorMessage))
        {
            return false;
        }

        saveData.world.worldItems.Clear();
        saveData.world.gatherableResources.Clear();

        foreach (WorldItemPickup worldItem in worldItems.Values)
        {
            if (!worldItem.IsAvailable)
            {
                continue;
            }

            if (worldItem.ItemData == null)
            {
                errorMessage =
                    $"{worldItem.gameObject.name}의 Item Data가 누락되었습니다.";
                return false;
            }

            if (!itemDatabase.TryGetItem(
                worldItem.ItemData.ItemId,
                out ItemData registeredItem))
            {
                errorMessage =
                    $"ItemDatabase에 등록되지 않은 월드 아이템입니다: "
                    + $"{worldItem.ItemData.ItemId}";
                return false;
            }

            WorldItemSaveData itemSaveData = new WorldItemSaveData();
            itemSaveData.worldObjectId = worldItem.WorldObjectId;
            itemSaveData.itemId = registeredItem.ItemId;
            itemSaveData.quantity = worldItem.Quantity;
            itemSaveData.position =
                SaveVector3Data.FromVector3(worldItem.transform.position);
            itemSaveData.rotation =
                SaveQuaternionData.FromQuaternion(worldItem.transform.rotation);

            saveData.world.worldItems.Add(itemSaveData);
        }

        foreach (GatherableResource resource in resources.Values)
        {
            GatherableResourceSaveData resourceSaveData =
                new GatherableResourceSaveData();

            resourceSaveData.worldObjectId = resource.WorldObjectId;
            resourceSaveData.remainingQuantity = resource.RemainingQuantity;
            resourceSaveData.isDepleted = resource.IsDepleted;
            resourceSaveData.respawnRemainingSeconds =
                resource.RespawnRemainingSeconds;

            saveData.world.gatherableResources.Add(resourceSaveData);
        }

        saveData.world.hasCapturedWorldState = true;
        errorMessage = string.Empty;
        return true;
    }

    public bool TryRestore(
        SaveGameData saveData,
        out string errorMessage)
    {
        if (saveData == null || saveData.world == null)
        {
            errorMessage = "월드 저장 데이터가 누락되었습니다.";
            return false;
        }

        if (!saveData.world.hasCapturedWorldState)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (!TryBuildCurrentObjectMaps(
            out Dictionary<string, WorldItemPickup> currentWorldItems,
            out Dictionary<string, GatherableResource> currentResources,
            out errorMessage))
        {
            return false;
        }

        if (!TryValidateSavedState(
            saveData,
            currentResources,
            out errorMessage))
        {
            return false;
        }

        foreach (WorldItemPickup currentWorldItem in currentWorldItems.Values)
        {
            currentWorldItem.SetAvailableForLoad(false);
        }

        foreach (GatherableResource currentResource in currentResources.Values)
        {
            currentResource.ResetForLoad();
        }

        for (int index = 0;
            index < saveData.world.worldItems.Count;
            index++)
        {
            WorldItemSaveData itemSaveData =
                saveData.world.worldItems[index];

            itemDatabase.TryGetItem(
                itemSaveData.itemId,
                out ItemData itemData);

            if (!currentWorldItems.TryGetValue(
                itemSaveData.worldObjectId,
                out WorldItemPickup worldItem))
            {
                Vector3 spawnPosition =
                    itemSaveData.position.ToVector3();

                Quaternion spawnRotation =
                    itemSaveData.rotation.ToQuaternion();

                WorldItemPickup restorePrefab =
                    ResolveWorldItemPrefab(itemData);

                worldItem = Instantiate(
                    restorePrefab,
                    spawnPosition,
                    spawnRotation,
                    worldItemParent);
            }

            worldItem.RestoreFromSave(
                itemData,
                itemSaveData.quantity,
                itemSaveData.worldObjectId,
                itemSaveData.position.ToVector3(),
                itemSaveData.rotation.ToQuaternion());
        }

        for (int index = 0;
            index < saveData.world.gatherableResources.Count;
            index++)
        {
            GatherableResourceSaveData resourceSaveData =
                saveData.world.gatherableResources[index];

            GatherableResource resource =
                currentResources[resourceSaveData.worldObjectId];

            resource.RestoreFromSave(
                resourceSaveData.remainingQuantity,
                resourceSaveData.isDepleted,
                resourceSaveData.respawnRemainingSeconds);
        }

        errorMessage = string.Empty;
        return true;
    }

    private WorldItemPickup ResolveWorldItemPrefab(ItemData itemData)
    {
        if (pickupRegistry != null
            && pickupRegistry.TryGetPickup(
                itemData,
                out WorldItemPickup registeredPrefab)
            && registeredPrefab != null)
        {
            return registeredPrefab;
        }

        return worldItemPrefab;
    }

    private bool TryBuildCurrentObjectMaps(
        out Dictionary<string, WorldItemPickup> worldItems,
        out Dictionary<string, GatherableResource> resources,
        out string errorMessage)
    {
        worldItems =
            new Dictionary<string, WorldItemPickup>(StringComparer.Ordinal);

        resources =
            new Dictionary<string, GatherableResource>(StringComparer.Ordinal);

        HashSet<string> usedWorldObjectIds =
            new HashSet<string>(StringComparer.Ordinal);

        WorldItemPickup[] foundWorldItems =
            UnityEngine.Object.FindObjectsByType<WorldItemPickup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int index = 0; index < foundWorldItems.Length; index++)
        {
            WorldItemPickup worldItem = foundWorldItems[index];
            string worldObjectId = worldItem.WorldObjectId;

            if (string.IsNullOrWhiteSpace(worldObjectId))
            {
                errorMessage =
                    $"{worldItem.gameObject.name}의 World Object ID가 비어 있습니다.";
                return false;
            }

            if (!usedWorldObjectIds.Add(worldObjectId))
            {
                errorMessage =
                    $"중복 World Object ID가 있습니다: {worldObjectId}";
                return false;
            }

            worldItems.Add(worldObjectId, worldItem);
        }

        GatherableResource[] foundResources =
            UnityEngine.Object.FindObjectsByType<GatherableResource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int index = 0; index < foundResources.Length; index++)
        {
            GatherableResource resource = foundResources[index];
            string worldObjectId = resource.WorldObjectId;

            if (string.IsNullOrWhiteSpace(worldObjectId))
            {
                errorMessage =
                    $"{resource.gameObject.name}의 World Object ID가 비어 있습니다.";
                return false;
            }

            if (!usedWorldObjectIds.Add(worldObjectId))
            {
                errorMessage =
                    $"중복 World Object ID가 있습니다: {worldObjectId}";
                return false;
            }

            resources.Add(worldObjectId, resource);
        }

        errorMessage = string.Empty;
        return true;
    }

    private bool TryValidateSavedState(
        SaveGameData saveData,
        Dictionary<string, GatherableResource> currentResources,
        out string errorMessage)
    {
        if (saveData.world.worldItems == null
            || saveData.world.gatherableResources == null)
        {
            errorMessage =
                "월드 아이템 또는 채집 자원 목록이 누락되었습니다.";
            return false;
        }

        HashSet<string> usedWorldObjectIds =
            new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0;
            index < saveData.world.worldItems.Count;
            index++)
        {
            WorldItemSaveData itemSaveData =
                saveData.world.worldItems[index];

            if (itemSaveData == null)
            {
                errorMessage =
                    "비어 있는 월드 아이템 저장 항목이 있습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemSaveData.worldObjectId))
            {
                errorMessage =
                    "월드 아이템의 World Object ID가 비어 있습니다.";
                return false;
            }

            if (!usedWorldObjectIds.Add(itemSaveData.worldObjectId))
            {
                errorMessage =
                    $"중복 저장 World Object ID입니다: "
                    + $"{itemSaveData.worldObjectId}";
                return false;
            }

            if (!itemDatabase.TryGetItem(
                itemSaveData.itemId,
                out ItemData itemData))
            {
                errorMessage =
                    $"등록되지 않은 월드 Item ID입니다: "
                    + $"{itemSaveData.itemId}";
                return false;
            }

            if (itemSaveData.quantity <= 0)
            {
                errorMessage =
                    $"월드 아이템 수량이 잘못되었습니다: {itemData.ItemId}";
                return false;
            }

            if (itemSaveData.position == null
                || itemSaveData.rotation == null)
            {
                errorMessage =
                    $"월드 아이템 위치 또는 회전이 누락되었습니다: "
                    + $"{itemSaveData.worldObjectId}";
                return false;
            }
        }

        for (int index = 0;
            index < saveData.world.gatherableResources.Count;
            index++)
        {
            GatherableResourceSaveData resourceSaveData =
                saveData.world.gatherableResources[index];

            if (resourceSaveData == null)
            {
                errorMessage =
                    "비어 있는 채집 자원 저장 항목이 있습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                resourceSaveData.worldObjectId))
            {
                errorMessage =
                    "채집 자원의 World Object ID가 비어 있습니다.";
                return false;
            }

            if (!usedWorldObjectIds.Add(
                resourceSaveData.worldObjectId))
            {
                errorMessage =
                    $"중복 저장 World Object ID입니다: "
                    + $"{resourceSaveData.worldObjectId}";
                return false;
            }

            if (!currentResources.TryGetValue(
                resourceSaveData.worldObjectId,
                out GatherableResource resource))
            {
                errorMessage =
                    $"Scene에서 채집 자원을 찾을 수 없습니다: "
                    + $"{resourceSaveData.worldObjectId}";
                return false;
            }

            if (resourceSaveData.remainingQuantity < 0
                || resourceSaveData.remainingQuantity >
                    resource.TotalQuantity)
            {
                errorMessage =
                    $"채집 자원 수량이 잘못되었습니다: "
                    + $"{resourceSaveData.worldObjectId}";
                return false;
            }

            if (resourceSaveData.isDepleted
                && resourceSaveData.remainingQuantity != 0)
            {
                errorMessage =
                    $"소진 자원의 남은 수량이 0이 아닙니다: "
                    + $"{resourceSaveData.worldObjectId}";
                return false;
            }

            if (!resourceSaveData.isDepleted
                && resourceSaveData.remainingQuantity <= 0)
            {
                errorMessage =
                    $"활성 자원의 남은 수량이 잘못되었습니다: "
                    + $"{resourceSaveData.worldObjectId}";
                return false;
            }

            bool hasInvalidRespawnTime =
                resourceSaveData.respawnRemainingSeconds < 0f
                || float.IsNaN(
                    resourceSaveData.respawnRemainingSeconds)
                || float.IsInfinity(
                    resourceSaveData.respawnRemainingSeconds);

            if (hasInvalidRespawnTime)
            {
                errorMessage =
                    $"채집 자원 재생성 시간이 잘못되었습니다: "
                    + $"{resourceSaveData.worldObjectId}";
                return false;
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}
