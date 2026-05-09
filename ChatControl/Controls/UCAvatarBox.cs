using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
// 新增：用于抗锯齿绘制，让默认头像更平滑
using System.Drawing.Drawing2D;

namespace ChatControl.Controls
{
    // 自定义头像选择/裁剪用户控件
    public partial class UCAvatarBox : UserControl
    {
        // 对外暴露当前头像（null时自动返回默认头像）
        // 保证存数据库时永远有值，不会为空
        public Image? AvatarImage
        {
            get
            {
                return pictureBox1?.Image ?? GetDefaultAvatar();
            }
        }

        // 对外暴露头像字节数组（用于存储到数据库）
        public byte[]? AvatarBytes => ImageToBytes(AvatarImage);

        // 控件构造函数（初始化时执行）
        public UCAvatarBox()
        {
            // 初始化控件（VS设计器自动生成的控件初始化逻辑）
            InitializeComponent();
            // 设置控件鼠标悬停时显示手型，提示可点击
            this.Cursor = Cursors.Hand;

            // 初始化时自动显示默认头像（如果PictureBox为空）
            if (pictureBox1.Image == null)
            {
                pictureBox1.Image = GetDefaultAvatar();
            }
        }

        // ===================== 核心新增：默认头像绘制（小圆头+大圆身） =====================
        /// <summary>
        /// 生成默认头像（白色背景 + 灰色小圆头 + 灰色大圆身，无性别区分）
        /// </summary>
        /// <returns>默认头像Bitmap对象（可直接存数据库）</returns>
        private Bitmap GetDefaultAvatar()
        {
            // 获取PictureBox的尺寸，保证默认头像和控件大小一致
            int size = Math.Max(pictureBox1.Width, pictureBox1.Height);
            // 兜底：避免控件尺寸为0导致绘制失败
            if (size <= 0) size = 120;

            // 创建和控件尺寸一致的位图画布
            Bitmap defaultAvatar = new Bitmap(size, size);
            // 使用using自动释放Graphics资源，避免内存泄漏
            using (Graphics g = Graphics.FromImage(defaultAvatar))
            {
                // 开启抗锯齿，让圆形绘制更平滑，无锯齿
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // 设置头像背景为白色
                g.Clear(Color.White);

                // 创建灰色画笔/画刷（默认头像颜色）
                using (Brush grayBrush = new SolidBrush(Color.LightGray))
                {
                    // 1. 绘制小圆（头）：尺寸为整体的1/3，居中偏上
                    int headSize = size / 3;
                    int headX = (size - headSize) / 2; // 水平居中
                    int headY = size / 4;              // 垂直偏上
                    g.FillEllipse(grayBrush, headX, headY, headSize, headSize);

                    // 2. 绘制大圆（身体）：尺寸为整体的2/3，接在头部下方
                    int bodySize = size * 2 / 3;
                    int bodyX = (size - bodySize) / 2; // 水平居中
                    int bodyY = headY + headSize - 10; // 接头部下方（-10让头和身体稍微重叠，更自然）
                    g.FillEllipse(grayBrush, bodyX, bodyY, bodySize, bodySize);
                }
            }

            // 返回绘制好的默认头像
            return defaultAvatar;
        }

        // ===================== 原有功能：点击PictureBox选择头像图片 =====================
        /// <summary>
        /// PictureBox点击事件：打开文件选择器选择头像图片
        /// </summary>
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // 使用using自动释放OpenFileDialog资源
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                // 设置文件筛选：仅显示常见图片格式
                ofd.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
                // 设置对话框标题，提示用户选择头像
                ofd.Title = "请选择头像图片";

                // 如果用户选择了文件并点击"确定"
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 使用using自动释放源图片资源，避免内存泄漏
                        using (Image sourceImg = Image.FromFile(ofd.FileName))
                        {
                            // 打开裁剪窗口，传入Bitmap格式的源图片
                            ShowCropForm(new Bitmap(sourceImg));
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕获图片选择/读取异常，避免程序崩溃
                        MessageBox.Show($"图片选择失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // ===================== 原有功能：显示头像裁剪窗口 =====================
        /// <summary>
        /// 显示头像裁剪窗口，支持拖动裁剪框、滚轮缩放裁剪框
        /// </summary>
        /// <param name="sourceBmp">用户选择的源图片（Bitmap格式）</param>
        private void ShowCropForm(Bitmap sourceBmp)
        {
            // 1. 创建裁剪窗口
            Form cropForm = new Form
            {
                Text = "头像裁剪（滚轮可缩放框）",       // 窗口标题
                Size = new Size(600, 650),              // 窗口尺寸
                StartPosition = FormStartPosition.CenterParent, // 相对于父控件居中显示
                FormBorderStyle = FormBorderStyle.FixedDialog,  // 固定对话框样式，不可拉伸
                MaximizeBox = false,                    // 禁用最大化按钮
                MinimizeBox = false,                    // 禁用最小化按钮
                BackColor = Color.Black,                // 黑色背景，突出图片
                Padding = new Padding(0, 0, 0, 60)      // 底部内边距，给确认按钮留空间
            };

            // 2. 创建裁剪窗口内的图片显示控件
            PictureBox picCrop = new PictureBox
            {
                Dock = DockStyle.Fill,                  // 填充整个窗口
                Image = sourceBmp,                      // 显示用户选择的源图片
                SizeMode = PictureBoxSizeMode.Zoom,     // 按比例缩放图片，避免拉伸变形
                BackColor = Color.Black,                // 背景色黑色
                Cursor = Cursors.Default                // 默认鼠标样式
            };

            // 3. 创建确认裁剪按钮
            Button btnConfirm = new Button
            {
                Text = "确认裁剪",                      // 按钮文字
                Dock = DockStyle.Bottom,                // 停靠在窗口底部
                Height = 50,                            // 按钮高度
                Font = new Font("微软雅黑", 10, FontStyle.Bold), // 字体样式
                BackColor = Color.FromArgb(0, 122, 204),// 按钮背景色（蓝色）
                ForeColor = Color.White,                // 文字白色
                FlatStyle = FlatStyle.Flat,             // 扁平化样式
                TextAlign = ContentAlignment.MiddleCenter, // 文字居中
            };
            // 去掉按钮边框，样式更简洁
            btnConfirm.FlatAppearance.BorderSize = 0;

            // 4. 将控件添加到裁剪窗口（注意顺序：先加PictureBox，后加按钮，按钮在顶层）
            cropForm.Controls.Add(picCrop);
            cropForm.Controls.Add(btnConfirm);

            // 5. 裁剪框参数初始化
            int cropSize = 280;    // 裁剪框初始尺寸（正方形）
            int minSize = 80;      // 裁剪框最小尺寸（避免缩太小）
            int maxSize = 400;     // 裁剪框最大尺寸（避免缩太大）

            // 初始化裁剪框位置：在PictureBox中居中显示
            Rectangle cropRect = new Rectangle(
                (picCrop.Width - cropSize) / 2,  // X坐标：水平居中
                (picCrop.Height - cropSize) / 2, // Y坐标：垂直居中
                cropSize,                        // 宽度
                cropSize                         // 高度
            );

            // 拖动相关变量
            Point dragOffset = Point.Empty; // 鼠标按下时与裁剪框左上角的偏移量（避免拖动瞬移）
            bool isDragging = false;        // 是否正在拖动裁剪框

            // 6. 绘制裁剪遮罩和裁剪框（PictureBox重绘时执行）
            picCrop.Paint += (s, e) =>
            {
                // 绘制裁剪框外的半透明黑色遮罩，突出裁剪区域
                using (SolidBrush br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    // 顶部遮罩
                    e.Graphics.FillRectangle(br, 0, 0, picCrop.Width, cropRect.Top);
                    // 底部遮罩
                    e.Graphics.FillRectangle(br, 0, cropRect.Bottom, picCrop.Width, picCrop.Height - cropRect.Bottom);
                    // 左侧遮罩
                    e.Graphics.FillRectangle(br, 0, cropRect.Top, cropRect.Left, cropRect.Height);
                    // 右侧遮罩
                    e.Graphics.FillRectangle(br, cropRect.Right, cropRect.Top, picCrop.Width - cropRect.Right, cropRect.Height);
                }

                // 绘制白色裁剪框边框（3像素宽，醒目）
                using (Pen pen = new Pen(Color.White, 3))
                {
                    e.Graphics.DrawRectangle(pen, cropRect);
                }
            };

            // 7. 鼠标按下事件：开始拖动裁剪框
            picCrop.MouseDown += (s, e) =>
            {
                // 仅当左键点击裁剪框内部时，允许拖动
                if (e.Button == MouseButtons.Left && cropRect.Contains(e.Location))
                {
                    isDragging = true; // 标记为拖动状态
                    // 计算鼠标与裁剪框左上角的偏移量
                    dragOffset = new Point(e.X - cropRect.X, e.Y - cropRect.Y);
                    picCrop.Cursor = Cursors.SizeAll; // 鼠标样式改为"移动"图标
                }
            };

            // 8. 鼠标移动事件：拖动裁剪框
            picCrop.MouseMove += (s, e) =>
            {
                if (isDragging) // 如果正在拖动
                {
                    // 计算新的X/Y坐标，使用Math.Clamp限制在PictureBox范围内（避免超出）
                    int nx = Math.Clamp(e.X - dragOffset.X, 0, picCrop.Width - cropSize);
                    int ny = Math.Clamp(e.Y - dragOffset.Y, 0, picCrop.Height - cropSize);
                    // 更新裁剪框位置
                    cropRect = new Rectangle(nx, ny, cropSize, cropSize);
                    picCrop.Invalidate(); // 重绘PictureBox，更新遮罩和裁剪框位置
                }
                else
                {
                    // 非拖动状态：鼠标在裁剪框内显示手型，否则默认样式
                    picCrop.Cursor = cropRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
                }
            };

            // 9. 鼠标松开事件：结束拖动
            picCrop.MouseUp += (s, e) =>
            {
                isDragging = false; // 取消拖动标记
                picCrop.Cursor = Cursors.Default; // 恢复默认鼠标样式
            };

            // 10. 鼠标滚轮事件：缩放裁剪框
            picCrop.MouseWheel += (s, e) =>
            {
                // 滚轮向上（e.Delta>0）放大20像素，向下缩小20像素
                int change = e.Delta > 0 ? 20 : -20;
                int newSize = cropSize + change;

                // 限制缩放范围在最小/最大值之间
                if (newSize >= minSize && newSize <= maxSize)
                {
                    cropSize = newSize; // 更新裁剪框尺寸

                    // 保持裁剪框中心缩放（避免缩放时位置偏移）
                    int cx = cropRect.X + cropRect.Width / 2; // 原裁剪框中心X坐标
                    int nx = cx - cropSize / 2;               // 新X坐标（中心不变）
                    int ny = cropRect.Y + cropRect.Height / 2 - cropSize / 2; // 新Y坐标（中心不变）

                    // 限制新坐标在PictureBox范围内
                    nx = Math.Clamp(nx, 0, picCrop.Width - cropSize);
                    ny = Math.Clamp(ny, 0, picCrop.Height - cropSize);

                    // 更新裁剪框位置和尺寸
                    cropRect = new Rectangle(nx, ny, cropSize, cropSize);
                    picCrop.Invalidate(); // 重绘裁剪框
                }
            };

            // 11. 确认裁剪按钮点击事件
            btnConfirm.Click += (s, e) =>
            {
                try
                {
                    // 获取源图片真实尺寸
                    Size imgSize = sourceBmp.Size;
                    // 获取PictureBox显示尺寸
                    Size boxSize = picCrop.Size;

                    // 计算图片在PictureBox中的缩放比例（按宽/高中较小值缩放，避免变形）
                    float scale = Math.Min((float)boxSize.Width / imgSize.Width, (float)boxSize.Height / imgSize.Height);
                    // 计算图片在PictureBox中实际显示的宽/高
                    int displayW = (int)(imgSize.Width * scale);
                    int displayH = (int)(imgSize.Height * scale);
                    // 计算图片在PictureBox中居中显示的偏移X/Y（解决裁剪错位问题）
                    int displayX = (boxSize.Width - displayW) / 2;
                    int displayY = (boxSize.Height - displayH) / 2;

                    // 计算裁剪框在源图片中的相对位置比例
                    float rx = (cropRect.X - displayX) / (float)displayW; // X轴比例
                    float ry = (cropRect.Y - displayY) / (float)displayH; // Y轴比例
                    float rsize = cropSize / (float)displayW;             // 裁剪尺寸比例

                    // 转换为源图片中的实际像素坐标/尺寸
                    int rxInt = (int)(rx * imgSize.Width);    // 源图中裁剪起始X
                    int ryInt = (int)(ry * imgSize.Height);   // 源图中裁剪起始Y
                    int rsizeInt = (int)(rsize * imgSize.Width); // 源图中裁剪尺寸（正方形）

                    // 安全校验：避免裁剪尺寸超出源图片范围，导致崩溃
                    rsizeInt = Math.Min(rsizeInt, imgSize.Width - rxInt);
                    rsizeInt = Math.Min(rsizeInt, imgSize.Height - ryInt);

                    // 创建裁剪后的最终头像位图
                    Bitmap final = new Bitmap(rsizeInt, rsizeInt);
                    using (Graphics g = Graphics.FromImage(final))
                    {
                        // 设置绘图质量：抗锯齿 + 高质量插值，保证裁剪后图片清晰
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        // 从源图片裁剪指定区域，并绘制到最终位图中
                        g.DrawImage(sourceBmp,
                            new Rectangle(0, 0, rsizeInt, rsizeInt), // 目标区域（最终头像）
                            new Rectangle(rxInt, ryInt, rsizeInt, rsizeInt), // 源图片裁剪区域
                            GraphicsUnit.Pixel); // 单位：像素
                    }

                    // 将裁剪后的头像设置到主PictureBox
                    pictureBox1.Image = final;
                    // 关闭裁剪窗口
                    cropForm.Close();
                }
                catch (Exception ex)
                {
                    // 捕获裁剪异常，避免程序崩溃
                    MessageBox.Show($"裁剪失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    // 释放资源，避免内存泄漏
                    cropForm.Dispose();
                    sourceBmp.Dispose();
                }
            };

            // 显示裁剪窗口（模态窗口，阻塞直到用户关闭）
            cropForm.ShowDialog();
        }

        // ===================== 原有功能：Image转字节数组（用于存数据库） =====================
        /// <summary>
        /// 将Image对象转换为PNG格式的字节数组
        /// </summary>
        /// <param name="img">要转换的Image对象</param>
        /// <returns>PNG格式字节数组（null表示输入为空）</returns>
        private byte[]? ImageToBytes(Image? img)
        {
            // 空值校验：如果图片为空，返回null
            if (img == null) return null;
            // 使用using自动释放MemoryStream资源
            using (MemoryStream ms = new MemoryStream())
            {
                // 将图片保存为PNG格式到内存流（PNG无损，适合头像）
                img.Save(ms, ImageFormat.Png);
                // 将内存流转换为字节数组返回
                return ms.ToArray();
            }
        }
    }
}