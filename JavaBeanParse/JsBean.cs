using System.Collections.Generic;

namespace iMobie.Social.WhatsApp.JavaBeanParse
{
    public class JsBean
    {
        public string fieId { get; set; }
        public object data { get; set; }
        public int fieIdsCount { get; set; }
        public int firstObjIndex { get; set; }
        public int flags { get; set; }
        public string name { get; set; }
        public int numObjFields { get; set; }
        public byte[] primData { get; set; }
        public int primDataSize { get; set; }
        public long suid { get; set; }
        public List<FieId> fieIds { get; set; }
    }
}
