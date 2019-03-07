﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Background;
using Microsoft.Toolkit.Parsers.Rss;
using MonkeyCache.SQLite;
using SampleBackground.Models;

namespace SampleBackground.Services
{
    public abstract class BaseRssFeed : IBackgroundTask
    {
        private readonly IRssParserService _parserService;
        private readonly string _url;

        protected BaseRssFeed(int minutes, string url)
        {
            _url = url;
            _parserService = new RssParserService();
            Interval = TimeSpan.FromMinutes(minutes);
        }

        public TimeSpan Interval { get; set; }

        public async Task StartJob()
        {
            var existingList = Barrel.Current.Get<List<RssData>>("NewsFeeds") ?? new List<RssData>();

            try
            {
                var result = await _parserService.Parse(_url);

                foreach (var rssSchema in result)
                {
                    var isExist = existingList.Any(e => e.Guid == rssSchema.InternalID);

                    if (!isExist)
                    {
                        existingList.Add(new RssData
                        {
                            Title = rssSchema.Title,
                            PubDate = rssSchema.PublishDate,
                            Link = rssSchema.FeedUrl,
                            Guid = rssSchema.InternalID,
                            Author = rssSchema.Author,
                            Thumbnail = string.IsNullOrWhiteSpace(rssSchema.ImageUrl)
                                ? $"https://placeimg.com/80/80/nature"
                                : rssSchema.ImageUrl,
                            Description = rssSchema.Summary
                        });
                    }
                }

                existingList = existingList.OrderByDescending(e => e.PubDate).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            Barrel.Current.Add("NewsFeeds", existingList, TimeSpan.FromDays(30));
        }
    }
}
