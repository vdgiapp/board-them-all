using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game
{
    [DisallowMultipleComponent]
    public class ObjectPooler : MonoBehaviourSingleton<ObjectPooler>
    {
        /// <summary>
        /// delay for clear pool (secs)
        /// </summary>
        public float delayClear = 600;

        Dictionary<string, Pooling> dicPooling;
        Dictionary<string, float> dicCountDown;
        
        // Dictionary<EParticleEffectType, ParticleSystem> dicEffectInUse;

        protected void Start()
        {
            dicPooling = new Dictionary<string, Pooling>();
            dicCountDown = new Dictionary<string, float>();
            // dicEffectInUse = new Dictionary<EParticleEffectType, ParticleSystem>();
            TigerForge.EventManager.StartListening(Constant.EVENT_ON_NEW_SECOND, OnNextSecond);
            Update();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            TigerForge.EventManager.StopListening(Constant.EVENT_ON_NEW_SECOND, OnNextSecond);
        }

        private void OnNextSecond()
        {
            var keys = dicPooling.Keys.ToList();
            foreach (var key in keys)
            {
                dicCountDown[key] -= 1;
                if (dicCountDown[key] <= 0)
                    ClearPool(key);
            }
        }

        private void Update()
        {
            foreach (var item in dicPooling)
            {
                item.Value.OnUpdate();
            }
        }

        public PoolingObject GetObject(PoolingObject _refPrefabs)
        {
            if (!dicPooling.ContainsKey(_refPrefabs.poolingKey))
            {
                dicPooling.Add(_refPrefabs.poolingKey, new Pooling(_refPrefabs));
                dicCountDown.Add(_refPrefabs.poolingKey, delayClear);
            }

            dicCountDown[_refPrefabs.poolingKey] = delayClear;
            return dicPooling[_refPrefabs.poolingKey].GetObject();
        }

        public void DespawnObj(PoolingObject obj)
        {
            if (dicPooling.ContainsKey(obj.poolingKey))
                dicPooling[obj.poolingKey].SetObject(obj);
            else
                Destroy(obj);
        }

        public void ClearAll()
        {
            foreach (var item in dicPooling)
            {
                item.Value.ClearPool();
            }

            dicPooling.Clear();
            dicCountDown.Clear();
        }

        public void ClearPool(string key)
        {
            if (dicPooling.ContainsKey(key))
            {
                dicPooling[key].ClearPool();
                dicPooling.Remove(key);
                dicCountDown.Remove(key);
            }
        }
        
        // public void PlayGameFx(EParticleEffectType fxType, Vector3 pos, float scale = 1, float rotation = 0,
        //     int emit = 1)
        // {
        //     if (fxType == EParticleEffectType.None)
        //         return;
        //     if (!dicEffectInUse.ContainsKey(fxType))
        //         dicEffectInUse.Add(fxType,
        //             Instantiate(DataSystem.Instance.dataAnimationEffect.GetParticleSystem(fxType), this.transform));
        //     ParticleSystem fx = dicEffectInUse[fxType];
        //     fx.transform.position = pos;
        //     for (int i = 0; i < emit; i++)
        //     {
        //         ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        //         emitParams.startSize = scale;
        //         emitParams.rotation = rotation;
        //         fx.Emit(emitParams, 1);
        //     }
        // }

        public static T Spawn<T>(T refPrefabs, Transform parent = null) where T : PoolingObject
        {
            T obj = Instance.GetObject(refPrefabs) as T;
            obj.transform.SetParent(parent, true);
            obj.transform.localPosition = Vector3.zero;
            obj.OnSpawn();
            return obj;
        }

        public static T Spawn<T>(T refPrefabs, Vector3 pos, Transform parent = null) where T : PoolingObject
        {
            T obj = Spawn(refPrefabs, parent);
            obj.transform.position = pos;
            return obj;
        }

        public static void Despawn<T>(T obj) where T : PoolingObject
        {
            Instance?.DespawnObj(obj);
        }
    }
    
    public class PoolingObject : MonoBehaviour
    {
        public string poolingKey;
        public System.Action _OnDespawn, _OnSpawn;
        protected bool activing;

        public virtual void OnSpawn()
        {
            gameObject.SetActive(true);
            activing = true;
            _OnSpawn?.Invoke();
        }

        public virtual void Despawn()
        {
            activing = false;
            gameObject.SetActive(false);
            ObjectPooler.Despawn(this);
            _OnDespawn?.Invoke();
        }

        protected virtual IEnumerator IEDespawn(float time)
        {
            yield return CoroutineUtils.Wait(time);
            Despawn();
        }

        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            transform.DOKill();
        }
    }
    
    public class Pooling
    {
        public Queue<PoolingObject> pool;
        List<PoolingObject> cachedObj;
        PoolingObject refPrefabs;

        public Pooling(PoolingObject _refPrefabs)
        {
            refPrefabs = _refPrefabs;
            pool = new Queue<PoolingObject>();
            cachedObj = new List<PoolingObject>();
        }

        public PoolingObject GetObject()
        {
            PoolingObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
                //DebugUtils.LogColor($"Pooling Object Get {obj.name}");
            }
            else
            {
                obj = Object.Instantiate(refPrefabs);
                //DebugUtils.LogColor($"Instantiate Object {obj.name}");
            }

            return obj;
        }

        public void SetObject(PoolingObject obj)
        {
            cachedObj.Add(obj);
            //DebugUtils.LogColor($"Pooling Object Set {obj.name}");
        }

        public void OnUpdate()
        {
            foreach (PoolingObject obj in cachedObj)
            {
                if (!pool.Contains(obj))
                    pool.Enqueue(obj);
            }

            cachedObj.Clear();
        }

        public void ClearPool()
        {
            while (pool.Count > 0)
            {
                PoolingObject o = pool.Dequeue();
                Object.Destroy(o);
            }
        }
    }
}