﻿using System;
using System.Threading.Tasks;
using System.Xml;

namespace BooruSharp.Booru
{
    public abstract partial class Booru
    {
        public async Task<Search.Related.SearchResult[]> GetRelated(string tag)
        {
            if (relatedUrl == null)
                throw new Search.FeatureUnavailable();
            XmlDocument xml = await GetXml(CreateUrl(relatedUrl, SearchArg("tags") + tag));
            int i = 0;
            Search.Related.SearchResult[] results = new Search.Related.SearchResult[xml.ChildNodes.Item(1).FirstChild.ChildNodes.Count];
            foreach (XmlNode node in xml.ChildNodes.Item(1).FirstChild.ChildNodes)
            {
                string[] args = GetStringFromXml(node, "name", "count");
                results[i] = new Search.Related.SearchResult(args[0], Convert.ToInt32(args[1]));
                i++;
            }
            return (results);
        }
    }
}
