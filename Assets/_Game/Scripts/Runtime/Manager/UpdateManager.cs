using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public interface IUpdatable
    {
        void Update(float deltaTime);
    }

    public interface IFixedUpdatable
    {
        void FixedUpdate(float fixedDeltaTime);
    }

    public interface ILateUpdatable
    {
        void LateUpdate(float deltaTime);
    }

    public class UpdateManager : MonoBehaviour
    {
        private readonly List<IUpdatable> _updatableList = new();
        private readonly List<IFixedUpdatable> _fixedUpdatableList = new();
        private readonly List<ILateUpdatable> _lateUpdatableList = new();
        
        private readonly HashSet<IUpdatable> _updatableSet = new();
        private readonly HashSet<IFixedUpdatable> _fixedUpdatableSet = new();
        private readonly HashSet<ILateUpdatable> _lateUpdatableSet = new();

#region Register
        public void Register(IUpdatable target)
        {
            if (_updatableSet.Add(target))
            {
                _updatableList.Add(target);
            }
        }
        
        public void Register(IFixedUpdatable target)
        {
            if (_fixedUpdatableSet.Add(target))
            {
                _fixedUpdatableList.Add(target);
            }
        }
        
        public void Register(ILateUpdatable target)
        {
            if (_lateUpdatableSet.Add(target))
            {
                _lateUpdatableList.Add(target);
            }
        }
#endregion

#region Unregister
        public void Unregister(IUpdatable target)
        {
            if (_updatableSet.Remove(target))
            {
                _updatableList.Remove(target);
            }
        }
        
        public void Unregister(IFixedUpdatable target)
        {
            if (_fixedUpdatableSet.Remove(target))
            {
                _fixedUpdatableList.Remove(target);
            }
        }
        
        public void Unregister(ILateUpdatable target)
        {
            if (_lateUpdatableSet.Remove(target))
            {
                _lateUpdatableList.Remove(target);
            }
        }
#endregion

#region Loops
        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _updatableList.Count - 1; i >= 0; i--)
            {
                _updatableList[i].Update(dt);
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = _fixedUpdatableList.Count - 1; i >= 0; i--)
            {
                _fixedUpdatableList[i].FixedUpdate(dt);
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = _lateUpdatableList.Count - 1; i >= 0; i--)
            {
                _lateUpdatableList[i].LateUpdate(dt);
            }
        }
#endregion
    }
}

