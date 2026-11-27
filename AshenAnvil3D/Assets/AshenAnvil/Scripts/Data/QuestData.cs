using UnityEngine;

namespace AshenAnvil
{
    [CreateAssetMenu(menuName = "Ashen Anvil/Quest", fileName = "Quest_")]
    public class QuestData : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 3)] public string description;
        public QuestGoal goal;
        public int target = 10;
        public int goldReward = 100;
        public ItemData rewardItem;
    }
}
