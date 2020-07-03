using Server.Data;
using SadConsole.Entities;

using Point = SadRogue.Primitives.Point;
using Color = SadRogue.Primitives.Color;

using static SadConsole.PointExtensions;
using Server;

namespace Client
{
    public class MapActor : Entity
    {
        public DataHandle<Actor> Actor { get; }
        public SadConsole.Console ParentTileMap { get; }

        public MapActor(SadConsole.Console parent, GameServer server, DataHandle<Actor> actor) : base(1,1)
        {
            parent.Children.Add(this);
            this.Parent = parent;
            ParentTileMap = parent;

            this.Actor = actor;
            Animation.Font = parent.Font;
            Animation.FontSize = parent.FontSize;

            server.QueryData(Actor, actor => {
                Animation.Surface.Cells[0] = new SadConsole.ColoredGlyph
                {
                    Foreground = Color.White,
                    Background = Color.Transparent,
                    Glyph = actor.aiType == nameof(Server.Logic.AIType.PlayerControlled) ? 707 : 125,
                };
                Position = new Point(actor.position.x, actor.position.y)
                    .SurfaceLocationToPixel(ParentTileMap.FontSize.X, ParentTileMap.FontSize.Y);
            });
            
            Animation.UsePixelPositioning = true;
        }
    }
}