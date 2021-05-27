using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.EyeIntheSky
{
    public class FSpriteClonable : FSprite, ICloneable
    {
        public FSpriteClonable() : base() { }
        public FSpriteClonable(string elementName, bool quadType = true) : base(elementName, quadType) { }

        public object Clone()
        {
            var res = new FSpriteClonable();
            res._atlas = this._atlas;
            res._element = this._element;
            
            res._scaleX = this._scaleX;
            res._scaleY = this._scaleY;
            res._color = this._color;
            res._alpha = this._alpha;
            res._anchorX = this._anchorX;
            res._anchorY = this._anchorY;
            return res;
        }
    }
}
