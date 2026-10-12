using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Emitted once per second by the game timer.
    /// </summary>
    public readonly struct EventTimerNewSecond { }

    /// <summary>
    /// Emitted by the game timer when a new day starts.
    /// </summary>
    public readonly struct EventTimerNewDay { }

    /// <summary>
    /// Emitted by the game timer when a new week starts.
    /// </summary>
    public readonly struct EventTimerNewWeek { }

    /// <summary>
    /// Emitted by the game timer when a new month starts.
    /// </summary>
    public readonly struct EventTimerNewMonth { }

    /// <summary>
    /// Emitted when the app is about to lose control: <c>OnApplicationPause(true)</c>,
    /// <c>OnApplicationFocus(false)</c> and <c>OnApplicationQuit</c>. Pending data is saved on it.
    /// </summary>
    public readonly struct EventAppSuspend { }

    /// <summary>
    /// Emitted when the data of the singleton <typeparamref name="TOwner"/> changes.
    /// Listen with <see cref="PlayerPrefSingletonExtensions.OnDataChanged{TOwner}(Behaviour, Action)"/>
    /// or <c>EventBus&lt;DataChanged&lt;TOwner&gt;&gt;.Listen</c>.
    /// </summary>
    /// <typeparam name="TOwner">The singleton class whose data changed.</typeparam>
    public readonly struct EventDataChanged<TOwner> { }

    /// <summary>
    /// Base class for a lazily created singleton that owns persisted data (<typeparamref name="TData"/>,
    /// stored as JSON in PlayerPrefs) and reacts to timer events.
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

        // Needed when Domain Reload is disabled: the bus is cleared on Play, so a stale instance would be deaf.
        static PlayerPrefSingleton()
        {
            EventBusRegistry.Register(() => instance = null);
        }

        /// <summary>
        /// PlayerPrefs key of the saved data. Defaults to the class name; override to keep an existing key.
        /// </summary>
        public virtual string GetKeyData() => typeof(T).Name;

        /// <summary>
        /// The loaded data. Valid only after <see cref="Initialize"/>.
        /// </summary>
        protected TData cachedData;

        private bool _isInitialized;
        private bool _firstTick = true;
        private bool _isDirty;

        /// <summary>
        /// Loads (or creates) the data and starts listening to timer and app events.
        /// Safe to call more than once; only the first call has an effect.
        /// A corrupt save is kept under <c>&lt;key&gt;_corrupt</c> and replaced by new data.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            cachedData = Load();
            if (cachedData == null)
            {
                cachedData = new TData();
                cachedData.OnNewData();
                OnNewData();
                SaveData();
            }

            cachedData.FirstTimeInit();
            PlayerPrefSingletonInitQueue.Enqueue(RunInitHooks);
        }
        
        private void RunInitHooks()
        {
            EventBus<EventTimerNewSecond>.Listen(HandleNewSecond);
            EventBus<EventTimerNewDay>.Listen(HandleNewDay);
            EventBus<EventTimerNewWeek>.Listen(HandleNewWeek);
            EventBus<EventTimerNewMonth>.Listen(HandleNewMonth);
            EventBus<EventAppSuspend>.Listen(HandleSuspend);

            FirstTimeInit();
            OnInitSuccess();
        }

        private TData Load()
        {
            string key = GetKeyData();
            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<TData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PlayerPrefs.SetString(key + "_corrupt", json); // keep a copy before it is overwritten
                return null;
            }
        }

        /// <summary>
        /// Writes the data to PlayerPrefs immediately and clears the pending-save flag.
        /// Usually not needed: <see cref="OnValueChange"/> schedules a save for the next
        /// <see cref="EventTimerNewSecond"/>.
        /// </summary>
        protected void SaveData()
        {
            _isDirty = false;
            PlayerPrefs.SetString(GetKeyData(), JsonConvert.SerializeObject(cachedData));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Call after changing the data. Schedules a save (at most once per second, and on
        /// <see cref="EventAppSuspend"/>) and emits <see cref="EventDataChanged{TOwner}"/> for this singleton.
        /// </summary>
        protected virtual void OnValueChange()
        {
            _isDirty = true;
            EventBus<EventDataChanged<T>>.Emit(default);
        }

        /// <summary>
        /// Deletes the saved data from PlayerPrefs and cancels any pending save.
        /// The in-memory data is untouched.
        /// </summary>
        public void ClearData()
        {
            _isDirty = false; // otherwise a pending save would write the key back
            PlayerPrefs.DeleteKey(GetKeyData());
        }

        private void HandleNewSecond(in EventTimerNewSecond _)
        {
            if (_firstTick)
            {
                _firstTick = false;
                OnFirstTick();
            }
            OnNewSecond();
            if (_isDirty) SaveData(); // debounced save: at most once per second
        }

        private void HandleSuspend(in EventAppSuspend _)
        {
            if (_isDirty) SaveData();
        }

        private void HandleNewDay(in EventTimerNewDay _) => OnNextDay();
        private void HandleNewWeek(in EventTimerNewWeek _) => OnNextWeek();
        private void HandleNewMonth(in EventTimerNewMonth _) => OnNextMonth();

        /// <summary>
        /// Called when no save exists, after <see cref="PlayerPrefCachedData.OnNewData"/>. Fill in defaults here.
        /// </summary>
        protected virtual void OnNewData() { }

        /// <summary>
        /// Called once during <see cref="Initialize"/>, after the data is ready and before <see cref="OnInitSuccess"/>.
        /// </summary>
        protected virtual void FirstTimeInit() { }

        /// <summary>Called at the end of <see cref="Initialize"/>.</summary>
        protected virtual void OnInitSuccess() { }

        /// <summary>Called on the first <see cref="EventTimerNewSecond"/> only, before <see cref="OnNewSecond"/>.</summary>
        protected virtual void OnFirstTick() { }

        /// <summary>Called on every <see cref="EventTimerNewSecond"/>. Changes made here are saved in the same tick.</summary>
        protected virtual void OnNewSecond() { }

        /// <summary>Called on <see cref="EventTimerNewDay"/>.</summary>
        protected virtual void OnNextDay() { }

        /// <summary>Called on <see cref="EventTimerNewWeek"/>.</summary>
        protected virtual void OnNextWeek() { }

        /// <summary>Called on <see cref="EventTimerNewMonth"/>.</summary>
        protected virtual void OnNextMonth() { }
    }

    /// <summary>
    /// Base class of the data persisted by a <see cref="PlayerPrefSingleton{T, TData}"/>. Must be JSON-serializable.
    /// </summary>
    public abstract class PlayerPrefCachedData
    {
        /// <summary>Called once when no save exists. Fill in default values.</summary>
        public abstract void OnNewData();

        /// <summary>
        /// Called once per <see cref="PlayerPrefSingleton{T, TData}.Initialize"/>,
        /// after the data is loaded or created.
        /// </summary>
        public abstract void FirstTimeInit();
    }
    
    public static class PlayerPrefSingletonInitQueue
    {
        private static readonly Queue<Action> queue = new();
        private static bool _running;

        public static void Enqueue(Action runHooks)
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
                    catch (Exception ex)
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

    /// <summary>Helpers to listen to <see cref="EventDataChanged{TOwner}"/> from a Behaviour.</summary>
    public static class PlayerPrefSingletonExtensions
    {
        /// <summary>
        /// Calls <paramref name="handler"/> whenever the data of <typeparamref name="TOwner"/> changes,
        /// for the lifetime of <paramref name="behaviour"/> (active and enabled only; removed when destroyed).
        /// Call once, e.g. in <c>Awake</c>: <c>this.OnDataChanged&lt;Inventory&gt;(Refresh);</c>
        /// Allocates one small closure at registration, nothing on emit.
        /// </summary>
        /// <typeparam name="TOwner">The singleton class to observe.</typeparam>
        /// <param name="behaviour">The Behaviour that defines the listener's lifetime.</param>
        /// <param name="handler">Parameterless callback.</param>
        public static void OnDataChanged<TOwner>(this Behaviour behaviour, Action handler)
        {
            EventBus<EventDataChanged<TOwner>>.Listen((in EventDataChanged<TOwner> _) => handler(), behaviour);
        }

        /// <summary>
        /// Same as <see cref="OnDataChanged{TOwner}(Behaviour, Action)"/> but with the raw bus handler signature.
        /// </summary>
        /// <typeparam name="TOwner">The singleton class to observe.</typeparam>
        /// <param name="behaviour">The Behaviour that defines the listener's lifetime.</param>
        /// <param name="handler">Callback receiving the (empty) event.</param>
        public static void OnDataChanged<TOwner>(this Behaviour behaviour, BusHandler<EventDataChanged<TOwner>> handler)
        {
            EventBus<EventDataChanged<TOwner>>.Listen(handler, behaviour);
        }
    }
}
