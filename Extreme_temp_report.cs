using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Temp_listener_forms
{
    public class Extreme_temp_report
    {
        public string key;
        public double value;
        public DateTime when;
        public string sensor;
        public double diff;

        public Extreme_temp_report(string k) {

               key = k;
        }

        public Extreme_temp_report(string k,double v,DateTime w,string s,double d)
        {

            key = k;
            value = v;
            when = w;
            sensor = s; 
            diff = d;
        }

        public string get_string()
        {
            StringBuilder sb = new StringBuilder(key);
            sb.Append(",");
           
            sb.Append(when);
            sb.Append(",");
            sb.Append(sensor);
            sb.Append(",");
            sb.Append(value);
            
            //sb.Append(",");
            //sb.Append(diff);

            return sb.ToString();

        }
    }
}
