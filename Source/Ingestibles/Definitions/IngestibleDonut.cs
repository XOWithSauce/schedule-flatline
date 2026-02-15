namespace Flatline
{
    public class IngestibleDonut : Ingestible
    {
        public override string ItemID => "donut";
        public override float Food { get => 0.15f; }
        public override float Energy { get => 0.02f; }
        public override float Thirst { get => 0f; }
        public override float HPRegen { get => 1f; }
    }
}