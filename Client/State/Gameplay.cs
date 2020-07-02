using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Server;
using Server.Data;
using Server.Logic;
using Server.Message;

using C = System.Console;

namespace Client.State
{
    public class Gameplay : IState<StateManager>
    {
        public Consoles.Gameplay GameplayConsole { get; }
        private bool returnToTitle = false;

        private int Width { get; }
        private int Height { get; }

        public Gameplay(int w, int h, GameServer s)
        {
            GameplayConsole = new Consoles.Gameplay(w, h, s);
            Width = w;
            Height = h;
        }

        public IState<StateManager>? OnEnter(StateManager obj)
        {
            obj.Children.Add(GameplayConsole);
            GameplayConsole.IsFocused = true;
            return null;
        }

        public IState<StateManager>? Exec(StateManager obj)
        {
            if (GameplayConsole.ShouldReturnToTitle)
                return new State.TitleScreen(Width, Height);
            return null;
        }

        public IState<StateManager>? OnExit(StateManager obj)
        {
            obj.Children.Remove(GameplayConsole);
            return null;
        }
    }
}