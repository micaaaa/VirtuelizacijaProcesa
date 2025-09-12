using System;
using System.IO;

namespace Common
{
    public class FileHandler : IDisposable
    {
        private FileStream fileStream;
        private StreamWriter writer;
        private bool disposed = false;

        public FileHandler(string filePath)
        {
            fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            writer = new StreamWriter(fileStream);
        }

        public void WriteToFile(string content)
        {
            if (disposed)
                throw new ObjectDisposedException("FileHandler");

            writer.WriteLine(content);
            writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                writer?.Dispose();
                fileStream?.Dispose();
            }

            disposed = true;
        }

        ~FileHandler()
        {
            Dispose(false);
        }
    }
}
