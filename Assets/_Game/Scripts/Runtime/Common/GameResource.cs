using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public abstract class GameResource
    {
        public static GameResource Deserialize(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogError("Null or empty parse resource input.");
                return null;
            }

            string[] resourceData = input.Split('|');
            if (resourceData.Length < 1)
            {
                Debug.LogError($"Invalid resource format (split length < 1): {input}");
                return null;
            }

            switch (resourceData[0])
            {
                case "CommonResource" when resourceData[1].TryToEnum(out ECommonResource resourceType):
                {
                    if (!long.TryParse(resourceData[2], out long resourceValue))
                    {
                        Debug.LogError($"Invalid Resource Value format '{resourceData[2]}' in: {input}");
                        return null;
                    }
                    return new CommonResource(resourceType, resourceValue);
                }
                case "BoosterResource" when resourceData[1].TryToEnum(out EBoosterResource boosterType):
                {
                    if (!long.TryParse(resourceData[2], out long resourceValue))
                    {
                        Debug.LogError($"Invalid Resource Value format '{resourceData[2]}' in: {input}");
                        return null;
                    }
                    return new BoosterResource(boosterType, resourceValue);
                }
                default:
                {
                    Debug.LogError($"Null parse resource or unknown type: {input}");
                    return null;
                }
            }
        }
        
        public abstract bool TypeEquals(GameResource other);
        public abstract string Serialize();
    }

    public class PackageResource
    {
        public readonly List<GameResource> resources = new();
        
        public PackageResource() { }
        public PackageResource(List<GameResource> resources)
        {
            AddResource(resources);
        }
        
        public PackageResource(params GameResource[] resources)
        {
            AddResource(resources);
        }

        public void AddResource(PackageResource pack) => AddResource(pack.resources);
        public void AddResource(params GameResource[] resource) => AddResource((IList<GameResource>)resource);
        public void AddResource(IList<GameResource> resource)
        {
            if (resource == null) return;
            if (resource.Count <= 0) return;
            foreach (GameResource newResource in resource)
            {
                foreach (GameResource existingItem in resources)
                {
                    if (existingItem.TypeEquals(newResource) && existingItem is ItemResource existingItemRes &&
                        newResource is ItemResource newItemRes)
                    {
                        existingItemRes.value += newItemRes.value;
                        break;
                    }
                }
                resources.Add(newResource);
            }
        }

        public long GetResource(ECommonResource type)
        {
            foreach (GameResource item in resources)
            {
                if (item is CommonResource res && res.type == type)
                {
                    return res.value;
                }
            }
            return 0;
        }
    }

    public abstract class ItemResource : GameResource
    {
        public long value;
    }
    
    public enum ECommonResource
    {
        Coin,
        Energy,
    }
    public class CommonResource : ItemResource
    {
        public ECommonResource type;
        
        public CommonResource() { }
        public CommonResource(ECommonResource type, long value)
        {
            this.type = type;
            this.value = value;
        }
        
        public override bool TypeEquals(GameResource source)
        {
            if (source is CommonResource common) return common.type == type;
            return false;
        }

        public override string Serialize() => $"CommonResource|{type}|{value}";
    }
    
    public enum EBoosterResource
    {
        UndoLastMove,
        PickOneColor,
        AddQueueSlot,
        AddOneVehicle,
    }
    public class BoosterResource : ItemResource
    {
        public EBoosterResource type;
        
        public BoosterResource() { }
        public BoosterResource(EBoosterResource type, long value)
        {
            this.type = type;
            this.value = value;
        }

        public override bool TypeEquals(GameResource source)
        {
            if (source is BoosterResource common) return common.type == type;
            return false;
        }

        public override string Serialize() => $"BoosterResource|{type}|{value}";
    }
}