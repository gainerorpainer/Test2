using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace StopOrderTrader.Lib
{
    [Serializable]
    public abstract class EasyFileSerializer
    {
        [NonSerialized]
        XmlSerializer _xmlSerializer;

        [NonSerialized]
        string _filePath;

        public bool IsCompressed { get; set; }

        protected EasyFileSerializer(string filepath, bool compressed = false)
        {
            _xmlSerializer = new XmlSerializer(GetType());
            _filePath = filepath;

            IsCompressed = compressed;
        }

        internal void Serialize()
        {
            // Serialize
            if (!IsCompressed)
            {
                using (var fs = File.Create(_filePath))
                    _xmlSerializer.Serialize(fs, this);
            }
            else
            {
                using (var fs = File.Create(_filePath))
                using (var comp = new GZipStream(fs, CompressionMode.Compress))
                    _xmlSerializer.Serialize(comp, this);
            }
        }

        public static T Deserialize<T>(string filepath, bool compressed = false)
        {
            if (!compressed)
                using (var fs = File.OpenRead(filepath))
                    return (T)new XmlSerializer(typeof(T)).Deserialize(fs);
            else
            {
                using (var fs = File.OpenRead(filepath))
                using (var comp = new GZipStream(fs, CompressionMode.Decompress))
                    return (T)new XmlSerializer(typeof(T)).Deserialize(comp);
            }
        }
    }
}
