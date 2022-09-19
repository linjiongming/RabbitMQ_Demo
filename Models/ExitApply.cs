using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ExitApply
    {
        private decimal? _amount;

        public string CarNumber { get; set; }
        public DateTime EnterTime { get; set; }
        public DateTime ExitTime { get; set; }
        public TimeSpan Duration => ExitTime - EnterTime;
        public decimal Amount
        {
            get
            {
                if (_amount == null)
                {
                    if (Duration.TotalMinutes < 30)
                    {
                        _amount = 0;
                    }
                    else if (Duration.TotalHours <= 1)
                    {
                        _amount = 5;
                    }
                    else
                    {
                        int hours = (int)Math.Ceiling(Duration.TotalHours) - 1;
                        _amount = 5 + hours * 0.5m;
                    }
                }
                return _amount ?? 0;
            }
        }

        public override string ToString()
        {
            return $"车辆({CarNumber})于{ExitTime:yyyy年M月d日HH:mm}离开停车场，停车时长{Duration.TotalMinutes}分钟，停车费用{Amount:0.0}元";
        }
    }
}
