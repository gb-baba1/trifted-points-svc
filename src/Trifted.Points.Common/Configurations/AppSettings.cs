namespace Trifted.Points.Common.Configurations
{
    public class AppSettings
    {
        public bool RunEtlPackage { get; set; }

        public Adapter Adapter { get; set; }

        public string AllowedCorsOrigin { get; set; }

        public string AwsAccessKeyId { get; set; }

        public string AwsAccessSecretKey { get; set; }

        public string DatabaseNamespace { get; set; }

        public string AwsRegion { get; set; }

        public ServiceBaseUrl ServiceBaseUrl { get; set; }
    }

    public class Adapter
    {
    }


    public class ServiceBaseUrl
    {
    }
}