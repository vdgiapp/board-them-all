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
        
        private Coroutine oneSecondLoopCoroutine;
        private bool breakOneSecondLoop = false;

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(IEInitialize());
            StartOneSecondLoop();
            
            IEnumerator IEInitialize()
            {
                DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);
                yield return null;
                _ = GameDatabase.Instance;
                
                yield return null;
                Initialized = true;
            }
        }

        public void StartOneSecondLoop()
        {
            if (oneSecondLoopCoroutine != null)
            {
                StopCoroutine(oneSecondLoopCoroutine);
                oneSecondLoopCoroutine = null;
            }
            if (!breakOneSecondLoop) oneSecondLoopCoroutine = StartCoroutine(IEOneSecondLoop());
            else Debug.LogWarning($"{nameof(breakOneSecondLoop)} is enabled, you needs to turn it off to run method {nameof(StartOneSecondLoop)}");
        }
        
        private IEnumerator IEOneSecondLoop()
        {
            while (!breakOneSecondLoop)
            {
                yield return CoroutineUtils.WaitRealtime(1f);
                if (Initialized) TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_NEW_SECOND);
            }
            if (oneSecondLoopCoroutine != null)
            {
                StopCoroutine(oneSecondLoopCoroutine);
                oneSecondLoopCoroutine = null;
            }
        }
    }
}