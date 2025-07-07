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

    public void ResetBonuses()
    {
        magnetRangeBonus = 0f;
        magnetSpeedBonus = 0f;
    }

    public void Init(Unit playerUnit)
    {
        ResetBonuses();
        this.playerUnit = playerUnit;
        resourceMgr = GameManager.Instance.resourceMgr;
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
                break;

            case CollectibleType.Explosion:
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

    public float GetMagnetRange() => baseMagnetRange + magnetRangeBonus;
    public void AddMagnetRangeBonus(float bonus) => magnetRangeBonus += bonus;
    public float GetMagnetSpeed() => baseMagnetSpeed + magnetSpeedBonus;
    public void AddMagnetSpeedBonus(float bonus) => magnetSpeedBonus += bonus;
}