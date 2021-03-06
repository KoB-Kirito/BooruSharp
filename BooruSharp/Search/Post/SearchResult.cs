﻿using System;

namespace BooruSharp.Search.Post
{
    public struct SearchResult
    {
        public SearchResult(Uri fileUrl, Uri previewUrl, Uri postUrl, Rating rating, string[] tags, int id,
                            int? size, int height, int width, int? previewHeight, int? previewWidth, DateTime? creation, string source, int? score, string md5)
        {
            this.fileUrl = fileUrl;
            this.previewUrl = previewUrl;
            this.postUrl = postUrl;
            this.rating = rating;
            this.tags = tags;
            this.id = id;
            this.size = size;
            this.height = height;
            this.width = width;
            this.previewHeight = previewHeight;
            this.previewWidth = previewWidth;
            this.creation = creation;
            this.source = source;
            this.score = score;
            this.md5 = md5;
        }

        /// <summary>
        /// Url of the image
        /// </summary>
        public readonly Uri fileUrl;

        /// <summary>
        /// Preview url of the image
        /// </summary>
        public readonly Uri previewUrl;

        /// <summary>
        /// The url of the post
        /// </summary>
        public readonly Uri postUrl;

        /// <summary>
        /// Is the image safe or not
        /// </summary>
        public readonly Rating rating;

        /// <summary>
        /// All the tags contained in the image
        /// </summary>
        public readonly string[] tags;

        /// <summary>
        /// Id of the image
        /// </summary>
        public readonly int id;

        /// <summary>
        /// Size in octets of the image
        /// </summary>
        public readonly int? size;

        /// <summary>
        /// Height in pixels of the image
        /// </summary>
        public readonly int height;

        /// <summary>
        /// Width in pixels of the image
        /// </summary>
        public readonly int width;

        /// <summary>
        /// Height in pixels of the preview image
        /// </summary>
        public readonly int? previewHeight;

        /// <summary>
        /// Width in pixels of the preview image
        /// </summary>
        public readonly int? previewWidth;

        /// <summary>
        /// When was the post created
        /// </summary>
        public readonly DateTime? creation;

        /// <summary>
        /// Where the image is coming from
        /// </summary>
        public readonly string source;

        /// <summary>
        /// Score of the image
        /// </summary>
        public readonly int? score;

        /// <summary>
        /// Hash (md5) of the file
        /// </summary>
        public readonly string md5;
    }
}
