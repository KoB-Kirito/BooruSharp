﻿using BooruSharp.Booru;
using BooruSharp.Others;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace BooruSharp.UnitTests
{
    public static class General
    {
        public static async Task<ABooru> CreateBooru(Type t)
        {
            var b = (ABooru)Activator.CreateInstance(t);
            if (t == typeof(Pixiv))
            {
                Skip.If(Environment.GetEnvironmentVariable("PIXIV_USER_ID") == null);
                await ((Pixiv)b).LoginAsync(Environment.GetEnvironmentVariable("PIXIV_USER_ID"), Environment.GetEnvironmentVariable("PIXIV_PASSWORD"));
            }
            return b;
        }

        private static async Task<string> CheckUrl(Uri url)
        {
            try
            {
                using (HttpClient hc = new HttpClient())
                {
                    hc.DefaultRequestHeaders.Add("User-Agent", "BooruSharp");
                    await hc.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                }
                return null;
            }
            catch (WebException ex)
            { return ex.Message + " for " + url; }
        }

        public static async Task CheckResult(Search.Post.SearchResult result, string inputTag)
        {
            if (result.fileUrl != null)
            {
                string resFile = await CheckUrl(result.fileUrl);
                string resPreview = await CheckUrl(result.previewUrl);
                string resPost = await CheckUrl(result.postUrl);
                Assert.True(resPost == null, resPost);
                Assert.True(resFile == null, resFile);
                Assert.True(resPreview == null, resPreview);
                Assert.NotEqual(0, result.height);
                Assert.NotEqual(0, result.width);
                if (result.previewHeight != null)
                {
                    Assert.NotEqual(0, result.previewHeight);
                    Assert.NotEqual(0, result.previewWidth);
                }
            }
            Assert.InRange(result.rating, Search.Post.Rating.Safe, Search.Post.Rating.Explicit);
            Assert.Contains(inputTag, result.tags);
            Assert.NotEqual(0, result.id);
            if (result.size.HasValue)
                Assert.NotEqual(0, result.size.Value);
        }

        public static async Task CheckGetRandom(ABooru booru, string s1)
        {
            Search.Post.SearchResult result = await booru.GetRandomPostAsync(s1);
            Search.Post.SearchResult result2;
            int i = 0;
            do
            {
                result2 = await booru.GetRandomPostAsync(s1);
                i++;
            } while (result.id == result2.id && i < 5);
            Assert.NotEqual(result.id, result2.id);
            await CheckResult(result, s1);
        }

        public static async Task CheckGetRandoms(ABooru booru, string s1)
        {
            Search.Post.SearchResult[] result = await booru.GetRandomPostsAsync(5, s1);
            Assert.NotEmpty(result);
            Search.Post.SearchResult[] result2;
            int i = 0;
            do
            {
                result2 = await booru.GetRandomPostsAsync(5, s1);
                Assert.NotEmpty(result2);
                i++;
            } while (result[0].id == result2[0].id && i < 5);
            Assert.NotEqual(result[0].id, result2[0].id);
            await CheckResult(result[0], s1);
        }

        public static async Task CheckTag(ABooru booru, string s1 = "pantyhose")
        {
            Search.Tag.SearchResult result = await booru.GetTagAsync(s1);
            Assert.Equal(s1, result.name);
            Assert.InRange(result.type, Search.Tag.TagType.Trivia, Search.Tag.TagType.Metadata);
            Assert.NotEqual((Search.Tag.TagType)2, result.type);
            Assert.NotEqual(0, result.count);
        }

        public static void CheckWiki(Search.Wiki.SearchResult result)
        {
            Assert.InRange(result.lastUpdate, result.creation, DateTime.Now);
        }

        public static void CheckRelated(Search.Related.SearchResult[] result)
        {
            Assert.NotEmpty(result);
        }

        public static void CheckComment(Search.Comment.SearchResult[] result)
        {
            foreach (Search.Comment.SearchResult res in result)
            {
                Assert.NotEqual(0, res.authorId);
                Assert.NotEqual(0, res.commentId);
                Assert.NotEqual(0, res.postId);
                Assert.NotEmpty(res.body);
            }
            Assert.NotEmpty(result);
        }

        public static bool CompareArray(Search.Post.SearchResult[] res1, Search.Post.SearchResult[] res2)
        {
            if (res1.Length != res2.Length)
                return false;
            for (int i = 0; i < res1.Length; i++)
                if (res1[i].id != res2[i].id)
                    return false;
            return true;
        }

        public static async Task<Search.Post.SearchResult> GetRandomPost(ABooru booru)
        {
            if (booru.NoEmptyPostSearch())
                return await booru.GetRandomPostAsync("スク水"); // Pixiv doesn't handle random search with no tag
            return await booru.GetRandomPostAsync();
        }
    }

    public class BooruTests
    {
        [Fact]
        public void IsBooruAuthSet()
        {
            var b = new Gelbooru();
            Assert.True(b.Auth == null);
            b.Auth = new BooruAuth("AAA", "AAA");
            Assert.False(b.Auth == null);
        }

        [SkipIfNoEnvTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task UnsetFavoriteError(Type t)
        {
            var booru = await General.CreateBooru(t);
            var id = (await General.GetRandomPost(booru)).id;
            booru.Auth = new BooruAuth("AAA", "AAA");
            if (!booru.HasFavoriteAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.RemoveFavoriteAsync(id); });
            else if (t == typeof(Gelbooru))
                await Assert.ThrowsAsync<Search.AuthentificationInvalid>(async delegate () { await booru.RemoveFavoriteAsync(id); });
        }

        [SkipIfNoEnvTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task SetFavoriteError(Type t)
        {
            var booru = (ABooru)Activator.CreateInstance(t);
            if (booru is Pixiv)
                await Assert.ThrowsAsync<Search.AuthentificationInvalid>(async delegate () { await ((Pixiv)booru).LoginAsync("AAA", "AAA"); });
            else
            {
                booru.Auth = new BooruAuth("AAA", "AAA");
                if (!booru.HasFavoriteAPI())
                    await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.AddFavoriteAsync(800); });
                else
                    await Assert.ThrowsAsync<Search.AuthentificationInvalid>(async delegate () { await booru.AddFavoriteAsync(800); });
            }
        }

        [SkipIfNoEnvTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        // [InlineData(typeof(Xbooru))] // Xbooru allow to add post with invalid ID
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task SetFavoriteInvalidId(Type t)
        {
            var booru = await General.CreateBooru(t);
            string name = t.ToString().ToUpper().Split('.').Last();
            booru.Auth = new BooruAuth(Environment.GetEnvironmentVariable(name + "_USER_ID"), Environment.GetEnvironmentVariable(name + "_PASSWORD_HASH"));
            if (!booru.HasFavoriteAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.AddFavoriteAsync(int.MaxValue); });
            else
                await Assert.ThrowsAsync<Search.InvalidPostId>(async delegate () { await booru.AddFavoriteAsync(int.MaxValue); });
        }

        [SkipIfNoEnvTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task SetFavorite(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasFavoriteAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.AddFavoriteAsync(10); });
            else
            {
                var id = (await General.GetRandomPost(booru)).id;
                string name = t.ToString().ToUpper().Split('.').Last();
                booru.Auth = new BooruAuth(Environment.GetEnvironmentVariable(name + "_USER_ID"), Environment.GetEnvironmentVariable(name + "_PASSWORD_HASH"));

                await booru.AddFavoriteAsync(id);
                await booru.RemoveFavoriteAsync(id);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task GetByMd5(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasPostByMd5API())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetPostByMd5Async("0"); });
            else
            {
                Search.Post.SearchResult result1;
                do
                {
                    result1 = await General.GetRandomPost(booru);
                } while (result1.md5 == null);
                var result2 = await booru.GetPostByMd5Async(result1.md5);
                Assert.Equal(result1.id, result2.id);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task GetById(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasPostByIdAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetPostByIdAsync(0); });
            else
            {
                Search.Post.SearchResult result1 = await General.GetRandomPost(booru);
                var result2 = await booru.GetPostByIdAsync(result1.id);
                Assert.Equal(result1.id, result2.id);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task GetLastPosts(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (booru.NoEmptyPostSearch())
                await Assert.ThrowsAsync<ArgumentException>(async () => await booru.GetLastPostsAsync());
            else
            {
                var results = await booru.GetLastPostsAsync();
                Assert.NotInRange(results.Length, 0, 1);
                Assert.NotEqual(results[0].id, results[1].id);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621), "kantai_collection", "swimwear")]
        [InlineData(typeof(E926), "kantai_collection", "swimwear")]
        [InlineData(typeof(Furrybooru), "kantai_collection")]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan), "hibiki_(kancolle)")]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "swimsuit", "asian")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection", "explosions")]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru), "kantai_collection")]
        [InlineData(typeof(Yandere), "kantai_collection", "swimsuits")]
        [InlineData(typeof(Pixiv), "響(艦隊これくしょん)", "水着艦娘")]
        public async Task GetLastPostsWithTags(Type t, string tag = "hibiki_(kantai_collection)", string tag2 = "swimsuit")
        {
            var booru = await General.CreateBooru(t);
            Search.Post.SearchResult[] results;
            results = await booru.GetLastPostsAsync(tag, tag2);
            Assert.NotInRange(results.Length, 0, 1);
            Assert.NotEqual(results[0].id, results[1].id);
            foreach (var elem in results)
            {
                Assert.Contains(tag, elem.tags);
                Assert.Contains(tag2, elem.tags);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621), "kantai_collection", "swimwear")]
        [InlineData(typeof(E926), "kantai_collection", "swimwear")]
        [InlineData(typeof(Furrybooru), "kantai_collection")]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan), "hibiki_(kancolle)")]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "swimsuit", "asian")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection", "explosions")]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru), "kantai_collection")]
        [InlineData(typeof(Yandere), "kantai_collection", "swimsuits")]
        [InlineData(typeof(Pixiv), "響(艦隊これくしょん)", "水着")]
        public async Task GetPostCount(Type t, string tag = "hibiki_(kantai_collection)", string tag2 = "swimsuit")
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasPostCountAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetPostCountAsync(); });
            else
            {
                int countEmpty = booru.NoEmptyPostSearch() ? int.MaxValue : await booru.GetPostCountAsync(); // Pixiv doesn't handle PostCount with no tag
                var countOne = await booru.GetPostCountAsync(tag);
                var countTwo = await booru.GetPostCountAsync(tag, tag2);
                Assert.NotEqual(0, countEmpty);
                Assert.NotEqual(0, countOne);
                Assert.NotEqual(0, countTwo);
                Assert.InRange(countOne, countTwo, countEmpty);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "small_breasts")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection")]
        [InlineData(typeof(SankakuComplex), "small_breasts")]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv), "スク水")]
        public async Task GetRandom(Type t, string tag = "school_swimsuit")
        {
            await General.CheckGetRandom(await General.CreateBooru(t), tag);
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "small_breasts")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection")]
        [InlineData(typeof(SankakuComplex), "small_breasts")]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv), "スク水")]
        public async Task GetRandoms(Type t, string tag = "school_swimsuit")
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasMultipleRandomAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await General.CheckGetRandoms(booru, tag); });
            else
                await General.CheckGetRandoms(booru, tag);
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "small_breasts")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection")]
        [InlineData(typeof(SankakuComplex), "small_breasts")]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv), "スク水")]
        public async Task GetRandomsTooMany(Type t, string tag = "school_swimsuit")
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasMultipleRandomAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetRandomPostsAsync(int.MaxValue, tag); });
            else
            {
                var result = await booru.GetRandomPostsAsync(int.MaxValue, tag);
                Assert.NotEmpty(result);
                foreach (var r in result)
                    Assert.Contains(tag, r.tags);
            }
        }

        [Fact]
        public async Task SetHttpClient()
        {
            var booru = new Gelbooru();
            HttpClient hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "BooruSharp.Unit-Tests");
            booru.HttpClient = hc;
            await General.CheckGetRandom(booru, "kantai_collection");
            Assert.Single(hc.DefaultRequestHeaders.GetValues("User-Agent"));
            Assert.Contains("BooruSharp.Unit-Tests", hc.DefaultRequestHeaders.GetValues("User-Agent"));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621), "kantai_collection")]
        [InlineData(typeof(E926), "kantai_collection")]
        [InlineData(typeof(Furrybooru), "kantai_collection")]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan), "hibiki_(kancolle)")]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "school_swimsuit", "small_breasts")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection", "explosions")]
        [InlineData(typeof(SankakuComplex), "hibiki_(kantai_collection)", "old_school_swimsuit")]
        [InlineData(typeof(Xbooru), "kantai_collection")]
        [InlineData(typeof(Yandere), "kantai_collection")]
        [InlineData(typeof(Pixiv), "響(艦隊これくしょん)", "水着艦娘")]
        public async Task GetRandom2Tags(Type t, string tag = "hibiki_(kantai_collection)", string tag2 = "school_swimsuit")
        {
            var booru = await General.CreateBooru(t);
            var result = await booru.GetRandomPostAsync(tag, tag2);
            Assert.Contains(tag, result.tags);
            Assert.Contains(tag2, result.tags);
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621), "kantai_collection")]
        [InlineData(typeof(E926), "kantai_collection")]
        [InlineData(typeof(Furrybooru), "kantai_collection")]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan), "hibiki_(kancolle)")]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru), "school_swimsuit", "small_breasts")]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection", "explosions")]
        [InlineData(typeof(SankakuComplex), "hibiki_(kantai_collection)", "old_school_swimsuit")]
        [InlineData(typeof(Xbooru), "kantai_collection")]
        [InlineData(typeof(Yandere), "kantai_collection")]
        [InlineData(typeof(Pixiv), "響(艦隊これくしょん)", "スク水")]
        public async Task GetRandoms2Tags(Type t, string tag = "hibiki_(kantai_collection)", string tag2 = "school_swimsuit")
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasMultipleRandomAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetRandomPostsAsync(5, tag, tag2); });
            else
            {
                var result = await booru.GetRandomPostsAsync(5, tag, tag2);
                Assert.NotEmpty(result);
                foreach (var r in result)
                {
                    Assert.Contains(tag, r.tags);
                    Assert.Contains(tag2, r.tags);
                }
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), false)]
        [InlineData(typeof(DanbooruDonmai), true)]
        [InlineData(typeof(E621), false, "sea", "loli", "swimwear")]
        [InlineData(typeof(E926), false, "sea", "breasts", "swimwear")]
        [InlineData(typeof(Furrybooru), false, "water")]
        [InlineData(typeof(Gelbooru), false)]
        [InlineData(typeof(Konachan), false, "water")]
        [InlineData(typeof(Lolibooru), false)]
        [InlineData(typeof(Realbooru), false, "water")]
        [InlineData(typeof(Rule34), false)]
        [InlineData(typeof(Safebooru), false)]
        [InlineData(typeof(Sakugabooru), false, "kantai_collection", "explosions", "fire")]
        [InlineData(typeof(SankakuComplex), false, "ocean", "loli", "swimsuit")]
        [InlineData(typeof(Xbooru), false, "ocean", "small_breasts")]
        [InlineData(typeof(Yandere), false, "see_through", "loli", "swimsuits")]
        [InlineData(typeof(Pixiv), false, "東方", "貧乳", "水着")]
        public async Task TooManyTags(Type t, bool throwError, string tag = "ocean", string tag2 = "flat_chest", string tag3 = "swimsuit")
        {
            var booru = await General.CreateBooru(t);
            Search.Post.SearchResult result;
            if (throwError)
            {
                await Assert.ThrowsAsync<Search.TooManyTags>(async () =>
                {
                    result = await booru.GetRandomPostAsync(tag, tag2, tag3);
                });
            }
            else
            {
                result = await booru.GetRandomPostAsync(tag, tag2, tag3);
                Assert.Contains(result.tags, x => x.Contains(tag));
                Assert.Contains(result.tags, x => x.Contains(tag2));
                Assert.Contains(result.tags, x => x.Contains(tag3));
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), false)]
        [InlineData(typeof(DanbooruDonmai), true)]
        [InlineData(typeof(E621), false, "sea", "loli", "swimwear")]
        [InlineData(typeof(E926), false, "sea", "loli", "swimwear")]
        [InlineData(typeof(Furrybooru), false, "water")]
        [InlineData(typeof(Gelbooru), false)]
        [InlineData(typeof(Konachan), false, "water")]
        [InlineData(typeof(Lolibooru), false)]
        [InlineData(typeof(Realbooru), false, "water")]
        [InlineData(typeof(Rule34), false)]
        [InlineData(typeof(Safebooru), false)]
        [InlineData(typeof(Sakugabooru), false, "kantai_collection", "explosions", "fire")]
        [InlineData(typeof(SankakuComplex), false, "ocean", "loli", "swimsuit")]
        [InlineData(typeof(Xbooru), false, "ocean", "small_breasts")]
        [InlineData(typeof(Yandere), false, "see_through", "loli", "swimsuits")]
        [InlineData(typeof(Pixiv), false, "水", "貧乳", "水着")]
        public async Task TooManyTagsMany(Type t, bool throwError, string tag = "ocean", string tag2 = "flat_chest", string tag3 = "swimsuit")
        {
            var booru = await General.CreateBooru(t);
            Search.Post.SearchResult[] result;
            if (throwError)
            {
                await Assert.ThrowsAsync<Search.TooManyTags>(async () =>
                {
                    result = await booru.GetRandomPostsAsync(5, tag, tag2, tag3);
                });
            }
            else if (!booru.HasMultipleRandomAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetRandomPostsAsync(5, tag, tag2, tag3); });
            else
            {
                result = await booru.GetRandomPostsAsync(5, tag, tag2, tag3);
                foreach (var r in result)
                {
                    Assert.Contains(tag, r.tags);
                    Assert.Contains(tag2, r.tags);
                    Assert.Contains(tag3, r.tags);
                }
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task GetRandomFail(Type t)
        {
            await Assert.ThrowsAsync<Search.InvalidTags>(async () => await (await General.CreateBooru(t)).GetRandomPostAsync("someInvalidTag"));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task GetRandomsFail(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasMultipleRandomAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetRandomPostsAsync(5, "someInvalidTag"); });
            else
                Assert.Empty(await booru.GetRandomPostsAsync(5, "someInvalidTag"));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru), "kantai_collection")]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv), "パンスト")]
        public async Task CheckTag(Type t, string tag = "pantyhose")
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasTagByIdAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetTagAsync(tag); });
            else
                await General.CheckTag(booru, tag);
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task CheckTagFail(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasTagByIdAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetTagAsync("someRandomTag"); });
            else
                await Assert.ThrowsAsync<Search.InvalidTags>(() => booru.GetTagAsync("someRandomTag"));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), "female", true)]
        [InlineData(typeof(DanbooruDonmai), "hibi", true)]
        [InlineData(typeof(E621), "hibiki", true)]
        [InlineData(typeof(E926), "hibiki", true)]
        [InlineData(typeof(Furrybooru), "hibiki", true)]
        [InlineData(typeof(Gelbooru), "hibiki", true)]
        [InlineData(typeof(Konachan), "hibiki", false)]
        [InlineData(typeof(Lolibooru), "hibiki", false)]
        [InlineData(typeof(Realbooru), "female", true)]
        [InlineData(typeof(Rule34), "hibiki", true)]
        [InlineData(typeof(Safebooru), "hibiki", true)]
        [InlineData(typeof(Sakugabooru), "kantai", false)]
        [InlineData(typeof(SankakuComplex), "hibiki", false)]
        [InlineData(typeof(Xbooru), "hibiki", true)]
        [InlineData(typeof(Yandere), "hibiki", false)]
        [InlineData(typeof(Pixiv), "艦隊こ", false)]
        public async Task CheckTags(Type t, string tag, bool onlyOnce)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasTagByIdAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetTagAsync(tag); });
            else if (onlyOnce)
                Assert.NotEmpty(await booru.GetTagsAsync(tag));
            else
                Assert.NotInRange((await booru.GetTagsAsync(tag)).Length, 0, 1);
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), "hibiki_(kantai_collection)", 2033)]
        [InlineData(typeof(DanbooruDonmai), "hibiki_(kantai_collection)", 1240738)]
        [InlineData(typeof(E621), "kantai_collection", 267881)]
        [InlineData(typeof(E926), "kantai_collection", 1329650)]
        [InlineData(typeof(Furrybooru), "kantai_collection", 151628)]
        [InlineData(typeof(Gelbooru), "hibiki_(kantai_collection)", 463392)]
        [InlineData(typeof(Konachan), "hibiki_(kancolle)", 75885)]
        [InlineData(typeof(Lolibooru), "hibiki_(kantai_collection)", 2939)]
        [InlineData(typeof(Realbooru), "kantai_collection", 688290)]
        [InlineData(typeof(Rule34), "hibiki_(kantai_collection)", 321239)]
        [InlineData(typeof(Safebooru), "hibiki_(kantai_collection)", 316679)]
        [InlineData(typeof(Sakugabooru), "kantai_collection", 7148)]
        [InlineData(typeof(SankakuComplex), "kantai_collection", 458437)]
        [InlineData(typeof(Xbooru), "hibiki_(kantai_collection)", 151883)]
        [InlineData(typeof(Yandere), "hibiki_(kancolle)", 98153)]
        [InlineData(typeof(Pixiv), "響(艦隊これくしょん)", -1)]
        public async Task TagId(Type t, string tag, int tagId)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasTagByIdAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetTagAsync(tagId); });
            else
                Assert.Equal(tag, (await booru.GetTagAsync(tagId)).name);
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task TagIdFail(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasTagByIdAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetTagAsync(int.MaxValue); });
            else
                await Assert.ThrowsAsync<Search.InvalidTags>(() => booru.GetTagAsync(int.MaxValue));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), "highres", 82)]
        [InlineData(typeof(DanbooruDonmai), "futanari", 3589)]
        [InlineData(typeof(E621), "futanari", 123)]
        [InlineData(typeof(E926), "futanari", 123)]
        [InlineData(typeof(Furrybooru), "futanari", -1)]
        [InlineData(typeof(Gelbooru), "futanari", -1)]
        [InlineData(typeof(Konachan), "futanari", 757)]
        [InlineData(typeof(Lolibooru), "futanari", 158)]
        [InlineData(typeof(Realbooru), "futanari", -1)]
        [InlineData(typeof(Rule34), "futanari", -1)]
        [InlineData(typeof(Safebooru), "futanari", -1)]
        [InlineData(typeof(Sakugabooru), "animated", 13)]
        [InlineData(typeof(SankakuComplex), "blush", 826)]
        [InlineData(typeof(Xbooru), "futanari", -1)]
        [InlineData(typeof(Yandere), "futanari", 167)]
        [InlineData(typeof(Pixiv), "ふたなり", -1)]
        public async Task CheckWiki(Type t, string tag, int? id)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasWikiAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetWikiAsync(tag); });
            else
            {
                Search.Wiki.SearchResult result = await booru.GetWikiAsync(tag);
                Assert.Equal(id, result.id);
                General.CheckWiki(result);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task CheckWikiFail(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasWikiAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetWikiAsync("yetAnotherTag"); });
            else
                await Assert.ThrowsAsync<Search.InvalidTags>(() => booru.GetWikiAsync("yetAnotherTag"));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), "kantai_collection", "anchor_symbol")]
        [InlineData(typeof(DanbooruDonmai), "kantai_collection", "serafuku")]
        [InlineData(typeof(E621), "sky", "cloud")]
        [InlineData(typeof(E926), "sky", "cloud")]
        [InlineData(typeof(Furrybooru), "sky", "cloud")]
        [InlineData(typeof(Gelbooru), "sky", "cloud")]
        [InlineData(typeof(Konachan), "sky", "clouds")]
        [InlineData(typeof(Lolibooru), "sky", "cloud")]
        [InlineData(typeof(Realbooru), "sky", "clouds")]
        [InlineData(typeof(Rule34), "sky", "clouds")]
        [InlineData(typeof(Safebooru), "sky", "clouds")]
        [InlineData(typeof(Sakugabooru), "kantai_collection", "explosions")]
        [InlineData(typeof(SankakuComplex), "sky", "clouds")]
        [InlineData(typeof(Xbooru), "sky", "clouds")]
        [InlineData(typeof(Yandere), "landscape", "wallpaper")]
        [InlineData(typeof(Pixiv), "空", "雲")]
        public async Task CheckRelated(Type t, string tag, string related)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasRelatedAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetRelatedAsync(tag); });
            else
            {
                Search.Related.SearchResult[] result = await booru.GetRelatedAsync(tag);
                General.CheckRelated(result);
                Assert.Contains(result, x => x.name == related);
            }
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task CheckRelatedFail(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasRelatedAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetRelatedAsync("thisWillFail"); });
            else
                Assert.Empty(await booru.GetRelatedAsync("thisWillFail"));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru), 257639)]
        [InlineData(typeof(DanbooruDonmai), 3193008)]
        [InlineData(typeof(E621), 59432)]
        [InlineData(typeof(E926), 541858)]
        [InlineData(typeof(Furrybooru), 1282210)]
        [InlineData(typeof(Gelbooru), 3988284)]
        [InlineData(typeof(Konachan), 142938)]
        [InlineData(typeof(Lolibooru), 134097)]
        [InlineData(typeof(Realbooru), 646911)]
        [InlineData(typeof(Rule34), 2840746)]
        [InlineData(typeof(Safebooru), 132)]
        [InlineData(typeof(Sakugabooru), 38886)]
        [InlineData(typeof(SankakuComplex), 48)]
        [InlineData(typeof(Xbooru), 740157)]
        [InlineData(typeof(Yandere), 619494)]
        [InlineData(typeof(Pixiv), -1)]
        public async Task CheckComment(Type t, int id)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasCommentAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetCommentsAsync(id); });
            else
                General.CheckComment(await booru.GetCommentsAsync(id));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task CheckCommentFail(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasCommentAPI())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetCommentsAsync(int.MaxValue); });
            else
                Assert.Empty(await booru.GetCommentsAsync(int.MaxValue));
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task CheckLastComment(Type t)
        {
            var booru = await General.CreateBooru(t);
            if (!booru.HasSearchLastComment())
                await Assert.ThrowsAsync<Search.FeatureUnavailable>(async delegate () { await booru.GetLastCommentsAsync(); });
            else
                General.CheckComment(await booru.GetLastCommentsAsync());
        }

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926))]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru))]
        [InlineData(typeof(Sakugabooru))]
        [InlineData(typeof(SankakuComplex))]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv))]
        public async Task CheckAvailable(Type t)
        {
            await (await General.CreateBooru(t)).CheckAvailabilityAsync();
        }

        /*[SkippableTheory]
        public async Task CheckNotAvailable(Type t)
        {
            await Assert.ThrowsAsync<HttpRequestException>(async () => await ((Booru.Booru)Activator.CreateInstance(t, (BooruAuth)null)).CheckAvailability());
        }*/

        [SkippableTheory]
        [InlineData(typeof(Atfbooru))]
        [InlineData(typeof(DanbooruDonmai))]
        [InlineData(typeof(E621))]
        [InlineData(typeof(E926), "breast")]
        [InlineData(typeof(Furrybooru))]
        [InlineData(typeof(Gelbooru))]
        [InlineData(typeof(Konachan))]
        [InlineData(typeof(Lolibooru))]
        [InlineData(typeof(Realbooru))]
        [InlineData(typeof(Rule34))]
        [InlineData(typeof(Safebooru), "breast")]
        [InlineData(typeof(Sakugabooru), "another")]
        [InlineData(typeof(SankakuComplex), "pussy_line")]
        [InlineData(typeof(Xbooru))]
        [InlineData(typeof(Yandere))]
        [InlineData(typeof(Pixiv), "おまんこ")]
        public async Task CheckIsSafe(Type t, string explicitTag = "pussy")
        {
            ABooru b = await General.CreateBooru(t);
            bool isSafe = b.IsSafe();
            bool foundExplicit = false;
            for (int i = 0; i < 10; i++)
            {
                var image = await b.GetRandomPostAsync(explicitTag);
                if (isSafe && image.fileUrl != null)
                    Assert.NotEqual(Search.Post.Rating.Explicit, image.rating);
                if (image.rating == Search.Post.Rating.Explicit)
                    foundExplicit = true;
            }
            if (!isSafe)
                Assert.True(foundExplicit);

        }
    }

    // TODO: Find a way to check if commands are available on website or not

    public class Other
    {
        [Fact]
        public async Task GelbooruTagCharacter()
        {
            Assert.Equal(Search.Tag.TagType.Character, (await new Gelbooru().GetTagAsync("cirno")).type);
        }

        [Fact]
        public async Task GelbooruTagCopyright()
        {
            Assert.Equal(Search.Tag.TagType.Copyright, (await new Gelbooru().GetTagAsync("kantai_collection")).type);
        }

        [Fact]
        public async Task GelbooruTagArtist()
        {
            Assert.Equal(Search.Tag.TagType.Artist, (await new Gelbooru().GetTagAsync("mtu_(orewamuzituda)")).type);
        }

        [Fact]
        public async Task GelbooruTagMetadata()
        {
            Assert.Equal(Search.Tag.TagType.Metadata, (await new Gelbooru().GetTagAsync("uncensored")).type);
        }

        [Fact]
        public async Task GelbooruTagTrivia()
        {
            Assert.Equal(Search.Tag.TagType.Trivia, (await new Gelbooru().GetTagAsync("futanari")).type);
        }
    }
}
