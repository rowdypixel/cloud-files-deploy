using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudFiles_Deploy
{
    class MD5Result
    {
        public string MD5 { get; set; }
        public string LocalPath { get; set; }
        public string CloudPath { get; set; }

        public override bool Equals(object obj)
        {
            MD5Result theObject = obj as MD5Result;
            if (theObject == null)
                return base.Equals(obj);
            else
                return (theObject.CloudPath.ToLower() == this.CloudPath.ToLower() && theObject.MD5.ToLower() == this.MD5.ToLower());
        }
    }
}
