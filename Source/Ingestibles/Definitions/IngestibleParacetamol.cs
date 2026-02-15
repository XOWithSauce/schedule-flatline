namespace Flatline
{
    public class IngestibleParacetamol : Ingestible
    {
        public static readonly float SystemicAmountPerDose = 0.17f;
        public static readonly float MaxHPRegen = 4f;
        public override string ItemID => "paracetamol";
        public override float Food { get => 0f; }
        public override float Energy { get => 0f; }
        public override float Thirst { get => 0f; }
        public override float HPRegen { get => 8f; }

        public override bool healIllness => true;
        public override float toxicityInDose => SystemicAmountPerDose;
    }
}