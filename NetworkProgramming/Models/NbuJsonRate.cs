using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;

namespace NetworkProgramming.Models
{
    // модель для JSON данных курсов от НБУ
    internal class NbuJsonRate
    {
        public Int16 r030 { get; set; }              // short
        public String txt { get; set; }
        public Single rate { get; set; }             // float
        public String cc { get; set; }
        public String exchangedate { get; set; }
    }    
}


