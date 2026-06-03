using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class HoldButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Settings")]
    [SerializeField] private float holdDelay = 1f;
    [SerializeField] private float startInterval = 0.5f;
    [SerializeField] private float minInterval = 0.05f;
    [SerializeField] private float acceleration = 0.9f;

    [Header("Events")]
    public UnityEvent onHoldClick;

    private bool isHolding;
    private bool hasStartedRepeating;

    private float nextTriggerTime;
    private float currentInterval;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isHolding) return;

        isHolding = true;
        hasStartedRepeating = false;

        // single click ONLY once
        onHoldClick?.Invoke();

        // reset timing
        currentInterval = startInterval;
        nextTriggerTime = Time.time + holdDelay;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        hasStartedRepeating = false;
    }

    private void Update()
    {
        if (!isHolding)
            return;

        // still in delay phase → do nothing
        if (Time.time < nextTriggerTime)
            return;

        // start repeating only once after delay
        if (!hasStartedRepeating)
        {
            hasStartedRepeating = true;
            nextTriggerTime = Time.time; // fire immediately after delay
            return;
        }

        if (Time.time >= nextTriggerTime)
        {
            onHoldClick?.Invoke();

            currentInterval = Mathf.Max(currentInterval * acceleration, minInterval);
            nextTriggerTime = Time.time + currentInterval;
        }
    }
}