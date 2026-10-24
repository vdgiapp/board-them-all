using System;
using System.Collections.Generic;
using Game.Manager;
using Game.UI;

namespace Game
{
    public class PlayerResourcePref : PlayerPrefSingleton<PlayerResourcePref, PlayerResourceCachedData>
    {
        public override int GetCurrentVersion() => 1;
        public override string GetKeyData() => "player_resource";
        public override string GetKeyEvent() => Constant.EVENT_ON_PLAYER_RESOURCE_UPDATED;

        public long GetResource(ECommonResource type)
        {
            return cachedData.GetResource(type);
        }

        public long GetResource(EBoosterResource type)
        {
            return cachedData.GetResource(type);
        }

        public void AddResource(IList<GameResource> resources, EResourceFrom resourceFrom, Action onSuccess = null, Action onFailed = null)
        {
            if (!CanAddResource(resources))
            {
                TextDialog.ShowText("Not enough resources!", showImmediately: true);
                onFailed?.Invoke();
                return;
            }
            cachedData.AddResource(resources);
            OnValueChange();
            SaveData();
            onSuccess?.Invoke();
        }

        public bool CanAddResource(params GameResource[] resources) => CanAddResource((IList<GameResource>)resources);
        public bool CanAddResource(IList<GameResource> resources)
        {
            if (resources == null) return false;
            if (resources.Count <= 0) return false;
            foreach (GameResource resource in resources)
            {
                switch (resource)
                {
                    case CommonResource r when GetResource(r.type) + r.value < 0:
                    {
                        return false;
                    }
                    case BoosterResource r when GetResource(r.type) + r.value < 0:
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [Serializable]
    public class PlayerResourceCachedData : PlayerPrefCachedData
    {
        public Dictionary<string, long> commonResources = new()
        {
            [nameof(ECommonResource.Coin)] = 100,
            [nameof(ECommonResource.Energy)] = 5,
        };
        public Dictionary<string, long> boosterResources = new()
        {
            [nameof(EBoosterResource.UndoLastMove)] = 0,
            [nameof(EBoosterResource.PickOneColor)] = 0,
            [nameof(EBoosterResource.AddQueueSlot)] = 0,
            [nameof(EBoosterResource.AddOneVehicle)] = 0,
        };
        
        public override void OnNewData()
        {
            
        }

        public override void FirstTimeInit()
        {
            
        }
        
        public long GetResource(ECommonResource type)
        {
            return commonResources.GetValueOrDefault(type.ToString(), 0);
        }

        public long GetResource(EBoosterResource type)
        {
            return boosterResources.GetValueOrDefault(type.ToString(), 0);
        }

        public void AddResource(IList<GameResource> resources)
        {
            if (resources == null) return;
            if (resources.Count <= 0) return;
            foreach (GameResource resource in resources)
            {
                switch (resource)
                {
                    case CommonResource r:
                    {
                        commonResources.TryAdd(r.type.ToString(), 0);
                        commonResources[r.type.ToString()] += r.value;
                        break;
                    }
                    case BoosterResource r:
                    {
                        boosterResources.TryAdd(r.type.ToString(), 0);
                        boosterResources[r.type.ToString()] += r.value;
                        break;
                    }
                }
            }
        }
    }
    
    public enum EResourceFrom
    {
        
    }
}