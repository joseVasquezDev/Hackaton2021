using System;
using System.Runtime.Serialization;

namespace ReverseProxyApplication
{
    [DataContract]
    public class ResponseModels
    {
        [DataMember(Name = "hostname")]
        public string Hostname { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "data")]
        public string Data { get; set; }

        [DataMember(Name = "date")]
        public DateTime Date { get; set; }

        [DataMember(Name = "validitySeconds")]
        public int ValiditySeconds { get; set; }

        [DataMember(Name = "token")]
        public string Token { get; set; }
    }
}
