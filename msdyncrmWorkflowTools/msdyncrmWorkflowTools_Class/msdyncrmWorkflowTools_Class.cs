﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Net.Http.Headers;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Microsoft.Crm.Sdk.Messages;

namespace msdyncrmWorkflowTools
{
    public class msdyncrmWorkflowTools_Class
    {
        private IOrganizationService service;
        private ITracingService tracing;

        public msdyncrmWorkflowTools_Class(IOrganizationService _service, ITracingService _tracing)
        {
            service = _service;
            tracing = _tracing;
        }

        public msdyncrmWorkflowTools_Class(IOrganizationService _service)
        {
            service = _service;
            tracing = null;
        }

        public void QueryValues()
        {
        }

        public string AzureTranslateText(string subscriptionKey, string text, string sourceLanguage, string destinationLanguage)
        {
            string uri;
            string result = "";
            var authTokenSource = new AzureAuthToken(subscriptionKey.Trim());
            string authToken = authTokenSource.GetAccessToken();
            HttpWebRequest req;

            if (sourceLanguage == "")
            {
                uri = "https://api.microsofttranslator.com/v2/Http.svc/Detect?text=" + text;
                 req = HttpWebRequest.Create(uri) as HttpWebRequest;
                req.Headers.Add("Authorization", authToken);
                using (StreamReader sr = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    
                    result = sr.ReadToEnd();
                    xmlDoc.LoadXml(result);
                    sourceLanguage = xmlDoc.ChildNodes[0].InnerText;
                }

            }




            uri = "https://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + text + "&from=" + sourceLanguage + "&to=" + destinationLanguage;

            req =HttpWebRequest.Create(uri) as HttpWebRequest;
            req.Headers.Add("Authorization", authToken);
            //req.Accept = "application/json";
           // req.Method = "POST";
           // req.ContentType = "application/json";
            //req.ContentLength = data.Length;

           // req.GetRequestStream().Write(data, 0, data.Length);

            using (StreamReader sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {

                result = sr.ReadToEnd();

            }


            return result;
        }

        public string AzureFunctionCall(string jSon, string serviceUrl)
        {
            string response = "";
            using (WebClient client = new WebClient())
            {

                var webClient = new WebClient();
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";

                response = webClient.UploadString(serviceUrl, jSon);
            }
            return response;
        }


        public string AzureTextAnalyticsSentiment(string subscriptionKey, string text, string language)
        {
            string result = "";

            byte[] data = Encoding.UTF8.GetBytes("{\"documents\":[" + "{\"language\":\""+language+"\" , \"id\":\"1\",\"text\":\""+ text + "\"}]}");
            HttpWebRequest req =
                HttpWebRequest.Create(
                    "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment") as HttpWebRequest;
            req.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            req.Accept = "application/json";
            req.Method = "POST";
            req.ContentType = "application/json";
            req.ContentLength = data.Length;

            req.GetRequestStream().Write(data, 0, data.Length);

            using (StreamReader sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                
                result = sr.ReadToEnd();
                
            }
            

            return result;
        }












        public void DeleteOptionValue(bool globalOptionSet, string attributeName, string entityName, int optionValue)
        {
            if (globalOptionSet)
            {

                DeleteOptionValueRequest deleteOptionValueRequest =
                  new DeleteOptionValueRequest
                  {
                      OptionSetName = attributeName,
                      Value = optionValue                    
                  };
                service.Execute(deleteOptionValueRequest);
            }
            else
            {
                // Create a request.
                DeleteOptionValueRequest insertOptionValueRequest =
                   new DeleteOptionValueRequest
                   {
                       AttributeLogicalName = attributeName,
                       EntityLogicalName = entityName,
                       Value = optionValue
                   };
                service.Execute(insertOptionValueRequest);
            }

        }

        public void SalesLiteratureToEmail(string _FileName, string salesLiteratureId, string emailid)
        {
            if (_FileName == "*")
                _FileName = "";
            _FileName = _FileName.Replace("*", "%");

            #region "Query Attachments"
            string fetchXML = @"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='salesliteratureitem'>
                        <attribute name='filename' />
                        <attribute name='salesliteratureitemid' />
                        <attribute name='title' />
                        <attribute name='documentbody' />
                        <attribute name='mimetype' />

                        <filter type='and'>
                          <condition attribute='filename' operator='like' value='%" + _FileName + @"%' />
                          <condition attribute='salesliteratureid' operator='eq' value='" + salesLiteratureId + @"' />
                        </filter>
                      </entity>
                    </fetch>";
            if (tracing!=null)tracing.Trace(String.Format("FetchXML: {0} ", fetchXML));
            EntityCollection attachmentFiles = service.RetrieveMultiple(new FetchExpression(fetchXML));

            if (attachmentFiles.Entities.Count == 0)
            {
                if (tracing!=null) tracing.Trace(String.Format("No Attachment Files found."));
                return;
            }


            #endregion

            #region "Add Attachments to Email"
            int i = 1;
            foreach (Entity file in attachmentFiles.Entities)
            {
                Entity _Attachment = new Entity("activitymimeattachment");
                _Attachment["objectid"] = new EntityReference("email", new Guid (emailid));
                _Attachment["objecttypecode"] = "email";
                _Attachment["attachmentnumber"] = i;
                i++;

                if (file.Attributes.Contains("title"))
                {
                    _Attachment["subject"] = file.Attributes["title"].ToString();
                }
                if (file.Attributes.Contains("filename"))
                {
                    _Attachment["filename"] = file.Attributes["filename"].ToString();
                }
                if (file.Attributes.Contains("documentbody"))
                {
                    _Attachment["body"] = file.Attributes["documentbody"].ToString();
                }
                if (file.Attributes.Contains("mimetype"))
                {
                    _Attachment["mimetype"] = file.Attributes["mimetype"].ToString();
                }

                service.Create(_Attachment);


            }

            #endregion
        }

       

        public void InsertOptionValue(bool globalOptionSet, string attributeName, string entityName, string optionText, int optionValue, int languageCode)
        {
            if (globalOptionSet)
            {
                InsertOptionValueRequest insertOptionValueRequest =
                  new InsertOptionValueRequest
                  {
                      OptionSetName= attributeName,
                      Value = optionValue,
                      Label = new Label(optionText, languageCode)
                  };
                int insertOptionValue = ((InsertOptionValueResponse)service.Execute(insertOptionValueRequest)).NewOptionValue;
            }
            else
            {
                // Create a request.
                InsertOptionValueRequest insertOptionValueRequest =
                   new InsertOptionValueRequest
                   {
                       AttributeLogicalName = attributeName,
                       EntityLogicalName = entityName,
                       Value = optionValue,
                       Label = new Label(optionText, languageCode)
                   };
                int insertOptionValue = ((InsertOptionValueResponse)service.Execute(insertOptionValueRequest)).NewOptionValue;
            }
            // Execute the request.
            
        }
        public void AssociateEntity(string PrimaryEntityName, Guid PrimaryEntityId, string _relationshipName, string _relationshipEntityName, string entityName, string ParentId)
        {
            try
            {
                EntityCollection relations = getAssociations(PrimaryEntityName, PrimaryEntityId, _relationshipName, _relationshipEntityName, entityName, ParentId);


                if (relations.Entities.Count == 0)
                {
                    EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
                    relatedEntities.Add(new EntityReference(entityName, new Guid(ParentId)));
                    Relationship relationship = new Relationship(_relationshipName);
                    service.Associate(PrimaryEntityName, PrimaryEntityId, relationship, relatedEntities);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : {0} - {1}", ex.Message, ex.StackTrace);
                //    objCommon.tracingService.Trace("Error : {0} - {1}", ex.Message, ex.StackTrace);//
                //throw ex;
                // if (ex.Detail.ErrorCode != 2147220937)//ignore if the error is a duplicate insert
                //{
                // throw ex;
                //}
            }
            
        }

        public EntityCollection getAssociations(string PrimaryEntityName, Guid PrimaryEntityId, string _relationshipName, string _relationshipEntityName, string entityName, string ParentId)
        {
            //
            string fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                      <entity name='" + PrimaryEntityName + @"'>
                                        <link-entity name='" + _relationshipEntityName + @"' from='" + PrimaryEntityName + @"id' to='" + PrimaryEntityName + @"id' visible='false' intersect='true'>
                                        <link-entity name='"+ PrimaryEntityName + @"' from='"+ PrimaryEntityName + @"id' to='"+ PrimaryEntityName + @"id' alias='ab'>
                                            <filter type='and'>
                                            <condition attribute='"+ PrimaryEntityName + @"id' operator='eq' value='"+ PrimaryEntityId.ToString()+ @"' />
                                            </filter>
                                        </link-entity>
                                        <link-entity name='" + entityName + @"' from='" + entityName + @"id' to='" + entityName + @"id' alias='ac'>
                                                <filter type='and'>
                                                  <condition attribute='" + entityName + @"id' operator='eq' value='" + ParentId + @"' />
                                                </filter>
                                              </link-entity>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
            
            EntityCollection relations = service.RetrieveMultiple(new FetchExpression(fetchXML));

            return relations;
        }

        public void UpdateChildRecords(string relationshipName, string parentEntityType, string parentEntityId, string parentFieldNameToUpdate, string setValueToUpdate, string childFieldNameToUpdate)
        {
            //1) Get child lookup field name
            RetrieveRelationshipRequest req = new RetrieveRelationshipRequest()
            {
                Name = relationshipName
            };
            RetrieveRelationshipResponse res = (RetrieveRelationshipResponse)service.Execute(req);
            OneToManyRelationshipMetadata rel = (OneToManyRelationshipMetadata)res.RelationshipMetadata;
            string childEntityType = rel.ReferencingEntity;
            string childEntityFieldName = rel.ReferencingAttribute;

            //2) retrieve all child records
            QueryByAttribute querybyattribute = new QueryByAttribute(childEntityType);
            querybyattribute.ColumnSet = new ColumnSet(childEntityFieldName);
            querybyattribute.Attributes.AddRange(childEntityFieldName);
            querybyattribute.Values.AddRange(new Guid(parentEntityId));
            EntityCollection retrieved = service.RetrieveMultiple(querybyattribute);

            //2') retrieve parent fielv value
            var valueToUpdate=new object();
            if (parentFieldNameToUpdate != null && parentFieldNameToUpdate != "")
            {
                Entity retrievedEntity = (Entity)service.Retrieve(parentEntityType, new Guid(parentEntityId), new ColumnSet(parentFieldNameToUpdate));
                if (retrievedEntity.Attributes.Contains(parentFieldNameToUpdate))
                {
                    valueToUpdate = retrievedEntity.Attributes[parentFieldNameToUpdate];
                }
                else
                {
                    valueToUpdate = null;
                }
            }
            else
            {
                valueToUpdate = setValueToUpdate;
            }

            //3) update each child record

            foreach (Entity child in retrieved.Entities)
            {
                if (childEntityType.ToLower() == "dynamicpropertyinstance")
                {
                    //pending...
                    UpdateProductPropertiesRequest req2 = new UpdateProductPropertiesRequest();
                   // req2.
                    break;
                }
                     
                Entity entUpdate = new Entity(childEntityType);
                entUpdate.Id = child.Id;
                entUpdate.Attributes.Add(childFieldNameToUpdate, valueToUpdate);
                service.Update(entUpdate);
            }


        }

    }


    public class AzureAuthToken
    {
        /// URL of the token service
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
        /// Name of header used to pass the subscription key to the token service
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
        /// After obtaining a valid token, this class will cache it for this duration.
        /// Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);

        /// Cache the value of the last valid token obtained from the token service.
        private string storedTokenValue = string.Empty;
        /// When the last valid token was obtained.
        private DateTime storedTokenTime = DateTime.MinValue;

        /// Gets the subscription key.
        public string SubscriptionKey { get; private set; } = string.Empty;

        /// Gets the HTTP status code for the most recent request to the token service.
        public HttpStatusCode RequestStatusCode { get; private set; }

        /// <summary>
        /// Creates a client to obtain an access token.
        /// </summary>
        /// <param name="key">Subscription key to use to get an authentication token.</param>
        public AzureAuthToken(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key", "A subscription key is required");
            }

            this.SubscriptionKey = key;
            this.RequestStatusCode = HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Gets a token for the specified subscription.
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public async Task<string> GetAccessTokenAsync()
        {
            if (SubscriptionKey == string.Empty) return string.Empty;

            // Re-use the cached token if there is one.
            if ((DateTime.Now - storedTokenTime) < TokenCacheDuration)
            {
                return storedTokenValue;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = ServiceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.SendAsync(request);
                this.RequestStatusCode = response.StatusCode;
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadAsStringAsync();
                storedTokenTime = DateTime.Now;
                storedTokenValue = "Bearer " + token;
                return storedTokenValue;
            }
        }

        /// <summary>
        /// Gets a token for the specified subscription. Synchronous version.
        /// Use of async version preferred
        /// </summary>
        /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
        /// <remarks>
        /// This method uses a cache to limit the number of request to the token service.
        /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
        /// request to the token service, this method caches the access token. Subsequent 
        /// invocations of the method return the cached token for the next 5 minutes. After
        /// 5 minutes, a new token is fetched from the token service and the cache is updated.
        /// </remarks>
        public string GetAccessToken()
        {
            // Re-use the cached token if there is one.
            if ((DateTime.Now - storedTokenTime) < TokenCacheDuration)
            {
                return storedTokenValue;
            }

            string accessToken = null;
            var task = Task.Run(async () =>
            {
                accessToken = await GetAccessTokenAsync();
            });

            while (!task.IsCompleted)
            {
                System.Threading.Thread.Yield();
            }
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            else if (task.IsCanceled)
            {
                throw new Exception("Timeout obtaining access token.");
            }
            return accessToken;
        }

    }

}
