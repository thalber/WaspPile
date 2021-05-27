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
            target.element = original.element;
            target.color = original.color;
            target.sortZ = original.sortZ;
            
        }
    }
}
