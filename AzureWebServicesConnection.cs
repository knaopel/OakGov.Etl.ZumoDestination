using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OakGov.Etl.ZumoDestination
{
    public static class AzureWebServicesConnection
    {
        public static string Get(Uri GetUri, string Key, bool IsAdmin = false)
        {
            HttpWebRequest request = HttpWebRequest.Create(GetUri) as HttpWebRequest;
            request.Method = "GET";
            request.Accept = "application/json";
            if (IsAdmin)
            {
                request.Headers.Add("X-ZUMO-MASTER", Key);
            }
            else
            {
                request.Headers.Add("X-ZUMO-APPLICATION", Key);
            }
            request.ContentType = "application/json";
            request.Host = GetUri.Host;
            request.ContentLength = 0;
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                return streamReader.ReadToEnd();
            }
            catch (System.Net.WebException we)
            {
                return we.Message;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string InsertItem(Uri InsertUri, string InsertBody, string AdminKey)
        {
            HttpWebRequest request = HttpWebRequest.Create(InsertUri) as HttpWebRequest;
            request.Method = "POST";
            request.Accept = "application/json";
            request.Headers.Add("X-ZUMO-MASTER", AdminKey);
            request.ContentType = "application/json";
            request.Host = InsertUri.Host;
            request.ContentLength = InsertBody.Length;

            StreamWriter streamWriter = new StreamWriter(request.GetRequestStream());
            streamWriter.Write(InsertBody);
            streamWriter.Flush();

            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                StreamReader reader = new StreamReader(response.GetResponseStream());
                string result = reader.ReadToEnd();
                return result;
            }
            catch (WebException we)
            {
                return we.Message;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string UpdateItem(Uri UpdateUri, string UpdateBody, string AdminKey)
        {
            HttpWebRequest request = HttpWebRequest.Create(UpdateUri) as HttpWebRequest;
            request.Method = "PATCH";
            request.Accept = "application/json";
            request.Headers.Add("X-ZUMO-MASTER", AdminKey);
            request.ContentType = "application/json";
            request.Host = UpdateUri.Host;
            request.ContentLength = UpdateBody.Length;

            
            StreamWriter streamWriter = new StreamWriter(request.GetRequestStream());
            streamWriter.Write(UpdateBody);
            streamWriter.Flush();
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                StreamReader reader = new StreamReader(response.GetResponseStream());
                string result = reader.ReadToEnd();
                return result;
            }
            catch (System.Net.WebException we)
            {
                return we.Message;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string DeleteItem(Uri DeleteUri, string AdminKey)
        {
            HttpWebRequest request = HttpWebRequest.Create(DeleteUri) as HttpWebRequest;
            request.Method = "DELETE";
            request.Headers.Add("X-ZUMO-MASTER", AdminKey);
            request.ContentType = "application/json";
            request.Host = DeleteUri.Host;
            request.ContentLength = 0;
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                string result = streamReader.ReadToEnd();
                return result;
            }
            catch (WebException we)
            {
                if (we.Status == WebExceptionStatus.Timeout)
                {
                    return DeleteItem(DeleteUri, AdminKey);
                }
                else
                {
                    return we.Message;
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static bool DeleteBatch(Uri DeleteUri, string DeleteBatchBody, string AdminKey)
        {
            HttpWebRequest request = HttpWebRequest.Create(DeleteUri) as HttpWebRequest;
            request.Method = "POST";
            request.Accept = "application/json";
            request.Headers.Add("X-ZUMO-MASTER", AdminKey);
            request.ContentType = "application/json";
            request.Host = DeleteUri.Host;
            request.ContentLength = DeleteBatchBody.Length;

            StreamWriter streamWriter = new StreamWriter(request.GetRequestStream());
            streamWriter.Write(DeleteBatchBody);
            streamWriter.Flush();

            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                string result = streamReader.ReadToEnd();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class DeleteBatch
    {
        public string tableName { get; set; }
        public string ids { get; set; }
    }
}
