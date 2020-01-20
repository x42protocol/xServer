using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using X42.Utilities;

namespace X42.Feature.X42Client.Utils.Web
{
    /// <inheritdoc />
    /// <summary>
    ///     Used For Making REST API Requests & Parsing Responses
    /// </summary>
    public class ApiClient : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;
        private bool disposed;
        private HttpClient httpClient;

        /// <summary>
        ///     Base Address For Requests
        /// </summary>
        /// <param name="serviceAddress">e.g. "http://moocow.com/api"</param>
        /// <param name="mainLogger"> for use with main logging in application</param>
        public ApiClient(string serviceAddress, ILogger mainLogger)
        {
            logger = mainLogger;
            InitClient(serviceAddress);
        }

        public ApiClient(string serviceAddress, string username, string password, ILogger mainLogger)
        {
            logger = mainLogger;
            InitClient(serviceAddress, username, password);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Setup The Client For Use
        /// </summary>
        private void InitClient(string serviceAddress, string username = null, string password = null)
        {
            Guard.Null(serviceAddress, nameof(serviceAddress), "API Service Address Cannot Be Null/Empty!");

            httpClient = new HttpClient
            {
                BaseAddress = new Uri(serviceAddress)
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            //httpClient.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
            //httpClient.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("defalte"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(new ProductHeaderValue("xServer", "4.0")));

            //if creds were provided then add a basic auth header
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password))));
            }
            else
            {
                logger.LogInformation($"API Client With Base Address '{serviceAddress}' Setup");
            }
        } //end of private void InitClient(string serviceAddress, string username = null, string password = null)


        /// <summary>
        ///     Send A Post Request To A Remote API
        /// </summary>
        /// <typeparam name="T">Class of Expected Response To Parse</typeparam>
        /// <param name="apiURL">Custom API Endpoint</param>
        /// <param name="PostData">Data To Send</param>
        /// <returns></returns>
        public virtual async Task<T> SendPost<T>(string apiURL, Dictionary<string, string> PostData)
        {
            try
            {
                Guard.Null(apiURL, nameof(apiURL));
                Guard.Null(PostData, nameof(PostData),
                    $"PostData For Request '{httpClient.BaseAddress}/{apiURL}' Cannot Be Empty/Null!");

                logger.LogDebug($"Sending Post Request To '{apiURL}' With Data:\n {PostData}");

                HttpResponseMessage _Result = await httpClient.PostAsync(apiURL, new FormUrlEncodedContent(PostData));
                if (_Result == null)
                {
                    logger.LogDebug($"Post Response From '{apiURL}' Is NULL!");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                } //end of if (_Result == null)


                if (!_Result.IsSuccessStatusCode)
                {
                    logger.LogDebug($"Post Response From '{apiURL}' Is HTTP 500 (Server Error)");
                    throw new Exception($"Error Response Returned - HTTP Code {_Result.StatusCode}");
                } //end of if (!_Result.IsSuccessStatusCode)

                logger.LogDebug($"Recieved HTTP 200 From '{apiURL}'");

                string _ObjectData = await _Result.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(_ObjectData))
                {
                    logger.LogDebug($"Response Data From '{apiURL}' Is NULL/Empty");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                } //end of if (string.IsNullOrWhiteSpace(_ObjectData))

                logger.LogDebug($"Recieved Data From Request To '{apiURL}'");

                return JsonConvert.DeserializeObject<T>(_ObjectData);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Exception When Sending POST Request To '{apiURL}'\n Data:\n{PostData}", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<T> SendPost<T>(string apiURL, Dictionary<string, string> PostData)


        /// <summary>
        ///     Send A Post Request To A Remote API
        /// </summary>
        /// <typeparam name="T">Class of Expected Response To Parse</typeparam>
        /// <param name="apiURL">Custom API Endpoint</param>
        /// <param name="PostData">Data To Send As JSON</param>
        /// <returns></returns>
        public virtual async Task<T> SendPostJSON<T>(string apiURL, object PostData)
        {
            try
            {
                Guard.Null(apiURL, nameof(apiURL));
                Guard.Null(PostData, nameof(PostData), $"PostData For Request '{httpClient.BaseAddress}/{apiURL}' Cannot Be Empty/Null!");

                logger.LogDebug($"Sending Post Request To '{apiURL}' With Data:\n {PostData}");

                HttpContent content = new StringContent(JsonConvert.SerializeObject(PostData), Encoding.Unicode, "application/json");
                HttpResponseMessage _Result = await httpClient.PostAsync(apiURL, content);
                if (_Result == null)
                {
                    logger.LogDebug($"Post Response From '{apiURL}' Is NULL!");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                } //end of if (_Result == null)


                if (!_Result.IsSuccessStatusCode)
                {
                    logger.LogDebug($"Post Response From '{apiURL}' Is HTTP 500 (Server Error)");
                    throw new Exception($"Error Response Returned - HTTP Code {_Result.StatusCode}");
                } //end of if (!_Result.IsSuccessStatusCode)

                logger.LogDebug($"Recieved HTTP 200 From '{apiURL}'");

                string _ObjectData = await _Result.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(_ObjectData))
                {
                    logger.LogDebug($"Response Data From '{apiURL}' Is NULL/Empty");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                } //end of if (string.IsNullOrWhiteSpace(_ObjectData))

                logger.LogDebug($"Recieved Data From Request To '{apiURL}'");

                return JsonConvert.DeserializeObject<T>(_ObjectData);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Exception When Sending POST Request To '{apiURL}'\n Data:\n{PostData}", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<T> SendPost<T>(string apiURL, Dictionary<string, string> PostData)


        public virtual async Task<HttpStatusCode> SendPost(string apiURL, object PostData)
        {
            try
            {
                Guard.Null(apiURL, nameof(apiURL));
                Guard.Null(PostData, nameof(PostData),
                    $"PostData For Request '{httpClient.BaseAddress}/{apiURL}' Cannot Be Empty/Null!");

                logger.LogDebug($"Sending Post Request To '{apiURL}' With Data:\n {PostData}");

                HttpResponseMessage _Result = await httpClient.PostAsync(apiURL,
                    new StringContent(JsonConvert.SerializeObject(PostData), Encoding.UTF8, "application/json"));
                if (_Result == null)
                {
                    logger.LogDebug($"Post Response From '{apiURL}' Is NULL!");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                }

                logger.LogDebug($"Recieved Data From Request To '{apiURL}', With Status Code '{_Result.StatusCode}'");

                return _Result.StatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Exception When Sending POST Request To '{apiURL}'\n Data:\n{PostData}", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<bool> SendPostBool(string apiURL, object PostData)


        /// <summary>
        ///     Send a GET Request
        /// </summary>
        /// <typeparam name="T">Class of Expected Response To Parse</typeparam>
        /// <param name="apiURL">Custom API Endpoint</param>
        /// <returns></returns>
        public virtual async Task<T> SendGet<T>(string apiURL)
        {
            try
            {
                Guard.Null(apiURL, nameof(apiURL));

                logger.LogDebug($"Sending GET To '{apiURL}'");

                HttpResponseMessage _Result = await httpClient.GetAsync(apiURL);
                if (_Result == null)
                {
                    logger.LogDebug($"Get Response From '{apiURL}' Is NULL!");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                } //end of if (_Result == null)

                if (!_Result.IsSuccessStatusCode)
                {
                    logger.LogDebug($"Get Response From '{apiURL}' Is HTTP 500 (Server Error)");
                    throw new Exception($"Error Response Returned - HTTP Code {_Result.StatusCode}");
                } //end of if (!_Result.IsSuccessStatusCode)

                string _ObjectData = await _Result.Content.ReadAsStringAsync();
                //if (string.IsNullOrWhiteSpace(_ObjectData)) { throw new ErrorException(500, $"Error Deserializing Object"); }

                if (string.IsNullOrWhiteSpace(_ObjectData))
                {
                    logger.LogDebug($"Get Response Data From '{apiURL}' Is NULL/Empty");
                    throw new Exception($"Error Response Returned - HTTP Code {_Result.StatusCode}");
                } //end of if (string.IsNullOrWhiteSpace(_ObjectData))

                logger.LogDebug($"Recieved Data From Request To '{apiURL}'");

                return JsonConvert.DeserializeObject<T>(_ObjectData);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error Sending GET To '{apiURL}'", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<T> SendGet<T>(string apiURL)


        /// <summary>
        ///     Send a GET Request
        /// </summary>
        /// <typeparam name="T">Class of Expected Response To Parse</typeparam>
        public virtual async Task<HttpStatusCode> SendGet(string apiURL)
        {
            try
            {
                Guard.Null(apiURL, nameof(apiURL));

                logger.LogDebug($"Sending GET Request To '{apiURL}'");

                HttpResponseMessage _Result = await httpClient.GetAsync(apiURL);
                if (_Result == null)
                {
                    logger.LogError($"Get Response From '{apiURL}' Is NULL!");
                    throw new Exception("Result Object From Web Request Is NULL!!");
                } //end of if (_Result == null)

                logger.LogDebug($"Recieved Data From Request To '{apiURL}', With Status Code '{_Result.StatusCode}'");

                return _Result.StatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Exception When Sending GET Request To '{apiURL}'", ex);
                throw;
            } //end of try-catch
        } //end of  public async Task<bool> SendGetBool(string apiURL)

        private void Dispose(bool disposing)
        {
            if (disposed || !disposing) return;


            if (httpClient != null)
            {
                HttpClient hc = httpClient;
                httpClient = null;
                hc.Dispose();
            }

            disposed = true;
        } //end of private void Dispose(bool disposing)
    } //end of public class APIClient : IDisposable
}