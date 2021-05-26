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
        public static object ShallowClone(this object o)
        {
            var tp = typeof(FSprite);
            ConstructorInfo chosenCtor = null;
            foreach (var ctor in tp.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (ctor.GetParameters().Length == 0 && !ctor.IsStatic) chosenCtor = ctor;
            }
            if (chosenCtor == null) throw new ArgumentException("suiting parameterless constructor not found");
            object res = chosenCtor.Invoke(new object[0]);
            foreach (var fld in tp.GetFields())
            {
                fld.SetValue(res, fld.GetValue(o));
            }
            return res;
        }
    }
}
