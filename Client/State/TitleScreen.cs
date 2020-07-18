using System;
using System.Collections.Generic;
using System.IO;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Util;
using Util.Functional;

namespace Client.State
{
    public class TitleScreen : SadConsole.UI.ControlsConsole, IState<StateManager>, IResizeHandler
    {
        public SadConsole.UI.Controls.Button btnNewGame = new SadConsole.UI.Controls.Button(20);
        public SadConsole.UI.Controls.Button btnLoadGame = new SadConsole.UI.Controls.Button(20);
        public SadConsole.UI.Controls.Button btnExit = new SadConsole.UI.Controls.Button(20);

        public SadConsole.UI.Controls.Label lblName = new SadConsole.UI.Controls.Label("MYSTOWER");

        enum Transition
        {
            None,
            NewGame,
            LoadGame,
        }

        Transition nextScreen = Transition.None;

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

            btnNewGame.Click += (a, b) => nextScreen = Transition.NewGame;
            btnLoadGame.Click += (a, b) => nextScreen = Transition.LoadGame;
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

        private Result<Util.Database> LoadDefaultServerDatabases()
        {
            Util.Database db = new Database();
            
            var dict = ArchetypeJson.Read<Server.Database.ActorArchetype>(
                File.ReadAllText("Resources/Data/Server/ActorArchetype.json"));
            if (dict.IsSuccess)
                db.AddDatabase(dict.Value);
            else
                return Result.Error(dict.Err);

            return Result.Ok(db);
        }

        private IState<StateManager>? CreateGameplayState(Func<Util.Database, Result<Server.GameServer>> serverFactory)
        {
            nextScreen = Transition.None;

            return LoadDefaultServerDatabases()
                    .Bind(serverFactory)
                    .Match<Gameplay?>(
                        ok: server => new Gameplay(server),
                        err: error => {
                            System.Console.WriteLine($"Error creating server: {error.Message}");
                            return null;
                        }
                    );
        }

        public IState<StateManager>? Exec(StateManager obj)
            => nextScreen switch
            {
                Transition.NewGame => CreateGameplayState(db => Server.GameServer.NewGame(db)),
                Transition.LoadGame => CreateGameplayState(db => Server.GameServer.FromSaveGame(File.ReadAllText("Saves/save.sav"), db)),
                _ => null
            };

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