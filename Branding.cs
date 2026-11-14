using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AudioConverter;

/// <summary>
/// The brand artwork is optional. Each image is looked up in the packaged resources and quietly
/// skipped when it isn't there, so a missing file can never stop the app from starting.
/// </summary>
public static class Branding
{
    public static ImageSource? AppIcon() => Load("Images/app.ico");

    public static ImageSource? Logo() => Load("Images/logo.png");

    static ImageSource? Load(string relativePath)
    {
        try
        {
            var uri = new Uri("pack://application:,,,/" + relativePath, UriKind.Absolute);
            return BitmapFrame.Create(uri);
        }
        catch
        {
            return null;
        }
    }
}
