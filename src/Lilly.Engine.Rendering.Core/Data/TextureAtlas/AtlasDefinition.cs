namespace Lilly.Engine.Rendering.Core.Data.TextureAtlas;

public class AtlasDefinition
{
    public string TextureName { get; set; }
    public string Name { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Margin { get; set; }

    public int Spacing { get; set; }

    public List<AtlasRegion> Regions { get; set; } = [];

    public AtlasDefinition(string textureName, string name, int width, int height, int margin, int spacing)
    {
        TextureName = textureName;
        Name = name;
        Width = width;
        Height = height;
        Margin = margin;
        Spacing = spacing;
    }

    public void AddRegion(AtlasRegion region)
    {
        Regions.Add(region);
    }

    public override string ToString()
    {
        return
            $"AtlasDefinition(TextureName={TextureName} Name={Name}, Width={Width}, Height={Height}, Margin={Margin}, Spacing={Spacing}, Regions=[{string.Join(", ", Regions)}])";
    }
}
