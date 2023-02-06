using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkarAudioQBSync
{
    class Settings
    {
        private string enabled;
        private string minOrderDate;

        public Settings(string enabled, string date)
        {
            this.enabled = enabled;
            this.minOrderDate = date;
        }

        public string getEnabled()
        {
            return this.enabled;
        }
        public string getMinOrderDate()
        {
            return this.minOrderDate;
        }
    }
}
