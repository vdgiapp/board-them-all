using System.Collections.Generic;
using Sirenix.OdinSerializer;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Base class for a lazily created singleton that owns persisted data (<typeparamref name="TData"/>,
    /// stored as Base64 string in PlayerPrefs) and reacts to timer events.
    /// Call <see cref="Initialize"/> once at startup before using the data.
    /// </summary>
    /// <typeparam name="T">The concrete singleton class (curiously recurring). Needs a public parameterless constructor.</typeparam>
    /// <typeparam name="TData">The persisted data type.</typeparam>
    public abstract class PlayerPrefSingleton<T, TData> where T : PlayerPrefSingleton<T, TData>, new() where TData : PlayerPrefCachedData, new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                    instance.Initialize(); // idempotent
                }
                return instance;
            }
        }

        public virtual string GetKeyData() => typeof(T).Name;
        public abstract string GetKeyEvent();
        protected virtual int GetCurrentVersion() => 1;

        protected TData cachedData;
        private bool _isInitialized;
        private bool _firstTick = true;
        private bool _isDirty;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            cachedData = Load();
            if (cachedData == null)
            {
                cachedData = new TData();
                cachedData.Version = GetCurrentVersion();
                cachedData.OnNewData();
                OnNewData();
                SaveData();
            }
            else if (cachedData.Version < GetCurrentVersion())
            {
                OnMigrate(cachedData.Version);
                cachedData.Version = GetCurrentVersion();
                SaveData();
            }
            cachedData.FirstTimeInit();
            PlayerPrefSingletonInitQueue.Enqueue(RunInitHooks);
        }
        
        private void RunInitHooks()
        {
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_NEW_SECOND, HandleNewSecond);
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_NEW_DAY, HandleNewDay);
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_NEW_WEEK, HandleNewWeek);
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_NEW_MONTH, HandleNewMonth);
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_APP_SUSPEND, HandleSuspend);

            FirstTimeInit();
            OnInitSuccess();
        }
        
        private TData Load()
        {
            string key = GetKeyData();
            string base64Data = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(base64Data)) return null;
            try
            {
                byte[] bytes = System.Convert.FromBase64String(base64Data);
                return SerializationUtility.DeserializeValue<TData>(bytes, DataFormat.Binary);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                PlayerPrefs.SetString(key + "_corrupt", base64Data); // keep a copy before it is overwritten
                return null;
            }
        }

        protected void SaveData()
        {
            _isDirty = false;
            byte[] bytes = SerializationUtility.SerializeValue(cachedData, DataFormat.Binary);
            string base64Data = System.Convert.ToBase64String(bytes);
            PlayerPrefs.SetString(GetKeyData(), base64Data);
            PlayerPrefs.Save();
        }

        protected virtual void OnValueChange()
        {
            _isDirty = true;
            TigerForge.EventManager.EmitEvent(GetKeyEvent());
        }

        public void ClearData()
        {
            _isDirty = false; // otherwise a pending save would write the key back
            PlayerPrefs.DeleteKey(GetKeyData());
        }

        private void HandleNewSecond()
        {
            if (_firstTick)
            {
                _firstTick = false;
                OnFirstTick();
            }
            OnNewSecond();
            if (_isDirty) SaveData(); // debounced save: at most once per second
        }

        private void HandleSuspend() { if (_isDirty) SaveData(); }
        private void HandleNewDay() => OnNextDay();
        private void HandleNewWeek() => OnNextWeek();
        private void HandleNewMonth() => OnNextMonth();

        protected virtual void OnNewData() { }
        protected virtual void FirstTimeInit() { }
        protected virtual void OnMigrate(int fromVersion) { }
        protected virtual void OnInitSuccess() { }

        protected virtual void OnFirstTick() { }
        protected virtual void OnNewSecond() { }
        protected virtual void OnNextDay() { }
        protected virtual void OnNextWeek() { }
        protected virtual void OnNextMonth() { }
    }

    /// <summary>
    /// Base class of the data persisted by a <see cref="PlayerPrefSingleton{T, TData}"/>. Must be Base64 serializable.
    /// </summary>
    [System.Serializable]
    public abstract class PlayerPrefCachedData
    {
        public int Version;
        public abstract void OnNewData();
        public abstract void FirstTimeInit();
    }
    
    public static class PlayerPrefSingletonInitQueue
    {
        private static readonly Queue<System.Action> queue = new();
        private static bool _running;

        public static void Enqueue(System.Action runHooks)
        {
            queue.Enqueue(runHooks);
            if (_running) return;

            _running = true;
            try
            {
                while (queue.Count > 0)
                {
                    try
                    {
                        queue.Dequeue()();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
            finally
            {
                _running = false;
            }
        }
    }
}