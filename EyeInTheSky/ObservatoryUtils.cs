using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace WaspPile.EyeIntheSky
{
    public static class ObservatoryUtils
    {
#warning rough, combusts when parameterless constructor is not present
        public static void CopyPropertiesToOther(this FSprite original, FSprite target)
        {
            target.scaleX = original.scaleX;
            target.scaleY = original.scaleY;
            target.element = Futile.atlasManager.GetElementWithName(original.element.name);
            target.color = original.color;
            target.sortZ = original.sortZ;
        }
        public static FSprite Clone(this FSprite orig)
        {
            var res = new FSprite(orig.element, orig._facetTypeQuad);
            res.scaleX = orig.scaleX;
            res.scaleY = orig.scaleY;
            res.rotation = orig.rotation;
            res.sortZ = orig.sortZ;
            res.shader = orig.shader;
            return res;
        }
    }
}
