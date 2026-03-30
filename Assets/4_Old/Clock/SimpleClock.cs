using UnityEngine;
using System;
using DNExtensions.Utilities;


public class SimpleClock : MonoBehaviour
{
    [Header("Clock Hands")]
    [SerializeField] private Transform hourPivotHand;
    [SerializeField] private Transform minutePivotHand;
    [SerializeField] private Transform secondPivotHand;

    [Header("Custom Time Settings")]
    [SerializeField] private bool useSystemTime;
    [SerializeField] private int startHour = 10;
    [SerializeField] private int startMinute = 10;
    [SerializeField] private int startSecond = 30;


    [Header("Debug")]
    [ReadOnly] public string currentTimeString;
    [ReadOnly] public float hourAngle;
    [ReadOnly] public float minuteAngle;
    [ReadOnly] public float secondAngle;

    private DateTime _currentTime;
    private float _elapsedTime;

    void Start()
    {
        InitializeClock();
    }

    void InitializeClock()
    {
        if (useSystemTime)
        {
            _currentTime = DateTime.Now;
        }
        else
        {
            _currentTime = new DateTime(2000, 1, 1, startHour, startMinute, startSecond);
        }
        _elapsedTime = 0f;
        UpdateClockHands();
    }

    void Update()
    {
        if (useSystemTime)
        {
            _currentTime = DateTime.Now;
        }
        else
        {
            _elapsedTime += Time.deltaTime;
            _currentTime = _currentTime.AddSeconds(Time.deltaTime);
        }
        UpdateClockHands();
    }

    void UpdateClockHands()
    {
        float hour = _currentTime.Hour + _currentTime.Minute / 60f;
        float minute = _currentTime.Minute + _currentTime.Second / 60f;
        float second = _currentTime.Second + _currentTime.Millisecond / 1000f;

        hourAngle = hour * 30f;
        minuteAngle = minute * 6f;
        secondAngle = second * 6f;

        hourPivotHand.localRotation = Quaternion.Euler(0, 0, -hourAngle);
        minutePivotHand.localRotation = Quaternion.Euler(0, 0, -minuteAngle);
        secondPivotHand.localRotation = Quaternion.Euler(0, 0, -secondAngle);

        currentTimeString = _currentTime.ToString("HH:mm:ss.fff");
    }


    public void SetCustomRandomTime()
    {
        useSystemTime = false;
        startHour = UnityEngine.Random.Range(0, 24);
        startMinute = UnityEngine.Random.Range(0, 60);
        startSecond = UnityEngine.Random.Range(0, 60);
        InitializeClock();
    }

    public void ToggleTimeMode()
    {
        useSystemTime = !useSystemTime;
        InitializeClock();
    }
}