using Entities;
using ProcGen;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null)
        {
            // TODO: Figure out a sensible system for melee combos
            player.RecoilAnimator.SetBool("swinging", true);
            player.RecoilAnimator.SetTrigger("attackLongMelee");
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
