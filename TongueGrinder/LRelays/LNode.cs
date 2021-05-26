using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WaspPile.TongueGrinder
{
    public class LNode
    {
        public static LNode Parse(string text)
        {
            LNode result = null;
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] == ' ') continue;
                else if (text[i] == '#') { result = new LObject(text); break; }
                else { result = new LArray(text); break; }
            }
            return result;
        }

        abstract internal class LNodeParser : IDisposable
        {
            internal LNodeParser(LNode pr, string text)
            {
                itx = new StringReader(text);
                Owner = pr;
            }

            internal LNode Owner;
            virtual internal void Update()
            {
                lifetime++;
            }
            internal int lifetime = 0;
            
            public void Dispose()
            {
                itx?.Dispose();
            }

            public StringReader itx;
            abstract internal bool Ready { get; }
        }


    }
}
