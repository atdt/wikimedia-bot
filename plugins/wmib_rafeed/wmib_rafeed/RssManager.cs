﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using System.Threading;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;

namespace wmib
{
    public static class RssManager
    {
        public static bool CompareLists(List<RssFeedItem> source, List<RssFeedItem> target)
        {
            if (source == null || target == null)
            {
                return true;
            }
            if (target.Count != source.Count)
            {
                return false;
            }
            int curr = 0;
            foreach (RssFeedItem item in source)
            {
                if (item.Link != target[curr].Link || item.Description != target[curr].Description || target[curr].PublishDate != item.PublishDate)
                {
                    return false;
                }
                curr++;
            }
            return true;
        }

        public static bool ContainsItem(List<RssFeedItem> list, RssFeedItem item)
        {
            foreach (RssFeedItem Item in list)
            {
                if (Item.Link == item.Link && Item.Title == item.Title && item.Description == Item.Description && Item.PublishDate == item.PublishDate)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,
                                      System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Reads the relevant Rss feed and returns a list of RssFeedItems
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<RssFeedItem> ReadFeed(string url, Feed.item item, string channel)
        {
            try
            {
                //create a new list of the rss feed items to return
                List<RssFeedItem> rssFeedItems = new List<RssFeedItem>();

                //create a http request which will be used to retrieve the rss feed
                ServicePointManager.ServerCertificateValidationCallback = Validator;
                HttpWebRequest rssFeed = (HttpWebRequest)WebRequest.Create(url);

                XmlDocument rss = new XmlDocument();
                rss.Load(rssFeed.GetResponse().GetResponseStream());

                if (url.StartsWith("http://bugzilla.wikimedia") || url.StartsWith("https://bugzilla.wikimedia"))
                {
                    if (rss.ChildNodes[1].Name.ToLower() == "feed")
                    {
                        foreach (XmlNode entry in rss.ChildNodes[1].ChildNodes)
                        {
                            if (entry.Name == "entry")
                            {
                                RssFeedItem curr = new RssFeedItem();
                                foreach (XmlNode data in entry.ChildNodes)
                                {
                                    switch (data.Name.ToLower())
                                    {
                                        case "title":
                                            curr.Title = data.InnerText;
                                            break;
                                        case "link":
                                            foreach (XmlAttribute attribute in data.Attributes)
                                            {
                                                if (attribute.Name == "href")
                                                {
                                                    curr.Link = attribute.Value;
                                                }
                                            }
                                            break;
                                        case "author":
                                            if (data.ChildNodes.Count > 0)
                                            {
                                                curr.Author = data.ChildNodes[0].InnerText;
                                            }
                                            break;
                                        case "summary":
                                            string html = System.Web.HttpUtility.HtmlDecode(data.InnerText);
                                            if (html.Contains("<table>"))
                                            {
                                                try
                                                {
                                                    XmlDocument summary = new XmlDocument();
                                                    summary.LoadXml(html);
                                                    foreach (XmlNode tr in summary.ChildNodes[0].ChildNodes)
                                                    {
                                                        bool type = true;
                                                        string st = "";
                                                        foreach (XmlNode td in tr.ChildNodes)
                                                        {
                                                            if (type)
                                                            {
                                                                st = td.InnerText;
                                                            }
                                                            else
                                                            {
                                                                switch (st.Replace(" ", ""))
                                                                {
                                                                    case "Product":
                                                                        curr.bugzilla_product = td.InnerText;
                                                                        break;
                                                                    case "Status":
                                                                        curr.bugzilla_status = td.InnerText;
                                                                        break;
                                                                    case "Component":
                                                                        curr.bugzilla_component = td.InnerText;
                                                                        break;
                                                                    case "Assignee":
                                                                        curr.bugzilla_assignee = td.InnerText;
                                                                        break;
                                                                    case "Reporter":
                                                                        curr.bugzilla_reporter = td.InnerText;
                                                                        break;
                                                                    case "Resolution":
                                                                        curr.bugzilla_reso = td.InnerText;
                                                                        break;
                                                                    case "Priority":
                                                                        curr.bugzilla_priority = td.InnerText;
                                                                        break;
                                                                    case "Severity":
                                                                        curr.bugzilla_severity = td.InnerText;
                                                                        break;
                                                                }
                                                            }
                                                            type = !type;
                                                        }
                                                    }
                                                }
                                                catch (ThreadAbortException fail)
                                                {
                                                    throw fail;
                                                }
                                                catch (Exception fail)
                                                {
                                                    core.Log("RAFEED: " + fail.Message + fail.StackTrace);
                                                }
                                            }
                                            break;
                                        case "guid":
                                            curr.ItemId = data.InnerText;
                                            break;
                                        case "channelid":
                                            curr.ChannelId = data.InnerText;
                                            break;
                                        case "date":
                                            curr.PublishDate = data.Value;
                                            break;
                                    }
                                }
                                rssFeedItems.Add(curr);
                            }
                        }

                        return rssFeedItems;
                    }
                }

                if (rss.ChildNodes[1].Name.ToLower() == "feed")
                {
                    foreach (XmlNode entry in rss.ChildNodes[1].ChildNodes)
                    {
                        if (entry.Name == "entry")
                        {
                            RssFeedItem curr = new RssFeedItem();
                            foreach (XmlNode data in entry.ChildNodes)
                            {
                                switch (data.Name.ToLower())
                                {
                                    case "title":
                                        curr.Title = data.InnerText;
                                        break;
                                    case "link":
                                        foreach (XmlAttribute attribute in data.Attributes)
                                        {
                                            if (attribute.Name == "href")
                                            {
                                                curr.Link = attribute.Value;
                                            }
                                        }
                                        break;
                                    case "author":
                                        if (data.ChildNodes.Count > 0)
                                        {
                                            curr.Author = data.ChildNodes[0].InnerText;
                                        }
                                        break;
                                    case "summary":
                                        curr.Description = data.InnerText;
                                        break;
                                    case "guid":
                                        curr.ItemId = data.InnerText;
                                        break;
                                    case "channelid":
                                        curr.ChannelId = data.InnerText;
                                        break;
                                    case "date":
                                        curr.PublishDate = data.Value;
                                        break;
                                }
                            }
                            rssFeedItems.Add(curr);
                        }
                    }

                    return rssFeedItems;
                }

                foreach (XmlNode node in rss.ChildNodes)
                {
                    if (node.Name.ToLower() == "rss" || node.Name.ToLower() == "channel")
                    {
                        foreach (XmlNode entry in node.ChildNodes[0].ChildNodes)
                        {
                            if (entry.Name == "item")
                            {
                                RssFeedItem curr = new RssFeedItem();
                                foreach (XmlNode data in entry.ChildNodes)
                                {
                                    switch (data.Name.ToLower())
                                    {
                                        case "title":
                                            curr.Title = data.InnerText;
                                            break;
                                        case "link":
                                            curr.Link = data.InnerText;
                                            break;
                                        case "description":
                                            curr.Description = data.InnerText;
                                            break;
                                        case "guid":
                                            curr.ItemId = data.InnerText;
                                            break;
                                        case "channelid":
                                            curr.ChannelId = data.InnerText;
                                            break;
                                        case "date":
                                            curr.PublishDate = data.Value;
                                            break;
                                    }
                                }
                                rssFeedItems.Add(curr);
                            }
                        }

                        return rssFeedItems;
                    }
                }
                if (item.retries < 1)
                {
                    item.disabled = true;
                    core.irc._SlowQueue.DeliverMessage("Unable to parse the feed from " + url + " this url is probably not a valid rss, the feed will be disabled, until you re-enable it by typing @rss+ " + item.name, channel);
                    return null;
                }
                item.retries--;
                return null;
            }
            catch (ThreadAbortException fail)
            {
                throw fail;
            }
            catch (Exception fail)
            {
                core.Log("Unable to parse feed from " + url + " I will try to do that again " + item.retries.ToString() + " times", true);
                core.handleException(fail);
                if (item.retries < 1)
                {
                    item.disabled = true;
                    core.irc._SlowQueue.DeliverMessage("Unable to parse the feed from " + url + " this url is probably not a valid rss, the feed will be disabled, until you re-enable it by typing @rss+ " + item.name, channel);
                    return null;
                }
                item.retries--;
                return null;
            }
        }
    }
}
