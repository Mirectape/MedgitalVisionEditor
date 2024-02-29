using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasLoader
{
    /// <summary>
    /// Загружает SpriteAtlas из ресурсов.
    /// </summary>
    /// <param name="path">Путь к атласу в папке Resources.</param>
    /// <returns>Загруженный SpriteAtlas или null, если атлас не найден.</returns>
    public static SpriteAtlas LoadSpriteAtlas(string path)
    {
        return Resources.Load<SpriteAtlas>(path);
    }
}