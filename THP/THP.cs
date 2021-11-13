using Partiality.Modloader;
using System.Collections.Generic;
using System.Linq;
using System;

namespace WaspPile.playground
{
    public class THP : PartialityMod
    {
        public THP()
        {
            this.author = "thalber";
            this.Version = "0.0.0";
            this.ModID = "THP";
        }

        public override void OnEnable()
        {
            try
            {
                throw new Exception("I'm here, right at the line...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
