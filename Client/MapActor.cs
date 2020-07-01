using Server.Data;
using SadConsole.Entities;

using Point = SadRogue.Primitives.Point;
using Color = SadRogue.Primitives.Color;

using static SadConsole.PointExtensions;

namespace Client
{
    public class MapActor : Entity
    {
        // TODO!: this really needs to be replaced by a handle or ID; direct access
        //        to server data structures is VERY dangerous in async!
        public Actor Actor { get; }
        public SadConsole.Console ScrollingParent { get; }

        public MapActor(SadConsole.Console parent, Actor actor) :
            base(Color.White, Color.Transparent, actor.aiType == nameof(Server.Logic.AIType.PlayerControlled) ? 707 : 125)
        {
            parent.Children.Add(this);
            this.Parent = parent;
            ScrollingParent = parent;

            this.Actor = actor;
            Animation.Font = parent.Font;
            Animation.FontSize = parent.FontSize;

            Position = new Point(Actor.position.x, Actor.position.y)
                .SurfaceLocationToPixel(ScrollingParent.FontSize.X, ScrollingParent.FontSize.Y);
            
            Animation.UsePixelPositioning = true;
        }
    }
}