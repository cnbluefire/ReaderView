using ReaderView.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Composition;
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
    public sealed class ReaderView : Control
    {
        public ReaderView()
        {
            this.DefaultStyleKey = typeof(ReaderView);

            PointerWheelChangedEventHandler = new PointerEventHandler(_PointerWheelChanged);
            this.AddHandler(UIElement.PointerWheelChangedEvent, PointerWheelChangedEventHandler, true);
            this.SizeChanged += ReaderView_SizeChanged;

            IndexFliter = new EventTimeFliter();
            CreateContentWaiter = new EventWaiter();
            CreateContentWaiter.ResetWhenWaitCall = true;
            CreateContentWaiter.Arrived += CreateContentWaiter_Arrived;

            this.Unloaded += (s, a) =>
            {
                this.RemoveHandler(UIElement.PointerWheelChangedEvent, PointerWheelChangedEventHandler);

            };

        }

        string content;
        double startX = 0;
        bool IsCoreSelectedChanged;
        EventTimeFliter IndexFliter;
        EventWaiter CreateContentWaiter;

        Compositor compositor;
        Vector3KeyFrameAnimation OffsetAnimation;
        Visual PanelVisual;

        PointerEventHandler PointerWheelChangedEventHandler;

        StackPanel ContentPanel;
        Border ContentBorder;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ContentPanel = GetTemplateChild(nameof(ContentPanel)) as StackPanel;
            ContentBorder = GetTemplateChild(nameof(ContentBorder)) as Border;

            SetupComposition();
        }

        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            if (IndexFliter.IsEnable)
            {
                PanelVisual.StopAnimation("Offset");
                startX = -Index * this.ActualWidth;
            }
            base.OnManipulationStarted(e);
        }

        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
        {
            if (Math.Abs(e.Cumulative.Translation.X) < this.ActualWidth)
            {
                PanelVisual.Offset = new Vector3((float)(startX + e.Cumulative.Translation.X), 0f, 0f);
            }

            base.OnManipulationDelta(e);
        }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            IsCoreSelectedChanged = true;
            if (e.Cumulative.Translation.X > 150 || e.Velocities.Linear.X > 0.5)
            {
                Index--;
                if (Index < 0) Index = 0;
            }
            else if (e.Cumulative.Translation.X < -150 || e.Velocities.Linear.X < -0.5)
            {
                Index++;
                if (Index > Count - 1) Index = Count - 1;
            }
            GotoIndex(Index);
            IsCoreSelectedChanged = false;
        }


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

        private void SetupComposition()
        {
            compositor = Window.Current.Compositor;

            OffsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            OffsetAnimation.Duration = TimeSpan.FromSeconds(0.15d);
            OffsetAnimation.Target = "Offset";
            OffsetAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;

            PanelVisual = ElementCompositionPreview.GetElementVisual(ContentPanel);
        }

        private void GotoIndex(int index, bool UseAnimation = true)
        {
            if (UseAnimation)
            {
                OffsetAnimation.InsertKeyFrame(1f, new Vector3((float)(-this.ActualWidth * index), 0f, 0f));
                PanelVisual.StartAnimation("Offset", OffsetAnimation);
            }
            else
            {
                PanelVisual.Offset = new Vector3((float)(-this.ActualWidth * index), 0f, 0f);
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



        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.Register("Index", typeof(int), typeof(ReaderView), new PropertyMetadata(0, (s, a) =>
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

                        if (index < 0) sender.OnPrevPageSelected();
                        else if (index > sender.Count - 1) sender.OnNextPageSelected();
                    }
                }
            }));



        public int Count
        {
            get { return (int)GetValue(CountProperty); }
            private set { SetValue(CountProperty, value); }
        }

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register("Count", typeof(int), typeof(ReaderView), new PropertyMetadata(0));



        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register("LineHeight", typeof(double), typeof(ReaderView), new PropertyMetadata(10d, (s, a) =>
            {
                if (a.NewValue != a.OldValue)
                {
                    if (s is ReaderView sender)
                    {
                        sender.CreateContentWaiter.Wait();
                    }
                }
            }));



        private void ReaderView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CreateContentWaiter.Wait();
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


        private void _PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (IndexFliter.IsEnable)
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

        public event EventHandler PrevPageSelected;
        public event EventHandler NextPageSelected;

        private void OnPrevPageSelected()
        {
            PrevPageSelected?.Invoke(this, EventArgs.Empty);
        }

        private void OnNextPageSelected()
        {
            NextPageSelected?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum SetContentMode
    {
        First, Last, Stay
    }
}
