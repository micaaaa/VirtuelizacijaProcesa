using System;
using System.IO;

namespace Common
{
    public class FileHandler : IDisposable
    {
        private TextWriter textWriter;
        private TextReader textReader;
        private bool disposed = false;
        private string path;

        public string Path { get => path; }

        public FileHandler(string filePath)
        {
            path = filePath;
        }

        ~FileHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    textWriter?.Dispose();
                    textReader?.Dispose();
                }

                disposed = true;
            }
        }

        public void WriteToFile(string content)
        {
            if (textWriter == null)
            {
                textWriter = File.AppendText(path);
            }

            textWriter.WriteLine(content);
            textWriter.Flush();
            textWriter.Close();
            textWriter = null;
        }

        public string ReadFromFile()
        {
            if (textReader == null)
            {
                textReader = File.OpenText(path);
            }

            string content = textReader.ReadToEnd();
            textReader.Close();
            textReader = null;
            return content;
        }

        public void DeleteAllContent()
        {
            File.WriteAllText(path, string.Empty);
        }
    }
}
