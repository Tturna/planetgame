using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UI
{
    public class InvertedMaskImage : Image {
        public override Material materialForRendering
        {
            get
            {
                // get a copy of the base material or you going to F*** up the whole project
                var materialCopy = new Material(base.materialForRendering);
                materialCopy.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
                return materialCopy;
            }
        }
    }
}