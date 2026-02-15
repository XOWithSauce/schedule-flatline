namespace Flatline
{
    public class IngestibleFluMedicine : Ingestible
    {
        public override string ItemID => "flumedicine";
        public override float Food { get => 0f; }
        public override float Energy { get => 0f; }
        public override float Thirst { get => 0.05f; }
        public override float HPRegen { get => 3f; }

        public override bool healIllness => true;
    }
}