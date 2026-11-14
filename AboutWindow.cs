using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AudioConverter;

/// <summary>The About box: version, release date and credits.</summary>
public sealed class AboutWindow : Window
{
    public const string ReleaseDate = "14 November 2026";

    public AboutWindow()
    {
        Title = "About FormatCraft [Audio]";
        Width = 560;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Icon = Branding.AppIcon();
        FontFamily = (FontFamily)Application.Current.FindResource("BrandFont");

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string versionText = version == null ? "" : $"{version.Major}.{version.Minor}.{version.Build}";

        var panel = new StackPanel { Margin = new Thickness(26) };

        var logo = Branding.Logo() is { } logoSource
            ? new Image
            {
                Source = logoSource,
                Width = 104,
                Height = 104,
                Margin = new Thickness(0, 0, 20, 0)
            }
            : null;

        if (logo != null)
            RenderOptions.SetBitmapScalingMode(logo, BitmapScalingMode.HighQuality);

        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(new TextBlock
        {
            Text = "FormatCraft [Audio]",
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("NavyBrush")
        });
        titles.Children.Add(new TextBlock
        {
            Text = $"Version {versionText}  -  {ReleaseDate}",
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        });

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
        if (logo != null) header.Children.Add(logo);
        header.Children.Add(titles);
        panel.Children.Add(header);

        AddLinkSection(panel, "Website", "https://sonuslucis.com", "sonuslucis.com");
        AddSection(panel, "Created by", "Sonus Lucis - concept, design, direction and testing");
        AddSection(panel, "Programmed by", "Claude, made by Anthropic - all of the code");
        AddSection(panel, "Built with",
            "FFmpeg (https://ffmpeg.org), used under the terms of its licence. Everything is processed on your own " +
            "computer; no audio is ever uploaded anywhere.");
        AddSection(panel, "Typeface", "Montserrat, by The Montserrat Project Authors (SIL Open Font License 1.1)");

        var ok = new Button
        {
            Content = "OK",
            Width = 90,
            Padding = new Thickness(0, 5, 0, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
            IsDefault = true,
            IsCancel = true,
            Style = (Style)Application.Current.FindResource("PrimaryButton")
        };
        ok.Click += (_, _) => Close();
        panel.Children.Add(ok);

        Content = panel;
    }

    static void AddLinkSection(StackPanel panel, string heading, string url, string display)
    {
        panel.Children.Add(new TextBlock
        {
            Text = heading,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("NavyBrush"),
            Margin = new Thickness(0, 0, 0, 2)
        });
        var link = new Hyperlink(new Run(display)) { NavigateUri = new Uri(url) };
        link.RequestNavigate += (_, e) =>
        {
            MainWindow.OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        };
        panel.Children.Add(new TextBlock(link) { Margin = new Thickness(0, 0, 0, 12) });
    }

    static void AddSection(StackPanel panel, string heading, string text)
    {
        panel.Children.Add(new TextBlock
        {
            Text = heading,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("NavyBrush"),
            Margin = new Thickness(0, 0, 0, 2)
        });
        panel.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });
    }
}
