using Server.Data;
using SadConsole.Entities;

using Point = SadRogue.Primitives.Point;
using Color = SadRogue.Primitives.Color;

using static SadConsole.PointExtensions;
using Server;
using Util.Functional;

namespace Client
{
    public class MapActor : Entity
    {
        public DataHandle<Actor> Actor { get; }
        public Direction Facing { get; set; }
        public bool ShowFacingMarker { get => facingMarker.IsVisible; set => facingMarker.IsVisible = value; }

        public string DisplayName { get; private set; }

        private readonly Entity facingMarker;

        public MapActor(DataHandle<Actor> actor) : base(1,1)
        {
            DisplayName = "[NAME NOT SET]";

            Actor = actor;
            
            Animation.UsePixelPositioning = true;

            var facingFont = SadConsole.GameHost.Instance.Fonts["Directionals"];
            facingMarker = new Entity(1, 1, facingFont, facingFont.GetFontSize(SadConsole.Font.Sizes.Four));

            facingMarker.Animation.Surface.Cells[0] = new SadConsole.ColoredGlyph
            {
                Foreground = Color.Yellow,
                Background = Color.Transparent,
                Glyph = 2,
            };
            facingMarker.Animation.UsePixelPositioning = true;
            facingMarker.Parent = this;
        }

        public class TilesetNotFound : IError
        {
            private string key;
            public TilesetNotFound(string key) => this.key = key;
            public string Message => $"No tileset '{key}' found!";
        }

        private Result<SadConsole.Font> GetFontByName(string key)
        {
            if (!SadConsole.GameHost.Instance.Fonts.TryGetValue(key, out var font))
                return Result.Error(new TilesetNotFound(key));
            return Result.Ok(font);
        }

        private int SetAppearance(Database.ActorAppearance appearance, SadConsole.Font font)
        {
            Animation.Font = font;
            Animation.FontSize = Animation.Font.GetFontSize(SadConsole.Font.Sizes.Four);

            Animation.Surface.Cells[0] = new SadConsole.ColoredGlyph
            {
                Foreground = Color.White,
                Background = Color.Transparent,
                Glyph = appearance.Glyph,
            };

            return 0;
        }

        public void Sync(IClientContext clientContext, GameServer server)
            => server.QueryData(Actor, actor => {
                var result =
                    from appearance in clientContext.Database.Lookup<Database.ActorAppearance>(actor.Archetype.AppearanceId)
                    from font in GetFontByName(appearance.Tileset)
                    select SetAppearance(appearance, font);
                
                if (!result.IsSuccess)
                {
                    // TODO: replace with actual logging call!
                    System.Console.WriteLine($"Warning: {result.Err.Message}");

                    Animation.Font = SadConsole.GameHost.Instance.Fonts["Tileset"];
                    Animation.FontSize = Animation.Font.GetFontSize(SadConsole.Font.Sizes.Four);

                    Animation.Surface.Cells[0] = new SadConsole.ColoredGlyph
                    {
                        Foreground = Color.White,
                        Background = Color.Transparent,
                        Glyph = 1,
                    };
                }

                if (clientContext.StringTable.TryGetValue(actor.Archetype.NameId, out var name))
                {
                    DisplayName = name;
                }
                else
                {
                    // TODO: replace with actual logging call!
                    System.Console.WriteLine($"Unable to find ID '{actor.Archetype.NameId}' in string table!");
                    DisplayName = actor.Archetype.NameId;
                }

                Position = new Point(actor.Position.x, actor.Position.y)
                    .SurfaceLocationToPixel(Animation.FontSize.X, Animation.FontSize.Y);
                Facing = actor.Facing;
            });

        public override void Update(System.TimeSpan delta)
        {
            base.Update(delta);
        }

        public override void Render(System.TimeSpan delta)
        {
            facingMarker.Animation.Surface.Cells[0].Glyph = Facing switch
            {
                Direction.W => 2,
                Direction.N => 3,
                Direction.E => 4,
                Direction.S => 5,
                Direction.SW => 6,
                Direction.NW => 7,
                Direction.NE => 8,
                Direction.SE => 9,
                _ => 0
            };
            facingMarker.Animation.IsDirty = true;
            base.Render(delta);
        }
    }
}