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
        public Vec2i Facing { get; set; }
        public bool ShowFacingMarker { get => facingMarker.IsVisible; set => facingMarker.IsVisible = value; }

        private Entity facingMarker;

        // TODO!: This constructor should take a Font instead of using the parent console's Font.
        //        Once that's taken care of, remove the requirement entirely and have caller set parent.
        public MapActor(SadConsole.Console parent, DataHandle<Actor> actor) : base(1,1)
        {
            ParentTileMap = parent;

            this.Actor = actor;
            Animation.Font = parent.Font;
            Animation.FontSize = parent.FontSize;
            
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

        public void Sync(GameServer server)
            => server.QueryData(Actor, actor => {
                Animation.Surface.Cells[0] = new SadConsole.ColoredGlyph
                {
                    Foreground = Color.White,
                    Background = Color.Transparent,
                    Glyph = actor.AiType == nameof(Server.Logic.AIType.PlayerControlled) ? 707 : 125,
                };
                Position = new Point(actor.Position.x, actor.Position.y)
                    .SurfaceLocationToPixel(Animation.FontSize.X, Animation.FontSize.Y);
                Facing = actor.Facing;
            });

        public override void Update(System.TimeSpan delta)
        {
            base.Update(delta);
        }

        public override void Draw(System.TimeSpan delta)
        {
            facingMarker.Animation.Surface.Cells[0].Glyph = Facing switch
            {
                (-1, 0) => 2,
                (0, -1) => 3,
                (1, 0) => 4,
                (0, 1) => 5,
                (-1, 1) => 6,
                (-1, -1) => 7,
                (1, -1) => 8,
                (1, 1) => 9,
                _ => 0
            };
            facingMarker.Animation.IsDirty = true;
            base.Draw(delta);
        }
    }
}