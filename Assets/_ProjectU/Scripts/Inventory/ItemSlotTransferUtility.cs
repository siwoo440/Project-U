public static class ItemSlotTransferUtility // 공통 슬롯 이동과 합치기 처리
{
    public static bool TryMoveOrMerge( // 컨테이너 사이 아이템 이동 처리
        IItemSlotContainer sourceContainer, // 출발 슬롯 컨테이너
        int sourceIndex, // 출발 슬롯 번호
        IItemSlotContainer targetContainer, // 대상 슬롯 컨테이너
        int targetIndex) // 대상 슬롯 번호
    {
        if (sourceContainer == null || targetContainer == null) // 컨테이너 존재 확인
        {
            return false; // 이동 실패 반환
        }

        if (!IsValidIndex(sourceContainer, sourceIndex)) // 출발 슬롯 범위 확인
        {
            return false; // 이동 실패 반환
        }

        if (!IsValidIndex(targetContainer, targetIndex)) // 대상 슬롯 범위 확인
        {
            return false; // 이동 실패 반환
        }

        bool isSameContainer = ReferenceEquals(sourceContainer, targetContainer); // 같은 컨테이너 여부 확인

        if (isSameContainer && sourceIndex == targetIndex) // 같은 슬롯 여부 확인
        {
            return false; // 중복 이동 차단
        }

        InventorySlot sourceSlot = sourceContainer.GetSlot(sourceIndex); // 출발 슬롯 조회
        InventorySlot targetSlot = targetContainer.GetSlot(targetIndex); // 대상 슬롯 조회

        if (sourceSlot == null || sourceSlot.ItemData == null || sourceSlot.Quantity <= 0) // 출발 아이템 확인
        {
            return false; // 빈 슬롯 이동 차단
        }

        bool isTargetSlotEmpty = targetSlot == null || targetSlot.ItemData == null || targetSlot.Quantity <= 0; // 대상 빈 슬롯 여부

        if (isTargetSlotEmpty) // 대상 빈 슬롯 확인
        {
            if (!targetContainer.TrySetSlotDirect(targetIndex, sourceSlot)) // 대상 슬롯 이동 적용
            {
                return false; // 대상 변경 실패 반환
            }

            if (!sourceContainer.TrySetSlotDirect(sourceIndex, null)) // 출발 슬롯 비우기 적용
            {
                targetContainer.TrySetSlotDirect(targetIndex, targetSlot); // 기존 대상 슬롯 원상 복구
                return false; // 출발 변경 실패 반환
            }

            NotifyChangedContainers(sourceContainer, targetContainer, isSameContainer); // 변경 내용 알림
            return true; // 빈 슬롯 이동 성공
        }

        if (targetSlot.Contains(sourceSlot.ItemData)) // 같은 아이템 여부 확인
        {
            if (targetSlot.IsFull) // 대상 최대 중첩 여부 확인
            {
                return false; // 최대 중첩 슬롯 이동 차단
            }

            int sourceQuantity = sourceSlot.Quantity; // 출발 수량 저장
            int remainingQuantity = targetSlot.AddQuantity(sourceQuantity); // 대상 슬롯 수량 추가
            int movedQuantity = sourceQuantity - remainingQuantity; // 실제 이동 수량 계산

            if (movedQuantity <= 0) // 실제 이동 여부 확인
            {
                return false; // 변경 없음 반환
            }

            sourceSlot.RemoveQuantity(movedQuantity); // 출발 슬롯 수량 감소

            if (sourceSlot.Quantity <= 0) // 출발 슬롯 소진 여부 확인
            {
                sourceContainer.TrySetSlotDirect(sourceIndex, null); // 소진된 출발 슬롯 비우기
            }

            NotifyChangedContainers(sourceContainer, targetContainer, isSameContainer); // 변경 내용 알림
            return true; // 같은 아이템 합치기 성공
        }

        if (!sourceContainer.TrySetSlotDirect(sourceIndex, targetSlot)) // 대상 아이템을 출발 슬롯에 적용
        {
            return false; // 출발 슬롯 변경 실패
        }

        if (!targetContainer.TrySetSlotDirect(targetIndex, sourceSlot)) // 출발 아이템을 대상 슬롯에 적용
        {
            sourceContainer.TrySetSlotDirect(sourceIndex, sourceSlot); // 출발 슬롯 원상 복구
            return false; // 대상 슬롯 변경 실패
        }

        NotifyChangedContainers(sourceContainer, targetContainer, isSameContainer); // 변경 내용 알림
        return true; // 서로 다른 아이템 교환 성공
    }

    private static bool IsValidIndex(IItemSlotContainer container, int index) // 슬롯 번호 범위 확인
    {
        return index >= 0 && index < container.SlotCapacity; // 유효 범위 결과 반환
    }

    private static void NotifyChangedContainers( // 변경된 컨테이너 알림
        IItemSlotContainer sourceContainer, // 출발 컨테이너
        IItemSlotContainer targetContainer, // 대상 컨테이너
        bool isSameContainer) // 같은 컨테이너 여부
    {
        sourceContainer.NotifyContentsChanged(); // 출발 컨테이너 변경 알림

        if (!isSameContainer) // 서로 다른 컨테이너 확인
        {
            targetContainer.NotifyContentsChanged(); // 대상 컨테이너 변경 알림
        }
    }
}
