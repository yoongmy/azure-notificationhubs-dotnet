using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;

namespace SendRestExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse the hubname and connection string.
            string hubName = ConfigurationManager.AppSettings["HubName"];
            string fullConnectionString = ConfigurationManager.AppSettings["DefaultFullSharedAccessSignature"];
            string exit = string.Empty;
            do
            {
                Console.Write("Enter notification platform (android or ios): ");
                string platform = Console.ReadLine();

                Console.Write("Enter notification tag: ");
                string tag = Console.ReadLine();

                Console.Write("Enter notification message: ");
                string msg = Console.ReadLine();

                // Example sending a native notification
                Console.WriteLine("\nSending.... ");
                string result = SendNativeNotificationREST(hubName, fullConnectionString, msg, "Test", platform, tag).Result;

                if (result != null)
                    Console.WriteLine(string.Format("Result: {0}", result) + "\n");
                else
                    Console.WriteLine("Something went wrong, please try again");

                do
                {
                    Console.Write("Do you want to send push notification again? Repeat(r)/Restart(y)/Exit(n): ");
                    exit = Console.ReadLine();

                    if (exit.ToLower() == "r")
                    {
                        Console.WriteLine("\nSending.... ");
                        result = SendNativeNotificationREST(hubName, fullConnectionString, msg, "Test", platform, tag).Result;

                        if (result != null)
                            Console.WriteLine(string.Format("Result: {0}", result) + "\n");
                        else
                            Console.WriteLine("Something went wrong, please try again");
                    }

                } while (exit.ToLower() == "r");

            } while (exit.ToLower() == "y");
        }

        private static async Task<string> SendNativeNotificationREST(string hubName, string connectionString, string message, string title, string nativeType, string tag)
        {
            var connectionSaSUtil = new ConnectionStringUtility(connectionString);

            var hubResource = "messages/?";
            var apiVersion = "api-version=2015-04";

            //=== Generate SaS Security Token for Authentication header ===
            // Determine the targetUri that we will sign
            var uri = connectionSaSUtil.Endpoint + hubName + "/" + hubResource + apiVersion;

            // 10 min expiration
            var sasToken = connectionSaSUtil.GetSaSToken(uri, 10);

            WebHeaderCollection headers = new WebHeaderCollection();
            string body;
            HttpWebResponse response = null;

            switch (nativeType.ToLower())
            {

                case "ios":
                    headers.Add("ServiceBusNotification-Format", "apple");
                    headers.Add("ServiceBusNotification-Tags", tag);
                    body = "{\"aps\":{\"alert\":{\"title\":\"" + title + "\", \"body\":\"" + message + "\"}}}";
                    response = await ExecuteREST("POST", uri, sasToken, headers, body);
                    break;

                case "android":
                    headers.Add("ServiceBusNotification-Format", "gcm");
                    headers.Add("ServiceBusNotification-Tags", tag);
                    body = "{\"data\":{\"title\":\"" + title + "\", \"body\":\"" + message + "\", \"priority\":\"high\"}}";
                    response = await ExecuteREST("POST", uri, sasToken, headers, body);
                    break;
            }

            if (response != null && response.StatusCode == HttpStatusCode.Created)
            {
                return "Notification successfully sent";
            }
            else if (response != null && response.StatusCode == HttpStatusCode.BadRequest)
            {
                return "The request is malformed (for example, not valid routing headers, not valid content-type, message exceeds size, bad message format)";
            }
            else if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Authorization failure. The access key was incorrect.";
            }
            else if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
            {
                return "Quota exceeded or message too large; message was rejected.";
            }
            else if (response != null && response.StatusCode == HttpStatusCode.NotFound)
            {
                return "No message branch at the URI.";
            }
            else if (response != null && response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                return "Requested entity too large. The message size cannot be over 64 Kb.";
            }

            return null;
        }
        private static async Task<HttpWebResponse> ExecuteREST(string httpMethod, string uri, string sasToken, WebHeaderCollection headers = null, string body = null, string contentType = "application/json")
        {
            //=== Execute the request 

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            HttpWebResponse response = null;

            request.Method = httpMethod;
            request.ContentType = contentType;
            request.ContentLength = 0;

            if (sasToken != null)
                request.Headers.Add("Authorization", sasToken);

            if (headers != null)
            {
                request.Headers.Add(headers);
            }

            if (body != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body);

                try
                {
                    request.ContentLength = bytes.Length;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    response = (HttpWebResponse)we.Response;
                }
                else
                    Console.WriteLine(we.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return response;
        }
    }
}
