using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Tetris
{
    
    public class Game1 : Game
    {
        // ---------- konstanty ----------
        const int Cols = 10;
        const int Rows = 20;
        const int Cell = 30;
        const int BoardX = 30;
        const int BoardY = 30;
        const int PanelX = BoardX + Cols * Cell + 30;

        // ---------- tvary (1 = čtverec) ----------
        static readonly string[][] ShapeDefs =
        {
            new[] { "0000", "1111", "0000", "0000" }, // I
            new[] { "11", "11" },                     // O
            new[] { "010", "111", "000" },            // T
            new[] { "011", "110", "000" },            // S
            new[] { "110", "011", "000" },            // Z
            new[] { "100", "111", "000" },            // J
            new[] { "001", "111", "000" },            // L
        };

        static readonly Color[] PieceColors =
        {
            new Color(0, 220, 230),   // I
            new Color(240, 220, 0),   // O
            new Color(170, 60, 220),  // T
            new Color(60, 210, 70),   // S
            new Color(230, 50, 50),   // Z
            new Color(50, 90, 230),   // J
            new Color(240, 140, 0),   // L
        };

        // ---------- font z čtverečků (3x5) ----------
        static readonly Dictionary<char, string> Font = new Dictionary<char, string>
        {
            {'0',"111101101101111"},{'1',"010110010010111"},{'2',"111001111100111"},
            {'3',"111001111001111"},{'4',"101101111001001"},{'5',"111100111001111"},
            {'6',"111100111101111"},{'7',"111001001001001"},{'8',"111101111101111"},
            {'9',"111101111001111"},
            {'A',"010101111101101"},{'C',"111100100100111"},{'D',"110101101101110"},
            {'E',"111100111100111"},{'G',"111100101101111"},{'I',"111010010010111"},
            {'L',"100100100100111"},{'M',"101111111101101"},{'N',"110101101101101"},
            {'O',"111101101101111"},{'P',"111101111100100"},{'R',"110101110101101"},
            {'S',"111100111001111"},{'T',"111010010010010"},{'U',"101101101101111"},
            {'V',"101101101101010"},{'X',"101101010101101"},
        };

        // ---------- stav hry ----------
        readonly GraphicsDeviceManager _graphics;
        SpriteBatch _sb;
        Texture2D _pixel;

        int[,] _board = new int[Rows, Cols]; // 0 = prázdno, 1..7 = barva
        int[,] _shape;
        int _type, _px, _py;
        int _nextType;
        readonly Queue<int> _bag = new Queue<int>();
        readonly Random _rng = new Random();

        int _score, _lines, _level;
        bool _gameOver, _paused;

        double _fallTimer;
        double _moveTimer;
        int _moveDir;

        KeyboardState _kb, _prevKb;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = PanelX + 200,
                PreferredBackBufferHeight = BoardY * 2 + Rows * Cell,
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Tetris (jen čtverce a obdélníky)";
        }

        protected override void Initialize()
        {
            base.Initialize();
            NewGame();
        }

        protected override void LoadContent()
        {
            _sb = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        // ---------- logika ----------
        void NewGame()
        {
            _board = new int[Rows, Cols];
            _score = _lines = _level = 0;
            _gameOver = _paused = false;
            _bag.Clear();
            _nextType = DrawFromBag();
            SpawnPiece();
        }

        int DrawFromBag()
        {
            if (_bag.Count == 0)
            {
                var list = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                foreach (var t in list) _bag.Enqueue(t);
            }
            return _bag.Dequeue();
        }

        static int[,] BuildShape(int type)
        {
            var def = ShapeDefs[type];
            int n = def.Length;
            var m = new int[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    m[y, x] = def[y][x] == '1' ? 1 : 0;
            return m;
        }

        static int[,] Rotate(int[,] m, bool clockwise)
        {
            int n = m.GetLength(0);
            var r = new int[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (clockwise) r[x, n - 1 - y] = m[y, x];
                    else r[n - 1 - x, y] = m[y, x];
            return r;
        }

        void SpawnPiece()
        {
            _type = _nextType;
            _nextType = DrawFromBag();
            _shape = BuildShape(_type);
            _px = (Cols - _shape.GetLength(0)) / 2;
            _py = 0;
            _fallTimer = 0;
            if (Collides(_shape, _px, _py))
                _gameOver = true;
        }

        bool Collides(int[,] shape, int px, int py)
        {
            int n = shape.GetLength(0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    if (shape[y, x] == 0) continue;
                    int bx = px + x, by = py + y;
                    if (bx < 0 || bx >= Cols || by >= Rows) return true;
                    if (by >= 0 && _board[by, bx] != 0) return true;
                }
            return false;
        }

        bool TryMove(int dx, int dy)
        {
            if (Collides(_shape, _px + dx, _py + dy)) return false;
            _px += dx; _py += dy;
            return true;
        }

        void TryRotate(bool clockwise)
        {
            var r = Rotate(_shape, clockwise);
            foreach (int kick in new[] { 0, -1, 1, -2, 2 })
            {
                if (!Collides(r, _px + kick, _py))
                {
                    _shape = r;
                    _px += kick;
                    return;
                }
            }
        }

        void LockPiece()
        {
            int n = _shape.GetLength(0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (_shape[y, x] != 0 && _py + y >= 0)
                        _board[_py + y, _px + x] = _type + 1;

            ClearLines();
            SpawnPiece();
        }

        void ClearLines()
        {
            int cleared = 0;
            for (int y = Rows - 1; y >= 0; y--)
            {
                bool full = true;
                for (int x = 0; x < Cols; x++)
                    if (_board[y, x] == 0) { full = false; break; }
                if (!full) continue;

                for (int yy = y; yy > 0; yy--)
                    for (int x = 0; x < Cols; x++)
                        _board[yy, x] = _board[yy - 1, x];
                for (int x = 0; x < Cols; x++) _board[0, x] = 0;

                cleared++;
                y++; 
            }

            if (cleared > 0)
            {
                int[] points = { 0, 100, 300, 500, 800 };
                _score += points[cleared] * (_level + 1);
                _lines += cleared;
                _level = _lines / 10;
            }
        }

        int GhostY()
        {
            int gy = _py;
            while (!Collides(_shape, _px, gy + 1)) gy++;
            return gy;
        }

        bool Pressed(Keys k) => _kb.IsKeyDown(k) && _prevKb.IsKeyUp(k);

        protected override void Update(GameTime gameTime)
        {
            _prevKb = _kb;
            _kb = Keyboard.GetState();
            double dt = gameTime.ElapsedGameTime.TotalSeconds;

            if (Pressed(Keys.Escape)) Exit();
            if (Pressed(Keys.Enter) && _gameOver) NewGame();
            if (Pressed(Keys.P) && !_gameOver) _paused = !_paused;

            if (_gameOver || _paused)
            {
                base.Update(gameTime);
                return;
            }

            // vodorovný pohyb
            int dir = 0;
            if (_kb.IsKeyDown(Keys.Left)) dir = -1;
            else if (_kb.IsKeyDown(Keys.Right)) dir = 1;

            if (dir == 0) _moveDir = 0;
            else if (dir != _moveDir)
            {
                _moveDir = dir;
                TryMove(dir, 0);
                _moveTimer = 0.17;
            }
            else
            {
                _moveTimer -= dt;
                if (_moveTimer <= 0)
                {
                    TryMove(dir, 0);
                    _moveTimer = 0.05;
                }
            }

            // rotace
            if (Pressed(Keys.Up) || Pressed(Keys.X)) TryRotate(true);
            if (Pressed(Keys.Z)) TryRotate(false);

            // hard drop
            if (Pressed(Keys.Space))
            {
                int gy = GhostY();
                _score += (gy - _py) * 2;
                _py = gy;
                LockPiece();
                base.Update(gameTime);
                return;
            }

            // gravitace
            double interval = Math.Max(0.05, 0.8 - 0.07 * _level);
            if (_kb.IsKeyDown(Keys.Down)) interval = Math.Min(interval, 0.04);

            _fallTimer += dt;
            while (_fallTimer >= interval && !_gameOver)
            {
                _fallTimer -= interval;
                if (TryMove(0, 1))
                {
                    if (_kb.IsKeyDown(Keys.Down)) _score += 1;
                }
                else
                {
                    LockPiece();
                    break;
                }
            }

            base.Update(gameTime);
        }

        // ---------- kreslení (pouze obdélníky) ----------
        void Rect(int x, int y, int w, int h, Color c)
        {
            _sb.Draw(_pixel, new Rectangle(x, y, w, h), c);
        }

        void Block(int x, int y, int size, Color c)
        {
            Rect(x, y, size, size, Color.Lerp(c, Color.Black, 0.55f)); // okraj
            Rect(x + 2, y + 2, size - 4, size - 4, c);                 // výplň
            Rect(x + 2, y + 2, size - 4, 4, Color.Lerp(c, Color.White, 0.45f)); // odlesk
        }

        void Outline(int x, int y, int size, Color c)
        {
            Rect(x, y, size, 2, c);
            Rect(x, y + size - 2, size, 2, c);
            Rect(x, y, 2, size, c);
            Rect(x + size - 2, y, 2, size, c);
        }

        void Text(string s, int x, int y, int scale, Color c)
        {
            foreach (char ch in s.ToUpperInvariant())
            {
                if (Font.TryGetValue(ch, out var g))
                {
                    for (int i = 0; i < 15; i++)
                        if (g[i] == '1')
                            Rect(x + (i % 3) * scale, y + (i / 3) * scale, scale, scale, c);
                }
                x += 4 * scale;
            }
        }

        int TextWidth(string s, int scale) => s.Length * 4 * scale - scale;

        void DrawShapeAt(int[,] shape, int type, int ox, int oy, int size)
        {
            int n = shape.GetLength(0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (shape[y, x] != 0)
                        Block(ox + x * size, oy + y * size, size, PieceColors[type]);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(18, 18, 26));
            _sb.Begin(samplerState: SamplerState.PointClamp);

            // pozadí hrací plochy + rámeček
            Rect(BoardX - 6, BoardY - 6, Cols * Cell + 12, Rows * Cell + 12, new Color(70, 70, 90));
            Rect(BoardX, BoardY, Cols * Cell, Rows * Cell, new Color(8, 8, 14));

            // mřížka z tenkých obdélníků
            var grid = new Color(24, 24, 36);
            for (int x = 1; x < Cols; x++) Rect(BoardX + x * Cell, BoardY, 1, Rows * Cell, grid);
            for (int y = 1; y < Rows; y++) Rect(BoardX, BoardY + y * Cell, Cols * Cell, 1, grid);

            // zafixované bloky
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                    if (_board[y, x] != 0)
                        Block(BoardX + x * Cell, BoardY + y * Cell, Cell, PieceColors[_board[y, x] - 1]);

            if (!_gameOver)
            {
                int n = _shape.GetLength(0);
                int gy = GhostY();
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        if (_shape[y, x] == 0) continue;

                        // duch (kde padající tvar dopadne)
                        if (gy + y >= 0)
                            Outline(BoardX + (_px + x) * Cell, BoardY + (gy + y) * Cell, Cell,
                                    Color.Lerp(PieceColors[_type], Color.Black, 0.25f));

                        // aktuální tvar
                        if (_py + y >= 0)
                            Block(BoardX + (_px + x) * Cell, BoardY + (_py + y) * Cell, Cell, PieceColors[_type]);
                    }
            }

            // ---------- panel ----------
            var label = new Color(150, 150, 180);
            var value = Color.White;

            Text("NEXT", PanelX, BoardY, 4, label);
            Rect(PanelX, BoardY + 30, 130, 110, new Color(8, 8, 14));
            {
                var ns = BuildShape(_nextType);
                int n = ns.GetLength(0);
                int size = 24;
                int w = n * size;
                // vystředění podle skutečného obsazení
                int minX = n, maxX = 0, minY = n, maxY = 0;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        if (ns[y, x] != 0)
                        {
                            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                        }
                int cw = (maxX - minX + 1) * size;
                int ch = (maxY - minY + 1) * size;
                int ox = PanelX + (130 - cw) / 2 - minX * size;
                int oy = BoardY + 30 + (110 - ch) / 2 - minY * size;
                DrawShapeAt(ns, _nextType, ox, oy, size);
            }

            Text("SCORE", PanelX, BoardY + 170, 3, label);
            Text(_score.ToString(), PanelX, BoardY + 195, 4, value);

            Text("LEVEL", PanelX, BoardY + 250, 3, label);
            Text(_level.ToString(), PanelX, BoardY + 275, 4, value);

            Text("LINES", PanelX, BoardY + 330, 3, label);
            Text(_lines.ToString(), PanelX, BoardY + 355, 4, value);

            // ---------- překryvné zprávy ----------
            if (_gameOver)
            {
                Overlay("GAME OVER", "PRESS ENTER", new Color(230, 60, 60));
            }
            else if (_paused)
            {
                Overlay("PAUSED", "P TO RESUME", new Color(240, 220, 0));
            }

            _sb.End();
            base.Draw(gameTime);
        }

        void Overlay(string title, string sub, Color accent)
        {
            int bw = Cols * Cell;
            Rect(BoardX, BoardY + Rows * Cell / 2 - 60, bw, 120, new Color(0, 0, 0, 220));
            Rect(BoardX, BoardY + Rows * Cell / 2 - 60, bw, 4, accent);
            Rect(BoardX, BoardY + Rows * Cell / 2 + 56, bw, 4, accent);

            int ts = 5;
            Text(title, BoardX + (bw - TextWidth(title, ts)) / 2, BoardY + Rows * Cell / 2 - 30, ts, accent);
            int ss = 3;
            Text(sub, BoardX + (bw - TextWidth(sub, ss)) / 2, BoardY + Rows * Cell / 2 + 20, ss, Color.White);
        }
    }
}
