using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpeedUp.Model
{
    public class LoginInfoExtra
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserAgent { get; set; }
        public string UaPassword { get; set; }        
        public string RetsUrl { get; set; }
        public string RetsVersion { get; set; }
        public string HttpUserAgent { get; set; }
        public string HttpMethod { get; set; }
        public string HttpVersion { get; set; }
        public string ByPassAuthentication { get; set; }
    }
}
