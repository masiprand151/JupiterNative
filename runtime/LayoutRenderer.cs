using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace Jupiter.Runtime;

public static class LayoutRenderer
{
  static readonly Dictionary<string, TextBox> keyedTextInputs = new();
  static readonly Dictionary<string, LibVLCSharp.Shared.MediaPlayer> keyedMediaPlayers = new();
  static readonly Dictionary<string, VideoView> keyedVideoViews = new();
  static readonly HashSet<TextBox> subscribedTextInputs = new();
  static readonly HashSet<TextBox> syncingTextInputs = new();
  static readonly HashSet<TextBox> autoFocusedTextInputs = new();
  static readonly Dictionary<TextBox, ScriptObject?> textInputCallbacks = new();
  static readonly Dictionary<TextBox, ScriptObject?> textInputSubmitCallbacks = new();
  static readonly Dictionary<TextBox, ScriptObject?> textInputFocusCallbacks = new();
  static readonly Dictionary<TextBox, ScriptObject?> textInputBlurCallbacks = new();
  static readonly Dictionary<string, PasswordBox> keyedPasswordInputs = new();
  static readonly HashSet<PasswordBox> subscribedPasswordInputs = new();
  static readonly HashSet<PasswordBox> syncingPasswordInputs = new();
  static readonly Dictionary<PasswordBox, ScriptObject?> passwordInputCallbacks = new();
  static readonly Dictionary<string, Slider> keyedSliders = new();
  static readonly HashSet<Slider> subscribedSliders = new();
  static readonly Dictionary<Slider, ScriptObject?> sliderCallbacks = new();
  static readonly Dictionary<string, CheckBox> keyedCheckBoxes = new();
  static readonly Dictionary<string, RadioButton> keyedRadioButtons = new();
  static readonly Dictionary<string, CheckBox> keyedSwitches = new();
  static readonly Dictionary<string, DatePicker> keyedDatePickers = new();

  public static UIElement Render(object? value, V8ScriptEngine engine)
  {
    if (value is null) return new TextBlock();
    if (value is string or double or int or long or decimal) return new TextBlock { Text = value.ToString() };
    if (TryGetItems(value, out var items))
    {
      var panel = new StackPanel();
      foreach (var item in items) panel.Children.Add(Render(item, engine)); return panel;
    }
    if (value is not ScriptObject node) return new TextBlock { Text = value.ToString() ?? string.Empty };
    var type = node.GetProperty("type")?.ToString();
    var props = node.GetProperty("props") as ScriptObject;
    var children = node.GetProperty("children");
    if (type == "Window") return RenderWindow(props, children, engine);
    if (type is "View" or "SafeAreaView") return RenderView(props, children, engine);
    if (type == "ScrollView") return RenderScrollView(props, children, engine);
    if (type == "Text") return RenderText(props, children);
    if (type == "Button") return RenderButton(props, children);
    if (type is "Pressable" or "TouchableOpacity") return RenderPressable(props, children, engine);
    if (type == "ActivityIndicator") return RenderActivityIndicator(props);
    if (type == "Switch") return RenderSwitch(props);
    if (type == "Modal") return RenderModal(props, children, engine);
    if (type == "Slider") return RenderSlider(props);
    if (type == "ProgressBar") return RenderProgressBar(props);
    if (type == "CheckBox") return RenderCheckBox(props);
    if (type == "RadioButton") return RenderRadioButton(props);
    if (type == "DatePicker") return RenderDatePicker(props);
    if (type == "Image") return RenderImage(props);
    if (type == "Video") return RenderVideo(props);
    if (type == "TextInput") return Equals(Get(props, "secureTextEntry"), true) ? RenderPasswordInput(props) : RenderTextInput(props);
    return new TextBlock { Text = $"Unknown Jupiter component: {type}" };
  }

  static string GetLayoutValue(ScriptObject? props, string name, string fallback)
  {
    return Get(props, name)?.ToString() ?? fallback;
  }

  // static UIElement RenderView(ScriptObject? props, object? children, V8ScriptEngine engine)
  // {
  //   var directionValue = Get(props, "direction")?.ToString();
  //   var direction = directionValue is "horizontal" or "row" ? Orientation.Horizontal : Orientation.Vertical;

  //   var justifyContent = GetLayoutValue(
  //          props,
  //          "justifyContent",
  //          "start"
  //      );

  //   var alignItems = GetLayoutValue(
  //       props,
  //       "alignItems",
  //       "stretch"
  //   );


  //   var childValues = ChildValues(children);
  //   if (childValues.Any(IsModalNode))
  //   {
  //     var layers = new Grid();
  //     var contentChildren = childValues.Where(child => !IsModalNode(child)).ToList();
  //     var contentPanel = new StackPanel { Orientation = direction };
  //     AddChildren(contentPanel, contentChildren, engine, direction, Number(Get(props, "gap"), 0));
  //     layers.Children.Add(contentPanel);
  //     foreach (var child in childValues.Where(IsModalNode))
  //     {
  //       var element = Render(child, engine);
  //       Grid.SetRow(element, 0);
  //       layers.Children.Add(element);
  //     }
  //     return WrapContainer(layers, props);
  //   }
  //   if (direction == Orientation.Vertical && childValues.Any(IsScrollViewNode))
  //   {
  //     var grid = new Grid();
  //     foreach (var child in childValues)
  //     {
  //       var element = Render(child, engine);
  //       grid.RowDefinitions.Add(new RowDefinition { Height = element is ScrollViewer ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
  //       Grid.SetRow(element, grid.RowDefinitions.Count - 1);
  //       grid.Children.Add(element);
  //     }
  //     return WrapContainer(grid, props);
  //   }
  //   var panel = new StackPanel { Orientation = direction };
  //   AddChildren(panel, children, engine, direction, Number(Get(props, "gap"), 0));
  //   return WrapContainer(panel, props);
  // }


  static UIElement RenderView(ScriptObject? props, object? children, V8ScriptEngine engine)
  {
    var directionValue = Get(props, "direction")?.ToString();
    var direction = directionValue is "horizontal" or "row"
        ? Orientation.Horizontal
        : Orientation.Vertical;

    var justifyContent = GetLayoutValue(props, "justifyContent", "start");
    var alignItems = GetLayoutValue(props, "alignItems", "stretch");
    var gap = Number(Get(props, "gap"), 0);

    var childValues = ChildValues(children);

    // Mengatur alignment tanpa mengubah AddChildren atau WrapContainer.
    void ApplyAlignment(StackPanel panel)
    {
      // Posisi kelompok children pada sumbu utama.
      if (direction == Orientation.Horizontal)
      {
        panel.HorizontalAlignment = justifyContent switch
        {
          "center" => HorizontalAlignment.Center,
          "end" => HorizontalAlignment.Right,
          _ => HorizontalAlignment.Stretch
        };
      }
      else
      {
        panel.VerticalAlignment = justifyContent switch
        {
          "center" => VerticalAlignment.Center,
          "end" => VerticalAlignment.Bottom,
          _ => VerticalAlignment.Stretch
        };
      }

      // Posisi setiap child pada sumbu silang.
      foreach (UIElement child in panel.Children)
      {
        if (child is not FrameworkElement element)
          continue;

        if (direction == Orientation.Horizontal)
        {
          element.VerticalAlignment = alignItems switch
          {
            "start" => VerticalAlignment.Top,
            "center" => VerticalAlignment.Center,
            "end" => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Stretch
          };
        }
        else
        {
          element.HorizontalAlignment = alignItems switch
          {
            "start" => HorizontalAlignment.Left,
            "center" => HorizontalAlignment.Center,
            "end" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Stretch
          };
        }
      }
    }

    if (childValues.Any(IsModalNode))
    {
      var layers = new Grid();

      var contentChildren = childValues
          .Where(child => !IsModalNode(child))
          .ToList();

      var contentPanel = new StackPanel
      {
        Orientation = direction
      };

      AddChildren(contentPanel, contentChildren, engine, direction, gap);
      ApplyAlignment(contentPanel);
      layers.Children.Add(contentPanel);

      foreach (var child in childValues.Where(IsModalNode))
      {
        var element = Render(child, engine);
        Grid.SetRow(element, 0);
        layers.Children.Add(element);
      }

      return WrapContainer(layers, props);
    }

    if (direction == Orientation.Vertical && childValues.Any(IsScrollViewNode))
    {
      var grid = new Grid();

      foreach (var child in childValues)
      {
        var element = Render(child, engine);

        grid.RowDefinitions.Add(new RowDefinition
        {
          Height = element is ScrollViewer
                ? new GridLength(1, GridUnitType.Star)
                : GridLength.Auto
        });

        Grid.SetRow(element, grid.RowDefinitions.Count - 1);
        grid.Children.Add(element);
      }

      return WrapContainer(grid, props);
    }

    var panel = new StackPanel
    {
      Orientation = direction
    };

    AddChildren(panel, children, engine, direction, gap);
    ApplyAlignment(panel);

    return WrapContainer(panel, props);
  }


  static UIElement RenderWindow(ScriptObject? props, object? children, V8ScriptEngine engine)
  {
    var grid = new Grid();
    var childValues = ChildValues(children);
    foreach (var child in childValues)
    {
      var element = Render(child, engine);
      grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      Grid.SetRow(element, grid.RowDefinitions.Count - 1);
      grid.Children.Add(element);
    }
    return WrapContainer(grid, props);
  }

  static UIElement RenderScrollView(ScriptObject? props, object? children, V8ScriptEngine engine)
  {
    var horizontal = Equals(Get(props, "horizontal"), true);
    var orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
    var panel = new StackPanel { Orientation = orientation };
    AddChildren(panel, children, engine, orientation, Number(Get(props, "gap"), 0));
    return new ScrollViewer
    {
      Content = panel,
      Padding = Thickness(Get(props, "padding")),
      Margin = Thickness(Get(props, "margin")),
      Width = Size(Get(props, "width")),
      Height = Size(Get(props, "height")),
      HorizontalAlignment = Alignment(Get(props, "alignment")),
      VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : (Equals(Get(props, "showsVerticalScrollIndicator"), false) ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto),
      HorizontalScrollBarVisibility = horizontal ? (Equals(Get(props, "showsHorizontalScrollIndicator"), false) ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto) : ScrollBarVisibility.Disabled
    };
  }

  static Border WrapContainer(Panel panel, ScriptObject? props) => new()
  {
    Child = panel,
    Padding = Thickness(Get(props, "padding")),
    Margin = Thickness(Get(props, "margin")),
    Width = Size(Get(props, "width")),
    Height = Size(Get(props, "height")),
    HorizontalAlignment = Alignment(Get(props, "alignment")),
    Background = Color(Get(props, "backgroundColor"), Brushes.Transparent)
  };

  static TextBlock RenderText(ScriptObject? props, object? children) => new()
  {
    Text = Get(props, "value")?.ToString() ?? TextValue(children),
    FontSize = Number(Get(props, "fontSize"), 14),
    FontWeight = FontWeight(Get(props, "fontWeight")),
    Foreground = Color(Get(props, "color"), Brushes.Black),
    TextAlignment = TextAlignment(Get(props, "textAlign")),
    Margin = Thickness(Get(props, "margin"))
  };

  static Button RenderButton(ScriptObject? props, object? children)
  {
    var button = new Button
    {
      Content = Get(props, "title")?.ToString() ?? TextValue(children),
      Width = Size(Get(props, "width")),
      Height = Size(Get(props, "height")),
      Margin = Thickness(Get(props, "margin")),
      IsEnabled = !Equals(Get(props, "disabled"), true),
      Cursor = Cursors.Hand,
      HorizontalAlignment = Alignment(Get(props, "alignment"))
    };
    if (Get(props, "onPress") is ScriptObject callback) button.Click += (_, _) => { try { callback.Invoke(false); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter event error", MessageBoxButton.OK, MessageBoxImage.Error); } };
    return button;
  }

  static Border RenderPressable(ScriptObject? props, object? children, V8ScriptEngine engine)
  {
    var panel = new StackPanel();
    AddChildren(panel, children, engine, Orientation.Vertical, Number(Get(props, "gap"), 0));
    var border = new Border { Child = panel, Padding = Thickness(Get(props, "padding")), Margin = Thickness(Get(props, "margin")), Background = Color(Get(props, "backgroundColor"), Brushes.Transparent), Opacity = Number(Get(props, "opacity"), 1), Cursor = Cursors.Hand };
    border.IsEnabled = !Equals(Get(props, "disabled"), true);
    if (Get(props, "onPress") is ScriptObject callback && border.IsEnabled) border.MouseLeftButtonUp += (_, _) => { try { callback.Invoke(false); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter event error", MessageBoxButton.OK, MessageBoxImage.Error); } };
    return border;
  }

  static ProgressBar RenderActivityIndicator(ScriptObject? props) => new()
  {
    IsIndeterminate = true,
    IsEnabled = !Equals(Get(props, "animating"), false),
    Width = IndicatorSize(Get(props, "size")),
    Height = 20,
    Foreground = Color(Get(props, "color"), Brushes.DodgerBlue),
    Margin = Thickness(Get(props, "margin"))
  };

  static CheckBox RenderSwitch(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var control = key is not null && keyedSwitches.TryGetValue(key, out var existing) ? existing : new CheckBox();
    if (key is not null) keyedSwitches[key] = control;
    control.IsChecked = Equals(Get(props, "value") ?? Get(props, "defaultValue"), true); control.IsEnabled = !Equals(Get(props, "disabled"), true); control.Margin = Thickness(Get(props, "margin"));
    if (Get(props, "onValueChange") is ScriptObject callback)
    {
      control.Checked += (_, _) => { try { callback.Invoke(false, true); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter event error", MessageBoxButton.OK, MessageBoxImage.Error); } };
      control.Unchecked += (_, _) => { try { callback.Invoke(false, false); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter event error", MessageBoxButton.OK, MessageBoxImage.Error); } };
    }
    return control;
  }

  static UIElement RenderModal(ScriptObject? props, object? children, V8ScriptEngine engine)
  {
    var panel = new StackPanel();
    AddChildren(panel, children, engine, Orientation.Vertical, Number(Get(props, "gap"), 0));
    var overlay = new Grid { Visibility = Equals(Get(props, "visible"), false) ? Visibility.Collapsed : Visibility.Visible, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
    var backdrop = new Border { Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 0, 0, 0)), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, Cursor = Cursors.Hand };
    var card = new Border { Child = panel, Padding = Thickness(Get(props, "padding")), Background = Color(Get(props, "backgroundColor"), Brushes.White), BorderBrush = Color(Get(props, "borderColor"), Brushes.Gray), BorderThickness = new Thickness(Number(Get(props, "borderWidth"), 1)), CornerRadius = new CornerRadius(Number(Get(props, "borderRadius"), 8)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MinWidth = 280, MaxWidth = 560, Margin = Thickness(Get(props, "margin")) };
    overlay.Children.Add(backdrop);
    overlay.Children.Add(card);
    if (Get(props, "onRequestClose") is ScriptObject callback) backdrop.MouseLeftButtonUp += (_, _) => { try { callback.Invoke(false); } catch { } };
    return overlay;
  }

  static Slider RenderSlider(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var control = key is not null && keyedSliders.TryGetValue(key, out var keyedSlider) ? keyedSlider : new Slider();
    if (key is not null) keyedSliders[key] = control;
    var value = Number(Get(props, "value"), 0);
    if (!control.IsMouseCaptureWithin) control.Value = value;
    control.Minimum = Number(Get(props, "minimumValue"), 0);
    control.Maximum = Number(Get(props, "maximumValue"), 1);
    control.Orientation = Orientation.Horizontal;
    control.FlowDirection = FlowDirection.LeftToRight;
    control.IsDirectionReversed = false;
    control.IsEnabled = !Equals(Get(props, "disabled"), true);
    control.Width = Size(Get(props, "width"));
    control.Margin = Thickness(Get(props, "margin"));
    control.HorizontalAlignment = HorizontalAlignment.Stretch;
    var step = Number(Get(props, "step"), 0);
    if (step > 0) { control.TickFrequency = step; control.IsSnapToTickEnabled = true; }
    sliderCallbacks[control] = Get(props, "onValueChange") as ScriptObject;
    if (!subscribedSliders.Contains(control))
    {
      subscribedSliders.Add(control);
      control.ValueChanged += (_, e) =>
      {
        try
        {
          sliderCallbacks.TryGetValue(control, out var callback);
          callback?.Invoke(false, e.NewValue);
        }
        catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter slider error", MessageBoxButton.OK, MessageBoxImage.Error); }
      };
    }
    return control;
  }

  static ProgressBar RenderProgressBar(ScriptObject? props) => new()
  { Value = Number(Get(props, "value"), 0), Minimum = Number(Get(props, "minimum"), 0), Maximum = Number(Get(props, "maximum"), 100), Width = Size(Get(props, "width")), Height = Size(Get(props, "height")), Foreground = Color(Get(props, "color"), Brushes.DodgerBlue), Margin = Thickness(Get(props, "margin")) };

  static CheckBox RenderCheckBox(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var control = key is not null && keyedCheckBoxes.TryGetValue(key, out var existing) ? existing : new CheckBox();
    if (key is not null) keyedCheckBoxes[key] = control;
    control.Content = Get(props, "label")?.ToString();
    return RenderToggle(props, control);
  }
  static RadioButton RenderRadioButton(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var control = key is not null && keyedRadioButtons.TryGetValue(key, out var existing) ? existing : new RadioButton();
    if (key is not null) keyedRadioButtons[key] = control;
    control.Content = Get(props, "label")?.ToString();
    return RenderToggle(props, control);
  }
  static T RenderToggle<T>(ScriptObject? props, T control) where T : System.Windows.Controls.Primitives.ToggleButton
  {
    control.IsChecked = Equals(Get(props, "value") ?? Get(props, "defaultValue"), true); control.IsEnabled = !Equals(Get(props, "disabled"), true); control.Margin = Thickness(Get(props, "margin"));
    if (Get(props, "onValueChange") is ScriptObject callback) control.Checked += (_, _) => { try { callback.Invoke(false, true); } catch { } };
    if (Get(props, "onValueChange") is ScriptObject uncheckedCallback) control.Unchecked += (_, _) => { try { uncheckedCallback.Invoke(false, false); } catch { } };
    return control;
  }

  static DatePicker RenderDatePicker(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var picker = key is not null && keyedDatePickers.TryGetValue(key, out var existing) ? existing : new DatePicker();
    if (key is not null) keyedDatePickers[key] = picker;
    picker.IsEnabled = !Equals(Get(props, "disabled"), true); picker.Width = Size(Get(props, "width")); picker.Margin = Thickness(Get(props, "margin"));
    if (DateTime.TryParse(Get(props, "value")?.ToString(), out var value)) picker.SelectedDate = value;
    if (Get(props, "onChange") is ScriptObject callback) picker.SelectedDateChanged += (_, _) => { try { callback.Invoke(false, picker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty); } catch { } };
    return picker;
  }

  static System.Windows.Controls.Image RenderImage(ScriptObject? props)
  {
    var source = Get(props, "src") ?? Get(props, "source");
    var uri = source is ScriptObject sourceObject ? Get(sourceObject, "uri")?.ToString() : source?.ToString();
    var fit = Get(props, "objectFit")?.ToString() ?? Get(props, "resizeMode")?.ToString();
    var image = new System.Windows.Controls.Image
    {
      Width = Size(Get(props, "width")),
      Height = Size(Get(props, "height")),
      Margin = Thickness(Get(props, "margin")),
      Stretch = fit switch { "cover" => Stretch.UniformToFill, "fill" or "stretch" => Stretch.Fill, _ => Stretch.Uniform },
      ToolTip = Get(props, "alt")?.ToString()
    };
    try
    {
      var imageUri = ResolveImageUri(uri);
      if (imageUri is not null)
      {
        var bitmap = new BitmapImage();
        bitmap.BeginInit(); bitmap.UriSource = imageUri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        image.Source = bitmap;
      }
      else throw new FileNotFoundException("Image source was not found.", uri);
    }
    catch (Exception error)
    {
      if (Get(props, "onError") is ScriptObject callback) { try { callback.Invoke(false, error.Message); } catch { } }
    }
    return image;
  }
  // Simpan instance LibVLC secara global di class agar tidak dibuat berulang-ulang
  static LibVLC? _libVLCInstance;
  static LibVLC GetLibVLC()
  {
    if (_libVLCInstance == null)
    {
      Core.Initialize();
      _libVLCInstance = new LibVLC();
    }
    return _libVLCInstance;
  }

  static FrameworkElement RenderVideo(ScriptObject? props)
  {
    if (props == null) return new FrameworkElement();

    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var source = Get(props, "src") ?? Get(props, "source");
    var uriString = source is ScriptObject sourceObject ? Get(sourceObject, "uri")?.ToString() : source?.ToString();

    double width = Size(Get(props, "width"));
    double height = Size(Get(props, "height"));

    var libVLC = GetLibVLC();
    var mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVLC)
    {
      EnableHardwareDecoding = false
    };


    if (key is not null)
    {
      keyedMediaPlayers[key] = mediaPlayer;
    }

    mediaPlayer.Volume = 100;
    mediaPlayer.Mute = false;
    var videoView = new VideoView()
    {
      Width = double.IsNaN(width) ? 300 : width,
      Height = double.IsNaN(height) ? 200 : height,
      Margin = Thickness(Get(props, "margin")),
      MediaPlayer = mediaPlayer
    };

    try
    {
      if (!string.IsNullOrEmpty(uriString))
      {
        Uri? mediaUri = null;

        // 1. Tangani Shared Folder (UNC Path seperti \\server\folder\file.mp4) atau Absolute Path
        if (uriString.StartsWith(@"\\") || uriString.StartsWith("//"))
        {
          // Konversi UNC path menjadi format Uri jaringan (file://...)
          mediaUri = new Uri(uriString.Replace("\\", "/"));
        }
        else
        {
          // Coba resolusi file lokal / relatif terlebih dahulu
          mediaUri = ResolveImageUri(uriString);
        }

        // 2. Jika bukan file lokal/shared folder (misal: Link YouTube atau URL HTTP biasa), gunakan Uri langsung
        if (mediaUri == null)
        {
          Uri.TryCreate(uriString, UriKind.Absolute, out mediaUri);
        }

        videoView.Loaded += (_, _) =>
        {
          Task.Run(() =>
                {
                  try
                  {
                    // Jika ini adalah URL YouTube atau web stream, LibVLC menangani string URL langsung via Media
                    using var media = mediaUri != null && mediaUri.IsFile
                              ? new Media(libVLC, mediaUri)
                              : new Media(libVLC, uriString, FromType.FromLocation);

                    mediaPlayer.Media = media;

                    if (Equals(Get(props, "autoPlay"), true) || Equals(Get(props, "autoplay"), true))
                    {
                      mediaPlayer.Play();
                    }
                  }
                  catch (Exception ex)
                  {
                    Application.Current.Dispatcher.Invoke(() =>
                          {
                            MessageBox.Show($"LibVLC Play Error: {ex.Message}", "Jupiter Video Error", MessageBoxButton.OK, MessageBoxImage.Error);
                          });
                  }
                });
        };

        if (Get(props, "onProgress") is ScriptObject progressCallback)
        {
          mediaPlayer.TimeChanged += (_, e) =>
          {
            Application.Current.Dispatcher.Invoke(() =>
                    {
                      try { progressCallback.Invoke(false, e.Time, mediaPlayer.Length); } catch { }
                    });
          };
        }

        videoView.Unloaded += (_, _) =>
        {
          if (key is not null) keyedMediaPlayers.Remove(key);
          mediaPlayer.Stop();
          mediaPlayer.Dispose();
        };
      }
    }
    catch (Exception error)
    {
      if (Get(props, "onError") is ScriptObject callback)
      {
        try { callback.Invoke(false, error.Message); } catch { }
      }
    }

    return videoView;
  }

  static Uri? ResolveImageUri(string? source)
  {
    if (string.IsNullOrWhiteSpace(source)) return null;
    if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
    {
      if (absoluteUri.IsFile && !File.Exists(absoluteUri.LocalPath)) return null;
      return absoluteUri;
    }
    if (Path.IsPathRooted(source)) return File.Exists(source) ? new Uri(source) : null;
    var locations = new[] { new DirectoryInfo(AppContext.BaseDirectory), new DirectoryInfo(Directory.GetCurrentDirectory()) };
    foreach (var location in locations)
    {
      var current = location;
      while (current is not null)
      {
        var candidate = Path.Combine(current.FullName, source);
        if (File.Exists(candidate)) return new Uri(candidate);
        current = current.Parent;
      }
    }
    return null;
  }

  static TextBox RenderTextInput(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var box = key is not null && keyedTextInputs.TryGetValue(key, out var keyedBox) ? keyedBox : new TextBox();
    if (key is not null) keyedTextInputs[key] = box;
    var value = Get(props, "value")?.ToString() ?? Get(props, "defaultValue")?.ToString() ?? string.Empty;
    if (box.Text != value)
    {
      syncingTextInputs.Add(box);
      try { box.Text = value; }
      finally { syncingTextInputs.Remove(box); }
    }
    box.ToolTip = Get(props, "placeholder")?.ToString(); box.IsEnabled = !Equals(Get(props, "disabled"), true); box.Margin = Thickness(Get(props, "margin"));
    box.Width = Size(Get(props, "width")); box.Height = Size(Get(props, "height")); box.IsReadOnly = Equals(Get(props, "editable"), false); box.AcceptsReturn = Equals(Get(props, "multiline"), true); box.MaxLength = Convert.ToInt32(Get(props, "maxLength") ?? 0); if (Equals(Get(props, "secureTextEntry"), true)) box.ToolTip = "Secure text entry";
    if (key is not null) box.Tag = key;
    textInputCallbacks[box] = Get(props, "onChangeText") as ScriptObject;
    textInputSubmitCallbacks[box] = Get(props, "onSubmitEditing") as ScriptObject;
    textInputFocusCallbacks[box] = Get(props, "onFocus") as ScriptObject;
    textInputBlurCallbacks[box] = Get(props, "onBlur") as ScriptObject;
    if (!subscribedTextInputs.Contains(box))
    {
      subscribedTextInputs.Add(box);
      box.TextChanged += (_, _) => { if (syncingTextInputs.Contains(box)) return; try { textInputCallbacks.TryGetValue(box, out var activeCallback); activeCallback?.Invoke(false, box.Text); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter input error", MessageBoxButton.OK, MessageBoxImage.Error); } };
      box.KeyDown += (_, args) => { if (args.Key == Key.Enter && !box.AcceptsReturn) { try { textInputSubmitCallbacks.TryGetValue(box, out var activeCallback); activeCallback?.Invoke(false, box.Text); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter input submit error", MessageBoxButton.OK, MessageBoxImage.Error); } args.Handled = true; } };
      box.GotKeyboardFocus += (_, _) => { try { textInputFocusCallbacks.TryGetValue(box, out var activeCallback); activeCallback?.Invoke(false); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter input focus error", MessageBoxButton.OK, MessageBoxImage.Error); } };
      box.LostKeyboardFocus += (_, _) => { try { textInputBlurCallbacks.TryGetValue(box, out var activeCallback); activeCallback?.Invoke(false); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter input blur error", MessageBoxButton.OK, MessageBoxImage.Error); } };
    }
    if (Equals(Get(props, "autoFocus"), true) && autoFocusedTextInputs.Add(box)) box.Dispatcher.BeginInvoke(() => { if (!box.IsKeyboardFocusWithin) Keyboard.Focus(box); }, System.Windows.Threading.DispatcherPriority.ContextIdle);
    return box;
  }

  static PasswordBox RenderPasswordInput(ScriptObject? props)
  {
    var key = Get(props, "id")?.ToString() ?? Get(props, "testID")?.ToString();
    var box = key is not null && keyedPasswordInputs.TryGetValue(key, out var keyedBox) ? keyedBox : new PasswordBox();
    if (key is not null) keyedPasswordInputs[key] = box;
    var value = Get(props, "value")?.ToString() ?? Get(props, "defaultValue")?.ToString() ?? string.Empty;
    if (box.Password != value)
    {
      syncingPasswordInputs.Add(box);
      try { box.Password = value; }
      finally { syncingPasswordInputs.Remove(box); }
    }
    box.ToolTip = Get(props, "placeholder")?.ToString(); box.IsEnabled = !Equals(Get(props, "disabled"), true); box.IsEnabled = !Equals(Get(props, "editable"), false) && box.IsEnabled; box.Margin = Thickness(Get(props, "margin"));
    box.Width = Size(Get(props, "width")); box.Height = Size(Get(props, "height"));
    passwordInputCallbacks[box] = Get(props, "onChangeText") as ScriptObject;
    if (!subscribedPasswordInputs.Contains(box))
    {
      subscribedPasswordInputs.Add(box);
      box.PasswordChanged += (_, _) => { if (syncingPasswordInputs.Contains(box)) return; try { passwordInputCallbacks.TryGetValue(box, out var callback); callback?.Invoke(false, box.Password); } catch (Exception error) { MessageBox.Show(error.ToString(), "Jupiter password input error", MessageBoxButton.OK, MessageBoxImage.Error); } };
    }
    return box;
  }

  static void AddChildren(Panel panel, object? children, V8ScriptEngine engine, Orientation orientation, double gap)
  {
    if (TryGetItems(children, out var items)) foreach (var child in items) { var element = Render(child, engine); if (gap > 0 && element is FrameworkElement item) { var m = item.Margin; item.Margin = orientation == Orientation.Horizontal ? new Thickness(m.Left + gap, m.Top, m.Right, m.Bottom) : new Thickness(m.Left, m.Top + gap, m.Right, m.Bottom); } panel.Children.Add(element); }
    else if (children is not null) panel.Children.Add(Render(children, engine));
  }

  static List<object?> ChildValues(object? children)
  {
    if (TryGetItems(children, out var items)) return items.ToList();
    return children is null ? new List<object?>() : new List<object?> { children };
  }

  static bool IsScrollViewNode(object? value) => value is ScriptObject node && node.GetProperty("type")?.ToString() == "ScrollView";
  static bool IsModalNode(object? value) => value is ScriptObject node && node.GetProperty("type")?.ToString() == "Modal";

  static bool TryGetItems(object? value, out IEnumerable<object?> items)
  {
    if (value is ScriptObject script) { var lengthValue = script.GetProperty("length"); if (lengthValue is not null and not Undefined) { var length = Convert.ToInt32(lengthValue); items = Enumerable.Range(0, length).Select(index => script.GetProperty(index.ToString())); return true; } items = Array.Empty<object?>(); return false; }
    if (value is IEnumerable list && value is not string) { items = list.Cast<object?>(); return true; }
    items = Array.Empty<object?>(); return false;
  }

  static object? Get(ScriptObject? props, string name)
  {
    var value = props?.GetProperty(name);
    if (value is not null and not Undefined) return value;
    if (props?.GetProperty("style") is ScriptObject style)
    {
      var styledValue = style.GetProperty(name);
      if (styledValue is not null and not Undefined) return styledValue;
    }
    return null;
  }
  static double Number(object? value, double fallback) => value is null or Undefined ? fallback : Convert.ToDouble(value);
  static double Size(object? value) => value is null or Undefined ? double.NaN : Convert.ToDouble(value);
  static double IndicatorSize(object? value) => value?.ToString() switch { "small" => 16, "large" => 32, null => 24, _ => Number(value, 24) };
  static Thickness Thickness(object? value) => value is null or Undefined ? new Thickness(0) : new Thickness(Convert.ToDouble(value));
  static HorizontalAlignment Alignment(object? value) => value?.ToString() switch { "center" => HorizontalAlignment.Center, "right" or "flex-end" => HorizontalAlignment.Right, "left" or "flex-start" => HorizontalAlignment.Left, _ => HorizontalAlignment.Stretch };
  static FontWeight FontWeight(object? value) => value?.ToString() == "bold" ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
  static System.Windows.TextAlignment TextAlignment(object? value) => value?.ToString() switch { "center" => System.Windows.TextAlignment.Center, "right" => System.Windows.TextAlignment.Right, "justify" => System.Windows.TextAlignment.Justify, _ => System.Windows.TextAlignment.Left };
  static Brush Color(object? value, Brush fallback) { if (value is null) return fallback; try { return (Brush)new BrushConverter().ConvertFromString(value.ToString()!)!; } catch { return fallback; } }
  static string TextValue(object? value) { if (TryGetItems(value, out var items)) return string.Concat(items.Select(TextValue)); return value?.ToString() ?? string.Empty; }

  static HorizontalAlignment GetHorizontalAlignment(
    ScriptObject? props)
  {
    return Get(props, "alignItems")?.ToString() switch
    {
      "start" => HorizontalAlignment.Left,
      "center" => HorizontalAlignment.Center,
      "end" => HorizontalAlignment.Right,
      "stretch" => HorizontalAlignment.Stretch,
      _ => HorizontalAlignment.Stretch
    };
  }

  static VerticalAlignment GetVerticalAlignment(
      ScriptObject? props)
  {
    return Get(props, "alignItems")?.ToString() switch
    {
      "start" => VerticalAlignment.Top,
      "center" => VerticalAlignment.Center,
      "end" => VerticalAlignment.Bottom,
      "stretch" => VerticalAlignment.Stretch,
      _ => VerticalAlignment.Stretch
    };
  }
}
