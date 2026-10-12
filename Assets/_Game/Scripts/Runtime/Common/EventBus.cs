using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Handler for an event of type <typeparamref name="T"/>. The event is passed by <c>in</c>
    /// (read-only reference) so no copy of the struct is made.
    /// </summary>
    /// <typeparam name="T">The event struct type.</typeparam>
    /// <param name="e">The emitted event.</param>
    public delegate void BusHandler<T>(in T e) where T : struct;

    /// <summary>
    /// Zero-GC static event bus, one independent channel per event struct type. Main thread only.
    /// <list type="bullet">
    /// <item><description><b>Bound</b> listener (a <see cref="Behaviour"/> is given as lifetime): runs only while that
    /// Behaviour is active and enabled, and is dropped automatically once it is destroyed.</description></item>
    /// <item><description><b>Unbound</b> listener (plain C# class, no lifetime): runs until the returned
    /// <see cref="ListenHandle{T}"/> is disposed.</description></item>
    /// </list>
    /// All listeners are cleared when entering Play Mode, even with Domain Reload disabled.
    /// </summary>
    /// <typeparam name="T">The event struct type.</typeparam>
    public static class EventBus<T> where T : struct
    {
        private struct Entry
        {
            public BusHandler<T> handler; 
            public Behaviour lifetime; 
            public bool bound;
        }

        private static Entry[] entries = new Entry[8];
        private static int count;
        private static int emitting; // >0 while inside Emit (supports nested Emit)
        private static bool dirty; // tombstones to compact after emitting ends

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        /// <summary>
        /// Registers a listener for <typeparamref name="T"/>.
        /// Allocates only when the internal array has to grow; cache nothing, call it once per listener.
        /// </summary>
        /// <param name="handler">Called on every <see cref="Emit"/>. Cannot be null.</param>
        /// <param name="lifetime">
        /// Optional. When given, the handler runs only while this Behaviour is active and enabled and is removed
        /// automatically after it is destroyed. When null, the listener stays until the returned handle is disposed.
        /// </param>
        /// <returns>A handle to stop listening. Only needed for unbound listeners (no <paramref name="lifetime"/>).</returns>
        public static ListenHandle<T> Listen(BusHandler<T> handler, Behaviour lifetime = null)
        {
            if (count == entries.Length) Array.Resize(ref entries, count * 2);
            entries[count++] = new Entry
            {
                handler = handler,
                lifetime = lifetime,
                bound = (object)lifetime != null, // reference check, not Unity's overloaded ==
            };
            return new ListenHandle<T>(handler);
        }

        /// <summary>
        /// Removes the first listener registered with <paramref name="handler"/>. Safe to call during <see cref="Emit"/>.
        /// </summary>
        public static void Unlisten(BusHandler<T> handler)
        {
            for (int i = 0; i < count; i++)
            {
                if (entries[i].handler != handler) continue;
                if (emitting > 0) { entries[i] = default; dirty = true; }
                else
                {
                    count--;
                    Array.Copy(entries, i + 1, entries, i, count - i);
                    entries[count] = default;
                }
                return;
            }
        }

        /// <summary>
        /// Invokes all listeners of <typeparamref name="T"/> in registration order. Zero allocation.
        /// Listeners added during the emit run from the next emit on. An exception in one handler
        /// is logged and does not stop the others. Nested emits are supported.
        /// </summary>
        /// <param name="e">The event to deliver.</param>
        public static void Emit(in T e)
        {
            int n = count; // snapshot: listeners added during Emit run from the next one
            emitting++;
            for (int i = 0; i < n; i++)
            {
                Entry en = entries[i];
                if (en.handler == null) continue; // tombstone
                if (en.bound)
                {
                    if (!en.lifetime) // destroyed
                    {
                        entries[i] = default;
                        dirty = true; 
                        continue;
                    }
                    if (!en.lifetime.isActiveAndEnabled) continue; // disabled: skip, keep
                }
                try
                {
                    en.handler(e);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
            if (--emitting == 0 && dirty)
            {
                Compact();
            }
        }

        private static void Compact()
        {
            int w = 0;
            for (int r = 0; r < count; r++)
            {
                if (entries[r].handler != null)
                {
                    entries[w++] = entries[r];
                }
            }
            Array.Clear(entries, w, count - w);
            count = w;
            dirty = false;
        }

        public static void Clear()
        {
            Array.Clear(entries, 0, count);
            count = 0; emitting = 0; dirty = false;
        }
    }

    /// <summary>
    /// Zero-allocation handle returned by <see cref="EventBus{T}.Listen"/>.
    /// Only needed to stop an unbound (plain C#) listener; bound listeners clean up by themselves.
    /// </summary>
    /// <typeparam name="T">The event struct type.</typeparam>
    public readonly struct ListenHandle<T> : IDisposable where T : struct
    {
        private readonly BusHandler<T> handler;
        
        public ListenHandle(BusHandler<T> h)
        {
            handler = h;
        }

        /// <summary>
        /// Stops listening. Calling it again, or after the bus was cleared, does nothing.
        /// </summary>
        public void Dispose()
        {
            EventBus<T>.Unlisten(handler);
        }
    }

    /// <summary>
    /// Extension methods to listen to <see cref="EventBus{T}"/> from a Behaviour.
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// Listens to <typeparamref name="T"/> for the lifetime of <paramref name="owner"/>
        /// (active and enabled only; removed when destroyed). Call once, e.g. in <c>Awake</c>.
        /// The type argument must be written explicitly: <c>this.Listen&lt;DamageEvent&gt;(OnDamage);</c>
        /// </summary>
        /// <typeparam name="T">The event struct type.</typeparam>
        /// <param name="owner">The Behaviour that defines the listener's lifetime.</param>
        /// <param name="handler">Called on every emit of <typeparamref name="T"/>.</param>
        public static void Listen<T>(this Behaviour owner, BusHandler<T> handler) where T : struct
        {
            EventBus<T>.Listen(handler, owner);
        }
    }

    /// <summary>
    /// Clears every <see cref="EventBus{T}"/> (and anything else registered) when entering Play Mode.
    /// </summary>
    static class EventBusRegistry
    {
        private static readonly List<Action> clears = new();

        /// <summary>
        /// Registers an action to run once per Play Mode start, before any scene loads.
        /// </summary>
        public static void Register(Action clear) => clears.Add(clear);

        // Runs even when Domain Reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            for (int i = 0; i < clears.Count; i++) clears[i]();
        }
    }
}