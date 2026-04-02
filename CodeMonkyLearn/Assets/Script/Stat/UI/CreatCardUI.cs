using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using UnityEngine.XR;

public class CreatCardUI : MonoBehaviour
{
    //引用所有文本与按钮，统一方法随机化，被生成时调用随机化
    [SerializeField] private TextMeshProUGUI stability;
    [SerializeField] private TextMeshProUGUI handling;
    [SerializeField] private TextMeshProUGUI presition;
    [SerializeField] private TextMeshProUGUI constitution;
    [SerializeField] private TextMeshProUGUI willpower;
    [SerializeField] private TextMeshProUGUI speed;
    [SerializeField] private TextMeshProUGUI randomName;
    [SerializeField] private TextMeshProUGUI trait1;
    [SerializeField] private TextMeshProUGUI trait2;
    [SerializeField] private Button creat;


    // 特质中文映射
    private Dictionary<Traits, string> TraitNames = new Dictionary<Traits, string>()
    {
        { Traits.Strong, "强壮" },
        { Traits.Agile, "敏捷" },
        { Traits.Brave, "勇敢" },
        { Traits.MagicResist, "魔抗" },
        { Traits.Lazy, "懒惰" },
        { Traits.Thrifty, "节约" },
        { Traits.SteelArm, "麒麟臂" }
    };

    // 存储当前面板的随机数据
    private string currentName;
    private int currentStability;
    private int currentHandling;
    private int currentPresition;
    private int currentConstitution;
    private int currentWillpower;
    private int currentSpeed;
    private Traits currentTrait1;
    private Traits currentTrait2;


    void Start()
    {
        showRandomText();
        creat.onClick.AddListener(OnClickCreat);
        creat.onClick.AddListener(() => creat.interactable = false); // 点一次后禁用
    }

    public void showRandomText()
    {
        int trainLevel = StatManager.Instance.trainLevel;
        int min = 1;
        int max = 6 + trainLevel;

        // 随机并存储
        currentName = GetRandomName();
        currentStability = Random.Range(min, max + 1);
        currentHandling = Random.Range(min, max + 1);
        currentPresition = Random.Range(min, max + 1);
        currentConstitution = Random.Range(min, max + 1);
        currentWillpower = Random.Range(min, max + 1);
        currentSpeed = Random.Range(min, max + 1);

        // 随机两个不同特质
        Traits[] all = (Traits[])System.Enum.GetValues(typeof(Traits));
        currentTrait1 = all[Random.Range(0, all.Length)];
        do { currentTrait2 = all[Random.Range(0, all.Length)]; } while (currentTrait2 == currentTrait1);

        // 更新 UI 显示
        randomName.text = currentName;
        stability.text = $"稳定: {currentStability}";
        handling.text = $"操控: {currentHandling}";
        presition.text = $"精准: {currentPresition}";
        constitution.text = $"体质: {currentConstitution}";
        willpower.text = $"意志: {currentWillpower}";
        speed.text = $"速度: {currentSpeed}";
        trait1.text = TraitNames.TryGetValue(currentTrait1, out string name1) ? name1 : currentTrait1.ToString();
        trait2.text = TraitNames.TryGetValue(currentTrait2, out string name2) ? name2 : currentTrait2.ToString();
    }

    public void OnClickCreat()
    {
        StatManager.Instance.CreatUnit(
            currentName,
            currentStability, currentHandling, currentPresition,
            currentConstitution, currentWillpower, currentSpeed,
            currentTrait1, currentTrait2
        );
        StatManager.Instance.onClickCreat();
    }


    private string GetRandomName()
    {
        List<string> pool = StatManager.Instance.GetNamePool();
        return pool[Random.Range(0, pool.Count)];
    }

}
