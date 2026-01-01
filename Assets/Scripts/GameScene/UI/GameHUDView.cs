using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

public class GameHUDView : MonoBehaviour
{
    public Slider HealthSlider;
    public Image QSkill;
    public Image WSkill;
    public Image ESkill;
    public TMP_Text points;
    public Sprite itemIcon;

    public Image item1;
    public Image item2;
    public Image item3;

    //[SerializeField] GameHUDViewModel _vm;
    private PlayerNetworkHealth _pnHealth;
    private PlayerNetworkCombat _pnCombat;
    private PlayerNetworkCore _core;

    public static GameHUDView Instance { get; private set; } // 简单的单例


    private void Awake()
    {
        Instance = this;
    }

    // 提供一个公开的 Bind 方法，接收具体的玩家实例
    public void BindToLocalPlayer(PlayerNetworkHealth health, PlayerNetworkCombat combat, PlayerNetworkCore core)
    {
        _pnHealth = health;
        _pnCombat = combat;
        _core = core;
        BindView();
    }
    public void BindView()
    {
        _pnHealth.CurrentHealthVar.OnValueChanged += UpdateHealthSlider;

        _pnCombat.QSkillActiveVar.OnValueChanged += UpdateQSkill;
        _pnCombat.WSkillActiveVar.OnValueChanged += UpdateWSkill;
        _pnCombat.ESkillActiveVar.OnValueChanged += UpdateESkill;

        _core.PointVar.OnValueChanged += UpdatePoints;
        _core.ItemsVar.OnListChanged += UpdateItems;

        // 3. 立即刷新一次 UI 到最新状态
        UpdateHealthSlider(_pnHealth.MaxHealth, _pnHealth.MaxHealth);
        UpdateQSkill(true, _pnCombat.QSkillActiveVar.Value);
        UpdateWSkill(true, _pnCombat.WSkillActiveVar.Value);
        UpdateESkill(true, _pnCombat.ESkillActiveVar.Value);
    }
    public void UpdateHealthSlider(int preHealth, int currentHealth)
    {
        if (HealthSlider == null) return;
        //Debug.Log($"prehealth = {preHealth}, currenthealth = {currentHealth}");
        HealthSlider.value = (float)currentHealth / (float)_pnHealth.MaxHealth;
    }
    public void UpdateQSkill(bool ignore, bool active)
    {
        QSkill.color = active ? Color.white : Color.gray;
    }
    public void UpdateWSkill(bool ignore, bool active)
    {
        WSkill.color = active ? Color.white : Color.gray;
    }
    public void UpdateESkill(bool ignore, bool active)
    {
        ESkill.color = active ? Color.white : Color.gray;
    }
    public void UpdatePoints(int pre, int current)
    {
        points.text = current.ToString();
    }

    public void UpdateItems(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        // ❌ 错误写法：直接拿结构体和 null 比较会导致崩溃
        // item1.sprite = changeEvent.Value == null ? itemIcon : null;

        // ✅ 正确逻辑：根据列表中当前的元素数量，决定显示几个图标
        // 我们不关心这次是 Add 还是 Remove，我们只关心现在背包里有几个东西

        // 获取当前背包里的物品数量
        int count = _core.ItemsVar.Count;
        // 格子 1：如果数量 >= 1，显示图标，否则隐藏
        if (count >= 1)
        {
            item1.sprite = itemIcon; // 这里暂时都用通用图标，以后可以根据 _core.ItemsVar[0] 的名字换图片
            item1.color = Color.white; // 确保图片可见
        }
        else
        {
            item1.sprite = null;
            item1.color = Color.clear; // 或者设为透明
        }

        // 格子 2
        if (count >= 2)
        {
            item2.sprite = itemIcon;
            item2.color = Color.white;
        }
        else
        {
            item2.sprite = null;
            item2.color = Color.clear;
        }

        // 格子 3
        if (count >= 3)
        {
            item3.sprite = itemIcon;
            item3.color = Color.white;
        }
        else
        {
            item3.sprite = null;
            item3.color = Color.clear;
        }
    }
}