using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DesuraDumper
{
    public class CookieAwareWebClient : WebClient
    {
        static System.Reflection.MethodInfo RemoveAndAdd = typeof(WebHeaderCollection).GetMethod("RemoveAndAdd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool isMono;

        public CookieContainer CookieContainer { get; set; }
        public Uri Uri { get; set; }
        public string Accept { get; set; }
		public bool KeepAlive { get; set; }

        public CookieAwareWebClient()
            : this(new CookieContainer())
        {
            isMono = Type.GetType("Mono.Runtime") != null;
			KeepAlive = true;
        }

        void MonoSetAccept()
        {
            RemoveAndAdd.Invoke(Headers, new object[] { "Accept", Accept });
        }

        public CookieAwareWebClient(CookieContainer cookies)
        {
            this.CookieContainer = cookies;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest httpRequest = (HttpWebRequest)request;
            httpRequest.Accept = Accept;
            httpRequest.CookieContainer = CookieContainer;
            // MONO: looks like Mono doesn't take too well to this?
            //httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            if (isMono) MonoSetAccept();
			httpRequest.KeepAlive = KeepAlive;
            return httpRequest;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            string setCookieHeader = response.Headers[HttpResponseHeader.SetCookie];

            //do something if needed to parse out the cookie.
            if (setCookieHeader != null)
            {
                // MONO: workaround for stubborn datetime parsing
                string[] cookieParts = setCookieHeader.Split(new string[] { "; " }, StringSplitOptions.None);
                for (int i = 0; i < cookieParts.Length; ++i)
                {
                    if (cookieParts[i].StartsWith("expires="))
                    {
                        // Split at equal sign
                        string[] expireParts = cookieParts[i].Split(new char[] { '=' }, 2);
                        expireParts[1] = DateTime.Parse(expireParts[1]).ToUniversalTime().ToString("ddd, dd-MMM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " GMT";
                        cookieParts[i] = string.Join("=", expireParts);
                    }
                }
                setCookieHeader = string.Join("; ", cookieParts);
                //Console.Write(setCookieHeader);
                this.CookieContainer.SetCookies(request.RequestUri, setCookieHeader);
            }
            return response;
        }
    }
}
