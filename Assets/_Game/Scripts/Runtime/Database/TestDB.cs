using NaughtyAttributes;
using UnityEngine;

namespace Game.Database
{
    [CreateAssetMenu(fileName = "TestDB", menuName = "Game/Database/TestDB")]
    public class TestDB : ScriptableObject, IGameModuleData
    {
        [SerializeField] private string testString;

        [Button("Log testString from GameDatabase")]
        private void LogTestString()
        {
            Debug.Log(GameDatabase.GetModule<TestDB>().testString);
        }
    }
}