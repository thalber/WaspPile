using Partiality.Modloader;
using System.Collections.Generic;
using System.Linq;


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

        private List<string> resnames;

        public override void OnEnable()
        {
            base.OnEnable();
            resnames = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();
            resnames.Add(Properties.Resources.SecondString);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(RWCustom.Custom.RootFolderDirectory(), "RESNAMES.txt"), resnames.ToArray());
        }
    }
}
