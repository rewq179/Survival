using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;
using System;

public enum IconType
{
    Skill,
    Equipment,
    Item
}

public class ResourceManager : MonoBehaviour
{
    private string skillIconPath = "Icon/Skill";
    private string equipmentIconPath = "Icon/Equipment";
    private string itemIconPath = "Icon/Item";
    private string skillEffectPath = "SkillEffect";

    private Dictionary<string, Sprite> skillIcons = new();
    private Dictionary<string, Sprite> equipmentIcons = new();
    private Dictionary<string, Sprite> itemIcons = new();
    private Dictionary<SkillKey, GameObject> skillEffects = new();

    public void LoadAllIcons()
    {
        ClearCache();

        LoadIconsFromPath(skillIconPath, skillIcons);
        LoadIconsFromPath(equipmentIconPath, equipmentIcons);
        LoadIconsFromPath(itemIconPath, itemIcons);
        LoadSkillEffectsFromPath(skillEffectPath);
    }

    private void ClearCache()
    {
        skillIcons.Clear();
        equipmentIcons.Clear();
        itemIcons.Clear();
        skillEffects.Clear();
    }

    private void LoadIconsFromPath(string path, Dictionary<string, Sprite> iconDictionary)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        foreach (Sprite sprite in sprites)
        {
            iconDictionary[sprite.name] = sprite;
        }
    }

    private void LoadSkillEffectsFromPath(string path)
    {
        GameObject[] effects = Resources.LoadAll<GameObject>(path);
        foreach (GameObject effect in effects)
        {
            skillEffects[Enum.Parse<SkillKey>(effect.name)] = effect;
        }
    }

    public Sprite GetSkillIcon(string iconName)
    {
        if (skillIcons.TryGetValue(iconName, out Sprite sprite))
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

    public GameObject GetSkillEffect(Transform parent, SkillKey skillKey)
    {
        if (skillEffects.TryGetValue(skillKey, out GameObject effect))
            return Instantiate(effect, parent, false);

        return null;
    }
}