namespace Flatline
{
    public class IngestibleBanana : Ingestible
    {
        public override string ItemID => "banana";
        public override float Food { get => 0.18f; }
        public override float Energy { get => 0.015f; }
        public override float Thirst { get => 0f; }
        public override float HPRegen { get => 2f; }
    }
}