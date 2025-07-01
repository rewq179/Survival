using UnityEngine;
using System.Collections.Generic;

public class DamageTextMgr : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private DamageText damageTextPrefab; // 3D용 프리팹
    private const int POOL_SIZE = 20;

    // 애니메이션 설정
    private const float FADE_TIME = 1f;
    private const float MOVE_DISTANCE = 2f;

    private Queue<DamageText> textPool = new Queue<DamageText>(POOL_SIZE);

    public void ShowDamageText(Vector3 worldPosition, int damage, Color textColor)
    {
        PopDamageText().ShowDamageText(worldPosition, damage, textColor);
    }

    public DamageText PopDamageText()
    {
        if (textPool.Count == 0)
        {
            DamageText damageText = Instantiate(damageTextPrefab, transform);
            damageText.Initialize(this, FADE_TIME, MOVE_DISTANCE);
            return damageText;
        }

        return textPool.Dequeue();
    }

    public void PushDamageText(DamageText damageText)
    {
        damageText.Reset();
        textPool.Enqueue(damageText);
    }
}
