using SkiaSharp;
using webapi.Models.ModelsImpl;

namespace webapi.Pdf;

public static class MapImageGenerator
{
    private const int TileSize = 256;
    private const int MaxTiles = 6; // max tiles per axis → 6×6 grid

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "RechtUndOrdnungSH/1.0 PDF generator" } }
    };

    public static async Task<byte[]?> GenerateAsync(List<Coordinate> coordinates, int targetWidth = 1500)
    {
        if (coordinates.Count == 0) return null;

        var minLat = coordinates.Min(c => c.Latitude);
        var maxLat = coordinates.Max(c => c.Latitude);
        var minLon = coordinates.Min(c => c.Longitude);
        var maxLon = coordinates.Max(c => c.Longitude);

        var zoom = ChooseZoom(minLat, maxLat, minLon, maxLon);

        var x1 = Math.Max(0, LonToTile(minLon, zoom) - 1);
        var x2 = Math.Min((1 << zoom) - 1, LonToTile(maxLon, zoom) + 1);
        var y1 = Math.Max(0, LatToTile(maxLat, zoom) - 1);
        var y2 = Math.Min((1 << zoom) - 1, LatToTile(minLat, zoom) + 1);

        // Clamp to MaxTiles × MaxTiles
        if (x2 - x1 + 1 > MaxTiles) x2 = x1 + MaxTiles - 1;
        if (y2 - y1 + 1 > MaxTiles) y2 = y1 + MaxTiles - 1;

        int tilesX = x2 - x1 + 1;
        int tilesY = y2 - y1 + 1;
        int bitmapW = tilesX * TileSize;
        int bitmapH = tilesY * TileSize;

        using var bitmap = new SKBitmap(bitmapW, bitmapH);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.LightGray);

            // Download and draw tiles in parallel
            var downloads = new List<(int tx, int ty, Task<byte[]?> task)>();
            for (int tx = x1; tx <= x2; tx++)
                for (int ty = y1; ty <= y2; ty++)
                    downloads.Add((tx, ty, DownloadTileAsync(tx, ty, zoom)));

            await Task.WhenAll(downloads.Select(d => d.task));

            foreach (var (tx, ty, task) in downloads)
            {
                var bytes = await task;
                if (bytes == null) continue;
                using var tile = SKBitmap.Decode(bytes);
                if (tile == null) continue;
                canvas.DrawBitmap(tile, (tx - x1) * TileSize, (ty - y1) * TileSize);
            }

            // Convert a coordinate to pixel position (Mercator-correct)
            SKPoint ToPixel(double lat, double lon)
            {
                var fx = FractionalTileX(lon, zoom) - x1;
                var fy = FractionalTileY(lat, zoom) - y1;
                return new SKPoint((float)(fx * TileSize), (float)(fy * TileSize));
            }

            var pts = coordinates.Select(c => ToPixel(c.Latitude, c.Longitude)).ToArray();

            DrawShape(canvas, pts, coordinates.Count);
        }

        // Scale to target width, preserving aspect ratio
        int scaledW = Math.Min(targetWidth, bitmapW);
        int scaledH = (int)Math.Round((double)bitmapH * scaledW / bitmapW);
        var scaledInfo = new SKImageInfo(scaledW, scaledH);
        using var scaled = bitmap.Resize(scaledInfo, new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var img = SKImage.FromBitmap(scaled ?? bitmap);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static void DrawShape(SKCanvas canvas, SKPoint[] pts, int count)
    {
        var stroke = new SKPaint
        {
            Color = SKColor.Parse("#1565C0"),
            StrokeWidth = 3f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeCap = SKStrokeCap.Round
        };
        var fill = new SKPaint
        {
            Color = SKColor.Parse("#1565C0").WithAlpha(40),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        var dot = new SKPaint
        {
            Color = SKColor.Parse("#1565C0"),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        var dotBorder = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true
        };

        if (count == 1)
        {
            canvas.DrawCircle(pts[0], 9, dot);
            canvas.DrawCircle(pts[0], 9, dotBorder);
        }
        else if (count == 2)
        {
            canvas.DrawLine(pts[0], pts[1], stroke);
            foreach (var p in pts) { canvas.DrawCircle(p, 6, dot); canvas.DrawCircle(p, 6, dotBorder); }
        }
        else
        {
            var path = new SKPath();
            path.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) path.LineTo(pts[i]);
            path.Close();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
            foreach (var p in pts) { canvas.DrawCircle(p, 5, dot); canvas.DrawCircle(p, 5, dotBorder); }
        }
    }

    private static async Task<byte[]?> DownloadTileAsync(int x, int y, int zoom)
    {
        try
        {
            return await Http.GetByteArrayAsync($"https://tile.openstreetmap.org/{zoom}/{x}/{y}.png");
        }
        catch
        {
            return null;
        }
    }

    private static int ChooseZoom(double minLat, double maxLat, double minLon, double maxLon)
    {
        if (minLat == maxLat && minLon == maxLon) return 15;

        for (int z = 17; z >= 5; z--)
        {
            int dx = LonToTile(maxLon, z) - LonToTile(minLon, z) + 1;
            int dy = LatToTile(minLat, z) - LatToTile(maxLat, z) + 1;
            if (dx <= MaxTiles - 2 && dy <= MaxTiles - 2)
                return z;
        }
        return 5;
    }

    private static int LonToTile(double lon, int z) =>
        (int)Math.Floor((lon + 180) / 360 * (1 << z));

    private static int LatToTile(double lat, int z)
    {
        var rad = lat * Math.PI / 180;
        return (int)Math.Floor((1 - Math.Log(Math.Tan(rad) + 1 / Math.Cos(rad)) / Math.PI) / 2 * (1 << z));
    }

    private static double FractionalTileX(double lon, int z) =>
        (lon + 180) / 360 * (1 << z);

    private static double FractionalTileY(double lat, int z)
    {
        var rad = lat * Math.PI / 180;
        return (1 - Math.Log(Math.Tan(rad) + 1 / Math.Cos(rad)) / Math.PI) / 2 * (1 << z);
    }
}
