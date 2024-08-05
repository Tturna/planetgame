using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    // ReSharper disable once InconsistentNaming
    public class TMPUGUIFontMaterialSetter : MonoBehaviour
    {
        [SerializeField] private Material material;

        private void Awake()
        {
            GetComponent<TextMeshProUGUI>().fontSharedMaterial = material;
        }
    }
}
