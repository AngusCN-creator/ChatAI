using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChatControl.Utils
{
    /// <summary>
    /// GDI+ 扩展方法：圆角、圆形头像、双缓冲
    /// </summary>
    public static class GraphicsExtensions
    {
        /// <summary>
        /// 绘制圆角矩形填充（画未读角标用）
        /// </summary>
        public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
        {
            using (GraphicsPath path = GetRoundedRectPath(x, y, width, height, radius))
            {
                g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 获取圆角矩形路径
        /// </summary>
        private static GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        /// <summary>
        /// 为控件开启双缓冲（解决ListBox闪烁、缩放卡顿）
        /// </summary>
        public static void SetDoubleBuffered(this Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(control, enable, null);
        }

        /// <summary>
        /// 绘制圆形头像（匹配你的效果图样式）
        /// </summary>
        /// <summary>
        /// 绘制圆形图片（修复空引用、资源释放、参数无效问题）
        /// </summary>
        public static void DrawCircleImage(this Graphics g, Image? image, Rectangle rect)
        {
            if (g == null) return;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // 1. 兜底：如果图片为空，用默认图标
            Image safeImage = image ?? SystemIcons.Application.ToBitmap();

            try
            {
                // 2. 创建圆形裁剪路径
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    g.SetClip(path);

                    // 3. 高质量绘制（解决拉伸、模糊）
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawImage(safeImage, rect);

                    // 4. 取消裁剪，避免影响后续绘制
                    g.ResetClip();
                }
            }
            catch (Exception ex)
            {
                // 异常兜底：绘制默认图标，避免程序崩溃
                g.DrawImage(SystemIcons.Application.ToBitmap(), rect);
                Console.WriteLine($"绘制圆形头像异常：{ex.Message}");
            }
            finally
            {
                // 5. 仅当 safeImage 是我们自己创建的默认图标时才释放
                if (safeImage == SystemIcons.Application.ToBitmap())
                {
                    safeImage.Dispose();
                }
            }
        }
    }
}