using System.Collections.Generic;

namespace Agritech
{
    /// <summary>Everything the mod persists inside the save file.</summary>
    public class SaveModel
    {
        /// <summary>Soil nutrient level per tile, 0 (depleted) to 1 (rich). Key: "Location|x,y". Absent = 1.</summary>
        public Dictionary<string, float> Soil { get; set; } = new();

        /// <summary>Collar records, keyed by FarmAnimal.myID.</summary>
        public Dictionary<long, CollarRecord> Collars { get; set; } = new();

        /// <summary>Virtual fence zones drawn on the farm.</summary>
        public List<FenceZone> Zones { get; set; } = new();

        /// <summary>Index into Zones that collared animals are herded into; -1 = herding off.</summary>
        public int ActiveZone { get; set; } = -1;

        /// <summary>Agritech skill experience.</summary>
        public int SkillXp { get; set; }

        /// <summary>Relationship with each buyer, 0-1000. Key: buyer id.</summary>
        public Dictionary<string, int> Relationships { get; set; } = new();

        public List<Contract> Contracts { get; set; } = new();
        public List<Shipment> Shipments { get; set; } = new();
        public int NextContractId { get; set; } = 1;

        /// <summary>Most recent provenance certificates issued by the grading table, newest last (max 40).</summary>
        public List<string> ProvenanceLedger { get; set; } = new();

        /// <summary>Per-horse husbandry records, keyed by Horse.HorseId.</summary>
        public Dictionary<string, HorseRecord> Horses { get; set; } = new();

        /// <summary>Foals born and waiting for a new stable.</summary>
        public int PendingFoals { get; set; }
        public int DaysSinceLastFoal { get; set; }

        /// <summary>Buyer visits already hosted this visit day. Key: "buyer|day".</summary>
        public HashSet<string> HostedVisits { get; set; } = new();

        /// <summary>Drone actions performed today (for the terminal read-out).</summary>
        public int DroneActionsToday { get; set; }
        public int DroneXpToday { get; set; }
    }

    public class CollarRecord
    {
        public long AnimalId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int CollaredOnDay { get; set; }
        public int ProductsLogged { get; set; }
        public int DaysTracked { get; set; }

        /// <summary>Rolling history of daily health scores (0-100), newest last, max 28 entries.</summary>
        public List<int> HealthHistory { get; set; } = new();
    }

    public class FenceZone
    {
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool Contains(float tileX, float tileY)
            => tileX >= X && tileX < X + Width && tileY >= Y && tileY < Y + Height;
    }

    public class Contract
    {
        public int Id { get; set; }
        public string BuyerId { get; set; }
        public string MaterialId { get; set; }

        /// <summary>Minimum luxury tier (0 = any, 1 Fine, 2 Prime, 3 Estate, 4 Couture). Ignored for ungraded goods.</summary>
        public int MinTier { get; set; }
        public int Quantity { get; set; }
        public int PostedDay { get; set; }
        public int DeadlineDay { get; set; }
        public int Reward { get; set; }
        public bool Standing { get; set; }

        /// <summary>Open, Shipped, Done, Expired.</summary>
        public string Status { get; set; } = "Open";
    }

    public class Shipment
    {
        public int ContractId { get; set; }
        public string Mode { get; set; }
        public int ArriveDay { get; set; }
        public int Value { get; set; }
        public bool Spoiled { get; set; }
    }

    public class HorseRecord
    {
        public string HorseId { get; set; }
        public string Name { get; set; }
        public int Bond { get; set; }
        public int LastGroomedDay { get; set; } = -1;
        public float TrainingDistance { get; set; }
        public int Foals { get; set; }

        public int TrainingLevel => (int)System.Math.Min(5, TrainingDistance / 20000f);
    }
}
