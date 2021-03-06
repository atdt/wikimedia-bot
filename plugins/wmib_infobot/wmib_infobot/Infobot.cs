﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Text;

namespace wmib
{
    public class infobot_core
    {
        /// <summary>
        /// Data file
        /// </summary>
        public string datafile_raw = "";
        public string datafile_xml = "";
        public string temporary_data = "";
        public bool Sensitive = true;
        public bool stored = true;
        public static readonly string prefix = "!";

        [NonSerialized()]
        private Thread Th;

        [NonSerialized()]
        public Thread SnapshotManager;

        // if we need to update dump
        public bool update = true;

        /// <summary>
        /// Locked
        /// </summary>
        public bool locked = false;

        public static config.channel Reply;

        public static DateTime NA = DateTime.MaxValue;

        public class InfobotKey
        {
            /// <summary>
            /// Text
            /// </summary>
            public string Text;

            /// <summary>
            /// Key
            /// </summary>
            public string Key;

            public string user;

            public string locked;

            public DateTime created;

            public bool Raw;

            public int Displayed = 0;

            public DateTime lasttime;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Key">Key</param>
            /// <param name="Text">Text of the key</param>
            /// <param name="User">User who created the key</param>
            /// <param name="Lock">If key is locked or not</param>
            public InfobotKey(string key, string text, string User, string Lock = "false", string date = "", string time = "", int Number = 0, bool RAW = false)
            {
                Text = text;
                Key = key;
                locked = Lock;
                user = User;
                Raw = RAW;
                Displayed = Number;
                if (time == "")
                {
                    lasttime = NA;
                }
                else
                {
                    lasttime = DateTime.FromBinary(long.Parse(time));
                }
                if (date == "")
                {
                    created = DateTime.Now;
                }
                else
                {
                    created = DateTime.FromBinary(long.Parse(date));
                }
            }
        }

        public class InfobotAlias
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name;

            /// <summary>
            /// Key
            /// </summary>
            public string Key;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">Alias</param>
            /// <param name="key">Key</param>
            public InfobotAlias(string name, string key)
            {
                Name = name;
                Key = key;
            }
        }

        public class InfoItem
        {
            public config.channel Channel;
            public string User;
            public string Name;
            public string Host;
        }

        /// <summary>
        /// List of all items in class
        /// </summary>
        public List<InfobotKey> Keys = new List<InfobotKey>();

        /// <summary>
        /// List of all aliases we want to use
        /// </summary>
        public List<InfobotAlias> Alias = new List<InfobotAlias>();

        /// <summary>
        /// Channel name
        /// </summary>
        public string Channel;

        private string search_key;


        /// <summary>
        /// Load it
        /// </summary>
        public bool Load()
        {
            Keys.Clear();
            // Checking if db isn't broken
            core.recoverFile(datafile_raw, Channel);
            if (!File.Exists(datafile_raw))
            {
                return false;
            }

            string[] db = File.ReadAllLines(datafile_raw);
            foreach (string x in db)
            {
                if (x.Contains(config.separator))
                {
                    string[] info = x.Split(Char.Parse(config.separator));
                    string type = info[2];
                    string value = info[1];
                    string name = info[0];
                    if (type == "key")
                    {
                        string Locked = info[3];
                        Keys.Add(new InfobotKey(name.Replace("<separator>", "|"), value.Replace("<separator>", "|"), "", Locked, NA.ToBinary().ToString(),
                            NA.ToBinary().ToString()));
                    }
                    else
                    {
                        Alias.Add(new InfobotAlias(name.Replace("<separator>", "|"), value.Replace("<separator>", "|")));
                    }
                }
            }
            return true;
        }

        public bool AliasExist(string name, bool sensitive = true)
        {
            if (sensitive)
            {
                lock (Keys)
                {
                    foreach (InfobotAlias key in Alias)
                    {
                        if (key.Name == name)
                        {
                            return true;
                        }
                    }
                }
            }
            if (!sensitive)
            {
                name = name.ToLower();
                lock (Keys)
                {
                    foreach (InfobotAlias key in Alias)
                    {
                        if (key.Name.ToLower() == name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool KeyExist(string name, bool sensitive = true)
        {
            if (!sensitive)
            {
                name = name.ToLower();
                foreach (InfobotKey key in Keys)
                {
                    if (key.Key.ToLower() == name)
                    {
                        return true;
                    }
                }
            }
            if (sensitive)
            {
                foreach (InfobotKey key in Keys)
                {
                    if (key.Key == name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public InfobotKey GetKey(string name, bool sensitive = true)
        {
            if (!sensitive)
            {
                name = name.ToLower();
                foreach (InfobotKey key in Keys)
                {
                    if (key.Key.ToLower() == name)
                    {
                        return key;
                    }
                }
            }
            if (sensitive)
            {
                foreach (InfobotKey key in Keys)
                {
                    if (key.Key == name)
                    {
                        return key;
                    }
                }
            }
            return null;
        }

        public bool LoadData()
        {
            Keys.Clear();
            // Checking if db isn't broken
            core.recoverFile(datafile_xml, Channel);
            if (Load())
            {
                core.Log("Obsolete database found for " + Channel + " converting to new format");
                Save();
                File.Delete(datafile_raw);
                return true;
            }
            if (!File.Exists(datafile_xml))
            {
                // Create db
                Save();
                return true;
            }
            try
            {
                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                if (!File.Exists(datafile_xml))
                {
                    Keys.Clear();
                    return true;
                }
                data.Load(datafile_xml);
                lock (Keys)
                {
                    lock (Alias)
                    {
                        Keys.Clear();
                        Alias.Clear();
                        foreach (System.Xml.XmlNode xx in data.ChildNodes[0].ChildNodes)
                        {
                            if (xx.Name == "alias")
                            {
                                InfobotAlias _Alias = new InfobotAlias(xx.Attributes[0].Value, xx.Attributes[1].Value);
                                Alias.Add(_Alias);
                                continue;
                            }
                            bool raw = false;
                            if (xx.Attributes.Count > 6)
                            {
                                raw = bool.Parse(xx.Attributes[6].Value);
                            }
                            InfobotKey _key = new InfobotKey(xx.Attributes[0].Value, xx.Attributes[1].Value, xx.Attributes[2].Value, "false", xx.Attributes[3].Value,
                            xx.Attributes[4].Value, int.Parse(xx.Attributes[5].Value), raw);
                            Keys.Add(_key);
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return true;
        }

        public void Info(string key, config.channel chan)
        {
            InfobotKey CV = GetKey(key, Sensitive);
            if (CV == null)
            {
                core.irc._SlowQueue.DeliverMessage("There is no such a key", chan.Name, IRC.priority.low);
                return;
            }
                if (CV.Key == key)
                {
                    string created = "N/A";
                    string last = "N/A";
                    string name = "N/A";
                    if (CV.lasttime != NA)
                    {
                        TimeSpan span = DateTime.Now - CV.lasttime;
                        last = CV.lasttime.ToString() + " (" + span.ToString() + " ago)";
                    }
                    if (CV.created != NA)
                    {
                        created = CV.created.ToString();
                    }
                    if (CV.user != "")
                    {
                        name = CV.user;
                    }
                    string type = " this key is normal";
                    if (CV.Raw)
                    {
                        type = " this key is raw";
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("infobot-data", chan.Language, new List<string> {key, name, created, CV.Displayed.ToString(),
                        last + type }), chan.Name, IRC.priority.low);
                    return;
                }
        }

        public List<InfobotKey> SortedItem()
        {
            List<InfobotKey> OriginalList = new List<InfobotKey>();
            List<InfobotKey> Item = new List<InfobotKey>();
            locked = true;
            OriginalList.AddRange(Keys);
            locked = false;
            try
            {
                if (Keys.Count > 0)
                {
                    List<string> Name = new List<string>();
                    foreach (InfobotKey curr in OriginalList)
                    {
                        Name.Add(curr.Key);
                    }
                    Name.Sort();
                    foreach (string f in Name)
                    {
                        foreach (InfobotKey g in OriginalList)
                        {
                            if (f == g.Key)
                            {
                                Item.Add(g);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                core.Log("Exception while creating list for html");
                locked = false;
            }
            return Item;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="database"></param>
        /// <param name="channel"></param>
        public infobot_core(string database, string channel, bool sensitive = true)
        {
            Sensitive = sensitive;
            datafile_xml = database + ".xml";
            datafile_raw = database;
            Channel = channel;
            LoadData();
        }

        public static string parseInfo(string key, string[] pars, string original, InfobotKey Key)
        {
            string keyv = key;
            bool raw = false;
            if (Key != null)
            {
                raw = Key.Raw;
            }
            if (pars.Length > 1)
            {
                string keys = "";
                int curr = 1;
                while (pars.Length > curr)
                {
                    if (!raw)
                    {
                        keyv = keyv.Replace("$" + curr.ToString(), pars[curr]);
                        keyv = keyv.Replace("$url_encoded_" + curr.ToString(), System.Web.HttpUtility.UrlEncode(pars[curr]));
                    }
                    if (keys == "")
                    {
                        keys = pars[curr];
                    }
                    else
                    {
                        keys = keys + " " + pars[curr];
                    }
                    curr++;
                }
                keyv = keyv.Replace("$*", original);
                keyv = keyv.Replace("$url_encoded_*", System.Web.HttpUtility.UrlEncode(original));
            }
            return keyv;
        }

        public static bool Linkable(config.channel host, config.channel guest)
        {
            if (host == null)
            {
                return false;
            }
            if (guest == null)
            {
                return false;
            }
            if (host.sharedlink.Contains(guest))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Save to a file
        /// </summary>
        public void Save()
        {
            if (locked)
            {
                core.Log("Unable to save " + Channel + " because the db is locked by some module", true);
                return;
            }
            update = true;
            try
            {
                if (File.Exists(datafile_xml))
                {
                    core.backupData(datafile_xml);
                    if (!File.Exists(config.tempName(datafile_xml)))
                    {
                        core.Log("Unable to create backup file for " + this.Channel);
                    }
                }
                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                System.Xml.XmlNode xmlnode = data.CreateElement("database");

                lock (Alias)
                {
                    foreach (InfobotAlias key in Alias)
                    {
                        System.Xml.XmlAttribute name = data.CreateAttribute("alias_key_name");
                        name.Value = key.Name;
                        System.Xml.XmlAttribute kk = data.CreateAttribute("alias_key_key");
                        kk.Value = key.Key;
                        System.Xml.XmlAttribute created = data.CreateAttribute("date");
                        created.Value = "";
                        System.Xml.XmlNode db = data.CreateElement("alias");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(created);
                        xmlnode.AppendChild(db);
                    }
                }
                lock (Keys)
                {
                    foreach (InfobotKey key in Keys)
                    {
                        System.Xml.XmlAttribute name = data.CreateAttribute("key_name");
                        name.Value = key.Key;
                        System.Xml.XmlAttribute kk = data.CreateAttribute("data");
                        kk.Value = key.Text;
                        System.Xml.XmlAttribute created = data.CreateAttribute("created_date");
                        created.Value = key.created.ToBinary().ToString();
                        System.Xml.XmlAttribute nick = data.CreateAttribute("nickname");
                        nick.Value = key.user;
                        System.Xml.XmlAttribute last = data.CreateAttribute("touched");
                        last.Value = key.lasttime.ToBinary().ToString();
                        System.Xml.XmlAttribute triggered = data.CreateAttribute("triggered");
                        triggered.Value = key.Displayed.ToString();
                        XmlAttribute k = data.CreateAttribute("raw");
                        k.Value = key.Raw.ToString();
                        System.Xml.XmlNode db = data.CreateElement("key");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(nick);
                        db.Attributes.Append(created);
                        db.Attributes.Append(last);
                        db.Attributes.Append(triggered);
                        db.Attributes.Append(k);
                        xmlnode.AppendChild(db);
                    }
                }

                data.AppendChild(xmlnode);
                data.Save(datafile_xml);
                if (File.Exists(config.tempName(datafile_xml)))
                {
                    File.Delete(config.tempName(datafile_xml));
                }
            }
            catch (Exception b)
            {
                try
                {
                    if (core.recoverFile(datafile_xml, Channel))
                    {
                        core.Log("Recovered db for channel " + Channel);
                    }
                    else
                    {
                        core.handleException(b, Channel);
                    }
                }
                catch (Exception bb)
                {
                    core.handleException(bb, Channel);
                }
            }
        }

        /// <summary>
        /// Get value of key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public string getValue(string key)
        {
            if (Sensitive)
            {
                foreach (InfobotKey data in Keys)
                {
                    if (data.Key == key)
                    {
                        data.lasttime = DateTime.Now;
                        data.Displayed++;
                        stored = false;
                        return data.Text;
                    }
                }
                return "";
            }
            string key2 = key.ToLower();
            foreach (InfobotKey data in Keys)
            {
                if (data.Key.ToLower() == key2)
                {
                    data.lasttime = DateTime.Now;
                    data.Displayed++;
                    stored = false;
                    return data.Text;
                }
            }
            return "";
        }

        /// <summary>
        /// Print a value to channel if found, this message doesn't need to be a valid command for it to work
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="user">User</param>
        /// <param name="chan">Channel</param>
        /// <param name="host">Host name</param>
        /// <returns></returns>
        public bool print(string name, string user, config.channel chan, string host)
        {
            try
            {
                // check if it starts with the prefix
                if (!name.StartsWith(prefix))
                {
                    return true;
                }
                // check if this channel is allowed to access the db
                config.channel data = isAllowed(chan);
                bool Allowed = (data != null);
                // handle prefix
                name = name.Substring(1);
                infobot_core infobot = null;

                if (Allowed)
                {
                    infobot = (infobot_core)data.RetrieveObject("Infobot");
                }

                // check if key is ignored
                string ignore_test = name;
                if (ignore_test.Contains(" "))
                {
                    ignore_test = ignore_test.Substring(0, ignore_test.IndexOf(" "));
                }
                if (chan.Infobot_IgnoredNames.Contains(ignore_test))
                {
                    return true;
                }

                // check if key has some parameters or command
                if (name.Contains(" "))
                {
                    // split by parameters so we can easily get the arguments user provided
                    string[] parm = name.Split(' ');
                    // someone want to create a new key
                    if (parm[1] == "is")
                    {
                        // check if they are approved to do that
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                // check if we can deliver error message
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            // they can but there is only 1 parameter and we need at least 2
                            if (parm.Length < 3)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("key", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            // get a key name
                            string key = name.Substring(name.IndexOf(" is") + 4);
                            // check if there is pipe symbol in the key, which is not a valid symbol
                            if (parm[0].Contains("|"))
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage("Invalid symbol in the key", chan.Name);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.setKey(key, parm[0], user, chan);
                            }
                        }
                        else
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                            }
                        }
                        return false;
                    }
                    // alias
                    bool force = false;
                    if (parm[1] == "alias" || parm[1] == "force-alias")
                    {
                        if (parm[1] == "force-alias")
                        {
                            force = true;
                        }
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (parm.Length < 3)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc.Message(messages.get("InvalidAlias", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.aliasKey(name.Substring(name.IndexOf(" alias") + 7), parm[0], "", chan, force);
                                return true;
                            }
                        }
                        else
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                            }
                        }
                        return false;
                    }
                    if (parm[1] == "unalias")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                lock (infobot.Alias)
                                {
                                    foreach (InfobotAlias b in infobot.Alias)
                                    {
                                        if (b.Name == parm[0])
                                        {
                                            infobot.Alias.Remove(b);
                                            core.irc.Message(messages.get("AliasRemoved", chan.Language), chan.Name);
                                            infobot.stored = false;
                                            return false;
                                        }
                                    }
                                }
                            }
                            return false;
                        }
                        if (!chan.suppress_warnings)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                        }
                        return false;
                    }
                    // remove key
                    if (parm[1] == "del")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.rmKey(parm[0], "", chan);
                            }
                        }
                        else
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                            }
                        }
                        return false;
                    }
                }
                if (!Allowed)
                {
                    return true;
                }
                string User = "";
                string original = name;
                if (original.Contains(" "))
                {
                    original = original.Substring(original.IndexOf(" ") + 1);
                }
                if (name.Contains("|"))
                {
                    User = name.Substring(name.IndexOf("|") + 1);
                    if (Module.GetConfig(chan, "Infobot.Trim-white-space-in-name", true))
                    {
                        if (User.EndsWith(" "))
                        {
                            while (User.EndsWith(" "))
                            {
                                User = User.Substring(0, User.Length - 1);
                            }
                        }
                        if (User.StartsWith(" "))
                        {
                            while (User.StartsWith(" "))
                            {
                                User = User.Substring(1);
                            }
                        }
                    }
                    name = name.Substring(0, name.IndexOf("|"));
                }
                string[] p = name.Split(' ');
                int parameters = p.Length;
                string keyv = "";
                if (infobot != null)
                {
                    keyv = infobot.getValue(p[0]);
                }
                InfobotKey _key = GetKey(p[0]);
                bool raw = false;
                if (_key != null)
                {
                    if (_key.Raw)
                    {
                        raw = _key.Raw;
                        name = original;
                        User = "";
                    }
                }
                if (keyv != "")
                {
                    keyv = parseInfo(keyv, p, original, _key);
                    if (User == "")
                    {
                        core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                    }
                    return true;
                }
                if (infobot != null)
                {
                    foreach (InfobotAlias b in infobot.Alias)
                    {
                        if (Sensitive)
                        {
                            if (b.Name == p[0])
                            {
                                keyv = infobot.getValue(b.Key);
                                if (keyv != "")
                                {
                                    keyv = parseInfo(keyv, p, original, _key);
                                    if (User == "")
                                    {
                                        core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                    }
                                    else
                                    {
                                        core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                    }
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            if (b.Name.ToLower() == p[0].ToLower())
                            {
                                keyv = infobot.getValue(b.Key);
                                if (keyv != "")
                                {
                                    keyv = parseInfo(keyv, p, original, _key);
                                    if (User == "")
                                    {
                                        core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                    }
                                    else
                                    {
                                        core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
                if (Module.GetConfig(chan, "Infobot.auto-complete", false))
                {
                    if (infobot != null)
                    {
                        List<string> results = new List<string>();
                        foreach (InfobotKey f in infobot.Keys)
                        {
                            if (!results.Contains(f.Key) && f.Key.StartsWith(p[0]))
                            {
                                results.Add(f.Key);
                            }
                        }
                        foreach (InfobotAlias f in infobot.Alias)
                        {
                            if (!results.Contains(f.Key) && f.Key.StartsWith(p[0]))
                            {
                                results.Add(f.Key);
                            }
                        }

                        if (results.Count == 1)
                        {
                            keyv = infobot.getValue(results[0]);
                            if (keyv != "")
                            {
                                keyv = parseInfo(keyv, p, original, _key);
                                if (User == "")
                                {
                                    core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                }
                                else
                                {
                                    core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                }
                                return true;
                            }
                            foreach (InfobotAlias alias in infobot.Alias)
                            {
                                if (alias.Name == p[0])
                                {
                                    keyv = infobot.getValue(alias.Key);
                                    if (keyv != "")
                                    {
                                        keyv = parseInfo(keyv, p, original, _key);
                                        if (User == "")
                                        {
                                            core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                        }
                                        else
                                        {
                                            core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                        }
                                        return true;
                                    }
                                }
                            }
                        }

                        if (results.Count > 1)
                        {
                            if (Module.GetConfig(chan, "Infobot.Sorted", false))
                            {
                                results.Sort();
                            }
                            string x = "";
                            foreach (string ix in results)
                            {
                                x += ix + ", ";
                            }
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-c-e", chan.Language, new List<string>() { x }), chan.Name);
                            return true;
                        }
                    }
                }

                if (Module.GetConfig(chan, "Infobot.Help", false) && infobot != null)
                {
                    List<string> Sugg = new List<string>();
                    p[0] = p[0].ToLower();
                    foreach (InfobotKey f in infobot.Keys)
                    {
                        if (!Sugg.Contains(f.Key) && (f.Text.Contains(p[0]) || f.Key.ToLower().Contains(p[0])))
                        {
                            Sugg.Add(f.Key);
                        }
                    }

                    if (Sugg.Count > 0)
                    {
                        string x = "";
                        if (Module.GetConfig(chan, "Infobot.Sorted", false))
                        {
                            Sugg.Sort();
                        }
                        foreach (string a in Sugg)
                        {
                            x += "!" + a + ", ";
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot-help", chan.Language, new List<string>() { x }), chan.Name);
                        return true;
                    }
                }
            }
            catch (Exception b)
            {
                core.handleException(b);
            }
            return true;
        }

        private void StartSearch()
        {
            Regex value = new Regex(search_key, RegexOptions.Compiled);
            config.channel _channel = core.getChannel(Channel);
            string results = "";
            int count = 0;
            lock (Keys)
            {
                foreach (InfobotKey data in Keys)
                {
                    if (data.Key == search_key || value.Match(data.Text).Success)
                    {
                        count++;
                        results = results + data.Key + ", ";
                    }
                }
            }
            if (results == "")
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("ResultsWereNotFound", Reply.Language), Reply.Name);
            }
            else
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("Results", _channel.Language, new List<string> { count.ToString() }) + results, Reply.Name);
            }
            RegularModule.running = false;
        }

        /// <summary>
        /// Search
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="Chan"></param>
        public void RSearch(string key, config.channel Chan)
        {
            if (!key.StartsWith("@regsearch"))
            {
                return;
            }
            if (!misc.IsValidRegex(key))
            {
                core.irc.Message(messages.get("Error1", Chan.Language), Chan.Name);
                return;
            }
            if (key.Length < 11)
            {
                core.irc.Message(messages.get("Search1", Chan.Language), Chan.Name);
                return;
            }
            config.channel data = isAllowed(Chan);
            bool Allowed = (data != null);
            if (!Allowed)
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("db7", Chan.Language), Chan.Name);
                return;
            }
            infobot_core infobot = (infobot_core)data.RetrieveObject("Infobot");
            if (infobot == null)
            {
                core.Log("Unable to perform regsearch because the Infobot doesn't exist in " + Chan.Name, true);
                return;
            }
            infobot.search_key = key.Substring(11);
            RegularModule.running = true;
            Reply = Chan;
            Th = new Thread(infobot.StartSearch);
            Th.Start();
            int check = 1;
            while (RegularModule.running)
            {
                check++;
                Thread.Sleep(100);
                if (check > 8)
                {
                    Th.Abort();
                    core.irc.Message(messages.get("Error2", Chan.Language), Chan.Name);
                    RegularModule.running = false;
                    return;
                }
            }
        }

        public void Find(string key, config.channel Chan)
        {
            if (Chan == null)
            {
                return;
            }
            if (!key.StartsWith("@search"))
            {
                return;
            }
            config.channel data = isAllowed(Chan);
            bool Allowed = (data != null);
            if (!Allowed)
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("db7", Chan.Language), Chan.Name);
                return;
            }
            if (key.Length < 9)
            {
                core.irc.Message(messages.get("Error1", Chan.Language), Chan.Name);
                return;
            }
            key = key.Substring(8);
            int count = 0;
            infobot_core infobot = (infobot_core)data.RetrieveObject("Infobot");
            if (infobot == null)
            {
                core.Log("Unable to perform regsearch because the Infobot doesn't exist in " + Chan.Name, true);
                return;
            }
            string results = "";
            lock (infobot.Keys)
            {
                foreach (InfobotKey Data in infobot.Keys)
                {
                    if (Data.Key == key || Data.Text.Contains(key))
                    {
                        results = results + Data.Key + ", ";
                        count++;
                    }
                }
            }
            if (results == "")
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("ResultsWereNotFound", Chan.Language), Chan.Name);
            }
            else
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("Results", Chan.Language, new List<string> { count.ToString() }) + results, Chan.Name);
            }
        }

        private config.channel isAllowed(config.channel chan)
        {
            bool Allowed;
            config.channel data = null;
            if (chan == null)
            {
                return chan;
            }
            if (chan.shared == "local" || chan.shared == "")
            {
                data = chan;
                Allowed = true;
            }
            else
            {
                Allowed = Linkable(core.getChannel(chan.shared), chan);
                if (Allowed != false)
                {
                    data = core.getChannel(chan.shared);
                }
                if (data == null)
                {
                    Allowed = false;
                }
            }
            if (Allowed)
            {
                return data;
            }
            return null;
        }

        public void setRaw(string key, string user, config.channel chan)
        {
            while (locked)
            {
                Thread.Sleep(200);
            }
            InfobotKey Key = GetKey(key, Sensitive);
            if (Key == null)
            {
                core.irc._SlowQueue.DeliverMessage("There is no such a key, " + user, chan.Name);
                return;
            }
            Key.Raw = true;
            core.irc._SlowQueue.DeliverMessage("This key will be displayed with no extra styling, variables and will ignore all symbols", chan.Name);
            stored = false;
        }

        public void unsetRaw(string key, string user, config.channel chan)
        {
            while (locked)
            {
                Thread.Sleep(200);
            }
            InfobotKey Key = GetKey(key, Sensitive);
            if (Key == null)
            {
                core.irc._SlowQueue.DeliverMessage("There is no such a key, " + user, chan.Name);
                return;
            }
            Key.Raw = false;
            core.irc._SlowQueue.DeliverMessage("This key will be displayed normally", chan.Name);
            stored = false;
        }

        /// <summary>
        /// Save a new key
        /// </summary>
        /// <param name="Text">Text</param>
        /// <param name="key">Key</param>
        /// <param name="user">User who created it</param>
        public void setKey(string Text, string key, string user, config.channel chan)
        {
            while (locked)
            {
                Thread.Sleep(200);
            }
            lock (Keys)
            {
                config.channel ch = core.getChannel(Channel);
                try
                {
                    if (KeyExist(key, Sensitive))
                    {
                        if (!chan.suppress_warnings)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Error3", chan.Language), chan.Name);
                        }
                        return;
                    }
                    Keys.Add(new InfobotKey(key, Text, user, "false"));
                    core.irc.Message(messages.get("infobot6", chan.Language), chan.Name);
                    infobot_core infobot = (infobot_core)ch.RetrieveObject("Infobot");
                    if (infobot == null)
                    {
                        core.Log("Unable to save the key because the Infobot doesn't exist in " + ch.Name, true);
                        return;
                    }
                    infobot.stored = false;
                }
                catch (Exception b)
                {
                    core.handleException(b);
                }
            }
        }

        public void SnapshotStart()
        {
            try
            {
                while (!this.stored)
                {
                    Thread.Sleep(100);
                }
                locked = true;
                lock (this.Alias)
                {
                    lock (this.Keys)
                    {
                        DateTime creationdate = DateTime.Now;
                        core.Log("Creating snapshot " + temporary_data);
                        File.Copy(datafile_xml, temporary_data);
                        locked = false;
                        core.irc._SlowQueue.DeliverMessage("Snapshot " + temporary_data + " was created for current database as of " + creationdate.ToString(), Channel);
                    }
                }
            }
            catch (Exception fail)
            {
                core.Log("Unable to create a snapshot for " + Channel, true);
                core.handleException(fail);
            }
        }

        public void RecoverStart()
        {
            try
            {
                while (!this.stored)
                {
                    Thread.Sleep(100);
                }
                locked = true;
                lock (this.Alias)
                {
                    lock (this.Keys)
                    {
                        core.Log("Recovering snapshot " + temporary_data);
                        File.Copy(temporary_data, datafile_xml, true);
                        this.Keys.Clear();
                        this.Alias.Clear();
                        core.Log("Loading snapshot of " + Channel);
                        LoadData();
                        locked = false;
                        core.irc._SlowQueue.DeliverMessage("Snapshot " + temporary_data + " was loaded and previous database was permanently deleted", Channel);
                    }
                }
            }
            catch (Exception fail)
            {
                core.Log("Unable to recover a snapshot for " + Channel + " the db is likely broken now", true);
                core.handleException(fail);
            }
        }

        public bool isValid(string name)
        {
            if (name == "")
            {
                return false;
            }
            foreach (char i in name)
            {
                if (i == '\0')
                {
                    continue;
                }
                if (((int)i) < 48)
                {
                    return false;
                }
                if (((int)i) > 122)
                {
                    return false;
                }
                if (((int)i) > 90)
                {
                    if (((int)i) < 97)
                    {
                        return false;
                    }
                }
                if (((int)i) > 57)
                {
                    if (((int)i) < 65)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void RecoverSnapshot(config.channel chan, string name)
        {
            try
            {
                if (!isValid(name))
                {
                    core.irc._SlowQueue.DeliverMessage("This is not a valid name for snapshot, you can only use a-zA-Z and 0-9 chars", chan.Name);
                    return;
                }
                if (SnapshotManager != null)
                {
                    if (SnapshotManager.ThreadState == ThreadState.Running)
                    {
                        core.irc._SlowQueue.DeliverMessage("There is already another snapshot operation running for this channel", chan.Name);
                        return;
                    }
                }
                if (locked)
                {
                    core.irc._SlowQueue.DeliverMessage("There is already another datafile operation running for this channel", chan.Name);
                    return;
                }
                locked = true;
                string datafile = RegularModule.SnapshotsDirectory + Path.DirectorySeparatorChar + Channel + Path.DirectorySeparatorChar + name;
                if (!File.Exists(datafile))
                {
                    core.irc._SlowQueue.DeliverMessage("The requested datafile " + name + " was not found", chan.Name, IRC.priority.low);
                    return;
                }

                SnapshotManager = new Thread(RecoverStart);
                temporary_data = datafile;
                SnapshotManager.Name = "Snapshot";
                SnapshotManager.Start();
                RegularModule.SetConfig(chan, "HTML.Update", true);
            }
            catch (Exception fail)
            {
                core.handleException(fail);
                locked = false;
            }
        }

        public void CreateSnapshot(config.channel chan, string name)
        {
            try
            {
                if (!isValid(name))
                {
                    core.irc._SlowQueue.DeliverMessage("This is not a valid name for snapshot, you can only use a-zA-Z and 0-9 chars", chan.Name);
                    return;
                }
                if (SnapshotManager != null)
                {
                    if (SnapshotManager.ThreadState == ThreadState.Running)
                    {
                        core.irc._SlowQueue.DeliverMessage("There is already another snapshot operation running for this channel", chan.Name);
                        return;
                    }
                }
                if (locked)
                {
                    core.irc._SlowQueue.DeliverMessage("There is already another datafile operation running for this channel", chan.Name);
                    return;
                }
                locked = true;
                string datafile = RegularModule.SnapshotsDirectory + Path.DirectorySeparatorChar + Channel + Path.DirectorySeparatorChar + name;
                if (File.Exists(datafile))
                {
                    core.irc._SlowQueue.DeliverMessage("The requested snapshot " + name + " already exist", chan.Name, IRC.priority.low);
                    return;
                }
                SnapshotManager = new Thread(SnapshotStart);
                temporary_data = datafile;
                SnapshotManager.Name = "Snapshot";
                SnapshotManager.Start();
            }
            catch (Exception fail)
            {
                core.handleException(fail);
                locked = false;
            }
        }

        /// <summary>
        /// Alias
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="al">Alias</param>
        /// <param name="user">User</param>
        public void aliasKey(string key, string al, string user, config.channel chan, bool enforced = false)
        {
            config.channel ch = core.getChannel(Channel);
            if (ch == null)
            {
                return;
            }
            lock (Alias)
            {
                foreach (InfobotAlias stakey in Alias)
                {
                    if (stakey.Name == al)
                    {
                        if (!chan.suppress_warnings)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot7", chan.Language), chan.Name);
                        }
                        return;
                    }
                }
                if (!KeyExist(key))
                {
                    if (!enforced)
                    {
                        if (AliasExist(key))
                        {
                            core.irc._SlowQueue.DeliverMessage("Unable to create alias for " + key + " because the target is alias, but not a key, if you really want to create this broken alias do !" + al + " force-alias " + key, chan.Name);
                            return;
                        }
                        core.irc._SlowQueue.DeliverMessage("Unable to create alias for " + key + " because there is no such key, if you really want to create this broken alias do !" + al + " force-alias " + key, chan.Name);
                        return;
                    }
                }
                Alias.Add(new InfobotAlias(al, key));
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("infobot8", chan.Language), chan.Name);
            stored = false;
        }

        public void rmKey(string key, string user, config.channel _ch)
        {
            config.channel ch = core.getChannel(Channel);
            while (locked)
            {
                Thread.Sleep(200);
            }
            lock (Keys)
            {
                foreach (InfobotKey keys in Keys)
                {
                    if (Sensitive)
                    {
                        if (keys.Key == key)
                        {
                            Keys.Remove(keys);
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot9", _ch.Language) + key, _ch.Name);
                            stored = false;
                            return;
                        }
                    }
                    else
                    {
                        if (keys.Key.ToLower() == key.ToLower())
                        {
                            Keys.Remove(keys);
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot9", _ch.Language) + key, _ch.Name);
                            stored = false;
                            return;
                        }
                    }
                }
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("infobot10", _ch.Language), _ch.Name);
        }
    }
}
