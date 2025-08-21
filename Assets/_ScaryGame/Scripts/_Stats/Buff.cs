
public class Buff
{
    public bool IsPermanent => _timer == null;
    public bool IsExpired => _timer != null && !_timer.IsRunning;
    public object Source => _modifier.Source;
    public Stat Stat => _stat;

    private readonly Timer _timer;
    private readonly StatModifier _modifier;
    private readonly Stat _stat;

    public Buff(Stat stat, StatModifier modifier, float? duration = null)
    {
        _stat = stat;
        _modifier = modifier;
        _timer = duration.HasValue ? new Timer(duration.Value) : null;
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