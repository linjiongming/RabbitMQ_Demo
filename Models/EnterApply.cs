using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class EnterApply
    {
        public string CarNumber { get; set; }
        public DateTime EnterTime { get; set; }
        public override string ToString()
        {
            return $"车辆({CarNumber})于{EnterTime:yyyy年M月d日HH:mm}进入停车场";
        }
    }
}
