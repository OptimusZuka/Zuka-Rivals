using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace IRacingTracker;

/// <summary>Gestao de tags: ver, criar (nome, icone, cor) e apagar as personalizadas.</summary>
public partial class MainWindow
{
    private static readonly string[] PickIcons =
    {
        "skull", "shield", "bolt", "warning", "star", "repeat", "handshake", "flag",
        "heart", "eye", "clock", "flame", "crown", "lock", "target", "car",
    };

    private static readonly int[] PickColors =
    {
        0xF85149, 0xF0883E, 0xE3B341, 0x3FB950, 0x56D4DD, 0x58A6FF, 0xBC8CFF, 0xF778BA, 0xE6EDF3, 0x8B949E,
    };

    private string _newTagIcon = "flag";
    private int _newTagColor = 0x58A6FF;
    private readonly Dictionary<string, Border> _iconButtons = new();
    private readonly Dictionary<string, Path> _iconPaths = new();
    private readonly Dictionary<int, Border> _colorButtons = new();

    private static Path MakeIconPath(string key, Brush stroke)
    {
        return new Path
        {
            Data = TagIcons.Get(key),
            Stroke = stroke,
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
    }

    private static Viewbox IconBox(Path path, double size)
    {
        var canvas = new Canvas { Width = 24, Height = 24 };
        canvas.Children.Add(path);
        return new Viewbox { Width = size, Height = size, Child = canvas };
    }

    private void InitTagManager()
    {
        IconPicker.Children.Clear();
        ColorPicker.Children.Clear();
        _iconButtons.Clear();
        _iconPaths.Clear();
        _colorButtons.Clear();

        foreach (string key in PickIcons)
        {
            string k = key;
            Path path = MakeIconPath(k, BrushCache.Rgb(_newTagColor));
            var b = new Border
            {
                Width = 32, Height = 32, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 4, 4),
                BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Child = IconBox(path, 18),
            };
            b.MouseLeftButtonUp += (_, _) => { _newTagIcon = k; UpdatePickers(); };
            _iconButtons[k] = b;
            _iconPaths[k] = path;
            IconPicker.Children.Add(b);
        }

        foreach (int color in PickColors)
        {
            int c = color;
            var b = new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(11), Margin = new Thickness(0, 0, 6, 4),
                Background = BrushCache.Rgb(c), BorderThickness = new Thickness(2), Cursor = Cursors.Hand,
            };
            b.MouseLeftButtonUp += (_, _) => { _newTagColor = c; UpdatePickers(); };
            _colorButtons[c] = b;
            ColorPicker.Children.Add(b);
        }

        UpdatePickers();
        RebuildTagList();
    }

    private void UpdatePickers()
    {
        Brush stroke = BrushCache.Rgb(_newTagColor);
        foreach (var kv in _iconButtons)
        {
            bool on = kv.Key == _newTagIcon;
            kv.Value.Background = on ? BrushCache.Rgb(0x26323D) : BrushCache.Rgb(0x1C1C1C);
            kv.Value.BorderBrush = on ? BrushCache.Rgb(0x5A86B0) : BrushCache.Rgb(0x333333);
            _iconPaths[kv.Key].Stroke = stroke;
        }
        foreach (var kv in _colorButtons)
            kv.Value.BorderBrush = kv.Key == _newTagColor ? Brushes.White : Brushes.Transparent;
    }

    private void RebuildTagList()
    {
        TagListPanel.Children.Clear();
        foreach (TagDef t in _tags)
        {
            TagDef tag = t;
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(IconBox(MakeIconPath(tag.IconKey, tag.Brush), 14));
            row.Children.Add(new TextBlock
            {
                Text = tag.Name, Margin = new Thickness(6, 0, 0, 0), FontSize = 12,
                Foreground = BrushCache.Rgb(0xD0D0D0), ToolTip = tag.Tooltip,
            });
            if (!tag.IsBuiltin)
            {
                var x = new TextBlock
                {
                    Text = "✕", Margin = new Thickness(8, 0, 0, 0), FontSize = 11, Cursor = Cursors.Hand,
                    Foreground = BrushCache.Rgb(0xE5534B), ToolTip = Loc.T("tag_del_tip"),
                };
                x.MouseLeftButtonUp += (_, _) => DeleteCustomTag(tag);
                row.Children.Add(x);
            }
            TagListPanel.Children.Add(new Border
            {
                Child = row, Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1),
                BorderBrush = BrushCache.Rgb(0x333333), Background = BrushCache.Rgb(0x1C1C1C),
            });
        }
    }

    private void ReloadTags()
    {
        if (_history == null) return;
        _tags = _history.LoadTags();
        _driverTags = _history.LoadAllDriverTags();
        if (_fTag > 0 && _tags.All(t => t.Id != _fTag)) _fTag = -2;
        CloseDetail();
        RebuildTagList();
        UpdateFilterUi();
    }

    private void CreateTag_Click(object sender, RoutedEventArgs e)
    {
        CreateTag();
    }

    private void CreateTag()
    {
        if (_history == null) { Toast(Loc.T("hist_unavail")); return; }
        string name = (TagNameBox.Text ?? "").Trim();
        if (name.Length == 0) { Toast(Loc.T("tag_need_name")); return; }
        if (_tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            Toast(Loc.T("tag_exists"));
            return;
        }

        try
        {
            _history.AddCustomTag(name, _newTagIcon, _newTagColor);
            TagNameBox.Text = "";
            EndTyping(true);
            ReloadTags();
            Toast(Loc.T("tag_created", name));
        }
        catch (Exception ex)
        {
            AppPaths.Log("Criar tag: " + ex);
            Toast(Loc.T("tag_err_create", ex.Message));
        }
    }

    private void DeleteCustomTag(TagDef tag)
    {
        if (_history == null) return;
        try
        {
            BeginTyping();
            var answer = MessageBox.Show(this,
                Loc.T("tag_del_q", tag.Name),
                Loc.T("tag_del_title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            EndTyping(false);
            if (answer != MessageBoxResult.Yes) return;

            _history.DeleteCustomTag(tag.Id);
            ReloadTags();
            Toast(Loc.T("tag_deleted", tag.Name));
        }
        catch (Exception ex)
        {
            EndTyping(false);
            AppPaths.Log("Apagar tag: " + ex);
            Toast(Loc.T("tag_err_del", ex.Message));
        }
    }

    private void TagNameBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginTyping();
    }

    private void TagNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CreateTag();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            EndTyping(true);
        }
    }
}
