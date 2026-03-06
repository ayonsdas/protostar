public class InputBuffer
{
    private Timer timer;
    public InputBuffer(float bufferTime)
    {
        timer = new Timer(bufferTime);
    }

    public void Press()
    {
        timer.Restart();
    }

    public bool Consume()
    {
        if (timer.IsActive)
        {
            timer.Stop();
            return true;
        }

        return false;
    }
}
