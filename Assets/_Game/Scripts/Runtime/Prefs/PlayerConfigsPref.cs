using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game
{
    public class PlayerConfigsPref : PlayerPrefSingleton<PlayerConfigsPref, PlayerConfigsCachedData>
    {
        public override int GetCurrentVersion() => 1;
        public override string GetKeyData() => "player_configs";
        public override string GetKeyEvent() => Constant.EVENT_ON_PLAYER_CONFIGS_UPDATED;

        public void SetConfig(EPlayerConfig config, int value)
        {
            cachedData.SetConfig(config, (int)value);
            OnValueChange();
        }
        
        public void SetConfig(EPlayerConfig config, float value)
        {
            cachedData.SetConfig(config, value);
            OnValueChange();
        }
        
        public void SetConfig(EPlayerConfig config, bool value)
        {
            cachedData.SetConfig(config, value ? 1f : 0f);
            OnValueChange();
        }

        public int GetConfigInt(EPlayerConfig config)
        {
            return (int)cachedData.GetConfig(config);
        }
        
        public float GetConfigFloat(EPlayerConfig config)
        {
            return cachedData.GetConfig(config);
        }

        public bool GetConfigBool(EPlayerConfig config)
        {
            return cachedData.GetConfig(config) >= 1;
        }
    }

    [Serializable]
    public class PlayerConfigsCachedData : PlayerPrefCachedData
    {
        public Dictionary<string, float> playerConfigs = new()
        {
            [nameof(EPlayerConfig.MusicEnabled)] = 1f,
            [nameof(EPlayerConfig.SoundEnabled)] = 1f,
            [nameof(EPlayerConfig.HapticEnabled)] = 1f,
        };

        public override void OnNewData()
        {
            
        }

        public override void FirstTimeInit()
        {
        }

        public void SetConfig(EPlayerConfig config, float value)
        {
            playerConfigs[config.ToString()] = value;
        }

        public float GetConfig(EPlayerConfig config)
        {
            return playerConfigs[config.ToString()];
        }
    }

    public enum EPlayerConfig
    {
        MusicEnabled,
        SoundEnabled,
        HapticEnabled,
    }
}