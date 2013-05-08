using Ctr.FormatLibrary.Pwx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Ctr.FormatLibrary.Gpx
{
    /// <summary>
    /// Helpers for Gpx transformation
    /// </summary>
    public static class GpxHelpers
    {
        /// <summary>
        /// Validate the gpx document
        /// </summary>
        /// <param name="xdoc"></param>
        public static void ValidateGpx(XDocument xdoc)
        {
            using (var schemaStream = System.Reflection.Assembly.GetAssembly(typeof(GpxHelpers)).GetManifestResourceStream(@"Ctr.FormatLibrary.Schema.Gpx.v1_1.gpx.xsd"))
            {
                bool errorValidating = false;
                string errorsMsg = string.Empty;

                XmlSchema schema = XmlSchema.Read(schemaStream, (sender, validationEvent) => { });
                XmlSchemaSet set = new XmlSchemaSet();
                set.Add(schema);
                xdoc.Validate(set, (sender, validationEvent) =>
                {
                    errorValidating = true;
                    errorsMsg += validationEvent.Message;
                });

                if (errorValidating)
                {
                    throw new XmlSchemaException("Error validating output Gpx file: " + errorsMsg);
                }
            }
        }
    }
}
