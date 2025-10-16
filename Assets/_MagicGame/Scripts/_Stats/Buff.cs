
namespace ProjectTinker
{
    public class Buff
    {
        public bool IsPermanent => _timer == null;
        public bool IsExpired => _timer != null && !_timer.IsRunning;
        public object Source => _modifier.Source;
        public Stat Stat => _stat;
        public string BuffName => _buffName;

        private readonly Timer _timer;
        private readonly StatModifier _modifier;
        private readonly Stat _stat;
        private string _buffName;

        public Buff(Stat stat, StatModifier modifier, string buffName, float? duration = null)
        {
            _stat = stat;
            _modifier = modifier;
            _buffName = buffName;
            _timer = duration.HasValue ? new Timer(duration.Value) : null;
        }

        // Helper method to create a buff from configuration
        public static Buff FromConfiguration(Stat stat, BuffConfiguration config)
        {
            float value = config.modifierType == StatModifierType.Flat ? config.flatValue : config.percentValue;
            StatModifier modifier = new StatModifier(value, config.modifierType, config);
            return new Buff(stat, modifier, config.buffName, config.duration > 0 ? config.duration : null);
        }

        public void Apply()
        {
            _stat.AddModifier(_modifier);
        }

        public void Remove()
        {
            _stat.RemoveModifier(_modifier);
        }

        public void Tick(float deltaTime)
        {
            _timer?.Tick(deltaTime);
        }
    }
}
