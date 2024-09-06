using Life.BizSystem;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemsManager.Entities
{
    public class ItemsManager_Item : ModKit.ORM.ModEntity<ItemsManager_Item>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int MaxSlotCount { get; set; }
        public int Weight { get; set; }
        public bool IsBuyable { get; set; }
        public bool IsResellable { get; set; }
        public bool IsStackable { get; set; }

        public string ActivityAccess { get; set; }
        [Ignore]
        public List<Activity.Type> LActivityAccess { get; set; } = new List<Activity.Type>();

        public bool IsConsumable { get; set; }
        public int Food { get; set; }
        public int Thirst { get; set; }
        public bool IsCuisinable { get; set; }
        public bool IsAlcohol { get; set; }
        public float AlcoholValue { get; set; }
        public float TimeToBeCooked { get; set; }
        public float TimeToBeBurnt{ get; set; }

        public ItemsManager_Item()
        {
        }

        public void SerializeActivityAccess(List<Activity.Type> listActivityAccess)
        {
            // Convertir la liste d'énum en une chaîne avec les index séparés par des virgules
            ActivityAccess = string.Join(",", listActivityAccess.Select(a => ((int)a).ToString()));
        }

        public void DeserializeActivityAccess(string activityAccess)
        {
            if (!string.IsNullOrEmpty(activityAccess))
            {
                // Séparer la chaîne en différentes parties
                var parts = activityAccess.Split(',');

                // Vider la liste avant de la peupler
                LActivityAccess.Clear();

                foreach (var part in parts)
                {
                    // Convertir chaque élément en int et ensuite en Type (vérifier si conversion réussie)
                    if (int.TryParse(part, out int value))
                    {
                        if (Enum.IsDefined(typeof(Activity.Type), value))
                        {
                            LActivityAccess.Add((Activity.Type)value);
                        }
                    }
                }
            }
        }
    }
}
