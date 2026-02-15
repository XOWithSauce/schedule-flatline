namespace Flatline
{
    public class IngestibleAddy : Ingestible
    {
        public static readonly float SystemicAmountPerDose = 0.22f;
        public override string ItemID => "addy";
        public override float Food { get => 0f; }
        public override float Energy { get => 0.18f; }
        public override float Thirst { get => 0f; }
        public override float HPRegen { get => 1f; }

        public override bool increaseSanity => true;
        public override float toxicityInDose => SystemicAmountPerDose;
    }
}