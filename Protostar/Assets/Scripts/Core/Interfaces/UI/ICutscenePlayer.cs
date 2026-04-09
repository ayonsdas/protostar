using System;

public interface ICutscenePlayer
{
    event Action OnClose;
}

public interface ICutscenePlayer<T> : ICutscenePlayer
{
    void Play(T cutscene);
}