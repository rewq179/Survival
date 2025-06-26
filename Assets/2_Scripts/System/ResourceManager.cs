using UnityEngine;
using System.Collections.Generic;

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

    private Dictionary<string, Sprite> skillIcons = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> equipmentIcons = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> itemIcons = new Dictionary<string, Sprite>();
    private Dictionary<string, GameObject> skillEffects = new Dictionary<string, GameObject>();

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
            skillEffects[effect.name] = effect;
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

    public GameObject GetSkillEffect(SkillLauncher launcher, string skillName)
    {
        if (skillEffects.TryGetValue(skillName, out GameObject effect))
            return Instantiate(effect, launcher.transform, false);

        return null;
    }
}