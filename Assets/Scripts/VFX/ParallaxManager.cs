using Entities;
using Unity.Mathematics;
using UnityEngine;

namespace VFX
{
    public class ParallaxManager : MonoBehaviour
    {
        [SerializeField] private GameObject fgParent, mgParent, bgParent;
        private PlayerController _player;
        private float _oldZ;

        private void Start()
        {
            _player = PlayerController.instance;
        }

        // Update is called once per frame
        private void Update()
        {
            // Rotate all the layers depending on player position relative to the planet
            
            var rotation = _player.transform.rotation;
            // var eRotation = _player.transform.eulerAngles;
            // print($"Euler Z: {eRotation.z}");
            // print($"Quaternion Z: {rotation.z}");

            // var diff = Quaternion.Euler(0f, 0f, rotation.z - _oldZ);
            // _oldZ = rotation.z;

            fgParent.transform.rotation = Quaternion.Lerp(quaternion.identity, rotation, .1f);
            mgParent.transform.rotation = Quaternion.Lerp(quaternion.identity, rotation, .25f);
            bgParent.transform.rotation = Quaternion.Lerp(quaternion.identity, rotation, .4f);
        }
    }
}
