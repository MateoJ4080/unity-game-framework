using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Events
{
    /// <summary>
    /// Decoupled event system for communication between components
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        private static EventBus _instance;
        private readonly Dictionary<Type, List<IEventHandler>> _eventHandlers = new Dictionary<Type, List<IEventHandler>>();
        private readonly Dictionary<Type, List<Delegate>> _eventActions = new Dictionary<Type, List<Delegate>>();

        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject eventBusObject = new GameObject("EventBus");
                    _instance = eventBusObject.AddComponent<EventBus>();
                    DontDestroyOnLoad(eventBusObject);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Subscribe to an event using an Action
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : class
        {
            Type eventType = typeof(T);

            if (!Instance._eventActions.ContainsKey(eventType))
            {
                Instance._eventActions[eventType] = new List<Delegate>();
            }

            Instance._eventActions[eventType].Add(handler);
        }

        /// <summary>
        /// Subscribe to an event using the IEventHandler interface
        /// </summary>
        public static void Subscribe<T>(IEventHandler<T> handler) where T : class
        {
            Type eventType = typeof(T);

            if (!Instance._eventHandlers.ContainsKey(eventType))
            {
                Instance._eventHandlers[eventType] = new List<IEventHandler>();
            }

            Instance._eventHandlers[eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from an event using an Action
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : class
        {
            Type eventType = typeof(T);

            if (Instance._eventActions.ContainsKey(eventType))
            {
                Instance._eventActions[eventType].Remove(handler);

                if (Instance._eventActions[eventType].Count == 0)
                {
                    Instance._eventActions.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Unsubscribe from an event using the IEventHandler interface
        /// </summary>
        public static void Unsubscribe<T>(IEventHandler<T> handler) where T : class
        {
            Type eventType = typeof(T);

            if (Instance._eventHandlers.ContainsKey(eventType))
            {
                Instance._eventHandlers[eventType].Remove(handler);

                if (Instance._eventHandlers[eventType].Count == 0)
                {
                    Instance._eventHandlers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers (Notify)
        /// </summary>
        public static void Publish<T>(T eventData) where T : class
        {
            Type eventType = typeof(T);

            // Notify handlers using Actions
            if (Instance._eventActions.ContainsKey(eventType))
            {
                var actions = new List<Delegate>(Instance._eventActions[eventType]);
                foreach (var action in actions)
                {
                    try
                    {
                        ((Action<T>)action)?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error executing event handler for {eventType}: {e.Message}");
                    }
                }
            }

            // Notify handlers using interfaces
            if (Instance._eventHandlers.ContainsKey(eventType))
            {
                var handlers = new List<IEventHandler>(Instance._eventHandlers[eventType]);
                foreach (var handler in handlers)
                {
                    try
                    {
                        ((IEventHandler<T>)handler)?.HandleEvent(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error executing event handler for {eventType}: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Clear all event handlers
        /// </summary>
        public static void Clear()
        {
            Instance._eventHandlers.Clear();
            Instance._eventActions.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }

    /// <summary>
    /// Base interface for event handlers
    /// </summary>
    public interface IEventHandler
    {
    }

    /// <summary>
    /// Generic interface for typed event handlers
    /// </summary>
    public interface IEventHandler<T> : IEventHandler where T : class
    {
        void HandleEvent(T eventData);
    }
}
