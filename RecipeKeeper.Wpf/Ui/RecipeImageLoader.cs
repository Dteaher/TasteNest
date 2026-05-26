using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace RecipeKeeper.Wpf.Ui;

public static class RecipeImageLoader
{
    private const string FallbackImage = "Assets/Recipes/recipe-placeholder.jpg";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static BitmapImage? Load(string imagePath)
    {
        var uri = ResolveImageUri(string.IsNullOrWhiteSpace(imagePath) ? FallbackImage : imagePath);
        if (uri is null)
        {
            return null;
        }

        try
        {
            return uri.Scheme is "http" or "https"
                ? LoadRemoteImage(uri)
                : LoadLocalImage(uri);
        }
        catch
        {
            return string.Equals(imagePath, FallbackImage, StringComparison.OrdinalIgnoreCase)
                ? null
                : Load(FallbackImage);
        }
    }

    private static Uri? ResolveImageUri(string imagePath)
    {
        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme is "http" or "https" or "file"
                ? absoluteUri
                : null;
        }

        var localPath = Path.Combine(AppContext.BaseDirectory, imagePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(localPath)
            ? new Uri(localPath, UriKind.Absolute)
            : null;
    }

    private static BitmapImage LoadLocalImage(Uri uri)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.UriSource = uri;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapImage LoadRemoteImage(Uri uri)
    {
        var bytes = HttpClient.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        using var stream = new MemoryStream(bytes);

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TasteNest/1.0");
        return client;
    }
}
