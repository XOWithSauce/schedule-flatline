namespace Flatline
{
    public class IngestibleEnergyDrink : Ingestible
    {
        public override string ItemID => "energydrink";
        public override float Food { get => 0f; }
        public override float Energy { get => 0.05f; }
        public override float Thirst { get => 0.10f; }
        public override float HPRegen { get => 1f; }

        public override bool increaseSanity => true;
    }
}