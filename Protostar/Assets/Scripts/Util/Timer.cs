public class Timer
{
    private float time;
    private float timer;
    private bool stopped;

    public bool IsActive => !stopped && timer > 0;

    public Timer(float time)
    {
        this.time = time;
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

    public void Restart()
    {
        timer = time;
        Start();
    }
}