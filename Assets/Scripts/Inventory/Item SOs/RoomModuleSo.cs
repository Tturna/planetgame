using UnityEngine;

namespace Inventory.Item_SOs
{
    [CreateAssetMenu(fileName = "RoomModule", menuName = "SO/RoomModule")]
    public class RoomModuleSo : PlaceableSo
    {
        public int prefabIndex;
        public float verticalSpawnOffset;
    }
}
