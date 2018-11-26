using ReaderView.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace ReaderView.Controls
{
    public sealed class ReaderView : Control, IInteractionTrackerOwner
    {
        #region Ctor.

        public ReaderView()
        {
            this.DefaultStyleKey = typeof(ReaderView);

            _GestureRecognizer = new GestureRecognizer();
            _GestureRecognizer.GestureSettings = GestureSettings.ManipulationTranslateX;
            _GestureRecognizer.ManipulationStarted += _GestureRecognizer_ManipulationStarted;
            _GestureRecognizer.ManipulationUpdated += _GestureRecognizer_ManipulationUpdated;
            _GestureRecognizer.ManipulationCompleted += _GestureRecognizer_ManipulationCompleted;

            PointerWheelChangedEventHandler = new PointerEventHandler(_PointerWheelChanged);
            PointerPressedEventHandler = new PointerEventHandler(_PointerPressed);
            PointerMovedEventHandler = new PointerEventHandler(_PointerMoved);
            PointerReleasedEventHandler = new PointerEventHandler(_PointerReleased);
            PointerCanceledEventHandler = new PointerEventHandler(_PointerCanceled);

            this.AddHandler(UIElement.PointerWheelChangedEvent, PointerWheelChangedEventHandler, true);
            this.AddHandler(UIElement.PointerPressedEvent, PointerPressedEventHandler, true);
            this.AddHandler(UIElement.PointerMovedEvent, PointerMovedEventHandler, true);
            this.AddHandler(UIElement.PointerReleasedEvent, PointerReleasedEventHandler, true);
            this.AddHandler(UIElement.PointerCanceledEvent, PointerCanceledEventHandler, true);
            this.SizeChanged += ReaderView_SizeChanged;


            IndexWaiter = new EventWaiter();
            CreateContentDelayer = new EventDelayer();
            CreateContentDelayer.ResetWhenDelayed = true;
            CreateContentDelayer.Arrived += CreateContentWaiter_Arrived;

            this.Unloaded += (s, a) =>
            {
                this.RemoveHandler(UIElement.PointerWheelChangedEvent, PointerWheelChangedEventHandler);
                this.RemoveHandler(UIElement.PointerPressedEvent, PointerPressedEventHandler);
                this.RemoveHandler(UIElement.PointerMovedEvent, PointerMovedEventHandler);
                this.RemoveHandler(UIElement.PointerReleasedEvent, PointerReleasedEventHandler);
                this.RemoveHandler(UIElement.PointerCanceledEvent, PointerCanceledEventHandler);

            };

        }
        #endregion Ctor.

        #region Field

        string content;
        double startX = 0;
        bool IsCoreSelectedChanged;
        bool IsAnimating;
        EventWaiter IndexWaiter;
        EventDelayer CreateContentDelayer;

        Compositor compositor;
        Vector3KeyFrameAnimation OffsetAnimation;
        Visual PanelVisual;
        Visual ReaderViewVisual;

        InteractionTracker m_tracker;
        VisualInteractionSource m_source;
        ExpressionAnimation OffsetBind;

        PointerEventHandler PointerWheelChangedEventHandler;
        PointerEventHandler PointerPressedEventHandler;
        PointerEventHandler PointerMovedEventHandler;
        PointerEventHandler PointerReleasedEventHandler;
        PointerEventHandler PointerCanceledEventHandler;
        GestureRecognizer _GestureRecognizer;

        StackPanel ContentPanel;
        Border ContentBorder;

        #endregion Field

        #region Overrides

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ContentPanel = GetTemplateChild(nameof(ContentPanel)) as StackPanel;
            ContentBorder = GetTemplateChild(nameof(ContentBorder)) as Border;

            SetupComposition();
            SetupTracker();
        }

        #endregion Overrides

        #region Public Methods

        public void SetContent(string Content, SetContentMode mode = SetContentMode.First)
        {
            this.content = Content;
            CreateContent();
            IsCoreSelectedChanged = true;
            switch (mode)
            {
                case SetContentMode.First:
                    Index = 0;
                    break;
                case SetContentMode.Last:
                    Index = Count - 1;
                    break;
                case SetContentMode.Stay:
                    Index = Index > Count - 1 ? Count - 1 : Index;
                    break;
            }
            GotoIndex(Index, false);
            IsCoreSelectedChanged = false;
        }

        #endregion Public Methods

        #region Private Method

        private void SetupComposition()
        {
            compositor = Window.Current.Compositor;

            OffsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            OffsetAnimation.Duration = TimeSpan.FromSeconds(0.15d);
            OffsetAnimation.Target = "Offset";
            OffsetAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;

            PanelVisual = ElementCompositionPreview.GetElementVisual(ContentPanel);
            ReaderViewVisual = ElementCompositionPreview.GetElementVisual(this);
        }

        private void SetupTracker()
        {
            m_tracker = InteractionTracker.CreateWithOwner(compositor, this);
            InitTrackerPositions();

            m_source = VisualInteractionSource.Create(ReaderViewVisual);
            m_source.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;

            m_source.IsPositionXRailsEnabled = false;
            m_source.PositionXChainingMode = InteractionChainingMode.Never;
            m_source.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;

            m_source.IsPositionYRailsEnabled = false;
            m_source.PositionYChainingMode = InteractionChainingMode.Never;
            m_source.PositionYSourceMode = InteractionSourceMode.Disabled;

            m_tracker.InteractionSources.Add(m_source);

            OffsetBind = compositor.CreateExpressionAnimation("-tracker.Position");
            OffsetBind.SetReferenceParameter("tracker", m_tracker);

            PanelVisual.StartAnimation("Offset", OffsetBind);
        }

        private void InitTrackerPositions()
        {
            if (m_tracker != null)
            {
                m_tracker.MaxPosition = new Vector3((float)((Count + 1) * this.ActualWidth), 0f, 0f);
                m_tracker.MinPosition = new Vector3(-(float)(this.ActualWidth), 0f, 0f);
            }
        }

        private void GotoIndex(int index, bool UseAnimation = true)
        {
            if (index < 0) return;
            if (UseAnimation)
            {
                OffsetAnimation.InsertKeyFrame(1f, new Vector3((float)(this.ActualWidth * index), 0f, 0f));
                m_tracker.TryUpdatePositionWithAnimation(OffsetAnimation);
                //PanelVisual.StartAnimation("Offset", OffsetAnimation);
            }
            else
            {
                m_tracker.TryUpdatePosition(new Vector3((float)(this.ActualWidth * index), 0f, 0f));
                //PanelVisual.Offset = new Vector3((float)(-this.ActualWidth * index), 0f, 0f);
            }
        }

        private void CreateContent()
        {
            ContentPanel.Children.Clear();

            var readerPage = new ReaderPage();
            readerPage.Padding = Padding;
            readerPage.SetContent(content, LineHeight);
            ContentPanel.Children.Add(readerPage);
            readerPage.Width = ContentBorder.ActualWidth;
            readerPage.Height = ContentBorder.ActualHeight;
            readerPage.Measure(new Windows.Foundation.Size(ContentBorder.ActualWidth, ContentBorder.ActualHeight));

            while (readerPage.HasOverflow)
            {
                var tmp = new ReaderPage();
                tmp.Padding = Padding;
                tmp.SetContent(readerPage);
                ContentPanel.Children.Add(tmp);
                tmp.Width = ContentBorder.ActualWidth;
                tmp.Height = ContentBorder.ActualHeight;
                tmp.Measure(new Windows.Foundation.Size(ContentBorder.ActualWidth, ContentBorder.ActualHeight));
                readerPage = tmp;
            }

            Count = ContentPanel.Children.Count;
        }

        #endregion Private Method

        #region Dependency Properties

        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        public int Count
        {
            get { return (int)GetValue(CountProperty); }
            private set { SetValue(CountProperty, value); }
        }

        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public object Footer
        {
            get { return (object)GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.Register("Index", typeof(int), typeof(ReaderView), new PropertyMetadata(-1, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    var index = (int)a.NewValue;
                    if (s is ReaderView sender)
                    {
                        if (!sender.IsCoreSelectedChanged)
                        {
                            sender.GotoIndex(index);
                        }

                        sender.OnSelectionChanged();

                        if (index < 0) sender.OnPrevPageSelected();
                        else if (index > sender.Count - 1) sender.OnNextPageSelected();
                    }
                }
            }));

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register("Count", typeof(int), typeof(ReaderView), new PropertyMetadata(0, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is ReaderView sender)
                    {
                        sender.InitTrackerPositions();
                    }
                }
            }));

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register("LineHeight", typeof(double), typeof(ReaderView), new PropertyMetadata(10d, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is ReaderView sender)
                    {
                        sender.CreateContentDelayer.Delay();
                    }
                }
            }));

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(ReaderView), new PropertyMetadata(null));

        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register("Footer", typeof(object), typeof(ReaderView), new PropertyMetadata(null));



        #endregion Dependency Properties

        #region Events

        public event EventHandler PrevPageSelected;
        public event EventHandler NextPageSelected;
        public event EventHandler<int> SelectionChanged;

        private void OnPrevPageSelected()
        {
            PrevPageSelected?.Invoke(this, EventArgs.Empty);
        }

        private void OnNextPageSelected()
        {
            NextPageSelected?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, Index);
        }

        private void ReaderView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CreateContentDelayer.Delay();
        }

        private void CreateContentWaiter_Arrived(object sender, EventArgs e)
        {
            CreateContent();
            IsCoreSelectedChanged = true;
            if (Index > Count - 1)
            {
                Index = Count - 1;
            }
            GotoIndex(Index, false);
            IsCoreSelectedChanged = false;
        }

        #region Manipulation Events

        private void _GestureRecognizer_ManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
            startX = Index * this.ActualWidth;
        }

        private void _GestureRecognizer_ManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            if (Math.Abs(args.Cumulative.Translation.X) < this.ActualWidth)
            {
                m_tracker.TryUpdatePosition(new Vector3((float)(startX - args.Cumulative.Translation.X), 0f, 0f));
            }
        }

        private void _GestureRecognizer_ManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            IsCoreSelectedChanged = true;
            if (args.Cumulative.Translation.X > 150 || args.Velocities.Linear.X > 0.5)
            {
                Index--;
                if (Index < 0) Index = 0;
            }
            else if (args.Cumulative.Translation.X < -150 || args.Velocities.Linear.X < -0.5)
            {
                Index++;
                if (Index > Count - 1) Index = Count - 1;
            }
            GotoIndex(Index);
            IsCoreSelectedChanged = false;
        }

        #endregion Manipulation Events

        #region Pointer Events

        private void _PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (IndexWaiter.IsEnabled)
            {
                IsCoreSelectedChanged = true;
                if (e.GetCurrentPoint(this).Properties.MouseWheelDelta > 0)
                {
                    Index--;
                    if (Index < 0) Index = 0;
                }
                else
                {
                    Index++;
                    if (Index > Count - 1) Index = Count - 1;
                }
                GotoIndex(Index);
                IsCoreSelectedChanged = false;
            }
        }

        private void _PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (IndexWaiter.IsEnabled)
            {
                if (m_source != null)
                {
                    var pointer = e.GetCurrentPoint(this);
                    if (pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Pen || pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
                    {
                        try
                        {
                            m_source.TryRedirectForManipulation(pointer);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex);
                        }
                    }
                    else
                    {
                        this.CapturePointer(e.Pointer);
                        _GestureRecognizer.ProcessDownEvent(pointer);
                    }
                }
            }
        }

        private void _PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            if (pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var pointers = e.GetIntermediatePoints(this);
                _GestureRecognizer.ProcessMoveEvents(pointers);
            }
        }


        private void _PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            if (pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                _GestureRecognizer.CompleteGesture();
                this.ReleasePointerCapture(e.Pointer);
            }
        }

        private void _PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            if (pointer.PointerDevice.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                _GestureRecognizer.ProcessUpEvent(pointer);
                this.ReleasePointerCapture(e.Pointer);
            }
        }

        #endregion Pointer Events

        #region Interaction Tracker Events
        public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
        {
            IsAnimating = true;
        }

        public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
        {
            if (IsAnimating)
            {
                IsAnimating = false;
            }
            else
            {
                IsCoreSelectedChanged = true;
                var delta = sender.Position.X - Index * this.ActualWidth;
                if (delta < -40)
                {
                    Index--;
                    if (Index < 0)
                    {
                        Index = 0;
                        OnPrevPageSelected();
                    }
                }
                else if (delta > 40)
                {
                    Index++;
                    if (Index > Count - 1)
                    {
                        Index = Count - 1;
                        OnNextPageSelected();
                    }
                }
                GotoIndex(Index);
                IsCoreSelectedChanged = false;
            }
        }

        public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
        {
            IsAnimating = false;

        }

        public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
        {
            IsAnimating = false;

        }

        public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
        {

        }

        public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
        {

        }
        #endregion Interaction Tracker Events
        #endregion Events
    }

    public enum SetContentMode
    {
        First, Last, Stay
    }
}
