# Creating Custom UI

This tutorial shows you how to build custom UI controls for menus, HUDs, and game interfaces using Lilly.Engine's UI system.

## What You'll Build

- Main menu with buttons
- In-game HUD with health/score
- Settings menu with controls
- Custom UI theme
- Responsive layouts

## Prerequisites

The UI system is part of the `Lilly.Engine.GameObjects` plugin. Make sure it's enabled:

```csharp
var pluginRegistry = new PluginRegistry();
pluginRegistry.RegisterPlugin(new LillyGameObjectPlugin());
```

## Step 1: Main Menu Scene

Let's start with a simple main menu:

```csharp
using Lilly.Engine.Core.Interfaces;
using Lilly.Engine.Scenes;
using Lilly.Engine.GameObjects.UI;
using Lilly.Engine.GameObjects.Types;
using Silk.NET.Maths;
using System.Drawing;

namespace Lilly.Engine.Game.Scenes;

public class MainMenuScene : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly ISceneManager _sceneManager;

    private ButtonGameObject? _playButton;
    private ButtonGameObject? _settingsButton;
    private ButtonGameObject? _exitButton;
    private TextGameObject? _titleText;

    public MainMenuScene(
        IAssetManager assetManager,
        ISceneManager sceneManager)
    {
        _assetManager = assetManager;
        _sceneManager = sceneManager;
    }

    public override void Initialize()
    {
        // Load font
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 32);
        var titleFont = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 64);

        // Title
        _titleText = new TextGameObject(titleFont)
        {
            Position = new Vector2D<float>(400, 100),
            Text = "My Awesome Game",
            Color = Color.White
        };
        AddGameObject(_titleText);

        // Play button
        _playButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(400, 300),
            Size = new Vector2D<float>(200, 60),
            Text = "Play"
        };
        _playButton.OnClick += OnPlayClicked;
        AddGameObject(_playButton);

        // Settings button
        _settingsButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(400, 380),
            Size = new Vector2D<float>(200, 60),
            Text = "Settings"
        };
        _settingsButton.OnClick += OnSettingsClicked;
        AddGameObject(_settingsButton);

        // Exit button
        _exitButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(400, 460),
            Size = new Vector2D<float>(200, 60),
            Text = "Exit"
        };
        _exitButton.OnClick += OnExitClicked;
        AddGameObject(_exitButton);
    }

    private void OnPlayClicked()
    {
        _sceneManager.ActivateSceneWithTransition(
            "Game",
            TransitionEffect.Fade,
            duration: 0.5f
        );
    }

    private void OnSettingsClicked()
    {
        _sceneManager.ActivateScene("Settings");
    }

    private void OnExitClicked()
    {
        Environment.Exit(0);
    }

    public override void Dispose()
    {
        // Unsubscribe from events
        if (_playButton != null)
            _playButton.OnClick -= OnPlayClicked;
        if (_settingsButton != null)
            _settingsButton.OnClick -= OnSettingsClicked;
        if (_exitButton != null)
            _exitButton.OnClick -= OnExitClicked;

        base.Dispose();
    }
}
```

## Step 2: Game HUD

Create a HUD that displays player stats:

```csharp
public class GameHUDScene : BaseScene
{
    private readonly IAssetManager _assetManager;

    private TextGameObject? _healthText;
    private TextGameObject? _scoreText;
    private TextGameObject? _ammoText;
    private RectangleGameObject? _healthBar;

    private int _health = 100;
    private int _score = 0;
    private int _ammo = 30;

    public GameHUDScene(IAssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public override void Initialize()
    {
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

        // Health bar background
        var healthBarBg = new RectangleGameObject
        {
            Position = new Vector2D<float>(20, 20),
            Size = new Vector2D<float>(200, 30),
            Color = Color.FromArgb(50, 255, 0, 0)
        };
        AddGameObject(healthBarBg);

        // Health bar
        _healthBar = new RectangleGameObject
        {
            Position = new Vector2D<float>(20, 20),
            Size = new Vector2D<float>(200, 30),
            Color = Color.FromArgb(200, 255, 0, 0)
        };
        AddGameObject(_healthBar);

        // Health text
        _healthText = new TextGameObject(font)
        {
            Position = new Vector2D<float>(30, 25),
            Text = $"HP: {_health}/100",
            Color = Color.White
        };
        AddGameObject(_healthText);

        // Score text
        _scoreText = new TextGameObject(font)
        {
            Position = new Vector2D<float>(20, 60),
            Text = $"Score: {_score}",
            Color = Color.Yellow
        };
        AddGameObject(_scoreText);

        // Ammo text
        _ammoText = new TextGameObject(font)
        {
            Position = new Vector2D<float>(20, 90),
            Text = $"Ammo: {_ammo}/30",
            Color = Color.White
        };
        AddGameObject(_ammoText);
    }

    public void UpdateHealth(int health)
    {
        _health = Math.Clamp(health, 0, 100);

        if (_healthText != null)
            _healthText.Text = $"HP: {_health}/100";

        if (_healthBar != null)
        {
            // Scale health bar based on health percentage
            float healthPercent = _health / 100f;
            _healthBar.Size = new Vector2D<float>(200 * healthPercent, 30);

            // Change color based on health
            if (_health > 50)
                _healthBar.Color = Color.FromArgb(200, 0, 255, 0);  // Green
            else if (_health > 25)
                _healthBar.Color = Color.FromArgb(200, 255, 255, 0);  // Yellow
            else
                _healthBar.Color = Color.FromArgb(200, 255, 0, 0);  // Red
        }
    }

    public void UpdateScore(int score)
    {
        _score = score;
        if (_scoreText != null)
            _scoreText.Text = $"Score: {_score}";
    }

    public void UpdateAmmo(int ammo)
    {
        _ammo = ammo;
        if (_ammoText != null)
        {
            _ammoText.Text = $"Ammo: {_ammo}/30";

            // Change color when low
            _ammoText.Color = _ammo < 10 ? Color.Red : Color.White;
        }
    }
}
```

Use the HUD in your game scene:

```csharp
public class GameScene : BaseScene
{
    private readonly GameHUDScene _hud;

    public GameScene(IAssetManager assetManager)
    {
        _hud = new GameHUDScene(assetManager);
    }

    public override void Initialize()
    {
        _hud.Initialize();

        // Test: damage player over time
        StartCoroutine(DamagePlayerRoutine());
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _hud.Update(deltaTime);
    }

    public override void Render(IRenderer renderer)
    {
        base.Render(renderer);
        _hud.Render(renderer);
    }

    private IEnumerator<float> DamagePlayerRoutine()
    {
        int health = 100;

        while (health > 0)
        {
            yield return 1f;  // Wait 1 second

            health -= 10;
            _hud.UpdateHealth(health);
        }
    }
}
```

## Step 3: Settings Menu

Create a settings menu with various controls:

```csharp
public class SettingsScene : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly ISceneManager _sceneManager;

    private TextEditGameObject? _playerNameInput;
    private ComboBoxGameObject? _resolutionCombo;
    private ListBoxGameObject? _qualityList;
    private ButtonGameObject? _backButton;
    private TextGameObject? _volumeLabel;

    private float _volume = 0.5f;

    public SettingsScene(
        IAssetManager assetManager,
        ISceneManager sceneManager)
    {
        _assetManager = assetManager;
        _sceneManager = sceneManager;
    }

    public override void Initialize()
    {
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

        // Title
        var title = new TextGameObject(font)
        {
            Position = new Vector2D<float>(400, 50),
            Text = "Settings",
            Color = Color.White
        };
        AddGameObject(title);

        // Player name label
        var nameLabel = new TextGameObject(font)
        {
            Position = new Vector2D<float>(100, 120),
            Text = "Player Name:",
            Color = Color.White
        };
        AddGameObject(nameLabel);

        // Player name input
        _playerNameInput = new TextEditGameObject(font)
        {
            Position = new Vector2D<float>(250, 120),
            Size = new Vector2D<float>(300, 40),
            Text = "Player",
            MaxLength = 20
        };
        AddGameObject(_playerNameInput);

        // Resolution label
        var resLabel = new TextGameObject(font)
        {
            Position = new Vector2D<float>(100, 180),
            Text = "Resolution:",
            Color = Color.White
        };
        AddGameObject(resLabel);

        // Resolution combo box
        _resolutionCombo = new ComboBoxGameObject(font)
        {
            Position = new Vector2D<float>(250, 180),
            Size = new Vector2D<float>(300, 40)
        };
        _resolutionCombo.AddItem("1920x1080");
        _resolutionCombo.AddItem("1600x900");
        _resolutionCombo.AddItem("1280x720");
        _resolutionCombo.SelectedIndex = 0;
        AddGameObject(_resolutionCombo);

        // Quality label
        var qualityLabel = new TextGameObject(font)
        {
            Position = new Vector2D<float>(100, 240),
            Text = "Quality:",
            Color = Color.White
        };
        AddGameObject(qualityLabel);

        // Quality list box
        _qualityList = new ListBoxGameObject(font)
        {
            Position = new Vector2D<float>(250, 240),
            Size = new Vector2D<float>(300, 120)
        };
        _qualityList.AddItem("Low");
        _qualityList.AddItem("Medium");
        _qualityList.AddItem("High");
        _qualityList.AddItem("Ultra");
        _qualityList.SelectedIndex = 2;  // High
        AddGameObject(_qualityList);

        // Volume label
        _volumeLabel = new TextGameObject(font)
        {
            Position = new Vector2D<float>(100, 380),
            Text = $"Volume: {(int)(_volume * 100)}%",
            Color = Color.White
        };
        AddGameObject(_volumeLabel);

        // Back button
        _backButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(400, 450),
            Size = new Vector2D<float>(200, 60),
            Text = "Back"
        };
        _backButton.OnClick += OnBackClicked;
        AddGameObject(_backButton);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Volume control with arrow keys (when focused on volume)
        if (_input.IsKeyPressed(Key.Left))
        {
            _volume = Math.Max(0, _volume - 0.1f);
            UpdateVolumeLabel();
        }
        if (_input.IsKeyPressed(Key.Right))
        {
            _volume = Math.Min(1, _volume + 0.1f);
            UpdateVolumeLabel();
        }
    }

    private void UpdateVolumeLabel()
    {
        if (_volumeLabel != null)
            _volumeLabel.Text = $"Volume: {(int)(_volume * 100)}%";
    }

    private void OnBackClicked()
    {
        // Save settings here
        SaveSettings();

        _sceneManager.ActivateScene("MainMenu");
    }

    private void SaveSettings()
    {
        var settings = new
        {
            PlayerName = _playerNameInput?.Text ?? "Player",
            Resolution = _resolutionCombo?.SelectedItem ?? "1920x1080",
            Quality = _qualityList?.SelectedItem ?? "High",
            Volume = _volume
        };

        // Save to file or config
        Console.WriteLine($"Saving settings: {settings}");
    }

    public override void Dispose()
    {
        if (_backButton != null)
            _backButton.OnClick -= OnBackClicked;

        base.Dispose();
    }
}
```

## Step 4: Custom UI Theme

Create a custom theme for your UI:

```csharp
using Lilly.Engine.GameObjects.UI;
using System.Drawing;

public static class MyGameTheme
{
    public static UITheme CreateTheme()
    {
        return new UITheme
        {
            // Button colors
            ButtonNormalColor = Color.FromArgb(200, 50, 50, 150),
            ButtonHoverColor = Color.FromArgb(220, 70, 70, 180),
            ButtonPressedColor = Color.FromArgb(180, 30, 30, 120),
            ButtonDisabledColor = Color.FromArgb(100, 100, 100, 100),
            ButtonTextColor = Color.White,

            // Text edit colors
            TextEditBackgroundColor = Color.FromArgb(200, 30, 30, 30),
            TextEditBorderColor = Color.FromArgb(255, 100, 100, 100),
            TextEditTextColor = Color.White,
            TextEditPlaceholderColor = Color.Gray,

            // List/Combo box colors
            ListBoxBackgroundColor = Color.FromArgb(200, 20, 20, 20),
            ListBoxItemNormalColor = Color.FromArgb(0, 0, 0, 0),
            ListBoxItemHoverColor = Color.FromArgb(100, 100, 100, 150),
            ListBoxItemSelectedColor = Color.FromArgb(150, 50, 150, 200),
            ListBoxBorderColor = Color.FromArgb(255, 80, 80, 80),
            ListBoxTextColor = Color.White,

            // Padding and borders
            ButtonBorderThickness = 2,
            TextEditBorderThickness = 2,
            ListBoxBorderThickness = 2,
            Padding = 10
        };
    }
}
```

Apply the theme to your controls:

```csharp
public override void Initialize()
{
    var theme = MyGameTheme.CreateTheme();

    var button = new ButtonGameObject(font)
    {
        Theme = theme,
        // ... rest of setup ...
    };
}
```

Or apply globally:

```csharp
// In bootstrap code
UITheme.Default = MyGameTheme.CreateTheme();
```

## Step 5: Responsive Layouts

Create layouts that adapt to screen size:

```csharp
public class ResponsiveMenuScene : BaseScene
{
    private readonly IWindowService _windowService;
    private readonly IAssetManager _assetManager;

    private List<ButtonGameObject> _buttons = new();

    public ResponsiveMenuScene(
        IWindowService windowService,
        IAssetManager assetManager)
    {
        _windowService = windowService;
        _assetManager = assetManager;
    }

    public override void Initialize()
    {
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

        // Create buttons
        var buttonTexts = new[] { "Play", "Settings", "Credits", "Exit" };

        foreach (var text in buttonTexts)
        {
            var button = new ButtonGameObject(font)
            {
                Text = text
            };
            _buttons.Add(button);
            AddGameObject(button);
        }

        // Initial layout
        UpdateLayout();

        // Update layout on window resize
        _windowService.OnResize += OnWindowResized;
    }

    private void UpdateLayout()
    {
        var windowSize = _windowService.GetSize();
        var centerX = windowSize.X / 2;
        var startY = windowSize.Y / 3;

        var buttonWidth = windowSize.X * 0.3f;  // 30% of screen width
        var buttonHeight = 60;
        var spacing = 20;

        for (int i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];

            button.Position = new Vector2D<float>(
                centerX - buttonWidth / 2,
                startY + i * (buttonHeight + spacing)
            );

            button.Size = new Vector2D<float>(buttonWidth, buttonHeight);
        }
    }

    private void OnWindowResized(Vector2D<int> newSize)
    {
        UpdateLayout();
    }

    public override void Dispose()
    {
        _windowService.OnResize -= OnWindowResized;
        base.Dispose();
    }
}
```

## Step 6: Stack Layout

Create vertical/horizontal layouts:

```csharp
public class StackLayoutExample : BaseScene
{
    public override void Initialize()
    {
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

        // Vertical stack
        var vStack = new StackLayoutGameObject
        {
            Position = new Vector2D<float>(100, 100),
            Orientation = StackOrientation.Vertical,
            Spacing = 10
        };

        vStack.AddChild(new ButtonGameObject(font)
        {
            Size = new Vector2D<float>(200, 50),
            Text = "Button 1"
        });

        vStack.AddChild(new ButtonGameObject(font)
        {
            Size = new Vector2D<float>(200, 50),
            Text = "Button 2"
        });

        vStack.AddChild(new TextEditGameObject(font)
        {
            Size = new Vector2D<float>(200, 40),
            Text = "Input here"
        });

        AddGameObject(vStack);

        // Horizontal stack
        var hStack = new StackLayoutGameObject
        {
            Position = new Vector2D<float>(100, 300),
            Orientation = StackOrientation.Horizontal,
            Spacing = 15
        };

        hStack.AddChild(new ButtonGameObject(font)
        {
            Size = new Vector2D<float>(100, 50),
            Text = "Left"
        });

        hStack.AddChild(new ButtonGameObject(font)
        {
            Size = new Vector2D<float>(100, 50),
            Text = "Center"
        });

        hStack.AddChild(new ButtonGameObject(font)
        {
            Size = new Vector2D<float>(100, 50),
            Text = "Right"
        });

        AddGameObject(hStack);
    }
}
```

## Step 7: Quake Console

Add a developer console for debugging:

```csharp
public class GameWithConsole : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly ICommandSystemService _commands;

    private QuakeConsoleGameObject? _console;

    public GameWithConsole(
        IAssetManager assetManager,
        ICommandSystemService commands)
    {
        _assetManager = assetManager;
        _commands = commands;
    }

    public override void Initialize()
    {
        // Register console commands
        _commands.RegisterCommand("god", () =>
        {
            Console.WriteLine("God mode enabled!");
        });

        _commands.RegisterCommand("spawn", (string entityName) =>
        {
            Console.WriteLine($"Spawning {entityName}");
        });

        _commands.RegisterCommand("teleport", (float x, float y, float z) =>
        {
            Console.WriteLine($"Teleporting to {x}, {y}, {z}");
        });

        // Create console
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 16);

        _console = new QuakeConsoleGameObject(font)
        {
            Position = new Vector2D<float>(0, 0),
            Size = new Vector2D<float>(800, 400),
            Visible = false  // Hidden by default
        };

        AddGameObject(_console);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Toggle console with ~ key
        if (_input.IsKeyPressed(Key.GraveAccent))
        {
            _console!.Visible = !_console.Visible;
        }
    }
}
```

## Complete Example: Pause Menu

Here's a full pause menu implementation:

```csharp
public class PauseMenuOverlay : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly ISceneManager _sceneManager;

    private RectangleGameObject? _overlay;
    private ButtonGameObject? _resumeButton;
    private ButtonGameObject? _settingsButton;
    private ButtonGameObject? _quitButton;

    private bool _isPaused = false;

    public bool IsPaused => _isPaused;

    public PauseMenuOverlay(
        IAssetManager assetManager,
        ISceneManager sceneManager)
    {
        _assetManager = assetManager;
        _sceneManager = sceneManager;
    }

    public override void Initialize()
    {
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 32);

        // Semi-transparent overlay
        _overlay = new RectangleGameObject
        {
            Position = Vector2D<float>.Zero,
            Size = new Vector2D<float>(800, 600),
            Color = Color.FromArgb(150, 0, 0, 0),
            Visible = false
        };
        AddGameObject(_overlay);

        // Resume button
        _resumeButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(300, 200),
            Size = new Vector2D<float>(200, 60),
            Text = "Resume",
            Visible = false
        };
        _resumeButton.OnClick += Resume;
        AddGameObject(_resumeButton);

        // Settings button
        _settingsButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(300, 280),
            Size = new Vector2D<float>(200, 60),
            Text = "Settings",
            Visible = false
        };
        _settingsButton.OnClick += OpenSettings;
        AddGameObject(_settingsButton);

        // Quit button
        _quitButton = new ButtonGameObject(font)
        {
            Position = new Vector2D<float>(300, 360),
            Size = new Vector2D<float>(200, 60),
            Text = "Quit to Menu",
            Visible = false
        };
        _quitButton.OnClick += QuitToMenu;
        AddGameObject(_quitButton);
    }

    public void Pause()
    {
        _isPaused = true;
        ShowMenu();
    }

    public void Resume()
    {
        _isPaused = false;
        HideMenu();
    }

    private void ShowMenu()
    {
        _overlay!.Visible = true;
        _resumeButton!.Visible = true;
        _settingsButton!.Visible = true;
        _quitButton!.Visible = true;
    }

    private void HideMenu()
    {
        _overlay!.Visible = false;
        _resumeButton!.Visible = false;
        _settingsButton!.Visible = false;
        _quitButton!.Visible = false;
    }

    private void OpenSettings()
    {
        _sceneManager.ActivateScene("Settings");
    }

    private void QuitToMenu()
    {
        Resume();
        _sceneManager.ActivateScene("MainMenu");
    }

    public override void Dispose()
    {
        _resumeButton!.OnClick -= Resume;
        _settingsButton!.OnClick -= OpenSettings;
        _quitButton!.OnClick -= QuitToMenu;

        base.Dispose();
    }
}
```

Use in your game scene:

```csharp
public class GameScene : BaseScene
{
    private readonly PauseMenuOverlay _pauseMenu;

    public override void Update(float deltaTime)
    {
        // Toggle pause with Escape
        if (_input.IsKeyPressed(Key.Escape))
        {
            if (_pauseMenu.IsPaused)
                _pauseMenu.Resume();
            else
                _pauseMenu.Pause();
        }

        // Only update game if not paused
        if (!_pauseMenu.IsPaused)
        {
            // Game logic here
        }

        _pauseMenu.Update(deltaTime);
    }

    public override void Render(IRenderer renderer)
    {
        // Always render game
        base.Render(renderer);

        // Render pause menu on top
        _pauseMenu.Render(renderer);
    }
}
```

## Next Steps

- Add animations to UI elements
- Implement drag and drop
- Create modal dialogs
- Add tooltips
- Build an inventory system
- Create a dialogue system

Check the source code in `src/Lilly.Engine.GameObjects/` for all available UI controls and their properties!