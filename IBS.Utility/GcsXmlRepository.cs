using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;

namespace IBS.Utility
{
    public class GcsXmlRepository(StorageClient storageClient, string bucketName, string objectName)
        : IXmlRepository
    {
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            try
            {
                using var stream = new MemoryStream();
                storageClient.DownloadObject(bucketName, objectName, stream);
                stream.Position = 0;

                var doc = XDocument.Load(stream);
                return (doc.Root?.Elements() ?? []).ToList();
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // No keys yet — safe to ignore
                return new List<XElement>();
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var elements = GetAllElements().ToList();
            elements.Add(element);

            var doc = new XDocument(new XElement("root", elements));

            using var stream = new MemoryStream();
            doc.Save(stream);
            stream.Position = 0;

            storageClient.UploadObject(bucketName, objectName, "application/xml", stream);
        }
    }
}
