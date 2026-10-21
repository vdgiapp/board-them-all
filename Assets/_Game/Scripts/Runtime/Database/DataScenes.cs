using Gustom.References;
using Sirenix.OdinSerializer;
using UnityEngine;

namespace Game.Database
{
    [CreateAssetMenu(fileName = "Data Scenes", menuName = "Game/Database/Data Scenes")]
    public class DataScenes : SerializedScriptableObject, IGameModuleData
    {
        [field: SerializeField] public SceneReference essentials { get; private set; }
        [field: SerializeField] public SceneReference loadScene { get; private set; }
        [field: SerializeField] public SceneReference mainMenu { get; private set; }
        [field: SerializeField] public SceneReference gameplay { get; private set; }
    }
}