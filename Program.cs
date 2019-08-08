using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ModManagerInstall
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}


namespace ModManagerNet
{
        public class ModManager
    {
        public class ModInfo : IEquatable<ModInfo>
        {
            public string Id;

            public string DisplayName;

            public string Author;

            public string Version;

            public string ManagerVersion;

            public string[] Requirements;

            public string AssemblyName;

            public string EntryMethod;

            public string HomePage;

            public string Repository;

            public static implicit operator bool(ModInfo exists)
            {
                return exists != null;
            }

            public bool Equals(ModInfo other)
            {
                return Id.Equals(other.Id);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                ModInfo other;
                if ((other = (obj as ModInfo)) != null)
                    return Equals(other);
                return false;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        public static readonly string modsPath = Path.Combine(Environment.CurrentDirectory, "Mods");

        public static Version GetVersion()
        {
            return mVersion;
        }

        public class ModEntry
        {
            public class ModLogger
            {
                protected readonly string Prefix;

                protected readonly string PrefixError;

                protected readonly string PrefixCritical;

                protected readonly string PrefixWarning;

                public ModLogger(string Id)
                {
                    Prefix = $"[{Id}] ";
                    PrefixError = $"[{Id}] [Error] ";
                    PrefixCritical = $"[{Id}] [Critical] ";
                    PrefixWarning = $"[{Id}] [Warning] ";
                }

                public void Log(string str)
                {
                    ModManager.Logger.Log(str, Prefix);
                }

                public void Error(string str)
                {
                    ModManager.Logger.Log(str, PrefixError);
                }

                public void Critical(string str)
                {
                    ModManager.Logger.Log(str, PrefixCritical);
                }

                public void Warning(string str)
                {
                    ModManager.Logger.Log(str, PrefixWarning);
                }
            }

            public readonly ModInfo Info;

            public readonly string Path;

            private Assembly mAssembly;

            public readonly Version Version;

            public readonly Version ManagerVersion;

            public Version NewestVersion;

            public readonly Dictionary<string, Version> Requirements = new Dictionary<string, Version>();

            public readonly ModLogger Logger;

            public bool HasUpdate;

            public Func<ModEntry, bool, bool> OnToggle;

            public Action<ModEntry> OnGUI;

            public Action<ModEntry> OnSaveGUI;

            private Dictionary<long, MethodInfo> mCache = new Dictionary<long, MethodInfo>();

            private bool mStarted;

            private bool mErrorOnLoading;

            public bool Enabled = true;

            private bool mActive;

            public Assembly Assembly => mAssembly;

            public bool Started => mStarted;

            public bool ErrorOnLoading => mErrorOnLoading;

            public bool Toggleable => OnToggle != null;

            public bool Active
            {
                get
                {
                    return mActive;
                }
                set
                {
                    if (mStarted && !mErrorOnLoading)
                        try
                        {
                            if (value)
                            {
                                if (!mActive && (OnToggle == null || OnToggle(this, arg2: true)))
                                {
                                    mActive = true;
                                    Logger.Log("Active.");
                                }
                            }
                            else if (mActive && OnToggle != null && OnToggle(this, arg2: false))
                            {
                                mActive = false;
                                Logger.Log("Inactive.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error trying to call 'OnToggle' function.");
                            Logger.Error(ex.ToString());
                        }
                }
            }

            public ModEntry(ModInfo info, string path)
            {
                Info = info;
                Path = path;
                Logger = new ModLogger(Info.Id);
                Version = ParseVersion(info.Version);
                ManagerVersion = ((!string.IsNullOrEmpty(info.ManagerVersion)) ? ParseVersion(info.ManagerVersion) : new Version());
                if (info.Requirements == null || info.Requirements.Length == 0)
                    return;
                Regex regex = new Regex("(.*)-(\\d\\.\\d\\.\\d).*");
                string[] requirements = info.Requirements;
                foreach (string text in requirements)
                {
                    Match match = regex.Match(text);
                    if (match.Success)
                        Requirements.Add(match.Groups[1].Value, ParseVersion(match.Groups[2].Value));
                    else if (!Requirements.ContainsKey(text))
                    {
                        Requirements.Add(text, null);
                    }
                }
            }

            public bool Load()
            {
                if (mStarted)
                {
                    if (mErrorOnLoading)
                        return false;
                    return true;
                }
                mErrorOnLoading = false;
                Logger.Log($"Version '{Info.Version}'. Loading.");
                if (string.IsNullOrEmpty(Info.AssemblyName))
                {
                    mErrorOnLoading = true;
                    Logger.Error(string.Format("{0} is null.", "AssemblyName"));
                }
                if (string.IsNullOrEmpty(Info.EntryMethod))
                {
                    mErrorOnLoading = true;
                    Logger.Error(string.Format("{0} is null.", "EntryMethod"));
                }
                if (!string.IsNullOrEmpty(Info.ManagerVersion) && ManagerVersion > GetVersion())
                {
                    mErrorOnLoading = true;
                    Logger.Error($"Mod Manager must be version '{Info.ManagerVersion}' or higher.");
                }
                if (Requirements.Count > 0)
                {
                    foreach (KeyValuePair<string, Version> requirement in Requirements)
                    {
                        string key = requirement.Key;
                        ModEntry modEntry = FindMod(key);
                        if (modEntry == null || modEntry.Assembly == null)
                        {
                            mErrorOnLoading = true;
                            Logger.Error($"Required mod '{key}' not loaded.");
                        }
                        else if (!modEntry.Enabled)
                        {
                            mErrorOnLoading = true;
                            Logger.Error($"Required mod '{key}' disabled.");
                        }
                        else if (!modEntry.Active)
                        {
                            Logger.Log($"Required mod '{key}' inactive.");
                        }
                        else if (requirement.Value != null && requirement.Value > modEntry.Version)
                        {
                            mErrorOnLoading = true;
                            Logger.Error($"Required mod '{key}' must be version '{requirement.Value}' or higher.");
                        }
                    }
                }
                if (mErrorOnLoading)
                    return false;
                string text = System.IO.Path.Combine(Path, Info.AssemblyName);
                if (File.Exists(text))
                {
                    try
                    {
                        if (mAssembly == null)
                            mAssembly = Assembly.LoadFile(text);
                    }
                    catch (Exception ex)
                    {
                        mErrorOnLoading = true;
                        Logger.Error($"Error loading file '{text}'.");
                        Logger.Error(ex.ToString());
                        return false;
                    }
                    try
                    {
                        object[] param = new object[1]
                        {
                    this
                        };
                        Type[] types = new Type[1]
                        {
                    typeof(ModEntry)
                        };
                        if (FindMethod(Info.EntryMethod, types, showLog: false) == null)
                        {
                            param = null;
                            types = null;
                        }
                        if (!Invoke(Info.EntryMethod, out object result, param, types) || (result != null && !(bool)result))
                        {
                            mErrorOnLoading = true;
                            Logger.Log("Not loaded.");
                        }
                    }
                    catch (Exception ex2)
                    {
                        mErrorOnLoading = true;
                        Logger.Error($"Error loading file '{text}'.");
                        Logger.Error(ex2.ToString());
                        return false;
                    }
                    mStarted = true;
                    if (!mErrorOnLoading && Enabled)
                    {
                        Active = true;
                        return true;
                    }
                }
                else
                {
                    mErrorOnLoading = true;
                    Logger.Error($"'{text}' not found.");
                }
                return false;
            }

            public bool Invoke(string namespaceClassnameMethodname, out object result, object[] param = null, Type[] types = null)
            {
                result = null;
                try
                {
                    MethodInfo methodInfo = FindMethod(namespaceClassnameMethodname, types);
                    if (methodInfo != null)
                    {
                        result = methodInfo.Invoke(null, param);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error trying to call '{namespaceClassnameMethodname}'.");
                    FNALoggerEXT.LogError(ex.ToString());
                }
                return false;
            }

            private MethodInfo FindMethod(string namespaceClassnameMethodname, Type[] types, bool showLog = true)
            {
                long num = namespaceClassnameMethodname.GetHashCode();
                if (types != null)
                {
                    Type[] array = types;
                    foreach (Type type in array)
                    {
                        num += type.GetHashCode();
                    }
                }
                if (!mCache.TryGetValue(num, out MethodInfo value))
                {
                    if (mAssembly != null)
                    {
                        int num2 = namespaceClassnameMethodname.LastIndexOf('.');
                        if (num2 != -1)
                        {
                            string text = namespaceClassnameMethodname.Substring(0, num2);
                            string name = namespaceClassnameMethodname.Substring(num2 + 1);
                            Type type2 = mAssembly.GetType(text);
                            if (type2 != null)
                            {
                                if (types == null)
                                    types = new Type[0];
                                value = type2.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, new ParameterModifier[0]);
                                if (value == null && showLog)
                                {
                                    if (types.Length != 0)
                                        Logger.Log(string.Format("Method '{0}[{1}]' not found.", namespaceClassnameMethodname, string.Join(", ", (from x in types
                                                                                                                                                  select x.Name).ToArray())));
                                    else
                                        Logger.Log($"Method '{namespaceClassnameMethodname}' not found.");
                                }
                            }
                            else if (showLog)
                            {
                                Logger.Error($"Class '{text}' not found.");
                            }
                        }
                        else if (showLog)
                        {
                            Logger.Error($"Function name error '{namespaceClassnameMethodname}'.");
                        }
                    }
                    else if (showLog)
                    {
                        ModManager.Logger.Error($"Can't find method '{namespaceClassnameMethodname}'. Mod '{Info.Id}' is not loaded.");
                    }
                    mCache[num] = value;
                }
                return value;
            }
        }

        public static readonly List<ModEntry> modEntries = new List<ModEntry>();

        public static ModEntry FindMod(string id)
        {
            return modEntries.FirstOrDefault((ModEntry x) => x.Info.Id == id);
        }

        public static Version ParseVersion(string str)
        {
            string[] array = str.Split('.', ',');
            if (array.Length >= 3)
            {
                Regex regex = new Regex("\\D");
                return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")), int.Parse(regex.Replace(array[2], "")));
            }
            Logger.Error($"Error parsing version {str}");
            return new Version();
        }

        public static class Logger
        {
            private const string Prefix = "[Manager] ";

            private const string PrefixError = "[Manager] [Error] ";

            public static readonly string filepath = Path.Combine(modsPath, "ModManager.log");

            public static void Log(string str)
            {
                Log(str, "[Manager] ");
            }

            public static void Log(string str, string prefix)
            {
                Write(prefix + str);
            }

            public static void Error(string str)
            {
                Error(str, "[Manager] [Error] ");
            }

            public static void Error(string str, string prefix)
            {
                Write(prefix + str);
            }

            public static void Write(string str)
            {
                Console.WriteLine(str);
                try
                {
                    using (StreamWriter streamWriter = File.AppendText(filepath))
                        streamWriter.WriteLine(str);
                }
                catch (Exception exception)
                {
                    FNALoggerEXT.LogError(exception.ToString());
                }
            }

            public static void Clear()
            {
                if (File.Exists(filepath))
                    try
                    {
                        File.Delete(filepath);
                        using (File.Create(filepath))
                        {
                        }
                    }
                    catch (Exception exception)
                    {
                        FNALoggerEXT.LogError(exception.ToString());
                    }
            }
        }

        public class Param
        {
            [Serializable]
            public class Mod
            {
                [XmlAttribute]
                public string Id;

                [XmlAttribute]
                public bool Enabled = true;
            }

            public int ShortcutKeyId;

            public int CheckUpdates = 1;

            public List<Mod> ModParams = new List<Mod>();

            public static readonly string filepath = Path.Combine(modsPath, "ModManager.xml");

            public void Save()
            {
                try
                {
                    ModParams.Clear();
                    foreach (ModEntry modEntry in modEntries)
                    {
                        ModParams.Add(new Mod
                        {
                            Id = modEntry.Info.Id,
                            Enabled = modEntry.Enabled
                        });
                    }
                    using (StreamWriter textWriter = new StreamWriter(filepath))
                        new XmlSerializer(typeof(Param)).Serialize(textWriter, this);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
            }

            public static Param Load()
            {
                if (File.Exists(filepath))
                    try
                    {
                        using (FileStream stream = File.OpenRead(filepath))
                        {
                            Param param = new XmlSerializer(typeof(Param)).Deserialize(stream) as Param;
                            foreach (Mod modParam in param.ModParams)
                            {
                                ModEntry modEntry = FindMod(modParam.Id);
                                if (modEntry != null)
                                    modEntry.Enabled = modParam.Enabled;
                            }
                            return param;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.ToString());
                    }
                return new Param();
            }
        }

        public static bool isStarted = false;
        private static Param mParams = new Param();
        private static Version mVersion = new Version();

        public static void Start()
        {

            if (isStarted)
            {
                Logger.Log("Cancel start. Already started.");
                return;
            }
            mVersion = ParseVersion("0.12.2");
            Logger.Clear();
            Console.WriteLine();
            Console.WriteLine();
            Logger.Log(string.Format("Version '{0}'. Initialize.", "0.12.2"));
            if (Directory.Exists(modsPath))
            {
                Logger.Log("Parsing mods.");
                int num = 0;
                string[] directories = Directory.GetDirectories(modsPath);
                ModInfo modInfo;
                foreach (string text in directories)
                {
                    string text2 = Path.Combine(text, "info.json");
                    if (File.Exists(text2))
                    {
                        num++;
                        Logger.Log($"Reading file '{text2}'.");
                        try
                        {
                            modInfo = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(text2));
                            if (string.IsNullOrEmpty(modInfo.Id))
                                Logger.Error("Id is null.");
                            else if (modEntries.Exists((ModEntry x) => x.Info.Id == modInfo.Id))
                            {
                                Logger.Error($"Id '{modInfo.Id}' already uses another mod.");
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(modInfo.AssemblyName))
                                    modInfo.AssemblyName = modInfo.Id + ".dll";
                                ModEntry item = new ModEntry(modInfo, text + Path.DirectorySeparatorChar.ToString());
                                modEntries.Add(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error parsing file '{text2}'.");
                            Logger.Log(ex.Message);
                        }
                    }
                }
                if (modEntries.Count > 0)
                {
                    Logger.Log("Sorting mods.");
                    modEntries.Sort(Compare);
                    mParams = Param.Load();
                    Logger.Log("Loading mods.");
                    foreach (ModEntry modEntry in modEntries)
                    {
                        modEntry.Load();
                    }
                }
                Logger.Log($"Finish. Found {num} mods. Successful loaded {modEntries.Count((ModEntry x) => x.Active)} mods.".ToUpper());
                Console.WriteLine();
                Console.WriteLine();
            }
            else
                Directory.CreateDirectory(modsPath);
            isStarted = true;
        }


        private static int Compare(ModEntry x, ModEntry y)
        {
            if (x.Requirements.Count > 0 && x.Requirements.ContainsKey(y.Info.Id))
            {
                return 1;
            }
            if (y.Requirements.Count > 0 && y.Requirements.ContainsKey(x.Info.Id))
            {
                return -1;
            }
            return string.Compare(x.Info.Id, y.Info.Id, StringComparison.Ordinal);
        }

    }
}
