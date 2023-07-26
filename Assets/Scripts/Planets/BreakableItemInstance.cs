using UnityEngine;
using UnityEngine.Serialization;

namespace Planets
{
    public class BreakableItemInstance : MonoBehaviour
    {
        [FormerlySerializedAs("oreSo")] public ScriptableObject itemSo;
        [FormerlySerializedAs("oreToughness")] public int toughness;
    }
}
