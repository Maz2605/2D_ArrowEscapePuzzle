using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Utils.DesignPattern.Events
{
    public static class EventManager<T> where T : struct, Enum
    {
        private static readonly Dictionary<int, Delegate> _events = new Dictionary<int, Delegate>();

        private static int ToInt(T key) => Convert.ToInt32(key);

        #region Add Listener
        
        public static void AddListener(T eventID, Action callback)
        {
            int id = ToInt(eventID);
            if (!_events.ContainsKey(id)) _events[id] = null;
            _events[id] = (Action)_events[id] + callback;
        }

        public static void AddListener<TParam>(T eventID, Action<TParam> callback)
        {
            int id = ToInt(eventID);
            if (!_events.ContainsKey(id)) _events[id] = null;

            var currentDel = _events[id];
            if (currentDel != null && currentDel.GetType() != typeof(Action<TParam>))
            {
                Debug.LogError($"[EventManager] LỖI: Event '{eventID}' đã có listener kiểu khác. Không thể add kiểu {typeof(Action<TParam>).Name}.");
                return;
            }

            _events[id] = (Action<TParam>)_events[id] + callback;
        }
        #endregion

        #region Remove Listener
        
        public static void RemoveListener(T eventID, Action callback)
        {
            int id = ToInt(eventID);
            if (_events.TryGetValue(id, out var del))
            {
                var currentDel = del as Action;
                currentDel -= callback;
                if (currentDel == null) _events.Remove(id);
                else _events[id] = currentDel;
            }
        }

        public static void RemoveListener<TParam>(T eventID, Action<TParam> callback)
        {
            int id = ToInt(eventID);
            if (_events.TryGetValue(id, out var del))
            {
                var currentDel = del as Action<TParam>;
                currentDel -= callback;
                if (currentDel == null) _events.Remove(id);
                else _events[id] = currentDel;
            }
        }
        #endregion

        #region Post Event
        
        public static void Post(T eventID)
        {
            int id = ToInt(eventID);
            if (_events.TryGetValue(id, out var del))
            {
                (del as Action)?.Invoke();
            }
        }

        public static void Post<TParam>(T eventID, TParam param)
        {
            int id = ToInt(eventID);
            if (_events.TryGetValue(id, out var del))
            {
                var callback = del as Action<TParam>;
                if (callback != null)
                {
                    callback.Invoke(param);
                }
                else
                {
                    Debug.LogError($"[EventManager] LỖI: Event '{eventID}' bắn tham số kiểu {typeof(TParam).Name}, nhưng listener đang chờ kiểu khác!");
                }
            }
        }
        #endregion

        #region Maintenance
        
        public static void ClearAll()
        {
            _events.Clear();
        }
        #endregion
    }
}