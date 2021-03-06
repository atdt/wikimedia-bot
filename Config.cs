//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena benapetr@gmail.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace wmib
{
    /// <summary>
    /// Configuration
    /// </summary>
    public static class config
    {
        /// <summary>
        /// Represent a channel
        /// </summary>
        [Serializable()]
        public class channel : MarshalByRefObject
        {
            /// <summary>
            /// Channel name
            /// </summary>
            public string Name;

            /// <summary>
            /// Language
            /// </summary>
            public string Language;

            /// <summary>
            /// List of users
            /// </summary>
            public List<User> UserList = new List<User>();
            
            /// <summary>
            /// Whether the channel contains a fresh user list
            /// </summary>
            public bool FreshList = false;

            /// <summary>
            /// Deprecated
            /// </summary>
            public string LogDir;

            /// <summary>
            /// List of channels that have shared infobot db
            /// </summary>
            public List<string> SharedChans = new List<string>();

            /// <summary>
            /// Objects created by extensions
            /// </summary>
            public Dictionary<string, object> ExtensionObjects = new Dictionary<string, object>();

            private Dictionary<string, string> ExtensionData = new Dictionary<string, string>();

            /// <summary>
            /// If messages aren't sent
            /// </summary>
            public bool suppress = false;

            /// <summary>
            /// List of ignored names for infobot
            /// </summary>
            public List<string> Infobot_IgnoredNames = new List<string>();

            /// <summary>
            /// Wait time between responses to users who try to speak to the bot
            /// </summary>
            public int respond_wait = 120;

            /// <summary>
            /// Whether bot should respond to users who think that the bot is user and speak to him
            /// </summary>
            public bool respond_message = false;

            /// <summary>
            /// Time of last message received in channel
            /// </summary>
            public System.DateTime last_msg = System.DateTime.Now;
			
            /// <summary>
            /// Doesn't send any warnings on error
            /// </summary>
			public bool suppress_warnings = false;

            /// <summary>
            /// Configuration text
            /// </summary>
            private string conf = null;

            /// <summary>
            /// Whether unknown users should be ignored or not
            /// </summary>
            public bool ignore_unknown = false;

            /// <summary>
            /// Target db of shared infobot - deprecated
            /// </summary>
            public string shared = null;

            /// <summary>
            /// List of channels we share db with
            /// </summary>
            public List<config.channel> sharedlink = null;

            /// <summary>
            /// Users
            /// </summary>
            public IRCTrust Users = null;

            /// <summary>
            /// Add a line to config
            /// </summary>
            /// <param name="a">Name of key</param>
            /// <param name="b">Value</param>
            private void AddConfig(string a, string b)
            {
                conf += "\n" + a + "=" + b + ";";
            }

            /// <summary>
            /// Returns true if this channel is already existing in memory
            /// </summary>
            /// <param name="_Channel"></param>
            /// <returns></returns>
            public static bool channelExist(string _Channel)
            {
                string conf_file = variables.config + "/" + _Channel + ".setting";
                if (File.Exists(conf_file))
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Change the config
            /// </summary>
            /// <param name="name"></param>
            /// <param name="data"></param>
            public void Extension_SetConfig(string name, string data)
            {
                lock (ExtensionData)
                {
                    if (ExtensionData.ContainsKey(name))
                    {
                        ExtensionData[name] = data;
                        return;
                    }
                }
                ExtensionData.Add(name, data);
            }

            /// <summary>
            /// Retrieve a config
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public string Extension_GetConfig(string key)
            {
                lock (ExtensionData)
                {
                    if (ExtensionData.ContainsKey(key))
                    {
                        return ExtensionData[key];
                    }
                }
                return null;
            }

            /// <summary>
            /// Get an object created by extension of name
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public object RetrieveObject(string name)
            {
                try
                {
                    lock (ExtensionObjects)
                    {
                        if (ExtensionObjects.ContainsKey(name))
                        {
                            return ExtensionObjects[name];
                        }
                    }
                }
                catch (Exception er)
                {
                    core.handleException(er);
                }
                return null;
            }

            /// <summary>
            /// Remove object from memory
            /// </summary>
            /// <param name="Nm">Name of object</param>
            /// <returns></returns>
            public bool UnregisterObject(string Nm)
            {
                try
                {
                    lock (ExtensionObjects)
                    {
                        if (!ExtensionObjects.ContainsKey(Nm))
                        {
                            return true;
                        }
                        ExtensionObjects.Remove(Nm);
                        return true;
                    }
                }
                catch (Exception er)
                {
                    core.handleException(er);
                }
                return false;
            }

            /// <summary>
            /// Register a new object in memory
            /// </summary>
            /// <param name="Ob">Data</param>
            /// <param name="Nm">Name</param>
            /// <returns></returns>
            public bool RegisterObject(object Ob, string Nm)
            {
                try
                {
                    lock (ExtensionObjects)
                    {
                        if (ExtensionObjects.ContainsKey(Nm))
                        {
                            return false;
                        }
                        ExtensionObjects.Add(Nm, Ob);
                        return true;
                    }
                }
                catch (Exception er)
                {
                    core.handleException(er);
                    return false;
                }
            }

            /// <summary>
            /// Load config of channel :)
            /// </summary>
            public void LoadConfig()
            {
                string conf_file = variables.config + "/" + Name + ".setting";
                core.recoverFile(conf_file, Name);
                try
                {
                    System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                    if (!File.Exists(conf_file))
                    {
                        SaveConfig();
                        return;
                    }
                    data.Load(conf_file);
                    foreach (System.Xml.XmlNode xx in data.ChildNodes[0].ChildNodes)
                    {
                        switch (xx.Name)
                        { 
                            case "extension":
                                if (ExtensionData.ContainsKey(xx.Attributes[0].Value))
                                {
                                    ExtensionData[xx.Attributes[0].Value] = xx.Attributes[1].Value;
                                }
                                else
                                {
                                    ExtensionData.Add(xx.Attributes[0].Value, xx.Attributes[1].Value);
                                }
                                continue;
                            case "ignored":
                                Infobot_IgnoredNames.Add(xx.Attributes[1].Value);
                                continue;
                            case "sharedch":
                                SharedChans.Add(xx.Attributes[1].Value);
                                continue;

                        }
                        switch (xx.Attributes[0].Value)
                        { 
                            case "talkmode":
                                this.suppress = bool.Parse(xx.Attributes[1].Value);
                                break;
                            case "langcode":
                                this.Language = xx.Attributes[1].Value;
                                break;
                            case "respond_message":
                                this.respond_message = bool.Parse(xx.Attributes[1].Value);
                                break;
                            case "ignore-unknown":
                                this.ignore_unknown = bool.Parse(xx.Attributes[1].Value);
                                break;
                            case "suppress-warnings":
                                this.suppress_warnings = bool.Parse(xx.Attributes[1].Value);
                                break;
                            case "respond_wait":
                                this.respond_wait = int.Parse(xx.Attributes[1].Value);
                                break;
                            case "sharedinfo":
                                this.shared = xx.Attributes[1].Value;
                                break;
                        }
                    }
                }
                catch (Exception fail)
                {
                    core.Log("Unable to load the config of " + Name, true);
                    core.handleException(fail);
                }
            }

            private static void InsertData(string key, string value, ref XmlDocument document, ref XmlNode node, string Name = "local")
            {
                XmlAttribute name = document.CreateAttribute("key");
                name.Value = key;
                System.Xml.XmlAttribute kk = document.CreateAttribute("value");
                kk.Value = value;
                System.Xml.XmlNode db = document.CreateElement(Name);
                db.Attributes.Append(name);
                db.Attributes.Append(kk);
                node.AppendChild(db);
            }

            /// <summary>
            /// Save config
            /// </summary>
            public void SaveConfig()
            {
                try
                {
                    System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                    System.Xml.XmlNode xmlnode = data.CreateElement("channel");
                    InsertData("talkmode", suppress.ToString(), ref data, ref xmlnode);
                    InsertData("langcode", this.Language, ref data, ref xmlnode);
                    InsertData("respond_message", this.respond_message.ToString(), ref data, ref xmlnode);
                    InsertData("ignore-unknown", this.ignore_unknown.ToString(), ref data, ref xmlnode);
                    InsertData("suppress-warnings", this.suppress_warnings.ToString(), ref data, ref xmlnode);
                    InsertData("respond_wait", respond_wait.ToString(), ref data, ref xmlnode);
                    InsertData("sharedinfo", this.shared, ref data, ref xmlnode);
                    if (!(sharedlink.Count < 1))
                    {
                        foreach (channel current in sharedlink)
                        {
                            InsertData("name", current.Name, ref data, ref xmlnode, "sharedch");
                        }
                    }
                    if (!(Infobot_IgnoredNames.Count < 1))
                    {
                        foreach (string curr in Infobot_IgnoredNames)
                        {
                            InsertData("name", curr, ref data, ref xmlnode, "ignored");
                        }
                    }
                    if (ExtensionData.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> item in ExtensionData)
                        {
                            InsertData(item.Key, item.Value, ref data, ref xmlnode, "extension");
                        }
                    }
                    if (File.Exists(variables.config + "/" + Name + ".setting"))
                    {
                        core.backupData(variables.config + "/" + Name + ".setting");
                        if (!File.Exists(config.tempName(variables.config + "/" + Name + ".setting")))
                        {
                            core.Log("Unable to create backup file for " + Name);
                        }
                    }
                    data.AppendChild(xmlnode);
                    data.Save(variables.config + "/" + Name + ".setting");
                    if (File.Exists(config.tempName(variables.config + "/" + Name + ".setting")))
                    {
                        File.Delete(config.tempName(variables.config + "/" + Name + ".setting"));
                    }
                }
                catch (Exception)
                {
                    core.recoverFile(variables.config + "/" + Name + ".setting", Name);
                }
            }

            /// <summary>
            /// Return true if user is present in channel
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public bool containsUser(string name)
            {
                name = name.ToUpper();
                foreach (User us in UserList)
                {
                    if (name == us.Nick.ToUpper())
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Return number of channels that infobot share db with
            /// </summary>
            /// <returns></returns>
            public int Shares()
            {
                foreach (string x in SharedChans)
                {
                    string name = x.Replace(" ", "");
                    if (name != "")
                    {
                        if (core.getChannel(name) != null)
                        {
                            if (sharedlink.Contains(core.getChannel(name)) == false)
                            {
                                sharedlink.Add(core.getChannel(name));
                            }
                        }
                    }
                }
                return 0;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">Channel</param>
            public channel(string name)
            {
                Name = name;
                conf = "";
                //Info = true;
                Language = "en";
                sharedlink = new List<channel>();
                shared = "";
                suppress = false;
                //Feed = false;
                //Logged = false;
                LoadConfig();
                if (!Directory.Exists(config.path_txt))
                {
                    Directory.CreateDirectory(config.path_txt);
                }
                if (!Directory.Exists(config.path_txt + "/" + Name))
                {
                    Directory.CreateDirectory(config.path_txt + "/" + Name);
                }
                if (!Directory.Exists(config.path_htm))
                {
                    Directory.CreateDirectory(config.path_htm);
                }
                if (!Directory.Exists(config.path_htm + Path.DirectorySeparatorChar + Name))
                {
                    Directory.CreateDirectory(config.path_htm + Path.DirectorySeparatorChar + Name);
                }
                LogDir = Path.DirectorySeparatorChar + Name + Path.DirectorySeparatorChar;
                Users = new IRCTrust(Name);
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        try
                        {
                            if (module.working)
                            {
                                config.channel self = this;
                                module.Hook_Channel(self);
                            }
                        }
                        catch (Exception fail)
                        {
                            core.Log("MODULE: exception at Hook_Channel in " + module.Name, true);
                            core.handleException(fail);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is a temporary string containing the configuration data
        /// </summary>
        private static string text = null;

        /// <summary>
        /// Network the bot is connecting to
        /// </summary>
        public static string network = "irc.freenode.net";

        /// <summary>
        /// Nick name
        /// </summary>
        public static string username = "wm-bot";

        /// <summary>
        /// Uptime
        /// </summary>
        public static System.DateTime UpTime;

        /// <summary>
        /// Debug channel (doesn't need to exist)
        /// </summary>
        public static string debugchan = null;

        /// <summary>
        /// Link to css
        /// </summary>
        public static string css;

        /// <summary>
        /// Login name
        /// </summary>
        public static string login = "";

        /// <summary>
        /// Login pw
        /// </summary>
        public static string password = "";

        /// <summary>
        /// Whether the bot is using external network module
        /// </summary>
        public static bool serverIO = false;

        /// <summary>
        /// The webpages url
        /// </summary>
        public static string url = "";

        /// <summary>
        /// Dump
        /// </summary>
        public static string DumpDir = "dump";

        /// <summary>
        /// This is a log where network log is dumped to
        /// </summary>
        public static string TransactionLog = "transaction.dat";

        /// <summary>
        /// Path to txt logs
        /// </summary>
        public static string path_txt = "log";

        /// <summary>
        /// Network traffic is logged
        /// </summary>
        public static bool Logging = false;

        /// <summary>
        /// Path to html which is generated by this process
        /// </summary>
        public static string path_htm = "html";

        /// <summary>
        /// Version
        /// </summary>
        public static string version = "wikimedia bot v. 1.10.8.0";

        /// <summary>
        /// Separator for system db
        /// </summary>
        public static string separator = "|";

        /// <summary>
        /// User name
        /// </summary>
        public static string name = "wm-bot";

        /// <summary>
        /// List of channels the bot is in
        /// </summary>
        public static List<channel> channels = new List<channel>();

        /// <summary>
        /// This is a string which commands are prefixed with
        /// </summary>
        public const string CommandPrefix = "@";

        /// <summary>
        /// Add line to the config file
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private static void AddConfig(string a, string b)
        {
            text = text + "\n" + a + "=" + b + ";";
        }

        /// <summary>
        /// Save a channel config
        /// </summary>
        public static void Save()
        {
            text = "";
            AddConfig("username", username);
            AddConfig("password", password);
            AddConfig("web", url);
            AddConfig("serverIO", serverIO.ToString());
            AddConfig("debug", debugchan);
            AddConfig("network", network);
            AddConfig("style_html_file", css);
            AddConfig("nick", login);
            text += "\nchannels=";
            lock (config.channels)
            {
                foreach (channel current in channels)
                {
                    text += current.Name + ",\n";
                }
            }
            text = text + ";";
            File.WriteAllText(variables.config + "/wmib", text);
            text = null;
        }

        /// <summary>
        /// Parse config data text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string parseConfig(string text, string name)
        {
            if (text.Contains(name + "="))
            {
                string x = text;
                x = text.Substring(text.IndexOf(name + "=")).Replace(name + "=", "");
                if (x.Contains(";"))
                {
                    x = x.Substring(0, x.IndexOf(";"));
                    return x;
                }
            }
            return "";
        }

        /// <summary>
        /// Return a temporary name
        /// </summary>
        /// <param name="file">File you need to have temp for</param>
        /// <returns></returns>
        public static string tempName(string file)
        {
            return (file + "~");
        }

        /// <summary>
        /// Load config of bot
        /// </summary>
        public static int Load()
        {
            try
            {
                if (Directory.Exists(variables.config) == false)
                {
                    Directory.CreateDirectory(variables.config);
                }
                if (!System.IO.File.Exists(variables.config + "/wmib"))
                {
                    System.IO.File.WriteAllText(variables.config + "/wmib", "//this is configuration file for bot, you need to fill in some stuff for it to work");
                }
                text = File.ReadAllText(variables.config + "/wmib");
                bool _serverIO;
                if (bool.TryParse(parseConfig(text, "serverIO"), out _serverIO))
                {
                    serverIO = _serverIO;
                }
                foreach (string x in parseConfig(text, "channels").Replace("\n", "").Split(','))
                {
                    string name = x.Replace(" ", "").Replace("\n", "");
                    if (name != "")
                    {
                        lock (channels)
                        {
                            channels.Add(new channel(name));
                        }
                    }
                }
                Program.Log("Channels were all loaded");

                // Now when all chans are loaded let's link them together
                foreach (channel ch in channels)
                {
                    ch.Shares();
                }
                Program.Log("Channel db's working");
                username = parseConfig(text, "username");
                network = parseConfig(text, "network");
                login = parseConfig(text, "nick");
                debugchan = parseConfig(text, "debug");
                css = parseConfig(text, "style_html_file");
                url = parseConfig(text, "web");
                password = parseConfig(text, "password");
                if (login == "")
                {
                    Console.WriteLine("Error there is no username for bot");
                    return 1;
                }
                if (network == "")
                {
                    Console.WriteLine("Error irc server is wrong");
                    return 1;
                }
                if (username == "")
                {
                    Console.WriteLine("Error there is no username for bot");
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                core.handleException(ex);
            }
            if (!Directory.Exists(DumpDir))
            {
                Directory.CreateDirectory(DumpDir);
            }
            return 0;
        }
    }
}
