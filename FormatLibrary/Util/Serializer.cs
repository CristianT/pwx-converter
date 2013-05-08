using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Ctr.FormatLibrary.Util
{
    /// <summary>
    /// Util to serialize and deserialize objects.
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Returns an object deserialized from a string in the form of a Xml.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string obj)
        {
            using(MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(obj)))
            {
                ms.Seek(0, SeekOrigin.Begin);

                XmlSerializer s = new XmlSerializer(typeof(T));
                return (T) s.Deserialize(ms);
            }
        }

        /// <summary>
        /// Returns an XDocument from an object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static XDocument Serialize<T>(T obj)
        {
            XDocument doc = new XDocument();
            using (var writer = doc.CreateWriter())
            {
                // write xml into the writer
                XmlSerializer s = new XmlSerializer(typeof(T));
                s.Serialize(writer, obj);
            }

            return doc;
        }
    }
}
