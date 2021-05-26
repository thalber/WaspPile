using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaspPile.EyeIntheSky.Rulesets
{
    public class InitRuleset
    {
        public InitRuleset(Type target, Dictionary<int, FSprite> ExampleSprites, int? extendArray = null)
        {
            targetType = target;
            spriteReplacements = ExampleSprites;
            additionalSlots = extendArray;
        }
        public Type targetType;
        public Dictionary<int, FSprite> spriteReplacements;
        public int? additionalSlots = 0;
    }
}
