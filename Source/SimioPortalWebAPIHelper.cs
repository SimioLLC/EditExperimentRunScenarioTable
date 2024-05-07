using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml;
using System.Web.UI.WebControls;

namespace EditExperimentRunScenarioTable
{
    public static class SimioPortalWebAPIHelper
    {
        internal static string Token;
        internal static ICredentials Credentials;
        internal static bool UseDefaultCredentials;
        internal static Uri Uri;
        internal static string Url = Properties.Settings.Default.URL;
        internal static string PersonalAccessToken = Properties.Settings.Default.PersonalAccessToken;
        internal static string AuthenticationType = Properties.Settings.Default.AuthenticationType;
        internal static string Domain = Properties.Settings.Default.Domain;
        internal static string UserName = Properties.Settings.Default.UserName;
        internal static string Password = Properties.Settings.Default.Password;
        internal static string ProjectName = Properties.Settings.Default.ProjectName;
        internal static string ModelName = Properties.Settings.Default.ModelName;
        internal static string ExperimentName = Properties.Settings.Default.ExperimentName;
        internal static string ScenarioName = Properties.Settings.Default.ScenarioName;

        internal static void setCredentials()
        {
            //
            // Resolve values to objects where necessary
            //
            var cache = new CredentialCache();
            Credentials = null;

            if (AuthenticationType.ToLower() == "currentuser")
            {
                // Not sure if we need both lines, but we'll do it anyway
                UseDefaultCredentials = true;
                Credentials = CredentialCache.DefaultCredentials;
            }
            else if (String.IsNullOrWhiteSpace(UserName) == false)
            {
                Credentials = cache;
                var rootUrl = new Uri(Uri.GetLeftPart(UriPartial.Authority));
                if (String.IsNullOrWhiteSpace(Domain) == false)
                    cache.Add(rootUrl, AuthenticationType, new NetworkCredential(UserName, Password ?? String.Empty));
                else
                    cache.Add(rootUrl, AuthenticationType, new NetworkCredential(UserName, Password ?? String.Empty, Domain));
            }
        }

        internal static void obtainBearerToken()
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/auth", Method.POST);
            request.AddHeader("Accept", "application/json");
            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            };
            request.AddJsonBody("{\n    \"personalAccessToken\": \"" + PersonalAccessToken + "\"    \n}");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.StatusDescription + " : " + response.ErrorMessage);
                else throw new Exception(response.StatusDescription + " : " + response.Content);
            }
            var xmlDoc = responseToXML(response.Content);
            XmlNodeList node = xmlDoc.GetElementsByTagName("token");
            Token = node[0].InnerText;
            Console.WriteLine("Bearer Token Received Successfully");
        }

        internal static Int32 findModelId()
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/models?owned_models=true", Method.GET);
            request.AddHeader("Authorization", "Bearer " + Token);
            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }
            request.AddHeader("Accept", "application/json");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.StatusDescription + " : " + response.ErrorMessage);
                else throw new Exception(response.StatusDescription + " : " + response.Content);
            }
            var xmlDoc = responseToXML(response.Content);
            Int32 returnInt = 0;
            var dataNodes = xmlDoc.SelectSingleNode("data");
            foreach (XmlNode itemNodes in dataNodes)
            {
                XmlNodeList projectNode = ((XmlElement)itemNodes).GetElementsByTagName("projectName");
                if (ProjectName == projectNode[0].InnerText)
                {
                    XmlNodeList idNode = ((XmlElement)itemNodes).GetElementsByTagName("id");
                    Int32 tempReturnInt = Convert.ToInt32(idNode[0].InnerXml);
                    if (tempReturnInt > returnInt)
                        returnInt = tempReturnInt;
                }
            }
            return returnInt;
        }

        internal static Int32 findExperimentId(Int32 modelId)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/experiments?model_id=" + modelId.ToString(), Method.GET);
            request.AddHeader("Authorization", "Bearer " + Token);
            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }
            request.AddHeader("Accept", "application/json");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.StatusDescription + " : " + response.ErrorMessage);
                else throw new Exception(response.StatusDescription + " : " + response.Content);
            }
            var xmlDoc = responseToXML(response.Content);
            Int32 returnInt = 0;
            var dataNodes = xmlDoc.SelectSingleNode("data");
            foreach (XmlNode itemNodes in dataNodes)
            {
                XmlNodeList projectNode = ((XmlElement)itemNodes).GetElementsByTagName("projectName");
                XmlNodeList modelIdNode = ((XmlElement)itemNodes).GetElementsByTagName("modelId");
                XmlNodeList nameNode = ((XmlElement)itemNodes).GetElementsByTagName("name");
                if (ProjectName == projectNode[0].InnerText && modelId.ToString() == modelIdNode[0].InnerText &&
                     (ExperimentName == nameNode[0].InnerXml))
                {
                    XmlNodeList idNode = ((XmlElement)itemNodes).GetElementsByTagName("id");
                    returnInt = Convert.ToInt32(idNode[0].InnerXml);
                    break;
                }
            }
            return returnInt;
        }

        internal static Int32 findExperimentRunId(Int32 experimentId)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/runs?experiment_id=" + experimentId.ToString(), Method.GET);
            request.AddHeader("Authorization", "Bearer " + Token);
            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }
            request.AddHeader("Accept", "application/json");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.StatusDescription + " : " + response.ErrorMessage);
                else throw new Exception(response.StatusDescription + " : " + response.Content);
            }
            var xmlDoc = responseToXML(response.Content);
            Int32 returnInt = 0;
            var dataNodes = xmlDoc.SelectSingleNode("data");
            foreach (XmlNode itemNodes in dataNodes)
            {
                XmlNodeList nameNode = ((XmlElement)itemNodes).GetElementsByTagName("scenarioNames");
                if (ScenarioName == nameNode[0].InnerXml)
                {
                    XmlNodeList idNode = ((XmlElement)itemNodes).GetElementsByTagName("id");
                    returnInt = Convert.ToInt32(idNode[0].InnerXml);
                    break;
                }                
            }
            return returnInt;
        }

        internal static XmlDocument getExperimentRunTableRowData(Int32 experimentRunId, string tableName)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/runs/" + experimentRunId.ToString() + "/scenarios/" + ScenarioName + "/table-data/" + tableName, Method.GET);
            request.AddHeader("Authorization", "Bearer " + Token);

            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            return responseToXML(response.Content);            
        }

        internal static XmlDocument getModelTableSchema(Int32 modelId)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/models/" + modelId.ToString() + "/table-schemas", Method.GET);
            request.AddHeader("Authorization", "Bearer " + Token);
            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            return responseToXML(response.Content);
        }

        internal static void insertExperimentRunScenarioTableRows(Int32 experimentRunId, Int32 rowIndex, string tableName)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/runs/" + experimentRunId.ToString() + "/scenarios/" + ScenarioName + "/table-data/" + tableName + "/rows/" + rowIndex.ToString(), Method.POST);
            request.AddHeader("Authorization", "Bearer " + Token);

            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
        }

        internal static void setExperimentRunScenarioTableRows(Int32 experimentRunId, Int32 rowIndex, string tableName, string columnName, string columnValue)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/runs/" + experimentRunId.ToString() + "/scenarios/" + ScenarioName + "/table-data/" + tableName + "/rows", Method.PATCH);
            request.AddHeader("Authorization", "Bearer " + Token);

            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }

            request.AddJsonBody("{\"rowindex\": " + rowIndex.ToString() + ",\"columnname\": \"" + columnName + "\",\"value\": \"" + columnValue + "\"}");

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
        }

        internal static void deleteExperimentRunScenarioTableRows(Int32 experimentRunId, Int32 rowIndex, string tableName)
        {
            var client = new RestClient(Uri);
            client.Timeout = -1;
            var request = new RestRequest("/api/v1/runs/" + experimentRunId.ToString() + "/scenarios/" + ScenarioName + "/table-data/" + tableName + "/rows/" + rowIndex.ToString(), Method.DELETE);
            request.AddHeader("Authorization", "Bearer " + Token);
            if (String.IsNullOrWhiteSpace(AuthenticationType) == false && AuthenticationType.ToLower() != "none")
            {
                if (UseDefaultCredentials)
                {
                    request.UseDefaultCredentials = true;
                }
                else
                {
                    client.Authenticator = new RestSharp.Authenticators.NtlmAuthenticator(Credentials);
                }
            }
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
        }

        internal static XmlDocument responseToXML(string responseContent)
        {
            string resultString;
            var isProbablyJSONObject = false;
            var isXMLResponse = false;
            using (var stream = GenerateStreamFromString(responseContent))
            using (var reader = new StreamReader(stream))
            {
                resultString = reader.ReadToEnd();
            }

            // We are looking for the first non-whitespace character (and are specifically not Trim()ing here
            //  to eliminate memory allocations on potentially large (we think?) strings
            foreach (var theChar in resultString)
            {
                if (Char.IsWhiteSpace(theChar))
                    continue;

                if (theChar == '{')
                {
                    isProbablyJSONObject = true;
                    break;
                }
                else if (theChar == '<')
                {
                    isXMLResponse = true;
                    break;
                }
                else
                {
                    // Any other character?
                    break;
                }
            }

            XmlDocument xmlDoc;
            if (isProbablyJSONObject == false)
            {
                var prefix = "{ items: ";
                var postfix = "}";

                using (var combinedReader = new StringReader(prefix)
                                            .Concat(new StringReader(resultString))
                                            .Concat(new StringReader(postfix)))
                {
                    var settings = new JsonSerializerSettings
                    {
                        Converters = { new Newtonsoft.Json.Converters.XmlNodeConverter() { DeserializeRootElementName = "data" } },
                        DateParseHandling = DateParseHandling.None,
                    };
                    using (var jsonReader = new JsonTextReader(combinedReader) { CloseInput = false, DateParseHandling = DateParseHandling.None })
                    {
                        xmlDoc = JsonSerializer.CreateDefault(settings).Deserialize<XmlDocument>(jsonReader);
                    }
                }
            }
            else
            {
                xmlDoc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(resultString, "data");
            }

            return xmlDoc;
        }

        internal static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
