using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaspPile.TongueGrinder
{
    public class LGroup : IEnumerable<LObject>
    {
        private List<LObject> myObjects;

        public IEnumerator<LObject> GetEnumerator()
        {
            return ((IEnumerable<LObject>)myObjects).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)myObjects).GetEnumerator();
        }
    }
}
