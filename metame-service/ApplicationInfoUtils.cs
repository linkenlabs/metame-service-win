using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MetaMe.WindowsClient
{
    class ApplicationInfoUtils
    {
        public static ImmutableArray<ApplicationInfo> GetApplicationInfo(bool showHidden)
        {
            var apps = GetApplicationInfoFromFacts().ToList();

            Action<ApplicationInfo> addIfMissing = (datum) =>
            {
                var existing = (from item in apps
                                where item.Name == datum.Name
                                select item).FirstOrDefault();

                if (existing == null)
                {
                    apps.Add(datum);
                }
            };

            GetApplicationInfoFromScannedApplications().ToList().ForEach(addIfMissing);

            //filter out browser related items
            string[] browserRelatedKeywords = new string[] { "- Other", "- New Tab", "Google Chrome", "Firefox", "Tor Browser" };

            apps = apps.FindAll(item =>
            {
                foreach (var keyword in browserRelatedKeywords)
                {
                    if (item.Name.Contains(keyword))
                    {
                        return false;
                    }
                }
                return true;
            });

            //remove hidden
            if (!showHidden)
            {
                var hideList = ClientApplication.Instance.GetHiddenAppList();

                apps = (from item in apps
                        where !hideList.Items.Contains(item.Name)
                        select item).ToList();
            }

            return apps.ToImmutableArray();
        }

        static ImmutableArray<ApplicationInfo> GetApplicationInfoFromFacts()
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();

            var result = (from item in hourLevelFacts
                          where !string.IsNullOrEmpty(item.AppName)
                          group item by item.AppName into g
                          select new
                          {
                              AppName = g.Key,
                              Usage = g.Sum(item => item.TotalActiveDuration)
                          }).ToList();

            var apps = result.ConvertAll((item) => new ApplicationInfo
            {
                Name = item.AppName,
                Type = GetTypeFromAppName(item.AppName),
                Relevance = item.Usage,
                Base64Icon = IconHelpers.GetBase64Icon(item.AppName)
            });

            return apps.ToImmutableArray();
        }
        public static string GetTypeFromAppName(string appName)
        {
            if ((!appName.Contains(".")
                && !appName.Contains("localhost"))
                || appName.Contains(" "))
            {
                return "application";
            }

            if (appName.Contains("localhost"))
            {
                return "website";
            }

            var result = Uri.CheckHostName(appName);

            if (result == UriHostNameType.Unknown)
            {
                return "application";
            }
            else
            {
                return "website";
            }
        }
        static ImmutableArray<ApplicationInfo> GetApplicationInfoFromScannedApplications()
        {
            var scannedApplications = ClientApplication.Instance.GetScannedApplications();
            var converted = scannedApplications.Select(datum => new ApplicationInfo
            {
                Name = datum.FileDescription,
                Type = "application",
                Base64Icon = IconHelpers.GetBase64Icon(datum.FileDescription),
            }).Where(item => !string.IsNullOrEmpty(item.Name));
            return converted.ToImmutableArray();
        }
    }
}
