namespace Flatline
{
    public abstract class Ingestible
    {
        public abstract string ItemID { get; }
        public abstract float Food { get; } // 0...1 float
        public abstract float Energy { get; } // 0 ...1 float
        public abstract float Thirst { get; } // 0...1 float
        public abstract float HPRegen { get; } // 0 - 100 float

        public virtual bool healIllness => false;
        public virtual bool increaseSanity => false;
        public virtual float toxicityInDose { get; }
    }
}