﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

using FancyWM.Models;

namespace FancyWM.Converters
{
    internal class KeybindingModel
    {
        public List<string> Keys { get; set; }
        public bool IsDirectMode { get; set; }
    }

    class KeybindingConverter : JsonConverter<KeybindingDictionary>
    {
        private readonly bool m_useDefaults;

        public KeybindingConverter()
        {
            m_useDefaults = true;
        }

        public KeybindingConverter(bool useDefaults)
        {
            m_useDefaults = useDefaults;
        }

        public override KeybindingDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return ReadLatest(ref reader, typeToConvert, options);
            }
            catch (Exception ex)
            {
                try
                {
                    return Read2dot3dot5Compatible(ref reader, typeToConvert, options);
                }
                catch (Exception exCompat)
                {
                    throw new AggregateException(ex, exCompat);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, KeybindingDictionary value, JsonSerializerOptions options)
        {
            var dict = new Dictionary<string, object>();
            foreach (var (action, keybinding) in value)
            {
                if (keybinding == null)
                {
                    dict.Add(action.ToString(), null);
                    continue;
                }

                var keys = keybinding.Keys.Select(x => x.ToString()).ToArray();
                dict.Add(action.ToString(), new
                {
                    IsDirectMode = keybinding.IsDirectMode,
                    Keys = keys,
                });
            };
            JsonSerializer.Serialize(writer, dict, options);
        }

        private KeybindingDictionary ReadLatest(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var defaultDict = new KeybindingDictionary(m_useDefaults);
            var dict = JsonSerializer.Deserialize<IDictionary<string, KeybindingModel?>>(ref reader, options);
            if (dict == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var (keyName, keyBinds) in dict)
            {
                try
                {
                    var key = (BindableAction)Enum.Parse(typeof(BindableAction), keyName, ignoreCase: true);
                    defaultDict[key] = keyBinds == null ? null : new Keybinding(new HashSet<Key>(keyBinds.Keys.Select(x => (Key)Enum.Parse(typeof(Key), x))), keyBinds.IsDirectMode);
                }
                catch (ArgumentException)
                {
                    // Probably revereted from a newer version
                    continue;
                }
            }

            return defaultDict;
        }

        private KeybindingDictionary Read2dot3dot5Compatible(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var defaultDict = new KeybindingDictionary(m_useDefaults);
            var dict = JsonSerializer.Deserialize<IDictionary<string, string[]>>(ref reader, options);

            if (dict == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var (keyName, keyStrings) in dict)
            {
                try
                {
                    var key = (BindableAction)Enum.Parse(typeof(BindableAction), keyName, ignoreCase: true);
                    var value = new HashSet<Key>(keyStrings.Select(x => (Key)Enum.Parse(typeof(Key), x)));
                    defaultDict[key] = new Keybinding(value, false);
                }
                catch (ArgumentException)
                {
                    // Probably revereted from a newer version
                    continue;
                }
            }

            return defaultDict;
        }
    }
}
