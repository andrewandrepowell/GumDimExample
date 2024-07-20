using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumDimExample;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using System;
using System.Diagnostics;
using System.Linq;
using ToolsUtilities;

namespace GumDimExample
{
    public class DimmerRuntime : GraphicalUiElement
    {
        private const float _waitTotalSecs = 2;
        private float _waitSecs;
        private static Color Lerp(Color low, Color high, float ratio)
        {
            var a = MathHelper.Lerp(low.A, high.A, ratio);
            var r = MathHelper.Lerp(low.R, high.R, ratio);
            var g = MathHelper.Lerp(low.G, high.G, ratio);
            var b = MathHelper.Lerp(low.B, high.B, ratio);
            return new Color(r: (int)Math.Round(r), g: (int)Math.Round(g), b: (int)Math.Round(b), alpha: (int)Math.Round(a));
        }
        private void SetColor(Color color)
        {
            SetProperty("RenderAlpha", (int)color.A);
            SetProperty("RenderRed", (int)color.R);
            SetProperty("RenderGreen", (int)color.G);
            SetProperty("RenderBlue", (int)color.B);
        }
        public enum States { Hidden, Shown, Hiding, Showing }
        public States State { get; private set; } = States.Shown;
        public Color DimColor = Color.Black;
        public void Hide()
        {
            Debug.Assert(State == States.Shown);
            _waitSecs = _waitTotalSecs;
            State = States.Hiding;
        }
        public void Show()
        {
            Debug.Assert(State == States.Hidden);
            _waitSecs = _waitTotalSecs;
            State = States.Showing;
        }
        public void Update(GameTime gameTime)
        {
            if (_waitSecs > 0)
            {
                _waitSecs -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_waitSecs > 0)
                {
                    if (State == States.Hiding)
                        SetColor(Lerp(Color.Transparent, DimColor, _waitSecs / _waitTotalSecs));
                    else if (State == States.Showing)
                        SetColor(Lerp(DimColor, Color.Transparent, _waitSecs / _waitTotalSecs));
                }
                else
                    if (State == States.Hiding)
                {
                    SetColor(Color.Transparent);
                    State = States.Hidden;
                }
                else if (State == States.Showing)
                {
                    SetColor(DimColor);
                    State = States.Shown;
                }
            }
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private const int _screenWidth = 800;
        private const int _screenHeight = 450;
        private DimmerRuntime _dimmerRuntime;
        private Texture2D _texture;
        private RenderTarget2D _screenTarget;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = _screenWidth;
            _graphics.PreferredBackBufferHeight = _screenHeight;
            _graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize(GraphicsDevice, fullInstantiation: true);
            var gumProject = GumProjectSave.Load($"gum/gum_proj.gumx");
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            FileManager.RelativeDirectory = "Content/gum/";
            ElementSaveExtensions.RegisterGueInstantiationType("dimmer", typeof(DimmerRuntime));
            var gumScreen = gumProject.Screens
                    .Where(x => x.Name == "screen_0").First()
                    .ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
            _dimmerRuntime = (DimmerRuntime)gumScreen.GetGraphicalUiElementByName("window_0.dimmer_0");

            _texture = Content.Load<Texture2D>("texture_0");
            _screenTarget = new RenderTarget2D(
                GraphicsDevice,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
        }

        protected override void Initialize()
        {            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);            
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
            _dimmerRuntime.Update(gameTime);

            if (_dimmerRuntime.State == DimmerRuntime.States.Hidden)
                _dimmerRuntime.Show();
            else if (_dimmerRuntime.State == DimmerRuntime.States.Shown)
                _dimmerRuntime.Hide();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_screenTarget);
            GraphicsDevice.Clear(Color.Yellow);
            _spriteBatch.Begin();
            _spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
            _spriteBatch.End();
            SystemManagers.Default.Draw();

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.CornflowerBlue); // THIS ISSUE IS RIGHT HERE. BY REMOVING THIS LINE, ISSUE IS FIXED.
            _spriteBatch.Begin();
            _spriteBatch.Draw(_screenTarget, Vector2.Zero, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
