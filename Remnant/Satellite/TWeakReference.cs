using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.Remnant.Satellite
{
    internal class TWeakReference <tt>
        where tt : class
    {
        internal TWeakReference(tt tar)
        {
            __wr = new WeakReference(tar);
        }
        private WeakReference __wr;
        internal tt Target => __wr?.Target as tt;
    }
}
