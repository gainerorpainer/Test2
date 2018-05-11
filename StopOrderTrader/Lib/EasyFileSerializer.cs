using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace StopOrderTrader.Lib
{
    public abstract class EasyFileSerializer 
    {
        XmlSerializer _xmlSerializer;
        string _filePath;
        DateTime _lastSerialization = DateTime.Now;

        protected EasyFileSerializer(string filepath)
        {
            _xmlSerializer = new XmlSerializer(GetType());
            _filePath = filepath;
        }

        internal Task AsyncSerialize()
        {
            return Task.Run(() =>
            {
                lock (this)
                {
                    _lastSerialization = DateTime.Now;

                    // Serialize
                    using (var fs = File.Create(_filePath))
                        _xmlSerializer.Serialize(fs, this);
                }
            });
        }

        internal void Serialize()
        {
            AsyncSerialize().Wait();
        }
    }
}
