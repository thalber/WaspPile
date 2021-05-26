using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.EyeIntheSky
{
    public class Toopl<T1, T2>
    {
        public Toopl(T1 first, T2 second) { item1 = first; item2 = second; }
        public T1 item1 { get; set; }
        public T2 item2 { get; set; }
    }
    public class Toopl<T1, T2, T3>
    {
        public Toopl(T1 first, T2 second, T3 third) { item1 = first; item2 = second; item3 = third; }
        public T1 item1 { get; set; }
        public T2 item2 { get; set; }
        public T3 item3 { get; set; }
    }
}
