using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WaspPile.TongueGrinder.StaticUtils;

namespace WaspPile.TongueGrinder
{
    public class LObject : LNode, IEnumerable<LNode>
    {
        

        public LObject(string text)
        {
#warning redo
            var pr = new LObjectParser(this, text);
        }

        private enum parsemode
        {
            ReadingPropName,
            ReadingChildCode,
            Cruise
        }

        private HashSet<LNode> myProps;

        public IEnumerator<LNode> GetEnumerator()
        {
            return ((IEnumerable<LNode>)myProps).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)myProps).GetEnumerator();
        }

        internal class LObjectParser : LNodeParser
        {
            internal LObjectParser(LObject pr, string text) : base(pr, text)
            {
                
            }
            LObject LO => (LObject)Owner;
            internal override void Update()
            {
                base.Update();
            }
            
            internal override bool Ready => false;

        }
    }
}
