namespace Flatline
{
 
    public class IngestibleBean : Ingestible
    {
        public override string ItemID => "megabean";
        public override float Food { get => 0.08f; }
        public override float Energy { get => 0.10f; }
        public override float Thirst { get => 0f; }
        public override float HPRegen { get => 3f; }
    }
}