using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sensitive
{
    public class AcDrawNode
    {
        public Rectangle rect;
        public AcNode ac_node;
    }
    public class AcDraw
    {
        public AcDraw(Int32 _imgw, Int32 _imgh)
        {
            interval = 100;
            imgw = _imgh;
            imgh = _imgh;
            bmp = new Bitmap(imgw, imgh);
            g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.HighQuality;
            show_root_faile_connection = false;

            nodes = new Dictionary<AcNode, AcDrawNode>();
        }

        ~AcDraw()
        {
            
        }

        public void test()
        {
            var nodePos = new Point(200, 50);
            var nodeRect = DrawNode(nodePos, "256", Brushes.Black);
            var curveRect = new Rectangle();
            curveRect.X = nodeRect.X + nodeRect.Width / 2;
            curveRect.Y = nodeRect.Y + nodeRect.Height;
            curveRect.Width = 0;
            curveRect.Height = 150;
            
            DrawCurve(curveRect);
        }

        public void save()
        {
            bmp.Save("test.bmp");
        }


        public void drawAcNodes(AcNode root)
        {
            ac_root = root;
            drawOneAcNode(new Point(imgw / 2, 50), root);
            Queue<AcNode> line = new Queue<AcNode>();
            foreach (var n in root.nexts)
            {
                line.Enqueue(n.Value);
            }

            if (line.Count == 0)
            {
                return;
            }

            drawOnelineAcNodes(50 + 100, line);
        }

        public void drawOnelineAcNodes(Int32 posY, Queue<AcNode> line)
        {
            
            if (line.Count == 0)
            {
                return;
            }

            var lineStartX = 0;
            if (line.Count % 2 == 0)
            {
                lineStartX = imgw / 2 - (line.Count / 2 * interval - interval / 2);
            } else
            {
                lineStartX = imgw / 2 - (line.Count / 2 * interval);
            }
            int index = 0;
            child_line = new Queue<AcNode>();
            foreach (var node in line)
            {
                ++index;
                var drawNode = drawOneAcNode(new Point(lineStartX, posY), node);
                drawTwoNodeConnection(drawNode, nodes[drawNode.ac_node.parent], false, index, line.Count);
                if (drawNode.ac_node.failed != ac_root || show_root_faile_connection)
                {
                    drawTwoNodeConnection(drawNode, nodes[drawNode.ac_node.failed], true, index, line.Count);
                }
                lineStartX += interval;
                foreach (var n in node.nexts)
                {
                    child_line.Enqueue(n.Value);
                }
            }

            line = child_line;
            drawOnelineAcNodes(posY + interval, line);
        }

        private AcDrawNode drawOneAcNode(Point pos, AcNode node)
        {
            AcDrawNode drawNode = new AcDrawNode();
            drawNode.ac_node = node;
            drawNode.rect = DrawNode(pos, node.parent == null ? "root" : node.value.ToString(), node.is_end ? Brushes.Red : Brushes.Black);
            nodes.Add(node, drawNode);
            return drawNode;
        }

        static public Point center(Rectangle rect)
        {
            return new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }

        private void drawTwoNodeConnection(AcDrawNode n1, AcDrawNode n2, bool useCap, int lineIndex, int lineCount)
        {
            var center1 = center(n1.rect);
            var center2 = center(n2.rect);
            Rectangle rect;
            if (useCap) {
                if (n1.rect.Right < n2.rect.Left)
                {
                    // 左指向右
                    if (lineIndex == 1)
                    { // 最左边
                        rect = new Rectangle(n1.rect.Left, center1.Y, n2.rect.Left - n1.rect.Left, center2.Y - center1.Y);
                    } else
                    {
                        rect = new Rectangle(n1.rect.Right, center1.Y, n2.rect.Left - n1.rect.Right, center2.Y - center1.Y);
                    }
                    
                }
                else if (n1.rect.Left > n2.rect.Right)
                {
                    // 右指向左
                    if (lineIndex == lineCount)
                    {
                        rect = new Rectangle(n1.rect.Right, center1.Y, n2.rect.Right - n1.rect.Right, center2.Y - center1.Y);
                    } else
                    {
                        rect = new Rectangle(n1.rect.Left, center1.Y, n2.rect.Right - n1.rect.Left, center2.Y - center1.Y);
                    }
                }
                else
                {
                    rect = new Rectangle(center1.X, n1.rect.Top, center2.X - center1.X, n2.rect.Bottom - n1.rect.Top);
                }
            }
            else
            {
                rect = new Rectangle(center1.X, n1.rect.Top, center2.X - center1.X, n2.rect.Bottom - n1.rect.Top);
            }
            DrawCurve(rect, useCap);
        }

        public Rectangle DrawNode(Point pos, string text, Brush color)
        {
            int w = 60, h = 40, fontSize = 14;
            Rectangle rect = new Rectangle(pos.X - w / 2, pos.Y - h / 2, w, h);
            var pen = new Pen(color, 2.5f);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawEllipse(pen, rect);

            StringFormat sf = new StringFormat();
            sf.LineAlignment = StringAlignment.Center;
            sf.Alignment = StringAlignment.Center;
            g.SmoothingMode = SmoothingMode.Default;
            g.DrawString(text, new Font("微软雅黑", fontSize), color, rect, sf);
            return rect;
        }

        public void DrawCurve(Rectangle rect, bool useCap = false)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            var points1 = new List<PointF>();
            //if (useCap)
            //{
            //    points1.Add(new PointF(rect.X, rect.Y));
            //    points1.Add(new PointF(rect.X - rect.Width/6, rect.Y - rect.Height/3.5f));
            //    points1.Add(new PointF(rect.X + rect.Width - rect.Width/6, rect.Y + rect.Height - rect.Height/3.5f));
            //    points1.Add(new PointF(rect.X + rect.Width, rect.Y + rect.Height));
            //} else
            {
                float fact = (1 - (rect.Height / interval - 1) * 0.45f);
                points1.Add(new PointF(rect.X, rect.Y));
                points1.Add(new PointF(rect.X + rect.Width / 6, rect.Y + rect.Height / 3.5f));
                points1.Add(new PointF(rect.X + rect.Width - rect.Width / 6 * fact, rect.Y + rect.Height - rect.Height / 3.5f));
                points1.Add(new PointF(rect.X + rect.Width, rect.Y + rect.Height));
            }
            var pen = new Pen(Brushes.Black, 2.5f);
            if (useCap)
            {
                var capw = 4;
                pen = new Pen(Brushes.Red, 1.5f);
                pen.DashStyle = DashStyle.Dash;
                GraphicsPath capPath = new GraphicsPath();
                // capPath.AddLine(-capw, -capw, capw, -capw);
                capPath.AddLine(-capw / 2, -capw, 0, 0);
                capPath.AddLine(capw / 2, -capw, 0, 0);
                pen.CustomEndCap = new CustomLineCap(null, capPath);
            }
            g.DrawCurve(pen, points1.ToArray(), 0.5f);
        }

        public bool show_root_faile_connection;
        private Int32 interval;
        private Int32 imgw;
        private Int32 imgh;
        private Graphics g;
        private Bitmap bmp;
        Queue<AcNode> line;
        Queue<AcNode> child_line;
        AcNode ac_root;

        private Dictionary<AcNode, AcDrawNode> nodes;
    }
}
