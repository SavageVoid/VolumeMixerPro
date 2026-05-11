using System;
using System.IO;
using System.Windows.Input;
using System.Xml.Serialization;
namespace VolumeMixerPro
{
    public class HotkeyDef
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public string KeyStr { get; set; }
        public bool Matches(int vkCode, bool ctrl, bool alt, bool shift)
        {
            if (this.Ctrl != ctrl || this.Alt != alt || this.Shift != shift) return false;
            if (Enum.TryParse<Key>(KeyStr, true, out Key key))
            {
                return vkCode == KeyInterop.VirtualKeyFromKey(key);
            }
            return false;
        }
        public override string ToString()
        {
            string res = "";
            if (Ctrl) res += "Ctrl + ";
            if (Alt) res += "Alt + ";
            if (Shift) res += "Shift + ";
            string k = KeyStr;
            if (k == "D0") k = "0";
            if (k == "D1") k = "1";
            res += k;
            return res;
        }
    }
    public class AppSettings
    {
        public HotkeyDef ToggleMenu { get; set; } = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "V" };
        public HotkeyDef MuteActive { get; set; } = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "M" };
        public HotkeyDef SoloMode { get; set; } = new HotkeyDef { Ctrl = true, Alt = true, Shift = true, KeyStr = "M" };
        public HotkeyDef UnmuteAll { get; set; } = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "D0" };
        public HotkeyDef PanicReset { get; set; } = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "P" };
        public bool EnablePanicMode { get; set; } = true;
        public bool EnableSoloMode { get; set; } = true;
        public bool EnableMuteMode { get; set; } = true;
    }
    public static class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");
        public static AppSettings Current { get; private set; } = new AppSettings();
        public static event Action OnSettingsChanged;
        public static void Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (StreamReader reader = new StreamReader(SettingsFile))
                    {
                        var loaded = (AppSettings)serializer.Deserialize(reader);
                        if (loaded != null)
                        {
                            if (loaded.ToggleMenu == null) loaded.ToggleMenu = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "V" };
                            if (loaded.MuteActive == null) loaded.MuteActive = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "M" };
                            if (loaded.SoloMode   == null) loaded.SoloMode   = new HotkeyDef { Ctrl = true, Alt = true, Shift = true, KeyStr = "M" };
                            if (loaded.UnmuteAll  == null) loaded.UnmuteAll  = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "D0" };
                            if (loaded.PanicReset == null) loaded.PanicReset = new HotkeyDef { Ctrl = true, Alt = true, KeyStr = "P" };
                            Current = loaded;
                        }
                    }
                }
                catch { }
            }
            else
            {
                Save();
            }
        }
        public static void Save()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (StreamWriter writer = new StreamWriter(SettingsFile))
                {
                    serializer.Serialize(writer, Current);
                }
                OnSettingsChanged?.Invoke();
            }
            catch { }
        }
        public static void Reset()
        {
            Current = new AppSettings();
            Save();
        }
    }
}
