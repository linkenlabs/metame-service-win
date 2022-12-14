using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MetaMe.Sensors
{
    class UrlUtils
    {
        public static string ExtractUrlHost(string url)
        {
            string pattern = GetUrlRegex();
            Match match = Regex.Match(url, pattern);
            if (match == null)
            {
                return String.Empty;
            }
            var urlHostGroup = match.Groups[2];
            return urlHostGroup.Value;
        }

        public static string ExtractUrlProtocolAndHost(string url)
        {
            string pattern = GetUrlRegex();
            Match match = Regex.Match(url, pattern);
            if (match == null)
            {
                return String.Empty;
            }
            var urlProtocolGroup = match.Groups[1];
            var urlHostGroup = match.Groups[2];
            return urlProtocolGroup.Value + urlHostGroup.Value;
        }

        public static string GetUrlRegex()
        {
            //ripped from https://gist.github.com/dperini/729294
            //dperini was the best url regex apparently. See https://mathiasbynens.be/demo/url-regex

            var urlRegex =
            "^" +

            ////create a capture group 1 for protocol
            // protocol identifier
            "(" +
            "(?:(?:https?|ftp)://)" +
            ")" +
            // user:pass authentication
            "(?:\\S+(?::\\S*)?@)?" +
            //create a capture group 2 for domain section
            "(" +
            "(?:" +
              // IP address exclusion
              // private & local networks
              "(?!(?:10|127)(?:\\.\\d{1,3}){3})" +
              "(?!(?:169\\.254|192\\.168)(?:\\.\\d{1,3}){2})" +
              "(?!172\\.(?:1[6-9]|2\\d|3[0-1])(?:\\.\\d{1,3}){2})" +
              // IP address dotted notation octets
              // excludes loopback network 0.0.0.0
              // excludes reserved space >= 224.0.0.0
              // excludes network & broacast addresses
              // (first & last IP address of each class)
              "(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])" +
              "(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}" +
              "(?:\\.(?:[1-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))" +
            "|" +
              // host name
              "(?:(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)" +
              // domain name
              "(?:\\.(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)*" +
              // TLD identifier
              "(?:\\.(?:[a-z\\u00a1-\\uffff]{2,}))" +
              // TLD may end with dot
              "\\.?" +
              "|" +
              "localhost" +
            ")" +
            // port number
            "(?::\\d{2,5})?" +
            ")" + //close capture group
            // resource path
            "(?:[/?#]\\S*)?" +
            "$";

            return urlRegex;
        }
    }
}
