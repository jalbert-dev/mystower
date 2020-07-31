using System;

namespace Server.Data
{
    public enum Direction
    {
        N,
        S,
        E,
        W,
        NE,
        NW,
        SE,
        SW,
    }

    public static class DirectionExtensions
    {
        public static Vec2i ToVec(this Direction self) => self switch
        {
            Direction.N => (0, -1),
            Direction.S => (0, 1),
            Direction.E => (1, 0),
            Direction.W => (-1, 0),
            Direction.NE => (1, -1),
            Direction.NW => (-1, -1),
            Direction.SE => (1, 1),
            Direction.SW => (-1, 1),
            _ => throw new Exception("ToVec called on invalid Direction!"),
        };

        private static Direction FindClosestDirection(int x, int y)
        {
            // TODO: This would be a good fit for C# 9.0's relational patterns in a switch expression
            var angle = Math.Atan2(y, x) * (180 / Math.PI);
            // we want to map atan2's range to [0, 360] instead of [-180, 180]
            angle = angle < 0.0 ? angle + 360.0 : angle;
            if (angle >= 330 || angle <= 30) return Direction.E;
            else if (angle <= 60) return Direction.NE;
            else if (angle <= 120) return Direction.N;
            else if (angle <= 150) return Direction.NW;
            else if (angle <= 210) return Direction.W;
            else if (angle <= 240) return Direction.SW;
            else if (angle <= 300) return Direction.S;
            else if (angle <= 330) return Direction.SE;
            else throw new Exception($"Unexpected angle {angle}! (expected value in range [0, 360])");
        }

        public static Direction ToClosestDirection(this (int, int) self) => self switch
        {
            (0, -1) => Direction.N,
            (0, 1) => Direction.S,
            (1, 0) => Direction.E,
            (-1, 0) => Direction.W,
            (1, -1) => Direction.NE,
            (-1, -1) => Direction.NW,
            (1, 1) => Direction.SE,
            (-1, 1) => Direction.SW,

            _ => FindClosestDirection(self.Item1, self.Item2)
        };

        public static Direction ToClosestDirection(this Vec2i self) => (self.x, self.y).ToClosestDirection();
    }
}