﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooruSharp.Booru.Template
{
    public class E621 : Booru
    {
        public E621(string url, BooruAuth auth = null) : base(url, auth, UrlFormat.danbooru, 750, BooruOptions.wikiSearchUseTitle, BooruOptions.noTagById)
        { }

        public override bool IsSafe()
            => false;

        public override Search.Post.SearchResult GetPostSearchResult(object json)
        {
            var elem = ((JArray)((JObject)json)["posts"]).FirstOrDefault();
            if (elem == null)
                throw new Search.InvalidTags();
            List<string> tags = elem["tags"]["general"].ToObject<string[]>().ToList();
            tags.AddRange(elem["tags"]["species"].ToObject<string[]>().ToList());
            tags.AddRange(elem["tags"]["character"].ToObject<string[]>().ToList());
            tags.AddRange(elem["tags"]["copyright"].ToObject<string[]>().ToList());
            tags.AddRange(elem["tags"]["artist"].ToObject<string[]>().ToList());
            tags.AddRange(elem["tags"]["meta"].ToObject<string[]>().ToList()); // TODO: Check others tags
            return new Search.Post.SearchResult(
                    new Uri(elem["file"]["url"].Value<string>()),
                    new Uri(elem["preview"]["url"].Value<string>()),
                    GetRating(elem["rating"].Value<string>()[0]),
                    tags.ToArray(),
                    elem["id"].Value<int>(),
                    elem["file"]["size"].Value<int>(),
                    elem["file"]["height"].Value<int>(),
                    elem["file"]["width"].Value<int>(),
                    elem["preview"]["height"].Value<int>(),
                    elem["preview"]["width"].Value<int>(),
                    elem["created_at"].Value<DateTime>(),
                    elem["sources"].Count() > 0 ? elem["sources"][0].Value<string>() : null,
                    elem["score"]["total"].Value<int>()
                );
        }
    }
}