using UnityEngine;

namespace AshenAnvil
{
    public static class LootSystem
    {
        public static void DropFromMonster(MonsterData data, Vector3 position)
        {
            if (data == null) return;
            int gold = Random.Range(data.goldMin, data.goldMax + 1);
            DropGold(position, gold, data.IsBoss ? 10 : 4);

            foreach (var entry in data.loot)
                if (entry.item != null && Random.value < entry.chance)
                    Pickup.SpawnItem(position, entry.item);

            var db = GameDatabase.Instance;
            if (db != null && db.defaultPotion != null && Random.value < 0.08f)
                Pickup.SpawnItem(position, db.defaultPotion);
        }

        public static void DropGold(Vector3 position, int total, int maxCoins)
        {
            if (total <= 0) return;
            int coins = Mathf.Clamp(total / 3, 1, maxCoins);
            int per = total / coins;
            int remainder = total - per * coins;
            for (int i = 0; i < coins; i++)
                Pickup.SpawnGold(position, per + (i == 0 ? remainder : 0));
        }
    }
}
