using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Util;
using Util.Functional;

namespace Client
{
    public interface IClientContext
    {
        Dictionary<string, string> StringTable { get; }
        Util.Database Database { get; }
        IFileSystem Filesystem { get; }
    }

    public class ErrorParsingStringTable : IError
    {
        private readonly JsonException ex;
        public ErrorParsingStringTable(JsonException ex) => this.ex = ex;
        public string Message => $"Error parsing string table: {ex.Message}";
    }

    public class ClientContext : IClientContext
    {
        public Dictionary<string, string> StringTable { get; }
        public Util.Database Database { get; }
        public IFileSystem Filesystem { get; }

        public ClientContext(Dictionary<string, string> stringTable,
                             Util.Database database,
                             IFileSystem filesystem)
        {
            StringTable = stringTable;
            Database = database;
            Filesystem = filesystem;
        }

        private static Result<Dictionary<string, string>> ParseStringTable(string txt)
        {
            try
            {
                return Result.Ok(JsonConvert.DeserializeObject<Dictionary<string, string>>(txt));
            }
            catch (JsonException ex)
            {
                return Result.Error(new ErrorParsingStringTable(ex));
            }
        }

        private static Result<Util.Database> LoadDefaultClientDatabase(IFileSystem fs)
        {
            Util.Database db = new Util.Database();

            Result<Unit> read_table<T>(string path) => 
                fs.ReadAllText(path)
                    .Bind(ArchetypeJson.Read<T>)
                    .Finally(table => db.AddDatabase(table));

            return
                read_table<Database.ActorAppearance>("Resources/Data/Client/ActorAppearance.json")
                .Map(_ => db);
        }

        public static Result<ClientContext> Construct(IFileSystem fs) 
             => from stringTable in 
                    fs.ReadAllText("Resources/Data/Client/StringTable.json")
                        .Bind(ParseStringTable)
                from db in
                    LoadDefaultClientDatabase(fs)
                select new ClientContext(stringTable, db, fs);
    }
}