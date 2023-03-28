using Entities;
using ProcGen;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet = null, PlayerController player = null)
        {
            // TODO: Implement melee swing attack
            // Archvale style?
            // I guess add a function to the player controller script that controls the hand animator and
            // call that from here
            throw new System.NotImplementedException();
        }

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet = null, PlayerController player = null) => false;

        public override bool UseOnceSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet = null, PlayerController player = null)
        {
            throw new System.NotImplementedException();
        }

        public override bool UseContinuousSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet = null, PlayerController player = null) => false;
    }
}
