﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monbsoft.MachineMonitor.Controls
{
    /// <summary>
    /// An alternative impementation of a progress bar.
    /// Progression is represented by a loop filling up in a clockwise fashion.
    /// Like the traditional progress bar, it inherits from RangeBase, so Minimum, Maximum and Value properties work the same way.
    /// </summary>
    [TemplatePart(Name = OutlineFigurePartName, Type = typeof(PathFigure))]
    [TemplatePart(Name = OutlineArcPartName, Type = typeof(ArcSegment))]
    [TemplatePart(Name = BarFigurePartName, Type = typeof(PathFigure))]
    [TemplatePart(Name = BarArcPartName, Type = typeof(ArcSegment))]
    [TemplatePart(Name = ValueTextPartName, Type = typeof(TextBlock))]
    public class RadialProgressBar : ProgressBar
    {
        #region Champs
        /// <summary>
        /// Identifies the Outline dependency property
        /// </summary>
        public static readonly DependencyProperty OutlineProperty = DependencyProperty.Register(nameof(Outline), typeof(Brush), typeof(RadialProgressBar), new PropertyMetadata(new SolidColorBrush()));

        /// <summary>
        /// Identifies the Thickness dependency property
        /// </summary>
        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(RadialProgressBar), new PropertyMetadata(0.0, ThicknessChangedHandler));

        private const string BarArcPartName = "BarArcPart";
        private const string BarFigurePartName = "BarFigurePart";
        private const string OutlineArcPartName = "OutlineArcPart";
        private const string OutlineFigurePartName = "OutlineFigurePart";
        private const string ValueTextPartName = "ValueTextPart";

        private bool allTemplatePartsDefined = false;
        private ArcSegment barArc;
        private PathFigure barFigure;
        private ArcSegment outlineArc;
        private PathFigure outlineFigure;
        private TextBlock valueText;
        #endregion

        #region Constructeurs
        /// <summary>
        /// Initializes a new instance of the <see cref="RadialProgressBar"/> class.
        /// Create a default circular progress bar
        /// </summary>
        public RadialProgressBar()
        {
            DefaultStyleKey = typeof(RadialProgressBar);
            SizeChanged += SizeChangedHandler;
        }
        #endregion

        #region Propriétés
        /// <summary>
        /// Gets or sets the color of the circular ouline on which the segment is drawn
        /// </summary>
        public Brush Outline
        {
            get { return (Brush)GetValue(OutlineProperty); }
            set { SetValue(OutlineProperty, value); }
        }
        /// <summary>
        /// Gets or sets the thickness of the circular ouline and segment
        /// </summary>
        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }
        /// <summary>
        /// Update the visual state of the control when its template is changed.
        /// </summary>
        #endregion

        #region Méthodes
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            outlineFigure = GetTemplateChild(OutlineFigurePartName) as PathFigure;
            outlineArc = GetTemplateChild(OutlineArcPartName) as ArcSegment;
            barFigure = GetTemplateChild(BarFigurePartName) as PathFigure;
            barArc = GetTemplateChild(BarArcPartName) as ArcSegment;
            valueText = GetTemplateChild(ValueTextPartName) as TextBlock;

            allTemplatePartsDefined = outlineFigure != null && outlineArc != null && barFigure != null && barArc != null;

            RenderAll();
        }
        /// <summary>
        /// Called when the Maximum property changes.
        /// </summary>
        /// <param name="oldMaximum">Old value of the Maximum property.</param>
        /// <param name="newMaximum">New value of the Maximum property.</param>
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            base.OnMaximumChanged(oldMaximum, newMaximum);
            RenderSegment();
        }
        /// <summary>
        /// Called when the Minimum property changes.
        /// </summary>
        /// <param name="oldMinimum">Old value of the Minimum property.</param>
        /// <param name="newMinimum">New value of the Minimum property.</param>
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            base.OnMinimumChanged(oldMinimum, newMinimum);
            RenderSegment();
        }
        /// <summary>
        /// Called when the Value property changes.
        /// </summary>
        /// <param name="oldValue">Old value of the Value property.</param>
        /// <param name="newValue">New value of the Value property.</param>
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            valueText.Text = $"{Math.Round(newValue, 0)}%";
            RenderSegment();
        }
        // Render outline and progress segment when thickness is changed
        private static void ThicknessChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as RadialProgressBar;
            sender.RenderAll();
        }

        // Compute size of ellipse so that the outer edge touches the bounding rectangle
        private Size ComputeEllipseSize()
        {
            var safeThickness = Math.Max(Thickness, 0.0);
            var width = Math.Max((ActualWidth - safeThickness) / 2.0, 0.0);
            var height = Math.Max((ActualHeight - safeThickness) / 2.0, 0.0);
            return new Size(width, height);
        }
        private double ComputeNormalizedRange()
        {
            var range = Maximum - Minimum;
            var delta = Value - Minimum;
            var output = range == 0.0 ? 0.0 : delta / range;
            output = Math.Min(Math.Max(0.0, output), 0.9999);
            return output;
        }
        // Render the progress segment and the loop outline. Needs to run when control is resized or retemplated
        private void RenderAll()
        {
            if (!allTemplatePartsDefined)
            {
                return;
            }

            var size = ComputeEllipseSize();
            var segmentWidth = size.Width;
            var translationFactor = Math.Max(Thickness / 2.0, 0.0);

            outlineFigure.StartPoint = barFigure.StartPoint = new Point(segmentWidth + translationFactor, translationFactor);
            outlineArc.Size = barArc.Size = new Size(segmentWidth, size.Height);
            outlineArc.Point = new Point(segmentWidth + translationFactor - 0.05, translationFactor);

            RenderSegment();
        }
        // Render the segment representing progress ratio.
        private void RenderSegment()
        {
            if (!allTemplatePartsDefined)
            {
                return;
            }

            var normalizedRange = ComputeNormalizedRange();

            var angle = 2 * Math.PI * normalizedRange;
            var size = ComputeEllipseSize();
            var translationFactor = Math.Max(Thickness / 2.0, 0.0);

            double x = (Math.Sin(angle) * size.Width) + size.Width + translationFactor;
            double y = (((Math.Cos(angle) * size.Height) - size.Height) * -1) + translationFactor;

            barArc.IsLargeArc = angle >= Math.PI;
            barArc.Point = new Point(x, y);
        }
        // Render outline and progress segment when control is resized.
        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            var self = sender as RadialProgressBar;
            self.RenderAll();
        }
        #endregion
    }
}
