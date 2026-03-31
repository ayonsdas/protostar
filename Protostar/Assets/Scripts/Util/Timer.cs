public class Timer
{
    private float duration;
    private float timer;
    private bool stopped = true;

    public bool IsActive => !stopped && timer > 0;

    public Timer(float duration)
    {
        this.duration = duration;
        timer = duration;
        UpdateManager.OnUpdate += HandleUpdate;
    }

    ~Timer()
    {
        UpdateManager.OnUpdate -= HandleUpdate;
    }

    private void HandleUpdate(float deltaTime)
    {
        if (!stopped)
            timer -= deltaTime;
    }

    public void Start()
    {
        stopped = false;
    }

    public void Stop()
    {
        stopped = true;
    }

    public void Reset()
    {
        timer = duration;
    }

    public void Restart()
    {
        timer = duration;
        Start();
    }

    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        Reset();
    }
}