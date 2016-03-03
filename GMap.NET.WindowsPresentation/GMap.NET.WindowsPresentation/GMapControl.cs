using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GMap.NET.Internals;
using System.Diagnostics;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using GMap.NET.WindowsPresentation.HelpersAndUtils;
using GMap.NET.WindowsPresentation.Interfaces;

namespace GMap.NET.WindowsPresentation
{
   /// <summary>
   /// GMap.NET control for Windows Presentation
   /// </summary>
   public partial class GMapControl : ItemsControl, Interface, IDisposable
   {
      public delegate void SelectionChange(RectLatLng Selection, bool ZoomToFit);
      private delegate void MethodInvoker();

      #region Fields

      private static DataTemplate DataTemplateInstance;
      private static ItemsPanelTemplate ItemsPanelTemplateInstance;
      private static Style StyleInstance;
      
      private readonly Core _core = new Core();
      private PointLatLng _selectionStart;
      private PointLatLng _selectionEnd;
      private Typeface _tileTypeface = new Typeface("Arial");
      private bool _showTileGridLines = false;
      private FormattedText _copyright;
      private ScaleModesEnum _scaleModeEnum = ScaleModesEnum.Integer;
      private readonly ScaleTransform _lastScaleTransform = new ScaleTransform();
      private Application _loadedApp;
      private Canvas _mapCanvas = null;
      /// <summary>
      /// current selected area in map
      /// </summary>
      private RectLatLng _selectedArea;
      private RectLatLng? _lazySetZoomToFitRect = null;
      private bool _lazyEvents = true;
      private readonly RotateTransform _rotationMatrix = new RotateTransform();
      private GeneralTransform _rotationMatrixInvert = new RotateTransform();
      private bool _renderHelperLine = false;
      private bool _isSelected = false;
      private int _onMouseUpTimestamp = 0;
      private Cursor _cursorBefore = Cursors.Arrow;
#if DEBUG
      private readonly Pen _virtualCenterCrossPen = new Pen(Brushes.Blue, 1);
#endif
      private HelperLineOptionsEnum _helperLineOptionEnum = HelperLineOptionsEnum.DontShow;

      public Pen HelperLinePen = new Pen(Brushes.Blue, 1);
      public Brush EmptyMapBackground = Brushes.WhiteSmoke;
      public Pen CenterCrossPen = new Pen(Brushes.Red, 1);
      public bool ShowCenter = true;

      /// <summary>
      /// if true, selects area just by holding mouse and moving
      /// </summary>
      public bool DisableAltForSelection = false;

      /// <summary>
      /// pen for empty tile borders
      /// </summary>
      public Pen EmptyTileBorders = new Pen(Brushes.White, 1.0);

      /// <summary>
      /// pen for Selection
      /// </summary>
      public Pen SelectionPen = new Pen(Brushes.Blue, 2.0);

      /// <summary>
      /// background of selected area
      /// </summary>
      public Brush SelectedAreaFill =
         new SolidColorBrush(Color.FromArgb(33, Colors.RoyalBlue.R, Colors.RoyalBlue.G, Colors.RoyalBlue.B));

      /// <summary>
      /// /// <summary>
      /// pen for empty tile background
      /// </summary>
      public Brush EmptytileBrush = Brushes.Navy;

      /// <summary>
      /// text on empty tiles
      /// </summary>
      public FormattedText EmptyTileText =
         new FormattedText("We are sorry, but we don't\nhave imagery at this zoom\n     level for this region.",
            System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Arial"), 16,
            Brushes.Blue);

      /// <summary>
      /// map dragg button
      /// </summary>
      [Category("GMap.NET")]
      public MouseButton DragButton = MouseButton.Right;

      /// <summary>
      /// Figure for selection
      /// </summary>
      public GMapSelectionFigureEnum SelectionFigure = GMapSelectionFigureEnum.Rectangle;

      /// <summary>
      /// is touch control enabled
      /// </summary>
      public bool TouchEnabled = true;

      /// <summary>
      /// map boundaries
      /// </summary>
      public RectLatLng? BoundsOfMap = null;

      /// <summary>
      /// current markers overlay offset
      /// </summary>
      internal readonly TranslateTransform MapTranslateTransform = new TranslateTransform();
      internal readonly TranslateTransform MapOverlayTranslateTransform = new TranslateTransform();
      internal ScaleTransform MapScaleTransform = new ScaleTransform();
      internal RotateTransform MapRotateTransform = new RotateTransform();

      #endregion

      #region Properties

      #region DependencyProperties and related stuff

      /// <summary>
      /// Reverses MouseWheel zooming direction
      /// </summary>
      public static readonly DependencyProperty InvertedMouseWheelZoomingProperty = DependencyProperty.Register(
         "InvertedMouseWheelZooming", typeof(bool), typeof(GMapControl), new PropertyMetadata(false));

      public bool InvertedMouseWheelZooming
      {
         get { return (bool)GetValue(InvertedMouseWheelZoomingProperty); }
         set { SetValue(InvertedMouseWheelZoomingProperty, value); }
      }

      /// <summary>
      /// Lets you zoom by MouseWheel even when pointer is in area of marker
      /// </summary>
      public static readonly DependencyProperty IgnoreMarkerOnMouseWheelProperty = DependencyProperty.Register(
         "IgnoreMarkerOnMouseWheel", typeof(bool), typeof(GMapControl), new PropertyMetadata(false));

      public bool IgnoreMarkerOnMouseWheel
      {
         get { return (bool)GetValue(IgnoreMarkerOnMouseWheelProperty); }
         set { SetValue(IgnoreMarkerOnMouseWheelProperty, value); }
      }

      public System.Windows.Point MapPoint
      {
         get { return (System.Windows.Point) GetValue(MapPointProperty); }
         set { SetValue(MapPointProperty, value); }
      }
      
      // Using a DependencyProperty as the backing store for point.  This enables animation, styling, binding, etc...
      public static readonly DependencyProperty MapPointProperty =
         DependencyProperty.Register("MapPoint", typeof (System.Windows.Point), typeof (GMapControl),
            new PropertyMetadata(new Point(), OnMapPointPropertyChanged));
      
      private static void OnMapPointPropertyChanged(DependencyObject source,
         DependencyPropertyChangedEventArgs e)
      {
         Point temp = (Point) e.NewValue;
         (source as GMapControl).Position = new PointLatLng(temp.X, temp.Y);
      }

      public static readonly DependencyProperty MapProviderProperty = DependencyProperty.Register("MapProvider",
         typeof (GMapProvider), typeof (GMapControl),
         new UIPropertyMetadata(EmptyProvider.Instance, new PropertyChangedCallback(MapProviderPropertyChanged)));

      private static readonly DependencyPropertyKey OverlaysKey
         = DependencyProperty.RegisterReadOnly("Overlays", typeof (ObservableCollection<GMapOverlay>),
            typeof (GMapControl),
            new FrameworkPropertyMetadata(null,
               FrameworkPropertyMetadataOptions.None,
               OnOverlaysPropChanged));

      public static readonly DependencyProperty OverlaysKeyProperty
         = OverlaysKey.DependencyProperty;

      /// <summary>
      /// Collection of overlays
      /// </summary>
      public ObservableCollection<GMapOverlay> Overlays
      {
         get { return (ObservableCollection<GMapOverlay>)GetValue(OverlaysKeyProperty); }
         private set { SetValue(OverlaysKey, value); }
      }

      private static void OnOverlaysPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         ((GMapControl)d).OnOverlaysPropChanged(e);
      }

      private void OnOverlaysPropChanged(DependencyPropertyChangedEventArgs e)
      {
      }

      /// <summary>
      /// type of map
      /// </summary>
      [Browsable(false)]
      public GMapProvider MapProvider
      {
         get { return GetValue(MapProviderProperty) as GMapProvider; }
         set { SetValue(MapProviderProperty, value); }
      }

      private static void MapProviderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         GMapControl map = (GMapControl) d;
         if (map != null && e.NewValue != null)
         {
            Debug.WriteLine("MapType: " + e.OldValue + " -> " + e.NewValue);

            RectLatLng viewarea = map.SelectedArea;
            if (viewarea != RectLatLng.Empty)
            {
               map.Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat/2,
                  viewarea.Lng + viewarea.WidthLng/2);
            }
            else
            {
               viewarea = map.ViewArea;
            }

            map._core.Provider = e.NewValue as GMapProvider;

            map._copyright = null;
            if (!string.IsNullOrEmpty(map._core.Provider.Copyright))
            {
               map._copyright = new FormattedText(map._core.Provider.Copyright, CultureInfo.CurrentUICulture,
                  FlowDirection.LeftToRight, new Typeface("GenericSansSerif"), 9, Brushes.Navy);
            }

            if (map._core.IsStarted && map._core.zoomToArea)
            {
               // restore zoomrect as close as possible
               if (viewarea != RectLatLng.Empty && viewarea != map.ViewArea)
               {
                  int bestZoom = map._core.GetMaxZoomToFitRect(viewarea);
                  if (bestZoom > 0 && map.Zoom != bestZoom)
                  {
                     map.Zoom = bestZoom;
                  }
               }
            }
         }
      }

      public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof (double),
         typeof (GMapControl),
         new UIPropertyMetadata(0.0, new PropertyChangedCallback(ZoomPropertyChanged),
            new CoerceValueCallback(OnCoerceZoom)));

      /// <summary>
      /// map zoom
      /// </summary>
      [Category("GMap.NET")]
      public double Zoom
      {
         get { return (double) (GetValue(ZoomProperty)); }
         set { SetValue(ZoomProperty, value); }
      }

      private static object OnCoerceZoom(DependencyObject o, object value)
      {
         GMapControl map = o as GMapControl;
         if (map != null)
         {
            double result = (double) value;
            if (result > map.MaxZoom)
            {
               result = map.MaxZoom;
            }
            if (result < map.MinZoom)
            {
               result = map.MinZoom;
            }

            return result;
         }
         else
         {
            return value;
         }
      }

      /// <summary>
      /// Specifies, if a floating map scale is displayed using a 
      /// stretched, or a narrowed map.
      /// If <code>_scaleModeEnum</code> is <code>ScaleDown</code>,
      /// then a scale of 12.3 is displayed using a map zoom level of 13
      /// resized to the lower level. If the parameter is <code>ScaleUp</code> ,
      /// then the same scale is displayed using a zoom level of 12 with an
      /// enlarged scale. If the value is <code>Dynamic</code>, then until a
      /// remainder of 0.25 <code>ScaleUp</code> is applied, for bigger
      /// remainders <code>ScaleDown</code>.
      /// </summary>
      [Category("GMap.NET")]
      [Description("map scale type")]
      public ScaleModesEnum ScaleModeEnum
      {
         get { return _scaleModeEnum; }
         set
         {
            _scaleModeEnum = value;
            InvalidateVisual();
         }
      }

      private static void ZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         GMapControl map = (GMapControl) d;
         if (map != null && map.MapProvider.Projection != null)
         {
            double value = (double) e.NewValue;

            Debug.WriteLine("Zoom: " + e.OldValue + " -> " + value);

            double remainder = value%1;
            if (map.ScaleModeEnum != ScaleModesEnum.Integer && remainder != 0 && map.ActualWidth > 0)
            {
               bool scaleDown;

               switch (map.ScaleModeEnum)
               {
                  case ScaleModesEnum.ScaleDown:
                     scaleDown = true;
                     break;

                  case ScaleModesEnum.Dynamic:
                     scaleDown = remainder > 0.25;
                     break;

                  default:
                     scaleDown = false;
                     break;
               }

               if (scaleDown)
                  remainder--;

               double scaleValue = Math.Pow(2d, remainder);
               {
                  if (map.MapScaleTransform == null)
                  {
                     map.MapScaleTransform = map._lastScaleTransform;
                  }
                  map.MapScaleTransform.ScaleX = scaleValue;
                  map.MapScaleTransform.ScaleY = scaleValue;

                  map._core.scaleX = 1/scaleValue;
                  map._core.scaleY = 1/scaleValue;

                  map.MapScaleTransform.CenterX = map.ActualWidth/2;
                  map.MapScaleTransform.CenterY = map.ActualHeight/2;
               }

               map._core.Zoom = Convert.ToInt32(scaleDown ? Math.Ceiling(value) : value - remainder);
            }
            else
            {
               map.MapScaleTransform = null;
               map._core.scaleX = 1;
               map._core.scaleY = 1;
               map._core.Zoom = (int) Math.Floor(value);
            }

            if (map.IsLoaded)
            {
               map.ValidateLayerMinMaxZoom();
               map.ForceUpdateOverlays();
               map.InvalidateVisual(true);
            }
         }  
      }

      private void ValidateLayerMinMaxZoom()
      {
         foreach (var gMapOverlay in Overlays)
         {
            if (!gMapOverlay.IsActive)
               continue;
            Debug.WriteLine(
               $"Overlay name={gMapOverlay.OverlayName}, HidenByZoomValidation={gMapOverlay.HidenByZoomValidation}");
            if (gMapOverlay.HidenByZoomValidation)
            {
               gMapOverlay.HideLayerMarkers();
            }
            else
            {
               gMapOverlay.ShowLayerMarkers();
            }
         }
      }

      #endregion

      /// <summary>
      /// enables filling empty tiles using lower level images
      /// </summary>
      [Browsable(false)]
      public bool FillEmptyTiles
      {
         get { return _core.fillEmptyTiles; }
         set { _core.fillEmptyTiles = value; }
      }

      public PointLatLng SelectionStart => _selectionStart;
      public PointLatLng SelectionEnd => _selectionEnd;

      /// <summary>
      /// max zoom
      /// </summary>         
      [Category("GMap.NET")]
      [Description("maximum zoom level of map")]
      public int MaxZoom
      {
         get { return _core.maxZoom; }
         set { _core.maxZoom = value; }
      }

      /// <summary>
      /// min zoom
      /// </summary>      
      [Category("GMap.NET")]
      [Description("minimum zoom level of map")]
      public int MinZoom
      {
         get { return _core.minZoom; }
         set { _core.minZoom = value; }
      }

      /// <summary>
      /// draw lines at the mouse pointer position
      /// </summary>
      [Browsable(false)]
      public HelperLineOptionsEnum HelperLineOptionEnum
      {
         get { return _helperLineOptionEnum; }
         set
         {
            _helperLineOptionEnum = value;
            _renderHelperLine = (_helperLineOptionEnum == HelperLineOptionsEnum.ShowAlways);
            if (_core.IsStarted)
            {
               InvalidateVisual();
            }
         }
      }

      /// <summary>
      /// map zooming type for mouse wheel
      /// </summary>
      [Category("GMap.NET")]
      [Description("map zooming type for mouse wheel")]
      public MouseWheelZoomType MouseWheelZoomType
      {
         get { return _core.MouseWheelZoomType; }
         set { _core.MouseWheelZoomType = value; }
      }

      /// <summary>
      /// enable map zoom on mouse wheel
      /// </summary>
      [Category("GMap.NET")]
      [Description("enable map zoom on mouse wheel")]
      public bool MouseWheelZoomEnabled
      {
         get { return _core.MouseWheelZoomEnabled; }
         set { _core.MouseWheelZoomEnabled = value; }
      }
      
      /// <summary>
      /// shows tile gridlines
      /// </summary>
      [Category("GMap.NET")]
      public bool ShowTileGridLines
      {
         get { return _showTileGridLines; }
         set
         {
            _showTileGridLines = value;
            InvalidateVisual();
         }
      }

      /// <summary>
      /// retry count to get tile 
      /// </summary>
      [Browsable(false)]
      public int RetryLoadTile
      {
         get { return _core.RetryLoadTile; }
         set { _core.RetryLoadTile = value; }
      }

      /// <summary>
      /// how many levels of tiles are staying decompresed in memory
      /// </summary>
      [Browsable(false)]
      public int LevelsKeepInMemmory
      {
         get { return _core.LevelsKeepInMemmory; }
         set { _core.LevelsKeepInMemmory = value; }
      }

      [Browsable(false)]
      public RectLatLng SelectedArea
      {
         get { return _selectedArea; }
         set
         {
            _selectedArea = value;
            InvalidateVisual();
         }
      }

      /// <summary>
      /// bearing for rotation of the map
      /// </summary>
      [Category("GMap.NET")]
      public float Bearing
      {
         get { return _core.bearing; }
         set
         {
            if (_core.bearing != value)
            {
               bool resize = _core.bearing == 0;
               _core.bearing = value;

               UpdateRotationMatrix();

               if (value != 0 && value % 360 != 0)
               {
                  _core.IsRotated = true;

                  if (_core.tileRectBearing.Size == _core.tileRect.Size)
                  {
                     _core.tileRectBearing = _core.tileRect;
                     _core.tileRectBearing.Inflate(1, 1);
                  }
               }
               else
               {
                  _core.IsRotated = false;
                  _core.tileRectBearing = _core.tileRect;
               }

               if (resize)
               {
                  _core.OnMapSizeChanged((int)ActualWidth, (int)ActualHeight);
               }

               if (_core.IsStarted)
               {
                  ForceUpdateOverlays();
               }
            }
         }
      }

      /// <summary>
      /// returs true if map bearing is not zero
      /// </summary>         
      public bool IsRotated => _core.IsRotated;

      /// <summary>
      /// occurs when mouse selection is changed
      /// </summary>        
      public event SelectionChange OnSelectionChange;

      private static readonly DependencyPropertyKey MarkersKey
         = DependencyProperty.RegisterReadOnly("Markers", typeof (ObservableCollection<GMapMarker>),
            typeof (GMapControl),
            new FrameworkPropertyMetadata(null,
               FrameworkPropertyMetadataOptions.None,
               OnMarkersPropChanged));

      public static readonly DependencyProperty MarkersProperty
         = MarkersKey.DependencyProperty;

      /// <summary>
      /// List of markers
      /// </summary>
      public ObservableCollection<GMapMarker> Markers
      {
         get { return (ObservableCollection<GMapMarker>) GetValue(MarkersProperty); }
         private set { SetValue(MarkersKey, value); }
      }

      private static void OnMarkersPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
      {
         ((GMapControl) d).OnMarkersPropChanged(e);
      }

      private void OnMarkersPropChanged(DependencyPropertyChangedEventArgs e)
      {

      }
      
      protected bool DesignModeInConstruct => System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
      
      /// <summary>
      /// markers overlay
      /// </summary>
      internal Canvas MapCanvas
      {
         get
         {
            if (_mapCanvas == null)
            {
               if (this.VisualChildrenCount > 0)
               {
                  Border border = VisualTreeHelper.GetChild(this, 0) as Border;
                  ItemsPresenter items = border.Child as ItemsPresenter;
                  DependencyObject target = VisualTreeHelper.GetChild(items, 0);
                  _mapCanvas = target as Canvas;

                  _mapCanvas.RenderTransform = MapTranslateTransform;
               }
            }

            return _mapCanvas;
         }
      }

      public GMaps Manager => GMaps.Instance;

      #endregion Properties

      public GMapControl()
      {
         if (!DesignModeInConstruct)
         {
            #region -- templates --

            #region -- xaml --

            //  <ItemsControl Name="figures">
            //    <ItemsControl.ItemTemplate>
            //        <DataTemplate>
            //            <ContentPresenter Content="{Binding Path=Shape}" />
            //        </DataTemplate>
            //    </ItemsControl.ItemTemplate>
            //    <ItemsControl.ItemsPanel>
            //        <ItemsPanelTemplate>
            //            <Canvas />
            //        </ItemsPanelTemplate>
            //    </ItemsControl.ItemsPanel>
            //    <ItemsControl.ItemContainerStyle>
            //        <Style>
            //            <Setter Property="Canvas.Left" Value="{Binding Path=LocalPositionX}"/>
            //            <Setter Property="Canvas.Top" Value="{Binding Path=LocalPositionY}"/>
            //        </Style>
            //    </ItemsControl.ItemContainerStyle>
            //</ItemsControl> 

            #endregion

            if (DataTemplateInstance == null)
            {
               DataTemplateInstance = new DataTemplate(typeof (GMapMarker));
               {
                  FrameworkElementFactory fef = new FrameworkElementFactory(typeof (ContentPresenter));
                  fef.SetBinding(ContentPresenter.ContentProperty, new Binding("Shape"));
                  DataTemplateInstance.VisualTree = fef;
               }
            }
            ItemTemplate = DataTemplateInstance;

            if (ItemsPanelTemplateInstance == null)
            {
               var factoryPanel = new FrameworkElementFactory(typeof (Canvas));
               {
                  factoryPanel.SetValue(Canvas.IsItemsHostProperty, true);

                  ItemsPanelTemplateInstance = new ItemsPanelTemplate();
                  {
                     ItemsPanelTemplateInstance.VisualTree = factoryPanel;
                  }
               }
            }
            ItemsPanel = ItemsPanelTemplateInstance;

            if (StyleInstance == null)
            {
               StyleInstance = new Style();
               {
                  StyleInstance.Setters.Add(new Setter(Canvas.LeftProperty, new Binding("LocalPositionX")));
                  StyleInstance.Setters.Add(new Setter(Canvas.TopProperty, new Binding("LocalPositionY")));
                  StyleInstance.Setters.Add(new Setter(Canvas.ZIndexProperty, new Binding("ZIndex")));
               }
            }
            ItemContainerStyle = StyleInstance;

            #endregion

            Markers = new ObservableCollection<GMapMarker>();
            Overlays = new ObservableCollection<GMapOverlay>();

            ClipToBounds = true;
            SnapsToDevicePixels = true;

            _core.SystemType = "WindowsPresentation";

            _core.RenderMode = GMap.NET.RenderMode.WPF;

            Overlays.CollectionChanged += OverlaysOnCollectionChanged;
            _core.OnMapZoomChanged += new MapZoomChanged(ForceUpdateOverlays);
            _core.OnCurrentPositionChanged += CoreOnCurrentPositionChanged;
            Loaded += new RoutedEventHandler(GMapControl_Loaded);
            Dispatcher.ShutdownStarted += new EventHandler(Dispatcher_ShutdownStarted);
            SizeChanged += new SizeChangedEventHandler(GMapControl_SizeChanged);

            // by default its internal property, feel free to use your own
            if (ItemsSource == null)
            {
               ItemsSource = Markers;
            }

            _core.Zoom = (int) ((double) ZoomProperty.DefaultMetadata.DefaultValue);
         }
      }

      private void OverlaysOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
      {
         if (eventArgs.OldItems != null)
         {
            foreach (GMapOverlay item in eventArgs.OldItems)
            {
               item.MapControl = null;
               //todo: delete overlay items
            }
         }

         if (eventArgs.NewItems != null)
         {
            foreach (GMapOverlay item in eventArgs.NewItems)
            {
               item.MapControl = this;
            }
         }

         if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
         {
            //todo: delete overlay items
         }
      }

      private void CoreOnCurrentPositionChanged(PointLatLng pointLatLng)
      {
         Position = pointLatLng;
      }

      static GMapControl()
      {
         GMapImageProxy.Enable();
#if !PocketPC
         GMaps.Instance.SQLitePing();
#endif
      }

      void invalidatorEngage(object sender, ProgressChangedEventArgs e)
      {
         base.InvalidateVisual();
      }

      /// <summary>
      /// enque built-in thread safe invalidation
      /// </summary>
      public new void InvalidateVisual()
      {
         if (_core.Refresh != null)
         {
            _core.Refresh.Set();
         }
      }

      /// <summary>
      /// Invalidates the rendering of the element, and forces a complete new layout
      /// pass. System.Windows.UIElement.OnRender(System.Windows.Media.DrawingContext)
      /// is called after the layout cycle is completed. If not forced enques built-in thread safe invalidation
      /// </summary>
      /// <param name="forced"></param>
      public void InvalidateVisual(bool forced)
      {
         if (forced)
         {
            lock (_core.invalidationLock)
            {
               _core.lastInvalidation = DateTime.Now;
            }
            base.InvalidateVisual();
         }
         else
         {
            InvalidateVisual();
         }
      }

      protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
      {
         base.OnItemsChanged(e);

         if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
         {
            ForceUpdateOverlays(e.NewItems);
         }
         else
         {
            InvalidateVisual();
         }
      }

      /// <summary>
      /// inits core system
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void GMapControl_Loaded(object sender, RoutedEventArgs e)
      {
         if (!_core.IsStarted)
         {
            if (_lazyEvents)
            {
               _lazyEvents = false;

               if (_lazySetZoomToFitRect.HasValue)
               {
                  SetZoomToFitRect(_lazySetZoomToFitRect.Value);
                  _lazySetZoomToFitRect = null;
               }
            }
            _core.OnMapOpen().ProgressChanged += new ProgressChangedEventHandler(invalidatorEngage);
            ForceUpdateOverlays();

            if (Application.Current != null)
            {
               _loadedApp = Application.Current;

               _loadedApp.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,
                  new Action(delegate()
                  {
                     _loadedApp.SessionEnding += new SessionEndingCancelEventHandler(Current_SessionEnding);
                  }
                     ));
            }
         }
      }

      void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
      {
         GMaps.Instance.CancelTileCaching();
      }

      void Dispatcher_ShutdownStarted(object sender, EventArgs e)
      {
         Dispose();
      }

      /// <summary>
      /// recalculates size
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void GMapControl_SizeChanged(object sender, SizeChangedEventArgs e)
      {
         System.Windows.Size constraint = e.NewSize;

         // 50px outside control
         //region = new GRect(-50, -50, (int)constraint.Width + 100, (int)constraint.Height + 100);

         _core.OnMapSizeChanged((int) constraint.Width, (int) constraint.Height);

         if (_core.IsStarted)
         {
            if (IsRotated)
            {
               UpdateRotationMatrix();
            }

            ForceUpdateOverlays();
         }
      }

      void ForceUpdateOverlays()
      {
         ForceUpdateOverlays(ItemsSource);
      }

      void ForceUpdateOverlays(System.Collections.IEnumerable items)
      {
         using (Dispatcher.DisableProcessing())
         {
            //Debug.WriteLine($"ForceUpdateOverlays, trace={Environment.StackTrace}");
            //Debug.WriteLine($"ForceUpdateOverlays");
            UpdateMarkersOffset();

            // Update markers
            foreach (GMapMarker i in items)
            {
               if (i != null)
               {
                  i.ForceUpdateLocalPosition(this);

                  if (i is IShapable)
                  {
                     (i as IShapable).RegenerateShape(this);
                  }
               }
            }

            // Update overlays
            //foreach (var gMapOverlay in Overlays)
            //{
            //   Debug.WriteLine($"ForceUpdateOverlays, Overlays count = {Overlays.Count}");
            //   Debug.WriteLine($"ForceUpdateOverlays, gMapOverlay.Markers count = {gMapOverlay.Markers.Count}");
            //   Debug.WriteLine($"ForceUpdateOverlays, gMapOverlay.Routes count = {gMapOverlay.Routes.Count}");
            //   // Markers of overlay
            //   foreach (var gMapMarker in gMapOverlay.Markers)
            //   {
            //      if (gMapMarker != null)
            //      {
            //         gMapMarker.ForceUpdateLocalPosition(this);

            //         if (gMapMarker is IShapable)
            //         {
            //            (gMapMarker as IShapable).RegenerateShape(this);
            //         }
            //      }
            //   }

            //   // Routes or overlay
            //   foreach (var gMapRoute in gMapOverlay.Routes)
            //   {
            //      if (gMapRoute != null)
            //      {
            //         gMapRoute.ForceUpdateLocalPosition(this);
            //         gMapRoute.RegenerateShape(this);
            //      }
            //   }
            //}
         }
         InvalidateVisual();
      }

      /// <summary>
      /// updates markers overlay offset
      /// </summary>
      void UpdateMarkersOffset()
      {
         if (MapCanvas != null)
         {
            if (MapScaleTransform != null)
            {
               var tp =
                  MapScaleTransform.Transform(new System.Windows.Point(_core.renderOffset.X, _core.renderOffset.Y));
               MapOverlayTranslateTransform.X = tp.X;
               MapOverlayTranslateTransform.Y = tp.Y;

               // map is scaled already
               MapTranslateTransform.X = _core.renderOffset.X;
               MapTranslateTransform.Y = _core.renderOffset.Y;
            }
            else
            {
               MapTranslateTransform.X = _core.renderOffset.X;
               MapTranslateTransform.Y = _core.renderOffset.Y;

               MapOverlayTranslateTransform.X = MapTranslateTransform.X;
               MapOverlayTranslateTransform.Y = MapTranslateTransform.Y;
            }
         }
      }

      /// <summary>
      /// render map in WPF
      /// </summary>
      /// <param name="g"></param>
      void DrawMap(DrawingContext g)
      {
         if (MapProvider == EmptyProvider.Instance || MapProvider == null)
         {
            return;
         }

         _core.tileDrawingListLock.AcquireReaderLock();
         _core.Matrix.EnterReadLock();
         try
         {
            foreach (var tilePoint in _core.tileDrawingList)
            {
               _core.tileRect.Location = tilePoint.PosPixel;
               _core.tileRect.OffsetNegative(_core.compensationOffset);

               //if(region.IntersectsWith(Core.tileRect) || IsRotated)
               {
                  bool found = false;

                  Tile t = _core.Matrix.GetTileWithNoLock(_core.Zoom, tilePoint.PosXY);
                  if (t.NotEmpty)
                  {
                     foreach (GMapImage img in t.Overlays)
                     {
                        if (img != null && img.Img != null)
                        {
                           if (!found)
                              found = true;

                           var imgRect = new Rect(_core.tileRect.X + 0.6, _core.tileRect.Y + 0.6,
                              _core.tileRect.Width + 0.6, _core.tileRect.Height + 0.6);
                           if (!img.IsParent)
                           {
                              g.DrawImage(img.Img, imgRect);
                           }
                           else
                           {
                              // TODO: move calculations to loader thread
                              var geometry = new RectangleGeometry(imgRect);
                              var parentImgRect =
                                 new Rect(_core.tileRect.X - _core.tileRect.Width*img.Xoff + 0.6,
                                    _core.tileRect.Y - _core.tileRect.Height*img.Yoff + 0.6,
                                    _core.tileRect.Width*img.Ix + 0.6, _core.tileRect.Height*img.Ix + 0.6);

                              g.PushClip(geometry);
                              g.DrawImage(img.Img, parentImgRect);
                              g.Pop();
                              geometry = null;
                           }
                        }
                     }
                  }
                  else if (FillEmptyTiles && MapProvider.Projection is MercatorProjection)
                  {
                     #region -- fill empty tiles --

                     int zoomOffset = 1;
                     Tile parentTile = Tile.Empty;
                     long Ix = 0;

                     while (!parentTile.NotEmpty && zoomOffset < _core.Zoom && zoomOffset <= LevelsKeepInMemmory)
                     {
                        Ix = (long) Math.Pow(2, zoomOffset);
                        parentTile = _core.Matrix.GetTileWithNoLock(_core.Zoom - zoomOffset++,
                           new GMap.NET.GPoint((int) (tilePoint.PosXY.X/Ix), (int) (tilePoint.PosXY.Y/Ix)));
                     }

                     if (parentTile.NotEmpty)
                     {
                        long Xoff = Math.Abs(tilePoint.PosXY.X - (parentTile.Pos.X*Ix));
                        long Yoff = Math.Abs(tilePoint.PosXY.Y - (parentTile.Pos.Y*Ix));

                        var geometry =
                           new RectangleGeometry(new Rect(_core.tileRect.X + 0.6, _core.tileRect.Y + 0.6,
                              _core.tileRect.Width + 0.6, _core.tileRect.Height + 0.6));
                        var parentImgRect = new Rect(_core.tileRect.X - _core.tileRect.Width*Xoff + 0.6,
                           _core.tileRect.Y - _core.tileRect.Height*Yoff + 0.6, _core.tileRect.Width*Ix + 0.6,
                           _core.tileRect.Height*Ix + 0.6);

                        // render tile 
                        {
                           foreach (GMapImage img in parentTile.Overlays)
                           {
                              if (img != null && img.Img != null && !img.IsParent)
                              {
                                 if (!found)
                                    found = true;

                                 g.PushClip(geometry);
                                 g.DrawImage(img.Img, parentImgRect);
                                 g.DrawRectangle(SelectedAreaFill, null, geometry.Bounds);
                                 g.Pop();
                              }
                           }
                        }

                        geometry = null;
                     }

                     #endregion
                  }

                  // add text if tile is missing
                  if (!found)
                  {
                     lock (_core.FailedLoads)
                     {
                        var lt = new LoadTask(tilePoint.PosXY, _core.Zoom);

                        if (_core.FailedLoads.ContainsKey(lt))
                        {
                           g.DrawRectangle(EmptytileBrush, EmptyTileBorders,
                              new Rect(_core.tileRect.X, _core.tileRect.Y, _core.tileRect.Width,
                                 _core.tileRect.Height));

                           var ex = _core.FailedLoads[lt];
                           FormattedText TileText = new FormattedText("Exception: " + ex.Message,
                              System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                              _tileTypeface, 14, Brushes.Red);
                           TileText.MaxTextWidth = _core.tileRect.Width - 11;

                           g.DrawText(TileText,
                              new System.Windows.Point(_core.tileRect.X + 11, _core.tileRect.Y + 11));

                           g.DrawText(EmptyTileText,
                              new System.Windows.Point(
                                 _core.tileRect.X + (double)_core.tileRect.Width/2 - EmptyTileText.Width/2,
                                 _core.tileRect.Y + (double)_core.tileRect.Height/2 - EmptyTileText.Height/2));
                        }
                     }
                  }

                  if (ShowTileGridLines)
                  {
                     g.DrawRectangle(null, EmptyTileBorders,
                        new Rect(_core.tileRect.X, _core.tileRect.Y, _core.tileRect.Width, _core.tileRect.Height));

                     if (tilePoint.PosXY == _core.centerTileXYLocation)
                     {
                        FormattedText TileText = new FormattedText("CENTER:" + tilePoint.ToString(),
                           System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                           _tileTypeface, 16, Brushes.Red);
                        TileText.MaxTextWidth = _core.tileRect.Width;
                        g.DrawText(TileText,
                           new System.Windows.Point(
                              _core.tileRect.X + (double)_core.tileRect.Width/2 - EmptyTileText.Width/2,
                              _core.tileRect.Y + (double)_core.tileRect.Height/2 - TileText.Height/2));
                     }
                     else
                     {
                        FormattedText TileText = new FormattedText("TILE: " + tilePoint.ToString(),
                           System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                           _tileTypeface, 16, Brushes.Red);
                        TileText.MaxTextWidth = _core.tileRect.Width;
                        g.DrawText(TileText,
                           new System.Windows.Point(
                              _core.tileRect.X + (double)_core.tileRect.Width/2 - EmptyTileText.Width/2,
                              _core.tileRect.Y + (double)_core.tileRect.Height/2 - TileText.Height/2));
                     }
                  }
               }
            }
         }
         finally
         {
            _core.Matrix.LeaveReadLock();
            _core.tileDrawingListLock.ReleaseReaderLock();
         }
      }

      /// <summary>
      /// gets image of the current view
      /// </summary>
      /// <returns></returns>
      public ImageSource ToImageSource()
      {
         FrameworkElement obj = this;

         // Save current canvas transform
         Transform transform = obj.LayoutTransform;
         obj.LayoutTransform = null;

         // fix margin offset as well
         Thickness margin = obj.Margin;
         obj.Margin = new Thickness(0, 0,
            margin.Right - margin.Left, margin.Bottom - margin.Top);

         // Get the size of canvas
         Size size = new Size(obj.ActualWidth, obj.ActualHeight);

         // force control to Update
         obj.Measure(size);
         obj.Arrange(new Rect(size));

         RenderTargetBitmap bmp = new RenderTargetBitmap(
            (int) size.Width, (int) size.Height, 96, 96, PixelFormats.Pbgra32);

         bmp.Render(obj);

         if (bmp.CanFreeze)
         {
            bmp.Freeze();
         }

         // return values as they were before
         obj.LayoutTransform = transform;
         obj.Margin = margin;

         return bmp;
      }

      /// <summary>
      /// sets zoom to max to fit rect
      /// </summary>
      /// <param name="rect">area</param>
      /// <returns></returns>
      public bool SetZoomToFitRect(RectLatLng rect)
      {
         if (_lazyEvents)
         {
            _lazySetZoomToFitRect = rect;
         }
         else
         {
            int maxZoom = _core.GetMaxZoomToFitRect(rect);
            if (maxZoom > 0)
            {
               PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat/2), rect.Lng + (rect.WidthLng/2));
               Position = center;

               if (maxZoom > MaxZoom)
               {
                  maxZoom = MaxZoom;
               }

               if (_core.Zoom != maxZoom)
               {
                  Zoom = maxZoom;
               }

               return true;
            }
         }
         return false;
      }
      
      /// <summary>
      /// sets to max zoom to fit all markers and centers them in map
      /// </summary>
      /// <param name="ZIndex">z index or null to check all</param>
      /// <returns></returns>
      public bool ZoomAndCenterMarkers(int? ZIndex)
      {
         RectLatLng? rect = GetRectOfAllMarkers(ZIndex);
         if (rect.HasValue)
         {
            return SetZoomToFitRect(rect.Value);
         }

         return false;
      }

      /// <summary>
      /// gets rectangle with all objects inside
      /// </summary>
      /// <param name="ZIndex">z index or null to check all</param>
      /// <returns></returns>
      public RectLatLng? GetRectOfAllMarkers(int? ZIndex)
      {
         RectLatLng? ret = null;

         double left = double.MaxValue;
         double top = double.MinValue;
         double right = double.MinValue;
         double bottom = double.MaxValue;
         IEnumerable<GMapMarker> Overlays;

         if (ZIndex.HasValue)
         {
            Overlays = ItemsSource.Cast<GMapMarker>().Where(p => p != null && p.ZIndex == ZIndex);
         }
         else
         {
            Overlays = ItemsSource.Cast<GMapMarker>();
         }

         if (Overlays != null)
         {
            foreach (var m in Overlays)
            {
               if (m.Shape != null && m.Shape.Visibility == System.Windows.Visibility.Visible)
               {
                  // left
                  if (m.Position.Lng < left)
                  {
                     left = m.Position.Lng;
                  }

                  // top
                  if (m.Position.Lat > top)
                  {
                     top = m.Position.Lat;
                  }

                  // right
                  if (m.Position.Lng > right)
                  {
                     right = m.Position.Lng;
                  }

                  // bottom
                  if (m.Position.Lat < bottom)
                  {
                     bottom = m.Position.Lat;
                  }
               }
            }
         }

         if (left != double.MaxValue && right != double.MinValue && top != double.MinValue &&
             bottom != double.MaxValue)
         {
            ret = RectLatLng.FromLTRB(left, top, right, bottom);
         }

         return ret;
      }

      /// <summary>
      /// offset position in pixels
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      public void Offset(int x, int y)
      {
         if (IsLoaded)
         {
            if (IsRotated)
            {
               Point p = new Point(x, y);
               p = _rotationMatrixInvert.Transform(p);
               x = (int) p.X;
               y = (int) p.Y;

               _core.DragOffset(new GPoint(x, y));

               ForceUpdateOverlays();
            }
            else
            {
               _core.DragOffset(new GPoint(x, y));

               UpdateMarkersOffset();
               InvalidateVisual(true);
            }
         }
      }

      /// <summary>
      /// updates rotation matrix
      /// </summary>
      void UpdateRotationMatrix()
      {
         System.Windows.Point center = new System.Windows.Point(ActualWidth/2.0, ActualHeight/2.0);

         _rotationMatrix.Angle = -Bearing;
         _rotationMatrix.CenterY = center.Y;
         _rotationMatrix.CenterX = center.X;

         _rotationMatrixInvert = _rotationMatrix.Inverse;
      }

      /// <summary>
      /// apply transformation if in rotation mode
      /// </summary>
      System.Windows.Point ApplyRotation(double x, double y)
      {
         System.Windows.Point ret = new System.Windows.Point(x, y);

         if (IsRotated)
         {
            ret = _rotationMatrix.Transform(ret);
         }

         return ret;
      }

      /// <summary>
      /// apply transformation if in rotation mode
      /// </summary>
      System.Windows.Point ApplyRotationInversion(double x, double y)
      {
         System.Windows.Point ret = new System.Windows.Point(x, y);

         if (IsRotated)
         {
            ret = _rotationMatrixInvert.Transform(ret);
         }

         return ret;
      }

      #region UserControl Events

      protected override void OnRender(DrawingContext drawingContext)
      {
         if (!_core.IsStarted)
            return;

         Debug.WriteLine($"OnRender, time = {DateTime.Now.ToLongTimeString()}");

         // Render map background
         drawingContext.DrawRectangle(EmptyMapBackground, null, new Rect(RenderSize));

         // Render map
         if (IsRotated)
         {
            drawingContext.PushTransform(_rotationMatrix);

            if (MapScaleTransform != null)
            {
               drawingContext.PushTransform(MapScaleTransform);
               drawingContext.PushTransform(MapTranslateTransform);
               {
                  DrawMap(drawingContext);
               }
               drawingContext.Pop();
               drawingContext.Pop();
            }
            else
            {
               drawingContext.PushTransform(MapTranslateTransform);
               {
                  DrawMap(drawingContext);
               }
               drawingContext.Pop();
            }

            drawingContext.Pop();
         }
         else
         {
            if (MapScaleTransform != null)
            {
               drawingContext.PushTransform(MapScaleTransform);
               drawingContext.PushTransform(MapTranslateTransform);
               {
                  DrawMap(drawingContext);

#if DEBUG
                  drawingContext.DrawLine(_virtualCenterCrossPen, new Point(-20, 0), new Point(20, 0));
                  drawingContext.DrawLine(_virtualCenterCrossPen, new Point(0, -20), new Point(0, 20));
#endif
               }
               drawingContext.Pop();
               drawingContext.Pop();
            }
            else
            {
               drawingContext.PushTransform(MapTranslateTransform);
               {
                  DrawMap(drawingContext);
#if DEBUG
                  drawingContext.DrawLine(_virtualCenterCrossPen, new Point(-20, 0), new Point(20, 0));
                  drawingContext.DrawLine(_virtualCenterCrossPen, new Point(0, -20), new Point(0, 20));
#endif
               }
               drawingContext.Pop();
            }
         }

         // Render selection
         if (!SelectedArea.IsEmpty)
         {
            GPoint p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
            GPoint p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

            long x1 = p1.X;
            long y1 = p1.Y;
            long x2 = p2.X;
            long y2 = p2.Y;

            switch (SelectionFigure)
            {
               case GMapSelectionFigureEnum.Ellipse:
                  drawingContext.DrawEllipse(SelectedAreaFill, SelectionPen,
                  new System.Windows.Point(x1 + (x2 - x1)/2, y1 + (y2 - y1)/2), ((double)x2 - x1)/2, ((double)y2 - y1)/2);
                  break;
               case GMapSelectionFigureEnum.Circle:
                  GPoint cp1 = FromLatLngToLocal(_selectionStart);
                  GPoint cp2 = FromLatLngToLocal(_selectionEnd);
                  //((x1-x2)*(x1-x2)+(y1-y2)*(y1-y2)) < d*d
                  double radius = Math.Sqrt((cp1.X - cp2.X) * (cp1.X - cp2.X) + (cp1.Y - cp2.Y) * (cp1.Y - cp2.Y));
                  drawingContext.DrawEllipse(SelectedAreaFill, SelectionPen, new Point(cp1.X, cp1.Y), radius, radius);
                  break;
               case GMapSelectionFigureEnum.Rectangle:
                  drawingContext.DrawRoundedRectangle(SelectedAreaFill, SelectionPen,
                     new Rect(x1, y1, x2 - x1, y2 - y1), 5, 5);
                  break;
            }
         }

         // Render center
         if (ShowCenter)
         {
            drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point((ActualWidth/2) - 5, ActualHeight/2),
               new System.Windows.Point((ActualWidth/2) + 5, ActualHeight/2));
            drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point(ActualWidth/2, (ActualHeight/2) - 5),
               new System.Windows.Point(ActualWidth/2, (ActualHeight/2) + 5));
         }

         // Render mouse 
         if (_renderHelperLine)
         {
            var p = Mouse.GetPosition(this);

            drawingContext.DrawLine(HelperLinePen, new Point(p.X, 0), new Point(p.X, ActualHeight));
            drawingContext.DrawLine(HelperLinePen, new Point(0, p.Y), new Point(ActualWidth, p.Y));
         }

         #region -- copyright --
         // Render copyright
         if (_copyright != null)
         {
            drawingContext.DrawText(_copyright, new System.Windows.Point(5, ActualHeight - _copyright.Height - 5));
         }

         #endregion

         base.OnRender(drawingContext);
      }
      
      protected override void OnKeyUp(KeyEventArgs e)
      {
         base.OnKeyUp(e);

         if (HelperLineOptionEnum == HelperLineOptionsEnum.ShowOnModifierKey)
         {
            _renderHelperLine = !(e.IsUp && (e.Key == Key.LeftShift || e.SystemKey == Key.LeftAlt));
            if (!_renderHelperLine)
            {
               InvalidateVisual();
            }
         }
      }

      protected override void OnKeyDown(KeyEventArgs e)
      {
         base.OnKeyDown(e);

         if (HelperLineOptionEnum == HelperLineOptionsEnum.ShowOnModifierKey)
         {
            _renderHelperLine = e.IsDown && (e.Key == Key.LeftShift || e.SystemKey == Key.LeftAlt);
            if (_renderHelperLine)
            {
               InvalidateVisual();
            }
         }
      }
      
      protected override void OnMouseWheel(MouseWheelEventArgs e)
      {
         base.OnMouseWheel(e);

         if (MouseWheelZoomEnabled && (IsMouseDirectlyOver || IgnoreMarkerOnMouseWheel) && !_core.IsDragging)
         {
            System.Windows.Point p = e.GetPosition(this);

            if (MapScaleTransform != null)
            {
               p = MapScaleTransform.Inverse.Transform(p);
            }

            p = ApplyRotationInversion(p.X, p.Y);

            if (_core.mouseLastZoom.X != (int) p.X && _core.mouseLastZoom.Y != (int) p.Y)
            {
               if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
               {
                  _core.position = FromLocalToLatLng((int) p.X, (int) p.Y);
               }
               else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
               {
                  _core.position = FromLocalToLatLng((int) ActualWidth/2, (int) ActualHeight/2);
               }
               else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
               {
                  _core.position = FromLocalToLatLng((int) p.X, (int) p.Y);
               }

               _core.mouseLastZoom.X = (int) p.X;
               _core.mouseLastZoom.Y = (int) p.Y;
            }

            // set mouse position to map center
            if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
            {
               System.Windows.Point ps = PointToScreen(new System.Windows.Point(ActualWidth/2, ActualHeight/2));
               Stuff.SetCursorPos((int) ps.X, (int) ps.Y);
            }

            _core.MouseWheelZooming = true;

            if (e.Delta > 0)
            {
               if (!InvertedMouseWheelZooming)
               {
                  Zoom = ((int) Zoom) + 1;
               }
               else
               {
                  Zoom = ((int) (Zoom + 0.99)) - 1;
               }
            }
            else
            {
               if (InvertedMouseWheelZooming)
               {
                  Zoom = ((int) Zoom) + 1;
               }
               else
               {
                  Zoom = ((int) (Zoom + 0.99)) - 1;
               }
            }

            _core.MouseWheelZooming = false;
         }
      }

      protected override void OnMouseDown(MouseButtonEventArgs e)
      {
         base.OnMouseDown(e);

         if (CanDragMap && e.ChangedButton == DragButton)
         {
            Point p = e.GetPosition(this);

            if (MapScaleTransform != null)
            {
               p = MapScaleTransform.Inverse.Transform(p);
            }

            p = ApplyRotationInversion(p.X, p.Y);

            _core.mouseDown.X = (int) p.X;
            _core.mouseDown.Y = (int) p.Y;

            InvalidateVisual();
         }
         else
         {
            if (!_isSelected)
            {
               Point p = e.GetPosition(this);
               _isSelected = true;
               SelectedArea = RectLatLng.Empty;
               _selectionEnd = PointLatLng.Empty;
               _selectionStart = FromLocalToLatLng((int) p.X, (int) p.Y);
            }
         }
      }

      protected override void OnMouseUp(MouseButtonEventArgs e)
      {
         base.OnMouseUp(e);

         if (_isSelected)
         {
            _isSelected = false;
         }

         if (_core.IsDragging)
         {
            if (isDragging)
            {
               _onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
               isDragging = false;
               Debug.WriteLine("IsDragging = " + isDragging);
               Cursor = _cursorBefore;
               Mouse.Capture(null);
            }
            _core.EndDrag();

            if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
            {
               if (_core.LastLocationInBounds.HasValue)
               {
                  Position = _core.LastLocationInBounds.Value;
               }
            }
         }
         else
         {
            if (e.ChangedButton == DragButton)
            {
               _core.mouseDown = GPoint.Empty;
            }

            if (!_selectionEnd.IsEmpty && !_selectionStart.IsEmpty)
            {
               bool zoomtofit = false;

               if (!SelectedArea.IsEmpty && Keyboard.Modifiers == ModifierKeys.Shift)
               {
                  zoomtofit = SetZoomToFitRect(SelectedArea);
               }

               if (OnSelectionChange != null)
               {
                  OnSelectionChange(SelectedArea, zoomtofit);
               }
            }
            else
            {
               InvalidateVisual();
            }
         }
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         base.OnMouseMove(e);

         // wpf generates to many events if mouse is over some visual
         // and OnMouseUp is fired, wtf, anyway...
         // http://greatmaps.codeplex.com/workitem/16013
         if ((e.Timestamp & Int32.MaxValue) - _onMouseUpTimestamp < 55)
         {
            Debug.WriteLine("OnMouseMove skipped: " + ((e.Timestamp & Int32.MaxValue) - _onMouseUpTimestamp) + "ms");
            return;
         }

         if (!_core.IsDragging && !_core.mouseDown.IsEmpty)
         {
            Point p = e.GetPosition(this);

            if (MapScaleTransform != null)
            {
               p = MapScaleTransform.Inverse.Transform(p);
            }

            p = ApplyRotationInversion(p.X, p.Y);

            // cursor has moved beyond drag tolerance
            if (Math.Abs(p.X - _core.mouseDown.X)*2 >= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(p.Y - _core.mouseDown.Y)*2 >= SystemParameters.MinimumVerticalDragDistance)
            {
               _core.BeginDrag(_core.mouseDown);
            }
         }

         if (_core.IsDragging)
         {
            if (!isDragging)
            {
               isDragging = true;
               Debug.WriteLine("IsDragging = " + isDragging);
               _cursorBefore = Cursor;
               Cursor = Cursors.SizeAll;
               Mouse.Capture(this);
            }

            if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
            {
               // ...
            }
            else
            {
               Point p = e.GetPosition(this);

               if (MapScaleTransform != null)
               {
                  p = MapScaleTransform.Inverse.Transform(p);
               }

               p = ApplyRotationInversion(p.X, p.Y);

               _core.mouseCurrent.X = (int) p.X;
               _core.mouseCurrent.Y = (int) p.Y;
               {
                  _core.Drag(_core.mouseCurrent);
               }

               if (IsRotated || _scaleModeEnum != ScaleModesEnum.Integer)
               {
                  ForceUpdateOverlays();
               }
               else
               {
                  UpdateMarkersOffset();
               }
            }
            InvalidateVisual(true);
         }
         else
         {
            if (_isSelected && !_selectionStart.IsEmpty &&
                (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt ||
                 DisableAltForSelection))
            {
               System.Windows.Point p = e.GetPosition(this);
               _selectionEnd = FromLocalToLatLng((int) p.X, (int) p.Y);
               {
                  GMap.NET.PointLatLng p1 = _selectionStart;
                  GMap.NET.PointLatLng p2 = _selectionEnd;

                  double x1 = Math.Min(p1.Lng, p2.Lng);
                  double y1 = Math.Max(p1.Lat, p2.Lat);
                  double x2 = Math.Max(p1.Lng, p2.Lng);
                  double y2 = Math.Min(p1.Lat, p2.Lat);

                  SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
               }
            }

            if (_renderHelperLine)
            {
               InvalidateVisual(true);
            }
         }
      }
      
      protected override void OnStylusDown(StylusDownEventArgs e)
      {
         base.OnStylusDown(e);

         if (TouchEnabled && CanDragMap && !e.InAir)
         {
            Point p = e.GetPosition(this);

            if (MapScaleTransform != null)
            {
               p = MapScaleTransform.Inverse.Transform(p);
            }

            p = ApplyRotationInversion(p.X, p.Y);

            _core.mouseDown.X = (int) p.X;
            _core.mouseDown.Y = (int) p.Y;

            InvalidateVisual();
         }
      }

      protected override void OnStylusUp(StylusEventArgs e)
      {
         base.OnStylusUp(e);

         if (TouchEnabled)
         {
            if (_isSelected)
            {
               _isSelected = false;
            }

            if (_core.IsDragging)
            {
               if (isDragging)
               {
                  _onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
                  isDragging = false;
                  Debug.WriteLine("IsDragging = " + isDragging);
                  Cursor = _cursorBefore;
                  Mouse.Capture(null);
               }
               _core.EndDrag();

               if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
               {
                  if (_core.LastLocationInBounds.HasValue)
                  {
                     Position = _core.LastLocationInBounds.Value;
                  }
               }
            }
            else
            {
               _core.mouseDown = GPoint.Empty;
               InvalidateVisual();
            }
         }
      }

      protected override void OnStylusMove(StylusEventArgs e)
      {
         base.OnStylusMove(e);

         if (TouchEnabled)
         {
            // wpf generates to many events if mouse is over some visual
            // and OnMouseUp is fired, wtf, anyway...
            // http://greatmaps.codeplex.com/workitem/16013
            if ((e.Timestamp & Int32.MaxValue) - _onMouseUpTimestamp < 55)
            {
               Debug.WriteLine("OnMouseMove skipped: " + ((e.Timestamp & Int32.MaxValue) - _onMouseUpTimestamp) +
                               "ms");
               return;
            }

            if (!_core.IsDragging && !_core.mouseDown.IsEmpty)
            {
               Point p = e.GetPosition(this);

               if (MapScaleTransform != null)
               {
                  p = MapScaleTransform.Inverse.Transform(p);
               }

               p = ApplyRotationInversion(p.X, p.Y);

               // cursor has moved beyond drag tolerance
               if (Math.Abs(p.X - _core.mouseDown.X)*2 >= SystemParameters.MinimumHorizontalDragDistance ||
                   Math.Abs(p.Y - _core.mouseDown.Y)*2 >= SystemParameters.MinimumVerticalDragDistance)
               {
                  _core.BeginDrag(_core.mouseDown);
               }
            }

            if (_core.IsDragging)
            {
               if (!isDragging)
               {
                  isDragging = true;
                  Debug.WriteLine("IsDragging = " + isDragging);
                  _cursorBefore = Cursor;
                  Cursor = Cursors.SizeAll;
                  Mouse.Capture(this);
               }

               if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
               {
                  // ...
               }
               else
               {
                  Point p = e.GetPosition(this);

                  if (MapScaleTransform != null)
                  {
                     p = MapScaleTransform.Inverse.Transform(p);
                  }

                  p = ApplyRotationInversion(p.X, p.Y);

                  _core.mouseCurrent.X = (int) p.X;
                  _core.mouseCurrent.Y = (int) p.Y;
                  {
                     _core.Drag(_core.mouseCurrent);
                  }

                  if (IsRotated)
                  {
                     ForceUpdateOverlays();
                  }
                  else
                  {
                     UpdateMarkersOffset();
                  }
               }
               InvalidateVisual();
            }
         }
      }

      #endregion

      #region IGControl Members

      /// <summary>
      /// Call it to empty tile cache & reload tiles
      /// </summary>
      public void ReloadMap()
      {
         _core.ReloadMap();
      }

      /// <summary>
      /// sets position using geocoder
      /// </summary>
      /// <param name="keys"></param>
      /// <returns></returns>
      public GeoCoderStatusCode SetPositionByKeywords(string keys)
      {
         GeoCoderStatusCode status = GeoCoderStatusCode.Unknow;

         GeocodingProvider gp = MapProvider as GeocodingProvider;
         if (gp == null)
         {
            gp = GMapProviders.OpenStreetMap as GeocodingProvider;
         }

         if (gp != null)
         {
            var pt = gp.GetPoint(keys, out status);
            if (status == GeoCoderStatusCode.G_GEO_SUCCESS && pt.HasValue)
            {
               Position = pt.Value;
            }
         }

         return status;
      }

      public PointLatLng FromLocalToLatLng(int x, int y)
      {
         if (MapScaleTransform != null)
         {
            var tp = MapScaleTransform.Inverse.Transform(new System.Windows.Point(x, y));
            x = (int) tp.X;
            y = (int) tp.Y;
         }

         if (IsRotated)
         {
            var f = _rotationMatrixInvert.Transform(new System.Windows.Point(x, y));

            x = (int) f.X;
            y = (int) f.Y;
         }

         return _core.FromLocalToLatLng(x, y);
      }

      public GPoint FromLatLngToLocal(PointLatLng point)
      {
         GPoint ret = _core.FromLatLngToLocal(point);

         if (MapScaleTransform != null)
         {
            var tp = MapScaleTransform.Transform(new System.Windows.Point(ret.X, ret.Y));
            ret.X = (int) tp.X;
            ret.Y = (int) tp.Y;
         }

         if (IsRotated)
         {
            var f = _rotationMatrix.Transform(new System.Windows.Point(ret.X, ret.Y));

            ret.X = (int) f.X;
            ret.Y = (int) f.Y;
         }

         return ret;
      }

      public bool ShowExportDialog()
      {
         var dlg = new Microsoft.Win32.SaveFileDialog();
         {
            dlg.CheckPathExists = true;
            dlg.CheckFileExists = false;
            dlg.AddExtension = true;
            dlg.DefaultExt = "gmdb";
            dlg.ValidateNames = true;
            dlg.Title = "GMap.NET: Export map to db, if file exsist only new data will be added";
            dlg.FileName = "DataExp";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
               bool ok = GMaps.Instance.ExportToGMDB(dlg.FileName);
               if (ok)
               {
                  MessageBox.Show("Complete!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Information);
               }
               else
               {
                  MessageBox.Show("  Failed!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
               }

               return ok;
            }
         }

         return false;
      }

      public bool ShowImportDialog()
      {
         var dlg = new Microsoft.Win32.OpenFileDialog();
         {
            dlg.CheckPathExists = true;
            dlg.CheckFileExists = false;
            dlg.AddExtension = true;
            dlg.DefaultExt = "gmdb";
            dlg.ValidateNames = true;
            dlg.Title = "GMap.NET: Import to db, only new data will be added";
            dlg.FileName = "DataImport";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
               Cursor = Cursors.Wait;

               bool ok = GMaps.Instance.ImportFromGMDB(dlg.FileName);
               if (ok)
               {
                  MessageBox.Show("Complete!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Information);
                  ReloadMap();
               }
               else
               {
                  MessageBox.Show("  Failed!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
               }

               Cursor = Cursors.Arrow;

               return ok;
            }
         }

         return false;
      }

      public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
         "Position", typeof (PointLatLng), typeof (GMapControl),
         new FrameworkPropertyMetadata(default(PointLatLng), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            PositionChangedCallBack));

      /// <summary>
      /// Current coordinates of the map center
      /// </summary>
      [Browsable(false)]
      public PointLatLng Position
      {
         get { return (PointLatLng) GetValue(PositionProperty); }
         set { SetValue(PositionProperty, value); }
      }

      private static void PositionChangedCallBack(DependencyObject source,
         DependencyPropertyChangedEventArgs e)
      {
         ((GMapControl) source).PositionChanged(e);
      }

      private void PositionChanged(DependencyPropertyChangedEventArgs e)
      {
         _core.Position = Position;
         if (_core.IsStarted)
         {
            ForceUpdateOverlays();
         }
      }

      [Browsable(false)]
      public GPoint PositionPixel
      {
         get { return _core.PositionPixel; }
      }

      [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
      [Browsable(false)]
      public string CacheLocation
      {
         get { return CacheLocator.Location; }
         set { CacheLocator.Location = value; }
      }

      bool isDragging = false;

      [Browsable(false)]
      public bool IsDragging
      {
         get { return isDragging; }
      }

      [Browsable(false)]
      public RectLatLng ViewArea
      {
         get
         {
            if (!IsRotated)
            {
               return _core.ViewArea;
            }
            else if (_core.Provider.Projection != null)
            {
               var p = FromLocalToLatLng(0, 0);
               var p2 = FromLocalToLatLng((int) Width, (int) Height);

               return RectLatLng.FromLTRB(p.Lng, p.Lat, p2.Lng, p2.Lat);
            }
            return RectLatLng.Empty;
         }
      }

      [Category("GMap.NET")]
      public bool CanDragMap
      {
         get { return _core.CanDragMap; }
         set { _core.CanDragMap = value; }
      }

      public GMap.NET.RenderMode RenderMode
      {
         get { return GMap.NET.RenderMode.WPF; }
      }

      #endregion

      #region IGControl event Members

      public event PositionChanged OnPositionChanged
      {
         add { _core.OnCurrentPositionChanged += value; }
         remove { _core.OnCurrentPositionChanged -= value; }
      }

      public event TileLoadComplete OnTileLoadComplete
      {
         add { _core.OnTileLoadComplete += value; }
         remove { _core.OnTileLoadComplete -= value; }
      }

      public event TileLoadStart OnTileLoadStart
      {
         add { _core.OnTileLoadStart += value; }
         remove { _core.OnTileLoadStart -= value; }
      }

      public event MapDrag OnMapDrag
      {
         add { _core.OnMapDrag += value; }
         remove { _core.OnMapDrag -= value; }
      }

      public event MapZoomChanged OnMapZoomChanged
      {
         add { _core.OnMapZoomChanged += value; }
         remove { _core.OnMapZoomChanged -= value; }
      }

      /// <summary>
      /// occures on map type changed
      /// </summary>
      public event MapTypeChanged OnMapTypeChanged
      {
         add { _core.OnMapTypeChanged += value; }
         remove { _core.OnMapTypeChanged -= value; }
      }

      /// <summary>
      /// occurs on empty tile displayed
      /// </summary>
      public event EmptyTileError OnEmptyTileError
      {
         add { _core.OnEmptyTileError += value; }
         remove { _core.OnEmptyTileError -= value; }
      }

      #endregion

      #region IDisposable Members

      public virtual void Dispose()
      {
         if (_core.IsStarted)
         {
            Overlays.CollectionChanged -= OverlaysOnCollectionChanged;
            _core.OnMapZoomChanged -= new MapZoomChanged(ForceUpdateOverlays);
            _core.OnCurrentPositionChanged -= CoreOnCurrentPositionChanged;
            Loaded -= new RoutedEventHandler(GMapControl_Loaded);
            Dispatcher.ShutdownStarted -= new EventHandler(Dispatcher_ShutdownStarted);
            SizeChanged -= new SizeChangedEventHandler(GMapControl_SizeChanged);
            if (_loadedApp != null)
            {
               _loadedApp.SessionEnding -= new SessionEndingCancelEventHandler(Current_SessionEnding);
            }
            _core.OnMapClose();
         }
      }

      #endregion
   }
}