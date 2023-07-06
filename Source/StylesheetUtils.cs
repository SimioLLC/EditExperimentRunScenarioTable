using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

namespace EditExperimentRunScenarioTable
{
    public static partial class JsonExtensions
    {
        public static XmlDocument DeserializeXmlNode(string json, string rootName, string rootPropertyName)
        {
            return DeserializeXmlNode(new StringReader(json), rootName, rootPropertyName);
        }

        public static XmlDocument DeserializeXmlNode(TextReader textReader, string rootName, string rootPropertyName)
        {
            var prefix = "{" + JsonConvert.SerializeObject(rootPropertyName) + ":";
            var postfix = "}";

            using (var combinedReader = new StringReader(prefix).Concat(textReader).Concat(new StringReader(postfix)))
            {
                var settings = new JsonSerializerSettings
                {
                    Converters = { new Newtonsoft.Json.Converters.XmlNodeConverter() { DeserializeRootElementName = rootName } },
                    DateParseHandling = DateParseHandling.None,
                };
                using (var jsonReader = new JsonTextReader(combinedReader) { CloseInput = false, DateParseHandling = DateParseHandling.None })
                {
                    return JsonSerializer.CreateDefault(settings).Deserialize<XmlDocument>(jsonReader);
                }
            }
        }
    }   

    public static class XMLTransformationUtils
    {

        internal static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        internal static string GetStringFromTransform(string stylesheet, string xmlString)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            XPathDocument stylesheetXPD = new XPathDocument(GenerateStreamFromString(stylesheet));
            xslt.Load(stylesheetXPD);
            MemoryStream stream = new MemoryStream();
            XPathDocument inputXPD = new XPathDocument(GenerateStreamFromString(xmlString));
            xslt.Transform(inputXPD, null, stream);
            stream.Position = 0;
            var sr = new StreamReader(stream);
            return sr.ReadToEnd();
        }

        internal static DataSet GetDataSetFromTransform(string stylesheet, string xmlString)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            XPathDocument stylesheetXPD = new XPathDocument(GenerateStreamFromString(stylesheet));
            xslt.Load(stylesheetXPD);
            MemoryStream stream = new MemoryStream();
            XPathDocument inputXPD = new XPathDocument(GenerateStreamFromString(xmlString));
            xslt.Transform(inputXPD, null, stream);
            stream.Position = 0;
            DataSet ds = new DataSet();
            ds.ReadXml(stream);
            return ds;
        }

        internal static string GetWebRequestResponse(string requestUri, string bearerToken, string apiKey, string domain, string username, string password, bool addOuterContainerElement, bool isXMLResult)
        {
            Uri uri = new Uri(requestUri);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            if (bearerToken.Length > 0)
            {
                httpWebRequest.Headers.Add("Authorization", "Bearer " + bearerToken);
            }
            else
            { 
                if (apiKey.Length == 0)
                {
                    httpWebRequest.UseDefaultCredentials = false;
                    httpWebRequest.PreAuthenticate = true;
                    if (domain.Length > 0) httpWebRequest.Credentials = new NetworkCredential(username, password, domain);
                    else httpWebRequest.Credentials = new NetworkCredential(username, password);
                }
                else
                {
                    httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
                    httpWebRequest.Headers.Add("APIKey", apiKey);
                }
            }

            httpWebRequest.Method = WebRequestMethods.Http.Get;      
            httpWebRequest.Accept = "*/*";
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
            httpWebRequest.Timeout = 600000;

            string resultString;
            using (var response = httpWebRequest.GetResponse())
            {
                long len = response.ContentLength;
                using (Stream stream = response.GetResponseStream())
                {
                    Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                    StreamReader reader = new StreamReader(stream, encode);
                    resultString = reader.ReadToEnd();
                }
            }

            XmlDocument xmlDoc;
            if (isXMLResult)               
            {
                xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(resultString);
                return xmlDoc.InnerXml;
            }
            else
            {
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(resultString);
                var responseData = JsonConvert.SerializeObject(jsonObj);
                if (addOuterContainerElement)
                {
                    //added outer container element
                    xmlDoc = JsonExtensions.DeserializeXmlNode(responseData, "d", "results");
                    return xmlDoc.InnerXml;
                }
                else
                {
                    xmlDoc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(responseData);
                    return xmlDoc.InnerXml; ;
                }
            }
        }

    }   
}
