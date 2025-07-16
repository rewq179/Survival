using UnityEngine;
using System;
using System.Collections.Generic;

public class RewardMgr : MonoBehaviour
{
    private Unit playerUnit;
    private ResourceMgr resourceMgr;
    private List<CollectibleItem> activeItems = new();
    private readonly CollectibleType[] randomTypes = {
        CollectibleType.Magnet, CollectibleType.Freeze, CollectibleType.Explosion, CollectibleType.Heal
    };

    
    // 자석 스탯
    private float baseMagnetRange = 3f;
    private float baseMagnetSpeed = 10f;
    private float magnetRangeBonus = 0f;
    private float magnetSpeedBonus = 0f;

    // 오브젝트 풀
    private int POOL_SIZE = 5;
    private Dictionary<CollectibleType, Queue<CollectibleItem>> itemPools = new();

    // 배치 처리를 위한 데이터 구조
    private List<CollectibleItem> itemsInMagnetRange = new();
    private List<CollectibleItem> itemsOutsideMagnetRange = new();
    
    // 배치 처리용 임시 데이터
    private Vector3[] itemPositions;
    private float[] itemDistances;
    private bool[] itemInRange;

    public void ResetBonuses()
    {
        magnetRangeBonus = 0f;
        magnetSpeedBonus = 0f;
    }

    public void Init(Unit playerUnit)
    {
        ResetBonuses();
        this.playerUnit = playerUnit;
        resourceMgr = GameMgr.Instance.resourceMgr;
        InitializeItemPools();
    }

    private void InitializeItemPools()
    {
        for (CollectibleType type = 0; type < CollectibleType.Max; type++)
        {
            Queue<CollectibleItem> pool = new Queue<CollectibleItem>();
            itemPools[type] = pool;

            for (int i = 0; i < POOL_SIZE; i++)
            {
                RemoveItem(PopCollectibleItem(type));
            }
        }
    }

    public void CreateItem(Unit unit)
    {
        Vector3 position = unit.transform.position;

        // 고정 드랍
        UnitData data = DataMgr.GetUnitData(unit.UnitID);
        CreateCollectibleItem(CollectibleType.Exp, position, data.exp);
        CreateCollectibleItem(CollectibleType.Gold, position, data.gold);

        // 랜덤 드랍
        CollectibleType type = GetRandomItemType();
        CreateCollectibleItem(type, position, GetRandomItemValue(type));
    }

    public void CreateCollectibleItem(CollectibleType type, Vector3 position, float value)
    {
        if (type == CollectibleType.Max)
            return;

        CollectibleItem item = PopCollectibleItem(type);
        item.Init(type, position, value);
        activeItems.Add(item);
    }

    private void RemoveItem(CollectibleItem item)
    {
        PushCollectibleItem(item);
        activeItems.Remove(item);
        item.Reset();
    }

    private CollectibleType GetRandomItemType()
    {
        if (UnityEngine.Random.Range(0f, 1f) >= 0.1f)
            return CollectibleType.Max;

        return randomTypes[UnityEngine.Random.Range(0, randomTypes.Length)];
    }

    private float GetRandomItemValue(CollectibleType type)
    {
        return type switch
        {
            CollectibleType.Heal => 0.6f,
            _ => 0f
        };
    }

    public void OnItemCollected(CollectibleItem item)
    {
        switch (item.Type)
        {
            case CollectibleType.Gold:
                playerUnit.AddGold((int)item.Value);
                break;

            case CollectibleType.Exp:
                playerUnit.AddExp(item.Value);
                break;

            case CollectibleType.Magnet:
                ActivateGlobalMagnet();
                break;

            case CollectibleType.Freeze:
                GameMgr.Instance.skillMgr.ExecuteItemSkill(playerUnit, CollectibleType.Freeze);
                break;

            case CollectibleType.Explosion:
                GameMgr.Instance.skillMgr.ExecuteItemSkill(playerUnit, CollectibleType.Explosion);
                break;

            case CollectibleType.Heal:
                playerUnit.TakeHealRate(item.Value);
                break;
        }

        RemoveItem(item);
    }

    private void ActivateGlobalMagnet()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            activeItems[i].ApplyGlobalMagnetEffect();
        }
    }

    private CollectibleItem PopCollectibleItem(CollectibleType type)
    {
        if (itemPools.TryGetValue(type, out Queue<CollectibleItem> pool) && pool.Count > 0)
            return pool.Dequeue();

        return resourceMgr.GetCollectibleItem(type);
    }

    private void PushCollectibleItem(CollectibleItem item)
    {
        if (itemPools.TryGetValue(item.Type, out Queue<CollectibleItem> pool))
            pool.Enqueue(item);
    }

    public float GetMagnetRange()
    {
        float multiplier = 1 + playerUnit.GetFinalStat(StatType.MagnetRange);
        return (baseMagnetRange + magnetRangeBonus) * multiplier;
    }

    public void AddMagnetRangeBonus(float bonus) => magnetRangeBonus += bonus;

    public float GetMagnetSpeed() => baseMagnetSpeed + magnetSpeedBonus;
    public void AddMagnetSpeedBonus(float bonus) => magnetSpeedBonus += bonus;

    private void Update()
    {
        CheckAllItems();
    }

    private void CheckAllItems()
    {
        if (activeItems.Count == 0 || playerUnit == null)
            return;

        Vector3 playerPos = playerUnit.transform.position;
        float magnetRange = GetMagnetRange();
        float magnetSpeed = GetMagnetSpeed();
        float magnetRangeSqr = magnetRange * magnetRange;
        float collectRangeSqr = CollectibleItem.BASE_COLLECT_RANGE * CollectibleItem.BASE_COLLECT_RANGE;

        EnsureArrayCapacity(activeItems.Count);
        // 1단계: 모든 아이템의 거리 계산
        CalculateItemDistances(playerPos);
        // 2단계: 자석 범위 내 아이템 이동
        MoveItemsInMagnetRange(playerPos, magnetSpeed, magnetRangeSqr, collectRangeSqr);
        // 3단계: 수집 범위 내 아이템 처리
        CollectItemsInRange(collectRangeSqr);
    }

    private void CalculateItemDistances(Vector3 playerPos)
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            CollectibleItem item = activeItems[i];
            if (item == null || item.IsCollected)
                continue;

            Vector3 itemPos = item.transform.position;
            itemPositions[i] = itemPos;
            
            Vector3 direction = playerPos - itemPos;
            itemDistances[i] = direction.sqrMagnitude;
        }
    }

    private void MoveItemsInMagnetRange(Vector3 playerPos, float magnetSpeed, float magnetRangeSqr, float collectRangeSqr)
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            CollectibleItem item = activeItems[i];
            if (item == null || item.IsCollected)
                continue;

            float distance = itemDistances[i];
            bool shouldMove = item.IsGolbalMagentEffect || distance <= magnetRangeSqr;
            if (!shouldMove)
                continue;;

            Vector3 direction = (playerPos - itemPositions[i]).normalized;
            float curSpeed = item.IsGolbalMagentEffect ? magnetSpeed * CollectibleItem.MAGNET_EFFECT_SPEED_MULTIPLIER : magnetSpeed;
            
            Vector3 newPos = itemPositions[i] + direction * curSpeed * Time.deltaTime;
            item.transform.position = newPos;
            itemPositions[i] = newPos;
        }
    }

    private void CollectItemsInRange(float collectRangeSqr)
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            CollectibleItem item = activeItems[i];
            if (item == null || item.IsCollected)
                continue;

            float distance = itemDistances[i];
            if (distance <= collectRangeSqr)
            {
                item.Collect();
            }
        }
    }

    private void EnsureArrayCapacity(int count)
    {
        if (itemPositions == null || itemPositions.Length < count)
        {
            itemPositions = new Vector3[count];
            itemDistances = new float[count];
            itemInRange = new bool[count];
        }
    }
}