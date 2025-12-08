using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace ACController.Models
{
    [Serializable, XmlRoot("ACController")]
    public class AppSettings
    {

        #region Static

        static AppSettings()
        {
            _XmlSerializerNamespaces.Add("", "");
        }

        private static XmlSerializer _XmlSerializer = new XmlSerializer(typeof(AppSettings));
        private static XmlSerializerNamespaces _XmlSerializerNamespaces = new XmlSerializerNamespaces();
        private static string _ConfigFilePath = Path.Combine(Path.GetFullPath("."), "config.xml");

        public static void SaveConfig(ref AppSettings config)
        {
            // delete the file if exist
            if (File.Exists(_ConfigFilePath))
                File.Delete(_ConfigFilePath);

            // save he xml file
            using (var fs = new FileStream(_ConfigFilePath, FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(fs, new XmlWriterSettings()
            {
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                Indent = true,
                Encoding = Encoding.UTF8,
            }))
                _XmlSerializer.Serialize(xmlWriter, config, _XmlSerializerNamespaces);
        }

        public static void LoadConfig(ref AppSettings config)
        {
            // delete the file if exist
            if (!File.Exists(_ConfigFilePath))
            {
                config = new AppSettings();
                SaveConfig(ref config);
                return;
            }

            // read the xml file
            using (var fs = new FileStream(_ConfigFilePath, FileMode.Open))
                config = (AppSettings)_XmlSerializer.Deserialize(fs);
        }

        #endregion

        public string ServerAddress { get; set; } = "http://localhost:7239/";
        public string ACRootDirectory { get; set; } = "C:\\Program Files\\Steam\\steamapps\\common\\assettocorsa";
        public string MachineName { get; set; } = Environment.MachineName;

    }
}
