using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Zombie;
using Interop.QBFC13;

namespace SkarAudioQBSync
{
    class QBSync
    {
        protected bool isDebug = false;
        protected Zombie.SDKConnection cn;
        protected bool loaded = false;

        protected string FullName = "";
        protected string ListID = "";


        public QBSync(Zombie.SDKConnection cn)
        {
            this.cn = cn;
        }
        public bool getLoaded()
        {
            return this.loaded;
        }
        public string getListID()
        {
            return this.ListID;
        }
        public string getFullName()
        {
            return this.FullName;
        }
        public void setFullName(string fullName)
        {
            this.FullName = fullName;
        }
    }
}
