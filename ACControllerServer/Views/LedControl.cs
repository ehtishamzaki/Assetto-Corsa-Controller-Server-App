using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace ACControllerServer.Views
{

    /// <summary>
    /// Interaction logic for LedControl
    /// </summary>
    public class LedControl : ContentControl
    {

        /// <summary>
        /// Constructor of the led control
        /// </summary>
        public LedControl()
        {
            Loaded += LoadLeds;
        }

        #region Properties

        /// <summary>
        /// Ellipses controls
        /// </summary>
        private Ellipse _Ellipse = new Ellipse();

        /// <summary>
        /// Orientation of the leds
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(LedControl),
                new PropertyMetadata(Orientation.Horizontal));


        /// <summary>
        /// Colors of the leds in on mode. Amount of colors equal the amount of leds displayed
        /// </summary>
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Leds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(LedControl),
                new PropertyMetadata(Brushes.Red, LedChanged));

        /// <summary>
        /// Thickness of Border around the led
        /// </summary>
        public double LedBorderThickness
        {
            get { return (double)GetValue(LedBorderThicknessProperty); }
            set { SetValue(LedBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LedBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LedBorderThicknessProperty =
            DependencyProperty.Register("LedBorderThickness", typeof(double), typeof(LedControl), 
                new PropertyMetadata(5.0, LedChanged));

        /// <summary>
        /// Change the color of indicator using animation
        /// </summary>
        public bool IsAnimated
        {
            get { return (bool)GetValue(IsAnimatedProperty); }
            set { SetValue(IsAnimatedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAnimated.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.Register("IsAnimated", typeof(bool), typeof(LedControl), new PropertyMetadata(false));

        /// <summary>
        /// Speed of Animation, Value Range 0.1 to 1.0
        /// </summary>
        public double AnimationSpeed
        {
            get { return (double)GetValue(AnimationSpeedProperty); }
            set { SetValue(AnimationSpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnimationSpeed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnimationSpeedProperty =
            DependencyProperty.Register("AnimationSpeed", typeof(double), typeof(LedControl), new PropertyMetadata(0.5));


        #endregion

        #region Method

        /// <summary>
        /// Property changed callback for LEDs
        /// </summary>
        /// <param name="d">The DependencyObject</param>
        /// <param name="e">The DependencyPropertyChangedEventArgs</param>
        private static void LedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LedControl)d).LoadLeds(d, null);
        }

        /// <summary>
        /// Load led into the control content
        /// </summary>
        /// <param name="sender">Object sender</param>
        /// <param name="e">Routed event atguments</param>
        private void LoadLeds(object sender, RoutedEventArgs e)
        {
            FrameworkElement parent = Parent as FrameworkElement;
            double size;

            // get the size based on orientation
            if (Orientation == Orientation.Horizontal)
                size = Height;
            else
                size = Width;

            // Give it some size if forgotten to define width or height in combination with orientation
            if ((size.Equals(double.NaN)) && (parent != null))
            {
                if (parent.ActualWidth != double.NaN)
                {
                    size = parent.ActualWidth;
                }
                else if (parent.ActualHeight != double.NaN)
                {
                    size = parent.ActualHeight;
                }
            }

            Color LedColor = ((SolidColorBrush)Fill).Color;
            _Ellipse.Height = size > 4 ? size - 4 : size;
            _Ellipse.Width = size > 4 ? size - 4 : size;
            _Ellipse.Margin = new Thickness(2);
            _Ellipse.StrokeThickness = LedBorderThickness;
            _Ellipse.Style = null;
            // Border for led
            RadialGradientBrush srgb = new RadialGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(255, 87, 86, 84), 0.8d),
                    new GradientStop(Color.FromArgb(255, 116, 119, 124), 0.9d),
                    new GradientStop(Color.FromArgb(255, 14, 34, 8), 0.95d),
                });

            srgb.GradientOrigin = new System.Windows.Point(0.5d, 0.5d);
            srgb.Center = new System.Windows.Point(0.5d, 0.5d);
            srgb.RadiusX = 0.5d;
            srgb.RadiusY = 0.5d;
            _Ellipse.Stroke = srgb;
            // Color of led
            RadialGradientBrush rgb = new RadialGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(150, LedColor.R, LedColor.G, LedColor.B), 0.1d),
                    new GradientStop(Color.FromArgb(200, LedColor.R, LedColor.G, LedColor.B), 0.4d),
                    new GradientStop(Color.FromArgb(255, LedColor.R, LedColor.G, LedColor.B), 1.0d),
                });

            rgb.GradientOrigin = new System.Windows.Point(0.5d, 0.5d);
            rgb.Center = new System.Windows.Point(0.5d, 0.5d);
            rgb.RadiusX = 0.5d;
            rgb.RadiusY = 0.5d;
            _Ellipse.Fill = rgb;

            Content = _Ellipse;

            if (IsAnimated)
            {
                DoubleAnimation animation = new DoubleAnimation();
                animation.From = AnimationSpeed;
                animation.To = 1.0d;
                animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                animation.AutoReverse = false;
                _Ellipse.Fill.BeginAnimation(Brush.OpacityProperty, animation);
            }

        }

        #endregion

    }
}
