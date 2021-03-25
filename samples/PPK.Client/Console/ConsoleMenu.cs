using System;
using System.Collections.Generic;
using System.Linq;

namespace PPK.Client
{
    public class ConsoleMenu
    {
        private int _selectedItemIndex = 0;
        private bool _itemIsSelected = false;
        private bool _menuIsDrawn = false;
        public event EventHandler MenuItemSelected;

        public ConsoleMenu(string description, IEnumerable<string> menuItems) {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MenuItems = menuItems ?? throw new ArgumentNullException(nameof(menuItems));
        }

        public string Description { get; }
        public IEnumerable<string> MenuItems { get; }

        public void Draw() {
            if (_menuIsDrawn) {
                return;
            }
            _menuIsDrawn = true;
            StartDrawLoop();
        }

        protected virtual void OnMenuItemSelected(MenuItemEventArgs eventArgs) {
            var handler = MenuItemSelected;
            handler?.Invoke(this, eventArgs);
        }

        private void StartDrawLoop() {
            var cursorBottom = 0;
            Console.CursorVisible = false;
            ConsoleKeyInfo pressedKey;
            // Get cursor position before drawing menu.
            var cursorTop = Console.CursorTop;
            while (!_itemIsSelected) {
                DrawInternal();
                // Get cursor position after drawing menu.
                cursorBottom = Console.CursorTop;
                pressedKey = Console.ReadKey(intercept: true);
                HandleKeyPress(pressedKey.Key);
                // Set cursor position to initial position to re-draw.
                Console.SetCursorPosition(0, cursorTop);
            }
            // Set cursor position to bottom after selection.
            Console.SetCursorPosition(0, cursorBottom);
            Console.CursorVisible = true;
            OnMenuItemSelected(new MenuItemEventArgs { SelectedItemIndex = _selectedItemIndex });
        }

        private void DrawInternal() {
            Console.WriteLine(Description);
            for (var i = 0; i < MenuItems.Count(); i++) {
                var menuItem = MenuItems.ElementAt(i);
                if (_selectedItemIndex == i) {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                Console.WriteLine(menuItem);
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        private void HandleKeyPress(ConsoleKey pressedKey) {
            switch (pressedKey) {
                case ConsoleKey.UpArrow:
                    _selectedItemIndex = (_selectedItemIndex == 0) ? MenuItems.Count() - 1 : _selectedItemIndex - 1;
                    break;
                case ConsoleKey.DownArrow:
                    _selectedItemIndex = (_selectedItemIndex == MenuItems.Count() - 1) ? 0 : _selectedItemIndex + 1;
                    break;
                case ConsoleKey.Enter:
                    _itemIsSelected = true;
                    break;
            }
        }
    }
}
