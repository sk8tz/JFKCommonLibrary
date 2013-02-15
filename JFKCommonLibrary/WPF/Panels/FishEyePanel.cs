using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace JFKCommonLibrary.WPF.Panels
{
    /// <summary>
    /// FishEyePanel (from http://www.codeproject.com/Articles/15705/FishEyePanel-FanPanel-Examples-of-custom-layout-pa)
    /// </summary>

    public class FishEyePanel : Panel
    {
        enum AnimateState { None, Up, Down };

        public FishEyePanel()
        {
            this.Background = Brushes.Transparent;
            this.MouseMove += new MouseEventHandler(this.FishEyePanel_MouseMove);
            this.MouseEnter += new MouseEventHandler(this.FishEyePanel_MouseEnter);
            this.MouseLeave += new MouseEventHandler(this.FishEyePanel_MouseLeave);
        }

        public double Magnification
        {
            get { return (double)this.GetValue(MagnificationProperty); }
            set { this.SetValue(MagnificationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Magnification.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MagnificationProperty =
            DependencyProperty.Register("Magnification", typeof(double), typeof(FishEyePanel), new UIPropertyMetadata(2d));

        public int AnimationMilliseconds
        {
            get { return (int)this.GetValue(AnimationMillisecondsProperty); }
            set { this.SetValue(AnimationMillisecondsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnimationMilliseconds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnimationMillisecondsProperty =
            DependencyProperty.Register("AnimationMilliseconds", typeof(int), typeof(FishEyePanel), new UIPropertyMetadata(125));


        // If set true we scale different sized children to a constant width
        public bool ScaleToFit
        {
            get { return (bool)this.GetValue(ScaleToFitProperty); }
            set { this.SetValue(ScaleToFitProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScaleToFit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleToFitProperty =
            DependencyProperty.Register("ScaleToFit", typeof(bool), typeof(FishEyePanel), new UIPropertyMetadata(true));

        public enum ScaleFromEnum { Middle, Up, Down };

        public ScaleFromEnum ScaleFrom
        {
            get { return (ScaleFromEnum)GetValue(ResizeFromProperty); }
            set { SetValue(ResizeFromProperty, value); }
        }

        public static readonly DependencyProperty ResizeFromProperty =
        DependencyProperty.Register("ScaleFrom", typeof(ScaleFromEnum), typeof(FishEyePanel), new UIPropertyMetadata(ScaleFromEnum.Middle));

        private bool animating = false;
        private Size ourSize;
        private double totalChildWidth = 0;
        private bool wasMouseOver = false;

        void FishEyePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.animating)
                this.InvalidateArrange();
        }

        void FishEyePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.InvalidateArrange();
        }

        void FishEyePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            this.InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size idealSize = new Size(0, 0);

            // Allow children as much room as they want - then scale them
            Size size = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
            foreach (UIElement child in this.Children)
            {
                child.Measure(size);
                idealSize.Width += child.DesiredSize.Width;
                idealSize.Height = Math.Max(idealSize.Height, child.DesiredSize.Height);
            }

            // EID calls us with infinity, but framework doesn't like us to return infinity
            if (double.IsInfinity(availableSize.Height) || double.IsInfinity(availableSize.Width))
                return idealSize;
            else
                return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Children == null || this.Children.Count == 0)
                return finalSize;

            this.ourSize = finalSize;
            this.totalChildWidth = 0;

            foreach (UIElement child in this.Children)
            {
                // If this is the first time we've seen this child, add our transforms
                if (child.RenderTransform as TransformGroup == null)
                {
                    child.RenderTransformOrigin = new Point(0, 0.5);
                    TransformGroup group = new TransformGroup();
                    child.RenderTransform = group;

                    child.RenderTransform = group;
                    ScaleTransform st = new ScaleTransform();
                    switch (ScaleFrom)
                    {
                        case ScaleFromEnum.Middle:
                            break;
                        case ScaleFromEnum.Down:
                            st.CenterY = child.DesiredSize.Height / 2;
                            break;
                        case ScaleFromEnum.Up:
                            st.CenterY = -child.DesiredSize.Height / 2;
                            break;
                    }
                    group.Children.Add(st);
                    group.Children.Add(new TranslateTransform());
//                    group.Children.Add(new RotateTransform());
                }

                child.Arrange(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
                this.totalChildWidth += child.DesiredSize.Width;
            }

            this.AnimateAll();

            return finalSize;
        }

        void AnimateAll()
        {
            if (this.Children == null || this.Children.Count == 0)
                return;

            this.animating = true;

            double childWidth = this.ourSize.Width / this.Children.Count;
            // Scale the children so they fit in our size
            double overallScaleFactor = this.ourSize.Width / this.totalChildWidth;

            UIElement prevChild = null;
            UIElement theChild = null;
            UIElement nextChild = null;

            double widthSoFar = 0;
            double theChildX = 0;
            double ratio = 0;

            if (this.IsMouseOver)
            {
                double x = Mouse.GetPosition(this).X;
                foreach (UIElement child in this.Children)
                {
                    if (theChild == null)
                        theChildX = widthSoFar;
                    widthSoFar += (this.ScaleToFit ? childWidth : child.DesiredSize.Width * overallScaleFactor);
                    if (x < widthSoFar && theChild == null)
                        theChild = child;
                    if (theChild == null)
                        prevChild = child;
                    if (nextChild == null && theChild != child && theChild != null)
                    {
                        nextChild = child;
                        break;
                    }
                }
                if (theChild != null)
                    ratio = (x - theChildX) / (this.ScaleToFit ? childWidth : (theChild.DesiredSize.Width * overallScaleFactor));    // Range 0-1 of where the mouse is inside the child
            }

            // These next few lines took two of us hours to write!
            double mag = this.Magnification;
            double extra = 0;
            if (theChild != null)
                extra += (mag - 1);

            if (prevChild == null)
                extra += (ratio * (mag - 1));
            else if (nextChild == null)
                extra += ((mag - 1) * (1 - ratio));
            else
                extra += (mag - 1);

            double prevScale = this.Children.Count * (1 + ((mag - 1) * (1 - ratio))) / (this.Children.Count + extra);
            double theScale = (mag * this.Children.Count) / (this.Children.Count + extra);
            double nextScale = this.Children.Count * (1 + ((mag - 1) * ratio)) / (this.Children.Count + extra);
            double otherScale = this.Children.Count / (this.Children.Count + extra);       // Applied to all non-interesting children

            // Adjust for different sized children - we overmagnify large children, so shrink the others
            if (!this.ScaleToFit && this.IsMouseOver)
            {
                double bigWidth = 0;
                double actualWidth = 0;
                if (prevChild != null)
                {
                    bigWidth += prevScale * prevChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += prevChild.DesiredSize.Width;
                }
                if (theChild != null)
                {
                    bigWidth += theScale * theChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += theChild.DesiredSize.Width;
                }
                if (nextChild != null)
                {
                    bigWidth += nextScale * nextChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += nextChild.DesiredSize.Width;
                }
                double w = (this.totalChildWidth - actualWidth) * overallScaleFactor * otherScale;
                otherScale *= (this.ourSize.Width - bigWidth) / w;
            }

            widthSoFar = 0;
            double duration = 0;
            if (this.wasMouseOver != this.IsMouseOver)
                duration = this.AnimationMilliseconds;

            foreach (UIElement child in this.Children)
            {
                double scale = otherScale;
                if (child == prevChild)
                {
                    scale = prevScale;
                }
                else if (child == theChild)
                {
                    scale = theScale;
                }
                else if (child == nextChild)
                {
                    scale = nextScale;
                }

                if (this.ScaleToFit)
                {
                    // Now scale each individual child so it is a standard width
                    scale *= childWidth / child.DesiredSize.Width;
                }
                else
                {
                    // Apply overall scale so all children fit our width
                    scale *= overallScaleFactor;
                }

                this.AnimateTo(child, 0, widthSoFar, (this.ourSize.Height - child.DesiredSize.Height) / 2, scale, duration);
                widthSoFar += child.DesiredSize.Width * scale;
            }

            this.wasMouseOver = this.IsMouseOver;
        }

        private void AnimateTo(UIElement child, double r, double x, double y, double s, double duration)
        {
            TransformGroup group = (TransformGroup)child.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform trans = (TranslateTransform)group.Children[1];
//            RotateTransform rot = (RotateTransform)group.Children[2];

            if (duration == 0)
            {
                trans.BeginAnimation(TranslateTransform.XProperty, null);
                trans.BeginAnimation(TranslateTransform.YProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
//                rot.BeginAnimation(RotateTransform.AngleProperty, null);
                trans.X = x;
                trans.Y = y;
                scale.ScaleX = s;
                scale.ScaleY = s;
//                rot.AngleProperty = r;
                this.animation_Completed(null, null);
            }
            else
            {
                trans.BeginAnimation(TranslateTransform.XProperty, this.MakeAnimation(x, duration, this.animation_Completed));
                trans.BeginAnimation(TranslateTransform.YProperty, this.MakeAnimation(y, duration));
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, this.MakeAnimation(s, duration));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, this.MakeAnimation(s, duration));
//                rot.BeginAnimation(RotateTransform.AngleProperty, MakeAnimation(r, duration));
            }
        }

        private DoubleAnimation MakeAnimation(double to, double duration)
        {
            return this.MakeAnimation(to, duration, null);
        }

        private DoubleAnimation MakeAnimation(double to, double duration, EventHandler endEvent)
        {
            DoubleAnimation anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(duration));
            anim.AccelerationRatio = 0.2;
            anim.DecelerationRatio = 0.7;
            if (endEvent != null)
                anim.Completed += endEvent;
            return anim;
        }

        void animation_Completed(object sender, EventArgs e)
        {
            this.animating = false;
        }
    }
}
