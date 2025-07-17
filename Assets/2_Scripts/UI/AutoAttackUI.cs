using UnityEngine;
using UnityEngine.UI;

public class AutoAttackUI : MonoBehaviour
{
    [SerializeField] private GameObject onObject;
    [SerializeField] private Button onButton;
    [SerializeField] private GameObject offObject;
    [SerializeField] private Button offButton;
    private Unit playerUnit;

    private void Awake()
    {
        onButton.onClick.AddListener(OnButtonClick);
        offButton.onClick.AddListener(OffButtonClick);
    }

    public void Init(Unit playerUnit)
    {
        this.playerUnit = playerUnit;
    }

    private void OnButtonClick() => SetAutoAttack(true);
    private void OffButtonClick() => SetAutoAttack(false);

    private void SetAutoAttack(bool isAutoAttack)
    {
        playerUnit.SetAutoAttack(isAutoAttack);
        UIMgr.Instance.attackJoystick.SetAutoAttack(isAutoAttack);

        onObject.SetActive(isAutoAttack);
        offObject.SetActive(!isAutoAttack);
    }
}
