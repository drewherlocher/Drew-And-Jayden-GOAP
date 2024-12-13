using System;
using UnityEngine;

public class CountdownTimer
{
    private float timerInterval;
    private float currentTime;
    private bool isRunning;

    public event Action OnTimerStart;
    public event Action OnTimerStop;


    // Constructor to initialize the timer with an interval
    public CountdownTimer(float interval)
    {
        timerInterval = interval;
        currentTime = interval;
        isRunning = false;
    }

    // Starts the countdown timer
    public void Start()
    {
        if (!isRunning)
        {
            currentTime = timerInterval; // Reset to initial interval
            isRunning = true;
        }
    }

    // Stops the countdown timer
    public void Stop()
    {
        isRunning = false;
    }

    // Resets the timer back to the initial interval
    public void Reset()
    {
        currentTime = timerInterval;
    }

    // This method is called to tick the timer each frame
    public void Tick(float deltaTime)
    {
        if (isRunning)
        {
            currentTime -= deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;  // Ensure it doesn't go negative
                isRunning = false;

                // Trigger the OnTimerStop event
                OnTimerStop?.Invoke();
            }
        }
    }

    // Returns the remaining time
    public float GetRemainingTime()
    {
        return currentTime;
    }

    // Checks if the timer is running
    public bool IsRunning()
    {
        return isRunning;
    }
}
