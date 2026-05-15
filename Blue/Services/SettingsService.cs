using Blue.Core.Services;
using Blue.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Blue.Services
{
    public class SettingsService : ObservableObject, ISettingsService
    {
        private static ApplicationDataContainer Settings = ApplicationData.Current.LocalSettings;

        private bool autoPin = (bool)(Settings.Values["AutoPin"] ?? true);
        public bool AutoPin
        {
            get => autoPin;
            set
            {
                Settings.Values["AutoPin"] = value;
                SetProperty(ref autoPin, value);
            }
        }

        private bool enableTray = (bool)(Settings.Values["enableTray"] ?? true);
        public bool enableTray
        {
            get => enableTray;
            set
            {
                Settings.Values["enableTray"] = value;
                SetProperty(ref enableTray, value);
                //if (value)
                   // ClippyTrayListener.Recreate();
               // else
                    // ClippyTrayListener.Remove();
            }
        }

        private bool translucentBackground = (bool)(Settings.Values["TranslucentBackground"] ?? true);
        public bool TranslucentBackground
        {
            get => translucentBackground;
            set
            {
                Settings.Values["TranslucentBackground"] = value;
                SetProperty(ref translucentBackground, value);
            }
        }

        private int tokens = (int)(Settings.Values["Tokens"] ?? 100);
        public int Tokens
        {
            get => tokens;
            set
            {
                if (value > 50 && value < 2000)
                {
                    Settings.Values["Tokens"] = value;
                    SetProperty(ref tokens, value);
                }
                else
                    SetProperty(ref tokens, 100);
            }
        }

        private bool keyboardEnabled = (bool)(Settings.Values["KeyboardEnabled"] ?? true);
        public bool KeyboardEnabled
        {
            get => keyboardEnabled;
            set
            {
                Settings.Values["KeyboardEnabled"] = value;
                SetProperty(ref keyboardEnabled, value);
            }
        }
    }
}
