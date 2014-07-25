using Envoc.AzureLongRunningTask.AzureCommon.Models.Queues;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Blob;
using Envoc.AzureLongRunningTask.AzureCommon.Persistance.Queues;
using Envoc.AzureLongRunningTask.AzureCommon.Service;
using Envoc.AzureLongRunningTask.Common.Models;
using RestSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;

namespace ImageProcessor
{
    public class ProcessImageJobService : QueueProcessorBase<ProcessImageJob>
    {
        private readonly IStorageContext<FileBlob> fileStorageContext;
        private readonly IStorageContext<ResultBlob> resultStorageContext;

        public ProcessImageJobService(IQueueContext<ProcessImageJob> queueContext, 
            IStorageContext<FileBlob> fileStorageContext,
            IStorageContext<ResultBlob> resultStorageContext)
            : base(queueContext)
        {
            this.fileStorageContext = fileStorageContext;
            this.resultStorageContext = resultStorageContext;
        }

        protected override bool Process(IQueueEntity<ProcessImageJob> job, CancellationToken processJobToken)
        {
            var filePath = job.Value.FilePath;
            var file = fileStorageContext.GetBlob(filePath);
            if (file == null)
            {
                // TODO: error notifications and such
                return true;
            }

            var bitmap = GetBitmap(file);
            if (bitmap == null)
            {
                file.Stream.Dispose();
                // TODO: error notifications and such
                return true;
            }

            var results = DoComplicatedWork(bitmap);
            var blob = new ResultBlob
            {
                Stream = new MemoryStream(),
                Name = string.Format("{0}/image.png", job.Value.RequestId)
            };
            results.Save(blob.Stream, ImageFormat.Png);
            blob.Stream.Position = 0;
            resultStorageContext.Store(blob);
            results.Dispose();
            bitmap.Dispose();

            var restClient = new RestClient();
            var request = new RestRequest(job.Value.PostbackUrl, Method.POST);
            request.AddParameter("apikey", job.Value.ApiKey);
            request.AddParameter("requestid", job.Value.RequestId);
            request.AddParameter("resultpath", blob.Name);

            try
            {
                var result = restClient.Execute(request);
                return result.ResponseStatus == ResponseStatus.Completed && result.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return false;
            }
        }

        private static Bitmap GetBitmap(IFileBlob imageBlob)
        {
            try
            {
                var filename = Path.GetFileName(imageBlob.Name) ?? "temp.png";
                // Make sure you allocate space on the role's settings.
                Directory.CreateDirectory("temp");
                var filePath = Path.Combine("temp", filename);
                using (var fileStream = new TempFileStream(filePath, imageBlob.Stream))
                {
                    return new Bitmap(fileStream);
                }
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                imageBlob.Stream.Dispose();
            }
        }

        private static Bitmap DoComplicatedWork(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height);
            var graphics = Graphics.FromImage(result);

            // Invert the image
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix(
                new[]
                {
                    new float[] {-1, 0, 0, 0, 0},
                    new float[] {0, -1, 0, 0, 0},
                    new float[] {0, 0, -1, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {1, 1, 1, 0, 1}
                }));
            graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 1, 1, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            
            // Draw helpful text
            graphics.DrawString("Hello Azure", new Font("ariel", source.Width / 10.0f), Brushes.Red, 0, 0);
            graphics.Dispose();
            return result;
        }
    }
}