
public class Buff
{
    public string Name;
    public bool IsPermanent => _timer == null;
    public bool IsExpired => _timer != null && _timer.IsDone;

    private readonly Timer _timer;
    private readonly StatModifier _modifier;
    private readonly Stat _stat;

    public Buff(string name, Stat stat, StatModifier modifier, float? duration = null)
    {
        Name = name;
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