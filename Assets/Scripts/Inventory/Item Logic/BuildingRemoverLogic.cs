using Cameras;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class BuildingRemoverLogic : ItemLogicBase
    {
        private int buildingBoundsMask;
        private Camera mainCam;
        
        public override bool UseOnce(UseParameters useParameters)
        {
            if (buildingBoundsMask == default)
            {
                buildingBoundsMask = 1 << LayerMask.NameToLayer("BuildingBounds");
            }

            if (mainCam == default)
            {
                mainCam = CameraController.instance.mainCam;
            }
            
            var mousePoint = mainCam.ScreenToWorldPoint(Input.mousePosition);
            
            var hit = Physics2D.OverlapPoint(mousePoint, buildingBoundsMask);

            if (!hit) return true;
            
            var roomModule = hit.transform.root.GetComponent<RoomModule>();
            var roomModuleItemSo = roomModule.itemSo;
            var roomPosition = roomModule.transform.position;
            
            Object.Destroy(roomModule.gameObject);

            var item = new Item(roomModuleItemSo);
            InventoryManager.SpawnItem(item, roomPosition);
            
            return true;
        }

        public override bool UseContinuous(UseParameters useParameters)
        {
            return false;
        }

        public override bool UseOnceSecondary(UseParameters useParameters)
        {
            return false;
        }

        public override bool UseContinuousSecondary(UseParameters useParameters)
        {
            return false;
        }
    }
}
