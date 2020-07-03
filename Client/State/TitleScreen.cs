using System.Collections.Generic;
using System.IO;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Util;

namespace Client.State
{
    public class TitleScreen : SadConsole.UI.ControlsConsole, IState<StateManager>, IResizeHandler
    {
        public SadConsole.UI.Controls.Button btnNewGame = new SadConsole.UI.Controls.Button(20);
        public SadConsole.UI.Controls.Button btnLoadGame = new SadConsole.UI.Controls.Button(20);
        public SadConsole.UI.Controls.Button btnExit = new SadConsole.UI.Controls.Button(20);

        public SadConsole.UI.Controls.Label lblName = new SadConsole.UI.Controls.Label("MYSTOWER");

        bool startNewGame = false;
        bool loadGame = false;

        List<SadConsole.UI.Controls.Button> focusOrder;
        int focused = 0;
        public TitleScreen() : base(1, 1) 
        {
            btnNewGame.Text = "New Game";
            btnLoadGame.Text = "Load Game";
            btnExit.Text = "Exit";

            lblName.Alignment = SadConsole.HorizontalAlignment.Center;
            
            ControlHostComponent.Add(btnNewGame);
            ControlHostComponent.Add(btnLoadGame);
            ControlHostComponent.Add(btnExit);
            ControlHostComponent.Add(lblName);

            btnNewGame.Click += (a, b) => startNewGame = true;
            btnLoadGame.Click += (a, b) => loadGame = true;
            btnExit.Click += (a, b) => System.Environment.Exit(0);

            focusOrder = new List<SadConsole.UI.Controls.Button>()
            {
                btnNewGame,
                btnLoadGame,
                btnExit,
            };
            TriggerFocus();

            OnWindowResize(SadConsole.Settings.Rendering.RenderWidth, SadConsole.Settings.Rendering.RenderHeight);
        }

        public void OnWindowResize(int width, int height)
        {
            var (w, h) = (width / FontSize.X, height / FontSize.Y);
            Resize(w, h, w, h, false);

            btnNewGame.Position = new SadRogue.Primitives.Point(w / 2 - btnNewGame.Width / 2, h / 2);
            btnLoadGame.PlaceRelativeTo(btnNewGame, Direction.Types.Down, 2);
            btnExit.PlaceRelativeTo(btnLoadGame, Direction.Types.Down, 2);
        }

        private void TriggerFocus()
        {
            if (focused < 0)
                focused += focusOrder.Count;
            focused = focused % focusOrder.Count;
            for (int i = 0; i < focusOrder.Count; i++)
                focusOrder[i].IsFocused = i == focused;
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.Down))
            {
                focused++;
                TriggerFocus();
                return true;
            }
            if (keyboard.IsKeyPressed(Keys.Up))
            {
                focused--;
                TriggerFocus();
                return true;
            }
            if (keyboard.IsKeyPressed(Keys.Enter))
            {
                for (int i = 0; i < focusOrder.Count; i++)
                {
                    if (focusOrder[i].IsFocused)
                    {
                        focusOrder[i].DoClick();
                        return true;
                    }
                }
            }
            return false;
        }

        public IState<StateManager>? Exec(StateManager obj)
        {
            if (startNewGame)
                return new State.Gameplay(Width, Height, Server.GameServer.NewGame());
            if (loadGame)
                return new State.Gameplay(Width, Height, Server.GameServer.FromSaveGame(File.ReadAllText("Saves/save.sav")));
            return null;
        }

        public IState<StateManager>? OnEnter(StateManager obj)
        {
            obj.Children.Add(this);
            IsFocused = true;
            return null;
        }

        public IState<StateManager>? OnExit(StateManager obj)
        {
            obj.Children.Remove(this);
            return null;
        }
    }
}