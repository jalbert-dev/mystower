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

        private enum Transition
        {
            None,
            NewGame,
            LoadGame,
        }

        private Transition nextScreen = Transition.None;
        private readonly List<SadConsole.UI.Controls.Button> focusOrder;
        private int focused = 0;
        public TitleScreen() : base(1, 1) 
        {
            btnNewGame.Text = "New Game";
            btnLoadGame.Text = "Load Game";
            btnExit.Text = "Exit";

            lblName.Alignment = HorizontalAlignment.Center;
            
            ControlHostComponent.Add(btnNewGame);
            ControlHostComponent.Add(btnLoadGame);
            ControlHostComponent.Add(btnExit);
            ControlHostComponent.Add(lblName);

            btnNewGame.Click += (a, b) => nextScreen = Transition.NewGame;
            btnLoadGame.Click += (a, b) => nextScreen = Transition.LoadGame;
            btnExit.Click += (a, b) => Environment.Exit(0);

            focusOrder = new List<SadConsole.UI.Controls.Button>()
            {
                btnNewGame,
                btnLoadGame,
                btnExit,
            };
            TriggerFocus();

            OnWindowResize(Settings.Rendering.RenderWidth, Settings.Rendering.RenderHeight);
        }

        public void OnWindowResize(int width, int height)
        {
            var (w, h) = (width / FontSize.X, height / FontSize.Y);
            Resize(w, h, w, h, false);

            btnNewGame.Position = new Point(w / 2 - btnNewGame.Width / 2, h / 2);
            btnLoadGame.PlaceRelativeTo(btnNewGame, Direction.Types.Down, 2);
            btnExit.PlaceRelativeTo(btnLoadGame, Direction.Types.Down, 2);
        }

        private void TriggerFocus()
        {
            if (focused < 0)
                focused += focusOrder.Count;
            focused %= focusOrder.Count;
            for (int i = 0; i < focusOrder.Count; i++)
                focusOrder[i].IsFocused = i == focused;
        }

        public override bool ProcessKeyboard(Keyboard keyboard)
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

        private static Result<Util.Database> LoadDefaultServerDatabase(IFileSystem fs)
        {
            Util.Database db = new Util.Database();

            Result<Unit> read_table<T>(string path) => 
                fs.ReadAllText(path)
                    .Bind(ArchetypeJson.Read<T>)
                    .Finally(table => db.AddDatabase(table));

            return
                read_table<Server.Database.ActorArchetype>("Resources/Data/Server/ActorArchetype.json")
                .Map(_ => db);
        }

        private IState<StateManager>? CreateGameplayState(Func<Util.Database, Result<Server.GameServer>> serverFactory)
        {
            IFileSystem fs = new LooseFileSystem();

            nextScreen = Transition.None;

            var result = 
                from server in 
                    LoadDefaultServerDatabase(fs).Bind(serverFactory)
                from clientContext in
                    ClientContext.Construct(fs)
                select
                    new Gameplay(clientContext, server);

            return result.Match<Gameplay?>(
                ok: server => server,
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