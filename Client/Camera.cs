using SadRogue.Primitives;

namespace Client
{
    public interface ICamera
    {
        int ViewWidth { get; }
        int ViewHeight { get; }
        Point ViewSize { get; set; }
        int CenterX { get; }
        int CenterY { get; }
        Point Center { get; }

        Rectangle View { get; }

        delegate void CameraEventHandler(ICamera cam);
        CameraEventHandler? OnViewSizeChanged { get; set; }
        CameraEventHandler? OnCameraPosChanged { get; set; }
    }

    public class FixedCenterCamera : ICamera
    {
        public int ViewWidth { get; private set; }
        public int ViewHeight { get; private set; }

        public Point ViewSize
        {
            get => new Point(ViewWidth, ViewHeight);
            set {
                ViewWidth = value.X;
                ViewHeight = value.Y;

                OnViewSizeChanged?.Invoke(this);
            }
        }

        public int CenterX { get; private set; }
        public int CenterY { get; private set; }

        public Point Center 
        { 
            get => new Point(CenterX, CenterY);
        }

        public Rectangle View => Rectangle.WithPositionAndSize(default, ViewSize).WithCenter(Center);

        public ICamera.CameraEventHandler? OnViewSizeChanged { get; set; }
        public ICamera.CameraEventHandler? OnCameraPosChanged { get; set; }

        public FixedCenterCamera(int viewWidth, int viewHeight)
            => (ViewWidth, ViewHeight) = (viewWidth, ViewHeight);

        public void SetCenter(int pxX, int pxY)
        {
            CenterX = pxX;
            CenterY = pxY;

            OnCameraPosChanged?.Invoke(this);
        }
    }
}