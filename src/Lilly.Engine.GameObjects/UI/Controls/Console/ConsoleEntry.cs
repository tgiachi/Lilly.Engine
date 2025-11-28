using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Controls.Console;

/// <summary>
/// Represents a single entry in the console.
/// </summary>
internal readonly record struct ConsoleEntry(string Text, Color4b Foreground, Color4b? Background);
