using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using AnimationModels;

namespace TowerLightsControllerGUI.Controls
{
    /// <summary>
    /// Interaction logic for FrameViewer.xaml
    /// </summary>
    public partial class FrameViewer : FrameworkElement
    {
        public FrameViewer()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (this.Frame != null)
            {
                double windowWidth = (this.ActualWidth / this.Frame.ColumnCount);
                double windowHeight = (this.ActualHeight / this.Frame.RowCount);
                double spaceX = 0.1 * windowWidth;
                double spaceY = 0.1 * windowHeight;

                // draw the background
                drawingContext.DrawRectangle(new SolidColorBrush(new System.Windows.Media.Color() { R = 179, G = 123, B = 123, A = 255 }), null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));

                for (int row = 0; row < this.Frame.RowCount; row++)
                {
                    for (int col = 0; col < this.Frame.ColumnCount; col++)
                    {
                        double x = col * (windowWidth);
                        double y = row * (windowHeight);
                        System.Windows.Media.Color color = new System.Windows.Media.Color();
                        color.R = this.Frame.Get(row, col).Red;
                        color.G = this.Frame.Get(row, col).Green;
                        color.B = this.Frame.Get(row, col).Blue;
                        color.A = 255;
                        
                        Rect window = new Rect(x, y, windowWidth, windowHeight);
                        //window.Scale(0.9, 0.9);
                        //window.Offset(spaceX, spaceY);
                        window.Inflate(-spaceX, -spaceY);
                        drawingContext.DrawRectangle(new SolidColorBrush(color), null, window);

                    }
                }
            }
            
        }



        public KeyFrame Frame
        {
            get { return (KeyFrame)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Frame.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
            "Frame",
            typeof(KeyFrame),
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    }
}
