using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AADB2C.CustomPolicy.Parser
{
    public class ResourceHelper
    {
        private IEFParser Parser;
        internal ResourceHelper(IEFParser parser)
        {
            Parser = parser;
        }
        public static TrustFrameworkPolicy ConvertXmlToTFP(string xml)
        {
            TrustFrameworkPolicy tfPolicy = null;
            //XmlSchemaSet schemas = new XmlSchemaSet();

            //schemas.Add(null, XmlReader.Create(xsdStream));

            XmlSerializer serializer = new XmlSerializer(typeof(TrustFrameworkPolicy));

            serializer.UnknownAttribute += UnknownAttribute;
            serializer.UnknownElement += UnknownElement;
            using (TextReader reader = new StringReader(xml))
            {
                //XDocument doc = XDocument.Load(reader);

                //string msg = "";
                //var errors = false;
                //doc.Validate(schemas, (o, err) =>
                //{
                //    msg = err.Message;
                //    Debug.WriteLine(msg == "" ? "Document is valid" : "Document invalid: " + msg);
                //    errors = true;
                //});
                //if (!errors)
                //{
                tfPolicy = (TrustFrameworkPolicy)serializer.Deserialize(reader);
                Debug.WriteLine("finished ConvertXmlToTFP {0}", tfPolicy.PolicyId);
                //}
            }


            return tfPolicy;
        }

        static void UnknownElement(object sender, XmlElementEventArgs e)
        {
            Debug.WriteLine("Unexpected element: {0} as line {1}, column {2}",
                e.Element.Name, e.LineNumber, e.LinePosition);
        }

        static void UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            Debug.WriteLine("Unexpected attribute: {0} as line {1}, column {2}",
                e.Attr.Name, e.LineNumber, e.LinePosition);
        }

        public static string SerializeToXml(TrustFrameworkPolicy input)
        {
            XmlSerializer ser = new XmlSerializer(typeof(TrustFrameworkPolicy), "http://schemas.microsoft.com/online/cpim/schemas/2013/06");
            string result = string.Empty;

            using (MemoryStream memStm = new MemoryStream())
            {
                ser.Serialize(memStm, input);

                memStm.Position = 0;
                result = new StreamReader(memStm).ReadToEnd();
            }

            return result;
        }

        
        public void DownloadPolicy(string filename, string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            doc.Save(filename);

        }

        public void Download1POwnedPolicies()
        {
            foreach (var item in Parser.PolicyIdsInTenant.Keys)
            {
                var tfp = Parser.PolicyIdsInTenant[item];
                if (tfp.PolicyConstraints != null && tfp.PolicyConstraints.Is1POwned)
                {
                    string xml = SerializeToXml(tfp);
                    Debug.WriteLine("start....{0}......", tfp.PolicyId);
                    DownloadPolicy(tfp.PolicyId + ".xml", xml);
                    Debug.WriteLine("end....{0}......", tfp.PolicyId);
                }
            }

        }

        public void DownloadKeyset(string filename, TrustFrameworkKeyset keyset)
        {
            string json = keyset.ToJson();
            File.WriteAllText(filename, json);
        }
        public void DownloadAllKeysets()
        {
            foreach (var item in Parser.KeySetsInTenant.Keys)
            {
                Debug.WriteLine("start....{0}......", item);
                DownloadKeyset(item + ".json", Parser.KeySetsInTenant[item]);
                Debug.WriteLine("end....{0}......", item);
            }
        }
        internal bool LoadEmbeddedResources()
        {
            bool ret = false;
            var assembly = Assembly.GetAssembly(typeof(ResourceHelper));
            string[] res = assembly.GetManifestResourceNames();
            foreach (var item in res)
            {
                if (item.Contains(".xsd"))
                {
                    Parser.xsdStream = assembly.GetManifestResourceStream(item);
                    continue;

                }
                using (Stream stream = assembly.GetManifestResourceStream(item))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    Debug.WriteLine("starting deserializing of {0}", item);
                    var tfp = ConvertXmlToTFP(result);
                    if (item.Contains(Constants.TemplatePolicyBaseId))
                    {
                        Parser.TemplateBasePolicy = tfp;
                        Parser.BasePolicyId = Parser.TemplateBasePolicy.PolicyId;
                    }
                    else if (item.Contains(Constants.TemplatePolicyExtensionsId))
                    {
                        Parser.TemplatePolicyExtensions = tfp;

                    }
                    else if (item.Contains(Constants.TemplateTenantPolicyId))
                    {
                        Parser.TemplateTenantPolicy = tfp;

                    }
                    else
                    {
                        Parser.TemplateRPsInTenant[tfp.PolicyId] = tfp;
                    }
                }
            }

            return ret;
        }
    }
}
