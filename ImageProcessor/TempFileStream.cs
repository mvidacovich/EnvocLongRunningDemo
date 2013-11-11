using System;
using System.IO;

namespace ImageProcessor
{
    public sealed class TempFileStream : Stream
    {
        private readonly string tempFilePath;
        private readonly FileStream fileStream;

        public TempFileStream(string tempFilePath, Stream sourceStream)
        {
            this.tempFilePath = tempFilePath;
            fileStream = InitializeStream(sourceStream);
        }

        private FileStream InitializeStream(Stream file)
        {
            FileStream result;
            const int buffer = 4 * 1024 * 1024;
            using (var stream = File.Create(tempFilePath, buffer))
            {
                file.CopyTo(stream, buffer);
                stream.Flush(true);
                stream.Dispose();
                file.Dispose();

                try
                {
                    result = File.Open(tempFilePath, FileMode.Open);
                }
                catch (Exception)
                {
                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                    }
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                    throw;
                }
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            fileStream.Dispose();
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            fileStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return fileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return fileStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return fileStream.Length; }
        }

        public override long Position
        {
            get { return fileStream.Position; }
            set { fileStream.Position = value; }
        }
    }
}