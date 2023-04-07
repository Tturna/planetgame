using Entities;
using ProcGen;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        private Animator _recoilAnimator;
        private ItemAnimationManager _itemAnimationManager;
        
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null)
        {
            _recoilAnimator ??= player.RecoilAnimator;
            _itemAnimationManager ??= player.ItemAnimationManager;
            
            _itemAnimationManager.AttackMelee(_recoilAnimator, "attackLongMelee");
            
            return true;
        }

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null) => false;

        public override bool UseOnceSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null)
        {
            throw new System.NotImplementedException();
        }

        public override bool UseContinuousSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null) => false;
    }
}
