using System;
using System.IO;
using Util.Functional;

namespace Client
{
    public interface IFileSystem
    {
        Result<string> ReadAllText(string id);
    }

    public class ExceptionReadingFile : IError
    {
        private readonly Exception innerException;

        public ExceptionReadingFile(Exception innerException) => this.innerException = innerException;

        public string Message => $"Exception while reading file: {innerException.Message}";
    }

    public class LooseFileSystem : IFileSystem
    {
        public Result<string> ReadAllText(string id)
        {
            try
            {
                return Result.Ok(File.ReadAllText(id));
            }
            catch (Exception ex)
            {
                return Result.Error(new ExceptionReadingFile(ex));
            }
        }
    }
}