using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

using Hagring;

public class FetchDeviceConfigError: Exception
{
    public FetchDeviceConfigError(Exception innerException) :
        base("Failed to fetch device config", innerException) { /* NOP */ }
}

public class DeviceConfigParseError : Exception
{
    public DeviceConfigParseError(Exception innerException) :
        base("Error parsing device config", innerException) { /* NOP */ }
}

public class FetchModelConfigError : Exception
{
    public FetchModelConfigError(Exception innerException) :
        base("Failed to fetch model config", innerException) { /* NOP */ }
}

public class ModelDownloadError : Exception
{
    public ModelDownloadError(Exception innerException) :
        base("Error downloading model", innerException) { /* NOP */ }
}

public class AuthenticationError : Exception
{
    public AuthenticationError(Exception innerException) :
        base("", innerException) { /* NOP */ }

}

public class ConnectionError : Exception
{
    public ConnectionError(Exception innerException) :
        base("", innerException) { /* NOP */ }
}

public class CloudAPI
{
    static public event ModelInstanceUpdatedHandler ModelInstanceUpdated;
    public delegate void ModelInstanceUpdatedHandler(ModelInstance instance);

    // disable warning "Field `XXX' is never assigned to" for
    // our JSON deserialization object fields
    #pragma warning disable 0649
    class GetDeviceResponse
    {
        public device[] devices;
    }

    public class device
    {
        public string serialNo;
    }

    public class Sweref99
    {
        public string projection;

        public double x;
        public double y;
        public double z;

        public double pitch;
        public double roll;
        public double yaw;
    }

    public class Itm
    {
        public double north;
        public double east;
        public double altitude;

        public double pitch;
        public double roll;
        public double yaw;
    }

    public class Position
    {
        public Sweref99 sweref99;
        public Itm itm;

        public GPSPosition GpsPos
        {
            get
            {
                if (itm != null)
                {
                    return new GPSPosition("itm", itm.north, itm.east, itm.altitude);
                }

                /* must be a sweref position */
                return new GPSPosition(
                    GPSPosition.hapiToInternalProj(sweref99.projection),
                    sweref99.x, sweref99.y, sweref99.z);
            }
            set
            {
                if (value.Projection.Equals("itm"))
                {
                    sweref99 = null;

                    itm = new Itm();
                    itm.north = value.North;
                    itm.east = value.East;
                    itm.altitude = value.Altitude;

                    return;
                }

                /* some sweref position */
                itm = null;

                sweref99 = new Sweref99();
                sweref99.projection = GPSPosition.internalToHAPIProj(value.Projection);
                sweref99.x = value.North;
                sweref99.y = value.East;
                sweref99.z = value.Altitude;
            }
        }

        public double yaw
        {
            get
            {
                if (itm != null)
                {
                    return itm.yaw;
                }
                return sweref99.yaw;
            }

            set
            {
                if (itm != null)
                {
                    itm.yaw = value;
                    return;
                }

                sweref99.yaw = value;
            }
        }
    }

    public class Model
    {
        public string folderId;
        public string model;
        public string name;
        public string importStatus;
        public Position defaultPosition;
    }

    public class ModelsResponse
    {
        public Model[] models;
    }

    public class Folder
    {
        public string id;
        public string name;
        public string parent;
    }

    public class FoldersResponse
    {
        public Folder[] folders;
    }

    public class ModelInstance
    {
        public string name;
        public string description;
        public bool hidden;
        public string instanceId;
        public string modelId;
        public Position position;
    }

    public class ModelInstanceUpdate
    {
        public string name;
        public string description;
        public bool hidden;
        public Position position;

        public ModelInstanceUpdate(ModelInstance inst)
        {
            this.name = inst.name;
            this.description = inst.description;
            this.hidden = inst.hidden;
            this.position = inst.position;
        }
    }

    class ModelInstancesResponse
    {
        public ModelInstance[] modelInstances;
    }

    class WSMessage
    {
        public string command;
        public string deviceName;
        public string data;
    }

    class WSDeviceStatusReply
    {
        public class DeviceStatusData
        {
            public class Position
            {
                public string currentProjectionRef;
                public double currentLatitude;
                public double currentLongitude;
                public double currentAltitude;
            }

            public string deviceName;
            public Position position = new Position();
        }

        public DeviceStatusData data = new DeviceStatusData();

        public WSDeviceStatusReply(string deviceName)
        {
            data.deviceName = deviceName;
        }

        public void SetPosition(string projectionRef, double longitude, double latitude, double altitude)
        {
            data.position.currentLongitude = longitude;
            data.position.currentLatitude = latitude;
            data.position.currentAltitude = altitude;
            data.position.currentProjectionRef = projectionRef;
        }
    }
    #pragma warning restore

    const string PROTOCOL = "https";
    const string HOST = "stable.brab.ws";

//    string DeviceSerialNo;

    Uri modelsUri;
    Uri foldersUri;
    Uri devicesUri;

    // Uri _modelInstancesUri = null;
    // Uri modelInstancesUri
    // {
    //     get
    //     {
    //         if (_modelInstancesUri == null)
    //         {
    //             _modelInstancesUri = BuildUri("/v1/device/{0}/models", DeviceSerialNo);
    //         }

    //         return _modelInstancesUri;
    //     }
    // }

    WSDeviceStatusReply devStatusReply;

    string Username;
    string Password;

    static CloudAPI _Instance = null;
    public static CloudAPI Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new CloudAPI();
            }

            return _Instance;
        }
    }

    CloudAPI()
    {
        SetupHttp();
    }

    public void SetUserCredentials(string uname, string passwd)
    {
        Username = uname;
        Password = passwd;
    }

    static bool DisableCertificateCheck(
        object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        /* it's always OK! */
        return true;
    }

    ///
    /// build complete Uri by combining cloud protocol and host
    /// with specified path
    ///
    static Uri BuildUri(string pathFormat, params object[] args)
    {
        var builder = new UriBuilder(PROTOCOL, HOST);
        builder.Path = string.Format(pathFormat, args);

        return builder.Uri;
    }

    void SetupHttp()
    {
        /* no certificate check */
        ServicePointManager.ServerCertificateValidationCallback = DisableCertificateCheck;

        /* constuct static API Uri's */
        modelsUri = BuildUri("v1/models");
        foldersUri = BuildUri("v1/folders");
        devicesUri = BuildUri("v1/devices");
    }

    void HandleUpdateModelInstanceCommand(string data)
    {
        var conf = JsonConvert.DeserializeObject<ModelInstance>(data);

        if (ModelInstanceUpdated != null)
        {
            ModelInstanceUpdated(conf);
        }
    }


    CredentialCache GetCredentials(Uri uri, string username, string password)
    {
        var credentials = new CredentialCache();

        /*
         * Use the specified credentials if provided otherwise fallback to
         * device's serial number as username and the password from cloud.json
         */
        credentials.Add(uri, "Basic",
                        new NetworkCredential(username, password));
        return credentials;
    }

    HttpWebResponse httpGetResponse(Uri uri)
    {
        var request = (HttpWebRequest)WebRequest.Create(uri);
        /*
         * disable automatic redirect flow feature
         *
         * it seems that something goes wrong when following
         * redirects to AWS S3 pre-signed URL,
         * we allways get 'bad request' exception,
         * the problem arises while downloading assets,
         * suspected mono bug
         *
         * we do 'manual' redirect following instead
         */
        request.AllowAutoRedirect = false;
        request.Credentials = GetCredentials(uri, Username, Password);

        return (HttpWebResponse)request.GetResponse();
    }

    class AsyncPutRequest
    {
        const int MAX_RETRIES = 5;

        WebRequest request;
        CredentialCache credentials;
        Uri uri;
        byte[] content;
        int retries = MAX_RETRIES;

        internal AsyncPutRequest(CredentialCache credentials, Uri uri, byte[] content)
        {
            this.credentials = credentials;
            this.uri = uri;
            this.content = content;
            StartRequest();
        }

        void StartRequest()
        {
            request = WebRequest.Create(uri);
            request.Credentials = credentials;
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.ContentLength = content.Length;

            request.BeginGetRequestStream(FinishRequest, null);
        }

        void SendGetData(IAsyncResult ar)
        {
            using (Stream bodyStream = request.EndGetRequestStream(ar))
            {
                bodyStream.Write(content, 0, content.Length);
            }
            request.GetResponse();
        }

        void FinishRequest(IAsyncResult ar)
        {
            try
            {
                SendGetData(ar);
            }
            catch (ObjectDisposedException)
            {
                /*
                 * for some reason we get an ObjectDisposedException while writing
                 * the body of HTTP PUT request, this is possibly a mono bug
                 *
                 * work around this bug by retrying making a HTTP PUT request
                 * a couple of times, as the exception is thrown intermittently
                 */
                Log.Wrn("ObjectDisposedException while doing HTTP PUT");
                if (retries > 0)
                {
                    /* let's try once more */
                    retries -= 1;
                    StartRequest();
                    return;
                }
                Log.Err("To many ObjectDisposedException, giving up");
            }
            catch (WebException e)
            {
                Log.Err("HTTP PUT to {0} failed: {1}", uri, e);
            }
        }
    }

    //
    // Make asynchronous HTTP PUT request, were we don't really care about a reply.
    //
    // Will only check HTTP status code for error code,
    // and log a message in case of errors.
    //
    // void httpPut(Uri uri, byte[] content)
    // {
    //     new AsyncPutRequest(GetCredentials(uri), uri, content);
    // }

    String httpGet(Uri uri)
    {
        String responseBody;

        using (HttpWebResponse response = httpGetResponse(uri))
        {
            StreamReader reader = new StreamReader(response.GetResponseStream());
            responseBody = reader.ReadToEnd();
        }

        return responseBody;
    }

    // public void Init(String DeviceSerialNo)
    // {
    //     this.main = main;
    //     this.DeviceSerialNo = DeviceSerialNo;
    //     webSocket = SetupWS();
    // }

    void CopyStream(Stream src, Stream dest)
    {
        /* like, ehh, 16 pages is good buffer size */
        int buffSize = 1024 * 4 * 16;
        byte[] buffer = new byte[buffSize];
        int bytesRead;

        do {
            bytesRead = src.Read(buffer, 0, buffSize);
            dest.Write(buffer, 0, bytesRead);
        } while (bytesRead > 0);
    }

    /// <summary>
    /// Handle redirection to AWS S3 pre-signed URL for the model asset bundle.
    /// Returns a http response object where the body is the asset bundle.
    /// </summary>
    HttpWebResponse GetModelDownloadResponse(Uri uri)
    {
        HttpWebResponse response = httpGetResponse(uri);

        /*
         * perform 'manual' redict follow, by making a new
         * request to redirected URL
         *
         * this is due to a possible mono bug, see comment in
         * httpGetResponse() for more information
         */
        var S3Uri = new Uri(response.Headers["Location"]);
        return httpGetResponse(S3Uri);
    }

    public void DownloadModel(string modelID, string assetVersion, String destFile)
    {
        var baseUri = new UriBuilder(PROTOCOL, HOST);
        baseUri.Path = string.Format("brab/v1/models/{0}/asset/{1}",
                                     modelID, assetVersion);

        Log.Msg("asset url {0}", baseUri);

        try
        {
            using (HttpWebResponse response = GetModelDownloadResponse(baseUri.Uri))
            using (FileStream fs = Utils.CreateFile(destFile))
            {
                CopyStream(response.GetResponseStream(), fs);
            }
        }
        catch (WebException e)
        {
            throw new ModelDownloadError(e);
        }
    }

    // public ModelInstance[] GetModelInstances()
    // {
    //     ModelInstancesResponse r;

    //     try
    //     {
    //         var responseText = httpGet(modelInstancesUri);
    //         Log.Msg("Got models configuration: {0}", responseText);
    //         r = JsonConvert.DeserializeObject<ModelInstancesResponse>(responseText);
    //     }
    //     catch (WebException e)
    //     {
    //         throw new FetchModelConfigError(e);
    //     }

    //     return r.modelInstances;
    // }

    public Model GetModel(string modelId)
    {
        var resp = httpGet(BuildUri("v1/models/{0}", modelId));
        return JsonConvert.DeserializeObject<Model>(resp);
    }

    ///
    /// Check if the WebException is one of the classes of
    /// error we want to handle, and throw an appropriate exception.
    ///
    /// Does nothing if WebException encoded an unhandled error.
    ///
    void MapWebException(WebException e)
    {
        /*
         * map the thrown exception to 3 classes of errors
         *
         * Authentication Error: the HTTP 401 reply      -> throw AuthenticationError exception
         * Connection Error:     can't reach the host    -> throw ConnectionError excepion
         * Unexpected Error:                             -> re-throw original exception
         */
        switch (e.Status)
        {
            case WebExceptionStatus.ProtocolError:
                {
                    var response = e.Response as HttpWebResponse;
                    if ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new AuthenticationError(e);
                    }
                }
                break;
            case WebExceptionStatus.NameResolutionFailure:
            case WebExceptionStatus.ConnectFailure:
                /*
                 * these errors may be caused by problems with internet connection
                 * on the client side, so handle them as separate class of problems
                 */
                throw new ConnectionError(e);
        }

        /*
         * unexpected error,
         * do nothing, let the caller deal with it
         */
    }

    public Model[] GetModels()
    {
        try
        {
            var resp = JsonConvert.DeserializeObject<ModelsResponse>(httpGet(modelsUri));
            return resp.models;
        }
        catch (WebException e)
        {
            MapWebException(e);
            /*
             * unexpected error while doing HTTP GET,
             * re-throw original exception
             */
            throw e;
        }
    }

    public Folder[] GetFolders()
    {
        var resp = JsonConvert.DeserializeObject<FoldersResponse>(httpGet(foldersUri));
        return resp.folders;
    }

    // public device[] GetDevices(string username, string password)
    // {
    //     try
    //     {
    //         GetDeviceResponse response = null;
    //         var responseText = httpGet(devicesUri, username, password);

    //         Log.Msg("Got devices : {0}", responseText);
    //         response = JsonConvert.DeserializeObject<GetDeviceResponse>(responseText);

    //         return response.devices;
    //     }
    //     catch (WebException e)
    //     {
    //         /*
    //          * map the thrown exception to 3 classes of errors
    //          *
    //          * Authentication Error: the HTTP 401 reply      -> throw AuthenticationError exception
    //          * Connection Error:     can't reach the host    -> throw ConnectionError excepion
    //          * Unexpected Error:                             -> re-throw original exception
    //          */
    //         switch (e.Status)
    //         {
    //             case WebExceptionStatus.ProtocolError:
    //                 {
    //                     var response = e.Response as HttpWebResponse;
    //                     if ((e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
    //                     {
    //                         throw new AuthenticationError(e);
    //                     }
    //                 }
    //                 break;
    //             case WebExceptionStatus.NameResolutionFailure:
    //             case WebExceptionStatus.ConnectFailure:
    //                 /*
    //                  * these errors may be caused by problems with internet connection
    //                  * on the client side, so handle them as separate class of problems
    //                  */
    //                 throw new ConnectionError(e);
    //         }

    //         /*
    //          * unexpected error while doing HTTP GET,
    //          * re-throw original exception
    //          */
    //         throw e;
    //     }
    // }

    // public void UpdateModelConfiguration(ModelInstance instance)
    // {
    //     var updateJson = JsonConvert.SerializeObject(
    //         new ModelInstanceUpdate(instance),
    //         Formatting.None,
    //         /* don't include null properties */
    //         new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

    //     httpPut(
    //         BuildUri("/v1/device/{0}/models/{1}", DeviceSerialNo, instance.instanceId),
    //         Encoding.UTF8.GetBytes(updateJson));
    // }
}
