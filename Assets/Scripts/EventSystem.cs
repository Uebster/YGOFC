using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// FASE 12: Event System
/// Purpose: Centralized event broadcasting for Lua script interactions
/// Allows cards to subscribe to events like DRAW, DESTROY, SUMMON, DAMAGE, and CUSTOM
/// 
/// Usage:
/// EventSystem.Subscribe(EVENT_DRAW, (event) => { ... });
/// EventSystem.RaiseEvent(EVENT_DRAW, card, player);
/// </summary>
public class EventSystem : MonoBehaviour
{
    private static EventSystem instance;
    public static EventSystem Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("EventSystem");
                instance = go.AddComponent<EventSystem>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public delegate void EventCallback(DuelEvent evt);
    private Dictionary<int, List<EventCallback>> eventCallbacks = new Dictionary<int, List<EventCallback>>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    public static void Subscribe(int eventCode, EventCallback callback)
    {
        if (Instance.eventCallbacks.ContainsKey(eventCode) == false)
        {
            Instance.eventCallbacks[eventCode] = new List<EventCallback>();
        }
        Instance.eventCallbacks[eventCode].Add(callback);
        Debug.Log($"[EventSystem] Subscribed to event {eventCode}. Total subscribers: {Instance.eventCallbacks[eventCode].Count}");
    }

    /// <summary>
    /// Unsubscribe from an event type
    /// </summary>
    public static void Unsubscribe(int eventCode, EventCallback callback)
    {
        if (Instance.eventCallbacks.ContainsKey(eventCode))
        {
            Instance.eventCallbacks[eventCode].Remove(callback);
            Debug.Log($"[EventSystem] Unsubscribed from event {eventCode}. Remaining subscribers: {Instance.eventCallbacks[eventCode].Count}");
        }
    }

    /// <summary>
    /// Raise an event and notify all subscribers
    /// </summary>
    public static void RaiseEvent(int eventCode, params object[] args)
    {
        DuelEvent duelEvent = new DuelEvent
        {
            eventCode = eventCode,
            parameters = args,
            timestamp = Time.time
        };

        Debug.Log($"[EventSystem] Raising event {eventCode} with {args.Length} parameters");

        if (Instance.eventCallbacks.ContainsKey(eventCode))
        {
            List<EventCallback> callbacks = Instance.eventCallbacks[eventCode];
            for (int i = 0; i < callbacks.Count; i++)
            {
                try
                {
                    callbacks[i]?.Invoke(duelEvent);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EventSystem] Error in event callback: {ex.Message}");
                }
            }
            Debug.Log($"[EventSystem] Event {eventCode} processed by {callbacks.Count} subscribers");
        }
        else
        {
            Debug.LogWarning($"[EventSystem] No subscribers for event {eventCode}");
        }
    }

    /// <summary>
    /// Clear all subscribers for an event
    /// </summary>
    public static void ClearEvent(int eventCode)
    {
        if (Instance.eventCallbacks.ContainsKey(eventCode))
        {
            Instance.eventCallbacks[eventCode].Clear();
        }
    }

    /// <summary>
    /// Clear all subscribers for all events
    /// </summary>
    public static void ClearAllEvents()
    {
        Instance.eventCallbacks.Clear();
        Debug.Log("[EventSystem] All events cleared");
    }

    /// <summary>
    /// Get count of subscribers for an event (DEBUG)
    /// </summary>
    public static int GetSubscriberCount(int eventCode)
    {
        if (Instance.eventCallbacks.ContainsKey(eventCode))
        {
            return Instance.eventCallbacks[eventCode].Count;
        }
        return 0;
    }
}

/// <summary>
/// Data structure for duel events
/// </summary>
public struct DuelEvent
{
    public int eventCode;
    public object[] parameters;
    public float timestamp;

    public override string ToString()
    {
        return $"DuelEvent(code={eventCode}, params={parameters?.Length ?? 0}, time={timestamp})";
    }
}
