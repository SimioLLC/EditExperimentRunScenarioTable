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
        internal static string RunSchedulePlanScenarioName = Properties.Settings.Default.RunSchedulePlanScenarioName;

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
            var client = new RestClient(Uri + "/api/RequestToken");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
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
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", "{\n    \"PersonalAccessToken\": \"" + PersonalAccessToken + "\",\n    \"Purpose\": \"PublicAPI\"\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            var xmlDoc = responseToXML(response.Content);
            XmlNodeList node = xmlDoc.GetElementsByTagName("Token");
            Token = node[0].InnerText;
            Console.WriteLine("Bearer Token Received Successfully");
        }

        internal static Int32[] findExperimentIds()
        {
            var client = new RestClient(Uri + "/api/Query");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
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
            request.AlwaysMultipartFormData = true;
            request.AddParameter("Type", "GetExperimentRuns");
            request.AddParameter("Query", "{\"ReturnNonOwnedRuns\":false}");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            var xmlDoc = responseToXML(response.Content);
            Int32[] returnInt = new int[2];
            var dataNodes = xmlDoc.SelectSingleNode("data");
            foreach (XmlNode itemNodes in dataNodes)
            {
                XmlNodeList projectNode = ((XmlElement)itemNodes).GetElementsByTagName("ProjectName");
                XmlNodeList expRunDescriptionNode = ((XmlElement)itemNodes).GetElementsByTagName("Description");
                XmlNodeList scenarioNamesNode = ((XmlElement)itemNodes).GetElementsByTagName("ScenarioNames");
                if (ProjectName == projectNode[0].InnerText &&
                    RunSchedulePlanScenarioName == scenarioNamesNode[0].InnerXml) 
                {
                    XmlNodeList idNode = ((XmlElement)itemNodes).GetElementsByTagName("Id");
                    XmlNodeList experimentIdNode = ((XmlElement)itemNodes).GetElementsByTagName("ExperimentId");
                    returnInt[0] = Convert.ToInt32(idNode[0].InnerXml);
                    returnInt[1] = Convert.ToInt32(experimentIdNode[0].InnerXml);
                    break;
                }
            }
            if (returnInt[1] == 0)
            {
                throw new Exception("Experiment Run Cannot Be Found");
            }
            return returnInt;
        }

        internal static XmlDocument getExperimentRunTableRowData(Int32 experimentRunId, string tableName)
        {
            var client = new RestClient(Uri + "/api/Query");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
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
            request.AlwaysMultipartFormData = true;
            request.AddParameter("Type", "GetExperimentRunTableRowData");
            request.AddParameter("Query", "{\"ExperimentRunId\": " + experimentRunId.ToString() + ",\"ScenarioName\": \"" + RunSchedulePlanScenarioName + "\",\"TableName\": \"" + tableName + "\"}");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            return responseToXML(response.Content);            
        }

        internal static XmlDocument getModelTableSchema(Int32 experimentRunId)
        {
            var client = new RestClient(Uri + "/api/Query");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
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
            request.AlwaysMultipartFormData = true;
            request.AddParameter("Type", "GetModelTableSchema");
            request.AddParameter("Query", "{\"ExperimentRunId\": " + experimentRunId.ToString() + "}");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            return responseToXML(response.Content);
        }

        internal static void insertExperimentRunScenarioTableRows(Int32 experimenRuntId, Int32 rowIndex, string tableName)
        {
            var client = new RestClient(Uri + "/api/Command");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
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
            request.AlwaysMultipartFormData = true;
            request.AddParameter("Type", "InsertExperimentRunScenarioTableRow");
            request.AddParameter("Command", "{\"ExperimentRunId\": " + experimenRuntId.ToString() + ",\"RowIndex\": " + rowIndex.ToString() + ",\"ScenarioName\": \"" + RunSchedulePlanScenarioName + "\",\"TableName\": \"" + tableName + "\"}");

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            else
            {
                var xmlDoc = responseToXML(response.Content);
                var successedNode = xmlDoc.SelectSingleNode("data/Succeeded");
                if (successedNode.InnerText.ToLower() == "false")
                {
                    var failureMessageNode = xmlDoc.SelectSingleNode("data/FailureMessage");
                    throw new Exception(failureMessageNode.InnerText);
                }
            }
        }

        internal static void setExperimentRunScenarioTableRows(Int32 experimenRuntId, Int32 rowIndex, string tableName, string columnName, string columnValue)
        {
            var client = new RestClient(Uri + "/api/Command");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
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
            request.AlwaysMultipartFormData = true;
            request.AddParameter("Type", "SetExperimentRunScenarioTableValue");
            request.AddParameter("Command", "{\"ExperimentRunId\": " + experimenRuntId.ToString() + ",\"RowIndex\": " + rowIndex.ToString() + ",\"ScenarioName\": \"" + RunSchedulePlanScenarioName + "\",\"TableName\": \"" + tableName + "\",\"ColumnName\": \"" + columnName + "\",\"Value\": \"" + columnValue + "\"}");

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            else
            {
                var xmlDoc = responseToXML(response.Content);
                var successedNode = xmlDoc.SelectSingleNode("data/Succeeded");
                if (successedNode.InnerText.ToLower() == "false")
                {
                    var failureMessageNode = xmlDoc.SelectSingleNode("data/FailureMessage");
                    throw new Exception(failureMessageNode.InnerText);
                }
            }
        }

        internal static void deleteExperimentRunScenarioTableRows(Int32 experimenRuntId, Int32 rowIndex, string tableName)
        {
            var client = new RestClient(Uri + "/api/Command");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "multipart/form-data");
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
            request.AlwaysMultipartFormData = true;
            request.AddParameter("Type", "RemoveExperimentRunScenarioTableRow");
            request.AddParameter("Command", "{\"ExperimentRunId\": " + experimenRuntId.ToString() + ",\"RowIndex\": " + rowIndex.ToString() + ",\"ScenarioName\": \"" + RunSchedulePlanScenarioName + "\",\"TableName\": \"" + tableName + "\"}");

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (response.ErrorMessage != null) throw new Exception(response.ErrorMessage);
                else throw new Exception(response.Content);
            }
            else
            {
                var xmlDoc = responseToXML(response.Content);
                var successedNode = xmlDoc.SelectSingleNode("data/Succeeded");
                if (successedNode.InnerText.ToLower() == "false")
                {
                    var failureMessageNode = xmlDoc.SelectSingleNode("data/FailureMessage");
                    throw new Exception(failureMessageNode.InnerText);
                }
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
