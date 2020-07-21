using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Server.Data;
using Util.Functional;

namespace Server
{
    public static class GameStateIO
    {
        public class ErrorSavingGame : IError
        {
            private readonly JsonException exception;
            public ErrorSavingGame(JsonException exception) => this.exception = exception;
            public string Message => $"Error saving game: {exception.Message}";
        }
        public class ErrorLoadingGame : IError
        {
            private readonly JsonException exception;
            public ErrorLoadingGame(JsonException exception) => this.exception = exception;
            public string Message => $"Error loading game: {exception.Message}";
        }

        private static JsonSerializerSettings CreateSerializationContext(Util.Database context) => new JsonSerializerSettings()
        {
            Context = new StreamingContext(StreamingContextStates.All, context)
        };

        public static IError? SaveToStream(GameState self, TextWriter outStream, Util.Database context)
        {
            try
            {
                outStream.Write(JsonConvert.SerializeObject(self, CreateSerializationContext(context)));
                return null;
            }
            catch (JsonException ex)
            {
                return new ErrorSavingGame(ex);
            }
        }

        public static Result<GameState> LoadFromString(string str, Util.Database context)
        {
            try
            {
                return Result.Ok(JsonConvert.DeserializeObject<GameState>(str, CreateSerializationContext(context))!);
            }
            catch (JsonException ex)
            {
                return Result.Error(new ErrorLoadingGame(ex));
            }
        }
    }
}