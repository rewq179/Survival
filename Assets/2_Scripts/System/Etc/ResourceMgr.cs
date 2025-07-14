using UnityEngine;
using System.Collections.Generic;
using System;

public enum IconType
{
    Skill,
    Equipment,
    Item
}

public enum MonsterType
{
    Bee = 1001,        // 벌
    Bat = 1002,        // 박쥐
    Golem = 1003,      // 골렘
    Cobra = 1004,      // 코브라
    Dragon = 1005      // 드래곤
}

public class ResourceMgr : MonoBehaviour
{
    private string skillIconPath = "Icon/Skill";
    private string equipmentIconPath = "Icon/Equipment";
    private string itemIconPath = "Icon/Item";
    private string skillEffectPath = "SkillEffect";
    private string collectibleItemPath = "CollectibleItem";
    private string unitPrefabPath = "Prefabs/Unit/Monster";

    [SerializeField] private Unit playerUnitPrefab;
    private Dictionary<SkillKey, Sprite> skillIcons = new();
    private Dictionary<string, Sprite> equipmentIcons = new();
    private Dictionary<string, Sprite> itemIcons = new();
    private Dictionary<SkillKey, SkillParticleController> skillEffects = new();
    private Dictionary<int, Unit> unitPrefabs = new Dictionary<int, Unit>();
    private Dictionary<CollectibleType, CollectibleItem> collectibleItemPrefabs = new();

    public void LoadAllIcons()
    {
        ClearCache();

        LoadSkillIconsFromPath();
        LoadIconsFromPath(equipmentIconPath, equipmentIcons);
        LoadIconsFromPath(itemIconPath, itemIcons);
        LoadSkillEffectsFromPath();
        LoadUnitPrefabsFromPath();
        LoadCollectibleItemPrefabsFromPath();
    }

    private void ClearCache()
    {
        skillIcons.Clear();
        equipmentIcons.Clear();
        itemIcons.Clear();
        skillEffects.Clear();
        unitPrefabs.Clear();
        collectibleItemPrefabs.Clear();
    }

    private void LoadIconsFromPath(string path, Dictionary<string, Sprite> iconDictionary)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        foreach (Sprite sprite in sprites)
        {
            iconDictionary[sprite.name] = sprite;
        }
    }

    private void LoadSkillIconsFromPath()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(skillIconPath);
        foreach (Sprite sprite in sprites)
        {
            if (Enum.TryParse(sprite.name, out SkillKey skillKey))
                skillIcons[skillKey] = sprite;
        }
    }

    private void LoadSkillEffectsFromPath()
    {
        SkillParticleController[] effects = Resources.LoadAll<SkillParticleController>(skillEffectPath);
        foreach (SkillParticleController effect in effects)
        {
            if (Enum.TryParse(effect.name, out SkillKey skillKey))
                skillEffects[skillKey] = effect;
        }
    }

    private void LoadUnitPrefabsFromPath()
    {
        Unit[] units = Resources.LoadAll<Unit>(unitPrefabPath);
        foreach (Unit unit in units)
        {
            if (Enum.TryParse(unit.name, out MonsterType type))
                unitPrefabs[(int)type] = unit;
        }
    }

    private void LoadCollectibleItemPrefabsFromPath()
    {
        CollectibleItem[] items = Resources.LoadAll<CollectibleItem>(collectibleItemPath);
        foreach (CollectibleItem item in items)
        {
            collectibleItemPrefabs[Enum.Parse<CollectibleType>(item.name)] = item;
        }
    }

    public Sprite GetSkillIcon(SkillKey skillKey)
    {
        if (skillIcons.TryGetValue(skillKey, out Sprite sprite))
            return sprite;

        return null;
    }

    public Sprite GetEquipmentIcon(string iconName)
    {
        if (equipmentIcons.TryGetValue(iconName, out Sprite sprite))
            return sprite;

        return null;
    }

    public Sprite GetItemIcon(string iconName)
    {
        if (itemIcons.TryGetValue(iconName, out Sprite sprite))
            return sprite;

        return null;
    }

    public SkillParticleController GetSkillEffect(Transform parent, SkillKey skillKey)
    {
        if (skillEffects.TryGetValue(skillKey, out SkillParticleController effect))
            return Instantiate(effect, parent, false);

        return null;
    }

    public Unit GetPlayerUnit() => Instantiate(playerUnitPrefab);
    public Unit GetEnemyPrefab(int unitID)
    {
        if (unitPrefabs.TryGetValue(unitID, out Unit prefab))
            return prefab;

        return null;
    }

    public CollectibleItem GetCollectibleItem(CollectibleType type)
    {
        if (collectibleItemPrefabs.TryGetValue(type, out CollectibleItem prefab))
            return Instantiate(prefab);

        return null;
    }
}