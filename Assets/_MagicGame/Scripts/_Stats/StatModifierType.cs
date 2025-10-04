
namespace ProjectWizard
{
    public enum StatModifierType
    {
        Flat,
        Percent
    }

    public class StatModifier
    {
        public float Value { get; }
        public StatModifierType Type { get; }
        public object Source { get; } // for removal tracking

        public StatModifier(float value, StatModifierType type, object source = null, bool isPermanent = false)
        {
            Value = value;
            Type = type;
            Source = source;
        }
    }
}
