using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriatgePeces
{
    public class Peca
    {
        public string Tipus { get; set; }
        public DateTime DataDeteccio { get; set; }

        public Peca(string tipus)
        {
            Tipus = tipus;
            DataDeteccio = DateTime.Now;
        }
    }
}

