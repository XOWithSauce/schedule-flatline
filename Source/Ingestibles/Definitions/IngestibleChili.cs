namespace Flatline
{

    public class IngestibleChili : Ingestible
    {
        public static readonly float SystemicAmountPerDose = 0.05f;
        public override string ItemID => "chili";
        public override float Food { get => 0.05f; }
        public override float Energy { get => 0.03f; }
        public override float Thirst { get => 0f; }
        public override float HPRegen { get => 5f; }
        public override float toxicityInDose => SystemicAmountPerDose;
    }
}