namespace Flatline
{
    public class IngestibleCuke : Ingestible
    {
        public override string ItemID => "cuke";
        public override float Food { get => 0f; }
        public override float Energy { get => 0.015f; }
        public override float Thirst { get => 0.14f; }
        public override float HPRegen { get => 3f; }
    }
}