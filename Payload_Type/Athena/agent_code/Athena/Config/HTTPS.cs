﻿using Athena.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Athena.Config
{
    /**
    public class HTTPS
    {
        public string userAgent { get; set; }
        public string hostHeader { get; set; }
        public string getURL { get; set; }
        public string postURL { get; set; }
        public string psk { get; set; }
        public string param { get; set; }
        public DateTime killDate { get; set; }
        public bool encryptedExchangeCheck { get; set; }
        //Change this to Dictionary or Convert from JSON string?
        public string headers { get; set; }
        public string proxyHost { get; set; }
        public string proxyPass { get; set; }
        public int proxyPort { get; set; }
        public string proxyUser { get; set; }

        public HTTPS()
        {
            int callbackPort = Int32.Parse("callback_port");
            string callbackHost = "callback_host";
            string callbackURL = $"{callbackHost}:{callbackPort}";
            this.userAgent = "user-agent";
            this.hostHeader = "%HOSTHEADER%";
            this.getURL = "callback_host:callback_port/get_uri?query_path_name";
            this.postURL = "callback_host:callback_port/post_uri";
            this.param = "query_path_name";
            this.proxyHost = "proxy_host";
            this.proxyPass = "proxy_pass";
            this.proxyPort = Int32.Parse("proxy_port");
            this.proxyUser = "proxy_user";

            if (!String.IsNullOrEmpty(this.psk))
            {
                Globals.encrypted = true;
            }
        }
        public async Task<string> Send(object obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                Console.WriteLine("Request: " + json);
                var content = new StringContent(Misc.Base64Encode(Globals.mc.MythicConfig.uuid + json));
                var response = await Globals.client.PostAsync(Globals.mc.MythicConfig.httpConfig.postURL, content);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return "";
            }
        }
    }
    **/
}
