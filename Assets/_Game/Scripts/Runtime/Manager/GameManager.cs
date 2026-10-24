using System;
using System.Collections;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Manager
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        [field: Header("Game state")]
        [field: SerializeField, ReadOnly] public bool Initialized { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private int tweenersCapacity = 1000;
        [SerializeField] private int sequencesCapacity = 250;
        
        public bool breakOneSecondLoop { get; set; } = false;
        
        private Coroutine _oneSecondLoopCoroutine;

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(IEInitialize());
            StartOneSecondLoop();
        }
        
        private void OnApplicationPause(bool paused)
        {
            if (paused) TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_APP_SUSPEND);
        }

        private void OnApplicationQuit()
        {
            TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_APP_SUSPEND);
        }

        private IEnumerator IEInitialize()
        {
            DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);
            yield return null;
            _ = GameDatabase.Instance;
            _ = CachedTimerPref.Instance;
            _ = PlayerConfigsPref.Instance;
            _ = PlayerResourcePref.Instance;
            yield return null;
            Initialized = true;
        }
        
        public void StartOneSecondLoop()
        {
            if (_oneSecondLoopCoroutine != null)
            {
                StopCoroutine(_oneSecondLoopCoroutine);
                _oneSecondLoopCoroutine = null;
            }
            if (!breakOneSecondLoop) _oneSecondLoopCoroutine = StartCoroutine(IEOneSecondLoop());
        }
        
        private IEnumerator IEOneSecondLoop()
        {
            while (!breakOneSecondLoop)
            {
                yield return CoroutineUtils.WaitRealtime(1f);
                if (Initialized) TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_NEW_SECOND);
            }
            if (_oneSecondLoopCoroutine != null)
            {
                StopCoroutine(_oneSecondLoopCoroutine);
                _oneSecondLoopCoroutine = null;
            }
        }
    }
}