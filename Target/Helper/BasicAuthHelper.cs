using System;
using System.Collections.Generic;
using System.Text;
using NLog.Targets.NetworkJSON.ExtensionMethods;

namespace NLog.Targets.NetworkJSON.Helper
{
    public class BasicAuthHelper
    {
        public const string BasicAuthPrefix = "Basic";

        public static string GetBasicAuth(string basicAuthString)
        {
            var pos = basicAuthString.IndexOf(' ');
            return basicAuthString.Substring(0, pos).CompareNoCase(BasicAuthPrefix) ? basicAuthString.Substring(pos + 1) : null;
        }

        public static KeyValuePair<string, string>? GetBasicAuthUserAndPassword(string userPassBase64)
        {
            if (userPassBase64.IsNullOrEmpty()) return null;
            var userPass = Encoding.UTF8.GetString(Convert.FromBase64String(userPassBase64));
            var pos = userPass.IndexOf(':');
            return new KeyValuePair<string, string>(userPass.Substring(0, pos), userPass.Substring(pos + 1));
        }

        public static string MakeBasicAuth(string userName, string password)
        {
            return $"{BasicAuthPrefix} {EncodeToBase64(userName, password)}";
        }

        public static string EncodeToBase64(string userName, string password)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
        }
    }
}
