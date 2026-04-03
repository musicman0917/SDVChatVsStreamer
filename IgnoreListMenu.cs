using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SDVChatVsStreamer.UI;

public class IgnoreListMenu : IClickableMenu
{
    private readonly ModConfig _config;
    private readonly IModHelper _helper;

    // User list state
    private List<string> _users = new();
    private int _scrollOffset = 0;
    private const int RowHeight     = 64;
    private const int MaxVisibleRows = 7;

    // Add field state
    private string _addText    = "";
    private bool _addFocused   = false;
    private double _caretTimer = 0;
    private bool _caretVisible = true;

    // Layout constants
    private Rectangle _titleBounds;
    private Rectangle _listBounds;
    private Rectangle _addFieldBounds;
    private Rectangle _addButtonBounds;
    private Rectangle _doneButtonBounds;
    private Rectangle _scrollUpBounds;
    private Rectangle _scrollDownBounds;

    // Remove button hit areas — rebuilt each draw
    private readonly List<Rectangle> _removeButtons = new();

    public IgnoreListMenu(ModConfig config, IModHelper helper)
        : base(
            x: (Game1.uiViewport.Width  - 600) / 2,
            y: (Game1.uiViewport.Height - 640) / 2,
            width:  600,
            height: 640,
            showUpperRightCloseButton: true)
    {
        _config = config;
        _helper = helper;
        LoadUsers();
        LayoutRects();
    }

    private void LoadUsers()
    {
        _users = _config.IgnoredUsers
            .Split(',')
            .Select(u => u.Trim())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();
    }

    private void SaveUsers()
    {
        _config.IgnoredUsers = string.Join(", ", _users);
        _helper.WriteConfig(_config);
    }

    private void LayoutRects()
    {
        int px = xPositionOnScreen;
        int py = yPositionOnScreen;

        _titleBounds    = new Rectangle(px + 32, py + 24, width - 64, 48);
        _listBounds     = new Rectangle(px + 32, py + 88, width - 80, MaxVisibleRows * RowHeight);
        _scrollUpBounds = new Rectangle(px + width - 48, py + 88, 40, 40);
        _scrollDownBounds = new Rectangle(px + width - 48, py + 88 + MaxVisibleRows * RowHeight - 40, 40, 40);

        int addY = py + 88 + MaxVisibleRows * RowHeight + 20;
        _addFieldBounds  = new Rectangle(px + 32, addY, width - 160, 56);
        _addButtonBounds = new Rectangle(px + width - 120, addY, 88, 56);

        _doneButtonBounds = new Rectangle(px + (width - 160) / 2, addY + 72, 160, 56);
    }

    // ─── Draw ─────────────────────────────────────────────────────────────────

    public override void draw(SpriteBatch b)
    {
        // Dim background
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds,
            Color.Black * 0.5f);

        // Window box
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        // Title
        string title = "Ignored Users";
        var titleSize = Game1.dialogueFont.MeasureString(title);
        b.DrawString(Game1.dialogueFont, title,
            new Vector2(_titleBounds.X + (_titleBounds.Width - titleSize.X) / 2f, _titleBounds.Y),
            Game1.textColor);

        // Divider
        b.Draw(Game1.staminaRect,
            new Rectangle(_titleBounds.X, _titleBounds.Bottom + 8, _titleBounds.Width, 2),
            Color.Gray * 0.5f);

        // User list
        _removeButtons.Clear();
        int visibleCount = Math.Min(MaxVisibleRows, _users.Count - _scrollOffset);

        for (int i = 0; i < visibleCount; i++)
        {
            int idx  = i + _scrollOffset;
            var user = _users[idx];
            int rowY = _listBounds.Y + i * RowHeight;

            // Row background (alternating)
            if (i % 2 == 0)
                b.Draw(Game1.staminaRect,
                    new Rectangle(_listBounds.X, rowY, _listBounds.Width, RowHeight - 4),
                    Color.Black * 0.05f);

            // Username
            b.DrawString(Game1.smallFont, user,
                new Vector2(_listBounds.X + 8, rowY + (RowHeight - Game1.smallFont.MeasureString(user).Y) / 2f),
                Game1.textColor);

            // Remove (X) button
            var removeRect = new Rectangle(_listBounds.Right - 48, rowY + 12, 40, 40);
            _removeButtons.Add(removeRect);

            bool hovered = removeRect.Contains(Game1.getMousePosition());
            drawTextureBox(b, removeRect.X - 4, removeRect.Y - 4, 48, 48,
                hovered ? Color.Red * 0.8f : Color.Red * 0.5f);
            b.DrawString(Game1.smallFont, "X",
                new Vector2(removeRect.X + 12, removeRect.Y + 8),
                Color.White);
        }

        // Empty state
        if (_users.Count == 0)
        {
            string empty = "No ignored users. Add one below.";
            b.DrawString(Game1.smallFont, empty,
                new Vector2(
                    _listBounds.X + (_listBounds.Width - Game1.smallFont.MeasureString(empty).X) / 2f,
                    _listBounds.Y + _listBounds.Height / 2f),
                Game1.textColor * 0.5f);
        }

        // Scroll buttons
        if (_scrollOffset > 0)
        {
            bool hov = _scrollUpBounds.Contains(Game1.getMousePosition());
            drawTextureBox(b, _scrollUpBounds.X, _scrollUpBounds.Y, _scrollUpBounds.Width, _scrollUpBounds.Height,
                hov ? Color.LightGray : Color.White);
            b.DrawString(Game1.smallFont, "▲",
                new Vector2(_scrollUpBounds.X + 10, _scrollUpBounds.Y + 6), Game1.textColor);
        }

        if (_scrollOffset + MaxVisibleRows < _users.Count)
        {
            bool hov = _scrollDownBounds.Contains(Game1.getMousePosition());
            drawTextureBox(b, _scrollDownBounds.X, _scrollDownBounds.Y, _scrollDownBounds.Width, _scrollDownBounds.Height,
                hov ? Color.LightGray : Color.White);
            b.DrawString(Game1.smallFont, "▼",
                new Vector2(_scrollDownBounds.X + 10, _scrollDownBounds.Y + 6), Game1.textColor);
        }

        // Add field
        drawTextureBox(b, _addFieldBounds.X, _addFieldBounds.Y, _addFieldBounds.Width, _addFieldBounds.Height,
            _addFocused ? Color.LightYellow : Color.White);

        string displayText = _addText.Length > 0 ? _addText : "Add username...";
        Color textColor     = _addText.Length > 0 ? Game1.textColor : Game1.textColor * 0.4f;
        string caret        = (_addFocused && _caretVisible) ? "|" : "";
        b.DrawString(Game1.smallFont, displayText + caret,
            new Vector2(_addFieldBounds.X + 10, _addFieldBounds.Y + 14), textColor);

        // Add button
        bool addHov = _addButtonBounds.Contains(Game1.getMousePosition());
        drawTextureBox(b, _addButtonBounds.X, _addButtonBounds.Y, _addButtonBounds.Width, _addButtonBounds.Height,
            addHov ? Color.LightGreen : Color.White);
        b.DrawString(Game1.smallFont, "Add",
            new Vector2(_addButtonBounds.X + 16, _addButtonBounds.Y + 14), Game1.textColor);

        // Done button
        bool doneHov = _doneButtonBounds.Contains(Game1.getMousePosition());
        drawTextureBox(b, _doneButtonBounds.X, _doneButtonBounds.Y, _doneButtonBounds.Width, _doneButtonBounds.Height,
            doneHov ? Color.LightGreen : Color.White);
        b.DrawString(Game1.smallFont, "Save & Close",
            new Vector2(_doneButtonBounds.X + 12, _doneButtonBounds.Y + 14), Game1.textColor);

        // Close button
        upperRightCloseButton?.draw(b);

        drawMouse(b);
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // Close button
        if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
        {
            SaveUsers();
            exitThisMenu();
            return;
        }

        // Done button
        if (_doneButtonBounds.Contains(x, y))
        {
            SaveUsers();
            exitThisMenu();
            Game1.playSound("select");
            return;
        }

        // Remove buttons
        for (int i = 0; i < _removeButtons.Count; i++)
        {
            if (_removeButtons[i].Contains(x, y))
            {
                int idx = i + _scrollOffset;
                if (idx < _users.Count)
                {
                    _users.RemoveAt(idx);
                    if (_scrollOffset > 0 && _scrollOffset >= _users.Count)
                        _scrollOffset--;
                    Game1.playSound("trashcan");
                }
                return;
            }
        }

        // Add button
        if (_addButtonBounds.Contains(x, y))
        {
            AddCurrentUser();
            return;
        }

        // Add field focus
        _addFocused = _addFieldBounds.Contains(x, y);

        // Scroll buttons
        if (_scrollUpBounds.Contains(x, y) && _scrollOffset > 0)
        {
            _scrollOffset--;
            Game1.playSound("shiny4");
        }
        if (_scrollDownBounds.Contains(x, y) && _scrollOffset + MaxVisibleRows < _users.Count)
        {
            _scrollOffset++;
            Game1.playSound("shiny4");
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0 && _scrollOffset > 0)
            _scrollOffset--;
        else if (direction < 0 && _scrollOffset + MaxVisibleRows < _users.Count)
            _scrollOffset++;
    }

    public override void receiveKeyPress(Keys key)
    {
        if (_addFocused)
        {
            if (key == Keys.Enter)
            {
                AddCurrentUser();
                return;
            }
            if (key == Keys.Escape)
            {
                _addFocused = false;
                return;
            }
            if (key == Keys.Back && _addText.Length > 0)
            {
                _addText = _addText[..^1];
                return;
            }
            // Don't close menu on escape when typing
            return;
        }

        if (key == Keys.Escape)
        {
            SaveUsers();
            exitThisMenu();
        }
    }

    public override void receiveGamePadButton(Buttons b)
    {
        // Suppress gamepad B closing without saving
        if (b == Buttons.B)
        {
            SaveUsers();
            exitThisMenu();
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private KeyboardState _prevKeyState;

    public override void update(GameTime time)
    {
        base.update(time);

        // Caret blink
        _caretTimer += time.ElapsedGameTime.TotalSeconds;
        if (_caretTimer >= 0.5)
        {
            _caretVisible = !_caretVisible;
            _caretTimer   = 0;
        }

        // Text input when add field is focused
        if (_addFocused)
        {
            var ks   = Keyboard.GetState();
            var prev = _prevKeyState;

            foreach (var key in ks.GetPressedKeys())
            {
                if (!prev.IsKeyDown(key))
                    HandleTypedKey(key, ks);
            }

            _prevKeyState = ks;
        }
    }

    private void HandleTypedKey(Keys key, KeyboardState ks)
    {
        bool shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);

        if (key == Keys.Back)
        {
            if (_addText.Length > 0)
                _addText = _addText[..^1];
            return;
        }
        if (key == Keys.Enter) { AddCurrentUser(); return; }
        if (key == Keys.Escape) { _addFocused = false; return; }

        // Letters
        if (key >= Keys.A && key <= Keys.Z)
        {
            char c = (char)('a' + (key - Keys.A));
            if (_addText.Length < 32)
                _addText += shift ? char.ToUpper(c) : c;
            return;
        }
        // Digits
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (_addText.Length < 32)
                _addText += (char)('0' + (key - Keys.D0));
            return;
        }
        // Underscore
        if (key == Keys.OemMinus && shift && _addText.Length < 32)
            _addText += '_';
    }

    private void AddCurrentUser()
    {
        var name = _addText.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_users.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            Game1.addHUDMessage(new HUDMessage($"{name} is already in the list.", HUDMessage.error_type));
            return;
        }
        _users.Add(name);
        _addText    = "";
        _addFocused = false;
        Game1.playSound("coin");

        // Scroll to bottom to show newly added user
        if (_users.Count > MaxVisibleRows)
            _scrollOffset = _users.Count - MaxVisibleRows;
    }
}